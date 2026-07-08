namespace FS.GG.Audio.Core

/// Public contract type exposed by this FS.GG.Audio.Core package.
/// Opaque, product-owned identifier naming a sound effect. The framework does not own the
/// id -> asset mapping (kept out of the library, like per-game stat mapping in symbology); a
/// product resolves it to a real asset in its own host layer.
type SoundId = SoundId of string

/// Public contract type exposed by this FS.GG.Audio.Core package.
/// Opaque, product-owned identifier naming a music track.
type TrackId = TrackId of string

/// Public contract type exposed by this FS.GG.Audio.Core package.
/// A named mixer bus (004-audio-engine). `Master` scales every other bus; `SetMasterVolume`
/// targets `Master`. The engine (FS.GG.Audio.Engine) owns the mixing semantics; the vocabulary
/// only names the buses a product can address.
type Bus =
    | Master
    | Music
    | Sfx
    | Ui
    | Ambient

/// Public contract type exposed by this FS.GG.Audio.Core package.
/// A requested sound action, expressed as a pure value from a product's `update`. Data only —
/// no case carries a device handle, stream, or effectful closure. Volume/level are normalized
/// gains in `[0.0, 1.0]`; out-of-range values are clamped at the boundary, never thrown on.
/// The last three cases are additive (004-audio-engine); the original four are unchanged.
type AudioEffect =
    | PlaySfx of sound: SoundId * volume: float
    | PlayMusic of track: TrackId * loop: bool
    | StopMusic
    | SetMasterVolume of level: float
    | PlaySfx3D of sound: SoundId * x: float * y: float * z: float * volume: float
    | SetBusVolume of bus: Bus * level: float
    | Duck of bus: Bus * amount: float * milliseconds: float

/// Public contract type exposed by this FS.GG.Audio.Core package.
/// Ordered evidence of what a product requested, produced by the record-only interpreter. This
/// is the primary, hardware-free evidence for the headless path: the recorded requests ARE the
/// evidence (no real sound output is involved).
type AudioEvidence =
    { /// Requested effects in dispatch order, oldest first, with volumes normalized.
      Requested: AudioEffect list }

/// Public contract module exposed by this FS.GG.Audio.Core package.
/// The audio request vocabulary plus a pure record-only interpreter. A product's `update` emits
/// `AudioEffect` values (it never plays sound); the interpreter folds a batch into `AudioEvidence`.
/// A real audio-output backend is deferred (FS.GG.Audio.Host) and will consume the same values
/// without changing this surface.
[<RequireQualifiedAccess>]
module Audio =

    /// Public contract value exposed by this FS.GG.Audio.Core package.
    /// Lower bound of the normalized volume range.
    val minVolume: float

    /// Public contract value exposed by this FS.GG.Audio.Core package.
    /// Upper bound of the normalized volume range.
    val maxVolume: float

    /// Public contract function exposed by this FS.GG.Audio.Core package.
    /// Clamp a requested volume into `[minVolume, maxVolume]`. Total; never throws. `nan` clamps to
    /// `minVolume`.
    val clampVolume: level: float -> float

    /// Public contract function exposed by this FS.GG.Audio.Core package.
    /// Smart constructor for a sound-effect request; clamps the carried volume at the boundary.
    val playSfx: sound: SoundId -> volume: float -> AudioEffect

    /// Public contract function exposed by this FS.GG.Audio.Core package.
    /// Smart constructor for a music request.
    val playMusic: track: TrackId -> loop: bool -> AudioEffect

    /// Public contract value exposed by this FS.GG.Audio.Core package.
    /// Request that the current music track stop. A no-op at the interpreter if nothing is playing.
    val stopMusic: AudioEffect

    /// Public contract function exposed by this FS.GG.Audio.Core package.
    /// Smart constructor for a master-volume request; clamps the carried level at the boundary.
    val setMasterVolume: level: float -> AudioEffect

    /// Public contract function exposed by this FS.GG.Audio.Core package (004-audio-engine).
    /// Smart constructor for a positional sound-effect request; clamps the carried volume. `z = 0`
    /// is the 2D stereo-pan-by-x convenience.
    val playSfx3D: sound: SoundId -> x: float -> y: float -> z: float -> volume: float -> AudioEffect

    /// Public contract function exposed by this FS.GG.Audio.Core package (004-audio-engine).
    /// Smart constructor for a per-bus volume request; clamps the carried level at the boundary.
    val setBusVolume: bus: Bus -> level: float -> AudioEffect

    /// Public contract function exposed by this FS.GG.Audio.Core package (004-audio-engine).
    /// Smart constructor for a timed duck request; clamps `amount` to `[0, 1]` and `milliseconds`
    /// to a non-negative floor.
    val duck: bus: Bus -> amount: float -> milliseconds: float -> AudioEffect

    /// Public contract value exposed by this FS.GG.Audio.Core package.
    /// Evidence with no requests recorded yet.
    val emptyEvidence: AudioEvidence

    /// Public contract function exposed by this FS.GG.Audio.Core package.
    /// Record-only interpreter over a single requested effect: append it to the evidence (pure,
    /// total). Carried volumes are normalized so recorded evidence is always in range.
    val record: effect: AudioEffect -> evidence: AudioEvidence -> AudioEvidence

    /// Public contract function exposed by this FS.GG.Audio.Core package.
    /// Record-only interpreter over a batch, preserving dispatch order. This is the headless-safe
    /// host boundary: no device access, never blocks, never throws. Returns the accumulated evidence.
    val interpret: effects: AudioEffect list -> AudioEvidence
