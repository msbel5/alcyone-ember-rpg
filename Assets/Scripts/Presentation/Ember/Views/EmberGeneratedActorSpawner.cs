using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;
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
    /// SPRITE + SIZE + SCATTER (SOUL-04 visual fix): each billboard draws from Generated/Core by role
    /// (npc_guard, npc_sage, ...). Hand-authored sprites are deliberately not a normal path.
    /// Missing generated art falls back to a small neutral quad so failures are visible without reintroducing
    /// the old Art/Characters dependency.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberGeneratedActorSpawner : MonoBehaviour
    {
        // Cap: nearest-N generated NPCs to the player get a billboard. A small cap keeps the starting
        // settlement populated (and visually readable) without instantiating a 750-strong crowd or a
        // pile of overlapping billboards.
        [SerializeField] private int _maxSpawnCount = 24;

        // On-screen billboard height in world units. Mirrors EmberWorldspaceBuilder.SpawnActor's
        // FitBillboardToPlayableHeight target (2.1) so spawned NPCs read at the SAME size as authored
        // actors — never giant.
        [SerializeField] private float _billboardTargetHeight = 2.1f;

        // World-unit spacing between spawned NPCs. Worldgen seeds most NPCs at the settlement origin
        // tile, so without an offset every billboard would stack on the same XZ (and on the player).
        // We fan them out on a ring/grid by spawn index using this spacing; ~1.5u clears 2.1u-tall
        // billboards comfortably.
        [SerializeField] private float _spawnSpacing = 1.5f;

        private static readonly HashSet<string> LoggedSpriteResolutions = new HashSet<string>();
        private Sprite _fallbackSprite;
        private readonly HashSet<ulong> _spawnedIds = new HashSet<ulong>();

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

            // Kind-aware density from the director (an Inn spawns a handful, a City a crowd); falls back to
            // the serialized default when no realize has set it (baked scenes).
            int cap = Mathf.Max(0, EmberCrpg.Presentation.Ember.WorldDirector.RuntimeNpcDensity.CapOrDefault(_maxSpawnCount));
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

            // F6/night staging: citizens leave the street 22:00–06:00; guards and outlaws keep prowling.
            var curfew = root.AddComponent<EmberCrpg.Presentation.Ember.WorldDirector.NightCurfewView>();
            var spriteRole = candidate.SpriteRole ?? string.Empty;
            // F29: bestiary monsters ("monster_*") are hostile by definition.
            bool hostileRole = spriteRole.IndexOf("outlaw", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || spriteRole.IndexOf("bandit", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || spriteRole.StartsWith("monster_", System.StringComparison.OrdinalIgnoreCase);
            curfew.Prowler = hostileRole
                          || spriteRole.IndexOf("guard", System.StringComparison.OrdinalIgnoreCase) >= 0;

            // "Billboard" child — ActorView.Awake binds it by this exact name. Same local offset and
            // SpriteRenderer sorting order as EmberWorldspaceBuilder.SpawnActor's authored billboard.
            var billboard = new GameObject("Billboard");
            billboard.transform.SetParent(root.transform, worldPositionStays: false);
            billboard.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var renderer = billboard.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolvePlaceholderSprite(candidate);
            renderer.sortingOrder = 10;
            // F29: monsters keep their own stature — a wolf is hip-high, a wisp looms.
            FitBillboardToPlayableHeight(billboard.transform, renderer,
                BestiaryBillboardSpriteFactory.TargetHeightFor(spriteRole, _billboardTargetHeight));
            var facing = billboard.AddComponent<CameraFacingBillboard>();

            // F10 hit feel: every spawned actor can flash on a landed strike and fall flat on death.
            root.AddComponent<ActorCombatFeedbackView>().Bind(candidate.Id, renderer, facing);
            root.AddComponent<NpcEventEchoView>().Bind(candidate.Id); // M6: real events float up
            // F33: the two-frame walk — mirror-swap gait while the root glides.
            root.AddComponent<BillboardWalkAnimView>().Bind(renderer);

            // F10 ("savaşamadım"): hostiles must READ hostile from across the street — a red diamond
            // floats over outlaw/bandit billboards (root-parented, unscaled; faces the camera itself).
            if (hostileRole)
                AddHostileMarker(root.transform);

            // ActorView on the root, stamped with the stable id so the host's id-keyed PushWorldViews
            // sync drives this view (SOUL-03 movement). BindDomainActorId is the runtime-safe equivalent
            // of the Editor builder's SerializedObject write.
            var actorView = root.AddComponent<ActorView>();
            actorView.BindDomainActorId(id);
            // Walk speed ~1.2 m/s, matched to the colony schedule's 1 tile / 0.83 s tick, so the billboard
            // glides continuously with the sim instead of stride-then-pausing (combat keeps the snap chase).
            // F14: hostiles glide at chase speed (sim steps 1 cell / 0.45s ≈ 2.2 m/s) so the view keeps up.
            actorView.SetGroundSpeed(hostileRole ? 3.4f : 1.3f); // F18: diagonal chase steps peak 3.13 m/s
            // The colony schedule now disperses NPCs across the (enlarged) settlement and walks each between its
            // home and a DISTINCT day-spot. On top of that purposeful motion, a small idle mill keeps them
            // subtly moving at their current spot so the town reads as alive instead of frozen statues. Visual
            // only — never written back to the sim.
            // F18: NOT for hostiles — a monster idles still and chases with PURPOSE (DOOM/Daggerfall);
            // the ±2.2m mill made pursuers read drunk and ate ~1.5m of the measured chase closure.
            if (!hostileRole)
            {
                actorView.EnableWander(2.2f);
                // F27: pose pictograms — a hammer over workers in work hours, a mug over everyone
                // at the midday meal (the schedule's lunch window).
                bool workerRole = spriteRole.IndexOf("farmer", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || spriteRole.IndexOf("blacksmith", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || spriteRole.IndexOf("artisan", System.StringComparison.OrdinalIgnoreCase) >= 0;
                root.AddComponent<NpcPoseIconView>().Bind(workerRole);
                root.AddComponent<NpcActivityLabelView>().Bind(); // PLAYTEST FIX: says what they are doing
            }
            root.AddComponent<GeneratedNpcAccessibilityGuard>();
            // F10: the sync plane is y=0 — ground the view on what it actually stands on (terrain or a
            // built floor) so hillside NPCs and the floated dungeon chamber's haunters stay visible.
            root.AddComponent<BillboardGroundingView>();

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

        // F10 hostility marker: a small red diamond (rotated quad) hovering over the head. Geometry +
        // unlit-solid material per the banked rule (runtime alpha-clip materials strip in player builds);
        // its collider is destroyed so it can never eat the interact raycast meant for the actor.
        private static void AddHostileMarker(Transform actorRoot)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "HostileMarker";
            quad.transform.SetParent(actorRoot, worldPositionStays: false);
            quad.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            quad.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            var markerRenderer = quad.GetComponent<MeshRenderer>();
            if (markerRenderer != null)
                markerRenderer.sharedMaterial = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeMaterialPalette.Solid(
                    new Color(0.85f, 0.12f, 0.10f));
            quad.AddComponent<CameraFacingBillboard>();
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
        // STREAMING RESPAWN ("quest 250m diyor ama orası boş"): the original call was one-shot at host init,
        // so NPCs that become nearby only after the player WALKS never materialized. SpawnMissingNearbyActors
        // is re-entrant and capped, so re-scanning when the player has moved far enough streams billboards in
        // as you walk — Daggerfall-style — without ever double-spawning.
        private Vector2 _lastScanAnchor = new Vector2(float.MinValue, float.MinValue);
        private float _nextScanTime;
        private const float ScanIntervalSeconds = 2.5f;
        private const float ScanMoveThresholdMeters = 40f;

        private void Update()
        {
            if (Time.unscaledTime < _nextScanTime) return;
            _nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
            var anchor = ResolvePlayerAnchorXZ();
            if ((anchor - _lastScanAnchor).sqrMagnitude < ScanMoveThresholdMeters * ScanMoveThresholdMeters) return;
            _lastScanAnchor = anchor;
            SpawnMissingNearbyActors();
        }

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

        // Normal path: generated AI billboard from Assets/Generated/Core. Last resort: neutral runtime quad.
        private Sprite ResolvePlaceholderSprite(SpawnableActor candidate)
        {
            var generated = ResolveGeneratedSprite(candidate);
            if (generated != null) return generated;
            // F29: bestiary roles fall back to their generated SILHOUETTE, never the neutral grey —
            // a wolf must read as a wolf even with the forge off (library wins when it exists).
            var silhouette = BestiaryBillboardSpriteFactory.For(candidate.SpriteRole);
            if (silhouette != null)
            {
                LogSpriteResolution(candidate.SpriteRole, "bestiary-silhouette", "runtime-pixel-mask");
                return silhouette;
            }
            LogSpriteResolution(candidate.SpriteRole, "missing", "neutral-runtime-placeholder");
            return GetOrCreateFallbackSprite();
        }

        private static Sprite ResolveGeneratedSprite(SpawnableActor candidate)
        {
            var database = GeneratedAssetRuntimeDatabase.Current;
            if (GeneratedNpcBillboardResolver.TryResolveRecord(database, candidate.SpriteRole, candidate.Seed, out var record))
            {
                var librarySprite = GeneratedCoreSpriteLoader.TryLoadRelativeSprite(
                    string.IsNullOrWhiteSpace(record.spritePath) ? record.relativeAssetPath : record.spritePath,
                    record.stableId);
                if (librarySprite != null)
                {
                    LogSpriteResolution(candidate.SpriteRole, "library", string.IsNullOrWhiteSpace(record.spritePath) ? record.relativeAssetPath : record.spritePath);
                    return librarySprite;
                }
            }

            var fallbackCoreId = GeneratedNpcBillboardResolver.BuildFallbackCoreId(candidate.SpriteRole);
            var fallback = string.IsNullOrEmpty(fallbackCoreId) ? null : GeneratedCoreSpriteLoader.TryLoadPortrait(fallbackCoreId);
            if (fallback != null)
                LogSpriteResolution(candidate.SpriteRole, "core", "Assets/Generated/Core/" + fallbackCoreId + ".png");
            return fallback;
        }

        private static void LogSpriteResolution(string role, string source, string path)
        {
            var key = (role ?? string.Empty) + "|" + source + "|" + (path ?? string.Empty);
            if (!LoggedSpriteResolutions.Add(key)) return;
            Debug.Log("[NpcBillboardResolve] role=" + (role ?? string.Empty) + " source=" + source + " file=" + (path ?? string.Empty));
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
