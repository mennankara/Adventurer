using Adventurer.Coroutines.CommonSubroutines;
using Adventurer.Game.Quests;

namespace Adventurer.Coroutines.BountyCoroutines.Subroutines
{
    public interface IBountySubroutine : ISubroutine
    {
        BountyData BountyData { get; }
    }
}
