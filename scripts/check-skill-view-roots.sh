#!/usr/bin/env bash
# The RUNTIME SKILL-ROOT SET this repo declares — asserted on every `Deterministic gate` run, which is
# a REQUIRED check on `main`, against ADR-0011's two roots as amended by ADR-0067 §5.
#
# WHY THIS EXISTS (FS-GG/.github#1747, ADR-0067 §9 phase 4). This repo used to commit its skills
# TWICE: `.claude/skills` and `.agents/skills` held byte-identical trees (50 ids, 70 tracked files
# each; `diff -r` between them was silent, measured at 44981d8 before anything was retired). Phase 4
# retired the second copy: `.agents/skills` is now a VIEW root (ADR-0065 §A root's three dispositions)
# whose content `scripts/skill-view generate` resolves from `.claude/skills` at checkout. The union of
# `<FsggKitSkillRoots>` and `<FsggKitViewSkillRoots>` is the runtime root set, and it did not change.
#
# WHAT THE RETIREMENT GAVE UP, WHICH IS THE ONLY REASON THIS FILE IS HERE. Before it, a change that
# dropped `.agents/skills` from this repo's runtime contract would have been caught by
# `coordination-coherence`: the root was materialized into, so removing it produced missing files
# against the pin. Now it is not materialized into, and both kit gates go QUIET instead of red.
# MEASURED ON THIS REPO'S OWN TREE, 2026-07-28, with the root emptied out of
# `<FsggKitViewSkillRoots>` and the directory deleted:
#
#   * `dotnet build .config/kit/FS.GG.Kit.receiver.proj -t:FsggKitMaterialize`
#       -> "FS.GG.Kit: materialized 30 file(s) (0 written)"
#          "FS.GG.Kit: no view skill roots declared (FsggKitViewSkillRoots is empty) — nothing to
#           assert."   Build succeeded, 0 Warning(s), 0 Error(s).
#   * `coordination-sync` in its `check` + `against-pin` mode, `--repo FS-GG/FS.GG.Rendering .`
#       -> "skill roots = .claude/skills (from .config/kit/FS.GG.Kit.receiver.proj)"
#          "OK — all 28 materialized file(s) match the FS.GG.Kit 0.15.0 this tree pins."
#
# That second one IS the required `kit / coordination-kit` context. Both green, and `.agents/skills`
# simply gone from the runtime contract. The only observable consequence would be that Codex resolves
# zero skills here and exits 0 saying nothing (ADR-0067 §8's measured silent class). That is exactly
# the trade ADR-0067 §8 forbids — "a rewrite that removes the loud failure and adds the quiet one is
# worse than no rewrite" — so the retirement ships the replacement alarm in the same change. This
# is it. A gate that is green on the tree it exists to fail is decoration; this one is not.
#
# WHY A STANDALONE SCRIPT ON `Deterministic gate`, AND NOT A FOURTH SHAPE. Three receivers have paid
# for this alarm already and each wired it to the required check it actually had:
# `FS.GG.Templates` -> `tests/composition`; `FS.GG.Audio` -> `Build + test`; `FS.GG.Net` -> a NEW gate
# job that is NOT required, which is why FS-GG/.github#1727 is open against it. This repo's required
# contexts are `Deterministic gate`, `API compatibility gate (breaking-change → SemVer major)`,
# `kit / coordination-kit` and `skill-view-check`. Two are authored here; `kit / coordination-kit` is a
# `uses:` of the hub's reusable workflow and cannot be given a step at all. `Deterministic gate`
# already carries this repo's other skill-root assertion (`scripts/materialize-skill-roots.sh --check`,
# FS.GG.Rendering#1080/#1082), so the two live together and are read together. Same assertion as
# Audio's, same can-fire discipline, wired to the required check this repo actually has — a third
# instance of a shape, deliberately not a fourth shape. FS-GG/.github#1710 owns collapsing all of them
# into one, and this file is the fourth hand-copy it predicted.
#
# IT GRADES THE DECLARATION, NOT MSBUILD'S EVALUATION, and that is deliberate rather than lazy. The
# faithful alternative is `dotnet msbuild -getProperty:` on the receiver project, which needs a RESTORE
# of the pinned FS.GG.Kit — a network round-trip added to a REQUIRED check to grade a two-line fact
# this repo authors in its own tree. It would also introduce a second source of truth for the package's
# defaults: a property this repo does NOT declare evaluates to the package default, so a text reader
# would have to restate `.claude/skills;.agents/skills` to interpret an absence, and a restated default
# is the invented-location bug one file over. Requiring BOTH properties to be declared EXPLICITLY
# removes the question: an absence is a RED, not a guess.
#
# `.codex/skills` IS NOT IN THIS ALARM'S SUBJECT, AND MUST NOT BE ADDED TO IT. ADR-0067 §5 retired it
# from the runtime contract (FS-GG/.github#1636), and the pinned kit sweeps only the kit's OWN four
# directories out of a retired root — this repo's 46 skills of its own stay there, untouched, and
# ADR-0065 §Retiring a root forbids anyone hand-deleting them. "This repo keeps files under
# `.codex/skills`" and "`.codex/skills` is a runtime root" are different claims; naming it here would
# assert the second one. Narrowing `scripts/materialize-skill-roots.sh`'s own `DEFAULT_ROOTS` from
# three to two is a separate contract change with its own item (FS.GG.Rendering#1120) and is NOT
# ridden in on this file.
#
# Fails CLOSED throughout: an unreadable project, a missing property, a multi-line declaration this
# reader cannot parse, a union that is not ADR-0011's two, and a view root that is absent or does not
# expose the live root are each a failure. "I could not look" is never "looked, and fine"
# (FS-GG/.github#266).

