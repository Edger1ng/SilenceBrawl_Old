using System.Linq;

namespace Supercell.Laser.Logic.Home
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Quest;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Titan.DataStream;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Numerics;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using static System.Collections.Specialized.BitVector32;

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientHome
    {
        public const int DAILYOFFERS_COUNT = 10;

        public static readonly int[] GoldPacksPrice = new int[]
        {
            20, 50, 140, 280
        };

        public static readonly int[] GoldPacksAmount = new int[]
        {
            150, 400, 1200, 2600
        };

        [JsonProperty] public long HomeId;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int NameColorId;
        [JsonProperty] public int CharacterId;

        [JsonProperty] public List<OfferBundle> OfferBundles;

        [JsonProperty] public int TrophiesReward;
        [JsonProperty] public int TokenReward;
        [JsonProperty] public int StarTokenReward;
        [JsonProperty] public int StarPointsGained;
        [JsonProperty] public int PowerPlayTrophiesReward;

        [JsonProperty] public BigInteger BrawlPassProgress;
        [JsonProperty] public BigInteger PremiumPassProgress;
        [JsonProperty] public int BrawlPassTokens;
        [JsonProperty] public bool HasPremiumPass;
        [JsonProperty] public List<int> UnlockedEmotes;

        [JsonProperty] public int Experience;
        [JsonProperty] public int TokenDoublers;

        [JsonProperty] public int TrophyRoadProgress;
        [JsonProperty] public Quests Quests;
        [JsonProperty] public NotificationFactory NotificationFactory;
        [JsonProperty] public List<int> UnlockedSkins;
        [JsonProperty] public int[] SelectedSkins;
        [JsonProperty] public int PowerPlayGamesPlayed;
        [JsonProperty] public int PowerPlayScore;
        [JsonProperty] public int PowerPlayHighestScore;
        [JsonProperty] public int BattleTokens;
        [JsonProperty] public DateTime BattleTokensRefreshStart;
        [JsonProperty] public DateTime PremiumEndTime;
        [JsonProperty] public DateTime ChatBanEndTime;
        [JsonProperty] public DateTime BanEndTime;
        [JsonProperty] public List<long> ReportsIds;
        [JsonProperty] public bool BlockFriendRequests;
        [JsonProperty] public string IpAddress;
        [JsonProperty] public string Device;
        [JsonProperty] public List<string> OffersClaimed;
        [JsonProperty] public string Day;


        [JsonIgnore] public EventData[] Events;

        public PlayerThumbnailData Thumbnail => DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(ThumbnailId);
        public NameColorData NameColor => DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(NameColorId);
        public CharacterData Character => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);

        public HomeMode HomeMode;

        [JsonProperty] public DateTime LastVisitHomeTime;
        [JsonProperty] public DateTime LastRotateDate;

        [JsonIgnore] public bool ShouldUpdateDay;

        public ClientHome()
        {
            ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            NameColorId = GlobalId.CreateGlobalId(43, 0);
            CharacterId = GlobalId.CreateGlobalId(16, 0);
            SelectedSkins = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

            OfferBundles = new List<OfferBundle>();
            OffersClaimed = new List<string>();
            ReportsIds = new List<long>();
            UnlockedSkins = new List<int>();
            LastVisitHomeTime = DateTime.UnixEpoch;

            TrophyRoadProgress = 1;

            BrawlPassProgress = 1;
            PremiumPassProgress = 1;

            UnlockedEmotes = new List<int>();
            BattleTokens = 100;
            BattleTokensRefreshStart = new();
            if (NotificationFactory == null)
            {
                NotificationFactory = new NotificationFactory();
            }

        }

        public void HomeVisited()
        {
            
            RotateShopContent(DateTime.UtcNow, OfferBundles.Count == 0);
            LastVisitHomeTime = DateTime.UtcNow;
            //Quests = null;
            UpdateOfferBundles();

            string Today = LastVisitHomeTime.ToString("d");
            if (Today != Day)
            {
                Day = Today;
            }

            if (Quests == null && TrophyRoadProgress >= 11)
            {
                Quests = new Quests();
                Quests.AddRandomQuests(HomeMode.Avatar.Heroes, 6);
            }
        }

        /*public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
            StarPointsGained = 0;
            PowerPlayTrophiesReward = 0;
        }*/
        public int TimerMath(DateTime timer_start, DateTime timer_end)
        {
            {
                DateTime timer_now = DateTime.Now;
                if (timer_now > timer_start)
                {
                    if (timer_now < timer_end)
                    {
                        int time_sec = (int)(timer_end - timer_now).TotalSeconds;
                        return time_sec;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
        }
        public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            while (ShouldAddTokens())
            {
                BattleTokensRefreshStart = BattleTokensRefreshStart.AddMinutes(15);
                BattleTokens = Math.Min(1000, BattleTokens + 100);
                if (BattleTokens == 1000)
                {
                    BattleTokensRefreshStart = new();
                    break;
                }
            }
            RotateShopContent(DateTime.UtcNow, OfferBundles.Count == 0);

        }

        public int GetbattleTokensRefreshSeconds()
        {
            if (BattleTokensRefreshStart == new DateTime())
            {
                return -1;
            }
            return (int)BattleTokensRefreshStart.AddMinutes(30).Subtract(DateTime.UtcNow).TotalSeconds;
        }
        public bool ShouldAddTokens()
        {
            if (BattleTokensRefreshStart == new DateTime())
            {
                return false;
            }
            return GetbattleTokensRefreshSeconds() < 1;
        }

        public void PurchaseOffer(int index)
        {
            if (index < 0 || index >= OfferBundles.Count) return;

            OfferBundle bundle = OfferBundles[index];
            if (bundle.Purchased) return;

            if (bundle.Currency == 0)
            {
                if (!HomeMode.Avatar.UseDiamonds(bundle.Cost)) return;
            }
            else if (bundle.Currency == 1)
            {
                if (!HomeMode.Avatar.UseGold(bundle.Cost)) return;
            }
            else if (bundle.Currency == 3)
            {
                if (!HomeMode.Avatar.UseStarPoints(bundle.Cost)) return;
            }

            bundle.Purchased = true;

            if (bundle.Claim == "debug")
            {
                ;
            }
            else
            {
                OffersClaimed.Add(bundle.Claim);
            }

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            Random rand = new Random();

            foreach (Offer offer in bundle.Items)
            {
                if (offer.Type == ShopItem.BrawlBox || offer.Type == ShopItem.FreeBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(10);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.HeroPower)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(6);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.BigBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(12);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.MegaBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(11);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.Skin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Gems)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Coin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(7);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.GuaranteedHero)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(1);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.CoinDoubler)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(2);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.EmoteBundle)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    List<int> Emotes_All = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300 };
                    List<int> Emotes_Locked = Emotes_All.Except(UnlockedEmotes).OrderBy(x => Guid.NewGuid()).Take(3).ToList();;

                    foreach (int x in Emotes_Locked){
                        GatchaDrop reward = new GatchaDrop(11);
                        reward.Count = 1;
                        reward.PinGlobalId = 52000000 + x;
                        unit.AddDrop(reward);
                        UnlockedEmotes.Add(x);
                    }
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Emote)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.Count = 1;
                    reward.PinGlobalId = 52000155;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else
                {
                    // todo...
                }

                command.Execute(HomeMode);

                
            }
            UpdateOfferBundles();
            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            HomeMode.GameListener.SendMessage(message);
        }

        private void RotateShopContent(DateTime time, bool isNewAcc)
        {
            /*if (OfferBundles.Select(bundle => bundle.IsDailyDeals).ToArray().Length > 6)
            {
                OfferBundles.RemoveAll(bundle => bundle.IsDailyDeals);
            }*/
            bool IsUpdated = false;
            int offLen = OfferBundles.Count;
            OfferBundles.RemoveAll(offer => offer.EndTime <= time);
            foreach (OfferBundle o in OfferBundles)
            
            {
                //LastRotateDate = new DateTime();
                if (LastRotateDate < DateTime.UtcNow.Date)
                {
                    IsUpdated = true;
                    OfferBundles.RemoveAll(offer => offer.IsDailyDeals);
                    LastRotateDate = DateTime.UtcNow.Date;
                    UpdateDailyOfferBundles();
                    UpdateDailySkins();
                    PowerPlayGamesPlayed = 0;
                    ReportsIds = new List<long>();
                    if (Quests != null)
                    {
                        Quests.QuestList.RemoveAll(bundle => bundle.IsDailyQuest);
                        Quests.AddRandomQuests(HomeMode.Avatar.Heroes, Quests.QuestList.Count >= 18 ? 8 : 10);
                    }
                }
            }
            if (OfferBundles == null)
            {
                UpdateDailyOfferBundles();
                IsUpdated = true;
            }
            else if (OfferBundles.Count == 0)
            {
                UpdateDailyOfferBundles();
                IsUpdated = true;
            }
            if (IsUpdated)
            {
                LogicDayChangedCommand newday = new()
                {
                    Home = this
                };
                newday.Home.Events = Events;
                AvailableServerCommandMessage eventupdated = new()
                {
                    Command = newday,
                };
                HomeMode.GameListener.SendMessage(eventupdated);
            }
        }

        public void UpdateOfferBundles()
        {
            OfferBundles.RemoveAll(bundle => bundle.IsTrue);


            GenerateOffer(
                new DateTime(2024, 11, 18, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                6, 999, 135, ShopItem.Skin,
                0, 300, 0,
                "server_corsair", "Корсар Кольт", "offer_lny"
            );

            GenerateOffer(
                new DateTime(2024, 11, 18, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                6, 38, 135, ShopItem.GuaranteedHero,
                20000, 35000, 3,
                "starpoints_exclusive", "Вольт!", "offer_xmas"
            );

            GenerateOffer(
                new DateTime(2024, 11, 18, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                6, 999, 52, ShopItem.Skin,
                100000, 35000, 3,
                "starpoints_exclusiveshelly", "Шелли", "offer_xmas"
            );

            GenerateOffer(
                new DateTime(2024, 11, 22, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                6, 999, 68, ShopItem.Skin,
                0, 35000, 0,
                "600subs_day1", "День 1", "offer_xmas"
            );

            GenerateOffer(
                new DateTime(2024, 11, 23, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                10000, 999, 68, ShopItem.Coin,
                0, 35000, 0,
                "600subs_day2", "День 2", "offer_xmas"
            );

            GenerateOffer(
                new DateTime(2024, 11, 24, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                40, 999, 68, ShopItem.Gems,
                0, 35000, 0,
                "600subs_day3", "День 3", "offer_xmas"
            );

            GenerateOffer(
                new DateTime(2024, 11, 18, 0, 0, 0), new DateTime(2024, 11, 30, 10, 0, 0),
                6, 999, 172, ShopItem.Skin,
                149, 150, 0,
                "server_corsair34", "Злой Джин", "offer_lny"
            );


        }

        public void GenerateOffer(
            DateTime OfferStart,
            DateTime OfferEnd,
            int Count,
            int BrawlerID,
            int Extra,
            ShopItem Item,
            int Cost,
            int OldCost,
            int Currency,
            string Claim,
            string Title,
            string BGR
            )
        {


            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;

            if (OffersClaimed.Contains(bundle.Claim))
            {
                bundle.Purchased = true;
            }
            if (TimerMath(OfferStart, OfferEnd) == -1)
            {
                bundle.Purchased = true;
            }
            if (HomeMode.HasHeroUnlocked(16000000 + BrawlerID))
            {
                bundle.Purchased = true;
            }

            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);

            OfferBundles.Add(bundle);
        }

        private void UpdateDailySkins()
        {
            List<string> skins = new() { "Witch", "Rockstar", "Beach", "Pink", "Panda", "White", "Hair", "Gold", "Rudo", "Bandita", "Rey", "Knight", "Caveman", "Dragon", "Summer", "Summertime", "Pheonix", "Greaser", "GirlPrereg", "Box", "Santa", "Chef", "Boombox", "Wizard", "Reindeer", "GalElf", "Hat", "Footbull", "Popcorn", "Hanbok", "Cny", "Valentine", "WarsBox", "Nightwitch", "Cart", "Shiba", "GalBunny", "Ms", "GirlHotrod", "Maple", "RR", "Mecha", "MechaWhite", "MechaNight", "FootbullBlue", "Outlaw", "Hogrider", "BoosterDefault", "Shark", "HoleBlue", "BoxMoonFestival", "WizardRed", "Pirate", "GirlWitch", "KnightDark", "DragonDark", "DJ", "Wolf", "Brown", "Total", "Sally", "Leonard", "SantaRope", "Gift", "GT", "SniperDefaultAddonBee", "SniperLadyBug", "SniperLadyBugAddonBee", "Virus", "BoosterVirus", "HoleStreetNinja", "Gamer", "Valentines", "Koala", "BearKoala", "TurretDefault", "AgentP", "Football", "Arena", "Tanuki", "Horus", "ArenaPSG", "DarkBunny", "College", "TurretTanuki", "TotemDefault", "Bazaar", "RedDragon", "Constructor", "Hawaii", "Barbking", "Trader", "StationSummer", "Silver", "SniperMonster", "BombMonster", "SniperMonsterAddonBee", "Bank", "Retro", "Ranger", "Tracksuit", "Knight", "RetroAddon", "Mask", "GiftShop", "Atomic" };
            List<int> skis = new();
            List<int> starss = new();
            foreach (Hero h in HomeMode.Avatar.Heroes)
            {
                CharacterData c = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(h.CharacterId);
                string cn = c.Name;
                foreach (string name in skins)
                {
                    SkinData s = DataTables.Get(DataType.Skin).GetData<SkinData>(cn + name);
                    if (s != null)
                    {
                        if (UnlockedSkins.Contains(s.GetGlobalId())) continue;
                        if (s.Name == "RocketGirlRanger") continue;
                        if (s.Name == "PowerLevelerKnight") continue;
                        if (s.Name == "BlowerTrader") continue;
                        if (s.CostLegendaryTrophies > 1)
                        {
                            starss.Add(s.GetGlobalId());
                            continue;
                        }
                        if (!s.Name.EndsWith("Gold"))
                            skis.Add(s.GetGlobalId());
                        else
                        {
                            string sss = s.Name.Replace("Gold", "Silver");
                            SkinData sc = DataTables.Get(DataType.Skin).GetData<SkinData>(sss);
                            if (sc == null)
                            {
                                skis.Add(s.GetGlobalId());
                                continue;
                            }
                            if (UnlockedSkins.Contains(sc.GetGlobalId()))
                            {
                                skis.Add(sc.GetGlobalId());
                            }
                        }
                    }
                }
            }


            Random random = new Random();
            int[] selectedElements = new int[Math.Min(skis.Count, 8)];
            for (int i = 0; i < Math.Min(skis.Count, 8); i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = random.Next(0, skis.Count);
                } while (selectedElements.Contains(skis[randomIndex]));

                selectedElements[i] = skis[randomIndex];
            }

            foreach (int bbbbbb in selectedElements)
            {
                SkinData skin = DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(bbbbbb);
                OfferBundle bundle = new OfferBundle();
                bundle.IsDailyDeals = false;
                bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
                if (skin.CostGems > 0)
                {
                    bundle.Currency = 0;
                    bundle.Cost = (skin.CostGems - 1);
                }
                else if (skin.CostCoins > 0)
                {
                    bundle.Currency = 1;
                    bundle.Cost = (skin.CostCoins);
                }
                else
                {
                    continue;
                }

                Offer offer = new Offer(ShopItem.Skin, 1);
                offer.SkinDataId = GlobalId.GetInstanceId(bbbbbb);
                bundle.Items.Add(offer);
                OfferBundles.Add(bundle);
            }
            int[] selectedStElements = new int[Math.Min(starss.Count, 3)];
            for (int i = 0; i < Math.Min(starss.Count, 3); i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = random.Next(0, starss.Count);
                } while (selectedStElements.Contains(starss[randomIndex]));

                selectedStElements[i] = starss[randomIndex];
            }
            foreach (int bbbbbb in selectedStElements)
            {
                SkinData skin = DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(bbbbbb);
                OfferBundle bundle = new OfferBundle();
                bundle.Currency = 3;
                bundle.IsDailyDeals = false;
                bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
                bundle.Cost = skin.CostLegendaryTrophies;

                Offer offer = new Offer(ShopItem.Skin, 1);
                offer.SkinDataId = GlobalId.GetInstanceId(bbbbbb);
                bundle.Items.Add(offer);
                OfferBundles.Add(bundle);
            }
        }

        private void UpdateDailyOfferBundles()
        {
            OfferBundles = new List<OfferBundle>();
            OfferBundles.Add(GenerateDailyGift());

            List<Hero> unlockedHeroes = HomeMode.Avatar.Heroes;
            List<Hero> PossibleHeroes = new();
            foreach (Hero h in unlockedHeroes)
            {
                if (h.PowerLevel == 8) continue;
                if (h.PowerPoints >= 1410) continue;
                PossibleHeroes.Add(h);
            }
            Random random = new Random();
            bool shouldPowerPoints = true;
            bool hasMG = false;
            int offcount = 0;
            for (int i = 1; i < DAILYOFFERS_COUNT; i++)
            {
                if (PossibleHeroes.Count == 0) break;
                offcount++;
                if (!hasMG && (random.Next(0, 100) > 33))
                {
                    i++;
                    offcount++;
                    hasMG = true;
                    OfferBundle a = GenerateDailyOffer(false, null);
                    if (a != null)
                    {
                        OfferBundles.Add(a);
                    }
                }
                int inds = random.Next(0, PossibleHeroes.Count);

                Hero brawler = PossibleHeroes[inds];
                PossibleHeroes.Remove(PossibleHeroes[inds]);
                OfferBundle dailyOffer = GenerateDailyOffer(shouldPowerPoints, brawler);
                if (dailyOffer != null)
                {
                    if (!shouldPowerPoints) shouldPowerPoints = dailyOffer.Items[0].Type != ShopItem.HeroPower;
                    OfferBundles.Add(dailyOffer);
                }
            }
            if (offcount < 5 && !hasMG)
            {
                OfferBundle dailyOffer = GenerateDailyOffer(false, null);
                if (dailyOffer != null)
                {
                    OfferBundles.Add(dailyOffer);
                }
            }
        }

        private OfferBundle GenerateDailyGift()
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = true;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
            bundle.Cost = 0;
            
            Offer offer1 = new Offer(ShopItem.BrawlBox, 1);
            bundle.Items.Add(offer1);

            return bundle;
        }

        private OfferBundle GenerateDailyOffer(bool shouldPowerPoints, Hero brawler)
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = true;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)

            Random random = new Random();
            int type = shouldPowerPoints ? 0 : 1; // getting a type

            switch (type)
            {
                case 0: // Power points
                    
                    //for (int i = 0; i < Math.Min(PossibleHeroes.Count, 5))
                    

                    int count = random.Next(15, 100) + 1;
                    Offer offer = new Offer(ShopItem.HeroPower, count, brawler.CharacterId);

                    bundle.Items.Add(offer);
                    bundle.Cost = count * 2;
                    bundle.Currency = 1;

                    break;
                case 1: // mega box
                    Offer megaBoxOffer = new Offer(ShopItem.MegaBox, 1);
                    bundle.Items.Add(megaBoxOffer);
                    bundle.Cost = 40;
                    bundle.OldCost = 80;
                    bundle.Currency = 0;
                    break;
            }

            return bundle;
        }
        public void LogicDailyData(ByteStream encoder, DateTime utcNow)
        {

            encoder.WriteVInt(utcNow.Year * 1000 + utcNow.DayOfYear); // 0x78d4b8
            encoder.WriteVInt(utcNow.Hour * 3600 + utcNow.Minute * 60 + utcNow.Second); // 0x78d4cc
            encoder.WriteVInt(HomeMode.Avatar.Trophies); // 0x78d4e0
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // 0x78d4f4
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // highest trophy again?
            encoder.WriteVInt(TrophyRoadProgress);
            encoder.WriteVInt(Experience + 1909); // experience

            ByteStreamHelper.WriteDataReference(encoder, Thumbnail);
            ByteStreamHelper.WriteDataReference(encoder, NameColorId);

            encoder.WriteVInt(18); // Played game modes
            for (int i = 0; i < 18; i++)
            {
                encoder.WriteVInt(i);
            }

            encoder.WriteVInt(39); // Selected Skins Dictionary
            for (int i = 0; i < 39; i++)
            {
                encoder.WriteVInt(29);
                try
                {
                    encoder.WriteVInt(SelectedSkins[i]);
                }
                catch
                {
                    encoder.WriteVInt(0);
                    SelectedSkins = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                }
            }


            encoder.WriteVInt(UnlockedSkins.Count); // Played game modes
            foreach (int s in UnlockedSkins)
            {
                ByteStreamHelper.WriteDataReference(encoder, s);
            }

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // 122
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(TokenDoublers);
            DateTime targetDateTime = new DateTime(2024, 12, 1, 9, 30, 0);
            TimeSpan timeDifference = targetDateTime - utcNow;
            int secondsRemaining = (int)timeDifference.TotalSeconds;
            encoder.WriteVInt(secondsRemaining); // SESOIN TIME
            encoder.WriteVInt(0);

            DateTime BPtargetDateTime = new DateTime(2024, 12, 1, 10, 0, 0);
            TimeSpan BPtimeDifference = BPtargetDateTime - utcNow;
            int BPsecondsRemaining = (int)BPtimeDifference.TotalSeconds;
            encoder.WriteVInt(BPsecondsRemaining); // BP TIME

            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(true);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(2);
            encoder.WriteVInt(2);
            encoder.WriteVInt(2);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteVInt(OfferBundles.Count); // Shop offers at 0x78e0c4
            foreach (OfferBundle offerBundle in OfferBundles)
            {
                offerBundle.Encode(encoder);
            }

            encoder.WriteVInt(0);

            encoder.WriteVInt(BattleTokens); // 0x78e228
            encoder.WriteVInt(GetbattleTokensRefreshSeconds()); // 0x78e23c
            encoder.WriteVInt(0); // 0x78e250
            encoder.WriteVInt(0); // 0x78e3a4
            encoder.WriteVInt(0); // 0x78e3a4

            ByteStreamHelper.WriteDataReference(encoder, Character);

            encoder.WriteString("RU");
            encoder.WriteString("SilenceBrawl");

            encoder.WriteVInt(6);
            {
                encoder.WriteInt(3);
                encoder.WriteInt(TokenReward); // tokens

                encoder.WriteInt(4);
                encoder.WriteInt(TrophiesReward); // trophies

                encoder.WriteInt(8);
                encoder.WriteInt(StarPointsGained); // trophies

                encoder.WriteInt(7);
                encoder.WriteInt(HomeMode.Avatar.DoNotDisturb ? 1 : 0); // trophies

                encoder.WriteInt(9);
                encoder.WriteInt(1); // trophies

                encoder.WriteInt(10);
                encoder.WriteInt(PowerPlayTrophiesReward); // trophies

            }

            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
            StarPointsGained = 0;
            PowerPlayTrophiesReward = 0;

            encoder.WriteVInt(0); // array

            encoder.WriteVInt(1); // BrawlPassSeasonData
            {
                encoder.WriteVInt(1);
                encoder.WriteVInt(BrawlPassTokens);
                //encoder.WriteVInt(PremiumPassProgress);
                encoder.WriteBoolean(HasPremiumPass);
                encoder.WriteVInt(0);

                if (encoder.WriteBoolean(true)) // Track 9
                {
                    encoder.WriteLongLong128(PremiumPassProgress);
                }
                if (encoder.WriteBoolean(true)) // Track 10
                {
                    encoder.WriteLongLong128(BrawlPassProgress);
                }
            }

            encoder.WriteVInt(1);
            {
                encoder.WriteVInt(2);
                encoder.WriteVInt(PowerPlayScore);
            }

            if (Quests != null)
            {
                encoder.WriteBoolean(true);
                Quests.Encode(encoder);
            }
            else
            {
                encoder.WriteBoolean(true);
                encoder.WriteVInt(0);
            }

            encoder.WriteBoolean(true);

            encoder.WriteVInt(UnlockedEmotes.Count);
            foreach (int i in UnlockedEmotes)
            {
                encoder.WriteVInt(52);
                encoder.WriteVInt(i);
                encoder.WriteVInt(1);
                encoder.WriteVInt(1);
                encoder.WriteVInt(1);
            }
        }

        public void LogicConfData(ByteStream encoder, DateTime utcNow)
        {
            encoder.WriteVInt(utcNow.Year * 1000 + utcNow.DayOfYear);
            encoder.WriteVInt(100);
            encoder.WriteVInt(10);
            encoder.WriteVInt(30);
            encoder.WriteVInt(3);
            encoder.WriteVInt(80);
            encoder.WriteVInt(10);
            encoder.WriteVInt(40);
            encoder.WriteVInt(1000);
            encoder.WriteVInt(550);
            encoder.WriteVInt(0);
            encoder.WriteVInt(999900);

            encoder.WriteVInt(0); // Array

            encoder.WriteVInt(9);
            for (int i = 1; i <= 9; i++)
                encoder.WriteVInt(i);

            encoder.WriteVInt(Events.Length);
            foreach (EventData data in Events)
            {
                data.IsSecondary = false;
                data.Encode(encoder);
            }

            encoder.WriteVInt(Events.Length);
            foreach (EventData data in Events)
            {
                data.IsSecondary = true;
                data.EndTime.AddSeconds((int)(data.EndTime - DateTime.Now).TotalSeconds);
                data.Encode(encoder);
            }

            encoder.WriteVInt(8);
            {
                encoder.WriteVInt(20);
                encoder.WriteVInt(35);
                encoder.WriteVInt(75);
                encoder.WriteVInt(140);
                encoder.WriteVInt(290);
                encoder.WriteVInt(480);
                encoder.WriteVInt(800);
                encoder.WriteVInt(1250);
            }

            encoder.WriteVInt(8);
            {
                encoder.WriteVInt(1);
                encoder.WriteVInt(2);
                encoder.WriteVInt(3);
                encoder.WriteVInt(4);
                encoder.WriteVInt(5);
                encoder.WriteVInt(10);
                encoder.WriteVInt(15);
                encoder.WriteVInt(20);
            }

            encoder.WriteVInt(3);
            {
                encoder.WriteVInt(10);
                encoder.WriteVInt(30);
                encoder.WriteVInt(80);
            }

            encoder.WriteVInt(3);
            {
                encoder.WriteVInt(6);
                encoder.WriteVInt(20);
                encoder.WriteVInt(60);
            }

            ByteStreamHelper.WriteIntList(encoder, GoldPacksPrice);
            ByteStreamHelper.WriteIntList(encoder, GoldPacksAmount);

            encoder.WriteVInt(2);
            encoder.WriteVInt(200);
            encoder.WriteVInt(20);

            encoder.WriteVInt(8640);
            encoder.WriteVInt(10);
            encoder.WriteVInt(5);

            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);

            encoder.WriteVInt(50);
            encoder.WriteVInt(604800);

            encoder.WriteBoolean(true);

            encoder.WriteVInt(0); // Array

            encoder.WriteVInt(2); // IntValueEntries
            {
                encoder.WriteInt(1);
                encoder.WriteInt(4100001); // theme

                encoder.WriteInt(46);
                encoder.WriteInt(1);
            }
        }

        public void Encode(ByteStream encoder)
        {
            DateTime utcNow = DateTime.UtcNow;

            LogicDailyData(encoder, utcNow);
            LogicConfData(encoder, utcNow);

            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteLong(HomeId);
            NotificationFactory.Encode(encoder);
            /*encoder.WriteVInt(1);
            {
                encoder.WriteVInt(83);
                encoder.WriteInt(0);
                encoder.WriteBoolean(false);
                encoder.WriteInt(0);
                encoder.WriteString(null);
                encoder.WriteInt(0);
                encoder.WriteStringReference("");
                encoder.WriteInt(0);
                encoder.WriteStringReference("");
                encoder.WriteInt(0);
                encoder.WriteStringReference("");
                encoder.WriteStringReference("");
                encoder.WriteStringReference("");
                encoder.WriteStringReference("");
                encoder.WriteVInt(0);
            }*/

            encoder.WriteVInt(0);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
        }
    }
}
