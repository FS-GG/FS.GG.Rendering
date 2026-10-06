#!/usr/bin/env python3
"""Cut only the retained SVG producer's tag triple; never publish or notify.

Run in release-tags.yml with its repository GITHUB_TOKEN. The workflow's outer
timeout bounds imported local source checks; each remote command also has a deadline.
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
from datetime import datetime, timezone

sys.dont_write_bytecode = True
SPEC = importlib.util.spec_from_file_location('selected_release_guard', Path(__file__).with_name('release-source-guard.py'))
guard = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
SPEC.loader.exec_module(guard)

PRODUCER = '6c9f766fdd91483c2de6f061e75589e94852a265'
VERSION = '0.32.1'
PLAN = 'eng/release/svg-export-prefix-0.32.1.json'
TAGS = ('fs-gg-ui/v0.32.1', 'fs-gg-ui-template/v0.32.1', 'v0.32.1')
REFS = tuple('refs/tags/' + tag for tag in TAGS)
WORKFLOW = 'FS-GG/FS.GG.Rendering/.github/workflows/release-tags.yml@refs/heads/main'
ORIGINS = ('https://github.com/FS-GG/FS.GG.Rendering', 'https://github.com/FS-GG/FS.GG.Rendering.git')


class Refused(Exception):
    """Known failure or unknown observation; neither permits another effect."""


class Git:
    def __init__(self, root: Path):
        self.root = root
        self.deadline = time.monotonic() + 240

    def __call__(self, *args: str) -> str:
        remaining = self.deadline - time.monotonic()
        if remaining <= 0:
            raise Refused('git command budget exhausted')
        try:
            seconds = min(20, remaining)
            # GNU timeout (also used by the workflow) signals git and its HTTPS children.
            # Killing only git on a Python timeout could leave a child holding its pipes.
            result = subprocess.run(
                ['timeout', '--kill-after=2s', f'{seconds:.3f}s',
                 'git', '-C', str(self.root), *args], capture_output=True, text=True,
                timeout=seconds + 5, env={**os.environ, 'GIT_TERMINAL_PROMPT': '0'})
        except (OSError, subprocess.TimeoutExpired) as exc:
            # Never print command output/remote URLs, which may contain authentication details.
            raise Refused(f'git {args[0]} unavailable or timed out; outcome unknown') from exc
        if result.returncode != 0:
            raise Refused(f'git {args[0]} exit {result.returncode}; outcome unknown')
        if len(result.stdout) > 16384:
            raise Refused(f'git {args[0]} output limit exceeded')
        return result.stdout.strip()


def executor_identity(git, env: dict) -> dict:
    expected = env.get('FSGG_EXPECTED_EXECUTOR_SHA', '')
    if (not re.fullmatch('[0-9a-f]{40}', expected)
            or env.get('GITHUB_SHA') != expected or env.get('GITHUB_WORKFLOW_SHA') != expected
            or env.get('GITHUB_REPOSITORY') != 'FS-GG/FS.GG.Rendering'
            or env.get('GITHUB_WORKFLOW_REF') != WORKFLOW
            or env.get('GITHUB_REF') != 'refs/heads/main'
            or env.get('GITHUB_EVENT_NAME') != 'workflow_dispatch'
            or not re.fullmatch('[1-9][0-9]*', env.get('GITHUB_RUN_ID', ''))
            or not re.fullmatch('[1-9][0-9]*', env.get('GITHUB_RUN_ATTEMPT', ''))):
        raise Refused('selected tag executor/event/workflow identity refused')
    if git('rev-parse', 'HEAD') != expected:
        raise Refused('checkout HEAD differs from reviewed executor')
    git('diff', '--quiet', 'HEAD', '--', '.github/workflows/release-tags.yml',
        'scripts/selected-producer-tags.py', 'scripts/release-source-guard.py', guard.ATTEMPT)
    for arguments in [('remote', 'get-url', '--all', 'origin'),
                      ('remote', 'get-url', '--push', '--all', 'origin')]:
        if git(*arguments) not in ORIGINS:
            raise Refused('origin must name only the canonical HTTPS repository')
    heads = parse_refs(git('ls-remote', 'origin', 'refs/heads/main'), {'refs/heads/main'})
    if heads != {'refs/heads/main': expected}:
        raise Refused('protected main moved or is unknown; reselect executor before effects')
    return {'expectedExecutorSha': expected, 'executorSha': expected,
            'workflowSha': env['GITHUB_WORKFLOW_SHA'], 'workflowRef': WORKFLOW,
            'run': env['GITHUB_RUN_ID'], 'attempt': env['GITHUB_RUN_ATTEMPT']}


def parse_refs(text: str, allowed: set[str]) -> dict:
    observed = {}
    for line in text.splitlines():
        fields = line.split()
        if (len(fields) != 2 or not re.fullmatch('[0-9a-f]{40}', fields[0])
                or fields[1] not in allowed or fields[1] in observed):
            raise Refused('remote ref response malformed, duplicated or outside selected refs')
        observed[fields[1]] = fields[0]
    return observed


def probe(git, refs=REFS) -> dict:
    requested = tuple(value for ref in refs for value in (ref, ref + '^{}'))
    observed = parse_refs(git('ls-remote', 'origin', *requested), set(requested))
    for ref in refs:
        if ref + '^{}' in observed or observed.get(ref) not in (None, PRODUCER):
            raise Refused(f'{ref}: conflicting or annotated tag; preserve and stop')
    return {ref: observed.get(ref) for ref in refs}


def ordered_prefix(state: dict) -> None:
    absent = False
    for ref in REFS:
        if state[ref] is None:
            absent = True
        elif absent:
            raise Refused('existing matching tags do not form the mandated ordered prefix')


def selected_binding(root: Path, source: str, version: str) -> dict:
    if source != PRODUCER or guard.PRODUCER != PRODUCER or version != VERSION:
        raise Refused('only original producer6c9 and version0.32.1 are admitted')
    # The publisher's executor_identity deliberately accepts only release.yml.
    # Reuse its immutable subject validators, never spoof its workflow environment.
    binding = guard.validate_attempt(root, source, require_ready=True)
    guard.validate(root, source, PLAN, version, require_publication_ready=False)
    return binding


class Operation:
    def __init__(self, root: Path, mode: str, source: str, version: str, env: dict,
                 git=None, binding_check=selected_binding):
        self.root, self.mode, self.source, self.version = root, mode, source, version
        self.env, self.git, self.binding_check = env, git or Git(root), binding_check
        self.path = root / 'artifacts/selected-producer-tags' / (mode + '.json')
        self.receipt = {'schema': 'fsgg.rendering.selected-producer-tags/v1', 'mode': mode,
                        'producerSha': source, 'version': version, 'tags': list(TAGS),
                        'result': 'pending', 'publication': 'not-performed',
                        'notification': 'not-performed', 'events': []}

    def record(self, stage: str, **fields) -> None:
        self.receipt['events'].append({'utc': datetime.now(timezone.utc).isoformat(),
                                       'stage': stage, **fields})
        self.path.parent.mkdir(parents=True, exist_ok=True)
        with self.path.open('w') as stream:
            json.dump(self.receipt, stream, indent=2)
            stream.write('\n')
            stream.flush()
            os.fsync(stream.fileno())

    def admit(self) -> None:
        identity = executor_identity(self.git, self.env)
        binding = self.binding_check(self.root, self.source, self.version)
        # Preserve all selected candidate/receiver coordinates, not a new readiness claim.
        self.receipt.update(identity)
        self.receipt['binding'] = binding
        self.record('identities-accepted')

    def run(self) -> dict:
        self.record('started')
        try:
            if self.mode not in ('check', 'cut'):
                raise Refused('unsupported operation mode')
            self.admit()
            initial = probe(self.git)
            ordered_prefix(initial)
            self.record('all-three-before-effects', refs=initial)
            known = initial
            if self.mode == 'cut':
                for ref in REFS:
                    self.admit()
                    current = probe(self.git)
                    ordered_prefix(current)
                    # A tag observed before this run or created by it must never disappear.
                    if any(current[previous] != PRODUCER for previous in REFS
                           if known[previous] == PRODUCER):
                        raise Refused('an observed tag disappeared; stop before the next effect')
                    self.record('before-tag', ref=ref, refs=current)
                    if current[ref] == PRODUCER:
                        self.record('preserved', ref=ref)
                    else:
                        # Journal before transport; a failed acknowledgement may still have effects.
                        self.record('push-requested', ref=ref, target=PRODUCER)
                        try:
                            self.git('push', '--no-follow-tags', '--recurse-submodules=no',
                                     '--porcelain', 'origin', f'{PRODUCER}:{ref}')
                        except Refused:
                            self.record('push-outcome-unknown', ref=ref)
                            # One bounded READ ONLY observation, never another push or implicit retry.
                            try:
                                observed = probe(self.git, (ref,))
                                self.record('unknown-push-readback', ref=ref, refs=observed)
                            except Refused:
                                self.record('unknown-push-readback-unavailable', ref=ref)
                            raise
                        self.record('push-acknowledged', ref=ref)
                    observed = probe(self.git, (ref,))
                    if observed[ref] != PRODUCER:
                        raise Refused(f'{ref}: immediate readback did not match original producer')
                    self.record('immediate-readback-matched', ref=ref, refs=observed)
                    known = {**current, ref: PRODUCER}
                self.admit()
                final = probe(self.git)
                if any(value != PRODUCER for value in final.values()):
                    raise Refused('final ordered triple readback incomplete')
                self.record('triple-readback-matched', refs=final)
            self.receipt['result'] = 'passed'
            self.record('completed')
            return self.receipt
        except (Refused, SystemExit, OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as exc:
            self.receipt['result'] = 'refused-or-unknown'
            # Exceptions from source validators contain paths/identities only; do not expose git output.
            self.record('stopped', reason=str(exc))
            raise Refused(str(exc)) from exc


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', required=True, choices=('check', 'cut'))
    parser.add_argument('--source-sha', required=True)
    parser.add_argument('--version', required=True)
    args = parser.parse_args()
    operation = Operation(Path.cwd(), args.mode, args.source_sha, args.version, dict(os.environ))
    try:
        result = operation.run()
    except Refused as exc:
        print(f'selected-producer-tags: {exc}; stop and observe before resume', file=sys.stderr)
        return 1
    print(json.dumps({'result': result['result'], 'mode': args.mode, 'receipt': str(operation.path)}))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
