using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HotSeatTurnUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject turnControlsRoot;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI currentBidText;
    [SerializeField] private GameObject checkSeparatorText;

    [Header("Buttons")]
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button checkButton;

    private UnityAction raiseAction;
    private UnityAction checkAction;

    private RectTransform raiseRect;
    private RectTransform checkRect;
    private TextMeshProUGUI checkPromptText;

    private void Awake()
    {
        Hide();

        ConfigureResponsiveLayout();

        PokerButtonTheme.ApplyTo(raiseButton);
        PokerButtonTheme.ApplyTo(checkButton);

        if (raiseButton != null)
            raiseButton.onClick.AddListener(HandleRaiseClicked);

        if (checkButton != null)
            checkButton.onClick.AddListener(HandleCheckClicked);
    }

    public void Show()
    {
        if (turnControlsRoot != null)
            turnControlsRoot.SetActive(true);
    }

    public void Hide()
    {
        if (turnControlsRoot != null)
            turnControlsRoot.SetActive(false);
    }

    public void SetActions(
        UnityAction onRaise,
        UnityAction onCheck)
    {
        raiseAction = onRaise;
        checkAction = onCheck;
    }

    public void SetTimer(float seconds)
    {
        if (timerText == null)
            return;

        int displayedSeconds = seconds >= 0f
            ? Mathf.CeilToInt(seconds)
            : Mathf.FloorToInt(seconds);

        timerText.text = displayedSeconds + " s";
    }

    public void SetCurrentBid(string bidName)
    {
        if (currentBidText == null)
            return;

        currentBidText.text = string.IsNullOrWhiteSpace(bidName)
            ? "BRAK POPRZEDNIEJ DEKLARACJI"
            : "POPRZEDNI GRACZ WSKAZAŁ:\n" + bidName.ToUpper();
    }

    public void SetCheckAvailable(bool available)
    {
        if (checkButton != null)
            checkButton.gameObject.SetActive(available);

        if (checkSeparatorText != null)
            checkSeparatorText.SetActive(available);

        if (checkPromptText != null)
            checkPromptText.text = "CZY CHCESZ SPRAWDZIĆ?";

        SetButtonLabel(
            raiseButton,
            available ? "NIE — PODBIJAM WYŻEJ" : "WSKAŻ UKŁAD"
        );
        SetButtonLabel(checkButton, "TAK — SPRAWDZAM");

        if (raiseRect != null)
        {
            raiseRect.anchoredPosition = available
                ? new Vector2(-215f, 74f)
                : new Vector2(0f, 74f);
            raiseRect.sizeDelta = available
                ? new Vector2(390f, 104f)
                : new Vector2(820f, 104f);
        }

        if (checkRect != null)
        {
            checkRect.anchoredPosition = new Vector2(215f, 74f);
            checkRect.sizeDelta = new Vector2(390f, 104f);
        }
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = value;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 34f;
            label.fontStyle = FontStyles.Bold;
        }
    }

    private void ConfigureResponsiveLayout()
    {
        if (turnControlsRoot != null &&
            turnControlsRoot.transform is RectTransform rootRect)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        if (currentBidText != null)
        {
            RectTransform bidRect = currentBidText.rectTransform;
            bidRect.anchorMin = new Vector2(0.5f, 1f);
            bidRect.anchorMax = new Vector2(0.5f, 1f);
            bidRect.pivot = new Vector2(0.5f, 1f);
            // Keep the declaration clearly below the safe-area exit button.
            bidRect.anchoredPosition = new Vector2(0f, -285f);
            bidRect.sizeDelta = new Vector2(880f, 110f);
            currentBidText.alignment = TextAlignmentOptions.Center;
            currentBidText.enableAutoSizing = true;
            currentBidText.fontSizeMin = 28f;
            currentBidText.fontSizeMax = 40f;
            currentBidText.fontStyle = FontStyles.Bold;
        }

        raiseRect = raiseButton != null
            ? raiseButton.transform as RectTransform
            : null;
        checkRect = checkButton != null
            ? checkButton.transform as RectTransform
            : null;

        ConfigureBottomButton(raiseRect);
        ConfigureBottomButton(checkRect);

        if (checkSeparatorText != null)
        {
            checkPromptText = checkSeparatorText.GetComponent<TextMeshProUGUI>();

            if (checkSeparatorText.transform is RectTransform promptRect)
            {
                promptRect.anchorMin = new Vector2(0.5f, 0f);
                promptRect.anchorMax = new Vector2(0.5f, 0f);
                promptRect.pivot = new Vector2(0.5f, 0f);
                promptRect.anchoredPosition = new Vector2(0f, 202f);
                promptRect.sizeDelta = new Vector2(820f, 64f);
            }

            if (checkPromptText != null)
            {
                checkPromptText.text = "CZY CHCESZ SPRAWDZIĆ?";
                checkPromptText.alignment = TextAlignmentOptions.Center;
                checkPromptText.enableAutoSizing = true;
                checkPromptText.fontSizeMin = 24f;
                checkPromptText.fontSizeMax = 34f;
                checkPromptText.fontStyle = FontStyles.Bold;
            }
        }
    }

    private static void ConfigureBottomButton(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (raiseButton != null)
            raiseButton.interactable = interactable;

        if (checkButton != null)
            checkButton.interactable = interactable;
    }

    private void HandleRaiseClicked()
    {
        raiseAction?.Invoke();
    }

    private void HandleCheckClicked()
    {
        checkAction?.Invoke();
    }

    private void OnDestroy()
    {
        if (raiseButton != null)
            raiseButton.onClick.RemoveListener(HandleRaiseClicked);

        if (checkButton != null)
            checkButton.onClick.RemoveListener(HandleCheckClicked);
    }
}
