# Phase 0 Research — Ant-derived design-token taxonomy (F1)

The spec assumes a generated, drift-gated token model. A Phase-0 audit of the actual machinery
revised that picture and fixed the technical approach. Each decision below resolves a constraint the
plan depends on.

## R1 — There is no token generator today (load-bearing)

**Finding**: `src/DesignSystem/DesignTokens.fs` carries a `// GENERATED — do not edit` header naming
`./fake.sh build -t RefreshSurfaceBaselines`, but **no such generator exists**: there is no `fake.sh`,
no `build.fsx`, and nothing in the repo parses `design-tokens.tokens.json`. The "design-token-drift
gate" referenced in the `.fsi` comments is in fact `tests/Controls.Tests/DesignTokenParityTests.fs`,
which asserts value parity against **frozen literals** and never reads the JSON. The CI `gate.yml`
runs only the *surface*-baseline drift step, not a token-drift step.

**Decision**: F1 builds the generator the spec assumes — a deterministic build-time
`scripts/generate-design-tokens.fsx` (run via `dotnet fsi`) that reads the DTCG source and emits the
new internal taxonomy module, plus a real drift test that regenerates and compares. The generator
emits **only the new internal `DesignTokensExt` module** in F1; the existing public `DesignTokens.fs`
(flat primitives) is **left byte-identical** and untouched.

**Rationale**: Honors FR-003/FR-010 ("generated", "drift gate passes") with a real mechanism instead
of a by-convention header. Scoping the generator to the new internal module avoids any risk of a
byte-diff or value churn on the existing *public* `DesignTokens.fs`, which would break neutrality
(US2). The DTCG source already holds the existing primitives, so the JSON stays a faithful superset.

**Alternatives considered**:
- *Retrofit the generator to also (re)emit the existing public `DesignTokens.fs`.* Rejected for F1:
  any whitespace/ordering difference would redden the public file and the existing parity test; it is
  out of F1's additive scope. F5 (public promotion) is the right place to unify generation.
- *Keep hand-maintaining the new tokens (no generator).* Rejected: violates FR-003 "generated, not
  hand-coded" and leaves the larger token set unguarded.

## R2 — Internal placement (zero public-surface delta)

**Decision**: The taxonomy lands as `module internal DesignTokensExt` in `FS.GG.UI.DesignSystem` with
**no `.fsi`**, reached by tests (and the future F4 resolver) via `InternalsVisibleTo`. F1 adds
`<InternalsVisibleTo Include="Controls.Tests" />` to `DesignSystem.fsproj`.

**Rationale**: This is the established internal pattern (`module internal Reconcile` /
`module internal RetainedRender`, reached via IVT). An internal module has no public `.fsi` and adds
**no** row to any per-package surface baseline, so the surface-drift gate stays green (US2/FR-005).
`Directory.Build.props` promotes FS0078 (a `private`/`internal` modifier on a top-level binding in a
file *with* a companion `.fsi`) to an error — a `module internal` with **no** `.fsi` does not trigger
it, consistent with Principle II.

**Alternatives considered**:
- *Public `DesignTokensExt.fsi` now.* Rejected: that is exactly the public-surface change the spec
  defers to F5; it would redden the baseline and commit the framework to a token contract before the
  resolver/themes have validated the shape.
- *A separate internal namespace or satellite project.* Rejected: heavier than a single internal
  module; a new project would also touch `.slnx` + the refresh-script (surface-gate risk) for no
  benefit.

## R3 — JSON parsing without a product dependency

**Finding**: `DesignTokenParityTests` enforces that `Controls.fsproj` references **no** JSON parser
(`System.Text.Json`/`Newtonsoft.Json`/`FSharp.Data` are on a forbidden list); the framework reads
tokens only as generated F# values.

**Decision**: The generator is a standalone `.fsx` executed by `dotnet fsi` (where `System.Text.Json`
is available from the SDK) and is **not compiled into any product or test assembly**. The drift test
invokes the generator as a subprocess (`dotnet fsi scripts/generate-design-tokens.fsx --check` or
regenerate-to-temp) and diffs file text, so neither the product nor the test assembly gains a JSON
dependency.

**Rationale**: Mirrors `scripts/refresh-surface-baselines.fsx` (script-time `System.Reflection`, no
product impact). Keeps the forbidden-package guard green and avoids pulling a parser into a packaged
assembly.

