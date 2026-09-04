using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoResumeRoom : MonoBehaviourPunCallbacks
{
    private const string ResumePendingPrefsKey = "ResumePending";
    private const string LastRoomCodePrefsKey = "lastRoomCode";
    private const string GameStartedKey = "gameStarted";
    private const string GameEndedKey = "gameEnded";

    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private float waitForPhotonSeconds = 8f;

    private bool triedAutoResume = false;
    private bool leavingRejectedRoom = false;

    private void Start()
    {
        StartCoroutine(TryAutoResume());
    }

    private IEnumerator TryAutoResume()
    {
        if (PlayerPrefs.GetInt(ResumePendingPrefsKey, 0) != 1)
            yield break;

        string roomCode = PlayerPrefs.GetString(LastRoomCodePrefsKey, "");
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            ClearResumePrefs();
            yield break;
        }

        float timer = 0f;
        while (!PhotonNetwork.IsConnectedAndReady && timer < waitForPhotonSeconds)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
            yield break;

        if (triedAutoResume)
            yield break;

        triedAutoResume = true;
        PhotonNetwork.RejoinRoom(roomCode);
    }

    public override void OnJoinedRoom()
    {
        bool gameStarted = false;
        bool gameEnded = false;

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameStartedKey, out object startedValue) &&
                startedValue is bool startedBool)
            {
                gameStarted = startedBool;
            }

            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameEndedKey, out object endedValue) &&
                endedValue is bool endedBool)
            {
                gameEnded = endedBool;
            }
        }

        int playerCount = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;

        if (gameEnded || playerCount <= 1)
        {
            ClearResumePrefs();

            if (PhotonNetwork.InRoom && !leavingRejectedRoom)
            {
                leavingRejectedRoom = true;
                PhotonNetwork.LeaveRoom();
            }

            return;
        }

        ClearResumePrefs();
        if (gameStarted)
            HotSeatOrientationLock.LockLandscape();

        SceneManager.LoadScene(gameStarted ? gameSceneName : lobbySceneName);
    }

    public override void OnLeftRoom()
    {
        leavingRejectedRoom = false;

        if (SceneManager.GetActiveScene().name != "MainMenu")
            SceneManager.LoadScene("MainMenu");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ClearResumePrefs();
        Debug.LogWarning($"AutoResumeRoom: Rejoin failed: {message} ({returnCode})");
    }

    private void ClearResumePrefs()
    {
        PlayerPrefs.SetInt(ResumePendingPrefsKey, 0);
        PlayerPrefs.DeleteKey(LastRoomCodePrefsKey);
        PlayerPrefs.Save();
    }
}
