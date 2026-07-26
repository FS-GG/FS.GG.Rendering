// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.5.0 (src/FS.GG.Audio.Engine/Engine.fsi); regenerate when $(FsGgAudioVersion) moves.
namespace FS.GG.Audio.Engine

open FS.GG.Audio.Core
open FS.GG.Audio.Host

/// Public contract type. Distance-attenuation configuration for 3D voices (DEC-001). Defaults
/// (`Engine.defaultSpatial`): `RefDistance = 1`, `Rolloff = 1`, `MaxDistance = None` (uncapped).
///
/// Attenuation ONLY. None of these affect `Voice.Pan`, which is the sine of the azimuth to the
/// source and is a pure direction — so tuning the distance model cannot silently re-tune the
/// panning. (It once could: `RefDistance` doubled as the pan width.)
type SpatialConfig =
    { /// Distance at which a voice plays unattenuated; attenuation begins beyond it.
      RefDistance: float
      /// How sharply gain falls off past `RefDistance`, in the inverse-distance model
      /// `ref / (ref + rolloff * (d - ref))`. `0` disables attenuation.
      Rolloff: float
      /// Distance past which a voice attenuates no further. `None` leaves it uncapped. It caps how
      /// far away a source SOUNDS, never the direction it comes from.
      MaxDistance: float option }

/// Public contract type. A one-shot voice realized on a `step`, exposed for headless assertions.
/// `EffectiveGain` = request-gain × bus gain × Master gain × distance attenuation, clamped to
/// `[0,1]`. `Positional` is false when 3D was unavailable (the backend is not an `IMixingBackend`)
/// and the voice degraded to a non-positional one at the bus-scaled gain.
type Voice =
    { Sound: SoundId
      Bus: Bus
      RequestGain: float
      EffectiveGain: float
      /// Stereo pan in `[-1, 1]`: `-1` hard left, `0` centred, `+1` hard right. Total — a
      /// non-finite emitter position gives `0.0` (centred), never `nan`.
      ///
      /// The SINE OF THE AZIMUTH to the source, so it is a pure direction and does not vary with
      /// distance: a source far away but nearly straight ahead is near-centred, and only one beside
      /// the listener is hard-panned. Front and back are not distinguishable in a scalar pan (45°
      /// behind-right and 45° ahead-right both give `+0.707`); elevation is out by DEC-001.
      ///
      /// `0.0` whenever `Positional` is false — a degraded voice has no direction to carry.
      Pan: float
      Positional: bool }

/// Public contract type. The mixing/voice engine: named buses with independent gain, active
/// fade/duck envelopes, the listener, and the single music voice. A pure, deterministic state
/// model advanced once per frame by `Engine.step`; it opens no device of its own and never throws.
///
/// NOT THREAD-SAFE. Drive one engine from ONE thread: `step`, `fadeBus`, `crossFade` and
/// `setListener` all mutate shared state with no locking, and the observables (`BusGain`,
/// `LastVoices`, `MusicGain`) read it. Two threads in `step` can corrupt the bus dictionaries — a
/// torn resize is not a wrong gain, it is undefined behaviour, and it will not announce itself.
///
/// Deliberate, not an omission: a game mixes on one thread, and locking a per-frame call to serve a
/// case nobody has would cost every caller for nothing. It is written down because the surface does
/// not imply it — an `Engine.T` looks like an ordinary object, and `FS.GG.Audio.Elmish`'s
/// `Audio.Cmd.ofEngine` hands `step` to the Elmish runtime, which runs effects on whatever thread it
/// likes. If that is not your engine's thread, that is the caller's problem to solve, and this is the
/// notice that there is one to solve.
[<Sealed>]
type T =
    /// The bus's own realized gain in `[0,1]` (base gain shaped by any active fade, times any
    /// active duck). Master is NOT folded in here — it is applied to each voice's `EffectiveGain`.
    member BusGain: bus: Bus -> float
    /// Realized gain of the current music voice (`Music` bus gain × Master), or `0.0` when none.
    member MusicGain: float
    /// Current listener position (metres).
    member Listener: float * float * float
    /// The one-shot voices realized on the most recent `step`, in dispatch order.
    member LastVoices: Voice list
    /// The device-fault latch this engine's realize pass reports through (#33). Ask it whether the
    /// backend is faulting persistently right now — `Device.IsPersistent DeviceDiagnostics.Realize` is
    /// the machine-readable form of "nothing this process plays is currently audible".
    ///
    /// Note it reports the realize pass of a backend that throws AT the engine. The bundled OpenAL
    /// backend guards its own device calls and so never throws this far; its faults surface on its own
    /// latch, under `PlayAt`/`SetListener`/`SetBusGain`/`Play`.
    member Device: DeviceDiagnostics.T

/// Public contract module. Engine construction and the per-frame drive.
[<RequireQualifiedAccess>]
module Engine =

    /// The default distance-attenuation configuration: `RefDistance = 1`, `Rolloff = 1`, uncapped.
    val defaultSpatial: SpatialConfig

    /// Create an engine over a backend (with `defaultSpatial`). Buses start at unity gain. The
    /// engine feature-detects `IMixingBackend` to realize continuous mixing/spatial control.
    val create: backend: IAudioBackend -> T

    /// Create an engine over a backend with an explicit spatial configuration.
    val createWith: config: SpatialConfig -> backend: IAudioBackend -> T

    /// Advance the engine by `dt` seconds and apply a per-frame batch of effects, in order:
    /// completes elapsed fade/duck envelopes, applies the batch (bus/master sets, ducks, one-shot
    /// and 3D voices, music start/stop), resolves effective gains, and realizes through the
    /// backend (`IMixingBackend` when present, else plain `Play` with gains folded in). Total;
    /// never throws — identical inputs produce identical realized calls (FR-007/FR-008).
    val step: engine: T -> dt: float -> effects: AudioEffect list -> unit

    /// Install a timed linear fade of a bus's base gain to `target` over `seconds` (DEC-003),
    /// realized as subsequent `step`s advance. `seconds <= 0` sets the gain immediately.
    ///
    /// Total on a non-finite `seconds`. `nan` sets the gain immediately, as `<= 0` does — it is not a
    /// duration and there is no intent in it to honour. An INFINITE `seconds` is honoured as what it
    /// says: a fade so slow it never arrives, holding the bus at its current gain. (It is not treated
    /// as immediate, which would make `fadeBus engine Music 0.0 infinity` silence the music at once —
    /// the opposite of the request.)
    val fadeBus: engine: T -> bus: Bus -> target: float -> seconds: float -> unit

    /// Install an equal-power cross-fade over `seconds` (DEC-003): `fromBus` ramps from its current
    /// gain down to 0 and `toBus` ramps from 0 up to unity, at constant summed power.
    ///
    /// `seconds` is treated exactly as `fadeBus` treats it, including the non-finite cases — but note
    /// what an infinite one means here: `toBus` holds at the START of its ramp, which is `0` (silent),
    /// not at whatever gain it had before. `fromBus` holds at its current gain, as `fadeBus` does.
    ///
    /// Note `toBus` ramps to UNITY, not to whatever gain it was configured with — so a cross-fade
    /// onto a bus the player had turned down will put it back to full and leave it there. That is
    /// long-standing behaviour and is called out here rather than changed (review 2026-07-16 §3.7).
    val crossFade: engine: T -> fromBus: Bus -> toBus: Bus -> seconds: float -> unit

    /// Set the listener position (metres) used to resolve subsequent 3D voices (FR-005).
    val setListener: engine: T -> x: float -> y: float -> z: float -> unit
