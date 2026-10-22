#!/usr/bin/env python3
"""Protected Rendering capability observer: GET requests plus exactly one reader mint.

After every guard and App metadata read it mints ONE installation token with the same
reader request settlement uses (`settle-original-tags.py` MINTS['reader']), judges the
mint response with settlement's own `mint_token`, reads only the two ruleset routes
settlement's `policy_details` reads, reports the digest that identity sees, and revokes
the token even on failure. No writer mint, workflow/ref/package mutation or settlement
run. App permission ceilings are observations; no read establishes mutation authority.
"""
from __future__ import annotations
import argparse
import base64
from datetime import datetime, timezone
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import tempfile
import time
import http.client
import urllib.error
import urllib.request

sys.dont_write_bytecode = True
REPO = 'FS-GG/FS.GG.Rendering'
REPO_ID = 1269292235
PREFIX = '/repos/' + REPO
APP_SLUG = 'fs-gg-cross-repo-dispatch'
WORKFLOW = REPO + '/.github/workflows/release-tag-capability.yml@refs/heads/main'
REPORT = 'artifacts/tag-capability/report.json'
# The reader mint and policy code is loaded from settlement's own source, so those bytes are guarded too.
SOURCE_PATHS = ('scripts/release-tag-capability.py', 'scripts/test-release-tag-capability.py',
    '.github/workflows/release-tag-capability.yml', '.github/workflows/gate.yml',
    'eng/release/font-disposal-0.32.3-workflow-fences.json', 'scripts/settle-original-tags.py',
    'scripts/selected-producer-tags.py', 'scripts/release-source-guard.py', 'scripts/release-preflight.py')
MAX_REQUESTS = 18   # 12 job/App reads + 1 reader mint + at most 4 reader ruleset reads + 1 revocation
CLEANUP_WINDOW = 4   # seconds; shorter than timeout --kill-after=5s
POLICY_LISTING = 'rulesets?includes_parents=true&per_page=100'
REFS = ('refs/tags/fs-gg-ui/v0.32.3', 'refs/tags/fs-gg-ui-template/v0.32.3', 'refs/tags/v0.32.3')
SAFE_ENV = {'PATH': os.defpath, 'LANG': 'C', 'LC_ALL': 'C'}


class Refused(Exception):
    """Closed reason code only; never retain provider/crypto exception text."""


def need(condition, reason):
    if not condition:
        raise Refused(reason)


def utc():
    return datetime.now(timezone.utc).isoformat()


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        need(key not in result, 'duplicate-json-key')
        result[key] = value
    return result


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise Refused('redirect-refused')


class Budget:
    def __init__(self):
        self.started = time.monotonic()
        self.deadline = self.started + 180
        self.effect_deadline = self.deadline - 30
        self.requests = 0
        self.bytes = 0

    def remaining(self):
        value = self.effect_deadline - time.monotonic()
        need(value > 0, 'observation-deadline')
        return value


