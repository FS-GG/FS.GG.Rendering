#!/usr/bin/env bash
# test-harness — the shared half of this repo's behavioural shell suites (FS.GG.Game#259).
#
# Two suites test the two halves of skill-refs:
#
#   scripts/test-check-skill-refs.sh   — the GATE  (FS.GG.Game#243)
#   scripts/test-skill-refs-sweep.sh   — the SWEEP (FS.GG.Game#251)
#
# They were written months apart, and the second grew its own copy of the first's scaffolding. The
# copies had already stopped being the same program, and the divergence was load-bearing rather than
# cosmetic: THE TWO FAKE `gh`s ANSWERED DIFFERENTLY, and the older one could not be wrong. It ignored
# `--jq` and printed the issue state whatever it was asked for, so a subject that asked the API for
# the wrong thing would still have been handed the right answer. A fake that cannot be wrong tests
# nothing — the "green over an unexamined subject" shape that both of these suites exist to refuse,
# committed by the suites themselves.
#
# So there is now ONE fake `gh`, and it is the faithful one: it derives its HTTP method the way real
# `gh` does, honours the query parameters of the tracker lookup, and applies `--jq` to a real
# response body. The gate suite only ever makes it answer one read today — but when the gate grows a
# second `gh` call, it will meet a fake that CAN be wrong, which is the whole point.
#
# SOURCED, NEVER EXECUTED:
#
#     REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
#     # shellcheck source-path=SCRIPTDIR
#     # shellcheck source=lib/test-harness.sh
#     . "$REPO_ROOT/scripts/lib/test-harness.sh"
#     harness_init
#
# COPY BOTH PRAGMAS, and in that order. `source=` is resolved against shellcheck's source path, which
# defaults to the CWD — not to the script holding the pragma — so on its own `lib/test-harness.sh`
# names nothing from the repo root and this file is never followed. Nothing fails loudly: shellcheck
# emits SC1091 (an `info`, which a `-S warning` floor never prints) and lints the sourcing suite with
# this file UNREAD — so every variable the suite sets FOR it (`RC`, read only by `expect_rc` below;
# `HARNESS_OUTPUT_LABEL`, read only by `bad`) looks unused, and shellcheck invents an SC2034 for each.
# That is not a hypothetical: it is where #266's three "unused variable" findings came from, and the
# obvious fix — deleting the "dead" `RC` — would have broken every exit-code assertion in both suites.
# `source-path=SCRIPTDIR` resolves it from any working directory. The gate REDs on SC1091 (gate.yml,
# "Lint the repo's own shell"), so a suite that copies only half of this will be caught — but the
# cheaper moment to get it right is here.
#
# What it gives you: the counters and the summary block, the `ok`/`bad`/`expect_*` family, the
# counted fixture builder, one faithful fake `gh` with the state helpers that drive it, and the
# `skill`/`issue` fixture writers.
#
# What it deliberately does NOT give you is a way to RUN your subject. The gate suite runs a script
# in a git repo; the sweep suite runs `run:` blocks extracted from a YAML file with a runner's env
# around them. That half is genuinely different, it is where each suite's reasoning lives, and
# merging it would produce a `run()` with a mode flag — which is two functions wearing one name.

[[ ${BASH_SOURCE[0]} != "${0}" ]] || {
  echo "test-harness: this file is sourced by a suite, not run directly" >&2; exit 2; }

# The fake `gh` answers the tracker lookup with `jq`, so jq is a requirement of the HARNESS, not of
# whichever suite happens to exercise that endpoint. Exit 2, not 1: a suite that could not RUN is a
# different fact from a suite that ran and found a bug, and a harness whose whole subject is
# not-conflating-those does not get to conflate them itself.
command -v jq >/dev/null || { echo "test-harness: jq is required" >&2; exit 2; }

# ── state ───────────────────────────────────────────────────────────────────────────────────────

n_pass=0
n_fail=0
n_fix=0
TMPROOT=""
FIX=""
OUT=""
RC=0

# `bad` labels the captured output. The gate suite runs a SCRIPT; the sweep suite runs a workflow
# STEP. Same block, different noun — so the noun is a knob, set before the first failure.
: "${HARNESS_OUTPUT_LABEL:=subject output}"

# harness_init — the temp root every fixture is built under, removed on exit.
harness_init() {
  TMPROOT=$(mktemp -d)
  trap 'rm -rf "$TMPROOT"' EXIT
}

