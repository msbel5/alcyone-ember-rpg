# Sprint 3 Validation — Canonical Adapter

This is the requested Sprint 3 validation path. The validation procedure remains the repo-local script documented in `docs/validation.md`; this page adapts that truth into the `docs/` Sprint path so task runners and reviewers have one canonical Sprint 3 entry point.

## Run from repo root

```bash
tools/validation/run-validation.sh
```

The script writes evidence logs and test artifacts under `validation-output/` (gitignored). The latest full log is copied to `validation-output/latest.log`.

## What the script does

1. Detects a Unity editor binary from explicit flags, Unity-related environment variables, `Unity`/`unity-editor`/`unity` on `PATH`, and common install locations.
2. If Unity is found, runs real EditMode tests and writes `validation-output/unity-editmode-results.xml` plus `validation-output/unity-editmode.log`.
3. If Unity is not found, runs the deterministic pure-C# NUnit fallback harness with `/home/msbel/.dotnet/dotnet`.

## Fallback meaning

Fallback PASS means the pure domain/simulation/save/pure-presentation EditMode test corpus passed under .NET 8. It is not a real Unity EditMode run and does not validate Unity assembly definitions, editor import/serialization quirks, scenes, input, rendering, or PlayMode behavior.

Use explicit modes when needed:

```bash
# Require a real Unity editor; exits BLOCKED if not found.
tools/validation/run-validation.sh --mode unity

# Force the .NET fallback; still not a Unity run.
tools/validation/run-validation.sh --mode fallback
```

Keep this adapter aligned with `docs/validation.md` when validation behavior changes.
