#!/usr/bin/env python3
"""Canary-first, dependency-ordered, stop-at-first-refusal nuget.org push of the retained original archives.

Order: a topological order over the FS.GG.UI.* `<dependencies>` read from the actual custody
archives' nuspecs. Each dependency is pushed before its dependent. Ties are broken by ordinal id, so
the first package is the lowest-id leaf (no FS.GG.UI.* dependency); that package is the canary. The
template (FS.GG.UI.Template) and then the BOM (FS.GG.UI) go last. The run refuses if the graph has a
cycle or an unknown FS.GG.UI.* dependency, or if any other package depends on the template or BOM.

The run stops at the first refusal or unresolved outcome. A push is never retried blindly. When an
outcome is uncertain (transport error, timeout, 408/429/5xx) or nuget.org reports 409 Conflict, the
script observes the destination flat container with `release-custody.py probe` for a bounded window.
Only an exact payload match counts as present. nuget.org adds a repository signature, so the
comparison covers every payload entry except `.signature.p7s`; the raw .nupkg hash never equals the
original.

ADR-0012 §4 keeps GitHub Packages first. This step runs only after the org feed has accepted and
read back all nineteen archives, so an incomplete run here leaves the org feed ahead of nuget.org
(`feedSplit: org-feed-ahead`). Reconcile by rerunning this step with the same custody archives;
never repack.

The receipt (`fsgg.rendering.nuget-publish-outcomes/v1`) holds closed tags only. It never holds the
key, push output or response text. Each package is marked `push-requested` before its push, and the
final receipt is written even when the run is interrupted.
"""
from __future__ import annotations

import argparse
import importlib.util
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
# Same bounded window as the post-push flat-container readback (180 x 10 s); observation never pushes.
OBSERVE_ATTEMPTS, OBSERVE_DELAY = 180, 10.0
BOM = "FS.GG.UI"
TEMPLATE = "FS.GG.UI.Template"
LAST = (TEMPLATE, BOM)

# release-custody.py probe exit codes.
MATCHED, ABSENT, MISMATCH, UNREADABLE = 0, 4, 5, 6
DESTINATION = {MATCHED: "matched", ABSENT: "absent", MISMATCH: "mismatch", UNREADABLE: "unreadable"}

# Closed outcome tags.
NOT_ATTEMPTED = "not-attempted"
PUSH_REQUESTED = "push-requested"
PUSHED = "pushed"
PRESENT = "already-present-verified"
PRESENT_MISMATCH = "present-mismatch"
DESTINATION_UNREADABLE = "destination-unreadable"
PROBE_REFUSED = "probe-refused"
EXECUTOR_REFUSED = "executor-refused"
REFUSED = "refused"
CONFLICT = "conflict-observed:"      # + matched-is-PRESENT | absent | mismatch | unreadable | refused
UNCERTAIN = "uncertain-observed:"    # + matched | absent | mismatch | unreadable | refused
CONTINUES = {PUSHED, PRESENT, UNCERTAIN + "matched"}
OUTCOMES = ({NOT_ATTEMPTED, PUSH_REQUESTED, PUSHED, PRESENT, PRESENT_MISMATCH, DESTINATION_UNREADABLE,
             PROBE_REFUSED, EXECUTOR_REFUSED, REFUSED}
            | {CONFLICT + d for d in ("absent", "mismatch", "unreadable", "refused")}
            | {UNCERTAIN + d for d in ("matched", "absent", "mismatch", "unreadable", "refused")})
INTERRUPTIONS = {None, "event-record-failed", "exception"}
FEED_SPLITS = {"none", "org-feed-ahead"}


class OrderRefused(Exception):
    pass


class RecordFailed(Exception):
    pass


@dataclass
class Effects:
    probe: Callable[[str], int]
    push: Callable[[str], tuple[int, str]]
    executor: Callable[[], int]
    record: Callable[[str, str, str], None]
    sleep: Callable[[float], None]
    checkpoint: Callable[[dict], None] = lambda receipt: None


def is_product(package: str) -> bool:
    lower = package.lower()
    return lower == BOM.lower() or lower.startswith(BOM.lower() + ".")


