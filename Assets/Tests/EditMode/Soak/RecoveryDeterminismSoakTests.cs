using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Soak
{
    /// <summary>PRD-10 deterministic 1/7/30-day recovery gates over the canonical composer.</summary>
    public sealed class RecoveryDeterminismSoakTests
    {
        private const int TicksPerDay = 1440;
        private const int Seed = 4242;
        private const int MaxSerializedSaveBytes = 4 * 1024 * 1024;

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(30)]
        public void SameSeedAndInput_ProduceSameDigestActionAndEventSequence(int days)
        {
            var first = Run(days);
            var second = Run(days);

            Assert.Multiple(() =>
            {
                Assert.That(second.Digest, Is.EqualTo(first.Digest));
                Assert.That(second.ActionTrace, Is.EqualTo(first.ActionTrace));
                Assert.That(second.EventTrace, Is.EqualTo(first.EventTrace));
            });
            AssertBounded(first.World);
            AssertBounded(second.World);
        }

        [Test]
        public void OneDay_HasAtMostOneAutonomousStepAndNoFakeMovementProgress()
        {
            var world = BuildWorld();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            for (var tick = 1; tick <= TicksPerDay; tick++)
            {
                var before = world.Actors.Records
                    .Where(actor => actor != null)
                    .ToDictionary(actor => actor.Id, actor => new ActorTick(
                        actor.Position,
                        actor.ActionState.CurrentAction,
                        actor.ActionState.Phase,
                        actor.ActionState.ProgressTicks));

                composer.Advance(world, tick);

                foreach (var actor in world.Actors.Records)
                {
                    if (actor == null || actor.Role == ActorRole.Player
                        || !before.TryGetValue(actor.Id, out var prior))
                        continue;

                    var distance = actor.Position.ChebyshevDistanceTo(prior.Position);
                    if (distance > 1)
                        Assert.Fail(
                            $"tick {tick}: actor {actor.Id.Value} moved {distance} cells; " +
                            "more than one autonomous position writer ran");

                    var state = actor.ActionState;
                    if (state.CurrentAction == prior.Action
                        && state.Phase == ActionPhase.Running
                        && prior.Phase == ActionPhase.Running
                        && IsMovement(state.CurrentAction)
                        && actor.Position.Equals(prior.Position)
                        && state.ProgressTicks > prior.ProgressTicks)
                    {
                        Assert.Fail(
                            $"tick {tick}: {actor.Id.Value}/{state.CurrentAction} advanced " +
                            "progress without movement");
                    }
                }
            }

            WriteFiveActorStories(world);
        }

        [Test]
        public void MidActionSaveLoad_ContinuesWithTheUninterruptedDigest()
        {
            var original = BuildWorld();
            var actor = original.Actors.Records.First(row =>
                row != null && row.Role == ActorRole.Talker);
            actor.ApplyNeeds(actor.Needs.WithHunger(new NeedValue(95)));
            var originalComposer = new WorldTickComposer();
            originalComposer.Advance(original, 0);

            var tick = 0;
            while (actor.ActionState.CurrentAction == ActorActionType.None && tick < 120)
                originalComposer.Advance(original, ++tick);
            Assert.That(actor.ActionState.CurrentAction, Is.Not.EqualTo(ActorActionType.None),
                "fixture never reached a mid-action save point");
            Assert.That(actor.ActionState.Phase, Is.EqualTo(ActionPhase.Running));
            var savedActorId = actor.Id.Value;
            var savedAction = actor.ActionState.CurrentAction;

            var before = WorldStateDigest.Compute(original);
            var data = WorldSaveMapper.ToData(original);
            var loaded = WorldSaveMapper.ToWorld(data, BuildWorld());
            Assert.Multiple(() =>
            {
                Assert.That(WorldStateDigest.Compute(loaded), Is.EqualTo(before),
                    "the save boundary itself must be byte-identical");
                Assert.That(ActionTrace(loaded.ActionLog), Is.EqualTo(ActionTrace(original.ActionLog)),
                    "the retained action sequence and monotone identity must survive the save");
                Assert.That(EventTrace(loaded.Events), Is.EqualTo(EventTrace(original.Events)),
                    "the retained event sequence and monotone identity must survive the save");
            });

            var loadedComposer = new WorldTickComposer();
            loadedComposer.RebuildAccumulatorsFrom(loaded.Time);
            loadedComposer.Advance(loaded, tick);
            var reachedTerminalBoundary = false;
            for (var step = 1; step <= 240; step++)
            {
                var nextTick = tick + step;
                originalComposer.Advance(original, nextTick);
                loadedComposer.Advance(loaded, nextTick);
                Assert.Multiple(() =>
                {
                    Assert.That(WorldStateDigest.Compute(loaded),
                        Is.EqualTo(WorldStateDigest.Compute(original)),
                        $"post-load digest diverged at tick {nextTick}");
                    Assert.That(ActionTrace(loaded.ActionLog),
                        Is.EqualTo(ActionTrace(original.ActionLog)),
                        $"post-load action sequence diverged at tick {nextTick}");
                    Assert.That(EventTrace(loaded.Events),
                        Is.EqualTo(EventTrace(original.Events)),
                        $"post-load event sequence diverged at tick {nextTick}");
                });

                reachedTerminalBoundary = HasTerminalTransition(
                    original.ActionLog, savedActorId, savedAction);
                if (reachedTerminalBoundary)
                    break;
            }
            Assert.That(reachedTerminalBoundary, Is.True,
                "the restored mid-action episode must reach a terminal boundary within 240 ticks");
        }

        private static Snapshot Run(int days)
        {
            var world = BuildWorld();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            composer.Advance(world, days * TicksPerDay);
            return new Snapshot(
                world,
                WorldStateDigest.Compute(world),
                ActionTrace(world.ActionLog),
                EventTrace(world.Events));
        }

        private static WorldState BuildWorld()
        {
            var world = new WorldFactory().Create(Seed);
            WorldFactory.SeedVillagers(world);
            world.EnsureInvariants();
            return world;
        }

        private static void AssertBounded(WorldState world)
        {
            Assert.That(world.Events.Count,
                Is.LessThanOrEqualTo(WorldTickComposer.MaxRetainedWorldEvents));
            Assert.That(world.ActionLog.Count,
                Is.LessThanOrEqualTo(ActionLogRing.Capacity));
            Assert.That(world.Events.TotalAppended,
                Is.EqualTo(world.Events.FirstRetainedSeq + world.Events.Count));
            for (var index = 0; index < world.Events.Count; index++)
                Assert.That(world.Events.Events[index].Sequence,
                    Is.EqualTo(world.Events.FirstRetainedSeq + index));

            foreach (var actor in world.Actors.Records.Where(row => row != null))
            {
                Assert.That(actor.Needs.Hunger.Value, Is.InRange(NeedValue.Min, NeedValue.Max));
                Assert.That(actor.Needs.Fatigue.Value, Is.InRange(NeedValue.Min, NeedValue.Max));
                Assert.That(actor.Needs.Thirst.Value, Is.InRange(NeedValue.Min, NeedValue.Max));
                Assert.That(actor.ActionState.CarriedUnits, Is.GreaterThanOrEqualTo(0));
                if (actor.ActionState.CarriedUnits > 0)
                    Assert.That(actor.ActionState.CarriedMatterTag, Is.Not.Null.And.Not.Empty,
                        "every carried unit must retain its conserved matter identity");
                if (IsTerminal(actor.ActionState.Phase))
                {
                    Assert.That(actor.ActionState.CarriedUnits, Is.Zero,
                        "terminal actors cannot retain carried matter");
                    Assert.That(world.Reservations.TryGetByActor(actor.Id.Value, out _), Is.False,
                        "terminal actors cannot retain reservations");
                }
            }
            foreach (var reservation in world.Reservations.Rows.Where(row => row != null))
            {
                Assert.That(world.Actors.TryGet(new ActorId(reservation.ActorId), out var owner), Is.True,
                    "every reservation must have a live actor owner");
                Assert.That(owner.ActionState.ReservationId.Value, Is.EqualTo(reservation.Id),
                    "every reservation must be the owner's current action claim");
                Assert.That(IsTerminal(owner.ActionState.Phase), Is.False,
                    "terminal action state cannot own a reservation");
            }
            foreach (var pile in world.Stockpiles.Where(row => row != null))
                foreach (var entry in pile.Entries)
                    Assert.That(entry.Value, Is.GreaterThanOrEqualTo(0),
                        $"negative matter at site {pile.SiteId.Value}, item {entry.Key}");

            var save = WorldSaveMapper.ToData(world);
            Assert.That(save.worldEvents.Length,
                Is.LessThanOrEqualTo(WorldTickComposer.MaxRetainedWorldEvents));
            Assert.That(save.actionLogTickMinutes.Length,
                Is.LessThanOrEqualTo(ActionLogRing.Capacity));
            var saveBytes = Encoding.UTF8.GetByteCount(
                new JsonSliceSaveService().SaveToJson(world));
            Assert.That(saveBytes, Is.LessThanOrEqualTo(MaxSerializedSaveBytes),
                "bounded logs must keep a long-soak save below the pinned 4 MiB budget");
        }

        private static string ActionTrace(ActionLogRing log)
        {
            var text = new StringBuilder(log.Count * 48);
            text.Append("total=").Append(log.TotalPushed).Append('|');
            for (var index = 0; index < log.Count; index++)
            {
                var row = log.At(index);
                text.Append(row.TickMinutes).Append(':')
                    .Append(row.ActorId).Append(':')
                    .Append((int)row.Intent).Append(':')
                    .Append((int)row.FromAction).Append('/')
                    .Append((int)row.FromPhase).Append('>')
                    .Append((int)row.ToAction).Append('/')
                    .Append((int)row.ToPhase).Append(':')
                    .Append(row.TargetId).Append(':')
                    .Append((int)row.Reason).Append('|');
            }
            return text.ToString();
        }

        private static string EventTrace(WorldEventLog log)
        {
            var text = new StringBuilder(log.Count * 56);
            text.Append("first=").Append(log.FirstRetainedSeq)
                .Append(":total=").Append(log.TotalAppended).Append('|');
            foreach (var row in log.Events)
                text.Append(row.Sequence).Append(':')
                    .Append(row.Tick.TotalMinutes).Append(':')
                    .Append((int)row.Kind).Append(':')
                    .Append(row.ActorId.Value).Append(':')
                    .Append(row.SiteId.Value).Append(':')
                    .Append(row.Reason).Append('|');
            return text.ToString();
        }

        private static void WriteFiveActorStories(WorldState world)
        {
            var rows = new List<ActionLogEntry>();
            for (var index = 0; index < world.ActionLog.Count; index++)
                rows.Add(world.ActionLog.At(index));
            var stories = new List<(ulong ActorId, List<ActionLogEntry> Rows, int TerminalIndex)>();
            foreach (var group in rows.GroupBy(row => row.ActorId).OrderBy(group => group.Key))
            {
                var actorRows = group.ToList();
                var terminalIndex = actorRows.FindLastIndex(row => IsTerminal(row.ToPhase));
                if (terminalIndex >= 0)
                    stories.Add((group.Key, actorRows, terminalIndex));
                if (stories.Count == 5)
                    break;
            }
            Assert.That(stories.Count, Is.EqualTo(5),
                "one living day must produce five actor-owned stories");

            foreach (var story in stories)
            {
                world.Actors.TryGet(new ActorId(story.ActorId), out var actor);
                var terminal = story.Rows[story.TerminalIndex];
                var episodeStart = story.TerminalIndex;
                while (episodeStart > 0
                       && story.Rows[episodeStart - 1].Intent == terminal.Intent)
                    episodeStart--;
                var transitions = story.Rows
                    .Skip(episodeStart)
                    .Take(story.TerminalIndex - episodeStart + 1)
                    .Select(row =>
                    $"{row.FromAction}/{row.FromPhase}->{row.ToAction}/{row.ToPhase}" +
                    $"({row.Reason})@{row.TickMinutes}");
                TestContext.Out.WriteLine(
                    $"STORY actor={actor?.Name ?? "unknown"}#{story.ActorId}: " +
                    string.Join("; ", transitions));
            }
        }

        private static bool HasTerminalTransition(
            ActionLogRing log,
            ulong actorId,
            ActorActionType action)
        {
            for (var index = 0; index < log.Count; index++)
            {
                var row = log.At(index);
                if (row.ActorId == actorId
                    && row.FromAction == action
                    && IsTerminal(row.ToPhase))
                    return true;
            }
            return false;
        }

        private static bool IsTerminal(ActionPhase phase)
            => phase == ActionPhase.Succeeded || phase == ActionPhase.Failed;

        private static bool IsMovement(ActorActionType action)
        {
            switch (action)
            {
                case ActorActionType.MoveToFood:
                case ActorActionType.MoveToPlot:
                case ActorActionType.HaulCrop:
                case ActorActionType.MoveToBed:
                case ActorActionType.MoveToWorksite:
                case ActorActionType.OnWatch:
                case ActorActionType.Hunt:
                case ActorActionType.FollowPlayer:
                case ActorActionType.ReportCrime:
                case ActorActionType.Pursue:
                    return true;
                default:
                    return false;
            }
        }

        private readonly struct ActorTick
        {
            public ActorTick(
                GridPosition position,
                ActorActionType action,
                ActionPhase phase,
                int progressTicks)
            {
                Position = position;
                Action = action;
                Phase = phase;
                ProgressTicks = progressTicks;
            }

            public GridPosition Position { get; }
            public ActorActionType Action { get; }
            public ActionPhase Phase { get; }
            public int ProgressTicks { get; }
        }

        private sealed class Snapshot
        {
            public Snapshot(
                WorldState world,
                string digest,
                string actionTrace,
                string eventTrace)
            {
                World = world;
                Digest = digest;
                ActionTrace = actionTrace;
                EventTrace = eventTrace;
            }

            public WorldState World { get; }
            public string Digest { get; }
            public string ActionTrace { get; }
            public string EventTrace { get; }
        }
    }
}
