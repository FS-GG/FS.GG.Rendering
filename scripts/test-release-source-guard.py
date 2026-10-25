#!/usr/bin/env python3
"""Offline source/plan and workflow admission mutants; launches no .NET or browser."""
import importlib.util
import hashlib
import copy
import json
from pathlib import Path
import sys
import os
import re
import subprocess
import tempfile
import unittest
import zipfile
from unittest.mock import patch
sys.dont_write_bytecode = True
SCRIPT = Path(__file__).with_name('release-source-guard-original730.py')
spec = importlib.util.spec_from_file_location('guard', SCRIPT)
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)
ROOT = SCRIPT.parent.parent
PLAN = 'eng/release/svg-external-authority-0.32.0.json'

class SourceGuardTests(unittest.TestCase):
    def validate(self, mutate=None, version='0.32.0', sha='a'*40, require_ready=False):
        def source(_root, _sha, path):
            # Historical 0.32.0 validation keeps its actual producer's delivered axes.
            # Current successor axes are independently exercised below.
            historical_axes = {'template/base/Directory.Packages.props',
                               '.template.package/FS.GG.UI.Template.fsproj',
                               'template/product-skills/fs-gg-symbology/reference.fsx'}
            value = (guard.source_bytes(ROOT, guard.PRODUCER, path).decode()
                     if path in historical_axes else (ROOT/path).read_text())
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

    def test_actual_workflow_mode_guard_allows_only_nonmutating_modes_without_readiness(self):
        release=(ROOT/'.github/workflows/release.yml').read_text()
        block=re.search(r'^  release-source-guard:\n(.*?)(?=^  [a-z-]+:)',release,re.M|re.S).group(1)
        script=block.split('        run: |\n',1)[1]
        script='\n'.join(line[10:] for line in script.splitlines() if line.strip())
        with tempfile.TemporaryDirectory() as folder:
            temp=Path(folder)
            # Capture the real workflow invocation; no candidate source, credentials or network execute.
            python=temp/'python3'
            python.write_text("#!/usr/bin/env bash\n[[ \"$1\" != - ]] || exit 0\n[[ \"$*\" != *--executor-only* ]] || exit 0\nprintf '%s\\n' \"$@\" > \"$CAPTURE\"\nfor arg in \"$@\"; do [[ \"$arg\" != --require-publication-ready ]] || exit 42; done\n")
            python.chmod(0o700)
            git=temp/'git'
            git.write_text("#!/usr/bin/env bash\nprintf '%s\\n' \"$@\" >> \"$GIT_CAPTURE\"\n")
            git.chmod(0o700)
            for source_only,preflight_only in [('',''),('false','false'),('true','false'),('false','true'),('true','true'),('TRUE','false'),('false','TRUE')]:
                with self.subTest(source_only=source_only,preflight_only=preflight_only):
                    capture=temp/'args';git_capture=temp/'git-args'
                    for path in (capture,git_capture): path.unlink(missing_ok=True)
                    env={**os.environ,'PATH':str(temp)+os.pathsep+os.environ['PATH'],
                         'GITHUB_SHA':'a'*40,'REQUESTED_SOURCE':'','INPUT_VERSION':'0.32.0','EVENT_TAG':'',
                         'SOURCE_ONLY':source_only,'PREFLIGHT_ONLY':preflight_only,'BOUND_ATTEMPT':'false',
                         'CAPTURE':str(capture),'GIT_CAPTURE':str(git_capture),'GITHUB_OUTPUT':str(temp/'outputs'),
                         'SUCCESSOR_ATTEMPT':'false'}
                    result=subprocess.run(['bash','-c',script],env=env,capture_output=True,text=True)
                    nonmutating=source_only=='true' or preflight_only=='true'
                    self.assertEqual(0 if nonmutating else 42,result.returncode,result.stderr)
                    self.assertEqual(not nonmutating,'--require-publication-ready' in capture.read_text().splitlines())
                    self.assertFalse(git_capture.exists())
            for source_only,preflight_only,requested,success in [('true','false','b'*40,False),('false','true','b'*40,True),('false','true','main',False)]:
                with self.subTest(requested=requested,preflight_only=preflight_only):
                    capture=temp/'args';git_capture=temp/'git-args'
                    for path in (capture,git_capture): path.unlink(missing_ok=True)
                    env.update(SOURCE_ONLY=source_only,PREFLIGHT_ONLY=preflight_only,REQUESTED_SOURCE=requested)
                    result=subprocess.run(['bash','-c',script],env=env,capture_output=True,text=True)
                    self.assertEqual(success,result.returncode==0,result.stderr)
                    self.assertEqual(success,capture.exists())
                    self.assertEqual(success,git_capture.exists())

    def test_readonly_dispatch_does_not_admit_pack_publish_or_tag_jobs(self):
        release=(ROOT/'.github/workflows/release.yml').read_text()
        blocks=dict(re.findall(r'^  ([a-z-]+):\n(.*?)(?=^  [a-z-]+:|\Z)',release,re.M|re.S))
        for name in ('package-tests','template-product-tests','source-package-custody','publish-packages'):
            self.assertIn('!inputs.preflight-only',blocks[name])
        self.assertIn('!inputs.source-only && (inputs.validate-only || inputs.preflight-only)',blocks['publication-preflight'])
        self.assertNotIn('dotnet nuget push',blocks['publication-preflight'])
        self.assertNotIn('git push',blocks['publication-preflight'])
        tags=(ROOT/'.github/workflows/release-tags.yml').read_text()
        self.assertIn('--source-sha "$GITHUB_SHA" --require-publication-ready',tags)
        self.assertIn('needs: [plan, validate]',tags)
        self.assertLess(tags.index('--require-publication-ready'),tags.index('actions/setup-dotnet'))

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
        self.assertIn('rendering-source-candidate-${{ github.sha }}-${{ env.SOURCE_VERSION }}',candidate)
        self.assertIn('--baseline "$SOURCE_BASELINE" --release-plan "$SOURCE_PLAN"',candidate)

class InertSuccessorTests(unittest.TestCase):
    PLAN = 'eng/release/svg-export-prefix-0.32.1.json'

    def validate(self, mutation=None, ready=False, plan_path=None):
        def source(_root, _sha, path):
            value = (ROOT/path).read_text()
            # Read the genuine historical0.32.1 axes; current source has its own release plan.
            if path in ('template/base/Directory.Packages.props', '.template.package/FS.GG.UI.Template.fsproj', 'template/product-skills/fs-gg-symbology/reference.fsx'):
                value = guard.source_bytes(ROOT, '6c9f766fdd91483c2de6f061e75589e94852a265', path).decode()
            return mutation(path, value) if mutation else value
        def git(_root, *args):
            if args[0] == 'ls-tree':
                return '\n'.join(str(f.relative_to(ROOT)) for f in (ROOT/'src').rglob('*.fsproj')) + '\n.template.package/FS.GG.UI.Template.fsproj'
            return 'b'*40 if args[0] == 'rev-parse' else ''
        with patch.object(guard.preflight, 'source_text', side_effect=source), patch.object(guard.preflight, 'git', side_effect=git):
            return guard.validate(ROOT, 'a'*40, plan_path or self.PLAN, '0.32.1', ready)

    def test_complete_successor_is_source_only(self):
        actual = self.validate()
        self.assertEqual(('0.32.1', '0.32.0', 19, 'pass'),
                         tuple(actual[k] for k in ('version', 'baselineVersion', 'rosterCount', 'result')))
        self.assertIn('no publication authority', actual['scope'])

    def test_successor_effect_and_readiness_mutants_refuse(self):
        for ready, claimed in [(True, False), (False, True), (True, True)]:
            def mutate(path, value):
                if path == self.PLAN:
                    doc = json.loads(value); doc['publicationReady'] = claimed; return json.dumps(doc)
                return value
            with self.subTest(ready=ready, claimed=claimed), self.assertRaises(SystemExit), patch.object(guard, 'acquire_selected', side_effect=AssertionError('acquisition forbidden')), patch.object(guard.preflight, 'request', side_effect=AssertionError('network forbidden')):
                self.validate(mutate, ready)

    def test_successor_wrong_tuple_axes_and_roster_refuse(self):
        for field, value in [('version', '0.32.2'), ('baselineVersion', '0.31.0'),
                             ('tags', ['v0.32.1']), ('packages', []), ('sdkVersion', '10.0.x')]:
            def mutate(path, text):
                if path == self.PLAN:
                    doc=json.loads(text); doc[field]=value; return json.dumps(doc)
                return text
            with self.subTest(field=field), self.assertRaises(SystemExit): self.validate(mutate)
        for axis in ['template/base/Directory.Packages.props', '.template.package/FS.GG.UI.Template.fsproj',
                     'template/product-skills/fs-gg-symbology/reference.fsx']:
            with self.subTest(axis=axis), self.assertRaises(SystemExit):
                self.validate(lambda path, value: value.replace('0.32.1', '0.32.0') if path == axis else value)
        with self.assertRaises(SystemExit):
            self.validate(lambda path, value: value, plan_path=PLAN)

    def test_actual_committed_successor_and_original_producer(self):
        # Real Git source reads: candidate source-only acceptance, historic original
        # acceptance, and cross-selection refusal. No restore, network or CLR.
        head = 'd3b819a71eabcc0f557bcd23677dfd4397439428'
        self.assertEqual('pass', guard.validate(ROOT, head, self.PLAN, '0.32.1')['result'])
        self.assertEqual('pass', guard.validate(ROOT, guard.PRODUCER, PLAN, '0.32.0')['result'])
        with self.assertRaises(SystemExit): guard.validate(ROOT, head, PLAN, '0.32.0')
        with self.assertRaises(SystemExit): guard.validate(ROOT, head, self.PLAN, '0.32.1', True)

    def test_actual_current_workflow_entry_admits_only_source_and_readonly_modes(self):
        head = guard.preflight.git(ROOT, 'rev-parse', 'HEAD')
        release = (ROOT/'.github/workflows/release.yml').read_text()
        block = re.search(r'^  release-source-guard:\n(.*?)(?=^  [a-z-]+:)', release, re.M|re.S).group(1)
        script = block.split('        run: |\n', 1)[1]
        script = '\n'.join(line[10:] for line in script.splitlines() if line.strip())
        with tempfile.TemporaryDirectory() as folder:
            marker = Path(folder)/'effect-reached'
            env = {**os.environ, 'GITHUB_SHA': head, 'REQUESTED_SOURCE': '',
                   'BOUND_ATTEMPT': 'false', 'SUCCESSOR_ATTEMPT': 'false', 'EVENT_TAG': '', 'EFFECT_MARKER': str(marker),
                   'GITHUB_OUTPUT': str(Path(folder)/'outputs'),
                   'FSGG_EXPECTED_EXECUTOR_SHA':head,'GITHUB_WORKFLOW_SHA':head,
                   'GITHUB_WORKFLOW_REF':'FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main',
                   'GITHUB_REPOSITORY':'FS-GG/FS.GG.Rendering','GITHUB_RUN_ID':'1','GITHUB_RUN_ATTEMPT':'1'}
            # Execute the actual shell entry and actual guard. Source/preflight may
            # pass this static boundary; publisher/tag calls must stop before it.
            for source, preflight, version, tag in [('true','false','0.32.3',''),
                    ('false','true','0.32.3',''), ('false','false','0.32.3',''),
                    ('true','true','0.32.3',''), ('false','false','','v0.32.3'),
                    ('true','false','0.32.2',''),
                    ('false','true','0.32.2',''), ('false','false','0.32.2',''),
                    ('false','false','','v0.32.2'), ('true','false','0.32.0','')]:
                marker.unlink(missing_ok=True)
                env.update(SOURCE_ONLY=source, PREFLIGHT_ONLY=preflight, INPUT_VERSION=version, EVENT_TAG=tag)
                result = subprocess.run(['bash', '-c', script+'\nprintf reached > "$EFFECT_MARKER"'],
                                        cwd=ROOT, env=env, capture_output=True, text=True, timeout=10)
                with self.subTest(source=source, preflight=preflight, version=version, tag=tag):
                    allowed = version=='0.32.3' and source=='true' and preflight=='false'
                    self.assertEqual(allowed, result.returncode==0, result.stdout+result.stderr)
                    self.assertEqual(allowed, marker.exists(), result.stdout+result.stderr)
                    if allowed:
                        self.assertEqual(['plan=eng/release/font-disposal-0.32.3.json','version=0.32.3','baseline=0.32.1'],Path(env['GITHUB_OUTPUT']).read_text().splitlines())
                    if not allowed: self.assertTrue('release-preflight:' in result.stderr+result.stdout or 'source qualification only' in result.stderr+result.stdout)
            for flag in ('BOUND_ATTEMPT','SUCCESSOR_ATTEMPT'):
                marker.unlink(missing_ok=True)
                env.update(SOURCE_ONLY='true',PREFLIGHT_ONLY='false',INPUT_VERSION='0.32.3',EVENT_TAG='',BOUND_ATTEMPT='false',SUCCESSOR_ATTEMPT='false')
                env[flag]='true'
                result=subprocess.run(['bash','-c',script+'\nprintf reached > "$EFFECT_MARKER"'],cwd=ROOT,env=env,capture_output=True,text=True,timeout=10)
                self.assertNotEqual(0,result.returncode);self.assertFalse(marker.exists())
            marker.unlink(missing_ok=True)
            tags = (ROOT/'.github/workflows/release-tags.yml').read_text()
            body = re.search(r'- name: Validate successor release source before \.NET qualification\n        run: \|\n(.*?)(?=\n      - uses:)', tags, re.S).group(1)
            command = '\n'.join(line[10:] for line in body.splitlines())
            result = subprocess.run(['bash', '-ec', command+'\nprintf reached > "$EFFECT_MARKER"'],
                                    cwd=ROOT, env=env, capture_output=True, text=True, timeout=10)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse(marker.exists())

