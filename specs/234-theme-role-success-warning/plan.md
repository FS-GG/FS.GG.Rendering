# Implementation Plan: theme-role Success/Warning

**Branch**: `234-theme-role-success-warning` · **Spec**: [spec.md](./spec.md) · **Issue**: FS-GG/FS.GG.Rendering#46

## Summary

Make the `Style` resolver read the active theme's own `Success`/`Warning` role colours (feature 125)
instead of string-matching `theme.Name = "dark"` against Default DTCG token literals, and carry the
resolved `ThemeMode` on `RolePalette` so `Theming.toTheme` projects a mode-correct framework `Theme`.

## Technical context

- **Stack**: F# 8 / .NET 10. Touched projects: `FS.GG.UI.DesignSystem` (`Style.fs`) and
  `FS.GG.UI.Themes.Default` (`Theming.fs`/`.fsi`).
- **Resolver seam**: `Style.successColor`/`warningColor` feed `applyVariant` (Success/Warning variants)
  and `applyValidation` (`Valid`→success stroke, `Pending`→warning stroke). Both are `module`-private
  (not in `Style.fsi`) — no public-surface change there.
- **Theme roles**: `Theme.Success`/`Theme.Warning` exist since feature 125; `Theme.light`/`Theme.dark`
  and `AntTheme.antLight`/`antDark` already populate them.
- **Live-theming**: `Theming.resolve mode accent -> RolePalette`; `Theming.toTheme palette -> Theme`.

## Constitution / governance check

- **No theme-identity branching (Feature 132 FR-014)**: removing `isDark` *strengthens* the guard that
  no code branches on `theme.Name`. ✔
- **FR-008 (colour from the theme)**: variant/state colours now originate from the `Theme` record, not
  inline `DesignTokens` literals. ✔
- **Public surface**: `RolePalette` gains a `Mode` field. The surface baseline is *type-name* level
  (`GetExportedTypes().FullName`), so no new type ⇒ baseline unchanged; the `.fsi` is updated for docs.
  `Style.fsi` unchanged. No cross-repo contract touched — ships in the next batched `fs-gg-ui`
  coherent-set release, no version bump in this feature merge. ✔

## Approach

1. **Style.fs** — delete `isDark`; `successColor theme = theme.Success`, `warningColor theme =
   theme.Warning`; correct the stale comment.
2. **Theming.fs** — add `Mode: ThemeMode` to `RolePalette`; `resolve` sets it; factor a private
   `baseThemeFor mode`; `toTheme` seeds `baseThemeFor palette.Mode` then overwrites the neutral roles.
3. **Theming.fsi** — document the new `Mode` field and correct the `toTheme` "carry from `Theme.light`"
   note.
4. **Tests** — new `tests/Controls.Tests/Feature234ThemeRoleColorTests.fs`: custom-named theme honours
   its own Success/Warning; Ant dark resolves from its fields; Default light/dark unchanged; Dark
   `toTheme` projects dark Success/Warning/Name (fail-before/pass-after).
5. **Verify** — full Controls.Tests (includes Feature 108 theming + Feature 132 theme parity).

## Files

| File | Change |
|---|---|
| `src/DesignSystem/Style.fs` | Read `theme.Success`/`theme.Warning`; drop `isDark`. |
| `src/Themes.Default/Theming.fs` | `RolePalette.Mode`; mode-seeded `toTheme`. |
| `src/Themes.Default/Theming.fsi` | Document `Mode`; correct `toTheme` doc. |
| `tests/Controls.Tests/Feature234ThemeRoleColorTests.fs` | New regression suite (+ fsproj entry). |
| `specs/234-theme-role-success-warning/*` | Spec Kit artifacts. |

## Risks & mitigations

- **`RolePalette` additive field breaks a constructor** → only `Theming.resolve` constructs it
  (grep-verified); no external site.
- **`toTheme` `Name`/non-colour fields change** → Light/Dark share density/radius/contrast/font, so only
  `Name` + `Success`/`Warning` change in practice; `Name` isn't a behavioural switch anywhere (the sole
  reader, `Style.isDark`, is removed). The fragment-reuse key uses the static `host.Theme`.
- **Ant seed == Default light tokens** → the Ant case is not a *visible* regression (documented in the
  test); the visible fixes are custom themes and Dark `toTheme`.

## Out of scope

- A first-class `Theme.FocusRing` field (RolePalette's `FocusRing` stays dropped by `toTheme`; it equals
  `Accent` today, so no behavioural bug — a separate enhancement).
- Any change to `AntTheme`, `DesignTokens`, or the retained-render / cache paths.
