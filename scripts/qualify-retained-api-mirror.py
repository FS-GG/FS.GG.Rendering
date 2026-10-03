#!/usr/bin/env python3
"""Read-only, explicit original730 qualification for the tagged publication repair window."""
import argparse
import contextlib
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import urllib.error
import urllib.request
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('release_guard', ROOT/'scripts/release-source-guard.py')
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)
spec = importlib.util.spec_from_file_location('custody', ROOT/'scripts/release-custody.py')
custody = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = custody
spec.loader.exec_module(custody)
TAGS = ['fs-gg-ui/v0.32.0','fs-gg-ui-template/v0.32.0','v0.32.0']

def git(*args):
    return subprocess.check_output(['git','-C',str(ROOT),*args],stderr=subprocess.DEVNULL,text=True).strip()

def source_facts(candidate):
    if not re.fullmatch('[0-9a-f]{40}',candidate) or git('rev-parse','HEAD') != candidate:
        raise ValueError('retained mirror exact candidate refused')
    producer_tree=git('rev-parse',guard.PRODUCER+':src')
    if git('status','--porcelain','--untracked-files=all','--','src'):
        raise ValueError('retained mirror candidate source has uncommitted changes')
    if git('rev-parse',candidate+':src') != producer_tree:
        raise ValueError('retained mirror candidate source/API differs from original producer')
    if any(git('rev-parse',tag+'^{commit}') != guard.PRODUCER for tag in TAGS):
        raise ValueError('retained mirror exact producer tags refused')
    binding=guard.validate_attempt(ROOT,guard.PRODUCER)
    guard.validate(ROOT,guard.PRODUCER,binding['planPath'],'0.32.0')
    for path in ['template/base/Directory.Packages.props','Directory.Build.props','Directory.Packages.props','global.json']:
        if guard.source_bytes(ROOT,candidate,path) != guard.source_bytes(ROOT,guard.PRODUCER,path):
            raise ValueError('retained mirror candidate pins/build inputs differ from original producer')
    return binding,producer_tree

def verify_local(candidate):
    binding,source_tree=source_facts(candidate)
    out=ROOT/'artifacts/attempt';archives=ROOT/'artifacts/packages'
    if hashlib.sha256((out/'original.zip').read_bytes()).hexdigest()!=guard.OUTER:
        raise ValueError('retained mirror original outer digest refused')
    if hashlib.sha256((out/'producer-plan.json').read_bytes()).hexdigest()!=guard.PLAN_HASH or hashlib.sha256((archives/'release-custody.json').read_bytes()).hexdigest()!=guard.CUSTODY:
        raise ValueError('retained mirror plan/custody digest refused')
    _,manifest=custody.verified_manifest(argparse.Namespace(plan=out/'producer-plan.json',archives=archives,manifest=archives/'release-custody.json',source_sha=guard.PRODUCER))
    if hashlib.sha256((out/'qualification.zip').read_bytes()).hexdigest()!=binding['qualification']['outerSha256']:
        raise ValueError('retained mirror native qualification artifact digest refused')
    joined=json.loads((out/'input-binding.json').read_text())
    if joined['producerSha']!=guard.PRODUCER or joined['executorSha']!=candidate or joined['qualification']!=binding['qualification']:
        raise ValueError('retained mirror native acquisition/candidate join refused')
    return binding,source_tree,manifest

