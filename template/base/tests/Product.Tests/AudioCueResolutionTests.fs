module AppRoot.AudioCueResolutionTests

open System
open System.IO
open System.Text
open Expecto

//#if (profile == "app" || profile == "sample-pack" || profile == "game")
let private validWave () =
    let bytes = Array.zeroCreate<byte> 44
    Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0)
    Encoding.ASCII.GetBytes("WAVE").CopyTo(bytes, 8)
    bytes

let private withFixture action =
    let root = Path.Combine(Path.GetTempPath(), "fs-gg-audio-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory root |> ignore
    try action root
    finally Directory.Delete(root, true)

let private writeCue root (FS.GG.Audio.Core.SoundId id) (bytes: byte[]) =
    Directory.CreateDirectory root |> ignore
    File.WriteAllBytes(Path.Combine(root, id + ".wav"), bytes)

let private populateCompleteMap root =
    for id in AppRoot.AudioCues.declaredCueIds do
        AppRoot.AudioCues.writeDeterministicPlaceholder root id |> ignore

let private optionalEnvironment name =
    match Environment.GetEnvironmentVariable name with
    | null | "" -> None
    | value -> Some value

[<Tests>]
let audioCueResolutionTests =
    testList "audio cue resolution readiness (#1210)" [
        test "fresh and missing-one fixtures remain red independently of requested cues" {
            withFixture (fun root ->
                Expect.isFalse (AppRoot.AudioCues.audioContentReadyAt root) "fresh scaffold content is explicitly incomplete"
                let first = AppRoot.AudioCues.declaredCueIds |> List.head
                writeCue root first (validWave ())
                Expect.isFalse (AppRoot.AudioCues.audioContentReadyAt root) "one valid asset cannot hide every other missing declared cue")
        }
        test "malformed and complete fixtures are classified by resolver readiness" {
            withFixture (fun root ->
                populateCompleteMap root
                Expect.isTrue (AppRoot.AudioCues.audioContentReadyAt root) "complete map is ready"
                let damaged = AppRoot.AudioCues.declaredCueIds |> List.head
                writeCue root damaged (Encoding.UTF8.GetBytes "not-a-wave")
                let malformed = AppRoot.AudioCues.resolutionEvidenceAt root |> List.find (fun finding -> finding.CueId = damaged)
                Expect.equal malformed.Problem (Some "malformed WAV") "malformed asset is named rather than silently resolving"
                Expect.isFalse (AppRoot.AudioCues.audioContentReadyAt root) "corrupting a complete map returns it to red")
        }
        test "placeholder option is deterministic and carries a reviewable digest" {
            let cue = AppRoot.AudioCues.declaredCueIds |> List.head
            let first = AppRoot.AudioCues.deterministicPlaceholderWave cue
            let second = AppRoot.AudioCues.deterministicPlaceholderWave cue
            Expect.equal first second "same source parameters emit byte-identical WAV bytes"
            Expect.equal (AppRoot.AudioCues.placeholderSha256 first) (AppRoot.AudioCues.placeholderSha256 second) "digest is reproducible"
        }
        // The repository-owned integration probe filters these two tests by name. The writer creates
        // source assets through the runtime helper; the probe then checks real Release build and
        // publish directories, plus mutated missing/malformed controls, through runtime readiness.
        test "artifact fixture writer" {
            match optionalEnvironment "FSGG_AUDIO_FIXTURE_SOURCE_ROOT" with
            | None -> ()
            | Some root ->
                populateCompleteMap root
                Expect.isTrue (AppRoot.AudioCues.audioContentReadyAt root) "source fixture is complete"
        }
        test "artifact output readiness probe" {
            match optionalEnvironment "FSGG_AUDIO_FIXTURE_PROBE_ROOT", optionalEnvironment "FSGG_AUDIO_FIXTURE_EXPECT" with
            | Some root, Some "complete" ->
                Expect.isTrue (AppRoot.AudioCues.audioContentReadyAt root) "actual artifact output is complete"
                for id in AppRoot.AudioCues.declaredCueIds do
                    let expected = AppRoot.AudioCues.deterministicPlaceholderWave id |> AppRoot.AudioCues.placeholderSha256
                    let (FS.GG.Audio.Core.SoundId raw) = id
                    let actual = File.ReadAllBytes(Path.Combine(root, raw + ".wav")) |> AppRoot.AudioCues.placeholderSha256
                    Expect.equal actual expected $"{raw} artifact digest equals committed-source generation"
            | Some root, Some expectedProblem ->
                let findings = AppRoot.AudioCues.resolutionEvidenceAt root
                Expect.isTrue (findings |> List.exists (fun finding -> finding.Problem = Some expectedProblem)) $"actual artifact reports {expectedProblem}"
            | _ -> ()
        }
    ]
//#endif
