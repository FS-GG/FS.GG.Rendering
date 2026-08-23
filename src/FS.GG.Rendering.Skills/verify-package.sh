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
#
# A MACHINE-READABLE RECEIPT, OPTIONALLY. Set `VERIFY_PACKAGE_JUNIT=<path>` and the run also writes
# a JUnit report with one testcase per numbered step. Human stdout is unchanged and the verdict is
# unchanged; the file exists because a green log is not a receipt anything can read — SDD's
# `evidence --from-test-report` needs a report a runner produced, and so does CI's test summary.
# The report is written on FAILURE too (that is the point), including when a helper aborts the run
# under `set -e`, which the EXIT trap below covers.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_ROOT="$(cd "$HERE/../.." && pwd)"
MANIFEST="$SRC_ROOT/template/skill-manifest/skill-manifest.json"
WORK="$(mktemp -d)"

JUNIT="${VERIFY_PACKAGE_JUNIT:-}"
CURRENT_STEP="preconditions"
JUNIT_WRITTEN=0
CASE_NAMES=()
CASE_FAILURES=()   # "" for a passing case, else the failure message

xml_escape() {
  local s="$1"
  s="${s//&/&amp;}"; s="${s//</&lt;}"; s="${s//>/&gt;}"; s="${s//\"/&quot;}"
  printf '%s' "$s"
}

write_junit() {
  [ -n "$JUNIT" ] || return 0
  [ "$JUNIT_WRITTEN" -eq 0 ] || return 0
  JUNIT_WRITTEN=1
  mkdir -p "$(dirname "$JUNIT")"
  local failures=0 i
  for i in "${!CASE_NAMES[@]}"; do
    [ -z "${CASE_FAILURES[$i]}" ] || failures=$((failures + 1))
  done
  {
    printf '<?xml version="1.0" encoding="utf-8"?>\n'
    printf '<testsuites>\n'
    printf '  <testsuite name="verify-package" tests="%d" failures="%d" errors="0" skipped="0">\n' \
      "${#CASE_NAMES[@]}" "$failures"
    for i in "${!CASE_NAMES[@]}"; do
      printf '    <testcase classname="FS.GG.Rendering.Skills.verify-package" name="%s"' \
        "$(xml_escape "${CASE_NAMES[$i]}")"
      if [ -z "${CASE_FAILURES[$i]}" ]; then
        printf ' />\n'
      else
        printf '>\n      <failure message="%s" />\n    </testcase>\n' \
          "$(xml_escape "${CASE_FAILURES[$i]}")"
      fi
    done
    printf '  </testsuite>\n'
    printf '</testsuites>\n'
  } > "$JUNIT"
  echo "verify-package: wrote $JUNIT"
}

# Covers both the ordinary exits and an abort under `set -e` (a helper dying mid-step), so a run
# that ends without reaching `fail` still leaves a receipt naming the step it died in.
on_exit() {
  local rc=$?
  if [ "$rc" -ne 0 ] && [ "$JUNIT_WRITTEN" -eq 0 ]; then
    CASE_NAMES+=("$CURRENT_STEP")
    CASE_FAILURES+=("aborted with exit $rc")
    write_junit
  fi
  rm -rf "$WORK"
}
trap on_exit EXIT

step() { CURRENT_STEP="$1"; echo "== $1 =="; }
step_ok() { CASE_NAMES+=("$CURRENT_STEP"); CASE_FAILURES+=(""); echo "   $1"; }

fail() {
  echo "verify-package: FAIL — $*" >&2
  CASE_NAMES+=("$CURRENT_STEP")
  CASE_FAILURES+=("$*")
  write_junit
  exit 1
}

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

step "1. stage + derive parity (all product rows staged and content-addressed)"
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
step_ok "${#DELIVERED_ROWS[@]} product skill(s) staged & content-addressed"

