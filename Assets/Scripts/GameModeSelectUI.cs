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
            button.interactable = true;

            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();

            layout.enabled = true;

            layout.ignoreLayout = false;
            layout.minWidth = buttonWidth;
            layout.preferredWidth = buttonWidth;
            layout.minHeight = 96f;
            layout.preferredHeight = 96f;

            if (modePanel == null)
                modePanel = button.transform.parent as RectTransform;

            if (button.transform is RectTransform buttonRect)
            {
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = new Vector2(buttonWidth, 96f);
                buttonRect.localScale = Vector3.one;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyles.Bold;
                label.enableAutoSizing = true;
                label.fontSizeMin = 20f;
                label.fontSizeMax = 30f;
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
            heading.fontSizeMin = 30f;
            heading.fontSizeMax = 46f;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
        }
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
