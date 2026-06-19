module Feature163PackageFeedTests

open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature163 PackageFeed" [
        test "discovers packable packages and classifies stale current and missing pins" {
            let root = Feature163TestFixtures.createTempRoot "feature163-package-feed"

            try
                let feed = Path.Combine(root, "feed")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.2.3" |> ignore
                Feature163TestFixtures.writePackageProject root "src/Scene/Scene.fsproj" "FS.GG.UI.Scene" "1.2.4" |> ignore
                Feature163TestFixtures.writeSampleProject
                    root
                    "samples/Demo/Demo.fsproj"
                    [ "FS.GG.UI.Controls", "1.0.0"
                      "FS.GG.UI.Scene", "1.2.4"
                      "FS.GG.UI.Unknown", "9.9.9" ]
                |> ignore

                let packages = PackageFeed.discoverPackablePackages root feed
                Expect.equal packages.Length 2 "packages"
                Expect.contains (packages |> List.map _.PackageId) "FS.GG.UI.Controls" "controls package"

                let pins = PackageFeed.readSelectedPackagePins root [ "samples/Demo" ] packages Set.empty []
                let byId = pins |> List.map (fun pin -> pin.PackageId, pin) |> Map.ofList

                Expect.equal byId["FS.GG.UI.Controls"].Status PackageFeed.Stale "stale controls"
                Expect.equal byId["FS.GG.UI.Scene"].Status PackageFeed.Current "current scene"
                Expect.equal byId["FS.GG.UI.Unknown"].Status PackageFeed.MissingExpectedPackage "missing expected package"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "refresh mode rewrites stale FS.GG.UI package pins before sample build" {
            let root = Feature163TestFixtures.createTempRoot "feature163-package-refresh"

            try
                let feed = Path.Combine(root, "feed")
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "2.0.0" |> ignore
                let sample =
                    Feature163TestFixtures.writeSampleProject
                        root
                        "samples/Demo/Demo.fsproj"
                        [ "FS.GG.UI.Controls", "1.0.0" ]

                let options: PackageFeed.PackageFeedOptions =
                    { RepositoryRoot = root
                      SelectedSamples = [ "samples/Demo" ]
                      FeedPath = feed
                      OutDir = outDir
                      Mode = PackageFeed.Refresh
                      PackBeforeCheck = false
                      IsolatedCachePath = None
                      Cold = false
                      ClearGlobalCache = false
                      AllowedExceptionIds = Set.empty
                      CompatibilityExceptions = [] }

                let result = PackageFeed.runWorkflow options

                Expect.equal result.Status PackageFeed.Passed "refresh resolves stale pins"
                Expect.contains result.ChangedFiles sample "changed sample"
                Expect.stringContains (File.ReadAllText sample) "Version=\"2.0.0\"" "pin refreshed"
                Expect.isTrue (File.Exists(Path.Combine(outDir, "package-pins.md"))) "pin evidence"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "pure MVU emits discovery sample feed restore and evidence effects" {
            let options: PackageFeed.PackageFeedOptions =
                { RepositoryRoot = "/repo"
                  SelectedSamples = [ "samples/AntShowcase" ]
                  FeedPath = "/feed"
                  OutDir = "/out"
                  Mode = PackageFeed.Proof
                  PackBeforeCheck = true
                  IsolatedCachePath = Some "/cache"
                  Cold = false
                  ClearGlobalCache = false
                  AllowedExceptionIds = Set.empty
                  CompatibilityExceptions = [] }

            let model, effects = PackageFeed.init options
            Expect.equal model.RepositoryRoot "/repo" "root"
            Expect.contains effects PackageFeed.PackLocalFeed "pack"
            Expect.contains effects PackageFeed.ReadProjectFiles "discover"
            Expect.contains effects PackageFeed.ReadSampleProjects "samples"
            Expect.contains effects PackageFeed.CreateGeneratedNuGetConfig "config"
            Expect.contains effects PackageFeed.RunRestore "restore"
            Expect.contains effects PackageFeed.WritePackageEvidence "evidence"

            let failed, failedEffects = PackageFeed.update (PackageFeed.WorkflowFailed "boom") model
            Expect.equal failed.Status (Some PackageFeed.Failed) "failed"
            Expect.contains failedEffects PackageFeed.WritePackageEvidence "failed evidence"
        }

        test "accepted compatibility exception is explicit and not a silent stale-pin pass" {
            let root = Feature163TestFixtures.createTempRoot "feature163-compat-exception"

            try
                let feed = Path.Combine(root, "feed")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "2.0.0" |> ignore
                Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "FS.GG.UI.Controls", "1.0.0" ] |> ignore

                let ex: PackageFeed.CompatibilityException =
                    { Id = "demo-controls-v1"
                      PackageId = "FS.GG.UI.Controls"
                      DeclaredVersion = "1.0.0"
                      ExpectedVersion = "2.0.0"
                      SamplePath = "Demo.fsproj"
                      Reason = "compatibility coverage"
                      Owner = "test"
                      Review = "expires on next package feature" }

                let packages = PackageFeed.discoverPackablePackages root feed
                let pins = PackageFeed.readSelectedPackagePins root [ "samples/Demo" ] packages (Set.ofList [ ex.Id ]) [ ex ]

                Expect.equal pins[0].Status PackageFeed.CompatibilityException "exception status"
                Expect.equal pins[0].CompatibilityExceptionId (Some ex.Id) "exception id"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }
    ]
