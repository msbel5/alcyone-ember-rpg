using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// F10 eye-proof finding: the id-keyed sync projects actors onto the y=0 plane, so billboards sank
    /// into rising terrain and under the (hillside-floated) dungeon floor — the chamber haunters were
    /// invisible. This snaps the view to whatever it stands on (terrain, built floor) AFTER the sync,
    /// presentation-only, throttled to 4Hz + first frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BillboardGroundingView : MonoBehaviour
    {
        private float _nextProbe;
        private float _groundY;
        private bool _hasGround;

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextProbe)
            {
                _nextProbe = Time.unscaledTime + 0.25f;
                var origin = new Vector3(transform.position.x, transform.position.y + 60f, transform.position.z);
                // RaycastAll: the actor carries its own interact hitbox — skip our own hierarchy. Of the
                // remaining surfaces pick the HIGHEST that is not overhead cover (roofs, tree canopies),
                // so an indoor actor lands on the floor slab, not on the roof above it.
                var hits = Physics.RaycastAll(origin, Vector3.down, 160f);
                float best = float.MinValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    var t = hits[i].transform;
                    if (t == null || t.IsChildOf(transform)) continue;
                    // PLAYTEST FIX ("iki NPC cakisinca biri digerinin ustune cikti"): a NEIGHBOUR
                    // actor's interact hitbox counted as ground — two converging NPCs laddered up
                    // each other. Actors are never ground.
                    if (t.GetComponent<EmberCrpg.Presentation.Ember.Interaction.EmberInteractable>() != null) continue;
                    string n = t.name;
                    if (n.Contains("Roof") || n.Contains("Canopy") || n.Contains("canopy")) continue;
                    if (hits[i].point.y > best) best = hits[i].point.y;
                }
                if (best > float.MinValue)
                {
                    _groundY = best;
                    _hasGround = true;
                }
            }

            if (_hasGround && Mathf.Abs(transform.position.y - _groundY) > 0.05f)
                transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
        }
    }
}