def decide(statuses):
    if len(statuses)!=19 or any(s not in (200,404) for s in statuses):
        raise ValueError('retained mirror public readback unavailable')
    return any(s==404 for s in statuses)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--select-original730',action='store_true',required=True)
    parser.add_argument('--candidate-sha',required=True)
    parser.add_argument('--receipt',type=Path,required=True)
    parser.add_argument('--verify',action='store_true')
    args=parser.parse_args()
    if not re.fullmatch('[0-9a-f]{40}',args.candidate_sha) or git('rev-parse','HEAD') != args.candidate_sha:
        raise ValueError('retained mirror exact candidate refused')
    if args.verify:
        binding,source_tree,_=verify_local(args.candidate_sha)
        receipt=json.loads(args.receipt.read_text())
        expected={'schema':'fsgg.rendering.retained-api-mirror/v1','candidateSha':args.candidate_sha,'candidateTree':git('rev-parse',args.candidate_sha+'^{tree}'),'producerSha':guard.PRODUCER,'sourceTree':source_tree,'producerTree':git('rev-parse',guard.PRODUCER+'^{tree}'),'custodySha256':guard.CUSTODY,'planSha256':guard.PLAN_HASH,'outerSha256':guard.OUTER,'mode':'retained-original','publicationAcceptance':False,'installedAcceptance':False,'mutation':'none'}
        if any(receipt.get(key)!=value for key,value in expected.items()) or not decide(receipt.get('publicStatuses',[])):
            raise ValueError('retained mirror receipt refused')
        return
    pin=(ROOT/'template/base/Directory.Packages.props').read_text()
    if '<FsGgUiVersion>0.32.0</FsGgUiVersion>' not in pin:
        print('retained-feed=');return
    present=[bool(subprocess.run(['git','rev-parse','--verify','refs/tags/'+tag],cwd=ROOT,capture_output=True).returncode==0) for tag in TAGS]
    if not any(present):print('retained-feed=');return
    if not all(present):raise ValueError('retained mirror partial tag triple refused')
    raw_plan=guard.source_bytes(ROOT,guard.PRODUCER,'eng/release/svg-external-authority-0.32.0.json')
    if hashlib.sha256(raw_plan).hexdigest()!=guard.PLAN_HASH:
        raise ValueError('retained mirror original plan digest refused')
    plan=json.loads(raw_plan)
    probe=ROOT/'artifacts/mirror/public-probes';probe.mkdir(parents=True,exist_ok=True)
    statuses=[]
    for row in plan['packages']:
        filename=row['id'].lower()+'.0.32.0.nupkg'
        target=probe/filename
        statuses.append(custody.download('https://api.nuget.org/v3-flatcontainer/'+row['id'].lower()+'/0.32.0/'+filename,target,''))
    pending=decide(statuses)
    if not pending:
        for target in probe.glob('*.nupkg'):target.unlink()
        print('retained-feed=');return
    binding,_=source_facts(args.candidate_sha)
    subprocess.run([sys.executable,str(ROOT/'scripts/release-source-guard.py'),'--attempt-binding','--acquire-bound-artifacts','--source-sha',guard.PRODUCER,'--plan',binding['planPath'],'--version','0.32.0'],cwd=ROOT,capture_output=True,check=True,timeout=120)
    archives=ROOT/'artifacts/packages'
    for row,status in zip(plan['packages'],statuses):
        if status==200:
            target=probe/(row['id'].lower()+'.0.32.0.nupkg')
            with contextlib.redirect_stdout(io.StringIO()):custody.compare_archive(archives/(row['id']+'.0.32.0.nupkg'),target)
            target.unlink()
    _,source_tree,_=verify_local(args.candidate_sha)
    args.receipt.parent.mkdir(parents=True,exist_ok=True)
    args.receipt.write_text(json.dumps({'schema':'fsgg.rendering.retained-api-mirror/v1','mode':'retained-original','candidateSha':args.candidate_sha,'candidateTree':git('rev-parse',args.candidate_sha+'^{tree}'),'workflowRef':os.environ.get('GITHUB_WORKFLOW_REF','local-readback'),'run':os.environ.get('GITHUB_RUN_ID','local-readback'),'producerSha':guard.PRODUCER,'sourceTree':source_tree,'producerTree':git('rev-parse',guard.PRODUCER+'^{tree}'),'custodySha256':guard.CUSTODY,'planSha256':guard.PLAN_HASH,'outerSha256':guard.OUTER,'publicStatuses':statuses,'publicationAcceptance':False,'installedAcceptance':False,'mutation':'none'},sort_keys=True)+'\n')
    print('retained-feed='+str(archives))

if __name__=='__main__':
    try:main()
    except (ValueError,OSError,KeyError,subprocess.SubprocessError,custody.CustodyError):
        sys.exit('retained API mirror qualification unavailable/refused')
