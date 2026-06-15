# Phase 0 — Research: Visual-State Style Layer (Feature 093)

This is a **backfill**: the implementation already ships. Rather than resolving forward-looking
unknowns, this document recovers the design decisions embodied in the imported code and confirms they
are coherent with the spec and constitution. Each decision is recorded as Decision / Rationale /
Alternatives considered.

## D1 — A single pure resolver, not per-kind procedural styling

- **Decision**: One function `Style.resolve : Theme → ResolvedStyle → StyleClass list → VisualState →
  ResolvedStyle` (`Style.fs:83`) folds all styling inputs into one flat record. Migrated controls
  call it once and read `style.Fill`/`style.Stroke`/`style.Foreground` (etc.) instead of reading
  `theme.Accent`/`theme.Danger`/`theme.Muted` inline.
- **Rationale**: Centralizing the fold makes styling intent (which variant? which state?) explicit
  and uniformly validatable and re-themeable. A theme swap re-paints every control consistently
  because all colours flow through one token-sourced path.
- **Alternatives considered**: Keeping inline per-kind styling (rejected: intent stays implicit and
  scattered, impossible to property-test or re-theme uniformly); a CSS-like selector/cascade engine
  (rejected as a permanent non-goal — see D6).

## D2 — Fixed precedence as a `List.fold` then one state application

- **Decision**: `classes |> List.fold (fun acc cls -> applyClass theme cls acc) baseStyle |>
  applyState theme state` (`Style.fs:83-86`). Precedence is therefore, last-writer-wins per field:
  `baseStyle < each class in attach order (earlier < later) < current visual state`.
- **Rationale**: A left fold makes "list position **is** attach order" literally true and makes the
  state layer provably outermost (it is applied after the entire class fold). This is exactly the
  shape the property suite proves (SC-004: applying classes then state equals re-resolving the
  class-folded style under that state with no classes).
- **Alternatives considered**: A priority/specificity number per class (rejected: reintroduces the
  specificity algebra ruled out in D6); merging via a per-field "most specific wins" (rejected: not
  expressible as a plain structural record comparison, which the parity proof relies on).

## D3 — `ResolvedStyle` is a flat 7-field record; geometry is excluded

- **Decision**: `ResolvedStyle = { Foreground; Fill; Stroke; StrokeWidth; FontFamily; FontSize;
  FontWeight }` (`Types.fs:207-214`) — paint and typography only. Geometry (sizes, paddings,
  hit-boxes) is computed by each control as before and is **not** part of `ResolvedStyle`.
- **Rationale**: A flat record makes last-writer-wins a trivial record-update per field and makes the
  parity proof a plain structural record comparison. Excluding geometry keeps the migration additive:
  the resolver governs paint/typography, the control keeps owning layout, so default output stays
  structurally scene-equal to the procedural baseline.
- **Alternatives considered**: A nested/partial style (`Map<field, value>` deltas) — rejected: a
  `Map` would complicate the identity proof (`resolve theme base [] Normal = base`) and the
  field-level last-writer-wins comparison.

## D4 — Totality: closed variant set, `Custom` identity delta, all eight states matched

- **Decision**: `StyleVariant` is a closed `[<RequireQualifiedAccess>]` DU of six cases
  (`Primary`/`Danger`/`Ghost`/`Neutral`/`Success`/`Warning`, `Types.fs:195-201`); `applyVariant` is a
  total match over it. An unknown `Custom` name flows through `applyCustom` (`Style.fs:44`) to an
  **identity delta** (returns the style unchanged) — never an exception, never a dropped field.
  `applyState` (`Style.fs:71`) matches all eight `VisualState` cases, with `Validation` delegated to
  `applyValidation` over its three `ValidationState` cases.
- **Rationale**: Totality is a hard requirement (FR-002/FR-004) and is what makes the resolver safe
  on the live path (Principle VI: no silent failure). The closed variant DU gives compiler-checked
  exhaustiveness; the `Custom` escape hatch stays harmless rather than fatal.
- **Alternatives considered**: Throwing on an unknown `Custom` (rejected: makes consumer typos
  fatal); an open string-keyed variant table (rejected: loses compiler exhaustiveness on the
  built-ins).

