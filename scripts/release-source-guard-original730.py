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
    # Closed source selections only. The original publisher/attempt remains bound to
    # its historical plan and producer; adding source preparation grants no effects.
    selections = {
        'eng/release/svg-external-authority-0.32.0.json': ('0.32.0', '0.31.0'),
        'eng/release/svg-export-prefix-0.32.1.json': ('0.32.1', '0.32.0'),
    }
    if plan_path not in selections or (version, plan.get('baselineVersion')) != selections[plan_path]:
        preflight.fail('release plan path/version/baseline is not a selected source tuple')
    if version == '0.32.1' and (require_publication_ready or plan['publicationReady'] is not False):
        preflight.fail('0.32.1 source preparation only: candidate custody and installed qualification are unbound')
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


ATTEMPT = 'eng/release/svg-external-authority-0.32.0-attempt.json'
PRODUCER = '730923fe9d27174e879566f21dab14a1b03d761a'
PLAN_HASH = '6accaeb58e4cf8f4f5fea49dffbdcdbd8fa4bae4cd6387949cd7fe0df4f31b69'
OUTER = '58a80ec49db26b36b3f873694053df1e0eb4c3fb5bac503c8905ac2547bdb4d7'
CUSTODY = '35d4c23fbd811c810987277aa1e28d2b35e605f0c9106e5385f1cb7080dc1ad9'

def source_bytes(root: Path, sha: str, path: str) -> bytes:
    # Existing text helper strips whitespace; custody must bind exact bytes, including final newline.
    return subprocess.check_output(['git','-C',str(root),'show',sha+':'+path],stderr=subprocess.DEVNULL)


def validate_attempt(root: Path, source_sha: str, require_ready: bool = True) -> dict:
    binding = json.loads((root/ATTEMPT).read_text(), object_pairs_hook=preflight.unique_json_object)
    expected = {'schema':'fsgg.rendering.publication-attempt/v1','producerSha':PRODUCER,
        'planPath':'eng/release/svg-external-authority-0.32.0.json','planSha256':PLAN_HASH,
        'producerRun':37046893526,'producerArtifact':11244853996,'outerSha256':OUTER,
        'outerSizeBytes':14790371,'custodySha256':CUSTODY,'version':'0.32.0','baselineVersion':'0.31.0',
        'githubEffectiveWrite':'unknown-before-attempt'}
    if source_sha != PRODUCER or any(binding.get(k)!=v for k,v in expected.items()):
        preflight.fail('selected attempt producer/artifact/plan identity differs')
    if hashlib.sha256(source_bytes(root,source_sha,binding['planPath'])).hexdigest()!=PLAN_HASH:
        preflight.fail('selected original plan bytes differ; never amend producer custody')
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
    result=json.loads(read('qualification.json'))
    if result.get('disposition')!='passed' or result.get('templates',{}).get('revision')!=q['templatesSha'] or result.get('producer',{}).get('revision')!=PRODUCER or result.get('producer',{}).get('custodyManifestSha256')!=CUSTODY:preflight.fail('native qualification producer/template join refused')
    if result.get('browserFamilies')!=['chromium','firefox','webkit'] or result.get('passedPerFamily')!=4 or result.get('skipped')!=0:preflight.fail('native three-family qualification refused')
    caller=preflight.source_text(root,q['callerSha'],'.github/workflows/templates-external-reference-qualification.yml')
    if re.findall(r'^    uses: FS-GG/FS.GG.Templates/\.github/workflows/fable-external-reference-source\.yml@([0-9a-f]{40})$',caller,re.M)!=[q['templatesSha']] or re.findall(r'^      templates-source: ([0-9a-f]{40})$',caller,re.M)!=[q['templatesSha']]:preflight.fail('native caller executable/checkout join refused')
    native=result.get('nativePreflight',{})
    context=native.get('caller',{})
    if native.get('requestedPhase')!='full' or context.get('repository')!='FS-GG/FS.GG.Rendering' or context.get('revision')!=q['callerSha'] or context.get('run')!=str(q['run']):preflight.fail('native full-phase caller receipt differs')
    selected=['FS.GG.UI.Scene.0.32.0.nupkg','FS.GG.UI.KeyboardInput.0.32.0.nupkg','FS.GG.UI.Scene.SvgBrowser.0.32.0.nupkg']
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
    # Native PR GITHUB_SHA describes its synthetic merge context; checkout HEAD is the subject.
    executor = preflight.git(root, 'rev-parse', 'HEAD')
    context = os.environ['GITHUB_SHA']
    if not preflight.SHA.fullmatch(executor) or not preflight.SHA.fullmatch(context):
        preflight.fail('selected reader executor/context identity refused')
    return {'executorSha': executor, 'workflowContextSha': context}


