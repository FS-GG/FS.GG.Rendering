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

# mirror_constant <id>… — inject a synthetic frozen-mirror roster into the fixture's COPY of the
# subject, by rewriting its `MIRRORED_SKILLS` assignment. Must run AFTER `fixture` (which copies $SUT).
#
# WHY THIS EXISTS. The production `MIRRORED_SKILLS` is EMPTY as of ADR-0063 (FS.GG.Rendering#965 retired
# the four game mirrors). The § 6 machinery cases and the § 9 stale-entry case exercise what the script
# does WHEN a mirror is declared — a capability that is dormant but must keep working for the next
# mirror — so they inject one here rather than depending on a constant that no longer names one. It is
# the twin of `mirror_roster`: that one supplies the REGISTRY's half of "a mirror is foreign AND we
# publish it" (the fake `gh`'s view), this one supplies the SCRIPT's declared half.
mirror_constant() {
  local joined="" id
  for id in "$@"; do joined+="$id\\n"; done
  joined=${joined%\\n}
  local sut="$FIX/scripts/check-skill-refs.sh"
  awk -v repl="MIRRORED_SKILLS=\$'$joined'" '
    /^MIRRORED_SKILLS=/ { print repl; next }
    { print }
  ' "$sut" >"$sut.new" && mv "$sut.new" "$sut"
  chmod +x "$sut"
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
  printf '%s\n' bin/ gh.log gh-state gh-bodies/ trackers.json unlabelled.json kit.txt mirrors.txt \
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

# run [args…] — env knobs: CI_MODE=1, SKIP_LINKS=1, GH_NOAUTH=1, KIT_FAIL=1, MIRRORS_FAIL=1,
# MIRRORS_EMPTY=1
run() {
  local -a e=(env -u GITHUB_ACTIONS -u SKILL_REFS_SKIP_LINKS -u GH_NOAUTH -u GH_KIT_FAIL \
                  -u GH_MIRRORS_FAIL -u GH_MIRRORS_EMPTY) base
  mapfile -t base < <(gh_env); e+=("${base[@]}")
  [[ ${CI_MODE:-0}   == 1 ]] && e+=("GITHUB_ACTIONS=true")
  [[ ${SKIP_LINKS:-0} == 1 ]] && e+=("SKILL_REFS_SKIP_LINKS=1")
  [[ ${GH_NOAUTH:-0}  == 1 ]] && e+=("GH_NOAUTH=1")
  [[ ${KIT_FAIL:-0}   == 1 ]] && e+=("GH_KIT_FAIL=1")
  [[ ${MIRRORS_FAIL:-0}  == 1 ]] && e+=("GH_MIRRORS_FAIL=1")
  [[ ${MIRRORS_EMPTY:-0} == 1 ]] && e+=("GH_MIRRORS_EMPTY=1")
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
# THE #1117 CHAIN  — `#A/#B` is TWO refs, and the `/#N` near-misses stay spared
# ════════════════════════════════════════════════════════════════════════════════════════════════
#
# Both extractors scan DESTRUCTIVELY and both demand a boundary char outside `[A-Za-z0-9._/#-]` in
# front of a ref. Those two facts together hid every ref after the FIRST of a slash-joined chain:
# once `#1080` is taken from `(#1080/#1082)` the remainder opens `/#1082`, and `/` is precisely the
# boundary char the class forbids. Measured on a real body whose refs were BOTH closed, the gate
# reported ONE finding and passed the other — green over a subject it never examined, which is the
# failure this script names in its own header.
#
# BOTH DIRECTIONS, and the second set is the load-bearing one. The naive fix — drop `/` from the
# boundary class — passes every chain case below and is wrong: the `/` is what spares `docs/#1` and
# a URL's `…/#2`. A suite that pinned only the chain would green-light exactly that fix, so the
# near-miss cases must be able to FAIL, and they are written with a `/` DIRECTLY before the `#`
# rather than the `page#1` shape (whose `e` the class already excludes on other grounds — a leg that
# cannot fail is not a leg).

case_start '§3 #1117: EVERY ref of a slash-joined chain is reported, not just the first'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Carried over from the body that used to sit at this path (#4242/#4243).
The staging plan ran #4244/#4245/#4246 in that order.
MD
run
expect_rc 1 'the chain is rejected'
expect_out_has 'bare ref — #4243' 'the SECOND ref of the pair is examined at last'
expect_out_has 'bare ref — #4246' 'and so is the last of a three-link chain'
expect_eq "$(grep -c 'bare ref —' <<<"$OUT")" 5 'one finding per REF (2 + 3), not one per chain'

case_start '§3 #1117: a `/` that no ref precedes is still not a boundary — `docs/#3` stays spared'
# THE FALSE-POSITIVE LEG. Links are skipped so the verdict is § 3's alone: § 3 is ungated by
# `link_mode`, so a green run here is a claim about the bare-ref scan and nothing else. `path/name#4`
# is a § 2 ref by grammar (owner/repo#num) and would resolve, or dangle, on its own merits — it is
# here to state that § 3 does not ALSO report it as bare.
fixture
skill fs-gg-alpha <<'MD'
# alpha
The anchor is https://example.com/docs/#1 and the sub-page is https://example.com/a/b/#2.
A relative one, docs/#3, is no different, and path/name#4 is a qualified ref, not a bare one.
MD
SKIP_LINKS=1 run
expect_rc 0 'a bare-looking `#N` behind a path separator is not a ref'
expect_out_has 'no bare #N refs' 'and it says so about the subject it scanned'

case_start '§3 #1117: one hop only — the separator does not chain on through path text'
# The rule is "the `/` that opens the remainder of a ref we JUST consumed", not "every `/` downstream
# of a ref". `#12` is consumed and its `/` becomes a boundary; `notes/` is not a ref, so by the time
# the scan reaches `/#3` no ref precedes THAT slash and ordinary boundaries apply. Left unbounded the
# rule would walk the whole line, so this case fails on a fix that rewrites the separator once and
# then keeps rewriting: the count is the leg, and the head's own finding is what makes it countable.
fixture
skill fs-gg-alpha <<'MD'
# alpha
See #12/notes/#3 for the working copy.
MD
SKIP_LINKS=1 run
expect_rc 1 'the chain HEAD is a bare ref and is reported'
expect_out_has 'bare ref — #12' 'the head, which is a real finding'
expect_out_hasnt 'bare ref — #3' 'and the path fragment behind it, which is not'
expect_eq "$(grep -c 'bare ref —' <<<"$OUT")" 1 'exactly one ref on the line, not two'

case_start '§3 #1117: a token the scan REJECTED does not open a chain — `#1a2b3c/#000000`'
# THE REVIEW FINDING ON THE FIRST CUT OF #1117, and the sharpest false-positive leg here. § 3's loop
# over-matches deliberately so a CSS colour can be taken whole and then thrown away — so a chain rule
# placed BEFORE that verdict chains off tokens that were just rejected, and `#000000` is reported as a
# bare issue ref in a rendering repo's palette. The rule's own justification is what forbids it: a `/`
# after a ref cannot be path text BECAUSE the ref ended in a digit, and `#1a2b3c` did not.
fixture
skill fs-gg-alpha <<'MD'
# alpha
The two-tone fill is #1a2b3c/#000000, and the tag in the export reads #12abc/#34.
MD
SKIP_LINKS=1 run
expect_rc 0 'a colour joined to a colour is still no issue ref'
expect_out_has 'no bare #N refs' 'nothing was promoted behind the rejected token'
expect_out_hasnt '000000' 'never mentions the second colour'

case_start '§3 #1140: every adjacent-ref separator yields both bare refs'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Ranges #4242-#4243, #4244_#4245, and #4246.#4247 are all two references.
MD
run
expect_rc 1 'each separator chain is rejected'
expect_out_has 'bare ref — #4242' 'the hyphen chain head is no longer dropped'
expect_out_has 'bare ref — #4245' 'the underscore chain tail is examined'
expect_out_has 'bare ref — #4247' 'the dot chain tail is examined'
expect_eq "$(grep -c 'bare ref —' <<<"$OUT")" 6 'two findings for every adjacent-ref separator'

case_start '§3 #1140: non-separator trailing runs remain prose, and a labelled-link head exposes its tail'
fixture
skill fs-gg-alpha <<'MD'
# alpha
Colours #1a2b3c and #abc123 plus a prose token #12_beta are not refs.
The split is [#587](https://github.com/FS-GG/FS.GG.Rendering/issues/587)/#588.
MD
run
expect_rc 1 'only the labelled-link chain tail is rejected'
expect_out_has 'bare ref — #588' 'the bare tail after a labelled link is examined'
expect_out_hasnt 'bare ref — #587' 'the labelled link head remains outside the bare scan'
expect_out_hasnt 'bare ref — #12' 'an underscore without a following ref is still prose'
expect_eq "$(grep -c 'bare ref —' <<<"$OUT")" 1 'only the chain tail is a bare ref'

case_start '§2 #1117: the chain is resolved ref-by-ref on the REPO surface too'
# § 2 and § 3 share `BARE_AWK`, so the repo surface's PROMOTION of bare refs to links inherits the
# same blindness — and there it is worse, because the missed ref is one the gate would have RESOLVED.
# This is the shape that was measured: both closed, one finding.
fixture
issue 'FS-GG/FS.GG.Rendering#4242' closed
issue 'FS-GG/FS.GG.Rendering#4243' closed
repo_skill Diagnostics <<'MD'
# Diagnostics
Both halves landed (#4242/#4243).
MD
run
expect_rc 1 'a closed issue is closed wherever it is read'
expect_out_has 'FS.GG.Rendering#4243 is CLOSED' 'the second ref is RESOLVED, not skipped'
expect_eq "$(grep -c 'stale link' <<<"$OUT")" 2 'two refs, two verdicts'

case_start '§2 #1117: a QUALIFIED chain (Repo#A/Repo#B) resolves both halves'
# `emit_links` has its own copy of the destructive scan and its own boundary class, so it has its own
# copy of the defect. Same rule, the qualified form.
fixture
issue 'FS-GG/FS.GG.Rendering#4242' closed
issue 'FS-GG/FS.GG.Rendering#4243' closed
skill fs-gg-alpha <<'MD'
# alpha
Both halves landed (FS.GG.Rendering#4242/FS.GG.Rendering#4243).
MD
run
expect_rc 1 'the qualified chain is rejected'
expect_out_has 'FS.GG.Rendering#4243 is CLOSED' 'the second qualified ref is resolved'
expect_eq "$(grep -c 'stale link' <<<"$OUT")" 2 'two links, two verdicts'

case_start '§3 #1138: a qualified head exposes its BARE tail, but is not double-reported'
fixture
skill fs-gg-alpha <<'MD'
# alpha
The next operation is FS.GG.Rendering#4242/#4243.
MD
run
expect_rc 1 'the bare tail is rejected in a published body'
expect_out_has 'bare ref — #4243' 'the bare tail is examined'
expect_out_hasnt 'bare ref — #4242' 'the qualified head remains §2’s subject'
expect_eq "$(grep -c 'bare ref —' <<<"$OUT")" 1 'only the bare tail is reported'

case_start '§2 #1138: a qualified head exposes and resolves its BARE tail on the REPO surface'
fixture
issue 'FS-GG/FS.GG.Rendering#4242' open
issue 'FS-GG/FS.GG.Rendering#4243' closed
repo_skill Diagnostics <<'MD'
# Diagnostics
The next operation is FS.GG.Rendering#4242/#4243.
MD
run
expect_rc 1 'the repo-surface bare tail resolves like every other bare ref'
expect_out_has 'FS.GG.Rendering#4243 is CLOSED' 'the bare tail is resolved and found stale'
expect_eq "$(grep -c 'stale link' <<<"$OUT")" 1 'only the closed bare tail is stale'

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
#
# EVERY FIXTURE HERE NOW DECLARES THE REGISTRY'S VIEW (`mirror_roster`, #722), and that is not harness
# ceremony — it is the contract these cases are pinning. Publishing `fs-gg-game-core` used to be enough
# to make it a mirror, because `MIRRORED_SKILLS` was the only reading of "which skills are frozen
# mirrors" and nothing checked it. It is checked now: a mirror is `owner: foreign` AND `we publish it`,
# and the fixture supplies both halves. A case that publishes a mirror body and does NOT say the registry
# calls it foreign is asserting that the constant is stale — which is the § 9 red, not this § .
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§6 a fully-QUALIFIED frozen mirror is green — both directions of ref'
# The bytes Game actually ships now. A foreign ref we cannot see is trusted; a self-qualified ref to a
# skill we DO publish resolves. This is the case the old convention could not produce.
fixture
mirror_roster fs-gg-game-core
mirror_constant fs-gg-game-core
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
mirror_roster fs-gg-game-core
mirror_constant fs-gg-game-core
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
mirror_roster fs-gg-game-core
mirror_constant fs-gg-game-core
skill fs-gg-game-core <<'MD'
# game-core
See [[fs-gg-ballistics]], which Game publishes and we do not.
MD
run
expect_rc 1 'the old NOTE is gone — this is a failure now, and it is clearable: qualify it upstream'
expect_out_has 'MIRRORED body' 'and it is the REF that failed — not the mirror-list check upstream of it'

case_start '§6 the stopgap is GONE — no note stream survives on the green path'
# The stopgap printed "N [[ref]](s) in the frozen mirrors" on success. If that sentence ever comes back,
# § 1 has stopped checking the four bodies again, and it will do so QUIETLY — which is the failure this
# whole section exists to prevent. So the absence is asserted, not assumed.
fixture
mirror_roster fs-gg-game-core
mirror_constant fs-gg-game-core
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
mirror_roster fs-gg-audio
mirror_constant fs-gg-audio
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
mirror_roster fs-gg-game-core
mirror_constant fs-gg-game-core
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
mirror_roster fs-gg-persistence
mirror_constant fs-gg-persistence
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

case_start '§7 a prose-ok [[…]] marker survives a QUALIFIED ref — the delimiter is IN the value (#733)'
# THE ONE SUBTLETY `row_has` CARRIES, PINNED — because until this case the suite could not SEE it.
#
# A wiki row is `file:line:ref`, and a QUALIFIED ref carries a colon OF ITS OWN. Field-split that row
# on `:` and take `$3` and you compare against `fs-gg-rendering`, never `fs-gg-rendering:ghost` — so
# the lookup matches nothing and a LIVE marker is reported STALE. The author is then told to drop the
# one line keeping their body green, and doing what the gate says turns a green tree red. That
# reasoning lived in a comment on exactly ONE of four near-identical lookups; #733 moved it into
# `row_has`, where all four inherit it — and a fifth caller inherits the fix instead of the bug.
#
# IT WAS A LATENT HOLE AND WOULD HAVE STAYED ONE. Every other prose-ok case in this suite writes a
# BARE ref (`[[link]]`, `[[ghost]]`), where a field-split is ACCIDENTALLY correct — the value has no
# delimiter in it, so `$3` is the whole value. Measured, not assumed: a deliberately field-splitting
# `row_has` passes all 226 of the other assertions. This is the case that fails, which is the only
# reason the consolidation is safe to make. It is the `grep -H` / Game#241 one-flag-bug class, and a
# gate does not get to commit the defect it exists to catch.
#
# The marker is LOAD-BEARING here, not decorative, so BOTH tables are exercised at once:
# `fs-gg-rendering:ghost` is SELF-qualified and does NOT resolve, so § 1 dangles it without the
# marker. The TAB table (`is_ref_prose`) must still EXCUSE it, and the COLON table (`wiki_has`) must
# not then call the live marker stale.
fixture
repo_readme <<'MD'
# Product skills — authoring notes

<!-- skill-refs: prose-ok [[fs-gg-rendering:ghost]] — the SHAPE of a QUALIFIED ref, not a ref -->
**A qualified ref names its publishing repo: `[[fs-gg-rendering:ghost]]`.**
MD
run
expect_rc 0 'the qualified illustration is excused, exactly as a bare one is'
expect_out_hasnt 'stale prose-ok marker' 'the LIVE marker is not called stale — its ref carries the delimiter'
expect_out_hasnt 'dangling' 'and the ref it excuses is not reported dangling either'

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

# ── § 7.5  the bill § 3's inversion pays: OFFLINE, a repo bare #N is not examined ───────────────

case_start '§7 OFFLINE, a repo bare #N is NOT claimed as resolved — it was not examined at all'
# THE FALSE SUBJECT CLAIM, and it was live until a review ran the offline path by hand.
#
# On a published body a bare `#N` is wrong BY FORM, so § 3 damns it with no network — which is why its
# header insists § 3 stay UNGATED by `link_mode`. On a REPO body the same token is a LINK, and whether
# it resolves is f(world): the verdict genuinely cannot be reached offline. That is a fair price. What
# is NOT fair is the summary saying "in a repo-internal body a bare #N is a link, and was resolved as
# one" — unconditionally — while the link half sat skipped. An offline run over a body citing an issue
# that does not exist passed GREEN, asserting an examination that never happened: the `.github#416`
# shape, inside the gate written to close it, and invisible in CI (where an unauthenticated `gh` is
# fatal) so it would have been wrong only on the laptops of the people maintaining it.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
repo_skill Diagnostics <<'MD'
# Diagnostics
The remaining gap is tracked in #4242.
MD
SKIP_LINKS=1 run
expect_rc 0 'offline, it cannot judge the ref, so it does not fail on it'
expect_out_hasnt 'was resolved as one' 'and it does NOT claim to have resolved it'
expect_out_has 'were NOT examined' 'it says plainly that these refs went unjudged'
expect_out_has 'NOT a clean bill of health' 'and refuses to let the green be read as one'

case_start '§7 ...but a prose-ok bare #N is NOT counted among them — it was never going to be a link'
# The marker says "this is prose, do not resolve it". Warning that it went unresolved would be noise
# about a ref the author has already, deliberately, declared is not a pointer.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
repo_skill LineDrawing <<'MD'
# Line drawing

<!-- skill-refs: prose-ok #1 — the design doc's number-one bug, not issue #1 -->
Mind the design doc's "#1 LOS bug".
MD
SKIP_LINKS=1 run
expect_rc 0 'green'
expect_out_hasnt 'were NOT examined' 'a ref declared to be prose is not a link that went unchecked'

# ── § 7.6  a body may not be on BOTH surfaces ───────────────────────────────────────────────────

case_start '§7 a body that is BOTH published and repo-internal is REFUSED, not silently mis-judged'
# `is_repo_body` wins wherever it is consulted, so such a body would be judged against `.claude/skills/`
# instead of the publish set AND would lose § 3's bare-#N rejection — while genuinely materializing into
# a stranger's repo, which is the exact hazard § 3 exists for. The gate would print `ok`, and the one
# body it was most wrong about is the one it never named.
#
# It is disjoint TODAY, and it would have been easy to write "disjoint by construction" in a comment and
# move on — which is what the first draft did. But the manifest already supplies four skills from
# off-convention roots, so it is a convention, not a construction.
fixture
skill_at fs-gg-diag 'src/Diag/skill' <<'MD'
# Diag — published from src/, so it is on both surfaces
The remaining gap is tracked in #4242.
MD
run
expect_rc 1 'a body on two surfaces is a refusal, not a guess'
expect_out_has 'cannot be on BOTH surfaces' 'and it says which invariant broke'
expect_out_has 'src/Diag/skill/SKILL.md' 'and names the body'

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

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 8  THE THIRD SURFACE — the wrappers are a SUBJECT; the coordination kit is not  (#723)
# ════════════════════════════════════════════════════════════════════════════════════════════════
# #698 left `.claude/skills/` and `.agents/skills/` out of the subject because they carry 88 bare `#N`
# that are FS-GG/.github's issue numbers, not ours. The provenance is what unpicks that: all 88 sit in
# the FOUR coordination-kit bodies, which a bot syncs and `coordination-coherence.yml` byte-locks — we
# could not qualify them if we tried, because the qualified bytes would fail that gate. The other 49
# wrappers are ours, and they carry nothing at all.
#
# So the subject widens and the kit is carved out of it — and a CARVE-OUT IS THE DANGEROUS DIRECTION,
# which is what most of these cases are about. A gate that skips a body says `ok` in exactly the same
# words as one that examined it.

case_start '§8 a wrapper body in .claude/skills/ is IN the subject — its dangling ref is caught'
fixture
claude_skill fs-gg-alpha
claude_body fs-gg-alpha <<'MD'
# alpha wrapper
See [[fs-gg-nowhere]].
MD
run
expect_rc 1 'a wrapper is a body, and its refs are checked like any other'
expect_out_has 'dangling [[fs-gg-nowhere]]' 'names the dangling ref'
expect_out_has '.claude/skills/fs-gg-alpha/SKILL.md' 'and the wrapper it is in'

case_start '§8 ...and so is one in .agents/skills/ — the root a single-root scan would never see'
fixture
claude_skill fs-gg-alpha
agents_body fs-gg-alpha <<'MD'
# alpha wrapper, Codex-active
See [[fs-gg-nowhere]].
MD
run
expect_rc 1 'the second root is a subject too — scanning one and calling the surface checked is the #698 hole, one directory over'
expect_out_has '.agents/skills/fs-gg-alpha/SKILL.md' 'and the finding names THAT root, not the Claude one'

case_start '§8 a KIT body is OUT of the subject — its bare #N is not resolved against us'
# The whole point. `#419` here is `.github`'s issue number; there is no FS.GG.Rendering#419 in this
# fixture's tracker, so a subject that scanned this body would resolve it, 404, and report a dangling
# link — a red that no diff of ours could clear, because qualifying it would break the byte-identity
# `coordination-coherence.yml` enforces.
fixture
claude_body pnext-item <<'MD'
# pnext-item
Canonical protocol lives in FS-GG/.github. The lock is a CAS (#419), and minting is the tool's job (#551).
MD
run
expect_rc 0 'the kit body is not scanned, so its .github issue numbers are not resolved here'
expect_out_hasnt 'dangling link' 'no dangling link is manufactured out of another repo issue number'
expect_out_hasnt '#419' 'and the ref is never mentioned at all — it is out of subject, not excused'

case_start '§8 ...and a dangling [[ref]] in a kit body is not reported either — OUT is out'
fixture
claude_body cross-repo-coordination <<'MD'
# cross-repo-coordination
See [[fs-gg-nowhere]] — .github's word, not ours.
MD
run
expect_rc 0 'a body we cannot edit is not a body we report on'
expect_out_hasnt 'dangling [[fs-gg-nowhere]]' 'the carve-out covers the wiki half too'

case_start '§8 the carve-out is SCOPED to the four — the same ref in OUR wrapper still fails'
# The hatch is not "anything under .claude/skills/". If it were, the widening would have bought nothing.
fixture
claude_skill fs-gg-alpha
claude_body fs-gg-alpha <<'MD'
# alpha
The lock is a CAS (#419), and see [[fs-gg-nowhere]].
MD
run
expect_rc 1 'our own wrapper gets no such excuse'
expect_out_has 'dangling [[fs-gg-nowhere]]' 'the wiki half fires'

case_start '§8 the kit is in the VOCABULARY even though it is out of the subject'
# Out of subject ≠ out of the world. An agent standing in this tree really can invoke [[pnext-item]],
# so a body of OURS that points at one is CORRECT, and calling it dangling would fire on right work.
fixture
claude_body pnext-item <<'MD'
# pnext-item
MD
repo_skill Scene <<'MD'
# Scene
To take an item, use [[pnext-item]].
MD
run
expect_rc 0 'a ref to a kit skill RESOLVES — the kit is a name an agent here can invoke'
expect_out_hasnt 'dangling [[pnext-item]]' 'and is not reported'

case_start '§8 KIT_SKILLS drift, the FAIL-OPEN direction: excluding a body no kit row protects'
# The one that matters. A name wrongly in KIT_SKILLS is a body of OURS whose refs nothing examines,
# under a gate that still prints `ok`. It cannot be allowed to be quiet, so it is not.
fixture
kit_roster cross-repo-coordination intra-repo-parallel-work check-board   # pnext-item is NOT a kit row
run
expect_rc 1 'a constant that excludes more than canonical does is a blind spot, and it is fatal'
expect_out_has 'does not match' 'says the constant and the roster disagree'
expect_out_has '< pnext-item' 'names the body it was about to skip for no reason'
expect_out_has 'examined by NOTHING' 'and says what that would have cost'

case_start '§8 KIT_SKILLS drift, the other direction: the kit GREW and the constant is stale'
fixture
kit_roster cross-repo-coordination intra-repo-parallel-work check-board pnext-item fs-gg-newkit
run
expect_rc 1 'a kit row the constant does not know about is also fatal'
expect_out_has '> fs-gg-newkit' 'names the new kit body'
expect_out_has 'the kit grew' 'and diagnoses which way the drift went'

case_start '§8 a roster the gate cannot parse is a REFUSAL, not a green pass'
# An empty read is not "the kit is empty" — it is the check failing to run, and blessing every name in
# the constant on the strength of a fetch that returned nothing is the `.github#416` shape exactly.
#
# This case doubles as the DECOY test. The fake's roster carries `kind: skill` rows OUTSIDE the `kit:`
# block, before it and after it. A parse that greps the whole file, or that enters the block and never
# leaves, would pick them up — and would then report DRIFT (a `>` line naming a decoy) instead of this
# refusal. So the verdict here is only reachable by a parse that scopes to the block correctly.
fixture
kit_roster    # no kit rows at all
run
expect_rc 1 'a kit roster with no skill rows is a broken check, not an empty one'
expect_out_has 'found no `kind: skill`' 'says the read came back without the rows it needed'
expect_out_hasnt 'decoy' 'and the parse never picked up a kind: skill row outside the kit block'

case_start '§8 the WRAPPER subject cannot silently vanish either — two constituents, two refusals'
# The bug this widening nearly shipped, in reverse. Build the subject as one union and refuse only when
# the whole union is empty, and deleting every src/*/skill/SKILL.md stops refusing — the wrappers hold
# the union up, and the gate reports green over a library surface that has GONE. Both constituents get
# their own refusal; this pins the new one.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
rm -rf "$FIX/.claude/skills/fs-gg-fixture"
mkdir -p "$FIX/.claude/skills/pnext-item"          # vocabulary survives; only KIT bodies remain
printf -- '---\nname: pnext-item\n---\n' >"$FIX/.claude/skills/pnext-item/SKILL.md"
run
expect_rc 1 'a wrapper subject consisting only of the kit is a subject that has moved, not an empty one'
expect_out_has 'no non-kit wrapper bodies' 'and it names what it went looking for'

case_start '§8 CI + unauthenticated gh → the kit exclusion cannot be verified, so the gate FAILS'
fixture
CI_MODE=1 GH_NOAUTH=1 run
expect_rc 1 'an exclusion it cannot verify is one it may not act on in CI'
expect_out_has 'KIT_SKILLS cannot be verified' 'says which claim it could not stand behind'
expect_out_hasnt 'ok — 4 coordination-kit' 'and does NOT report the exclusion as verified'

case_start '§8 locally, an unverifiable exclusion is ANNOUNCED, never silently trusted'
fixture
GH_NOAUTH=1 run
expect_rc 0 'the local run still gates — the subject is hermetic, only the cross-check is not'
expect_out_has 'was NOT verified' 'and it says the exclusion went unchecked'
expect_out_has 'refs nothing examined' 'spelling out what a wrong name in that list would cost'

case_start '§8 SKILL_REFS_SKIP_LINKS does NOT skip the kit check — that flag is about LINKS'
# "Degrade toward MORE checking, never less" (§ 3). The flag exists to spare a laptop ~14 link
# round-trips; it says nothing about whether the roster can be read, and a run that CAN verify the
# exclusion in one request must not throw that away because an unrelated half was turned off.
fixture
# A real link, so that "the link half was SKIPPED" is distinguishable from "there was nothing to skip"
# (`link_mode=empty`). Without one this case would pass while asserting nothing about the flag at all.
skill fs-gg-alpha <<'MD'
# alpha
Filed as https://github.com/FS-GG/FS.GG.Rendering/issues/4242.
MD
issue FS-GG/FS.GG.Rendering#4242 open
SKIP_LINKS=1 run
expect_rc 0 'green'
expect_out_has "verified against FS-GG/.github's kit roster" 'the exclusion was still verified'
expect_out_hasnt 'was NOT verified' 'skipping the link half did not silently widen the blind spot'
expect_out_has 'were NOT checked' 'while the LINK half really was skipped, as asked'

case_start '§8 a roster the gate could not READ is diagnosed as unreadable, NOT as a parse bug'
# The .github#430 shape, and the one this gate must never commit: a fetch that FAILED (rate limit, 403,
# network) reported as "the roster's shape changed", sending the reader off to debug a parse that is
# perfectly fine. The two states get two messages, and the real error is quoted back.
fixture
CI_MODE=1 KIT_FAIL=1 run
expect_rc 1 'a roster it could not read is not one it may act on in CI'
expect_out_has 'could not READ' 'says it failed to READ the file'
expect_out_has 'HTTP 403' "and quotes gh's own error rather than swallowing it"
expect_out_has 'do not go' 'and steers the reader AWAY from the parse'
expect_out_hasnt 'found no `kind: skill`' 'it does NOT claim it read a roster with no kit rows'

case_start '§8 ...and locally that same unreadable roster degrades to a NOTE, naming the real cause'
fixture
KIT_FAIL=1 run
expect_rc 0 'an offline laptop is not a defect in the tree'
expect_out_has 'was NOT verified' 'the exclusion is announced as unverified'
expect_out_has 'HTTP 403' 'and the reason is the one gh actually gave'

case_start '§8 a green run NAMES the exclusion — what you did not look at is part of the verdict'
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
run
expect_rc 0 'green'
expect_out_has 'OUT of subject' 'the skipped bodies are stated, not left to be inferred'
expect_out_has "verified against FS-GG/.github's kit roster" 'and so is the fact that the skip was earned'

# ════════════════════════════════════════════════════════════════════════════════════════════════
# § 9  MIRRORED_SKILLS IS VERIFIED — the OTHER hand-written narrowing  (#722)
#
# This gate once duplicated a mirror set maintained by a separate checker. #1147 retired that checker
# after all foreign bodies left Rendering. These tests now cover only this gate's dormant future-mirror
# capability: if Rendering ever publishes a foreign-owned body again, the constant that narrows reference
# checking must agree with the registry and manifest.
#
# AND #714 MADE THE DRIFT DANGEROUS. Being listed used to DEMOTE a § 1 finding to a note, so an omission
# meant MORE checking and rot failed safe. #714 inverted that: a listed body hard-fails a bare `[[ref]]`,
# so a mirror MISSING from the list has its refs judged against OUR publish set alone — green here, and
# dangling in the owning repo's gate. It fails OPEN, which is the incoherence #714 exists to end,
# re-created by an omission.
#
# A mirror is `owner: foreign` AND `we publish it`. The registry settles only the first half — its eight
# foreign product rows are indistinguishable there, and Rendering mirrors four while deliberately shipping
# no counterpart for the other four — so both halves are exercised here, and so is the seam between them.
# ════════════════════════════════════════════════════════════════════════════════════════════════

case_start '§9 MIRRORED_SKILLS drift, the FAIL-OPEN direction: a mirror the constant does not know'
# The one that matters. The registry says we do not own this published body; the constant does not list
# it; so § 1 judges its bare refs against OUR publish set and calls them green — while they dangle in the
# gate of the repo that actually owns the bytes.
fixture
mirror_roster fs-gg-scene            # the registry says fs-gg-scene is owned elsewhere now...
skill fs-gg-scene <<'MD'
# scene — a fifth mirror, and this gate does not know it
See [[fs-gg-alpha]] — bare, and green here, because nothing told this gate to look harder.
MD
skill fs-gg-alpha <<'MD'
# alpha
MD
run                                   # ...and MIRRORED_SKILLS (empty since ADR-0063) does not list it
expect_rc 1 'a mirror the constant omits is a fail-open, and the gate refuses to run on it'
expect_out_has '< fs-gg-scene' 'names the mirror it did not know about'
expect_out_has 'DANGEROUS' 'and says which direction of drift this is'
expect_out_has 'publish set ALONE' 'spelling out what the omission costs'

case_start '§9 MIRRORED_SKILLS drift, the other direction: the constant claims a mirror we OWN'
# #696's end state, arrived at halfway: the mirror is retired, the registry says the body is ours, and the
# constant still names it — so this gate hard-fails bare refs in a body we fully own. A loud false red.
fixture
mirror_constant fs-gg-audio
skill fs-gg-audio <<'MD'
# audio — ours now, per the registry, and no `mirror_roster` says otherwise
MD
run
expect_rc 1 'a stale entry is also fatal — loudly, which is the point'
expect_out_has '> fs-gg-audio' 'names the entry the registry no longer backs'
expect_out_has 'false red' 'and diagnoses it as the harmless-but-loud direction'

case_start '§9 a FOREIGN product row we do NOT publish is not a mirror'
# ballistics/ai/effects/physics are `owner: fs-gg-game` product rows that Rendering deliberately ships no
# counterpart for (.github#486). They are indistinguishable in the registry from the four we DO mirror, so
# a derivation that takes every foreign product row instead of intersecting with the publish set would
# manufacture four mirrors that do not exist here. The fake serves them in every fixture; this pins that
# they stay out.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
run
expect_rc 0 'a body we do not ship is not a mirror, however foreign the registry says it is'
expect_out_has '0 frozen mirror(s)' 'and the count says so'

case_start '§9 a foreign PROCESS row is not a mirror either — the scope filter is real'
# The fake always serves `fs-gg-decoy-process`: `owner: fs-gg-game`, but `scope: process`. Publish it, and
# a parse that ignores `scope:` derives it as a mirror the constant does not list — i.e. it reddens with
# the § 9 fail-open message. The scope filter is what keeps that from happening.
fixture
skill fs-gg-decoy-process <<'MD'
# a process skill, foreign-owned, and NOT a product mirror
MD
run
expect_rc 0 'a process row is not in the mirror question at all'
expect_out_hasnt 'fs-gg-decoy-process' 'the scope filter dropped it, so it never reached the comparison'

case_start '§9 an entry for a body we do NOT publish is inert, and is not flagged'
# THE EXACT BOUNDARY, and it is asserted rather than left to be inferred. `mirror_bodies` filters this
# constant through `is_published`, so an entry naming a body we do not publish reaches no verdict this
# gate can render. The comparison is scoped to the published set for that reason — everything that CAN
# change a verdict is checked, and this cannot. A fixture publishing none of the injected entries is the
# normal case, and it is green. (Production `MIRRORED_SKILLS` is EMPTY since ADR-0063, so the four are
# injected here to keep exercising the unpublished-entry boundary rather than a vacuous empty constant.)
fixture
mirror_constant fs-gg-game-core fs-gg-audio fs-gg-persistence fs-gg-model-swap
skill fs-gg-alpha <<'MD'
# alpha
MD
run
expect_rc 0 'the four unpublished entries change no verdict, so they are not a finding'
expect_out_has "verified against FS-GG/.github's skill registry" 'and the list was still verified'

case_start '§9 a registry that parses to NO product rows is the check FAILING, not an empty mirror set'
# An empty read is not "there are no mirrors" — it is the registry's shape moving under the parse, and
# blessing the constant on the strength of a match that found nothing is the fail-open one `sed` away.
fixture
skill fs-gg-alpha <<'MD'
# alpha
MD
MIRRORS_EMPTY=1 run
expect_rc 1 'a registry with no product rows is a broken parse, not an empty answer'
expect_out_has 'found no `scope: product` rows' 'says what it went looking for and did not find'
expect_out_has 'teach this parse' 'and sends the reader at the parse, not at the constant'

case_start '§9 CI + unauthenticated gh → MIRRORED_SKILLS cannot be verified, so the gate FAILS'
fixture
CI_MODE=1 GH_NOAUTH=1 run
expect_rc 1 'a narrowing it cannot verify is one it may not act on in CI'
expect_out_has 'MIRRORED_SKILLS cannot be verified' 'says which claim it could not stand behind'
expect_out_has 'dangling in the owning' 'and what the blind spot would cost'

case_start '§9 locally, an unverified mirror list is ANNOUNCED, never silently trusted'
fixture
GH_NOAUTH=1 run
expect_rc 0 'the local run still gates — the subject is hermetic, only the cross-check is not'
expect_out_has 'MIRRORED_SKILLS was NOT verified' 'and it says the list went unchecked'

case_start '§9 the registry read FAILS → the gate quotes gh, and does not blame the parse'
# "COULD NOT READ IT" and "READ IT AND IT SAID NOTHING" are different sentences (.github#430). A rate
# limit must not surface as a shape change, or the reader goes off to debug a parse that was fine.
fixture
CI_MODE=1 MIRRORS_FAIL=1 run
expect_rc 1 'a registry it could not read is not a constant it may call verified'
expect_out_has 'could not READ' 'names the read as the thing that failed'
expect_out_has 'HTTP 403' "and quotes gh's own error rather than swallowing it"
expect_out_hasnt 'found no `scope: product` rows' 'it does NOT claim it read a registry with no rows'

case_start '§9 SKILL_REFS_SKIP_LINKS does NOT skip the mirror check — that flag is about LINKS'
# The § 0c rule, and it applies to both narrowings: degrade toward MORE checking, never less. A flag that
# means "skip the link half" says nothing about whether a registry can be read.
fixture
mirror_constant fs-gg-audio
skill fs-gg-audio <<'MD'
# audio — the constant lists it, and no `mirror_roster` says the registry agrees
MD
SKIP_LINKS=1 run
expect_rc 1 'the mirror list is still verified, and still wrong'
expect_out_has '> fs-gg-audio' 'the drift is reported even with the link half off'

case_start '§9 a green run NAMES the verification — an unchecked narrowing is not silently green'
fixture
mirror_roster fs-gg-game-core
mirror_constant fs-gg-game-core
skill fs-gg-game-core <<'MD'
# game-core — a real mirror, and the registry agrees
See [[fs-gg-game:fs-gg-ballistics]].
MD
run
expect_rc 0 'green'
expect_out_has '1 frozen mirror(s)' 'the count of bodies held to the stricter rule is stated'
expect_out_has "verified against FS-GG/.github's skill registry" 'and so is the fact that it was earned'

# ── summary ─────────────────────────────────────────────────────────────────────────────────────
harness_summary test-check-skill-refs
