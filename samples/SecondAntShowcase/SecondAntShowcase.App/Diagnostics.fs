module SecondAntShowcase.App.Diagnostics

open System
open System.IO
open FS.GG.UI.Diagnostics

let private flag name args =
    let rec loop items =
        match items with
        | key :: value :: _ when key = name -> Some value
        | _ :: rest -> loop rest
        | [] -> None

    loop args

let private hasFlag name args =
    args |> List.exists ((=) name)

let private source subsystem =
    RuntimeDiagnostics.source
        (Some "SecondAntShowcase")
        subsystem
        None
        (Some "second-ant-showcase")

let private context stream =
    RuntimeDiagnostics.context
        (Some "antshowcase-diagnostics")
        (Some(DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc)))
        None
        [ "stream", stream ]

let diagnostics () =
    [ RuntimeDiagnostics.create
          (source "sample-cli")
          (Some "HeadlessEnvironment")
          (Some DiagnosticSeverity.Warning)
          (Some DiagnosticCategory.Environment)
          "The diagnostics command can run without opening a live viewer window."
          (Some "Use live visual-readiness commands when screenshot proof is required.")
          (context "stdout")
      RuntimeDiagnostics.create
          (source "opengl-host")
          (Some "DamageScopedDecision")
          (Some DiagnosticSeverity.Informational)
          (Some DiagnosticCategory.BackendCost)
          "Damage-scoped redraw can use an offscreen fallback for deterministic evidence."
          (Some "No action required unless a performance lane marks this scenario blocked.")
          (context "runtime") ]

let buildSummary outDir =
    RuntimeDiagnostics.writeArtifacts
        outDir
        (Some "antshowcase-diagnostics")
        []
        (diagnostics ())

let render verbose outDir =
    let summary = buildSummary outDir
    RuntimeDiagnostics.renderConsole verbose 12 summary

let run args =
    let outDir =
        flag "--out" args
        |> Option.defaultValue (Path.Combine("artifacts", "second-ant-showcase", "diagnostics"))

    let verbose = hasFlag "--verbose" args
    let json = hasFlag "--json" args
    let summary = buildSummary outDir

    if json then
        printfn "%s" (RuntimeDiagnostics.renderJson summary)
    else
        RuntimeDiagnostics.renderConsole verbose 12 summary
        |> List.iter (printfn "%s")

    match summary.Status with
    | ReadinessDiagnosticStatus.Accepted
    | ReadinessDiagnosticStatus.EnvironmentLimitedStatus -> 0
    | ReadinessDiagnosticStatus.Blocked
    | ReadinessDiagnosticStatus.ReviewRequired -> 1
