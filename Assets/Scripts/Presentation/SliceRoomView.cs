using EmberCrpg.Domain.World;
using UnityEngine;

// Design note:
// SliceRoomView owns room-geometry primitives and the visible south-door state.
// Inputs: parent transform, room geometry, and the current deterministic door state.
// Outputs: disposable floor, wall, and door GameObjects only.
// Bible reference: PRD Sprint 1 FR-03/FR-08, Sprint 2 FR-01/FR-02.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Builds and syncs the primitive room shell.</summary>
    public sealed class SliceRoomView
    {
        private GameObject _door;

        public void Rebuild(GameObject parent, ProceduralRoom room, bool doorOpen)
        {
            BuildFloor(parent, room);
            BuildWalls(parent, room);
            BuildDoor(parent, room);
            SyncDoor(doorOpen);
        }

        public void SyncDoor(bool doorOpen)
        {
            if (_door != null)
                _door.SetActive(!doorOpen);
        }

        private static void BuildFloor(GameObject parent, ProceduralRoom room)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(parent.transform, false);
            floor.transform.position = new Vector3((room.Width - 1) * SliceWorldView.CellSize * 0.5f, -0.1f, (room.Height - 1) * SliceWorldView.CellSize * 0.5f);
            floor.transform.localScale = new Vector3(room.Width * SliceWorldView.CellSize, 0.2f, room.Height * SliceWorldView.CellSize);
            floor.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.22f);
        }

        private static void BuildWalls(GameObject parent, ProceduralRoom room)
        {
            for (var x = 0; x < room.Width; x++)
            for (var y = 0; y < room.Height; y++)
            {
                var isPerimeter = x == 0 || y == 0 || x == room.Width - 1 || y == room.Height - 1;
                if (!isPerimeter || (x == room.DoorCell.X && y == room.DoorCell.Y))
                    continue;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(parent.transform, false);
                wall.transform.position = new Vector3(x * SliceWorldView.CellSize, 1f, y * SliceWorldView.CellSize);
                wall.transform.localScale = new Vector3(SliceWorldView.CellSize, 2f, SliceWorldView.CellSize);
                wall.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.4f);
            }
        }

        private void BuildDoor(GameObject parent, ProceduralRoom room)
        {
            _door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _door.name = "SouthDoor";
            _door.transform.SetParent(parent.transform, false);
            _door.transform.position = new Vector3(room.DoorCell.X * SliceWorldView.CellSize, 1f, room.DoorCell.Y * SliceWorldView.CellSize);
            _door.transform.localScale = new Vector3(SliceWorldView.CellSize, 2f, SliceWorldView.CellSize * 0.4f);
            _door.GetComponent<Renderer>().material.color = new Color(0.45f, 0.28f, 0.15f);
        }
    }
}