# ── reporting ───────────────────────────────────────────────────────────────────────────────────

case_start() { printf '\n%s\n' "$1"; }

ok()  { n_pass=$((n_pass + 1)); printf '  ok    %s\n' "$1"; }
bad() { # bad <what> <why>
  n_fail=$((n_fail + 1))
  printf '  FAIL  %s\n' "$1"
  printf '        %s\n' "$2"
  printf '        ── %s (exit %s) ──\n' "$HARNESS_OUTPUT_LABEL" "$RC"
  printf '%s\n' "$OUT" | sed 's/^/        │ /'
  # Only when the run actually called `gh`. A suite that never touches the API gets no empty section,
  # and one that does gets the call log without having to ask for it.
  if [[ -n $FIX && -s $FIX/gh.log ]]; then
    printf '        ── gh calls ──\n'
    sed 's/^/        │ /' "$FIX/gh.log"
  fi
  printf '        ─────────────────────────────\n'
}

harness_summary() { # harness_summary <suite-name>
  printf '\n────────────────────────────────────────\n'
  if ((n_fail)); then
    printf '%s: FAILED — %d passed, %d failed\n' "$1" "$n_pass" "$n_fail"
    exit 1
  fi
  printf '%s: ok — %d assertions passed\n' "$1" "$n_pass"
}

# ── assertions ──────────────────────────────────────────────────────────────────────────────────
#
# The haystack is EXPLICIT in expect_has/expect_hasnt/expect_re. The sweep suite asserts over five
# different ones — the step's output, the rendered issue body, the job summary, the JSON payloads it
# PUT on the wire, and the gh call log — and an implicit `$OUT` there would quietly assert against
# the wrong one and pass. `expect_out_*` is the two-argument convenience for the common case, named
# so that the choice of haystack stays visible at the call site rather than being the default nobody
# reads.

expect_rc()    { if [[ $RC == "$1" ]];   then ok "$2"; else bad "$2" "expected exit $1, got $RC";     fi; }
expect_eq()    { if [[ "$1" == "$2" ]];  then ok "$3"; else bad "$3" "expected '$2', got '$1'";       fi; }
expect_has()   { if grep -qF -- "$1" <<<"$2"; then ok "$3"; else bad "$3" "text does not contain: $1"; fi; }
expect_hasnt() { if grep -qF -- "$1" <<<"$2"; then bad "$3" "text should NOT contain: $1"; else ok "$3"; fi; }
expect_re()    { if grep -qE -- "$1" <<<"$2"; then ok "$3"; else bad "$3" "no line matches /$1/";      fi; }

expect_out_has()   { expect_has   "$1" "$OUT" "$2"; }
expect_out_hasnt() { expect_hasnt "$1" "$OUT" "$2"; }

# ── what the run asked the API for ──────────────────────────────────────────────────────────────

# gh_called <METHOD> <path-regex> <what>
gh_called() {
  if awk -F'\t' -v m="$1" -v p="$2" '$1==m && $2 ~ p {found=1} END{exit !found}' "$FIX/gh.log"; then
    ok "$3"
  else
    bad "$3" "no $1 matching /$2/ in the gh log"
  fi
}
gh_not_called() {
  if awk -F'\t' -v m="$1" -v p="$2" '$1==m && $2 ~ p {found=1} END{exit !found}' "$FIX/gh.log"; then
    bad "$3" "unexpected $1 matching /$2/ in the gh log"
  else
    ok "$3"
  fi
}
# The whole point of a `pull_request` run: it renders, and it writes NOTHING.
gh_no_writes() {
  local w
  w=$(awk -F'\t' '$1=="POST" || $1=="PATCH" || $1=="PUT" || $1=="DELETE"' "$FIX/gh.log")
  if [[ -z $w ]]; then ok "$1"; else bad "$1" "the run made writes it must not make:"$'\n'"$w"; fi
}

# ── the fixture ─────────────────────────────────────────────────────────────────────────────────

