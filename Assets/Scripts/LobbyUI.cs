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
            codeText.text = $"ID: {roomCode}\n<line-height=145%><color=#A8997A99><size=40%>DOTKNIJ, ABY SKOPIOWAĆ</size></color></line-height>";
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
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -105f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 120f);
        }

        if (gameModeText != null)
        {
            RectTransform rect = gameModeText.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -245f);
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
        codeText.text = $"ID: {roomCode}\n<line-height=145%><color=#BDAA7FAA><size=40%>SKOPIOWANO KOD ✓</size></color></line-height>";
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