class SuccessorRouteTests(unittest.TestCase):
    def test_selected_routes_join_without_changing_publisher_authority(self):
        release = (ROOT/'.github/workflows/release.yml').read_text()
        blocks = dict(re.findall(r'^  ([a-z-]+):\n(.*?)(?=^  [a-z-]+:|\Z)', release, re.M|re.S))
        admission = blocks['release-source-guard']
        self.assertIn('plan=eng/release/external-authority-recovery-0.32.2.json', admission)
        self.assertIn('plan=eng/release/svg-export-prefix-0.32.1.json', admission)
        self.assertIn('--plan "$plan"', admission)
        self.assertIn('[[ "$source_sha" == 6c9f766fdd91483c2de6f061e75589e94852a265 ]]', admission)
        candidate = blocks['source-package-custody']
        self.assertIn('release-pack.sh "$SOURCE_PLAN" "$GITHUB_SHA" "$SOURCE_VERSION"', candidate)
        preflight = blocks['publication-preflight']
        self.assertIn("inputs.bound-attempt && '0.32.0' || '0.32.1'", preflight)
        self.assertEqual(3, preflight.count('--plan "$RELEASE_PLAN"'))
        self.assertIn('--version "$RELEASE_BASELINE"', preflight)
        self.assertIn('fs.gg.ui.scene/$RELEASE_BASELINE/fs.gg.ui.scene.$RELEASE_BASELINE.nupkg', preflight)
        self.assertIn('git show "${{ steps.source.outputs.sha }}:$RELEASE_PLAN"', preflight)
        self.assertIn('--qualification artifacts/preflight/release-preflight.json', preflight)
        self.assertIn("FSGG_NUGET_SCOPE_ONLY: 'true'", preflight)
        self.assertIn('FSGG_NUGET_SCOPE_SOURCE: ${{ steps.source.outputs.sha }}', preflight)
        publisher = blocks['publish-packages']
        self.assertIn('!inputs.preflight-only && (inputs.bound-attempt || inputs.successor-attempt || inputs.font-disposal-attempt)', publisher)
        self.assertIn("PRODUCER_SHA: ${{ inputs.font-disposal-attempt && '4ec6eafbbbf02264db9b0db4780c590abe0bf9cd' || inputs.successor-attempt && '9b73395658d4683a067d150ca9441b04e802c4af' || '6c9f766fdd91483c2de6f061e75589e94852a265' }}", publisher)
        self.assertIn('plan=eng/release/svg-export-prefix-0.32.1.json', publisher)
        self.assertIn('selected=(--attempt-binding)', publisher)
        self.assertNotIn('release-pack.sh', publisher)
        script = (ROOT/'scripts/release-nuget-verify-key.fsx').read_text()
        self.assertIn('not successor', script)
        self.assertLess(script.index('not successor'), script.index('GetEnvironmentVariable("NUGET_API_KEY")'))
        self.assertLess(script.index('scopeSourceFromReceipt'), script.index('GetEnvironmentVariable("NUGET_API_KEY")'))
        self.assertLess(script.index('if scopeOnly then exit 0'), script.index('GetEnvironmentVariable("GITHUB_TOKEN")'))
        self.assertIn('producerSha=(if scopeOnly then null else producer)', script)
        self.assertIn('else "6c9f766fdd91483c2de6f061e75589e94852a265"', script)
        self.assertIn('sourceSha=scopeSource', script)
        self.assertEqual(2, release.count('dotnet fsi --define:DEBUG --exec scripts/test-release-nuget-verify-key.fsx'))
        self.assertNotIn('dotnet fsi --exec scripts/test-release-nuget-verify-key.fsx', release)

class SelectedAttemptTests(unittest.TestCase):
    def binding(self, mutate=None, ready=False):
        value=json.loads((ROOT/guard.ATTEMPT).read_text())
        value['attemptReady']=False
        value['qualification']['status']='qualified-source-candidate'
        if ready:
            value['attemptReady']=True
            value['qualification'].update(status='qualified',callerSha='a'*40,run=1,artifact=2,outerSha256='b'*64,outerSizeBytes=100)
        if mutate: mutate(value)
        return value

    def validate(self, value, require_ready=True, source=guard.PRODUCER):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);path=root/guard.ATTEMPT;path.parent.mkdir(parents=True);path.write_text(json.dumps(value))
            plan=guard.source_bytes(ROOT,guard.PRODUCER,'eng/release/svg-external-authority-0.32.0.json')
            with patch.object(guard,'source_bytes',return_value=plan):
                return guard.validate_attempt(root,source,require_ready)

    def test_unknown_binding_allows_read_inspection_only(self):
        self.assertFalse(self.validate(self.binding(),False)['attemptReady'])
        with self.assertRaises(SystemExit):self.validate(self.binding())
        self.assertTrue(self.validate(self.binding(ready=True))['attemptReady'])

    def test_original_producer_wrong_version_refuses_before_acquisition(self):
        with patch.object(guard, 'acquire_selected', side_effect=AssertionError('archive acquisition forbidden')), patch.object(guard.preflight, 'request', side_effect=AssertionError('network forbidden')):
            for version in ['0.31.0', '0.33.0', 'v0.32.0']:
                with self.subTest(version=version), self.assertRaises(SystemExit) as failure:
                    guard.validate(ROOT, guard.PRODUCER, PLAN, version)
                self.assertIn('requested version', str(failure.exception))
            with self.assertRaises(SystemExit):
                self.validate(self.binding(ready=True), source='a'*40)

    def test_wrong_selected_tuple_or_missing_native_proof_refuses(self):
        for key,value in [('producerSha','a'*40),('producerArtifact',0),('producerRun',0),('outerSha256','0'*64),('outerSizeBytes',1),('custodySha256','0'*64),('planSha256','0'*64),('version','0.31.0'),('githubEffectiveWrite','proven')]:
            with self.subTest(key=key),self.assertRaises(SystemExit):self.validate(self.binding(lambda doc:doc.update({key:value}),True))
        for key,value in [('status','unknown'),('templatesSha','main'),('callerSha',None),('run',None),('artifact',None),('outerSha256',None),('outerSizeBytes',None)]:
            with self.subTest(key=key),self.assertRaises(SystemExit):self.validate(self.binding(lambda doc:doc['qualification'].update({key:value}),True))
        with self.assertRaises(SystemExit):self.validate(self.binding(ready=True),source='a'*40)

    def test_actual_publisher_workflow_preserves_identity_order_and_no_regeneration(self):
        gate=(ROOT/'.github/workflows/gate.yml').read_text()
        api_gate=gate.split('  api-compatibility-gate:',1)[1].split('\n  # #241',1)[0]
        immutable_fetch='git fetch --no-tags --depth=1 origin '+guard.PRODUCER
        self.assertLess(api_gate.index(immutable_fetch),api_gate.index('python3 scripts/test-release-source-guard.py'))
        text=(ROOT/'.github/workflows/release.yml').read_text();publisher=text.split('  publish-packages:',1)[1]
        self.assertNotIn('release-pack.sh',publisher);self.assertNotIn('locked-restore',publisher)
        self.assertNotIn('actions/artifacts?name=',publisher);self.assertNotIn('sort_by(.created_at)',publisher)
        self.assertNotIn('--source-sha "$GITHUB_SHA"',publisher)
        self.assertIn('!inputs.preflight-only && (inputs.bound-attempt || inputs.successor-attempt || inputs.font-disposal-attempt)',publisher)
        self.assertLess(text.index('--source-sha "$source_sha" --version "$version"'), text.index('--acquire-bound-artifacts'))
        self.assertLess(publisher.index('--acquire-bound-artifacts'), publisher.index('Verify the version to publish is the version the guard validated'))
        self.assertLess(publisher.index('Verify the version to publish is the version the guard validated'), publisher.index('Verify custody before any release-shaped check'))
        self.assertLess(publisher.index('Verify custody before any release-shaped check'), publisher.index('dotnet nuget push'))
        self.assertIn('test "$(git rev-parse "$tag^{commit}")" = "$PRODUCER_SHA"',publisher)
        self.assertLess(publisher.index('--acquire-bound-artifacts'),publisher.index('NuGet login (OIDC'))
        self.assertLess(publisher.index('release-nuget-verify-key.fsx'),publisher.index('Replay original custody bytes to GitHub'))
        self.assertLess(publisher.index('Replay original custody bytes to GitHub'),publisher.index('Replay the same original custody bytes to nuget.org'))
        self.assertIn('trap \'record interrupted failed\' ERR',publisher)
        self.assertIn('Always retain original bytes',publisher)
        self.assertIn("if: always()",publisher)
        self.assertNotIn('< <(python3 scripts/release-custody.py list',publisher)
        for body in [publisher.split('Replay original custody bytes to GitHub',1)[1],publisher.split('Replay the same original custody bytes',1)[1]]:
            self.assertLess(body.index('probe'),body.index('dotnet nuget push'))
            self.assertIn('[[ "$rc" == 4 ]]',body)

    def test_fresh_fsi_probe_config_keeps_public_dependencies_and_original_staging(self):
        import xml.etree.ElementTree as ET
        text=(ROOT/'.github/workflows/release.yml').read_text()
        step=text.split('      - name: Packed template clean-checkout FSI contract (#1010)',1)[1].split('      - name:',1)[0]
        script=step.split('        run: |\n',1)[1]
        script='\n'.join(line[10:] for line in script.splitlines())
        config_block=script.split('user_config="$HOME/.nuget/NuGet/NuGet.Config"',1)[1].split('name="FsiContractProbe"',1)[0]
        with tempfile.TemporaryDirectory() as folder:
            temp=Path(folder);config=temp/'NuGet.Config';capture=temp/'capture'
            helper=temp/'dotnet'
            helper.write_text('#!/usr/bin/env bash\nprintf "%s\\n" "$@" > "$CAPTURE"\n')
            helper.chmod(0o700)
            env={**os.environ,'PATH':str(temp)+os.pathsep+os.environ['PATH'],'CAPTURE':str(capture)}
            command='user_config='+str(config)+'\n'+config_block
            result=subprocess.run(['bash','-c',command],env=env,capture_output=True,text=True,timeout=5)
            self.assertEqual(0,result.returncode,result.stderr)
            sources=ET.fromstring(config.read_text()).find('packageSources')
            self.assertEqual({'nuget.org':'https://api.nuget.org/v3/index.json'},{entry.get('key'):entry.get('value') for entry in sources})
            args=capture.read_text().splitlines()
            self.assertEqual(['nuget','add','source'],args[:3])
            self.assertTrue(args[3].endswith('/artifacts/packages'))
            self.assertIn('release-staging',args)
            self.assertIn(str(config),args)
            existing='<configuration><packageSources><add key="custom" value="/owned/feed" /></packageSources></configuration>'
            config.write_text(existing)
            result=subprocess.run(['bash','-c',command],env=env,capture_output=True,text=True,timeout=5)
            self.assertEqual(0,result.returncode,result.stderr)
            self.assertEqual(existing,config.read_text())

    def test_public_probe_has_its_own_shell_diagnostic_function(self):
        text=(ROOT/'.github/workflows/release.yml').read_text()
        step=text.split('      - name: Verify fresh existing-ID NuGet scope and complete read census before first writer',1)[1].split('      - name:',1)[0]
        script=step.split('        run: |\n',1)[1]
        script='\n'.join(line[10:] for line in script.splitlines())
        # Each workflow step runs a separate shell; functions in later writer steps cannot help.
        prefix=script.split('while IFS=',1)[0]
        function=next(line for line in prefix.splitlines() if line.startswith('record()'))
        with tempfile.TemporaryDirectory() as folder:
            temp=Path(folder); helper=temp/'python3';capture=temp/'captured'
            helper.write_text('#!/usr/bin/env bash\nprintf "%s\\n" "$@" > "$CAPTURE"\n')
            helper.chmod(0o700)
            result=subprocess.run(['bash','-c',function+'\nid=FS.GG.UI.Scene\nrecord probe requested'],env={**os.environ,'PATH':str(temp)+os.pathsep+os.environ['PATH'],'CAPTURE':str(capture)},capture_output=True,text=True,timeout=5)
            self.assertEqual(0,result.returncode,result.stderr)
            self.assertEqual(['scripts/release-source-guard.py','--record-event','--event-feed','nuget','--event-package','FS.GG.UI.Scene','--event-stage','probe','--event-result','requested'],capture.read_text().splitlines())

    def test_verify_key_secret_and_redirect_boundaries(self):
        helper=(ROOT/'scripts/NuGetVerifyKey.fs').read_text();wrapper=(ROOT/'scripts/release-nuget-verify-key.fsx').read_text()
        self.assertIn('AllowAutoRedirect = false',helper);self.assertIn('HttpCompletionOption.ResponseHeadersRead',helper)
        self.assertIn('X-NuGet-ApiKey',helper);self.assertIn('TimeSpan.FromSeconds 10.',helper)
        self.assertIn('ids.Length <> 19',helper);self.assertIn('baseline <> "0.31.0"',helper)
        self.assertIn('Environment.GetEnvironmentVariable("NUGET_API_KEY")',wrapper)
        self.assertNotIn('eprintfn "%A"',wrapper);self.assertNotIn('printfn "%s" key',wrapper)
        self.assertIn('noPackageVersionMutation=true',wrapper)
        self.assertIn('| None -> admit facts',wrapper)
        self.assertIn('| Some journey -> admitCurrent journey',wrapper)

    def test_exact_fsharp_publisher_indices_and_job_scope_mutants(self):
        fixture=(ROOT/'tests/Package.Tests/Feature209VersionCoherenceTests.fs').read_text().split('test "release.yml: publish-packages binds',1)[1].split('// #517',1)[0]
        pattern=re.search(r'Regex.Match\(yml, @"([^"]+)"\)',fixture).group(1).replace(r'\z',r'\Z')
        def literal(name):
            encoded=re.search(r'let '+name+r' = (?:idx |yml.IndexOf\()("(?:\\.|[^"\\])*")',fixture).group(1)
            return json.loads(encoded)
        def evaluate(text):
            job=re.search(pattern,text);self.assertIsNotNone(job)
            publisher=job.group(0)
            values={name:publisher.index(literal(name)) for name in ['verify','acquisition','custody','firstPush']}
            source=text.index(literal('sourceGuard'))
            last=publisher.rindex('dotnet nuget push')
            self.assertLess(source,job.start()+values['acquisition'])
            self.assertLess(values['acquisition'],values['verify'])
            self.assertLess(values['verify'],values['custody'])
            self.assertLess(values['custody'],values['firstPush']);self.assertLess(values['custody'],last)
            verify_step=publisher[values['verify']:]
            self.assertIn('.template.package/FS.GG.UI.Template.fsproj',verify_step)
            self.assertIn("steps.ver.outputs.push == 'true'",verify_step)
            self.assertIn('--source-sha "$PRODUCER_SHA"',publisher)
            self.assertNotIn('dotnet pack',publisher);self.assertNotIn('release-pack.sh',publisher)
            return values
        text=(ROOT/'.github/workflows/release.yml').read_text()
        values=evaluate(text)
        duplicate='  earlier-fixture-job:\n    runs-on: ubuntu-latest\n    steps:\n'+literal('verify')+'\n        run: true\n'
        def earlier_job(value):return value.replace('  publish-packages:\n',duplicate+'  publish-packages:\n',1)
        self.assertEqual(values,evaluate(earlier_job(text)))
        verify=text.index(literal('verify'));custody=text.index(literal('custody'))
        end=text.index('      - name: Retain original release bytes before any feed mutation',custody)
        removed=text[:verify]+text[custody:]
        reversed_steps=text[:verify]+text[custody:end]+text[verify:custody]+text[end:]
        for label,mutant in [('removed-publisher-guard',removed),('reversed-version-custody',reversed_steps)]:
            with self.subTest(label=label),self.assertRaises((AssertionError,ValueError)):
                evaluate(earlier_job(mutant))

    def test_partial_observations_sanitize_and_preserve_unknown(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)
            with patch.object(guard.preflight,'source_text',return_value=(ROOT/'eng/release/svg-external-authority-0.32.0.json').read_text()):
                guard.record_event(root,'github','FS.GG.UI.Scene','push','requested')
                guard.record_event(root,'github','FS.GG.UI.Scene','interrupted','failed')
                with self.assertRaises(SystemExit):guard.record_event(root,'github','secret-or-url','push','requested')
            events=[json.loads(line) for line in (root/'artifacts/attempt/publisher-events.jsonl').read_text().splitlines()]
            self.assertEqual(['requested','failed'],[row['outcome'] for row in events])
            self.assertTrue(all(row['producerSha']==guard.PRODUCER for row in events))
            self.assertFalse(any(row['outcome']=='absent' for row in events))