# fixture_new — a fresh fixture tree: an empty skill root, a `bin/` holding the fake `gh`, and the
# state files the fake answers out of. A suite's own `fixture()` calls this and then installs its
# subject.
#
# COUNTED, not $RANDOM. $$ is fixed within a run, so $RANDOM was the only entropy — a 1-in-32768 draw
# taken ~20 times, i.e. a ~0.6% birthday collision every run, or one run in ~170. `mkdir -p` would
# then hand the second fixture the FIRST one's tree, so a case would inherit a skill body it never
# wrote and pass or fail on it. A flaky test in a suite whose entire product is trustworthiness is
# worse than no test at all, and it would decay exactly like the defects these suites exist to catch:
# rarely, and looking like something else.
fixture_new() {
  [[ -n $TMPROOT ]] || { echo "test-harness: call harness_init before the first fixture" >&2; exit 2; }
  n_fix=$((n_fix + 1))
  FIX="$TMPROOT/fix-$n_fix"
  mkdir -p "$FIX/scripts" "$FIX/bin" "$FIX/template/product-skills" \
           "$FIX/template/skill-manifest" "$FIX/gh-bodies"
  : >"$FIX/gh.log"
  : >"$FIX/gh-state"
  : >"$FIX/skills.tsv"                # id<TAB>supplied-by — the fixture's publish set
  echo '[]' >"$FIX/trackers.json"    # issues carrying DECAY_LABEL. Empty: no tracker exists yet.
  echo '[]' >"$FIX/unlabelled.json"  # issues that do NOT carry it — visible only if `labels=` is dropped
  # FS-GG/.github's coordination-kit roster (#723). check-skill-refs.sh verifies its `KIT_SKILLS`
  # constant against this, because that constant decides which bodies it does NOT examine and its
  # fail-open direction is a body of ours going unchecked. Defaults to the four real kit skills, so a
  # fixture that does not care about the kit gets a roster that AGREES with the subject and is silent.
  # `kit_roster` overrides it to drive the drift cases.
  printf '%s\n' cross-repo-coordination intra-repo-parallel-work check-board pnext-item >"$FIX/kit.txt"
  # FS-GG/.github's SKILL registry (#722) — the other constant check-skill-refs.sh narrows itself by.
  # `MIRRORED_SKILLS` names the bodies it treats HARDER (a bare `[[ref]]` in one is a hard failure), and
  # since #714 the omission of a mirror fails OPEN, so that list is verified against `owner:` here.
  #
  # This file holds the published ids the registry marks as owned ELSEWHERE. Empty by default, which is
  # the honest default: a fixture's skills are its own, so the registry calls them ours, the derived
  # mirror set is empty, and a fixture that does not care about mirrors is silent. `mirror_roster`
  # overrides it to drive the drift cases.
  : >"$FIX/mirrors.txt"
  _write_manifest                     # a real manifest, even when empty: the subject refuses to run without one
  _seed_repo_surface                  # ditto the REPO surface (#698) — see below
  _write_fake_gh
}

# The REPO surface, seeded into every fixture (#698). The subject checks TWO subjects now — the bodies
# it PUBLISHES (the manifest) and the bodies it merely READS here (`src/*/skill/`, and the authoring
# note under template/product-skills/) — and it REFUSES TO RUN without either one, for the same reason
# it refuses without a manifest: a subject that has gone missing is not "nothing to check", and passing
# green over it is the `.github#416` shape the whole gate exists to close.
#
# So a fixture needs a repo surface exactly as it needs a manifest, and for exactly as long as the
# subject is entitled to demand one. Seeded EMPTY-BUT-REAL: one vocabulary entry and one body with no
# refs in it. It contributes no findings, so every case that predates #698 keeps asserting precisely
# what it asserted before — and the surface is nonetheless PRESENT, so a case that wants to exercise it
# adds to it (`claude_skill`, `repo_skill`) rather than conjuring it.
#
# DO NOT "fix" a repo-surface failure by making the subject tolerate an absent one. That would trade a
# loud, correct refusal for a gate that silently stops checking ten skill bodies the day somebody moves
# them — which is the exact defect #698 was filed about, re-introduced through its own test harness.
_seed_repo_surface() {
  mkdir -p "$FIX/.claude/skills/fs-gg-fixture" "$FIX/src/Fixture/skill"
  printf -- '---\nname: fs-gg-fixture\n---\n\n# Fixture wrapper\n' \
    >"$FIX/.claude/skills/fs-gg-fixture/SKILL.md"
  printf -- '# Fixture library skill\n\nNo refs.\n' >"$FIX/src/Fixture/skill/SKILL.md"
}

