using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandRankPanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private RoundLogUI roundLogUI;

    [Header("Selection")]
    [SerializeField] private Color selectedOptionColor = new Color(0.35f, 0.75f, 0.35f, 1f);
    [SerializeField] private Color invalidOptionColor = new Color(0.85f, 0.25f, 0.25f, 1f);
    [SerializeField] private float invalidFlashDuration = 1f;

    [Header("Visual state")]
    [SerializeField, Range(0.2f, 1f)] private float inactivePanelAlpha = 0.55f;
    [SerializeField] private Color raiseButtonColor = new Color32(255, 209, 51, 255);

    private static readonly string[] rankOrder = { "9", "10", "J", "Q", "K", "A" };

    private GameObject categoryList;
    private GameObject rankOptionList;
    private GameObject fullGroupList;
    private GameObject fullDetailList;

    private TMP_Text handRankTitle;

    private Button checkButton;
    private TMP_Text checkButtonText;
    private Image checkButtonVisual;

    private Button categoryButtonWysokaKarta;
    private Button categoryButtonPara;
    private Button categoryButtonDwiePary;
    private Button categoryButtonStrit;
    private Button categoryButtonTrojka;
    private Button categoryButtonFull;
    private Button categoryButtonKolor;
    private Button categoryButtonKareta;
    private Button categoryButtonPoker;

    private Button rankBackButton;
    private Button fullGroupBackButton;
    private Button fullDetailBackButton;

    private Button fullGroupButton999;
    private Button fullGroupButton101010;
    private Button fullGroupButtonJJJ;
    private Button fullGroupButtonDDD;
    private Button fullGroupButtonKKK;
    private Button fullGroupButtonAAA;

    private string selectedRankText = null;
    private Button selectedOptionButton = null;

    private ColorBlock cachedNormalColors;
    private bool hasCachedNormalColors = false;

    private bool canCheckCurrentTurn = false;
    private bool isPanelInputLocked = false;

    private Color cachedCheckButtonVisualColor = Color.white;
    private CanvasGroup panelCanvasGroup;

    private readonly Dictionary<Button, Coroutine> invalidFlashCoroutines = new Dictionary<Button, Coroutine>();

    public event Action<string> OnRaiseChosen;
    public event Action OnCheckChosen;

    private void Awake()
    {
        ResolveReferences();
        TryResolveTurnManager();
        TryResolveRoundLogUI();
        RefreshStaticButtonLabels();
        BindButtons();
        CacheNormalButtonColors();
        ConfigureModernPanelStyle();
        SetFirstMoveState();
    }

    private void ConfigureModernPanelStyle()
    {
        if (handRankTitle != null)
        {
            handRankTitle.text = string.Empty;
            handRankTitle.gameObject.SetActive(false);
        }

        Button topBackButton = GetButton(categoryList, "BackButton");
        if (topBackButton != null && topBackButton.transform is RectTransform backRect)
        {
            backRect.anchoredPosition += Vector2.left * 34f;
            PokerButtonTheme.ApplyTo(topBackButton);
        }

        if (checkButton != null && checkButton.transform is RectTransform checkRect)
        {
            checkRect.offsetMin += new Vector2(18f, 14f);
            checkRect.offsetMax += new Vector2(-30f, -12f);
            PokerButtonTheme.ApplyTo(checkButton);
            if (checkButtonVisual != null)
                checkButtonVisual.color = Color.white;
        }
    }

    private void ResolveReferences()
    {
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        Transform titleTransform = FindDirectChild(transform, "HandRankTitle");
        if (titleTransform != null)
            handRankTitle = titleTransform.GetComponent<TMP_Text>();

        Transform checkButtonTransform = FindDirectChild(transform, "CheckButton");
        if (checkButtonTransform != null)
        {
            checkButton = checkButtonTransform.GetComponent<Button>();
            checkButtonText = checkButtonTransform.GetComponentInChildren<TMP_Text>(true);

            Transform checkButtonVisualTransform = FindDirectChild(checkButtonTransform, "CheckButtonVisual");
            if (checkButtonVisualTransform != null)
            {
                checkButtonVisual = checkButtonVisualTransform.GetComponent<Image>();
                if (checkButtonVisual != null)
                    cachedCheckButtonVisualColor = checkButtonVisual.color;
            }
        }

        Transform rankScrollView = FindDirectChild(transform, "RankScrollView");
        Transform viewport = rankScrollView != null ? FindDirectChild(rankScrollView, "Viewport") : null;
        Transform content = viewport != null ? FindDirectChild(viewport, "Content") : null;

        if (content == null)
        {
            Debug.LogError("HandRankPanelUI: nie znaleziono RankScrollView/Viewport/Content");
            return;
        }

        categoryList = GetChildObject(content, "CategoryList");
        rankOptionList = GetChildObject(content, "RankOptionList");
        fullGroupList = GetChildObject(content, "FullGroupList");
        fullDetailList = GetChildObject(content, "FullDetailList");

        categoryButtonWysokaKarta = GetButton(categoryList, "CategoryButton_WysokaKarta");
        categoryButtonPara = GetButton(categoryList, "CategoryButton_Para");
        categoryButtonDwiePary = GetButton(categoryList, "CategoryButton_DwiePary");
        categoryButtonStrit = GetButton(categoryList, "CategoryButton_Strit");
        categoryButtonTrojka = GetButton(categoryList, "CategoryButton_Trójka");
        categoryButtonFull = GetButton(categoryList, "CategoryButton_Full");
        categoryButtonKolor = GetButton(categoryList, "CategoryButton_Kolor");
        categoryButtonKareta = GetButton(categoryList, "CategoryButton_Kareta");
        categoryButtonPoker = GetButton(categoryList, "CategoryButton_Poker");

        rankBackButton = GetButton(rankOptionList, "BackButton");
        fullGroupBackButton = GetButton(fullGroupList, "BackButton");
        fullDetailBackButton = GetButton(fullDetailList, "BackButton");

        fullGroupButton999 = GetButton(fullGroupList, "RankOptionButton_999??");
        fullGroupButton101010 = GetButton(fullGroupList, "RankOptionButton_101010??");
        fullGroupButtonJJJ = GetButton(fullGroupList, "RankOptionButton_JJJ??");
        fullGroupButtonDDD = GetButton(fullGroupList, "RankOptionButton_DDD??");
        fullGroupButtonKKK = GetButton(fullGroupList, "RankOptionButton_KKK??");
        fullGroupButtonAAA = GetButton(fullGroupList, "RankOptionButton_AAA??");
    }

    private void RefreshStaticButtonLabels()
    {
        SetButtonLabel(fullGroupButton999, "999??");
        SetButtonLabel(fullGroupButton101010, "101010??");
        SetButtonLabel(fullGroupButtonJJJ, "JJJ??");
        SetButtonLabel(fullGroupButtonDDD, "QQQ??");
        SetButtonLabel(fullGroupButtonKKK, "KKK??");
        SetButtonLabel(fullGroupButtonAAA, "AAA??");
    }

    private void SetButtonLabel(Button button, string textValue)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = textValue;
    }

    private void TryResolveTurnManager()
    {
        if (turnManager != null)
            return;

        turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
    }

    private void TryResolveRoundLogUI()
    {
        if (roundLogUI != null)
            return;

        roundLogUI = FindFirstObjectByType<RoundLogUI>(FindObjectsInactive.Include);
    }

    private void BindButtons()
    {
        AddClick(categoryButtonWysokaKarta, ShowHighCardOptions);
        AddClick(categoryButtonPara, ShowPairOptions);
        AddClick(categoryButtonDwiePary, ShowTwoPairsOptions);
        AddClick(categoryButtonStrit, ShowStraightOptions);
        AddClick(categoryButtonTrojka, ShowThreeOptions);
        AddClick(categoryButtonFull, ShowFullGroups);
        AddClick(categoryButtonKolor, ShowColorOptions);
        AddClick(categoryButtonKareta, ShowFourOptions);
        AddClick(categoryButtonPoker, ShowPokerOptions);

        AddClick(rankBackButton, ShowCategories);
        AddClick(fullGroupBackButton, ShowCategories);
        AddClick(fullDetailBackButton, ShowFullGroups);

        AddClick(fullGroupButton999, ShowFullDetails999);
        AddClick(fullGroupButton101010, ShowFullDetails101010);
        AddClick(fullGroupButtonJJJ, ShowFullDetailsJJJ);
        AddClick(fullGroupButtonDDD, ShowFullDetailsDDD);
        AddClick(fullGroupButtonKKK, ShowFullDetailsKKK);
        AddClick(fullGroupButtonAAA, ShowFullDetailsAAA);

        AddClick(checkButton, HandleCheckButtonClicked);
    }

    private void CacheNormalButtonColors()
    {
        Button sampleButton = GetFirstOptionButton(rankOptionList);
        if (sampleButton == null)
            sampleButton = GetFirstOptionButton(fullDetailList);

        if (sampleButton == null)
            return;

        cachedNormalColors = sampleButton.colors;
        hasCachedNormalColors = true;
    }

    public void SetFirstMoveState()
    {
        PrepareForNewTurn(false);
    }

    public void SetNormalTurnState()
    {
        PrepareForNewTurn(true);
    }

    public void PrepareForNewTurn(bool allowCheck)
    {
        TryResolveTurnManager();

        canCheckCurrentTurn = allowCheck;
        isPanelInputLocked = false;

        SetAllButtonsInteractable(true);
        ShowCategories();
    }

    public void SetPanelDimmed(bool value)
    {
        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.alpha = value ? inactivePanelAlpha : 1f;
    }

    private void LockPanelAfterAction()
    {
        isPanelInputLocked = true;
        SetAllButtonsInteractable(false);
        RefreshCheckButtonState();
    }

    private void SetAllButtonsInteractable(bool value)
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] == null)
                continue;

            if (allButtons[i] == checkButton)
                continue;

            allButtons[i].interactable = value;
        }
    }

    private Button GetFirstOptionButton(GameObject listObject)
    {
        if (listObject == null)
            return null;

        List<Button> buttons = GetOptionButtons(listObject.transform);
        return buttons.Count > 0 ? buttons[0] : null;
    }

    private void AddClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void ShowCategories()
    {
        ClearSelectedRank();
        SetTitle(string.Empty);
        SetOnlyOneListActive(categoryList);
    }

    public void ShowHighCardOptions()
    {
        ShowRankOptions("Wysoka karta", new string[]
        {
            "9",
            "10",
            "J",
            "Q",
            "K",
            "A"
        });
    }

    public void ShowPairOptions()
    {
        ShowRankOptions("Pary", new string[]
        {
            "9 9",
            "10 10",
            "J J",
            "Q Q",
            "K K",
            "A A"
        });
    }

    public void ShowTwoPairsOptions()
    {
        ShowRankOptions("Dwie pary", new string[]
        {
            "9 9 10 10",
            "9 9 J J",
            "9 9 Q Q",
            "9 9 K K",
            "9 9 A A",
            "10 10 J J",
            "10 10 Q Q",
            "10 10 K K",
            "10 10 A A",
            "J J Q Q",
            "J J K K",
            "J J A A",
            "Q Q K K",
            "Q Q A A",
            "K K A A"
        });
    }

    public void ShowStraightOptions()
    {
        ShowRankOptions("Strity", new string[]
        {
            "9 10 J Q K",
            "10 J Q K A"
        });
    }

    public void ShowThreeOptions()
    {
        ShowRankOptions("Trójki", new string[]
        {
            "9 9 9",
            "10 10 10",
            "J J J",
            "Q Q Q",
            "K K K",
            "A A A"
        });
    }

    public void ShowColorOptions()
    {
        ShowRankOptions("Kolory", new string[]
        {
            "Kolor ♦",
            "Kolor ♥",
            "Kolor ♣",
            "Kolor ♠"
        });
    }

    public void ShowFourOptions()
    {
        ShowRankOptions("Karety", new string[]
        {
            "9 9 9 9",
            "10 10 10 10",
            "J J J J",
            "Q Q Q Q",
            "K K K K",
            "A A A A"
        });
    }

    public void ShowPokerOptions()
    {
        ShowRankOptions("Pokery", new string[]
        {
            "Mały poker ♦",
            "Mały poker ♥",
            "Mały poker ♣",
            "Mały poker ♠",
            "Duży poker ♦",
            "Duży poker ♥",
            "Duży poker ♣",
            "Duży poker ♠"
        });
    }

    public void ShowFullGroups()
    {
        ClearSelectedRank();
        SetTitle("Fulle");
        SetOnlyOneListActive(fullGroupList);
    }

    public void ShowFullDetails999()
    {
        ShowFullDetails("Fulle 999??", new string[]
        {
            "9 9 9 10 10",
            "9 9 9 J J",
            "9 9 9 Q Q",
            "9 9 9 K K",
            "9 9 9 A A"
        });
    }

    public void ShowFullDetails101010()
    {
        ShowFullDetails("Fulle 101010??", new string[]
        {
            "10 10 10 9 9",
            "10 10 10 J J",
            "10 10 10 Q Q",
            "10 10 10 K K",
            "10 10 10 A A"
        });
    }

    public void ShowFullDetailsJJJ()
    {
        ShowFullDetails("Fulle JJJ??", new string[]
        {
            "J J J 9 9",
            "J J J 10 10",
            "J J J Q Q",
            "J J J K K",
            "J J J A A"
        });
    }

    public void ShowFullDetailsDDD()
    {
        ShowFullDetails("Fulle QQQ??", new string[]
        {
            "Q Q Q 9 9",
            "Q Q Q 10 10",
            "Q Q Q J J",
            "Q Q Q K K",
            "Q Q Q A A"
        });
    }

    public void ShowFullDetailsKKK()
    {
        ShowFullDetails("Fulle KKK??", new string[]
        {
            "K K K 9 9",
            "K K K 10 10",
            "K K K J J",
            "K K K Q Q",
            "K K K A A"
        });
    }

    public void ShowFullDetailsAAA()
    {
        ShowFullDetails("Fulle AAA??", new string[]
        {
            "A A A 9 9",
            "A A A 10 10",
            "A A A J J",
            "A A A Q Q",
            "A A A K K"
        });
    }

    private void ShowRankOptions(string title, string[] options)
    {
        ClearSelectedRank();
        SetTitle(title);
        SetOnlyOneListActive(rankOptionList);
        FillOptionList(rankOptionList, options);
    }

    private void ShowFullDetails(string title, string[] options)
    {
        ClearSelectedRank();
        SetTitle(title);
        SetOnlyOneListActive(fullDetailList);
        FillOptionList(fullDetailList, options);
    }

    private void FillOptionList(GameObject listObject, string[] optionTexts)
    {
        if (listObject == null)
            return;

        List<Button> buttons = GetOptionButtons(listObject.transform);

        for (int i = 0; i < buttons.Count; i++)
        {
            bool shouldBeVisible = i < optionTexts.Length;
            buttons[i].gameObject.SetActive(shouldBeVisible);

            if (shouldBeVisible)
            {
                string optionText = optionTexts[i];
                Button currentButton = buttons[i];

                TMP_Text text = currentButton.GetComponentInChildren<TMP_Text>(true);
                if (text != null)
                    text.text = optionText;

                RestoreButtonVisual(currentButton);
                currentButton.interactable = !isPanelInputLocked;

                currentButton.onClick.RemoveAllListeners();
                currentButton.onClick.AddListener(() => SelectRankOption(currentButton, optionText));
            }
        }

        Button backButton = GetButton(listObject, "BackButton");
        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }

    private void SelectRankOption(Button clickedButton, string optionText)
    {
        if (isPanelInputLocked)
            return;

        if (!CanSelectRank(optionText, out string blockReason))
        {
            AddLocalOnlyChatMessage(blockReason);
            FlashInvalidButton(clickedButton);
            Debug.Log(blockReason);
            return;
        }

        if (selectedOptionButton != null)
        {
            RestoreButtonVisual(selectedOptionButton);
        }

        selectedOptionButton = clickedButton;
        selectedRankText = optionText;

        ApplySelectedVisual(clickedButton);
        RefreshCheckButtonState();

        Debug.Log("Wybrany układ: " + selectedRankText);
    }

    private bool CanSelectRank(string candidateText, out string blockReason)
    {
        blockReason = string.Empty;

        TryResolveTurnManager();

        if (turnManager == null)
            return true;

        if (!turnManager.HasDeclarationThisRound)
            return true;

        string currentText = turnManager.CurrentDeclaredRankText;

        if (string.IsNullOrWhiteSpace(currentText))
            return true;

        string candidateId = GetHandIdFromOptionText(candidateText);
        string currentId = GetHandIdFromOptionText(currentText);

        if (string.IsNullOrEmpty(candidateId) || string.IsNullOrEmpty(currentId))
        {
            if (NormalizeText(candidateText) == NormalizeText(currentText))
            {
                blockReason = "Można przebić tylko wyższym układem.";
                return false;
            }

            Debug.LogWarning("HandRankPanelUI: nie udało się porównać układów: " + candidateText + " vs " + currentText);
            return true;
        }

        int candidateIndex = HandRankCatalog.GetIndex(candidateId);
        int currentIndex = HandRankCatalog.GetIndex(currentId);

        if (candidateIndex < 0 || currentIndex < 0)
        {
            if (NormalizeText(candidateText) == NormalizeText(currentText))
            {
                blockReason = "Można przebić tylko wyższym układem.";
                return false;
            }

            Debug.LogWarning("HandRankPanelUI: brak indeksu katalogu dla: " + candidateId + " / " + currentId);
            return true;
        }

        if (candidateIndex == currentIndex)
        {
            blockReason = "Można przebić tylko wyższym układem.";
            return false;
        }

        if (candidateIndex < currentIndex)
        {
            blockReason = "Można przebić tylko wyższym układem.";
            return false;
        }

        return true;
    }

    private void AddLocalOnlyChatMessage(string text)
    {
        TryResolveRoundLogUI();

        if (roundLogUI == null)
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        roundLogUI.AddSystemMessage(text);
    }

    private void FlashInvalidButton(Button button)
    {
        if (button == null)
            return;

        if (invalidFlashCoroutines.TryGetValue(button, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        invalidFlashCoroutines[button] = StartCoroutine(FlashInvalidButtonRoutine(button));
    }

    private IEnumerator FlashInvalidButtonRoutine(Button button)
    {
        if (button == null)
            yield break;

        ColorBlock colors = button.colors;
        colors.normalColor = invalidOptionColor;
        colors.highlightedColor = invalidOptionColor;
        colors.selectedColor = invalidOptionColor;
        button.colors = colors;

        yield return new WaitForSeconds(invalidFlashDuration);

        if (button != null)
        {
            if (button == selectedOptionButton)
                ApplySelectedVisual(button);
            else
                RestoreButtonVisual(button);
        }

        if (invalidFlashCoroutines.ContainsKey(button))
            invalidFlashCoroutines.Remove(button);
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
            parts[i] = NormalizeRankToken(rawParts[i]);
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
        string normalized = NormalizeRankToken(value);

        for (int i = 0; i < rankOrder.Length; i++)
        {
            if (rankOrder[i] == normalized)
                return true;
        }

        return false;
    }

    private int GetRankIndex(string value)
    {
        string normalized = NormalizeRankToken(value);

        for (int i = 0; i < rankOrder.Length; i++)
        {
            if (rankOrder[i] == normalized)
                return i;
        }

        return -1;
    }

    private string NormalizeRankToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim().ToUpper();

        if (normalized == "D" || normalized == "Q" || normalized == "QUEEN" || normalized == "DAMA")
            return "Q";

        return normalized;
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

    private void ClearSelectedRank()
    {
        if (selectedOptionButton != null)
        {
            RestoreButtonVisual(selectedOptionButton);
            selectedOptionButton = null;
        }

        selectedRankText = null;
        RefreshCheckButtonState();
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
        if (button == null || !hasCachedNormalColors)
            return;

        button.colors = cachedNormalColors;
    }

    private void RefreshCheckButtonVisual()
    {
        if (checkButtonVisual == null)
            return;

        if (string.IsNullOrEmpty(selectedRankText))
        {
            checkButtonVisual.color = cachedCheckButtonVisualColor;
            return;
        }

        checkButtonVisual.color = raiseButtonColor;
        
    }

    private void RefreshCheckButtonState()
    {
        if (checkButtonText != null)
            checkButtonText.text = string.IsNullOrEmpty(selectedRankText) ? "Sprawdzam" : "Przebij";

        RefreshCheckButtonVisual();

        if (checkButton != null)
        {
            bool canUseCheckButton = !isPanelInputLocked &&
                                     (!string.IsNullOrEmpty(selectedRankText) || canCheckCurrentTurn);

            checkButton.interactable = canUseCheckButton;
        }
    }

    private void HandleCheckButtonClicked()
    {
        if (isPanelInputLocked)
            return;

        if (string.IsNullOrEmpty(selectedRankText))
        {
            if (!canCheckCurrentTurn)
            {
                Debug.Log("Sprawdzam jest zablokowane w pierwszym ruchu rundy.");
                return;
            }

            Debug.Log("Akcja: Sprawdzam");
            LockPanelAfterAction();
            OnCheckChosen?.Invoke();
        }
        else
        {
            Debug.Log("Akcja: Przebij -> " + selectedRankText);
            LockPanelAfterAction();
            OnRaiseChosen?.Invoke(selectedRankText);
        }
    }

    private List<Button> GetOptionButtons(Transform listRoot)
    {
        List<Button> result = new List<Button>();

        if (listRoot == null)
            return result;

        for (int i = 0; i < listRoot.childCount; i++)
        {
            Transform child = listRoot.GetChild(i);

            if (child.name == "BackButton")
                continue;

            Button button = child.GetComponent<Button>();
            if (button != null)
                result.Add(button);
        }

        return result;
    }

    private void SetOnlyOneListActive(GameObject target)
    {
        if (categoryList != null) categoryList.SetActive(target == categoryList);
        if (rankOptionList != null) rankOptionList.SetActive(target == rankOptionList);
        if (fullGroupList != null) fullGroupList.SetActive(target == fullGroupList);
        if (fullDetailList != null) fullDetailList.SetActive(target == fullDetailList);
    }

    private void SetTitle(string value)
    {
        if (handRankTitle != null)
            handRankTitle.text = value;
    }

    private GameObject GetChildObject(Transform parent, string childName)
    {
        Transform child = FindDirectChild(parent, childName);
        return child != null ? child.gameObject : null;
    }

    private Button GetButton(GameObject parentObject, string childName)
    {
        if (parentObject == null)
            return null;

        Transform child = FindDirectChild(parentObject.transform, childName);
        if (child == null)
            return null;

        return child.GetComponent<Button>();
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
