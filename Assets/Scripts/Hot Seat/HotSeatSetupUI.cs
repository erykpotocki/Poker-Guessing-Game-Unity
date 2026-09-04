using System.Collections;
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
        RoundPause,
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
    private Button previewContinueButton;
    private Vector2 playerListBasePosition;
    private ScrollRect playerListScrollRect;
    private RectTransform playerListViewport;
    private Coroutine inputFocusRoutine;
    private CanvasGroup cardPanelCanvasGroup;
    private Coroutine cardPanelEntranceRoutine;
    private Coroutine previewTransitionRoutine;
    private Coroutine unseenCardSparkleRoutine;
    private readonly List<GameObject> unseenCardSparkles = new List<GameObject>();
    private readonly Vector3[] inputWorldCorners = new Vector3[4];

    private sealed class RoundRevealCard
    {
        public CardSpriteEntry Card;
        public Sprite FrontSprite;
        public Image Image;
        public Outline Outline;
    }

    private readonly List<GameObject> roundResultObjects =
        new List<GameObject>();

    private readonly List<RoundRevealCard> roundRevealCards =
        new List<RoundRevealCard>();

    private Coroutine roundRevealCoroutine;
    private bool roundRevealInProgress;

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
    private int pendingNextRoundStarterIndex = -1;
    private int roundNumber;
    private bool previewCardSeen;

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
            cardImage.preserveAspect = false;

        ApplySetupStyle();
        CreateSetupHeader();
        CreateSetupBackButton();
        EnsureScrollablePlayerList();
        ApplyCardScreenStyle();
        CreatePreviewContinueButton();
        if (playerListRoot is RectTransform listRect)
            playerListBasePosition = listRect.anchoredPosition;
        StartCoroutine(AnimateSetupEntrance());
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
        input.text = GetDefaultPlayerName(playerNumber);

        input.onValueChanged.AddListener(value =>
        {
            string cleaned = CleanName(value);

            if (cleaned != value)
                input.SetTextWithoutNotify(cleaned);
        });

        input.onSelect.AddListener(_ => BeginEditingPlayerName(input));
        input.onDeselect.AddListener(_ => FinishEditingPlayerName(input));

        CreateRemovePlayerButton(row, input);
        StylePlayerInput(input);
        playerInputs.Add(input);
        RefreshButtons();
        StartCoroutine(RevealNewPlayerRow(row.transform));
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
            if (IsDefaultPlayerName(playerInputs[i].text))
            {
                playerInputs[i].SetTextWithoutNotify(
                    GetDefaultPlayerName(i + 1)
                );
            }
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
        roundNumber = 0;

        ChooseCardBackForThisGame();

        for (int i = 0; i < playerInputs.Count; i++)
        {
            string playerName =
                CleanName(playerInputs[i].text);

            if (string.IsNullOrWhiteSpace(playerName))
                playerName = GetDefaultPlayerName(i + 1);

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
        roundNumber++;
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
        cardPanel.SetActive(false);

        if (passPhoneUI != null)
            passPhoneUI.Hide();

        if (turnManager != null)
            turnManager.StopTurn();

        if (passPhoneUI != null)
        {
            passPhoneUI.ShowDealIntro(
                roundNumber,
                players[currentPlayerIndex].Name,
                BeginRoundPreview
            );
            return;
        }

        BeginRoundPreview();
    }

    private void OnCardClicked()
    {
        if (currentPhase == HotSeatPhase.RoundResult)
        {
            if (roundRevealInProgress)
                return;

            StartNewRound(pendingNextRoundStarterIndex);
            return;
        }

        if (currentPhase == HotSeatPhase.RoundPause)
        {
            StartNewRound(pendingNextRoundStarterIndex);
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
            ShowCardBack();
            return;
        }

        ShowCardBack();
    }

    private void ShowCardBack()
    {
        cardVisible = false;

        HotSeatPlayer player = players[currentPlayerIndex];

        ShowCurrentPlayerHeading(player);

        Sprite backSprite =
            cardBackDatabase.GetBackSprite(cardBackIndex);

        if (player.CardCount > 1 && backSprite != null)
            ShowPlayerCardBacks(player.CardCount, backSprite);
        else
            ShowCardSprite(
                backSprite,
                "REWERS",
                cardBackColor,
                Color.white
            );

        instructionText.text =
            "UPEWNIJ SIĘ, ŻE NIKT NIE PATRZY\n" +
            "NACIŚNIJ KARTĘ, ŻEBY ODKRYĆ";

        if (previewContinueButton != null)
            previewContinueButton.gameObject.SetActive(
                currentPhase == HotSeatPhase.FirstCardPreview && previewCardSeen
            );
    }

    private void ShowCardFront()
    {
        ClearUnseenCardSparkles();
        cardVisible = true;

        HotSeatPlayer player = players[currentPlayerIndex];

        if (currentPhase == HotSeatPhase.FirstCardPreview)
        {
            previewCardSeen = true;
            if (previewContinueButton != null)
                previewContinueButton.gameObject.SetActive(true);
        }

        ShowCurrentPlayerHeading(player);

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

    private void ShowCurrentPlayerHeading(HotSeatPlayer player)
    {
        if (currentPlayerNameText == null || player == null)
            return;

        currentPlayerNameText.text = "TERAZ: " + player.Name.ToUpperInvariant();
        currentPlayerNameText.transform.SetAsLastSibling();
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
        // Every front and back fills the same physical card rectangle.
        // Source images have different aspect ratios and transparent margins,
        // so preserveAspect made the ornate back visibly narrower.
        cardImage.preserveAspect = false;

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
        List<Sprite> sprites = new List<Sprite>();
        foreach (CardSpriteEntry card in player.Cards)
        {
            Sprite sprite = card.sprite;

            if (sprite == null && cardDatabase != null)
                sprite = cardDatabase.GetCardSprite(card.suit, card.rank);

            sprites.Add(sprite);
        }

        ShowCardFan(sprites);
    }

    private void ShowPlayerCardBacks(int cardCount, Sprite backSprite)
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < cardCount; i++)
            sprites.Add(backSprite);

        ShowCardFan(sprites);
    }

    private void ShowCardFan(List<Sprite> sprites)
    {
        ClearRoundResultObjects();
        ClearExtraCardImages();
        EnsureCardText();

        int cardCount = Mathf.Clamp(sprites.Count, 1, 3);
        float scale = cardCount == 2 ? 0.76f : 0.64f;
        float cardWidth = 690f * scale;
        float cardHeight = 960f * scale;
        float horizontalStep = cardCount == 2 ? 145f : 120f;
        const float verticalStep = 22f;
        const float rotationStep = 8f;

        cardText.text = "";
        cardText.gameObject.SetActive(false);

        for (int i = 0; i < cardCount; i++)
        {
            Sprite sprite = sprites[i];

            Image image = i == 0 ? cardImage : CreateExtraCardImage();
            RectTransform rect = image.rectTransform;
            float centeredIndex = i - (cardCount - 1) * 0.5f;

            rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            rect.anchoredPosition = new Vector2(
                centeredIndex * horizontalStep,
                45f - Mathf.Abs(centeredIndex) * verticalStep
            );
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                -centeredIndex * rotationStep
            );
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : cardFrontColor;
            image.preserveAspect = false;

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
        rect.anchoredPosition = new Vector2(0f, 30f);
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
        rect.sizeDelta = new Vector2(760f, 1058f);
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = new Vector2(0f, 90f);
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

        int totalCards = 0;
        foreach (HotSeatPlayer player in resultPlayers)
            totalCards += Mathf.Min(player.Cards.Count, 3);

        float cardWidth = totalCards <= 6 ? 150f :
            totalCards <= 10 ? 118f : 94f;
        float cardHeight = cardWidth * 1.39f;
        float spacing = cardWidth * 0.78f;

        for (int row = 0; row < resultPlayers.Count; row++)
        {
            HotSeatPlayer player = resultPlayers[row];
            Vector2 seatPosition = GetResultSeatPosition(
                row,
                resultPlayers.Count
            );
            CreateRoundResultLabel(
                player.Name,
                seatPosition + new Vector2(0f, cardHeight * 0.62f)
            );

            int visibleCards = Mathf.Min(player.Cards.Count, 3);
            float firstX = -spacing * (visibleCards - 1) * 0.5f;

            for (int i = 0; i < visibleCards; i++)
            {
                CardSpriteEntry card = player.Cards[i];
                Sprite sprite = card.sprite;

                if (sprite == null && cardDatabase != null)
                    sprite = cardDatabase.GetCardSprite(card.suit, card.rank);

                CreateRoundResultCard(
                    card,
                    sprite,
                    seatPosition + new Vector2(firstX + spacing * i, -28f),
                    new Vector2(cardWidth, cardHeight)
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
        rect.sizeDelta = new Vector2(700f, 480f);

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

    private void CreateRoundResultCard(
        CardSpriteEntry card,
        Sprite frontSprite,
        Vector2 position,
        Vector2 size)
    {
        GameObject cardObject = new GameObject("HS_ResultCard");
        cardObject.transform.SetParent(cardImage.transform, false);

        RectTransform rect = cardObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = cardObject.AddComponent<Image>();
        Sprite backSprite = cardBackDatabase != null
            ? cardBackDatabase.GetBackSprite(cardBackIndex)
            : null;
        image.sprite = backSprite;
        image.color = backSprite != null ? Color.white : cardBackColor;
        image.preserveAspect = false;
        image.raycastTarget = false;

        Outline outline = cardObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.75f, 0.12f, 1f);
        outline.effectDistance = new Vector2(8f, -8f);
        outline.useGraphicAlpha = true;
        outline.enabled = false;

        roundRevealCards.Add(new RoundRevealCard
        {
            Card = card,
            FrontSprite = frontSprite,
            Image = image,
            Outline = outline
        });

        roundResultObjects.Add(cardObject);
    }

    private IEnumerator RevealCheckedHandRoutine(
        string handId,
        string declaredRank,
        bool declaredRankExists,
        string result)
    {
        List<int> order = new List<int>();
        for (int i = 0; i < roundRevealCards.Count; i++)
            order.Add(i);

        ShuffleIndexes(order);

        List<CardSpriteEntry> revealedCards = new List<CardSpriteEntry>();
        yield return new WaitForSeconds(0.45f);

        bool confirmed = false;
        for (int step = 0; step < order.Count; step++)
        {
            RoundRevealCard reveal = roundRevealCards[order[step]];
            RevealResultCard(
                reveal,
                handId,
                new Color(1f, 0.75f, 0.12f, 1f)
            );
            revealedCards.Add(reveal.Card);

            if (EvaluateCardsForHand(handId, revealedCards))
            {
                confirmed = true;
                Color success = new Color(0.22f, 0.9f, 0.38f, 1f);

                foreach (RoundRevealCard remaining in roundRevealCards)
                    RevealResultCard(remaining, handId, success);

                break;
            }

            yield return new WaitForSeconds(0.42f);
        }

        Color finalColor = confirmed
            ? new Color(0.22f, 0.9f, 0.38f, 1f)
            : new Color(1f, 0.25f, 0.22f, 1f);

        foreach (RoundRevealCard reveal in roundRevealCards)
        {
            bool relevant = IsCardRelevantToHand(handId, reveal.Card);
            if (reveal.Outline != null)
            {
                reveal.Outline.enabled = relevant;
                reveal.Outline.effectColor = finalColor;
            }

            if (reveal.Image != null)
            {
                reveal.Image.rectTransform.localScale = relevant
                    ? Vector3.one * 1.08f
                    : Vector3.one;
                Color baseCardColor = reveal.Image.sprite != null
                    ? Color.white
                    : cardFrontColor;
                reveal.Image.color = relevant
                    ? Color.Lerp(baseCardColor, finalColor, 0.16f)
                    : baseCardColor;

                if (relevant)
                    reveal.Image.transform.SetAsLastSibling();
            }
        }

        // Keep player labels readable after matching cards are brought forward.
        foreach (GameObject resultObject in roundResultObjects)
        {
            if (resultObject != null && resultObject.name == "HS_ResultPlayerName")
                resultObject.transform.SetAsLastSibling();
        }

        string verdict = declaredRankExists
            ? "<color=#57E878>UKŁAD JEST NA STOLE</color>"
            : "<color=#FF6660>UKŁADU NIE MA NA STOLE</color>";

        instructionText.text =
            "SPRAWDZONO: <color=#F2C14E>" +
            declaredRank.ToUpper() + "</color>\n" + verdict +
            "\n\n" + result +
            "\n\nDOTKNIJ, ABY PRZEJŚĆ DALEJ";

        roundRevealInProgress = false;
        roundRevealCoroutine = null;
    }

    private void RevealResultCard(
        RoundRevealCard reveal,
        string handId,
        Color matchColor)
    {
        if (reveal == null || reveal.Image == null)
            return;

        reveal.Image.sprite = reveal.FrontSprite;
        reveal.Image.color = reveal.FrontSprite != null
            ? Color.white
            : cardFrontColor;

        bool relevant = IsCardRelevantToHand(handId, reveal.Card);
        if (reveal.Outline != null)
        {
            reveal.Outline.enabled = relevant;
            reveal.Outline.effectColor = matchColor;
        }

        reveal.Image.rectTransform.localScale = relevant
            ? Vector3.one * 1.04f
            : Vector3.one;
    }

    private static void ShuffleIndexes(List<int> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            int randomIndex = Random.Range(i, values.Count);
            int temporary = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = temporary;
        }
    }

    private void ClearRoundResultObjects()
    {
        if (roundRevealCoroutine != null)
        {
            StopCoroutine(roundRevealCoroutine);
            roundRevealCoroutine = null;
            roundRevealInProgress = false;
        }

        foreach (GameObject resultObject in roundResultObjects)
        {
            if (resultObject != null)
                Destroy(resultObject);
        }

        roundResultObjects.Clear();
        roundRevealCards.Clear();
    }

    private void HideCardAndContinuePreview()
    {
        if (currentPhase != HotSeatPhase.FirstCardPreview || !previewCardSeen ||
            previewTransitionRoutine != null)
            return;

        previewTransitionRoutine = StartCoroutine(TransitionToNextPreview());
    }

    private IEnumerator TransitionToNextPreview()
    {
        if (previewContinueButton != null)
            previewContinueButton.interactable = false;

        if (cardPanelCanvasGroup == null && cardPanel != null)
        {
            cardPanelCanvasGroup = cardPanel.GetComponent<CanvasGroup>();
            if (cardPanelCanvasGroup == null)
                cardPanelCanvasGroup = cardPanel.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        const float fadeOutDuration = 0.18f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (cardPanelCanvasGroup != null)
                cardPanelCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        cardVisible = false;
        firstPreviewCount++;

        if (firstPreviewCount >= GetActivePlayerCount())
        {
            if (cardPanelCanvasGroup != null)
                cardPanelCanvasGroup.alpha = 1f;
            previewTransitionRoutine = null;
            ShowPassPhoneScreen();
            yield break;
        }

        currentPlayerIndex =
            GetNextActivePlayerIndex(currentPlayerIndex);

        previewCardSeen = false;
        ShowCardBack();

        elapsed = 0f;
        const float fadeInDuration = 0.24f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (cardPanelCanvasGroup != null)
                cardPanelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        if (cardPanelCanvasGroup != null)
            cardPanelCanvasGroup.alpha = 1f;
        if (previewContinueButton != null)
            previewContinueButton.interactable = true;

        PlayUnseenCardSparkles();
        previewTransitionRoutine = null;
    }

    private void PlayUnseenCardSparkles()
    {
        ClearUnseenCardSparkles();
        if (cardImage == null)
            return;

        Vector2[] anchors =
        {
            new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.82f),
            new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.18f)
        };

        foreach (Vector2 anchor in anchors)
        {
            GameObject sparkle = new GameObject(
                "UnseenCardSparkle", typeof(RectTransform), typeof(TextMeshProUGUI));
            sparkle.transform.SetParent(cardImage.transform, false);
            RectTransform rect = sparkle.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(90f, 90f);
            rect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = sparkle.GetComponent<TextMeshProUGUI>();
            text.text = "✦";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 58f;
            text.color = new Color(1f, 0.82f, 0.28f, 0f);
            text.raycastTarget = false;
            unseenCardSparkles.Add(sparkle);
        }

        unseenCardSparkleRoutine = StartCoroutine(AnimateUnseenCardSparkles());
    }

    private IEnumerator AnimateUnseenCardSparkles()
    {
        float elapsed = 0f;
        const float cycleDuration = 1.8f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < unseenCardSparkles.Count; i++)
            {
                GameObject sparkle = unseenCardSparkles[i];
                if (sparkle == null)
                    continue;

                float phase = Mathf.Repeat(
                    elapsed / cycleDuration + i * 0.19f, 1f);
                float glow = Mathf.Pow(Mathf.Sin(phase * Mathf.PI), 2f);
                TMP_Text text = sparkle.GetComponent<TMP_Text>();
                if (text != null)
                    text.color = new Color(1f, 0.82f, 0.28f,
                        Mathf.Lerp(0.16f, 0.95f, glow));
                sparkle.transform.localScale = Vector3.one *
                    Mathf.Lerp(0.65f, 1.18f, glow) * (i % 2 == 0 ? 1f : 0.86f);
            }
            yield return null;
        }
    }

    private void ClearUnseenCardSparkles()
    {
        if (unseenCardSparkleRoutine != null)
        {
            StopCoroutine(unseenCardSparkleRoutine);
            unseenCardSparkleRoutine = null;
        }

        foreach (GameObject sparkle in unseenCardSparkles)
        {
            if (sparkle != null)
                Destroy(sparkle);
        }
        unseenCardSparkles.Clear();
    }

    private void ShowPassPhoneScreen()
    {
        cardPanel.SetActive(false);

        if (passPhoneUI != null)
        {
            passPhoneUI.ShowRoundStart(
                roundNumber,
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

        int winnerIndex = declaredRankExists
            ? lastDeclarerIndex
            : currentPlayerIndex;

        HotSeatPlayer loser = players[loserIndex];
        bool eliminated = ApplyLoss(loser);

        if (GetActivePlayerCount() <= 1)
        {
            ShowGameOver();
            return;
        }

        HotSeatPlayer roundWinner = players[winnerIndex];
        string result =
            "<color=#F2C14E>WYGRYWA: " + roundWinner.Name.ToUpper() + "</color>\n" +
            "<color=#FF6B6B>PRZEGRYWA: " + loser.Name.ToUpper() + "</color>";

        if (eliminated)
            result += "\n" + loser.Name + " odpada z gry.";
        else
            result += "\nKolejna runda: " +
                      GetCardCountMessage(loser);

        currentPlayerIndex = loserIndex;
        pendingNextRoundStarterIndex = GetNextRoundStarterIndex();
        ShowRoundResult(result, declaredRank, declaredRankExists);
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
        ShowCardPanelAnimated();
        if (previewContinueButton != null)
            previewContinueButton.gameObject.SetActive(false);
        ShowCardBack();
    }

    private void BeginRoundPreview()
    {
        currentPhase = HotSeatPhase.FirstCardPreview;
        cardVisible = false;
        previewCardSeen = false;
        ShowCardPanelAnimated();
        ShowCardBack();
    }

    private void CreatePreviewContinueButton()
    {
        if (cardPanel == null || previewContinueButton != null)
            return;

        GameObject buttonObject = new GameObject(
            "HS_PreviewContinueButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(cardPanel.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 126f);
        rect.sizeDelta = new Vector2(790f, 136f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = pokerButtonSprite;
        image.type = Image.Type.Simple;
        image.color = new Color(0.34f, 0.055f, 0.035f, 1f);

        previewContinueButton = buttonObject.GetComponent<Button>();
        previewContinueButton.targetGraphic = image;
        previewContinueButton.onClick.AddListener(HideCardAndContinuePreview);
        PokerButtonTheme.ApplyTo(previewContinueButton);

        GameObject labelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "ZAKRYJ I PODAJ DALEJ";
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 29f;
        label.fontSizeMax = 41f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;

        buttonObject.SetActive(false);
    }

    private IEnumerator RevealNewPlayerRow(Transform row)
    {
        yield return null;
        if (row == null)
            yield break;

        CanvasGroup group = row.GetComponent<CanvasGroup>();
        if (group == null)
            group = row.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        row.localScale = Vector3.one * 0.94f;

        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            row.localScale = Vector3.LerpUnclamped(Vector3.one * 0.94f, Vector3.one, t);
            group.alpha = t;
            yield return null;
        }

        row.localScale = Vector3.one;
        group.alpha = 1f;
    }

    private void BeginEditingPlayerName(TMP_InputField input)
    {
        if (input == null)
            return;

        input.ActivateInputField();
        StartCoroutine(SelectWholePlayerName(input));
        FocusPlayerInput(input);
    }

    private static IEnumerator SelectWholePlayerName(TMP_InputField input)
    {
        // TMP updates the caret once more after the pointer event. Waiting one
        // frame keeps the familiar mobile behaviour: tap, select all, type.
        yield return null;
        if (input == null || !input.isFocused)
            yield break;

        input.caretPosition = input.text.Length;
        input.selectionStringAnchorPosition = 0;
        input.selectionStringFocusPosition = input.text.Length;
        input.ForceLabelUpdate();
    }

    private void FinishEditingPlayerName(TMP_InputField input)
    {
        if (input == null)
            return;

        if (string.IsNullOrWhiteSpace(input.text))
        {
            int index = playerInputs.IndexOf(input);
            if (index >= 0)
                input.SetTextWithoutNotify(GetDefaultPlayerName(index + 1));
        }

        StartCoroutine(RestoreDefaultPlayerListView());
    }

    private IEnumerator RestoreDefaultPlayerListView()
    {
        yield return null;
        foreach (TMP_InputField playerInput in playerInputs)
        {
            if (playerInput != null && playerInput.isFocused)
                yield break;
        }

        if (playerListRoot is RectTransform listRect)
            listRect.anchoredPosition = playerListBasePosition;
        if (playerListScrollRect != null)
        {
            playerListScrollRect.StopMovement();
            playerListScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void FocusPlayerInput(TMP_InputField input)
    {
        if (inputFocusRoutine != null)
            StopCoroutine(inputFocusRoutine);
        inputFocusRoutine = StartCoroutine(MoveFocusedInputToTop(input));
    }

    private IEnumerator MoveFocusedInputToTop(TMP_InputField input)
    {
        // Track the keyboard while this field stays focused. Mobile browsers
        // can report its final area several frames after focus.
        while (input != null && input.isFocused)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (!(playerListRoot is RectTransform listRect) ||
                !(input.transform is RectTransform inputRect))
                continue;

            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);

            if (playerListScrollRect != null && playerListViewport != null)
            {
                inputRect.GetWorldCorners(inputWorldCorners);
                Vector3 inputTopWorld = inputWorldCorners[1];
                Vector3 viewportTopWorld = playerListViewport.TransformPoint(
                    new Vector3(0f, playerListViewport.rect.yMax - 24f, 0f));
                Canvas listCanvas = input.GetComponentInParent<Canvas>();
                Camera listCamera = listCanvas != null &&
                    listCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? listCanvas.worldCamera
                    : null;
                float listScale = listCanvas != null
                    ? Mathf.Max(0.01f, listCanvas.scaleFactor)
                    : 1f;
                float inputTop = RectTransformUtility.WorldToScreenPoint(
                    listCamera, inputTopWorld).y;
                float viewportTop = RectTransformUtility.WorldToScreenPoint(
                    listCamera, viewportTopWorld).y;
                float scrollUp = viewportTop - inputTop;

                if (scrollUp > 0f)
                {
                    float maximumScrollY = Mathf.Max(playerListBasePosition.y,
                        playerListBasePosition.y + listRect.rect.height -
                        playerListViewport.rect.height);
                    listRect.anchoredPosition = new Vector2(
                        playerListBasePosition.x,
                        Mathf.Clamp(listRect.anchoredPosition.y + scrollUp / listScale,
                            playerListBasePosition.y, maximumScrollY));
                    playerListScrollRect.StopMovement();
                }
            }

            float keyboardHeight = GetKeyboardHeight();
            if (keyboardHeight <= 0f)
                continue;

            inputRect.GetWorldCorners(inputWorldCorners);
            Canvas canvas = input.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            float canvasScale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float inputBottom = RectTransformUtility.WorldToScreenPoint(
                eventCamera, inputWorldCorners[0]).y;
            const float keyboardClearancePixels = 42f;
            float overlapPixels = keyboardHeight + keyboardClearancePixels - inputBottom;

            if (overlapPixels <= 0f)
                continue;

            float targetY = listRect.anchoredPosition.y + overlapPixels / canvasScale;
            float maximumY = playerListViewport != null
                ? Mathf.Max(playerListBasePosition.y,
                    playerListBasePosition.y + listRect.rect.height - playerListViewport.rect.height)
                : targetY;

            listRect.anchoredPosition = new Vector2(
                playerListBasePosition.x,
                Mathf.Clamp(targetY, playerListBasePosition.y, maximumY));

            if (playerListScrollRect != null)
                playerListScrollRect.StopMovement();
        }

        inputFocusRoutine = null;
    }

    private static float GetKeyboardHeight()
    {
        if (!TouchScreenKeyboard.visible)
            return 0f;

        return Mathf.Max(0f, TouchScreenKeyboard.area.height);
    }

    private void EnsureScrollablePlayerList()
    {
        if (!(playerListRoot is RectTransform contentRect) ||
            contentRect.parent == null || playerListScrollRect != null)
            return;

        Transform originalParent = contentRect.parent;
        int siblingIndex = contentRect.GetSiblingIndex();

        GameObject viewportObject = new GameObject(
            "PlayerListViewport", typeof(RectTransform), typeof(Image),
            typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.layer = contentRect.gameObject.layer;
        viewportObject.transform.SetParent(originalParent, false);
        viewportObject.transform.SetSiblingIndex(siblingIndex);

        playerListViewport = viewportObject.GetComponent<RectTransform>();
        playerListViewport.anchorMin = new Vector2(0.5f, 1f);
        playerListViewport.anchorMax = new Vector2(0.5f, 1f);
        playerListViewport.pivot = new Vector2(0.5f, 1f);
        playerListViewport.anchoredPosition = new Vector2(0f, -480f);
        playerListViewport.sizeDelta = new Vector2(920f, 800f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        contentRect.SetParent(playerListViewport, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        ContentSizeFitter fitter = contentRect.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        playerListScrollRect = viewportObject.GetComponent<ScrollRect>();
        playerListScrollRect.viewport = playerListViewport;
        playerListScrollRect.content = contentRect;
        playerListScrollRect.horizontal = false;
        playerListScrollRect.vertical = true;
        playerListScrollRect.movementType = ScrollRect.MovementType.Clamped;
        playerListScrollRect.inertia = true;
        playerListScrollRect.decelerationRate = 0.12f;
        playerListScrollRect.scrollSensitivity = 70f;
    }

    private IEnumerator AnimateSetupEntrance()
    {
        if (setupPanel == null)
            yield break;

        CanvasGroup group = setupPanel.GetComponent<CanvasGroup>();
        if (group == null)
            group = setupPanel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        float elapsed = 0f;
        const float duration = 0.42f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            yield return null;
        }

        group.alpha = 1f;
    }

    private void ShowCardPanelAnimated()
    {
        if (cardPanel == null)
            return;

        cardPanel.SetActive(true);
        if (cardPanelCanvasGroup == null)
        {
            cardPanelCanvasGroup = cardPanel.GetComponent<CanvasGroup>();
            if (cardPanelCanvasGroup == null)
                cardPanelCanvasGroup = cardPanel.AddComponent<CanvasGroup>();
        }

        if (cardPanelEntranceRoutine != null)
            StopCoroutine(cardPanelEntranceRoutine);
        cardPanelEntranceRoutine = StartCoroutine(AnimateCardPanelEntrance());
    }

    private IEnumerator AnimateCardPanelEntrance()
    {
        cardPanelCanvasGroup.alpha = 0f;
        float elapsed = 0f;
        const float duration = 0.32f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cardPanelCanvasGroup.alpha = 1f - Mathf.Pow(1f - t, 3f);
            yield return null;
        }

        cardPanelCanvasGroup.alpha = 1f;
        cardPanelEntranceRoutine = null;
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

    private void ShowRoundResult(
        string result,
        string declaredRank,
        bool declaredRankExists)
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

        string handId = FindHandId(declaredRank);
        ShowRoundCards();
        cardImage.color = new Color(0.04f, 0.22f, 0.12f, 1f);

        instructionText.text =
            "SPRAWDZANY UKŁAD:\n" +
            "<color=#F2C14E>" + declaredRank.ToUpper() + "</color>\n\n" +
            "ODKRYWAM KARTY…";

        roundRevealInProgress = true;
        roundRevealCoroutine = StartCoroutine(
            RevealCheckedHandRoutine(
                handId,
                declaredRank,
                declaredRankExists,
                result
            )
        );
    }

    private void ShowRoundPause()
    {
        currentPhase = HotSeatPhase.RoundPause;
        cardVisible = false;
        cardPanel.SetActive(true);

        int starter = pendingNextRoundStarterIndex;
        string starterName = starter >= 0 && starter < players.Count
            ? players[starter].Name
            : "BRAK GRACZA";

        currentPlayerNameText.text = "PRZERWA";
        ShowCardSprite(
            null,
            "NASTĘPNĄ RUNDĘ\nROZPOCZYNA\n" + starterName.ToUpper(),
            new Color(0.04f, 0.22f, 0.12f, 1f),
            new Color(1f, 0.88f, 0.48f, 1f)
        );

        instructionText.text =
            "ROZPOCZYNAMY NASTĘPNĄ RUNDĘ?\n" +
            "DOTKNIJ KARTY, ABY ZACZĄĆ";
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
        roundNumber = 0;
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

        List<CardSpriteEntry> allCards = new List<CardSpriteEntry>();

        foreach (HotSeatPlayer player in players)
        {
            if (player.Eliminated)
                continue;

            allCards.AddRange(player.Cards);
        }

        return EvaluateCardsForHand(handId, allCards);
    }

    private static bool EvaluateCardsForHand(
        string handId,
        IEnumerable<CardSpriteEntry> cards)
    {
        if (string.IsNullOrEmpty(handId) || cards == null)
            return false;

        Dictionary<CardRank, int> rankCounts =
            new Dictionary<CardRank, int>();
        Dictionary<CardSuit, HashSet<CardRank>> suitRanks =
            new Dictionary<CardSuit, HashSet<CardRank>>();

        foreach (CardSpriteEntry card in cards)
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

    private bool IsCardRelevantToHand(
        string handId,
        CardSpriteEntry card)
    {
        if (string.IsNullOrEmpty(handId) || card == null)
            return false;

        if (handId.StartsWith("HIGH_") ||
            handId.StartsWith("PAIR_") ||
            handId.StartsWith("TRIPS_") ||
            handId.StartsWith("QUADS_"))
        {
            int separator = handId.IndexOf('_');
            CardRank? rank = GetRank(handId.Substring(separator + 1));
            return rank.HasValue && card.rank == rank.Value;
        }

        if (handId.StartsWith("TWOPAIR_") || handId.StartsWith("FULL_"))
        {
            string[] parts = handId.Split('_');
            if (parts.Length != 3)
                return false;

            CardRank? first = GetRank(parts[1]);
            CardRank? second = GetRank(parts[2]);
            return (first.HasValue && card.rank == first.Value) ||
                   (second.HasValue && card.rank == second.Value);
        }

        if (handId == "STRAIGHT_SMALL")
        {
            return card.rank == CardRank.Nine ||
                   card.rank == CardRank.Ten ||
                   card.rank == CardRank.Jack ||
                   card.rank == CardRank.Queen ||
                   card.rank == CardRank.King;
        }

        if (handId == "STRAIGHT_BIG")
        {
            return card.rank == CardRank.Ten ||
                   card.rank == CardRank.Jack ||
                   card.rank == CardRank.Queen ||
                   card.rank == CardRank.King ||
                   card.rank == CardRank.Ace;
        }

        if (handId.StartsWith("FLUSH_"))
        {
            CardSuit? suit = GetSuit(handId.Substring(6));
            return suit.HasValue && card.suit == suit.Value;
        }

        bool smallPoker = handId.StartsWith("POKER_SMALL_");
        bool bigPoker = handId.StartsWith("POKER_BIG_");
        if (smallPoker || bigPoker)
        {
            int suitOffset = smallPoker ? 12 : 10;
            CardSuit? suit = GetSuit(handId.Substring(suitOffset));
            if (!suit.HasValue || card.suit != suit.Value)
                return false;

            return smallPoker
                ? card.rank != CardRank.Ace
                : card.rank != CardRank.Nine;
        }

        return false;
    }

    private string FindHandId(string displayName)
    {
        string normalizedDisplay = NormalizeHandText(displayName);

        foreach (string handId in HandRankCatalog.GetAllIds())
        {
            if (NormalizeHandText(HandRankCatalog.GetDisplayName(handId)) ==
                normalizedDisplay)
            {
                return handId;
            }
        }

        return string.Empty;
    }

    private static string NormalizeHandText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        foreach (char character in value.Trim().ToUpperInvariant())
        {
            if (!char.IsWhiteSpace(character) &&
                character != '\u200B' && character != '\uFEFF')
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }

    private static int GetRankCount(
        Dictionary<CardRank, int> counts,
        CardRank? rank)
    {
        if (!rank.HasValue)
            return 0;

        return counts.TryGetValue(rank.Value, out int count)
            ? count
            : 0;
    }

    private static bool HasRanks(
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

    private static bool HasFlush(
        Dictionary<CardSuit, HashSet<CardRank>> suitRanks,
        CardSuit? suit)
    {
        return suit.HasValue &&
            suitRanks.TryGetValue(suit.Value, out HashSet<CardRank> ranks) &&
            ranks.Count >= 5;
    }

    private static bool HasStraightFlush(
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

    private static CardRank? GetRank(string value)
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

    private static CardSuit? GetSuit(string value)
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

        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character) ||
                character == ' ' || character == '-')
            {
                result += character;
            }

            if (result.Length >= maxNameLength)
                break;
        }

        return result;
    }

    private static string GetDefaultPlayerName(int playerNumber)
    {
        return "Gracz " + playerNumber;
    }

    private static bool IsDefaultPlayerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string compact = value.Replace(" ", "").ToUpperInvariant();
        return compact.StartsWith("GRACZ") &&
            int.TryParse(compact.Substring(5), out _);
    }

    private void RefreshButtons()
    {
        addPlayerButton.interactable =
            playerInputs.Count < maxPlayers;

        startButton.interactable =
            playerInputs.Count >= minPlayers;

        TMP_Text startLabel =
            startButton.GetComponentInChildren<TMP_Text>(true);
        if (startLabel != null)
            startLabel.text = "START";
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

        // Align the add action with the 900-unit player rows. Matching outer
        // edges makes the heading read as part of the same centered column.
        if (addPlayerButton != null &&
            addPlayerButton.transform is RectTransform addRect)
        {
            addRect.sizeDelta = new Vector2(900f, 126f);
            addRect.anchoredPosition = new Vector2(0f, -245f);
        }

        if (startButton != null && startButton.transform is RectTransform startRect)
        {
            startRect.anchoredPosition = new Vector2(0f, 260f);
            startRect.sizeDelta = new Vector2(660f, 136f);
        }

        if (playerListRoot is RectTransform listRect)
            listRect.anchoredPosition = new Vector2(0f, listRect.anchoredPosition.y);

        VerticalLayoutGroup playerLayout =
            playerListRoot != null ? playerListRoot.GetComponent<VerticalLayoutGroup>() : null;
        if (playerLayout != null)
            playerLayout.spacing = 22f;
    }

    private void CreateSetupHeader()
    {
        if (setupPanel == null || setupPanel.transform.Find("SetupHeading") != null)
            return;

        GameObject headingObject = new GameObject(
            "SetupHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
        headingObject.transform.SetParent(setupPanel.transform, false);

        RectTransform rect = headingObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -140f);
        rect.sizeDelta = new Vector2(900f, 72f);

        TextMeshProUGUI heading = headingObject.GetComponent<TextMeshProUGUI>();
        heading.text = "USTAW GRACZY";
        heading.alignment = TextAlignmentOptions.Center;
        heading.enableAutoSizing = true;
        heading.fontSizeMin = 32f;
        heading.fontSizeMax = 46f;
        heading.fontStyle = FontStyles.Bold;
        heading.characterSpacing = 2f;
        heading.color = new Color(1f, 0.82f, 0.30f, 1f);
        heading.raycastTarget = false;
    }

    private void CreateSetupBackButton()
    {
        if (setupPanel == null || startButton == null ||
            setupPanel.transform.Find("SetupBackButton") != null)
            return;

        BackToMenu navigation = FindFirstObjectByType<BackToMenu>();
        if (navigation != null)
            navigation.HideCornerButton();

        GameObject buttonObject = Instantiate(
            startButton.gameObject, startButton.transform.parent);
        buttonObject.name = "SetupBackButton";

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        if (navigation != null)
            button.onClick.AddListener(navigation.GoMainMenu);
        else
            button.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

        if (button.transform is RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 96f);
            rect.sizeDelta = new Vector2(420f, 92f);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "WSTECZ";
            label.enableAutoSizing = true;
            label.fontSizeMin = 22f;
            label.fontSizeMax = 30f;
            label.color = new Color(0.92f, 0.78f, 0.53f, 0.95f);
        }

        TMP_Text startLabel = startButton.GetComponentInChildren<TMP_Text>(true);
        if (startLabel != null)
        {
            startLabel.enableAutoSizing = true;
            startLabel.fontSizeMin = 32f;
            startLabel.fontSizeMax = 44f;
            startLabel.fontStyle = FontStyles.Bold;
        }

        PokerButtonTheme.ApplyTo(button);

        Image secondaryBackground = button.targetGraphic as Image;
        if (secondaryBackground != null)
            secondaryBackground.color = new Color(0.56f, 0.48f, 0.44f, 0.72f);

        foreach (Shadow shadow in button.GetComponents<Shadow>())
        {
            if (shadow != null && shadow.GetType() == typeof(Shadow))
                shadow.effectDistance = new Vector2(0f, -2f);
        }
    }

    private void ApplyCardScreenStyle()
    {
        if (cardPanel == null)
            return;

        Image background = cardPanel.GetComponent<Image>();
        if (background != null)
            background.color = new Color(0.005f, 0.05f, 0.03f, 0.93f);

        if (currentPlayerNameText != null)
        {
            RectTransform nameRect = currentPlayerNameText.rectTransform;
            // Keep the current player's name above the card. It used to overlap
            // the card rectangle and was rendered underneath the card artwork.
            nameRect.anchoredPosition = new Vector2(0f, -190f);
            nameRect.sizeDelta = new Vector2(900f, 72f);
            currentPlayerNameText.transform.SetAsLastSibling();

            currentPlayerNameText.color = new Color(1f, 0.84f, 0.38f, 1f);
            currentPlayerNameText.enableAutoSizing = true;
            currentPlayerNameText.fontSizeMin = 28f;
            currentPlayerNameText.fontSizeMax = 44f;
            currentPlayerNameText.fontStyle = FontStyles.Bold;
        }

        if (instructionText != null)
        {
            RectTransform instructionRect = instructionText.rectTransform;
            instructionRect.anchorMin = new Vector2(0.5f, 0f);
            instructionRect.anchorMax = new Vector2(0.5f, 0f);
            instructionRect.pivot = new Vector2(0.5f, 0f);
            instructionRect.anchoredPosition = new Vector2(0f, 405f);
            instructionRect.sizeDelta = new Vector2(900f, 158f);

            instructionText.color = new Color(1f, 0.95f, 0.82f, 1f);
            instructionText.enableAutoSizing = true;
            instructionText.fontSizeMin = 25f;
            instructionText.fontSizeMax = 38f;
            instructionText.fontStyle = FontStyles.Bold;
            instructionText.lineSpacing = 5f;
        }
    }

    private void StyleButton(Button button)
    {
        if (button == null)
            return;

        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(720f, 126f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
        }

        PokerButtonTheme.ApplyTo(button);
    }

    private void StylePlayerInput(TMP_InputField input)
    {
        if (input == null)
            return;

        Image background = input.targetGraphic as Image;
        if (background == null)
            background = input.GetComponent<Image>();

        if (background != null)
        {
            if (pokerButtonSprite != null)
            {
                background.sprite = pokerButtonSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }

            background.color = new Color(0.34f, 0.055f, 0.035f, 0.98f);

            Outline outline = background.GetComponent<Outline>();
            if (outline == null)
                outline = background.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0.92f, 0.67f, 0.20f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        if (input.textComponent != null)
        {
            input.textComponent.color = new Color(1f, 0.94f, 0.76f, 1f);
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            input.textComponent.enableAutoSizing = true;
            input.textComponent.fontSizeMin = 27f;
            input.textComponent.fontSizeMax = 39f;
        }

        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = string.Empty;
            placeholder.color = new Color(0.78f, 0.70f, 0.58f, 0.65f);
        }

        input.customCaretColor = true;
        input.caretColor = new Color(1f, 0.82f, 0.30f, 1f);
        input.caretBlinkRate = 0.55f;
        input.caretWidth = 4;
        input.onFocusSelectAll = true;
        input.keepTextSelectionVisible = true;
        input.selectionColor = new Color(0.72f, 0.49f, 0.08f, 0.55f);
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
