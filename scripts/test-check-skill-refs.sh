#!/usr/bin/env bash
# test-check-skill-refs — behavioural tests for scripts/check-skill-refs.sh.
#
# Ported from FS.GG.Game (Game#243/#259) with the gate itself (FS.GG.Rendering#655). The cases are
# theirs; what changes is which owner is SELF (fs-gg-rendering here, so the self/foreign roles in § 1
# invert), and the manifest-backed fixtures — see `skill_at` in lib/test-harness.sh.
#
# The subject is ~600 lines of load-bearing bash that gates every published skill body, and every
# defect it has ever had was found by a human reading it — which is precisely the property it was
# written to end for other things. #241 nearly shipped a one-flag bug (a missing `grep -H`) that
# would have reddened every correctly-marked citation on exactly the PRs that touch a skill, i.e.
# the ones where the gate is supposed to be trustworthy. It would have looked like the gate working.
#
# So these tests are BLACK-BOX: fixture tree in, exit code + output out. Nothing reaches inside the
# script, because the promise being tested is the one the gate makes to its callers — above all the
# one it makes when it says nothing:
#
#     "I will not report green over a subject I did not examine."
#
# Every assertion here is either a verdict (exit code) or a SUBJECT claim (what it says it looked
# at). The second kind is the point: a gate that passes because it checked nothing passes just as
# quietly as one that checked everything.
#
# THE FIXTURE IS A REAL REPO. The subject `cd`s to its own parent, shells out to `git` for
# `--changed`, and calls `gh` for link state — so each fixture is a throwaway git repo holding a
# copy of the script, plus a fake `gh` on PATH whose answers come from a per-fixture state file.
# Nothing here touches the network or the real board.
#
# The counters, the assertions, the fixture builder and that fake `gh` are SHARED with
# scripts/test-skill-refs-sweep.sh — see scripts/lib/test-harness.sh (FS.GG.Game#259). What is local
# to this file is the git-repo fixture and `run`, because the subject here is a script rather than a
# workflow step, and that is the part the two suites do not have in common.
#
# GITHUB_ACTIONS IS CONTROLLED, NEVER INHERITED. The subject's behaviour genuinely forks on it
# (SKILL_REFS_SKIP_LINKS is ignored in CI; an unreadable link is fatal in CI and a note locally),
# so a test that let it leak in from the environment would assert one thing on a laptop and a
# different thing in the very CI run that is supposed to be gating it. `run` scrubs it and each
# test opts in explicitly.
#
# Usage: scripts/test-check-skill-refs.sh [-v]
set -uo pipefail

VERBOSE=0
[[ ${1:-} == -v ]] && VERBOSE=1

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SUT="$REPO_ROOT/scripts/check-skill-refs.sh"
[[ -f $SUT ]] || { echo "test-check-skill-refs: cannot find $SUT" >&2; exit 2; }

# `source=` is resolved against shellcheck's source path, which defaults to the CWD — NOT to this
# script. Without the SCRIPTDIR below, `lib/test-harness.sh` names nothing from the repo root, the
# source is silently NOT followed (SC1091, an `info` that a `-S warning` floor never prints), and
# every variable this file sets FOR the harness — RC, read only by `expect_rc` — looks unused. That
# is where the three SC2034s in #266 came from: not dead code, an unread `source`.
# shellcheck source-path=SCRIPTDIR
# shellcheck source=lib/test-harness.sh
. "$REPO_ROOT/scripts/lib/test-harness.sh"
harness_init

# ── fixture ─────────────────────────────────────────────────────────────────────────────────────

# The shared tree (skill root, fake `gh`, its state files), plus the one thing that is ours: a copy
# of the subject, which the git helpers below then commit into a throwaway repo.
fixture() {
  fixture_new
  cp "$SUT" "$FIX/scripts/check-skill-refs.sh"
  chmod +x "$FIX/scripts/check-skill-refs.sh"
}

git_init() {
  git -C "$FIX" init -q -b main
  git -C "$FIX" config user.email test@example.invalid
  git -C "$FIX" config user.name 'skill-refs tests'
  # The harness's plumbing is not part of the repo under test, and `git add -A` cannot tell the
  # difference. `gh.log` is the one that bites: the fake `gh` APPENDS to it on every call, so a
  # fixture that tracked it would carry a file that mutates while the subject runs — and a case
  # that committed after a `run` would find the harness's own call log inside the diff it is
  # asserting on. Excluded before the first `add`, so none of it is ever tracked.
  printf '%s\n' bin/ gh.log gh-state gh-bodies/ trackers.json unlabelled.json \
    >"$FIX/.git/info/exclude"
  git -C "$FIX" add -A
  git -C "$FIX" commit -qm 'fixture base'
}
git_commit() { git -C "$FIX" add -A && git -C "$FIX" commit -qm "${1:-change}"; }
git_head()   { git -C "$FIX" rev-parse HEAD; }

# A commit that EXISTS but shares no ancestry with HEAD — `rev-parse --verify` says yes and
# `git diff base...HEAD` then dies with `fatal: no merge base`. This is the shape that crashed the
# gate job red on an innocent PR, so the fallback must recognise it and sweep, not explode.
#
# Built with plumbing (an empty tree, committed with no parent) rather than `checkout --orphan`,
# and that is not fussiness: the porcelain route leaves the tracked files untracked, so the
# `checkout` BACK to main fails — "untracked working tree files would be overwritten" — and HEAD
# silently stays on the orphan branch. `merge-base` then compares the commit with ITSELF and
# happily succeeds, so the fixture quietly stops being unrelated and the test passes for the wrong
# reason. commit-tree touches no branch, no index, and no working tree.
git_orphan() {
  local empty; empty=$(git -C "$FIX" mktree </dev/null)
  git -C "$FIX" commit-tree "$empty" -m 'unrelated root'
}

