#!/usr/bin/env python3
"""Offline loaded-identity fixtures; never invokes .NET."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
sys.dont_write_bytecode = True
SCRIPT = Path(__file__).with_name('release-toolchain-identity.py')
spec = importlib.util.spec_from_file_location('identity', SCRIPT)
identity = importlib.util.module_from_spec(spec)
spec.loader.exec_module(identity)

class IdentityTests(unittest.TestCase):
    def test_loaded_identity_and_failures(self):
        for case in ['valid', 'floating-loaded-sdk', 'missing-tool', 'timeout', 'malformed-fsi', 'missing-compiler']:
            with self.subTest(case=case), tempfile.TemporaryDirectory() as temp:
                root=Path(temp); folder=root/'sdk/10.0.401/FSharp'; folder.mkdir(parents=True)
                core=folder/'FSharp.Core.dll'; core.write_bytes(b'fixture-core')
                compiler=folder/'fsc.dll'
                if case != 'missing-compiler': compiler.write_bytes(b'fixture-compiler')
                receipt=root/'identity.json'
                def run(*args):
                    if case == 'missing-tool': raise FileNotFoundError('dotnet unavailable')
                    if case == 'timeout': raise subprocess.TimeoutExpired(args,60)
                    if args[1]=='--version': return '10.0.999' if case=='floating-loaded-sdk' else '10.0.401'
                    if args[1]=='--info': return 'fixture host/SDK info'
                    if args[1]=='fsi':
                        if case=='malformed-fsi': return 'invalid'
                        return json.dumps({'fsharpCorePath':str(core),'fsharpCoreVersion':'10.1.401','runtimeVersion':'10.0.0'})
                    return 'fixture F# compiler version'
                with patch.object(identity, 'run', side_effect=run), patch.object(identity.shutil,'which',return_value='/usr/bin/dotnet'), patch.object(sys,'argv',[str(SCRIPT),'--expected-sdk','10.0.401','--receipt',str(receipt)]):
                    self.assertEqual(0 if case=='valid' else 1,identity.main())
                self.assertEqual(case=='valid',receipt.exists())
                if case=='valid':
                    data=json.loads(receipt.read_text())
                    self.assertEqual('10.0.401',data['sdkVersion'])
                    self.assertEqual(64,len(data['compilerSha256']))

if __name__=='__main__': unittest.main()
