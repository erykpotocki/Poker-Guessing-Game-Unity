using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinRoomUI : MonoBehaviourPunCallbacks
{
    public TMP_InputField nickInput;
    public TMP_InputField roomIdInput;
    public Button joinButton;

    private const string GameStartedKey = "gameStarted";
    private const string UserIdPrefsKey = "PhotonUserId";

    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        ConfigureMobileInputs();
        ConfigureResponsiveLayout();
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(JoinRoom);
        }

        if (nickInput != null)
            nickInput.onValueChanged.AddListener(_ => Validate());

        if (roomIdInput != null)
            roomIdInput.onValueChanged.AddListener(_ => Validate());

        Validate();
    }

    private void ConfigureResponsiveLayout()
    {
        if (joinButton == null || !(joinButton.transform is RectTransform rect))
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.19f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(560f, 104f);
        rect.localScale = Vector3.one;
        PokerButtonTheme.ApplyTo(joinButton);
    }

    private void ConfigureMobileInputs()
    {
        if (nickInput != null)
            nickInput.keyboardType = TouchScreenKeyboardType.Default;
        if (roomIdInput != null)
        {
            roomIdInput.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            roomIdInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        }

        MobileInputFieldUX mobileUx = GetComponent<MobileInputFieldUX>();
        if (mobileUx == null)
            mobileUx = gameObject.AddComponent<MobileInputFieldUX>();
        mobileUx.Configure(250f, nickInput, roomIdInput);
    }

    private void Update()
    {
        Validate();
    }

    public override void OnConnectedToMaster()
    {
        Validate();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Validate();
    }

    private void Validate()
    {
        if (roomIdInput != null)
            roomIdInput.text = roomIdInput.text.ToUpper();

        bool nickOk = nickInput != null && nickInput.text.Trim().Length >= 2;
        bool idOk = roomIdInput != null && roomIdInput.text.Trim().Length >= 4;

        if (joinButton != null)
            joinButton.interactable = nickOk && idOk;
    }

    private void JoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("JoinRoomUI: Photon jeszcze nie jest gotowy.");
            return;
        }

        string nick = nickInput.text.Trim();
        string roomCode = roomIdInput.text.Trim().ToUpper();

        if (nick.Length < 2 || roomCode.Length < 4)
            return;

        PhotonNetwork.NickName = nick;

        PlayerPrefs.SetString("lastNick", nick);
        PlayerPrefs.SetString("lastRoomCode", roomCode);
        PlayerPrefs.Save();

        EnsurePersistentUserId();

        PhotonNetwork.JoinRoom(roomCode);
    }

    public override void OnJoinedRoom()
    {
        bool gameStarted = false;

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GameStartedKey))
        {
            object value = PhotonNetwork.CurrentRoom.CustomProperties[GameStartedKey];

            if (value is bool boolValue)
                gameStarted = boolValue;
        }

        if (gameStarted)
            HotSeatOrientationLock.LockLandscape();

        SceneManager.LoadScene(gameStarted ? gameSceneName : lobbySceneName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"JoinRoom failed: {message} ({returnCode})");
    }

    private void EnsurePersistentUserId()
    {
        string userId = PlayerPrefs.GetString(UserIdPrefsKey, "");

        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = System.Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(UserIdPrefsKey, userId);
            PlayerPrefs.Save();
        }

        if (PhotonNetwork.AuthValues == null)
            PhotonNetwork.AuthValues = new AuthenticationValues();

        PhotonNetwork.AuthValues.UserId = userId;
    }
}
