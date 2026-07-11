module AudioProfileWiringTests

// ADR-0024 — wire the standalone FS.GG.Audio component into the game/sample-pack scaffold profiles.
//
// FS.GG.Audio is its OWN repo and its OWN versioned component (the 7th FS-GG repo; registered in
// .github registry/dependencies.yml, contract fs-gg-audio). The Rendering template is only the FIRST
// CONSUMER EDGE of that package contract — so this gate asserts the WIRING SHAPE, nothing about audio's
// own surface or its version-of-truth:
//   * audio's public API surface is VERIFIED in the FS.GG.Audio repo, never here. It is nonetheless
//     BUNDLED here as api-surface .fsi (#247): a package `Product.fsproj` references but ships no
//     surface for is undiscoverable to the product author who reads `docs/api-surface/`. ADR-0024
//     originally withheld the bundle because a doc copy can outlive the package it claims; that
//     objection is now ENFORCED rather than avoided — see M-PROV in ApiSurfaceMirrorTests, which
//     fails when a mirror's stamped version drifts from the pinned $(FsGgAudioVersion).
//     (Also stale as written: since #161 the fs-gg-audio skill cites FS.GG.Audio.Core, not the
//     retired FS.GG.UI.Canvas edge — ADR-0024/#158 removed that file.)
//   * WHETHER 0.1.0-preview.1 is the coherent pin is the registry consumer-edge's job (.github#238),
//     not Rendering's — so no version-VALUE assertion lives here. M-PROV asserts only that the
//     bundled copy and the pin AGREE, not that either is the coherent value.
//
// What a Rendering consumer edge DOES owe (mirrors the FS.GG.Game.Core precedent, ADR-0022 P5):
//   G-AXIS   — the pins derive through a DISTINCT $(FsGgAudioVersion) axis (its own component/release
//              cadence), never $(FsGgUiVersion)/$(FsGgGameVersion).
//   G-PINS   — all four FS.GG.Audio.* packages are pinned in Directory.Packages.props through that axis.
//   G-REFS   — the product references all four.
//   G-GATE   — each pin/ref sits inside the gate for the profiles that can actually use it, and never
//              reaches `governed`/`headless-scene`. Two gates, not one (#436): Core+Host (request
//              vocabulary + device seam) ship to app/game/sample-pack, while Engine+Elmish
//              (buses/3D, the Audio.Cmd MVU bridge) stay simulation-only.
//   G-WIRE    — every profile that REFERENCES audio also WIRES it: a real sink at the launch site and a
//              cue map compiled. Referencing without wiring is the #436 bug, and it shipped on
//              `sample-pack` for a full release cycle precisely because this suite proved refs and
//              never proved wiring — the packages were there, the sound could not be.

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

let private programPath = repositoryPath "template/base/src/Product/Program.fs"

/// Engine (buses/fades/ducking/3D) and Elmish (the Audio.Cmd bridge) are SIMULATION-shaped.
let private simProfileGate = "profile == \"game\" || profile == \"sample-pack\""
let private simOnlyPackages = [ "FS.GG.Audio.Engine"; "FS.GG.Audio.Elmish" ]

/// Core (the pure AudioEffect vocabulary) and Host (the device seam) are what ANY product needs to
/// request a sound and have it realized — including a controls app with a start screen and a volume
/// slider. Issue #436 put `app` on this gate; #429 is what made it audible (`runInteractiveAppWithAudio`).
let private soundCapableGate = "profile == \"app\" || profile == \"game\" || profile == \"sample-pack\""
let private soundCapablePackages = [ "FS.GG.Audio.Core"; "FS.GG.Audio.Host" ]

