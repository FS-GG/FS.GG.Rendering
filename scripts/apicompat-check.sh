#!/usr/bin/env bash
# apicompat-check.sh — breaking-change (ApiCompat / Package Validation) detector for the
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
#   registry coherence id `apicompat-publicapi-gate` (Governance spec 088 research D1).
#
# THIS GATE DOES NOT SUBSUME readiness/surface-baselines/ — AND IT CANNOT (#754)
#   This block used to end "The source-level public-surface record stays the committed .fsi baselines in
#   readiness/surface-baselines/", which reads as though those baselines were a redundant second copy of
#   what runs here. #694 read it exactly that way and filed #754 to retire them in favour of this gate.
#   They answer different questions. This gate is SemVer-AWARE — it reports BREAKS, so ADDITIVE drift
#   never errors here — and it exits 0 having compared nothing on `FeedUnavailable` (every fork PR: no
#   token) and `NoBaselineYet`. It is not a floor. The baselines are also the API-symbol INPUT to
#   skill-parity evidence (Feature 168), so they are load-bearing regardless of what THIS gate does.
#   The full argument lives ONCE, in the header of `scripts/refresh-surface-baselines.fsx` — read it
#   before believing this gate replaces anything (one definition, two consumers, no drift; the #661 rule).
#
# OUT-OF-BAND, BUT NOT ADVISORY (the D7 shape; the status has since changed)
#   Per FS.GG.Governance spec 088 D7 this runs as a SEPARATE step and never reddens the normal
#   build/release pack (Package Validation is left OFF there) — that much is unchanged. But the
#   step is no longer informational. The script EXITS NON-ZERO when a real break is found, and its
#   `api-compatibility-gate` job IS in branch protection's required set on `main` (since 2026-07-09;
#   ADR-0101 authorized, ADR-0103 records it). `enforce_admins` is ON, so `gh pr merge --admin` does
#   not bypass it either. A break BLOCKS the merge, and the remedy is to CUT A SEMVER MAJOR — the
#   break is the only signal forcing that bump. Do NOT reach for `ApiCompatGenerateSuppressionFile`
#   to go green: blanket suppression inverts this gate and ships a silent break under a
#   preview-patch, the exact failure `apicompat-publicapi-gate` is registered to prevent (ADR-0101).
#   A suppression is legitimate only as ADR-0102 used one — `IsBaselineSuppression`, a single
#   diagnostic and target, and a lifetime note. See docs/ci/cadence-map.md §5.1.
#
# A GATE THAT COULD NOT RUN NEVER REPORTS A PASS (#216)
#   Every packable lands in exactly one of five states, and only two of them mean "compared".
#
#     OK               packed, and ApiCompat found no break vs the baseline.
#     BREAK            packed, and ApiCompat reported a CP#### error.            -> exit 1
#     NoBaselineYet    the feed ANSWERED, and this package has no published version yet. Nothing to
#                      compare against; a first publish is not a break.          -> exit 0
#     Indeterminate    the pack or the tool failed. The comparison did NOT happen, and the cause is
#                      a fact about the tree under test, not about the network.  -> exit 3
#     FeedUnavailable  the feed did not answer (transport error, 5xx, no token). The comparison did
#                      not happen for a reason external to the change.           -> exit 0, ::error::
#
#   Indeterminate used to exit 0. From Feature 211 until #186, all 17 packables failed to pack with
#   NU1403 and the script reported `Indeterminate=17` and PASSED — seventeen out of seventeen never
#   compared, and nothing said so above a per-project line in the job log. A pack failure is the tree
#   failing to build under Release+PackageValidation; that is exactly what a gate should redden on.
#
#   FeedUnavailable is split OUT of Indeterminate so that the bound ADR-0101 relies on still holds:
#   requiring this check takes a dependency on feed availability, and a feed outage must inform a
#   merge, not block it. It is a loud `::error::` and a job-summary line, never a silent pass. The
#   split is on WHO failed to answer, and it is drawn before packing (see `latest_version`) — pack
#   logs are never pattern-matched for "looks like a network problem", because NU1403 looks exactly
#   like one and was not.
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

