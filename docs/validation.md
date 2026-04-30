# Local validation on Pi

Run validation from the repo root:

```bash
tools/validation/run-validation.sh
```

The script writes evidence logs and test artifacts under `validation-output/` (gitignored). The latest full log is copied to `validation-output/latest.log`.

## What the script does

1. Detects a Unity editor binary from:
   - `--unity-path PATH`
   - `UNITY_EDITOR`, `UNITY_EXECUTABLE`, `UNITY_BINARY`, `UNITY_EXE`, `UNITY_PATH`
   - `Unity`, `unity-editor`, or `unity` on `PATH`
   - common Unity Hub/editor locations under `$HOME`, `/opt`, `/usr`, and `/Applications`
2. If Unity is found, runs real EditMode tests with:

```bash
<Unity> -batchmode -projectPath <repo> -runTests -testPlatform EditMode -testResults validation-output/unity-editmode-results.xml -logFile validation-output/unity-editmode.log -quit
```

3. If Unity is not found, runs a deterministic pure-C# NUnit fallback harness with `/home/msbel/.dotnet/dotnet`:

```bash
/home/msbel/.dotnet/dotnet test tools/validation/fallback/ValidationFallbackHarness.csproj --configuration Release --nologo --results-directory validation-output/fallback-test-results --logger 'trx;LogFileName=fallback.trx'
```

## Fallback meaning

The fallback harness compiles and runs:

- `Assets/Scripts/Domain/**/*.cs`
- `Assets/Scripts/Simulation/**/*.cs`
- `Assets/Scripts/Data/Save/**/*.cs`
- `Assets/Tests/EditMode/**/*.cs`
- `tools/validation/fallback/UnityJsonUtilityStub.cs`

It intentionally excludes Unity presentation/runtime `MonoBehaviour` code. It includes a minimal `UnityEngine.JsonUtility` stub backed by `System.Text.Json` so save/load tests can execute without the Unity editor.

**Important:** fallback PASS means the pure domain/simulation/save EditMode test corpus passed under .NET 9. It is not a real Unity EditMode run and does not validate Unity assembly definitions, editor import/serialization quirks, scenes, input, rendering, or PlayMode behavior.

Use explicit modes when needed:

```bash
# Require a real Unity editor; exits BLOCKED if not found.
tools/validation/run-validation.sh --mode unity

# Force the .NET fallback; still not a Unity run.
tools/validation/run-validation.sh --mode fallback
```
