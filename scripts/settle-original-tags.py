#!/usr/bin/env python3
"""Closed 0323 settlement: original Unknown plus immutable current convergence.

The admission digest selects root's separately reviewed administrative/writer
capability evidence. It is not a credential or a permission inference. No retries,
feed writes, ref updates/deletes, run cancellations or automatic continuations.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile

sys.dont_write_bytecode = True
SPEC = importlib.util.spec_from_file_location('settlement_tags', Path(__file__).with_name('selected-producer-tags.py'))
tags = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(tags)
Refused = tags.Refused
REPO = 'FS-GG/FS.GG.Rendering'
PREFIX = '/repos/' + REPO + '/'
WORKFLOW = REPO + '/.github/workflows/release-tag-settlement.yml@refs/heads/main'
BINDING = 'eng/release/font-disposal-0.32.3-original-tag-intent.json'
SOURCE = tags.guard.FONT_DISPOSAL['producerSha']
EFFECT = tags.guard.FONT_DISPOSAL['effectId']
REFS = tuple('refs/tags/' + s for s in ('fs-gg-ui/v0.32.3', 'fs-gg-ui-template/v0.32.3', 'v0.32.3'))
FENCES = {310031035: 'release-tags.yml', 295951545: 'release.yml', 303545957: 'template-dispatch.yml'}
MAX_BODY = 131072


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def need(condition, message):
    if not condition:
        raise Refused(message)


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise Refused('redirect refused; outcome unknown')


class API:
    """Finite, no-retry adapter. Transport evidence is retained before parsing."""
    def __init__(self, token, journal, deadline):
        need(bool(token), 'explicit credential unavailable')
        self.token, self.journal, self.deadline = token, journal, deadline
        self.opener = urllib.request.build_opener(NoRedirect)

    def call(self, method, path, body=None):
        allowed = (method == 'GET' and path.startswith(PREFIX) or
                   method == 'PUT' and any(path == PREFIX + f'actions/workflows/{i}/{v}' for i in FENCES for v in ('disable', 'enable')) or
                   method == 'POST' and path == PREFIX + 'git/refs' and body in [{'ref': r, 'sha': SOURCE} for r in REFS] or
                   method == 'DELETE' and path == '/installation/token')
        need(allowed and '..' not in path and '#' not in path, 'unsupported endpoint or request body')
        remaining = self.deadline - time.monotonic()
        need(remaining > 0, 'transport budget exhausted')
        self.journal('http-requested', method=method, path=path, body=body)
        request = urllib.request.Request('https://api.github.com' + path,
            data=json.dumps(body).encode() if body is not None else None, method=method,
            headers={'Authorization': 'Bearer ' + self.token, 'Accept': 'application/vnd.github+json',
                     'X-GitHub-Api-Version': '2022-11-28', 'Content-Type': 'application/json'})
        try:
            try:
                response = self.opener.open(request, timeout=min(10, remaining))
            except urllib.error.HTTPError as error:
                response = error
            with response:
                raw = response.read(MAX_BODY + 1)
                need(len(raw) <= MAX_BODY, 'response exceeds retained bound')
                # Never persist auth headers, arbitrary exceptions or credential echoes.
                need(self.token.encode() not in raw, 'credential echo refused')
                self.journal('http-response', method=method, path=path, status=response.code,
                    requestId=response.headers.get('x-github-request-id'), rawBody=raw.decode('utf-8'))
                return response.code, json.loads(raw) if raw else None
        except (OSError, ValueError, TimeoutError) as error:
            self.journal('http-outcome-unknown', method=method, path=path)
            raise Refused('transport unavailable, malformed or timed out; outcome unknown') from error

    def get(self, path):
        status, value = self.call('GET', PREFIX + path)
        need(status == 200, 'complete authenticated GET unavailable: ' + path)
        return value


def original(root, directory):
    raw_binding = (root / BINDING).read_bytes()
    need(digest(raw_binding) == 'a2443e3ca805a1b726eb0a47fcbf499dbaace56243deb50b00ea23c86503f5a6', 'closed original intent bytes differ')
    binding = json.loads(raw_binding)
    need(binding['effectId'] == EFFECT and binding['producerSha'] == SOURCE and binding['originalTransportOutcome'] == 'Unknown', 'original binding differs')
    docs = {}
    for pin in binding['artifacts']:
        raw = (directory / (str(pin['id']) + '.zip')).read_bytes()
        need(len(raw) == pin['size'] and digest(raw) == pin['sha256'], 'original archive replaced or truncated')
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            need(z.namelist() == [pin['member']], 'original archive inventory differs')
            member = z.read(pin['member'])
        need(len(member) == pin['memberSize'] and digest(member) == pin['memberSha256'], 'original receipt bytes differ')
        docs[pin['member']] = json.loads(member)
    cut = docs['cut.json']
    need(cut['run'] == str(binding['run']) and cut['attempt'] == '1' and cut['executorSha'] == binding['executorSha'] and cut['producerSha'] == SOURCE and cut['result'] == 'refused-or-unknown', 'original receipt identity differs')
    requests = [x for x in cut['events'] if x['stage'] == 'push-requested']
    need(requests == [{'utc': binding['requestedUtc'], 'stage': 'push-requested', 'ref': REFS[0], 'target': SOURCE}], 'original single create-only intent differs')
    need(cut['binding'] == json.loads((root / tags.guard.FONT_DISPOSAL_ATTEMPT).read_bytes()), 'original qualification roots replaced')
    return binding


def admission(raw, expected, env):
    need(re.fullmatch('[0-9a-f]{64}', expected or '') is not None and digest(raw) == expected, 'admission bytes differ')
    value = json.loads(raw)
    need(value['schema'] == 'fsgg.rendering.tag-settlement-admission/v1' and value['effectId'] == EFFECT and value['executorSha'] == env['FSGG_EXPECTED_EXECUTOR_SHA'], 'admission subject differs')
    need(value['originalTransportOutcome'] == 'Unknown' and value['allowedRefs'] == list(REFS), 'original uncertainty or intent replaced')
    need(value['writer']['kind'] == 'installation-token' and value['writer']['repository'] == REPO and value['writer']['permissions'] == {'contents': 'write', 'workflows': 'write', 'metadata': 'read'}, 'wrong or excess declared writer capability')
    need(value['writer']['capabilityEvidenceSha256'] and value['administrator']['capabilityEvidenceSha256'], 'separately accepted capability evidence missing')
    for key in ('policySha256', 'workflowInventorySha256'):
        need(re.fullmatch('[0-9a-f]{64}', value[key]) is not None, key + ' unavailable')
    need(value['activeRunDisposition'] == 'refuse' and value['restorePreviousStates'] is True and value['originalActorId'] == 1645484, 'unbounded or implicit administrative disposition')
    return value


def immutable_policy(api, expected):
    rules = api.get('rulesets?includes_parents=true&per_page=100')
    need(isinstance(rules, list) and 0 < len(rules) < 100, 'effective/inherited policy census incomplete')
    details = []
    for rule in rules:
        # Native detail links carry inherited organization ownership; never invent a repo-local id.
        link = rule.get('_links', {}).get('self', {}).get('href', '')
        parsed = urllib.parse.urlsplit(link)
        need(parsed.scheme == 'https' and parsed.netloc == 'api.github.com' and not parsed.query, 'effective policy detail route unknown')
        need(parsed.path == PREFIX + 'rulesets/' + str(rule['id']), 'inherited policy requires separately qualified detail adapter')
        details.append(api.get('rulesets/' + str(rule['id'])))
    need(digest(json.dumps(details, sort_keys=True, separators=(',', ':')).encode()) == expected, 'effective policy changed')
    covered = set()
    for rule in details:
        if rule.get('target') != 'tag' or rule.get('enforcement') != 'active':
            continue
        conditions = rule.get('conditions', {}).get('ref_name', {})
        if rule.get('bypass_actors') != [] or conditions.get('exclude') != []:
            continue
        if not {'update', 'deletion'} <= {x['type'] for x in rule.get('rules', [])}:
            continue
        # Exact includes deliberately refuse unexamined glob semantics.
        covered.update(set(conditions.get('include', [])) & set(REFS))
    need(covered == set(REFS), 'immutable exact-ref update/deletion restrictions or no-bypass proof missing')


class Settlement:
    def __init__(self, root, env, admitted, original_binding, admin=None, writer=None, identity=None):
        self.root, self.env, self.admitted = root, env, admitted
        self.path = root / 'artifacts/tag-settlement/settlement.json'
        self.receipt = {'schema': 'fsgg.rendering.tag-settlement/v1', 'effectId': EFFECT,
            'producerSha': SOURCE, 'originalTransportOutcome': 'Unknown', 'sameIntentConvergence': 'Unknown',
            'original': original_binding, 'admission': admitted, 'events': [], 'cleanup': 'pending',
            'publication': 'not-performed', 'notification': 'not-performed'}
        self.deadline = time.monotonic() + 240
        self.admin = admin or API(env.get('FSGG_SETTLEMENT_ADMIN_TOKEN'), self.record, self.deadline)
        self.writer = writer or API(env.get('FSGG_SETTLEMENT_WRITER_TOKEN'), self.record, self.deadline - 60)
        self.identity_check = identity or self.identity
        self.previous = {}

    def record(self, stage, **fields):
        need(len(self.receipt['events']) < 700, 'journal event bound exhausted')
        self.receipt['events'].append({'stage': stage, **fields})
        self.path.parent.mkdir(parents=True, exist_ok=True)
        raw = json.dumps(self.receipt, indent=2).encode() + b'\n'
        need(len(raw) < 4 * 1024 * 1024, 'journal byte bound exhausted')
        with self.path.open('wb') as stream:
            stream.write(raw); stream.flush(); os.fsync(stream.fileno())

    def identity(self):
        env = self.env
        expected = env.get('FSGG_EXPECTED_EXECUTOR_SHA', '')
        need(re.fullmatch('[0-9a-f]{40}', expected) is not None and env.get('GITHUB_SHA') == expected and env.get('GITHUB_WORKFLOW_SHA') == expected and env.get('GITHUB_WORKFLOW_REF') == WORKFLOW and env.get('GITHUB_REF') == 'refs/heads/main' and env.get('GITHUB_EVENT_NAME') == 'workflow_dispatch' and env.get('GITHUB_REPOSITORY') == REPO and env.get('GITHUB_RUN_ATTEMPT') == '1', 'settlement executor context refused')
        need(self.admin.get('git/ref/heads/main')['object'] == {'type': 'commit', 'sha': expected, 'url': 'https://api.github.com/repos/' + REPO + '/git/commits/' + expected}, 'protected main moved')
        git = tags.Git(self.root)
        need(git('rev-parse', 'HEAD') == expected, 'checkout differs')
        git('diff', '--quiet', 'HEAD', '--', 'scripts/settle-original-tags.py', 'scripts/selected-producer-tags.py', 'scripts/release-source-guard.py', BINDING, 'eng/release/font-disposal-0.32.3-workflow-fences.json', '.github/workflows/release-tag-settlement.yml')
        tags.selected_binding(self.root, SOURCE, '0.32.3')
        run = self.admin.get('actions/runs/' + str(self.receipt['original']['run']))
        need(run.get('status') == 'completed' and run.get('run_attempt') == 1 and run.get('head_sha') == self.receipt['original']['executorSha'] and run.get('actor', {}).get('id') == self.receipt['original']['actorId'] and run.get('event') == 'workflow_dispatch' and run.get('path') == '.github/workflows/release-tags.yml', 'old actor/run fence unknown')
        job = self.admin.get('actions/jobs/' + str(self.receipt['original']['job']))
        need(job.get('run_id') == self.receipt['original']['run'] and job.get('status') == 'completed', 'old job is active or unknown')
        self.receipt.update(executorSha=expected, workflowRef=WORKFLOW, run=env['GITHUB_RUN_ID'], attempt='1')

    def inventory(self):
        workflows = self.admin.get('actions/workflows?per_page=100')
        need(workflows['total_count'] == len(workflows['workflows']) < 100, 'workflow inventory incomplete')
        pairs = sorted((w['id'], w['path']) for w in workflows['workflows'])
        need(digest(json.dumps(pairs, separators=(',', ':')).encode()) == self.admitted['workflowInventorySha256'], 'workflow inventory changed')
        selected = [w for w in workflows['workflows'] if w['path'] == '.github/workflows/release-tag-settlement.yml']
        need(len(selected) == 1, 'settlement workflow unavailable')
        runs = self.admin.get('actions/workflows/' + str(selected[0]['id']) + '/runs?per_page=100')
        need(runs['total_count'] == len(runs['workflow_runs']) == 1 and str(runs['workflow_runs'][0]['id']) == self.env['GITHUB_RUN_ID'], 'prior subordinate settlement exists; no blind continuation')
        for i, name in FENCES.items():
            need((i, '.github/workflows/' + name) in pairs, 'required event workflow missing')
        # Every producer and executor workflow byte is pinned by the reviewed source census.
        pins = json.loads((self.root / 'eng/release/font-disposal-0.32.3-workflow-fences.json').read_bytes())
        git = tags.Git(self.root)
        for revision, expected in ((SOURCE, pins['producer']), ('HEAD', pins['executor'])):
            paths = git('ls-tree', '-r', '--name-only', revision, '.github/workflows').splitlines()
            need(paths == sorted(expected), 'workflow trigger source inventory differs')
            for path in paths:
                raw = tags.guard.source_bytes(self.root, revision, path)
                need(digest(raw) == expected[path], 'workflow trigger source bytes differ')

    def drained(self):
        for i in FENCES:
            workflow = self.admin.get('actions/workflows/' + str(i))
            need(workflow['state'] == 'disabled_manually', 'event workflow fence unavailable')
            for status in ('queued', 'in_progress', 'waiting', 'pending', 'requested'):
                runs = self.admin.get(f'actions/workflows/{i}/runs?status={status}&per_page=100')
                need(runs['total_count'] == len(runs['workflow_runs']) == 0, 'active or incomplete old-workflow census; separately owned terminal disposition required')

    def fence(self):
        self.identity_check()
        immutable_policy(self.admin, self.admitted['policySha256'])
        self.drained()
        need(time.monotonic() < self.deadline - 60, 'effect budget exhausted; cleanup reserved')

    def refs(self):
        values = {}
        for ref in REFS:
            status, value = self.admin.call('GET', PREFIX + 'git/ref/' + ref.removeprefix('refs/'))
            if status == 404:
                values[ref] = None
            else:
                need(status == 200 and value['ref'] == ref and value['object']['type'] == 'commit' and value['object']['sha'] == SOURCE, 'conflicting/annotated/unknown ref')
                values[ref] = SOURCE
        tags.ordered_prefix(values, REFS)
        return values

    def run(self):
        first_error = None
        try:
            self.record('started')
            self.identity_check(); self.inventory()
            immutable_policy(self.admin, self.admitted['policySha256'])
            # Record prior states before any disable. Unknown states refuse all effects.
            for i in FENCES:
                state = self.admin.get('actions/workflows/' + str(i))['state']
                need(state in ('active', 'disabled_manually'), 'workflow prior state unknown')
                self.previous[i] = state
            self.record('previous-workflow-states', states=self.previous)
            for i, state in self.previous.items():
                if state == 'active':
                    status, _ = self.admin.call('PUT', PREFIX + f'actions/workflows/{i}/disable')
                    need(status == 204, 'disable response unknown; readback and cleanup required')
            self.fence()
            known = self.refs()
            for ref in REFS:
                self.fence()
                current = self.refs()
                need(all(current[r] == SOURCE for r in REFS if known[r] == SOURCE), 'previous ref disappeared')
                if current[ref] is None:
                    self.record('subordinate-create-requested', ref=ref, target=SOURCE)
                    try:
                        status, _ = self.writer.call('POST', PREFIX + 'git/refs', {'ref': ref, 'sha': SOURCE})
                        self.record('subordinate-create-response', ref=ref, status=status)
                    except Refused:
                        self.record('subordinate-transport-unknown', ref=ref)
                    # One independent bounded full-prefix read even on lost acknowledgement.
                    self.fence()
                    current = self.refs()
                    need(current[ref] == SOURCE, 'subordinate create did not converge; no resend')
                self.record('exact-ref-readback', ref=ref, refs=current)
                known = current
            self.fence()
            final = self.refs()
            need(all(x == SOURCE for x in final.values()), 'final triple incomplete')
            self.receipt['refs'] = final
            self.receipt['sameIntentConvergence'] = 'Verified'
            self.record('triple-converged')
        except (Refused, SystemExit, OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError, zipfile.BadZipFile) as error:
            first_error = type(error).__name__
            self.record('first-failure', category=first_error)
        finally:
            # Retire the installation token before restoring any event trigger. Failed
            # retirement keeps fences closed and records an independently owned cleanup gap.
            retired = False
            try:
                status, _ = self.writer.call('DELETE', '/installation/token')
                need(status == 204, 'writer retirement unknown')
                retired = True
                self.record('writer-retired')
            except (Refused, OSError, ValueError, KeyError, TypeError):
                self.record('cleanup-failure', operation='writer-retirement')
            restored = retired
            if retired:
                for i, state in self.previous.items():
                    try:
                        current = self.admin.get('actions/workflows/' + str(i))['state']
                        if current != state:
                            action = 'enable' if state == 'active' else 'disable'
                            status, _ = self.admin.call('PUT', PREFIX + f'actions/workflows/{i}/{action}')
                            need(status == 204, 'restore acknowledgement unknown')
                        need(self.admin.get('actions/workflows/' + str(i))['state'] == state, 'restore readback differs')
                        self.record('workflow-restored', workflow=i, state=state)
                    except (Refused, OSError, ValueError, KeyError, TypeError):
                        restored = False
                        self.record('cleanup-failure', operation='workflow-restoration', workflow=i)
            self.receipt['cleanup'] = 'restored' if restored else 'Unknown-owned-gap'
            self.receipt['result'] = 'passed' if not first_error and restored and self.receipt['sameIntentConvergence'] == 'Verified' else 'refused-or-unknown'
            self.record('completed')
        need(self.receipt['result'] == 'passed', 'settlement or cleanup refused/unknown; preserve original and subordinate history')
        return self.receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--admission', type=Path, required=True)
    parser.add_argument('--admission-sha256', required=True)
    parser.add_argument('--original-directory', type=Path, required=True)
    args = parser.parse_args()
    root = Path.cwd(); env = dict(os.environ)
    try:
        admitted = admission(args.admission.read_bytes(), args.admission_sha256, env)
        binding = original(root, args.original_directory)
        def terminate(signum, frame):
            raise Refused('command deadline signaled; cleanup and retention required')
        signal.signal(signal.SIGTERM, terminate)
        Settlement(root, env, admitted, binding).run()
        return 0
    except (Refused, OSError, ValueError, KeyError, TypeError, zipfile.BadZipFile):
        print('tag settlement refused/unknown; retained evidence and separate cleanup disposition required', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
