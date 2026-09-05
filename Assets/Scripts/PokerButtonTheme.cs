using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies one scalable, touch-friendly visual language to horizontal UI buttons.
/// The background is generated at runtime, so it stays crisp without stretching
/// the old ornamental artwork.
/// </summary>
public sealed class PokerButtonTheme : MonoBehaviour
{
    private const float MinimumTouchWidth = 116f;
    private const float MinimumTouchHeight = 116f;
    private const float MenuLabelCanvasSize = 40f;
    private static readonly Color LabelColor = new Color(1f, 0.94f, 0.76f);
    private static readonly Color DisabledLabelColor = new Color(0.67f, 0.62f, 0.54f, 0.72f);

    private static PokerButtonTheme instance;

    private Sprite buttonSprite;
    private Texture2D buttonTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateThemeController()
    {
        EnsureController();
    }

    public static void EnsureController()
    {
        if (instance != null)
            return;

        instance = FindFirstObjectByType<PokerButtonTheme>();
        if (instance != null)
            return;

        GameObject controller = new GameObject("PokerButtonTheme");
        if (Application.isPlaying)
            DontDestroyOnLoad(controller);
        else
            controller.hideFlags = HideFlags.HideAndDontSave;
        instance = controller.AddComponent<PokerButtonTheme>();
    }

#if UNITY_EDITOR
    public static void RefreshEditorPreview()
    {
        if (Application.isPlaying)
            return;

        EnsureController();
        if (instance != null)
            instance.ApplyThemeToAllButtons();
    }
#endif

    public static void ApplyTo(Button button)
    {
        EnsureController();
        if (instance != null)
            instance.ApplyTheme(button);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        buttonSprite = CreateModernButtonSprite();

        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyThemeToAllButtons();
        if (Application.isPlaying)
            InvokeRepeating(nameof(ApplyThemeToAllButtons), 0.25f, 0.75f);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (instance == this)
            instance = null;

        if (buttonSprite != null)
            Destroy(buttonSprite);

        if (buttonTexture != null)
            Destroy(buttonTexture);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyThemeToAllButtons();
    }

