using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using UnityEngine;

// Design note:
// SliceWorldView composes the smaller Sprint 2 presentation views into one rebuild/sync seam.
// Inputs: room, actor, pickup, and door state from the controller/session.
// Outputs: disposable Unity primitives with no gameplay rules inside the renderer.
// Bible reference: PRD Sprint 1 FR-03/FR-08, Sprint 2 FR-01.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Composite runtime renderer for the tiny vertical slice.</summary>
    public sealed class SliceWorldView
    {
        public const float CellSize = 2f;

        private readonly SliceRoomView _room = new SliceRoomView();
        private readonly SliceMarkerView _markers = new SliceMarkerView();
        private GameObject _root;

        public void Rebuild(SliceWorldState world)
        {
            if (_root != null)
                Object.Destroy(_root);
            _root = new GameObject("Sprint2SliceView");
            _room.Rebuild(_root, world.Room, world.DoorOpen);
            _markers.Rebuild(_root, world);
        }

        public void Sync(SliceWorldState world)
        {
            _room.SyncDoor(world.DoorOpen);
            _markers.Sync(world);
        }

        public static Vector3 ToWorld(GridPosition cell)
        {
            return new Vector3(cell.X * CellSize, 0.9f, cell.Y * CellSize);
        }
    }
}
