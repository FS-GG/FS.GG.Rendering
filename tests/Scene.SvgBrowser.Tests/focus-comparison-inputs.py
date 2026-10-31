"""Bind the existing two packed fixtures; retain bounded hosted diagnostics only."""
import hashlib,json,os,pathlib,re,subprocess,sys,zipfile
BASE='f3eb5abb117042144a1cbb41a5ec43821642df74'
SOURCE='src/Scene.SvgBrowser/SvgBrowser.fs'
EXPECTED={'original':'e32cfb1f8c9683a4fddd158bba604ad825ef4c54a3b0f61d10b9902bde100cb0','candidate':'dd902ba6f12b1501362b442c4765bdd33e169c3577e2e8a989d6c83b940ae8df'}
def require(value,cause):
 if not value:raise RuntimeError(cause)
def digest(body):return hashlib.sha256(body).hexdigest()
def pin(path):
 body=path.read_bytes();return {'path':str(path),'bytes':len(body),'sha256':digest(body)}
ARTIFACT_CAP=4194304
TERMINAL_RESERVE=65536
def bounded_write(path,body):
 require(not path.exists(),'evidence output already exists')
 population=list(path.parent.iterdir());require(len(population)<128 and all(x.is_file() and not x.is_symlink() for x in population),'evidence population invalid')
 require(sum(x.stat().st_size for x in population)+len(body)<=ARTIFACT_CAP-TERMINAL_RESERVE,'retained evidence budget exhausted; original omitted, no proof promoted')
 with path.open('xb') as stream:stream.write(body)
def emit(path,value):bounded_write(path,(json.dumps(value,indent=2)+'\n').encode())
def packed_production_source(package):
 with zipfile.ZipFile(package) as archive:
  items=archive.infolist();require(len(items)<=512 and sum(x.file_size for x in items)<=16777216,'generated package inspection budget exceeded')
  selected=[x for x in items if x.filename=='fable/SvgBrowser.fs'];require(len(selected)==1,'actual packed production source absent/ambiguous')
  require(selected[0].file_size<=1048576,'packed source oversized');return archive.read(selected[0])
def check(candidate,original,out):
 requested=os.environ.get('FOCUS_CANDIDATE_SHA','');require(re.fullmatch('[0-9a-f]{40}',requested) is not None,'exact candidate checkout SHA absent')
 hosted=json.loads((out/'hosted-attempt.json').read_text());require(hosted['runId']==os.environ.get('GITHUB_RUN_ID') and hosted['runAttempt']==os.environ.get('GITHUB_RUN_ATTEMPT') and hosted['candidateHead']==requested,'original hosted attempt binding differs')
 selected=[]
 for name,repo,head in [('original',original,BASE),('candidate',candidate,requested)]:
  actual=subprocess.check_output(['git','-C',str(repo),'rev-parse','HEAD'],text=True).strip();require(actual==head,name+' checkout differs')
  require(pin(repo/SOURCE)['sha256']==EXPECTED[name],name+' actual production source differs')
  selected.append({'name':name,'repo':str(repo),'head':head,'productionSource':pin(repo/SOURCE),'expectedRetainedFocus':name=='candidate'})
 control=pin(candidate/'tests/Scene.SvgBrowser.Tests/focus-reconciliation-control.mjs')
 mutation_control=pin(candidate/'tests/Scene.SvgBrowser.Tests/mutation-reconciliation-control.mjs')
 emit(out/'checkouts.json',{'variants':selected,'control':control,'mutationControl':mutation_control,'runId':os.environ.get('GITHUB_RUN_ID'),'runAttempt':os.environ.get('GITHUB_RUN_ATTEMPT'),'candidateHead':requested,'baselineHead':BASE})
