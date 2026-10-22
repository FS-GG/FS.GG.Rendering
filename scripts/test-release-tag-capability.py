#!/usr/bin/env python3
"""Bounded causal controls for the read-only Rendering tag capability observer.

Everything is local: fake transports and a locally generated throwaway RSA key. No
network request, credential, workflow, ref, package or settlement effect is made.
Each negative control flips exactly one input of a passing baseline.
"""
import copy
import datetime
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
import http.client
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
READER_TOKEN = 'ghs_' + 'R' * 36
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


def reader_grant(**changes):
    value = {'token': READER_TOKEN, 'permissions': dict(settle.READER_PERMISSIONS), 'repository_selection': 'selected',
             'repositories': [{'id': c.REPO_ID, 'name': 'FS.GG.Rendering', 'full_name': c.REPO}],
             'expires_at': (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(hours=1)).isoformat().replace('+00:00', 'Z')}
    value.update(changes)
    return value


class ReaderFake:
    """Settlement's real API adapter (real route allowlist, bounds and revocation) over a fake socket.

    The reader sees the job fixture's ruleset bodies unless `data` overrides a route, so a test can make
    the reader identity see a different body. Every request is recorded with the bearer used."""
    def __init__(self, fake, grant=None, mint_code=201, mint_error=None, revoke_code=204, revoke_error=None, data=None, on_get=None):
        self.fake, self.calls, self.grant = fake, [], grant if grant is not None else reader_grant()
        self.mint_code, self.mint_error, self.revoke_code, self.revoke_error = mint_code, mint_error, revoke_code, revoke_error
        self.data, self.on_get, self.masked, self.order = data or {}, on_get, [], []
        self.mask_error = None

    def mask(self, token):
        self.masked.append(token); self.order.append('mask')
        if self.mask_error: raise self.mask_error

    def factory(self, token, role, journal, deadline):
        api = settle.API(token, role, journal, deadline)
        def opener(request, timeout):
            method, path = request.get_method(), request.full_url.removeprefix('https://api.github.com')
            body = json.loads(request.data) if request.data else None
            self.calls.append((role, method, path, body, token)); self.order.append((role, method, path))
            if role == 'app' and method == 'POST' and path == '/app/installations/9001/access_tokens':
                if self.mint_error: raise self.mint_error
                return Response(json.dumps(self.grant).encode(), self.mint_code)
            if role == 'reader' and (method, path) == ('DELETE', '/installation/token'):
                if self.revoke_error: raise self.revoke_error
                return Response(b'', self.revoke_code)
            if role == 'reader' and method == 'GET':
                if self.on_get: self.on_get(path)
                value = self.data[path] if path in self.data else self.fake.data[path]
                if isinstance(value, BaseException): raise value
                return Response(json.dumps(value).encode())
            raise AssertionError(('unexpected request', role, method, path))
        api.opener = type('O', (), {'open': staticmethod(opener)})()
        return api


def new_observer(root, fake, env=None, sign=signer, reader=None):
    reader = reader or ReaderFake(fake)
    observer = c.Observer(root, dict(env or ENV), transport=fake, signer=sign, settlement=settle,
                          api_factory=reader.factory, mask=reader.mask)
    observer.reader_fake = reader
    return observer


def observe(fake=None, env=None, sign=signer, guard_only=False, reader=None):
    fake = fake or Fake()
    with tempfile.TemporaryDirectory() as d:
        observer = new_observer(Path(d), fake, env, sign, reader and reader(fake) if callable(reader) else reader)
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
        # The observer's own adapter still has only GET; the single mint and revocation exist only through
        # settlement's shared mint_token/retire code, with the mint path named once in this source.
        self.assertNotRegex(source, r"method\s*=\s*'(?!GET')")
        self.assertEqual(['GET'], sorted(set(re.findall(r"method='([A-Z]+)'", source))))
        self.assertEqual(1, source.count('access_tokens'))
        for verb in ("'PUT'", "'PATCH'", "'DELETE'", 'installation/token'):
            self.assertNotIn(verb, source)

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
        budget.requests = c.MAX_REQUESTS
        with self.assertRaises(c.Refused) as caught:
            adapter.get(c.PREFIX)
        self.assertEqual('request-count-bound', caught.exception.args[0])

    def test_worst_case_complete_observation_fits_request_bound(self):
        # Three rulesets, the maximum, plus every protected-source/App read, must stay within the bound.
        listing = [{'id': i, '_links': {'self': {'href': 'https://api.github.com' + c.PREFIX + '/rulesets/' + str(i)}}} for i in (1, 2, 3)]
        data = {c.PREFIX + '/rulesets?includes_parents=true&per_page=100': listing}
        data.update({c.PREFIX + '/rulesets/' + str(i): dict(RULESET, id=i) for i in (1, 2, 3)})
        observer, fake = observe(Fake(**data))
        self.assertEqual([], reasons(observer))
        # 12 job/App reads + 1 reader mint + listing and three details + 1 revocation.
        self.assertEqual(c.MAX_REQUESTS, observer.report['requestCount'])
        self.assertEqual(18, c.MAX_REQUESTS)


