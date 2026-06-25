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

/// The symbol description: the full fixed channel set as typed fields (FR-002).
/// Pure over this value (FR-003): equal Token => equal Scene => equal SceneCodec canonical bytes.
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
      Shield: bool }

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
