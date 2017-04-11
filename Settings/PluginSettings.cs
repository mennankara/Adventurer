using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Adventurer.Game.Rift;
using Adventurer.Util;
using GreyMagic;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals;

namespace Adventurer.Settings
{

    [DataContract]
    public class PluginSettings
    {
        private static ConcurrentDictionary<int, PluginSettings> _settings = new ConcurrentDictionary<int, PluginSettings>();
        private AdventurerGems _gems;

        public static PluginSettings Current { get { return _settings.GetOrAdd(AdvDia.BattleNetHeroId, LoadCurrent()); } }
        [DataMember]
        public int HighestUnlockedRiftLevel { get; set; }
        [DataMember]
        public bool NormalRiftForXPShrine { get; set; }
        [DataMember]
        public int GreaterRiftLevel { get; set; }
        [DataMember]
        public bool GreaterRiftRunNephalem { get; set; }
        [DataMember]
        public int GreaterRiftGemUpgradeChance { get; set; }
        [DataMember]
        public bool GreaterRiftPrioritizeEquipedGems { get; set; }
        [DataMember]
        public bool BountyAct1 { get; set; }
        [DataMember]
        public bool BountyAct2 { get; set; }
        [DataMember]
        public bool BountyAct3 { get; set; }
        [DataMember]
        public bool BountyAct4 { get; set; }
        [DataMember]
        public bool BountyAct5 { get; set; }
        [DataMember]
        public bool BountyZerg { get; set; }
        [DataMember]
        public bool BountyPrioritizeBonusAct { get; set; }

        [DataMember]
        public bool? BountyMode0 { get; set; }
        [DataMember]
        public bool? BountyMode1 { get; set; }
        [DataMember]
        public bool? BountyMode2 { get; set; }
        [DataMember]
        public bool? BountyMode3 { get; set; }

        [DataMember]
        public bool NephalemRiftFullExplore { get; set; }

        [DataMember]
        public bool? KeywardenZergMode { get; set; }

        [DataMember]
        public bool? DebugLogging { get; set; }

        [DataMember]
        public bool UseEmpoweredRifts { get; set; }

        [DataMember]
        [DefaultValue(40)]
        public int EmpoweredRiftLevelLimit { get; set; }

        [DataMember]
        public int RiftCount { get; set; }

        [DataMember]
        public bool UseGemAutoLevel { get; set; }

        [DataMember]
        public string GreaterRiftLevelMax { get; set; }

        [DataMember]
        [DefaultValue(20)]
        public int GemAutoLevelReductionLimit { get; set; }

        [DataMember]
        public AdventurerGems Gems
        {
            get
            {
                if (_gems == null)
                {
                    _gems = new AdventurerGems();
                }
                var greaterRiftLevel = RiftData.GetGreaterRiftLevel();
                _gems.UpdateGems(greaterRiftLevel, GreaterRiftPrioritizeEquipedGems);
                return _gems;
            }
            set { _gems = value; }
        }

        public string GreaterRiftLevelRaw
        {
            get
            {
                switch (GreaterRiftLevel)
                {
                    case 0:
                        return "Max";
                    case -1:
                    case -2:
                    case -3:
                    case -4:
                    case -5:
                    case -6:
                    case -7:
                    case -8:
                    case -9:
                    case -10:
                        return "Max - " + (GreaterRiftLevel * -1);
                    default:
                        return GreaterRiftLevel.ToString();
                }
            }
            set
            {
                if (value == "Max")
                {
                    GreaterRiftLevel = 0;
                }
                else
                {
                    int greaterRiftLevel;
                    if (int.TryParse(value.Replace("Max - ", string.Empty), out greaterRiftLevel))
                    {
                        GreaterRiftLevel = greaterRiftLevel;
                    }
                    if (value.Contains("Max")) GreaterRiftLevel = GreaterRiftLevel * -1;
                }
            }
        }

        public List<AdventurerGem> GemUpgradePriority { get { return Gems.Gems; } }

        public PluginSettings() { }
        public PluginSettings(bool initializeDefaults)
        {
            if (!initializeDefaults) return;

            GreaterRiftLevel = 1;
            GreaterRiftRunNephalem = true;
            GreaterRiftGemUpgradeChance = 60;
            GreaterRiftPrioritizeEquipedGems = true;
            EmpoweredRiftLevelLimit = 60;
            GemAutoLevelReductionLimit = 20;
            BountyAct1 = true;
            BountyAct2 = true;
            BountyAct3 = true;
            BountyAct4 = true;
            BountyAct5 = true;
            BountyZerg = true;
            BountyMode0 = true;
            BountyMode1 = false;
            BountyMode2 = false;
            BountyMode3 = false;
            BountyPrioritizeBonusAct = true;
            NephalemRiftFullExplore = false;
        }

