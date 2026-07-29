#!/usr/bin/env bash
# Ember static asset/source audit (audit items EMB-004 / EMB-002 / EMB-003 / quick-checks §9).
#
# Bash + coreutils + Python 3 — no Unity, no LFS bytes required. Safe to run on a clean `lfs:false`
# checkout (that is exactly the false-green scenario it exists to catch).
#
# Sections:
#   0. README local document references -> HARD FAIL (dangling authority)
#   0b.Atlas authority manifest/index    -> HARD FAIL (portable paths + proof status)
#   1. Duplicate .meta GUIDs             -> HARD FAIL (always corrupts Unity asset identity)
#   2. LFS pointer runtime/plugin/model  -> reported; HARD FAIL with --require-runtime
#   2b.LFS pointer runtime visual assets -> reported; HARD FAIL with --require-runtime-visual
#   3. Missing .meta (asset w/o .meta)   -> WARN (lists offenders)
#   4. Orphan .meta (.meta w/o asset)    -> WARN (lists offenders; gitignored-binary metas noted)
#   5. Informational greps               -> counts only (Input. / PlayerPrefs / Task.Run / GetResult)
#
# Usage:
#   tools/validation/static-audit.sh                                  # source-only report mode
#   tools/validation/static-audit.sh --require-runtime                # strict plugins/models
#   tools/validation/static-audit.sh --require-runtime --require-runtime-visual
#                                                                      # strict plugins/models + art visuals
#   tools/validation/static-audit.sh --readme-links-only               # targeted authority gate
#   tools/validation/static-audit.sh --quiet                          # summary lines only
set -u

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT" || exit 2

REQUIRE_RUNTIME=0
REQUIRE_RUNTIME_VISUAL=0
README_LINKS_ONLY=0
QUIET=0
for arg in "$@"; do
  case "$arg" in
    --require-runtime) REQUIRE_RUNTIME=1 ;;
    --require-runtime-visual) REQUIRE_RUNTIME=1; REQUIRE_RUNTIME_VISUAL=1 ;;
    --readme-links-only) README_LINKS_ONLY=1 ;;
    --quiet) QUIET=1 ;;
  esac
done

FAIL=0
say()  { [ "$QUIET" -eq 1 ] || echo "$@"; }
head() { say ""; say "=== $* ==="; }

# ---------------------------------------------------------------------------
# 0. README local document references  (HARD FAIL — PRD-00 authority lock)
#    Validate both Markdown links and inline-code paths by checking every
#    docs/*.md token. Removing link syntax must not bypass the gate.
# ---------------------------------------------------------------------------
head "0. README local document references"
README_DOC_REFS="$(grep -oE 'docs/[A-Za-z0-9._/-]+\.md' README.md 2>/dev/null | sort -u)"
MISSING_README_DOCS=0
while IFS= read -r doc; do
  [ -z "$doc" ] && continue
  if [ ! -f "$doc" ]; then
    echo "  MISSING README document: $doc"
    MISSING_README_DOCS=$((MISSING_README_DOCS+1))
  fi
done <<< "$README_DOC_REFS"
if [ "$MISSING_README_DOCS" -eq 0 ]; then
  say "PASS: every README docs/*.md reference exists."
else
  echo "FAIL: $MISSING_README_DOCS README document reference(s) are dangling."
  FAIL=1
fi

if [ "$README_LINKS_ONLY" -eq 1 ]; then
  head "RESULT"
  if [ "$FAIL" -eq 0 ]; then echo "static-audit PASS"; else echo "static-audit FAIL"; fi
  exit "$FAIL"
fi

# ---------------------------------------------------------------------------
# 0b. Atlas authority  (HARD FAIL — PRD-09 portable navigation/proof status)
# ---------------------------------------------------------------------------
head "0b. Atlas authority"
ATLAS_PYTHON=()
if command -v python3 >/dev/null 2>&1 && python3 -c 'import sys; raise SystemExit(sys.version_info < (3, 8))' >/dev/null 2>&1; then
  ATLAS_PYTHON=(python3)
