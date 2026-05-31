using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Interaction;
using EmberCrpg.Presentation.Ember.Sprites;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// SOUL-04 (spawn-from-worldgen, BD-18): makes the generated worldgen population visible.
    ///
    /// SOUL-01's HydrateNpcs puts ~750 worldgen NPCs into WorldState.Actors, but scenes only AUTHOR a
    /// fixed cast of ~5 ActorViews, so the rest never render. EmberWorldHost.PushWorldViews already
    /// SYNCS any ActorView that carries a stable id each tick (SOUL-04 position-sync, DONE). This
    /// component closes the other half: at startup it instantiates a billboard ActorView for each nearby
    /// generated actor that has NO authored view, stamps that actor's stable id on it, and positions it
    /// by the SAME grid->world projection the adapter uses. The host then re-scans its ActorView set so
    /// the existing id-keyed sync drives SOUL-03 (ScheduleSystem) movement on the spawned views too, and
    /// the ad-hoc dialog paths fire for them via the EmberInteractable id we author here.
    ///
    /// DESIGN (conservative + additive):
    ///  - One-shot: spawns once in <see cref="SpawnMissingNearbyActors"/>, called by the host AFTER it
    ///    has cached the authored views. Re-entrant-safe (skips ids it already spawned / that already
    ///    have a view), so a second call (additive scene load, host re-run) never double-spawns.
    ///  - CAPPED: only the nearest <see cref="_maxSpawnCount"/> (&lt;=12 by default) candidates to the
    ///    player are materialised — never all 750 — so a large world cannot blow up the frame budget.
    ///  - ADDITIVE: it only ADDS GameObjects under its own root; it never touches, moves, or deletes the
    ///    authored actors, and it no-ops cleanly when there is no world / no player / no candidates.
    ///  - No Domain math here: candidate world positions arrive pre-projected as <see cref="SpawnableActor"/>
    ///    DTOs from the read model; only the stable id (a ulong) is rebuilt into an <see cref="ActorId"/>.
    ///
    /// BILLBOARD REUSE: each spawned actor mirrors the structure authored billboards use in
    /// EmberWorldspaceBuilder.SpawnActor — a root GameObject, a child "Billboard" transform (named so
    /// ActorView.Awake binds it) at local y=0.9 with a SpriteRenderer (sortingOrder 10) plus a
    /// CameraFacingBillboard, and an ActorView on the root — but built with RUNTIME APIs only (the
    /// Editor builder's AssetDatabase/SerializedObject paths are unavailable at play time).
    ///
    /// VISUAL-PROOF TODO (the only part still needing a Unity Editor screenshot): the placeholder sprite
    /// comes from the host's <see cref="SpriteRegistry"/> ("npc_placeholder", falling back to a generated
    /// 1x1 magenta texture so the billboard is never an invisible null sprite). Choosing a real per-role
    /// sprite/material and confirming the billboard's facing + world scale read correctly in-scene is the
    /// deliberately-deferred visual pass — the spawn, the stable-id stamping, the nearest-N cap, and the
    /// world positioning are all build-safe and headless-verifiable without it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberGeneratedActorSpawner : MonoBehaviour
    {
        // Cap: nearest-N generated NPCs to the player get a billboard. <=12 keeps the starting
        // settlement populated without instantiating a 750-strong crowd.
        [SerializeField] private int _maxSpawnCount = 12;

        // Sprite registry key for the placeholder character sprite. Resolved against the host's
        // registry; a miss falls back to a generated texture so the billboard always renders.
        [SerializeField] private string _placeholderSpriteKey = "npc_placeholder";

        private SpriteRegistry _spriteRegistry;
        private Sprite _fallbackSprite;
        private readonly HashSet<ulong> _spawnedIds = new HashSet<ulong>();

        /// <summary>
        /// Inject the host's sprite registry so spawned billboards reuse the same placeholder lookup
        /// authored views use. Optional: when null, every spawned billboard uses the generated fallback
        /// sprite. Safe to call before <see cref="SpawnMissingNearbyActors"/>.
        /// </summary>
        public void Configure(SpriteRegistry spriteRegistry)
        {
            _spriteRegistry = spriteRegistry;
        }

        /// <summary>
        /// Spawn a billboard for each nearby generated actor that has no authored ActorView. Returns the
        /// number of NEW billboards created this call (0 when there is nothing to do), so the host can
        /// decide whether to re-scan its view set. Idempotent: ids already spawned, or already present as
        /// an authored view, are skipped.
        /// </summary>
        public int SpawnMissingNearbyActors()
        {
            var readModel = EmberDomainAdapterLocator.WorldViewReadModel;
            if (readModel == null) return 0;

            var candidates = readModel.GetSpawnableActors();
            if (candidates == null || candidates.Count == 0) return 0;

            // Ids that already have an authored (or previously-spawned) ActorView must not be duplicated.
            // FindObjectsByType is O(scene) but runs only on this one-shot spawn, not per frame.
            var existingIds = CollectExistingViewIds();

            var anchor = ResolvePlayerAnchorXZ();

            // Build the candidate working set, skipping the empty sentinel, anything already viewed, and
            // anything we spawned on a prior call. Then order by squared distance to the player anchor and
            // take only the nearest N.
            var pending = new List<SpawnableActor>();
            foreach (var c in candidates)
            {
                if (c.Id == 0UL) continue;
                if (_spawnedIds.Contains(c.Id)) continue;
                if (existingIds.Contains(c.Id)) continue;
                pending.Add(c);
            }
            if (pending.Count == 0) return 0;

            pending.Sort((a, b) =>
                SqrDistanceXZ(a, anchor).CompareTo(SqrDistanceXZ(b, anchor)));

            int cap = Mathf.Max(0, _maxSpawnCount);
            int toSpawn = Mathf.Min(cap, pending.Count);

            int spawned = 0;
            for (int i = 0; i < toSpawn; i++)
            {
                if (SpawnOne(pending[i]))
                    spawned++;
            }
            return spawned;
        }

        private bool SpawnOne(SpawnableActor candidate)
        {
            var id = new ActorId(candidate.Id);
            string actorName = string.IsNullOrEmpty(candidate.Name) ? $"NPC {candidate.Id}" : candidate.Name;

            // Root GameObject parented under the spawner so the generated crowd is one tidy subtree and
            // never collides with authored hierarchy. Positioned by the pre-projected world XZ (Y up = 0),
            // matching DomainSimulationAdapter.ProjectActor so the first per-tick sync causes no jump.
            var root = new GameObject(actorName);
            root.transform.SetParent(transform, worldPositionStays: false);
            root.transform.position = new Vector3(candidate.WorldX, 0f, candidate.WorldZ);

            // "Billboard" child — ActorView.Awake binds it by this exact name. Same local offset and
            // SpriteRenderer sorting order as EmberWorldspaceBuilder.SpawnActor's authored billboard.
            var billboard = new GameObject("Billboard");
            billboard.transform.SetParent(root.transform, worldPositionStays: false);
            billboard.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var renderer = billboard.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolvePlaceholderSprite();
            renderer.sortingOrder = 10;
            FitBillboardToPlayableHeight(billboard.transform, renderer, 2.1f);
            billboard.AddComponent<CameraFacingBillboard>();

            // ActorView on the root, stamped with the stable id so the host's id-keyed PushWorldViews
            // sync drives this view (SOUL-03 movement). BindDomainActorId is the runtime-safe equivalent
            // of the Editor builder's SerializedObject write.
            var actorView = root.AddComponent<ActorView>();
            actorView.BindDomainActorId(id);

            // Dialog: author the stable id on an EmberInteractable too (3-arg overload built for runtime
            // spawners) so the interact raycaster prefers the id-keyed GetDialogSource(ActorId) path and
            // the ad-hoc greeting/topic dialog fires for this generated NPC.
            var interactable = root.AddComponent<EmberInteractable>();
            interactable.Setup(actorName, "General", id);

            _spawnedIds.Add(candidate.Id);
            return true;
        }

        // Collect ids carried by every ActorView already in the scene (authored or spawned). Mirrors how
        // ActorView exposes its stable id; views without an id (legacy name-keyed) simply don't contribute.
        private static HashSet<ulong> CollectExistingViewIds()
        {
            var ids = new HashSet<ulong>();
            var views = Object.FindObjectsByType<ActorView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var view in views)
            {
                if (view != null && view.HasDomainActorId)
                    ids.Add(view.DomainActorId.Value);
            }
            return ids;
        }

        // Player anchor for the nearest-N cull. Prefer the PlayerRig GameObject (the convention
        // EmberSaveService uses to find the player), then Camera.main, then world origin — always a
        // sensible, non-throwing fallback so a scene without a rig still spawns the closest-to-origin set.
        private static Vector2 ResolvePlayerAnchorXZ()
        {
            var rig = GameObject.Find("PlayerRig");
            if (rig != null)
            {
                var p = rig.transform.position;
                return new Vector2(p.x, p.z);
            }
            var cam = UnityEngine.Camera.main; // fully-qualified: EmberCrpg.Presentation.Ember.Camera namespace shadows the type
            if (cam != null)
            {
                var p = cam.transform.position;
                return new Vector2(p.x, p.z);
            }
            return Vector2.zero;
        }

        private static float SqrDistanceXZ(SpawnableActor a, Vector2 anchor)
        {
            float dx = a.WorldX - anchor.x;
            float dz = a.WorldZ - anchor.y;
            return dx * dx + dz * dz;
        }

        // Same scale-to-target-height behaviour as EmberWorldspaceBuilder so spawned billboards read at a
        // comparable size to authored ones. No-op for a null/degenerate sprite (e.g. the 1x1 fallback).
        private static void FitBillboardToPlayableHeight(Transform t, SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null) return;
            var spriteHeight = renderer.sprite.bounds.size.y;
            if (spriteHeight <= 0.001f) return;
            var scale = Mathf.Clamp(targetHeight / spriteHeight, 0.02f, 3f);
            t.localScale = new Vector3(scale, scale, scale);
        }

        private Sprite ResolvePlaceholderSprite()
        {
            if (_spriteRegistry != null)
            {
                var s = _spriteRegistry.GetSprite(_placeholderSpriteKey);
                if (s != null) return s;
            }
            return GetOrCreateFallbackSprite();
        }

        // Last-resort sprite so a spawned billboard is never an invisible null. A 1x1 magenta texture
        // doubles as a visible "placeholder art missing" flag for the deferred visual pass. Cached so we
        // allocate it at most once per spawner.
        private Sprite GetOrCreateFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                name = "EmberGeneratedActorFallback",
                filterMode = FilterMode.Point
            };
            tex.SetPixel(0, 0, new Color(1f, 0f, 1f, 1f));
            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _fallbackSprite.name = "EmberGeneratedActorFallback";
            return _fallbackSprite;
        }
    }
}
