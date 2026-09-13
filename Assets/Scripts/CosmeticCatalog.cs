using System;
using System.Collections.Generic;
using UnityEngine;
using PokerProfile;

public static class CosmeticCatalog
{
    private static readonly Dictionary<string,Sprite> avatarSprites=new Dictionary<string,Sprite>();
    public static Sprite ResolveAvatar(string id)
    {
        if(string.IsNullOrEmpty(id)||!id.StartsWith("download:"))return null;
        if(avatarSprites.Count==0)
        {
            foreach(var sprite in Resources.LoadAll<Sprite>("ShopAvatars"))avatarSprites[sprite.name]=sprite;
            var database=Resources.Load<AvatarDatabase>("ProfileAvatars");
            if(database!=null&&database.avatars!=null)
                for(int i=10;i<database.avatars.Length;i++)
                    if(database.avatars[i]!=null)avatarSprites[database.avatars[i].name]=database.avatars[i];
        }
        avatarSprites.TryGetValue(id.Substring(9),out var found);
        return found;
    }
    public sealed class Offer
    {
        public string Category, Id, Title;
        public Sprite Sprite;
        public int Gold, Diamonds;
        public bool Both, Spin;
        public bool Purchasable => Gold > 0 || Diamonds > 0;
    }
    public static Offer Get(string category,string id)
    {
        var o=new Offer{Category=category,Id=id,Title=id};
        if(category=="avatar" && id.StartsWith("download:"))
        {
            string name=id.Substring(9); o.Sprite=ResolveAvatar(id);
            int group=AvatarCategories.Category(10,name);
            o.Title=group==1?"Zwierzęta":group==2?"Halloween":group==3?"Wakacje":"Pokerzyści";
            if(group==1){o.Gold=5000;o.Spin=true;}
            else if(group==2||group==3)o.Gold=1000;
            else if(name.Contains("20_32_")||name.Contains("20_33_")){o.Gold=2000;o.Spin=true;}
            else o.Diamonds=50;
        }
        else if(category=="back")
        {
            o.Sprite=Array.Find(CardBackDatabase.OnlineSprites,s=>s.name==id);o.Title="Rewers "+id;
            if(o.Sprite==null)return o;
            if(id=="6")return o;
            if(id.EndsWith("prestige") && int.TryParse(id.Replace("prestige",""),out int tier))
            {o.Gold=tier<=6?5000:tier<=14?7500:10000;o.Diamonds=tier<=6?500:tier<=14?750:1000;o.Both=tier>=15;o.Spin=tier<15;}
            else if(id.Contains("wood")){o.Gold=250;o.Spin=true;}
            else if(id.Contains("clasic")){o.Gold=50;o.Spin=true;}
            else if(id.Length==1){o.Gold=1000;o.Spin=true;}
            else o.Spin=true;
        }
        else if(category=="frame" && id=="classic_wood")
        {o.Sprite=LevelFrameCatalog.Resolve(id);o.Title="Ramka za pierwszą grę";}
        else if(category=="frame" && id.StartsWith("level:") && int.TryParse(id.Substring(6),out int level))
        {o.Diamonds=Array.IndexOf(LevelFrameCatalog.Levels,level)>=0?level*10:0;o.Sprite=LevelFrameCatalog.Resolve(id);o.Title="Ramka · poziom "+level;}
        return o;
    }
    public static bool TryBuy(PlayerSave d,string category,string id,bool diamonds)
    {
        var offer=Get(category,id);
        var inventory=category=="avatar"?d.Inventory.OwnedAvatars:category=="back"?d.Inventory.OwnedCardBacks:d.Inventory.OwnedFrames;
        if(!offer.Purchasable||inventory.Contains(id))return false;
        if(category=="frame"&&!CanUnlockFrame(d,id))return false;
        if(!offer.Both&&(diamonds?offer.Diamonds:offer.Gold)<=0)return false;
        int gold=offer.Both||!diamonds?offer.Gold:0,gems=offer.Both||diamonds?offer.Diamonds:0;
        if(d.Wallet.Coins<gold||d.Wallet.RewardCurrency<gems)return false;
        d.Wallet.Coins-=gold;d.Wallet.RewardCurrency-=gems;
        ProgressionRules.Unlock(d,category,id,offer.Title,"shop");return true;
    }
    public static bool Owned(string category,string id)
    {
        var d=PlayerProfileService.Data;
        return category=="avatar"?d.Inventory.OwnedAvatars.Contains(id):category=="back"?d.Inventory.OwnedCardBacks.Contains(id):id=="none"||d.Inventory.OwnedFrames.Contains(id);
    }
    public static bool CanUnlockFrame(PlayerSave d,string id)
    {
        foreach(int level in LevelFrameCatalog.Levels)
            if(d.Progression.Level<level&&!d.Inventory.OwnedFrames.Contains("level:"+level))return id=="level:"+level;
        return false;
    }
    public static List<Offer> All(string category)
    {
        var result=new List<Offer>();
        if(category=="avatar")
        {
            ResolveAvatar("download:");
            var names=new List<string>(avatarSprites.Keys);names.Sort(StringComparer.Ordinal);
            foreach(var name in names)result.Add(Get(category,"download:"+name));
        }
        if(category=="back")foreach(var s in CardBackDatabase.OnlineSprites)result.Add(Get(category,s.name));
        if(category=="frame")
        {
            result.Add(Get(category,"classic_wood"));
            foreach(int level in LevelFrameCatalog.Levels)result.Add(Get(category,"level:"+level));
        }
        return result;
    }
}
