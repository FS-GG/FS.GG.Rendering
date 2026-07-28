#!/usr/bin/env bash
# materialize-skill-roots.sh — project this repository's agent-skill union into ADR-0011's three roots.
#
#   scripts/materialize-skill-roots.sh             # apply: write the union into every derived root
#   scripts/materialize-skill-roots.sh --check     # verify only; exit 1 on drift. No writes.
#   scripts/materialize-skill-roots.sh --list      # print the attributed union and exit 0
#
# WHY THIS EXISTS (FS.GG.Rendering#1080 / #1082 / .github#1504)
#
# ADR-0011 requires every agent-skill root — `.claude/skills`, `.codex/skills`, `.agents/skills` — to
# hold the BYTE-IDENTICAL UNION of the skills produced for this repo, so the Claude, Codex and generic
# agent runtimes are interchangeable. This repo did not hold it. Measured on `main` (ee5e6c3):
#
#   union 50 — .claude 50 / .codex 4 / .agents 50
#   46 PARTITIONED (absent from .codex), and — independently — 30 of the 50 DIVERGENT between
#   .claude and .agents.
#
# The two defects are distinct and the second was invisible: `skill-union-assert.sh` short-circuits the
# byte comparison for a skill that is `[partitioned]`, so its summary read `present=4 byte-identical=4`
# and looked like "nothing is divergent" while 46 ids went uncompared (.github#1506 corrected that
# summary to print denominators).
#
# 29 of the 30 differed on ONE line — `This is the Claude-active wrapper …` vs `… Codex-active wrapper …`
# — and `fs-gg-ant-design` differed wholly: `.claude/skills/fs-gg-ant-design/SKILL.md` held the CANONICAL
# BODY while `.agents/` held a wrapper routing INTO it, which is a shape no byte-identical union can
# contain (a root cannot route to itself).
#
# FS.GG.Rendering#1082 decided this (human call, 2026-07-27): **the byte-identical three-root union is
# correct, and the per-surface wrappers are legacy drift to be removed.** So:
#
#   * the surface self-identification was dropped from the wrapper bodies — `This is the Claude-active
#     wrapper for X.` / `This is the Codex-active wrapper for X.` both became `This is a wrapper for X.`
#     The three sentence CLASSES (`canonical local skill`, `generated-product skill variant`,
#     `canonical generated-product skill`) are preserved verbatim: only the surface word was drift.
#   * the Ant canonical body MOVED OUT of `.claude/skills/` to `docs/product/ant-design/skill/SKILL.md`
#     — the `<area>/skill/SKILL.md` convention `src/*/skill/` and `template/*/skill/` already use — and
#     `fs-gg-ant-design` became an ordinary wrapper routing there, identical in all three roots.
#
# WHY NOT `cp -R .agents/skills .codex/skills`
#
# It produces the same bytes TODAY and reproduces the defect on the next producer change, because nothing
# then knows the roots are DERIVED. This script gates the projection on each skill's PRODUCER, so a
# producer change is detected rather than laundered, and an id nobody produces is REFUSED rather than
# fanned out.
#
# THE PRODUCER AUTHORITIES, AND WHAT BINDS EACH SKILL
#
# There is no single producer for this tree, and saying so precisely is half the point. #1080's criterion
# 2 named two, and BOTH were misidentified (established by `curlew-9840`, re-confirmed here):
#
#   * `scripts/generate-skill-manifest.fsx` + `template/skill-manifest/skill-manifest.json` produce the
#     **fs-gg-ui TEMPLATE's** 18-row product catalog — different ids (`fs-gg-collision`, not this repo's
#     `fs-gg-product-collision`) — and the script writes exactly ONE file, the manifest. It writes no
#     skill root and is not an authority for one.
#   * `template/lifecycle/materialize-skill-roots.fsx` IS an ADR-0014 mirror, but its own header scopes it
#     to the standalone spec-kit lane; it is driven by `FsGgMaterializeSkillRoots` in
#     `template/base/Directory.Build.props`, which this repo's own `Directory.Build.props` does not carry;
#     and its verification input `.agents/skills/skill-manifest.json` does not exist here. Run against
#     this repo it would mirror `.agents/skills` over `.claude/skills` unconditionally.
#
# So the authorities are per-class, and every id in the union falls into exactly one class:
#
#   1. REPO-NATIVE (`fs-gg-*`, 30, plus `speckit-merge`) — authority: THIS repo. The bodies are authored
#      here, in `.agents/skills`, which ADR-0011 §3 confines a provider to. They are NOT generated: their
#      H1 titles and (for the 15 `fs-gg-product-*`) their front-matter are deliberately NOT derivable from
#      the canonical body they route to — measured, 29 H1 mismatches and 16 front-matter mismatches — so a
#      generator would have to invent them. `.agents/skills` is therefore the SOURCE and is never written
#      by this script. What IS checked, for every `fs-gg-*` wrapper, is that its route RESOLVES: the
#      canonical body it names must exist. That is the bond a wrapper exists to express, and it is exactly
#      what rots silently.
#   2. SPEC-KIT, base integration (9) — authority: `.specify/integrations/claude.manifest.json`, the
#      content-addressed record of the integration's installation. The integration id is `claude` and its
#      manifest's paths are `.claude/skills/...`, so `.claude/skills` is the PRODUCER'S OWN OUTPUT ROOT —
#      not an arbitrary donor — and this script derives that root from those paths rather than hardcoding
#      it. Checked: sha256(SKILL.md) equals the declared digest.
#   3. SPEC-KIT, extension-provided (6) — authority: `.specify/extensions/.registry`, which is what says an
#      extension is ENABLED and which commands it registered (`speckit.git.*` → `speckit-git-*`,
#      `speckit.agent-context.update` → `speckit-agent-context-update`). These are NOT in
#      claude.manifest.json: they arrive via the extension, not the base integration.
#   4. The four KIT skills — authority: the pinned `FS.GG.Kit`, materialized by `coordination-sync` /
#      `.config/kit/` and already bound to canonical, in every root THE KIT WRITES, by the required
#      `coordination-coherence` gate. That is TWO roots, not this repo's three, since ADR-0067 §5
#      retired `.codex/skills` — see §THE KIT'S ROOT SET IS NOT THIS REPO'S below.
#
# An id in no class is UNATTRIBUTED: reported, and refused rather than projected. That is the property
# `cp -R` cannot have.
#
# THE KIT SKILLS ARE VERIFIED HERE, NEVER WRITTEN (a deliberate divergence from Governance's precedent)
#
# FS.GG.Governance#326's script projects its kit skills as part of the union, calling it "a no-op while
# that gate is green". This script instead VERIFIES them (present in every root, byte-identical) and
# writes none of them. The reason is a failure mode, not taste: those bytes are owned by an EXTERNAL
# producer that writes all three roots itself. If the kit receiver lands an update in one root and this
# script is allowed to write them, `--check` goes red and the mechanical "fix" is to overwrite the kit's
# update from a stale root — laundering a real signal into a wrong repair. Verifying instead keeps the
# red (the union genuinely is incoherent) while leaving the only correct repair — re-run the kit receiver —
# the obvious one. This script refuses to write bytes it does not own.
#
# THE KIT'S ROOT SET IS NOT THIS REPO'S (FS.GG.Rendering#1088, .github#1636, ADR-0065 §Retiring a root)
#
# ADR-0065's ordered root set was ADR-0011's three until 2026-07-28, when ADR-0067 §5 retired
# `.codex/skills`: `.agents/skills` is Codex's OWN second native root, so `.codex/skills` carried no
# runtime the remaining two do not, and only ever produced a duplicate catalog entry. `FS.GG.Kit`
# 0.15.0 declares that root RETIRED (`FsggKitRetiredSkillRoots`) and its materializer sweeps the kit's
# OWN four directories out of it on the receiver's next restore.
#
# That sweep is the whole of the retirement on a receiver. It removed exactly `.codex/skills/{check-board,
# cross-repo-coordination,intra-repo-parallel-work,pnext-item}` here and touched nothing else: this repo's
# OWN 46 skills stay in `.codex/skills`, because §Retiring a root withdraws the KIT's copies from a
# retired root — it does not empty the root, and this script must not either.
#
# So a kit skill is now expected in TWO roots while every other class is expected in this repo's three,
# and conflating those two facts is exactly what broke. Before this change the kit class was verified
# across `$ROOTS`; the moment the pin moved to 0.15.0 this script reported
# `[kit-partitioned] <id> absent from .codex/skills — re-run the kit receiver` for all four, blaming the
# receiver for a sweep the receiver had just performed correctly, and pointing the reader at a repair
# (re-run the receiver) that reproduces the same state. The kit's output was right; the expectation was stale.
#
# The kit's set is READ from `scripts/skill-view`, which is itself kit-materialized: it is the pinned
# kit's own statement of its root set, sitting in this tree, moving with the pin. Restating
# `.claude/skills .agents/skills` here instead would be the second source of truth that `lib/roots.sh`'s
# own header warns about — it would agree with the kit today and diverge silently the next time the
# kit's set moves, in the direction that blames THIS tree for the kit's change. Absent or unparseable is
# a hard error, never a fallback to `$ROOTS`: falling back is how the stale expectation got here (#266).
#
# The retired root is not merely skipped. A kit skill that SURVIVES in a root the kit does not write is
# an unswept leftover — reported as `[kit-leftover]`, repaired by re-running the materializer, never by
# this script, which deletes no bytes it does not own any more than it writes them.
#
# THE ONE OVERRIDDEN SPEC-KIT ROW, RECORDED RATHER THAN SKIPPED
#
# `speckit-implement`'s on-disk body does NOT match its manifest digest, and that is correct: commit
# 73db0491 (Feature 168, "skill-parity-evidence") appended a `## Repository Evidence Rules (Feature 168)`
# section to the installed body. The manifest is an INSTALL RECEIPT, not a desired state — rewriting its
# digest would make the receipt lie and discard the only tamper baseline that file has — so the row is
# annotated here instead. The check is not skipped: it is REPLACED by one that fails if the override is
# ever lost (re-install, clobber), which a digest comparison could not distinguish from a routine update.
# This repo has no `.specify/presets/`, so unlike Governance the override lives in the commit history
# rather than in a preset command file, and there is no producer body to re-derive it from.

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SPECIFY="$REPO_ROOT/.specify"
CLAUDE_MANIFEST="$SPECIFY/integrations/claude.manifest.json"
EXT_REGISTRY="$SPECIFY/extensions/.registry"
DEFAULT_ROOTS=".claude/skills .codex/skills .agents/skills"

