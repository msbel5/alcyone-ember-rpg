using EmberCrpg.Domain.Forge;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class UnityResourceProbe : IResourceProbe
    {
        public long AvailableVideoMemoryMb()
        {
            return SystemInfo.graphicsMemorySize;
        }

        public long AvailableSystemMemoryMb()
        {
            return SystemInfo.systemMemorySize;
        }
    }
}
