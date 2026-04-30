using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using UnityEngine;

// Design note:
// SliceMarkerView owns actor and pickup marker primitives for the runtime slice.
// Inputs: parent transform plus current actor/pickup state from the pure world snapshot.
// Outputs: disposable markers whose transforms and active state mirror deterministic data.
// Bible reference: PRD Sprint 1 FR-03/FR-08, Sprint 2 FR-01.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Builds and syncs actor/pickup markers without gameplay logic.</summary>
    public sealed class SliceMarkerView
    {
        private readonly Dictionary<ulong, GameObject> _actors = new Dictionary<ulong, GameObject>();
        private readonly Dictionary<ulong, GameObject> _pickups = new Dictionary<ulong, GameObject>();

        public void Rebuild(GameObject parent, SliceWorldState world)
        {
            _actors.Clear();
            _pickups.Clear();
            BuildActor(parent, world.Talker, Color.cyan);
            BuildActor(parent, world.Merchant, Color.yellow);
            BuildActor(parent, world.Guard, Color.green);
            BuildActor(parent, world.Enemy, Color.red);
            foreach (var pickup in world.Pickups)
                BuildPickup(parent, pickup);
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

        private void BuildActor(GameObject parent, ActorRecord actor, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = actor.Name;
            marker.transform.SetParent(parent.transform, false);
            marker.GetComponent<Renderer>().material.color = color;
            _actors[actor.Id.Value] = marker;
            SyncActor(actor);
        }

        private void SyncActor(ActorRecord actor)
        {
            if (_actors.TryGetValue(actor.Id.Value, out var marker))
            {
                marker.transform.position = SliceWorldView.ToWorld(actor.Position);
                marker.SetActive(actor.IsAlive);
            }
        }

        private void BuildPickup(GameObject parent, RoomPickup pickup)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = pickup.Item.DisplayName;
            marker.transform.SetParent(parent.transform, false);
            marker.transform.localScale = Vector3.one * 0.7f;
            marker.transform.position = SliceWorldView.ToWorld(pickup.Position) + Vector3.up * 0.2f;
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.1f);
            _pickups[pickup.Item.Id.Value] = marker;
        }
    }
}
