#!/usr/bin/env python3
"""Qualify the secret-free boundary for one post-merge ordinary-v2 settlement."""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import sys
from typing import Any


SHA = re.compile(r"^[0-9a-f]{40}$")

RENDERING_SOURCE_PROFILE = {
    "key": "rendering-v1",
    "repository": "FS-GG/FS.GG.Rendering",
    "repositoryId": 1269292235,
    "requiredCheckAppId": 15368,
    "requiredChecks": [
        "Deterministic gate",
        "routine-eligibility",
    ],
    "requiredGateChecks": [
        "Deterministic gate",
        "API compatibility gate (breaking-change → SemVer major)",
        "kit / coordination-kit",
        "skill-view-check",
        "materialize / receiver-validate",
    ],
    "checkProducers": {
        "Deterministic gate": {
            "workflowId": 295951544, "path": ".github/workflows/gate.yml", "event": "pull_request",
        },
        "API compatibility gate (breaking-change → SemVer major)": {
            "workflowId": 295951544, "path": ".github/workflows/gate.yml", "event": "pull_request",
        },
        "kit / coordination-kit": {
            "workflowId": 307167629,
            "path": ".github/workflows/coordination-coherence.yml", "event": "pull_request",
        },
        "skill-view-check": {
            "workflowId": 321983529,
            "path": ".github/workflows/skill-view-check.yml", "event": "pull_request",
        },
        "materialize / receiver-validate": {
            "workflowId": 316870201,
            "path": ".github/workflows/kit-materialize.yml", "event": "pull_request",
        },
        "routine-eligibility": {
            "workflowId": 356280746,
            "path": ".github/workflows/routine-eligibility.yml", "event": "pull_request_target",
        },
    },
}


class Refusal(ValueError):
    pass


