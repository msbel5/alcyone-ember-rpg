using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using UnityEngine;

// Design note:
// SliceWorldView turns the Sprint 1 pure world snapshot into disposable Unity primitives.
// Inputs: room, actor, and pickup state from the controller.
// Outputs: floor, walls, markers, and pickup visibility.
// Bible reference: PRD FR-03/FR-08.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Primitive-only runtime renderer for the tiny vertical slice.</summary>
    public sealed class SliceWorldView
    {
        public const float CellSize = 2f;

        private readonly Dictionary<ulong, GameObject> _actors = new Dictionary<ulong, GameObject>();
        private readonly Dictionary<ulong, GameObject> _pickups = new Dictionary<ulong, GameObject>();
        private GameObject _root;

        public void Rebuild(SliceWorldState world)
        {
            if (_root != null)
                Object.Destroy(_root);
            _actors.Clear();
            _pickups.Clear();

            _root = new GameObject("Sprint1SliceView");
            BuildRoom(world.Room);
            BuildActor(world.Talker, Color.cyan);
            BuildActor(world.Merchant, Color.yellow);
            BuildActor(world.Guard, Color.green);
            BuildActor(world.Enemy, Color.red);
            foreach (var pickup in world.Pickups)
                BuildPickup(pickup);
        }

        public void Sync(SliceWorldState world)
        {
            SyncActor(world.Talker);
            SyncActor(world.Merchant);
            SyncActor(world.Guard);
            SyncActor(world.Enemy);
            foreach (var pickup in world.Pickups)
                if (_pickups.TryGetValue(pickup.Item.Id.Value, out var marker))
                    marker.SetActive(!pickup.IsCollected);
        }

        public static Vector3 ToWorld(GridPosition cell)
        {
            return new Vector3(cell.X * CellSize, 0.9f, cell.Y * CellSize);
        }

        private void BuildRoom(ProceduralRoom room)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(_root.transform, false);
            floor.transform.position = new Vector3((room.Width - 1) * CellSize * 0.5f, -0.1f, (room.Height - 1) * CellSize * 0.5f);
            floor.transform.localScale = new Vector3(room.Width * CellSize, 0.2f, room.Height * CellSize);
            floor.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.22f);

            for (var x = 0; x < room.Width; x++)
            for (var y = 0; y < room.Height; y++)
            {
                var isPerimeter = x == 0 || y == 0 || x == room.Width - 1 || y == room.Height - 1;
                if (!isPerimeter || (x == room.DoorCell.X && y == room.DoorCell.Y))
                    continue;

                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(_root.transform, false);
                wall.transform.position = new Vector3(x * CellSize, 1f, y * CellSize);
                wall.transform.localScale = new Vector3(CellSize, 2f, CellSize);
                wall.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.4f);
            }
        }

        private void BuildActor(ActorRecord actor, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = actor.Name;
            marker.transform.SetParent(_root.transform, false);
            marker.GetComponent<Renderer>().material.color = color;
            _actors[actor.Id.Value] = marker;
            SyncActor(actor);
        }

        private void SyncActor(ActorRecord actor)
        {
            if (!_actors.TryGetValue(actor.Id.Value, out var marker))
                return;
            marker.transform.position = ToWorld(actor.Position);
            marker.SetActive(actor.IsAlive);
        }

        private void BuildPickup(RoomPickup pickup)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = pickup.Item.DisplayName;
            marker.transform.SetParent(_root.transform, false);
            marker.transform.localScale = Vector3.one * 0.7f;
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.1f);
            marker.transform.position = ToWorld(pickup.Position) + Vector3.up * 0.2f;
            _pickups[pickup.Item.Id.Value] = marker;
        }
    }
}
