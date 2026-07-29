#!/usr/bin/env python3
"""Deterministic Atlas manifest, index, and closure-evidence gate."""

from __future__ import annotations

import argparse
import json
import re
import sys
import tempfile
from pathlib import Path, PurePosixPath, PureWindowsPath
from typing import Any, Iterable


REPO_ROOT = Path(__file__).resolve().parents[2]
ATLAS_DIR = REPO_ROOT / "docs" / "atlas"
MANIFEST_PATH = ATLAS_DIR / "systems.json"
INDEX_PATH = ATLAS_DIR / "INDEX.md"
SCORECARD_PATH = ATLAS_DIR / "BUG_REPORT_SCORECARD.md"

DOC_TOKEN_RE = re.compile(r"(?i)\bdocs/[A-Za-z0-9._/-]+\.md\b")
CAPABILITY_STATUS_RE = re.compile(r"\b(?:SHIPPED(?:-NO-TEST)?|PASS)\b", re.IGNORECASE)
CLOSED_PROOF_RE = re.compile(
    r"(?i)\b(failure|integration|regression)\s*=\s*`?([^`\s|]+)`?"
)

INDEX_PREAMBLE = """# SYSTEMS ATLAS — Explanatory/Historical Navigation

> This Atlas is a repository-relative navigation aid, not current
> implementation or closure authority. Method/callsite presence cannot close
> an item. Use [CURRENT_STATE](../recovery/CURRENT_STATE.md) and
> [IMPLEMENTATION_STATUS](../recovery/IMPLEMENTATION_STATUS.md) for current
> evidence status.
>
> Regenerate deterministically with
> `python tools/validation/atlas-authority.py --write`; validate with `--check`.

Usage: `rg 'Actor.Position' docs/atlas/` to find where fields live across systems.
Bug scorecard: [BUG_REPORT_SCORECARD.md](BUG_REPORT_SCORECARD.md)

"""


class AtlasError(ValueError):
    """A deterministic Atlas authority violation."""


def _read_manifest(path: Path) -> list[dict[str, Any]]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise AtlasError(f"{path}: invalid manifest: {exc}") from exc
    if not isinstance(value, list):
        raise AtlasError(f"{path}: manifest root must be a JSON array")
    return value


def _validate_repo_relative_path(value: str, *, field: str) -> PurePosixPath:
    if not value:
        raise AtlasError(f"{field}: path is empty")
    if "\\" in value:
        raise AtlasError(f"{field}: backslashes are forbidden: {value}")
    if (
        value.startswith("/")
        or value.startswith("//")
        or PureWindowsPath(value).is_absolute()
        or re.match(r"^[A-Za-z]:", value)
    ):
        raise AtlasError(f"{field}: absolute path is forbidden: {value}")

    path = PurePosixPath(value)
    if path.is_absolute() or "." in path.parts or ".." in path.parts:
        raise AtlasError(f"{field}: path must be normalized and repo-relative: {value}")
    if path.as_posix() != value:
        raise AtlasError(f"{field}: path is not normalized: {value}")
    return path


def validate_manifest(
    entries: list[dict[str, Any]], repo_root: Path, *, require_files: bool = True
) -> None:
    seen: set[str] = set()
    for index, entry in enumerate(entries):
        label = f"systems.json entry {index}"
        if not isinstance(entry, dict):
            raise AtlasError(f"{label}: entry must be an object")

        title = entry.get("title")
        file_value = entry.get("file")
        one_liner = entry.get("oneLiner")
        if not isinstance(title, str) or not title.strip():
            raise AtlasError(f"{label}: title must be a non-empty string")
        if not isinstance(file_value, str):
            raise AtlasError(f"{label}: file must be a string")
        if not isinstance(one_liner, str) or not one_liner.strip():
            raise AtlasError(f"{label}: oneLiner must be a non-empty string")
        if CAPABILITY_STATUS_RE.search(one_liner):
            raise AtlasError(
                f"{label}: capability-only closure wording is forbidden: {one_liner}"
            )

        relative = _validate_repo_relative_path(file_value, field=f"{label}.file")
        if relative.parts[:3] != ("docs", "atlas", "systems"):
            raise AtlasError(
                f"{label}.file: must remain under docs/atlas/systems: {file_value}"
            )
        if file_value in seen:
            raise AtlasError(f"{label}.file: duplicate manifest path: {file_value}")
        seen.add(file_value)
        if require_files and not (repo_root / Path(*relative.parts)).is_file():
            raise AtlasError(f"{label}.file: target does not exist: {file_value}")


