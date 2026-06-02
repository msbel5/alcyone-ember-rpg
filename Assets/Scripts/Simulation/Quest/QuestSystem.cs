using System;
using System.Collections.Generic;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Quest
{
    /// <summary>Pure deterministic quest tick that drives active quest state from the authored catalog.</summary>
    public sealed class QuestSystem
    {
        public void Tick(WorldState world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (world.Quests == null || world.Events == null)
                return;

            var view = new QuestWorldView(world);
            foreach (var entry in world.Quests.Active)
                TickQuest(world, view, entry.Key, entry.Value);
        }

        private static void TickQuest(WorldState world, in QuestWorldView view, QuestId questId, QuestState state)
        {
            if (state == null || state.IsComplete)
                return;

            var definition = QuestCatalog.Resolve(questId);
            var eventActorId = ResolveEventActorId(world, definition);
            var eventSiteId = ResolveEventSiteId(world, definition);

            AppendStartedIfNeeded(world, definition, eventActorId, eventSiteId);

            var tasks = definition.CreateTaskInstances();
            var context = new QuestMutationContext(world, state, eventActorId, eventSiteId);
            var wasComplete = state.IsComplete;

            for (var i = 0; i < tasks.Count && !state.IsComplete; i++)
                tasks[i].TryTrigger(i, in view, context);

            if (!wasComplete && state.IsComplete)
            {
                world.Events.Append(new WorldEvent(
                    world.Time,
                    WorldEventKind.QuestCompleted,
                    eventActorId,
                    eventSiteId,
                    BuildCompletedReason(definition.DisplayName, state.IsSuccess)));
            }
        }

        private static void AppendStartedIfNeeded(WorldState world, QuestDefinition definition, ActorId actorId, SiteId siteId)
        {
            var startedReason = BuildStartedReason(definition.DisplayName);
            for (var i = 0; i < world.Events.Count; i++)
            {
                var existing = world.Events.Events[i];
                if (existing.Kind == WorldEventKind.QuestStarted && string.Equals(existing.Reason, startedReason, StringComparison.Ordinal))
                    return;
            }

            world.Events.Append(new WorldEvent(world.Time, WorldEventKind.QuestStarted, actorId, siteId, startedReason));
        }

        private static ActorId ResolveEventActorId(WorldState world, QuestDefinition definition)
        {
            foreach (var binding in definition.ResourceBindings.Bindings.Values)
            {
                if (binding.Kind == QuestResourceKind.Person)
                    return binding.ActorId;
            }

            return world.Actors != null && world.Actors.TryFirstByRole(ActorRole.Player, out var player) && player != null
                ? player.Id
                : default;
        }

        private static SiteId ResolveEventSiteId(WorldState world, QuestDefinition definition)
        {
            foreach (var binding in definition.ResourceBindings.Bindings.Values)
            {
                if (binding.Kind == QuestResourceKind.Place)
                    return binding.SiteId;
            }

            if (world.Sites != null)
            {
                foreach (var site in world.Sites.Records)
                    return site.Id;
            }

            return default;
        }

        private static string BuildStartedReason(string displayName)
        {
            return "quest_started:" + displayName;
        }

        private static string BuildCompletedReason(string displayName, bool success)
        {
            return "quest_completed:" + displayName + ":" + (success ? "success" : "failure");
        }
    }
}
