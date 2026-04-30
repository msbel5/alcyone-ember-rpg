#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE_COMMIT="${SPRINT4_BASE_COMMIT:-b05100026c081361cf6f4c660dfa77fe620d644f}"
OLD_LINEAGE_COMMITS=(52f2e1e 116ae2e)

cd "$ROOT_DIR"

echo "=== Sprint 4 branch hygiene ==="
echo "root=$ROOT_DIR"
echo "base_commit=$BASE_COMMIT"
echo "head=$(git rev-parse --short HEAD)"
echo

if ! git cat-file -e "$BASE_COMMIT^{commit}" 2>/dev/null; then
  echo "FAIL base_commit_not_found=$BASE_COMMIT"
  exit 1
fi

if ! git merge-base --is-ancestor "$BASE_COMMIT" HEAD; then
  echo "FAIL head_does_not_descend_from_sprint4_base=$BASE_COMMIT"
  exit 1
fi

fresh_range="$BASE_COMMIT..HEAD"
for old_commit in "${OLD_LINEAGE_COMMITS[@]}"; do
  if git rev-list "$fresh_range" | grep -q "^$(git rev-parse --verify --quiet "$old_commit^{commit}" 2>/dev/null || printf '%s' "$old_commit")$"; then
    echo "FAIL old_lineage_commit_in_fresh_sprint4_range=$old_commit"
    exit 1
  fi
done

echo "PASS sprint4_branch_hygiene"
echo "note=Exact old lineage hashes 52f2e1e/116ae2e are not present as fresh commits; reviewers must still reject copied or cherry-picked old work."
