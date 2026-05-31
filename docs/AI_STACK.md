# Ember — AI Stack (authoritative)

This file defines model/runtime policy and proof boundaries.

## Local LLM policy

- Primary: local GGUF via `NativeLlmClient` (`USE_LLAMASHARP` path).
- Fallback: explicit canned/offline response paths.
- Cloud/provider paths are opt-in and non-authoritative.
- LLM output is flavour only; deterministic simulation remains authority.

## Image generation policy

- Primary forge path: ONNX runtime pipelines (SDXL/SD1.5 fallback).
- Generation failures must log and continue; no boot hard-abort.
- Runtime generation is local-first and visible to the player.

## Proof boundary labels

- `source-only`: code/static checks only (`lfs:false` checkout).
- `LFS-runtime`: real model/plugin/art bytes present.
- `Unity PlayMode`: scene/runtime behavior.
- `manual screenshot`: human visual evidence.
- `historical`: archived evidence.

Green source-only checks do **not** prove real LLM/art runtime behavior.

## Runtime verification checklist

1. Resolve runtime bytes (`git lfs pull`) or explicitly fetch via approved
   model bootstrap flow.
2. Run runtime pointer gates:
   - `bash tools/validation/static-audit.sh --require-runtime`
   - `bash tools/validation/static-audit.sh --require-runtime --require-runtime-visual`
3. Capture Unity runtime proof (PlayMode or player logs/screenshots).

## Download behavior guardrail

- Multi-GB model downloads must be explicit and user-visible (progress/log).
- No silent background download that can be confused with completed runtime.

## Current caution

Any claim like "real local LLM worked" is valid only if attached to
`LFS-runtime` + runtime evidence. Do not label fallback output as real model
inference.
