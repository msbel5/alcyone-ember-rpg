using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Tests.EditMode.Actions.Support
{
    /// <summary>
    /// W34 DOC2 §11: the ONE world-building path for the WORK story tests — FarmSliceWorld's
    /// sibling. Site(1) spans (0,0)-(10,10); the furnace bench sits at (4,5) so a smith seeded
    /// at (9,9) commutes a REAL 5-cell walk (MoveToWorksite must be observably Running, never
    /// a same-tick arrival). The recipe truth is the composer's ProductionRecipeRegistry:
    /// smelt 1001 = iron_ore x2 + fuel x1 → iron_ingot, 2 bench strokes.
    /// CONSTRAINT (no reservation, docs/ruh/w34/02 §3): the JobBoard claim IS the work lock —
    /// stories assert on WorkOrders rows and the claim, never on ledger rows.
    /// </summary>
    internal static class WorkSliceWorld
    {
        public const string OreTag = "iron_ore";
        public const string FuelTag = "fuel";
        public const string IngotTag = "iron_ingot";

        public static readonly SiteId Site = new SiteId(1UL);
        public static readonly GridPosition Bench = new GridPosition(4, 5);
        public static readonly JobId Job = new JobId(9101UL);
        public static readonly RecipeId SmeltRecipe = new RecipeId(1001UL);

        public static WorldState Build(int ore = 2, int fuel = 1)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(6 * GameTime.MinutesPerHour); // 06:00 — the workday opens
            // SiteKind.Region: outside the walls — ambient vermin key on Settlement sites and
            // would nibble the ledger (the FarmSliceWorld lesson; geometry unchanged).
            world.Sites.Add(new SiteRecord(Site, SiteKind.Region, "Forgestead",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            var pile = new StockpileComponent(Site);
            if (ore > 0) pile.Add(OreTag, ore);
            if (fuel > 0) pile.Add(FuelTag, fuel);
            world.Stockpiles.Add(pile);
            // One active Furnace bench: the claim machine's precondition AND decide gate 4.
            world.Worksites.Add(new WorksiteRecord(Site, Bench, WorksiteKind.Furnace, isActive: true));
            return world;
        }

        /// <summary>Fed, rested civilian with a Smith preference: the work decision never races
        /// a meal at t=0 (hunger 0 &lt; threshold 55), and the claim never refuses (&lt; 80).</summary>
        public static ActorRecord Smith(ulong id, int x, int y)
        {
            return new ActorRecord(
                new ActorId(id), "Smith" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
        }

        /// <summary>Posts one smelt(1001) job at the bench — the story tests' stand-in for the
        /// worldgen forge seed when worldgen itself is not under test.</summary>
        public static void PostSmeltJob(WorldState world, int quantity = 1, ulong jobNo = 0UL)
        {
            world.Jobs.Add(new JobRequest(
                new JobId(Job.Value + jobNo), SmeltRecipe, Site, Bench,
                WorksiteKind.Furnace, JobKind.Smith, JobPriority.Active(1),
                quantity, new ActorId(999UL)));
        }

        public static StockpileComponent Pile(WorldState world) => world.Stockpiles[0];

        /// <summary>The W32 single interruption gate: an armed chase targeting the actor — the
        /// advancer's pursuit probe fires at the NEXT advancement step.</summary>
        public static void Interrupt(WorldState world, ulong actorId, long minutes = 10)
        {
            world.GuardPursuits.Add(new PursuitRecord
            { GuardId = 99_999UL, TargetId = actorId, UntilMinutes = world.Time.TotalMinutes + minutes });
        }

        public static void ClearInterrupts(WorldState world) => world.GuardPursuits.Clear();

        /// <summary>Kills an actor in place (health 0) — W5's dead-smith gate.</summary>
        public static void Kill(ActorRecord actor)
        {
            actor.ApplyVitals(new ActorVitals(
                new VitalStat(0, 10), actor.Vitals.Fatigue, actor.Vitals.Mana));
        }
    }
}
