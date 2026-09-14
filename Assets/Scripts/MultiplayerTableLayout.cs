using UnityEngine;
using UnityEngine.UI;

public sealed class MultiplayerTableLayout : MonoBehaviour
{
    private RectTransform canvasRoot, board;
    private RectTransform tableOutline;
    private readonly System.Collections.Generic.List<(RectTransform seat,float angle)> seats=new();
    public void AttachSeat(RectTransform seat,float angle)
    {
        seats.Add((seat,angle));PositionSeat(seat,angle);
    }
    private void PositionSeat(RectTransform seat,float angle)
    {
        if(seat==null||tableOutline==null)return;
        if(seat.name=="Seat_Dealer")
        {
            var dealer=seat.GetComponent<SeatUIView>();
            if(dealer!=null){dealer.PositionDealer(tableOutline);return;}
        }
        // Follow the rounded rails of the supplied artwork, not an ellipse.
        Rect bounds=tableOutline.rect;
        float a=bounds.width*.49f,b=bounds.height*.46f;
        if(a<=0||b<=0)return;
        float radians=angle*Mathf.Deg2Rad;
        float x=Mathf.Sign(Mathf.Cos(radians))*Mathf.Pow(Mathf.Abs(Mathf.Cos(radians)),2f/3f);
        float y=Mathf.Sign(Mathf.Sin(radians))*Mathf.Pow(Mathf.Abs(Mathf.Sin(radians)),2f/3f);
        Vector2 normal=new Vector2(Mathf.Sign(x)*x*x/a,Mathf.Sign(y)*y*y/b).normalized;
        Vector2 edge=bounds.center+new Vector2(a*x,b*y+bounds.height*.015f)+normal*12;
        Vector3 desired=tableOutline.TransformPoint(edge);
        var view=seat.GetComponent<SeatUIView>();
        if(view!=null)view.PositionAvatarCenter(desired);else seat.position=desired;
    }
    public RectTransform Initialize(RectTransform center)
    {
        RectTransform root = center.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        foreach(Image image in root.GetComponentsInChildren<Image>(true))
        {
            string resource=image.name=="Table"?"MultiplayerArt/Table1":image.name=="BackgroundGame"?"MultiplayerArt/Room":null;
            if(resource==null)continue;
            Texture2D texture=Resources.Load<Texture2D>(resource);if(texture==null)continue;
            Rect crop=image.name=="Table"?new Rect(0,texture.height*.17f,texture.width,texture.height*.67f):new Rect(0,0,texture.width,texture.height);
            image.sprite=Sprite.Create(texture,crop,new Vector2(.5f,.5f));image.type=Image.Type.Simple;image.color=Color.white;
            image.preserveAspect=false;
        }
        canvasRoot=root;
        board=new GameObject("TablePresentation",typeof(RectTransform)).GetComponent<RectTransform>();
        board.SetParent(root,false);board.sizeDelta=new Vector2(1800,1000);
        RectTransform table = root.Find("Table") as RectTransform;
        tableOutline=table;
        if(table!=null){table.SetParent(board,false);table.anchorMin=table.anchorMax=table.pivot=new Vector2(.5f,.5f);table.anchoredPosition=Vector2.zero;table.sizeDelta=new Vector2(1510,710);}
        center.SetParent(board,false);center.anchorMin=center.anchorMax=new Vector2(.5f,.5f);center.anchoredPosition=Vector2.zero;
        RectTransform bid=root.Find("CurrentBidText") as RectTransform;
        if(bid!=null){bid.SetParent(board,false);bid.anchorMin=bid.anchorMax=new Vector2(.5f,.5f);bid.anchoredPosition=Vector2.zero;bid.sizeDelta=new Vector2(900,150);}
        board.SetSiblingIndex(table!=null?1:0);
        LateUpdate();
        return board;
    }
    private void LateUpdate()
    {
        if(canvasRoot==null||board==null||Screen.width<=0||Screen.height<=0)return;
        float sx=canvasRoot.rect.width/Screen.width,sy=canvasRoot.rect.height/Screen.height;
        float left=Screen.safeArea.xMin*sx+12;
        float right=(Screen.width-Screen.safeArea.xMax)*sx+MultiplayerPanelLayout.PanelWidth(canvasRoot.rect.width)+16;
        float top=(Screen.height-Screen.safeArea.yMax)*sy+220;
        float bottom=Screen.safeArea.yMin*sy+20;
        float width=Mathf.Max(300,canvasRoot.rect.width-left-right),height=Mathf.Max(240,canvasRoot.rect.height-top-bottom);
        board.anchorMin=board.anchorMax=new Vector2(.5f,.5f);
        // Move the whole presentation (including seats and cards) down slightly.
        float down=Mathf.Min(48f,height*.06f);
        board.anchoredPosition=new Vector2((left-right)*.5f,(bottom-top)*.5f-down);
        board.localScale=Vector3.one*Mathf.Min(width/1800,height/1000);
        foreach(var binding in seats)PositionSeat(binding.seat,binding.angle);
    }
}
