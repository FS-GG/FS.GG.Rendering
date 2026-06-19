module Feature163PackageSourceProofTests

open System.IO
open Expecto
open Rendering.Harness

let private baseOptions (root: string) (feed: string) (outDir: string) (mode: PackageFeed.PackageFeedMode) : PackageFeed.PackageFeedOptions =
    { RepositoryRoot = root
      SelectedSamples = [ "samples/Demo" ]
      FeedPath = feed
      OutDir = outDir
      Mode = mode
      PackBeforeCheck = false
      IsolatedCachePath = Some(Path.Combine(outDir, "cache"))
      Cold = false
      ClearGlobalCache = false
      AllowedExceptionIds = Set.empty
      CompatibilityExceptions = [] }

[<Tests>]
let tests =
    testList "Feature163 PackageSourceProof" [
        test "generated NuGet config constrains FS.GG.UI packages to local feed rule" {
            let root = Feature163TestFixtures.createTempRoot "feature163-source-rules"

            try
                let config = Path.Combine(root, "source-rules.nuget.config")
                let rules = PackageFeed.writeGeneratedNuGetConfig config (Path.Combine(root, "feed"))
                let text = File.ReadAllText config

                Expect.equal rules.Length 2 "rules"
                Expect.stringContains text "<packageSourceMapping>" "mapping"
                Expect.stringContains text "pattern=\"FS.GG.UI.*\"" "local framework mapping"
                Expect.stringContains text "key=\"nuget-local\"" "local source"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "proof mode fails closed when expected local feed package is missing" {
            let root = Feature163TestFixtures.createTempRoot "feature163-missing-feed"

            try
                let feed = Path.Combine(root, "feed")
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore
                Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "FS.GG.UI.Controls", "1.0.0" ] |> ignore

                let result = PackageFeed.runWorkflow (baseOptions root feed outDir PackageFeed.Proof)

                Expect.equal result.Status PackageFeed.Failed "missing local package fails"
                let proof = result.SourceProof.Value
                Expect.exists proof.Violations (fun v -> v.Contains "missing-local-package") "missing package violation"
                Expect.isTrue (File.Exists(Path.Combine(outDir, "source-proof.json"))) "json evidence"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "proof mode reports no-selected-samples and no-package-pins distinctly" {
            let root = Feature163TestFixtures.createTempRoot "feature163-proof-empty"

            try
                let feed = Path.Combine(root, "feed")
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore

                let noSamples =
                    PackageFeed.runWorkflow
                        { baseOptions root feed outDir PackageFeed.Proof with
                            SelectedSamples = [] }

                Expect.exists noSamples.SourceProof.Value.Violations (fun v -> v.Contains "no-selected-samples") "no samples"

                Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "Other.Package", "1.0.0" ] |> ignore
                let noPins = PackageFeed.runWorkflow (baseOptions root feed (Path.Combine(root, "out2")) PackageFeed.Proof)
                Expect.exists noPins.SourceProof.Value.Violations (fun v -> v.Contains "no-package-pins") "no pins"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "clear-global-cache without cold mode is a cache policy violation" {
            let root = Feature163TestFixtures.createTempRoot "feature163-cache-policy"

            try
                let feed = Path.Combine(root, "feed")
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore
                Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "FS.GG.UI.Controls", "1.0.0" ] |> ignore
                Feature163TestFixtures.touch (Path.Combine(feed, "FS.GG.UI.Controls.1.0.0.nupkg"))

                let result =
                    PackageFeed.runWorkflow
                        { baseOptions root feed outDir PackageFeed.Proof with
                            ClearGlobalCache = true
                            Cold = false }

                Expect.equal result.Status PackageFeed.Failed "cache policy fails"
                Expect.exists result.SourceProof.Value.Violations (fun v -> v.Contains "cache-policy-violation") "cache violation"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }
    ]
