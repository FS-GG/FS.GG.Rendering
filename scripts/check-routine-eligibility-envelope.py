#!/usr/bin/env python3
"""Validate the trusted routine-event envelope before invoking the base validator.

`--base-revision` is the live default-branch tip resolved at run time and the sole source of the
executables and policy. `--base-sha` is the pull_request_target event's base, kept only as a monotonic
floor: when the base has moved, the event base must be an ancestor of the live base, the head must not
already be contained in it, and the PR's change is re-derived as `merge-base(live base, head)..head`
against that live base (never trusted from the event). An unmoved base keeps the exact two-dot diff.

`--previous-head` (.github#5189) is the `synchronize` event's `before`, supplied by the trusted workflow
only after it proved the push merely added commits on top of it. The envelope re-proves that ancestry
(a value that is not a strict ancestor of the head refuses) and forwards it to the validator, which may
then admit a marker naming exactly that parent provisionally. Omitting it keeps the exact-head rule.
"""

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


def is_ancestor(git_dir: Path, ancestor: str, descendant: str) -> bool:
    result = subprocess.run(
        ["git", "-C", str(git_dir), "merge-base", "--is-ancestor", ancestor, descendant],
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode not in (0, 1):
        fail(f"cannot compare {ancestor} with {descendant}: {' '.join(result.stderr.split())}")
    return result.returncode == 0


def diff_base(git_dir: Path, event_base: str, live_base: str, head: str) -> str:
    """Return the commit the PR's change is measured from; refuse a move this history cannot prove.

    Merge-base counting runs over a bounded shallow fetch, so it is best-effort: an incomplete history
    refuses (fail-closed) and never widens what is admitted.
    """
    if live_base == event_base:
        return event_base
    if not is_ancestor(git_dir, event_base, live_base):
        fail(f"event base {event_base} is not an ancestor of live base {live_base}")
    if is_ancestor(git_dir, head, live_base):
        fail(f"head {head} is already contained in live base {live_base}")
    result = subprocess.run(
        ["git", "-C", str(git_dir), "merge-base", "--all", live_base, head],
        capture_output=True,
        text=True,
        check=False,
    )
    bases = result.stdout.split() if result.returncode == 0 else []
    if len(bases) != 1 or not SHA.fullmatch(bases[0]):
        fail(f"live base {live_base} and head {head} have no single merge base ({len(bases)} found)")
    print(f"routine-eligibility: base moved {event_base} -> {live_base}; change measured from merge base {bases[0]}")
    return bases[0]


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
    parser.add_argument("--previous-head", default=None)
    args = parser.parse_args()

    if args.base_ref != args.default_branch:
        fail(f"PR target {args.base_ref!r} is not default branch {args.default_branch!r}")
    if not SHA.fullmatch(args.base_sha):
        fail("base SHA is not exact lowercase 40-hex")
    if not SHA.fullmatch(args.head_sha):
        fail("head SHA is not exact lowercase 40-hex")
    live_base = revision(args.git_dir, args.base_revision)
    if not SHA.fullmatch(live_base):
        fail("live base is not exact lowercase 40-hex")
    if revision(args.git_dir, args.head_revision) != args.head_sha:
        fail("fetched head does not equal the event head SHA")
    if not args.head_ref.startswith("routine/"):
        fail("head ref is not a routine branch")
    if not args.validator.is_file() or not args.body.is_file():
        fail("trusted validator or PR body is absent")

    previous: list[str] = []
    if args.previous_head is not None:
        if not SHA.fullmatch(args.previous_head) or args.previous_head == args.head_sha:
            fail("previous head is not an exact lowercase 40-hex commit distinct from the head")
        if not is_ancestor(args.git_dir, args.previous_head, args.head_sha):
            fail(f"previous head {args.previous_head} is not an ancestor of head {args.head_sha}")
        previous = ["--routine-previous-head", args.previous_head]

    measured_from = diff_base(args.git_dir, args.base_sha, live_base, args.head_sha)

    completed = subprocess.run(
        [
            sys.executable,
            str(args.validator),
            "--repo", args.repository,
            "--head-ref", args.head_ref,
            "--head-sha", args.head_sha,
            "--base-sha", measured_from,
            "--routine-policy-ref", live_base,
            "--body", str(args.body),
            *previous,
        ],
        cwd=args.git_dir,
        check=False,
    )
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
