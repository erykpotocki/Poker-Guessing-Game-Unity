using PokerProfile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DailyRewardsUI : MonoBehaviour
{
    private string displayedDay;
    private bool timeVerified;
    private RectTransform panel;
    public static void Show(Canvas owner)
    {
        var old=owner.rootCanvas.transform.Find("DailyRewardsOverlay");
        if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);return;}
        var root=ShopUI.Overlay(owner,"DailyRewardsOverlay");
        root.GetComponent<Image>().color=new Color(0,0,0,.94f);
        root.GetComponent<Canvas>().sortingOrder=680;
        var ui=root.gameObject.AddComponent<DailyRewardsUI>();
        var dismiss=root.gameObject.AddComponent<Button>();dismiss.transition=Selectable.Transition.None;
        dismiss.onClick.AddListener(()=>Destroy(root.gameObject));ui.Build();
    }
    private void Update()
    {
        if(timeVerified!=PlayerProfileService.HasRewardTime || displayedDay!=RewardRules.DayKey(PlayerProfileService.RewardClock.UtcNow))Build();
        if(panel!=null){var root=(RectTransform)transform;panel.localScale=Vector3.one*Mathf.Max(.1f,Mathf.Min(1,Mathf.Min((root.rect.width-24)/960,(root.rect.height-24)/690)));}
    }
    private static Image Art(Transform parent,string resource,float x,float y,float size)
    {
        var image=ShopUI.Rect("RewardIcon",parent,x,y,size,size).gameObject.AddComponent<Image>();
        image.sprite=Resources.Load<Sprite>(resource);image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    private static string NextRewardPolishTime() => PlayerProfileService.HasRewardTime
        ? "Kolejna nagroda o 00:00 czasu polskiego. Postęp zachowany."
        : "Połącz z internetem, aby potwierdzić czas i odebrać nagrodę.";
    private void Build()
    {
        foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        timeVerified=PlayerProfileService.HasRewardTime;
        displayedDay=RewardRules.DayKey(PlayerProfileService.RewardClock.UtcNow);
        var d=PlayerProfileService.Data;var state=d.DailyRewards;
        bool available=PlayerProfileService.CanClaimDailyReward;
        bool completedWeek=!available&&state.Claims>0&&state.Claims%7==0;
        int week=completedWeek?state.Week-1:state.Week,day=completedWeek?8:state.Day;
        panel=ShopUI.Rect("UtilityDailyPanel",transform,0,0,960,690);
        panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        panel.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.04f,1);
        panel.gameObject.AddComponent<Button>().transition=Selectable.Transition.None;
        var border=panel.gameObject.AddComponent<Outline>();border.effectColor=new Color(.65f,.48f,.20f,.6f);border.effectDistance=new Vector2(1,-1);
        ShopUI.Text(panel,"KALENDARZ NAGRÓD",90,32,780,65,40).characterSpacing=5;
        var close=ShopUI.Rect("Close",panel,870,26,64,64).gameObject.AddComponent<Image>();close.color=Color.clear;
        close.gameObject.AddComponent<Button>().onClick.AddListener(()=>Destroy(gameObject));
        foreach(float angle in new[]{45f,-45f})
        {
            var stroke=ShopUI.Rect("CloseStroke",close.transform,0,0,32,3);stroke.anchorMin=stroke.anchorMax=stroke.pivot=new Vector2(.5f,.5f);stroke.anchoredPosition=Vector2.zero;stroke.localRotation=Quaternion.Euler(0,0,angle);
            var ink=stroke.gameObject.AddComponent<Image>();ink.color=new Color(1,1,1,.8f);ink.raycastTarget=false;
        }
        ShopUI.Text(panel,"TYDZIEŃ "+week+"   ·   PREMIA +"+System.Math.Min(50,(week-1)*5)+"%",40,107,880,44,27).color=new Color(.65f,.77f,.69f);
        const float width=116,gap=12,left=38;
        for(int i=1;i<=7;i++)
        {
            bool claimed=i<day,ready=i==day&&available,next=i==day;
            var tile=ShopUI.Rect("Day"+i,panel,left+(i-1)*(width+gap),184,width,245);
            tile.gameObject.AddComponent<Image>().color=claimed?new Color(.035f,.19f,.095f):next?new Color(.19f,.15f,.045f):new Color(.045f,.085f,.069f);
            var edge=tile.gameObject.AddComponent<Outline>();edge.effectDistance=new Vector2(1,-1);edge.effectColor=claimed?new Color(.20f,.78f,.39f):next?new Color(1,.78f,.25f):new Color(.25f,.32f,.26f);
            var band=ShopUI.Rect("Header",tile,0,0,width,46).gameObject.AddComponent<Image>();band.color=claimed?new Color(.06f,.35f,.16f):next?new Color(.64f,.44f,.10f):new Color(.11f,.17f,.13f);
            ShopUI.Text(tile,"DZIEŃ "+i,2,0,width-4,46,23);
            Art(tile,i==5?"SpinAssets/spin kolo":i==3||i==6?"WalletIcons/diament":"WalletIcons/złoto",26,67,64).color=claimed?new Color(.8f,1,.85f,.8f):Color.white;
            ShopUI.Text(tile,RewardRules.DailyAmount(i,week).ToString("N0"),2,144,width-4,42,29).fontStyle=FontStyles.Bold;
            ShopUI.Text(tile,claimed?"ODEBRANO":ready?"ODBIERZ":next?"NASTĘPNA":"—",2,196,width-4,32,19).color=claimed?new Color(.4f,1,.6f):next?new Color(1,.8f,.3f):new Color(.6f,.7f,.63f);
        }
        ShopUI.Text(panel,!timeVerified?"Sprawdzanie czasu przez internet":available?"Dzień "+day+" czeka na Ciebie":"Następna: dzień "+state.Day+" · "+RewardRules.DailyLabel(state.Day,state.Week),40,457,880,46,32);
        var action=ShopUI.Rect("UtilityDailyAction",panel,120,525,720,86).gameObject.AddComponent<Image>();action.color=new Color(1,.78f,.25f);
        var button=action.gameObject.AddComponent<Button>();button.targetGraphic=action;button.interactable=available||!timeVerified;
        button.onClick.AddListener(()=>{if(!timeVerified)RewardTimeSync.Retry();else if(PlayerProfileService.ClaimDailyReward())Build();});
        ShopUI.Text(action.transform,!timeVerified?"SPRAWDŹ POŁĄCZENIE":available?"ODBIERZ NAGRODĘ":"ODEBRANO",12,0,696,86,30).color=new Color(.08f,.06f,.02f);
        ShopUI.Text(panel,available?"Odbieraj po kolei. Przerwa nie resetuje postępu.":NextRewardPolishTime(),30,630,900,36,23).color=new Color(.63f,.73f,.67f);
        Update();
    }
}
