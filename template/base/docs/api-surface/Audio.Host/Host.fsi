// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.5.0 (src/FS.GG.Audio.Host/Host.fsi); regenerate when $(FsGgAudioVersion) moves.
namespace FS.GG.Audio.Host

open System
open FS.GG.Audio.Core

/// Public contract type. Caller-supplied resolution of product-owned ids to PCM (WAV) bytes.
/// The host does NOT own the id -> asset mapping (FR-005); a product supplies these functions.
///
/// `None` => unresolved: the host treats it as a no-op, never a throw — the id plays as SILENCE.
/// That is a real failure mode and the one a game developer actually hits (a typo'd id, an asset
/// that was never shipped), so the device backend does not swallow it: it names the id and the
/// reason once, on stderr (#28). See `AssetDiagnostics`. The Null backend records the request
/// regardless — it resolves nothing, so its evidence says "requested", never "audible".
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
    ///
    /// NOT THREAD-SAFE, and implementations are not required to be: drive a backend from ONE thread.
    /// That is a deliberate contract rather than an oversight — a game mixes on one thread, and
    /// locking a per-effect call to serve a case nobody has would cost every caller for nothing. It
    /// is written down because the surface does not imply it: nothing about `Play` looks
    /// thread-affine, and `FS.GG.Audio.Elmish`'s `Audio.Cmd` hands the drive to the Elmish runtime,
    /// which runs effects on whatever thread it likes.
    ///
    /// The bundled OpenAL backend is thread-affine for a second, harder reason: OpenAL's context
    /// currency is process-wide, so no lock on this type could make it safe. See `OpenAlBackend.create`.
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

    /// Decoded payload of a WAV file.
    type PcmData =
        { /// The `wFormatTag` from the `fmt ` chunk: which codec `Data` is actually in.
          /// `FormatPcm` (1) is the only one this component can play — see `tryParse`.
          ///
          /// Already resolved through `WAVE_FORMAT_EXTENSIBLE` (0xFFFE): a PCM file written in the
          /// extensible form — routine for multichannel exports — reports `FormatPcm` here, not
          /// 0xFFFE. It stays 0xFFFE only when the subformat GUID could not be read at all, which is
          /// not a claim that the file is PCM.
          FormatTag: int
          Channels: int
          BitsPerSample: int
          SampleRate: int
          Data: byte[] }

    /// Parse a minimal WAV (RIFF/WAVE, fmt + data chunks). Total; returns None on anything it does
    /// not understand rather than throwing, and terminates on any input — including a corrupt chunk
    /// size, which once made the walk spin forever.
    ///
    /// A STRUCTURAL parse: it reports what the header says and decides nothing about playability. A
    /// `Some` therefore does NOT mean the file can be played — a 5-channel 32-bit WAV parses here,
    /// and so does an IEEE-float one. Check `FormatTag` against `FormatPcm` before trusting `Data`
    /// to be PCM; the bundled OpenAL backend does, and reports `AssetDiagnostics.UnsupportedCodec`
    /// when it is not.
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

    /// RAW drive. Fold a per-frame batch of requests through the backend in dispatch order. The
    /// product's `update` is unchanged: it emits AudioEffect values; this plays them.
    ///
    /// There is NO mixing here: an effect satisfying `requiresEngine` is discarded or degraded rather
    /// than realized (#27), so a volume slider built on this sink does nothing. The first batch that
    /// carries such an effect logs one diagnostic to stderr naming the surface that does realize it —
    /// `FS.GG.Audio.Engine`'s `Engine.createSink`, an `AudioEffect list -> unit` of this exact shape.
    /// Keep `play` for deliberate fire-and-forget playback.
    ///
    /// This is THE raw drive, not merely one of them: `FS.GG.Audio.Elmish`'s `Audio.Cmd.ofEffects`
    /// delegates here (#29), so the dispatch-order guarantee and the diagnostic are the same on both
    /// surfaces. The warn-once latch is therefore shared and process-wide, and the message names the
    /// engine-backed destination for each surface (`Engine.createSink` here, `Audio.Cmd.ofEngine` in
    /// Elmish) rather than assuming which one dropped the effect.
    val play: backend: IAudioBackend -> effects: AudioEffect list -> unit

