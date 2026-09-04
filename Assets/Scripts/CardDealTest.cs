using System.Collections;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class CardDealTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private RectTransform cardsParent;
    [SerializeField] private CardBackDatabase cardBackDatabase;
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private RectTransform tableCenter;
    [SerializeField] private TurnManager turnManager;

    [Header("Dealer")]
    [SerializeField] private RectTransform dealerCardDealPoint;

    [Header("Round start")]
    [SerializeField] private int sharedRoundSeed = 12345;
    [SerializeField] private int forcedStarterPlayerId = -1;

    [Header("Card placement")]
    [SerializeField] private float inwardOffset = 120f;
    [SerializeField] private float multiCardSpread = 34f;

    [Header("Timing")]
    [SerializeField] private float cardHoldAtDealerTime = 2f;
    [SerializeField] private float shuffleDuration = 3f;
    [SerializeField] private float dealDuration = 0.35f;
    [SerializeField] private float delayBetweenDeals = 0.08f;

    [Header("Shuffle look")]
    [SerializeField] private float shuffleSpread = 35f;
    [SerializeField] private float shuffleSpeed = 6f;

    [Header("Card settings")]
    [SerializeField] private int backIndex = 0;

    [Header("Resume / restore")]
    [SerializeField] private bool tryRestoreSnapshotOnStart = true;
    [SerializeField] private float restoreWaitForRoomSeconds = 8f;
    [SerializeField] private float restoreWaitForSeatsSeconds = 5f;

    private const string SeatOrderKey = "seatOrderV1";
    private const string GameSeedKey = "gameSeedV1";

    private const string RoundCardsSnapshotKey = "roundCardsSnapshotV1";
    private const string RoundStarterKey = "roundStarterV1";
    private const string RoundDealerKey = "roundDealerV1";
    private const string RoundSeedKey = "roundSeedV1";

    private bool hasStarted = false;
    private bool useConfiguredRoundData = false;
    private bool restoreAttempted = false;

    private int runtimeStarterPlayerId = -1;
    private int runtimeDealerPlayerId = -1;

    private readonly Dictionary<RectTransform, int> seatOccupants = new Dictionary<RectTransform, int>();
    private readonly Dictionary<string, RectTransform> seatsByName = new Dictionary<string, RectTransform>();
    private readonly List<RectTransform> cachedSeatRects = new List<RectTransform>();

    private readonly Dictionary<int, int> configuredRoundCardCounts = new Dictionary<int, int>();

    private readonly Dictionary<int, List<CardSpriteEntry>> dealtCardsByPlayerId = new Dictionary<int, List<CardSpriteEntry>>();
    private readonly Dictionary<int, List<DealtCardView>> dealtViewsByPlayerId = new Dictionary<int, List<DealtCardView>>();
    private readonly List<CardSpriteEntry> allDealtCardsThisRound = new List<CardSpriteEntry>();
    private readonly List<DealtCardView> allDealtViewsThisRound = new List<DealtCardView>();

    private class PlayerDealInfo
    {
        public Vector2 CardTargetPos;
        public Vector2 InwardDir;
        public int StablePlayerId;
        public int CardCount;
    }

    private class DealTarget
    {
        public int StablePlayerId;
        public Vector2 EndPosition;
    }

    private void Awake()
    {
        CacheSeats();
    }

    private void Start()
    {
        if (tryRestoreSnapshotOnStart)
            StartCoroutine(TryRestoreRoundSnapshotAfterJoin());
    }

    public void StartDealTest()
    {
        if (allDealtCardsThisRound.Count > 0)
        {
            hasStarted = true;
            NotifyTurnManagerCardsReady();
            return;
        }

        if (hasStarted)
            return;

        hasStarted = true;
        useConfiguredRoundData = false;

        StopAllCoroutines();
        StartCoroutine(StartDealOrRestore());
    }

    public void StartConfiguredRoundDeal(Dictionary<int, int> roundCardCountsByPlayerId, int dealerPlayerId, int starterPlayerId, int roundSeed)
    {
        if (roundCardCountsByPlayerId == null || roundCardCountsByPlayerId.Count == 0)
            return;

        StopAllCoroutines();

        hasStarted = true;
        useConfiguredRoundData = true;

        configuredRoundCardCounts.Clear();
        foreach (KeyValuePair<int, int> pair in roundCardCountsByPlayerId)
        {
            if (pair.Key > 0 && pair.Value > 0)
                configuredRoundCardCounts[pair.Key] = pair.Value;
        }

        runtimeDealerPlayerId = dealerPlayerId;
        runtimeStarterPlayerId = starterPlayerId;
        sharedRoundSeed = roundSeed;

        ClearSpawnedCards();
        ClearDealtCardMemory();

        StartCoroutine(DealCurrentRound());
    }

    public void ResetDealTest()
    {
        hasStarted = false;
        useConfiguredRoundData = false;
        restoreAttempted = false;
        runtimeStarterPlayerId = -1;
        runtimeDealerPlayerId = -1;
        configuredRoundCardCounts.Clear();

        ClearSpawnedCards();
        ClearDealtCardMemory();
    }

    public void ClearSeatOccupants()
    {
        seatOccupants.Clear();
    }

    public void SetSeatOccupant(RectTransform seatRect, int stablePlayerId)
    {
        if (seatRect == null || stablePlayerId <= 0)
            return;

        seatOccupants[seatRect] = stablePlayerId;
    }

    public void SetSeatOccupantByName(string seatName, int stablePlayerId)
    {
        CacheSeats();

        if (string.IsNullOrWhiteSpace(seatName) || stablePlayerId <= 0)
            return;

        if (seatsByName.TryGetValue(seatName, out RectTransform seatRect))
            seatOccupants[seatRect] = stablePlayerId;
    }

    public void SetStarterPlayerId(int stablePlayerId)
    {
        runtimeStarterPlayerId = stablePlayerId;
    }

    public void SetSharedRoundSeed(int seed)
    {
        sharedRoundSeed = seed;
    }

    public void ClearDealtCardMemory()
    {
        dealtCardsByPlayerId.Clear();
        dealtViewsByPlayerId.Clear();
        allDealtCardsThisRound.Clear();
        allDealtViewsThisRound.Clear();
    }

    public List<CardSpriteEntry> GetCardsForPlayer(int stablePlayerId)
    {
        if (!dealtCardsByPlayerId.TryGetValue(stablePlayerId, out List<CardSpriteEntry> cards))
            return new List<CardSpriteEntry>();

        return new List<CardSpriteEntry>(cards);
    }

    public List<CardSpriteEntry> GetAllDealtCards()
    {
        return new List<CardSpriteEntry>(allDealtCardsThisRound);
    }

    public List<CardSpriteEntry> GetAllRoundCardsForEvaluation()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                RoundCardsSnapshotKey, out object rawSnapshot) &&
            rawSnapshot is string snapshot && !string.IsNullOrWhiteSpace(snapshot))
        {
            Dictionary<int, List<CardSpriteEntry>> parsed =
                DeserializeRoundSnapshot(snapshot);
            List<CardSpriteEntry> snapshotCards = new List<CardSpriteEntry>();

            foreach (KeyValuePair<int, List<CardSpriteEntry>> pair in parsed)
            {
                if (pair.Value != null)
                    snapshotCards.AddRange(pair.Value);
            }

            if (snapshotCards.Count > 0)
                return snapshotCards;
        }

        return GetAllDealtCards();
    }

    public void RevealAllDealtCards()
    {
        foreach (KeyValuePair<int, List<DealtCardView>> pair in dealtViewsByPlayerId)
        {
            int playerId = pair.Key;
            List<DealtCardView> views = pair.Value;

            if (views == null || views.Count == 0)
                continue;

            dealtCardsByPlayerId.TryGetValue(playerId, out List<CardSpriteEntry> entries);

            List<RectTransform> validRects = new List<RectTransform>();
            Vector2 averagePos = Vector2.zero;
            int validCount = 0;

            for (int i = 0; i < views.Count; i++)
            {
                DealtCardView view = views[i];
                if (view == null)
                    continue;

                RectTransform rect = view.GetComponent<RectTransform>();
                if (rect == null)
                    continue;

                if (entries != null && i < entries.Count)
                {
                    CardSpriteEntry entry = entries[i];
                    if (entry != null && entry.sprite != null)
                        view.ShowFront(entry.sprite);
                }

                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();

                validRects.Add(rect);
                averagePos += rect.anchoredPosition;
                validCount++;
            }

            if (validCount == 0)
                continue;

            averagePos /= validCount;

            float spacing = 90f;
            float totalWidth = (validCount - 1) * spacing;
            float startX = averagePos.x - (totalWidth * 0.5f);

            for (int i = 0; i < validRects.Count; i++)
            {
                Vector2 pos = validRects[i].anchoredPosition;
                pos.x = startX + (i * spacing);
                pos.y = averagePos.y;
                validRects[i].anchoredPosition = pos;
            }
        }
    }

    private IEnumerator StartDealOrRestore()
    {
        if (TryRestoreRoundSnapshotFromRoom())
        {
            NotifyTurnManagerCardsReady();
            yield break;
        }

        float wait = 0f;
        while (HasRoomSnapshot() && wait < restoreWaitForSeatsSeconds)
        {
            if (TryRestoreRoundSnapshotFromRoom())
            {
                NotifyTurnManagerCardsReady();
                yield break;
            }

            wait += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (HasRoomSnapshot())
            yield break;

        yield return StartCoroutine(DealCurrentRound());
    }

    private IEnumerator TryRestoreRoundSnapshotAfterJoin()
    {
        if (restoreAttempted)
            yield break;

        restoreAttempted = true;

        float roomWait = 0f;
        while (!PhotonNetwork.InRoom && roomWait < restoreWaitForRoomSeconds)
        {
            roomWait += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.InRoom)
            yield break;

        float seatsWait = 0f;
        while (seatOccupants.Count == 0 && seatsWait < restoreWaitForSeatsSeconds)
        {
            seatsWait += Time.deltaTime;
            yield return null;
        }

        TryRestoreRoundSnapshotFromRoom();
    }

    private bool HasRoomSnapshot()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoundCardsSnapshotKey, out object rawSnapshot))
            return false;

        return rawSnapshot is string snapshotString && !string.IsNullOrWhiteSpace(snapshotString);
    }

    public bool TryRestoreRoundSnapshotFromRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        if (allDealtCardsThisRound.Count > 0)
            return true;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoundCardsSnapshotKey, out object rawSnapshot))
            return false;

        if (rawSnapshot is not string snapshotString || string.IsNullOrWhiteSpace(snapshotString))
            return false;

        TryReadIntProp(RoundStarterKey, out runtimeStarterPlayerId);
        TryReadIntProp(RoundDealerKey, out runtimeDealerPlayerId);
        TryReadIntProp(RoundSeedKey, out sharedRoundSeed);

        Dictionary<int, List<CardSpriteEntry>> parsedCards = DeserializeRoundSnapshot(snapshotString);
        if (parsedCards.Count == 0)
            return false;

        ClearSpawnedCards();
        ClearDealtCardMemory();

        Dictionary<int, int> roundCardCounts = new Dictionary<int, int>();
        foreach (KeyValuePair<int, List<CardSpriteEntry>> pair in parsedCards)
        {
            if (pair.Key > 0 && pair.Value != null && pair.Value.Count > 0)
                roundCardCounts[pair.Key] = pair.Value.Count;
        }

        List<DealTarget> orderedTargets = GetOrderedDealTargets(roundCardCounts);
        if (orderedTargets.Count == 0)
            return false;

        Dictionary<int, int> nextCardIndexByPlayerId = new Dictionary<int, int>();

        for (int i = 0; i < orderedTargets.Count; i++)
        {
            DealTarget target = orderedTargets[i];
            if (!parsedCards.TryGetValue(target.StablePlayerId, out List<CardSpriteEntry> playerCards))
                continue;

            if (!nextCardIndexByPlayerId.TryGetValue(target.StablePlayerId, out int playerCardIndex))
                playerCardIndex = 0;

            if (playerCardIndex < 0 || playerCardIndex >= playerCards.Count)
                continue;

            CardSpriteEntry entry = playerCards[playerCardIndex];
            nextCardIndexByPlayerId[target.StablePlayerId] = playerCardIndex + 1;

            CardView spawnedCard = CreateBackCard(target.EndPosition);
            if (spawnedCard == null)
                continue;

            RectTransform rect = spawnedCard.GetComponent<RectTransform>();
            rect.anchoredPosition = target.EndPosition;

            DealtCardView dealtCardView = rect.GetComponent<DealtCardView>();
            if (dealtCardView == null)
            {
                Destroy(spawnedCard.gameObject);
                continue;
            }

            RegisterDealtCard(target.StablePlayerId, entry, dealtCardView);

            bool isLocalPlayersCard =
                PhotonNetwork.LocalPlayer != null &&
                target.StablePlayerId == PhotonNetwork.LocalPlayer.ActorNumber;

            if (isLocalPlayersCard && entry != null && entry.sprite != null)
                dealtCardView.ShowFront(entry.sprite);
        }

        RefreshLocalHandLayout();

        hasStarted = true;
        useConfiguredRoundData = false;
        return true;
    }

    private IEnumerator DealCurrentRound()
    {
        if (!useConfiguredRoundData)
            TryApplySharedGameSeed();

        if (cardPrefab == null)
        {
            Debug.LogError("CardDealTest: cardPrefab nie jest przypisany.");
            yield break;
        }

        if (cardsParent == null)
        {
            Debug.LogError("CardDealTest: cardsParent nie jest przypisany.");
            yield break;
        }

        if (cardBackDatabase == null)
        {
            Debug.LogError("CardDealTest: cardBackDatabase nie jest przypisany.");
            yield break;
        }

        if (cardDatabase == null)
        {
            Debug.LogError("CardDealTest: cardDatabase nie jest przypisany.");
            yield break;
        }

        if (cardDatabase.cards == null || cardDatabase.cards.Length == 0)
        {
            Debug.LogError("CardDealTest: CardDatabase nie ma żadnych kart.");
            yield break;
        }

        if (tableCenter == null)
        {
            Debug.LogError("CardDealTest: tableCenter nie jest przypisany.");
            yield break;
        }

        if (dealerCardDealPoint == null)
        {
            Debug.LogError("CardDealTest: dealerCardDealPoint nie jest przypisany.");
            yield break;
        }

        Dictionary<int, int> roundCardCounts = BuildCurrentRoundCardCounts();
        List<DealTarget> orderedTargets = GetOrderedDealTargets(roundCardCounts);

        float waitForSeatsTime = 0f;
        while (orderedTargets.Count == 0 && waitForSeatsTime < 5f)
        {
            waitForSeatsTime += Time.deltaTime;
            yield return null;

            roundCardCounts = BuildCurrentRoundCardCounts();
            orderedTargets = GetOrderedDealTargets(roundCardCounts);
        }

        if (orderedTargets.Count == 0)
        {
            Debug.LogError("CardDealTest: nie znaleziono żadnych kart do rozdania.");
            yield break;
        }

        if (orderedTargets.Count > cardDatabase.cards.Length)
        {
            Debug.LogError("CardDealTest: liczba wszystkich rozdawanych kart jest większa niż liczba kart w CardDatabase.");
            yield break;
        }

        List<CardSpriteEntry> shuffledCards = GetShuffledCards();
        Vector2 dealerPos = GetLocalPointInParent(cardsParent, dealerCardDealPoint);

        CardView mainCard = CreateBackCard(dealerPos);
        CardView extraCardA = CreateBackCard(dealerPos);
        CardView extraCardB = CreateBackCard(dealerPos);

        if (mainCard == null || extraCardA == null || extraCardB == null)
            yield break;

        RectTransform mainRect = mainCard.GetComponent<RectTransform>();
        RectTransform rectA = extraCardA.GetComponent<RectTransform>();
        RectTransform rectB = extraCardB.GetComponent<RectTransform>();

        yield return new WaitForSeconds(cardHoldAtDealerTime);

        float elapsed = 0f;

        while (elapsed < shuffleDuration)
        {
            elapsed += Time.deltaTime;

            float wave = Mathf.Sin(elapsed * shuffleSpeed);
            float offsetA = wave * shuffleSpread;
            float offsetB = Mathf.Sin((elapsed * shuffleSpeed) + 2.1f) * shuffleSpread;
            float offsetC = Mathf.Sin((elapsed * shuffleSpeed) + 4.2f) * shuffleSpread;

            mainRect.anchoredPosition = dealerPos + new Vector2(offsetA, 0f);
            rectA.anchoredPosition = dealerPos + new Vector2(offsetB, -8f);
            rectB.anchoredPosition = dealerPos + new Vector2(offsetC, 8f);

            yield return null;
        }

        mainRect.anchoredPosition = dealerPos;

        Destroy(extraCardA.gameObject);
        Destroy(extraCardB.gameObject);

        yield return new WaitForSeconds(0.15f);

        for (int i = 0; i < orderedTargets.Count; i++)
        {
            RectTransform cardRect;

            if (i == 0)
            {
                cardRect = mainRect;
                cardRect.anchoredPosition = dealerPos;
            }
            else
            {
                CardView nextCard = CreateBackCard(dealerPos);
                if (nextCard == null)
                    yield break;

                cardRect = nextCard.GetComponent<RectTransform>();
                cardRect.anchoredPosition = dealerPos;
            }

            yield return StartCoroutine(AnimateCardTo(cardRect, dealerPos, orderedTargets[i].EndPosition));
            ShowFrontForDealIndex(cardRect, shuffledCards, i, orderedTargets[i].StablePlayerId);
            yield return new WaitForSeconds(delayBetweenDeals);
        }

        PersistCurrentRoundSnapshotToRoom();
        NotifyTurnManagerCardsReady();
    }

    private void NotifyTurnManagerCardsReady()
    {
        if (turnManager == null)
            return;

        if (!turnManager.IsInitialized)
            turnManager.StartTurnSystem();
        else
            turnManager.NotifyRoundDealFinished(runtimeStarterPlayerId);
    }

    private void PersistCurrentRoundSnapshotToRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        string snapshot = SerializeCurrentRoundSnapshot();
        if (string.IsNullOrWhiteSpace(snapshot))
            return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { RoundCardsSnapshotKey, snapshot },
            { RoundStarterKey, runtimeStarterPlayerId },
            { RoundDealerKey, runtimeDealerPlayerId },
            { RoundSeedKey, sharedRoundSeed }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private string SerializeCurrentRoundSnapshot()
    {
        if (dealtCardsByPlayerId.Count == 0)
            return string.Empty;

        List<int> playerIds = new List<int>(dealtCardsByPlayerId.Keys);
        playerIds.Sort();

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < playerIds.Count; i++)
        {
            int playerId = playerIds[i];

            if (!dealtCardsByPlayerId.TryGetValue(playerId, out List<CardSpriteEntry> playerCards) ||
                playerCards == null || playerCards.Count == 0)
            {
                continue;
            }

            if (builder.Length > 0)
                builder.Append(";");

            builder.Append(playerId);
            builder.Append("=");

            for (int j = 0; j < playerCards.Count; j++)
            {
                CardSpriteEntry entry = playerCards[j];
                if (entry == null)
                    continue;

                if (j > 0)
                    builder.Append(",");

                builder.Append(SerializeRank(entry.rank.ToString()));
                builder.Append("~");
                builder.Append(SerializeSuit(entry.suit.ToString()));
            }
        }

        return builder.ToString();
    }

    private Dictionary<int, List<CardSpriteEntry>> DeserializeRoundSnapshot(string snapshot)
    {
        Dictionary<int, List<CardSpriteEntry>> result = new Dictionary<int, List<CardSpriteEntry>>();

        if (string.IsNullOrWhiteSpace(snapshot))
            return result;

        string[] playerChunks = snapshot.Split(';');
        for (int i = 0; i < playerChunks.Length; i++)
        {
            string chunk = playerChunks[i];
            if (string.IsNullOrWhiteSpace(chunk))
                continue;

            string[] split = chunk.Split('=');
            if (split.Length != 2)
                continue;

            if (!int.TryParse(split[0], out int playerId))
                continue;

            string cardsPart = split[1];
            if (string.IsNullOrWhiteSpace(cardsPart))
                continue;

            string[] cardTokens = cardsPart.Split(',');
            List<CardSpriteEntry> cards = new List<CardSpriteEntry>();

            for (int j = 0; j < cardTokens.Length; j++)
            {
                string token = cardTokens[j];
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                string[] cardSplit = token.Split('~');
                if (cardSplit.Length != 2)
                    continue;

                string rank = cardSplit[0];
                string suit = cardSplit[1];

                if (TryFindCardEntry(rank, suit, out CardSpriteEntry foundEntry))
                    cards.Add(foundEntry);
            }

            if (cards.Count > 0)
                result[playerId] = cards;
        }

        return result;
    }

    private bool TryFindCardEntry(string serializedRank, string serializedSuit, out CardSpriteEntry foundEntry)
    {
        foundEntry = null;

        if (cardDatabase == null || cardDatabase.cards == null)
            return false;

        for (int i = 0; i < cardDatabase.cards.Length; i++)
        {
            CardSpriteEntry entry = cardDatabase.cards[i];
            if (entry == null)
                continue;

            string rank = SerializeRank(entry.rank.ToString());
            string suit = SerializeSuit(entry.suit.ToString());

            if (rank == serializedRank && suit == serializedSuit)
            {
                foundEntry = entry;
                return true;
            }
        }

        return false;
    }

    private string SerializeRank(string rawRank)
    {
        if (string.IsNullOrWhiteSpace(rawRank))
            return string.Empty;

        string value = rawRank.Trim().ToUpper();

        if (value == "9" || value == "NINE")
            return "9";

        if (value == "10" || value == "TEN")
            return "10";

        if (value == "J" || value == "JACK")
            return "J";

        if (value == "Q" || value == "D" || value == "QUEEN" || value == "DAMA")
            return "Q";

        if (value == "K" || value == "KING")
            return "K";

        if (value == "A" || value == "ACE")
            return "A";

        return value;
    }

    private string SerializeSuit(string rawSuit)
    {
        if (string.IsNullOrWhiteSpace(rawSuit))
            return string.Empty;

        string value = rawSuit.Trim().ToLower();

        if (value.Contains("♦") || value.Contains("karo") || value.Contains("diamond") || value.Contains("dzwonek"))
            return "♦";

        if (value.Contains("♥") || value.Contains("kier") || value.Contains("heart") || value.Contains("serce"))
            return "♥";

        if (value.Contains("♣") || value.Contains("trefl") || value.Contains("club") || value.Contains("żołądź") || value.Contains("zoladz"))
            return "♣";

        if (value.Contains("♠") || value.Contains("pik") || value.Contains("spade") || value.Contains("wino"))
            return "♠";

        return value;
    }

    private bool TryReadIntProp(string key, out int value)
    {
        value = 0;

        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object rawValue))
            return false;

        if (rawValue is int intValue)
        {
            value = intValue;
            return true;
        }

        if (rawValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
        {
            value = parsedValue;
            return true;
        }

        return false;
    }

    private Dictionary<int, int> BuildCurrentRoundCardCounts()
    {
        Dictionary<int, int> result = new Dictionary<int, int>();

        if (useConfiguredRoundData && configuredRoundCardCounts.Count > 0)
        {
            foreach (KeyValuePair<int, int> pair in configuredRoundCardCounts)
            {
                if (pair.Key > 0 && pair.Value > 0)
                    result[pair.Key] = pair.Value;
            }

            return result;
        }

        CacheSeats();

        for (int i = 0; i < cachedSeatRects.Count; i++)
        {
            RectTransform seatRect = cachedSeatRects[i];
            if (seatRect == null)
                continue;

            if (!seatOccupants.TryGetValue(seatRect, out int stablePlayerId))
                continue;

            if (stablePlayerId <= 0)
                continue;

            result[stablePlayerId] = 1;
        }

        return result;
    }

    private void TryApplySharedGameSeed()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSeedKey, out object rawValue))
            return;

        if (rawValue is int intValue)
        {
            sharedRoundSeed = intValue;
            return;
        }

        if (rawValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
            sharedRoundSeed = parsedValue;
    }

    private List<CardSpriteEntry> GetShuffledCards()
    {
        List<CardSpriteEntry> shuffled = new List<CardSpriteEntry>(cardDatabase.cards);
        System.Random rng = new System.Random(sharedRoundSeed ^ 918273);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(0, i + 1);

            CardSpriteEntry temp = shuffled[i];
            shuffled[i] = shuffled[swapIndex];
            shuffled[swapIndex] = temp;
        }

        return shuffled;
    }

    private void ShowFrontForDealIndex(RectTransform cardRect, List<CardSpriteEntry> shuffledCards, int dealIndex, int stablePlayerId)
    {
        if (cardRect == null || shuffledCards == null || shuffledCards.Count == 0)
            return;

        if (dealIndex < 0 || dealIndex >= shuffledCards.Count)
            return;

        DealtCardView dealtCardView = cardRect.GetComponent<DealtCardView>();
        if (dealtCardView == null)
            return;

        CardSpriteEntry entry = shuffledCards[dealIndex];
        if (entry == null)
            return;

        RegisterDealtCard(stablePlayerId, entry, dealtCardView);

        bool isLocalPlayersCard =
            PhotonNetwork.LocalPlayer != null &&
            stablePlayerId == PhotonNetwork.LocalPlayer.ActorNumber;

        if (isLocalPlayersCard && entry.sprite != null)
        {
            dealtCardView.ShowFront(entry.sprite);
            RefreshLocalHandLayout();
        }
    }

    private void RegisterDealtCard(int stablePlayerId, CardSpriteEntry entry, DealtCardView dealtCardView)
    {
        if (stablePlayerId <= 0 || entry == null || dealtCardView == null)
            return;

        if (!dealtCardsByPlayerId.TryGetValue(stablePlayerId, out List<CardSpriteEntry> cards))
        {
            cards = new List<CardSpriteEntry>();
            dealtCardsByPlayerId.Add(stablePlayerId, cards);
        }

        if (!dealtViewsByPlayerId.TryGetValue(stablePlayerId, out List<DealtCardView> views))
        {
            views = new List<DealtCardView>();
            dealtViewsByPlayerId.Add(stablePlayerId, views);
        }

        cards.Add(entry);
        views.Add(dealtCardView);

        allDealtCardsThisRound.Add(entry);
        allDealtViewsThisRound.Add(dealtCardView);
    }

    private List<DealTarget> GetOrderedDealTargets(Dictionary<int, int> roundCardCounts)
    {
        CacheSeats();

        List<PlayerDealInfo> players = new List<PlayerDealInfo>();
        Vector2 tableCenterPos = GetLocalPointInParent(cardsParent, tableCenter);

        for (int i = 0; i < cachedSeatRects.Count; i++)
        {
            RectTransform seatRect = cachedSeatRects[i];
            if (seatRect == null)
                continue;

            if (!seatOccupants.TryGetValue(seatRect, out int stablePlayerId))
                continue;

            if (stablePlayerId <= 0)
                continue;

            if (!roundCardCounts.TryGetValue(stablePlayerId, out int cardCount))
                continue;

            if (cardCount <= 0)
                continue;

            Transform cardTargetTransform = seatRect.Find("CardTarget");
            if (cardTargetTransform == null)
                continue;

            RectTransform cardTargetRect = cardTargetTransform as RectTransform;
            if (cardTargetRect == null)
                continue;

            Vector2 seatPos = GetLocalPointInParent(cardsParent, seatRect);
            Vector2 cardTargetPos = GetLocalPointInParent(cardsParent, cardTargetRect);
            Vector2 inwardDir = (tableCenterPos - seatPos).normalized;

            PlayerDealInfo info = new PlayerDealInfo
            {
                CardTargetPos = cardTargetPos,
                InwardDir = inwardDir,
                StablePlayerId = stablePlayerId,
                CardCount = cardCount
            };

            players.Add(info);
        }

        if (players.Count == 0)
            return new List<DealTarget>();

        List<int> sharedSeatOrder = null;
        TryGetSharedSeatOrder(out sharedSeatOrder);

        if (sharedSeatOrder != null && sharedSeatOrder.Count > 0)
        {
            players.Sort((a, b) =>
            {
                int aIndex = GetSharedSeatIndex(sharedSeatOrder, a.StablePlayerId);
                int bIndex = GetSharedSeatIndex(sharedSeatOrder, b.StablePlayerId);
                return aIndex.CompareTo(bIndex);
            });
        }
        else
        {
            players.Sort((a, b) => a.StablePlayerId.CompareTo(b.StablePlayerId));
        }

        int startIndex = 0;

        if (useConfiguredRoundData)
        {
            startIndex = FindFirstReceiverIndexAfterDealer(players, sharedSeatOrder);
        }
        else
        {
            int starterPlayerId = GetStarterPlayerId(players);
            int foundIndex = players.FindIndex(p => p.StablePlayerId == starterPlayerId);
            startIndex = foundIndex >= 0 ? foundIndex : 0;
        }

        List<PlayerDealInfo> rotatedPlayers = new List<PlayerDealInfo>(players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            int index = (startIndex + i) % players.Count;
            rotatedPlayers.Add(players[index]);
        }

        List<DealTarget> orderedTargets = new List<DealTarget>();
        Dictionary<int, int> dealtSoFarByPlayerId = new Dictionary<int, int>();

        int maxCardsForAnyPlayer = 0;
        for (int i = 0; i < rotatedPlayers.Count; i++)
        {
            if (rotatedPlayers[i].CardCount > maxCardsForAnyPlayer)
                maxCardsForAnyPlayer = rotatedPlayers[i].CardCount;
        }

        for (int pass = 0; pass < maxCardsForAnyPlayer; pass++)
        {
            for (int i = 0; i < rotatedPlayers.Count; i++)
            {
                PlayerDealInfo player = rotatedPlayers[i];

                if (pass >= player.CardCount)
                    continue;

                if (!dealtSoFarByPlayerId.TryGetValue(player.StablePlayerId, out int cardIndexForPlayer))
                    cardIndexForPlayer = 0;

                Vector2 endPos = CalculateCardEndPosition(player, cardIndexForPlayer);

                orderedTargets.Add(new DealTarget
                {
                    StablePlayerId = player.StablePlayerId,
                    EndPosition = endPos
                });

                dealtSoFarByPlayerId[player.StablePlayerId] = cardIndexForPlayer + 1;
            }
        }

        return orderedTargets;
    }

    private Vector2 CalculateCardEndPosition(PlayerDealInfo player, int cardIndexForPlayer)
    {
        Vector2 lateralDir = new Vector2(-player.InwardDir.y, player.InwardDir.x);
        float centeredOffset = (cardIndexForPlayer - ((player.CardCount - 1) * 0.5f)) * multiCardSpread;

        return player.CardTargetPos + (player.InwardDir * inwardOffset) + (lateralDir * centeredOffset);
    }

    private int FindFirstReceiverIndexAfterDealer(List<PlayerDealInfo> players, List<int> sharedSeatOrder)
    {
        if (players == null || players.Count == 0)
            return 0;

        if (runtimeDealerPlayerId <= 0)
            return 0;

        if (sharedSeatOrder != null && sharedSeatOrder.Count > 0)
        {
            int dealerIndexInSharedOrder = sharedSeatOrder.IndexOf(runtimeDealerPlayerId);
            if (dealerIndexInSharedOrder < 0)
                dealerIndexInSharedOrder = -1;

            for (int step = 1; step <= sharedSeatOrder.Count; step++)
            {
                int sharedIndex = (dealerIndexInSharedOrder + step + sharedSeatOrder.Count) % sharedSeatOrder.Count;
                int actorNumber = sharedSeatOrder[sharedIndex];

                int foundPlayerIndex = players.FindIndex(p => p.StablePlayerId == actorNumber);
                if (foundPlayerIndex >= 0)
                    return foundPlayerIndex;
            }
        }

        int dealerIndexInPlayers = players.FindIndex(p => p.StablePlayerId == runtimeDealerPlayerId);
        if (dealerIndexInPlayers >= 0)
            return (dealerIndexInPlayers + 1) % players.Count;

        return 0;
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
                parsed.Add(actorNumber);
        }

        if (parsed.Count == 0)
            return false;

        sharedSeatOrder = parsed;
        return true;
    }

    private int GetSharedSeatIndex(List<int> sharedSeatOrder, int stablePlayerId)
    {
        if (sharedSeatOrder == null)
            return int.MaxValue;

        int index = sharedSeatOrder.IndexOf(stablePlayerId);
        if (index < 0)
            return int.MaxValue;

        return index;
    }

    private int GetStarterPlayerId(List<PlayerDealInfo> sortedTargets)
    {
        if (runtimeStarterPlayerId > 0)
            return runtimeStarterPlayerId;

        if (forcedStarterPlayerId > 0)
            return forcedStarterPlayerId;

        System.Random rng = new System.Random(sharedRoundSeed);
        int randomIndex = rng.Next(0, sortedTargets.Count);
        return sortedTargets[randomIndex].StablePlayerId;
    }

    private void CacheSeats()
    {
        seatsByName.Clear();
        cachedSeatRects.Clear();

        if (cardsParent == null)
            return;

        RectTransform[] allRects = cardsParent.GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rect in allRects)
        {
            if (!rect.name.StartsWith("Seat_"))
                continue;

            if (rect.name.Contains("Dealer"))
                continue;

            cachedSeatRects.Add(rect);

            if (!seatsByName.ContainsKey(rect.name))
                seatsByName.Add(rect.name, rect);
        }
    }

    private IEnumerator AnimateCardTo(RectTransform cardRect, Vector2 startPos, Vector2 endPos)
    {
        float time = 0f;

        while (time < dealDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / dealDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cardRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        cardRect.anchoredPosition = endPos;
    }

    private CardView CreateBackCard(Vector2 anchoredPos)
    {
        CardView spawnedCard = Instantiate(cardPrefab, cardsParent);
        spawnedCard.SetMultiplayerBack(cardBackDatabase, backIndex);

        RectTransform rect = spawnedCard.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("CardDealTest: prefab karty nie ma RectTransform.");
            Destroy(spawnedCard.gameObject);
            return null;
        }

        rect.localScale = Vector3.one;
        rect.anchoredPosition = anchoredPos;

        return spawnedCard;
    }

    private void ClearSpawnedCards()
    {
        DealtCardView[] dealtCards = cardsParent.GetComponentsInChildren<DealtCardView>(true);

        for (int i = 0; i < dealtCards.Length; i++)
        {
            if (dealtCards[i] == null)
                continue;

            Destroy(dealtCards[i].gameObject);
        }
    }

    private Vector2 GetLocalPointInParent(RectTransform parent, RectTransform target)
    {
        Camera cam = null;
        Canvas canvas = parent.GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(cam, target.position),
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private void RefreshLocalHandLayout()
    {
        if (PhotonNetwork.LocalPlayer == null)
            return;

        int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;

        if (!dealtViewsByPlayerId.TryGetValue(localPlayerId, out List<DealtCardView> localCards))
            return;

        if (localCards == null || localCards.Count == 0)
            return;

        float spacing = 90f;
        float centerX = 0f;
        int validCount = 0;

        for (int i = 0; i < localCards.Count; i++)
        {
            if (localCards[i] == null)
                continue;

            RectTransform rect = localCards[i].GetComponent<RectTransform>();
            if (rect == null)
                continue;

            centerX += rect.anchoredPosition.x;
            validCount++;
        }

        if (validCount == 0)
            return;

        centerX /= validCount;

        float totalWidth = (localCards.Count - 1) * spacing;
        float startX = centerX - (totalWidth * 0.5f);

        for (int i = 0; i < localCards.Count; i++)
        {
            if (localCards[i] == null)
                continue;

            RectTransform cardRect = localCards[i].GetComponent<RectTransform>();
            if (cardRect == null)
                continue;

            Vector2 pos = cardRect.anchoredPosition;
            pos.x = startX + (i * spacing);
            cardRect.anchoredPosition = pos;
        }
    }
}
