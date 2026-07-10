// See skill: fs-gg-audio
// Mirrored from FS-GG/FS.GG.Audio @ 0.1.0 (src/FS.GG.Audio.Elmish/Elmish.fsi); regenerate when $(FsGgAudioVersion) moves.
namespace FS.GG.Audio.Elmish

open FS.GG.Audio.Core
open FS.GG.Audio.Host

/// Public contract module. Elmish authoring surface for audio (DEC-004 of 002-audio-host).
/// A product's `update` returns these commands; the Elmish runtime executes them, playing through
/// the supplied backend. The commands dispatch no message (fire-and-forget audio), so `update`
/// stays a pure `Model -> Model * Cmd<'msg>`. Depends on Elmish (MIT) only — never on FS.GG.UI.
[<RequireQualifiedAccess>]
module Audio =

    /// Elmish command constructors that play `FS.GG.Audio.Core` effects through an IAudioBackend.
    [<RequireQualifiedAccess>]
    module Cmd =

        /// A command that plays a batch of effects through the backend, in dispatch order, when the
        /// Elmish runtime executes it. Dispatches no message.
        val ofEffects: backend: IAudioBackend -> effects: AudioEffect list -> Elmish.Cmd<'msg>

        /// Play one sound effect (mirrors `Core.Audio.playSfx`).
        val playSfx: backend: IAudioBackend -> sound: SoundId -> volume: float -> Elmish.Cmd<'msg>

        /// Play one music track (mirrors `Core.Audio.playMusic`).
        val playMusic: backend: IAudioBackend -> track: TrackId -> loop: bool -> Elmish.Cmd<'msg>

        /// Stop the current music (mirrors `Core.Audio.stopMusic`).
        val stopMusic: backend: IAudioBackend -> Elmish.Cmd<'msg>

        /// Set the master volume (mirrors `Core.Audio.setMasterVolume`).
        val setMasterVolume: backend: IAudioBackend -> level: float -> Elmish.Cmd<'msg>
