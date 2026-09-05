using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour, IOnEventCallback
{
    [System.Serializable]
    private class CardCountTimingEntry
    {
        [Min(2)] public int totalCards = 2;
        [Min(0f)] public float checkScreenSeconds = 3f;
        [Min(0f)] public float resultScreenSeconds = 2f;
    }

    [Header("Setup")]
    [SerializeField] private bool autoStartOnSceneLoad = true;
    [SerializeField] private HandRankPanelUI handRankPanelUI;
    [SerializeField] private RoundLogUI roundLogUI;
    [SerializeField] private CardDealTest cardDealTest;
    [SerializeField] private GameEndSequenceController gameEndSequenceController;
    [SerializeField] private AvatarDatabase avatarDatabase;

    [Header("Round resolution")]
    [SerializeField] private float fallbackCheckScreenSeconds = 4f;
    [SerializeField] private float fallbackResultScreenSeconds = 3f;

    [SerializeField] private List<CardCountTimingEntry> cardCountTimings = new List<CardCountTimingEntry>()
    {
        new CardCountTimingEntry { totalCards = 2,  checkScreenSeconds = 3.00f, resultScreenSeconds = 2.00f },
        new CardCountTimingEntry { totalCards = 3,  checkScreenSeconds = 3.25f, resultScreenSeconds = 2.25f },
        new CardCountTimingEntry { totalCards = 4,  checkScreenSeconds = 3.50f, resultScreenSeconds = 2.50f },
        new CardCountTimingEntry { totalCards = 5,  checkScreenSeconds = 3.75f, resultScreenSeconds = 2.75f },
        new CardCountTimingEntry { totalCards = 6,  checkScreenSeconds = 4.00f, resultScreenSeconds = 3.00f },
        new CardCountTimingEntry { totalCards = 7,  checkScreenSeconds = 4.50f, resultScreenSeconds = 3.25f },
        new CardCountTimingEntry { totalCards = 8,  checkScreenSeconds = 5.00f, resultScreenSeconds = 3.50f },
        new CardCountTimingEntry { totalCards = 9,  checkScreenSeconds = 5.50f, resultScreenSeconds = 3.75f },
        new CardCountTimingEntry { totalCards = 10, checkScreenSeconds = 6.00f, resultScreenSeconds = 4.00f },
        new CardCountTimingEntry { totalCards = 11, checkScreenSeconds = 6.50f, resultScreenSeconds = 4.25f },
        new CardCountTimingEntry { totalCards = 12, checkScreenSeconds = 7.00f, resultScreenSeconds = 4.50f },
        new CardCountTimingEntry { totalCards = 13, checkScreenSeconds = 7.50f, resultScreenSeconds = 4.75f },
        new CardCountTimingEntry { totalCards = 14, checkScreenSeconds = 8.00f, resultScreenSeconds = 5.00f },
        new CardCountTimingEntry { totalCards = 15, checkScreenSeconds = 8.50f, resultScreenSeconds = 5.25f },
        new CardCountTimingEntry { totalCards = 16, checkScreenSeconds = 9.00f, resultScreenSeconds = 5.50f },
        new CardCountTimingEntry { totalCards = 17, checkScreenSeconds = 9.50f, resultScreenSeconds = 5.75f },
        new CardCountTimingEntry { totalCards = 18, checkScreenSeconds = 10.00f, resultScreenSeconds = 6.00f }
    };

    [Header("Result colors")]
    [SerializeField] private Color declaredRankHighlightColor = new Color(1f, 0.82f, 0.2f, 1f);

    [Header("Debug")]
    [SerializeField] private bool logTurnFlow = true;
    [SerializeField] private float waitForRoomDataTimeout = 5f;
    [SerializeField] private float turnDurationSeconds = 60f;
    [SerializeField] private float overtimeKickSeconds = 120f;
    [SerializeField] private float disconnectedRemovalSeconds = 120f;

    private const string SeatOrderKey = "seatOrderV1";
    private const string GameSeedKey = "gameSeedV1";
    private const string AvatarIndexPropertyKey = "avatarIndex";

    private const string TurnStateActiveOrderKey = "turnActiveOrderV1";
    private const string TurnStatePenaltyStagesKey = "turnPenaltyStagesV1";
    private const string TurnStateCurrentActorKey = "turnCurrentActorV1";
    private const string TurnStateStarterActorKey = "turnStarterActorV1";
    private const string TurnStateRoundNumberKey = "turnRoundNumberV1";
    private const string TurnStateDeclaredRankKey = "turnDeclaredRankV1";
    private const string TurnStateHasDeclarationKey = "turnHasDeclarationV1";
    private const string TurnStateBidDisplayKey = "turnBidDisplayV1";
    private const string TurnStateLastDeclarerKey = "turnLastDeclarerV1";
    private const string TurnStateCheckingPlayerKey = "turnCheckingPlayerV1";
    private const string TurnStateWaitingKey = "turnWaitingV1";
    private const string TurnStateTransitionKey = "turnTransitionV1";
    private const string TurnStateGameOverKey = "turnGameOverV1";
    private const string GameEndedKey = "gameEnded";

    private const byte RaiseChosenEventCode = 71;
    private const byte CheckChosenEventCode = 72;

    private static readonly string[] RankOrder = { "9", "10", "J", "Q", "K", "A" };

    private readonly List<int> activePlayerOrder = new List<int>();
    private readonly Dictionary<int, SeatUIView> seatViewsByActorNumber = new Dictionary<int, SeatUIView>();
    private readonly Dictionary<int, int> playerPenaltyStageByActorNumber = new Dictionary<int, int>();

    private float currentTurnTimeLeft = 0f;
    private readonly HashSet<int> disconnectedActorNumbers = new HashSet<int>();

    private bool isInitialized = false;
    private bool hasDeclarationThisRound = false;
    private bool isRoundWaitingForResolution = false;
    private bool isRoundTransitionInProgress = false;
    private bool isHandPanelEventsBound = false;
    private bool isGameOver = false;
    private Coroutine beginnerBotTurnRoutine;

    private int currentTurnIndex = -1;
    private int starterPlayerId = -1;
    private int sharedGameSeed = 0;
    private int currentRoundNumber = 1;

    private int lastDeclarerActorNumber = -1;
    private int checkingPlayerActorNumber = -1;

    private string currentDeclaredRankText = null;
    private string currentBidDisplayText = string.Empty;

    public event Action<int> OnActivePlayerChanged;
    public event Action<string> OnCurrentBidDisplayChanged;

    public bool IsInitialized => isInitialized;
    public int CurrentPlayerActorNumber => GetCurrentPlayerActorNumber();
    public int StarterPlayerActorNumber => starterPlayerId;
    public float TurnDurationSeconds => turnDurationSeconds;
    public float CurrentTurnTimeLeft => currentTurnTimeLeft;
    public bool HasDeclarationThisRound => hasDeclarationThisRound;
    public string CurrentDeclaredRankText => currentDeclaredRankText;
    public int CurrentRoundNumber => currentRoundNumber;
    public string CurrentBidDisplayText => currentBidDisplayText;
    public bool IsResolutionLocked => isRoundWaitingForResolution || isRoundTransitionInProgress || isGameOver;

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        UnbindHandRankPanelEvents();
    }

    private void Start()
    {
        disconnectedRemovalSeconds = Mathf.Max(disconnectedRemovalSeconds, 300f);
        TryBindHandRankPanel();
        TryResolveRoundLogUI();
        TryResolveCardDealTest();
        RefreshLocalHandPanelState();

        if (autoStartOnSceneLoad)
        {
            StartCoroutine(InitializeTurnSystem());
        }
    }

    private void Update()
    {
        if (!isInitialized || isGameOver)
            return;

        RefreshDisconnectedPlayersFromRoomState();

        if (isRoundWaitingForResolution || isRoundTransitionInProgress)
            return;

        int currentActorNumber = GetCurrentPlayerActorNumber();
        if (currentActorNumber <= 0)
            return;

        currentTurnTimeLeft -= Time.deltaTime;

        if (disconnectedActorNumbers.Contains(currentActorNumber))
        {
            if (currentTurnTimeLeft <= 0f)
            {
                StartCoroutine(ResolveDisconnectedTurnRemoval(currentActorNumber));
            }

            return;
        }

        if (currentTurnTimeLeft <= -overtimeKickSeconds)
        {
            StartCoroutine(ResolveTurnTimeoutLoss(currentActorNumber));
        }
    }

    public void StartTurnSystem()
    {
        if (isInitialized)
            return;

        StartCoroutine(InitializeTurnSystem());
    }

    public void NotifyRoundDealFinished(int nextStarterActorNumber)
    {
        if (!isInitialized)
            return;

        if (isGameOver)
            return;

        int starterIndex = activePlayerOrder.IndexOf(nextStarterActorNumber);
        if (starterIndex < 0)
            starterIndex = 0;

        if (activePlayerOrder.Count == 0)
        {
            currentTurnIndex = -1;
            isRoundTransitionInProgress = false;
            RefreshLocalHandPanelState();
            SaveTurnStateToRoom();
            return;
        }

        currentTurnIndex = starterIndex;
        starterPlayerId = activePlayerOrder[currentTurnIndex];

        isRoundTransitionInProgress = false;
        isRoundWaitingForResolution = false;

        ResetCurrentTurnTimer();
        AddRoundStarterToLog(starterPlayerId);
        NotifyActivePlayerChanged();
        SaveTurnStateToRoom();
    }

    private IEnumerator InitializeTurnSystem()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("TurnManager: gracz nie jest w pokoju.");
            yield break;
        }

        List<int> sharedSeatOrder = null;
        float waitTime = 0f;

        while ((!TryGetSharedSeatOrder(out sharedSeatOrder) || !TryGetSharedGameSeed(out sharedGameSeed)) &&
               waitTime < waitForRoomDataTimeout)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (sharedSeatOrder == null || sharedSeatOrder.Count == 0)
        {
            Debug.LogError("TurnManager: nie udało się pobrać wspólnej kolejności graczy.");
            yield break;
        }

        CacheSeatViews();

        activePlayerOrder.Clear();
        playerPenaltyStageByActorNumber.Clear();
        disconnectedActorNumbers.Clear();

        for (int i = 0; i < sharedSeatOrder.Count; i++)
        {
            int actorNumber = sharedSeatOrder[i];

            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.Players != null &&
                (PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber) ||
                 LobbyBotRegistry.IsBot(actorNumber)))
            {
                activePlayerOrder.Add(actorNumber);
                playerPenaltyStageByActorNumber[actorNumber] = 0;
            }
        }

        if (activePlayerOrder.Count == 0)
        {
            Debug.LogError("TurnManager: brak aktywnych graczy do kolejki tur.");
            yield break;
        }

        RefreshDisconnectedPlayersFromRoomState();

        bool restoredFromRoom = TryRestoreTurnStateFromRoom();

        if (!restoredFromRoom)
        {
            System.Random rng = new System.Random(sharedGameSeed ^ 1357911);
            int randomIndex = rng.Next(0, activePlayerOrder.Count);

            currentTurnIndex = randomIndex;
            starterPlayerId = activePlayerOrder[currentTurnIndex];

            currentDeclaredRankText = null;
            currentBidDisplayText = string.Empty;
            hasDeclarationThisRound = false;
            isRoundWaitingForResolution = false;
            isRoundTransitionInProgress = false;
            isGameOver = false;
            currentRoundNumber = 1;
            lastDeclarerActorNumber = -1;
            checkingPlayerActorNumber = -1;
        }

        if (currentTurnIndex < 0 || currentTurnIndex >= activePlayerOrder.Count)
            currentTurnIndex = 0;

        if (starterPlayerId <= 0 || !activePlayerOrder.Contains(starterPlayerId))
            starterPlayerId = activePlayerOrder[currentTurnIndex];

        isInitialized = true;

        ResetCurrentTurnTimer();
        SetCurrentBidDisplayText(currentBidDisplayText);

        if (!restoredFromRoom)
        {
            ResetRoundLogForNewGame();
            AddRoundStarterToLog(starterPlayerId);
            SaveTurnStateToRoom();
        }

        if (logTurnFlow)
        {
            Debug.Log(
                "TurnManager: start kolejki tur | starter = " + starterPlayerId +
                " | active order = " + string.Join(" -> ", activePlayerOrder) +
                " | restored = " + restoredFromRoom
            );
        }

        NotifyActivePlayerChanged();
    }

    public void DebugNextTurn()
    {
        AdvanceToNextTurn();
    }

    public void AdvanceToNextTurn()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("TurnManager: system tur nie jest jeszcze zainicjalizowany.");
            return;
        }

        if (isRoundWaitingForResolution || isRoundTransitionInProgress || isGameOver)
        {
            return;
        }

        if (activePlayerOrder.Count == 0)
        {
            Debug.LogWarning("TurnManager: brak graczy w kolejce tur.");
            return;
        }

        int nextIndex = GetWrappedTurnIndex(currentTurnIndex + 1);
        if (nextIndex < 0)
            return;

        currentTurnIndex = nextIndex;
        ResetCurrentTurnTimer();
        NotifyActivePlayerChanged();
        SaveTurnStateToRoom();
    }

    public bool IsLocalPlayersTurn()
    {
        if (!isInitialized || PhotonNetwork.LocalPlayer == null)
            return false;

        return GetCurrentPlayerActorNumber() == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public int GetCurrentPlayerActorNumber()
    {
        if (!isInitialized)
            return -1;

        if (currentTurnIndex < 0 || currentTurnIndex >= activePlayerOrder.Count)
            return -1;

        return activePlayerOrder[currentTurnIndex];
    }

    public List<int> GetActivePlayersSnapshot()
    {
        return new List<int>(activePlayerOrder);
    }

    public void StartNewRoundFromPlayer(int actorNumber)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("TurnManager: nie można ustawić nowej rundy przed inicjalizacją.");
            return;
        }

        int index = activePlayerOrder.IndexOf(actorNumber);
        if (index < 0)
        {
            Debug.LogWarning("TurnManager: actorNumber nie istnieje w kolejce tur: " + actorNumber);
            return;
        }

        currentRoundNumber++;

        currentTurnIndex = index;
        starterPlayerId = activePlayerOrder[currentTurnIndex];
        currentDeclaredRankText = null;
        hasDeclarationThisRound = false;
        isRoundWaitingForResolution = false;
        isRoundTransitionInProgress = false;
        lastDeclarerActorNumber = -1;
        checkingPlayerActorNumber = -1;

        ResetCurrentTurnTimer();
        SetCurrentBidDisplayText(string.Empty);
        AddRoundHeaderToLog();
        AddRoundStarterToLog(starterPlayerId);

        if (logTurnFlow)
        {
            Debug.Log("TurnManager: nowa runda od gracza = " + actorNumber);
        }

        NotifyActivePlayerChanged();
        SaveTurnStateToRoom();
    }

    public void RemovePlayerFromTurnOrder(int actorNumber)
    {
        RemovePlayerFromTurnOrderInternal(actorNumber, true);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (!isInitialized)
            return;

        if (photonEvent == null)
            return;

        if (photonEvent.Code == RaiseChosenEventCode)
        {
            HandleNetworkRaiseEvent(photonEvent.CustomData);
        }
        else if (photonEvent.Code == CheckChosenEventCode)
        {
            HandleNetworkCheckEvent(photonEvent.CustomData);
        }
    }

    private void HandleNetworkRaiseEvent(object customData)
    {
        if (customData is not object[] data || data.Length < 2)
            return;

        if (data[0] is not int actorNumber)
            return;

        string selectedRankText = data[1] as string;
        if (string.IsNullOrEmpty(selectedRankText))
            return;

        int expectedActor = GetCurrentPlayerActorNumber();
        if (actorNumber != expectedActor)
        {
            Debug.LogWarning(
                "TurnManager: odebrano Przebij od nieaktywnego gracza. Oczekiwany = " +
                expectedActor + ", odebrany = " + actorNumber
            );
            return;
        }

        currentDeclaredRankText = selectedRankText;
        hasDeclarationThisRound = true;
        isRoundWaitingForResolution = false;
        isRoundTransitionInProgress = false;

        lastDeclarerActorNumber = actorNumber;
        checkingPlayerActorNumber = -1;

        SetCurrentBidDisplayText(selectedRankText);
        AddRaiseToLog(actorNumber, selectedRankText);

        if (logTurnFlow)
        {
            Debug.Log(
                "TurnManager: Przebij od gracza = " + actorNumber +
                " | układ = " + selectedRankText
            );
        }

        AdvanceToNextTurn();
    }

    private void HandleNetworkCheckEvent(object customData)
    {
        if (customData is not object[] data || data.Length < 1)
            return;

        if (data[0] is not int actorNumber)
            return;

        int expectedActor = GetCurrentPlayerActorNumber();
        if (actorNumber != expectedActor)
        {
            Debug.LogWarning(
                "TurnManager: odebrano Sprawdzam od nieaktywnego gracza. Oczekiwany = " +
                expectedActor + ", odebrany = " + actorNumber
            );
            return;
        }

        checkingPlayerActorNumber = actorNumber;
        isRoundWaitingForResolution = true;

        SetCurrentBidDisplayText("Sprawdzam");
        AddCheckToLog(actorNumber);

        if (logTurnFlow)
        {
            Debug.Log("TurnManager: Sprawdzam od gracza = " + actorNumber);
        }

        RefreshLocalHandPanelState();
        SaveTurnStateToRoom();

        if (!isRoundTransitionInProgress)
        {
            StartCoroutine(ResolveCheckAndStartNextRound());
        }
    }

    private IEnumerator ResolveCheckAndStartNextRound()
    {
        if (isRoundTransitionInProgress)
            yield break;

        isRoundTransitionInProgress = true;
        RefreshLocalHandPanelState();
        SaveTurnStateToRoom();

        TryResolveCardDealTest();

        int totalCardsInRound = GetCurrentRoundTotalCardCount();
        float checkScreenSeconds = GetCheckScreenSeconds(totalCardsInRound);
        float resultScreenSeconds = GetResultScreenSeconds(totalCardsInRound);

        if (cardDealTest != null)
        {
            cardDealTest.RevealAllDealtCards();
        }

        yield return new WaitForSeconds(checkScreenSeconds);

        bool declaredExists = EvaluateDeclaredRankExists(currentDeclaredRankText);
        int loserActorNumber = declaredExists ? checkingPlayerActorNumber : lastDeclarerActorNumber;

        string resultDisplayText = BuildResultDisplayText(loserActorNumber, declaredExists);
        string resultChatText = BuildResultChatText(loserActorNumber, declaredExists);

        SetCurrentBidDisplayText(resultDisplayText);
        AddSystemLog(resultChatText);

        List<int> activeOrderBeforeResolution = new List<int>(activePlayerOrder);

        bool eliminated = ApplyLossToPlayer(loserActorNumber, out int nextCardCount);
        if (eliminated)
        {
            AddSystemLog("<b>" + GetPlayerDisplayName(loserActorNumber) + "</b> odpada z gry");
            RemovePlayerFromTurnOrderInternal(loserActorNumber, false);
        }
        else
        {
            AddSystemLog("<b>" + GetPlayerDisplayName(loserActorNumber) + "</b> ma teraz " + nextCardCount + " " + GetCardWord(nextCardCount));
        }

        SaveTurnStateToRoom();

        yield return new WaitForSeconds(resultScreenSeconds);

        if (activePlayerOrder.Count <= 1)
        {
            HandleGameOver();
            yield break;
        }

        int nextStarterActorNumber = loserActorNumber;
        if (!activePlayerOrder.Contains(nextStarterActorNumber))
        {
            nextStarterActorNumber = GetNextActivePlayerAfterSnapshot(activeOrderBeforeResolution, loserActorNumber);
        }

        currentRoundNumber++;
        currentDeclaredRankText = null;
        hasDeclarationThisRound = false;
        isRoundWaitingForResolution = false;
        isRoundTransitionInProgress = false;
        lastDeclarerActorNumber = -1;
        checkingPlayerActorNumber = -1;

        SetCurrentBidDisplayText(string.Empty);
        AddRoundHeaderToLog();
        SaveTurnStateToRoom();

        Dictionary<int, int> nextRoundCardCounts = BuildRoundCardCountMap();
        int nextRoundSeed = ComputeNextRoundSeed(currentRoundNumber, loserActorNumber);

        if (cardDealTest != null)
        {
            cardDealTest.StartConfiguredRoundDeal(
                nextRoundCardCounts,
                loserActorNumber,
                nextStarterActorNumber,
                nextRoundSeed
            );
        }
        else
        {
            NotifyRoundDealFinished(nextStarterActorNumber);
        }
    }

    private IEnumerator ResolveTurnTimeoutLoss(int actorNumber)
    {
        if (isRoundTransitionInProgress || isRoundWaitingForResolution || isGameOver)
            yield break;

        if (actorNumber != GetCurrentPlayerActorNumber())
            yield break;

        isRoundTransitionInProgress = true;
        RefreshLocalHandPanelState();
        SaveTurnStateToRoom();

        string playerName = GetPlayerDisplayName(actorNumber);

        SetCurrentBidDisplayText(
            "<align=\"center\"><b>" + playerName + " przekroczył limit czasu</b>\n<size=70%>Dostaje karną kartę</size></align>"
        );

        AddSystemLog("<b>" + playerName + "</b> przekroczył limit czasu i dostaje karną kartę");

        yield return new WaitForSeconds(3f);

        List<int> activeOrderBeforeResolution = new List<int>(activePlayerOrder);

        bool eliminated = ApplyLossToPlayer(actorNumber, out int nextCardCount);
        if (eliminated)
        {
            AddSystemLog("<b>" + playerName + "</b> odpada z gry");
            RemovePlayerFromTurnOrderInternal(actorNumber, false);
        }
        else
        {
            AddSystemLog("<b>" + playerName + "</b> ma teraz " + nextCardCount + " " + GetCardWord(nextCardCount));
        }

        SaveTurnStateToRoom();

        yield return null;

        if (activePlayerOrder.Count <= 1)
        {
            HandleGameOver();
            yield break;
        }

        int nextStarterActorNumber = actorNumber;
        if (!activePlayerOrder.Contains(nextStarterActorNumber))
        {
            nextStarterActorNumber = GetNextActivePlayerAfterSnapshot(activeOrderBeforeResolution, actorNumber);
        }

        currentRoundNumber++;
        currentDeclaredRankText = null;
        hasDeclarationThisRound = false;
        isRoundWaitingForResolution = false;
        lastDeclarerActorNumber = -1;
        checkingPlayerActorNumber = -1;

        SetCurrentBidDisplayText(string.Empty);
        AddRoundHeaderToLog();
        SaveTurnStateToRoom();

        Dictionary<int, int> nextRoundCardCounts = BuildRoundCardCountMap();
        int nextRoundSeed = ComputeNextRoundSeed(currentRoundNumber, actorNumber);

        if (cardDealTest != null)
        {
            cardDealTest.StartConfiguredRoundDeal(
                nextRoundCardCounts,
                actorNumber,
                nextStarterActorNumber,
                nextRoundSeed
            );
        }
        else
        {
            NotifyRoundDealFinished(nextStarterActorNumber);
        }
    }

    private IEnumerator ResolveDisconnectedTurnRemoval(int actorNumber)
    {
        if (isRoundTransitionInProgress || isRoundWaitingForResolution || isGameOver)
            yield break;

        if (actorNumber != GetCurrentPlayerActorNumber())
            yield break;

        if (!disconnectedActorNumbers.Contains(actorNumber))
            yield break;

        isRoundTransitionInProgress = true;
        RefreshLocalHandPanelState();
        SaveTurnStateToRoom();

        string playerName = GetPlayerDisplayName(actorNumber);
        string previousBidDisplay = currentBidDisplayText;

        SetCurrentBidDisplayText(
            "<align=\"center\"><b>" + playerName + " opuścił grę</b>\n<size=70%>Zostaje usunięty z rozgrywki</size></align>"
        );

        AddSystemLog("<b>" + playerName + "</b> nie wrócił na swoją turę i zostaje usunięty z gry");

        yield return new WaitForSeconds(2f);

        RemovePlayerFromTurnOrderInternal(actorNumber, false);

        if (activePlayerOrder.Count <= 1)
        {
            HandleGameOver();
            yield break;
        }

        isRoundTransitionInProgress = false;
        SetCurrentBidDisplayText(previousBidDisplay);
        ResetCurrentTurnTimer();
        NotifyActivePlayerChanged();
        SaveTurnStateToRoom();
    }

    private string BuildResultDisplayText(int loserActorNumber, bool declaredExists)
    {
        string playerName = GetPlayerDisplayName(loserActorNumber);
        string declaredText = GetHighlightedDeclaredRankText();

        string resultLine = declaredExists
            ? "Układ " + declaredText + " jest na stole"
            : "Układu " + declaredText + " nie ma na stole";

        bool isFinalDuel = activePlayerOrder.Count == 2;
        bool loserWillBeEliminatedNow =
            playerPenaltyStageByActorNumber.TryGetValue(loserActorNumber, out int stageBeforeLoss) &&
            stageBeforeLoss >= 4;

        if (isFinalDuel && loserWillBeEliminatedNow)
        {
            return "<align=\"center\"><b>" + resultLine + "</b></align>";
        }

        return "<align=\"center\"><b>" + playerName + " przegrywa</b>\n<size=70%>" + resultLine + "</size></align>";
    }

    private string BuildResultChatText(int loserActorNumber, bool declaredExists)
    {
        string playerName = GetPlayerDisplayName(loserActorNumber);
        string declaredText = GetHighlightedDeclaredRankText();

        if (declaredExists)
            return playerName + " przegrywa — układ " + declaredText + " jest na stole";

        return playerName + " przegrywa — układu " + declaredText + " nie ma na stole";
    }

    private string GetHighlightedDeclaredRankText()
    {
        if (string.IsNullOrWhiteSpace(currentDeclaredRankText))
            return "tego układu";

        return WrapWithColor(currentDeclaredRankText, declaredRankHighlightColor);
    }

    private string WrapWithColor(string text, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        return "<color=#" + hex + ">" + text + "</color>";
    }

    private void NotifyActivePlayerChanged()
    {
        int actorNumber = GetCurrentPlayerActorNumber();

        RefreshActiveHighlight(actorNumber);
        RefreshLocalHandPanelState();
        ScheduleBeginnerBotTurn(actorNumber);

        if (logTurnFlow)
        {
            Debug.Log("TurnManager: aktywny gracz = " + actorNumber);
        }

        OnActivePlayerChanged?.Invoke(actorNumber);
    }

    private void ScheduleBeginnerBotTurn(int actorNumber)
    {
        if (beginnerBotTurnRoutine != null)
        {
            StopCoroutine(beginnerBotTurnRoutine);
            beginnerBotTurnRoutine = null;
        }

        if (!PhotonNetwork.IsMasterClient || !LobbyBotRegistry.IsBot(actorNumber) ||
            isRoundWaitingForResolution || isRoundTransitionInProgress || isGameOver)
        {
            return;
        }

        beginnerBotTurnRoutine = StartCoroutine(PlayBeginnerBotTurn(actorNumber));
    }

    private IEnumerator PlayBeginnerBotTurn(int actorNumber)
    {
        TryResolveCardDealTest();

        float cardsWait = 0f;
        while (cardsWait < 15f && GetCurrentPlayerActorNumber() == actorNumber &&
               cardDealTest != null && cardDealTest.GetCardsForPlayer(actorNumber).Count == 0)
        {
            cardsWait += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.9f);

        if (!PhotonNetwork.IsMasterClient || GetCurrentPlayerActorNumber() != actorNumber ||
            isRoundWaitingForResolution || isRoundTransitionInProgress || isGameOver)
        {
            beginnerBotTurnRoutine = null;
            yield break;
        }

        string declaration = ChooseBeginnerBotDeclaration(actorNumber);
        beginnerBotTurnRoutine = null;

        object[] data = new object[] { actorNumber, declaration };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(RaiseChosenEventCode, data, options, SendOptions.SendReliable);
    }

    private string ChooseBeginnerBotDeclaration(int actorNumber)
    {
        string truthfulId = GetStrongestBeginnerBotHandId(actorNumber);
        int truthfulIndex = HandRankCatalog.GetIndex(truthfulId);

        if (!hasDeclarationThisRound || string.IsNullOrWhiteSpace(currentDeclaredRankText))
            return HandRankCatalog.GetDisplayName(truthfulId);

        string currentId = GetHandIdFromOptionText(currentDeclaredRankText);
        int currentIndex = HandRankCatalog.GetIndex(currentId);

        if (truthfulIndex > currentIndex)
            return HandRankCatalog.GetDisplayName(truthfulId);

        List<string> allIds = HandRankCatalog.GetAllIds();
        int nextIndex = Mathf.Clamp(currentIndex + 1, 0, allIds.Count - 1);
        return HandRankCatalog.GetDisplayName(allIds[nextIndex]);
    }

    private string GetStrongestBeginnerBotHandId(int actorNumber)
    {
        TryResolveCardDealTest();
        List<CardSpriteEntry> cards = cardDealTest != null
            ? cardDealTest.GetCardsForPlayer(actorNumber)
            : new List<CardSpriteEntry>();

        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < cards.Count; i++)
        {
            CardSpriteEntry card = cards[i];
            if (card == null)
                continue;

            string rank = NormalizeRank(card.rank.ToString());
            if (string.IsNullOrEmpty(rank))
                continue;

            counts[rank] = counts.TryGetValue(rank, out int count) ? count + 1 : 1;
        }

        for (int requiredCount = 3; requiredCount >= 1; requiredCount--)
        {
            for (int i = RankOrder.Length - 1; i >= 0; i--)
            {
                string rank = RankOrder[i];
                if (!counts.TryGetValue(rank, out int count) || count < requiredCount)
                    continue;

                if (requiredCount == 3)
                    return "TRIPS_" + rank;
                if (requiredCount == 2)
                    return "PAIR_" + rank;
                return "HIGH_" + rank;
            }
        }

        return "HIGH_9";
    }

    private void SetCurrentBidDisplayText(string value)
    {
        currentBidDisplayText = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        OnCurrentBidDisplayChanged?.Invoke(currentBidDisplayText);
    }

    private void TryBindHandRankPanel()
    {
        if (handRankPanelUI == null)
        {
            handRankPanelUI = FindFirstObjectByType<HandRankPanelUI>(FindObjectsInactive.Include);
        }

        if (handRankPanelUI == null)
        {
            Debug.LogWarning("TurnManager: nie znaleziono HandRankPanelUI.");
            return;
        }

        if (isHandPanelEventsBound)
            return;

        handRankPanelUI.OnRaiseChosen += HandleLocalRaiseChosen;
        handRankPanelUI.OnCheckChosen += HandleLocalCheckChosen;
        isHandPanelEventsBound = true;

        if (logTurnFlow)
        {
            Debug.Log("TurnManager: podpięto HandRankPanelUI.");
        }
    }

    private void UnbindHandRankPanelEvents()
    {
        if (handRankPanelUI == null || !isHandPanelEventsBound)
            return;

        handRankPanelUI.OnRaiseChosen -= HandleLocalRaiseChosen;
        handRankPanelUI.OnCheckChosen -= HandleLocalCheckChosen;
        isHandPanelEventsBound = false;
    }

    private void HandleLocalRaiseChosen(string selectedRankText)
    {
        if (!IsLocalPlayersTurn())
        {
            Debug.LogWarning("TurnManager: lokalny gracz próbował zagrać poza swoją turą.");
            return;
        }

        object[] data = new object[]
        {
            PhotonNetwork.LocalPlayer.ActorNumber,
            selectedRankText
        };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(RaiseChosenEventCode, data, options, SendOptions.SendReliable);
    }

    private void HandleLocalCheckChosen()
    {
        if (!IsLocalPlayersTurn())
        {
            Debug.LogWarning("TurnManager: lokalny gracz próbował użyć Sprawdzam poza swoją turą.");
            return;
        }

        object[] data = new object[]
        {
            PhotonNetwork.LocalPlayer.ActorNumber
        };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(CheckChosenEventCode, data, options, SendOptions.SendReliable);
    }

    private void RefreshLocalHandPanelState()
    {
        TryBindHandRankPanel();

        if (handRankPanelUI == null)
            return;

        if (!isInitialized || isRoundWaitingForResolution || isRoundTransitionInProgress || isGameOver)
        {
            handRankPanelUI.SetPanelDimmed(true);
            SetHandRankPanelButtonsInteractable(false);
            return;
        }

        if (!IsLocalPlayersTurn())
        {
            handRankPanelUI.SetPanelDimmed(true);
            SetHandRankPanelButtonsInteractable(false);
            return;
        }

        handRankPanelUI.SetPanelDimmed(false);

        if (hasDeclarationThisRound)
            handRankPanelUI.SetNormalTurnState();
        else
            handRankPanelUI.SetFirstMoveState();
    }

    private void SetHandRankPanelButtonsInteractable(bool value)
    {
        if (handRankPanelUI == null)
            return;

        Button[] buttons = handRankPanelUI.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            buttons[i].interactable = value;
        }
    }

    private void TryResolveRoundLogUI()
    {
        if (roundLogUI != null)
            return;

        roundLogUI = FindFirstObjectByType<RoundLogUI>(FindObjectsInactive.Include);
    }

    private void TryResolveCardDealTest()
    {
        if (cardDealTest != null)
            return;

        cardDealTest = FindFirstObjectByType<CardDealTest>(FindObjectsInactive.Include);
    }

    private void ResetRoundLogForNewGame()
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        roundLogUI.ClearLog();
        roundLogUI.AddSystemMessage("Start");
        AddRoundHeaderToLog();
    }

    private void AddRoundHeaderToLog()
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        roundLogUI.AddRoundHeader(currentRoundNumber);
    }

    private void AddRoundStarterToLog(int actorNumber)
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        roundLogUI.AddSystemMessage("Rundę rozpoczyna: <b>" + GetPlayerDisplayName(actorNumber) + "</b>");
    }

    private void AddRaiseToLog(int actorNumber, string selectedRankText)
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        roundLogUI.AddPlayerRaise(GetPlayerDisplayName(actorNumber), selectedRankText);
    }

    private void AddCheckToLog(int actorNumber)
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        roundLogUI.AddPlayerCheck(GetPlayerDisplayName(actorNumber));
    }

    private void AddSystemLog(string text)
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        roundLogUI.AddSystemMessage(text);
    }

    private string GetPlayerDisplayName(int actorNumber)
    {
        if (LobbyBotRegistry.TryGetBot(actorNumber, out LobbyBotInfo bot))
            return bot.Name;

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.Players != null &&
            PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out Player player) &&
            player != null)
        {
            if (!string.IsNullOrWhiteSpace(player.NickName))
                return player.NickName;

            return "Gracz " + actorNumber;
        }

        return "Gracz " + actorNumber;
    }

    private Sprite GetPlayerAvatarSprite(int actorNumber)
    {
        if (avatarDatabase == null || avatarDatabase.avatars == null || avatarDatabase.avatars.Length == 0)
            return null;

        if (LobbyBotRegistry.TryGetBot(actorNumber, out LobbyBotInfo bot))
        {
            int botAvatarIndex = Mathf.Clamp(bot.AvatarIndex, 0, avatarDatabase.avatars.Length - 1);
            return avatarDatabase.avatars[botAvatarIndex];
        }

        if (PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.CurrentRoom.Players == null ||
            !PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out Player player) ||
            player == null ||
            player.CustomProperties == null)
        {
            return null;
        }

        if (!player.CustomProperties.TryGetValue(AvatarIndexPropertyKey, out object rawAvatarIndex))
            return null;

        int avatarIndex = -1;

        if (rawAvatarIndex is int intIndex)
            avatarIndex = intIndex;
        else if (rawAvatarIndex is byte byteIndex)
            avatarIndex = byteIndex;
        else if (rawAvatarIndex is string stringIndex && int.TryParse(stringIndex, out int parsedIndex))
            avatarIndex = parsedIndex;

        if (avatarIndex < 0 || avatarIndex >= avatarDatabase.avatars.Length)
            return null;

        return avatarDatabase.avatars[avatarIndex];
    }

    private bool ApplyLossToPlayer(int actorNumber, out int nextCardCount)
    {
        nextCardCount = 0;

        if (!playerPenaltyStageByActorNumber.TryGetValue(actorNumber, out int stage))
        {
            stage = 0;
        }

        stage++;
        playerPenaltyStageByActorNumber[actorNumber] = stage;

        nextCardCount = GetCardCountForStage(stage);
        return stage >= 5;
    }

    private int GetCardCountForStage(int stage)
    {
        switch (stage)
        {
            case 0: return 1;
            case 1: return 2;
            case 2: return 3;
            case 3: return 2;
            case 4: return 1;
            default: return 0;
        }
    }

    private string GetCardWord(int count)
    {
        if (count == 1)
            return "kartę";

        if (count >= 2 && count <= 4)
            return "karty";

        return "kart";
    }

    private int GetCurrentRoundTotalCardCount()
    {
        int totalCards = 0;

        for (int i = 0; i < activePlayerOrder.Count; i++)
        {
            int actorNumber = activePlayerOrder[i];

            if (!playerPenaltyStageByActorNumber.TryGetValue(actorNumber, out int stage))
                stage = 0;

            int cardCount = GetCardCountForStage(stage);

            if (cardCount > 0)
                totalCards += cardCount;
        }

        return totalCards;
    }

    private CardCountTimingEntry GetTimingEntryForTotalCards(int totalCards)
    {
        for (int i = 0; i < cardCountTimings.Count; i++)
        {
            CardCountTimingEntry entry = cardCountTimings[i];

            if (entry != null && entry.totalCards == totalCards)
                return entry;
        }

        return null;
    }

    private float GetCheckScreenSeconds(int totalCards)
    {
        CardCountTimingEntry entry = GetTimingEntryForTotalCards(totalCards);
        return entry != null ? entry.checkScreenSeconds : fallbackCheckScreenSeconds;
    }

    private float GetResultScreenSeconds(int totalCards)
    {
        CardCountTimingEntry entry = GetTimingEntryForTotalCards(totalCards);
        return entry != null ? entry.resultScreenSeconds : fallbackResultScreenSeconds;
    }

    private Dictionary<int, int> BuildRoundCardCountMap()
    {
        Dictionary<int, int> result = new Dictionary<int, int>();

        for (int i = 0; i < activePlayerOrder.Count; i++)
        {
            int actorNumber = activePlayerOrder[i];

            if (!playerPenaltyStageByActorNumber.TryGetValue(actorNumber, out int stage))
            {
                stage = 0;
                playerPenaltyStageByActorNumber[actorNumber] = stage;
            }

            int cardCount = GetCardCountForStage(stage);
            if (cardCount > 0)
            {
                result[actorNumber] = cardCount;
            }
        }

        return result;
    }

    private int ComputeNextRoundSeed(int roundNumber, int loserActorNumber)
    {
        int seed = sharedGameSeed;
        seed ^= roundNumber * 104729;
        seed ^= loserActorNumber * 7919;
        seed ^= 918273;
        return seed;
    }

    private int GetNextActivePlayerAfterSnapshot(List<int> snapshot, int actorNumber)
    {
        if (activePlayerOrder.Count == 0)
            return -1;

        if (snapshot == null || snapshot.Count == 0)
            return activePlayerOrder[0];

        int startIndex = snapshot.IndexOf(actorNumber);
        if (startIndex < 0)
            startIndex = 0;

        for (int step = 1; step <= snapshot.Count; step++)
        {
            int index = (startIndex + step) % snapshot.Count;
            int candidate = snapshot[index];

            if (activePlayerOrder.Contains(candidate))
                return candidate;
        }

        return activePlayerOrder[0];
    }

    private void HandleGameOver()
    {
        isGameOver = true;
        isRoundWaitingForResolution = false;
        isRoundTransitionInProgress = false;

        int winnerActorNumber = activePlayerOrder.Count > 0 ? activePlayerOrder[0] : -1;
        string winnerName = winnerActorNumber > 0 ? GetPlayerDisplayName(winnerActorNumber) : "BRAK";
        Sprite winnerAvatar = winnerActorNumber > 0 ? GetPlayerAvatarSprite(winnerActorNumber) : null;

        AddSystemLog("<b>Koniec gry</b>");
        AddSystemLog("Wygrywa: <b>" + winnerName + "</b>");
        SetCurrentBidDisplayText(string.Empty);

        RefreshLocalHandPanelState();
        SaveTurnStateToRoom();

        if (gameEndSequenceController != null)
        {
            gameEndSequenceController.PlayFinalWinnerSequence(winnerAvatar, winnerName);
        }

        if (PhotonNetwork.LocalPlayer != null && winnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("Wygrałeś");
        }
    }

    private void RemovePlayerFromTurnOrderInternal(int actorNumber, bool notify)
    {
        int previousActiveActor = GetCurrentPlayerActorNumber();

        int removedIndex = activePlayerOrder.IndexOf(actorNumber);
        if (removedIndex < 0)
            return;

        activePlayerOrder.RemoveAt(removedIndex);
        disconnectedActorNumbers.Remove(actorNumber);

        if (seatViewsByActorNumber.TryGetValue(actorNumber, out SeatUIView removedSeatView) && removedSeatView != null)
        {
            removedSeatView.SetEliminatedVisual(true);
        }

        if (activePlayerOrder.Count == 0)
        {
            currentTurnIndex = -1;
            RefreshActiveHighlight(-1);

            if (notify)
                RefreshLocalHandPanelState();

            SaveTurnStateToRoom();
            return;
        }

        if (removedIndex < currentTurnIndex)
        {
            currentTurnIndex--;
        }

        if (currentTurnIndex >= activePlayerOrder.Count)
        {
            currentTurnIndex = 0;
        }

        if (starterPlayerId == actorNumber)
        {
            starterPlayerId = activePlayerOrder[currentTurnIndex];
        }

        if (logTurnFlow)
        {
            Debug.Log(
                "TurnManager: usunięto gracza z kolejki = " + actorNumber +
                " | nowa kolejka = " + string.Join(" -> ", activePlayerOrder)
            );
        }

        if (notify)
        {
            int newActiveActor = GetCurrentPlayerActorNumber();
            if (newActiveActor != previousActiveActor)
                ResetCurrentTurnTimer();

            NotifyActivePlayerChanged();
        }

        SaveTurnStateToRoom();
    }

    private bool EvaluateDeclaredRankExists(string declaredText)
    {
        TryResolveCardDealTest();

        if (cardDealTest == null)
            return false;

        if (string.IsNullOrWhiteSpace(declaredText))
            return false;

        string handId = GetHandIdFromOptionText(declaredText);
        if (string.IsNullOrEmpty(handId))
        {
            Debug.LogWarning("TurnManager: nie udało się zmapować układu: " + declaredText);
            return false;
        }

        List<CardSpriteEntry> dealtCards = cardDealTest.GetAllRoundCardsForEvaluation();
        if (dealtCards == null || dealtCards.Count == 0)
            return false;

        Dictionary<string, int> rankCounts = new Dictionary<string, int>();
        Dictionary<string, int> suitCounts = new Dictionary<string, int>();
        Dictionary<string, HashSet<string>> suitRanks = new Dictionary<string, HashSet<string>>();

        for (int i = 0; i < dealtCards.Count; i++)
        {
            CardSpriteEntry entry = dealtCards[i];
            if (entry == null)
                continue;

            string rank = NormalizeRank(entry.rank.ToString());
            string suit = NormalizeSuit(entry.suit.ToString());

            if (string.IsNullOrEmpty(rank) || string.IsNullOrEmpty(suit))
                continue;

            if (!rankCounts.ContainsKey(rank))
                rankCounts[rank] = 0;

            rankCounts[rank]++;

            if (!suitCounts.ContainsKey(suit))
                suitCounts[suit] = 0;

            suitCounts[suit]++;

            if (!suitRanks.ContainsKey(suit))
                suitRanks[suit] = new HashSet<string>();

            suitRanks[suit].Add(rank);
        }

        if (handId.StartsWith("HIGH_"))
        {
            string rank = handId.Substring("HIGH_".Length);
            return GetCount(rankCounts, rank) >= 1;
        }

        if (handId.StartsWith("PAIR_"))
        {
            string rank = handId.Substring("PAIR_".Length);
            return GetCount(rankCounts, rank) >= 2;
        }

        if (handId.StartsWith("TWOPAIR_"))
        {
            string[] parts = handId.Split('_');
            if (parts.Length < 3)
                return false;

            return GetCount(rankCounts, parts[1]) >= 2 &&
                   GetCount(rankCounts, parts[2]) >= 2;
        }

        if (handId == "STRAIGHT_SMALL")
        {
            return HasStraight(rankCounts, new string[] { "9", "10", "J", "Q", "K" });
        }

        if (handId == "STRAIGHT_BIG")
        {
            return HasStraight(rankCounts, new string[] { "10", "J", "Q", "K", "A" });
        }

        if (handId.StartsWith("TRIPS_"))
        {
            string rank = handId.Substring("TRIPS_".Length);
            return GetCount(rankCounts, rank) >= 3;
        }

        if (handId.StartsWith("FULL_"))
        {
            string[] parts = handId.Split('_');
            if (parts.Length < 3)
                return false;

            return GetCount(rankCounts, parts[1]) >= 3 &&
                   GetCount(rankCounts, parts[2]) >= 2;
        }

        if (handId == "FLUSH_DIAMOND")
            return GetCount(suitCounts, "♦") >= 5;

        if (handId == "FLUSH_HEART")
            return GetCount(suitCounts, "♥") >= 5;

        if (handId == "FLUSH_CLUB")
            return GetCount(suitCounts, "♣") >= 5;

        if (handId == "FLUSH_SPADE")
            return GetCount(suitCounts, "♠") >= 5;

        if (handId.StartsWith("QUADS_"))
        {
            string rank = handId.Substring("QUADS_".Length);
            return GetCount(rankCounts, rank) >= 4;
        }

        if (handId == "POKER_SMALL_DIAMOND")
            return HasStraightFlush(suitRanks, "♦", new string[] { "9", "10", "J", "Q", "K" });

        if (handId == "POKER_SMALL_HEART")
            return HasStraightFlush(suitRanks, "♥", new string[] { "9", "10", "J", "Q", "K" });

        if (handId == "POKER_SMALL_CLUB")
            return HasStraightFlush(suitRanks, "♣", new string[] { "9", "10", "J", "Q", "K" });

        if (handId == "POKER_SMALL_SPADE")
            return HasStraightFlush(suitRanks, "♠", new string[] { "9", "10", "J", "Q", "K" });

        if (handId == "POKER_BIG_DIAMOND")
            return HasStraightFlush(suitRanks, "♦", new string[] { "10", "J", "Q", "K", "A" });

        if (handId == "POKER_BIG_HEART")
            return HasStraightFlush(suitRanks, "♥", new string[] { "10", "J", "Q", "K", "A" });

        if (handId == "POKER_BIG_CLUB")
            return HasStraightFlush(suitRanks, "♣", new string[] { "10", "J", "Q", "K", "A" });

        if (handId == "POKER_BIG_SPADE")
            return HasStraightFlush(suitRanks, "♠", new string[] { "10", "J", "Q", "K", "A" });

        return false;
    }

    private int GetCount(Dictionary<string, int> counts, string key)
    {
        return counts.TryGetValue(key, out int value) ? value : 0;
    }

    private bool HasStraight(Dictionary<string, int> rankCounts, string[] requiredRanks)
    {
        for (int i = 0; i < requiredRanks.Length; i++)
        {
            if (GetCount(rankCounts, requiredRanks[i]) <= 0)
                return false;
        }

        return true;
    }

    private bool HasStraightFlush(Dictionary<string, HashSet<string>> suitRanks, string suit, string[] requiredRanks)
    {
        if (!suitRanks.TryGetValue(suit, out HashSet<string> ranksForSuit) || ranksForSuit == null)
            return false;

        for (int i = 0; i < requiredRanks.Length; i++)
        {
            if (!ranksForSuit.Contains(requiredRanks[i]))
                return false;
        }

        return true;
    }

    private string NormalizeRank(string rawRank)
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

        return string.Empty;
    }

    private string NormalizeSuit(string rawSuit)
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

        return string.Empty;
    }

    private string GetHandIdFromOptionText(string optionText)
    {
        string normalized = NormalizeText(optionText);
        if (string.IsNullOrEmpty(normalized))
            return string.Empty;

        if (normalized == "Mały poker ♦")
            return "POKER_SMALL_DIAMOND";

        if (normalized == "Mały poker ♥")
            return "POKER_SMALL_HEART";

        if (normalized == "Mały poker ♣")
            return "POKER_SMALL_CLUB";

        if (normalized == "Mały poker ♠")
            return "POKER_SMALL_SPADE";

        if (normalized == "Duży poker ♦")
            return "POKER_BIG_DIAMOND";

        if (normalized == "Duży poker ♥")
            return "POKER_BIG_HEART";

        if (normalized == "Duży poker ♣")
            return "POKER_BIG_CLUB";

        if (normalized == "Duży poker ♠")
            return "POKER_BIG_SPADE";

        if (normalized == "Kolor ♦")
            return "FLUSH_DIAMOND";

        if (normalized == "Kolor ♥")
            return "FLUSH_HEART";

        if (normalized == "Kolor ♣")
            return "FLUSH_CLUB";

        if (normalized == "Kolor ♠")
            return "FLUSH_SPADE";

        string[] rawParts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] parts = new string[rawParts.Length];

        for (int i = 0; i < rawParts.Length; i++)
        {
            parts[i] = NormalizeRank(rawParts[i]);
        }

        if (parts.Length == 1 && IsRank(parts[0]))
            return "HIGH_" + parts[0];

        if (parts.Length == 2 && parts[0] == parts[1] && IsRank(parts[0]))
            return "PAIR_" + parts[0];

        if (parts.Length == 3 && parts[0] == parts[1] && parts[1] == parts[2] && IsRank(parts[0]))
            return "TRIPS_" + parts[0];

        if (parts.Length == 4)
        {
            if (parts[0] == parts[1] && parts[1] == parts[2] && parts[2] == parts[3] && IsRank(parts[0]))
                return "QUADS_" + parts[0];

            if (parts[0] == parts[1] && parts[2] == parts[3] && parts[0] != parts[2] && IsRank(parts[0]) && IsRank(parts[2]))
            {
                string firstPair = parts[0];
                string secondPair = parts[2];

                if (GetRankIndex(firstPair) > GetRankIndex(secondPair))
                {
                    string temp = firstPair;
                    firstPair = secondPair;
                    secondPair = temp;
                }

                return "TWOPAIR_" + firstPair + "_" + secondPair;
            }
        }

        if (parts.Length == 5)
        {
            string joined = string.Join(" ", parts);

            if (joined == "9 10 J Q K")
                return "STRAIGHT_SMALL";

            if (joined == "10 J Q K A")
                return "STRAIGHT_BIG";

            Dictionary<string, int> counts = new Dictionary<string, int>();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (!IsRank(part))
                    return string.Empty;

                if (!counts.ContainsKey(part))
                    counts[part] = 0;

                counts[part]++;
            }

            if (counts.Count == 2)
            {
                string tripleRank = string.Empty;
                string pairRank = string.Empty;

                foreach (KeyValuePair<string, int> pair in counts)
                {
                    if (pair.Value == 3)
                        tripleRank = pair.Key;
                    else if (pair.Value == 2)
                        pairRank = pair.Key;
                }

                if (!string.IsNullOrEmpty(tripleRank) && !string.IsNullOrEmpty(pairRank))
                    return "FULL_" + tripleRank + "_" + pairRank;
            }
        }

        return string.Empty;
    }

    private bool IsRank(string value)
    {
        for (int i = 0; i < RankOrder.Length; i++)
        {
            if (RankOrder[i] == value)
                return true;
        }

        return false;
    }

    private int GetRankIndex(string value)
    {
        for (int i = 0; i < RankOrder.Length; i++)
        {
            if (RankOrder[i] == value)
                return i;
        }

        return -1;
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string result = value.Trim();

        while (result.Contains("  "))
        {
            result = result.Replace("  ", " ");
        }

        return result;
    }

    private bool TryRestoreTurnStateFromRoom()
    {
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (!TryGetRoomStringProp(TurnStateActiveOrderKey, out string serializedOrder) || string.IsNullOrWhiteSpace(serializedOrder))
            return false;

        List<int> restoredOrder = DeserializeIntList(serializedOrder);
        if (restoredOrder.Count == 0)
            return false;

        List<int> validatedOrder = new List<int>();

        for (int i = 0; i < restoredOrder.Count; i++)
        {
            int actorNumber = restoredOrder[i];

            if (PhotonNetwork.CurrentRoom.Players != null &&
                (PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber) ||
                 LobbyBotRegistry.IsBot(actorNumber)))
            {
                validatedOrder.Add(actorNumber);
            }
        }

        if (validatedOrder.Count == 0)
            return false;

        activePlayerOrder.Clear();
        activePlayerOrder.AddRange(validatedOrder);

        Dictionary<int, int> restoredStages = new Dictionary<int, int>();
        if (TryGetRoomStringProp(TurnStatePenaltyStagesKey, out string serializedStages) && !string.IsNullOrWhiteSpace(serializedStages))
        {
            restoredStages = DeserializePenaltyStages(serializedStages);
        }

        playerPenaltyStageByActorNumber.Clear();
        for (int i = 0; i < activePlayerOrder.Count; i++)
        {
            int actorNumber = activePlayerOrder[i];
            playerPenaltyStageByActorNumber[actorNumber] =
                restoredStages.TryGetValue(actorNumber, out int stage) ? stage : 0;
        }

        TryGetRoomIntProp(TurnStateRoundNumberKey, out currentRoundNumber);
        if (currentRoundNumber <= 0)
            currentRoundNumber = 1;

        TryGetRoomBoolProp(TurnStateHasDeclarationKey, out hasDeclarationThisRound);
        TryGetRoomStringProp(TurnStateDeclaredRankKey, out currentDeclaredRankText);
        TryGetRoomStringProp(TurnStateBidDisplayKey, out currentBidDisplayText);
        TryGetRoomIntProp(TurnStateStarterActorKey, out starterPlayerId);
        TryGetRoomIntProp(TurnStateLastDeclarerKey, out lastDeclarerActorNumber);
        TryGetRoomIntProp(TurnStateCheckingPlayerKey, out checkingPlayerActorNumber);
        TryGetRoomBoolProp(TurnStateWaitingKey, out isRoundWaitingForResolution);
        TryGetRoomBoolProp(TurnStateTransitionKey, out isRoundTransitionInProgress);
        TryGetRoomBoolProp(TurnStateGameOverKey, out isGameOver);

        if (string.IsNullOrWhiteSpace(currentDeclaredRankText))
            currentDeclaredRankText = null;

        if (string.IsNullOrWhiteSpace(currentBidDisplayText))
            currentBidDisplayText = string.Empty;

        if (!TryGetRoomIntProp(TurnStateCurrentActorKey, out int restoredCurrentActor))
            restoredCurrentActor = activePlayerOrder[0];

        currentTurnIndex = activePlayerOrder.IndexOf(restoredCurrentActor);
        if (currentTurnIndex < 0)
            currentTurnIndex = 0;

        return true;
    }

    private void SaveTurnStateToRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { TurnStateActiveOrderKey, SerializeIntList(activePlayerOrder) },
            { TurnStatePenaltyStagesKey, SerializePenaltyStages() },
            { TurnStateCurrentActorKey, GetCurrentPlayerActorNumber() },
            { TurnStateStarterActorKey, starterPlayerId },
            { TurnStateRoundNumberKey, currentRoundNumber },
            { TurnStateDeclaredRankKey, currentDeclaredRankText ?? string.Empty },
            { TurnStateHasDeclarationKey, hasDeclarationThisRound },
            { TurnStateBidDisplayKey, currentBidDisplayText ?? string.Empty },
            { TurnStateLastDeclarerKey, lastDeclarerActorNumber },
            { TurnStateCheckingPlayerKey, checkingPlayerActorNumber },
            { TurnStateWaitingKey, isRoundWaitingForResolution },
            { TurnStateTransitionKey, isRoundTransitionInProgress },
            { TurnStateGameOverKey, isGameOver },
            { GameEndedKey, isGameOver }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private string SerializeIntList(List<int> values)
    {
        if (values == null || values.Count == 0)
            return string.Empty;

        return string.Join(",", values);
    }

    private List<int> DeserializeIntList(string serialized)
    {
        List<int> result = new List<int>();

        if (string.IsNullOrWhiteSpace(serialized))
            return result;

        string[] parts = serialized.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out int parsed))
                result.Add(parsed);
        }

        return result;
    }

    private string SerializePenaltyStages()
    {
        if (playerPenaltyStageByActorNumber.Count == 0)
            return string.Empty;

        List<string> parts = new List<string>();

        for (int i = 0; i < activePlayerOrder.Count; i++)
        {
            int actorNumber = activePlayerOrder[i];
            int stage = playerPenaltyStageByActorNumber.TryGetValue(actorNumber, out int value) ? value : 0;
            parts.Add(actorNumber + ":" + stage);
        }

        return string.Join(";", parts);
    }

    private Dictionary<int, int> DeserializePenaltyStages(string serialized)
    {
        Dictionary<int, int> result = new Dictionary<int, int>();

        if (string.IsNullOrWhiteSpace(serialized))
            return result;

        string[] entries = serialized.Split(';');
        for (int i = 0; i < entries.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(entries[i]))
                continue;

            string[] pair = entries[i].Split(':');
            if (pair.Length != 2)
                continue;

            if (!int.TryParse(pair[0], out int actorNumber))
                continue;

            if (!int.TryParse(pair[1], out int stage))
                continue;

            result[actorNumber] = stage;
        }

        return result;
    }

    private bool TryGetRoomStringProp(string key, out string value)
    {
        value = string.Empty;

        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object rawValue))
            return false;

        if (rawValue is string stringValue)
        {
            value = stringValue;
            return true;
        }

        return false;
    }

    private bool TryGetRoomIntProp(string key, out int value)
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

        if (rawValue is byte byteValue)
        {
            value = byteValue;
            return true;
        }

        if (rawValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
        {
            value = parsedValue;
            return true;
        }

        return false;
    }

    private bool TryGetRoomBoolProp(string key, out bool value)
    {
        value = false;

        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object rawValue))
            return false;

        if (rawValue is bool boolValue)
        {
            value = boolValue;
            return true;
        }

        if (rawValue is string stringValue && bool.TryParse(stringValue, out bool parsedValue))
        {
            value = parsedValue;
            return true;
        }

        return false;
    }

    private void CacheSeatViews()
    {
        seatViewsByActorNumber.Clear();

        SeatUIView[] allSeatViews = FindObjectsByType<SeatUIView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < allSeatViews.Length; i++)
        {
            SeatUIView seatView = allSeatViews[i];
            if (seatView == null)
                continue;

            if (seatView.name.Contains("Dealer"))
                continue;

            string[] parts = seatView.name.Split('_');
            if (parts.Length < 3)
                continue;

            if (!int.TryParse(parts[2], out int actorNumber))
                continue;

            if (!seatViewsByActorNumber.ContainsKey(actorNumber))
            {
                seatViewsByActorNumber.Add(actorNumber, seatView);
            }
        }
    }

    private void RefreshActiveHighlight(int activeActorNumber)
    {
        foreach (KeyValuePair<int, SeatUIView> pair in seatViewsByActorNumber)
        {
            if (pair.Value == null)
                continue;

            pair.Value.SetActiveTurnHighlight(pair.Key == activeActorNumber);
        }
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

    private bool TryGetSharedGameSeed(out int gameSeed)
    {
        gameSeed = 0;

        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSeedKey, out object rawValue))
            return false;

        if (rawValue is int intValue)
        {
            gameSeed = intValue;
            return true;
        }

        if (rawValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
        {
            gameSeed = parsedValue;
            return true;
        }

        return false;
    }

    private void ResetCurrentTurnTimer()
    {
        int currentActorNumber = GetCurrentPlayerActorNumber();

        if (currentActorNumber > 0 && disconnectedActorNumbers.Contains(currentActorNumber))
            currentTurnTimeLeft = disconnectedRemovalSeconds;
        else
            currentTurnTimeLeft = turnDurationSeconds;
    }

    private int GetWrappedTurnIndex(int index)
    {
        if (activePlayerOrder.Count == 0)
            return -1;

        return ((index % activePlayerOrder.Count) + activePlayerOrder.Count) % activePlayerOrder.Count;
    }

    private void RefreshDisconnectedPlayersFromRoomState()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || activePlayerOrder.Count == 0)
            return;

        int currentActorNumber = GetCurrentPlayerActorNumber();

        for (int i = 0; i < activePlayerOrder.Count; i++)
        {
            int actorNumber = activePlayerOrder[i];

            if (LobbyBotRegistry.IsBot(actorNumber))
            {
                disconnectedActorNumbers.Remove(actorNumber);
                continue;
            }

            bool wasDisconnected = disconnectedActorNumbers.Contains(actorNumber);
            bool isDisconnectedNow = false;

            if (PhotonNetwork.CurrentRoom.Players == null ||
                !PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out Player player) ||
                player == null)
            {
                isDisconnectedNow = true;
            }
            else if (player.IsInactive)
            {
                isDisconnectedNow = true;
            }

            if (isDisconnectedNow && !wasDisconnected)
            {
                disconnectedActorNumbers.Add(actorNumber);

                if (actorNumber == currentActorNumber &&
                    !isRoundWaitingForResolution &&
                    !isRoundTransitionInProgress &&
                    !isGameOver)
                {
                    ResetCurrentTurnTimer();
                }
            }
            else if (!isDisconnectedNow && wasDisconnected)
            {
                disconnectedActorNumbers.Remove(actorNumber);

                if (actorNumber == currentActorNumber &&
                    !isRoundWaitingForResolution &&
                    !isRoundTransitionInProgress &&
                    !isGameOver)
                {
                    ResetCurrentTurnTimer();
                }
            }
        }
    }
}
