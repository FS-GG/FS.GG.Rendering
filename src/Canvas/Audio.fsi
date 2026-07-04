namespace FS.GG.UI.Canvas

/// Public contract type exposed by this FS.GG.UI package.
/// Opaque, product-owned identifier naming a sound effect. The framework does not own the
/// id -> asset mapping (kept out of the library, like per-game stat mapping in symbology); a
/// product resolves it to a real asset in its own host layer.
type SoundId = SoundId of string

/// Public contract type exposed by this FS.GG.UI package.
/// Opaque, product-owned identifier naming a music track.
type TrackId = TrackId of string

/// Public contract type exposed by this FS.GG.UI package.
/// A requested sound action, expressed as a pure value from a product's `update`. Data only —
/// no case carries a device handle, stream, or effectful closure. Volume/level are normalized
/// gains in `[0.0, 1.0]`; out-of-range values are clamped at the boundary, never thrown on.
type AudioEffect =
    | PlaySfx of sound: SoundId * volume: float
    | PlayMusic of track: TrackId * loop: bool
    | StopMusic
    | SetMasterVolume of level: float

/// Public contract type exposed by this FS.GG.UI package.
/// Ordered evidence of what a product requested, produced by the record-only interpreter. This
/// is the primary, hardware-free evidence for the headless path: the recorded requests ARE the
/// evidence (no real sound output is involved).
type AudioEvidence =
    { /// Requested effects in dispatch order, oldest first, with volumes normalized.
      Requested: AudioEffect list }

/// Public contract module exposed by this FS.GG.UI package.
/// The audio request vocabulary plus a pure record-only interpreter. A product's `update` emits
/// `AudioEffect` values (it never plays sound); the interpreter folds a batch into `AudioEvidence`.
/// A real audio-output backend is deferred and will consume the same values without changing this
/// surface.
[<RequireQualifiedAccess>]
module Audio =

    /// Public contract value exposed by this FS.GG.UI package.
    /// Lower bound of the normalized volume range.
    val minVolume: float

    /// Public contract value exposed by this FS.GG.UI package.
    /// Upper bound of the normalized volume range.
    val maxVolume: float

    /// Public contract function exposed by this FS.GG.UI package.
    /// Clamp a requested volume into `[minVolume, maxVolume]`. Total; never throws. `nan` clamps to
    /// `minVolume`.
    val clampVolume: level: float -> float

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a sound-effect request; clamps the carried volume at the boundary.
    val playSfx: sound: SoundId -> volume: float -> AudioEffect

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a music request.
    val playMusic: track: TrackId -> loop: bool -> AudioEffect

    /// Public contract value exposed by this FS.GG.UI package.
    /// Request that the current music track stop. A no-op at the interpreter if nothing is playing.
    val stopMusic: AudioEffect

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a master-volume request; clamps the carried level at the boundary.
    val setMasterVolume: level: float -> AudioEffect

    /// Public contract value exposed by this FS.GG.UI package.
    /// Evidence with no requests recorded yet.
    val emptyEvidence: AudioEvidence

    /// Public contract function exposed by this FS.GG.UI package.
    /// Record-only interpreter over a single requested effect: append it to the evidence (pure,
    /// total). Carried volumes are normalized so recorded evidence is always in range.
    val record: effect: AudioEffect -> evidence: AudioEvidence -> AudioEvidence

    /// Public contract function exposed by this FS.GG.UI package.
    /// Record-only interpreter over a batch, preserving dispatch order. This is the headless-safe
    /// host boundary: no device access, never blocks, never throws. Returns the accumulated evidence.
    val interpret: effects: AudioEffect list -> AudioEvidence
