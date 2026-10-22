#!/usr/bin/env python3
"""Bounded causal controls for the actual closed settlement and HTTP adapter."""
import copy
import datetime
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import re
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
    """Shared provider state. Role views enforce the production closed route sets."""
    def __init__(self):
        self.calls = []; self.by_role = []; self.states = {i: 'active' for i in s.FENCES}; self.refs = {}
        self.policy = copy.deepcopy(POLICY); self.active = False; self.census_unknown = False
        self.create = 'accepted'; self.retire_fail = set(); self.restore = True; self.hook = lambda *a: None
        self.retired = []

    def call(self, method, path, body=None, role=None):
        self.calls.append((method, path, body)); self.by_role.append((role, method, path)); self.hook(method, path, body)
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
            if role in self.retire_fail: raise s.Refused('retirement unavailable')
            self.retired.append(role)
            return 204, None
        raise AssertionError((method, path))


class View:
    """One credential's view: production route policy, shared provider state."""
    def __init__(self, api, role, token=None):
        self.api, self.role, self.token, self.retired = api, role, token or 'token-' + role, False

    def call(self, method, path, body=None, cleanup_deadline=None):
        if not s.route_allowed(self.role, method, path, body):
            raise s.Refused('unsupported endpoint or request body')
        return self.api.call(method, path, body, role=self.role)

    def get(self, path):
        return self.call('GET', s.PREFIX + path)[1]

    def retire(self, cleanup_deadline):
        if self.role not in ('reader', 'writer'): raise s.Refused('only a minted installation token can be retired')
        result = self.call('DELETE', '/installation/token')
        self.retired = True
        return result


def views(api):
    return View(api, 'actions'), View(api, 'reader'), View(api, 'writer')


class SettlementTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.api = FakeAPI(); self.root = Path(self.temp.name)
        self.op = s.Settlement(self.root, {'GITHUB_RUN_ID': '123'}, copy.deepcopy(ADMITTED), PINS,
            *views(self.api), identity=lambda: None)
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
            self.api = FakeAPI(); self.op.actions, self.op.reader, self.op.writer = views(self.api)
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
            self.api = FakeAPI(); self.api.create = mode; self.op.actions, self.op.reader, self.op.writer = views(self.api)
            result = self.op.run()
            self.assertEqual('Verified', result['sameIntentConvergence']); self.assertEqual(3,len(self.writes()))

    def test_never_accepted_stops_after_one_request(self):
        self.api.create = 'never'
        with self.assertRaises(s.Refused): self.op.run()
        self.assertEqual(1,len(self.writes())); self.assertEqual('Unknown',self.op.receipt['sameIntentConvergence'])
        self.assertEqual('Unknown-owned-gap',self.op.receipt['cleanup'])
        self.assertEqual([s.REFS[0]],self.op.receipt['pendingSubordinateRefs'])
        self.assertTrue(all(state=='disabled_manually' for state in self.api.states.values()))

    def test_uncertain_template_create_remains_fenced_even_after_revocation_and_late_arrival(self):
        self.api.refs[s.REFS[0]]=s.SOURCE
        self.api.create='never'
        with self.assertRaises(s.Refused):self.op.run()
        self.assertEqual([s.REFS[1]],self.op.receipt['pendingSubordinateRefs'])
        self.assertEqual('Unknown-owned-gap',self.op.receipt['cleanup'])
        # Model the admitted request accepting after token retirement/finally.
        self.api.refs[s.REFS[1]]=s.SOURCE
        self.assertTrue(all(state=='disabled_manually' for state in self.api.states.values()))
        self.assertFalse(any(c[0]=='PUT' and c[1].endswith('/enable') for c in self.api.calls))

    def test_acknowledged_create_with_failed_fence_keeps_pending_event_fence(self):
        original_fence=self.op.fence
        def fence():
            if self.writes():raise s.Refused('main moved before readback')
            original_fence()
        self.op.fence=fence
        with self.assertRaises(s.Refused):self.op.run()
        self.assertEqual([s.REFS[0]],self.op.receipt['pendingSubordinateRefs'])
        self.assertTrue(all(state=='disabled_manually' for state in self.api.states.values()))

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
            self.api = FakeAPI(); self.op.actions, self.op.reader, self.op.writer = views(self.api)
            self.api.policy[0][field] = value
            # Even a freshly selected digest cannot turn insufficient policy into proof.
            self.op.admitted['policySha256'] = s.digest(json.dumps(self.api.policy,sort_keys=True,separators=(',',':')).encode())
            with self.assertRaises(s.Refused):self.op.run()
            self.assertEqual([],self.writes())
        self.op.admitted['policySha256']='0'*64
        with self.assertRaises(s.Refused):self.op.run()

    def test_disabled_workflow_with_active_old_job_and_unknown_census_block(self):
        for field in ('active','census_unknown'):
            self.api=FakeAPI();self.op.actions,self.op.reader,self.op.writer=views(self.api);setattr(self.api,field,True)
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
        self.api=FakeAPI();self.op.actions,self.op.reader,self.op.writer=views(self.api);self.api.active=True;self.api.retire_fail={'writer'}
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
                api=FakeAPI();actions,reader,writer=views(api);op=s.Settlement(Path(d),dict(env),copy.deepcopy(ADMITTED),PINS,actions,reader,writer)
                def get_main(path):
                    self.assertEqual('git/ref/heads/main',path)
                    return {'object':{'type':'commit','sha':'b'*40 if mutant=='main' else 'a'*40,'url':'https://api.github.com/repos/'+s.REPO+'/git/commits/'+'a'*40}}
                def get(path):
                    if path.startswith('actions/runs/'):
                        return {'status':'in_progress' if mutant=='old-active' else 'completed','run_attempt':2 if mutant=='old-attempt' else 1,'head_sha':PINS['executorSha'],'actor':{'id':0 if mutant=='actor' else PINS['actorId']},'event':'workflow_dispatch','path':'.github/workflows/release-tags.yml'}
                    if path.startswith('actions/jobs/'):return {'run_id':0 if mutant=='old-job' else PINS['run'],'status':'completed'}
                    raise AssertionError(path)
                actions.get=get;reader.get=get_main
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
        api=s.API('private-test-credential','writer',lambda *a,**k:None,10**20)
        for method,path,body in [('PATCH',s.PREFIX+'git/refs/tags/v0.32.3',{}),('DELETE',s.PREFIX+'git/refs/tags/v0.32.3',None),('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':'b'*40}),('PUT',s.PREFIX+'actions/runs/123/cancel',None)]:
            with patch.object(api.opener,'open',side_effect=AssertionError('must not transport')),self.assertRaises(s.Refused):api.call(method,path,body)

    def test_actual_adapter_retains_raw_response_and_request_id_once_without_auth(self):
        journal=[];api=s.API('private-test-credential','writer',lambda stage,**fields:journal.append((stage,fields)),10**20)
        class Response(io.BytesIO):
            code=201;headers={'x-github-request-id':'fixture-request'}
        with patch.object(api.opener,'open',return_value=Response(b'{"created":true}')) as opening:
            self.assertEqual((201,{'created':True}),api.call('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':s.SOURCE}))
        self.assertEqual(1,opening.call_count);self.assertEqual('fixture-request',journal[-1][1]['requestId'])
        self.assertNotIn('private-test-credential',json.dumps(journal))

    def test_actual_adapter_timeout_unknown_and_body_bound_never_retry(self):
        for failure in (TimeoutError('credential-private'),OSError('credential-private')):
            journal=[];api=s.API('credential-private','writer',lambda stage,**fields:journal.append((stage,fields)),10**20)
            with patch.object(api.opener,'open',side_effect=failure) as opening,self.assertRaises(s.Refused):
                api.call('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':s.SOURCE})
            self.assertEqual(1,opening.call_count);self.assertEqual('http-outcome-unknown',journal[-1][0])
            self.assertNotIn('credential-private',json.dumps(journal))
        api=s.API('credential-private','reader',lambda *a,**k:None,10**20)
        api.deadline=0
        with patch.object(api.opener,'open',side_effect=AssertionError('deadline forbids transport')),self.assertRaises(s.Refused):
            api.call('GET',s.PREFIX+'git/ref/tags/v0.32.3')

    def test_real_adapter_cleanup_reserve_cannot_extend_create_authority(self):
        api=s.API('private-fixture','writer',lambda *a,**k:None,0)
        class Response(io.BytesIO):
            code=204;headers={}
        with patch.object(api.opener,'open',return_value=Response(b'')) as opening:
            self.assertEqual((204,None),api.retire(10**20))
            self.assertEqual(1,opening.call_count)
        with patch.object(api.opener,'open',side_effect=AssertionError('expired effects remain forbidden')),self.assertRaises(s.Refused):
            api.call('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':s.SOURCE})
        with self.assertRaises(s.Refused):
            api.call('POST',s.PREFIX+'git/refs',{'ref':s.REFS[0],'sha':s.SOURCE},cleanup_deadline=10**20)

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
            'originalTransportOutcome':'Unknown','sameIntentConvergence':'Verified','cleanup':'restored','pendingSubordinateRefs':[],'result':'passed',
            'publication':'not-performed','notification':'not-performed','executorSha':'b'*40,'run':'11','attempt':'1',
            'workflowRef':s.WORKFLOW,'refs':{r:s.SOURCE for r in s.REFS}}
        for mutant in (None,'historical-outcome','convergence','cleanup','effect','annotated-current','wrong-current','wrong-native','changed-bytes','pending-subordinate'):
            with self.subTest(mutant=mutant),tempfile.TemporaryDirectory() as d:
                root=Path(d);doc=copy.deepcopy(receipt)
                if mutant=='historical-outcome':doc['originalTransportOutcome']='Rejected'
                if mutant=='convergence':doc['sameIntentConvergence']='Unknown'
                if mutant=='cleanup':doc['cleanup']='Unknown-owned-gap'
                if mutant=='effect':doc['effectId']='new-effect'
                if mutant=='pending-subordinate':doc['pendingSubordinateRefs']=[s.REFS[1]]
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