class NativeProofControls(unittest.TestCase):
    binding = SelectedAttemptTests.binding
    def proof(self):
        binding=self.binding(ready=True);q=binding['qualification']
        feed=Path('/home/developer/.local/share/fs-gg-private/rendering-source730-candidate-20261002')
        # In portable CI the fixture contains no actual packages; only these byte identities matter here.
        if not feed.exists():
            self.temp=tempfile.TemporaryDirectory();feed=Path(self.temp.name)
            for name in ['FS.GG.UI.Scene.0.32.0.nupkg','FS.GG.UI.KeyboardInput.0.32.0.nupkg','FS.GG.UI.Scene.SvgBrowser.0.32.0.nupkg']:(feed/name).write_bytes(b'package-payload-fixture')
        selected=['FS.GG.UI.Scene.0.32.0.nupkg','FS.GG.UI.KeyboardInput.0.32.0.nupkg','FS.GG.UI.Scene.SvgBrowser.0.32.0.nupkg']
        rows=[{'name':name,'sha256':hashlib.sha256((feed/name).read_bytes()).hexdigest()} for name in selected]
        browser=json.dumps({'stats':{'expected':4,'unexpected':0,'skipped':0,'flaky':0}}).encode()
        files={family+'-browser.json':browser for family in ['chromium','firefox','webkit']}
        files['provider-composition.log']=b'PASS actual-fixture\n'*180
        result={'disposition':'passed','templates':{'revision':q['templatesSha']},'producer':{'revision':guard.PRODUCER,'custodyManifestSha256':guard.CUSTODY,'archives':rows},'browserFamilies':['chromium','firefox','webkit'],'passedPerFamily':4,'skipped':0,'browserReportSha256':{family:hashlib.sha256(browser).hexdigest() for family in ['chromium','firefox','webkit']},'nativePreflight':{'requestedPhase':'full','caller':{'repository':'FS-GG/FS.GG.Rendering','revision':q['callerSha'],'run':str(q['run'])}}}
        return binding,feed,files,result

    def check_proof(self,binding,feed,files,result,caller=None):
        q=binding['qualification'];files={**files,'qualification.json':json.dumps(result).encode()}
        caller=caller or ('    uses: FS-GG/FS.GG.Templates/.github/workflows/fable-external-reference-source.yml@'+q['templatesSha']+'\n      templates-source: '+q['templatesSha']+'\n')
        with patch.object(guard.preflight,'source_text',return_value=caller):return guard.validate_qualification_payload(ROOT,binding,files.__getitem__,feed)

    def test_full_native_proof_join_and_refusals(self):
        binding,feed,files,result=self.proof()
        self.assertEqual(([4,4,4],180),self.check_proof(binding,feed,files,result))
        for name in ['preflight','template','producer','custody','families','archive','phase','caller-run','report-hash']:
            bad=copy.deepcopy(result)
            if name=='preflight':bad['disposition']='preflight-passed'
            if name=='template':bad['templates']['revision']='b'*40
            if name=='producer':bad['producer']['revision']='b'*40
            if name=='custody':bad['producer']['custodyManifestSha256']='0'*64
            if name=='families':bad['browserFamilies'].pop()
            if name=='archive':bad['producer']['archives'][0]['sha256']='0'*64
            if name=='phase':bad['nativePreflight']['requestedPhase']='preflight'
            if name=='caller-run':bad['nativePreflight']['caller']['run']='999'
            if name=='report-hash':bad['browserReportSha256']['webkit']='0'*64
            with self.subTest(name=name),self.assertRaises(SystemExit):self.check_proof(binding,feed,files,bad)
        with self.assertRaises(SystemExit):self.check_proof(binding,feed,{**files,'provider-composition.log':b'PASS fewer\n'*179},result)
        with self.assertRaises(SystemExit):self.check_proof(binding,feed,files,result,caller='    uses: moving@main\n')

    def test_native_metadata_refuses_before_archive_download(self):
        binding=self.binding(ready=True)
        good_run={'id':binding['producerRun'],'head_sha':guard.PRODUCER,'conclusion':'success','path':'.github/workflows/release.yml'}
        good_artifact={'id':binding['producerArtifact'],'expired':False,'workflow_run':{'id':binding['producerRun'],'head_sha':guard.PRODUCER},'digest':'sha256:'+guard.OUTER,'size_in_bytes':14790371}
        for name in ['source','conclusion','path','expired','id','digest','size','run-join']:
            run=copy.deepcopy(good_run);artifact=copy.deepcopy(good_artifact)
            if name=='source':run['head_sha']='a'*40
            if name=='conclusion':run['conclusion']='failure'
            if name=='path':run['path']='different.yml'
            if name=='expired':artifact['expired']=True
            if name=='id':artifact['id']=0
            if name=='digest':artifact['digest']='sha256:'+'0'*64
            if name=='size':artifact['size_in_bytes']=1
            if name=='run-join':artifact['workflow_run']['id']=0
            def command(args,**kwargs):
                self.assertEqual(['gh','api'],args[:2]);self.assertNotIn('/zip',args[-1])
                return json.dumps(run if '/runs/' in args[-1] else artifact).encode()
            with tempfile.TemporaryDirectory() as d,patch.dict(os.environ,{'GITHUB_REPOSITORY':'FS-GG/FS.GG.Rendering'}),patch.object(guard.subprocess,'check_output',side_effect=command),self.subTest(name=name),self.assertRaises(SystemExit):
                guard.acquire_selected(Path(d),binding)

class ActualPublisherReplayControls(unittest.TestCase):
    def test_actual_github_loop_refusal_and_same_byte_resume_order(self):
        text=(ROOT/'.github/workflows/release.yml').read_text()
        match=re.search(r'      - name: Replay original custody bytes to GitHub Packages and read them back\n.*?        run: \|\n(.*?)(?=\n      # Dual-publish)',text,re.S)
        self.assertIsNotNone(match)
        script='\n'.join(line[10:] for line in match[1].splitlines())
        ids=[p['id'] for p in json.loads((ROOT/'eng/release/svg-external-authority-0.32.0.json').read_text())['packages']]
        with tempfile.TemporaryDirectory() as d:
            temp=Path(d);bin_dir=temp/'bin';bin_dir.mkdir();attempt=temp/'artifacts/attempt';attempt.mkdir(parents=True)
            (attempt/'roster.tsv').write_text(''.join(id+'\t'+id+'.0.32.0.nupkg\n' for id in ids))
            python=bin_dir/'python3'
            python.write_text("#!"+sys.executable+"\nimport os,sys,json\nfrom pathlib import Path\nargs=sys.argv[1:]\nif args[0]=='-':os.execv('/usr/bin/python3',['python3',*args])\nmode=os.environ['FIXTURE_MODE']\nkind=Path(args[0]).name; command=args[1]\nwith open(os.environ['CAPTURE'],'a') as f:f.write(kind+' '+command+' '+(' '.join(args[2:]) if command=='--record-event' else '')+'\\n')\nif kind=='nuget-client-archive.py' and command=='probe':sys.exit(0 if mode in ['present','mismatch'] else 3 if mode=='refused' else 4)\nif kind=='release-custody.py' and command=='compare' and mode=='mismatch':sys.exit(3)\nsys.exit(0)\n")
            python.chmod(0o700)
            # Command fixture only: never starts a real dotnet executable or writes a package.
            dotnet=bin_dir/'dotnet'
            dotnet.write_text("#!"+sys.executable+"\nimport os,sys\nwith open(os.environ['CAPTURE'],'a') as f:f.write('native-push-request\\n')\nsys.exit(7 if os.environ['FIXTURE_MODE']=='push-failed' else 0)\n")
            dotnet.chmod(0o700)
            for mode,expected,pushes in [('present',0,0),('absent',0,19),('refused',3,0),('mismatch',3,0),('push-failed',7,1),('occupied-invisible',1,0)]:
                with self.subTest(mode=mode):
                    capture=temp/'commands';capture.write_text('')
                    verdict='OCCUPIED' if mode=='occupied-invisible' else 'ABSENT'
                    (attempt/'fresh-package-census.json').write_text(json.dumps({'packages':[{'id':id,'githubPackages':{'verdict':verdict}} for id in ids]}))
                    env={**os.environ,'PATH':str(bin_dir)+os.pathsep+os.environ['PATH'],'CAPTURE':str(capture),'FIXTURE_MODE':mode,'VER':'0.32.0','PRODUCER_PLAN':'fixed-plan','PRODUCER_SHA':guard.PRODUCER,'GITHUB_ACTOR':'fixture','GITHUB_TOKEN':'public-fixture-value'}
                    result=subprocess.run(['bash','-c',script],cwd=temp,env=env,text=True,capture_output=True,timeout=20)
                    self.assertEqual(expected,result.returncode,result.stderr)
                    commands=capture.read_text().splitlines();self.assertEqual(pushes,commands.count('native-push-request'))
                    if pushes:
                        requested=next(i for i,line in enumerate(commands) if '--event-stage push --event-result requested' in line)
                        self.assertLess(requested,commands.index('native-push-request'))
                    if mode=='push-failed':
                        self.assertTrue(any('--event-stage interrupted --event-result failed' in line for line in commands))
                        self.assertFalse(any('--event-stage push --event-result acknowledged' in line for line in commands))

class ExecutorFenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        spec = importlib.util.spec_from_file_location('successor_guard', ROOT/'scripts/release-source-guard.py')
        cls.subject = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(cls.subject)

    def environment(self):
        return {'FSGG_EXPECTED_EXECUTOR_SHA':'a'*40,'GITHUB_SHA':'a'*40,
                'GITHUB_WORKFLOW_SHA':'a'*40,'GITHUB_REPOSITORY':'FS-GG/FS.GG.Rendering',
                'GITHUB_WORKFLOW_REF':'FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main',
                'GITHUB_RUN_ID':'10','GITHUB_RUN_ATTEMPT':'1'}

    def test_executor_join_and_every_identity_mutant(self):
        subject=self.subject
        env=self.environment()
        with patch.dict(os.environ,env,clear=True), patch.object(subject.preflight,'git',return_value='a'*40):
            proof=subject.executor_identity(ROOT)
            self.assertEqual('a'*40,proof['expectedExecutorSha'])
            self.assertEqual('10',proof['run'])
        for field, value in [('FSGG_EXPECTED_EXECUTOR_SHA',''),('FSGG_EXPECTED_EXECUTOR_SHA','A'*40),
                ('FSGG_EXPECTED_EXECUTOR_SHA','b'*40),('GITHUB_SHA','b'*40),('GITHUB_WORKFLOW_SHA','b'*40),
                ('GITHUB_WORKFLOW_REF','FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/other'),
                ('GITHUB_REPOSITORY','fork/repo'),('GITHUB_RUN_ID','0'),('GITHUB_RUN_ATTEMPT','')]:
            with self.subTest(field=field,value=value),patch.dict(os.environ,{**env,field:value},clear=True),patch.object(subject.preflight,'git',return_value='a'*40),self.assertRaises(SystemExit):
                subject.executor_identity(ROOT)
        with patch.dict(os.environ,env,clear=True),patch.object(subject.preflight,'git',return_value='b'*40),self.assertRaises(SystemExit):
            subject.executor_identity(ROOT)

    def test_original_guard_and_successor_readiness_are_independent(self):
        self.assertEqual('eef83507eadf5dbcafd0e2dfbce40d39ed4a8e52b9e7e9793b9bc5610e2fb8bd',hashlib.sha256(SCRIPT.read_bytes()).hexdigest())
        binding=json.loads((ROOT/self.subject.ATTEMPT).read_text())
        self.assertTrue(binding['attemptReady'])
        self.assertTrue(self.subject.validate_attempt(ROOT,self.subject.PRODUCER)['attemptReady'])
        original_plan=self.subject.source_bytes(ROOT,self.subject.PRODUCER,binding['planPath'])
        self.assertFalse(json.loads(original_plan)['publicationReady'])
        pending=copy.deepcopy(binding);pending['attemptReady']=False
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);target=root/self.subject.ATTEMPT;target.parent.mkdir(parents=True);target.write_text(json.dumps(pending))
            with patch.object(self.subject,'source_bytes',return_value=original_plan):
                self.assertFalse(self.subject.validate_attempt(root,self.subject.PRODUCER,False)['attemptReady'])
                with self.assertRaises(SystemExit):self.subject.validate_attempt(root,self.subject.PRODUCER)
        for field,value in [('producerAttempt',2),('producerRun',1),('producerArtifact',1),
                            ('outerSha256','0'*64),('custodySha256','0'*64)]:
            changed=copy.deepcopy(binding);changed[field]=value
            with tempfile.TemporaryDirectory() as folder:
                root=Path(folder);target=root/self.subject.ATTEMPT;target.parent.mkdir(parents=True);target.write_text(json.dumps(changed))
                with patch.object(self.subject,'source_bytes',return_value=self.subject.source_bytes(ROOT,self.subject.PRODUCER,binding['planPath'])),self.assertRaises(SystemExit):
                    self.subject.validate_attempt(root,self.subject.PRODUCER,False)
        for field,value in [('attempt',2),('run',1),('callerSha','a'*40),('templatesSha','b'*40),('qualificationSha256','0'*64)]:
            changed=copy.deepcopy(binding);changed['qualification'][field]=value
            with tempfile.TemporaryDirectory() as folder:
                root=Path(folder);target=root/self.subject.ATTEMPT;target.parent.mkdir(parents=True);target.write_text(json.dumps(changed))
                with patch.object(self.subject,'source_bytes',return_value=self.subject.source_bytes(ROOT,self.subject.PRODUCER,binding['planPath'])),self.assertRaises(SystemExit):
                    self.subject.validate_attempt(root,self.subject.PRODUCER,False)

    def test_extracted_credential_job_fence_stops_before_authority(self):
        release=(ROOT/'.github/workflows/release.yml').read_text()
        head=guard.preflight.git(ROOT,'rev-parse','HEAD')
        env={**os.environ,**self.environment(),'FSGG_EXPECTED_EXECUTOR_SHA':head,
             'GITHUB_SHA':head,'GITHUB_WORKFLOW_SHA':head}
        for job in ('publication-preflight','publish-packages'):
            block=re.search(r'^  '+job+r':\n.*?(?=^  [a-z-]+:|\Z)',release,re.M|re.S).group()
            fence='python3 scripts/release-source-guard.py --executor-only'
            self.assertLess(block.index(fence),block.index('actions/setup-dotnet'))
            self.assertLess(block.index('Recheck executor before OIDC'),block.index('uses: NuGet/login'))
            for expected,ok in [(head,True),('',False),('b'*40,False)]:
                with tempfile.TemporaryDirectory() as folder:
                    marker=Path(folder)/'authority-reached'
                    result=subprocess.run(['bash','-ec',fence+'; printf reached > "$MARKER"'],cwd=ROOT,
                        env={**env,'FSGG_EXPECTED_EXECUTOR_SHA':expected,'MARKER':str(marker)},capture_output=True,text=True,timeout=10)
                    self.assertEqual(ok,result.returncode==0,result.stderr)
                    self.assertEqual(ok,marker.exists())
            if job=='publish-packages':
                self.assertEqual(2,block.count('              python3 scripts/release-source-guard.py --executor-only\n              dotnet nuget push'))
        boundary=(ROOT/'scripts/release-nuget-verify-key.fsx').read_text()
        self.assertLess(boundary.index('joinExecutor executorFacts.RootElement'),boundary.index('GetEnvironmentVariable("NUGET_API_KEY")'))
        self.assertLess(boundary.index('joinExecutor inputs.RootElement'),boundary.index('inspect (httpTransport key)'))


