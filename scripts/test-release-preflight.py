#!/usr/bin/env python3
"""Focused status-semantics tests for release-preflight.py."""

from __future__ import annotations

import importlib.util
import base64
import pathlib
import sys
import unittest
import urllib.error
from unittest.mock import patch


SCRIPT = pathlib.Path(__file__).with_name("release-preflight.py")
sys.dont_write_bytecode = True
SPEC = importlib.util.spec_from_file_location("release_preflight", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
SPEC.loader.exec_module(MODULE)


class Response:
    def __init__(self, code: int):
        self.status = code

    def __enter__(self):
        return self

    def __exit__(self, *_):
        return False

    def read(self, _size: int = -1):
        return b"x"


class StatusTests(unittest.TestCase):
    def test_flat_container_archive_filename_is_lowercase(self):
        expected = "fs.gg.ui.scene.0.28.0.nupkg"
        self.assertEqual(
            expected,
            MODULE.flat_container_filename("FS.GG.UI.Scene", "0.28.0"),
        )
        self.assertNotEqual(expected, "FS.GG.UI.Scene.0.28.0.nupkg")

    def test_github_token_is_sent_as_basic_x_access_token(self):
        observed = {}

        def open_request(request, timeout):
            observed["authorization"] = request.get_header("Authorization")
            observed["timeout"] = timeout
            return Response(200)

        with patch.object(MODULE.urllib.request, "urlopen", side_effect=open_request):
            self.assertEqual(200, MODULE.status("https://feed/package", "workflow-token"))
        expected = base64.b64encode(b"x-access-token:workflow-token").decode()
        self.assertEqual(f"Basic {expected}", observed["authorization"])
        self.assertEqual(30, observed["timeout"])

    def test_200_is_existing(self):
        with patch.object(MODULE.urllib.request, "urlopen", return_value=Response(200)):
            self.assertEqual(200, MODULE.status("https://feed/package", "token"))

    def test_404_is_absent(self):
        error = urllib.error.HTTPError("https://feed/package", 404, "missing", {}, None)
        with patch.object(MODULE.urllib.request, "urlopen", side_effect=error):
            self.assertEqual(404, MODULE.status("https://feed/package", "token"))

    def test_403_remains_auth_unavailable(self):
        error = urllib.error.HTTPError("https://feed/package", 403, "forbidden", {}, None)
        with patch.object(MODULE.urllib.request, "urlopen", side_effect=error):
            self.assertEqual(403, MODULE.status("https://feed/package", "token"))

    def test_transport_unavailable_fails_closed(self):
        with patch.object(MODULE.urllib.request, "urlopen", side_effect=OSError("offline")):
            with self.assertRaises(SystemExit) as raised:
                MODULE.status("https://feed/package", "token")
        self.assertIn("feed unavailable", str(raised.exception))

    def test_historical_publisher_diagnostic_observes_v3_indexes_and_archive(self):
        service = {
            "resources": [
                {"@id": "https://feed/download/", "@type": "PackageBaseAddress/3.0.0"},
                {"@id": "https://feed/registration/", "@type": "RegistrationsBaseUrl/3.6.0"},
            ]
        }
        versions = {"versions": ["0.28.0"]}
        responses = [
            (200, __import__("json").dumps(service).encode()),
            (200, __import__("json").dumps(versions).encode()),
            (200, b"{}"),
            (403, b"forbidden"),
        ]
        with patch.object(MODULE, "request", side_effect=responses) as request:
            observed = MODULE.github_nuget_diagnostic(
                "historical-token", "release-actor", "FS.GG.UI.Scene", "0.28.0", "0.29.0"
            )
        self.assertEqual(200, observed["serviceIndex"]["status"])
        self.assertTrue(observed["versionIndex"]["baselineListed"])
        self.assertFalse(observed["versionIndex"]["targetListed"])
        self.assertEqual(200, observed["registrationIndex"]["status"])
        self.assertEqual(403, observed["baselineArchive"]["status"])
        baseline_url = "https://feed/download/fs.gg.ui.scene/0.28.0/fs.gg.ui.scene.0.28.0.nupkg"
        self.assertEqual(baseline_url, observed["baselineArchive"]["url"])
        self.assertEqual(baseline_url, request.call_args_list[3].args[0])

    def test_historical_publisher_diagnostic_survives_service_index_denial(self):
        with patch.object(MODULE, "request", side_effect=[(403, b""), (403, b""), (403, b"")]):
            observed = MODULE.github_nuget_diagnostic(
                "historical-token", "release-actor", "FS.GG.UI.Scene", "0.28.0", "0.29.0"
            )
        self.assertEqual(403, observed["serviceIndex"]["status"])
        self.assertEqual(403, observed["versionIndex"]["status"])
        self.assertEqual("not-advertised", observed["registrationIndex"]["status"])
        self.assertEqual(403, observed["baselineArchive"]["status"])

    def test_workflow_runs_authenticated_preflight_before_cut(self):
        root = SCRIPT.parent.parent
        release = (root / ".github/workflows/release.yml").read_text()
        tags = (root / ".github/workflows/release-tags.yml").read_text()
        block = release[release.index("  publication-preflight:"):release.index("  publish-packages:")]
        self.assertIn("packages: write", block)
        self.assertIn("id-token: write", block)
        self.assertIn("uses: NuGet/login@v1", block)
        self.assertIn("uses: actions/create-github-app-token@v2", block)
        self.assertIn("permission-packages: read", block)
        self.assertIn("FSGG_PACKAGE_READ_TOKEN", block)
        self.assertIn("FSGG_HISTORICAL_PUBLISH_TOKEN", block)
        self.assertIn("FSGG_HISTORICAL_PUBLISH_ACTOR", block)
        self.assertIn("--github-workflow-token-env FSGG_HISTORICAL_PUBLISH_TOKEN", block)
        self.assertIn('--github-workflow-username "$FSGG_HISTORICAL_PUBLISH_ACTOR"', block)
        self.assertIn("github-packages-auth-diagnostic.json", block)
        self.assertIn('--github-installation-id "$FSGG_PACKAGE_READ_INSTALLATION_ID"', block)
        self.assertIn('--github-repository "$GITHUB_REPOSITORY"', block)
        self.assertIn('--github-workflow-ref "$GITHUB_WORKFLOW_REF"', block)
        self.assertIn('--github-run-id "$GITHUB_RUN_ID"', block)
        self.assertIn("release-preflight.py", block)
        self.assertNotIn("dotnet nuget push", block)
        self.assertIn("needs: [plan, validate]", tags)
        self.assertIn("validate-only: true", tags)
        self.assertIn("source-sha: ${{ github.sha }}", tags)
        validate = tags[tags.index("  validate:"):tags.index("  # #681 — THE PUSH")]
        self.assertIn("secrets: inherit", validate)


if __name__ == "__main__":
    unittest.main()