# ---- derived credentials: exact grants, role separation, masking and revocation -------------------------------
JWT = 'jwt.header.signature-fixture-0000'
TOKENS = {'actions': 'ghs_job_token_fixture_000000', 'reader': 'ghs_reader_token_fixture_0000', 'writer': 'ghs_writer_token_fixture_0000'}
INSTALLATION = {'id': 777, 'app_id': 4242, 'app_slug': s.cap.APP_SLUG, 'account': {'login': 'FS-GG', 'type': 'Organization', 'id': 9},
    'target_id': 9, 'target_type': 'Organization', 'suspended_at': None, 'repository_selection': 'all',
    'permissions': {'contents': 'write', 'workflows': 'write', 'administration': 'write'}, 'events': []}


def good_grant(role, **changes):
    value = {'token': TOKENS[role], 'permissions': dict(s.MINTS[role]['permissions']), 'repository_selection': 'selected',
        'repositories': [{'id': s.cap.REPO_ID, 'name': 'FS.GG.Rendering', 'full_name': s.REPO}],
        'expires_at': (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=59)).isoformat().replace('+00:00', 'Z')}
    value.update(changes)
    return value


class Deriver:
    """Provider fake for the App role plus the role views on the shared settlement state."""
    def __init__(self, api, **overrides):
        self.api, self.overrides, self.mints, self.masked, self.order, self.apps = api, overrides, [], [], [], []

    def factory(self, token, role, journal, deadline):
        self.order.append(('adapter', role, token))
        if role == 'app':
            return AppView(self, token)
        return View(self.api, role, token)

    def mask(self, token):
        self.masked.append(token); self.order.append(('mask', token))


class AppView:
    def __init__(self, deriver, token):
        self.deriver, self.token = deriver, token

    def call(self, method, path, body=None, cleanup_deadline=None):
        if not s.route_allowed('app', method, path, body): raise s.Refused('unsupported endpoint or request body')
        d = self.deriver
        if method == 'GET':
            return d.overrides.get('installation_status', 200), d.overrides.get('installation', copy.deepcopy(INSTALLATION))
        role = 'reader' if body == s.MINTS['reader'] else 'writer'
        d.mints.append((role, path, copy.deepcopy(body)))
        if d.overrides.get('fail_mint') == role: raise s.Refused('mint transport unknown')
        return d.overrides.get('mint_status', 201), d.overrides.get('grants', {}).get(role, good_grant(role))


class DerivedBase(unittest.TestCase):
    def build(self, **overrides):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.api = FakeAPI(); self.deriver = Deriver(self.api, **overrides)
        self.signed = []
        def signer(app_id, key, budget, cleanup):
            self.signed.append((app_id, key)); cleanup['temporaryKey'] = 'removed'
            if overrides.get('signer_fails'): raise s.cap.Refused('signing-unavailable')
            return JWT
        env = {'GITHUB_RUN_ID': '123', 'GITHUB_TOKEN': TOKENS['actions']}
        actions = View(self.api, 'actions', TOKENS['actions'])
        self.op = s.Settlement(Path(self.temp.name), env, copy.deepcopy(ADMITTED), PINS, actions, identity=lambda: None,
            app=('4242', 'PRIVATE KEY fixture ' + 'x' * 100), api_factory=self.deriver.factory, signer=signer, mask=self.deriver.mask)
        self.op.inventory = lambda: None
        return self.op

    def writes(self):
        return [c for c in self.api.calls if c[0] in ('POST', 'PUT')]


