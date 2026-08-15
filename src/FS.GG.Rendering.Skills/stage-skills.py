#!/usr/bin/env python3
"""stage-skills.py — lay this repository's product skill bytes out for packing.

WHAT IT DOES. Reads the committed producer manifest
`template/skill-manifest/skill-manifest.json`, and for every `scope: product` row copies that
row's whole source directory into `<out>/skills/<id>/`, after re-deriving the row's `SKILL.md`
digest from the source bytes and refusing to stage on any mismatch. The manifest itself is
copied byte-verbatim to `<out>/skill-manifest.json`.

DERIVED, NOT RESTATED (ADR-0058). The delivered set is a one-line predicate over the manifest
(`is_delivered` below) and nothing else. There is no second list of skill ids anywhere in this
package, so a row added to the manifest flows into the nupkg with no edit here — which is what
`verify-package.sh` step 2 measures with a synthetic row.

WHY THE SET COMES FROM THE MANIFEST AND NOT FROM A DIRECTORY GLOB. The obvious spelling is
`template/product-skills/*/`, and it is wrong: 3 of this repository's 18 product rows are
sourced from somewhere else entirely —

    fs-gg-feedback-report   template/feedback-report/skill/
    fs-gg-samples           template/fragments/samples/skill/
    fs-gg-project           template/base/.agents/skills/fs-gg-project/

A glob silently ships 15 of 18, and the one it is most important not to miss is
`fs-gg-feedback-report`, whose predicate is `always` and whose absence from a product tree is
the defect this package exists to close (.github#2380 -> #2545). The manifest already carries a
per-row `supplied-by`, so joining that is both simpler and complete. `.github#298` flagged the
`feedback-report` path anomaly years ago without tracing its consequence; the consequence is
exactly this, and reading `supplied-by` is what removes it.

WHY THE WHOLE DIRECTORY AND NOT ONLY `SKILL.md`. FS.GG.Game's equivalent stager copies one file
per row, which is correct for FS.GG.Game because its rows are single-file. Three of this
repository's rows are not:

    fs-gg-symbology         reference.fsx, reference/labels.md
    fs-gg-symbol-design     reference.fsx, reference/candidates.md
    fs-gg-feedback-report   scripts/feedback-tool.fsx, scripts/FeedbackReportTool.fs

ADR-0014 clause 4 requires an emitted skill body to be SELF-CONTAINED — "a guard rejects any
shipped skill whose body references a path that does not exist in the product tree". Shipping
`fs-gg-feedback-report`'s body without `scripts/feedback-tool.fsx` would satisfy the letter of
the digest requirement and deliver a skill that cannot run the tool it documents, which is the
very failure that opened this investigation. So the unit of delivery is the row's directory.
The `.template.config/template.json` rows agree: every skill source is emitted with
`copyOnly: ["**/*"]`, i.e. the whole tree, verbatim.

THE DIGEST IS THIS REPOSITORY'S, NOT FS.GG.Game's. `canonical_digest` folds CRLF to LF as well
as stripping a BOM, because `scripts/generate-skill-manifest.fsx` and the vendored
`Fsgg.SkillMirror` both do. FS.GG.Game's helper strips the BOM only; copying it verbatim would
agree on an LF checkout and disagree on a CRLF one — a difference that would not show up here
and would show up in someone else's clone.

Only `SKILL.md` is content-addressed against the manifest, because `SKILL.md` is the only thing
the manifest records a digest for. Sidecars are copied and their presence is asserted by
`verify-package.sh` step 3; they are not silently digest-checked against a record that does not
exist.

Exit codes: 0 staged, 2 on any misconfiguration. Pure stdlib, no network.
"""

import hashlib
import json
import os
import shutil
import sys

# Repo root = three levels up (src/FS.GG.Rendering.Skills/stage-skills.py -> repo root).
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MANIFEST_PATH = os.path.join(REPO_ROOT, "template", "skill-manifest", "skill-manifest.json")

BODY_FILENAME = "SKILL.md"


def die(message: str) -> None:
    print(f"stage-skills: {message}", file=sys.stderr)
    raise SystemExit(2)