set -euo pipefail

REPO_ROOT="${REPO_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"

# ADR-0011 Decision 1 as amended by ADR-0067 §5 and executed by FS-GG/.github#1636: `.codex/skills` is
# retired, and the runtime root set is these two. SORTED, so the comparison is set equality and not an
# accident of which property each root is declared in — moving a root between the two properties is a
# legal disposition change (ADR-0065) and must NOT red this.
FSGG_RUNTIME_ROOTS_EXPECTED='.agents/skills .claude/skills'

# The receiver project is where both properties live.
FSGG_RECEIVER_PROJ="${FSGG_RECEIVER_PROJ:-$REPO_ROOT/.config/kit/FS.GG.Kit.receiver.proj}"

PASS=0
FAIL=0
ok()  { PASS=$((PASS + 1)); printf '  \xe2\x9c\x93 %s\n' "$1"; }
bad() { FAIL=$((FAIL + 1)); printf '  \xe2\x9c\x97 %s\n' "$1"; }

# msbuild_property <file> <name>
# Echo the text of a single-line `<name>value</name>` element; echo nothing and return 1 when the
# element is absent, empty, or not on one line. Deliberately NOT an XML parser: the one thing this
# needs to distinguish is "declared with a value" from "anything else", and every "anything else"
# lands on the same red. A declaration this cannot read is a declaration a reviewer should reformat.
msbuild_property() {
  local file="$1" name="$2" value
  [[ -r "$file" ]] || return 1
  value="$(sed -n "s|^[[:space:]]*<${name}>\(.*\)</${name}>[[:space:]]*$|\1|p" "$file" | head -1)"
  [[ -n "$value" ]] || return 1
  printf '%s' "$value"
}

# runtime_root_union <file>
# Echo the sorted, space-separated union of <FsggKitSkillRoots> and <FsggKitViewSkillRoots>. Returns 1
# with nothing on stdout when either property is not declared — an undeclared property is the failure
# this alarm exists for, so it must not be silently treated as an empty contribution.
runtime_root_union() {
  local file="$1" live view
  live="$(msbuild_property "$file" FsggKitSkillRoots)"     || return 1
  view="$(msbuild_property "$file" FsggKitViewSkillRoots)" || return 1
  printf '%s;%s' "$live" "$view" | tr ';' '\n' \
    | sed 's|[[:space:]]||g; s|/*$||' | grep -v '^$' | sort -u | paste -sd' ' -
}

