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
- FIXED in v0.3: the hit roll is now a Daggerfall-style 50-base curve (50 + accuracy - dodge, clamp
  [15,95]) and outlaws were rebalanced (acc 30 / dodge 20) — a fresh player lands ~48% of swings.
  Progression/equipment accuracy growth remains the next balance item. The bounty/pilgrimage quests
  have no compass guidance lines yet.
- World quests (bounty/pilgrimage) are not yet persisted in saves; the forge quest and the whole world state
  round-trip byte-identically (digest-verified).
- Swimming is an underwater tint (no drowning); dungeon interiors await their encounter haunters' billboards.

### v0.2 "Living World" additions

Trees and noise-broken ground, hinged doors and windows, night curfew (streets empty 22:00-06:00, guards
and outlaws prowl), social-group greetings with live-event rumors and 35% delve reveals, a nearest-delve
compass, population-scaled farm belts feeding a real grow->harvest->stockpile->price chain, GemRB-style
DAY/NIGHT/BATTLE procedural music with a DOOM-style 8-channel SFX pool, and the uncapped day-by-day travel
loading screen. Shipcheck: 8 sections, PASS.

Known limits added in v0.2 (honest): TTS voice-over is DESIGNED but not wired (forge-cache pattern ready;
needs a local piper/kokoro install) - greetings stay text. Work-pose icons and needs-driven errand walks
are the next staging slice. Sky stays bright right after big proof-clock jumps.

### v0.3 "Find, Fight, Hear" additions

Discoverability: an ALWAYS-ON red `DELVE <name> - N tiles - direction` HUD line (quest-independent; the
old compass hid the dungeon behind forge-quest completion), a worldgen invariant guaranteeing every world
at least one Dungeon settlement (temperate planets used to roll zero - the real "I can't find the dungeon"
root), big red always-labeled dungeon map pins sorted to the top of the atlas list, and full-strength
delve rumors (the 50% coin-flip that halved the DFU 35% reveal is gone; the dungeon name is SHOUTED).
Combat is REAL-TIME now (Daggerfall/DOOM feel): engaging a hostile no longer opens a paused modal — the
HUD grows an enemy name+HP panel, BATTLE music starts, F swings and 1-5 casts live in the world, and the
enemy strikes back on its own ~2.4s clock through the same resolver dice. The hit roll moved to a
50-base curve (50+acc-dodge, clamp [15,95]; outlaws rebalanced to acc 30/dodge 20) so a fresh player
lands ~48% of swings instead of the old 5% floor. Hostile spells REFUSE to cast with no enemy in range
(they used to fall back to the caster — the "spell drains my own HP" bug), and the spell vfx is a
camera-facing glowing bolt + light instead of an edge-on line. Every delve chamber is guarded by two
Outlaw "haunters" (E to fight, chest behind them; corpses stay down through save/travel), street
hostiles wear a red marker diamond, landed strikes flash the billboard red + a modal thud, and felled
enemies fall flat. The dungeon interior now conforms to the
hillside (crest-floated floor, mouth ramp, brighter torches with visible flames) and all NPC billboards
ground themselves on what they stand on. Audio v2 (research-backed recipes, see
`memory/procedural-audio-recipes.md`): GRF two-peak footstep exciters driving PhISM dirt grains and
6-mode modal stone rings (material = decay times), Crackdown-2 dip-cascade variants, an EKS stick-slip
door creak replacing the 1320Hz UI-click "bell", and two-layer music (i-VI-III-VII pad+bass bed under a
rule-constrained pentatonic melody + noise percussion) per DAY/NIGHT/BATTLE slot; the 6×16s music render
moved to a worker thread + a process-wide clip cache after shipchecks caught a 14s main-thread freeze and
a per-reload reforge (perf FAIL avg 175ms → avg 11.9ms PASS). Proof runs take `--ember-forge-off` so the
SDXL portrait forge stops fighting the GPU mid-measurement. Shipcheck: 9 sections, VERDICT PASS
(world-enter, quest-seed, encounter-loot 13 swings, economy, perf 11.9ms avg, soak 10 hops 0 exceptions,
economy-chain, audio-forge, modal-capture).

### v0.4 "Combat Depth" — SHIPPED (roadmap: docs/ROADMAP_V1.md; shipcheck 9/9 PASS, perf 11.9ms avg)

