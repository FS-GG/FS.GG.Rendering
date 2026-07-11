// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.2.0 (src/FS.GG.Audio.Host/Host.fsi); regenerate when $(FsGgAudioVersion) moves.
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

/// Public contract module. The pure pan -> source-position mapping the OpenAL backend spatializes
/// through (#11). No device, no OpenAL types.
[<RequireQualifiedAccess>]
module Spatial =

    /// Map a stereo pan in `[-1, 1]` (as `IMixingBackend.PlayAt` carries it) to a source position in
    /// the listener's own frame: `-1` hard left, `0` dead ahead, `+1` hard right. Total — pan is
    /// clamped and `nan` centres. The result is always unit-length, which is what keeps a device's
    /// distance model from attenuating a gain `FS.GG.Audio.Engine` has already attenuated.
    val panToPosition: pan: float -> float * float * float

/// Public contract module. A device-free memo of uploaded buffer handles keyed by a product id
/// (`SoundId`/`TrackId`), so an asset is decoded and uploaded once rather than on every play (#20).
/// It holds only `uint` handles and a create-callback — no device, no OpenAL types — so it is
/// exercised headless.
[<RequireQualifiedAccess>]
module BufferCache =

    /// A memo of buffer handles keyed by `'k`.
    [<Sealed>]
    type T<'k when 'k: equality> =
        /// A fresh, empty cache.
        new: unit -> T<'k>
        /// The cached handle for `key`, created once via `create` on first miss. A `None` from
        /// `create` (unresolved / unparseable asset) is NOT cached, so a later successful resolve of
        /// the same id can still populate the entry.
        member GetOrAdd: key: 'k * create: (unit -> uint option) -> uint option
        /// Number of distinct handles held (one per successfully uploaded id).
        member Count: int
        /// Every cached handle, for deletion when the backend is disposed.
        member Handles: uint[]

/// Public contract module. A device-free, bounded pool of one-shot voice handles that reclaims
/// finished voices instead of leaking them (#20): the OpenAL backend used to allocate a source per
/// one-shot and never delete it, so a long session exhausted the source ceiling and `Play` then
/// failed silently. The pool takes its device operations as callbacks, so its reclaim/steal logic
/// runs headless behind counting fakes.
[<RequireQualifiedAccess>]
module VoicePool =

    /// The device operations a pool drives, named so the two `uint -> unit` handle operations cannot
    /// be transposed. In the OpenAL backend: `GenSource`, a `SourceState = Stopped` test,
    /// `SourceStop`, and `DeleteSource`.
    type Ops =
        { /// Allocate a fresh source handle.
          Gen: unit -> uint
          /// True once a handed-out voice has finished (is reclaimable).
          IsStopped: uint -> bool
          /// Stop a still-sounding voice so its handle can be reused or deleted.
          Stop: uint -> unit
          /// Release a handle for good.
          Delete: uint -> unit }

    /// A bounded pool of one-shot voice handles.
    [<Sealed>]
    type T =
        /// A pool driven by `ops`, holding at most `ceiling` live handles before it steals the
        /// oldest still-sounding voice.
        new: ops: Ops * ceiling: int -> T
        /// A source handle ready to be configured and played: reclaims finished voices, reuses a
        /// free handle when one exists, grows up to `ceiling`, and past it steals the oldest voice.
        member Acquire: unit -> uint
        /// Voices handed out and presumed still sounding.
        member ActiveCount: int
        /// Reclaimed handles available for reuse.
        member FreeCount: int
        /// True once the ceiling has forced at least one oldest-voice steal.
        member HasStolen: bool
        /// Stop and delete every handle the pool owns.
        member DisposeAll: unit -> unit

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
    ///
    /// The device backend also implements `IMixingBackend` (#11), so driven by `FS.GG.Audio.Engine`
    /// it spatializes: pan reaches the hardware, and bus fades/ducks reach the music voice. Test it
    /// with `:? IMixingBackend` rather than assuming — the Null fallback does not, which is exactly
    /// what makes the Engine take its non-positional degrade path on a machine with no device.
    /// Spatialization is per-source, so a positional sound must be a **mono** asset; OpenAL plays a
    /// stereo buffer centred, whatever position it is given.
    val create: resolver: AssetResolver -> IAudioBackend
