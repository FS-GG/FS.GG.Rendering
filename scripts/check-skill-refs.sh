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
# ── 0. WHAT THIS REPO PUBLISHES, AND THE BODIES IT DOES NOT OWN ─────────────────────────────────
# READ THIS BEFORE "FIXING" ANY REF THIS GATE REPORTS. `template/product-skills/` ships 14 skills,
# and — as of ADR-0063 (2026-07-21 amendment) — ALL 14 are Rendering's:
#
#   14 are OURS          (registry `owner: fs-gg-rendering`) — scene, symbology, elmish, skiaviewer,
#                        layout, keyboard-input, styling, ui-widgets, testing, and the game-sim
#                        skills collision / grids / line-drawing / visibility, which ADR-0022 P4
#                        explicitly did NOT migrate.
#    0 are FROZEN MIRRORS. There USED to be four (game-core, audio, persistence, model-swap, owned by
#                        fs-gg-game) — ADR-0022 §6's two-copies bridge. ADR-0063 RETIRED them
#                        (FS.GG.Rendering#965): FS.GG.Game flipped them to `mirrored: false` and
#                        FS.GG.Game.Skills now delivers them owner-sourced, so this repo ships no frozen
#                        copy. `MIRRORED_SKILLS` (below) is therefore EMPTY — the #696 end state — and the
#                        mirror machinery here is dormant, kept intact for any FUTURE mirror.
#
# SO (historical, while a mirror exists): A REF THIS GATE REPORTS INSIDE A FROZEN MIRROR IS NOT YOURS TO
# FIX HERE. There are none today, but the rule and mechanism below stand for the next one. Editing it would
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
# ── 0b. THE TWO SURFACES: WHAT WE SHIP, AND WHAT WE READ HERE (#698) ────────────────────────────
# This gate used to take its subject from the manifest ALONE — the 21 bodies that MATERIALIZE into a
# product. That is the right subject for a promise made to a STRANGER, and § 0 is the argument for it.
# It is not the only place this repo makes promises, and for one generation it was the only place we
# looked:
#
#   src/*/skill/SKILL.md            10 bodies, 37 refs   the LIBRARY-facing skills — the canonical
#                                                        instructions `.claude/skills/<id>/` points at
#   template/product-skills/README.md  1 body,  2 refs   the authoring note ABOUT this convention
#
# ROT WAS ALREADY THERE. `src/Testing/skill/SKILL.md` wrote `[[fsharp-build-orchestration]]` — a skill
# in NO registry row, NO manifest, and NO directory anywhere in the org. It was caught only because it
# happened to have a TWIN in a published body, which the manifest-scoped gate did see (#697 removed
# that one). The library copy survived, and nothing here would ever have reported it. That is luck, and
# luck is not a gate — which is the whole of #698.
#
# WHY THIS IS NOT "RUN THE GATE OVER MORE DIRECTORIES". A `[[ref]]`'s verdict is RELATIVE TO WHAT
# RESOLVES WHERE THE READER IS STANDING. That is § 0's own argument, and it is what forces TWO
# VOCABULARIES rather than one wider glob:
#
#   a reader of a PUBLISHED body stands in a SCAFFOLDED PRODUCT — what resolves there is what we
#     MATERIALIZE, so the vocabulary is the MANIFEST;
#   a reader of a LIBRARY body stands in THIS REPO, driving an agent — what resolves there is what the
#     agent can INVOKE, so the vocabulary is `.claude/skills/`.
#
# AND THE TWO SETS GENUINELY DISAGREE. This is not a technicality to be globbed away — the SAME STRING
# NAMES A DIFFERENT BODY on each surface:
#
#   [[fs-gg-scene]] in a PUBLISHED body -> template/product-skills/fs-gg-scene/  (the PRODUCT skill)
#   [[fs-gg-scene]] in a LIBRARY   body -> src/Scene/skill/                      (the LIBRARY skill)
#
# because `.claude/skills/` must disambiguate the two and calls the product variant
# `fs-gg-product-scene`, while the manifest ships into a product where no library skill exists and so
# needs no such prefix. Judge one surface with the other's vocabulary and you are not merely stricter
# or looser — you are answering about the wrong body.
#
# The sets differ in MEMBERSHIP too, and in the direction that would produce false reds: `.claude/skills/`
# carries skills the manifest never ships (`fs-gg-ant-design`, `speckit-*`, `cross-repo-coordination`),
# and the library bodies point at them CORRECTLY. Against the manifest, five sound refs to
# `fs-gg-ant-design` would be reported dangling — a gate firing on correct work, which is the one thing
# it may never do.
#
# THERE IS NO `--surface` FLAG, AND THAT IS THE DESIGN. The obvious shape is a flag, and a flag is an
# OPT-IN: the subject becomes whatever the CALL SITE remembers to ask for. That is this gate's own bug,
# one level up. It checked the manifest because the manifest is what it was POINTED AT, and 39 refs sat
# unexamined behind a green check for as long as nobody thought to point it anywhere else. A subject a
# caller can NARROW is a subject a caller can FORGET. So both surfaces are checked on every invocation;
# gate.yml and skill-refs-sweep.yml are unchanged and did not have to be taught anything; and there is
# no way to invoke this script that checks less than everything it knows about.
#
# WHAT DOES NOT CHANGE ACROSS SURFACES. § 2 is repo-independent — a CLOSED issue is closed wherever you
# read it — and so are the owner vocabulary, both escape hatches, the marker audit, and the scope/sweep
# split (§ 4). Only § 1's VOCABULARY and § 3's VERDICT turn on the surface. § 3's inversion is the one
# judgement here worth reading in full; it is argued where it happens.
#
# ── 1. WIKI REFS (Game#35) ──────────────────────────────────────────────────────────────────────
# A skill body that writes `[[fs-gg-ballistics]]` promises the reader a skill that RESOLVES WHERE THEY
# ARE STANDING — in a product, one this repo publishes; in this tree, one an agent here can invoke
# (§ 0b). It does not: `fs-gg-ballistics` is authored in FS.GG.Game and has NO body here, so it
# materializes into no product of ours and is in no `.claude/skills/`. An agent following the pointer
# finds nothing, on either surface.
#
# THE CONVENTION. A `[[ref]]` is either
#   * BARE      — `[[fs-gg-scene]]`                → MUST resolve in the reader's set: the MANIFEST in
#                 a published body, `.claude/skills/` in a repo-internal one (§ 0b), or
#   * QUALIFIED — `[[fs-gg-game:fs-gg-ballistics]]` → `<owner>:<skill-id>`, naming the publishing
#                 repo's registry `owner` id (registry/skills.yml in FS-GG/.github).
# Anything else is a dangling ref and fails. Bare code spans (`fs-gg-scene`) are prose, not
# pointers, and are deliberately NOT checked — only `[[...]]` promises resolvability.
#
# AND A BODY MAY DISCUSS THE SYNTAX WITHOUT INVOKING IT. `template/product-skills/README.md` is the
# doc that TEACHES this convention, so it writes `[[link]]` as an ILLUSTRATION — a shape, not a
# pointer. The old script's answer was to declare the README out of subject and check nothing in it,
# which is how its two real refs went unchecked; "a gate that fires on the document explaining it is a
# gate people learn to ignore" was right about the hazard and wrong about the remedy. The remedy is the
# one this script already uses twice: reject by default, and let the author declare the exception.
#
#     <!-- skill-refs: prose-ok [[link]] — the SHAPE of a ref, not a ref -->
#
# Same marker as § 3's, same sentence — "this pointer-shaped token is prose" — extended from a bare
# `#N` to a `[[ref]]`, because it is the identical claim about the identical kind of token. It is
# audited the same way too: a `prose-ok` that excuses nothing is reported, because dead config is where
# the next rot hides.
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
# ── § 3 INVERTS ON THE REPO SURFACE (#698) — THE PREMISE MOVES, NOT THE POLICY ──────────────────
# Every word above rests on ONE fact: the body is MATERIALIZED into a stranger's repo, so GitHub
# renders `#999` against THEIR tracker and the ref can never point here. THAT FACT IS FALSE OF A
# REPO-INTERNAL BODY. `src/Scene/skill/SKILL.md` ships nowhere. It is read HERE, in this repo, through
# the `.claude/skills/` wrapper that points an agent at it — and GitHub renders a `#999` in it against
# FS.GG.Rendering, which IS our tracker. THE POINTER IS CORRECT.
#
# So rejecting it here would demand a fix for a defect that does not exist, on the one surface where
# the idiomatic `#999` is the RIGHT thing to write. That is how a gate becomes the one people turn off,
# and § 3 has already made this argument once — about markdown link labels, two paragraphs up. The
# reasoning does not change just because the token does.
#
# BUT SILENCE IS NOT THE ALTERNATIVE, AND IN THIS SCRIPT IT NEVER IS. The ref still promises "there is
# a LIVE issue at the other end" — and that is § 2's question, which § 2 can answer. A bare `#999` in a
# repo-internal body IS a link to `FS.GG.Rendering#999`, because that is exactly what GitHub makes of
# it. So it is not REJECTED here; it is RESOLVED — against this repo, and it must be OPEN or carry a
# `closed-ok` marker like any other link. `prose-ok #N` still works, and now means "do not resolve
# this", which is the same sentence it always meant.
#
# The surface therefore goes from ZERO checking to § 2's full strictness, and the verdict each surface
# gets is the one its reader can actually act on. Degrade toward MORE checking, never less.
#
# THE LIMIT, SAID OUT LOUD RATHER THAN DISCOVERED LATER. A bare `#419` that MEANT `FS-GG/.github#419`
# is indistinguishable from one that meant ours, and it will be resolved against OURS — quietly, and
# possibly against a live issue of ours that has nothing to do with it. The gate cannot separate them
# and does not guess. It is not thereby WRONG: GitHub renders that ref against FS.GG.Rendering too, so
# the DOCUMENT already points where the gate says it points, and the defect is the author's to fix by
# QUALIFYING it. This is the same honest limit § 3 already owns for an all-numeric colour — the gate
# reports what the reader will actually get, and asks the author who knows what they meant.
#
# (This is why `.claude/skills/` is NOT in the subject: its ~35 bodies are largely MIRRORS of
# FS-GG/.github's canonical protocol docs, whose 85 bare `#N` are `.github`'s numbers, not ours.
# Resolving those against FS.GG.Rendering would be the limit above, 85 times over, on bodies we do not
# author — the § 0 frozen-mirror argument in a second costume. Filed rather than papered over: #723.)
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
      echo
      # NO COUNTS HERE, and that is not laziness (#733). The number of published bodies belongs to
      # the MANIFEST, which can change without touching this script — and `--help` runs BEFORE the
      # manifest is read, so it cannot know it. It used to say "the 21 bodies" and would have gone
      # on saying 21 forever. The summary already prints the real count ($n_skills), from the real
      # source, after the real load. A gate that hardcodes a fact it is capable of reading is a small
      # self-inflicted instance of the very drift it exists to prevent.
      #
      # The subject is described, not counted — and it is described in FULL: since #723 the repo
      # surface includes the WRAPPERS in both skill roots, not just the library bodies and the
      # authoring note. Naming a narrower subject than the gate actually has is the #698 shape, and
      # help text is where a reader goes to learn what was checked.
      echo "TWO SUBJECTS, always both — there is no flag to narrow this (see § 0b):"
      echo "  PUBLISHED  the bodies the skill manifest ships into a product."
      echo "             A [[ref]] resolves against the MANIFEST; a bare #N is REJECTED (it would"
      echo "             point at the reader's own tracker once materialized)."
      echo "  REPO       the bodies that ship NOWHERE — src/*/skill/SKILL.md, the authoring note"
      echo "             template/product-skills/README.md, and the non-kit wrappers under"
      echo "             .claude/skills/ and .agents/skills/ (§ 0c). Their reader is standing HERE,"
      echo "             so a [[ref]] resolves against .claude/skills/, and a bare #N is RESOLVED"
      echo "             against this repo rather than rejected."
      exit 0 ;;
    *) echo "check-skill-refs: unknown argument '$1'" >&2; exit 2 ;;
  esac
