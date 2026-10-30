#!/usr/bin/env python3
"""Offline controls for the canary-first nuget.org publisher. No network, credential or push."""
from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("canary", ROOT / "scripts/release-nuget-canary-push.py")
canary = importlib.util.module_from_spec(spec)
sys.modules["canary"] = canary
spec.loader.exec_module(canary)

ROWS = [(f"FS.GG.UI.Package{n}", f"fs.gg.ui.package{n}.1.0.0.nupkg") for n in range(1, 20)]
FILE_TO_ID = {file: package for package, file in ROWS}
KEY = "public-canary-fixture-key"
CONFLICT = "error: Response status code does not indicate success: 409 (A package with ID 'x' and version 'y' already exists and cannot be modified)."


class Fake:
    """Destination model: probe answers come from `present`/`probes`; pushes are scripted per id."""

    def __init__(self, pushes=None, present=(), probes=None, executor=0):
        self.pushes = pushes or {}
        self.present = set(present)
        self.probes = {k: list(v) for k, v in (probes or {}).items()}
        self.executor_rc = executor
        self.log = []
        self.events = []
        self.sleeps = 0
        self.checkpoints = []

    def probe(self, package):
        self.log.append(("probe", package))
        if self.probes.get(package):
            return self.probes[package].pop(0)
        return canary.MATCHED if package in self.present else canary.ABSENT

    def push(self, file):
        package = FILE_TO_ID[file]
        self.log.append(("push", package))
        rc, output = self.pushes.get(package, (0, "Your package was pushed."))
        if rc == 0:
            self.present.add(package)
        return rc, output

    def executor(self):
        return self.executor_rc

    def record(self, package, stage, outcome):
        self.events.append((package, stage, outcome))

    def sleep(self, _):
        self.sleeps += 1

    def effects(self):
        return canary.Effects(self.probe, self.push, self.executor, self.record, self.sleep,
                              lambda receipt: self.checkpoints.append(json.dumps(receipt)))

    def pushed(self):
        return [package for kind, package in self.log if kind == "push"]


def run(fake, attempts=3):
    return canary.publish(ROWS, fake.effects(), attempts, 0)


