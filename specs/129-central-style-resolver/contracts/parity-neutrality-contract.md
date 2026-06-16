# Contract — Parity / Neutrality / Totality / Divergence Gates

The automated evidence (FR-010) that F4 is behaviour-neutral, total, and seam-capable. All gates
run in the headless deterministic tier (no GL) in
`tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`.

## Parity oracle

The **pre-migration rendering is the oracle**. Because the old `primary: bool` path is removed,
the test encodes the oracle as the literal pre-migration `baseStyle` records (filled / outline,
copied from `Control.fs:823–839`) plus the unchanged `Style.resolve`. Parity = byte-equality of
`ResolvedStyle` (and of the emitted `Scene` for the rendered button) between:

- **Oracle**: `Style.resolve theme <literal structural base> classes state`
- **Migrated**: `StyleResolver.resolveDefault theme kind intent classes state`

## Gates

| Gate | Assertion | Requirements |
|------|-----------|--------------|
| **G1 Parity (style)** | For the default `light` (and `dark`) theme, for `kind ∈ {button, icon-button}`, for every `intent ∈ {primary, secondary, danger, ghost}`, for every `state ∈` the 8 `VisualState` cases (incl. a representative `Validation`): migrated `ResolvedStyle` **byte-equals** oracle. | FR-003, SC-001 |
| **G2 Parity (scene)** | The rendered button `Scene` for the representative set is byte-identical to the pre-migration scene (filled rect+text / outline stroke+text). | FR-003, SC-001 |
| **G3 Totality** | The full cross-product `{kinds incl. an unknown/`Custom` kind} × {intents incl. an unknown string} × {8 states}` returns a concrete `ResolvedStyle` with **zero exceptions**; two repeated runs are equal (determinism). | FR-004, SC-003 |
| **G4 Intent consumed** | Under a non-default policy that maps `danger` to `theme.Danger`, `resolve … "danger" …` ≠ `resolve … "primary" …`; under `neutralPolicy` they are **equal** (proving today's drop is preserved by default). | FR-002, FR-005, SC-002, SC-007 |
| **G5 Divergence without control edits** | G4's divergence is produced by calling `StyleResolver.resolve divergentPolicy …` directly — **no edit to any control render function**; control-type count unchanged. | FR-008, SC-006, SC-007 |
| **G6 Surface neutrality** | `dotnet fsi scripts/refresh-surface-baselines.fsx` (or `--check`) leaves `tests/surface-baselines/*.txt` unchanged (`git status --porcelain` empty); design-token-drift gate green. | FR-007, SC-004 |
| **G7 Suite integrity** | Full-suite pass/skip counts unchanged vs. pre-F4; no test removed/skipped/weakened. | FR-010, SC-005 |
| **G8 Render-loop neutrality** | Animation/layout/memoization/virtualization/cache/fingerprint metrics for identical inputs are unchanged (the resolver touches style assembly only — no edits to 097/099/103/113/114/116/117/120/121 seams). | FR-011, SC-008 |

## Non-goals (asserted out of scope)

- No visible intent divergence under any default/Ant theme (D2 owns that).
- No public-surface promotion (F5).
- No `Theme` shape change.
- No control migration beyond the button (others keep calling `Style.resolve` directly).