done

# The MANIFEST is the publish set — not a directory listing. This is the one structural change from
# FS.GG.Game's script, and it is not a refinement: a direct port is WRONG here, and silently.
#
# Game's `is_published` globs `template/product-skills/*/`, which in Game is exactly what it ships.
# In Rendering it is not. We materialize 21 skills; only 18 live under that root. The other three are
# supplied from off-convention paths — fs-gg-project from `template/base/.agents/skills/`,
# fs-gg-samples from `template/fragments/`, and fs-gg-feedback-report from `template/feedback-report/`.
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

# The PUBLISHED SUBJECT: those 21 bodies, which MATERIALIZE into somebody's product and so make
# promises to a reader who is not us. Their vocabulary is the manifest (§ 0b).
#
# It used to be the ONLY subject, and the comment here used to explain why `template/product-skills/
# README.md` was excluded from it: the README ships nowhere, and it discusses `[[…]]` refs in the
# abstract, so scanning it as a published body reports its ILLUSTRATIONS as dangling refs — "a gate
# that fires on the document explaining it is a gate people learn to ignore". That hazard was real and
# the diagnosis was right. The remedy was not: it declared the README (and the ten library bodies with
# it) OUT OF SUBJECT, and out of subject means UNCHECKED — which is how 39 refs, one of them already
# dead, came to sit behind a green gate (#698). The README is in the subject now, on the REPO surface,
# with `.claude/skills/` for a vocabulary and a `prose-ok [[…]]` marker for its illustrations. Reject
# by default, let the author declare the exception — the answer this script already gives twice.
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
# whose `owner:` is not ours AND which we also publish — i.e. ADR-0022 P4's migration set.
#
# IT IS VERIFIED AGAINST THE ORG REGISTRY (#722), and it did not use to be. This comment used to say
# "NOTHING VERIFIES IT", and justified that by arguing that BOTH ways it can rot fail SAFE. That was
# true while being listed DEMOTED a § 1 finding to a note: omitting a skill meant it got MORE checking.
# #714 inverted the polarity — being listed now makes the gate STRICTER — so the omission is the
# direction that fails OPEN, and the tolerance was left standing on a premise that had died one PR
# earlier. See `verify_mirrors` below, next to the subject it validates.
#
# It stays a CONSTANT, and that is not a hedge: § 1's verdicts are still f(tree) (§ 4), because the
# SUBJECT is built from this list, hermetically. The registry read only asserts that the list still
# tells the truth — the same split `KIT_SKILLS` runs under (§ 0c).
# EMPTY as of ADR-0063 (2026-07-21 amendment): the four game-owned mirrors (fs-gg-game-core, fs-gg-audio,
# fs-gg-persistence, fs-gg-model-swap) were RETIRED from this provider (FS.GG.Rendering#965) — FS.GG.Game
# flipped them to `mirrored: false` and FS.GG.Game.Skills now delivers them owner-sourced. This repo ships
# NO frozen mirror. The empty set is the #696 end state this machinery always aimed at (see the "MIRROR set
# may legitimately be empty" note in `verify_mirrors`), and it is VERIFIED against the registry: `canonical`
# derives to empty (no `owner: fs-gg-game` product row is published here) and must equal this constant.
MIRRORED_SKILLS=''

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

# ── 0c. THE THIRD SURFACE, AND THE FOUR BODIES IN IT WE DO NOT OWN (#723) ───────────────────────
# #698 brought two surfaces under this gate and named a THIRD it did not take: the 53 skill bodies under
# `.claude/skills/` and `.agents/skills/`. It left them out DELIBERATELY, on a real argument — they carry
# 88 bare `#N`, and § 3's inversion would resolve every one of them against FS.GG.Rendering, where they
# would dangle or, worse, quietly hit an unrelated open issue of ours and pass green.
#
# THE ARGUMENT WAS RIGHT AND THE CONCLUSION WAS TOO BROAD, and the provenance is what settles it. All 88
# of those bare refs sit in EXACTLY FOUR BODIES — `pnext-item` (38), `intra-repo-parallel-work` (29),
# `cross-repo-coordination` (12), `check-board` (9) — and those four are THE COORDINATION KIT:
#
#   * declared as `kind: skill` kit rows in FS-GG/.github `registry/repos.yml`;
#   * content-addressed there — each body's sha256 is the kit digest, pinned in `repos.lock`;
#   * written into BOTH roots here by `scripts/coordination-sync`, run by a bot, not by us;
#   * and byte-identity to canonical ENFORCED ON EVERY PR by `coordination-coherence.yml`, a required
#     check in this very run.
#
# So their `#419`, `#551`, `#322` are `.github`'s numbers — and, the part that settles it, WE COULD NOT
# QUALIFY THEM IF WE WANTED TO. Editing `#419` to `FS-GG/.github#419` in our copy changes the bytes, and
# `coordination-coherence` goes red. A ref gate over these bodies would not merely manufacture a red no
# diff of ours can clear; it would demand a diff that a DIFFERENT required gate rejects. Two gates in
# direct opposition, and no tree satisfies both. That is § 0's frozen-mirror argument in a second
# costume, and the fix is upstream: `.github` qualifies its own bare `#N` (the two-publish-sets
# arithmetic of #714/Game#279), and the sync brings the qualified bytes down.
#
# THE OTHER 49 ARE OURS, AND THEY CARRY NOTHING: zero `[[wiki refs]]`, zero links, zero bare `#N`. So
# this widening finds no defect today — and that is exactly why it is worth doing NOW rather than after
# one appears. An empty surface with no gate is not a clean surface, it is an UNWATCHED one, and § 0b's
# own lesson is that "no refs today" is luck: `src/Testing/skill/SKILL.md`'s dead `[[…]]` sat behind a
# green gate for a generation and was caught by a coincidence. `speckit-*` and the `fs-gg-*` wrappers are
# ours to edit, so a ref CAN appear in them tomorrow, and nothing here would have said a word.
#
# BOTH ROOTS, NOT ONE. `repo_vocab` reads `.claude/skills/` alone and is right to — a VOCABULARY needs
# only the directory NAMES, and both roots carry the same ones. A SUBJECT is a different question, and
# the bodies genuinely differ: 33 of the 53 pairs are NOT byte-identical (the wrapper line reads
# "Claude-active" in one root, "Codex-active" in the other). Codex reads `.agents/`, Claude reads
# `.claude/`, both are read by a real agent, and a ref added to one is invisible in the other. Scanning
# one root and calling the surface checked would rebuild, one directory over, the exact hole § 0b closed.
#
# ── THE REPO SURFACE (#698, WIDENED BY #723) ────────────────────────────────────────────────────
# The bodies that ship NOWHERE, and whose reader is therefore standing HERE (§ 0b): the ten canonical
# library skills that `.claude/skills/<id>/SKILL.md` wraps and points an agent at, the one authoring note
# about this very convention — and, since #723, THE WRAPPERS THEMSELVES, in both skill roots.
AGENT_SKILLS=".agents/skills"
# The COORDINATION KIT (§ 0c): the four bodies in the skill roots that FS-GG/.github authors and a bot
# syncs. OUT OF THE SUBJECT — their refs are `.github`'s to qualify — but firmly IN the vocabulary
# below, because an agent standing in this tree really can invoke `[[pnext-item]]`, and a body of ours
# that points at one is CORRECT.
#
# THIS LIST IS VERIFIED, and it has to be, because it does not fail safe. A name wrongly IN it is a body
# of OURS excluded from its own gate: its refs go unexamined and the gate still says `ok`. That is
# fail-OPEN, it is the `.github#416` shape, and it is the precise hazard § 0b refused a `--surface` flag
# over ("a subject a caller can NARROW is a subject a caller can FORGET"). The kit rows are fetched from
# canonical and compared against it below — the subject stays hermetic (it is built from THIS constant,
# so § 4's f(tree) split still holds for § 1 and § 3), and the fetch is a § 2-style f(world) check that
# the constant still tells the truth.
#
# `MIRRORED_SKILLS` IS VERIFIED THE SAME WAY, AND FOR THE SAME REASON (#722). This paragraph used to
# CONTRAST the two — "unlike MIRRORED_SKILLS, this one is verified" — on the grounds that the mirror
# list failed SAFE in both directions and so could go unchecked. It did, until #714 inverted its
# polarity one PR earlier; the contrast was stale on the day it was written, and it argued for the
# tolerance that #722 was filed about. One rule, one reading: a constant this gate narrows its own
# behaviour by is a constant it checks.
KIT_SKILLS=$'cross-repo-coordination\nintra-repo-parallel-work\ncheck-board\npnext-item'

