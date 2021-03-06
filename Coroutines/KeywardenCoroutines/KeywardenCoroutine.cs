﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adventurer.Coroutines.BountyCoroutines.Subroutines;
using Adventurer.Game.Actors;
using Adventurer.Game.Combat;
using Adventurer.Game.Exploration;
using Adventurer.Game.Rift;
using Adventurer.Settings;
using Adventurer.Util;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile.Common;
using Zeta.Common;
using Zeta.Common.Helpers;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Logger = Adventurer.Util.Logger;

namespace Adventurer.Coroutines.KeywardenCoroutines
{
    public class KeywardenCoroutine : IDisposable
    {
        private readonly KeywardenData _keywardenData;
        private Vector3 _keywardenLocation = Vector3.Zero;
        private HashSet<int> _levelAreaIds;
        private WaitCoroutine _waitCoroutine;
        private DateTime _markerCooldownUntil = DateTime.MinValue;

        private enum States
        {
            NotStarted,
            TakingWaypoint,
            Searching,
            Moving,
            Waiting,
            Completed,
            Failed
        }

        private States _state;
        private States State
        {
            get { return _state; }
            set
            {
                if (_state == value) return;

                switch (value)
                {
                    case States.NotStarted:
                        break;
                    default:
                        Logger.Debug("[Keywarden] " + value);
                        break;
                }
                _state = value;
            }
        }

        public KeywardenCoroutine(KeywardenData keywardenData)
        {
            _keywardenData = keywardenData;
            _levelAreaIds = new HashSet<int> { _keywardenData.LevelAreaId };
        }

        public async Task<bool> GetCoroutine()
        {
            switch (State)
            {
                case States.NotStarted:
                    return NotStarted();
                case States.TakingWaypoint:
                    return await TakingWaypoint();
                case States.Searching:
                    return await Searching();
                case States.Moving:
                    return await Moving();
                case States.Waiting:
                    return await Waiting();
                case States.Completed:
                    return await Completed();
                case States.Failed:
                    return await Failed();
            }
            return false;
        }

        private bool NotStarted()
        {
            DisablePulse();
            _keywardenLocation = Vector3.Zero;

            if (!_keywardenData.IsAlive)
            {
                State = States.Completed;
                return false;
            }
            TargetingHelper.TurnCombatOn();
            Logger.Info("[Keywarden] Lets go find da guy with da machina, shall we?");
            State = AdvDia.CurrentLevelAreaId == _keywardenData.LevelAreaId ? States.Searching : States.TakingWaypoint;
            return false;
        }

        private async Task<bool> TakingWaypoint()
        {
            DisablePulse();
            if (!await WaypointCoroutine.UseWaypoint(_keywardenData.WaypointNumber)) return false;
            State = States.Searching;
            return false;
        }

        private async Task<bool> Searching()
        {
            if (!_keywardenData.IsAlive)
            {
                State = States.Waiting;
                return false;
            }
            EnablePulse();

            if (_keywardenLocation != Vector3.Zero && DateTime.UtcNow > MoveCooldownUntil)
            {
                State = States.Moving;
                Logger.Info("[Keywarden] It's clobberin time!");
                return false;
            }
            
            if (!await MoveToMarker())
                return false;

            if (!await ExplorationCoroutine.Explore(_levelAreaIds))
                return false;
            
            Logger.Error("[Keywarden] Oh shit, that guy is nowhere to be found.");
            ScenesStorage.ResetVisited();
            State = States.Searching;
            return false;
        }

