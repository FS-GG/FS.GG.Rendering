# Quickstart: make a scaffolded game play a sound

## 1. Scaffold a game

```sh
dotnet new fs-gg-ui --profile game -o MyGame
cd MyGame
```

The audio seam is already wired. `src/Product/Program.fs` creates a backend and hands the viewer a
sink; you do not open that file.

## 2. Say what makes a sound

`src/Product/AudioCues.fs` is yours. It is a pure function of the message and the before/after model.
The Pong starter ships two cues:

```fsharp
let forTransition (msg: Msg) (previous: Model) (next: Model) : AudioEffect list =
    match msg with
    | Tick _ when scored previous next  -> [ Audio.playSfx (SoundId "score") 0.9 ]
    | Tick _ when bounced previous next -> [ Audio.playSfx (SoundId "bounce") 0.6 ]
    | _ -> []
```

Add your own case. Effects play in list order. Return `[]` for a silent transition.

## 3. Give the id a file

```sh
mkdir -p assets/audio
cp ~/blip.wav assets/audio/bounce.wav     # SoundId "bounce" -> assets/audio/bounce.wav
```

`SoundId`/`TrackId` are names **you** own; `AudioCues.resolver` maps them to bytes. An id with no file
resolves to `None`, which the backend records as a no-op rather than throwing — so step 3 is optional
while you are still building, and the game still requests the right sounds.

## 4. Run it

```sh
dotnet run --project src/Product
```

You hear the click. If the box has no OpenAL or no device, `OpenAlBackend.create` quietly returns the
record-only Null backend: the game runs, silently, and never throws.

## 5. Prove it without a sound card

This is how you test audio — by value, the way you test `update`:

```fsharp
GeneratedAppHost.dispatchKey host keyEvent model
|> snd                                  // ViewerEffect list
|> GeneratedAppHost.audioRequests       // AudioEffect list, dispatch order, non-audio dropped
|> Audio.interpret                      // AudioEvidence
```

No window, no device, deterministic. Or drive the real record-only backend, which is the same
composition `Program.fs` installs:

```fsharp
use backend = NullBackend.create ()
GeneratedAppHost.audioRequests effects |> Audio.play backend
backend.Evidence.Requested   // exactly what the product asked to hear
```

## Wanted silence?

`Viewer.runApp` still exists and discards audio. That is the whole difference between the two paths.
