using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text gameModeText;
    private Coroutine copyFeedbackRoutine;
    private string roomCode;

    private void Start()
    {
        ConfigureCodeCopyButton();
        ConfigureHeaderSpacing();
        RefreshUI();
    }

    public override void OnJoinedRoom()
    {
        RefreshUI();
    }

    public override void OnLeftRoom()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshCode();
        RefreshGameMode();
    }

    private void RefreshCode()
    {
        if (codeText == null)
            return;

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            roomCode = PhotonNetwork.CurrentRoom.Name;
            codeText.text = $"ID: {roomCode}\n<line-height=175%><color=#B8AA8A99><size=30%>DOTKNIJ, ABY SKOPIOWAĆ</size></color></line-height>";
        }
        else
        {
            roomCode = string.Empty;
            codeText.text = "ID: -";
        }
    }

    private void ConfigureHeaderSpacing()
    {
        if (codeText != null)
        {
            RectTransform rect = codeText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -78f);
            rect.sizeDelta = new Vector2(680f, 170f);
            codeText.alignment = TextAlignmentOptions.Top;
            codeText.fontSize = 68f;
            codeText.fontWeight = FontWeight.Bold;
            codeText.fontStyle = FontStyles.Bold;
        }

        if (gameModeText != null)
        {
            RectTransform rect = gameModeText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -310f);
            rect.sizeDelta = new Vector2(680f, 54f);
            gameModeText.alignment = TextAlignmentOptions.Center;
            gameModeText.fontSize = 34f;
            gameModeText.fontWeight = FontWeight.Bold;
        }

        TMP_Text playerCount = FindText("PlayerCountText");
        if (playerCount != null)
        {
            RectTransform rect = playerCount.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -238f);
            rect.sizeDelta = new Vector2(680f, 54f);
            playerCount.alignment = TextAlignmentOptions.Center;
            playerCount.fontSize = 32f;
            playerCount.fontWeight = FontWeight.Bold;
        }
    }

    private static TMP_Text FindText(string objectName)
    {
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text != null && text.name == objectName)
                return text;
        }

        return null;
    }

    private void ConfigureCodeCopyButton()
    {
        if (codeText == null)
            return;

        codeText.raycastTarget = true;
        Button copyButton = codeText.GetComponent<Button>();
        if (copyButton == null)
            copyButton = codeText.gameObject.AddComponent<Button>();

        copyButton.transition = Selectable.Transition.None;
        copyButton.targetGraphic = codeText;
        copyButton.onClick.RemoveListener(CopyRoomCode);
        copyButton.onClick.AddListener(CopyRoomCode);
    }

    private void CopyRoomCode()
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            return;

        GUIUtility.systemCopyBuffer = roomCode;
        if (copyFeedbackRoutine != null)
            StopCoroutine(copyFeedbackRoutine);
        copyFeedbackRoutine = StartCoroutine(ShowCopyFeedback());
    }

    private IEnumerator ShowCopyFeedback()
    {
        codeText.text = $"ID: {roomCode}\n<line-height=175%><color=#CBB98FAA><size=30%>SKOPIOWANO KOD ✓</size></color></line-height>";
        yield return new WaitForSecondsRealtime(1.4f);
        copyFeedbackRoutine = null;
        RefreshCode();
    }

    private void RefreshGameMode()
    {
        if (gameModeText == null)
            return;

        gameModeText.text = $"TRYB: {GameModeSelectUI.GetSelectedGameMode().ToUpperInvariant()}";
    }
}
