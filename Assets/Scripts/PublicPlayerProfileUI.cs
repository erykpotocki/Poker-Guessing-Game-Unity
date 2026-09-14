using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PublicPlayerProfileUI : MonoBehaviour
{
    private RectTransform anchor,card,root;
    public static void Show(Canvas canvas,Sprite avatar,string nickname,int games,int wins,string profileId,int actor=0,RectTransform anchor=null)
    {
        if(canvas==null)return;
        var old=canvas.rootCanvas.transform.Find("PublicPlayerProfile");if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
        var root=ShopUI.Overlay(canvas,"PublicPlayerProfile");root.offsetMin=root.offsetMax=Vector2.zero;
        // The transparent outside area dismisses the card without hiding the game.
        root.GetComponent<Image>().color=Color.clear;
        root.gameObject.AddComponent<Button>().onClick.AddListener(()=>Destroy(root.gameObject));
        bool local=PhotonNetwork.LocalPlayer!=null&&actor==PhotonNetwork.LocalPlayer.ActorNumber;
        float w=600,h=local?320:240;
        var card=ShopUI.Rect("UtilityPublicProfileCard",root,0,0,w,h);
        card.gameObject.AddComponent<Image>().color=new Color(.025f,.065f,.05f,.97f);
        card.gameObject.AddComponent<Button>().transition=Selectable.Transition.None;
        var border=card.gameObject.AddComponent<Outline>();border.effectColor=new Color(.65f,.49f,.22f,.8f);border.effectDistance=new Vector2(1,-1);
        var portrait=ShopUI.Rect("Avatar",card,18,18,78,78).gameObject.AddComponent<Image>();portrait.sprite=avatar;AvatarCircleUtility.Apply(portrait);
        var name=ShopUI.Text(card,nickname,112,18,w-185,38,28);name.richText=false;name.alignment=TextAlignmentOptions.Left;
        ShopUI.Button(card,"×",w-58,12,44,44,()=>Destroy(root.gameObject));
        int Stat(string key,int fallback=0)
        {
            var player=PhotonNetwork.CurrentRoom?.GetPlayer(actor);
            return player!=null&&player.CustomProperties.TryGetValue(key,out object value)&&value is int i?i:fallback;
        }
        string Value(string key,string fallback)
        {
            var player=PhotonNetwork.CurrentRoom?.GetPlayer(actor);
            return player!=null&&player.CustomProperties.TryGetValue(key,out object value)?value?.ToString()??fallback:fallback;
        }
        games=Stat(PhotonAvatarSync.GamesPlayedKey,games);wins=Stat(PhotonAvatarSync.GamesWonKey,wins);
        ShopUI.Text(card,"LVL "+Stat("levelV1",1)+"  ·  Dołączył: "+Value("joinedV1","—"),112,60,w-130,30,20).alignment=TextAlignmentOptions.Left;
        string[] rows={"Gry: "+games+"     Wygrane: "+wins+" ("+(games>0?100f*wins/games:0).ToString("0.0")+"%)",
            "Wygrane rundy: "+Stat("roundWinsV1")+"     Eliminacje: "+Stat("eliminationsV1"),
            "Kolekcja: "+Stat("avatarsV1")+" avatarów · "+Stat("framesV1")+" ramek · "+Stat("backsV1")+" rewersów"};
        for(int i=0;i<rows.Length;i++)ShopUI.Text(card,rows[i],22,112+i*38,w-44,32,23).alignment=TextAlignmentOptions.Left;
        if(local)for(int i=0;i<6;i++)
        {
            int face=i;var button=ShopUI.Button(card,"",22+i*94,250,84,54,()=>{PlayerReactions.Send(face);Destroy(root.gameObject);});
            var image=ShopUI.Rect("Face",button.transform,20,3,48,48).gameObject.AddComponent<Image>();image.sprite=PlayerReactions.Face(i);image.raycastTarget=false;
        }
        var ui=root.gameObject.AddComponent<PublicPlayerProfileUI>();ui.anchor=anchor;ui.root=root;ui.card=card;ui.LateUpdate();
    }
    private void LateUpdate()
    {
        if(card==null)return;
        float scale=Mathf.Min(1,Mathf.Min(root.rect.width*.43f/card.sizeDelta.x,(root.rect.height-32)/card.sizeDelta.y));
        card.localScale=Vector3.one*scale;
        var canvas=root.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector2 point=Vector2.zero;float halfAvatar=0;
        if(anchor!=null)
        {
            var corners=new Vector3[4];anchor.GetWorldCorners(corners);
            point=root.InverseTransformPoint(anchor.TransformPoint(anchor.rect.center));
            halfAvatar=Mathf.Abs(root.InverseTransformPoint(corners[2]).x-root.InverseTransformPoint(corners[0]).x)/2;
        }
        float width=card.sizeDelta.x*scale,height=card.sizeDelta.y*scale;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Screen.safeArea.min,camera,out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Screen.safeArea.max,camera,out var max);
        float x=point.x<root.rect.center.x?point.x+halfAvatar+12:point.x-halfAvatar-12-width;
        float y=point.y+height/2;
        card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.pivot=new Vector2(0,1);
        card.anchoredPosition=new Vector2(Mathf.Clamp(x,min.x+12,Mathf.Max(min.x+12,max.x-width-12)),Mathf.Clamp(y,min.y+height+12,Mathf.Max(min.y+height+12,max.y-12)));
    }
}
