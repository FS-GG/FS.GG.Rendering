/// Interactive windowed mode (contracts/cli.md). Looks up the `SampleEntry` by id and calls
/// its `Interactive` closure (the GL-gated `Harness.runInteractive` → `runInteractiveApp`).
/// GL-gated and advisory: on a no-window/no-GL host the closure discloses the reason and
/// exits 0 without launching (FR-008) — it never hangs and never fakes a render.
module SampleApps.App.Interactive

open FS.GG.UI.Themes.Default.Theming
open SampleApps.Core

let run (sampleId: string) (mode: ThemeMode): int =
    match Registry.all |> List.tryFind (fun e -> e.Id = sampleId) with
    | Some entry -> entry.Interactive mode
    | None ->
        eprintfn "sample-apps: unknown sample '%s' (try: list)." sampleId
        1
