namespace FS.GG.DocFences

open System.IO
open System.Text.RegularExpressions
open FS.GG.TestSupport

/// THE LIVE PIN (spec 255, FR-009).
///
/// Reads the pinned FS.GG.* packages and their versions from the SAME file the template hands a scaffolded
/// product — `template/base/Directory.Packages.props` — so the harness restores exactly what a reader's
/// product would. There is NO second hardcoded oracle version: the axis literals below are read from the
/// props on every run, and when one moves the harness follows it (the `oracleVersion = "0.9.0"` smell this
/// feature exists to delete).
module Pins =

    let private propsPath =
        Path.Combine(RepositoryRoot.value, "template", "base", "Directory.Packages.props")

    let private props = lazy (File.ReadAllText propsPath)

    let private axis (name: string) =
        let m = Regex.Match(props.Value, $"<{name}>([^<]+)</{name}>")
        if m.Success then m.Groups.[1].Value
        else failwithf "<%s> not found in %s" name propsPath

    /// The live UI-axis pin — the one THIS repo's merge publishes, and the axis the `PinPending` waiver can
    /// apply to.
    let uiVersion = lazy (axis "FsGgUiVersion")
    let gameVersion = lazy (axis "FsGgGameVersion")
    let audioVersion = lazy (axis "FsGgAudioVersion")

    let private versionForAxisToken (token: string) =
        match token with
        | "FsGgUiVersion" -> uiVersion.Value
        | "FsGgGameVersion" -> gameVersion.Value
        | "FsGgAudioVersion" -> audioVersion.Value
        | other -> failwithf "unknown version axis '%s' in %s" other propsPath

    /// Every pinned FS.GG.* package, paired with the version its axis resolves to. This is the reference set
    /// the harness hands the generated fence project.
    let pinnedPackages : (string * string) list =
        Regex.Matches(props.Value, @"<PackageVersion\s+Include=""(FS\.GG\.[^""]+)""\s+Version=""\$\((FsGg\w+Version)\)""")
        |> Seq.map (fun m -> m.Groups.[1].Value, versionForAxisToken (m.Groups.[2].Value))
        |> Seq.distinctBy fst
        |> List.ofSeq
