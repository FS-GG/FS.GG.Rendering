# Phase 0 Research: Lifecycle Choice Symbol

All Technical Context items resolved from the existing template and the Feature 128
(`designSystem`) / `feedback` precedents already in `.template.config/template.json`. No
NEEDS CLARIFICATION remained open after inspection; two design choices are flagged as
`/speckit-clarify` candidates (carried from the spec Assumptions) and resolved with a
default below.

## Decision 1 — Symbol shape: a `choice` parameter mirroring `designSystem`

- **Decision**: Add `lifecycle` as `{"type":"parameter","datatype":"choice","defaultValue":"spec-kit"}`
  with three `choices` (`spec-kit`, `sdd`, `none`), each carrying a self-describing
  `description` (FR-010).
- **Rationale**: `designSystem` is the exact same kind of option — a fixed-set choice with a
  no-diff default (`wcag`) and per-value descriptions. `choice` datatype natively rejects
  out-of-set values (FR-006/SC-004) with no extra work, identical to today's `designSystem`
  rejection behavior the spec points to.
- **Alternatives considered**:
  - *Two bools* (`emitLifecycle`, `sddIntent`): rejected — expands the combination matrix,
    allows nonsensical pairs, and breaks the single-discoverable-option goal (SC-005).
  - *Free `text` parameter*: rejected — no native validation, no discoverable choices, would
    need a hand-rolled fail-fast (violates simplicity, FR-006/FR-010).

## Decision 2 — Suppression mechanism: `condition` on `source` entries

- **Decision**: Gate output by adding `lifecycle == "spec-kit"` to the `condition` of each
  `source` entry that targets the gated set, composing with any existing `profile`/`feedback`
  condition (`&&`). Leave product `source` entries (`template/base/`, `samples/`, the ant
  overlay) unconditioned by lifecycle.
- **Rationale**: This is the precise mechanism Feature 128 uses (`designSystem == "ant"`
  gates the ant overlay) and `feedback == true` uses for the feedback skill/extensions. With
  the default `spec-kit` selected, every gated condition evaluates true → identical source
  set → byte-identical output (SC-001). Suppression is "this source does not run", which
  cannot perturb other sources (FR-005).
- **Alternatives considered**:
  - *`postActions` that delete files after generation*: rejected — leaves a window of emitted
    files, is OS-shell-dependent, and risks deleting product files; not byte-clean.
  - *Separate template identities per lifecycle*: rejected — triples maintenance and breaks
    the "one option, one template" contract the board item asks for.

## Decision 3 — The gated set maps to source entries (constitution lives in the generated tree)

- **Decision**: The gated set is realized by gating these sources: `.specify/` →`.specify/`;
  `.agents/skills/`→`.agents/skills/`; `.agents/skills/`→`.claude/skills/`;
  `.template.config/generated/`→`./` (this carries the **constitution**
  `.specify/memory/constitution.md`, plus `AGENTS.md` and `CLAUDE.md` — the "generated
  agent-context tree"); and every `profile`/`feedback`-conditioned skill source that targets
  `.agents/`/`.claude/`/`.specify/extensions/`.
- **Rationale**: Matches the board item's four named artifact groups literally (spec
  Assumptions). The constitution is delivered only via the generated tree, so gating that
  source suppresses the constitution as required (FR-004), confirmed by
  `find .template.config/generated -name constitution.md`.
- **Alternatives considered**: Gating the constitution independently of the rest of the
  generated tree — rejected; it is not emitted from a separate source, and the board item
  groups it with the agent-context tree.

## Decision 4 — Validation: Feature 128 report-gate + env-gated live regenerator

- **Decision**: Reuse the Feature 128 two-part pattern exactly:
  1. **Always-on deterministic gate** `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`
     — self-provisions (env-free `--emit-report` verdict-core path: no `dotnet new`, build, GL,
     or network) then asserts the **gitignored** validation report under `readiness/` (regenerated,
     never committed): covered-values equals the
     enumerated `lifecycle` choice set parsed from `template.json`; default byte-identical;
     gated set absent under `sdd`/`none`; product present; all 12 combos generate; unknown
     value fails).
  2. **Env-gated regenerator** `scripts/validate-lifecycle-template.fsx` behind
     `FS_GG_RUN_LIFECYCLE_VALIDATION=1` — performs real `dotnet new fs-gg-ui` per
     `lifecycle` × `profile`, diffs default-vs-no-value (proves `diff-vs-today=none`), checks
     the gated set absent for `sdd`/`none` and the product present, asserts an unknown value
     is rejected, and writes the gitignored report (regenerated, never committed).
