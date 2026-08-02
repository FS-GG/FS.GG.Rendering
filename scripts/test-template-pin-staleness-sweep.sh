#!/usr/bin/env bash
# test-template-pin-staleness-sweep — behavioural tests for
# .github/workflows/template-pin-staleness-sweep.yml (FS.GG.Rendering#1106).
#
# #1102/#1105 moved `pin-lags-feed` out of the PR lane and into that scheduled sweep, which RENDERS a
# tracking issue and FILES it. The renderer is ~120 lines of real bash — it parses `printDrift`'s
# `DRIFT [rule] location` lines out of stderr, pulls each finding's expected/actual back out with
# `grep -A3`, branches three ways on `rc`/`REPORTED`, and emits an axis-aware `Paths:` line — and
# nothing exercised it against a planted input. `TemplatePayloadPinsWaiverTests` pins the workflow's
# SHAPE (it exists, it is scheduled, its `Verdict` excludes `pull_request`, its body says `Paths:`);
# it never runs the renderer.
#
# AND THE OBVIOUS PLACE A TEST WOULD HAVE COME FROM DOES NOT WORK — the same trap
# `scripts/test-skill-refs-sweep.sh` was written for, one workflow over. The sweep has a
# `pull_request` trigger on its own file precisely so a change to it is checked BY it. But the
# renderer only runs when the sweep is RED, and the sweep is red only when somebody in another
# repository has published a newer package. On a normal day the pins are current, `rc=0`, and the
# render step is SKIPPED: a green check over a subject it never examined. That is the
# `FS-GG/.github#416` shape, sitting in the workflow whose entire job is to keep a signal honest.
#
# A sweep's failure mode is SILENCE. If the parser drifts, the sweep files a body describing the
# wrong thing — or files "could not complete" over a run that completed fine and found real drift —
# and a scheduled run notifies almost nobody. Since #1102 this sweep is the ONLY thing in the
# repository asserting that a scaffolded product's component pins are not stale (the property #235
# was filed for), so its silence is the whole risk.
#
# THE SUBJECT IS THE YAML, NOT A COPY OF IT. Every `run:` block executed here is EXTRACTED from the
# workflow at test time and run verbatim. Nothing is transcribed. A suite that re-implemented the
# renderer would pass forever while the workflow rotted beside it — which is the same defect one
# level up, and this file does not get to commit it either.
#
# WHAT CANNOT BE EXTRACTED IS PINNED INSTEAD (§0). The step `if:` conditions, the `DRY_RUN` env and
# the trigger are GitHub expressions, not bash; no harness in this language can evaluate them. So
# `pipeline()` MODELS them and §0 asserts the model still matches the YAML.
#
# THE TWO-ENDED CONTRACT, WITHOUT A NETWORK (§4). `test-skill-refs-sweep.sh` can run its real subject
# — check-skill-refs.sh is bash and its `gh` is fakeable. This sweep's subject is
# `validate-template-payload-pins.fsx`, whose staleness lane exists to ask nuget.org over HTTPS at a
# hard-coded URL. Running it here would mean either a network call (a suite that reds when nuget.org
# has an outage, and whose findings depend on what the world published that morning) or a fake feed,
# which is a much larger fiction than a fake `gh`.
#
# So the contract is asserted against the SCRIPT'S SOURCE instead, and — this is the part that makes
# it real rather than a second transcription — every planted stderr in this file is BUILT FROM the
# format strings lifted out of that source. `printDrift`'s four `eprintfn` formats, the `Location`
# and `Expected` shapes of the `pin-lags-feed` failure, the rule name, and the STALE banner are all
# read out of the .fsx at test time. Reword any of them and the fixtures change under the workflow's
# real `grep`/`sed`, and these cases go RED. That is the drift the sweep cannot self-diagnose.
#
# Usage: scripts/test-template-pin-staleness-sweep.sh [-v]
set -uo pipefail

VERBOSE=0
[[ ${1:-} == -v ]] && VERBOSE=1

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WF="$REPO_ROOT/.github/workflows/template-pin-staleness-sweep.yml"
[[ -f $WF ]] || { echo "test-template-pin-staleness-sweep: cannot find $WF" >&2; exit 2; }

# Exit 2 (not 1) throughout the preflight: a suite that could not RUN is a different fact from a
# suite that ran and found a bug, and a harness about not-conflating-those does not get to conflate
# them itself.
command -v python3 >/dev/null || { echo "test-template-pin-staleness-sweep: python3 is required" >&2; exit 2; }
python3 -c 'import yaml' 2>/dev/null || {
  echo "test-template-pin-staleness-sweep: PyYAML is required (python3 -m pip install pyyaml)" >&2; exit 2; }

# The subject here is a workflow STEP, not a script — so `bad` says so when it dumps the output.
HARNESS_OUTPUT_LABEL='step output'
# SCRIPTDIR, or the `source=` below resolves against the CWD and finds nothing.
# shellcheck source-path=SCRIPTDIR
# shellcheck source=lib/test-harness.sh
. "$REPO_ROOT/scripts/lib/test-harness.sh"
harness_init

# ── the workflow, as data ───────────────────────────────────────────────────────────────────────
#
# A real YAML parse, not a regex over the file: a workflow that does not PARSE fails this suite.
# `on:` is remapped because YAML 1.1 reads a bare `on` as the boolean true, so PyYAML hands back a
# `True` key and every naive lookup of "on" silently misses.
WF_JSON=$(python3 - "$WF" <<'PY'
import json, sys, yaml
with open(sys.argv[1]) as f:
    doc = yaml.safe_load(f)
if True in doc:                      # YAML 1.1: `on:` parses as the boolean true
    doc["on"] = doc.pop(True)
json.dump(doc, sys.stdout)
PY
) || { echo "test-template-pin-staleness-sweep: $WF does not parse as YAML" >&2; exit 2; }

wf()       { jq -r "$1" <<<"$WF_JSON"; }
step_run() { jq -r --arg id "$1" '.jobs.sweep.steps[] | select(.id==$id) | .run // ""' <<<"$WF_JSON"; }
step_if()  { jq -r --arg id "$1" '.jobs.sweep.steps[] | select(.id==$id) | .if  // ""' <<<"$WF_JSON"; }
step_env() { jq -r --arg id "$1" --arg k "$2" '.jobs.sweep.steps[] | select(.id==$id) | .env[$k] // ""' <<<"$WF_JSON"; }

SWEEP_SH="$TMPROOT/sweep.sh"
RENDER_SH="$TMPROOT/render.sh"
RECONCILE_SH="$TMPROOT/reconcile.sh"
step_run sweep     >"$SWEEP_SH"
step_run render    >"$RENDER_SH"
step_run reconcile >"$RECONCILE_SH"

DRIFT_LABEL=$(wf '.env.DRIFT_LABEL')
DRIFT_TITLE=$(wf '.env.DRIFT_TITLE')

