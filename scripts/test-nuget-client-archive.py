#!/usr/bin/env python3
"""Focused failure and mutation tests for supported NuGet-client archive readback."""

from __future__ import annotations

import importlib.util
import json
import pathlib
import sys
import tempfile
import unittest
import urllib.error
import zipfile
from unittest.mock import patch


SCRIPT = pathlib.Path(__file__).with_name("nuget-client-archive.py")
sys.dont_write_bytecode = True
SPEC = importlib.util.spec_from_file_location("nuget_client_archive", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
SPEC.loader.exec_module(MODULE)


class Response:
    def __init__(self, body: bytes):
        self.status = 200
        self.body = body

    def __enter__(self):
        return self

    def __exit__(self, *_):
        return False

    def read(self):
        return self.body


class NuGetClientArchiveTests(unittest.TestCase):
    def archive(self, path: pathlib.Path, package_id: str, version: str):
        with zipfile.ZipFile(path, "w") as output:
            output.writestr(
                f"{package_id}.nuspec",
                f"<package><metadata><id>{package_id}</id><version>{version}</version></metadata></package>",
            )

    def test_cache_path_uses_lowercase_flat_container_filename(self):
        path = MODULE.archive_path(pathlib.Path("cache"), "FS.GG.UI.Scene", "0.28.0")
        self.assertEqual(
            pathlib.Path("cache/fs.gg.ui.scene/0.28.0/fs.gg.ui.scene.0.28.0.nupkg"), path
        )

    def test_archive_identity_and_version_are_verified(self):
        with tempfile.TemporaryDirectory() as value:
            path = pathlib.Path(value) / "package.nupkg"
            self.archive(path, "FS.GG.UI.Scene", "0.28.0")
            self.assertEqual(64, len(MODULE.verify_archive(path, "FS.GG.UI.Scene", "0.28.0")))
            with self.assertRaises(MODULE.ArchiveError):
                MODULE.verify_archive(path, "FS.GG.UI.Canvas", "0.28.0")
            with self.assertRaises(MODULE.ArchiveError):
                MODULE.verify_archive(path, "FS.GG.UI.Scene", "0.29.0")

    def test_restore_inputs_force_fs_gg_to_github_and_use_package_download(self):
        with tempfile.TemporaryDirectory() as value:
            root = pathlib.Path(value)
            project, config = MODULE.write_restore_inputs(
                root,
                ["FS.GG.UI.Scene"],
                "0.28.0",
                "https://nuget.pkg.github.com/FS-GG/index.json",
                "release-actor",
                "secret-token",
            )
            self.assertIn('PackageDownload Include="FS.GG.UI.Scene" Version="[0.28.0]"', project.read_text())
            self.assertIn("<TargetFramework>net10.0</TargetFramework>", project.read_text())
            config_text = config.read_text()
            self.assertIn('packageSource key="github"', config_text)
            self.assertIn('package pattern="FS.GG.*"', config_text)
            self.assertIn('ClearTextPassword" value="secret-token"', config_text)
            self.assertEqual(0o600, config.stat().st_mode & 0o777)

    def test_index_distinguishes_present_absent_and_unavailable(self):
        present = Response(json.dumps({"versions": ["0.28.0"]}).encode())
        with patch.object(MODULE.urllib.request, "urlopen", return_value=present):
            self.assertTrue(
                MODULE.version_present("https://feed/{id_lower}/index.json", "FS.GG.UI.Scene", "0.28.0", "actor", "token")
            )
        missing = urllib.error.HTTPError("https://feed/index.json", 404, "missing", {}, None)
        with patch.object(MODULE.urllib.request, "urlopen", side_effect=missing):
            self.assertFalse(
                MODULE.version_present("https://feed/{id_lower}/index.json", "FS.GG.UI.Scene", "0.29.0", "actor", "token")
            )
        forbidden = urllib.error.HTTPError("https://feed/index.json", 403, "forbidden", {}, None)
        with patch.object(MODULE.urllib.request, "urlopen", side_effect=forbidden):
            with self.assertRaises(MODULE.ArchiveError):
                MODULE.version_present("https://feed/{id_lower}/index.json", "FS.GG.UI.Scene", "0.29.0", "actor", "token")


if __name__ == "__main__":
    unittest.main()
