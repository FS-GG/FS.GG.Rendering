#!/usr/bin/env python3
"""Every Templates commit the held caller pins must be reachable from Templates `main`.

GitHub resolves a reusable workflow only for a commit reachable from a branch or tag. A PR-head
commit that a squash merge leaves behind still shows in the commits API but fails at run time with
"workflow was not found", which invalidates the whole caller (Templates 72a54c27, FS-GG/.github#4817).
`compare/main...<sha>` reports `identical` or `behind` exactly when the commit is an ancestor of main.
Fail closed: an unreachable pin, an unknown status or an unavailable API all refuse.
"""
import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / '.github/workflows/templates-external-reference-qualification.yml'
REPOSITORY = 'FS-GG/FS.GG.Templates'
REACHABLE = {'identical', 'behind'}


def pins(text):
    uses = re.findall(r'^\s+uses: ' + re.escape(REPOSITORY) + r'/\.github/workflows/[A-Za-z0-9_.-]+@(\S+)$', text, re.M)
    sources = re.findall(r'^\s+templates-source: (\S+)$', text, re.M)
    assert uses, 'no Templates reusable-workflow pin found'
    assert len(uses) == len(sources) and uses == sources, 'workflow reference and templates-source must name the same commit'
    for sha in uses:
        assert re.fullmatch('[0-9a-f]{40}', sha), 'pin is not an exact commit: ' + sha
    return sorted(set(uses))


def compare_status(sha, token=None, opener=urllib.request.urlopen):
    request = urllib.request.Request('https://api.github.com/repos/%s/compare/main...%s' % (REPOSITORY, sha),
                                     headers={'Accept': 'application/vnd.github+json', 'User-Agent': 'fsgg-pin-reachability',
                                              **({'Authorization': 'Bearer ' + token} if token else {})})
    for attempt in (1, 2):
        try:
            with opener(request, timeout=20) as response:
                return json.load(response)['status']
        except urllib.error.HTTPError as error:
            if error.code >= 500 and attempt == 1:
                continue
            return 'unavailable-http-%d' % error.code
        except (urllib.error.URLError, TimeoutError, ValueError, KeyError):
            return 'unavailable'
    return 'unavailable'


def check(text, status):
    problems = []
    checked = pins(text)
    for sha in checked:
        state = status(sha)
        if state not in REACHABLE:
            problems.append('%s is not reachable from %s main (compare status %s)' % (sha, REPOSITORY, state))
    return checked, problems


def self_test():
    sample = ('jobs:\n  a:\n    uses: %s/.github/workflows/x.yml@%s\n    with:\n      templates-source: %s\n') % (REPOSITORY, 'a' * 40, 'a' * 40)
    table = {'a' * 40: 'behind', 'b' * 40: 'diverged'}
    cases = 0
    assert check(sample, table.get) == (['a' * 40], [])
    cases += 1
    for state in ['diverged', 'ahead', 'unavailable', 'unavailable-http-404', None, 'identical-ish']:
        _, problems = check(sample, lambda sha, s=state: s)
        assert len(problems) == 1, state
        cases += 1
    assert check(sample, lambda sha: 'identical')[1] == []
    cases += 1
    for bad in [sample.replace('@' + 'a' * 40, '@main'), sample.replace('templates-source: ' + 'a' * 40, 'templates-source: ' + 'b' * 40),
                sample.replace('a' * 40, 'a' * 39), 'jobs: {}\n']:
        try:
            pins(bad)
        except AssertionError:
            cases += 1
        else:
            raise AssertionError('malformed pin accepted')
    # The real compare statuses of the 2026-10-30 evidence, as the API reports them.
    live = {'fe24d30872c84d4c1c92afd7a05c5cf22d00bb10': 'behind', '72a54c27f49eff90f1c86bc68e7a8ed8bfd9a431': 'diverged'}
    assert check(sample.replace('a' * 40, '72a54c27f49eff90f1c86bc68e7a8ed8bfd9a431'), live.get)[1]
    assert not check(sample.replace('a' * 40, 'fe24d30872c84d4c1c92afd7a05c5cf22d00bb10'), live.get)[1]
    cases += 2
    print('PASS pin reachability self-test: %d cases' % cases)


if __name__ == '__main__':
    self_test()
    if sys.argv[1:] == ['--self-test']:
        raise SystemExit(0)
    assert sys.argv[1:] == [], 'unknown option'
    text = WORKFLOW.read_text()
    token = os.environ.get('GH_TOKEN') or os.environ.get('GITHUB_TOKEN')
    checked, problems = check(text, lambda sha: compare_status(sha, token))
    for sha in checked:
        print('pin', sha)
    if problems:
        print('\n'.join('REFUSED: ' + p for p in problems), file=sys.stderr)
        raise SystemExit(1)
    print('PASS %d distinct Templates pins are reachable from main' % len(checked))
