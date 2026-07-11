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
//   G-GATE   — each package's pin and ref sit in ITS profile gate (#436). Core/Host reach the three
//              VIEWER profiles — every profile that opens a window, and so every profile that can make
//              a sound; Engine/Elmish stay on the two simulation profiles. Neither half leaks into a
//              headless-scene/governed scaffold, which launches no viewer.
//
//              All four were `game || sample-pack` until #436: #429 had given the Controls host family
//              an audio sink, but `app` referenced NO audio package and launched through a sinkless
//              overload, so the profile the seam was BUILT for could not reach it. Core/Host moved
//              because they are what the fs-gg-audio skill tells an author to `open` and what the
//              scaffold compiles (AudioCues.fs opens both) — a skill may not out-reach its packages
//              (#430, R-REACH), so the Core/Host gate is the same string the fs-gg-audio rows in
//              .template.config/template.json and generate-skill-manifest.fsx carry.

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

/// Issue #436: audio reaches every profile that opens a VIEWER WINDOW — app as well as
/// game/sample-pack — because #429 gave the Controls host family an audio sink and an `app` scaffold
/// that references no FS.GG.Audio package cannot reach it. Same literal gate the SkiaViewer/Controls
/// pins sit in, and the SAME STRING the fs-gg-audio rows in .template.config/template.json and
/// scripts/generate-skill-manifest.fsx carry — a skill may not out-reach the packages it says to
/// `open` (#430, R-REACH).
///
/// `headless-scene`/`governed` remain excluded: they launch no viewer, so they can make no sound.
let private viewerProfileGate =
    "profile == \"app\" || profile == \"sample-pack\" || profile == \"game\""

/// The gate EACH audio package must sit in — pin and reference alike. The component is split, and the
/// split is load-bearing, so this is a per-package expectation rather than one gate for all four:
///
///   * Core/Host reach the VIEWER profiles. They are what the skill actually tells an author to
///     `open` (Core for the cue values, Host for the `AssetResolver`) and what the scaffold compiles
///     on every windowed profile — `AudioCues.fs` opens both, `Program.fs` builds the device sink
///     from Host. R-REACH therefore requires them wherever fs-gg-audio materializes.
///
///   * Engine/Elmish stay on the SIMULATION profiles. The skill names them in prose and never says to
///     `open` them, no scaffold source references them, and this repo builds no project for
///     FS.GG.Audio — so putting them on `app` would also break the #366 offline probe, which can only
///     resolve the audio packages the framework itself already pulls into the global-packages folder
///     (SkiaViewer -> Core, SkiaViewer.Tests -> Host). Shipping a package a profile cannot restore to
///     satisfy a paragraph nobody compiles is the trade this split refuses.
let private expectedGate =
    Map [ "FS.GG.Audio.Core", viewerProfileGate
          "FS.GG.Audio.Host", viewerProfileGate
          "FS.GG.Audio.Engine", simProfileGate
          "FS.GG.Audio.Elmish", simProfileGate ]

