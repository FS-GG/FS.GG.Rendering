# 0003. Design-system namespace relocation (no backward-source-compat shims)

**Status**: accepted
**Date**: 2026-06-15

## Decision

The design-system primitives and the default theme are carved out of the monolithic
`FS.GG.UI.Controls` assembly into two new, separately-referenceable packages, making the
four-layer architecture (`scene → design-system → theme/controls`) physically true:

- **`FS.GG.UI.DesignSystem`** (depends on `FS.GG.UI.Scene` only) — the token model
  (`DesignTokens`), the `Theme` record, `ResolvedStyle`, `StyleVariant`/`StyleClass`,
  `VisualState`/`ValidationState`, and the pure `Style.resolve` resolver.
- **`FS.GG.UI.Themes.Default`** (depends on `FS.GG.UI.DesignSystem` only) — the default
  Light/Dark `Theme` value module, the `Theming` mode/accent derivation (`ThemeMode`,
  `RolePalette`), and the DTCG `design-tokens.tokens.json` source.

The relocated types move to the matching namespaces — `FS.GG.UI.DesignSystem` and
`FS.GG.UI.Themes.Default` (the live-theming surface stays in the child namespace
`FS.GG.UI.Themes.Default.Theming` to keep its role field names out of `Theme` record-field
inference). `FS.GG.UI.Controls` keeps `Types`/`Attr`/`Control`/etc. and gains a single
`ProjectReference` to `DesignSystem`; it does **not** reference any theme package.

We **do not** ship backward-source-compat shims (no `TypeForwardedTo`, no namespace aliases). In-repo
consumers (`Controls.Elmish`, the test suites, the `ControlsGallery` sample, and the `template/`)
add `open FS.GG.UI.DesignSystem` (and `open FS.GG.UI.Themes.Default` where they use the default
theme/`Theming`) at the point of use.

## Rationale

- **Pre-1.0, in-repo-only consumers.** Every consumer of these types lives in this repository, so a
  clean namespace relocation is a single atomic change with no external breakage. Type-forwarding
  shims would be permanent public surface carried for no external audience.
- **Make the layer boundary physical, not documentary.** The "controls / design-system / themes /
  kits are distinct layers" clause becomes compiled structure: the acyclic graph
  (`DesignSystem → Scene`, `Themes.Default → DesignSystem`, `Controls → DesignSystem`) is enforced
  by the build — any forbidden back-edge fails to compile.
- **Behaviour-neutral.** No public type is removed (the surface-drift gate proves every relocated
  row reappears re-namespaced in the two new baselines), no render path changes, and the existing
  test suite passes unchanged. The only shape change is two **additive** `Theme` roles
  (`Success`/`Warning`), already sourced from existing tokens and read by no D1 render path.

## Revisit trigger

Reopen if a package is published for **external** consumers before a relocation: at that point a
deprecation/forwarding story (or a re-export shim) becomes worthwhile because breakage would no
longer be confined to this repo.

## Options considered

- **Clean namespace relocation, no shims (chosen)** — atomic, no permanent compat surface; the
  drift gate + green suite prove neutrality.
- **Relocate but keep `TypeForwardedTo`/aliases in `Controls`** — rejected: permanent public
  surface and an implicit `Controls → DesignSystem` re-export with no external audience to justify
  it pre-1.0.
- **Leave the types in `Controls`** — rejected: the layer boundary stays documentary; Workstream F
  and the D2/D3 theme work cannot depend on the primitives without dragging the whole control
  catalog.
