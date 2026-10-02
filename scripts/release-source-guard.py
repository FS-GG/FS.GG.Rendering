#!/usr/bin/env python3
"""Cheap exact-source release guard. No restore, credentials or network requests."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('preflight', Path(__file__).with_name('release-preflight.py'))
preflight = importlib.util.module_from_spec(spec)
assert spec.loader
spec.loader.exec_module(preflight)


def validate(root: Path, source_sha: str, plan_path: str, requested_version: str = '', require_publication_ready: bool = False) -> dict:
    if not preflight.SHA.fullmatch(source_sha):
        preflight.fail('source identity must be an exact lowercase 40-hex SHA')
    preflight.git(root, 'cat-file', '-e', f'{source_sha}^{{commit}}')
    text = lambda path: preflight.source_text(root, source_sha, path)
    raw = text(plan_path)
    plan = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    if plan.get('schema') != 'fsgg.rendering.release-plan/v1':
        preflight.fail('unsupported release-plan schema')
    if not isinstance(plan.get('publicationReady'), bool):
        preflight.fail('successor plan must explicitly record publication readiness')
    if require_publication_ready and plan['publicationReady'] is not True:
        preflight.fail('publication pending: genuine Rendering identity and publisher grants are not joined')
    version = plan.get('version')
    if version != '0.32.0' or plan.get('baselineVersion') != '0.31.0':
        preflight.fail('successor must be 0.32.0 against the published 0.31.0 baseline')
    if requested_version and requested_version != version:
        preflight.fail(f'requested version {requested_version!r} differs from plan {version}')
    for path, pattern in [
        ('template/base/Directory.Packages.props', r'<FsGgUiVersion>([^<]+)</FsGgUiVersion>'),
        ('.template.package/FS.GG.UI.Template.fsproj', r'<Version>([^<]+)</Version>'),
    ]:
        if preflight.single_value(text(path), pattern, path) != version:
            preflight.fail(f'{path}: release axis differs from {version}')
    packages = plan.get('packages', [])
    if len(packages) != 19 or any(not isinstance(p, dict) for p in packages):
        preflight.fail('release plan must contain exactly 19 package records')
    ids = [p.get('id') for p in packages]
    if any(not isinstance(p, str) for p in ids) or len({p.casefold() for p in ids}) != 19:
        preflight.fail('release roster must contain 19 unique package IDs')
    if {p['id']: p.get('kind') for p in packages} != preflight.source_package_roster(root, source_sha):
        preflight.fail('plan roster differs from exact source packables')
    for p in packages:
        expected = 'required' if p['kind'] == 'library' else 'not-applicable'
        if p.get('apiBaseline') != expected:
            preflight.fail(f"{p['id']}: ApiCompat baseline requirement differs from package kind")
    if plan.get('tags') != [f'fs-gg-ui/v{version}', f'fs-gg-ui-template/v{version}', f'v{version}']:
        preflight.fail('release tags differ from ordered coherent triple')
    checks = plan.get('releaseChecks', {})
    if checks.get('bomMembers') != 17 or checks.get('svgPackage') != 'FS.GG.UI.Scene.SvgBrowser':
        preflight.fail('release checks omit coherent BOM or SVG package')
    # Independently derive every delivered Fable source from the curated source project.
    fable_project = text('src/Scene.SvgBrowser/Fable/FS.GG.UI.Scene.SvgBrowser.fsproj')
    entries = {'fable/' + name.replace('\\', '/').rsplit('/', 1)[-1]
               for name in re.findall(r'<Compile Include="([^"]+)"', fable_project)}
    entries |= {'fable/FS.GG.UI.Scene.SvgBrowser.fsproj', 'fable-compatibility/compatibility-profile.v1.json'}
    planned = checks.get('fableEntries', [])
    if len(planned) != len(set(planned)) or set(planned) != entries:
        preflight.fail('plan Fable entries differ from delivered source project')
    project = text('src/Scene.SvgBrowser/Scene.SvgBrowser.fsproj')
    for name in ('SvgExternalSessionHost.fs', 'SvgExternalSessionHost.fsi'):
        if name not in project or name not in fable_project or not text('src/Scene.SvgBrowser/' + name):
            preflight.fail(f'external host source/pack entry is missing: {name}')
    if 'SvgExternalSessionHost' not in checks.get('svgSurfaceMarkers', []):
        preflight.fail('plan omits external host API surface marker')
    if plan.get('sdkVersion') != '10.0.401':
        preflight.fail('release plan SDK must be exactly 10.0.401')
    if json.loads(text('global.json'))['sdk']['version'] != plan['sdkVersion']:
        preflight.fail('source global.json differs from release SDK')
    for workflow in ('.github/workflows/release.yml', '.github/workflows/release-tags.yml'):
        versions = re.findall(r'dotnet-version:\s*[\'"]?([^\s\'"]+)', text(workflow))
        if not versions or any(v != plan['sdkVersion'] for v in versions):
            preflight.fail(f'{workflow}: every release SDK setup must be exactly 10.0.401')
    recipe = text('template/product-skills/fs-gg-symbology/reference.fsx')
    pins = re.findall(r'#r "nuget: (FS.GG.UI.[^,]+), ([^"]+)"', recipe)
    if len(pins) != 4 or any(v != version for _, v in pins):
        preflight.fail('shipped symbology recipe pins differ from coherent version')
    return {'schema': 'fsgg.rendering.release-source-guard/v1', 'sourceSha': source_sha,
            'sourceTree': preflight.git(root, 'rev-parse', f'{source_sha}^{{tree}}'),
            'version': version, 'baselineVersion': plan['baselineVersion'],
            'planSha256': hashlib.sha256(raw.encode()).hexdigest(), 'rosterCount': len(ids),
            'result': 'pass', 'scope': 'source only; no publication authority'}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo-root', type=Path, default=Path.cwd())
    parser.add_argument('--source-sha', required=True)
    parser.add_argument('--plan', required=True)
    parser.add_argument('--version', default='')
    parser.add_argument('--require-publication-ready', action='store_true')
    args = parser.parse_args()
    try:
        result = validate(args.repo_root.resolve(), args.source_sha, args.plan, args.version, args.require_publication_ready)
    except (OSError, ValueError, KeyError, TypeError) as error:
        preflight.fail(f'invalid release source/plan: {error}')
    print(json.dumps(result, sort_keys=True))
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
