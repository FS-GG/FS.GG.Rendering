#!/usr/bin/env python3
"""Create and verify immutable custody for a coherent Rendering release.

The manifest is deliberately generated from already-packed archives. Publication and every
release-shaped validator consume that same directory; recovery downloads this retained directory
and verifies it before replaying any byte. A feed-added NuGet signature is the only permitted
readback difference and is compared entry-by-entry rather than waved away at archive level.
"""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

SCHEMA = "fsgg.rendering.release-custody/v1"
SIGNATURE = ".signature.p7s"


class CustodyError(RuntimeError):
    pass


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_json(path: Path) -> dict:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise CustodyError(f"cannot read {path}: {exc}") from exc


def canonical_digest(value: dict) -> str:
    return sha256_bytes(json.dumps(value, sort_keys=True, separators=(",", ":")).encode())


def nuspec(path: Path) -> tuple[dict, dict[str, bytes]]:
    try:
        with zipfile.ZipFile(path) as archive:
            entries: dict[str, bytes] = {}
            for info in archive.infolist():
                if info.is_dir():
                    continue
                if info.filename in entries:
                    raise CustodyError(f"{path}: duplicate ZIP entry {info.filename}")
                entries[info.filename] = archive.read(info)
    except (OSError, zipfile.BadZipFile) as exc:
        raise CustodyError(f"cannot inspect {path}: {exc}") from exc

    candidates = [(name, data) for name, data in entries.items() if "/" not in name and name.lower().endswith(".nuspec")]
    if len(candidates) != 1:
        raise CustodyError(f"{path}: expected exactly one root .nuspec, found {len(candidates)}")
    try:
        root = ET.fromstring(candidates[0][1])
    except ET.ParseError as exc:
        raise CustodyError(f"{path}: invalid nuspec: {exc}") from exc

    def first(name: str) -> ET.Element | None:
        return root.find(f".//{{*}}{name}")

    def text(name: str) -> str:
        node = first(name)
        return (node.text or "").strip() if node is not None else ""

    repository = first("repository")
    license_node = first("license")
    dependencies = []
    for node in root.findall(".//{*}dependency"):
        dependencies.append({"id": node.attrib.get("id", ""), "version": node.attrib.get("version", "")})
    metadata = {
        "id": text("id"),
        "version": text("version"),
        "license": text("license"),
        "licenseType": license_node.attrib.get("type", "") if license_node is not None else "",
        "repositoryUrl": repository.attrib.get("url", "") if repository is not None else "",
        "repositoryCommit": repository.attrib.get("commit", "") if repository is not None else "",
        "readme": text("readme"),
        "dependencies": sorted(dependencies, key=lambda d: (d["id"], d["version"])),
    }
    return metadata, entries


def entry_hashes(entries: dict[str, bytes]) -> dict[str, str]:
    return {name: sha256_bytes(data) for name, data in sorted(entries.items()) if name != SIGNATURE}


def validate_plan(plan: dict) -> None:
    if plan.get("schema") != "fsgg.rendering.release-plan/v1":
        raise CustodyError("unsupported or missing release-plan schema")
    packages = plan.get("packages")
    if not isinstance(packages, list) or len(packages) != 19:
        raise CustodyError(f"release plan must name exactly 19 archives, found {len(packages or [])}")
    ids = [p.get("id") for p in packages]
    if len(set(ids)) != 19 or any(not value for value in ids):
        raise CustodyError("release plan package ids must be 19 unique non-empty values")
    kinds = [p.get("kind") for p in packages]
    if kinds.count("library") != 17 or kinds.count("bom") != 1 or kinds.count("template") != 1:
        raise CustodyError("release plan roster must be 17 libraries + one BOM + one template")
    if plan.get("baselineVersion") != "0.28.0":
        raise CustodyError("SVG Preview A must be compared with the actual 0.28.0 public baseline")


def archive_record(path: Path) -> dict:
    meta, entries = nuspec(path)
    return {
        "id": meta["id"],
        "version": meta["version"],
        "file": path.name,
        "size": path.stat().st_size,
        "sha256": sha256_file(path),
        "payloadEntries": entry_hashes(entries),
        "license": meta["license"],
        "licenseType": meta["licenseType"],
        "repositoryUrl": meta["repositoryUrl"],
        "repositoryCommit": meta["repositoryCommit"],
        "readme": meta["readme"],
        "dependencies": meta["dependencies"],
    }


