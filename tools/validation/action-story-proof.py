#!/usr/bin/env python3
"""Build fail-closed JSONL evidence from a fresh windowed-player action log."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ACTION_RE = re.compile(
    r"^\[Action\] t=(?P<tick>\d+) actor=(?P<actor>\d+) "
    r"intent=(?P<intent>\w+) ph=(?P<from_action>\w+)/(?P<from_phase>\w+)"
    r"->(?P<to_action>\w+)/(?P<to_phase>\w+) "
    r"tgt=site:(?P<site>\d+) why=(?P<reason>\w+)$"
)


def repo_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(ROOT).as_posix()
    except ValueError as exc:
        raise ValueError(f"proof artifact is outside the repository: {resolved}") from exc


def action_rows(log_path: Path) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for raw in log_path.read_text(encoding="utf-8", errors="replace").splitlines():
        match = ACTION_RE.match(raw.strip())
        if not match:
            continue
        item = match.groupdict()
        rows.append(
            {
                "actor_id": int(item["actor"]),
                "from_action": item["from_action"],
                "from_phase": item["from_phase"],
                "intent": item["intent"],
                "kind": "action_transition",
                "proof_level": "built-player-log",
                "reason": item["reason"],
                "site_id": int(item["site"]),
                "source": repo_path(log_path),
                "tick_minutes": int(item["tick"]),
                "to_action": item["to_action"],
                "to_phase": item["to_phase"],
            }
        )
    return rows


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def build(args: argparse.Namespace) -> list[dict[str, object]]:
    player = Path(args.player)
    if not player.is_file():
        raise ValueError(f"built player is missing: {player}")

    logs = [Path(value) for value in args.log]
    for log in logs:
        if not log.is_file():
            raise ValueError(f"player log is missing: {log}")

    text = "\n".join(
        log.read_text(encoding="utf-8", errors="replace") for log in logs
    )
    for marker in args.require_marker:
        if marker not in text:
            raise ValueError(f"required player marker is missing: {marker}")

    rows = [row for log in logs for row in action_rows(log)]
    observed = {
        str(row[field])
        for row in rows
        for field in ("from_action", "to_action")
    }
    missing_actions = sorted(set(args.require_action) - observed)
    if missing_actions:
        raise ValueError(
            "required action transition is missing: " + ", ".join(missing_actions)
        )

    screenshot_dir = Path(args.screenshot_dir)
    for name in args.require_screenshot:
        screenshot = screenshot_dir / name
        if not screenshot.is_file() or screenshot.stat().st_size == 0:
            raise ValueError(f"required screenshot is missing or empty: {screenshot}")
        rows.append(
            {
                "kind": "screenshot",
                "path": repo_path(screenshot),
                "proof_level": "built-player-screenshot",
                "sha256": sha256(screenshot),
                "size_bytes": screenshot.stat().st_size,
            }
        )

    rows.insert(
        0,
        {
            "kind": "proof_manifest",
            "player": repo_path(player),
            "player_sha256": sha256(player),
            "proof_level": "built-player",
            "run_label": args.run_label,
        },
    )
    return rows


def self_test() -> int:
    sample = (
        "[Action] t=61 actor=7 intent=Eat "
        "ph=MoveToFood/Running->MoveToFood/Succeeded "
        "tgt=site:1 why=Arrived"
    )
    match = ACTION_RE.match(sample)
    assert match is not None
    assert match.group("to_action") == "MoveToFood"
    assert ACTION_RE.match("[Action] guessed activity=eating") is None
    print("PASS action-story-proof self-test")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--player")
    parser.add_argument("--log", action="append", default=[])
    parser.add_argument("--screenshot-dir")
    parser.add_argument("--output")
    parser.add_argument("--run-label", default="prd-09")
    parser.add_argument("--require-action", action="append", default=[])
    parser.add_argument("--require-marker", action="append", default=[])
    parser.add_argument("--require-screenshot", action="append", default=[])
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    required = ("player", "screenshot_dir", "output")
    missing = [name for name in required if not getattr(args, name)]
    if missing or not args.log:
        parser.error("missing required proof inputs: " + ", ".join(missing or ["log"]))

    try:
        rows = build(args)
        output = Path(args.output)
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(
            "".join(json.dumps(row, sort_keys=True) + "\n" for row in rows),
            encoding="utf-8",
        )
    except (OSError, ValueError) as exc:
        print(f"FAIL action-story-proof: {exc}", file=sys.stderr)
        return 1
    print(f"PASS action-story-proof rows={len(rows)} output={repo_path(output)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
