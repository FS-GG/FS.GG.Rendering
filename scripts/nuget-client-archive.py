#!/usr/bin/env python3
"""Restore exact GitHub Packages archives through the supported NuGet client path."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


class ArchiveError(RuntimeError):
    pass


def load_ids(plan_path: Path, package_id: str | None) -> list[str]:
    packages = json.loads(plan_path.read_text()).get("packages", [])
    ids = [item.get("id") for item in packages]
    if len(ids) != 19 or not all(isinstance(item, str) for item in ids):
        raise ArchiveError("release plan must contain exactly 19 package IDs")
    if package_id is not None:
        if package_id not in ids:
            raise ArchiveError(f"package {package_id} is absent from the release plan")
        return [package_id]
    return ids


def archive_path(cache: Path, package_id: str, version: str) -> Path:
    lower = package_id.lower()
    return cache / lower / version / f"{lower}.{version}.nupkg"


def verify_archive(path: Path, package_id: str, version: str) -> str:
    if not path.is_file():
        raise ArchiveError(f"NuGet client did not retain expected archive {path}")
    try:
        with zipfile.ZipFile(path) as archive:
            nuspecs = [name for name in archive.namelist() if name.lower().endswith(".nuspec")]
            if len(nuspecs) != 1:
                raise ArchiveError(f"expected one nuspec in {path}, found {len(nuspecs)}")
            root = ET.fromstring(archive.read(nuspecs[0]))
    except (OSError, zipfile.BadZipFile, ET.ParseError) as exc:
        raise ArchiveError(f"invalid NuGet archive {path}: {exc}") from exc
    identity = next((node.text for node in root.iter() if node.tag.rsplit("}", 1)[-1] == "id"), None)
    observed_version = next(
        (node.text for node in root.iter() if node.tag.rsplit("}", 1)[-1] == "version"), None
    )
    if identity != package_id or observed_version != version:
        raise ArchiveError(
            f"archive identity mismatch: expected {package_id} {version}, "
            f"observed {identity} {observed_version}"
        )
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_restore_inputs(
    root: Path, package_ids: list[str], version: str, source_url: str, username: str, token: str
) -> tuple[Path, Path]:
    project = ET.Element("Project", {"Sdk": "Microsoft.NET.Sdk"})
    properties = ET.SubElement(project, "PropertyGroup")
    ET.SubElement(properties, "TargetFramework").text = "net10.0"
    group = ET.SubElement(project, "ItemGroup")
    for package_id in package_ids:
        ET.SubElement(group, "PackageDownload", {"Include": package_id, "Version": f"[{version}]"})
    project_path = root / "archive-readback.csproj"
    ET.ElementTree(project).write(project_path, encoding="utf-8", xml_declaration=True)

    config = ET.Element("configuration")
    sources = ET.SubElement(config, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", {"key": "github", "value": source_url})
    ET.SubElement(sources, "add", {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json"})
    credentials = ET.SubElement(config, "packageSourceCredentials")
    github = ET.SubElement(credentials, "github")
    ET.SubElement(github, "add", {"key": "Username", "value": username})
    ET.SubElement(github, "add", {"key": "ClearTextPassword", "value": token})
    mapping = ET.SubElement(config, "packageSourceMapping")
    github_mapping = ET.SubElement(mapping, "packageSource", {"key": "github"})
    ET.SubElement(github_mapping, "package", {"pattern": "FS.GG.*"})
    nuget_mapping = ET.SubElement(mapping, "packageSource", {"key": "nuget.org"})
    ET.SubElement(nuget_mapping, "package", {"pattern": "*"})
    config_path = root / "nuget.config"
    ET.ElementTree(config).write(config_path, encoding="utf-8", xml_declaration=True)
    config_path.chmod(0o600)
    return project_path, config_path


def restore(
    package_ids: list[str],
    version: str,
    source_url: str,
    username: str,
    token: str,
    cache: Path,
    attempts: int,
    retry_delay: int,
) -> list[dict]:
    cache.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="fsgg-nuget-readback-") as value:
        root = Path(value)
        project, config = write_restore_inputs(root, package_ids, version, source_url, username, token)
        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(cache.resolve())
        for attempt in range(1, attempts + 1):
            result = subprocess.run(
                [
                    "dotnet",
                    "restore",
                    str(project),
                    "--configfile",
                    str(config),
                    "--packages",
                    str(cache.resolve()),
                    "--no-cache",
                    "--force-evaluate",
                ],
                env=environment,
                check=False,
            )
            if result.returncode == 0:
                break
            if attempt == attempts:
                raise ArchiveError(
                    f"NuGet client restore failed with exit code {result.returncode} after {attempts} attempt(s)"
                )
            time.sleep(retry_delay)
    records = []
    for package_id in package_ids:
        path = archive_path(cache, package_id, version)
        digest = verify_archive(path, package_id, version)
        records.append({"id": package_id, "version": version, "path": str(path), "sha256": digest})
    return records


def version_present(index_template: str, package_id: str, version: str, username: str, token: str) -> bool:
    url = index_template.format(id_lower=package_id.lower())
    credential = base64.b64encode(f"{username}:{token}".encode()).decode()
    request = urllib.request.Request(
        url,
        headers={"Authorization": f"Basic {credential}", "User-Agent": "FS-GG.Rendering-nuget-client/1"},
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            status, body = response.status, response.read()
    except urllib.error.HTTPError as exc:
        status, body = exc.code, exc.read()
    except (OSError, urllib.error.URLError) as exc:
        raise ArchiveError(f"package version index unavailable for {package_id}: {exc}") from exc
    if status == 404:
        return False
    if status != 200:
        raise ArchiveError(f"package version index returned HTTP {status} for {package_id}")
    try:
        versions = json.loads(body).get("versions")
    except (UnicodeDecodeError, json.JSONDecodeError, AttributeError) as exc:
        raise ArchiveError(f"package version index is invalid for {package_id}") from exc
    if not isinstance(versions, list) or not all(isinstance(item, str) for item in versions):
        raise ArchiveError(f"package version index has no valid versions array for {package_id}")
    return version in versions


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("command", choices=["restore", "probe"])
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--package-id")
    parser.add_argument("--version", required=True)
    parser.add_argument("--source-url", default="https://nuget.pkg.github.com/FS-GG/index.json")
    parser.add_argument("--index-template", default="https://nuget.pkg.github.com/FS-GG/download/{id_lower}/index.json")
    parser.add_argument("--username", required=True)
    parser.add_argument("--token-env", required=True)
    parser.add_argument("--cache", type=Path, required=True)
    parser.add_argument("--receipt", type=Path)
    parser.add_argument("--attempts", type=int, default=1)
    parser.add_argument("--retry-delay", type=int, default=0)
    args = parser.parse_args()
    token = os.environ.get(args.token_env, "")
    if not token:
        raise ArchiveError(f"{args.token_env} is absent")
    if args.attempts < 1 or args.retry_delay < 0:
        raise ArchiveError("attempts must be positive and retry-delay must be non-negative")
    package_ids = load_ids(args.plan, args.package_id)
    if args.command == "probe":
        if len(package_ids) != 1:
            raise ArchiveError("probe requires exactly one --package-id")
        if not version_present(
            args.index_template, package_ids[0], args.version, args.username, token
        ):
            print(f"NuGet client archive absent: {package_ids[0]} {args.version}")
            return 4
    records = restore(
        package_ids,
        args.version,
        args.source_url,
        args.username,
        token,
        args.cache,
        args.attempts,
        args.retry_delay,
    )
    receipt = {
        "schema": "fsgg.rendering.nuget-client-archive/v1",
        "result": "pass",
        "source": args.source_url,
        "packages": records,
    }
    if args.receipt:
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    for record in records:
        print(f"NuGet client archive: {record['id']} {record['version']} {record['sha256']}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ArchiveError as exc:
        print(f"NuGet client archive refused: {exc}", file=sys.stderr)
        raise SystemExit(1)
