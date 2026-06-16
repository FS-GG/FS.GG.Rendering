# Implementation Plan: Design-System Template Parameter (`--designSystem wcag|ant`) — Workstream F, Phase F3

**Branch**: `128-design-system-template-param` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/128-design-system-template-param/spec.md`

## Summary

Turn F2's selectable **color policy** (`wcag` / `ant`) into a **first-class scaffolding choice** on the
project template: a `--designSystem wcag|ant` parameter (default `wcag`) that imprints the chosen
design language onto the generated product and makes the product **self-describing** about which policy
governs its colors. Add a **repeatable generated-product validation** that, for every accepted value,
scaffolds a product, builds it, and confirms its color/contrast governance reports the verdicts expected
for that policy.

The load-bearing constraint, surfaced by a Phase-0 audit of the layering and the F2/F3/F4/F5 split:
**F3 cannot make the generated product *run* the policy at runtime.** `ColorPolicy` is `module internal`
in `FS.GG.UI.Color` (public promotion deferred to **F5**); a generated product consumes the framework as
NuGet packages and can reach only the **public** `Contrast` (WCAG-only) surface — not `ColorPolicy.ant`.
Wiring the design-system *style resolver* onto the policy is deferred to **F4**. Therefore F3 is
**selection + recorded self-describing choice + framework-side validation**, *not* a runtime restyle.
The "governance gate that delegates to the selected policy" (FR-006) is realized as the framework's
existing internal `ColorPolicy` evaluation, **keyed by each scaffolded product's recorded choice** —
proving the parameter selects *rules*, not a palette, using the F2 engine verbatim.

Two technical decisions carry the design:

1. **Recording reuses the `feedback` no-diff pattern.** The `wcag`/default path emits **zero** new
   content (byte-identical to today's scaffold — SC-001); `wcag` is the *documented default*, identified
   by the **absence** of any override (exactly how `feedback==false` "induces no diff"). The `ant` path
   fires **conditional sources** (`(designSystem == "ant")`) that copy a self-describing record + the
   Ant design-language imprint into the product. **No base file is edited**, so the default scaffold
   cannot drift.
2. **Validation reuses the existing report-gate + env-gated-live-run pattern.** A committed
   generated-product validation report is asserted by an always-on gate test (like
   `GeneratedConsumerValidationTests`); the heavy live work — `dotnet new` per accepted value, real
   `dotnet build`, and per-policy verdict check — runs behind an env-gated regenerator/script (like
   `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE`). Coverage is enumerated from the template's own choice set, so
   no accepted value can ship unvalidated (FR-009/SC-006).

`wcag` ≡ today (byte-identical, no new files); `ant` records `ant` + carries the Ant imprint and passes
the Ant pairings; the two diverge on ≥1 pairing (the F2 `primary-hover-fg-on-surface` @ ratio ≈ 2.99 —
`Fail` under `wcag`, `Aa` (an AA pass) under `ant`), demonstrating policy-not-palette (SC-004). Zero public package
surface delta; the framework's surface baselines stay green (FR-011/SC-007).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo-wide `Directory.Build.props`). The always-on gate is an
Expecto test in `tests/Package.Tests`; the live scaffold/build/verify regenerator is a `dotnet fsi`
script gated by an env flag (the established heavy-op pattern).

**Primary Dependencies**: **none new** to any product/package assembly or generated product. F3 wires the
already-delivered F1 tokens (`DesignTokensExt`) and F2 engine (`ColorPolicy`) into the template; both stay
`internal`. The generated `ant` product gains **no** new package reference (FR-012: no React/DOM/web/
icon-font; Ant remains a rule set expressed in existing color primitives). The per-policy verdict check
runs inside the framework repo, where the regenerator script reaches the `internal` F2 engine by
`#load`-ing the `ColorPolicy` source closure (compiled into the script's own assembly — same-assembly
access, so no `InternalsVisibleTo` is needed; an `fsx` is **not** the `Controls.Tests` assembly and
cannot borrow its IVT grant). The check does not run in the generated product.