class GET:
    """Closed host/route/token-role adapter; no mutation method exists."""
    def __init__(self, token, role, budget, events):
        need(isinstance(token, str) and 20 < len(token) < 16384, 'credential-unavailable')
        self.token, self.role, self.budget, self.events = token, role, budget, events
        self.opener = urllib.request.build_opener(NoRedirect)

    def get(self, path):
        job_paths = {PREFIX, PREFIX + '/git/ref/heads/main', PREFIX + '/branches/main',
                     PREFIX + '/actions/workflows?per_page=100',
                     PREFIX + '/rulesets?includes_parents=true&per_page=100'}
        allowed = ((self.role == 'job' and (path in job_paths or re.fullmatch(re.escape(PREFIX) + r'/rulesets/[1-9][0-9]*', path)))
                   or (self.role == 'app' and path in ('/app', PREFIX + '/installation')))
        need(allowed, 'route-refused')
        remaining = self.budget.remaining()
        need(self.budget.requests < MAX_REQUESTS, 'request-count-bound')
        self.budget.requests += 1
        event = {'path': path, 'requestedUtc': utc(), 'status': None, 'requestId': None}
        self.events.append(event)
        request = urllib.request.Request('https://api.github.com' + path, method='GET',
            headers={'Authorization': 'Bearer ' + self.token, 'Accept': 'application/vnd.github+json',
                     'X-GitHub-Api-Version': '2022-11-28', 'User-Agent': 'fsgg-rendering-tag-capability'})
        try:
            try:
                response = self.opener.open(request, timeout=min(10, remaining))
            except urllib.error.HTTPError as error:
                response = error
            with response:
                event.update(status=response.code, requestId=request_id(response.headers.get('x-github-request-id')), observedUtc=utc())
                need(not response.headers.get('Link'), 'pagination-continuation-or-unknown')
                raw = response.read(256 * 1024 + 1)
                self.budget.bytes += len(raw)
                need(len(raw) <= 256 * 1024 and self.budget.bytes <= 2 * 1024 * 1024, 'response-byte-bound')
                event['responseBytes'] = len(raw)
                need(type(response.code) is int and 100 <= response.code <= 599, 'response-status-shape')
                need(response.code == 200, 'http-' + str(response.code))
                # Raw responses and credentials never enter the retained report.
                value = json.loads(raw, object_pairs_hook=unique)
                need(isinstance(value, (dict, list)), 'response-shape')
                return value
        except Refused:
            raise
        except (OSError, ValueError, TimeoutError, http.client.HTTPException):
            event['outcome'] = 'unknown'
            raise Refused('transport-or-json-unavailable') from None


def request_id(value):
    # Provider-controlled text enters evidence only in GitHub's fixed identifier shape.
    return value if isinstance(value, str) and re.fullmatch('[0-9A-Fa-f:-]{1,64}', value) else None


def local_git(root, *args):
    try:
        result = subprocess.run(['git', '-C', str(root), *args], env=SAFE_ENV,
            capture_output=True, timeout=5, check=False)
        need(result.returncode == 0 and len(result.stdout) <= 16384, 'checkout-unavailable-or-dirty')
        return result.stdout.decode().strip()
    except (OSError, ValueError, subprocess.SubprocessError):
        raise Refused('checkout-unavailable-or-dirty') from None


def main_ref(job, expected):
    value = job.get(PREFIX + '/git/ref/heads/main')
    need(isinstance(value, dict) and value.get('ref') == 'refs/heads/main'
         and value.get('object', {}).get('type') == 'commit' and value['object'].get('sha') == expected, 'main-moved-or-unknown')


def protected_source(root, env, job):
    expected = env.get('FSGG_EXPECTED_EXECUTOR_SHA', '')
    need(re.fullmatch('[0-9a-f]{40}', expected) is not None
         and env.get('GITHUB_SHA') == expected and env.get('GITHUB_WORKFLOW_SHA') == expected
         and env.get('GITHUB_WORKFLOW_REF') == WORKFLOW and env.get('GITHUB_REF') == 'refs/heads/main'
         and env.get('GITHUB_REPOSITORY') == REPO and env.get('GITHUB_EVENT_NAME') == 'workflow_dispatch'
         and env.get('GITHUB_RUN_ATTEMPT') == '1' and re.fullmatch('[1-9][0-9]*', env.get('GITHUB_RUN_ID', '')) is not None,
         'executor-context')
    need(local_git(root, 'rev-parse', 'HEAD') == expected, 'checkout-sha')
    local_git(root, 'diff', '--quiet', 'HEAD', '--', *SOURCE_PATHS)
    repository = job.get(PREFIX)
    need(isinstance(repository, dict) and repository.get('id') == REPO_ID and repository.get('full_name') == REPO
         and repository.get('owner', {}).get('login') == 'FS-GG' and repository.get('default_branch') == 'main'
         and repository.get('archived') is False, 'repository-identity')
    main_ref(job, expected)
    branch = job.get(PREFIX + '/branches/main')
    need(isinstance(branch, dict) and branch.get('name') == 'main' and branch.get('protected') is True
         and branch.get('commit', {}).get('sha') == expected, 'protected-main-unavailable')
    return {'executorSha': expected, 'workflowSha': expected, 'workflowRef': WORKFLOW,
            'run': env['GITHUB_RUN_ID'], 'attempt': '1', 'repositoryId': REPO_ID,
            'sourceSha256': {p: digest((root / p).read_bytes()) for p in SOURCE_PATHS}}


