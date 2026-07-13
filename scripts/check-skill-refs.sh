#!/usr/bin/env bash
# check-skill-refs — fail on a dangling pointer in this repo's published product skills.
#
# Ported from FS.GG.Game's gate (Game#35 → #202 → #208 → #238/#241), which was written after the
# same rot was found there. The three checks, the two escape hatches and the scope/sweep split are
# that script's, near-verbatim and deliberately so: it is battle-tested, and a gate that drifts
# between repos is a gate whose verdict nobody can predict. What is Rendering's — and what Game has
# no reason to know — is § 0 and the four constants. Filed as FS.GG.Rendering#655.
#
# A published SKILL.md makes three kinds of promise to its reader, and this script checks all three:
#
#   [[wiki-ref]]        (Game#35)  — "a skill by this name resolves"
#   an issue/PR link    (Game#202) — "there is a live issue at the other end"
#   a bare `#N`         (Game#208) — promises nothing it can keep: § 3 REJECTS it outright
#
# ── 0. WHAT THIS REPO PUBLISHES, AND THE FOUR BODIES IT DOES NOT OWN ────────────────────────────
# READ THIS BEFORE "FIXING" ANY REF THIS GATE REPORTS. `template/product-skills/` ships 17 skills,
# and they are not all Rendering's to edit:
#
#   13 are OURS          (registry `owner: fs-gg-rendering`) — scene, symbology, elmish, skiaviewer,
#                        layout, keyboard-input, styling, ui-widgets, testing, and the four game-sim
#                        skills collision / grids / line-drawing / visibility, which ADR-0022 P4
#                        explicitly did NOT migrate.
#    4 are FROZEN MIRRORS (registry `owner: fs-gg-game`) — game-core, audio, persistence, model-swap.
#                        ADR-0022 P4 migrated OWNERSHIP to FS.GG.Game; ADR-0022 §6 accepted the
#                        two-copies cost, so we still SHIP them and our bytes must stay IDENTICAL to
#                        FS.GG.Game's. (Verified so at the time of writing. `fsgg-skill-registry-check`
#                        has a `frozen-mirror` arm that reads `owner:` precisely to find this twin.)
#
# SO: A REF THIS GATE REPORTS INSIDE ONE OF THOSE FOUR IS NOT YOURS TO FIX HERE. Editing it would
# break the byte-identity — trading a dangling pointer for a mirror that silently diverges, which is
# strictly the worse defect, because nothing in THIS repo would report it. Fix it in FS.GG.Game and
# re-sync the mirror. In practice this costs nothing: Game runs this same gate, so its bodies are
# already clean, and we inherit every fix byte-for-byte — the `closed-ok` markers already sitting in
# our audio and persistence bodies arrived exactly that way, authored in Game and mirrored in.
#
# INSIDE A MIRRORED BODY, EVERY [[ref]] IS QUALIFIED — AND THAT IS THE WHOLE POINT (#714, Game#279).
#
# This repo used to argue the opposite, and the argument was: a `[[ref]]` promises *"a skill by this
# name resolves WHERE YOU ARE READING THIS"*, the reader stands in a SCAFFOLDED PRODUCT whose
# `.agents/skills/` we materialize from OUR manifest, and `[[fs-gg-game-core]]` resolves there because
# WE ship a body for it. True — and it settles the question for OUR OWN bodies, which only we mirror
# into products. It does NOT settle it for the four the mirror duplicates, because those bytes are read
# by TWO gates against TWO publish sets, and a bare ref resolves in exactly one of them:
#
#   [[fs-gg-ballistics]]   resolves in GAME (they publish it), dangles HERE (we do not).
#   [[fs-gg-scene]]        resolves HERE (we publish it),       dangles in GAME (they do not).
#
# Whichever way you write it bare, it is wrong in one repo. That is arithmetic, not a policy dispute —
# and it is why this gate NOTE'd the mirrors instead of failing on them (the #655 stopgap): the red was
# real and no diff of ours could clear it.
#
# The premise that made it unclearable was that a SELF-qualified ref was REFUSED ("write it bare"). Game
# removed that refusal — a ref that RESOLVES is true, whether or not you name its owner — and qualified
# every ref in the four bodies, both directions. Now one byte sequence satisfies both gates:
#
#   [[fs-gg-game:fs-gg-ballistics]]   in GAME: self + published -> CHECKED, ok.   HERE: foreign -> trusted.
#   [[fs-gg-rendering:fs-gg-scene]]   in GAME: foreign -> trusted.                HERE: self + published -> CHECKED, ok.
#
# Each ref is validated EXACTLY ONCE, by the only repo that can see the tree it names, and trusted
# everywhere else — which is better than a tie-break, because under the old convention this gate was
# obliged to have an opinion about `fs-gg-ballistics`, a skill it cannot see.
#
# So § 1 now: accepts a self-qualified ref that resolves; trusts a foreign qualified one; and REJECTS A
# BARE REF INSIDE A MIRRORED BODY, because that is the one shape that cannot be right in both repos.
# Bare is still right everywhere else — in our own 13 bodies, which no one else reads.
#
# The cost, paid knowingly: a product reader sees `[[fs-gg-game:fs-gg-game-core]]`, which reads like a
# pointer OUT of the product for a skill that is in fact IN it. Retiring the mirror (#696) is the real
# fix and makes the qualifiers merely redundant rather than load-bearing; this is the coherent cheap one.
#
# ── 1. WIKI REFS (Game#35) ──────────────────────────────────────────────────────────────────────
# A skill body that writes `[[fs-gg-ballistics]]` promises the reader a skill this repo publishes. It
# does not: `fs-gg-ballistics` is authored in FS.GG.Game and has NO body here, so it materializes
# into no product of ours. An agent following the pointer finds nothing.
#
# THE CONVENTION. A `[[ref]]` is either
#   * BARE      — `[[fs-gg-scene]]`                → MUST name a skill this repo publishes, or
#   * QUALIFIED — `[[fs-gg-game:fs-gg-ballistics]]` → `<owner>:<skill-id>`, naming the publishing
#                 repo's registry `owner` id (registry/skills.yml in FS-GG/.github).
# Anything else is a dangling ref and fails. Bare code spans (`fs-gg-scene`) are prose, not
# pointers, and are deliberately NOT checked — only `[[...]]` promises resolvability.
#
# The owner vocabulary is the registry's: a qualified ref whose owner is unknown is a typo, and a
# ref qualified with THIS repo's own owner must resolve locally (write it bare instead).
#
# ── 2. ISSUE / PR LINKS (Game#202) ──────────────────────────────────────────────────────────────
# The same sentence in fs-gg-persistence pointed at a CLOSED issue for four generations running
# (#445 → #535 → #587 → #535 again, the last one re-introduced by the very commit that adopted the
# "canonical" body). Every rot was caught by a human reading the prose; none by CI.
#
# THE CONVENTION. A pointer is an issue/PR link in one of two QUALIFIED forms:
#   * URL        — https://github.com/FS-GG/FS.GG.Rendering/issues/535
#   * SHORTHAND  — `FS.GG.Rendering#535`, or `FS-GG/FS.GG.Rendering#535` (owner defaults to FS-GG)
# It must resolve, and it must be OPEN — because the prose around it almost always tells the reader
# to go and DO something there ("add your case there"), and a closed issue is a place nobody can act.
#
# A BARE `#535` IS NOT ONE OF THOSE FORMS. It is not resolved — it is REJECTED. See § 3.
#
# CLOSED-AND-STILL-CORRECT IS REAL, so there is an opt-out. A body may legitimately cite the issue
# that IMPLEMENTED something — fs-gg-audio's "the scaffold ships it wired (FS.GG.Rendering#245)" is
# history, and history stays closed. Excuse such a citation with a marker naming the exact ref:
#
#     <!-- skill-refs: closed-ok FS.GG.Rendering#245 — cited as the issue that wired the seam -->
#
# WHY AN ERROR WITH AN OPT-OUT, AND NOT A WARNING. #202 floated a warning. A warning is what we
# already have: CI green, and a human expected to read the prose and notice. That is precisely the
# failure this gate exists to end — and the "gate reports green on a missing subject" shape
# (FS-GG/.github#416) the org keeps rediscovering late. The marker is what makes the strict default
# affordable: an honest citation costs one line and is then SELF-DOCUMENTING to the next reader,
# and the intent becomes machine-readable rather than inferred from the surrounding sentence.
# A marker that excuses nothing is itself reported — a stale allowlist is where the next rot hides.
#
# ── 3. BARE `#N` REFS (Game#208) ────────────────────────────────────────────────────────────────
# A bare `#999` in a shipped body is an ERROR. Not "unresolvable" — WRONG, and wrong by construction:
#
#   THESE BODIES MATERIALIZE INTO SOMEBODY ELSE'S REPOSITORY (`profile in [game, sample-pack]`, and
#   for most of ours `app` too). GitHub renders `#999` against the repo it is READ in, so in a
#   scaffolded product it links to that product's OWN tracker — an unrelated issue, or nothing. It
#   never points here.
#
# So its upstream state is beside the point: there is no `#999` we could resolve it against, and a
# bare ref is never correct in a shipped body even when the issue it meant is open. This is why the
# check needs no network, and why — unlike a stale link — it cannot be fixed by repointing it. The
# only fix is to QUALIFY it (`FS.GG.Rendering#999`), which survives materialization.
#
# Bare refs are ambiguous with prose — a body may say `the design doc's "#1 LOS bug"`, meaning the
# number-one bug and not issue #1. That ambiguity is real, and it is an argument for an ESCAPE HATCH,
# not for silence. Silence is the `.github#416` shape this gate exists to close — green over a
# pointer it never examined. So: reject by default, and let a body declare the one honest exception,
# naming the exact number:
#
#     <!-- skill-refs: prose-ok #1 — the design doc's number-one bug, not issue #1 -->
#
# NO CARVE-OUT FOR CODE. A `#123` inside a fence or a code span is reported too. GitHub does not
# autolink there, so the RENDERING hazard is absent — but the pointer is still unresolvable to the
# reader, and exempting code would hand anyone a way to park an unchecked ref where the gate cannot
# see it. That is the silent-no-op hole again, reopened as a convenience. The marker costs one line.
#
# THE MARKER IS FILE-SCOPED, and knowingly so — `closed-ok` is too, and a marker that had to sit on
# the ref's own line could not be written above a list, which is where the one real case wants it.
# Own the cost, because it is not free: `prose-ok #1` keys on a BARE INTEGER, so it excuses EVERY
# bare `#1` in that file — including a genuine `see #1` somebody adds later. `closed-ok` keys on a
# whole `owner/repo#num` and is far harder to collide with. So keep prose-ok markers RARE and their
# numbers odd-looking; a file that wants to excuse `#1`, `#2` and `#3` is a file that should be
# rewording its prose instead.
#
# WHY GUESSING IS NOT AN OPTION. `#1` in quotes reads as prose; `#1` after "see" reads as a pointer.
# Teaching the script that difference would make the gate's verdict depend on the surrounding
# sentence — unpredictable to the author, and wrong often enough to be turned off. The author knows
# which one they meant; the marker is how they say it, and it is self-documenting to the next reader.
#
# NETWORK. Resolving a link needs the API (REST only; a handful of calls, no GraphQL). The gate must
# never SELF-SKIP: under GITHUB_ACTIONS an unresolvable link is a FAILURE, never a pass. Locally,
# with no `gh` or no auth, the link half announces loudly that it did not run, and the wiki half
# still does. `SKILL_REFS_SKIP_LINKS=1` skips the link half locally; it is IGNORED in CI.
#
# ── 4. WHAT A MERGE GATE MAY ASK (Game#238) ─────────────────────────────────────────────────────
# Sort the three checks by what their verdict is a FUNCTION of, because they do not agree:
#
#   § 1 wiki refs   f(tree)         hermetic — same tree, same answer, forever
#   § 3 bare #N     f(tree)         hermetic — the FORM is the defect; no network can change it
#   § 2 link state  f(tree, WORLD)  NOT hermetic — it decays in place, with no commit
#
# § 2 asks whether the world still agrees with what we published. That is a real and valuable
# question — it is the one that caught the persistence pointer rotting four generations running.
# But its answer moves on its own. FS.GG.Rendering#494 was closed at 14:43:32Z, and from that minute
# FS.GG.Game's `main` — green minutes earlier, unchanged since — failed. The gate did not become
# wrong; the world moved, and § 2 correctly reported it. It just reported it AT whoever happened to
# have a PR open (Game#238).
#
# THE RULE THIS ENFORCES: **a merge gate may only demand a change inside the diff it is gating.**
# An unscoped § 2 breaks that rule, and not merely unfairly — IMPOSSIBLY. A diff touching four
# pathfinding files could only be turned green by an edit in `fs-gg-persistence/SKILL.md`, which is
# not in that item's declared `Paths:`. Under ADR-0021/0027 the author cannot just fix it: they must
# `widen` onto a file their item has nothing to do with, and collide with whoever holds the skills.
# The gate MANUFACTURES a false collision, and the worker's only honest move is to prove the red is
# not theirs.
#
# So § 2 is not weakened — a warning is what we already had, and § 2's own header explains why that
# failed. It is SCOPED. `--changed <base>` restricts the link half to the skill bodies THIS diff
# touches:
#
#   * A diff that touches a skill OWNS that skill's pointers. The fix — repoint, or `closed-ok` —
#     is inside a file the diff already holds, so it is always compliable. Every rot § 2 was written
#     to catch is introduced by such a diff, so authorship-time strictness is fully preserved.
#   * A diff that touches no skill CANNOT be reddened by the link half. There is no edit it could
#     make, so there is no verdict it deserves.
#
# AND THE SWEEP IS WHAT MAKES THAT HONEST — the two halves are ONE change, and shipping the scope
# without the sweep would be strictly worse than shipping neither. Scoping alone leaves every link
# in an unchanged file resolved by nobody: a gate green over a subject it never examined, which is
# the exact `.github#416` shape this script exists to close, reintroduced by its own fix. So the
# FULL sweep still runs — on a schedule, over `main`, in .github/workflows/skill-refs-sweep.yml,
# where it gates nothing. There, a red means the world moved and a citation needs maintenance, which
# is TRUE and is addressed to the repo. It is not an accusation aimed at a stranger's diff.
#
# Same subject, same strictness, two questions, and each asked where it can be answered:
#
#   did this DIFF introduce a bad pointer?   → the merge gate, blocking, fix is inside the diff
#   has the WORLD moved under a good one?    → the sweep, non-blocking, fix is a maintenance task
#
# DEGRADE TOWARD MORE CHECKING, NEVER LESS. If `--changed` gets a base it cannot resolve (a force
# push; `github.event.before` all-zeros on a branch's first push), it does NOT quietly conclude
# "no files changed" and pass — that is the silent no-op again, and it would disable the gate at
# exactly the moment history looks strange. It announces the fallback and sweeps EVERYTHING.
#
# Usage: scripts/check-skill-refs.sh                  # full sweep: every link in the tree
#        scripts/check-skill-refs.sh --changed <ref>  # link half only for skills this diff touches
set -euo pipefail

