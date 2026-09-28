#!/usr/bin/env python3
from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
POLICY_PATH = ROOT / "policy/v2-ci-ordinary-settlement.json"
SPEC = importlib.util.spec_from_file_location(
    "v2_ci_ordinary_qualification", ROOT / "tools/v2-ci-ordinary-qualification.py"
)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)
SOURCE = "53a0f6c8f03bb8c4a60d55c3ce8a38c78a26b1b5"
HEAD = "353ff86a50808959770a73863385646efaf69969"


class RenderingQualificationTests(unittest.TestCase):
    def setUp(self):
        self.policy = json.loads(POLICY_PATH.read_text())
        self.digest = hashlib.sha256(POLICY_PATH.read_bytes()).hexdigest()
        self.profile = MODULE.RENDERING_SOURCE_PROFILE
        self.runtime = {
            "schema": "fsgg.github.v2-ci-runtime/1",
            "eventName": "push",
            "sourceProfile": "rendering-v1",
            "repository": "FS-GG/FS.GG.Rendering",
            "repositoryId": 1269292235,
            "ref": "refs/heads/main",
            "eventAfter": SOURCE,
            "sourceSha": SOURCE,
            "workflowPath": ".github/workflows/v2-ci-ordinary-settlement.yml",
            "workflowRevision": SOURCE,
            "environment": "ordinary-v2",
            "runner": "github-hosted-ephemeral",
            "operationClass": "ordinary-post-merge-delivery-settlement",
            "phase": "secret-free-predecessor",
            "credentialAccess": False,
        }
        self.associations = [{
            "number": 1358,
            "node_id": "PR_kwDORendering1358",
            "state": "closed",
            "merged_at": "2026-09-27T18:17:18Z",
            "merge_commit_sha": SOURCE,
            "head": {"sha": HEAD},
            "base": {
                "ref": "main",
                "sha": "af5a748d075d6578300822c8b64251c7c85b3f91",
                "repo": {"full_name": "FS-GG/FS.GG.Rendering"},
            },
        }]

        def check(name: str, index: int) -> dict:
            producer = self.profile["checkProducers"][name]
            return {
                "name": name, "conclusion": "success", "sourceSha": HEAD, "appId": 15368,
                "checkRunId": index + 1, "workflowRunId": 1000 + index,
                "runAttempt": 1, "checkSuiteId": 2000 + index,
                "workflowId": producer["workflowId"], "workflowPath": producer["path"],
            }

        self.evidence = {
            "schema": "fsgg.github.v2-ci-qualification-evidence/1",
            "status": "passed",
            "subject": {
                "sourceSha": SOURCE, "qualificationSha": HEAD,
                "workflowPath": ".github/workflows/v2-ci-ordinary-settlement.yml",
                "workflowRevision": SOURCE, "environment": "ordinary-v2",
                "operationClass": "ordinary-post-merge-delivery-settlement",
                "policySha256": self.digest,
            },
            "checks": [check(name, index)
                       for index, name in enumerate(self.profile["requiredChecks"])],
            "gateChecks": [check(name, index)
                           for index, name in enumerate(self.profile["requiredGateChecks"])],
        }

    def qualify(self, runtime=None, associations=None, evidence=None):
        return MODULE.qualify(
            self.policy, self.digest, runtime or self.runtime,
            self.associations if associations is None else associations,
            evidence or self.evidence, "rendering-v1",
        )

    def test_receipt_binds_rendering_identity_and_separate_check_populations(self):
        receipt = self.qualify()
        self.assertEqual("rendering-v1", receipt["sourceProfile"])
        self.assertEqual("FS-GG/FS.GG.Rendering", receipt["sourceRepository"])
        self.assertEqual(1269292235, receipt["sourceRepositoryId"])
        self.assertFalse(receipt["credentialAccess"])
        self.assertEqual(set(self.profile["requiredChecks"]),
                         {check["name"] for check in receipt["requiredChecks"]})
        self.assertEqual(set(self.profile["requiredGateChecks"]),
                         {check["name"] for check in receipt["requiredGateChecks"]})
        self.assertEqual(2, len(receipt["requiredChecks"]))
        self.assertEqual(5, len(receipt["requiredGateChecks"]))

    def test_wrong_profile_repository_identity_and_producer_refuse(self):
        runtime = copy.deepcopy(self.runtime)
        runtime["sourceProfile"] = "audio-v1"
        with self.assertRaisesRegex(MODULE.Refusal, "wrong source profile"):
            self.qualify(runtime=runtime)
        runtime = copy.deepcopy(self.runtime)
        runtime["repositoryId"] = 1269292704
        with self.assertRaisesRegex(MODULE.Refusal, "wrong source repository identity"):
            self.qualify(runtime=runtime)
        evidence = copy.deepcopy(self.evidence)
        evidence["checks"][0]["workflowId"] = 1
        with self.assertRaisesRegex(MODULE.Refusal, "native producer"):
            self.qualify(evidence=evidence)
        with self.assertRaisesRegex(MODULE.Refusal, "unknown ordinary-v2 source profile"):
            MODULE.qualify(self.policy, self.digest, self.runtime, self.associations,
                           self.evidence, "caller-selected")

    def test_failed_incomplete_stale_and_foreign_evidence_refuse(self):
        evidence = copy.deepcopy(self.evidence)
        evidence["checks"][0]["conclusion"] = "failure"
        with self.assertRaisesRegex(MODULE.Refusal, "failed or stale"):
            self.qualify(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["gateChecks"].pop()
        with self.assertRaisesRegex(MODULE.Refusal, "population"):
            self.qualify(evidence=evidence)
        runtime = copy.deepcopy(self.runtime)
        runtime["ref"] = "refs/heads/feature"
        with self.assertRaisesRegex(MODULE.Refusal, "protected main"):
            self.qualify(runtime=runtime)
        associations = copy.deepcopy(self.associations)
        associations[0]["base"]["repo"]["full_name"] = "FS-GG/other"
        with self.assertRaisesRegex(MODULE.Refusal, "wrong repository"):
            self.qualify(associations=associations)

    def test_policy_remains_disabled_with_explicit_release_and_custody_refusals(self):
        self.assertEqual("source-qualified-not-installed", self.policy["status"])
        self.assertFalse(self.policy["credentialJob"]["installed"])
        self.assertEqual(3, self.policy["credentialJob"]["liveObservation"]["secretCount"])
        self.assertEqual(3, len(self.policy["credentialInventory"]))
        self.assertEqual("awaiting-published-rendering-profile-release",
                         self.policy["packagePin"]["status"])
        self.assertIsNone(self.policy["packagePin"]["version"])
        self.assertIsNone(self.policy["packagePin"]["sha256"])
        self.assertIn("no immutable published release asset",
                      self.policy["packagePin"]["refusal"])
        self.assertEqual(["OpenV2"], self.policy["unchangedGates"])
        self.assertEqual({"v1Admission": False, "receiverStateImport": False},
                         self.policy["migration"])


if __name__ == "__main__":
    unittest.main()
