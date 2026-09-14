using UnityEngine;
using UnityEngine.UI;

public sealed class MultiplayerPanelLayout : MonoBehaviour
{
    public static float PanelWidth(float canvasWidth)=>Mathf.Clamp(canvasWidth*.19f,280,365);
    private RectTransform panel, root, action, list, exit;
    private GameUtilityBar utilityBar;
    private void Start()
    {
        panel = transform as RectTransform;
        var backdrop=GetComponent<Image>();if(backdrop==null)backdrop=gameObject.AddComponent<Image>();
        backdrop.sprite=null;backdrop.color=new Color(.018f,.012f,.018f,.94f);
        root = GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        if(panel.parent!=root)panel.SetParent(root,false);
        action = transform.Find("CheckButton") as RectTransform;
        list = transform.Find("RankScrollView") as RectTransform;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == "OnClickOpenLeaveConfirm")
                    exit = button.transform as RectTransform;
        utilityBar = gameObject.AddComponent<GameUtilityBar>();
        utilityBar.Initialize(GetComponentInParent<Canvas>(), exit != null ? exit.GetComponent<Button>() : null);
        ApplyLayout();
    }

    private void LateUpdate() => ApplyLayout();
    private void ApplyLayout()
    {
        if (root == null || root.rect.width <= 0f || Screen.width <= 0) return;
        Rect safe = Screen.safeArea;
        float sx = root.rect.width / Screen.width;
        float sy = root.rect.height / Screen.height;
        float right = (Screen.width - safe.xMax) * sx + 26f;
        float top = (Screen.height - safe.yMax) * sy + 26f;
        float bottom = safe.yMin * sy + 26f;
        float buttonScale=Mathf.Clamp(PlayerPrefs.GetFloat("ui.handButtonScale",1f),.8f,1.6f);
        float actionHeight=78f*buttonScale;
        float width = PanelWidth(root.rect.width);
        panel.anchorMin = new Vector2(1f, 0f);
        panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(1f, 0.5f);
        panel.offsetMin = new Vector2(-width, 0f);
        // Extend the backdrop to the physical edge; keep interactive content
        // inside the safe area instead of moving the entire panel inwards.
        panel.offsetMax = Vector2.zero;
        var searchIcon=panel.Find("UtilitySearchToggle") as RectTransform;
        if(searchIcon!=null)searchIcon.anchoredPosition=new Vector2(-14f,-100f);
        var searchInput=panel.Find("UtilityHandSearch") as RectTransform;
        if(searchInput!=null)searchInput.offsetMax=new Vector2(-68f,-100f);
        if (action != null)
        {
            action.anchorMin = Vector2.zero;
            action.anchorMax = new Vector2(1f, 0f);
            action.pivot = new Vector2(0.5f, 0f);
            action.offsetMin = new Vector2(14f, bottom);
            action.offsetMax = new Vector2(-26f, bottom + actionHeight);
            TMPro.TMP_Text label = action.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null){CenterLabel(label);label.enableAutoSizing=true;label.fontSizeMin=18;label.fontSizeMax=32*buttonScale;}
        }
        if (list != null)
        {
            list.anchorMin = Vector2.zero;
            list.anchorMax = Vector2.one;
            list.offsetMin = new Vector2(14f, bottom + actionHeight + 20f);
            list.offsetMax = new Vector2(-26f, -top - 145f);
            ScrollRect scroll = list.GetComponent<ScrollRect>();
            if (scroll != null && scroll.viewport != null)
            {
                scroll.vertical=true;scroll.horizontal=false;scroll.scrollSensitivity=65;
                scroll.movementType=ScrollRect.MovementType.Clamped;
                var hit=scroll.viewport.GetComponent<Image>();
                if(hit==null)hit=scroll.viewport.gameObject.AddComponent<Image>();
                // Stencil masks need opaque pixels even when their own graphic is hidden.
                var mask=scroll.viewport.GetComponent<Mask>();
                if(mask!=null){mask.showMaskGraphic=false;hit.color=Color.white;}
                else hit.color=Color.clear;
                hit.raycastTarget=true;
                scroll.viewport.anchorMin = Vector2.zero;
                scroll.viewport.anchorMax = Vector2.one;
                scroll.viewport.offsetMin = scroll.viewport.offsetMax = Vector2.zero;
                // The actual visible list must be the scroll content, not its
                // fixed-height wrapper containing the four alternative lists.
                foreach(VerticalLayoutGroup group in list.GetComponentsInChildren<VerticalLayoutGroup>(false))
                    if(group.name=="CategoryList"||group.name=="RankOptionList"||group.name=="FullGroupList"||group.name=="FullDetailList")
                    {
                        var active=group.transform as RectTransform;
                        if(active.parent!=scroll.viewport)active.SetParent(scroll.viewport,false);
                        var wrapper=active.parent as RectTransform;
                        if(wrapper!=null && wrapper!=scroll.viewport)
                        {
                            wrapper.anchorMin=Vector2.zero;wrapper.anchorMax=Vector2.one;
                            wrapper.offsetMin=wrapper.offsetMax=Vector2.zero;
                        }
                        if(scroll.content!=active){scroll.StopMovement();scroll.content=active;active.anchoredPosition=Vector2.zero;}
                        active.anchorMin=new Vector2(0,1);active.anchorMax=Vector2.one;active.pivot=new Vector2(.5f,1);
                        break;
                    }
            }
            foreach (VerticalLayoutGroup layout in list.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {
                layout.padding.left = layout.padding.right = 0;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                var fitter=layout.GetComponent<ContentSizeFitter>();
                if(fitter==null)fitter=layout.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                RectTransform rect = layout.transform as RectTransform;
                rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
                rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            }
            foreach (Button button in list.GetComponentsInChildren<Button>(true))
            {
                LayoutElement element = button.GetComponent<LayoutElement>();
                if(element==null)element=button.gameObject.AddComponent<LayoutElement>();
                element.minWidth=0; element.preferredWidth=-1; element.flexibleWidth=1;
                float available=root.rect.height-top-bottom-210;
                element.preferredHeight=Mathf.Max(60,available/9-8)*buttonScale;
                element.minHeight=element.preferredHeight;
                element.flexibleHeight=0;
                TMPro.TMP_Text label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (label != null){CenterLabel(label);label.enableAutoSizing=true;label.fontSizeMin=18;label.fontSizeMax=32*buttonScale;}
            }
        }
        if (utilityBar != null) utilityBar.SetBounds(top, right);
    }

    private static void CenterLabel(TMPro.TMP_Text label)
    {
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.offsetMin = new Vector2(8f, 4f);
        rect.offsetMax = new Vector2(-8f, -4f);
        label.margin = Vector4.zero;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    }
}
