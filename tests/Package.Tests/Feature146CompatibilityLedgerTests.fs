module Feature146CompatibilityLedgerTests

open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let rec private findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || Directory.GetFiles(directory, "*.slnx").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let private repositoryRoot = findRepositoryRoot System.AppContext.BaseDirectory
let private path (relative: string) = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let feature146CompatibilityLedgerTests =
    testList "Feature146 compatibility ledger and package surface" [
        test "SceneCodec public contract files are present" {
            let sceneCodecFsi = File.ReadAllText(path "src/Scene/SceneCodec.fsi")
            let referenceFsi = File.ReadAllText(path "src/SkiaViewer/ReferenceRendering.fsi")
            let testingFsi = File.ReadAllText(path "src/Testing/Testing.fsi")

            Expect.stringContains sceneCodecFsi "module SceneCodec" "SceneCodec module is public"
            Expect.stringContains sceneCodecFsi "val exportScene" "SceneCodec export is public"
            Expect.stringContains sceneCodecFsi "val inspectWith" "SceneCodec inspection is public"
            Expect.stringContains referenceFsi "module ReferenceRendering" "ReferenceRendering module is public"
            Expect.stringContains referenceFsi "type ReferenceRenderingEffect" "ReferenceRendering effect surface is public"
            Expect.stringContains testingFsi "module PackageInspectionAssertions" "Testing package inspection helpers are public"
        }

        test "package inspection assertion helper accepts expected report" {
            let package = SceneCodec.export (Scene.rectangle (0.0, 0.0, 8.0, 8.0) Colors.white)
            let report = SceneCodec.inspect package.CanonicalBytes

            let result =
                PackageInspectionAssertions.validate
                    { Report = report
                      ExpectedStatus = PackageAccepted
                      RequiredDiagnosticFragments = [] }

            Expect.isTrue result.Accepted (String.concat "; " result.Diagnostics)
        }

        test "compatibility ledger names Feature146 surfaces and evidence links" {
            let ledgerPath = path "specs/146-render-anywhere-protocol/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists ledgerPath) "compatibility ledger exists"

            let ledger = File.ReadAllText ledgerPath
            Expect.stringContains ledger "SceneCodec" "ledger names SceneCodec"
            Expect.stringContains ledger "ReferenceRendering" "ledger names ReferenceRendering"
            Expect.stringContains ledger "browser-feasibility" "ledger links browser feasibility evidence"
        }
    ]
