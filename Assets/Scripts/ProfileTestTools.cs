using PokerProfile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ProfileTestTools
{
    public static bool CodesVisible=>PlayerPrefs.GetInt("test.codes",0)==1;
    public static bool Enabled=>PlayerPrefs.GetInt("test.enabled",0)==1;
    public static float RevealDelay=>PlayerPrefs.GetFloat("hotseat.revealDelay",1.14f);
    public static void InstallHotSeatSettings(Canvas canvas)
    {
        if(canvas==null)return;
        canvas=canvas.rootCanvas;
        if(canvas.transform.Find("UtilityHotSeatSettings")!=null)return;
        var r=ShopUI.Rect("UtilityHotSeatSettings",canvas.transform,0,0,170,64);
        r.anchorMin=r.anchorMax=r.pivot=Vector2.one;
        float scaleY=((RectTransform)canvas.transform).rect.height/Mathf.Max(1,Screen.height);
        r.anchoredPosition=new Vector2(-24,-((Screen.height-Screen.safeArea.yMax)*scaleY+16));
        var layer=r.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=4000;r.gameObject.AddComponent<GraphicRaycaster>();
        ShopUI.Button(r,"MENU",0,0,170,64,()=>ShowHotSeatMenu(canvas));
    }
    private static void ShowHotSeatMenu(Canvas canvas)
    {
        if(canvas.transform.Find("HotSeatOptions")!=null)return;
        var overlay=ShopUI.Overlay(canvas,"HotSeatOptions");overlay.GetComponent<Canvas>().sortingOrder=4100;overlay.GetComponent<Image>().color=new Color(0,0,0,.88f);
        var panel=ShopUI.Rect("UtilityHotSeatCard",overlay,0,0,720,720);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        panel.gameObject.AddComponent<Image>().color=new Color(.02f,.065f,.05f);
        panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min((overlay.rect.width-40)/720,(overlay.rect.height-40)/720));
        ShopUI.Text(panel,"MENU GRY",30,30,660,64,38);
        var title=ShopUI.Text(panel,"TEMPO ODKRYWANIA KART",30,120,660,55,27);
        float[] speeds={2.28f,1.14f,.38f};string[] names={"WOLNO","NORMALNIE","SZYBKO"};
        for(int i=0;i<3;i++){float delay=speeds[i];string name=names[i];ShopUI.Button(panel,name,30+i*225,192,210,66,()=>{PlayerPrefs.SetFloat("hotseat.revealDelay",delay);PlayerPrefs.Save();title.text="TEMPO: "+name;});}
        ShopUI.Button(panel,"DŹWIĘK / NASTĘPNY UTWÓR",60,300,600,76,()=>{Object.Destroy(overlay.gameObject);GameUtilityBar.ShowSettings(canvas);});
        ShopUI.Button(panel,"ZAKOŃCZ GRĘ",60,410,600,76,()=>
        {
            foreach(Transform child in panel){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}
            ShopUI.Text(panel,"ZAKOŃCZYĆ GRĘ?",40,125,640,80,40);
            ShopUI.Text(panel,"Czy na pewno chcesz zakończyć grę?\nBieżąca rozgrywka zostanie przerwana.",50,235,620,130,30);
            ShopUI.Button(panel,"TAK, ZAKOŃCZ",60,420,600,80,()=>{Object.Destroy(overlay.gameObject);UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");});
            ShopUI.Button(panel,"NIE, WRÓĆ DO GRY",60,530,600,80,()=>Object.Destroy(overlay.gameObject));
        });
        ShopUI.Button(panel,"WRÓĆ DO GRY",60,550,600,76,()=>Object.Destroy(overlay.gameObject));
    }
    private static int taps;
    public static void ShowResumePrompt(Canvas canvas,AutoResumeRoom resume)
    {
        if(canvas.rootCanvas.transform.Find("ResumePrompt")!=null)return;
        var root=ShopUI.Overlay(canvas,"ResumePrompt");
        root.GetComponent<Image>().color=new Color(0,0,0,.9f);
        root.GetComponent<Canvas>().sortingOrder=1900;
        var panel=ShopUI.Rect("ResumeCard",root,0,0,820,650);
        panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min((root.rect.width-40)/820,(root.rect.height-40)/650));
        panel.gameObject.AddComponent<Image>().color=new Color(.025f,.06f,.048f,1);
        var accent=ShopUI.Rect("Accent",panel,48,40,724,3).gameObject.AddComponent<Image>();
        accent.color=new Color(.85f,.64f,.24f);accent.raycastTarget=false;
        var title=ShopUI.Text(panel,"WRÓĆ DO GRY",40,70,740,80,48);title.fontStyle=FontStyles.Bold;
        var status=ShopUI.Text(panel,"Połączenie z rozpoczętym meczem zostało przerwane.\nMożesz jeszcze wrócić do stołu.",60,170,700,130,32);
        ShopUI.Text(panel,"Powrót jest dostępny przez 5 minut.\nJeśli mecz się zakończył, zamkniemy to okno.",60,310,700,80,25).color=new Color(.7f,.77f,.72f);
        var join=ShopUI.Button(panel,"DOŁĄCZ PONOWNIE",60,420,700,82,resume.ResumeSavedRoom);
        var dismiss=ShopUI.Button(panel,"ZOSTAŃ W MENU",60,522,700,76,resume.Dismiss);
        resume.Expired=()=>{resume.Status=null;resume.Expired=null;if(root!=null){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}};
        resume.Status=value=>{if(status!=null)status.text=value;if(join!=null)join.interactable=!resume.Busy;if(dismiss!=null)dismiss.interactable=!resume.Busy;};
    }
    public static void InstallLogo(Transform root)
    {
        foreach(var image in root.GetComponentsInChildren<Image>(true))
            if(image.name=="Logo")
            {
                image.raycastTarget=true;
                var button=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();
                button.transition=Selectable.Transition.None;
                button.onClick.AddListener(()=>{if(++taps>=10){PlayerPrefs.SetInt("test.codes",1);PlayerPrefs.Save();}});
                break;
            }
    }
    private static RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
    }
    private static TMP_Text Label(Transform parent,string text,float w,float h)
    {
        var t=Box(parent,"Label",0,0,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.text=text;t.fontSize=27;
        t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;return t;
    }
    private static Button Action(Transform parent,string text,float x,float y,float w,System.Action action)
    {
        var r=Box(parent,"UtilityTestButton",x,y,w,62);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.13f,.2f,.16f);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;Label(r,text,w,62);b.onClick.AddListener(()=>action());return b;
    }
    public static void AddCodeEntry(RectTransform parent)
    {
        var canvas=parent.GetComponentInParent<Canvas>();
        Action(parent,"KODY PROMOCYJNE",0,35,420,()=>ShowPromoCode(canvas));
    }
    public static void ShowPromoCode(Canvas canvas)
    {
        if(canvas==null)return;canvas=canvas.rootCanvas;
        var old=canvas.transform.Find("PromoCodeOverlay");if(old!=null){Object.Destroy(old.gameObject);return;}
        var overlay=ShopUI.Overlay(canvas,"PromoCodeOverlay");overlay.GetComponent<Canvas>().sortingOrder=2300;
        overlay.GetComponent<Image>().color=new Color(0,0,0,.8f);
        var panel=ShopUI.Rect("UtilityPromoCodeCard",overlay,0,0,760,390);
        panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=new Vector2(0,90);
        panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min((overlay.rect.width-36)/760,(overlay.rect.height-36)/390));
        panel.gameObject.AddComponent<Image>().color=new Color(.015f,.055f,.043f,1);
        var outline=panel.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.72f,.5f,.16f);outline.effectDistance=new Vector2(2,-2);
        ShopUI.Text(panel,"KOD PROMOCYJNY",40,28,580,62,36).alignment=TextAlignmentOptions.Left;
        ShopUI.Button(panel,"×",660,24,64,58,()=>Object.Destroy(overlay.gameObject));
        var inputRect=ShopUI.Rect("PromoCodeInput",panel,55,120,500,78);inputRect.gameObject.AddComponent<Image>().color=new Color(.08f,.1f,.09f);
        var input=inputRect.gameObject.AddComponent<TMP_InputField>();input.characterLimit=5;input.lineType=TMP_InputField.LineType.SingleLine;input.richText=false;
        var label=ShopUI.Text(inputRect,"",16,2,468,74,36);label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=false;
        input.textComponent=label;input.textViewport=inputRect;
        input.onValueChanged.AddListener(value=>{string upper=value.ToUpperInvariant();if(value!=upper)input.SetTextWithoutNotify(upper);});
        var status=ShopUI.Text(panel,"Wpisz maksymalnie 5 znaków",50,218,660,46,24);status.color=new Color(.7f,.78f,.72f);
        System.Action redeem=()=>
        {
            string code=input.text.Trim().ToUpperInvariant();var data=PlayerProfileService.Data;
            if(code=="41111")
            {PlayerPrefs.SetInt("test.enabled",1);PlayerPrefs.Save();status.text="Menu testowe odblokowane. Znajdziesz je w Ustawieniach.";status.color=new Color(.35f,1f,.55f);}
            else if(code=="KYRE"&&!data.Receipts.Contains("code:KYRE"))
            {data.Receipts.Add("code:KYRE");RewardRules.Gold(data,500);RewardRules.Diamonds(data,25);PlayerProfileService.Save();status.text="Odebrano 500 złota i 25 diamentów";status.color=new Color(.35f,1f,.55f);}
            else {status.text=code=="KYRE"?"Ten kod został już wykorzystany":"Nieprawidłowy kod";status.color=new Color(1f,.48f,.38f);}
        };
        ShopUI.Button(panel,"ZATWIERDŹ",570,120,140,78,()=>redeem());
        input.onSubmit.AddListener(_=>redeem());input.Select();input.ActivateInputField();
    }
    public static void AddSettingsButton(RectTransform parent,Canvas canvas)
    {if(Enabled)Action(parent,"MENU TESTOWE",0,18,330,()=>Show(canvas));}
    public static void Show(Canvas canvas)
    {
        if(!Enabled)return;
        var root=Box(canvas.rootCanvas.transform,"TestMenu",0,0,800,950);
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
        var layer=root.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=2000;root.gameObject.AddComponent<GraphicRaycaster>();
        root.gameObject.AddComponent<Image>().color=new Color(.015f,.03f,.025f,1);
        var bounds=((RectTransform)canvas.rootCanvas.transform).rect;root.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(bounds.width/820,bounds.height/970));
        var status=Label(root,"",760,100);status.rectTransform.anchoredPosition=new Vector2(0,810);
        System.Action refresh=()=>{var d=PlayerProfileService.Data;status.text=$"TESTY LOKALNE\n{d.Wallet.Coins} złota | {d.Wallet.RewardCurrency} diamentów | LVL {d.Progression.Level}";};
        System.Action<System.Action> edit=change=>{change();PlayerProfileService.Save();refresh();};
        var data=PlayerProfileService.Data;
        Action(root,"−1000 złota",-185,720,340,()=>edit(()=>data.Wallet.Coins=System.Math.Max(0,data.Wallet.Coins-1000)));
        Action(root,"+1000 złota",185,720,340,()=>edit(()=>RewardRules.Gold(data,1000)));
        Action(root,"−100 diamentów",-185,630,340,()=>edit(()=>data.Wallet.RewardCurrency=System.Math.Max(0,data.Wallet.RewardCurrency-100)));
        Action(root,"+100 diamentów",185,630,340,()=>edit(()=>RewardRules.Diamonds(data,100)));
        int[] steps={1,10,100};for(int i=0;i<3;i++){int step=steps[i];Action(root,"+"+step+" LVL",(i-1)*245,540,225,()=>edit(()=>data.Progression.Experience=Progression.Threshold(data.Progression.Level+step)));}
        Action(root,"+1 wygrana",-185,450,340,()=>edit(()=>data.Statistics.GamesWon++));
        Action(root,"+1 ukończona gra",185,450,340,()=>edit(()=>data.Statistics.GamesPlayed++));
        Action(root,"ODBLOKUJ KOSMETYKI",0,350,700,()=>edit(()=>{
            var db=Resources.Load<AvatarDatabase>("ProfileAvatars");for(int i=0;i<db.avatars.Length;i++)ProgressionRules.Own(data.Inventory.OwnedAvatars,PlayerProfileService.AvatarId(i,db.avatars[i]));
            foreach(int level in LevelFrameCatalog.Levels)ProgressionRules.Own(data.Inventory.OwnedFrames,"level:"+level);
            foreach(var back in Resources.LoadAll<Texture2D>("CardBacks"))ProgressionRules.Own(data.Inventory.OwnedCardBacks,back.name);
            foreach(var back in CardBackDatabase.OnlineSprites)ProgressionRules.Own(data.Inventory.OwnedCardBacks,back.name);
        }));
        Action(root,"UZUPEŁNIJ 3 SPINY",0,260,700,()=>edit(()=>{data.Wheel.ChargeVersion=1;data.Wheel.Charges=3;data.Wheel.NextFreeUtcTicks=0;}));
        Action(root,"ZAMKNIJ",0,65,400,()=>Object.Destroy(root.gameObject));refresh();
    }
}
