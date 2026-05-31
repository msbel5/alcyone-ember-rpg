#!/usr/bin/env bash
# Ember static asset/source audit (audit items EMB-004 / EMB-002 / EMB-003 / quick-checks §9).
#
# Pure bash + coreutils — no Unity, no LFS bytes required. Safe to run on a clean `lfs:false`
# checkout (that is exactly the false-green scenario it exists to catch).
#
# Sections:
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
#   tools/validation/static-audit.sh --quiet                          # summary lines only
set -u

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT" || exit 2

REQUIRE_RUNTIME=0
REQUIRE_RUNTIME_VISUAL=0
QUIET=0
for arg in "$@"; do
  case "$arg" in
    --require-runtime) REQUIRE_RUNTIME=1 ;;
    --require-runtime-visual) REQUIRE_RUNTIME=1; REQUIRE_RUNTIME_VISUAL=1 ;;
    --quiet) QUIET=1 ;;
  esac
done

FAIL=0
say()  { [ "$QUIET" -eq 1 ] || echo "$@"; }
head() { say ""; say "=== $* ==="; }

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
  while IFS= read -r asset; do
    case "$asset" in *.meta) continue ;; esac
    case "$asset" in */.*|.*) continue ;; esac
    if [ -z "$(git ls-files "${asset}.meta" 2>/dev/null)" ]; then
      echo "  UNTRACKED meta for tracked asset: ${asset}.meta"
      UNTRACKED_META=$((UNTRACKED_META+1))
    fi
  done < <(git ls-files Assets 2>/dev/null)
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
  while IFS= read -r meta; do
    case "$meta" in *.meta) ;; *) continue ;; esac
    asset="${meta%.meta}"
    if git check-ignore -q "$asset" 2>/dev/null && ! git check-ignore -q "$meta" 2>/dev/null; then
      echo "  TRACKED meta for gitignored asset: $meta"
      IGNORED_ASSET_META=$((IGNORED_ASSET_META+1))
    fi
  done < <(git ls-files Assets 2>/dev/null)
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
