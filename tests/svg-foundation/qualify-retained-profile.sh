#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output="${1:?qualification evidence output path is required}"
sdd_version=1.7.0
public_source=https://api.nuget.org/v3/index.json
package_source="${FSGG_SDD_PACKAGE_SOURCE:-$public_source}"
quint_sha=939b64095b706017f2f202c6f99c860c40be7c31bddc2b98557316e50f42cd7f
lmt_sha=37e0b0365c2641edce40b48605471f61fa12e97c3e2376152f0e849abdc31f10
: "${QUINT_BIN:?set QUINT_BIN to exact Quint 0.32.0}"
: "${LMT_BIN:?set LMT_BIN to the exact qualified lmt object}"

fail() { echo "svg-retained-qualification: $*" >&2; exit 1; }
sha() { sha256sum "$1" | cut -d' ' -f1; }
[[ -x "$QUINT_BIN" && "$(sha "$QUINT_BIN")" == "$quint_sha" ]] || fail 'Quint object mismatch'
[[ -x "$LMT_BIN" && "$(sha "$LMT_BIN")" == "$lmt_sha" ]] || fail 'lmt object mismatch'
[[ "$($QUINT_BIN --version)" == '0.32.0' ]] || fail 'Quint version mismatch'

scratch="$(mktemp -d "${TMPDIR:-/tmp}/svg-retained-profile.XXXXXX")"
trap 'rm -rf -- "$scratch"' EXIT
mkdir -p "$scratch/packages" "$scratch/http" "$scratch/source/models/svg-foundation"
export NUGET_PACKAGES="$scratch/packages"
export NUGET_HTTP_CACHE_PATH="$scratch/http"

if [[ "$package_source" == "$public_source" ]]; then
  cat > "$scratch/NuGet.Config" <<CONFIG
<configuration><packageSources><clear/><add key="public" value="$public_source"/></packageSources></configuration>
CONFIG
else
  cat > "$scratch/NuGet.Config" <<CONFIG
<configuration><packageSources><clear/><add key="candidate" value="$package_source"/><add key="nuget" value="$public_source"/></packageSources><packageSourceMapping><packageSource key="candidate"><package pattern="FS.GG.SDD.*"/><package pattern="FS.GG.Contracts"/></packageSource><packageSource key="nuget"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
CONFIG
fi

cli=''
for attempt in 1 2 3; do
  candidate="$scratch/tool-$attempt"
  if dotnet tool install FS.GG.SDD.Cli --version "$sdd_version" --tool-path "$candidate" \
    --configfile "$scratch/NuGet.Config" --no-cache; then
    cli="$candidate/fsgg-sdd"
    break
  fi
  echo "svg-retained-qualification: public SDD install attempt $attempt failed" >&2
  [[ "$attempt" == 3 ]] || sleep 10
done
[[ -n "$cli" && -x "$cli" ]] || fail 'public SDD installation failed after three attempts'
installed_version="$($cli --version)"
echo "svg-retained-qualification: installed-cli-version=$installed_version source=$package_source"
grep -F "$sdd_version" <<<"$installed_version" >/dev/null || fail "installed SDD identity mismatch: $installed_version"

export HTTP_PROXY=http://127.0.0.1:1
export HTTPS_PROXY=http://127.0.0.1:1
export ALL_PROXY=http://127.0.0.1:1
export NO_PROXY=127.0.0.1,localhost
if ! "$cli" typed-sdd provision --cache "$scratch/cache" --quint "$QUINT_BIN" --lmt "$LMT_BIN" > "$scratch/provision.json"; then
  cat "$scratch/provision.json" >&2
  fail 'installed SDD rejected the exact profile-2 tools'
fi
jq -e '.profile == "fsgg-quint-profile/2" and .outcome == "succeeded"' "$scratch/provision.json" >/dev/null || {
  cat "$scratch/provision.json" >&2
  fail 'profile-2 provision report missing'
}
echo 'svg-retained-qualification: exact profile-2 tools provisioned'