# The repo-native provider root: ADR-0011 §3 confines a provider to `.agents/skills/`.
NATIVE_SOURCE=".agents/skills"

# Externally-owned, written by coordination-sync and gated by `coordination-coherence`. Named here to
# ATTRIBUTE them, not to check them — duplicating that gate here would be a restatement that can
# disagree with it.
KIT_SKILLS="cross-repo-coordination intra-repo-parallel-work check-board pnext-item"

# Spec-kit rows whose on-disk body intentionally differs from the install receipt. id -> marker that
# proves the override is still in force.
OVERRIDE_IDS="speckit-implement"
OVERRIDE_MARKER_speckit_implement="## Repository Evidence Rules (Feature 168)"

die()  { echo "materialize-skill-roots: $*" >&2; exit 2; }
note() { echo "materialize-skill-roots: $*"; }

usage() { awk 'NR == 1 { next } /^#/ { sub(/^# ?/, ""); print; next } { exit }' "$0"; }

mode="apply"
while [ $# -gt 0 ]; do
  case "$1" in
    --check)   mode="check"; shift ;;
    --list)    mode="list";  shift ;;
    -h|--help) usage; exit 0 ;;
    *)         die "unknown argument '$1'. Try --help." ;;
  esac
done

command -v python3 >/dev/null 2>&1 || {
  echo "materialize-skill-roots: python3 is required (reads the producer's JSON manifest and registry)." >&2
  echo "  No PyYAML or other module is needed — every producer declaration this reads is JSON." >&2
  exit 3
}

