using System;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    public interface IFactionDecayEventPolicy
    {
        bool ShouldEmit(FactionReputation before, FactionReputation after, FactionDecayConfig config);
    }

    public sealed class MeaningfulFactionDecayEventPolicy : IFactionDecayEventPolicy
    {
        public const int DefaultLargeStepThreshold = 10;

        private readonly int _largeStepThreshold;

        public MeaningfulFactionDecayEventPolicy(int largeStepThreshold = DefaultLargeStepThreshold)
        {
            if (largeStepThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(largeStepThreshold));

            _largeStepThreshold = largeStepThreshold;
        }

        public bool ShouldEmit(FactionReputation before, FactionReputation after, FactionDecayConfig config)
        {
            if (before.Equals(after))
                return false;

            if (before.ToRelationKind() != after.ToRelationKind())
                return true;

            if (Math.Sign(before.Value) != Math.Sign(after.Value))
                return true;

            return Math.Abs(after.Value - before.Value) >= _largeStepThreshold;
        }
    }
}
