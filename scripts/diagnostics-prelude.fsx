#r "../src/Diagnostics/bin/Release/net10.0/FS.GG.UI.Diagnostics.dll"

open System
open FS.GG.UI.Diagnostics

let source =
    RuntimeDiagnostics.source
        (Some "FS.GG.UI.Diagnostics")
        "semantic-prelude"
        None
        (Some "feature169")

let context =
    RuntimeDiagnostics.context
        (Some "feature169-prelude")
        (Some(DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc)))
        None
        [ ("stream", "stdout") ]

let backend =
    RuntimeDiagnostics.create
        source
        (Some "DamageScopedDecision")
        (Some DiagnosticSeverity.Informational)
        (Some DiagnosticCategory.BackendCost)
        "Damage-scoped redraw used an offscreen fallback."
        (Some "No action required unless this appears in a performance-blocked lane.")
        context

let blocker =
    RuntimeDiagnostics.create
        source
        (Some "ReadinessBlocker")
        (Some DiagnosticSeverity.Error)
        (Some DiagnosticCategory.ReadinessBlocker)
        "Package feed proof did not restore the current local package."
        (Some "Refresh the local feed and rerun package validation.")
        context

let repeated = [ for _ in 1..100 -> backend ]
let summary = RuntimeDiagnostics.summarize (Some "feature169-prelude") [] [ "diagnostics-summary.json" ] (blocker :: repeated)
let console = RuntimeDiagnostics.renderConsole false 12 summary
let json = RuntimeDiagnostics.renderJson summary
let markdown = RuntimeDiagnostics.renderMarkdown summary

if summary.Status <> ReadinessDiagnosticStatus.Blocked then
    failwithf "expected blocked summary, got %s" (RuntimeDiagnostics.readinessStatusToken summary.Status)

if summary.Groups |> List.exists (fun group -> group.OccurrenceCount = 100) |> not then
    failwith "expected repeated backend-cost group"

if console.Length > 12 then
    failwithf "expected compact console output, got %d lines" console.Length

if not (json.Contains("\"schemaVersion\":\"runtime-diagnostics-v1\"")) then
    failwith "expected JSON schema token"

if not (markdown.Contains("blocked")) then
    failwith "expected Markdown status token"

printfn "diagnostics-prelude: status=%s groups=%d console-lines=%d"
    (RuntimeDiagnostics.readinessStatusToken summary.Status)
    summary.Groups.Length
    console.Length
