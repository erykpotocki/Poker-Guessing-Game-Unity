using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameUtilityBar : MonoBehaviour
{
    private RectTransform bar;
    private RectTransform controls;
    private RectTransform status;
    private TurnDebugUI turnStatus;
    private GameUtilityGlyph soundIcon;
    private Canvas canvas;
    public void Initialize(Canvas owner, Button exit)
    {
        canvas = owner.rootCanvas;
        AuthorWatermark.Ensure(canvas);
        bar = new GameObject("UtilityBar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        bar.SetParent(canvas.transform, false);
        bar.SetAsLastSibling();
        bar.GetComponent<Image>().color = new Color(0.015f, 0.015f, 0.015f, 0.94f);
        controls = new GameObject("UtilityControls", typeof(RectTransform)).GetComponent<RectTransform>();
        controls.SetParent(bar, false);
        controls.SetAsLastSibling();
        status = new GameObject("TurnStatus", typeof(RectTransform)).GetComponent<RectTransform>();
        status.SetParent(bar, false);
        turnStatus = FindFirstObjectByType<TurnDebugUI>();
        if (turnStatus != null) turnStatus.AttachToToolbar(status);
        if (exit != null)
        {
            exit.name = "UtilityExit";
            exit.transform.SetParent(controls, false);
            foreach (LayoutGroup group in exit.GetComponentsInChildren<LayoutGroup>(true)) group.enabled = false;
            TMP_Text label = exit.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.transform.SetParent(exit.transform, false); StyleLabel(label, "Wyjdź", 27f); }
            foreach (Image image in exit.GetComponentsInChildren<Image>(true))
                if (image.gameObject != exit.gameObject) image.enabled = false;
            StyleButton(exit);
            Position(exit.transform as RectTransform, 12f, 126f);
        }
        Button settings = ButtonAt("UtilitySettings", controls, 148f, 72f, () => ShowSettings(canvas));
        AddIcon(settings, true);
        Button sound = ButtonAt("UtilitySound", controls, 240f, 72f, GameAudioSettings.ToggleMute);
        soundIcon = AddIcon(sound, false);
        GameAudioSettings.Changed += RefreshSound;
        RefreshSound();
    }
    public void SetBounds(float top, float right)
    {
        if (bar == null) return;
        // Full-bleed background; only the controls respect the phone's safe area.
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, top + 68f);
        bar.anchoredPosition = Vector2.zero;
        controls.anchorMin = controls.anchorMax = Vector2.one;
        controls.pivot = Vector2.one;
        controls.sizeDelta = new Vector2(324f, 68f);
        float safeRight = Mathf.Max(12f, right);
        float safeTop = Mathf.Max(12f, top);
        controls.anchoredPosition = new Vector2(-safeRight, -safeTop);
        RectTransform root = canvas.transform as RectTransform;
        float left = Screen.width > 0 ? Screen.safeArea.xMin * root.rect.width / Screen.width + 26f : 26f;
        status.anchorMin = new Vector2(0f, 1f);
        status.anchorMax = Vector2.one;
        status.pivot = new Vector2(0f, 1f);
        status.offsetMin = new Vector2(left, -safeTop - 68f);
        status.offsetMax = new Vector2(-safeRight - 324f - 32f, -safeTop);
        if (turnStatus != null) turnStatus.LayoutStatusLabels();
    }
    private void RefreshSound()
    {
        if (soundIcon == null) return;
        soundIcon.muted = GameAudioSettings.Muted;
        soundIcon.color = GameAudioSettings.Muted ? new Color(0.8f, 0.45f, 0.35f) : new Color(0.85f, 0.81f, 0.7f);
        soundIcon.SetVerticesDirty();
    }
    private void OnDestroy()
    {
        GameAudioSettings.Changed -= RefreshSound;
        if (bar != null) Destroy(bar.gameObject);
    }
    private static void Position(RectTransform rect, float x, float width)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, 56f);
    }
    private static void StyleButton(Button button)
    {
        ButtonAudioFeedback.Ensure(button);
        Image image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.material = null;
        image.enabled = true;
        image.color = new Color(0.18f, 0.045f, 0.025f, 0.96f);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        foreach (Shadow shadow in button.GetComponents<Shadow>()) shadow.enabled = false;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.86f, 0.64f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        button.colors = colors;
    }
    private static Button ButtonAt(string name, Transform parent, float x, float width, UnityEngine.Events.UnityAction action)
    {
        Button button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(parent, false);
        Position(button.transform as RectTransform, x, width);
        StyleButton(button);
        button.onClick.AddListener(action);
        return button;
    }
    private static GameUtilityGlyph AddIcon(Button button, bool settings)
    {
        GameUtilityGlyph icon = new GameObject(settings ? "AudioSettingsIcon" : "MuteIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(GameUtilityGlyph)).GetComponent<GameUtilityGlyph>();
        icon.transform.SetParent(button.transform, false);
        icon.rectTransform.sizeDelta = new Vector2(34f, 34f);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        icon.rectTransform.anchoredPosition = Vector2.zero;
        icon.settings = settings;
        icon.color = new Color(0.85f, 0.81f, 0.7f);
        icon.raycastTarget = false;
        icon.SetAllDirty();
        return icon;
    }
    private static void StyleLabel(TMP_Text text, string caption, float size)
    {
        text.text = caption;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Normal;
        text.color = new Color(0.85f, 0.81f, 0.7f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.margin = Vector4.zero;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        text.raycastTarget = false;
    }
    private static TMP_Text Label(Transform parent, string caption, float y)
    {
        TMP_Text text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        StyleLabel(text, caption, 30f);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(510f, 48f);
        rect.anchoredPosition = new Vector2(0f, y);
        return text;
    }
    public static void ShowSettings(Canvas canvas)
    {
        Transform root=canvas.rootCanvas.transform;
        if(root.Find("AudioSettingsOverlay")!=null)return;
        var overlay=ShopUI.Overlay(canvas,"AudioSettingsOverlay");overlay.GetComponent<Image>().color=new Color(0,0,0,.88f);
        overlay.GetComponent<Canvas>().sortingOrder=1800;
        bool gameplay=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Game";
        var box=ShopUI.Rect("UtilitySettingsCard",overlay,0,0,680,gameplay?1200:1000);
        box.anchorMin=box.anchorMax=box.pivot=new Vector2(.5f,.5f);box.anchoredPosition=Vector2.zero;
        box.gameObject.AddComponent<Image>().color=new Color(.018f,.065f,.05f,1);
        var outline=box.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.7f,.5f,.2f);outline.effectDistance=new Vector2(2,-2);
        ShopUI.Text(box,"USTAWIENIA",40,26,500,60,38).alignment=TextAlignmentOptions.Left;
        ShopUI.Button(box,"×",590,26,56,56,()=>{PlayerPrefs.Save();Destroy(overlay.gameObject);});
        var ticker=ShopUI.Rect("NowPlaying",box,40,108,410,58);ticker.gameObject.AddComponent<Image>().color=new Color(.04f,.12f,.095f);
        ticker.gameObject.AddComponent<RectMask2D>();
        var next=ShopUI.Button(box,"NASTĘPNY",468,108,172,58,CasinoAudio.NextTrack);next.name="UtilityNextTrack";next.interactable=CasinoAudio.CanSkip;
        var track=ShopUI.Text(ticker,"",0,0,600,58,26);track.enableAutoSizing=false;track.textWrappingMode=TextWrappingModes.NoWrap;track.overflowMode=TextOverflowModes.Overflow;track.alignment=TextAlignmentOptions.MidlineLeft;
        AddSlider(box,"Muzyka",-200,GameAudioSettings.Music,GameAudioSettings.SetMusic);
        var musicMute=SettingsAction(box,"MusicMute","",0,-350,GameAudioSettings.ToggleMusicMute);
        AddSlider(box,"Efekty dźwiękowe",-445,GameAudioSettings.Effects,GameAudioSettings.SetEffects);
        var effectsMute=SettingsAction(box,"EffectsMute","",0,-595,GameAudioSettings.ToggleEffectsMute);
        var haptics=SettingsAction(box,"Haptics","",0,-690,GameAudioSettings.ToggleHaptics);
        var mute=SettingsAction(box,"MasterMute","",0,-785,GameAudioSettings.ToggleMute);
        foreach(var button in new[]{musicMute,effectsMute,haptics,mute})((RectTransform)button.transform).sizeDelta=new Vector2(580,64);
        var menu=FindFirstObjectByType<MainMenuUI>();
        if(menu!=null)SettingsAction(box,"Rules","ZASADY GRY",0,-880,()=>{Destroy(overlay.gameObject);menu.ShowRules();});
        if(gameplay)
        {
            AddSlider(box,"Wielkość przycisków",-900,PlayerPrefs.GetFloat("ui.handButtonScale",1f),v=>PlayerPrefs.SetFloat("ui.handButtonScale",v),.8f,1.6f);
            SettingsAction(box,"ChatSettings","USTAWIENIA CZATU",0,-1050,()=>{Destroy(overlay.gameObject);RoundLogUI.ShowOptions(canvas);});
        }
        if(ProfileTestTools.Enabled)ProfileTestTools.AddSettingsButton(box,canvas);
        var state=overlay.gameObject.AddComponent<AudioSettingsPanelState>();
        state.Initialize(box,track,musicMute.GetComponentInChildren<TMP_Text>(),effectsMute.GetComponentInChildren<TMP_Text>(),haptics.GetComponentInChildren<TMP_Text>(),mute.GetComponentInChildren<TMP_Text>());
    }    private static Button SettingsAction(RectTransform box, string name, string caption, float x, float y, UnityEngine.Events.UnityAction action)
    {
        Button button = ButtonAt("Utility" + name, box, 0f, 240f, action);
        RectTransform rect = button.transform as RectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        TMP_Text label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(rect, false);
        StyleLabel(label, caption, 25f);
        return button;
    }
    private static void AddSlider(RectTransform box, string caption, float y, float value, UnityEngine.Events.UnityAction<float> onChange,float minimum=0,float maximum=1)
    {
        TMP_Text label = Label(box, caption + "  " + Mathf.RoundToInt(value * 100f) + "%", y);
        Slider slider = new GameObject("Volume", typeof(RectTransform), typeof(Image), typeof(Slider)).GetComponent<Slider>();
        RectTransform rect = slider.transform as RectTransform;
        rect.SetParent(box, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(500f, 50f);
        rect.anchoredPosition = new Vector2(0f, y - 50f);
        slider.GetComponent<Image>().color = new Color(0.22f, 0.19f, 0.13f);
        Image handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        handle.transform.SetParent(rect, false);
        handle.rectTransform.sizeDelta = new Vector2(28f, 8f);
        handle.color = new Color(0.9f, 0.7f, 0.32f);
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue=minimum;slider.maxValue=maximum;slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => { onChange(v); label.text = caption + "  " + Mathf.RoundToInt(v * 100f) + "%"; });
    }
}
