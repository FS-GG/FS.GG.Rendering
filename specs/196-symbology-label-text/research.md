# Phase 0 Research: Symbology Label / Glyph Text Channel

All NEEDS CLARIFICATION from the Technical Context are resolved below. Each item records the **Decision**, **Rationale**, and **Alternatives considered**, grounded against the tree on 2026-06-25.

## R1. Where the label lives — field shape on `Token`

- **Decision**: Add **one optional field** `Label: string option` to the existing `Token` record in `src/Symbology/Symbology.fsi`/`.fs`, defaulting to `None` in `defaultToken`. No new type, no `ChannelMap` change, no new grammar.
- **Rationale**: Every symbology feature has held the "one channel vocabulary" line — a single `'stats -> Token` mapping drives every grammar unchanged (FR-001). A field on the shared `Token` is the only shape that keeps that invariant: all three grammars read it from the same value, and the board/motion functions already thread the whole `Token`, so they need no signature change. `string option` is the plainest opt-in shape (constitution III): `None` is the unambiguous default, distinct from an empty `Some ""`, and `defaultToken` setting `None` guarantees byte-identical output for every existing token (FR-002).
- **Alternatives considered**:
  - *Separate `LabelledToken` wrapper / parallel type* — rejected: forks the vocabulary, forces a second mapping, and breaks the "one `Token` drives all grammars" contract.
  - *`Label: string` with `""` = none* — rejected: conflates "no label" with "empty label" and risks a non-default `defaultToken`; `option` makes the opt-in explicit at the type level.
  - *Label as a per-grammar argument (not on `Token`)* — rejected: would require every board/motion function to grow a parameter and would let the label diverge per grammar, violating FR-001.

## R2. Per-grammar label-region siting (FR-003)

- **Decision**: Each grammar sites the label in a dedicated, **screen-aligned** region that does not overlap the sigil, health, or other channels. Provisional regions (a design-loop detail; the *contract* is FR-003 — sited, observable, non-overlapping):
  - **Token (Directional Token)**: a short caption strip **below the body belly**, beneath the bottom-mounted health arc (`healthArc`), centred on `Cx`. The label is screen-aligned even though the body/sigil rotate with `Heading`.
  - **Badge**: a caption band along the **bottom inner edge of the frame**, below the speed-pip row and health bar (`badgeSpeedPips`/`badgeHealthBar`), within the frame polygon.
  - **Ring**: a centred caption **beneath the sigil**, inside the ring's inner disc, above the bottom-rim speed beads — clear of the outer ring stroke and health-arc segments.
- **Rationale**: The existing grammars already mount their channels in fixed regions (sigil centre, health bottom/arc, speed beads/pips, shield corner, heading pip/needle); the label takes the one remaining uncrowded zone in each (below-belly / frame-bottom / inner-disc-below-sigil). Screen-alignment matches how Badge/Ring already treat heading as a discrete indicator rather than rotating text, and keeps a label readable regardless of `Heading` (the Token grammar draws the caption screen-aligned even though its body rotates).
- **Alternatives considered**:
  - *Label above the symbol (external)* — rejected: would overflow grid cells on a `gallery` board and collide with the neighbour above; the spec requires the label to stay **within the symbol footprint** (FR-005).
  - *Rotating the Token-grammar label with the body* — rejected: rotated text is hard to read and risks colliding with the heading-rotated sigil; screen-aligned captions are the legible choice and match Badge/Ring.
  - *A single shared region across grammars* — rejected: each grammar has different free space; per-grammar siting is what FR-003 asks for.

## R3. Fit-to-region strategy (FR-005) — shrink vs ellipsis-truncate

- **Decision**: An internal pure `fitLabel` helper (1) **trims** the string and returns "no label" for empty/whitespace; (2) measures the trimmed text at a base font size with `Scene.measureTextResolved text font`; (3) if it overflows the region width, **shrinks the font** toward a floor size, and if still over at the floor, **truncates with an ellipsis** at a measured glyph boundary (re-measuring the candidate so the ellipsis itself fits). The result is always within the region width and never cut mid-glyph.
- **Rationale**: Shrink-first preserves the whole string when a modest size reduction suffices (best legibility for short callsigns); ellipsis-truncate is the graceful floor for genuinely overlong strings, and measuring the truncated candidate (including the ellipsis) is what guarantees "never clips mid-glyph" and "never overflows" (FR-005). Driving both off `measureTextResolved` ties the fit to the *resolved* measurement, so under a fixed provider the geometry is deterministic (FR-008); with no measurer installed it falls back byte-identically to the pure `measureText` heuristic (which is deliberately conservative — a box it sizes is never narrower than the renderer draws), so the pure path never throws and never under-sizes.
- **Alternatives considered**:
  - *Truncate-only (no shrink)* — rejected: discards legible characters unnecessarily for labels that would fit one size down.
  - *Shrink-only (no floor/truncate)* — rejected: an arbitrarily long string would shrink to illegibility; a font floor + ellipsis keeps a sane minimum.
  - *Clip via a scene clip rect* — rejected: a hard clip cuts mid-glyph (exactly what FR-005 forbids) and hides overflow rather than fitting it.

