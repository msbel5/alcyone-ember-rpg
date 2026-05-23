#!/usr/bin/env bash
# Audit (ninth pass G-P1) — POSIX-side fail-fast scan for unresolved git
# merge markers inside Assets/Scripts/**/*.cs. Build the marker literals
# from individual characters so this file itself never trips the scan.
set -u
ROOT="${1:-../../../Assets/Scripts}"

open=$(printf '<%.0s' 1 2 3 4 5 6 7)
close=$(printf '>%.0s' 1 2 3 4 5 6 7)

if [ ! -d "$ROOT" ]; then
    echo "guard-conflict-markers: skip — path '$ROOT' does not exist."
    exit 0
fi

if grep -r -l -F -e "$open" -e "$close" --include="*.cs" "$ROOT" 2>/dev/null; then
    echo "CONFLICT MARKER FOUND in $ROOT" >&2
    exit 1
fi

exit 0
