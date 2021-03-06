using Zeta.Common;

namespace Adventurer.Game.Exploration.SceneMapping
{
    public interface IWorldRegion
    {
        bool Contains(Vector3 position);

        IWorldRegion Offset(Vector2 min);

        CombineType CombineType { get; }
    }
}