## R4. Scene node for the label — tofu-free verification path (FR-004/FR-009)

- **Decision**: Emit the label with `Scene.glyphRunProof position text font paint` (which builds `GlyphRunData` carrying per-glyph `Missing` flags + `FallbackMode`). The pure library **never installs and never requires** a measurer (`measureTextResolved` falls back to the pure heuristic; `glyphRunProof` builds deterministic proof data either way). Tofu-free *rendering* is verified at the **render edge**: a `Symbology.Render.Tests` test rasterises a labelled token through `Render.toPng` (which, via `SkiaViewer.Fonts.installMeasurementSeam`, installs the real measurer/shaper) and asserts the label's glyph run resolves **non-tofu** (`Missing = false` for covered glyphs) — the same fail-loud text contract Controls text already rides (`Feature136TextRenderingTests`: uncovered chars are *disclosed* as tofu, never drawn as a plausible-wrong glyph).
- **Rationale**: Splitting "deterministic text node" (pure library) from "tofu-free glyphs" (render edge) matches the existing seam exactly (`Scene.setRealTextMeasurer` / `measureTextResolved`; the edge owns the real measurer). `glyphRunProof` is the constructor that carries the `Missing` evidence the tofu test needs, so the claim is verified by **real** rasterisation, not asserted. FR-009 is satisfied because the pure path emits the node and never throws without a measurer.
- **Alternatives considered**:
  - *`Scene.text` / `Scene.textAt` (plain text node)* — rejected: carries no per-glyph `Missing` evidence, so the tofu-free claim could not be verified at the edge; `glyphRunProof` is the proof-bearing constructor.
  - *Requiring the measurer in the library and throwing without it* — rejected: violates FR-009 and the pure-library seam; the library must stay measurer-optional.
  - *Asserting tofu-free purely in unit tests without rasterising* — rejected: tofu is a rendering property; only a render-bridge raster under the installed measurer proves it (constitution V — prefer real evidence).

## R5. Zero-drift strategy for label-free tokens (FR-002/FR-012/SC-003)

- **Decision**: Guarantee byte-identical output for `Label = None` (and empty/whitespace `Some`) by emitting **no label scene node at all** on those paths — the grammar's element list is unchanged, so its `SceneCodec` canonical bytes are unchanged. The existing pinned goldens in `DeterminismTests` (`token defaultToken`, `gallery`, `filmstrip` canonical-byte SHAs) are the regression guard and must stay green; they are asserted unchanged in this feature.
- **Rationale**: Adding a record field changes the `Token` *value* but not the *emitted Scene* when the field is `None`, because the canonical bytes derive from the scene element list, not the source record. Routing every "no label" case (incl. whitespace) through the exact pre-feature code path makes zero-drift structural, not coincidental. This mirrors how spec 195 kept the Token grammar byte-unchanged while adding Badge/Ring.
- **Alternatives considered**:
  - *Always emitting an empty/zero-width label node* — rejected: changes the element list and would drift the goldens, breaking FR-002.
  - *Relying on canonical-byte canonicalisation to drop empty nodes* — rejected: fragile and implicit; explicit no-node is the honest, testable guarantee.

## R6. Linter invariance — label as inspection-detail (FR-011/SC-006)

- **Decision**: Do **not** add the label to the legibility capacity table (`Legibility.fs` 11-channel pre-attentive set). `Legibility.score`/`scoreAnimated` keep their signatures and bodies unchanged; the label is treated as an **inspection-detail** channel that does not enter pop-out governance. A test renders/scores a roster with and without labels (and across all three grammars) and asserts the `Report` is **identical**.
- **Rationale**: The linter governs *pre-attentive* channels (the ones that must pop out at a glance); the label is explicitly an inspection-detail identity channel (spec Assumptions), read after attention lands, not a pop-out one. Leaving the capacity table untouched keeps the verdict grammar-independent and unchanged by labels (FR-011), and keeps the linter's `.fsi`/baseline at **zero drift**.
- **Alternatives considered**:
  - *Add `Label` as a 12th capacity channel* — rejected: would change the linter verdict and baseline, contradicting FR-011/SC-006 and over-governing an inspection-only channel.
  - *Have the linter warn on label overuse* — out of scope for this thread; if ever wanted it is a separate linter feature, not part of the label channel contract.

