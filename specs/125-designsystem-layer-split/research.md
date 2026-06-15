# Phase 0 Research — Design-System Layer Split (D1)

This phase resolves the technical unknowns and risk decisions the carve depends on. There were no
`NEEDS CLARIFICATION` markers in the Technical Context; the items below are the design decisions
that make a high-blast-radius, behaviour-neutral assembly split safe.

---

## R1 — Cross-assembly F# record-field inference (the load-bearing gotcha)

**Decision**: Preserve the **relative declaration order `ResolvedStyle` before `Theme`** inside the
new `FS.GG.UI.DesignSystem` package's types file, and have `Control.fs` (and every renderer file
with unannotated `theme.*` accesses) `open FS.GG.UI.DesignSystem`. Treat a **clean Release build +
green suite** as the proof. Where the compiler reports an ambiguous field (`Foreground`,
`FontFamily`, `FontSize` are shared between `Theme` and `ResolvedStyle`), add an explicit type
annotation at the use site with a one-line disclosure comment — never reorder fields or rename.

**Rationale**: Today the suite is green *because* `Types.fsi` declares `ResolvedStyle` immediately
before `Theme`, so F#'s "last-declared type wins for an ambiguous bare record field" rule binds
`theme.Foreground` to `Theme`. The existing `Style.fsi` header comment documents this precisely.
Both types move **together** into the same DesignSystem types file, so their relative order — the
thing the inference actually depends on — is preserved. The open risk is only whether
*cross-assembly* `open` ordering changes which type "wins" for code in the Controls assembly. This
is the single highest-likelihood compile break, so it is verified by build, not assumed.

**Alternatives considered**:
- *Annotate every `theme.*` access up front* — rejected as noisy and behaviour-irrelevant; only
  annotate where the compiler forces it (Principle III: simplest code that compiles).
- *Keep design-system types in `namespace FS.GG.UI.Controls`* (assembly move, no namespace move) —
  would sidestep most opens, but the spec explicitly requires the relocation + decision record
  (FR-008, SC-005, US3.4). Rejected.

---

## R2 — Namespace target for the relocated design-system types

**Decision**: Relocate the design-system types from `namespace FS.GG.UI.Controls` to
`namespace FS.GG.UI.DesignSystem`, and the default-theme + Theming surface to
`namespace FS.GG.UI.Themes.Default`. **No backward-source-compat shims** (`TypeForwardedTo`, type
aliases). Record the relocation in `docs/product/decisions/0003-designsystem-namespace-relocation.md`.

**Rationale**: Spec Assumption "No external consumers yet (pre-1.0, in-repo only)" makes a clean
relocation the cheapest correct option; the only consumers are the sample gallery, the template,
and the framework's own tests, all updatable in this same change. The namespace mirrors the package
identity (`FS.GG.UI.DesignSystem`), keeping the `FS.GG.UI.*` scheme from decision 0001 coherent.
The surface-drift gate plus the green suite prove no public capability is lost (SC-005) — only
relocated.

**Alternatives considered**:
- *Ship `TypeForwardedTo`/aliases for source-compat* — explicitly deferred by the spec assumption
  until a real external-consumer requirement exists; would add surface to baseline and muddy the
  "relocation only" evidence. Rejected for D1.

---

## R3 — Where each type/value lands (carve boundary)

**Decision**: Split today's single `Controls/Types.fsi`/`.fs` into a **design-system slice** that
moves and a **controls slice** that stays.

- **→ `FS.GG.UI.DesignSystem`** (the styling vocabulary): `ValidationState`, `VisualState`,
  `StyleVariant`, `StyleClass`, `ResolvedStyle`, `Theme`; plus the whole `DesignTokens` module and
  the `Style` (`resolve`) module.
- **→ `FS.GG.UI.Themes.Default`**: the `Theme` *value* module (`light`/`dark`/`withDensity`/
  `withAccent`/`resolve`), the `Theming` namespace (`ThemeMode`/`RolePalette`/`Theming.resolve`/
  `toTheme`), and the `design-tokens.tokens.json` source + generation tooling.
