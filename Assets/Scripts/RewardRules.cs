using System;
using System.Collections.Generic;

namespace PokerProfile
{
    public interface IRewardClock { DateTime UtcNow { get; } }
    public sealed class DeviceRewardClock : IRewardClock { public DateTime UtcNow => DateTime.UtcNow; }
    [Serializable] public sealed class DailyRewardState
    {
        public int Claims;
        public int CalendarVersion;
        public string LastClaimDay = "";
        public int Day => Claims % 7 + 1;
        public int Week => Claims / 7 + 1;
    }
    public enum AchievementStatus { Locked, ReadyToClaim, Claimed }
    public sealed class CareerMission
    {
        public string Id, Name, Description, Kind, Icon, Avatar;
        public long Target;
        public int Gold, Diamonds, Spins;
        public long Progress(PlayerSave d) => RewardRules.Progress(d, Kind);
        public AchievementStatus Status(PlayerSave d) => d.ClaimedCareerMissions.Contains(Id) ? AchievementStatus.Claimed : Progress(d) >= Target ? AchievementStatus.ReadyToClaim : AchievementStatus.Locked;
    }
    public static class RewardRules
    {
        public static bool BetaReady(PlayerSave d) => Math.Max(d.Statistics.BotGames,d.Statistics.BotWins)>=5 && d.Statistics.OnlineGames>=5 && d.Statistics.OfflineGames>=5;
        public static bool ClaimBeta(PlayerSave d)
        {
            if(!BetaReady(d)||d.Inventory.OwnedAvatars.Contains("download:reward_beta_2026"))return false;
            ProgressionRules.Unlock(d,"avatar","download:reward_beta_2026","Beta Tester 2026","beta");return true;
        }
        public static bool CompleteOfflineMatch(PlayerSave d,string id)
        {
            if(string.IsNullOrWhiteSpace(id)||d.Receipts.Contains("offline:"+id))return false;
            d.Receipts.Add("offline:"+id);d.Statistics.OfflineGames++;return true;
        }
        public static readonly int[] AdThresholds = { 1, 5, 10, 25, 50, 100, 250 };
        public static readonly List<CareerMission> Missions = Build();
        private static List<CareerMission> Build()
        {
            var list = new List<CareerMission>();
            Add(list,"games","Rozegrane gry",new long[]{1,10,50,100,500,1000});
            Add(list,"wins","Wygrane gry",new long[]{1,10,50,100,500,1000});
            Add(list,"bots","Wygrane z botami",new long[]{1,10,50,100,500,1000});
            Add(list,"onlineWins","Wygrane z graczami",new long[]{1,10,25,50,100,250});
            Add(list,"onlineGames","Rozegrane gry online",new long[]{1,10,50,100,500,1000});
            Add(list,"spins","Wykonane spiny",new long[]{1,10,50,100,500,1000});
            Add(list,"gold","Łącznie zdobyte złoto",new long[]{1000,10000,100000,500000,1000000});
            Add(list,"diamonds","Łącznie zdobyte diamenty",new long[]{10,50,100,500,1000});
            foreach(string kind in new[]{"avatars","backs","frames"})
                Add(list,kind,kind=="avatars"?"Odblokowane avatary":kind=="backs"?"Odblokowane rewersy":"Odblokowane ramki",new long[]{1,5,10,25});
            Add(list,"daily","Odebrane dni kalendarza",new long[]{7});
            for(int i=0;i<AdThresholds.Length;i++)list.Add(new CareerMission{Id="ad_avatar_"+(i+1),Kind="ads",Name="Kolekcja reklam · "+(i+1),Description="Obejrzyj ukończone reklamy",Target=AdThresholds[i],Avatar="download:reward_ad_"+(i+1)});
            return list;
        }
        private static void Add(List<CareerMission> list,string kind,string name,long[] targets)
        {
            for(int i=0;i<targets.Length;i++)
            {
                long target=targets[i];
                string icon=kind=="games"&&target==1?"first_game":kind=="games"&&target==100?"games_100":kind=="wins"&&target==10?"wins_10":kind=="bots"&&target==100?"bots_100":kind=="onlineWins"&&target==25?"online_25":kind=="spins"&&target==50?"spins_50":kind=="gold"&&target==100000?"gold_100k":kind=="avatars"&&target==10?"avatars_10":kind=="daily"?"daily_7":null;
                list.Add(new CareerMission{Id="career_"+kind+"_"+target,Kind=kind,Name=name+" · "+target,Description=name,Target=target,Icon=icon,Gold=(i+1)*(i+1)*(kind=="bots"?25:kind=="onlineWins"?100:50)});
            }
        }
        public static long Progress(PlayerSave d,string kind)
        {
            switch(kind)
            {
                case "games":return d.Statistics.GamesPlayed;
                case "wins":return d.Statistics.GamesWon;
                case "bots":return d.Statistics.BotWins;
                case "onlineWins":return d.Statistics.OnlineWins;
                case "onlineGames":return d.Statistics.OnlineGames;
                case "spins":return d.Statistics.Spins;
                case "gold":return d.Statistics.TotalGoldEarned;
                case "diamonds":return d.Statistics.TotalDiamondsEarned;
                case "avatars":return new HashSet<string>(d.Inventory.OwnedAvatars).Count;
                case "backs":return new HashSet<string>(d.Inventory.OwnedCardBacks).Count;
                case "frames":return new HashSet<string>(d.Inventory.OwnedFrames).Count;
                case "daily":return d.DailyRewards.Claims;
                case "ads":return d.Statistics.AdsWatched;
                default:return 0;
            }
        }
        public static void Gold(PlayerSave d,long amount) { amount=Math.Max(0,amount);d.Wallet.Coins+=amount;d.Statistics.TotalGoldEarned+=amount; }
        public static void Diamonds(PlayerSave d,long amount) { amount=Math.Max(0,amount);d.Wallet.RewardCurrency+=amount;d.Statistics.TotalDiamondsEarned+=amount; }
        public static bool Claim(PlayerSave d,string id)
        {
            var mission=Missions.Find(m=>m.Id==id);
            if(mission==null||mission.Status(d)!=AchievementStatus.ReadyToClaim)return false;
            d.ClaimedCareerMissions.Add(id);
            if(mission.Avatar!=null)ProgressionRules.Unlock(d,"avatar",mission.Avatar,mission.Name,"ad");
            else ProgressionRules.Own(d.OwnedBadges,id);
            Gold(d,mission.Gold);Diamonds(d,mission.Diamonds);d.BonusSpins+=mission.Spins;
            return true;
        }
        // All reward days use Warsaw midnight, independent of the device timezone.
        public static string DayKey(DateTime utc) => PolishTime(utc).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
    public static System.DateTime PolishTime(System.DateTime utc)
    {
        foreach(string id in new[]{"Europe/Warsaw","Central European Standard Time"})
        {
            try{return System.TimeZoneInfo.ConvertTimeFromUtc(utc,System.TimeZoneInfo.FindSystemTimeZoneById(id));}
            catch(System.TimeZoneNotFoundException){}
            catch(System.InvalidTimeZoneException){}
        }
        // WebGL can omit the timezone database. Modern Polish DST changes at
        // 01:00 UTC on the last Sundays of March and October.
        var march=new System.DateTime(utc.Year,3,31,1,0,0,System.DateTimeKind.Utc);
        var october=new System.DateTime(utc.Year,10,31,1,0,0,System.DateTimeKind.Utc);
        var start=march.AddDays(-(int)march.DayOfWeek);
        var end=october.AddDays(-(int)october.DayOfWeek);
        return utc.AddHours(utc>=start&&utc<end?2:1);
    }

        public static bool CanClaimDaily(PlayerSave d,DateTime utc) => string.CompareOrdinal(DayKey(utc),d.DailyRewards.LastClaimDay)>0;
        public static int DailyAmount(int day,int week)
        {
            int[] amounts={500,750,5,1000,1,10,2000};
            if(day<1||day>7)throw new ArgumentOutOfRangeException(nameof(day));
            return day==5?1:(int)(amounts[day-1]*(100L+Math.Min(10,Math.Max(0,week-1))*5)/100);
        }
        public static string DailyLabel(int day,int week) => DailyAmount(day,week)+(day==3||day==6?" diamentów":day==5?" darmowy spin":" złota");
        public static bool ClaimDaily(PlayerSave d,DateTime utc)
        {
            if(!CanClaimDaily(d,utc))return false;
            int day=d.DailyRewards.Day,amount=DailyAmount(day,d.DailyRewards.Week);
            d.DailyRewards.CalendarVersion=1;
            d.DailyRewards.LastClaimDay=DayKey(utc);d.DailyRewards.Claims++;
            if(day==5)d.BonusSpins++;
            else if(day==3||day==6)Diamonds(d,amount);
            else Gold(d,amount);
            return true;
        }
    }
}
