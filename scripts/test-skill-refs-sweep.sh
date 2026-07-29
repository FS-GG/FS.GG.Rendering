#!/usr/bin/env bash
# test-skill-refs-sweep — behavioural tests for .github/workflows/skill-refs-sweep.yml (FS.GG.Game#251).
#
# #249 taught the sweep to FILE its finding as an issue, so a decayed pointer stops being a red nobody
# reads. That filing path — render the body, create/reopen/update the tracker, close it when green — is
# now the load-bearing half of the workflow, and it had no test. Worse, the obvious place a test would
# have come from does not work:
#
#   The sweep's interesting code only runs when the tree is DECAYED. The tree is normally green. So the
#   `pull_request` trigger — which exists precisely so that "the sweep is itself code, and a change to
#   it must be checked BY it" — gives a PR that rewrites the renderer a GREEN run in which the renderer
#   never executed. On #249's own run: sweep success, render skipped, reconcile skipped, verdict
#   skipped. A green check over a subject it never examined: the `FS-GG/.github#416` shape, in the code
#   whose entire job is to not do that.
#
# So this suite plants the decay the real trigger cannot, and drives the workflow's own `run:` blocks
# through every branch of it.
#
# THE SUBJECT IS THE YAML, NOT A COPY OF IT. Every `run:` block executed here is EXTRACTED from
# .github/workflows/skill-refs-sweep.yml at test time and run verbatim. Nothing is transcribed. A test
# that re-implemented the renderer would pass forever while the workflow rotted beside it — which is
# the same defect one level up, and this file does not get to commit it either.
#
# WHAT CANNOT BE EXTRACTED IS PINNED INSTEAD. Three things decide WHICH blocks run — the step `if:`
# conditions, the `DRY_RUN` env, and the `pull_request` trigger — and they are GitHub expressions, not
# bash. No harness in this language can evaluate them. So § 0 asserts they still read exactly as this
# file models them: if someone rewires the workflow, these tests fail LOUDLY rather than going on
# silently testing a pipeline the workflow no longer has.
#
# THE FAKE `gh` REPRODUCES THE FOOTGUN. `gh api` switches to POST the moment any `-f` appears, unless
# `-X` says otherwise — which is exactly how #249's tracker lookup came to POST at the CREATE-ISSUE
# endpoint, one missing field away from a daily blank-issue factory. The fake derives its method the
# same way, so dropping the `-X GET` fails § 4 here instead of filing junk in production. It now
# lives in scripts/lib/test-harness.sh (FS.GG.Game#259), shared with the gate's suite — which used to
# carry a second, weaker fake of its own.
#
# Usage: scripts/test-skill-refs-sweep.sh [-v]
set -uo pipefail

VERBOSE=0
[[ ${1:-} == -v ]] && VERBOSE=1

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WF="$REPO_ROOT/.github/workflows/skill-refs-sweep.yml"
REAL_CHECKER="$REPO_ROOT/scripts/check-skill-refs.sh"
[[ -f $WF ]]           || { echo "test-skill-refs-sweep: cannot find $WF" >&2; exit 2; }
[[ -f $REAL_CHECKER ]] || { echo "test-skill-refs-sweep: cannot find $REAL_CHECKER" >&2; exit 2; }

# The harness requires `jq`. These two are this suite's alone: it parses the workflow as real YAML.
# Named, not inferred from a stack trace 40 lines later. Exit 2 (not 1) throughout the preflight: a
# suite that could not RUN is a different fact from a suite that ran and found a bug, and a harness
# about not-conflating-those does not get to conflate them itself.
command -v python3 >/dev/null || { echo "test-skill-refs-sweep: python3 is required" >&2; exit 2; }
python3 -c 'import yaml' 2>/dev/null || {
  echo "test-skill-refs-sweep: PyYAML is required (python3 -m pip install pyyaml)" >&2; exit 2; }

# The subject here is a workflow STEP, not a script — so `bad` says so when it dumps the output.
HARNESS_OUTPUT_LABEL='step output'
# SCRIPTDIR, or the `source=` below resolves against the CWD and finds nothing — see the note in
# test-check-skill-refs.sh. Unfollowed, the harness's reads of HARNESS_OUTPUT_LABEL and RC are
# invisible, and both turn into phantom SC2034s (#266).
# shellcheck source-path=SCRIPTDIR
# shellcheck source=lib/test-harness.sh
. "$REPO_ROOT/scripts/lib/test-harness.sh"
harness_init

# ── the workflow, as data ───────────────────────────────────────────────────────────────────────
#
# A real YAML parse, not a regex over the file. That is worth a python3 dependency on its own — it
# means a workflow that does not PARSE fails this suite, which is the closest thing this repo has to
# the actionlint pass it still lacks (see the note at the foot of this file).
#
# `on:` is remapped because YAML 1.1 reads a bare `on` as the boolean true, so PyYAML hands back a
# `True` key and every naive lookup of "on" silently misses. That is precisely the class of quiet
# nothing this suite exists to refuse, so it is fixed at the source rather than worked around at each
# use.
WF_JSON=$(python3 - "$WF" <<'PY'
import json, sys, yaml
with open(sys.argv[1]) as f:
    doc = yaml.safe_load(f)
if True in doc:                      # YAML 1.1: `on:` parses as the boolean true
    doc["on"] = doc.pop(True)
json.dump(doc, sys.stdout)
PY
) || { echo "test-skill-refs-sweep: $WF does not parse as YAML" >&2; exit 2; }

