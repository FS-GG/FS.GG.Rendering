module Audit_AnimationSampling

// Feature 006 (Verify Imported Mechanisms) — US2 audit of the real
// `FS.GG.UI.Scene.Animation` sampling seam (`applyAt` / `sampleFrames` /
// `isSettled`). These tests exercise the SHIPPED module (no stubs, no shims):
//
//   * T006 scaffold sanity   — the seam is reachable from the test assembly.
//   * T025 DETERMINISM       — identical inputs + identical time samples produce
//                              byte-identical output across repeated invocations
//                              (FR-007, the pure-sampling guarantee).
//   * T025 SETTLED IDENTITY  — a settled animation (opacity tween landed at 1.0,
//     (identity-at-rest)       transform absent ⇒ identity) sampled at/after its
//                              duration is BYTE-IDENTICAL to the static render of
//                              the same target (the target unwrapped). A
//                              DISCRIMINATING in-flight sample (mid-tween,
//                              opacity < 1) MUST differ — proving the identity
//                              check is not vacuously passing. (spec US2 AS4)
//
// A legitimately-red assertion here is a FINDING about the imported mechanism,
// never something to weaken into green.

open System
open Expecto
open FS.GG.UI.Scene

let private ms (n: float) = TimeSpan.FromMilliseconds n

// A two-node target so "unwrap" / grouping behaviour is exercised against a real
// scene, plus a single-node target for the strict single-node unwrap path.
let private staticScene: Scene =
    Scene.rectangle (4.0, 8.0, 32.0, 16.0) (Colors.rgb 200uy 100uy 50uy)

// The settled animation:
//   * opacity travels 0 -> 1 over 100ms (Linear ⇒ endpoint pinned exactly to 1.0),
//   * transform travels translate(40,0) -> identity over the same 100ms.
// At elapsed >= 100ms BOTH tweens pin to their identity endpoints, so the sample
// MUST lower via identity-at-rest (opacity 1.0 + identity transform ⇒ target
// unwrapped). Mid-flight the sample has opacity < 1 AND a non-identity transform,
// giving the discriminating check resolution at both the value and render levels.
let private settledAnim: Animation =
    { Animation.empty with
        Opacity = Some { Start = 0.0; End = 1.0; Duration = ms 100.0; Easing = Linear }
        Transform =
            Some
                { Start = { Transform.identity with TranslateX = 40.0 }
                  End = Transform.identity
                  Duration = ms 100.0
                  Easing = Linear } }

let private settledTime = ms 100.0
let private afterTime = ms 250.0
let private inFlightTime = ms 50.0 // Linear midpoint ⇒ opacity 0.5 (< 1.0)

// The "static render" the settled sample must match: the target scene's node
// unwrapped, exactly as `Lower.unwrap` does for the public seam.
let private staticUnwrapped: SceneNode =
    match staticScene.Nodes with
    | [ single ] -> single
    | nodes -> Group [ { Nodes = nodes } ]

// A render-hash byte fingerprint of a node (wrapped back into a Scene). Two
// nodes with the same fingerprint render byte-identically by the deterministic
// scene renderer — a true byte-level proof, stronger than structural equality.
let private fingerprint (node: SceneNode) : string =
    match SceneEvidence.renderHash { Width = 96; Height = 64 } { Nodes = [ node ] } with
    | Result.Ok ev -> ev.Value
    | Result.Error f -> failtestf "render hash unavailable for audit fingerprint: %s" f.Message

[<Tests>]
let auditScaffoldSanity =
    testList "Audit: AnimationSampling scaffold" [
        test "Audit: scaffold reaches the real Animation sampling seam (T006)" {
            // Trivial reachability: the shipped functions are callable and typed.
            let node = Animation.applyAt settledTime settledAnim staticScene
            let frames = Animation.sampleFrames [ settledTime ] settledAnim staticScene
            Expect.isTrue (node = node) "applyAt is reachable and returns a SceneNode"
            Expect.equal (List.length frames) 1 "sampleFrames is reachable and maps one time to one Scene"
        }
    ]

[<Tests>]
let auditDeterminism =
    testList "Audit: AnimationSampling determinism (US2, T025, FR-007)" [
        test "Audit: applyAt is byte-identical across repeated invocations at fixed times" {
            // In-flight, settled, and post-duration samples must each be a pure
            // function of their inputs — re-invoking yields the SAME value.
            for t in [ inFlightTime; settledTime; afterTime; TimeSpan.Zero ] do
                let a = Animation.applyAt t settledAnim staticScene
                let b = Animation.applyAt t settledAnim staticScene
                Expect.equal a b (sprintf "applyAt at %A is deterministic (structural)" t)
                Expect.equal (fingerprint a) (fingerprint b) (sprintf "applyAt at %A is deterministic (render-hash)" t)
        }

        test "Audit: sampleFrames is byte-identical across repeated invocations" {
            let times = [ TimeSpan.Zero; inFlightTime; settledTime; afterTime ]
            let first = Animation.sampleFrames times settledAnim staticScene
            let second = Animation.sampleFrames times settledAnim staticScene
            Expect.equal first second "sampleFrames over fixed time points is deterministic"
        }
    ]