class RecoverySuccessorTests(unittest.TestCase):
    PLAN = 'eng/release/external-authority-recovery-0.32.2.json'

    def validate(self, mutation=None, require_ready=False):
        spec = importlib.util.spec_from_file_location('recovery_guard', ROOT/'scripts/release-source-guard.py')
        current_guard = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(current_guard)
        def source(_root, _sha, path):
            value = (ROOT / path).read_text()
            if path in ('template/base/Directory.Packages.props', '.template.package/FS.GG.UI.Template.fsproj', 'template/product-skills/fs-gg-symbology/reference.fsx'):
                value = guard.source_bytes(ROOT, '9b73395658d4683a067d150ca9441b04e802c4af', path).decode()
            return mutation(path, value) if mutation else value
        def git(_root, *args):
            if args[0] == 'ls-tree':
                return '\n'.join(str(f.relative_to(ROOT)) for f in (ROOT/'src').rglob('*.fsproj')) + '\n.template.package/FS.GG.UI.Template.fsproj'
            return 'b'*40 if args[0] == 'rev-parse' else ''
        with patch.object(current_guard.preflight, 'source_text', side_effect=source), patch.object(current_guard.preflight, 'git', side_effect=git):
            return current_guard.validate(ROOT, 'a'*40, self.PLAN, '0.32.2', require_ready)

    def test_exact_source_only_coherent_successor(self):
        actual = self.validate()
        self.assertEqual(('0.32.2', '0.32.1', 19), tuple(actual[k] for k in ('version', 'baselineVersion', 'rosterCount')))
        self.assertIn('no publication authority', actual['scope'])

    def test_readiness_and_effects_still_refuse(self):
        with self.assertRaises(SystemExit):
            self.validate(require_ready=True)
        def forged_ready(path, text):
            if path == self.PLAN:
                doc = json.loads(text); doc['publicationReady'] = True; return json.dumps(doc)
            return text
        with self.assertRaises(SystemExit):
            self.validate(forged_ready)

    def test_wrong_baseline_and_axes_refuse(self):
        def wrong_baseline(path, text):
            if path == self.PLAN:
                doc = json.loads(text); doc['baselineVersion'] = '0.32.0'; return json.dumps(doc)
            return text
        with self.assertRaises(SystemExit):
            self.validate(wrong_baseline)
        for axis in ('template/base/Directory.Packages.props', '.template.package/FS.GG.UI.Template.fsproj', 'template/product-skills/fs-gg-symbology/reference.fsx'):
            with self.subTest(axis=axis), self.assertRaises(SystemExit):
                self.validate(lambda path, text: text.replace('0.32.2', '0.32.1') if path == axis else text)

    def test_historical_publisher_is_not_rebound(self):
        current = (ROOT/'.github/workflows/release.yml').read_text()
        publisher = re.search(r'^  publish-packages:\n(.*?)(?=^  [a-z-]+:|\Z)', current, re.M|re.S).group(1)
        self.assertIn('inputs.bound-attempt', publisher)
        self.assertIn("PRODUCER_SHA: ${{ inputs.font-disposal-attempt && '4ec6eafbbbf02264db9b0db4780c590abe0bf9cd' || inputs.successor-attempt && '9b73395658d4683a067d150ca9441b04e802c4af' || '6c9f766fdd91483c2de6f061e75589e94852a265' }}", publisher)
        self.assertIn('plan=eng/release/svg-export-prefix-0.32.1.json', publisher)
        self.assertIn('selected=(--attempt-binding)', publisher)
        self.assertIn('selected=(--successor-attempt)', publisher)
        self.assertNotIn('release-pack.sh', publisher)



class CurrentRetainedAttemptTests(unittest.TestCase):
    def setUp(self):
        spec=importlib.util.spec_from_file_location('current_retained_guard',ROOT/'scripts/release-source-guard.py')
        self.guard=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.guard)
        self.binding=json.loads((ROOT/self.guard.SUCCESSOR_ATTEMPT).read_text())
        # Preserve the original pending-state refusal controls after genuine qualification joins.
        self.binding['attemptReady']=False
        self.binding['qualification']={key:('pending' if key=='status' else None) for key in self.binding['qualification']}

    def test_pending_original_binding_allows_only_preparation(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);path=root/self.guard.SUCCESSOR_ATTEMPT;path.parent.mkdir(parents=True)
            path.write_text(json.dumps(self.binding))
            with patch.object(subprocess,'check_output',side_effect=AssertionError('no acquisition')):
                with patch.object(self.guard,'source_bytes',return_value=(ROOT/self.guard.SUCCESSOR['planPath']).read_bytes()):
                    self.assertFalse(self.guard.validate_successor_attempt(root,self.guard.SUCCESSOR['producerSha'],False)['attemptReady'])
                    with self.assertRaises(SystemExit):self.guard.validate_successor_attempt(root,self.guard.SUCCESSOR['producerSha'],True)

    def test_closed_candidate_and_unknown_receiver_mutants(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);path=root/self.guard.SUCCESSOR_ATTEMPT;path.parent.mkdir(parents=True)
            def inspect(binding,ready=False):
                path.write_text(json.dumps(binding))
                with patch.object(self.guard,'source_bytes',return_value=(ROOT/self.guard.SUCCESSOR['planPath']).read_bytes()):
                    return self.guard.validate_successor_attempt(root,self.guard.SUCCESSOR['producerSha'],ready)
            self.assertFalse(inspect(self.binding)['attemptReady'])
            mutants=[]
            for key,value in self.guard.SUCCESSOR.items():
                wrong=copy.deepcopy(self.binding);wrong[key]=(True if type(value) is int else str(value)+'-wrong');mutants.append((key,wrong))
            for key in ['attemptReady','toolchain','sourcePackQualification','qualification']:
                wrong=copy.deepcopy(self.binding);wrong[key]=True if key=='attemptReady' else {};mutants.append((key,wrong))
            wrong=copy.deepcopy(self.binding);wrong['qualification']['run']=123;mutants.append(('unknown-partial-qualification',wrong))
            wrong=copy.deepcopy(self.binding);wrong['undeclared']=1;mutants.append(('undeclared-field',wrong))
            for name,wrong in mutants:
                with self.subTest(name=name),self.assertRaises(SystemExit):inspect(wrong)
            qualified=copy.deepcopy(self.binding);qualified['attemptReady']=True
            qualified['qualification']={'status':'qualified','templatesSha':'a'*40,'callerSha':'b'*40,'run':123,'attempt':1,'artifact':456,'outerSha256':'c'*64,'outerSizeBytes':1024,'qualificationSha256':'d'*64,'observationSha256':'e'*64,'rootReadbackSha256':'f'*64}
            self.assertTrue(inspect(qualified,True)['attemptReady']) # inert shape control, no native acceptance
            for field in ['run','artifact','outerSizeBytes','attempt','templatesSha','callerSha','outerSha256','qualificationSha256','observationSha256','rootReadbackSha256']:
                bad=copy.deepcopy(qualified);bad['qualification'][field]=None
                with self.subTest(field=field),self.assertRaises(SystemExit):inspect(bad,True)

    def test_historical_functions_and_original_plan_remain_exact(self):
        import ast
        old=self.guard.source_bytes(ROOT,self.guard.SUCCESSOR['producerSha'],'scripts/release-source-guard.py').decode()
        new=(ROOT/'scripts/release-source-guard.py').read_text()
        def functions(text):
            lines=text.splitlines(True)
            return {node.name:''.join(lines[node.lineno-1:node.end_lineno]) for node in ast.parse(text).body if isinstance(node,ast.FunctionDef)}
        before,after=functions(old),functions(new)
        for name in ['validate_attempt','validate_qualification_payload','executor_identity','acquire_selected','record_event','main']:
            self.assertEqual(before[name],after[name],name)
        self.assertEqual((ROOT/self.guard.ATTEMPT).read_bytes(),self.guard.source_bytes(ROOT,self.guard.SUCCESSOR['producerSha'],self.guard.ATTEMPT))
        self.assertFalse(json.loads((ROOT/self.guard.SUCCESSOR['planPath']).read_text())['publicationReady'])

    def test_current_prerequisites_precede_credentials_and_writer_paths(self):
        wrapper=(ROOT/'scripts/release-nuget-verify-key.fsx').read_text()
        credential=wrapper.index('let key = Environment.GetEnvironmentVariable("NUGET_API_KEY")')
        for literal in ['verify-key-current-effect-refused','verify-key-current-journey-refused','verify-key-current-toolchain-refused']:
            self.assertLess(wrapper.index(literal),credential)
        release=(ROOT/'.github/workflows/release.yml').read_text()
        self.assertIn('mixed immutable attempt selections',release)
        self.assertIn('authority=(--successor-attempt)',release)
        self.assertIn('[[ "$SOURCE_ONLY" != true && "$source_sha" == 9b73395658d4683a067d150ca9441b04e802c4af ]]',release)
        self.assertEqual(1,release.count('  publish-packages:'))



class FontDisposalSourceTests(unittest.TestCase):
    PLAN = 'eng/release/font-disposal-0.32.3.json'

    def validate(self, mutation=None, require_ready=False):
        spec = importlib.util.spec_from_file_location('font_release_guard', ROOT/'scripts/release-source-guard.py')
        subject = importlib.util.module_from_spec(spec); spec.loader.exec_module(subject)
        def source(_root, _sha, path):
            raw = (ROOT/path).read_text()
            return mutation(path, raw) if mutation else raw
        def git(_root, *args):
            if args[0] == 'ls-tree':
                return '\n'.join(str(f.relative_to(ROOT)) for f in (ROOT/'src').rglob('*.fsproj')) + '\n.template.package/FS.GG.UI.Template.fsproj'
            return 'b'*40 if args[0] == 'rev-parse' else ''
        with patch.object(subject.preflight, 'source_text', side_effect=source), patch.object(subject.preflight, 'git', side_effect=git):
            return subject.validate(ROOT, 'a'*40, self.PLAN, '0.32.3', require_ready)

    def test_closed_complete_source_candidate_and_no_fabricated_attempt(self):
        value = self.validate()
        self.assertEqual(('0.32.3', '0.32.1', 19, 'pass'), tuple(value[k] for k in ('version','baselineVersion','rosterCount','result')))
        plan = json.loads((ROOT/self.PLAN).read_text())
        self.assertIs(plan['publicationReady'], False)
        self.assertNotIn('producerSha', plan); self.assertNotIn('qualification', plan)
        self.assertTrue(json.loads((ROOT/'eng/release/font-disposal-0.32.3-attempt.json').read_text())['attemptReady'])
        old = json.loads((ROOT/'eng/release/external-authority-recovery-0.32.2.json').read_text())
        self.assertEqual(old['packages'], plan['packages'])

    def test_readiness_claim_and_effect_request_always_refuse(self):
        for request, claim in [(True,False),(False,True),(True,True)]:
            def mutate(path, raw):
                if path == self.PLAN:
                    value=json.loads(raw);value['publicationReady']=claim;return json.dumps(value)
                return raw
            with self.subTest(request=request,claim=claim),self.assertRaises(SystemExit):self.validate(mutate,request)

    def test_tuple_roster_sdk_and_each_axis_mutants_refuse(self):
        for field, value in [('version','0.32.4'),('baselineVersion','0.32.2'),('packages',[]),('tags',['v0.32.3']),('sdkVersion','10.0.x')]:
            def mutate(path, raw):
                if path == self.PLAN:
                    doc=json.loads(raw);doc[field]=value;return json.dumps(doc)
                return raw
            with self.subTest(field=field),self.assertRaises(SystemExit):self.validate(mutate)
        for axis in ('template/base/Directory.Packages.props','.template.package/FS.GG.UI.Template.fsproj','template/product-skills/fs-gg-symbology/reference.fsx'):
            with self.subTest(axis=axis),self.assertRaises(SystemExit):self.validate(lambda path,raw:raw.replace('0.32.3','0.32.2') if path==axis else raw)

    def test_source_custody_uses_guarded_outputs_and_cannot_publish(self):
        release=(ROOT/'.github/workflows/release.yml').read_text()
        blocks=dict(re.findall(r'^  ([a-z-]+):\n(.*?)(?=^  [a-z-]+:|\Z)',release,re.M|re.S))
        guard_block=blocks['release-source-guard'];candidate=blocks['source-package-custody']
        self.assertIn('plan=eng/release/font-disposal-0.32.3.json',guard_block)
        self.assertIn('$SOURCE_ONLY" == true && "$PREFLIGHT_ONLY" != true && "$BOUND_ATTEMPT" != true && "$successor" != true',guard_block)
        for key in ('plan','version','baseline'):
            self.assertIn('needs.release-source-guard.outputs.'+key,candidate)
        self.assertIn('release-pack.sh "$SOURCE_PLAN" "$GITHUB_SHA" "$SOURCE_VERSION"',candidate)
        self.assertIn('--baseline "$SOURCE_BASELINE" --release-plan "$SOURCE_PLAN"',candidate)
        self.assertNotIn('0.32.2',candidate)
        for effect in ('publication-preflight','publish-packages'):
            self.assertIn('!inputs.source-only',blocks[effect])
        self.assertIn('selected=(--font-disposal-attempt)',blocks['publish-packages'])
        self.assertIn("inputs.font-disposal-attempt && 'eng/release/font-disposal-0.32.3.json'",blocks['publication-preflight'])



