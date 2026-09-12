#!/usr/bin/env python3
"""Non-publishing collision and authority preflight for a Rendering release."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
from pathlib import Path


SHA = re.compile(r"^[0-9a-f]{40}$")


def fail(message: str) -> None:
    raise SystemExit(f"release-preflight: {message}")


def git(root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *args], capture_output=True, text=True, check=False
    )
    if result.returncode:
        fail(f"git {' '.join(args)} failed: {' '.join(result.stderr.split())}")
    return result.stdout.strip()


def status(url: str, token: str | None = None) -> int:
    headers = {"User-Agent": "FS-GG.Rendering-release-preflight/1"}
    if token:
        credential = base64.b64encode(f"x-access-token:{token}".encode()).decode()
        headers["Authorization"] = f"Basic {credential}"
    request = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            response.read(1)
            return response.status
    except urllib.error.HTTPError as error:
        return error.code
    except (OSError, urllib.error.URLError) as error:
        fail(f"feed unavailable for {url}: {error}")


def source_text(root: Path, source_sha: str, path: str) -> str:
    return git(root, "show", f"{source_sha}:{path}")


def single_value(text: str, pattern: str, label: str) -> str:
    values = re.findall(pattern, text)
    if len(values) != 1:
        fail(f"expected exactly one {label}, found {len(values)}")
    return values[0]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--workflow-sha", required=True)
    parser.add_argument("--github-app-id", required=True)
    parser.add_argument("--github-installation-id", required=True)
    parser.add_argument("--github-repository", required=True)
    parser.add_argument("--github-workflow-ref", required=True)
    parser.add_argument("--github-run-id", required=True)
    parser.add_argument("--github-token-env", default="GITHUB_TOKEN")
    parser.add_argument(
        "--github-url-template",
        default="https://nuget.pkg.github.com/FS-GG/download/{id_lower}/{version}/{filename}",
    )
    parser.add_argument(
        "--nuget-url-template",
        default="https://api.nuget.org/v3-flatcontainer/{id_lower}/{version}/{filename}",
    )
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()

    root = args.repo_root.resolve()
    if not SHA.fullmatch(args.source_sha) or not SHA.fullmatch(args.workflow_sha):
        fail("source and workflow identities must be exact lowercase 40-hex SHAs")
    if not args.github_app_id.isdigit() or not args.github_installation_id.isdigit():
        fail("GitHub App and installation identities must be decimal IDs")
    if args.github_repository != "FS-GG/FS.GG.Rendering":
        fail(f"unexpected GitHub repository identity {args.github_repository!r}")
    if "/.github/workflows/release.yml@" not in args.github_workflow_ref:
        fail(f"unexpected workflow identity {args.github_workflow_ref!r}")
    if not args.github_run_id.isdigit():
        fail("GitHub workflow run identity must be a decimal ID")
    token = os.environ.get(args.github_token_env, "")
    if not token:
        fail(f"{args.github_token_env} is absent; authenticated GitHub collision state unavailable")

    plan_bytes = args.plan.read_bytes()
    plan = json.loads(plan_bytes)
    if plan.get("version") != args.version:
        fail(f"plan version {plan.get('version')!r} does not equal requested {args.version!r}")
    packages = plan.get("packages")
    if not isinstance(packages, list) or len(packages) != 19:
        fail("release plan must contain exactly 19 packages")
    ids = [item.get("id") for item in packages]
    if not all(isinstance(item, str) for item in ids) or len(ids) != len(set(ids)):
        fail("release plan package IDs must be 19 unique strings")

    git(root, "cat-file", "-e", f"{args.source_sha}^{{commit}}")
    framework = single_value(
        source_text(root, args.source_sha, "template/base/Directory.Packages.props"),
        r"<FsGgUiVersion>([^<]+)</FsGgUiVersion>",
        "FsGgUiVersion",
    )
    template = single_value(
        source_text(root, args.source_sha, ".template.package/FS.GG.UI.Template.fsproj"),
        r"<Version>([^<]+)</Version>",
        "template Version",
    )
    if framework != args.version or template != args.version:
        fail(
            f"source axes do not match {args.version}: framework={framework}, template={template}"
        )

    tags = plan.get("tags")
    expected_tags = [
        f"fs-gg-ui/v{args.version}",
        f"fs-gg-ui-template/v{args.version}",
        f"v{args.version}",
    ]
    if tags != expected_tags:
        fail(f"plan tags are not the ordered release triple {expected_tags}")
    for tag in tags:
        if git(root, "ls-remote", "--tags", "origin", f"refs/tags/{tag}"):
            fail(f"tag collision: {tag} already exists")

    anchor_id = "FS.GG.UI.Scene"
    baseline = plan.get("baselineVersion")
    anchor_lower = anchor_id.lower()
    anchor_file = f"{anchor_id}.{baseline}.nupkg"
    anchor_url = args.github_url_template.format(
        id=anchor_id, id_lower=anchor_lower, version=baseline, filename=anchor_file
    )
    anchor_status = status(anchor_url, token)
    if anchor_status != 200:
        fail(
            f"authenticated GitHub Packages read unavailable: known {anchor_id} {baseline} "
            f"returned HTTP {anchor_status}, expected 200"
        )

    observations = []
    for package_id in ids:
        lower = package_id.lower()
        filename = f"{package_id}.{args.version}.nupkg"
        github_url = args.github_url_template.format(
            id=package_id, id_lower=lower, version=args.version, filename=filename
        )
        nuget_url = args.nuget_url_template.format(
            id=package_id, id_lower=lower, version=args.version, filename=filename
        )
        github_status = status(github_url, token)
        nuget_status = status(nuget_url)
        for feed, observed in (("GitHub Packages", github_status), ("nuget.org", nuget_status)):
            if observed == 200:
                fail(f"collision: {package_id} {args.version} already exists on {feed}")
            if observed != 404:
                fail(
                    f"{feed} collision state unavailable for {package_id} {args.version}: "
                    f"HTTP {observed}, expected 404 absent or 200 collision"
                )
        observations.append(
            {"id": package_id, "githubPackages": github_status, "nugetOrg": nuget_status}
        )

    receipt = {
        "schema": "fsgg.rendering.release-preflight/v1",
        "result": "pass",
        "sourceSha": args.source_sha,
        "workflowSha": args.workflow_sha,
        "workflow": {
            "repository": args.github_repository,
            "ref": args.github_workflow_ref,
            "runId": args.github_run_id,
        },
        "version": args.version,
        "frameworkVersion": framework,
        "templateVersion": template,
        "planSha256": hashlib.sha256(plan_bytes).hexdigest(),
        "rosterCount": len(ids),
        "roster": ids,
        "tags": tags,
        "tagCollisions": 0,
        "githubReadAnchor": {
            "id": anchor_id,
            "version": baseline,
            "status": anchor_status,
        },
        "collisions": observations,
        "permissions": {
            "githubPackages": {
                "credential": "fs-gg-cross-repo-dispatch installation token",
                "appId": args.github_app_id,
                "installationId": args.github_installation_id,
                "scope": f"{args.github_repository}:packages:read",
                "proof": "authenticated baseline read passed",
            },
            "nugetOrg": "id-token:write job grant; NuGet/login completed before this script",
        },
        "mutation": "none",
    }
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    print(
        f"release-preflight: PASS · source {args.source_sha} · {len(ids)} packages absent on both feeds · "
        "authenticated GitHub baseline read 200 · no tags · no mutation"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
