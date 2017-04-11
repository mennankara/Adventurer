using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventurer.Coroutines
{
    public interface ICoroutine
    {
        Task<bool> GetCoroutine();
        Guid Id { get; }
        void Reset();
    }
}

