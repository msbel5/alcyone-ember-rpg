# Ember — Determinism Boundary

> Audit items EMB-038 / EMB-039 / EMB-040. Ember's bet is a **deterministic living-world**: the same
> seed + the same input command log must produce the same world. This page draws the line between
> what is authoritative (must be bit-stable) and what is presentation-tier (may vary per machine).

## Authoritative tiers — MUST be deterministic
- `EmberCrpg.Domain`, the authoritative parts of `EmberCrpg.Simulation`, and the save mapper
  (`Assets/Scripts/Data/Save`).
- These use **`DeterministicRng` seeded per tick** — never `System.Random`, never
  `UnityEngine.Random`, never wall-clock time.
- **Enforced** by `tools/validation/static-audit.sh` §6 (HARD FAIL if `DateTime.Now/UtcNow` or
  `UnityEngine.Random` appears under `Assets/Scripts/Domain` or `Assets/Scripts/Data/Save`).

## Presentation / cache tiers — NOT part of the replay digest
These intentionally use non-deterministic or runtime-specific sources because their output never
feeds the authoritative world state or the save:

- **Forge latent noise** — `Simulation/Forge/LatentNoiseSampler.SampleGaussian(uint seed, …)` uses
  `new System.Random((int)seed)`. The output is the **generated image**, a per-playthrough *visual
  cache* on the player's machine. Images are generated locally per run (Ember ships code, the player
  generates assets) and are **not** shared/replayed across machines, so cross-runtime bit-stability of
  the noise is not required and not promised. The seed gives per-run reproducibility within a machine;
  it is explicitly **not** part of the deterministic-replay digest.
- **Generation logging/timing** — `GenerationFailureLog` / `VisibleGenerationPipeline` use
  `DateTime.UtcNow` for log timestamps and elapsed-ms timing only. Timestamps **never** enter a
  generated-asset ID or any world/save field.
- **Actor billboard shake** — `Presentation/Ember/Views/ActorView` uses `UnityEngine.Random.Range`
  for a tiny visual jitter. Presentation-only; never read by Domain/Simulation/save.

## Rule for new code
- Touching world state, the tick, or the save? → `DeterministicRng`, no wall-clock.
- Generating a visual/audio asset or a purely cosmetic effect? → any RNG is fine, but keep its output
  out of the authoritative tiers and out of any asset ID that the seed contract depends on.
- If a generated asset ever becomes part of the seed contract (shared/replayed), move its sampler to
  `DeterministicRng` first.