wf()       { jq -r "$1" <<<"$WF_JSON"; }
step_run() { jq -r --arg id "$1" '.jobs.sweep.steps[] | select(.id==$id) | .run // ""' <<<"$WF_JSON"; }
step_if()  { jq -r --arg id "$1" '.jobs.sweep.steps[] | select(.id==$id) | .if  // ""' <<<"$WF_JSON"; }
step_env() { jq -r --arg id "$1" --arg k "$2" '.jobs.sweep.steps[] | select(.id==$id) | .env[$k] // ""' <<<"$WF_JSON"; }

# The three blocks under test, lifted out of the YAML once.
SWEEP_SH="$TMPROOT/sweep.sh"
RENDER_SH="$TMPROOT/render.sh"
RECONCILE_SH="$TMPROOT/reconcile.sh"
step_run sweep     >"$SWEEP_SH"
step_run render    >"$RENDER_SH"
step_run reconcile >"$RECONCILE_SH"

# The workflow's own env, so the suite tests the REAL label and title rather than a guess at them.
DECAY_LABEL=$(wf '.env.DECAY_LABEL')
DECAY_TITLE=$(wf '.env.DECAY_TITLE')

# ── fixture ─────────────────────────────────────────────────────────────────────────────────────
#
# One fixture = one simulated workflow run. RUNNER_TEMP is shared across the three steps exactly as it
# is on a runner, which is what lets the suite drive the REAL wiring: `sweep` writes findings.txt and
# its outputs, `render` reads them and writes body.md, `reconcile` reads that. Nothing is passed
# between steps by this harness that the workflow does not pass between them itself.
#
# The shared tree — the skill root, the fake `gh`, and the state files it answers out of — comes from
# fixture_new. What is added here is a runner: the files GitHub gives a step (RUNNER_TEMP,
# GITHUB_OUTPUT, GITHUB_STEP_SUMMARY), and a stub standing in for check-skill-refs.sh.
fixture() {
  fixture_new
  mkdir -p "$FIX/runner-temp"
  : >"$FIX/github-output"
  : >"$FIX/step-summary"
  echo 0 >"$FIX/stub.rc"
  : >"$FIX/stub.out"
  : >"$FIX/stub.err"

  # Stub subject for the sweep step. The three files ARE the contract the sweep step reads: its exit
  # code, and whatever it says on stderr. Cases that need the real script call `use_real_checker`.
  cat >"$FIX/scripts/check-skill-refs.sh" <<'STUB'
#!/usr/bin/env bash
d=$(cd "$(dirname "$0")/.." && pwd)
cat "$d/stub.out"
cat "$d/stub.err" >&2
exit "$(cat "$d/stub.rc")"
STUB
  chmod +x "$FIX/scripts/check-skill-refs.sh"
}

# ── what the subject says ───────────────────────────────────────────────────────────────────────

# The FAILED banner. The sweep step keys `reported=` on this exact sentence, so it is a contract
# between two files — pinned for real, against the real script, in § 5.
#
# "a skill body", not "a published skill" (#698). The script's subject grew past what it PUBLISHES —
# it now also sweeps the library bodies under src/*/skill/ and the authoring note, which ship nowhere
# and whose refs are judged against `.claude/skills/` instead of the manifest. The banner had to stop
# saying "published" to stay true, and this pin is what forced the sweep's grep to move with it: § 5
# runs the REAL script over REAL planted decay, so a banner the workflow can no longer find is a red
# test rather than a sweep that silently files nothing.
BANNER='check-skill-refs: FAILED — every pointer in a skill body must resolve: a [[ref]] to a'

sweep_green()   { echo 0 >"$FIX/stub.rc"; printf 'check-skill-refs: ok — every [[ref]] resolves\n' >"$FIX/stub.err"; }
sweep_findings() { echo 1 >"$FIX/stub.rc"; { cat; printf '%s\n' "$BANNER"; } >"$FIX/stub.err"; }   # findings on stdin
sweep_drifted() { echo 1 >"$FIX/stub.rc"; { printf 'template/product-skills/a/SKILL.md L12 — stale link\n'
                                            printf '%s\n' "$BANNER"; } >"$FIX/stub.err"; }
sweep_broken()  { echo 1 >"$FIX/stub.rc"; printf 'check-skill-refs: no authenticated `gh`; refusing to self-skip in CI\n' >"$FIX/stub.err"; }

use_real_checker() { cp "$REAL_CHECKER" "$FIX/scripts/check-skill-refs.sh"; chmod +x "$FIX/scripts/check-skill-refs.sh"; }

# ── running a step ──────────────────────────────────────────────────────────────────────────────

REPO=FS-GG/FS.GG.Game
SERVER=https://github.com
SHA=abc1234def5678
RUN_URL="$SERVER/$REPO/actions/runs/999"

# The env every step gets: GitHub's defaults, the workflow's own `env:`, and the fixture's plumbing.
# `gh_env` is the half the shared fake `gh` answers out of — PATH, its call log, and its state files.
step_base_env() {
  gh_env
  echo "RUNNER_TEMP=$FIX/runner-temp"
  echo "GITHUB_OUTPUT=$FIX/github-output"
  echo "GITHUB_STEP_SUMMARY=$FIX/step-summary"
  echo "GITHUB_SERVER_URL=$SERVER"
  echo "GH_TOKEN=fake-token"
  echo "GH_REPO=$REPO"
  echo "DECAY_LABEL=$DECAY_LABEL"
  echo "DECAY_TITLE=$DECAY_TITLE"
}

# GITHUB_OUTPUT is a real key=value file on a runner; read it back the way Actions does.
out_of() { sed -n "s/^$1=//p" "$FIX/github-output" | tail -1; }

