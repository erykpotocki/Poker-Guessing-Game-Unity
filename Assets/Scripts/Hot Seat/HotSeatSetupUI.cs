using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HotSeatSetupUI : MonoBehaviour
{
    private enum HotSeatPhase
    {
        FirstCardPreview,
        TurnLoop,
        RoundResult,
        GameOver
    }

    private class HotSeatPlayer
    {
        public string Name;
        public int CardCount = 1;
        public bool Eliminated;
        public bool PenaltyGoingUp = true;

        public readonly List<CardSpriteEntry> Cards =
            new List<CardSpriteEntry>();
    }

    [Header("Setup UI")]
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private Button addPlayerButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Transform playerListRoot;
    [SerializeField] private GameObject playerNameRowPrefab;

    [Header("Card UI")]
    [SerializeField] private GameObject cardPanel;
    [SerializeField] private TextMeshProUGUI currentPlayerNameText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Image cardImage;
    [SerializeField] private Button cardButton;
    [SerializeField] private TextMeshProUGUI cardText;

    [Header("Pass Phone UI")]
    [SerializeField] private HotSeatPassPhoneUI passPhoneUI;

    [Header("Turn Management")]
    [SerializeField] private HotSeatTurnManager turnManager;
    [SerializeField] private HotSeatBidController bidController;

    [Header("Card Databases")]
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private CardBackDatabase cardBackDatabase;
    [SerializeField] private Sprite resultTableSprite;
    [SerializeField] private Sprite pokerButtonSprite;
    [SerializeField] private int cardBackIndex;

    [Header("Settings")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 6;
    [SerializeField] private int maxNameLength = 10;

    [Header("Fallback Colors")]
    [SerializeField] private Color cardBackColor =
        new Color(0.08f, 0.08f, 0.1f, 1f);

    [SerializeField] private Color cardFrontColor =
        new Color(0.92f, 0.88f, 0.78f, 1f);

    private readonly List<TMP_InputField> playerInputs =
        new List<TMP_InputField>();

    private readonly List<Image> extraCardImages =
        new List<Image>();

    private Button multiCardTouchButton;

    private readonly List<GameObject> roundResultObjects =
        new List<GameObject>();

    private readonly List<HotSeatPlayer> players =
        new List<HotSeatPlayer>();

    private HotSeatPhase currentPhase;
    private int starterIndex;
    private int currentPlayerIndex;
    private int firstPreviewCount;
    private int lastDeclarerIndex = -1;
    private bool cardVisible;
    private bool waitingForCardReveal;
    private bool pendingCanCheck;
    private bool pendingBeginNewRound;

    private void Start()
    {
        PokerButtonTheme.EnsureController();

        addPlayerButton.onClick.AddListener(AddPlayer);
        startButton.onClick.AddListener(StartHotSeat);
        cardButton.onClick.AddListener(OnCardClicked);

        if (bidController != null)
        {
            bidController.RaiseConfirmed += HandleRaiseConfirmed;
            bidController.CheckConfirmed += HandleCheckConfirmed;
        }

        if (cardPanel != null)
            cardPanel.SetActive(false);

        if (passPhoneUI != null)
            passPhoneUI.Hide();

        if (turnManager != null)
            turnManager.StopTurn();

        if (cardImage != null)
            cardImage.preserveAspect = true;

        ApplySetupStyle();
        RefreshButtons();
    }

    private void AddPlayer()
    {
        if (playerInputs.Count >= maxPlayers)
            return;

        int playerNumber = playerInputs.Count + 1;

        GameObject row = Instantiate(
            playerNameRowPrefab,
            playerListRoot
        );

        TMP_InputField input =
            row.GetComponentInChildren<TMP_InputField>();

        input.characterLimit = maxNameLength;
        input.text = "GRACZ" + playerNumber;

        input.onValueChanged.AddListener(value =>
        {
            string cleaned = CleanName(value);

            if (cleaned != value)
                input.SetTextWithoutNotify(cleaned);
        });

        CreateRemovePlayerButton(row, input);
        playerInputs.Add(input);
        RefreshButtons();
    }

    private void CreateRemovePlayerButton(
        GameObject row,
        TMP_InputField input)
    {
        GameObject buttonObject = new GameObject("RemovePlayerButton");
        buttonObject.transform.SetParent(row.transform, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(82f, 82f);
        rect.anchoredPosition = new Vector2(-16f, 0f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.62f, 0.12f, 0.12f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => RemovePlayer(input, row));

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "×";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 56f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        if (input.textViewport is RectTransform inputRect)
            inputRect.offsetMax = new Vector2(-100f, inputRect.offsetMax.y);
    }

    private void RemovePlayer(TMP_InputField input, GameObject row)
    {
        playerInputs.Remove(input);
        Destroy(row);

        for (int i = 0; i < playerInputs.Count; i++)
        {
            string currentName = playerInputs[i].text;
            string suffix = currentName.StartsWith("GRACZ")
                ? currentName.Substring(5)
                : string.Empty;

            if (int.TryParse(suffix, out _))
                playerInputs[i].SetTextWithoutNotify("GRACZ" + (i + 1));
        }

        RefreshButtons();
    }

    private void StartHotSeat()
    {
        if (playerInputs.Count < minPlayers)
            return;

        if (cardDatabase == null)
        {
            Debug.LogError(
                "HotSeatSetupUI: nie przypisano CardDatabase."
            );

            return;
        }

        if (cardBackDatabase == null)
        {
            Debug.LogError(
                "HotSeatSetupUI: nie przypisano CardBackDatabase."
            );

            return;
        }

        players.Clear();

        ChooseCardBackForThisGame();

        for (int i = 0; i < playerInputs.Count; i++)
        {
            string playerName =
                CleanName(playerInputs[i].text);

            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "GRACZ" + (i + 1);

            players.Add(new HotSeatPlayer
            {
                Name = playerName,
                CardCount = 1,
                Eliminated = false,
                PenaltyGoingUp = true
            });
        }

        StartNewRound();
    }

    private void StartNewRound(int requestedStarterIndex = -1)
    {
        DealCards();

        starterIndex = requestedStarterIndex >= 0 &&
                       requestedStarterIndex < players.Count &&
                       !players[requestedStarterIndex].Eliminated
            ? requestedStarterIndex
            : GetRandomActivePlayerIndex();

        if (starterIndex < 0)
        {
            ShowGameOver();
            return;
        }

        currentPlayerIndex = starterIndex;
        firstPreviewCount = 0;
        lastDeclarerIndex = -1;
        currentPhase = HotSeatPhase.FirstCardPreview;

        setupPanel.SetActive(false);
        cardPanel.SetActive(true);

        if (passPhoneUI != null)
            passPhoneUI.Hide();

        if (turnManager != null)
            turnManager.StopTurn();

        ShowCardBack();
    }

    private void OnCardClicked()
    {
        if (currentPhase == HotSeatPhase.RoundResult)
        {
            StartNewRound(GetNextRoundStarterIndex());
            return;
        }

        if (currentPhase == HotSeatPhase.GameOver)
        {
            RestartGame();
            return;
        }

        if (!cardVisible)
        {
            ShowCardFront();
            return;
        }

        if (currentPhase == HotSeatPhase.FirstCardPreview)
        {
            HideCardAndContinuePreview();
            return;
        }

        ShowCardBack();
    }

    private void ShowCardBack()
    {
        cardVisible = false;

        HotSeatPlayer player = players[currentPlayerIndex];

        currentPlayerNameText.text = player.Name;

        Sprite backSprite =
            cardBackDatabase.GetBackSprite(cardBackIndex);

        ShowCardSprite(
            backSprite,
            "REWERS",
            cardBackColor,
            Color.white
        );

        instructionText.text =
            "UPEWNIJ SIĘ, ŻE NIKT NIE PATRZY\n" +
            "NACIŚNIJ KARTĘ, ŻEBY ODKRYĆ";
    }

    private void ShowCardFront()
    {
        cardVisible = true;

        HotSeatPlayer player = players[currentPlayerIndex];

        currentPlayerNameText.text = player.Name;

        if (player.Cards.Count == 0)
        {
            ShowCardSprite(
                null,
                "BRAK KARTY",
                cardFrontColor,
                Color.black
            );

            EnableTurnActionsAfterCardReveal();
            return;
        }

        if (player.Cards.Count > 1)
        {
            ShowPlayerCardSprites(player);

            instructionText.text =
                "ZAPAMIĘTAJ SWOJE KARTY\n" +
                "NACIŚNIJ PONOWNIE, ŻEBY ZAKRYĆ";

            EnableTurnActionsAfterCardReveal();
            return;
        }

        CardSpriteEntry card = player.Cards[0];
        Sprite frontSprite = card.sprite;

        if (frontSprite == null && cardDatabase != null)
        {
            frontSprite = cardDatabase.GetCardSprite(
                card.suit,
                card.rank
            );
        }

        ShowCardSprite(
            frontSprite,
            GetCardDisplayName(card),
            cardFrontColor,
            Color.black
        );

        instructionText.text =
            "ZAPAMIĘTAJ SWOJĄ KARTĘ\n" +
            "NACIŚNIJ PONOWNIE, ŻEBY ZAKRYĆ";

        EnableTurnActionsAfterCardReveal();
    }

    private void ShowCardSprite(
        Sprite sprite,
        string fallbackText,
        Color fallbackColor,
        Color fallbackTextColor)
    {
        ClearRoundResultObjects();
        ClearExtraCardImages();
        EnsureCardText();

        cardText.fontSize = 70;

        cardImage.sprite = sprite;
        cardImage.preserveAspect = true;

        if (sprite != null)
        {
            cardImage.color = Color.white;
            cardText.text = "";
            cardText.gameObject.SetActive(false);
            return;
        }

        cardImage.color = fallbackColor;
        cardText.gameObject.SetActive(true);
        cardText.text = fallbackText;
        cardText.color = fallbackTextColor;
    }

    // A compact fan keeps the player name visible and lets the player
    // recognise every card without filling the entire portrait screen.
    private void ShowPlayerCardSprites(HotSeatPlayer player)
    {
        ClearExtraCardImages();
        EnsureCardText();

        int cardCount = player.Cards.Count;
        float scale = cardCount == 2 ? 0.38f : 0.34f;
        float cardWidth = 650f * scale;
        float cardHeight = 900f * scale;
        const float horizontalStep = 58f;
        const float verticalStep = 12f;
        const float rotationStep = 7f;

        cardText.text = "";
        cardText.gameObject.SetActive(false);

        for (int i = 0; i < cardCount; i++)
        {
            CardSpriteEntry card = player.Cards[i];
            Sprite sprite = card.sprite;

            if (sprite == null && cardDatabase != null)
                sprite = cardDatabase.GetCardSprite(card.suit, card.rank);

            Image image = i == 0 ? cardImage : CreateExtraCardImage();
            RectTransform rect = image.rectTransform;
            float centeredIndex = i - (cardCount - 1) * 0.5f;

            rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            rect.anchoredPosition = new Vector2(
                centeredIndex * horizontalStep,
                -32f - Mathf.Abs(centeredIndex) * verticalStep
            );
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                -centeredIndex * rotationStep
            );
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : cardFrontColor;
            image.preserveAspect = true;

            if (i > 0)
                extraCardImages.Add(image);
        }

        ShowMultiCardTouchArea(
            cardWidth + horizontalStep * (cardCount - 1) + 40f,
            cardHeight + 48f
        );
    }

    private void ShowMultiCardTouchArea(float width, float height)
    {
        if (multiCardTouchButton == null)
        {
            GameObject touchObject = new GameObject(
                "CardTouchArea",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            touchObject.transform.SetParent(cardImage.transform.parent, false);

            Image image = touchObject.GetComponent<Image>();
            image.color = Color.clear;

            multiCardTouchButton = touchObject.GetComponent<Button>();
            multiCardTouchButton.targetGraphic = image;
            multiCardTouchButton.onClick.AddListener(OnCardClicked);
        }

        RectTransform rect = multiCardTouchButton.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -32f);
        rect.sizeDelta = new Vector2(width, height);
        multiCardTouchButton.gameObject.SetActive(true);
        multiCardTouchButton.transform.SetAsLastSibling();
    }

    private Image CreateExtraCardImage()
    {
        GameObject cardObject = new GameObject("HS_ExtraCard");
        cardObject.transform.SetParent(cardImage.transform.parent, false);
        cardObject.transform.SetAsLastSibling();

        Image image = cardObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void ClearExtraCardImages()
    {
        if (multiCardTouchButton != null)
            multiCardTouchButton.gameObject.SetActive(false);

        foreach (Image image in extraCardImages)
        {
            if (image != null)
                Destroy(image.gameObject);
        }

        extraCardImages.Clear();

        if (cardImage == null)
            return;

        RectTransform rect = cardImage.rectTransform;
        rect.sizeDelta = new Vector2(650f, 900f);
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = new Vector2(0f, 224f);
    }

    private void ShowRoundCards()
    {
        ClearRoundResultObjects();
        CreateRoundTable();

        List<HotSeatPlayer> resultPlayers = new List<HotSeatPlayer>();
        foreach (HotSeatPlayer player in players)
        {
            if (player.Cards.Count > 0)
                resultPlayers.Add(player);
        }

        for (int row = 0; row < resultPlayers.Count; row++)
        {
            HotSeatPlayer player = resultPlayers[row];
            Vector2 seatPosition = GetResultSeatPosition(
                row,
                resultPlayers.Count
            );
            CreateRoundResultLabel(
                player.Name,
                seatPosition + new Vector2(0f, 54f)
            );

            int visibleCards = Mathf.Min(player.Cards.Count, 3);
            float spacing = 82f;
            float firstX = -spacing * (visibleCards - 1) * 0.5f;

            for (int i = 0; i < visibleCards; i++)
            {
                CardSpriteEntry card = player.Cards[i];
                Sprite sprite = card.sprite;

                if (sprite == null && cardDatabase != null)
                    sprite = cardDatabase.GetCardSprite(card.suit, card.rank);

                CreateRoundResultCard(
                    sprite,
                    seatPosition + new Vector2(firstX + spacing * i, -22f)
                );
            }
        }
    }

    private Vector2 GetResultSeatPosition(int index, int playerCount)
    {
        float angle = 90f - 360f * index / playerCount;
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(
            Mathf.Cos(radians) * 220f,
            Mathf.Sin(radians) * 270f
        );
    }

    private void CreateRoundTable()
    {
        if (resultTableSprite == null)
            return;

        GameObject tableObject = new GameObject("HS_ResultPokerTable");
        tableObject.transform.SetParent(cardImage.transform, false);
        tableObject.transform.SetAsFirstSibling();

        RectTransform rect = tableObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 430f);

        Image image = tableObject.AddComponent<Image>();
        image.sprite = resultTableSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        roundResultObjects.Add(tableObject);
    }

    private void CreateRoundResultLabel(string playerName, Vector2 position)
    {
        GameObject labelObject = new GameObject("HS_ResultPlayerName");
        labelObject.transform.SetParent(cardImage.transform, false);

        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 48f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = playerName;
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.12f, 0.12f, 0.15f);
        label.raycastTarget = false;

        roundResultObjects.Add(labelObject);
    }

    private void CreateRoundResultCard(Sprite sprite, Vector2 position)
    {
        GameObject cardObject = new GameObject("HS_ResultCard");
        cardObject.transform.SetParent(cardImage.transform, false);

        RectTransform rect = cardObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(96f, 132f);
        rect.anchoredPosition = position;

        Image image = cardObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : cardFrontColor;
        image.preserveAspect = true;
        image.raycastTarget = false;

        roundResultObjects.Add(cardObject);
    }

    private void ClearRoundResultObjects()
    {
        foreach (GameObject resultObject in roundResultObjects)
        {
            if (resultObject != null)
                Destroy(resultObject);
        }

        roundResultObjects.Clear();
    }

    private void HideCardAndContinuePreview()
    {
        cardVisible = false;
        firstPreviewCount++;

        if (firstPreviewCount >= GetActivePlayerCount())
        {
            ShowPassPhoneScreen();
            return;
        }

        currentPlayerIndex =
            GetNextActivePlayerIndex(currentPlayerIndex);

        ShowCardBack();
    }

    private void ShowPassPhoneScreen()
    {
        cardPanel.SetActive(false);

        if (passPhoneUI != null)
        {
            passPhoneUI.ShowInitialRound(
                players[starterIndex].Name,
                BeginTurnLoop
            );
            return;
        }

        BeginTurnLoop();
    }

    private void ShowPassPhoneScreen(
        string playerName,
        UnityAction onStartTurn)
    {
        cardPanel.SetActive(false);

        if (passPhoneUI != null)
        {
            passPhoneUI.ShowNextTurn(playerName, onStartTurn);
            return;
        }

        onStartTurn?.Invoke();
    }

    private void BeginTurnLoop()
    {
        currentPhase = HotSeatPhase.TurnLoop;
        currentPlayerIndex = starterIndex;
        cardVisible = false;

        BeginTurnForCurrentPlayer(false, true);
    }

    private void HandleRaiseConfirmed(string chosenRank)
    {
        lastDeclarerIndex = currentPlayerIndex;
        currentPlayerIndex =
            GetNextActivePlayerIndex(currentPlayerIndex);

        cardVisible = false;
        ShowPassPhoneScreen(
            players[currentPlayerIndex].Name,
            () => BeginTurnForCurrentPlayer(true, false)
        );
    }

    private void HandleCheckConfirmed()
    {
        if (currentPhase != HotSeatPhase.TurnLoop ||
            lastDeclarerIndex < 0)
            return;

        if (turnManager != null)
            turnManager.StopTurn();

        string declaredRank = bidController != null
            ? bidController.CurrentBid
            : string.Empty;

        bool declaredRankExists =
            EvaluateDeclaredRankExists(declaredRank);

        int loserIndex = declaredRankExists
            ? currentPlayerIndex
            : lastDeclarerIndex;

        HotSeatPlayer loser = players[loserIndex];
        bool eliminated = ApplyLoss(loser);

        if (GetActivePlayerCount() <= 1)
        {
            ShowGameOver();
            return;
        }

        string result = declaredRankExists
            ? loser.Name + " przegrywa — układ istnieje."
            : loser.Name + " przegrywa — układu nie ma.";

        if (eliminated)
            result += "\n" + loser.Name + " odpada z gry.";
        else
            result += "\nKolejna runda: " +
                      GetCardCountMessage(loser);

        currentPlayerIndex = loserIndex;
        ShowRoundResult(result);
    }

    private void BeginTurnForCurrentPlayer(
        bool canCheck,
        bool beginNewRound)
    {
        currentPhase = HotSeatPhase.TurnLoop;
        cardVisible = false;
        waitingForCardReveal = true;
        pendingCanCheck = canCheck;
        pendingBeginNewRound = beginNewRound;
        cardPanel.SetActive(true);
        ShowCardBack();
    }

    private void EnableTurnActionsAfterCardReveal()
    {
        if (currentPhase != HotSeatPhase.TurnLoop ||
            !waitingForCardReveal)
            return;

        waitingForCardReveal = false;

        if (bidController != null)
        {
            if (pendingBeginNewRound)
                bidController.BeginNewRound();
            else
                bidController.BeginTurn(pendingCanCheck);

            return;
        }

        if (turnManager != null)
            turnManager.BeginTurn(pendingCanCheck, "");
    }

    private void ChooseCardBackForThisGame()
    {
        int backCount = cardBackDatabase != null
            ? cardBackDatabase.BackCount
            : 0;

        if (backCount > 0)
            cardBackIndex = Random.Range(0, backCount);
    }

    private bool ApplyLoss(HotSeatPlayer player)
    {
        if (player.PenaltyGoingUp)
        {
            player.CardCount++;

            if (player.CardCount >= 3)
            {
                player.CardCount = 3;
                player.PenaltyGoingUp = false;
            }

            return false;
        }

        if (player.CardCount > 1)
        {
            player.CardCount--;
            return false;
        }

        player.CardCount = 0;
        player.Eliminated = true;
        return true;
    }

    private void ShowRoundResult(string result)
    {
        currentPhase = HotSeatPhase.RoundResult;
        cardVisible = false;
        cardPanel.SetActive(true);

        currentPlayerNameText.text = "WYNIK RUNDY";
        ShowCardSprite(
            null,
            "",
            cardFrontColor,
            Color.black
        );

        ShowRoundCards();
        cardImage.color = new Color(0.04f, 0.22f, 0.12f, 1f);
        instructionText.text = result +
            "\n\nDOTKNIJ EKRANU\nABY ZACZĄĆ KOLEJNĄ RUNDĘ";
    }

    private void ShowGameOver()
    {
        currentPhase = HotSeatPhase.GameOver;
        cardVisible = false;
        cardPanel.SetActive(true);

        HotSeatPlayer winner = GetLastActivePlayer();
        string winnerName = winner != null ? winner.Name : "BRAK ZWYCIĘZCY";

        currentPlayerNameText.text = "KONIEC GRY";
        ShowCardSprite(
            null,
            "ZWYCIĘZCA\n" + winnerName,
            cardFrontColor,
            Color.black
        );

        instructionText.text =
            "NACIŚNIJ KARTĘ, ABY ZAGRAĆ PONOWNIE";
    }

    private void RestartGame()
    {
        foreach (HotSeatPlayer player in players)
        {
            player.CardCount = 1;
            player.Eliminated = false;
            player.PenaltyGoingUp = true;
            player.Cards.Clear();
        }

        StartNewRound();
    }

    private HotSeatPlayer GetLastActivePlayer()
    {
        foreach (HotSeatPlayer player in players)
        {
            if (!player.Eliminated)
                return player;
        }

        return null;
    }

    private int GetRandomActivePlayerIndex()
    {
        List<int> activeIndexes = new List<int>();

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].Eliminated)
                activeIndexes.Add(i);
        }

        if (activeIndexes.Count == 0)
            return -1;

        return activeIndexes[Random.Range(0, activeIndexes.Count)];
    }

    private int GetNextRoundStarterIndex()
    {
        if (currentPlayerIndex >= 0 &&
            currentPlayerIndex < players.Count &&
            !players[currentPlayerIndex].Eliminated)
        {
            return currentPlayerIndex;
        }

        return GetNextActivePlayerIndex(currentPlayerIndex);
    }

    private string GetCardCountMessage(HotSeatPlayer player)
    {
        string cardWord = player.CardCount == 1 ? "kartę" :
            player.CardCount <= 4 ? "karty" : "kart";

        return player.Name + " ma teraz " +
            player.CardCount + " " + cardWord + ".";
    }

    private string BuildPlayerCardsText(HotSeatPlayer player)
    {
        List<string> cardNames = new List<string>();

        foreach (CardSpriteEntry card in player.Cards)
            cardNames.Add(GetCardDisplayName(card).Replace("\n", " "));

        return string.Join("\n", cardNames);
    }

    private string BuildAllCardsText()
    {
        List<string> lines = new List<string>();

        foreach (HotSeatPlayer player in players)
        {
            if (player.Eliminated && player.Cards.Count == 0)
                continue;

            lines.Add(player.Name + ": " +
                BuildPlayerCardsText(player).Replace("\n", ", "));
        }

        return string.Join("\n", lines);
    }

    private bool EvaluateDeclaredRankExists(string declaredText)
    {
        string handId = FindHandId(declaredText);
        if (string.IsNullOrEmpty(handId))
            return false;

        Dictionary<CardRank, int> rankCounts =
            new Dictionary<CardRank, int>();
        Dictionary<CardSuit, HashSet<CardRank>> suitRanks =
            new Dictionary<CardSuit, HashSet<CardRank>>();

        foreach (HotSeatPlayer player in players)
        {
            if (player.Eliminated)
                continue;

            foreach (CardSpriteEntry card in player.Cards)
            {
                if (card == null)
                    continue;

                if (!rankCounts.ContainsKey(card.rank))
                    rankCounts[card.rank] = 0;

                rankCounts[card.rank]++;

                if (!suitRanks.ContainsKey(card.suit))
                    suitRanks[card.suit] = new HashSet<CardRank>();

                suitRanks[card.suit].Add(card.rank);
            }
        }

        if (handId.StartsWith("HIGH_"))
            return GetRankCount(rankCounts, GetRank(handId.Substring(5))) >= 1;

        if (handId.StartsWith("PAIR_"))
            return GetRankCount(rankCounts, GetRank(handId.Substring(5))) >= 2;

        if (handId.StartsWith("TRIPS_"))
            return GetRankCount(rankCounts, GetRank(handId.Substring(6))) >= 3;

        if (handId.StartsWith("QUADS_"))
            return GetRankCount(rankCounts, GetRank(handId.Substring(6))) >= 4;

        if (handId.StartsWith("TWOPAIR_"))
        {
            string[] parts = handId.Split('_');
            return parts.Length == 3 &&
                GetRankCount(rankCounts, GetRank(parts[1])) >= 2 &&
                GetRankCount(rankCounts, GetRank(parts[2])) >= 2;
        }

        if (handId.StartsWith("FULL_"))
        {
            string[] parts = handId.Split('_');
            return parts.Length == 3 &&
                GetRankCount(rankCounts, GetRank(parts[1])) >= 3 &&
                GetRankCount(rankCounts, GetRank(parts[2])) >= 2;
        }

        if (handId == "STRAIGHT_SMALL")
            return HasRanks(rankCounts, CardRank.Nine, CardRank.Ten,
                CardRank.Jack, CardRank.Queen, CardRank.King);

        if (handId == "STRAIGHT_BIG")
            return HasRanks(rankCounts, CardRank.Ten, CardRank.Jack,
                CardRank.Queen, CardRank.King, CardRank.Ace);

        if (handId.StartsWith("FLUSH_"))
            return HasFlush(suitRanks, GetSuit(handId.Substring(6)));

        if (handId.StartsWith("POKER_SMALL_"))
            return HasStraightFlush(suitRanks,
                GetSuit(handId.Substring(12)), CardRank.Nine,
                CardRank.Ten, CardRank.Jack, CardRank.Queen,
                CardRank.King);

        if (handId.StartsWith("POKER_BIG_"))
            return HasStraightFlush(suitRanks,
                GetSuit(handId.Substring(10)), CardRank.Ten,
                CardRank.Jack, CardRank.Queen, CardRank.King,
                CardRank.Ace);

        return false;
    }

    private string FindHandId(string displayName)
    {
        foreach (string handId in HandRankCatalog.GetAllIds())
        {
            if (string.Equals(HandRankCatalog.GetDisplayName(handId),
                displayName, System.StringComparison.OrdinalIgnoreCase))
            {
                return handId;
            }
        }

        return string.Empty;
    }

    private int GetRankCount(
        Dictionary<CardRank, int> counts,
        CardRank? rank)
    {
        if (!rank.HasValue)
            return 0;

        return counts.TryGetValue(rank.Value, out int count)
            ? count
            : 0;
    }

    private bool HasRanks(
        Dictionary<CardRank, int> counts,
        params CardRank[] requiredRanks)
    {
        foreach (CardRank rank in requiredRanks)
        {
            if (GetRankCount(counts, rank) <= 0)
                return false;
        }

        return true;
    }

    private bool HasFlush(
        Dictionary<CardSuit, HashSet<CardRank>> suitRanks,
        CardSuit? suit)
    {
        return suit.HasValue &&
            suitRanks.TryGetValue(suit.Value, out HashSet<CardRank> ranks) &&
            ranks.Count >= 5;
    }

    private bool HasStraightFlush(
        Dictionary<CardSuit, HashSet<CardRank>> suitRanks,
        CardSuit? suit,
        params CardRank[] requiredRanks)
    {
        if (!suit.HasValue ||
            !suitRanks.TryGetValue(suit.Value, out HashSet<CardRank> ranks))
        {
            return false;
        }

        foreach (CardRank rank in requiredRanks)
        {
            if (!ranks.Contains(rank))
                return false;
        }

        return true;
    }

    private CardRank? GetRank(string value)
    {
        switch (value)
        {
            case "9": return CardRank.Nine;
            case "10": return CardRank.Ten;
            case "J": return CardRank.Jack;
            case "Q": return CardRank.Queen;
            case "K": return CardRank.King;
            case "A": return CardRank.Ace;
            default: return null;
        }
    }

    private CardSuit? GetSuit(string value)
    {
        switch (value)
        {
            case "DIAMOND": return CardSuit.Karo;
            case "HEART": return CardSuit.Kier;
            case "CLUB": return CardSuit.Trefl;
            case "SPADE": return CardSuit.Pik;
            default: return null;
        }
    }

    private void DealCards()
    {
        List<CardSpriteEntry> deck = CreateDeck();

        if (deck.Count == 0)
            return;

        Shuffle(deck);

        int deckIndex = 0;

        foreach (HotSeatPlayer player in players)
        {
            player.Cards.Clear();

            if (player.Eliminated)
                continue;

            for (int i = 0; i < player.CardCount; i++)
            {
                if (deckIndex >= deck.Count)
                    break;

                player.Cards.Add(deck[deckIndex]);
                deckIndex++;
            }
        }
    }

    private List<CardSpriteEntry> CreateDeck()
    {
        List<CardSpriteEntry> deck =
            new List<CardSpriteEntry>();

        if (cardDatabase == null ||
            cardDatabase.cards == null ||
            cardDatabase.cards.Length == 0)
        {
            Debug.LogError(
                "HotSeatSetupUI: CardDatabase nie zawiera kart."
            );

            return deck;
        }

        foreach (CardSpriteEntry card in cardDatabase.cards)
        {
            if (card != null)
                deck.Add(card);
        }

        return deck;
    }

    private void Shuffle(List<CardSpriteEntry> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);

            CardSpriteEntry temporaryCard = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temporaryCard;
        }
    }

    private int GetNextActivePlayerIndex(int fromIndex)
    {
        int index = fromIndex;

        for (int i = 0; i < players.Count; i++)
        {
            index++;

            if (index >= players.Count)
                index = 0;

            if (!players[index].Eliminated)
                return index;
        }

        return fromIndex;
    }

    private int GetActivePlayerCount()
    {
        int count = 0;

        foreach (HotSeatPlayer player in players)
        {
            if (!player.Eliminated)
                count++;
        }

        return count;
    }

    private string GetCardDisplayName(CardSpriteEntry card)
    {
        if (card == null)
            return "BRAK KARTY";

        return GetRankDisplayName(card.rank) +
               "\n" +
               GetSuitDisplayName(card.suit);
    }

    private string GetRankDisplayName(CardRank rank)
    {
        switch (rank)
        {
            case CardRank.Nine:
                return "9";

            case CardRank.Ten:
                return "10";

            case CardRank.Jack:
                return "WALET";

            case CardRank.Queen:
                return "DAMA";

            case CardRank.King:
                return "KRÓL";

            case CardRank.Ace:
                return "AS";

            default:
                return rank.ToString().ToUpper();
        }
    }

    private string GetSuitDisplayName(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Karo:
                return "KARO ♦";

            case CardSuit.Kier:
                return "KIER ♥";

            case CardSuit.Trefl:
                return "ŻOŁĄDŹ ♣";

            case CardSuit.Pik:
                return "PIK ♠";

            default:
                return suit.ToString().ToUpper();
        }
    }

    private string CleanName(string value)
    {
        string result = "";

        foreach (char character in value.ToUpper())
        {
            if (char.IsLetterOrDigit(character))
                result += character;

            if (result.Length >= maxNameLength)
                break;
        }

        return result;
    }

    private void RefreshButtons()
    {
        addPlayerButton.interactable =
            playerInputs.Count < maxPlayers;

        startButton.interactable =
            playerInputs.Count >= minPlayers;
    }

    private void ApplySetupStyle()
    {
        Image background = setupPanel != null
            ? setupPanel.GetComponent<Image>()
            : null;

        if (background != null)
            background.color = new Color(0.015f, 0.08f, 0.045f, 0.98f);

        StyleButton(addPlayerButton);
        StyleButton(startButton);
    }

    private void StyleButton(Button button)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            if (pokerButtonSprite != null)
            {
                image.sprite = pokerButtonSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            image.color = Color.white;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(720f, 126f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
        }
    }

    private void EnsureCardText()
    {
        if (cardText != null)
            return;

        GameObject textObject =
            new GameObject("HS_CardText");

        textObject.transform.SetParent(
            cardImage.transform,
            false
        );

        RectTransform rectTransform =
            textObject.AddComponent<RectTransform>();

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        cardText =
            textObject.AddComponent<TextMeshProUGUI>();

        cardText.alignment =
            TextAlignmentOptions.Center;

        cardText.fontSize = 70;
        cardText.fontStyle = FontStyles.Bold;
        cardText.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (bidController != null)
        {
            bidController.RaiseConfirmed -= HandleRaiseConfirmed;
            bidController.CheckConfirmed -= HandleCheckConfirmed;
        }
    }
}
