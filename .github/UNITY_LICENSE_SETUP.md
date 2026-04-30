# Unity License Setup (one-time, ~5 min)

GitHub Actions Unity Test Runner needs a Unity license. For Personal (free) license:

## Step 1 — Add UNITY_EMAIL + UNITY_PASSWORD to GitHub Secrets

1. Go to https://github.com/msbel5/alcyone-ember-rpg/settings/secrets/actions
2. Add Repository secrets:
   - `UNITY_EMAIL`: your Unity ID email
   - `UNITY_PASSWORD`: your Unity ID password

## Step 2 — Activate Unity License via GameCI workflow

Run this once locally on your dev machine (or trigger manually via GitHub Actions):

### Option A — Local activation (recommended)

```bash
docker run -it --rm -v "$(pwd):/project" \
  -e UNITY_EMAIL="$UNITY_EMAIL" \
  -e UNITY_PASSWORD="$UNITY_PASSWORD" \
  unityci/editor:6000.3.13f1-base-3 \
  unity-editor -batchmode -nographics -createManualActivationFile -quit
```

This generates `Unity_v6000.x.alf` in the current directory.

Upload it to https://license.unity3d.com/manual to get `Unity_v6000.x.ulf`.

### Option B — Use GameCI activate workflow

Create `.github/workflows/unity-activate.yml` (template at https://game.ci/docs/github/activation/) and run via workflow_dispatch.

## Step 3 — Add UNITY_LICENSE secret

Open `Unity_v6000.x.ulf` in a text editor, copy ALL contents, paste as secret:
- Name: `UNITY_LICENSE`
- Value: full XML contents of .ulf file

## Step 4 — Verify

Push any commit. The CI should run and tests should pass (assuming code compiles).
