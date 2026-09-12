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


def flat_container_filename(package_id: str, version: str) -> str:
    """Return the NuGet V3 flat-container archive name (always lowercase)."""
    return f"{package_id}.{version}.nupkg".lower()


def git(root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *args], capture_output=True, text=True, check=False
    )
    if result.returncode:
        fail(f"git {' '.join(args)} failed: {' '.join(result.stderr.split())}")
    return result.stdout.strip()


def status(url: str, token: str | None = None, username: str = "x-access-token") -> int:
    return request(url, token, username)[0]


def request(
    url: str, token: str | None = None, username: str = "x-access-token"
) -> tuple[int, bytes]:
    headers = {"User-Agent": "FS-GG.Rendering-release-preflight/1"}
    if token:
        credential = base64.b64encode(f"{username}:{token}".encode()).decode()
        headers["Authorization"] = f"Basic {credential}"
    request = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as error:
        return error.code, error.read()
    except (OSError, urllib.error.URLError) as error:
        fail(f"feed unavailable for {url}: {error}")


def github_nuget_diagnostic(
    token: str, username: str, package_id: str, baseline: str, target: str
) -> dict:
    """Observe the established workflow token through NuGet V3 without mutating a feed."""
    service_url = "https://nuget.pkg.github.com/FS-GG/index.json"
    service_status, service_body = request(service_url, token, username)
    result: dict = {
        "credential": "repository GITHUB_TOKEN (historical 0.28 publisher)",
        "serviceIndex": {"url": service_url, "status": service_status},
    }
    resources: list[dict] = []
    if service_status == 200:
        try:
            document = json.loads(service_body)
            resources = document.get("resources", [])
        except (UnicodeDecodeError, json.JSONDecodeError, AttributeError):
            result["serviceIndex"]["parse"] = "invalid-json"

    lower = package_id.lower()
    filename = flat_container_filename(package_id, baseline)
    fallback_base = "https://nuget.pkg.github.com/FS-GG/download/"
    package_base = next(
        (
            item.get("@id")
            for item in resources
            if str(item.get("@type", "")).startswith("PackageBaseAddress/")
        ),
        fallback_base,
    )
    registration_base = next(
        (
            item.get("@id")
            for item in resources
            if str(item.get("@type", "")).startswith("RegistrationsBaseUrl/")
        ),
        None,
    )
    version_url = f"{package_base.rstrip('/')}/{lower}/index.json"
    version_status, version_body = request(version_url, token, username)
    version_observation: dict = {"url": version_url, "status": version_status}
    if version_status == 200:
        try:
            versions = json.loads(version_body).get("versions", [])
            version_observation.update(
                {
                    "baselineListed": baseline in versions,
                    "targetListed": target in versions,
                }
            )
        except (UnicodeDecodeError, json.JSONDecodeError, AttributeError):
            version_observation["parse"] = "invalid-json"
    result["versionIndex"] = version_observation

    if registration_base:
        registration_url = f"{registration_base.rstrip('/')}/{lower}/index.json"
        registration_status, _ = request(registration_url, token, username)
        result["registrationIndex"] = {
            "url": registration_url,
            "status": registration_status,
        }
    else:
        result["registrationIndex"] = {"status": "not-advertised"}

    archive_url = f"{package_base.rstrip('/')}/{lower}/{baseline}/{filename}"
    archive_status, _ = request(archive_url, token, username)
    result["baselineArchive"] = {"url": archive_url, "status": archive_status}
    return result


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
    parser.add_argument("--github-workflow-token-env")
    parser.add_argument("--github-workflow-username")
    parser.add_argument("--diagnostic", type=Path)
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
    if args.github_workflow_token_env:
        workflow_token = os.environ.get(args.github_workflow_token_env, "")
        if not workflow_token:
            fail(f"{args.github_workflow_token_env} is absent; historical publisher diagnostic unavailable")
        if not args.github_workflow_username:
            fail("historical publisher diagnostic requires the repository workflow actor")
        diagnostic = github_nuget_diagnostic(
            workflow_token,
            args.github_workflow_username,
            anchor_id,
            baseline,
            args.version,
        )
        if args.diagnostic:
            args.diagnostic.parent.mkdir(parents=True, exist_ok=True)
            args.diagnostic.write_text(
                json.dumps(diagnostic, indent=2, sort_keys=True) + "\n"
            )
        print("release-preflight: historical publisher diagnostic " + json.dumps(diagnostic, sort_keys=True))
    anchor_lower = anchor_id.lower()
    anchor_file = flat_container_filename(anchor_id, baseline)
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
        filename = flat_container_filename(package_id, args.version)
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