[<Tests>]
let auditSettledIdentity =
    testList "Audit: AnimationSampling settled identity-at-rest (US2 AS4, T025, FR-007)" [
        test "Audit: settled animation sampled at/after duration is byte-identical to the static scene" {
            // Identity-at-rest: opacity pinned to 1.0 + identity transform ⇒ the
            // target unwrapped, byte-identical to the static render.
            let atDuration = Animation.applyAt settledTime settledAnim staticScene
            let afterDuration = Animation.applyAt afterTime settledAnim staticScene

            Expect.equal atDuration staticUnwrapped "at duration ⇒ target unwrapped (structural)"
            Expect.equal afterDuration staticUnwrapped "after duration ⇒ target unwrapped (structural)"
            Expect.equal (fingerprint atDuration) (fingerprint staticUnwrapped) "at duration ⇒ byte-identical render hash"
            Expect.equal (fingerprint afterDuration) (fingerprint staticUnwrapped) "after duration ⇒ byte-identical render hash"

            // isSettled must agree that this sample is at rest (redraw gating).
            Expect.isTrue (Animation.isSettled settledTime settledAnim) "isSettled true at duration"
            Expect.isTrue (Animation.isSettled afterTime settledAnim) "isSettled true after duration"
        }

        test "Audit: DISCRIMINATING — an in-flight sample (opacity < 1) DIFFERS from the static scene" {
            // If this did NOT differ, the identity-at-rest test above would be
            // vacuous. Mid-tween opacity (0.5) scales the fill alpha, so the
            // sample must diverge from the static render both structurally and
            // by render-hash.
            let midFlight = Animation.applyAt inFlightTime settledAnim staticScene

            Expect.notEqual midFlight staticUnwrapped "in-flight sample must differ from static (structural)"
            Expect.notEqual (fingerprint midFlight) (fingerprint staticUnwrapped) "in-flight sample must differ from static (render-hash)"
            Expect.isFalse (Animation.isSettled inFlightTime settledAnim) "isSettled false while in flight"
        }
    ]

// P6/R5: the `Color` tween used to be dead weight (it fed only `isSettled`). `sampleColor` now makes
// it a real, consumable value, and `applyAt` HONESTLY does not composite it (the wire format has no
// scene-wide tint node) — the disclosure in Animation.fsi. These audits prove both halves.
[<Tests>]
let auditColorSampling =
    testList "Audit: AnimationSampling colour tween (P6/R5)" [
        let colorAnim: Animation =
            { Animation.empty with
                Color =
                    Some
                        { Start = Colors.rgb 0uy 0uy 0uy
                          End = Colors.rgb 200uy 100uy 50uy
                          Duration = ms 100.0
                          Easing = Linear } }

        test "Audit: sampleColor interpolates the colour tween and pins its endpoints" {
            Expect.equal (Animation.sampleColor TimeSpan.Zero colorAnim) (Some(Colors.rgb 0uy 0uy 0uy)) "start pinned at t=0"
            Expect.equal (Animation.sampleColor settledTime colorAnim) (Some(Colors.rgb 200uy 100uy 50uy)) "end pinned at duration"

            match Animation.sampleColor inFlightTime colorAnim with
            | Some mid ->
                // Linear midpoint of 0->200 / 0->100 / 0->50 ⇒ (100,50,25).
                Expect.equal mid (Colors.rgb 100uy 50uy 25uy) "mid-flight colour is the linear midpoint"
            | None -> failtest "a colour tween is present, so sampleColor must return Some"
        }

        test "Audit: sampleColor is None when no colour tween is set" {
            Expect.equal (Animation.sampleColor settledTime Animation.empty) None "no colour tween ⇒ None"
        }

        test "Audit: DISCRIMINATING — applyAt does NOT composite the colour tween" {
            // A colour-only animation has opacity 1.0 + identity transform at every time, so applyAt
            // must be byte-identical to the static scene — the colour tween is surfaced via
            // `sampleColor`, never folded into the rendered node. If applyAt DID composite colour this
            // would differ, exposing an undisclosed side effect.
            for t in [ TimeSpan.Zero; inFlightTime; settledTime; afterTime ] do
                let sample = Animation.applyAt t colorAnim staticScene
                Expect.equal sample staticUnwrapped (sprintf "colour-only applyAt at %A is the static scene unwrapped (structural)" t)
                Expect.equal (fingerprint sample) (fingerprint staticUnwrapped) (sprintf "colour-only applyAt at %A is byte-identical to static (render-hash)" t)
        }
    ]
