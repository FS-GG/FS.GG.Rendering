#!/usr/bin/env python3
"""Bounded causal controls for the actual closed settlement and HTTP adapter."""
import copy
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import urllib.error

SPEC = importlib.util.spec_from_file_location('settle', Path(__file__).with_name('settle-original-tags.py'))
s = importlib.util.module_from_spec(SPEC); SPEC.loader.exec_module(s)
ROOT = Path(__file__).resolve().parent.parent
PINS = json.loads((ROOT / s.BINDING).read_text())
POLICY = [{'id': 1, 'target': 'tag', 'enforcement': 'active', 'bypass_actors': [],
           'conditions': {'ref_name': {'include': list(s.REFS), 'exclude': []}},
           'rules': [{'type': 'update'}, {'type': 'deletion'}]}]
POLICY_HASH = s.digest(json.dumps(POLICY, sort_keys=True, separators=(',', ':')).encode())
ADMITTED = {'schema': 'fsgg.rendering.tag-settlement-admission/v1', 'effectId': s.EFFECT,
    'executorSha': 'a'*40, 'originalTransportOutcome': 'Unknown', 'allowedRefs': list(s.REFS),
    'writer': {'kind': 'installation-token', 'repository': s.REPO,
        'permissions': {'contents': 'write', 'workflows': 'write', 'metadata': 'read'}, 'capabilityEvidenceSha256': 'b'*64},
    'administrator': {'capabilityEvidenceSha256': 'c'*64}, 'policySha256': POLICY_HASH,
    'workflowInventorySha256': 'd'*64, 'originalActorId': 1645484,
    'activeRunDisposition': 'refuse', 'restorePreviousStates': True}


class FakeAPI:
    def __init__(self):
        self.calls = []; self.states = {i: 'active' for i in s.FENCES}; self.refs = {}
        self.policy = copy.deepcopy(POLICY); self.active = False; self.census_unknown = False
        self.create = 'accepted'; self.retire = True; self.restore = True; self.hook = lambda *a: None

    def get(self, path):
        return self.call('GET', s.PREFIX + path)[1]

    def call(self, method, path, body=None):
        self.calls.append((method, path, body)); self.hook(method, path, body)
        short = path.removeprefix(s.PREFIX)
        if method == 'GET':
            if short.startswith('rulesets?'):
                return 200, [{'id': 1, '_links': {'self': {'href': 'https://api.github.com' + s.PREFIX + 'rulesets/1'}}}]
            if short == 'rulesets/1': return 200, self.policy[0]
            if short.startswith('git/ref/'):
                ref = 'refs/' + short.removeprefix('git/ref/')
                if ref not in self.refs: return 404, {'message': 'Not Found'}
                value = self.refs[ref]
                return 200, {'ref': ref, 'object': {'type': 'tag' if value == 'annotated' else 'commit', 'sha': value}}
            if '/runs?' in short:
                return 200, {'total_count': 1 if self.active or self.census_unknown else 0,
                    'workflow_runs': [{'status': 'queued'}] if self.active else []}
            if short.startswith('actions/workflows/'):
                return 200, {'state': self.states[int(short.split('/')[2])]}
        if method == 'PUT':
            i = int(short.split('/')[2]); action = short.split('/')[3]
            if action == 'enable' and not self.restore: raise s.Refused('restore unavailable')
            self.states[i] = 'active' if action == 'enable' else 'disabled_manually'
            return 204, None
        if method == 'POST':
            if self.create in ('accepted', 'lost', 'conflict'): self.refs[body['ref']] = body['sha']
            if self.create in ('lost', 'never'): raise s.Refused('transport unknown')
            return (422 if self.create == 'conflict' else 201), {'ref': body['ref']}
        if method == 'DELETE':
            if not self.retire: raise s.Refused('retirement unavailable')
            return 204, None
        raise AssertionError((method, path))


class SettlementTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.api = FakeAPI(); self.root = Path(self.temp.name)
        self.op = s.Settlement(self.root, {'GITHUB_RUN_ID': '123'}, copy.deepcopy(ADMITTED), PINS,
            self.api, self.api, identity=lambda: None)
        self.op.inventory = lambda: None

    def writes(self):
        return [c for c in self.api.calls if c[0] == 'POST']

    def test_reachable_success_actual_operation_restores_and_preserves_unknown(self):
        result = self.op.run()
        self.assertEqual('Unknown', result['originalTransportOutcome'])
        self.assertEqual('Verified', result['sameIntentConvergence'])
        self.assertEqual('restored', result['cleanup'])
        self.assertEqual(list(s.REFS), [c[2]['ref'] for c in self.writes()])
        self.assertTrue(all(state == 'active' for state in self.api.states.values()))
        retired = next(i for i,c in enumerate(self.api.calls) if c[0] == 'DELETE')
        restored = next(i for i,c in enumerate(self.api.calls) if c[0] == 'PUT' and c[1].endswith('/enable'))
        self.assertLess(retired, restored)

    def test_late_original_before_and_after_new_create_same_value(self):
        for before in (True, False):
            self.api = FakeAPI(); self.op.admin = self.op.writer = self.api
            self.op.receipt['events'] = []
            if before: self.api.refs[s.REFS[0]] = s.SOURCE
            else:
                def late(method, path, body):
                    if method == 'POST': self.api.refs[s.REFS[0]] = s.SOURCE
                self.api.hook = late
            self.op.run()
            self.assertEqual(2 if before else 3, len(self.writes()))
            self.assertEqual({r:s.SOURCE for r in s.REFS}, self.api.refs)

    def test_lost_ack_and_duplicate_conflict_need_exact_readback_without_resend(self):
        for mode in ('lost', 'conflict'):
            self.api = FakeAPI(); self.api.create = mode; self.op.admin = self.op.writer = self.api
            result = self.op.run()
            self.assertEqual('Verified', result['sameIntentConvergence']); self.assertEqual(3,len(self.writes()))

    def test_never_accepted_stops_after_one_request(self):
        self.api.create = 'never'
        with self.assertRaises(s.Refused): self.op.run()
        self.assertEqual(1,len(self.writes())); self.assertEqual('Unknown',self.op.receipt['sameIntentConvergence'])

    def test_conflict_annotated_out_of_order_and_disappearing_prefix_refuse(self):
        for refs in ({s.REFS[0]:'b'*40},{s.REFS[0]:'annotated'},{s.REFS[2]:s.SOURCE}):
            self.api.refs = refs
            with self.assertRaises(s.Refused): self.op.refs()
        self.api.refs = {s.REFS[0]:s.SOURCE}; count=0
        def disappear(method,path,body):
            nonlocal count
            if method == 'GET' and path.endswith('git/ref/tags/fs-gg-ui/v0.32.3'):
                count += 1
                if count == 2:self.api.refs.clear()
        self.api.hook=disappear
        with self.assertRaises(s.Refused):self.op.run()
        self.assertEqual([],self.writes())

    def test_stale_policy_bypass_weak_immutability_and_wrong_tags_block_writes(self):
        for field,value in [('bypass_actors',[{'actor_id':1}]),('rules',[{'type':'update'}]),('enforcement','evaluate'),('conditions',{'ref_name':{'include':['refs/tags/other'],'exclude':[]}})]:
            self.api = FakeAPI(); self.op.admin = self.op.writer = self.api
            self.api.policy[0][field] = value
            # Even a freshly selected digest cannot turn insufficient policy into proof.
            self.op.admitted['policySha256'] = s.digest(json.dumps(self.api.policy,sort_keys=True,separators=(',',':')).encode())
            with self.assertRaises(s.Refused):self.op.run()
            self.assertEqual([],self.writes())
        self.op.admitted['policySha256']='0'*64
        with self.assertRaises(s.Refused):self.op.run()

    def test_disabled_workflow_with_active_old_job_and_unknown_census_block(self):
        for field in ('active','census_unknown'):
            self.api=FakeAPI();self.op.admin=self.op.writer=self.api;setattr(self.api,field,True)
            with self.assertRaises(s.Refused):self.op.run()
            self.assertEqual([],self.writes())

    def test_main_or_original_actor_unknown_blocks_all_tag_writes(self):
        self.op.identity_check=lambda: (_ for _ in ()).throw(s.Refused('main moved or actor unknown'))
        with self.assertRaises(s.Refused):self.op.run()
        self.assertEqual([],self.writes())

    def test_cleanup_failures_do_not_erase_convergence_or_first_failure(self):
        self.api.restore=False
        with self.assertRaises(s.Refused):self.op.run()
        self.assertEqual('Verified',self.op.receipt['sameIntentConvergence'])
        self.assertEqual('Unknown-owned-gap',self.op.receipt['cleanup'])
        self.api=FakeAPI();self.op.admin=self.op.writer=self.api;self.api.active=True;self.api.retire=False
        with self.assertRaises(s.Refused):self.op.run()
        self.assertTrue(any(e['stage']=='first-failure' for e in self.op.receipt['events']))
        self.assertFalse(any(c[0]=='PUT' and c[1].endswith('/enable') for c in self.api.calls))

    def test_bound_exhaustion_retains_secondary_cleanup_outcome(self):
        self.op.deadline=0
        with self.assertRaises(s.Refused):self.op.run()
        self.assertEqual([],self.writes())
        self.assertEqual('restored',self.op.receipt['cleanup'])

    def test_preserve_previously_disabled_states(self):
        self.api.states[310031035]='disabled_manually'
        self.op.run();self.assertEqual('disabled_manually',self.api.states[310031035])


