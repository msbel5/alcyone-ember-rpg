#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DOTNET_CLI="${DOTNET_CLI:-/home/msbel/.dotnet/dotnet}"
MODE="auto"
UNITY_OVERRIDE=""
OUTPUT_DIR="${VALIDATION_OUTPUT_DIR:-$ROOT_DIR/validation-output}"

usage() {
  cat <<USAGE
Usage: tools/validation/run-validation.sh [--mode auto|unity|fallback] [--unity-path PATH] [--output-dir DIR]

Runs local validation for Alcyone Ember RPG.
- auto: run real Unity EditMode tests when a Unity editor binary is found; otherwise run the pure-C# NUnit fallback harness.
- unity: require a Unity editor binary and run real EditMode tests.
- fallback: run only the pure-C# NUnit fallback harness; this is not a Unity EditMode run.

Environment:
  DOTNET_CLI              .NET CLI path for fallback (default: /home/msbel/.dotnet/dotnet)
  UNITY_EDITOR/PATH/etc.  Unity editor binary candidates
  VALIDATION_OUTPUT_DIR   Evidence output directory (default: ./validation-output)
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mode)
      MODE="${2:-}"; shift 2 ;;
    --unity-path)
      UNITY_OVERRIDE="${2:-}"; shift 2 ;;
    --output-dir)
      OUTPUT_DIR="${2:-}"; shift 2 ;;
    -h|--help)
      usage; exit 0 ;;
    *)
      echo "FAIL unknown_arg=$1" >&2
      usage >&2
      exit 64 ;;
  esac
done

case "$MODE" in
  auto|unity|fallback) ;;
  *) echo "FAIL invalid_mode=$MODE" >&2; usage >&2; exit 64 ;;
esac

mkdir -p "$OUTPUT_DIR"
LOG_FILE="$OUTPUT_DIR/validation-$(date -u +%Y%m%dT%H%M%SZ).log"
LATEST_LOG="$OUTPUT_DIR/latest.log"
exec > >(tee "$LOG_FILE") 2>&1

finish_latest_log() {
  cp "$LOG_FILE" "$LATEST_LOG" 2>/dev/null || true
}
trap finish_latest_log EXIT

log_header() {
  echo "=== Alcyone Ember RPG validation ==="
  echo "timestamp_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "root=$ROOT_DIR"
  echo "mode=$MODE"
  echo "output_dir=$OUTPUT_DIR"
  echo "dotnet_cli=$DOTNET_CLI"
  echo "git_head=$(git -C "$ROOT_DIR" rev-parse --short HEAD 2>/dev/null || echo unknown)"
  echo
}

is_executable_file() {
  [[ -n "${1:-}" && -f "$1" && -x "$1" ]]
}

find_unity_editor() {
  if is_executable_file "$UNITY_OVERRIDE"; then
    printf '%s\n' "$UNITY_OVERRIDE"
    return 0
  fi

  local var value
  for var in UNITY_EDITOR UNITY_EXECUTABLE UNITY_BINARY UNITY_EXE UNITY_PATH; do
    value="${!var:-}"
    if is_executable_file "$value"; then
      printf '%s\n' "$value"
      return 0
    fi
  done

  local cmd
  for cmd in Unity unity-editor unity; do
    if command -v "$cmd" >/dev/null 2>&1; then
      printf '%s\n' "$(command -v "$cmd")"
      return 0
    fi
  done

  local candidates=(
    "$HOME/Unity/Hub/Editor"/*/Editor/Unity
    "$HOME/.local/share/unity3d/Hub/Editor"/*/Editor/Unity
    /opt/Unity/Editor/Unity
    /opt/unity/editor/Unity
    /usr/bin/unity-editor
    /usr/local/bin/unity-editor
    /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity
    /Applications/Unity/Unity.app/Contents/MacOS/Unity
  )

  local candidate
  for candidate in "${candidates[@]}"; do
    if is_executable_file "$candidate"; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

run_unity_editmode() {
  local unity_bin="$1"
  local unity_log="$OUTPUT_DIR/unity-editmode.log"
  local results="$OUTPUT_DIR/unity-editmode-results.xml"

  echo "STATUS unity_editor=FOUND path=$unity_bin"
  echo "STATUS unity_editmode=RUNNING"
  echo "command: '$unity_bin' -batchmode -projectPath '$ROOT_DIR' -runTests -testPlatform EditMode -testResults '$results' -logFile '$unity_log' -quit"

  set +e
  "$unity_bin" \
    -batchmode \
    -projectPath "$ROOT_DIR" \
    -runTests \
    -testPlatform EditMode \
    -testResults "$results" \
    -logFile "$unity_log" \
    -quit
  local exit_code=$?
  set -e

  echo "unity_exit_code=$exit_code"
  echo "unity_log=$unity_log"
  echo "unity_results=$results"
  if [[ $exit_code -eq 0 ]]; then
    echo "PASS unity_editmode"
  else
    echo "FAIL unity_editmode"
  fi
  return "$exit_code"
}

run_fallback_harness() {
  local project="$ROOT_DIR/tools/validation/fallback/ValidationFallbackHarness.csproj"
  local result_dir="$OUTPUT_DIR/fallback-test-results"
  mkdir -p "$result_dir"
  rm -f "$result_dir/fallback.trx"

  echo "STATUS unity_editor=BLOCKED reason=not_found"
  echo "STATUS fallback_harness=RUNNING note='Pure C# NUnit harness; not a Unity EditMode run.'"

  if [[ ! -x "$DOTNET_CLI" ]]; then
    echo "BLOCKED fallback_harness reason=dotnet_not_executable path=$DOTNET_CLI"
    return 2
  fi

  "$DOTNET_CLI" --info | sed -n '1,40p'
  echo
  echo "command: '$DOTNET_CLI' test '$project' --configuration Release --nologo --results-directory '$result_dir' --logger 'trx;LogFileName=fallback.trx'"

  set +e
  "$DOTNET_CLI" test "$project" \
    --configuration Release \
    --nologo \
    --results-directory "$result_dir" \
    --logger "trx;LogFileName=fallback.trx"
  local exit_code=$?
  set -e

  echo "fallback_exit_code=$exit_code"
  echo "fallback_trx=$result_dir/fallback.trx"
  if [[ $exit_code -eq 0 ]]; then
    echo "PASS fallback_harness"
  else
    echo "FAIL fallback_harness"
  fi
  return "$exit_code"
}

log_header
UNITY_BIN=""
if [[ "$MODE" != "fallback" ]] && UNITY_BIN="$(find_unity_editor)"; then
  run_unity_editmode "$UNITY_BIN"
elif [[ "$MODE" == "unity" ]]; then
  echo "BLOCKED unity_editmode reason=unity_editor_not_found"
  echo "Checked env vars: UNITY_EDITOR UNITY_EXECUTABLE UNITY_BINARY UNITY_EXE UNITY_PATH"
  echo "Checked commands: Unity unity-editor unity"
  echo "Checked common Unity Hub/Editor paths under HOME, /opt, /usr, /Applications"
  exit 2
else
  run_fallback_harness
fi