[ -d "$SPECIFY" ]         || die "no .specify/ in $REPO_ROOT — this repo has no spec-kit producer to drive."
[ -f "$CLAUDE_MANIFEST" ] || die "producer manifest not found: $CLAUDE_MANIFEST"

# ---------------------------------------------------------------------------------------------------
# Roots. Same precedence as coordination-sync / skill-union-assert.sh: $AGENT_SKILL_ROOTS, else a
# checked-in .agent-skill-roots, else ADR-0011's three. An ABSENT root is a hard error at every level —
# declaring roots narrows what is asked for, it never weakens the answer (.github#517 / #266).
# ---------------------------------------------------------------------------------------------------
if [ -n "${AGENT_SKILL_ROOTS:-}" ]; then
  ROOTS="$AGENT_SKILL_ROOTS"; ROOTS_SRC="\$AGENT_SKILL_ROOTS"
elif [ -f "$REPO_ROOT/.agent-skill-roots" ]; then
  ROOTS="$(sed 's/#.*$//' "$REPO_ROOT/.agent-skill-roots" | tr '\n\t\r' '   ' | tr -s ' ' | sed 's/^ *//; s/ *$//')"
  ROOTS_SRC=".agent-skill-roots"
  [ -n "$ROOTS" ] || die ".agent-skill-roots parses to nothing — a tree that checked it in meant to say something."
