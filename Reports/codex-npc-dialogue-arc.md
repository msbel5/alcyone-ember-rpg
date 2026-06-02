You are Codex (gpt-5.5, xhigh) working in the Unity 6 CRPG repo at C:/Users/msbel/projects/alcyone-ember-rpg
(project EmberCrpg, deterministic single-player CRPG with an embedded AI DM). Quality bar: SOLID, design
patterns (manager/strategy/factory where natural), LOW lines of code, log at each step, human-readable +
commented code. Verify your engine-free changes with `bash tools/validation/run-validation.sh --mode fallback`.
Do NOT commit (the orchestrator commits + builds). HARD CONSTRAINT: do NOT modify anything under
`Assets/Scripts/Presentation/Ember/WorldDirector/**` or `Assets/Scripts/Simulation/WorldDirector/**`
(another agent owns the procedural terrain there right now).

Fix three coupled NPC-dialogue problems found in a playtest of the runtime-generated world:

== 1) DOUBLE NPC SPRITE (clean this carefully — a prior attempt AI-drifted here) ==
Spawned worldgen NPCs (Assets/Scripts/Presentation/Ember/Views/EmberGeneratedActorSpawner.cs) render as a
billboard whose sprite is a generated portrait that contains TWO faces (multi-subject generation bias), and
that same portrait is being used as the in-world body — so each NPC reads as two overlapping faces.
Desired: the IN-WORLD NPC billboard shows a SINGLE figure; the face PORTRAIT appears ONLY in the dialogue
portrait slot (never as the in-world billboard). Investigate SpriteRegistry
(Assets/Scripts/Presentation/Ember/Sprites/SpriteRegistry.cs), the spawner's placeholder keys
(blacksmith/merchant/innkeeper/warrior/knight), and the generation prompts
(Assets/Scripts/Simulation/Generation/StaticPromptCatalog.cs + CoreAssetManifest.cs). If the NPC sprites are
generated, enforce SINGLE-SUBJECT prompts (exactly one person, centered, no second person/no crowd — the
project already learned SDXL-Turbo needs an explicit "exactly one" constraint, see the "dice" prompt). Add an
EditMode test if you add engine-free prompt logic.

== 2) NPC + DM PORTRAITS IN THE DIALOGUE SLOT ==
The dialogue portrait slot is a grey placeholder. Path: DialogBoxPanel.ResolvePortraitSprite ->
EmberWorldHost.GetSpriteFromHost(name) -> SpriteRegistry.GetSprite(name) (returns null for generated PNGs).
DomainSimulationAdapter.Dialog.cs returns "portrait_npc_placeholder" for EVERY speaker and maps NpcRole to
keys (Artisan->"blacksmith"). Wire the slot to LOAD THE GENERATED PORTRAIT PNG: dm_portrait.png for the
DM/oracle speaker, and a per-archetype NPC portrait for NPCs, from these paths in order:
  Application.persistentDataPath/Generated/Core/<id>.png  (built player),
  <projectRoot>/Assets/Generated/Core/<id>.png            (editor),
  Application.streamingAssetsPath/Generated/Core/<id>.png.
Load PNG bytes -> Texture2D.LoadImage -> Sprite.Create (mirror the loader in
Assets/Scripts/Presentation/Ember/UI/Options/GeneratedAssetsSection.Helpers.cs). Make the adapter send a real
per-speaker key (e.g. dm_portrait for the DM, and a stable archetype id for NPCs) and resolve it to the PNG.
Fall back to the existing placeholder when the asset is not generated yet. Note: dm_portrait is generated and
the per-archetype NPC portraits may need to be ADDED to CoreAssetManifest + StaticPromptCatalog (single-subject
character bust prompts) so the forge produces them.

== 3) LLM DIALOGUE CRASH: 'Native error: llama_decode failed: InvalidInputBatch' ==
Talking shows that raw native error in the dialogue box. Find the LLM inference path (search for llama_decode,
NativeLlmClient, the dialogue/oracle LLM call). Fix the InvalidInputBatch (commonly: an empty token batch, a
batch larger than n_batch, or a context/n_ctx overflow — clamp/guard the batch + prompt length). CRUCIAL: the
player must NEVER see a raw native error — on ANY LLM failure, degrade gracefully to the deterministic fallback
line (the project rule is "LLM is flavour/last-fallback only; the deterministic sim is the source of truth").

Report at the end: the exact files you changed and a one-line why for each. Remember: do NOT commit, do NOT
touch WorldDirector/**.