        private async Task<bool> MoveToMarker()
        {
            if (_markerCooldownUntil > DateTime.UtcNow)
                return true;

            var marker = GetKeywardenMarker();
            if (marker == null)
                return true;

            if (marker.Position.Distance(AdvDia.MyPosition) < 20f)
            {
                Logger.Info("[Keywarden] Finished Following marker");
                return true;
            }

            if (_markerCoroutine == null)
            {
                Logger.Info("[Keywarden] Following a keywarden marker, lets see where it goes");
                _markerCoroutine = new MoveToMapMarkerCoroutine(-1, AdvDia.CurrentWorldId, marker.NameHash, 0, false);
            }

            if (!_markerCoroutine.IsDone)
            {
                if (!await _markerCoroutine.GetCoroutine())
                    return false;
            }

            if (_markerCoroutine.State == MoveToMapMarkerCoroutine.States.Failed)
            {
                _markerCooldownUntil = DateTime.UtcNow.Add(TimeSpan.FromMinutes(1));
                Logger.Info("[Keywarden] Looks like we can't find a path to the keywarden marker :(");
                return true;
            }

            Logger.Info("[Keywarden] Finished Following marker");
            return true;
        }

        private MoveToMapMarkerCoroutine _markerCoroutine;

        private async Task<bool> Moving()
        {
            EnablePulse();
            TargetingHelper.TurnCombatOn();

            if (!await NavigationCoroutine.MoveTo(_keywardenLocation, 15))
            {
                return false;
            }

            if (NavigationCoroutine.LastResult == CoroutineResult.Failure && (NavigationCoroutine.LastMoveResult == MoveResult.Failed || NavigationCoroutine.LastMoveResult == MoveResult.PathGenerationFailed))
            {
                var canClientPathTo = await AdvDia.DefaultNavigationProvider.CanFullyClientPathTo(_keywardenLocation);
                if (!canClientPathTo)
                {
                    State = States.Searching;
                    MoveCooldownUntil = DateTime.UtcNow.AddSeconds(10);
                    Logger.Debug("[Keywarden] Can't seem to get to the keywarden!");
                }
                return false;
            }

            if (_keywardenData.IsAlive)
            {
                _keywardenLocation = GetKeywardenLocation();
                if (_keywardenLocation == Vector3.Zero)
                {
                    State = States.Searching;
                }
            }
            else
            {
                Logger.Info("[Keywarden] Keywarden shish kebab!");
                State = States.Waiting;
            }
            return false;
        }

        public DateTime MoveCooldownUntil = DateTime.MinValue;

        private async Task<bool> Waiting()
        {
            DisablePulse();
            if (_waitCoroutine == null)
            {
                _waitCoroutine = new WaitCoroutine(5000);
            }
            if (!await _waitCoroutine.GetCoroutine()) return false;
            _waitCoroutine = null;
            State = States.Completed;
            return false;
        }


        private async Task<bool> Completed()
        {
            DisablePulse();
            return true;
        }

        private async Task<bool> Failed()
        {
            DisablePulse();
            return true;
        }

        private void Scans()
        {
            _keywardenLocation = GetKeywardenLocation();
            if (State == States.Searching)
            {
                PulseCheck();
                ZergCheck();
            }
        }

        private DateTime _lastPulseCheck = DateTime.MinValue;
        private void PulseCheck()
        {
            if (DateTime.UtcNow.Subtract(_lastPulseCheck).TotalSeconds < 5)
                return;

            _lastPulseCheck = DateTime.UtcNow;

            // marker check
        }

        private Vector3 GetKeywardenLocation()
        {
            if(_keywardenLocation!=Vector3.Zero) return _keywardenLocation;
            var keywarden = ZetaDia.Actors.GetActorsOfType<DiaUnit>(true).FirstOrDefault(a => a.IsFullyValid() && a.ActorSnoId == _keywardenData.KeywardenSNO);
            return (keywarden != null) ? keywarden.Position : Vector3.Zero;
        }
        
