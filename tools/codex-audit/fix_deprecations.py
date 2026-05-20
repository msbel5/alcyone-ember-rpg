#!/usr/bin/env python3
"""
Faz 1 sweep — replace deprecated SliceWorldState role accessors:
  receiver.Player   = expr;  ->  receiver.ReplaceActorView(ActorRole.Player, expr);
  receiver.Player           ->  receiver.Actors.FirstByRole(ActorRole.Player)

Word boundary (?![\\w_]) after the role name prevents matches inside
identifiers like PlayerInventory, PlayerEquipment, etc.

Skips when the qualifier is `ActorRole.` (enum reference).
"""

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets"

DENY = {
    "Assets/Scripts/Domain/World/SliceWorldState.cs",
    "Assets/Scripts/Domain/Actors/ActorRole.cs",
    "Assets/Scripts/Domain/Actors/ActorStore.cs",
}

# Receiver identifiers that are ENUMS (not SliceWorldState instances).
# Skip them — `DungeonSpawnKind.Player` is an enum value, not a deprecated prop.
RECEIVER_SKIP = {"ActorRole", "DungeonSpawnKind"}

# ANY C# identifier as receiver — with skip for "ActorRole" in the callback.
RECEIVER = r"(?P<rcv>[A-Za-z_][A-Za-z0-9_]*)"
ROLE     = r"(?P<role>Player|Talker|Merchant|Guard|Enemy)"
BOUNDARY = r"(?![\w_])"

# 1) Assignment.
ASSIGN_RE = re.compile(
    r"(?<![\w.])" + RECEIVER + r"\." + ROLE + BOUNDARY + r"\s*=\s*(?!=)(?P<rhs>[^;]+?);",
    re.DOTALL,
)

# 2) Read.
READ_RE = re.compile(
    r"(?<![\w.])" + RECEIVER + r"\." + ROLE + BOUNDARY + r"(?!\s*=(?!=))",
)


def rewrite_text(src: str) -> tuple[str, int, int]:
    a_count = 0
    def _assign(m: re.Match) -> str:
        nonlocal a_count
        if m.group("rcv") in RECEIVER_SKIP:
            return m.group(0)
        a_count += 1
        rhs = m.group("rhs").strip()
        return f'{m.group("rcv")}.ReplaceActorView(ActorRole.{m.group("role")}, {rhs});'
    out = ASSIGN_RE.sub(_assign, src)

    r_count = 0
    def _read(m: re.Match) -> str:
        nonlocal r_count
        if m.group("rcv") in RECEIVER_SKIP:
            return m.group(0)
        r_count += 1
        return f'{m.group("rcv")}.Actors.FirstByRole(ActorRole.{m.group("role")})'
    out = READ_RE.sub(_read, out)
    return out, a_count, r_count


def ensure_using(src: str) -> str:
    if "EmberCrpg.Domain.Actors" in src:
        return src
    lines = src.splitlines(keepends=True)
    last_using = -1
    for i, line in enumerate(lines[:120]):
        if re.match(r"\s*using\s+[\w.]+\s*;\s*\r?\n?$", line):
            last_using = i
    if last_using == -1:
        return src
    sample = lines[last_using]
    nl = "\r\n" if sample.endswith("\r\n") else "\n"
    lines.insert(last_using + 1, f"using EmberCrpg.Domain.Actors;{nl}")
    return "".join(lines)


def iter_targets():
    for path in ASSETS.rglob("*.cs"):
        rel = path.relative_to(ROOT).as_posix()
        if rel in DENY:
            continue
        yield path, rel


def main() -> int:
    total_assign = 0
    total_read = 0
    touched = 0
    for path, rel in iter_targets():
        try:
            original = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            original = path.read_text(encoding="utf-8-sig")
        new, ac, rc = rewrite_text(original)
        if ac == 0 and rc == 0:
            continue
        if "ActorRole." in new and "EmberCrpg.Domain.Actors" not in new:
            new = ensure_using(new)
        path.write_text(new, encoding="utf-8")
        touched += 1
        total_assign += ac
        total_read += rc
        print(f"  ok : {rel}  (assign={ac} read={rc})")
    print()
    print(f"Touched {touched} files. Replacements: {total_assign} writes + {total_read} reads = {total_assign + total_read}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