def verify_release_shape(plan: dict, records: list[dict], archives: Path, source_sha: str) -> None:
    version = plan["version"]
    by_id = {record["id"]: record for record in records}
    expected = {item["id"] for item in plan["packages"]}
    if set(by_id) != expected:
        raise CustodyError(f"archive roster mismatch: missing={sorted(expected-set(by_id))}, unexpected={sorted(set(by_id)-expected)}")
    if any(record["version"] != version for record in records):
        bad = [f"{r['id']}={r['version']}" for r in records if r["version"] != version]
        raise CustodyError(f"all 19 archives must carry {version}: {', '.join(bad)}")

    kinds = {item["id"]: item["kind"] for item in plan["packages"]}
    for record in records:
        if record["license"] != "MIT" or record["licenseType"] != "expression":
            raise CustodyError(f"{record['id']}: expected MIT license expression")
        if record["repositoryUrl"] != "https://github.com/FS-GG/FS.GG.Rendering.git":
            raise CustodyError(f"{record['id']}: missing canonical repository source metadata")
        if kinds[record["id"]] != "bom" and record["repositoryCommit"] != source_sha:
            raise CustodyError(f"{record['id']}: repository commit does not bind the archive to source {source_sha}")

    libraries = {p["id"] for p in plan["packages"] if p["kind"] == "library"}
    bom = by_id[[p["id"] for p in plan["packages"] if p["kind"] == "bom"][0]]
    bom_internal = {d["id"]: d["version"] for d in bom["dependencies"] if d["id"].startswith("FS.GG.UI.")}
    if set(bom_internal) != libraries:
        raise CustodyError(f"BOM membership mismatch: missing={sorted(libraries-set(bom_internal))}, unexpected={sorted(set(bom_internal)-libraries)}")
    wrong = {key: value for key, value in bom_internal.items() if value != f"[{version}]"}
    if wrong:
        raise CustodyError(f"BOM dependencies are not exact {version}: {wrong}")

    svg_id = plan["releaseChecks"]["svgPackage"]
    svg_path = archives / by_id[svg_id]["file"]
    _, svg_entries = nuspec(svg_path)
    for name in plan["releaseChecks"]["fableEntries"]:
        if name not in svg_entries:
            raise CustodyError(f"{svg_id}: required Fable entry missing: {name}")
    surface = svg_entries.get("api-surface/SvgBrowser.fsi", b"").decode("utf-8", errors="replace")
    for marker in plan["releaseChecks"]["svgSurfaceMarkers"]:
        if marker not in surface:
            raise CustodyError(f"{svg_id}: SVG public surface marker missing: {marker}")

    template = by_id["FS.GG.UI.Template"]
    _, template_entries = nuspec(archives / template["file"])
    for name in plan["releaseChecks"]["generatedTemplateEntries"]:
        if name not in template_entries:
            raise CustodyError(f"FS.GG.UI.Template: generated/source entry missing: {name}")


def records_from_archives(plan: dict, archives: Path, source_sha: str) -> list[dict]:
    if not archives.is_dir():
        raise CustodyError(f"archive directory does not exist: {archives}")
    paths = sorted(archives.glob("*.nupkg"))
    if len(paths) != 19:
        raise CustodyError(f"expected exactly 19 .nupkg archives, found {len(paths)}")
    records = [archive_record(path) for path in paths]
    verify_release_shape(plan, records, archives, source_sha)
    return sorted(records, key=lambda r: r["id"])


def command_prepare(args: argparse.Namespace) -> None:
    plan = load_json(args.plan)
    validate_plan(plan)
    if not re.fullmatch(r"[0-9a-f]{40}", args.source_sha):
        raise CustodyError("source SHA must be a full lowercase 40-character git commit")
    records = records_from_archives(plan, args.archives, args.source_sha)
    manifest = {
        "schema": SCHEMA,
        "source": {"repository": "https://github.com/FS-GG/FS.GG.Rendering.git", "commit": args.source_sha},
        "releasePlanSha256": sha256_file(args.plan),
        "version": plan["version"],
        "baselineVersion": plan["baselineVersion"],
        "tags": plan["tags"],
        "archives": records,
    }
    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"custody prepared: 19 immutable archives · source {args.source_sha} · manifest {canonical_digest(manifest)}")


def verified_manifest(args: argparse.Namespace) -> tuple[dict, dict]:
    plan = load_json(args.plan)
    validate_plan(plan)
    manifest = load_json(args.manifest)
    if manifest.get("schema") != SCHEMA:
        raise CustodyError("unsupported or missing custody schema")
    if manifest.get("releasePlanSha256") != sha256_file(args.plan):
        raise CustodyError("release plan digest differs from the one bound by custody")
    if args.source_sha and manifest.get("source", {}).get("commit") != args.source_sha:
        raise CustodyError("custody source SHA does not match the requested release source")
    source_sha = manifest.get("source", {}).get("commit", "")
    actual = records_from_archives(plan, args.archives, source_sha)
    if manifest.get("archives") != actual:
        raise CustodyError("retained archives differ from the custody manifest; never repack or substitute after publication starts")
    return plan, manifest