        public MinimapMarker GetKeywardenMarker()
        {
            var marker = ZetaDia.Minimap.Markers.CurrentWorldMarkers.FirstOrDefault(m => m.MinimapTextureSnoId == 81058 && m.IsPointOfInterest && _keyardenMarkerHashes.Contains(m.NameHash));
            if (marker != null)
            {
                //Logger.Info("Found Keywarden Marker Distance={0} NameHash={1}",
                //    marker.Position.Distance(AdvDia.MyPosition), marker.NameHash);

                return marker;
            }
            //A3 - Id = 2 MinimapTextureSnoId = 81058 NameHash = 1424459156 IsPointOfInterest = True IsPortalEntrance = False IsPortalExit = False IsWaypoint = False Location = x = "3987" y = "3668" z = "29"  Distance = 33
            //A1 - Id = 3 MinimapTextureSnoId = 81058 NameHash = 1928482775 IsPointOfInterest = True IsPortalEntrance = False IsPortalExit = False IsWaypoint = False Location = x = "1774" y = "772" z = "10"  Distance = 141
            //A4 - Id=4 MinimapTextureSnoId=81058 NameHash=-946519244 IsPointOfInterest=True IsPortalEntrance=False IsPortalExit=False IsWaypoint=False Location=x="3012" y="2139" z="-23"  Distance=426
            // Id = 5 MinimapTextureSnoId = 81058 NameHash = -38012987 IsPointOfInterest = True IsPortalEntrance = False IsPortalExit = False IsWaypoint = False Location = x = "2808" y = "4543" z = "112"  Distance = 279
            return null;
        }

        private readonly HashSet<int> _keyardenMarkerHashes = new HashSet<int>
        {
            1424459156,
            1928482775,
            -946519244,
            -38012987
        };

        private void ZergCheck()
        {
            if (PluginSettings.Current.KeywardenZergMode.HasValue && !PluginSettings.Current.KeywardenZergMode.Value)
            {
                return;
            }
            var corruptGrowthDetectionRadius = ZetaDia.Me.ActorClass == ActorClass.Barbarian ? 30 : 20;
            var combatState = false;

            if (_keywardenLocation != Vector3.Zero && _keywardenLocation.Distance(AdvDia.MyPosition) <= 50f)
            {
                TargetingHelper.TurnCombatOn();
                return;
            }

            if (!combatState && ZetaDia.Me.HitpointsCurrentPct <= 0.8f)
            {
                combatState = true;
            }

            if (!combatState && _keywardenData.Act == Act.A4)
            {
                if (ZetaDia.Actors.GetActorsOfType<DiaUnit>(true).Any(
                            a =>
                                a.IsFullyValid() && KeywardenDataFactory.A4CorruptionSNOs.Contains(a.ActorSnoId) &&
                                a.IsAlive & a.Position.Distance(AdvDia.MyPosition) <= corruptGrowthDetectionRadius))
                {
                    combatState = true;
                }
            }


            if (!combatState && ZetaDia.Actors.GetActorsOfType<DiaUnit>(true).Any(u => u.IsFullyValid() && u.IsAlive && KeywardenDataFactory.GoblinSNOs.Contains(u.ActorSnoId)))
            {
                combatState = true;
            }

            if (!combatState && ZetaDia.Actors.GetActorsOfType<DiaUnit>(true).Count(u => u.IsFullyValid() && u.IsHostile && u.IsAlive && u.Position.Distance(AdvDia.MyPosition) <= 15f) >= 4)
            {
                combatState = true;
            }

            if (combatState)
            {
                TargetingHelper.TurnCombatOn();
            }
            else
            {
                TargetingHelper.TurnCombatOff();
            }
        }

        #region OnPulse Implementation
        private readonly WaitTimer _pulseTimer = new WaitTimer(TimeSpan.FromMilliseconds(250));
        private bool _isPulsing;


        private void EnablePulse()
        {
            if (!_isPulsing)
            {
                Logger.Debug("[Rift] Registered to pulsator.");
                Pulsator.OnPulse += OnPulse;
                _isPulsing = true;
            }
        }

        private void DisablePulse()
        {
            if (_isPulsing)
            {
                Logger.Debug("[Rift] Unregistered from pulsator.");
                Pulsator.OnPulse -= OnPulse;
                _isPulsing = false;
            }
        }

        private void OnPulse(object sender, EventArgs e)
        {
            if (!Adventurer.GetCurrentTag().StartsWith("KeywardensTag"))
            {
                DisablePulse();
                return;
            }
            if (_pulseTimer.IsFinished)
            {
                _pulseTimer.Stop();
                Scans();
                _pulseTimer.Reset();
            }
        }

        #endregion

        public void Dispose()
        {
            DisablePulse();
        }
    }
}
