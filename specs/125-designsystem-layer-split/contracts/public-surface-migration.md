# Contract — Public-Surface Migration (relocations only, no removals)

This is the consumer-facing contract for the **namespace relocation**: every public symbol that is
referenceable today stays referenceable after the split, possibly under a new namespace, and
nothing is dropped or weakened (FR-012 / SC-005). The drift-gate baselines are the machine-checked
form of this table.

## Before → after

| Symbol (today, `FS.GG.UI.Controls`) | After: package | After: namespace |
|---|---|---|
| `Theme` (type) | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `ResolvedStyle` | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `StyleVariant` | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `StyleClass` | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `VisualState` | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `ValidationState` | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `DesignTokens` (+ `Light`/`Dark`) | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `Style` (`resolve`) | `FS.GG.UI.DesignSystem` | `FS.GG.UI.DesignSystem` |
| `Theme` (module: `light`/`dark`/`withDensity`/`withAccent`/`resolve`) | `FS.GG.UI.Themes.Default` | `FS.GG.UI.Themes.Default` |
| `ThemeMode` | `FS.GG.UI.Themes.Default` | `FS.GG.UI.Themes.Default` |
| `RolePalette` | `FS.GG.UI.Themes.Default` | `FS.GG.UI.Themes.Default` (child-namespace isolation preserved) |
| `Theming` (`resolve`/`toTheme`) | `FS.GG.UI.Themes.Default` | `FS.GG.UI.Themes.Default` |

> Today `Theming` already lives in the child namespace `FS.GG.UI.Controls.Theming`; it relocates to
> the `Themes.Default` package keeping an equivalent child-namespace isolation so it does not poison
> `Theme` field inference where the theme package constructs themes.

## Additive change (not a relocation)

| Symbol | Change | Compat impact |
|---|---|---|
| `Theme.Success : Color` | **new field** | additive; no existing field changes; no consumer breaks at the type level (record construction sites within the repo are updated in this change) |
| `Theme.Warning : Color` | **new field** | additive; sourced from existing `DesignTokens.*.warning` |

## Consumer migration (what a caller does)

- Code that names a moved **type** (`Theme`, `ResolvedStyle`, `VisualState`, `StyleVariant`,
  `StyleClass`, `ValidationState`, `ResolvedStyle`) or `DesignTokens`/`Style.resolve`:
  add `open FS.GG.UI.DesignSystem`.
- Code that uses the default **theme values** (`Theme.light`/`Theme.dark`/`Theme.withAccent`/…) or
  `Theming`: add `open FS.GG.UI.Themes.Default`.
- No symbol is renamed; no signature changes (besides the two additive `Theme` fields). The
  migration is purely an added `open`.

## Verification

- **Drift gate**: `FS.GG.UI.Controls.txt` shrinks by exactly the relocated rows; those rows appear
  (re-namespaced) in `FS.GG.UI.DesignSystem.txt` / `FS.GG.UI.Themes.Default.txt`. A line-level
  before/after diff shows **relocations only, zero removals** (SC-005).
- **Build + full suite green** after consumers add the opens — no behaviour change (FR-005, SC-001).
- **Decision record** `docs/product/decisions/0003-designsystem-namespace-relocation.md` states the
  relocation and the no-shim rationale (FR-008); `template/` and bridge/migration docs reflect it.