/// Public contract module. The deterministic, headless record-only backend — the default and
/// the test/CI backend (FR-002).
[<RequireQualifiedAccess>]
module NullBackend =

    /// Maximum number of raw effects retained in a Null backend's diagnostic history.
    [<Literal>]
    val DiagnosticCapacity: int = 64

    /// A bounded operational snapshot, separate from the deliberate recorder's audit Evidence.
    type DiagnosticSnapshot =
        {
            /// Raw effects most recently presented to `IAudioBackend.Play`, oldest first. These are
            /// not normalized through Core and never exceed `DiagnosticCapacity`.
            Recent: AudioEffect list
            /// Effects overwritten by the bounded ring since construction or `ClearDiagnostics`.
            DroppedCount: int64
        }

    /// A record-only backend: opens no device, never throws.
    ///
    /// Like every backend here it is NOT thread-safe and should be driven from one thread. It makes
    /// exactly one concurrency guarantee, and only because it used to be free and would otherwise
    /// have been lost: reading `Evidence` while another thread is in `Play` will not throw. Two
    /// threads in `Play` interleave as they always did.
    [<Sealed>]
    type T =
        interface IAudioBackend
        /// Accumulated evidence for a deliberately requested recorder — equal to
        /// `FS.GG.Audio.Core.Audio.interpret` of the same batch.
        ///
        /// A backend created deliberately by `NullBackend.create` retains each effect until `Clear`;
        /// the retained requests ARE the evidence a headless test asserts on. A backend substituted
        /// by `OpenAlBackend.create` after device failure records nothing: that production fallback
        /// is process-lifetime silence, not an observer, and therefore stays bounded without
        /// requiring an external caller to discover and clear it.
        ///
        /// Materialized on each read, so read it once and bind it rather than re-reading it in a loop.
        member Evidence: AudioEvidence
        /// Effects recorded since construction or the last `Clear`. Always zero for a substituted
        /// device-unavailable fallback. `Evidence.Requested.Length` without materializing the list.
        member RecordedCount: int
        /// Why this backend is silent (#34): `Requested` when the product built it on purpose,
        /// `DeviceUnavailable` when `OpenAlBackend.create` substituted it. Prefer `Backend.kindOf`,
        /// which answers the same question for ANY `IAudioBackend` without a type test.
        member Silence: Silence
        /// Drop everything recorded so far. `Evidence` is then empty until the next `Play` on a
        /// deliberately requested recorder. This does not clear the separate diagnostic history.
        member Clear: unit -> unit
        /// Recent raw effects accepted by this silent backend, oldest first, plus the number
        /// overwritten. This fixed-size operational history is available for both deliberately
        /// requested recorders and device-unavailable fallbacks; it is not audit Evidence.
        member Diagnostics: DiagnosticSnapshot
        /// Clear only the bounded operational history and reset its dropped count. Deliberate
        /// recorder Evidence is unchanged.
        member ClearDiagnostics: unit -> unit

    /// Create a fresh Null backend. Its `Silence` is `Requested` — this is the deliberate,
    /// record-only backend, never a substitution.
    val create: unit -> T

/// Public contract module. The real OpenAL device backend (Silk.NET.OpenAL) (FR-003, FR-004).
[<RequireQualifiedAccess>]
module OpenAlBackend =

    /// Attempt to open an OpenAL device and return a backend that plays through it. If the device
    /// or the OpenAL Soft native library is unavailable, log the reason and return a Null backend
    /// instead (degrade-to-zero, FR-004) — the returned IAudioBackend is always usable, never null,
    /// and never throws into game code.
    ///
    /// **That substitution is silent unless you ask (#34).** The returned value is an `IAudioBackend`
    /// either way, so a caller who does not check cannot tell a device from a no-op: a shipped
    /// game runs silently, and a headless test suite asserts playback against a no-op and passes
    /// *because* nothing played. Ask `Backend.isDeviceBacked` (or `Backend.kindOf`, which also carries
    /// the device's reason) — in a product, to surface "no audio device" in its own UI rather than
    /// trusting stderr; in a test, to SKIP loudly rather than assert vacuously.
    ///
    /// The device backend also implements `IMixingBackend` (#11), so driven by `FS.GG.Audio.Engine`
    /// it spatializes: pan reaches the hardware, and bus fades/ducks reach the music voice. The Null
    /// fallback does not, which is exactly what makes the Engine take its non-positional degrade path
    /// on a machine with no device.
    /// Spatialization is per-source, so a positional sound must be a **mono** asset; OpenAL plays a
    /// stereo buffer centred, whatever position it is given.
    ///
    /// An id `resolver` cannot resolve — or resolves to bytes this backend cannot decode — plays as
    /// SILENCE (there is nothing to play), but not silently: the id and the reason are named once on
    /// stderr (#28, see `AssetDiagnostics`). Playback is otherwise untouched, and a missing track in
    /// particular does not stop the music already playing.
    ///
    /// ONE THREAD PER BACKEND — and in practice, one backend. OpenAL's context currency
    /// (`alcMakeContextCurrent`) is process-wide rather than per-object. Each backend re-asserts its
    /// own context before every device call, so two backends **on one thread** coexist correctly
    /// (they did not before 2026-07-16: the second silently broke the first, and every call on it
    /// came back `AL_INVALID_NAME`). But two backends driven from two threads race for that currency
    /// by construction, and no lock inside this library can fix it. A game wants one device backend.
    val create: resolver: AssetResolver -> IAudioBackend
