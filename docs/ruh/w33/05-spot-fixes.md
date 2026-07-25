# W33-05 — Four independent spot-fixes (B03, B17, B18, guards-eat)

Scope: four surgical fixes queued out of the W32 triage (`docs/ruh/w32/00-bug-triage.md`).
Each is independently landable — disjoint files, no ordering dependency between them.
Constraints unchanged from the constitution: determinism (pure functions of WorldState +
Stamp, no wall-clock, no unseeded RNG), save compat (all-zero `ActorActionState` still
extends Idle; no new save fields anywhere below), chunking invariance is the referee
(`CadenceChunkingInvarianceTests` / `EatChunkingPhaseTraceTests` must stay green), low LOC,
and every new code comment states the constraint it protects.

All anchors below are verbatim from the CURRENT working tree (verified 2026-07-25, on top of
W32 commit `5049d445`).

---

## Fix 1 — B03: the ownership lint is a fake seatbelt (critical→high)

### Current code

`Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs:14-22` — the "known ids"
are HAND-TYPED:

```csharp
var knownIds = new[]
{
    "core.time", "core.magic", "living.schedule", "living.companion_follow",
    "living.decision", "living.action_advance", "econ.jobs", "quest.tick", "living.needs",
    "living.consumption", "living.predation", "living.companion_guard",
    "living.witness", "living.ambient", "living.rumors",
    "world.growth", "world.harvest", "econ.prices", "econ.trade",
    "world.shortage", "world.history", "econ.caravan", "faction.decay",
};
```

The real registered ids are the 22 `: base("…")` calls in
`Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs` (grepped, every one):

| line | id | slot |
|---|---|---|
| :98 | `core.time` | PerTick:10 |
| :116 | `core.magic` | PerTick:20 |
| :134 | `econ.jobs` | Hourly:10 |
| :236 | `living.schedule` | PerTick:20 |
| :257 | `living.decision` | PerTick:18 |
| :274 | `living.action_advance` | PerTick:22 |
| :288 | `quest.tick` | Hourly:15 |
| :305 | `living.ambient` | Hourly:50 |
| :317 | `living.rumors` | Hourly:55 |
| :330 | `living.consumption` | Hourly:35 |
| :344 | `living.companion_follow` | PerTick:21 |
| :352 | `living.companion_guard` | Hourly:42 |
| :361 | `living.predation` | Hourly:40 |
| :370 | `living.witness` | Hourly:45 |
| :381 | `econ.shortage_response` | Daily:27 |
| :394 | `living.needs` | Hourly:30 |
| :439 | `world.caravans` | Daily:10 |
| :457 | `world.harvest` | Daily:25 |
| :513 | `econ.plantgrowth` | Daily:20 |
| :547 | `world.runtime_history` | Daily:28 |
| :556 | `econ.prices` | Daily:30 |
| :615 | `politics.faction_decay` | Daily:40 |

Diff of the two sets: the hand-typed list carries **six ghost ids** that match NO registered
system (`econ.trade`, `world.growth`, `world.shortage`, `world.history`, `econ.caravan`,
`faction.decay`) and is **missing five real ids** (`world.caravans`, `econ.plantgrowth`,
`econ.shortage_response`, `world.runtime_history`, `politics.faction_decay`). The lint
therefore blesses the ghost ledger row it exists to catch:

`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:54-60` (`World.Stockpiles`):

```csharp
["World.Stockpiles"] = new[]
{
    "world.harvest@Daily:25",
    "living.action_advance@PerTick:22", // W32: TakeFood decrement + failure return
    "living.ambient@Hourly:50",   // vermin theft
    "econ.trade@Daily:28",
},
```

`econ.trade@Daily:28` is a ghost: no such system exists at any cadence. The REAL daily
stockpile trader is `world.caravans@Daily:10` — `CaravanSystem.cs:50`
(`var loaded = origin?.Remove(route.ItemTag, route.QuantityPerCaravan) ?? 0;`) and `:92`
(`destination.Add(route.ItemTag, caravan.PayloadRemaining);`), wired through
`world.FindStockpile` at `DefaultTickSystems.cs:448`.

### Fix design