def acquire_selected(root: Path, binding: dict) -> None:
    # GET only; GH_TOKEN is inherited through the environment, never an argument or receipt.
    if os.environ.get('GITHUB_REPOSITORY')!='FS-GG/FS.GG.Rendering': preflight.fail('selected reader context refused')
    out=root/'artifacts/attempt'; out.mkdir(parents=True,exist_ok=True)
    def api(path):
        return subprocess.check_output(['gh','api','repos/FS-GG/FS.GG.Rendering/'+path],stderr=subprocess.DEVNULL,timeout=60)
    def packet(run_id,artifact_id,sha,outer_hash,size,path):
        run=json.loads(api(f'actions/runs/{run_id}')); meta=json.loads(api(f'actions/artifacts/{artifact_id}'))
        if run.get('id')!=run_id or run.get('head_sha')!=sha or run.get('conclusion')!='success' or run.get('path')!=path:
            preflight.fail('selected native run identity/result refused')
        joined=meta.get('workflow_run',{})
        if meta.get('id')!=artifact_id or meta.get('expired') is not False or joined.get('id')!=run_id or joined.get('head_sha')!=sha or meta.get('digest')!='sha256:'+outer_hash or meta.get('size_in_bytes')!=size:
            preflight.fail('selected native artifact metadata refused')
        raw=api(f'actions/artifacts/{artifact_id}/zip')
        if len(raw)!=size or hashlib.sha256(raw).hexdigest()!=outer_hash: preflight.fail('selected artifact bytes differ')
        return raw
    raw=packet(binding['producerRun'],binding['producerArtifact'],PRODUCER,OUTER,binding['outerSizeBytes'],'.github/workflows/release.yml')
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
    raw=packet(q['run'],q['artifact'],q['callerSha'],q['outerSha256'],q['outerSizeBytes'],'.github/workflows/templates-external-reference-qualification.yml')
    proof=out/'qualification.zip';proof.write_bytes(raw)
    with zipfile.ZipFile(proof) as z:
        def read(name):
            matches=[n for n in z.namelist() if Path(n).name==name]
            if len(matches)!=1:preflight.fail('native qualification evidence missing/duplicated')
            return z.read(matches[0])
        counts,providers=validate_qualification_payload(root,binding,read,archives)
    facts={'originalCustodyVerified':True,'nativeTemplatesQualified':True,'providerPassed':providers,'browserPassed':counts,'browserUnexpected':0,'browserSkipped':0,'browserFlaky':0,'githubEffectiveWrite':'unknown-before-attempt'}
    (out/'qualification-facts.json').write_text(json.dumps(facts,sort_keys=True)+'\n')
    (out/'input-binding.json').write_text(json.dumps({'producerSha':PRODUCER,**executor_identity(root),'run':os.environ['GITHUB_RUN_ID'],'attempt':os.environ['GITHUB_RUN_ATTEMPT'],'originalPlanSha256':PLAN_HASH,'outerSha256':OUTER,'custodySha256':CUSTODY,'qualification':q,'publication':'not-yet-observed'},sort_keys=True)+'\n')

def record_event(root: Path, feed: str, package: str, stage: str, outcome: str) -> None:
    ids=[p['id'] for p in json.loads(preflight.source_text(root,PRODUCER,'eng/release/svg-external-authority-0.32.0.json'))['packages']]
    if feed not in ('github','nuget') or package not in ids+['all19','unknown'] or stage not in ('probe','compare','push','readback','interrupted') or outcome not in ('requested','acknowledged','matched','failed','absent','refused'):
        preflight.fail('publisher diagnostic fields refused')
    path=root/'artifacts/attempt/publisher-events.jsonl';path.parent.mkdir(parents=True,exist_ok=True)
    event={'schema':'fsgg.rendering.publisher-observation/v1','producerSha':PRODUCER,'executorSha':os.environ.get('GITHUB_SHA','unknown'),'run':os.environ.get('GITHUB_RUN_ID','unknown'),'attempt':os.environ.get('GITHUB_RUN_ATTEMPT','unknown'),'planSha256':PLAN_HASH,'custodySha256':CUSTODY,'utc':datetime.now(timezone.utc).isoformat(),'feed':feed,'packageId':package,'stage':stage,'outcome':outcome}
    with path.open('a',encoding='utf-8') as stream: stream.write(json.dumps(event,sort_keys=True)+'\n')


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo-root', type=Path, default=Path.cwd())
    parser.add_argument('--source-sha')
    parser.add_argument('--plan', default='eng/release/svg-external-authority-0.32.0.json')
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

if __name__ == '__main__':
    raise SystemExit(main())
