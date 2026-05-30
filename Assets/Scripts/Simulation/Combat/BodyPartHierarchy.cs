using System;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Rng;

// Design note:
// BodyPartHierarchy keeps DFU-style weighted zones plus parent/damage/AC modifiers in pure simulation.
// Inputs: seeded RNG selection or collider-specified body part.
// Outputs: body-part node data for the deterministic RTWP damage pipeline.
// Bible reference: MASTER_MECHANICS_BIBLE.md §10 body-part weights, Sprint 4 Phase 2 hit abstraction.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Humanoid body hierarchy and weighted hit-location selector.</summary>
    public sealed class BodyPartHierarchy
    {
        private static readonly BodyPartNode[] Nodes =
        {
            new BodyPartNode(BodyPart.Head, BodyPart.Chest, 2, 12, 140),
            new BodyPartNode(BodyPart.RightArm, BodyPart.Chest, 3, 4, 85),
            new BodyPartNode(BodyPart.LeftArm, BodyPart.Chest, 3, 4, 85),
            new BodyPartNode(BodyPart.Chest, null, 4, 8, 110),
            new BodyPartNode(BodyPart.Hands, BodyPart.Chest, 4, 2, 75),
            new BodyPartNode(BodyPart.Legs, BodyPart.Chest, 3, 3, 90),
            new BodyPartNode(BodyPart.Feet, BodyPart.Legs, 1, 1, 70),
        };

        public BodyPartNode GetNode(BodyPart bodyPart)
        {
            for (var i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Part == bodyPart)
                    return Nodes[i];
            }

            throw new ArgumentOutOfRangeException(nameof(bodyPart), bodyPart, null);
        }

        public BodyPart Select(IDeterministicRng rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var total = 0;
            for (var i = 0; i < Nodes.Length; i++)
                total += Nodes[i].SelectionWeight;

            var roll = rng.NextInt(total);
            var cursor = 0;
            for (var i = 0; i < Nodes.Length; i++)
            {
                cursor += Nodes[i].SelectionWeight;
                if (roll < cursor)
                    return Nodes[i].Part;
            }

            return BodyPart.Chest;
        }
    }
}