1. **Delete the hand-typed list; derive from the single source.** The construction fixture
   already exists at `WorldTickRegistryTests.cs:46-58`
   (`DefaultTickSystems.Create(new GameTimeAdvanceSystem(DefaultCalendar()), …)`). Extract it
   into a shared test helper — `Assets/Tests/EditMode/Composition/DefaultRegistryFixture.cs`,
   `internal static WorldTickRegistry CreateDefault()` — moving `DefaultCalendar()` and
   `DefaultPlantSpecies()` (`WorldTickRegistryTests.cs:91-120`) with it. Both test classes
   call the fixture. Calling `Create` and reading `registry.Ordered` IS "reading every
   `base(...)` id" — at runtime, from the composition root, immune to future registrations.

2. **Lint the FULL slot triple, not the bare id** (the ledger row format is already
   `"systemId@Cadence:Order"` per `FieldOwnershipRegistry.cs:14`), which is simultaneously
   the required **reverse lint — a declared writer with no real system at that exact slot
   fails**:

```csharp
[Test]
public void EveryDeclaredWriter_IsARealRegisteredSystem_AtItsDeclaredSlot()
{
    // B03: the known-id set is DERIVED from the composition root — a hand-typed list
    // rotted into six ghosts and blessed the econ.trade@Daily:28 phantom writer.
    var registered = DefaultRegistryFixture.CreateDefault().Ordered
        .Select(s => $"{s.Id}@{s.Cadence}:{s.Order}")
        .ToHashSet();
    var ghosts = FieldOwnershipRegistry.Writers
        .SelectMany(kv => kv.Value)
        .Distinct()
        .Where(w => !registered.Contains(w))
        .ToList();
    Assert.That(ghosts, Is.Empty,
        "ownership ledger declares writers with no real registered system at that slot: "
        + string.Join(", ", ghosts));
}
```

   This replaces `EveryDeclaredWriter_ExistsInTheTickRegistry` outright (the derived
   full-triple check subsumes the bare-id check) and additionally catches cadence/order
   drift — a writer moved from `Hourly:35` to `Hourly:36` without a ledger update now fails.
   `TickCadence` enum `ToString()` matches the ledger strings (`PerTick`/`Hourly`/`Daily`),
   already relied on by the canonical-order pin at `WorldTickRegistryTests.cs:60-88`.

3. **Fix the ledger row the honest lint exposes**: in `FieldOwnershipRegistry.cs:59` replace
   `"econ.trade@Daily:28",` with `"world.caravans@Daily:10", // caravan load/unload (CaravanSystem :50/:92)`.
   Verified: with this one substitution, every other declared triple in the ledger matches a
   registered slot — the new lint goes green with exactly this diff.

Out of scope (explicitly): the OTHER direction — a registered system that writes a field
without a ledger row — is B04 (`later-slice`, needs per-system write-site analysis). This fix
only makes the declared→registered direction real.

### Blast radius
Tests + one ledger string. No simulation behavior change; `CoreMutableFields_HaveDeclaredOwnership`
(`FieldOwnershipRegistryTests.cs:33-41`) unchanged.

---

## Fix 2 — B17: the 8x8 grey placeholder must never be stamped fresh (high)

### Current code

The placeholder is born honest — `OnnxAssetForge.cs:119-131`:

```csharp
if (placeholder)
{
    var bytes = PlaceholderPng(request);
    ...
    return new AssetGenerationResult(
        request.RequestId, bytes, "image/png", stopwatch.ElapsedMilliseconds,
        true,
        string.IsNullOrEmpty(initError) ? "placeholder" : initError,
        isPlaceholder: true); // EMB-042: mark provenance — these bytes are a fallback, not a real generation.
}
```

with `PlaceholderPng` at `:164-168` = `OnnxPngEncoder.EncodeRgba(8, 8, BuildSolidGrayRgba(8, 8, gray))`
— the 8x8 solid grey. `AssetGenerationResult.cs:36-41` even documents the trap: *"Success can
be true for a placeholder, so callers/loading log must check this."*

**The stamp site ignores it.** `VisibleGenerationPipeline.cs` success path `:75-82`:

```csharp
if (result.Success)
{
    Write(entry, result.ImageBytes);
    succeeded++;
    if (result.IsPlaceholder) placeholders++;   // EMB-042: provenance — fallback, not real gen
    ...
```

`IsPlaceholder` is only COUNTED; `Write` never learns it — `:136-142`:

```csharp
private void Write(ManifestEntry entry, byte[] bytes)
{
    var fullPath = AssetManifestScanner.Resolve(_projectRoot, entry.ExpectedPath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
    File.WriteAllBytes(fullPath, bytes ?? Array.Empty<byte>());
    if (entry.RequiresGeneration) GeneratedAssetProvenance.Write(fullPath, entry, _catalog);
}
```

