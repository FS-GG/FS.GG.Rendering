module Canvas.Tests.AudioTests

// Feature 243 (US1 + US2): pure audio request surface + record-only interpreter.
// US1 — a product's `update` emits AudioEffect values with zero IO.
// US2 — the record-only interpreter folds requests into ordered AudioEvidence, headless-safe:
//        never touches a device, never blocks, never throws, clamps out-of-range volume.
// All real pure computation — no synthetic evidence (nothing is faked; the recorded requests ARE
// the evidence).

open Expecto
open FsCheck
open FS.GG.UI.Canvas

// --- US1: a tiny pure product model whose `update` maps game events to requested audio effects.
//     This stands in for a real product's update: pure, returns AudioEffect values, does no IO.

type private GameEvent =
    | Fired
    | EnteredLevel
    | Paused
    | Muted

// The whole point: `update` is a pure function to an AudioEffect — no device, no IO, no state.
let private update (event: GameEvent) : AudioEffect =
    match event with
    | Fired -> Audio.playSfx (SoundId "fire") 0.8
    | EnteredLevel -> Audio.playMusic (TrackId "level1") true
    | Paused -> Audio.stopMusic
    | Muted -> Audio.setMasterVolume 0.0

[<Tests>]
let tests =
    testList "Feature 243 Audio request surface + record-only interpreter (US1, US2)" [

        // ---- US1: pure request surface ----

        test "update maps game events to the exact requested audio effects (US1)" {
            let requested = [ Fired; EnteredLevel; Paused ] |> List.map update
            Expect.equal
                requested
                [ PlaySfx(SoundId "fire", 0.8); PlayMusic(TrackId "level1", true); StopMusic ]
                "each event yields its requested effect, as a plain value"
        }

        test "smart constructors clamp carried volume into [0,1] (US1)" {
            Expect.equal (Audio.playSfx (SoundId "x") 2.0) (PlaySfx(SoundId "x", 1.0)) "over-max sfx volume clamps to 1.0"
            Expect.equal (Audio.playSfx (SoundId "x") -0.5) (PlaySfx(SoundId "x", 0.0)) "below-min sfx volume clamps to 0.0"
            Expect.equal (Audio.setMasterVolume 3.0) (SetMasterVolume 1.0) "over-max master level clamps to 1.0"
        }

        test "clampVolume is total, including nan (US1, Principle VI)" {
            Expect.equal (Audio.clampVolume 0.5) 0.5 "in-range passes through"
            Expect.equal (Audio.clampVolume 5.0) Audio.maxVolume "over-max -> maxVolume"
            Expect.equal (Audio.clampVolume -1.0) Audio.minVolume "under-min -> minVolume"
            Expect.equal (Audio.clampVolume nan) Audio.minVolume "nan -> minVolume, no exception"
        }

        testCase "clampVolume result is always within [minVolume, maxVolume] (FsCheck >=1000 cases)" <| fun () ->
            let prop (level: float) =
                let c = Audio.clampVolume level
                c >= Audio.minVolume && c <= Audio.maxVolume
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)

        // ---- US2: record-only interpreter ----

        test "interpret records requests in dispatch order (US2)" {
            let ev =
                Audio.interpret
                    [ Audio.playSfx (SoundId "fire") 0.8
                      Audio.playMusic (TrackId "level1") true
                      Audio.stopMusic ]
            Expect.equal
                ev.Requested
                [ PlaySfx(SoundId "fire", 0.8); PlayMusic(TrackId "level1", true); StopMusic ]
                "recorded oldest-first, faithfully"
        }

        test "interpret normalizes out-of-range volume in recorded evidence (US2)" {
            // A raw effect built WITHOUT the smart ctor still gets normalized at the boundary.
            let ev = Audio.interpret [ PlaySfx(SoundId "loud", 9.0); SetMasterVolume -4.0 ]
            Expect.equal
                ev.Requested
                [ PlaySfx(SoundId "loud", 1.0); SetMasterVolume 0.0 ]
                "boundary clamps carried volumes regardless of construction path"
        }

        test "record appends to evidence oldest-first (US2)" {
            let ev =
                Audio.emptyEvidence
                |> Audio.record (Audio.playSfx (SoundId "a") 0.3)
                |> Audio.record Audio.stopMusic
            Expect.equal ev.Requested [ PlaySfx(SoundId "a", 0.3); StopMusic ] "second record lands after the first"
        }

        test "emptyEvidence and interpret [] are both empty (US2)" {
            Expect.equal Audio.emptyEvidence.Requested [] "emptyEvidence has no requests"
            Expect.equal (Audio.interpret []).Requested [] "interpreting no effects records nothing"
        }

        test "StopMusic with nothing playing is a well-defined recorded no-op, not an error (US2)" {
            // The interpreter does not throw or dedupe; policy is the product's. It just records.
            let ev = Audio.interpret [ StopMusic; StopMusic ]
            Expect.equal ev.Requested [ StopMusic; StopMusic ] "stop-when-idle records without error"
        }

        // Random-input coverage using FsCheck's auto-generation of `float list` (matching the
        // simple prop style used by RngTests): every recorded volume is normalized and the batch
        // count is preserved, over arbitrary volumes including negatives / huge / nan.
        testCase "interpret preserves count and normalizes recorded volumes (FsCheck >=500 cases)" <| fun () ->
            let prop (vols: float list) =
                let effects = vols |> List.map (fun v -> PlaySfx(SoundId "s", v))
                let ev = Audio.interpret effects
                List.length ev.Requested = List.length effects
                && ev.Requested
                   |> List.forall (function
                       | PlaySfx(_, v) -> v >= Audio.minVolume && v <= Audio.maxVolume
                       | _ -> false)
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
    ]