# Appends a markdown block to the job summary, so a gate that did not run says so on the run's face
# rather than on line 400 of a collapsed log. No-op outside Actions.
summarize() {
  [ -n "${GITHUB_STEP_SUMMARY:-}" ] || return 0
  printf '%s\n' "$@" >> "$GITHUB_STEP_SUMMARY"
}

token="${NUGET_FEED_TOKEN:-${GH_TOKEN:-${GITHUB_TOKEN:-}}}"
if [ -z "$token" ]; then
  # Fork PRs get no secret, by design (gate.yml FR-001/FR-013) — they must still merge, so this is
  # exit 0. But it is FeedUnavailable, not a pass: nothing was compared.
  echo "::error title=ApiCompat did not run::no feed token (NUGET_FEED_TOKEN / GH_TOKEN / GITHUB_TOKEN) — no baseline could be read, so NO package was compared. This is not a pass." >&2
  summarize "### API compatibility gate — did not run" "" \
            "No feed token, so every packable resolved \`FeedUnavailable\`. **Nothing was compared.**" \
            "Exiting 0 so fork PRs still merge (ADR-0101)."
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

# Latest published version of a package id on the feed.
#
# Sets LV_STATUS to one of ok | nobaseline | feedunavailable, and on `ok` sets LV_VERSION.
#
# This used to return "" for BOTH "the package is not published" and "curl failed", and the caller
# read "" as NoBaselineYet — so a feed outage or an expired token reported every package as a happy
# first publish and exited 0. That is a second instance of the green-by-omission #216 filed, one call
# earlier than the one it names. The two causes are now separated at the source: a 404 is the feed
# ANSWERING "no such package"; a transport failure or any other HTTP status is the feed not
# answering. So `curl -f` is deliberately NOT used — it collapses 404 and 5xx into the same exit 22.
#
# The flat-container `versions` array has NO guaranteed order (NuGet API spec) — nuget.org happens
# to return it oldest-first, GitHub Packages returns it NEWEST-first. So the max must be computed,
# never read off an end: taking `tail -1` picked the OLDEST version on this feed, which silently
# baselined every package against 0.1.52-preview.1 and re-reported both already-shipped majors
# (0.2.0, 0.3.0) as fresh breaks on every run. That is what kept `api-compatibility-gate` red on
# `main` from the 0.2.0 release onward, and it would have made the job permanently red after any
# major — i.e. un-requirable by construction (ADR-0101).
#
# `sort -V` gives Debian version ordering, which agrees with SemVer on the dotted numeric core and
# compares numeric prerelease identifiers numerically (`preview.10` > `preview.2`). It disagrees on
# one rule: SemVer says a prerelease precedes its own release (`1.0.0-preview.1` < `1.0.0`), while
# plain `-` sorts after end-of-string. Mapping the prerelease separator to `~` — the one character
# `sort -V` orders before everything, including the empty string — restores exactly that rule. Only
# the FIRST `-` is the separator, so `sed 's/-/~/'` (no `g`) is deliberate.
#
# Requires GNU coreutils `sort` (ubuntu-latest CI, and the repo's Linux dev boxes).
LV_STATUS=""; LV_VERSION=""; LV_ERR=""
latest_version() {
  local id_lower http rc body err
  id_lower="$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
  body="$workdir/index.json"; err="$workdir/index.err"
  LV_STATUS=""; LV_VERSION=""; LV_ERR=""

  http="$(curl -sSL -o "$body" -w '%{http_code}' \
            -H "Authorization: Bearer $token" "$FEED_DL/$id_lower/index.json" 2>"$err")"
  rc=$?
  if [ "$rc" -ne 0 ]; then
    LV_STATUS=feedunavailable; LV_ERR="curl exit $rc: $(tr -d '\n' <"$err" | cut -c1-120)"; return
  fi
  case "$http" in
    200) ;;
    404) LV_STATUS=nobaseline; return ;;
    *)   LV_STATUS=feedunavailable; LV_ERR="HTTP $http"; return ;;
  esac

  LV_VERSION="$(tr ',' '\n' <"$body" | grep -oE '"[0-9][^"]*"' | tr -d '"' \
                  | sed 's/-/~/' | sort -V | tail -1 | sed 's/~/-/')"
  # 200 with an empty `versions` array: the feed answered, the package is unpublished.
  if [ -n "$LV_VERSION" ]; then LV_STATUS=ok; else LV_STATUS=nobaseline; fi
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

echo "apicompat-check — ApiCompat/Package Validation vs the org feed baseline (REQUIRED check on main)"
echo "feed: $FEED_URL   packables: ${#projects[@]}"
echo

ok=0; broke=0; nobaseline=0; indeterminate=0; feedunavailable=0
declare -a break_lines indeterminate_lines feed_lines

for proj in "${projects[@]}"; do
  pkgid="$(grep -oE '<PackageId>[^<]+</PackageId>' "$proj" | sed -E 's/<\/?PackageId>//g' | head -1)"
  [ -z "$pkgid" ] && pkgid="$(basename "$proj" .fsproj)"

  if [ -n "$FORCE_BASELINE" ]; then
    baseline="$FORCE_BASELINE"
  else
    latest_version "$pkgid"
    case "$LV_STATUS" in
      nobaseline)
        printf '  %-28s NoBaselineYet (feed has no published version)\n' "$pkgid"
        nobaseline=$((nobaseline+1)); continue ;;
      feedunavailable)
        printf '  %-28s FeedUnavailable (baseline lookup failed: %s)\n' "$pkgid" "$LV_ERR"
        feedunavailable=$((feedunavailable+1)); feed_lines+=("    $pkgid: $LV_ERR"); continue ;;
    esac
    baseline="$LV_VERSION"
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
      printf '  %-28s Indeterminate (pack/tool failure — NOT compared; see log)\n' "$pkgid"
      indeterminate=$((indeterminate+1))
      first_err="$(grep -m1 -oE 'error [A-Z]+[0-9]+: .*' "$log" | sed -E 's/ \[.*//')"
      indeterminate_lines+=("    $pkgid: ${first_err:-pack failed with no diagnosable error; see job log}")
      tail -3 "$log" | sed 's/^/      /'
      echo "::error title=ApiCompat could not run for $pkgid::pack failed, so this package was never compared against baseline $baseline (see job log)"
    fi
  fi