Line `:141` is THE stamp site: `GeneratedAssetProvenance.Write` emits the `.promptmeta`
sidecar with the current `version` + `promptHash` (`GeneratedAssetProvenance.cs:60-68`), so
`IsFresh` returns true forever (`:38-51`) and `AssetManifestScanner.ScanAsync` reports the
8x8 grey as `Cached` (`AssetManifestScanner.cs:24-30`). Installing a real model later never
regenerates anything.

### Fix design

Thread the provenance bit into the write — three lines:

```csharp
// :77
Write(entry, result.ImageBytes, result.IsPlaceholder);

// :136
private void Write(ManifestEntry entry, byte[] bytes, bool isPlaceholder)
{
    ...
    // B17: a placeholder is a visible stand-in, never provenance. No .promptmeta means
    // IsFresh stays "stale_missing_provenance" and the scanner retries the entry the
    // moment a real model exists — stamping it would freeze the 8x8 grey as canonical.
    if (entry.RequiresGeneration && !isPlaceholder)
        GeneratedAssetProvenance.Write(fullPath, entry, _catalog);
}
```

The PNG is still written (the loading run still shows SOMETHING and `Placeholders` in
`PipelineResult` still counts it); only the freshness stamp is withheld.
`GeneratedAssetProvenance.IsFresh` already returns exactly the right verdict for a
stampless file: `if (!File.Exists(stampPath)) { reason = "stale_missing_provenance"; return false; }`
(`GeneratedAssetProvenance.cs:23-24`). No scanner change needed.

**Already-poisoned installs**: `.promptmeta` files stamped next to placeholders before this
fix remain "fresh". Considered and rejected: (a) bumping `GeneratedAssetProvenance.Version`
(`:15`, `"real-images-v4"`) — invalidates every REAL generated asset too; (b) an
`IsFresh` byte-size/8x8 heuristic — false-staleness risk on legitimately tiny assets.
Decision: accept and document — deleting the generated-assets folder (or the sidecars) is
the user-level remediation; new installs can no longer be poisoned.

### Tests
Extend `Assets/Tests/EditMode/Generation/VisibleGenerationPipelineTests.cs` with a stub
forge returning `Success=true, IsPlaceholder=true`:
- PNG exists at `ExpectedPath`, `.promptmeta` ABSENT, and a follow-up
  `AssetManifestScanner.ScanAsync` (with catalog) reports `RequiresGeneration` /
  `stale_missing_provenance`.
