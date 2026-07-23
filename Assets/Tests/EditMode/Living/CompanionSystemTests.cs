using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// V3 YOLDAŞ — TDD first. Companions are STATE, not a new role: recruited civilians keep
    /// their identity (and their memories — the dialogue pipeline already recalls them), but
    /// follow the player, stand with them in danger, and leave when dismissed.
    /// </summary>
    public sealed class CompanionSystemTests
    {
        private static ActorRecord Actor(ulong id, string name, ActorRole role, GridPosition position)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(30, 30), new VitalStat(10, 10), new VitalStat(10, 10)),
                position, accuracy: 60, dodge: 10, armor: 0, baseDamage: 3);
        }

        private static WorldState World(out ActorRecord player, out ActorRecord friend)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            player = Actor(1, "Warden", ActorRole.Player, new GridPosition(5, 5));
            friend = Actor(2, "Fenn", ActorRole.Talker, new GridPosition(6, 5));
            world.Actors.Add(player);
            world.Actors.Add(friend);
            return world;
        }

        [Test]
        public void TryRecruit_NearbyCivilian_JoinsAndEmitsEvent()
        {
            var world = World(out _, out var friend);

            bool joined = CompanionService.TryRecruit(world, friend.Id);

            Assert.That(joined, Is.True);
            Assert.That(world.CompanionIds, Does.Contain(friend.Id.Value));
            Assert.That(world.Events.Events.Any(e =>
                e.Kind == WorldEventKind.ActorTalked && e.Reason.StartsWith("companion_joined")), Is.True,
                "recruitment is a story beat — it must be logged");
        }

        [Test]
        public void TryRecruit_BeyondReachOrOverCap_IsRefused()
        {
            var world = World(out _, out var friend);
            friend.MoveTo(new GridPosition(20, 20)); // out of recruiting reach
            Assert.That(CompanionService.TryRecruit(world, friend.Id), Is.False, "too far to ask");

            friend.MoveTo(new GridPosition(6, 5));
            for (ulong extra = 10; extra < 10 + CompanionService.MaxCompanions; extra++)
            {
                var filler = Actor(extra, $"F{extra}", ActorRole.Talker, new GridPosition(5, 6));
                world.Actors.Add(filler);
                Assert.That(CompanionService.TryRecruit(world, filler.Id), Is.True);
            }
            Assert.That(CompanionService.TryRecruit(world, friend.Id), Is.False, "the party is full");
        }

        [Test]
        public void TickFollow_CompanionLagsBehind_StepsTowardThePlayer()
        {
            var world = World(out var player, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            player.MoveTo(new GridPosition(15, 5)); // player walked off

            new CompanionSystem().TickFollow(world);

            Assert.That(friend.Position, Is.EqualTo(new GridPosition(7, 5)),
                "one Chebyshev step toward the player per tick");
        }

        [Test]
        public void TickFollow_CompanionAtHeel_HoldsPosition()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id); // adjacent (Chebyshev 1)

            new CompanionSystem().TickFollow(world);

            Assert.That(friend.Position, Is.EqualTo(new GridPosition(6, 5)), "no jitter at heel range");
        }

        [Test]
        public void TickGuard_EnemyBesideThePlayer_CompanionStrikesIt()
        {
            var world = World(out var player, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            var wolf = Actor(9, "Wolf", ActorRole.Enemy, new GridPosition(4, 5)); // beside the player
            world.Actors.Add(wolf);

            new CompanionSystem().TickGuard(world, new GameTime(60));

            Assert.That(world.Events.Events.Any(e =>
                e.Kind == WorldEventKind.CombatResolved && e.ActorId.Equals(friend.Id)), Is.True,
                "a companion does not watch the player bleed — it strikes the adjacent hostile");
        }

        [Test]
        public void TickFollow_CompanionDied_LeavesThePartyWithAFallenEvent()
        {
            // M2: death is a story beat, not a silent list entry — the party shrinks and the
            // log carries the loss.
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);
            friend.ApplyVitals(new ActorVitals(
                new VitalStat(0, friend.Vitals.Health.Max), friend.Vitals.Fatigue, friend.Vitals.Mana));

            new CompanionSystem().TickFollow(world);

            Assert.That(world.CompanionIds, Is.Empty, "the fallen leave the roster");
            Assert.That(world.Events.Events.Any(e => e.Reason.StartsWith("companion_fell")), Is.True);
        }

        [Test]
        public void TryDismiss_Companion_LeavesAndEmitsEvent()
        {
            var world = World(out _, out var friend);
            CompanionService.TryRecruit(world, friend.Id);

            Assert.That(CompanionService.TryDismiss(world, friend.Id), Is.True);
            Assert.That(world.CompanionIds, Is.Empty);
            Assert.That(world.Events.Events.Any(e => e.Reason.StartsWith("companion_left")), Is.True);
        }
    }
}
