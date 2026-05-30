# Faz 0 — reviewer-comments bundle

**Sprint phase:** Faz 0 (audit and realignment, active).
**Run:** `@EMSPR` 2026-05-09 19:25 Europe/Istanbul.
**PR target:** #78 (`realignment-faz-0` -> `main`).

## Increment goal

Address actionable review feedback on PR #78 from
`copilot-pull-request-reviewer` and `chatgpt-codex-connector` so the
Faz 0 doc set is internally consistent before Mami merges.

## Atom map (this bundle)

```
- [x] docs/ROADMAP.md :: Faz 0 deliverables :: clarify CRON_CODES.md @EMSPR is out-of-repo  [box=meta]
- [x] docs/reference/UPSTREAM_README.md :: top banner :: label whole file as unedited verbatim upstream mirror so typos and Windows paths are no longer ambiguous  [box=meta]
- [x] docs/reference/README.md :: License section :: replace "MIT-equivalent" with the upstream verbatim license statement plus a pointer to the upstream LICENSE  [box=meta]
```

The remaining two Copilot comments (README.md repo-layout +
absolute-path framing) are already addressed by commit `1914c34`
("faz 0: correct README repo-layout to match real Assets/Scripts
asmdefs") and the explicit captain-paths callout at lines 65-69 of
`README.md`; verified the on-disk structure matches the README
description (`Assets/Scripts/{Domain,Simulation,Data,Presentation}` +
`Assets/Tests/{EditMode,PlayMode}`).

## Files changed

- `docs/ROADMAP.md`
- `docs/reference/UPSTREAM_README.md`
- `docs/reference/README.md`
- `docs/sprint-faz-0-reviewer-comments-bundle.md` (this file)

## Validation

`./tools/validation/run-validation.sh --mode fallback` →
`Passed!  - Failed: 0, Passed: 617, Skipped: 0, Total: 617`.

## Thalamus

- `packet_id`: `pkt_20260509162847_a27f2e58d90f`
- `resolver_key`: `sha256:9a0e22e76e08cec0cd54420cdfa5e894d2e94656e97d973a7f0de136facf83ff`
- `category_filter`: `["atoms.code", "atoms.plan", "atoms.memory"]`
- route confidence: 0.35 (low; doc-only, decomposed inline rather
  than via atom-of-thoughts MCP).

## Agent rules v2 alignment

- Product-visible? **No** — this is a doc-fix bundle on top of an
  open doc-only PR. It counts as PR-administration on Faz 0, not as
  a sprint test-only PR.
- Speculative utility? None added.
- New `SpellEffectCode` entries? None.
- New `SliceWorldState` named fields? None.
- Playable-proof PR cadence? Faz 0 is meta; the playable-proof rule
  starts firing in Faz 1 (Core Store reset).

## Next increment

After PR #78 merges, the active sprint becomes Faz 1 (Core Store
reset). The next `@EMSPR` run will:

1. Decompose Faz 1 against `docs/mechanic-map-v1.md`
   (`box=WORLD` + `box=LIVING` + `box=MATTER`).
2. Write `docs/faz-1-atom-map.md`.
3. Open the first atom: introduce `ActorStore` skeleton (no
   consumer migration yet — that follows in subsequent atoms).
