using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameModeSelectUI : MonoBehaviour
{
    public const string SelectedGameModeKey = "selectedGameMode";

    public const string BeginnerModeName = "Początkujący";
    public const string ClassicModeName = "Klasyczny";
    public const string FastModeName = "Przyśpieszony";
    public const string Mode420Name = "420";

    private RectTransform closeButtonRect;
    private Vector2 closeButtonBasePosition;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Start()
    {
        PokerButtonTheme.EnsureController();
        ConfigureModeButtons();
    }

    private void Update()
    {
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (size != lastScreenSize || Screen.safeArea != lastSafeArea)
            ConfigureModeButtons();
    }

#if UNITY_EDITOR
    public void RefreshEditorPreview() => ConfigureModeButtons();
#endif

    public void SelectBeginnerMode()
    {
        SelectModeAndGoToCreateRoom(BeginnerModeName);
    }

    public void SelectClassicMode()
    {
        SelectModeAndGoToCreateRoom(ClassicModeName);
    }

    public void SelectFastMode()
    {
        SelectModeAndGoToCreateRoom(FastModeName);
    }

    public void Select420Mode()
    {
        SelectModeAndGoToCreateRoom(Mode420Name);
    }

    public void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public static string GetSelectedGameMode()
    {
        return PlayerPrefs.GetString(SelectedGameModeKey, ClassicModeName);
    }

    private void SelectModeAndGoToCreateRoom(string modeName)
    {
        // Also guard direct UnityEvent calls, not just pointer interaction.
        if (modeName != ClassicModeName)
            return;

        PlayerPrefs.SetString(SelectedGameModeKey, modeName);
        PlayerPrefs.Save();

        SceneManager.LoadScene("CreateRoom");
    }

    private void ConfigureModeButtons()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        float availableWidth = canvasRect != null ? canvasRect.rect.width : 1080f;
        float buttonWidth = Mathf.Clamp(availableWidth * 0.76f, 520f, 760f);
        RectTransform modePanel = null;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (!button.name.StartsWith("Mode"))
                continue;

            button.enabled = true;
            bool available = button.name == "Mode2";
            button.interactable = available;
            ConfigureAvailability(button, available);

            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();

            layout.enabled = true;

            layout.ignoreLayout = false;
            layout.minWidth = buttonWidth;
            layout.preferredWidth = buttonWidth;
            layout.minHeight = 116f;
            layout.preferredHeight = 116f;

            if (modePanel == null)
                modePanel = button.transform.parent as RectTransform;

            if (button.transform is RectTransform buttonRect)
            {
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = new Vector2(buttonWidth, 116f);
                buttonRect.localScale = Vector3.one;
            }

        }

        if (modePanel != null)
        {
            modePanel.localScale = Vector3.one;
            modePanel.anchorMin = modePanel.anchorMax = new Vector2(0.5f, 0.45f);
            modePanel.pivot = new Vector2(0.5f, 0.5f);
            modePanel.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup group = modePanel.GetComponent<VerticalLayoutGroup>();
            if (group != null)
            {
                group.spacing = 24f;
                group.childAlignment = TextAnchor.MiddleCenter;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
            }
        }

        // Apply the shared typography after the panel scale has been normalized.
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name.StartsWith("Mode"))
                PokerButtonTheme.ApplyTo(button);
        }

        TMP_Text heading = null;
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text.Trim().Equals("Wybierz Tryb", System.StringComparison.OrdinalIgnoreCase))
            {
                heading = text;
                break;
            }
        }

        if (heading != null)
        {
            RectTransform headingRect = heading.rectTransform;
            headingRect.localScale = Vector3.one;
            headingRect.anchorMin = headingRect.anchorMax = new Vector2(0.5f, 0.69f);
            headingRect.pivot = new Vector2(0.5f, 0.5f);
            headingRect.anchoredPosition = Vector2.zero;
            headingRect.sizeDelta = new Vector2(buttonWidth, 100f);
            heading.enableAutoSizing = true;
            heading.fontSizeMin = 42f;
            heading.fontSizeMax = 60f;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private static void ConfigureAvailability(Button button, bool available)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            string title = label.text.Split('\n')[0];
            label.text = available ? title : title +
                "\n<size=55%><color=#B8AA8A>NIEDOSTĘPNY</color></size>";
        }

        Transform existing = button.transform.Find("ModeLock");
        if (existing != null)
        {
            existing.gameObject.SetActive(!available);
            return;
        }
        if (available)
            return;

        GameObject lockObject = new GameObject("ModeLock", typeof(RectTransform));
        RectTransform lockRect = lockObject.GetComponent<RectTransform>();
        lockRect.SetParent(button.transform, false);
        lockRect.anchorMin = lockRect.anchorMax = new Vector2(0f, 0.5f);
        lockRect.anchoredPosition = new Vector2(42f, 0f);
        lockRect.sizeDelta = new Vector2(36f, 46f);
        // UI geometry avoids depending on an emoji glyph in the menu font.
        CreateLockPart(lockRect, "Body", new Vector2(0f, -7f), new Vector2(32f, 26f));
        CreateLockPart(lockRect, "ShackleTop", new Vector2(0f, 19f), new Vector2(22f, 5f));
        CreateLockPart(lockRect, "ShackleLeft", new Vector2(-9f, 11f), new Vector2(5f, 16f));
        CreateLockPart(lockRect, "ShackleRight", new Vector2(9f, 11f), new Vector2(5f, 16f));
        Image keyhole = CreateLockPart(lockRect, "Keyhole", new Vector2(0f, -7f), new Vector2(5f, 11f));
        keyhole.color = new Color(0.15f, 0.07f, 0.04f);
    }

    private static Image CreateLockPart(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject part = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = part.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = part.GetComponent<Image>();
        image.color = new Color(0.72f, 0.64f, 0.44f);
        image.raycastTarget = false;
        return image;
    }

    private void ResolveCloseButton()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == "Wyjdź" ||
                button.name.ToLowerInvariant().Contains("close"))
            {
                closeButtonRect = button.transform as RectTransform;
                if (closeButtonRect != null)
                    closeButtonBasePosition = closeButtonRect.anchoredPosition;
                break;
            }
        }
    }

    private void ApplySafeArea()
    {
        lastSafeArea = Screen.safeArea;
        if (closeButtonRect == null)
            return;

        Canvas canvas = closeButtonRect.GetComponentInParent<Canvas>();
        float canvasScale = canvas != null
            ? Mathf.Max(0.01f, canvas.scaleFactor)
            : 1f;
        float topInset = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);

        closeButtonRect.anchoredPosition = closeButtonBasePosition +
            Vector2.down * (topInset / canvasScale + 22f);
    }
}