# claude_skill <id> — add <id> to the REPO vocabulary: the set a `[[ref]]` in a repo-internal body
# resolves against (`.claude/skills/`). This is NOT the manifest, and the difference is the whole point
# of #698 — `[[fs-gg-ant-design]]` resolves HERE and dangles in a published body, and the same string
# can name a different body on each surface. A case that wants a repo ref to RESOLVE registers it here;
# one that wants it to DANGLE simply does not.
claude_skill() {
  mkdir -p "$FIX/.claude/skills/$1"
  printf -- '---\nname: %s\n---\n\n# %s\n' "$1" "$1" >"$FIX/.claude/skills/$1/SKILL.md"
}

# repo_skill <name> — a LIBRARY-facing body at src/<name>/skill/SKILL.md, content on stdin. Ships
# nowhere, so its reader is standing in this repo: its `[[refs]]` are judged against `.claude/skills/`,
# and its bare `#N` are RESOLVED against this repo rather than rejected (the subject's § 3 inversion).
repo_skill() {
  mkdir -p "$FIX/src/$1/skill"
  cat >"$FIX/src/$1/skill/SKILL.md"
}

# repo_readme — the authoring note at template/product-skills/README.md, content on stdin. A repo body
# like any other, and the one that must be able to WRITE the `[[…]]` syntax without INVOKING it.
repo_readme() { cat >"$FIX/template/product-skills/README.md"; }

# claude_body / agents_body <id> — a WRAPPER body with real content on stdin (#723). `claude_skill`
# above writes a ref-less stub, because its job is to put a NAME in the vocabulary; these write a body
# worth SCANNING, because since #723 the wrappers are a subject and not just a word list.
#
# TWO HELPERS, NOT ONE, and the duplication is the point: the two roots are NOT byte-identical in the
# real repo (the wrapper line reads "Claude-active" in one and "Codex-active" in the other), Codex reads
# `.agents/` and Claude reads `.claude/`, and a ref added to one is invisible in the other. A suite that
# could only write to one root could not tell a two-root subject from a one-root one — which is the
# defect these exist to make expressible.
claude_body() { mkdir -p "$FIX/.claude/skills/$1"; cat >"$FIX/.claude/skills/$1/SKILL.md"; }
agents_body() { mkdir -p "$FIX/.agents/skills/$1"; cat >"$FIX/.agents/skills/$1/SKILL.md"; }

# The fixture's skill-manifest — the PUBLISH SET the subject reads (Rendering's script asks the
# manifest, not the directory listing; see the MANIFEST note in check-skill-refs.sh).
#
# Rewritten on every `skill`/`skill_at`, because the subject reads it from DISK at run time and a
# fixture that registered a body without republishing would hand the subject a stale publish set —
# and the test would then pass or fail on a set the case never wrote.
_write_manifest() {
  {
    printf '{\n  "schemaVersion": 1,\n  "skills": [\n'
    local first=1 id dir
    while IFS=$'\t' read -r id dir; do
      [[ -z $id ]] && continue
      ((first)) || printf ',\n'
      first=0
      printf '    { "id": "%s", "scope": "product", "sha256": "0", "resolvablePath": ".agents/skills/%s/SKILL.md", "materializes-when": "always", "supplied-by": "%s" }' \
        "$id" "$id" "$dir"
    done <"$FIX/skills.tsv"
    printf '\n  ]\n}\n'
  } >"$FIX/template/skill-manifest/skill-manifest.json"
}

# gh_env — the env the fake `gh` answers out of. Every suite's runner must pass this through, or the
# fake logs to nowhere and reads its state from nowhere.
gh_env() {
  echo "PATH=$FIX/bin:$PATH"
  echo "GH_LOG=$FIX/gh.log"
  echo "GH_BODIES=$FIX/gh-bodies"
  echo "GH_STATE=$FIX/gh-state"
  echo "GH_TRACKERS=$FIX/trackers.json"
  echo "GH_UNLABELLED=$FIX/unlabelled.json"
  echo "GH_KIT=$FIX/kit.txt"
  echo "GH_MIRRORS=$FIX/mirrors.txt"
  echo "GH_PUBLISHED=$FIX/skills.tsv"
}

# skill <id> — the SKILL.md body on stdin. Publishes it from the conventional root, and REGISTERS it
# in the manifest, which is what makes it published as far as the subject is concerned.
skill() { skill_at "$1" "template/product-skills/$1"; }

