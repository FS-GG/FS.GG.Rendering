module AppRoot.AudioCues

open System.IO
open FS.GG.Audio.Core
open FS.GG.Audio.Host
open AppRoot.Model

// ─────────────────────────────────────────────────────────────────────────────────────────────
// Sound, as pure values (issue #245).
//
// `forTransition` is the ONLY place this product decides what to play. It is a pure function of the
// message and the before/after model — no device, no file handle, no `unit -> unit`. The host turns
// the returned values into real playback: `Program.fs` passes `Audio.play backend` to
// `Viewer.runAppWithAudio`, and the viewer hands it each `ViewerEffect.PlayAudio` batch in dispatch
// order. Your `update` never changes.
//
// REPLACEABLE. When you swap the starter model for your own, rewrite this file: it names your `Msg`
// cases. The seam that carries its output (`PlayAudio` / `runAppWithAudio`) is durable and does not.
//
// Test it the way you test `update` — by value, with no sound card:
//     AudioCues.forTransition SaveRequested before after = [ Audio.playSfx (SoundId "save") 0.8 ]
// ─────────────────────────────────────────────────────────────────────────────────────────────

/// Where `resolver` looks for PCM WAV files, relative to the running product.
[<Literal>]
let assetRoot = "assets/audio"

let private soundPath (name: string) = Path.Combine(assetRoot, name + ".wav")

let private tryReadAsset (name: string) =
    let path = soundPath name
    if File.Exists path then Some(File.ReadAllBytes path) else None

/// The product owns the id -> asset mapping; the framework never does (FS.GG.Audio FR-005).
/// An id with no file on disk resolves to `None`, which the backend treats as a recorded no-op —
/// so a game with no assets yet still runs, and still requests the right sounds.
let resolver: AssetResolver =
    { ResolveSound = fun (SoundId id) -> tryReadAsset id
      ResolveTrack = fun (TrackId id) -> tryReadAsset id }

/// What this product asks to hear when `msg` takes it from `previous` to `next`.
/// Return `[]` for a silent transition. Effects play in list order.
let forTransition (msg: Msg) (previous: Model) (next: Model) : AudioEffect list =
    match msg with
    | SaveRequested -> [ Audio.playSfx (SoundId "save") 0.8 ]
    | Navigated _ when previous.Page <> next.Page -> [ Audio.playSfx (SoundId "navigate") 0.5 ]
    | _ -> []