class AdmissionAndAdapterTests(unittest.TestCase):
    def test_actual_executor_old_actor_job_and_main_fences(self):
        env={'FSGG_EXPECTED_EXECUTOR_SHA':'a'*40,'GITHUB_SHA':'a'*40,'GITHUB_WORKFLOW_SHA':'a'*40,
             'GITHUB_WORKFLOW_REF':s.WORKFLOW,'GITHUB_REF':'refs/heads/main','GITHUB_EVENT_NAME':'workflow_dispatch',
             'GITHUB_REPOSITORY':s.REPO,'GITHUB_RUN_ATTEMPT':'1','GITHUB_RUN_ID':'123'}
        for mutant in (None,'main','actor','old-active','old-attempt','old-job','checkout','binding','context'):
            with self.subTest(mutant=mutant),tempfile.TemporaryDirectory() as d:
                api=FakeAPI();op=s.Settlement(Path(d),dict(env),copy.deepcopy(ADMITTED),PINS,api,api)
                def get(path):
                    if path=='git/ref/heads/main':return {'object':{'type':'commit','sha':'b'*40 if mutant=='main' else 'a'*40,'url':'https://api.github.com/repos/'+s.REPO+'/git/commits/'+'a'*40}}
                    if path.startswith('actions/runs/'):
                        return {'status':'in_progress' if mutant=='old-active' else 'completed','run_attempt':2 if mutant=='old-attempt' else 1,'head_sha':PINS['executorSha'],'actor':{'id':0 if mutant=='actor' else PINS['actorId']},'event':'workflow_dispatch','path':'.github/workflows/release-tags.yml'}
                    if path.startswith('actions/jobs/'):return {'run_id':0 if mutant=='old-job' else PINS['run'],'status':'completed'}
                    raise AssertionError(path)
                api.get=get
                if mutant=='context':op.env['GITHUB_EVENT_NAME']='push'
                def git(*args):return ('b'*40 if mutant=='checkout' else 'a'*40) if args[0]=='rev-parse' else ''
                def binding(*args):
                    if mutant=='binding':raise SystemExit('changed original qualification')
                    return {}
                with patch.object(s.tags,'Git',return_value=git),patch.object(s.tags,'selected_binding',side_effect=binding):
                    if mutant:
                        with self.assertRaises((s.Refused,SystemExit)):op.identity()
                    else:
                        op.identity();self.assertEqual('a'*40,op.receipt['executorSha'])
                self.assertEqual([],api.calls)

    def test_actual_admission_digest_and_capability_mutants(self):
        raw=json.dumps(ADMITTED).encode(); env={'FSGG_EXPECTED_EXECUTOR_SHA':'a'*40}
        self.assertEqual(s.EFFECT,s.admission(raw,s.digest(raw),env)['effectId'])
        for mutate in [lambda d:d.update(effectId='new'),lambda d:d.update(originalTransportOutcome='Rejected'),lambda d:d.update(allowedRefs=[]),lambda d:d['writer']['permissions'].update(packages='write'),lambda d:d.update(activeRunDisposition='cancel-all'),lambda d:d.update(executorSha='b'*40)]:
            doc=copy.deepcopy(ADMITTED);mutate(doc);raw=json.dumps(doc).encode()
            with self.assertRaises(s.Refused):s.admission(raw,s.digest(raw),env)
        with self.assertRaises(s.Refused):s.admission(raw+b' ',s.digest(raw),env)

    def test_actual_adapter_rejects_patch_delete_update_and_arbitrary_create_before_transport(self):
        api=s.API('private-test-credential',lambda *a,**k:None,10**20)
        for method,path,body in [('PATCH',s.PREFIX+'git/refs/tags/v0.32.3',{}),('DELETE',s.PREFIX+'git/refs/tags/v0.32.3',None),('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':'b'*40}),('PUT',s.PREFIX+'actions/runs/123/cancel',None)]:
            with patch.object(api.opener,'open',side_effect=AssertionError('must not transport')),self.assertRaises(s.Refused):api.call(method,path,body)

    def test_actual_adapter_retains_raw_response_and_request_id_once_without_auth(self):
        journal=[];api=s.API('private-test-credential',lambda stage,**fields:journal.append((stage,fields)),10**20)
        class Response(io.BytesIO):
            code=201;headers={'x-github-request-id':'fixture-request'}
        with patch.object(api.opener,'open',return_value=Response(b'{"created":true}')) as opening:
            self.assertEqual((201,{'created':True}),api.call('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':s.SOURCE}))
        self.assertEqual(1,opening.call_count);self.assertEqual('fixture-request',journal[-1][1]['requestId'])
        self.assertNotIn('private-test-credential',json.dumps(journal))

    def test_actual_adapter_timeout_unknown_and_body_bound_never_retry(self):
        for failure in (TimeoutError('credential-private'),OSError('credential-private')):
            journal=[];api=s.API('credential-private',lambda stage,**fields:journal.append((stage,fields)),10**20)
            with patch.object(api.opener,'open',side_effect=failure) as opening,self.assertRaises(s.Refused):
                api.call('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':s.SOURCE})
            self.assertEqual(1,opening.call_count);self.assertEqual('http-outcome-unknown',journal[-1][0])
            self.assertNotIn('credential-private',json.dumps(journal))
        api=s.API('credential-private',lambda *a,**k:None,10**20)
        api.deadline=0
        with patch.object(api.opener,'open',side_effect=AssertionError('deadline forbids transport')),self.assertRaises(s.Refused):
            api.call('GET',s.PREFIX+'git/ref/tags/v0.32.3')

    def test_actual_original_binding_wrong_bytes_and_missing_receipts_refuse(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);path=root/s.BINDING;path.parent.mkdir(parents=True)
            path.write_bytes((ROOT/s.BINDING).read_bytes()+b' ')
            with self.assertRaisesRegex(s.Refused,'closed original'):s.original(root,root)
            path.write_bytes((ROOT/s.BINDING).read_bytes())
            with self.assertRaises(OSError):s.original(root,root)

    def test_actual_workflow_has_only_manual_entry_and_shared_non_canceling_lock(self):
        text=(ROOT/'.github/workflows/release-tag-settlement.yml').read_text()
        triggers=text.split('\non:\n')[1].split('\nconcurrency:')[0]
        self.assertIn('  workflow_dispatch:',triggers)
        for forbidden in ('  push:', '  release:', '  workflow_call:'):self.assertNotIn(forbidden,triggers)
        for required in ('group: release-tags','cancel-in-progress: false','timeout-minutes: 7','persist-credentials: false','if: always()','--original-directory','--admission-sha256'):
            self.assertIn(required,text)
        for forbidden in ('packages: write','id-token: write','secrets: inherit','git push','workflow_dispatches','repository_dispatch'):
            self.assertNotIn(forbidden,text)