def canonical_digest(raw: bytes) -> str:
    """sha256 over the body text's UTF-8 bytes, BOM-free and LF-folded.

    Byte-parity with scripts/generate-skill-manifest.fsx:156-163 and
    template/lifecycle/skill-mirror-vendored.fs:34-46, which are this repository's two existing
    implementations of the same digest. Both fold CRLF before hashing; a BOM never enters
    because `File.ReadAllText` strips it.
    """
    if raw.startswith(b"\xef\xbb\xbf"):
        raw = raw[3:]
    return hashlib.sha256(raw.replace(b"\r\n", b"\n")).hexdigest()


def is_delivered(row: dict) -> bool:
    """The delivered class: every owner-authored product row. The only place the set is decided."""
    return row.get("scope") == "product"


def safe_relative(base: str, path: str, what: str) -> str:
    """Reject anything that would escape `base` — an absolute path or a `..` segment."""
    if os.path.isabs(path):
        die(f"{what}: '{path}' is absolute; manifest paths are repo-relative")
    parts = path.replace("\\", "/").split("/")
    if any(part == ".." for part in parts):
        die(f"{what}: '{path}' contains a '..' segment")
    resolved = os.path.realpath(os.path.join(base, *[p for p in parts if p]))
    if resolved != base and not resolved.startswith(base + os.sep):
        die(f"{what}: '{path}' resolves outside the repository")
    return resolved


def main() -> None:
    if len(sys.argv) != 2:
        die("usage: stage-skills.py <output-dir>")
    out = os.path.abspath(sys.argv[1])

    if not os.path.isfile(MANIFEST_PATH):
        die(
            "template/skill-manifest/skill-manifest.json not found "
            "(is this a FS.GG.Rendering checkout?)"
        )

    with open(MANIFEST_PATH, "rb") as handle:
        manifest_bytes = handle.read()
    try:
        manifest = json.loads(manifest_bytes.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        die(f"template/skill-manifest/skill-manifest.json is not readable JSON: {exc}")

    rows = manifest.get("skills", [])
    if not isinstance(rows, list):
        die("manifest 'skills' is not a list")

    # Regenerated every pack, so a retired row cannot linger in a stale staging dir.
    shutil.rmtree(out, ignore_errors=True)
    os.makedirs(out)

    # Byte-verbatim, never re-serialized: the packed manifest must diff clean against the
    # committed one, which verify-package.sh asserts in steps 1 and 4.
    with open(os.path.join(out, "skill-manifest.json"), "wb") as handle:
        handle.write(manifest_bytes)

    repo_root_real = os.path.realpath(REPO_ROOT)
    staged = 0

    for row in rows:
        if not is_delivered(row):
            continue

        skill_id = row.get("id")
        if not skill_id:
            die("a product row has no id")
        if "/" in skill_id or skill_id in (".", ".."):
            die(f"skill id '{skill_id}' is not a plain directory name")

        supplied_by = row.get("supplied-by")
        if not supplied_by:
            die(f"skill {skill_id}: no 'supplied-by' path — cannot locate its bytes")

        src_dir = safe_relative(repo_root_real, supplied_by, f"skill {skill_id} supplied-by")
        if not os.path.isdir(src_dir):
            die(f"skill {skill_id}: supplied-by '{supplied_by}' is not a directory")

        body_path = os.path.join(src_dir, BODY_FILENAME)
        if not os.path.isfile(body_path):
            die(f"skill {skill_id}: no {BODY_FILENAME} under '{supplied_by}'")

        want = row.get("sha256")
        if not want:
            die(f"skill {skill_id}: manifest row carries no sha256")

        with open(body_path, "rb") as handle:
            got = canonical_digest(handle.read())
        if got != want:
            die(
                f"skill {skill_id}: {BODY_FILENAME} sha256 {got} != manifest {want} — "
                "the manifest is stale (run `dotnet fsi scripts/generate-skill-manifest.fsx` "
                "and commit)"
            )

        dest = os.path.join(out, "skills", skill_id)
        # The whole row directory, so a body's sidecars travel with it (ADR-0014 clause 4).
        shutil.copytree(src_dir, dest)
        staged += 1

    if staged == 0:
        die("no product rows staged — a truncated or empty manifest?")

    print(f"stage-skills: staged {staged} product skill(s) + the manifest into {out}")


if __name__ == "__main__":
    main()
