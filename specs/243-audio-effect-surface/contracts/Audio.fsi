// Contract draft (Phase 1) for feature 243 — the FSI-first sketch of the public audio
// request surface. This is the design surface exercised in FSI before any .fs exists
// (Constitution Principle I). The shipped file will be src/Canvas/Audio.fsi.
//
// Design intent:
//   * AudioEffect is a PURE value: a requested sound action, carrying data only — never a
//     device handle, callback, or IO. Product `update` returns these; it never plays sound.
//   * The record-only interpreter folds a batch of requests into ordered evidence. This is
//     the headless / no-audio-device path: the recorded requests ARE the evidence.
//   * A real audio-output backend (SkiaViewer host) is deferred; it will consume the same
//     AudioEffect values without changing this surface (FR-006).

namespace FS.GG.UI.Canvas

/// Opaque product-owned identifier for a sound effect. The framework does not own the
/// id -> asset mapping (kept out of the library, like per-game stat mapping in symbology).
type SoundId = SoundId of string

/// Opaque product-owned identifier for a music track.
type TrackId = TrackId of string

/// A requested sound action, expressed as a pure value from product `update`.
/// Volume is a normalized gain in [0.0, 1.0]; out-of-range values are clamped by the
/// interpreter, never thrown on (Constitution Principle VI: safe failure).
type AudioEffect =
    | PlaySfx of sound: SoundId * volume: float
    | PlayMusic of track: TrackId * loop: bool
    | StopMusic
    | SetMasterVolume of level: float

/// Ordered evidence of what a product requested, produced by the record-only interpreter.
/// This is the primary, hardware-free evidence for the headless path (US2).
type AudioEvidence =
    { /// Requested effects in dispatch order (oldest first).
      Requested: AudioEffect list }

[<RequireQualifiedAccess>]
module Audio =

    /// The normalized volume range accepted by the surface.
    val minVolume: float
    val maxVolume: float

    /// Clamp a requested volume into [minVolume, maxVolume]. Total; never throws.
    val clampVolume: level: float -> float

    /// Smart constructors (validate/clamp at the boundary, return plain values).
    val playSfx: sound: SoundId -> volume: float -> AudioEffect
    val playMusic: track: TrackId -> loop: bool -> AudioEffect
    val stopMusic: AudioEffect
    val setMasterVolume: level: float -> AudioEffect

    /// Empty evidence (no requests yet).
    val emptyEvidence: AudioEvidence

    /// Record-only interpreter: append one requested effect to evidence (pure, total).
    /// Applies boundary clamping to carried volumes so recorded evidence is normalized.
    val record: effect: AudioEffect -> evidence: AudioEvidence -> AudioEvidence

    /// Record-only interpreter over a batch, preserving dispatch order. This is the
    /// headless-safe "host boundary" for the minimal slice: no device access, never blocks,
    /// never throws (FR-005). Returns the accumulated evidence.
    val interpret: effects: AudioEffect list -> AudioEvidence
