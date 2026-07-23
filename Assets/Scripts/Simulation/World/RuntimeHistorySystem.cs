using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Rng;

// CAN SUYU H4: RUNTIME HISTORY. Worldgen's HistorySystems write a rich chronicle and then
// FREEZE at minute zero — after that no faction ever changed its mind about another. This
// daily system keeps history moving with two couplings:
//   1) yesterday's SIMULATION events drift faction relations (the watch earning renown by
//      answering predation; grain shortages straining the craft/trade axis), and
//   2) a monthly chronicle event — seeded by (RoomSeed, dayIndex) so different worlds live
//      different histories — with a concrete mechanical effect, never just a log line.
// Stateless step instance (the H1 lesson): everything derives from world state and time.
namespace EmberCrpg.Simulation.World
{
    /// <summary>Daily: event→relation drift + a monthly seeded chronicle event.</summary>
    public sealed class RuntimeHistorySystem
    {
        // The three relation axes runtime history steers. Presentation's faction hydration
        // guarantees these tags exist — sharing the constants kills the magic-string drift risk.
        public const string LawTag = "law";
        public const string CraftTag = "craft";
        public const string TradeTag = "trade";

        public const int GuardRenownDelta = 1;
        public const int ShortageTensionDelta = -1;
        public const int FestivalBondDelta = 4;
        public const int DisputeDelta = -6;
        public const int CaravanWheat = 25;

        private readonly FactionReputationSystem _reputation = new FactionReputationSystem();

        public void Tick(WorldState world) => Tick(world, world?.Time ?? default);

        // Catchup contract: windows, the chronicle day index, its rng, and all event stamps
        // derive from the cadence-boundary stamp so multi-day jumps replay identically.
        public void Tick(WorldState world, GameTime stamp)
        {
            if (world?.Factions == null || world.Events == null || stamp.TotalMinutes <= 0)
                return;
            var law = FindByTag(world, LawTag);
            var craft = FindByTag(world, CraftTag);
            var trade = FindByTag(world, TradeTag);
            if (law == null || craft == null || trade == null)
                return;

            DriftFromYesterday(world, stamp, law, craft, trade);
            MonthlyChronicle(world, stamp, law, craft, trade);
        }

        private void DriftFromYesterday(WorldState world, GameTime stamp, FactionRecord law, FactionRecord craft, FactionRecord trade)
        {
            long dayStart = stamp.TotalMinutes - GameTime.MinutesPerDay;
            int guardResponses = 0, shortages = 0;
            // Depth-capped for the same O(history) reason as the witness scan (see
            // CascadeSystems) — 8192 covers a full production day plus catchup bursts.
            var events = world.Events.Events;
            int scanFloor = System.Math.Max(0, events.Count - 8192);
            for (int i = events.Count - 1; i >= scanFloor; i--)
            {
                var evt = events[i];
                if (evt.Tick.TotalMinutes <= dayStart || evt.Tick.TotalMinutes > stamp.TotalMinutes) continue;
                if (evt.Kind == WorldEventKind.GuardResponded) guardResponses++;
                else if (evt.Kind == WorldEventKind.ShortageDetected) shortages++;
            }

            // The watch answering trouble is SEEN by the town's factions; scarcity strains trade.
            if (guardResponses > 0)
            {
                _reputation.ApplyDelta(world.Factions, law.Id, craft.Id, GuardRenownDelta, "watch_renown", stamp, world.Events);
                _reputation.ApplyDelta(world.Factions, law.Id, trade.Id, GuardRenownDelta, "watch_renown", stamp, world.Events);
            }
            if (shortages > 0)
                _reputation.ApplyDelta(world.Factions, craft.Id, trade.Id, ShortageTensionDelta, "grain_tension", stamp, world.Events);
        }

