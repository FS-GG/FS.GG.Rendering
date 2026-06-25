namespace FS.GG.UI.Symbology

open FS.GG.UI.Scene

/// Affiliation -> stroke hue (saturated faction palette; never the state palette — FR-019).
type Faction =
    | Ally
    | Enemy
    | Neutral
    | Custom of Color

/// Class -> body silhouette (fixed table — FR-005). v1 Token grammar ships these three (R3/G3).
type Klass =
    | Mobile
    | Heavy
    | Scout

/// Identity mark -> centre vector sigil (no label text this iteration — FR-022).
type Sigil =
    | Bolt
    | Ring
    | Fang
    | Mark of PathSpec

/// Confirmed vs suspected -> solid vs dashed stroke (inspection channel).
type TokenState =
    | Confirmed
    | Suspected

/// Activity / alert rhythm -> deterministic motion overlay (FR-007). One active at a time (budget).
type Motion =
    | Idle
    | Pulse
    | Spin
    | Blink
    | Damage
    | Moving

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

/// The symbol description: the full fixed channel set as typed fields (FR-002).
/// Pure over this value (FR-003): equal Token => equal Scene => equal SceneCodec canonical bytes
/// (under a fixed text-measurement provider; FR-008).
type Token =
    { Cx: float
      Cy: float
      R: float
      Heading: float
      Faction: Faction
      Klass: Klass
      Sigil: Sigil
      State: TokenState
      Threat: float
      Charge: float
      Speed: int
      Health: float
      Shield: bool
      /// Optional identity label (name / callsign / code), now a `LabelText` (feature 198). `None` = no
      /// label (default) and renders byte-identically to the pre-feature symbol (FR-002). A
      /// `Some (LabelText.Plain s)` is the spec-197 single-/multi-line label verbatim; a
      /// `Some (LabelText.Rich runs)` carries per-run styled spans. Empty/whitespace/empty-run content is
      /// treated as no label (FR-007). When present it is drawn screen-aligned in the grammar's label
      /// region, fitted per run to that region via real text measurement (FR-006), and tofu-free when
      /// rendered through the headless render bridge's real measurer (FR-005). Inspection-detail:
      /// it does NOT enter the legibility capacity table (FR-012).
      Label: LabelText option }

/// The selectable symbol form factor (FR-001/FR-002). All three consume the SAME fixed Token channel
/// set: one `'stats -> Token` mapping drives any grammar unchanged. The choice changes the DRAWING,
/// never the per-game ChannelMap. `[<RequireQualifiedAccess>]` so `Grammar.Token` never collides with
/// the `Token` record in this namespace.
[<RequireQualifiedAccess>]
type Grammar =
    | Token
    | Badge
    | Ring

/// The fixed grammar (FR-004/FR-006). The per-game `'stats -> Token` mapping lives OUTSIDE this library.
[<RequireQualifiedAccess>]
module Symbology =

    /// Fully-populated baseline so a ChannelMap overrides only the fields a game encodes.
    val defaultToken: Token

    // ---- Rich-text label constructors (feature 198, FR-001/FR-003) ----

    /// An unstyled (plain) label — `= LabelText.Plain`. Single- or multi-line via embedded `\n`.
    val plainLabel: text: string -> LabelText

    /// A default-styled run (no per-run colour/weight/size override). Style by record-copying, e.g.
    /// `{ Symbology.run "BRAVO-6" with Weight = Some 700; Color = Some teamBlue }`.
    val run: text: string -> LabelRun

    /// A rich (styled-run) label — `= LabelText.Rich`. An all-default run list renders byte-identically
    /// to the equivalent `plainLabel` (FR-002).
    val richLabel: runs: LabelRun list -> LabelText

    // ---- Laid-out label constructors (feature 199, FR-001/FR-002) ----

    /// A `Center`-aligned paragraph of styled runs — `= { Runs = runs; Align = Center }`. Slant /
    /// decoration / tracking are set by record-copying a `run`, e.g.
    /// `{ Symbology.run "quoted" with Italic = Some true }`.
    val paragraph: runs: LabelRun list -> LabelParagraph

    /// A paragraph of styled runs with an explicit alignment (feature 199, FR-001).
    val align: alignment: LabelAlign -> runs: LabelRun list -> LabelParagraph

    /// A laid-out (paragraph) label — `= LabelText.Laid`. A single `Center` paragraph of all-default
    /// runs renders byte-identically to the equivalent `richLabel`/`plainLabel` (FR-004/SC-003).
    val laidLabel: paragraphs: LabelParagraph list -> LabelText

    /// The Directional-Token element: renders every channel so each observably alters output (SC-002).
    /// Pure & deterministic (FR-003). Zero/empty area degrades to a visible placeholder (FR-020).
    val token: token: Token -> Scene

    /// Deterministic motion overlay; phase is caller-owned, no wall-clock (FR-007/FR-009).
    /// Identical (motion, token, phase) => identical Scene.
    val animate: motion: Motion -> token: Token -> phase: float -> Scene

    /// Reproducible grid of symbols for at-a-glance review (FR-008).
    val gallery: cols: int -> spacing: float -> tokens: Token list -> Scene

    /// Motion sampled across `samples` phase steps from a deterministic schedule (FR-008/FR-009/SC-006).
    /// Frames are byte-reproducible from the schedule alone.
    val filmstrip: samples: int -> entries: (Motion * Token) list -> Scene

    // ---- NEW grammars (FR-001) ----

    /// The Badge element: a compact, screen-aligned framed emblem encoding EVERY channel (FR-003).
    /// Heading is a discrete edge indicator, not whole-body rotation (FR-006). Pure & deterministic
    /// (FR-004). A degenerate token (R <= 0) degrades to a visible placeholder (FR-005); never throws.
    val badge: token: Token -> Scene

    /// The Ring element: a centred radial gauge encoding EVERY channel (FR-003). Continuous channels
    /// read as radial/arc quantities; the health arc sweep is monotone in Health (FR-007). Heading is a
    /// discrete needle, not body rotation (FR-006). Pure & deterministic (FR-004); R <= 0 -> placeholder.
    val ring: token: Token -> Scene

    // ---- NEW grammar dispatch + grammar-parameterized boards (FR-008) ----

    /// Render a token in the SELECTED grammar. `render Grammar.Token` reproduces `token` byte-for-byte.
    val render: grammar: Grammar -> token: Token -> Scene

    /// Reproducible grid of symbols drawn in the selected grammar (FR-008). Empty/single roster OK.
    /// `galleryIn Grammar.Token` reproduces `gallery` byte-for-byte (FR-010).
    val galleryIn: grammar: Grammar -> cols: int -> spacing: float -> tokens: Token list -> Scene

    /// Motion filmstrip in the selected grammar; only grammar-agnostic overlays apply on Badge/Ring
    /// (FR-014). `filmstripIn Grammar.Token` reproduces `filmstrip` byte-for-byte.
    val filmstripIn: grammar: Grammar -> samples: int -> entries: (Motion * Token) list -> Scene

    /// Deterministic motion overlay in the selected grammar (FR-014). On Badge/Ring, applies only the
    /// grammar-agnostic centre/radius overlays (Pulse/Blink/Damage); directional motions degrade to the
    /// static base symbol. `animateIn Grammar.Token` reproduces `animate` byte-for-byte.
    val animateIn: grammar: Grammar -> motion: Motion -> token: Token -> phase: float -> Scene
