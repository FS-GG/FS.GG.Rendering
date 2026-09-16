module AnimationTests

// Feature 073 (US1 + US2): pure animation core tests — easing endpoint pinning +
// per-curve monotonicity (FsCheck), tween/transform clamping and bounds, and the
// AnimationState retargeting state machine. All real pure computation (no [S]).

open System
open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene

let private allEasings = [ Linear; EaseIn; EaseOut; EaseInOut ]

let private ms (n: float) = TimeSpan.FromMilliseconds n

[<Tests>]
let easingTests =
    testList
        "Easing (SC-002, FR-003)"
        [
            test "every curve pins both endpoints" {
                for e in allEasings do
                    Expect.equal (Easing.apply e 0.0) 0.0 (sprintf "%A pins f(0) = 0" e)
                    Expect.equal (Easing.apply e 1.0) 1.0 (sprintf "%A pins f(1) = 1" e)
            }

            test "input is clamped to [0,1] (out-of-domain yields the endpoint)" {
                for e in allEasings do
                    Expect.equal (Easing.apply e -0.5) 0.0 (sprintf "%A clamps t < 0 to f(0)" e)
                    Expect.equal (Easing.apply e 1.5) 1.0 (sprintf "%A clamps t > 1 to f(1)" e)
                    Expect.equal (Easing.apply e -10.0) 0.0 (sprintf "%A clamps far-negative t to f(0)" e)
                    Expect.equal (Easing.apply e 42.0) 1.0 (sprintf "%A clamps far-positive t to f(1)" e)
            }

            test "EaseInOut is symmetric about its midpoint" {
                Expect.floatClose Accuracy.high (Easing.apply EaseInOut 0.5) 0.5 "EaseInOut(0.5) = 0.5"
            }

            test "Easing.Default is the documented FR-003 default (EaseInOut)" {
                Expect.equal Easing.Default EaseInOut "the documented default curve is EaseInOut"
            }

            testCase "every curve is monotonically non-decreasing over [0,1] (FsCheck >=1000 cases)"
            <| fun () ->
                let monotone (e: Easing) =
                    // Two samples in [0,1] from integer draws; the lower input must
                    // not produce a greater eased output (allow a tiny float slack).
                    let prop (a: int) (b: int) =
                        let lo = float (min (abs a % 1001) (abs b % 1001)) / 1000.0
                        let hi = float (max (abs a % 1001) (abs b % 1001)) / 1000.0
                        Easing.apply e lo <= Easing.apply e hi + 1e-9

                    Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)

                allEasings |> List.iter monotone
        ]

