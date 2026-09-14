using System;
using NUnit.Framework;
using PokerProfile;

public class ProfileLevelProgressionTests
{
    [Test]
    public void AvatarOffersResolveImportedSpriteNamesIncludingSubassets()
    {
        var sprites=UnityEngine.Resources.LoadAll<UnityEngine.Sprite>("ShopAvatars");
        Assert.That(sprites.Length,Is.GreaterThan(0));
        foreach(var sprite in sprites)
        {
            string id="download:"+sprite.name;
            Assert.That(CosmeticCatalog.Get("avatar",id).Sprite,Is.SameAs(sprite),id);
            Assert.That(PlayerProfileService.ResolveSpinPreview(new SpinPrize{Category="avatar",ItemId=id}),Is.SameAs(sprite),id);
        }
        Assert.That(CosmeticCatalog.All("frame").Exists(offer=>offer.Id=="classic_wood"),Is.True);
    }
    [Test]
    public void ResumeExpiresAfterFiveMinutesAndRejectsLegacyOrFutureDates()
    {
        long now=DateTime.UtcNow.Ticks;
        Assert.That(ResumeTicket.WithinWindow(now,now),Is.True);
        Assert.That(ResumeTicket.WithinWindow(now-TimeSpan.TicksPerMinute*4,now),Is.True);
        Assert.That(ResumeTicket.WithinWindow(now-TimeSpan.TicksPerMinute*5,now),Is.False);
        Assert.That(ResumeTicket.WithinWindow(now-TimeSpan.TicksPerDay,now),Is.False);
        Assert.That(ResumeTicket.WithinWindow(0,now),Is.False);
        Assert.That(ResumeTicket.WithinWindow(now+1,now),Is.False);
    }
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator WalletGainSurvivesSceneBarReplacement()
    {
        yield return new UnityEngine.TestTools.EnterPlayMode();
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
        var dataField=typeof(PlayerProfileService).GetField("current",flags);
        var goldField=typeof(PortraitMenuTopBar).GetField("lastGold",flags);
        var diamondField=typeof(PortraitMenuTopBar).GetField("lastDiamonds",flags);
        var original=dataField.GetValue(null);var oldGold=goldField.GetValue(null);var oldDiamonds=diamondField.GetValue(null);
        var canvasObject=new UnityEngine.GameObject("WalletTestCanvas",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
        try
        {
            var save=new PlayerSave();save.Wallet.Coins=10;save.Wallet.RewardCurrency=12;
            dataField.SetValue(null,save);goldField.SetValue(null,-1L);diamondField.SetValue(null,-1L);
            var canvas=canvasObject.GetComponent<UnityEngine.Canvas>();canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;
            UnityEngine.Canvas.ForceUpdateCanvases();PortraitMenuTopBar.Ensure(canvas);
            var bar=canvasObject.transform.Find("PortraitMenuTopBar");
            Assert.That(bar.Find("Gold").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("10"));
            UnityEngine.Object.Destroy(bar.gameObject);yield return null;
            save.Wallet.Coins=15;
            PortraitMenuTopBar.Ensure(canvas);bar=canvasObject.transform.Find("PortraitMenuTopBar");
            Assert.That(bar.Find("Gold/CurrencyGain").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("+5"));
            Assert.That(bar.Find("Diamonds").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("12"));
            yield return new UnityEngine.WaitForSecondsRealtime(1.2f);
            Assert.That(bar.Find("Gold").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("15"));
            Assert.That(bar.Find("Gold/CurrencyGain"),Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
            dataField.SetValue(null,original);goldField.SetValue(null,oldGold);diamondField.SetValue(null,oldDiamonds);
        }
        yield return new UnityEngine.TestTools.ExitPlayMode();
    }

    [Test]
    public void FullBackCatalogHasExpectedPricesAndSpinExclusions()
    {
        var offers=CosmeticCatalog.All("back");
        Assert.That(offers.Count,Is.GreaterThan(40));
        var ids=new System.Collections.Generic.HashSet<string>();
        foreach(var offer in offers)
        {
            Assert.That(ids.Add(offer.Id),Is.True,"Duplicate: "+offer.Id);
            Assert.That(offer.Sprite,Is.Not.Null,offer.Id);
            if(offer.Id.EndsWith("wood")){Assert.That(offer.Gold,Is.EqualTo(250));Assert.That(offer.Spin,Is.True);}
            if(offer.Id.EndsWith("clasic")){Assert.That(offer.Gold,Is.EqualTo(50));Assert.That(offer.Spin,Is.True);}
        }
        for(int i=15;i<=24;i++)
        {
            var offer=CosmeticCatalog.Get("back",i+"prestige");
            Assert.That(offer.Gold,Is.EqualTo(10000));Assert.That(offer.Diamonds,Is.EqualTo(1000));
            Assert.That(offer.Both,Is.True);Assert.That(offer.Spin,Is.False);
        }
        foreach(string id in new[]{"2","3","4","5"})
        {var offer=CosmeticCatalog.Get("back",id);Assert.That(offer.Gold,Is.EqualTo(1000));Assert.That(offer.Spin,Is.True);}
        foreach(string id in new[]{"22","33","44","55","66"})
        {var offer=CosmeticCatalog.Get("back",id);Assert.That(offer.Purchasable,Is.False);Assert.That(offer.Spin,Is.True);}
        var fresh=new PlayerSave();
        Assert.That(fresh.Profile.SelectedCardBackId,Is.EqualTo("6"));
        Assert.That(fresh.Inventory.OwnedCardBacks,Does.Contain("6"));
        Assert.That(CardBackDatabase.OnlineSprites[0].name,Is.EqualTo("6"));
    }

    [Test]
    public void MenuThemePreservesResponsiveShopCaptions()
    {
        var root=new UnityEngine.GameObject("TestUi",typeof(UnityEngine.RectTransform));
        try
        {
            var button=ShopUI.Button(root.transform,"Nieukończone / w trakcie",0,0,280,90,()=>{});
            var label=button.GetComponentInChildren<TMPro.TMP_Text>();
            PokerButtonTheme.ApplyTo(button);
            Assert.That(label.enableAutoSizing,Is.True);
            Assert.That(label.fontSizeMax,Is.EqualTo(25));
            Assert.That(button.transform.Find("__MobileTouchTarget"),Is.Null);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }

    [Test]
    public void GameCategoriesBecomeTheVisibleScrollContentImmediately()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            HandRankPanelUI panel=null;
            foreach(var root in scene.GetRootGameObjects())
            {panel=root.GetComponentInChildren<HandRankPanelUI>(true);if(panel!=null)break;}
            Assert.That(panel,Is.Not.Null);
            typeof(HandRankPanelUI).GetMethod("ResolveReferences",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,null);
            panel.ShowCategories();
            var scroll=panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            Assert.That(scroll.content.name,Is.EqualTo("CategoryList"));
            Assert.That(scroll.content.parent,Is.EqualTo(scroll.viewport));
            Assert.That(scroll.content.gameObject.activeSelf,Is.True);
            Assert.That(scroll.content.GetComponentsInChildren<UnityEngine.UI.Button>().Length,Is.EqualTo(9));
            // Exercise the runtime layout: a transparent stencil image hid every button.
            var layout=panel.gameObject.AddComponent<MultiplayerPanelLayout>();
            typeof(MultiplayerPanelLayout).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(layout,null);
            Assert.That(scroll.viewport.GetComponent<UnityEngine.UI.Image>().color.a,Is.EqualTo(1));
            Assert.That(scroll.viewport.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic,Is.False);
            // References must survive moving the active list out of its old wrapper.
            typeof(HandRankPanelUI).GetMethod("ResolveReferences",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,null);
            panel.ShowCategories();
            Assert.That(scroll.content.name,Is.EqualTo("CategoryList"));
            Assert.That(scroll.content.GetComponentsInChildren<UnityEngine.UI.Button>().Length,Is.EqualTo(9));
        }
        finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }

    [Test]
    public void CombinedPrestigePurchaseNeverPartiallyCharges()
    {
        var save=new PlayerSave();save.Wallet.Coins=10000;save.Wallet.RewardCurrency=999;
        Assert.That(CosmeticCatalog.TryBuy(save,"back","15prestige",false),Is.False);
        Assert.That(save.Wallet.Coins,Is.EqualTo(10000));
        Assert.That(save.Wallet.RewardCurrency,Is.EqualTo(999));
        save.Wallet.RewardCurrency=1000;
        Assert.That(CosmeticCatalog.TryBuy(save,"back","15prestige",true),Is.True);
        Assert.That(save.Wallet.Coins,Is.Zero);Assert.That(save.Wallet.RewardCurrency,Is.Zero);
        Assert.That(CosmeticCatalog.TryBuy(save,"back","15prestige",true),Is.False);
    }
    [Test]
    public void MissionCurrencyNeedsExplicitClaimAndHasNoPopup()
    {
        var save=new PlayerSave();Assert.That(ProgressionRules.ClaimIntroMission(save,1),Is.False);
        save.RulesRead=true;Assert.That(save.Wallet.RewardCurrency,Is.Zero);
        Assert.That(ProgressionRules.ClaimIntroMission(save,1),Is.True);
        Assert.That(save.Wallet.RewardCurrency,Is.EqualTo(10));Assert.That(save.PendingUnlocks,Is.Empty);
        Assert.That(ProgressionRules.ClaimIntroMission(save,1),Is.False);
        Assert.That(save.Wallet.RewardCurrency,Is.EqualTo(10));
    }
    [TestCase("Q Q 9 9","dama dama 99",true)]
    [TestCase("J J","jupek dupek",true)]
    [TestCase("K K","król king",true)]
    [TestCase("Kolor ♥","serce",true)]
    [TestCase("Kolor ♦","dzwonek",true)]
    [TestCase("Kolor ♠","wino",true)]
    [TestCase("Kolor ♣","żołądź",true)]
    [TestCase("Kolor ♣","clubs",true)]
    [TestCase("Kolor ♥","wino",false)]
    [TestCase("Q Q","dama dama dama",false)]
    public void SearchUnderstandsCardAliases(string candidate,string query,bool expected)
    {
        Assert.That(HandRankPanelUI.MatchesSearch(candidate,query),Is.EqualTo(expected));
    }
    [Test]
    public void EmptyDeserializedPrizeCannotBlockTheNextSpin()
    {
        var save=new PlayerSave();save.Wheel.PendingPrize=UnityEngine.JsonUtility.FromJson<SpinPrize>("{}");
        Assert.That(save.Wheel.PendingPrize.IsValid,Is.False);
        Assert.That(ProgressionRules.ClaimSpinPrize(save),Is.False);
        Assert.That(save.Wheel.PendingPrize,Is.Null);
        Assert.That(save.Wallet.Coins,Is.Zero);
    }
    [Test]
    public void FramesCanNeverBeClaimedFromSpin()
    {
        var save=new PlayerSave();save.Wheel.PendingPrize=new SpinPrize{Category="frame",ItemId="level:10"};
        Assert.That(ProgressionRules.ClaimSpinPrize(save),Is.False);
        Assert.That(save.Inventory.OwnedFrames,Is.Empty);
        Assert.That(save.Wheel.PendingPrize,Is.Null);
    }
    [Test]
    public void EarlyFramesMustBeUnlockedInOrder()
    {
        var save=new PlayerSave();
        Assert.That(CosmeticCatalog.CanUnlockFrame(save,"level:10"),Is.True);
        Assert.That(CosmeticCatalog.CanUnlockFrame(save,"level:400"),Is.False);
        save.Inventory.OwnedFrames.Add("level:10");
        Assert.That(CosmeticCatalog.CanUnlockFrame(save,"level:20"),Is.True);
        Assert.That(CosmeticCatalog.CanUnlockFrame(save,"level:30"),Is.False);
    }
    [Test]
    public void PlacementRewardsAreOrderedAndFirstWinLevelsUp()
    {
        for(int total=2;total<=10;total++)
        {
            Assert.That(ProgressionRules.MatchExperience(total,total),Is.EqualTo(15));
            for(int place=2;place<=total;place++)Assert.That(ProgressionRules.MatchExperience(place,total),Is.LessThan(ProgressionRules.MatchExperience(place-1,total)));
        }
        var save=new PlayerSave();ProgressionRules.CompleteMatch(save,"winner",true,DateTime.UtcNow,20,ProgressionRules.MatchExperience(1,6));
        Assert.That(save.Progression.Level,Is.GreaterThanOrEqualTo(2));
        Assert.That(save.Inventory.OwnedFrames,Does.Not.Contain("classic_wood"));
    }    [Test]
    public void PendingSpinDoesNotGrantGoldUntilClaimAndCannotBeClaimedTwice()
    {
        var save=new PlayerSave();save.Wheel.PendingPrize=new SpinPrize{Category="gold",Amount=44};
        Assert.That(save.Wallet.Coins,Is.Zero);
        Assert.That(ProgressionRules.ClaimSpinPrize(save),Is.True);
        Assert.That(save.Wallet.Coins,Is.EqualTo(44));
        Assert.That(ProgressionRules.ClaimSpinPrize(save),Is.False);
        Assert.That(save.Wallet.Coins,Is.EqualTo(44));
    }
    [TestCase("avatar","download:test")]
    [TestCase("back","3clasic")]
    public void CosmeticsStayLockedUntilSpinClaim(string category,string id)
    {
        var save=new PlayerSave();save.Wheel.PendingPrize=new SpinPrize{Category=category,ItemId=id};
        var inventory=category=="avatar"?save.Inventory.OwnedAvatars:save.Inventory.OwnedCardBacks;
        Assert.That(inventory,Does.Not.Contain(id));
        ProgressionRules.ClaimSpinPrize(save);
        Assert.That(inventory,Does.Contain(id));
        Assert.That(save.Wheel.AvatarsWon,Is.EqualTo(1));
    }
    [Test]
    public void ThresholdsAndNextLevelCostsAgree()
    {
        for(int level=1;level<=1000;level++)
        {
            var progression=new Progression{Experience=Progression.Threshold(level)};
            Assert.That(progression.Level,Is.EqualTo(level));
            Assert.That(progression.CurrentExperience,Is.Zero);
            Assert.That(progression.RequiredExperience,Is.EqualTo(100L+50L*(level-1)));
            progression.Experience+=progression.RequiredExperience-1;
            Assert.That(progression.Level,Is.EqualTo(level));
            progression.Experience++;
            Assert.That(progression.Level,Is.EqualTo(level+1));
        }
    }

    [Test]
    public void CompletionGrantsLevelGoldOnlyOnce_AndNeverForHotSeat()
    {
        var save=new PlayerSave();save.Progression.Experience=90;
        Assert.That(ProgressionRules.CompleteMatch(save,"hotseat:test",false,DateTime.UtcNow,20,25),Is.False);
        Assert.That(save.Progression.Experience,Is.EqualTo(90));
        Assert.That(ProgressionRules.CompleteMatch(save,"online:test",false,DateTime.UtcNow,20,25),Is.True);
        Assert.That(save.Progression.Level,Is.EqualTo(2));
        Assert.That(save.Progression.CurrentExperience,Is.EqualTo(15));
        Assert.That(save.Wallet.Coins,Is.EqualTo(25));
        Assert.That(save.Wallet.RewardCurrency,Is.EqualTo(2));
        Assert.That(ProgressionRules.CompleteMatch(save,"online:test",false,DateTime.UtcNow,20,25),Is.False);
        Assert.That(save.Wallet.Coins,Is.EqualTo(25));
        Assert.That(Progression.LevelGold(1000),Is.EqualTo(100));
    }
    [Test]
    public void LevelTenUnlocksFrameWithoutEquippingIt()
    {
        var save=new PlayerSave();save.Progression.Experience=Progression.Threshold(10)-10;
        ProgressionRules.CompleteMatch(save,"frame-test",true,DateTime.UtcNow,20,25);
        Assert.That(save.Inventory.OwnedFrames,Does.Contain("level:10"));
        Assert.That(save.Profile.SelectedFrameId,Is.EqualTo("none"));
        Assert.That(save.Wallet.RewardCurrency,Is.EqualTo(10));
    }
}