class DerivedCredentialTests(DerivedBase):
    def test_success_mints_exact_requests_masks_before_use_and_retires_both_after_writer_before_restore(self):
        op = self.build(); result = op.run()
        self.assertEqual('passed', result['result']); self.assertEqual('restored', result['cleanup'])
        self.assertEqual([('reader', s.MINTS['reader']), ('writer', s.MINTS['writer'])], [(m[0], m[2]) for m in self.deriver.mints])
        self.assertEqual({'administration': 'read', 'metadata': 'read'}, self.deriver.mints[0][2]['permissions'])
        self.assertEqual({'contents': 'write', 'workflows': 'write', 'metadata': 'read'}, self.deriver.mints[1][2]['permissions'])
        self.assertEqual(['FS.GG.Rendering'], self.deriver.mints[1][2]['repositories'])
        self.assertTrue(all(m[1] == '/app/installations/777/access_tokens' for m in self.deriver.mints))
        self.assertEqual([JWT, TOKENS['reader'], TOKENS['writer']], self.deriver.masked)
        for token in self.deriver.masked:  # the credential is masked before any adapter ever holds it
            self.assertLess(self.deriver.order.index(('mask', token)), self.deriver.order.index(next(o for o in self.deriver.order if o[0] == 'adapter' and o[2] == token)))
        self.assertEqual(['writer', 'reader'], self.api.retired)
        calls = [(r, m, p) for r, m, p in self.api.by_role]
        writer_gone = next(i for i, c in enumerate(calls) if c[:2] == ('writer', 'DELETE'))
        first_enable = next(i for i, c in enumerate(calls) if c[1] == 'PUT' and c[2].endswith('/enable'))
        reader_gone = next(i for i, c in enumerate(calls) if c[:2] == ('reader', 'DELETE'))
        self.assertLess(writer_gone, first_enable); self.assertLess(first_enable, reader_gone)
        self.assertIsNone(op.app)  # the App secret is dropped as soon as it signs
        self.assertEqual(1, len(self.signed))

    def test_wrong_or_excess_minted_permissions_and_scope_are_refused_before_any_effect_and_still_retired(self):
        for role in ('reader', 'writer'):
            exact = s.MINTS[role]['permissions']
            wrong = [dict(exact, administration='write') if role == 'reader' else dict(exact, administration='read'),
                     dict(exact, packages='read'), {k: v for k, v in exact.items() if k != 'metadata'},
                     {**exact, **{k: 'write' for k in exact if exact[k] == 'read'}}, {**exact, **{k: 'read' for k in exact if exact[k] == 'write'}}, {}]
            for permissions in [w for w in wrong if w != exact]:
                with self.subTest(role=role, permissions=permissions):
                    op = self.build(grants={role: good_grant(role, permissions=permissions)})
                    with self.assertRaises(s.Refused): op.run()
                    self.assertEqual([], self.writes())          # no workflow disable, no ref create
                    self.assertIn(role, self.api.retired)         # the refused token is still revoked
                    self.assertEqual('refused-or-unknown', op.receipt['result'])
            for field, value in (('repository_selection', 'all'), ('repositories', []), ('repositories', None),
                    ('repositories', [{'id': 1, 'name': 'Other', 'full_name': 'FS-GG/Other'}]),
                    ('repositories', [{'id': s.cap.REPO_ID, 'name': 'FS.GG.Rendering', 'full_name': s.REPO}] * 2),
                    ('repositories', [{'id': 5, 'name': 'FS.GG.Rendering', 'full_name': s.REPO}]),
                    ('repositories', [{'id': s.cap.REPO_ID, 'name': 'FS.GG.Rendering', 'full_name': 'FS-GG/Other'}]),
                    ('repositories', [{'id': s.cap.REPO_ID, 'name': 'Other', 'full_name': s.REPO}]),
                    ('expires_at', 'tomorrow'), ('expires_at', None), ('token', 'short'), ('token', None)):
                with self.subTest(role=role, field=field):
                    op = self.build(grants={role: good_grant(role, **{field: value})})
                    with self.assertRaises(s.Refused): op.run()
                    self.assertEqual([], self.writes())
        stale = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(hours=5)).isoformat()
        op = self.build(grants={'writer': good_grant('writer', expires_at=stale)})
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual([], self.writes())

    def test_writer_mint_failure_retires_the_reader_and_performs_no_effect(self):
        for failure in ({'fail_mint': 'writer'}, {'mint_status': 403}):
            op = self.build(**failure)
            with self.assertRaises(s.Refused): op.run()
            self.assertEqual([], self.writes())
            if 'fail_mint' in failure: self.assertEqual(['reader'], self.api.retired)

    def test_installation_identity_and_signing_failures_refuse_before_any_mint(self):
        for override in ({'installation': dict(INSTALLATION, app_id=1)}, {'installation': dict(INSTALLATION, suspended_at='2026-01-01')},
                {'installation': dict(INSTALLATION, app_slug='other-app')}, {'installation_status': 404}, {'signer_fails': True}):
            op = self.build(**override)
            with self.assertRaises(s.Refused): op.run()
            self.assertEqual([], self.deriver.mints); self.assertEqual([], self.writes()); self.assertEqual([], self.api.retired)
        op = self.build(); op.app = ('0', 'k')
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual([], self.signed[:0]); self.assertEqual([], self.deriver.mints)

    def test_no_secret_is_used_before_the_local_executor_guards_pass(self):
        op = self.build(); op.local_check = lambda: (_ for _ in ()).throw(s.Refused('executor context refused'))
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual([], self.signed); self.assertEqual([], self.deriver.mints); self.assertEqual([], self.deriver.masked)
        self.assertEqual([], self.api.calls)

    def test_installation_token_is_not_reused_across_roles_and_distinctness_is_enforced(self):
        op = self.build(grants={'writer': good_grant('writer', token=TOKENS['reader'])})
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual([], self.writes())
        op = self.build(grants={'reader': good_grant('reader', token=TOKENS['actions'])})
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual([], self.writes())

    def test_job_token_is_never_used_for_rulesets_refs_tags_or_retirement(self):
        self.build().run()
        for role, method, path in self.api.by_role:
            if role == 'actions':
                self.assertTrue(path.startswith(s.PREFIX + 'actions/'), path)
                self.assertIn(method, ('GET', 'PUT'))
                self.assertNotIn('rulesets', path); self.assertNotIn('git/', path)
        self.assertEqual({'actions', 'reader', 'writer'}, {r for r, _, _ in self.api.by_role})

    def test_ruleset_reader_never_writes_and_writer_never_reads_or_toggles_workflows(self):
        self.build().run()
        for role, method, path in self.api.by_role:
            if role == 'reader':
                self.assertIn(method, ('GET', 'DELETE'))
                if method == 'DELETE': self.assertEqual('/installation/token', path)
                else: self.assertTrue('rulesets' in path or 'git/ref/' in path, path)
            if role == 'writer':
                self.assertIn(method, ('POST', 'DELETE'))
                self.assertTrue(path == s.PREFIX + 'git/refs' or path == '/installation/token', path)
        self.assertEqual(list(s.REFS), [c[2]['ref'] for c in self.writes() if c[0] == 'POST'])
        self.assertTrue(any(r == 'reader' and 'rulesets' in p for r, _, p in self.api.by_role))

    def test_failure_after_minting_still_revokes_both_tokens_and_keeps_fences_for_unrevoked_writer(self):
        op = self.build(); self.api.policy[0]['bypass_actors'] = [{'actor_id': 1}]
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual({'writer', 'reader'}, set(self.api.retired)); self.assertEqual([], self.writes())
        op = self.build(); self.api.create = 'never'
        with self.assertRaises(s.Refused): op.run()
        self.assertEqual({'writer', 'reader'}, set(self.api.retired))

    def test_revocation_failures_are_retained_as_owned_gaps_without_cross_blocking(self):
        op = self.build(); self.api.retire_fail = {'reader'}
        with self.assertRaises(s.Refused): op.run()   # triple converged and workflows restored, but a live read credential remains
        self.assertEqual('Verified', op.receipt['sameIntentConvergence'])
        self.assertEqual('Unknown-owned-gap', op.receipt['cleanup']); self.assertEqual('refused-or-unknown', op.receipt['result'])
        self.assertTrue(all(state == 'active' for state in self.api.states.values()))
        self.assertTrue(any(e.get('operation') == 'reader-retirement' for e in op.receipt['events']))
        op = self.build(); self.api.retire_fail = {'writer'}
        with self.assertRaises(s.Refused): op.run()
        self.assertTrue(all(state == 'disabled_manually' for state in self.api.states.values()))  # unretired writer keeps fences closed
        self.assertIn('reader', self.api.retired)

    def test_receipt_and_stdout_never_contain_a_credential_or_private_key(self):
        out = io.StringIO()
        with patch.object(s.sys, 'stdout', out):
            op = self.build(mask=None); op.mask = s.mask_credential; op.run()
        self.assertEqual({'::add-mask::' + t for t in (JWT, TOKENS['reader'], TOKENS['writer'])}, set(out.getvalue().split()))
        raw = (Path(self.temp.name) / 'artifacts/tag-settlement/settlement.json').read_text()
        for secret in (JWT, TOKENS['reader'], TOKENS['writer'], TOKENS['actions'], 'PRIVATE KEY fixture'):
            self.assertNotIn(secret, raw)
        minted = [e for e in op.receipt['events'] if e['stage'] == 'credential-minted']
        self.assertEqual([{'administration': 'read', 'metadata': 'read'}, {'contents': 'write', 'metadata': 'read', 'workflows': 'write'}], [e['permissions'] for e in minted])