# run [args…] — env knobs: CI_MODE=1, SKIP_LINKS=1, GH_NOAUTH=1
run() {
  local -a e=(env -u GITHUB_ACTIONS -u SKILL_REFS_SKIP_LINKS -u GH_NOAUTH) base
  mapfile -t base < <(gh_env); e+=("${base[@]}")
  [[ ${CI_MODE:-0}   == 1 ]] && e+=("GITHUB_ACTIONS=true")
  [[ ${SKIP_LINKS:-0} == 1 ]] && e+=("SKILL_REFS_SKIP_LINKS=1")
  [[ ${GH_NOAUTH:-0}  == 1 ]] && e+=("GH_NOAUTH=1")
  OUT=$("${e[@]}" "$FIX/scripts/check-skill-refs.sh" "$@" 2>&1)
  RC=$?
  ((VERBOSE)) && { printf '    ── %s\n' "$*"; printf '%s\n' "$OUT" | sed 's/^/    │ /'; }
  return 0
}

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 1  WIKI REFS  — f(tree), hermetic
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§1 a bare [[ref]] to a published skill, and a qualified FOREIGN ref, both resolve'
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-beta]] for the other half, and [[fs-gg-game:fs-gg-ballistics]] for the ballistics model.
MD
skill fs-gg-beta <<'MD'
# beta
MD
run
expect_rc 0 'clean tree passes'
expect_out_has 'every [[ref]] in them resolves against the manifest' 'says the wiki half resolved, and against WHICH set'
# BOTH subjects, or the claim is narrower than the reader will take it for (#698). "Every [[ref]]
# resolves" was TRUE of the published bodies for a whole generation while 37 refs in the library ones
# went unexamined — a true sentence that read as a claim about the tree. The green line must name every
# subject it actually looked at, so a surface that quietly stops being checked cannot hide behind it.
expect_out_has 'repo-internal body/bodies' 'and names the REPO surface as a subject it examined'

case_start '§1 a bare [[ref]] to a skill this repo does NOT publish fails'
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-nowhere]].
MD
run
expect_rc 1 'dangling bare ref fails'
expect_out_has 'dangling [[fs-gg-nowhere]]' 'names the dangling ref'
expect_out_has 'qualify it as' 'tells the author how to fix it'

case_start '§1 a SELF-qualified ref to a skill we DO publish RESOLVES (#714, Game#279)'
# It used to FAIL here ("write it bare"), and that refusal is what made the mirror convention
# unsatisfiable: the four bodies are byte-identical to Game's, and Game must qualify OUR skills to
# reach them. So the same bytes were correct there and refused here, and no diff could fix both.
# A ref that RESOLVES is true. Naming its owner does not make it less so.
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-rendering:fs-gg-beta]].
MD
skill fs-gg-beta <<'MD'
# beta
MD
run
expect_rc 0 'a self-qualified ref that resolves is accepted'

case_start '§1 a SELF-qualified ref to a skill we do NOT publish still dangles'
# The other half, and it is what keeps the acceptance above from being a hole: qualification is not a
# licence. We can SEE our own tree, so we still check it.
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-rendering:fs-gg-nowhere]].
MD
run
expect_rc 1 'a self-qualified ref to a skill we do not publish fails'
expect_out_has 'does not publish' 'says we do not publish it'

case_start '§1 a ref qualified with an owner outside the registry vocabulary fails'
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-bogus:fs-gg-scene]].
MD
run
expect_rc 1 'unknown owner fails'
expect_out_has "unknown owner 'fs-gg-bogus'" 'names the unknown owner'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 2  ISSUE / PR LINKS  — f(tree, world)
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§2 an OPEN issue link passes; a CLOSED one fails'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' open
run
expect_rc 0 'open link passes'
expect_out_has 'issue/PR link(s) in the tree are open or marked' 'states what it resolved'

fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' closed
run
expect_rc 1 'closed link fails'
expect_out_has 'stale link' 'calls it a stale link'
expect_out_has 'closed-ok' 'offers the marker as the opt-out'