cd "$(dirname "$0")/.."

CHANGED_BASE=""
while (($#)); do
  case $1 in
    --changed)
      [[ $# -ge 2 ]] || { echo "check-skill-refs: --changed needs a base ref" >&2; exit 2; }
      CHANGED_BASE=$2; shift 2 ;;
    --changed=*) CHANGED_BASE=${1#*=}; shift ;;
    -h|--help)
      echo "usage: check-skill-refs.sh [--changed <base-ref>]"
      echo "  (no args)          full sweep: every issue/PR link in the tree"
      echo "  --changed <ref>    link half only for skill bodies changed since <ref>"
      echo "                     ([[refs]] and bare #N are hermetic and always sweep the tree)"
      exit 0 ;;
    *) echo "check-skill-refs: unknown argument '$1'" >&2; exit 2 ;;
  esac
done

# The MANIFEST is the publish set — not a directory listing. This is the one structural change from
# FS.GG.Game's script, and it is not a refinement: a direct port is WRONG here, and silently.
#
# Game's `is_published` globs `template/product-skills/*/`, which in Game is exactly what it ships.
# In Rendering it is not. We materialize 21 skills; only 17 live under that root. The other four are
# supplied from off-convention paths — fs-gg-project from `template/base/.agents/skills/`,
# fs-gg-samples from `template/fragments/`, and the two feedback skills from `template/feedback*/`.
# A directory scan cannot see them, so it calls a CORRECT `[[fs-gg-project]]` dangling — and the
# suggested fix (`[[fs-gg-rendering:fs-gg-project]]`) is then reported as self-qualified. A false red
# with no green on the other side of it, which is the one thing a gate may never do.
#
# So ask the artifact that KNOWS. `template/skill-manifest/skill-manifest.json` is what the scaffold
# materializes from: generator-produced (`scripts/generate-skill-manifest.fsx`), byte-digest-checked,
# and CI-drift-guarded, so it cannot quietly disagree with the tree. It is also what FS-GG/.github's
# registry reconciles FROM. If it lists a skill, a product gets that skill — which is precisely the
# question a `[[ref]]` asks — so the manifest is not merely a better proxy for the publish set, it IS
# the publish set, and every other source of truth here is downstream of it.
MANIFEST="template/skill-manifest/skill-manifest.json"

# The SUBJECT is those 21 published bodies — the files that MATERIALIZE into somebody's product and
# so make promises to a reader who is not us. Deliberately NOT `find $SKILL_ROOT -name '*.md'`, which
# is Game's scan: that sweeps `template/product-skills/README.md` too, and the README is a Rendering-
# internal doc ABOUT the convention, not a published body. It ships nowhere, promises nobody, and it
# discusses `[[…]]` refs in the abstract — so scanning it reports its illustrations as dangling refs.
# A gate that fires on the document explaining it is a gate people learn to ignore. Repo-internal docs
# are a different subject with different stakes; this script does not claim them, and says so here
# rather than leaving the boundary to be inferred from a glob.
SELF_OWNER="fs-gg-rendering"
# This repo's GitHub name — the qualification a bare `#N` in a body of OURS almost always wants. It
# is only ever a SUGGESTION in § 3's message: the author may well have meant another repo, and the
# gate does not guess (see § 3).
SELF_REPO="FS.GG.Rendering"
# registry/skills.yml `owner:` vocabulary (FS-GG/.github), one per line.
KNOWN_OWNERS=$'fs-gg-game\nfs-gg-rendering\nfs-gg-sdd'
# The org an unqualified `FS.GG.Rendering#535` belongs to.
DEFAULT_OWNER="FS-GG"

# The FROZEN MIRRORS (§ 0): bodies we ship but FS.GG.Game owns and authors. Exactly the registry rows
# whose `owner:` is fs-gg-game AND which we also publish — i.e. ADR-0022 P4's migration set. It is a
# constant because the registry lives in another repo and this gate takes no network to answer a
# hermetic question.
#
# NOTHING VERIFIES IT, and that is stated here rather than left to be discovered: the manifest carries
# no `owner` field, so this repo holds no local evidence of who owns a body. Both ways it can rot fail
# SAFE, which is why the absence is tolerable — see the note above `mirror_bodies` for the argument,
# and do not add a guard here that only appears to check it.
MIRRORED_SKILLS=$'fs-gg-game-core\nfs-gg-audio\nfs-gg-persistence\nfs-gg-model-swap'

# NOT `exit 0`. A missing manifest is not "nothing to check" — it is this gate's entire subject gone
# missing, and passing green over it is the `.github#416` shape (a gate reports green because it found
# no subject) that § 2 and § 3 exist to close. If the manifest is gone, the gate is broken; say so.
if [[ ! -f $MANIFEST ]]; then
  echo "check-skill-refs: FAILED — no $MANIFEST, so the publish set is unknown and no pointer in a" >&2
  echo "  published body can be resolved. This gate cannot be green without its subject." >&2
  exit 1
fi
command -v jq >/dev/null || { echo "check-skill-refs: FAILED — jq is required to read $MANIFEST." >&2; exit 1; }

# The skills THIS repo publishes, straight from the manifest — see the MANIFEST note above, and § 0
# for why this is PUBLICATION and not the registry's `owner:`.
published=$(jq -r '.skills[].id' "$MANIFEST" | sort -u)

# The bodies to scan: the SKILL.md each manifest row is SUPPLIED BY. `supplied-by` is a directory, so
# the body is the SKILL.md inside it. A row whose body is missing is a broken manifest, and it is
# reported rather than skipped — an unreadable subject is not an absent one.
body_paths=$(jq -r '.skills[]."supplied-by"' "$MANIFEST" | sed 's:/*$:/:' | sort -u \
  | while IFS= read -r d; do printf '%s\n' "${d}SKILL.md"; done)

missing_bodies=0
while IFS= read -r b; do
  [[ -z $b ]] && continue
  if [[ ! -f $b ]]; then
    echo "check-skill-refs: FAILED — $MANIFEST supplies a body at '$b', which does not exist." >&2
    missing_bodies=1
  fi
done <<<"$body_paths"
((missing_bodies)) && exit 1

# NUL-separated, so a path with a space cannot split. `-r` on every consuming xargs: with an empty
# list, xargs would otherwise run its command with NO file operands, and grep/awk would then read
# THIS SCRIPT'S stdin — reporting zero hits from whatever it found there. A silent no-op is the one
# outcome this gate may never produce.
body_files() { while IFS= read -r b; do [[ -n $b ]] && printf '%s\0' "$b"; done <<<"$body_paths"; }

# The directories the bodies live in — the pathspec `--changed` diffs against (§ 4). Derived from the
# manifest too, so a skill supplied from a new root is picked up here the moment it is published,
# rather than being quietly outside the merge gate's scope.
#
# An ARRAY, not a word-split string. A `supplied-by` with a space in it would split into two pathspecs
# that match nothing, and `git diff` would report no changed files — so the link half would announce
# "this diff touches no published skill body" on a PR that edited one. That is a green gate over a
# subject it never examined, arriving through the very fallback that exists to prevent it (§ 4).
mapfile -t body_dirs < <(jq -r '.skills[]."supplied-by"' "$MANIFEST" | sed 's:/*$::' | sort -u)

# -x -F, never -w: `grep -w game` matches inside `fs-gg-game` (a `-` is a word boundary), and an
# unanchored pattern is a REGEX, so `fs.gg.game` would match too. Both would wave a typo'd owner
# through — and a foreign qualified ref is trusted, so nothing downstream would catch it.
is_published() { grep -qxF -- "$1" <<<"$published"; }
is_known_owner() { grep -qxF -- "$1" <<<"$KNOWN_OWNERS"; }

fail=0
report() {
  printf '%s:%s: %s\n' "$1" "$2" "$3" >&2
  # A GitHub Actions annotation when running in CI; harmless locally.
  [[ -n ${GITHUB_ACTIONS:-} ]] && printf '::error file=%s,line=%s::%s\n' "$1" "$2" "$3"
  fail=1
}

# THE #655 STOPGAP IS GONE (#714), and this is its gravestone, because a reader who finds the mirrors
# fully checked should know they were once not.
#
# There WAS a `note()` here: a §1 finding in a mirrored body was reported and NOT failed on, because it
# was a RED WE COULD NOT CLEAR. The reasoning was sound and the premise was "ONE BYTE SEQUENCE CANNOT
# SATISFY BOTH REPOS" — `[[fs-gg-ballistics]]` is right in Game and dangles here, `[[fs-gg-rendering:
# fs-gg-scene]]` is right in Game and was refused HERE as self-qualified. No edit made both repos green,
# so a hard fail would have demanded a change no diff of ours could make.
#
# FS.GG.Game falsified the premise (Game#279): they REMOVED the self-qualified refusal — a ref that
# resolves is true, whether or not you name its owner — and qualified every ref in the four bodies, both
# directions. One byte sequence now satisfies both gates, so the red is clearable, so the stopgap is not
# merely unnecessary but harmful: while it stood, §1 findings in the four mirrored bodies were not
# CHECKED AT ALL, which is the thing this script exists to prevent. They are checked now.
#
# What the note bought is kept elsewhere and does not need a stream of its own: a §1 finding in a mirror
# is still not fixable HERE (the bytes are Game's), and §1 now says so in the finding itself — fix it in
# the canonical and re-sync. It is a real failure, because now it CAN be cleared.

# The mirrored bodies, by path. A mirrored id we do not publish contributes nothing and is SKIPPED,
# not failed — there is no body, so there is nothing to protect and nothing to demote.
#
# THE ESCAPE HATCH FAILS SAFE IN BOTH DIRECTIONS, which is why no guard is needed here and why an
# earlier draft's guard was removed (it fired on every case in the test suite, and it was checking the
# wrong thing):
#
#   * MIRRORED_SKILLS goes STALE (the P6 provider epic retires the mirror, and the bodies go away).
#     The entries then match no body, `mirror_bodies` is empty, and every § 1 finding hard-fails
#     again — which is correct, because with no mirror there is nothing we may not edit. Dead config,
#     no effect.
#   * A FIFTH skill is mirrored and nobody adds it here. Its § 1 findings hard-fail, unfixably — a
#     loud red, with § 0 at the top of this file explaining exactly what it means and what to do. It
#     degrades toward MORE checking, never less, which is the rule this script keeps everywhere else.
#
# The one direction that WOULD be dangerous — an entry here for a skill whose ownership came BACK to
# us, silently demoting its real findings to notes — cannot be detected locally at all: it turns on
# the registry's `owner:`, which lives in FS-GG/.github and which this gate deliberately takes no
# network to read. The org's own `fsgg-skill-registry-check` (`frozen-mirror` arm) is what adjudicates
# that, and it is the right place for it. Pretending to check it here with the manifest — which
# carries no `owner` field — would be a gate reporting green over a question it never asked.
mirror_bodies=""
while IFS= read -r mid; do
  [[ -z $mid ]] && continue
  is_published "$mid" || continue
  mirror_bodies+="$(jq -r --arg id "$mid" '.skills[] | select(.id==$id) | ."supplied-by"' "$MANIFEST" \
    | sed 's:/*$:/SKILL.md:')"$'\n'
done <<<"$MIRRORED_SKILLS"

is_mirror() { [[ -n $mirror_bodies ]] && grep -qxF -- "$1" <<<"$mirror_bodies"; }

# ────────────────────────────────────────────────────────────────────────────────────────────────
# 1. WIKI REFS
# ────────────────────────────────────────────────────────────────────────────────────────────────
# A § 1 verdict is RELATIVE TO A PUBLISH SET — that much was always true, and it is why the mirrors
# used to be NOTE'd rather than failed on (the #655 stopgap, now gone; see the gravestone above `report`).
# The premise was that ONE BYTE SEQUENCE COULD NOT SATISFY BOTH REPOS, and it was true only because a
# SELF-qualified ref was refused. Game removed that refusal (Game#279), qualified every ref in the four
# bodies, and the premise is false: a fully-qualified body is green in both.
#
# So § 1 FAILS on the mirrors now, like everything else. Three rules, and each says which repo owns the
# verdict:
#
#   SELF-qualified + published  -> OK.      We can see the tree; the ref resolves; it is TRUE.
#   FOREIGN-qualified           -> TRUSTED. We cannot see their tree. Their gate checks it, and does.
#   BARE, in a MIRRORED body    -> FAIL.    The one shape that cannot be right in both repos. Fix it in
#                                           the canonical and re-sync; do not edit the mirror.
#   BARE, in one of OUR bodies  -> checked against our publish set, exactly as before.
#
# Every ref is now validated EXACTLY ONCE, by the only repo that can see the tree it names. Nothing is
# weakened: a typo'd owner still fails, a self-qualified ref to a skill we do NOT publish still dangles,
# and a bare ref we do not publish still dangles.
#
# § 2 and § 3 always failed on a mirror and still do: a CLOSED issue is closed in every repo and a bare
# `#N` is unresolvable in every repo, so those verdicts never depended on whose publish set you ask.
# What changed is that § 1's verdict no longer does either.
while IFS=: read -r file line ref; do
  [[ -z ${ref:-} ]] && continue
  finding=""
  if [[ $ref == *:* ]]; then
    owner=${ref%%:*}
    id=${ref#*:}
    if ! is_known_owner "$owner"; then
      finding="dangling [[$ref]] — unknown owner '$owner' (known: $KNOWN_OWNERS)"
    elif [[ $owner == "$SELF_OWNER" ]] && ! is_published "$id"; then
      finding="dangling [[$ref]] — qualified to this repo, which does not publish '$id'"
    fi
    # A SELF-qualified ref that RESOLVES is accepted (Game#279 / #714). It used to be refused —
    # "write it bare as [[$id]]" — and that refusal is what made the convention unmirrorable, so it
    # is what Game removed. A ref that resolves is TRUE; naming the owner does not make it less so.
    #
    # A FOREIGN qualified ref is trusted: this repo cannot see the other repo's tree.
  elif is_mirror "$file"; then
    # ONE LINE, and it must be: `report` writes `path:line: message` to stderr, and that shape is the
    # only contract skill-refs-sweep.yml has with this script (it greps `^[^ :]+:[0-9]+: `). A finding
    # wrapped over several lines still matches on its first line, and every continuation line is then
    # silently dropped by the sweep — so the issue it files would carry half the reason.
    finding="bare [[$ref]] in a MIRRORED body — the same bytes are judged by BOTH repos, and a bare ref resolves in exactly ONE publish set ([[fs-gg-scene]] resolves here and dangles in Game; [[fs-gg-ballistics]] the reverse), so it is wrong in one of them whichever way you write it. QUALIFY it as [[<owner>:$ref]]. Fix it in the OWNING canonical (FS.GG.Game) and re-sync — do NOT edit the mirror here, which would break the byte-identity ADR-0022 §6 requires"
  elif ! is_published "$ref"; then
    finding="dangling [[$ref]] — this repo does not publish it; qualify it as [[<owner>:$ref]]"
  fi
  [[ -z $finding ]] && continue
  report "$file" "$line" "$finding"
done < <(body_files | xargs -0 -r grep -HEon '\[\[[A-Za-z0-9._:-]+\]\]' \
  | sed -E 's/\[\[(.*)\]\]$/\1/')

# ────────────────────────────────────────────────────────────────────────────────────────────────
# 2. ISSUE / PR LINKS
# ────────────────────────────────────────────────────────────────────────────────────────────────

# Every hit is normalised to a `file<TAB>line<TAB>owner<TAB>repo<TAB>num` row, in ONE pass.
#
# A `skill-refs:` marker is STRIPPED before extraction: it is this gate's config, not the body's
# prose. Left in, a marker would be scanned as a link and so EXCUSE ITSELF — a typo'd ref would
# validate against nothing but its own mention, and a marker outliving the sentence it was written
# for would still look live. Config must not be its own subject.
#
# The persistence pointer is BOTH forms at once — `[FS-GG/FS.GG.Rendering#587](https://…/587)` — so
# the two scans overlap by design and `sort -u` collapses them to one row (and so to one API call).
#
# The leading `[^A-Za-z0-9._/#-]` (or start-of-line) is what keeps a BARE `#1` out of THIS scan: in
# fs-gg-line-drawing's `the design doc's "#1 LOS bug"` the char before `#` is a quote and no repo
# token precedes it, so nothing matches. It is consumed by the match, so the ref is re-trimmed. A
# bare ref is no longer thereby IGNORED — § 3 scans for exactly what this pattern declines to claim.

# Strip skill-refs markers, terminating on `-->` rather than on the first `>`. A rationale is prose
# and may well contain one ("superseded -> #9"), and a marker left un-stripped is scanned as a ref
# and EXCUSES ITSELF — the exact defect the strip exists to prevent. Other HTML comments are kept: a
# ref inside one is still a ref.
#
# Shared by BOTH scans, and load-bearing for each. For § 2 an un-stripped `closed-ok FS.GG.X#9` is a
# link that vouches for itself; for § 3 an un-stripped `prose-ok #9` is *itself a bare `#9`* — it
# would excuse itself and every marker would be self-justifying. Config must not be its own subject.
AWK_STRIP='
  function strip_markers(s,   res, i, j, seg) {
    res = ""
    while ((i = index(s, "<!--")) > 0) {
      j = index(substr(s, i), "-->")
      if (j == 0) {                       # unterminated — drop it if it is ours, else stop
        if (substr(s, i) ~ /^<!--[[:space:]]*skill-refs:/) s = substr(s, 1, i - 1)
        break
      }
      seg = substr(s, i, j + 2)
      res = res substr(s, 1, i - 1)
      if (seg !~ /^<!--[[:space:]]*skill-refs:/) res = res seg
      s = substr(s, i + j + 2)
    }
    return res s
  }'

# WHOLE-TREE, and it stays that way even under `--changed`. § 1 and § 3 are f(tree): they need no
# network, they cost nothing, and they cannot decay — so there is no reason to scope them and one
# good reason not to. Scoping a hermetic check buys nothing and can only lose coverage.
md_files() { body_files; }

# The files § 2 resolves links in — the ONLY check that is f(world), and so the only one scoped.
# See § 4. Deletions are excluded (--diff-filter=ACMR): a body the diff REMOVED has no pointers
# left to keep, and scanning a path that is no longer on disk would just fail to open it.
link_scope="tree"          # tree | diff
link_scope_note=""
if [[ -n $CHANGED_BASE ]]; then
  # USABLE means two things, and checking only the first is a bug this script already made once.
  #
  #   1. it RESOLVES — `github.event.before` is all-zeros on a branch's first push, and a base that
  #      was never fetched is not here either;
  #   2. it SHARES HISTORY with HEAD — `git diff A...HEAD` is defined via the merge base, so a
  #      commit that EXISTS but has no common ancestor (a force push that rewrote the root; a ref
  #      from an unrelated repo) makes git exit 128 with `fatal: no merge base`.
  #
  # (2) is not hypothetical and existence does not imply it: `git rev-parse --verify` says yes to an
  # orphan commit, and the diff then dies — killing the gate job with a raw `fatal:` on a PR that did
  # nothing wrong. That is the FALSE RED of #238 walking back in through the fallback that exists to
  # prevent it. Both conditions, or sweep the tree.
  base_problem=""; base_hint=""
  if ! git rev-parse --verify --quiet "$CHANGED_BASE^{commit}" >/dev/null; then
    base_problem="does not resolve here"
    base_hint="It was never fetched, or it is the all-zeros of a first/force push."
  elif ! git merge-base "$CHANGED_BASE" HEAD >/dev/null 2>&1; then
    base_problem="shares no history with HEAD"
    base_hint="There is no merge base, so there is no diff to take against it."
  fi

  if [[ -z $base_problem ]]; then
    link_scope="diff"
    # NOT `${CHANGED_BASE:0:12}` — that is a SHA abbreviation applied to something that need not be a
    # SHA, and it turns `--changed origin/some-long-branch` into `origin/some` in the report: a ref
    # that does not exist, named as the thing we diffed against. Abbreviate only what git says is one.
    link_scope_note="changed since $(git rev-parse --short "$CHANGED_BASE" 2>/dev/null || printf '%s' "$CHANGED_BASE")"
  else
    # DEGRADE TOWARD MORE CHECKING (§ 4). An unusable base is not "nothing changed" — reading it that
    # way would switch the gate off precisely when history looks strange. Say so, and sweep everything.
    echo "check-skill-refs: NOTE — base '$CHANGED_BASE' $base_problem. $base_hint" >&2
    echo "  Falling back to the FULL link sweep rather than checking nothing." >&2
    link_scope_note="base '$CHANGED_BASE' $base_problem — swept the whole tree instead"
  fi
fi

# Under `--changed`, intersect the diff with the manifest's bodies. The `-f` test is not enough on its
# own: a diff may touch any `.md` under a body dir (a README beside a SKILL.md), and only the BODIES
# are this gate's subject — so filter to paths the manifest actually supplies, not merely to paths
# that changed under the right directory.
link_md_files() {
  if [[ $link_scope == diff ]]; then
    git diff --name-only -z --diff-filter=ACMR "$CHANGED_BASE...HEAD" -- "${body_dirs[@]}" \
      | while IFS= read -r -d '' f; do
          [[ -f $f ]] && grep -qxF -- "$f" <<<"$body_paths" && printf '%s\0' "$f"
        done
  else
    md_files
  fi
}

emit_links() {
  link_md_files | xargs -0 -r awk -v OFS='\t' -v def="$DEFAULT_OWNER" "$AWK_STRIP"'
      {
        line = strip_markers($0)

        s = line
        while (match(s, /https:\/\/github\.com\/[A-Za-z0-9._-]+\/[A-Za-z0-9._-]+\/(issues|pull)\/[0-9]+/)) {
          u = substr(s, RSTART, RLENGTH); s = substr(s, RSTART + RLENGTH)
          split(u, p, "/")            # p[4]=owner  p[5]=repo  p[7]=num
          print FILENAME, FNR, p[4], p[5], p[7]
        }

        s = line
        while (match(s, /(^|[^A-Za-z0-9._\/#-])([A-Za-z0-9._-]+\/)?[A-Za-z][A-Za-z0-9._-]*#[0-9]+/)) {
          t = substr(s, RSTART, RLENGTH); s = substr(s, RSTART + RLENGTH)
          sub(/^[^A-Za-z0-9]+/, "", t)
          h = index(t, "#"); num = substr(t, h + 1); nr = substr(t, 1, h - 1)
          sl = index(nr, "/")
          if (sl) print FILENAME, FNR, substr(nr, 1, sl - 1), substr(nr, sl + 1), num
          else    print FILENAME, FNR, def, nr, num
        }
      }'
}

# § 3's scan: every BARE `#N`, normalised to `file<TAB>line<TAB>num`. No API call — the FORM is the
# defect, so there is nothing to resolve and nothing the network could tell us.
#
# `#[0-9][A-Za-z0-9_-]*` over-matches on purpose, and only an all-digit run is kept. A CSS colour
# `#1a2b3c` opens exactly like an issue ref, and a bare `#[0-9]+` would match its leading `#1` and
# report a dangling ref that was never there — a false positive in a gate people would then turn
# off. Taking the WHOLE run and then requiring it to be all digits rejects `1a2b3c` and `abc123`.
# An all-NUMERIC colour (`#123456`) is genuinely indistinguishable from an issue ref — no pattern
# can separate them — so it is reported, and `prose-ok` is the answer. That is the honest limit:
# the gate declines to GUESS, here as everywhere, and asks the author who knows.
#
# The leading class carries `/` and `#`, which is what excludes the three near-misses: a URL fragment
# (`…/page#1`), a markdown heading (`## 3`), and the `#535` of a qualified `FS.GG.Rendering#535` —
# that last one is § 2's ref, and reporting it here would double-report every honest link.
emit_bare() {
  md_files | xargs -0 -r awk -v OFS='\t' "$AWK_STRIP"'
      {
        s = strip_markers($0)

        # An EXPLICIT markdown link is not a bare ref, and this is the difference between a gate and
        # a nuisance: `[#587](https://github.com/FS-GG/FS.GG.Rendering/issues/587)` is the single most
        # idiomatic way to cite an issue, and it is CORRECT — the `#587` is a link LABEL, so GitHub
        # renders it as display text and never autolinks it. It cannot resolve against the reader`s
        # tree, which is the whole hazard § 3 exists for. Reporting it would reject the right
        # answer, and this gate would be the one people turn off.
        #
        # Drop the whole `[label](absolute-url)` construct — the label of ANY http(s) link, not just a
        # `[#N]` one, so `[see #459](https://…)` is spared too. Nothing is lost: § 2 scans the
        # UNSTRIPPED line, so the URL is still resolved and a closed or missing target still fails.
        # `[#587]` with NO target keeps its report, and must: GitHub autolinks that one.
        gsub(/\[[^]]*\]\(https?:\/\/[^)]+\)/, " ", s)

        while (match(s, /(^|[^A-Za-z0-9._\/#-])#[0-9][A-Za-z0-9_-]*/)) {
          t = substr(s, RSTART, RLENGTH); s = substr(s, RSTART + RLENGTH)
          sub(/^[^#]*#/, "", t)          # drop the consumed boundary char and the `#`
          if (t ~ /^[0-9]+$/) print FILENAME, FNR, t
        }
      }'
}

# `sort -u` must dedupe on the WHOLE row — keying it would collapse two distinct refs sharing a line.
# Order for display in a second, non-unique pass.
links=$(emit_links | sort -u | sort -t$'\t' -k1,1 -k2,2n)
bares=$(emit_bare | sort -u | sort -t$'\t' -k1,1 -k2,2n)

# How many bodies the link half actually LOOKED at — reported, so a scoped run states its subject
# rather than leaving the reader to infer it from a count of zero.
n_link_files=$(link_md_files | tr '\0' '\n' | grep -c . || true)

# The closed-ok allowlist, normalised to `file<TAB>line<TAB>owner/repo#num` — one row per marker.
#
# SCOPED WITH § 2, because it IS § 2: auditing a marker means resolving its ref, so it is f(world)
# and decays the same way. A `closed-ok` whose issue REOPENS goes red with no commit — the identical
# time-bomb, one level down — and left tree-wide it would redden the innocent PRs the scope exists
# to protect, through the very mechanism we just closed. The sweep audits every marker.
#
# -H, and it is load-bearing: `grep -r <dir>` always prefixes the filename, but grep given a SINGLE
# file operand does not, and under `--changed` a one-file diff is the common case. Without it the
# row would parse as `<line>:<match>` and every marker in that file would silently stop excusing
# anything — turning a correct, marked citation red. `|| true` covers both grep's no-match 1 and
# xargs' 123, which `set -e` would otherwise take as fatal.
markers=$( { link_md_files | xargs -0 -r grep -HEon \
    '<!--[[:space:]]*skill-refs:[[:space:]]*closed-ok[[:space:]]+[A-Za-z0-9._/-]+#[0-9]+' \
    || true; } | while IFS= read -r m; do
    [[ -z $m ]] && continue
    mfile=${m%%:*}; mrest=${m#*:}; mline=${mrest%%:*}; mref=${mrest##* }
    [[ $mref == */* ]] || mref="$DEFAULT_OWNER/$mref"
    printf '%s\t%s\t%s\n' "$mfile" "$mline" "$mref"
  done)

# The prose-ok allowlist, normalised to `file<TAB>line<TAB>num` — one row per marker.
# WHOLE-TREE, pairing with § 3: no network, no decay, so nothing to scope away from.
prose_markers=$( { md_files | xargs -0 -r grep -HEon \
    '<!--[[:space:]]*skill-refs:[[:space:]]*prose-ok[[:space:]]+#[0-9]+' \
    || true; } | while IFS= read -r m; do
    [[ -z $m ]] && continue
    mfile=${m%%:*}; mrest=${m#*:}; mline=${mrest%%:*}; mnum=${m##*#}
    printf '%s\t%s\t%s\n' "$mfile" "$mline" "$mnum"
  done)

is_prose() { # file num
  [[ -n $prose_markers ]] &&
    awk -F'\t' -v f="$1" -v n="$2" '$1==f && $3==n {found=1} END{exit !found}' <<<"$prose_markers"
}

is_excused() { # file owner/repo#num
  [[ -n $markers ]] && awk -F'\t' -v f="$1" -v r="$2" '$1==f && $3==r {found=1} END{exit !found}' <<<"$markers"
}

# ── link resolution ─────────────────────────────────────────────────────────────────────────────
# THREE states, and conflating the first two is a bug this gate has already made once: "there is
# nothing to check" is not "I could not check". A file holding only a stale MARKER has no links —
# and is exactly the dead config the marker audit exists to catch, so it must still reach the API.
link_mode=checked          # checked | empty | skipped
skip_reason=""
if [[ -z $links && -z $markers ]]; then
  link_mode=empty
elif [[ -n ${SKILL_REFS_SKIP_LINKS:-} && -z ${GITHUB_ACTIONS:-} ]]; then
  link_mode=skipped
  skip_reason="SKILL_REFS_SKIP_LINKS is set"
elif ! command -v gh >/dev/null 2>&1 || ! gh auth status >/dev/null 2>&1; then
  if [[ -n ${GITHUB_ACTIONS:-} ]]; then
    # NEVER let the gate self-skip: a link it cannot read is one it cannot call green.
    echo "check-skill-refs: FAILED — no authenticated \`gh\` in CI, so issue/PR links cannot be" >&2
    echo "  resolved. Give the step a token (env: GH_TOKEN: \${{ secrets.GITHUB_TOKEN }}); do not" >&2
    echo "  skip the check — a green gate over an unread link is the defect this gate exists for." >&2
    exit 1
  fi
  link_mode=skipped
  skip_reason="no authenticated \`gh\` (run \`gh auth login\`)"
fi

# The cache is a FILE, not a variable, because every call site is a `$(resolve_state …)` command
# substitution — a subshell, whose variable writes are discarded the moment it exits. A shell-var
# cache here would silently never hit, and each ref would be re-fetched: once as a link, again in
# the marker audit. A file write outlives the subshell.
state_cache=$(mktemp)
trap 'rm -f "$state_cache"' EXIT

resolve_state() { # owner repo num -> open | closed | missing | unresolved
  local owner=$1 repo=$2 num=$3 key="$1/$2#$3" hit out err attempt
  hit=$(awk -F'\t' -v k="$key" '$1==k {print $2; exit}' "$state_cache")
  if [[ -n $hit ]]; then printf '%s' "$hit"; return 0; fi

  err=$(mktemp)
  out="unresolved"
  for attempt in 1 2 3; do
    if out=$(gh api "repos/$owner/$repo/issues/$num" --jq .state 2>"$err"); then
      break
    fi
    if grep -q 'HTTP 404' "$err"; then out="missing"; break; fi
    out="unresolved"
    # A transient 5xx / secondary limit, not a verdict — but do not sleep after the LAST attempt.
    if ((attempt < 3)); then sleep $((attempt * 2)); fi
  done
  rm -f "$err"

  printf '%s\t%s\n' "$key" "$out" >>"$state_cache"
  printf '%s' "$out"
}

checked=0
if [[ $link_mode == checked && -n $links ]]; then
  while IFS=$'\t' read -r file line owner repo num; do
    [[ -z ${num:-} ]] && continue
    ref="$owner/$repo#$num"
    checked=$((checked + 1))
    case "$(resolve_state "$owner" "$repo" "$num")" in
      open) ;;
      closed)
        if ! is_excused "$file" "$ref"; then
          # Name the owner unless it is the default one — a marker is matched on the CANONICAL
          # owner/repo#num, so suggesting a bare `Repo#n` for a foreign-owned link would hand the
          # author a marker that normalises to the wrong org and never silences anything.
          sugg="$repo#$num"
          [[ $owner == "$DEFAULT_OWNER" ]] || sugg="$owner/$repo#$num"
          report "$file" "$line" "stale link — $ref is CLOSED. Repoint it at the live issue, or, if it is cited as history, excuse it: <!-- skill-refs: closed-ok $sugg — why -->"
        fi
        ;;
      missing)
        report "$file" "$line" "dangling link — $ref does not exist"
        ;;
      *)
        # Could not read it. In CI that is a failure, never a pass.
        report "$file" "$line" "unresolvable link — could not read $ref from the API after 3 tries"
        ;;
    esac
  done <<<"$links"
fi

# A marker that excuses nothing is dead config — and dead config is where the next rot hides. Three
# ways it goes dead: the sentence it guarded was rewritten and the link is gone; the issue reopened;
# the issue never existed (a typo). Markers are excluded from `links` above, so a marker can no
# longer vouch for itself and these checks mean something.
if [[ $link_mode == checked && -n $markers ]]; then
  while IFS=$'\t' read -r mfile mline mref; do
    [[ -z ${mref:-} ]] && continue
    if ! awk -F'\t' -v f="$mfile" -v r="$mref" \
         '$1==f && ($3"/"$4"#"$5)==r {found=1} END{exit !found}' <<<"$links"; then
      report "$mfile" "$mline" "stale closed-ok marker — nothing in this file links to $mref; drop it"
      continue
    fi
    mowner=${mref%%/*}; mrest=${mref#*/}; mrepo=${mrest%#*}; mnum=${mref##*#}
    case "$(resolve_state "$mowner" "$mrepo" "$mnum")" in
      closed) ;;   # doing its job
      open)
        report "$mfile" "$mline" "stale closed-ok marker — $mref is OPEN again; drop the marker"
        ;;
      missing)
        report "$mfile" "$mline" "stale closed-ok marker — $mref does not exist"
        ;;
    esac
  done <<<"$markers"
fi

# ────────────────────────────────────────────────────────────────────────────────────────────────
# 3. BARE `#N` REFS
# ────────────────────────────────────────────────────────────────────────────────────────────────
# UNGATED BY `link_mode`, and that is the point. § 2 needs the API to learn whether an issue is open;
# § 3 needs nothing — a bare ref is wrong by its FORM, in every repo, whatever the issue's state. So
# it must still run with SKILL_REFS_SKIP_LINKS set, and on a laptop with no `gh`. Hanging it off
# `link_mode` would make an offline check silently skippable — the very shape § 3 exists to close.
n_bare=0
if [[ -n $bares ]]; then
  while IFS=$'\t' read -r file line num; do
    [[ -z ${num:-} ]] && continue
    n_bare=$((n_bare + 1))
    is_prose "$file" "$num" && continue
    report "$file" "$line" "bare ref — #$num is read against the repo this body is MATERIALIZED into, so it points at the reader's own tracker, never ours. Qualify it (e.g. $SELF_REPO#$num), or, if it is prose and not a pointer, say so: <!-- skill-refs: prose-ok #$num — why -->"
  done <<<"$bares"
fi

# A prose-ok marker that excuses nothing is dead config, exactly as a stale closed-ok is: the
# sentence it guarded was rewritten, or its number changed and the marker kept the old one. Markers
# are stripped before extraction, so one cannot vouch for itself and this check means something.
if [[ -n $prose_markers ]]; then
  while IFS=$'\t' read -r mfile mline mnum; do
    [[ -z ${mnum:-} ]] && continue
    if ! awk -F'\t' -v f="$mfile" -v n="$mnum" \
         '$1==f && $3==n {found=1} END{exit !found}' <<<"$bares"; then
      report "$mfile" "$mline" "stale prose-ok marker — nothing in this file writes a bare #$mnum; drop it"
    fi
  done <<<"$prose_markers"
fi

if ((fail)); then
  echo >&2
  echo "check-skill-refs: FAILED — every pointer in a published skill must resolve: a [[ref]] to a" >&2
  echo "  skill, and an issue/PR link to a LIVE issue (or a marked, deliberate citation of history)." >&2
  echo "  A bare #N is not a pointer at all — qualify it, or mark it as prose." >&2
  echo >&2
  echo "  If the body is one of the four FROZEN MIRRORS (game-core, audio, persistence, model-swap)," >&2
  echo "  do NOT fix it here — our bytes must stay identical to FS.GG.Game's. Fix it there and" >&2
  echo "  re-sync. See § 0 at the top of this script." >&2
  exit 1
fi

n_skills=$(grep -c . <<<"$published")
# No "we OWN" hedge any more (#714). Every [[ref]] in every published body — the four mirrors included —
# is now judged, and every one of them resolves. That sentence could not be said while the stopgap stood.
echo "check-skill-refs: ok — $n_skills skills published; every [[ref]] resolves."

# The link half reports its SUBJECT, not just its verdict. "I found no stale links" and "I did not
# look at any" are different sentences, and a gate that prints one when it means the other is the
# `.github#416` shape this script exists to close. Under `--changed` the second is the NORMAL case —
# most diffs touch no skill — so it is the one that must never read as a clean bill of health.
case $link_mode in
  checked)
    if [[ $link_scope == diff ]]; then
      echo "check-skill-refs: ok — all $checked issue/PR link(s) in the $n_link_files skill body/bodies this diff touches are open or marked."
    else
      echo "check-skill-refs: ok — all $checked issue/PR link(s) in the tree are open or marked."
    fi
    ;;
  empty)
    if [[ $link_scope == diff ]]; then
      echo "check-skill-refs: link check N/A — this diff touches no published skill body, so it owns no"
      echo "  pointers and cannot be judged on them. Every link in the tree is swept on a schedule"
      echo "  (.github/workflows/skill-refs-sweep.yml) — that is where a link the WORLD broke surfaces."
    else
      echo "check-skill-refs: ok — no issue/PR links in the tree to check."
    fi
    ;;
  skipped)
    echo "check-skill-refs: NOTE — issue/PR links were NOT checked ($skip_reason)." >&2
    ;;
esac
if [[ -n $link_scope_note ]]; then
  echo "check-skill-refs: link scope — $link_scope_note."
fi

# Said out loud even at zero. § 3 is the half that still runs when the link half is skipped, so a
# silent pass here is indistinguishable from a check that did not happen — and "I found nothing" and
# "I did not look" being the same output is the defect this gate keeps being extended to kill.
if ((n_bare > 0)); then
  echo "check-skill-refs: ok — $n_bare bare #N ref(s); every one is marked prose-ok."
else
  echo "check-skill-refs: ok — no bare #N refs."
fi