author_once() {
  local destination="$1"
  mkdir -p "$destination/models/svg-foundation"
  cp "$root/models/svg-foundation/retained-interaction.md" "$destination/models/svg-foundation/"
  cp "$root/models/svg-foundation/retained-interaction.bindings.json" "$destination/models/svg-foundation/"
  if ! "$cli" typed-sdd author --root "$destination" --work svg-qual-01-2 \
    --title 'Retained SVG reducer qualification' --agent codex --session svg-qual-01-2 \
    --backend quint-specification-v1 --cache "$scratch/cache" --profile fsgg-quint-profile/2 \
    --source models/svg-foundation/retained-interaction.md \
    --bindings models/svg-foundation/retained-interaction.bindings.json > "$destination-author.json"; then
    cat "$destination-author.json" >&2
    fail "installed SDD author failed for $destination"
  fi
  if ! "$cli" typed-sdd inspect --root "$destination" --work svg-qual-01-2 > "$destination-inspect.json"; then
    cat "$destination-inspect.json" >&2
    fail "installed SDD inspect failed for $destination"
  fi
  echo "svg-retained-qualification: offline author+inspect passed for $destination"
}

author_once "$scratch/author-a"
author_once "$scratch/author-b"
diff -ru "$scratch/author-a/readiness/svg-qual-01-2" "$scratch/author-b/readiness/svg-qual-01-2" >/dev/null \
  || fail 'two offline installed author runs diverged'
diff -ru "$root/readiness/svg-qual-01-2" "$scratch/author-a/readiness/svg-qual-01-2" >/dev/null \
  || fail 'committed extracted authority is stale'

