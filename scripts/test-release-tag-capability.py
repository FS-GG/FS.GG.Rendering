#!/usr/bin/env python3
"""Bounded causal controls for the read-only Rendering tag capability observer.

Everything is local: fake transports and a locally generated throwaway RSA key. No
network request, credential, workflow, ref, package or settlement effect is made.
Each negative control flips exactly one input of a passing baseline.
"""
import copy
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import urllib.error

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent.parent


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(file))
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


c = load('capability', 'release-tag-capability.py')
settle = load('settle', 'settle-original-tags.py')
SHA = 'a' * 40
APP_ID = 424242
JOB_TOKEN = 'ghs_' + 'J' * 36
PRIVATE_KEY = '-----BEGIN PRIVATE KEY-----\n' + 'K' * 120 + '\n-----END PRIVATE KEY-----\n'
JWT = 'eyJ.' + 'P' * 40 + '.' + 'S' * 40
ENV = {'FSGG_EXPECTED_EXECUTOR_SHA': SHA, 'GITHUB_SHA': SHA, 'GITHUB_WORKFLOW_SHA': SHA,
       'GITHUB_WORKFLOW_REF': c.WORKFLOW, 'GITHUB_REF': 'refs/heads/main',
       'GITHUB_REPOSITORY': c.REPO, 'GITHUB_EVENT_NAME': 'workflow_dispatch',
       'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_RUN_ID': '77', 'GITHUB_TOKEN': JOB_TOKEN,
       'FSGG_DISPATCH_APP_ID': str(APP_ID), 'FSGG_DISPATCH_APP_PRIVATE_KEY': PRIVATE_KEY}
WORKFLOWS = {'total_count': 3, 'workflows': [
    {'id': 310031035, 'path': '.github/workflows/release-tags.yml'},
    {'id': 295951545, 'path': '.github/workflows/release.yml'},
    {'id': 303545957, 'path': '.github/workflows/template-dispatch.yml'}]}
RULESET = {'id': 1, 'target': 'tag', 'enforcement': 'active', 'bypass_actors': [],
           'conditions': {'ref_name': {'include': list(c.REFS), 'exclude': []}},
           'rules': [{'type': 'update'}, {'type': 'deletion'}]}
LISTING = [{'id': 1, '_links': {'self': {'href': 'https://api.github.com' + c.PREFIX + '/rulesets/1'}}}]
GRANTS = {'contents': 'write', 'workflows': 'write', 'metadata': 'read'}
APP = {'id': APP_ID, 'slug': c.APP_SLUG, 'owner': {'login': 'FS-GG', 'type': 'Organization'},
       'permissions': dict(GRANTS), 'events': ['workflow_run']}
INSTALLATION = {'id': 9001, 'app_id': APP_ID, 'app_slug': c.APP_SLUG, 'target_id': 55, 'target_type': 'Organization',
                'account': {'login': 'FS-GG', 'type': 'Organization', 'id': 55}, 'suspended_at': None,
                'repository_selection': 'selected', 'permissions': dict(GRANTS), 'events': ['workflow_run']}
REPO = {'id': c.REPO_ID, 'full_name': c.REPO, 'owner': {'login': 'FS-GG'}, 'default_branch': 'main', 'archived': False}


class Seq:
    """Successive responses for one route; the last repeats."""
    def __init__(self, *items):
        self.items = list(items)


class Response:
    def __init__(self, body=b'{}', code=200, link=None):
        self.body, self.code = body, code
        self.headers = {'x-github-request-id': 'REQ', **({'Link': link} if link else {})}
    def __enter__(self): return self
    def __exit__(self, *a): return False
    def read(self, n): return self.body[:n]