# skill_at <id> <dir> — the SKILL.md body on stdin, supplied from an ARBITRARY directory.
#
# Not a convenience: it is the only way to write a fixture for the defect that a directory-scanning
# `is_published` cannot see. Rendering supplies four of its 21 skills from off-convention roots
# (fs-gg-project from template/base/.agents/skills/, and so on), and a scan of template/product-skills
# calls every one of them unpublished — reddening a CORRECT `[[fs-gg-project]]` with no green on the
# other side of it. A suite that could only build conventional fixtures could not state that case, so
# the bug would be untestable in exactly the suite written to prevent it.
skill_at() {
  local id=$1 dir=$2
  mkdir -p "$FIX/$dir"
  cat >"$FIX/$dir/SKILL.md"
  printf '%s\t%s\n' "$id" "$dir" >>"$FIX/skills.tsv"
  _write_manifest
}

# unpublished <id> <dir> — a body on disk that the manifest does NOT list. The inverse fixture: it
# proves the subject asks the MANIFEST and not the filesystem, so a stray directory under the skill
# root is not silently treated as published.
unpublished() { mkdir -p "$FIX/$2"; cat >"$FIX/$2/SKILL.md"; }

# issue <owner/repo#num> <open|closed> — link state. Anything not registered 404s as missing.
issue() { printf '%s\t%s\n' "$1" "$2" >>"$FIX/gh-state"; }

# kit_roster <id>… — FS-GG/.github's coordination-kit roster, as the fake serves it (#723). No args =
# an empty kit. Use it to drive KIT_SKILLS drift in BOTH directions: a name the subject excludes but
# the roster does not have (fail-OPEN — a body of ours going unexamined), and a name the roster has
# but the subject does not exclude (the kit grew, and the subject is resolving .github's numbers here).
kit_roster() { printf '%s\n' "$@" >"$FIX/kit.txt"; }

# mirror_roster <id>… — the PUBLISHED ids FS-GG/.github's skill registry says are owned ELSEWHERE, as
# the fake serves them (#722). No args = the fixture owns everything it publishes, which is the default.
#
# A frozen mirror is `owner: foreign` AND `we publish it` — the registry settles only the first half (its
# eight foreign product rows look identical, and Rendering mirrors four of them and ships no counterpart
# for the other four). So this helper marks OWNERSHIP, and the fixture's own `skill` calls decide the
# other half. That is what makes both drift directions expressible:
#
#   mirror_roster fs-gg-x  +  skill fs-gg-x   and fs-gg-x NOT in MIRRORED_SKILLS
#       -> the FAIL-OPEN direction: a mirror this gate does not know is one, so its bare refs are judged
#          against our publish set alone — green here, dangling in the owner's gate.
#   skill fs-gg-audio      +  no mirror_roster
#       -> the other direction: the constant claims a mirror the registry says we own. A loud false red.
mirror_roster() { printf '%s\n' "$@" >"$FIX/mirrors.txt"; }

# The repo's issues, as a flat JSON array on stdin. `trackers` carry DECAY_LABEL; `unlabelled` do not.
trackers()   { cat >"$FIX/trackers.json"; }
unlabelled() { cat >"$FIX/unlabelled.json"; }

# The one fake `gh`. It records every call and answers the endpoints these suites' subjects use.
#
# THE METHOD IS DERIVED, NOT DECLARED, and that is the most load-bearing line in this file. Real
# `gh api` sends POST the moment a field flag is present unless `-X` overrides it — the footgun that
# had #249's tracker LOOKUP POSTing to /issues, the create-issue endpoint, one missing field away
# from a daily blank-issue factory. Reproduce the footgun and a regression that drops the `-X GET`
# shows up as "the lookup was a POST"; hard-code the method and this harness would bless the bug it
# was written to catch.
#
# An unknown ref 404s rather than erroring, deliberately: check-skill-refs.sh retries an ERROR three
# times with 2s+4s backoff, so a typo'd fixture ref would cost six seconds of sleep and read as a
# hang. 404 is the honest answer for "no such issue" anyway — it is what the real API says.
_write_fake_gh() {
  cat >"$FIX/bin/gh" <<'FAKE_GH'
#!/usr/bin/env bash
case ${1:-} in
  auth) [[ ${GH_NOAUTH:-0} == 1 ]] && exit 1; exit 0 ;;
  api)  shift ;;
  *)    echo "fake gh: unsupported command: ${1:-}" >&2; exit 1 ;;
