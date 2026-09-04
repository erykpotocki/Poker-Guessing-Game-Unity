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
        foreach (Button button in FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text.Trim().ToUpperInvariant() == "X")
            {
                cornerBackButton = button;
                return;
            }
        }
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

        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(
            Mathf.Max(28f, leftInset + 20f),
            -Mathf.Max(116f, topInset + 28f));
        buttonRect.sizeDelta = new Vector2(148f, 148f);
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
            label.fontSize = 108f;

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
