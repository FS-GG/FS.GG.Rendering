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
import subprocess
import zipfile
import os
from datetime import datetime, timezone

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('preflight', Path(__file__).with_name('release-preflight.py'))
preflight = importlib.util.module_from_spec(spec)
assert spec.loader
spec.loader.exec_module(preflight)

# SDKs a selected release source may name; see the check in validate().
RELEASE_SDKS = frozenset({'10.0.401', '10.0.403'})


def validate(root: Path, source_sha: str, plan_path: str, requested_version: str = '', require_publication_ready: bool = False) -> dict:
    if not preflight.SHA.fullmatch(source_sha):
        preflight.fail('source identity must be an exact lowercase 40-hex SHA')
    preflight.git(root, 'cat-file', '-e', f'{source_sha}^{{commit}}')
    text = lambda path: preflight.source_text(root, source_sha, path)
    raw = (source_bytes(root, source_sha, plan_path).decode()
           if plan_path == 'eng/release/palette-heading-0.32.4.json' else text(plan_path))
    plan = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    if plan.get('schema') != 'fsgg.rendering.release-plan/v1':
        preflight.fail('unsupported release-plan schema')
    if not isinstance(plan.get('publicationReady'), bool):
        preflight.fail('successor plan must explicitly record publication readiness')
    if require_publication_ready and plan['publicationReady'] is not True:
        preflight.fail('publication pending: genuine Rendering identity and publisher grants are not joined')
    version = plan.get('version')
    # Closed source selections only. The original publisher/attempt remains bound to
    # its historical plan and producer; adding source preparation grants no effects.
    selections = {
        'eng/release/svg-external-authority-0.32.0.json': ('0.32.0', '0.31.0'),
        'eng/release/svg-export-prefix-0.32.1.json': ('0.32.1', '0.32.0'),
        'eng/release/external-authority-recovery-0.32.2.json': ('0.32.2', '0.32.1'),
        'eng/release/font-disposal-0.32.3.json': ('0.32.3', '0.32.1'),
    }
    if plan_path == 'eng/release/palette-heading-0.32.4.json':
        # Exact-source transport only. The maintained typed selector interprets the new
        # successor and refuses all effects until actual original custody/receiver joins exist.
        envelope = {'sourceSha': source_sha, 'planPath': plan_path, 'version': requested_version or version,
                    'intent': 'publication' if require_publication_ready else 'source-qualification',
                    'rawPlan': raw, 'sourcePackages': [{'id': key, 'kind': value} for key, value
                        in preflight.source_package_roster(root, source_sha).items()]}
        command = ['dotnet', 'fsi', '--exec', str(root / 'scripts/release-selected-successor.fsx')]
        completed = subprocess.run(command, input=json.dumps(envelope), text=True, capture_output=True, timeout=60,
                                   env={**os.environ, 'DOTNET_NOLOGO': 'true', 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE': '1'})
        if completed.returncode != 0:
            preflight.fail('typed successor refused: ' + completed.stderr.strip())
        verdict = json.loads(completed.stdout, object_pairs_hook=preflight.unique_json_object)
        if verdict != {'result': 'pass', 'scope': 'source-qualification-only', 'publicationAdmitted': False}:
            preflight.fail('typed successor verdict unavailable or malformed')
    elif plan_path not in selections or (version, plan.get('baselineVersion')) != selections[plan_path]:
        preflight.fail('release plan path/version/baseline is not a selected source tuple')
    if version in ('0.32.1', '0.32.2', '0.32.3') and (require_publication_ready or plan['publicationReady'] is not False):
        preflight.fail(f'{version} source preparation only: candidate custody and installed qualification are unbound')
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
    # Closed pair (FS-GG/.github#4943). Retained and historical producer sources (every selected plan above, all
    # hash-bound) were built, and their plans written, on 10.0.401; a source prepared for the current release
    # toolchain is exactly 10.0.403. Anything else, including a floating or neighbouring SDK, refuses.
    if plan.get('sdkVersion') not in RELEASE_SDKS:
        preflight.fail('release plan SDK must be exactly one of ' + ' or '.join(sorted(RELEASE_SDKS)))
    if json.loads(text('global.json'))['sdk']['version'] != plan['sdkVersion']:
        preflight.fail('source global.json differs from release SDK')
    for workflow in ('.github/workflows/release.yml', '.github/workflows/release-tags.yml'):
        versions = re.findall(r'dotnet-version:\s*[\'"]?([^\s\'"]+)', text(workflow))
        if not versions or any(v != plan['sdkVersion'] for v in versions):
            preflight.fail(f"{workflow}: every release SDK setup must be exactly {plan['sdkVersion']}")
    recipe = text('template/product-skills/fs-gg-symbology/reference.fsx')
    pins = re.findall(r'#r "nuget: (FS.GG.UI.[^,]+), ([^"]+)"', recipe)
    if len(pins) != 4 or any(v != version for _, v in pins):
        preflight.fail('shipped symbology recipe pins differ from coherent version')
    return {'schema': 'fsgg.rendering.release-source-guard/v1', 'sourceSha': source_sha,
            'sourceTree': preflight.git(root, 'rev-parse', f'{source_sha}^{{tree}}'),
            'version': version, 'baselineVersion': plan['baselineVersion'],
            'planSha256': hashlib.sha256(raw.encode()).hexdigest(), 'rosterCount': len(ids),
            'result': 'pass', 'scope': 'source only; no publication authority'}


ATTEMPT = 'eng/release/svg-export-prefix-0.32.1-attempt.json'
PRODUCER = '6c9f766fdd91483c2de6f061e75589e94852a265'
PLAN_HASH = 'ac433609d14d0a672ad42e5f3578afdd6ecf6ee1144c33d5e6aec951535d84f2'
OUTER = '486182db3efd8442c007f66322a7bfb27caf0cb8e3c87bc2521e66dd90a7adee'
CUSTODY = 'ecd0c5d742fd8d19d5659991e413e92971c1d2cb9aea913faa2bb249f53f577d'

def source_bytes(root: Path, sha: str, path: str) -> bytes:
    # Existing text helper strips whitespace; custody must bind exact bytes, including final newline.
    return subprocess.check_output(['git','-C',str(root),'show',sha+':'+path],stderr=subprocess.DEVNULL)


def validate_attempt(root: Path, source_sha: str, require_ready: bool = True) -> dict:
    binding = json.loads((root/ATTEMPT).read_text(), object_pairs_hook=preflight.unique_json_object)
    expected = {'schema':'fsgg.rendering.publication-attempt/v1','producerSha':PRODUCER,
        'planPath':'eng/release/svg-export-prefix-0.32.1.json','planSha256':PLAN_HASH,
        'producerRun':37419955679,'producerAttempt':1,'producerArtifact':11392773621,'outerSha256':OUTER,
        'outerSizeBytes':14912688,'custodySha256':CUSTODY,'version':'0.32.1','baselineVersion':'0.32.0',
        'githubEffectiveWrite':'unknown-before-attempt'}
    if source_sha != PRODUCER or any(binding.get(k)!=v for k,v in expected.items()):
        preflight.fail('selected attempt producer/artifact/plan identity differs')
    if hashlib.sha256(source_bytes(root,source_sha,binding['planPath'])).hexdigest()!=PLAN_HASH:
        preflight.fail('selected original plan bytes differ; never amend producer custody')
    q = binding.get('qualification', {})
    qualified = {'status':'qualified','templatesSha':'4eff52b27933c69a9ab4e276b3a29032d279b7e0',
        'callerSha':'67091200bc7bbcbb8189b2cf84244b2af919ecd1','run':37428584026,'attempt':1,
        'artifact':11396281620,'outerSha256':'90d6693543e21fe98649a3efca3f64a0bfcca64450967a095a75bbd9bbd0edb3',
        'outerSizeBytes':30941,'qualificationSha256':'ac3ce9a684807e1cfa8f8ef14acef2b1d6a1832ae936320db8be6a3382b8b623',
        'rootReadbackSha256':'71c09f045e08d85261fd26575da5072a783259c1171d951d6ecf7d628707fec7'}
    if any(q.get(k)!=v for k,v in qualified.items()):
        preflight.fail('selected accepted receiver qualification identity differs')
    if not isinstance(binding.get('attemptReady'),bool): preflight.fail('attempt readiness must be explicit')
    if require_ready:
        q=binding.get('qualification',{})
        if binding['attemptReady'] is not True or q.get('status')!='qualified':
            preflight.fail('attempt pending: genuine native Templates and authority prerequisites not joined')
        for key in ('templatesSha','callerSha'):
            if not preflight.SHA.fullmatch(str(q.get(key,''))): preflight.fail('native qualification identity unknown')
        for key in ('run','artifact','outerSizeBytes'):
            if type(q.get(key)) is not int or q[key]<=0: preflight.fail('native qualification artifact unknown')
        if not re.fullmatch('[0-9a-f]{64}',str(q.get('outerSha256',''))): preflight.fail('native qualification digest unknown')
    return binding


def validate_qualification_payload(root: Path, binding: dict, read, archives: Path) -> tuple[list[int], int]:
    q=binding['qualification']
    raw=read('qualification.json')
    if hashlib.sha256(raw).hexdigest()!=q['qualificationSha256']:preflight.fail('accepted qualification digest differs')
    result=json.loads(raw)
    if result.get('disposition')!='passed' or result.get('templates',{}).get('revision')!=q['templatesSha'] or result.get('producer',{}).get('revision')!=PRODUCER or result.get('producer',{}).get('custodyManifestSha256')!=CUSTODY:preflight.fail('native qualification producer/template join refused')
    if result.get('browserFamilies')!=['chromium','firefox','webkit'] or result.get('passedPerFamily')!=4 or result.get('skipped')!=0:preflight.fail('native three-family qualification refused')
    caller=preflight.source_text(root,q['callerSha'],'.github/workflows/templates-external-reference-qualification.yml')
    if re.findall(r'^    uses: FS-GG/FS.GG.Templates/\.github/workflows/fable-external-reference-source\.yml@([0-9a-f]{40})$',caller,re.M)!=[q['templatesSha']] or re.findall(r'^      templates-source: ([0-9a-f]{40})$',caller,re.M)!=[q['templatesSha']]:preflight.fail('native caller executable/checkout join refused')
    native=result.get('nativePreflight',{})
    context=native.get('caller',{})
    if native.get('requestedPhase')!='full' or context.get('repository')!='FS-GG/FS.GG.Rendering' or context.get('revision')!=q['callerSha'] or context.get('run')!=str(q['run']) or context.get('attempt')!=str(q['attempt']):preflight.fail('native full-phase caller receipt differs')
    selected=['FS.GG.UI.Scene.0.32.1.nupkg','FS.GG.UI.KeyboardInput.0.32.1.nupkg','FS.GG.UI.Scene.SvgBrowser.0.32.1.nupkg']
    actual={row['name']:row['sha256'] for row in result['producer']['archives']}
    if len(result['producer']['archives'])!=3 or actual!={name:hashlib.sha256((archives/name).read_bytes()).hexdigest() for name in selected}:preflight.fail('native qualification archives differ from original selected bytes')
    counts=[]
    for family in result['browserFamilies']:
        data=read(family+'-browser.json')
        if hashlib.sha256(data).hexdigest()!=result['browserReportSha256'][family]:preflight.fail('native browser digest differs')
        stats=json.loads(data)['stats']
        if any(stats.get(k)!=v for k,v in [('expected',4),('unexpected',0),('skipped',0),('flaky',0)]):preflight.fail('native browser nonpass results')
        counts.append(stats['expected'])
    providers=sum(line.startswith('PASS ') for line in read('provider-composition.log').decode().splitlines())
    if providers!=180:preflight.fail('native Provider requires exactly180 checks')
    return counts,providers


def executor_identity(root: Path) -> dict:
    expected = os.environ.get('FSGG_EXPECTED_EXECUTOR_SHA', '')
    executor = preflight.git(root, 'rev-parse', 'HEAD')
    context = os.environ.get('GITHUB_SHA', '')
    workflow = os.environ.get('GITHUB_WORKFLOW_SHA', '')
    workflow_ref = os.environ.get('GITHUB_WORKFLOW_REF', '')
    run = os.environ.get('GITHUB_RUN_ID', '')
    attempt = os.environ.get('GITHUB_RUN_ATTEMPT', '')
    if (not preflight.SHA.fullmatch(expected) or expected != executor or expected != context or
        expected != workflow or os.environ.get('GITHUB_REPOSITORY') != 'FS-GG/FS.GG.Rendering' or
        workflow_ref != 'FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main' or
        not re.fullmatch('[1-9][0-9]*', run) or not re.fullmatch('[1-9][0-9]*', attempt)):
        preflight.fail('selected Stage D executor/context identity refused')
    return {'expectedExecutorSha': expected, 'executorSha': executor,
            'workflowContextSha': context, 'workflowSha': workflow,
            'workflowRef': workflow_ref, 'run': run, 'attempt': attempt}


def acquire_selected(root: Path, binding: dict) -> None:
    executor_identity(root)
    # GET only; GH_TOKEN is inherited through the environment, never an argument or receipt.
    if os.environ.get('GITHUB_REPOSITORY')!='FS-GG/FS.GG.Rendering': preflight.fail('selected reader context refused')
    out=root/'artifacts/attempt'; out.mkdir(parents=True,exist_ok=True)
    def api(path):
        return subprocess.check_output(['gh','api','repos/FS-GG/FS.GG.Rendering/'+path],stderr=subprocess.DEVNULL,timeout=60)
    def packet(run_id,run_attempt,artifact_id,sha,outer_hash,size,path):
        run=json.loads(api(f'actions/runs/{run_id}')); meta=json.loads(api(f'actions/artifacts/{artifact_id}'))
        if run.get('id')!=run_id or run.get('run_attempt')!=run_attempt or run.get('head_sha')!=sha or run.get('conclusion')!='success' or run.get('path')!=path:
            preflight.fail('selected native run identity/result refused')
        joined=meta.get('workflow_run',{})
        if meta.get('id')!=artifact_id or meta.get('expired') is not False or joined.get('id')!=run_id or joined.get('head_sha')!=sha or meta.get('digest')!='sha256:'+outer_hash or meta.get('size_in_bytes')!=size:
            preflight.fail('selected native artifact metadata refused')
        raw=api(f'actions/artifacts/{artifact_id}/zip')
        if len(raw)!=size or hashlib.sha256(raw).hexdigest()!=outer_hash: preflight.fail('selected artifact bytes differ')
        return raw
    raw=packet(binding['producerRun'],binding['producerAttempt'],binding['producerArtifact'],PRODUCER,OUTER,binding['outerSizeBytes'],'.github/workflows/release.yml')
    original=out/'original.zip';original.write_bytes(raw)
    archives=root/'artifacts/packages';archives.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(original) as z:
        names=[n for n in z.namelist() if n.endswith('.nupkg') or Path(n).name=='release-custody.json']
        if len(names)!=20 or len({Path(n).name for n in names})!=20: preflight.fail('original19 archive roster refused')
        for n in names:(archives/Path(n).name).write_bytes(z.read(n))
    if hashlib.sha256((archives/'release-custody.json').read_bytes()).hexdigest()!=CUSTODY:preflight.fail('original custody digest differs')
    original_plan=out/'producer-plan.json';original_plan.write_bytes(source_bytes(root,PRODUCER,binding['planPath']))
    subprocess.run(['python3',str(root/'scripts/release-custody.py'),'verify','--plan',str(original_plan),'--archives',str(archives),'--manifest',str(archives/'release-custody.json'),'--source-sha',PRODUCER],check=True)
    q=binding['qualification']
    raw=packet(q['run'],q['attempt'],q['artifact'],q['callerSha'],q['outerSha256'],q['outerSizeBytes'],'.github/workflows/templates-external-reference-qualification.yml')
    proof=out/'qualification.zip';proof.write_bytes(raw)
    with zipfile.ZipFile(proof) as z:
        def read(name):
            matches=[n for n in z.namelist() if Path(n).name==name]
            if len(matches)!=1:preflight.fail('native qualification evidence missing/duplicated')
            return z.read(matches[0])
        counts,providers=validate_qualification_payload(root,binding,read,archives)
    facts={**executor_identity(root),'originalCustodyVerified':True,'nativeTemplatesQualified':True,'providerPassed':providers,'browserPassed':counts,'browserUnexpected':0,'browserSkipped':0,'browserFlaky':0,'githubEffectiveWrite':'unknown-before-attempt'}
    (out/'qualification-facts.json').write_text(json.dumps(facts,sort_keys=True)+'\n')
    (out/'input-binding.json').write_text(json.dumps({'producerSha':PRODUCER,**executor_identity(root),'run':os.environ['GITHUB_RUN_ID'],'attempt':os.environ['GITHUB_RUN_ATTEMPT'],'originalPlanSha256':PLAN_HASH,'outerSha256':OUTER,'custodySha256':CUSTODY,'qualification':q,'publication':'not-yet-observed'},sort_keys=True)+'\n')

def record_event(root: Path, feed: str, package: str, stage: str, outcome: str) -> None:
    ids=[p['id'] for p in json.loads(preflight.source_text(root,PRODUCER,'eng/release/svg-export-prefix-0.32.1.json'))['packages']]
    if feed not in ('github','nuget') or package not in ids+['all19','unknown'] or stage not in ('probe','compare','push','readback','interrupted') or outcome not in ('requested','acknowledged','matched','failed','absent','refused'):
        preflight.fail('publisher diagnostic fields refused')
    path=root/'artifacts/attempt/publisher-events.jsonl';path.parent.mkdir(parents=True,exist_ok=True)
    event={'schema':'fsgg.rendering.publisher-observation/v1','producerSha':PRODUCER,**executor_identity(root),'planSha256':PLAN_HASH,'custodySha256':CUSTODY,'utc':datetime.now(timezone.utc).isoformat(),'feed':feed,'packageId':package,'stage':stage,'outcome':outcome}
    with path.open('a',encoding='utf-8') as stream: stream.write(json.dumps(event,sort_keys=True)+'\n')


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo-root', type=Path, default=Path.cwd())
    parser.add_argument('--source-sha')
    parser.add_argument('--executor-only', action='store_true')
    parser.add_argument('--executor-fence', action='store_true')
    parser.add_argument('--plan', default='eng/release/svg-export-prefix-0.32.1.json')
    parser.add_argument('--version', default='')
    parser.add_argument('--require-publication-ready', action='store_true')
    parser.add_argument('--attempt-binding', action='store_true')
    parser.add_argument('--attempt-preflight', action='store_true')
    parser.add_argument('--record-event', action='store_true')
    for field in ('feed','package','stage','result'): parser.add_argument('--event-'+field)
    parser.add_argument('--acquire-bound-artifacts', action='store_true')
    args = parser.parse_args()
    try:
        root=args.repo_root.resolve()
        if args.executor_only or args.executor_fence or args.attempt_binding or args.record_event:
            identity=executor_identity(root)
            if args.executor_only:
                print(json.dumps(identity, sort_keys=True))
                return 0
        if args.record_event:
            record_event(root,args.event_feed,args.event_package,args.event_stage,args.event_result)
            return 0
        if args.attempt_preflight and (not args.attempt_binding or args.acquire_bound_artifacts or args.require_publication_ready):preflight.fail('attempt preflight cannot admit acquisition or effects')
        binding=validate_attempt(root,args.source_sha,not args.attempt_preflight) if args.attempt_binding else None
        result = validate(root, args.source_sha, args.plan, args.version, args.require_publication_ready and not args.attempt_binding)
        if binding is not None: result['planSha256']=binding['planSha256']
        if args.acquire_bound_artifacts:
            if binding is None: preflight.fail('selected acquisition requires attempt binding')
            acquire_selected(root,binding)
    except (OSError, ValueError, KeyError, TypeError) as error:
        preflight.fail(f'invalid release source/plan: {error}')
    print(json.dumps(result, sort_keys=True))
    return 0

# The successor is an additional closed subject of this canonical executor. Historical
# functions above retain their exact original bytes and module identities.
SUCCESSOR_ATTEMPT = 'eng/release/external-authority-recovery-0.32.2-attempt.json'
SUCCESSOR = {
    'schema': 'fsgg.rendering.publication-attempt/v1',
    'producerSha': '9b73395658d4683a067d150ca9441b04e802c4af',
    'planPath': 'eng/release/external-authority-recovery-0.32.2.json',
    'planSha256': '81571fc65ac117e90467428d2e50e103ec875aa33cb83b054d9056bf4129aa1a',
    'producerRun': 38200411661, 'producerAttempt': 1, 'producerArtifact': 11725317255,
    'outerSha256': '7b0532caf431cfa41acaf35555016cf126bc495c5a6e13fe7db6b8fbe7478cca',
    'outerSizeBytes': 15007722,
    'custodySha256': '85bb1dcdf0378d05ca536d5878b88dedc301ff1524d39d98966e23888aae0120',
    'version': '0.32.2', 'baselineVersion': '0.32.1',
    'githubEffectiveWrite': 'unknown-before-attempt',
    'effectId': 'rendering-0322-original38200411661-a1',
}


def validate_successor_attempt(root: Path, source_sha: str, require_ready: bool = True) -> dict:
    binding = json.loads((root / SUCCESSOR_ATTEMPT).read_text(), object_pairs_hook=preflight.unique_json_object)
    if source_sha != SUCCESSOR['producerSha'] or any(type(binding.get(k)) is not type(v) or binding.get(k) != v for k, v in SUCCESSOR.items()):
        preflight.fail('successor selected producer/artifact/plan/effect identity differs')
    if set(binding) != set(SUCCESSOR) | {'attemptReady', 'qualification', 'sourcePackQualification', 'toolchain'}:
        preflight.fail('successor attempt fields differ')
    if hashlib.sha256(source_bytes(root, source_sha, binding['planPath'])).hexdigest() != binding['planSha256']:
        preflight.fail('successor original raw plan bytes differ')
    if binding['toolchain'] != {'sdkVersion': '10.0.401', 'compilerSha256': '2bd6190eafa94f47739538467f5a7cd52de233a627789c69f07959d7578e4d6b', 'fsharpCoreSha256': '2232698f623c28b8bcca32fa3cc8943cdd8529b20b487b5938106aae1c2ab256', 'runtimeVersion': '10.0.12'}:
        preflight.fail('successor original toolchain identity differs')
    source_pack = binding['sourcePackQualification']
    if source_pack != {'run': 38200411661, 'attempt': 1, 'apiCompatPassed': 17, 'apiCompatBreaks': 0, 'scope': 'source-pack-only', 'rootReadbackSha256': '51219aa0559e7bfdb8499256db87ea86a6f8f5f22461dc88fa2ebeb1c8efa809'}:
        preflight.fail('successor accepted source-pack qualification differs')
    q = binding['qualification']
    qfields = {'status', 'templatesSha', 'callerSha', 'run', 'attempt', 'artifact', 'outerSha256', 'outerSizeBytes', 'qualificationSha256', 'observationSha256', 'rootReadbackSha256'}
    if not isinstance(q, dict) or set(q) != qfields or type(binding['attemptReady']) is not bool:
        preflight.fail('successor qualification/readiness shape refused')
    if binding['attemptReady']:
        if q['status'] != 'qualified': preflight.fail('successor native qualification not accepted')
        for k in ('templatesSha', 'callerSha'):
            if not preflight.SHA.fullmatch(str(q[k])): preflight.fail('successor genuine qualification source unknown')
        for k in ('run', 'artifact', 'outerSizeBytes'):
            if type(q[k]) is not int or q[k] <= 0: preflight.fail('successor genuine qualification artifact unknown')
        if q['outerSizeBytes'] > 16777216: preflight.fail('successor native qualification archive budget exceeded')
        if type(q['attempt']) is not int or q['attempt'] != 1: preflight.fail('successor original qualification attempt refused')
        for k in ('outerSha256', 'qualificationSha256', 'observationSha256', 'rootReadbackSha256'):
            if not re.fullmatch('[0-9a-f]{64}', str(q[k])): preflight.fail('successor accepted qualification digest unknown')
    elif q['status'] != 'pending' or any(q[k] is not None for k in qfields - {'status'}):
        preflight.fail('successor pending qualification must retain explicit unknowns')
    if require_ready and not binding['attemptReady']:
        preflight.fail('successor attempt pending: retained candidate journey and root readback unjoined')
    return binding


def validate_successor_qualification(root: Path, binding: dict, read, archives: Path) -> dict:
    q = binding['qualification']
    raw = read('qualification.json')
    if hashlib.sha256(raw).hexdigest() != q['qualificationSha256']: preflight.fail('successor qualification digest differs')
    result = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    native = result.get('nativePreflight', {})
    if (result.get('disposition') != 'passed' or result.get('templates', {}).get('revision') != q['templatesSha']
        or result.get('producer', {}).get('revision') != binding['producerSha']
        or result.get('producer', {}).get('custodyManifestSha256') != binding['custodySha256']
        or result.get('browserFamilies') != ['chromium', 'firefox', 'webkit'] or result.get('passedPerFamily') != 8
        or result.get('skipped') != 0 or result.get('renderingInputSource') != 'candidate'
        or result.get('templatesInputSource') != 'source-built-candidate'
        or result.get('renderingPublicInputsQualified') is not False): preflight.fail('successor exact source-built candidate qualification refused')
    caller = preflight.source_text(root, q['callerSha'], '.github/workflows/templates-external-reference-qualification.yml')
    if (re.findall(r'^    uses: FS-GG/FS.GG.Templates/\.github/workflows/fable-external-reference-source\.yml@([0-9a-f]{40})$', caller, re.M) != [q['templatesSha']]
        or re.findall(r'^      templates-source: ([0-9a-f]{40})$', caller, re.M) != [q['templatesSha']]
        or re.findall(r'^      rendering-input-source: ([a-z]+)$', caller, re.M) != ['candidate']): preflight.fail('successor native caller executable/checkout/mode join refused')
    context = native.get('caller', {})
    if native.get('requestedPhase') != 'full' or context != {'repository': 'FS-GG/FS.GG.Rendering', 'revision': q['callerSha'], 'run': str(q['run']), 'attempt': str(q['attempt'])}:
        preflight.fail('successor genuine full-phase caller receipt differs')
    selected = ['FS.GG.UI.Scene.0.32.2.nupkg', 'FS.GG.UI.KeyboardInput.0.32.2.nupkg', 'FS.GG.UI.Scene.SvgBrowser.0.32.2.nupkg']
    actual = result['producer']['archives']
    if len(actual) != 3 or {row['name']: row['sha256'] for row in actual} != {n: hashlib.sha256((archives / n).read_bytes()).hexdigest() for n in selected}:
        preflight.fail('successor three consumed archives differ from original candidate')
    for family in result['browserFamilies']:
        data = read(family + '-browser.json')
        if hashlib.sha256(data).hexdigest() != result['browserReportSha256'][family] or any(json.loads(data)['stats'].get(k) != v for k, v in [('expected', 8), ('unexpected', 0), ('skipped', 0), ('flaky', 0)]):
            preflight.fail('successor browser reports incomplete or changed')
    if sum(line.startswith('PASS ') for line in read('provider-composition.log').decode().splitlines()) != 186:
        preflight.fail('successor Provider requires exactly186 assertions')
    observation_raw = read('orca/observation.json')
    if hashlib.sha256(observation_raw).hexdigest() != q['observationSha256']: preflight.fail('successor accepted AT observation digest differs')
    observation = json.loads(observation_raw)
    if (observation.get('result') != 'passed' or observation.get('templates') != result['templates']
        or observation.get('caller') != context or observation.get('producer') != native.get('producer')
        or observation.get('physicalAudioHardware') != 'not-observed'
        or observation.get('cleanup', {}).get('disposition') != 'observed-empty'
        or observation['cleanup'].get('unknown') != [] or observation['cleanup'].get('remaining') != []
        or len(observation.get('actions', [])) != 15): preflight.fail('successor accepted15-action AT/custody join refused')
    for name, sha in observation['evidenceSha256'].items():
        if Path(name).name != name or hashlib.sha256(read('orca/' + name)).hexdigest() != sha: preflight.fail('successor AT evidence changed or unavailable')
    process = json.loads(read('orca/process-result.json'))
    bound = json.loads(read('orca/bind-result.json'))
    if process.get('exit') != 0 or process.get('firstCause') is not None or process.get('cleanup') != observation['cleanup'] or process.get('firstGuardCensus') is not None or bound.get('exit') != 0 or bound.get('firstCause') is not None:
        preflight.fail('successor original AT execution/binder/cleanup differs')
    return {'providerPassed': 186, 'browserPassed': [8, 8, 8], 'atPassed': 15, 'atCustodyQualified': True}


def acquire_successor(root: Path, binding: dict) -> None:
    identity = executor_identity(root)
    out = root / 'artifacts/attempt'; out.mkdir(parents=True, exist_ok=True)
    def api(path):
        return subprocess.check_output(['gh', 'api', 'repos/FS-GG/FS.GG.Rendering/' + path], stderr=subprocess.DEVNULL, timeout=60)
    def packet(run_id, attempt, artifact, source, sha, size, path):
        run = json.loads(api(f'actions/runs/{run_id}')); meta = json.loads(api(f'actions/artifacts/{artifact}'))
        if (run.get('id') != run_id or run.get('run_attempt') != attempt or run.get('head_sha') != source or run.get('conclusion') != 'success' or run.get('path') != path
            or meta.get('id') != artifact or meta.get('expired') is not False or meta.get('workflow_run', {}).get('id') != run_id
            or meta['workflow_run'].get('head_sha') != source or meta.get('digest') != 'sha256:' + sha or meta.get('size_in_bytes') != size): preflight.fail('successor original run/artifact metadata refused')
        raw = api(f'actions/artifacts/{artifact}/zip')
        if len(raw) != size or hashlib.sha256(raw).hexdigest() != sha: preflight.fail('successor original archive bytes differ')
        return raw
    raw = packet(binding['producerRun'], binding['producerAttempt'], binding['producerArtifact'], binding['producerSha'], binding['outerSha256'], binding['outerSizeBytes'], '.github/workflows/release.yml')
    (out / 'original.zip').write_bytes(raw)
    archives = root / 'artifacts/packages'; archives.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(out / 'original.zip') as z:
        names = z.namelist()
        if len(names) != 20 or len(set(names)) != 20 or any(Path(n).name != n for n in names) or set(names) != {r['file'] for r in json.loads(z.read('release-custody.json'))['archives']} | {'release-custody.json'}:
            preflight.fail('successor exact19 archives unsafe or substituted')
        for name in names: (archives / name).write_bytes(z.read(name))
    if hashlib.sha256((archives / 'release-custody.json').read_bytes()).hexdigest() != binding['custodySha256']: preflight.fail('successor original custody differs')
    original_plan = out / 'producer-plan.json'; original_plan.write_bytes(source_bytes(root, binding['producerSha'], binding['planPath']))
    subprocess.run(['python3', str(root / 'scripts/release-custody.py'), 'verify', '--plan', str(original_plan), '--archives', str(archives), '--manifest', str(archives / 'release-custody.json'), '--source-sha', binding['producerSha']], check=True)
    q = binding['qualification']
    proof = packet(q['run'], q['attempt'], q['artifact'], q['callerSha'], q['outerSha256'], q['outerSizeBytes'], '.github/workflows/templates-external-reference-qualification.yml')
    (out / 'qualification.zip').write_bytes(proof)
    with zipfile.ZipFile(out / 'qualification.zip') as z:
        def read(name):
            matches = [n for n in z.namelist() if n == name or n.endswith('/' + name)]
            if len(matches) != 1: preflight.fail('successor qualification evidence missing or duplicated')
            return z.read(matches[0])
        current = validate_successor_qualification(root, binding, read, archives)
    facts = {**identity, **current, 'originalCustodyVerified': True, 'nativeTemplatesQualified': True, 'browserUnexpected': 0, 'browserSkipped': 0, 'browserFlaky': 0, 'githubEffectiveWrite': 'unknown-before-attempt'}
    (out / 'qualification-facts.json').write_text(json.dumps(facts, sort_keys=True) + '\n')
    (out / 'input-binding.json').write_text(json.dumps({**identity, 'producerSha': binding['producerSha'], 'originalPlanSha256': binding['planSha256'], 'outerSha256': binding['outerSha256'], 'custodySha256': binding['custodySha256'], 'effectId': binding['effectId'], 'qualification': q, 'publication': 'not-yet-observed'}, sort_keys=True) + '\n')


# Separate qualified subject; original source-pack and native AT observations remain distinct.
FONT_DISPOSAL_ATTEMPT = 'eng/release/font-disposal-0.32.3-attempt.json'
FONT_DISPOSAL = {'schema': 'fsgg.rendering.publication-attempt/v1', 'producerSha': '4ec6eafbbbf02264db9b0db4780c590abe0bf9cd', 'planPath': 'eng/release/font-disposal-0.32.3.json', 'planSha256': '89369de0c0944c66a52c6ed914cef78d6bb7670866baedf85cd8a198c5c46239', 'producerRun': 39509314882, 'producerAttempt': 1, 'producerArtifact': 12305198394, 'outerSha256': '84647332c124e511545787c8c3f8634d4f5177068521acfc86c93430ac73ef63', 'outerSizeBytes': 15073469, 'custodySha256': '7dbd4329546932c4c1d12767550b7b78aae85e32c81b354935b2c615b3e298c9', 'version': '0.32.3', 'baselineVersion': '0.32.1', 'githubEffectiveWrite': 'unknown-before-attempt', 'effectId': 'rendering-0323-original39509314882-a1'}
FONT_DISPOSAL_BINDING_SHA256 = '4d665ab254ef012ea354106262ad6874b818ec68da9d46dad6fcdb08471e9701'
FONT_DISPOSAL_QUALIFICATION = {'status': 'qualified',
 'templatesSha': '752a2ac1fa996c3235ac0330ffbcefb6b8301cb4',
 'callerSha': '4e48c97533554ccece8db3db828c4cc327464050',
 'run': 39531420324,
 'attempt': 1,
 'artifact': 12315224479,
 'outerSha256': 'd6df73c1baf26d7a75a7db7e13e0c0c55429a69e150ea4581f8afcbabb9aa2c5',
 'outerSizeBytes': 728382,
 'qualificationSha256': 'd730c961b79bd8b5185ceb81d970dfdd4649c083ad669dcc52693311b5975dd1',
 'observationSha256': '6a555861231b4c3e881298513aa71a320692589cb6cada85c682a1e442813ad1',
 'sourceQualificationSha256': 'b79e2a2939f4603fa914bbdf1e5a832390dd05448e6b30da1b6e2a0bad3f056a',
 'rootReadbackSha256': '7dab9d3473110961508570af4ad7e95cbc05918dde4b23f2a61a64c8f6d02c4f'}

def validate_font_disposal_attempt(root: Path, source_sha: str, require_ready: bool = True) -> dict:
    raw = (root / FONT_DISPOSAL_ATTEMPT).read_bytes()
    binding = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    if hashlib.sha256(raw).hexdigest() != FONT_DISPOSAL_BINDING_SHA256 or source_sha != FONT_DISPOSAL['producerSha']:
        preflight.fail('font-disposal immutable original binding differs')
    if any(type(binding.get(k)) is not type(v) or binding.get(k) != v for k, v in FONT_DISPOSAL.items()):
        preflight.fail('font-disposal selected producer/artifact identity differs')
    if hashlib.sha256(source_bytes(root, source_sha, binding['planPath'])).hexdigest() != binding['planSha256']:
        preflight.fail('font-disposal original raw plan differs')
    if binding.get('qualification') != FONT_DISPOSAL_QUALIFICATION or binding.get('attemptReady') is not True:
        preflight.fail('font-disposal accepted native qualification/readiness differs')
    return binding

FONT_DISPOSAL_FONT_CASES = ['Studio awaits genuine Noto readiness and retains the mounted font through replacement and redo',
 'Studio does not announce or mount Noto before native readiness is delivered',
 'Studio reports a font diagnostic without committing text and disposes the failed activation',
 'Studio timeout prevents a late genuine font load from mounting or leaking',
 'Studio replacement prevents a late genuine font load from mounting or leaking',
 'Studio stale font completion cannot replace or cancel a newer activation',
 'Studio unload releases its mounted native font and Blob']
FONT_DISPOSAL_CASES = [('external-command-outcomes.spec.ts',
  ('external-command-outcomes.spec.ts', 'external authority reference command outcomes'),
  'unknown receipt survives projection and reconnect without replay'),
 ('external-command-outcomes.spec.ts',
  ('external-command-outcomes.spec.ts', 'external authority reference command outcomes'),
  'explicit reconciliation and rearm retain the original and use a fresh correlation'),
 ('external-command-outcomes.spec.ts',
  ('external-command-outcomes.spec.ts', 'external authority reference command outcomes'),
  'old generation completion is inert and abandonment requires explicit rearm'),
 ('external-command-outcomes.spec.ts',
  ('external-command-outcomes.spec.ts', 'external authority reference command outcomes'),
  'delayed completion settles once and disposal makes retained controls inert'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'FourD reference composes local session, normalized input, transformed hit testing and settled '
  'disposal'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'external authority reference fences captured snapshots and settles commands independently'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'external authority reference recovers failures and disposes owned resources terminally'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'external authority reference uses production keyboard pointer editing and IME input'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio awaits genuine Noto readiness and retains the mounted font through replacement and redo'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio does not announce or mount Noto before native readiness is delivered'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio reports a font diagnostic without committing text and disposes the failed activation'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio timeout prevents a late genuine font load from mounting or leaking'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio replacement prevents a late genuine font load from mounting or leaking'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio stale font completion cannot replace or cancel a newer activation'),
 ('two-client.spec.ts',
  ('two-client.spec.ts',),
  'Studio unload releases its mounted native font and Blob')]
FONT_DISPOSAL_ACTIONS = ['Mount external authority reference',
 'Connect sample authority',
 'Complete current external snapshot',
 'Lose next command receipt',
 'Increment external value',
 'Rearm sample commands',
 'Increment external value',
 'Reconcile unknown command',
 'Disconnect sample authority',
 'Connect sample authority',
 'Complete current external snapshot',
 'Rearm sample commands',
 'Increment external value',
 'Dispose external reference',
 'Increment external value']

def validate_font_zip(archive) -> None:
    """Bound original ZIP inventory before reading any payload; never extract paths."""
    infos = archive.infolist()
    names = [entry.filename for entry in infos]
    if (not 1 <= len(infos) <= 512 or len(set(names)) != len(names)
        or sum(entry.file_size for entry in infos) > 134217728):
        preflight.fail('font-disposal ZIP inventory exceeds closed bounds')
    for entry in infos:
        name = entry.filename
        if (not name or name.startswith('/') or '\\' in name or '\x00' in name
            or any(part in ('', '.', '..') for part in name.rstrip('/').split('/'))
            or ':' in name or entry.file_size > 16777216 or entry.flag_bits & 1
            or (entry.external_attr >> 16) & 0o170000 not in (0, 0o100000, 0o040000)):
            preflight.fail('font-disposal ZIP member unsafe or oversized')


def validate_font_browser(data: dict) -> None:
    if any(data.get('stats', {}).get(k) != v for k, v in [('expected', 15), ('unexpected', 0), ('skipped', 0), ('flaky', 0)]):
        preflight.fail('font-disposal browser exact15 single attempts required')
    found = []
    def walk(suite, ancestors=()):
        title = suite.get('title', '')
        ancestry = ancestors + ((title,) if title else ())
        for spec in suite.get('specs', []):
            identity = (suite.get('file'), ancestry, spec.get('title'))
            tests = spec.get('tests', [])
            if len(tests) != 1 or tests[0].get('status') != 'expected' or len(tests[0].get('results', [])) != 1:
                preflight.fail('font-disposal browser duplicate/retried test refused')
            result = tests[0]['results'][0]
            if result.get('status') != 'passed' or result.get('retry') != 0:
                preflight.fail('font-disposal browser test did not pass first attempt')
            found.append(identity)
        for child in suite.get('suites', []): walk(child, ancestry)
    walk(data)
    if len(found) != 15 or set(found) != set(FONT_DISPOSAL_CASES):
        preflight.fail('font-disposal exact file/suite/leaf identities differ')


def validate_font_disposal_qualification(root: Path, binding: dict, read, archives: Path) -> dict:
    q = binding['qualification']
    raw = read('qualification.json')
    if hashlib.sha256(raw).hexdigest() != q['qualificationSha256']:
        preflight.fail('font-disposal accepted qualification digest differs')
    result = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    native = result.get('nativePreflight', {})
    context = {'repository': 'FS-GG/FS.GG.Rendering', 'revision': q['callerSha'], 'run': str(q['run']), 'attempt': str(q['attempt'])}
    producer = {'revision': binding['producerSha'], 'run': str(binding['producerRun']), 'attempt': str(binding['producerAttempt']), 'artifact': str(binding['producerArtifact']), 'archiveSha256': binding['outerSha256'], 'custodySha256': binding['custodySha256'], 'version': binding['version'], 'plan': binding['planPath'], 'rawPlanSha256': binding['planSha256']}
    if (result.get('disposition') != 'passed' or result.get('templates', {}).get('revision') != q['templatesSha']
        or result.get('producer', {}).get('revision') != binding['producerSha']
        or result['producer'].get('custodyManifestSha256') != binding['custodySha256']
        or result.get('browserFamilies') != ['chromium', 'firefox', 'webkit'] or result.get('passedPerFamily') != 15
        or result.get('studioFontCases') != FONT_DISPOSAL_FONT_CASES
        or result.get('skipped') != 0 or result.get('qualificationMode') != 'font-disposal'
        or result.get('renderingInputSource') != 'candidate' or result.get('templatesInputSource') != 'source-built-candidate'
        or result.get('actualScreenReader') != 'not-observed' or result.get('publication') is not False
        or result.get('installedAcceptance') is not False or result.get('renderingPublicInputsQualified') is not False
        or native.get('requestedPhase') != 'full' or native.get('caller') != context or native.get('producer') != producer):
        preflight.fail('font-disposal exact full-phase original candidate joins refused')
    caller = preflight.source_text(root, q['callerSha'], '.github/workflows/templates-external-reference-qualification.yml')
    job = caller.split('\n  qualify-font-disposal:', 1)
    if len(job) != 2 or re.findall(r'(?m)^    uses: FS-GG/FS.GG.Templates/.github/workflows/fable-external-reference-source.yml@([0-9a-f]{40})$', job[1]) != [q['templatesSha']] or re.findall(r'(?m)^      templates-source: ([0-9a-f]{40})$', job[1]) != [q['templatesSha']] or '\n      rendering-input-source: font-disposal\n' not in job[1]:
        preflight.fail('font-disposal actual caller job/callee source differs')
    selected = ['FS.GG.UI.Scene.0.32.3.nupkg', 'FS.GG.UI.KeyboardInput.0.32.3.nupkg', 'FS.GG.UI.Scene.SvgBrowser.0.32.3.nupkg']
    consumed = result['producer'].get('archives', [])
    if len(consumed) != 3 or {row['name']: row['sha256'] for row in consumed} != {name: hashlib.sha256((archives / name).read_bytes()).hexdigest() for name in selected}:
        preflight.fail('font-disposal consumed original archives differ')
    for family in result['browserFamilies']:
        data = read(family + '-browser.json')
        if hashlib.sha256(data).hexdigest() != result['browserReportSha256'][family]:
            preflight.fail('font-disposal browser report digest differs')
        validate_font_browser(json.loads(data, object_pairs_hook=preflight.unique_json_object))
    if sum(line.startswith('PASS ') for line in read('provider-composition.log').decode().splitlines()) != 186:
        preflight.fail('font-disposal Provider requires exactly186 assertions')
    source = read('orca/source-qualification.json')
    if hashlib.sha256(source).hexdigest() != q['sourceQualificationSha256']:
        preflight.fail('font-disposal separate source qualification differs')
    sq = json.loads(source)
    if sq.get('actualScreenReader') != 'not-observed' or sq.get('producer') != result['producer'] or sq.get('templates') != result['templates']:
        preflight.fail('font-disposal source qualification cannot claim native AT')
    raw = read('orca/observation.json')
    if hashlib.sha256(raw).hexdigest() != q['observationSha256']:
        preflight.fail('font-disposal accepted AT observation digest differs')
    observation = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    actions = observation.get('actions', [])
    if (observation.get('result') != 'passed' or observation.get('templates') != result['templates']
        or observation.get('caller') != context or observation.get('producer') != producer
        or observation.get('sourceQualificationSha256') != q['sourceQualificationSha256']
        or observation.get('keyboard') != 'xdotool-X11' or observation.get('speechBoundary') != 'Orca SPEECH OUTPUT'
        or observation.get('physicalAudioHardware') != 'not-observed'
        or observation.get('cleanup', {}).get('disposition') != 'observed-empty'
        or observation['cleanup'].get('unknown') != [] or observation['cleanup'].get('remaining') != []
        or [action.get('name') for action in actions] != FONT_DISPOSAL_ACTIONS
        or any(action.get('key') != 'Return' or action.get('atspiFocused') is not True or action.get('directReferenceControl') is not True for action in actions)
        or any(type(action.get('eventSequence')) is not int for action in actions)
        or any(a['eventSequence'] >= b['eventSequence'] for a, b in zip(actions, actions[1:]))):
        preflight.fail('font-disposal accepted15-key AT/custody join refused')
    evidence = observation.get('evidenceSha256', {})
    if len(evidence) != 27: preflight.fail('font-disposal exact27 AT evidence members required')
    for name, sha in evidence.items():
        if Path(name).name != name or not re.fullmatch('[0-9a-f]{64}', str(sha)) or hashlib.sha256(read('orca/' + name)).hexdigest() != sha:
            preflight.fail('font-disposal AT evidence changed or unavailable')
    process = json.loads(read('orca/process-result.json')); bound = json.loads(read('orca/bind-result.json'))
    companion = json.loads(read('orca/browser-companion-stop-result.json'))
    if (process.get('exit') != 0 or process.get('firstCause') is not None or process.get('cleanup') != observation['cleanup']
        or not isinstance(process.get('knownStarts'), dict) or len(process['knownStarts']) != 57 or process.get('reportingErrors') != []
        or process.get('firstGuardCensus') is not None or process.get('firstRssBudgetCensus') is not None
        or bound.get('exit') != 0 or bound.get('firstCause') is not None or bound.get('runtimeExit') != 0 or bound.get('cleanup') != observation['cleanup']
        or companion.get('exit') != 0 or companion.get('firstCause') is not None or companion.get('originalBirthAbsent') is not True or companion.get('reaped') is not True):
        preflight.fail('font-disposal original AT/binder/companion retirement differs')
    speech = read('orca/orca-debug.log')
    for phase in ('unknown', 'reconciled', 'rearmed', 'disposed'):
        window = observation.get('observations', {}).get(phase, {})
        start, end = window.get('speechStart'), window.get('speechEnd')
        if type(start) is not int or type(end) is not int or not 0 <= start < end <= len(speech) or b'SPEECH OUTPUT' not in speech[start:end]:
            preflight.fail('font-disposal four genuine speech windows required')
    return {'providerPassed': 186, 'browserPassed': [15, 15, 15], 'atPassed': 15, 'atSpeechWindows': 4, 'atEvidenceMembers': 27, 'atCustodyQualified': True, 'scope': 'sampled known-birth cleanup; no global containment or physical audio claim'}

def acquire_font_disposal(root: Path, binding: dict) -> None:
    identity = executor_identity(root)
    settlement = accepted_tag_settlement(root)
    out = root / 'artifacts/attempt'; out.mkdir(parents=True, exist_ok=True)
    def api(path):
        return subprocess.check_output(['gh', 'api', 'repos/FS-GG/FS.GG.Rendering/' + path], stderr=subprocess.DEVNULL, timeout=60)
    def packet(run_id, attempt, artifact, source, sha, size, path):
        run = json.loads(api(f'actions/runs/{run_id}')); meta = json.loads(api(f'actions/artifacts/{artifact}'))
        if (run.get('id') != run_id or run.get('run_attempt') != attempt or run.get('head_sha') != source or run.get('conclusion') != 'success' or run.get('path') != path
            or meta.get('id') != artifact or meta.get('expired') is not False or meta.get('workflow_run', {}).get('id') != run_id
            or meta['workflow_run'].get('head_sha') != source or meta.get('digest') != 'sha256:' + sha or meta.get('size_in_bytes') != size): preflight.fail('font-disposal original run/artifact metadata refused')
        raw = api(f'actions/artifacts/{artifact}/zip')
        if len(raw) != size or hashlib.sha256(raw).hexdigest() != sha: preflight.fail('font-disposal original archive bytes differ')
        return raw
    raw = packet(binding['producerRun'], binding['producerAttempt'], binding['producerArtifact'], binding['producerSha'], binding['outerSha256'], binding['outerSizeBytes'], '.github/workflows/release.yml')
    (out / 'original.zip').write_bytes(raw)
    archives = root / 'artifacts/packages'; archives.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(out / 'original.zip') as z:
        validate_font_zip(z)
        names = z.namelist()
        if len(names) != 20 or len(set(names)) != 20 or any(Path(n).name != n for n in names) or set(names) != {r['file'] for r in json.loads(z.read('release-custody.json'))['archives']} | {'release-custody.json'}:
            preflight.fail('font-disposal exact19 archives unsafe or substituted')
        for name in names: (archives / name).write_bytes(z.read(name))
    if hashlib.sha256((archives / 'release-custody.json').read_bytes()).hexdigest() != binding['custodySha256']: preflight.fail('font-disposal original custody differs')
    original_plan = out / 'producer-plan.json'; original_plan.write_bytes(source_bytes(root, binding['producerSha'], binding['planPath']))
    subprocess.run(['python3', str(root / 'scripts/release-custody.py'), 'verify', '--plan', str(original_plan), '--archives', str(archives), '--manifest', str(archives / 'release-custody.json'), '--source-sha', binding['producerSha']], check=True)
    q = binding['qualification']
    proof = packet(q['run'], q['attempt'], q['artifact'], q['callerSha'], q['outerSha256'], q['outerSizeBytes'], '.github/workflows/templates-external-reference-qualification.yml')
    (out / 'qualification.zip').write_bytes(proof)
    with zipfile.ZipFile(out / 'qualification.zip') as z:
        validate_font_zip(z)
        def read(name):
            matches = [n for n in z.namelist() if n == name or n.endswith('/' + name)]
            if len(matches) != 1: preflight.fail('font-disposal qualification evidence missing or duplicated')
            return z.read(matches[0])
        current = validate_font_disposal_qualification(root, binding, read, archives)
    facts = {**identity, **current, 'originalCustodyVerified': True, 'nativeTemplatesQualified': True, 'browserUnexpected': 0, 'browserSkipped': 0, 'browserFlaky': 0, 'githubEffectiveWrite': 'unknown-before-attempt'}
    (out / 'qualification-facts.json').write_text(json.dumps(facts, sort_keys=True) + '\n')
    (out / 'input-binding.json').write_text(json.dumps({**identity, 'producerSha': binding['producerSha'], 'originalPlanSha256': binding['planSha256'], 'outerSha256': binding['outerSha256'], 'custodySha256': binding['custodySha256'], 'effectId': binding['effectId'], 'qualification': q, 'tagSettlement': settlement, 'publication': 'not-yet-observed'}, sort_keys=True) + '\n')


SETTLEMENT_FIELDS = ('run', 'attempt', 'artifact', 'artifactSha256', 'receiptSha256', 'executorSha')
SETTLEMENT_RECEIPT = 'artifacts/attempt/accepted-tag-settlement.json'


def selected_tag_settlement() -> dict:
    """Parse only the root-selected settlement coordinates; no transport."""
    try:
        selected = json.loads(os.environ.get('FSGG_ACCEPTED_TAG_SETTLEMENT', '{}'))
    except ValueError:
        preflight.fail('held0323 requires explicit accepted settlement coordinates')
    if not isinstance(selected, dict) or set(selected) != set(SETTLEMENT_FIELDS) or any(type(selected[k]) is not int or selected[k] < 1 for k in ('run', 'attempt', 'artifact')) or selected['attempt'] != 1:
        preflight.fail('held0323 requires explicit accepted settlement coordinates')
    if any(type(selected[k]) is not str or not re.fullmatch('[0-9a-f]{64}', selected[k]) for k in ('artifactSha256', 'receiptSha256')) or type(selected['executorSha']) is not str or not re.fullmatch('[0-9a-f]{40}', selected['executorSha']):
        preflight.fail('accepted settlement digest/source malformed')
    return selected


def settlement_refs() -> dict:
    return {'refs/tags/' + name: FONT_DISPOSAL['producerSha'] for name in ('fs-gg-ui/v0.32.3', 'fs-gg-ui-template/v0.32.3', 'v0.32.3')}


def settlement_claims_refused(receipt, selected: dict) -> bool:
    """Pure receipt assertions shared by native acquisition and the cached record path."""
    return (not isinstance(receipt, dict) or receipt.get('schema') != 'fsgg.rendering.tag-settlement/v1' or receipt.get('effectId') != FONT_DISPOSAL['effectId'] or receipt.get('producerSha') != FONT_DISPOSAL['producerSha']
        or receipt.get('originalTransportOutcome') != 'Unknown' or receipt.get('sameIntentConvergence') != 'Verified' or receipt.get('cleanup') != 'restored' or receipt.get('pendingSubordinateRefs') != [] or receipt.get('result') != 'passed'
        or receipt.get('executorSha') != selected['executorSha'] or receipt.get('run') != str(selected['run']) or receipt.get('attempt') != '1'
        or receipt.get('workflowRef') != 'FS-GG/FS.GG.Rendering/.github/workflows/release-tag-settlement.yml@refs/heads/main'
        or receipt.get('refs') != settlement_refs() or receipt.get('publication') != 'not-performed' or receipt.get('notification') != 'not-performed')


def accepted_tag_settlement(root: Path) -> dict:
    """Acquire only the root-selected successful settlement; never settle during publish."""
    selected = selected_tag_settlement()
    def api(path):
        return subprocess.check_output(['gh', 'api', 'repos/FS-GG/FS.GG.Rendering/' + path], stderr=subprocess.DEVNULL, timeout=30)
    run = json.loads(api('actions/runs/' + str(selected['run'])))
    meta = json.loads(api('actions/artifacts/' + str(selected['artifact'])))
    if (run.get('id') != selected['run'] or run.get('run_attempt') != 1 or run.get('status') != 'completed' or run.get('conclusion') != 'success'
        or run.get('event') != 'workflow_dispatch' or run.get('head_sha') != selected['executorSha'] or run.get('path') != '.github/workflows/release-tag-settlement.yml'
        or meta.get('id') != selected['artifact'] or meta.get('expired') is not False or meta.get('digest') != 'sha256:' + selected['artifactSha256']
        or meta.get('workflow_run', {}).get('id') != selected['run'] or meta.get('workflow_run', {}).get('head_sha') != selected['executorSha']
        or not 0 < meta.get('size_in_bytes', 0) <= 8 * 1024 * 1024):
        preflight.fail('accepted settlement native provenance differs')
    raw = api('actions/artifacts/' + str(selected['artifact']) + '/zip')
    if len(raw) != meta['size_in_bytes'] or hashlib.sha256(raw).hexdigest() != selected['artifactSha256']:
        preflight.fail('accepted settlement archive differs')
    import io
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        validate_font_zip(z)
        names = [n for n in z.namelist() if n == 'settlement.json' or n.endswith('/settlement.json')]
        if len(names) != 1: preflight.fail('accepted settlement receipt missing or duplicated')
        receipt_raw = z.read(names[0])
    if hashlib.sha256(receipt_raw).hexdigest() != selected['receiptSha256']:
        preflight.fail('accepted settlement receipt bytes differ')
    receipt = json.loads(receipt_raw)
    refs = settlement_refs()
    if settlement_claims_refused(receipt, selected):
        preflight.fail('accepted settlement independent assertions/cleanup differ')
    for ref, sha in refs.items():
        current = json.loads(api('git/ref/' + ref.removeprefix('refs/')))
        if current.get('ref') != ref or current.get('object', {}).get('type') != 'commit' or current.get('object', {}).get('sha') != sha:
            preflight.fail('accepted settlement current immutable ref differs')
    out = root / 'artifacts/attempt'; out.mkdir(parents=True, exist_ok=True)
    (out / 'accepted-tag-settlement.json').write_bytes(receipt_raw)
    return {**selected, 'originalTransportOutcome': 'Unknown', 'sameIntentConvergence': 'Verified', 'cleanup': 'restored', 'effectId': FONT_DISPOSAL['effectId']}


EXECUTOR_FIELDS = ('expectedExecutorSha', 'executorSha', 'workflowContextSha', 'workflowSha', 'workflowRef', 'run', 'attempt')


def verified_cached_tag_settlement(root: Path, identity: dict) -> dict:
    """Publisher observation path: reuse the settlement this job already authenticated.

    Native provenance, archive bytes and live immutable refs were verified once by acquisition
    in this same job (and joined again by the F# precredential entry). Each of ~191 event
    records must not repeat those GITHUB_TOKEN reads: they would exhaust the per-repository
    rate limit inside the feed loops. This path performs NO transport; it re-joins the cached
    receipt bytes to the selected digest, the receipt claims, and this job's input binding.
    """
    selected = selected_tag_settlement()
    try:
        raw = (root / SETTLEMENT_RECEIPT).read_bytes()
        binding = json.loads((root / 'artifacts/attempt/input-binding.json').read_bytes(), object_pairs_hook=preflight.unique_json_object)
        receipt = json.loads(raw, object_pairs_hook=preflight.unique_json_object)
    except (OSError, ValueError):
        preflight.fail('cached accepted settlement unavailable; acquisition must authenticate it first')
    if hashlib.sha256(raw).hexdigest() != selected['receiptSha256']:
        preflight.fail('cached accepted settlement bytes differ from the selected receipt')
    joined = binding.get('tagSettlement') if isinstance(binding, dict) else None
    if (not isinstance(joined, dict) or any(joined.get(k) != selected[k] for k in SETTLEMENT_FIELDS)
            or joined.get('effectId') != FONT_DISPOSAL['effectId'] or joined.get('sameIntentConvergence') != 'Verified'
            or binding.get('effectId') != FONT_DISPOSAL['effectId'] or binding.get('producerSha') != FONT_DISPOSAL['producerSha']
            or set(identity) != set(EXECUTOR_FIELDS) or any(binding.get(k) != identity[k] for k in EXECUTOR_FIELDS)):
        preflight.fail('cached accepted settlement was not authenticated by this attempt')
    if settlement_claims_refused(receipt, selected):
        preflight.fail('cached accepted settlement assertions differ')
    return {**selected, 'originalTransportOutcome': 'Unknown', 'sameIntentConvergence': 'Verified', 'cleanup': 'restored', 'effectId': FONT_DISPOSAL['effectId']}


def font_disposal_main() -> int:
    parser = argparse.ArgumentParser(description='Closed retained0.32.3 subject of the canonical executor')
    parser.add_argument('--font-disposal-attempt', action='store_true', required=True)
    parser.add_argument('--repo-root', type=Path, default=Path.cwd())
    parser.add_argument('--source-sha', default=FONT_DISPOSAL['producerSha'])
    parser.add_argument('--plan', default=FONT_DISPOSAL['planPath'])
    parser.add_argument('--version', default=FONT_DISPOSAL['version'])
    parser.add_argument('--executor-fence', action='store_true')
    parser.add_argument('--attempt-preflight', action='store_true')
    parser.add_argument('--acquire-bound-artifacts', action='store_true')
    parser.add_argument('--record-event', action='store_true')
    for field in ('feed', 'package', 'stage', 'result'): parser.add_argument('--event-' + field)
    args = parser.parse_args()
    root = args.repo_root.resolve()
    identity = executor_identity(root)
    if args.plan != FONT_DISPOSAL['planPath'] or args.version != FONT_DISPOSAL['version'] or (args.attempt_preflight and (args.acquire_bound_artifacts or args.record_event)):
        preflight.fail('font-disposal mixed source/plan/effect route refused')
    binding = validate_font_disposal_attempt(root, args.source_sha, not args.attempt_preflight)
    result = validate(root, args.source_sha, args.plan, args.version, False)
    result['planSha256'] = binding['planSha256']
    if not args.attempt_preflight and not args.acquire_bound_artifacts:
        # Event records reuse this job's authenticated receipt; other entries authenticate natively.
        if args.record_event: verified_cached_tag_settlement(root, identity)
        else: accepted_tag_settlement(root)
    if args.acquire_bound_artifacts: acquire_font_disposal(root, binding)
    if args.record_event:
        ids = [p['id'] for p in json.loads(source_bytes(root, binding['producerSha'], binding['planPath']))['packages']]
        if args.event_feed not in ('github', 'nuget') or args.event_package not in ids + ['all19', 'unknown'] or args.event_stage not in ('probe', 'compare', 'push', 'readback', 'interrupted') or args.event_result not in ('requested', 'acknowledged', 'matched', 'failed', 'absent', 'refused'):
            preflight.fail('font-disposal publisher observation fields refused')
        event = {'schema': 'fsgg.rendering.publisher-observation/v1', **identity, 'producerSha': binding['producerSha'], 'planSha256': binding['planSha256'], 'custodySha256': binding['custodySha256'], 'effectId': binding['effectId'], 'utc': datetime.now(timezone.utc).isoformat(), 'feed': args.event_feed, 'packageId': args.event_package, 'stage': args.event_stage, 'outcome': args.event_result}
        path = root / 'artifacts/attempt/publisher-events.jsonl'; path.parent.mkdir(parents=True, exist_ok=True)
        with path.open('a') as stream: stream.write(json.dumps(event, sort_keys=True) + '\n')
        return 0
    print(json.dumps(result, sort_keys=True))
    return 0


def successor_main() -> int:
    parser = argparse.ArgumentParser(description='Closed retained0.32.2 subject of the canonical executor')
    parser.add_argument('--successor-attempt', action='store_true', required=True)
    parser.add_argument('--repo-root', type=Path, default=Path.cwd())
    parser.add_argument('--source-sha', default=SUCCESSOR['producerSha'])
    parser.add_argument('--plan', default=SUCCESSOR['planPath'])
    parser.add_argument('--version', default=SUCCESSOR['version'])
    parser.add_argument('--executor-fence', action='store_true')
    parser.add_argument('--attempt-preflight', action='store_true')
    parser.add_argument('--acquire-bound-artifacts', action='store_true')
    parser.add_argument('--record-event', action='store_true')
    for field in ('feed', 'package', 'stage', 'result'): parser.add_argument('--event-' + field)
    args = parser.parse_args()
    root = args.repo_root.resolve()
    identity = executor_identity(root)
    if args.plan != SUCCESSOR['planPath'] or args.version != SUCCESSOR['version'] or (args.attempt_preflight and (args.acquire_bound_artifacts or args.record_event)):
        preflight.fail('successor mixed source/plan/effect route refused')
    binding = validate_successor_attempt(root, args.source_sha, not args.attempt_preflight)
    result = validate(root, args.source_sha, args.plan, args.version, False)
    result['planSha256'] = binding['planSha256']
    if args.acquire_bound_artifacts: acquire_successor(root, binding)
    if args.record_event:
        ids = [p['id'] for p in json.loads(source_bytes(root, binding['producerSha'], binding['planPath']))['packages']]
        if args.event_feed not in ('github', 'nuget') or args.event_package not in ids + ['all19', 'unknown'] or args.event_stage not in ('probe', 'compare', 'push', 'readback', 'interrupted') or args.event_result not in ('requested', 'acknowledged', 'matched', 'failed', 'absent', 'refused'):
            preflight.fail('successor publisher observation fields refused')
        event = {'schema': 'fsgg.rendering.publisher-observation/v1', **identity, 'producerSha': binding['producerSha'], 'planSha256': binding['planSha256'], 'custodySha256': binding['custodySha256'], 'effectId': binding['effectId'], 'utc': datetime.now(timezone.utc).isoformat(), 'feed': args.event_feed, 'packageId': args.event_package, 'stage': args.event_stage, 'outcome': args.event_result}
        path = root / 'artifacts/attempt/publisher-events.jsonl'; path.parent.mkdir(parents=True, exist_ok=True)
        with path.open('a') as stream: stream.write(json.dumps(event, sort_keys=True) + '\n')
        return 0
    print(json.dumps(result, sort_keys=True))
    return 0


# One selected-successor adapter. Historical subject/settlement functions remain untouched.
def successor_typed(root: Path, intent: str, evidence=None) -> dict:
    payload={'intent':intent}
    if evidence is not None:payload['evidence']=evidence
    result=subprocess.run(['dotnet','fsi','--exec',str(root/'scripts/release-selected-successor.fsx')],
                          input=json.dumps(payload),text=True,capture_output=True,timeout=90,
                          env={**os.environ,'DOTNET_NOLOGO':'true','DOTNET_SKIP_FIRST_TIME_EXPERIENCE':'1'})
    if result.returncode:preflight.fail('typed retained successor refused: '+result.stderr.strip())
    return json.loads(result.stdout,object_pairs_hook=preflight.unique_json_object)


def native_packet(root: Path, run_id, attempt, artifact, source, sha, size, workflow) -> bytes:
    # Shared native read transport. Interpretation/admission stays in SelectedSuccessor.
    def api(path):return subprocess.check_output(['gh','api','repos/FS-GG/FS.GG.Rendering/'+path],stderr=subprocess.DEVNULL,timeout=60)
    run=json.loads(api(f'actions/runs/{run_id}'));meta=json.loads(api(f'actions/artifacts/{artifact}'))
    if (run.get('id')!=run_id or run.get('run_attempt')!=attempt or run.get('head_sha')!=source or run.get('conclusion')!='success' or run.get('path')!=workflow
        or meta.get('id')!=artifact or meta.get('expired') is not False or meta.get('workflow_run',{}).get('id')!=run_id
        or meta['workflow_run'].get('head_sha')!=source or meta.get('digest')!='sha256:'+sha or meta.get('size_in_bytes')!=size):preflight.fail('retained native packet identity refused')
    raw=api(f'actions/artifacts/{artifact}/zip')
    if len(raw)!=size or hashlib.sha256(raw).hexdigest()!=sha:preflight.fail('retained native packet bytes differ')
    return raw


def successor_zip(raw):
    import io,stat
    z=zipfile.ZipFile(io.BytesIO(raw));rows=z.infolist()
    if len(rows)>4096 or len({i.filename for i in rows})!=len(rows) or sum(i.file_size for i in rows)>134217728:preflight.fail('retained packet bounds/duplicates refused')
    for i in rows:
        if Path(i.filename).is_absolute() or '..' in Path(i.filename).parts or stat.S_IFMT(i.external_attr>>16) not in (0,stat.S_IFREG,stat.S_IFDIR):preflight.fail('retained packet path/type refused')
    return z


def retain_exact(path: Path, raw: bytes) -> None:
    # Acquisition may resume; it may never silently replace retained custody.
    if path.is_symlink() or (path.exists() and (not path.is_file() or path.read_bytes()!=raw)):
        preflight.fail('retained operand path already contains different bytes: '+path.name)
    if not path.exists():
        with path.open('xb') as stream:stream.write(raw)


def retained_evidence(root: Path, binding: dict, acquire=True) -> dict:
    out=root/'artifacts/attempt';out.mkdir(parents=True,exist_ok=True)
    if acquire:
        for filename,item,workflow in [('original.zip',binding,'.github/workflows/release.yml'),('qualification.zip',binding['qualification'],'.github/workflows/templates-external-reference-qualification.yml'),('browser.zip',binding['browser'],'.github/workflows/release.yml')]:
            producer=filename=='original.zip'
            raw=native_packet(root,item['producerRun'] if producer else item['run'],item['producerAttempt'] if producer else item['attempt'],item['producerArtifact'] if producer else item['artifact'],item['producerSha'] if producer else item.get('callerSha',item.get('sourceSha')),item['outerSha256'],item['outerSizeBytes'],workflow)
            retain_exact(out/filename,raw)
    # Rehash every retained operand on cache reuse; a cached verdict never admits changed bytes.
    for filename,item in [('original.zip',binding),('qualification.zip',binding['qualification']),('browser.zip',binding['browser'])]:
        raw=(out/filename).read_bytes()
        if len(raw)!=item['outerSizeBytes'] or hashlib.sha256(raw).hexdigest()!=item['outerSha256']:preflight.fail('retained cached packet differs')
    archives=root/'artifacts/packages';archives.mkdir(parents=True,exist_ok=True)
    with successor_zip((out/'original.zip').read_bytes()) as z:
        names=z.namelist()
        if len(names)!=20 or any(Path(n).name!=n for n in names):preflight.fail('retained original19 closure refused')
        for name in names:retain_exact(archives/name,z.read(name))
    plan=source_bytes(root,binding['producerSha'],binding['planPath'])
    if hashlib.sha256(plan).hexdigest()!=binding['planSha256']:preflight.fail('immutable retained plan differs')
    retain_exact(out/'producer-plan.json',plan)
    subprocess.run(['python3',str(root/'scripts/release-custody.py'),'verify','--plan',str(out/'producer-plan.json'),'--archives',str(archives),'--manifest',str(archives/'release-custody.json'),'--source-sha',binding['producerSha']],check=True,stdout=subprocess.DEVNULL)
    def member(z,suffix):
        names=[n for n in z.namelist() if n==suffix or n.endswith('/'+suffix)]
        if len(names)!=1:preflight.fail('retained evidence missing/ambiguous: '+suffix)
        return z.read(names[0])
    with successor_zip((out/'qualification.zip').read_bytes()) as z, successor_zip((out/'browser.zip').read_bytes()) as browser:
        q=lambda name:member(z,'palette-candidate/'+name)
        template_names=[n for n in z.namelist() if n.startswith('palette-template-feed/') and n.endswith('.nupkg')]
        if len(template_names)!=1:preflight.fail('retained Templates archive missing/ambiguous')
        evidence={'Qualification':q('qualification.json').decode(),'Observer':q('orca-input.json').decode(),'Before':q('consumer-before.json').decode(),'After':q('consumer-after.json').decode(),
                  'Assets':q('receiver/SvgFoundation/Studio/obj/project.assets.json').decode(),'Custody':(archives/'release-custody.json').read_text(),
                  'Preflight':member(z,'external-reference-preflight.json').decode(),'Browser':member(browser,'source-browser-observations.json').decode(),
                  'BrowserOriginal':member(browser,'source-browser-observations.original-candidate.json').decode(),'ProducerJobs':member(z,'rendering-jobs.json').decode(),
                  'LockHash':hashlib.sha256(q('receiver/SvgFoundation/Studio/packages.lock.json')).hexdigest(),'TemplateHash':hashlib.sha256(z.read(template_names[0])).hexdigest()}
    verdict=successor_typed(root,'qualified-publication',evidence)
    if verdict!={'result':'pass','scope':'qualified-retained-inputs','publicationAdmitted':False}:preflight.fail('typed qualification verdict refused')
    (out/'successor-evidence.json').write_text(json.dumps(evidence,sort_keys=True)+'\n')
    return evidence


def validate_retained_successor(root: Path, source_sha: str, acquire=True) -> dict:
    binding=successor_typed(root,'coordinates')
    if source_sha!=binding['producerSha']:preflight.fail('selected retained source differs')
    retained_evidence(root,binding,acquire)
    return binding


def retained_producer_tags(root: Path, source: str) -> None:
    spec=importlib.util.spec_from_file_location('retained_tag_probe',root/'scripts/selected-producer-tags.py')
    tags=importlib.util.module_from_spec(spec);spec.loader.exec_module(tags)
    refs=tuple('refs/tags/'+tag for tag in ('fs-gg-ui/v0.32.4','fs-gg-ui-template/v0.32.4','v0.32.4'))
    try:state=tags.probe(lambda *arguments: preflight.git(root,*arguments),refs,source)
    except tags.Refused as error:preflight.fail(str(error))
    if any(state[ref]!=source for ref in refs):preflight.fail('retained publication requires all three original producer lightweight tags')


def retained_main() -> int:
    parser=argparse.ArgumentParser(description='Typed qualified retained successor; no regeneration')
    parser.add_argument('--retained-successor',action='store_true',required=True)
    parser.add_argument('--source-sha',default=os.environ.get('PRODUCER_SHA'));parser.add_argument('--plan');parser.add_argument('--version')
    parser.add_argument('--attempt-preflight',action='store_true');parser.add_argument('--executor-fence',action='store_true')
    parser.add_argument('--acquire-bound-artifacts',action='store_true');parser.add_argument('--record-event',action='store_true')
    for field in ('feed','package','stage','result'):parser.add_argument('--event-'+field)
    args=parser.parse_args();root=Path.cwd();identity=executor_identity(root)
    if not args.source_sha:preflight.fail('retained successor source is required')
    if os.environ.get('GITHUB_RUN_ATTEMPT')!='1':preflight.fail('retained successor requires fresh first attempt')
    if not args.attempt_preflight and os.environ.get('FSGG_SUCCESSOR_UNRESOLVED_SEND')!='false':preflight.fail('retained successor requires an explicit reconciled prior-send state')
    if not args.attempt_preflight and not args.record_event:
        retained_producer_tags(root,args.source_sha)
    if args.record_event:
        binding=json.loads((root/'artifacts/attempt/input-binding.json').read_text())
        if any(binding.get(key)!=value for key,value in identity.items()) or binding.get('producerSha')!=args.source_sha:preflight.fail('cached selected attempt context differs')
        if hashlib.sha256((root/'artifacts/attempt/successor-evidence.json').read_bytes()).hexdigest()!=binding.get('qualifiedEvidenceSha256'):preflight.fail('cached selected evidence changed')
    else:binding=validate_retained_successor(root,args.source_sha)
    if (args.plan and args.plan!=binding['planPath']) or (args.version and args.version!=binding['version']):preflight.fail('mixed retained plan/version refused')
    if args.record_event:
        ids=[row['id'] for row in json.loads((root/'artifacts/attempt/producer-plan.json').read_text())['packages']]
        if args.event_feed not in ('github','nuget') or args.event_package not in ids+['all19','unknown'] or args.event_stage not in ('probe','compare','push','readback','interrupted') or args.event_result not in ('requested','acknowledged','matched','failed','absent','refused'):preflight.fail('retained publisher event refused')
        event={'schema':'fsgg.rendering.publisher-observation/v1',**identity,'producerSha':binding['producerSha'],'planSha256':binding['planSha256'],'custodySha256':binding['custodySha256'],'effectId':binding['effectId'],'utc':datetime.now(timezone.utc).isoformat(),'feed':args.event_feed,'packageId':args.event_package,'stage':args.event_stage,'outcome':args.event_result}
        with (root/'artifacts/attempt/publisher-events.jsonl').open('a') as f:f.write(json.dumps(event,sort_keys=True)+'\n')
    else:
        facts={**identity,'originalCustodyVerified':True,'nativeTemplatesQualified':True,'githubEffectiveWrite':'unknown-before-attempt'}
        (root/'artifacts/attempt/qualification-facts.json').write_text(json.dumps(facts,sort_keys=True)+'\n')
        (root/'artifacts/attempt/input-binding.json').write_text(json.dumps({**binding,**identity,'originalPlanSha256':binding['planSha256'],'qualifiedEvidenceSha256':hashlib.sha256((root/'artifacts/attempt/successor-evidence.json').read_bytes()).hexdigest()},sort_keys=True)+'\n')
        print(json.dumps({'result':'pass','scope':'qualified retained inputs; no write authority',**binding},sort_keys=True))
    return 0


if __name__ == '__main__':
    if os.environ.get('FSGG_RELEASE_ATTEMPT')=='palette' and '--executor-only' not in sys.argv[1:] and '--retained-successor' not in sys.argv[1:]:sys.argv.append('--retained-successor')
    if '--retained-successor' in sys.argv[1:]:raise SystemExit(retained_main())
    if os.environ.get('FSGG_RELEASE_ATTEMPT') == '0323' and '--executor-only' not in sys.argv[1:] and '--font-disposal-attempt' not in sys.argv[1:]:
        sys.argv.append('--font-disposal-attempt')
    if os.environ.get('FSGG_RELEASE_ATTEMPT') == '0322' and '--executor-only' not in sys.argv[1:] and '--successor-attempt' not in sys.argv[1:]:
        sys.argv.append('--successor-attempt')
    raise SystemExit(font_disposal_main() if '--font-disposal-attempt' in sys.argv[1:] else successor_main() if '--successor-attempt' in sys.argv[1:] else main())