class DisabledFontAttemptTests(unittest.TestCase):
    def setUp(self):
        spec=importlib.util.spec_from_file_location('disabled_font_guard',ROOT/'scripts/release-source-guard.py')
        self.g=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.g)

    def test_genuine_original_binding_all19_and_runtime_without_borrowing_qualification(self):
        g=self.g
        # The normal API gate is shallow: pure binding validation must not fetch or
        # require the retained producer object. Its immutable plan is unchanged here.
        plan_bytes=(ROOT/g.FONT_DISPOSAL['planPath']).read_bytes()
        self.assertEqual(g.FONT_DISPOSAL['planSha256'],hashlib.sha256(plan_bytes).hexdigest())
        missing_history=subprocess.CalledProcessError(128,['git','show','missing-producer'])
        with patch.object(g.subprocess,'check_output',side_effect=missing_history):
            # Causal condition: the old direct source_bytes implementation cannot work.
            with self.assertRaises(subprocess.CalledProcessError):
                g.source_bytes(ROOT,g.FONT_DISPOSAL['producerSha'],g.FONT_DISPOSAL['planPath'])
            with patch.object(g,'source_bytes',return_value=plan_bytes) as source:
                b=g.validate_font_disposal_attempt(ROOT,g.FONT_DISPOSAL['producerSha'],False)
                source.assert_called_once_with(ROOT,g.FONT_DISPOSAL['producerSha'],g.FONT_DISPOSAL['planPath'])
            with patch.object(g,'source_bytes',return_value=plan_bytes+b'changed'):
                with self.assertRaisesRegex(SystemExit,'original raw plan differs'):
                    g.validate_font_disposal_attempt(ROOT,g.FONT_DISPOSAL['producerSha'],False)
        self.assertEqual(19,len(b['sourcePackQualification']['archives']))
        self.assertEqual('10.0.13',b['toolchain']['runtimeVersion'])
        self.assertTrue(b['attemptReady']);self.assertEqual(g.FONT_DISPOSAL_QUALIFICATION,b['qualification'])
        with patch.object(g,'source_bytes',return_value=plan_bytes):
            self.assertEqual(b,g.validate_font_disposal_attempt(ROOT,g.FONT_DISPOSAL['producerSha']))
        for key,value in [('producerSha',g.SUCCESSOR['producerSha']),('producerArtifact',g.SUCCESSOR['producerArtifact']),('outerSha256','a'*64),('attemptReady',False),('toolchain',{**b['toolchain'],'runtimeVersion':'10.0.12'})]:
            with self.subTest(key=key),tempfile.TemporaryDirectory() as d:
                root=Path(d);f=root/g.FONT_DISPOSAL_ATTEMPT;f.parent.mkdir(parents=True);bad=copy.deepcopy(b);bad[key]=value;f.write_text(json.dumps(bad))
                with self.assertRaises(SystemExit):g.validate_font_disposal_attempt(root,g.FONT_DISPOSAL['producerSha'],False)

    def test_qualified_route_and_mixed_preflight_refuses_before_acquisition(self):
        g=self.g
        for extra,ok in [(['--attempt-preflight'],True),([],True),(['--acquire-bound-artifacts'],True),(['--attempt-preflight','--acquire-bound-artifacts'],False)]:
            with self.subTest(extra=extra),patch.object(sys,'argv',['guard','--font-disposal-attempt','--repo-root',str(ROOT),*extra]),patch.object(g,'executor_identity',return_value={}),patch.object(g,'source_bytes',return_value=(ROOT/g.FONT_DISPOSAL['planPath']).read_bytes()),patch.object(g,'validate',return_value={}),patch.object(g,'acquire_font_disposal') as acquire,patch.object(g,'accepted_tag_settlement',return_value={}):
                if ok:self.assertEqual(0,g.font_disposal_main())
                else:
                    with self.assertRaises(SystemExit):g.font_disposal_main()
                self.assertEqual(ok and '--acquire-bound-artifacts' in extra,acquire.called)

    def test_actual_workflow_shell_selects_preflight_and_refuses_mixed_attempts_before_port(self):
        import textwrap
        workflow=(ROOT/'.github/workflows/release.yml').read_text()
        block=workflow.split('      - name: Refuse wrong source, axes, roster, delivered surface, or SDK before costly jobs',1)[1].split('  package-tests:',1)[0]
        script=textwrap.dedent(block.split('        run: |\n',1)[1])
        for case in ('preflight','mixed-bound','mixed-successor','mixed-source'):
            with self.subTest(case=case),tempfile.TemporaryDirectory() as d:
                root=Path(d);calls=root/'calls';fake=root/'python3'
                fake.write_text('#!/usr/bin/env bash\nprintf "%s\n" "$*" >> "$CALLS"\nif [[ "$1" == - ]]; then exec "'+sys.executable+'" "$@"; fi\n');fake.chmod(0o700)
                env={**os.environ,'PATH':str(root)+os.pathsep+os.environ['PATH'],'CALLS':str(calls),'GITHUB_OUTPUT':str(root/'output'),'INPUT_VERSION':'0.32.3','REQUESTED_SOURCE':self.g.FONT_DISPOSAL['producerSha'],'GITHUB_SHA':self.g.FONT_DISPOSAL['producerSha'],'PREFLIGHT_ONLY':'true','FONT_DISPOSAL_ATTEMPT':'true','BOUND_ATTEMPT':'false','SUCCESSOR_ATTEMPT':'false','SOURCE_ONLY':'false'}
                if case=='mixed-bound':env['BOUND_ATTEMPT']='true'
                if case=='mixed-successor':env['SUCCESSOR_ATTEMPT']='true'
                if case=='mixed-source':env['SOURCE_ONLY']='true'
                result=subprocess.run(['bash','-c',script],cwd=ROOT,env=env,capture_output=True,text=True,timeout=5)
                self.assertEqual(case=='preflight',result.returncode==0,result.stdout+result.stderr)
                if case=='preflight':
                    lines=calls.read_text().splitlines();self.assertEqual('scripts/release-source-guard.py --executor-only',lines[0]);self.assertIn('--font-disposal-attempt --attempt-preflight --executor-fence',lines[1])
                else:self.assertFalse(calls.exists(),'mixed selection must refuse before tools/fence')

    def test_preflight_private_root_uses_supported_step_handoff_before_setup(self):
        text=(ROOT/'.github/workflows/release.yml').read_text().split('  publication-preflight:',1)[1].split('  publish-packages:',1)[0]
        self.assertIn('if: inputs.font-disposal-attempt',text)
        self.assertNotIn('runner.',text.split('    steps:',1)[0])
        self.assertLess(text.index('Require a fresh private SDK root'),text.index('uses: actions/setup-dotnet@v6'))
        self.assertIn('printf \'DOTNET_INSTALL_DIR=%s\\n\' "$expected" >> "$GITHUB_ENV"',text)
        self.assertIn('release-toolchain-identity.py --expected-sdk 10.0.401',text)


class SourceSdkInstallationRootTests(unittest.TestCase):
    JOBS = ('package-tests', 'template-product-tests', 'source-package-custody')
    STEP = 'Require a fresh private SDK root for source qualification'

    def block(self, name):
        text = (ROOT/'.github/workflows/release.yml').read_text()
        return re.search(r'^  '+name+r':\n(.*?)(?=^  [a-z][a-z0-9-]*:\n|\Z)', text, re.M|re.S).group(1)

    def preparation(self, name):
        import textwrap
        block = self.block(name)
        step = re.search(r'^      - name: '+self.STEP+r'\n(.*?)(?=^      - |\Z)', block, re.M|re.S).group(1)
        self.assertIn('        if: inputs.source-only' + (' || inputs.font-disposal-attempt' if name in ('package-tests','template-product-tests') else '') + '\n', step)
        self.assertIn('        shell: bash\n', step)
        return textwrap.dedent(step.split('        run: |\n', 1)[1])

    def test_actual_workflow_routes_three_source_workloads_through_supported_isolated_install(self):
        scripts = []
        for name in self.JOBS:
            with self.subTest(job=name):
                block = self.block(name)
                job_env = block.split('    steps:\n', 1)[0]
                self.assertNotIn('runner.', job_env)  # GitHub disallows runner in jobs.<id>.env.
                self.assertIn("      DOTNET_INSTALL_DIR: ''\n", job_env)
                step = block.split('      - name: '+self.STEP+'\n',1)[1].split('      - ',1)[0]
                self.assertIn('          DOTNET_INSTALL_DIR: ${{ runner.temp }}/rendering-source-dotnet-10.0.401\n', step)
                self.assertLess(block.index(self.STEP), block.index('uses: actions/setup-dotnet@v6'))
                self.assertLess(block.index('uses: actions/setup-dotnet@v6'), block.index('release-toolchain-identity.py --expected-sdk 10.0.401'))
                self.assertIn("dotnet-version: '10.0.401'", block)
                self.assertNotIn('DOTNET_ROOT:', block)
                scripts.append(self.preparation(name))
        self.assertEqual(1, len(set(scripts)))
        # Installation isolation must not rewrite the developer's roll-forward policy.
        self.assertEqual({'version':'10.0.401','rollForward':'latestFeature','allowPrerelease':False}, json.loads((ROOT/'global.json').read_text())['sdk'])

    def test_actual_preparation_allows_fresh_root_and_refuses_occupied_or_wrong_roots_before_installer(self):
        for name in self.JOBS:
            for case in ('fresh','directory','file','dangling-link','wrong-root','empty-root','relative-parent','linked-parent','missing-parent','trailing-slash'):
                with self.subTest(job=name,case=case), tempfile.TemporaryDirectory() as value:
                    root=Path(value); parent=root/'runner temp'; parent.mkdir(); selected=parent/'rendering-source-dotnet-10.0.401'
                    env={**os.environ,'RUNNER_TEMP':str(parent),'DOTNET_INSTALL_DIR':str(selected),'GITHUB_ENV':str(root/'github-env')}
                    if case=='directory': selected.mkdir()
                    if case=='file': selected.write_text('owner bytes')
                    if case=='dangling-link': selected.symlink_to(root/'missing-target')
                    if case=='wrong-root': env['DOTNET_INSTALL_DIR']=str(parent/'other-sdk')
                    if case=='empty-root': env['DOTNET_INSTALL_DIR']=''
                    if case=='relative-parent': env['RUNNER_TEMP']='relative'
                    if case=='linked-parent':
                        alias=root/'alias';alias.symlink_to(parent, target_is_directory=True);env.update(RUNNER_TEMP=str(alias),DOTNET_INSTALL_DIR=str(alias/selected.name))
                    if case=='missing-parent': env.update(RUNNER_TEMP=str(root/'absent'),DOTNET_INSTALL_DIR=str(root/'absent'/selected.name))
                    if case=='trailing-slash': env['RUNNER_TEMP']=str(parent)+'/'
                    marker=root/'installer-called'
                    # Actual YAML shell, then an inert installer port: no SDK/install/network exists here.
                    command=self.preparation(name)+'\nprintf installer > "$INSTALLER_MARKER"\n'
                    result=subprocess.run(['bash','-c',command],env={**env,'INSTALLER_MARKER':str(marker)},text=True,capture_output=True,timeout=5)
                    self.assertEqual(case=='fresh',result.returncode==0,result.stdout+result.stderr)
                    self.assertEqual(case=='fresh',marker.exists())
                    if case=='file': self.assertEqual('owner bytes',selected.read_text())
                    if case=='directory': self.assertTrue(selected.is_dir())
                    if case=='dangling-link': self.assertTrue(selected.is_symlink())

    def test_actual_preparation_exports_verified_root_to_subsequent_action_only_after_success(self):
        for name in self.JOBS:
            with self.subTest(job=name), tempfile.TemporaryDirectory() as value:
                root=Path(value); parent=root/'runner temp'; parent.mkdir()
                selected=parent/'rendering-source-dotnet-10.0.401'; environment=root/'github-env'
                env={**os.environ,'RUNNER_TEMP':str(parent),'DOTNET_INSTALL_DIR':str(selected),'GITHUB_ENV':str(environment)}
                result=subprocess.run(['bash','-c',self.preparation(name)],env=env,text=True,capture_output=True,timeout=5)
                self.assertEqual(0,result.returncode,result.stderr)
                # Apply the documented GITHUB_ENV handoff to a distinct inert action port.
                self.assertTrue(environment.is_file(), 'verified SDK root must be handed to subsequent action')
                inherited=dict(line.split('=',1) for line in environment.read_text().splitlines())
                result=subprocess.run(['bash','-c','test "$DOTNET_INSTALL_DIR" = "$EXPECTED_ROOT"'],env={**os.environ,**inherited,'EXPECTED_ROOT':str(selected)},capture_output=True,timeout=5)
                self.assertEqual(0,result.returncode)
                environment.unlink(); selected.mkdir()
                result=subprocess.run(['bash','-c',self.preparation(name)],env=env,capture_output=True,timeout=5)
                self.assertNotEqual(0,result.returncode)
                self.assertFalse(environment.exists(), 'refused root must not reach subsequent action')

    def test_actual_loaded_guard_refuses402_without_changing_expected401_or_creating_receipt(self):
        with tempfile.TemporaryDirectory() as value:
            root=Path(value); fake=root/'dotnet'; calls=root/'calls';receipt=root/'receipt.json'
            fake.write_text('#!/usr/bin/env bash\nprintf "%s\\n" "$*" >> "$SDK_CALLS"\nprintf "10.0.402\\n"\n');fake.chmod(0o700)
            result=subprocess.run([sys.executable,'-B',str(ROOT/'scripts/release-toolchain-identity.py'),'--expected-sdk','10.0.401','--receipt',str(receipt)],env={**os.environ,'PATH':str(root)+os.pathsep+os.environ['PATH'],'SDK_CALLS':str(calls)},text=True,capture_output=True,timeout=5)
            self.assertEqual(1,result.returncode)
            self.assertIn("loaded SDK '10.0.402', expected '10.0.401'",result.stderr)
            self.assertEqual(['--version'],calls.read_text().splitlines())
            self.assertFalse(receipt.exists())


class OriginalArtifactFixtureTests(unittest.TestCase):
    def fixture(self, root):
        import argparse, contextlib, io
        spec=importlib.util.spec_from_file_location('original_test_custody',ROOT/'scripts/release-custody.py')
        custody=importlib.util.module_from_spec(spec);spec.loader.exec_module(custody)
        archives=root/'archives';archives.mkdir();plan=ROOT/'eng/release/font-disposal-0.32.3.json'
        doc=json.loads(plan.read_text());source=subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],text=True).strip()
        libraries=[r['id'] for r in doc['packages'] if r['kind']=='library']
        for row in doc['packages']:
            name=row['id'];deps=''
            if row['kind']=='bom':deps=''.join(f'<dependency id="{n}" version="[0.32.3]" />' for n in libraries)
            xml=f'<package><metadata><id>{name}</id><version>0.32.3</version><license type="expression">MIT</license><repository url="https://github.com/FS-GG/FS.GG.Rendering.git" commit="{source}" /><dependencies>{deps}</dependencies></metadata></package>'
            with zipfile.ZipFile(archives/(name+'.0.32.3.nupkg'),'w') as z:
                z.writestr(name+'.nuspec',xml)
                if name=='FS.GG.UI.Scene.SvgBrowser':
                    for entry in doc['releaseChecks']['fableEntries']:z.writestr(entry,'inert fixture bytes')
                    z.writestr('api-surface/SvgBrowser.fsi','\n'.join(doc['releaseChecks']['svgSurfaceMarkers']))
                if row['kind']=='template':
                    for entry in doc['releaseChecks']['generatedTemplateEntries']:z.writestr(entry,'inert fixture bytes')
        manifest=archives/'release-custody.json'
        with contextlib.redirect_stdout(io.StringIO()):custody.command_prepare(argparse.Namespace(plan=plan,archives=archives,manifest=manifest,source_sha=source))
        tools=root/'tools';tools.mkdir();marker=root/'native-called'
        for name in ('dotnet','node','npm'):
            f=tools/name;f.write_text('#!/usr/bin/env bash\nprintf "%s %s\\n" "'+name+'" "$*" >> "$TOOL_MARKER"\nexit 43\n');f.chmod(0o700)
        env={k:v for k,v in os.environ.items() if not k.startswith('SVG_SCENE_ORIGINAL_') and k!='SVG_SCENE_FOCUS_SDK'}
        env.update(PATH=str(tools)+os.pathsep+env['PATH'],TOOL_MARKER=str(marker),PYTHONDONTWRITEBYTECODE='1',SVG_SCENE_ORIGINAL_CANDIDATE_DIR=str(archives),SVG_SCENE_ORIGINAL_CUSTODY=str(manifest),SVG_SCENE_ORIGINAL_CUSTODY_SHA256=hashlib.sha256(manifest.read_bytes()).hexdigest(),SVG_SCENE_ORIGINAL_SOURCE_SHA=source,TMPDIR=str(root))
        return archives,manifest,marker,env

    def invoke(self,root,env,runner=False):
        path=ROOT/'tests/Scene.SvgBrowser.Tests'/('run.sh' if runner else 'build-fixture.sh')
        return subprocess.run(['bash',str(path),str(root/('observation.json' if runner else 'work'))],cwd=ROOT,env=env,capture_output=True,text=True,timeout=10)

    def test_actual_original_shell_stages_three_originals_without_pack_and_default_still_packs(self):
        import xml.etree.ElementTree as ET
        for original in (True,False):
            with tempfile.TemporaryDirectory() as value:
                root=Path(value);archives,manifest,marker,env=self.fixture(root)
                if not original:env={k:v for k,v in env.items() if not k.startswith('SVG_SCENE_ORIGINAL_')}
                result=self.invoke(root,env);self.assertEqual(43,result.returncode,result.stdout+result.stderr)
                calls=marker.read_text().splitlines();self.assertEqual(1,len(calls))
                if original:
                    self.assertTrue(calls[0].startswith('dotnet restore '));self.assertNotIn(' pack ',calls[0])
                    feed=root/'work/feed';self.assertEqual(3,len(list(feed.glob('*.nupkg'))))
                    for f in feed.glob('*.nupkg'):self.assertEqual((archives/f.name).read_bytes(),f.read_bytes())
                    refs=ET.parse(root/'work/Browser/BrowserFixture.fsproj').getroot().findall('.//PackageReference')
                    selected=[r for r in refs if r.get('Include','').startswith('FS.GG.UI.')]
                    self.assertEqual(3,len(selected));self.assertTrue(all(r.get('Version')=='[0.32.3]' for r in selected))
                else:self.assertTrue(calls[0].startswith('dotnet pack '));self.assertIn('Version=0.32.0',calls[0])

    def test_actual_builder_and_runner_refuse_wrong_or_missing_inputs_before_native_tools(self):
        for fault in ('missing','manifest-hash','source','version','archive-hash','missing-archive'):
            for runner in (False,True):
                with self.subTest(fault=fault,runner=runner),tempfile.TemporaryDirectory() as value:
                    root=Path(value);archives,manifest,marker,env=self.fixture(root)
                    if fault=='missing':env.pop('SVG_SCENE_ORIGINAL_SOURCE_SHA')
                    if fault=='manifest-hash':env['SVG_SCENE_ORIGINAL_CUSTODY_SHA256']='0'*64
                    if fault=='source':env['SVG_SCENE_ORIGINAL_SOURCE_SHA']='b'*40
                    if fault=='version':
                        doc=json.loads(manifest.read_text());doc['version']='0.32.2';manifest.write_text(json.dumps(doc));env['SVG_SCENE_ORIGINAL_CUSTODY_SHA256']=hashlib.sha256(manifest.read_bytes()).hexdigest()
                    if fault=='archive-hash':
                        with next(archives.glob('*.nupkg')).open('ab') as stream:stream.write(b'changed')
                    if fault=='missing-archive':next(archives.glob('*.nupkg')).unlink()
                    result=self.invoke(root,env,runner);self.assertNotEqual(0,result.returncode);self.assertFalse(marker.exists(),result.stdout+result.stderr)

    def test_actual_full_custody_function_detects_original_change_after_first_read(self):
        with tempfile.TemporaryDirectory() as value:
            root=Path(value);archives,manifest,marker,env=self.fixture(root)
            text=(ROOT/'tests/Scene.SvgBrowser.Tests/build-fixture.sh').read_text();start=text.index('verify_original() {');end=text.index('if [[ "$original_mode" == true ]]; then',start)
            script='set -euo pipefail\nrepo="'+str(ROOT)+'"\n'+text[start:end]+'\nverify_original >/dev/null\nprintf changed >> "'+str(next(archives.glob('*.nupkg')))+'"\nverify_original >/dev/null\n'
            result=subprocess.run(['bash','-c',script],cwd=ROOT,env=env,capture_output=True,text=True,timeout=10)
            self.assertNotEqual(0,result.returncode);self.assertIn('never repack or substitute',result.stderr);self.assertFalse(marker.exists())

    def test_source_only_ci_uses_actual_original_candidate_for_all_three_browser_families(self):
        text=(ROOT/'.github/workflows/release.yml').read_text();body=re.search(r'^  source-package-custody:\n(.*?)(?=^  [a-z-]+:|\Z)',text,re.M|re.S).group(1)
        self.assertLess(body.index('release-pack.sh'),body.index('SVG_SCENE_ORIGINAL_CANDIDATE_DIR:'))
        for field in ('CANDIDATE_DIR','CUSTODY','SOURCE_SHA'):
            self.assertIn('SVG_SCENE_ORIGINAL_'+field+':',body)
        self.assertIn('SVG_SCENE_ORIGINAL_CUSTODY_SHA256=',body)
        self.assertIn('playwright-core install --with-deps chromium firefox webkit',body)
        self.assertIn('bash tests/Scene.SvgBrowser.Tests/run.sh',body)
        runner=(ROOT/'tests/Scene.SvgBrowser.Tests/run.sh').read_text()
        self.assertIn('for family in chromium firefox webkit; do',runner)
        self.assertIn('node "$work/Browser/studio-test.mjs" --browser "$family"',runner)
        self.assertIn('release-custody.py" verify',runner)


