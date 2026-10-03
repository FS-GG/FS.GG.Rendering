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

    def test_actual_workflow_mode_guard_allows_only_nonmutating_modes_without_readiness(self):
        release=(ROOT/'.github/workflows/release.yml').read_text()
        block=re.search(r'^  release-source-guard:\n(.*?)(?=^  [a-z-]+:)',release,re.M|re.S).group(1)
        script=block.split('        run: |\n',1)[1]
        script='\n'.join(line[10:] for line in script.splitlines() if line.strip())
        with tempfile.TemporaryDirectory() as folder:
            temp=Path(folder)
            # Capture the real workflow invocation; no candidate source, credentials or network execute.
            python=temp/'python3'
            python.write_text("#!/usr/bin/env bash\nprintf '%s\\n' \"$@\" > \"$CAPTURE\"\nfor arg in \"$@\"; do [[ \"$arg\" != --require-publication-ready ]] || exit 42; done\n")
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
                         'CAPTURE':str(capture),'GIT_CAPTURE':str(git_capture)}
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
        self.assertIn('rendering-source-candidate-${{ github.sha }}-0.32.0',candidate)
        self.assertIn('--baseline 0.31.0',candidate)

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
        self.assertIn('!inputs.preflight-only && inputs.bound-attempt',publisher)
        self.assertLess(text.index('--source-sha "$source_sha" --version "$version"'), text.index('--attempt-binding --acquire-bound-artifacts'))
        self.assertLess(publisher.index('--attempt-binding --acquire-bound-artifacts'), publisher.index('Verify the version to publish is the version the guard validated'))
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
        self.assertIn('if not(admit facts)',wrapper)

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

if __name__ == '__main__': unittest.main()