def read_json(path: str) -> Any:
    target = pathlib.Path(path)
    if target.is_symlink() or not target.is_file() or target.stat().st_size > 2 * 1024 * 1024:
        raise Refusal(f"invalid bounded JSON input: {path}")
    try:
        return json.loads(target.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise Refusal(f"unreadable JSON input {path}: {error}") from error


def policy_digest(path: str) -> str:
    target = pathlib.Path(path)
    return hashlib.sha256(target.read_bytes()).hexdigest()


def require_equal(actual: Any, expected: Any, message: str) -> None:
    if actual != expected:
        raise Refusal(message)


def source_profile(policy: dict[str, Any], selector: str) -> dict[str, Any]:
    """Resolve a bounded selector to code-owned source identity and check policy."""
    if selector in ("", "dotgithub-v1"):
        require_equal(policy.get("repository"), "FS-GG/.github",
                      "legacy source repository differs from policy")
        qualification = policy["qualification"]
        return {
            "key": "dotgithub-v1",
            "repository": "FS-GG/.github",
            "repositoryId": 1269292704,
            "requiredCheckAppId": qualification["requiredCheckAppId"],
            "requiredChecks": qualification["requiredChecks"],
            "requiredGateChecks": qualification["requiredGateChecks"],
            "checkProducers": qualification["checkProducers"],
        }
    if selector == RENDERING_SOURCE_PROFILE["key"]:
        return RENDERING_SOURCE_PROFILE
    raise Refusal("unknown ordinary-v2 source profile")


def qualify(policy: dict[str, Any], digest: str, runtime: dict[str, Any],
            associations: list[dict[str, Any]], evidence: dict[str, Any],
            source_profile_key: str = "dotgithub-v1") -> dict[str, Any]:
    require_equal(policy.get("schema"), "fsgg.github.v2-ci-ordinary-settlement-policy/1",
                  "unsupported policy schema")
    installed = policy.get("credentialJob", {}).get("installed")
    if not isinstance(installed, bool):
        raise Refusal("credential activation is malformed")
    expected_status = "installed" if installed else "source-qualified-not-installed"
    require_equal(policy.get("status"), expected_status, "policy activation status differs from credential job")

    trigger = policy["trigger"]
    workflow = policy["workflow"]
    job = policy["credentialJob"]
    qualification = policy["qualification"]
    selected_source = source_profile(policy, source_profile_key)
    source = runtime.get("sourceSha")
    if not isinstance(source, str) or not SHA.fullmatch(source):
        raise Refusal("source SHA is missing or malformed")
    require_equal(runtime.get("schema"), "fsgg.github.v2-ci-runtime/1", "unsupported runtime schema")
    require_equal(runtime.get("eventName"), trigger["event"], "wrong event for the selected policy")
    require_equal(runtime.get("sourceProfile"), selected_source["key"], "wrong source profile")
    require_equal(runtime.get("repository"), selected_source["repository"], "wrong source repository")
    require_equal(runtime.get("repositoryId"), selected_source["repositoryId"],
                  "wrong source repository identity")
    require_equal(runtime.get("ref"), trigger["ref"], "wrong source ref: protected main is required")
    require_equal(runtime.get("eventAfter"), source, "wrong source: push after SHA differs from the runtime source")
    require_equal(runtime.get("workflowPath"), workflow["path"], "wrong workflow")
    require_equal(runtime.get("workflowRevision"), source, "wrong workflow revision: it must be the pushed source")
    require_equal(runtime.get("environment"), job["environment"], "wrong environment")
    require_equal(runtime.get("runner"), job["runner"], "wrong runner class")
    require_equal(runtime.get("operationClass"), policy["operationClass"], "unsupported operation class")
    require_equal(runtime.get("phase"), qualification["phase"], "credential use is forbidden in the predecessor")
    require_equal(runtime.get("credentialAccess"), False, "credential use is forbidden in the predecessor")

    if not isinstance(associations, list) or len(associations) != trigger["associationCount"]:
        raise Refusal("ambiguous merge association: expected exactly one associated pull request")
    pull = associations[0]
    if pull.get("state") != "closed" or pull.get("merged_at") is None:
        raise Refusal("associated pull request is not merged")
    require_equal(pull.get("merge_commit_sha"), source, "wrong source: associated merge commit differs")
    require_equal((pull.get("base") or {}).get("ref"), "main", "associated pull request did not merge to main")
    require_equal(((pull.get("base") or {}).get("repo") or {}).get("full_name"), selected_source["repository"],
                  "associated pull request belongs to the wrong repository")
    number = pull.get("number")
    if not isinstance(number, int) or isinstance(number, bool) or number < 1:
        raise Refusal("associated pull request number is invalid")
    head_sha = (pull.get("head") or {}).get("sha")
    if not isinstance(head_sha, str) or not SHA.fullmatch(head_sha):
        raise Refusal("associated pull request head SHA is invalid")
    node_id = pull.get("node_id")
    base_sha = (pull.get("base") or {}).get("sha")
    if not isinstance(node_id, str) or not node_id or not isinstance(base_sha, str) or not SHA.fullmatch(base_sha):
        raise Refusal("associated pull request identity is incomplete")

    require_equal(evidence.get("schema"), "fsgg.github.v2-ci-qualification-evidence/1",
                  "unsupported qualification evidence schema")
    require_equal(evidence.get("status"), "passed", "qualification evidence did not pass")
    subject = evidence.get("subject") or {}
    expected_subject = {
        "sourceSha": source,
        "qualificationSha": head_sha,
        "workflowPath": workflow["path"],
        "workflowRevision": source,
        "environment": job["environment"],
        "operationClass": policy["operationClass"],
        "policySha256": digest,
    }
    require_equal(subject, expected_subject, "stale or mismatched qualification evidence")
    if set(selected_source["checkProducers"]) != set(
            selected_source["requiredChecks"] + selected_source["requiredGateChecks"]):
        raise Refusal("check-producer policy population is incomplete")

    def validate_checks(field: str, expected: str) -> list[dict[str, Any]]:
        population = evidence.get(field)
        if not isinstance(population, list):
            raise Refusal(f"qualification {field} are missing")
        check_map: dict[str, dict[str, Any]] = {}
        for check in population:
            if not isinstance(check, dict) or not isinstance(check.get("name"), str) or check["name"] in check_map:
                raise Refusal(f"qualification {field} are malformed or duplicated")
            check_map[check["name"]] = check
        if set(check_map) != set(selected_source[expected]):
            raise Refusal(f"qualification {field} population is incomplete or unexpected")
        for name, check in check_map.items():
            if (check.get("conclusion") != "success" or check.get("sourceSha") != head_sha
                    or check.get("appId") != selected_source["requiredCheckAppId"]):
                raise Refusal(f"qualification check {name} is failed or stale")
            producer = selected_source["checkProducers"][name]
            if (check.get("workflowId") != producer["workflowId"]
                    or check.get("workflowPath") != producer["path"]
                    or any(not isinstance(check.get(key), int) or isinstance(check.get(key), bool)
                           or check[key] < 1 for key in ("checkRunId", "workflowRunId", "runAttempt", "checkSuiteId"))):
                raise Refusal(f"qualification check {name} has no bound native producer")
        return population

    checks = validate_checks("checks", "requiredChecks")
    gate_checks = validate_checks("gateChecks", "requiredGateChecks")

    return {
        "schema": "fsgg.github.v2-ci-secret-free-predecessor-receipt/1",
        "status": "qualified",
        "policyId": policy["policyId"],
        "policySha256": digest,
        "sourceProfile": selected_source["key"],
        "sourceRepository": selected_source["repository"],
        "sourceRepositoryId": selected_source["repositoryId"],
        "sourceSha": source,
        "pullRequest": number,
        "pullRequestNodeId": node_id,
        "pullRequestBaseSha": base_sha,
        "mergeCommitSha": source,
        "qualificationSha": head_sha,
        "requiredChecks": sorted(checks, key=lambda check: check["name"]),
        "requiredGateChecks": sorted(gate_checks, key=lambda check: check["name"]),
        "workflowPath": workflow["path"],
        "workflowRevision": source,
        "environment": job["environment"],
        "operationClass": policy["operationClass"],
        "credentialAccess": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("qualify", nargs="?")
    parser.add_argument("--policy", required=True)
    parser.add_argument("--runtime", required=True)
    parser.add_argument("--associations", required=True)
    parser.add_argument("--evidence", required=True)
    parser.add_argument("--source-profile", choices=("dotgithub-v1", "rendering-v1"),
                        default="dotgithub-v1")
    parser.add_argument("--output")
    args = parser.parse_args()
    try:
        receipt = qualify(read_json(args.policy), policy_digest(args.policy), read_json(args.runtime),
                          read_json(args.associations), read_json(args.evidence), args.source_profile)
        encoded = json.dumps(receipt, sort_keys=True, separators=(",", ":")) + "\n"
        if args.output:
            output = pathlib.Path(args.output)
            if output.exists() or output.is_symlink():
                raise Refusal("output must be absent")
            output.write_text(encoded, encoding="utf-8")
        else:
            sys.stdout.write(encoded)
        return 0
    except (KeyError, OSError, TypeError, Refusal) as error:
        print(f"v2 ordinary settlement refused: {error}", file=sys.stderr)
        return 3


if __name__ == "__main__":
    raise SystemExit(main())
