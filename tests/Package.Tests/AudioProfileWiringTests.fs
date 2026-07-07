module AudioProfileWiringTests

// ADR-0024 — wire the standalone FS.GG.Audio component into the game/sample-pack scaffold profiles.
//
// FS.GG.Audio is its OWN repo and its OWN versioned component (the 7th FS-GG repo; registered in
// .github registry/dependencies.yml, contract fs-gg-audio). The Rendering template is only the FIRST
// CONSUMER EDGE of that package contract — so this gate asserts the WIRING SHAPE, nothing about audio's
// own surface or its version-of-truth:
//   * audio's public API surface is verified in the FS.GG.Audio repo — NOT bundled as api-surface .fsi
//     here (contrast Feature240, which bundles Game.Core .fsi because the fs-gg-game-core skill cites
//     those members). The record-only fs-gg-audio skill cites the pure AudioEffect edge in
//     FS.GG.UI.Canvas (an in-repo file), not the standalone packages.
//   * WHETHER 0.1.0-preview.1 is the coherent pin is the registry consumer-edge's job (.github#238),
//     not Rendering's — so no version-VALUE assertion lives here.
//
// What a Rendering consumer edge DOES owe (mirrors the FS.GG.Game.Core precedent, ADR-0022 P5):
//   G-AXIS   — the pins derive through a DISTINCT $(FsGgAudioVersion) axis (its own component/release
//              cadence), never $(FsGgUiVersion)/$(FsGgGameVersion).
//   G-PINS   — all four FS.GG.Audio.* packages are pinned in Directory.Packages.props through that axis.
//   G-REFS   — the product references all four.
//   G-GATE   — both the pins and the refs sit inside the `profile == "game" || sample-pack` gate (the
//              real host-side audio realization ships on the simulation profiles only), so they can
//              never leak into an app/none/governed scaffold.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private propsPath = repositoryPath "template/base/Directory.Packages.props"
let private projPath = repositoryPath "template/base/src/Product/Product.fsproj"

/// The four packages of the standalone FS.GG.Audio component (Core/Host/Engine/Elmish).
let private audioPackages =
    [ "FS.GG.Audio.Core"; "FS.GG.Audio.Host"; "FS.GG.Audio.Engine"; "FS.GG.Audio.Elmish" ]

/// The `dotnet new` gate that opens the region a line sits in, or None if the line is ungated.
/// Walks upward: the nearest `<!--#if ... -->` not already closed by an intervening `<!--#endif -->`.
let private enclosingGate (lines: string[]) (lineIndex: int) =
    let ifRegex = Regex(@"<!--#if\s+\((?<cond>.*?)\)\s*-->")
    let rec walk i depth =
        if i < 0 then None
        elif lines.[i].Contains "<!--#endif" then walk (i - 1) (depth + 1)
        else
            let m = ifRegex.Match lines.[i]
            if m.Success then
                if depth = 0 then Some(m.Groups.["cond"].Value.Trim()) else walk (i - 1) (depth - 1)
            else walk (i - 1) depth
    walk lineIndex 0

/// The gate a given package's declaration line sits in, within `text`.
let private gateOf (text: string) (packageId: string) =
    let lines = text.Replace("\r\n", "\n").Split('\n')
    match lines |> Array.tryFindIndex (fun l -> l.Contains($"Include=\"{packageId}\"")) with
    | Some idx -> Some(enclosingGate lines idx)
    | None -> None

let private simProfileGate = "profile == \"game\" || profile == \"sample-pack\""

[<Tests>]
let audioProfileWiringTests =
    testList
        "Audio profile wiring (ADR-0024, first consumer edge of fs-gg-audio)"
        [
          // G-AXIS — FS.GG.Audio is its own component: a single distinct version axis, a bare literal,
          // decoupled from the UI and Game.Core axes (it releases on its own cadence).
          test "FS.GG.Audio pins derive through a distinct $(FsGgAudioVersion) axis, not the UI/Game axis" {
              let props = File.ReadAllText propsPath
              let axis = Regex.Matches(props, "<FsGgAudioVersion>([^<]+)</FsGgAudioVersion>")
              Expect.equal axis.Count 1 "exactly one <FsGgAudioVersion> axis literal"
              let value = axis.[0].Groups.[1].Value.Trim()
              Expect.isFalse (value.Contains "$(") $"the audio axis is a bare literal, not derived (was '{value}')"
          }

          // G-PINS — every FS.GG.Audio.* package is pinned, and pinned THROUGH the audio axis (never a
          // hardcoded literal and never $(FsGgUiVersion)/$(FsGgGameVersion) — that would recouple the
          // standalone component to another repo's release).
          test "all four FS.GG.Audio.* packages are pinned through $(FsGgAudioVersion)" {
              let props = File.ReadAllText propsPath
              for pkg in audioPackages do
                  let m = Regex.Match(props, $"<PackageVersion\\s+Include=\"{Regex.Escape pkg}\"\\s+Version=\"([^\"]+)\"")
                  Expect.isTrue m.Success $"{pkg} is pinned in Directory.Packages.props"
                  Expect.equal (m.Groups.[1].Value) "$(FsGgAudioVersion)" $"{pkg} derives through the audio axis, not $(FsGgUiVersion)/$(FsGgGameVersion) or a literal"
          }

          // G-REFS — the product references all four so a game/sample-pack product compiles the real
          // host-side realization (buses/fades/3D + device backend) behind the pure AudioEffect edge.
          test "the product references all four FS.GG.Audio.* packages" {
              let proj = File.ReadAllText projPath
              for pkg in audioPackages do
                  Expect.stringContains proj $"Include=\"{pkg}\"" $"Product.fsproj references {pkg}"
          }

          // G-GATE — the real audio realization ships on the simulation profiles only: every pin and
          // every ref sits inside the `game || sample-pack` gate, so it cannot leak into app/none/governed.
          test "every FS.GG.Audio pin and reference is gated to the game/sample-pack profiles only" {
              let props = File.ReadAllText propsPath
              let proj = File.ReadAllText projPath
              for pkg in audioPackages do
                  Expect.equal (gateOf props pkg) (Some(Some simProfileGate)) $"{pkg} pin is gated to the sim profiles only"
                  Expect.equal (gateOf proj pkg) (Some(Some simProfileGate)) $"{pkg} reference is gated to the sim profiles only"
          }
        ]
