#!/usr/bin/env python3
"""Offline controls for the canary-first nuget.org publisher. No network, credential or push."""
from __future__ import annotations

import importlib.util
import json
import re
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("canary", ROOT / "scripts/release-nuget-canary-push.py")
canary = importlib.util.module_from_spec(spec)
sys.modules["canary"] = canary
spec.loader.exec_module(canary)

ROWS = [(f"FS.GG.UI.Package{n:02d}", f"fs.gg.ui.package{n:02d}.1.0.0.nupkg") for n in range(1, 20)]
FILE_TO_ID = {file: package for package, file in ROWS}
KEY = "public-canary-fixture-key"
CONFLICT = "error: Response status code does not indicate success: 409 (A package with ID 'x' and version 'y' already exists and cannot be modified)."
FORBIDDEN = "error: Response status code does not indicate success: 403 (Forbidden)."

# Release-shaped roster (plan ids) with an FS.GG.UI.* graph shaped like the real one: the BOM pins
# every library exactly, Controls needs KeyboardInput/Layout/Scene, and leaves include Build/Diagnostics/Scene.
PLAN_IDS = ["FS.GG.UI.Build", "FS.GG.UI.Canvas", "FS.GG.UI.Controls", "FS.GG.UI.Controls.Elmish",
            "FS.GG.UI.DesignSystem", "FS.GG.UI.Diagnostics", "FS.GG.UI.Elmish", "FS.GG.UI.KeyboardInput",
            "FS.GG.UI.Layout", "FS.GG.UI.Scene", "FS.GG.UI.Scene.SvgBrowser", "FS.GG.UI.SkiaViewer",
            "FS.GG.UI.Symbology", "FS.GG.UI.Symbology.Render", "FS.GG.UI.Testing", "FS.GG.UI.Themes.AntDesign",
            "FS.GG.UI.Themes.Default", "FS.GG.UI", "FS.GG.UI.Template"]
LIBRARIES = [p for p in PLAN_IDS if p not in ("FS.GG.UI", "FS.GG.UI.Template")]
GRAPH = {
    "FS.GG.UI.Canvas": ["FS.GG.UI.Scene"],
    "FS.GG.UI.Controls": ["FS.GG.UI.KeyboardInput", "FS.GG.UI.Layout", "FS.GG.UI.Scene", "FSharp.Core"],
    "FS.GG.UI.Controls.Elmish": ["FS.GG.UI.Controls", "FS.GG.UI.Elmish", "Elmish"],
    "FS.GG.UI.DesignSystem": ["FS.GG.UI.Controls"],
    "FS.GG.UI.Elmish": ["FS.GG.UI.Scene"],
    "FS.GG.UI.KeyboardInput": ["FS.GG.UI.Scene"],
    "FS.GG.UI.Layout": ["FS.GG.UI.Scene"],
    "FS.GG.UI.Scene.SvgBrowser": ["FS.GG.UI.Scene", "Fable.Core"],
    "FS.GG.UI.SkiaViewer": ["FS.GG.UI.Canvas", "FS.GG.UI.KeyboardInput", "SkiaSharp"],
    "FS.GG.UI.Symbology": ["FS.GG.UI.Scene"],
    "FS.GG.UI.Symbology.Render": ["FS.GG.UI.Symbology", "FS.GG.UI.Canvas"],
    "FS.GG.UI.Testing": ["FS.GG.UI.Controls", "FS.GG.UI.SkiaViewer"],
    "FS.GG.UI.Themes.AntDesign": ["FS.GG.UI.DesignSystem"],
    "FS.GG.UI.Themes.Default": ["fs.gg.ui.designsystem"],  # NuGet ids are case-insensitive
    "FS.GG.UI": LIBRARIES,
}
PLAN_ROWS = sorted((p, f"{p.lower()}.0.32.4.nupkg") for p in PLAN_IDS)  # roster.tsv is id-sorted


