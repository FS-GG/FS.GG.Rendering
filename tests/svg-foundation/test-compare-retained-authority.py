#!/usr/bin/env python3
"""Offline guard fixtures; these are not installed-compiler qualification evidence."""
import importlib.util
import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

sys.dont_write_bytecode = True

directory = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("compare_authority", directory / "compare-retained-authority.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
frozen = directory.parent.parent / "readiness/svg-qual-01-2"


class AuthorityGuardTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory()
        self.addCleanup(self.scratch.cleanup)
        self.current = Path(self.scratch.name) / "current"
        shutil.copytree(frozen, self.current)
        self.authority = self.current / "typed-authority.json"
        data = json.loads(self.authority.read_text())
        data["packageIdentity"] = "FS.GG.SDD.Artifacts/2.1.0"
        self.authority.write_text(json.dumps(data))

    def test_only_selected_identity_may_differ(self):
        module.compare(frozen, self.current)

    def test_wrong_package_and_profile_refuse(self):
        for key, value in (("packageIdentity", "FS.GG.SDD.Artifacts/2.1.00"),
                           ("profileIdentity", "fsgg-quint-profile/1"),
                           ("toolchainIdentity", "changed")):
            with self.subTest(key=key):
                original = self.authority.read_text()
                data = json.loads(original)
                data[key] = value
                self.authority.write_text(json.dumps(data))
                with self.assertRaises(ValueError):
                    module.compare(frozen, self.current)
                self.authority.write_text(original)

    def test_model_contract_receipt_and_bindings_changes_refuse(self):
        for name in ("retainedInteraction.qnt", "contract.json", "receipt.json", "bindings.fs"):
            with self.subTest(name=name):
                path = self.current / "quint" / name
                original = path.read_bytes()
                path.write_bytes(original + b"\nchanged")
                with self.assertRaises(ValueError):
                    module.compare(frozen, self.current)
                path.write_bytes(original)

    def test_missing_and_extra_files_refuse(self):
        extra = self.current / "unexpected.json"
        extra.write_text("{}")
        with self.assertRaises(ValueError):
            module.compare(frozen, self.current)
        extra.unlink()
        (self.current / "quint/receipt.json").unlink()
        with self.assertRaises(ValueError):
            module.compare(frozen, self.current)


if __name__ == "__main__":
    unittest.main()
