#!/usr/bin/env bash
# verify-package.sh — the FS.GG.Rendering.Skills gate (ADR-0062, ADR-0063, ADR-0014;
# FS.GG.Rendering#1240). Meant to be run by CI and locally. Nothing executes a workflow, so the
# verification lives in a script the workflow CALLS rather than in hand-copied YAML. Mirrors
# FS.GG.Game's src/FS.GG.Game.Skills/verify-package.sh and .github's src/FS.GG.Drivers equivalent.
#
# It proves the things this package must get right:
#   1. DERIVED, NOT RESTATED — the staged set is exactly template/skill-manifest/skill-manifest.json's
#      `scope: product` rows (ADR-0058), and the packed manifest is byte-identical to the committed one.
#   2. NEW ROWS FLOW, INCLUDING OUT-OF-TREE ONES — a synthetic row sourced from OUTSIDE
#      template/product-skills/ is staged without joining any hand-maintained set.
#   3. PACKS, WITH SIDECARS — `dotnet pack` produces a nupkg carrying the manifest, every delivered
#      SKILL.md, every sidecar file beside those bodies, and the consumer handle + README.
#   4. CONTENT-ADDRESSED — every PACKED SKILL.md's canonical digest matches its manifest sha256,
#      checked against the bytes unzipped from the nupkg rather than against the working tree.
#   5. FAILS LOUD — a tampered byte is DETECTED by that same digest check, never silently delivered.
#
# STEP 5 IS THE POINT OF STEP 4. It asserts that the VERDICT of the very function step 4 passes
# with flips under a mutation. Asserting merely that "a digest changed" would be tautological —
# any appended byte changes a sha256 — and would pass even against a verify that never ran.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_ROOT="$(cd "$HERE/../.." && pwd)"
MANIFEST="$SRC_ROOT/template/skill-manifest/skill-manifest.json"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
fail() { echo "verify-package: FAIL — $*" >&2; exit 1; }

[ -f "$MANIFEST" ] || fail "template/skill-manifest/skill-manifest.json not found (is this a FS.GG.Rendering checkout?)"

# canonical_digest: BOM-stripped, LF-folded body sha256 — byte-parity with
# scripts/generate-skill-manifest.fsx and template/lifecycle/skill-mirror-vendored.fs, this
# repository's two existing implementations of the same digest. A tiny python helper keeps it
# correct for a BOM'd or CRLF body, which `sha256sum` would not.
digest() { python3 - "$1" <<'PY'
import hashlib, sys
raw = open(sys.argv[1], "rb").read()
if raw.startswith(b"\xef\xbb\xbf"):
    raw = raw[3:]
print(hashlib.sha256(raw.replace(b"\r\n", b"\n")).hexdigest())
PY
}

# The delivered set the manifest declares: (id, sha256, supplied-by) for every `scope: product` row.
mapfile -t DELIVERED_ROWS < <(python3 - "$MANIFEST" <<'PY'
import json, sys
doc = json.load(open(sys.argv[1]))
for s in doc.get("skills", []):
    if s.get("scope") == "product":
        print(f"{s['id']}\t{s['sha256']}\t{s.get('supplied-by','')}")
PY
)
[ "${#DELIVERED_ROWS[@]}" -gt 0 ] || fail "manifest declares no product rows — nothing to deliver"

echo "== 1. stage + derive parity (all product rows staged and content-addressed) =="
python3 "$HERE/stage-skills.py" "$WORK/stage" >/dev/null
diff -q "$MANIFEST" "$WORK/stage/skill-manifest.json" >/dev/null \
  || fail "staged skill-manifest.json is not byte-identical to template/skill-manifest/skill-manifest.json"
for row in "${DELIVERED_ROWS[@]}"; do
  id="${row%%$'\t'*}"; rest="${row#*$'\t'}"; want="${rest%%$'\t'*}"
  f="$WORK/stage/skills/$id/SKILL.md"
  [ -f "$f" ] || fail "skill '$id' not staged (skills/$id/SKILL.md missing)"
  got="$(digest "$f")"
  [ "$got" = "$want" ] || fail "skill '$id' staged sha256 $got != manifest $want"
done
# The staged set must be EXACTLY the delivered set — a stager that shipped an extra directory
# would pass every per-row check above while delivering bytes nothing declares.
staged_count="$(find "$WORK/stage/skills" -mindepth 1 -maxdepth 1 -type d | wc -l)"
[ "$staged_count" -eq "${#DELIVERED_ROWS[@]}" ] \
  || fail "staged $staged_count skill dir(s) but the manifest declares ${#DELIVERED_ROWS[@]} product row(s)"
echo "   ${#DELIVERED_ROWS[@]} product skill(s) staged & content-addressed"

echo "== 2. a newly added product row is staged from the manifest, even sourced out-of-tree =="
# Deliberately sourced from OUTSIDE template/product-skills/, because 3 of this repository's real
# rows are (fs-gg-feedback-report, fs-gg-samples, fs-gg-project). A stager that derived the path
# from the id instead of reading `supplied-by` would pass a fixture rooted at product-skills/ and
# fail on the real catalog; this fixture is shaped so it cannot.
fixture="$WORK/new-row-repo"
mkdir -p "$fixture/src/FS.GG.Rendering.Skills" "$fixture/template/skill-manifest" \
         "$fixture/template/somewhere-else/new-row/skill/notes"
