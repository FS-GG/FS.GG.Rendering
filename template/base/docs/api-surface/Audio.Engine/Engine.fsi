// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.3.0 (src/FS.GG.Audio.Engine/Engine.fsi); regenerate when $(FsGgAudioVersion) moves.
namespace FS.GG.Audio.Engine

open FS.GG.Audio.Core
open FS.GG.Audio.Host

/// Public contract type. Distance-attenuation configuration for 3D voices (DEC-001). Defaults
/// (`Engine.defaultSpatial`): `RefDistance = 1`, `Rolloff = 1`, `MaxDistance = None` (uncapped).
type SpatialConfig =
    { RefDistance: float
      Rolloff: float
      MaxDistance: float option }

/// Public contract type. A one-shot voice realized on a `step`, exposed for headless assertions.
/// `EffectiveGain` = request-gain × bus gain × Master gain × distance attenuation, clamped to
/// `[0,1]`. `Pan` is in `[-1, 1]`. `Positional` is false when 3D was unavailable (the backend is
/// not an `IMixingBackend`) and the voice degraded to a non-positional one at the bus-scaled gain.
type Voice =
    { Sound: SoundId
      Bus: Bus
      RequestGain: float
      EffectiveGain: float
      Pan: float
      Positional: bool }

/// Public contract type. The mixing/voice engine: named buses with independent gain, active
/// fade/duck envelopes, the listener, and the single music voice. A pure, deterministic state
/// model advanced once per frame by `Engine.step`; it opens no device of its own and never throws.
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
    val fadeBus: engine: T -> bus: Bus -> target: float -> seconds: float -> unit

    /// Install an equal-power cross-fade over `seconds` (DEC-003): `fromBus` ramps from its current
    /// gain down to 0 and `toBus` ramps from 0 up to unity, at constant summed power.
    val crossFade: engine: T -> fromBus: Bus -> toBus: Bus -> seconds: float -> unit

    /// Set the listener position (metres) used to resolve subsequent 3D voices (FR-005).
    val setListener: engine: T -> x: float -> y: float -> z: float -> unit