elif command -v python >/dev/null 2>&1 && python -c 'import sys; raise SystemExit(sys.version_info < (3, 8))' >/dev/null 2>&1; then
  ATLAS_PYTHON=(python)
elif command -v py >/dev/null 2>&1 && py -3 -c 'import sys; raise SystemExit(sys.version_info < (3, 8))' >/dev/null 2>&1; then
  ATLAS_PYTHON=(py -3)
fi

if [ "${#ATLAS_PYTHON[@]}" -eq 0 ]; then
  echo "FAIL: Python 3 is required for tools/validation/atlas-authority.py --check."
  FAIL=1
elif ATLAS_OUTPUT="$("${ATLAS_PYTHON[@]}" tools/validation/atlas-authority.py --check 2>&1)"; then
  say "$ATLAS_OUTPUT"
else
  echo "$ATLAS_OUTPUT"
  FAIL=1
fi

# ---------------------------------------------------------------------------
# 1. Duplicate .meta GUIDs  (HARD FAIL)
# ---------------------------------------------------------------------------
head "1. Duplicate .meta GUIDs"
DUP_GUIDS="$(grep -rh '^guid: ' Assets --include='*.meta' 2>/dev/null | sort | uniq -d)"
if [ -n "$DUP_GUIDS" ]; then
  echo "FAIL: duplicate GUIDs found:"
  while IFS= read -r g; do
    [ -z "$g" ] && continue
    echo "  $g"
    grep -rl "^$g$" Assets --include='*.meta' 2>/dev/null | sed 's/^/      /'
  done <<< "$DUP_GUIDS"
  FAIL=1
else
  say "PASS: no duplicate GUIDs."
fi

# ---------------------------------------------------------------------------
# 2. LFS pointer runtime plugin/model binaries
# ---------------------------------------------------------------------------
head "2. LFS pointer runtime plugins/models"
RUNTIME_PTRS="$(grep -rIl '^version https://git-lfs.github.com/spec/v1' \
               Assets/Plugins Assets/StreamingAssets 2>/dev/null)"
if [ -n "$RUNTIME_PTRS" ]; then
  N=$(printf '%s\n' "$RUNTIME_PTRS" | grep -c .)
  if [ "$REQUIRE_RUNTIME" -eq 1 ]; then
    echo "FAIL (--require-runtime): $N runtime plugin/model files are LFS pointers (run 'git lfs pull'):"
    printf '%s\n' "$RUNTIME_PTRS" | sed 's/^/  /'
    FAIL=1
  else
    say "INFO: $N runtime plugin/model files are LFS pointers — SOURCE-ONLY MODE."
    say "      EditMode/source tests are valid; build/forge/LLM proof is NOT (run 'git lfs pull')."
  fi
else
  say "PASS: no runtime plugins/models are LFS pointers — RUNTIME-PRESENT MODE."
fi

# ---------------------------------------------------------------------------
# 2b. LFS pointer runtime visual assets
# ---------------------------------------------------------------------------
head "2b. LFS pointer runtime visuals"
VISUAL_PTRS="$(grep -rIl '^version https://git-lfs.github.com/spec/v1' \
              Assets/Art Assets/Generated/Core 2>/dev/null)"
if [ -n "$VISUAL_PTRS" ]; then
  V=$(printf '%s\n' "$VISUAL_PTRS" | grep -c .)
  if [ "$REQUIRE_RUNTIME_VISUAL" -eq 1 ]; then
    echo "FAIL (--require-runtime-visual): $V runtime visual asset files are LFS pointers:"
    printf '%s\n' "$VISUAL_PTRS" | sed 's/^/  /'
    FAIL=1
  else
    say "INFO: $V runtime visual asset files are LFS pointers."
    say "      Use --require-runtime-visual to make this a hard runtime-proof gate."
  fi
