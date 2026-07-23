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

FEED_URL="${APICOMPAT_TEST_FEED_URL:-https://nuget.pkg.github.com/FS-GG/index.json}"
FEED_DL="${APICOMPAT_TEST_FEED_DL:-https://nuget.pkg.github.com/FS-GG/download}"
FORCE_BASELINE=""
SELF_TEST=""
while [ $# -gt 0 ]; do
  case "$1" in
    --baseline) FORCE_BASELINE="${2:-}"; shift 2 ;;
    # Classify captured SDK output and exit. No feed, no pack, no network — so the required tier can run
    # it (ADR-0105) and the classifier below cannot rot unnoticed.
    --self-test) SELF_TEST=1; shift ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

# Appends a markdown block to the job summary, so a gate that did not run says so on the run's face
# rather than on line 400 of a collapsed log. No-op outside Actions.
summarize() {
  [ -n "${GITHUB_STEP_SUMMARY:-}" ] || return 0
  printf '%s\n' "$@" >> "$GITHUB_STEP_SUMMARY"
}

# ---- how a failed pack is classified (#776) ----------------------------------------------------
#
# ONE DEFINITION, TWO CONSUMERS — the pack loop below, and `--self-test`. The signatures are the part
# that can rot (a future SDK could reword them), and a rot here is silent: the stale branch stops
# matching and a dead suppression goes back to being reported as "pack failed, never compared". So the
# patterns live in ONE place and are asserted against captured SDK output, rather than being written
# twice and trusted.
#
# Both strings below are VERBATIM from .NET SDK 10.0.301 (Microsoft.NET.ApiCompat.ValidatePackage.targets),
# captured by planting a suppression for a member that was never removed and packing against a real feed
# baseline:
#
#   error : Unnecessary suppressions found. The APICompat suppression file can be updated by rebuilding
#           with '/p:ApiCompatGenerateSuppressionFile=true' [/…/Canvas.Lib.fsproj]
#   error : [Baseline] CP0002 (Target: 'M:FS.GG.UI.Canvas.Persistence.thisMemberNeverExisted(System.Int32)')
#
# NOTE the second line: `error : [Baseline] CP0002`, not `error CP0002`. `is_break` deliberately cannot
# match it — that line is a suppression that fired, not an unsuppressed break — and that near-miss is
# exactly why a dead suppression used to land in the Indeterminate bucket.
is_stale_suppression() { grep -q 'Unnecessary suppressions found' "$1"; }
is_break()             { grep -qE 'error CP[0-9]' "$1"; }

# The dead entries name themselves, so REPORT them: the fix is then a deletion somebody can SEE rather
# than a regeneration they have to trust (#776). Same rule as the two predicates above — one definition,
# two consumers (the pack loop and `--self-test`) — and for the same reason: this pattern rots exactly as
# silently as they do, and until #871 it was the one pattern here that NOTHING exercised.
#
# `Target` IS NOT ALWAYS THE MESSAGE'S LAST FIELD, and assuming it was is the whole of #871. The previous
# pattern ended `'\)`, demanding the target's closing quote be followed straight by the paren. Both lines
# below are verbatim from SDK 10.0.302 (captured from real failing packs of Diagnostics and Canvas):
#
#   error : [Baseline] CP0002 (Target: 'M:FS.GG.UI.Canvas.Persistence.gone(System.Int32)')
#   error : [Baseline] CP0001 (Target: 'T:FS.GG.UI.Diagnostics.DiagnosticReadinessImpact', Left: 'lib/net10.0/FS.GG.UI.Diagnostics.dll', Right: 'lib/net10.0/FS.GG.UI.Diagnostics.dll')
#
# A removed MEMBER ends at the target; a removed TYPE carries Left/Right after it, because the suppression
# this repo writes for one has `<Left>`/`<Right>` elements and the SDK echoes them back. So the old pattern
# matched the member and missed the type — and a type is what a major removes. The gate printed
# `DELETE the entries above` above an EMPTY LIST, which is what #869 hit on the live 0.12.0 transition.
#
# Emit `<DiagnosticId>  <Target>`: those are the two fields that identify the `<Suppression>` element to
# delete, so the reader maps the line onto the XML instead of onto SDK prose. `[Baseline]` is required, so
# an UNSUPPRESSED break (`error CP0002: ...`) is never listed here as a dead entry.
stale_entries() {
  grep -oE "\[Baseline\] CP[0-9]+ \(Target: '[^']*'" "$1" \
    | sed -E "s/\[Baseline\] (CP[0-9]+) \(Target: '(.*)'/\1  \2/" \
    | sort -u
}

