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

    def read(self, _size: int):
        return b"x"


class StatusTests(unittest.TestCase):
    def test_github_token_is_sent_as_bearer(self):
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
