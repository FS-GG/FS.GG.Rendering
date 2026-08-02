module Issue1210AudioResolutionReadinessTests

open System.IO
open System.Diagnostics
open Expecto
open FS.GG.TestSupport

let private sourcePath =
    Path.Combine(RepositoryRoot.value, "template/base/src/Product/AudioCues.fs")

let private source () = File.ReadAllText sourcePath

let private quote (value: string) = "\"" + value.Replace("\"", "\\\"") + "\""

let private run (workingDirectory: string) (arguments: string) (environment: (string * string) list) =
    let startInfo = ProcessStartInfo("dotnet", arguments)
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    for name, value in environment do startInfo.Environment.[name] <- value
    use child =
        match Process.Start startInfo |> Option.ofObj with
        | Some child -> child
        | None -> failwith $"could not start dotnet {arguments}"
    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    child.WaitForExit()
    let output = stdout.Result + stderr.Result
    Expect.equal child.ExitCode 0 $"dotnet {arguments} succeeds\n{output}"

let private generatedArtifactProbe () =
    let temp = Path.Combine(Path.GetTempPath(), "fsgg-audio-artifacts-" + System.Guid.NewGuid().ToString("N"))
    let pack = Path.Combine(temp, "pack")
    let hive = Path.Combine(temp, "hive")
    let product = Path.Combine(temp, "product")
    Directory.CreateDirectory pack |> ignore
    Directory.CreateDirectory hive |> ignore
    try
        let templateProject = Path.Combine(RepositoryRoot.value, ".template.package/FS.GG.UI.Template.fsproj")
        run RepositoryRoot.value ($"pack {quote templateProject} -o {quote pack}") []
        let package = Directory.GetFiles(pack, "FS.GG.UI.Template.*.nupkg") |> Array.exactlyOne
        run RepositoryRoot.value ($"new install {quote package} --force --debug:custom-hive {quote hive}") []
        run RepositoryRoot.value ($"new fs-gg-ui --name ArtifactAudio --output {quote product} --profile app --lifecycle none --debug:custom-hive {quote hive}") []
        let tests = Path.Combine(product, "tests/ArtifactAudio.Tests/ArtifactAudio.Tests.fsproj")
        let project = Path.Combine(product, "src/ArtifactAudio/ArtifactAudio.fsproj")
        let sourceAssets = Path.Combine(product, "assets/audio")
        run product ($"restore {quote tests} --locked-mode") []
        let writerFilter = quote "artifact fixture writer"
        run product ($"test {quote tests} --no-restore --filter {writerFilter}") [ "FSGG_AUDIO_FIXTURE_SOURCE_ROOT", sourceAssets ]
        run product ($"build {quote project} -c Release --no-restore") []
        let buildAssets = Path.Combine(product, "src/ArtifactAudio/bin/Release/net10.0/assets/audio")
        let publish = Path.Combine(temp, "publish")
        run product ($"publish {quote project} -c Release --no-restore -o {quote publish}") []
        let publishAssets = Path.Combine(publish, "assets/audio")
        let probe expected root =
            let probeFilter = quote "artifact output readiness probe"
            run product ($"test {quote tests} --no-restore --no-build --filter {probeFilter}")
                [ "FSGG_AUDIO_FIXTURE_PROBE_ROOT", root; "FSGG_AUDIO_FIXTURE_EXPECT", expected ]
        probe "complete" buildAssets
        probe "complete" publishAssets
        File.Delete(Path.Combine(buildAssets, "start.wav"))
        probe "missing" buildAssets
        File.WriteAllText(Path.Combine(publishAssets, "start.wav"), "not-a-wave")
        probe "malformed WAV" publishAssets
    finally
        if Directory.Exists temp then Directory.Delete(temp, true)

[<Tests>]
let audioResolutionReadinessTests =
    testList "generated audio resolution readiness (#1210)" [
        test "cue ids are production-owned and resolution is independent from request evidence" {
            let text = source ()
            Expect.stringContains text "let declaredCueIds : SoundId list" "one product-owned declaration owns the cue vocabulary"
            Expect.stringContains text "let resolutionEvidence () : CueResolution list" "the resolver produces distinct content evidence"
            Expect.stringContains text "let audioContentReady ()" "build/publish checks have an explicit readiness predicate"
            Expect.stringContains text "let writeDeterministicPlaceholder" "scaffold authors have an executable placeholder option"
            Expect.stringContains text "request-only test must never stand in for it" "guidance forbids request evidence from certifying assets"
        }

        test "missing and malformed WAVs are explicit findings, while runtime stays safely degradable" {
            let text = source ()
            Expect.stringContains text "if not (File.Exists path) then Some \"missing\"" "a missing declared asset is named"
            Expect.stringContains text "else Some \"malformed WAV\"" "a malformed declared asset is named"
            Expect.stringContains text "bytes.Length >= 44" "a deterministic binary header floor prevents renamed text files"
            Expect.stringContains text "if isWave bytes then Some bytes else None" "runtime resolver deliberately degrades malformed content"
        }

        test "scaffold default is deliberately incomplete instead of silently shipping silent assets" {
            let assetDirectory = Path.Combine(RepositoryRoot.value, "template/base/assets/audio")
            Expect.isFalse (Directory.Exists assetDirectory) "the base scaffold has no pretend audio assets"
            Expect.stringContains (source ()) "intentionally asset-less scaffold" "the red default is documented beside its executable predicate"
            Expect.stringContains (source ()) "deterministic reviewable PCM WAV bytes from committed source" "generated binary placeholders are a reviewable option, not hidden output"
            let project = File.ReadAllText(Path.Combine(RepositoryRoot.value, "template/base/src/Product/Product.fsproj"))
            Expect.stringContains project "CopyToOutputDirectory=\"PreserveNewest\"" "authored WAVs are copied into real build/publish output"
        }
        test "fresh scaffold build and publish outputs preserve generated bytes and readiness controls" {
            generatedArtifactProbe ()
        }
    ]
