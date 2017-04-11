using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventurer.Game.Exploration
{
    public class SceneData : ISceneData
    {
        public int WorldDynamicId { get; set; }
        public List<IGroupNode> ExplorationNodes { get; set; }
    }
}

