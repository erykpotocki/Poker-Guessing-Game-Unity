using System;
using PokerProfile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class PlayerProfileUI : MonoBehaviour, IPointerDownHandler
{
    private RectTransform root, body;
    private string message = "";
    private string section = "avatar";
    private int avatarCategory;
    private bool offlineBacks;
    private TMP_InputField nicknameInput;
    private bool keyboardWasVisible;
    private Image nicknameCaret;
    private int lastCaretPosition=-1;
    private float caretChangedAt;
    private bool focusNicknameAtEnd;
    private void LateUpdate()
    {
        if(nicknameCaret==null||nicknameInput==null)return;
        bool focused=nicknameInput.isFocused;
        if(focused&&focusNicknameAtEnd)
        {
            nicknameInput.MoveTextEnd(false);nicknameInput.ForceLabelUpdate();
            focusNicknameAtEnd=false;lastCaretPosition=-1;caretChangedAt=Time.unscaledTime;
        }
        int position=nicknameInput.caretPosition;
        if(!focused){nicknameCaret.enabled=false;lastCaretPosition=-1;return;}
        if(position!=lastCaretPosition){lastCaretPosition=position;caretChangedAt=Time.unscaledTime;}
        nicknameCaret.enabled=nicknameInput.selectionAnchorPosition==nicknameInput.selectionFocusPosition &&
            Mathf.Repeat(Time.unscaledTime-caretChangedAt,1.1f)<.65f;
        TMP_Text text=nicknameInput.textComponent;
        text.ForceMeshUpdate();
        var info=text.textInfo;
        float x=0,top=text.fontSize*.8f,bottom=-text.fontSize*.2f;
        if(info.characterCount>0)
        {
            int index=Mathf.Clamp(position>0?position-1:0,0,info.characterCount-1);
            var character=info.characterInfo[index];
            x=position>0?character.xAdvance:character.origin;
            top=character.ascender;bottom=character.descender;
        }
        else{x=text.rectTransform.rect.xMin;top=text.rectTransform.rect.center.y+text.fontSize*.5f;bottom=top-text.fontSize;}
        RectTransform caret=nicknameCaret.rectTransform;
        caret.position=text.rectTransform.TransformPoint(new Vector3(x+2,(top+bottom)*.5f,0));
        var caretViewport=nicknameInput.textViewport;
        var caretPosition=caretViewport.InverseTransformPoint(caret.position);
        caretPosition.x=Mathf.Clamp(caretPosition.x,caretViewport.rect.xMin+3,caretViewport.rect.xMax-3);
        caret.position=caretViewport.TransformPoint(caretPosition);
        caret.sizeDelta=new Vector2(Mathf.Max(3,2/Mathf.Max(.01f,Mathf.Abs(caret.lossyScale.x))),Mathf.Max(24,top-bottom));
        caret.SetAsLastSibling();
    }
    public void OnPointerDown(PointerEventData pointer)
    {
        if(nicknameInput!=null&&nicknameInput.isFocused&&!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)nicknameInput.transform,pointer.position,pointer.pressEventCamera))
        {nicknameInput.DeactivateInputField();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
    }
    private void Update()
    {
        if(nicknameInput==null||!nicknameInput.isFocused)return;
        var keyboard=nicknameInput.touchScreenKeyboard;
        bool visible=HotSeatOrientationLock.KeyboardFraction>.1f || (keyboard!=null && keyboard.status==TouchScreenKeyboard.Status.Visible);
        if(visible)keyboardWasVisible=true;
        if((keyboardWasVisible&&!visible)||(keyboard!=null&&(keyboard.status==TouchScreenKeyboard.Status.Done||keyboard.status==TouchScreenKeyboard.Status.Canceled||keyboard.status==TouchScreenKeyboard.Status.LostFocus)))
        {
            nicknameInput.DeactivateInputField();
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        }
    }
    public static void Show(Canvas canvas)
    {
        SpinRewardUI.Close(canvas);
        if (canvas.rootCanvas.transform.Find("PlayerProfileOverlay") != null) return;
        GameObject obj = new GameObject("PlayerProfileOverlay",typeof(RectTransform),typeof(Image),typeof(Canvas),typeof(GraphicRaycaster),typeof(PlayerProfileUI));
        obj.transform.SetParent(canvas.rootCanvas.transform,false);
        RectTransform rect = obj.transform as RectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        PortraitMenuTopBar.ApplyOverlayInset(rect);
        obj.GetComponent<Image>().color = new Color(.01f,.018f,.012f,1f);
        var group=obj.AddComponent<CanvasGroup>();group.alpha=1;group.ignoreParentGroups=true;
        Canvas modal = obj.GetComponent<Canvas>(); modal.overrideSorting = true; modal.sortingOrder = 680;
        obj.GetComponent<PlayerProfileUI>().Build();
    }
    private static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
    {
        RectTransform rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin = rect.anchorMax = new Vector2(0f,1f); rect.pivot = new Vector2(0f,1f);
        rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height); return rect;
    }
    private static TMP_Text Text(Transform parent,string value,float x,float y,float width,float height,float size = 32)
    {
        TMP_Text text = Rect("Label",parent,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value; text.richText = false; text.fontSize = size;
        text.color = new Color(1f,.91f,.7f); text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft; return text;
    }
    private static Button Button(Transform parent,string value,float x,float y,float width,float height,Action action,bool enabled = true)
    {
        RectTransform rect = Rect("ProfileAction",parent,x,y,width,height);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        TMP_Text text = Text(rect,value,0,0,width,height,30); text.name="ResponsiveLabel";text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=30;
        button.interactable = enabled; button.onClick.AddListener(()=>action()); PokerButtonTheme.ApplyTo(button); return button;
    }
    private void Build()
    {
        foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        root = transform as RectTransform;
        Canvas.ForceUpdateCanvases();
        float sx = root.rect.width / Mathf.Max(1,Screen.width), sy = root.rect.height / Mathf.Max(1,Screen.height);
        float left = Screen.safeArea.xMin*sx+24f, top = 24f;
        float width = Screen.safeArea.width*sx-48f;
        var data = PlayerProfileService.Data;
        ProgressionRules.RefreshPeriods(data,DateTime.UtcNow);
        AvatarDatabase headerAvatars = Resources.Load<AvatarDatabase>("ProfileAvatars");
        if (headerAvatars != null && headerAvatars.avatars != null && headerAvatars.avatars.Length > 0)
        {
            int selected = Mathf.Clamp(PlayerProfileService.AvatarIndex,0,headerAvatars.avatars.Length-1);
            Image currentAvatar = Rect("CurrentAvatar",root,left,top+88,148,148).gameObject.AddComponent<Image>();
            currentAvatar.sprite = headerAvatars.avatars[selected]; currentAvatar.preserveAspect = true;
            AvatarCircleUtility.Apply(currentAvatar);
            currentAvatar.raycastTarget = false;
        }
        TMP_Text heading=Text(root,"MÓJ PROFIL",left+104,top,width-208,72,42);
        heading.alignment=TextAlignmentOptions.Center;
        heading.textWrappingMode=TextWrappingModes.NoWrap;
        heading.enableAutoSizing=true;heading.fontSizeMin=28;heading.fontSizeMax=42;
        RectTransform closeRect=Rect("UtilityProfileClose",root,left+width-96,top-4,96,96);
        Image closeHit=closeRect.gameObject.AddComponent<Image>();closeHit.color=Color.clear;
        Button close=closeRect.gameObject.AddComponent<Button>();close.targetGraphic=closeHit;
        close.transition=Selectable.Transition.None;
        close.onClick.AddListener(()=>Destroy(gameObject));
        // Two strokes make a true icon; the invisible hit target stays comfortably large.
        foreach(float angle in new[]{45f,-45f})
        {
            RectTransform stroke=Rect("CloseStroke",closeRect,48,48,36,3);
            stroke.pivot=new Vector2(.5f,.5f);stroke.localRotation=Quaternion.Euler(0,0,angle);
            Image ink=stroke.gameObject.AddComponent<Image>();ink.color=new Color(1,1,1,.85f);ink.raycastTarget=false;
        }
        TMP_Text signature=Text(root,"© Eryk Potocki",left+(width-320f)*.5f,root.rect.height-28,320,22,16);
        signature.alignment=TextAlignmentOptions.Bottom;signature.color=new Color(1f,.86f,.62f,.36f);
        signature.rectTransform.anchorMin=signature.rectTransform.anchorMax=new Vector2(.5f,0);
        signature.rectTransform.pivot=new Vector2(.5f,0);signature.rectTransform.anchoredPosition=new Vector2(0,1);
        float inputWidth=Mathf.Min(540,width-172);
        RectTransform inputRect = Rect("ProfileNickname",root,left+172,top+88,inputWidth,66);
        Image inputHit=inputRect.gameObject.AddComponent<Image>();inputHit.color=Color.clear;
        TMP_InputField input = inputRect.gameObject.AddComponent<TMP_InputField>();
        nicknameInput=input;
        // Render our own caret: TMP's native mobile caret is not reliable with
        // the hidden system input. Its position still comes from TMP editing.
        input.customCaretColor=true;input.caretColor=Color.clear;input.caretWidth=3;input.caretBlinkRate=.85f;
        input.targetGraphic=inputHit;input.transition=Selectable.Transition.None;
        RectTransform viewport=Rect("NicknameViewport",inputRect,4,0,inputWidth-12,60);
        viewport.gameObject.AddComponent<RectMask2D>();
        TMP_Text label = Text(viewport,"",0,0,inputWidth-12,60,48);
        label.textWrappingMode=TextWrappingModes.NoWrap;
        input.textViewport = viewport; input.textComponent = label;
        nicknameCaret=Rect("NicknameCaret",viewport,0,0,3,36).gameObject.AddComponent<Image>();
        nicknameCaret.color=Color.white;nicknameCaret.raycastTarget=false;nicknameCaret.enabled=false;
        nicknameCaret.rectTransform.pivot=new Vector2(.5f,.5f);
        Image underline=Rect("NicknameUnderline",inputRect,4,64,inputWidth-12,1.5f).gameObject.AddComponent<Image>();
        underline.color=new Color(1f,.91f,.7f,.25f);underline.raycastTarget=false;
        input.text = data.Profile.Nickname; input.characterLimit = 20;
        input.onFocusSelectAll = false; input.resetOnDeActivation = false;
        input.richText = false; input.shouldHideSoftKeyboard = false;
        input.shouldHideMobileInput=true;
        input.lineType = TMP_InputField.LineType.SingleLine;
        TMP_Text nicknameStatus=Text(root,"Dotknij nicku, aby go zmienić",left+172,top+158,width-172,28,20);
        nicknameStatus.color=new Color(1f,.91f,.7f,.45f);
        input.onSelect.AddListener(_=>{keyboardWasVisible=false;underline.color=new Color(1f,.82f,.32f,.8f);nicknameStatus.text="";focusNicknameAtEnd=true;caretChangedAt=Time.unscaledTime;});
        input.onValueChanged.AddListener(value=>{
            // Save valid edits without rewriting the field, caret or an incomplete draft.
            bool saved=PlayerProfileService.SetNickname(value);
            nicknameStatus.text=saved?"":"Nick musi mieć od 2 do 20 znaków";
        });
        input.onEndEdit.AddListener(value=>{
            if(!PlayerProfileService.SetNickname(value))input.SetTextWithoutNotify(PlayerProfileService.Data.Profile.Nickname);
            nicknameStatus.text="";keyboardWasVisible=false;
            underline.color=new Color(1f,.91f,.7f,.25f);
        });

        RectTransform view = Rect("ProfileScroll",root,left,top+254,width,Mathf.Max(180,root.rect.height-top-290));
        view.gameObject.AddComponent<Image>().color = Color.clear;
        view.gameObject.AddComponent<RectMask2D>();
        ProfileSwipeScroll scroll = view.gameObject.AddComponent<ProfileSwipeScroll>();
        scroll.Navigate=direction=>{
            int page=section=="avatar"?avatarCategory:section=="frame"?6:7;
            int next=Mathf.Clamp(page+direction,0,7);
            if(next==page)return;
            section=next<6?"avatar":next==6?"frame":"back";
            if(next<6)avatarCategory=next;
            Build();
        };
        body = Rect("Content",view,0,0,width,1200); scroll.viewport = view; scroll.content = body;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 65;
        float y = 0f;
        if (!string.IsNullOrEmpty(message)) { Text(body,message,8,y,width-16,72); y += 80; }
        Text(body,$"Ukończone gry: {data.Statistics.GamesPlayed}   Wygrane: {data.Statistics.GamesWon}  ({(data.Statistics.GamesPlayed>0?100f*data.Statistics.GamesWon/data.Statistics.GamesPlayed:0):0}%)\nWygrane rundy: {data.Statistics.RoundsWon}   Eliminacje: {data.Statistics.Eliminations}",8,y,width-16,110,28); y += 122;
        string[] tabIds={"avatar","frame","back"}; string[] tabNames={"AVATARY","RAMKI","REWERSY"};
        float tabWidth=(width-20f)/3f;
        for(int tab=0;tab<3;tab++)
        {
            string id=tabIds[tab];
            Button(body,tabNames[tab],8+tab*tabWidth,y,tabWidth-6,64,()=>{section=id;Build();},section!=id);
        }
        y+=78;
        if(section=="avatar")
        {
        string[] categories={"ODBLOKOWANE","DARMOWE","ZWIERZĘTA","HALLOWEEN","WAKACYJNE","LUDZIE"};
        var categoryView=Rect("CategoryViewport",body,8,y,width-16,60);
        categoryView.gameObject.AddComponent<Image>().color=Color.clear;
        categoryView.gameObject.AddComponent<RectMask2D>();
        var categoryContent=Rect("Categories",categoryView,0,0,1200,60);
        var categoryScroll=categoryView.gameObject.AddComponent<ScrollRect>();
        categoryScroll.viewport=categoryView;categoryScroll.content=categoryContent;
        categoryScroll.horizontal=true;categoryScroll.vertical=false;categoryScroll.movementType=ScrollRect.MovementType.Clamped;
        float categoryX=0,selectedX=0;
        for(int c=0;c<categories.Length;c++)
        {
            int selected=c;
            float categoryWidth=categories[c].Length*19f+30;
            var tabRect=Rect("Category",categoryContent,categoryX,0,categoryWidth,60);
            var hit=tabRect.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var categoryButton=tabRect.gameObject.AddComponent<Button>();categoryButton.targetGraphic=hit;categoryButton.transition=Selectable.Transition.None;
            categoryButton.onClick.AddListener(()=>{avatarCategory=selected;Build();});
            var caption=Text(tabRect,categories[c],0,0,categoryWidth,56,27);
            caption.alignment=TextAlignmentOptions.Center;
            caption.color=c==avatarCategory?new Color(1,.82f,.32f):new Color(.65f,.65f,.60f);
            if(c==avatarCategory){selectedX=categoryX;Rect("Underline",tabRect,10,54,categoryWidth-20,3).gameObject.AddComponent<Image>().color=new Color(1,.82f,.32f);}
            categoryX+=categoryWidth+14;
        }
        categoryContent.sizeDelta=new Vector2(categoryX,60);
        categoryContent.anchoredPosition=new Vector2(-Mathf.Clamp(selectedX-(width-16)*.3f,0,Mathf.Max(0,categoryX-width+16)),0);
        y+=80;
        AvatarDatabase avatars = Resources.Load<AvatarDatabase>("ProfileAvatars");
        if (avatars != null && avatars.avatars != null)
        {
            const int columns = 4;
            float cell = (width-16f)/columns;
            float icon = cell-14f;
            var ordered = new System.Collections.Generic.List<int>();
            for (int i=0;i<avatars.avatars.Length;i++)
                if(avatars.avatars[i]!=null && data.Inventory.OwnedAvatars.Contains(PlayerProfileService.AvatarId(i,avatars.avatars[i])) &&
                    (avatarCategory==0||AvatarCategories.Category(i,avatars.avatars[i].name)==avatarCategory-1))ordered.Add(i);
            ordered.Sort((a,b)=> {
                bool first=data.Inventory.OwnedAvatars.Contains(PlayerProfileService.AvatarId(a,avatars.avatars[a]));
                bool second=data.Inventory.OwnedAvatars.Contains(PlayerProfileService.AvatarId(b,avatars.avatars[b]));
                return first==second?a.CompareTo(b):first?-1:1;
            });
            for (int position=0;position<ordered.Count;position++)
            {
                int i=ordered[position]; string id=PlayerProfileService.AvatarId(i,avatars.avatars[i]);
                float x=8f+(position%columns)*cell+(cell-icon)*.5f;
                float rowY=y+(position/columns)*(icon+18f);
                AvatarTile(id,avatars.avatars[i],x,rowY,icon,data.Profile.SelectedAvatarId==id);
            }
            y += Mathf.Ceil(ordered.Count/(float)columns)*(icon+18f)+12f;
            if(ordered.Count==0){Text(body,"Nie masz jeszcze avatarów w tej kategorii.\nNowe znajdziesz w sklepie.",8,y,width-16,110,28);y+=122;}
        }
        }
        if(section=="frame")
        {
        Text(body,"RAMKI",8,y,width-16,64,38); y += 70;
        const int frameColumns=4;
        float frameCell=(width-16)/frameColumns;
        float frameSize=frameCell-14;
        float frameLeft=8+(frameCell-frameSize)*.5f;
        float frameRowHeight=frameSize+54;
        FrameTile("none",null,frameLeft,y,frameSize,true,data.Profile.SelectedFrameId=="none");
        int framePosition=1;
        if(data.Inventory.OwnedFrames.Contains("classic_wood"))
        {
            FrameTile("classic_wood",LevelFrameCatalog.Resolve("classic_wood"),frameLeft+frameCell,y,frameSize,true,data.Profile.SelectedFrameId=="classic_wood");
            framePosition++;
        }
        int[] frameLevels=LevelFrameCatalog.Levels;
        for(int i=0;i<frameLevels.Length;i++)
        {
            string id="level:"+frameLevels[i];
            if(!data.Inventory.OwnedFrames.Contains(id))continue;
            int position=framePosition++;
            FrameTile(id,LevelFrameCatalog.Resolve(id),frameLeft+(position%frameColumns)*frameCell,
                y+(position/frameColumns)*frameRowHeight,frameSize,data.Inventory.OwnedFrames.Contains(id),data.Profile.SelectedFrameId==id);
            Text(body,"LVL "+frameLevels[i],frameLeft+(position%frameColumns)*frameCell,y+(position/frameColumns)*frameRowHeight+frameSize+4,frameSize,34,26).alignment=TextAlignmentOptions.Center;
        }
        y+=Mathf.Ceil(framePosition/(float)frameColumns)*frameRowHeight;
        }
        if(section=="back")
        {
        Text(body,"REWERSY",8,y,width-16,64,38); y += 70;
        Button(body,"ONLINE",8,y,(width-24)/2,60,()=>{offlineBacks=false;Build();},offlineBacks);
        Button(body,"JEDEN TELEFON",width/2,y,(width-24)/2,60,()=>{offlineBacks=true;Build();},!offlineBacks);y+=78;
        CardBackDatabase backs = gameObject.GetComponent<CardBackDatabase>();
        if (backs == null) backs = gameObject.AddComponent<CardBackDatabase>();
        backs.Online=!offlineBacks;
        const int backColumns=3;
        float backCell=(width-16)/backColumns;
        float backWidth=backCell-24, backHeight=backWidth*1.5f;
        int backPosition=0;
        for (int i=0;i<backs.BackCount;i++)
        {
            Sprite sprite = backs.GetBackSprite(i); string id = sprite.texture.name;
            if(!offlineBacks&&!data.Inventory.OwnedCardBacks.Contains(id))continue;
            int position=backPosition++;
            BackTile(id,sprite,8+(position%backColumns)*backCell,y+(position/backColumns)*(backHeight+24),backWidth,backHeight,(offlineBacks?data.Profile.SelectedOfflineCardBackId:data.Profile.SelectedCardBackId)==id);
        }
        y+=Mathf.Ceil(backPosition/(float)backColumns)*(backHeight+24);
        }
        body.sizeDelta = new Vector2(width,y);
    }
    private void Achievement(string title,int value,int target,ref float y,float width)
    { Text(body,title+"   "+Mathf.Min(value,target)+" / "+target,8,y,width-16,76,28); y+=84; }
    private void ItemRow(string category,string id,string title,Sprite sprite,bool owned,bool selected,ref float y,float width)
    {
        if (sprite != null)
        {
            Image image = Rect("Cosmetic",body,8,y+8,100,100).gameObject.AddComponent<Image>();
            image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false;
        }
        Text(body,title,126,y+4,width-398,110,28);
        Button(body,selected?"WYBRANO":owned?"ZAŁÓŻ":"ZABLOKOWANE",width-260,y+20,250,76,()=>{ PlayerProfileService.Equip(category,id); Build(); },owned&&!selected);
        y+=126;
    }

    private void AvatarTile(string id,Sprite sprite,float x,float y,float size,bool selected)
    {
        RectTransform tile=Rect("AvatarTile",body,x,y,size,size);
        Image border=tile.gameObject.AddComponent<Image>();
        border.color=new Color(.07f,.055f,.035f,.65f);
        TileOutline(tile,size,size,selected);
        Button button=tile.gameObject.AddComponent<Button>();button.targetGraphic=border;button.transition=Selectable.Transition.ColorTint;
        RectTransform imageRect=Rect("Avatar",tile,5,5,size-10,size-10);
        Image image=imageRect.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
        AvatarCircleUtility.Apply(image);
        bool owned=PlayerProfileService.Data.Inventory.OwnedAvatars.Contains(id);
        button.interactable=true;
        if(!owned)
        {
            image.enabled=false;
            RectTransform icon=Rect("ModeLock",tile,size*.5f,size*.5f,36,46);
            icon.pivot=new Vector2(.5f,.5f);
            icon.localScale=Vector3.one*(size/80f);
            GameModeSelectUI.DrawLock(icon);
        }
        button.onClick.AddListener(()=>{PlayerProfileService.Equip("avatar",id);Build();});
    }

    private void FrameTile(string id,Sprite sprite,float x,float y,float size,bool owned,bool selected)
    {
        RectTransform tile=Rect("FrameTile",body,x,y,size,size);
        Image border=tile.gameObject.AddComponent<Image>();
        border.color=new Color(.07f,.055f,.035f,.65f);
        TileOutline(tile,size,size,selected);
        Button button=tile.gameObject.AddComponent<Button>();button.targetGraphic=border;button.interactable=true;
        if(sprite!=null)
        {
            Image image=Rect("Frame",tile,6,6,size-12,size-12).gameObject.AddComponent<Image>();
            image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
            image.color=owned?Color.white:new Color(.3f,.3f,.3f,.6f);
        }
        else
        {
            TMP_Text empty=Text(tile,"BEZ\nRAMKI",8,8,size-16,size-16,24);empty.alignment=TextAlignmentOptions.Center;
        }
        if(!owned)
        {
            Image shade=Rect("LockedShade",tile,3,3,size-6,size-6).gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.46f);shade.raycastTarget=false;
            RectTransform icon=Rect("ModeLock",tile,size*.5f,size*.5f,36,46);
            icon.pivot=new Vector2(.5f,.5f);
            icon.localScale=Vector3.one*(size/80f);
            GameModeSelectUI.DrawLock(icon);
        }
        button.onClick.AddListener(()=>{PlayerProfileService.Equip("frame",id);Build();});
    }
    private void BackTile(string id,Sprite sprite,float x,float y,float width,float height,bool selected)
    {
        RectTransform tile=Rect("UtilityBackTile",body,x,y,width,height);
        Image hit=tile.gameObject.AddComponent<Image>();hit.color=new Color(.07f,.055f,.035f,.65f);
        Button button=tile.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
        Image art=Rect("BackArtwork",tile,7,7,width-14,height-14).gameObject.AddComponent<Image>();
        // The database removes each source's margins. Normalize the remaining art
        // to the same portrait card footprint instead of letterboxing each differently.
        art.sprite=sprite;art.type=Image.Type.Simple;art.preserveAspect=false;art.raycastTarget=false;
        TileOutline(tile,width,height,selected);
        bool owned=offlineBacks||PlayerProfileService.Data.Inventory.OwnedCardBacks.Contains(id);
        button.interactable=true;
        if(!owned)
        {
            art.color=new Color(.45f,.45f,.45f);
            var lockRect=Rect("ModeLock",tile,width/2,height/2,36,46);lockRect.pivot=new Vector2(.5f,.5f);
            lockRect.localScale=Vector3.one*(width/90);GameModeSelectUI.DrawLock(lockRect);
        }
        button.onClick.AddListener(()=>{PlayerProfileService.Equip(offlineBacks?"offlineBack":"back",id);Build();});
    }
    private static void TileOutline(RectTransform tile,float width,float height,bool selected)
    {
        if(!selected)return;
        float line=3;
        Color tint=selected?new Color(1f,.76f,.25f):new Color(.35f,.27f,.13f,.6f);
        RectTransform[] edges={Rect("TopEdge",tile,0,0,width,line),Rect("BottomEdge",tile,0,height-line,width,line),
            Rect("LeftEdge",tile,0,0,line,height),Rect("RightEdge",tile,width-line,0,line,height)};
        foreach(var edge in edges){var ink=edge.gameObject.AddComponent<Image>();ink.color=tint;ink.raycastTarget=false;}
    }
}
