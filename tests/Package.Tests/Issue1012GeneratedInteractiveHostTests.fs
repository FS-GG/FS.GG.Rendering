module Issue1012GeneratedInteractiveHostTests

open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private read (relative: string) = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))

[<Tests>]
let generatedInteractiveHostContract =
    testList "issue-1012 generated interactive host contract" [
        test "template host keeps geometry, both key edges, held ticks, and runtime settings on their owning seams" {
            let host = read "template/base/src/Product/EvidenceCommands.fs"
            let program = read "template/base/src/Product/Program.fs"

            [ "InitialSize = { Width = 1280; Height = 720 }"
              "RawKeyChanged of key: KeyId * isDown: bool"
              "Some(RawKeyChanged(ViewerKeyboard.toKeyId key, isDown))"
              "HeldKeys: Set<KeyId>"
              "Environment.SpecialFolder.LocalApplicationData"
              "legacyShellSettingsPath = \"readiness/game-shell-settings.json\""
              "if persistShellSettings migrated then" ]
            |> List.iter (fun token -> Expect.stringContains host token $"generated host carries `{token}`")

            Expect.stringContains program "shellConfig.InitialDisplay" "default launch derives behavior from the authored shell display"
            Expect.stringContains program "runInteractiveAppWithWindowBehaviorAndAudio viewerOptions launchRequest" "default and flagged game launches select one explicit overload"
        }

        test "generated-product behavior proves retained pointer and down-ticks-up through InteractiveAppHost" {
            let behavior = read "template/base/tests/Product.Tests/BehaviorTests.fs"

            [ "captureRespondsProof"
              "clickWithProof \"start\""
              "clickWithProof rebindId"
              "raw (Letter 'Q') true"
              "raw (Letter 'W') true"
              "afterTick1"
              "afterTick2"
              "raw (Letter 'W') false"
              "afterReleaseTick" ]
            |> List.iter (fun token -> Expect.stringContains behavior token $"generated test carries `{token}`")
        }

        test "release lane instantiates the packed game profile and runs its emitted tests" {
            let release = read ".github/workflows/release.yml"
            let gameStart = release.IndexOf("- name: Instantiate + build the game profile", System.StringComparison.Ordinal)
            Expect.isGreaterThanOrEqual gameStart 0 "release workflow has the generated game-product lane"
            let block = release.Substring(gameStart, min 5000 (release.Length - gameStart))
            Expect.stringContains block "dotnet new fs-gg-ui --name GameProduct --profile game" "release lane instantiates the packed game profile"
            Expect.stringContains block "dotnet test \"$work/GameProduct\"" "release lane runs the emitted host acceptance tests"
        }
    ]