## D5 — `Loading` inherits `Normal`; `resolve … [] Normal = base` is a strict identity

- **Decision**: The default no-class/`Normal` path returns `baseStyle` unchanged, and `Loading`
  deliberately produces the `Normal` paint (parity preservation), per `applyState`.
- **Rationale**: FR-005/SC-003 require structural scene-equal parity for the default case, so `Normal`
  must be a strict identity. `Loading` reusing `Normal` keeps a control's loading frame visually stable
  with its resting frame, preserving the pre-refactor look.
- **Alternatives considered**: A distinct `Loading` shimmer/desaturation (rejected here: it would
  break parity and belongs to later design-system work, Workstream F).

## D6 — Single-control styling only; no selectors, specificity, or cascade

- **Decision**: The resolver operates on one control's own `(classes, state)`; there is no selector
  matching, no specificity algebra, and no cross-control cascade. These are **permanent roadmap
  non-goals**, not deferrals (spec Assumptions).
- **Rationale**: Keeps the resolver pure, total, and trivially testable, and keeps Principle III
  (idiomatic simplicity) honest. The design-system arc (Workstreams F/D, features 095/096) enriches
  *this* resolver and the slot/state vocabulary rather than adding a cascade.
- **Alternatives considered**: A web-style cascade (rejected as out of product scope and a
  complexity multiplier with no current consumer need).

## D7 — Colours strictly from DTCG tokens; `ResolvedStyle` declared before `Theme`

- **Decision**: Every colour the variant/state layers emit is read from `theme.*`
  (`Accent`/`Danger`/`Muted`/`Background`/`Foreground`) or `DesignTokens.*` (e.g.
  `DesignTokens.Dark.success`) — no inline literals (FR-008). `ResolvedStyle` is declared on
  `Types.fsi` **before** `Theme` so the overlapping bare field names
  (`Foreground`/`FontFamily`/`FontSize`) resolve to `Theme` at the many unannotated `theme.*` render
  sites (documented in the `Style.fsi` header comment).
- **Rationale**: Token-only sourcing is what makes a theme swap re-paint consistently (SC-006); the
  declaration-order trick avoids annotating every `theme.*` access while keeping both records public.
- **Alternatives considered**: Annotating every render-site access (rejected: noisy, touches many
  call sites); merging the two records (rejected: they are genuinely distinct concepts).

## D8 — Verification by structural scene equality against a frozen procedural oracle

- **Decision**: Parity (SC-003) is judged by **structural scene equality** between the
  resolver-driven render and a frozen inline reproduction of the pre-refactor `buttonGeom`/
  `checkboxGeom` geometry (`Feature093ParityTests.fs`), which also writes the six
  `readiness/parity/*.scene.txt` artifacts. The property suite proves purity/determinism/outermost-
  state over ≥1000 `Gen093`-generated inputs. State survival (SC-005) is proven through the live
  `RetainedRender.init`/`step` path, not a hand-seeded state map.
- **Rationale**: This is the same oracle technique `DesignTokenParityTests` uses; it proves the
  contract without a GL context. Disclosed limitation: it proves *structural* equality, **not** pixel
  output or desktop visibility.
- **Alternatives considered**: Pixel-diff/golden-image tests (rejected: require a GL surface and are
  out of this feature's scope); a hand-seeded state map for survival (rejected: would not prove the
  state actually rides the keyed reconciler diff).

## Resolved unknowns

All Technical-Context items are concrete (recovered from shipped code) — **no NEEDS CLARIFICATION
remain**. The only open items are the two recorded deviations and one follow-up:

- **DF-1** (Tier-2 follow-up): strip the redundant `private` modifiers from `Style.fs`'s helpers now
  that FS0078-as-error makes `.fsi` the visibility authority.
- **DF-2** (bounded follow-up): add frozen-oracle parity coverage for the four migrated kinds beyond
  Button/CheckBox (`RadioGroup`/`Slider`/`Switch`/`TextBox`), which currently rely on the
  totality/purity proofs but have no parity scene.
