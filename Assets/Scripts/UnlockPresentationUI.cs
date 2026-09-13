using System.Collections.Generic;
using PokerProfile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnlockPresentationUI : MonoBehaviour
{
    private readonly List<RectTransform> stars=new();
    public static void ShowPending(Canvas owner)
    {
        if(owner==null||PlayerProfileService.PeekUnlock()==null)return;
        if(owner.rootCanvas.transform.Find("UnlockPresentation")!=null)return;
        GameObject obj=new GameObject("UnlockPresentation",typeof(RectTransform),typeof(Image),typeof(Canvas),typeof(GraphicRaycaster),typeof(UnlockPresentationUI));
        obj.transform.SetParent(owner.rootCanvas.transform,false);RectTransform root=obj.transform as RectTransform;
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        obj.GetComponent<Image>().color=new Color(0,0,0,.86f);Canvas c=obj.GetComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=700;
        var fade=obj.AddComponent<CanvasGroup>();fade.alpha=0;fade.interactable=false;
        obj.GetComponent<UnlockPresentationUI>().Build(owner);
    }
    private RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
    {RectTransform r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    private TMP_Text Text(Transform parent,string value,Vector2 pos,Vector2 size,float font)
    {TMP_Text t=Rect("Label",parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.fontSize=font;t.alignment=TextAlignmentOptions.Center;t.color=new Color(1,.82f,.26f);t.raycastTarget=false;return t;}
    private void Build(Canvas owner)
    {
        PendingUnlock item=PlayerProfileService.PeekUnlock();if(item==null){Destroy(gameObject);return;}
        RectTransform panel=Rect("GoldenUnlock",transform,Vector2.zero,new Vector2(700,800));panel.gameObject.AddComponent<Image>().color=new Color(.015f,.045f,.040f,1f);
        bool level=item.Category=="level";
        Text(panel,level?item.Title:"Odblokowano nagrodę",new Vector2(0,level?260:245),new Vector2(620,level?170:70),level?34:28).richText=false;
        if(!level)Text(panel,"Gratulacje!",new Vector2(0,320),new Vector2(620,80),60);
        if(!level)Text(panel,item.Title,new Vector2(0,-165),new Vector2(620,64),26).richText=false;
        Sprite preview=Resolve(item);
        Image image=Rect("UnlockedItem",panel,new Vector2(0,20),new Vector2(280,280)).gameObject.AddComponent<Image>();image.sprite=preview;image.preserveAspect=true;image.raycastTarget=false;
        if(item.Category=="avatar"||level) AvatarCircleUtility.Apply(image);
        for(int i=0;i<12;i++){float a=i*2.39996f;RectTransform star=Rect("Star",panel,new Vector2(Mathf.Cos(a)*UnityEngine.Random.Range(150,235),20+Mathf.Sin(a)*UnityEngine.Random.Range(150,210)),new Vector2(46,46));var sparkle=star.gameObject.AddComponent<RawImage>();sparkle.texture=Resources.Load<Texture2D>("UI/Sparkles/SparkleBurst");sparkle.raycastTarget=false;stars.Add(star);}
        var bounds=((RectTransform)owner.rootCanvas.transform).rect;
        panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(bounds.width/730,bounds.height/840));
        Button accept=Rect("Continue",panel,new Vector2(0,-280),new Vector2(390,82)).gameObject.AddComponent<Button>();Image bg=accept.gameObject.AddComponent<Image>();accept.targetGraphic=bg;
        TMP_Text label=Text(accept.transform,"KONTYNUUJ",Vector2.zero,new Vector2(390,82),31);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        accept.onClick.AddListener(()=>{PlayerProfileService.AcceptUnlock();transform.SetParent(null);Destroy(gameObject);ShowPending(owner);});PokerButtonTheme.ApplyTo(accept);
    }
    private static Sprite Resolve(PendingUnlock item)
    {
        if(item.Category=="level")return Resolve(new PendingUnlock{Category="avatar",ItemId=item.ItemId});
        if(item.Category=="avatar" && item.ItemId.StartsWith("download:")) return CosmeticCatalog.ResolveAvatar(item.ItemId);
        if(item.Category=="frame")return LevelFrameCatalog.Resolve(item.ItemId);
        if(item.Category=="avatar"){AvatarDatabase db=Resources.Load<AvatarDatabase>("ProfileAvatars");if(db!=null&&int.TryParse(item.ItemId.Replace("avatar_",""),out int i)&&db.avatars!=null&&i>=0&&i<db.avatars.Length)return db.avatars[i];}
        if(item.Category=="back")return CardBackDatabase.FindOnline(item.ItemId);
        return null;
    }
    private float animationTime;
    private void Update()
    {
        animationTime+=Time.unscaledDeltaTime;
        var group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        group.alpha=Mathf.SmoothStep(0,1,animationTime/.45f);group.interactable=animationTime>.45f;
        for(int i=0;i<stars.Count;i++)if(stars[i]!=null)
        {
            float t=Mathf.Clamp01((animationTime-.25f-i*.015f)/1.1f);
            stars[i].localScale=Vector3.one*(.5f+.9f*t);stars[i].Rotate(0,0,(i%2==0?35:-25)*Time.unscaledDeltaTime);
            var image=stars[i].GetComponent<RawImage>();image.color=new Color(1,1,1,Mathf.Sin(t*Mathf.PI));
            if(t>=1)stars[i].gameObject.SetActive(false);
        }
    }
}
