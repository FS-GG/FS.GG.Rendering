module Rendering.Harness.UnitTests

open System
open System.IO
open Expecto
open Rendering.Harness

let private baseFacts =
    { EffectiveBackend = X11
      Display = Some ":1"
      GlRenderer = Some "test"
      GlVersion = Some "4.6"
      GlDirect = true
      RefreshHz = Some 119.93
      Extensions = [ "XTEST" ]
      SwapControl = None
      VblankSource = None
      UinputAvailable = false }

let private withBackend b = { baseFacts with EffectiveBackend = b }
let private withPresent = { baseFacts with SwapControl = Some 1; VblankSource = Some "HDMI-A-1" }

[<Tests>]
let runPlanTests =
    testList "RunPlan (pure overclaim/degradation core)" [
        test "T0 and T1 always run (no live desktop needed)" {
            Expect.equal (RunPlan.plan T0 (withBackend NoDisplay)).Degradation Run "T0 runs headless"
            Expect.equal (RunPlan.plan T1 (withBackend NoDisplay)).Degradation Run "T1 runs offscreen"
        }
        test "T2 skips with no display" {
            match (RunPlan.plan T2 (withBackend NoDisplay)).Degradation with
            | Skip _ -> ()
            | other -> failtestf "expected Skip, got %A" other
        }
        test "T2 is fail-classified on Wayland" {
            match (RunPlan.plan T2 (withBackend Wayland)).Degradation with
            | FailClassified _ -> ()
            | other -> failtestf "expected FailClassified, got %A" other
        }
        test "T2 runs on X11" {
            Expect.equal (RunPlan.plan T2 (withBackend X11)).Degradation Run "T2 runs on X11"
        }
        test "vsync-faithful claimable ONLY with present facts (T3)" {
            let withFacts = RunPlan.plan T3 withPresent
            let without = RunPlan.plan T3 baseFacts
            Expect.isTrue withFacts.VsyncFaithfulAllowed "present facts → allowed"
            Expect.isFalse without.VsyncFaithfulAllowed "missing facts → not allowed"
            Expect.contains without.NotAuthoritativeFor "vsync-faithful" "missing facts → listed as NOT authoritative"
        }
        test "T-uinput skips cleanly when /dev/uinput absent" {
            match (RunPlan.plan TUinput baseFacts).Degradation with
            | Skip _ -> ()
            | other -> failtestf "expected Skip, got %A" other
        }
        test "every tier declares a non-empty notAuthoritativeFor (no overclaim)" {
            for t in [ T0; T1; T2; T3; TUinput ] do
                let p = RunPlan.plan t baseFacts
                Expect.isNonEmpty p.NotAuthoritativeFor (sprintf "%A must list what it does NOT prove" t)
        }
    ]

[<Tests>]
let tierTests =
    // T0/T1 are deterministic + headless (offscreen raster), so they run in the suite (M1 / Principle V).
    let tmp tag = Path.Combine(Path.GetTempPath(), sprintf "harness-%s-%s" tag (Guid.NewGuid().ToString("N")))
    testList "Tiers (deterministic, headless)" [
        test "T0 renders a deterministic, non-blank scene" {
            let ev, _ = Tiers.runOffscreen T0 baseFacts (tmp "t0")
            Expect.equal ev.Status Passed "T0 passes (non-blank + byte-identical re-render)"
            Expect.equal ev.ProofLevel Deterministic "T0 proof level"
        }
        test "T1 offscreen readback is non-blank" {
            let ev, _ = Tiers.runOffscreen T1 baseFacts (tmp "t1")
            Expect.equal ev.Status Passed "T1 passes (non-blank offscreen readback)"
            Expect.equal ev.ProofLevel OffscreenPixels "T1 proof level"
        }
    ]

[<Tests>]
let evidenceTests =
    testList "Evidence schema" [
        let sample =
            { Evidence.RunId = "r1"
              Evidence.Tier = T1
              Evidence.Subcommand = "offscreen"
              Evidence.Status = Passed
              Evidence.SkipReason = None
              Evidence.ProofLevel = OffscreenPixels
              Evidence.AuthoritativeFor = [ "renderer-pixels" ]
              Evidence.NotAuthoritativeFor = [ "desktop-visibility" ]
              Evidence.Facts = baseFacts
              Evidence.Frames = 0
              Evidence.P50Ms = None
              Evidence.P95Ms = None
              Evidence.P99Ms = None
              Evidence.Artifacts = [ "summary.md" ] }
        test "run.json carries proofLevel and notAuthoritativeFor" {
            let json = Evidence.toJson sample
            Expect.stringContains json "\"proofLevel\": \"offscreen-pixels\"" "proof level present"
            Expect.stringContains json "\"notAuthoritativeFor\": [\"desktop-visibility\"]" "not-authoritative present"
        }
        test "summary restates what the run does NOT prove" {
            Expect.stringContains (Evidence.toSummary sample) "NOT" "summary states non-authoritative scope"
        }
        test "percentiles: empty → None, non-empty → values" {
            Expect.equal (Evidence.percentiles []) (None, None, None) "empty"
            let (p50, _, p99) = Evidence.percentiles [ 1.0; 2.0; 3.0; 4.0 ]
            Expect.isSome p50 "p50 computed"
            Expect.isSome p99 "p99 computed"
        }
    ]
