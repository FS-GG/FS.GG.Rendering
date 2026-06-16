# Contract — Gate, Value-Parity & Render-Neutrality Invariants

The invariants F5 must satisfy. Each maps to a Success Criterion and is checked in the quickstart runbook.

## INV-1 — Surface-baseline delta is exactly the promoted symbols (SC-002, SC-007)

After regenerating baselines, `git diff tests/surface-baselines/`:

- **`FS.GG.UI.DesignSystem.txt`**: ONLY additions, and every added line is one of —
  `FS.GG.UI.DesignSystem.StyleResolver`, `FS.GG.UI.DesignSystem.StyleResolver+IntentPolicy`,
  `FS.GG.UI.DesignSystem.DesignTokensExt`, and each `FS.GG.UI.DesignSystem.DesignTokensExt+<Sub>[+<Sub>]` nested
  module type. No deletions, no reordering noise that isn't a promoted type.
- **Every other `*.txt`**: byte-identical (no change).
- Every added row is named (by symbol or by rule "the full DesignTokensExt taxonomy") in decision record `0004`,
  and `0004` names no promoted symbol that is absent from the diff. (Two-way agreement.)

> Caution: `refresh-surface-baselines.fsx` always rewrites (no real `--check`). Verify by `git diff`, not exit code.

## INV-2 — Token-value parity & generator drift (SC-003)

- The regenerated `DesignTokensExt.fs` token *values* are byte-identical to the pre-promotion file (only the
  `module` line loses `internal`, and a paired `.fsi` appears). Confirm by diffing values (ignoring the modifier
  line) and by an in-test assertion comparing representative promoted values to literals.
- The generator drift check passes: committed `DesignTokensExt.{fs,fsi}` == freshly generated. Run the generator's
  `--check` over BOTH files.

## INV-3 — Render neutrality under the neutral policy (SC-003, FR-004)

- `StyleResolver.resolveDefault` produces byte-identical `ResolvedStyle` to the pre-promotion internal binding for
  the full `{kind} × {intent} × {state}` cross-product (the Feature129 totality/parity oracle still holds).
- Rendered output (gallery render-identity / existing visual suites) is unchanged. No control code changed.

## INV-4 — Public-path consumability (SC-001, FR-008)

- With the `InternalsVisibleTo` grants removed, `tests/Controls.Tests/Feature130PublicSurfaceTests.fs` compiles and
  calls `StyleResolver.resolve`/`resolveDefault`/`baseStyleFor`/`neutralPolicy` and reads `DesignTokensExt.*`
  values — proving access is public, not internal.
- The promoted symbols appear in the *public* surface baseline (INV-1), which is itself machine proof of public
  visibility.

## INV-5 — Divergence reachable via a public policy (SC-005)

- Supplying a non-default `IntentPolicy` (e.g. mapping `"danger"` to `theme.Danger`) to `StyleResolver.resolve`
  yields a `ResolvedStyle` that differs from `resolveDefault` for a `danger` button — with **zero** edits to any
  control. Proven from the public-path test (no IVT).

## INV-6 — No new dependency, no Theme shape change, neutral perf (FR-011, FR-004, perf)

- No package/project reference added; `System.Text.Json` remains script-only.
- `Theme` record shape unchanged (baseline `FS.GG.UI.DesignSystem.Theme` row identical).
- No new allocation/runtime path; `buttonGeom`'s call site is unchanged IL (public vs internal binding only).

## INV-7 — Test counts & suite green (SC-006, FR-012)

- Full suite green (0 failures). Pass/skip counts change only by the **additive** Feature130 tests. Existing
  Feature126/Feature129 suites stay green (now exercising public surface; no edits required beyond compilation).

## INV-8 — Color policy deferral recorded (FR-003)

- `0004` records `ColorPolicy` as deliberately **not** promoted, with rationale (no `FS.GG.UI.Color` baseline;
  `--design-system` template param already exposes the choice; no consumer needs the engine public).
