module Feature175TraceReadbackTests

// Feature 175 S3 — the structured live-trace READ-BACK path. Diagnosing the focus lag once meant
// adding env-gated eprintfn traces and REPACKING to observe. `RenderLagTrace` now also captures
// emitted events in-memory, so a test or tool can observe live state programmatically — no env var,
// no repack. The capture buffer is process-global, so these tests are SEQUENCED (so they do not race
// each other on the global toggle) and assert on the PRESENCE of uniquely-identified events (other
// test files emit through the same trace concurrently).

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testSequenced
    <| testList "Feature175TraceReadback" [
        test "the read-back captures a real framework emit (the F1 runtime-state-repaint policy)" {
            Viewer.traceStartCapture ()
            // A no-message input re-derive: `Viewer.runtimeStateRepaint` emits "runtime-state-repaint".
            Viewer.runtimeStateRepaint false "stale" (fun () -> "fresh") |> ignore
            let events = Viewer.traceDrainCapture ()

            Expect.exists
                events
                (fun (event, _) -> event = "runtime-state-repaint")
                "the in-memory read-back captured the repaint the policy emitted (observable without a repack)"
        }

        test "the read-back preserves the event name and its key/value fields" {
            // A uniquely-named event no other code path emits → deterministic despite concurrent noise.
            Viewer.traceStartCapture ()
            Viewer.traceEmit "s3-readback-probe" [ "cause", "focus"; "id", "nav-2" ]
            let events = Viewer.traceDrainCapture ()

            match events |> List.tryFind (fun (event, _) -> event = "s3-readback-probe") with
            | Some(_, fields) -> Expect.equal fields [ "cause", "focus"; "id", "nav-2" ] "fields round-trip through the read-back"
            | None -> failtest "the uniquely-named probe event was not captured by the read-back"
        }
    ]
