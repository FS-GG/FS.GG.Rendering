---
name: fs-gg-design-system
description: Work on the design-token / theme / state-style-resolver pipeline — the DTCG token source, generated token modules, built-in themes and the live-theming primitive, and the pure StyleResolver that turns theme + kind + intent + classes + state into a ResolvedStyle.
---

# Design System Capability

## Scope

Owns `src/DesignSystem/` (the token taxonomy, the `Theme`/`ResolvedStyle`/`VisualState` types, and the
pure state→style resolver) **and** `src/Themes.Default/` (the DTCG token source-of-truth, the built-in
`light`/`dark` themes, and the live-theming primitive). Together these are **one pipeline**:

```
design-tokens.tokens.json (DTCG source, in Themes.Default)
   └─► generated token modules  (DesignTokens / DesignTokensExt, in DesignSystem)
         └─► Theme  (light / dark / withDensity / withAccent — Themes.Default)
               └─► Style.resolve / StyleResolver.resolve  (theme + kind + intent + classes + state → ResolvedStyle)
                     └─► the migrated FS.GG.UI controls apply the ResolvedStyle (paint + typography only)
```

The **per-control geometry** and the **control set itself** are NOT here — they live in `Controls`
(see [[fs-gg-ui-widgets]]). The resolver governs **paint and typography only**; geometry stays computed
in the controls. Ant-pattern *advice* (which token maps to which Ant region) lives in
[[fs-gg-ant-design]]; this skill owns the machinery those patterns target.

## Public Contract

- **Types** (`FS.GG.UI.DesignSystem`, `src/DesignSystem/Types.DesignSystem.fsi`):
  - `ValidationState` = `Valid | Invalid of string | Pending of string`.
  - `VisualState` = `Normal | Disabled | Hover | Pressed | Focused | FocusedHover | Selected | Loading
    | Validation of ValidationState` — the eight-case interaction state the resolver folds (note
    `FocusedHover`, the combined hover+focus state from feature 175).
  - `StyleVariant` (`[<RequireQualifiedAccess>]`) = `Primary | Danger | Ghost | Neutral | Success |
    Warning` — the **closed**, compiler-checked set of built-in semantic variants.
  - `StyleClass` = `Variant of StyleVariant | Custom of string` — one attached class; a control carries
    a `StyleClass list` whose **list position is the attach order** the resolver folds left-to-right.
  - `ResolvedStyle` = flat record `{ Foreground; Fill; Stroke; StrokeWidth; FontFamily; FontSize;
    FontWeight }` — the per-control paint/typography output (flat so precedence is last-writer-wins
    per field and parity is a structural record compare). **Declared before `Theme`** on purpose so the
    shared field names resolve to `Theme` for bare `theme.*` accesses.
  - `Theme` = `{ Name; Foreground; Background; Accent; Danger; Success; Warning; Muted; FontFamily;
    FontSize; Density; CornerRadius; ContrastRequiredRatio }` — the active palette + metrics.
- **Generated token modules** (`FS.GG.UI.DesignSystem`):
  - `DesignTokens` (`DesignTokens.fsi`) — the flat primitives under `DesignTokens.Light.*` /
    `DesignTokens.Dark.*` (`foreground`/`background`/`accent`/`danger`/`success`/`warning`/`muted`,
    `fontFamily`/`fontSize`/`density`/`cornerRadius`/`contrastRequiredRatio`). These feed `Theme.light`
    / `Theme.dark`.
  - `DesignTokensExt` (`DesignTokensExt.fsi`) — the Ant-derived taxonomy: `Seed` → `Map` (Light/Dark) →
    `Alias` (Light/Dark) → `Component` (Button/Input/Table/Tabs/Menu) plus `Space`, `Density`, `Type`,
    `Elevation`. Token *references* are greppable (e.g. `DesignTokensExt.Seed.colorPrimary`).