/// The two hosts EvidenceCommands.fs defines. Both must route the initial model through the cue seam
/// (G-INIT) — `generatedHost` since #458, `interactiveHost` since #436.
let private cueSeamHosts = [ "generatedHost"; "interactiveHost" ]

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

          // G-GATE — each audio package sits in ITS gate, pin and reference alike (#436): Core/Host on
          // the viewer profiles (so the Controls family can reach #429's sink), Engine/Elmish on the
          // simulation profiles. Neither half can leak into a headless-scene/governed scaffold, which
          // launches no viewer.
          //
          // Pin and ref are checked against the SAME expectation on purpose: a package pinned on a
          // profile it is not referenced on is dead weight, and one referenced where it is not pinned
          // does not restore. Drifting them apart is how a profile ends up shipping a dependency it
          // never wires — which is exactly half of what #436 fixed (sample-pack referenced all four
          // audio packages and launched through a sinkless overload).
          test "every FS.GG.Audio pin and reference sits in its package's profile gate" {
              let props = File.ReadAllText propsPath
              let proj = File.ReadAllText projPath
              for pkg in audioPackages do
                  let gate = Map.find pkg expectedGate
                  Expect.equal (gateOf props pkg) (Some(Some gate)) $"{pkg} pin is gated to `{gate}`"
                  Expect.equal (gateOf proj pkg) (Some(Some gate)) $"{pkg} reference is gated to `{gate}`"
          }

          // G-INIT (issue #458) — the INITIAL state reaches the cue seam.
          //
          // `forTransition` is a function of a TRANSITION, and `initialModel` does not make one. So
          // while `Init` was `fun () -> initialModel, []`, ANY effect the initial state implied was
          // silently never emitted — and state that is LOADED rather than transitioned into (settings,
          // a save game, a resumed session) is exactly the state that enters through that door.
          //
          // The real assertion lives in the generated product's own tests, where the sink can be
          // observed (Product.Tests/BehaviorTests: `Init` hands out the `PlayAudio` batch). But those
          // run only in a SCAFFOLDED product, which is to say: not on a PR to this repo. So the payload
          // gets a guard here too, or the wiring can be deleted from the template and every gate in
          // this repo stays green — the #434 lesson, applied to the thing that fixes #458.
          test "BOTH hosts route the INITIAL model through the audio cue seam (#458, #436)" {
              let evidenceCommands = File.ReadAllText(repositoryPath "template/base/src/Product/EvidenceCommands.fs")
              let model = File.ReadAllText(repositoryPath "template/base/src/Product/Model.fs")
              let audioCues = File.ReadAllText(repositoryPath "template/base/src/Product/AudioCues.fs")

              /// The body of a top-level `let <name> ...` binding, up to the next column-0 `let`.
              let hostRegion (name: string) =
                  let start = evidenceCommands.IndexOf $"let {name}"
                  Expect.isGreaterThan start -1 $"EvidenceCommands.fs defines {name}"
                  let after = evidenceCommands.IndexOf("\nlet ", start + 1)
                  let stop = if after < 0 then evidenceCommands.Length else after
                  evidenceCommands.Substring(start, stop - start)

              // This assertion used to be scoped to `generatedHost` alone, because `interactiveHost`
              // (the app profile) still carried the effect-free `Init = fun () -> initialModel, []` —
              // correct only for as long as that profile compiled no AudioCues.fs and so had no cue
              // seam to miss. The note left here said whoever wired audio into the app profile had to
              // route this Init through the seam too, or #458 would simply reappear one profile over.
              //
              // #436 wired it. So the assertion is now over BOTH hosts: each one dispatches `Started`
              // through the SAME `forTransition` its Update calls — no separate startup cue path that
              // could drift out of sync, and no host left with a seam it silently bypasses at startup.
              for host in cueSeamHosts do
                  let region = hostRegion host

                  Expect.stringContains
                      region
                      "AppRoot.AudioCues.forTransition Started initialModel initialModel"
                      $"{host}.Init dispatches Started through the SAME cue seam Update uses — not a separate startup branch"

                  Expect.isFalse
                      (region.Contains "Init = fun () -> initialModel, []")
                      $"{host}.Init must not go back to producing the initial model with no effects (the #458 hole)"

                  // The seam is only wired if Update carries the cues out too — an Init-only wiring
                  // would emit the startup cue and then go silent for every subsequent transition.
                  Expect.stringContains
                      region
                      "AppRoot.AudioCues.forTransition msg model next"
                      $"{host}.Update lifts each transition's cues onto PlayAudio"

              Expect.stringContains model "| Started" "the starter Msg declares Started"
              Expect.stringContains audioCues "| Started ->" "AudioCues.forTransition handles Started"

              // A `Started` case that returns [] makes the product-side test vacuous: it would pass
              // whether or not Init is wired to the seam. The scaffold must ship it emitting something
              // — and in EVERY starter's cue map, since #436 gave the file one `forTransition` per
              // starter (the two profiles ship different `Msg` types).
              //
              // Comments are stripped FIRST. AudioCues.fs documents the seam with a worked example of a
              // `Started` cue (`| Started -> [ Audio.setMasterVolume ... ]`), so matching the raw file
              // counts prose as wiring. That is not hypothetical: this assertion used to take the FIRST
              // regex match, and in the shipped file that match was the comment — the gate was reading
              // documentation and calling it code.
              let audioCuesCode =
                  audioCues.Replace("\r\n", "\n").Split('\n')
                  |> Array.filter (fun line -> not ((line.TrimStart()).StartsWith "//"))
                  |> String.concat "\n"

              let startedCues =
                  Regex.Matches(audioCuesCode, @"\|\s*Started\s*->\s*\[(?<cues>[^\]]*)\]")

              Expect.equal
                  startedCues.Count
                  2
                  "AudioCues ships a wired `Started` cue for BOTH starters (the game's and the Controls one)"

              for startedCue in startedCues do
                  Expect.isFalse
                      (String.IsNullOrWhiteSpace startedCue.Groups.["cues"].Value)
                      "the scaffold ships `Started` wired to a real cue — an empty one makes the product's own regression test vacuous"
          }
        ]