# assert_runtime_roots <lane>
assert_runtime_roots() {
  local lane="$1" union
  if ! union="$(runtime_root_union "$FSGG_RECEIVER_PROJ")"; then
    bad "$lane: cannot read the runtime root set from $FSGG_RECEIVER_PROJ — both <FsggKitSkillRoots> and <FsggKitViewSkillRoots> must be declared, each on ONE line. ADR-0067 §9 phase 4 made this repo's second runtime root a generated VIEW, and no other gate can see it leave the contract (see this file's header)."
    return
  fi
  if [[ "$union" == "$FSGG_RUNTIME_ROOTS_EXPECTED" ]]; then
    ok "$lane: runtime skill roots are ADR-0011's two ($union) — the union of <FsggKitSkillRoots> and <FsggKitViewSkillRoots>"
  else
    bad "$lane: this repo's runtime skill roots are '$union', not '$FSGG_RUNTIME_ROOTS_EXPECTED'. A root that leaves this union leaves the runtime contract, and BOTH kit gates stay green while it does: coordination-coherence looks only at <FsggKitSkillRoots>, and FsggKitCheckSkillView reports 'nothing to assert' for an empty <FsggKitViewSkillRoots>. Codex would then resolve zero skills here and exit 0 saying nothing (ADR-0067 §8). If the root set is genuinely meant to change, that is an ADR-0065 §Retiring a root contract migration — amend the record and this constant in the same change."
  fi
}

# THE VIEW IS ACTUALLY THERE, not merely declared — and here an ABSENT view root is a RED.
#
# THIS IS A DELIBERATE DIVERGENCE FROM FS.GG.Audio's COPY OF THIS FILE, WHICH TREATS AN ABSENT VIEW
# ROOT AS EXPECTED. Audio's alarm rides `Build + test`, a job that never materializes and never
# generates, so on that runner an absent view root is the normal state and reddening it would fire on
# every green build. This repo is the opposite case: the `Deterministic gate` job runs
# `scripts/skill-view generate` as a step BEFORE this one — it has to, because the very next step is
# `scripts/materialize-skill-roots.sh --check`, whose root set still names `.agents/skills` and which
# dies with "configured root is absent" without it. So by the time this runs, in the one job that
# matters, the view is required to exist, and treating its absence as "expected on a bare clone" would
# be the fail-open half of exactly the mutation this file exists to catch: the generate step deleted
# and the root gone, with the declaration left innocently intact.
#
# The cost of the divergence, said out loud: a developer who runs this script in a fresh clone before
# running the materialize gets a red. That is honest rather than annoying — a fresh clone genuinely has
# no skills at `.agents/skills`, both runtimes genuinely resolve zero there, and the repair is one
# command that the failure names. `scripts/materialize-skill-roots.sh --check` already fails on that
# same tree for the same reason, so this repo's posture is unchanged, not newly strict.
assert_view_resolves() {
  local lane="$1" view="$REPO_ROOT/.agents/skills" live="$REPO_ROOT/.claude/skills" live_n view_n
  if [[ ! -e "$view" ]]; then
    bad "$lane: the view root .agents/skills does not exist. It is generated, never committed (ADR-0067 §6) — run 'dotnet build .config/kit/FS.GG.Kit.receiver.proj -t:FsggKitMaterialize', or 'bash scripts/skill-view generate --source .claude/skills --roots \".agents/skills\"' directly. An absent runtime root is exit 0 with no diagnostic in BOTH runtimes (ADR-0067 §8): the agent quietly has no skills."
    return
  fi
  if [[ ! -d "$view" ]]; then
    bad "$lane: .agents/skills exists but is not a directory — a DANGLING view link, or the plain TEXT FILE a committed symlink degrades to under 'git -c core.symlinks=false', resolves to zero skills and BOTH runtimes exit 0 saying nothing (ADR-0067 §6/§8)."
    return
  fi
  live_n="$(find "$live" -mindepth 1 -maxdepth 1 -type d | wc -l)"
  view_n="$(find -L "$view" -mindepth 1 -maxdepth 1 -type d | wc -l)"
  if [[ "$live_n" -gt 0 && "$live_n" -eq "$view_n" ]]; then
    ok "$lane: the generated view exposes all $view_n skill(s) the live root holds"
  else
    bad "$lane: the generated view exposes $view_n skill(s) but the live root holds $live_n. A partly-visible view root is ADR-0067 §8's silent class, and an empty live root would make 'everything is visible' a statement about nothing."
  fi
}

