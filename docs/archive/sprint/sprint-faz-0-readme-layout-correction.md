# Sprint Faz 0 — README repo-layout correction

_Date:_ 2026-05-09
_Branch:_ `realignment-faz-0`
_PR:_ updates [#78](https://github.com/msbel5/alcyone-ember-rpg/pull/78)
_Atom:_ fix `README.md` to match actual on-disk layout
_Box:_ `[box=meta]` (Faz 0 acceptance gate — five-minute orientation)

## Increment goal

Both Copilot and ChatGPT-Codex AI reviewers flagged the same defect on
PR #78: the README `Repo layout` block describes
`Assets/Ember/{Domain, Simulation, Persistence, Tests, Unity}` and an
`Ember.Core` library that do not exist in the repo. The actual layout
is `Assets/{Scripts/{Domain, Simulation, Presentation, Data}, Tests/{EditMode, PlayMode}, Scenes, Art}`
with `EmberCrpg.*` asmdefs.

This breaks Faz 0's acceptance test ("a fresh contributor opens the
repo, reads `README.md`, and within five minutes can answer the four
orientation questions"). A reader who tries to find `Assets/Ember/`
fails at minute one.

## Files changed

- `README.md`
  - `Engine + architecture (locked)` — replace `Ember.Core` /
    `Assets/Ember/` claims with the real `EmberCrpg.{Domain, Simulation, Data}`
    assemblies, and explicitly label `Ember.Core` + Persistence as
    Faz 1 plans.
  - `How sprints work` — add a short note that the
    `/home/msbel/...` and `~/.openclaw/...` paths are Captain-side
    configuration on Mami's Pi, not tracked files (addresses Copilot's
    line-70 concern).
  - `Repo layout` — replace the `Assets/Ember/` tree with the real
    on-disk tree (Scripts + Tests + Scenes + Art + tools/validation),
    and add a "Planned (Faz 1)" paragraph explaining the future
    `Ember.Core` umbrella so the architecture intent is preserved
    without lying about the present.

## Bot reviews addressed

- `Copilot` PR #78 inline comment on `README.md` line 89 — addressed:
  layout block now matches `Assets/Scripts/...` + `Assets/Tests/...`
  with `EmberCrpg.*` asmdefs. The future `Ember.Core` move is labelled
  as planned.
- `Copilot` PR #78 inline comment on `README.md` line 70 — addressed:
  Captain-side absolute paths now carry an explicit "not tracked
  files" disclaimer.
- `Copilot` PR #78 inline comment on `README.md` line 48 — addressed:
  the architecture summary no longer references `Assets/Ember/` and
  uses the actual assembly names.
- `chatgpt-codex-connector` inline comment on `README.md` line 48 —
  same fix; both bots flagged the identical defect.

The remaining Copilot comments on `docs/ROADMAP.md` line 33 and
`docs/reference/UPSTREAM_README.md` / `docs/reference/README.md` are
out of scope for this single-atom fix and tracked for a separate
small atom on the next run (see "Next increment" below).

## Validation

`tools/validation/run-validation.sh --mode fallback` — see commit
output. Documentation-only change; `EditMode Tests` should remain
SUCCESS and `GitGuardian Security Checks` SUCCESS. `PlayMode Tests +
Screenshots` is unchanged by a README edit.

## Thalamus packet

- `packet_id`: `pkt_20260509161634_7a7e9ab3f7a9`
- `resolver_key`: `sha256:e6f6853b2ab20ceb04923c9ac2f046876a117d033dae3e4ee8dba553b1bd2ca2`

## Faz 0 acceptance impact

After this fix, the four orientation questions in
`## Five-minute orientation` are answerable without contradicting the
real repo:

- _What kind of game is this?_ — deterministic living-world CRPG.
- _What is in scope right now?_ — Faz 0 reset, then Faz 1 Core Store.
- _What is reference / not yet built?_ — `docs/reference/` is the older
  Godot/Python project; `Ember.Core` umbrella is Faz 1 plan.
- _What is the next sprint's playable acceptance?_ — top of
  `docs/ROADMAP.md` (Faz 1: spawn a guard, talk, walk to second site,
  remembered across save/load).

This is the third commit on the realignment branch. Per
`agent-rules-v2` rule #1, this is a doc fix that directly unlocks the
Faz 0 acceptance gate (rather than another test-only PR), so it
counts toward visible progress.

## Next increment

Address the remaining Copilot comments on PR #78 in a separate small
atom:

- `docs/ROADMAP.md` line 33 — clarify that the `CRON_CODES.md` update
  is out-of-repo (lives in `~/.openclaw/workspace/CRON_CODES.md`), not
  a Faz 0 deliverable inside this repo.
- `docs/reference/README.md` license block — replace the
  "MIT-equivalent" summary with a link to the upstream LICENSE plus a
  note about asset licensing.
- `docs/reference/UPSTREAM_README.md` — add an explicit "unedited
  upstream excerpt" disclaimer, or clean the typos.

Once PR #78 is green and merged, Faz 1 (Core Store reset) atom-map
authoring is the next single increment.
