#!/usr/bin/env python3
"""Fail before release workload if the loaded SDK differs; retain loaded compiler identities."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys


def run(*args):
    result = subprocess.run(args, capture_output=True, text=True, timeout=60, check=True)
    return result.stdout.strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--expected-sdk', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    try:
        actual = run('dotnet', '--version')
        if actual != args.expected_sdk:
            raise ValueError(f'loaded SDK {actual!r}, expected {args.expected_sdk!r}')
        info = run('dotnet', '--info')
        fsharp = json.loads(run('dotnet', 'fsi', '--exec', str(Path(__file__).with_suffix('.fsx'))).splitlines()[-1])
        core = Path(fsharp['fsharpCorePath']).resolve(strict=True)
        compiler = core.with_name('fsc.dll')
        if not compiler.is_file() or core.parent.parent.name != actual:
            raise ValueError('loaded F# core/compiler does not belong to the selected SDK')
        data = {'schema': 'fsgg.rendering.release-toolchain/v1', 'sdkVersion': actual,
                'dotnetHost': str(Path(shutil.which('dotnet')).resolve()), 'dotnetInfo': info,
                'compilerPath': str(compiler), 'compilerVersion': run('dotnet', str(compiler), '--help').splitlines()[0], 'compilerSha256': hashlib.sha256(compiler.read_bytes()).hexdigest(),
                'fsharpCoreSha256': hashlib.sha256(core.read_bytes()).hexdigest(), **fsharp}
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(data, indent=2, sort_keys=True) + '\n')
        print(f'release-toolchain: loaded SDK {actual}; F# core {fsharp["fsharpCoreVersion"]}; compiler {data["compilerSha256"]}')
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        print(f'release-toolchain: {error}', file=sys.stderr)
        return 1
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