def native_boundary(scope_only=False, current_attempt=False, font_attempt=False):
    """Selected native mode: actual wrapper, synthetic joins, no credential/network."""
    import time
    start=time.monotonic()
    head='a'*40
    plan=ROOT/('eng/release/font-disposal-0.32.3.json' if font_attempt else 'eng/release/external-authority-recovery-0.32.2.json' if scope_only or current_attempt else 'eng/release/svg-export-prefix-0.32.1.json')
    proof={'expectedExecutorSha':head,'executorSha':head,'workflowContextSha':head,
           'workflowSha':head,'workflowRef':'FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main',
           'run':'10','attempt':'1'}
    inputs={**proof,'producerSha':'6c9f766fdd91483c2de6f061e75589e94852a265',
            'originalPlanSha256':'ac433609d14d0a672ad42e5f3578afdd6ecf6ee1144c33d5e6aec951535d84f2',
            'outerSha256':'486182db3efd8442c007f66322a7bfb27caf0cb8e3c87bc2521e66dd90a7adee',
            'custodySha256':'ecd0c5d742fd8d19d5659991e413e92971c1d2cb9aea913faa2bb249f53f577d'}
    # A closed child environment cannot inherit an actual publication credential.
    allowed=['PATH','HOME','TMPDIR','DOTNET_CLI_HOME','DOTNET_ROOT','DOTNET_HOST_PATH',
             'DOTNET_PROCESSOR_COUNT','DOTNET_MULTILEVEL_LOOKUP','DOTNET_NOLOGO',
             'DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_SKIP_FIRST_TIME_EXPERIENCE',
             'DOTNET_EnableDiagnostics','DOTNET_EnableEventPipe','LANG','LC_ALL']
    env={k:os.environ[k] for k in allowed if k in os.environ}
    env.update(FSGG_EXPECTED_EXECUTOR_SHA=head,GITHUB_SHA=head,GITHUB_WORKFLOW_SHA=head,
               GITHUB_REPOSITORY='FS-GG/FS.GG.Rendering',GITHUB_WORKFLOW_REF=proof['workflowRef'],
               GITHUB_RUN_ID='10',GITHUB_RUN_ATTEMPT='1',FSGG_NUGET_SCOPE_ONLY='false',NUGET_API_KEY='')
    cases=[('positive-join',{}, {}, {},'credential'),
           ('missing-expected',{'FSGG_EXPECTED_EXECUTOR_SHA':''},{},{},'executor'),
           ('mismatched-expected',{'FSGG_EXPECTED_EXECUTOR_SHA':'b'*40},{},{},'executor'),
           ('wrong-context',{'GITHUB_SHA':'b'*40},{},{},'executor'),
           ('wrong-workflow',{'GITHUB_WORKFLOW_SHA':'b'*40},{},{},'executor')]
    for field in ('expectedExecutorSha','executorSha','workflowContextSha','workflowSha','workflowRef','run','attempt'):
        cases.append(('facts-'+field,{}, {field:'different'}, {},'executor'))
    for field in ('run','attempt'):
        cases.append(('input-'+field,{}, {}, {field:'different'},'executor'))
    cases.append(('input-producer',{}, {}, {'producerSha':'b'*40},'input'))
    if scope_only:
        env.update(FSGG_NUGET_SCOPE_ONLY='true', FSGG_NUGET_SCOPE_SOURCE=head)
        cases=[('recovery-scope-join',{}, {}, {},'credential'),
               ('recovery-effects-refused',{'FSGG_NUGET_SCOPE_ONLY':'false'},{},{},'unavailable'),
               ('recovery-source-refused',{'FSGG_NUGET_SCOPE_SOURCE':'b'*40},{},{},'unavailable'),
               ('recovery-version-refused',{}, {'version':'0.32.1'}, {},'unavailable'),
               ('recovery-plan-refused',{}, {'planSha256':'b'*64}, {},'unavailable'),
               ('recovery-executor-refused',{'FSGG_EXPECTED_EXECUTOR_SHA':'b'*40},{},{},'executor')]
    if current_attempt:
        env.update(FSGG_RELEASE_ATTEMPT='0322')
        proof.update(originalCustodyVerified=True,nativeTemplatesQualified=True,providerPassed=186,browserPassed=[8,8,8],browserUnexpected=0,browserSkipped=0,browserFlaky=0,atPassed=15,atCustodyQualified=True)
        inputs.update(producerSha='9b73395658d4683a067d150ca9441b04e802c4af',originalPlanSha256='81571fc65ac117e90467428d2e50e103ec875aa33cb83b054d9056bf4129aa1a',outerSha256='7b0532caf431cfa41acaf35555016cf126bc495c5a6e13fe7db6b8fbe7478cca',custodySha256='85bb1dcdf0378d05ca536d5878b88dedc301ff1524d39d98966e23888aae0120',effectId='rendering-0322-original38200411661-a1')
        cases=[('current-positive-no-credential',{}, {}, {},'credential'),
               ('current-unknown-selection',{'FSGG_RELEASE_ATTEMPT':'unknown'}, {}, {},'input'),
               ('current-producer',{}, {}, {'producerSha':'b'*40},'input'),
               ('current-effect',{}, {}, {'effectId':'different'},'input'),
               ('current-provider',{}, {'providerPassed':180}, {},'input'),
               ('current-browser',{}, {'browserPassed':[4,4,4]}, {},'input'),
               ('current-AT',{}, {'atPassed':14}, {},'input'),
               ('current-custody',{}, {'atCustodyQualified':False}, {},'input'),
               ('current-toolchain',{}, {'testToolchainRuntime':'different'}, {},'input'),
               ('current-scope-effect-mixed',{'FSGG_NUGET_SCOPE_ONLY':'true'}, {}, {},'input')]
    if font_attempt:
        env.update(FSGG_RELEASE_ATTEMPT='0323')
        proof.update(originalCustodyVerified=True,nativeTemplatesQualified=True,providerPassed=186,browserPassed=[15,15,15],browserUnexpected=0,browserSkipped=0,browserFlaky=0,atPassed=15,atSpeechWindows=4,atEvidenceMembers=27,atCustodyQualified=True)
        inputs.update(producerSha='4ec6eafbbbf02264db9b0db4780c590abe0bf9cd',originalPlanSha256='89369de0c0944c66a52c6ed914cef78d6bb7670866baedf85cd8a198c5c46239',outerSha256='84647332c124e511545787c8c3f8634d4f5177068521acfc86c93430ac73ef63',custodySha256='7dbd4329546932c4c1d12767550b7b78aae85e32c81b354935b2c615b3e298c9',effectId='rendering-0323-original39509314882-a1')
        settlement={'schema':'fsgg.rendering.tag-settlement/v1','effectId':inputs['effectId'],'producerSha':inputs['producerSha'],'originalTransportOutcome':'Unknown','sameIntentConvergence':'Verified','cleanup':'restored','pendingSubordinateRefs':[],'result':'passed','publication':'not-performed','notification':'not-performed','executorSha':'b'*40,'run':'11','attempt':'1','refs':{'refs/tags/'+n:inputs['producerSha'] for n in ['fs-gg-ui/v0.32.3','fs-gg-ui-template/v0.32.3','v0.32.3']}}
        settlement_raw=json.dumps(settlement).encode()
        inputs['tagSettlement']={'receiptSha256':hashlib.sha256(settlement_raw).hexdigest(),'artifactSha256':'c'*64,'executorSha':'b'*40,'run':11,'attempt':1,'artifact':12}
        env['FSGG_ACCEPTED_TAG_SETTLEMENT']=json.dumps(inputs['tagSettlement'])
        cases=[('font-positive-no-credential',{}, {}, {},'credential'),
               ('font-unknown-selection',{'FSGG_RELEASE_ATTEMPT':'unknown'},{},{},'input'),
               ('font-scope-effect-mixed',{'FSGG_NUGET_SCOPE_ONLY':'true'},{},{},'input')]
        for field in ('producerSha','originalPlanSha256','outerSha256','custodySha256','effectId'):
            cases.append(('font-input-'+field,{}, {}, {field:'different'},'input'))
        for field in ('expectedExecutorSha','executorSha','workflowContextSha','workflowSha','workflowRef','run','attempt'):
            cases.append(('font-executor-'+field,{}, {field:'different'}, {},'executor'))
        for field,bad in [('originalCustodyVerified',False),('nativeTemplatesQualified',False),('providerPassed',180),('browserPassed',[8,8,8]),('browserPassed',[15,15,14]),('browserUnexpected',1),('browserSkipped',1),('browserFlaky',1),('atPassed',14),('atSpeechWindows',3),('atEvidenceMembers',26),('atCustodyQualified',False),('testToolchainRuntime','10.0.12'),('testToolchainSdk','10.0.402'),('testToolchainCompiler','bad'),('testToolchainCore','bad')]:
            cases.append(('font-proof-'+field+'-'+str(bad),{}, {field:bad}, {},'input'))
        for field in ('originalTransportOutcome','sameIntentConvergence','cleanup','effectId','producerSha','result','executorSha','run','refs','pendingSubordinateRefs'):
            cases.append(('font-settlement-'+field,{}, {'testSettlementField':field}, {},'input'))
        cases.append(('font-settlement-replaced-bytes',{}, {'testSettlementBytes':True}, {},'input'))
        cases.append(('font-settlement-selection-missing',{'FSGG_ACCEPTED_TAG_SETTLEMENT':''}, {}, {},'input'))
        cases.append(('font-settlement-selection-mismatch',{'FSGG_ACCEPTED_TAG_SETTLEMENT':'{}'}, {}, {},'input'))
        cases.append(('font-settlement-missing',{}, {}, {'tagSettlement':{}},'input'))
        cases += [('font-stripped-plan',{}, {'testPlanBytes':'stripped'}, {},'input'),
                  ('font-wrong-plan',{}, {'testPlanBytes':'historical'}, {},'input')]
    results=[]
    with tempfile.TemporaryDirectory(prefix='executor-boundary-'  ,dir=os.environ.get('TMPDIR')) as folder:
        root=Path(folder);attempt=root/'artifacts/attempt';attempt.mkdir(parents=True)
        for name,environment,mutant,input_mutant,stage in cases:
            # Wall budget for every native case compiling the fsx; hosted runners took 104.6 s at 9e8bd516
            # and exceeded 140 s after fc2b177e (local 80.5 s -> 83.5 s). Per-case cap stays 15 s.
            remaining=(280 if font_attempt else 85)-(time.monotonic()-start)
            if remaining<=0:raise RuntimeError('native-boundary-original-deadline')
            facts=attempt/'qualification-facts.json'
            if scope_only:
                plan_doc=json.loads(plan.read_text())
                receipt_proof={'schema':'fsgg.rendering.release-preflight/v1','result':'pass','mutation':'none',
                    'sourceSha':head,'workflowSha':head,'version':'0.32.2','frameworkVersion':'0.32.2',
                    'templateVersion':'0.32.2','planSha256':hashlib.sha256(plan.read_bytes()).hexdigest(),
                    'rosterCount':19,'roster':[row['id'] for row in plan_doc['packages']],
                    'workflow':{'repository':env['GITHUB_REPOSITORY'],'ref':proof['workflowRef'],'runId':'10'}}
                facts.write_text(json.dumps({**receipt_proof,**mutant}))
                preflight=root/'artifacts/preflight';preflight.mkdir(exist_ok=True)
                (preflight/'executor-binding.json').write_text(json.dumps(proof))
            else:
                facts.write_text(json.dumps({**proof,**mutant}))
            if font_attempt:
                observed=copy.deepcopy(settlement)
                if 'testSettlementField' in mutant:observed[mutant['testSettlementField']]='different'
                observed_raw=json.dumps(observed).encode()
                selected=copy.deepcopy(inputs)
                selected['tagSettlement']['receiptSha256']=hashlib.sha256(observed_raw).hexdigest()
                (attempt/'accepted-tag-settlement.json').write_bytes(observed_raw+(b' ' if mutant.get('testSettlementBytes') else b''))
            else:selected=inputs
            (attempt/'input-binding.json').write_text(json.dumps({**selected,**input_mutant}))
            if current_attempt or font_attempt:
                toolchain=root/'artifacts/toolchain';toolchain.mkdir(exist_ok=True)
                tool=json.loads((ROOT/'eng/release/external-authority-recovery-0.32.2-attempt.json').read_text())['toolchain']
                if font_attempt:tool['runtimeVersion']='10.0.13'
                for field,key in [('testToolchainRuntime','runtimeVersion'),('testToolchainSdk','sdkVersion'),('testToolchainCompiler','compilerSha256'),('testToolchainCore','fsharpCoreSha256')]:
                    if field in mutant:tool[key]=mutant[field]
                (toolchain/'publish-packages.json').write_text(json.dumps(tool))
            receipt=attempt/'scope.json';receipt.unlink(missing_ok=True)
            command=['/usr/share/dotnet/dotnet','exec','--fx-version','10.0.12',
                     '/usr/share/dotnet/sdk/10.0.401/FSharp/fsi.dll','--nologo','--define:DEBUG','--optimize-',
                     '--exec',str(ROOT/'scripts/release-nuget-verify-key.fsx'),
                     '--plan',str(plan),'--receipt',str(receipt),'--qualification',str(facts),'--census',str(attempt/'census.json')]
            if font_attempt:
                # Inherit the workflow's strictly qualified SDK401 installation; no machine-global runtime12 path.
                command=['dotnet','fsi','--nologo','--define:DEBUG','--optimize-','--exec',str(ROOT/'scripts/release-nuget-verify-key.fsx'),*command[command.index('--plan'):]]
                if 'testPlanBytes' in mutant:
                    changed=root/'mutated-plan.json'
                    changed.write_bytes(plan.read_bytes().strip() if mutant['testPlanBytes']=='stripped' else (ROOT/'eng/release/external-authority-recovery-0.32.2.json').read_bytes())
                    command[command.index('--plan')+1]=str(changed)
            if font_attempt:
                env['FSGG_ACCEPTED_TAG_SETTLEMENT']=json.dumps(selected['tagSettlement'])
            child=subprocess.run(command,cwd=root,env={**env,**environment},capture_output=True,text=True,timeout=min(15,remaining))
            expected=('NuGet verify-key: unavailable/refused; no publication eligibility' if stage=='unavailable' else
                      'NuGet verify-key: refused stage='+stage+'; no publication eligibility')
            if child.returncode!=3 or expected not in child.stderr or 'error FS' in child.stderr or receipt.exists():
                raise AssertionError(name+': actual exit/stage/compilation/receipt refused '+child.stdout+child.stderr)
            results.append({'case':name,'actualExit':child.returncode,'stage':stage,'receiptCreated':False})
    print('SVG_EXECUTOR_BOUNDARY '+json.dumps({'cases':results,'passed':len(results),'actualCredentials':False,
          'networkSelected':False,'positiveJoinReached':'credential','scopeOnly':scope_only,'currentAttempt':current_attempt,'fontAttempt':font_attempt,'elapsedSeconds':time.monotonic()-start},sort_keys=True))


