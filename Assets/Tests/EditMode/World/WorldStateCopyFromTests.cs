using System.Reflection;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// ARCH-12: WorldState.CopyFrom (the reflection-free save/load restore) must mirror EVERY
    /// public field. A field added to WorldState without being added to CopyFrom would be silently
    /// dropped on restore; this reflection walk fails if any public instance field is missed.
    /// </summary>
    public sealed class WorldStateCopyFromTests
    {
        [Test]
        public void CopyFrom_MirrorsEveryPublicField()
        {
            // Populated source: the factory fills the reference fields (stores/lists) + Time = 480.
            var src = new WorldFactory().Create(roomSeed: 1);

            // Sentinel the scalar fields the factory may leave at default, so a missed copy is detectable
            // (for a field both sides leave at default, a miss is harmless and undetectable by design).
            src.RoomSeed = 99;
            src.CurrentRoomId = 11; src.PlayerRoomId = 12; src.TalkerRoomId = 13; src.MerchantRoomId = 14;
            src.GuardRoomId = 15; src.EnemyRoomId = 16; src.PickupRoomId = 17;
            src.DoorOpen = true; src.GuardDoorAccessGranted = true; src.GuardWarningCount = 5;
            src.EncounterActive = true; src.LastNarrative = "sentinel-narrative";

            var dst = new WorldState();
            dst.CopyFrom(src);

            foreach (var f in typeof(WorldState).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(f.GetValue(dst), Is.EqualTo(f.GetValue(src)),
                    $"CopyFrom did not mirror public field '{f.Name}'");
            }

            // SOUL-01: explicit guard on the four production-economy stores now homed on the world
            // root. The reflection walk above already covers them, but pin them by reference so a
            // future CopyFrom edit that drops one of these fails with an unambiguous message rather
            // than a generic field-name mismatch.
            Assert.That(dst.Plants, Is.SameAs(src.Plants), "CopyFrom did not mirror Plants");
            Assert.That(dst.Soils, Is.SameAs(src.Soils), "CopyFrom did not mirror Soils");
            Assert.That(dst.Jobs, Is.SameAs(src.Jobs), "CopyFrom did not mirror Jobs");
            Assert.That(dst.Worksites, Is.SameAs(src.Worksites), "CopyFrom did not mirror Worksites");
        }

        [Test]
        public void CopyFrom_NullSource_IsNoOp()
        {
            var w = new WorldFactory().Create(roomSeed: 1);
            var beforeTime = w.Time;
            var beforeActors = w.Actors;
            w.CopyFrom(null);
            Assert.That(w.Time, Is.EqualTo(beforeTime));
            Assert.That(w.Actors, Is.SameAs(beforeActors));
        }
    }
}