# Deliberately NOT feed-dependent: this suite runs where the network may not, and a classifier test that
# needed a live pack could not be a gate (ADR-0105). Fixtures are the captured shapes above.
if [ -n "$SELF_TEST" ]; then
  t="$(mktemp -d)"; trap 'rm -rf "$t"' EXIT; fails=0
  assert() { # <name> <expected: stale|break|other> <log-content>
    printf '%s\n' "$3" > "$t/log"
    # SAME ORDER AS THE PACK LOOP, deliberately: break BEFORE stale. If this helper and the loop ever
    # disagree, the test is grading a classifier nobody runs.
    got=other
    if   is_break             "$t/log"; then got=break
    elif is_stale_suppression "$t/log"; then got=stale
    fi
    if [ "$got" = "$2" ]; then echo "  ok    $1 -> $got"
    else echo "  FAIL  $1 -> $got (expected $2)"; fails=$((fails+1)); fi
  }

  assert "a dead suppression is STALE, not Indeterminate" stale \
"/usr/share/dotnet/sdk/10.0.301/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.ApiCompat.ValidatePackage.targets(39,5): error : Unnecessary suppressions found. The APICompat suppression file can be updated by rebuilding with '/p:ApiCompatGenerateSuppressionFile=true' [/x/Canvas.Lib.fsproj]
/usr/share/dotnet/sdk/10.0.301/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.ApiCompat.ValidatePackage.targets(39,5): error : [Baseline] CP0002 (Target: 'M:FS.GG.UI.Canvas.Persistence.gone(System.Int32)') [/x/Canvas.Lib.fsproj]"

  assert "an unsuppressed break is still BREAK" break \
"/x/Canvas.Lib.fsproj : error CP0002: Member 'M:FS.GG.UI.Canvas.Persistence.interpret' exists on the left but not on the right"

  # THE ONE THAT MUST NOT COLLAPSE. A real break and a dead suppression CAN co-occur — one file's entry
  # goes dead on the publish while a different member is newly removed — and BREAK is the stronger signal:
  # the gate ran and found something a human must decide about (a SemVer major), where a stale entry is a
  # chore. Classify this as `stale` and a GENUINE API BREAK is reported as tidy-up and merged. The pack
  # loop tests `is_break` first for exactly this reason; this fixture is what stops someone "simplifying"
  # that order back.
  assert "a break alongside a dead suppression is still a BREAK" break \
"/x/a.fsproj : error CP0002: Member 'M:Some.Real.Break' exists on the left but not on the right
/x/a.fsproj : error : Unnecessary suppressions found. The APICompat suppression file can be updated by rebuilding with '/p:ApiCompatGenerateSuppressionFile=true'"

  assert "an ordinary pack failure is neither" other \
"/x/a.fsproj : error FS0039: The value or constructor 'foo' is not defined."

  assert "an empty log is neither — silence is not a finding" other ""

  # ---- the EXTRACTOR (#871) ----------------------------------------------------------------------
  #
  # Classifying a log `stale` is half the job; the other half is naming the entries to delete, and until
  # #871 nothing here graded that. The regex had been written against the CP0002 fixture above — the one
  # shape where `Target` is the last field — so it silently reported NOTHING for a removed TYPE, and the
  # gate printed "DELETE the entries above" over an empty list on the live 0.12.0 transition (#869).
  #
  # These fixtures are the SDK's real messages. If a future SDK rewords them, this fails LOUDLY here
  # instead of quietly handing the next worker a heading with nothing under it.
  assert_entries() { # <name> <expected> <log-content>
    printf '%s\n' "$3" > "$t/log"
    got="$(stale_entries "$t/log")"
    if [ "$got" = "$2" ]; then echo "  ok    $1"
    else
      echo "  FAIL  $1"
      echo "          expected: ${2:-<nothing>}"
      echo "          got:      ${got:-<nothing>}"
      fails=$((fails+1))
    fi
  }

  # THE #871 REGRESSION. A removed TYPE carries Left/Right after the target; the old pattern demanded the
  # target be last and matched nothing at all here. This is the fixture that was missing.
  assert_entries "a dead TYPE suppression is named (Target is NOT the last field)" \
