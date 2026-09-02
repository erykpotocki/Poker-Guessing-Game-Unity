using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies the shared wood-and-gold visual language to every Unity UI button.
/// It is created automatically, so dynamic Hot Seat buttons receive the theme too.
/// </summary>
public sealed class PokerButtonTheme : MonoBehaviour
{
    private const string ButtonTexturePath = "PokerUI/PokerButtonWoodRect";
    private static readonly Color LabelColor = new Color(1f, 0.94f, 0.73f);
    private Sprite buttonSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateThemeController()
    {
        EnsureController();
    }

    public static void EnsureController()
    {
        if (FindFirstObjectByType<PokerButtonTheme>() != null)
            return;

        GameObject controller = new GameObject("PokerButtonTheme");
        DontDestroyOnLoad(controller);
        controller.AddComponent<PokerButtonTheme>();
    }

    private void Awake()
    {
        Sprite[] importedSprites =
            Resources.LoadAll<Sprite>(ButtonTexturePath);

        float largestArea = 0f;
        foreach (Sprite importedSprite in importedSprites)
        {
            if (importedSprite == null)
                continue;

            float area = importedSprite.rect.width * importedSprite.rect.height;
            if (area > largestArea)
            {
                largestArea = area;
                buttonSprite = importedSprite;
            }
        }

        if (buttonSprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(ButtonTexturePath);
            if (texture != null)
            {
                buttonSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyThemeToAllButtons();
        InvokeRepeating(nameof(ApplyThemeToAllButtons), 0.4f, 0.8f);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyThemeToAllButtons();
    }

    private void ApplyThemeToAllButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || IsExcludedFromWoodTheme(button))
                continue;

            Image background = button.targetGraphic as Image;
            if (background == null)
                background = button.GetComponent<Image>();

            if (background == null)
                continue;

            if (buttonSprite != null && background.sprite != buttonSprite)
            {
                background.sprite = buttonSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }

            background.enabled = true;
            background.material = null;
            background.color = buttonSprite != null
                ? Color.white
                : new Color(0.42f, 0.06f, 0.035f, 1f);
            background.canvasRenderer.SetAlpha(1f);

            Outline outline = background.GetComponent<Outline>();
            if (outline == null)
                outline = background.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0.92f, 0.67f, 0.20f, 1f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            outline.useGraphicAlpha = true;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.91f, 0.63f);
            colors.pressedColor = new Color(0.78f, 0.63f, 0.36f);
            colors.selectedColor = new Color(1f, 0.86f, 0.48f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.75f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = LabelColor;
        }
    }

    private static bool IsExcludedFromWoodTheme(Button button)
    {
        string name = button.name.ToLowerInvariant();

        // Compact destructive controls stay immediately recognisable.
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

        // Poker cards are tall touch areas. Decorative and menu buttons are horizontal.
        return rect.rect.height > rect.rect.width * 1.15f;
    }
}
