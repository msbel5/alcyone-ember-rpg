using EmberCrpg.Simulation.Bestiary;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Bestiary
{
    /// <summary>
    /// F29: the bestiary table is deterministic and complete — five base types with full dice,
    /// sprite role, hit material and a forge prompt; archetype rotations and the boss apex pick
    /// never drift; actor names round-trip back to their type.
    /// </summary>
    public sealed class BestiaryCatalogTests
    {
        [Test]
        public void All_FiveTypes_AreComplete()
        {
            Assert.That(WorldBestiaryCatalog.All.Count, Is.EqualTo(5));
            foreach (var entry in WorldBestiaryCatalog.All)
            {
                Assert.That(entry.Key, Is.Not.Empty);
                Assert.That(entry.DisplayPrefix, Does.EndWith(" of "), entry.Key);
                Assert.That(entry.SpriteRole, Does.StartWith("monster_"), entry.Key);
                Assert.That(entry.HitMaterial, Is.Not.Empty, entry.Key);
                // House style: the prompt is a descriptor BODY (the forge wraps it in the NPC
                // sprite style envelope) — no style/count/backdrop tokens of its own.
                Assert.That(entry.ForgePrompt, Is.Not.Empty, entry.Key);
                Assert.That(entry.ForgePrompt, Does.Not.Contain("sprite"), entry.Key);
                Assert.That(entry.Accuracy, Is.GreaterThan(0), entry.Key);
                Assert.That(entry.BaseDamage, Is.GreaterThan(0), entry.Key);
                Assert.That(entry.HealthMax, Is.GreaterThan(0), entry.Key);
            }
        }

        [Test]
        public void Archetypes_RotateTheirOwnTypes_Deterministically()
        {
            // Cave runs beasts, crypt runs the dead, ruin runs squatters.
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Mağara", 0).Key, Is.EqualTo(WorldBestiaryCatalog.WolfKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Mağara", 1).Key, Is.EqualTo(WorldBestiaryCatalog.SpiderKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Mağara", 2).Key, Is.EqualTo(WorldBestiaryCatalog.BanditKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Mağara", 3).Key, Is.EqualTo(WorldBestiaryCatalog.WolfKey));

            Assert.That(WorldBestiaryCatalog.EntryForSlot("Kripta", 0).Key, Is.EqualTo(WorldBestiaryCatalog.SkeletonKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Kripta", 1).Key, Is.EqualTo(WorldBestiaryCatalog.GhostKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Kripta", 2).Key, Is.EqualTo(WorldBestiaryCatalog.SpiderKey));

            // Every archetype rotation carries THREE types — the trio proof frame depends on it.
            Assert.That(WorldBestiaryCatalog.TypesFor("Mağara").Count, Is.EqualTo(3));
            Assert.That(WorldBestiaryCatalog.TypesFor("Kripta").Count, Is.EqualTo(3));
            Assert.That(WorldBestiaryCatalog.TypesFor("Harabe").Count, Is.EqualTo(3));

            Assert.That(WorldBestiaryCatalog.EntryForSlot("Harabe", 0).Key, Is.EqualTo(WorldBestiaryCatalog.BanditKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Harabe", 1).Key, Is.EqualTo(WorldBestiaryCatalog.SkeletonKey));
            Assert.That(WorldBestiaryCatalog.EntryForSlot("Harabe", 2).Key, Is.EqualTo(WorldBestiaryCatalog.SpiderKey));

            // Unknown archetypes fall back to the cave mix instead of throwing.
            Assert.That(WorldBestiaryCatalog.EntryForSlot("???", 0).Key, Is.EqualTo(WorldBestiaryCatalog.WolfKey));
        }

        [Test]
        public void BossApex_MatchesItsArchetype()
        {
            Assert.That(WorldBestiaryCatalog.ApexKeyFor("Mağara"), Is.EqualTo(WorldBestiaryCatalog.WolfKey));
            Assert.That(WorldBestiaryCatalog.ApexKeyFor("Kripta"), Is.EqualTo(WorldBestiaryCatalog.GhostKey));
            Assert.That(WorldBestiaryCatalog.ApexKeyFor("Harabe"), Is.EqualTo(WorldBestiaryCatalog.BanditKey));
        }

        [Test]
        public void ActorNames_RoundTripToTheirType_AndHitMaterial()
        {
            Assert.That(WorldBestiaryCatalog.FromActorName("Fen Wolf of Korvane").Key, Is.EqualTo(WorldBestiaryCatalog.WolfKey));
            Assert.That(WorldBestiaryCatalog.FromActorName("Bone Walker of Korvane").Key, Is.EqualTo(WorldBestiaryCatalog.SkeletonKey));
            Assert.That(WorldBestiaryCatalog.HitMaterialForName("Pit Spider of Korvane"), Is.EqualTo("chitin"));
            Assert.That(WorldBestiaryCatalog.HitMaterialForName("Grave Wisp of Korvane"), Is.EqualTo("wail"));
            // Non-bestiary targets thud as flesh; nulls never throw.
            Assert.That(WorldBestiaryCatalog.HitMaterialForName("Watch of Korvane I"), Is.EqualTo("flesh"));
            Assert.That(WorldBestiaryCatalog.HitMaterialForName(null), Is.EqualTo("flesh"));
            Assert.That(WorldBestiaryCatalog.IsBestiaryName("Warden of Korvane"), Is.False,
                "the boss keeps its Warden name; its apex type comes from the archetype, not the name");
        }
    }
}
