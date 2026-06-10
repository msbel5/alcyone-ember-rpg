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

## How to Play (v0.1.0 vertical slice)

Run `Builds\Windows64\alcyone-ember-rpg.exe` → New Game → answer the creation questions → you spawn in a
procedurally realized settlement on a 5120 km living continent.

- **Explore**: WASD + mouse; every building has a furnished, hearth-lit interior; farm plots grow with the
  sim's days; a trade cart appears at the plaza while a caravan is in town; region banners mark borders.
- **Quests**: the forge errand (fetch), an outlaw bounty (kill), a shrine pilgrimage (visit) — all paid in
  gold on completion.
- **Fight**: press E on an outlaw → real combat (accuracy/dodge/armor/stamina); victory pays spoils + bounty.
- **Trade**: shop prices come from the sim's live per-settlement price ledger; loot gold funds purchases.
- **Travel**: M map → click a settlement → the journey ticks day-by-day behind a loading screen (no cap);
  needs, prices and caravans live through every day on the road.
- **Sound**: procedural footsteps, wind ambience, encounter sting, UI clicks (no asset files).

### Proof modes (the game tests itself)

`alcyone-ember-rpg.exe --ember-proof-screenshots <dir> <mode> --ember-proof-quit`
with mode = `--ember-lookaround` (360° + interior + farm captures), `--ember-looptest` (full game loop,
LOOP-PROOF transcript), `--ember-playthrough` (33-capture tour), `--ember-shipcheck` (7-section regression
pack + soak: 10 fast-travels, exception & memory watch — current verdict: PASS).

### Known limits (honest)

- ~88 settlements ship by default (every viable planet site founded); denser worlds need flavor-preserving
  site-pool growth — open design question.
- A fresh player vs an outlaw sits at the 5% hit floor: progression/equipment accuracy growth is the next
  balance item. The bounty/pilgrimage quests have no compass guidance lines yet.
- World quests (bounty/pilgrimage) are not yet persisted in saves; the forge quest and the whole world state
  round-trip byte-identically (digest-verified).
- Swimming is an underwater tint (no drowning); dungeon interiors await their encounter haunters' billboards.