## R7. Determinism is provider-relative (FR-008/SC-004)

- **Decision**: The contract is **reproducibility under a fixed measurement provider**: identical labelled `Token` + same provider ⇒ identical scene ⇒ identical canonical bytes, in-process and across processes. Where label geometry is fit using measured width, the output is a deterministic function of the *resolved* measurement; the feature does not promise identical bytes across *different* providers. Verified by render-twice canonical-byte equality (in-process) and a pinned golden SHA proxy (cross-process, since purity-under-a-fixed-provider guarantees byte-identity in any process running that provider).
- **Rationale**: This mirrors exactly how the repo already treats the measurement seam (`measureTextResolved` dispatches to the installed provider; the pure heuristic is the no-provider default). The library performs no wall-clock/RNG/IO, so the only input that can vary the fit is the measurement provider — making "fixed provider ⇒ fixed bytes" the precise, honest determinism claim.
- **Alternatives considered**:
  - *Promise identical bytes across all providers* — rejected: impossible, since different real measurers report different advances and the fit depends on them; over-promising would make the contract untestable.
  - *Forbid measured fit (fixed-width truncation only)* — rejected: defeats real-measurement fit (FR-005) and the whole point of riding the real measurer for legible, non-clipping labels.

## R8. Test-battery shape (constitution V)

- **Decision**: Extend the existing Expecto batteries rather than add a parallel suite:
  - `ChannelPresenceTests` — two tokens differing **only in `Label`** produce differing canonical bytes (label observably alters output); applied across all three grammars.
  - `DeterminismTests` — labelled token render-twice byte-equal; the `token`/`gallery`/`filmstrip` `Label=None` goldens are **byte-unchanged** (regression guard, FR-002).
  - `PlaceholderTests` — a degenerate (`R <= 0`) token **with** a label renders the placeholder and never throws (FR-007).
  - `GalleryTests` — a labelled roster is reproducible per grammar via `galleryIn` (FR-010).
  - `LegibilityTests` — label presence does **not** change a roster's `Report` (FR-011/SC-006).
  - New `LabelTests.fs` — empty/whitespace label ⇒ no label node and no throw (FR-006); an overlong label measures **within** its region width (`measureTextResolved` of the fitted result ≤ region width, no mid-glyph cut); per-grammar siting is observable.
  - New render-bridge test in `Symbology.Render.Tests` — a labelled token rasterised through `Render.toPng` under the installed measurer has a **non-tofu** label glyph run (FR-004).
- **Rationale**: The label is a new channel on an existing value, so the natural test surface is the existing batteries that already vary one channel at a time and assert determinism/placeholder/board reproducibility. The two genuinely new concerns (fit-within-region and tofu-free rasterisation) get focused new tests. All scene-construction tests are real (pure logic); the tofu test is real rasterisation (no synthetic substitute).
- **Alternatives considered**:
  - *One monolithic `LabelTests.fs` covering everything* — rejected: duplicates the determinism/placeholder/presence harnesses the existing batteries already own; extending them keeps coverage consistent and discoverable.

## Summary of resolved unknowns

| Unknown (from Technical Context) | Resolution |
|---|---|
| Field name/shape for the label | `Label: string option` on `Token`; `None` default (R1) |
| Per-grammar label regions | Below-belly (Token) / frame-bottom band (Badge) / inner-disc-below-sigil (Ring), screen-aligned (R2) |
| Overlong-label fit policy | Trim → measure → shrink-to-floor → ellipsis-truncate at a measured boundary (R3) |
| Which scene node carries the label | `Scene.glyphRunProof` (carries `Missing`/`FallbackMode` tofu evidence); verified non-tofu at the render edge (R4) |
| How zero-drift is guaranteed for label-free tokens | Emit **no** node on `None`/empty/whitespace; pinned goldens stay green (R5) |
| Linter treatment of the label | Inspection-detail; **not** in the capacity table; verdict unchanged (R6) |
| Determinism contract | Provider-relative: fixed provider ⇒ byte-identical (R7) |
| Test battery | Extend presence/determinism/placeholder/gallery/legibility + new LabelTests + render-bridge tofu test (R8) |