        private void MonthlyChronicle(WorldState world, GameTime stamp, FactionRecord law, FactionRecord craft, FactionRecord trade)
        {
            // Fires at each month's END (day 30, 60, ...) — the month closes, the chronicle
            // is written. Day-30 boundaries are reachable in a 31-day gate run; short unit
            // runs (1-15 days) never see one, so decay semantics tests stay unpolluted.
            long dayIndex = stamp.TotalMinutes / GameTime.MinutesPerDay;
            if (dayIndex <= 0 || dayIndex % GameTime.DaysPerMonth != 0)
                return;

            // Seeded by (world seed, day) — two worlds write two DIFFERENT chronicles.
            var rng = new XorShiftRng((((uint)world.RoomSeed * 2654435761u) ^ ((uint)dayIndex * 40503u)) | 1u);
            int intensity = rng.NextInt(20) + 1;
            var site = FirstSite(world);
            string entry;
            switch (rng.NextInt(3))
            {
                case 0: // festival: the town remembers it likes itself
                    _reputation.ApplyDelta(world.Factions, law.Id, craft.Id, FestivalBondDelta, "festival", stamp, world.Events);
                    _reputation.ApplyDelta(world.Factions, craft.Id, trade.Id, FestivalBondDelta, "festival", stamp, world.Events);
                    entry = "festival";
                    break;
                case 1: // caravan surge: real grain hits the larder
                    if (world.Stockpiles.Count > 0 && world.Stockpiles[0] != null)
                        world.Stockpiles[0].Add("wheat", CaravanWheat + intensity);
                    entry = "caravan_surge";
                    break;
                default: // border dispute: the law and the merchants fall out
                    _reputation.ApplyDelta(world.Factions, law.Id, trade.Id, DisputeDelta, "border_dispute", stamp, world.Events);
                    entry = "border_dispute";
                    break;
            }

            // Every month ALSO rolls a diplomatic ripple so relations never fossilize.
            var pairs = new[] { (law, craft), (law, trade), (craft, trade) };
            var (pa, pb) = pairs[rng.NextInt(3)];
            int ripple = rng.NextInt(9) - 4; // -4..+4
            if (ripple != 0)
                _reputation.ApplyDelta(world.Factions, pa.Id, pb.Id, ripple, "chronicle_ripple", stamp, world.Events);

            world.Events.Append(new WorldEvent(stamp, WorldEventKind.ChronicleEvent,
                default, site, $"chronicle:{entry} intensity:{intensity} day:{dayIndex}"));

            ArriveMigrants(world, stamp, dayIndex, rng);
        }

        // PLAYTEST FIX ("vardigimda kimse yoktu"): month's end refills bled-out settlements —
        // any settlement site holding fewer than MigrantFloor living civilians receives up to
        // MigrantsPerMonth newcomers at deterministic homes inside its bounds. ActorSpawned
        // gets its first runtime emitter; the chronicle rng keeps worlds divergent.
        public const int MigrantFloor = 4;
        public const int MigrantsPerMonth = 2;

        private static readonly string[] MigrantNames =
            { "Rill", "Oswic", "Petra", "Halvard", "Ines", "Corm", "Sable", "Yudo" };

        private static void ArriveMigrants(WorldState world, GameTime stamp, long dayIndex, XorShiftRng rng)
        {
            if (world.Sites?.Records == null || world.Actors == null) return;
            var loadout = new WorldActorLoadoutFactory();
            foreach (var site in world.Sites.Records)
            {
                if (site == null || site.Kind != SiteKind.Settlement) continue;
                int living = 0;
                foreach (var actor in world.Actors.Records)
                    if (actor != null && actor.IsAlive
                        && actor.Role != ActorRole.Player && actor.Role != ActorRole.Enemy
                        && actor.Home.X >= site.MinBound.X && actor.Home.X <= site.MaxBound.X
                        && actor.Home.Y >= site.MinBound.Y && actor.Home.Y <= site.MaxBound.Y)
                        living++;
                if (living >= MigrantFloor) continue;

                int arriving = System.Math.Min(MigrantsPerMonth, MigrantFloor - living);
                for (int k = 0; k < arriving; k++)
                {
                    ulong id = 400_000_000UL
                        + ((ulong)site.Id.Value * 4096UL) + ((ulong)(dayIndex % 512) * 8UL) + (ulong)k;
                    if (world.Actors.Contains(new EmberCrpg.Domain.Core.ActorId(id))) continue;
                    int width = System.Math.Max(1, site.MaxBound.X - site.MinBound.X);
                    int height = System.Math.Max(1, site.MaxBound.Y - site.MinBound.Y);
                    var home = new EmberCrpg.Domain.Actors.GridPosition(
                        site.MinBound.X + rng.NextInt(width), site.MinBound.Y + rng.NextInt(height));
                    var name = MigrantNames[rng.NextInt(MigrantNames.Length)] + " of the Road";
                    world.Actors.Add(loadout
                        .Create(new EmberCrpg.Domain.Core.ActorId(id), name, ActorRole.Talker, home)
                        .WithHomeAndAnchor(home, home));
                    world.Events.Append(new WorldEvent(stamp, WorldEventKind.ActorSpawned,
                        new EmberCrpg.Domain.Core.ActorId(id), site.Id,
                        $"migrant_arrived name:{name} site:{site.Id.Value}"));
                }
            }
        }

        private static FactionRecord FindByTag(WorldState world, string tag)
        {
            foreach (var faction in world.Factions.Records)
                if (faction != null && faction.HasTag(tag)) return faction;
            return null;
        }

        private static SiteId FirstSite(WorldState world)
        {
            if (world.Sites?.Records != null)
                foreach (var site in world.Sites.Records)
                    if (site != null) return site.Id;
            return new SiteId(1UL);
        }
    }
}
