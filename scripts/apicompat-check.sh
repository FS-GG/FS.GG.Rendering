#!/usr/bin/env bash
# apicompat-check.sh — advisory breaking-change (ApiCompat / Package Validation) detector for the
# FS.GG.UI.* packables. H3 / FS-GG/.github#20, epic FS-GG/.github#16 Pillar 5.
#
# WHAT IT DOES
#   For each IsPackable FS.GG.UI.* project, pack Release with the .NET SDK's Package Validation
#   enabled and compare the freshly-packed assembly against that package's BASELINE on the org
#   GitHub Packages feed. A removed or changed public member surfaces as a CP#### error — i.e. a
#   public-API break that, under the registry's version ranges, must force a SemVer major.
#
# WHY THIS SHAPE (not the shared FsggApiGate knob)
#   The FS.GG.UI.* packages are F#. Microsoft.CodeAnalysis.PublicApiAnalyzers (the C# half of the
#   org shared-build-config api-breaking-change-gate) is a Roslyn/C# source analyzer and does NOT
#   analyze F# — so for these packables the operative detector is the language-agnostic SDK
#   ApiCompat / Package Validation (assembly + package level). Mechanism recorded in FS-GG/.github
#   registry coherence id `apicompat-publicapi-gate` (Governance spec 088 research D1). The
#   source-level public-surface record stays the committed .fsi baselines in readiness/surface-baselines/.
#
# ADVISORY (mirrors FS.GG.Governance spec 088 D7)
#   This runs as a SEPARATE step and never reddens the normal build/release pack (Package
#   Validation is left OFF there). The script EXITS NON-ZERO when a real break is found, so the
#   gate flips from advisory → required by dropping `continue-on-error` on the CI job — no script
#   change. Fail-safe: a package with no baseline on the feed is reported NoBaselineYet (NOT a
#   silent pass); a pack/tool failure unrelated to API is reported Indeterminate.
#
# AUTH
#   Needs read access to https://nuget.pkg.github.com/FS-GG. Provide a token via NUGET_FEED_TOKEN
#   (CI: secrets.GITHUB_TOKEN with `packages: read`; locally: a PAT or `gh auth token`). CPM
#   requires package source mapping, so we write a throwaway, source-mapped nuget.config that
#   serves only FS.GG.* from the feed (everything else from nuget.org).
#
# USAGE
#   scripts/apicompat-check.sh [--baseline <version>]
#     --baseline <version>  force one baseline version for every package (default: each package's
#                           own latest published version on the feed).
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

FEED_URL="https://nuget.pkg.github.com/FS-GG/index.json"
FEED_DL="https://nuget.pkg.github.com/FS-GG/download"
FORCE_BASELINE=""
while [ $# -gt 0 ]; do
  case "$1" in
    --baseline) FORCE_BASELINE="${2:-}"; shift 2 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

token="${NUGET_FEED_TOKEN:-${GH_TOKEN:-${GITHUB_TOKEN:-}}}"
if [ -z "$token" ]; then
  echo "::warning::apicompat-check: no feed token (NUGET_FEED_TOKEN / GH_TOKEN / GITHUB_TOKEN) — cannot read baselines." >&2
  echo "All packages would resolve Indeterminate without feed access; exiting advisory-clean." >&2
  exit 0
fi
feed_user="${NUGET_FEED_USER:-${GITHUB_ACTOR:-x-access-token}}"

workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT
cfg="$workdir/nuget.config"
cat > "$cfg" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="fsgg" value="$FEED_URL" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    <packageSource key="fsgg"><package pattern="FS.GG.*" /></packageSource>
  </packageSourceMapping>
  <packageSourceCredentials>
    <fsgg>
      <add key="Username" value="$feed_user" />
      <add key="ClearTextPassword" value="$token" />
    </fsgg>
  </packageSourceCredentials>
</configuration>
EOF

