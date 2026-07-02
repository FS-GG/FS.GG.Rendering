# Research: theme-role Success/Warning

## R1 — Where success/warning are resolved

`src/DesignSystem/Style.fs`: `successColor`/`warningColor` (module-private) feed:
- `applyVariant` → `StyleVariant.Success` (Fill+Stroke) and `StyleVariant.Warning` (Fill+Stroke).
- `applyValidation` → `Valid` (success stroke) and `Pending` (warning stroke).

These are the only two seams; both are private (absent from `Style.fsi`), so changing their bodies is
not a public-surface change.

## R2 — `isDark` usage

`isDark` is referenced ONLY by `successColor`/`warningColor` (repo-wide grep). Removing it and reading
`theme.Success`/`theme.Warning` eliminates the last `theme.Name` behavioural branch in the codebase —
the only other `.Name` reads (`Controls.Elmish/ControlsElmish.fs`) are *attribute* names, unrelated to
theme identity.

## R3 — Do the themes populate Success/Warning?

Yes. `Theme.light`/`Theme.dark` (`src/Themes.Default/Theme.fs`) set Success/Warning from
`DesignTokens.Light/Dark`. `AntTheme.antLight`/`antDark` (`src/Themes.AntDesign/AntTheme.fs`) set them
from `DesignTokensExt.Seed.colorSuccess/colorWarning`. So reading `theme.Success`/`theme.Warning` is
always well-defined.

## R4 — Was AntDesign actually mis-rendered?

Partially no. `DesignTokens.Light.success = (21,128,61)` is byte-identical to the Ant seed
`colorSuccess = (21,128,61)` (and light warning `(180,83,9)` == Ant warning). So the old string-match
bug happened to resolve the *same* colour for AntDesign — not a visible Ant regression. The visible
breakages are:
- **Custom themes** with bespoke Success/Warning → silently replaced by Default light tokens.
- **`Theming.toTheme (resolve Dark _)`** → `Name` pinned to "light", so a Dark live-theme resolved
  Default **light** success/warning (`(21,128,61)`/`(180,83,9)`) instead of Dark (`(74,222,128)`/
  `(251,191,36)`).

The regression tests target the visible cases; the Ant test is a consistency check.

## R5 — `Theming.toTheme` mode loss

`resolve mode accent` seeds neutrals from `Theme.light`/`Theme.dark` but returns a `RolePalette` that
dropped the mode; `toTheme` then unconditionally seeded from `Theme.light`. Fix: carry `Mode:
ThemeMode` on `RolePalette` and seed `toTheme` from `baseThemeFor palette.Mode`. Light/Dark DTCG tokens
share density=1.0, cornerRadius=4.0, contrastRequiredRatio=4.5 (and font), so mode-based seeding changes
only `Name` + `Success`/`Warning` for the neutral-overwritten `Theme` in practice.

Considered and rejected: adding only `Success`/`Warning` to `RolePalette` (narrower, but `Mode` also
fixes `Name` and future mode-scoped fields, and reads more clearly as "the palette knows its mode").

## R6 — `RolePalette` surface change

`RolePalette` is public (`Themes.Default.Theming`). Adding `Mode` is additive at the record level. The
surface baseline (`readiness/surface-baselines/FS.GG.UI.Themes.Default.txt`) is validated at *type
FullName* granularity (`GetExportedTypes`) — no new type, so the baseline is unchanged and the drift
gate stays green. Only `Theming.resolve` constructs a `RolePalette` (grep-verified), so the required
field is source-safe.

## R7 — FocusRing (deliberately out of scope)

`RolePalette.FocusRing` is computed (`= accent`) but the `Theme` record has no `FocusRing` field, so
`toTheme` drops it. Today the focus ring is painted from `theme.Accent` (Style `Focused`/`FocusedHover`)
and `resolve` sets `FocusRing = accent = Accent`, so the drop is a no-op. Wiring a first-class
`Theme.FocusRing` (record field + all theme constructors + resolver `Focused` state) is a genuine
enhancement but out of scope for this correctness fix; captured here as a follow-up candidate.