# assert_can_fire <lane>
# "Demonstrated, not asserted" (FS-GG/.github#1611 category D: a gate that never fires and a gate that
# always passes are indistinguishable from outside). Entirely offline, entirely local: fixture projects
# and fixture trees in a temp dir, driving the ASSERTIONS rather than only the predicates, with the
# counters snapshotted and restored. Driving the assertion is the part that matters — a demo that
# exercises only the predicate survives a mutation of the `bad` arm.
assert_can_fire() {
  local lane="$1" tmp saved_pass saved_fail saved_root proj
  tmp="$(mktemp -d)"
  saved_pass="$PASS" saved_fail="$FAIL" saved_root="$REPO_ROOT"

  local ok_cases=0 fired=0

  # ── the DECLARATION lane ───────────────────────────────────────────────────────────────────────
  # (1) the shape this repo ships: both declared, union is the two roots -> PASS
  proj="$tmp/good.proj"
  printf '<Project>\n  <FsggKitSkillRoots>.claude/skills</FsggKitSkillRoots>\n  <FsggKitViewSkillRoots>.agents/skills</FsggKitViewSkillRoots>\n</Project>\n' > "$proj"
  PASS=0 FAIL=0; FSGG_RECEIVER_PROJ="$proj" assert_runtime_roots "$lane" >/dev/null
  [[ "$FAIL" -eq 0 && "$PASS" -eq 1 ]] && ok_cases=$((ok_cases + 1))

  # (2) the disposition swap: same union, roots declared the other way round -> PASS. This is a legal
  #     ADR-0065 move and reddening it would make the alarm an obstacle to the contract it protects.
  proj="$tmp/swapped.proj"
  printf '<Project>\n  <FsggKitSkillRoots>.agents/skills</FsggKitSkillRoots>\n  <FsggKitViewSkillRoots>.claude/skills</FsggKitViewSkillRoots>\n</Project>\n' > "$proj"
  PASS=0 FAIL=0; FSGG_RECEIVER_PROJ="$proj" assert_runtime_roots "$lane" >/dev/null
  [[ "$FAIL" -eq 0 && "$PASS" -eq 1 ]] && ok_cases=$((ok_cases + 1))

  # (3) THE REGRESSION THIS FILE EXISTS FOR: the view root emptied. Every kit gate is green on that
  #     tree — measured on THIS repo, see the header — and this must not be.
  proj="$tmp/emptied.proj"
  printf '<Project>\n  <FsggKitSkillRoots>.claude/skills</FsggKitSkillRoots>\n  <FsggKitViewSkillRoots></FsggKitViewSkillRoots>\n</Project>\n' > "$proj"
  PASS=0 FAIL=0; FSGG_RECEIVER_PROJ="$proj" assert_runtime_roots "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  # (4) the property deleted outright -> RED. An absent property must never read as an empty
  #     contribution to the union, which would make the deletion the very thing it silently allows.
  proj="$tmp/deleted.proj"
  printf '<Project>\n  <FsggKitSkillRoots>.claude/skills</FsggKitSkillRoots>\n</Project>\n' > "$proj"
  PASS=0 FAIL=0; FSGG_RECEIVER_PROJ="$proj" assert_runtime_roots "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  # (5) a THIRD root added without a contract migration -> RED. The alarm is set equality, not a
  #     minimum: ADR-0065 governs adding a root exactly as it governs removing one. `.codex/skills` is
  #     the realistic mistake here — it is retired (ADR-0067 §5) and this repo still holds 46 of its
  #     OWN skills there, which is not the same thing as it being a runtime root.
  proj="$tmp/extra.proj"
  printf '<Project>\n  <FsggKitSkillRoots>.claude/skills;.codex/skills</FsggKitSkillRoots>\n  <FsggKitViewSkillRoots>.agents/skills</FsggKitViewSkillRoots>\n</Project>\n' > "$proj"
  PASS=0 FAIL=0; FSGG_RECEIVER_PROJ="$proj" assert_runtime_roots "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  # (6) an unreadable project -> RED. "I could not look" is never "looked, and fine".
  PASS=0 FAIL=0; FSGG_RECEIVER_PROJ="$tmp/does-not-exist.proj" assert_runtime_roots "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  # ── the VIEW lane, driven over fixture trees ───────────────────────────────────────────────────
  # (7) a resolved view -> PASS.
  mkdir -p "$tmp/tree-ok/.claude/skills/a" "$tmp/tree-ok/.claude/skills/b" "$tmp/tree-ok/.agents"
  ln -s ../.claude/skills "$tmp/tree-ok/.agents/skills"
  PASS=0 FAIL=0; REPO_ROOT="$tmp/tree-ok" assert_view_resolves "$lane" >/dev/null
  [[ "$FAIL" -eq 0 && "$PASS" -eq 1 ]] && ok_cases=$((ok_cases + 1))

  # (8) THE SECOND HALF OF THE MUTATION: the view root DELETED -> RED. This is the arm Audio's copy
  #     deliberately does not have; see the note above assert_view_resolves.
  mkdir -p "$tmp/tree-absent/.claude/skills/a" "$tmp/tree-absent/.agents"
  PASS=0 FAIL=0; REPO_ROOT="$tmp/tree-absent" assert_view_resolves "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  # (9) a DANGLING link, and the Windows text-file degradation it shares an outcome with -> RED.
  mkdir -p "$tmp/tree-dangling/.claude/skills/a" "$tmp/tree-dangling/.agents"
  ln -s ../.claude/nowhere "$tmp/tree-dangling/.agents/skills"
  PASS=0 FAIL=0; REPO_ROOT="$tmp/tree-dangling" assert_view_resolves "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  # (10) a PARTLY-VISIBLE view — a real directory holding some of the skills -> RED. The class the
  #      kit's own FsggKitCheckSkillView names, reproduced here over the repo's whole set rather than
  #      the kit's four.
  mkdir -p "$tmp/tree-partial/.claude/skills/a" "$tmp/tree-partial/.claude/skills/b" "$tmp/tree-partial/.agents/skills/a"
  PASS=0 FAIL=0; REPO_ROOT="$tmp/tree-partial" assert_view_resolves "$lane" >/dev/null
  [[ "$FAIL" -eq 1 ]] && fired=$((fired + 1))

  PASS="$saved_pass" FAIL="$saved_fail" REPO_ROOT="$saved_root"
  rm -rf "$tmp"

  if [[ "$ok_cases" -eq 3 && "$fired" -eq 7 ]]; then
    ok "$lane: the runtime-root alarm can fire — 7 of 7 regressions RED (emptied view root, deleted property, extra root, unreadable project, absent view, dangling view, partly-visible view) and 3 of 3 legal shapes GREEN"
  else
    bad "$lane: the runtime-root alarm is NOT demonstrably live — $ok_cases/3 legal shapes passed and $fired/7 regressions fired. A gate that cannot fire is not a gate (FS-GG/.github#1611 category D)."
  fi
}

printf 'skill-view-roots: the runtime skill-root contract (ADR-0011 / ADR-0065 / ADR-0067 §8)\n'
assert_runtime_roots "roots"
assert_can_fire      "can-fire"
assert_view_resolves "view"

printf 'skill-view-roots: %d passed, %d failed\n' "$PASS" "$FAIL"
[[ "$FAIL" -eq 0 ]] || exit 1