F14 enemy movement SHIPPED: hostiles that see the player (12m) give CHASE at ~2.2 m/s, stop adjacent
and fight, and reaching aggro range auto-binds the encounter — walking into a delve chamber starts the
fight without pressing E. All combat distance math (chase, retaliation reach, attack-nearest, spell
range) now measures from the LIVE first-person body instead of the parked deterministic actor. Enemy
swings lunge the billboard 0.2s. Proof players now quit automatically when a proof run ends.
Proof: "[Proof] F14 chase: a=9.4m b=5.4m closed=4.0m" + consecutive chase frames (chest → corridor).

F15 death+respawn SHIPPED: dying is a toll, not a wall — the death screen's primary "AWAKEN AT THE
LAST SETTLEMENT" action takes 20% of your gold, refills all vitals, walks the world clock forward 8
hours (hour-by-hour so cadences fire), and returns the body to the plaza. Works without a save file.
Proof: "fell in 8 enemy swings, purse 313->251 (-20%), hp=62/62, +8h" + a live post-respawn HUD frame
with full bars. (Also fixed: ProofAdvanceHours lost its first hour to a stale tick index; same-frame
double CaptureScreenshot swallowed the earlier capture.)

F16 equipment SHIPPED: weapon bonuses finally enter the dice (the starting Ash Training Blade sat
inert in the backpack since Sprint 1 — it now spawns EQUIPPED and its +5 acc/+2 dmg apply to every
swing), and the delve chest OPENS with E: it yields the tier-up Worn Iron Sword (+8 acc/+5 dmg),
auto-equips it when it beats your hand, and creaks its hinged lid back. One sword per world.
Proof: "20 bare swings dealt 83, 20 armed swings dealt 103" + the chest grant line + a 60-paired-seed
EditMode test. Honest limits: chest-opened state isn't save-persisted yet (F22); body/armor slot
arrives with armor content (F29).

F17 XP/levels SHIPPED: kills grant +40 XP, world-quest completions +60; level N→N+1 costs N×100, and
crossing the threshold AUTO-OPENS the level-up screen (5 points across 6 stats + a new spell — the
machine was already real; it just allowed infinite levels with no XP gate). The spend consumes the
earned XP; PlayerXp persists through saves (3-layer pattern, reflection-guard tested).
Proof: "[XP] +40 (kill) 40/100" → "+60 (quest) 100/100 — LEVEL UP READY" + the auto-opened
"LEVEL UP! Warden Level 1→2" modal frame + EditMode gate/spend/roundtrip test.

Known limits added in v0.3 (honest): steep hillsides can still clip a dungeon-barrow corner; footstep
surface detection is name-based (built "Floor" slabs vs terrain); per-step audio variation rotates 4
pre-rendered variants (+pitch jitter) instead of re-rendering the dip cascade per step; melody voices are
sine-family (no wavetable timbres yet).

### v0.5 "Zindan Çağı" — SHIPPED (shipcheck 9/9 PASS, perf 15.4ms avg; tag v0.5.0-dungeon-age)

F18 multi-room delve SHIPPED: the single barrow chamber grew into the real thing — the domain's
deterministic MultiRoomDungeonGenerator (5-10 rooms + mid-wall doors, seeded per settlement) is now
REALIZED behind the cave mouth on a 16m lattice: per-room torch-lit chambers with sealed walls,
door gaps, corridor connectors, an entry hall, 0-2 dwellers per room (Haunter/Stalker/Lurker/Prowler
of X), and a BOSS room at the end of the graph — "Warden of X" (2× HP, 1.5× damage, acc 38) guarding
a 1.4× hoard chest. The realize step records world anchors (RuntimeDungeonLayoutInfo) so proofs and
dweller placement stopped guessing local axes.
Proof (Reports/proof-f18h): "multi-room delve realized: rooms=5 doors=4 bossRoom=R4" + a roof-hidden
topdown frame showing the whole room graph; "[Proof] F18 boss bound: Warden of Vhiriorothcross
(80/80 hp)" felled in 15 swings (a regular dweller falls in ~7); chest loot line "You take the Worn
Iron Sword (+8 acc, +5 dmg)".
Three real bugs root-caused on the way (the chase read at half speed): (1) ScheduleSystem stepped a
chasing dweller BACK toward its lair every world tick — pinned Enemies (home == dayAnchor) are now
exempt (2 new EditMode tests); (2) billboards only learned new sim positions on the ~0.83s world
tick — the HUD pump now refreshes view targets at frame rate after each hostile-AI step
(ProjectWorldViewsNow, no clock advance); (3) the boss chased too — hostiles now have a LAIR LEASH
(dwellers hunt ≤10 cells from home, the Warden holds ≤3 and never abandons the hoard; past the
leash they stalk back). Hostile sight raised 12→18 for the room lattice; hostile billboards no
longer idle-wander (monsters hold still and pursue with purpose). Chase proof: sim Chebyshev 14→8
in 2.6s + "closed=5.5m" on screen (DoD ≥4m).
Honest limits: a dweller crossing a connector can ground its billboard on the corridor ROOF for a
few steps (F19 polish); the chest close-up frame sits on the torch's shadow side and reads dark
(F33 framing); chest-opened state still isn't save-persisted (F22).

