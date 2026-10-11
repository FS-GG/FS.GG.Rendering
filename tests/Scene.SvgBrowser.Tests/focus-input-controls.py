"""Inert source-coupled controls; actual retained package inputs must be pinned by owner."""
import importlib.util,json,os,pathlib,sys,tempfile,warnings,zipfile
here=pathlib.Path(__file__).parent
spec=importlib.util.spec_from_file_location('actual_inputs',here/'focus-comparison-inputs.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
def inert_audit(event,args):
 if event in ['subprocess.Popen','os.system','os.posix_spawn','socket.__new__','socket.connect','socket.bind','ctypes.dlopen']:raise RuntimeError('inert controls refuse process/network/native effects')
sys.addaudithook(inert_audit)
names=[]
def check(name,action):action();names.append(name)
def require(v):
 if not v:raise AssertionError('control predicate failed')
def refused(action):
 try:action()
 except RuntimeError:return
 raise AssertionError('expected refusal absent')
with tempfile.TemporaryDirectory() as raw:
 root=pathlib.Path(raw)
 for index,package in enumerate(sys.argv[1:]):
  selected=pathlib.Path(package);body=module.packed_production_source(selected)
  expected=module.EXPECTED['original' if index==0 else 'candidate']
  check('actual-retained-package-layout-'+str(index),lambda:require(module.digest(body)==expected))
 require(len(sys.argv)==3)
 for label,entries in [('missing',[('content/fable/SvgBrowser.fs',b'x')]),('duplicate',[('fable/SvgBrowser.fs',b'x'),('fable/SvgBrowser.fs',b'y')]),('oversized',[('fable/SvgBrowser.fs',b'x'*1048577)])]:
  archive=root/(label+'.zip')
  with warnings.catch_warnings():
   warnings.simplefilter('ignore')
   with zipfile.ZipFile(archive,'w') as output:
    for name,body in entries:output.writestr(name,body)
  check('package-'+label+'-refusal',lambda p=archive:refused(lambda:module.packed_production_source(p)))
 evidence=root/'bounded';evidence.mkdir();limit=module.ARTIFACT_CAP-module.TERMINAL_RESERVE
 check('exact-evidence-budget-admitted',lambda:module.bounded_write(evidence/'proof',b'x'*limit))
 check('over-budget-not-promoted',lambda:refused(lambda:module.bounded_write(evidence/'excess',b'x')))
 require(not (evidence/'excess').exists())
 check('reserved-refusal-record',lambda:module.refusal_receipt(evidence,RuntimeError('original budget refused')))
 require((evidence/'preparation-refusal.json').exists());require(sum(x.stat().st_size for x in evidence.iterdir())<=module.ARTIFACT_CAP-4096)
 check('duplicate-output-refusal',lambda:refused(lambda:module.bounded_write(evidence/'proof',b'x')))
 linked=root/'linked';linked.mkdir();(linked/'link').symlink_to(evidence/'proof')
 check('linked-evidence-refusal',lambda:refused(lambda:module.bounded_write(linked/'new',b'x')))
 # Execute the exact final promotion code extracted from the actual YAML, not a copied predicate.
 workflow=(here.parent.parent/'.github/workflows/svg-browser.yml').read_text();marker='      - name: Record original terminal step status';body=workflow.split(marker,1)[1].split("          python3 - <<'PY'\n",1)[1].split('          PY',1)[0];code='\n'.join(line[10:] for line in body.splitlines())
 for label,size,expectProof,priorStatus in [('good',limit,True,'failure'),('oversized',module.ARTIFACT_CAP,False,'failure'),('oversized-prior-success',module.ARTIFACT_CAP,False,'success')]:
  tmp=root/label;out=tmp/'svg-focus-comparison/evidence';out.mkdir(parents=True);(out/'proof').write_bytes(b'x'*size)
  os.environ.update(RUNNER_TEMP=str(tmp),FOCUS_JOB_STATUS=priorStatus,GITHUB_RUN_ID='original1',GITHUB_RUN_ATTEMPT='1')
  def promote():
   try:exec(compile(code,'actual_workflow_finalizer','exec'),{})
   except RuntimeError as error:
    require(not expectProof and str(error)=='upload population/budget refused; bounded receipt retained');return
   require(expectProof)
  check('actual-upload-promotion-'+label,promote)
  upload=tmp/'svg-focus-comparison/upload';require((upload/'proof').exists()==expectProof);require(sum(x.stat().st_size for x in upload.iterdir())<=module.ARTIFACT_CAP)
  receipt=json.loads((upload/'hosted-step-status.json').read_text());require(receipt['statusBeforeUpload']==priorStatus and receipt['nativeJourneyAcceptance'] is False)
print(json.dumps({'complete':True,'names':names,'count':len(names),'nativeJourneyAcceptance':False}))
