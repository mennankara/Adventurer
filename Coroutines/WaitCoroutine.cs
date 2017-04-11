using System;
using System.Threading.Tasks;
using Adventurer.Coroutines.BountyCoroutines.Subroutines;
using Adventurer.Game.Combat;
using Adventurer.Game.Quests;
using Adventurer.Util;
using Zeta.Common.Helpers;

namespace Adventurer.Coroutines
{
    public class WaitCoroutine : IBountySubroutine
    {

        private readonly TimeSpan _waitTime;
        private bool _isDone;

        public bool IsDone
        {
            get { return _isDone; }
        }

        private WaitTimer _waitTimer;

        public WaitCoroutine(int milliSeconds)
        {
            _waitTime = TimeSpan.FromMilliseconds(milliSeconds);
        }

        public async Task<bool> GetCoroutine()
        {
            SafeZerg.Instance.DisableZerg();
            if (_waitTimer == null)
            {
                _waitTimer = new WaitTimer(_waitTime);
                _waitTimer.Reset();
                Logger.Debug("[Wait] Waiting for {0} seconds", _waitTime.TotalSeconds);
            }
            if (!_waitTimer.IsFinished) return false;
            _isDone = true;
            return true;
        }

        public void Reset()
        {
            _isDone = false;
            _waitTimer = null;
        }

        public void DisablePulse()
        {
        }

        public BountyData BountyData
        {
            get { return null; }
        }
    }
}
