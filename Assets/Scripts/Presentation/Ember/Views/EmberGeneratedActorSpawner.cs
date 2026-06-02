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
    ///  - CAPPED: only the nearest <see cref="_maxSpawnCount"/> (6 by default) candidates to the
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
    /// SPRITE + SIZE + SCATTER (SOUL-04 visual fix): each billboard draws a REAL character sprite from
    /// the host's <see cref="SpriteRegistry"/> using the SAME keys authored ActorViews use (see
    /// <see cref="_placeholderSpriteKeys"/>; "blacksmith" is EmberWorldspaceBuilder's universal fallback,
    /// so it is effectively guaranteed). It is sized by <see cref="FitBillboardToPlayableHeight"/> to the
    /// same ~2.1u height as authored actors, and fanned out on concentric rings by spawn index so the
    /// co-located worldgen crowd does not stack on one tile / the camera. The earlier build resolved an
    /// UNREGISTERED "npc_placeholder" key, which fell back to a 1x1 magenta texture that scaled into a
    /// screen-filling shape — that path is gone; the only remaining fallback is a small neutral quad sized
    /// to the same height, used solely when no registry is wired.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberGeneratedActorSpawner : MonoBehaviour
    {
        // Cap: nearest-N generated NPCs to the player get a billboard. A small cap keeps the starting
        // settlement populated (and visually readable) without instantiating a 750-strong crowd or a
        // pile of overlapping billboards.
        [SerializeField] private int _maxSpawnCount = 10;

        // On-screen billboard height in world units. Mirrors EmberWorldspaceBuilder.SpawnActor's
        // FitBillboardToPlayableHeight target (2.1) so spawned NPCs read at the SAME size as authored
        // actors — never giant.
        [SerializeField] private float _billboardTargetHeight = 2.1f;

        // World-unit spacing between spawned NPCs. Worldgen seeds most NPCs at the settlement origin
        // tile, so without an offset every billboard would stack on the same XZ (and on the player).
        // We fan them out on a ring/grid by spawn index using this spacing; ~1.5u clears 2.1u-tall
        // billboards comfortably.
        [SerializeField] private float _spawnSpacing = 1.5f;

        // Sprite registry keys, in priority order, resolved against the host's registry. These are
        // REAL character-sprite file names that SpriteRegistryAutoBuilder always writes from
        // Assets/Art/Characters — the SAME source authored ActorViews draw from. "blacksmith" is the
        // authored universal fallback (EmberWorldspaceBuilder.ResolveSpriteAlias's default), so it is
        // effectively guaranteed present. The first hit wins; we NEVER use an unregistered key (which
        // would yield the 1x1 magenta "missing texture" placeholder that previously stretched into a
        // screen-filling shape).
        [SerializeField]
        private string[] _placeholderSpriteKeys =
        {
            "blacksmith", "merchant", "innkeeper", "warrior", "knight"
        };

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
                if (SpawnOne(pending[i], spawned))
                    spawned++;
            }
            return spawned;
        }

        private bool SpawnOne(SpawnableActor candidate, int spawnIndex)
        {
            var id = new ActorId(candidate.Id);
            string actorName = string.IsNullOrEmpty(candidate.Name) ? $"NPC {candidate.Id}" : candidate.Name;

            // Root GameObject parented under the spawner so the generated crowd is one tidy subtree and
            // never collides with authored hierarchy. Base position is the pre-projected world XZ (Y up = 0),
            // matching DomainSimulationAdapter.ProjectActor. Worldgen seeds most NPCs at the SAME settlement
            // tile, so we add a deterministic per-index ring/grid offset to spread them out — otherwise every
            // billboard (and the player) would stack on one point. The first per-tick id-keyed sync may later
            // pull each view to its authored grid position; until the sim moves them this keeps them legible.
            var offset = SpawnOffset(spawnIndex);
            var root = new GameObject(actorName);
            root.transform.SetParent(transform, worldPositionStays: false);
            root.transform.position = new Vector3(candidate.WorldX + offset.x, 0f, candidate.WorldZ + offset.y);

            // "Billboard" child — ActorView.Awake binds it by this exact name. Same local offset and
            // SpriteRenderer sorting order as EmberWorldspaceBuilder.SpawnActor's authored billboard.
            var billboard = new GameObject("Billboard");
            billboard.transform.SetParent(root.transform, worldPositionStays: false);
            billboard.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var renderer = billboard.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolvePlaceholderSprite();
            renderer.sortingOrder = 10;
            FitBillboardToPlayableHeight(billboard.transform, renderer, _billboardTargetHeight);
            billboard.AddComponent<CameraFacingBillboard>();

            // ActorView on the root, stamped with the stable id so the host's id-keyed PushWorldViews
            // sync drives this view (SOUL-03 movement). BindDomainActorId is the runtime-safe equivalent
            // of the Editor builder's SerializedObject write.
            var actorView = root.AddComponent<ActorView>();
            actorView.BindDomainActorId(id);
            // Cosmetic idle wander so the co-located worldgen crowd spreads out and looks alive instead of a
            // static clump on the settlement tile. Purely visual (never written to the sim); the id-keyed
            // sync still relocates the NPC when the sim actually moves it.
            actorView.EnableWander(4f);

            // Dialog: author the stable id on an EmberInteractable too (3-arg overload built for runtime
            // spawners) so the interact raycaster prefers the id-keyed GetDialogSource(ActorId) path and
            // the ad-hoc greeting/topic dialog fires for this generated NPC.
            var interactable = root.AddComponent<EmberInteractable>();
            interactable.Setup(actorName, "General", id);

            // The interact raycaster needs a collider to hit. Authored ActorViews ship one, so the baked
            // scenes worked; spawned worldgen NPCs had none — which is exactly why pressing E on an NPC in
            // the generated world opened no dialogue. Add a person-sized box so interaction works.
            var hitBox = root.AddComponent<BoxCollider>();
            hitBox.center = new Vector3(0f, 0.9f, 0f);
            hitBox.size = new Vector3(0.8f, 1.8f, 0.8f);

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

        // Deterministic XZ offset (world units) per spawn index so co-located worldgen NPCs fan out
        // instead of stacking on one tile / the player. Index 0 sits at the origin; the rest spiral
        // outward on concentric square rings (8, 16, 24 ... slots per ring) — a compact, overlap-free
        // settlement scatter that needs no scene data and is stable across runs. Spacing comes from
        // _spawnSpacing so the spread always clears the billboard footprint.
        private Vector2 SpawnOffset(int index)
        {
            float spacing = Mathf.Max(0f, _spawnSpacing);
            if (index <= 0 || spacing <= 0f) return Vector2.zero;

            // Ring r (1-based) holds 8*r slots; find the ring this index lands in.
            int ring = 1;
            int remaining = index;
            while (remaining > 8 * ring)
            {
                remaining -= 8 * ring;
                ring++;
            }

            // Position evenly around this ring's perimeter, radius = ring * spacing.
            int slots = 8 * ring;
            float angle = (Mathf.PI * 2f) * ((remaining - 1) / (float)slots);
            float radius = ring * spacing;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
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

        // Resolve the SAME sprite source authored ActorViews use: a real key in the host's registry,
        // which SpriteRegistryAutoBuilder populates from Assets/Art/Characters. We try each configured
        // key in order and take the first hit; "blacksmith" (the authored universal fallback) is
        // effectively guaranteed, so a spawned billboard almost always gets real character art. Only if
        // the registry is missing/empty do we fall back to a generated sprite — and that sprite is sized
        // to read at the SAME height as the others, so it can never balloon to fill the screen.
        private Sprite ResolvePlaceholderSprite()
        {
            if (_spriteRegistry != null && _placeholderSpriteKeys != null)
            {
                for (int i = 0; i < _placeholderSpriteKeys.Length; i++)
                {
                    var key = _placeholderSpriteKeys[i];
                    if (string.IsNullOrEmpty(key)) continue;
                    if (IsPortraitKey(key)) continue;
                    var s = _spriteRegistry.GetSprite(key);
                    if (s != null) return s;
                }
            }
            return GetOrCreateFallbackSprite();
        }

        private static bool IsPortraitKey(string key)
        {
            return key.IndexOf("portrait", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Last-resort sprite for the (rare) case where no registry sprite resolves, so a billboard is
        // never an invisible null. Deliberately built as a small, neutral-grey 1-world-unit quad: a 64px
        // texture at 64 PPU gives bounds.size.y == 1, so FitBillboardToPlayableHeight scales it to the
        // SAME ~2.1u height as every other actor — NEVER the giant magenta the old 1x1 placeholder
        // produced. Cached so we allocate it at most once per spawner.
        private Sprite GetOrCreateFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = "EmberGeneratedActorFallback",
                filterMode = FilterMode.Bilinear
            };
            var fill = new Color32(110, 110, 120, 255);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            tex.SetPixels32(pixels);
            tex.Apply();
            // pixelsPerUnit == size -> sprite bounds are exactly 1x1 world unit (same convention the
            // height-fit math expects); the billboard is then scaled to _billboardTargetHeight.
            _fallbackSprite = Sprite.Create(
                tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _fallbackSprite.name = "EmberGeneratedActorFallback";
            return _fallbackSprite;
        }
    }
}
