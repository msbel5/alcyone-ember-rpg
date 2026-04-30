using System.IO;
using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.World;
using UnityEngine;

// Design note:
// SliceGameController wires Sprint 1's pure slice services into a runnable Unity presentation shell.
// Inputs: scene lifecycle, keyboard/mouse actions, and JSON save/load hotkeys.
// Outputs: one-room play loop with movement, pickup, combat, dialogue, and persistence.
// Bible reference: PRD FR-03 through FR-08.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Thin presentation orchestrator for the vertical slice.</summary>
    public sealed class SliceGameController : MonoBehaviour
    {
        private readonly SliceWorldFactory _factory = new SliceWorldFactory();
        private readonly SliceWorldView _view = new SliceWorldView();
        private readonly PickupService _pickups = new PickupService();
        private readonly EncounterTurnService _turns = new EncounterTurnService();
        private readonly AskAboutService _askAbout = new AskAboutService();
        private readonly AskDmService _askDm = new AskDmService();
        private readonly ThinkService _think = new ThinkService();
        private readonly JsonSliceSaveService _save = new JsonSliceSaveService();

        private SliceWorldState _world;
        private SlicePlayerRig _player;
        private EncounterState _encounter;
        private IDeterministicRng _rng;
        private string _status;
        private string _savePath;

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "sprint1-slice.json");
            _rng = new XorShiftRng(1337);
            var playerObject = new GameObject("SlicePlayer");
            _player = playerObject.AddComponent<SlicePlayerRig>();
            StartNewWorld();
        }

        private void Update()
        {
            _world.Player.MoveTo(_player.ReadGridPosition());
            HandleInput();
            _view.Sync(_world);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10f, 10f, 560f, 165f),
$@"Sprint 1 Slice
WASD + mouse look | E pickup | F advance encounter | 1/2/3 Ask About | Q Ask DM | R Think | F5 save | F9 load
Player HP {_world.Player.Vitals.Health.Current}/{_world.Player.Vitals.Health.Max} | Enemy HP {_world.Enemy.Vitals.Health.Current}/{_world.Enemy.Vitals.Health.Max}
Inventory {_world.PlayerInventory.Items.Count}/{_world.PlayerInventory.Capacity} | Save {_savePath}
Encounter active: {_world.EncounterActive} | Next actor: {DescribeNextActor()}

{_status}");
        }

        private void StartNewWorld()
        {
            _world = _factory.Create(1337);
            _encounter = null;
            _world.EncounterActive = false;
            _player.SnapToGrid(_world.Player.Position);
            _view.Rebuild(_world);
            _status = _world.LastNarrative;
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.E)) TryPickup();
            if (Input.GetKeyDown(KeyCode.F)) AdvanceEncounter();
            if (Input.GetKeyDown(KeyCode.Alpha1)) AskAbout("embers");
            if (Input.GetKeyDown(KeyCode.Alpha2)) AskAbout("gate");
            if (Input.GetKeyDown(KeyCode.Alpha3)) AskAbout("watch");
            if (Input.GetKeyDown(KeyCode.Q)) _status = _askDm.Ask(_world, "What matters right now?");
            if (Input.GetKeyDown(KeyCode.R)) _status = _think.Think(_world);
            if (Input.GetKeyDown(KeyCode.F5)) File.WriteAllText(_savePath, _save.SaveToJson(_world));
            if (Input.GetKeyDown(KeyCode.F9) && File.Exists(_savePath)) LoadWorld(File.ReadAllText(_savePath));
        }

        private void TryPickup()
        {
            var pickup = _world.Pickups.FirstOrDefault(candidate => !candidate.IsCollected && candidate.Position.ManhattanDistanceTo(_world.Player.Position) <= 1);
            _status = pickup != null && _pickups.TryCollect(pickup, _world.PlayerInventory)
                ? $"Picked up {pickup.Item.DisplayName}."
                : "No pickup within reach or inventory is full.";
        }

        private void AdvanceEncounter()
        {
            if (_world.Enemy.Vitals.Health.IsDepleted)
            {
                _status = "Ash Rat is already down.";
                return;
            }
            if (_world.Enemy.Position.ManhattanDistanceTo(_world.Player.Position) > 1 && _encounter == null)
            {
                _status = "Move next to Ash Rat and press F to start the approved Sprint 1 encounter loop.";
                return;
            }
            _encounter ??= new EncounterState(_world.Player.Id, _world.Enemy.Id);
            _world.EncounterActive = true;
            var strike = _turns.Advance(_encounter, _world.Player, _world.Enemy, _rng);
            _status = strike.Summary;
            if (_encounter.IsFinished)
                _world.EncounterActive = false;
        }

        private void AskAbout(string topicId)
        {
            _status = _world.Talker.Position.ManhattanDistanceTo(_world.Player.Position) <= 2
                ? _askAbout.Ask(_world, topicId)
                : "Stand closer to Sage Nera before asking about a topic.";
        }

        private void LoadWorld(string json)
        {
            _world = _save.LoadFromJson(json);
            _encounter = null;
            _player.SnapToGrid(_world.Player.Position);
            _view.Rebuild(_world);
            _status = "Slice state loaded from JSON.";
        }

        private string DescribeNextActor()
        {
            if (_encounter == null || _encounter.IsFinished)
                return "none";
            return _encounter.PlayerActsNext ? _world.Player.Name : _world.Enemy.Name;
        }
    }
}