class QualifiedFontProofTests(unittest.TestCase):
    def setUp(self):
        spec=importlib.util.spec_from_file_location('qualified_font_guard',ROOT/'scripts/release-source-guard.py')
        self.g=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.g)

    def browser(self):
        g=self.g
        report={'stats':{'expected':15,'unexpected':0,'skipped':0,'flaky':0},'suites':[]}
        for file,ancestry,title in g.FONT_DISPOSAL_CASES:
            parent=report
            for item in ancestry:
                child=next((s for s in parent.setdefault('suites',[]) if s['title']==item),None)
                if child is None:
                    child={'title':item,'file':file,'suites':[],'specs':[]};parent['suites'].append(child)
                parent=child
            parent.setdefault('specs',[]).append({'title':title,'tests':[{'status':'expected','results':[{'status':'passed','retry':0}]}]})
        return report

    def test_exact_nested_and_direct_font_cases_and_causal_refusals(self):
        g=self.g;report=self.browser();g.validate_font_browser(report)
        def specs(s):
            result=list(s.get('specs',[]))
            for child in s.get('suites',[]):result+=specs(child)
            return result
        mutations=[lambda q:q['stats'].update(expected=8),
                   lambda q:specs(q)[0]['tests'][0]['results'].append({'status':'passed','retry':1}),
                   lambda q:specs(q)[0]['tests'][0]['results'][0].update(status='skipped'),
                   lambda q:specs(q)[0].update(title='unrelated test'),
                   lambda q:q['suites'][0]['suites'][0].update(title='wrong describe ancestry'),
                   lambda q:q['suites'][0]['suites'][0].update(file='wrong.spec.ts'),
                   lambda q:specs(q)[-1].update(title=specs(q)[-2]['title'])]
        for index,mutate in enumerate(mutations):
            bad=copy.deepcopy(report);mutate(bad)
            with self.subTest(index=index),self.assertRaises(SystemExit):g.validate_font_browser(bad)

    def fixture(self,root):
        g=self.g;b=json.loads((ROOT/g.FONT_DISPOSAL_ATTEMPT).read_bytes());q=b['qualification'];members={}
        caller={'repository':'FS-GG/FS.GG.Rendering','revision':q['callerSha'],'run':str(q['run']),'attempt':'1'}
        producer={'revision':b['producerSha'],'run':str(b['producerRun']),'attempt':'1','artifact':str(b['producerArtifact']),'archiveSha256':b['outerSha256'],'custodySha256':b['custodySha256'],'version':b['version'],'plan':b['planPath'],'rawPlanSha256':b['planSha256']}
        selected=['FS.GG.UI.Scene.0.32.3.nupkg','FS.GG.UI.KeyboardInput.0.32.3.nupkg','FS.GG.UI.Scene.SvgBrowser.0.32.3.nupkg']
        for name in selected:(root/name).write_bytes(name.encode())
        prod={'revision':b['producerSha'],'custodyManifestSha256':b['custodySha256'],'archives':[{'name':name,'sha256':hashlib.sha256((root/name).read_bytes()).hexdigest()} for name in selected]}
        result={'disposition':'passed','templates':{'revision':q['templatesSha']},'producer':prod,'browserFamilies':['chromium','firefox','webkit'],'passedPerFamily':15,'studioFontCases':g.FONT_DISPOSAL_FONT_CASES,'skipped':0,'qualificationMode':'font-disposal','renderingInputSource':'candidate','templatesInputSource':'source-built-candidate','actualScreenReader':'not-observed','publication':False,'installedAcceptance':False,'renderingPublicInputsQualified':False,'nativePreflight':{'requestedPhase':'full','caller':caller,'producer':producer},'browserReportSha256':{}}
        def store(name,data):
            raw=json.dumps(data,sort_keys=True).encode() if not isinstance(data,bytes) else data
            members[name]=raw;return hashlib.sha256(raw).hexdigest()
        for family in result['browserFamilies']:result['browserReportSha256'][family]=store(family+'-browser.json',self.browser())
        store('provider-composition.log',b'PASS assertion\n'*186)
        q['sourceQualificationSha256']=store('orca/source-qualification.json',{'actualScreenReader':'not-observed','producer':prod,'templates':result['templates']})
        cleanup={'disposition':'observed-empty','unknown':[],'remaining':[]}
        process={'exit':0,'firstCause':None,'cleanup':cleanup,'firstGuardCensus':None,'firstRssBudgetCensus':None,'knownStarts':{str(i):i for i in range(57)},'reportingErrors':[]}
        bound={'exit':0,'firstCause':None,'runtimeExit':0,'cleanup':cleanup}
        companion={'exit':0,'firstCause':None,'originalBirthAbsent':True,'reaped':True}
        store('orca/process-result.json',process);store('orca/bind-result.json',bound);store('orca/browser-companion-stop-result.json',companion)
        store('orca/orca-debug.log',b'SPEECH OUTPUT qualified!\n'*4)
        # Synthetic placeholders are inert byte fixtures, not claims of actual host evidence.
        for i in range(22):store('orca/fixture-'+str(i),b'inert')
        evidence={name[5:]:hashlib.sha256(raw).hexdigest() for name,raw in members.items() if name.startswith('orca/')}
        self.assertEqual(27,len(evidence))
        observation={'result':'passed','templates':result['templates'],'caller':caller,'producer':producer,'sourceQualificationSha256':q['sourceQualificationSha256'],'keyboard':'xdotool-X11','speechBoundary':'Orca SPEECH OUTPUT','physicalAudioHardware':'not-observed','cleanup':cleanup,'actions':[{'name':name,'key':'Return','atspiFocused':True,'directReferenceControl':True,'eventSequence':i} for i,name in enumerate(g.FONT_DISPOSAL_ACTIONS)],'evidenceSha256':evidence,'observations':{phase:{'speechStart':i*24,'speechEnd':(i+1)*24} for i,phase in enumerate(['unknown','reconciled','rearmed','disposed'])}}
        q['observationSha256']=store('orca/observation.json',observation)
        q['qualificationSha256']=store('qualification.json',result)
        return b,members,result,observation

    def test_full_proof_first_attempt_source_at_and_custody_joins(self):
        g=self.g
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);b,members,result,observation=self.fixture(root)
            # The accepted proof binds the original caller, not today's independently
            # evolving source routes. CI fetches this exact immutable source alongside
            # the original producer plans; production acceptance/constants stay intact.
            caller_path='.github/workflows/templates-external-reference-qualification.yml'
            caller_sha=b['qualification']['callerSha']
            caller=g.preflight.source_text(ROOT,caller_sha,caller_path)
            def original_caller(source_root,source_sha,path):
                self.assertEqual((ROOT,caller_sha,caller_path),(source_root,source_sha,path))
                return caller
            with patch.object(g.preflight,'source_text',side_effect=original_caller):
                facts=g.validate_font_disposal_qualification(ROOT,b,members.__getitem__,root)
                self.assertEqual([15,15,15],facts['browserPassed']);self.assertEqual(15,facts['atPassed']);self.assertEqual(4,facts['atSpeechWindows'])
                for name in ['qualification.json','chromium-browser.json','orca/source-qualification.json','orca/observation.json','orca/bind-result.json']:
                    bad=dict(members);bad[name]+=b' '
                    with self.subTest(name=name),self.assertRaises(SystemExit):g.validate_font_disposal_qualification(ROOT,b,bad.__getitem__,root)
                mutations=[('result',lambda r,o:r.update(actualScreenReader='observed')),
                           ('result',lambda r,o:r['nativePreflight'].update(requestedPhase='preflight-only')),
                           ('result',lambda r,o:r['nativePreflight']['producer'].update(archiveSha256='a'*64)),
                           ('observation',lambda r,o:o['actions'].pop()),
                           ('observation',lambda r,o:o['actions'][0].update(key='Space')),
                           ('observation',lambda r,o:o['cleanup'].update(unknown=[1])),
                           ('observation',lambda r,o:o['observations']['disposed'].update(speechEnd=999999)),
                           ('observation',lambda r,o:o['evidenceSha256'].pop('fixture-0'))]
                for index,(kind,mutate) in enumerate(mutations):
                    bb=copy.deepcopy(b);rr=copy.deepcopy(result);oo=copy.deepcopy(observation);mm=dict(members);mutate(rr,oo)
                    key='qualification.json' if kind=='result' else 'orca/observation.json';value=rr if kind=='result' else oo
                    mm[key]=json.dumps(value,sort_keys=True).encode();bb['qualification']['qualificationSha256' if kind=='result' else 'observationSha256']=hashlib.sha256(mm[key]).hexdigest()
                    with self.subTest(index=index),self.assertRaises(SystemExit):g.validate_font_disposal_qualification(ROOT,bb,mm.__getitem__,root)
                with patch.object(g.preflight,'source_text',return_value=caller.replace(b['qualification']['templatesSha'],'a'*40)):
                    with self.assertRaises(SystemExit):g.validate_font_disposal_qualification(ROOT,b,members.__getitem__,root)

    def test_zip_closed_paths_collisions_types_and_bounds(self):
        import io
        g=self.g
        for name in ['../escape','/absolute','a\\b','a//b','a/./b','C:drive']:
            with self.subTest(name=name):
                stream=io.BytesIO()
                with zipfile.ZipFile(stream,'w') as z:z.writestr(name,b'inert')
                with zipfile.ZipFile(stream) as z,self.assertRaises(SystemExit):g.validate_font_zip(z)
        stream=io.BytesIO()
        with zipfile.ZipFile(stream,'w') as z:
            info=zipfile.ZipInfo('link');info.external_attr=0o120777<<16;z.writestr(info,b'target')
        with zipfile.ZipFile(stream) as z,self.assertRaises(SystemExit):g.validate_font_zip(z)
        stream=io.BytesIO()
        with zipfile.ZipFile(stream,'w') as z:z.writestr('safe/member',b'inert')
        with zipfile.ZipFile(stream) as z:g.validate_font_zip(z)

    def test_event_entry_selection_and_closed_observation_fields(self):
        g=self.g
        # Execute the actual module dispatch tail with inert handlers: callbacks inherit
        # 0323 from workflow env, while executor-only and historical0322 stay distinct.
        tail=(ROOT/'scripts/release-source-guard.py').read_text().split("if __name__ == '__main__':",1)[1]
        import textwrap
        for selected,argv,expected in [('0323',['guard','--record-event'],'font'),('0322',['guard','--record-event'],'successor'),('0323',['guard','--executor-only'],'legacy')]:
            seen=[]
            ports={'os':os,'sys':sys,'font_disposal_main':lambda:seen.append('font'),'successor_main':lambda:seen.append('successor'),'main':lambda:seen.append('legacy')}
            with patch.dict(os.environ,{'FSGG_RELEASE_ATTEMPT':selected}),patch.object(sys,'argv',list(argv)),self.assertRaises(SystemExit):exec(textwrap.dedent(tail),ports)
            self.assertEqual([expected],seen)
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);path=root/g.FONT_DISPOSAL_ATTEMPT;path.parent.mkdir(parents=True);path.write_bytes((ROOT/g.FONT_DISPOSAL_ATTEMPT).read_bytes())
            plan=(ROOT/g.FONT_DISPOSAL['planPath']).read_bytes()
            args=['guard','--font-disposal-attempt','--repo-root',str(root),'--record-event','--event-feed','github','--event-package','all19','--event-stage','push','--event-result','requested']
            with patch.object(g,'executor_identity',return_value={'executorSha':'a'*40}),patch.object(g,'source_bytes',return_value=plan),patch.object(g,'validate',return_value={}),patch.object(g,'verified_cached_tag_settlement',return_value={}),patch.object(g,'accepted_tag_settlement',side_effect=AssertionError('record path must not re-authenticate over the network')),patch.object(sys,'argv',args):
                self.assertEqual(0,g.font_disposal_main())
            event=json.loads((root/'artifacts/attempt/publisher-events.jsonl').read_text())
            self.assertEqual(g.FONT_DISPOSAL['producerSha'],event['producerSha']);self.assertEqual(g.FONT_DISPOSAL['planSha256'],event['planSha256']);self.assertEqual('a'*40,event['executorSha'])
            for field,value in [('feed','other'),('package','other'),('stage','other'),('result','other')]:
                bad=list(args);bad[bad.index('--event-'+field)+1]=value
                before=(root/'artifacts/attempt/publisher-events.jsonl').read_bytes()
                with patch.object(g,'executor_identity',return_value={}),patch.object(g,'source_bytes',return_value=plan),patch.object(g,'validate',return_value={}),patch.object(sys,'argv',bad),self.assertRaises(SystemExit):g.font_disposal_main()
                self.assertEqual(before,(root/'artifacts/attempt/publisher-events.jsonl').read_bytes())

    IDENTITY={'expectedExecutorSha':'a'*40,'executorSha':'a'*40,'workflowContextSha':'a'*40,'workflowSha':'a'*40,
        'workflowRef':'FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main','run':'7','attempt':'1'}

    def _cached_settlement_root(self,d):
        g=self.g;root=Path(d)
        path=root/g.FONT_DISPOSAL_ATTEMPT;path.parent.mkdir(parents=True);path.write_bytes((ROOT/g.FONT_DISPOSAL_ATTEMPT).read_bytes())
        receipt={'schema':'fsgg.rendering.tag-settlement/v1','effectId':g.FONT_DISPOSAL['effectId'],'producerSha':g.FONT_DISPOSAL['producerSha'],
            'originalTransportOutcome':'Unknown','sameIntentConvergence':'Verified','cleanup':'restored','pendingSubordinateRefs':[],'result':'passed',
            'publication':'not-performed','notification':'not-performed','executorSha':'b'*40,'run':'11','attempt':'1',
            'workflowRef':'FS-GG/FS.GG.Rendering/.github/workflows/release-tag-settlement.yml@refs/heads/main','refs':g.settlement_refs()}
        member=json.dumps(receipt).encode()
        selected={'run':11,'attempt':1,'artifact':12,'artifactSha256':'c'*64,'receiptSha256':hashlib.sha256(member).hexdigest(),'executorSha':'b'*40}
        attempt=root/'artifacts/attempt';attempt.mkdir(parents=True)
        (attempt/'accepted-tag-settlement.json').write_bytes(member)
        joined={**selected,'originalTransportOutcome':'Unknown','sameIntentConvergence':'Verified','cleanup':'restored','effectId':g.FONT_DISPOSAL['effectId']}
        (attempt/'input-binding.json').write_text(json.dumps({**self.IDENTITY,'producerSha':g.FONT_DISPOSAL['producerSha'],'effectId':g.FONT_DISPOSAL['effectId'],'tagSettlement':joined}))
        return root,receipt,selected

    def _record_args(self,root):
        return ['guard','--font-disposal-attempt','--repo-root',str(root),'--record-event','--event-feed','nuget','--event-package','all19','--event-stage','push','--event-result','requested']

    def test_record_event_reuses_authenticated_settlement_with_zero_transport(self):
        # Exercises the REAL verified_cached_tag_settlement (no mock bypass). Every transport
        # primitive the module could reach counts and refuses: ~191 publisher records must not
        # spend GITHUB_TOKEN requests or re-download the settlement archive.
        import socket
        g=self.g;plan=(ROOT/g.FONT_DISPOSAL['planPath']).read_bytes();transport=[]
        def refuse(*a,**k):transport.append(a[:1]);raise AssertionError('record path transport')
        with tempfile.TemporaryDirectory() as d:
            root,_,selected=self._cached_settlement_root(d)
            with patch.dict(os.environ,{'FSGG_ACCEPTED_TAG_SETTLEMENT':json.dumps(selected)}),patch.object(g,'executor_identity',return_value=dict(self.IDENTITY)),patch.object(g,'source_bytes',return_value=plan),patch.object(g,'validate',return_value={}),\
                 patch.object(g.subprocess,'check_output',side_effect=refuse),patch.object(g.subprocess,'run',side_effect=refuse),patch.object(g.subprocess,'Popen',side_effect=refuse),patch.object(socket.socket,'connect',refuse):
                for _ in range(191):
                    with patch.object(sys,'argv',self._record_args(root)):self.assertEqual(0,g.font_disposal_main())
            self.assertEqual([],transport)
            self.assertEqual(191,len((root/'artifacts/attempt/publisher-events.jsonl').read_text().splitlines()))

    def test_record_event_refuses_tampered_or_unauthenticated_cached_settlement(self):
        g=self.g;plan=(ROOT/g.FONT_DISPOSAL['planPath']).read_bytes()
        def mutate(kind,root,receipt,selected):
            attempt=root/'artifacts/attempt'
            if kind=='tampered-bytes':
                p=attempt/'accepted-tag-settlement.json';p.write_bytes(p.read_bytes()+b' ')
            if kind=='missing-receipt':(attempt/'accepted-tag-settlement.json').unlink()
            if kind=='missing-binding':(attempt/'input-binding.json').unlink()
            if kind in ('claims-rehashed','refs-rehashed','pending-rehashed'):
                doc=dict(receipt)
                if kind=='claims-rehashed':doc['sameIntentConvergence']='Unknown'
                if kind=='refs-rehashed':doc['refs']={**doc['refs'],'refs/tags/v0.32.3':'c'*40}
                if kind=='pending-rehashed':doc['pendingSubordinateRefs']=['refs/tags/v0.32.3']
                raw=json.dumps(doc).encode();(attempt/'accepted-tag-settlement.json').write_bytes(raw)
                selected=dict(selected,receiptSha256=hashlib.sha256(raw).hexdigest())
                b=json.loads((attempt/'input-binding.json').read_text());b['tagSettlement']['receiptSha256']=selected['receiptSha256'];(attempt/'input-binding.json').write_text(json.dumps(b))
            if kind=='other-selection':selected=dict(selected,run=99)
            if kind=='binding-other-attempt':
                b=json.loads((attempt/'input-binding.json').read_text());b['tagSettlement']['artifact']=13;(attempt/'input-binding.json').write_text(json.dumps(b))
            if kind=='duplicate-key':
                p=attempt/'accepted-tag-settlement.json';raw=p.read_bytes().replace(b'{"schema"',b'{"result":"passed","schema"',1);p.write_bytes(raw)
                selected=dict(selected,receiptSha256=hashlib.sha256(raw).hexdigest())
                b=json.loads((attempt/'input-binding.json').read_text());b['tagSettlement']['receiptSha256']=selected['receiptSha256'];(attempt/'input-binding.json').write_text(json.dumps(b))
            if kind=='no-selection':return None
            return selected
        for kind in ('tampered-bytes','missing-receipt','missing-binding','claims-rehashed','refs-rehashed','pending-rehashed','other-selection','binding-other-attempt','duplicate-key','no-selection','other-run-identity','other-executor-identity'):
            with self.subTest(kind=kind),tempfile.TemporaryDirectory() as d:
                root,receipt,selected=self._cached_settlement_root(d)
                chosen=mutate(kind,root,receipt,selected)
                env={'FSGG_ACCEPTED_TAG_SETTLEMENT':'' if chosen is None else json.dumps(chosen)}
                identity=dict(self.IDENTITY,run='8') if kind=='other-run-identity' else dict(self.IDENTITY,executorSha='d'*40) if kind=='other-executor-identity' else dict(self.IDENTITY)
                with patch.dict(os.environ,env),patch.object(g,'executor_identity',return_value=identity),patch.object(g,'source_bytes',return_value=plan),patch.object(g,'validate',return_value={}),\
                     patch.object(g.subprocess,'check_output',side_effect=AssertionError('no transport')),patch.object(sys,'argv',self._record_args(root)),self.assertRaises(SystemExit):
                    g.font_disposal_main()
                self.assertFalse((root/'artifacts/attempt/publisher-events.jsonl').exists())

    def test_non_record_font_entry_still_authenticates_settlement_natively(self):
        g=self.g;plan=(ROOT/g.FONT_DISPOSAL['planPath']).read_bytes()
        with tempfile.TemporaryDirectory() as d:
            root,_,_=self._cached_settlement_root(d)
            with patch.object(g,'executor_identity',return_value={}),patch.object(g,'source_bytes',return_value=plan),patch.object(g,'validate',return_value={}),\
                 patch.object(g,'accepted_tag_settlement',return_value={}) as native,patch.object(g,'verified_cached_tag_settlement',side_effect=AssertionError('static guard must not trust a cache')),\
                 patch.object(sys,'argv',['guard','--font-disposal-attempt','--repo-root',str(root),'--executor-fence']):
                self.assertEqual(0,g.font_disposal_main())
            native.assert_called_once()

    def test_acquisition_joins_native_originals_before_proof_and_never_repacks(self):
        import io
        g=self.g;b=json.loads((ROOT/g.FONT_DISPOSAL_ATTEMPT).read_bytes())
        def archive(rows):
            stream=io.BytesIO()
            with zipfile.ZipFile(stream,'w') as z:
                for name,raw in rows.items():z.writestr(name,raw)
            return stream.getvalue()
        custody=json.dumps({'archives':[{'file':'package-'+str(i)+'.nupkg'} for i in range(19)]}).encode()
        candidate=archive({**{'package-'+str(i)+'.nupkg':b'inert package' for i in range(19)},'release-custody.json':custody})
        proof=archive({'external-reference/qualification.json':b'{}'})
        b['outerSha256']=hashlib.sha256(candidate).hexdigest();b['outerSizeBytes']=len(candidate);b['custodySha256']=hashlib.sha256(custody).hexdigest()
        q=b['qualification'];q['outerSha256']=hashlib.sha256(proof).hexdigest();q['outerSizeBytes']=len(proof)
        for wrong in [False,True]:
            with self.subTest(wrong_native_source=wrong),tempfile.TemporaryDirectory() as d:
                root=Path(d);calls=[]
                def api(argv,**kwargs):
                    self.assertEqual(60,kwargs['timeout']);self.assertEqual(['gh','api'],argv[:2]);calls.append(argv[-1]);path=argv[-1]
                    subject=(q if str(q['run']) in path or str(q['artifact']) in path else {'run':b['producerRun'],'attempt':b['producerAttempt'],'artifact':b['producerArtifact'],'callerSha':b['producerSha'],'outerSha256':b['outerSha256'],'outerSizeBytes':b['outerSizeBytes']})
                    if path.endswith('/zip'):return proof if subject is q else candidate
                    workflow='.github/workflows/templates-external-reference-qualification.yml' if subject is q else '.github/workflows/release.yml'
                    if '/runs/' in path:return json.dumps({'id':subject['run'],'run_attempt':1,'head_sha':'b'*40 if wrong else subject['callerSha'],'conclusion':'success','path':workflow}).encode()
                    return json.dumps({'id':subject['artifact'],'expired':False,'workflow_run':{'id':subject['run'],'head_sha':subject['callerSha']},'digest':'sha256:'+subject['outerSha256'],'size_in_bytes':subject['outerSizeBytes']}).encode()
                with patch.object(g,'executor_identity',return_value={}),patch.object(g.subprocess,'check_output',side_effect=api),patch.object(g.subprocess,'run') as verify,patch.object(g,'source_bytes',return_value=b'original raw plan'),patch.object(g,'validate_font_disposal_qualification',return_value={'atPassed':15}) as validate,patch.object(g,'accepted_tag_settlement',return_value={'receiptSha256':'a'*64}):
                    if wrong:
                        with self.assertRaises(SystemExit):g.acquire_font_disposal(root,b)
                        verify.assert_not_called();validate.assert_not_called();self.assertFalse(any(p.endswith('/zip') for p in calls))
                    else:
                        g.acquire_font_disposal(root,b)
                        self.assertEqual(6,len(calls));verify.assert_called_once();self.assertIn('verify',verify.call_args.args[0]);validate.assert_called_once()
                        self.assertEqual(candidate,(root/'artifacts/attempt/original.zip').read_bytes())
                        facts=json.loads((root/'artifacts/attempt/input-binding.json').read_text());self.assertEqual(b['producerSha'],facts['producerSha']);self.assertEqual('not-yet-observed',facts['publication'])

    def test_publisher_reuses_same_supported_fresh_sdk_root_guard(self):
        text=(ROOT/'.github/workflows/release.yml').read_text()
        pre=text.split('  publication-preflight:',1)[1].split('  publish-packages:',1)[0]
        pub=text.split('  publish-packages:',1)[1]
        def step(block,name):return block.split('      - name: Require a fresh private SDK root for font-disposal '+name,1)[1].split('      - uses: actions/setup-dotnet@v6',1)[0]
        self.assertEqual(step(pre,'preflight'),step(pub,'publisher'))
        self.assertIn('if: inputs.font-disposal-attempt',step(pub,'publisher'))
        self.assertIn('release-toolchain-identity.py --expected-sdk 10.0.401',pub)
        self.assertIn('accepted source39509314882/a1:17 comparisons vs0.32.1',pub)


