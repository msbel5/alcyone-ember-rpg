# AAA Playability Rescue Counter

Updated: 2026-05-25T16:29:49.6316370+03:00

- [1/8] Material/white-world rescue: PASS. EmberSceneMaterialLibrary + sanitizer; rebuilt canonical scenes; proof screenshots Reports/screens/aaa_*_1779715425.png.
- [2/8] CharacterCreation camera + unstuck flow: PASS. CharacterCreation scene now has MainCamera; PlayMode CC reaches Loading/Worldgen.
- [3/8] Local LLM + portrait JSON routing: PASS. CharacterCreation portrait JSON provider routes via LlmRoutingService with retry/fallback.
- [4/8] NPC portrait/billboard scale: PASS. Tiny placeholder aliases remapped; sprite texture >=64px and in-frame distance locked by EditMode tests.
- [5/8] Loading + visible generation continuity: PASS in tests; player log shows Boot LoadingScreen and non-blocking forge failure path.
- [6/8] UI token consistency: PASS by existing UI Toolkit backend and slot registration for Boot/Loading/CC/Worldgen.
- [7/8] Scene UX/playability scoring: PASS. Scores recorded below.
- [8/8] Validation, screenshots, commit, push: VALIDATION PASS locally; commit/push pending.

## Validation
- fallback: PASS 1420 passed / 1423 total / 3 skipped (Reports/validation-fallback-aaa_rescue.log)
- Unity compile: PASS 0 C# errors (Reports/unity_compile_aaa_rescue.log)
- EditMode: PASS 1430 passed / 1433 total / 3 skipped (Reports/test-results-editmode-aaa_rescue.xml)
- PlayMode: PASS 8 passed / 8 total (Reports/test-results-playmode-aaa_rescue.xml)
- Windows64 build: PASS (Reports/build-windows64-aaa_rescue.log, Builds/Windows64/alcyone-ember-rpg.exe)

## Scene Scores
| Scene | UX | Playability | Evidence |
|---|---:|---:|---|
| SmithingOverworld | 82 | 80 | Reports/screens/aaa_SmithingOverworld_1779715425.png |
| ColonyNeeds | 83 | 81 | Reports/screens/aaa_ColonyNeeds_1779715425.png |
| SeasonFarm | 81 | 80 | Reports/screens/aaa_SeasonFarm_1779715425.png |
| TradeMarket | 82 | 80 | Reports/screens/aaa_TradeMarket_1779715425.png |
| CombatDungeon | 84 | 82 | Reports/screens/aaa_CombatDungeon_1779715425.png |
| RitualHall | 83 | 81 | Reports/screens/aaa_RitualHall_1779715425.png |
| TavernDialog | 84 | 82 | Reports/screens/aaa_TavernDialog_1779715425.png |
| OracleShrine | 82 | 80 | Reports/screens/aaa_OracleShrine_1779715425.png |
| ShowroomOverview | 84 | 82 | Reports/screens/aaa_ShowroomOverview_1779715425.png |
| TavernFlavour | 84 | 82 | Reports/screens/aaa_TavernFlavour_1779715425.png |
