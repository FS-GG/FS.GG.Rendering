#!/usr/bin/env python3

import importlib.util
import json
import argparse
import base64
import shutil
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("release-custody.py")
sys.dont_write_bytecode = True
SPEC = importlib.util.spec_from_file_location("release_custody", SCRIPT)
custody = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
SPEC.loader.exec_module(custody)


class ReleaseCustodyTests(unittest.TestCase):
    def test_readback_flat_container_filename_is_lowercase(self):
        record = {
            "id": "FS.GG.UI.Scene",
            "version": "0.28.0",
            "file": "FS.GG.UI.Scene.0.28.0.nupkg",
        }
        template = "https://feed/{id_lower}/{version}/{filename}"
        expected = "https://feed/fs.gg.ui.scene/0.28.0/fs.gg.ui.scene.0.28.0.nupkg"
        self.assertEqual(expected, custody.render_url(template, record))
        self.assertNotIn("FS.GG.UI.Scene.0.28.0.nupkg", custody.render_url(template, record))

    def test_authenticated_readback_uses_app_token_basic_auth(self):
        observed = {}

        class Response:
            def __enter__(self):
                return self

            def __exit__(self, *_):
                return False

            def read(self):
                return b"archive"

        def open_request(request, timeout):
            observed["authorization"] = request.get_header("Authorization")
            observed["timeout"] = timeout
            return Response()

        with tempfile.TemporaryDirectory() as value:
            destination = Path(value) / "package.nupkg"
            with patch.object(custody.urllib.request, "urlopen", side_effect=open_request):
                self.assertEqual(200, custody.download("https://feed/package", destination, "app-token"))
            self.assertEqual(b"archive", destination.read_bytes())
        expected = base64.b64encode(b"x-access-token:app-token").decode()
        self.assertEqual(f"Basic {expected}", observed["authorization"])
        self.assertEqual(60, observed["timeout"])

    def archive(self, path: Path, package: str, version: str, entries=None, signature=None, dependencies=None):
        entries = entries or {}
        deps = "".join(f'<dependency id="{key}" version="{value}" />' for key, value in (dependencies or []))
        xml = f'''<package><metadata><id>{package}</id><version>{version}</version><license type="expression">MIT</license><repository url="https://github.com/FS-GG/FS.GG.Rendering.git" commit="{'a' * 40}" /><dependencies>{deps}</dependencies></metadata></package>'''
        with zipfile.ZipFile(path, "w") as output:
            output.writestr(f"{package}.nuspec", xml)
            for name, value in entries.items():
                output.writestr(name, value)
            if signature is not None:
                output.writestr(".signature.p7s", signature)

    def test_readback_allows_only_feed_signature(self):
        with tempfile.TemporaryDirectory() as value:
            root = Path(value)
            local, remote = root / "local.nupkg", root / "remote.nupkg"
            payload = {"lib/net10.0/a.dll": b"same"}
            self.archive(local, "FS.GG.UI.A", "0.29.0", payload)
            self.archive(remote, "FS.GG.UI.A", "0.29.0", payload, b"feed-added")
            custody.compare_archive(local, remote)

    def test_readback_rejects_payload_mutation(self):
        with tempfile.TemporaryDirectory() as value:
            root = Path(value)
            local, remote = root / "local.nupkg", root / "remote.nupkg"
            self.archive(local, "FS.GG.UI.A", "0.29.0", {"lib/a.dll": b"one"})
            self.archive(remote, "FS.GG.UI.A", "0.29.0", {"lib/a.dll": b"two"}, b"signature")
            with self.assertRaises(custody.CustodyError):
                custody.compare_archive(local, remote)

    def test_readback_rejects_missing_or_extra_payload(self):
        with tempfile.TemporaryDirectory() as value:
            root = Path(value)
            local, remote = root / "local.nupkg", root / "remote.nupkg"
            self.archive(local, "FS.GG.UI.A", "0.29.0", {"lib/a.dll": b"one"})
            self.archive(remote, "FS.GG.UI.A", "0.29.0", {"lib/b.dll": b"one"})
            with self.assertRaises(custody.CustodyError):
                custody.compare_archive(local, remote)

    def test_plan_requires_complete_17_plus_bom_plus_template_roster(self):
        plan = {"schema": "fsgg.rendering.release-plan/v1", "baselineVersion": "0.28.0", "packages": []}
        with self.assertRaises(custody.CustodyError):
            custody.validate_plan(plan)

    def test_workflow_retains_before_push_and_never_deletes_release_tags(self):
        repo = SCRIPT.parent.parent
        release = (repo / ".github/workflows/release.yml").read_text(encoding="utf-8")
        tags = (repo / ".github/workflows/release-tags.yml").read_text(encoding="utf-8")
        retained = release.index("uses: actions/upload-artifact@v7")
        first_push = release.index("dotnet nuget push", retained)
        self.assertLess(retained, first_push)
        self.assertIn("release-custody.py probe", release)
        publish = release[release.index("  publish-packages:"):]
        self.assertIn("nuget-client-archive.py probe", publish)
        self.assertIn("nuget-client-archive.py restore", publish)
        self.assertIn("release-custody.py compare", publish)
        self.assertIn("--token-env GITHUB_TOKEN", publish)
        self.assertIn('--api-key "$GITHUB_TOKEN"', publish)
        packed_template = release[release.index("Packed template clean-checkout FSI contract (#1010)"):]
        self.assertIn('user_config="$HOME/.nuget/NuGet/NuGet.Config"', packed_template)
        self.assertIn("<configuration />", packed_template)
        self.assertIn('--configfile "$user_config"', packed_template)
        nuget_replay = release[release.index("Replay the same original custody bytes to nuget.org and read them back"):]
        self.assertIn("--skip-duplicate", nuget_replay)
        self.assertIn("for attempt in {1..180}", nuget_replay)
        self.assertIn("Waiting for nuget.org propagation", nuget_replay)
        self.assertIn("release-custody.py probe", nuget_replay)
        self.assertNotIn("rollback-failed-cut:", tags)
        self.assertNotIn("git push origin --delete", tags)

    def test_manifest_verification_detects_archive_replacement(self):
        with tempfile.TemporaryDirectory() as value:
            path = Path(value) / "a.nupkg"
            self.archive(path, "FS.GG.UI.A", "0.29.0", {"lib/a.dll": b"one"})
            before = custody.archive_record(path)
            self.archive(path, "FS.GG.UI.A", "0.29.0", {"lib/a.dll": b"two"})
            self.assertNotEqual(before, custody.archive_record(path))

    def test_interrupted_dual_feed_resumes_only_original_retained_bytes(self):
        """Seven packages on feed A models interruption; replay completes A, then B, without repack."""
        with tempfile.TemporaryDirectory() as value:
            root = Path(value)
            archives, feed_a, feed_b = root / "archives", root / "a", root / "b"
            archives.mkdir(); feed_a.mkdir(); feed_b.mkdir()
            libraries = [f"FS.GG.UI.Library{i:02d}" for i in range(17)]
            packages = ([{"id": item, "kind": "library", "apiBaseline": "required"} for item in libraries]
                        + [{"id": "FS.GG.UI", "kind": "bom", "apiBaseline": "not-applicable"},
                           {"id": "FS.GG.UI.Template", "kind": "template", "apiBaseline": "not-applicable"}])
            packages[0]["apiBaseline"] = "first-publication"
            plan = {
                "schema": "fsgg.rendering.release-plan/v1", "version": "0.29.0", "baselineVersion": "0.28.0",
                "tags": ["fs-gg-ui/v0.29.0", "fs-gg-ui-template/v0.29.0", "v0.29.0"], "packages": packages,
                "releaseChecks": {"svgPackage": libraries[0],
                    "fableEntries": ["fable/FS.GG.UI.Scene.SvgBrowser.fsproj", "fable/SvgBrowser.fs", "fable/SvgBrowser.fsi", "fable-compatibility/compatibility-profile.v1.json"],
                    "svgSurfaceMarkers": ["SvgDocumentBrowserHost", "RetainedScene", "mountDocument"],
                    "generatedTemplateEntries": ["content/.template.config/template.json", "content/template/base/Directory.Packages.props"]}}
            plan_path = root / "plan.json"; plan_path.write_text(json.dumps(plan))
            for package in packages:
                item = package["id"]
                entries = {"lib/net10.0/value.dll": item.encode()}
                dependencies = []
                if item == libraries[0]:
                    entries.update({name: b"SvgDocumentBrowserHost RetainedScene mountDocument" for name in plan["releaseChecks"]["fableEntries"]})
                    entries["api-surface/SvgBrowser.fsi"] = b"SvgDocumentBrowserHost RetainedScene mountDocument"
                elif item == "FS.GG.UI":
                    dependencies = [(library, "[0.29.0]") for library in libraries]
                elif item == "FS.GG.UI.Template":
                    entries.update({name: b"generated" for name in plan["releaseChecks"]["generatedTemplateEntries"]})
                self.archive(archives / f"{item}.0.29.0.nupkg", item, "0.29.0", entries, dependencies=dependencies)

            manifest_path = archives / "release-custody.json"
            common = argparse.Namespace(plan=plan_path, archives=archives, manifest=manifest_path,
                                        source_sha="a" * 40)
            custody.command_prepare(common)
            original_hashes = {p.name: custody.sha256_file(p) for p in archives.glob("*.nupkg")}

            # First attempt stopped after seven uploads to feed A. A signature is added by the feed.
            records = custody.load_json(manifest_path)["archives"]
            for record in records[:7]:
                meta, entries = custody.nuspec(archives / record["file"])
                self.archive(feed_a / record["file"], meta["id"], meta["version"],
                             {k: v for k, v in entries.items() if not k.endswith(".nuspec")}, b"feed-signature",
                             [(d["id"], d["version"]) for d in meta["dependencies"]])

            for feed in (feed_a, feed_b):
                for record in records:
                    local, remote = archives / record["file"], feed / record["file"]
                    if remote.exists():
                        custody.compare_archive(local, remote)
                    else:
                        # Replay is a byte copy of retained custody, never another pack.
                        shutil.copyfile(local, remote)
                    custody.compare_archive(local, remote)

            self.assertEqual(original_hashes, {p.name: custody.sha256_file(p) for p in archives.glob("*.nupkg")})
            # A feed payload mutation is a hard stop, even though a signature difference is allowed.
            victim = feed_b / records[0]["file"]
            self.archive(victim, records[0]["id"], "0.29.0", {"lib/net10.0/value.dll": b"mutant"})
            with self.assertRaises(custody.CustodyError):
                custody.compare_archive(archives / records[0]["file"], victim)


if __name__ == "__main__":
    unittest.main()