**Storage**: dotnet `template.json` symbols + conditional sources; a new `template/design-system/ant/`
overlay (the Ant record + imprint copied only for `ant`); a committed validation report artifact under
`specs/128-design-system-template-param/readiness/`; the F2 committed reports under `docs/reports/` are
reused (not duplicated) as the per-policy expectation oracle.

**Testing**: existing repo suites via the harness, unchanged pass/skip counts. New always-on gate
`Feature128DesignSystemTemplateTests` in `Package.Tests` (deterministic, no GL, no `dotnet new`):
asserts the committed validation report shows both values covered, both build-pass, `wcag` ≡ today,
`ant` passes its pairings, and ≥1 divergence. New env-gated live regenerator
(`scripts/validate-design-system-template.fsx`) performs `dotnet new` per value + real build + per-policy
verdict check and writes the report. Surface baselines (`tests/surface-baselines/*.txt`) show **zero
delta**.

**Target Platform**: the project template (`dotnet new fs-gg-ui`) and the products it scaffolds
(library/app consumed via packages). No GL required for the gate; the live build is real .NET build.

**Project Type**: multi-project F# library/framework + a dotnet project template (single solution
`FS.GG.Rendering.slnx`).

**Performance Goals**: none new — behaviour-neutral on the default path. No render/hot-path code; the
parameter affects only scaffold-time content selection and a test-time/CI verification.

**Constraints**:
- **Default path is a true no-op (FR-003/SC-001)** — `wcag`/no-value scaffold is **byte-identical** to
  today's output; the implementation edits **no** base file and emits **no** new content on that path.
- **Policy stays internal (FR-011/SC-007)** — no public package API added; framework surface baselines
  unchanged; the generated product calls **no** internal policy member (it records a choice; the
  framework validates it).