# Latest published version of a package id on the feed, or empty if none (NoBaselineYet).
latest_version() {
  local id_lower; id_lower="$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
  curl -fsSL -H "Authorization: Bearer $token" "$FEED_DL/$id_lower/index.json" 2>/dev/null \
    | grep -oE '"[0-9][^"]*"' | tr -d '"' | tail -1
}

# A check version strictly greater than the baseline that PRESERVES prerelease-ness. For a
# prerelease baseline we append a `.apicheck` identifier (SemVer precedence: more prerelease fields
# sort higher when the leading ones are equal, so 0.1.52-preview.1.apicheck > 0.1.52-preview.1) —
# crucially it stays a prerelease, so packages with prerelease dependencies (e.g. SkiaSharp -preview)
# don't trip NU5104 ("a stable release should not have a prerelease dependency"). A stable baseline
# bumps its patch. ApiCompat still reports real breaks regardless of the version number (proven);
# additions — which are not breaks — never error.
check_version() {
  local b="$1"
  if [[ "$b" == *-* ]]; then printf '%s.apicheck' "$b"; return; fi
  local major minor patch; IFS='.' read -r major minor patch <<<"$b"
  printf '%s.%s.%s' "${major:-0}" "${minor:-0}" "$(( ${patch:-0} + 1 ))"
}

mapfile -t projects < <(grep -rl '<IsPackable>true</IsPackable>' src --include='*.fsproj' | sort)

echo "apicompat-check — advisory ApiCompat/Package Validation vs the org feed baseline"
echo "feed: $FEED_URL   packables: ${#projects[@]}"
echo

ok=0; broke=0; nobaseline=0; indeterminate=0
declare -a break_lines

for proj in "${projects[@]}"; do
  pkgid="$(grep -oE '<PackageId>[^<]+</PackageId>' "$proj" | sed -E 's/<\/?PackageId>//g' | head -1)"
  [ -z "$pkgid" ] && pkgid="$(basename "$proj" .fsproj)"

  baseline="$FORCE_BASELINE"
  [ -z "$baseline" ] && baseline="$(latest_version "$pkgid")"
  if [ -z "$baseline" ]; then
    printf '  %-28s NoBaselineYet (not on feed)\n' "$pkgid"
    nobaseline=$((nobaseline+1)); continue
  fi

  cv="$(check_version "$baseline")"
  log="$workdir/${pkgid}.log"
  if dotnet pack "$proj" -c Release --configfile "$cfg" \
        -p:Version="$cv" \
        -p:EnablePackageValidation=true \
        -p:PackageValidationBaselineVersion="$baseline" \
        -o "$workdir/out" >"$log" 2>&1; then
    printf '  %-28s OK            (compatible with %s)\n' "$pkgid" "$baseline"
    ok=$((ok+1))
  else
    if grep -qE 'error CP[0-9]' "$log"; then
      printf '  %-28s BREAK         (vs %s)\n' "$pkgid" "$baseline"
      broke=$((broke+1))
      while IFS= read -r l; do break_lines+=("    $pkgid: $l"); done \
        < <(grep -oE 'error CP[0-9]+: .*' "$log" | sed -E 's/ \[.*//' | sort -u)
      echo "::warning title=ApiCompat break in $pkgid::public-API break vs baseline $baseline (see job log)"
    else
      printf '  %-28s Indeterminate (pack/tool failure — not a clean pass; see log)\n' "$pkgid"
      indeterminate=$((indeterminate+1))
      tail -3 "$log" | sed 's/^/      /'
    fi
  fi
done

echo
echo "summary: OK=$ok  BREAK=$broke  NoBaselineYet=$nobaseline  Indeterminate=$indeterminate  (total ${#projects[@]})"
if [ "$broke" -gt 0 ]; then
  echo
  echo "breaking changes (force a SemVer major, or suppress deliberately with ApiCompatGenerateSuppressionFile):"
  printf '%s\n' "${break_lines[@]}"
  exit 1
fi
exit 0
