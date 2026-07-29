using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// V3 YOLDAŞ: companions are recruited CIVILIANS, not a new actor role — they keep their
// identity, their sprite, and crucially their ActorMemory, so the dialogue pipeline recalls
// the journey you shared ("witnessed_attack" beside you means something now). Membership
// lives on WorldState.CompanionIds; behavior splits into a per-tick FOLLOW (heel distance)
// and an hourly GUARD strike (an adjacent hostile near the player or the companion gets hit
// with the same deterministic dice predation uses). Stateless step instances (H1 lesson).
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Recruit/dismiss rules — the party's front door.</summary>
    public static class CompanionService
    {
        public const int MaxCompanions = 2;
        public const int RecruitReachCells = 3;

        public static bool TryRecruit(WorldState world, ActorId actorId)
        {
            var player = FindPlayer(world);
            if (player == null || world.CompanionIds == null) return false;
            if (world.CompanionIds.Count >= MaxCompanions) return false;
            if (world.CompanionIds.Contains(actorId)) return false;
            if (!world.Actors.TryGet(actorId, out var actor) || actor == null || !actor.IsAlive) return false;
            if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) return false;
            if (actor.Position.ChebyshevDistanceTo(player.Position) > RecruitReachCells) return false;

            world.CompanionIds.Add(actorId);
            world.Events?.Append(new WorldEvent(world.Time, WorldEventKind.ActorTalked, actorId, default,
                $"companion_joined name:{actor.Name}"));
            return true;
        }

        public static bool TryDismiss(WorldState world, ActorId actorId)
        {
            if (world?.CompanionIds == null || !world.CompanionIds.Remove(actorId)) return false;
            world.Actors.TryGet(actorId, out var actor);
            world.Events?.Append(new WorldEvent(world.Time, WorldEventKind.ActorTalked, actorId, default,
                $"companion_left name:{actor?.Name ?? "?"}"));
            return true;
        }

        public static bool IsCompanion(WorldState world, ActorId actorId)
            => world?.CompanionIds != null && world.CompanionIds.Contains(actorId);

        public static ActorRecord FindPlayer(WorldState world) // public: the proof surface in Presentation also needs it
        {
            if (world?.Actors?.Records == null) return null;
            foreach (var actor in world.Actors.Records)
                if (actor != null && actor.IsAlive && actor.Role == ActorRole.Player) return actor;
            return null;
        }

    }

    /// <summary>
    /// Companion membership housekeeping and deterministic target selection.
    /// Movement and combat are owned by the action lifecycle, never by this policy helper.
    /// </summary>
    public sealed class CompanionSystem
    {
        public const int HeelCells = 1;       // at or inside heel range the companion stands easy
        public const int GuardReachCells = 2; // hostiles this close to player OR companion get struck

        /// <summary>Decision-slot cleanup: fallen companions leave the roster loudly.</summary>
        public int SweepFallen(WorldState world)
        {
            if (world?.CompanionIds == null || world.CompanionIds.Count == 0) return 0;

            var removed = 0;
            for (int i = world.CompanionIds.Count - 1; i >= 0; i--)
            {
                if (world.Actors.TryGet(world.CompanionIds[i], out var member)
                    && member != null && member.IsAlive)
                    continue;
                var fallenId = world.CompanionIds[i];
                world.CompanionIds.RemoveAt(i);
                if (world.HuntTargets != null)
                    for (var row = world.HuntTargets.Count - 1; row >= 0; row--)
                        if (world.HuntTargets[row].HunterId == fallenId.Value)
                            world.HuntTargets.RemoveAt(row);
                world.Actors.TryGet(fallenId, out var fallen);
                world.Events?.Append(new WorldEvent(world.Time, WorldEventKind.ActorTalked,
                    fallenId, default, $"companion_fell name:{fallen?.Name ?? "?"}"));
                removed++;
            }
            return removed;
        }

        /// <summary>
        /// First deterministic live enemy within guard reach of either the player or companion.
        /// Selection is read-only; the lifecycle records the relationship in HuntTargets.
        /// </summary>
        public ActorRecord FindGuardThreat(
            WorldState world,
            GridPosition player,
            GridPosition companion)
        {
            if (world?.Actors?.Records == null) return null;
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            foreach (var actor in world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive || actor.Role != ActorRole.Enemy) continue;
                int dist = System.Math.Min(
                    actor.Position.ChebyshevDistanceTo(player),
                    actor.Position.ChebyshevDistanceTo(companion));
                if (dist <= GuardReachCells && dist < bestDist) { bestDist = dist; best = actor; }
            }
            return best;
        }
    }
}
