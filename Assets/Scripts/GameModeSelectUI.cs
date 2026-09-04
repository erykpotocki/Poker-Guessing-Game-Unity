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

    private void Start()
    {
        PokerButtonTheme.EnsureController();
        ConfigureModeButtons();
    }

    private void Update()
    {
        // BackToMenu applies one shared safe-area layout to the close button.
    }

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

            layout.minWidth = 560f;
            layout.preferredWidth = 620f;
            layout.minHeight = 88f;
            layout.preferredHeight = 96f;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyles.Bold;
                label.enableAutoSizing = true;
                label.fontSizeMin = 20f;
                label.fontSizeMax = 30f;
            }
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
