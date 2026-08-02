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

            Expect.stringContains program "let launchRequest = AppRoot.WindowOptions.toViewerLaunchRequest windowBehavior" "default and flagged launch behavior comes from the same parsed safe-default overlay"
            Expect.stringContains program "runInteractiveAppWithWindowBehaviorAndAudio viewerOptions launchRequest" "default and flagged game launches select one explicit overload"
            Expect.stringContains host "ApplyLogicalCanvas(AppRoot.GameShell.logicalSize settings)" "DisplayChanged reaches the dynamic viewer-owned logical canvas"
            Expect.stringContains host "LogicalSize = Some { Width = 1280; Height = 720 }" "the generated game seeds the initial logical canvas"
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

            [ "SetResolution resolution1080"
              "ApplyLogicalCanvas size"
              "LogicalCanvas.toLogicalPoint"
              "corresponding physical point still activates Config"
              "Live.runScriptWithWindowBehavior"
              "Runtime window behavior applied: mode=fullscreen" ]
            |> List.iter (fun token -> Expect.stringContains behavior token $"generated resolution proof carries `{token}`")
        }

        test "release lane instantiates the packed game profile and runs its emitted tests" {
            let release = read ".github/workflows/release.yml"
            let gameStart = release.IndexOf("- name: Instantiate + build the game profile", System.StringComparison.Ordinal)
            Expect.isGreaterThanOrEqual gameStart 0 "release workflow has the generated game-product lane"
            let block = release.Substring(gameStart, min 5000 (release.Length - gameStart))
            Expect.stringContains block "dotnet new fs-gg-ui --name GameProduct --profile game" "release lane instantiates the packed game profile"
            Expect.stringContains block "dotnet test \"$work/GameProduct\"" "release lane runs the emitted host acceptance tests"
        }

        test "packed-template pre-publish probe restores the staged framework set" {
            let release = read ".github/workflows/release.yml"
            let probeStart = release.IndexOf("- name: Packed template clean-checkout FSI contract", System.StringComparison.Ordinal)
            Expect.isGreaterThanOrEqual probeStart 0 "release workflow has the packed-template probe"
            let block = release.Substring(probeStart, min 5000 (release.Length - probeStart))
            let source = block.IndexOf("dotnet nuget add source \"$(pwd)/artifacts/packages\"", System.StringComparison.Ordinal)
            let scaffold = block.IndexOf("dotnet new fs-gg-ui --name \"$name\"", System.StringComparison.Ordinal)
            Expect.isGreaterThanOrEqual source 0 "the unpublished coherent set is exposed as a restore source"
            Expect.isGreaterThan scaffold source "the staging source is configured before the generated product restores"
            Expect.stringContains block "--configfile \"$HOME/.nuget/NuGet/NuGet.Config\"" "the /tmp product can see the staging source"
        }
    ]
