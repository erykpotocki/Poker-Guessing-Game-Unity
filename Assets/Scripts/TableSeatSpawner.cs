using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TableSeatSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform tableCenter;
    [SerializeField] private GameObject seatPrefab;
    [SerializeField] private AvatarDatabase avatarDatabase;
    [SerializeField] private CardDealTest cardDealTest;

    [Header("Dealer")]
    [SerializeField] private AvatarDatabase krupierAvatarDatabase;

    [Header("Layout")]
    [SerializeField] private float radiusX = 750f;
    [SerializeField] private float radiusY = 350f;

    private const string AvatarKey = "avatarIndex";
    private const string SeatOrderKey = "seatOrderV1";
    private const string GameSeedKey = "gameSeedV1";

    private void Start()
    {
        StartCoroutine(SpawnSeatsWhenReady());
    }

    private IEnumerator SpawnSeatsWhenReady()
    {
        float roomWait = 0f;

        while (!PhotonNetwork.InRoom && roomWait < 10f)
        {
            roomWait += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("TableSeatSpawner: nie udało się wejść do roomu na czas.");
            yield break;
        }

        if (tableCenter == null || seatPrefab == null)
            yield break;

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureSharedTableData();
        }

        List<int> sharedSeatOrder = null;
        int sharedGameSeed = 0;

        float waitTime = 0f;
        while ((!TryGetSharedSeatOrder(out sharedSeatOrder) || !TryGetSharedGameSeed(out sharedGameSeed)) && waitTime < 5f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (sharedSeatOrder == null || sharedSeatOrder.Count == 0)
        {
            Debug.LogError("TableSeatSpawner: nie udało się pobrać wspólnej kolejności miejsc z Room Custom Properties.");
            yield break;
        }

        ClearSpawnedSeats();

        if (cardDealTest != null)
        {
            cardDealTest.ClearSeatOccupants();
            cardDealTest.SetSharedRoundSeed(sharedGameSeed);
        }

        Player[] roomPlayers = PhotonNetwork.PlayerList;
        Dictionary<int, Player> playersByActorNumber = new Dictionary<int, Player>();

        for (int i = 0; i < roomPlayers.Length; i++)
        {
            playersByActorNumber[roomPlayers[i].ActorNumber] = roomPlayers[i];
        }

        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int localIndexInSharedOrder = sharedSeatOrder.IndexOf(localActorNumber);

        if (localIndexInSharedOrder < 0)
        {
            Debug.LogError("TableSeatSpawner: lokalny gracz nie istnieje we wspólnej kolejności miejsc.");
            yield break;
        }

        float[] localSeatAngles = GetLocalSeatAngles(sharedSeatOrder.Count);

        for (int localSeatIndex = 0; localSeatIndex < sharedSeatOrder.Count; localSeatIndex++)
        {
            int sharedIndex = (localIndexInSharedOrder + localSeatIndex) % sharedSeatOrder.Count;
            int actorNumber = sharedSeatOrder[sharedIndex];

            if (playersByActorNumber.TryGetValue(actorNumber, out Player player))
            {
                SpawnPlayerSeat(player, localSeatAngles[localSeatIndex], localSeatIndex);
                continue;
            }

            if (LobbyBotRegistry.TryGetBot(actorNumber, out LobbyBotInfo bot))
            {
                SpawnBotSeat(bot, localSeatAngles[localSeatIndex], localSeatIndex);
                continue;
            }

            Debug.LogWarning("TableSeatSpawner: nie znaleziono gracza ani bota dla ActorNumber = " + actorNumber);
        }

        SpawnDealerSeat(90f);
    }

    private void EnsureSharedTableData()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        bool hasSeatOrder =
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SeatOrderKey, out object seatOrderObj) &&
            seatOrderObj is string seatOrderString &&
            !string.IsNullOrWhiteSpace(seatOrderString);

        bool hasGameSeed =
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSeedKey, out object gameSeedObj) &&
            gameSeedObj is int;

        if (hasSeatOrder && hasGameSeed)
            return;

        Player[] players = PhotonNetwork.PlayerList;
        List<int> actorNumbers = new List<int>();

        for (int i = 0; i < players.Length; i++)
        {
            actorNumbers.Add(players[i].ActorNumber);
        }

        List<LobbyBotInfo> bots = LobbyBotRegistry.GetBots();
        for (int i = 0; i < bots.Count; i++)
        {
            actorNumbers.Add(bots[i].ActorNumber);
        }

        actorNumbers.Sort();

        int sharedGameSeed = PhotonNetwork.ServerTimestamp ^ (actorNumbers.Count * 48611) ^ Guid.NewGuid().GetHashCode();

        System.Random rng = new System.Random(sharedGameSeed);

        for (int i = actorNumbers.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(0, i + 1);

            int temp = actorNumbers[i];
            actorNumbers[i] = actorNumbers[swapIndex];
            actorNumbers[swapIndex] = temp;
        }

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { SeatOrderKey, string.Join(",", actorNumbers) },
            { GameSeedKey, sharedGameSeed }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log("TableSeatSpawner: shared seat order = " + string.Join(" -> ", actorNumbers) + " | seed = " + sharedGameSeed);
    }

    private bool TryGetSharedSeatOrder(out List<int> sharedSeatOrder)
    {
        sharedSeatOrder = null;

        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SeatOrderKey, out object rawValue))
            return false;

        if (rawValue is not string joined || string.IsNullOrWhiteSpace(joined))
            return false;

        string[] parts = joined.Split(',');
        List<int> parsed = new List<int>();

        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out int actorNumber))
            {
                parsed.Add(actorNumber);
            }
        }

        if (parsed.Count == 0)
            return false;

        sharedSeatOrder = parsed;
        return true;
    }

    private bool TryGetSharedGameSeed(out int sharedGameSeed)
    {
        sharedGameSeed = 0;

        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSeedKey, out object rawValue))
            return false;

        if (rawValue is int intValue)
        {
            sharedGameSeed = intValue;
            return true;
        }

        if (rawValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
        {
            sharedGameSeed = parsedValue;
            return true;
        }

        return false;
    }

    private void ClearSpawnedSeats()
    {
        Transform parent = tableCenter.parent;
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            if (!child.name.StartsWith("Seat_"))
                continue;

            Destroy(child.gameObject);
        }
    }

    private float[] GetLocalSeatAngles(int playerCount)
    {
        switch (playerCount)
        {
            case 1:
                return new float[] { -115f };

            case 2:
                return new float[] { -115f, 55f };

            case 3:
                return new float[] { -115f, -175f, 55f };

            case 4:
                return new float[] { -115f, -175f, 125f, -5f };

            case 5:
                return new float[] { -115f, -175f, 125f, 55f, -65f };

            default:
                return new float[] { -115f, -175f, 125f, 55f, -5f, -65f };
        }
    }

    private void SpawnPlayerSeat(Player p, float angleDeg, int seatIndex)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector2 pos = new Vector2(
            Mathf.Cos(angleRad) * radiusX,
            Mathf.Sin(angleRad) * radiusY
        );

        GameObject seatGO = Instantiate(seatPrefab, tableCenter.parent);
        seatGO.name = $"Seat_{seatIndex}_{p.ActorNumber}_{p.NickName}";

        RectTransform seatRT = seatGO.GetComponent<RectTransform>();
        seatRT.anchoredPosition = tableCenter.anchoredPosition + pos;

        SeatUIView view = seatGO.GetComponent<SeatUIView>();
        if (view != null)
        {
            int idx = 0;

            if (p.CustomProperties != null && p.CustomProperties.ContainsKey(AvatarKey))
                idx = (int)p.CustomProperties[AvatarKey];

            Sprite avatar = null;

            if (avatarDatabase != null &&
                avatarDatabase.avatars != null &&
                avatarDatabase.avatars.Length > 0)
            {
                idx = Mathf.Clamp(idx, 0, avatarDatabase.avatars.Length - 1);
                avatar = avatarDatabase.avatars[idx];
            }

            view.Set(p.NickName, avatar);
        }

        if (cardDealTest != null)
        {
            cardDealTest.SetSeatOccupant(seatRT, p.ActorNumber);
        }
    }

    private void SpawnBotSeat(LobbyBotInfo bot, float angleDeg, int seatIndex)
    {
        if (bot == null)
            return;

        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 pos = new Vector2(
            Mathf.Cos(angleRad) * radiusX,
            Mathf.Sin(angleRad) * radiusY
        );

        GameObject seatGO = Instantiate(seatPrefab, tableCenter.parent);
        seatGO.name = $"Seat_{seatIndex}_{bot.ActorNumber}_{bot.Name}";

        RectTransform seatRT = seatGO.GetComponent<RectTransform>();
        seatRT.anchoredPosition = tableCenter.anchoredPosition + pos;

        SeatUIView view = seatGO.GetComponent<SeatUIView>();
        if (view != null)
        {
            Sprite avatar = null;
            if (avatarDatabase != null && avatarDatabase.avatars != null &&
                avatarDatabase.avatars.Length > 0)
            {
                int avatarIndex = Mathf.Clamp(bot.AvatarIndex, 0, avatarDatabase.avatars.Length - 1);
                avatar = avatarDatabase.avatars[avatarIndex];
            }

            view.Set(bot.Name, avatar);
        }

        if (cardDealTest != null)
            cardDealTest.SetSeatOccupant(seatRT, bot.ActorNumber);
    }

    private void SpawnDealerSeat(float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector2 pos = new Vector2(
            Mathf.Cos(angleRad) * radiusX,
            Mathf.Sin(angleRad) * radiusY
        );

        GameObject dealerGO = Instantiate(seatPrefab, tableCenter.parent);
        dealerGO.name = "Seat_Dealer";

        RectTransform dealerRT = dealerGO.GetComponent<RectTransform>();
        dealerRT.anchoredPosition = tableCenter.anchoredPosition + pos;

        SeatUIView view = dealerGO.GetComponent<SeatUIView>();
        if (view != null)
        {
            Sprite dealerAvatar = null;

            if (krupierAvatarDatabase != null &&
                krupierAvatarDatabase.avatars != null &&
                krupierAvatarDatabase.avatars.Length > 0)
            {
                dealerAvatar = krupierAvatarDatabase.avatars[0];
            }

            view.Set("Krupier", dealerAvatar);
        }
    }
}
