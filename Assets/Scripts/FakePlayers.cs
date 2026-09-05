using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyBotInfo
{
    public int ActorNumber;
    public string Name;
    public int AvatarIndex;
}

public static class LobbyBotRegistry
{
    public const string RoomPropertyKey = "testBotsV1";
    public const int FirstBotActorNumber = 10001;

    public static List<LobbyBotInfo> GetBots(Room room = null)
    {
        room ??= PhotonNetwork.CurrentRoom;
        List<LobbyBotInfo> result = new List<LobbyBotInfo>();

        if (room == null || room.CustomProperties == null ||
            !room.CustomProperties.TryGetValue(RoomPropertyKey, out object raw) ||
            raw is not string serialized || string.IsNullOrWhiteSpace(serialized))
        {
            return result;
        }

        string[] entries = serialized.Split(';');
        for (int i = 0; i < entries.Length; i++)
        {
            string[] parts = entries[i].Split('|');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out int actorNumber) ||
                !int.TryParse(parts[2], out int avatarIndex) ||
                actorNumber < FirstBotActorNumber ||
                string.IsNullOrWhiteSpace(parts[1]))
            {
                continue;
            }

            result.Add(new LobbyBotInfo
            {
                ActorNumber = actorNumber,
                Name = parts[1],
                AvatarIndex = Mathf.Max(0, avatarIndex)
            });
        }

        result.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
        return result;
    }

    public static bool TryGetBot(int actorNumber, out LobbyBotInfo bot)
    {
        List<LobbyBotInfo> bots = GetBots();
        for (int i = 0; i < bots.Count; i++)
        {
            if (bots[i].ActorNumber != actorNumber)
                continue;

            bot = bots[i];
            return true;
        }

        bot = null;
        return false;
    }

    public static bool IsBot(int actorNumber) => TryGetBot(actorNumber, out _);

    public static string Serialize(List<LobbyBotInfo> bots)
    {
        if (bots == null || bots.Count == 0)
            return string.Empty;

        List<string> entries = new List<string>(bots.Count);
        for (int i = 0; i < bots.Count; i++)
        {
            LobbyBotInfo bot = bots[i];
            if (bot == null || bot.ActorNumber < FirstBotActorNumber ||
                string.IsNullOrWhiteSpace(bot.Name))
            {
                continue;
            }

            entries.Add(bot.ActorNumber + "|" + bot.Name + "|" + Mathf.Max(0, bot.AvatarIndex));
        }

        return string.Join(";", entries);
    }
}

public class LobbyDebugFakePlayers : MonoBehaviourPunCallbacks
{
    private static readonly string[] BotNames =
    {
        "BOT Molek",
        "BOT Kubix",
        "BOT Gardjew",
        "BOT Emil",
        "BOT Sebek"
    };

    [Header("Legacy UI (kept for scene compatibility)")]
    [SerializeField] private TextMeshProUGUI playersListText;
    [SerializeField] private TextMeshProUGUI playersCountText;

    [Header("Debug bots")]
    [SerializeField] private int maxPlayers = 6;

    private readonly List<LobbyBotInfo> bots = new List<LobbyBotInfo>();
    private Button addBotButton;
    private Button startButton;
    private TMP_Text addBotButtonLabel;
    private LobbyPlayersListUI playersListUI;

    public static int BotCount => LobbyBotRegistry.GetBots().Count;

    private void Start()
    {
        playersListUI = FindFirstObjectByType<LobbyPlayersListUI>();
        SyncBotsFromRoom();
        CreateAddBotButton();
        RefreshUI();
    }

    public override void OnJoinedRoom()
    {
        SyncBotsFromRoom();
        CreateAddBotButton();
        RefreshUI();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null ||
            !propertiesThatChanged.ContainsKey(LobbyBotRegistry.RoomPropertyKey))
        {
            return;
        }

        SyncBotsFromRoom();
        RefreshUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshUI();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshUI();

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (addBotButton == null && PhotonNetwork.IsMasterClient)
            CreateAddBotButton();
        RefreshUI();
    }

    public void AddBot()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        SyncBotsFromRoom();
        int realPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int maximumBots = Mathf.Min(BotNames.Length, Mathf.Max(0, maxPlayers - realPlayers));
        if (bots.Count >= maximumBots)
        {
            RefreshUI();
            return;
        }

        List<string> freeNames = new List<string>(BotNames);
        for (int i = 0; i < bots.Count; i++)
        {
            freeNames.Remove(bots[i].Name);
        }

        string chosenName = freeNames[Random.Range(0, freeNames.Count)];

        bots.Add(new LobbyBotInfo
        {
            ActorNumber = LobbyBotRegistry.FirstBotActorNumber + bots.Count,
            Name = chosenName,
            AvatarIndex = 0
        });

        string serializedBots = LobbyBotRegistry.Serialize(bots);
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { LobbyBotRegistry.RoomPropertyKey, serializedBots }
        });
        // Photon confirms the room property asynchronously. Updating the local
        // cache now makes rapid consecutive taps add separate bots immediately.
        PhotonNetwork.CurrentRoom.CustomProperties[LobbyBotRegistry.RoomPropertyKey] = serializedBots;

        RefreshUI();
    }

    private void SyncBotsFromRoom()
    {
        bots.Clear();
        bots.AddRange(LobbyBotRegistry.GetBots());
    }

    private void CreateAddBotButton()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || addBotButton != null)
            return;

        foreach (Button button in FindObjectsByType<Button>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text.ToUpperInvariant().Contains("ROZPOCZNIJ"))
            {
                startButton = button;
                break;
            }
        }

        if (startButton == null)
            return;

        RectTransform startRect = startButton.transform as RectTransform;
        if (startRect != null)
            startRect.sizeDelta = new Vector2(560f, 104f);

        addBotButton = Instantiate(startButton, startButton.transform.parent);
        addBotButton.name = "AddTestBotButton";
        RectTransform addRect = addBotButton.transform as RectTransform;
        if (addRect != null && startRect != null)
        {
            addRect.anchoredPosition = startRect.anchoredPosition + Vector2.up * 138f;
            addRect.sizeDelta = new Vector2(560f, 104f);
        }

        addBotButtonLabel = addBotButton.GetComponentInChildren<TMP_Text>(true);
        addBotButton.onClick.RemoveAllListeners();
        addBotButton.onClick.AddListener(AddBot);
        PokerButtonTheme.ApplyTo(addBotButton);
    }

    private void RefreshUI()
    {
        if (playersListUI == null)
            playersListUI = FindFirstObjectByType<LobbyPlayersListUI>();

        playersListUI?.Refresh();
        FindFirstObjectByType<LobbyStartPhoton>()?.RefreshStartButton();

        if (playersCountText != null && PhotonNetwork.CurrentRoom != null)
        {
            int total = PhotonNetwork.CurrentRoom.PlayerCount + bots.Count;
            playersCountText.text = $"GRACZE: {total}/{maxPlayers}";
        }

        int maximumBots = 0;
        if (PhotonNetwork.CurrentRoom != null)
        {
            int realPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            maximumBots = Mathf.Min(BotNames.Length, Mathf.Max(0, maxPlayers - realPlayers));
        }

        if (addBotButton != null)
        {
            addBotButton.interactable = PhotonNetwork.IsMasterClient && bots.Count < maximumBots;
        }

        if (addBotButtonLabel != null)
            addBotButtonLabel.text = bots.Count < maximumBots
                ? "DODAJ BOTA"
                : "BOTY DODANE";
    }
}