class CanaryPublishTests(unittest.TestCase):
    def outcomes(self, receipt):
        return [row["outcome"] for row in receipt["packages"]]

    def test_all_absent_pushes_canary_first_then_roster_order(self):
        fake = Fake()
        receipt = run(fake)
        self.assertTrue(receipt["completed"])
        self.assertEqual(ROWS[0][0], receipt["canaryPackageId"])
        self.assertEqual([package for package, _ in ROWS], fake.pushed())
        self.assertEqual([canary.PUSHED] * 19, self.outcomes(receipt))
        # Each push is preceded by its own destination probe and executor recheck.
        self.assertEqual(("probe", ROWS[0][0]), fake.log[0])
        self.assertEqual(("push", ROWS[0][0]), fake.log[1])

    def test_canary_refusal_stops_before_any_other_push(self):
        for output in ["error: Response status code does not indicate success: 403 (Forbidden).",
                       "error: Response status code does not indicate success: 401 (Unauthorized).",
                       "error: Response status code does not indicate success: 400 (Bad Request)."]:
            fake = Fake(pushes={ROWS[0][0]: (1, output)})
            receipt = run(fake)
            self.assertEqual([ROWS[0][0]], fake.pushed())
            self.assertFalse(receipt["completed"])
            self.assertEqual(ROWS[0][0], receipt["stoppedAt"])
            self.assertEqual(canary.REFUSED, receipt["packages"][0]["outcome"])
            self.assertEqual([canary.NOT_ATTEMPTED] * 18, self.outcomes(receipt)[1:])
            self.assertEqual(1, sum(1 for kind, _ in fake.log if kind == "probe"))

    def test_later_refusal_stops_at_first_refusal(self):
        fake = Fake(pushes={ROWS[3][0]: (1, "error: Response status code does not indicate success: 403 (Forbidden).")})
        receipt = run(fake)
        self.assertEqual([package for package, _ in ROWS[:4]], fake.pushed())
        self.assertEqual([canary.PUSHED] * 3 + [canary.REFUSED] + [canary.NOT_ATTEMPTED] * 15, self.outcomes(receipt))
        self.assertEqual(403, receipt["packages"][3]["httpStatus"])

    def test_already_present_before_push_is_verified_not_pushed(self):
        fake = Fake(present={ROWS[0][0], ROWS[1][0]})
        receipt = run(fake)
        self.assertTrue(receipt["completed"])
        self.assertNotIn(ROWS[0][0], fake.pushed())
        self.assertEqual(ROWS[2][0], receipt["canaryPackageId"])
        self.assertEqual([canary.PRESENT] * 2 + [canary.PUSHED] * 17, self.outcomes(receipt))

    def test_conflict_with_matching_payload_is_already_present(self):
        # 409, then the destination payload matches the local archive.
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)}, probes={ROWS[0][0]: [canary.ABSENT, canary.MATCHED]})
        receipt = run(fake)
        self.assertTrue(receipt["completed"])
        self.assertEqual(canary.PRESENT, receipt["packages"][0]["outcome"])
        self.assertEqual(409, receipt["packages"][0]["httpStatus"])
        self.assertEqual(1, fake.pushed().count(ROWS[0][0]))

    def test_conflict_with_mismatching_payload_fails(self):
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)}, probes={ROWS[0][0]: [canary.ABSENT, 1]})
        receipt = run(fake)
        self.assertFalse(receipt["completed"])
        self.assertEqual(canary.CONFLICT_UNVERIFIED, receipt["packages"][0]["outcome"])
        self.assertEqual([ROWS[0][0]], fake.pushed())

    def test_conflict_never_visible_fails(self):
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)})
        receipt = run(fake, attempts=4)
        self.assertEqual(canary.CONFLICT_UNVERIFIED, receipt["packages"][0]["outcome"])
        self.assertEqual([ROWS[0][0]], fake.pushed())
        self.assertEqual(3, fake.sleeps)

    def test_uncertain_push_observes_destination_without_retry(self):
        for rc, output in [(124, ""), (1, "error: The HTTP request timed out."),
                           (1, "error: Response status code does not indicate success: 503 (Service Unavailable).")]:
            # Accepted despite the transport error: observed match, continue.
            fake = Fake(pushes={ROWS[0][0]: (rc, output)}, probes={ROWS[0][0]: [canary.ABSENT, canary.ABSENT, canary.MATCHED]})
            receipt = run(fake)
            self.assertTrue(receipt["completed"])
            self.assertEqual(canary.UNCERTAIN_MATCHED, receipt["packages"][0]["outcome"])
            self.assertEqual(1, fake.pushed().count(ROWS[0][0]))
            # Not visible: stop, never re-push.
            fake = Fake(pushes={ROWS[0][0]: (rc, output)})
            receipt = run(fake, attempts=2)
            self.assertFalse(receipt["completed"])
            self.assertEqual(canary.UNCERTAIN_ABSENT, receipt["packages"][0]["outcome"])
            self.assertEqual([ROWS[0][0]], fake.pushed())
            # Visible but different payload (or unreadable): stop.
            fake = Fake(pushes={ROWS[0][0]: (rc, output)}, probes={ROWS[0][0]: [canary.ABSENT, 1]})
            receipt = run(fake)
            self.assertEqual(canary.UNCERTAIN_REFUSED, receipt["packages"][0]["outcome"])
            self.assertEqual([ROWS[0][0]], fake.pushed())

    def test_probe_or_executor_refusal_stops_before_push(self):
        fake = Fake(probes={ROWS[0][0]: [1]})
        receipt = run(fake)
        self.assertEqual([], fake.pushed())
        self.assertEqual(canary.PROBE_REFUSED, receipt["packages"][0]["outcome"])
        fake = Fake(executor=1)
        receipt = run(fake)
        self.assertEqual([], fake.pushed())
        self.assertEqual(canary.EXECUTOR_REFUSED, receipt["packages"][0]["outcome"])

    def test_classification_is_closed(self):
        self.assertEqual(("ok", None), canary.classify_push(0, "anything"))
        self.assertEqual(("conflict", 409), canary.classify_push(1, CONFLICT))
        self.assertEqual(("refused", 403), canary.classify_push(1, "Response status code does not indicate success: 403 (x)"))
        self.assertEqual(("uncertain", 502), canary.classify_push(1, "Response status code does not indicate success: 502 (x)"))
        self.assertEqual(("uncertain", None), canary.classify_push(1, "socket closed"))
        two = "Response status code does not indicate success: 403 (x)\nResponse status code does not indicate success: 409 (y)"
        self.assertEqual(("uncertain", None), canary.classify_push(1, two))

    def test_receipt_is_closed_tags_and_never_holds_key_or_output(self):
        fake = Fake(pushes={ROWS[2][0]: (1, "error: Response status code does not indicate success: 403 (" + KEY + " Forbidden).")})
        receipt = run(fake)
        allowed = {canary.PUSHED, canary.PRESENT, canary.REFUSED, canary.CONFLICT_UNVERIFIED, canary.UNCERTAIN_MATCHED,
                   canary.UNCERTAIN_ABSENT, canary.UNCERTAIN_REFUSED, canary.PROBE_REFUSED, canary.EXECUTOR_REFUSED,
                   canary.NOT_ATTEMPTED}
        self.assertTrue(set(self.outcomes(receipt)) <= allowed)
        self.assertEqual({"schema", "feed", "canaryPackageId", "stoppedAt", "completed", "packages"}, set(receipt))
        text = json.dumps(receipt)
        self.assertNotIn(KEY, text)
        self.assertNotIn("Forbidden", text)
        self.assertTrue(fake.checkpoints and all(KEY not in c for c in fake.checkpoints))
        # Partial state is checkpointed before each package, so a crash still leaves a receipt.
        self.assertGreaterEqual(len(fake.checkpoints), 4)
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "receipt.json"
            canary.write_receipt(path, receipt, KEY)
            self.assertNotIn(KEY, path.read_text())
            with self.assertRaises(SystemExit):
                canary.write_receipt(path, {**receipt, "stoppedAt": KEY}, KEY)

    def test_events_use_closed_publisher_vocabulary(self):
        fake = Fake(pushes={ROWS[1][0]: (1, CONFLICT)}, probes={ROWS[1][0]: [canary.ABSENT, canary.MATCHED]})
        run(fake)
        for _, stage, outcome in fake.events:
            self.assertIn(stage, ("probe", "compare", "push", "readback", "interrupted"))
            self.assertIn(outcome, ("requested", "acknowledged", "matched", "failed", "absent", "refused"))

    def test_roster_shape_is_hard(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "roster.tsv"
            path.write_text("".join(f"{p}\t{f}\n" for p, f in ROWS))
            self.assertEqual(ROWS, canary.read_roster(path))
            for bad in [ROWS[:18], ROWS[:18] + [ROWS[0]]]:
                path.write_text("".join(f"{p}\t{f}\n" for p, f in bad))
                with self.assertRaises(SystemExit):
                    canary.read_roster(path)


if __name__ == "__main__":
    unittest.main(verbosity=1)
