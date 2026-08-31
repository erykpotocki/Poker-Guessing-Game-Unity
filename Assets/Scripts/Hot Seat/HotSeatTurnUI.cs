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

    private void Awake()
    {
        Hide();

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
            ? "BRAK DEKLARACJI"
            : bidName.ToUpper();
    }

    public void SetCheckAvailable(bool available)
    {
        if (checkButton != null)
            checkButton.gameObject.SetActive(available);

        if (checkSeparatorText != null)
            checkSeparatorText.SetActive(available);
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