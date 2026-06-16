# Implementation Plan: Ant-derived design-token taxonomy (Workstream F, Phase F1)

**Branch**: `126-ant-token-taxonomy` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/126-ant-token-taxonomy/spec.md`

## Summary

Enrich the design-token model from today's flat ~13 primitives into the Ant-derived layered
taxonomy — **seed → map → alias → component** — plus four supplementary semantic groups (**spacing**,
**named density**, **type scale**, **elevation**). The expansion is **generated** from the DTCG source
and lands **internal/additive-first**: a new `module internal` in `FS.GG.UI.DesignSystem` (no `.fsi`,
reached by tests/future-resolver via `InternalsVisibleTo`), so there is **zero public-surface-baseline
change**, **zero change to any existing token value**, an **unchanged public `Theme` record**, and
**byte-identical rendered output**. This is the first reusable pillar of Workstream F; the resolver
(F4), policy validator (F2/F3), concrete themes (D2), and public-surface promotion (F5) consume it
later and are out of scope here.

The load-bearing technical decision, surfaced by a Phase-0 audit: **no token generator exists today**.
`src/DesignSystem/DesignTokens.fs` carries a "generated" header but is hand-maintained, and the
"design-token-drift gate" referenced in comments is in fact `DesignTokenParityTests` (value-parity
assertions that never read the JSON). F1 therefore **builds the generator the spec assumes**: a
deterministic build-time `dotnet fsi` script that reads the DTCG source and emits the internal token
module, plus a real drift test (regenerate-then-compare) that proves the committed artifact matches the
source. The existing public flat-primitives module is left **byte-identical** (out of F1's additive
scope; F5 may unify generation when it promotes public surface).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo-wide `Directory.Build.props`). The generator is a
standalone `.fsx` run via `dotnet fsi`.

**Primary Dependencies**: none new to any product/package assembly. The generator uses
`System.Text.Json` from the BCL/SDK at script time only (mirrors `scripts/refresh-surface-baselines.fsx`
using `System.Reflection`); it is **not** compiled into any product assembly. The product's
no-JSON-parser-dependency constraint (the `DesignTokenParityTests` forbidden-package guard on
`Controls.fsproj`) is preserved — the product reads only generated F# values.

**Storage**: the DTCG source of truth `src/Themes.Default/design-tokens.tokens.json` (extended); the
generated F# module `src/DesignSystem/DesignTokensExt.fs`. No runtime storage.

**Testing**: existing repo suites via the harness; new `Feature126TokenTaxonomyTests` in
`Controls.Tests`; render-identity reuses the gallery `ThemeInvarianceTests`/`PageRenderTests` and the
existing `DesignTokenParityTests`. The surface-drift gate (`scripts/refresh-surface-baselines.fsx` +
`tests/surface-baselines/*.txt`) must show **no delta**.

**Target Platform**: library packages consumed in-repo (themes, future resolver, tests).

**Project Type**: multi-project F# library/framework (single solution `FS.GG.Rendering.slnx`).

**Performance Goals**: none new — behaviour-neutral. No hot-path code added; no render path reads the
new tokens in F1.

**Constraints**:
- **Behaviour- and contract-neutral is the hard gate** — existing suite passes with identical
  pass/skip counts, render output byte-identical, public surface baselines unchanged, public `Theme`
  record shape unchanged, every existing token value byte-identical.
- **Generated, not hand-coded** — the new taxonomy is emitted by the generator from the DTCG source;
  the drift test fails on any divergence; the artifact is marked generated.
- **Internal/additive-first** — new tokens live in a `module internal` with no `.fsi`; public
  promotion is deferred (F5).
- **No new product dependency** — the generator is build-time tooling only.
- **Acyclic layering preserved** — generated module in `FS.GG.UI.DesignSystem` (dep = Scene only);
  DTCG source in `FS.GG.UI.Themes.Default` (dep = DesignSystem only).

**Scale/Scope**: 1 DTCG source file extended; 1 new generated internal `.fs`; 1 new generator script;
1 `InternalsVisibleTo` line; 1 new test file. The taxonomy adds the four Ant layers (seed, map.{light,
dark}, alias.{light,dark}, component.<family>) for the catalog families named in the analysis
(button/input/table/tabs/menu) plus spacing/density/type-scale/elevation groups. No consumer wired.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | **PASS** | The new surface is **internal** (no public `.fsi`). The "FSI honesty" intent is met by the generator emitting the values and the semantic tests *naming* the internal tokens (via `InternalsVisibleTo`) the way the future resolver will. No public API is drafted because none is added (deferred to F5). |
| II. Visibility lives in `.fsi` | **PASS** | The generated module is `module internal DesignTokensExt` with **no** `.fsi` — the established internal pattern (`Reconcile`, `RetainedRender`). No access modifiers on top-level bindings (so `Directory.Build.props`' FS0078-as-error does not fire — it targets files *with* a companion `.fsi`). The public `DesignTokens.fsi` is untouched; per-package surface baselines unchanged. |
| III. Idiomatic simplicity | **PASS** | Generator is plain F# + `System.Text.Json`; generated module is flat `let` bindings of `Color`/`float`. No operators, SRTP, reflection, CEs, or type providers. |
| IV. Elmish/MVU boundary | **N/A** | No stateful/I-O workflow; pure data + a deterministic build script. |
| V. Test evidence | **PASS** | Drift/idempotency test (regenerate-then-compare), layer-coverage test (names a token from every layer), neutrality test (existing values byte-identical; surface baselines clean), generated-marker check; render-identity via existing gallery suites. No test removed/weakened/skipped. |
| VI. Observability & safe failure | **N/A** | No runtime/critical path, no GL/IO altered. |
| Change Classification | **Tier 2 (internal change)** | No public API surface added/removed/modified; no new product/package dependency; no inter-package contract change; no observable behaviour change. Requires spec + tests; `.fsi` and baselines remain untouched. (The speckit chain still produces plan/tasks — exceeds the Tier-2 minimum, which is fine.) |
| Engineering Constraints — layering clause | **PASS** | Tokens stay in the design-system layer; DTCG source with the default theme; no control fork; no theme dependency added to Controls. |

**Gate result: PASS** — no violations; Complexity Tracking not required. The single watch-item is the
**no-generator-today** reality (Research R1): F1 builds a real, deterministic generator + drift test
rather than relying on the by-convention "generated" header.

## Project Structure

### Documentation (this feature)

```text
specs/126-ant-token-taxonomy/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — generator/drift reality, internal-placement, value decisions
├── data-model.md        # Phase 1 — the token taxonomy: groups, members, light/dark, values
├── quickstart.md        # Phase 1 — neutrality + generation/drift validation runbook
├── contracts/
│   ├── token-taxonomy-contract.md     # the internal token surface + DTCG source schema
│   └── generation-drift-contract.md   # generate→idempotent→drift-gate + neutrality invariants
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Scene/                       # FS.GG.UI.Scene (unchanged — Color)
├── DesignSystem/                # FS.GG.UI.DesignSystem
│   ├── DesignSystem.fsproj      #   + Compile DesignTokensExt.fs; + InternalsVisibleTo Controls.Tests
│   ├── DesignTokens.fsi/.fs     #   UNCHANGED public flat primitives (byte-identical)
│   ├── DesignTokensExt.fs       #   NEW — `module internal`, GENERATED, NO .fsi (the taxonomy)
│   ├── Types.DesignSystem.fsi/.fs   # unchanged (Theme record NOT modified)
│   └── Style.fsi/.fs            #   unchanged
├── Themes.Default/
│   └── design-tokens.tokens.json    # EXTENDED — + seed/map/alias/component/spacing/density/type/elevation
│                                    #   (existing light/dark primitive entries byte-identical)
└── Controls/, …                 # unchanged (no consumer wired in F1)

scripts/
├── refresh-surface-baselines.fsx    # unchanged
└── generate-design-tokens.fsx       # NEW — DTCG source → src/DesignSystem/DesignTokensExt.fs (deterministic)

tests/
├── surface-baselines/*.txt          # UNCHANGED (zero delta — the neutrality proof)
└── Controls.Tests/
    └── Feature126TokenTaxonomyTests.fs   # NEW — drift/idempotency, layer coverage, neutrality, marker
```

**Structure Decision**: Multi-project F# solution. The taxonomy is added as one **generated internal**
module in the existing `FS.GG.UI.DesignSystem` project (no new project, no `.slnx`/refresh-script
change → no surface-gate risk). The DTCG source in `FS.GG.UI.Themes.Default` is extended in place. A
build-time generator script joins `scripts/` next to the surface-baseline refresher. The drift test
lives in `Controls.Tests` (which already references `DesignSystem` and is the IVT target). See
`contracts/generation-drift-contract.md` for the generate/drift contract and `data-model.md` for the
token-by-token taxonomy.

## Complexity Tracking

> Not required — Constitution Check passed with no violations.