"CP0001  T:FS.GG.UI.Diagnostics.DiagnosticReadinessImpact" \
"/x.targets(39,5): error : Unnecessary suppressions found. The APICompat suppression file can be updated by rebuilding with '/p:ApiCompatGenerateSuppressionFile=true' [/x/Diagnostics.fsproj]
/x.targets(39,5): error : [Baseline] CP0001 (Target: 'T:FS.GG.UI.Diagnostics.DiagnosticReadinessImpact', Left: 'lib/net10.0/FS.GG.UI.Diagnostics.dll', Right: 'lib/net10.0/FS.GG.UI.Diagnostics.dll') [/x/Diagnostics.fsproj]"

  # The shape that already worked, kept so the fix for the type cannot regress the member. Note the target
  # itself contains parens — `gone(System.Int32)` — so the pattern may not stop at the first `)`.
  assert_entries "a dead MEMBER suppression is still named (parens inside the target)" \
"CP0002  M:FS.GG.UI.Canvas.Persistence.gone(System.Int32)" \
"/x.targets(39,5): error : [Baseline] CP0002 (Target: 'M:FS.GG.UI.Canvas.Persistence.gone(System.Int32)') [/x/Canvas.Lib.fsproj]"

  # An UNSUPPRESSED break is not a dead entry. Listing it here would tell a worker to delete a suppression
  # that does not exist, for a break they must instead cut a major for.
  assert_entries "an unsuppressed break is NOT listed as a dead entry" "" \
"/x/a.fsproj : error CP0002: Member 'M:Some.Real.Break' exists on the left but not on the right"

  if [ "$fails" -gt 0 ]; then echo "apicompat-check --self-test: $fails FAILED"; exit 1; fi
  echo "apicompat-check --self-test: all classifier signatures hold"
  exit 0
fi

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

# Baseline VERSION discovery is feed-backed, and baseline PACKAGE resolution must be too. NuGet
# otherwise reuses a same-ID/version entry from the caller's ambient global-packages or HTTP cache,
# even when that entry came from a temporary/local feed and differs from the configured feed. Keep
# both caches under the gate-owned work directory so every baseline restore starts from empty,
# resolves the configured-feed bytes, and is removed by the cleanup trap above (#1033).
export NUGET_PACKAGES="$workdir/packages"
export NUGET_HTTP_CACHE_PATH="$workdir/http-cache"
mkdir -p "$NUGET_PACKAGES" "$NUGET_HTTP_CACHE_PATH"

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

# The single-project override is an internal functional-test seam: the regression drives this exact
# script against deliberately conflicting local packages without changing the production project set.
if [ -n "${APICOMPAT_TEST_PROJECT:-}" ]; then
  projects=("$APICOMPAT_TEST_PROJECT")
else
  mapfile -t projects < <(grep -rl '<IsPackable>true</IsPackable>' src --include='*.fsproj' | sort)
fi

echo "apicompat-check — ApiCompat/Package Validation vs the org feed baseline (REQUIRED check on main)"
echo "feed: $FEED_URL   packables: ${#projects[@]}"
echo

