# Ember — Alcyone Living-World CRPG

Unity/C# implementation of Ember. The older Godot/Python project is
read-only reference only.

## Core identity

- Deterministic simulation-first CRPG
- 3D world with 2D billboard actors
- Local-first AI flavour layer (never world authority)
- No silent black-box generation during boot/new-game flows

## Canonical docs (read in this order)

1. `docs/CURRENT_STATE.md`
2. `docs/REMEDIATION_V2_COUNTER.md`
3. `docs/EMBER_VISION_BIBLE.md`
4. `docs/AI_STACK.md`
5. `docs/PRD_GOVERNANCE.md`

Compatibility note: `docs/EMBER_GOAL.md` now redirects to this same canonical
read order.

## Proof modes (important)

- `source-only`: static checks + pure C# validation on `lfs:false` checkout.
- `LFS-runtime`: model/art/plugin bytes resolved (`git lfs pull`) and runtime
  checks are allowed.
- `Unity PlayMode`: scene/runtime behavior.
- `manual screenshot`: human visual evidence.
- `historical`: archived evidence; not current status by itself.

Never treat source-only green as runtime AI/art/build proof.

## Validation quickstart

```bash
# Source-only structural gate
bash tools/validation/static-audit.sh

# Runtime plugins+models pointer gate
bash tools/validation/static-audit.sh --require-runtime

# Runtime visual pointer gate (art + generated-core)
bash tools/validation/static-audit.sh --require-runtime --require-runtime-visual

# Fallback C# test harness
bash tools/validation/run-validation.sh --mode fallback
```

## Repo note

`Reference/**` and `docs/reference/**` are reference/history material. Active
Unity implementation requirements live under `docs/prds/` and the docs listed
above.

## License

MIT.
