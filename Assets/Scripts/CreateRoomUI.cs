using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreateRoomUI : MonoBehaviourPunCallbacks
{
    public TMP_InputField nickInput;
    public TMP_Text roomCodeText;
    public Button createButton;

    private const string UserIdPrefsKey = "PhotonUserId";

    private void Start()
    {
        ConfigureResponsiveLayout();
        ConfigureMobileInput();
        if (roomCodeText != null)
            roomCodeText.text = "";

        if (createButton != null)
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(CreateRoom);
            createButton.interactable = PhotonNetwork.IsConnectedAndReady;
        }

        ValidateCreateButton();
    }

    private void ConfigureResponsiveLayout()
    {
        SetCentered(createButton != null ? createButton.transform as RectTransform : null, 0.255f, 560f, 104f);
        if (createButton != null) PokerButtonTheme.ApplyTo(createButton);
    }

    private void ConfigureMobileInput()
    {
        if (nickInput == null)
            return;

        nickInput.keyboardType = TouchScreenKeyboardType.Default;
        MobileInputFieldUX mobileUx = GetComponent<MobileInputFieldUX>();
        if (mobileUx == null)
            mobileUx = gameObject.AddComponent<MobileInputFieldUX>();
        mobileUx.Configure(0f, nickInput);
    }

    private static void SetCentered(RectTransform rect, float anchorY, float width, float height)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private void Update()
    {
        ValidateCreateButton();
    }

    public override void OnConnectedToMaster()
    {
        ValidateCreateButton();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        ValidateCreateButton();
    }

    private void ValidateCreateButton()
    {
        if (createButton == null || nickInput == null)
            return;

        bool nickOk = nickInput.text.Trim().Length >= 2;
        createButton.interactable = PhotonNetwork.IsConnectedAndReady && nickOk;
    }

    private void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("CreateRoomUI: Photon jeszcze nie jest gotowy.");
            return;
        }

        string nick = nickInput.text.Trim();
        if (nick.Length < 2)
            return;

        PhotonNetwork.NickName = nick;
        PlayerPrefs.SetString("lastNick", nick);
        PlayerPrefs.Save();
        EnsurePersistentUserId();

        string code = GenerateCode(4);
        PlayerPrefs.SetString("lastRoomCode", code);
        PlayerPrefs.Save();

        if (roomCodeText != null)
            roomCodeText.text = code;

        var opts = new RoomOptions
        {
            MaxPlayers = 6,
            IsVisible = false,
            IsOpen = true,
            PlayerTtl = 300000,
            EmptyRoomTtl = 300000
        };

        PhotonNetwork.CreateRoom(code, opts);
    }

    public override void OnCreatedRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (roomCodeText != null)
            roomCodeText.text = "Błąd, spróbuj ponownie";

        Debug.LogWarning($"CreateRoom failed: {message} ({returnCode})");
        ValidateCreateButton();
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

    private string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var arr = new char[length];
        for (int i = 0; i < length; i++)
            arr[i] = chars[Random.Range(0, chars.Length)];
        return new string(arr);
    }
}
