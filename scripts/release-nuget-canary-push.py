#!/usr/bin/env python3
"""Canary-first, stop-at-first-refusal nuget.org push of the retained original archives.

The first roster package that needs a push is the canary: it is pushed and classified before any
other package is attempted. Packages are then pushed one at a time in the existing roster order.
The run stops at the first refusal or unresolved outcome. A push is never retried blindly. When an
outcome is uncertain (transport error, timeout or server error) or nuget.org reports 409 Conflict,
the script reads the destination flat container and compares its payload with the local archive
(`release-custody.py probe`). Only an exact payload match counts as present. nuget.org adds a
repository signature, so the comparison covers every payload entry except `.signature.p7s`; the raw
.nupkg hash never equals the original.

The receipt (`fsgg.rendering.nuget-publish-outcomes/v1`) holds closed tags only. It never holds the
key, push output or response text.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

ROOT = Path(__file__).resolve().parents[1]
SCHEMA = "fsgg.rendering.nuget-publish-outcomes/v1"
FLAT_CONTAINER = "https://api.nuget.org/v3-flatcontainer/{id_lower}/{version}/{filename}"
NUGET_SOURCE = "https://api.nuget.org/v3/index.json"
STATUS = re.compile(r"Response status code does not indicate success: (\d{3})\b")

# Closed outcome tags.
PUSHED = "pushed"
PRESENT = "already-present-verified"
REFUSED = "refused"
CONFLICT_UNVERIFIED = "conflict-unverified"
UNCERTAIN_MATCHED = "uncertain-observed:matched"
UNCERTAIN_ABSENT = "uncertain-observed:absent"
UNCERTAIN_REFUSED = "uncertain-observed:refused"
PROBE_REFUSED = "probe-refused"
EXECUTOR_REFUSED = "executor-refused"
NOT_ATTEMPTED = "not-attempted"
CONTINUES = {PUSHED, PRESENT, UNCERTAIN_MATCHED}

# release-custody.py probe exit codes: 0 payload matched, 4 absent, anything else refused/mismatch/unavailable.
MATCHED, ABSENT = 0, 4


@dataclass
class Effects:
    probe: Callable[[str], int]
    push: Callable[[str], tuple[int, str]]
    executor: Callable[[], int]
    record: Callable[[str, str, str], None]
    sleep: Callable[[float], None]
    checkpoint: Callable[[dict], None] = lambda receipt: None


def classify_push(returncode: int, output: str) -> tuple[str, int | None]:
    """Return ("ok"|"conflict"|"refused"|"uncertain", httpStatus)."""
    if returncode == 0:
        return "ok", None
    codes = STATUS.findall(output)
    if len(codes) != 1:
        return "uncertain", None
    status = int(codes[0])
    if status == 409:
        return "conflict", status
    if status >= 500 or status in (408, 429):
        return "uncertain", status
    return "refused", status


def observe(effects: Effects, package: str, attempts: int, delay: float) -> int:
    """Observe the destination without pushing. Polls only while absent (propagation)."""
    result = ABSENT
    for attempt in range(attempts):
        result = effects.probe(package)
        if result != ABSENT:
            return result
        if attempt + 1 < attempts:
            effects.sleep(delay)
    return result


def publish(rows: list[tuple[str, str]], effects: Effects, attempts: int, delay: float) -> dict:
    outcomes = [{"packageId": package, "outcome": NOT_ATTEMPTED, "httpStatus": None} for package, _ in rows]
    canary = None
    stopped = None

    def receipt() -> dict:
        return {
            "schema": SCHEMA,
            "feed": "nuget.org",
            "canaryPackageId": canary,
            "stoppedAt": stopped,
            "completed": stopped is None and all(o["outcome"] in CONTINUES for o in outcomes),
            "packages": outcomes,
        }

    for index, (package, file) in enumerate(rows):
        effects.checkpoint(receipt())
        row = outcomes[index]
        effects.record(package, "probe", "requested")
        before = effects.probe(package)
        if before == MATCHED:
            row["outcome"] = PRESENT
            effects.record(package, "compare", "matched")
            continue
        if before != ABSENT:
            row["outcome"] = PROBE_REFUSED
            effects.record(package, "probe", "refused")
            stopped = package
            break
        effects.record(package, "probe", "absent")
        if effects.executor() != 0:
            row["outcome"] = EXECUTOR_REFUSED
            effects.record(package, "push", "refused")
            stopped = package
            break
        if canary is None:
            canary = package
        effects.record(package, "push", "requested")
        returncode, output = effects.push(file)
        kind, status = classify_push(returncode, output)
        row["httpStatus"] = status
        if kind == "ok":
            row["outcome"] = PUSHED
            effects.record(package, "push", "acknowledged")
            continue
        if kind == "refused":
            row["outcome"] = REFUSED
            effects.record(package, "push", "refused")
            stopped = package
            break
        effects.record(package, "push", "failed")
        seen = observe(effects, package, attempts, delay)
        if kind == "conflict":
            row["outcome"] = PRESENT if seen == MATCHED else CONFLICT_UNVERIFIED
        else:
            row["outcome"] = {MATCHED: UNCERTAIN_MATCHED, ABSENT: UNCERTAIN_ABSENT}.get(seen, UNCERTAIN_REFUSED)
        if row["outcome"] in CONTINUES:
            effects.record(package, "compare", "matched")
            continue
        effects.record(package, "compare", "failed")
        stopped = package
        break
    final = receipt()
    effects.checkpoint(final)
    return final


def write_receipt(path: Path, receipt: dict, key: str) -> None:
    text = json.dumps(receipt, indent=2, sort_keys=True) + "\n"
    if key and key in text:
        raise SystemExit("nuget publish receipt refused: credential reflection")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def real_effects(args: argparse.Namespace, key: str) -> Effects:
    custody = [sys.executable, str(ROOT / "scripts/release-custody.py"), "probe", "--plan", str(args.plan),
               "--archives", str(args.archives), "--manifest", str(args.manifest), "--source-sha", args.source_sha,
               "--url-template", FLAT_CONTAINER]

    def probe(package: str) -> int:
        return subprocess.run([*custody, "--package-id", package], check=False).returncode

    def push(file: str) -> tuple[int, str]:
        try:
            result = subprocess.run(
                ["dotnet", "nuget", "push", str(args.archives / file), "--source", NUGET_SOURCE,
                 "--api-key", key, "--timeout", str(args.push_timeout)],
                check=False, capture_output=True, text=True, timeout=args.push_timeout + 60)
        except subprocess.TimeoutExpired:
            return 124, ""
        output = (result.stdout + result.stderr).replace(key, "***")
        print(output, end="" if output.endswith("\n") else "\n")
        return result.returncode, output

    def executor() -> int:
        return subprocess.run([sys.executable, str(ROOT / "scripts/release-source-guard.py"), "--executor-only"],
                              check=False, stdout=subprocess.DEVNULL).returncode

    def record(package: str, stage: str, outcome: str) -> None:
        subprocess.run([sys.executable, str(ROOT / "scripts/release-source-guard.py"), "--record-event",
                        "--event-feed", "nuget", "--event-package", package, "--event-stage", stage,
                        "--event-result", outcome], check=True)

    def checkpoint(receipt: dict) -> None:
        write_receipt(args.receipt, receipt, key)

    return Effects(probe=probe, push=push, executor=executor, record=record, sleep=time.sleep, checkpoint=checkpoint)


def read_roster(path: Path) -> list[tuple[str, str]]:
    rows = [tuple(line.split("\t")) for line in path.read_text(encoding="utf-8").splitlines() if line]
    if len(rows) != 19 or any(len(row) != 2 for row in rows) or len({row[0].lower() for row in rows}) != 19:
        raise SystemExit("nuget publish refused: roster must hold exactly 19 unique id/file rows")
    return [(row[0], row[1]) for row in rows]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--archives", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--roster", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    parser.add_argument("--observe-attempts", type=int, default=30)
    parser.add_argument("--observe-delay", type=float, default=10.0)
    parser.add_argument("--push-timeout", type=int, default=300)
    args = parser.parse_args()
    key = os.environ.get("NUGET_API_KEY", "")
    if not key.strip():
        raise SystemExit("nuget publish refused: credential unavailable")
    rows = read_roster(args.roster)
    receipt = publish(rows, real_effects(args, key), args.observe_attempts, args.observe_delay)
    tally = {}
    for row in receipt["packages"]:
        tally[row["outcome"]] = tally.get(row["outcome"], 0) + 1
    print("nuget.org publish outcomes: " + ", ".join(f"{k}={v}" for k, v in sorted(tally.items()))
          + f"; canary={receipt['canaryPackageId']}; stoppedAt={receipt['stoppedAt']}")
    return 0 if receipt["completed"] else 3


if __name__ == "__main__":
    raise SystemExit(main())