[<Tests>]
let tweenTests =
    testList
        "Tween + interpolation (SC-002, SC-007)"
        [
            test "progress clamps elapsed to [0, Duration]" {
                let tween =
                    {
                        Start = 0.0
                        End = 10.0
                        Duration = ms 100.0
                        Easing = Linear
                    }

                Expect.equal (Tween.progress (ms -50.0) tween) 0.0 "before start clamps to 0 progress"
                Expect.equal (Tween.progress (ms 100.0) tween) 1.0 "at duration is full progress"
                Expect.equal (Tween.progress (ms 250.0) tween) 1.0 "after end clamps to full progress"
                Expect.floatClose Accuracy.high (Tween.progress (ms 50.0) tween) 0.5 "linear half-way is 0.5"
            }

            test "non-positive duration resolves immediately to End (no divide-by-zero)" {
                let zero =
                    {
                        Start = 3.0
                        End = 9.0
                        Duration = TimeSpan.Zero
                        Easing = EaseInOut
                    }

                let neg =
                    {
                        Start = 3.0
                        End = 9.0
                        Duration = ms -10.0
                        Easing = EaseInOut
                    }

                Expect.equal (Tween.progress TimeSpan.Zero zero) 1.0 "zero duration ⇒ progress 1.0"
                Expect.equal (Tween.sample Animation.lerpFloat TimeSpan.Zero zero) 9.0 "zero duration ⇒ End value"
                Expect.equal (Tween.progress (ms 5.0) neg) 1.0 "negative duration ⇒ progress 1.0"
                Expect.equal (Tween.sample Animation.lerpFloat (ms 5.0) neg) 9.0 "negative duration ⇒ End value"
            }

            test "sample pins the exact Start and End values at the bounds" {
                let tween =
                    {
                        Start = 24.0
                        End = 0.0
                        Duration = ms 300.0
                        Easing = EaseOut
                    }

                Expect.equal (Tween.sample Animation.lerpFloat TimeSpan.Zero tween) 24.0 "t=0 ⇒ exactly Start"
                Expect.equal (Tween.sample Animation.lerpFloat (ms 300.0) tween) 0.0 "t=Duration ⇒ exactly End"
            }

            test "lerpFloat stays within [a,b] across [0,1]" {
                for i in 0..10 do
                    let t = float i / 10.0
                    let v = Animation.lerpFloat 4.0 20.0 t
                    Expect.isTrue (v >= 4.0 && v <= 20.0) (sprintf "lerpFloat in bounds at t=%f" t)

                Expect.equal (Animation.lerpFloat 4.0 20.0 0.0) 4.0 "t=0 ⇒ a"
                Expect.equal (Animation.lerpFloat 4.0 20.0 1.0) 20.0 "t=1 ⇒ b"
            }

            test "lerpColor interpolates each RGBA byte within bounds" {
                let a =
                    {
                        Red = 0uy
                        Green = 0uy
                        Blue = 0uy
                        Alpha = 0uy
                    }

                let b =
                    {
                        Red = 200uy
                        Green = 100uy
                        Blue = 50uy
                        Alpha = 255uy
                    }

                let mid = Animation.lerpColor a b 0.5
                Expect.equal mid.Red 100uy "red half-way (rounded)"
                Expect.equal mid.Green 50uy "green half-way"
                Expect.equal mid.Blue 25uy "blue half-way"
                Expect.equal (Animation.lerpColor a b 0.0) a "t=0 ⇒ a"
                Expect.equal (Animation.lerpColor a b 1.0) b "t=1 ⇒ b"
            }

            test "Transform.identity / isIdentity / lerp per-field" {
                Expect.isTrue (Transform.isIdentity Transform.identity) "identity is identity"

                let moved =
                    { Transform.identity with
                        TranslateY = 24.0
                    }

                Expect.isFalse (Transform.isIdentity moved) "a translated transform is not identity"
                let mid = Transform.lerp moved Transform.identity 0.5
                Expect.floatClose Accuracy.high mid.TranslateY 12.0 "translateY lerps half-way"
                Expect.equal (Transform.lerp moved Transform.identity 1.0) Transform.identity "t=1 ⇒ identity"

                Expect.isTrue
                    (Transform.isIdentity (Transform.lerp moved Transform.identity 1.0))
                    "settled lerp is identity"
            }

            test "Transform.toPerspectiveTransform maps identity to the identity matrix" {
                let m = Transform.toPerspectiveTransform Transform.identity
                Expect.floatClose Accuracy.high m.M11 1.0 "M11 = 1"
                Expect.floatClose Accuracy.high m.M22 1.0 "M22 = 1"
                Expect.floatClose Accuracy.high m.M12 0.0 "M12 = 0"
                Expect.floatClose Accuracy.high m.M21 0.0 "M21 = 0"
                Expect.equal m.M13 0.0 "M13 = translateX = 0"
                Expect.equal m.M23 0.0 "M23 = translateY = 0"
                Expect.equal m.M33 1.0 "M33 = 1"
            }

            test "toPerspectiveTransform carries translate into M13/M23" {
                let m =
                    Transform.toPerspectiveTransform
                        { Transform.identity with
                            TranslateX = 12.0
                            TranslateY = -7.0
                        }

                Expect.equal m.M13 12.0 "M13 = translateX"
                Expect.equal m.M23 -7.0 "M23 = translateY"
            }
        ]