def attest(candidate,original,work,out):
 seed=json.loads((out/'checkouts.json').read_text());variants=[]
 for selected in seed['variants']:
  name=selected['name'];parent=work/name; sdk=json.loads((parent/'global.json').read_text());require(sdk=={'sdk':{'version':'10.0.401','rollForward':'disable','allowPrerelease':False}},name+' private SDK selection differs');require((parent/'sdk-version.log').read_text().strip()=='10.0.401',name+' actual SDK differs');bounded_write(out/(name+'-global.json'),(parent/'global.json').read_bytes());bounded_write(out/(name+'-sdk-version.log'),(parent/'sdk-version.log').read_bytes());repo=pathlib.Path(selected['repo']);fixture=work/name/'fixture';before=fixture/'source-manifest.tsv';after=fixture/'source-manifest-after.tsv'
  require(before.read_bytes()==after.read_bytes(),name+' source manifest changed during build');require(pin(repo/SOURCE)==selected['productionSource'],name+' production source changed after build')
  source_row=selected['productionSource']['sha256']+'\t'+SOURCE
  require(source_row in before.read_text().splitlines(),name+' production source absent from fixture manifest')
  if name=='candidate':require((fixture/'logs/sdk-version.log').read_text().strip()=='10.0.401' and json.loads((fixture/'global.json').read_text())==sdk,'candidate helper SDK context differs')
  packages=list((fixture/'feed').glob('FS.GG.UI.Scene.SvgBrowser.*.nupkg'));require(len(packages)==1,name+' actual production package ambiguous')
  package=packages[0]
  packed=packed_production_source(package)
  require(packed==repo.joinpath(SOURCE).read_bytes(),name+' packed production source differs from checkout')
  dist=fixture/'Browser/dist';manifest_path=dist/'artifact-manifest.json';manifest=json.loads(manifest_path.read_text());require(manifest['sourceSha256']=='sha256:'+digest(before.read_bytes()),name+' dist source-manifest join differs')
  declared=set()
  for item in manifest['files']:
   relative=pathlib.PurePosixPath(item['path']);require(not relative.is_absolute() and '..' not in relative.parts and item['path'] not in declared,'dist path/duplicate invalid');declared.add(item['path']);path=dist/item['path']
   require(path.is_file() and not path.is_symlink(),'dist member missing/linked');actual=pin(path);require(actual['bytes']==item['bytes'] and 'sha256:'+actual['sha256']==item['sha256'],'dist bytes differ')
  actual_population={str(x.relative_to(dist)) for x in dist.rglob('*') if x.is_file() and x.name!='artifact-manifest.json'};require(actual_population==declared and not any(x.is_symlink() for x in dist.rglob('*')),'dist population differs')
  for item in manifest['packages']:
   path=fixture/'feed'/item['path'];require(path.parent==fixture/'feed' and path.is_file() and not path.is_symlink(),'package manifest path invalid');require('sha256:'+pin(path)['sha256']==item['sha256'],'package manifest bytes differ')
  require(package.stat().st_size<=1048576,'selected proof package exceeds retained artifact budget')
  retained_package=out/(name+'-svg-browser.nupkg');bounded_write(retained_package,package.read_bytes())
  bounded_write(out/(name+'-source-manifest.tsv'),before.read_bytes());bounded_write(out/(name+'-source-manifest-after.tsv'),after.read_bytes())
  variants.append({**selected,'retainedPackage':pin(retained_package),'sourceManifestAfter':pin(after),'dist':str(dist),'distManifest':pin(manifest_path),'sourceManifest':pin(before),'package':pin(package),'actualSdkVersion':(parent/'sdk-version.log').read_text().strip(),'sdkSelection':pin(parent/'global.json'),'packedSourceSha256':digest(packed),'artifactId':manifest['artifactId']})
 require(pin(pathlib.Path(seed['control']['path']))==seed['control'],'exact comparison control changed')
 require(pin(pathlib.Path(seed['mutationControl']['path']))==seed['mutationControl'],'exact mutation control changed')
 config={**seed,'variants':variants,'workSeconds':60,'cleanupSeconds':10,'diagnosticRetentionBytes':4194304,'nativeJourneyAcceptance':False}
 emit(out/'comparison-inputs.json',config)
 for name in ['original','candidate']:
  src=work/name/'fixture/Browser/dist/artifact-manifest.json';bounded_write(out/(name+'-fixture-manifest.json'),src.read_bytes())
 emit(out/'runtime-inputs.json',{'node':subprocess.check_output(['node','--version'],text=True).strip(),'dotnet':subprocess.check_output(['dotnet','--info'],text=True,cwd=work/'candidate'),'runner':{k:os.environ.get(k) for k in ['RUNNER_OS','RUNNER_ARCH','RUNNER_NAME','ImageOS','ImageVersion','GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','GITHUB_JOB']},'resourceAccounting':'ordinary GitHub-hosted job contract; no private sampled-RSS/whole-tree/mmap claim'})
