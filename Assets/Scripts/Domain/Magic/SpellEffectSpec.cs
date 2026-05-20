using System;

// Design note:
// SpellEffectSpec describes a single deterministic effect inside a spell definition.
// Inputs: effect kind, magnitude, and duration in simulation ticks (0 = instantaneous).
// Outputs: validated immutable spec snapshot; resolution lives in later sprints.
// Bible reference: MASTER_MECHANICS_BIBLE.md §14-§15 (cost components, effect parameters),
// EMBER_VISION_BIBLE.md §3 Layer 3 (gameplay mechanics, Unity-free Domain).
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Immutable per-effect descriptor inside a spell definition.</summary>
    public readonly struct SpellEffectSpec
    {
        public SpellEffectSpec(SpellEffectCode kind, int magnitude, int durationTicks)
        {
            if (kind == SpellEffectCode.None)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Spell effects must specify a real kind.");
            if (magnitude < 0)
                throw new ArgumentOutOfRangeException(nameof(magnitude), magnitude, "Magnitude must be zero or positive.");
            if (durationTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(durationTicks), durationTicks, "Duration ticks must be zero or positive.");

            Kind = kind;
            Magnitude = magnitude;
            DurationTicks = durationTicks;
        }

        public SpellEffectCode Kind { get; }
        public int Magnitude { get; }
        public int DurationTicks { get; }
        public bool IsInstantaneous => DurationTicks == 0;
    }
}
