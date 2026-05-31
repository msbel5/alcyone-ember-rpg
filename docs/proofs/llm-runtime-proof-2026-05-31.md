# Runtime LLM proof — headless, log-based (2026-05-31)

Mode: **LFS-resolved Win64 player, headless auto-driver, no screenshots** (read the proof `.txt` +
`Player.log`, not images). Reproduces the real local-Qwen round-trip and validates the 2026-05-31
`NativeLlmClient` hardening at runtime.

## How it was run
```
Builds/Windows64/alcyone-ember-rpg.exe \
  --ember-proof-screenshots <out> --ember-llm-proof --ember-proof-quit \
  -logFile <player.log>
```
`EmberProofScreenshotDriver.RunLlmProof()` waits for `ForgeLocator.NativeLlm`, records availability,
then calls `NativeLlmClient.Complete()` **off the main thread** (the gameplay path) and writes the
response. Player exit code 0.

## Result (`<out>/llm-proof.txt`, verbatim)
```
NativeLlm:   EmberCrpg.Infrastructure.AiDm.NativeLlmClient
ModelPath:   …/alcyone-ember-rpg_Data/StreamingAssets/Models/qwen2.5-1.5b-instruct-q4_k_m.gguf
IsAvailable: True
Calling Complete() OFF the main thread (Task.Run); polling...
RESULT OK
--- RESPONSE TEXT ---
The tavern buzzes with whispers of the impending raid, tales of a powerful beast that roams the dark woods.
User:
--- END ---
```

## What it proves
- **`IsAvailable: True` with the real 986 MB GGUF** → the LEFT-005 hardening
  (`IsUsableModelFile`: GGUF magic + ≥1 MB) correctly *accepts* the genuine model. A bug in that gate
  would have read `False`. The `File.Exists` → `IsUsableModelFile` swap did **not** break detection.
- **`RESULT OK` + coherent, on-prompt Qwen output** (not the canned fallback string) → real local
  inference still works off the main thread after the hardening edits. No regression.

## What it surfaced (and the follow-up fix)
- The raw response carried a trailing **`User:`** turn-marker. This is the *raw* `Complete()` output —
  llama.cpp's `AntiPrompt` stops generation only *after* emitting the marker. The gameplay dialog path
  already strips this via the adapter's `SanitizeNpcLine`, so the user-visible bubble is clean; but the
  proof showed the raw client can leak it to any caller.
- **Fix applied at the source:** `NativeLlmClient.StripTrailingTurnMarkers` now trims a trailing
  `User:` / `Assistant:` / `System:` / `Memory:` / `<|im…` from every `Complete()` result
  (colon/tag-anchored so ordinary prose is untouched). +4 EditMode regression tests pin it, using this
  proof's exact leaked string as a fixture.

## Still `[E]` (needs a human/visual run, not headless)
Generated-NPC billboard *visibility*, the character-creation *portrait* on screen, the single-source
HUD layout, and the full scene-tour remain visual checks — tracked as `LEFT-16` in
`docs/REMEDIATION_V2_COUNTER.md`. This proof covers the LLM round-trip + readiness only.
