using Photon.Pun;
using TMPro;
using UnityEngine;

public class TurnDebugUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text turnTimerText;

    [Header("Timer thresholds")]
    [SerializeField] private float warningThresholdSeconds = 10f;

    [Header("Timer colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.65f, 0.25f, 1f);
    [SerializeField] private Color overtimeTimerColor = Color.red;

    [Header("Pulse settings")]
    [SerializeField] private float warningPulseSpeed = 1.2f;
    [SerializeField] private float warningPulseScaleAmount = 0.035f;
    [SerializeField] private float overtimePulseSpeed = 6f;
    [SerializeField] private float overtimePulseScaleAmount = 0.15f;

    private Vector3 timerBaseScale = Vector3.one;

    private void OnEnable()
    {
        if (turnManager != null)
            turnManager.OnActivePlayerChanged += HandleActivePlayerChanged;
    }

    private void OnDisable()
    {
        if (turnManager != null)
            turnManager.OnActivePlayerChanged -= HandleActivePlayerChanged;
    }

    private void Start()
    {
        if (turnTimerText != null)
        {
            timerBaseScale = turnTimerText.rectTransform.localScale;
            turnTimerText.color = normalTimerColor;
            turnTimerText.rectTransform.localScale = timerBaseScale;
        }

        RefreshNow();
    }

    private void Update()
    {
        if (turnManager == null || !turnManager.IsInitialized)
            return;

        UpdateTimerVisuals();
    }

    public void RefreshNow()
    {
        if (turnText == null)
            return;

        if (turnManager == null || !turnManager.IsInitialized)
        {
            turnText.text = "Tura: ---";

            if (turnTimerText != null)
            {
                turnTimerText.text = "Czas: ---";
                turnTimerText.color = normalTimerColor;
                turnTimerText.rectTransform.localScale = timerBaseScale;
            }

            return;
        }

        UpdateText(turnManager.CurrentPlayerActorNumber);
    }

    private void HandleActivePlayerChanged(int actorNumber)
    {
        if (turnTimerText != null)
        {
            turnTimerText.color = normalTimerColor;
            turnTimerText.rectTransform.localScale = timerBaseScale;
        }

        UpdateText(actorNumber);
    }

    private void UpdateText(int actorNumber)
    {
        if (turnText == null)
            return;

        if (actorNumber <= 0)
        {
            turnText.text = "Tura: brak aktywnego gracza";

            if (turnTimerText != null)
                turnTimerText.text = "Czas: ---";

            return;
        }

        bool isLocalTurn =
            PhotonNetwork.LocalPlayer != null &&
            actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        if (isLocalTurn)
        {
            turnText.text = "Twoja tura";
        }
        else
        {
            string playerDisplayName = "Gracz " + actorNumber;

            if (LobbyBotRegistry.TryGetBot(actorNumber, out LobbyBotInfo bot))
            {
                playerDisplayName = bot.Name;
            }
            else if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.Players != null &&
                PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out Photon.Realtime.Player player) &&
                player != null &&
                !string.IsNullOrWhiteSpace(player.NickName))
            {
                playerDisplayName = player.NickName;
            }

            turnText.text = "Ruch gracza " + playerDisplayName;
        }

        UpdateTimerVisuals();
    }

    private void UpdateTimerVisuals()
    {
        if (turnTimerText == null)
            return;

        if (turnManager == null || !turnManager.IsInitialized)
        {
            turnTimerText.text = "Czas: ---";
            turnTimerText.color = normalTimerColor;
            turnTimerText.rectTransform.localScale = timerBaseScale;
            return;
        }

        float currentTimeLeft = turnManager.CurrentTurnTimeLeft;

        int displaySeconds = currentTimeLeft > 0f
            ? Mathf.CeilToInt(currentTimeLeft)
            : -Mathf.FloorToInt(Mathf.Abs(currentTimeLeft));

        turnTimerText.text = "Czas: " + displaySeconds + " s";

        if (turnManager.IsResolutionLocked)
        {
            return;
        }

        if (currentTimeLeft > warningThresholdSeconds)
        {
            turnTimerText.color = normalTimerColor;
            turnTimerText.rectTransform.localScale = timerBaseScale;
            return;
        }

        if (currentTimeLeft > 0f)
        {
            turnTimerText.color = warningTimerColor;

            float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * warningPulseSpeed)) * warningPulseScaleAmount;
            turnTimerText.rectTransform.localScale = timerBaseScale * pulse;
            return;
        }

        turnTimerText.color = overtimeTimerColor;

        float overtimePulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * overtimePulseSpeed)) * overtimePulseScaleAmount;
        turnTimerText.rectTransform.localScale = timerBaseScale * overtimePulse;
    }
}