- **stays in `FS.GG.UI.Controls`**: every control-semantic type — `Control<'msg>`, `Attr`/
  `AttrValue`/`AttrCategory`, `ControlEvent`/`ControlEventBinding`/`ControlEventOrigin`/`NavPayload`,
  `ControlSchema`/`Standard*`/`Known*`, `AccessibilityMetadata` & friends (`AccessibilityRole`,
  `KeyboardOperation`, `ContrastEvidence`, `NavRange`, `CollectionPosition`), `ControlDiagnostic*`,
  `ControlRenderResult`, `ControlId`/`ControlKind`, `ChartPoint`/`ChartSeries`.

**Rationale**: The dependency arrows decide the boundary. `AttrValue` references `Theme`,
`StyleClass`, `VisualState`, `ValidationState` → those must compile *before* Controls, i.e. live in
DesignSystem (Controls depends on DesignSystem). `VisualState` references `ValidationState`, so
`ValidationState` must travel with it. `ResolvedStyle` and `Theme` reference only `Color` (Scene) →
they satisfy the "DesignSystem depends on Scene only" rule. The control-semantic types reference
`Scene`/`Layout` and each other but are never referenced *by* the design-system types, so they stay.

**Alternatives considered**:
- *Move `ContrastEvidence`/`AccessibilityMetadata` too* — rejected; they are accessibility/control
  concerns consumed by `Accessibility.fs`/`Control.fs`, not styling primitives, and the design
  system never references them.
- *Leave `ValidationState` in Controls* — impossible; `VisualState.Validation of ValidationState`
  would create a Controls→DesignSystem→Controls cycle. It moves.

---

## R4 — `Theme` record vs `Theme` module split across packages

**Decision**: The **`Theme` record type** lives in `FS.GG.UI.DesignSystem`; the **`Theme` module**
(`light`/`dark`/…) lives in `FS.GG.UI.Themes.Default`. F# permits a same-named type and module in
different namespaces/assemblies; the `[<CompilationRepresentation(ModuleSuffix)>]` on the module
already resolves the CLI name clash.

**Rationale**: Matches the layer semantics — the *shape* of a theme is a design-system primitive;
the *concrete Light/Dark values* are the default theme. `Theme.light = DesignTokens.Light` palette,
so the module depends on `DesignTokens` (in DesignSystem) → `Themes.Default → DesignSystem`, the
intended arrow. Consumers that today write `Theme.light` add `open FS.GG.UI.Themes.Default`; those
that name the `Theme` *type* add `open FS.GG.UI.DesignSystem`.

**Alternatives considered**:
- *Keep `Theme.light`/`dark` in DesignSystem* — would make DesignSystem ship concrete default
  values, blurring the "primitives vs concrete theme" boundary the phase exists to draw. Rejected.

---

## R5 — DTCG token source location vs the generated `DesignTokens` module

**Decision**: The **generated `DesignTokens.fs`/`.fsi`** is committed in the **DesignSystem**
package (it is the token model DesignSystem *exposes*). The **`design-tokens.tokens.json` source +
its generation/refresh tooling** move with **Themes.Default** (per the spec assumption). The
`RefreshSurfaceBaselines`/`DesignTokenDrift` generation step writes `DesignTokens.fs` into the
DesignSystem source dir from the JSON that now lives under Themes.Default; the drift check is
updated to the new source/output paths.

**Rationale**: This honours the spec's explicit assumption ("token source stays with the default
theme; the *generated* token model is what the design-system package exposes"), while keeping the
behaviour-neutral guarantee — the generated values are byte-identical, only their committed
location and the generator's input/output paths change. Workstream F later relocates the *source*
into DesignSystem; D1 does not.

**Risk/мitigation**: the cross-package generation path is the subtlest non-compile wrinkle. The
`DesignTokenDrift` test (and the refresh script) must be re-pointed in the **same** change, or CI
reddens. Captured as a task in D1.4 alongside the baseline work. If re-pointing the generator
across packages proves to expand scope, the fallback (recorded here, not chosen) is to keep the
JSON co-located with the generated module in DesignSystem for D1 and relocate it in F — but the
default follows the spec assumption.

---

## R6 — Surface-baseline & solution atomicity (the most likely CI-reddener)

**Decision**: In one commit: add two rows to `scripts/refresh-surface-baselines.fsx`
(`"FS.GG.UI.DesignSystem", "DesignSystem"` and `"FS.GG.UI.Themes.Default", "Themes.Default"`),
add both projects to `FS.GG.Rendering.slnx`, build Debug, run the refresh script, and commit the
**two new baselines + the regenerated (smaller) `FS.GG.UI.Controls.txt`** together.