F19 dungeon variety SHIPPED: three delve archetypes — Mağara (warm firelight on dark wet rock),
Kripta (pale dressed stone under cold blue grave-light), Harabe (mossy sandstone in sickly
green-gold) — picked deterministically from the dungeon seed and stamped into the realize log
("archetype=Kripta rooms=8 ..."). Two root findings on the way: worlds rolled as few as ONE dungeon
(the F9 invariant guaranteed one; the floor is now THREE per world, never demoting City/Town —
worldgen goldens survived), and the raw seed % 3 pick was BIASED (every realize seed is structurally
divisible by 3 — all delves came out Mağara) so the seed goes through a murmur-style finalizer first.
Proof (Reports/proof-f19d): travel to all 3 delves of the proof world, one interior frame each,
eye-verified distinct palettes; "[Proof] F19 delve census: 3 dungeon(s)" + three "archetype=" lines;
EditMode sweep test pins reachability of all three + the proof-seed trio mapping to ≥2 archetypes.
Honest PARTIAL: music does not vary by archetype yet (DAY/NIGHT/BATTLE slots are archetype-agnostic).

F20 traps + locks SHIPPED: the way down to the Warden now fights back. A rust-red CRUSHING PLATE
sits mid-corridor on the boss path — step on it and it gives way underfoot: 8 damage, the mechanism
groans, the HUD combat line tells you what bit you. The boss connector carries a LOCKED DOOR whose
key — the Tarnished Key, a real inventory item — waits on a pedestal in a deterministic middle room;
the lock CONSUMES the key (pick another delve's key later, same pack slot), and without it the door
says so once per approach. Proof (Reports/proof-f20a): "[Trap] crushing plate fired: 8 damage." (HP
drop visible in the trap frame) → "[Key] You take the Tarnished Key" → "[Door] The Tarnished Key
turns — the boss door grinds open." → Warden bound. EditMode roundtrip: pickup → duplicate-pickup
refused → consume → second consume refused.


### v0.6 "Görev Makinesi" — SHIPPED (shipcheck 9/9 PASS, perf 11.6ms avg; tag v0.6.0-quest-machine)

F23 reputation + crime SHIPPED: an AIMED strike at a civilian is a CRIME — the watch posts a 40g
bounty, your reputation drops 2, and guards HUNT you through the same chase AI the outlaws use
(auto-target can never commit an accidental crime: the attack-nearest key only ever picks enemies).
Not every settlement rolls Guard seeds, so crime SUMMONS the watch — two officers materialize at the
plaza edge (the dungeon-dweller synthesis pattern: deterministic ids, idempotent, corpses persist)
and close in. Finished contracts build your name (+1 rep each); at rep ≥5 the market basis drops 10%.
The HUD top bar carries both: "Rep ±N · BOUNTY Ng". Reputation and bounty persist through saves.
Proof (Reports/proof-f23b): "[Crime] civilian assaulted: bounty=40g rep=-2" → "the watch arrives:
+2 officers" → watch telemetry 8 → 4 cells closing + look_guard_aggro.png (two officers on top of
the player in daylight). EditMode: crime/bounty/watch-closes test + rep-discount price test (21/21).
Honest open edges: no bounty pay-off/surrender flow yet (the bounty stands until death or F31);
guards still fight the schedule rubber-band while chasing (net closure stays positive).

