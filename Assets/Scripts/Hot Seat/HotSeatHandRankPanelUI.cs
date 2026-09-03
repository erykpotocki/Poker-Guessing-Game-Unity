using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HotSeatHandRankPanelUI : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private Color selectedOptionColor =
        new Color(0.35f, 0.75f, 0.35f, 1f);

    [SerializeField] private Color invalidOptionColor =
        new Color(0.85f, 0.25f, 0.25f, 1f);

    [SerializeField] private float invalidFlashDuration = 1f;

    private static readonly string[] RankOrder =
    {
        "9", "10", "J", "Q", "K", "A"
    };

    private static readonly string[] StraightOptions =
    {
        "9 10 J Q K",
        "10 J Q K A"
    };

    private static readonly string[] FlushOptions =
    {
        "Kolor ♦",
        "Kolor ♥",
        "Kolor ♣",
        "Kolor ♠"
    };

    private static readonly string[] PokerOptions =
    {
        "Mały poker ♦",
        "Mały poker ♥",
        "Mały poker ♣",
        "Mały poker ♠",
        "Duży poker ♦",
        "Duży poker ♥",
        "Duży poker ♣",
        "Duży poker ♠"
    };

    private static readonly List<string> CatalogIds =
        HandRankCatalog.GetAllIds();

    private GameObject categoryList;
    private GameObject rankOptionList;
    private GameObject fullGroupList;
    private GameObject fullDetailList;

    private RectTransform rankScrollViewRect;
    private RectTransform rankViewportRect;
    private RectTransform rankContentRect;
    private ScrollRect rankScrollRect;

    private TMP_Text handRankTitle;

    private Button actionButton;
    private TMP_Text actionButtonText;
    private Image actionButtonVisual;
    private Button cancelSelectionButton;

    private Button categoryButtonHighCard;
    private Button categoryButtonPair;
    private Button categoryButtonTwoPairs;
    private Button categoryButtonStraight;
    private Button categoryButtonTrips;
    private Button categoryButtonFull;
    private Button categoryButtonFlush;
    private Button categoryButtonQuads;
    private Button categoryButtonPoker;

    private Button rankBackButton;
    private Button fullGroupBackButton;
    private Button fullDetailBackButton;

    private Button fullGroupButton999;
    private Button fullGroupButton101010;
    private Button fullGroupButtonJJJ;
    private Button fullGroupButtonQQQ;
    private Button fullGroupButtonKKK;
    private Button fullGroupButtonAAA;

    private readonly Dictionary<Button, ColorBlock> originalButtonColors =
        new Dictionary<Button, ColorBlock>();

    private readonly Dictionary<Button, Coroutine> invalidFlashCoroutines =
        new Dictionary<Button, Coroutine>();

    private CanvasGroup panelCanvasGroup;

    private Button selectedOptionButton;
    private string selectedRankText;
    private string currentBidText;

    private bool canCheckCurrentTurn;
    private bool inputLocked;
    private bool initialized;

    public event Action<string> RaiseChosen;
    public event Action CheckChosen;
    public event Action CancelChosen;

    private void Awake()
    {
        Initialize();
    }

    public void Open(bool allowCheck, string currentBid)
    {
        Initialize();

        canCheckCurrentTurn = allowCheck;
        currentBidText = currentBid;
        inputLocked = false;

        gameObject.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        SetAllButtonsInteractable(true);
        ShowCategories();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (initialized)
            return;

        ResolveReferences();
        CreateCancelSelectionButton();
        ConfigureResponsiveLayout();
        RefreshStaticButtonLabels();
        BindButtons();
        ApplyPokerTheme();
        CacheOriginalButtonColors();

        initialized = true;
    }

    private void ApplyPokerTheme()
    {
        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        background.color = new Color(0.015f, 0.09f, 0.052f, 1f);
        background.raycastTarget = true;

        foreach (Button button in GetComponentsInChildren<Button>(true))
            PokerButtonTheme.ApplyTo(button);

        if (handRankTitle != null)
        {
            handRankTitle.text = "WYBIERZ UKŁAD";
            handRankTitle.color = new Color(1f, 0.82f, 0.3f);
            handRankTitle.fontStyle = FontStyles.Bold;
        }
    }

    private void ResolveReferences()
    {
        panelCanvasGroup = GetComponent<CanvasGroup>();

        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        Transform titleTransform =
            FindDirectChild(transform, "HandRankTitle");

        if (titleTransform != null)
            handRankTitle = titleTransform.GetComponent<TMP_Text>();

        Transform checkButtonTransform =
            FindDirectChild(transform, "CheckButton");

        if (checkButtonTransform != null)
        {
            actionButton =
                checkButtonTransform.GetComponent<Button>();

            actionButtonText =
                checkButtonTransform.GetComponentInChildren<TMP_Text>(true);

            Transform visualTransform =
                FindDirectChild(
                    checkButtonTransform,
                    "CheckButtonVisual"
                );

            if (visualTransform != null)
            {
                actionButtonVisual =
                    visualTransform.GetComponent<Image>();

                // The prefab keeps its visible background on a child object.
                // Make that image the actual target so the shared theme replaces
                // the old white/legacy action-button artwork as well.
                if (actionButton != null && actionButtonVisual != null)
                    actionButton.targetGraphic = actionButtonVisual;
            }
        }

        Transform scrollView =
            FindDirectChild(transform, "RankScrollView");

        rankScrollViewRect = scrollView as RectTransform;
        rankScrollRect = scrollView != null
            ? scrollView.GetComponent<ScrollRect>()
            : null;

        Transform viewport = scrollView != null
            ? FindDirectChild(scrollView, "Viewport")
            : null;

        rankViewportRect = viewport as RectTransform;

        Transform content = viewport != null
            ? FindDirectChild(viewport, "Content")
            : null;

        rankContentRect = content as RectTransform;

        if (content == null)
        {
            Debug.LogError(
                "HotSeatHandRankPanelUI: nie znaleziono " +
                "RankScrollView/Viewport/Content."
            );

            return;
        }

        categoryList =
            GetChildObject(content, "CategoryList");

        rankOptionList =
            GetChildObject(content, "RankOptionList");

        fullGroupList =
            GetChildObject(content, "FullGroupList");

        fullDetailList =
            GetChildObject(content, "FullDetailList");

        categoryButtonHighCard =
            GetButton(
                categoryList,
                "CategoryButton_WysokaKarta"
            );

        categoryButtonPair =
            GetButton(
                categoryList,
                "CategoryButton_Para"
            );

        categoryButtonTwoPairs =
            GetButton(
                categoryList,
                "CategoryButton_DwiePary"
            );

        categoryButtonStraight =
            GetButton(
                categoryList,
                "CategoryButton_Strit"
            );

        categoryButtonTrips =
            GetButton(
                categoryList,
                "CategoryButton_Trójka"
            );

        categoryButtonFull =
            GetButton(
                categoryList,
                "CategoryButton_Full"
            );

        categoryButtonFlush =
            GetButton(
                categoryList,
                "CategoryButton_Kolor"
            );

        categoryButtonQuads =
            GetButton(
                categoryList,
                "CategoryButton_Kareta"
            );

        categoryButtonPoker =
            GetButton(
                categoryList,
                "CategoryButton_Poker"
            );

        rankBackButton =
            GetButton(rankOptionList, "BackButton");

        fullGroupBackButton =
            GetButton(fullGroupList, "BackButton");

        fullDetailBackButton =
            GetButton(fullDetailList, "BackButton");

        fullGroupButton999 =
            GetButton(
                fullGroupList,
                "RankOptionButton_999??"
            );

        fullGroupButton101010 =
            GetButton(
                fullGroupList,
                "RankOptionButton_101010??"
            );

        fullGroupButtonJJJ =
            GetButton(
                fullGroupList,
                "RankOptionButton_JJJ??"
            );

        fullGroupButtonQQQ =
            GetButton(
                fullGroupList,
                "RankOptionButton_DDD??"
            );

        fullGroupButtonKKK =
            GetButton(
                fullGroupList,
                "RankOptionButton_KKK??"
            );

        fullGroupButtonAAA =
            GetButton(
                fullGroupList,
                "RankOptionButton_AAA??"
            );
    }

    private void BindButtons()
    {
        AddClick(
            categoryButtonHighCard,
            ShowHighCardOptions
        );

        AddClick(
            categoryButtonPair,
            ShowPairOptions
        );

        AddClick(
            categoryButtonTwoPairs,
            ShowTwoPairsOptions
        );

        AddClick(
            categoryButtonStraight,
            ShowStraightOptions
        );

        AddClick(
            categoryButtonTrips,
            ShowTripsOptions
        );

        AddClick(
            categoryButtonFull,
            ShowFullGroups
        );

        AddClick(
            categoryButtonFlush,
            ShowFlushOptions
        );

        AddClick(
            categoryButtonQuads,
            ShowQuadsOptions
        );

        AddClick(
            categoryButtonPoker,
            ShowPokerOptions
        );

        AddClick(rankBackButton, ShowCategories);
        AddClick(fullGroupBackButton, ShowCategories);
        AddClick(fullDetailBackButton, ShowFullGroups);

        AddClick(
            fullGroupButton999,
            () => ShowFullDetails("9")
        );

        AddClick(
            fullGroupButton101010,
            () => ShowFullDetails("10")
        );

        AddClick(
            fullGroupButtonJJJ,
            () => ShowFullDetails("J")
        );

        AddClick(
            fullGroupButtonQQQ,
            () => ShowFullDetails("Q")
        );

        AddClick(
            fullGroupButtonKKK,
            () => ShowFullDetails("K")
        );

        AddClick(
            fullGroupButtonAAA,
            () => ShowFullDetails("A")
        );

        AddClick(actionButton, HandleActionButtonClicked);
        AddClick(cancelSelectionButton, HandleCancelSelectionClicked);
    }

    private void CreateCancelSelectionButton()
    {
        Transform existing = FindDirectChild(transform, "CancelSelectionButton");
        if (existing != null)
        {
            cancelSelectionButton = existing.GetComponent<Button>();
            return;
        }

        GameObject buttonObject = new GameObject(
            "CancelSelectionButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 128f);
        rect.sizeDelta = new Vector2(360f, 66f);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "WRÓĆ DO KARTY";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;

        cancelSelectionButton = buttonObject.GetComponent<Button>();
        cancelSelectionButton.targetGraphic = buttonObject.GetComponent<Image>();
    }

    private void ConfigureResponsiveLayout()
    {
        if (transform is RectTransform panelRect)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 42f);
            panelRect.sizeDelta = new Vector2(940f, 800f);
        }

        if (handRankTitle != null)
        {
            RectTransform titleRect = handRankTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(-48f, 64f);

            handRankTitle.alignment = TextAlignmentOptions.Center;
            handRankTitle.textWrappingMode = TextWrappingModes.NoWrap;
            handRankTitle.enableAutoSizing = true;
            handRankTitle.fontSizeMin = 20f;
            handRankTitle.fontSizeMax = 31f;
        }

        if (rankScrollViewRect != null)
        {
            rankScrollViewRect.anchorMin = Vector2.zero;
            rankScrollViewRect.anchorMax = Vector2.one;
            rankScrollViewRect.offsetMin = new Vector2(70f, 226f);
            rankScrollViewRect.offsetMax = new Vector2(-70f, -112f);
        }

        if (rankScrollRect != null)
        {
            rankScrollRect.content = rankContentRect;
            rankScrollRect.viewport = rankViewportRect;
            rankScrollRect.horizontal = false;
            rankScrollRect.vertical = true;
            rankScrollRect.movementType = ScrollRect.MovementType.Clamped;
            rankScrollRect.inertia = true;
            rankScrollRect.scrollSensitivity = 55f;
        }

        // The scene used to override the prefab viewport with a large negative
        // position. After making the panel compact that pushed every option
        // outside the mask, leaving only the cancel button visible.
        if (rankViewportRect != null)
        {
            rankViewportRect.anchorMin = Vector2.zero;
            rankViewportRect.anchorMax = Vector2.one;
            rankViewportRect.offsetMin = Vector2.zero;
            rankViewportRect.offsetMax = Vector2.zero;
            rankViewportRect.pivot = new Vector2(0.5f, 0.5f);
        }

        if (rankContentRect != null)
        {
            rankContentRect.anchorMin = new Vector2(0f, 1f);
            rankContentRect.anchorMax = new Vector2(1f, 1f);
            rankContentRect.pivot = new Vector2(0.5f, 1f);
            rankContentRect.anchoredPosition = Vector2.zero;
            rankContentRect.sizeDelta = new Vector2(0f, 720f);
        }

        if (actionButton != null && actionButton.transform is RectTransform actionRect)
        {
            actionRect.anchorMin = new Vector2(0f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = new Vector2(0.5f, 0f);
            actionRect.anchoredPosition = new Vector2(0f, 24f);
            actionRect.sizeDelta = new Vector2(-40f, 76f);
        }

        if (cancelSelectionButton != null && cancelSelectionButton.transform is RectTransform cancelRect)
        {
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0.5f);
            cancelRect.anchoredPosition = new Vector2(0f, 164f);
            cancelRect.sizeDelta = new Vector2(320f, 62f);
        }

        ConfigureOptionButtons(categoryList);
        ConfigureOptionButtons(rankOptionList);
        ConfigureOptionButtons(fullGroupList);
        ConfigureOptionButtons(fullDetailList);
    }

    private static void ConfigureOptionButtons(GameObject listObject)
    {
        if (listObject == null)
            return;

        if (listObject.transform is RectTransform listRect)
        {
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = new Vector2(0f, 720f);
        }

        VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.spacing = 14f;

        foreach (Button button in listObject.GetComponentsInChildren<Button>(true))
        {
            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element == null)
                element = button.gameObject.AddComponent<LayoutElement>();

            element.enabled = true;

            element.minHeight = 82f;
            element.preferredHeight = 82f;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 24f;
                label.fontSizeMax = 32f;
            }
        }
    }

    private void HandleCancelSelectionClicked()
    {
        if (inputLocked)
            return;

        Close();
        CancelChosen?.Invoke();
    }

    private void RefreshStaticButtonLabels()
    {
        SetButtonLabel(fullGroupButton999, "999??");
        SetButtonLabel(fullGroupButton101010, "101010??");
        SetButtonLabel(fullGroupButtonJJJ, "JJJ??");
        SetButtonLabel(fullGroupButtonQQQ, "QQQ??");
        SetButtonLabel(fullGroupButtonKKK, "KKK??");
        SetButtonLabel(fullGroupButtonAAA, "AAA??");
    }

    private void CacheOriginalButtonColors()
    {
        Button[] buttons =
            GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button != null &&
                !originalButtonColors.ContainsKey(button))
            {
                originalButtonColors.Add(
                    button,
                    button.colors
                );
            }
        }
    }

    private void SetButtonLabel(
        Button button,
        string textValue)
    {
        if (button == null)
            return;

        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = textValue;
    }

    private void AddClick(
        Button button,
        UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void ShowCategories()
    {
        ClearSelectedRank();
        SetTitle(string.IsNullOrWhiteSpace(currentBidText)
            ? "WYBIERZ UKŁAD"
            : "WYBIERZ WYŻSZY UKŁAD");
        SetOnlyOneListActive(categoryList);
    }

    private void ShowHighCardOptions()
    {
        ShowRankOptions(
            "Wysoka karta",
            BuildRepeatedOptions(1)
        );
    }

    private void ShowPairOptions()
    {
        ShowRankOptions(
            "Pary",
            BuildRepeatedOptions(2)
        );
    }

    private void ShowTwoPairsOptions()
    {
        ShowRankOptions(
            "Dwie pary",
            BuildTwoPairOptions()
        );
    }

    private void ShowStraightOptions()
    {
        ShowRankOptions(
            "Strity",
            StraightOptions
        );
    }

    private void ShowTripsOptions()
    {
        ShowRankOptions(
            "Trójki",
            BuildRepeatedOptions(3)
        );
    }

    private void ShowFlushOptions()
    {
        ShowRankOptions(
            "Kolory",
            FlushOptions
        );
    }

    private void ShowQuadsOptions()
    {
        ShowRankOptions(
            "Karety",
            BuildRepeatedOptions(4)
        );
    }

    private void ShowPokerOptions()
    {
        ShowRankOptions(
            "Pokery",
            PokerOptions
        );
    }

    private void ShowFullGroups()
    {
        ClearSelectedRank();
        SetTitle("Fulle");
        SetOnlyOneListActive(fullGroupList);
    }

    private void ShowFullDetails(string tripleRank)
    {
        ClearSelectedRank();

        SetTitle(
            "Fulle " +
            RepeatRank(tripleRank, 3) +
            "??"
        );

        SetOnlyOneListActive(fullDetailList);

        FillOptionList(
            fullDetailList,
            BuildFullOptions(tripleRank)
        );
        RefreshScrollLayout(fullDetailList);
    }

    private void ShowRankOptions(
        string title,
        string[] options)
    {
        ClearSelectedRank();
        SetTitle(title);
        SetOnlyOneListActive(rankOptionList);
        FillOptionList(rankOptionList, options);
        RefreshScrollLayout(rankOptionList);
    }

    private void FillOptionList(
        GameObject listObject,
        string[] optionTexts)
    {
        if (listObject == null)
            return;

        List<Button> buttons =
            GetOptionButtons(listObject.transform);

        for (int i = 0; i < buttons.Count; i++)
        {
            bool shouldBeVisible =
                i < optionTexts.Length;

            Button currentButton = buttons[i];

            currentButton.gameObject.SetActive(
                shouldBeVisible
            );

            if (!shouldBeVisible)
                continue;

            string optionText = optionTexts[i];

            TMP_Text text =
                currentButton.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
                text.text = optionText;

            RestoreButtonVisual(currentButton);

            currentButton.interactable =
                !inputLocked;

            currentButton.onClick.RemoveAllListeners();

            currentButton.onClick.AddListener(
                () => SelectRankOption(
                    currentButton,
                    optionText
                )
            );
        }

        Button backButton =
            GetButton(listObject, "BackButton");

        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }

    private void SelectRankOption(
        Button clickedButton,
        string optionText)
    {
        if (inputLocked)
            return;

        if (!CanSelectRank(optionText))
        {
            FlashInvalidButton(clickedButton);

            Debug.Log(
                "Można przebić tylko wyższym układem."
            );

            return;
        }

        if (selectedOptionButton != null)
            RestoreButtonVisual(selectedOptionButton);

        selectedOptionButton = clickedButton;
        selectedRankText = optionText;

        ApplySelectedVisual(clickedButton);
        RefreshActionButtonState();
    }

    private bool CanSelectRank(string candidateText)
    {
        if (string.IsNullOrWhiteSpace(currentBidText))
            return true;

        string candidateId =
            FindHandId(candidateText);

        string currentId =
            FindHandId(currentBidText);

        if (string.IsNullOrEmpty(candidateId) ||
            string.IsNullOrEmpty(currentId))
        {
            return NormalizeText(candidateText) !=
                   NormalizeText(currentBidText);
        }

        return HandRankCatalog.CanBeat(
            candidateId,
            currentId
        );
    }

    private string FindHandId(string displayText)
    {
        string normalizedDisplay =
            NormalizeText(displayText);

        foreach (string id in CatalogIds)
        {
            string catalogDisplay =
                HandRankCatalog.GetDisplayName(id);

            if (NormalizeText(catalogDisplay) ==
                normalizedDisplay)
            {
                return id;
            }
        }

        return string.Empty;
    }

    private void HandleActionButtonClicked()
    {
        if (inputLocked)
            return;

        if (!string.IsNullOrEmpty(selectedRankText))
        {
            string chosenRank = selectedRankText;

            LockPanel();
            Close();

            RaiseChosen?.Invoke(chosenRank);
            return;
        }

        if (!canCheckCurrentTurn)
            return;

        LockPanel();
        Close();

        CheckChosen?.Invoke();
    }

    private void LockPanel()
    {
        inputLocked = true;

        SetAllButtonsInteractable(false);
        RefreshActionButtonState();
    }

    private void SetAllButtonsInteractable(bool value)
    {
        Button[] buttons =
            GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null || button == actionButton)
                continue;

            button.interactable = value;
        }
    }

    private void ClearSelectedRank()
    {
        if (selectedOptionButton != null)
        {
            RestoreButtonVisual(selectedOptionButton);
            selectedOptionButton = null;
        }

        selectedRankText = null;
        RefreshActionButtonState();
    }

    private void RefreshActionButtonState()
    {
        if (actionButton == null)
            return;

        bool hasSelection =
            !string.IsNullOrEmpty(selectedRankText);

        bool shouldBeVisible =
            hasSelection || canCheckCurrentTurn;

        actionButton.gameObject.SetActive(
            shouldBeVisible
        );

        if (actionButtonText != null)
        {
            actionButtonText.text =
                hasSelection
                    ? "Przebij"
                    : "Sprawdzam";
        }

        if (actionButtonVisual != null)
            actionButtonVisual.color = Color.white;

        actionButton.interactable =
            shouldBeVisible && !inputLocked;
    }

    private void ApplySelectedVisual(Button button)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;

        colors.normalColor = selectedOptionColor;
        colors.highlightedColor = selectedOptionColor;
        colors.selectedColor = selectedOptionColor;

        button.colors = colors;
    }

    private void RestoreButtonVisual(Button button)
    {
        if (button == null)
            return;

        if (originalButtonColors.TryGetValue(
            button,
            out ColorBlock originalColors))
        {
            button.colors = originalColors;
        }
    }

    private void FlashInvalidButton(Button button)
    {
        if (button == null)
            return;

        if (invalidFlashCoroutines.TryGetValue(
            button,
            out Coroutine runningCoroutine))
        {
            if (runningCoroutine != null)
                StopCoroutine(runningCoroutine);
        }

        invalidFlashCoroutines[button] =
            StartCoroutine(
                FlashInvalidButtonRoutine(button)
            );
    }

    private IEnumerator FlashInvalidButtonRoutine(
        Button button)
    {
        ColorBlock colors = button.colors;

        colors.normalColor = invalidOptionColor;
        colors.highlightedColor = invalidOptionColor;
        colors.selectedColor = invalidOptionColor;

        button.colors = colors;

        yield return new WaitForSeconds(
            invalidFlashDuration
        );

        if (button != null)
        {
            if (button == selectedOptionButton)
                ApplySelectedVisual(button);
            else
                RestoreButtonVisual(button);
        }

        invalidFlashCoroutines.Remove(button);
    }

    private string[] BuildRepeatedOptions(int amount)
    {
        string[] options =
            new string[RankOrder.Length];

        for (int i = 0; i < RankOrder.Length; i++)
        {
            options[i] =
                RepeatRank(RankOrder[i], amount);
        }

        return options;
    }

    private string[] BuildTwoPairOptions()
    {
        List<string> options =
            new List<string>();

        for (int first = 0;
             first < RankOrder.Length - 1;
             first++)
        {
            for (int second = first + 1;
                 second < RankOrder.Length;
                 second++)
            {
                options.Add(
                    RepeatRank(RankOrder[first], 2) +
                    " " +
                    RepeatRank(RankOrder[second], 2)
                );
            }
        }

        return options.ToArray();
    }

    private string[] BuildFullOptions(string tripleRank)
    {
        List<string> options =
            new List<string>();

        foreach (string pairRank in RankOrder)
        {
            if (pairRank == tripleRank)
                continue;

            options.Add(
                RepeatRank(tripleRank, 3) +
                " " +
                RepeatRank(pairRank, 2)
            );
        }

        return options.ToArray();
    }

    private string RepeatRank(
        string rank,
        int amount)
    {
        string result = "";

        for (int i = 0; i < amount; i++)
        {
            if (i > 0)
                result += " ";

            result += rank;
        }

        return result;
    }

    private List<Button> GetOptionButtons(
        Transform listRoot)
    {
        List<Button> result =
            new List<Button>();

        if (listRoot == null)
            return result;

        for (int i = 0;
             i < listRoot.childCount;
             i++)
        {
            Transform child =
                listRoot.GetChild(i);

            if (child.name == "BackButton")
                continue;

            Button button =
                child.GetComponent<Button>();

            if (button != null)
                result.Add(button);
        }

        return result;
    }

    private void SetOnlyOneListActive(
        GameObject target)
    {
        if (categoryList != null)
            categoryList.SetActive(
                target == categoryList
            );

        if (rankOptionList != null)
            rankOptionList.SetActive(
                target == rankOptionList
            );

        if (fullGroupList != null)
            fullGroupList.SetActive(
                target == fullGroupList
            );

        if (fullDetailList != null)
            fullDetailList.SetActive(
                target == fullDetailList
            );

        RefreshScrollLayout(target);
    }

    private void SetTitle(string value)
    {
        if (handRankTitle != null)
        {
            string declaration = string.IsNullOrWhiteSpace(currentBidText)
                ? "ROZPOCZYNASZ RUNDĘ"
                : "POPRZEDNIA DEKLARACJA: " + currentBidText.ToUpper();

            handRankTitle.text = declaration + "\n" + value.ToUpper();
        }
    }

    private void RefreshScrollLayout(GameObject activeList)
    {
        if (activeList == null || rankContentRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        int visibleButtons = 0;
        foreach (Button button in activeList.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.activeSelf)
                visibleButtons++;
        }

        const float buttonHeight = 82f;
        const float spacing = 14f;
        float viewportHeight = rankViewportRect != null
            ? rankViewportRect.rect.height
            : 0f;
        float listHeight = Mathf.Max(
            viewportHeight,
            visibleButtons * buttonHeight +
            Mathf.Max(0, visibleButtons - 1) * spacing
        );

        if (activeList.transform is RectTransform listRect)
            listRect.sizeDelta = new Vector2(0f, listHeight);

        rankContentRect.sizeDelta = new Vector2(0f, listHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rankContentRect);

        if (rankScrollRect != null)
        {
            rankScrollRect.StopMovement();
            rankScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim()
            .ToUpperInvariant();
    }

    private GameObject GetChildObject(
        Transform parent,
        string childName)
    {
        Transform child =
            FindDirectChild(parent, childName);

        return child != null
            ? child.gameObject
            : null;
    }

    private Button GetButton(
        GameObject parentObject,
        string childName)
    {
        if (parentObject == null)
            return null;

        Transform child =
            FindDirectChild(
                parentObject.transform,
                childName
            );

        return child != null
            ? child.GetComponent<Button>()
            : null;
    }

    private Transform FindDirectChild(
        Transform parent,
        string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform child =
                parent.GetChild(i);

            if (child.name == childName)
                return child;
        }

        return null;
    }
}
