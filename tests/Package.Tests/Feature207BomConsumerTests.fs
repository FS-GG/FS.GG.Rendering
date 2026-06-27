module Feature207BomConsumerTests

// Feature 207 — ALWAYS-ON gate over the BOM consumer-validation report (validation contract Layer 1),
// mirroring GeneratedConsumerValidationTests / Feature163PackageFeedValidationTests /
// Feature204LifecycleTemplateTests.
//
// Deterministic and GL-free: it asserts the report written by scripts/validate-bom-consumer.fsx. The
// HEAVY live work — pack the coherent snapshot to a throwaway feed, restore a clean consumer whose
// only FS.GG.UI declaration is `FS.GG.UI@V`, double-restore for reproducibility, and force a member
// to Y<V / Y>V under a warnings-as-errors policy — runs behind FS_GG_RUN_BOM_CONSUMER_SMOKE=1 inside
// that script (the env-gated regenerator), exactly the repo's two-layer package-validation pattern.
//
// If the committed report is absent (gitignored on a fresh checkout) the test self-provisions a
// verdict-core report env-free so the structural gate is green-by-construction; the live-only tokens
// (resolved-members-at-version, forced-mismatch, reproducibility) are asserted only when the report
// discloses `provenance: live` — the committed evidence — so a degraded verdict-core checkout stays
// honest rather than falsely green on numbers it never measured.

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private reportPath =
    repositoryPath "specs/207-ui-bom-metapackage/readiness/bom-consumer-validation.md"

// ---- self-provisioning (mirrors Feature204) ---------------------------------------------------
let private selfProvisionReport () =
    if not (File.Exists reportPath) then
        let psi = ProcessStartInfo "dotnet"
        psi.WorkingDirectory <- repositoryRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        [ "fsi"; "scripts/validate-bom-consumer.fsx" ] |> List.iter psi.ArgumentList.Add
        match Process.Start psi with
        | null -> ()
        | started ->
            use proc = started
            proc.StandardOutput.ReadToEnd() |> ignore
            proc.StandardError.ReadToEnd() |> ignore
            proc.WaitForExit()

let private reportProvisioned = selfProvisionReport ()

let private readReport () =
    Expect.isTrue
        (File.Exists reportPath)
        (sprintf
            "BOM consumer validation report missing at %s — regenerate via FS_GG_RUN_BOM_CONSUMER_SMOKE=1 dotnet fsi scripts/validate-bom-consumer.fsx"
            reportPath)
    File.ReadAllText reportPath

[<Tests>]
let feature207BomConsumerTests =
    testList "Feature207 BOM consumer validation" [

        // Structural tokens present in BOTH provenance modes (env-free verdict core re-derives them).
        test "report records the BOM identity, parity, and dependencies-only shape" {
            let report = readReport ()
            [ "feature: 207-ui-bom-metapackage"
              "bom-package-id: FS.GG.UI"
              "members-expected: 16"
              "parity: pass"
              "single-version-token: true"
              "exact-bracket-form: true"
              "ships-no-lib: true"
              "single-reference: true"
              "result: pass" ]
            |> List.iter (fun token -> Expect.stringContains report token (sprintf "report includes %s" token))
        }

        // The corrected-mechanism disclosure (constitution V: disclose; no synthetic green). The report
        // must carry the observed codes + consumer-policy condition, not R1's unconditional NU1107 claim.
        test "report discloses the observed loud-deviation mechanism (NU1605/NU1608 + warnings-as-errors)" {
            let report = readReport ()
            [ "NU1605"; "NU1608"; "WarningsAsErrors"; "mechanism-note:" ]
            |> List.iter (fun token -> Expect.stringContains report token (sprintf "mechanism disclosure includes %s" token))
        }

        // Provenance is disclosed (live committed evidence OR env-free verdict-core self-provision).
        test "report discloses provenance" {
            let report = readReport ()
            Expect.isTrue
                (report.Contains "provenance: live" || report.Contains "provenance: verdict-core")
                "report discloses whether it was self-provisioned env-free or written from the live run"
        }

        // Live-only evidence (US1 N->1 collapse, US3 reproducibility/channel, US2 forced-mismatch both
        // directions). Asserted only when the committed report is the live run — a verdict-core fresh
        // checkout is exempt so it stays honest rather than asserting numbers it never measured.
        test "live report evidences one-reference coherence, reproducibility, and forced mismatch (both directions)" {
            let report = readReport ()
            if report.Contains "provenance: live" then
                [ "bom-version: 0.1.51-preview.1"
                  "members-resolved: 16"
                  "resolved-members-at-version: 16/16 at 0.1.51-preview.1"
                  "clean-consumer-build: pass"
                  "channel: preview"
                  "reproducibility: identical"
                  "forced-mismatch: pass"
                  "downgrade Y<V"
                  "code=NU1605 loud-under-warnaserror-exit=1"
                  "upgrade   Y>V"
                  "code=NU1608 loud-under-warnaserror-exit=1" ]
                |> List.iter (fun token -> Expect.stringContains report token (sprintf "live report includes %s" token))
            else
                Expect.stringContains report "provenance: verdict-core" "non-live report must disclose verdict-core provenance"
        }
    ]