- **Rationale**: This is the repo's established design-time validation shape (report-gate +
  `FS_*` live op), it is GL-free and network-free, it keeps the deterministic suite green on
  a fresh checkout via self-provisioning, and it provides real `dotnet new` evidence
  (Principle V). Enumerating choices from `template.json` enforces coverage (a new value left
  unvalidated fails the gate — TP-7).
- **Alternatives considered**: A pure JSON-shape unit test only — rejected; it cannot prove
  byte-identical default or real file suppression (the spec's standing assumption: shape
  tests pass while real generation is wrong).

## Decision 5 — Byte-identical proof method

- **Decision**: Prove SC-001 by scaffolding each profile twice — once with no `--lifecycle`
  and once with `--lifecycle spec-kit` — and a third reference with the pre-feature template,
  asserting an empty recursive diff. The regenerator records `diff-vs-today=none` per profile,
  mirroring the `designSystem` `wcag` `diff-vs-today=none` line.
- **Rationale**: Directly operationalizes FR-002/SC-001. The "no `--lifecycle` == explicit
  default" leg also proves FR-002/Acceptance #3.
- **Alternatives considered**: Hashing committed fixtures of the full tree — rejected as
  brittle and large; live diff against a from-`git`-stash reference is cheaper and truthful.

## Open design choices (clarify candidates) — resolved with a default to proceed

These come from the spec Assumptions; the plan proceeds with the literal board-item reading
and flags them so `/speckit-clarify` can override before implementation locks in.

### CC-1 — Dangling references in ungated product docs (the spec "Suppressed-but-referenced" edge case)

- **Finding (verified)**: `template/base/CLAUDE.md` and `template/base/README.md` are ungated
  product files that reference `.specify/`/skills. Under `sdd`/`none` the generated-tree
  `CLAUDE.md` (which normally overwrites the base one, since `.template.config/generated/` is
  listed after `template/base/`) is suppressed, so the **base** `CLAUDE.md` survives and points
  at a `.specify/` workspace that was not produced — exactly the edge case the spec forbids.
- **Default decision (to proceed)**: Treat this as in-scope implementation work, NOT a silent
  acceptance. Make the product-facing `CLAUDE.md`/`README.md` lifecycle-aware so that under
  `sdd`/`none` no emitted file carries a dangling reference to a suppressed artifact. Concrete
  options for the implementer (decide in tasks): (a) move the lifecycle-referencing prose into
  the gated generated-tree copy and keep an ungated, lifecycle-neutral base copy; or (b) gate
  the base copies and provide a neutral fallback. Verified by a regenerator assertion that
  greps the emitted tree under `sdd`/`none` for references to suppressed paths.
- **Clarify trigger**: If the user prefers to accept markdown-only dangling references as
  non-breaking (build still succeeds), CC-1 collapses to a documentation note instead of code.

### CC-2 — Are product-authoring skills part of the gated set? (spec Assumption) — **RESOLVED (accepted 2026-06-27)**

- **Decision (locked in spec.md Assumptions)**: Follow the board item literally — **all**
  `.agents/`/`.claude/` skill sources (including product-authoring skills like `fs-gg-scene`) are
  gated for non-`spec-kit` lifecycles; under `sdd` the downstream scaffold re-supplies needed
  skills. The gated-source map (all 8 product-skill + 2 sample-pack skill entries) reflects this.
- **Reopen trigger**: If product-authoring skills should remain while only Spec-Kit/governance
  files are gated, re-run `/speckit-clarify`; the gated-source map then narrows to the `.specify/`
  + constitution + governance context subset and the product-skill `source` entries stay ungated.

### CC-3 — Distinct template-emitted `sdd` skeleton/marker? — **RESOLVED (accepted 2026-06-27)**

- **Decision (locked in spec.md Assumptions)**: `sdd` and `none` suppress the **identical**
  template-level set; the template emits no separate SDD skeleton (that is the P2 downstream
  scaffold's job). The two values differ only by declared intent carried in the choice value.
- **Reopen trigger**: If a template-emitted `sdd` marker file is later desired, re-run
  `/speckit-clarify` and add one ungated-for-`sdd` source; this does not affect the `none` path or
  the byte-identical default.

## Resolved unknowns summary

| Technical Context item | Resolution |
|------------------------|-----------|
| Symbol type | `choice`, default `spec-kit` (Decision 1) |
| Suppression mechanism | `condition` on gated `source` entries (Decision 2) |
| Gated set → sources | Decision 3 table (mirrors plan.md gated-source map) |
| Unknown-value handling | native `choice` rejection (Decision 1; FR-006) |
| Testing approach | report-gate + env-gated regenerator (Decision 4) |
| Byte-identical proof | triple-scaffold diff, `diff-vs-today=none` (Decision 5) |
| Dangling-ref edge case | in-scope; CC-1 with default + clarify trigger |
