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
                title + "\n\n" +
                message + "\n\n" +
                playerName;
        }

        if (passPhonePanel != null)
            passPhonePanel.SetActive(true);
    }

    public void Hide()
    {
        if (passPhonePanel != null)
            passPhonePanel.SetActive(false);
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