class PublisherSettlementTests(unittest.TestCase):
    def test_real_publisher_acquisition_binds_native_artifact_claims_and_live_refs(self):
        import zipfile
        guard=s.tags.guard
        receipt={'schema':'fsgg.rendering.tag-settlement/v1','effectId':s.EFFECT,'producerSha':s.SOURCE,
            'originalTransportOutcome':'Unknown','sameIntentConvergence':'Verified','cleanup':'restored','result':'passed',
            'publication':'not-performed','notification':'not-performed','executorSha':'b'*40,'run':'11','attempt':'1',
            'workflowRef':s.WORKFLOW,'refs':{r:s.SOURCE for r in s.REFS}}
        for mutant in (None,'historical-outcome','convergence','cleanup','effect','annotated-current','wrong-current','wrong-native','changed-bytes'):
            with self.subTest(mutant=mutant),tempfile.TemporaryDirectory() as d:
                root=Path(d);doc=copy.deepcopy(receipt)
                if mutant=='historical-outcome':doc['originalTransportOutcome']='Rejected'
                if mutant=='convergence':doc['sameIntentConvergence']='Unknown'
                if mutant=='cleanup':doc['cleanup']='Unknown-owned-gap'
                if mutant=='effect':doc['effectId']='new-effect'
                member=json.dumps(doc).encode();stream=io.BytesIO()
                with zipfile.ZipFile(stream,'w') as z:z.writestr('settlement.json',member)
                raw=stream.getvalue()
                selected={'run':11,'attempt':1,'artifact':12,'artifactSha256':s.digest(raw),'receiptSha256':s.digest(member),'executorSha':'b'*40}
                calls=[]
                def api(argv,**kwargs):
                    path=argv[-1];calls.append(path)
                    if path.endswith('/runs/11'):
                        value={'id':11,'run_attempt':1,'status':'completed','conclusion':'success','event':'workflow_dispatch','head_sha':'c'*40 if mutant=='wrong-native' else 'b'*40,'path':'.github/workflows/release-tag-settlement.yml'}
                    elif path.endswith('/artifacts/12'):
                        value={'id':12,'expired':False,'digest':'sha256:'+s.digest(raw),'workflow_run':{'id':11,'head_sha':'b'*40},'size_in_bytes':len(raw)}
                    elif path.endswith('/zip'):return raw+(b' ' if mutant=='changed-bytes' else b'')
                    elif '/git/ref/' in path:
                        ref='refs/'+path.split('/git/ref/')[1]
                        value={'ref':ref,'object':{'type':'tag' if mutant=='annotated-current' else 'commit','sha':'c'*40 if mutant=='wrong-current' else s.SOURCE}}
                    else:raise AssertionError(path)
                    return json.dumps(value).encode()
                with patch.dict(s.os.environ,{'FSGG_ACCEPTED_TAG_SETTLEMENT':json.dumps(selected)}),patch.object(guard.subprocess,'check_output',side_effect=api):
                    if mutant:
                        with self.assertRaises(SystemExit):guard.accepted_tag_settlement(root)
                        self.assertFalse((root/'artifacts/attempt/accepted-tag-settlement.json').exists())
                    else:
                        result=guard.accepted_tag_settlement(root)
                        self.assertEqual(s.EFFECT,result['effectId'])
                        self.assertEqual(member,(root/'artifacts/attempt/accepted-tag-settlement.json').read_bytes())
                        self.assertEqual(6,len(calls))

    def test_missing_root_acceptance_blocks_real_acquisition_before_original_bytes_or_transport(self):
        guard=s.tags.guard
        with tempfile.TemporaryDirectory() as d,patch.dict(s.os.environ,{'FSGG_ACCEPTED_TAG_SETTLEMENT':''}),patch.object(guard,'executor_identity',return_value={}),patch.object(guard.subprocess,'check_output',side_effect=AssertionError('no original artifact or transport may start')):
            with self.assertRaises((SystemExit,ValueError)):guard.acquire_font_disposal(Path(d),guard.FONT_DISPOSAL)

    def test_actual_main_original_refusal_never_constructs_credential_adapter(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);path=root/s.BINDING;path.parent.mkdir(parents=True);path.write_bytes((ROOT/s.BINDING).read_bytes())
            admission_path=root/'admission.json';raw=json.dumps(ADMITTED).encode();admission_path.write_bytes(raw)
            argv=['settle','--admission',str(admission_path),'--admission-sha256',s.digest(raw),'--original-directory',str(root)]
            with patch.object(s.sys,'argv',argv),patch.dict(s.os.environ,{'FSGG_EXPECTED_EXECUTOR_SHA':'a'*40}),patch.object(s.Path,'cwd',return_value=root),patch.object(s,'API',side_effect=AssertionError('credentials must not be constructed')):
                self.assertEqual(1,s.main())

    def test_reviewed_source_census_has_no_unfenced_selected_tag_trigger(self):
        # Closed source equality detects new/changed triggers before any workflow-state effect.
        pins=json.loads((ROOT/'eng/release/font-disposal-0.32.3-workflow-fences.json').read_text())
        self.assertEqual(sorted(pins['executor']),sorted(str(p.relative_to(ROOT)) for p in (ROOT/'.github/workflows').glob('*.yml')))
        for path,sha in pins['executor'].items():self.assertEqual(sha,s.digest((ROOT/path).read_bytes()),path)
        for path,sha in pins['producer'].items():self.assertEqual(sha,s.digest(s.tags.guard.source_bytes(ROOT,s.SOURCE,path)),path)


if __name__=='__main__':unittest.main()
