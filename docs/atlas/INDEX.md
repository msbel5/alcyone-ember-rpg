# SYSTEMS ATLAS — Explanatory/Historical Navigation

> This Atlas is a repository-relative navigation aid, not current
> implementation or closure authority. Method/callsite presence cannot close
> an item. Use [CURRENT_STATE](../recovery/CURRENT_STATE.md) and
> [IMPLEMENTATION_STATUS](../recovery/IMPLEMENTATION_STATUS.md) for current
> evidence status.
>
> Regenerate deterministically with
> `python tools/validation/atlas-authority.py --write`; validate with `--check`.

Usage: `rg 'Actor.Position' docs/atlas/` to find where fields live across systems.
Bug scorecard: [BUG_REPORT_SCORECARD.md](BUG_REPORT_SCORECARD.md)

- [Time & Cadence](systems/01-time-cadence.md) - Historical map of tick composition, cadence, time ownership, and field-writer ordering.
- [Needs & Consumption](systems/02-needs-consumption.md) - Historical map of needs progression, food reservations, consumption actions, and recovery policy.
- [Schedule & Movement](systems/03-schedule-movement.md) - Historical map of schedule intent, action-owned movement, navigation, and reachability.
- [Cascades & Crime](systems/04-cascades-crime.md) - Historical map of predation, witnesses, reports, pursuits, guards, and companions.
- [Economy](systems/05-economy.md) - Historical map of stockpiles, prices, caravans, trade, and shortage response.
- [Plants & Harvest](systems/06-plants-harvest.md) - Historical map of plant growth, planting, harvest, haul, and field projection.
- [History & Rumors](systems/07-history-rumors.md) - Historical map of world events, runtime history, rumors, chronicles, and NPC echoes.
- [Quests](systems/08-quests.md) - Historical map of catalog quests, generated contracts, and the main-quest spine.
- [Magic & Combat](systems/09-magic-combat.md) - Historical map of spell execution, melee resolution, effects, and combat boundaries.
- [Save/Load](systems/10-save-load.md) - Historical map of save DTOs, mapper directions, action persistence, and digest round-trips.
- [Worldgen & Overland](systems/11-worldgen-overland.md) - Historical map of planet generation, overland generation, caching, and cold-load rebuild.
- [World Realize](systems/12-world-realize.md) - Historical map of scene realization, buildings, interiors, dungeons, and runtime terrain.
- [Actor Views](systems/13-actor-views.md) - Historical map of actor spawning, simulation projection, animation, labels, and feedback.
- [Dialog State](systems/14-dialog-state.md) - Historical map of dialog state, topic flow, memory, deterministic text, and AI flavor.
- [LLM Runtime](systems/15-llm-runtime.md) - Historical map of local model routing, request serialization, sanitization, and fallback.
- [TTS & Speech](systems/16-tts-speech.md) - Historical map of speech synthesis, playback ownership, retry, cooldown, and shutdown.
- [Forge & Assets](systems/17-forge-assets.md) - Historical map of generated assets, inference routing, cache identity, and provenance.
- [UI & Input](systems/18-ui-input.md) - Historical map of UI ownership, modal input, options, keybind display, and player controls.
- [Adapter Contract](systems/19-adapter-contract.md) - Historical map of adapter roles, read models, commands, projection, and async apply boundaries.
- [Proof Harness](systems/20-proof-harness.md) - Historical map of proof modes, runtime capture, census observation, and source-only fallback limits.
