#!/usr/bin/env bash
# Offline regression for the status boundary: only an explicitly admitted first publication may
# pass without a comparison. A missing required baseline and a wholly unavailable feed fail closed.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
tmp="$(mktemp -d)"
trap 'find "$tmp" -type f -delete; find "$tmp" -depth -type d -empty -delete' EXIT

mkdir -p "$tmp/bin"
cat > "$tmp/bin/curl" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
out=""
while (($#)); do
  if [[ "$1" == "-o" ]]; then out="$2"; shift 2; else shift; fi
done
[[ -z "$out" ]] || printf '%s\n' '{"versions":[]}' > "$out"
printf '404'
EOF
chmod +x "$tmp/bin/curl"

project="src/Scene.SvgBrowser/Scene.SvgBrowser.fsproj"
common=(env PATH="$tmp/bin:$PATH" NUGET_FEED_TOKEN=test APICOMPAT_TEST_PROJECT="$project" scripts/apicompat-check.sh)

"${common[@]}" --release-plan eng/release/svg-preview-a-0.29.0.json > "$tmp/first.log"
grep -q 'FirstPublication' "$tmp/first.log"

python3 - eng/release/svg-preview-a-0.29.0.json "$tmp/required.json" <<'PY'
import json, sys
with open(sys.argv[1], encoding="utf-8") as stream:
    plan = json.load(stream)
for package in plan["packages"]:
    if package["id"] == "FS.GG.UI.Scene.SvgBrowser":
        package["apiBaseline"] = "required"
with open(sys.argv[2], "w", encoding="utf-8") as stream:
    json.dump(plan, stream)
PY
set +e
"${common[@]}" --release-plan "$tmp/required.json" > "$tmp/required.log" 2>&1
required_rc=$?
set -e
[[ "$required_rc" == 5 ]]
grep -q 'Unavailable' "$tmp/required.log"

set +e
env -u NUGET_FEED_TOKEN -u GH_TOKEN -u GITHUB_TOKEN \
  APICOMPAT_TEST_FEED_URL=https://nuget.pkg.github.com/FS-GG/index.json \
  scripts/apicompat-check.sh > "$tmp/no-token.log" 2>&1
unavailable_rc=$?
set -e
[[ "$unavailable_rc" == 5 ]]
grep -q 'NO package was compared' "$tmp/no-token.log"

echo "apicompat release statuses: pass / admitted-first-publication / unavailable fail-closed — OK"
