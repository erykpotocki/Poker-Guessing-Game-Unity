using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SeatUIView : MonoBehaviour
{
    public RectTransform ProfileAnchor => avatarImage!=null?avatarImage.rectTransform:transform as RectTransform;
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
    private GameObject readyIndicator;
    private Image cosmeticFrame;
    private RectTransform dealerTable;
    public void PositionDealer(RectTransform table)
    {
        dealerTable=table; if(nickText!=null)nickText.gameObject.SetActive(false);
        Rect bounds=table.rect;
        PositionAvatarCenter(table.TransformPoint(bounds.center+new Vector2(0,bounds.height*.485f)));
    }
    public void PositionAvatarCenter(Vector3 worldCenter)
    {
        if(avatarImage==null){transform.position=worldCenter;return;}
        transform.position+=worldCenter-avatarImage.rectTransform.TransformPoint(avatarImage.rectTransform.rect.center);
    }
    private TurnManager turnClock;

    public void SetReadyIndicator(bool ready)
    {
        if (readyIndicator == null && ready && avatarImage != null)
        {
            EnsureCircleSprites();
            readyIndicator = new GameObject("ReadyIndicator", typeof(RectTransform), typeof(Image));
            RectTransform rect = readyIndicator.GetComponent<RectTransform>();
            rect.SetParent(avatarImage.transform.parent, false);
            rect.anchorMin = avatarImage.rectTransform.anchorMin;
            rect.anchorMax = avatarImage.rectTransform.anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one*184f;
            rect.SetAsFirstSibling();
            Image ring = readyIndicator.GetComponent<Image>();
            ring.sprite = circleRingSprite;
            ring.color = new Color(0.16f, 1f, 0.36f);
            ring.raycastTarget = false;
        }
        if (readyIndicator != null) readyIndicator.SetActive(ready);
    }

    private void LateUpdate()
    {
        bool framed=cosmeticFrame!=null&&cosmeticFrame.gameObject.activeSelf;
        float haloDiameter=framed?184f:120f+64f*.67f;
        if(activeTurnHighlight!=null)activeTurnHighlight.sizeDelta=Vector2.one*haloDiameter;
        if(readyIndicator!=null)((RectTransform)readyIndicator.transform).sizeDelta=Vector2.one*haloDiameter;
        if (readyIndicator != null && readyIndicator.activeSelf && avatarImage != null)
            readyIndicator.transform.position = avatarImage.rectTransform.TransformPoint(avatarImage.rectTransform.rect.center);
        if (avatarImage == null) return;
        Vector3 center = avatarImage.rectTransform.TransformPoint(avatarImage.rectTransform.rect.center);
        if (cosmeticFrame != null) cosmeticFrame.rectTransform.position = center;
        if (activeTurnHighlight != null) activeTurnHighlight.position = center;
        if (nickText != null)
        {
            nickText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            nickText.rectTransform.position = avatarImage.rectTransform.TransformPoint(
                avatarImage.rectTransform.rect.center + new Vector2(0f, -90f));
            nickText.rectTransform.sizeDelta = new Vector2(230f, 40f);
            if(dealerTable!=null)
            {
                Rect bounds=dealerTable.rect;
                nickText.rectTransform.position=dealerTable.TransformPoint(bounds.center+new Vector2(0,bounds.height*.35f));
                nickText.rectTransform.sizeDelta=new Vector2(330,44);
            }
            nickText.alignment = TextAlignmentOptions.Center;
            nickText.transform.SetAsLastSibling();
        }
    }

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

        float wave = .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(3f, pulseSpeed));
        if(turnClock==null)turnClock=FindFirstObjectByType<TurnManager>();
        bool overtime=turnClock!=null && !turnClock.IsResolutionLocked && turnClock.CurrentTurnTimeLeft<=0;
        var ring=activeTurnHighlight.GetComponent<Image>();
        if(ring!=null)ring.color=overtime?new Color(1,.16f,.12f):new Color(1,.75f,.18f);
        if(overtime)wave=.5f+.5f*Mathf.Sin(Time.unscaledTime*6);
        activeTurnHighlight.localScale = highlightBaseScale * (1f + wave * Mathf.Clamp(pulseScaleAmount, .04f, .07f));
        if(ring!=null){Color ink=ring.color;ink.a=.3f+.7f*wave;ring.color=ink;}
    }

    public void Set(string nick, Sprite avatar)
    {
        if(circularAvatarContent==null)ConfigureCircularAvatar();
        if(avatarImage!=null)
        {
            avatarImage.overrideSprite=null;
            avatarImage.sprite=circleMaskSprite;
            avatarImage.type=Image.Type.Simple;
            avatarImage.material=null;
            var mask=avatarImage.GetComponent<Mask>();
            if(mask!=null){mask.enabled=true;mask.showMaskGraphic=false;}
        }
        if(circularAvatarContent!=null){circularAvatarContent.maskable=true;circularAvatarContent.material=null;}
        if (nickText != null)
            nickText.text = nick;

        if (circularAvatarContent != null)
            circularAvatarContent.sprite = avatar;
        else if (avatarImage != null)
            avatarImage.sprite = avatar;
    }

    public void SetActiveTurnHighlight(bool isActive)
    {
        SetTurnHighlight(isActive);
    }

    public void ConfigureProfileButton(UnityAction action)
    {
        if (avatarImage == null || action == null) return;
        Button button = avatarImage.GetComponent<Button>();
        if (button == null) button = avatarImage.gameObject.AddComponent<Button>();
        button.targetGraphic = avatarImage;
        button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        avatarImage.raycastTarget = true;
    }

    public void ApplyFrame(string frameId)
    {
        Sprite frameSprite=LevelFrameCatalog.Resolve(frameId);
        bool enabled = frameSprite!=null;
        if (!enabled)
        {
            if (cosmeticFrame != null) cosmeticFrame.gameObject.SetActive(false);
            return;
        }
        if (cosmeticFrame == null && avatarImage != null)
        {
            GameObject frame = new GameObject("CosmeticFrame",typeof(RectTransform),typeof(Image));
            frame.transform.SetParent(avatarImage.transform.parent,false);
            cosmeticFrame = frame.GetComponent<Image>();
            cosmeticFrame.sprite = Resources.Load<Sprite>("Cosmetics/ClassicWood");
            cosmeticFrame.preserveAspect = true;
            cosmeticFrame.raycastTarget = false;
            cosmeticFrame.rectTransform.anchorMin = avatarImage.rectTransform.anchorMin;
            cosmeticFrame.rectTransform.anchorMax = avatarImage.rectTransform.anchorMax;
            cosmeticFrame.rectTransform.pivot = new Vector2(.5f,.5f);
            cosmeticFrame.rectTransform.sizeDelta = new Vector2(154f,154f);
            cosmeticFrame.transform.SetSiblingIndex(avatarImage.transform.GetSiblingIndex()+1);
        }
        if (cosmeticFrame != null) {cosmeticFrame.sprite=frameSprite;cosmeticFrame.gameObject.SetActive(frameSprite != null);}
    }

    public void ConfigureDealerCaption()
    {
        if (nickText == null || avatarImage == null) return;
        nickText.gameObject.SetActive(false);
        avatarImage.rectTransform.sizeDelta=Vector2.one*136;
        RectTransform caption = nickText.rectTransform;
        caption.anchorMin = caption.anchorMax = new Vector2(0.5f, 0.5f);
        caption.anchoredPosition = avatarImage.rectTransform.anchoredPosition + new Vector2(0f, -90f);
        caption.sizeDelta = new Vector2(210f, 40f);
        nickText.fontSize = 32f;
        nickText.fontStyle = FontStyles.Bold;
        nickText.textWrappingMode=TextWrappingModes.NoWrap;
        var shadow=nickText.GetComponent<Shadow>();
        if(shadow==null)shadow=nickText.gameObject.AddComponent<Shadow>();
        shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-2);
        nickText.enableAutoSizing = false;
        nickText.alignment = TextAlignmentOptions.Center;
        nickText.color = new Color(1f, 0.91f, 0.7f);
    }

    private void SetTurnHighlight(bool isActive)
    {
        if (isEliminated)
            isActive = false;

        isActiveTurn = isActive;

        if (activeTurnHighlight == null)
            return;

        activeTurnHighlight.gameObject.SetActive(isActive);
        if(!isActive)activeTurnHighlight.localScale = highlightBaseScale;
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
        avatarImage.overrideSprite=null;
        avatarImage.type=Image.Type.Simple;
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
        if(circularAvatarContent.GetComponent<CircularAvatarMesh>()==null)circularAvatarContent.gameObject.AddComponent<CircularAvatarMesh>();
    }

    private void ConfigureCircularHighlight()
    {
        if (activeTurnHighlight == null)
            return;

        EnsureCircleSprites();
        activeTurnHighlight.sizeDelta = new Vector2(184f, 184f);
        activeTurnHighlight.pivot = new Vector2(0.5f, 0.5f);
        activeTurnHighlight.SetAsFirstSibling();
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
        // At 184 units the inner edge slightly overlaps the 120-unit avatar.
        // The avatar and cosmetic frame render above this continuous halo.
        float innerRadius = size * 0.30f;

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
