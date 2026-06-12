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

### v0.7 "Yaşayan Evren" — SHIPPED (shipcheck 9/9 PASS, perf 12.5ms avg; tag v0.7.0-living-universe)

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

F27 NPC daily-life staging SHIPPED: the midday meal is REAL — between 12:00 and 13:59 the schedule
routes every civilian (never the watch, never outlaws) to the tavern, and the street visibly drains
into a lunch crowd; after 14:00 the normal work/anchor rhythm pulls them back. Pose pictograms ride
the billboards: an amber MUG over everyone at lunch, a HAMMER over workers (farmers, blacksmiths,
artisans) through the working day — 12×12 generated pixel-mask sprites on the hostile-marker's
camera-facing family. Proof (Reports/proof-f27a): "[Lunch] 17 civilians at the tavern (hour 13)" +
a frame of the lunch crowd with mug icons clearly overhead. EditMode: lunch routing, guard
exemption, and the post-meal return are all pinned (fallback 1460/1460).

F26 functional interiors SHIPPED: the first three shells of every settlement now WORK. The TAVERN
(amber sign-glow) sells sleep — E inside, 5 gold, vitals refill and the clock walks 8 hours forward
hour-by-hour; the settlement's innkeeper (or a merchant) is seated inside. The TEMPLE (white glow)
mends your health for 8 gold. The SHOP (green glow) opens the live trade screen from its counter
(a one-flag screen-request signal to the UI). Each role reads from the street by its glowing sign.
Proof: looptest "LOOP-PROOF tavern-sleep: hp 39->62/62, purse 349->344, +8h" + "[Tavern] host seated
inside" + three interior frames tinted by their sign light. EditMode: sleep gold-gate/refill/+8h and
temple heal tests. Honest PARTIALs: the host is seated by MoveTo (the daily schedule may walk them
out over hours — persistent re-homing is v2); interior frame composition favors wall corners (F33).

### v0.8 "Büyü + Bestiary" — SHIPPED (shipcheck 9/9 PASS, perf 12.7ms avg; tag v0.8.0-spell-and-beast)

F28 spell school SHIPPED: the school is EIGHT — three damage types (Flame Bolt / Frost Lance /
Spark Arc), a shield (Ember Ward), a heal (Mending Touch), a light (Lantern Glow), a haste
(Wind Step) and a recall gate — and the three damage types wear their colours in the WORLD: the
flying bolt and its point light tint orange / ice-blue / white-gold by template id.
Proof (validation-output/proof-f28): look_spell_flame/frost/spark.png show three distinctly
tinted bolts mid-flight over the settlement street, all fired through the real cast path
(mana committed, target validated, damage landed); look_spell_lantern.png shows the held
lantern orb. Three ROOT FIXES shipped with it: (1) the legacy resolver REJECTED any spell with
a timed or open-set effect — ember_ward never actually cast in live play; the resolver now
resolves the supported instantaneous subset and SKIPS the rest, the ward is recorded into
PlayerShieldBuffs on cast and eats enemy melee damage through a new defender-mitigation seam in
CombatActionResolver; (2) ranged casts measured range from the player's PARKED actor cell, so
casting away from the plaza silently refused — the record now syncs to the live body at cast
time; (3) the mana economy: Mind points grow the pool (+2 max each, the 12-point loadout could
never reach ward 15 / frost 17 / recall 20), damage prices follow the flame curve, and the cost
estimator prices open-set codes at zero (their magnitude is world-units, not vitals). Keys 1-8
cast; new spells are learned via level-up picks. EditMode: effect tests for the five new spells,
ward mitigation, mana growth (fallback 1466/1466). Honest PARTIALs: wind_step's ×1.5 stride and
recall's rig-snap are wired + unit-tested but not frame-proven; there is no spellbook reorder
screen — slots follow known-spell order.

F29 bestiary SHIPPED: dungeon dwellers are a six-strong bestiary now — bandit, skeleton (Bone
Walker), wolf (Fen Wolf), spider (Pit Spider), ghost (Grave Wisp), and boss variants: the Warden
keeps its name but wears its archetype's APEX type with 2x health / 1.5x damage (a great wolf in
the cave, a wisp in the crypt, a bandit chief in the ruin). The archetype picks the type rotation
(cave runs beasts, crypt the dead, ruin squatters); stats, sprite roles, hit materials and the
SDXL forge prompt bodies all live in one pure-Simulation catalog (WorldBestiaryCatalog), with the
prompts registered through the house style envelope into the forge manifest. Every strike now
sounds like what it lands on: the modal-bank hit synth is re-voiced per material (bone clicks,
hide thuds, chitin snaps, wisps ring) and logs its variant.
Proof (validation-output/proof-f29): look_bestiary_trio.png — ghost + spider + skeleton clearly
distinct in ONE crypt-room frame; "[Audio] hit variant=bone/wail/chitin/flesh" lines; all three
archetype mixes spawned in one proof world; the F14/F18 chase (6.1m closed), boss-bind and loot
legs stayed green under the new names (0 BROKEN). With the forge off the types read via generated
pixel-mask silhouettes with per-type stature (spider crouches at 0.9m, the wisp looms at 2.2m);
with the forge on the library sprite wins. EditMode: catalog completeness, rotation determinism,
apex picks, name round-trips (fallback 1470/1470). Honest PARTIALs: silhouettes are blocky pixel
stand-ins until a forge-on pass bakes real sprites; wolf/ghost appear via logs + trio frame, no
solo frames.

