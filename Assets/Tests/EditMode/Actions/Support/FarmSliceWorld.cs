using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living.Actions;

namespace EmberCrpg.Tests.EditMode.Actions.Support
{
    /// <summary>
    /// W33 DOC4 §0: the ONE world-building path for the FARM story tests — EatSliceWorld's
    /// sibling. Site(1) spans (0,0)-(10,10) (centre (5,5)); the soil belt runs along y=0 so a
    /// haul is a REAL walk (belt→centre Chebyshev 5 &gt; EatReachCells 2 — HaulCrop must be
    /// observable Running, never a same-tick arrival). The composer's wheat species is the
    /// catalog: seed(1 day) → sprout(1 day) → ripe(harvestable).
    /// CONSTRAINT (seed-corn rule, W33-01 §7.2): the crop is its OWN seed — SeedTag == CropTag,
    /// one "wheat" ledger. Story setups must budget for diners eating the seed corn.
    /// </summary>
    internal static class FarmSliceWorld
    {
        /// <summary>Tag codec owner is Simulation's FarmOperations (internal); tests read the
        /// wire literals. SeedTag == CropTag is the seed-corn rule, not a typo.</summary>
        public const string SeedTag = "wheat";
        public const string CropTag = "wheat";
        public const string PlotKeyPrefix = "plot:";
        public const string CarryKeyPrefix = "carry:";

        /// <summary>Verbatim HarvestCropAdvancer.HarvestYieldUnits — the retired HarvestStep's "+2".</summary>
        public const int HarvestYield = EmberCrpg.Simulation.Living.Actions.HarvestCropAdvancer.HarvestYieldUnits;

        public static readonly SiteId Site = new SiteId(1UL);

        /// <summary>Soil belt cell i sits at (i, 0) with id 101+i.</summary>
        public static WorldComponentId SoilId(int index) => new WorldComponentId(101UL + (ulong)index);

        public static WorldState Build(int seedStock = 4, int soilCells = 2)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(6 * GameTime.MinutesPerHour); // 06:00 — the workday opens
            // SiteKind.Region: a farmstead OUTSIDE the walls. Ambient vermin (AmbientLifeSystem)
            // key on Settlement sites and would nibble the seed-corn ledger ~1/rat/hour — noise
            // the farm stories must not budget for. Geometry (centre, reach, seats) is unchanged.
            world.Sites.Add(new SiteRecord(Site, SiteKind.Region, "Farmstead",
                new GridPosition(0, 0), new GridPosition(10, 10))); // centre (5,5)
            var pile = new StockpileComponent(Site);
            if (seedStock > 0) pile.Add(SeedTag, seedStock);
            world.Stockpiles.Add(pile);
            for (var i = 0; i < soilCells; i++)
                world.Soils.Add(SoilId(i), new SoilComponent(
                    SoilId(i), Site, new GridPosition(i, 0), fertility: 50, moisture: 50, plantId: default));
            // One active Field worksite: the claim machine's precondition (jobs wait without it).
            world.Worksites.Add(new WorksiteRecord(Site, new GridPosition(0, 0), WorksiteKind.Field, isActive: true));
            return world;
        }

        /// <summary>Fed, rested civilian with a Farmer job preference: the farm decision never
        /// races a meal at t=0 (hunger 0 &lt; threshold 55).</summary>
        public static ActorRecord Farmer(ulong id, int x, int y)
        {
            return new ActorRecord(
                new ActorId(id), "Farmer" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1,
                jobPreferences: new[] { new ActorJobPreference(JobKind.Farmer, JobPriority.Active(1)) });
        }

        /// <summary>F5's circle-closing diner: hunger 80 eats NOW — and 80 ≥ the job refusal
        /// threshold, so the diner never competes for the planting job.</summary>
        public static ActorRecord Hungry(ulong id, int x, int y)
        {
            var actor = new ActorRecord(
                new ActorId(id), "Diner" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
            actor.ApplyNeeds(actor.Needs.WithHunger(new NeedValue(80)));
            return actor;
        }

        /// <summary>Posts one planting job per free belt cell start (ids 9001+): the story tests'
        /// stand-in for the shortage cascade when the cascade itself is not under test.</summary>
        public static void PostPlantingJob(WorldState world, ulong jobNo = 1UL)
        {
            world.Jobs.Add(EmberCrpg.Simulation.Process.FarmingJobRequestFactory.CreatePlantingJob(
                new JobId(9000UL + jobNo), Site, new GridPosition(0, 0),
                new ActorId(999UL), JobPriority.Active(1), quantity: 1));
        }

        /// <summary>Seeds a plant on belt cell i at the given stage (worldgen-style field).
        /// Plant id mirrors FarmOperations.PlantIdFor: PlantIdBase(500000) + soil id.</summary>
        public static PlantComponent Plant(WorldState world, int soilIndex, string stage)
        {
            var soil = world.Soils.Get(SoilId(soilIndex));
            var plantId = new WorldComponentId(500_000UL + soil.Id.Value);
            var plant = new PlantComponent(plantId, Site, soil.Position, "wheat",
                new PlantStageId(stage), 0);
            world.Plants.Add(plantId, plant);
            world.Soils.Replace(soil.Id, soil.WithPlant(plantId));
            return plant;
        }

        public static PlantComponent PlantRipe(WorldState world, int soilIndex = 0)
            => Plant(world, soilIndex, "ripe");

        /// <summary>Conservation counter: pile + hands + (ripe plot ≙ its yield). A ripe plot's
        /// potential IS HarvestYield — the commit mints exactly that, so the sum is flat across
        /// every legal transition (W33 DOC4 F4).</summary>
        public static int TotalCrop(WorldState world)
        {
            var pile = world.Stockpiles.Where(p => p != null).Sum(p => p.Get(CropTag));
            var hands = world.Actors.Records.Where(a => a != null).Sum(a => a.ActionState.CarriedUnits);
            var ripe = world.Plants.Rows.Count(r => r.Value != null && r.Value.StageId.Value == "ripe");
            return pile + hands + ripe * HarvestYield;
        }

        /// <summary>Active plot claims ("plot:" rows) — the ledger IS the exclusivity.</summary>
        public static int PlotClaims(WorldState world)
            => world.Reservations.Rows.Count(r => r != null && r.ItemTag.StartsWith(PlotKeyPrefix));

        public static ReservationRecord PlotClaim(WorldState world)
            => world.Reservations.Rows.Single(r => r != null && r.ItemTag.StartsWith(PlotKeyPrefix));

        /// <summary>The W32 single interruption gate: an armed chase targeting the actor — the
        /// probe fires at the NEXT advancement step (this is the slice's FarmOps.Interrupt).</summary>
        public static void Interrupt(WorldState world, ulong actorId)
        {
            world.GuardPursuits.Add(new PursuitRecord
            { GuardId = 99_999UL, TargetId = actorId, UntilMinutes = world.Time.TotalMinutes + 10 });
        }

        /// <summary>Clears the interruption gate so the story can resume.</summary>
        public static void ClearInterrupts(WorldState world) => world.GuardPursuits.Clear();

        /// <summary>Meals of ONE actor as terminal action outcomes (never reason-string grep of
        /// diagnostics — the eat completion line is the pinned W32 grammar).</summary>
        public static int MealsOf(WorldState world, ulong actorId)
            => world.Events.Events.Count(e => e.Kind == WorldEventKind.ActionCompleted
                && e.ActorId.Value == actorId && e.Reason != null && e.Reason.Contains("eat:consume completed"));
    }
}
