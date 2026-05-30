# EMB-006 — Real LLM round-trip PROVEN (2026-05-30)

The on-device Qwen2.5-1.5B-Instruct (GGUF, via LLamaSharp/llama.cpp, `USE_LLAMASHARP`) produced a
genuine, coherent, in-character Dungeon-Master response — not canned fallback text.

Captured by `EmberProofScreenshotDriver` `--ember-llm-proof` (inference run OFF the main thread via
Task.Run, polled by the coroutine — the gameplay-safe path) from the shipped Win64 build:

```
NativeLlm: EmberCrpg.Simulation.AiDm.NativeLlmClient
ModelPath: .../StreamingAssets/Models/qwen2.5-1.5b-instruct-q4_k_m.gguf
IsAvailable: True
Calling Complete() OFF the main thread (Task.Run); polling...
RESULT OK
--- RESPONSE TEXT ---
The tavern buzzes with whispers of the impending raid, tales of a powerful beast that roams the dark woods.
--- END ---
```

Prompt: system "You are the Ember dungeon master. Reply in one short, vivid sentence." +
turn "Player asks: What rumours stir in the tavern tonight?"

Proves: LLM genuinely wired (NativeLlmClient registered) · real model loads (llama.cpp loaded the
GGUF: vocab + EOS tokens + 302 MiB compute buffer) · coherent on-topic generation · off-thread
inference keeps the main loop responsive (no freeze).

Minor follow-up (not blocking): add "User:" / `<|im_end|>` as a stop sequence so the model can't
bleed into hallucinating the next turn (the response itself is clean; only a trailing "User:" token
leaked). Tracked under dialogue polish.

To re-run: `alcyone-ember-rpg.exe --ember-llm-proof --ember-proof-screenshots <dir> --ember-proof-quit`
then read `<dir>/llm-proof.txt`.