cp "$HERE/stage-skills.py" "$fixture/src/FS.GG.Rendering.Skills/stage-skills.py"
printf 'new row body\n' > "$fixture/template/somewhere-else/new-row/skill/SKILL.md"
printf 'a sidecar the body depends on\n' > "$fixture/template/somewhere-else/new-row/skill/notes/detail.md"
new_sha="$(digest "$fixture/template/somewhere-else/new-row/skill/SKILL.md")"
python3 - "$fixture/template/skill-manifest/skill-manifest.json" "$new_sha" <<'PY'
import json, sys
doc = {"schemaVersion": 1, "skills": [{
    "id": "fs-gg-new-row",
    "scope": "product",
    "sha256": sys.argv[2],
    "resolvablePath": ".agents/skills/fs-gg-new-row/SKILL.md",
    "materializes-when": "always",
    "supplied-by": "template/somewhere-else/new-row/skill/",
}]}
with open(sys.argv[1], "w") as handle:
    json.dump(doc, handle)
PY
python3 "$fixture/src/FS.GG.Rendering.Skills/stage-skills.py" "$fixture/stage" >/dev/null
[ -f "$fixture/stage/skills/fs-gg-new-row/SKILL.md" ] \
  || fail "a new product row was not staged from the manifest"
[ -f "$fixture/stage/skills/fs-gg-new-row/notes/detail.md" ] \
  || fail "a new product row's sidecar was not staged alongside its body"
echo "   synthetic out-of-tree product row flowed from manifest to staged bytes, sidecar included"

echo "== 3. pack + content assert (bodies AND their sidecars) =="
dotnet pack "$HERE/FS.GG.Rendering.Skills.csproj" -c Release -o "$WORK/out" >/dev/null
nupkg="$(echo "$WORK"/out/FS.GG.Rendering.Skills.*.nupkg)"
[ -f "$nupkg" ] || fail "no nupkg produced"
entries="$(unzip -Z1 "$nupkg")"
for want in "build/FS.GG.Rendering.Skills.props" "README.md" "rendering-skills/skill-manifest.json"; do
  grep -qx "$want" <<<"$entries" || fail "nupkg is missing $want"
done
for row in "${DELIVERED_ROWS[@]}"; do
  id="${row%%$'\t'*}"; supplied="${row##*$'\t'}"
  grep -qx "rendering-skills/skills/$id/SKILL.md" <<<"$entries" \
    || fail "nupkg is missing rendering-skills/skills/$id/SKILL.md"
  # Every file beside the body in that row's source directory must also be packed. This is what
  # makes fs-gg-feedback-report's scripts/feedback-tool.fsx — the tool whose absence from a
  # product tree started .github#2380 — a packaging FAILURE rather than a silent omission.
  while IFS= read -r rel; do
    grep -qx "rendering-skills/skills/$id/$rel" <<<"$entries" \
      || fail "nupkg is missing sidecar rendering-skills/skills/$id/$rel (declared under $supplied)"
  done < <(cd "$SRC_ROOT/$supplied" && find . -type f ! -name SKILL.md -printf '%P\n')
done
echo "   nupkg carries the manifest + every delivered SKILL.md + its sidecars + the handle + README"

# content_addressed_ok <content-dir>: returns 0 iff every delivered SKILL.md under <content-dir>/skills/
# digests to its manifest sha256; non-zero (naming the first mismatch) otherwise. This IS the check a
# consuming materializer performs at scaffold time — the load-bearing content-addressed verify — so the
# gate both asserts it PASSES on the real package (step 4) and asserts it FIRES on a tampered byte
# (step 5). Asserting the digest merely "changed" would be tautological; asserting this function's
# VERDICT flips is what proves the verify.
content_addressed_ok() {
  local dir="$1" row id want got
  for row in "${DELIVERED_ROWS[@]}"; do
    id="${row%%$'\t'*}"; local rest="${row#*$'\t'}"; want="${rest%%$'\t'*}"
    got="$(digest "$dir/skills/$id/SKILL.md")" || return 1
    [ "$got" = "$want" ] || { echo "      content-address mismatch: $id sha256 $got != manifest $want" >&2; return 1; }
  done
  return 0
}

echo "== 4. content-addressed: every PACKED byte matches its manifest sha256 =="
unzip -q "$nupkg" "rendering-skills/*" -d "$WORK/unpacked"
diff -q "$MANIFEST" "$WORK/unpacked/rendering-skills/skill-manifest.json" >/dev/null \
  || fail "packed rendering-skills/skill-manifest.json is not byte-identical to the committed manifest"
content_addressed_ok "$WORK/unpacked/rendering-skills" \
  || fail "a packed SKILL.md does not match its manifest sha256 (the ADR-0014 record)"
echo "   every packed SKILL.md verifies against the manifest — the ADR-0014 record a consumer uses"

echo "== 5. a tampered byte is REJECTED by that same verify (fail-loud) =="
cp -r "$WORK/unpacked/rendering-skills" "$WORK/tampered"
first_id="${DELIVERED_ROWS[0]%%$'\t'*}"
echo "CORRUPT" >> "$WORK/tampered/skills/$first_id/SKILL.md"     # bytes drift from the recorded sha256
if content_addressed_ok "$WORK/tampered"; then
  fail "the content-addressed verify PASSED against a tampered '$first_id' — it is not firing"
fi
echo "   tampered skill '$first_id' rejected by the content-addressed verify, as required"

echo "verify-package: OK"
