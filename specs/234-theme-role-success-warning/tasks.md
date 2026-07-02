# Tasks: theme-role Success/Warning

**Feature**: `234-theme-role-success-warning` · **Issue**: FS-GG/FS.GG.Rendering#46

Ordered, dependency-aware. `[X]` = done.

## Phase 1 — Investigation

- [X] T001 Confirm the resolver seams onto success/warning (`applyVariant`, `applyValidation`) and that
  `successColor`/`warningColor`/`isDark` are module-private. → `research.md` R1/R2.
- [X] T002 Confirm every built-in/Ant theme populates `Theme.Success`/`Theme.Warning`. → `research.md` R3.
- [X] T003 Establish which cases are *visibly* broken (custom themes, Dark `toTheme`) vs coincidentally
  fine (Ant seed == Default light tokens). → `research.md` R4.
- [X] T004 Confirm `RolePalette` has a single constructor and the surface baseline is type-level (added
  field is source- and gate-safe). → `research.md` R6.

## Phase 2 — Implementation (US1, P1)

- [X] T005 `src/DesignSystem/Style.fs`: `successColor theme = theme.Success`, `warningColor theme =
  theme.Warning`; delete `isDark`; correct the stale comment (FR-001/FR-002/FR-006).
- [X] T006 `src/Themes.Default/Theming.fs`: add `RolePalette.Mode: ThemeMode`; `resolve` sets it; factor
  `baseThemeFor`; `toTheme` seeds `baseThemeFor palette.Mode` then overwrites neutrals (FR-003/FR-004).
- [X] T007 `src/Themes.Default/Theming.fsi`: document `Mode`; correct the `toTheme` doc (FR-006).
- [X] T008 Build DesignSystem, Themes.Default, Themes.AntDesign, Controls — clean.

## Phase 3 — Tests & verification

- [X] T009 Add `tests/Controls.Tests/Feature234ThemeRoleColorTests.fs` (+ fsproj entry): custom-named
  theme honours its Success/Warning; Ant dark resolves from its fields; Default light/dark unchanged;
  Dark `toTheme` projects dark Success/Warning/Name (fail-before/pass-after; SC-001/SC-002/SC-003).
- [X] T010 Run full Controls.Tests (includes Feature 108 theming + Feature 132 theme parity) — green.

## Phase 4 — Artifacts & delivery

- [X] T011 Write spec / plan / research / fsi-surface-deltas / tasks.
- [X] T012 Squash-merge to `main`, update the Coordination board item to Done, push. Closes #46.
