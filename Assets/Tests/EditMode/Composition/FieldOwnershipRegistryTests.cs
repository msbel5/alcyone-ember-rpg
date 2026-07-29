using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Living.Actions;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>REFORM #2 lint: every declared writer must be a REAL registered system, and
    /// the core mutable fields must have a declared ownership row at all.</summary>
    public sealed class FieldOwnershipRegistryTests
    {
        [Test]
        public void EveryDeclaredWriter_IsARealRegisteredSystem_AtItsDeclaredSlot()
        {
            // B03: the known-id set is DERIVED from the composition root — a hand-typed list
            // rotted into six ghosts and blessed the econ.trade@Daily:28 phantom writer.
            // Linting the FULL "id@Cadence:Order" triple is simultaneously the reverse lint:
            // a declared writer with no real system at that exact slot (or one whose cadence/
            // order drifted without a ledger update) fails here.
            var registered = DefaultRegistryFixture.CreateDefault().Ordered
                .Select(s => $"{s.Id}@{s.Cadence}:{s.Order}")
                .ToHashSet();
            var ghosts = FieldOwnershipRegistry.Writers
                .SelectMany(kv => kv.Value)
                .Distinct()
                .Where(w => !registered.Contains(w))
                .ToList();
            Assert.That(ghosts, Is.Empty,
                "ownership ledger declares writers with no real registered system at that slot: "
                + string.Join(", ", ghosts));
        }

        [Test]
        public void CoreMutableFields_HaveDeclaredOwnership()
        {
            foreach (var field in new[]
                { "Actor.Position", "Actor.Needs", "Actor.Vitals", "World.Stockpiles", "World.GuardPursuits",
                  "Actor.ActionState", "World.Reservations" }) // W32: the new single-writer rows are pinned
                Assert.That(FieldOwnershipRegistry.Writers.ContainsKey(field), Is.True,
                    field + " has no declared ownership - undeclared writers breed cadence conflicts");
        }

        [Test]
        public void MoveToAndApplyVitals_ProductionCallsites_MatchTheRepoRelativeGate()
        {
            AssertInventoryMatches(
                "Actor.Position/.MoveTo",
                Inventory(@"\.MoveTo\s*\("),
                FieldOwnershipRegistry.AllowedMoveToCallsites);
            AssertInventoryMatches(
                "Actor.Vitals/.ApplyVitals",
                Inventory(@"\.ApplyVitals\s*\("),
                FieldOwnershipRegistry.AllowedApplyVitalsCallsites);
        }

        [Test]
        public void NeedsAndStockpiles_ProductionMutations_MatchTheExplicitDebtGate()
        {
            AssertInventoryMatches(
                "Actor.Needs/.ApplyNeeds",
                Inventory(@"\.ApplyNeeds\s*\("),
                FieldOwnershipRegistry.AllowedApplyNeedsCallsites);
            AssertInventoryMatches(
                "World.Stockpiles/add-remove-assign",
                Inventory(
                    @"\b(?:_pile|pile|stockpile|stockpiles|larder|furnaceStock|stallStock|origin|destination)\??\.(?:Add|Remove)\s*\(",
                    @"\bcontext\.TerrainStockpile\.(?:Add|Remove)\s*\(",
                    @"\b(?:world|_world)\.Stockpiles(?:\s*\[[^\]]+\])?\.(?:Add|Remove)\s*\(",
                    @"\bFoodOperations\.FindPile\([^)]*\)\?\.Add\s*\(",
                    @"^\s*\?\.Add\(cropTag,\s*state\.CarriedUnits\);",
                    @"\bStockpiles\s*(?:\?\?=|=(?!=))"),
                FieldOwnershipRegistry.AllowedStockpileMutationCallsites);
        }

        [Test]
        public void ActorActionState_HasOneAuthoritativeProductionWriter_AndNoHiddenCallsites()
        {
            var actual = Inventory(
                @"\.ApplyActionState\s*\(",
                @"\bActionState\s*=(?!=)");
            AssertInventoryMatches(
                "Actor.ActionState/invoke-or-direct-assign",
                actual,
                FieldOwnershipRegistry.ActionStateCallsiteClassifications.Keys);

            var authoritative = actual.Where(identity =>
                    FieldOwnershipRegistry.ActionStateCallsiteClassifications[identity]
                    == FieldOwnershipRegistry.ActionStateAuthoritative)
                .ToArray();
            Assert.That(authoritative, Has.Length.EqualTo(1),
                "Actor.ActionState must derive exactly one authoritative writer from the complete inventory");
            Assert.That(authoritative.Single(), Is.EqualTo(
                    "Assets/Scripts/Simulation/Living/Actions/ActionAdvancer.cs:55::actor.ApplyActionState(next);"),
                "all live ActionState transitions must cross ActionAdvancer.TransitionTo");
        }

        [Test]
        public void CompanionCutover_ActiveActionHasOneMovementWriterAndLegacyStepsAreGone()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(60);
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            var pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", 1);
            world.Stockpiles.Add(pile);

            var player = Actor(1, "Warden", ActorRole.Player, new GridPosition(20, 0));
            var companion = Actor(2, "Fenn", ActorRole.Talker, new GridPosition(0, 0));
            world.Actors.Add(player);
            world.Actors.Add(companion);
            world.CompanionIds.Add(companion.Id);
            Assert.That(world.Reservations.TryReserve(
                1UL, "wheat", companion.Id.Value, 999, 1, out var reservationId), Is.True);
            companion.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.MoveToFood, new SiteId(1), ItemId.Empty,
                new ReservationId(reservationId), 60, ActionInterruptPolicy.Interruptible));

            var before = companion.Position;
            var lifecycle = new ActionLifecycleSystem(new ActionLogManager());
            lifecycle.Decide(world, new GameTime(60));
            lifecycle.Advance(world, new GameTime(60));
            var after = companion.Position;
            var production = DefaultRegistryFixture.CreateDefault().Ordered.ToArray();
            var actionSlot = production.Single(step => step.Id == "living.action_advance");

            Assert.Multiple(() =>
            {
                Assert.That(after.ChebyshevDistanceTo(before), Is.EqualTo(1),
                    "the active Eat advancer owns exactly one autonomous movement step");
                Assert.That(companion.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Eat),
                    "companion follow cannot replace or move an actor with an active need action");
                Assert.That(production.Any(step => step.Id == "living.companion_follow"), Is.False);
                Assert.That(production.Any(step => step.Id == "living.companion_guard"), Is.False);
                Assert.That(FieldOwnershipRegistry.PositionDebtAllowList,
                    Does.Not.Contain(
                        "Assets/Scripts/Simulation/Living/CompanionSystem.cs:99::companion.MoveTo(MovementService.StepToward(companion.Position, player.Position, world.NavView));"));
                Assert.That(FieldOwnershipRegistry.PositionActionSpineCallsites,
                    Does.Contain(
                        "Assets/Scripts/Simulation/Living/Actions/FollowPlayerAdvancer.cs:48::actor.MoveTo(movement.Position);"));
                Assert.That(actionSlot.Cadence, Is.EqualTo(TickCadence.PerTick));
                Assert.That(actionSlot.Order, Is.EqualTo(22));
                Assert.That(FieldOwnershipRegistry.Writers["Actor.Position"],
                    Does.Not.Contain("living.companion_follow@PerTick:21"));
                Assert.That(FieldOwnershipRegistry.Writers["Actor.Vitals"],
                    Does.Not.Contain("living.companion_guard@Hourly:42"));
            });
        }

        private static ActorRecord Actor(
            ulong id,
            string name,
            ActorRole role,
            GridPosition position)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                position, accuracy: 60, dodge: 10, armor: 0, baseDamage: 3);
        }

        private static void AssertInventoryMatches(
            string label,
            IReadOnlyList<string> actual,
            IEnumerable<string> allowed)
        {
            var expected = allowed.OrderBy(
                identity => identity, StringComparer.Ordinal).ToArray();
            var unregistered = actual.Except(
                expected, StringComparer.Ordinal).ToArray();
            var stale = expected.Except(
                actual, StringComparer.Ordinal).ToArray();
            TestContext.Progress.WriteLine($"INVENTORY {label} count={actual.Count}");
            foreach (var identity in actual)
                TestContext.Progress.WriteLine($"  {identity}");
            Assert.That(unregistered, Is.Empty,
                $"new unregistered {label} callsite(s): {string.Join(", ", unregistered)}");
            Assert.That(stale, Is.Empty,
                $"registered {label} callsite(s) no longer exist: {string.Join(", ", stale)}");
        }

        private static IReadOnlyList<string> Inventory(params string[] patterns)
        {
            var root = Path.GetFullPath(FindRepositoryRoot());
            var scripts = Path.GetFullPath(Path.Combine(root, "Assets", "Scripts"));
            ValidateInsideRoot(root, scripts);
            var expressions = patterns.Select(pattern => new Regex(
                pattern, RegexOptions.CultureInvariant)).ToArray();
            var rows = new List<string>();

            foreach (var file in Directory.EnumerateFiles(
                         scripts, "*.cs", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, Path.GetFullPath(file))
                    .Replace('\\', '/');
                ValidateRelative(relative);
                if (relative == "Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs")
                    continue; // the ledger contains source-line strings; it is not a writer
                var lines = File.ReadAllLines(file);
                for (var line = 0; line < lines.Length; line++)
                {
                    if (!expressions.Any(expression => expression.IsMatch(lines[line])))
                        continue;
                    rows.Add($"{relative}:{line + 1}::{lines[line].Trim()}");
                }
            }
            return rows.OrderBy(identity => identity, StringComparer.Ordinal).ToArray();
        }

        private static void ValidateInsideRoot(string root, string path)
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            ValidateRelative(relative);
        }

        private static void ValidateRelative(string relative)
        {
            if (Path.IsPathRooted(relative)
                || relative == ".."
                || relative.StartsWith("../", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"source inventory escaped repository root: {relative}");
        }

        private static string FindRepositoryRoot()
        {
            foreach (var seed in new[]
                     {
                         Directory.GetCurrentDirectory(),
                         TestContext.CurrentContext.TestDirectory,
                     })
            {
                var directory = new DirectoryInfo(seed);
                while (directory != null)
                {
                    if (Directory.Exists(Path.Combine(directory.FullName, "Assets", "Scripts")))
                        return directory.FullName;
                    directory = directory.Parent;
                }
            }
            Assert.Fail("repository root containing Assets/Scripts was not found");
            return null;
        }
    }
}
