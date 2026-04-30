using System.IO;
using UnityEngine;

// Design note:
// SliceGameController wires the slice session, player rig, HUD, and world view into one MonoBehaviour.
// Inputs: scene lifecycle, keyboard/mouse actions, and JSON save/load hotkeys.
// Outputs: a playable one-room shell with deterministic services behind thin presentation wrappers.
// Bible reference: PRD Sprint 1 FR-08, Sprint 2 FR-01 through FR-04.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Thin presentation controller for the playable vertical slice.</summary>
    public sealed class SliceGameController : MonoBehaviour
    {
        private readonly SliceGameSession _session = new SliceGameSession();
        private readonly SliceWorldView _view = new SliceWorldView();

        private SlicePlayerRig _player;
        private SliceAudioCueDriver _audio;
        private string _savePath;

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "sprint2-slice.json");
            _player = new GameObject("SlicePlayer").AddComponent<SlicePlayerRig>();
            _audio = gameObject.GetComponent<SliceAudioCueDriver>() ?? gameObject.AddComponent<SliceAudioCueDriver>();
            _session.StartNewWorld(1337);
            RebuildView();
        }

        private void Update()
        {
            _session.SyncPlayerPosition(_player.ReadGridPosition());
            HandleInput();
            _player.SnapToGrid(_session.World.Player.Position);
            _view.Sync(_session.World);
            _audio.Apply(_session.CurrentAtmosphere);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10f, 10f, 820f, 255f), SliceHudFormatter.Format(_session.World, _savePath, _session.Status, _session.DescribeNextActor(), _session.CurrentAtmosphere));
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.I)) _session.InspectInventory();
            if (Input.GetKeyDown(KeyCode.Z)) _session.EquipFirstWeapon();
            if (Input.GetKeyDown(KeyCode.X)) _session.UnequipWeapon();
            if (Input.GetKeyDown(KeyCode.E)) _session.TryPickup();
            if (Input.GetKeyDown(KeyCode.M)) _session.TradeWithMerchant();
            if (Input.GetKeyDown(KeyCode.G)) _session.InteractWithGuard();
            if (Input.GetKeyDown(KeyCode.T)) _session.ToggleDoor();
            if (Input.GetKeyDown(KeyCode.F)) _session.AdvanceEncounter();
            if (Input.GetKeyDown(KeyCode.Alpha1)) _session.AskAbout("embers");
            if (Input.GetKeyDown(KeyCode.Alpha2)) _session.AskAbout("gate");
            if (Input.GetKeyDown(KeyCode.Alpha3)) _session.AskAbout("watch");
            if (Input.GetKeyDown(KeyCode.Q)) _session.AskDm();
            if (Input.GetKeyDown(KeyCode.R)) _session.Think();
            if (Input.GetKeyDown(KeyCode.F5)) File.WriteAllText(_savePath, _session.SaveToJson());
            if (Input.GetKeyDown(KeyCode.F9) && File.Exists(_savePath))
            {
                _session.LoadFromJson(File.ReadAllText(_savePath));
                RebuildView();
            }
        }

        private void RebuildView()
        {
            _player.SnapToGrid(_session.World.Player.Position);
            _view.Rebuild(_session.World);
        }
    }
}