run_sweep() {
  local -a e=(env) base
  mapfile -t base < <(step_base_env); e+=("${base[@]}")
  # GITHUB_ACTIONS is not a knob here, it is a fact: this workflow only ever runs on a runner. It
  # matters because check-skill-refs.sh genuinely forks on it — SKILL_REFS_SKIP_LINKS is ignored in
  # CI, and an unreadable link is fatal there rather than a note. § 5 depends on that fork.
  e+=("GITHUB_ACTIONS=true")
  [[ ${GH_NOAUTH:-0} == 1 ]] && e+=("GH_NOAUTH=1")
  OUT=$(cd "$FIX" && "${e[@]}" bash "$SWEEP_SH" 2>&1); RC=$?
  ((VERBOSE)) && { printf '    ── sweep\n'; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

# `render` and `reconcile` receive their step `env:` from GitHub, which interpolates `${{ … }}` before
# the shell ever sees it. The harness does that interpolation — and § 0 pins the two expressions whose
# meaning it is standing in for, so this stays a MODEL of the workflow rather than a fork of it.
run_render() {
  # DRY_RUN is `${{ github.event_name == 'pull_request' }}` — which GitHub hands the shell as the
  # STRING "true"/"false", never the event name. Compute it the way GitHub would; § 0 pins the
  # expression this is standing in for.
  local dry=false
  [[ ${EVENT:-schedule} == pull_request ]] && dry=true
  local -a e=(env) base
  mapfile -t base < <(step_base_env); e+=("${base[@]}")
  e+=("REPO=$REPO" "SERVER=$SERVER" "SHA=$SHA" "RUN_URL=$RUN_URL"
      "FINDINGS=$(out_of findings)" "REPORTED=$(out_of reported)" "DRY_RUN=$dry")
  OUT=$(cd "$FIX" && "${e[@]}" bash "$RENDER_SH" 2>&1); RC=$?
  ((VERBOSE)) && { printf '    ── render\n'; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

run_reconcile() {
  local -a e=(env) base
  mapfile -t base < <(step_base_env); e+=("${base[@]}")
  e+=("REPO=$REPO" "SHA=$SHA" "RUN_URL=$RUN_URL" "RC=$(out_of rc)")
  [[ ${GH_LABEL_EXISTS:-0}    == 1 ]] && e+=("GH_LABEL_EXISTS=1")
  [[ ${GH_LABEL_POST_FAILS:-0} == 1 ]] && e+=("GH_LABEL_POST_FAILS=1")
  [[ -n ${GH_NEW_NUM:-} ]] && e+=("GH_NEW_NUM=$GH_NEW_NUM")
  OUT=$(cd "$FIX" && "${e[@]}" bash "$RECONCILE_SH" 2>&1); RC=$?
  ((VERBOSE)) && { printf '    ── reconcile\n'; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

# One simulated run, gated exactly as the workflow gates its steps (§ 0 pins those conditions).
#   EVENT=pull_request  → render runs, reconcile does NOT
#   EVENT=schedule      → both run, when the sweep is red
pipeline() {
  run_sweep
  [[ $(out_of rc) != 0 ]] && run_render
  [[ ${EVENT:-schedule} != pull_request ]] && run_reconcile
  return 0
}

body()    { cat "$FIX/runner-temp/body.md" 2>/dev/null; }
summary() { cat "$FIX/step-summary" 2>/dev/null; }
# Every JSON payload the run PUT on the wire (`--input -`). Fields passed as `-f` are in the gh log.
any_payload() { cat "$FIX"/gh-bodies/body-*.json 2>/dev/null; }

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 0  THE WIRING THIS SUITE CANNOT EXECUTE — so it pins it instead
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# `if:`, `DRY_RUN` and the trigger are GitHub expressions. Bash cannot evaluate them, so `pipeline()`
# MODELS them. A model that silently stops matching its subject is the whole disease this repo keeps
# treating, so the model is asserted against the YAML on every run. If you rewire the workflow and
# these fail: good. Re-read `pipeline()` and make it true again.

case_start '§0 the harness is executing the workflow'"'"'s real run: blocks, not a copy of them'
expect_has 'findings=' "$(cat "$SWEEP_SH")"      'the sweep block came out of the YAML'
expect_has 'Paths:'    "$(cat "$RENDER_SH")"     'the render block came out of the YAML'
expect_has 'gh api'    "$(cat "$RECONCILE_SH")"  'the reconcile block came out of the YAML'
# An extractor that quietly returned nothing would make every case below pass vacuously — the exact
# green-over-an-unexamined-subject this file exists to refuse. So it is refused here first.
if [[ $(wc -l <"$SWEEP_SH") -ge 10 && $(wc -l <"$RENDER_SH") -ge 40 && $(wc -l <"$RECONCILE_SH") -ge 40 ]]; then
  ok 'all three blocks are substantial — extraction did not silently yield an empty subject'
else
  bad 'all three blocks are substantial' \
      "extracted $(wc -l <"$SWEEP_SH")/$(wc -l <"$RENDER_SH")/$(wc -l <"$RECONCILE_SH") lines (sweep/render/reconcile)"
fi

case_start '§0 the step conditions still say what pipeline() models'
expect_eq "$(step_if render)"    "steps.sweep.outputs.rc != '0'" 'render runs only when the sweep is red'
expect_eq "$(step_if reconcile)" "github.event_name != 'pull_request'" 'reconcile never runs on a pull_request'
expect_eq "$(step_env render DRY_RUN)" "\${{ github.event_name == 'pull_request' }}" \
          'DRY_RUN is the pull_request predicate'
# THE VERDICT CARRIES BOTH CLAUSES, and the second one is #1114. With only `rc != '0'` a
# `pull_request` run FAILS the job — and `landable` scores every workflow run and check-run on the
# head SHA, required or not, so that red gates the merge of exactly the PRs that edit this sweep,
# demanding a fix in a skill body their item never declared in its `Paths:`. Measured on #1113.
# `scripts/test-template-pin-staleness-sweep.sh` pins the same two clauses on the sibling sweep,
# which has carried them since #1105 created it — this file's subject was the one that had not.
#
# This pin must MOVE WITH the workflow, never be deleted: `pipeline()` below models a PR run as
# "render, then stop", and that model is only honest while the verdict actually excludes PR runs.
expect_eq "$(jq -r '.jobs.sweep.steps[] | select(.name=="Verdict") | .if' <<<"$WF_JSON")" \
          "steps.sweep.outputs.rc != '0' && github.event_name != 'pull_request'" \
          'the verdict re-raises only on a scheduled/dispatched red, never on a PR'

case_start '§0 the pull_request trigger still fires on the sweep'"'"'s own code'
expect_has '.github/workflows/skill-refs-sweep.yml' "$(wf '.on.pull_request.paths[]')" \
           'a change to the workflow is checked by the workflow'
expect_has 'scripts/check-skill-refs.sh' "$(wf '.on.pull_request.paths[]')" \
           'a change to the script is checked by the workflow'
expect_eq "$(wf '.permissions.issues')" 'write' 'the job can still write the issue it files'
expect_eq "$(wf '.on.schedule[0].cron')" '0 7 * * *' 'the daily sweep is still scheduled'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 1  THE SWEEP STEP — parsing the script's findings out of its stderr
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# The `path:line: message` line is the ONE contract this workflow has with the script. Everything
# downstream is built on it, so its three outcomes — parsed, drifted, never-ran — must stay distinct.

case_start '§1 a green sweep reports rc=0, no findings, nothing to file'
fixture
sweep_green
run_sweep
expect_rc 0 'the step succeeds'
expect_eq "$(out_of rc)"       '0'     'rc=0'
expect_eq "$(out_of findings)" '0'     'no findings'
expect_eq "$(out_of reported)" 'false' 'the script printed no FAILED banner'

case_start '§1 findings are parsed, counted, and sorted by file then LINE-NUMERICALLY'
fixture
sweep_findings <<'ERR'
template/product-skills/zeta/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
template/product-skills/alpha/SKILL.md:100: bare ref #4242
template/product-skills/alpha/SKILL.md:9: dangling link FS.GG.Rendering#4242
check-skill-refs: swept 12 files
ERR
run_sweep
expect_rc 0 'the step carries the failure rather than dying on it'
expect_eq "$(out_of rc)"       '1'    'the script'"'"'s exit code is carried, not discarded'
expect_eq "$(out_of findings)" '3'    'three findings parsed'
expect_eq "$(out_of reported)" 'true' 'the FAILED banner was seen'
# `sort -t: -k1,1 -k2,2n`. Lexically, "100" sorts before "9" — a plain sort would put alpha:100 first
# and the issue body would list line 100 above line 9. The `n` is what stops that.
expect_eq "$(head -1 "$FIX/runner-temp/findings.txt" | cut -d: -f1-2)" \
          'template/product-skills/alpha/SKILL.md:9' 'line 9 sorts before line 100, numerically'
expect_hasnt 'swept 12 files' "$(cat "$FIX/runner-temp/findings.txt")" \
             'a non-finding stderr line is not mistaken for a finding'

case_start '§1 DRIFT: the script reported, but this workflow could not parse it'
fixture
sweep_drifted            # banner present, but the finding lines are no longer `path:line: msg`
run_sweep
expect_eq "$(out_of rc)"       '1'     'still red'
expect_eq "$(out_of findings)" '0'     'nothing parsed'
expect_eq "$(out_of reported)" 'true'  'but the script SAYS it reported — the contract has drifted'

case_start '§1 BROKEN: the script never judged a pointer at all'
fixture
sweep_broken
run_sweep
expect_eq "$(out_of rc)"       '1'     'red'
expect_eq "$(out_of findings)" '0'     'nothing parsed'
expect_eq "$(out_of reported)" 'false' 'and it never claimed to report — the run itself failed'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 2  THE RENDER STEP — three bodies, and they must never be confused for one another
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Calling a broken sweep by decay's name (or the reverse) would point the fixer at the wrong files
# while the real problem sits unread. That is the failure this whole workflow is about; the renderer
# does not get to commit it.

case_start '§2 DECAY body: lists every finding, grouped by file, linked to the swept SHA'
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed — mark it `<!-- skill-refs: closed-ok FS.GG.Rendering#100 — why -->`
template/product-skills/alpha/SKILL.md:14: bare ref #4242 — qualify it as FS.GG.Game#4242
template/product-skills/beta/SKILL.md:3: dangling link FS.GG.Rendering#4242
ERR
run_sweep
run_render
expect_rc 0 'the renderer runs clean'
B=$(body)
expect_has 'The daily skill-refs sweep is **red**' "$B" 'names the finding for what it is'
expect_has '**3 finding(s)**'                      "$B" 'counts them'
expect_has '### `template/product-skills/alpha/SKILL.md`' "$B" 'groups findings under their file'
expect_has '### `template/product-skills/beta/SKILL.md`'  "$B" 'and under the second file'
expect_has "blob/$SHA/template/product-skills/alpha/SKILL.md#L9" "$B" 'deep-links the exact line at the swept SHA'
expect_hasnt 'could not complete' "$B" 'does not also cry infra failure'
# The message is rendered in a CODE SPAN, and that is not cosmetic. Raw, GitHub would swallow the
# `<!-- … -->` marker that IS the fix — telling the reader to write nothing — and would autolink the
# bare `#4242` onto whatever unrelated issue holds that number. So assert the SPAN, not merely that
# the text survived: the text survives either way, and only the span makes it legible.
expect_has '<!-- skill-refs: closed-ok' "$B" 'the marker that IS the fix survives into the body'
expect_re  '^- \[L9\]\(.*#L9\) — `.*`$' "$B" \
           'the finding is rendered INSIDE a code span, so GitHub cannot swallow the marker'
expect_has "'"'<!-- skill-refs: closed-ok FS.GG.Rendering#100 — why -->'"'" "$B" \
           'backticks in the message are downgraded so they cannot break out of that span'

case_start '§2 DECAY body: carries a Paths: line, deduped, so the item is SCHEDULABLE when filed'
# A finding filed without a touch-set is one `take`/`batch` refuse: it lands on the board looking like
# work and is invisible to every worker who asks for work (FS-GG/.github#442). Two findings in one file
# must not declare it twice.
expect_has 'Paths: template/product-skills/alpha/SKILL.md template/product-skills/beta/SKILL.md' "$B" \
           'declares each decayed file exactly once'

case_start '§2 DRIFT body: says the decay is REAL and that it cannot see it'
fixture
sweep_drifted
run_sweep
run_render
B=$(body)
expect_has 'The sweep found decay — and this workflow could not read it' "$B" 'names the drift'
expect_has '**The decay is real, and it is not listed here, because this workflow cannot see it.**' "$B" \
           'refuses to imply the tree is clean'
expect_hasnt 'the links are UNCHECKED' "$B" 'does NOT misreport drift as an infra failure'

case_start '§2 BROKEN body: says the links are UNCHECKED — the #416 shape, named'
fixture
sweep_broken
run_sweep
run_render
B=$(body)
expect_has 'The sweep could not complete — the links are UNCHECKED' "$B" 'names the infra failure'
expect_has 'FS-GG/.github#416' "$B" 'says what class of defect an unchecked link is'
expect_hasnt 'could not read it' "$B" 'does NOT misreport an infra failure as drift'
expect_has 'Last 40 lines of the run' "$B" 'carries the evidence a human needs to diagnose it'

# THIS CASE IS WHAT #1114 REMOVED THE RED WITHOUT LOSING. The verdict no longer re-raises on a
# `pull_request` run, so on a DECAYED tree the render asserted here is the whole of what that run
# still produces. (On the normal green tree it produces less again — `render` is gated on `rc != '0'`
# too, so it is skipped, which is #249's gap and the reason this suite plants its own decay rather
# than trusting the trigger.) If the render were ever to go as well, the trigger would be checking
# nothing at all and would be green about it — #249's failure a second time, and invisible rather
# than merely unlucky. The structural half is pinned in § 0: `render`'s `if:` has no event clause, so
# it must never grow the one the verdict now carries. This is the behavioural half. Keep both.
case_start '§2 a PR run RENDERS — the half that was skipped on #249'"'"'s own green run'
fixture
EVENT=pull_request
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
expect_has '**Dry run.**' "$(summary)" 'the PR run says it filed nothing'
expect_has 'The daily skill-refs sweep is **red**' "$(summary)" 'but it DID render the body, into the summary'
expect_has 'stale link FS.GG.Rendering#100' "$(summary)" 'and the findings themselves, not just the preamble'
gh_no_writes 'a pull_request run writes NOTHING to the API'
unset EVENT

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 3  THE RECONCILE STEP — six branches over (sweep verdict × tracker state)
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§3 red + NO tracker → creates the label, then files the issue'
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
expect_rc 0 'reconcile runs clean'
gh_called POST "/labels$"  'creates the label on first use'
gh_called POST "/issues$"  'files the tracking issue'
expect_has 'FILED https://github.com/FS-GG/FS.GG.Game/issues/777' "$OUT" 'reports what it filed'
P=$(any_payload)
expect_has "$DECAY_TITLE" "$P" 'the issue carries the workflow'"'"'s title'
expect_has "$DECAY_LABEL" "$P" 'and its label — so the next run can find it again'
expect_has 'The daily skill-refs sweep is'  "$P" 'the body is the rendered one, not an empty POST'

case_start '§3 red + no tracker + the label already exists → does not re-create it'
fixture
GH_LABEL_EXISTS=1
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
gh_not_called POST "/labels$" 'a green repo that already has the label spends no call on it'
gh_called     POST "/issues$" 'and still files the issue'
unset GH_LABEL_EXISTS

case_start '§3 a label POST that fails cannot veto the report'
# `|| true`, deliberately: the probe reads ANY non-zero as "absent", including a rate-limit blip, and
# the POST would then 422. Under `set -e` that aborts the step — losing the finding to a cosmetic
# detail about a label, at the exact moment the sweep is red and has something to say.
fixture
GH_LABEL_POST_FAILS=1
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
expect_rc 0 'the step survives a 422 on the label'
gh_called POST "/issues$" 'and the finding is still filed'
unset GH_LABEL_POST_FAILS

case_start '§3 red + tracker OPEN → rewrites the body, and stays quiet'
fixture
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
gh_called     PATCH "/issues/42$"          'rewrites the body so it is never stale'
gh_not_called POST  "/issues/42/comments$" 'a daily comment on an open item is the noise that gets this muted'
gh_not_called POST  "/issues$"             'does NOT file a second tracker'
expect_has 'UPDATED #42 (already open)' "$OUT" 'says what it did'

case_start '§3 red + tracker CLOSED → reopens it, and THAT gets a comment'
fixture
trackers <<'JSON'
[{"number":42,"state":"closed"}]
JSON
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
gh_called PATCH "/issues/42$"          'reopens the tracker'
gh_called POST  "/issues/42/comments$" 'a fresh regression is worth a notification'
expect_has 'REOPENED #42' "$OUT" 'says it reopened'
expect_has '"state": "open"' "$(any_payload | jq . 2>/dev/null || any_payload)" 'the PATCH really sets state:open'

case_start '§3 GREEN + tracker OPEN → comments, then closes it'
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

case_start '§3 GREEN + tracker already CLOSED → does nothing at all'
fixture
trackers <<'JSON'
[{"number":42,"state":"closed"}]
JSON
sweep_green
pipeline
gh_no_writes 'a green run over a closed tracker is a no-op'
expect_has 'green; no open tracking issue. Nothing to do.' "$OUT" 'and says so'

case_start '§3 GREEN + NO tracker → does nothing at all'
fixture
sweep_green
pipeline
gh_no_writes 'the common case — green repo, no tracker — writes nothing'
expect_has 'Nothing to do.' "$OUT" 'and says so'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 4  THE TRACKER LOOKUP — the bug #249 actually shipped a fix for
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§4 the tracker lookup is a GET — `gh api` POSTs the moment an -f appears'
# Drop the `-X GET` and this "read" POSTs to /issues — the CREATE-ISSUE endpoint. It 422s today only
# because `title` is missing: it is ONE field away from being a daily blank-issue factory. The fake gh
# derives its method exactly as the real one does, so that regression fails HERE.
fixture
sweep_green
pipeline
gh_called     GET  "/issues$" 'the lookup reads'
gh_not_called POST "/issues$" 'the lookup does not CREATE — the -X GET is doing its job'

case_start '§4 a PULL REQUEST carrying the label is not mistaken for the tracker'
# /issues returns PRs too. Without the `select(.pull_request == null)` filter the sweep would PATCH a
# pull request's body with a decay report.
fixture
trackers <<'JSON'
[{"number":41,"state":"open","pull_request":{"url":"https://api.github.com/…/pulls/41"}},
 {"number":42,"state":"open"}]
JSON
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
expect_has 'tracker: 42 open' "$OUT" 'skips the PR and finds the real issue'
gh_called     PATCH "/issues/42$" 'writes to the issue'
gh_not_called PATCH "/issues/41$" 'and never to the pull request'

case_start '§4 the ORIGINAL tracker stays the tracker when a duplicate exists'
# `sort=created&direction=asc` + `first`: oldest wins, so a duplicate made by hand does not steal the
# thread of comments — and the history — that the real one carries.
fixture
trackers <<'JSON'
[{"number":42,"state":"open"},{"number":88,"state":"open"}]
JSON
sweep_green
pipeline
gh_called     PATCH "/issues/42$" 'closes the oldest tracker'
gh_not_called PATCH "/issues/88$" 'and leaves the duplicate alone'

case_start '§4 a CLOSED tracker is still FOUND — `state=all`, or every regression files a duplicate'
# Drop `-f state=all` and the API defaults to `open`. The closed tracker becomes invisible, the sweep
# concludes none exists, and it files a SECOND one — every time the repo regresses. The tracker that
# carries the history is the one that stops being used.
fixture
trackers <<'JSON'
[{"number":42,"state":"closed"}]
JSON
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
gh_called     PATCH "/issues/42$" 'the closed tracker is found and reopened'
gh_not_called POST  "/issues$"    'NOT filed as a fresh duplicate'
expect_has 'state=all' "$(cut -f3 "$FIX/gh.log")" 'the lookup asks for closed issues too'

case_start '§4 the lookup is filtered by LABEL — or the sweep rewrites a stranger'"'"'s issue'
# Without `-f labels=…`, /issues returns every issue in the repo. `first` is then the OLDEST issue
# that exists — some unrelated bug from a year ago — and the sweep PATCHes a decay report over its
# body. There is no undo for that, and the tracker it was looking for stays unfiled.
fixture
unlabelled <<'JSON'
[{"number":7,"state":"open"}]
JSON
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link FS.GG.Rendering#100 is closed
ERR
pipeline
expect_has 'tracker: 42 open' "$OUT" 'finds the labelled tracker, not the older stranger'
gh_called     PATCH "/issues/42$" 'writes to its own tracker'
gh_not_called PATCH "/issues/7$"  'and never to an issue it does not own'

case_start '§4 the lookup paginates — a truncated read looks exactly like "no tracker exists"'
# Asserted on the argv rather than modelled: the real truncation only bites past a 30-item page, and a
# 31-tracker fixture would be theatre. The consequence is not theatre — a truncated read reports "no
# tracking issue", so the sweep files a fresh one every day (FS-GG/.github#547).
fixture
sweep_green
pipeline
expect_has '--paginate' "$(awk -F'\t' '$1=="GET" && $2 ~ /\/issues$/ {print $4}' "$FIX/gh.log")" \
           'the tracker lookup reads every page'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 5  THE CONTRACT, END TO END — the REAL script, real decay, no stubs
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Everything above stubs check-skill-refs.sh, so everything above would still pass if the script's
# output format drifted away from the `path:line: message` line the sweep greps for. That drift is the
# one failure the workflow cannot self-diagnose — it renders the "could not read it" body and tells
# nobody which files are rotten.
#
# So: plant real decay in a real fixture tree, run the REAL script, and feed its REAL stderr through
# the workflow's REAL parser. This is the canary. If it goes red, the two files have stopped agreeing.

case_start '§5 the real script'"'"'s findings parse through the real sweep step, and render'
fixture
use_real_checker
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' closed
run_sweep
expect_eq "$(out_of rc)"       '1'    'the real script goes red on the planted decay'
expect_eq "$(out_of reported)" 'true' 'the real FAILED banner is the one the sweep greps for'
if [[ $(out_of findings) -ge 1 ]]; then
  ok 'the real `path:line: message` lines parse — the one contract between script and workflow holds'
else
  bad 'the real findings parse' \
      "the sweep parsed 0 findings out of the real script's output. The report() format and the grep in skill-refs-sweep.yml have DRIFTED."
fi
run_render
expect_has 'The daily skill-refs sweep is **red**' "$(body)" 'and the decay body renders from real output'
expect_has 'template/product-skills/fs-gg-alpha/SKILL.md' "$(body)" 'naming the file that really rotted'
expect_has 'Paths: template/product-skills/fs-gg-alpha/SKILL.md' "$(body)" 'with a real, schedulable touch-set'

case_start '§5 a real GREEN tree closes the tracker — the full happy path, end to end'
fixture
use_real_checker
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' open
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
pipeline
expect_eq "$(out_of rc)" '0' 'the real script is green on a healthy tree'
gh_called PATCH "/issues/42$" 'so the tracker is closed'
expect_has 'CLOSED #42' "$OUT" 'the decay work item is done, and says so where it was filed'

case_start '§5 an UNAUTHENTICATED gh files "UNCHECKED" — it never files a green'
# The script refuses to self-skip in CI, and the workflow must not launder that refusal into silence:
# an unchecked link called green is the #416 defect the sweep exists to close.
fixture
use_real_checker
GH_NOAUTH=1
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' open
run_sweep
expect_eq "$(out_of rc)"       '1'     'an unreadable link is not a link it may call green'
expect_eq "$(out_of reported)" 'false' 'and it never judged a pointer, so this is not decay'
run_render
expect_has 'the links are UNCHECKED' "$(body)" 'it files the infra failure, by its right name'
unset GH_NOAUTH

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 6  THE `Class:` LINE — the row this workflow files must be CLASSABLE, and classed correctly
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Since ADR-0066 the `Class:` body line is the authority behind the board's `Class` column, and an
# open Ready row that declares none is `CLASS-UNSET` — counted as a POSSIBLE defect, so a burn-down
# can never establish the board is defect-free while it exists. This body is rewritten on every
# sweep, so nobody but the sweep can put the line there: a human's edit is erased by the next run,
# and an edit to the board column is overwritten by `reconcile` from this text (#1103).
#
# The line must therefore be DERIVED and not guessed (#1588 AC3), which is what this section is
# about. § 6a fixes the mapping against stubbed findings, § 6b fixes it against the REAL script's
# real prose, and § 6c proves it FOLLOWS the findings instead of latching the first run's value.

# The newest JSON payload this run put on the wire. `any_payload` concatenates every one of them, so
# it cannot tell "the second PATCH carries the new class" from "the first one did" — which is the
# only interesting question in § 6c.
last_payload() {
  local n
  n=$(ls -1 "$FIX"/gh-bodies/body-*.json 2>/dev/null | sed 's/.*body-//;s/\.json$//' | sort -n | tail -1)
  [[ -n $n ]] && cat "$FIX/gh-bodies/body-$n.json"
}

# The `Class:` lines the ENGINE would read out of the rendered body — `^ {0,3}[Cc]lass:`, outside any
# fence (FS-GG/.github's `Class.declaredValues`). Modelled rather than shared, because the engine is
# F# and this is bash; what it buys is that the prose mention in the body's "## Severity" paragraph —
# which opens with a backtick — is counted the way the parser counts it, i.e. not at all.
class_lines() { grep -E '^ {0,3}[Cc]lass:' <<<"$(body)" || true; }

case_start '§6a DECAY only → hardening: the world moved, and nothing here is broken'
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link — FS-GG/FS.GG.Rendering#100 is CLOSED. Repoint it at the live issue
template/product-skills/alpha/SKILL.md:11: dangling link — FS-GG/FS.GG.Rendering#4242 does not exist
template/product-skills/beta/SKILL.md:3: unresolvable link — could not read FS-GG/FS.GG.Rendering#7 from the API after 3 tries
template/product-skills/beta/SKILL.md:5: stale closed-ok marker — FS-GG/FS.GG.Rendering#100 is OPEN again; drop the marker
ERR
run_sweep
run_render
expect_rc 0 'the renderer runs clean'
expect_eq "$(class_lines)" 'Class: hardening' 'all four world-moved kinds map to hardening, and to ONE line'
expect_has 'world-moved decay' "$(body)" 'and the body says why, rather than asserting it'

case_start '§6a a HERMETIC finding → defect: a gate regression on `main`, not decay'
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: bare ref — #4242 is read against the repo this body is MATERIALIZED into
ERR
run_sweep
run_render
expect_eq "$(class_lines)" 'Class: defect' 'a bare `#N` is not decay'

case_start '§6a every hermetic kind is recognised as one'
# Four spellings, and `dangling [[…]]` vs `dangling link —` is the pair a looser match collapses:
# they sit on OPPOSITE sides of the mapping, so getting them confused silently downgrades a gate
# regression to hardening — the failure mode that looks like a classed row.
for msg in \
  'bare [[fs-gg-x]] in a MIRRORED body — the same bytes are judged by BOTH repos' \
  'dangling [[fs-gg-x]] — this repo does not publish it; qualify it as [[<owner>:fs-gg-x]]' \
  'stale prose-ok marker — nothing in this file writes [[fs-gg-x]]; drop it' \
  'stale prose-ok marker — nothing in this file writes a bare #4242; drop it'
do
  fixture
  sweep_findings <<ERR
template/product-skills/alpha/SKILL.md:9: $msg
ERR
  run_sweep
  run_render
  expect_eq "$(class_lines)" 'Class: defect' "hermetic: ${msg:0:34}…"
done

case_start '§6a a MIXED run is defect — the gate regression is not averaged away'
# `Class.fromBody`'s own precedence, applied at the point the line is written rather than left for
# the reader: a run carrying both must not resolve to the class that lets a burn-down stop.
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link — FS-GG/FS.GG.Rendering#100 is CLOSED
template/product-skills/alpha/SKILL.md:11: bare ref — #4242 is read against the repo this body is MATERIALIZED into
template/product-skills/beta/SKILL.md:3: dangling link — FS-GG/FS.GG.Rendering#4242 does not exist
ERR
run_sweep
run_render
expect_eq "$(class_lines)" 'Class: defect' 'one hermetic finding among three raises the whole row'
expect_has 'include 1 **hermetic** one(s)' "$(body)" 'and the body names how many, so the reader can check it'

case_start '§6a a kind this workflow does not RECOGNISE classes UP, and says so'
# The mapping is a second contract with `report()`'s prose. If a finding is reworded, the honest
# answer is "I could not derive this", and #266's rule decides which way that falls: a subject you
# could not evaluate is never a subject that passed. Landing it in `hardening` would be invisible —
# the row still looks classed.
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: rotten pointer — some future finding nobody has written yet
ERR
run_sweep
run_render
expect_eq "$(class_lines)" 'Class: defect' 'an underivable severity is not quietly the gentle one'
expect_has 'does not recognise 1 of the findings' "$(body)" 'and the body refuses to imply it derived anything'

case_start '§6a the DRIFT and BROKEN bodies are classed too — they are the #416 shape, not decay'
# These file a row as much as the decay body does, and an unclassed one is just as unschedulable.
fixture
sweep_drifted
run_sweep
run_render
expect_eq "$(class_lines)" 'Class: defect' 'the drift body is a defect: real decay this workflow cannot read'
fixture
sweep_broken
run_sweep
run_render
expect_eq "$(class_lines)" 'Class: defect' 'the "links UNCHECKED" body is a defect: nothing was examined'
# The `<details>` block below it is fenced, and `Class:` is read OUTSIDE fences only. A line that
# drifted inside the fence would parse as nothing at all — with the body still LOOKING classed.
expect_re '^Class: defect' "$(body)" 'and it is a real line at column 0, not fenced evidence'

case_start '§6a the class travels with the body onto the wire'
fixture
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link — FS-GG/FS.GG.Rendering#100 is CLOSED
ERR
pipeline
expect_has 'Class: hardening' "$(any_payload)" 'the filed issue carries the class, not just the local render'

case_start '§6b the REAL script: real decay derives hardening'
# Everything in §6a stubs the script, so all of it would still pass if `report()` reworded its
# findings — and the class would silently become `defect`-by-unrecognised, or worse, drift to a
# spelling that lands in the wrong bucket. So: real decay, real prose, real mapping.
fixture
use_real_checker
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' closed
run_sweep
run_render
expect_eq "$(out_of reported)" 'true' 'the real script reported'
expect_eq "$(class_lines)" 'Class: hardening' \
          'the REAL `stale link` prose still lands in the decay bucket'
expect_hasnt 'does not recognise' "$(body)" \
             'and it was recognised — a rewording would show up HERE, not as a silent misclass'

case_start '§6b the REAL script: a real bare `#N` derives defect'
fixture
use_real_checker
skill fs-gg-alpha <<'MD'
# alpha
Add your case at #4242.
MD
run_sweep
run_render
expect_eq "$(out_of reported)" 'true' 'the real script reported'
expect_eq "$(class_lines)" 'Class: defect' \
          'the REAL `bare ref` prose still lands in the hermetic bucket'
expect_hasnt 'does not recognise' "$(body)" 'and it was recognised, not classed up by accident'

case_start '§6c the class FOLLOWS the findings — it does not latch the first run'"'"'s value'
# The point of the line is that a driver can trust it, and a latched value is worse than none: a row
# that says `hardening` while a gate regression sits on `main` is a lie the board will believe. The
# body is rewritten in full on every sweep, so the class must move with it — in BOTH directions.
fixture
trackers <<'JSON'
[{"number":42,"state":"open"}]
JSON
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link — FS-GG/FS.GG.Rendering#100 is CLOSED
ERR
pipeline
expect_eq "$(class_lines)" 'Class: hardening' 'run 1: plain decay'
expect_has 'Class: hardening' "$(last_payload)" 'and that is what the tracker was rewritten with'

# Run 2, SAME fixture and SAME tracker: a gate regression appears alongside the decay.
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link — FS-GG/FS.GG.Rendering#100 is CLOSED
template/product-skills/alpha/SKILL.md:11: bare ref — #4242 is read against the repo this body is MATERIALIZED into
ERR
pipeline
expect_eq "$(class_lines)" 'Class: defect' 'run 2: the row is upgraded, not left at the first value'
expect_has 'Class: defect' "$(last_payload)" 'and the tracker is REWRITTEN with the new class'
gh_called PATCH "/issues/42$" 'onto the same tracker, not a second one'

# Run 3: the gate regression is fixed and only decay remains. The class must come back DOWN — a
# ratchet would leave every tracker that ever saw one bare ref permanently `defect`, which is the
# same lie pointing the other way.
sweep_findings <<'ERR'
template/product-skills/alpha/SKILL.md:9: stale link — FS-GG/FS.GG.Rendering#100 is CLOSED
ERR
pipeline
expect_eq "$(class_lines)" 'Class: hardening' 'run 3: it comes back down, too — this is not a ratchet'
expect_has 'Class: hardening' "$(last_payload)" 'and the tracker follows it down'

# ── summary ─────────────────────────────────────────────────────────────────────────────────────
#
# STILL MISSING, and worth someone's next hour: nothing in CI parses this workflow's YAML beyond
# GitHub itself. The PyYAML load at the top of this file is a lower bound on that — a workflow that
# does not parse fails here — but it does not know an `if:` from a typo'd `ifs:`, and actionlint does.
harness_summary test-skill-refs-sweep
