"""Real isolated Git controls for producer-source/current-lock composition; no CLR/browser."""
import importlib.util
import pathlib
import subprocess
import tempfile
import unittest

HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('benchmark_source', HERE / 'hosted-benchmark-source.py')
source = importlib.util.module_from_spec(spec)
spec.loader.exec_module(source)


class OverlayControls(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.temp.name)
        self.addCleanup(self.temp.cleanup)
        self.git('init', '-q')
        self.git('config', 'user.email', 'fixture@example.invalid')
        self.git('config', 'user.name', 'Isolated fixture')
        for directory in source.PRODUCERS:
            path = self.root / directory
            path.mkdir(parents=True)
            (path / 'Producer.fs').write_text('old source\n')
            (path / 'old-only.fs').write_text('old-only source\n')
            (path / 'packages.lock.json').write_text('{"Core":"old"}\n')
        self.old = self.commit('old producer')
        for directory in source.PRODUCERS:
            path = self.root / directory
            (path / 'Producer.fs').write_text('new source\n')
            (path / 'old-only.fs').unlink()
            (path / 'new-only.fs').write_text('new-only source\n')
            (path / 'packages.lock.json').write_text('{"Core":"current"}\n')
        self.current = self.commit('current harness')

    def git(self, *args):
        return source.git(self.root, *args).decode().strip()

    def commit(self, message):
        self.git('add', '.')
        self.git('commit', '-qm', message)
        return self.git('rev-parse', 'HEAD')

    def test_all_three_locks_current_and_sources_old(self):
        result = source.overlay(self.root, self.old)
        self.assertEqual(len(result['harnessLocks']), 3)
        for directory in source.PRODUCERS:
            with self.subTest(directory=directory):
                path = self.root / directory
                self.assertEqual((path / 'Producer.fs').read_text(), 'old source\n')
                self.assertTrue((path / 'old-only.fs').exists())
                self.assertFalse((path / 'new-only.fs').exists())
                self.assertEqual((path / 'packages.lock.json').read_text(), '{"Core":"current"}\n')
                self.assertEqual(self.git('show', self.old + ':' + directory + '/packages.lock.json'), '{"Core":"old"}')

    def test_candidate_arm_preserves_current_sources_and_locks(self):
        source.overlay(self.root, self.current)
        self.assertEqual(self.git('status', '--porcelain'), '')

    def test_missing_harness_lock_refuses_before_overlay(self):
        (self.root / source.LOCKS[1]).unlink()
        self.commit('missing lock')
        before = self.git('write-tree')
        with self.assertRaises(subprocess.CalledProcessError):
            source.overlay(self.root, self.old)
        self.assertEqual(self.git('write-tree'), before)
        self.assertEqual(self.git('status', '--porcelain'), '')

    def test_dirty_producer_refuses_before_overlay(self):
        (self.root / source.PRODUCERS[0] / 'Producer.fs').write_text('uncommitted\n')
        with self.assertRaises(subprocess.CalledProcessError):
            source.overlay(self.root, self.old)
        self.assertEqual((self.root / source.PRODUCERS[0] / 'Producer.fs').read_text(), 'uncommitted\n')

    def test_malformed_revision_refuses_before_overlay(self):
        with self.assertRaises(ValueError):
            source.overlay(self.root, 'HEAD')
        self.assertEqual(self.git('status', '--porcelain'), '')


if __name__ == '__main__':
    unittest.main(verbosity=2)