def dependency_order(rows: list[tuple[str, str]], dependencies: dict[str, list[str]]) -> list[tuple[str, str]]:
    """Topological push order over FS.GG.UI.* edges; lowest-id leaf first; template then BOM last."""
    by_lower = {package.lower(): package for package, _ in rows}
    if len(by_lower) != len(rows) or any(name.lower() not in by_lower for name in LAST):
        raise OrderRefused("roster must be unique and contain the template and BOM")
    edges: dict[str, set[str]] = {}
    for package, _ in rows:
        needed = set()
        for dependency in dependencies.get(package, []):
            if not is_product(dependency):
                continue
            if dependency.lower() not in by_lower:
                raise OrderRefused(f"unknown FS.GG.UI dependency {dependency} of {package}")
            target = by_lower[dependency.lower()]
            if target == package:
                raise OrderRefused(f"self dependency {package}")
            needed.add(target)
        edges[package] = needed
    for package, needed in edges.items():
        if package != BOM and BOM in needed:
            raise OrderRefused(f"{package} depends on the BOM, which must be pushed last")
        if package not in LAST and TEMPLATE in needed:
            raise OrderRefused(f"{package} depends on the template, which must be pushed last")
    remaining = {package: set(needed) for package, needed in edges.items() if package not in LAST}
    order: list[str] = []
    while remaining:
        ready = sorted(package for package, needed in remaining.items() if not needed)
        if not ready:
            raise OrderRefused("FS.GG.UI dependency cycle")
        chosen = ready[0]
        order.append(chosen)
        del remaining[chosen]
        for needed in remaining.values():
            needed.discard(chosen)
    order.extend(LAST)
    pushed: set[str] = set()
    for package in order:
        if not edges[package] <= pushed:
            raise OrderRefused(f"{package} would precede a dependency")
        pushed.add(package)
    files = dict(rows)
    return [(package, files[package]) for package in order]


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


def observe(effects: Effects, package: str, attempts: int, delay: float) -> str:
    """Read the destination without pushing; poll only while absent or unreadable."""
    result = ABSENT
    for attempt in range(attempts):
        result = effects.probe(package)
        if result not in (ABSENT, UNREADABLE):
            break
        if attempt + 1 < attempts:
            effects.sleep(delay)
    return DESTINATION.get(result, "refused")


def publish(rows: list[tuple[str, str]], effects: Effects, attempts: int, delay: float) -> dict:
    """Push `rows` in the given (dependency) order; rows[0] is the designated canary."""
    outcomes = [{"packageId": package, "outcome": NOT_ATTEMPTED, "httpStatus": None} for package, _ in rows]
    state = {"firstPush": None, "stopped": None, "interruption": None}

    def receipt() -> dict:
        completed = (state["stopped"] is None and state["interruption"] is None
                     and all(o["outcome"] in CONTINUES for o in outcomes))
        return {
            "schema": SCHEMA,
            "feed": "nuget.org",
            "order": [package for package, _ in rows],
            "canaryPackageId": rows[0][0] if rows else None,
            "firstPushPackageId": state["firstPush"],
            "stoppedAt": state["stopped"],
            "interruption": state["interruption"],
            "completed": completed,
            # GitHub Packages (ADR-0012 §4) completed before this step; anything short of complete
            # leaves the org feed ahead of nuget.org until a same-custody replay converges it.
            "feedSplit": "none" if completed else "org-feed-ahead",
            "packages": outcomes,
        }

    def record(package: str, stage: str, outcome: str) -> None:
        try:
            effects.record(package, stage, outcome)
        except Exception as exc:  # the receipt row is already set; never hide a completed push
            raise RecordFailed(package) from exc

    current = None
    try:
        for index, (package, file) in enumerate(rows):
            current = package
            row = outcomes[index]
            effects.checkpoint(receipt())
            record(package, "probe", "requested")
            before = effects.probe(package)
            if before == MATCHED:
                row["outcome"] = PRESENT
                record(package, "compare", "matched")
                continue
            if before != ABSENT:
                row["outcome"] = {MISMATCH: PRESENT_MISMATCH, UNREADABLE: DESTINATION_UNREADABLE}.get(before, PROBE_REFUSED)
                state["stopped"] = package
                record(package, "probe", "refused")
                break
            record(package, "probe", "absent")
            if effects.executor() != 0:
                row["outcome"] = EXECUTOR_REFUSED
                state["stopped"] = package
                record(package, "push", "refused")
                break
            record(package, "push", "requested")
            row["outcome"] = PUSH_REQUESTED
            if state["firstPush"] is None:
                state["firstPush"] = package
            effects.checkpoint(receipt())  # in-flight state is durable before the push
            returncode, output = effects.push(file)
            kind, status = classify_push(returncode, output)
            row["httpStatus"] = status
            if kind == "ok":
                row["outcome"] = PUSHED
                record(package, "push", "acknowledged")
                continue
            if kind == "refused":
                row["outcome"] = REFUSED
                state["stopped"] = package
                record(package, "push", "refused")
                break
            # The row stays push-requested (in flight) until the bounded observation resolves it.
            seen = observe(effects, package, attempts, delay)
            if kind == "conflict":
                row["outcome"] = PRESENT if seen == "matched" else CONFLICT + seen
            else:
                row["outcome"] = UNCERTAIN + seen
            if row["outcome"] in CONTINUES:
                record(package, "push", "failed")
                record(package, "compare", "matched")
                continue
            state["stopped"] = package
            record(package, "push", "failed")
            record(package, "compare", "failed")
            break
    except RecordFailed:
        state["interruption"] = "event-record-failed"
        state["stopped"] = state["stopped"] or current
    except BaseException:
        state["interruption"] = "exception"
        state["stopped"] = state["stopped"] or current
        raise
    finally:
        final = receipt()
        effects.checkpoint(final)
    return final


