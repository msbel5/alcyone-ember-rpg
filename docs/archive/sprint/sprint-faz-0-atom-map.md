# Faz 0 — Atom map

_Date:_ 2026-05-09
_Branch:_ `realignment-faz-0` (PR [#78](https://github.com/msbel5/alcyone-ember-rpg/pull/78))
_Phase box:_ `[box=meta]` — audit and realignment, no gameplay surface.
_Acceptance gate:_ a fresh contributor opens the repo, reads
`README.md`, `docs/ROADMAP.md`, `docs/mechanic-map-v1.md`, and within
five minutes can answer the four orientation questions in
`README.md`.

This atom map is the canonical decomposition of Faz 0 against
`docs/mechanic-map-v1.md` and `docs/agent-rules-v2.md`. Per
`CRON_CODES.md @EMSPR` (PRD-V atomic decomposition + sprint promotion
hard rule), Faz 0 is not a candidate for promotion until every row
below is checked off, every sub-area has at least one merged PR, and
`tools/validation/run-validation.sh` passes.

Format: `- [ ] file/path :: scope :: brief responsibility [box=...]`.

## Canonical doc set (one PR, #78)

- [x] `docs/alcyone-audit-2026-05-09.md` :: audit body :: explain why the Sprint 5 magic-test loop stalled and what changes now [box=meta]
- [x] `docs/mechanic-map-v1.md` :: 8-box living-world model :: TIME, WORLD, LIVING, MATTER, PROCESS, SOCIETY, CRPG, AI/DM frame for all future decomposition [box=meta]
- [x] `docs/agent-rules-v2.md` :: five rules :: product-visible increment, no speculative utility, data-driven effects, world-store promotion, playable proof [box=meta]
- [x] `README.md` :: repo orientation :: locked architecture summary, sprint workflow, real on-disk layout (`Assets/Scripts/{Domain,Simulation,Data,Presentation}` + `Assets/Tests/{EditMode,PlayMode}`) [box=meta]
- [x] `docs/ROADMAP.md` :: 12-phase roadmap :: Faz 0..12 with `player can ...` acceptance per phase, supersedes Sprint 0 roadmap [box=meta]

## Upstream reference mirror

The mirror lives under `docs/reference/` and exists so future
decomposition resolves PRD/architecture references in-repo without a
network hop.

- [x] `docs/reference/README.md` :: index :: explain the mirror, link upstream LICENSE, list PRD/architecture/handoff sub-trees [box=meta]
- [x] `docs/reference/UPSTREAM_README.md` :: top banner :: label whole file as unedited verbatim upstream mirror so typos and Windows-style links are unambiguous [box=meta]
- [x] `docs/reference/prd/` :: 97 PRD mirrors :: snapshot of `msbel5/ember-rpg/docs/prd/active` plus PRD_STANDARD template [box=meta]
- [x] `docs/reference/architecture/` :: 7 architecture notes :: `runtime_authority`, `reference_notes`, `KERNEL_DIRECT_UI_CUTOVER`, `ember_mechanics_canon_v1`, `automation_stack`, `creation_state_machine`, `MECHANISM_DIAGRAM` [box=meta]
- [x] `docs/reference/handoffs/` :: 2 handoff prompts :: `COPILOT_WAVE_2_PROMPT`, `COPILOT_WAVE_3_CONSUMER_WIRING` [box=meta]

## Bot-review pins (small follow-up commits on the same branch)

- [x] `README.md` :: repo-layout :: replace nonexistent `Assets/Ember/` claim with real `Assets/Scripts/...` tree (commit `1914c34`) [box=meta]
- [x] `README.md` :: captain paths callout :: explicitly mark `/home/msbel/...` and `~/.openclaw/...` as captain-side configuration outside the repo (commit `1914c34`) [box=meta]
- [x] `docs/ROADMAP.md` :: Faz 0 deliverables :: clarify `CRON_CODES.md @EMSPR` lives out-of-repo and is not part of this PR's tracked artifacts (commit `38431ca`) [box=meta]
- [x] `docs/reference/UPSTREAM_README.md` :: top banner :: explicit "Unedited verbatim upstream mirror" framing so typos and Windows paths are no longer ambiguous (commit `38431ca`) [box=meta]
- [x] `docs/reference/README.md` :: license section :: replace "MIT-equivalent" summary with upstream verbatim license statement plus pointer (commit `38431ca`) [box=meta]

## Out-of-repo Faz 0 deliverables (tracked elsewhere)

- [x] `~/.openclaw/workspace/CRON_CODES.md` :: `@EMSPR` section :: codify `agent-rules-v2` (product-visible rule, no speculative utility, data-driven effects, world-store promotion, playable proof) so the cron loop reads them on every run [box=meta]
- [x] `~/.openclaw/workspace/CRON_CODES.md` :: `@EMSPR` section :: PRD-V atomic decomposition rule + sprint promotion hard rule + AI bot review queue contract [box=meta]

These two rows are tracked here for completeness; the file lives on
the captain workspace and is not part of this repo's commit history.

## This atom map

- [x] `docs/sprint-faz-0-atom-map.md` :: this file :: canonical Faz 0 decomposition required by sprint promotion hard rule [box=meta]

## Promotion checklist

- [x] every Faz 0 atom above is checked off
- [ ] PR #78 is merged into `main` _(CI fully green as of 2026-05-09 17:15 UTC: EditMode + PlayMode + Test Summary + GitGuardian all SUCCESS; only blocker is Mami's manual merge per agent rules; auto-merge intentionally not used)_
- [x] `tools/validation/run-validation.sh --mode fallback` passes on the active branch (verified on commit `38431ca`: 617/617 EditMode tests pass)
- [x] sprint summary file recording final atom count + bundle count: this file, atom count = 16, bundle count = 4 (canonical doc set, upstream mirror, bot-review pins, out-of-repo)
- [x] product-visible PR count for Faz 0 = 1 (PR #78 itself; the realignment is the visible deliverable for the meta box — fresh contributor orientation goes from "no map" to "five-minute orientation")

## Next sprint

Faz 1 — Core Store reset (boxes WORLD, LIVING, MATTER). Decompose
against `docs/mechanic-map-v1.md` once PR #78 is merged. Expected
atoms: `ActorStore`, `ItemStore`, `SiteStore`, `FactionStore`,
`WorldEvent` + `ReasonTrace`, deprecated-view shims over the
existing `SliceWorldState.Player/Talker/Merchant/Guard/Enemy`
fields, deterministic save/load round-trip test.

## Thalamus packet

- packet_id: `pkt_20260509164702_83918e201770`
- resolver_key: `sha256:6952e7054be8f0291ef1ce5359ad1e065e94bb4d730051c485d53e6bef9725bb`
