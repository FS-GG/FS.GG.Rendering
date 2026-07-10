// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.1.0 (src/FS.GG.Audio.Host/Host.fsi); regenerate when $(FsGgAudioVersion) moves.
namespace FS.GG.Audio.Host

open System
open FS.GG.Audio.Core

/// Public contract type. Caller-supplied resolution of product-owned ids to PCM (WAV) bytes.
/// The host does NOT own the id -> asset mapping (FR-005); a product supplies these functions.
/// `None` => unresolved: the host treats it as a recorded no-op, never a throw.
type AssetResolver =
    { ResolveSound: SoundId -> byte[] option
      ResolveTrack: TrackId -> byte[] option }

/// Public contract type. The narrow device seam (FR-001). Implementations: the Null/record
/// backend (default, deterministic) and the OpenAL backend (Silk.NET). Game-facing code holds an
/// IAudioBackend and never names a concrete backend type.
type IAudioBackend =
    inherit IDisposable
    /// Realize one requested effect. Volumes arrive already clamped by Core. Never throws; a
    /// backend that cannot act degrades to a no-op.
    abstract member Play: effect: AudioEffect -> unit

/// Public contract type (004-audio-engine). Optional mixing/spatial control a backend MAY
/// implement alongside `IAudioBackend`. `FS.GG.Audio.Engine` feature-detects it
/// (`:? IMixingBackend`); a backend that does not implement it degrades to plain `Play`, with
/// bus/fade/duck folded into one-shot gains and 3D collapsed to non-positional voices. Additive:
/// existing backends that implement only `IAudioBackend` stay valid.
type IMixingBackend =
    inherit IAudioBackend
    /// Set a bus's realized gain (already clamped to `[0,1]`), called as fades/ducks advance.
    abstract member SetBusGain: bus: Bus * gain: float -> unit
    /// Set the listener position in metres.
    abstract member SetListener: x: float * y: float * z: float -> unit
    /// Play a positional one-shot with a pre-resolved effective gain and pan in `[-1, 1]`.
    abstract member PlayAt: sound: SoundId * gain: float * pan: float -> unit

/// Public contract module. A pure, total minimal PCM WAV reader (no device, no OpenAL types).
[<RequireQualifiedAccess>]
module Wav =

    /// Decoded PCM payload of a WAV file.
    type PcmData =
        { Channels: int
          BitsPerSample: int
          SampleRate: int
          Data: byte[] }

    /// Parse a minimal PCM WAV (RIFF/WAVE, fmt + data chunks). Total; returns None on anything
    /// it does not understand rather than throwing.
    val tryParse: bytes: byte[] -> PcmData option

/// Public contract module. The imperative drive (FR-006).
[<RequireQualifiedAccess>]
module Audio =

    /// Fold a per-frame batch of requests through the backend in dispatch order. The product's
    /// `update` is unchanged: it emits AudioEffect values; this plays them.
    val play: backend: IAudioBackend -> effects: AudioEffect list -> unit

/// Public contract module. The deterministic, headless record-only backend — the default and
/// the test/CI backend (FR-002).
[<RequireQualifiedAccess>]
module NullBackend =

    /// A record-only backend: opens no device, never throws.
    [<Sealed>]
    type T =
        interface IAudioBackend
        /// Accumulated evidence — equal to `FS.GG.Audio.Core.Audio.interpret` of the same batch.
        member Evidence: AudioEvidence

    /// Create a fresh Null backend.
    val create: unit -> T

/// Public contract module. The real OpenAL device backend (Silk.NET.OpenAL) (FR-003, FR-004).
[<RequireQualifiedAccess>]
module OpenAlBackend =

    /// Attempt to open an OpenAL device and return a backend that plays through it. If the device
    /// or the OpenAL Soft native library is unavailable, log the reason and return a Null backend
    /// instead (degrade-to-zero, FR-004) — the returned IAudioBackend is always usable, never null,
    /// and never throws into game code.
    val create: resolver: AssetResolver -> IAudioBackend