[<Tests>]
let animationStateTests =
    testList
        "AnimationState retargeting (SC-006, SC-007)"
        [
            test "create sets Current = Start = Target = initial, Elapsed = 0" {
                let s = AnimationState.create Animation.lerpFloat 5.0 (ms 200.0) EaseInOut
                Expect.equal s.Current 5.0 "Current = initial"
                Expect.equal s.Start 5.0 "Start = initial"
                Expect.equal s.Target 5.0 "Target = initial"
                Expect.equal s.Elapsed TimeSpan.Zero "Elapsed = 0"
                Expect.isFalse (AnimationState.isActive s) "a freshly created state is not active (Current = Target)"
            }

            test "advance adds the delta capped at Duration and eases Current toward Target" {
                let s =
                    AnimationState.create Animation.lerpFloat 0.0 (ms 200.0) Linear
                    |> AnimationState.retarget 100.0

                let half = AnimationState.advance (ms 100.0) s
                Expect.equal half.Elapsed (ms 100.0) "elapsed accrues"
                Expect.floatClose Accuracy.high (AnimationState.value half) 50.0 "linear half-way is 50"
                Expect.isTrue (AnimationState.isActive half) "still active mid-flight"

                let over = AnimationState.advance (ms 500.0) half
                Expect.equal over.Elapsed (ms 200.0) "elapsed capped at Duration"
                Expect.equal (AnimationState.value over) 100.0 "settled ⇒ exactly Target"
                Expect.isFalse (AnimationState.isActive over) "settled ⇒ not active"
            }

            test "retarget continues from the displayed value — no snap back to the original start" {
                let s =
                    AnimationState.create Animation.lerpFloat 0.0 (ms 200.0) Linear
                    |> AnimationState.retarget 100.0
                    |> AnimationState.advance (ms 100.0) // Current ≈ 50

                let displayed = AnimationState.value s
                let retargeted = AnimationState.retarget 0.0 s
                Expect.equal retargeted.Start displayed "retarget sets Start = displayed Current"
                Expect.equal retargeted.Current displayed "Current does not jump on retarget"
                Expect.equal retargeted.Target 0.0 "Target is the new target"
                Expect.equal retargeted.Elapsed TimeSpan.Zero "Elapsed resets to 0"

                // A second mid-flight retarget still continues from the displayed value.
                let mid = AnimationState.advance (ms 50.0) retargeted
                let secondDisplayed = AnimationState.value mid
                let again = AnimationState.retarget 75.0 mid
                Expect.equal again.Start secondDisplayed "second retarget also continues from the displayed value"
            }

            test "isActive is false once settled" {
                let s =
                    AnimationState.create Animation.lerpFloat 0.0 (ms 100.0) EaseInOut
                    |> AnimationState.retarget 10.0
                    |> AnimationState.advance (ms 100.0)

                Expect.isFalse (AnimationState.isActive s) "fully advanced ⇒ inactive"
            }
        ]

let private scalar time value easing =
    {
        Time = ms time
        Value = ScalarValue value
        EasingToNext = easing
    }

