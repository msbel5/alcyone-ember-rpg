# Branch audit — 2026-05-23

Audit of 35 restored remote branches against `main @ 3e36bde`. Goal: identify any
code/changes that exist on a branch but NOT on main, that should be ported forward.

## Method

For each remote branch:
1. `git log main..origin/<b>` — list unique commits
2. `git diff main..origin/<b> --stat` — file-level diff size
3. `git cherry main origin/<b>` — patch-id based already-applied check
4. Spot-check unique source files: do they exist on main? Different content? Superseded?

Constraints honored:
- 800 sprite PNGs purged 2026-05-23 (`docs/art-audit-2026-05-23.md`) NOT restored.
- Sentis `SentisAssetForge.cs` NOT restored.
- `com.unity.ai.inference` manifest entry NOT restored.
- `StaticEditorFlags.NavigationStatic` flag MUST stay (with `#pragma warning disable CS0618`).
- Branches whose only "uniqueness" is squash-merge SHA churn are IGNORE.

## Results

| Branch | Unique commits | Unique files (non-art src) | Verdict | Reason |
|---|---|---|---|---|
| agent/sprint-3-validation-and-depth | 4 | 9 src (DM/Memory/Reputation) | IGNORE | Old DM-query/memory/reputation scaffold superseded by mature `Domain/Memory/`, `FactionReputation`, `NpcMemoryStore`, `AskDmService`, `DmAgentToolSurface` on main. Branch diverged at start of Sprint 3. |
| agent/sprint-4-colony-needs-acceptance | 0 | 0 | IGNORE | HEAD == merge-base — fully on main. |
| agent/sprint-4-faz3-procedural-dungeon | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-4-faz4-equipment-inventory-ui | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-4-faz5-audio-atmosphere | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-4-ipathfinder-compat | 3 | 0 | IGNORE | `csharp 9` fix already in main (`a2c5828`). `TryFindPath` API rename is cosmetic refactor (1 impl + 1 consumer). No bug fixed. |
| agent/sprint-4-jobassignment-dry | 3 | 0 | IGNORE | C# 9 fix is already-applied (cherry `-`). "DRY" refactor inlines `TryClaimCandidate` — opposite direction of main's `JobAssignmentSystem.cs`, which kept the helper after audit-pass refactors. |
| agent/sprint-5-direct-mana-restore-mana-asymmetric-zero-drain | 1 | 0 | IGNORE | Patch-id match (`-`): test + doc already on main (`SpellEffectResolutionServiceTests.cs`, `docs/sprint-5-spell-effect-direct-mana-restore-mana-asymmetric-zero-drain.md`). |
| agent/sprint-5-magic-effect-resolution | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-roll-doc-evidence | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-shield-buff-actor-keyed-batch-absorption | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-shield-buff-damage-absorption | 1 | 0 | IGNORE | Patch-id match (`-`): `ShieldBuffService`, `ShieldBuffAbsorptionResult`, tests, doc all on main. |
| agent/sprint-5-shield-buff-registry-tick-sweep | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-cooldown-foundation | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-roll-execution | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-success-chance | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-success-roll | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-target-costs | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-target-range | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint-5-spell-target-validation | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint3-phase1-validation-hardening | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint3-phase4-npc-memory | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint3-phase5-dm-query-layer | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint3-phase6-unity-ci-unlock | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint3-summary | 0 | 0 | IGNORE | HEAD == merge-base. |
| agent/sprint4-faz1-validation-baseline | 0 | 0 | IGNORE | HEAD == merge-base. |
| charactergen-worldgen | 16 | 0 | IGNORE | All non-art file diffs are: stale OnnxAssetForge/NativeLlmClient/EmbeddingClient/ModelManifest/ForgeLocator/ModelBootstrap (main has newer post-Sentis-removal versions); 568 art-PNG additions (purged); `WorldgenSmokeTest.cs` ADDED ON MAIN, missing from branch. Branch is BEHIND main. PR #203 + #202 + #205 already squash-merged the feature content. |
| codex/fix-ember-audit-pass | 0 | 0 | IGNORE | HEAD == merge-base (`f2e7c0d`, "fix: close ember audit regressions"). |
| docs/fix-faz-12-phase-labels | 1 | 0 | IGNORE | Patch-id match (`-`): merged via PR #111 (`9f88ad5`). |
| mami/audit-eighth-pass-cluster | 3 | 0 | IGNORE | Both unique commits' content is squash-merged via PR #200 (`4723c6a`) + PR #201 (`f751905`). |
| mami/audit-eighth-pass-fixes | 1 | 0 | IGNORE | Patch-id match (`-`): squashed as PR #200 (`4723c6a`). |
| mami/audit-ninth-pass-21-findings | 2 | 0 | IGNORE | One commit (`4c53db7`) is patch-id match (`-`) — squashed as PR #202 (`f0d0bbc`). The other (`13c75f6`) is PR #203's own commit, also folded into main via #205 / subsequent work. |
| mami/audit-seventh-pass-33-findings | 1 | 0 | IGNORE | Patch-id match (`-`): squashed as PR #199 (`16a33ef`). |
| mami/audit-sixth-pass-48-findings | 1 | 0 | IGNORE | Patch-id match (`-`): squashed as PR #198 (`2d277e7`). |
| mami/navmesh-deprecation-fix | 14 | 0 | IGNORE | **DO-NOT-PORT marker hit**: branch REMOVES `StaticEditorFlags.NavigationStatic` from `EmberTerrainBuilder.cs` — exactly the regression `8aae54c` ("nav flag restored") reverted on main. Other 13 commits are charactergen-worldgen feature commits already in main via PR #202/#203/#205. |

## Verdict summary

- **PORT_FORWARD: 0 branches**
- **NEEDS_REVIEW: 0 branches**
- **IGNORE: 35 branches**

Nothing to port. Mami's earlier squash-merges (PR #198 through #205) plus the
`fbf478e` + `54ad722` Unity-AI pass + `8aae54c` nav-flag-restore commits already
captured every functional change from these branches. The branches survived as
stale snapshots — some predating main by an entire sprint, others holding
purged sprite PNGs or the wrong direction on the NavMesh fix.

## Cleanup

All 35 branches are safe to delete from `origin`. See the
`Branch deletion log` section below for the executed list.