# The RESOLVABLE SET for a body read HERE: the skills an agent in this tree can actually INVOKE. Not
# the manifest — § 0b is the argument, and the two sets name different bodies under the same string.
#
# `.claude/skills/` and `.agents/skills/` carry the same directory NAMES — `skill-parity`, a check in
# this same required job, is what holds that true — so either root gives the same vocabulary; this reads
# the Claude one because CLAUDE.md is what drives an agent standing in this tree. (Their CONTENTS are
# not identical, which is why the SUBJECT must read both. A vocabulary needs only the names.)
CLAUDE_SKILLS=".claude/skills"
# `if`, not `&&` — same pipefail trap as `repo_body_paths` below. With no `.claude/skills/` at all the
# glob stays literal, `[[ -d ]]` is false, the `for` returns 1, pipefail reddens the pipeline and the
# script dies silently — swallowing the very "no vocabulary" refusal written three lines down to
# announce it.
repo_vocab=$(
  for d in "$CLAUDE_SKILLS"/*/; do
    if [[ -d $d ]]; then basename "$d"; fi
  done | sort -u)

# THE REPO SUBJECT (§ 0b, widened by § 0c) — TWO CONSTITUENTS, AND EACH ONE MUST BE THERE.
#
# It is tempting to build this as one union and refuse only when the whole thing is empty. That is a
# BUG, and the test suite caught it: with the union, deleting every `src/*/skill/SKILL.md` no longer
# refuses, because the `.claude/skills/` wrappers are still standing and the union is non-empty. The
# gate would report green over a library surface that had VANISHED — the precise `.github#416` shape
# § 0b was written to close, rebuilt by the very widening meant to close another one. A subject with
# two constituents needs two refusals: an absent constituent is one that MOVED, not one that is empty.
#
# `if`, NOT `[[ -f $b ]] && printf`. The LAST thing the first loop tests is the README, and a tree
# without one leaves the `for` returning 1 — which `set -o pipefail` (line 1) promotes to the whole
# pipeline, so the assignment fails and `set -e` kills the script: exit 1, no findings, no banner,
# nothing. A gate that dies without a word is the worst thing in this file's value system, and the
# existence of an OPTIONAL member of the subject must not be able to cause it.
lib_body_paths=$(
  for b in src/*/skill/SKILL.md template/product-skills/README.md; do
    if [[ -f $b ]]; then printf '%s\n' "$b"; fi
  done | sort -u)

# The wrappers, in BOTH roots, minus the kit (§ 0c). Keyed on the SET, not on either root's presence:
# root-vs-root symmetry is `skill-parity`'s question, and this gate does not need to answer it twice.
wrapper_body_paths=$(
  for root in "$CLAUDE_SKILLS" "$AGENT_SKILLS"; do
    for d in "$root"/*/; do
      if [[ -d $d ]] && ! grep -qxF -- "$(basename "$d")" <<<"$KIT_SKILLS" && [[ -f ${d}SKILL.md ]]; then
        printf '%s\n' "${d}SKILL.md"
      fi
    done
  done | sort -u)

# `|| true`, and it is NOT decoration — it is the same pipefail trap the two loops above are written to
# dodge, one level up. If BOTH constituents were empty, `grep -v` would match nothing, exit 1, redden
# the pipeline under `set -o pipefail`, abort the assignment, and `set -e` would kill the script: exit 1
# with no findings and no banner. The two refusals below are what SAY so, and they are written three
# lines too late to survive that. Let the assignment succeed, and let the refusals do the talking.
repo_body_paths=$(printf '%s\n%s\n' "$lib_body_paths" "$wrapper_body_paths" | grep -v '^[[:space:]]*$' | sort -u || true)

# The same refusal the manifest gets, for the same reason. An empty vocabulary is not "nothing
# resolves" — it is the repo surface's subject gone missing, and every one of its 37 refs would be
# reported dangling: a gate so loud it is indistinguishable from a broken one, and the fix would look
# like deleting the refs. If `.claude/skills/` is gone, this gate is broken; say so.
if [[ -z $repo_vocab ]]; then
  echo "check-skill-refs: FAILED — no skills under $CLAUDE_SKILLS/, so nothing a repo-internal body" >&2
  echo "  points at can be resolved. This gate cannot be green without its vocabulary." >&2
  exit 1
fi
if [[ -z $lib_body_paths ]]; then
  echo "check-skill-refs: FAILED — no repo-internal skill bodies found (src/*/skill/SKILL.md)." >&2
  echo "  That is a subject this gate is supposed to have; finding none means it moved, not that" >&2
  echo "  there is nothing to check. See § 0b." >&2
  exit 1
fi
# The SECOND constituent, refused separately — see the two-refusals note above `lib_body_paths`. The
# vocabulary refusal does NOT cover this: `.claude/skills/` can be full of directories (so `repo_vocab`
# is happy) while every non-kit SKILL.md inside them has been deleted or renamed, and the wrapper
# subject would then vanish in silence behind a green tick.
if [[ -z $wrapper_body_paths ]]; then
  echo "check-skill-refs: FAILED — no non-kit wrapper bodies found under $CLAUDE_SKILLS/ or $AGENT_SKILLS/." >&2
  echo "  That is a subject this gate is supposed to have (§ 0c); finding none means the wrappers moved," >&2
  echo "  not that there is nothing to check. The coordination kit alone is NOT this gate's subject." >&2
  exit 1
fi

# ── ONE PROBE, TWO CONSTANTS (§ 0c and § 0 · #722) ──────────────────────────────────────────────
# `KIT_SKILLS` (§ 0c) and `MIRRORED_SKILLS` (§ 0) are both hand-written lists that NARROW this gate's own
# behaviour, both checked against a registry in FS-GG/.github, and both fatal-in-CI when they cannot be
# checked. So whether `gh` can answer at all is asked ONCE, and the CI refusal is written ONCE and names
# both. Two copies of that policy would be #722's own defect one level down: one rule, two readings, and
# only one of them maintained — which is precisely what this file is being repaired for.
#
# f(world), like § 2, and both inherit § 2's contract: in CI they MUST run, because a self-skip widens a
# blind spot in silence. NEITHER SUBJECT MOVES EITHER WAY — each is built from its own constant,
# hermetically, so § 4's f(tree) split still holds for § 1 and § 3. What an unverified run loses is only
# the promise that the constants still match canonical.
#
# NOT gated on `SKILL_REFS_SKIP_LINKS`, and NOT on `link_mode`. That flag means "skip the LINK half" — it
# says nothing about whether a registry can be read, and honouring it here would trade two 1-request
# checks away for nothing, on a laptop that could perfectly well have made the requests. § 3's rule is
# "degrade toward MORE checking, never less", and skipping a check the environment can run is the wrong
# direction. (`link_mode` is worse still: it is `empty` on a tree with no links at all, a branch taken
# BEFORE this probe, so it says nothing about `gh` either.)
gh_ready=1
gh_unready_why=""
if ! command -v gh >/dev/null 2>&1 || ! gh auth status >/dev/null 2>&1; then
  gh_ready=0
  gh_unready_why="no authenticated \`gh\` (run \`gh auth login\`)"
fi

if ((!gh_ready)) && [[ -n ${GITHUB_ACTIONS:-} ]]; then
  echo "check-skill-refs: FAILED — $gh_unready_why in CI, so FS-GG/.github's registries cannot be read," >&2
  echo "  and NEITHER list this gate narrows itself by can be verified:" >&2
  echo "    * KIT_SKILLS cannot be verified (registry/repos.yml, § 0c) — it decides which bodies this" >&2
  echo "      gate does NOT examine, so a wrong name there is a body of ours nothing checks." >&2
  echo "    * MIRRORED_SKILLS cannot be verified (registry/skills.yml, § 0) — it decides which bodies get" >&2
  echo "      the STRICTER bare-ref rule, so a mirror MISSING from it has its refs judged against our" >&2
  echo "      publish set alone: green here, dangling in the owning repo's gate (#722)." >&2
  echo "  Both are blind spots, not details. Give the step a token (env: GH_TOKEN: \${{ secrets.GITHUB_TOKEN }})." >&2
  exit 1
fi

# fetch_registry <file> — the BYTES of FS-GG/.github `registry/<file>` into REGISTRY_YAML; non-zero with
# `gh`'s own complaint in REGISTRY_WHY.
#
# "COULD NOT READ IT" AND "READ IT AND IT SAID NOTHING" ARE DIFFERENT SENTENCES, and this gate has already
# been burned once by a tool that conflated them — `verify-paths` reports "not inside a GitHub checkout"
# when the real cause is an exhausted rate limit (.github#430), sending the reader to debug a checkout
# that was fine. So the FETCH is kept separate from the PARSE: `gh`'s own stderr is captured and quoted
# back, and a failed request never gets to masquerade as a registry whose shape we misread. BOTH callers
# inherit that, and a fix to it fixes both — the shape #722 is about.
#
# `Accept: …raw` — the file's BYTES, not the `contents` JSON envelope whose `.content` is base64. It is
# one fewer stage, and it drops `base64 -d`, which is GNU-only: BSD/macOS spell it `-D`, so the decode
# would have failed on a maintainer's laptop and surfaced as an empty-parse refusal — a portability bug
# wearing the costume of a parse bug.
REGISTRY_YAML=""
REGISTRY_WHY=""
fetch_registry() { # <file>
  local err
  err=$(mktemp)
  if REGISTRY_YAML=$(gh api "repos/FS-GG/.github/contents/registry/$1" \
                       -H 'Accept: application/vnd.github.raw' 2>"$err"); then
    rm -f "$err"
    return 0
  fi
  REGISTRY_WHY=$(grep -v '^[[:space:]]*$' "$err" | head -2 | tr '\n' ' ' | sed 's/[[:space:]]*$//' || true)
  rm -f "$err"
  : "${REGISTRY_WHY:=\`gh api\` failed with no message}"
  return 1
}

# ── THE KIT EXCLUSION IS CHECKED, NOT ASSUMED (§ 0c) ────────────────────────────────────────────
# `KIT_SKILLS` decides what this gate DOES NOT LOOK AT, and § 0c is the argument for why that list, of
# all the lists in this repo, may not go unverified: its fail-open direction is a body of OURS whose refs
# are silently never examined, under a gate that still prints `ok`.
#
# IT SITS HERE, NEXT TO THE SUBJECT IT VALIDATES, and not down with the link half where the rest of the
# network work lives. `KIT_SKILLS` narrowed `wrapper_body_paths` twenty lines up; § 1 is about to judge
# that narrowed set. A check that says "the narrowing was legitimate" belongs BEFORE the narrowing is
# relied on, not several hundred lines after — the dependency and the ordering should agree, or the next
# person to add a finding between them inherits a verdict over a subject nobody had validated yet.
# `verify_mirrors` sits where ITS subject is built, for the same reason.
kit_mode=checked
kit_skip_reason=""
if ((!gh_ready)); then
  kit_mode=skipped
  kit_skip_reason=$gh_unready_why
fi

if [[ $kit_mode == checked ]]; then
  if ! fetch_registry repos.yml; then
    if [[ -n ${GITHUB_ACTIONS:-} ]]; then
      echo "check-skill-refs: FAILED — could not READ FS-GG/.github registry/repos.yml: $REGISTRY_WHY" >&2
      echo "  So KIT_SKILLS (§ 0c) is unverified, and that list decides which bodies this gate does NOT" >&2
      echo "  examine. This is the ROSTER being unreadable, not the roster being wrong — do not go" >&2
      echo "  looking for a parse bug. A check that did not run has proved nothing." >&2
      exit 1
    fi
    # Locally, an unreachable roster is the offline case, not a defect in the tree. Announce and degrade.
    kit_mode=skipped
    kit_skip_reason="could not read the roster: $REGISTRY_WHY"
  else
    kit_yaml=$REGISTRY_YAML
    # The kit rows of the roster: `kind: skill` only. `fsgg-coord` is a kit row too, but it is
    # `kind: client` — a script, not a body — and no part of this gate's subject either way.
    kit_canonical=$(
      printf '%s\n' "$kit_yaml" \
        | awk '/^kit:/{inkit=1; next} inkit && /^[^ \t#-]/{inkit=0} inkit' \
        | grep -E 'kind:[[:space:]]*skill' \
        | sed -nE 's/.*[{,][[:space:]]*id:[[:space:]]*([A-Za-z0-9_.-]+).*/\1/p' \
        | sort -u || true)

    # NOW an empty result really does mean what this message says: the bytes arrived and carried no kit
    # rows. That is the roster's shape changing under us, and it is not something to pass green over —
    # it would excuse every name in the constant on the strength of a parse that matched nothing.
    if [[ -z $kit_canonical ]]; then
      echo "check-skill-refs: FAILED — read FS-GG/.github registry/repos.yml ($(wc -l <<<"$kit_yaml") lines)" >&2
      echo "  but found no \`kind: skill\` kit rows in it. KIT_SKILLS (§ 0c) is therefore unverified, and" >&2
      echo "  this gate will not call an unverified exclusion green. The roster PARSED to nothing, so its" >&2
      echo "  shape has changed: teach this parse. Do not skip it." >&2
      exit 1
    fi

    if ! kit_diff=$(diff <(sort -u <<<"$KIT_SKILLS") <(printf '%s\n' "$kit_canonical")); then
      echo "check-skill-refs: FAILED — KIT_SKILLS does not match FS-GG/.github's coordination-kit roster." >&2
      echo "  '<' is in this script and NOT canonical; '>' is canonical and NOT in this script:" >&2
      # `|| true`: a `grep` that matches nothing exits 1, which `set -o pipefail` would promote and
      # `set -e` would turn into a wordless death — killing the three lines below, which are the ones
      # that say which direction is dangerous. A gate does not die mid-explanation.
      grep -E '^[<>]' <<<"$kit_diff" | sed 's/^/    /' >&2 || true
      echo "  A '<' line is the DANGEROUS one: this gate is skipping a body that no coordination-kit row" >&2
      echo "  protects, so its refs are examined by NOTHING (§ 0c). A '>' line means the kit grew, and this" >&2
      echo "  gate is now resolving another repo's issue numbers against $SELF_REPO." >&2
      echo "  Fix KIT_SKILLS in this script to match the roster." >&2
      exit 1
    fi
  fi
fi

# A BODY HAS EXACTLY ONE SURFACE, AND THAT IS CHECKED, NOT ASSUMED.
#
# `is_repo_body` decides which set judges a body, and it WINS everywhere it is consulted — `resolves_for`,
# `published_body_files`, `emit_bare`. So a body that is somehow BOTH — published by the manifest AND
# sitting under `src/*/skill/` — is silently treated as repo-internal, and BOTH halves of that are wrong
# in the dangerous direction:
#
#   * it loses § 3's rejection of a bare `#N` — while genuinely MATERIALIZING into a stranger's repo,
#     which is the precise hazard § 3 exists for and the precise body it would fire on; and
#   * its `[[refs]]` are judged against `.claude/skills/` rather than the manifest, so a ref that
#     dangles for the product reader who actually receives it is called green.
#
# The gate would report `ok`, and the one body it was most wrong about is the one it never mentioned.
#
# This IS disjoint today — the manifest supplies from `template/` roots, never from `src/` — so it would
# be easy to write "disjoint by construction" in a comment and move on. That is exactly what the first
# draft of this line did. But the manifest ALREADY supplies three skills from off-convention roots
# (`template/base/`, `template/fragments/`, `template/feedback-report/`), so "the roots never overlap" is a
# convention, not a construction, and the day someone publishes a library skill straight out of
# `src/*/skill/` this gate goes quietly wrong. An invariant this file relies on is one it checks — and
# here it is free, because both lists are already in hand.
overlap=$(comm -12 <(sort -u <<<"$body_paths") <(sort -u <<<"$repo_body_paths") | grep -v '^[[:space:]]*$' || true)
if [[ -n $overlap ]]; then
  echo "check-skill-refs: FAILED — a body cannot be on BOTH surfaces, and these are:" >&2
  while IFS= read -r b; do [[ -n $b ]] && echo "    $b" >&2; done <<<"$overlap"
  echo "  Each is supplied by $MANIFEST (so it MATERIALIZES into a product) and also matches the" >&2
  echo "  repo-internal glob (src/*/skill/SKILL.md, template/product-skills/README.md). This gate" >&2
  echo "  would judge it as repo-internal — against $CLAUDE_SKILLS/ instead of the publish set, and" >&2
  echo "  WITHOUT § 3's bare-#N rejection, which a materialized body is exactly the thing that needs." >&2
  echo "  Supply it from a template/ root, or stop publishing it. See § 0b." >&2
  exit 1
fi

# The FULL subject: both surfaces. There is no invocation that checks one and not the other (§ 0b).
all_body_paths=$(printf '%s\n%s\n' "$body_paths" "$repo_body_paths" | grep -v '^[[:space:]]*$' | sort -u)

# NUL-separated, so a path with a space cannot split. `-r` on every consuming xargs: with an empty
# list, xargs would otherwise run its command with NO file operands, and grep/awk would then read
# THIS SCRIPT'S stdin — reporting zero hits from whatever it found there. A silent no-op is the one
# outcome this gate may never produce.
body_files() { while IFS= read -r b; do [[ -n $b ]] && printf '%s\0' "$b"; done <<<"$all_body_paths"; }

# The PUBLISHED bodies alone — § 3's subject, and only § 3's. A bare `#N` is a defect BY FORM there and
# is RESOLVED rather than rejected on the repo surface; the argument is in § 3's inversion note.
#
# `if`, NOT `[[ … ]] && … && printf`. A `while` returns the status of the LAST command its body ran, so
# an `&&` chain that short-circuits on the final line makes the whole function return 1 — and under
# `set -o pipefail` (line 1) that reddens every pipeline it feeds, aborting the assignment downstream
# and killing the script with a bare `exit 1` and NOT ONE WORD of output. The filter's verdict on the
# last file is not the function's verdict. An `if` with no `else` returns 0 whichever way it goes.
published_body_files() {
  while IFS= read -r b; do
    if [[ -n $b ]] && ! is_repo_body "$b"; then printf '%s\0' "$b"; fi
  done <<<"$all_body_paths"
}

is_repo_body() { grep -qxF -- "$1" <<<"$repo_body_paths"; }

# The whole-tree set the hermetic halves (§ 1, § 3) sweep. Defined HERE rather than down in § 2 because
# § 1 consumes it and § 1 runs first; the argument for why it is never scoped lives at its old site.
md_files() { body_files; }

# The directories the bodies live in — the pathspec `--changed` diffs against (§ 4). Derived from the
# manifest too, so a skill supplied from a new root is picked up here the moment it is published,
# rather than being quietly outside the merge gate's scope.
#
# An ARRAY, not a word-split string. A `supplied-by` with a space in it would split into two pathspecs
# that match nothing, and `git diff` would report no changed files — so the link half would announce
# "this diff touches no published skill body" on a PR that edited one. That is a green gate over a
# subject it never examined, arriving through the very fallback that exists to prevent it (§ 4).
mapfile -t body_dirs < <(jq -r '.skills[]."supplied-by"' "$MANIFEST" | sed 's:/*$::' | sort -u)

# The repo bodies join the pathspec as FILES, not as their directories. `src/Scene/skill` would work,
# but `template/product-skills` — the README's directory — is the parent of every published body, so
# naming it would make the link scope match all 17 of them on a diff that touched only the README. The
# file path IS a valid pathspec and matches exactly the one body. Being exact costs nothing here.
mapfile -t repo_body_specs < <(printf '%s\n' "$repo_body_paths")
body_dirs+=("${repo_body_specs[@]}")

# -x -F, never -w: `grep -w game` matches inside `fs-gg-game` (a `-` is a word boundary), and an
# unanchored pattern is a REGEX, so `fs.gg.game` would match too. Both would wave a typo'd owner
# through — and a foreign qualified ref is trusted, so nothing downstream would catch it.
is_published() { grep -qxF -- "$1" <<<"$published"; }
is_known_owner() { grep -qxF -- "$1" <<<"$KNOWN_OWNERS"; }
in_repo_vocab() { grep -qxF -- "$1" <<<"$repo_vocab"; }

# § 1's verdict, and the ONE thing that turns on the surface (§ 0b): does this id resolve WHERE THIS
# BODY IS READ? In a product, that is what we materialize. In this tree, it is what an agent can invoke.
resolves_for() { # file id
  if is_repo_body "$1"; then in_repo_vocab "$2"; else is_published "$2"; fi
}

# Every § 1 finding NAMES the set that judged it — so a dangling ref says which vocabulary answered
# rather than leaving the author to guess why `[[fs-gg-ant-design]]` is fine in one body and dangling
# in another. That is written into each message at its site, in that surface's own words, rather than
# funnelled through one generic helper: "we do not publish it" and "no skill by that name resolves
# here" are different sentences because they are different facts (§ 0b).

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
# THIS LIST'S POLARITY INVERTED WITH THE STOPGAP (#714), AND THE ROT ANALYSIS INVERTS WITH IT. It used
# to be an ESCAPE HATCH — being listed DEMOTED a § 1 finding to a note — so omitting a skill meant it
# got MORE checking, and the old argument here was that it "degrades toward MORE checking, never less".
#
# It is now the opposite. Being listed makes this gate STRICTER: a bare `[[ref]]` in a listed body is a
# hard failure (§ 1), because a bare ref cannot be right in both repos. So:
#
#   * MIRRORED_SKILLS goes STALE — a body stops being mirrored (the #696 provider epic retires it) and
#     nobody removes it here. Bare refs in a body we now fully OWN are hard-failed as "bare in a
#     MIRRORED body", telling the author to qualify a ref that is perfectly correct. A FALSE RED — loud
#     and wrong, but loud. Someone hits it, reads § 0, deletes the entry.
#
#   * A FIFTH skill becomes mirrored and nobody adds it here — AND THIS IS NOW THE DANGEROUS ONE, so it
#     is written down rather than argued away. Its bare refs are judged against OUR publish set alone,
#     so a bare `[[fs-gg-scene]]` in it PASSES here, silently, while dangling in Game's gate. That is
#     precisely the incoherence #714 exists to end, re-created by an omission. It fails OPEN.
#
# ── SO THE LIST IS CHECKED (#722) ───────────────────────────────────────────────────────────────
# This comment used to end "NOTHING HERE CAN CATCH THAT", and argued the gap was better than a guard:
# the mirror set turns on the registry's `owner:`, which lives in FS-GG/.github, and this gate "takes
# no network so it can answer a hermetic question". The first half is true and is why the check below
# is f(world). The second half was already false when it was written — § 0c's kit check reads
# FS-GG/.github over the network, in this file, and landed one PR earlier (#723). What #723 also
# supplied was the ARGUMENT: a constant this gate narrows its own behaviour by, whose rot fails OPEN,
# gets verified against canonical. That is this list, since #714.
#
# TWO READINGS OF ONE RULE WAS THE ACTUAL DEFECT. The frozen-mirror guard has its own reading of "which
# skills are foreign mirrors"; this gate read it off a constant nothing checked. A fifth mirror already
# reds SOMEWHERE — the guard hard-fails on a foreign registry row it has no `Disposition` for. But the fix
# it names is "declare a disposition", and doing exactly that clears its red while THIS list is still stale
# and still fail-open. The hole was never a missing red; it was that the two readings could be repaired
# independently. Now they cannot: the reading that narrows THIS gate is checked against the same registry
# the other one is checked against.
#
# #738 MOVED WHERE THAT OTHER RED LANDS, and this paragraph used to assert the old shape ("both run in the
# same required job", "the .fsx DERIVES the set from the registry"). Both claims are now false, and a stale
# cross-reference in a gate's header is exactly the kind of lie that gets believed:
#
#   * The foreign-skill set is PINNED IN-TREE, in `scripts/FrozenMirrorVerdict.fs` (`foreignSkills`) —
#     that is where a `Disposition` is declared now, NOT in `check-frozen-mirrors.fsx`.
#   * The registry-reading half of that guard is `check-frozen-mirrors.fsx --freshness`, in the
#     NON-REQUIRED `frozen-mirror-freshness` job. It had to leave the required gate: a new `.github`
#     registry row is another repo's commit, and a required gate whose verdict turns on one hands this
#     repo's merge button to that repo (ADR-0105; it wedged every merge here in #714).
#
# NONE OF WHICH WEAKENS THE ARGUMENT ABOVE — it only relocates its second half. Both readings of "which
# skills are mirrors" are still checked against the registry, and still red; what changed is that neither
# red can now freeze the repo. That is the correct place for a check whose subject is another repo's `main`,
# and it is the same line THIS gate's own `verify_mirrors` sits on: fail-open while stale, never a wedge.
#
# WHAT THE REGISTRY CAN SETTLE, AND WHAT IT CANNOT. It names the OWNER — and that is all it can name.
# The eight `owner: fs-gg-game` product rows are indistinguishable from one another there (same scope,
# same owner, same shape), yet we mirror four of them and deliberately ship NO counterpart for the other
# four — ballistics/ai/effects/physics were authored in Game and never migrated, and vendoring one in
# would manufacture a two-copies obligation ADR-0022 §6 never imposed (.github#486; Rendering#505 asked
# and was refused). So "is it a mirror" is `owner: foreign` AND `we publish it`, and only the first half
# is the registry's to answer. `check-frozen-mirrors.fsx` makes the same split and calls it a
# `Disposition`; here the second half is `is_published`, which is the same fact read off the manifest.
#
# AND THE SUBJECT IS THE PUBLISHED SET — the exact boundary, not a convenient one. `mirror_bodies` below
# turns this constant into behaviour by filtering it through `is_published`, so an entry naming a body we
# do NOT publish reaches no verdict this gate can render: it is inert, and it is not flagged. Everything
# that CAN change a verdict is checked. A typo'd entry is still caught, and from the other side — writing
# `fs-gg-persistance` does not silence `fs-gg-persistence`, which is published, foreign, and thereby
# missing from the list.
verify_mirrors() {
  local yaml product canonical declared d

  if ! fetch_registry skills.yml; then
    if [[ -n ${GITHUB_ACTIONS:-} ]]; then
      echo "check-skill-refs: FAILED — could not READ FS-GG/.github registry/skills.yml: $REGISTRY_WHY" >&2
      echo "  So MIRRORED_SKILLS (§ 0) is unverified, and that list decides which bodies get the STRICTER" >&2
      echo "  bare-ref rule. This is the REGISTRY being unreadable, not the constant being wrong — do not" >&2
      echo "  go looking for a parse bug. A check that did not run has proved nothing." >&2
      exit 1
    fi
    # Locally, an unreachable registry is the offline case, not a defect in the tree. Announce and degrade.
    mirror_mode=skipped
    mirror_skip_reason="could not read the registry: $REGISTRY_WHY"
    return 0
  fi
  yaml=$REGISTRY_YAML

  # Every PRODUCT row, as `owner<TAB>id`. Anchored on `[{,]` exactly as § 0c's kit parse is: an
  # unanchored `id:` would match inside another field's VALUE too, and a row whose `source:` carried the
  # string would hand back an id that is not one.
  product=$(
    printf '%s\n' "$yaml" \
      | awk '
          /^[[:space:]]*-[[:space:]]*\{/ {
            if (!match($0, /[{,][[:space:]]*id:[[:space:]]*[A-Za-z0-9_.-]+/))    next
            id = substr($0, RSTART, RLENGTH); sub(/.*id:[[:space:]]*/, "", id)
            if (!match($0, /[{,][[:space:]]*scope:[[:space:]]*[A-Za-z0-9_.-]+/)) next
            sc = substr($0, RSTART, RLENGTH); sub(/.*scope:[[:space:]]*/, "", sc)
            if (!match($0, /[{,][[:space:]]*owner:[[:space:]]*[A-Za-z0-9_.-]+/)) next
            ow = substr($0, RSTART, RLENGTH); sub(/.*owner:[[:space:]]*/, "", ow)
            if (sc == "product") print ow "\t" id
          }' || true)

  # AN EMPTY PARSE IS THE CHECK FAILING, NOT A REGISTRY WITH NO MIRRORS. The bytes arrived; if they carry
  # no `scope: product` row at all, the registry's SHAPE has moved under this parse, and blessing the
  # constant on the strength of a match that found nothing is the fail-open one `sed` away.
  #
  # The MIRROR set may legitimately be empty — that is #696's end state, and it is compared against an
  # empty constant below rather than refused here. The PRODUCT set may not: this repo publishes 17 of
  # those rows, and a registry naming none of them is one we failed to read.
  if [[ -z $product ]]; then
    echo "check-skill-refs: FAILED — read FS-GG/.github registry/skills.yml ($(wc -l <<<"$yaml") lines)" >&2
    echo "  but found no \`scope: product\` rows in it. MIRRORED_SKILLS (§ 0) is therefore unverified, and" >&2
    echo "  this gate will not call an unverified narrowing green. The registry PARSED to nothing, so its" >&2
    echo "  shape has changed: teach this parse. Do not skip it." >&2
    exit 1
  fi

  # `if`, NOT an `&&` chain — a `while` returns the status of the LAST command its body ran, and a
  # short-circuit on the final line would return 1 and kill the script under `set -e` without a word.
  # The same trap `published_body_files` documents; it is no less lethal here.
  canonical=""
  while IFS=$'\t' read -r ow id; do
    if [[ -n $id && $ow != "$SELF_OWNER" ]] && is_published "$id"; then
      canonical+="$id"$'\n'
    fi
  done <<<"$product"

  declared=""
  while IFS= read -r id; do
    if [[ -n $id ]] && is_published "$id"; then
      declared+="$id"$'\n'
    fi
  done <<<"$MIRRORED_SKILLS"

  # CANONICAL FIRST, DECLARED SECOND — the OPPOSITE order to § 0c's kit diff, and deliberately so. In
  # both blocks `<` is the DANGEROUS direction, because that is the thing a reader must be able to trust
  # without re-deriving it: there, a name we exclude that no kit row protects; here, a mirror the registry
  # names that this gate does not know about. The operand order is what makes those line up, so do not
  # "harmonise" it with the kit's without also swapping the two messages below.
  if ! d=$(diff <(sort -u <<<"$canonical" | grep -v '^$' || true) \
                <(sort -u <<<"$declared"  | grep -v '^$' || true)); then
    echo "check-skill-refs: FAILED — MIRRORED_SKILLS does not match FS-GG/.github's skill registry." >&2
    echo "  '<' is a mirror the REGISTRY names and this script does NOT list; '>' is listed here and the" >&2
    echo "  registry says we OWN it:" >&2
    # `|| true`: a `grep` that matches nothing exits 1, which `pipefail` would promote and `set -e` would
    # turn into a wordless death — killing the lines below, which are the ones that say which direction is
    # dangerous. A gate does not die mid-explanation.
    grep -E '^[<>]' <<<"$d" | sed 's/^/    /' >&2 || true
    echo "  A '<' line is the DANGEROUS one: this gate does not know that body is a mirror, so its bare" >&2
    echo "  [[refs]] are judged against OUR publish set ALONE — green here, and dangling in the owning" >&2
    echo "  repo's gate, which is the incoherence #714 closed and an omission here re-opens (§ 0)." >&2
    echo "  A '>' line means the registry says that body is OURS now, and this gate is hard-failing bare" >&2
    echo "  refs in it that are perfectly correct — a loud false red." >&2
    echo "  Fix MIRRORED_SKILLS in this script to match the registry." >&2
    exit 1
  fi
}

# Same policy as § 0c's kit check, from the same probe: verified when `gh` can answer, fatal in CI when
# it cannot (handled at the probe, which names both lists), announced-and-degraded locally.
#
# YES, THIS CAN RED A DIFF THAT TOUCHED NO SKILL — § 4's objection, and it is answered rather than
# ignored. The registry moving under us is f(world), so a fifth mirror reds whoever next opens a PR, and
# the fix (one line, here) is not in their diff. Three things make that the right trade, and the first is
# decisive: `check-frozen-mirrors.fsx --freshness` ALREADY reds that PR, because a foreign registry row with
# no `Disposition` is a hard fail there. The red is not new — only its second half is, and the second half
# is the one that says WHICH list is now lying. Then: `owner:` changes at a migration, not continuously as
# § 2's link state does (it has moved once, at ADR-0022 P4, and moves once more when #696 retires the
# mirror); and while it is stale this gate is fail-OPEN, so merging past it is the thing § 4 would actually
# be protecting.
#
# THIS USED TO SAY "in this same required job", AND #738 MADE THAT FALSE — in the direction that matters,
# because the sentence was doing real work: it is the reason reding a no-skill diff was judged acceptable.
# The frozen-mirror guard's registry-reading half is now the NON-REQUIRED `frozen-mirror-freshness` job
# (ADR-0105 — a required gate may not take its verdict from another repo's `main`; that is what wedged
# every merge here in #714). The argument survives the move intact, and is arguably stronger: BOTH readings
# of "which skills are mirrors" now red in lanes that CANNOT freeze the repo, which is exactly what a check
# whose subject is f(world) should do. What neither may do is go quiet.
mirror_mode=checked
mirror_skip_reason=""
if ((!gh_ready)); then
  mirror_mode=skipped
  mirror_skip_reason=$gh_unready_why
fi
if [[ $mirror_mode == checked ]]; then verify_mirrors; fi

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
#
# AND THE SET IT IS RELATIVE TO IS NOW THE READER'S (#698). "The publish set" was only ever the right
# set because the only bodies in the subject were PUBLISHED ones. With the repo surface in the subject
# (§ 0b), the general rule is the one that was always underneath: a `[[ref]]` resolves against WHAT THE
# READER OF THIS BODY HAS. `resolves_for` picks that set, and the three rules above are unchanged —
# only the noun "published" widens to "resolves". A mirror is a published body, so `is_mirror` is
# unaffected: a repo-internal body is never mirrored, because it is never shipped.

# Strip skill-refs markers, terminating on `-->` rather than on the first `>`. A rationale is prose
# and may well contain one ("superseded -> #9"), and a marker left un-stripped is scanned as a ref
# and EXCUSES ITSELF — the exact defect the strip exists to prevent. Other HTML comments are kept: a
# ref inside one is still a ref.
#
# Shared by ALL THREE scans, and load-bearing for each. For § 2 an un-stripped `closed-ok FS.GG.X#9` is
# a link that vouches for itself; for § 3 an un-stripped `prose-ok #9` is *itself a bare `#9`*. § 1 got
# this LATE (#698) and had a latent hole until it did: its scan was a raw `grep`, so a marker was never
# stripped from it, and the moment `prose-ok [[…]]` exists a marker becomes *itself a `[[ref]]`* —
# reporting the very config written to silence it. Nothing in the tree had a `[[…]]` inside a marker, so
# the hole was invisible; that is what a latent hole is. Config must not be its own subject — in any § .
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

# Every `[[ref]]`, normalised to `file:line:ref`. `read` with three names puts every remaining `:` in
# the last one, which is what a QUALIFIED `owner:id` needs — so this shape survives the colon it carries.
emit_wiki() {
  body_files | xargs -0 -r awk -v OFS=':' "$AWK_STRIP"'
      {
        s = strip_markers($0)
        while (match(s, /\[\[[A-Za-z0-9._:-]+\]\]/)) {
          print FILENAME, FNR, substr(s, RSTART + 2, RLENGTH - 4)
          s = substr(s, RSTART + RLENGTH)
        }
      }'
}
wikirefs=$(emit_wiki | sort -u | sort -t: -k1,1 -k2,2n)

# The prose-ok allowlist for § 1 — `prose-ok [[id]]`, normalised to `file<TAB>id`. A body that TEACHES
# this convention must be able to write its shape without invoking it (§ 1). WHOLE-TREE, pairing with
# § 1: no network, no decay, so nothing to scope away from.
#
# FILE-SCOPED, exactly like the other two markers, and the same caveat applies: it excuses EVERY
# `[[link]]` in that file. Keep them rare.
ref_markers=$( { md_files | xargs -0 -r grep -HEon \
    '<!--[[:space:]]*skill-refs:[[:space:]]*prose-ok[[:space:]]+\[\[[A-Za-z0-9._:-]+\]\]' \
    || true; } | while IFS= read -r m; do
    [[ -z $m ]] && continue
    mfile=${m%%:*}; mrest=${m#*:}; mline=${mrest%%:*}
    mid=${m##*\[\[}; mid=${mid%%\]\]*}
    printf '%s\t%s\t%s\n' "$mfile" "$mline" "$mid"
  done)

# ── ONE ROW LOOKUP, FOUR TABLES (#733) ──────────────────────────────────────────────────────────
# `is_ref_prose`, `wiki_has`, `is_prose` and `is_excused` are the same question asked of four tables:
# "does this file have a row carrying this value?" They were four near-identical awk scans, differing
# only in delimiter and table — and the difference that MATTERED was invisible between them.
#
# THE SUBTLETY, AND WHY IT MAY NOT LIVE IN ONE OF THE FOUR. Three tables can field-split on `$3`.
# `wiki_has` cannot: its ref can itself CONTAIN the delimiter, because a qualified
# `[[fs-gg-game:fs-gg-scene]]` carries a colon. Split it and the comparison is against `fs-gg-game`,
# which never matches — so a live `prose-ok` marker would be reported STALE while it was doing its
# job, and the author would be told to drop the one line keeping their body green. That reasoning
# lived in a comment on exactly ONE of the four, where it protected exactly one of them, and nothing
# stopped the next author re-deriving it for a fifth table and getting it wrong. It is the one-flag-bug
# class (`grep -H`, Game#241) this suite exists to catch, and a gate does not get to commit it.
#
# So: a row is `<file><sep><line><sep><value>`, and THE VALUE IS EVERYTHING PAST THE SECOND SEPARATOR
# — never field 3. That is exact for the colon table, and harmless for the tab ones, whose values
# carry no tab. One helper, one reading, and a fifth caller inherits the fix instead of the bug.
#
# The empty-table guard comes with it. `is_ref_prose` had one and `wiki_has` did not — a difference
# with no reason behind it, which is its own small evidence for this consolidation.
row_has() { # rows sep file value
  [[ -n $1 ]] || return 1
  awk -v sep="$2" -v f="$3" -v v="$4" '
    {
      i = index($0, sep);                if (!i) next
      rest = substr($0, i + length(sep))
      j = index(rest, sep);              if (!j) next
      if (substr($0, 1, i - 1) == f && substr(rest, j + length(sep)) == v) found = 1
    }
    END { exit !found }' <<<"$1"
}

is_ref_prose() { row_has "$ref_markers" $'\t' "$1" "$2"; }   # file id
wiki_has()     { row_has "$wikirefs"    ':'   "$1" "$2"; }   # file id

while IFS=: read -r file line ref; do
  [[ -z ${ref:-} ]] && continue
  is_ref_prose "$file" "$ref" && continue
  finding=""
  if [[ $ref == *:* ]]; then
    owner=${ref%%:*}
    id=${ref#*:}
    if ! is_known_owner "$owner"; then
      finding="dangling [[$ref]] — unknown owner '$owner' (known: $KNOWN_OWNERS)"
    elif [[ $owner == "$SELF_OWNER" ]] && ! resolves_for "$file" "$id"; then
      # Say WHICH set failed it, in that set's own vocabulary. "This repo does not publish it" is the
      # true and useful sentence for a PUBLISHED body — and it is the wrong one for a repo-internal
      # body, which does not publish anything and whose reader wants to hear about `.claude/skills/`.
      # One generic message would be a small lie on both surfaces; the author needs to know which
      # vocabulary answered, because the same ref can resolve in one and dangle in the other (§ 0b).
      if is_repo_body "$file"; then
        finding="dangling [[$ref]] — qualified to this repo, where '$id' does not resolve ($CLAUDE_SKILLS/)"
      else
        finding="dangling [[$ref]] — qualified to this repo, which does not publish '$id'"
      fi
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
  elif ! resolves_for "$file" "$ref"; then
    # The finding NAMES THE SET it was judged against (§ 0b): the same `[[fs-gg-ant-design]]` resolves
    # in a repo-internal body and dangles in a published one, and an author who is not told which
    # vocabulary answered will read the verdict as a bug in the gate.
    if is_repo_body "$file"; then
      finding="dangling [[$ref]] — no skill by that name resolves in this repo ($CLAUDE_SKILLS/), so an agent reading this body cannot invoke it. Name a skill that is there, or qualify it as [[<owner>:$ref]] if another repo publishes it"
    else
      finding="dangling [[$ref]] — this repo does not publish it; qualify it as [[<owner>:$ref]]"
    fi
  fi
  [[ -z $finding ]] && continue
  report "$file" "$line" "$finding"
done <<<"$wikirefs"

# A `prose-ok [[…]]` that excuses nothing is dead config, exactly as a stale `closed-ok` is — the
# illustration it guarded was reworded, or renamed. Markers are stripped before extraction, so one
# cannot vouch for itself and this check means something.
if [[ -n $ref_markers ]]; then
  while IFS=$'\t' read -r mfile mline mid; do
    [[ -z ${mid:-} ]] && continue
    wiki_has "$mfile" "$mid" ||
      report "$mfile" "$mline" "stale prose-ok marker — nothing in this file writes [[$mid]]; drop it"
  done <<<"$ref_markers"
fi

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

# `md_files` (the whole-tree set § 1 and § 3 sweep) is defined up with `body_files`, because § 1 needs
# it and § 1 now runs above this point. The reason it is whole-tree is here: § 1 and § 3 are f(tree) —
# they need no network, cost nothing, and cannot decay, so there is no reason to scope them and one
# good reason not to. Scoping a hermetic check buys nothing and can only lose coverage.

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

# Under `--changed`, intersect the diff with THE SUBJECT. The `-f` test is not enough on its own: a diff
# may touch any `.md` under a body dir (a README beside a SKILL.md), and only the BODIES are this gate's
# subject — so filter to paths the subject actually contains, not merely to paths that changed under the
# right directory.
#
# `all_body_paths`, NOT `body_paths` (#698). This filtered against the MANIFEST's bodies alone, and it
# had to change with the subject: a repo-internal body is not in the manifest, so it would be dropped
# here — and the link half is EXACTLY the half that is scoped, so under `--changed` (which is what
# gate.yml runs on every PR) the repo surface's links would be checked by NOBODY. The gate would report
# green having examined none of them, on precisely the diffs that touched them. That is the
# `.github#416` shape this whole change exists to close, re-created inside its own fix, and it is
# invisible from the full sweep — where the scope falls through to `md_files` and everything
# looks fine. § 7 pins it with a scoped run over a repo body.
#
# ── MATERIALISED ONCE, AND THE LIST IS WHAT IS MATERIALISED (#733) ──────────────────────────────
# `git diff` is a fork, and this subject has FOUR consumers: `emit_links` (§ 2), `repo_link_files`
# (§ 3's promoted bare refs), the `closed-ok` marker scan, and `is_link_scoped` in the audit below.
# So it is computed HERE, into a variable, and `link_md_files` merely re-emits it.
#
# It used to be a FUNCTION that shelled out to `git diff` on every call, with a comment down at
# `link_files_list` claiming the diff was materialised ONCE. It was materialised once AT THAT ONE
# CALL SITE; the other three re-ran it, so under `--changed` the diff ran FOUR TIMES. Harmless for
# runtime — four forks — but the comment reasoned explicitly about avoiding repeated diffs, and in
# this file the argument IS the artifact: a comment that quietly stops being true is the exact
# failure mode the whole script is written against, and the next reader adds a fifth consumer on the
# strength of it. Materialise the LIST, not one reading of it, and the claim becomes true again.
#
# ── A FAILING `git diff` IS NOT AN EMPTY DIFF, AND THE `|| true` CANNOT TELL THEM APART ─────────
# The obvious form of this — `$( … | grep -v … || true)` over the whole `if` — is a FAIL-OPEN, and
# it was written here and caught in review. The empty case genuinely NEEDS that `|| true` (a scope
# of zero bodies is the NORMAL case: most diffs touch no skill body, and `grep -v` then exits 1 and
# `pipefail` would kill the script). But the same `|| true` also swallows git's exit code — so a
# `git diff` that DIES hands back an empty list, indistinguishable from a clean one, and the run
# then prints "this diff touches no published skill body" and exits GREEN over a subject it never
# examined. Measured, not theorised: with a `git diff` forced to exit 128, that is exactly what it
# printed, while the code this replaced died with git's own `fatal:`. Losing a loud death for a
# silent pass is the `.github#416` shape, in the gate written to close it.
#
# So the diff is taken on its own, its status is CHECKED, and only the FILTER gets the `|| true`.
# It goes through a file rather than a variable because a command substitution cannot carry `-z`
# output at all: bash discards NUL bytes, which is the very byte the `-z` is for.
if [[ $link_scope == diff ]]; then
  diff_out=$(mktemp)
  if ! git diff --name-only -z --diff-filter=ACMR "$CHANGED_BASE...HEAD" -- "${body_dirs[@]}" >"$diff_out"; then
    rm -f "$diff_out"
    echo "check-skill-refs: FAILED — \`git diff\` against '$CHANGED_BASE' failed, so the set of skill" >&2
    echo "  bodies this diff touches is UNKNOWN. That is NOT the same as 'it touches none', and" >&2
    echo "  reading it that way would report green over a subject nothing examined. The base itself" >&2
    echo "  resolved and shares history (both are checked above), so this is git failing, not the" >&2
    echo "  ref being bad. Degrade toward MORE checking, never less (§ 4)." >&2
    exit 1
  fi
  link_files_list=$(
    while IFS= read -r -d '' f; do
      # `if`, not an `&&` chain. The while's status is its LAST command's, so a final changed file
      # that is NOT a body leaves this returning 1 — and under `pipefail` that reddens the
      # pipeline, aborting this assignment and killing the script with a bare `exit 1` and no
      # output at all. The filter's verdict on the last file is not the scan's.
      if [[ -f $f ]] && grep -qxF -- "$f" <<<"$all_body_paths"; then printf '%s\n' "$f"; fi
    done <"$diff_out" | grep -v '^[[:space:]]*$' || true)
  rm -f "$diff_out"
else
  link_files_list=$(printf '%s\n' "$all_body_paths" | grep -v '^[[:space:]]*$' || true)
fi

# `|| true` above, and `if` (not `&&`) here — the same pipefail trap, and here it is REACHABLE on the
# happy path. An EMPTY scope is the NORMAL case under `--changed`: most diffs touch no skill body at
# all (§ 4 says so in as many words). `<<<""` still feeds the loop one empty line, so an `&&` chain
# would short-circuit on it, return 1, redden every pipeline this function feeds, and kill the script
# with no findings and no banner — on precisely the diffs the scope exists to leave alone.
link_md_files() {
  while IFS= read -r f; do
    if [[ -n $f ]]; then printf '%s\0' "$f"; fi
  done <<<"$link_files_list"
}
is_link_scoped() { grep -qxF -- "$1" <<<"$link_files_list"; }

# How many bodies the link half actually LOOKED at — reported, so a scoped run states its subject
# rather than leaving the reader to infer it from a count of zero.
n_link_files=$(grep -c . <<<"$link_files_list" || true)

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
#
# ONE extractor, TWO subjects, TWO verdicts (#698). The scan is identical on both surfaces — a bare
# `#N` is a bare `#N` wherever it is written — but what it MEANS is not, so the caller passes the file
# set and decides. On a published body the form is the defect (§ 3 rejects it); on a repo-internal one
# the ref genuinely points at OUR tracker, so it is fed to § 2 and RESOLVED. See § 3's inversion note.
BARE_AWK='
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
emit_bare_rows() { "$1" | xargs -0 -r awk -v OFS='\t' "$AWK_STRIP$BARE_AWK"; }

# § 3's subject: PUBLISHED bodies, whole tree. Unchanged.
emit_bare() { emit_bare_rows published_body_files; }

# § 2's extra subject: the bare `#N` of a REPO-INTERNAL body, which is a link to THIS repo (§ 3's
# inversion note). SCOPED WITH § 2, not with § 3 — it is now f(world), so it decays like any other link
# and must not redden a diff that did not touch the body. `repo_link_files` is the intersection of the
# link scope with the repo surface, so `--changed` governs it exactly as it governs an explicit link.
repo_link_files() {
  link_md_files | while IFS= read -r -d '' f; do
    # `if`, not `&&` — see published_body_files. A filter whose last file fails the test must not
    # thereby report FAILURE for the whole scan; under `pipefail` that kills the script silently.
    if is_repo_body "$f"; then printf '%s\0' "$f"; fi
  done
}

# The prose-ok allowlist, normalised to `file<TAB>line<TAB>num` — one row per marker.
# WHOLE-TREE, pairing with § 3: no network, no decay, so nothing to scope away from.
#
# It must be read BEFORE the repo surface's bare refs are promoted to links below, because that
# promotion consults it: on the repo surface `prose-ok #N` suppresses the RESOLUTION rather than a
# rejection (§ 3's inversion note), so a marker read too late would be a marker that excused nothing.
prose_markers=$( { md_files | xargs -0 -r grep -HEon \
    '<!--[[:space:]]*skill-refs:[[:space:]]*prose-ok[[:space:]]+#[0-9]+' \
    || true; } | while IFS= read -r m; do
    [[ -z $m ]] && continue
    mfile=${m%%:*}; mrest=${m#*:}; mline=${mrest%%:*}; mnum=${m##*#}
    printf '%s\t%s\t%s\n' "$mfile" "$mline" "$mnum"
  done)

is_prose() { row_has "$prose_markers" $'\t' "$1" "$2"; }   # file num

# `sort -u` must dedupe on the WHOLE row — keying it would collapse two distinct refs sharing a line.
# Order for display in a second, non-unique pass.
bares=$(emit_bare | sort -u | sort -t$'\t' -k1,1 -k2,2n)

# The repo surface's bare refs, promoted to § 2 link rows against THIS repo — because that is precisely
# what GitHub makes of them where this body is read. A `prose-ok #N` still suppresses one: on this
# surface the marker means "do not RESOLVE this", which is the same sentence it always meant.
#
# A row here can COLLIDE with one emit_links already produced — `[#260](https://…/260)` is a link whose
# label the bare scan drops, but a body that writes both the bare `#260` and the URL yields the same
# `owner/repo#num` twice. `sort -u` on the whole row collapses them, so it is resolved once and reported
# once, exactly as the two overlapping § 2 scans have always been.
repo_bares=$(emit_bare_rows repo_link_files | sort -u)

# `if`, NOT `[[ -n $repo_bares ]] && while …`. Under `set -e` a command substitution whose LAST command
# fails aborts the assignment — and with no repo-surface bare refs (the normal case, and the state of
# the tree today) that `[[ -n … ]]` is simply FALSE, so the substitution exits 1 and the whole script
# dies. Silently: exit 1, no findings, no banner, which reads exactly like a gate that failed and told
# you nothing. An `if` with no `else` returns 0. This script may not fail without saying why — it is
# the defect it exists to prevent, and it does not get to commit it itself.
repo_bare_links=$(
  if [[ -n $repo_bares ]]; then
    while IFS=$'\t' read -r f l n; do
      [[ -z ${n:-} ]] && continue
      is_prose "$f" "$n" && continue
      printf '%s\t%s\t%s\t%s\t%s\n' "$f" "$l" "$DEFAULT_OWNER" "$SELF_REPO" "$n"
    done <<<"$repo_bares"
  fi)

links=$( { emit_links; [[ -n $repo_bare_links ]] && printf '%s\n' "$repo_bare_links"; true; } \
  | sort -u | sort -t$'\t' -k1,1 -k2,2n)

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

is_excused() { row_has "$markers" $'\t' "$1" "$2"; }   # file owner/repo#num

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
    # NOT a `row_has` call, and deliberately: a `links` row is five fields, and the key is COMPOSED
    # from three of them (`owner/repo#num`) rather than being a value sitting past the second
    # separator. Forcing it through the helper would mean splitting `$mref` back apart at the call
    # site to re-join it here — contorting both ends to share code that does not fit. `row_has`
    # carries the four tables that ARE the same shape; this one is a different question.
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
#
# AUDITED AGAINST BOTH SURFACES' BARE REFS (#698). A `prose-ok #N` in a repo-internal body is a live
# marker doing a real job — it stops the ref being RESOLVED (§ 3's inversion note) — but its bare ref
# lands in `repo_bares`, never in `bares`. Auditing against `bares` alone would call every one of them
# stale and tell the author to drop the marker that is the only thing keeping their line green. A
# staleness check that fires on live config is worse than none: it teaches people to ignore it.
#
# The union is scope-aware and honestly so: under `--changed`, `repo_bares` holds only the bodies this
# diff touched, so a marker in an UNTOUCHED repo body has nothing to pair with here. That is why the
# audit is gated on the LINK scope — the same reason the `closed-ok` audit is (it is § 2's question,
# and it decays like § 2). A tree-wide run audits every marker; the sweep is where that happens.
all_bares=$(printf '%s\n%s\n' "$bares" "$repo_bares" | grep -v '^[[:space:]]*$' || true)
if [[ -n $prose_markers ]]; then
  while IFS=$'\t' read -r mfile mline mnum; do
    [[ -z ${mnum:-} ]] && continue
    # Under a scoped run, a repo body outside the diff was never scanned for bare refs — so we cannot
    # tell a dead marker from an unexamined one, and "I did not look" must never be reported as "I
    # found nothing" (§ 4). Skip it; the full sweep audits it.
    if [[ $link_scope == diff ]] && is_repo_body "$mfile" && ! is_link_scoped "$mfile"; then
      continue
    fi
    # The FIFTH copy of the scan `row_has` now carries — same shape as `is_prose`, a different table
    # (the union of both surfaces' bare refs, not the markers). It is the helper's whole point that
    # this one is a call and not a re-derivation.
    if ! row_has "$all_bares" $'\t' "$mfile" "$mnum"; then
      report "$mfile" "$mline" "stale prose-ok marker — nothing in this file writes a bare #$mnum; drop it"
    fi
  done <<<"$prose_markers"
fi

if ((fail)); then
  echo >&2
  # THE PINNED SENTENCE. skill-refs-sweep.yml greps this line to learn "the script reported at least
  # one finding" — the sentinel that tells a DRIFTED parser (banner, but zero findings scraped) apart
  # from a clean run. Change it and you must change the grep in .github/workflows/skill-refs-sweep.yml
  # and the pin in scripts/test-skill-refs-sweep.sh, which is what makes it a contract rather than a
  # string. It used to say "a published skill"; #698 widened the subject past what ships, so it says
  # "a skill body" — the noun that is true of BOTH surfaces. A contract sentence that is false of half
  # its subject is how the two ends drift apart in the first place.
  echo "check-skill-refs: FAILED — every pointer in a skill body must resolve: a [[ref]] to a" >&2
  echo "  skill, and an issue/PR link to a LIVE issue (or a marked, deliberate citation of history)." >&2
  echo "  In a PUBLISHED body a bare #N is not a pointer at all — qualify it, or mark it as prose." >&2
  echo >&2
  echo "  A [[ref]] is judged against WHAT ITS READER HAS (§ 0b): the skill manifest for a published" >&2
  echo "  body, $CLAUDE_SKILLS/ for a repo-internal one. The same ref can resolve in one and dangle" >&2
  echo "  in the other, and that is correct — check which body you are in before 'fixing' it." >&2
  echo >&2
  echo "  If the body is one of the four FROZEN MIRRORS (game-core, audio, persistence, model-swap)," >&2
  echo "  do NOT fix it here — our bytes must stay identical to FS.GG.Game's. Fix it there and" >&2
  echo "  re-sync. See § 0 at the top of this script." >&2
  exit 1
fi

# `|| true` on the count, and it is not defensive noise — it is a silent death. `grep -c .` prints `0`
# and EXITS 1 on empty input, so with an empty publish set (a manifest that lists no skills — legal,
# and the state of a repo that publishes nothing yet) `set -e` kills the script HERE, after every check
# has passed: exit 1, no findings, no banner, not one word. The verdict would be a lie and the reason
# for it invisible. Nothing about counting a subject may decide the gate's verdict.
n_skills=$(grep -c . <<<"$published" || true)
n_repo_bodies=$(grep -c . <<<"$repo_body_paths" || true)
n_repo_vocab=$(grep -c . <<<"$repo_vocab" || true)
# No "we OWN" hedge any more (#714). Every [[ref]] in every published body — the four mirrors included —
# is now judged, and every one of them resolves. That sentence could not be said while the stopgap stood.
#
# BOTH SUBJECTS ARE NAMED, and neither is allowed to be silent (#698). "Every [[ref]] resolves" was true
# of the published bodies while 37 refs in the library ones went unexamined — a true sentence that read
# as a claim about the whole tree. A gate that states a narrower subject than the reader assumes is the
# `.github#416` shape wearing a green tick, and this script has now been on both ends of it.
echo "check-skill-refs: ok — $n_skills skills published; every [[ref]] in them resolves against the manifest."
echo "check-skill-refs: ok — $n_repo_bodies repo-internal body/bodies; every [[ref]] in them resolves against $CLAUDE_SKILLS/ ($n_repo_vocab skills)."

# AND THE EXCLUSION IS REPORTED TOO — because it is the one thing this gate deliberately does NOT read,
# and an unstated exclusion is indistinguishable from a subject that never had those bodies in it. That
# is the `.github#416` shape one last time: say what you did not look at, and say whether you were
# entitled to skip it.
n_kit=$(grep -c . <<<"$KIT_SKILLS" || true)
if [[ $kit_mode == checked ]]; then
  echo "check-skill-refs: ok — $n_kit coordination-kit body/bodies per skill root are OUT of subject (§ 0c); KIT_SKILLS verified against FS-GG/.github's kit roster."
else
  echo "check-skill-refs: NOTE — $n_kit coordination-kit body/bodies per skill root were skipped as OUT of subject (§ 0c), but KIT_SKILLS was NOT verified against FS-GG/.github's roster ($kit_skip_reason)." >&2
  echo "  A name wrongly in that list is a body of OURS whose refs nothing examined. CI verifies it; this run did not." >&2
fi

# AND THE STRICTER RULE IS REPORTED ON THE SAME TERMS (#722) — for the same reason, and it is the newer
# half of it: the mirrors are the bodies this gate treats HARDER than the rest, and a run that did not
# confirm WHICH bodies those are has not earned the silence. Say the number, and say whether the list
# that produced it was checked.
n_mirror=$(grep -c . <<<"$mirror_bodies" || true)
if [[ $mirror_mode == checked ]]; then
  echo "check-skill-refs: ok — $n_mirror frozen mirror(s) (§ 0) hard-fail a bare [[ref]]; MIRRORED_SKILLS verified against FS-GG/.github's skill registry."
else
  echo "check-skill-refs: NOTE — $n_mirror frozen mirror(s) (§ 0) were treated as such, but MIRRORED_SKILLS was NOT verified against FS-GG/.github's skill registry ($mirror_skip_reason)." >&2
  echo "  A mirror MISSING from that list has its bare [[refs]] judged against our publish set alone — green here, dangling in the owning repo. CI verifies it; this run did not." >&2
fi

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
#
# AND IT NAMES ITS SUBJECT, which since #698 is narrower than the tree: § 3's REJECTION applies to the
# PUBLISHED bodies only. A repo-internal body's bare `#N` is not a defect to count here — it is a link,
# and the LINK half judges it (§ 3's inversion note). Saying "no bare #N refs" flat would be a claim
# about a subject this line does not have, which is the exact error the line exists to prevent. Name
# the subject, or say nothing you cannot stand behind.
if ((n_bare > 0)); then
  echo "check-skill-refs: ok — $n_bare bare #N ref(s) in published bodies; every one is marked prose-ok."
else
  echo "check-skill-refs: ok — no bare #N refs in published bodies."
fi

# ── AND THE REPO SURFACE'S BARE REFS ARE ONLY AS CHECKED AS THE LINK HALF IS ────────────────────
# The bill this surface pays for § 3's inversion, and it is stated rather than buried.
#
# On a published body a bare `#N` is wrong BY FORM, so § 3 can damn it with no network — which is why
# § 3's header (above) insists it stays UNGATED by `link_mode`: "hanging it off `link_mode` would make
# an offline check silently skippable — the very shape § 3 exists to close." On a REPO body the same
# token is not wrong by form at all; it is a LINK, and whether it resolves is f(world). So its verdict
# genuinely CANNOT be reached offline, and promoting it into the link half necessarily hands it that
# half's fate: when the link half does not run, these are not examined by anything.
#
# THAT IS TOLERABLE. WHAT IS NOT is saying nothing about it — which is precisely what this line did
# when first written: it printed "in a repo-internal body a bare #N is a link, and was resolved as one"
# UNCONDITIONALLY, so an offline run over a body citing an issue that does not exist passed green while
# asserting an examination that never happened. The `.github#416` shape, in the gate written to close
# it. CI is safe (an unauthenticated `gh` is fatal there, and `SKILL_REFS_SKIP_LINKS` is ignored), so
# it could have sat here for a long time being wrong only on the laptops of the people maintaining it.
#
# Counted from the PROMOTED rows, not the raw scan: a bare ref silenced by `prose-ok` is not a link and
# was never going to be resolved, so counting it here would manufacture a warning about a ref the author
# has already, deliberately, declared to be prose.
n_repo_bare=$(grep -c . <<<"$repo_bare_links" || true)
if ((n_repo_bare > 0)); then
  if [[ $link_mode == checked ]]; then
    echo "check-skill-refs: ok — $n_repo_bare bare #N ref(s) in repo-internal bodies; each resolved as a link to $SELF_REPO, which is what GitHub makes of it where that body is read."
  else
    echo "check-skill-refs: NOTE — $n_repo_bare bare #N ref(s) in repo-internal bodies were NOT examined ($skip_reason)." >&2
    echo "  On that surface a bare #N is a LINK to $SELF_REPO (§ 3), so the link half is the only thing" >&2
    echo "  that judges it — and it did not run. This is NOT a clean bill of health for them." >&2
  fi
fi