def render_index(entries: list[dict[str, Any]]) -> str:
    rows: list[str] = []
    for entry in entries:
        repo_path = PurePosixPath(entry["file"])
        atlas_relative = repo_path.relative_to(PurePosixPath("docs/atlas"))
        rows.append(
            f"- [{entry['title']}]({atlas_relative.as_posix()}) - "
            f"{entry['oneLiner'].strip()}"
        )
    return INDEX_PREAMBLE + "\n".join(rows) + "\n"


def validate_index(entries: list[dict[str, Any]], index_path: Path) -> None:
    expected = render_index(entries)
    try:
        actual = index_path.read_text(encoding="utf-8")
    except OSError as exc:
        raise AtlasError(f"{index_path}: cannot read generated index: {exc}") from exc
    if actual != expected:
        raise AtlasError(
            f"{index_path}: stale generated index; run "
            "python tools/validation/atlas-authority.py --write"
        )


def iter_dangling_doc_tokens(atlas_dir: Path, repo_root: Path) -> Iterable[str]:
    for source in sorted(atlas_dir.rglob("*.md")):
        for line_number, line in enumerate(
            source.read_text(encoding="utf-8").splitlines(), start=1
        ):
            for match in DOC_TOKEN_RE.finditer(line):
                token = match.group(0).replace("\\", "/")
                relative = PurePosixPath(token)
                target = repo_root / Path(*relative.parts)
                if not target.is_file():
                    display = source.relative_to(repo_root).as_posix()
                    yield f"{display}:{line_number}: dangling document token: {token}"


def validate_doc_tokens(atlas_dir: Path, repo_root: Path) -> None:
    dangling = list(iter_dangling_doc_tokens(atlas_dir, repo_root))
    if dangling:
        raise AtlasError("\n".join(dangling))


def validate_scorecard(scorecard_path: Path, repo_root: Path) -> None:
    try:
        lines = scorecard_path.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        raise AtlasError(f"{scorecard_path}: cannot read scorecard: {exc}") from exc

    for line_number, line in enumerate(lines, start=1):
        if not re.match(r"^\|\s*B\d+\s*\|", line):
            continue
        columns = [column.strip() for column in line.strip().strip("|").split("|")]
        if len(columns) < 3 or columns[2].strip("*` ").upper() != "CLOSED":
            continue

        proofs = {kind.lower(): value for kind, value in CLOSED_PROOF_RE.findall(line)}
        missing = [
            kind
            for kind in ("failure", "integration", "regression")
            if kind not in proofs
        ]
        if missing:
            raise AtlasError(
                f"{scorecard_path.relative_to(repo_root).as_posix()}:{line_number}: "
                f"CLOSED requires failure+integration+regression proof artifacts; "
                f"missing {', '.join(missing)}"
            )

        for kind, value in proofs.items():
            relative = _validate_repo_relative_path(
                value, field=f"scorecard line {line_number} {kind}"
            )
            target = repo_root / Path(*relative.parts)
            if not target.is_file():
                raise AtlasError(
                    f"{scorecard_path.relative_to(repo_root).as_posix()}:{line_number}: "
                    f"{kind} proof artifact does not exist: {value}"
                )


def check_tree(
    *,
    repo_root: Path = REPO_ROOT,
    atlas_dir: Path = ATLAS_DIR,
    manifest_path: Path = MANIFEST_PATH,
    index_path: Path = INDEX_PATH,
    scorecard_path: Path = SCORECARD_PATH,
) -> None:
    entries = _read_manifest(manifest_path)
    validate_manifest(entries, repo_root)
    validate_index(entries, index_path)
    validate_doc_tokens(atlas_dir, repo_root)
    validate_scorecard(scorecard_path, repo_root)


def write_index() -> None:
    entries = _read_manifest(MANIFEST_PATH)
    validate_manifest(entries, REPO_ROOT)
    INDEX_PATH.write_bytes(render_index(entries).encode("utf-8"))
    check_tree()