case_start '§2 a CLOSED link with a matching closed-ok marker passes'
fixture
skill fs-gg-alpha <<'MD'
# alpha
<!-- skill-refs: closed-ok FS.GG.Rendering#245 — cited as the issue that wired the seam -->
The scaffold ships it wired (FS.GG.Rendering#245).
MD
issue 'FS-GG/FS.GG.Rendering#245' closed
run
expect_rc 0 'marked history citation passes'

case_start '§2 a marker that excuses NOTHING is reported as dead config'
fixture
skill fs-gg-alpha <<'MD'
# alpha
<!-- skill-refs: closed-ok FS.GG.Rendering#999 — the sentence this guarded is long gone -->
Nothing here links anywhere.
MD
issue 'FS-GG/FS.GG.Rendering#999' closed
run
expect_rc 1 'orphan marker fails'
expect_out_has 'stale closed-ok marker' 'calls out the dead marker'
expect_out_has 'nothing in this file links to' 'says why it is dead'

case_start '§2 a marker whose issue REOPENED is reported'
fixture
skill fs-gg-alpha <<'MD'
# alpha
<!-- skill-refs: closed-ok FS.GG.Rendering#245 — history -->
The scaffold ships it wired (FS.GG.Rendering#245).
MD
issue 'FS-GG/FS.GG.Rendering#245' open
run
expect_rc 1 'reopened issue defeats its marker'
expect_out_has 'is OPEN again; drop the marker' 'tells the author to drop it'

case_start '§2 a link to an issue that does not exist fails as dangling'
fixture
skill fs-gg-alpha <<'MD'
# alpha
See FS.GG.Rendering#4242.
MD
run   # unregistered → the fake gh 404s it
expect_rc 1 'missing issue fails'
expect_out_has 'dangling link' 'calls it dangling'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 3  BARE #N  — f(tree), hermetic, no network
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§3 a bare #N is rejected outright'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Track it in #4242.
MD
run
expect_rc 1 'bare ref fails'
expect_out_has 'bare ref' 'calls it a bare ref'
expect_out_has 'FS.GG.Rendering#4242' 'suggests the qualified form'

case_start '§3 a bare #N with a matching prose-ok marker passes'
fixture
skill fs-gg-alpha <<'MD'
# alpha
<!-- skill-refs: prose-ok #4242 — the design doc's number-one bug, not an issue -->
The design doc's "#4242 LOS bug" is the one to read first.
MD
run
expect_rc 0 'prose-ok excuses the bare ref'
expect_out_has 'bare #N ref(s) in published bodies; every one is marked prose-ok' 'states the subject it excused — and WHICH bodies it is a claim about (#698)'

case_start '§3 a prose-ok marker that excuses nothing is dead config'
fixture
skill fs-gg-alpha <<'MD'
# alpha
<!-- skill-refs: prose-ok #4242 — nothing writes this any more -->
Clean prose.
MD
run
expect_rc 1 'orphan prose-ok fails'
expect_out_has 'stale prose-ok marker' 'calls out the dead prose marker'

case_start '§3 a CSS colour is not an issue ref, and neither is a labelled markdown link'
fixture
skill fs-gg-alpha <<'MD'
# alpha
The accent colour is #1a2b3c and the background is #abc123.
See [#587](https://github.com/FS-GG/FS.GG.Rendering/issues/587) for the rationale,
and [see #459](https://github.com/FS-GG/FS.GG.Rendering/issues/459) too.
A heading marker like ## 3 is not a ref either.
MD
issue 'FS-GG/FS.GG.Rendering#587' open
issue 'FS-GG/FS.GG.Rendering#459' open
run
expect_rc 0 'colours and labelled links do not fire §3'
expect_out_has 'no bare #N refs' 'reports zero bare refs, out loud'
expect_out_hasnt '1a2b3c' 'never mentions the colour'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# THE #241 NEAR-MISS  — closed-ok markers must survive a SINGLE-FILE --changed scope
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# `grep -r <dir>` prefixes the filename; `grep <file>` given ONE file operand does not. Under
# `--changed` a one-file diff is the COMMON case, so without `-H` every marker row parses as
# `<line>:<match>` instead of `<file>:<line>:<match>`, `is_excused` matches nothing, and every
# correct, deliberately-marked citation goes red — on exactly the PRs that touch a skill.
#
# This is the test that would have caught it. It is a `--changed` run whose diff touches exactly
# one skill body, and that body's closed link is legitimately marked.

case_start '#241 regression: a marked closed link survives a ONE-FILE --changed scope'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Nothing yet.
MD
skill fs-gg-beta <<'MD'
# beta
MD
git_init
BASE=$(git_head)
skill fs-gg-alpha <<'MD'
# alpha
<!-- skill-refs: closed-ok FS.GG.Rendering#245 — cited as the issue that wired the seam -->
The scaffold ships it wired (FS.GG.Rendering#245).
MD
git_commit 'touch exactly one skill'
issue 'FS-GG/FS.GG.Rendering#245' closed
run --changed "$BASE"
expect_rc 0 'the marker still excuses its link when grep sees a single file'
expect_out_hasnt 'stale link' 'the marked citation is not reported as stale'
expect_out_hasnt 'stale closed-ok marker' 'the marker is not reported as dead'
expect_out_has 'skill body/bodies this diff touches' 'names the scoped subject'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# SCOPE  (#238) — the link half may only judge what the diff touches…
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start 'scope: a diff touching NO skill cannot be reddened by a link that rotted elsewhere'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
mkdir -p "$FIX/src"
echo 'let x = 1' >"$FIX/src/thing.fs"
git_init
BASE=$(git_head)
echo 'let x = 2' >"$FIX/src/thing.fs"        # a diff that touches no skill body
git_commit 'unrelated change'
issue 'FS-GG/FS.GG.Rendering#100' closed     # …and the world moved under an untouched skill
run --changed "$BASE"
expect_rc 0 'the innocent diff is not reddened'
expect_out_has 'link check N/A' 'says the link half judged nothing'
expect_out_has 'swept on a schedule' 'points at where that rot DOES surface'
expect_out_hasnt 'stale link' 'does not report the untouched stale link'

case_start 'scope: …but a diff that DOES touch a skill still owns that skill'"'"'s links'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Nothing yet.
MD
git_init
BASE=$(git_head)
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
git_commit 'introduce a stale link'
issue 'FS-GG/FS.GG.Rendering#100' closed
run --changed "$BASE"
expect_rc 1 'scoping did not neuter the gate'
expect_out_has 'stale link' 'still catches the rot the diff introduced'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# FALLBACK — degrade toward MORE checking, never less
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start 'fallback: an all-zeros base (first push) sweeps the tree rather than checking nothing'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
git_init
issue 'FS-GG/FS.GG.Rendering#100' closed
run --changed 0000000000000000000000000000000000000000
expect_rc 1 'the fallback SWEPT — it found the stale link it could have skipped'
expect_out_has 'does not resolve here' 'names the unusable base'
expect_out_has 'Falling back to the FULL link sweep' 'announces the fallback, loudly'
# The `link scope — …` summary line is NOT asserted here: on a failing run the subject prints its
# failure block and exits before reaching the summary. The fallback is still announced up front
# (above), which is the disclosure that matters. The summary wording is pinned in the clean-tree
# fallback case below, which is the run that actually reaches it.

case_start 'fallback: an UNRELATED-HISTORY base sweeps instead of dying with `fatal: no merge base`'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
git_init
ORPHAN=$(git_orphan)
issue 'FS-GG/FS.GG.Rendering#100' closed
run --changed "$ORPHAN"
expect_rc 1 'exits 1 on the stale link it swept up — NOT 128 from a raw git fatal'
expect_out_has 'shares no history with HEAD' 'diagnoses the real problem'
expect_out_has 'Falling back to the FULL link sweep' 'announces the fallback'
# Not `expect_out_hasnt 'no merge base'`: the subject's OWN diagnosis says "there is no merge base, so
# there is no diff to take against it", which is the sentence we want. What must never appear is
# git's raw plumbing error leaking through as the gate's verdict.
expect_out_hasnt 'fatal:' 'never leaks git'"'"'s raw fatal into the gate output'

case_start 'fallback: an unusable base over a CLEAN tree still passes, and still says what it did'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
git_init
issue 'FS-GG/FS.GG.Rendering#100' open
run --changed 0000000000000000000000000000000000000000
expect_rc 0 'clean tree under a bad base passes'
expect_out_has 'Falling back to the FULL link sweep' 'still announces the fallback'
expect_out_has 'swept the whole tree instead' 'the summary restates the scope, so a green run cannot be mistaken for a scoped one'
expect_out_hasnt 'link check N/A' 'a fallback is never reported as "nothing to judge"'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# NEVER SELF-SKIP — the promise that matters most
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start 'CI + unauthenticated gh → FAIL, never a quiet pass'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' open
CI_MODE=1 GH_NOAUTH=1 run
expect_rc 1 'a link it cannot read is not a link it may call green'
expect_out_has 'no authenticated' 'says why it failed'
expect_out_hasnt 'ok — all' 'does not claim it resolved anything'

case_start 'CI ignores SKILL_REFS_SKIP_LINKS — the skip is a local convenience, not a CI escape'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' closed
CI_MODE=1 SKIP_LINKS=1 run
expect_rc 1 'the stale link is still caught in CI'
expect_out_has 'stale link' 'the link half ran anyway'

case_start 'locally, SKILL_REFS_SKIP_LINKS skips the link half — and SAYS it skipped it'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Add your case at FS.GG.Rendering#100.
MD
issue 'FS-GG/FS.GG.Rendering#100' closed
SKIP_LINKS=1 run
expect_rc 0 'the skipped half cannot fail the run'
expect_out_has 'were NOT checked' 'a skipped subject is announced, never silently passed'
expect_out_has 'SKILL_REFS_SKIP_LINKS is set' 'names the reason it skipped'
expect_out_hasnt 'stale link' 'the link half really was skipped — the stale link went unreported'

case_start 'locally, the hermetic §3 still gates with the link half skipped — it needs no network'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Track it in #4242.
MD
SKIP_LINKS=1 run
expect_rc 1 'a bare ref is caught with no network at all'
expect_out_has 'bare ref' '§3 fired offline'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# ARGUMENT HANDLING
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start 'args: --changed with no base is a usage error, not a silent full sweep'
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
run --changed
expect_rc 2 'refuses a --changed with no ref'
expect_out_has 'needs a base ref' 'says what is missing'

fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
run --nonsense
expect_rc 2 'refuses an unknown argument'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 5  THE PUBLISH SET IS THE MANIFEST, NOT THE DIRECTORY LISTING   (FS.GG.Rendering#655)
#
# The two cases FS.GG.Game's suite cannot state, because Game has no skill supplied from outside its
# skill root and so its directory scan is accidentally correct. Ours is not: four of Rendering's 21
# skills are supplied from template/base/, template/feedback*/ and template/fragments/, and a scan of
# template/product-skills/ calls every one of them unpublished. A direct port of the gate reddens a
# CORRECT [[fs-gg-project]] and then reports its own suggested fix as self-qualified — a false red
# with no green on the other side of it. These pin that shut, in both directions.
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§5 a skill supplied from OUTSIDE the skill root is published — a bare ref to it resolves'
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-project]] for the product-level wiring.
MD
skill_at fs-gg-project template/base/.agents/skills/fs-gg-project <<'MD'
# project
MD
run
expect_rc 0 'an off-convention supplied-by is still published'
expect_out_hasnt 'dangling [[fs-gg-project]]' 'does NOT report the correct ref as dangling'

case_start '§5 a body on disk that the manifest does NOT list is NOT published'
fixture
skill fs-gg-alpha <<'MD'
# alpha
See [[fs-gg-stray]].
MD
unpublished fs-gg-stray template/product-skills/fs-gg-stray <<'MD'
# stray — on disk, but no manifest row
MD
run
expect_rc 1 'a directory is not a publication'
expect_out_has 'dangling [[fs-gg-stray]]' 'asks the manifest, not the filesystem'

case_start '§5 a manifest row whose BODY is missing fails — an unreadable subject is not an absent one'
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
# The manifest promises a body; the tree does not have it. Skipping it would mean the gate reports
# green having examined one fewer body than it claims to publish — the shortfall invisible, because
# nothing counts what it did not open.
rm -f "$FIX/template/product-skills/fs-gg-alpha/SKILL.md"
run
expect_rc 1 'a promised body that is not on disk fails'
expect_out_has 'which does not exist' 'names the body the manifest promised'

case_start '§5 the manifest is the SUBJECT — its absence fails, and is never a green no-op'
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
rm -f "$FIX/template/skill-manifest/skill-manifest.json"
run
expect_rc 1 'a missing manifest fails'
expect_out_has 'cannot be green without its subject' 'refuses to pass over an absent publish set'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 6  THE FROZEN MIRRORS   (ADR-0022 §6 · Game#279 · #714)
#
# Four bodies we ship but FS.GG.Game owns, byte-identical to theirs by ADR — so the SAME BYTES are read
# by two gates against two publish sets.
#
# § 1 used to NOTE them instead of failing, because a BARE ref resolves in exactly one repo and a
# SELF-qualified one was REFUSED, so no byte sequence was green in both and no diff of ours could clear
# the red. Game removed the self-qualified refusal (Game#279) and qualified every ref in the four
# bodies, both directions. One byte sequence now satisfies both gates, so the red is clearable — and the
# stopgap, which had been leaving § 1 UNCHECKED in exactly these four bodies, is gone (#714).
#
# What is pinned here is the new contract, in both directions: a fully-qualified mirror is GREEN, and a
# BARE ref inside one is a HARD FAILURE — the one shape that cannot be right in both repos.
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§6 a fully-QUALIFIED frozen mirror is green — both directions of ref'
# The bytes Game actually ships now. A foreign ref we cannot see is trusted; a self-qualified ref to a
# skill we DO publish resolves. This is the case the old convention could not produce.
fixture
skill fs-gg-game-core <<'MD'
# game-core — a frozen mirror of FS.GG.Game's body
See [[fs-gg-game:fs-gg-ballistics]], which Game publishes and we do not.
And [[fs-gg-rendering:fs-gg-alpha]], which we publish and Game does not.
MD
skill fs-gg-alpha <<'MD'
# alpha
MD
run
expect_rc 0 'one byte sequence, green in both repos'

case_start '§6 a BARE ref inside a FROZEN MIRROR is now a HARD FAILURE'
# The invariant that keeps the whole convention standing. A bare ref in a mirrored body resolves in
# exactly ONE repo, so it silently re-creates the incoherence the qualification exists to remove — and
# it would do so with BOTH gates green, which is precisely how this got lost the first time. It fails
# even when we DO publish the skill, because it is Game's gate it breaks, not ours.
fixture
skill fs-gg-game-core <<'MD'
# game-core
See [[fs-gg-alpha]] — bare, and we publish it, so nothing else here would object.
MD
skill fs-gg-alpha <<'MD'
# alpha
MD
run
expect_rc 1 'a bare ref in a mirrored body fails even when WE can resolve it'
expect_out_has 'MIRRORED body' 'names the reason it is judged differently'
expect_out_has 'QUALIFY it' 'tells the author what to write instead'

case_start '§6 ...and a bare ref in a mirror to a skill NOBODY here publishes fails too'
fixture
skill fs-gg-game-core <<'MD'
# game-core
See [[fs-gg-ballistics]], which Game publishes and we do not.
MD
run
expect_rc 1 'the old NOTE is gone — this is a failure now, and it is clearable: qualify it upstream'

case_start '§6 the stopgap is GONE — no note stream survives on the green path'
# The stopgap printed "N [[ref]](s) in the frozen mirrors" on success. If that sentence ever comes back,
# § 1 has stopped checking the four bodies again, and it will do so QUIETLY — which is the failure this
# whole section exists to prevent. So the absence is asserted, not assumed.
fixture
skill fs-gg-game-core <<'MD'
# game-core
See [[fs-gg-game:fs-gg-ballistics]].
MD
run
expect_rc 0 'green'
expect_out_hasnt 'in the frozen mirrors' 'no note stream — the mirrors are CHECKED, not excused'
expect_out_hasnt 'not ours to fix' 'and no note findings'
expect_out_has 'every [[ref]] in them resolves' 'says it plainly: every ref, mirrors included'

case_start '§6 the SAME ref outside a mirror still FAILS — the hatch is scoped to the four bodies'
fixture
skill fs-gg-scene <<'MD'
# scene — ours, not a mirror
See [[fs-gg-ballistics]].
MD
run
expect_rc 1 'our own body gets no such excuse'
expect_out_has 'dangling [[fs-gg-ballistics]]' 'reports it as a real finding'
expect_out_hasnt 'not ours to fix' 'and does not call it a note'

case_start '§6 §2 STILL FAILS inside a mirror — a closed issue is closed in every repo'
fixture
skill fs-gg-audio <<'MD'
# audio — a frozen mirror
Go and add your case in FS.GG.Rendering#900.
MD
issue 'FS-GG/FS.GG.Rendering#900' closed
run
expect_rc 1 'a stale link in a mirror is still a hard failure'
expect_out_has 'stale link' 'names it as a stale link, not a note'

case_start '§6 a mirror finding IS on STDERR now — the sweep must be able to file it'
# THE CROSS-FILE CONTRACT, pinned with the sweep's OWN regex, and #714 INVERTS it.
#
# skill-refs-sweep.yml decides what to file by grepping stderr for `^[^ :]+:[0-9]+: ` — `report()`'s
# line shape. Under the stopgap a mirror finding was a NOTE on stdout, deliberately kept OFF stderr so
# the sweep could not file 16 unactionable issues a day about refs nobody here could touch.
#
# That reasoning died with the stopgap. A §1 finding in a mirror is now a REAL failure with a REAL fix
# (qualify it in the canonical, re-sync), so it must reach stderr and the sweep MUST file it — the
# decay it reports is now decay somebody can act on. Pinned with the sweep's regex, because neither
# file can catch a seam bug alone: the script would look correct, the sweep would look correct, and the
# finding would be lost between them.
fixture
skill fs-gg-game-core <<'MD'
# game-core — a frozen mirror
See [[fs-gg-ballistics]].
MD
# shellcheck disable=SC2034  # OUT/RC are read by the expect_* helpers
mapfile -t _e < <(gh_env)
err=$(env -u GITHUB_ACTIONS "${_e[@]}" "$FIX/scripts/check-skill-refs.sh" 2>&1 >/dev/null)
expect_has 'fs-gg-ballistics' "$err" 'the finding reaches stderr, where the sweep looks'
if grep -qE '^[^ :]+:[0-9]+: ' <<<"$err"; then
  ok "the sweep's own regex matches it, so it can be filed"
else
  bad "the sweep's own regex matches it, so it can be filed" "no line matched ^[^ :]+:[0-9]+: on stderr"
fi

# ...and the WHOLE finding must sit on that ONE line. The sweep files what its regex matched and drops
# everything else, so a finding wrapped over several lines would file with half its reason missing —
# and it would look fine in the CI log, where the continuation lines are still printed, which is what
# makes it the kind of bug nobody sees. This is the longest finding the script emits, so it is the one
# that wraps first. Asserted by looking for its LAST phrase among the report-shaped lines only.
expect_has 'byte-identity' "$(grep -E '^[^ :]+:[0-9]+: ' <<<"$err")" \
  'the whole finding is on the report line — its tail did not wrap onto a line the sweep drops'

case_start '§6 §3 STILL FAILS inside a mirror — a bare #N is unresolvable in every repo'
fixture
skill fs-gg-persistence <<'MD'
# persistence — a frozen mirror
Track it in #4242.
MD
run
expect_rc 1 'a bare ref in a mirror is still a hard failure'
expect_out_has 'bare ref' 'names it as a bare ref, not a note'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 7  THE REPO SURFACE (#698) — the bodies that ship NOWHERE
# ════════════════════════════════════════════════════════════════════════════════════════════════
# The subject used to be the manifest and nothing else, so 37 refs in `src/*/skill/` and 2 in the
# authoring note were checked by NOTHING — and one of them was already dead (`[[fsharp-build-
# orchestration]]`, a skill in no registry, no manifest and no directory anywhere in the org). It
# survived only because its published TWIN happened to be caught by the manifest-scoped gate; that is
# luck, and these cases are the gate that replaces it.
#
# The load-bearing claim, and the one every case here circles: A `[[ref]]`'S VERDICT IS RELATIVE TO
# WHAT RESOLVES WHERE ITS READER STANDS. A published body's reader is in a scaffolded product, so the
# manifest answers. A library body's reader is in THIS repo driving an agent, so `.claude/skills/`
# answers. The two sets disagree — in membership, and in which BODY a name points at — so a suite that
# only proved "more files are scanned now" would prove the easy half and miss the design.

case_start '§7 a repo body resolves its refs against .claude/skills/, NOT the manifest'
# THE case. `fs-gg-ant-design` is a real skill an agent here can invoke, and it is NOT published — the
# manifest never ships it. Judged against the publish set (the pre-#698 vocabulary) this CORRECT ref is
# reported dangling, and five of them sit in the library bodies today. A gate that reddens correct work
# is one people switch off, so the wrong vocabulary is not a stricter gate; it is a broken one.
fixture
claude_skill fs-gg-ant-design
repo_skill Controls <<'MD'
# Controls
Apply the tokens per [[fs-gg-ant-design]].
MD
run
expect_rc 0 'a ref to a .claude/skills/ skill the manifest does NOT publish is CORRECT here'

case_start '§7 ...and the converse: PUBLISHED is not enough for a repo body'
# The other half, and the one that proves the surfaces are not simply UNIONED. A skill the manifest
# publishes but that no wrapper exposes cannot be invoked by an agent standing here, so the pointer
# leads nowhere for THIS body's reader — which is the only reader it has.
fixture
skill fs-gg-alpha <<'MD'
# alpha — published, but no .claude/skills/ wrapper
MD
repo_skill Scene <<'MD'
# Scene
See [[fs-gg-alpha]].
MD
run
expect_rc 1 'a manifest-only skill does not resolve for a reader standing in this repo'
expect_out_has '.claude/skills/' 'and the finding names the vocabulary that judged it'

case_start '§7 the rot that was actually there: a ref to a skill that exists NOWHERE'
# `[[fsharp-build-orchestration]]`, verbatim, in the body it was verbatim in.
fixture
repo_skill Testing <<'MD'
# Testing

## Related
- [[fsharp-build-orchestration]] runs the governed targets these helpers back.
MD
run
expect_rc 1 'the dead ref #698 was filed about is now caught'
expect_out_has 'fsharp-build-orchestration' 'and named'

case_start '§7 the finding tells the author WHICH set failed it, not just that it failed'
# The same string resolves on one surface and dangles on the other, so "dangling" without a vocabulary
# reads as a bug in the gate. An author who cannot tell which set answered cannot tell a real dangling
# ref from a ref they wrote on the wrong surface.
fixture
repo_skill Layout <<'MD'
# Layout
See [[fs-gg-nowhere]].
MD
run
expect_rc 1 'dangles'
expect_out_has 'cannot invoke it' 'says what the reader would actually experience'
expect_out_has '.claude/skills/' 'names the set'

case_start '§7 a published body is STILL judged against the manifest — the surfaces do not bleed'
# `.claude/skills/` is a superset of the manifest in practice, so a bug that judged EVERYTHING against
# it would pass every existing case and quietly stop checking the published bodies — green, and wrong.
fixture
claude_skill fs-gg-ant-design
skill fs-gg-alpha <<'MD'
# alpha
A published body pointing at a skill that is in .claude/skills/ but is NOT published: [[fs-gg-ant-design]].
MD
run
expect_rc 1 'a product reader has no .claude/skills/, so this dangles where it is READ'
expect_out_has 'does not publish it' 'and it is the PUBLISH set that says so'

# ── § 7.1  the authoring note, and writing a syntax without invoking it ─────────────────────────

case_start '§7 the README may WRITE [[link]] without INVOKING it — prose-ok [[…]]'
# The doc that TEACHES the convention has to be able to show its shape. The old script's answer was to
# declare the README out of subject, which is how its two real refs went unchecked. Reject by default;
# let the author declare the exception — the answer this script already gives twice.
fixture
repo_readme <<'MD'
# Product skills — authoring notes

<!-- skill-refs: prose-ok [[link]] — the SHAPE of a ref, not a ref -->
**A `[[link]]` is not an instrument declaration.**
MD
run
expect_rc 0 'the illustration is excused, and the doc can explain itself'

case_start '§7 ...but an UNMARKED illustration still fails — silence is never the default'
fixture
repo_readme <<'MD'
# Product skills — authoring notes
**A `[[link]]` is not an instrument declaration.**
MD
run
expect_rc 1 'no marker, no exemption'
expect_out_has 'dangling [[link]]' 'reported like any other unresolvable ref'

case_start '§7 a prose-ok [[…]] marker cannot excuse ITSELF'
# § 1's scan was a raw `grep` until #698 and never stripped markers — harmless while no marker could
# contain a `[[…]]`, and a live hole the moment one can. An un-stripped marker IS a `[[ref]]`: it would
# report the very config written to silence it, and then excuse its own report. Config must not be its
# own subject. Here the marker names a ref the body does NOT write, so if the marker were scanned it
# would find its own `[[ghost]]` and call the marker live; it is stale, and must be reported so.
fixture
repo_skill Scene <<'MD'
# Scene

<!-- skill-refs: prose-ok [[ghost]] — nothing in this body writes it -->
No refs here at all.
MD
run
expect_rc 1 'a marker that excuses nothing is dead config'
expect_out_has 'stale prose-ok marker' 'and it is reported, not silently believed'
expect_out_has 'ghost' 'naming the ref it claimed to excuse'

# ── § 7.2  § 3 INVERTS: a bare #N in a repo body is RESOLVED, not rejected ──────────────────────
# § 3 rejects a bare `#N` because the body MATERIALIZES into a stranger's repo, where GitHub renders it
# against THEIR tracker. That premise is false of a body that ships nowhere: it is read HERE, and
# GitHub renders `#N` in it against OUR tracker. The pointer is correct — so rejecting it would fire on
# correct work, and the gate would be the one people turn off.
#
# But silence is not the alternative. The ref still promises a LIVE issue at the other end, which is
# § 2's question, so it is RESOLVED against this repo instead. The surface goes from zero checking to
# § 2's full strictness, and each surface gets the verdict its reader can act on.

case_start '§7 a bare #N in a repo body is RESOLVED against this repo, and passes when OPEN'
fixture
issue 'FS-GG/FS.GG.Rendering#4242' open
repo_skill Diagnostics <<'MD'
# Diagnostics
The remaining gap is tracked in #4242.
MD
run
expect_rc 0 'a bare ref to a LIVE issue of ours is correct in a body that ships nowhere'
expect_out_hasnt 'bare ref —' 'and it is NOT rejected as a bare ref: that verdict belongs to the other surface'

case_start '§7 ...and FAILS when that issue is CLOSED — as a stale LINK, not as a bare ref'
# The verdict AND its name matter. "Bare ref — qualify it" would tell the author to fix a form that is
# already correct here; "stale link" tells them the truth: the issue is closed, repoint or excuse it.
fixture
issue 'FS-GG/FS.GG.Rendering#4242' closed
repo_skill Diagnostics <<'MD'
# Diagnostics
The remaining gap is tracked in #4242.
MD
run
expect_rc 1 'a closed issue is closed wherever it is read'
expect_out_has 'stale link' 'and it is reported as § 2 decay, which is what it is'
expect_out_has 'closed-ok' 'offering the marker that fits an honest citation of history'

case_start '§7 ...and a bare #N pointing at NOTHING dangles'
fixture
repo_skill Diagnostics <<'MD'
# Diagnostics
See #4242.
MD
run
expect_rc 1 'an issue that does not exist is a dangling pointer, bare or not'
expect_out_has 'dangling link' 'named for what it is'

case_start '§7 prose-ok #N still works on the repo surface — it suppresses the RESOLUTION'
# Same marker, same sentence — "this pointer-shaped token is prose" — doing a different job because
# the default verdict differs. Without it the gate would resolve `#1` against our tracker and report
# whatever it found there.
fixture
repo_skill LineDrawing <<'MD'
# Line drawing

<!-- skill-refs: prose-ok #1 — the design doc's number-one bug, not issue #1 -->
Mind the design doc's "#1 LOS bug".
MD
run
expect_rc 0 'the marker stops it being resolved at all'
# AND IT IS NOT THEN CALLED DEAD FOR IT. The staleness audit pairs each marker against the bare refs it
# FOUND — but a repo body's bare refs are promoted into the LINK half, never into § 3's list. An audit
# that consulted § 3's list alone would find nothing to pair with and report every one of these markers
# stale: "drop it", said of the only thing keeping the author's line green. A staleness check that
# fires on live config is worse than no check at all — it teaches people to ignore it.
expect_out_hasnt 'stale prose-ok' 'the marker is doing its job, and is not slandered for it'

case_start '§7 §3 STILL rejects a bare #N in a PUBLISHED body — the inversion is scoped'
# The premise that moved is "this body is materialized". It did not move for the bodies that are.
fixture
issue 'FS-GG/FS.GG.Rendering#4242' open
skill fs-gg-alpha <<'MD'
# alpha
The remaining gap is tracked in #4242.
MD
run
expect_rc 1 'a bare ref in a SHIPPED body is wrong by its form, however live the issue is'
expect_out_has 'bare ref' 'and it is rejected on FORM, not resolved'
expect_out_has 'MATERIALIZED' 'for the reason that is true only of a shipped body'

# ── § 7.3  the subject, and refusing to run without it ──────────────────────────────────────────

case_start '§7 the gate REFUSES to run with no .claude/skills/ — a missing vocabulary is not "no refs"'
# The `.github#416` shape, one surface down. With no vocabulary every repo ref dangles — but reporting
# 37 findings would be a gate so loud it looks broken, and the "fix" would look like deleting the refs.
# With a TOLERANT reading it would instead pass green over ten unexamined bodies. Neither. Refuse.
fixture
rm -rf "$FIX/.claude"
run
expect_rc 1 'no vocabulary, no verdict'
expect_out_has 'cannot be green without its vocabulary' 'and it says exactly that'

case_start '§7 the gate REFUSES to run with no repo bodies — the subject cannot silently vanish'
fixture
rm -rf "$FIX/src"
run
expect_rc 1 'a subject that has gone missing is not a subject with nothing in it'
expect_out_has 'repo-internal skill bodies' 'and it names what it went looking for'

case_start '§7 a green run NAMES both subjects — neither surface may pass silently'
# The sentence that was true and misleading for a whole generation: "every [[ref]] resolves", said
# while 37 refs went unexamined. A gate that states a narrower subject than its reader assumes is the
# `.github#416` shape wearing a green tick, and this script has now been on both ends of it.
fixture
claude_skill fs-gg-ant-design
skill fs-gg-alpha <<'MD'
# alpha
MD
repo_skill Scene <<'MD'
# Scene
See [[fs-gg-ant-design]].
MD
run
expect_rc 0 'green'
expect_out_has 'skills published' 'names the published subject'
expect_out_has 'repo-internal body/bodies' 'and names the repo subject'
expect_out_has '.claude/skills/' 'and the vocabulary it judged the second one against'

# ── § 7.4  the SCOPED run — the one gate.yml actually makes on every PR ─────────────────────────

case_start '§7 a --changed run SEES a repo body — the link scope is the SUBJECT, not the manifest'
# THE REGRESSION THIS SECTION EXISTS FOR, and it was live in this very change until a scoped run was
# tried by hand. `link_md_files` intersected the diff with the MANIFEST's bodies — correct while the
# manifest WAS the subject, and silently wrong the moment it stopped being. A repo body is not in the
# manifest, so it was dropped from the link scope; and the link half is the ONLY half that is scoped.
# So under `--changed` — which is what gate.yml runs on every PR — the repo surface's links were
# resolved by NOBODY, and the gate reported green having examined none of them, on precisely the diffs
# that touched them.
#
# It is invisible from the full sweep, where `link_md_files` falls through to the whole tree and
# everything looks fine. Only a SCOPED run over a REPO body can state it. That is this case.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
repo_skill Symbology <<'MD'
# Symbology
Nothing yet.
MD
git_init
BASE=$(git_head)
repo_skill Symbology <<'MD'
# Symbology
They filed FS.GG.Rendering#4242 rather than working around it.
MD
git_commit 'touch exactly one REPO body'
issue 'FS-GG/FS.GG.Rendering#4242' closed
run --changed "$BASE"
expect_rc 1 'the stale link in the repo body IS resolved under a scoped run'
expect_out_has 'stale link' 'and reported — not skipped for not being in the manifest'
expect_out_has 'src/Symbology' 'in the repo body the diff touched'

case_start '§7 ...and a scoped run over a repo body resolves its BARE #N too'
# The other half of the same scope. A repo body's bare `#N` is promoted into the LINK half (§ 3's
# inversion), so it inherits the link half's scoping — and would inherit its blindness too.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
repo_skill Diagnostics <<'MD'
# Diagnostics
Nothing yet.
MD
git_init
BASE=$(git_head)
repo_skill Diagnostics <<'MD'
# Diagnostics
The remaining gap is tracked in #4242.
MD
git_commit 'introduce a bare ref in a repo body'
issue 'FS-GG/FS.GG.Rendering#4242' closed
run --changed "$BASE"
expect_rc 1 'a bare ref in a touched repo body is resolved, and its issue is closed'
expect_out_has 'stale link' 'reported as § 2 decay, under the scope, exactly as an explicit link would be'

case_start '§7 a tree with NO repo bare refs exits 0 WITH output — never a silent exit 1'
# A regression pin for a real bug in this change, and the ugliest kind. Under `set -o pipefail` a
# filter loop whose LAST test fails returns 1, which reddens the pipeline it feeds, which aborts the
# assignment, which `set -e` turns into a bare `exit 1` — no findings, no banner, not one word. It fired
# on the NORMAL tree (no repo body has a bare `#N`), so the gate died silently on every clean run and
# every fixture at once. A gate that fails without saying why is the defect this whole script exists to
# prevent; it does not get to commit it itself.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
run
expect_rc 0 'a clean tree with no repo-surface bare refs is green'
expect_out_has 'check-skill-refs: ok' 'and it SAYS so — an exit code with no words is not a verdict'

# ── summary ─────────────────────────────────────────────────────────────────────────────────────
harness_summary test-check-skill-refs