class Fake:
    """Destination model: probe answers come from `present`/`probes`; pushes are scripted per id."""

    def __init__(self, pushes=None, present=(), probes=None, executor=0, record_fails=None):
        self.pushes = pushes or {}
        self.present = set(present)
        self.probes = {k: list(v) for k, v in (probes or {}).items()}
        self.executor_rc = executor
        self.record_fails = record_fails
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
        # The in-flight tag is durable before the push itself.
        last = self.checkpoints[-1]["packages"]
        assert next(r for r in last if r["packageId"] == package)["outcome"] == canary.PUSH_REQUESTED
        rc, output = self.pushes.get(package, (0, "Your package was pushed."))
        if rc == 0:
            self.present.add(package)
        return rc, output

    def executor(self):
        return self.executor_rc

    def record(self, package, stage, outcome):
        if self.record_fails == (package, stage, outcome):
            raise RuntimeError("event log unavailable")
        self.events.append((package, stage, outcome))

    def sleep(self, _):
        self.sleeps += 1

    def effects(self):
        return canary.Effects(self.probe, self.push, self.executor, self.record, self.sleep,
                              lambda receipt: self.checkpoints.append(json.loads(json.dumps(receipt))))

    def pushed(self):
        return [package for kind, package in self.log if kind == "push"]


def run(fake, attempts=3, rows=ROWS):
    return canary.publish(rows, fake.effects(), attempts, 0)


