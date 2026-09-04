using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class LobbyStartPhoton : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private Button startButton;
    [SerializeField] private int minPlayers = 2;

    private const string GameStartedKey = "gameStarted";

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        RefreshStartButton();
    }

    public override void OnJoinedRoom() => RefreshStartButton();
    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshStartButton();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshStartButton();
    public override void OnMasterClientSwitched(Player newMasterClient) => RefreshStartButton();

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null &&
            propertiesThatChanged.TryGetValue(GameStartedKey, out object value) &&
            value is bool started && started)
        {
            HotSeatOrientationLock.LockLandscape();
        }
    }

    private void RefreshStartButton()
    {
        if (startButton == null) return;

        bool isMaster = PhotonNetwork.IsMasterClient;
        int count = PhotonNetwork.CurrentRoom != null
            ? PhotonNetwork.CurrentRoom.PlayerCount + LobbyDebugFakePlayers.BotCount
            : 0;

        startButton.interactable = isMaster && (count >= minPlayers);
    }

    public void OnClickStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount + LobbyDebugFakePlayers.BotCount < minPlayers) return;

        Hashtable roomProps = new Hashtable();
        roomProps[GameStartedKey] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        HotSeatOrientationLock.LockLandscape();
        PhotonNetwork.LoadLevel(gameSceneName);
    }
}
