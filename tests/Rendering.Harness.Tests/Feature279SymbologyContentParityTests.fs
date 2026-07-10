module Feature279SymbologyContentParityTests

// Feature 279 — the CONTENT-parity gate for the two fs-gg-symbology skill variants.
//
// `fs-gg-symbology` ships as two hand-maintained SKILL.md files stating one body of knowledge:
//   * LIBRARY  src/Symbology/skill/SKILL.md
//   * PRODUCT  template/product-skills/fs-gg-symbology/SKILL.md
// Every label feature (196->200) was written twice, once per file, and they drifted in emphasis. The
// existing SkillParity harness (Feature223) asserts only NAME-level parity (a wrapper exists); it says
// nothing about CONTENT, so the two variants can — and did — diverge on load-bearing doctrine while
// parity stayed green.
//
// This gate closes that hole WITHOUT prescribing structure (the "Assert" arm of #279, not "Generate"):
// scripts/check-symbology-skill-parity.fsx is the single source of the parity rules — it fails when the
// two variants diverge on the public Legibility API, the per-grammar label budgets, the grammar set,
// the identity-label invariants, or the escape-hatch doctrine, while ignoring the parts (channel table,
// fake.sh vs dotnet, GitHub vs in-tree links) that differ BY DESIGN. This test IS the CI gate: it runs
// that script on every PR (Rendering.Harness.Tests is in the slnx deterministic tier) and fails the
// gate when the script reports drift, surfacing the script's own report as the failure message.
//
// Deterministic and GL-free: the script reads two markdown files and shells nothing but `dotnet fsi`.

open System.Diagnostics
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

/// Run `dotnet fsi scripts/check-symbology-skill-parity.fsx` from the repo root; return its exit code
/// and combined stdout+stderr. The script is the single source of the parity rules (kept out of the
/// test so the two cannot drift); this test only enforces its verdict.
let private runParityScript () =
    let psi = ProcessStartInfo "dotnet"
    psi.WorkingDirectory <- repositoryRoot
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    [ "fsi"; "scripts/check-symbology-skill-parity.fsx" ] |> List.iter psi.ArgumentList.Add

    match Process.Start psi with
    | null -> failwith "could not start `dotnet fsi scripts/check-symbology-skill-parity.fsx`"
    | started ->
        use proc = started
        // Read both streams to end BEFORE WaitForExit so a full pipe buffer cannot deadlock the child.
        let outTask = proc.StandardOutput.ReadToEndAsync()
        let errTask = proc.StandardError.ReadToEndAsync()
        proc.WaitForExit()
        let combined = (outTask.Result + errTask.Result).TrimEnd()
        proc.ExitCode, combined

[<Tests>]
let feature279SymbologyContentParityTests =
    testList "Feature279 symbology content parity" [

        // The gate: the library and product symbology skills must agree on their load-bearing content.
        // Red case: change one variant's public API / label budget / invariant / escape-hatch doctrine
        // without the other, and the script exits non-zero — failing this test with its drift report.
        test "the two fs-gg-symbology variants are in load-bearing content parity" {
            let exitCode, output = runParityScript ()
            Expect.equal
                exitCode
                0
                (sprintf
                    "scripts/check-symbology-skill-parity.fsx reported symbology skill content drift:\n\n%s\n\nThe library (src/Symbology/skill/SKILL.md) and product (template/product-skills/fs-gg-symbology/SKILL.md) skills restate one body of knowledge (#279); a load-bearing change to one must land in the other. Run `dotnet fsi scripts/check-symbology-skill-parity.fsx` to reproduce."
                    output)
        }
    ]