def encoded(raw):
    return base64.urlsafe_b64encode(raw).rstrip(b'=').decode('ascii')


def app_jwt(app_id, private_key, budget, cleanup):
    """Same fixed RS256 pattern as pinned maintained App readers; no mint route."""
    need(isinstance(private_key, str) and 100 < len(private_key.encode()) <= 32768 and 'PRIVATE KEY' in private_key, 'private-key-unavailable')
    now = int(time.time())
    header = encoded(b'{"alg":"RS256","typ":"JWT"}')
    payload = encoded(json.dumps({'iat': now - 60, 'exp': now + 480, 'iss': str(app_id)}, separators=(',', ':')).encode())
    unsigned = (header + '.' + payload).encode()
    directory = None; key = None
    try:
        directory = Path(tempfile.mkdtemp(prefix='rendering-capability-key-'))
        directory.chmod(0o700)
        key = directory / 'key.pem'
        descriptor = os.open(key, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
        with os.fdopen(descriptor, 'wb') as stream:
            stream.write(private_key.encode()); stream.flush()
        result = subprocess.run(['/usr/bin/openssl', 'dgst', '-sha256', '-sign', str(key)],
            input=unsigned, capture_output=True, env=SAFE_ENV,
            timeout=min(10, budget.remaining()), check=False)
        need(result.returncode == 0 and 64 <= len(result.stdout) <= 8192, 'signing-unavailable')
        return header + '.' + payload + '.' + encoded(result.stdout)
    except (OSError, ValueError, subprocess.SubprocessError):
        raise Refused('signing-unavailable') from None
    finally:
        try:
            if key is not None and key.exists(): key.unlink()
            if directory is not None:
                directory.rmdir()
                cleanup['temporaryKey'] = 'removed'
        except OSError:
            cleanup['temporaryKey'] = 'Unknown-owned-gap'
            raise Refused('temporary-key-cleanup-unavailable') from None


def grants(value):
    need(isinstance(value, dict) and len(value) <= 100 and all(
        isinstance(k, str) and re.fullmatch('[a-z][a-z_]{0,63}', k) and v in ('read', 'write', 'admin', 'none')
        for k, v in value.items()), 'permission-shape')
    return dict(sorted(value.items()))


def events(value):
    need(isinstance(value, list) and len(value) <= 100 and all(isinstance(v, str) and re.fullmatch('[a-z][a-z_]{0,63}', v) for v in value)
         and len(set(value)) == len(value), 'event-shape')
    return sorted(value)


def app_identity(value, app_id):
    need(isinstance(value, dict) and type(value.get('id')) is int and value['id'] == app_id
         and value.get('slug') == APP_SLUG and value.get('owner', {}).get('login') == 'FS-GG'
         and value['owner'].get('type') == 'Organization', 'app-identity')
    return {'identityMatched': True, 'identityFingerprint': digest(('FS-GG/' + APP_SLUG + '/' + str(app_id)).encode()),
            'permissions': grants(value.get('permissions')), 'events': events(value.get('events'))}


def installation_identity(value, app_id):
    need(isinstance(value, dict) and type(value.get('id')) is int and value['id'] > 0
         and type(value.get('app_id')) is int and value['app_id'] == app_id and value.get('app_slug') == APP_SLUG
         and value.get('account', {}).get('login') == 'FS-GG' and value['account'].get('type') == 'Organization'
         and type(value['account'].get('id')) is int and type(value.get('target_id')) is int and value.get('target_id') == value['account']['id']
         and value.get('target_type') == 'Organization' and 'suspended_at' in value and value['suspended_at'] is None
         and value.get('repository_selection') in ('selected', 'all'), 'installation-identity-or-state')
    return {'identityMatched': True, 'identityFingerprint': digest(json.dumps(
                [app_id, value['id'], value['account']['id']], separators=(',', ':')).encode()),
            'repositorySelection': value['repository_selection'], 'selectedRepositoriesComplete': False,
            'targetRepositoryIncluded': True, 'permissions': grants(value.get('permissions')), 'events': events(value.get('events'))}


def workflow_inventory(job):
    value = job.get(PREFIX + '/actions/workflows?per_page=100')
    need(isinstance(value, dict) and type(value.get('total_count')) is int and isinstance(value.get('workflows'), list)
         and 0 < len(value['workflows']) == value['total_count'] < 100, 'workflow-census-incomplete')
    pairs = []
    for row in value['workflows']:
        need(isinstance(row, dict) and type(row.get('id')) is int and row['id'] > 0 and isinstance(row.get('path'), str)
             and 0 < len(row['path']) <= 256, 'workflow-identity')
        pairs.append((row['id'], row['path']))
    need(len({i for i,p in pairs}) == len(pairs) and len({p for i,p in pairs}) == len(pairs), 'workflow-duplicate')
    return {'complete': True, 'count': len(pairs), 'sha256': digest(json.dumps(sorted(pairs), separators=(',', ':')).encode())}


def policy_observation(job, retained=None):
    listing = job.get(PREFIX + '/rulesets?includes_parents=true&per_page=100')
    need(isinstance(listing, list) and len(listing) <= 3, 'policy-cardinality-bound')
    details, observations, covered, identifiers = [], [], set(), set()
    for row in listing:
        need(isinstance(row, dict) and type(row.get('id')) is int and row['id'] > 0 and row['id'] not in identifiers, 'policy-list-identity')
        identifiers.add(row['id'])
        link = row.get('_links', {}).get('self', {}).get('href')
        need(link == 'https://api.github.com' + PREFIX + '/rulesets/' + str(row['id']), 'inherited-policy-detail-unsupported')
        detail = job.get(PREFIX + '/rulesets/' + str(row['id']))
        need(isinstance(detail, dict) and detail.get('id') == row['id'], 'policy-detail-identity')
        details.append(detail)
        if retained is not None: retained[row['id']] = detail
        present = 'bypass_actors' in detail
        actors = detail.get('bypass_actors')
        need(not present or isinstance(actors, list), 'policy-bypass-shape')
        # Actor identities need not be serialized to preserve exact empty versus unknown.
        conditions = (detail.get('conditions') or {}).get('ref_name')
        need(conditions is None or isinstance(conditions, dict), 'policy-condition-shape')
        include = conditions.get('include') if conditions is not None else None
        exclude = conditions.get('exclude') if conditions is not None else None
        need(include is None or isinstance(include, list) and all(isinstance(x, str) and len(x) <= 256 for x in include), 'policy-include-shape')
        need(exclude is None or isinstance(exclude, list) and all(isinstance(x, str) and len(x) <= 256 for x in exclude), 'policy-exclude-shape')
        rules = detail.get('rules')
        need(isinstance(rules, list) and all(isinstance(x, dict) and isinstance(x.get('type'), str) for x in rules), 'policy-rule-shape')
        types = [x['type'] for x in rules]
        need(detail.get('target') in ('tag', 'branch', 'push') and detail.get('enforcement') in ('active', 'evaluate', 'disabled')
             and all(re.fullmatch('[a-z][a-z_]{0,63}', value) for value in types), 'policy-rule-shape')
        observations.append({'id': row['id'], 'target': detail.get('target'), 'enforcement': detail.get('enforcement'),
            'bypassActorsFieldPresent': present, 'bypassActorsEmpty': actors == [] if present else None,
            'bypassActorCount': len(actors) if present else None, 'include': include, 'exclude': exclude, 'ruleTypes': types})
        if (detail.get('target') == 'tag' and detail.get('enforcement') == 'active' and present and actors == []
            and exclude == [] and isinstance(include, list) and {'update', 'deletion'} <= set(types)):
            covered.update(set(include) & set(REFS))
    # This digest covers the bodies as the JOB token saw them and is not the settlement digest: settlement
    # recomputes with the minted reader identity, whose bodies can differ. `readerPolicySha256`, observed
    # through that same mint and code path, is the value for admission; this one is a comparison only.
    return {'available': True, 'complete': True, 'observedAs': 'job-token',
            'digestJoinCandidate': bool(details) and all('bypass_actors' in d for d in details),
            'sha256': digest(json.dumps(details, sort_keys=True, separators=(',', ':')).encode()),
            'rulesets': observations, 'immutablePolicyReady': covered == set(REFS)}


def load_settlement():
    """Settlement's own module: the reader mint, grant judgement, adapter and policy digest are not duplicated."""
    spec = importlib.util.spec_from_file_location('observer_settlement', Path(__file__).with_name('settle-original-tags.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class RulesetReader:
    """The minted reader may reach only the two ruleset routes settlement's policy_details reads."""
    def __init__(self, api):
        self.api, self.reads = api, 0

    def get(self, path):
        need(path == POLICY_LISTING or re.fullmatch('rulesets/[1-9][0-9]*', path) is not None, 'route-refused')
        need(self.reads < 4, 'request-count-bound')
        self.reads += 1
        value = self.api.get(path)
        if path == POLICY_LISTING:
            need(isinstance(value, list) and len(value) <= 3, 'policy-cardinality-bound')
        return value


MISSING = object()


def reader_fields(detail, job_detail):
    """Top-level field names of one ruleset body as the reader saw it; names only, never values."""
    names = [k for k in detail if isinstance(k, str) and re.fullmatch('[a-z][a-z_]{0,63}', k)]
    result = {'id': detail.get('id') if type(detail.get('id')) is int else None, 'fields': sorted(names),
              'unnamedFieldCount': len(detail) - len(names)}
    if isinstance(job_detail, dict):
        result['fieldsDifferingFromJobToken'] = sorted(k for k in names if detail.get(k, MISSING) != job_detail.get(k, MISSING))
        result['fieldsOnlyForJobToken'] = sorted(k for k in job_detail if k not in detail and isinstance(k, str) and re.fullmatch('[a-z][a-z_]{0,63}', k))
    return result


class Observer:
    def __init__(self, root, env, transport=GET, signer=app_jwt, settlement=None, api_factory=None, mask=None):
        self.root, self.env, self.transport, self.signer = root, env, transport, signer
        self.settlement, self.api_factory, self.mask = settlement, api_factory, mask
        self.job_details = {}
        self.budget = Budget(); self.secrets = []
        self.report = {'schema': 'fsgg.rendering.tag-capability/v2', 'requestedUtc': utc(),
            'result': 'pending', 'observations': [], 'gaps': [], 'cleanup': {'temporaryKey': 'not-created', 'readerToken': 'not-minted'},
            'installationTokenMinted': False, 'workflowMutation': False, 'tagMutation': False,
            'publication': False, 'settlementEligible': False, 'writerPermissionCeilingCompatible': False,
            'selectedRepositoriesComplete': False, 'immutablePolicyReady': False}

    def gap(self, stage, error):
        # Only our closed reason vocabulary can enter evidence.
        self.report['gaps'].append({'stage': stage, 'reason': error.args[0] if isinstance(error, Refused) else 'unavailable'})

    def journal(self, stage, **fields):
        """Reader-adapter journal: route, status and request id only; never a body, token or provider text."""
        if stage == 'http-requested':
            self.budget.requests += 1
            self.report['observations'].append({'role': fields.get('role'), 'method': fields.get('method'),
                'path': fields.get('path') if fields.get('role') != 'app' or fields.get('method') != 'POST' else '<reader-token-mint>',
                'requestedUtc': utc(), 'status': None, 'requestId': None})
        elif stage == 'http-response' and self.report['observations']:
            event = self.report['observations'][-1]
            event.update(status=fields.get('status'), requestId=request_id(fields.get('requestId')), observedUtc=utc())
            if event.get('method') == 'POST' and fields.get('status') == 201:
                self.report['installationTokenMinted'] = True   # a credential now exists and must be revoked
        elif stage == 'http-outcome-unknown' and self.report['observations']:
            self.report['observations'][-1]['outcome'] = 'unknown'
            if fields.get('method') == 'POST':
                self.report['cleanup']['readerToken'] = 'mint-outcome-unknown'

    def reader_phase(self, jwt, installation_id, job_digest):
        """Mint ONE reader token, read the ruleset bodies it sees, and always revoke it."""
        held = []
        settle = self.settlement or load_settlement()
        factory = self.api_factory or settle.API
        mask = self.mask or settle.mask_credential
        stage = 'reader-mint-refused'
        try:
            try:
                app = factory(jwt, 'app', self.journal, self.budget.effect_deadline)
                def hold(api):
                    held.append(api); self.secrets.append(api.token)
                    self.report['cleanup']['readerToken'] = 'minted-unrevoked'
                path = '/app/installations/' + str(installation_id) + '/access_tokens'
                api, grant = settle.mint_token(app, path, 'reader', mask, factory, self.journal, self.budget.effect_deadline, hold)
                stage = 'reader-policy-unavailable'
                details = settle.policy_details(RulesetReader(api))
                digest_value = settle.policy_digest(details)
                try:
                    settle.policy_coverage(details); ready = True
                except Exception:
                    ready = False
                observed = {'observedAs': 'reader-token', 'permissions': grant['permissions'],
                    'sha256': digest_value, 'matchesJobTokenDigest': job_digest == digest_value,
                    'immutablePolicyReady': ready, 'rulesets': [reader_fields(d, self.job_details.get(d.get('id'))) for d in details]}
                self.report['readerPolicy'] = observed
                self.report['readerPolicySha256'] = digest_value
            except Refused:
                raise
            except Exception:
                raise Refused('reader-credential-refused' if held and stage == 'reader-mint-refused' else stage) from None
        except Refused as error:
            self.gap('readerPolicy', error)
        finally:
            self.revoke(held)

    def revoke(self, held):
        if not held and self.report['installationTokenMinted']:
            # A credential exists that this process cannot name (malformed response); it can only expire.
            self.report['cleanup']['readerToken'] = 'minted-unretrievable'
            self.gap('cleanup', Refused('reader-token-unretrievable'))
        for api in held:
            try:
                # The SIGTERM that interrupts a hung read arrives after the observation deadline (190 s
                # timeout vs 180 s budget): revocation gets its own short window ahead of the 5 s kill grace.
                status, _ = api.retire(max(self.budget.deadline, time.monotonic() + CLEANUP_WINDOW))
                self.report['cleanup']['readerToken'] = 'revoked' if status == 204 else 'revocation-unconfirmed'
            except Exception:
                self.report['cleanup']['readerToken'] = 'revocation-unconfirmed'
            if self.report['cleanup']['readerToken'] != 'revoked':
                self.gap('cleanup', Refused('reader-revocation-unconfirmed'))

    def run(self, guard_only=False):
        try:
            job_token = self.env.get('GITHUB_TOKEN', '')
            self.secrets.append(job_token)
            job = self.transport(job_token, 'job', self.budget, self.report['observations'])
            # These calls finish before either App secret is accessed or JWT signed.
            self.report['executor'] = protected_source(self.root, self.env, job)
            if guard_only:
                return self.report
            for stage, operation in (('workflowInventory', workflow_inventory),
                                     ('policy', lambda j: policy_observation(j, self.job_details))):
                try:
                    value = operation(job); self.report[stage] = value
                    if stage == 'policy': self.report['immutablePolicyReady'] = value['immutablePolicyReady']
                except Exception as error:
                    self.gap(stage, error)
            app_id_raw = self.env.pop('FSGG_DISPATCH_APP_ID', '')
            private_key = self.env.pop('FSGG_DISPATCH_APP_PRIVATE_KEY', '')
            self.secrets.extend([app_id_raw, private_key])
            need(isinstance(app_id_raw, str) and re.fullmatch('[1-9][0-9]{0,19}', app_id_raw), 'app-id-unavailable')
            app_id = int(app_id_raw)
            jwt = self.signer(app_id, private_key, self.budget, self.report['cleanup'])
            self.secrets.append(jwt)
            app = self.transport(jwt, 'app', self.budget, self.report['observations'])
            self.report['app'] = app_identity(app.get('/app'), app_id)
            installation = app.get(PREFIX + '/installation')
            initial = installation_identity(installation, app_id)
            self.report['installation'] = initial
            main_ref(job, self.report['executor']['executorSha'])
            final = installation_identity(app.get(PREFIX + '/installation'), app_id)
            need(initial == final, 'installation-moved')
            self.report['writerPermissionCeilingCompatible'] = all(
                grant.get('contents') == 'write' and grant.get('workflows') == 'write'
                for grant in (self.report['app']['permissions'], initial['permissions']))
            self.report['determiningRereadComplete'] = True
            self.reader_phase(jwt, installation['id'], self.report.get('policy', {}).get('sha256'))
        except Exception as error:  # reduced to a closed reason by gap(); never traceback text
            self.gap('observer', error)
        finally:
            self.report.update(observedUtc=utc(), requestCount=self.budget.requests, responseBytes=self.budget.bytes,
                elapsedSeconds=round(time.monotonic() - self.budget.started, 3))
            if any(g['stage'] == 'observer' for g in self.report['gaps']):
                # An incomplete determining observation cannot leave positive readiness facts behind.
                self.report['writerPermissionCeilingCompatible'] = self.report['immutablePolicyReady'] = False
            self.report['result'] = 'observed' if not self.report['gaps'] else 'refused-or-incomplete'
        return self.report

    def canaries(self):
        """Raw, stripped, JSON-escaped and per-line forms: a multi-line key is escaped inside JSON."""
        found = set()
        for value in self.secrets:
            if not value: continue
            for form in (value, value.strip(), json.dumps(value)[1:-1], json.dumps(value.strip())[1:-1],
                         *(line.strip() for line in value.splitlines() if len(line.strip()) >= 16)):
                if form: found.add(form)
        return found

    def retain(self, late=False):
        raw = json.dumps(self.report, sort_keys=True, separators=(',', ':')).encode() + b'\n'
        need(len(raw) <= 65536, 'report-byte-bound')
        need(not any(canary.encode() in raw for canary in self.canaries()), 'report-secret-canary')
        need(late or time.monotonic() <= self.budget.deadline, 'report-deadline')
        path = self.root / REPORT
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open('xb') as stream:
            stream.write(raw); stream.flush(); os.fsync(stream.fileno())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--guard-only', action='store_true')
    args = parser.parse_args()
    observer = Observer(Path.cwd(), dict(os.environ))
    if not args.guard_only:
        # The workflow timeout signals SIGTERM; unwind through the reader token's revocation.
        signal.signal(signal.SIGTERM, lambda signum, frame: sys.exit(143))
    late = False
    try:
        observer.run(args.guard_only)
    except SystemExit:
        # Signalled mid-run: the reader token was already revoked by run()'s unwinding; retain that outcome.
        late = True
        observer.gap('observer', Refused('terminated'))
        observer.report['result'] = 'refused-or-incomplete'
    try:
        if not args.guard_only: observer.retain(late)
        return 0 if observer.report['result'] == 'observed' else 1
    except (Refused, OSError, ValueError, TypeError):
        print('release-tag-capability refused; sanitized report unavailable', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