class FontWriterBoundaryFixtureTests(unittest.TestCase):
    def test_actual_boundary_launch_uses_selected_sdk_and_closed_positive_inputs(self):
        class Captured(Exception): pass
        captured=[]
        def inspect_child(command, *, cwd, env, **kwargs):
            captured.append(command)
            self.assertEqual(['dotnet','fsi'],command[:2])
            self.assertNotIn('/usr/share/dotnet', ' '.join(command))
            self.assertEqual('0323',env['FSGG_RELEASE_ATTEMPT'])
            self.assertEqual('false',env['FSGG_NUGET_SCOPE_ONLY'])
            self.assertEqual('',env['NUGET_API_KEY'])
            self.assertNotIn('GITHUB_TOKEN',env)
            plan=Path(command[command.index('--plan')+1]).read_bytes()
            self.assertEqual('89369de0c0944c66a52c6ed914cef78d6bb7670866baedf85cd8a198c5c46239',hashlib.sha256(plan).hexdigest())
            facts=json.loads((cwd/'artifacts/attempt/qualification-facts.json').read_text())
            self.assertEqual(([15,15,15],15,4,27),(facts['browserPassed'],facts['atPassed'],facts['atSpeechWindows'],facts['atEvidenceMembers']))
            inputs=json.loads((cwd/'artifacts/attempt/input-binding.json').read_text())
            self.assertEqual('4ec6eafbbbf02264db9b0db4780c590abe0bf9cd',inputs['producerSha'])
            self.assertEqual('rendering-0323-original39509314882-a1',inputs['effectId'])
            self.assertEqual('10.0.13',json.loads((cwd/'artifacts/toolchain/publish-packages.json').read_text())['runtimeVersion'])
            self.assertLessEqual(kwargs['timeout'],15)
            raise Captured()
        with patch.dict(os.environ,{'NUGET_API_KEY':'ambient-not-a-credential','GITHUB_TOKEN':'ambient-not-a-credential'}), patch.object(subprocess,'run',side_effect=inspect_child):
            with self.assertRaises(Captured):native_boundary(font_attempt=True)
        self.assertEqual(1,len(captured))

    def test_hosted_boundary_is_in_no_credential_sdk_step(self):
        text=(ROOT/'.github/workflows/gate.yml').read_text()
        before,after=text.split('      - name: ApiCompat vs feed baseline',1)
        step=before.rsplit('      - name: Qualify closed read-only release selectors without credentials',1)[1]
        self.assertIn('release-toolchain-identity.py --expected-sdk 10.0.401',step)
        self.assertIn('dotnet fsi --define:DEBUG --exec scripts/test-release-nuget-verify-key.fsx',step)
        self.assertIn('python3 scripts/test-release-source-guard.py --native-font-disposal-boundary',step)
        self.assertNotIn('secrets.',step)
        self.assertNotIn('env:',step)


if __name__ == '__main__':
    if sys.argv[1:]==['--native-boundary']: native_boundary()
    elif sys.argv[1:]==['--native-scope-boundary']: native_boundary(True)
    elif sys.argv[1:]==['--native-current-boundary']: native_boundary(False,True)
    elif sys.argv[1:]==['--native-font-disposal-boundary']: native_boundary(False,False,True)
    else: unittest.main()
