# Phase 1 Data Model: Lifecycle Choice Symbol

This feature has no runtime data model. The "entities" are template-engine declarations and
the file groups they control. They are modeled here as the contract the implementation must
satisfy.

## Entity: `lifecycle` choice (template symbol)

The new `symbols` entry in `.template.config/template.json`.

| Field | Value / Rule |
|-------|--------------|
| `type` | `parameter` |
| `datatype` | `choice` |
| `defaultValue` | `spec-kit` |
| `choices` | exactly `spec-kit`, `sdd`, `none` (no others) |
| `description` (symbol) | self-describing summary of what the option controls |
| per-choice `description` | each value states what it emits/suppresses (FR-010, SC-005) |

**Validation rules**
- Out-of-set value → engine rejects generation (FR-006/SC-004); no silent fallback.
- Default selection (or explicit `spec-kit`) → byte-identical to pre-feature output
  (FR-002/SC-001).
- Mutually exclusive: exactly one value per generation.

**State / behavior**
- `spec-kit` → emit the full gated set (FR-003).
- `sdd` → suppress the gated set; product emitted (FR-004); intent = "external owner supplies
  lifecycle".
- `none` → suppress the gated set; product emitted (FR-004); intent = "no lifecycle".
- At the template's output level, `sdd` and `none` suppress the **identical** set; they differ
  only by the declared intent the value carries (research CC-3).

## Entity: Gated lifecycle scaffolding (controlled source set)

The set of `source` entries gated by `lifecycle == "spec-kit"`. Emitted in full for
`spec-kit`; entirely absent for `sdd`/`none`.

| Gated source | Target | Composes with |
|--------------|--------|----------------|
| `.specify/` | `.specify/` | — |
| `.agents/skills/` | `.agents/skills/` | — |
| `.agents/skills/` | `.claude/skills/` | — |
| `.template.config/generated/` | `./` (constitution + `AGENTS.md` + `CLAUDE.md`) | — |
| `template/product-skills/*` (8) | `.agents/` & `.claude/` | existing `profile` condition |
| `template/fragments/samples/skill/` (2) | `.agents/` & `.claude/` | `profile == "sample-pack"` |
| `template/feedback/skill/` (2) | `.agents/` & `.claude/` | `feedback == true` |
| `template/feedback/extensions/` | `.specify/extensions/feedback/` | `feedback == true` |

**Invariant**: the gated set is exactly the four board-item groups — `.specify/` workspace,
constitution, agent skill/context files (`.agents/`, `.claude/`), and the generated
agent-context tree. Nothing else is gated.

## Entity: Generated product (never altered by lifecycle)

The ungated `source` entries. Their output for a given `(profile, designSystem, feedback)`
is identical whether `lifecycle` is `spec-kit`, `sdd`, or `none` (FR-005).

| Ungated source | Target |
|----------------|--------|
| `template/base/` | `./` (source, project files, product tests, docs) |
| `template/fragments/samples/` | `samples/` (sample-pack product content) |
| `template/design-system/ant/` | `./` (ant overlay; only when `designSystem == "ant"`) |

**Edge note (research CC-1)**: `template/base/CLAUDE.md` and `template/base/README.md` are in
this ungated group but reference suppressed artifacts. The implementation MUST ensure no
emitted file under `sdd`/`none` carries a dangling reference to a suppressed path (spec
"Suppressed-but-referenced" edge case).

## Composition matrix (must all generate — SC-004)

`lifecycle` (3) × `profile` (4) = 12 valid combinations, each crossable with `designSystem`
(2) and `feedback` (2). The validation regenerator proves all 12 `lifecycle`×`profile`
combos generate, and that `designSystem`/`feedback` effects are the union of their intended
outputs with no silent override (FR-008).

## Validation report (gitignored, self-provisioned gate artifact)

Written by `scripts/validate-lifecycle-template.fsx`, asserted by
`Feature204LifecycleTemplateTests.fs`. Shape mirrors the Feature 128 report.

**Persistence (mirrors Feature 128 exactly)**: the report lives at
`specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md`, which is
**gitignored** (`specs/*/readiness/`) and therefore **never committed**. Because a fresh checkout
has no report, the always-on gate **self-provisions** it at module initialization via an env-free
`--emit-report` verdict-core path (NO `dotnet new`, build, GL, or network) before the assertions
run, then asserts the report exists and holds the lines below. The full live proof — real
`dotnet new` per combo + diff + suppression checks — stays opt-in behind
`FS_GG_RUN_LIFECYCLE_VALIDATION=1` and overwrites the same report. "Committed" anywhere else in
these docs refers to this self-provisioned artifact's *presence at assert time*, not a checked-in
file.

| Line | Meaning |
|------|---------|
| `covered-values: spec-kit, sdd, none` | equals the enumerated `lifecycle` choices (coverage gate) |
| `<value>: generate=pass` (×3, ×profiles) | the combo scaffolded successfully |
| `spec-kit: diff-vs-today=none` (per profile) | byte-identical default (SC-001) |
| `sdd: gated-absent=ok product-present=ok diff-vs-default=gated-only` | suppression + product intact (SC-003); the default-vs-`sdd` diff differs in *only* gated paths (FR-009 "exactly the gated set and nothing else") |
| `none: gated-absent=ok product-present=ok` | suppression + product intact (SC-003) |
| `dangling-refs: none` | no emitted file references a suppressed path (CC-1) |
| `unknown-value: rejected` | fail-fast proof (SC-004) |
| `provenance: verdict-core (env-free…)` \| `provenance: live` | discloses whether the report was self-provisioned env-free (diff/suppression lines synthesized — Constitution V) or written from the real `dotnet new` live run |
| `result: pass` | all checks held |