class Fake:
    """Transport factory returning the production GET adapter over a fake socket.

    The real route allowlist, request bound, byte bound and closed-reason handling therefore
    apply to every observer path exercised below."""
    def __init__(self, **override):
        self.calls = []
        self.data = {
            c.PREFIX: REPO,
            c.PREFIX + '/git/ref/heads/main': {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': SHA}},
            c.PREFIX + '/branches/main': {'name': 'main', 'protected': True, 'commit': {'sha': SHA}},
            c.PREFIX + '/actions/workflows?per_page=100': WORKFLOWS,
            c.PREFIX + '/rulesets?includes_parents=true&per_page=100': LISTING,
            c.PREFIX + '/rulesets/1': RULESET,
            '/app': APP, c.PREFIX + '/installation': INSTALLATION}
        self.data = copy.deepcopy(self.data); self.data.update(copy.deepcopy(override))

    def __call__(self, token, role, budget, events):
        adapter = c.GET(token, role, budget, events)
        def opener(request, timeout):
            self.calls.append((role, request.get_method(), request.full_url.removeprefix('https://api.github.com')))
            value = self.data[self.calls[-1][2]]
            if isinstance(value, Seq):
                value = value.items.pop(0) if len(value.items) > 1 else value.items[0]
            if isinstance(value, BaseException): raise value
            return Response(json.dumps(value).encode())
        adapter.opener = type('O', (), {'open': staticmethod(opener)})()
        return adapter


def signer(app_id, key, budget, cleanup):
    cleanup['temporaryKey'] = 'removed'
    return JWT


def observe(fake=None, env=None, sign=signer, guard_only=False):
    fake = fake or Fake()
    with tempfile.TemporaryDirectory() as d:
        observer = c.Observer(Path(d), dict(env or ENV), transport=fake, signer=sign)
        # Local checkout identity is controlled separately (CheckoutDrift); source bytes are irrelevant here.
        with patch.object(c, 'local_git', side_effect=lambda r, *a: SHA if a[:1] == ('rev-parse',) else ''), \
                patch.object(Path, 'read_bytes', lambda self: b'source'):
            observer.run(guard_only)
    return observer, fake


def reasons(observer):
    return [(g['stage'], g['reason']) for g in observer.report['gaps']]


class RealHttpAdapter(unittest.TestCase):
    """The production GET adapter itself, with only the socket replaced."""
    def adapter(self, role='job', response=None, error=None):
        budget = c.Budget(); events = []
        adapter = c.GET(JOB_TOKEN, role, budget, events)
        seen = []
        def opener(request, timeout):
            seen.append((request.get_method(), request.full_url, dict(request.header_items())))
            if error: raise error
            return response or Response()
        adapter.opener = type('O', (), {'open': staticmethod(opener)})()
        return adapter, seen, budget, events

    def test_only_get_to_the_closed_host_with_closed_routes(self):
        adapter, seen, _, _ = self.adapter()
        adapter.get(c.PREFIX)
        self.assertEqual([('GET', 'https://api.github.com' + c.PREFIX)], [(m, u) for m, u, _ in seen])

    def test_no_installation_token_route_or_mutation_route_is_reachable(self):
        for role, path in (('app', '/app/installations/9001/access_tokens'), ('app', c.PREFIX + '/actions/workflows/1/disable'),
                           ('job', c.PREFIX + '/git/refs'), ('job', c.PREFIX + '/actions/workflows/1/disable'),
                           ('job', '/app'), ('app', c.PREFIX), ('job', c.PREFIX + '/rulesets/0'), ('job', c.PREFIX + '/rulesets/1/../x'),
                           ('job', c.PREFIX + '/git/ref/tags/v0.32.3')):
            adapter, seen, budget, _ = self.adapter(role)
            with self.assertRaises(c.Refused) as caught:
                adapter.get(path)
            self.assertEqual('route-refused', caught.exception.args[0], (role, path))
            self.assertEqual([], seen); self.assertEqual(0, budget.requests)

    def test_source_has_no_mutation_verb_or_token_mint_route(self):
        source = (ROOT / 'scripts/release-tag-capability.py').read_text()
        self.assertNotIn('access_tokens', source)
        self.assertNotRegex(source, r"method\s*=\s*'(?!GET')")
        self.assertEqual(['GET'], sorted(set(re.findall(r"method='([A-Z]+)'", source))))

    def test_redirect_pagination_status_and_size_refuse(self):
        for kwargs, reason in (({'response': Response(link='<x>; rel="next"')}, 'pagination-continuation-or-unknown'),
                               ({'response': Response(code=403)}, 'http-403'),
                               ({'response': Response(body=b'x' * (256 * 1024 + 1))}, 'response-byte-bound'),
                               ({'response': Response(body=b'{"a":1,"a":2}')}, 'duplicate-json-key'),
                               ({'response': Response(body=b'7')}, 'response-shape'),
                               ({'error': TimeoutError('boom ' + JOB_TOKEN)}, 'transport-or-json-unavailable')):
            adapter, *_ = self.adapter(**kwargs)
            with self.assertRaises(c.Refused) as caught:
                adapter.get(c.PREFIX)
            self.assertEqual(reason, caught.exception.args[0])
            self.assertNotIn(JOB_TOKEN, str(caught.exception))
        with self.assertRaises(c.Refused):
            c.NoRedirect().redirect_request(None, None, 302, 'Found', {}, 'https://elsewhere')

    def test_request_count_is_bounded(self):
        adapter, _, budget, _ = self.adapter()
        budget.requests = 12
        with self.assertRaises(c.Refused) as caught:
            adapter.get(c.PREFIX)
        self.assertEqual('request-count-bound', caught.exception.args[0])

    def test_worst_case_complete_observation_fits_request_bound(self):
        # Three rulesets, the maximum, plus every protected-source/App read, must stay within twelve.
        listing = [{'id': i, '_links': {'self': {'href': 'https://api.github.com' + c.PREFIX + '/rulesets/' + str(i)}}} for i in (1, 2, 3)]
        data = {c.PREFIX + '/rulesets?includes_parents=true&per_page=100': listing}
        data.update({c.PREFIX + '/rulesets/' + str(i): dict(RULESET, id=i) for i in (1, 2, 3)})
        observer, fake = observe(Fake(**data))
        self.assertEqual([], reasons(observer))
        self.assertEqual(12, observer.report['requestCount'])


class OrderingAndSecrets(unittest.TestCase):
    def test_complete_baseline_observes_without_authority_claims(self):
        observer, fake = observe()
        report = observer.report
        self.assertEqual([], reasons(observer)); self.assertEqual('observed', report['result'])
        self.assertTrue(report['writerPermissionCeilingCompatible'] and report['immutablePolicyReady'])
        self.assertTrue(report['determiningRereadComplete'])
        for key in ('installationTokenMinted', 'workflowMutation', 'tagMutation', 'publication', 'settlementEligible', 'selectedRepositoriesComplete'):
            self.assertIs(False, report[key], key)
        self.assertEqual(['job'] * 6 + ['app', 'app', 'job', 'app'], [role for role, _, _ in fake.calls])

    def test_protected_source_failures_precede_any_app_secret_use(self):
        for name, override, env in (
                ('moved main', {c.PREFIX + '/git/ref/heads/main': {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': 'b' * 40}}}, {}),
                ('unprotected', {c.PREFIX + '/branches/main': {'name': 'main', 'protected': False, 'commit': {'sha': SHA}}}, {}),
                ('wrong repository id', {c.PREFIX: dict(REPO, id=1)}, {}),
                ('archived', {c.PREFIX: dict(REPO, archived=True)}, {}),
                ('wrong workflow ref', {}, {'GITHUB_WORKFLOW_REF': 'FS-GG/FS.GG.Rendering/.github/workflows/release-tag-capability.yml@refs/heads/other'}),
                ('rerun', {}, {'GITHUB_RUN_ATTEMPT': '2'}),
                ('non-main ref', {}, {'GITHUB_REF': 'refs/heads/feature'}),
                ('sha not expected', {}, {'GITHUB_SHA': 'c' * 40}),
                ('malformed expected', {}, {'FSGG_EXPECTED_EXECUTOR_SHA': 'A' * 40})):
            called = []
            env_now = dict(ENV, **env)
            observer, fake = observe(Fake(**override), env_now, sign=lambda *a: called.append(a))
            self.assertEqual('refused-or-incomplete', observer.report['result'], name)
            self.assertEqual([], called, name)
            self.assertEqual([], [x for x in fake.calls if x[0] == 'app'], name)
            self.assertNotIn('app', observer.report, name)
            self.assertIn('FSGG_DISPATCH_APP_PRIVATE_KEY', env_now, name)  # never popped before the guard passed

    def test_checkout_drift_refuses_before_secrets(self):
        with tempfile.TemporaryDirectory() as d:
            observer = c.Observer(Path(d), dict(ENV), transport=Fake(), signer=lambda *a: self.fail('signed'))
            with patch.object(c, 'local_git', side_effect=c.Refused('checkout-unavailable-or-dirty')):
                observer.run()
            self.assertEqual([('observer', 'checkout-unavailable-or-dirty')], reasons(observer))

    def test_guard_only_never_touches_app_secrets(self):
        env = dict(ENV)
        observer, fake = observe(env=env, sign=lambda *a: self.fail('signed'), guard_only=True)
        self.assertEqual([], reasons(observer))
        self.assertEqual({'job'}, {role for role, _, _ in fake.calls})
        self.assertEqual(3, len(fake.calls))
        self.assertIn('FSGG_DISPATCH_APP_PRIVATE_KEY', env)

    def test_missing_or_malformed_app_credentials_stop_without_signing(self):
        for key, value in (('FSGG_DISPATCH_APP_ID', ''), ('FSGG_DISPATCH_APP_ID', '0'), ('FSGG_DISPATCH_APP_ID', '12ab')):
            observer, fake = observe(env=dict(ENV, **{key: value}), sign=lambda *a: self.fail('signed'))
            self.assertIn(('observer', 'app-id-unavailable'), reasons(observer))
            self.assertEqual([], [x for x in fake.calls if x[0] == 'app'])

    def test_provider_error_text_and_secrets_never_enter_the_report(self):
        for failure in (urllib.error.URLError(JOB_TOKEN + PRIVATE_KEY + JWT), OSError(JWT), ValueError(PRIVATE_KEY)):
            observer, _ = observe(Fake(**{'/app': failure}))
            self.assertEqual([('observer', 'transport-or-json-unavailable')], reasons(observer))
            raw = json.dumps(observer.report)
            for secret in (JOB_TOKEN, PRIVATE_KEY.strip(), JWT, 'K' * 20):
                self.assertNotIn(secret, raw)
        # A signer failure is likewise reduced to its closed reason.
        def failing(*a): raise c.Refused('signing-unavailable')
        observer, _ = observe(sign=failing)
        self.assertEqual([('observer', 'signing-unavailable')], reasons(observer))
        self.assertNotIn('app', observer.report)

    def test_retention_refuses_secret_canaries_and_overwrites(self):
        for canary in (JOB_TOKEN, PRIVATE_KEY.strip(), JWT, str(APP_ID)):
            observer, _ = observe()
            observer.report['note'] = 'leak ' + canary
            with tempfile.TemporaryDirectory() as d, self.assertRaises(c.Refused) as caught:
                observer.root = Path(d); observer.retain()
            self.assertEqual('report-secret-canary', caught.exception.args[0]); self.assertFalse((Path(d) / c.REPORT).exists())
        observer, _ = observe()
        with tempfile.TemporaryDirectory() as d:
            observer.root = Path(d); observer.retain()
            self.assertEqual('fsgg.rendering.tag-capability/v1', json.loads((Path(d) / c.REPORT).read_text())['schema'])
            with self.assertRaises(FileExistsError):
                observer.retain()

    def test_multiline_secret_is_caught_after_json_escaping_even_with_short_lines(self):
        observer, _ = observe()
        observer.secrets.append('ab\ncd\nef\n')  # no line reaches the per-line canary length
        observer.report['note'] = 'leak ab\ncd\nef'
        with tempfile.TemporaryDirectory() as d, self.assertRaises(c.Refused) as caught:
            observer.root = Path(d); observer.retain()
        self.assertEqual('report-secret-canary', caught.exception.args[0])

    def test_main_exit_status_and_stderr_never_carry_credentials(self):
        class Capture:
            def __init__(self): self.text = ''
            def write(self, x): self.text += x
            def flush(self): pass

        def run_main(fake, retain=None):
            real = c.Observer
            factory = lambda root, env: real(root, dict(ENV), transport=fake, signer=signer)
            with tempfile.TemporaryDirectory() as d, patch.object(c, 'Observer', factory), patch.object(sys, 'argv', ['x']), \
                    patch.object(c.Path, 'cwd', return_value=Path(d)), patch.object(c, 'local_git', side_effect=lambda r, *a: SHA if a[:1] == ('rev-parse',) else ''), \
                    patch.object(Path, 'read_bytes', lambda self: b'source'), patch.object(sys, 'stderr', Capture()) as err:
                if retain: 
                    with patch.object(real, 'retain', side_effect=retain):
                        return c.main(), err.text
                return c.main(), err.text
        self.assertEqual(0, run_main(Fake())[0])
        self.assertEqual(1, run_main(Fake(**{c.PREFIX + '/branches/main': {'name': 'main', 'protected': False, 'commit': {'sha': SHA}}}))[0])
        status, text = run_main(Fake(), retain=c.Refused('report-secret-canary'))
        self.assertEqual(1, status)
        self.assertNotIn(JOB_TOKEN, text); self.assertIn('sanitized report unavailable', text)


class AppAndInstallationIdentity(unittest.TestCase):
    def refused(self, **override):
        observer, _ = observe(Fake(**override))
        self.assertEqual('refused-or-incomplete', observer.report['result'])
        return reasons(observer)

    def test_wrong_app_or_installation_identity_refuses(self):
        self.assertEqual([('observer', 'app-identity')], self.refused(**{'/app': dict(APP, slug='other')}))
        self.assertEqual([('observer', 'app-identity')], self.refused(**{'/app': dict(APP, id=1)}))
        self.assertEqual([('observer', 'app-identity')], self.refused(**{'/app': dict(APP, owner={'login': 'someone', 'type': 'Organization'})}))
        for field, value in (('app_id', 1), ('app_slug', 'other'), ('suspended_at', '2026-01-01T00:00:00Z'),
                             ('target_type', 'User'), ('target_id', 999), ('repository_selection', 'none')):
            self.assertEqual([('observer', 'installation-identity-or-state')],
                             self.refused(**{c.PREFIX + '/installation': dict(INSTALLATION, **{field: value})}), field)
        missing = dict(INSTALLATION); del missing['suspended_at']
        self.assertEqual([('observer', 'installation-identity-or-state')], self.refused(**{c.PREFIX + '/installation': missing}))

    def test_installation_that_moves_between_rereads_refuses(self):
        moved = dict(INSTALLATION, permissions={'contents': 'read'})
        observer, fake = observe(Fake(**{c.PREFIX + '/installation': Seq(INSTALLATION, moved)}))
        self.assertIn(('observer', 'installation-moved'), reasons(observer))
        self.assertNotIn('determiningRereadComplete', observer.report)
        self.assertEqual(2, sum(1 for x in fake.calls if x[2] == c.PREFIX + '/installation'))
        stable, _ = observe(Fake(**{c.PREFIX + '/installation': Seq(INSTALLATION, copy.deepcopy(INSTALLATION))}))
        self.assertEqual([], reasons(stable))

    def test_main_moving_before_final_reread_refuses(self):
        old = {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': SHA}}
        new = {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': 'd' * 40}}
        observer, _ = observe(Fake(**{c.PREFIX + '/git/ref/heads/main': Seq(old, new)}))
        self.assertIn(('observer', 'main-moved-or-unknown'), reasons(observer))
        self.assertNotIn('determiningRereadComplete', observer.report)

    def test_permission_ceiling_is_observation_only_and_each_axis_matters(self):
        for who in ('/app', c.PREFIX + '/installation'):
            for grants in ({'contents': 'write'}, {'workflows': 'write'}, {'contents': 'read', 'workflows': 'write'},
                           {'contents': 'write', 'workflows': 'read'}):
                base = APP if who == '/app' else INSTALLATION
                observer, _ = observe(Fake(**{who: dict(base, permissions=grants)}))
                self.assertEqual('observed', observer.report['result'], (who, grants))
                self.assertIs(False, observer.report['writerPermissionCeilingCompatible'], (who, grants))
                self.assertIs(False, observer.report['settlementEligible'])

    def test_malformed_permissions_or_events_refuse(self):
        for bad in ({'contents': 'owner'}, {'Contents': 'write'}, [], 'write'):
            self.assertEqual([('observer', 'permission-shape')], self.refused(**{'/app': dict(APP, permissions=bad)}))
        for bad in (['a', 'a'], ['A'], 'x', [1]):
            self.assertEqual([('observer', 'event-shape')], self.refused(**{'/app': dict(APP, events=bad)}))


class PolicyAndInventory(unittest.TestCase):
    def policy(self, detail, listing=None):
        data = {c.PREFIX + '/rulesets/1': detail}
        if listing is not None: data[c.PREFIX + '/rulesets?includes_parents=true&per_page=100'] = listing
        observer, _ = observe(Fake(**data))
        return observer

    def test_each_single_weakening_denies_immutable_policy_readiness(self):
        weakened = {
            'bypass actor present': dict(RULESET, bypass_actors=[{'actor_id': 1, 'actor_type': 'Integration', 'bypass_mode': 'always'}]),
            'bypass field absent (insufficient visibility)': {k: v for k, v in RULESET.items() if k != 'bypass_actors'},
            'deletion rule missing': dict(RULESET, rules=[{'type': 'update'}]),
            'update rule missing': dict(RULESET, rules=[{'type': 'deletion'}]),
            'exclude non-empty': dict(RULESET, conditions={'ref_name': {'include': list(c.REFS), 'exclude': ['refs/tags/x']}}),
            'one ref uncovered': dict(RULESET, conditions={'ref_name': {'include': list(c.REFS[:2]), 'exclude': []}}),
            'glob instead of exact refs': dict(RULESET, conditions={'ref_name': {'include': ['refs/tags/fs-gg-ui/*'], 'exclude': []}}),
            'evaluate only': dict(RULESET, enforcement='evaluate'),
            'disabled': dict(RULESET, enforcement='disabled'),
            'branch target': dict(RULESET, target='branch'),
        }
        self.assertTrue(self.policy(RULESET).report['immutablePolicyReady'])
        for name, detail in weakened.items():
            observer = self.policy(detail)
            self.assertIs(False, observer.report['immutablePolicyReady'], name)
            self.assertEqual('observed', observer.report['result'], name)  # a fact, not a refusal

    def test_coverage_can_be_split_across_rulesets_but_not_partial(self):
        listing = [{'id': i, '_links': {'self': {'href': 'https://api.github.com' + c.PREFIX + '/rulesets/' + str(i)}}} for i in (1, 2)]
        parts = {c.PREFIX + '/rulesets?includes_parents=true&per_page=100': listing,
                 c.PREFIX + '/rulesets/1': dict(RULESET, id=1, conditions={'ref_name': {'include': list(c.REFS[:2]), 'exclude': []}}),
                 c.PREFIX + '/rulesets/2': dict(RULESET, id=2, conditions={'ref_name': {'include': list(c.REFS[2:]), 'exclude': []}})}
        self.assertTrue(observe(Fake(**parts))[0].report['immutablePolicyReady'])
        parts[c.PREFIX + '/rulesets/2'] = dict(parts[c.PREFIX + '/rulesets/2'], rules=[{'type': 'update'}])
        self.assertFalse(observe(Fake(**parts))[0].report['immutablePolicyReady'])

    def test_inherited_unbounded_or_malformed_policy_is_a_gap_not_readiness(self):
        inherited = [{'id': 1, '_links': {'self': {'href': 'https://api.github.com/orgs/FS-GG/rulesets/1'}}}]
        observer = self.policy(RULESET, inherited)
        self.assertEqual([('policy', 'inherited-policy-detail-unsupported')], reasons(observer))
        self.assertFalse(observer.report['immutablePolicyReady'])
        self.assertEqual([('policy', 'policy-cardinality-bound')], reasons(self.policy(RULESET, LISTING * 4)))
        self.assertEqual([('policy', 'policy-detail-identity')], reasons(self.policy(dict(RULESET, id=2))))
        self.assertEqual([('policy', 'policy-bypass-shape')], reasons(self.policy(dict(RULESET, bypass_actors='none'))))
        self.assertEqual([('policy', 'policy-rule-shape')], reasons(self.policy(dict(RULESET, rules=[{'type': 'Update'}]))))
        # A policy gap does not stop the independent App observations.
        self.assertIn('app', observer.report)

    def test_inventory_gaps_do_not_masquerade_as_complete(self):
        short = dict(WORKFLOWS, total_count=4)
        observer, _ = observe(Fake(**{c.PREFIX + '/actions/workflows?per_page=100': short}))
        self.assertEqual([('workflowInventory', 'workflow-census-incomplete')], reasons(observer))
        self.assertNotIn('workflowInventory', observer.report)
        dup = {'total_count': 2, 'workflows': [WORKFLOWS['workflows'][0], dict(WORKFLOWS['workflows'][0])]}
        observer, _ = observe(Fake(**{c.PREFIX + '/actions/workflows?per_page=100': dup}))
        self.assertEqual([('workflowInventory', 'workflow-duplicate')], reasons(observer))
        odd = {'total_count': 1, 'workflows': [{'id': 1, 'path': 'x/../.github/workflows/a.yml'}]}
        observer, _ = observe(Fake(**{c.PREFIX + '/actions/workflows?per_page=100': odd}))
        self.assertEqual([('workflowInventory', 'workflow-identity')], reasons(observer))

    def test_observed_digests_equal_the_digests_settlement_will_require(self):
        # The observation exists to feed root's admission bytes; a digest the settlement entry
        # would then reject is a defect. Join both implementations on identical inputs.
        report = observe()[0].report
        pairs = sorted((w['id'], w['path']) for w in WORKFLOWS['workflows'])
        self.assertEqual(settle.digest(json.dumps(pairs, separators=(',', ':')).encode()), report['workflowInventory']['sha256'])

        class Api:
            def get(self, path):
                return LISTING if path.startswith('rulesets?') else RULESET
        settle.immutable_policy(Api(), report['policy']['sha256'])
        with self.assertRaises(settle.Refused):
            settle.immutable_policy(Api(), '0' * 64)
        weak = dict(RULESET, bypass_actors=[{'x': 1}])
        weak_report = observe(Fake(**{c.PREFIX + '/rulesets/1': weak}))[0].report
        self.assertFalse(weak_report['immutablePolicyReady'])

        class WeakApi:
            def get(self, path):
                return LISTING if path.startswith('rulesets?') else weak
        with self.assertRaises(settle.Refused):  # observer and settlement agree on the negative too
            settle.immutable_policy(WeakApi(), weak_report['policy']['sha256'])

    def test_observer_refs_and_workflow_identity_equal_settlement(self):
        self.assertEqual(settle.REFS, c.REFS)
        self.assertEqual(settle.REPO, c.REPO)
        self.assertEqual(settle.PREFIX.rstrip('/'), c.PREFIX)


@unittest.skipUnless(shutil.which('openssl') and Path('/usr/bin/openssl').exists(), 'openssl required')
class JwtSigning(unittest.TestCase):
    def test_rs256_jwt_verifies_and_temporary_key_is_removed(self):
        with tempfile.TemporaryDirectory() as d:
            key, pub = Path(d, 'k.pem'), Path(d, 'p.pem')
            subprocess.run(['openssl', 'genrsa', '-out', str(key), '2048'], check=True, capture_output=True)
            subprocess.run(['openssl', 'rsa', '-in', str(key), '-pubout', '-out', str(pub)], check=True, capture_output=True)
            cleanup = {}
            with tempfile.TemporaryDirectory() as scratch, patch.object(c.tempfile, 'tempdir', scratch):
                token = c.app_jwt(7, key.read_text(), c.Budget(), cleanup)
                self.assertEqual([], list(Path(scratch).iterdir()))
            self.assertEqual('removed', cleanup['temporaryKey'])
            header, payload, signature = token.split('.')
            pad = lambda s: s + '=' * (-len(s) % 4)
            import base64
            claims = json.loads(base64.urlsafe_b64decode(pad(payload)))
            self.assertEqual('7', claims['iss']); self.assertLessEqual(claims['exp'] - claims['iat'], 540)
            message, sig = Path(d, 'm'), Path(d, 's')
            message.write_bytes((header + '.' + payload).encode()); sig.write_bytes(base64.urlsafe_b64decode(pad(signature)))
            result = subprocess.run(['openssl', 'dgst', '-sha256', '-verify', str(pub), '-signature', str(sig), str(message)], capture_output=True)
            self.assertEqual(0, result.returncode, result.stderr)
            # Causal control: a different message does not verify, so the check above is not vacuous.
            message.write_bytes(b'tampered')
            self.assertNotEqual(0, subprocess.run(['openssl', 'dgst', '-sha256', '-verify', str(pub), '-signature', str(sig), str(message)], capture_output=True).returncode)

    def test_bad_key_refuses_with_closed_reason_and_leaves_no_key_behind(self):
        with tempfile.TemporaryDirectory() as scratch, patch.object(c.tempfile, 'tempdir', scratch):
            cleanup = {}
            with self.assertRaises(c.Refused) as caught:
                c.app_jwt(7, PRIVATE_KEY, c.Budget(), cleanup)
            self.assertEqual('signing-unavailable', caught.exception.args[0])
            self.assertNotIn('K' * 20, str(caught.exception))
            self.assertEqual([], list(Path(scratch).iterdir()))
            self.assertEqual('removed', cleanup['temporaryKey'])
        for bad in ('', 'short', 'x' * 200):
            with self.assertRaises(c.Refused):
                c.app_jwt(7, bad, c.Budget(), {})


class WorkflowSource(unittest.TestCase):
    text = (ROOT / '.github/workflows/release-tag-capability.yml').read_text()
    gate = (ROOT / '.github/workflows/gate.yml').read_text()

    def test_manual_main_only_first_attempt_exact_sha_and_least_privilege(self):
        t = self.text
        self.assertRegex(t, r'(?m)^on:\n  workflow_dispatch:\n    inputs:\n      expected-executor-sha:')
        self.assertEqual(1, len(re.findall(r'(?m)^on:', t)))
        self.assertNotRegex(t, r'(?m)^  (push|pull_request|pull_request_target|schedule|workflow_run|repository_dispatch):')
        self.assertRegex(t, r'(?m)^permissions:\n  contents: read\n  actions: read\n(?!  )')
        for needle in ("github.ref == 'refs/heads/main'", 'github.run_attempt == 1', 'github.sha == inputs.expected-executor-sha',
                       "github.repository == 'FS-GG/FS.GG.Rendering'", 'persist-credentials: false'):
            self.assertIn(needle, t)
        self.assertNotRegex(t, r'(?i)(write|admin)\b(?!.*description)')
        self.assertNotIn('settle', t.replace('settlement', ''))
        self.assertEqual('release-tag-capability', re.search(r'group: (\S+)', t).group(1))  # does not take the release-tags lock

    def test_app_secrets_are_scoped_to_exactly_one_step_after_guards(self):
        steps = re.split(r'(?m)^      - ', self.text)[1:]
        holders = [s for s in steps if 'secrets.FSGG_DISPATCH_APP' in s]
        self.assertEqual(1, len(holders))
        self.assertEqual(2, self.text.count('secrets.FSGG_DISPATCH_APP'))
        index = steps.index(holders[0])
        self.assertIn('test-release-tag-capability.py', steps[1])
        self.assertIn('--guard-only', steps[2])
        self.assertLess(1, index)
        self.assertLess(2, index)
        self.assertNotIn('secrets.', self.text.split('Observe existing dispatcher')[0])
        self.assertNotIn('secrets.GITHUB_TOKEN', self.text)
        self.assertNotIn('workflows: write', self.text)
        self.assertIn('if: always()', steps[-1]); self.assertIn('artifacts/tag-capability/report.json', steps[-1])

    def test_step_timeouts_fit_inside_the_job_and_observer_budget(self):
        self.assertIn('timeout-minutes: 7', self.text)
        self.assertEqual(['60s', '60s', '190s'], re.findall(r'timeout --kill-after=5s (\d+s)', self.text))
        self.assertLess(180, 190)  # the observer's own deadline expires before its outer timeout

    def test_gate_runs_these_controls_before_the_settlement_controls_without_credentials(self):
        a = self.gate.index('scripts/test-release-tag-capability.py')
        b = self.gate.index('scripts/test-settle-original-tags.py')
        self.assertLess(a, b)
        block = self.gate[self.gate.rindex('- name:', 0, a):a]
        self.assertNotIn('secrets.', block)

    def test_source_paths_are_all_present_and_include_this_control(self):
        for path in c.SOURCE_PATHS:
            self.assertTrue((ROOT / path).is_file(), path)
        self.assertIn('scripts/test-release-tag-capability.py', c.SOURCE_PATHS)

    def test_workflow_is_in_the_reviewed_workflow_census_and_adds_no_tag_trigger(self):
        pins = json.loads((ROOT / 'eng/release/font-disposal-0.32.3-workflow-fences.json').read_text())
        path = '.github/workflows/release-tag-capability.yml'
        self.assertEqual(c.digest((ROOT / path).read_bytes()), pins['executor'][path])
        self.assertNotIn(path, pins['producer'])  # the pinned producer never contained it
        self.assertEqual(c.digest((ROOT / '.github/workflows/gate.yml').read_bytes()), pins['executor']['.github/workflows/gate.yml'])


if __name__ == '__main__':
    unittest.main()
