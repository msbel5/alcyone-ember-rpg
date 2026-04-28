# alcyone-ember-rpg

Alcyone agent sandbox. Source: https://github.com/msbel5/ember-rpg

## Why this exists
Mami can'\''t fork her own repo. So Alcyone creates a private alcyone-*
mirror to iterate without dirtying the main code. See
~/.openclaw/workspace/AGENT_REPO_WORKFLOW.md.

## How to fetch files from source as needed
```bash
# Fetch a single file (no full clone)
gh api repos/msbel5/ember-rpg/contents/<path> --jq .content | base64 -d > <path>

# Or sparse-clone a subdirectory
git clone --filter=blob:none --no-checkout https://github.com/msbel5/ember-rpg.git src
cd src && git sparse-checkout init --cone && git sparse-checkout set <subdir> && git checkout main
```

## Branches
- main — clean from source (initially empty)
- agent/* — Alcyone'\''s work branches per AGENT_REPO_WORKFLOW.md

## Inspector approval gate
Every PR/branch from Alcyone needs Inspector APPROVED before merge.