class RealAdapterDerivationTests(unittest.TestCase):
    """Real adapters over a scripted transport: exact bearer-to-route mapping and no credential persistence."""
    def run_settlement(self, grants=None, only_derive=False):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.requests = []; self.roles = {TOKENS['actions']: 'actions', JWT: 'app', TOKENS['reader']: 'reader', TOKENS['writer']: 'writer'}
        pins = json.loads((ROOT / 'eng/release/font-disposal-0.32.3-workflow-fences.json').read_text())
        ids = dict(s.FENCES); pairs = []; free = iter(range(1000, 2000))
        for path in sorted(pins['executor']):
            name = path.rsplit('/', 1)[1]
            pairs.append((next((i for i, n in s.FENCES.items() if n == name), None) or next(free), path))
        inventory = s.digest(json.dumps(sorted(pairs), separators=(',', ':')).encode())
        self.state = {i: 'active' for i in s.FENCES}; self.refs = {}
        settlement_id = next(i for i, p in pairs if p.endswith('release-tag-settlement.yml'))
        grants = grants or {}
        class Response(io.BytesIO):
            def __init__(inner, code, value):
                super().__init__(json.dumps(value).encode() if value is not None else b''); inner.code, inner.headers = code, {'x-github-request-id': 'F:1'}
            def __enter__(inner): return inner
            def __exit__(inner, *a): return False
        def route(request, timeout=None):
            token = request.get_header('Authorization').removeprefix('Bearer ')
            method, path = request.get_method(), request.full_url.removeprefix('https://api.github.com')
            body = json.loads(request.data) if request.data else None
            role = self.roles.get(token, 'unknown'); self.requests.append((role, method, path, body))
            short = path.removeprefix(s.PREFIX)
            if method == 'GET' and path == s.INSTALLATION_PATH: return Response(200, INSTALLATION)
            if method == 'POST' and path.endswith('/access_tokens'):
                kind = 'reader' if body == s.MINTS['reader'] else 'writer'
                return Response(201, grants.get(kind, good_grant(kind)))
            if method == 'DELETE': return Response(204, None)
            if short.startswith('rulesets?'): return Response(200, [{'id': 1, '_links': {'self': {'href': 'https://api.github.com' + s.PREFIX + 'rulesets/1'}}}])
            if short == 'rulesets/1': return Response(200, POLICY[0])
            if short == 'git/ref/heads/main': return Response(200, {'object': {'type': 'commit', 'sha': 'a' * 40, 'url': 'https://api.github.com/repos/' + s.REPO + '/git/commits/' + 'a' * 40}})
            if short.startswith('git/ref/'):
                ref = 'refs/' + short.removeprefix('git/ref/')
                return Response(200, {'ref': ref, 'object': {'type': 'commit', 'sha': s.SOURCE}}) if ref in self.refs else Response(404, {'message': 'Not Found'})
            if method == 'POST': self.refs[body['ref']] = body['sha']; return Response(201, {'ref': body['ref']})
            if short == 'actions/workflows?per_page=100': return Response(200, {'total_count': len(pairs), 'workflows': [{'id': i, 'path': p} for i, p in pairs]})
            if short == f'actions/workflows/{settlement_id}/runs?per_page=100': return Response(200, {'total_count': 1, 'workflow_runs': [{'id': 123}]})
            if short.startswith('actions/runs/'): return Response(200, {'status': 'completed', 'run_attempt': 1, 'head_sha': PINS['executorSha'], 'actor': {'id': PINS['actorId']}, 'event': 'workflow_dispatch', 'path': '.github/workflows/release-tags.yml'})
            if short.startswith('actions/jobs/'): return Response(200, {'run_id': PINS['run'], 'status': 'completed'})
            if method == 'PUT':
                i = int(short.split('/')[2]); self.state[i] = 'active' if short.endswith('enable') else 'disabled_manually'; return Response(204, None)
            if short.startswith('actions/workflows/') and '/runs?' in short: return Response(200, {'total_count': 0, 'workflow_runs': []})
            if short.startswith('actions/workflows/'): return Response(200, {'state': self.state[int(short.split('/')[2])]})
            raise AssertionError((method, path))
        class Opener:
            open = staticmethod(route)
        admitted = copy.deepcopy(ADMITTED, ); admitted['workflowInventorySha256'] = inventory
        env = {'GITHUB_RUN_ID': '123', 'GITHUB_TOKEN': TOKENS['actions'], 'FSGG_EXPECTED_EXECUTOR_SHA': 'a' * 40}
        def signer(app_id, key, budget, cleanup):
            cleanup['temporaryKey'] = 'removed'; return JWT
        with patch.object(s.urllib.request, 'build_opener', return_value=Opener()), patch.object(s.Settlement, 'local_identity', lambda self: None):
            op = s.Settlement(Path(self.temp.name), env, admitted, PINS, app=('4242', 'k' * 120), signer=signer, mask=lambda token: None)
            op.root = ROOT; op.path = Path(self.temp.name) / 'settlement.json'
            try: result = op.run()
            except s.Refused: result = None
        return op, result

    def test_end_to_end_real_adapters_use_closed_routes_and_bearer_role_mapping(self):
        op, result = self.run_settlement()
        self.assertIsNotNone(result); self.assertEqual('passed', result['result'])
        self.assertNotIn('unknown', {r for r, *_ in self.requests})
        for role, method, path, body in self.requests:
            self.assertTrue(s.route_allowed(role, method, path, body), (role, method, path))
        roles = {r: {(m, p.removeprefix(s.PREFIX).split('?')[0].split('/')[0] if p.startswith(s.PREFIX) else p) for rr, m, p, _ in self.requests if rr == r} for r in set(r for r, *_ in self.requests)}
        self.assertEqual({('GET', 'actions'), ('PUT', 'actions')}, roles['actions'])
        self.assertEqual({('GET', 'rulesets'), ('GET', 'git'), ('DELETE', '/installation/token')}, roles['reader'])
        self.assertEqual({('POST', 'git'), ('DELETE', '/installation/token')}, roles['writer'])
        self.assertEqual(3, len([r for r in self.requests if r[0] == 'writer' and r[1] == 'POST']))
        self.assertEqual({r: s.SOURCE for r in s.REFS}, self.refs)
        raw = (Path(self.temp.name) / 'settlement.json').read_text()
        for secret in (JWT, *TOKENS.values()): self.assertNotIn(secret, raw)
        self.assertEqual(2, len([r for r in self.requests if r[1] == 'DELETE']))

    def test_end_to_end_excess_grant_refuses_before_effect_and_revokes_through_real_adapter(self):
        op, result = self.run_settlement(grants={'writer': good_grant('writer', permissions={**s.WRITER_PERMISSIONS, 'administration': 'write'})})
        self.assertIsNone(result)
        self.assertEqual([], [r for r in self.requests if r[1] in ('PUT',) or (r[1] == 'POST' and r[2].endswith('git/refs'))])
        self.assertEqual({'writer', 'reader'}, {r[0] for r in self.requests if r[1] == 'DELETE'})
        raw = (Path(self.temp.name) / 'settlement.json').read_text()
        for secret in (JWT, *TOKENS.values()): self.assertNotIn(secret, raw)


