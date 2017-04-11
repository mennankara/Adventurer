using System;
using Adventurer.Game.Actors;
using Adventurer.Game.Exploration;
//using Adventurer.Game.Grid;
using Adventurer.Game.Quests;
using Adventurer.Util;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Game;

namespace Adventurer.Game.Events
{
    public enum ProfileType
    {
        Unknown,
        Rift,
        Bounty,
        Keywarden
    }

    public static class PluginEvents
    {
        public static ProfileType CurrentProfileType { get; internal set; }
        public static long WorldChangeTime { get; private set; }
        private static uint _lastUpdate;

        public static long TimeSinceWorldChange
        {
            get
            {
                if (WorldChangeTime == 0)
                {
                    return Int32.MaxValue;
                }
                return PluginTime.CurrentMillisecond - WorldChangeTime;
            }
        }

        public static void GameEvents_OnWorldChanged(object sender, EventArgs e)
        {
            if (!Adventurer.IsAdventurerTagRunning())
            {
                Logger.Debug("[BotEvents] Reseting the grids.");
                ScenesStorage.Reset();
            }
            WorldChangeTime = PluginTime.CurrentMillisecond;
            Logger.Debug("[BotEvents] World has changed to WorldId: {0} LevelAreaSnoIdId: {1}", AdvDia.CurrentWorldId, AdvDia.CurrentLevelAreaId);
            EntryPortals.AddEntryPortal();
        }

        public static void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            ScenesStorage.Reset();
            //AdvDia.Update();
        }

        public static void OnBotStart(IBot bot)
        {
            Pulsator.OnPulse += Pulsator_OnPulse;
        }

        public static void OnBotStop(IBot bot)
        {
            Pulsator.OnPulse -= Pulsator_OnPulse;
            BountyStatistics.Report();
        }


        private static void Pulsator_OnPulse(object sender, EventArgs e)
        {            
            PulseUpdates();
        }

        public static void PulseUpdates()
        {
            var curFrame = ZetaDia.Memory.Executor.FrameCount;
            if (curFrame == _lastUpdate) return;
            _lastUpdate = curFrame;

            // Trinity uses adventurer scenestorage and grid base 
            ScenesStorage.Update();

            if (!Adventurer.IsAdventurerTagRunning()) return;
            ExplorationGrid.PulseSetVisited();
            BountyStatistics.Pulse();
        }





    }
}
