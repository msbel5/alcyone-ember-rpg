# Ember Recovery Final Report — source and targeted runtime PASS, full lane PARTIAL

## Delivery boundary

- Recovery scope completed in order from PRD-00 through PRD-10.
- Base HEAD: `f301feb0e8538c39fe17f4ca80b34d64e8a4dacb`.
- The existing W32-W38 `ActorActionState` / lifecycle / advancer spine is the
  canonical behavior path. No parallel V2/V3 framework or third mutation path
  was introduced.
- `_codex_handoff/` remains local and ignored. The two ignored orphan metadata
  files were removed from Git tracking without deleting local assets.

## Per-PRD outcome

| PRD | status | changed behavior | old path removed | targeted proof | final proof / remaining debt |
|---|---|---|---|---|---|
| PRD-00 | PASS | README and Atlas authority now reference existing, bounded recovery documents | Dangling authority links and two tracked/ignored orphan `.meta` entries | README/Atlas/static gates PASS | Final strict static audit PASS; no PRD-00 debt |
| PRD-01 | PASS | Repo-relative writer inventory pins Position, Vitals, Needs, Stockpiles and the single ActionState writer seam | Unclassified mutations cannot silently enter the production inventory | `FieldOwnershipRegistryTests` PASS 6/6 | Included in final 120-test gate; line-oriented scanner remains post-recovery debt |
| PRD-02 | PASS | Bounded deterministic routing returns moved/arrived/unreachable; no-route actions terminate without fake progress and clean claims | Greedy/fake-progress action movement paths | Movement/action gate PASS 33/33 | Included in final gate; direct-distance reservation TTL remains post-recovery debt |
| PRD-03 | PASS | Mid-action target, reservation, carried matter, actor target and hunt relationships round-trip append-compatibly | Unmapped/stale hunt restore and terminal target leaks | Save/action gate PASS 46/46 | Final save gate PASS; existing schema-0 fixtures retained |
| PRD-04 | PASS | Companion follow is persistent; companion guard uses Hunt→StrikeQuarry and canonical cleanup | Companion direct `MoveTo`, direct combat, and duplicate tick systems | Companion gate PASS 57/57 | Final companion gate PASS; Unity PlayMode action-story pack PASS |
| PRD-05 | PASS | Witness report, guard pursuit and predation are durable actions with safe retarget/lost/dead cleanup | Direct witness/guard/predation movement and strike mutation paths | Witness/guard/predation gate PASS 82/82; legacy path scan 0 hits | Final guard/hunt gates PASS; 64-fact report dedup window remains bounded debt |
| PRD-06 | PASS | One minute/tick is fail-fast; cadence has one owner; civilian needs have Eat/Sleep closure and Player/Enemy explicit opt-out | Editable/normalizing time scale and scattered `% 60` action gates | Time/needs/cadence gate PASS 101/101 | Final needs/options gate PASS |
| PRD-07 | PASS | OnWatch remains Running without completion spam; event identity is monotone, bounded and sequence-cursor based | Arrival completion pulses, retained-index cursors and multiple trim owners | Watch/event gate PASS 73/73 | Final event/watch gate PASS; built-player night frame shows real Sleeping activity |
| PRD-08 | PASS | Vermin/caravan/history mutations name the real matter destination; all carry failure paths conserve tagged matter; events are semantic | First-tag theft, stockpile-index delivery, rowless carry erasure and false success-event kinds | Matter/event gate PASS 72/72; perf regression fix gate PASS 35/35 | Final conservation gate PASS; full fallback suite completion DEFERRED after 300-second budget |
| PRD-09 | PARTIAL | Projection reads only real CurrentAction/events; apply failures are counted/logged; Atlas is repo-relative and fail-closed | `DescribeScheduleWord`, silent apply exceptions, absolute/stale Atlas authority | Projection/movement PASS 40/40; Atlas self-test/check, LFS fsck and strict static audit PASS | Unity PlayMode action stories PASS 8/8 and visual tour complete; built-player `[Action]` JSONL deferred |
| PRD-10 | PASS/PARTIAL | Exact 1/7/30-day deterministic traces, bounded state/save, through-terminal restore, one movement writer and five coherent stories are pinned | Terminal food/crop loss, unreachable report retries, strike fake progress, digest-blind roots and synchronous proof travel | Soak PASS 5/5; release gate PASS 32/32; selected final gate PASS 120/120 | Unity EditMode 5/5, PlayMode 8/8, Windows build and shipcheck 9/9 PASS; full Forge/CUDA lane PARTIAL |

## Final validation ledger

- Source/static audit: **PASS**.
- Atlas self-test and current-tree authority check: **PASS**.
- Git LFS fsck and strict runtime-asset/visual source audit: **PASS**.
- Final selected pure-C# recovery gate: **PASS**, 120/120.
- Full pure-C# fallback suite: **DEFERRED** after one 300-second attempt.
- Targeted Unity EditMode recovery soak: **PASS**, 5/5.
- Targeted Unity PlayMode projection/story pack: **PASS**, 8/8.
- Windows64 build: **PASS**, `14,159,578,133`-byte reported total payload.
- Built-player shipcheck: **PASS**, 9/9 sections and zero logged exceptions.
- Built-player visual tour: **COMPLETE**, zero scanned runtime exceptions.
- Full EditMode/Forge CUDA smoke: **PARTIAL**, Unity terminates in
  `cudnn64_9.dll`; no full-suite PASS is claimed.
- Built-player action-transition JSONL: **DEFERRED**; its PlayMode story proof is
  green, but source or PlayMode evidence is not relabelled as player JSONL.

## Evidence authority

The durable implementation ledger is
`docs/recovery/IMPLEMENTATION_STATUS.md`. Detailed logs live under
`Reports/recovery/PRD-00` through `Reports/recovery/PRD-10`. Source/static and
pure-C# results prove only those lanes; Unity XML and built-player logs/screens
are the runtime authority.

## Remaining debt classification

Release-blocking code debt found during the recovery review was fixed and is
covered by the final selected gate. The remaining items are post-recovery proof
or hardening debt:

- fix or isolate the Forge CUDA/cuDNN provider path before calling full EditMode
  green;
- capture built-player authoritative action-transition JSONL;
- replace harness-driven self-play with a manual keyboard/mouse pass when a
  working Computer Use runtime is available;
- address live-population day-catch-up cost and visible world/dungeon polish;
- replace the line-oriented mutation scanner with a syntax-aware gate if needed;
- decide whether report dedup must outlive the bounded actor-memory window;
- calculate reservation TTL from routed detour length rather than direct distance;
- schedule a longer machine window if completion of the entire fallback suite is
  required independently of the selected recovery gate.

The recovery and runtime follow-up are committed and pushed directly to `main`
under explicit user authorization. No pull request or merge step was used.
