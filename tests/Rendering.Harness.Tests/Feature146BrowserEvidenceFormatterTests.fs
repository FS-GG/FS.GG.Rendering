module Feature146BrowserEvidenceFormatterTests

open Expecto
open Rendering.Harness

[<Tests>]
let feature146BrowserEvidenceFormatterTests =
    testList "Feature146 browser evidence formatters" [
        test "browser report formatter states no comparison was performed" {
            let report =
                RenderAnywhere.buildBrowserCapabilityReport (RenderAnywhere.corpus ()) [] "canvaskit-command-stream/proof"

            let text = RenderAnywhere.formatBrowserReport report |> String.concat "\n"

            Expect.stringContains text "candidate-backend: canvaskit-command-stream/proof" "candidate backend is formatted"
            Expect.stringContains text "comparison: not performed" "report does not present itself as a diff"
            Expect.stringContains text "decision: fallback:" "fallback decision is formatted"
            Expect.stringContains text "basic-primitives: missing-reference" "per-scene status is formatted"
            Expect.stringContains text "NOT cross-backend fidelity evidence" "report refuses to be read as fidelity evidence"
        }

        test "browser report never emits a tolerance, diff, or candidate-identity field" {
            let report =
                RenderAnywhere.buildBrowserCapabilityReport (RenderAnywhere.corpus ()) [] "canvaskit-command-stream/proof"

            // Match the emitted `field: value` form, so prose that merely names a diff does not trip this.
            let emitsField (name: string) =
                RenderAnywhere.formatBrowserReport report
                |> List.exists (fun line -> line.Trim().TrimStart('-').Trim().StartsWith(name + ":", System.StringComparison.Ordinal))

            Expect.isFalse (emitsField "tolerance") "a report that compares nothing must not print a tolerance"
            Expect.isFalse (emitsField "diff") "a report that compares nothing must not print a diff metric"
            Expect.isFalse (emitsField "candidate") "a report whose candidate never ran must not print a candidate identity"
        }

        test "browser report writer persists markdown" {
            let out = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fs-gg-feature146-browser-test")
            if System.IO.Directory.Exists out then System.IO.Directory.Delete(out, true)

            let report =
                RenderAnywhere.runBrowserCapabilityCommand (System.IO.Path.Combine(out, "absent-reference")) out

            let path = System.IO.Path.Combine(out, "browser-feasibility.md")

            Expect.isTrue (System.IO.File.Exists path) "browser feasibility markdown is written"
            Expect.hasLength report.Scenarios 3 "report covers corpus"
        }
    ]