def command_verify(args: argparse.Namespace) -> None:
    _, manifest = verified_manifest(args)
    print(f"custody verified: 19 archives · version {manifest['version']} · source {manifest['source']['commit']}")


def compare_archive(local: Path, remote: Path) -> None:
    local_meta, local_entries = nuspec(local)
    remote_meta, remote_entries = nuspec(remote)
    if (local_meta["id"], local_meta["version"]) != (remote_meta["id"], remote_meta["version"]):
        raise CustodyError(f"readback identity mismatch for {local_meta['id']}")
    local_payload = entry_hashes(local_entries)
    remote_payload = entry_hashes(remote_entries)
    if local_payload != remote_payload:
        missing = sorted(set(local_payload) - set(remote_payload))
        extra = sorted(set(remote_payload) - set(local_payload))
        changed = sorted(name for name in set(local_payload) & set(remote_payload) if local_payload[name] != remote_payload[name])
        raise CustodyError(f"readback payload mismatch for {local_meta['id']}: missing={missing}, extra={extra}, changed={changed}")
    remote_signature = SIGNATURE in remote_entries
    print(f"readback match: {local_meta['id']} {local_meta['version']} · payload entries exact · feed signature={'present (excluded)' if remote_signature else 'absent'}")


def command_compare(args: argparse.Namespace) -> None:
    compare_archive(args.local, args.remote)


def render_url(template: str, record: dict) -> str:
    return template.format(
        id=record["id"],
        id_lower=record["id"].lower(),
        version=record["version"],
        filename=record["file"].lower(),
    )


def download(url: str, destination: Path, token: str) -> int:
    request = urllib.request.Request(url)
    if token:
        credential = base64.b64encode(f"x-access-token:{token}".encode()).decode()
        request.add_header("Authorization", f"Basic {credential}")
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            destination.write_bytes(response.read())
        return 200
    except urllib.error.HTTPError as exc:
        if exc.code == 404:
            return 404
        raise CustodyError(f"readback HTTP {exc.code} from {url}") from exc
    except OSError as exc:
        raise CustodyError(f"readback unavailable from {url}: {exc}") from exc


def command_probe(args: argparse.Namespace) -> None:
    _, manifest = verified_manifest(args)
    candidates = [r for r in manifest["archives"] if r["id"] == args.package_id]
    if len(candidates) != 1:
        raise CustodyError(f"package {args.package_id} is not uniquely present in custody")
    record = candidates[0]
    temp = args.archives / f".{record['file']}.readback"
    token = os.environ.get(args.token_env, "") if args.token_env else ""
    status = download(render_url(args.url_template, record), temp, token)
    if status == 404:
        print(f"readback missing: {record['id']} {record['version']}")
        raise SystemExit(4)
    try:
        compare_archive(args.archives / record["file"], temp)
    finally:
        temp.unlink(missing_ok=True)


def command_list(args: argparse.Namespace) -> None:
    _, manifest = verified_manifest(args)
    for record in manifest["archives"]:
        print(f"{record['id']}\t{record['file']}")


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser()
    sub = result.add_subparsers(dest="command", required=True)
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--plan", type=Path, required=True)
    common.add_argument("--archives", type=Path, required=True)
    common.add_argument("--manifest", type=Path, required=True)
    common.add_argument("--source-sha", default="")

    prepare = sub.add_parser("prepare", parents=[common])
    prepare.set_defaults(handler=command_prepare)
    verify = sub.add_parser("verify", parents=[common])
    verify.set_defaults(handler=command_verify)
    listing = sub.add_parser("list", parents=[common])
    listing.set_defaults(handler=command_list)
    probe = sub.add_parser("probe", parents=[common])
    probe.add_argument("--package-id", required=True)
    probe.add_argument("--url-template", required=True)
    probe.add_argument("--token-env", default="")
    probe.set_defaults(handler=command_probe)
    compare = sub.add_parser("compare")
    compare.add_argument("--local", type=Path, required=True)
    compare.add_argument("--remote", type=Path, required=True)
    compare.set_defaults(handler=command_compare)
    return result


def main() -> int:
    args = parser().parse_args()
    try:
        args.handler(args)
        return 0
    except CustodyError as exc:
        print(f"release custody refused: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