else
  say "PASS: no runtime visual assets are LFS pointers."
fi

# ---------------------------------------------------------------------------
# 3. Missing .meta  (asset file/dir without a sibling .meta)  (WARN)
#    Skip hidden (dot-prefixed) paths — Unity's importer ignores anything whose
#    name starts with '.', so those never get .meta files by design (.idea, etc).
# ---------------------------------------------------------------------------
head "3. Missing .meta under Assets (non-hidden)"
MISSING=0
while IFS= read -r f; do
  case "$f" in *.meta) continue ;; esac
  case "$f" in */.*|.*) continue ;;   # hidden file or any segment starting with '.'
  esac
  [ -e "${f}.meta" ] || { echo "  MISSING meta: $f"; MISSING=$((MISSING+1)); }
done < <(find Assets -type f ! -name '*.meta' 2>/dev/null)
if [ "$MISSING" -eq 0 ]; then say "PASS: every non-hidden Asset file has a .meta on disk."; else say "WARN: $MISSING asset file(s) missing a .meta (Unity will mint local GUIDs)."; fi

# ---------------------------------------------------------------------------
# 3b. Tracked asset with UNTRACKED .meta  (HARD FAIL — clean-clone reference breakage)
#     The asset is committed but its .meta is not, so a fresh clone mints a new
#     local GUID and every scene/prefab reference to it breaks. This is the real
#     EMB-002 defect (font/plugin metas that were never `git add`ed).
# ---------------------------------------------------------------------------
head "3b. Tracked asset whose .meta is untracked"
UNTRACKED_META=0
if git rev-parse --git-dir >/dev/null 2>&1; then
  declare -A TRACKED_FILES=()
  TRACKED_ASSETS=()
  TRACKED_METAS=()
  while IFS= read -r -d '' tracked; do
    TRACKED_FILES["$tracked"]=1
    case "$tracked" in
      *.meta) TRACKED_METAS+=("$tracked") ;;
      */.*|.*) ;;
      *) TRACKED_ASSETS+=("$tracked") ;;
    esac
  done < <(git ls-files -z Assets 2>/dev/null)

  for asset in "${TRACKED_ASSETS[@]}"; do
    if [ -z "${TRACKED_FILES["${asset}.meta"]+present}" ]; then
      echo "  UNTRACKED meta for tracked asset: ${asset}.meta"
      UNTRACKED_META=$((UNTRACKED_META+1))
    fi
  done
  if [ "$UNTRACKED_META" -eq 0 ]; then say "PASS: every tracked asset has a tracked .meta."
  else echo "FAIL: $UNTRACKED_META tracked asset(s) have an untracked .meta (git add them)."; FAIL=1; fi
else
  say "SKIP: not a git repo."
fi

# ---------------------------------------------------------------------------
# 3c. Tracked .meta whose ASSET is gitignored  (HARD FAIL — HYG-11)
#     The reverse of 3b: a .meta is committed but its asset is gitignored (e.g. a
#     cuDNN .dll or an .onnx.data shard), so a clean clone gets a tracked .meta
#     with no asset = dangling import. This is the gap the cuDNN/onnx-meta hazard
#     exploited; gitignore the .meta alongside its asset to fix.
# ---------------------------------------------------------------------------
head "3c. Tracked .meta whose asset is gitignored"
IGNORED_ASSET_META=0
if git rev-parse --git-dir >/dev/null 2>&1; then
  declare -A IGNORED_PATHS=()
  while IFS= read -r -d '' ignored; do
    IGNORED_PATHS["$ignored"]=1
  done < <(
    for meta in "${TRACKED_METAS[@]}"; do
      printf '%s\0' "${meta%.meta}"
    done | git check-ignore --no-index -z --stdin 2>/dev/null
  )

  for meta in "${TRACKED_METAS[@]}"; do
    asset="${meta%.meta}"
    if [ -n "${IGNORED_PATHS["$asset"]+ignored}" ]; then
      echo "  TRACKED meta for gitignored asset: $meta"
      IGNORED_ASSET_META=$((IGNORED_ASSET_META+1))
    fi
  done
  if [ "$IGNORED_ASSET_META" -eq 0 ]; then say "PASS: no tracked .meta points at a gitignored asset."
  else echo "FAIL: $IGNORED_ASSET_META tracked .meta point at a gitignored asset (gitignore the .meta too)."; FAIL=1; fi