class OrderingAndSecrets(unittest.TestCase):
    def test_complete_baseline_observes_without_authority_claims(self):
        observer, fake = observe()
        report = observer.report
        self.assertEqual([], reasons(observer)); self.assertEqual('observed', report['result'])
        self.assertTrue(report['writerPermissionCeilingCompatible'] and report['immutablePolicyReady'])
        self.assertTrue(report['determiningRereadComplete'])
        for key in ('workflowMutation', 'tagMutation', 'publication', 'settlementEligible', 'selectedRepositoriesComplete'):
            self.assertIs(False, report[key], key)
        # Exactly one reader token was minted, and it is revoked.
        self.assertIs(True, report['installationTokenMinted'])
        self.assertEqual('revoked', report['cleanup']['readerToken'])
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
            self.assertEqual('fsgg.rendering.tag-capability/v2', json.loads((Path(d) / c.REPORT).read_text())['schema'])
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
            def factory(root, env):
                reader = ReaderFake(fake)
                return real(root, dict(ENV), transport=fake, signer=signer, settlement=settle, api_factory=reader.factory, mask=reader.mask)
            with tempfile.TemporaryDirectory() as d, patch.object(c, 'Observer', factory), patch.object(sys, 'argv', ['x']), patch.object(c.signal, 'signal'), \
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
        # The reader identity judges the same census with settlement's own checks and fails closed too.
        self.assertEqual([('policy', 'inherited-policy-detail-unsupported'), ('readerPolicy', 'reader-policy-unavailable')], reasons(observer))
        self.assertFalse(observer.report['immutablePolicyReady'])
        self.assertNotIn('readerPolicySha256', observer.report)
        self.assertEqual('revoked', observer.report['cleanup']['readerToken'])
        self.assertEqual([('policy', 'policy-cardinality-bound'), ('readerPolicy', 'policy-cardinality-bound')], reasons(self.policy(RULESET, LISTING * 4)))
        # Duplicate ids are a job-observer shape check; settlement's detail census (reused verbatim) does not make it.
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
        odd = {'total_count': 1, 'workflows': [{'id': 1, 'path': ''}]}
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


