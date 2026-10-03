#!/usr/bin/env python3
"""Offline exact-candidate/source/tag and public disposition controls; no acquisition or CLR."""
import importlib.util
from pathlib import Path
import sys
import unittest
import json
import tempfile
import os
import subprocess
from unittest.mock import patch
sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('mirror',Path(__file__).with_name('qualify-retained-api-mirror.py'))
mirror=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mirror)

class RetainedMirrorTests(unittest.TestCase):
    def test_public_complete_goes_back_to_default_and_partial_remains_pending(self):
        self.assertFalse(mirror.decide([200]*19))
        self.assertTrue(mirror.decide([404]*19))
        self.assertTrue(mirror.decide([200]*18+[404]))

    def test_unknown_public_disposition_refuses(self):
        for statuses in [[200]*18,[200]*18+[403],[200]*18+[500],[200]*18+[None]]:
            with self.subTest(statuses=statuses),self.assertRaises(ValueError):mirror.decide(statuses)

    def test_exact_candidate_source_pins_and_three_producer_tags_required(self):
        candidate='a'*40
        def git(*args):
            value=args[-1]
            if args[0]=='status':return ''
            if value=='HEAD':return candidate
            if value.endswith(':src'):return 'b'*40
            if value.endswith('^{commit}'):return mirror.guard.PRODUCER
            raise AssertionError(args)
        with patch.object(mirror,'git',side_effect=git),patch.object(mirror.guard,'validate_attempt',return_value={'planPath':'plan'}) as binding,patch.object(mirror.guard,'validate',return_value={}),patch.object(mirror.guard,'source_bytes',return_value=b'pinned'):
            self.assertEqual('b'*40,mirror.source_facts(candidate)[1])
            for mutation in ['head','source','tag','pins']:
                def changed(*args):
                    if mutation=='head' and args[-1]=='HEAD':return 'c'*40
                    if mutation=='source' and args[-1]==candidate+':src':return 'c'*40
                    if mutation=='tag' and args[-1]==mirror.TAGS[1]+'^{commit}':return 'c'*40
                    return git(*args)
                with self.subTest(mutation=mutation),patch.object(mirror,'git',side_effect=changed),patch.object(mirror.guard,'source_bytes',side_effect=lambda root,sha,path: b'changed' if mutation=='pins' and sha==candidate else b'pinned'):
                    with self.assertRaises(ValueError):mirror.source_facts(candidate)

    def test_reverification_missing_changed_or_accepted_receipts_refuse(self):
        candidate='a'*40
        with tempfile.TemporaryDirectory() as folder:
            receipt=Path(folder)/'qualified.json'
            good={'schema':'fsgg.rendering.retained-api-mirror/v1','candidateSha':candidate,'candidateTree':'c'*40,'producerSha':mirror.guard.PRODUCER,'sourceTree':'b'*40,'producerTree':'c'*40,'custodySha256':mirror.guard.CUSTODY,'planSha256':mirror.guard.PLAN_HASH,'outerSha256':mirror.guard.OUTER,'mode':'retained-original','publicationAcceptance':False,'installedAcceptance':False,'mutation':'none','publicStatuses':[404]*19}
            argv=['helper','--select-original730','--candidate-sha',candidate,'--receipt',str(receipt),'--verify']
            with patch.object(sys,'argv',argv),patch.object(mirror,'verify_local',return_value=({},'b'*40,{})),patch.object(mirror,'git',side_effect=lambda *args: candidate if args[-1]=='HEAD' else 'c'*40):
                with self.assertRaises(OSError):mirror.main()
                receipt.write_text(json.dumps(good));mirror.main()
                for key,value in [('candidateSha','d'*40),('candidateTree','d'*40),('sourceTree','d'*40),('custodySha256','d'*64),('publicationAcceptance',True),('installedAcceptance',True),('publicStatuses',[200]*19),('publicStatuses',[404]*18+[403])]:
                    bad={**good,key:value};receipt.write_text(json.dumps(bad))
                    with self.subTest(key=key,value=value),self.assertRaises(ValueError):mirror.main()

    def test_actual_workflow_passes_head_even_when_native_sha_is_merge_context(self):
        root=Path(__file__).resolve().parent.parent
        text=(root/'.github/workflows/gate.yml').read_text()
        step=text.split('      - name: Qualify explicit original730 tagged recovery mirror input (read only)',1)[1].split('      - name:',1)[0]
        self.assertNotIn('          GITHUB_SHA:',step)
        script=step.split('        run: |\n',1)[1]
        script='\n'.join(line[10:] for line in script.splitlines())
        with tempfile.TemporaryDirectory() as folder:
            temp=Path(folder);capture=temp/'captured';output=temp/'output';helper=temp/'python3'
            helper.write_text('#!/usr/bin/env bash\nprintf "%s\\n" "$@" > "$CAPTURE"\n')
            helper.chmod(0o700)
            env={**os.environ,'PATH':str(temp)+os.pathsep+os.environ['PATH'],'CAPTURE':str(capture),'GITHUB_OUTPUT':str(output),'GITHUB_SHA':'b'*40,'FSGG_MIRROR_CANDIDATE_SHA':'a'*40}
            result=subprocess.run(['bash','-c',script],env=env,capture_output=True,text=True,timeout=5)
            self.assertEqual(0,result.returncode,result.stderr)
            args=capture.read_text().splitlines()
            self.assertEqual('a'*40,args[args.index('--candidate-sha')+1])
            self.assertNotIn('b'*40,args)

    def test_executor_identity_preserves_checkout_and_native_merge_separately(self):
        with patch.object(mirror.guard.preflight,'git',return_value='a'*40),patch.dict(os.environ,{'GITHUB_SHA':'b'*40}):
            self.assertEqual({'executorSha':'a'*40,'workflowContextSha':'b'*40},mirror.guard.executor_identity(mirror.ROOT))
        with patch.object(mirror.guard.preflight,'git',return_value='a'*40),patch.dict(os.environ,{'GITHUB_SHA':'unknown'}),self.assertRaises(SystemExit):
            mirror.guard.executor_identity(mirror.ROOT)

    def test_actual_workflow_reads_originals_before_expensive_work_with_no_writer_permissions(self):
        root=Path(__file__).resolve().parent.parent
        text=(root/'.github/workflows/gate.yml').read_text()
        block=text.split('  gate:\n',1)[1].split('\n  api-compatibility-gate:',1)[0]
        self.assertLess(block.index('--select-original730'),block.index('name: Set up .NET'))
        self.assertIn('actions: read',block)
        self.assertNotIn('packages: write',block)
        self.assertNotIn('dotnet nuget push',block)
        self.assertIn('FS_GG_RETAINED_API_MIRROR_CANDIDATE: ${{ github.event.pull_request.head.sha || github.sha }}',block)
        generator=(root/'scripts/refresh-api-surface-mirror.fsx').read_text()
        self.assertIn('"--verify"',generator)
        self.assertIn('custodyProcess.WaitForExit(30000)',generator)
        self.assertIn('custodyProcess.ExitCode <> 0',generator)

if __name__=='__main__':unittest.main()