done

compared=$((ok + broke))
echo
echo "summary: OK=$ok  BREAK=$broke  NoBaselineYet=$nobaseline  Indeterminate=$indeterminate  FeedUnavailable=$feedunavailable  (total ${#projects[@]}, compared $compared)"

if [ "$broke" -gt 0 ]; then
  echo
  echo "breaking changes (force a SemVer major, or suppress deliberately with ApiCompatGenerateSuppressionFile):"
  printf '%s\n' "${break_lines[@]}"
fi
if [ "$indeterminate" -gt 0 ]; then
  echo
  echo "NOT COMPARED — these packables failed to pack, so the gate did not run for them:"
  printf '%s\n' "${indeterminate_lines[@]}"
  summarize "### API compatibility gate — $indeterminate of ${#projects[@]} packables were NOT compared" "" \
            "\`dotnet pack\` failed, so ApiCompat never ran for them. This is not a pass." "" \
            '```' "${indeterminate_lines[@]}" '```'
fi
if [ "$feedunavailable" -gt 0 ]; then
  echo
  echo "FEED UNAVAILABLE — no baseline could be read for these packables (external to this change):"
  printf '%s\n' "${feed_lines[@]}"
  echo "::error title=ApiCompat did not run::the package feed did not answer for $feedunavailable packable(s) — they were NOT compared. Exiting 0 (ADR-0101: a feed outage informs a merge, it does not block one)."
  summarize "### API compatibility gate — feed unavailable" "" \
            "$feedunavailable of ${#projects[@]} packables were **not compared**: the feed did not answer." \
            "Exit 0 by decision (ADR-0101), not because the check passed." "" \
            '```' "${feed_lines[@]}" '```'
fi

# A break is the stronger signal: it means the gate RAN and found something. Report it as 1 even if
# some other packable also failed to pack.
[ "$broke" -gt 0 ] && exit 1
[ "$indeterminate" -gt 0 ] && exit 3
exit 0
