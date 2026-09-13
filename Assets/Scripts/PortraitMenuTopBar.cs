using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PortraitMenuTopBar : MonoBehaviour
{
    private static readonly string[] MenuScenes = { "MainMenu", "GameModeSelect", "CreateRoom", "JoinRoom", "Lobby" };
    private static Sprite spinSprite;
    private static Sprite circleMaskSprite;
    private Canvas owner;
    private RectTransform bar;
    private TMP_Text goldText, diamondText;
    private TMP_Text calendarDay;
    private GameObject dailyDot;
    private Image goldIcon, diamondIcon;
    private Image profileImage;
    private TMP_Text levelText, experienceText;
    private RectTransform experienceTrack, experienceFill;
    private Vector2Int lastScreen;
    private Rect lastSafeArea;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallBootstrap()
    {
        if (FindFirstObjectByType<PortraitMenuTopBarBootstrap>() != null) return;
        GameObject obj = new GameObject("PortraitMenuTopBarBootstrap", typeof(PortraitMenuTopBarBootstrap));
        DontDestroyOnLoad(obj);
    }

    public static bool Supports(string sceneName)
    {
        foreach (string item in MenuScenes) if (item == sceneName) return true;
        return false;
    }

    public static void Ensure(Canvas canvas)
    {
        if (canvas == null || canvas.rootCanvas.transform.Find("PortraitMenuTopBar") != null) return;
        GameObject obj = new GameObject("PortraitMenuTopBar", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster), typeof(PortraitMenuTopBar));
        obj.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform root = obj.transform as RectTransform;
        root.anchorMin = new Vector2(0f, 1f); root.anchorMax = Vector2.one; root.pivot = new Vector2(.5f, 1f);
        root.offsetMin = root.offsetMax = Vector2.zero;
        Image background = obj.GetComponent<Image>(); background.color = Color.black; background.raycastTarget = false;
        // Keep the functional bar above menu overlays so its profile/spin icons
        // remain reachable while the shop or another menu panel is open.
        Canvas layer = obj.GetComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 700;
        PortraitMenuTopBar component = obj.GetComponent<PortraitMenuTopBar>(); component.owner = canvas.rootCanvas; component.Build();
    }

    // Menu overlays must begin below the functional bar. The bar background
    // may extend behind the status area, but its content must stay visible.
    public static void ApplyOverlayInset(RectTransform root)
    {
        if (root == null || Screen.height <= 0) return;
        RectTransform canvasRect = root.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        float scaleY = canvasRect != null ? canvasRect.rect.height / Screen.height : 1f;
        float safeTop = (Screen.height - Screen.safeArea.yMax) * scaleY;
        float panelHeight = safeTop + 130f;
        root.offsetMin = Vector2.zero;
        root.offsetMax = new Vector2(0f, -panelHeight);
    }

    private void Build()
    {
        bar = transform as RectTransform;
        goldIcon = CreateCurrencyIcon("GoldIcon", "WalletIcons/złoto");
        diamondIcon = CreateCurrencyIcon("DiamondIcon", "WalletIcons/diament");
        goldText = CreateText("Gold", new Color(1f, .72f, .16f));
        diamondText = CreateText("Diamonds", new Color(.3f, .78f, 1f));
        levelText=CreateText("Level",new Color(.75f,1f,.72f));
        experienceText=CreateText("ExperienceCount",Color.white);
        foreach(var text in new[]{levelText,experienceText}){text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=24;text.alignment=TextAlignmentOptions.Center;}
        experienceTrack=new GameObject("ExperienceTrack",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        experienceTrack.SetParent(transform,false);experienceTrack.GetComponent<Image>().color=new Color(.04f,.15f,.07f);experienceTrack.GetComponent<Image>().raycastTarget=false;
        experienceFill=new GameObject("ExperienceFill",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        experienceFill.SetParent(experienceTrack,false);experienceFill.GetComponent<Image>().color=new Color(.12f,.72f,.29f);experienceFill.GetComponent<Image>().raycastTarget=false;
        experienceText.transform.SetAsLastSibling();
        CreateIconButton("Spin", CreateSpinSprite(), ShowSpin, out _);
        CreateIconButton("DailyRewards", null, ()=>DailyRewardsUI.Show(owner), out var calendarIcon);
        calendarIcon.color=new Color(.92f,.94f,.90f);
        var calendar=transform.Find("DailyRewards");
        var header=ShopUI.Rect("CalendarHeader",calendar,0,0,68,16);header.gameObject.AddComponent<Image>().color=new Color(.8f,.18f,.16f);
        header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;header.sizeDelta=new Vector2(0,16);header.anchoredPosition=Vector2.zero;
        calendarDay=ShopUI.Text(calendar,"",0,12,68,56,36);calendarDay.color=new Color(.03f,.10f,.06f);
        calendarDay.rectTransform.anchorMin=Vector2.zero;calendarDay.rectTransform.anchorMax=Vector2.one;calendarDay.rectTransform.offsetMin=new Vector2(0,0);calendarDay.rectTransform.offsetMax=new Vector2(0,-12);
        dailyDot=ShopUI.Rect("Available",calendar,0,0,18,18).gameObject;dailyDot.AddComponent<Image>().color=Color.red;
        var dotRect=(RectTransform)dailyDot.transform;dotRect.anchorMin=dotRect.anchorMax=new Vector2(1,1);dotRect.anchoredPosition=new Vector2(-4,4);
        CreateIconButton("Profile", null, ShowProfile, out profileImage);
        PlayerProfileService.Changed += Refresh;
        Layout(); Refresh();
        if(PlayerProfileService.PeekUnlock()?.Source=="level")UnlockPresentationUI.ShowPending(owner);
    }

    private Image CreateCurrencyIcon(string name, string resourcePath)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>(resourcePath);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateText(string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(transform, false);
        TMP_Text text = obj.GetComponent<TextMeshProUGUI>(); text.fontSize = 34; text.fontStyle = FontStyles.Bold;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.color = color; text.raycastTarget = false; return text;
    }

    private void CreateIconButton(string name, Sprite sprite, UnityEngine.Events.UnityAction action, out Image icon)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); buttonObject.transform.SetParent(transform, false);
        Image buttonBackground = buttonObject.GetComponent<Image>();
        buttonBackground.color = Color.clear;
        buttonBackground.raycastTarget = true;
        Button button = buttonObject.GetComponent<Button>(); button.onClick.AddListener(action);
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image)); iconObject.transform.SetParent(buttonObject.transform, false);
        Image maskImage = iconObject.GetComponent<Image>(); maskImage.sprite = name == "Profile" ? CreateCircleMaskSprite() : sprite; maskImage.preserveAspect = true;
        RectTransform iconRect = maskImage.rectTransform; iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one; iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        if (name == "Profile")
        {
            Mask mask = iconObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            GameObject content = new GameObject("Avatar", typeof(RectTransform), typeof(Image)); content.transform.SetParent(iconObject.transform, false);
            icon = content.GetComponent<Image>(); icon.preserveAspect = false; icon.raycastTarget = false;
            content.AddComponent<CircularAvatarMesh>();
            RectTransform contentRect = icon.rectTransform; contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = Vector2.one; contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
        }
        else { icon = maskImage; icon.raycastTarget = false; }
        button.targetGraphic = buttonBackground; ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, .9f, .58f); colors.pressedColor = new Color(.72f, .72f, .72f); button.colors = colors;
    }

    // The gameplay scene has no menu bar. Retain the last visible balances so
    // rewards earned there animate when the player returns to the menu.
    private static long lastGold=-1,lastDiamonds=-1;
    private Coroutine goldAnimation,diamondAnimation;
    private readonly System.Collections.Generic.Dictionary<TMP_Text,long> displayedCurrency=new();
    private void AnimateCurrency(TMP_Text text,long amount,ref long previous,ref Coroutine animation)
    {
        if(previous==amount)
        {
            if(!displayedCurrency.ContainsKey(text)){text.text=FormatCurrency(amount);displayedCurrency[text]=amount;}
            return;
        }
        long start=previous;previous=amount;
        if(animation!=null)StopCoroutine(animation);
        var old=text.transform.Find("CurrencyGain");if(old!=null)Destroy(old.gameObject);
        if(start<0||amount<=start){text.text=FormatCurrency(amount);displayedCurrency[text]=amount;return;}
        long displayed=displayedCurrency.TryGetValue(text,out long currentValue)?currentValue:start;
        animation=StartCoroutine(CurrencyGain(text,displayed,amount,amount-start));
    }
    private System.Collections.IEnumerator CurrencyGain(TMP_Text target,long start,long end,long gain)
    {
        var go=new GameObject("CurrencyGain",typeof(RectTransform),typeof(TextMeshProUGUI));
        go.transform.SetParent(target.transform,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(180,64);
        var label=go.GetComponent<TMP_Text>();label.text="+"+gain;label.fontSize=32;label.color=target.color;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        float elapsed=0,duration=Mathf.Clamp((end-start)*.045f,.35f,1.5f);
        while(elapsed<duration+.7f)
        {
            elapsed+=Time.unscaledDeltaTime;
            displayedCurrency[target]=start+(long)System.Math.Round((end-start)*Mathf.Clamp01(elapsed/duration));
            target.text=FormatCurrency(displayedCurrency[target]);
            Layout();
            rect.anchoredPosition=new Vector2(0,-8-18*elapsed);
            var color=target.color;color.a=1-Mathf.Clamp01((elapsed-duration)/.7f);label.color=color;
            yield return null;
        }
        target.text=FormatCurrency(end);Destroy(go);
    }
    private void Refresh()
    {
        if (goldText == null) return;
        var data = PlayerProfileService.Data;
        AnimateCurrency(goldText,data.Wallet.Coins,ref lastGold,ref goldAnimation);
        AnimateCurrency(diamondText,data.Wallet.RewardCurrency,ref lastDiamonds,ref diamondAnimation);
        levelText.text="LVL "+data.Progression.Level;
        experienceText.text=$"{data.Progression.CurrentExperience}/{data.Progression.RequiredExperience} EXP";
        float fraction=(float)((double)data.Progression.CurrentExperience/data.Progression.RequiredExperience);
        experienceFill.anchorMin=Vector2.zero;experienceFill.anchorMax=new Vector2(Mathf.Clamp01(fraction),1);
        experienceFill.offsetMin=experienceFill.offsetMax=Vector2.zero;
        AvatarDatabase avatars = Resources.Load<AvatarDatabase>("ProfileAvatars");
        if (profileImage != null && avatars != null && avatars.avatars != null && avatars.avatars.Length > 0)
            profileImage.sprite = avatars.avatars[Mathf.Clamp(PlayerProfileService.AvatarIndex, 0, avatars.avatars.Length - 1)];
        Layout();
    }

    public static string FormatCurrency(long amount)
    {
        if(amount<10000)return amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        decimal divisor=amount>=1000000000?1000000000m:amount>=1000000?1000000m:1000m;
        string suffix=amount>=1000000000?" mld":amount>=1000000?" mln":"k";
        decimal compact=decimal.Floor(amount/divisor*10)/10;
        return compact.ToString("0.#",System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))+suffix;
    }

    private void Update()
    {
        if(calendarDay!=null)calendarDay.text=PlayerProfileService.RewardClock.UtcNow.Day.ToString();
        if(dailyDot!=null)dailyDot.SetActive(PokerProfile.RewardRules.CanClaimDaily(PlayerProfileService.Data,PlayerProfileService.RewardClock.UtcNow));
        if (lastScreen != new Vector2Int(Screen.width, Screen.height) || lastSafeArea != Screen.safeArea) Layout();
        if(owner!=null && PlayerProfileService.PeekUnlock()?.Source=="level" &&
            owner.rootCanvas.transform.Find("SpinRewardOverlay")==null)
            UnlockPresentationUI.ShowPending(owner);
    }

    private void Layout()
    {
        if (owner == null || bar == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastScreen = new Vector2Int(Screen.width, Screen.height); lastSafeArea = Screen.safeArea;
        RectTransform canvasRect = owner.transform as RectTransform;
        float sx = canvasRect.rect.width / Screen.width, sy = canvasRect.rect.height / Screen.height;
        float safeTop = (Screen.height - Screen.safeArea.yMax) * sy;
        float safeLeft = Screen.safeArea.xMin * sx, safeRight = (Screen.width - Screen.safeArea.xMax) * sx;
        float height = safeTop + 130f; bar.sizeDelta = new Vector2(0f, height);
        RectTransform spin = transform.Find("Spin") as RectTransform, profile = transform.Find("Profile") as RectTransform;
        float unit=(canvasRect.rect.width-safeLeft-safeRight)/1000f;
        const float growth=94f/72f;
        float contentUnit=unit*growth;
        Place(profile,Vector2.one,Vector2.one,new Vector2(-safeRight-65*unit,-safeTop-65),new Vector2(94,94)*unit);
        Place(spin,Vector2.one,Vector2.one,new Vector2(-safeRight-178*unit,-safeTop-65),new Vector2(68,68)*contentUnit);
        Place(transform.Find("DailyRewards") as RectTransform,Vector2.one,Vector2.one,new Vector2(-safeRight-285*unit,-safeTop-65),new Vector2(68,68)*unit);
        goldText.fontSize=diamondText.fontSize=34*contentUnit;
        goldText.enableAutoSizing=diamondText.enableAutoSizing=true;
        goldText.fontSizeMin=diamondText.fontSizeMin=22*contentUnit;
        goldText.fontSizeMax=diamondText.fontSizeMax=34*contentUnit;
        float goldWidth=Mathf.Clamp(goldText.GetPreferredValues(goldText.text,10000,100).x+4*unit,36*contentUnit,100*contentUnit);
        float diamondWidth=Mathf.Clamp(diamondText.GetPreferredValues(diamondText.text,10000,100).x+4*unit,36*contentUnit,100*contentUnit);
        float goldX=safeLeft+30*unit;
        float goldValueX=goldX+35*contentUnit;
        float diamondX=goldValueX+goldWidth+24*contentUnit;
        float diamondValueX=diamondX+35*contentUnit;
        Place(goldIcon.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(goldX,-safeTop-65),Vector2.one*42*contentUnit);
        PlaceLeft(goldText.rectTransform,new Vector2(goldValueX,-safeTop-65),new Vector2(goldWidth,70*growth));
        Place(diamondIcon.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(diamondX,-safeTop-65),Vector2.one*42*contentUnit);
        PlaceLeft(diamondText.rectTransform,new Vector2(diamondValueX,-safeTop-65),new Vector2(diamondWidth,70*growth));
        float xpLeft=safeLeft+490*unit;
        float xpRight=canvasRect.rect.width-safeRight-335*unit;
        float xpWidth=Mathf.Max(120*unit,xpRight-xpLeft-28*unit);
        xpLeft+=14*contentUnit;
        foreach(var text in new[]{levelText,experienceText}){text.fontSizeMin=14*contentUnit;text.fontSizeMax=26*contentUnit;}
        PlaceLeft(levelText.rectTransform,new Vector2(xpLeft,-safeTop-65+16*growth),new Vector2(xpWidth,26*growth));
        PlaceLeft(experienceTrack,new Vector2(xpLeft,-safeTop-65-12*growth),new Vector2(xpWidth,24*growth));
        PlaceLeft(experienceText.rectTransform,new Vector2(xpLeft,-safeTop-65-12*growth),new Vector2(xpWidth,26*growth));
    }

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        if (rect == null) return; rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
    }

    private static void PlaceLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void ShowProfile()
    {
        if(owner==null)return;
        var old=owner.transform.Find("ProfileDropdown");if(old!=null){Destroy(old.gameObject);return;}
        var overlay=ShopUI.Overlay(owner,"ProfileDropdown");overlay.GetComponent<Canvas>().sortingOrder=740;
        overlay.GetComponent<Image>().color=new Color(0,0,0,.35f);
        var dismiss=overlay.gameObject.AddComponent<Button>();dismiss.transition=Selectable.Transition.None;dismiss.onClick.AddListener(()=>Destroy(overlay.gameObject));
        float width=Mathf.Min(460,overlay.rect.width-40);
        var panel=ShopUI.Rect("UtilityProfileDropdownCard",overlay,0,0,width,468);
        panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,1);panel.anchoredPosition=new Vector2(-20,-12);
        panel.gameObject.AddComponent<Image>().color=new Color(.02f,.065f,.05f,1);
        System.Action<System.Action> open=action=>{Destroy(overlay.gameObject);action();};
        ShopUI.Button(panel,"MÓJ PROFIL",18,18,width-36,72,()=>open(()=>PlayerProfileUI.Show(owner)));
        ShopUI.Button(panel,"MISJE",18,106,width-36,72,()=>open(()=>MissionsUI.Show(owner)));
        ShopUI.Button(panel,"PRZYGODA",18,194,width-36,72,()=>open(()=>
        {
            var menu=FindFirstObjectByType<MainMenuUI>();
            if(menu!=null)menu.ShowAdventure();
            else
            {
                var info=ShopUI.Overlay(owner,"AdventurePreview");
                ShopUI.Text(info,"PRZYGODA — WKRÓTCE",30,80,info.rect.width-60,100,40);
                ShopUI.Button(info,"WRÓĆ",30,220,info.rect.width-60,70,()=>Destroy(info.gameObject));
            }
        }));
        ShopUI.Button(panel,"USTAWIENIA",18,282,width-36,72,()=>open(()=>GameUtilityBar.ShowSettings(owner)));
        ShopUI.Button(panel,"KODY PROMOCYJNE",18,370,width-36,72,()=>open(()=>ProfileTestTools.ShowPromoCode(owner)));
    }
    private void ShowSpin() { if (owner != null) SpinRewardUI.Show(owner); }
    private void OnDestroy() { PlayerProfileService.Changed -= Refresh; }

    private static Sprite CreateSpinSprite()
    {
        if (spinSprite != null) return spinSprite;
        const int size = 96; Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false); texture.name = "DefaultSpinIcon";
        Color[] colors = { new Color(1f,.24f,.18f), new Color(1f,.73f,.12f), new Color(.24f,.82f,.5f), new Color(.25f,.62f,1f), new Color(.72f,.3f,1f), new Color(1f,.3f,.68f) };
        Vector2 center = new Vector2((size-1)*.5f, (size-1)*.5f);
        for (int y=0;y<size;y++) for (int x=0;x<size;x++)
        {
            Vector2 delta = new Vector2(x,y)-center; float radius=delta.magnitude;
            if(radius>45f){texture.SetPixel(x,y,Color.clear);continue;}
            if(radius<10f){texture.SetPixel(x,y,new Color(1f,.88f,.38f));continue;}
            float angle=Mathf.Atan2(delta.y,delta.x)+Mathf.PI; int segment=Mathf.FloorToInt(angle/(Mathf.PI*2f)*colors.Length)%colors.Length;
            texture.SetPixel(x,y,radius>40f?new Color(1f,.82f,.25f):colors[segment]);
        }
        texture.Apply(); spinSprite = Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f); spinSprite.name="DefaultSpinIcon"; return spinSprite;
    }

    private static Sprite CreateCircleMaskSprite()
    {
        if (circleMaskSprite != null) return circleMaskSprite;
        const int size = 128; Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * .5f, (size - 1) * .5f); float radius = size * .48f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), center) + 1f);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        texture.Apply(); circleMaskSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        return circleMaskSprite;
    }
}

public sealed class PortraitMenuTopBarBootstrap : MonoBehaviour
{
    private void Awake() { SceneManager.sceneLoaded += Loaded; StartCoroutine(Install(SceneManager.GetActiveScene())); }
    private void OnDestroy() { SceneManager.sceneLoaded -= Loaded; }
    private void Loaded(Scene scene, LoadSceneMode mode) { StartCoroutine(Install(scene)); }
    private IEnumerator Install(Scene scene)
    {
        if (!PortraitMenuTopBar.Supports(scene.name)) yield break;
        yield return null; yield return null;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas != null && canvas.isRootCanvas && canvas.gameObject.scene == scene && canvas.renderMode != RenderMode.WorldSpace)
            { PortraitMenuTopBar.Ensure(canvas); yield break; }
        }
    }
}