class HardeningFindings(unittest.TestCase):
    """Controls added after independent review of the first candidate."""
    def test_http_protocol_errors_are_closed_reasons_not_tracebacks(self):
        for failure in (http.client.BadStatusLine(JOB_TOKEN), http.client.LineTooLong('x'), http.client.IncompleteRead(b'x')):
            observer, _ = observe(Fake(**{'/app': failure}))
            self.assertEqual([('observer', 'transport-or-json-unavailable')], reasons(observer))
            self.assertNotIn(JOB_TOKEN, json.dumps(observer.report))

    def test_unexpected_exception_in_app_phase_is_a_closed_incomplete_gap(self):
        observer, _ = observe(Fake(**{'/app': RuntimeError(JOB_TOKEN)}))
        self.assertEqual([('observer', 'unavailable')], reasons(observer))
        self.assertEqual('refused-or-incomplete', observer.report['result'])
        self.assertNotIn(JOB_TOKEN, json.dumps(observer.report))

    def test_empty_ruleset_census_is_not_a_join_candidate(self):
        observer, _ = observe(Fake(**{c.PREFIX + '/rulesets?includes_parents=true&per_page=100': []}))
        self.assertIs(False, observer.report['policy']['digestJoinCandidate'])
        self.assertIs(False, observer.report['immutablePolicyReady'])

    def test_unexpected_exception_is_reduced_to_closed_reason_and_stage_isolated(self):
        observer, _ = observe(Fake(**{c.PREFIX + '/rulesets/1': dict(RULESET, conditions=7)}))
        self.assertEqual([('policy', 'unavailable')], reasons(observer))
        self.assertIn('app', observer.report)  # the App observation still ran

    def test_refused_determining_reread_leaves_no_positive_readiness(self):
        moved = dict(INSTALLATION, permissions={'contents': 'read'})
        observer, _ = observe(Fake(**{c.PREFIX + '/installation': Seq(INSTALLATION, moved)}))
        self.assertIs(False, observer.report['writerPermissionCeilingCompatible'])
        self.assertIs(False, observer.report['immutablePolicyReady'])
        old = {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': SHA}}
        new = {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': 'd' * 40}}
        observer, _ = observe(Fake(**{c.PREFIX + '/git/ref/heads/main': Seq(old, new)}))
        self.assertIs(False, observer.report['writerPermissionCeilingCompatible'])
        self.assertIs(False, observer.report['immutablePolicyReady'])
        self.assertIs(True, observe()[0].report['writerPermissionCeilingCompatible'])  # baseline control

    def test_policy_digest_is_labelled_with_the_observing_identity_and_join_visibility(self):
        baseline = observe()[0].report['policy']
        self.assertEqual('job-token', baseline['observedAs']); self.assertIs(True, baseline['digestJoinCandidate'])
        hidden = {k: v for k, v in RULESET.items() if k != 'bypass_actors'}
        self.assertIs(False, observe(Fake(**{c.PREFIX + '/rulesets/1': hidden}))[0].report['policy']['digestJoinCandidate'])

    def test_non_tag_rulesets_and_null_conditions_do_not_abort_policy_observation(self):
        for detail in (dict(RULESET, target='push', conditions=None), dict(RULESET, conditions=None)):
            observer, _ = observe(Fake(**{c.PREFIX + '/rulesets/1': detail}))
            self.assertEqual([], reasons(observer)); self.assertFalse(observer.report['immutablePolicyReady'])

    def test_dynamic_workflow_paths_do_not_block_the_digest_settlement_accepts(self):
        rows = {'total_count': 2, 'workflows': WORKFLOWS['workflows'][:1] + [{'id': 5, 'path': 'dynamic/github-code-scanning/codeql'}]}
        observer, _ = observe(Fake(**{c.PREFIX + '/actions/workflows?per_page=100': rows}))
        self.assertEqual([], reasons(observer)); self.assertEqual(2, observer.report['workflowInventory']['count'])

    def test_request_id_is_kept_only_in_github_identifier_shape(self):
        self.assertEqual('ABCD:12:ef-9', c.request_id('ABCD:12:ef-9'))
        for hostile in ('ghs_' + 'x' * 30, 'a b', '<script>', '', None, 'A' * 65):
            self.assertIsNone(c.request_id(hostile))

    def test_hostile_request_id_header_never_reaches_retained_events(self):
        events = []; adapter = c.GET(JOB_TOKEN, 'job', c.Budget(), events)
        hostile = Response(); hostile.headers['x-github-request-id'] = JOB_TOKEN
        adapter.opener = type('O', (), {'open': staticmethod(lambda request, timeout: hostile)})()
        adapter.get(c.PREFIX)
        self.assertIsNone(events[0]['requestId']); self.assertNotIn(JOB_TOKEN, json.dumps(events))

    def test_authorization_header_per_role_and_redirect_handler_installed(self):
        for role, token, path in (('job', JOB_TOKEN, c.PREFIX), ('app', JWT, '/app')):
            adapter = c.GET(token, role, c.Budget(), [])
            self.assertTrue(any(isinstance(h, c.NoRedirect) for h in adapter.opener.handlers))
            seen = []
            def opener(request, timeout):
                seen.append(dict(request.header_items())); return Response()
            adapter.opener = type('O', (), {'open': staticmethod(opener)})()
            adapter.get(path)
            self.assertEqual('Bearer ' + token, seen[0]['Authorization'], role)
            self.assertEqual('2022-11-28', seen[0]['X-github-api-version'])
        # The App role is signed with the JWT, never the job token.
        observer, fake = observe()
        tokens = {role: set() for role in ('job', 'app')}
        real = Fake.__call__
        def spy(self, token, role, budget, events):
            tokens[role].add(token); return real(self, token, role, budget, events)
        with patch.object(Fake, '__call__', spy):
            observe(fake)
        self.assertEqual({JOB_TOKEN}, tokens['job']); self.assertEqual({JWT}, tokens['app'])

    def test_local_checkout_identity_uses_a_real_git_repository(self):
        env = {'GIT_AUTHOR_NAME': 'a', 'GIT_AUTHOR_EMAIL': 'a@x', 'GIT_COMMITTER_NAME': 'a', 'GIT_COMMITTER_EMAIL': 'a@x', 'PATH': '/usr/bin:/bin'}
        git = lambda root, *a: subprocess.run(['git', '-C', str(root), *a], check=True, capture_output=True, env=env, text=True).stdout.strip()
        with tempfile.TemporaryDirectory() as d:
            root = Path(d)
            git(root, 'init', '-q'); (root / 'f').write_text('1'); git(root, 'add', 'f'); git(root, 'commit', '-qm', 'one')
            head = git(root, 'rev-parse', 'HEAD')
            self.assertEqual(head, c.local_git(root, 'rev-parse', 'HEAD'))
            c.local_git(root, 'diff', '--quiet', 'HEAD', '--', 'f')  # clean passes
            (root / 'f').write_text('2')
            with self.assertRaises(c.Refused) as caught:
                c.local_git(root, 'diff', '--quiet', 'HEAD', '--', 'f')
            self.assertEqual('checkout-unavailable-or-dirty', caught.exception.args[0])
            # protected_source binds the checkout: a different expected SHA or dirty source refuses.
            job = Fake()('x' * 40, 'job', c.Budget(), [])
            with self.assertRaises(c.Refused) as caught:
                c.protected_source(root, dict(ENV, FSGG_EXPECTED_EXECUTOR_SHA=SHA, GITHUB_SHA=SHA, GITHUB_WORKFLOW_SHA=SHA), job)
            self.assertEqual('checkout-sha', caught.exception.args[0])
            pinned = dict(ENV, FSGG_EXPECTED_EXECUTOR_SHA=head, GITHUB_SHA=head, GITHUB_WORKFLOW_SHA=head)
            with patch.object(c, 'SOURCE_PATHS', ('f',)):
                fake = Fake(**{c.PREFIX + '/git/ref/heads/main': {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': head}},
                               c.PREFIX + '/branches/main': {'name': 'main', 'protected': True, 'commit': {'sha': head}}})
                job = fake('x' * 40, 'job', c.Budget(), [])
                with self.assertRaises(c.Refused):  # dirty source file
                    c.protected_source(root, pinned, job)
                (root / 'f').write_text('1')
                self.assertEqual(head, c.protected_source(root, pinned, job)['executorSha'])


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


class ReaderIdentityDigest(unittest.TestCase):
    """One exact-permission reader mint, read through settlement's own code, always revoked."""
    def run_with(self, reader_kwargs=None, fake=None, **observe_kwargs):
        fake = fake or Fake()
        reader = ReaderFake(fake, **(reader_kwargs or {}))
        observer, fake = observe(fake, reader=reader, **observe_kwargs)
        return observer, reader

    def reader_calls(self, reader, token=READER_TOKEN):
        return [(m, p) for _, m, p, _, tok in reader.calls if tok == token]

    def deletes(self, reader):
        return [x for x in reader.calls if x[1] == 'DELETE']

    def assert_revoked_once(self, reader):
        deletes = self.deletes(reader)
        self.assertEqual([('reader', 'DELETE', '/installation/token', None, READER_TOKEN)], deletes)

    # -- baseline and exact request shape ---------------------------------------------------------------
    def test_baseline_mints_exactly_one_exact_reader_token_reads_only_rulesets_and_revokes(self):
        observer, reader = self.run_with()
        report = observer.report
        self.assertEqual([], reasons(observer)); self.assertEqual('observed', report['result'])
        posts = [x for x in reader.calls if x[1] == 'POST']
        self.assertEqual([('app', 'POST', '/app/installations/9001/access_tokens', settle.MINTS['reader'], JWT)], posts)
        self.assertEqual({'repositories': ['FS.GG.Rendering'], 'permissions': {'administration': 'read', 'metadata': 'read'}}, posts[0][3])
        self.assertEqual([('GET', settle.PREFIX + 'rulesets?includes_parents=true&per_page=100'),
                          ('GET', settle.PREFIX + 'rulesets/1'), ('DELETE', '/installation/token')], self.reader_calls(reader))
        self.assert_revoked_once(reader)
        self.assertEqual([JWT], [x[4] for x in reader.calls if x[0] == 'app'])  # the JWT only mints
        self.assertIs(True, report['installationTokenMinted'])
        self.assertEqual('revoked', report['cleanup']['readerToken'])
        self.assertEqual([READER_TOKEN], reader.masked)
        self.assertEqual('mask', reader.order[1])   # masked right after the mint response, before any reader use
        self.assertLess(reader.order.index('mask'), reader.order.index(('reader', 'GET', settle.PREFIX + 'rulesets?includes_parents=true&per_page=100')))
        self.assertEqual({'administration': 'read', 'metadata': 'read'}, report['readerPolicy']['permissions'])
        for key in ('workflowMutation', 'tagMutation', 'publication', 'settlementEligible'):
            self.assertIs(False, report[key])

    def test_the_mint_happens_only_after_every_guard_and_app_metadata_read(self):
        observer, reader = self.run_with()
        events = observer.report['observations']
        mint_index = next(i for i, e in enumerate(events) if e.get('method') == 'POST')
        before = [(e['path']) for e in events[:mint_index]]
        # protected-source reads, workflow/ruleset census, /app, installation, main re-read, installation re-read.
        self.assertEqual(['/app', c.PREFIX + '/installation', c.PREFIX + '/git/ref/heads/main', c.PREFIX + '/installation'], before[-4:])
        self.assertEqual(10, mint_index)
        self.assertTrue(observer.report['determiningRereadComplete'])
        self.assertEqual(['POST', 'GET', 'GET', 'DELETE'], [e['method'] for e in events[mint_index:]])

    def test_no_mint_when_any_guard_or_determining_read_fails(self):
        cases = (
            ('moved main', {c.PREFIX + '/git/ref/heads/main': {'ref': 'refs/heads/main', 'object': {'type': 'commit', 'sha': 'b' * 40}}}, {}, signer),
            ('unprotected', {c.PREFIX + '/branches/main': {'name': 'main', 'protected': False, 'commit': {'sha': SHA}}}, {}, signer),
            ('wrong repository', {c.PREFIX: dict(REPO, id=1)}, {}, signer),
            ('rerun', {}, {'GITHUB_RUN_ATTEMPT': '2'}, signer),
            ('wrong app', {'/app': dict(APP, slug='other')}, {}, signer),
            ('suspended installation', {c.PREFIX + '/installation': dict(INSTALLATION, suspended_at='2026-01-01T00:00:00Z')}, {}, signer),
            ('moving installation', {c.PREFIX + '/installation': Seq(INSTALLATION, dict(INSTALLATION, id=9002))}, {}, signer),
            ('bad app id', {}, {'FSGG_DISPATCH_APP_ID': '0'}, signer),
            ('signing failure', {}, {}, lambda *a: (_ for _ in ()).throw(c.Refused('signing-unavailable'))))
        for name, override, env, sign in cases:
            observer, reader = self.run_with(fake=Fake(**override), env=dict(ENV, **env), sign=sign)
            self.assertEqual([], reader.calls, name)
            self.assertIs(False, observer.report['installationTokenMinted'], name)
            self.assertEqual('not-minted', observer.report['cleanup']['readerToken'], name)
            self.assertEqual('refused-or-incomplete', observer.report['result'], name)

    def test_guard_only_never_mints(self):
        observer, reader = self.run_with(guard_only=True, sign=lambda *a: self.fail('signed'))
        self.assertEqual([], reader.calls)

    # -- the digest -----------------------------------------------------------------------------------
    def test_reader_digest_equals_settlement_immutable_policy_on_identical_bodies(self):
        report = self.run_with()[0].report
        expected = c.digest(json.dumps([RULESET], sort_keys=True, separators=(',', ':')).encode())
        self.assertEqual(expected, report['readerPolicySha256'])
        self.assertEqual(report['readerPolicySha256'], report['readerPolicy']['sha256'])

        class Api:
            def get(self, path):
                return LISTING if path.startswith('rulesets?') else RULESET
        settle.immutable_policy(Api(), report['readerPolicySha256'])   # the exact value settlement will require
        with self.assertRaises(settle.Refused):
            settle.immutable_policy(Api(), '0' * 64)
        self.assertEqual(report['policy']['sha256'], report['readerPolicySha256'])
        self.assertTrue(report['readerPolicy']['matchesJobTokenDigest'])
        self.assertTrue(report['readerPolicy']['immutablePolicyReady'])

    def test_a_body_that_differs_by_caller_is_reported_and_only_the_reader_digest_matches_settlement(self):
        # The reader identity sees an extra caller-dependent field the job token does not.
        reader_body = dict(RULESET, current_user_can_bypass='never', updated_at='2026-10-22T00:00:00Z')
        observer, reader = self.run_with({'data': {settle.PREFIX + 'rulesets/1': reader_body}})
        report = observer.report
        self.assertEqual([], reasons(observer))
        self.assertNotEqual(report['policy']['sha256'], report['readerPolicySha256'])
        self.assertFalse(report['readerPolicy']['matchesJobTokenDigest'])

        class Api:
            def get(self, path):
                return LISTING if path.startswith('rulesets?') else reader_body
        settle.immutable_policy(Api(), report['readerPolicySha256'])
        with self.assertRaises(settle.Refused):   # the job-token digest would have burned the single-use settlement
            settle.immutable_policy(Api(), report['policy']['sha256'])
        entry = report['readerPolicy']['rulesets'][0]
        self.assertEqual(sorted(reader_body), entry['fields'])
        self.assertEqual(['current_user_can_bypass', 'updated_at'], entry['fieldsDifferingFromJobToken'])
        self.assertEqual([], entry['fieldsOnlyForJobToken'])
        self.assertEqual(0, entry['unnamedFieldCount'])

    def test_reader_only_weakening_denies_the_reader_readiness_flag_but_not_the_job_one(self):
        weak = dict(RULESET, bypass_actors=[{'actor_id': 1}])
        observer, reader = self.run_with({'data': {settle.PREFIX + 'rulesets/1': weak}})
        self.assertTrue(observer.report['immutablePolicyReady'])
        self.assertFalse(observer.report['readerPolicy']['immutablePolicyReady'])
        with self.assertRaises(settle.Refused):
            class Api:
                def get(self, path):
                    return LISTING if path.startswith('rulesets?') else weak
            settle.immutable_policy(Api(), observer.report['readerPolicySha256'])

    def test_field_names_only_and_hostile_names_are_counted_not_retained(self):
        hostile = dict(RULESET, **{'X-Evil name': 1})
        report = self.run_with({'data': {settle.PREFIX + 'rulesets/1': hostile}})[0].report
        entry = report['readerPolicy']['rulesets'][0]
        self.assertEqual(1, entry['unnamedFieldCount'])
        self.assertNotIn('Evil', json.dumps(report))

    def test_a_body_that_echoes_the_reader_token_is_refused_and_the_token_revoked(self):
        observer, reader = self.run_with({'data': {settle.PREFIX + 'rulesets/1': dict(RULESET, name=READER_TOKEN)}})
        self.assertIn(('readerPolicy', 'reader-policy-unavailable'), reasons(observer))
        self.assert_revoked_once(reader)
        self.assertNotIn(READER_TOKEN, json.dumps(observer.report))

    # -- same code path as settlement ---------------------------------------------------------------------
    def test_mint_and_policy_read_go_through_settlements_own_functions(self):
        calls = []
        def spy(name):
            real = getattr(settle, name)
            def wrapper(*a, **k):
                calls.append(name); return real(*a, **k)
            return wrapper
        with patch.object(settle, 'mint_token', spy('mint_token')), patch.object(settle, 'policy_details', spy('policy_details')), \
                patch.object(settle, 'policy_digest', spy('policy_digest')), patch.object(settle, 'policy_coverage', spy('policy_coverage')):
            self.run_with()
        self.assertEqual(['mint_token', 'policy_details', 'policy_digest', 'policy_coverage'], calls)

    def test_permission_set_and_mint_logic_are_not_duplicated_here(self):
        source = (ROOT / 'scripts/release-tag-capability.py').read_text().split('from __future__')[1]
        for needle in ("'administration'", 'READER_PERMISSIONS', 'expires_at', 'urlsplit', 'granted(', 'MINTS'):
            self.assertNotIn(needle, source, needle)
        settle_source = (ROOT / 'scripts/settle-original-tags.py').read_text()
        self.assertIn('mint_token(app, path, role', settle_source)       # settlement itself uses the shared function
        self.assertIn('policy_details(api)', settle_source)
        self.assertEqual({'administration': 'read', 'metadata': 'read'}, settle.READER_PERMISSIONS)
        # The default (production) loader exposes the same code and constants.
        loaded = c.load_settlement()
        self.assertEqual(settle.MINTS, loaded.MINTS)
        self.assertEqual(['mint_token', 'policy_details', 'policy_digest', 'policy_coverage', 'granted'],
                         [n for n in ('mint_token', 'policy_details', 'policy_digest', 'policy_coverage', 'granted') if callable(getattr(loaded, n))])

    # -- excess or wrong grants: refused and revoked ------------------------------------------------------------
    def test_excess_or_wrong_grant_is_refused_and_still_revoked_without_any_use(self):
        good = reader_grant()
        cases = {
            'administration write': reader_grant(permissions={'administration': 'write', 'metadata': 'read'}),
            'extra contents': reader_grant(permissions={'administration': 'read', 'metadata': 'read', 'contents': 'read'}),
            'missing metadata': reader_grant(permissions={'administration': 'read'}),
            'all repositories': reader_grant(repository_selection='all'),
            'two repositories': reader_grant(repositories=good['repositories'] * 2),
            'other repository': reader_grant(repositories=[{'id': 1, 'name': 'Other', 'full_name': 'FS-GG/Other'}]),
            'no expiry': reader_grant(expires_at=None),
            'far expiry': reader_grant(expires_at='2099-01-01T00:00:00Z')}
        for name, grant in cases.items():
            observer, reader = self.run_with({'grant': grant})
            self.assertIn(('readerPolicy', 'reader-credential-refused'), reasons(observer), name)
            self.assertEqual('refused-or-incomplete', observer.report['result'], name)
            self.assertEqual([('DELETE', '/installation/token')], self.reader_calls(reader), name)   # revoked, never used to read
            self.assert_revoked_once(reader)
            self.assertNotIn('readerPolicySha256', observer.report, name)
            self.assertEqual('revoked', observer.report['cleanup']['readerToken'], name)
            self.assertIs(True, observer.report['installationTokenMinted'], name)

    def test_mask_failure_still_revokes_the_held_token(self):
        fake = Fake(); reader = ReaderFake(fake); reader.mask_error = OSError('mask ' + READER_TOKEN)
        observer, _ = observe(fake, reader=reader)
        self.assertIn(('readerPolicy', 'reader-credential-refused'), reasons(observer))
        self.assert_revoked_once(reader)
        self.assertEqual([('DELETE', '/installation/token')], self.reader_calls(reader))
        self.assertNotIn(READER_TOKEN, json.dumps(observer.report))

    def test_mint_refusal_malformed_token_and_unknown_outcome(self):
        observer, reader = self.run_with({'mint_code': 403})
        self.assertIn(('readerPolicy', 'reader-mint-refused'), reasons(observer))
        self.assertIs(False, observer.report['installationTokenMinted'])
        self.assertEqual([], self.deletes(reader))
        self.assertEqual('not-minted', observer.report['cleanup']['readerToken'])
        # 201 with an unusable token: a credential exists that cannot be named, so it cannot be revoked.
        for bad in ('short', 'has space ' + 'x' * 30, 7, None):
            observer, reader = self.run_with({'grant': reader_grant(token=bad)})
            self.assertIn(('readerPolicy', 'reader-mint-refused'), reasons(observer), bad)
            self.assertIn(('cleanup', 'reader-token-unretrievable'), reasons(observer), bad)
            self.assertEqual('minted-unretrievable', observer.report['cleanup']['readerToken'])
            self.assertIs(True, observer.report['installationTokenMinted'])
            self.assertEqual([], self.deletes(reader))
        observer, reader = self.run_with({'mint_error': TimeoutError('boom ' + JWT)})
        self.assertIn(('readerPolicy', 'reader-mint-refused'), reasons(observer))
        self.assertEqual('mint-outcome-unknown', observer.report['cleanup']['readerToken'])
        self.assertNotIn(JWT, json.dumps(observer.report))
        self.assertEqual('refused-or-incomplete', observer.report['result'])

    # -- the token reaches nothing but the two ruleset routes ----------------------------------------------------------
    def test_token_is_never_used_for_a_non_ruleset_route(self):
        listing = [{'id': 1, '_links': {'self': {'href': 'https://api.github.com' + c.PREFIX + '/rulesets/1'}}}]
        observer, reader = self.run_with()
        for method, path in self.reader_calls(reader)[:-1]:
            self.assertEqual('GET', method)
            self.assertTrue(path == settle.PREFIX + POLICY or re.fullmatch(re.escape(settle.PREFIX) + r'rulesets/[1-9][0-9]*', path), path)
        # A hostile listing that tries to steer the detail read to another route is refused before any request.
        for ident in ('1/../git/refs', '1/../../actions/workflows', '0', '-1'):
            href = 'https://api.github.com' + c.PREFIX + '/rulesets/' + ident
            hostile = [{'id': ident, '_links': {'self': {'href': href}}}]
            observer, reader = self.run_with(fake=Fake(**{c.PREFIX + '/rulesets?includes_parents=true&per_page=100': hostile}))
            self.assertIn(('readerPolicy', 'route-refused'), reasons(observer), ident)
            self.assertEqual([('GET', settle.PREFIX + POLICY), ('DELETE', '/installation/token')], self.reader_calls(reader), ident)

    def test_ruleset_reader_is_closed_and_bounded(self):
        class Api:
            def __init__(self): self.paths = []
            def get(self, path):
                self.paths.append(path); return [] if path == c.POLICY_LISTING else {}
        for path in ('git/ref/heads/main', 'git/ref/tags/v0.32.3', '', 'rulesets', 'rulesets/0', 'rulesets/1/../x', 'rulesets/1?x', PREFIX_PATH,
                     'actions/workflows?per_page=100', 'installation', '../app'):
            api = Api(); reader = c.RulesetReader(api)
            with self.assertRaises(c.Refused) as caught:
                reader.get(path)
            self.assertEqual('route-refused', caught.exception.args[0], path); self.assertEqual([], api.paths)
        api = Api(); reader = c.RulesetReader(api)
        reader.get(c.POLICY_LISTING)
        for i in (1, 2, 3):
            reader.get('rulesets/' + str(i))
        with self.assertRaises(c.Refused) as caught:
            reader.get('rulesets/4')
        self.assertEqual('request-count-bound', caught.exception.args[0])
        self.assertEqual(4, len(api.paths))

    def test_more_than_three_rulesets_stop_before_any_detail_read(self):
        many = [{'id': i, '_links': {'self': {'href': 'https://api.github.com' + c.PREFIX + '/rulesets/' + str(i)}}} for i in (1, 2, 3, 4)]
        observer, reader = self.run_with(fake=Fake(**{c.PREFIX + '/rulesets?includes_parents=true&per_page=100': many}))
        self.assertIn(('readerPolicy', 'policy-cardinality-bound'), reasons(observer))
        self.assertEqual([('GET', settle.PREFIX + POLICY), ('DELETE', '/installation/token')], self.reader_calls(reader))

    # -- revocation on every failure path ----------------------------------------------------------------------------
    def test_revoked_after_any_read_failure(self):
        listing = c.PREFIX + '/rulesets?includes_parents=true&per_page=100'
        cases = {'listing transport': {listing: OSError('x ' + READER_TOKEN)},
                 'detail transport': {settle.PREFIX + 'rulesets/1': http.client.IncompleteRead(b'')},
                 'listing not a list': {listing: {'message': 'nope'}},
                 'detail not json': {settle.PREFIX + 'rulesets/1': ValueError('bad')}}
        for name, data in cases.items():
            observer, reader = self.run_with({'data': {k.replace(c.PREFIX, settle.PREFIX.rstrip('/'), 1): v for k, v in data.items()}})
            self.assertTrue(any(stage == 'readerPolicy' for stage, _ in reasons(observer)), name)
            self.assert_revoked_once(reader)
            self.assertEqual('revoked', observer.report['cleanup']['readerToken'], name)
            self.assertNotIn(READER_TOKEN, json.dumps(observer.report), name)
            self.assertNotIn('readerPolicySha256', observer.report, name)
            self.assertEqual('refused-or-incomplete', observer.report['result'], name)

    def test_revoked_when_the_process_is_signalled_or_interrupted_mid_read(self):
        for exc in (SystemExit(143), KeyboardInterrupt()):
            fake = Fake(); reader = ReaderFake(fake, on_get=lambda path, exc=exc: (_ for _ in ()).throw(exc))
            with tempfile.TemporaryDirectory() as d:
                observer = new_observer(Path(d), fake, reader=reader)
                with patch.object(c, 'local_git', side_effect=lambda r, *a: SHA if a[:1] == ('rev-parse',) else ''), \
                        patch.object(Path, 'read_bytes', lambda self: b'source'), self.assertRaises(type(exc)):
                    observer.run()
            self.assert_revoked_once(reader)
            self.assertEqual('revoked', observer.report['cleanup']['readerToken'])

    def test_sigterm_handler_exits_through_the_revocation_path(self):
        handlers = []
        with patch.object(c.signal, 'signal', side_effect=lambda sig, h: handlers.append((sig, h))), patch.object(sys, 'argv', ['x']), \
                patch.object(c, 'Observer') as factory:
            factory.return_value.report = {'result': 'observed'}
            c.main()
        self.assertEqual([c.signal.SIGTERM], [s for s, _ in handlers])
        with self.assertRaises(SystemExit) as caught:
            handlers[0][1](c.signal.SIGTERM, None)
        self.assertEqual(143, caught.exception.code)

    def test_unconfirmed_revocation_is_a_gap_and_never_reported_revoked(self):
        for kwargs in ({'revoke_code': 404}, {'revoke_code': 500}, {'revoke_code': 200}, {'revoke_error': TimeoutError('x ' + READER_TOKEN)}):
            observer, reader = self.run_with(kwargs)
            self.assertIn(('cleanup', 'reader-revocation-unconfirmed'), reasons(observer), kwargs)
            self.assertEqual('revocation-unconfirmed', observer.report['cleanup']['readerToken'], kwargs)
            self.assertEqual('refused-or-incomplete', observer.report['result'], kwargs)
            self.assertEqual(1, len(self.deletes(reader)))
            self.assertNotIn(READER_TOKEN, json.dumps(observer.report))

    def test_revocation_is_a_single_attempt_with_no_retry(self):
        observer, reader = self.run_with({'revoke_code': 500})
        self.assertEqual(1, len(self.deletes(reader)))

    # -- secrets and the retained report ----------------------------------------------------------------------------------
    def test_reader_token_is_a_canary_and_never_in_the_report_or_observations(self):
        observer, reader = self.run_with()
        raw = json.dumps(observer.report)
        for secret in (READER_TOKEN, JWT, JOB_TOKEN):
            self.assertNotIn(secret, raw)
        self.assertNotIn('rawBody', raw)
        self.assertIn(READER_TOKEN, observer.canaries())
        observer.report['note'] = 'leak ' + READER_TOKEN
        with tempfile.TemporaryDirectory() as d, self.assertRaises(c.Refused) as caught:
            observer.root = Path(d); observer.retain()
        self.assertEqual('report-secret-canary', caught.exception.args[0])

    def test_observations_keep_route_status_and_request_id_only(self):
        observer, reader = self.run_with()
        mine = [e for e in observer.report['observations'] if e.get('method')]
        self.assertEqual(['POST', 'GET', 'GET', 'DELETE'], [e['method'] for e in mine])
        self.assertEqual(['<reader-token-mint>'], [e['path'] for e in mine if e['method'] == 'POST'])
        for e in mine:
            self.assertEqual({'role', 'method', 'path', 'requestedUtc', 'status', 'requestId', 'observedUtc'}, set(e))
        self.assertEqual([201, 200, 200, 204], [e['status'] for e in mine])

    def test_hostile_request_id_from_the_reader_adapter_is_not_retained(self):
        fake = Fake(); reader = ReaderFake(fake)
        factory = reader.factory
        def hostile(token, role, journal, deadline):
            api = factory(token, role, journal, deadline)
            inner = api.opener.open
            def opener(request, timeout):
                response = inner(request, timeout); response.headers['x-github-request-id'] = 'bad id ' + READER_TOKEN
                return response
            api.opener = type('O', (), {'open': staticmethod(opener)})()
            return api
        reader.factory = hostile
        observer, _ = observe(fake, reader=reader)
        self.assertNotIn(READER_TOKEN, json.dumps(observer.report))
        self.assertTrue(all(e['requestId'] is None for e in observer.report['observations'] if e.get('method')))

    def test_schema_version_and_flag_semantics(self):
        observer, _ = self.run_with()
        self.assertEqual('fsgg.rendering.tag-capability/v2', observer.report['schema'])
        self.assertIs(True, observer.report['installationTokenMinted'])
        # A run that never reached the mint still reports false.
        early, _ = self.run_with(fake=Fake(**{c.PREFIX + '/branches/main': {'name': 'main', 'protected': False, 'commit': {'sha': SHA}}}))
        self.assertIs(False, early.report['installationTokenMinted'])

    def test_a_job_observer_gap_does_not_hide_the_reader_digest_and_vice_versa(self):
        odd = {'total_count': 1, 'workflows': [{'id': 1, 'path': ''}]}
        observer, _ = self.run_with(fake=Fake(**{c.PREFIX + '/actions/workflows?per_page=100': odd}))
        self.assertEqual([('workflowInventory', 'workflow-identity')], reasons(observer))
        self.assertIn('readerPolicySha256', observer.report)

    def test_source_paths_cover_every_file_executed_after_the_guard(self):
        for path in ('scripts/settle-original-tags.py', 'scripts/selected-producer-tags.py',
                     'scripts/release-source-guard.py', 'scripts/release-preflight.py'):
            self.assertIn(path, c.SOURCE_PATHS)
        # Every sibling script settlement imports is guarded; a new import would need a new pin.
        imported = set(re.findall(r"with_name\('([a-z-]+\.py)'\)", ''.join(
            (ROOT / 'scripts' / n).read_text() for n in ('settle-original-tags.py', 'selected-producer-tags.py', 'release-source-guard.py'))))
        self.assertEqual(set(), {'scripts/' + n for n in imported} - set(c.SOURCE_PATHS))


POLICY = 'rulesets?includes_parents=true&per_page=100'
PREFIX_PATH = c.PREFIX


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
        self.assertNotIn('secrets.', self.text.split('Observe dispatcher App metadata')[0])
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