def retain(work,out,status):
 # Only these bounded evidence files are uploaded. Raw build logs remain ephemeral.
 logs=sorted([*work.glob('*/fixture/logs/*.log'),*work.glob('*/sdk-version.log'),*work.glob('*.log')]);records=[];existing=sum(x.stat().st_size for x in out.iterdir() if x.is_file());remaining=max(0,min(2097152,4194304-existing-65536))
 require(len(logs)<=64,'retained log-count bound exceeded')
 for name in ['original','candidate']:
  statuses=work/name/'fixture/logs/statuses.tsv'
  if statuses.is_file():
   body=statuses.read_bytes();require(len(body)<=8192,'fixture stage statuses oversized');bounded_write(out/(name+'-stage-statuses.tsv'),body)
 for index,path in enumerate(logs):
  size=path.stat().st_size;take=min(size,remaining,131072);body=b''
  if take:
   with path.open('rb') as stream:stream.seek(max(0,size-take));body=stream.read(take)
   bounded_write(out/('diagnostic-'+str(index)+'.log'),body);remaining-=len(body)
  records.append({'source':str(path.relative_to(work)),'rawBytes':size,'retainedTailBytes':len(body),'omittedPrefixBytes':size-len(body),'retainedSha256':digest(body) if body else None,'rawFullDigest':'unmeasured'})
 emit(out/'diagnostic-retention.json',{'shellExit':status,'logs':records,'retainedLogBudget':2097152,'allArtifactBudget':4194304,'wholeLogAccounting':False,'omissionsExplicit':True})
 require(sum(x.stat().st_size for x in out.iterdir() if x.is_file())<=4194304,'total retained diagnostic/evidence budget exceeded')
def main():
 mode=sys.argv[1]
 if mode=='retain':retain(pathlib.Path(sys.argv[2]),pathlib.Path(sys.argv[3]),int(sys.argv[4]))
 else:
  candidate=pathlib.Path(sys.argv[2]).resolve();original=pathlib.Path(sys.argv[3]).resolve();out=pathlib.Path(sys.argv[4]).resolve()
  if mode=='check':check(candidate,original,out)
  elif mode=='attest':attest(candidate,original,pathlib.Path(sys.argv[5]).resolve(),out)
  else:raise RuntimeError('fixed mode required')

def refusal_receipt(out,error):
 # This uses the reserved space only, and cannot promote omitted proof bytes.
 body=(json.dumps({'firstCause':type(error).__name__+': '+str(error)[:4096],'coverage':'incomplete; original evidence omitted/refused where budget exhausted','nativeJourneyAcceptance':False})+'\n').encode()
 existing=sum(x.stat().st_size for x in out.iterdir() if x.is_file())
 if len(body)>8192 or existing+len(body)>ARTIFACT_CAP-4096:return
 target=out/'preparation-refusal.json'
 if not target.exists():
  with target.open('xb') as stream:stream.write(body)
if __name__=='__main__':
 try:main()
 except Exception as error:
  out=pathlib.Path(sys.argv[3] if sys.argv[1]=='retain' else sys.argv[4])
  if out.is_dir():refusal_receipt(out,error)
  raise