else
  say "SKIP: not a git repo."
fi

# ---------------------------------------------------------------------------
# 4. Orphan .meta  (.meta whose asset is gone)  (WARN)
# ---------------------------------------------------------------------------
head "4. Orphan .meta under Assets"
ORPHAN=0
while IFS= read -r m; do
  asset="${m%.meta}"
  if [ ! -e "$asset" ]; then
    # A .meta whose binary asset is gitignored (e.g. cuDNN .dll) is intentional, not a true orphan.
    if git check-ignore -q "$asset" 2>/dev/null; then
      say "  (ok) gitignored-asset meta: $m"
    else
      echo "  ORPHAN meta: $m"
      ORPHAN=$((ORPHAN+1))
    fi
  fi
done < <(find Assets -type f -name '*.meta' 2>/dev/null)
if [ "$ORPHAN" -eq 0 ]; then say "PASS: no orphan .meta (excluding gitignored-asset metas)."; else say "WARN: $ORPHAN orphan .meta file(s) (asset deleted but meta remains)."; fi

# ---------------------------------------------------------------------------
# 5. Informational source greps  (counts only — never fail)
# ---------------------------------------------------------------------------
head "5. Source hygiene counts (informational)"
c_input=$(grep -rIn -E '\bInput\.(GetKey|GetAxis|GetMouseButton|GetButton)' Assets/Scripts 2>/dev/null | grep -c . )
c_prefs=$(grep -rIn 'PlayerPrefs' Assets/Scripts 2>/dev/null | grep -c . )
c_taskrun=$(grep -rIn 'Task\.Run' Assets/Scripts 2>/dev/null | grep -c . )
c_block=$(grep -rIn 'GetAwaiter()\.GetResult()' Assets/Scripts 2>/dev/null | grep -c . )
say "  legacy UnityEngine.Input call sites : $c_input   (EMB-015)"
say "  PlayerPrefs usages                  : $c_prefs   (EMB-011)"
say "  Task.Run sites                      : $c_taskrun   (EMB-007/018)"
say "  sync .GetAwaiter().GetResult() sites: $c_block   (EMB-018)"

# ---------------------------------------------------------------------------
# 6. Determinism boundary guard  (HARD FAIL — EMB-038/039/040)
#    The authoritative tiers (Domain + the save mapper) must never depend on
#    wall-clock time or engine visual RNG, or deterministic replay breaks.
#    Forge image noise (System.Random) and ActorView shake (UnityEngine.Random)
#    are presentation-tier visual-only by design — see docs/DETERMINISM.md.
# ---------------------------------------------------------------------------
head "6. Determinism boundary (Domain + Data/Save authoritative tiers)"
LEAK="$(grep -rIn -E 'UnityEngine\.Random|DateTime\.(UtcNow|Now)' \
        Assets/Scripts/Domain Assets/Scripts/Data/Save 2>/dev/null)"
if [ -n "$LEAK" ]; then
  echo "FAIL: wall-clock/engine-RNG leaked into an authoritative tier (breaks deterministic replay):"
  printf '%s\n' "$LEAK" | sed 's/^/  /'
  FAIL=1
else
  say "PASS: no DateTime.Now/UtcNow or UnityEngine.Random in Domain or Data/Save."
fi

# ---------------------------------------------------------------------------
head "RESULT"
if [ "$FAIL" -eq 0 ]; then echo "static-audit PASS"; else echo "static-audit FAIL"; fi
exit "$FAIL"