# The staleness lane's env switch, read out of the sweep step rather than assumed. It is what makes
# `validate-template-payload-pins.fsx` evaluate `pin-lags-feed` AT ALL: without it the script runs
# the structural lane and exits 0, which is a GREEN that judged no pin's freshness (#1102). §0
# asserts it is still there, and the fake `dotnet` records the value it was actually handed.
SWEEP_LANE_ENV='FS_GG_TEMPLATE_PIN_STALENESS_SWEEP'
SWEEP_LANE_VALUE=$(step_env sweep "$SWEEP_LANE_ENV")

# ── the script under the sweep, and the formats this workflow reads it by ───────────────────────
#
# DERIVED FROM THE WORKFLOW, not hard-coded: the sweep step names the .fsx it runs, so that is where
# the suite goes looking. Rename the script in the workflow alone and the existence check below
# fails, which is the honest answer.
FSX_REL=$(grep -oE '[A-Za-z0-9_./-]+\.fsx' "$SWEEP_SH" | head -n1)
[[ -n $FSX_REL ]] || {
  echo "test-template-pin-staleness-sweep: the sweep step names no .fsx to run" >&2; exit 2; }
FSX="$REPO_ROOT/$FSX_REL"
[[ -f $FSX ]] || {
  echo "test-template-pin-staleness-sweep: the sweep step runs $FSX_REL, which does not exist" >&2; exit 2; }

