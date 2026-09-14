using System;
using System.Collections.Generic;

namespace PokerProfile
{
    [Serializable] public sealed class PlayerProfile
    {
        public string Nickname = "Gracz"; public string JoinedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public string SelectedAvatarId = "avatar_0";
        public string SelectedFrameId = "none";
        public string SelectedCardBackId = "6";
        public string SelectedOfflineCardBackId = "HotSeatBack_Ornate";
    }
    [Serializable] public sealed class Wallet { public long Coins, RewardCurrency; }
    [Serializable] public sealed class Inventory
    {
        public List<string> OwnedAvatars = new List<string> { "avatar_0" };
        public List<string> OwnedFrames = new List<string>();
        public List<string> OwnedCardBacks = new List<string> { "6" };
    }
    [Serializable] public sealed class Statistics { public int BotGames, OfflineGames; public int BotWins, OnlineWins, OnlineGames; public long TotalGoldEarned, TotalDiamondsEarned; public int GamesPlayed, GamesWon, RoundsPlayed, RoundsWon, Eliminations, AdsWatched, Spins; }
    [Serializable] public sealed class OpponentRecord
    {
        public string ProfileId = "";
        public string Nickname = "";
        public int GamesTogether;
        public int WinsAgainst;
    }
    [Serializable] public sealed class PendingUnlock
    {
        public string Source = "";
        public string Category = "";
        public string ItemId = "";
        public string Title = "";
    }
    [Serializable] public sealed class Progression
    {
        public long Experience;
        public int CurveVersion;
        public int Level => 1+(int)Math.Floor((Math.Sqrt(9+Math.Max(0,Experience)*.16)-3)/2);
        public static long Threshold(int level) { long n=Math.Max(0,level-1);return 25*n*(n+3); }
        public long RequiredExperience => 100L+50L*(Level-1);
        public long CurrentExperience => Math.Max(0,Experience)-Threshold(Level);
        public static int LevelGold(int level) => Math.Min(100,5+Math.Max(0,level-2)*3);
    }
    [Serializable] public sealed class MissionPeriod
    {
        public string Key = "";
        public int GamesBaseline, WinsBaseline, RoundsBaseline;
        public List<string> Claimed = new List<string>();
    }
    [Serializable] public sealed class WheelState
    {
        public string Day = "";
        public bool FreeUsed;
        public int ExtraUsed, ExtraCredits;
        public long NextFreeUtcTicks;
        public int ChargeVersion, Charges, AvatarsWon;
        public SpinPrize PendingPrize;
    }
    [Serializable] public sealed class SpinPrize
    {
        public int Sector, Amount;
        public string Category, ItemId, Label;
        public bool IsValid => (Category=="gold"||Category=="diamonds") ? Amount>0 :
            (Category=="avatar"||Category=="back"||Category=="frame") && !string.IsNullOrEmpty(ItemId);
    }
    [Serializable] public sealed class PlayerSave
    {
        public int Version = 2;
        public DailyRewardState DailyRewards = new DailyRewardState();
        public List<string> ClaimedCareerMissions = new List<string>(), OwnedBadges = new List<string>();
        public int BonusSpins;
        public bool RulesRead;
        public List<string> ClaimedIntroMissions = new List<string>();
        public PlayerProfile Profile = new PlayerProfile();
        public Wallet Wallet = new Wallet();
        public Inventory Inventory = new Inventory();
        public Statistics Statistics = new Statistics();
        public Progression Progression = new Progression();
        public MissionPeriod Daily = new MissionPeriod(), Weekly = new MissionPeriod();
        public WheelState Wheel = new WheelState();
        public List<string> Achievements = new List<string>();
        public List<string> Receipts = new List<string>();
        public List<OpponentRecord> Opponents = new List<OpponentRecord>();
        public List<PendingUnlock> PendingUnlocks = new List<PendingUnlock>();
    }
    public interface IProfileStore { string Load(); void Save(string json); }
    public enum AdOutcome { Completed, Cancelled, Failed, Unavailable }
    public enum AdReward { Currency, ExtraSpin }
    public interface IRewardedAdProvider
    {
        bool IsAvailable { get; }
        void Show(string requestId, Action<AdOutcome> completed);
    }
    public sealed class DisabledRewardedAds : IRewardedAdProvider
    {
        public bool IsAvailable => false;
        public void Show(string requestId, Action<AdOutcome> completed) => completed(AdOutcome.Unavailable);
    }
    // Only an explicitly injected test provider simulates success. Never connects to an ad network.
    public sealed class TestRewardedAds : IRewardedAdProvider
    {
        public AdOutcome NextOutcome = AdOutcome.Completed;
        public bool IsAvailable => true;
        public void Show(string requestId, Action<AdOutcome> completed) => completed(NextOutcome);
    }
    public static class ProgressionRules
    {
        public static bool ClaimSpinPrize(PlayerSave data)
        {
            var prize=data.Wheel.PendingPrize;if(prize==null||!prize.IsValid){data.Wheel.PendingPrize=null;return false;}
            if(prize.Category=="gold")RewardRules.Gold(data,prize.Amount);
            else if(prize.Category=="diamonds")RewardRules.Diamonds(data,prize.Amount);
            else if(prize.Category=="avatar"){Own(data.Inventory.OwnedAvatars,prize.ItemId);data.Wheel.AvatarsWon++;}
            else if(prize.Category=="back"){Own(data.Inventory.OwnedCardBacks,prize.ItemId);data.Wheel.AvatarsWon++;}
            else if(prize.Category=="frame"){data.Wheel.PendingPrize=null;return false;}
            else return false;
            data.Wheel.PendingPrize=null;return true;
        }
        public static readonly string[] IntroMissionIds={"first_game","read_rules","first_win","ten_rounds"};
        public static bool IntroMissionReady(PlayerSave d,int mission)=>mission==0?d.Statistics.GamesPlayed>0:mission==1?d.RulesRead:mission==2?d.Statistics.GamesWon>0:mission==3&&d.Statistics.RoundsPlayed>=10;
        public static bool ClaimIntroMission(PlayerSave d,int mission)
        {
            d.ClaimedIntroMissions??=new List<string>();
            if(mission<0||mission>=IntroMissionIds.Length||!IntroMissionReady(d,mission)||d.ClaimedIntroMissions.Contains(IntroMissionIds[mission]))return false;
            d.ClaimedIntroMissions.Add(IntroMissionIds[mission]);
            if(mission==0)Unlock(d,"frame","classic_wood","Ramka za pierwszą grę","mission");
            else if(mission==1)RewardRules.Diamonds(d,10);
            else RewardRules.Gold(d,mission==2?100:50);
            return true;
        }
        public static int MatchExperience(int placement,int total)
        {
            total=Math.Max(2,total);placement=Math.Max(1,Math.Min(total,placement));
            return (int)Math.Round(150*(.1+.9*(total-placement)/(double)(total-1)));
        }
        public const int MaxExtraSpins = 2;
        public static void RefreshPeriods(PlayerSave data, DateTime utc)
        {
            DateTime day = utc.Date;
            string today = day.ToString("yyyy-MM-dd");
            string week = day.AddDays(-(((int)day.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd");
            ResetPeriod(data.Daily, today, data.Statistics);
            ResetPeriod(data.Weekly, week, data.Statistics);
            // Never reset backwards if the device clock moves back.
            if (string.CompareOrdinal(today,data.Wheel.Day) > 0)
            { data.Wheel.Day = today; data.Wheel.FreeUsed = false; data.Wheel.ExtraUsed = 0; data.Wheel.ExtraCredits = 0; }
        }
        private static void ResetPeriod(MissionPeriod period, string key, Statistics stats)
        {
            if (string.CompareOrdinal(key,period.Key) <= 0) return;
            period.Key = key; period.GamesBaseline = stats.GamesPlayed;
            period.WinsBaseline = stats.GamesWon; period.RoundsBaseline = stats.RoundsPlayed;
            period.Claimed.Clear();
        }
        private static bool Receipt(PlayerSave data,string id)
        {
            if (string.IsNullOrWhiteSpace(id) || data.Receipts.Contains(id)) return false;
            data.Receipts.Add(id); return true;
        }
        public static bool CompleteMatch(PlayerSave data,string matchId,bool won,DateTime utc,int coins=20,int xp=25,bool diamond=false,bool advanced=false)
        {
            if ((matchId??"").StartsWith("hotseat:",StringComparison.OrdinalIgnoreCase)) return false;
            RefreshPeriods(data,utc);
            if (!Receipt(data,"match:"+matchId)) return false;
            data.Statistics.GamesPlayed++;
            if (won) data.Statistics.GamesWon++;
            RewardRules.Gold(data,Math.Max(0,Math.Min(won&&advanced?230:200,coins)));
            int previousLevel=data.Progression.Level;
            data.Progression.Experience += Math.Max(0,Math.Min(150,xp));
            for(int level=previousLevel+1;level<=data.Progression.Level;level++)
            {
                RewardRules.Gold(data,Progression.LevelGold(level));
                RewardRules.Diamonds(data,level);
                data.PendingUnlocks.Add(new PendingUnlock {Source="level",Category="level",ItemId=data.Profile.SelectedAvatarId,
                    Title=data.Profile.Nickname+"\nGratulacje! Poziom "+level+"\n"+Progression.LevelGold(level)+" złota + "+level+" diamentów"});
            }
            foreach(int level in LevelFrameCatalog.Levels)
                if(data.Progression.Level>=level&&!data.Inventory.OwnedFrames.Contains("level:"+level))
                    Unlock(data,"frame","level:"+level,"Ramka za poziom "+level,"level");
            if (diamond) RewardRules.Diamonds(data,1);
            Evaluate(data);
            return true;
        }
        public static bool CompleteRound(PlayerSave data,string roundId,DateTime utc)
        {
            RefreshPeriods(data,utc);
            if (!Receipt(data,"round:"+roundId)) return false;
            data.Statistics.RoundsPlayed++; Evaluate(data); return true;
        }
        public static void Evaluate(PlayerSave d)
        {
            // The first-game frame is claimed explicitly in Missions.
            Award(d,"games_10",d.Statistics.GamesPlayed,10,()=>Unlock(d,"avatar","avatar_1","Avatar za 10 gier","mission"));
            Award(d,"games_50",d.Statistics.GamesPlayed,50,()=>Unlock(d,"avatar","avatar_2","Avatar za 50 gier","mission"));
            Award(d,"games_100",d.Statistics.GamesPlayed,100,()=>Unlock(d,"back","HotSeatBack_RedDiamond","Rewers za 100 gier","mission"));
            Award(d,"wins_10",d.Statistics.GamesWon,10,()=>RewardRules.Gold(d,150));
            Award(d,"ads_10",d.Statistics.AdsWatched,10,()=>RewardRules.Diamonds(d,5));
            Award(d,"spins_10",d.Statistics.Spins,10,()=>RewardRules.Gold(d,100));
        }
        public static void Own(List<string> inventory,string id) { if (!inventory.Contains(id)) inventory.Add(id); }
        public static void Unlock(PlayerSave data,string category,string id,string title,string source="")
        {
            List<string> inventory = category == "avatar" ? data.Inventory.OwnedAvatars :
                category == "back" ? data.Inventory.OwnedCardBacks : data.Inventory.OwnedFrames;
            bool wasOwned = inventory.Contains(id);
            Own(inventory,id);
            if (wasOwned) return;
            data.PendingUnlocks ??= new List<PendingUnlock>();
            data.PendingUnlocks.Add(new PendingUnlock { Category=category,ItemId=id,Title=title,Source=source });
        }
        private static void Award(PlayerSave d,string id,int progress,int target,Action grant)
        {
            if (progress < target || d.Achievements.Contains(id)) return;
            d.Achievements.Add(id); grant();
        }
        public static bool ClaimMission(PlayerSave d,bool weekly,string kind,DateTime utc)
        {
            RefreshPeriods(d,utc);
            MissionPeriod period = weekly ? d.Weekly : d.Daily;
            int target = kind == "wins" ? (weekly ? 3 : 1) : kind == "rounds" ? (weekly ? 30 : 5) : (weekly ? 10 : 2);
            int progress = kind == "wins" ? d.Statistics.GamesWon-period.WinsBaseline : kind == "rounds" ? d.Statistics.RoundsPlayed-period.RoundsBaseline : d.Statistics.GamesPlayed-period.GamesBaseline;
            if ((kind != "games" && kind != "wins" && kind != "rounds") || progress < target || period.Claimed.Contains(kind)) return false;
            period.Claimed.Add(kind); RewardRules.Gold(d,weekly ? 100 : 20); return true;
        }
        public static bool CompleteAd(PlayerSave d,string requestId,AdOutcome outcome,AdReward reward,DateTime utc)
        {
            RefreshPeriods(d,utc);
            if (outcome != AdOutcome.Completed || (reward == AdReward.ExtraSpin && d.Wheel.ExtraUsed+d.Wheel.ExtraCredits >= MaxExtraSpins)) return false;
            if (!Receipt(d,"ad:"+requestId)) return false;
            d.Statistics.AdsWatched++;
            if (reward == AdReward.ExtraSpin) d.Wheel.ExtraCredits++; else RewardRules.Diamonds(d,2);
            Evaluate(d); return true;
        }
        public static string Spin(PlayerSave d,int roll,DateTime utc)
        {
            RefreshPeriods(d,utc);
            if (d.Wheel.FreeUsed)
            {
                if (d.Wheel.ExtraCredits <= 0 || d.Wheel.ExtraUsed >= MaxExtraSpins) return null;
                d.Wheel.ExtraCredits--; d.Wheel.ExtraUsed++;
            }
            else d.Wheel.FreeUsed = true;
            roll = Math.Max(0,Math.Min(999,roll));
            d.Statistics.Spins++;
            string reward;
            if (roll < 5) { RewardRules.Diamonds(d,1); reward = "1 niebieski diament"; }
            else { int coins = 10 + roll % 41; RewardRules.Gold(d,coins); reward = coins + " monet"; }
            Evaluate(d); return reward;
        }
    }
}