step "2. a newly added product row is staged from the manifest, even sourced out-of-tree"
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
new_sidecar_sha="$(digest "$fixture/template/somewhere-else/new-row/skill/notes/detail.md")"
python3 - "$fixture/template/skill-manifest/skill-manifest.json" "$new_sha" "$new_sidecar_sha" <<'PY'
import json, sys
doc = {"schemaVersion": 2, "skills": [{
    "id": "fs-gg-new-row",
    "scope": "product",
    "sha256": sys.argv[2],
    "resolvablePath": ".agents/skills/fs-gg-new-row/SKILL.md",
    "materializes-when": "always",
    "supplied-by": "template/somewhere-else/new-row/skill/",
    "files": [
        {"path": "SKILL.md", "sha256": sys.argv[2]},
        {"path": "notes/detail.md", "sha256": sys.argv[3]},
    ],
}]}
with open(sys.argv[1], "w") as handle:
    json.dump(doc, handle)
PY
python3 "$fixture/src/FS.GG.Rendering.Skills/stage-skills.py" "$fixture/stage" >/dev/null
[ -f "$fixture/stage/skills/fs-gg-new-row/SKILL.md" ] \
  || fail "a new product row was not staged from the manifest"
[ -f "$fixture/stage/skills/fs-gg-new-row/notes/detail.md" ] \
  || fail "a new product row's sidecar was not staged alongside its body"
step_ok "synthetic out-of-tree product row flowed from manifest to staged bytes, sidecar included"

step "3. pack + content assert (bodies AND their sidecars)"
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
step_ok "nupkg carries the manifest + every delivered SKILL.md + its sidecars + the handle + README"

# manifest_files_ok <content-dir>: the schema-v2 closed-set verifier used by the consumer route.
# Every declared file must exist with its canonical text digest, SKILL.md must retain the legacy row
# digest, and every packed skill directory must contain no undeclared sidecar.
manifest_files_ok() {
  python3 - "$1" <<'PY'
import hashlib, json, os, sys
root = sys.argv[1]
doc = json.load(open(os.path.join(root, "skill-manifest.json"), encoding="utf-8"))
if doc.get("schemaVersion") != 2:
    raise SystemExit("schemaVersion is not 2")
for row in doc.get("skills", []):
    if row.get("scope") != "product":
        continue
    files = row.get("files")
    if not isinstance(files, list) or not files:
        raise SystemExit(f"{row.get('id')}: missing files")
    declared = {f.get("path"): f.get("sha256") for f in files if isinstance(f, dict)}
    if len(declared) != len(files) or "SKILL.md" not in declared or declared["SKILL.md"] != row.get("sha256"):
        raise SystemExit(f"{row.get('id')}: invalid closed file set")
    directory = os.path.join(root, "skills", row["id"])
    actual = set()
    for base, _, names in os.walk(directory):
        for name in names:
            actual.add(os.path.relpath(os.path.join(base, name), directory).replace(os.sep, "/"))
    if actual != set(declared):
        raise SystemExit(f"{row['id']}: declared set differs from packed bytes")
    for path, want in declared.items():
        raw = open(os.path.join(directory, path), "rb").read()
        if raw.startswith(b"\xef\xbb\xbf"):
            raw = raw[3:]
        got = hashlib.sha256(raw.replace(b"\r\n", b"\n")).hexdigest()
        if got != want:
            raise SystemExit(f"{row['id']}/{path}: digest mismatch")
PY
}

step "4. content-addressed: every PACKED declared byte matches its schema-v2 manifest"
unzip -q "$nupkg" "rendering-skills/*" -d "$WORK/unpacked"
diff -q "$MANIFEST" "$WORK/unpacked/rendering-skills/skill-manifest.json" >/dev/null \
  || fail "packed rendering-skills/skill-manifest.json is not byte-identical to the committed manifest"
manifest_files_ok "$WORK/unpacked/rendering-skills" \
  || fail "a packed declared skill file does not match the schema-v2 manifest"
step_ok "every packed declared file verifies against the schema-v2 manifest"

step "5. a tampered byte is REJECTED by that same verify (fail-loud)"
cp -r "$WORK/unpacked/rendering-skills" "$WORK/tampered"
first_id="${DELIVERED_ROWS[0]%%$'\t'*}"
echo "CORRUPT" >> "$WORK/tampered/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx"
if manifest_files_ok "$WORK/tampered"; then
  fail "the schema-v2 verifier PASSED against a tampered feedback-report sidecar — it is not firing"
fi
step_ok "tampered feedback-report sidecar rejected by the schema-v2 verifier, as required"

write_junit
echo "verify-package: OK"