esac

raw="$*"
path=""; method=""; jqx=""; input=""; silent=0; has_field=0; slurp=0
fields=(); headers=()
while (($#)); do
  case $1 in
    -X|--method)      method=$2; shift 2 ;;
    -f|-F|--raw-field|--field) fields+=("$2"); has_field=1; shift 2 ;;
    --input)          input=$2; has_field=1; shift 2 ;;
    --jq)             jqx=$2; shift 2 ;;
    # `-H` takes a VALUE, so it must consume two words. It used to fall through to the `-*` catch-all,
    # which shifts ONE — leaving `Accept: …` to be read as a positional, i.e. very nearly as the PATH.
    # It survived only because the path was always already set by then. A fake that mis-parses the real
    # tool's argv is a fake that answers a question nobody asked.
    -H|--header)      headers+=("$2"); shift 2 ;;
    --silent)         silent=1; shift ;;
    --slurp)          slurp=1; shift ;;
    --paginate)       shift ;;
    -*)               shift ;;
    *)                [[ -z $path ]] && path=$1; shift ;;
  esac
done
# Did the caller ask for the RAW representation? `contents` is the one endpoint whose answer changes
# shape on this header: with it you get the file's bytes, without it a JSON object whose `.content` is
# base64. The fake honours the difference rather than serving one shape to both — see the roster case.
wants_raw=0
for h in "${headers[@]-}"; do
  [[ $h == *[Aa]ccept:*raw* ]] && wants_raw=1
done
# Exactly `gh`'s rule, and the reason `-X GET` is not decoration.
[[ -z $method ]] && { if ((has_field)); then method=POST; else method=GET; fi; }

body=""
[[ $input == - ]] && body=$(cat)

seq=$(( $(wc -l <"$GH_LOG") + 1 ))
printf '%s\t%s\t%s\t%s\n' "$method" "$path" "${fields[*]-}" "$raw" >>"$GH_LOG"
[[ -n $body ]] && printf '%s' "$body" >"$GH_BODIES/body-$seq.json"