/// The launchers that take NO audio sink. `runInteractiveApp` silently discarded every `PlayAudio`
/// before #429, and `Viewer.runApp` still does — so a profile that references FS.GG.Audio and then
/// launches through one of these ships the dependency while being structurally incapable of sound.
/// That is exactly what `sample-pack` did (#436): four audio packages, no cue map, sinkless launch.
/// `(?![A-Za-z])` keeps `runInteractiveApp` from matching `runInteractiveAppWithAudio`.
let private sinklessLaunchers =
    [ @"ControlsElmish\.runInteractiveApp(?![A-Za-z])", "ControlsElmish.runInteractiveApp"
      @"ControlsElmish\.runInteractiveAppWithWindowBehavior(?![A-Za-z])", "ControlsElmish.runInteractiveAppWithWindowBehavior"
      @"Viewer\.runApp(?![A-Za-z])", "Viewer.runApp"
      @"Viewer\.runAppWithWindowBehavior(?![A-Za-z])", "Viewer.runAppWithWindowBehavior" ]

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

          // G-GATE — audio ships on the profiles that can make a sound, and NOWHERE else. Two gates,
          // because the packages are not one thing (#436): Core+Host are the request vocabulary and the
          // device seam, which a controls `app` needs as much as a game does; Engine+Elmish are
          // simulation-shaped (buses/3D, the Audio.Cmd MVU bridge) and stay on game/sample-pack. Neither
          // gate may reach `governed` or `headless-scene` — those profiles have no host and no sound.
          test "every FS.GG.Audio pin and reference is gated to the profiles that can use it" {
              let props = File.ReadAllText propsPath
              let proj = File.ReadAllText projPath
              for pkg in soundCapablePackages do
                  Expect.equal (gateOf props pkg) (Some(Some soundCapableGate)) $"{pkg} pin is gated to the sound-capable profiles (app/game/sample-pack)"
                  Expect.equal (gateOf proj pkg) (Some(Some soundCapableGate)) $"{pkg} reference is gated to the sound-capable profiles (app/game/sample-pack)"
              for pkg in simOnlyPackages do
                  Expect.equal (gateOf props pkg) (Some(Some simProfileGate)) $"{pkg} pin stays on the simulation profiles"
                  Expect.equal (gateOf proj pkg) (Some(Some simProfileGate)) $"{pkg} reference stays on the simulation profiles"
              for pkg in audioPackages do
                  for gate in [ gateOf props pkg; gateOf proj pkg ] do
                      match gate with
                      | Some(Some g) ->
                          Expect.isFalse (g.Contains "governed") $"{pkg} must never reach the governed profile (gate was '{g}')"
                          Expect.isFalse (g.Contains "headless-scene") $"{pkg} must never reach headless-scene (gate was '{g}')"
                      | _ -> failtestf "%s is not inside a profile gate at all" pkg
          }

          // G-WIRE — the assertion this suite was missing, and the one that would have caught #436.
          //
          // Everything above proves the packages are REFERENCED. Nothing proved they were WIRED, and the
          // gap was real on two profiles at once: `app` referenced no audio package and launched through
          // the sinkless `runInteractiveApp` (so #429's seam was unreachable from a generated product),
          // while `sample-pack` referenced ALL FOUR packages, compiled no cue map, and launched through
          // the sinkless `Viewer.runApp` — it shipped the dependency and could not make a sound.
          //
          // `governed`/`headless-scene` return long before this launch block, so every profile that
          // reaches it is a sound-capable one. Hence the invariant is flat and checkable: Program.fs must
          // contain NO sinkless launcher at all. A future profile that wants silence must say so by not
          // referencing audio, not by quietly dropping the sink.
          test "no sound-capable profile launches through a sinkless entry point" {
              let program = File.ReadAllText programPath
              for pattern, name in sinklessLaunchers do
                  Expect.isFalse
                      (Regex.IsMatch(program, pattern))
                      $"Program.fs still launches via {name}, which takes no audio sink — every PlayAudio the product emits is silently discarded (#429/#436). Use the ...WithAudio sibling."
              // ...and the audio-capable siblings ARE the ones in use, so the check above cannot pass
              // vacuously by the launch block having been renamed or deleted.
              for launcher in
                  [ "ControlsElmish.runInteractiveAppWithAudio"
                    "ControlsElmish.runInteractiveAppWithWindowBehaviorAndAudio"
                    "Viewer.runAppWithAudio"
                    "Viewer.runAppWithWindowBehaviorAndAudio" ] do
                  Expect.stringContains program launcher $"Program.fs launches through {launcher}"
          }

          // The sink needs a cue map to have anything to say: AudioCues.fs must compile on every profile
          // that references audio. It was `game` only, which is why `sample-pack` had packages and silence.
          test "the cue map compiles on every sound-capable profile" {
              let proj = File.ReadAllText projPath
              Expect.equal
                  (gateOf proj "AudioCues.fs")
                  (Some(Some soundCapableGate))
                  "AudioCues.fs compiles on app/game/sample-pack — every profile that references FS.GG.Audio"
          }
        ]
