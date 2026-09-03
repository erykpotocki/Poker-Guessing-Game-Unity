using TMPro;
using System.Collections;
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
    private CanvasGroup canvasGroup;
    private Coroutine transitionRoutine;

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
        ShowRoundStart(1, playerName, onStartTurn);
    }

    public void ShowDealIntro(int roundNumber, string firstPlayerName, UnityAction onContinue)
    {
        Show(
            "ROZDANIE KART — RUNDA " + roundNumber,
            "ZA CHWILĘ KAŻDY PO KOLEI ZOBACZY SWOJE KARTY.\n" +
            "ODKRYJ JE, ZAPAMIĘTAJ, ZAKRYJ I DOPIERO WTEDY PODAJ TELEFON DALEJ.",
            "PIERWSZY: " + firstPlayerName,
            "ROZDAJ KARTY",
            onContinue
        );
    }

    public void ShowRoundStart(int roundNumber, string playerName, UnityAction onStartTurn)
    {
        Show(
            "ROZPOCZNIJ RUNDĘ " + roundNumber,
            "W TEJ RUNDZIE MUSISZ PODBIĆ LUB SPRAWDZIĆ.\nPOWODZENIA!\n\n" +
            "PODAJ TELEFON GRACZOWI, KTÓRY ZACZYNA:",
            playerName,
            roundNumber == 1 ? "ROZPOCZNIJ GRĘ" : "ROZPOCZNIJ RUNDĘ " + roundNumber,
            onStartTurn
        );
    }

    public void ShowNextTurn(string playerName, UnityAction onStartTurn)
    {
        Show(
            "PRZEKAŻ TELEFON",
            "PODAJ TELEFON NASTĘPNEMU GRACZOWI:",
            playerName,
            "GOTOWE",
            onStartTurn
        );
    }

    private void Show(
        string title,
        string message,
        string playerName,
        string buttonLabel,
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

        TMP_Text label = startTurnButton != null
            ? startTurnButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (label != null)
            label.text = buttonLabel;

        if (passPhonePanel != null)
        {
            passPhonePanel.SetActive(true);
            PlayEntrance();
        }
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
            canvasGroup = passPhonePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = passPhonePanel.AddComponent<CanvasGroup>();

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

    private void PlayEntrance()
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(AnimateEntrance());
    }

    private IEnumerator AnimateEntrance()
    {
        RectTransform panelRect = passPhonePanel.transform as RectTransform;
        Vector3 targetScale = Vector3.one;
        float elapsed = 0f;
        const float duration = 0.38f;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (panelRect != null)
            panelRect.localScale = targetScale * 0.965f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (canvasGroup != null)
                canvasGroup.alpha = eased;
            if (panelRect != null)
                panelRect.localScale = Vector3.LerpUnclamped(targetScale * 0.965f, targetScale, eased);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (panelRect != null)
            panelRect.localScale = targetScale;
        transitionRoutine = null;
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