else
  ROOTS="$DEFAULT_ROOTS"; ROOTS_SRC="default (ADR-0011's three)"
fi

# The spec-kit producer's output root, derived from the manifest's own paths rather than hardcoded.
SPECKIT_SOURCE="$(python3 - "$CLAUDE_MANIFEST" <<'PY'
import json, sys, posixpath
m = json.load(open(sys.argv[1], encoding="utf-8"))
roots = set()
for p in m.get("files", {}):
    parts = p.split("/")
    if len(parts) >= 3 and parts[1] == "skills":
        roots.add(posixpath.join(parts[0], parts[1]))
if len(roots) != 1:
    sys.stderr.write(f"expected exactly one skill output root in the manifest, found {sorted(roots)}\n")
    sys.exit(2)
print(roots.pop())
PY
)" || die "could not derive the spec-kit output root from $CLAUDE_MANIFEST"

for r in $ROOTS; do
  [ -d "$REPO_ROOT/$r" ] || die "configured root is absent: $r (roots from $ROOTS_SRC)"
done
for s in "$NATIVE_SOURCE" "$SPECKIT_SOURCE"; do
  case " $ROOTS " in
    *" $s "*) ;;
    *) die "source root $s is not in the root set ($ROOTS, from $ROOTS_SRC) — refusing to project bytes
  into roots while the tree that produces them is not itself asserted." ;;
  esac
done

