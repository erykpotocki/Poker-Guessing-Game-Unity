using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviourPunCallbacks
{
    private static NetworkBootstrap instance;
    private static string sessionUserId;

    private const string UserIdPrefsKey = "PhotonUserId";
    private const string ResumePendingPrefsKey = "ResumePending";
    private const string LastRoomCodePrefsKey = "lastRoomCode";
    private bool shouldRecoverRoom;
    private bool rejoinAfterMasterConnection;
    private bool recoveryRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBootstrapExists()
    {
        if (instance != null)
            return;

        GameObject bootstrap = new GameObject(nameof(NetworkBootstrap));
        bootstrap.AddComponent<NetworkBootstrap>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "0.1";
        PhotonNetwork.KeepAliveInBackground = 300f;

        EnsurePersistentUserIdBeforeConnect();
    }

    private void Start()
    {
        ConnectIfNeeded();
    }

    public void ConnectIfNeeded()
    {
        if (PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon connected to Master | UserId = " + PhotonNetwork.LocalPlayer?.UserId);

        if (rejoinAfterMasterConnection)
        {
            rejoinAfterMasterConnection = false;
            TryRejoinSavedRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (PlayerPrefs.GetInt(ResumePendingPrefsKey, 0) != 1)
            return;

        shouldRecoverRoom = true;
        recoveryRunning = false;
        if (Application.isFocused)
            RequestRoomRecovery();
    }

    public override void OnJoinedRoom()
    {
        shouldRecoverRoom = false;
        rejoinAfterMasterConnection = false;
        recoveryRunning = false;
        PlayerPrefs.SetInt(ResumePendingPrefsKey, 0);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            RememberCurrentRoom();
            return;
        }

        if (shouldRecoverRoom || PlayerPrefs.GetInt(ResumePendingPrefsKey, 0) == 1)
            RequestRoomRecovery();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && (shouldRecoverRoom ||
            PlayerPrefs.GetInt(ResumePendingPrefsKey, 0) == 1))
        {
            RequestRoomRecovery();
        }
    }

    private void RememberCurrentRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        shouldRecoverRoom = true;
        PlayerPrefs.SetString(LastRoomCodePrefsKey, PhotonNetwork.CurrentRoom.Name);
        PlayerPrefs.SetInt(ResumePendingPrefsKey, 1);
        PlayerPrefs.Save();
    }

    private IEnumerator RecoverRoomAfterResume()
    {
        yield return new WaitForSecondsRealtime(0.75f);

        if (PhotonNetwork.InRoom)
        {
            OnJoinedRoom();
            yield break;
        }

        EnsurePersistentUserIdBeforeConnect();
        if (!PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.ReconnectAndRejoin())
            {
                rejoinAfterMasterConnection = true;
                PhotonNetwork.ConnectUsingSettings();
            }
            yield break;
        }

        if (PhotonNetwork.IsConnectedAndReady)
            TryRejoinSavedRoom();
        else
            rejoinAfterMasterConnection = true;

    }

    private void RequestRoomRecovery()
    {
        if (recoveryRunning)
            return;

        recoveryRunning = true;
        StartCoroutine(RecoverRoomAfterResume());
    }

    private void TryRejoinSavedRoom()
    {
        string roomCode = PlayerPrefs.GetString(LastRoomCodePrefsKey, "");
        if (!string.IsNullOrWhiteSpace(roomCode) && !PhotonNetwork.InRoom)
            PhotonNetwork.RejoinRoom(roomCode);
        else if (string.IsNullOrWhiteSpace(roomCode))
            recoveryRunning = false;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        recoveryRunning = false;
        Debug.LogWarning($"Nie udało się wrócić do pokoju: {message} ({returnCode})");
    }

    private void EnsurePersistentUserIdBeforeConnect()
    {
        if (string.IsNullOrWhiteSpace(sessionUserId))
        {
            sessionUserId = PlayerPrefs.GetString(UserIdPrefsKey, "");

            if (string.IsNullOrWhiteSpace(sessionUserId))
            {
                sessionUserId = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(UserIdPrefsKey, sessionUserId);
                PlayerPrefs.Save();
            }
        }

        if (PhotonNetwork.AuthValues == null)
            PhotonNetwork.AuthValues = new AuthenticationValues();

        PhotonNetwork.AuthValues.UserId = sessionUserId;
    }
}
