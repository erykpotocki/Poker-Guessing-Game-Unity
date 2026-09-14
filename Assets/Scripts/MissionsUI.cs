using System.Linq;
using PokerProfile;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionsUI : MonoBehaviour
{
    private int tab=1;
    private bool collection;
    public static void Show(Canvas owner)
    {
        var old=owner.rootCanvas.transform.Find("MissionsOverlay");if(old!=null)Destroy(old.gameObject);
        ShopUI.Overlay(owner,"MissionsOverlay").gameObject.AddComponent<MissionsUI>().Build();
    }
    private void Build()
    {
        foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var d=PlayerProfileService.Data;
        var r=(RectTransform)transform;float w=r.rect.width,h=r.rect.height;
        ShopUI.Text(r,"MISJE I ODZNAKI",20,20,w-130,70,40);
        MissionButton(r,"×",w-100,20,76,66,()=>Destroy(gameObject));
        string[] tabs={"DZIENNE","KARIERA","SPECJALNE"};
        for(int i=0;i<3;i++){int n=i;ShopUI.ShopTab(r,tabs[i],16+i*(w-32)/3,102,(w-44)/3,66,tab==i&&!collection,()=>{tab=n;collection=false;Build();});}
        ShopUI.ShopTab(r,collection?"POKAŻ MISJE":"KOLEKCJA ODZNAK · "+d.OwnedBadges.Count,16,180,w-32,60,collection,()=>{collection=!collection;Build();});
        var view=ShopUI.Rect("MissionList",r,16,254,w-32,h-274);view.gameObject.AddComponent<Image>().color=Color.clear;view.gameObject.AddComponent<RectMask2D>();
        var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=view;
        var content=ShopUI.Rect("Content",view,0,0,w-32,0);scroll.content=content;
        float y=0,width=w-32;
        if(collection || tab==1 || tab==2)
        {
            var missions=RewardRules.Missions.Where(m=>collection?d.OwnedBadges.Contains(m.Id):tab==2?m.Kind=="ads":m.Kind!="ads").OrderByDescending(m=>m.Status(d)==AchievementStatus.ReadyToClaim).ThenBy(m=>m.Status(d)==AchievementStatus.Claimed);
            foreach(var m in missions)
            {
                var status=m.Status(d);bool claimed=status==AchievementStatus.Claimed,ready=status==AchievementStatus.ReadyToClaim;
                var row=ShopUI.Rect(m.Id,content,0,y,width,190);y+=202;
                Color color=CategoryColor(m.Kind);row.gameObject.AddComponent<Image>().color=Color.Lerp(new Color(.025f,.045f,.04f),color,.15f);
                var stripe=ShopUI.Rect("Category",row,0,0,5,190).gameObject.AddComponent<Image>();stripe.color=color;
                Sprite sprite=m.Avatar!=null?CosmeticCatalog.ResolveAvatar(m.Avatar):m.Icon!=null?Resources.Load<Sprite>("AchievementBadges/"+m.Icon):null;
                if(sprite!=null){var art=ShopUI.Rect("Badge",row,12,20,112,112).gameObject.AddComponent<Image>();art.sprite=sprite;art.preserveAspect=true;art.raycastTarget=false;}
                else ShopUI.Text(row,m.Kind=="gold"?"G":m.Kind=="diamonds"?"D":"★",16,30,100,90,56).color=color;
                ShopUI.Text(row,m.Name,136,10,width-148,42,27);
                long progress=System.Math.Min(m.Target,m.Progress(d));
                ShopUI.Text(row,m.Description+"\n"+progress.ToString("N0")+" / "+m.Target.ToString("N0"),136,54,width-148,62,22);
                var track=ShopUI.Rect("Progress",row,136,122,width-154,7);track.gameObject.AddComponent<Image>().color=new Color(.15f,.15f,.15f);
                ShopUI.Rect("Fill",track,0,0,(width-154)*Mathf.Clamp01((float)progress/m.Target),7).gameObject.AddComponent<Image>().color=color;
                ShopUI.Text(row,m.Avatar!=null?"Nagroda: avatar":m.Gold+" złota + odznaka",12,144,width*.56f,34,22);
                MissionButton(row,claimed?"ODEBRANO":ready?"ODBIERZ":"ZABLOKOWANE",width*.60f,140,width*.38f,42,()=>{if(PlayerProfileService.ClaimCareerMission(m.Id))Build();}).interactable=ready;
            }
            if(tab==2&&!collection)
            {
                var beta=ShopUI.Rect("Beta",content,0,y,width,330);y+=344;
                beta.gameObject.AddComponent<Image>().color=new Color(.035f,.09f,.065f);
                var art=ShopUI.Rect("Avatar",beta,12,20,120,120).gameObject.AddComponent<Image>();art.sprite=CosmeticCatalog.ResolveAvatar("download:reward_beta_2026");art.preserveAspect=true;
                bool owned=d.Inventory.OwnedAvatars.Contains("download:reward_beta_2026");
                ShopUI.Text(beta,"Beta Tester 2026",146,12,width-158,46,30);
                ShopUI.Text(beta,"Aby odebrać avatar, ukończ po 5 gier w każdym trybie.",146,62,width-158,72,25);
                string progress="Z botami: "+Mathf.Min(5,Mathf.Max(d.Statistics.BotGames,d.Statistics.BotWins))+" / 5"+
                    "\nZ ludźmi: "+Mathf.Min(5,d.Statistics.OnlineGames)+" / 5"+
                    "\nOffline na jednym telefonie: "+Mathf.Min(5,d.Statistics.OfflineGames)+" / 5";
                ShopUI.Text(beta,progress,16,152,width-32,110,26);
                MissionButton(beta,owned?"ODEBRANO":"ODBIERZ AVATAR",16,274,width-32,44,()=>{if(PlayerProfileService.ClaimBetaTester2026())Build();}).interactable=!owned&&RewardRules.BetaReady(d);

            }
        }
        if(!collection && tab==0)
        {
            ShopUI.Text(content,"Nagrody kalendarza odbieraj codziennie po kolei.\nPrzerwa nie resetuje postępu.",12,y,width-24,86,27);y+=96;
            MissionButton(content,"OTWÓRZ KALENDARZ",12,y,width-24,70,()=>DailyRewardsUI.Show(GetComponent<Canvas>()));y+=90;
            foreach(string kind in new[]{"games","wins","rounds"})
            {
                ProgressionRules.RefreshPeriods(d,PlayerProfileService.RewardClock.UtcNow);
                int target=kind=="games"?2:kind=="wins"?1:5;
                int progress=kind=="games"?d.Statistics.GamesPlayed-d.Daily.GamesBaseline:kind=="wins"?d.Statistics.GamesWon-d.Daily.WinsBaseline:d.Statistics.RoundsPlayed-d.Daily.RoundsBaseline;
                ShopUI.Text(content,(kind=="games"?"Rozegraj gry":kind=="wins"?"Wygraj grę":"Rozegraj rundy")+" · "+System.Math.Min(target,progress)+" / "+target+"\nNagroda: 20 złota",12,y,width*.6f,84,27);
                MissionButton(content,d.Daily.Claimed.Contains(kind)?"ODEBRANO":"ODBIERZ",width*.65f,y,width*.33f,70,()=>{PlayerProfileService.ClaimMission(false,kind);Build();}).interactable=progress>=target&&!d.Daily.Claimed.Contains(kind);y+=100;
            }
        }
        if(!collection&&tab==1)
        {
            string[] titles={"Pierwsza gra · ramka","Przeczytaj zasady · 10 diamentów","Pierwsza wygrana · 100 złota","10 rund · 50 złota"};
            for(int i=0;i<titles.Length;i++)
            {
                int index=i;bool claimed=d.ClaimedIntroMissions.Contains(ProgressionRules.IntroMissionIds[i]);
                ShopUI.Text(content,titles[i],12,y,width*.62f,70,25);
                MissionButton(content,claimed?"ODEBRANO":"ODBIERZ",width*.65f,y,width*.33f,65,()=>{if(ProgressionRules.ClaimIntroMission(d,index)){PlayerProfileService.Save();Build();}}).interactable=!claimed&&ProgressionRules.IntroMissionReady(d,i);y+=82;
            }
        }
        if(y==0){ShopUI.Text(content,"Nie masz jeszcze odznak. Odbierz ukończone misje w zakładce KARIERA.",12,12,width-24,120,28);y=140;}
        content.sizeDelta=new Vector2(width,y);
    }
    private static Button MissionButton(Transform parent,string title,float x,float y,float width,float height,System.Action action)
        => ShopUI.ShopTab(parent,title,x,y,width,height,true,action);
    private static Color CategoryColor(string kind)
    {
        switch(kind){case "wins":return new Color(1,.3f,.2f);case "bots":return Color.cyan;case "spins":return new Color(1,.5f,.1f);case "gold":return new Color(1,.8f,.2f);case "diamonds":case "daily":return new Color(.4f,.7f,1);case "onlineWins":case "onlineGames":return Color.green;default:return new Color(.8f,.4f,1);}
    }
}
