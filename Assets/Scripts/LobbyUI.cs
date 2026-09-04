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
            codeText.text = $"ID: {roomCode}\n<color=#D7B36A><size=65%>DOTKNIJ, ABY SKOPIOWAĆ</size></color>";
        }
        else
        {
            roomCode = string.Empty;
            codeText.text = "ID: -";
        }
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
        codeText.text = $"ID: {roomCode}\n<color=#F5C451><size=65%>SKOPIOWANO KOD ✓</size></color>";
        yield return new WaitForSecondsRealtime(1.4f);
        copyFeedbackRoutine = null;
        RefreshCode();
    }

    private void RefreshGameMode()
    {
        if (gameModeText == null)
            return;

        gameModeText.text = $"Tryb: {GameModeSelectUI.GetSelectedGameMode()}";
    }
}