## R4 — A real drift gate (regenerate-then-compare)

**Decision**: Add `Feature126TokenTaxonomyTests` to `Controls.Tests` with a test that runs the
generator to a temp path and asserts the committed `DesignTokensExt.fs` is **byte-identical** (currency
+ idempotency in one shot), plus a generated-marker check. CI coverage comes for free because the test
runs under `dotnet test`. Optionally a follow-up may add a `gate.yml` step mirroring the
surface-baseline step; that CI edit is **not required** for F1 (the unit test is the gate).

**Rationale**: Matches the proven surface-baseline pattern (regenerate in place, fail on diff). Proves
FR-003 (idempotent) and FR-010 (committed artifact matches source) without a JSON dependency in the
test assembly.

**Alternatives considered**:
- *Value-parity test that re-parses the JSON in the test assembly.* Rejected: pulls `System.Text.Json`
  into `Controls.Tests` and duplicates the generator's parsing logic; the subprocess regenerate-diff
  is simpler and tests the real tool.

## R5 — Behaviour- and contract-neutral proof

**Decision**: Nothing reads the new tokens in F1. Neutrality is proven exactly as feature 125 proved
it: the full existing suite passes with identical pass/skip counts, the gallery render-identity tests
(`ThemeInvarianceTests`/`PageRenderTests`) stay green, the existing `DesignTokenParityTests` (existing
primitive values) stay green, and `scripts/refresh-surface-baselines.fsx` shows **zero** baseline
delta. The public `Theme` record is **not** modified (its `Success`/`Warning` already landed in D1).

**Rationale**: An unconsumed token can only break neutrality by (a) reddening a baseline — prevented by
R2's internal placement — or (b) being accidentally read on a render path — caught by render identity.

## R6 — Taxonomy scope and values

**Decision**: Adopt the four Ant layers + four supplementary groups (`data-model.md` enumerates
members):
- **seed** (brand/scale inputs), **map.light/dark** (explicit per-mode values — *no* derivation
  algorithms in F1), **alias.light/dark** (render-facing names), **component.<family>** for the
  catalog families the analysis names (button, input, table, tabs, menu).
- **spacing** `xs/sm/md/lg/xl = 4/8/16/24/32`; **named density** `Comfortable/Middle/Compact`;
  **type scale** `body/small/title/section/display` (+ line-height); **elevation**
  `none/low/medium/high`.
- Values are **deliberate and Ant-informed**, not mechanically copied: Ant's defaults (brand
  `#1677ff`, functional families, 8-unit grid, `controlHeight 32`) inform structure and genuinely new
  groups, but **existing Light/Dark primitive values are preserved exactly** and never replaced with an
  Ant number (FR-004/FR-011, edge case).

**Rationale**: Matches the Ant adoption analysis (Pillar 1) and keeps F1 additive. Explicit per-mode
map/alias values (vs. algorithmic tint/shade ladders) are the analysis's prescribed starting point and
keep the generator trivial; the structure leaves room for algorithms later without a vocabulary change.

**Alternatives considered**:
- *Algorithmic map derivation from seed now.* Rejected per the analysis ("start as explicit DTCG
  aliases before adding algorithms"); adds generator complexity with no F1 consumer.
- *Exhaustive component tokens for all 52 catalog controls.* Rejected: F1 seeds the families the
  analysis names; more are added when F4 migrates a control to consume them (no value in unconsumed
  breadth).

## R7 — Named density preserves `Theme.withDensity`

**Decision**: Named-density tokens are constants (`Comfortable`/`Middle`/`Compact`) expressed as
density multipliers in the source; `Comfortable = 1.0` equals today's default so existing behaviour is
unchanged, with `Middle`/`Compact` smaller (Ant-informed by `controlHeight 40/32/24`). They are
**unconsumed** in F1 — `Theme.density`/`Theme.withDensity` continue to behave exactly as today.

**Rationale**: Provides the vocabulary D2/F4 need while guaranteeing no behaviour change (the named
levels are not wired into `Theme` or any render path here).

## Resolved unknowns

No `NEEDS CLARIFICATION` markers remain. The one material design choice (explicit per-mode map values
vs. algorithmic derivation) is resolved by R6 (explicit first). All other gaps are covered by reasonable
defaults documented in the spec's Assumptions and the decisions above.
