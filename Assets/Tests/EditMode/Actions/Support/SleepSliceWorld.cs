using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Tests.EditMode.Actions.Support
{
    /// <summary>
    /// W34 DOC4 §0 (Sleep half): the ONE world-building path for the SLEEP story tests —
    /// FarmSliceWorld's sibling, minus the crop machinery. Site(1) spans (0,0)-(10,10) so
    /// the settlement hosts a village; a Tired villager's Home is a specific cell inside
    /// (BedRoom) — the slice's "bed" is that cell (§3, no bed furniture yet). Site is
    /// Settlement (not Region): the night's fatigue must survive alongside settlement-only
    /// systems (vermin, curfew clock) rather than in an artificial Region cul-de-sac.
    /// CONSTRAINT (bed-key rule, SleepOperations.cs:23-27): the "bed:{x}:{y}" tag is the
    /// reservation lock; the ledger never parses it. Fatigue lives on Needs (not Vitals).
    /// </summary>
    internal static class SleepSliceWorld
    {
        public static readonly SiteId Site = new SiteId(1UL);
        public static readonly GridPosition BedRoom = new GridPosition(2, 2);
        // A far-away away-from-bed anchor: 7 Chebyshev from BedRoom, well past BedReachCells(1).
        public static readonly GridPosition FarField = new GridPosition(9, 9);

        public static WorldState Build()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(21 * GameTime.MinutesPerHour); // 21:00 — one hour before curfew
            world.Sites.Add(new SiteRecord(Site, SiteKind.Settlement, "Homestead",
                new GridPosition(0, 0), new GridPosition(10, 10))); // centre (5,5)
            // Empty larder — a hungry actor would out-priority sleep (§4); story tests keep
            // Hunger comfortable so the sleep decision never races a meal at 22:00.
            world.Stockpiles.Add(new StockpileComponent(Site));
            return world;
        }

        /// <summary>Fed, thirsty-neutral, but fatigued villager whose Home is BedRoom.
        /// Fatigue 80 keeps a wide floor: the SleepAdvancer recovers 2/3 per tick — a full
        /// night (~8h = 480 ticks) can dip fatigue by ~320, so 80 will bottom out and stay
        /// abed until dawn (§5.3 "Fatigue 0 does NOT end the night").</summary>
        public static ActorRecord Tired(ulong id, int x, int y)
        {
            var actor = new ActorRecord(
                new ActorId(id), "Sleeper" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1,
                home: BedRoom);
            actor.ApplyNeeds(actor.Needs.WithFatigue(new NeedValue(80)));
            return actor;
        }

        /// <summary>The W32 single interruption gate: an armed chase targeting the sleeper —
        /// the advancer's pursuit probe fires at the NEXT advancement step (S2's fixture).</summary>
        public static void Interrupt(WorldState world, ulong actorId, long minutes = 50)
        {
            world.GuardPursuits.Add(new PursuitRecord
            { GuardId = 99_999UL, TargetId = actorId, UntilMinutes = world.Time.TotalMinutes + minutes });
        }

        public static void ClearInterrupts(WorldState world) => world.GuardPursuits.Clear();

        /// <summary>Count of bed-reservation rows currently owned by the actor.</summary>
        public static int BedReservations(WorldState world, ulong actorId)
            => world.Reservations.Rows.Count(r => r != null && r.ActorId == actorId
                && r.ItemTag != null && r.ItemTag.StartsWith("bed:"));
    }
}