class CanaryPublishTests(unittest.TestCase):
    def outcomes(self, receipt):
        return [row["outcome"] for row in receipt["packages"]]

    def test_all_absent_pushes_canary_first_then_given_order(self):
        fake = Fake()
        receipt = run(fake)
        self.assertTrue(receipt["completed"])
        self.assertEqual("none", receipt["feedSplit"])
        self.assertEqual(ROWS[0][0], receipt["canaryPackageId"])
        self.assertEqual(ROWS[0][0], receipt["firstPushPackageId"])
        self.assertEqual([package for package, _ in ROWS], fake.pushed())
        self.assertEqual([canary.PUSHED] * 19, self.outcomes(receipt))
        self.assertEqual(("probe", ROWS[0][0]), fake.log[0])
        self.assertEqual(("push", ROWS[0][0]), fake.log[1])

    def test_canary_refusal_stops_before_any_other_push(self):
        for output in [FORBIDDEN, "error: Response status code does not indicate success: 401 (Unauthorized).",
                       "error: Response status code does not indicate success: 400 (Bad Request)."]:
            fake = Fake(pushes={ROWS[0][0]: (1, output)})
            receipt = run(fake)
            self.assertEqual([ROWS[0][0]], fake.pushed())
            self.assertFalse(receipt["completed"])
            self.assertEqual("org-feed-ahead", receipt["feedSplit"])
            self.assertEqual(ROWS[0][0], receipt["stoppedAt"])
            self.assertEqual(canary.REFUSED, receipt["packages"][0]["outcome"])
            self.assertEqual([canary.NOT_ATTEMPTED] * 18, self.outcomes(receipt)[1:])
            self.assertEqual(1, sum(1 for kind, _ in fake.log if kind == "probe"))

    def test_later_refusal_stops_at_first_refusal(self):
        fake = Fake(pushes={ROWS[3][0]: (1, FORBIDDEN)})
        receipt = run(fake)
        self.assertEqual([package for package, _ in ROWS[:4]], fake.pushed())
        self.assertEqual([canary.PUSHED] * 3 + [canary.REFUSED] + [canary.NOT_ATTEMPTED] * 15, self.outcomes(receipt))
        self.assertEqual(403, receipt["packages"][3]["httpStatus"])
        self.assertEqual("org-feed-ahead", receipt["feedSplit"])

    def test_already_present_before_push_is_verified_not_pushed(self):
        fake = Fake(present={ROWS[0][0], ROWS[1][0]})
        receipt = run(fake)
        self.assertTrue(receipt["completed"])
        self.assertNotIn(ROWS[0][0], fake.pushed())
        self.assertEqual(ROWS[0][0], receipt["canaryPackageId"])
        self.assertEqual(ROWS[2][0], receipt["firstPushPackageId"])
        self.assertEqual([canary.PRESENT] * 2 + [canary.PUSHED] * 17, self.outcomes(receipt))

    def test_conflict_with_matching_payload_is_already_present(self):
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)}, probes={ROWS[0][0]: [canary.ABSENT, canary.ABSENT, canary.MATCHED]})
        receipt = run(fake)
        self.assertTrue(receipt["completed"])
        self.assertEqual(canary.PRESENT, receipt["packages"][0]["outcome"])
        self.assertEqual(409, receipt["packages"][0]["httpStatus"])
        self.assertEqual(1, fake.pushed().count(ROWS[0][0]))

    def test_conflict_with_mismatching_payload_fails(self):
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)}, probes={ROWS[0][0]: [canary.ABSENT, canary.MISMATCH]})
        receipt = run(fake)
        self.assertFalse(receipt["completed"])
        self.assertEqual(canary.CONFLICT + "mismatch", receipt["packages"][0]["outcome"])
        self.assertEqual([ROWS[0][0]], fake.pushed())

    def test_conflict_never_visible_or_unreadable_fails(self):
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)})
        receipt = run(fake, attempts=4)
        self.assertEqual(canary.CONFLICT + "absent", receipt["packages"][0]["outcome"])
        self.assertEqual([ROWS[0][0]], fake.pushed())
        self.assertEqual(3, fake.sleeps)
        fake = Fake(pushes={ROWS[0][0]: (1, CONFLICT)}, probes={ROWS[0][0]: [canary.ABSENT] + [canary.UNREADABLE] * 3})
        receipt = run(fake)
        self.assertEqual(canary.CONFLICT + "unreadable", receipt["packages"][0]["outcome"])

    def test_uncertain_push_observes_destination_without_retry(self):
        for rc, output in [(124, ""), (1, "error: The HTTP request timed out."),
                           (1, "error: Response status code does not indicate success: 503 (Service Unavailable)."),
                           (1, "error: Response status code does not indicate success: 408 (Request Timeout)."),
                           (1, "error: Response status code does not indicate success: 429 (Too Many Requests).")]:
            for observed, expected, completed in [([canary.ABSENT, canary.UNREADABLE, canary.MATCHED], "matched", True),
                                                  ([canary.ABSENT], "absent", False),
                                                  ([canary.ABSENT, canary.MISMATCH], "mismatch", False),
                                                  ([canary.ABSENT] + [canary.UNREADABLE] * 3, "unreadable", False),
                                                  ([canary.ABSENT, 1], "refused", False)]:
                fake = Fake(pushes={ROWS[0][0]: (rc, output)}, probes={ROWS[0][0]: observed})
                receipt = run(fake, attempts=2 if expected == "absent" else 3)
                self.assertEqual(canary.UNCERTAIN + expected, receipt["packages"][0]["outcome"], (output, expected))
                self.assertEqual(completed, receipt["completed"])
                self.assertEqual(1, fake.pushed().count(ROWS[0][0]))  # never re-pushed

    def test_destination_states_before_push_are_distinct_and_stop(self):
        for code, tag in [(canary.MISMATCH, canary.PRESENT_MISMATCH), (canary.UNREADABLE, canary.DESTINATION_UNREADABLE),
                          (1, canary.PROBE_REFUSED)]:
            fake = Fake(probes={ROWS[0][0]: [code]})
            receipt = run(fake)
            self.assertEqual([], fake.pushed())
            self.assertEqual(tag, receipt["packages"][0]["outcome"])
            self.assertEqual(ROWS[0][0], receipt["stoppedAt"])
        fake = Fake(executor=1)
        receipt = run(fake)
        self.assertEqual([], fake.pushed())
        self.assertEqual(canary.EXECUTOR_REFUSED, receipt["packages"][0]["outcome"])

    def test_classification_is_closed(self):
        self.assertEqual(("ok", None), canary.classify_push(0, "anything"))
        self.assertEqual(("conflict", 409), canary.classify_push(1, CONFLICT))
        self.assertEqual(("refused", 403), canary.classify_push(1, "Response status code does not indicate success: 403 (x)"))
        self.assertEqual(("uncertain", 502), canary.classify_push(1, "Response status code does not indicate success: 502 (x)"))
        self.assertEqual(("uncertain", 408), canary.classify_push(1, "Response status code does not indicate success: 408 (Request Timeout)"))
        self.assertEqual(("uncertain", 429), canary.classify_push(1, "Response status code does not indicate success: 429 (Too Many Requests)"))
        self.assertEqual(("refused", 400), canary.classify_push(1, "Response status code does not indicate success: 400 (Bad Request)"))
        self.assertEqual(("uncertain", None), canary.classify_push(1, "socket closed"))
        two = "Response status code does not indicate success: 403 (x)\nResponse status code does not indicate success: 409 (y)"
        self.assertEqual(("uncertain", None), canary.classify_push(1, two))

    def test_receipt_is_closed_tags_and_never_holds_key_or_output(self):
        fake = Fake(pushes={ROWS[2][0]: (1, "error: Response status code does not indicate success: 403 (" + KEY + " Forbidden).")})
        receipt = run(fake)
        self.assertTrue(set(self.outcomes(receipt)) <= canary.OUTCOMES)
        self.assertEqual({"schema", "feed", "order", "canaryPackageId", "firstPushPackageId", "stoppedAt",
                          "interruption", "completed", "feedSplit", "packages"}, set(receipt))
        text = json.dumps(receipt)
        self.assertNotIn(KEY, text)
        self.assertNotIn("Forbidden", text)
        self.assertTrue(fake.checkpoints and all(KEY not in json.dumps(c) for c in fake.checkpoints))
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "receipt.json"
            canary.write_receipt(path, receipt, KEY)
            self.assertNotIn(KEY, path.read_text())
            with self.assertRaises(SystemExit):
                canary.write_receipt(path, {**receipt, "stoppedAt": KEY}, KEY)
            open_tag = json.loads(json.dumps(receipt)); open_tag["packages"][0]["outcome"] = "free text"
            with self.assertRaises(SystemExit):
                canary.write_receipt(path, open_tag, KEY)

    def test_in_flight_tag_is_checkpointed_before_every_push(self):
        fake = Fake()  # Fake.push asserts the last checkpoint marks this package push-requested
        run(fake)
        self.assertEqual(19, sum(1 for c in fake.checkpoints
                                 for r in c["packages"] if r["outcome"] == canary.PUSH_REQUESTED))

    def test_record_failure_never_hides_a_completed_push_and_final_receipt_is_written(self):
        fake = Fake(record_fails=(ROWS[1][0], "push", "acknowledged"))
        receipt = run(fake)
        self.assertEqual("event-record-failed", receipt["interruption"])
        self.assertEqual(canary.PUSHED, receipt["packages"][1]["outcome"])  # the push happened and is recorded
        self.assertEqual(ROWS[1][0], receipt["stoppedAt"])
        self.assertFalse(receipt["completed"])
        self.assertEqual("org-feed-ahead", receipt["feedSplit"])
        self.assertEqual(receipt, fake.checkpoints[-1])
        self.assertEqual([p for p, _ in ROWS[:2]], fake.pushed())
        # A record failure before the push leaves no in-flight claim.
        fake = Fake(record_fails=(ROWS[0][0], "push", "requested"))
        receipt = run(fake)
        self.assertEqual([], fake.pushed())
        self.assertEqual(canary.NOT_ATTEMPTED, receipt["packages"][0]["outcome"])

    def test_unexpected_exception_still_writes_final_receipt(self):
        fake = Fake()
        effects = fake.effects()
        def broken_push(file):
            raise KeyboardInterrupt()
        effects.push = broken_push
        with self.assertRaises(KeyboardInterrupt):
            canary.publish(ROWS, effects, 3, 0)
        final = fake.checkpoints[-1]
        self.assertEqual("exception", final["interruption"])
        self.assertEqual(canary.PUSH_REQUESTED, final["packages"][0]["outcome"])  # in flight, unresolved
        self.assertEqual("org-feed-ahead", final["feedSplit"])

    def test_events_use_closed_publisher_vocabulary(self):
        fake = Fake(pushes={ROWS[1][0]: (1, CONFLICT)}, probes={ROWS[1][0]: [canary.ABSENT, canary.MATCHED]})
        run(fake)
        for _, stage, outcome in fake.events:
            self.assertIn(stage, ("probe", "compare", "push", "readback", "interrupted"))
            self.assertIn(outcome, ("requested", "acknowledged", "matched", "failed", "absent", "refused"))

    def test_observation_window_matches_post_push_readback(self):
        self.assertEqual((180, 10.0), (canary.OBSERVE_ATTEMPTS, canary.OBSERVE_DELAY))
        release = (ROOT / ".github/workflows/release.yml").read_text()
        readback = release.split("Replay the same original custody bytes to nuget.org", 1)[1]
        self.assertIn("for attempt in {1..180}", readback)
        self.assertIn("sleep 10", readback)

    def test_roster_shape_is_hard(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "roster.tsv"
            path.write_text("".join(f"{p}\t{f}\n" for p, f in ROWS))
            self.assertEqual(ROWS, canary.read_roster(path))
            for bad in [ROWS[:18], ROWS[:18] + [ROWS[0]]]:
                path.write_text("".join(f"{p}\t{f}\n" for p, f in bad))
                with self.assertRaises(SystemExit):
                    canary.read_roster(path)


class DependencyOrderTests(unittest.TestCase):
    def nupkg(self, folder: Path, package: str, file: str, dependencies):
        deps = "".join(f'<dependency id="{d}" version="[0.32.4]" />' for d in dependencies)
        xml = f'<package><metadata><id>{package}</id><version>0.32.4</version><dependencies><group targetFramework="net10.0">{deps}</group></dependencies></metadata></package>'
        with zipfile.ZipFile(folder / file, "w") as archive:
            archive.writestr(f"{package}.nuspec", xml)

    def ordered(self, graph=GRAPH):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for package, file in PLAN_ROWS:
                self.nupkg(root, package, file, graph.get(package, []))
            return canary.dependency_order(PLAN_ROWS, canary.archive_dependencies(root, PLAN_ROWS))

    def test_dependencies_precede_dependents_canary_is_leaf_template_and_bom_last(self):
        order = [package for package, _ in self.ordered()]
        self.assertEqual(sorted(PLAN_IDS), sorted(order))
        position = {package: index for index, package in enumerate(order)}
        for package, dependencies in GRAPH.items():
            for dependency in dependencies:
                if canary.is_product(dependency):
                    target = next(p for p in PLAN_IDS if p.lower() == dependency.lower())
                    self.assertLess(position[target], position[package], (target, package))
        canary_id = order[0]
        self.assertFalse([d for d in GRAPH.get(canary_id, []) if canary.is_product(d)], canary_id)
        self.assertEqual("FS.GG.UI.Build", canary_id)  # lowest-id leaf
        self.assertEqual(["FS.GG.UI.Template", "FS.GG.UI"], order[-2:])
        self.assertLess(position["FS.GG.UI.Scene"], position["FS.GG.UI.Controls"])
        self.assertLess(position["FS.GG.UI.KeyboardInput"], position["FS.GG.UI.Controls"])
        self.assertLess(position["FS.GG.UI.Layout"], position["FS.GG.UI.Controls"])
        # Deterministic: the id-sorted roster input and its reverse give the same order.
        self.assertEqual(order, [p for p, _ in canary.dependency_order(list(reversed(PLAN_ROWS)),
                                                                     {p: GRAPH.get(p, []) for p in PLAN_IDS})])

    def test_publish_follows_dependency_order(self):
        ordered = self.ordered()
        fake = Fake()
        global FILE_TO_ID
        saved, FILE_TO_ID = FILE_TO_ID, {file: package for package, file in ordered}
        try:
            receipt = run(fake, rows=ordered)
        finally:
            FILE_TO_ID = saved
        self.assertEqual([p for p, _ in ordered], fake.pushed())
        self.assertEqual("FS.GG.UI.Build", receipt["canaryPackageId"])
        self.assertEqual([p for p, _ in ordered], receipt["order"])

    def test_cycle_unknown_and_last_violations_refuse(self):
        cyclic = {**GRAPH, "FS.GG.UI.Scene": ["FS.GG.UI.Controls"]}
        unknown = {**GRAPH, "FS.GG.UI.Layout": ["FS.GG.UI.Scene", "FS.GG.UI.Unknown"]}
        on_bom = {**GRAPH, "FS.GG.UI.Testing": ["FS.GG.UI"]}
        on_template = {**GRAPH, "FS.GG.UI.Testing": ["FS.GG.UI.Template"]}
        template_on_bom = {**GRAPH, "FS.GG.UI.Template": ["FS.GG.UI"]}
        self_dep = {**GRAPH, "FS.GG.UI.Scene": ["FS.GG.UI.Scene"]}
        for graph in [cyclic, unknown, on_bom, on_template, template_on_bom, self_dep]:
            with self.assertRaises(canary.OrderRefused):
                self.ordered(graph)
        with self.assertRaises(canary.OrderRefused):
            canary.dependency_order([r for r in PLAN_ROWS if r[0] != "FS.GG.UI"], GRAPH)

    def test_archive_identity_must_match_roster(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for package, file in PLAN_ROWS:
                self.nupkg(root, "FS.GG.UI.Other" if package == "FS.GG.UI.Scene" else package, file, [])
            with self.assertRaises(canary.OrderRefused):
                canary.archive_dependencies(root, PLAN_ROWS)


class CustodyProbeExitCodeTests(unittest.TestCase):
    def test_probe_codes_are_distinct_and_match_helper(self):
        custody = canary.load_custody()
        self.assertEqual((custody.PROBE_MATCHED, custody.PROBE_ABSENT, custody.PROBE_MISMATCH, custody.PROBE_UNREADABLE),
                         (canary.MATCHED, canary.ABSENT, canary.MISMATCH, canary.UNREADABLE))
        self.assertEqual(4, len({canary.MATCHED, canary.ABSENT, canary.MISMATCH, canary.UNREADABLE, 1}) - 1)


if __name__ == "__main__":
    unittest.main(verbosity=1)
