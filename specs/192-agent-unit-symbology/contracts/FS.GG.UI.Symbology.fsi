// CONTRACT SKETCH — Phase 1 (/speckit-plan). Authored .fsi-first per Constitution I/II.
// This is the intended PUBLIC surface of the pure, Scene-only library. The real
// src/Symbology/Symbology.fsi is validated by FSI use and pinned by
// readiness/surface-baselines/FS.GG.UI.Symbology.txt before src/Symbology/Symbology.fs exists.
//
// Dependency rule (FR-001/FR-011): references ONLY FS.GG.UI.Scene. No IO, no GL, no codec call here.
// Determinism rule (FR-003/FR-009): every function below is pure; no wall-clock, no IO; caller owns phase.

namespace FS.GG.UI.Symbology

open FS.GG.UI.Scene   // Scene, Color, Point, Rect, Size, PathSpec (Types/Scene)

/// Affiliation → stroke hue (saturated faction palette; never the state palette — FR-019).
type Faction =
    | Ally
    | Enemy
    | Neutral
    | Custom of Color

/// Class → body silhouette (fixed table — FR-005). v1 Token grammar ships these three (R3/G3).
type Klass =
    | Mobile
    | Heavy
    | Scout

/// Identity mark → centre vector sigil (no label text this iteration — FR-022).
type Sigil =
    | Bolt
    | Ring
    | Fang
    | Mark of PathSpec

/// Confirmed vs suspected → solid vs dashed stroke (inspection channel).
type TokenState =
    | Confirmed
    | Suspected

/// Activity / alert rhythm → deterministic motion overlay (FR-007). One active at a time (budget).
type Motion =
    | Idle
    | Pulse
    | Spin
    | Blink
    | Damage
    | Moving

/// The symbol description: the full fixed channel set as typed fields (FR-002).
/// Pure over this value (FR-003): equal Token ⇒ equal Scene ⇒ equal SceneCodec canonical bytes.
type Token =
    { Cx: float
      Cy: float
      R: float            // size; R <= 0 ⇒ visible placeholder, never a blank/crash (FR-020)
      Heading: float      // radians; whole-body rotation (gauges stay screen-aligned)
      Faction: Faction    // stroke hue
      Klass: Klass        // silhouette
      Sigil: Sigil        // centre identity mark
      State: TokenState   // stroke dash (inspection state)
      Threat: float       // stroke width   (normalised 0..1)
      Charge: float       // interior radial-gradient intensity — charge/energy (normalised 0..1)
      Speed: int          // tail bead count (0..4)
      Health: float       // belly arc len + hue (0..1)
      Shield: bool }      // corner mount (boolean mount flag; one slot in v1)

/// The fixed grammar (FR-004/FR-006). The per-game `'stats -> Token` mapping lives OUTSIDE this library.
[<RequireQualifiedAccess>]
module Symbology =

    /// Fully-populated baseline so a ChannelMap overrides only the fields a game encodes.
    val defaultToken: Token

    /// The Directional-Token element: renders every channel so each observably alters output (SC-002).
    /// Pure & deterministic (FR-003). Zero/empty area degrades to a visible placeholder (FR-020).
    val token: token: Token -> Scene

    /// Deterministic motion overlay; phase is caller-owned, no wall-clock (FR-007/FR-009).
    /// Identical (motion, token, phase) ⇒ identical Scene.
    val animate: motion: Motion -> token: Token -> phase: float -> Scene

    /// Reproducible grid of symbols for at-a-glance review (FR-008).
    val gallery: cols: int -> spacing: float -> tokens: Token list -> Scene

    /// Motion sampled across `samples` phase steps from a deterministic schedule (FR-008/FR-009/SC-006).
    /// Frames are byte-reproducible from the schedule alone.
    val filmstrip: samples: int -> entries: (Motion * Token) list -> Scene
