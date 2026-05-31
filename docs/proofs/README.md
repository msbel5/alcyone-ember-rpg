# Proof Artifact Policy

Every proof artifact must carry one mode label:

- `source-only`
- `LFS-runtime`
- `Unity PlayMode`
- `manual screenshot`
- `historical`

## What source-only can prove

- Static repo hygiene
- Pure C# deterministic tests
- Compile-time structure checks

## What source-only cannot prove

- Real local-LLM inference quality
- ONNX/art generation with runtime model bytes
- Runtime scene wiring and player interaction quality

## Required commands by proof level

```bash
# source-only
bash tools/validation/static-audit.sh
bash tools/validation/run-validation.sh --mode fallback

# runtime bytes present checks
bash tools/validation/static-audit.sh --require-runtime
bash tools/validation/static-audit.sh --require-runtime --require-runtime-visual
```

Runtime claims must include logs/screenshots captured from Unity PlayMode or a
player run.