        private int highestUnlockedRiftLevel;

        public List<string> GreaterRiftLevels
        {
            get
            {
                var unlockedRiftLevel = 0;

                var result = SafeFrameLock.ExecuteWithinFrameLock(() =>
                {
                    unlockedRiftLevel = ZetaDia.Me.HighestUnlockedRiftLevel;

                }, true);

                if (!result.Success)
                {
                    //Logger.Error("[Settings][GreaterRiftLevels] " + result.Exception.Message);
                    unlockedRiftLevel = HighestUnlockedRiftLevel;
                }
                else
                {
                    HighestUnlockedRiftLevel = unlockedRiftLevel;
                }

                if (unlockedRiftLevel == 0)
                {
                    unlockedRiftLevel = 1;
                }

                var levels = new List<string>();
                for (var i = 1; i <= unlockedRiftLevel; i++)
                {
                    levels.Add(i.ToString());
                }

                var highest = unlockedRiftLevel;
                if (highest > 10) highest = 10;

                for (var i = highest - 1; i >= 0; i--)
                {
                    if (i == 0)
                    {
                        levels.Insert(0, "Max");
                    }
                    else
                    {
                        levels.Insert(0, "Max - " + i);
                    }
                }

                return levels;
            }
        }



        public void UpdateGemList()
        {
            if (_gems != null)
            {
                var greaterRiftLevel = RiftData.GetGreaterRiftLevel();
                _gems.UpdateGems(greaterRiftLevel, GreaterRiftPrioritizeEquipedGems);
            }
        }

        public List<int> GemUpgradeChances
        {
            get { return new List<int> { 100, 90, 80, 70, 60, 30, 15, 8, 4, 2, 1, 0 }; }
        }

        public List<string> RiftCounts
        {
            get { return new List<string> { "Infinity", "1", "5", "10", "20", "50" }; }
        }

        public string RiftCountSetting
        {
            get
            {
                return RiftCount <= 0 ? "Infinity" : RiftCount.ToString();
            }
            set
            {
                if (value == "Infinity")
                {
                    RiftCount = 0;
                }
                else
                {
                    int riftCount;
                    if (int.TryParse(value, out riftCount))
                    {
                        RiftCount = riftCount;
                    }
                }
            }
        }

        public static PluginSettings LoadCurrent()
        {
            var json = FileUtils.ReadFromTextFile(FileUtils.SettingsPath);
            if (!string.IsNullOrEmpty(json))
            {
                var current = JsonSerializer.Deserialize<PluginSettings>(json);
                if (current != null)
                {
                    if (!current.KeywardenZergMode.HasValue)
                        current.KeywardenZergMode = true;

                    var isMode0 = current.BountyMode0.HasValue && current.BountyMode0.Value;
                    var isMode1 = current.BountyMode1.HasValue && current.BountyMode1.Value;
                    var isMode2 = current.BountyMode2.HasValue && current.BountyMode2.Value;
                    var isMode3 = current.BountyMode3.HasValue && current.BountyMode3.Value;

                    var modes = (isMode0 ? 1 : 0) + (isMode1 ? 1 : 0) + (isMode2 ? 1 : 0) + (isMode3 ? 1 : 0);

                    if (modes > 1 || modes == 0)
                    {
                        current.BountyMode0 = true;
                        current.BountyMode1 = false;
                        current.BountyMode2 = false;
                        current.BountyMode3 = false;
                    }
                    else
                    {
                        if (!current.BountyMode0.HasValue)
                            current.BountyMode0 = false;
                        if (!current.BountyMode1.HasValue)
                            current.BountyMode1 = false;
                        if (!current.BountyMode2.HasValue)
                            current.BountyMode2 = false;
                        if (!current.BountyMode3.HasValue)
                            current.BountyMode3 = false;
                    }
                    return current;
                }
            }
            return new PluginSettings(true);
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            foreach (var p in GetType().GetProperties())
            {
                foreach (var dv in p.GetCustomAttributes(true).OfType<DefaultValueAttribute>())
                {
                    p.SetValue(this, dv.Value);
                }
            }
        }

        public void Save()
        {
            var result = JsonSerializer.Serialize(this);
            FileUtils.WriteToTextFile(FileUtils.SettingsPath, result);
            Logger.Verbose("FileUtils.SettingsPath {0}", FileUtils.SettingsPath);
            Logger.Info("Settings saved.");
        }
    }

}