let private clip loop =
    {
        Id = "arena-intro"
        Duration = ms 100.0
        Tracks =
            [
                PositionX, [ scalar 0.0 0.0 Linear; scalar 100.0 20.0 Linear ]
                PositionY, [ scalar 0.0 10.0 Linear; scalar 100.0 30.0 Linear ]
                RotationDegrees, [ scalar 0.0 0.0 Linear; scalar 100.0 90.0 Linear ]
                ScaleX, [ scalar 0.0 1.0 Linear; scalar 100.0 2.0 Linear ]
                ScaleY, [ scalar 0.0 1.0 Linear; scalar 100.0 3.0 Linear ]
                Opacity, [ scalar 0.0 0.0 Linear; scalar 100.0 1.0 Linear ]
                CustomScalar "glow", [ scalar 0.0 2.0 Linear; scalar 100.0 4.0 Linear ]
                Color,
                [
                    {
                        Time = TimeSpan.Zero
                        Value =
                            ColorValue
                                {
                                    Red = 0uy
                                    Green = 0uy
                                    Blue = 0uy
                                    Alpha = 255uy
                                }
                        EasingToNext = Linear
                    }
                    {
                        Time = ms 100.0
                        Value =
                            ColorValue
                                {
                                    Red = 200uy
                                    Green = 100uy
                                    Blue = 50uy
                                    Alpha = 255uy
                                }
                        EasingToNext = Linear
                    }
                ]
                PathMorph "body",
                [
                    {
                        Time = TimeSpan.Zero
                        Value = PathValue [ 0.0, 0.0; 10.0, 0.0 ]
                        EasingToNext = Linear
                    }
                    {
                        Time = ms 100.0
                        Value = PathValue [ 0.0, 10.0; 20.0, 10.0 ]
                        EasingToNext = Linear
                    }
                ]
            ]
        Cues =
            [
                {
                    Id = "start"
                    Time = TimeSpan.Zero
                    Payload = "ui.start"
                }
                {
                    Id = "impact"
                    Time = ms 50.0
                    Payload = "sfx.impact"
                }
            ]
        Loop = loop
    }

