module Feature151ReadinessFixtures

open System
open System.IO
open FS.GG.UI.Testing

let requiredFiles =
    [ "validation-summary.md"
      "corpus-validation.md"
      "scrollviewer-validation.md"
      "reuse-validation.md"
      "full-incremental-parity.md"
      "regression-evidence.md"
      "compatibility-ledger.md"
      "package-validation.md"
      "limitations.md" ]

let evidence name =
    { Name = name
      Path = Some($"specs/151-complete-p8-layout/readiness/{name}")
      Status = LayoutReadinessAccepted
      Required = true
      Diagnostics = [] }

let acceptedReport =
    { Feature = "151-complete-p8-layout"
      ContractStatus = LayoutReadinessAccepted
      ScrollViewerStatus = LayoutReadinessAccepted
      IntrinsicStatus = LayoutReadinessAccepted
      ParityStatus = LayoutReadinessAccepted
      CompatibilityStatus = LayoutReadinessAccepted
      DiagnosticsStatus = LayoutReadinessAccepted
      Evidence = requiredFiles |> List.map evidence
      CompatibilityDeltas =
        [ { Surface = "FS.GG.UI.Layout"
            Change = "Feature151 broadens evidence without a new public API delta."
            Migration = Some "No migration required."
            Intentional = true } ]
      Limitations = [ "P7 compositor live proof remains environment-limited and is not claimed by P8." ] }

let rec findRepositoryRoot directory =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || Directory.GetFiles(directory, "*.slnx").Length > 0 then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let repositoryRoot = findRepositoryRoot AppContext.BaseDirectory

let readinessDirectory =
    Path.Combine(repositoryRoot, "specs", "151-complete-p8-layout", "readiness")

let existingReadinessFiles () =
    if Directory.Exists readinessDirectory then
        Directory.GetFiles(readinessDirectory)
        |> Array.map (fun path -> Path.GetFileName path |> Option.ofObj |> Option.defaultValue "")
        |> Array.toList
    else
        []
