# Implementation Plan: record per-skill materialization conditions on the product skill-manifest

**Branch**: `238-skill-materializes-when` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/238-skill-materializes-when/spec.md`

## Summary

Extend the machine-generated product skill-manifest so every entry records **why/when** its body
materializes and **where** the body comes from — two additive, optional fields:
`materializes-when` (the verbatim `template.json` `sources[].condition` that gates the skill's body)
and `supplied-by` (the provider source directory that holds the canonical body). The one entry that
matters is `fs-gg-project`: today it is `declared ∧ absent` in the sdd lane, indistinguishable from a
supply failure; recording `materializes-when: "(lifecycle == \"spec-kit\")"` makes it honestly
`declared ∧ condition-false ∧ absent`, so the downstream `.github#164` union gate can tell legitimate
suppression from a real `[missing]`. **No emission behavior changes** — the same skills materialize in
the same lanes as today; this feature only annotates the existing manifest.

The change is confined to three artifacts: the generator (`scripts/generate-skill-manifest.fsx`), its
output (`template/skill-manifest/skill-manifest.json`), and a new no-drift test
(`tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs`).

> The Foundational "early live smoke run" clause of the template does not apply: there is no running
> app surface here. The equivalent honest evidence is the deterministic `--check` gate plus the
> template.json-equivalence test (see Constitution Check → Principle V).

## Technical Context

**Language/Version**: F# on .NET (`net10.0`); the producer is an F# script run via `dotnet fsi`. The
manifest itself is a JSON data artifact.

**Primary Dependencies**: none new. Generator uses BCL only (`System.Security.Cryptography.SHA256`,
`System.Text`, `System.IO`). Test uses Expecto + `System.Text.Json` (already in `Package.Tests`).

**Storage**: files — `template/skill-manifest/skill-manifest.json` (data),
`.template.config/template.json` (the authoritative condition source).

**Testing**: Expecto via `tests/Package.Tests` (deterministic, GL-free, no `dotnet new`), plus the
generator's `--check` self-gate.

**Target Platform**: build-time / CI tooling; not a runtime surface.

**Project Type**: governance/build tooling inside the rendering framework repo.

**Performance Goals**: N/A (one-shot generation over 12 tiny files).

**Constraints**: additive/backward-compatible (unknown-key-tolerant consumers unaffected);
`materializes-when` MUST equal the `template.json` condition verbatim (single source of truth); the
shipped scaffolds' materialized skill sets stay byte-identical.

**Scale/Scope**: 12 manifest entries; 1 generator; 1 new test file; 0 `.fsi`/public-API changes.

## Constitution Check

*GATE: passes. Re-checked after Phase 1 design — still passes.*

- **I. Spec → FSI → Semantic tests → Implementation.** No public F# module is added, so there is no
  `.fsi` surface to sketch; the "contract" here is the **JSON manifest schema**, drafted as
  `contracts/skill-manifest.schema.md` before the generator/test are written, then exercised by the
  Package.Tests suite the same way a downstream consumer parses it. Order preserved: spec → contract →
  failing test → regenerate. **PASS (with the JSON-contract substitution noted).**
- **II. Visibility lives in `.fsi`.** No new public F# module; the generator is a script and the test
  is internal. **PASS (N/A).**
- **III. Idiomatic simplicity.** The generator gains a per-entry lookup of an already-known source dir
  (`supplied-by`) and a static condition string (`materializes-when`); the test reuses the exact
  template.json-parsing pattern already in Feature231's "catalog coherent with emission rows" test. No
  clever abstractions, no new operators/SRTP/reflection. **PASS.**
- **IV. Elmish/MVU boundary.** No stateful/IO workflow. **PASS (N/A).**
- **V. Test evidence is mandatory.** A new discriminating test fails before the fields exist / on any
  stale condition and passes after; the generator `--check` fails loud on drift. No synthetic evidence
  (real files, real template.json). **PASS.**
- **VI. Observability & safe failure.** `--check` and the test fail loud with actionable messages
  ("run dotnet fsi scripts/generate-skill-manifest.fsx"). **PASS.**

**Change Classification: Tier 1 (contracted change).** It alters the shape of the `skill-manifest`
cross-repo contract (ADR-0014 / the proposed `skill-registry`). The contract surface here is the JSON
schema, not an `.fsi`, so there is **no public-surface baseline to update**; the required artifact
chain is spec (done) + plan (this) + the schema contract + test evidence + docs. Recorded in
Complexity Tracking: none — no constitution violation to justify.

## Project Structure

### Documentation (this feature)

```text
specs/238-skill-materializes-when/
├── plan.md                        # This file
├── research.md                    # Phase 0 — R1..R5 decisions
├── data-model.md                  # Phase 1 — manifest entry schema + the 12-row condition table
├── contracts/
│   └── skill-manifest.schema.md   # Phase 1 — the JSON contract (v1 + additive fields)
├── quickstart.md                  # Phase 1 — regenerate + --check + test validation
├── checklists/requirements.md     # (from /speckit-specify)
└── tasks.md                       # (/speckit-tasks — not created here)
```

### Source Code (repository root)

```text
scripts/
└── generate-skill-manifest.fsx                      # CHANGED: emit materializes-when + supplied-by

template/skill-manifest/
└── skill-manifest.json                              # REGENERATED: +2 keys per entry (schemaVersion stays 1)

.template.config/
└── template.json                                    # UNCHANGED: the authoritative sources[].condition strings

tests/Package.Tests/
├── Feature231SkillManifestTests.fs                  # UNCHANGED: reads only id/scope/sha256/resolvablePath — stays green
└── Feature238SkillMaterializesWhenTests.fs          # NEW: presence + template.json-equivalence + supplied-by + fs-gg-project honesty
```

**Structure Decision**: single-repo tooling change. The generator is the **sole producer** of the
manifest; the new test is the **drift guard** binding `materializes-when` back to `template.json`. No
change under `src/`, no `.fsi`, no template emission rows.

## Complexity Tracking

No constitution violations — table intentionally empty.
