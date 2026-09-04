using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviourPunCallbacks
{
    private static NetworkBootstrap instance;
    private static string sessionUserId;

    private const string UserIdPrefsKey = "PhotonUserId";

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