[<Tests>]
let animationClipTests =
    testList
        "portable animation clips"
        [
            test "validated scalar, color and compatible path tracks sample deterministically" {
                let accepted =
                    clip Once
                    |> AnimationClip.validate
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let a = AnimationClip.sample (ms 50.0) Seek accepted
                let b = AnimationClip.sample (ms 50.0) Seek accepted
                Expect.equal a b "the same explicit sample is structural-equal"
                Expect.equal a.Values[PositionX] (ScalarValue 10.0) "position samples halfway"
                Expect.equal a.Values[PositionY] (ScalarValue 20.0) "vertical position samples halfway"
                Expect.equal a.Values[RotationDegrees] (ScalarValue 45.0) "rotation samples halfway"
                Expect.equal a.Values[ScaleX] (ScalarValue 1.5) "horizontal scale samples halfway"
                Expect.equal a.Values[ScaleY] (ScalarValue 2.0) "vertical scale samples halfway"
                Expect.equal a.Values[Opacity] (ScalarValue 0.5) "opacity samples halfway"
                Expect.equal a.Values[CustomScalar "glow"] (ScalarValue 3.0) "custom scalar samples halfway"

                Expect.equal
                    a.Values[Color]
                    (ColorValue
                        {
                            Red = 100uy
                            Green = 50uy
                            Blue = 25uy
                            Alpha = 255uy
                        })
                    "color samples halfway"

                Expect.equal
                    a.Values[PathMorph "body"]
                    (PathValue [ 0.0, 5.0; 15.0, 5.0 ])
                    "matching path points interpolate"

                Expect.isEmpty a.Cues "seek never emits historical cues"
            }

            test "live cues use the half-open interval and stable loop occurrence" {
                let accepted =
                    clip (Repeat 2)
                    |> AnimationClip.validate
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let initial = AnimationClip.sample TimeSpan.Zero (LiveAdvance(ms -1.0)) accepted

                Expect.equal
                    (initial.Cues |> List.map (fun occurrence -> occurrence.Cue.Id))
                    [ "start" ]
                    "a caller can cross the initial boundary once"

                let first = AnimationClip.sample (ms 50.0) (LiveAdvance TimeSpan.Zero) accepted

                Expect.equal
                    (first.Cues
                     |> List.map (fun occurrence -> occurrence.Cue.Id, occurrence.Iteration))
                    [ "impact", 0 ]
                    "zero-time cue is outside (0,50]"

                let wrapped = AnimationClip.sample (ms 150.0) (LiveAdvance(ms 50.0)) accepted

                Expect.equal
                    (wrapped.Cues
                     |> List.map (fun occurrence -> occurrence.Cue.Id, occurrence.Iteration))
                    [ "start", 1; "impact", 1 ]
                    "wrap emits each crossed occurrence once"

                Expect.equal wrapped.Iteration 1 "second repeat iteration"
                Expect.equal wrapped.LocalTime (ms 50.0) "repeat local time wraps"
            }

            test "ping-pong reverses samples and cue time without changing track data" {
                let accepted =
                    { clip (PingPong 2) with
                        Cues =
                            [
                                {
                                    Id = "start"
                                    Time = TimeSpan.Zero
                                    Payload = "ui.start"
                                }
                                {
                                    Id = "impact"
                                    Time = ms 50.0
                                    Payload = "sfx.impact"
                                }
                                {
                                    Id = "turn"
                                    Time = ms 100.0
                                    Payload = "ui.turn"
                                }
                            ]
                    }
                    |> AnimationClip.validate
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let turn = AnimationClip.sample (ms 100.0) (LiveAdvance(ms 50.0)) accepted

                Expect.equal
                    (turn.Cues |> List.map (fun occurrence -> occurrence.Cue.Id))
                    [ "turn" ]
                    "a turnaround endpoint emits once"

                let reverse = AnimationClip.sample (ms 150.0) (LiveAdvance(ms 125.0)) accepted
                Expect.equal reverse.Direction Reverse "second pass reverses"
                Expect.equal reverse.LocalTime (ms 50.0) "reverse pass samples from the far endpoint"
                Expect.equal reverse.Values[PositionX] (ScalarValue 10.0) "track samples at reversed local time"

                Expect.equal
                    (reverse.Cues |> List.map (fun occurrence -> occurrence.Cue.Id))
                    [ "impact" ]
                    "reverse cue is emitted when crossed"

                let complete = AnimationClip.sample (ms 250.0) Paused accepted
                Expect.isTrue complete.IsComplete "elapsed is capped at the bounded iteration count"
                Expect.equal complete.LocalTime TimeSpan.Zero "an even ping-pong settles at its start"
                Expect.isEmpty complete.Cues "pause emits no cues"
            }

            test "validation rejects type, time, topology, identity and loop defects together" {
                let invalid =
                    { clip (Repeat 0) with
                        Id = " "
                        Tracks =
                            [
                                Opacity, [ scalar 10.0 0.0 Linear; scalar 90.0 Double.NaN Linear ]
                                PathMorph "body",
                                [
                                    {
                                        Time = TimeSpan.Zero
                                        Value = PathValue [ 0.0, 0.0; 1.0, 1.0 ]
                                        EasingToNext = Linear
                                    }
                                    {
                                        Time = ms 100.0
                                        Value = PathValue [ 0.0, 0.0; 1.0, 1.0; 2.0, 2.0 ]
                                        EasingToNext = Linear
                                    }
                                ]
                                Color, [ scalar 0.0 0.0 Linear; scalar 100.0 1.0 Linear ]
                            ]
                        Cues =
                            [
                                {
                                    Id = "same"
                                    Time = ms 80.0
                                    Payload = "a"
                                }
                                {
                                    Id = "same"
                                    Time = ms 20.0
                                    Payload = "b"
                                }
                            ]
                    }

                match AnimationClip.validate invalid with
                | Ok _ -> failtest "invalid clip was accepted"
                | Error issues ->
                    Expect.contains issues EmptyClipId "empty clip identity"
                    Expect.contains issues (InvalidLoopIterations 0) "unbounded/empty loop"
                    Expect.contains issues (InvalidKeyframeTime(Opacity, 0)) "track must start at zero"
                    Expect.contains issues (InvalidKeyframeValue(Opacity, 1)) "nonfinite scalar"
                    Expect.contains issues (MismatchedKeyframeValue(Color, 0)) "track type mismatch"
                    Expect.contains issues (IncompatiblePathTopology(PathMorph "body")) "path topology mismatch"
                    Expect.contains issues (DuplicateCueId "same") "cue identities are unique"
                    Expect.contains issues UnorderedCues "cues are ordered"
            }
        ]
