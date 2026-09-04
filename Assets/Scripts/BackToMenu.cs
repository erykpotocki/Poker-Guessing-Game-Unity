using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class BackToMenu : MonoBehaviourPunCallbacks
{
    private const string ResumePendingPrefsKey = "ResumePending";
    private const string LastRoomCodePrefsKey = "lastRoomCode";
    private Button cornerBackButton;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Start()
    {
        ResolveCornerBackButton();
        ApplySafeAreaPosition();
    }

    private void Update()
    {
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.safeArea == lastSafeArea && screenSize == lastScreenSize)
            return;

        ApplySafeAreaPosition();
    }

    private void ResolveCornerBackButton()
    {
        Button bestCandidate = null;
        float bestScore = float.MinValue;

        foreach (Button button in FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            RectTransform rect = button.transform as RectTransform;
            if (label == null || rect == null ||
                label.text.Trim().ToUpperInvariant() != "X")
                continue;

            // Only a navigation control belongs near the upper-left corner.
            // This excludes the X buttons used for deleting player rows.
            float score = rect.anchorMin.y * 10f - rect.anchorMin.x;
            if (rect.anchorMin.y < 0.75f || rect.anchorMin.x > 0.25f ||
                score <= bestScore)
                continue;

            bestCandidate = button;
            bestScore = score;
        }

        cornerBackButton = bestCandidate;
    }

    private void ApplySafeAreaPosition()
    {
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (cornerBackButton == null)
            ResolveCornerBackButton();

        if (cornerBackButton == null ||
            !(cornerBackButton.transform is RectTransform buttonRect))
            return;

        Canvas canvas = cornerBackButton.GetComponentInParent<Canvas>();
        float canvasScale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
        float leftInset = Screen.safeArea.xMin / canvasScale;
        float topInset = (Screen.height - Screen.safeArea.yMax) / canvasScale;

        bool useBottomBackButton = SceneManager.GetActiveScene().name != "Game" &&
                                   SceneManager.GetActiveScene().name != "BootLoading";

        if (useBottomBackButton)
        {
            float bottomInset = Screen.safeArea.yMin / canvasScale;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, bottomInset + 38f);
            buttonRect.sizeDelta = new Vector2(380f, 92f);
            buttonRect.localScale = Vector3.one;

            TMP_Text bottomLabel = cornerBackButton.GetComponentInChildren<TMP_Text>(true);
            if (bottomLabel != null)
            {
                bottomLabel.text = "COFNIJ";
                bottomLabel.rectTransform.anchorMin = Vector2.zero;
                bottomLabel.rectTransform.anchorMax = Vector2.one;
                bottomLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bottomLabel.rectTransform.anchoredPosition = Vector2.zero;
                bottomLabel.rectTransform.offsetMin = new Vector2(18f, 8f);
                bottomLabel.rectTransform.offsetMax = new Vector2(-18f, -8f);
                bottomLabel.rectTransform.localScale = Vector3.one;
                bottomLabel.alignment = TextAlignmentOptions.Center;
                bottomLabel.textWrappingMode = TextWrappingModes.NoWrap;
                bottomLabel.overflowMode = TextOverflowModes.Ellipsis;
                bottomLabel.fontStyle = FontStyles.Bold;
                bottomLabel.enableAutoSizing = true;
                bottomLabel.fontSizeMin = 28f;
                bottomLabel.fontSizeMax = 38f;
            }

            PokerButtonTheme.ApplyTo(cornerBackButton);
            return;
        }

        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(
            leftInset + 24f,
            -(topInset + 24f));
        buttonRect.sizeDelta = new Vector2(112f, 112f);
        buttonRect.localScale = Vector3.one;

        Image background = cornerBackButton.targetGraphic as Image;
        if (background == null)
            background = cornerBackButton.GetComponent<Image>();
        if (background != null)
            background.color = Color.clear;

        cornerBackButton.transition = Selectable.Transition.None;

        TMP_Text label = cornerBackButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "×";
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.95f, 0.82f, 0.42f, 0.96f);
            label.fontStyle = FontStyles.Normal;
            label.enableAutoSizing = false;
            label.fontSize = 62f;

            Shadow shadow = label.GetComponent<Shadow>();
            if (shadow == null)
                shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;
        }
    }

    public void HideCornerButton()
    {
        if (cornerBackButton == null)
            ResolveCornerBackButton();

        if (cornerBackButton != null)
            cornerBackButton.gameObject.SetActive(false);
    }

    public void GoMainMenu()
    {
        HotSeatOrientationLock.LockPortrait();
        ClearResumeData();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    public override void OnLeftRoom()
    {
        HotSeatOrientationLock.LockPortrait();
        ClearResumeData();
        SceneManager.LoadScene("MainMenu");
    }

    private void ClearResumeData()
    {
        PlayerPrefs.SetInt(ResumePendingPrefsKey, 0);
        PlayerPrefs.DeleteKey(LastRoomCodePrefsKey);
        PlayerPrefs.Save();
    }
}
