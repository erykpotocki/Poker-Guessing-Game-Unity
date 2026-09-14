using System;
using NUnit.Framework;
using PokerProfile;
using UnityEngine;

public class CareerRewardTests
{
    [Test] public void CalendarPersistsAndNeverSkipsOrClaimsTwice()
    {
        var d=new PlayerSave();var monday=new DateTime(2026,9,14,12,0,0,DateTimeKind.Utc);
        Assert.IsTrue(RewardRules.ClaimDaily(d,monday));Assert.AreEqual(500,d.Wallet.Coins);
        d=JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(d));
        Assert.IsFalse(RewardRules.ClaimDaily(d,monday.AddHours(9)));
        Assert.IsFalse(RewardRules.ClaimDaily(d,monday.AddDays(-1)));
        Assert.IsTrue(RewardRules.ClaimDaily(d,monday.AddDays(3)));
        Assert.AreEqual(1250,d.Wallet.Coins);Assert.AreEqual(3,d.DailyRewards.Day);
        Assert.IsFalse(RewardRules.ClaimDaily(d,monday.AddDays(3)));
    }
    [Test] public void CalendarCyclesAndCapsLinearBonus()
    {
        var d=new PlayerSave();var now=new DateTime(2026,1,1);
        for(int i=0;i<7;i++)Assert.IsTrue(RewardRules.ClaimDaily(d,now.AddDays(i)));
        Assert.AreEqual(1,d.DailyRewards.Day);Assert.AreEqual(2,d.DailyRewards.Week);
        Assert.AreEqual(1,d.BonusSpins);Assert.AreEqual(15,d.Wallet.RewardCurrency);
        Assert.AreEqual(525,RewardRules.DailyAmount(1,2));
        Assert.AreEqual(750,RewardRules.DailyAmount(1,100));
        Assert.IsFalse(RewardRules.ClaimDaily(d,now.AddDays(6)));
    }
    [Test] public void AllReachedTiersAreClaimableExactlyOnceAndBadgesAreSeparate()
    {
        var d=new PlayerSave();d.Statistics.BotWins=600;
        foreach(var m in RewardRules.Missions.FindAll(m=>m.Kind=="bots"&&m.Target<=600))
        { Assert.IsTrue(RewardRules.Claim(d,m.Id));Assert.IsFalse(RewardRules.Claim(d,m.Id)); }
        Assert.AreEqual(5,d.OwnedBadges.Count);Assert.AreEqual(1,d.Inventory.OwnedAvatars.Count);
        var restored=JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(d));
        Assert.IsFalse(RewardRules.Claim(restored,"career_bots_100"));
        Assert.AreEqual(AchievementStatus.Locked,RewardRules.Missions.Find(m=>m.Id=="career_bots_1000").Status(d));
    }
    [Test] public void HistoricalEarningsSurviveSpendingAndDuplicateMatch()
    {
        var d=new PlayerSave();RewardRules.Gold(d,1000);RewardRules.Diamonds(d,100);
        d.Wallet.Coins-=900;d.Wallet.RewardCurrency-=80;
        Assert.AreEqual(1000,RewardRules.Progress(d,"gold"));Assert.AreEqual(100,RewardRules.Progress(d,"diamonds"));
        Assert.IsTrue(ProgressionRules.CompleteMatch(d,"match-1",true,DateTime.UtcNow));
        long total=d.Statistics.TotalGoldEarned;
        Assert.IsFalse(ProgressionRules.CompleteMatch(d,"match-1",true,DateTime.UtcNow));
        Assert.AreEqual(total,d.Statistics.TotalGoldEarned);Assert.AreEqual(1,d.Statistics.GamesPlayed);
    }
    [Test] public void AdAvatarsNeverBecomeBadgesOrShopSpinPrizes()
    {
        var d=new PlayerSave();d.Statistics.AdsWatched=250;
        for(int i=1;i<=7;i++)
        {
            Assert.IsTrue(RewardRules.Claim(d,"ad_avatar_"+i));
            var offer=CosmeticCatalog.Get("avatar","download:reward_ad_"+i);
            Assert.IsFalse(offer.Purchasable);Assert.IsFalse(offer.Spin);Assert.IsNotNull(offer.Sprite);
        }
        Assert.AreEqual(0,d.OwnedBadges.Count);Assert.AreEqual(8,d.Inventory.OwnedAvatars.Count);
        Assert.IsFalse(CosmeticCatalog.Get("avatar","download:reward_beta_2026").Purchasable);
        foreach(var m in RewardRules.Missions)if(m.Icon!=null)Assert.IsNotNull(Resources.Load<Sprite>("AchievementBadges/"+m.Icon),m.Icon);
    }
    [TestCase(999, "999")]
    [TestCase(1034, "1k")]
    [TestCase(1568, "1,5k")]
    [TestCase(10000, "10k")]
    [TestCase(100000, "100k")]
    [TestCase(999999, "999,9k")]
    public void WalletAbbreviatesThousandsDownwards(long amount,string expected)
    {
        Assert.AreEqual(expected,PortraitMenuTopBar.FormatCurrency(amount));
    }
    [TestCase("2026-01-15T22:59:59Z","2026-01-15")]
    [TestCase("2026-01-15T23:00:00Z","2026-01-16")]
    [TestCase("2026-07-15T21:59:59Z","2026-07-15")]
    [TestCase("2026-07-15T22:00:00Z","2026-07-16")]
    [TestCase("2026-03-29T22:00:00Z","2026-03-30")]
    [TestCase("2026-10-25T23:00:00Z","2026-10-26")]
    public void CalendarChangesAtPolishMidnight(string instant,string day)
    {
        var utc=DateTime.Parse(instant,null,System.Globalization.DateTimeStyles.AdjustToUniversal);
        Assert.AreEqual(day,RewardRules.DayKey(utc));
    }
    [Test] public void UnverifiedInternetClockCannotGrantDailyReward()
    {
        var original=PlayerProfileService.RewardClock;
        try
        {
            PlayerProfileService.RewardClock=new InternetRewardClock();
            Assert.IsFalse(PlayerProfileService.HasRewardTime);
            Assert.IsFalse(PlayerProfileService.ClaimDailyReward());
        }
        finally{PlayerProfileService.RewardClock=original;}
    }
    [Test] public void BetaRequiresFiveCompletedGamesInEveryModeAndClaimsOnlyOnce()
    {
        var d=new PlayerSave();d.Statistics.BotGames=5;d.Statistics.OnlineGames=5;
        for(int i=0;i<4;i++)Assert.IsTrue(RewardRules.CompleteOfflineMatch(d,"offline-"+i));
        Assert.IsFalse(RewardRules.ClaimBeta(d));
        Assert.IsFalse(RewardRules.CompleteOfflineMatch(d,"offline-0"));
        Assert.AreEqual(4,d.Statistics.OfflineGames);
        Assert.IsTrue(RewardRules.CompleteOfflineMatch(d,"offline-4"));
        Assert.IsTrue(RewardRules.ClaimBeta(d));Assert.IsFalse(RewardRules.ClaimBeta(d));
        Assert.AreEqual(0,d.Statistics.GamesPlayed);
        Assert.AreEqual(0,d.Wallet.Coins);
        Assert.IsTrue(d.Inventory.OwnedAvatars.Contains("download:reward_beta_2026"));
    }
    private sealed class MemoryStore : IProfileStore
    {
        public string Json;
        public string Load()=>Json;
        public void Save(string json)=>Json=json;
    }
    [Test] public void ServiceClassifiesMatchesAndBonusSpinsSurviveReload()
    {
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
        var dataField=typeof(PlayerProfileService).GetField("current",flags);
        var storeField=typeof(PlayerProfileService).GetField("store",flags);
        var original=dataField.GetValue(null);var originalStore=storeField.GetValue(null);
        var memory=new MemoryStore();var d=new PlayerSave();
        dataField.SetValue(null,d);storeField.SetValue(null,memory);
        try
        {
            Assert.IsTrue(PlayerProfileService.CompleteMatch("bot-test",true,2,1));
            Assert.IsFalse(PlayerProfileService.CompleteMatch("bot-test",true,2,1));
            Assert.IsTrue(PlayerProfileService.CompleteMatch("online-test",true,1,2));
            Assert.IsTrue(PlayerProfileService.CompleteMatch("online-loss",false,0,2));
            Assert.AreEqual(1,d.Statistics.BotWins);Assert.AreEqual(1,d.Statistics.OnlineWins);Assert.AreEqual(2,d.Statistics.OnlineGames);
            d.Wheel.ChargeVersion=1;d.Wheel.Charges=3;d.BonusSpins=2;
            PlayerProfileService.Save();dataField.SetValue(null,null);
            Assert.AreEqual(5,PlayerProfileService.SpinCharges);
            Assert.IsNotNull(PlayerProfileService.Spin());d=PlayerProfileService.Data;
            Assert.AreEqual(3,d.Wheel.Charges);Assert.AreEqual(1,d.BonusSpins);
            int spins=d.Statistics.Spins;Assert.IsNotNull(PlayerProfileService.Spin());
            Assert.AreEqual(spins,d.Statistics.Spins);Assert.AreEqual(1,d.BonusSpins);
        }
        finally { dataField.SetValue(null,original);storeField.SetValue(null,originalStore); }
    }
}