def write_receipt(path: Path, receipt: dict, key: str) -> None:
    if receipt.get("schema") != SCHEMA or receipt.get("interruption") not in INTERRUPTIONS \
            or receipt.get("feedSplit") not in FEED_SPLITS \
            or any(row["outcome"] not in OUTCOMES for row in receipt.get("packages", [])):
        raise SystemExit("nuget publish receipt refused: open tag")
    text = json.dumps(receipt, indent=2, sort_keys=True) + "\n"
    if key and key in text:
        raise SystemExit("nuget publish receipt refused: credential reflection")
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + ".tmp")
    temp.write_text(text, encoding="utf-8")
    temp.replace(path)


def load_custody():
    spec = importlib.util.spec_from_file_location("release_custody", ROOT / "scripts/release-custody.py")
    module = importlib.util.module_from_spec(spec)
    sys.modules.setdefault("release_custody", module)
    spec.loader.exec_module(module)
    return module


def archive_dependencies(archives: Path, rows: list[tuple[str, str]]) -> dict[str, list[str]]:
    custody = load_custody()
    result = {}
    for package, file in rows:
        meta, _ = custody.nuspec(archives / file)
        if meta["id"] != package:
            raise OrderRefused(f"archive {file} declares {meta['id']}, roster says {package}")
        result[package] = [d["id"] for d in meta["dependencies"]]
    return result


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
    parser.add_argument("--observe-attempts", type=int, default=OBSERVE_ATTEMPTS)
    parser.add_argument("--observe-delay", type=float, default=OBSERVE_DELAY)
    parser.add_argument("--push-timeout", type=int, default=300)
    args = parser.parse_args()
    key = os.environ.get("NUGET_API_KEY", "")
    if not key.strip():
        raise SystemExit("nuget publish refused: credential unavailable")
    rows = read_roster(args.roster)
    try:
        ordered = dependency_order(rows, archive_dependencies(args.archives, rows))
    except OrderRefused as exc:
        raise SystemExit(f"nuget publish refused: {exc}") from exc
    print("nuget.org dependency order: " + " -> ".join(package for package, _ in ordered))
    receipt = publish(ordered, real_effects(args, key), args.observe_attempts, args.observe_delay)
    tally: dict[str, int] = {}
    for row in receipt["packages"]:
        tally[row["outcome"]] = tally.get(row["outcome"], 0) + 1
    print("nuget.org publish outcomes: " + ", ".join(f"{k}={v}" for k, v in sorted(tally.items()))
          + f"; canary={receipt['canaryPackageId']}; stoppedAt={receipt['stoppedAt']}"
          + f"; interruption={receipt['interruption']}; feedSplit={receipt['feedSplit']}")
    return 0 if receipt["completed"] else 3


if __name__ == "__main__":
    raise SystemExit(main())