def _expect_failure(name: str, action: Any, contains: str) -> None:
    try:
        action()
    except AtlasError as exc:
        if contains.lower() not in str(exc).lower():
            raise AssertionError(
                f"{name}: expected error containing {contains!r}, got {exc!r}"
            ) from exc
        return
    raise AssertionError(f"{name}: expected AtlasError")


def run_self_test() -> None:
    cases = 0
    with tempfile.TemporaryDirectory(prefix="ember-atlas-authority-") as raw:
        root = Path(raw)
        atlas = root / "docs" / "atlas"
        systems = atlas / "systems"
        recovery = root / "docs" / "recovery"
        reports = root / "Reports" / "proof"
        systems.mkdir(parents=True)
        recovery.mkdir(parents=True)
        reports.mkdir(parents=True)
        (systems / "01-a.md").write_text("# A\n", encoding="utf-8")
        (recovery / "CURRENT_STATE.md").write_text("# Current\n", encoding="utf-8")
        good = [
            {
                "title": "A",
                "file": "docs/atlas/systems/01-a.md",
                "oneLiner": "Historical map of A.",
            }
        ]

        _expect_failure(
            "absolute",
            lambda: validate_manifest(
                [{**good[0], "file": "D:/repo/docs/atlas/systems/01-a.md"}],
                root,
            ),
            "absolute",
        )
        cases += 1
        _expect_failure(
            "backslash",
            lambda: validate_manifest(
                [{**good[0], "file": r"docs\atlas\systems\01-a.md"}], root
            ),
            "backslashes",
        )
        cases += 1
        _expect_failure(
            "missing",
            lambda: validate_manifest(
                [{**good[0], "file": "docs/atlas/systems/missing.md"}], root
            ),
            "does not exist",
        )
        cases += 1
        _expect_failure(
            "duplicate",
            lambda: validate_manifest([good[0], dict(good[0])], root),
            "duplicate",
        )
        cases += 1
        _expect_failure(
            "capability-status",
            lambda: validate_manifest(
                [{**good[0], "oneLiner": "A capability is SHIPPED."}], root
            ),
            "closure wording",
        )
        cases += 1

        index = atlas / "INDEX.md"
        index.write_text("stale\n", encoding="utf-8")
        _expect_failure(
            "stale-index",
            lambda: validate_index(good, index),
            "stale generated index",
        )
        cases += 1

        index.write_text(render_index(good), encoding="utf-8")
        dangling = atlas / "dangling.md"
        dangling.write_text("See `docs/missing.md`.\n", encoding="utf-8")
        _expect_failure(
            "dangling-doc",
            lambda: validate_doc_tokens(atlas, root),
            "dangling document token",
        )
        cases += 1
        dangling.unlink()

        scorecard = atlas / "BUG_REPORT_SCORECARD.md"
        scorecard.write_text(
            "| ID | Title | Status | Evidence |\n"
            "|---|---|---|---|\n"
            "| B21 | bounded log | CLOSED | method exists |\n",
            encoding="utf-8",
        )
        _expect_failure(
            "closed-without-proof",
            lambda: validate_scorecard(scorecard, root),
            "requires failure+integration+regression",
        )
        cases += 1

        for name in ("failure.log", "integration.log", "regression.log"):
            (reports / name).write_text("PASS\n", encoding="utf-8")
        scorecard.write_text(
            "| ID | Title | Status | Evidence |\n"
            "|---|---|---|---|\n"
            "| B21 | bounded log | CLOSED | "
            "failure=`Reports/proof/failure.log` "
            "integration=`Reports/proof/integration.log` "
            "regression=`Reports/proof/regression.log` |\n",
            encoding="utf-8",
        )
        validate_manifest(good, root)
        validate_index(good, index)
        validate_doc_tokens(atlas, root)
        validate_scorecard(scorecard, root)
        cases += 1

    print(f"atlas-authority self-test PASS ({cases} cases)")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Regenerate and validate the repository-relative Systems Atlas."
    )
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--write", action="store_true", help="regenerate INDEX.md")
    mode.add_argument("--check", action="store_true", help="validate current Atlas")
    mode.add_argument(
        "--self-test", action="store_true", help="exercise false-green rejection"
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        if args.self_test:
            run_self_test()
        elif args.write:
            write_index()
            print("atlas-authority write PASS")
        else:
            check_tree()
            print("atlas-authority check PASS")
    except (AtlasError, AssertionError) as exc:
        print(f"atlas-authority FAIL: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