- **Selects rules, not palette (FR-004/FR-006/SC-004)** — the `ant` product is governed by the `ant`
  *policy* (verified via F2's engine), not merely given different colors; ≥1 pairing diverges by policy.
- **No-overclaim preserved (FR-010, edge case)** — where `ant` certifies a pairing WCAG would fail, the
  authority recorded with the verdict is Ant, not WCAG (F2's `AuthorityNote`, reused verbatim).
- **Unknown value rejected, never substituted (FR-007/SC-005)** — handled by the template engine's
  `choice` validation (same machinery as `profile`), surfacing the accepted set; no silent fallback.
- **Coverage is exhaustive (FR-009/SC-006)** — validation enumerates the `designSystem` choice set so
  every accepted value is exercised; a new value cannot ship unvalidated.
- **Real build (assumption)** — the live validation scaffolds an actual product per value and builds it,
  so a broken `ant` (or regressed `wcag`) scaffold cannot ship undetected.
- **Orthogonal to `profile`** — design-system choice is independent of the product profile; any profile
  may be generated under either policy (no combinatorial fork in behaviour).

**Scale/Scope**: 1 new `template.json` `choice` symbol (`designSystem`) + 2–3 conditional `sources`
entries (the `ant` overlay, mirroring `feedback`); 1 new `template/design-system/ant/` overlay (a
self-describing record + the Ant imprint, copied only for `ant`); 1 new env-gated regenerator script;
1 committed validation report; 1 new always-on gate test file; PROVENANCE note that the `ant` imprint
reuses F1/F2 verbatim (no new color values). Two accepted values (`wcag`, `ant`); the shape must admit
future values (`material`, `fluent`) without reshaping.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | **PASS** | No new public F# surface (no `.fsi` to draft). The "honest audience" intent is met by exercising the template through the *same* `dotnet new` surface a maintainer uses (the live regenerator) and by the gate test asserting the recorded contract. Per-policy verdicts reuse F2's already-FSI-honest `ColorPolicy` calls. Gate test authored failing-first (report absent → red), then greened by the regenerator. |
| II. Visibility lives in `.fsi` | **PASS** | No public module added or changed; `ColorPolicy`/`DesignTokensExt` remain `internal` (no `.fsi`). No access modifiers added to any `.fs`. Per-package surface baselines untouched. |
| III. Idiomatic simplicity | **PASS** | Plain mechanisms: a dotnet `choice` parameter + conditional `sources` (identical to `feedback`/`profile`); a verification script that shells `dotnet new`/`dotnet build` and compares strings; a gate test asserting a committed report. No operators/SRTP/reflection/CEs/type providers/non-trivial active patterns. |
| IV. Elmish/MVU boundary | **N/A** | No stateful/long-running workflow in the framework. The only I/O (scaffold, build, read/write the report) lives at the script/test **edge**, expressed as straight-line process calls — no `update`-style logic to model. |
| V. Test evidence | **PASS** | Real evidence: a real `dotnet new` per accepted value and a real `dotnet build` (not mocked); per-policy verdicts from the real F2 engine over real F1 tokens; the no-diff proof is a real byte-compare of two scaffolds. Failing-first gate (no report → fail). No test removed/weakened; heavy live op is **env-gated**, not skipped-without-rationale (rationale: `dotnet new`+build cost, same posture as `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE`). |
| VI. Observability & safe failure | **PASS (light)** | Unknown design-system value is rejected by the template engine with the accepted set surfaced — no silent fallback (FR-007/SC-005). The validation report enumerates coverage, build result, and per-policy verdict per value; a missing/failed variant is a loud failure, not a swallowed one. `ant`'s no-overclaim authority is disclosed (FR-010, via F2). |
| **Change Classification** | **Tier 1 (template scaffolding contract)** | Adds a new accepted scaffolding option — a change to the template's external **command/scaffolding contract** — so it takes the full chain: spec, plan, template-contract update, the new generated-product **validation** (the template's contract gate), test evidence, docs/PROVENANCE. **Package** API surface is *unchanged* (FR-011/SC-007): no `.fsi` edits, **zero** surface-baseline delta — the Tier-1 obligations that touch public F# surface are N/A here because no public F# surface changes. |
| Engineering Constraints — layering / one-control-set | **PASS** | No control forked per design language; no `AntButton`. The `ant` choice selects a *policy/imprint*, not a behaviour fork. `DesignSystem` stays dep = Scene only (F1/F2 posture untouched). |
| Engineering Constraints — no React/DOM/icon-font (FR-012) | **PASS** | The `ant` overlay adds **no** package and **no** web/icon dependency; the Ant imprint is the existing F1 token values + F2 rule set expressed in the existing color primitives. |
| Engineering Constraints — template checks pay for themselves | **PASS** | The generated-product validation is a narrow, owned check protecting a concrete contract (both scaffolds build + govern correctly); always-on cost is a string-assert over a committed report, heavy live cost is env-gated. |

**Gate result: PASS** — no violations; Complexity Tracking not required. Two watch-items, both resolved
in Phase 0: **(R1)** the runtime-impossibility of calling the internal policy from a generated product →
F3 is selection + recorded choice + framework-side verification (not a runtime restyle, which is F4);
**(R2)** preserving byte-identical `wcag`/default → record **only** on the `ant` path via conditional
sources, editing no base file (the `feedback` pattern), with `wcag` self-described as the documented
default-by-absence.

## Project Structure

### Documentation (this feature)

```text
specs/128-design-system-template-param/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — runtime-impossibility, no-diff recording, validation pattern, coverage, provenance
├── data-model.md        # Phase 1 — Design-System Parameter / Policy Record / Generated-Product Validation shapes
├── quickstart.md        # Phase 1 — scaffold-both-variants + build + verdict validation runbook
├── contracts/
│   ├── template-parameter-contract.md        # the designSystem parameter: name, choices, default, no-diff & rejection semantics, recorded marker
│   └── generated-product-validation-contract.md  # the per-value scaffold→build→verdict gate: report format, coverage, drift/failure semantics
├── readiness/
│   └── design-system-template-validation.md  # committed validation report (regenerated by the env-gated script; asserted by the gate test)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
.template.config/
└── template.json                 # + "designSystem" choice symbol (default "wcag", choices wcag|ant);
                                   #   + conditional sources (designSystem == "ant") → copy the ant overlay
                                   #   (mirrors the existing "feedback" no-diff pattern; NO base edit)

template/
├── base/                         # UNCHANGED — guarantees wcag/default is byte-identical to today (SC-001)
├── feedback/                     # unchanged (the precedent pattern this feature mirrors)
└── design-system/                # NEW — overlay copied ONLY when designSystem == "ant"
    └── ant/
        ├── design-system.json    #   self-describing record: { "policy": "ant", "authority": "AntExpectation" } (FR-005/SC-002)
        └── docs/reports/
            └── color-policy-ant.md   #   the Ant design-language imprint as committed data (reuses F2's report; FR-004)

docs/reports/                     # UNCHANGED — F2's color-policy-{wcag,ant}.md reused as the per-policy verdict oracle
src/Color/ColorPolicy.fs          # UNCHANGED — internal F2 engine; reached by the framework validation via IVT
src/DesignSystem/DesignTokensExt.fs  # UNCHANGED — internal F1 Ant tokens

scripts/
└── validate-design-system-template.fsx   # NEW (env-gated live) — for each accepted value: dotnet new --designSystem <p>,
                                          #   build, assert wcag≡today + ant records ant, run ColorPolicy.byName→evaluate
                                          #   per recorded policy, write readiness/design-system-template-validation.md

tests/
├── surface-baselines/*.txt       # UNCHANGED (zero delta — the package-neutrality proof, SC-007)
└── Package.Tests/
    └── Feature128DesignSystemTemplateTests.fs  # NEW (always-on gate) — asserts the committed validation report:
                                                #   coverage = every choice, both build-pass, wcag≡today, ant passes pairings, ≥1 divergence
```

**Structure Decision**: Multi-project F# solution + dotnet template. The parameter lands in
`.template.config/template.json` as a `choice` symbol with conditional `sources` — the **same machinery**
as `profile` and `feedback`, so defaulting, unknown-value rejection, and casing follow established
template conventions (FR-001/FR-002/FR-007, assumption "template-mechanism reuse"). `template/base/` is
**untouched** so the `wcag`/default scaffold is byte-identical to today (SC-001); the `ant` record +
imprint live in a **new `template/design-system/ant/` overlay** copied only on the `ant` path (the
`feedback==true` precedent). The generated-product validation reuses the repo's established
**report-gate + env-gated-live-run** pattern (`GeneratedConsumerValidationTests` for the always-on
assert; `deferredPackageSmokeTests`/`FS_SKIA_RUN_*` for the heavy live op), with F2's committed
`docs/reports/color-policy-{wcag,ant}.md` as the per-policy verdict oracle so no second source of truth
is introduced. See `contracts/template-parameter-contract.md` for the parameter surface and
`contracts/generated-product-validation-contract.md` for the validation gate; `data-model.md` has the
entity shapes.

## Complexity Tracking

> Not required — Constitution Check passed with no violations.

Two decisions worth recording (neither a violation):

1. **F3 records + validates the choice; it does not run the policy in the generated product.** The
   rejected alternative — exposing `ColorPolicy` publicly so the generated product could evaluate its own
   colors at build/test time — would have pulled F5's public-surface promotion (and a surface-baseline
   delta) into F3, violating FR-011/SC-007 and the deliberate F2 "internal-first" posture. Recording the
   choice and verifying it framework-side (where the internal engine is legitimately reachable) keeps F3
   zero-public-delta while still proving the choice selects *policy* (the F2 engine is the oracle).
   Runtime use of the policy by products is F4's job.
2. **The `wcag`/default path emits nothing.** The rejected alternative — recording `wcag` explicitly in
   every scaffold (e.g. a `<FsGgDesignSystem>wcag</FsGgDesignSystem>` line in base) — would diff today's
   output and break SC-001's "byte-identical to today." Recording only on the non-default path (the
   `feedback` precedent) makes `wcag` the documented default-by-absence; this is the only reading that
   satisfies FR-005 + SC-001 + SC-002 together, and it matches the spec's own edge-case note and the
   "default false induces no diff" assumption.
