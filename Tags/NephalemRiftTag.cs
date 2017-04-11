using System.Diagnostics;
using System.Threading.Tasks;
using Adventurer.Coroutines.RiftCoroutines;
using Adventurer.Game.Combat;
using Adventurer.Game.Events;
using Adventurer.Game.Rift;
using Adventurer.Settings;
using Adventurer.Util;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Game.Internals;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace Adventurer.Tags
{
    [XmlElement("NephalemRift")]
    public class NephalemRiftTag : ProfileBehavior
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private RiftCoroutine _riftCoroutine;
        private bool _isDone;

        [XmlAttribute("riftCount")]
        public int RiftCount { get; set; }

        public override bool IsDone
        {
            get
            {
                return _isDone;
            }
        }

        public override void OnStart()
        {
            if (!Adventurer.Enabled)
            {
                Logger.Error("Plugin is not enabled. Please enable Adventurer and try again.");
                _isDone = true;
                return;
            }

            var riftOptions = new RiftCoroutine.RiftOptions
            {
                RiftCount = RiftCount > 0 ? RiftCount : PluginSettings.Current.RiftCount,
            };

            PluginEvents.CurrentProfileType = ProfileType.Rift;

            _stopwatch.Start();
            //AdvDia.Update(true);
            _riftCoroutine = new RiftCoroutine(RiftType.Nephalem, riftOptions);
        }



        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => Coroutine());
        }

        public async Task<bool> Coroutine()
        {
            if (_isDone)
            {
                return true;
            }

            PluginEvents.PulseUpdates();
            if (_riftCoroutine == null || await _riftCoroutine.GetCoroutine())
            {
                _isDone = true;
            }
            return true;
        }

        public override void OnDone()
        {
            Logger.Info("[Rift] It took {0} ms to finish the rift", _stopwatch.ElapsedMilliseconds);
            base.OnDone();
        }

        public override void ResetCachedDone(bool force = false)
        {
            _isDone = false;
            _riftCoroutine = null;
            base.ResetCachedDone(force);
        }
    }
}
