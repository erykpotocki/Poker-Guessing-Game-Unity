using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoundLogUI : MonoBehaviour, IOnEventCallback, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const byte ChatEvent=84;
    private readonly List<(string text,bool human)> messages=new();
    private RectTransform root,canvasRoot,viewport,content,header,inputRect,resizeGrip;
    private TMP_Text text,title;
    private TMP_InputField input;
    private ScrollRect scroll;
    private CanvasGroup group;
    private bool collapsed,editing,resizing;
    private Vector2 dragStart,positionStart,sizeStart;
    private float lastSent=-10;
    private readonly Dictionary<int,float> incoming=new();
    private void Awake()
    {
        var canvas=GetComponentInParent<Canvas>().rootCanvas;canvasRoot=(RectTransform)canvas.transform;
        foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}

        root=(RectTransform)transform;root.SetParent(canvasRoot,false);root.localScale=Vector3.one;
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(0,1);
        var background=GetComponent<Image>();if(background==null)background=gameObject.AddComponent<Image>();background.sprite=null;background.color=new Color(.015f,.02f,.02f,.94f);
        group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        header=ShopUI.Rect("ChatHeader",root,0,0,400,40);
        header.gameObject.AddComponent<Image>().color=new Color(.15f,.12f,.05f,.8f);
        title=ShopUI.Text(header,"CZAT",8,0,230,40,22);title.alignment=TextAlignmentOptions.Left;
        var collapse=ShopUI.Button(header,"−",0,0,52,38,()=>{collapsed=!collapsed;Refresh();});collapse.name="Collapse";
        ShopUI.Button(header,"EDYTUJ",0,0,90,38,()=>{editing=!editing;collapsed=false;Layout();}).name="EditChat";
        resizeGrip=ShopUI.Rect("UtilityResizeGrip",root,0,0,38,24);
        resizeGrip.gameObject.AddComponent<Image>().color=new Color(.8f,.6f,.15f,.8f);
        var grip=resizeGrip.gameObject.AddComponent<RoundLogResizeHandle>();grip.Owner=this;
        ShopUI.Text(resizeGrip,"///",0,0,38,24,22).raycastTarget=false;
        viewport=ShopUI.Rect("Viewport",root,8,44,384,100);viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
        content=ShopUI.Rect("Content",viewport,0,0,384,100);text=ShopUI.Text(content,"",0,0,384,100,23);text.alignment=TextAlignmentOptions.TopLeft;text.margin=Vector4.zero;text.textWrappingMode=TextWrappingModes.Normal;
        text.enableAutoSizing=false;
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        inputRect=ShopUI.Rect("MessageInput",root,8,150,320,40);inputRect.gameObject.AddComponent<Image>().color=new Color(.12f,.14f,.13f);
        input=inputRect.gameObject.AddComponent<TMP_InputField>();input.characterLimit=180;input.lineType=TMP_InputField.LineType.SingleLine;input.richText=false;
        var label=ShopUI.Text(inputRect,"",8,2,300,36,22);label.alignment=TextAlignmentOptions.Left;input.textComponent=label;input.textViewport=inputRect;
        input.onSubmit.AddListener(_=>Send());ShopUI.Button(root,"➤",340,150,52,40,Send).name="Send";
        Refresh();
    }
    private void OnEnable()=>PhotonNetwork.AddCallbackTarget(this);
    private void OnDisable()=>PhotonNetwork.RemoveCallbackTarget(this);
    private static string Escape(string s)=>(s??"").Replace("<","‹").Replace(">","›");
    public void ClearLog(){messages.Clear();Refresh();}
    public void AddSystemMessage(string value){if(!string.IsNullOrWhiteSpace(value))Add(value,false);}
    public void AddRoundHeader(int round)=>Add("<color=#FFD568>Runda "+round+"</color>",false);
    public void AddPlayerRaise(string player,string rank)=>Add(Escape(player)+": wybiera "+Escape(rank),false);
    public void AddPlayerCheck(string player)=>Add(Escape(player)+": <color=#FFD568>SPRAWDZAM</color>",false);
    public void AddPlayerLeftGame(string player)=>Add(Escape(player)+" wyszedł z gry",false);
    public void AddPlayerRejoinedGame(string player)=>Add(Escape(player)+" wrócił do gry",false);
    private void Add(string value,bool human)
    {
        messages.Add((value,human));if(messages.Count>250)messages.RemoveAt(0);Refresh();
    }
    private void Send()
    {
        string value=input.text.Trim();if(value.Length==0||!PhotonNetwork.InRoom||Time.unscaledTime-lastSent<1)return;
        lastSent=Time.unscaledTime;input.SetTextWithoutNotify("");
        PhotonNetwork.RaiseEvent(ChatEvent,value,new RaiseEventOptions{Receivers=ReceiverGroup.All},SendOptions.SendReliable);
    }
    public void OnEvent(EventData e)
    {
        if(e.Code!=ChatEvent||!(e.CustomData is string value)||value.Length>180)return;
        var player=PhotonNetwork.CurrentRoom?.GetPlayer(e.Sender);if(player==null)return;
        if(incoming.TryGetValue(e.Sender,out float previous)&&Time.unscaledTime-previous<.8f)return;
        incoming[e.Sender]=Time.unscaledTime;Add("<color=#9DE1FF>"+Escape(player.NickName)+":</color> "+Escape(value),true);
    }
    private void Refresh()
    {
        if(text==null)return;int filter=PlayerPrefs.GetInt("chat.filter",0);
        text.text=string.Join("\n",messages.Where(m=>filter==0||(filter==1?!m.human:m.human)).Select(m=>m.text));Layout();
        Canvas.ForceUpdateCanvases();scroll.StopMovement();scroll.verticalNormalizedPosition=0;
    }
    private void LateUpdate()=>Layout();
    private void Layout()
    {
        if(root==null||canvasRoot==null)return;
        float sx=canvasRoot.rect.width/Mathf.Max(1,Screen.width),sy=canvasRoot.rect.height/Mathf.Max(1,Screen.height);
        float left=Screen.safeArea.xMin*sx+8,right=Screen.safeArea.xMax*sx-8;
        float top=(Screen.height-Screen.safeArea.yMax)*sy+8,bottom=canvasRoot.rect.height-Screen.safeArea.yMin*sy-8;
        float available=Mathf.Max(1,right-left),w=Mathf.Clamp(PlayerPrefs.GetFloat("chat.width",440),Mathf.Min(250,available),available);
        float availableHeight=Mathf.Max(150,bottom-top);
        float maxHeight=Mathf.Clamp(PlayerPrefs.GetFloat("chat.height",200),150,availableHeight);
        float preferred=text.GetPreferredValues(text.text,w-16,0).y+8;
        float footer=editing?118:90;
        float bodyHeight=PlayerPrefs.GetInt("chat.manualSize",0)==1?Mathf.Max(28,maxHeight-footer):Mathf.Clamp(preferred,28,Mathf.Max(28,maxHeight-footer));
        float h=collapsed?40:bodyHeight+footer;
        float defaultX=right-MultiplayerPanelLayout.PanelWidth(canvasRoot.rect.width)-10-w;
        root.sizeDelta=new Vector2(w,h);root.anchoredPosition=new Vector2(Mathf.Clamp(PlayerPrefs.GetFloat("chat.x",defaultX),left,Mathf.Max(left,right-w)),-Mathf.Clamp(PlayerPrefs.GetFloat("chat.y",top+92),top,Mathf.Max(top,bottom-h)));
        header.sizeDelta=new Vector2(w,40);((RectTransform)header.Find("Collapse")).anchoredPosition=new Vector2(w-54,0);
        ((RectTransform)header.Find("EditChat")).anchoredPosition=new Vector2(w-150,0);
        header.Find("EditChat").GetComponentInChildren<TMP_Text>().text=editing?"GOTOWE":"EDYTUJ";
        title.rectTransform.sizeDelta=new Vector2(w-164,40);title.text=editing?"PRZESUŃ CZAT":"CZAT";
        resizeGrip.gameObject.SetActive(editing&&!collapsed);resizeGrip.anchoredPosition=new Vector2(w-42,-h+26);resizeGrip.SetAsLastSibling();
        viewport.gameObject.SetActive(!collapsed);inputRect.gameObject.SetActive(!collapsed);root.Find("Send").gameObject.SetActive(!collapsed);
        viewport.sizeDelta=new Vector2(w-16,bodyHeight);content.sizeDelta=new Vector2(w-16,Mathf.Max(preferred,bodyHeight));text.rectTransform.sizeDelta=content.sizeDelta;
        inputRect.anchoredPosition=new Vector2(8,-(bodyHeight+48));inputRect.sizeDelta=new Vector2(w-76,36);input.textComponent.rectTransform.sizeDelta=new Vector2(w-92,34);
        ((RectTransform)root.Find("Send")).anchoredPosition=new Vector2(w-62,-(bodyHeight+48));
        bool visible=PlayerPrefs.GetInt("chat.visible",1)!=0;group.alpha=visible?1:0;group.blocksRaycasts=visible;
    }
    public void OnBeginDrag(PointerEventData e)
    { BeginDrag(e,false); }
    public void BeginDrag(PointerEventData e,bool resize)
    {
        if(!editing)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot,e.position,e.pressEventCamera,out dragStart);
        resizing=resize;positionStart=root.anchoredPosition;sizeStart=root.sizeDelta;
    }
    public void OnDrag(PointerEventData e)
    {
        if(!editing)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot,e.position,e.pressEventCamera,out var p);var delta=p-dragStart;
        if(resizing){PlayerPrefs.SetInt("chat.manualSize",1);PlayerPrefs.SetFloat("chat.width",sizeStart.x+delta.x);PlayerPrefs.SetFloat("chat.height",sizeStart.y-delta.y);}
        else{PlayerPrefs.SetFloat("chat.x",positionStart.x+delta.x);PlayerPrefs.SetFloat("chat.y",-positionStart.y-delta.y);}
        Layout();
    }
    public void OnEndDrag(PointerEventData e){if(editing)PlayerPrefs.Save();}
    public static void ShowOptions(Canvas canvas)
    {
        var chat=FindFirstObjectByType<RoundLogUI>();if(chat==null)return;
        var r=ShopUI.Overlay(canvas,"ChatSettings");float w=r.rect.width;
        ShopUI.Text(r,"USTAWIENIA CZATU",20,20,w-40,65,38);
        ShopUI.Button(r,PlayerPrefs.GetInt("chat.visible",1)==1?"UKRYJ CZAT":"POKAŻ CZAT",20,100,w-40,70,()=>{PlayerPrefs.SetInt("chat.visible",1-PlayerPrefs.GetInt("chat.visible",1));Destroy(r.gameObject);chat.Refresh();});
        string[] names={"Wszystkie wiadomości","Tylko system","Tylko gracze"};
        for(int i=0;i<3;i++){int n=i;ShopUI.Button(r,names[i],20,190+i*80,w-40,70,()=>{PlayerPrefs.SetInt("chat.filter",n);chat.Refresh();Destroy(r.gameObject);});}
        ShopUI.Button(r,chat.editing?"ZAKOŃCZ EDYCJĘ":"ZMIEŃ POZYCJĘ I ROZMIAR",20,450,w-40,70,()=>{chat.editing=!chat.editing;chat.collapsed=false;PlayerPrefs.SetInt("chat.visible",1);Destroy(r.gameObject);});
        ShopUI.Button(r,"ZAMKNIJ",20,540,w-40,70,()=>Destroy(r.gameObject));
    }
}

public class RoundLogResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RoundLogUI Owner;
    public void OnBeginDrag(PointerEventData e)=>Owner.BeginDrag(e,true);
    public void OnDrag(PointerEventData e)=>Owner.OnDrag(e);
    public void OnEndDrag(PointerEventData e)=>Owner.OnEndDrag(e);
}
