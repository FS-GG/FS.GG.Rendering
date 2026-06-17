/// Headless deterministic evidence mode (contracts/cli.md). Thin orchestrator: select the
/// requested sample(s) from the registry, run each one's `RunEvidence` closure (the shared
/// `Harness.evidenceFor` — `Perf.runScript` state + outcome derivation + degrade-and-disclose
/// screenshot + record writes), print a one-line disclosure per sample, and return the exit
/// code. The state + outcome half needs no GL, so the run degrades cleanly on a no-GL host
/// (FR-008) and always exits 0 on success (FR-014).
module SampleApps.App.Evidence

open SampleApps.Core
open SampleApps.Core.Harness

/// Run evidence over all samples (or the one named by `sampleFilter`). Exit `0` on success
/// (including disclosed degraded runs); exit `2` only when an explicit `--sample` matched
/// nothing.
let run (seed: int) (outDir: string) (sampleFilter: string option): int =
    let samples =
        match sampleFilter with
        | Some id -> Registry.all |> List.filter (fun e -> e.Id = id)
        | None -> Registry.all
    match sampleFilter with
    | Some id when List.isEmpty samples ->
        eprintfn "sample-apps: no sample matched '%s'." id
        2
    | _ ->
        let records = samples |> List.map (fun (e: SampleEntry) -> e.RunEvidence seed outDir)
        for r in records do
            printfn
                "  %-10s provesScreenshot=%-5b outcome=%-13s notAuthoritativeFor=[%s]"
                r.SampleId
                r.Screenshot.ProvesScreenshot
                r.Outcome.Kind
                (String.concat "; " r.NotAuthoritativeFor)
        printfn "sample-apps: wrote %d sample evidence record(s) under %s/%d" (List.length records) outDir seed
        0
