using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndSequenceController : MonoBehaviourPunCallbacks
{
    [Header("Refs")]
    [SerializeField] private GameEndOverlayUI overlayUI;

    [Header("Timing")]
    [SerializeField] private float fireworksDelay = 0.25f;
    [SerializeField] private float continueButtonDelay = 3f;

    [Header("Scene Return")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private GameObject loadingOverlay;

    private Coroutine currentRoutine;
    private bool isLeavingRoom = false;

    private void Awake()
    {
        if (overlayUI != null)
        {
            overlayUI.HideInstant();
            overlayUI.SetContinueAction(OnContinueClicked);
        }

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }
    }

    public void PlayFinalWinnerSequence(Sprite winnerAvatar, string winnerNick)
    {
        if (overlayUI == null)
        {
            Debug.LogWarning("GameEndSequenceController: brak overlayUI.");
            return;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(PlayFinalWinnerSequenceRoutine(winnerAvatar, winnerNick));
    }

    private IEnumerator PlayFinalWinnerSequenceRoutine(Sprite winnerAvatar, string winnerNick)
    {
        overlayUI.ShowWinner(winnerAvatar, winnerNick);

        yield return new WaitForSeconds(fireworksDelay);
        overlayUI.ShowFireworks();

        yield return new WaitForSeconds(continueButtonDelay);
        overlayUI.ShowContinueButton();

        currentRoutine = null;
    }

    private void OnContinueClicked()
    {
        if (isLeavingRoom)
            return;

        isLeavingRoom = true;
        HotSeatOrientationLock.LockPortrait();

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
        }

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public override void OnLeftRoom()
    {
        HotSeatOrientationLock.LockPortrait();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