    private void ApplyThemeToAllButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ApplyTheme(button);
    }

    private void ApplyTheme(Button button)
    {
        if (button == null || IsExcludedFromTheme(button))
            return;

        Image background = button.targetGraphic as Image;
        if (background == null)
            background = button.GetComponent<Image>();

        if (background == null)
            return;

        if (Application.isPlaying)
            ConfigureMobileTouchTarget(button);

        bool firstApplication = background.sprite != buttonSprite;
        if (firstApplication)
        {
            background.sprite = buttonSprite;
            background.type = Image.Type.Sliced;
            background.preserveAspect = false;
            background.fillCenter = true;
            background.material = null;
            background.color = Color.white;
            background.raycastTarget = true;

            DisableLegacyOutline(background);
            if (Application.isPlaying)
                ConfigureShadow(background);
            ConfigureButtonTransitions(button);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        label.color = button.interactable ? LabelColor : DisabledLabelColor;
        bool usesCompactMainMenuFont =
            button.gameObject.scene.name == "MainMenu" &&
            !button.name.StartsWith("Info");
        bool usesCompactModeFont =
            button.gameObject.scene.name == "GameModeSelect" &&
            button.name.StartsWith("Mode");
        bool keepsGameplayFont =
            button.gameObject.scene.name == "Game" ||
            button.gameObject.scene.name == "Hot Seat";
        bool isMainMenuUtilityButton =
            button.name == "RulesButton" || button.name == "SettingsButton";

        if (!keepsGameplayFont && TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
            label.fontWeight = FontWeight.Bold;
            label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        }
        else
        {
            label.fontStyle = FontStyles.Bold;
        }

        float labelCanvasScale = 1f;
        if (!keepsGameplayFont)
        {
            // Compare sizes in canvas units, not in differently scaled parents.
            Canvas canvas = label.GetComponentInParent<Canvas>();
            Transform canvasTransform = canvas != null ? canvas.rootCanvas.transform : null;
            Vector3 parentScale = Vector3.one;
            for (Transform parent = label.transform.parent;
                 parent != null && parent != canvasTransform; parent = parent.parent)
                parentScale = Vector3.Scale(parentScale, parent.localScale);

            labelCanvasScale = Mathf.Max(0.001f, Mathf.Abs(parentScale.y));
            label.rectTransform.localScale = new Vector3(
                labelCanvasScale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)), 1f, 1f);
            float localFontSize = MenuLabelCanvasSize / labelCanvasScale;
            label.enableAutoSizing = false;
            label.fontSize = localFontSize;
            label.fontSizeMin = localFontSize;
            label.fontSizeMax = localFontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.characterSpacing = 0f;
        }
        else
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 36f;
            label.characterSpacing = 1f;
        }
        label.margin = usesCompactMainMenuFont
            ? new Vector4(isMainMenuUtilityButton ? 8f : 14f, 6f,
                isMainMenuUtilityButton ? 8f : 14f, 6f)
            : usesCompactModeFont
                ? new Vector4(14f, 4f, 14f, 4f)
                : new Vector4(18f, 6f, 18f, 6f);

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        if (Application.isPlaying)
        {
            Shadow textShadow = GetExactShadow(label.gameObject);
            if (textShadow == null)
                textShadow = label.gameObject.AddComponent<Shadow>();

            textShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            textShadow.effectDistance = new Vector2(1.5f, -1.5f) / labelCanvasScale;
            textShadow.useGraphicAlpha = true;
        }
    }

    private static void ConfigureMobileTouchTarget(Button button)
    {
        if (!(button.transform is RectTransform buttonRect))
            return;

        Transform existing = button.transform.Find("__MobileTouchTarget");
        GameObject targetObject;
        if (existing == null)
        {
            targetObject = new GameObject(
                "__MobileTouchTarget", typeof(RectTransform), typeof(Image));
            targetObject.transform.SetParent(button.transform, false);
            targetObject.transform.SetAsFirstSibling();

            Image targetImage = targetObject.GetComponent<Image>();
            targetImage.color = new Color(1f, 1f, 1f, 0.001f);
            targetImage.raycastTarget = true;
        }
        else
        {
            targetObject = existing.gameObject;
        }

        RectTransform targetRect = targetObject.GetComponent<RectTransform>();
        targetRect.anchorMin = targetRect.anchorMax = new Vector2(0.5f, 0.5f);
        targetRect.pivot = new Vector2(0.5f, 0.5f);
        targetRect.anchoredPosition = Vector2.zero;
        targetRect.sizeDelta = new Vector2(
            Mathf.Max(MinimumTouchWidth, Mathf.Abs(buttonRect.rect.width)),
            Mathf.Max(MinimumTouchHeight, Mathf.Abs(buttonRect.rect.height)));
    }

    private static void ConfigureButtonTransitions(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.82f, 1f);
        colors.pressedColor = new Color(0.72f, 0.66f, 0.60f, 1f);
        colors.selectedColor = new Color(1f, 0.88f, 0.66f, 1f);
        colors.disabledColor = new Color(0.43f, 0.40f, 0.38f, 0.82f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    private static void DisableLegacyOutline(Image background)
    {
        Outline outline = background.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }

    private static void ConfigureShadow(Image background)
    {
        Shadow shadow = GetExactShadow(background.gameObject);
        if (shadow == null)
            shadow = background.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0f, 0f, 0f, 0.52f);
        shadow.effectDistance = new Vector2(0f, -5f);
        shadow.useGraphicAlpha = true;
    }

    private static Shadow GetExactShadow(GameObject target)
    {
        foreach (Shadow effect in target.GetComponents<Shadow>())
        {
            if (effect != null && effect.GetType() == typeof(Shadow))
                return effect;
        }

        return null;
    }

    private Sprite CreateModernButtonSprite()
    {
        const int size = 96;
        const float radius = 23f;
        const float border = 3.5f;

        buttonTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "PokerButtonModern",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color topFill = new Color(0.40f, 0.075f, 0.055f, 1f);
        Color bottomFill = new Color(0.17f, 0.018f, 0.018f, 1f);
        Color topBorder = new Color(1f, 0.80f, 0.34f, 1f);
        Color bottomBorder = new Color(0.62f, 0.34f, 0.07f, 1f);

        for (int y = 0; y < size; y++)
        {
            float vertical = y / (size - 1f);
            Color fill = Color.Lerp(bottomFill, topFill, vertical);
            Color gold = Color.Lerp(bottomBorder, topBorder, vertical);

            for (int x = 0; x < size; x++)
            {
                float outerCoverage = RoundedRectCoverage(x, y, size, radius, 0f);
                float innerCoverage = RoundedRectCoverage(x, y, size, radius, border);
                float borderCoverage = Mathf.Clamp01(outerCoverage - innerCoverage);

                Color pixel = Color.Lerp(fill, gold, borderCoverage);
                pixel.a = outerCoverage;

                float highlight = Mathf.SmoothStep(0f, 1f, vertical) * innerCoverage * 0.08f;
                pixel.r = Mathf.Clamp01(pixel.r + highlight);
                pixel.g = Mathf.Clamp01(pixel.g + highlight * 0.75f);
                pixel.b = Mathf.Clamp01(pixel.b + highlight * 0.35f);

                buttonTexture.SetPixel(x, y, pixel);
            }
        }

        buttonTexture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            buttonTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(28f, 28f, 28f, 28f)
        );
        sprite.name = "PokerButtonModern";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static float RoundedRectCoverage(
        float x,
        float y,
        float size,
        float radius,
        float inset)
    {
        float half = size * 0.5f - inset;
        float localRadius = Mathf.Max(1f, radius - inset);
        Vector2 point = new Vector2(
            Mathf.Abs(x + 0.5f - size * 0.5f),
            Mathf.Abs(y + 0.5f - size * 0.5f)
        );
        Vector2 corner = point - new Vector2(half - localRadius, half - localRadius);
        Vector2 outside = new Vector2(Mathf.Max(corner.x, 0f), Mathf.Max(corner.y, 0f));
        float distance = outside.magnitude + Mathf.Min(Mathf.Max(corner.x, corner.y), 0f) - localRadius;
        return Mathf.Clamp01(0.5f - distance);
    }

    private static bool IsExcludedFromTheme(Button button)
    {
        string name = button.name.ToLowerInvariant();

        if (name.Contains("removeplayer") ||
            name.Contains("delete") ||
            name.Contains("usun"))
        {
            return true;
        }

        if (name == "cardbutton" ||
            name.StartsWith("cardtouch") ||
            name.StartsWith("hs_extracard") ||
            name.StartsWith("rewers"))
        {
            return true;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
            return false;

        return rect.rect.height > rect.rect.width * 1.15f;
    }
}