**Rationale**: The refresh script derives the bin path as `src/<row-slug>/bin/Debug/net10.0/
<PackageId>.dll` and fails if a dll is missing, so the project folders MUST be named exactly
`DesignSystem` and `Themes.Default` with matching `AssemblyName`. The drift gate (`gate.yml` step
4) fails on any untracked/changed baseline, so the regeneration is part of *this* change, never a
follow-up (Edge Case "Drift gate on untracked baselines"; FR-007; SC-004).

**Alternatives considered**:
- *Defer baseline regen to a follow-up PR* — explicitly the single most likely way to redden CI;
  rejected by FR-007 and the plan's atomicity constraint.

---

## R7 — `Theme.Success`/`Theme.Warning` additive role fields

**Decision**: Add `Success: Color` and `Warning: Color` to the `Theme` record **during the move**,
sourced from the already-present `DesignTokens.{Light,Dark}.success`/`warning` tokens. The addition
is purely additive: `Theme.light`/`dark` set the new fields from the existing tokens; no existing
field value or render path changes.

**Rationale**: The tokens already define success/warning (see `DesignTokens.fsi`), and `Style.fs`
currently reads them directly as a workaround (per the plan report §7.1). Folding the roles into
`Theme` now is the natural home for them and is required by FR-004/US1.3 — but D1 only *adds the
fields*; no control is migrated to consume them (that is F). Render output stays identical because
nothing reads the new fields yet.

**Risk/mitigation**: Adding record fields changes the `Theme` constructor surface; every
construction site (`Theme.light`/`dark`, `Theming.toTheme`, and any test that builds a `Theme`
literal) must set them. The compiler enumerates these exhaustively — green build is the proof.
This is the only *shape* change in an otherwise pure move, so it is called out as the one place a
"behaviour-neutral" claim needs the additive-only argument (Edge Case "Theme record gains roles").

---

## R8 — Consumer-update strategy (blast radius)

**Decision**: Drive consumer fixes by the compiler: after the carve, every file that fails to
resolve a moved type gets `open FS.GG.UI.DesignSystem` (and `open FS.GG.UI.Themes.Default` where it
uses `Theme.light`/`dark`/`Theming`). Known consumer sets: `src/Controls.Elmish`, the
`Controls.Tests` and `Elmish.Tests` suites (~80 files), `SkiaViewer.Tests`, the `samples/
ControlsGallery` tree, and the `template/base` product + its `docs/api-surface/` snapshot.

**Rationale**: The opens are mechanical and behaviour-irrelevant; the green suite confirms no
semantic change. The `template/base/docs/api-surface/Controls/{Theme,Types}.fsi` snapshots and the
template product source must be regenerated/updated so the template pack/instantiate check stays
green (Edge Case "Sample gallery and template"; FR-008).

**Alternatives considered**:
- *Auto-`open` via `AutoOpen`/global usings* — rejected; hides the layer dependency the feature
  exists to make explicit, and is not idiomatic curated-`.fsi` style.

---

## Summary of resolved unknowns

| # | Question | Resolution |
|---|---|---|
| R1 | Will cross-assembly field inference still bind `theme.*` to `Theme`? | Preserve `ResolvedStyle`-before-`Theme` order in one DesignSystem types file; prove by build; annotate only where forced. |
| R2 | New namespace or keep `FS.GG.UI.Controls`? | Relocate to `FS.GG.UI.DesignSystem` / `FS.GG.UI.Themes.Default`; decision record; no shims. |
| R3 | Which types move vs stay? | Styling vocabulary + tokens + resolver move; all control-semantic types stay (see data-model). |
| R4 | `Theme` type vs module split? | Type → DesignSystem; value module → Themes.Default. |
| R5 | Where does the DTCG source live? | Generated module → DesignSystem; JSON source + generator → Themes.Default; re-point drift check. |
| R6 | How to keep the drift gate green? | Atomic: +2 script rows, +2 projects, regenerate + commit 2 new & 1 shrunk baseline in one change. |
| R7 | Add `Success`/`Warning` now? | Yes, additive from existing tokens during the move; nothing consumes them yet. |
| R8 | How to update ~85 consumers? | Compiler-driven `open`s; regenerate template api-surface snapshot. |
