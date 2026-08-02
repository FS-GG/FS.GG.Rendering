module Issue1210AudioResolutionReadinessTests

open System.IO
open Expecto
open FS.GG.TestSupport

let private sourcePath =
    Path.Combine(RepositoryRoot.value, "template/base/src/Product/AudioCues.fs")

let private source () = File.ReadAllText sourcePath

[<Tests>]
let audioResolutionReadinessTests =
    testList "generated audio resolution readiness (#1210)" [
        test "cue ids are production-owned and resolution is independent from request evidence" {
            let text = source ()
            Expect.stringContains text "let declaredCueIds : SoundId list" "one product-owned declaration owns the cue vocabulary"
            Expect.stringContains text "let resolutionEvidence () : CueResolution list" "the resolver produces distinct content evidence"
            Expect.stringContains text "let audioContentReady ()" "build/publish checks have an explicit readiness predicate"
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
        }
    ]
