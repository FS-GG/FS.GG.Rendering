#!/usr/bin/env python3
"""Offline source/plan and workflow admission mutants; launches no .NET or browser."""
import importlib.util
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.dont_write_bytecode = True
SCRIPT = Path(__file__).with_name('release-source-guard.py')
spec = importlib.util.spec_from_file_location('guard', SCRIPT)
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)
ROOT = SCRIPT.parent.parent
PLAN = 'eng/release/svg-external-authority-0.32.0.json'

class SourceGuardTests(unittest.TestCase):
    def validate(self, mutate=None, version='0.32.0', sha='a'*40, require_ready=False):
        def source(_root, _sha, path):
            value = (ROOT/path).read_text()
            return mutate(path, value) if mutate else value
        def git(_root, *args):
            if args[0] == 'ls-tree':
                return '\n'.join(str(f.relative_to(ROOT)) for f in (ROOT/'src').rglob('*.fsproj')) + '\n.template.package/FS.GG.UI.Template.fsproj'
            return 'b'*40 if args[0] == 'rev-parse' else ''
        with patch.object(guard.preflight, 'source_text', side_effect=source), patch.object(guard.preflight, 'git', side_effect=git):
            return guard.validate(ROOT, sha, PLAN, version, require_ready)

    def test_valid_source_permits_qualification(self):
        self.assertEqual('pass', self.validate()['result'])

    def test_pending_authority_refuses_effect_calls_and_allows_source_only(self):
        with self.assertRaises(SystemExit) as result:
            self.validate(require_ready=True)
        self.assertIn('publication pending', str(result.exception))
        def joined(path, value):
            if path == PLAN:
                data=json.loads(value); data['publicationReady']=True; return json.dumps(data)
            return value
        self.assertEqual('pass', self.validate(joined, require_ready=True)['result'])

    def test_wrong_inputs_refuse_before_any_network_or_dotnet(self):
        for mutation in ['axis', 'template-axis', 'baseline', 'sdk', 'floating-workflow', 'missing-host', 'missing-host-plan', 'missing-surface', 'roster', 'api-baseline', 'tag', 'recipe']:
            def mutate(path, value):
                if mutation == 'axis' and path == 'template/base/Directory.Packages.props':
                    return value.replace('0.32.0', '0.4.0-preview.1')
                if mutation == 'template-axis' and path == '.template.package/FS.GG.UI.Template.fsproj':
                    return value.replace('0.32.0', '0.31.0')
                if mutation == 'floating-workflow' and path == '.github/workflows/release.yml':
                    return value.replace("dotnet-version: '10.0.401'", "dotnet-version: '10.0.x'")
                if mutation == 'missing-host' and path == 'src/Scene.SvgBrowser/SvgExternalSessionHost.fs':
                    return ''
                if mutation == 'recipe' and path == 'template/product-skills/fs-gg-symbology/reference.fsx':
                    return value.replace('0.32.0', '0.31.0')
                if path == PLAN:
                    data = json.loads(value)
                    if mutation == 'baseline': data['baselineVersion'] = '0.30.0'
                    if mutation == 'sdk': data['sdkVersion'] = '10.0.x'
                    if mutation == 'missing-host-plan': data['releaseChecks']['fableEntries'].remove('fable/SvgExternalSessionHost.fs')
                    if mutation == 'missing-surface': data['releaseChecks']['svgSurfaceMarkers'].remove('SvgExternalSessionHost')
                    if mutation == 'roster': data['packages'][0]['id'] = 'FS.GG.UI.Impostor'
                    if mutation == 'api-baseline': data['packages'][0]['apiBaseline'] = 'not-applicable'
                    if mutation == 'tag': data['tags'].reverse()
                    return json.dumps(data)
                return value
            with self.subTest(mutation=mutation), patch.object(guard.preflight, 'request', side_effect=AssertionError('network forbidden')):
                with self.assertRaises(SystemExit): self.validate(mutate)
        for version in ['0.31.0', '0.4.0-preview.1', 'v0.32.0']:
            with self.subTest(version=version), self.assertRaises(SystemExit): self.validate(version=version)
        with self.assertRaises(SystemExit): self.validate(sha='main')

    def test_workflow_gate_edges_and_source_only_effect_boundary(self):
        release = (ROOT/'.github/workflows/release.yml').read_text()
        def block(name):
            return release.split('  '+name+':\n',1)[1].split('\n  ',1)[0]
        # Literal job boundaries avoid interpreting GitHub expressions as Python.
        import re
        blocks = dict(re.findall(r'^  ([a-z-]+):\n(.*?)(?=^  [a-z-]+:|\Z)', release, re.M | re.S))
        for name in ('package-tests', 'template-product-tests', 'publication-preflight'):
            self.assertIn('needs: release-source-guard', blocks[name])
        self.assertIn('needs: [release-source-guard, package-tests, template-product-tests]', blocks['publish-packages'])
        for name in ('publication-preflight', 'publish-packages'):
            self.assertIn('!inputs.source-only', blocks[name])
        for name in ('package-tests','template-product-tests','source-package-custody','publication-preflight','publish-packages'):
            body = blocks[name]
            self.assertLess(body.index('dotnet-version:'), body.index('release-toolchain-identity.py'))
            self.assertLess(body.index('release-toolchain-identity.py'), body.index('if-no-files-found: error'))
        self.assertNotIn('packages: write', blocks['release-source-guard'])
        self.assertNotIn('NuGet/login', blocks['release-source-guard'])
        self.assertIn('--require-publication-ready', blocks['release-source-guard'])
        self.assertIn('SOURCE_ONLY', blocks['release-source-guard'])
        candidate=blocks['source-package-custody']
        self.assertIn('inputs.source-only',candidate)
        self.assertNotIn('packages: write',candidate)
        self.assertNotIn('id-token: write',candidate)
        self.assertNotIn('NuGet/login',candidate)
        self.assertIn('release-pack.sh',candidate)
        self.assertIn('rendering-source-candidate-${{ github.sha }}-0.32.0',candidate)
        self.assertIn('--baseline 0.31.0',candidate)

if __name__ == '__main__': unittest.main()
