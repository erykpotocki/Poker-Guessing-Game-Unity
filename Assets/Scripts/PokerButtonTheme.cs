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
        Texture2D texture = Resources.Load<Texture2D>(ButtonTexturePath);

        if (texture == null)
        {
            Sprite[] importedSprites =
                Resources.LoadAll<Sprite>("PokerUI");

            foreach (Sprite importedSprite in importedSprites)
            {
                if (importedSprite != null &&
                    importedSprite.name.StartsWith("PokerButtonWoodRect"))
                {
                    texture = importedSprite.texture;
                    break;
                }
            }
        }
        if (texture != null)
        {
            buttonSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(106f, 106f, 106f, 106f)
            );
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
        if (buttonSprite == null)
            return;

        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || IsExcludedFromWoodTheme(button))
                continue;

            Image background = button.targetGraphic as Image;
            if (background == null)
                background = button.GetComponent<Image>();

            if (background == null)
                continue;

            if (background.sprite != buttonSprite)
            {
                background.sprite = buttonSprite;
                background.type = Image.Type.Sliced;
                background.preserveAspect = false;
                background.color = Color.white;

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.91f, 0.63f);
                colors.pressedColor = new Color(0.78f, 0.63f, 0.36f);
                colors.selectedColor = new Color(1f, 0.86f, 0.48f);
                colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.75f);
                colors.colorMultiplier = 1f;
                button.colors = colors;
            }

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