- Control: `IsPlaceholder=false` still stamps and rescans as `Cached` (pins the happy path
  so the new flag can't over-suppress).

### Blast radius
One private method signature + one call site inside `VisibleGenerationPipeline` (the class
has no other `Write` callers) + tests. `ForgeMenu`'s `AssetForgeCache` path is separate and
already guarded by `forge.IsAvailable()` (`OnnxAssetForge.cs:95` returns
`!_placeholderMode && …`, checked at `ForgeMenu.cs:33`), so placeholders don't reach that
cache today.

---

## Fix 3 — B18: the forge cache key ignores W/H/negative/steps (medium)

### Current code

The hash site — `Assets/Scripts/Simulation/Forge/PromptComposers.cs:47-51`:

```csharp
public static string CacheKey(AssetGenerationRequest request)
{
    if (request == null) throw new ArgumentNullException(nameof(request));
    return PromptHash.Sha256(request.Prompt + "|" + request.Style + "|" + request.Seed);
}
```

Consumers: `AssetForgeCache.PathFor` (`AssetForgeCache.cs:21`,
`Path.Combine(_root, PromptComposers.CacheKey(request) + ".png")` under
`persistentDataPath/forge-cache`, `:14`) and the editor bake `ForgeMenu.cs:42`, which also
persists the key as `portraitAssetPath` on the NPC seed record.

`AssetGenerationRequest` carries exactly the fields the key ignores
(`AssetGenerationRequest.cs:60-70`): `Width`, `Height`, `NegativePrompt`, and `Steps`
(*"Diffusion fidelity steps — a CONFIG VARIABLE (default 1) … set it per AssetKind via
ImageGenKindTemplate / ImageGenSpec.Steps"*). Retuning any of them — a portrait respec from
1024 to 512, a `BaseNegative` edit, a per-kind steps bump — leaves the key identical and
serves stale pixels as cache hits.

### Fix design — cache-invalidation-safe key change

```csharp
public static string CacheKey(AssetGenerationRequest request)
{
    if (request == null) throw new ArgumentNullException(nameof(request));
    // B18: every field that changes the pixels is IN the key — W/H/negative/steps were
    // invisible, so retuning them served stale art as cache hits. "v2|" names the key
    // schema: widening the key again is a one-character bump, and every v1 entry becomes
    // a clean MISS (never a wrong hit).
    return PromptHash.Sha256(
        "v2|" + request.Prompt + "|" + request.Style + "|" + request.Seed
        + "|" + request.Width + "x" + request.Height
        + "|" + request.NegativePrompt
        + "|" + request.Steps);
}
```

Determinism: still a pure function of the request — SHA-256 over an ordinal string;
`TickCadence`-style enum and digit formatting are culture-stable (`Width`/`Height` are
validated positive, no sign-symbol variance).

**Invalidation semantics (the point of the design)**: keys are cache FILENAMES, so a key
change can only produce misses, never mismatched hits. Every pre-fix entry in
`forge-cache/` becomes unreachable and is regenerated on next demand.

**Disk impact (explicit)**: orphaned v1 PNGs are NOT deleted — at 1024px they are MB-scale
each, so a fully baked NPC roster leaves a full bake's worth of dead bytes in
`persistentDataPath/forge-cache` until the user clears the folder. No GC in scope
(deliberate: startup deletion could race an in-progress editor bake; the folder is
documented as safe to delete wholesale).

**Seed-record note**: `portraitAssetPath` values already saved by `ForgeMenu` keep their v1
strings, and the v1 files they point at still exist — old worlds keep resolving. New bakes
emit v2 keys. The key is opaque data on the record; no save-schema change.

**Known residual gap (deferred, named here so it isn't rediscovered)**: `ModelHint` also
changes pixels and stays outside the key. Folding it in means another full-cache churn;
do it as `v3` if/when model switching actually ships. `TimeoutSeconds` is correctly
excluded — it never changes output pixels.

### Tests
`Assets/Tests/EditMode/Forge/PromptComposerTests.cs` (or `AssetForgeCacheTests.cs`):
else-equal requests differing only in `Width` ⇒ different key; same for `Height`,
`NegativePrompt`, `Steps`; two identical requests ⇒ identical key (pins determinism and the
schema tag).

### Blast radius
One method body + tests. `AssetForgeCache` and `ForgeMenu` consume the key opaquely.

---

## Fix 4 — guards-eat: pursuit-aware EatIntent eligibility (B09 remainder)

### Current code — and a finding the queue text got backwards

The W32 eligibility gate, `Assets/Scripts/Simulation/Living/Actions/ActionLifecycleSystem.cs:42-46`
(the Decide phase, `living.decision@PerTick:18`):

```csharp
if (actor == null || !actor.IsAlive) continue;
if (actor.Role == ActorRole.Player || actor.Role == ActorRole.Enemy) continue;
// One gate covers Running AND the one-advancement terminal handover states.
if (actor.ActionState.CurrentAction != ActorActionType.None) continue;
if (actor.Needs.Hunger.Value < NeedConsumptionSystem.HungerEatThreshold) continue;
```

**Verified against blame (`5049d445`): the shipped gate already ADMITS guards** — only
`Player`/`Enemy` are skipped at `:43` (`ActorRole.cs:11-15`: `Guard = 3` is neither). A
hungry idle guard gets EatIntent today. The B09 remainder is therefore NOT "add guards to
the set"; it is the two things the set membership leaves broken/unproven:

1. **No pursuit carve-out — lunch currently starves justice.** Nothing in the gate consults
   `world.GuardPursuits`. A guard mid-chase whose `Hunger` crosses
   `HungerEatThreshold` (`NeedConsumptionSystem.cs:15`, `= 55`) is granted EatIntent, and
   once actioned, `ScheduleSystem.Advance` no longer touches him —
   `ScheduleSystem.cs:48-51`:

   ```csharp
   // W32 EAT: the action layer owns this actor's legs now — an active (or terminal,
   // not yet consumed) action means the schedule may not touch its cell this tick.
   if (actor.ActionState.CurrentAction != ActorActionType.None)
       continue;
   ```

   which means the pursuit resolution AND its pruning (`TryResolvePursuit`,
   `ScheduleSystem.cs:72-93` — expiry, dead-quarry, >40-cell loss all prune in there) stop
   running for that guard: the chase silently stalls while he walks to a meal. The
   quarry-side half of this rule already exists — `ActionAdvancer.cs:34-40` probes
   `IsPursuitQuarry` every step (*"being hunted outranks lunch"*). The hunter side is
   missing.

2. **Nothing pins guards-eat.** No story test covers a guard eating; the class doc
   (`ActionLifecycleSystem.cs:15`, *"Decides EatIntent for idle hungry civilians"*) and the
   `ScheduleSystem.ChooseTarget` comment (`:109-110`, *"guards eat off-shift — an honest
   simplification, logged in ROADMAP_V2"*) both still describe the pre-W32 world. One
   well-meaning "restore the doc'd behavior" edit re-starves every guard, and no test fires.

### Fix design

One new gate line after `:43`, plus a read-only helper:

```csharp
// guards-eat (B09 remainder): the watch eats only when no chase is live — pursuit
// outranks lunch, mirroring the quarry-side probe (ActionAdvancer.cs:36). READ-ONLY:
// living.decision is not a declared World.GuardPursuits writer (FieldOwnershipRegistry
// keeps witness=arms, schedule=resolves/prunes); pruning stays in ScheduleSystem.
if (actor.Role == ActorRole.Guard && HasLivePursuit(world, actor, stamp)) continue;
```

```csharp
private static bool HasLivePursuit(WorldState world, ActorRecord actor, GameTime stamp)
{
    var pursuits = world.GuardPursuits;
    if (pursuits == null) return false;
    for (var i = 0; i < pursuits.Count; i++)
        if (pursuits[i].GuardId == actor.Id.Value && stamp.TotalMinutes <= pursuits[i].UntilMinutes)
            return true;
    return false;
}
```

Shape mirrors `ActionAdvancer.IsPursuitQuarry` (`:91-100`) — same expiry predicate
(`stamp.TotalMinutes <= UntilMinutes`), keyed on `GuardId` instead of `TargetId`, and
deliberately does NOT replicate the dead-quarry/40-cell checks: those require pruning to
stay cheap, and pruning is `living.schedule`'s job under the single-writer ledger
(`FieldOwnershipRegistry.cs:49-53`). Worst case a guard with a stale-but-unexpired row
defers lunch until `UntilMinutes` passes — bounded, deterministic, and the schedule (which
now keeps owning his legs) prunes the row the same tick it routes him.

Doc-comment updates in the same commit (comments state constraints, so stale ones are
bugs): `ActionLifecycleSystem.cs:15` → "…for idle hungry civilians and off-pursuit
guards"; `ScheduleSystem.cs:109-110` → the watch eats via the action layer, pursuit
permitting.

Explicitly NOT changed: the role gate at `:43`. The task's "guards also get EatIntent" is
satisfied by pinning what the code already grants — and the new story tests make removing
guards from the set a red build instead of a silent regression.

### Determinism / save / chunking
- Gate reads only `WorldState` + `stamp`; stateless between calls (chunking law upheld —
  `PursuitRecord` rows are saved state, so chunked replay sees identical pursuits at
  identical stamps).
- No new fields, no new writers: `World.GuardPursuits` ledger row unchanged;
  `Actor.ActionState` single-writer story unchanged (the new line only SKIPS a start).
- All-zero `ActorActionState` still decodes as Idle — untouched.

### Tests (story tests, `Assets/Tests/EditMode/Actions/`, alongside `EatStoryChainTests.cs`)
- `GuardEatsOffWatch` — hungry guard (`Hunger >= 55`), no pursuit rows ⇒ Decide starts
  `MoveToFood`; run the chain to `ConsumeFood`; hunger drops (the B09 closure, render-layer
  equivalent of the existing civilian pin in `EatHungerAtCompletionTests`).
- `PursuitOutranksLunch` — hungry guard + live pursuit row (`GuardId` = guard,
  `UntilMinutes >= now`) ⇒ Decide leaves `ActionState.CurrentAction == None` AND
  `ScheduleSystem.Advance` still steps him toward the quarry the same tick.
- `LunchAfterTheChase` — same fixture with `stamp` past `UntilMinutes` ⇒ next Decide grants
  EatIntent (expiry predicate pinned to `<=`, exactly `IsPursuitQuarry`'s).

### Blast radius
`ActionLifecycleSystem` (+~12 lines), two stale comments, three tests. No registry, save,
or advancer changes.

---

## Queue order and independence

Independent by construction (disjoint files). Suggested landing order = triage order:
**B03** (test-only truth restored first — it lints the others' world), **B17**, **B18**,
**guards-eat**. Total estimated diff: ~60 lines of production code across all four, the
rest tests.