- **Resolver** (`FS.GG.UI.DesignSystem`):
  - `Style.resolve : theme -> baseStyle -> classes -> state -> ResolvedStyle` (`Style.fsi`) — the pure,
    total, deterministic back-half overlay. Precedence (last-writer-wins per field): `baseStyle` < each
    class in attach order (earlier < later) < the visual state.
  - `StyleResolver` (`StyleResolver.fsi`) — the front half: `baseStyleFor theme kind`, the overridable
    `IntentPolicy` (`{ ApplyIntent }`) with the identity `neutralPolicy`, the full
    `resolve policy theme kind intent classes state`, and `resolveDefault theme kind intent classes
    state` (the intent-neutral path control render code calls).
- **Themes + live-theming** (`FS.GG.UI.Themes.Default`):
  - `Theme` module (`Theme.fsi`) — `light`, `dark`, `withDensity density theme`, `withAccent accent
    theme`, and `resolve overrides` (the caller's `Theme option` else `light`).
  - `Theming` (child namespace `FS.GG.UI.Themes.Default.Theming`, `Theming.fsi`) — `ThemeMode = Light |
    Dark`, the `RolePalette` record (`Background`/`Foreground`/`Accent`/`Danger`/`Muted`/`FocusRing`),
    `Theming.resolve mode accent -> RolePalette`, and `Theming.toTheme palette -> Theme` (projects the
    role palette back onto the framework `Theme`).

Surface changes require regenerating the matching baselines under `readiness/surface-baselines/`
(`FS.GG.UI.DesignSystem.txt`, `FS.GG.UI.Themes.Default.txt`, and — when the Ant taxonomy changes —
`FS.GG.UI.Themes.AntDesign.txt`) with **zero drift** on every other baseline.

## The token pipeline — never hand-edit a generated file

`design-tokens.tokens.json` (in `src/Themes.Default/`) is the **DTCG single source of truth**. Two
generated F# artifacts derive from it, and **both are GENERATED — do not edit by hand**:

- `src/DesignSystem/DesignTokensExt.fs` + `.fsi` — regenerate with
  `dotnet fsi scripts/generate-design-tokens.fsx` (run with `--check` to fail when the committed file is
  stale; the generator's `--check` covers **both** the `.fs` and `.fsi`).
- `src/DesignSystem/DesignTokens.fs` (the flat primitive module) — regenerated via
  `./fake.sh build -t RefreshSurfaceBaselines`; currency is enforced by the `DesignTokenDrift` check.

To change a colour / metric: **edit the DTCG JSON, then regenerate** — never touch the `.fs`. The
curated `.fsi` files are the sole public-surface declaration (Principle II); the generated `.fs` carries
no access modifiers.

## How styling actually resolves (the precedence model)

`Style.resolve` is the heart. It folds, **last-writer-wins per `ResolvedStyle` field**:

1. **`baseStyle`** — the kind's structural default (`StyleResolver.baseStyleFor theme kind`: `"icon-button"`
   → accent outline; any other kind → filled accent — a defined, visible fallback, never empty or an
   exception).
2. **each `StyleClass` in attach order** — earlier classes are overridden by later ones; a `Variant`
   pulls colours from the `theme`, a `Custom` string that no policy recognises contributes an **identity
   delta** (never an exception, never a silent drop).
3. **the `VisualState`** — wins over any class for the same field.

Guarantees to preserve when editing the resolver:

- **Total + deterministic** over every `(Theme, ResolvedStyle, StyleClass list, VisualState)` — all eight
  visual states, every variant, any custom string.
- **Identity at the default path**: `Style.resolve theme baseStyle [] Normal = baseStyle` **exactly**
  (the parity proof). `resolveDefault` is byte-identical to the pre-promotion internal call.
- **No inline colour literals** — every colour a layer reads originates from the `theme` (which is
  DTCG-generated). New semantic colours come from the token source, not a literal in the resolver.
- **No selectors / specificity / cross-control cascade** — these are permanent roadmap non-goals. Style
  is a per-control fold, not a CSS engine.

Intent divergence (e.g. a "danger" intent recolouring without per-control edits) is expressed by
swapping the `IntentPolicy` at `StyleResolver.resolve`, **not** by branching inside controls.

## Theming — built-in themes and the live primitive

- Build a palette from the built-ins: `Theme.light` / `Theme.dark`, then `Theme.withDensity` /
  `Theme.withAccent` for compact/branded variants; `Theme.resolve overrides` picks the caller's theme
  else `light`.
- The **live-theming primitive** (`Theming`) is for runtime mode+accent switching: `Theming.resolve mode
  accent` seeds neutral/structural roles from the matching base theme and overrides `Accent`/`FocusRing`
  with the accent, then `Theming.toTheme` projects the `RolePalette` onto a framework `Theme` (the exact
  "paint theme" handed to the render path; non-colour fields carry from `Theme.light`).
- **Namespace placement is deliberate.** `RolePalette`/`Theming` live in the **child** namespace
  `FS.GG.UI.Themes.Default.Theming` so the role field names (`Background`/`Foreground`/`Accent`/…) are
  not auto-in-scope during theme/consumer compilation, where they would poison `Theme` record-field
  inference. Consumers `open FS.GG.UI.Themes.Default.Theming` explicitly. Keep new theming surface in the
  child namespace for the same reason.

## Layering rules (do not break these)

- **One semantic control set styled by themes — no per-theme control forks.** There is no `AntButton`
  behaviour copy; Ant styling is a theme over the single control set (the Ant *concept* of named regions
  + tokens-as-materials, not its React/DOM mechanism). See [[fs-gg-ant-design]].
- **Resolver = paint + typography only.** Geometry stays computed in the controls; do not push geometry
  into `ResolvedStyle`.
- **`StyleVariant` is closed; free-form lives one level up** in `StyleClass.Custom`. Add a built-in
  variant only when it is genuinely common and theme-sourced.
- **Colour pairings are validated by `ColorPolicy`** (`wcag` / `ant`) at the control/test layer — the
  theme carries `ContrastRequiredRatio`; honour it rather than shipping a failing pairing.

## Build Commands

`dotnet build src/DesignSystem/DesignSystem.fsproj` and
`dotnet build src/Themes.Default/Themes.Default.fsproj`.

## Test Commands

The design-system surface is exercised from the control package tests (there is no standalone
`DesignSystem.Tests` project): `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` (resolver
precedence / parity — e.g. the Feature093 retained-state and Feature130 public-surface suites) and
`dotnet test tests/Package.Tests/Package.Tests.fsproj` (packaged-surface checks). Token currency:
`dotnet fsi scripts/generate-design-tokens.fsx --check`.

## Evidence

Public surface baselines live under `readiness/surface-baselines/`
(`FS.GG.UI.DesignSystem.txt`, `FS.GG.UI.Themes.Default.txt`, `FS.GG.UI.Themes.AntDesign.txt`). Token
currency is proven by the generator's `--check` and the `DesignTokenDrift` check; resolver parity is the
structural `ResolvedStyle` comparison (`resolve theme baseStyle [] Normal = baseStyle`).

## Package Boundary

`FS.GG.UI.DesignSystem` references **only** `FS.GG.UI.Scene` (for `Color`). `FS.GG.UI.Themes.Default`
references `DesignSystem` (+ `Scene`) and is the home of the DTCG source. Neither references `Controls`,
`SkiaViewer`, or any host/IO — controls depend on the design system, never the reverse. The `Theming`
primitive deliberately adds **no** `Color` package dependency (its WCAG reuse is test-only).

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs, the DTCG spec, and Ant Design's own token
docs via the [[fs-gg-ant-design]] hub), then community sources. Record findings and resolving links in
the feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's **Sources**
line. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-ant-design]] supplies the Ant pattern → token/region/resolver advice this machinery targets.
- [[fs-gg-ui-widgets]] consumes `ResolvedStyle` to paint the single semantic control set.
- [[fs-gg-scene]] supplies `Color` (the only dependency of the token/theme layer).
- [[fs-gg-layout]] owns geometry/metrics consumption that the resolver deliberately does not.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Design Tokens (DTCG) format: https://design-tokens.github.io/community-group/format/
- Ant Design tokens (via the hub): `docs/product/ant-design/reference/ant-llms-sources.md`
