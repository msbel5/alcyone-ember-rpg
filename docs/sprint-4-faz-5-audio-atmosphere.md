# Sprint 4 Faz 5 — Audio and Atmosphere Hooks

_Date:_ 2026-04-30
_Branch:_ `agent/sprint-4-faz5-audio-atmosphere`
_Base:_ `1ad4855` — Sprint 4 Faz 4 equipment inventory UI

## Scope delivered

Faz 5 adds clean-room audio/atmosphere hooks without importing audio assets and without adding `UnityEngine` references to Domain or Simulation.

Delivered:
- deterministic `SliceAtmosphereSelector` that maps current generated dungeon state into cue ids
- `SliceAtmosphereCueSet` contract with ambience, music, SFX, and a debug reason string
- HUD/debug feedback for the currently selected ambience/music/SFX cue ids
- optional `SliceAudioCueDriver` Unity adapter that can play inspector-assigned clips through `AudioSource`s when clips exist, and stays silent/safe when no clips are assigned
- fallback tests for room template variation, visited/cleared room-state variation, combat/door cue priority, and HUD-visible cue feedback
- small Faz 4 cleanup coverage for the `AlreadyEquipped` error code and an equipment-aware inventory remove guard

## Cue mapping

The selector is presentation-facing and deterministic. It reads current `SliceWorldState` only; it does not mutate gameplay state.

Inputs used:
- generated room template id (`ember-hall`, `ash-cell`, `watch-node-*`)
- current room id and spawn-room ids
- room visited/cleared state
- active combat state and enemy vitality
- closed generated doors connected to the current room
- guard clearance and uncollected pickup state

Current cue ids are intentionally small and asset-agnostic:
- ambience: `ambience.ember-hall`, `ambience.ash-cell`, `ambience.watch-node` plus `.unvisited` / `.cleared` variants
- music: `music.dungeon.explore`, `music.dungeon.quiet-npc`, `music.tension.enemy-near`, `music.combat.low`, `music.dungeon.resolved`
- SFX: `sfx.closed-door-pressure`, `sfx.pickup-ember-hum`, `sfx.guard-watch`, `sfx.combat-pulse`, `sfx.none`

## Validation evidence

Best available local gate:

```text
tools/validation/run-validation.sh --mode fallback
Passed!  - Failed: 0, Passed: 102, Skipped: 0, Total: 102
PASS fallback_harness
```

Static checks for this phase:
- `git diff --check`
- scan `Assets/Scripts/Domain` and `Assets/Scripts/Simulation` for `UnityEngine` references
- presentation file-size sanity check

## Caveats / manual gaps

- Local validation remains the pure .NET fallback harness, not a real local Unity Editor/EditMode/PlayMode run.
- No audio assets were imported. The Unity driver is a hook/adapter only; actual clip assignment and listener mix need a Unity manual pass.
- `SliceAudioCueDriver` was not manually heard in Unity in this environment.
- The equipment-aware remove guard only protects call sites that pass `EquipmentState`; a full future drop/use path should explicitly use that guarded overload or add a dedicated drop service.
