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

    [Header("Action button")]
    [SerializeField] private Color raiseButtonColor =
        new Color32(255, 209, 51, 255);

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

    private TMP_Text handRankTitle;

    private Button actionButton;
    private TMP_Text actionButtonText;
    private Image actionButtonVisual;

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

    private Color originalActionButtonColor = Color.white;

    private Button selectedOptionButton;
    private string selectedRankText;
    private string currentBidText;

    private bool canCheckCurrentTurn;
    private bool inputLocked;
    private bool initialized;

    public event Action<string> RaiseChosen;
    public event Action CheckChosen;

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
        RefreshStaticButtonLabels();
        BindButtons();
        CacheOriginalButtonColors();
        ApplyPokerTheme();

        initialized = true;
    }

    private void ApplyPokerTheme()
    {
        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        background.color = new Color(0.025f, 0.12f, 0.07f, 0.96f);
        background.raycastTarget = true;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = button == actionButton
                    ? new Color(0.72f, 0.49f, 0.08f)
                    : new Color(0.10f, 0.34f, 0.19f);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.fontStyle = FontStyles.Bold;
            }
        }

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

                if (actionButtonVisual != null)
                {
                    originalActionButtonColor =
                        actionButtonVisual.color;
                }
            }
        }

        Transform scrollView =
            FindDirectChild(transform, "RankScrollView");

        Transform viewport = scrollView != null
            ? FindDirectChild(scrollView, "Viewport")
            : null;

        Transform content = viewport != null
            ? FindDirectChild(viewport, "Content")
            : null;

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
        SetTitle("Podbij:");
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
    }

    private void ShowRankOptions(
        string title,
        string[] options)
    {
        ClearSelectedRank();
        SetTitle(title);
        SetOnlyOneListActive(rankOptionList);
        FillOptionList(rankOptionList, options);
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
        {
            actionButtonVisual.color =
                hasSelection
                    ? raiseButtonColor
                    : originalActionButtonColor;
        }

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
    }

    private void SetTitle(string value)
    {
        if (handRankTitle != null)
            handRankTitle.text = value;
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