# ---------------------------------------------------------------------------------------------------
# The KIT's root set — read from the pinned kit's own materialized declaration, never restated here.
# See §THE KIT'S ROOT SET IS NOT THIS REPO'S in the header for why this is a separate set at all.
# ---------------------------------------------------------------------------------------------------
KIT_ROOTS_DECL="$SCRIPT_DIR/skill-view"
[ -f "$KIT_ROOTS_DECL" ] || die "cannot determine the kit's root set: $KIT_ROOTS_DECL is absent.
  It is materialized from the pinned FS.GG.Kit — run
  \`dotnet build .config/kit/FS.GG.Kit.receiver.proj -t:FsggKitMaterialize\`. Refusing to fall back to
  this repo's own root set: that substitution is exactly what .github#1636 turned into a false red."
KIT_ROOTS="$(sed -n 's/^DEFAULT_ROOTS="\([^"]*\)".*/\1/p' "$KIT_ROOTS_DECL" | head -n 1)"
[ -n "$KIT_ROOTS" ] || die "could not read DEFAULT_ROOTS from $KIT_ROOTS_DECL — the kit's root-set
  declaration moved. Refusing to guess."

# Roots this repo asserts that the kit does NOT write. A kit skill here is an unswept leftover.
KIT_NONROOTS=""
for r in $ROOTS; do
  case " $KIT_ROOTS " in *" $r "*) ;; *) KIT_NONROOTS="$KIT_NONROOTS $r" ;; esac
done
for r in $KIT_ROOTS; do
  case " $ROOTS " in
    *" $r "*) ;;
    *) die "the kit writes $r, which is not in this repo's root set ($ROOTS, from $ROOTS_SRC) — kit
  bytes would land in a root nothing here asserts." ;;
  esac
done

note "roots: $ROOTS (from $ROOTS_SRC)"
note "kit roots:          $KIT_ROOTS (from $(basename "$KIT_ROOTS_DECL"), materialized by the pinned FS.GG.Kit)"
note "repo-native source: $NATIVE_SOURCE (ADR-0011 §3 provider root)"
note "spec-kit source:    $SPECKIT_SOURCE (derived from $(basename "$CLAUDE_MANIFEST"))"

# ---------------------------------------------------------------------------------------------------
# Attribute every id in the union, and verify it against its producer authority.
# Emits TSV: <id>\t<class>\t<source-root>\t<verdict>\t<detail>
# ---------------------------------------------------------------------------------------------------
ATTRIB="$(python3 - "$REPO_ROOT" "$NATIVE_SOURCE" "$SPECKIT_SOURCE" "$CLAUDE_MANIFEST" "$EXT_REGISTRY" \
                   "$KIT_SKILLS" "$OVERRIDE_IDS" "$ROOTS" <<'PY'
import hashlib, json, os, re, sys

repo, native_src, speckit_src, manifest_p, ext_registry, kit_s, override_s, roots_s = sys.argv[1:9]
kit = set(kit_s.split())
overrides = set(override_s.split())
roots = roots_s.split()

OVERRIDE_MARKERS = {"speckit-implement": "## Repository Evidence Rules (Feature 168)"}

def ids_in(root):
    d = os.path.join(repo, root)
    return {n for n in os.listdir(d) if os.path.isdir(os.path.join(d, n))} if os.path.isdir(d) else set()

union = set()
for r in roots:
    union |= ids_in(r)

def sha(p):
    with open(p, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()

# --- producer declarations -------------------------------------------------------------------------
manifest = json.load(open(manifest_p, encoding="utf-8"))
declared = {}
for p, d in manifest.get("files", {}).items():
    parts = p.split("/")
    if len(parts) == 4 and parts[1] == "skills" and parts[3] == "SKILL.md":
        declared[parts[2]] = d if isinstance(d, str) else (d.get("sha256") or "")

ext_ids = set()
if os.path.isfile(ext_registry):
    reg = json.load(open(ext_registry, encoding="utf-8"))
    for ext_id, ext in (reg.get("extensions") or {}).items():
        if not ext.get("enabled", False):
            continue
        for _agent, cmds in (ext.get("registered_commands") or {}).items():
            for c in cmds:
                # `speckit.git.commit` -> `speckit-git-commit`
                ext_ids.add(c.replace(".", "-"))

rows = []
for sid in sorted(union):
    if sid in kit:
        # Externally owned. Verified below (presence + cross-root identity), never written.
        rows.append((sid, "kit", "-", "ok", "FS.GG.Kit / coordination-coherence"))
        continue

    if sid in declared or sid in ext_ids:
        src = speckit_src
        body = os.path.join(repo, src, sid, "SKILL.md")
        if not os.path.isfile(body):
            rows.append((sid, "spec-kit", src, "MISSING", f"declared but absent from {src}"))
            continue
        if sid in overrides:
            marker = OVERRIDE_MARKERS.get(sid, "")
            text = open(body, encoding="utf-8").read()
            if marker and marker in text:
                rows.append((sid, "spec-kit", src, "ok", f"repo-local override in force ({marker!r})"))
            else:
                rows.append((sid, "spec-kit", src, "OVERRIDE-LOST",
                             f"recorded override marker is gone: {marker!r}"))
            continue
        if sid in declared:
            got = sha(body)
            if got == declared[sid]:
                rows.append((sid, "spec-kit", src, "ok", "manifest digest matches"))
            else:
                rows.append((sid, "spec-kit", src, "DRIFTED",
                             f"declared {declared[sid][:16]}… on-disk {got[:16]}…"))
        else:
            rows.append((sid, "spec-kit", src, "ok", "registered by an enabled extension"))
        continue

    # Repo-native: authored in the provider root.
    src = native_src
    body = os.path.join(repo, src, sid, "SKILL.md")
    if not os.path.isfile(body):
        rows.append((sid, "UNATTRIBUTED", "-", "REFUSED",
                     f"no producer declares it and {src}/{sid}/SKILL.md does not exist"))
        continue
    text = open(body, encoding="utf-8").read()
    detail = "repo-authored"
    verdict = "ok"
    if sid.startswith("fs-gg-"):
        m = re.search(r"`(\.\./\.\./\.\./[^`]+)`", text)
        if not m:
            verdict, detail = "NO-ROUTE", "a fs-gg-* wrapper must name the canonical body it routes to"
        else:
            target = m.group(1)[len("../../../"):]
            if os.path.exists(os.path.join(repo, target)):
                detail = f"routes to {target}"
            else:
                verdict, detail = "DANGLING-ROUTE", f"canonical body does not exist: {target}"
    rows.append((sid, "repo-native", src, verdict, detail))

for r in rows:
    print("\t".join(r))
PY
)" || die "attribution failed"

echo "$ATTRIB" | while IFS="$(printf '\t')" read -r id cls src verdict detail; do
  [ "$verdict" = "ok" ] || echo "materialize-skill-roots: [$verdict] $id ($cls): $detail" >&2
done

BAD="$(echo "$ATTRIB" | awk -F'\t' '$4 != "ok"' | wc -l | tr -d ' ')"
[ "$BAD" = "0" ] || die "$BAD skill(s) failed producer attribution/verification (above). Refusing to project."

if [ "$mode" = "list" ]; then
  printf '%s\n' "id	class	source	verdict	detail"
  echo "$ATTRIB"
  exit 0
fi

# ---------------------------------------------------------------------------------------------------
# Project each attributed skill from its source root into every other root. Kit skills are verified,
# never written (see the header).
# ---------------------------------------------------------------------------------------------------
# `while read` in a pipeline runs in a subshell, so counters set there would be lost. Use a temp file.
COUNTS="$(mktemp)"
trap 'rm -f "$COUNTS"' EXIT
echo "0 0" > "$COUNTS"

echo "$ATTRIB" | {
  drift=0; changed=0
  while IFS="$(printf '\t')" read -r id cls src verdict detail; do
    [ -n "$id" ] || continue
    if [ "$cls" = "kit" ]; then
      # Verify only: present in every root THE KIT WRITES, byte-identical across those, and swept from
      # every root it does not. Never written or deleted here (see the header).
      first=""
      for r in $KIT_ROOTS; do
        d="$REPO_ROOT/$r/$id"
        if [ ! -d "$d" ]; then
          echo "materialize-skill-roots: [kit-partitioned] $id absent from $r — re-run the kit receiver" >&2
          drift=$((drift + 1)); continue
        fi
        if [ -z "$first" ]; then first="$d"; elif ! diff -qr "$first" "$d" >/dev/null 2>&1; then
          echo "materialize-skill-roots: [kit-divergent] $id differs between roots — re-run the kit receiver" >&2
          drift=$((drift + 1))
        fi
      done
      for r in $KIT_NONROOTS; do
        if [ -d "$REPO_ROOT/$r/$id" ]; then
          echo "materialize-skill-roots: [kit-leftover] $id survives in $r, a root the kit does not write — re-run the kit receiver, which sweeps it" >&2
          drift=$((drift + 1))
        fi
      done
      continue
    fi
    SRC_DIR="$REPO_ROOT/$src/$id"
    for r in $ROOTS; do
      [ "$r" = "$src" ] && continue
      DST_DIR="$REPO_ROOT/$r/$id"
      if [ "$mode" = "check" ]; then
        if [ ! -d "$DST_DIR" ] || ! diff -qr "$SRC_DIR" "$DST_DIR" >/dev/null 2>&1; then
          echo "materialize-skill-roots: [drift] $r/$id differs from $src/$id" >&2
          drift=$((drift + 1))
        fi
      else
        if [ ! -d "$DST_DIR" ] || ! diff -qr "$SRC_DIR" "$DST_DIR" >/dev/null 2>&1; then
          rm -rf "$DST_DIR"
          cp -R "$SRC_DIR" "$DST_DIR"
          changed=$((changed + 1))
        fi
      fi
    done
  done
  echo "$drift $changed" > "$COUNTS"
}
read -r drift changed < "$COUNTS"

# An id present in a root that the attributed union does not contain is a leftover projection: it would
# survive a producer REMOVING a skill, which is the regression this whole apparatus exists to catch.
ATTRIB_IDS="$(echo "$ATTRIB" | cut -f1 | sort -u)"
for r in $ROOTS; do
  for d in "$REPO_ROOT/$r"/*/; do
    [ -d "$d" ] || continue
    id="$(basename "$d")"
    if ! echo "$ATTRIB_IDS" | grep -qx "$id"; then
      echo "materialize-skill-roots: [orphan] $r/$id is in no producer's declared set" >&2
      drift=$((drift + 1))
    fi
  done
done

total="$(echo "$ATTRIB" | grep -c . || true)"
if [ "$mode" = "check" ]; then
  if [ "$drift" -ne 0 ]; then
    die "$drift drift finding(s) across $total attributed skill(s) — run scripts/materialize-skill-roots.sh"
  fi
  note "$total skill(s) attributed; every root is the byte-identical union. No drift."
else
  [ "$drift" -eq 0 ] || die "$drift finding(s) this script must not repair (above)."
  note "$total skill(s) attributed; $changed projection(s) written."
fi