# fsx_lit <start-substring> <hit-substring> — the first double-quoted literal on the first line
# CONTAINING <hit-substring>, at or after the first line containing <start-substring>. Both are
# FIXED substrings (awk `index`, never a regex): the anchors are F# source with brackets, dollars and
# parentheses in them, and a regex would silently mean something else.
#
# TWO anchors, because the interesting literals are not unique in the file — and because the value
# must never be its own search key. Grepping for "pin-lags-feed" to discover that the rule is called
# `pin-lags-feed` proves nothing; anchoring on the enclosing `let` and on the CONTROL FLOW does.
fsx_lit() {
  local line
  line=$(awk -v a="$1" -v b="$2" '
    index($0, a) { f = 1 }
    f && index($0, b) { print; exit }
  ' "$FSX")
  [[ -n $line ]] || {
    echo "test-template-pin-staleness-sweep: $FSX_REL has no line containing '$2' after '$1' — the source moved" >&2
    exit 2; }
  local lit
  lit=$(sed -n 's/^[^"]*"\([^"]*\)".*/\1/p' <<<"$line")
  [[ -n $lit ]] || {
    echo "test-template-pin-staleness-sweep: no string literal on: $line" >&2; exit 2; }
  printf '%s' "$lit"
}

# fsx_lit_after <start-substring> <flow-substring> — the first line AFTER <flow-substring> whose
# first non-blank character is a double quote: F#'s wrapped `eprintfn` argument. For the two BANNERS
# the sweep step keys on, this is what keeps the search honest — the key is the CONTROL FLOW that
# reaches the print, never a fragment of the sentence being looked for.
fsx_lit_after() {
  local line
  line=$(awk -v a="$1" -v b="$2" '
    index($0, a) { f = 1 }
    f && index($0, b) { g = 1; next }
    g && /^[ \t]*"/ { print; exit }
  ' "$FSX")
  [[ -n $line ]] || {
    echo "test-template-pin-staleness-sweep: $FSX_REL prints no literal after '$2' — the source moved" >&2
    exit 2; }
  local lit
  lit=$(sed -n 's/^[^"]*"\([^"]*\)".*/\1/p' <<<"$line")
  [[ -n $lit ]] || {
    echo "test-template-pin-staleness-sweep: no string literal on: $line" >&2; exit 2; }
  printf '%s' "$lit"
}

# `|| exit 2` on EVERY one of these, and it is not decoration: `fsx_lit` runs in a command
# substitution, so its `exit 2` ends the SUBSHELL and hands back an empty string. Unchecked, a moved
# source would leave every format empty, every planted finding blank, and this whole suite green over
# a subject it never described — the #266 shape, in the file written to refuse it.

# `printDrift` — the four lines it writes per failure. THE contract between the script and this
# workflow: the first is what the sweep step greps, the second and third are what the render step
# pulls back out with `grep -A3`.
FMT_DRIFT=$(fsx_lit 'let printDrift'      'eprintfn "DRIFT [')     || exit 2
FMT_EXPECTED=$(fsx_lit 'let printDrift'   'eprintfn "  expected:') || exit 2
FMT_ACTUAL=$(fsx_lit 'let printDrift'     'eprintfn "  actual:')   || exit 2
FMT_FIX=$(fsx_lit 'let printDrift'        'eprintfn "  fix:')      || exit 2

# The `pin-lags-feed` failure's own shape, from `stalenessFailures`.
STALE_RULE=$(fsx_lit 'let stalenessFailures' 'Rule = "')            || exit 2
FMT_LOCATION=$(fsx_lit 'let stalenessFailures' 'Location = sprintf "') || exit 2
FMT_EXPECTED_VALUE=$(fsx_lit 'let stalenessFailures' 'Expected = sprintf "') || exit 2
PROPS_REL=$(fsx_lit 'let propsRel' 'let propsRel')                  || exit 2

# The STALE banner — the sentence the sweep step keys `reported=` on. Taken as the first bare string
# literal after the staleness lane calls `printDrift stale`, so the search key is the CONTROL FLOW
# and never the sentence itself.
BANNER_STALE=$(fsx_lit_after 'if stalenessSweep then' 'printDrift stale') || exit 2
# …and the fail-closed one, printed when the lane cannot run at all (exit 2).
BANNER_CANNOT_RUN=$(fsx_lit_after 'if stalenessSweep then' 'printDrift structural') || exit 2
# A structural rule name, for the exit-2 fixture. Its `DRIFT` lines are real and must NOT be counted
# as findings by a sweep step that greps for one rule.
STRUCTURAL_RULE=$(fsx_lit 'let structuralFailures' 'Rule = "')      || exit 2

# The axis names the script actually knows, and the one the workflow special-cases.
axis_names() {
  awk '/^let axes =/ { f = 1 } f { print } f && /\]/ { exit }' "$FSX" \
    | grep -oE '"Fs[A-Za-z]*Version"' | tr -d '"'
}
# The token the RENDER/SWEEP steps branch on for the wide `Paths:` line, read off the workflow.
CONTRACTS_AXIS=$(grep -oE "grep -qF '(Fs[A-Za-z]*Version)'" "$SWEEP_SH" | head -n1 | sed "s/.*'\(.*\)'/\1/")
OTHER_AXIS=$(axis_names | grep -v -x "$CONTRACTS_AXIS" | head -n1)
MIRROR_AXIS=$(axis_names | grep -E '^FsGg(Game|Audio)Version$' | head -n1)
SECOND_MIRROR_AXIS=$(axis_names | grep -E '^FsGg(Game|Audio)Version$' | tail -n1)
NON_MIRROR_AXIS=$(grep -oE '"FsGgUiVersion"' "$FSX" | head -n1 | tr -d '"')
[[ -n $CONTRACTS_AXIS && -n $OTHER_AXIS && -n $MIRROR_AXIS && -n $SECOND_MIRROR_AXIS && -n $NON_MIRROR_AXIS ]] || {
  echo "test-template-pin-staleness-sweep: could not derive the axes (contracts='$CONTRACTS_AXIS' other='$OTHER_AXIS' mirror='$MIRROR_AXIS' second-mirror='$SECOND_MIRROR_AXIS' non-mirror='$NON_MIRROR_AXIS')" >&2
  exit 2; }

# expand <printf-style-format> <arg>… — substitute %s/%d left to right. F# `sprintf`/`eprintfn` and
# this share exactly enough grammar for the formats above; nothing here needs widths or flags.
expand() {
  local rest=$1 out=""; shift
  while [[ $rest == *%* ]] && (($#)); do
    out+=${rest%%%*}      # everything before the first %
    rest=${rest#*%}       # everything after it
    out+=$1; shift
    rest=${rest:1}        # drop the conversion character
  done
  printf '%s%s' "$out" "$rest"
}

# drift_block <rule> <location> <expected> <actual> <fix> — the four lines the script would print,
# in the script's own formats.
drift_block() {
  expand "$FMT_DRIFT"    "$1" "$2"; printf '\n'
  expand "$FMT_EXPECTED" "$3";      printf '\n'
  expand "$FMT_ACTUAL"   "$4";      printf '\n'
  expand "$FMT_FIX"      "$5";      printf '\n'
}

# lag <axis> <pinned> <newest> — one real `pin-lags-feed` failure, rendered exactly as
# `stalenessFailures` + `printDrift` would render it.
lag() {
  drift_block "$STALE_RULE" \
              "$(expand "$FMT_LOCATION" "$PROPS_REL" "$1")" \
              "$(expand "$FMT_EXPECTED_VALUE" "$3" 'stable version')" \
              "$2" \
              "bump \$($1) to $3 — follow docs/ci/cadence-map.md §4b"
}

# ── fixture ─────────────────────────────────────────────────────────────────────────────────────
#
# One fixture = one simulated workflow run. RUNNER_TEMP is shared across the three steps exactly as
# it is on a runner, which is what lets the suite drive the REAL wiring: `sweep` writes findings.txt
# and its outputs, `render` reads them and writes body.md, `reconcile` reads that.
#
# The subject of the sweep step is `dotnet fsi <script>`, so the stub is a fake `dotnet`. It also
# RECORDS what it was asked to do — argv and the lane env — because a sweep step that stopped
# invoking the staleness lane would otherwise pass every case in this file.
fixture() {
  fixture_new
  mkdir -p "$FIX/runner-temp"
  : >"$FIX/github-output"
  : >"$FIX/step-summary"
  : >"$FIX/dotnet.log"
  echo 0 >"$FIX/stub.rc"
  : >"$FIX/stub.out"
  : >"$FIX/stub.err"

  cat >"$FIX/bin/dotnet" <<'STUB'
#!/usr/bin/env bash
printf '%s\n' "$*" >>"$FSGG_FIX/dotnet.log"
printf 'lane=%s\n' "${FS_GG_TEMPLATE_PIN_STALENESS_SWEEP-<unset>}" >>"$FSGG_FIX/dotnet.log"
cat "$FSGG_FIX/stub.out"
cat "$FSGG_FIX/stub.err" >&2
exit "$(cat "$FSGG_FIX/stub.rc")"
STUB
  chmod +x "$FIX/bin/dotnet"
}

# ── what the subject says ───────────────────────────────────────────────────────────────────────

# UP TO DATE: exit 0, nothing on stderr worth parsing.
sweep_green() {
  echo 0 >"$FIX/stub.rc"
  printf 'template payload pins: UP TO DATE — every feed-owned axis equals newest stable on nuget.org.\n' \
    >"$FIX/stub.out"
  : >"$FIX/stub.err"
}

# STALE: the drift blocks on stdin, then the script's own banner. Exit 1.
sweep_stale() {
  echo 1 >"$FIX/stub.rc"
  local n=${1:-1}
  { cat; expand "$BANNER_STALE" "$n"; printf '\n'; } >"$FIX/stub.err"
}

# The lane could not run: structural failures, the fail-closed banner, exit 2. Its `DRIFT` lines are
# REAL — they are simply a different rule, and the sweep must not count them as staleness findings.
sweep_cannot_run() {
  echo 2 >"$FIX/stub.rc"
  { drift_block "$STRUCTURAL_RULE" "$PROPS_REL (line 42)" 'an axis-derived pin' 'a bare literal' 'derive it'
    expand "$BANNER_CANNOT_RUN" 1 "$PROPS_REL"; printf '\n'; } >"$FIX/stub.err"
}

# The contract DRIFTED: the script says it found drift, but in a shape this workflow cannot parse.
sweep_drifted() {
  echo 1 >"$FIX/stub.rc"
  { printf 'drift! %s is behind\n' "$PROPS_REL"
    expand "$BANNER_STALE" 1; printf '\n'; } >"$FIX/stub.err"
}

# The sweep never ran to a verdict: non-zero, no findings, no banner.
sweep_broken() {
  echo 1 >"$FIX/stub.rc"
  printf 'MSBUILD : error MSB1003: Specify a project or solution file.\n' >"$FIX/stub.err"
}

# plant <state> — the six sweep outcomes this workflow has to tell apart, named once so a case that
# wants all of them can loop over the names instead of carrying six heredocs.
plant() {
  case $1 in
    lagging)    sweep_stale 1 <<<"$(lag "$NON_MIRROR_AXIS" 4.1.0 4.3.0)" ;;
    mirror)     sweep_stale 1 <<<"$(lag "$MIRROR_AXIS" 4.1.0 4.3.0)" ;;
    mirror_two) sweep_stale 1 <<<"$(lag "$SECOND_MIRROR_AXIS" 4.1.0 4.3.0)" ;;
    contracts)  sweep_stale 1 <<<"$(lag "$CONTRACTS_AXIS" 7.0.0 7.2.0)" ;;
    two)        sweep_stale 2 <<<"$(lag "$OTHER_AXIS" 4.1.0 4.3.0)
$(lag "$CONTRACTS_AXIS" 7.0.0 7.2.0)" ;;
    cannot_run) sweep_cannot_run ;;
    drifted)    sweep_drifted ;;
    broken)     sweep_broken ;;
    *) echo "test-template-pin-staleness-sweep: no such planted state: $1" >&2; exit 2 ;;
  esac
}

