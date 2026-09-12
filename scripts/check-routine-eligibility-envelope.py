#!/usr/bin/env python3
"""Validate the trusted routine-event envelope before invoking the base validator."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path


SHA = re.compile(r"^[0-9a-f]{40}$")


def fail(message: str) -> None:
    raise SystemExit(f"routine-eligibility: {message}")


def revision(git_dir: Path, value: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(git_dir), "rev-parse", value],
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        fail(f"cannot resolve {value}: {' '.join(result.stderr.split())}")
    return result.stdout.strip()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--git-dir", type=Path, required=True)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--default-branch", required=True)
    parser.add_argument("--base-ref", required=True)
    parser.add_argument("--base-revision", required=True)
    parser.add_argument("--base-sha", required=True)
    parser.add_argument("--head-ref", required=True)
    parser.add_argument("--head-revision", required=True)
    parser.add_argument("--head-sha", required=True)
    parser.add_argument("--body", type=Path, required=True)
    parser.add_argument("--validator", type=Path, required=True)
    args = parser.parse_args()

    if args.base_ref != args.default_branch:
        fail(f"PR target {args.base_ref!r} is not default branch {args.default_branch!r}")
    if not SHA.fullmatch(args.base_sha):
        fail("base SHA is not exact lowercase 40-hex")
    if not SHA.fullmatch(args.head_sha):
        fail("head SHA is not exact lowercase 40-hex")
    if revision(args.git_dir, args.base_revision) != args.base_sha:
        fail("fetched base does not equal the event base SHA")
    if revision(args.git_dir, args.head_revision) != args.head_sha:
        fail("fetched head does not equal the event head SHA")
    if not args.head_ref.startswith("routine/"):
        fail("head ref is not a routine branch")
    if not args.validator.is_file() or not args.body.is_file():
        fail("trusted validator or PR body is absent")

    completed = subprocess.run(
        [
            sys.executable,
            str(args.validator),
            "--repo", args.repository,
            "--head-ref", args.head_ref,
            "--head-sha", args.head_sha,
            "--base-sha", args.base_sha,
            "--routine-policy-ref", args.base_sha,
            "--body", str(args.body),
        ],
        cwd=args.git_dir,
        check=False,
    )
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
