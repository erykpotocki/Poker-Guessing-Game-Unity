using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpinRewardUI : MonoBehaviour
{
    private TMP_Text timer, spinCount, result;
    private Button spin;
    private RectTransform wheel;
    private RectTransform panel, rewardPreview;
    private TMP_Text headline, spinCaption;
    private Image spinBackground;
    private bool spinning;
    private RectTransform advertisement;
    private void OnDestroy(){if(advertisement!=null)Destroy(advertisement.gameObject);}
    public static void Show(Canvas owner)
    {
        if(owner==null)return;
        Transform old=owner.rootCanvas.transform.Find("SpinRewardOverlay"); if(old!=null){Close(owner);return;}
        Transform profile=owner.rootCanvas.transform.Find("PlayerProfileOverlay");
        if(profile!=null){profile.gameObject.SetActive(false);Destroy(profile.gameObject);}
        GameObject obj=new GameObject("SpinRewardOverlay",typeof(RectTransform),typeof(Image),typeof(Canvas),typeof(GraphicRaycaster),typeof(SpinRewardUI));
        obj.transform.SetParent(owner.rootCanvas.transform,false);
        RectTransform root=obj.transform as RectTransform; root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;PortraitMenuTopBar.ApplyOverlayInset(root);
        obj.GetComponent<Image>().color=new Color(0,0,0,.94f); Canvas c=obj.GetComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=680;
        Button backdrop=obj.AddComponent<Button>();backdrop.transition=Selectable.Transition.None;
        backdrop.onClick.AddListener(()=>obj.GetComponent<SpinRewardUI>().TryClose());
        // The panel Image catches clicks inside; only the surrounding backdrop dismisses.
        obj.GetComponent<SpinRewardUI>().Build();
    }
    public static void Close(Canvas owner)
    {
        if(owner==null)return;
        Transform overlay=owner.rootCanvas.transform.Find("SpinRewardOverlay");
        if(overlay!=null)
        {
            var ui=overlay.GetComponent<SpinRewardUI>();
            if(ui==null||!ui.spinning){overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
        }
    }
    private RectTransform Box(string name,Transform parent,Vector2 pos,Vector2 size)
    {
        RectTransform r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    private TMP_Text Text(Transform parent,string value,Vector2 pos,Vector2 size,float font)
    {
        TMP_Text t=Box("Label",parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.fontSize=font;
        t.alignment=TextAlignmentOptions.Center;t.color=new Color(1,.9f,.65f);t.raycastTarget=false;return t;
    }
    private Button Action(Transform parent,string label,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        RectTransform r=Box("UtilitySpinAction",parent,pos,size);Image image=r.gameObject.AddComponent<Image>();Button b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
        TMP_Text t=Text(r,label,Vector2.zero,size,30);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;
        t.color=new Color(.08f,.06f,.02f);t.fontStyle=FontStyles.Bold;
        image.color=new Color(1f,.78f,.25f);
        b.onClick.AddListener(action);return b;
    }
    private void Build()
    {
        panel=Box("UtilitySpinPanel",transform,Vector2.zero,new Vector2(820,1400));panel.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.04f,1f);
        Button panelHit=panel.gameObject.AddComponent<Button>();panelHit.transition=Selectable.Transition.None;
        Text(panel,"KOŁO NAGRÓD",new Vector2(0,630),new Vector2(590,76),40).characterSpacing=6;
        Button close=Action(panel,"",new Vector2(355,630),new Vector2(96,96),TryClose);
        close.GetComponent<Image>().color=Color.clear;close.transition=Selectable.Transition.None;
        foreach(float angle in new[]{45f,-45f})
        {
            var stroke=Box("CloseStroke",close.transform,Vector2.zero,new Vector2(32,3));
            stroke.localRotation=Quaternion.Euler(0,0,angle);
            var ink=stroke.gameObject.AddComponent<Image>();ink.color=new Color(1,1,1,.8f);ink.raycastTarget=false;
        }
        GameObject wheelObject = new GameObject("RewardWheel", typeof(RectTransform), typeof(Image)); wheelObject.transform.SetParent(panel, false);
        wheel = wheelObject.transform as RectTransform; wheel.anchorMin=wheel.anchorMax=wheel.pivot=new Vector2(.5f,.5f); wheel.anchoredPosition=new Vector2(0,230); wheel.sizeDelta=new Vector2(600,600);
        Image wheelImage=wheelObject.GetComponent<Image>(); wheelImage.sprite=Resources.Load<Sprite>("SpinAssets/spin kolo"); wheelImage.preserveAspect=true; wheelImage.raycastTarget=false;
        Text(panel,"▼",new Vector2(0,560),new Vector2(88,76),52).color=new Color(1f,.78f,.18f);
        headline=Text(panel,"TWÓJ SZCZĘŚLIWY MOMENT",new Vector2(0,-135),new Vector2(740,78),38);
        headline.fontStyle=FontStyles.Bold;
        result=Text(panel,"Zakręć i odkryj swoją nagrodę",new Vector2(0,-225),new Vector2(720,86),32);
        rewardPreview=Box("RewardPreview",panel,new Vector2(0,-70),new Vector2(176,176));rewardPreview.gameObject.SetActive(false);

        spin=Action(panel,"Zakręć spinem",new Vector2(0,-578),new Vector2(720,112),Spin);
        spinCaption=spin.GetComponentInChildren<TMP_Text>();spinBackground=spin.GetComponent<Image>();
        spinCount=Text(panel,"",new Vector2(0,-350),new Vector2(720,72),54);
        timer=Text(panel,"",new Vector2(0,-448),new Vector2(720,100),32);
        Canvas.ForceUpdateCanvases();
        var root=transform as RectTransform;
        float scale=Mathf.Min(1f,Mathf.Min((root.rect.width-24)/820f,(root.rect.height-24)/1400f));
        panel.localScale=Vector3.one*Mathf.Max(.1f,scale);
        Refresh();
    }
    private void Spin()
    {
        if(spinning)return;
        StartCoroutine(!PlayerProfileService.CanSpin && PlayerProfileService.Data.Wheel.PendingPrize==null ? WatchAd() : SpinWheel());
    }
    private void TryClose()
    {
        if(spinning)return;
        gameObject.SetActive(false);Destroy(gameObject);
    }
    private IEnumerator WatchAd()
    {
        spinning=true;
        var cover=Box("AdvertisementPlaceholder",transform,Vector2.zero,Vector2.zero);advertisement=cover;
        cover.SetParent(GetComponentInParent<Canvas>().rootCanvas.transform,false);
        var adLayer=cover.gameObject.AddComponent<Canvas>();adLayer.overrideSorting=true;adLayer.sortingOrder=1500;cover.gameObject.AddComponent<GraphicRaycaster>();
        cover.anchorMin=Vector2.zero;cover.anchorMax=Vector2.one;cover.offsetMin=cover.offsetMax=Vector2.zero;
        cover.gameObject.AddComponent<Image>().color=Color.black;
        var caption=Text(cover,"",Vector2.zero,new Vector2(780,240),42);
        float elapsed=0;
        while(elapsed<5f)
        {
            elapsed+=Time.unscaledDeltaTime;
            caption.text="REKLAMA TESTOWA\n"+Mathf.CeilToInt(5-elapsed)+" s";
            yield return null;
        }
        PlayerProfileService.GrantCompletedAdSpin();
        Destroy(cover.gameObject);spinning=false;Refresh();
    }
    private IEnumerator SpinWheel()
    {
        if (!PlayerProfileService.CanSpin && PlayerProfileService.Data.Wheel.PendingPrize==null) { Refresh(); yield break; }
        spinning=true; spin.interactable=false; result.text="Koło wybiera Twoją nagrodę…";
        result.fontSize=32;result.rectTransform.sizeDelta=new Vector2(720,86);
        rewardPreview.gameObject.SetActive(false);headline.text="POWODZENIA!";
        headline.rectTransform.anchoredPosition=new Vector2(0,-135);headline.fontSize=38;
        wheel.localScale=Vector3.one;wheel.anchoredPosition=new Vector2(0,230);

        // Persist the single result before animation: closing/reopening the
        // overlay cannot grant it twice or lose an already-won diamond.
        string reward=PlayerProfileService.Spin(out int sector,out Sprite avatar);
        if (reward == null) { spinning=false; Refresh(); yield break; }
        float start=wheel.localEulerAngles.z;
        float target=start+1440f+Mathf.Repeat(sector*45f-start,360f), elapsed=0f;
        while(elapsed<4f){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/4f);float eased=1f-Mathf.Pow(1f-t,3f);wheel.localEulerAngles=new Vector3(0,0,Mathf.Lerp(start,target,eased));yield return null;}
        headline.text="Gratulacje!";headline.fontSize=64;
        headline.rectTransform.anchoredPosition=new Vector2(0,80);
        result.text="Wygrywasz\n"+reward;result.fontSize=38;
        result.rectTransform.sizeDelta=new Vector2(720,92);

        foreach(Transform child in rewardPreview)Destroy(child.gameObject);
        var portrait=Box("Prize",rewardPreview,Vector2.zero,new Vector2(168,168));
        if(avatar!=null)
        {
            var image=portrait.gameObject.AddComponent<Image>();image.sprite=avatar;image.preserveAspect=true;image.raycastTarget=false;
            if(PlayerProfileService.Data.Wheel.PendingPrize.Category=="avatar")AvatarCircleUtility.Apply(image);
        }
        else
        {
            var image=portrait.gameObject.AddComponent<Image>();
            image.sprite=Resources.Load<Sprite>(reward.Contains("diament")?"WalletIcons/diament":"WalletIcons/złoto");
            image.preserveAspect=true;image.raycastTarget=false;
            image.enabled=image.sprite!=null;
        }
        rewardPreview.gameObject.SetActive(true);
        yield return Celebrate();
        PlayerProfileService.ClaimSpinPrize();
        // Unlock dismissal only after the persisted prize has been applied and
        // the wallet/inventory notification has refreshed the top bar.
        yield return null;
        spinning=PlayerProfileService.Data.Wheel.PendingPrize!=null;
        Refresh();
    }
    private IEnumerator Celebrate()
    {
        var glints=new RectTransform[12];
        var images=new RawImage[glints.Length];
        // These project assets are imported as textures, not Sprite assets.
        Texture2D burst=Resources.Load<Texture2D>("UI/Sparkles/SparkleBurst");
        Texture2D cluster=Resources.Load<Texture2D>("UI/Sparkles/SparkleCluster");
        for(int i=0;i<glints.Length;i++)
        {
            glints[i]=Box("PrizeSparkle",rewardPreview,Vector2.zero,new Vector2(44,44));
            var sparkle=glints[i].gameObject.AddComponent<RawImage>();
            sparkle.texture=i%3==0?cluster:burst;
            sparkle.raycastTarget=false;sparkle.color=new Color(1,1,1,0);
            sparkle.enabled=sparkle.texture!=null;images[i]=sparkle;
        }
        float elapsed=0;
        while(elapsed<1.4f)
        {
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/1.4f);
            float settle=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.5f),3);
            wheel.localScale=Vector3.one*Mathf.Lerp(1,.62f,settle);
            wheel.anchoredPosition=Vector2.Lerp(new Vector2(0,230),new Vector2(0,350),settle);
            rewardPreview.localScale=Vector3.one*(1+.12f*Mathf.Sin(t*Mathf.PI*3)*(1-t));
            for(int i=0;i<glints.Length;i++)
            {
                float angle=i*Mathf.PI*2/glints.Length;
                glints[i].anchoredPosition=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Mathf.Lerp(75,155,t);
                glints[i].localRotation=Quaternion.Euler(0,0,t*100);
                images[i].color=new Color(1,1,1,Mathf.Sin(t*Mathf.PI));
            }
            yield return null;
        }
        rewardPreview.localScale=Vector3.one;
        foreach(var glint in glints)Destroy(glint.gameObject);
    }
    private void Update(){Refresh();}
    private void Refresh()
    {
        if(timer==null||spin==null)return;TimeSpan left=PlayerProfileService.SpinRemaining;spin.interactable=!spinning;
        int charges=PlayerProfileService.SpinCharges;
        spinCount.text=$"{charges}/3 spiny";
        string countdown=$"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        timer.text=PlayerProfileService.Data.Wheel.Charges==3?"Wszystkie spiny dostępne":$"<size=28>Kolejny spin za</size>\n<size=40>{countdown}</size>";
        spinCaption.text=charges>0||PlayerProfileService.Data.Wheel.PendingPrize!=null?"Zakręć spinem":"Obejrzyj reklamę, by zakręcić już teraz!";
        spinCaption.fontSize=32;
        spinBackground.color=spin.interactable?new Color(1,.78f,.25f):new Color(.19f,.25f,.22f);
        spinCaption.color=spin.interactable?new Color(.08f,.06f,.02f):new Color(.7f,.77f,.72f);
    }
}