resp=""
case "$method $path" in
  "GET repos/"*"/issues")
    # THE TRACKER LOOKUP, and the fake honours the query rather than ignoring it — because every
    # parameter on it is load-bearing, and a fake that returned the same list regardless would bless
    # a lookup that asks the API for entirely the wrong thing:
    #
    #   labels=   drop it and /issues returns EVERY issue in the repo — `first` is then some stranger,
    #             and the sweep PATCHes a decay report over it.
    #   state=all drop it and the API defaults to OPEN — a CLOSED tracker becomes invisible, so every
    #             regression files a SECOND tracker instead of reopening the first.
    #   direction= flip it and the NEWEST duplicate wins, stealing the thread from the original.
    #   --slurp   drop it and gh emits a flat array, so the caller's `.[][]` dies.
    want_label=""; want_state=open; dir=asc
    for f in "${fields[@]-}"; do
      case $f in
        labels=*)    want_label=${f#labels=} ;;
        state=*)     want_state=${f#state=} ;;
        direction=*) dir=${f#direction=} ;;
      esac
    done
    items=$(cat "$GH_TRACKERS")
    # Unrelated issues exist in every real repo. They are only invisible because of `labels=`.
    [[ -z $want_label ]] && items=$(jq -c -s 'add' "$GH_UNLABELLED" <(printf '%s' "$items"))
    [[ $want_state != all ]] && items=$(jq -c --arg s "$want_state" '[.[] | select(.state==$s)]' <<<"$items")
    items=$(jq -c 'sort_by(.number)' <<<"$items")          # issue number as a proxy for created-at
    [[ $dir == desc ]] && items=$(jq -c 'reverse' <<<"$items")
    # `--paginate --slurp` makes gh emit an array OF PAGES, which is why the caller flattens with
    # `.[][]`. Without --slurp it is a flat array and that flatten dies — so the nesting belongs here,
    # in the fake, not in the fixture.
    #
    # `--paginate` itself is NOT modelled: the real thing only bites past a 30-item page, and a
    # 31-tracker fixture would be theatre. It is asserted on the raw argv instead, and that
    # difference is stated rather than hidden.
    if ((slurp)); then resp=$(jq -c '[.]' <<<"$items"); else resp=$items; fi ;;
  "GET repos/"*"/labels/"*)
    [[ ${GH_LABEL_EXISTS:-0} == 1 ]] || { echo "gh: Not Found (HTTP 404)" >&2; exit 1; }
    resp='{"name":"label"}' ;;
  "POST repos/"*"/labels")
    [[ ${GH_LABEL_POST_FAILS:-0} == 1 ]] && { echo "gh: Validation Failed (HTTP 422) already_exists" >&2; exit 1; }
    resp='{"name":"label"}' ;;
  "POST repos/"*"/issues")
    n=${GH_NEW_NUM:-777}
    resp="{\"number\":$n,\"html_url\":\"https://github.com/${GH_REPO:-OWNER/REPO}/issues/$n\"}" ;;
  "PATCH repos/"*"/issues/"*)
    resp='{"ok":true}' ;;
  "POST repos/"*"/issues/"*"/comments")
    resp='{"ok":true}' ;;
  "GET repos/"*"/issues/"*)
    # check-skill-refs.sh resolving a link's state (`--jq .state`). Unregistered → 404.
    #
    # The response is a real JSON object and `--jq` is really applied to it. The gate suite's old fake
    # printed the bare state and ignored `--jq` entirely, which meant it would have answered `open` to
    # a subject asking for `.stat`, `.title`, or nothing at all. That fake could not fail a wrong
    # question, so it never asked one.
    k=${path#repos/}; owner=${k%%/*}
    r=${k#*/};        repo=${r%%/*}
    num=${path##*/}
    st=$(awk -F'\t' -v k="$owner/$repo#$num" '$1==k {print $2; exit}' "$GH_STATE")
    case $st in
      open|closed) resp="{\"state\":\"$st\"}" ;;
      *) echo "gh: Not Found (HTTP 404)" >&2; exit 1 ;;
    esac ;;
  "GET repos/FS-GG/.github/contents/registry/repos.yml")
    # GH_KIT_FAIL — the request itself fails (rate limit, 403, network), as opposed to succeeding and
    # returning a roster the subject cannot parse. These are DIFFERENT states and the subject must not
    # conflate them: it is the .github#430 defect ("blames the wrong thing") that this models.
    [[ ${GH_KIT_FAIL:-0} == 1 ]] && { echo "gh: API rate limit exceeded (HTTP 403)" >&2; exit 1; }
    # THE COORDINATION-KIT ROSTER (#723). `GH_KIT` holds the kit skill ids, one per line; an EMPTY file
    # is a roster with no `kind: skill` kit rows at all, which is a real state the subject must refuse
    # over rather than read as "the kit is empty". No special-case branch for it: the loop below simply
    # contributes no rows, which is exactly what an empty roster means.
    #
    # AND IT IS A FAKE THAT CAN BE WRONG — three ways, deliberately:
    #
    #  * TWO DECOYS. `kind: skill` rows that are NOT kit rows, one BEFORE the `kit:` block and one
    #    AFTER it. A parse that greps the whole file picks up the first; one that enters the block and
    #    never leaves picks up the second. Either way the drift check reddens instead of passing.
    #  * A `kind: client` ROW INSIDE the block. `fsgg-coord` is a genuine kit row, but it is a script,
    #    not a body — a parse that takes every kit row would exclude a skill that does not exist.
    #  * THE ACCEPT HEADER IS HONOURED. Ask for raw and you get the file's bytes; ask without it and
    #    you get the real API's JSON envelope, whose `.content` is base64. A subject that forgot the
    #    header gets an envelope it cannot parse as YAML, which is precisely what the real API would
    #    hand it. Serving raw to both would be a fake that cannot be wrong about the one line that
    #    decides the response shape.
    rows=""
    while IFS= read -r id; do
      [[ -z $id ]] && continue
      rows+="  - { id: $id, kind: skill,  source: .claude/skills/$id }"$'\n'
    done <"${GH_KIT:-/dev/null}"
    yaml=$'repos:\n  - { id: rendering, full: FS-GG/FS.GG.Rendering, role: framework }\nbefore-kit:\n  - { id: decoy-before, kind: skill, source: nowhere }\nkit:\n'"$rows"$'  - { id: fsgg-coord, kind: client, source: scripts/fsgg-coord }\nafter-kit:\n  - { id: decoy-after, kind: skill, source: nowhere }\n'
    if ((wants_raw)); then
      resp=$yaml
    else
      resp=$(jq -nc --arg c "$(printf '%s' "$yaml" | base64 | tr -d '\n')" '{content:$c}')
    fi ;;
  "GET repos/FS-GG/.github/contents/registry/skills.yml")
    # THE SKILL REGISTRY (#722). check-skill-refs.sh verifies `MIRRORED_SKILLS` against it, because that
    # list decides which bodies get the STRICTER bare-ref rule and — since #714 inverted its polarity —
    # a mirror MISSING from it fails OPEN.
    #
    # GH_MIRRORS_FAIL — the request itself fails, as opposed to succeeding and returning a registry the
    # subject cannot parse. Different states, and the subject must not conflate them (.github#430).
    [[ ${GH_MIRRORS_FAIL:-0} == 1 ]] && { echo "gh: API rate limit exceeded (HTTP 403)" >&2; exit 1; }
    # GH_MIRRORS_EMPTY — the bytes arrive and carry no `scope: product` row at all. That is the registry's
    # SHAPE moving under the parse, and it must be refused rather than read as "there are no mirrors":
    # blessing the constant on a match that found nothing is the fail-open the verification exists to
    # close. It is NOT the same as an empty MIRROR set, which is legal (#696's end state).
    #
    # A MIRROR IS `owner: foreign` AND `we publish it`, and only the first half is the registry's to say —
    # so the rows are built from the fixture's OWN publish set, exactly as the real registry is reconciled
    # from the producer manifests. `GH_MIRRORS` names the published ids that are owned elsewhere.
    #
    # AND IT IS A FAKE THAT CAN BE WRONG — three ways, deliberately:
    #
    #  * THE FOUR NEVER-MIRRORED FOREIGN ROWS. ballistics/ai/effects/physics are `owner: fs-gg-game`
    #    product rows that Rendering deliberately ships NO counterpart for (.github#486). They are always
    #    served, and they are indistinguishable in the registry from the four we DO mirror. A derivation
    #    that takes every foreign product row — instead of intersecting with the publish set — picks these
    #    up and reddens.
    #  * A `scope: process` ROW WITH A FOREIGN OWNER. A parse that ignores `scope:` derives it as a mirror.
    #    Its id is publishable, so a fixture can put it in the publish set and make that bug fire.
    #  * THE ACCEPT HEADER IS HONOURED, as on the roster: raw gives bytes, its absence gives the JSON
    #    envelope whose `.content` is base64. A subject that forgot the header gets exactly what the real
    #    API would hand it.
    rows=""
    if [[ ${GH_MIRRORS_EMPTY:-0} != 1 ]]; then
      while IFS=$'\t' read -r id _dir; do
        [[ -z $id ]] && continue
        owner=fs-gg-rendering
        grep -qxF -- "$id" "${GH_MIRRORS:-/dev/null}" 2>/dev/null && owner=fs-gg-game
        rows+="  - { id: $id, scope: product, owner: $owner, source: FS.GG.X/template/product-skills/$id/SKILL.md, sha256: 0 }"$'\n'
      done <"${GH_PUBLISHED:-/dev/null}"
      for ns in fs-gg-ballistics fs-gg-ai fs-gg-effects fs-gg-physics; do
        rows+="  - { id: $ns, scope: product, owner: fs-gg-game, source: FS.GG.Game/template/product-skills/$ns/SKILL.md, sha256: 0 }"$'\n'
      done
      rows+="  - { id: fs-gg-decoy-process, scope: process, owner: fs-gg-game, source: FS.GG.Game/x/SKILL.md, sha256: 0 }"$'\n'
    fi
    yaml=$'schemaVersion: 1\nupdated: 2026-07-13\nskills:\n'"$rows"
    if ((wants_raw)); then
      resp=$yaml
    else
      resp=$(jq -nc --arg c "$(printf '%s' "$yaml" | base64 | tr -d '\n')" '{content:$c}')
    fi ;;
  *)
    echo "fake gh: unmodelled endpoint: $method $path" >&2; exit 1 ;;
esac

if [[ -n $jqx ]]; then printf '%s' "$resp" | jq -r "$jqx"
elif ((silent));  then :
else                   printf '%s' "$resp"
fi
exit 0
FAKE_GH
  chmod +x "$FIX/bin/gh"
}
