module AppRoot.AudioCues

open System.IO
open FS.GG.Audio.Core
open FS.GG.Audio.Host
open AppRoot.Geometry
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
// REPLACE ME, with Model.fs. This file names your `Msg` cases and reads your `Model`'s fields, so a
// model swap rewrites it. The seam that carries its output (`PlayAudio` / `runAppWithAudio`) is
// durable and does not — see docs/scaffold-map.md.
//
// Test it the way you test `update` — by value, with no sound card:
//     AudioCues.forTransition (Tick 0.016) before after = [ Audio.playSfx (SoundId "bounce") 0.6 ]
// ─────────────────────────────────────────────────────────────────────────────────────────────

/// Where `resolver` looks for PCM WAV files, relative to the running product.
[<Literal>]
let assetRoot = "assets/audio"

let private tryReadAsset (name: string) =
    let path = Path.Combine(assetRoot, name + ".wav")
    if File.Exists path then Some(File.ReadAllBytes path) else None

/// The product owns the id -> asset mapping; the framework never does (FS.GG.Audio FR-005).
/// An id with no file on disk resolves to `None`, which the backend treats as a recorded no-op —
/// so a game with no assets yet still runs, and still requests the right sounds.
let resolver: AssetResolver =
    { ResolveSound = fun (SoundId id) -> tryReadAsset id
      ResolveTrack = fun (TrackId id) -> tryReadAsset id }

let private scored (previous: Model) (next: Model) =
    next.LeftScore > previous.LeftScore || next.RightScore > previous.RightScore

/// A bounce is the frame the ball reverses along either axis — off a wall or off a paddle.
/// `before * after < 0.0` is "strictly opposite signs": a component that was or becomes zero is a
/// ball starting or stopping along that axis, not a reflection. (`sign 0.0 = 0`, so comparing signs
/// would call that a bounce.)
let private bounced (previous: Model) (next: Model) =
    let reversed (before: float) (after: float) = before * after < 0.0
    reversed previous.Ball.Velocity.Vx next.Ball.Velocity.Vx
    || reversed previous.Ball.Velocity.Vy next.Ball.Velocity.Vy

/// What this product asks to hear when `msg` takes it from `previous` to `next`.
/// Return `[]` for a silent transition. Effects play in list order.
///
/// Drop a WAV at `assets/audio/<id>.wav` and you hear it; leave it out and the request is recorded
/// but silent. Add your own cases — this is your file.
///
/// ── `Started`, and the trap it exists to close (issue #458) ──────────────────────────────────
///
/// `forTransition` is a function of a TRANSITION. The initial model does not make one: it is
/// produced by `initialModel`, not dispatched into. So without `Started`, ANY sound the initial
/// state implies is never requested — and that is a hole in this pattern, not a bug in a function.
///
/// It bites the moment you *load* state instead of *transitioning into* it:
///
///     Load the player's saved settings in `initialModel`, fold them into the model, and the model
///     is CORRECT. The settings ARE loaded. And the mixer is never told, because no transition ever
///     carried them to it. Nothing catches this — no type is wrong, no requirement unsatisfied, and
///     a test that asserts on the model passes, because a restored volume the mixer never heard is
///     indistinguishable, from inside the model, from one that was restored properly.
///
///     It surfaces later as: turn the music down, restart, and get full-volume music from a settings
///     screen that correctly reports it as quiet.
///
/// The same applies to a save game, restored window geometry, a resumed session, a replayed
/// checkpoint — anything that enters the model through a door a transition-shaped seam is not
/// watching. `Started` is that door, and the host dispatches it as `forTransition Started m m`.
///
/// So: **put anything the initial state implies under `Started`**, e.g.
///
///     | Started -> [ Audio.setMasterVolume next.Settings.Volume
///                    Audio.playMusic (TrackId "theme") ]
///
/// Assert it at the SINK, not at the model — the only test that catches this class asks *what the
/// engine was told*, not *what the model holds*.
let forTransition (msg: Msg) (previous: Model) (next: Model) : AudioEffect list =
    match msg with
    // The scaffold ships this seam WIRED rather than empty, for the same reason #245 shipped the
    // bounce/score cues wired: a seam demonstrated is a seam an author trusts and edits, and a seam
    // that emits nothing is one nobody can tell is broken. (It is also what gives the regression test
    // in Product.Tests a real failure leg — with `[]` here, a test asserting "Init emits the cues"
    // passes whether or not `Init` is wired to the seam at all, which is how this class of bug
    // survives its own fix.)
    //
    // Silent until you drop `assets/audio/start.wav` — an unresolved id is a recorded no-op, never an
    // error. Replace this with whatever YOUR initial state implies (restored volume, resumed music).
    | Started -> [ Audio.playSfx (SoundId "start") 0.5 ]
    // A score resets the ball, which also reverses it — so score wins over bounce, and only one
    // sound plays on that frame.
    | Tick _ when scored previous next -> [ Audio.playSfx (SoundId "score") 0.9 ]
    | Tick _ when bounced previous next -> [ Audio.playSfx (SoundId "bounce") 0.6 ]
    | _ -> []
