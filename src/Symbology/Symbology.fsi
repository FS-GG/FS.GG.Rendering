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
