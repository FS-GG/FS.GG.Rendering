// Feature 127 (Workstream F, F2) — on-demand regenerator for the committed color-policy reports
// (docs/reports/color-policy-{wcag,ant}.md).
//
// Research R3 decision: the policy engine is `module internal ColorPolicy` in FS.GG.UI.Color with
// NO public .fsi, and the design-system *pairing catalog* lives in the Controls.Tests assembly (the
// one place that references both Color and the internal DesignTokensExt). Reaching internal symbols
// of a built assembly from the `dotnet fsi` dynamic assembly via InternalsVisibleTo is fragile on
// net10 and, more importantly, the catalog is NOT in a referenceable library — so a standalone fsx
// would have to re-implement the catalog and evaluation, creating a SECOND evaluator that can drift
// from the compiled one. That is exactly the failure mode the single-evaluator design forbids.
//
// Therefore the supported regeneration path is the env-gated test update mode, which reuses the
// exact in-process `ColorPolicy.renderReport` evaluator the drift gate verifies. This script is a
// thin, dependency-free wrapper that shells out to it (no re-implementation, no internal-access
// tricks), keeping one source of truth.
//
// Usage:  dotnet fsi scripts/generate-policy-report.fsx

open System
open System.Diagnostics
open System.IO

let repoRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some p -> find p.FullName
            | None -> failwith "Could not locate repository root (FS.GG.Rendering.slnx)."
    find __SOURCE_DIRECTORY__

let psi = ProcessStartInfo("dotnet")
psi.WorkingDirectory <- repoRoot
psi.UseShellExecute <- false
[ "test"
  "tests/Controls.Tests/Controls.Tests.fsproj"
  "-c"; "Debug"
  "--filter"; "Feature127" ]
|> List.iter psi.ArgumentList.Add
psi.Environment["UPDATE_POLICY_REPORTS"] <- "1"

printfn "Regenerating docs/reports/color-policy-{wcag,ant}.md via the env-gated test evaluator..."
use proc = Process.Start psi
proc.WaitForExit()

if proc.ExitCode = 0 then
    printfn "Done. Re-run the suite without UPDATE_POLICY_REPORTS to verify the drift gate."
else
    eprintfn "Regeneration failed (exit code %d)." proc.ExitCode

exit proc.ExitCode
