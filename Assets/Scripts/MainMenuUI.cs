using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private Transform menuGroup;
    private Button primaryButton;
    private Button secondaryButton;
    private Button rulesButton;
    private Button settingsButton;
    private Button backButton;
    private GameObject infoOverlay;
    private TMP_Text infoTitle;
    private TMP_Text infoBadge;
    private TMP_Text infoBody;
    private CanvasGroup infoCanvasGroup;
    private CanvasGroup screenCanvasGroup;
    private bool transitionInProgress;

    private const string RulesText =
        "<b>CEL GRY</b>\nZostań ostatnim graczem, który nie odpadł.\n\n" +
        "<b>POCZĄTEK RUNDY</b>\nKażdy po kolei ogląda swoją kartę i przekazuje telefon dalej. " +
        "Nie pokazuj swojej karty pozostałym graczom.\n\n" +
        "<b>W SWOJEJ TURZE</b>\nZadeklaruj układ wyższy od poprzedniego albo sprawdź, " +
        "czy poprzedni gracz mówi prawdę. Układ tworzą wszystkie karty znajdujące się na stole.\n\n" +
        "<b>SPRAWDZENIE</b>\nJeżeli zadeklarowany układ jest na stole, przegrywa sprawdzający. " +
        "Jeżeli go nie ma — przegrywa gracz, który go zadeklarował.\n\n" +
        "<b>KARA</b>\nPrzegrany dostaje kolejną kartę. Liczba kart zmienia się: " +
        "1 → 2 → 3 → 2 → 1, a następna porażka oznacza odpadnięcie.\n\n" +
        "<b>PAMIĘTAJ</b>\nW każdej rundzie musisz przebić poprzednią deklarację albo ją sprawdzić.";

    private const string SettingsText =
        "<b>DŹWIĘK I MUZYKA</b>\nPrzełącznik muzyki oraz efektów dźwiękowych pojawi się tutaj wkrótce.\n\n" +
        "<b>JĘZYK</b>\nGra korzysta obecnie z języka polskiego. W przyszłości dodamy wybór języka.\n\n" +
        "<b>WIĘCEJ OPCJI</b>\nTo miejsce jest przygotowane na kolejne ustawienia gry.";

    private void Awake()
    {
        PokerButtonTheme.EnsureController();
        BuildMenu();
        StartCoroutine(AnimateMenuEntrance());
    }

    private void BuildMenu()
    {
        Button[] authoredButtons = FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Button styleSource = null;

        foreach (Button button in authoredButtons)
        {
            if (button == null)
                continue;

            if (styleSource == null && button.name.Contains("Stwórz"))
                styleSource = button;

            if (button.name == "Ustawienia" || button.name.Contains("Stwórz") ||
                button.name.Contains("Dołącz") || button.name == "Hot Seat")
            {
                if (menuGroup == null)
                    menuGroup = button.transform.parent;
                button.gameObject.SetActive(false);
            }
        }

        if (menuGroup == null)
            return;

        menuGroup.localScale = new Vector3(3.25f, 2.5f, 1f);

        primaryButton = CreateMenuButton(
            "PrimaryModeButton", styleSource, new Vector2(62.8f, -94f), new Vector2(218f, 52f), true);
        secondaryButton = CreateMenuButton(
            "SecondaryModeButton", styleSource, new Vector2(62.8f, -166f), new Vector2(218f, 52f), true);
        rulesButton = CreateMenuButton(
            "RulesButton", styleSource, new Vector2(6.3f, -232f), new Vector2(105f, 34f), false);
        settingsButton = CreateMenuButton(
            "SettingsButton", styleSource, new Vector2(119.3f, -232f), new Vector2(105f, 34f), false);
        backButton = CreateMenuButton(
            "ModeBackButton", styleSource, new Vector2(62.8f, -238f), new Vector2(218f, 38f), false);

        Canvas canvas = menuGroup.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            screenCanvasGroup = canvas.GetComponent<CanvasGroup>();
            if (screenCanvasGroup == null)
                screenCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            BuildInfoOverlay(canvas.transform, styleSource);
        }

        ShowMainChoices();
    }

    private Button CreateMenuButton(
        string objectName,
        Button source,
        Vector2 position,
        Vector2 size,
        bool withSubtitle)
    {
        GameObject buttonObject = new GameObject(
            objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(menuGroup, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        Image sourceImage = source != null ? source.GetComponent<Image>() : null;
        if (sourceImage != null)
        {
            image.sprite = sourceImage.sprite;
            image.type = sourceImage.type;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text title = CreateText("Title", buttonObject.transform);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = withSubtitle ? new Vector2(0f, 0.43f) : Vector2.zero;
        titleRect.anchorMax = withSubtitle ? new Vector2(1f, 0.96f) : Vector2.one;
        titleRect.offsetMin = new Vector2(9f, withSubtitle ? 0f : 3f);
        titleRect.offsetMax = new Vector2(-9f, withSubtitle ? -1f : -3f);
        title.fontSizeMin = withSubtitle ? 7f : 6f;
        title.fontSizeMax = withSubtitle ? 11f : 9f;

        if (withSubtitle)
        {
            TMP_Text subtitle = CreateText("Subtitle", buttonObject.transform);
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 0.08f);
            subtitleRect.anchorMax = new Vector2(1f, 0.43f);
            subtitleRect.offsetMin = new Vector2(12f, 0f);
            subtitleRect.offsetMax = new Vector2(-12f, 0f);
            subtitle.fontStyle = FontStyles.Normal;
            subtitle.fontSizeMin = 5f;
            subtitle.fontSizeMax = 7.5f;
            subtitle.color = new Color(0.91f, 0.81f, 0.63f, 1f);
        }

        PokerButtonTheme.ApplyTo(button);
        return button;
    }

    private static TMP_Text CreateText(string name, Transform parent)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.95f, 0.82f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private void BuildInfoOverlay(Transform canvasTransform, Button styleSource)
    {
        infoOverlay = new GameObject(
            "MainMenuInfoOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        infoOverlay.transform.SetParent(canvasTransform, false);
        infoOverlay.transform.SetAsLastSibling();

        RectTransform overlayRect = infoOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image shade = infoOverlay.GetComponent<Image>();
        shade.color = new Color(0.005f, 0.018f, 0.018f, 0.96f);
        infoCanvasGroup = infoOverlay.GetComponent<CanvasGroup>();

        GameObject panelObject = new GameObject(
            "Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(infoOverlay.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.075f, 0.08f);
        panelRect.anchorMax = new Vector2(0.925f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.015f, 0.075f, 0.067f, 0.98f);
        Outline panelOutline = panelObject.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.78f, 0.48f, 0.12f, 0.9f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        infoBadge = CreateOverlayText("Badge", panelObject.transform, 0.84f, 0.90f, 17f, 24f);
        infoBadge.color = new Color(0.92f, 0.68f, 0.24f, 1f);
        infoBadge.characterSpacing = 2f;

        infoTitle = CreateOverlayText("Heading", panelObject.transform, 0.73f, 0.86f, 28f, 46f);
        infoTitle.color = new Color(1f, 0.90f, 0.55f, 1f);

        GameObject dividerObject = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        dividerObject.transform.SetParent(panelObject.transform, false);
        RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.12f, 0.705f);
        dividerRect.anchorMax = new Vector2(0.88f, 0.712f);
        dividerRect.offsetMin = Vector2.zero;
        dividerRect.offsetMax = Vector2.zero;
        dividerObject.GetComponent<Image>().color = new Color(0.78f, 0.48f, 0.12f, 0.75f);

        infoBody = CreateOverlayText("Body", panelObject.transform, 0.19f, 0.68f, 16f, 25f);
        infoBody.alignment = TextAlignmentOptions.TopLeft;
        infoBody.fontStyle = FontStyles.Normal;
        infoBody.color = new Color(0.96f, 0.91f, 0.81f, 1f);
        infoBody.lineSpacing = 5f;
        infoBody.textWrappingMode = TextWrappingModes.Normal;
        infoBody.rectTransform.offsetMin = new Vector2(34f, 0f);
        infoBody.rectTransform.offsetMax = new Vector2(-34f, 0f);

        Button closeButton = CreateOverlayButton(panelObject.transform, styleSource);
        closeButton.onClick.AddListener(CloseInfoOverlay);

        infoOverlay.SetActive(false);
    }

    private static TMP_Text CreateOverlayText(
        string name, Transform parent, float anchorBottom, float anchorTop, float minSize, float maxSize)
    {
        TMP_Text text = CreateText(name, parent);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, anchorBottom);
        rect.anchorMax = new Vector2(1f, anchorTop);
        rect.offsetMin = new Vector2(28f, 0f);
        rect.offsetMax = new Vector2(-28f, 0f);
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        return text;
    }

    private static Button CreateOverlayButton(Transform parent, Button source)
    {
        GameObject buttonObject = new GameObject(
            "InfoBackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.055f);
        rect.anchorMax = new Vector2(0.85f, 0.145f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        Image sourceImage = source != null ? source.GetComponent<Image>() : null;
        if (sourceImage != null)
        {
            image.sprite = sourceImage.sprite;
            image.type = sourceImage.type;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        TMP_Text label = CreateText("Title", buttonObject.transform);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(16f, 4f);
        label.rectTransform.offsetMax = new Vector2(-16f, -4f);
        label.text = "WRÓĆ";
        label.fontSizeMin = 16f;
        label.fontSizeMax = 26f;
        PokerButtonTheme.ApplyTo(button);
        return button;
    }

    private void ShowMainChoices()
    {
        if (transitionInProgress || primaryButton == null)
            return;

        ConfigureButton(primaryButton, "MULTIPLAYER", "Graj online", ShowMultiplayerOptions);
        ConfigureButton(secondaryButton, "GRA NA JEDNYM TELEFONIE", "Graj offline", GoHotSeat);
        ConfigureButton(rulesButton, "ZASADY", string.Empty, ShowRules);
        ConfigureButton(settingsButton, "USTAWIENIA", string.Empty, ShowSettings);
        rulesButton.gameObject.SetActive(true);
        settingsButton.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        StartCoroutine(AnimateButtons(primaryButton, secondaryButton, rulesButton, settingsButton));
    }

    private void ShowMultiplayerOptions()
    {
        if (transitionInProgress || primaryButton == null)
            return;

        ConfigureButton(primaryButton, "STWÓRZ POKÓJ", "Załóż nową grę online", GoCreateRoom);
        ConfigureButton(secondaryButton, "DOŁĄCZ DO POKOJU", "Wpisz kod pokoju", GoJoinRoom);
        ConfigureButton(backButton, "WRÓĆ", string.Empty, ShowMainChoices);
        rulesButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        StartCoroutine(AnimateButtons(primaryButton, secondaryButton, backButton));
    }

    private static void ConfigureButton(
        Button button, string title, string subtitle, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);

        Transform titleTransform = button.transform.Find("Title");
        if (titleTransform != null)
            titleTransform.GetComponent<TMP_Text>().text = title;

        Transform subtitleTransform = button.transform.Find("Subtitle");
        if (subtitleTransform != null)
        {
            TMP_Text subtitleText = subtitleTransform.GetComponent<TMP_Text>();
            subtitleText.text = subtitle;
            subtitleTransform.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitle));
        }
    }

    private void ShowRules()
    {
        ShowInfo("ZASADY GRY", "JAK GRAĆ", RulesText);
    }

    private void ShowSettings()
    {
        ShowInfo("USTAWIENIA", "W PRZYGOTOWANIU", SettingsText);
    }

    private void ShowInfo(string title, string badge, string body)
    {
        if (infoOverlay == null || transitionInProgress)
            return;

        infoTitle.text = title;
        infoBadge.text = badge;
        infoBody.text = body;
        menuGroup.gameObject.SetActive(false);
        infoOverlay.SetActive(true);
        StartCoroutine(AnimateInfoOverlay(true));
    }

    private void CloseInfoOverlay()
    {
        if (infoOverlay != null && infoOverlay.activeSelf)
            StartCoroutine(AnimateInfoOverlay(false));
    }

    private IEnumerator AnimateInfoOverlay(bool appearing)
    {
        float start = appearing ? 0f : 1f;
        float end = appearing ? 1f : 0f;
        infoCanvasGroup.alpha = start;
        infoCanvasGroup.interactable = appearing;
        infoCanvasGroup.blocksRaycasts = appearing;

        float elapsed = 0f;
        const float duration = 0.24f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOut(Mathf.Clamp01(elapsed / duration));
            infoCanvasGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        infoCanvasGroup.alpha = end;
        if (!appearing)
        {
            infoOverlay.SetActive(false);
            menuGroup.gameObject.SetActive(true);
            StartCoroutine(AnimateButtons(primaryButton, secondaryButton, rulesButton, settingsButton));
        }
    }

    private IEnumerator AnimateMenuEntrance()
    {
        yield return null;
        if (screenCanvasGroup != null)
            screenCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        const float duration = 0.48f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (screenCanvasGroup != null)
                screenCanvasGroup.alpha = EaseOut(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (screenCanvasGroup != null)
            screenCanvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateButtons(params Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            if (button == null || !button.gameObject.activeSelf)
                continue;

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
                group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            button.transform.localScale = Vector3.one * 0.92f;

            float elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOut(Mathf.Clamp01(elapsed / duration));
                group.alpha = t;
                button.transform.localScale = Vector3.LerpUnclamped(
                    Vector3.one * 0.92f, Vector3.one, t);
                yield return null;
            }

            group.alpha = 1f;
            button.transform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.055f);
        }
    }

    private static float EaseOut(float value)
    {
        return 1f - Mathf.Pow(1f - value, 3f);
    }

    public void GoCreateRoom() { StartSceneTransition("GameModeSelect"); }
    public void GoJoinRoom() { StartSceneTransition("JoinRoom"); }

    public void GoHotSeat()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        StartSceneTransition("Hot Seat");
    }

    private void StartSceneTransition(string sceneName)
    {
        if (!transitionInProgress)
            StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        transitionInProgress = true;
        float startAlpha = screenCanvasGroup != null ? screenCanvasGroup.alpha : 1f;
        float elapsed = 0f;
        const float duration = 0.32f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (screenCanvasGroup != null)
                screenCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f,
                    Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}