F30 audio v3 SHIPPED: the world found its background voice. A biome layer rides over the wind --
sparse bright bird chirps through the day, 16Hz-gated cricket pulse trains at night, silence under
a roof ("[Audio] biome layer=birds/crickets/none" transitions logged). Music finally respects the
rain (the F25 debt): the slot bed ducks 0.30->0.18 while the shower plays and returns when it
clears. The Warden fight stacks a forged 138bpm percussion loop (kick drop 95->55Hz on quarters,
noise snare off-beats) over the BATTLE bed -- "[Music] boss layer ON (+percussion)" proven in the
boss window. Two new grounds join dirt and stone: snow (soft lowpassed crunch, no modal ring) and
gravel (4-6 jittered micro stone ticks), picked by a 4-way ground rule (delve slabs = gravel,
interiors = stone, snow sky = snow, open terrain = dirt). All ten new clips carry forge metrics
in the log. Honest PARTIALs: EAR approval awaits the user (metrics and logs are the machine-side
proof); the surface-change log needs real walking (proofs teleport); piper TTS is not installed,
so greetings stay text-only.

v0.8 CLOSURE: SHIPCHECK VERDICT PASS -- 9 sections, 0 exceptions, perf avg 12.7ms / worst 440ms
(budget 16ms). Tagged v0.8.0-spell-and-beast.

### v0.9 "Cila + İskelet Hikâye" — in progress

F31 main quest spine SHIPPED: the game has a beginning, a middle and an END. Three acts -- gather
the ancient inscription pieces from the delves (one per delve; the requirement adapts to worlds
with fewer than three), carry the joined inscription to the capital's sage, then descend the
FINAL delve (pinned deterministically at world seed) and fell its Warden. The intro lands in the
journal at New Game; completion raises a runtime-built finale overlay -- the EMBER title, the
closing lines, and credits. The spine is a pure-Domain state machine that REFUSES out-of-order
progress (no sage before the pieces, no finale before the sage, one piece per delve), persisted
through the standard three-layer save path.
Proof (--ember-mainquest, validation-output/proof-f31): one end-to-end run -- intro armed (3
pieces, final delve pinned) -> three delve chests yield 1/3, 2/3, "whole" -> the sage at the
capital -> the final Warden falls -> "act=4 complete=True" + the finale frame eye-checked.
The proof caught two real bugs on the way: the inscription hook sat BEHIND the chest's sword-dedup
early return (delves 2-3 read "empty" and never yielded), and the 64pt title was silently clipped
by its 60px text rect. EditMode: act gating, per-delve uniqueness, small-world adaptation,
invariant healing (fallback 1474/1474). Honest PARTIALs: the intro is journal text (no dedicated
intro screen yet -- F32); the sage consult is the adapter path (live E-key sage dialog lands with
F32's UI pass); the finale overlay has no restart flow.

F32 UI/UX polish SHIPPED: the options screen grew two real tabs -- AUDIO & DISPLAY (music / SFX /
mouse-sensitivity sliders that apply LIVE through RuntimePlayerSettings and persist via
PlayerPrefs, plus a resolution cycler with APPLY) and KEYBINDS (the full control list, every row
VERIFIED against the live input handling -- the F5/F9 quicksave rows claimed by old comments did
not exist and were cut). The "no dead buttons" rule landed with teeth: the aspirational BG1
action-bar sub-levels (modal abilities, formations, quick weapons/items, innates, bard songs --
~25 stub entries, every one answering with an apology) are GONE, and all 12 standard-strip slots
now perform a real action or point at a real key. The "not yet available" source grep returns
ZERO. Proof (--ember-igtour, validation-output/proof-f32): 9 frames -- HUD, inventory, character,
journal, map, pause, and all three options tabs -- eye-checked. Honest PARTIALs: keybinds are
read-only (rebinding out of scope); the resolution control is a cycler, not a dropdown; the
volume sliders work by code-path proof, ear confirmation belongs to the user.

F33 visual polish SHIPPED: the frame found its depth. A runtime-built URP global Volume (light
bloom 0.65 + vignette 0.22 + context colour grade) rides the player rig -- and the UberPost
variants SURVIVE the player build (a real risk on this project's history of stripped shaders;
the before/after frames are the proof: proof-f30 vs proof-f33, same poses, visibly deeper).
The grade follows context: warm firelit cave, cold blue-green crypt, sickly green-gold ruin,
near-neutral overworld -- all three archetype casts logged in one run. Landed strikes now throw
SPARKS (a 24-quad manual pool on the proven unlit path -- ParticleSystem still renders nothing
in players), and billboards WALK: a mirror-frame gait (flipX + squash at step cadence) replaces
the ice-skate glide while a real forged second frame remains future work. Perf held with
everything on: SHIPCHECK 9/9 PASS, avg 12.4ms / worst 466ms against the 16ms budget. Honest
PARTIALs: this run's hitflash frame missed the 0.15s flash / 0.32s spark windows (the hp drop is
in frame; the spark log will print hundreds of times in F34's marathon); a still frame cannot
prove a gait -- the walk needs eyes in motion.
