module AppRoot.AudioCues

open System.IO
open FS.GG.Audio.Core
open FS.GG.Audio.Host
//#if (profile == "game")
open AppRoot.Geometry
//#endif
open AppRoot.Model

// ─────────────────────────────────────────────────────────────────────────────────────────────
// Sound, as pure values (issue #245).
//
// `forTransition` is the ONLY place this product decides what to play. It is a pure function of the
// message and the before/after model — no device, no file handle, no `unit -> unit`. The host turns
// the returned values into real playback: `Program.fs` passes `Audio.play backend` to the launch
// entry point, and the viewer hands it each `ViewerEffect.PlayAudio` batch in dispatch order. Your
// `update` never changes.
//
// Why BOTH models are in the signature: most gameplay cues (a shot, a death, a pickup, a room seal)
// have NO `Msg` of their own — they are recovered by DIFFING `previous` against `next`, so you add
// them without touching `Msg` or `Model.fs`. The `scored`/`bounced` helpers below do exactly this.
// Its coverage boundary lives at the point of use — see the `Tick` cues.
//
// REPLACE ME, with Model.fs. This file names your `Msg` cases and reads your `Model`'s fields, so a
// model swap rewrites it. The seam that carries its output (`PlayAudio` + the `*WithAudio` launch)
// is durable and does not — see docs/scaffold-map.md.
//
// Test it the way you test `update` — by value, with no sound card:
//     AudioCues.forTransition SaveRequested before after = [ Audio.playSfx (SoundId "save") 0.7 ]
//
// ── Which host carries it (issue #436) ───────────────────────────────────────────────────────
//
// Both of them, through the same seam. The two starter profiles ship DIFFERENT `Msg` types, so the
// cue map below is written twice — but the SIGNATURE is identical, and both hosts in
// EvidenceCommands.fs call it from `Init` AND `Update`:
//
//     app                  -> `interactiveHost`, launched by `ControlsElmish.runInteractiveAppWithAudio`
//     game / sample-pack   -> `generatedHost`,   launched by `Viewer.runAppWithAudio`
//
// That the Controls family can request sound at all is #429; that a scaffolded product on the `app`
// profile can REACH it is #436 — before which `app` referenced none of the FS.GG.Audio packages and
// launched through a sinkless overload, so "every game with a menu" got a silent menu.
// ─────────────────────────────────────────────────────────────────────────────────────────────

/// Where `resolver` looks for PCM WAV files, relative to the running product.
[<Literal>]
let assetRoot = "assets/audio"

/// One product-owned entry point for the cue vocabulary.  Keep ids here rather than
/// duplicating a hand-counted list in a test: adding a request without declaring its
/// asset is intentionally visible, and adding a declaration without an asset is red.
let declaredCueIds : SoundId list =
//#if (profile == "game")
    [ SoundId "start"; SoundId "score"; SoundId "bounce" ]
//#else
    [ SoundId "start"; SoundId "save"; SoundId "select"; SoundId "navigate" ]
//#endif

/// A resolver finding is deliberately separate from `AudioEvidence`: the latter proves
/// that an effect was requested; this proves that packaged product content can realize it.
type CueResolution =
    { CueId: SoundId
      ExpectedPath: string
      Problem: string option }

let private expectedPath root (SoundId id) = Path.Combine(root, id + ".wav")

/// A small, deterministic WAV sanity check. It is not an audio decoder; malformed headers
/// are rejected here so a text file renamed to `.wav` cannot make readiness green.
let private isWave (bytes: byte[]) =
    bytes.Length >= 44
    && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) = "RIFF"
    && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) = "WAVE"

let private tryReadAsset (id: SoundId) =
    let path = expectedPath assetRoot id
    try
        if File.Exists path then
            let bytes = File.ReadAllBytes path
            if isWave bytes then Some bytes else None
        else None
    with
    | :? IOException -> None

/// Run this in a product build/publish readiness check. It names every missing or malformed
/// cue and its expected packaged path; a request-only test must never stand in for it.
let resolutionEvidenceAt (root: string) : CueResolution list =
    declaredCueIds
    |> List.map (fun id ->
        let path = expectedPath root id
        let problem =
            try
                if not (File.Exists path) then Some "missing"
                elif isWave (File.ReadAllBytes path) then None
                else Some "malformed WAV"
            with
            | :? IOException -> Some "unreadable"

        { CueId = id; ExpectedPath = path; Problem = problem })

let resolutionEvidence () : CueResolution list = resolutionEvidenceAt assetRoot

/// Readiness is false for the intentionally asset-less scaffold. Add real assets, or generate
/// deterministic reviewable PCM WAV bytes from committed source, before a build/publish gate says
/// audio content is ready. Runtime playback may still degrade safely to silence.
let audioContentReadyAt root = resolutionEvidenceAt root |> List.forall (fun finding -> finding.Problem.IsNone)

let audioContentReady () = audioContentReadyAt assetRoot

/// The product owns the id -> asset mapping; the framework never does (FS.GG.Audio FR-005).
/// An id with no file on disk resolves to `None`, which the backend treats as a recorded no-op —
/// so a product with no assets yet still runs, and still requests the right sounds.
///
/// Model-agnostic on purpose: this half survives a model swap even though `forTransition` does not,
/// which is why it sits above the per-starter split below.
let resolver: AssetResolver =
    { ResolveSound = tryReadAsset
      ResolveTrack = fun (TrackId id) -> tryReadAsset (SoundId id) }

