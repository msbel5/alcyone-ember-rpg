# Validation Modes

Run commands from repo root.

## 1) Source-only gate (fast)

```bash
bash tools/validation/static-audit.sh
bash tools/validation/run-validation.sh --mode fallback
```

Use this for structure/hygiene/pure-C# checks only.

## 2) Runtime plugin/model gate

```bash
bash tools/validation/static-audit.sh --require-runtime
```

Fails when runtime plugin/model files are still Git LFS pointers.

## 3) Runtime visual gate

```bash
bash tools/validation/static-audit.sh --require-runtime --require-runtime-visual
```

Also fails when visual/runtime art paths are still LFS pointers
(`Assets/Art/**`, `Assets/Generated/Core/**`).

## 4) Unity-required validation

- Unity EditMode and PlayMode runs
- Player build run-through
- Scene tour screenshots

These require Unity runtime/editor sessions and cannot be replaced by
source-only green checks.