ok=0; broke=0; nobaseline=0; indeterminate=0; feedunavailable=0; stale=0
declare -a break_lines indeterminate_lines feed_lines stale_lines

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
    # A STALE SUPPRESSION IS NOT A PACK FAILURE, AND CALLING IT ONE COSTS AN HOUR (#776).
    #
    # A release that removes public API needs a TRANSIENT CompatibilitySuppressions.xml: the baseline is
    # the PUBLISHED package, which still has the member, so ApiCompat reports a real CP0002 and the merge
    # cannot happen without the suppression. The moment that release publishes, the baseline moves to the
    # version that just shipped — which does not have the member either — and the same entry now suppresses
    # NOTHING. .NET fails the pack with
    #
    #     error : Unnecessary suppressions found. ...
    #     error : [Baseline] CP0002 (Target: 'M:Some.Removed.Member')
    #
    # Note the shape: the target line reads `error : [Baseline] CP0002`, NOT `error CP0002`. The BREAK grep
    # below cannot match it, so before this branch existed a dead suppression fell through to Indeterminate
    # and announced "pack failed, so this package was never compared" — a MISDIAGNOSIS. The tool ran fine;
    # it found a suppression with nothing to suppress. #443's author went looking for a build failure.
    #
    # It matters because this gate is REQUIRED on `main` with enforce_admins: the transition happens ON THE
    # FEED, not in a commit, so the first PR after a publish reds with no diff having caused it, and every
    # PR in the repo is unmergeable until someone deletes the file. That has happened three times
    # (1159d906, 67d39e68, 855e75f2), each time because the only thing saying "delete me after the release"
    # was a COMMENT INSIDE THE FILE, and comments are not a gate. This branch is the gate.
    # A stale entry is RECORDED even when a break also fired, so the deletion is not lost — but it never
    # decides the status line. `is_break` is tested first: see the co-occurrence fixture in --self-test.
    if is_stale_suppression "$log"; then
      sup="$(dirname "$proj")/CompatibilitySuppressions.xml"
      stale=$((stale+1))
      stale_lines+=("    $pkgid: ${sup#"$repo_root/"} — these entries suppress nothing against baseline $baseline:")
      # The dead entries name themselves. Report them, so the fix is a deletion somebody can SEE, not a
      # regeneration they have to trust. `stale_entries` is the ONE definition `--self-test` grades (#871).
      named=0
      while IFS= read -r t; do stale_lines+=("      $t"); named=1; done < <(stale_entries "$log")
      # NAMED NOTHING is not NOTHING TO NAME (FS-GG/.github#266), and this is the branch #871 needed and
      # did not have. The log says there ARE unnecessary suppressions; if we could not name one of them,
      # the extractor — not the file — is what failed, and printing `DELETE the entries above` over silence
      # sends the reader looking for entries the gate is simply not showing them.
      #
      # A FIXTURE CANNOT COVER THIS, which is why it is a runtime branch and not a sixth assert. The
      # fixtures below are frozen SDK text: reword the `[Baseline]` line in a future SDK and they keep
      # passing while the real log stops matching — the identical rot, one level up. This branch keys on
      # the disagreement itself, so it fires whatever the SDK decides to say.
      if [ "$named" -eq 0 ]; then
        stale_lines+=("      (the pack log says this file has unnecessary suppressions, but NONE could be parsed out of it —")
        stale_lines+=("       the SDK's '[Baseline] CP#### (Target: ...)' line has probably been reworded. Fix \`stale_entries\`")
        stale_lines+=("       in scripts/apicompat-check.sh and its --self-test fixtures. Read the file and the job log; do NOT")
        stale_lines+=("       read this as 'the file is already empty'.)")
      fi
      echo "::error title=Stale ApiCompat suppression in $pkgid::$pkgid's CompatibilitySuppressions.xml suppresses nothing against the published baseline $baseline — the release it was written for is on the feed. DELETE the listed entries (and the file, if that empties it). Do NOT regenerate with ApiCompatGenerateSuppressionFile: that re-adds whatever is breaking TODAY and hides it."
    fi

    if is_break "$log"; then
      printf '  %-28s BREAK         (vs %s)\n' "$pkgid" "$baseline"
      broke=$((broke+1))
      while IFS= read -r l; do break_lines+=("    $pkgid: $l"); done \
        < <(grep -oE 'error CP[0-9]+: .*' "$log" | sed -E 's/ \[.*//' | sort -u)
      echo "::warning title=ApiCompat break in $pkgid::public-API break vs baseline $baseline (see job log)"
    elif is_stale_suppression "$log"; then
      printf '  %-28s STALE SUPPRESSION (vs %s — the release it was written for is PUBLISHED; delete it)\n' \
        "$pkgid" "$baseline"
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
echo "summary: OK=$ok  BREAK=$broke  StaleSuppression=$stale  NoBaselineYet=$nobaseline  Indeterminate=$indeterminate  FeedUnavailable=$feedunavailable  (total ${#projects[@]}, compared $compared)"

if [ "$broke" -gt 0 ]; then
  echo
  echo "breaking changes (force a SemVer major, or suppress deliberately with ApiCompatGenerateSuppressionFile):"
  printf '%s\n' "${break_lines[@]}"
fi
if [ "$stale" -gt 0 ]; then
  echo
  echo "STALE SUPPRESSION — the release these were written for is PUBLISHED, so they suppress nothing:"
  printf '%s\n' "${stale_lines[@]}"
  echo "  Fix: DELETE the entries above. If that empties the file, delete the file."
  echo "  Do NOT run ApiCompatGenerateSuppressionFile — it would re-add whatever is breaking TODAY and hide it."
  summarize "### API compatibility gate — $stale packable(s) carry a STALE suppression" "" \
            "A transient \`CompatibilitySuppressions.xml\` outlived the release it was written for. The baseline moved to the version that just published, which no longer has the member either — so the entry suppresses nothing and .NET fails the pack." "" \
            "This is a **deletion**, not a regeneration. It is the chore #776 exists to stop losing." "" \
            '```' "${stale_lines[@]}" '```'
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
# 4 = a suppression outlived its release. Distinct from 3 (could not compare) ON PURPOSE: the gate RAN
# here, and the fix is a one-line deletion rather than an investigation. Conflating the two is the whole
# of #776.
[ "$stale" -gt 0 ] && exit 4
[ "$indeterminate" -gt 0 ] && exit 3
exit 0
