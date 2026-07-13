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

                // #300 — a proof that short-circuits before the build must SAY it never built. If
                // BuildLogPath were `Some` here, "did not compile the consumer" and "compiled it
                // clean" would render identically in the evidence.
                Expect.isNone proof.BuildLogPath "no build log when the proof never reached the build"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // #300 — the mirror rule: a sample pin that disagrees with `src` `<Version>` cannot resolve,
        // because the samples map FS.GG.UI.* exclusively to a local feed that only `dotnet pack`
        // fills. Renovate PR #233 proposed exactly this and merged 4/4 green. `pinViolations` is
        // what the required gate prints, so it must name the offending pin, both versions, and the
        // project — a bare exit code sends the next reader to bump `<Version>` instead.
        test "pinViolations names each stale pin with expected and actual versions" {
            // Pins carry ABSOLUTE project paths (readSelectedPackagePins stores what the filesystem
            // enumeration returned); the violation line relativises them for the CI log.
            let root = Path.Combine(Path.GetTempPath(), "repo")

            let pin id declared expected status : PackageFeed.PackagePin =
                { PackageId = id
                  DeclaredVersion = declared
                  ExpectedVersion = expected
                  ProjectFilePath = Path.Combine(root, "samples", "Demo", "Demo.fsproj")
                  Status = status
                  CompatibilityExceptionId = None }

            let violations =
                PackageFeed.pinViolations
                    root
                    [ pin "FS.GG.UI.Controls" "0.4.0" (Some "0.4.0-preview.1") PackageFeed.Stale
                      pin "FS.GG.UI.Scene" "0.4.0-preview.1" (Some "0.4.0-preview.1") PackageFeed.Current
                      pin "FS.GG.UI.Ghost" "1.0.0" None PackageFeed.MissingExpectedPackage
                      pin "FS.GG.UI.Waived" "0.3.0" (Some "0.4.0-preview.1") PackageFeed.CompatibilityException ]

            Expect.hasLength violations 2 "only stale and missing-expected pins are violations"

            let stale = violations |> List.find (fun v -> v.StartsWith "stale-pin")
            Expect.stringContains stale "FS.GG.UI.Controls" "names the package"
            Expect.stringContains stale "expected 0.4.0-preview.1" "names the src <Version>"
            Expect.stringContains stale "actual 0.4.0" "names the pin that was proposed"
            Expect.stringContains stale "samples/Demo/Demo.fsproj" "names the project to fix"
            Expect.isFalse (stale.Contains root) "the path is relativised — an absolute runner path is noise in a CI log"

            Expect.exists violations (fun v -> v.StartsWith "missing-expected-package") "missing package"

            // A Current pin and an accepted CompatibilityException are silent — the gate must not
            // redden on a waiver that Feature 163 deliberately accepted.
            Expect.isFalse (violations |> List.exists (fun v -> v.Contains "FS.GG.UI.Scene")) "current pin is silent"
            Expect.isFalse (violations |> List.exists (fun v -> v.Contains "FS.GG.UI.Waived")) "accepted exception is silent"
        }

        // #300 — the four samples were ungated because nothing enumerated them. A hardcoded list in
        // two workflows would reproduce that: a fifth consumer would be silently unchecked. A sample
        // consumes packages exactly when its own nuget.config maps FS.GG.UI.* to the local feed.
        test "package-consuming samples are discovered by their nuget.config source mapping" {
            let root = Feature163TestFixtures.createTempRoot "feature163-discovery"

            try
                let writeConfig (sample: string) (mapping: string) =
                    let dir = Path.Combine(root, "samples", sample)
                    Directory.CreateDirectory dir |> ignore
                    File.WriteAllText(Path.Combine(dir, "nuget.config"), mapping)

                writeConfig "AntShowcase" "<configuration><packageSourceMapping><package pattern=\"FS.GG.UI.*\" /></packageSourceMapping></configuration>"
                writeConfig "ControlsGallery" "<configuration><packageSourceMapping><package pattern=\"FS.GG.UI.*\" /></packageSourceMapping></configuration>"
                // A sample with a nuget.config that does NOT map the framework packages is an
                // in-solution sample; it proves nothing about the packaged path and must not be picked up.
                writeConfig "CanvasDemo" "<configuration><packageSources><clear /></packageSources></configuration>"
                // ...and a sample with no nuget.config at all.
                Directory.CreateDirectory(Path.Combine(root, "samples", "SymbologyBoard")) |> ignore

                let discovered = PackageFeed.discoverPackageConsumingSamples root

                Expect.equal discovered [ "samples/AntShowcase"; "samples/ControlsGallery" ] "only the package-consuming samples, sorted"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "discovery on a tree with no samples directory returns nothing rather than throwing" {
            let root = Feature163TestFixtures.createTempRoot "feature163-discovery-empty"

            try
                Expect.isEmpty (PackageFeed.discoverPackageConsumingSamples root) "no samples/ dir"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // #266 — "nothing to check" and "checked, and it's fine" must not share an exit code. A
        // selected sample that contributes no project must never restore-and-build zero things and
        // call that `passed`. (`no-consumer-projects` guards the same class one level deeper, where
        // pin discovery and project discovery disagree; it is unreachable while both read the same
        // enumeration, and is kept so a future change to either cannot open a vacuous pass.)
        test "proof mode fails closed when a selected sample contributes nothing to build" {
            let root = Feature163TestFixtures.createTempRoot "feature163-no-projects"

            try
                let feed = Path.Combine(root, "feed")
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore
                Feature163TestFixtures.touch (Path.Combine(feed, "FS.GG.UI.Controls.1.0.0.nupkg"))

                let result =
                    PackageFeed.runWorkflow
                        { baseOptions root feed outDir PackageFeed.Proof with
                            SelectedSamples = [ "samples/Other" ] }

                Expect.equal result.Status PackageFeed.Failed "zero consumer projects is not a pass"
                let proof = result.SourceProof.Value
                Expect.exists proof.Violations (fun v -> v.Contains "no-package-pins" || v.Contains "no-consumer-projects") "fails closed, and says which"
                Expect.isNone proof.BuildLogPath "nothing was built"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "the mirror-rule hint tells the reader to fix the pin, not the version" {
            Expect.stringContains PackageFeed.mirrorRuleHint "samples/" "names where the pin lives"
            Expect.stringContains PackageFeed.mirrorRuleHint "<Version>" "names what it must mirror"
            Expect.stringContains PackageFeed.mirrorRuleHint "NU1102" "names the failure it prevents"
            Expect.stringContains PackageFeed.mirrorRuleHint "Fix the pin" "names the remedy"
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

        // #686 — the evidence under readiness/package-proof/ is COMMITTED, so it must not name the
        // machine that wrote it. It used to: the pin table wrote `pin.ProjectFilePath` raw, which is
        // absolute, so regenerating from any other checkout rewrote all 60-odd rows. That is not just
        // churn — it makes the artifact unreadable AS EVIDENCE, because a pin that has actually gone
        // stale is indistinguishable in the diff from "someone ran this from a different directory".
        //
        // Two checkouts of identical content are the whole bug, so assert the whole bug: same content,
        // different root, byte-identical evidence.
        test "evidence from two different checkouts of the same content is byte-identical" {
            let rootA = Feature163TestFixtures.createTempRoot "feature163-checkout-a"
            let rootB = Feature163TestFixtures.createTempRoot "feature163-checkout-b"

            try
                let evidence (root: string) =
                    Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore
                    Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "FS.GG.UI.Controls", "1.0.0" ] |> ignore

                    let outDir = Path.Combine(root, "out")
                    PackageFeed.runWorkflow (baseOptions root (Path.Combine(root, "feed")) outDir PackageFeed.Check)
                    |> ignore

                    File.ReadAllText(Path.Combine(outDir, "package-pins.md")),
                    File.ReadAllText(Path.Combine(outDir, "package-versions.md"))

                let pinsA, versionsA = evidence rootA
                let pinsB, versionsB = evidence rootB

                Expect.equal pinsA pinsB "the pin table does not move when the checkout does"
                Expect.equal versionsA versionsB "the version table does not move when the checkout does"

                // ...and it is relative because the paths were relativised, not because both runs
                // happened to be empty.
                Expect.stringContains pinsA "samples/Demo/Demo.fsproj" "the pin names its project, relative to the root"
                Expect.stringContains versionsA "src/Controls/Controls.fsproj" "the package names its project, relative to the root"
                Expect.isFalse (pinsA.Contains rootA) "no absolute path survives into the committed pin table"
                Expect.isFalse (versionsA.Contains rootA) "no absolute path survives into the committed version table"
            finally
                Feature163TestFixtures.deleteTempRoot rootA
                Feature163TestFixtures.deleteTempRoot rootB
        }

        // #686 — the same rule for the proof evidence, which carries paths the tables do not: the
        // package cache and the recorded restore command both embedded the runner's root.
        test "source proof evidence names paths under the root relatively" {
            let root = Feature163TestFixtures.createTempRoot "feature163-proof-paths"

            try
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore
                Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "FS.GG.UI.Controls", "1.0.0" ] |> ignore

                // Fails closed on the missing feed package, which is what keeps this test off `dotnet
                // restore` — the evidence is still written, and it is the evidence under test.
                let result = PackageFeed.runWorkflow (baseOptions root (Path.Combine(root, "feed")) outDir PackageFeed.Proof)
                Expect.equal result.Status PackageFeed.Failed "missing local package still fails closed"

                let markdown = File.ReadAllText(Path.Combine(outDir, "source-proof.md"))
                let json = File.ReadAllText(Path.Combine(outDir, "source-proof.json"))

                Expect.isFalse (markdown.Contains root) "no absolute path survives into source-proof.md"
                Expect.isFalse (json.Contains root) "no absolute path survives into source-proof.json"
                Expect.stringContains markdown "out/cache" "the package cache is named relative to the root"
                Expect.stringContains json "samples/Demo/Demo.fsproj" "the pin is named relative to the root"

                // A package SOURCE is not always a path: `AllowedSources` carries the feed directory
                // and the nuget.org URL in one list. Relativising a URL is not a no-op — it is
                // CORRUPTION. `Path.IsPathRooted "https://…"` is false, so the URL gets combined with
                // the root and normalised back down to `https:/api.nuget.org/…`, a source that does
                // not exist. The evidence would name it and nothing would notice.
                Expect.stringContains markdown "https://api.nuget.org/v3/index.json" "the nuget.org URL survives the markdown rules list intact"
                Expect.stringContains json "https://api.nuget.org/v3/index.json" "the nuget.org URL survives the json rules list intact"
                Expect.isFalse (markdown.Contains "https:/api.nuget.org") "the URL's scheme separator is not collapsed"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        // #686 — the converse, and the reason this is `evidencePath` rather than a bare `relativePath`.
        // The default feed lives OUTSIDE the repository (`~/.local/share/nuget-local`), and
        // `Path.GetRelativePath` would happily render it `../../../.local/share/nuget-local` — no less
        // machine-specific than the absolute form, only harder to read. Leave it alone.
        test "a feed outside the repository root is left absolute, not relativised into ../.." {
            let root = Feature163TestFixtures.createTempRoot "feature163-external-feed"
            let feed = Feature163TestFixtures.createTempRoot "feature163-external-feed-store"

            try
                let outDir = Path.Combine(root, "out")
                Feature163TestFixtures.writePackageProject root "src/Controls/Controls.fsproj" "FS.GG.UI.Controls" "1.0.0" |> ignore
                Feature163TestFixtures.writeSampleProject root "samples/Demo/Demo.fsproj" [ "FS.GG.UI.Controls", "1.0.0" ] |> ignore

                PackageFeed.runWorkflow (baseOptions root feed outDir PackageFeed.Check) |> ignore

                let versions = File.ReadAllText(Path.Combine(outDir, "package-versions.md"))

                Expect.stringContains versions feed "the out-of-tree feed keeps the only form it has"
                Expect.isFalse (versions.Contains "..") "an out-of-tree path is never walked out of the root"
            finally
                Feature163TestFixtures.deleteTempRoot root
                Feature163TestFixtures.deleteTempRoot feed
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
