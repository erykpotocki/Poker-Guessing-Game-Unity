using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HotSeatPassPhoneUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject passPhonePanel;
    [SerializeField] private TextMeshProUGUI passPhoneText;
    [SerializeField] private Button startTurnButton;

    private UnityAction startTurnAction;

    private void Awake()
    {
        ConfigureVisuals();

        if (passPhonePanel != null)
            passPhonePanel.SetActive(false);

        if (startTurnButton != null)
            startTurnButton.onClick.AddListener(HandleStartTurnClicked);
    }

    public void ShowInitialRound(string playerName, UnityAction onStartTurn)
    {
        Show(
            "ROZPOCZNIJ RUNDĘ",
            "PODAJ TELEFON GRACZOWI,\nKTÓRY ROZPOCZYNA RUNDĘ:",
            playerName,
            onStartTurn
        );
    }

    public void ShowNextTurn(string playerName, UnityAction onStartTurn)
    {
        Show(
            "PRZEKAŻ TELEFON",
            "PODAJ TELEFON NASTĘPNEMU GRACZOWI:",
            playerName,
            onStartTurn
        );
    }

    private void Show(
        string title,
        string message,
        string playerName,
        UnityAction onStartTurn)
    {
        startTurnAction = onStartTurn;

        if (passPhoneText != null)
        {
            passPhoneText.text =
                "<color=#F4C75E><size=125%>" + title + "</size></color>\n\n" +
                "<size=82%>" + message + "</size>\n\n" +
                "<color=#FFF0C2><size=112%>" + playerName.ToUpper() + "</size></color>";
        }

        if (passPhonePanel != null)
            passPhonePanel.SetActive(true);
    }

    public void Hide()
    {
        if (passPhonePanel != null)
            passPhonePanel.SetActive(false);
    }

    private void ConfigureVisuals()
    {
        if (passPhonePanel != null)
        {
            Image background = passPhonePanel.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.005f, 0.055f, 0.032f, 0.94f);
        }

        if (passPhoneText != null)
        {
            RectTransform textRect = passPhoneText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 80f);
            textRect.sizeDelta = new Vector2(820f, 520f);

            passPhoneText.alignment = TextAlignmentOptions.Center;
            passPhoneText.textWrappingMode = TextWrappingModes.Normal;
            passPhoneText.enableAutoSizing = true;
            passPhoneText.fontSizeMin = 24f;
            passPhoneText.fontSizeMax = 39f;
            passPhoneText.lineSpacing = 8f;
            passPhoneText.color = Color.white;
        }

        if (startTurnButton != null)
        {
            if (startTurnButton.transform is RectTransform buttonRect)
            {
                buttonRect.anchoredPosition = new Vector2(0f, 250f);
                buttonRect.sizeDelta = new Vector2(720f, 112f);
            }

            PokerButtonTheme.ApplyTo(startTurnButton);
        }
    }

    private void HandleStartTurnClicked()
    {
        Hide();

        UnityAction action = startTurnAction;
        startTurnAction = null;

        action?.Invoke();
    }

    private void OnDestroy()
    {
        if (startTurnButton != null)
            startTurnButton.onClick.RemoveListener(HandleStartTurnClicked);
    }
}
