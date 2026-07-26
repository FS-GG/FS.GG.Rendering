module Issue1071VisualCoverageTests

open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private read (path: string) =
    File.ReadAllText(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)))

[<Tests>]
let visualCoverageTests =
    testList "Issue1071 production-bound visual coverage" [
        test "the product owns an independent typed inventory and the real view consumes its projection" {
            let inventory = read "template/base/src/Product/GameplayVisualInventory.fs"
            let project = read "template/base/src/Product/Product.fsproj"
            let view = read "template/base/src/Product/View.fs"

            Expect.stringContains inventory "type GameplayVisualElement" "inventory is typed production source"
            Expect.stringContains inventory "let registeredBindings" "product owns an element-bound registry"
            Expect.stringContains inventory "FSharpType.GetUnionCases" "DU enumeration cannot silently omit a new gameplay case"
            Expect.stringContains inventory "RequiredStates" "each binding owns named per-element state evidence"
            Expect.stringContains inventory "let project (model: Model)" "product owns the state-to-scene projection"
            Expect.stringContains project "<Compile Include=\"GameplayVisualInventory.fs\" />" "inventory compiles into production"
            Expect.stringContains view "GameplayVisualInventory.project model" "the real view consumes the audited projection"
        }

        test "the generated gate starts from production inventory and checks registry plus observed production handles" {
            let gate = read "template/base/tests/Product.Tests/CoverageGateTests.fs"

            Expect.stringContains gate "GameplayVisualInventory.all" "catalog rows do not supply the subject set"
            Expect.stringContains gate "GameplayVisualInventory.registeredBindings" "shown handles resolve as exact element/handle pairs"
            Expect.stringContains gate "binding.RequiredStates" "observations use per-element required runtime states"
            Expect.stringContains gate "View.view model" "evidence traverses the real view"
            Expect.stringContains gate "binding.Project model |> SceneCodec.export" "distinctness is measured per element, not only on aggregate frames"
            Expect.stringContains gate "elementBaselines" "byte-identical visuals across distinct elements fail"
            Expect.stringContains gate "Catalog.audit" "the framework audit is the mechanical oracle"
            Expect.stringContains gate "Catalog.BindingGap.Missing" "missing inventory rows are regression tested"
            Expect.stringContains gate "Catalog.BindingGap.Unbound" "orphan handles are regression tested"
            Expect.stringContains gate "if List.isEmpty scene.Nodes" "empty runtime scenes cannot count as observations"
            Expect.stringContains gate "Catalog.BindingGap.Stale" "stale starter rows are regression tested"
        }

        test "Rogue2-derived door/trapdoor regressions are durable" {
            let gate = read "template/base/tests/Product.Tests/CoverageGateTests.fs"

            for required in [ "DoorOpen"; "DoorLocked"; "Trapdoor"; "Ball"; "LeftPaddle" ] do
                Expect.stringContains gate required $"fixture carries {required}"

            Expect.stringContains gate "List.distinct sceneDigests" "fixture proves required Rogue states are distinct"
        }

        test "both product skills require production audit plus an external exact-revision critic" {
            for path in
                [ "template/product-skills/fs-gg-symbol-design/SKILL.md"
                  "template/product-skills/fs-gg-symbology/SKILL.md" ] do
                let skill = read path
                Expect.stringContains skill "Catalog.audit" $"{path} starts from the mechanical audit"
                Expect.stringContains skill "fresh-context" $"{path} requires an independent cold read"
                Expect.stringContains skill "exact commit" $"{path} binds the critic to the reviewed revision"
                Expect.stringContains skill "outside the authored tree" $"{path} requires externally verifiable provenance"
        }
    ]
