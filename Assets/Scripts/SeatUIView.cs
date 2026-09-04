using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeatUIView : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nickText;
    [SerializeField] private RectTransform activeTurnHighlight;

    [Header("Highlight Pulse")]
    [SerializeField] private float pulseSpeed = 1.6f;
    [SerializeField] private float pulseScaleAmount = 0.04f;

    [Header("Eliminated Visual")]
    [SerializeField] private Color eliminatedAvatarTint = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color eliminatedNickTint = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Disconnected Visual")]
    [SerializeField] private Color disconnectedAvatarTint = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color disconnectedNickTint = new Color(0.4f, 0.4f, 0.4f, 1f);

    private bool isActiveTurn = false;
    private bool isEliminated = false;
    private bool isDisconnected = false;
    private Vector3 highlightBaseScale = Vector3.one;

    private Color defaultAvatarColor = Color.white;
    private Color defaultNickColor = Color.white;
    private Image circularAvatarContent;
    private static Sprite circleMaskSprite;
    private static Sprite circleRingSprite;

    private void Awake()
    {
        ConfigureCircularAvatar();
        ConfigureCircularHighlight();

        if (circularAvatarContent != null)
            defaultAvatarColor = circularAvatarContent.color;

        if (nickText != null)
            defaultNickColor = nickText.color;

        if (activeTurnHighlight != null)
        {
            highlightBaseScale = activeTurnHighlight.localScale;
            activeTurnHighlight.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isActiveTurn || isEliminated || activeTurnHighlight == null)
            return;

        float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseScaleAmount;
        activeTurnHighlight.localScale = highlightBaseScale * pulse;
    }

    public void Set(string nick, Sprite avatar)
    {
        if (nickText != null)
            nickText.text = nick;

        if (circularAvatarContent != null)
            circularAvatarContent.sprite = avatar;
        else if (avatarImage != null)
            avatarImage.sprite = avatar;
    }

    public void SetActiveTurnHighlight(bool isActive)
    {
        if (isEliminated)
            isActive = false;

        isActiveTurn = isActive;

        if (activeTurnHighlight == null)
            return;

        activeTurnHighlight.gameObject.SetActive(isActive);
        activeTurnHighlight.localScale = highlightBaseScale;
    }

    public void SetEliminatedVisual(bool value)
    {
        isEliminated = value;

        if (isEliminated)
        {
            isActiveTurn = false;

            if (activeTurnHighlight != null)
            {
                activeTurnHighlight.gameObject.SetActive(false);
                activeTurnHighlight.localScale = highlightBaseScale;
            }
        }

        ApplyCurrentVisualState();
    }

    public void SetDisconnectedVisual(bool value)
    {
        isDisconnected = value;

        if (isDisconnected)
        {
            isActiveTurn = false;

            if (activeTurnHighlight != null)
            {
                activeTurnHighlight.gameObject.SetActive(false);
                activeTurnHighlight.localScale = highlightBaseScale;
            }
        }

        ApplyCurrentVisualState();
    }

    public string GetDisplayedNick()
    {
        return nickText != null ? nickText.text : string.Empty;
    }

    private void ApplyCurrentVisualState()
    {
        Image visibleAvatar = circularAvatarContent != null
            ? circularAvatarContent
            : avatarImage;
        if (visibleAvatar != null)
        {
            if (isEliminated)
                visibleAvatar.color = eliminatedAvatarTint;
            else if (isDisconnected)
                visibleAvatar.color = disconnectedAvatarTint;
            else
                visibleAvatar.color = defaultAvatarColor;
        }

        if (nickText != null)
        {
            if (isEliminated)
                nickText.color = eliminatedNickTint;
            else if (isDisconnected)
                nickText.color = disconnectedNickTint;
            else
                nickText.color = defaultNickColor;
        }
    }

    private void ConfigureCircularAvatar()
    {
        if (avatarImage == null)
            return;

        EnsureCircleSprites();
        RectTransform avatarRect = avatarImage.rectTransform;
        avatarRect.sizeDelta = new Vector2(120f, 120f);
        avatarRect.anchoredPosition = new Vector2(0f, -4f);

        Sprite currentAvatar = avatarImage.sprite;
        avatarImage.sprite = circleMaskSprite;
        avatarImage.color = Color.white;
        avatarImage.preserveAspect = false;

        Mask mask = avatarImage.GetComponent<Mask>();
        if (mask == null)
            mask = avatarImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        Transform existing = avatarImage.transform.Find("CircularAvatarContent");
        if (existing != null)
            circularAvatarContent = existing.GetComponent<Image>();
        if (circularAvatarContent == null)
        {
            GameObject content = new GameObject(
                "CircularAvatarContent", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(avatarImage.transform, false);
            circularAvatarContent = content.GetComponent<Image>();
        }

        RectTransform contentRect = circularAvatarContent.rectTransform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        circularAvatarContent.sprite = currentAvatar;
        circularAvatarContent.color = Color.white;
        circularAvatarContent.preserveAspect = false;
        circularAvatarContent.raycastTarget = false;
    }

    private void ConfigureCircularHighlight()
    {
        if (activeTurnHighlight == null)
            return;

        EnsureCircleSprites();
        activeTurnHighlight.sizeDelta = new Vector2(146f, 146f);
        activeTurnHighlight.anchoredPosition = new Vector2(0f, 9f);

        Image highlightImage = activeTurnHighlight.GetComponent<Image>();
        if (highlightImage != null)
        {
            highlightImage.sprite = circleRingSprite;
            highlightImage.color = new Color(1f, 0.72f, 0.18f, 0.9f);
            highlightImage.preserveAspect = true;
            highlightImage.raycastTarget = false;
        }
    }

    private static void EnsureCircleSprites()
    {
        if (circleMaskSprite != null && circleRingSprite != null)
            return;

        const int size = 128;
        Texture2D maskTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Texture2D ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        maskTexture.name = "AvatarCircleMask";
        ringTexture.name = "ActiveTurnCircleRing";
        maskTexture.hideFlags = HideFlags.HideAndDontSave;
        ringTexture.hideFlags = HideFlags.HideAndDontSave;

        Color[] maskPixels = new Color[size * size];
        Color[] ringPixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = size * 0.49f;
        float innerRadius = size * 0.41f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float maskAlpha = Mathf.Clamp01(outerRadius - distance + 1f);
                float ringAlpha = Mathf.Clamp01(outerRadius - distance + 1f) *
                    Mathf.Clamp01(distance - innerRadius + 1f);
                int index = y * size + x;
                maskPixels[index] = new Color(1f, 1f, 1f, maskAlpha);
                ringPixels[index] = new Color(1f, 1f, 1f, ringAlpha);
            }
        }

        maskTexture.SetPixels(maskPixels);
        ringTexture.SetPixels(ringPixels);
        maskTexture.Apply(false, true);
        ringTexture.Apply(false, true);
        circleMaskSprite = Sprite.Create(maskTexture,
            new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        circleRingSprite = Sprite.Create(ringTexture,
            new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