class RouteIsolationTests(unittest.TestCase):
    def refused(self, role, method, path, body=None):
        api = s.API('credential-fixture', role, lambda *a, **k: None, 10**20)
        with patch.object(api.opener, 'open', side_effect=AssertionError('must not transport')), self.assertRaises(s.Refused):
            api.call(method, path, body)

    def test_job_token_cannot_reach_rulesets_refs_tags_mint_or_retirement(self):
        for method, path, body in [('GET', s.PREFIX + 'rulesets?includes_parents=true&per_page=100', None), ('GET', s.PREFIX + 'rulesets/1', None),
                ('GET', s.PREFIX + 'git/ref/heads/main', None), ('GET', s.PREFIX + 'git/ref/tags/v0.32.3', None),
                ('POST', s.PREFIX + 'git/refs', {'ref': s.REFS[0], 'sha': s.SOURCE}), ('DELETE', '/installation/token', None),
                ('POST', '/app/installations/1/access_tokens', s.MINTS['writer']), ('GET', s.INSTALLATION_PATH, None),
                ('PUT', s.PREFIX + 'actions/workflows/1/disable', None), ('PUT', s.PREFIX + 'actions/runs/1/cancel', None),
                ('GET', s.PREFIX + 'actions/artifacts/1/zip', None), ('GET', s.PREFIX + 'actions/runs/1/../../rulesets/1', None)]:
            with self.subTest(method=method, path=path): self.refused('actions', method, path, body)
        for kind in ('disable', 'enable'):
            self.assertTrue(s.route_allowed('actions', 'PUT', s.PREFIX + f'actions/workflows/{next(iter(s.FENCES))}/{kind}', None))

    def test_admin_read_token_cannot_write_toggle_read_actions_or_mint(self):
        for method, path, body in [('POST', s.PREFIX + 'git/refs', {'ref': s.REFS[0], 'sha': s.SOURCE}), ('PUT', s.PREFIX + f'actions/workflows/{next(iter(s.FENCES))}/disable', None),
                ('DELETE', s.PREFIX + 'git/refs/tags/v0.32.3', None), ('PATCH', s.PREFIX + 'rulesets/1', {}), ('PUT', s.PREFIX + 'rulesets/1', {}),
                ('GET', s.PREFIX + 'actions/workflows?per_page=100', None), ('GET', s.PREFIX + 'contents/README.md', None),
                ('POST', '/app/installations/1/access_tokens', s.MINTS['reader']), ('GET', s.INSTALLATION_PATH, None)]:
            with self.subTest(method=method, path=path): self.refused('reader', method, path, body)
        for method, path in [('GET', s.PREFIX + 'rulesets?includes_parents=true&per_page=100'), ('GET', s.PREFIX + 'rulesets/25621755'),
                ('GET', s.PREFIX + 'git/ref/tags/fs-gg-ui/v0.32.3'), ('GET', s.PREFIX + 'git/ref/heads/main'), ('DELETE', '/installation/token')]:
            self.assertTrue(s.route_allowed('reader', method, path, None), path)

    def test_writer_is_create_only_for_the_three_admitted_refs(self):
        for method, path, body in [('GET', s.PREFIX + 'git/ref/heads/main', None), ('GET', s.PREFIX + 'rulesets/1', None),
                ('POST', s.PREFIX + 'git/refs', {'ref': 'refs/heads/main', 'sha': s.SOURCE}), ('POST', s.PREFIX + 'git/refs', {'ref': s.REFS[0], 'sha': 'b' * 40}),
                ('PATCH', s.PREFIX + 'git/refs/tags/v0.32.3', {'sha': s.SOURCE}), ('DELETE', s.PREFIX + 'git/refs/tags/v0.32.3', None),
                ('PUT', s.PREFIX + 'actions/workflows/1/enable', None), ('POST', '/app/installations/1/access_tokens', s.MINTS['writer'])]:
            with self.subTest(method=method, path=path): self.refused('writer', method, path, body)
        for ref in s.REFS: self.assertTrue(s.route_allowed('writer', 'POST', s.PREFIX + 'git/refs', {'ref': ref, 'sha': s.SOURCE}))

    def test_app_jwt_can_only_read_the_installation_and_request_the_two_exact_mints(self):
        excess = [dict(s.MINTS['writer'], permissions={**s.WRITER_PERMISSIONS, 'administration': 'write'}),
                  dict(s.MINTS['reader'], permissions={**s.READER_PERMISSIONS, 'contents': 'read'}),
                  {'permissions': s.READER_PERMISSIONS}, dict(s.MINTS['reader'], repositories=['Other', 'FS.GG.Rendering']),
                  dict(s.MINTS['reader'], repositories=[]), {}, None]
        for body in excess:
            with self.subTest(body=body): self.refused('app', 'POST', '/app/installations/1/access_tokens', body)
        for method, path, body in [('GET', s.PREFIX + 'rulesets/1', None), ('DELETE', '/installation/token', None), ('POST', s.PREFIX + 'git/refs', {'ref': s.REFS[0], 'sha': s.SOURCE}),
                ('POST', '/app/installations/x/access_tokens', s.MINTS['reader']), ('DELETE', '/app/installations/1', None)]:
            with self.subTest(method=method, path=path): self.refused('app', method, path, body)
        self.assertTrue(s.route_allowed('app', 'GET', s.INSTALLATION_PATH, None))
        for body in s.MINTS.values(): self.assertTrue(s.route_allowed('app', 'POST', '/app/installations/1/access_tokens', body))

    def test_declared_permission_sets_are_the_exact_reviewed_ones(self):
        self.assertEqual({'contents': 'write', 'workflows': 'write', 'metadata': 'read'}, s.WRITER_PERMISSIONS)
        self.assertEqual({'administration': 'read', 'metadata': 'read'}, s.READER_PERMISSIONS)
        self.assertEqual(['FS.GG.Rendering'], s.APP_REPOSITORIES)
        self.assertEqual({'reader', 'writer'}, set(s.MINTS))

    def test_job_token_cannot_be_retired_and_unknown_role_is_refused(self):
        api = s.API('credential-fixture', 'actions', lambda *a, **k: None, 10**20)
        with patch.object(api.opener, 'open', side_effect=AssertionError('must not transport')), self.assertRaises(s.Refused): api.retire(10**20)
        with self.assertRaises(s.Refused): s.API('credential-fixture', 'admin', lambda *a, **k: None, 10**20)

    def test_mint_response_body_is_never_journaled(self):
        journal = []; api = s.API(JWT, 'app', lambda stage, **f: journal.append((stage, f)), 10**20)
        class Response(io.BytesIO):
            code = 201; headers = {}
        with patch.object(api.opener, 'open', return_value=Response(json.dumps(good_grant('writer')).encode())):
            status, value = api.call('POST', '/app/installations/9/access_tokens', s.MINTS['writer'])
        self.assertEqual(TOKENS['writer'], value['token']); self.assertNotIn(TOKENS['writer'], json.dumps(journal))

    def test_main_removes_every_secret_from_the_environment_before_any_child_or_guard_runs(self):
        seen = {}
        class Spy:
            def __init__(self, root, env, admitted, binding, **kw):
                seen.update(env=dict(env), environ=dict(s.os.environ), app=kw['app'])
            def run(self): return {}
        environment = {'GITHUB_TOKEN': TOKENS['actions'], 'FSGG_DISPATCH_APP_ID': '4242', 'FSGG_DISPATCH_APP_PRIVATE_KEY': 'PRIVATE KEY fixture',
                       'FSGG_EXPECTED_EXECUTOR_SHA': 'a' * 40}
        with tempfile.TemporaryDirectory() as d:
            raw = json.dumps(ADMITTED).encode(); (Path(d) / 'admission.json').write_bytes(raw)
            argv = ['settle', '--admission', str(Path(d) / 'admission.json'), '--admission-sha256', s.digest(raw), '--original-directory', d]
            with patch.object(s.sys, 'argv', argv), patch.dict(s.os.environ, environment), patch.object(s.Path, 'cwd', return_value=Path(d)), \
                 patch.object(s, 'original', return_value={}), patch.object(s, 'Settlement', Spy):
                self.assertEqual(0, s.main())
        for name in ('GITHUB_TOKEN', 'FSGG_DISPATCH_APP_ID', 'FSGG_DISPATCH_APP_PRIVATE_KEY'):
            self.assertNotIn(name, seen['environ'])
        self.assertNotIn('FSGG_DISPATCH_APP_PRIVATE_KEY', seen['env']); self.assertNotIn('FSGG_DISPATCH_APP_ID', seen['env'])
        self.assertEqual(('4242', 'PRIVATE KEY fixture'), seen['app'])
        self.assertEqual(TOKENS['actions'], seen['env']['GITHUB_TOKEN'])  # handed only to the settlement, never to a child

    def test_workflow_grants_least_privilege_and_no_user_provisioned_secret(self):
        text = '\n'.join(l for l in (ROOT / '.github/workflows/release-tag-settlement.yml').read_text().splitlines() if not l.lstrip().startswith('#'))
        job = text.split('\njobs:\n')[1]
        self.assertRegex(job, r'(?m)^    permissions:\n      actions: write\n      contents: read\n(?!      )')
        self.assertEqual(1, len(re.findall(r'(?m)^    permissions:', job)))
        for forbidden in ('FSGG_SETTLEMENT_ADMIN_TOKEN', 'FSGG_SETTLEMENT_WRITER_TOKEN', 'id-token', 'administration', 'workflows:', 'contents: write', 'pull-requests', 'packages'):
            self.assertNotIn(forbidden, text)
        secrets = re.findall(r'secrets\.([A-Z_]+)', text)
        self.assertEqual(['FSGG_DISPATCH_APP_ID', 'FSGG_DISPATCH_APP_PRIVATE_KEY'], secrets)
        steps = re.split(r'(?m)^      - ', job)[1:]
        holders = [x for x in steps if 'secrets.' in x]
        self.assertEqual(1, len(holders)); self.assertIn('scripts/settle-original-tags.py', holders[0])
        self.assertLess(next(i for i, x in enumerate(steps) if 'test-settle-original-tags.py' in x), steps.index(holders[0]))
        self.assertIn('GITHUB_TOKEN: ${{ github.token }}', holders[0])


if __name__=='__main__':unittest.main()
