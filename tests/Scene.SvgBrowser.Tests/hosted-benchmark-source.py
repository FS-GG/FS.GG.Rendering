"""Overlay one producer revision under the current benchmark dependency environment."""
import hashlib
import json
import pathlib
import re
import subprocess
import sys

PRODUCERS = ('src/Scene', 'src/Scene.SvgBrowser', 'src/KeyboardInput')
LOCKS = tuple(path + '/packages.lock.json' for path in PRODUCERS)


def git(tree, *args):
    return subprocess.check_output(['git', '-C', str(tree), *args])


def overlay(tree, revision):
    if not re.fullmatch('[0-9a-f]{40}', revision):
        raise ValueError('producer revision must be an exact commit SHA')
    harness = git(tree, 'rev-parse', 'HEAD').decode().strip()
    # Refuse missing harness locks before touching the isolated producer tree.
    locks = {path: git(tree, 'show', harness + ':' + path) for path in LOCKS}
    git(tree, 'cat-file', '-e', revision + '^{commit}')
    git(tree, 'diff', '--exit-code', 'HEAD', '--', *PRODUCERS)
    git(tree, 'rm', '-r', '-q', '--ignore-unmatch', '--', *PRODUCERS)
    git(tree, 'checkout', revision, '--', *PRODUCERS)
    # Both arms use this harness's central pins and locked dependency graph.
    # Only producer source varies; historical commits/locks are never rewritten.
    git(tree, 'checkout', harness, '--', *LOCKS)
    source_paths = [p for p in git(tree, 'ls-tree', '-r', '--name-only', revision, '--', *PRODUCERS).decode().splitlines() if p not in LOCKS]
    actual_paths = [p for p in git(tree, 'ls-files', '--', *PRODUCERS).decode().splitlines() if p not in LOCKS]
    if sorted(source_paths) != sorted(actual_paths):
        raise ValueError('producer source path set changed during overlay')
    # Git compares both content and executable modes against the named producer.
    git(tree, 'diff', '--exit-code', revision, '--', *PRODUCERS, *(':(exclude)' + p for p in LOCKS))
    for path, content in locks.items():
        if (pathlib.Path(tree) / path).read_bytes() != content:
            raise ValueError('harness dependency lock changed: ' + path)
    return {'producerRevision': revision, 'harnessRevision': harness,
            'producerSourceFiles': len(source_paths),
            'harnessLocks': {p: hashlib.sha256(b).hexdigest() for p, b in locks.items()}}


if __name__ == '__main__':
    print(json.dumps(overlay(pathlib.Path(sys.argv[1]), sys.argv[2]), sort_keys=True))
