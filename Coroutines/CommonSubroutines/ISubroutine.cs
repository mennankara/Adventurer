using System.Threading.Tasks;

namespace Adventurer.Coroutines.CommonSubroutines
{
    public interface ISubroutine
    {
        bool IsDone { get; }
        Task<bool> GetCoroutine();
        void Reset();
        void DisablePulse();

    }
}