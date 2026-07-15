// See skill: fs-gg-symbology
namespace FS.GG.UI.Symbology

open FS.GG.UI.Scene

/// One styled span of identity-label text (feature 198 — rich-text runs). Inspection-detail: each run
/// may carry its own colour / weight / size; an omitted (`None`) attribute inherits the default label
/// style for that attribute, so an all-default run reproduces the spec-196/197 uniform style exactly
/// (FR-002/FR-003). Rendered tofu-free at the render edge (FR-005); the pure library never requires a
/// measurer (FR-010). `Text` may embed `\n`/`\r\n` hard breaks; empty/whitespace runs drop (FR-007).
type LabelRun =
    { Text: string
      /// `None` ⇒ default label ink (the spec-196 ink). Author-supplied from the scene `Color`
      /// vocabulary; used as-is, never re-mapped or rejected at runtime (FR-013).
      Color: Color option
      /// `None` ⇒ default weight. Maps directly onto `FontSpec.Weight : int option`.
      Weight: int option
      /// `None` ⇒ `1.0`. Multiplies the grammar's base label size (keeps grammar-independence — FR-001).
      Scale: float option
      /// Feature 199 (FR-003) — synthetic italic/slant. `None`/`Some false` ⇒ upright. An all-default
      /// run (every 199 attribute unset/false/0) renders byte-identically to the spec-198 run (FR-004).
      Italic: bool option
      /// Feature 199 (FR-003/FR-008) — underline rule below the baseline. `None`/`Some false` ⇒ none.
      Underline: bool option
      /// Feature 199 (FR-003/FR-008) — strike-through rule at mid-x-height. `None`/`Some false` ⇒ none.
      Strike: bool option
      /// Feature 199 (FR-003/FR-007) — letter-spacing (tracking), an em-fraction of the resolved size,
      /// folded into measurement so it never pushes the block past the region. `None`/`Some 0.0` ⇒ none.
      Tracking: float option }

/// Per-paragraph horizontal alignment of a laid-out (`Laid`) label within its per-grammar region
/// (feature 199, FR-001). `Center` is the DEFAULT and reproduces the spec-198 flow byte-for-byte;
/// `Justify` distributes measured inter-word space to fill the region width, leaving the last line of
/// each paragraph (and any single-token line) un-justified (FR-007/FR-008).
type LabelAlign =
    | Leading
    | Center
    | Trailing
    | Justify

/// One explicit paragraph of a laid-out (`Laid`) label (feature 199, FR-002): an ordered run list plus
/// its alignment. Paragraph breaks are the list boundaries; hard line breaks inside a paragraph use the
/// runs' embedded `\n`/`\r\n`. An empty / all-whitespace / all-empty-run paragraph contributes no line
/// (FR-009). Each paragraph may carry its own alignment.
type LabelParagraph =
    { Runs: LabelRun list
      Align: LabelAlign }

/// The optional identity label's content (feature 198). `Plain` is the spec-197 channel verbatim
/// (single- or multi-line via embedded `\n`); `Rich` carries an ordered sequence of styled runs. A
/// `Plain` label, and a `Rich` label whose runs are all default-styled, render BYTE-IDENTICALLY to the
/// equivalent spec-197 label (layered zero-drift — FR-002/SC-003). `[<RequireQualifiedAccess>]` matches
/// the `Grammar` convention: written `LabelText.Plain` / `LabelText.Rich`.
[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string
    | Rich of LabelRun list
    /// Feature 199 (FR-001/FR-002) — explicit, individually-alignable paragraphs. A single `Center`
    /// paragraph of all-default runs renders BYTE-IDENTICALLY to the equivalent `Rich`/`Plain` label
    /// (default alignment = the spec-198 flow — layered zero-drift, FR-004/SC-003).
    | Laid of LabelParagraph list

/// An opt-in binding of the RESOLVED label to the existing symbology motion timeline (feature 200,
/// FR-005). Sampled as a deterministic function of the motion phase the board already supplies
/// (`animateIn`/`filmstripIn`); byte-identical to the static spec-199 label at the rest phase (FR-007);
/// fitted at every phase (FR-011). `[<RequireQualifiedAccess>]` (like `Grammar`/`LabelText`) so
/// `LabelMotion.Pulse` never collides with `Motion.Pulse`.
[<RequireQualifiedAccess>]
type LabelMotion =
    | TypeOn   // whole-glyph prefix reveal; rest = fully revealed
    | Fade     // run paint alpha ramp; rest = full alpha
    | Pulse    // size/alpha oscillation; rest = unscaled
    | Scroll   // overflow ticker within the region; rest = offset 0