# ── running a step ──────────────────────────────────────────────────────────────────────────────

REPO=FS-GG/FS.GG.Rendering
SERVER=https://github.com
SHA=abc1234def5678
RUN_URL="$SERVER/$REPO/actions/runs/999"

step_base_env() {
  gh_env
  echo "FSGG_FIX=$FIX"
  echo "RUNNER_TEMP=$FIX/runner-temp"
  echo "GITHUB_OUTPUT=$FIX/github-output"
  echo "GITHUB_STEP_SUMMARY=$FIX/step-summary"
  echo "GITHUB_SERVER_URL=$SERVER"
  echo "GH_TOKEN=fake-token"
  echo "GH_REPO=$REPO"
  echo "DRIFT_LABEL=$DRIFT_LABEL"
  echo "DRIFT_TITLE=$DRIFT_TITLE"
}

out_of() { sed -n "s/^$1=//p" "$FIX/github-output" | tail -1; }

run_sweep() {
  local -a e=(env) base
  mapfile -t base < <(step_base_env); e+=("${base[@]}")
  # The step's own `env:`, as the YAML declares it — not as this file guesses it.
  e+=("$SWEEP_LANE_ENV=$SWEEP_LANE_VALUE")
  OUT=$(cd "$FIX" && "${e[@]}" bash "$SWEEP_SH" 2>&1); RC=$?
  ((VERBOSE)) && { printf '    ── sweep\n'; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

run_render() {
  # DRY_RUN is `${{ github.event_name == 'pull_request' }}` — which GitHub hands the shell as the
  # STRING "true"/"false", never the event name. §0 pins the expression this stands in for.
  local dry=false
  [[ ${EVENT:-schedule} == pull_request ]] && dry=true
  local -a e=(env) base
  mapfile -t base < <(step_base_env); e+=("${base[@]}")
  e+=("REPO=$REPO" "SERVER=$SERVER" "SHA=$SHA" "RUN_URL=$RUN_URL"
      "FINDINGS=$(out_of findings)" "REPORTED=$(out_of reported)"
      "CONTRACTS=$(out_of contracts)" "CROSS_REPO_MIRROR=$(out_of cross_repo_mirror)" "RC=$(out_of rc)" "DRY_RUN=$dry")
  OUT=$(cd "$FIX" && "${e[@]}" bash "$RENDER_SH" 2>&1); RC=$?
  ((VERBOSE)) && { printf '    ── render\n'; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

run_reconcile() {
  local -a e=(env) base
  mapfile -t base < <(step_base_env); e+=("${base[@]}")
  e+=("REPO=$REPO" "SHA=$SHA" "RUN_URL=$RUN_URL" "RC=$(out_of rc)")
  [[ ${GH_LABEL_EXISTS:-0}     == 1 ]] && e+=("GH_LABEL_EXISTS=1")
  [[ ${GH_LABEL_POST_FAILS:-0} == 1 ]] && e+=("GH_LABEL_POST_FAILS=1")
  [[ -n ${GH_NEW_NUM:-} ]] && e+=("GH_NEW_NUM=$GH_NEW_NUM")
  OUT=$(cd "$FIX" && "${e[@]}" bash "$RECONCILE_SH" 2>&1); RC=$?
  ((VERBOSE)) && { printf '    ── reconcile\n'; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

# One simulated run, gated exactly as the workflow gates its steps (§0 pins those conditions).
#   EVENT=pull_request  → render runs, reconcile does NOT
#   EVENT=schedule      → both run, and render only when the sweep was red
pipeline() {
  run_sweep
  [[ $(out_of rc) != 0 ]] && run_render
  [[ ${EVENT:-schedule} != pull_request ]] && run_reconcile
  return 0
}

body()    { cat "$FIX/runner-temp/body.md" 2>/dev/null; }
summary() { cat "$FIX/step-summary" 2>/dev/null; }
any_payload() { cat "$FIX"/gh-bodies/body-*.json 2>/dev/null; }

# The `Class:`/`Paths:` lines the ENGINE would read out of the rendered body: `^ {0,3}Class:` OUTSIDE
# any fence. Modelled rather than shared — the engine is F# and this is bash — and fence-aware for a
# concrete reason: the "could not decide" bodies embed 40 lines of the script's stderr inside a code
# fence, so anything a failing build happened to print could otherwise be read as the item's class.
outside_fences() {
  awk '
    /^ ? ? ?```/ { fence = !fence; next }
    !fence       { print }
  ' <<<"$(body)"
}
class_lines() { outside_fences | grep -E '^ ? ? ?[Cc]lass:' || true; }
paths_lines() { outside_fences | grep -E '^ ? ? ?Paths: '   || true; }

# expect_paths_real <what> — every token on the body's `Paths:` line names something that EXISTS.
# A finding filed with a touch-set that matches no file is refused by `take` outright
# (FS-GG/.github#442): it lands on the board looking like work and is invisible to every worker who
# asks for work. The sweep writes that line unattended, so nothing but this asserts it.
expect_paths_real() {
  local line toks t; local -a missing=()
  line=$(paths_lines | head -n1)
  if [[ -z $line ]]; then bad "$1" 'the body carries no Paths: line at all'; return; fi
  read -ra toks <<<"${line#*Paths: }"
  if ((${#toks[@]} == 0)); then bad "$1" 'the Paths: line names no tokens'; return; fi
  for t in "${toks[@]}"; do [[ -e "$REPO_ROOT/$t" ]] || missing+=("$t"); done
  if ((${#missing[@]})); then
    bad "$1" "Paths: names ${#missing[@]} token(s) that do not exist in this tree: ${missing[*]}"
  else
    ok "$1"
  fi
}

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 0  THE WIRING THIS SUITE CANNOT EXECUTE — so it pins it instead
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§0 the harness is executing the workflow'"'"'s real run: blocks, not a copy of them'
expect_has 'findings=' "$(cat "$SWEEP_SH")"     'the sweep block came out of the YAML'
expect_has 'Paths:'    "$(cat "$RENDER_SH")"    'the render block came out of the YAML'
expect_has 'gh api'    "$(cat "$RECONCILE_SH")" 'the reconcile block came out of the YAML'
# An extractor that quietly returned nothing would make every case below pass vacuously — the exact
# green-over-an-unexamined-subject this file exists to refuse. So it is refused here first.
if [[ $(wc -l <"$SWEEP_SH") -ge 10 && $(wc -l <"$RENDER_SH") -ge 40 && $(wc -l <"$RECONCILE_SH") -ge 40 ]]; then
  ok 'all three blocks are substantial — extraction did not silently yield an empty subject'
else
  bad 'all three blocks are substantial' \
      "extracted $(wc -l <"$SWEEP_SH")/$(wc -l <"$RENDER_SH")/$(wc -l <"$RECONCILE_SH") lines (sweep/render/reconcile)"
fi

case_start '§0 the step conditions still say what pipeline() models'
expect_eq "$(step_if render)"    "steps.sweep.outputs.rc != '0'"     'render runs only when the sweep is red'
expect_eq "$(step_if reconcile)" "github.event_name != 'pull_request'" 'reconcile never runs on a pull_request'
expect_eq "$(step_env render DRY_RUN)" "\${{ github.event_name == 'pull_request' }}" \
          'DRY_RUN is the pull_request predicate'
# The verdict carries BOTH clauses. Dropping the second is FS.GG.Rendering#1114's defect one workflow
# over: a `pull_request` run then fails the job because somebody else published a package — which is
# the accusation #1102 abolished, re-committed by the workflow that abolished it.
expect_eq "$(jq -r '.jobs.sweep.steps[] | select(.name=="Verdict") | .if' <<<"$WF_JSON")" \
          "steps.sweep.outputs.rc != '0' && github.event_name != 'pull_request'" \
          'the verdict re-raises only on a scheduled/dispatched red, never on a PR'

case_start '§0 the pull_request trigger still fires on the sweep'"'"'s own code'
expect_has '.github/workflows/template-pin-staleness-sweep.yml' "$(wf '.on.pull_request.paths[]')" \
           'a change to the workflow is checked by the workflow'
expect_has "$FSX_REL" "$(wf '.on.pull_request.paths[]')" \
           'a change to the script it runs is checked by the workflow'
expect_eq "$(wf '.permissions.issues')" 'write' 'the job can still write the issue it files'
expect_has 'schedule' "$(wf '.on | keys | join(" ")')" 'the sweep is still SCHEDULED — this rule gates no merge'
expect_eq "$(wf '.on.schedule[0].cron')" '20 7 * * *' 'twenty minutes after the skill-refs sweep, so the two never contend'

case_start '§0 the sweep step still runs the STALENESS lane, and it is the lane that judges freshness'
# Without this env the script runs the STRUCTURAL lane and exits 0 — a green that judged no pin's
# freshness at all. The value is read out of the YAML, so removing it makes this fail rather than
# quietly turning every case below into a test of the wrong lane.
expect_eq "$SWEEP_LANE_VALUE" '1' "the sweep step still sets $SWEEP_LANE_ENV=1"
fixture
sweep_green
run_sweep
expect_has "fsi $FSX_REL" "$(cat "$FIX/dotnet.log")" 'the step really invoked the script it names'
expect_has 'lane=1'       "$(cat "$FIX/dotnet.log")" 'and the script really received the staleness lane'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 1  THE SWEEP STEP — parsing the script's findings out of its stderr
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Four outcomes must stay distinct — parsed, could-not-run (exit 2), drifted, never-ran — because the
# render step branches on exactly those and each branch sends a different person somewhere different.
# Calling one by another's name points the fixer at the wrong thing while the real problem sits unread.

case_start '§1 UP TO DATE reports rc=0, no findings, nothing to file'
fixture
sweep_green
run_sweep
expect_rc 0 'the step succeeds'
expect_eq "$(out_of rc)"        '0'     'rc=0'
expect_eq "$(out_of findings)"  '0'     'no findings'
expect_eq "$(out_of reported)"  'false' 'the script printed no STALE banner'
expect_eq "$(out_of contracts)" 'false' 'and no axis is implicated'

case_start '§1 ONE lagging axis is parsed and counted'
fixture
plant lagging
run_sweep
expect_rc 0 'the step carries the failure rather than dying on it'
expect_eq "$(out_of rc)"        '1'     'the script'"'"'s exit code is carried, not discarded'
expect_eq "$(out_of findings)"  '1'     'one finding parsed'
expect_eq "$(out_of reported)"  'true'  'the STALE banner was seen'
expect_eq "$(out_of contracts)" 'false' 'and it is not the Contracts axis'

case_start '§1 TWO lagging axes are both parsed, and the Contracts one is flagged'
fixture
plant two
run_sweep
expect_eq "$(out_of findings)"  '2'    'both findings parsed'
expect_eq "$(out_of contracts)" 'true' 'the expensive axis is recognised — it decides the touch-set'
expect_eq "$(wc -l <"$FIX/runner-temp/findings.txt" | tr -d ' ')" '2' \
          'findings.txt holds the two header lines and nothing else'

case_start '§1 the banner and the continuation lines are NOT mistaken for findings'
# `printDrift` writes four lines per failure and the script signs off with a banner. Grep the whole
# stderr instead of the `DRIFT [rule]` headers and one lagging axis becomes five.
fixture
plant contracts
run_sweep
expect_eq "$(out_of findings)" '1' 'one failure is one finding, not one per printed line'
expect_hasnt 'expected:' "$(cat "$FIX/runner-temp/findings.txt")" 'a continuation line is not a finding'

case_start '§1 exit 2 — the lane could not run, and its structural DRIFT lines are not staleness'
# `validate-template-payload-pins.fsx` fails CLOSED (FS-GG/.github#266): "nothing to check" and
# "checked, and it's fine" must not share an exit code. Its structural findings are REAL DRIFT lines
# under a different rule, and a sweep that counted them would file them as pin staleness — the
# required `gate` job's finding, misattributed to a sweep that never judged freshness.
fixture
sweep_cannot_run
run_sweep
expect_eq "$(out_of rc)"       '2'     'the fail-closed code is carried'
expect_eq "$(out_of findings)" '0'     'a structural DRIFT line is not a pin-lags-feed finding'
expect_eq "$(out_of reported)" 'false' 'and the STALE banner is absent — nothing was swept'

case_start '§1 DRIFT: the script reported, but this workflow could not parse it'
fixture
sweep_drifted
run_sweep
expect_eq "$(out_of rc)"       '1'    'still red'
expect_eq "$(out_of findings)" '0'    'nothing parsed'
expect_eq "$(out_of reported)" 'true' 'but the script SAYS it reported — the contract has drifted'

case_start '§1 BROKEN: the sweep never judged an axis at all'
fixture
sweep_broken
run_sweep
expect_eq "$(out_of rc)"       '1'     'red'
expect_eq "$(out_of findings)" '0'     'nothing parsed'
expect_eq "$(out_of reported)" 'false' 'and it never claimed to report — the run itself failed'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 2  THE RENDER STEP — four bodies, and they must never be confused for one another
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§2 STALE body: names the axis, both versions, and the swept commit'
fixture
plant lagging
run_sweep
run_render
expect_rc 0 'the renderer runs clean'
B=$(body)
expect_has 'The daily template-payload pin sweep is **red**' "$B" 'names the finding for what it is'
expect_has '**1 lagging axis(es)**' "$B" 'counts them'
expect_has "- \`\$($NON_MIRROR_AXIS)\` — pinned at \`4.1.0\`, newest on the feed is \`4.3.0\`" "$B" \
           'states BOTH sides, from the script'"'"'s own expected/actual lines'
expect_has "commit/$SHA" "$B" 'links the commit that was swept'
expect_hasnt 'could not decide' "$B" 'does not also cry infra failure'
# `${axis:-unknown}` / `${act:-?}` / `${exp:-?}` are the renderer's fallbacks. They are the shape a
# parse failure takes — a body that reads "`$(unknown)` — pinned at `?`, newest is `?`" is a filed
# issue that names nothing and sends nobody anywhere, and it is not distinguishable from a real
# finding by anything else in this pipeline.
expect_hasnt '$(unknown)' "$B" 'the axis was EXTRACTED from the DRIFT line, not defaulted'
expect_hasnt '`?`'        "$B" 'and so were both versions'

case_start '§2 STALE body: TWO lagging axes are both listed'
fixture
plant two
run_sweep
run_render
B=$(body)
expect_has '**2 lagging axis(es)**' "$B" 'counts both'
expect_has "- \`\$($OTHER_AXIS)\` — pinned at \`4.1.0\`, newest on the feed is \`4.3.0\`" "$B" 'the first axis'
expect_has "- \`\$($CONTRACTS_AXIS)\` — pinned at \`7.0.0\`, newest on the feed is \`7.2.0\`" "$B" \
           'the second axis, with ITS versions and not the first one'"'"'s'

case_start '§2 STALE body: the narrow touch-set when no cross-repository mirror axis is implicated'
fixture
plant lagging
pipeline
B=$(body)
expect_paths_real 'every token on the narrow Paths: line exists in this tree'
expect_hasnt 'scripts/api-surface-manifest.txt' "$(paths_lines)" \
             'the api-surface chain is NOT reserved on a non-mirror bump'
expect_has 'no axis with a cross-repository api-surface mirror' "$B" 'and the body says why the chain is not in play'

case_start '§2 STALE body: the M-PROV touch-set when a Game or Audio mirror axis is implicated'
fixture
plant mirror
pipeline
B=$(body)
expect_paths_real 'every token on the M-PROV Paths: line exists in this tree'
expect_has 'scripts/api-surface-manifest.txt' "$(paths_lines)" 'the api-surface chain IS reserved'
expect_hasnt 'tests/Build.Tests/mirror-omission-ledger.txt' "$(paths_lines)" 'the Contracts-only omission ledger stays unreserved'
expect_has 'M-PROV provenance stamps' "$B" 'and the body identifies the Game/Audio obligation'

fixture
plant mirror_two
pipeline
expect_has 'scripts/api-surface-manifest.txt' "$(paths_lines)" 'the second Game/Audio mirror axis also reserves the api-surface chain'

case_start '§2 STALE body: the WIDE touch-set, and the ordered routine, when Contracts IS implicated'
fixture
plant contracts
pipeline
B=$(body)
expect_paths_real 'every token on the wide Paths: line exists in this tree'
expect_has 'scripts/api-surface-manifest.txt' "$(paths_lines)" 'the api-surface chain IS reserved'
expect_has 'tests/Build.Tests/mirror-omission-ledger.txt' "$(paths_lines)" 'as is the omission ledger'
expect_has 'it is NOT a one-line diff' "$B" 'the body refuses the one-line-bump reading'
expect_has '`docs/ci/cadence-map.md` §4b' "$B" \
           'and names the routine that lists every gate the bump moves, by section'

case_start '§2 the routine the body sends the fixer to is a section that EXISTS'
# The body's whole claim is "this is not a one-line diff, work the routine". A deep link whose
# anchor has rotted lands the fixer at the top of a long document with no routine in sight, and the
# sweep has no way to notice: it renders the link unattended and nothing else reads it.
CADENCE_TARGET=$(grep -oE 'docs/ci/cadence-map\.md#[a-z0-9-]+' "$RENDER_SH" | head -n1)
CADENCE_DOC=${CADENCE_TARGET%%#*}
CADENCE_ANCHOR=${CADENCE_TARGET#*#}
if [[ -z $CADENCE_TARGET ]]; then
  bad 'the body deep-links the reconciliation routine' \
      'the render block links no docs/ci/cadence-map.md anchor at all'
elif [[ ! -f "$REPO_ROOT/$CADENCE_DOC" ]]; then
  bad "\`$CADENCE_DOC\` exists" 'the body points every fixer at a document that is not in this tree'
else
  # GitHub's heading slug: lowercase, drop everything that is not alphanumeric/space/hyphen, then
  # spaces to hyphens. Modelled here because the alternative is trusting a link nothing renders.
  if grep -E '^#{1,6} ' "$REPO_ROOT/$CADENCE_DOC" \
     | sed -E 's/^#{1,6} //' \
     | tr '[:upper:]' '[:lower:]' \
     | sed -E 's/[^a-z0-9 -]//g; s/ /-/g' \
     | grep -qx -- "$CADENCE_ANCHOR"; then
    ok "\`#$CADENCE_ANCHOR\` is a real heading in \`$CADENCE_DOC\`"
  else
    bad "\`#$CADENCE_ANCHOR\` is a real heading in \`$CADENCE_DOC\`" \
        'the deep link has rotted — the fixer lands at the top of the document, with no routine'
  fi
fi
expect_has "$CADENCE_TARGET" "$(body)" 'and the rendered body really carries that link'

case_start '§2 exit 2 body: the pins are UNCHECKED — not "fine", and not drift'
fixture
sweep_cannot_run
run_sweep
run_render
B=$(body)
expect_has 'could not decide — the pins are UNCHECKED' "$B" 'names the fail-closed state'
expect_hasnt 'could not read it'  "$B" 'does NOT misreport it as a parser drift'
expect_hasnt 'is **red**'         "$B" 'and does NOT claim an axis lags'
expect_has 'Last 40 lines of the run' "$B" 'carries the evidence a human needs to diagnose it'

case_start '§2 DRIFT body: says the drift is REAL and that it cannot see it'
fixture
sweep_drifted
run_sweep
run_render
B=$(body)
expect_has 'The sweep found drift — and this workflow could not read it' "$B" 'names the drift'
expect_has '**The drift is real, and it is not listed here, because this workflow cannot see it.**' "$B" \
           'refuses to imply the pins are fine'
expect_hasnt 'the pins are UNCHECKED' "$B" 'does NOT misreport drift as an infra failure'

case_start '§2 BROKEN body: the sweep itself did not run, and says only that'
fixture
sweep_broken
run_sweep
run_render
B=$(body)
expect_has 'exited non-zero without naming an axis' "$B" 'names the infra failure'
expect_hasnt 'could not read it' "$B" 'does NOT misreport it as drift'
expect_hasnt 'is **red**'        "$B" 'and does NOT claim an axis lags'

case_start '§2 a PR run RENDERS — the half a green scheduled trigger never reaches'
fixture
EVENT=pull_request
plant contracts
pipeline
expect_has '**Dry run.**' "$(summary)" 'the PR run says it filed nothing'
expect_has 'The daily template-payload pin sweep is **red**' "$(summary)" \
           'but it DID render the body, into the step summary'
gh_no_writes 'a pull_request run writes NOTHING to the API'
unset EVENT

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 3  THE ROW THIS WORKFLOW FILES — schedulable, or it is not work
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Nobody but the sweep can put these lines there: the body is rewritten on every run, so a human's
# edit is erased the next morning. A finding filed without a touch-set is invisible to `take`
# (FS-GG/.github#442); one filed without a class reads as unclassed (FS-GG/.github#1651).

case_start '§3 every body carries exactly one Class: defect, at column 0'
for state in lagging contracts two cannot_run drifted broken; do
  fixture
  plant "$state"
  run_sweep
  run_render
  expect_eq "$(class_lines)" 'Class: defect' "the $state body declares exactly one class, and it is defect"
  expect_re '^Class: defect' "$(body)" "the $state body's class is a real line, not fenced evidence"
done

case_start '§3 every body carries a Paths: line naming only files that exist'
fixture
sweep_cannot_run
run_sweep
run_render
expect_paths_real 'the fail-closed body reserves the script and the workflow, and both exist'
fixture
sweep_broken
run_sweep
run_render
expect_paths_real 'so does the never-ran body'
fixture
sweep_drifted
run_sweep
run_render
expect_paths_real 'and the parser-drift body'

case_start '§3 a Class:/Paths: line inside the embedded stderr cannot be read as the item'"'"'s own'
# The fail-closed bodies embed 40 lines of the script's stderr in a code fence. A failing build that
# happened to print `Class: hardening` must not downgrade the row — the engine reads `Class:` outside
# fences only, and the fence is what makes that true here.
fixture
echo 1 >"$FIX/stub.rc"
{ printf 'Class: hardening\n'
  printf 'Paths: nowhere/at/all.txt\n'
  printf 'MSBUILD : error MSB1003\n'; } >"$FIX/stub.err"
run_sweep
run_render
expect_eq "$(class_lines)" 'Class: defect' 'the fenced `Class: hardening` is not the row'"'"'s class'
expect_paths_real 'and the fenced `Paths:` is not the row'"'"'s touch-set'
expect_has 'Class: hardening' "$(body)" 'the line IS still in the body — as quoted evidence, inside the fence'

case_start '§3 the class and the touch-set travel with the body onto the wire'
fixture
plant contracts
pipeline
P=$(any_payload)
expect_has 'Class: defect' "$P" 'the filed issue carries the class, not just the local render'
expect_has 'Paths: template/base/Directory.Packages.props' "$P" 'and the touch-set'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 4  THE TWO-ENDED CONTRACT — asserted against the SCRIPT'S SOURCE, in both directions
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Every fixture above is built from the format strings lifted out of the .fsx, so a reword there
# already reds §1–§3. This section states the contract explicitly, so a failure NAMES it instead of
# arriving as a mysterious "0 findings parsed" three sections up.
#
# It runs in BOTH directions, which is the point of the section:
#   script → workflow : the lines `printDrift` really emits still satisfy the workflow's `grep`/`sed`
#   workflow → script : the literals the workflow greps for are still ones the script really prints

case_start '§4 the RULE the sweep greps for is the rule stalenessFailures declares'
expect_has "$STALE_RULE" "$(cat "$SWEEP_SH")" \
           "the workflow greps for \`$STALE_RULE\`, which is what \`stalenessFailures\` yields"
expect_hasnt "$STRUCTURAL_RULE" "$(cat "$SWEEP_SH")" \
             'and NOT for a structural rule — that is the required gate'"'"'s finding, not the sweep'"'"'s'

case_start '§4 the workflow'"'"'s grep matches the header line printDrift really writes'
# Direction one, mechanically: take the header format from the source, expand it, and run the
# workflow's OWN grep over it. A `printDrift` that stopped writing `DRIFT [rule] location` — or a
# workflow that tightened its pattern — fails here by name.
GREP_RE=$(python3 - "$SWEEP_SH" <<'PY'
import re, sys
m = re.search(r"grep -E '([^']+)'", open(sys.argv[1]).read())
print(m.group(1) if m else "")
PY
)
if [[ -z $GREP_RE ]]; then
  bad 'the sweep step still greps for its findings with a pattern' \
      'no `grep -E ...` in the sweep block — this section cannot check anything, so it refuses to pass'
else
  HEADER=$(expand "$FMT_DRIFT" "$STALE_RULE" "$(expand "$FMT_LOCATION" "$PROPS_REL" "$CONTRACTS_AXIS")")
  expect_re "$GREP_RE" "$HEADER" "the sweep's /$GREP_RE/ still matches a real printDrift header"
fi
expect_hasnt 'DRIFT [' "$(expand "$BANNER_STALE" 3)" \
             'and the banner is not itself shaped like a finding'

case_start '§4 the banner check matches the sentence the script really prints'
expect_has 'lag the feed' "$(expand "$BANNER_STALE" 3)" \
           'the STALE banner still carries the phrase the sweep keys `reported=` on'
expect_has 'lag the feed' "$(cat "$SWEEP_SH")" 'and the sweep still looks for it'
expect_hasnt 'lag the feed' "$(expand "$BANNER_CANNOT_RUN" 1 "$PROPS_REL")" \
             'while the fail-closed banner does NOT — exit 2 must not read as parsed drift'

case_start '§4 the render step'"'"'s sed prefixes are printDrift'"'"'s continuation prefixes'
# `expected: `/`actual:   ` — the exact byte prefixes, spacing included. Change the alignment in the
# script and the renderer silently writes `?` for both versions; §2 catches the symptom, this names
# the cause.
expect_has "$(expand "$FMT_EXPECTED" '')" "$(cat "$RENDER_SH")" \
           'the renderer sed-strips the expected: prefix the script writes'
expect_has "$(expand "$FMT_ACTUAL" '')" "$(cat "$RENDER_SH")" \
           'and the actual: prefix, alignment spaces and all'
# `cut -d' ' -f1` on the expected value is only correct because `Expected` starts with the VERSION
# and puts its parenthetical after it. Invert that and the issue would report `(newest` as the
# newest version.
expect_re '^[0-9]' "$(expand "$FMT_EXPECTED_VALUE" 7.2.0 'stable version')" \
          'Expected still leads with the version, which is what `cut -f1` takes'
expect_has 'cut -d' "$(cat "$RENDER_SH")" 'and the renderer still takes only that first field'

case_start '§4 the axis the workflow special-cases is an axis the script actually knows'
expect_has "$CONTRACTS_AXIS" "$(axis_names)" \
           "\`$CONTRACTS_AXIS\` is still declared in the script's \`axes\` list"
expect_has "$CONTRACTS_AXIS" "$(cat "$RENDER_SH")" 'and the renderer still branches on it'
# The axis is read out of the DRIFT header by a sed over `($(Name))`. That is `Location`'s shape, and
# nothing else asserts the two still agree.
expect_re '\(\$\([A-Za-z]+\)\)' "$(expand "$FMT_LOCATION" "$PROPS_REL" "$CONTRACTS_AXIS")" \
          'Location still carries the axis as `($(Name))`, which is what the renderer'"'"'s sed reads'

case_start '§4 the props file the findings name is the one the fix has to edit'
expect_has "$PROPS_REL" "$(cat "$RENDER_SH")" \
           "the touch-set names \`$PROPS_REL\` — the file \`Location\` points at"
if [[ -e "$REPO_ROOT/$PROPS_REL" ]]; then
  ok "and \`$PROPS_REL\` exists in this tree"
else
  bad "\`$PROPS_REL\` exists in this tree" "the script points every finding at a file that is not there"
fi

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 5  THE RECONCILE STEP — the tracker is one issue, rewritten, and closed when green
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§5 red + NO tracker → creates the label, then files the issue'
fixture
plant contracts
pipeline
expect_rc 0 'reconcile runs clean'
gh_called POST "/labels$" 'creates the label on first use'
gh_called POST "/issues$" 'files the tracking issue'
expect_has 'FILED https://github.com/FS-GG/FS.GG.Rendering/issues/777' "$OUT" 'reports what it filed'
P=$(any_payload)
expect_has "$DRIFT_TITLE" "$P" 'the issue carries the workflow'"'"'s title'
expect_has "$DRIFT_LABEL" "$P" 'and its label — so the next run can find it again'

case_start '§5 the tracker lookup is a GET — `gh api` POSTs the moment an -f appears'
# Drop the `-X GET` and this "read" POSTs to /issues, the CREATE-ISSUE endpoint: a daily
# blank-issue factory, one missing field away. The fake `gh` derives its method exactly as the real
# one does, so that regression fails HERE.
fixture
sweep_green
pipeline
gh_called     GET  "/issues$" 'the lookup reads'
gh_not_called POST "/issues$" 'the lookup does not CREATE — the -X GET is doing its job'
expect_has 'state=all'  "$(cut -f3 "$FIX/gh.log")" 'and it asks for closed trackers too, or every regression duplicates'
expect_has '--paginate' "$(awk -F'\t' '$1=="GET" && $2 ~ /\/issues$/ {print $4}' "$FIX/gh.log")" \
           'and reads every page — a truncated read looks exactly like "no tracker exists"'

case_start '§5 a PULL REQUEST carrying the label is not mistaken for the tracker'
fixture
trackers <<'JSON'
[{"number":41,"state":"open","pull_request":{"url":"https://api.github.com/…/pulls/41"}},
 {"number":42,"state":"open"}]
JSON
plant contracts
pipeline
expect_has 'tracker: 42 open' "$OUT" 'skips the PR and finds the real issue'
gh_called     PATCH "/issues/42$" 'writes to the issue'
gh_not_called PATCH "/issues/41$" 'and never to the pull request'

case_start '§5 the lookup is filtered by LABEL — or the sweep rewrites a stranger'"'"'s issue'
# Without `-f labels=…`, /issues returns every issue in the repo. `first` is then the OLDEST issue
# that exists — some unrelated bug from a year ago — and the sweep PATCHes a pin-drift report over
# its body. There is no undo for that, and the tracker it was looking for stays unfiled.
fixture
unlabelled <<'JSON'
[{"number":7,"state":"open"}]
JSON
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
plant lagging
pipeline
expect_has 'tracker: 42 open' "$OUT" 'finds the labelled tracker, not the older stranger'
gh_called     PATCH "/issues/42$" 'writes to its own tracker'
gh_not_called PATCH "/issues/7$"  'and never to an issue it does not own'

case_start '§5 red + tracker OPEN → rewrites the body, and stays quiet'
fixture
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
plant lagging
pipeline
gh_called     PATCH "/issues/42$"          'rewrites the body so it is never stale'
gh_not_called POST  "/issues/42/comments$" 'a daily comment on an open item is the noise that gets this muted'
gh_not_called POST  "/issues$"             'does NOT file a second tracker'
expect_has 'UPDATED #42 (already open)' "$OUT" 'says what it did'

case_start '§5 red + tracker CLOSED → reopens it, and THAT gets a comment'
fixture
trackers <<'JSON'
[{"number":42,"state":"closed"}]
JSON
plant lagging
pipeline
gh_called PATCH "/issues/42$"          'reopens the tracker'
gh_called POST  "/issues/42/comments$" 'a fresh regression is worth a notification'
expect_has 'REOPENED #42' "$OUT" 'says it reopened'

case_start '§5 GREEN + tracker OPEN → comments, then closes it'
# The half that keeps the tracker trustworthy: one that only ever opens is one people learn to ignore.
fixture
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
sweep_green
pipeline
gh_called POST  "/issues/42/comments$" 'says where the work was filed that it is done'
gh_called PATCH "/issues/42$"          'and closes it'
expect_has 'CLOSED #42' "$OUT" 'says it closed'
expect_has 'state_reason' "$(any_payload)" 'closed as completed, not as "not planned"'

case_start '§5 exit 2 also FILES — an unchecked pin set is not a green'
# rc=2 is non-zero, so the tracker must open. If the fail-closed code ever fell through to the green
# branch the sweep would CLOSE the tracker on a run that verified nothing — FS-GG/.github#266 exactly.
fixture
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
sweep_cannot_run
pipeline
gh_called     PATCH "/issues/42$"          'the tracker is rewritten with the UNCHECKED body'
gh_not_called POST  "/issues/42/comments$" 'and NOT closed with a "resolved" comment'
expect_has 'the pins are UNCHECKED' "$(any_payload)" 'the filed body says nothing was verified'

case_start '§5 GREEN + no tracker → does nothing at all'
fixture
sweep_green
pipeline
gh_no_writes 'the common case — pins current, no tracker — writes nothing'
expect_has 'Nothing to do.' "$OUT" 'and says so'

# ── summary ─────────────────────────────────────────────────────────────────────────────────────
#
# STILL MISSING, and stated rather than hidden: the real `validate-template-payload-pins.fsx` is
# never executed here. Its staleness lane asks nuget.org at a hard-coded URL, so running it would
# make this suite depend on the network and on what the world published this morning. §4 asserts the
# contract against the script's SOURCE instead, and every fixture in this file is generated from
# those same literals — which covers a reword, but not a change in `feedNewest`'s semantics. The
# workflow's own `pull_request` trigger is what exercises the real script; this suite is what
# exercises the ~120 lines that trigger cannot reach.
harness_summary test-template-pin-staleness-sweep
