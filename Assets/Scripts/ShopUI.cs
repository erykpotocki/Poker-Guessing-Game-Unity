using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopUI : MonoBehaviour
{
    private string category="avatar";
    private int currency;
    public static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    public static TMP_Text Text(Transform parent,string caption,float x,float y,float w,float h,float size=28)
    {
        var t=Rect("Label",parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.text=caption;t.fontSize=size;
        t.color=new Color(1,.9f,.7f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=size*.7f;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
    public static Button Button(Transform parent,string caption,float x,float y,float w,float h,Action action)
    {
        var r=Rect("Action",parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;
        Text(r,caption,4,0,w-8,h,25).name="ResponsiveLabel";b.onClick.AddListener(()=>action());PokerButtonTheme.ApplyTo(b);return b;
    }
    public static void Show(Canvas canvas)
    {
        if(canvas==null)return;
        var old=canvas.rootCanvas.transform.Find("ShopOverlay");if(old!=null)Destroy(old.gameObject);
        var root=Overlay(canvas,"ShopOverlay");root.gameObject.AddComponent<ShopUI>().Build();
    }
    public static RectTransform Overlay(Canvas canvas,string name)
    {
        var r=Rect(name,canvas.rootCanvas.transform,0,0,0,0);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        r.gameObject.AddComponent<Image>().color=new Color(.015f,.025f,.022f,1f);
        var layer=r.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=650;r.gameObject.AddComponent<GraphicRaycaster>();
        PortraitMenuTopBar.ApplyOverlayInset(r);Canvas.ForceUpdateCanvases();return r;
    }
    private void Build()
    {
        foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var root=(RectTransform)transform;float w=root.rect.width,h=root.rect.height;
        Text(root,"SKLEP",20,20,w-140,70,44);Button(root,"×",w-100,20,76,66,()=>Destroy(gameObject));
        Text(root,$"Złoto: {PlayerProfileService.Data.Wallet.Coins}    Diamenty: {PlayerProfileService.Data.Wallet.RewardCurrency}",20,94,w-40,56,30);
        string[] ids={"avatar","back","frame"},labels={"AVATARY","REWERSY","RAMKI"};
        for(int i=0;i<3;i++){string id=ids[i];Button(root,labels[i],20+i*(w-40)/3,164,(w-46)/3,60,()=>{category=id;Build();});}
        float sx=w/Mathf.Max(1,Screen.width);
        float left=Screen.safeArea.xMin*sx+24;
        float width=Screen.safeArea.width*sx-48;
        string[] filters={"Wszystko","Złoto","Diamenty"};
        for(int i=0;i<3;i++){int n=i;Button(root,(currency==i?"• ":"")+filters[i],left+i*width/3,242,width/3-6,58,()=>{currency=n;Build();});}
        var view=Rect("Products",root,left,320,width,Mathf.Max(100,h-346));view.gameObject.AddComponent<Image>().color=Color.clear;view.gameObject.AddComponent<RectMask2D>();
        var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        int columns=category=="back"?3:4;
        float cell=(width-16)/columns,artWidth=cell-(category=="back"?24:14),artHeight=category=="back"?artWidth*1.5f:artWidth;
        float rowHeight=artHeight+134;
        var items=CosmeticCatalog.All(category).FindAll(o=>!CosmeticCatalog.Owned(category,o.Id)&&(currency==0 || (currency==1?o.Gold>0:o.Diamonds>0)));
        var content=Rect("Content",view,0,0,width,Mathf.Max(120,Mathf.Ceil(items.Count/(float)columns)*rowHeight));scroll.viewport=view;scroll.content=content;
        for(int i=0;i<items.Count;i++)
        {
            var offer=items[i];float x=8+i%columns*cell+(cell-artWidth)*.5f,y=i/columns*rowHeight;
            var tile=Rect("UtilityShopProduct",content,x,y,artWidth,artHeight+118);
            var background=tile.gameObject.AddComponent<Image>();background.color=new Color(.07f,.055f,.035f,.85f);
            var art=Rect("Preview",tile,5,5,artWidth-10,artHeight-10).gameObject.AddComponent<Image>();art.sprite=offer.Sprite;art.preserveAspect=true;art.enabled=offer.Sprite!=null;art.raycastTarget=false;
            if(offer.Sprite!=null&&category=="avatar")AvatarCircleUtility.Apply(art);
            if(offer.Sprite==null)Text(tile,"Brak podglądu",6,8,artWidth-12,artHeight-16,24);
            Text(tile,Requirement(offer),4,artHeight+8,artWidth-8,104,24);
            var button=tile.gameObject.AddComponent<Button>();button.targetGraphic=background;button.transition=Selectable.Transition.ColorTint;
            button.onClick.AddListener(()=>Preview(GetComponent<Canvas>(),offer.Category,offer.Id,Build));
        }
        if(items.Count==0)Text(content,"Brak zablokowanych przedmiotów w tym filtrze.\nOdblokowane rzeczy znajdziesz w profilu.",8,20,width-16,120,30);
    }
    private static string Requirement(CosmeticCatalog.Offer o)
    {
        if(o.Category=="frame")return o.Id=="classic_wood"?"Misja: pierwsza gra":o.Title.Replace("Ramka · ","")+"\nlub wcześniej: "+Price(o);
        return Price(o)+(o.Spin&&o.Purchasable?"\nTakże ze spina":"");
    }
    private static string Price(CosmeticCatalog.Offer o)=>!o.Purchasable?(o.Id=="6"?"Darmowy":o.Category=="frame"?"Nagroda za misję":o.Spin?"Tylko ze spina":"Niedostępne"):o.Gold>0&&o.Diamonds>0?$"{o.Gold} złota\n{(o.Both?"+":"lub")} {o.Diamonds} diamentów":o.Gold>0?$"{o.Gold} złota":$"{o.Diamonds} diamentów";
    public static void Preview(Canvas canvas,string category,string id,Action changed=null)
    {
        if(canvas==null)return;var offer=CosmeticCatalog.Get(category,id);var root=Overlay(canvas,"PurchasePreview");root.GetComponent<Canvas>().sortingOrder=700;
        float w=root.rect.width,h=root.rect.height;float artH=Mathf.Min(h*.48f,600),artW=Mathf.Min(w-60,artH);
        Text(root,offer.Title,20,20,w-40,65,34);
        var art=Rect("Artwork",root,(w-artW)/2,100,artW,artH).gameObject.AddComponent<Image>();art.sprite=offer.Sprite;art.preserveAspect=true;art.enabled=offer.Sprite!=null;
        float y=110+artH;Text(root,Requirement(offer),20,y,w-40,120,30);y+=140;
        bool owned=CosmeticCatalog.Owned(category,id);bool allowed=category!="frame"||CosmeticCatalog.CanUnlockFrame(PlayerProfileService.Data,id);
        var status=Text(root,"",20,y+74,w-40,74,25);
        Action<bool> buy=gems=>{if(PlayerProfileService.BuyCosmetic(category,id,gems)){Destroy(root.gameObject);changed?.Invoke();UnlockPresentationUI.ShowPending(canvas);}else status.text="Za mało środków lub przedmiot już odblokowany.";};
        if(owned)status.text="Odblokowano — przedmiot znajdziesz w swoim profilu.";
        else if(!allowed)status.text=id=="classic_wood"?"Ukończ pierwszą grę i odbierz ramkę w Misjach.":"Odblokuj najpierw poprzednie ramki.\nRamki otrzymujesz też za poziom.";
        else if(offer.Both)Button(root,$"KUP: {offer.Gold} złota + {offer.Diamonds} diamentów",20,y,w-40,65,()=>buy(true));
        else if(offer.Purchasable)
        {
            float bw=(w-50)/2;if(offer.Gold>0)Button(root,$"KUP: {offer.Gold} złota",20,y,bw,65,()=>buy(false));
            if(offer.Diamonds>0)Button(root,$"KUP: {offer.Diamonds} diamentów",30+bw,y,bw,65,()=>buy(true));
        }
        Button(root,"WRÓĆ",20,h-84,w-40,64,()=>Destroy(root.gameObject));
    }
}