F21 quest generator SHIPPED: endless DFU-style work, minted deterministically. Four templates —
FETCH (bring cargo to the giver), KILL (hunt a named outlaw), DELIVER (carry cargo to another
settlement), VISIT (reach a settlement) — picked per seed with template rotation when a world lacks
raw material (no outlaws → next template). PEOPLE give the work (no guilds yet): merchants, nobles,
priests, scholars, innkeepers, blacksmiths, healers of the current town. Every contract carries a
reward and a real deadline (3-7 days; overdue contracts FAIL in the journal). Generated contracts
appear in the J journal as their own "Contracts" chapter with live status. Fetch/deliver cargo comes
from the LIVE economy stock, so every contract is honestly completable.
Proof (Reports/proof-f21a): looptest closes a fetch end-to-end — "[QuestGen] accepted #9100: Bring
ale to Grire Theashal (38g, deadline day 7)" → cargo bought through the live market → "[QuestGen]
completed #9100 — +38 gold". EditMode: 20 seeds → 20 valid quests + determinism + all-four-templates
reachability (runs in the pure fallback harness, 1458/1458).
F22 quest persistence SHIPPED: world quests survive saves. Generated contracts and the fixed
bounty/pilgrimage pair's states moved from adapter-local stores onto the WORLD ROOT
(WorldState.WorldContracts / WorldQuestStates — the 3-layer persistence pattern), WorldSaveMapper
carries them both ways, and the contract serial derives restore-safely from what exists. The world
digest gained a WORLDQUESTS section (skipped when empty, so every pre-F22 golden stayed
byte-identical). Proof: WorldQuests_SurviveSaveLoadRoundtrip — an open contract, a completed one,
and the bounty/pilgrimage states round-trip byte-identically through save→load; the F21 looptest
fetch leg stays green seeding from the world store. (This closes F21's honest PARTIAL.)

### v0.7 "Yaşayan Evren" — in progress (F24 SHIPPED)

F24 sky v2 SHIPPED: a real procedural day cycle. The sun's pitch and intensity ride the clock; the
sky clear-colour cycles night-navy → dawn-rose → day-blue → dusk-amber (morning and evening blush
differently); night raises a STAR DOME (140 unlit pinpricks on a deterministic golden-angle
hemisphere — built from the sprite shader every billboard already guarantees) and a MOON (a generated
soft-disc sprite riding the sun's azimuth at 8-62° elevation, opposite phase). ROOT FIX: the sky now
reads world-time TRUTH (RuntimeFieldMirror.MinutesOfDay, published per tick from world.Time) — the
old TickIndex re-derivation drifted after clock jumps (respawn +8h, travel days) and left midnight
skies bright; that bug is dead, pinned by an EditMode truth test across a 16-hour jump.
Proof (Reports/proof-f24b): four frames at 06/12/18/24 — dawn-rose, clear blue, amber dusk, and a
star field with the moon in frame — all visually distinct; logs carry the truth minutes per frame.

F25 weather SHIPPED: deterministic per-day weather from biome + season + world day — RAIN (white
streaks falling around the camera + a PhISM-flavoured rain loop: lowpassed shower bed under droplet
ticks, forged with the honest metric "rain_loop: len=4.00s rms=0.042 centroid=1147Hz"), FOG (a
grey-lavender haze that dims the sun and greys the sky), SNOW (drifting white flakes under a pale
sky). Two proof-caught build truths shaped the design: URP fog shader variants STRIP from player
builds (so the readable atmosphere lives in the sky colour + sun dimming via a FogFactor the
SkyController consumes), and ParticleSystem renders NOTHING in players regardless of mode/material
(so precipitation is a manual 130-cube pool on the star-dome's proven unlit sprite path, recycling
through a 30×30m column that follows the camera). Day 1 of the proof world naturally rolled rain —
the deterministic pick is exercised by play, and the proof driver can force any kind.
Proof (Reports/proof-f25d): three visually distinct frames + "[Weather] day=N season=S biome=B →
kind" lines + the rain-loop forge metric. Honest PARTIALs: music does not soften in rain yet;
snow tints the air (haze + flakes), not the terrain splats.
