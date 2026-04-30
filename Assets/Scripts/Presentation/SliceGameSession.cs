using System;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.World;

// Design note:
// SliceGameSession keeps Unity presentation orchestration thin by owning slice state and pure services.
// Inputs: requested player grid position, interaction commands, and JSON save/load text.
// Outputs: updated deterministic world state plus status text for the HUD and view.
// Bible reference: PRD Sprint 1 FR-03 through FR-08, Sprint 2 FR-01 through FR-05.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Presentation-facing session wrapper around the pure slice systems.</summary>
    public sealed class SliceGameSession
    {
        private readonly SliceWorldFactory _factory = new SliceWorldFactory();
        private readonly RoomMovementService _movement = new RoomMovementService();
        private readonly PickupService _pickups = new PickupService();
        private readonly EncounterTurnService _turns = new EncounterTurnService();
        private readonly AskAboutService _askAbout = new AskAboutService();
        private readonly AskDmService _askDm = new AskDmService();
        private readonly ThinkService _think = new ThinkService();
        private readonly MerchantTradeService _merchant = new MerchantTradeService();
        private readonly GuardInteractionService _guard = new GuardInteractionService();
        private readonly DoorInteractionService _door = new DoorInteractionService();
        private readonly JsonSliceSaveService _save = new JsonSliceSaveService();

        private EncounterState _encounter;
        private IDeterministicRng _rng = new XorShiftRng(1337);

        public SliceWorldState World { get; private set; }
        public string Status { get; private set; }

        public void StartNewWorld(int roomSeed)
        {
            _rng = new XorShiftRng((uint)roomSeed);
            _encounter = null;
            World = _factory.Create(roomSeed);
            SetStatus(World.LastNarrative);
        }

        public void SyncPlayerPosition(GridPosition desired)
        {
            var current = World.Player.Position;
            var deltaX = Math.Max(-1, Math.Min(1, desired.X - current.X));
            var deltaY = Math.Max(-1, Math.Min(1, desired.Y - current.Y));
            if (deltaX == 0 && deltaY == 0)
                return;
            World.Player.MoveTo(_movement.Move(World, current, deltaX, deltaY));
        }

        public void TryPickup()
        {
            var pickup = World.Pickups.Find(candidate => !candidate.IsCollected && candidate.Position.ManhattanDistanceTo(World.Player.Position) <= 1);
            SetStatus(pickup != null && _pickups.TryCollect(pickup, World.PlayerInventory)
                ? $"Picked up {pickup.Item.DisplayName}."
                : "No pickup within reach or inventory is full.");
        }

        public void AdvanceEncounter()
        {
            if (World.Enemy.Vitals.Health.IsDepleted)
            {
                SetStatus("Ash Rat is already down.");
                return;
            }
            if (World.Enemy.Position.ManhattanDistanceTo(World.Player.Position) > 1 && _encounter == null)
            {
                SetStatus("Move next to Ash Rat and press F to start the approved Sprint 1 encounter loop.");
                return;
            }
            _encounter ??= new EncounterState(World.Player.Id, World.Enemy.Id);
            World.EncounterActive = true;
            SetStatus(_turns.Advance(_encounter, World.Player, World.Enemy, _rng).Summary);
            if (_encounter.IsFinished)
                World.EncounterActive = false;
        }

        public void AskAbout(string topicId) => SetStatus(World.Talker.Position.ManhattanDistanceTo(World.Player.Position) <= 2 ? _askAbout.Ask(World, topicId) : "Stand closer to Sage Nera before asking about a topic.");
        public void AskDm() => SetStatus(_askDm.Ask(World, "What matters right now?"));
        public void Think() => SetStatus(_think.Think(World));
        public void TradeWithMerchant() => SetStatus(_merchant.TradeGateWrit(World));
        public void InteractWithGuard() => SetStatus(_guard.Interact(World));
        public void ToggleDoor() => SetStatus(_door.Toggle(World));
        public string SaveToJson() => _save.SaveToJson(World);

        public void LoadFromJson(string json)
        {
            _rng = new XorShiftRng(1337);
            _encounter = null;
            World = _save.LoadFromJson(json);
            SetStatus("Slice state loaded from JSON.");
        }

        public string DescribeNextActor()
        {
            if (_encounter == null || _encounter.IsFinished)
                return "none";
            return _encounter.PlayerActsNext ? World.Player.Name : World.Enemy.Name;
        }

        private void SetStatus(string status)
        {
            Status = status;
            World.LastNarrative = status;
        }
    }
}
