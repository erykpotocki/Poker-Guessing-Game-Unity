using System;
using UnityEngine;
using PokerProfile;

public static class PlayerProfileService
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern string PokerLoadProfile();
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern int PokerSaveProfile(string json);
#endif
    private sealed class PrefsStore : IProfileStore
    {
        public string Load()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string browserSave = PokerLoadProfile();
            if (!string.IsNullOrEmpty(browserSave)) return browserSave;
#endif
            return PlayerPrefs.GetString("playerProfile.v1", "");
        }
        public void Save(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Synchronous browser copy survives closing the tab before IndexedDB finishes.
            if (PokerSaveProfile(json) == 0) Debug.LogWarning("Zapis przeglądarki niedostępny; używam zapisu Unity.");
#endif
            PlayerPrefs.SetString("playerProfile.backup",PlayerPrefs.GetString("playerProfile.v1",""));
            PlayerPrefs.SetString("playerProfile.v1",json); PlayerPrefs.Save();
        }
    }
    private static IProfileStore store = new PrefsStore();
    private static PlayerSave current;
    public static IRewardClock RewardClock { get; set; } = new InternetRewardClock();
    public static bool ClaimDailyReward() { if(!CanClaimDailyReward || !RewardRules.ClaimDaily(Data,RewardClock.UtcNow))return false; Save();return true; }
    public static bool HasRewardTime => RewardClock is InternetRewardClock clock && clock.IsVerified;
    public static bool CanClaimDailyReward => HasRewardTime && RewardRules.CanClaimDaily(Data,RewardClock.UtcNow);
    public static void OnRewardTimeVerified()
    {
        var state=Data.DailyRewards;
        if(state.CalendarVersion==0)
        {
            // Legacy saves stored only a UTC date, without the claim instant.
            // Reserve today when that ambiguous date could fall on today's Polish date.
            var utc=RewardClock.UtcNow;
            if(state.Claims>0 && System.DateTime.TryParseExact(state.LastClaimDay,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var old)
                && old.Date>=utc.Date.AddDays(-1) && string.CompareOrdinal(state.LastClaimDay,RewardRules.DayKey(utc))<0)
                state.LastClaimDay=RewardRules.DayKey(utc);
            state.CalendarVersion=1;Save();
        }
    }
    public static bool ClaimBetaTester2026()
    {
        if(!RewardRules.ClaimBeta(Data))return false;Save();return true;
    }
    public static void GrantBetaTester2026()=>ClaimBetaTester2026();
    public static void CompleteOfflineMatch(string id)
    {
        if(RewardRules.CompleteOfflineMatch(Data,id))Save();
    }
    public static bool ClaimCareerMission(string id) { if(!RewardRules.Claim(Data,id))return false;Save();return true; }
    public static event Action Changed;
    public static event Action PurchaseCompleted;
    public static PlayerSave Data
    {
        get
        {
            if (current != null) return current;
            string json = store.Load();
            try { if (!string.IsNullOrEmpty(json)) current = JsonUtility.FromJson<PlayerSave>(json); } catch (Exception) { }
            if (current == null || current.Profile == null || current.Inventory == null || current.Wallet == null || current.Statistics == null)
            {
                current = new PlayerSave();
                current.Profile.Nickname = PlayerPrefs.GetString("lastNick","Gracz");
                string avatar = "avatar_" + Mathf.Max(0,PlayerPrefs.GetInt("avatarIndex",0));
                current.Profile.SelectedAvatarId = avatar;
                ProgressionRules.Own(current.Inventory.OwnedAvatars,avatar);
            }
            current.Profile ??= new PlayerProfile();
            if(!string.IsNullOrEmpty(json)&&!json.Contains("\"JoinedUtc\""))current.Profile.JoinedUtc="—";
            current.Inventory ??= new Inventory();
            current.Wallet ??= new Wallet();
            current.Statistics ??= new Statistics();
            current.Statistics.BotGames=Math.Max(current.Statistics.BotGames,current.Statistics.BotWins);
            current.Progression ??= new Progression();
            if(current.Progression.CurveVersion==0)
            {
                long oldXp=Math.Max(0,current.Progression.Experience);
                int oldLevel=1+(int)(oldXp/100);
                current.Progression.Experience=Progression.Threshold(oldLevel)+(oldXp%100)*(100L+50L*(oldLevel-1))/100;
                current.Progression.CurveVersion=1;
            }
            current.Daily ??= new MissionPeriod();
            current.Weekly ??= new MissionPeriod();
            current.Wheel ??= new WheelState();
            current.DailyRewards ??= new DailyRewardState();
            current.ClaimedCareerMissions ??= new System.Collections.Generic.List<string>();
            current.OwnedBadges ??= new System.Collections.Generic.List<string>();
            // Pre-migration earnings cannot be reconstructed from a wallet. Start these counters at zero.
            current.Version=2;
            if(current.Wheel.PendingPrize!=null && !current.Wheel.PendingPrize.IsValid)current.Wheel.PendingPrize=null;
            current.Achievements ??= new System.Collections.Generic.List<string>();
            current.Receipts ??= new System.Collections.Generic.List<string>();
            current.Opponents ??= new System.Collections.Generic.List<OpponentRecord>();
            current.PendingUnlocks ??= new System.Collections.Generic.List<PendingUnlock>();
            ProgressionRules.Own(current.Inventory.OwnedCardBacks,"6");
            if((current.Profile.SelectedCardBackId??"").StartsWith("HotSeatBack_")||string.IsNullOrEmpty(current.Profile.SelectedCardBackId))current.Profile.SelectedCardBackId="6";
            // Old wheel rewards used the global presentation queue. Keep the
            // owned items, but discard their duplicate notification on upgrade.
            current.PendingUnlocks.RemoveAll(item => item.Category == "avatar" &&
                (item.ItemId ?? "").StartsWith("download:") && string.IsNullOrEmpty(item.Source));
            for (int i = 0; i < 10; i++) ProgressionRules.Own(current.Inventory.OwnedAvatars,"avatar_"+i);
            ProgressionRules.RefreshPeriods(current,DateTime.UtcNow);
            return current;
        }
    }
    public static void Save() { store.Save(JsonUtility.ToJson(Data)); Changed?.Invoke(); }
    public static bool SetNickname(string nickname)
    {
        nickname = (nickname ?? "").Trim().Replace("<", "").Replace(">", "");
        if (nickname.Length < 2 || nickname.Length > 20) return false;
        Data.Profile.Nickname = nickname; Save(); return true;
    }
    public static bool Equip(string category,string id)
    {
        if (category == "avatar" && Data.Inventory.OwnedAvatars.Contains(id)) Data.Profile.SelectedAvatarId = id;
        else if (category == "frame" && (id == "none" || Data.Inventory.OwnedFrames.Contains(id))) Data.Profile.SelectedFrameId = id;
        else if (category == "back" && Data.Inventory.OwnedCardBacks.Contains(id)) Data.Profile.SelectedCardBackId = id;
        else if (category == "offlineBack" && id.StartsWith("HotSeatBack_")) Data.Profile.SelectedOfflineCardBackId = id;
        else return false;
        Save(); return true;
    }
    public static int AvatarIndex
    {
        get {
            string id = Data.Profile.SelectedAvatarId;
            if (id != null && id.StartsWith("avatar_") && int.TryParse(id.Substring(7),out int index)) return Mathf.Max(0,index);
            AvatarDatabase db = Resources.Load<AvatarDatabase>("ProfileAvatars");
            if (db != null && db.avatars != null)
                for (int i=0;i<db.avatars.Length;i++) if (AvatarId(i,db.avatars[i]) == id) return i;
            return 0;
        }
    }
    public static string AvatarId(int index, Sprite sprite) => index < 10 ? "avatar_"+index : "download:"+sprite.name;
    public static bool ShopAvailable => true;
    public static bool CompleteMatch(string id,bool won,int bots=0,int humans=1,int durationSeconds=0,bool advanced=false,int placement=0)
    {
        if ((id??"").StartsWith("hotseat:",StringComparison.OrdinalIgnoreCase)) return false;
        int minutes = Mathf.Clamp(durationSeconds / 60,0,20);
        int coins = Mathf.Clamp(10 + Mathf.Clamp(bots,0,5)*10 + minutes*7,10,200);
        if(won&&advanced)coins=Mathf.RoundToInt(coins*1.15f);
        int total=Mathf.Max(2,bots+humans);
        int rank=won?1:Mathf.Clamp(placement<=0?total:placement,2,total);
        int xp=ProgressionRules.MatchExperience(rank,total);
        bool completed = ProgressionRules.CompleteMatch(Data,id,won,DateTime.UtcNow,coins,xp,humans>=2,advanced);
        if (completed)
        {
            if(humans>=2) { Data.Statistics.OnlineGames++;if(won)Data.Statistics.OnlineWins++; }
            else if(bots>0){Data.Statistics.BotGames++;if(won)Data.Statistics.BotWins++;}
            Save();
        }
        return completed;
    }
    private static int StableDrop(string value,int modulo)
    {
        unchecked { int hash=17; foreach(char c in value??"") hash=hash*31+c; return (hash&int.MaxValue)%Mathf.Max(1,modulo); }
    }
    public static PendingUnlock PeekUnlock() => Data.PendingUnlocks.Count > 0 ? Data.PendingUnlocks[0] : null;
    public static void AcceptUnlock()
    {
        if (Data.PendingUnlocks.Count == 0) return;
        Data.PendingUnlocks.RemoveAt(0); Save();
    }
    public static bool BuyWithDiamonds(string category,string id,int price,string title) => BuyCosmetic(category,id,true);
    public static bool BuyCosmetic(string category,string id,bool diamonds)
    {
        if(!CosmeticCatalog.TryBuy(Data,category,id,diamonds))return false;
        Save();PurchaseCompleted?.Invoke();return true;    }    public static void RecordOpponentMatch(string profileId,string nickname,bool localWon)
    {
        if (string.IsNullOrWhiteSpace(profileId)) profileId = "nick:" + (nickname ?? "Gracz");
        if (Data.Opponents == null) Data.Opponents = new System.Collections.Generic.List<OpponentRecord>();
        OpponentRecord record = Data.Opponents.Find(item => item.ProfileId == profileId);
        if (record == null)
        {
            record = new OpponentRecord { ProfileId = profileId };
            Data.Opponents.Add(record);
        }
        record.Nickname = string.IsNullOrWhiteSpace(nickname) ? "Gracz" : nickname;
        record.GamesTogether++;
        if (localWon) record.WinsAgainst++;
        Save();
    }
    public static OpponentRecord GetOpponent(string profileId)
    {
        if (Data.Opponents == null || string.IsNullOrWhiteSpace(profileId)) return null;
        return Data.Opponents.Find(item => item.ProfileId == profileId);
    }
    public static void RecordRoundResult(string id,bool won,bool elimination)
    {
        if(!ProgressionRules.CompleteRound(Data,id,DateTime.UtcNow))return;
        if(won)Data.Statistics.RoundsWon++;
        if(elimination)Data.Statistics.Eliminations++;
        Save();
    }
    public static void CompleteRound(string id) { if (ProgressionRules.CompleteRound(Data,id,DateTime.UtcNow)) Save(); }
    public static TimeSpan SpinRemaining
    {
        get
        {
            DateTime now = DateTime.UtcNow;
            RefreshSpinCharges(now);
            long ticks = Data.Wheel.NextFreeUtcTicks - now.Ticks;
            return ticks > 0 ? TimeSpan.FromTicks(ticks) : TimeSpan.Zero;
        }
    }
    public static int SpinCharges { get { RefreshSpinCharges(DateTime.UtcNow); return Data.Wheel.Charges + Data.BonusSpins; } }
    public static bool CanSpin => SpinCharges > 0;
    public static float AvatarSpinChance => Data.Wheel.AvatarsWon==0?.10f:Data.Wheel.AvatarsWon==1?.05f:.10f/6f;
    public static string Spin() => Spin(out _);
    public static string Spin(out int sector)
    {
        return Spin(out sector, out _);
    }
    public static string DiamondAmount(int amount)
    {
        int last = amount % 10, lastTwo = amount % 100;
        return amount + (amount == 1 ? " diament" : last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14) ? " diamenty" : " diamentów");
    }
    public static string Spin(out int sector, out Sprite wonAvatar)
    {
        if(Data.Wheel.PendingPrize!=null && (!Data.Wheel.PendingPrize.IsValid || Data.Wheel.PendingPrize.Category=="frame" || ((Data.Wheel.PendingPrize.Category=="avatar" || Data.Wheel.PendingPrize.Category=="back") && !CosmeticCatalog.Get(Data.Wheel.PendingPrize.Category,Data.Wheel.PendingPrize.ItemId).Spin))){Data.Wheel.PendingPrize=null;Save();}
        if(Data.Wheel.PendingPrize!=null)
        {
            var pending=Data.Wheel.PendingPrize;sector=pending.Sector;
            // Older saves can have an empty label or an obsolete sector.
            if(pending.Category=="gold")pending.Sector=pending.Amount>50?6:3;
            if(pending.Category=="diamonds")pending.Sector=pending.Amount>3?2:1;
            sector=pending.Sector;
            wonAvatar=ResolveSpinPreview(pending);return pending.Category=="gold"?pending.Amount+" złota":pending.Category=="diamonds"?DiamondAmount(pending.Amount):pending.Category=="frame"?"Nowa ramka":pending.Category=="back"?"Nowy rewers":"Nowy avatar";
        }
        wonAvatar = null;
        sector = -1;
        DateTime now = DateTime.UtcNow;
        RefreshSpinCharges(now);
        if (Data.Wheel.Charges <= 0 && Data.BonusSpins <= 0) return null;
        // Clockwise from the top of the supplied wheel: ?, diamond, diamond
        // bag, gold, ?, diamond, gold bag, gold. One draw drives money AND art.
        float roll=UnityEngine.Random.value;
        float avatarChance=AvatarSpinChance;
        sector=roll<avatarChance?(UnityEngine.Random.value<.5f?0:4):
            roll<avatarChance+.30f?(UnityEngine.Random.value<.8f?(UnityEngine.Random.value<.5f?1:5):2):
            (UnityEngine.Random.value<.15f?6:(UnityEngine.Random.value<.5f?3:7));
        string reward;
        var prize=new SpinPrize{Sector=sector};
        if (sector == 0 || sector == 4)
        {
            var available = new System.Collections.Generic.List<Sprite>();
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("ShopAvatars"))
                if (CosmeticCatalog.Get("avatar","download:"+sprite.name).Spin && !Data.Inventory.OwnedAvatars.Contains("download:"+sprite.name)) available.Add(sprite);
            var backs=CosmeticCatalog.All("back").FindAll(o=>o.Spin&&!CosmeticCatalog.Owned("back",o.Id));
            if(backs.Count>0 && (available.Count==0||UnityEngine.Random.value<.5f))
            {
                var back=backs[UnityEngine.Random.Range(0,backs.Count)];
                prize.Category="back";prize.ItemId=back.Id;wonAvatar=back.Sprite;reward="Nowy rewers";
            }            else if (available.Count > 0)
            {
                Sprite sprite = available[UnityEngine.Random.Range(0,available.Count)];
                prize.Category="avatar";prize.ItemId="download:"+sprite.name;
                wonAvatar = sprite;
                reward = "Nowy avatar";
            }
            else
            {
                int amount = UnityEngine.Random.Range(1,4);
                prize.Category="diamonds";prize.Amount=amount;
                // With the cosmetic collection complete, land on a currency
                // sector instead of presenting currency under a question mark.
                sector=1;prize.Sector=sector;
                reward = DiamondAmount(amount);
            }
        }
        else if (sector == 1 || sector == 5 || sector == 2)
        {
            int amount = sector == 2 ? UnityEngine.Random.Range(4,7) : UnityEngine.Random.Range(1,4);
            prize.Category="diamonds";prize.Amount=amount; reward = DiamondAmount(amount);
        }
        else
        {
            int amount = sector == 6 ? UnityEngine.Random.Range(51,100) : UnityEngine.Random.Range(10,51);
            prize.Category="gold";prize.Amount=amount; reward = amount+" złota";
        }
        Data.Statistics.Spins++;
        Data.Wheel.FreeUsed = true;
        if (reward != null)
        {
            if(Data.BonusSpins>0)Data.BonusSpins--;
            else { if(Data.Wheel.Charges==3)Data.Wheel.NextFreeUtcTicks=now.AddHours(4).Ticks; Data.Wheel.Charges--; }
            prize.Label=reward;Data.Wheel.PendingPrize=prize;
            Save();
        }
        return reward;
    }
    public static Sprite ResolveSpinPreview(SpinPrize prize)
    {
        if(prize.Category=="avatar")return CosmeticCatalog.ResolveAvatar(prize.ItemId);
        if(prize.Category=="back")return CardBackDatabase.FindOnline(prize.ItemId);
        if(prize.Category=="frame")return LevelFrameCatalog.Resolve(prize.ItemId);
        return null;
    }
    public static void ClaimSpinPrize()
    {
        if(ProgressionRules.ClaimSpinPrize(Data))Save();
    }
    private static void RefreshSpinCharges(DateTime now)
    {
        var wheel=Data.Wheel;bool changed=false;
        if(wheel.ChargeVersion==0)
        {
            wheel.ChargeVersion=1;wheel.Charges=3;wheel.NextFreeUtcTicks=0;
            wheel.AvatarsWon=Data.Inventory.OwnedAvatars.FindAll(id=>id.StartsWith("download:")).Count;
            changed=true;
        }
        if(wheel.Charges<3 && wheel.NextFreeUtcTicks>0 && now.Ticks>=wheel.NextFreeUtcTicks)
        {
            long interval=TimeSpan.FromHours(4).Ticks;
            long recovered=1+(now.Ticks-wheel.NextFreeUtcTicks)/interval;
            wheel.Charges=(int)Math.Min(3,wheel.Charges+recovered);
            wheel.NextFreeUtcTicks=wheel.Charges==3?0:wheel.NextFreeUtcTicks+recovered*interval;
            changed=true;
        }
        if(changed)Save();
    }
    public static bool ClaimMission(bool weekly,string kind) { bool result = ProgressionRules.ClaimMission(Data,weekly,kind,DateTime.UtcNow); if (result) Save(); return result; }
    public static void GrantCompletedAdSpin()
    {
        RefreshSpinCharges(DateTime.UtcNow);
        if(Data.Wheel.Charges>=3)return;
        Data.Wheel.Charges++;
        Data.Statistics.AdsWatched++;
        if(Data.Wheel.Charges==3)Data.Wheel.NextFreeUtcTicks=0;
        Save();
    }
    private static bool adInFlight;
    public static void RequestRewardedAd(IRewardedAdProvider provider,AdReward reward,Action<AdOutcome> done)
    {
        if (adInFlight || provider == null || !provider.IsAvailable) { done?.Invoke(AdOutcome.Unavailable); return; }
        adInFlight = true;
        string request = Guid.NewGuid().ToString("N");
        bool completed = false;
        try
        {
            provider.Show(request,outcome => {
                if (completed) return;
                completed = true; adInFlight = false;
                if (ProgressionRules.CompleteAd(Data,request,outcome,reward,DateTime.UtcNow)) Save();
                done?.Invoke(outcome);
            });
        }
        catch (Exception) { adInFlight = false; if (!completed) done?.Invoke(AdOutcome.Failed); }
    }
}

