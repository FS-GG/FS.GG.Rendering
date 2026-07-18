// See skill: fs-gg-symbology
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

/// Channel selectors a `Token`'s auto-label projection may read (feature 200, FR-002). Each reads
/// ONLY the named encoded channel — never a game's raw stats — and renders a fixed, game-agnostic,
/// compact code suited to the tight per-grammar label regions.
type AutoField =
    | FactionCode   // Ally->"ALY" | Enemy->"ENY" | Neutral->"NEU" | Custom _ ->"CUS"
    | KlassCode     // Mobile->"MOB" | Heavy->"HVY" | Scout->"SCT"
    | StateCode     // Confirmed->"CFM" | Suspected->"SUS"
    | HealthTier    // round(Health*100) -> "H"+nn
    | ThreatTier    // bucket Threat [0,1] -> "T0".."T4"
    | SpeedPips     // Speed (0..4) -> "S0".."S4"
    | ShieldFlag    // Shield=true -> "SHD"; false -> contributes nothing

/// An opt-in auto-label projection request (feature 200, FR-001). The library projects a styled label
/// from the `Token`'s OWN encoded channels — a pure, deterministic function of those channels (FR-002),
/// overridable by an explicit `Label` (FR-003), yielding NO label when it projects to no drawable
/// glyphs (FR-004). The rendered field codes are joined by `Separator`.
type AutoLabelSpec =
    { Fields: AutoField list   // ordered selectors; [] -> projects to nothing -> no label
      Separator: string }      // joins the rendered field codes (e.g. " " or "·")

/// The symbol description: the full fixed channel set as typed fields (FR-002).
/// Pure over this value (FR-003): equal Token => equal Scene => equal SceneCodec canonical bytes
/// (under a fixed text-measurement provider; FR-008).
type Token =
    { Cx: float
      Cy: float
      R: float
      /// The body orientation: where the unit faces / drives. Whole-body rotation in `Grammar.Token`,
      /// a discrete indicator in `Badge`/`Ring`.
      Heading: float
      /// Feature 254 (FR-001) — an opt-in SECOND rotation channel, independent of `Heading`: where a
      /// unit *points* when that differs from where it faces (a turret on a hull, a weapon arc, a sensor
      /// or gaze direction). `None` = off (default), and a no-`SecondaryHeading` token renders
      /// BYTE-IDENTICALLY to the pre-feature symbol in every grammar (FR-002) — it contributes no node
      /// at all, rather than an empty one. When `Some angle` it is drawn as a centre-out barrel with a
      /// tip mark, deliberately shaped so it never reads as the primary nose / edge pip / needle
      /// (FR-003). Angles wrap, so any finite value is in-domain; non-finite is a `Legibility` error.
      SecondaryHeading: float option
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
      Label: LabelText option
      /// Feature 200 (FR-001) — opt-in channel projection. `None` = off (default); a no-`AutoLabel`
      /// token is byte-identical to spec 199. When `Some spec` AND `Label = None`, the library projects a
      /// styled label from this `Token`'s own channels (FR-002); an explicit `Label` always wins (FR-003).
      AutoLabel: AutoLabelSpec option
      /// Feature 200 (FR-005) — opt-in binding of the resolved label to the existing motion timeline.
      /// `None` = off (default); a no-`LabelMotion` token is byte-identical to spec 199 across the whole
      /// timeline. At the rest phase a motion-bound label equals the static spec-199 label (FR-007).
      LabelMotion: LabelMotion option }

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

    // ---- Auto-label / label-motion constructors (feature 200, FR-001/FR-005) ----

    /// An auto-label projection request over the given channel selectors, joined by a single space
    /// — `= { Fields = fields; Separator = " " }`. Set on a `Token` via `AutoLabel = Some (Symbology.autoLabel [...])`.
    val autoLabel: fields: AutoField list -> AutoLabelSpec

    /// An auto-label projection request with an explicit separator (e.g. `"·"`).
    val autoLabelSep: separator: string -> fields: AutoField list -> AutoLabelSpec

    /// Identity helper for readable `LabelMotion = Some (Symbology.labelMotion LabelMotion.TypeOn)` call sites.
    val labelMotion: kind: LabelMotion -> LabelMotion

    /// The Directional-Token element: renders every channel so each observably alters output (SC-002).
    /// Pure & deterministic (FR-003). Zero/empty area degrades to a visible placeholder (FR-020).
    /// Returns a `Scene` (`{ Nodes: SceneNode list }`), NOT a `SceneNode`: to compose a token into a
    /// `SceneNode` tree (e.g. a per-entity layer under `SceneNode.Group`), wrap it as `Group [ token tok ]`
    /// or splice its `.Nodes` into a `SceneNode list` — `yield! (token tok).Nodes`.
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
