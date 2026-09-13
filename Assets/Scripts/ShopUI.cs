using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopUI : MonoBehaviour
{
    private string category="avatar";
    private int currency;
    public static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    public static TMP_Text Text(Transform parent,string caption,float x,float y,float w,float h,float size=28)
    {
        var t=Rect("Label",parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.text=caption;t.fontSize=size;
        t.color=new Color(1,.9f,.7f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=size*.7f;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
    public static Button Button(Transform parent,string caption,float x,float y,float w,float h,Action action)
    {
        var r=Rect("Action",parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;
        Text(r,caption,4,0,w-8,h,25).name="ResponsiveLabel";b.onClick.AddListener(()=>action());PokerButtonTheme.ApplyTo(b);return b;
    }
    private static Button ShopTab(Transform parent,string caption,float x,float y,float w,float h,bool selected,Action action,bool secondary=false)
    {
        var r=Rect(secondary?"UtilityShopFilter":"UtilityShopTab",parent,x,y,w,h);
        var background=r.gameObject.AddComponent<Image>();
        background.color=selected?(secondary?new Color(.16f,.20f,.13f):new Color(.12f,.25f,.19f)):new Color(.035f,.07f,.06f);
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=background;button.transition=Selectable.Transition.ColorTint;
        button.onClick.AddListener(()=>action());
        var label=Text(r,caption,6,0,w-12,h,secondary?22:26);label.color=selected?new Color(1f,.8f,.28f):new Color(.78f,.78f,.7f);
        if(selected)
        {
            var line=Rect("Selection",r,secondary?18:10,h-4,w-(secondary?36:20),4).gameObject.AddComponent<Image>();
            line.color=new Color(1f,.72f,.18f);line.raycastTarget=false;
        }
        return button;
    }
    public static void Show(Canvas canvas)
    {
        if(canvas==null)return;
        var old=canvas.rootCanvas.transform.Find("ShopOverlay");if(old!=null)Destroy(old.gameObject);
        var root=Overlay(canvas,"ShopOverlay");root.gameObject.AddComponent<ShopUI>().Build();
    }
    public static RectTransform Overlay(Canvas canvas,string name)
    {
        var r=Rect(name,canvas.rootCanvas.transform,0,0,0,0);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        r.gameObject.AddComponent<Image>().color=new Color(.015f,.025f,.022f,1f);
        var layer=r.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=650;r.gameObject.AddComponent<GraphicRaycaster>();
        PortraitMenuTopBar.ApplyOverlayInset(r);Canvas.ForceUpdateCanvases();return r;
    }
    private void Build()
    {
        foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var root=(RectTransform)transform;float w=root.rect.width,h=root.rect.height;
        Text(root,"SKLEP",20,20,w-260,70,44);Button(root,"KODY",w-220,24,104,58,()=>ProfileTestTools.ShowPromoCode(root.GetComponentInParent<Canvas>()));Button(root,"×",w-100,20,76,66,()=>Destroy(gameObject));
        Text(root,$"Złoto: {PlayerProfileService.Data.Wallet.Coins}    Diamenty: {PlayerProfileService.Data.Wallet.RewardCurrency}",20,94,w-40,56,30);
        string[] ids={"avatar","back","frame"},labels={"AVATARY","REWERSY","RAMKI"};
        for(int i=0;i<3;i++){string id=ids[i];ShopTab(root,labels[i],20+i*(w-40)/3,164,(w-46)/3,60,category==id,()=>{category=id;Build();});}
        float sx=w/Mathf.Max(1,Screen.width);
        float left=Screen.safeArea.xMin*sx+24;
        float width=Screen.safeArea.width*sx-48;
        string[] filters={"Wszystko","Złoto","Diamenty"};
        for(int i=0;i<3;i++){int n=i;ShopTab(root,filters[i],left+i*width/3,242,width/3-6,52,currency==i,()=>{currency=n;Build();},true);}
        var view=Rect("Products",root,left,320,width,Mathf.Max(100,h-346));view.gameObject.AddComponent<Image>().color=Color.clear;view.gameObject.AddComponent<RectMask2D>();
        var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        int columns=category=="back"?3:4;
        float cell=(width-16)/columns,artWidth=cell-(category=="back"?24:14),artHeight=category=="back"?artWidth*1.5f:artWidth;
        float rowHeight=artHeight+134;
        var items=CosmeticCatalog.All(category).FindAll(o=>!CosmeticCatalog.Owned(category,o.Id)&&(currency==0 || (currency==1?o.Gold>0:o.Diamonds>0)));
        var content=Rect("Content",view,0,0,width,Mathf.Max(120,Mathf.Ceil(items.Count/(float)columns)*rowHeight));scroll.viewport=view;scroll.content=content;
        for(int i=0;i<items.Count;i++)
        {
            var offer=items[i];bool allowed=category!="frame"||CosmeticCatalog.CanUnlockFrame(PlayerProfileService.Data,offer.Id);
            float x=8+i%columns*cell+(cell-artWidth)*.5f,y=i/columns*rowHeight;
            var tile=Rect("UtilityShopProduct",content,x,y,artWidth,artHeight+118);
            var background=tile.gameObject.AddComponent<Image>();background.color=new Color(.07f,.055f,.035f,.85f);
            var art=Rect("Preview",tile,5,5,artWidth-10,artHeight-10).gameObject.AddComponent<Image>();art.sprite=offer.Sprite;art.preserveAspect=category!="back";art.enabled=offer.Sprite!=null;art.raycastTarget=false;
            if(offer.Sprite!=null&&category=="avatar")AvatarCircleUtility.Apply(art);
            if(offer.Sprite==null)Text(tile,"Brak podglądu",6,8,artWidth-12,artHeight-16,24);
            if(!allowed)
            {
                art.color=new Color(.15f,.15f,.15f,.72f);
                var shade=Rect("LockedShade",tile,5,5,artWidth-10,artHeight-10).gameObject.AddComponent<Image>();shade.color=new Color(.12f,.12f,.12f,.72f);shade.raycastTarget=false;
                var lockRect=Rect("ModeLock",tile,artWidth*.18f,artHeight*.12f,artWidth*.64f,artHeight*.76f);GameModeSelectUI.DrawLock(lockRect);
                Text(tile,"ZABLOKOWANA",4,artHeight+28,artWidth-8,52,22).color=new Color(.65f,.65f,.62f);
            }
            else if(offer.Purchasable)
            {
                // Diamonds are consistently shown above gold when both are required.
                if(offer.Diamonds>0)CurrencyAmount(tile,offer.Diamonds,true,8,artHeight+8,artWidth-16,38);
                if(offer.Gold>0)CurrencyAmount(tile,offer.Gold,false,8,artHeight+(offer.Diamonds>0?48:8),artWidth-16,38);
                if(offer.Category=="frame")Text(tile,offer.Title.Replace("Ramka · ",""),4,artHeight+84,artWidth-8,30,20);
                else if(offer.Gold>0&&offer.Diamonds>0)Text(tile,offer.Both?"Obie waluty":"Złoto lub diamenty",4,artHeight+84,artWidth-8,30,18);
            }
            else Text(tile,Requirement(offer),4,artHeight+8,artWidth-8,104,24);
            var button=tile.gameObject.AddComponent<Button>();button.targetGraphic=background;button.transition=Selectable.Transition.ColorTint;
            button.onClick.AddListener(()=>Preview(GetComponent<Canvas>(),offer.Category,offer.Id,Build));
        }
        if(items.Count==0)Text(content,"Brak zablokowanych przedmiotów w tym filtrze.\nOdblokowane rzeczy znajdziesz w profilu.",8,20,width-16,120,30);
    }
    private static string Requirement(CosmeticCatalog.Offer o)
    {
        if(o.Category=="frame")return o.Id=="classic_wood"?"Misja: pierwsza gra":o.Title.Replace("Ramka · ","")+"\nlub wcześniej: "+Price(o);
        return Price(o)+(o.Spin&&o.Purchasable?"\nTakże ze spina":"");
    }
    private static string Price(CosmeticCatalog.Offer o)=>!o.Purchasable?(o.Id=="6"?"Darmowy":o.Category=="frame"?"Nagroda za misję":o.Spin?"Tylko ze spina":"Niedostępne"):o.Gold>0&&o.Diamonds>0?$"{o.Gold} złota\n{(o.Both?"+":"lub")} {o.Diamonds} diamentów":o.Gold>0?$"{o.Gold} złota":$"{o.Diamonds} diamentów";
    private static void CurrencyAmount(Transform parent,int amount,bool gems,float x,float y,float width,float height)
    {
        float iconSize=height*.72f;
        var icon=Rect("UtilityCurrencyIcon",parent,x,y+(height-iconSize)/2,iconSize,iconSize).gameObject.AddComponent<Image>();
        icon.sprite=Resources.Load<Sprite>(gems?"WalletIcons/diament":"WalletIcons/złoto");icon.preserveAspect=true;icon.raycastTarget=false;
        float groupWidth=Mathf.Min(width,iconSize+8+Mathf.Max(50,amount.ToString("N0").Length*18));
        x+=(width-groupWidth)*.5f;
        icon.rectTransform.anchoredPosition=new Vector2(x,-(y+(height-iconSize)/2));
        var label=Text(parent,amount.ToString("N0"),x+iconSize+8,y,groupWidth-iconSize-8,height,height*.62f);
        label.alignment=TextAlignmentOptions.MidlineLeft;label.fontStyle=FontStyles.Bold;
        label.color=gems?new Color(.3f,.78f,1f):new Color(1f,.75f,.2f);
    }
    private static void PriceDisplay(Transform parent,CosmeticCatalog.Offer offer,float x,float y,float width,float height)
    {
        if(offer.Gold>0&&offer.Diamonds>0)
        {
            CurrencyAmount(parent,offer.Diamonds,true,x,y,width,height);
            Text(parent,offer.Both?"+":"LUB",x,y+height,width,32,20);
            CurrencyAmount(parent,offer.Gold,false,x,y+height+30,width,height);
        }
        else CurrencyAmount(parent,offer.Gold>0?offer.Gold:offer.Diamonds,offer.Gold==0,x,y,width,height);
    }
    public static void Preview(Canvas canvas,string category,string id,Action changed=null)
    {
        if(canvas==null)return;
        var existing=canvas.rootCanvas.transform.Find("PurchasePreview");if(existing!=null)Destroy(existing.gameObject);
        var offer=CosmeticCatalog.Get(category,id);var root=Overlay(canvas,"PurchasePreview");root.GetComponent<Canvas>().sortingOrder=710;
        root.GetComponent<Image>().color=new Color(0,0,0,.78f);
        const float w=740,h=990;
        var panel=Rect("UtilityPurchaseCard",root,0,0,w,h);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min((root.rect.width-40)/w,(root.rect.height-40)/h));
        panel.gameObject.AddComponent<Image>().color=new Color(.018f,.055f,.043f);
        var border=panel.gameObject.AddComponent<Outline>();border.effectColor=new Color(.64f,.45f,.15f,.8f);border.effectDistance=new Vector2(2,-2);
        Text(panel,"ODBLOKUJ PRZEDMIOT",54,30,w-170,48,28).alignment=TextAlignmentOptions.Left;
        Button(panel,"×",w-92,24,64,56,()=>Destroy(root.gameObject));
        Text(panel,offer.Title,40,98,w-80,62,38).fontStyle=FontStyles.Bold;
        var stage=Rect("UtilityArtworkStage",panel,160,182,420,380);stage.gameObject.AddComponent<Image>().color=new Color(.04f,.085f,.065f);
        var art=Rect("Artwork",stage,30,20,360,340).gameObject.AddComponent<Image>();art.sprite=offer.Sprite;art.preserveAspect=true;art.enabled=offer.Sprite!=null;art.raycastTarget=false;
        if(category=="avatar"&&offer.Sprite!=null)AvatarCircleUtility.Apply(art);
        string detail=category=="frame"?(id=="classic_wood"?"Nagroda za pierwszą grę":offer.Title.Replace("Ramka · ","Odblokowanie za ")+" lub wcześniejszy zakup"):
            offer.Spin?(offer.Purchasable?"Dostępny także w kole nagród":"Do zdobycia w kole nagród"):"Dodaj do swojej kolekcji";
        Text(panel,detail,50,580,w-100,66,26).color=new Color(.72f,.8f,.74f);
        bool owned=CosmeticCatalog.Owned(category,id),allowed=category!="frame"||CosmeticCatalog.CanUnlockFrame(PlayerProfileService.Data,id);
        if(!allowed)
        {
            art.color=new Color(.16f,.16f,.16f,.7f);
            var shade=Rect("LockedShade",stage,30,20,360,340).gameObject.AddComponent<Image>();shade.color=new Color(.12f,.12f,.12f,.72f);shade.raycastTarget=false;
            var lockRect=Rect("ModeLock",stage,105,55,210,270);GameModeSelectUI.DrawLock(lockRect);
        }
        else if(offer.Purchasable)PriceDisplay(panel,offer,offer.Gold>0&&offer.Diamonds>0?145:245,offer.Gold>0&&offer.Diamonds>0?638:665,offer.Gold>0&&offer.Diamonds>0?450:250,66);
        var status=Text(panel,"",44,852,w-88,64,25);
        Action<bool> buy=gems=>{if(PlayerProfileService.BuyCosmetic(category,id,gems)){Destroy(root.gameObject);changed?.Invoke();UnlockPresentationUI.ShowPending(canvas);}else{status.text="Za mało środków lub przedmiot już odblokowany.";status.color=new Color(1,.55f,.4f);}};
        if(owned)status.text="Przedmiot jest już w Twoim profilu.";
        else if(!allowed)status.text=id=="classic_wood"?"Odbierz ramkę za pierwszą grę w Misjach.":"Najpierw odblokuj poprzednie ramki.";
        else if(offer.Purchasable)
        {
            if(offer.Both)Button(panel,"KUP PRZEDMIOT",70,760,600,80,()=>buy(true));
            else if(offer.Gold>0&&offer.Diamonds>0)
            {
                Button(panel,"KUP ZA ZŁOTO",45,760,315,80,()=>buy(false));
                Button(panel,"KUP ZA DIAMENTY",380,760,315,80,()=>buy(true));
            }
            else Button(panel,"KUP PRZEDMIOT",70,760,600,80,()=>buy(offer.Diamonds>0));
        }
        else status.text="Tego przedmiotu nie można kupić.";
        Button(panel,"WRÓĆ DO SKLEPU",180,922,380,48,()=>Destroy(root.gameObject));
    }
}