// ─────────────────────────────────────────────────────────────────────────────────────────────
// `Started`, and the trap it exists to close (issue #458)
//
// `forTransition` is a function of a TRANSITION. The initial model does not make one: it is produced
// by `initialModel`, not dispatched into. So without `Started`, ANY sound the initial state implies
// is never requested — and that is a hole in this pattern, not a bug in a function.
//
// It bites the moment you *load* state instead of *transitioning into* it:
//
//     Load the player's saved settings in `initialModel`, fold them into the model, and the model
//     is CORRECT. The settings ARE loaded. And the mixer is never told, because no transition ever
//     carried them to it. Nothing catches this — no type is wrong, no requirement unsatisfied, and
//     a test that asserts on the model passes, because a restored volume the mixer never heard is
//     indistinguishable, from inside the model, from one that was restored properly.
//
//     It surfaces later as: turn the music down, restart, and get full-volume music from a settings
//     screen that correctly reports it as quiet.
//
// The same applies to a save game, restored window geometry, a resumed session, a replayed
// checkpoint — anything that enters the model through a door a transition-shaped seam is not
// watching. `Started` is that door, and the host dispatches it as `forTransition Started m m`.
//
// So: **put anything the initial state implies under `Started`**, e.g.
//
//     | Started -> [ Audio.setMasterVolume next.Settings.Volume
//                    Audio.playMusic (TrackId "theme") true ]
//
// Assert it at the SINK, not at the model — the only test that catches this class asks *what the
// engine was told*, not *what the model holds*.
//
// Both starters carry `Started` for this reason. It was the game's alone until #436 gave the
// Controls starter a cue seam; a seam without `Started` would have reproduced #458 one profile over.
// ─────────────────────────────────────────────────────────────────────────────────────────────

//#if (profile == "game")
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
    // NET-DIFF BOUNDARY: one `Tick` drains a WHOLE run of fixed sim steps (Model.advanceSim runs
    // `stepSim` that many times), so `previous`/`next` straddle the entire drain — an event that both
    // appears AND disappears inside that one host frame nets to no diff here and never cues. The real
    // cues are safe (a score persists to the next frame; a bounce reverses and stays reversed), but a
    // same-frame spawn+consume cue would silently not fire — cue it from its own `Msg` instead.
    //
    // A score resets the ball, which also reverses it — so score wins over bounce, and only one
    // sound plays on that frame.
    | Tick _ when scored previous next -> [ Audio.playSfx (SoundId "score") 0.9 ]
    | Tick _ when bounced previous next -> [ Audio.playSfx (SoundId "bounce") 0.6 ]
    | _ -> []
//#else
/// What this product asks to hear when `msg` takes it from `previous` to `next`.
/// Return `[]` for a silent transition. Effects play in list order.
///
/// Drop a WAV at `assets/audio/<id>.wav` and you hear it; leave it out and the request is recorded
/// but silent. Add your own cases — this is your file.
///
/// This is the CONTROLS starter's cue map (issue #436): an app's sounds are its interaction
/// feedback — a page turn, a committed save, a selection.
///
/// Note what `previous`/`next` buy you, because the page-turn cue below is the whole argument for
/// them being in the signature at all: a cue belongs to a TRANSITION, not to whichever message
/// happens to cause it. Several messages can cause one, and in this starter they do.
let forTransition (msg: Msg) (previous: Model) (next: Model) : AudioEffect list =
    match msg with
    // Shipped WIRED, not empty — see the `Started` note above. A `Started` case that returned `[]`
    // would make the Product.Tests regression test vacuous: it would pass whether or not `Init` is
    // routed through the seam at all, which is exactly how #458 survived its own first fix.
    // Silent until you drop `assets/audio/start.wav`; an unresolved id is a recorded no-op.
    | Started -> [ Audio.playSfx (SoundId "start") 0.5 ]
    // The user committed something — the one interaction in the starter that changes the world.
    | SaveRequested -> [ Audio.playSfx (SoundId "save") 0.7 ]
    | GridSelectionChanged _ -> [ Audio.playSfx (SoundId "select") 0.3 ]
    // A page turn, cued on the TRANSITION rather than on a message.
    //
    // Matching `| Navigated _` instead would be the natural-looking mistake, and it would cue the one
    // path this scaffold never takes: the starter navigates from the KEYBOARD, so a page change
    // arrives as `ViewerInput`/`ViewerKeyEventReceived` and `transitionViewerInput` folds it into a
    // new `Page` — `Navigated` is dispatched by nobody. You would drop `navigate.wav` in, hear
    // nothing, and blame the seam. Ask what CHANGED instead, and both routes cue.
    //
    // Quieter than the save: navigating is not committing. `Tick` cannot reach here — it never moves
    // the page — and it deliberately has no cue of its own, because a per-frame sound is a buzzsaw.
    | _ when next.Page <> previous.Page -> [ Audio.playSfx (SoundId "navigate") 0.4 ]
    | _ -> []
//#endif