// HTTPS time plus a monotonic timer: phone clock changes never advance reward days.
public sealed class InternetRewardClock : PokerProfile.IRewardClock
{
    private System.DateTime receivedUtc;
    private double receivedAt;
    private bool verified;
    public bool IsVerified => verified && UnityEngine.Time.realtimeSinceStartupAsDouble-receivedAt < 300;
    public System.DateTime UtcNow => verified ? receivedUtc.AddSeconds(System.Math.Max(0,UnityEngine.Time.realtimeSinceStartupAsDouble-receivedAt)) : System.DateTime.MinValue;
    public void Invalidate()=>verified=false;
    public void Accept(System.DateTime utc)
    {receivedUtc=System.DateTime.SpecifyKind(utc,System.DateTimeKind.Utc);receivedAt=UnityEngine.Time.realtimeSinceStartupAsDouble;verified=true;}
}
public sealed class RewardTimeSync : UnityEngine.MonoBehaviour
{
    [System.Serializable] private sealed class TimeReply { public string dateTime; public string timeZone; }
    private static RewardTimeSync instance;
    private bool busy;
    private double retryAt;
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if(instance!=null)return;
        instance=new UnityEngine.GameObject("RewardTimeSync").AddComponent<RewardTimeSync>();
        DontDestroyOnLoad(instance.gameObject);
    }
    public static void Retry() { if(instance!=null)instance.retryAt=0; }
    private void Update()
    {
        if(!busy && UnityEngine.Time.realtimeSinceStartupAsDouble>=retryAt)StartCoroutine(Sync());
    }
    private void OnApplicationPause(bool paused)
    {
        if(PlayerProfileService.RewardClock is InternetRewardClock clock)clock.Invalidate();
        if(!paused)retryAt=0;
    }
    private System.Collections.IEnumerator Sync()
    {
        busy=true;
        using(var request=UnityEngine.Networking.UnityWebRequest.Get("https://timeapi.io/api/time/current/zone?timeZone=UTC&nonce="+System.Guid.NewGuid().ToString("N")))
        {
            request.timeout=12;
            yield return request.SendWebRequest();
            if(request.result==UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                TimeReply reply=null;
                try{reply=UnityEngine.JsonUtility.FromJson<TimeReply>(request.downloadHandler.text);}catch(System.Exception){}
                if(reply!=null && reply.timeZone=="UTC" && System.DateTime.TryParse(reply.dateTime,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal|System.Globalization.DateTimeStyles.AdjustToUniversal,out var utc)
                    && utc.Year>=2026 && PlayerProfileService.RewardClock is InternetRewardClock clock)
                {clock.Accept(utc);PlayerProfileService.OnRewardTimeVerified();}
            }
        }
        retryAt=UnityEngine.Time.realtimeSinceStartupAsDouble+(PlayerProfileService.HasRewardTime?120:30);
        busy=false;
    }
}
