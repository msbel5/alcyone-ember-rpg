# ACTIVE working tasks — GenerationManager overhaul (DELETE this folder after implementation)

Source of truth: `docs/prds/PRD_generation_manager_v1.md`. This folder is scratch for the Codex/agent
implementation passes and should be `git rm -r docs/prds/_active` once everything below is `[x]` + merged.

Hard invariants (every slice): one generation at a time (capacity 1); Domain/Sim engine-free + deterministic;
`IAssetForge` contract unchanged; LLM crafts prompts only; fallback harness green + Win64 build Success.

## Slices
- [ ] **S1 — GenerationManager + serialize.** New `GenerationManager` (Sim core) owning the EXISTING
      `AssetForgeQueue` at **capacity 1** + a single worker draining it (priority: PlayerFacing→Nearby→
      Background). Route every image gen through it. Test: two concurrent `GenerateAsync` calls run
      sequentially; PlayerFacing preempts Background.
- [ ] **S2 — IResourceProbe + guard.** Engine-free `IResourceProbe` (impl in Presentation reads
      `SystemInfo.graphicsMemorySize`/`systemMemorySize`/process working set). Manager downscales (1024→512)
      or defers when below a kind threshold. Test: low-probe forces 512.
- [ ] **S3 — IPromptComposer + GeometricPromptCatalog.** Per-object GEOMETRIC templates + anti-pattern
      negatives (die/sword/potion/key/staff/shield/…); wire `LlmPromptComposer` as the optional refine step;
      expose via `IImageGenSpecFactory`. Test: composed die prompt contains "cube/faces/pips" + negative has
      "pile/many", not the literal "{slot}".
- [ ] **S4 — route portrait + CoreAssetManifest through the manager** (SDXL@1024 where CUDA, downscale to
      slot). Bump `GeneratedAssetProvenance.Version` to regenerate.
- [ ] **S5 — human eyeball** a die + sword + potion via `--ember-forge-proof`; confirm single recognizable
      objects.

## Codex invocation (per slice)
```
codex exec -m gpt-5.3-codex --config model_reasoning_effort="high" --sandbox workspace-write \
  --skip-git-repo-check -C <repo> -o <out> "<slice prompt: read PRD_generation_manager_v1.md, do Sx
  test-first, verify fallback, DO NOT git commit>" </dev/null 2><err>
```
Claude reviews each slice diff, Win64-builds it, commits + pushes. Eyeball checks (S5) are Claude+user.