qnt="$scratch/author-a/readiness/svg-qual-01-2/quint/retainedInteraction.qnt"
"$QUINT_BIN" typecheck "$qnt" > "$scratch/quint-typecheck.log"
"$QUINT_BIN" test "$qnt" --main retainedInteractionTest > "$scratch/quint-test.log"
for run in a b; do
  mkdir -p "$scratch/traces-$run"
  "$QUINT_BIN" run "$qnt" --main retainedInteraction --invariant retainedStateSafe \
    --max-steps 12 --max-samples 16 --n-traces 16 --seed=0x0123456789abcdef \
    --backend=typescript --out-itf "$scratch/traces-$run/trace_{seq}.itf.json" --verbosity 0 \
    > "$scratch/quint-run-$run.log"
  python3 "$root/tests/svg-foundation/extract-retained-traces.py" "$scratch/traces-$run.tsv" \
    "$scratch/traces-$run"/*.itf.json
done
cmp "$scratch/traces-a.tsv" "$scratch/traces-b.tsv" >/dev/null || fail 'fixed-seed model traces diverged'
cmp "$root/models/svg-foundation/retained-interaction.traces.tsv" "$scratch/traces-a.tsv" >/dev/null \
  || fail 'committed model-generated trace corpus is stale'

for run in a b; do
  mkdir -p "$scratch/document-traces-$run"
  "$QUINT_BIN" run "$qnt" --main retainedInteraction --step documentStep --invariant retainedStateSafe \
    --max-steps 12 --max-samples 16 --n-traces 16 --seed=0x1023456789abcdef \
    --backend=typescript --out-itf "$scratch/document-traces-$run/trace_{seq}.itf.json" --verbosity 0 \
    > "$scratch/quint-document-run-$run.log"
  python3 "$root/tests/svg-foundation/extract-retained-traces.py" --document "$scratch/document-traces-$run.tsv" \
    "$scratch/document-traces-$run"/*.itf.json
done
cmp "$scratch/document-traces-a.tsv" "$scratch/document-traces-b.tsv" >/dev/null \
  || fail 'fixed-seed document model traces diverged'
cmp "$root/models/svg-foundation/document-interaction.traces.tsv" "$scratch/document-traces-a.tsv" >/dev/null \
  || fail 'committed document model-generated trace corpus is stale'

mkdir -p "$scratch/stale-range/models/svg-foundation"
cp "$root/models/svg-foundation/retained-interaction.md" "$scratch/stale-range/models/svg-foundation/"
cp "$root/models/svg-foundation/retained-interaction.bindings.json" "$scratch/stale-range/models/svg-foundation/"
python3 - "$scratch/stale-range/models/svg-foundation/retained-interaction.md" <<'PY'
from pathlib import Path
import sys
p = Path(sys.argv[1])
p.write_text("<!-- shifts every declared source range -->\n" + p.read_text())
PY
if "$cli" typed-sdd author --root "$scratch/stale-range" --work svg-qual-01-2 \
  --title 'Retained SVG reducer qualification' --agent codex --session stale-range \
  --backend quint-specification-v1 --cache "$scratch/cache" --profile fsgg-quint-profile/2 \
  --source models/svg-foundation/retained-interaction.md \
  --bindings models/svg-foundation/retained-interaction.bindings.json > "$scratch/stale-range.json"; then
  fail 'shifted source unexpectedly satisfied stale bindings'
fi
grep -F 'typedSdd.v2.compilationFailed' "$scratch/stale-range.json" >/dev/null || fail 'stale-range diagnostic drifted'
[[ ! -e "$scratch/stale-range/readiness/svg-qual-01-2/typed-authority.json" ]] || fail 'stale-range refusal wrote authority'

mkdir -p "$scratch/stale-action/models/svg-foundation"
cp "$root/models/svg-foundation/retained-interaction.md" "$scratch/stale-action/models/svg-foundation/"
sed 's/"declaration": "selectObject"/"declaration": "selectObjectStale"/' \
  "$root/models/svg-foundation/retained-interaction.bindings.json" \
  > "$scratch/stale-action/models/svg-foundation/retained-interaction.bindings.json"
if "$cli" typed-sdd author --root "$scratch/stale-action" --work svg-qual-01-2 \
  --title 'Retained SVG reducer qualification' --agent codex --session stale-action \
  --backend quint-specification-v1 --cache "$scratch/cache" --profile fsgg-quint-profile/2 \
  --source models/svg-foundation/retained-interaction.md \
  --bindings models/svg-foundation/retained-interaction.bindings.json > "$scratch/stale-action.json"; then
  fail 'unknown action unexpectedly satisfied bindings'
fi
grep -F 'typedSdd.v2.compilationFailed' "$scratch/stale-action.json" >/dev/null || fail 'stale-action diagnostic drifted'
[[ ! -e "$scratch/stale-action/readiness/svg-qual-01-2/typed-authority.json" ]] || fail 'stale-action refusal wrote authority'

tree_sha="$(cd "$root/readiness/svg-qual-01-2" && find . -type f -print0 | sort -z | xargs -0 sha256sum | sha256sum | cut -d' ' -f1)"
mkdir -p "$(dirname "$output")"
jq -n --arg source "$package_source" --arg tree "$tree_sha" --arg corpus "$(sha "$root/models/svg-foundation/retained-interaction.traces.tsv")" \
  --arg documentCorpus "$(sha "$root/models/svg-foundation/document-interaction.traces.tsv")" \
  --arg quint "$quint_sha" --arg lmt "$lmt_sha" \
  '{schema:"fsgg.svg-retained-installed-qualification/v1",sdd:{package:"FS.GG.SDD.Cli",version:"1.7.0",source:$source},profile:"fsgg-quint-profile/2",offlineAuthorInspect:"passed",deterministicExtraction:"passed",committedEvidenceTreeSha256:$tree,boundedModel:{tests:"passed",retained:{run:"passed",steps:12,traces:16,seed:"0x0123456789abcdef",corpusSha256:$corpus},documentAmendment:{run:"passed",steps:12,traces:16,seed:"0x1023456789abcdef",corpusSha256:$documentCorpus}},staleRangeRefusal:"passed",staleActionRefusal:"passed",tools:{quint:{version:"0.32.0",sha256:$quint},lmt:{sha256:$lmt}}}' > "$output"
echo "svg-retained-qualification: installed-sdd=$sdd_version offline=passed deterministic=passed model-traces=passed stale-bindings=passed evidence=$output"
