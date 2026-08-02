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
        test "malformed, complete, build-output and publish-output fixtures are classified by resolver readiness" {
            withFixture (fun root ->
                for id in AppRoot.AudioCues.declaredCueIds do writeCue root id (validWave ())
                Expect.isTrue (AppRoot.AudioCues.audioContentReadyAt root) "complete map is ready"
                let damaged = AppRoot.AudioCues.declaredCueIds |> List.head
                writeCue root damaged (Encoding.UTF8.GetBytes "not-a-wave")
                let malformed = AppRoot.AudioCues.resolutionEvidenceAt root |> List.find (fun finding -> finding.CueId = damaged)
                Expect.equal malformed.Problem (Some "malformed WAV") "malformed asset is named rather than silently resolving"
                let buildOutput = Path.Combine(root, "build", "assets", "audio")
                let publishOutput = Path.Combine(root, "publish", "assets", "audio")
                Expect.isFalse (AppRoot.AudioCues.audioContentReadyAt buildOutput) "build output without copied cues is red"
                Expect.isFalse (AppRoot.AudioCues.audioContentReadyAt publishOutput) "publish output without copied cues is red")
        }
    ]
//#endif
