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
        var r=Box(canvas.rootCanvas.transform,"UtilityHotSeatSettings",-48,-48,76,76);
        r.anchorMin=r.anchorMax=r.pivot=Vector2.one;
        r.anchoredPosition=new Vector2(-24,-Mathf.Max(24,(Screen.height-Screen.safeArea.yMax)*((RectTransform)canvas.rootCanvas.transform).rect.height/Screen.height));
        var layer=r.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=1700;r.gameObject.AddComponent<GraphicRaycaster>();
        r.gameObject.AddComponent<Image>().color=new Color(0,0,0,.2f);
        var icon=Box(r,"SettingsIcon",0,14,48,48);var glyph=icon.gameObject.AddComponent<GameUtilityGlyph>();glyph.settings=true;glyph.color=Color.white;glyph.raycastTarget=false;
        var button=r.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
        button.onClick.AddListener(()=>{
            if(canvas.rootCanvas.transform.Find("HotSeatOptions")!=null)return;
            var panel=Box(canvas.rootCanvas.transform,"HotSeatOptions",0,0,720,600);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);
            panel.gameObject.AddComponent<Image>().color=new Color(.02f,.05f,.035f,1);
            var modal=panel.gameObject.AddComponent<Canvas>();modal.overrideSorting=true;modal.sortingOrder=1800;panel.gameObject.AddComponent<GraphicRaycaster>();
            var bounds=((RectTransform)canvas.rootCanvas.transform).rect;panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(bounds.width/750,bounds.height/630));
            var title=Label(panel,"TEMPO ODKRYWANIA KART",680,60);title.rectTransform.anchoredPosition=new Vector2(0,490);
            float[] speeds={2.28f,1.14f,.38f};string[] names={"WOLNO","NORMALNIE","SZYBKO"};
            for(int i=0;i<3;i++){float delay=speeds[i];string name=names[i];Action(panel,name,(i-1)*225,380,210,()=>{PlayerPrefs.SetFloat("hotseat.revealDelay",delay);PlayerPrefs.Save();title.text="TEMPO: "+name;});}
            bool confirmed=false;Button exit=null;
            exit=Action(panel,"WYJDŹ DO MENU",0,220,600,()=>{if(!confirmed){confirmed=true;exit.GetComponentInChildren<TMP_Text>().text="POTWIERDŹ ZAKOŃCZENIE GRY";}else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");});
            Action(panel,"WRÓĆ DO GRY",0,85,600,()=>Object.Destroy(panel.gameObject));
        });
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
        if(!CodesVisible)return;
        var button=Action(parent,"WPROWADŹ KOD",0,35,420,()=>{});
        button.onClick.AddListener(()=>{
            button.gameObject.SetActive(false);
            var r=Box(parent,"CodeInput",-45,35,330,62);r.gameObject.AddComponent<Image>().color=new Color(.1f,.1f,.1f);
            var input=r.gameObject.AddComponent<TMP_InputField>();var label=Label(r,"",310,62);
            input.textComponent=label;input.textViewport=label.rectTransform;input.characterLimit=5;
            input.onValueChanged.AddListener(value=>input.SetTextWithoutNotify(value.ToUpperInvariant()));
            var status=Label(parent,"",700,32);status.rectTransform.anchoredPosition=new Vector2(0,103);
            var confirm=Action(parent,"",170,35,70,()=>{
                string code=input.text.Trim().ToUpperInvariant();var data=PlayerProfileService.Data;
                if(code=="KYRE"&&!data.Receipts.Contains("code:KYRE"))
                {data.Receipts.Add("code:KYRE");data.Wallet.Coins+=500;data.Wallet.RewardCurrency+=25;PlayerProfileService.Save();status.text="Dodano 500 złota i 25 diamentów";}
                else if(code=="41111") {PlayerPrefs.SetInt("test.enabled",1);PlayerPrefs.Save();status.text="Menu testowe odblokowane"; var owner=parent.GetComponentInParent<Canvas>(); if(owner!=null)Show(owner);}
                else status.text=code=="KYRE"?"Ten kod został już wykorzystany":"Nieprawidłowy kod";
            });
            foreach(var part in new[]{new Vector3(-10,24,-45),new Vector3(8,30,45)})
            {var stroke=Box(confirm.transform,"CheckStroke",part.x,part.y,part.x<0?19:34,4);stroke.localRotation=Quaternion.Euler(0,0,part.z);var ink=stroke.gameObject.AddComponent<Image>();ink.color=new Color(.2f,1,.4f);ink.raycastTarget=false;}
            input.ActivateInputField();
        });
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
        Action(root,"+1000 złota",185,720,340,()=>edit(()=>data.Wallet.Coins+=1000));
        Action(root,"−100 diamentów",-185,630,340,()=>edit(()=>data.Wallet.RewardCurrency=System.Math.Max(0,data.Wallet.RewardCurrency-100)));
        Action(root,"+100 diamentów",185,630,340,()=>edit(()=>data.Wallet.RewardCurrency+=100));
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
