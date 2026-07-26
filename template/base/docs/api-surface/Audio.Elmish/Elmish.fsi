// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.5.0 (src/FS.GG.Audio.Elmish/Elmish.fsi); regenerate when $(FsGgAudioVersion) moves.
namespace FS.GG.Audio.Elmish

open FS.GG.Audio.Core
open FS.GG.Audio.Host
open FS.GG.Audio.Engine

/// Public contract module. Elmish authoring surface for audio (DEC-004 of 002-audio-host).
/// A product's `update` returns these commands; the Elmish runtime executes them, playing through
/// the supplied backend. The commands dispatch no message (fire-and-forget audio), so `update`
/// stays a pure `Model -> Model * Cmd<'msg>`. Depends on Elmish (MIT) only — never on FS.GG.UI.
///
/// Two playback paths, and the difference is load-bearing:
///   • `ofEffects` and the single-effect constructors play STRAIGHT at the `IAudioBackend`. There
///     is no mixing: `SetBusVolume`/`Duck` are backend no-ops and `PlaySfx3D` degrades to
///     non-positional. Use this for simple fire-and-forget playback.
///   • `ofEngine` routes an `AudioEffect` batch through `FS.GG.Audio.Engine.step`, so bus volume,
///     master scaling, side-chain ducking, and 3D positioning apply. Time-based envelopes (a `Duck`,
///     or a fade installed via the `Engine.fadeBus`/`crossFade` methods — those are engine calls,
///     not effects) advance only as the engine is stepped, so keep driving `ofEngine` each frame for
///     them to progress and restore. Reach for it whenever those semantics matter.
[<RequireQualifiedAccess>]
module Audio =

    /// Elmish command constructors that play `FS.GG.Audio.Core` effects.
    [<RequireQualifiedAccess>]
    module Cmd =

        /// RAW-BACKEND bridge: a command that plays a batch of effects STRAIGHT through the backend,
        /// in dispatch order, when the Elmish runtime executes it. Dispatches no message.
        /// No mixing is applied — `SetBusVolume`/`Duck` are backend no-ops and `PlaySfx3D` plays
        /// non-positional. For bus mixing / fades / ducking / 3D, use `ofEngine` instead.
        ///
        /// That drop is no longer SILENT (#29). This delegates to `FS.GG.Audio.Host`'s `Audio.play`
        /// rather than driving the backend itself, so the first batch carrying an effect the raw path
        /// cannot realize logs one diagnostic to stderr — naming `ofEngine` as the destination that
        /// does realize it. Playback is unchanged; only the silence is.
        val ofEffects: backend: IAudioBackend -> effects: AudioEffect list -> Elmish.Cmd<'msg>

        /// ENGINE-BACKED path: a command that advances the supplied engine by `dt` and realizes the
        /// `AudioEffect` batch through `FS.GG.Audio.Engine.step`, so bus volume, master scaling,
        /// side-chain ducking, and 3D positioning apply. Time-based envelopes (a `Duck`, or a fade
        /// installed via `Engine.fadeBus`/`crossFade`) advance only as the engine is stepped, so
        /// drive this each frame for them to progress and restore. The caller owns the `Engine.T`
        /// and the per-frame `dt` (exactly as a direct `Engine.step` caller does). Dispatches no message.
        ///
        /// AN `Engine.T` IS NOT THREAD-SAFE, AND THIS IS THE SURFACE WHERE THAT MATTERS. A `Cmd` is a
        /// description: the Elmish runtime decides when — and on which THREAD — to run it. The caller
        /// owns the engine, so the caller owns ensuring the runtime's effect thread is the thread that
        /// owns the engine. Nothing here can check it, and a mismatch corrupts the engine's bus state
        /// silently rather than raising. Under the standard Elmish runtime, effects run on the thread
        /// that dispatched — the ordinary single-threaded loop, where this is a non-issue. It is worth
        /// knowing about if yours dispatches from elsewhere.
        val ofEngine: engine: T -> dt: float -> effects: AudioEffect list -> Elmish.Cmd<'msg>

        /// Play one sound effect straight at the backend, no mixing (mirrors `Core.Audio.playSfx`).
        val playSfx: backend: IAudioBackend -> sound: SoundId -> volume: float -> Elmish.Cmd<'msg>

        /// Play one music track straight at the backend, no mixing (mirrors `Core.Audio.playMusic`).
        val playMusic: backend: IAudioBackend -> track: TrackId -> loop: bool -> Elmish.Cmd<'msg>

        /// Stop the current music straight at the backend (mirrors `Core.Audio.stopMusic`).
        val stopMusic: backend: IAudioBackend -> Elmish.Cmd<'msg>

        /// Set the master volume straight at the backend, a no-op unless the backend honours it
        /// (mirrors `Core.Audio.setMasterVolume`; the Engine owns real master scaling).
        val setMasterVolume: backend: IAudioBackend -> level: float -> Elmish.Cmd<'msg>
