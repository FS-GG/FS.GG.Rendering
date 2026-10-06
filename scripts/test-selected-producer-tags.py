#!/usr/bin/env python3
"""Pure tag-operation races and actual workflow admission controls; no remote effects."""
import copy
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.dont_write_bytecode = True
SPEC = importlib.util.spec_from_file_location('selected_tags', Path(__file__).with_name('selected-producer-tags.py'))
tags = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(tags)
ROOT = Path(__file__).resolve().parent.parent
EXECUTOR = 'a' * 40
ENV = {'FSGG_EXPECTED_EXECUTOR_SHA': EXECUTOR, 'GITHUB_SHA': EXECUTOR,
       'GITHUB_WORKFLOW_SHA': EXECUTOR, 'GITHUB_REPOSITORY': 'FS-GG/FS.GG.Rendering',
       'GITHUB_WORKFLOW_REF': tags.WORKFLOW, 'GITHUB_REF': 'refs/heads/main',
       'GITHUB_EVENT_NAME': 'workflow_dispatch', 'GITHUB_RUN_ID': '123',
       'GITHUB_RUN_ATTEMPT': '1'}


class FakeGit:
    def __init__(self, prefix=0):
        self.refs = {ref: tags.PRODUCER for ref in tags.REFS[:prefix]}
        self.calls, self.pushes = [], []
        self.head = self.main = EXECUTOR
        self.origin = tags.ORIGINS[1]
        self.on_call = lambda args: None

    def __call__(self, *args):
        self.calls.append(args)
        self.on_call(args)
        if args[0] == 'rev-parse':
            return self.head
        if args[0] == 'diff':
            return ''
        if args[0] == 'remote':
            return self.origin
        if args[0] == 'ls-remote':
            if args[2] == 'refs/heads/main':
                return self.main + '\trefs/heads/main'
            return '\n'.join(self.refs[ref] + '\t' + ref for ref in args[2:] if ref in self.refs)
        if args[0] == 'push':
            self.pushes.append(args)
            sha, ref = args[-1].split(':')
            self.refs[ref] = sha
            return ''
        raise AssertionError(f'unexpected git command: {args}')


def check_workflow(text):
    """Check literal dependencies and event exclusions, not general YAML semantics."""
    def need(condition, message):
        if not condition:
            raise ValueError(message)
    jobs = dict(re.findall(r'^  ([a-z][a-z-]*):\n(.*?)(?=^  [a-z][a-z-]*:|\Z)',
                           text.split('\njobs:\n', 1)[1], re.M | re.S))
    need(set(jobs) == {'selected-tag-plan', 'selected-tag-cut', 'plan', 'validate',
                       'cut', 'release', 'notify-templates'}, 'unexpected/missing job graph')
    dependencies = {'selected-tag-plan': [], 'selected-tag-cut': ['selected-tag-plan'],
                    'plan': [], 'validate': ['plan'], 'cut': ['plan', 'validate'],
                    'release': ['plan', 'cut'], 'notify-templates': ['plan', 'cut', 'release']}
    for job, expected in dependencies.items():
        values = re.findall(r'^    needs: (.+)$', jobs[job], re.M)
        actual = [part.strip() for part in values[0].strip('[]').split(',')] if values else []
        need(len(values) <= 1 and actual == expected, f'{job}: literal dependencies differ')
    dispatch_if = "github.repository == 'FS-GG/FS.GG.Rendering' && github.event_name == 'workflow_dispatch'"
    for job in ('selected-tag-plan', 'selected-tag-cut'):
        block = jobs[job]
        need(re.findall(r'^    if: (.+)$', block, re.M) == [dispatch_if], f'{job}: dispatch-only condition differs')
        need('    timeout-minutes: 6\n' in block, f'{job}: missing finite job bound')
        need('ref: ${{ github.sha }}' in block, f'{job}: checkout must use event SHA')
        need('FSGG_EXPECTED_EXECUTOR_SHA: ${{ inputs.expected-executor-sha }}' in block, f'{job}: missing executor input')
        need('SELECTED_SOURCE_SHA: ${{ inputs.source-sha }}' in block and
             'SELECTED_VERSION: ${{ inputs.version }}' in block, f'{job}: missing subject inputs')
        uses = re.findall(r'^\s+(?:- )?uses: (.+)$', block, re.M)
        need(uses == ['actions/checkout@v7', 'actions/upload-artifact@v7'],
             f'{job}: unexpected action outside checkout and evidence upload')
        need(not re.search(r'\b(dotnet|gh|curl|git push|secrets\.|id-token|packages:|create-github-app-token)\b', block),
             f'{job}: unexpected authority/native command')
    need('      contents: read\n' in jobs['selected-tag-plan'], 'tag plan is not read only')
    need('persist-credentials: false' in jobs['selected-tag-plan'], 'tag plan persists credentials')
    need('      contents: write\n' in jobs['selected-tag-cut'], 'tag cut grant missing')
    need('persist-credentials: true' in jobs['selected-tag-cut'], 'tag cut must use checkout GITHUB_TOKEN')
    need('--mode check --source-sha "$SELECTED_SOURCE_SHA" --version "$SELECTED_VERSION"' in jobs['selected-tag-plan'], 'wrong plan command')
    need('--mode cut --source-sha "$SELECTED_SOURCE_SHA" --version "$SELECTED_VERSION"' in jobs['selected-tag-cut'], 'wrong cut command')
    need('run: timeout --kill-after=10s 60s python3 scripts/test-selected-producer-tags.py' in jobs['selected-tag-plan'], 'pure controls must precede effects')
    need(re.findall(r'^    if: (.+)$', jobs['plan'], re.M) ==
         ["github.repository == 'FS-GG/FS.GG.Rendering' && github.event_name == 'push'"], 'ordinary plan must exclude dispatch')
    for job, condition in [('validate', "needs.plan.outputs.has-pending == 'true'"),
                           ('cut', "needs.plan.outputs.has-pending == 'true'"),
                           ('release', "needs.plan.outputs.release-tag == 'true'"),
                           ('notify-templates', "needs.plan.outputs.template-tag == 'true'")]:
        need(re.findall(r'^    if: (.+)$', jobs[job], re.M) == [condition], f'{job}: original success condition differs')
    need('uses: ./.github/workflows/release.yml' in jobs['release'], 'original publication call changed')
    need('uses: ./.github/workflows/template-dispatch.yml' in jobs['notify-templates'], 'original postpublication notification changed')
    need('  group: release-tags\n  cancel-in-progress: false' in text, 'tag serialization changed')
    return jobs


class TagTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.git = FakeGit()
        self.binding_calls = 0

    def binding(self, root, source, version):
        self.binding_calls += 1
        self.assertEqual(tags.PRODUCER, source)
        self.assertEqual(tags.VERSION, version)
        return {'attemptReady': True, 'producerSha': source}

    def operation(self, mode='cut', env=None, binding=None):
        return tags.Operation(self.root, mode, tags.PRODUCER, tags.VERSION,
                              ENV if env is None else env, self.git, binding or self.binding)

    def test_check_only_probes_without_push(self):
        result = self.operation('check').run()
        self.assertEqual('passed', result['result'])
        self.assertEqual([], self.git.pushes)
        self.assertIn('all-three-before-effects', [x['stage'] for x in result['events']])

    def test_cut_all_absent_in_exact_order_with_readback(self):
        result = self.operation().run()
        self.assertEqual([f'{tags.PRODUCER}:{ref}' for ref in tags.REFS], [x[-1] for x in self.git.pushes])
        self.assertTrue(all(x[1:-1] == ('--no-follow-tags', '--recurse-submodules=no', '--porcelain', 'origin') for x in self.git.pushes))
        events = result['events']
        self.assertEqual(3, sum(x['stage'] == 'immediate-readback-matched' for x in events))
        self.assertEqual(5, self.binding_calls)
        self.assertEqual('all-three-before-effects', events[2]['stage'])
        for index, call in enumerate(self.git.calls):
            if call[0] == 'push':
                self.assertEqual('ls-remote', self.git.calls[index+1][0])
        self.assertEqual('not-performed', result['publication'])
        self.assertEqual('not-performed', result['notification'])

    def test_matching_prefix_preserved_and_only_missing_suffix_pushed(self):
        for prefix in range(1, 4):
            with self.subTest(prefix=prefix):
                self.git = FakeGit(prefix)
                self.operation().run()
                self.assertEqual([f'{tags.PRODUCER}:{ref}' for ref in tags.REFS[prefix:]], [x[-1] for x in self.git.pushes])

    def test_conflicting_annotated_or_out_of_order_triple_stops_before_any_push(self):
        cases = [{tags.REFS[2]: 'b'*40}, {tags.REFS[0]: tags.PRODUCER, tags.REFS[0]+'^{}': tags.PRODUCER},
                 {tags.REFS[2]: tags.PRODUCER}, {tags.REFS[1]: tags.PRODUCER}]
        for refs in cases:
            with self.subTest(refs=refs):
                self.git = FakeGit()
                self.git.refs = refs
                with self.assertRaises(tags.Refused):
                    self.operation().run()
                self.assertEqual([], self.git.pushes)

    def test_wrong_executor_context_refuses_before_remote_probe(self):
        for key in ENV:
            with self.subTest(key=key):
                env = {**ENV, key: 'wrong'}
                self.git = FakeGit()
                with self.assertRaises(tags.Refused):
                    self.operation(env=env).run()
                self.assertEqual([], self.git.calls)

    def test_checkout_main_origin_and_dirty_source_refuse(self):
        for problem in ('head', 'main', 'origin', 'dirty'):
            with self.subTest(problem=problem):
                self.git = FakeGit()
                if problem == 'dirty':
                    def dirty(args):
                        if args[0] == 'diff':
                            raise tags.Refused('dirty source')
                    self.git.on_call = dirty
                else:
                    setattr(self.git, problem, 'b'*40)
                with self.assertRaises(tags.Refused):
                    self.operation().run()
                self.assertEqual([], self.git.pushes)

    def test_binding_unknown_blocks_all_effects(self):
        def failed(*args):
            raise SystemExit('selected receiver identity differs')
        with self.assertRaisesRegex(tags.Refused, 'receiver identity'):
            self.operation(binding=failed).run()
        self.assertEqual([], self.git.pushes)

    def test_change_between_initial_plan_and_first_effect_is_reconciled(self):
        for problem in ('main', 'tag', 'disappeared'):
            with self.subTest(problem=problem):
                self.git = FakeGit(1 if problem == 'disappeared' else 0)
                checks = 0
                def change(args):
                    nonlocal checks
                    if args == ('rev-parse', 'HEAD'):
                        checks += 1
                        if checks == 2:
                            if problem == 'main': self.git.main = 'b'*40
                            elif problem == 'tag': self.git.refs[tags.REFS[2]] = 'b'*40
                            else: self.git.refs.clear()
                self.git.on_call = change
                with self.assertRaises(tags.Refused):
                    self.operation().run()
                self.assertEqual([], self.git.pushes)

    def test_change_after_first_tag_stops_remaining_effects(self):
        for problem in ('main', 'tag', 'binding', 'disappeared'):
            with self.subTest(problem=problem):
                self.git = FakeGit()
                def change(args):
                    if self.git.pushes and args == ('rev-parse', 'HEAD'):
                        if problem == 'main': self.git.main = 'b'*40
                        elif problem == 'tag': self.git.refs[tags.REFS[2]] = 'b'*40
                        elif problem == 'disappeared': self.git.refs.clear()
                def binding(*args):
                    if self.git.pushes and problem == 'binding':
                        raise SystemExit('attempt changed')
                    return self.binding(*args)
                self.git.on_call = change
                with self.assertRaises(tags.Refused):
                    self.operation(binding=binding).run()
                self.assertEqual(1, len(self.git.pushes))

    def test_failed_push_ack_gets_one_readonly_observation_and_stops(self):
        for applied in (False, True):
            with self.subTest(applied=applied):
                self.git = FakeGit()
                def uncertain(args):
                    if args[0] == 'push':
                        if applied: self.git.refs[tags.REFS[0]] = tags.PRODUCER
                        raise tags.Refused('push acknowledgement unknown')
                self.git.on_call = uncertain
                operation = self.operation()
                with self.assertRaisesRegex(tags.Refused, 'acknowledgement unknown'):
                    operation.run()
                requested = [i for i, args in enumerate(self.git.calls) if args[0] == 'push']
                self.assertEqual(1, len(requested))
                self.assertEqual(1, len(self.git.calls) - requested[0] - 1)
                self.assertEqual('ls-remote', self.git.calls[-1][0])
                self.assertEqual('refused-or-unknown', json.loads(operation.path.read_text())['result'])

    def test_missing_or_mismatching_readback_stops_after_first_push(self):
        for value in (None, 'b'*40):
            with self.subTest(value=value):
                self.git = FakeGit()
                def disappear(args):
                    if self.git.pushes and args[0] == 'ls-remote':
                        self.git.refs = {} if value is None else {tags.REFS[0]: value}
                self.git.on_call = disappear
                with self.assertRaises(tags.Refused): self.operation().run()
                self.assertEqual(1, len(self.git.pushes))

    def test_transport_unknown_is_not_absence(self):
        def unavailable(args):
            if args[0] == 'ls-remote': raise tags.Refused('transport timeout')
        self.git.on_call = unavailable
        with self.assertRaisesRegex(tags.Refused, 'transport timeout'): self.operation().run()
        self.assertEqual([], self.git.pushes)

    def test_malformed_duplicate_and_unrequested_refs_refuse(self):
        for value in ['nonsense', f'{tags.PRODUCER}\trefs/heads/other',
                      '\n'.join([f'{tags.PRODUCER}\t{tags.REFS[0]}']*2)]:
            with self.subTest(value=value), self.assertRaises(tags.Refused):
                tags.parse_refs(value, set(tags.REFS))

    def test_git_missing_timeout_nonzero_and_deadline_refuse_without_output_leak(self):
        failures = [FileNotFoundError('secret'), subprocess.TimeoutExpired('git secret', 1),
                    subprocess.CompletedProcess(['git'], 1, stdout='secret', stderr='secret')]
        for failure in failures:
            with self.subTest(failure=type(failure).__name__):
                runner = tags.Git(self.root)
                kwargs = {'side_effect': failure} if isinstance(failure, Exception) else {'return_value': failure}
                with patch.object(tags.subprocess, 'run', **kwargs), self.assertRaises(tags.Refused) as exc:
                    runner('ls-remote', 'origin', 'refs/heads/main')
                self.assertNotIn('secret', str(exc.exception))
        runner.deadline = 0
        with patch.object(tags.subprocess, 'run', side_effect=AssertionError('budget exhausted')):
            with self.assertRaisesRegex(tags.Refused, 'budget'): runner('ls-remote', 'origin')

    def test_git_transport_has_child_timeout_and_output_cap(self):
        runner = tags.Git(self.root)
        result = subprocess.CompletedProcess(['git'], 0, stdout='a'*16385, stderr='')
        with patch.object(tags.subprocess, 'run', return_value=result) as run:
            with self.assertRaisesRegex(tags.Refused, 'output limit'):
                runner('ls-remote', 'origin', 'refs/heads/main')
        args, kwargs = run.call_args
        self.assertEqual(['timeout', '--kill-after=2s'], args[0][:2])
        self.assertLessEqual(kwargs['timeout'], 25)
        self.assertEqual('0', kwargs['env']['GIT_TERMINAL_PROMPT'])

    def test_actual_source_binding_and_immutable_plan_are_reused(self):
        binding = tags.selected_binding(ROOT, tags.PRODUCER, tags.VERSION)
        self.assertTrue(binding['attemptReady'])
        self.assertEqual(37419955679, binding['producerRun'])
        self.assertEqual(37428584026, binding['qualification']['run'])
        self.assertFalse(json.loads(tags.guard.source_bytes(ROOT, tags.PRODUCER, tags.PLAN))['publicationReady'])
        for source, version in [('b'*40, tags.VERSION), (tags.PRODUCER, '0.32.0')]:
            with self.assertRaises(tags.Refused): tags.selected_binding(ROOT, source, version)

    def test_actual_binding_refuses_readiness_candidate_receiver_and_plan_mutants(self):
        original = json.loads((ROOT/tags.guard.ATTEMPT).read_text())
        plan_bytes = tags.guard.source_bytes(ROOT, tags.PRODUCER, tags.PLAN)
        path = self.root/tags.guard.ATTEMPT
        path.parent.mkdir(parents=True)
        for mutation in ('readiness', 'candidate', 'receiver', 'plan'):
            with self.subTest(mutation=mutation):
                binding = copy.deepcopy(original)
                if mutation == 'readiness': binding['attemptReady'] = False
                elif mutation == 'candidate': binding['producerArtifact'] += 1
                elif mutation == 'receiver': binding['qualification']['run'] += 1
                path.write_text(json.dumps(binding))
                with patch.object(tags.guard, 'source_bytes', return_value=plan_bytes+(b' ' if mutation == 'plan' else b'')):
                    with self.assertRaises(SystemExit): tags.selected_binding(self.root, tags.PRODUCER, tags.VERSION)

    def test_actual_workflow_graph_and_known_bad_controls(self):
        text = (ROOT/'.github/workflows/release-tags.yml').read_text()
        check_workflow(text)
        mutants = [text.replace("github.event_name == 'push'", "github.event_name == 'workflow_dispatch'"),
                   text.replace('needs: selected-tag-plan', 'needs: plan'),
                   text.replace('needs: [plan, cut, release]', 'needs: [plan, cut]'),
                   text.replace('cancel-in-progress: false', 'cancel-in-progress: true'),
                   text.replace('--mode check', '--mode cut'),
                   text.replace('persist-credentials: false', 'persist-credentials: true'),
                   text.replace('    timeout-minutes: 6', '    timeout-minutes: 60'),
                   text.replace("github.event_name == 'workflow_dispatch'", 'always()')]
        for index, mutant in enumerate(mutants):
            with self.subTest(mutant=index), self.assertRaises(ValueError): check_workflow(mutant)


if __name__ == '__main__':
    unittest.main()
