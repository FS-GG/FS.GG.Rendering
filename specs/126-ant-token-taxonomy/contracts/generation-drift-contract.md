# Contract — generation, drift gate, and neutrality invariants

Defines the build-time generator and the checks that keep the taxonomy generated, current, and
behaviour-/contract-neutral.

## Generator (`scripts/generate-design-tokens.fsx`)

- Run via `dotnet fsi scripts/generate-design-tokens.fsx` from repo root. **Not** compiled into any
  product/test assembly (R3); may use `System.Text.Json` (SDK/BCL) at script time only.
- **Input**: `src/Themes.Default/design-tokens.tokens.json`.
- **Output**: writes `src/DesignSystem/DesignTokensExt.fs` (`module internal DesignTokensExt`) with a
  `// GENERATED — do not edit. Source: src/Themes.Default/design-tokens.tokens.json` header.
- **Determinism**: same input ⇒ byte-identical output (fixed ordering, fixed formatting). Running it
  twice in a row produces no diff (idempotent — FR-003).
- **Modes**:
  - default (write): regenerate the file in place.
  - `--check`: regenerate to a temp buffer and exit non-zero if it differs from the committed file
    (used by the drift test / optional CI step).
- **Validation**: fails loudly on malformed leaves, mode-parity gaps (a `map`/`alias` key in one mode
  but not the other), or unknown `$type` (per the source schema).

## Drift / idempotency test (`Feature126TokenTaxonomyTests`, Controls.Tests)

- **D-1 (currency + idempotency)**: invoke the generator (subprocess) and assert the committed
  `DesignTokensExt.fs` is byte-identical to freshly generated output (FR-003/FR-010).
- **D-2 (generated marker)**: assert the file carries the generated header and a source reference.
- **D-3 (no JSON dep leaks)**: the test does **not** reference a JSON parser; it shells out to the
  generator and diffs text (R3).

## Layer-coverage test (US1)

- **C-1**: via `InternalsVisibleTo`, name a token from **every** group — a `Seed`, a `Map.Light` and a
  `Map.Dark`, an `Alias.Light`/`Alias.Dark`, a `Component.Button`, a `Space`, a `Density`, a `Type`,
  an `Elevation` — and assert each resolves to its expected value (SC-001/SC-005).
- **C-2**: assert `Map`/`Alias` mode parity — for a sampled key, both `Light` and `Dark` exist (V2).

## Neutrality invariants (US2 — the hard gate)

- **N-1 (surface)**: `dotnet fsi scripts/refresh-surface-baselines.fsx` produces **zero** change to any
  `tests/surface-baselines/*.txt` (no public package gained surface). `git diff --quiet
  -- tests/surface-baselines` succeeds.
- **N-2 (existing values)**: existing `DesignTokens.Light/Dark` primitive values are byte-identical;
  `DesignTokenParityTests` stays green; the public `Theme` record shape is unchanged.
- **N-3 (render identity)**: gallery `ThemeInvarianceTests`/`PageRenderTests` and the
  `DesignTokenParityTests` render-parity test stay green — rendered output byte-identical (FR-008).
- **N-4 (suite parity)**: full `dotnet test FS.GG.Rendering.slnx -c Release` passes with the **same**
  pass/skip counts as before F1 (SC-003).
- **N-5 (build)**: `dotnet build -c Release` is green, 0 new warnings/errors (SC-006).

## Acceptance mapping

| Spec | Verified by |
|---|---|
| FR-001/FR-002 (taxonomy + supplementary groups) | source schema + C-1 |
| FR-003 (generated, idempotent) | generator determinism + D-1 |
| FR-004 (additive, values preserved) | N-2 |
| FR-005 (internal, no public surface) | N-1 + internal placement (no `.fsi`) |
| FR-006 (reachable by in-repo code/tests) | C-1 via `InternalsVisibleTo` |
| FR-007 (light/dark parity) | C-2 |
| FR-008 (behaviour-neutral) | N-3/N-4 |
| FR-009 (layer placement) | module in DesignSystem, source in Themes.Default |
| FR-010 (drift gate, marked generated) | D-1/D-2 |
| FR-011 (Ant as reference) | N-2 + data-model V6 |
