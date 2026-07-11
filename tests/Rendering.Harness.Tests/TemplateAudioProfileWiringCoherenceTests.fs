module TemplateAudioProfileWiringCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only audio-profile wiring gate.
//
// WHY THIS EXISTS. The ADR-0024 audio consumer edge — the game/sample-pack scaffold profiles wiring
// in the standalone FS.GG.Audio component — is asserted by two release-only checks and nothing at
// PR time:
//   * `AudioProfileWiringTests` (tests/Package.Tests) reads the template text and asserts the WIRING
//     SHAPE (axis / pins / refs / gate). Package.Tests is RELEASE-ONLY (not in FS.GG.Rendering.slnx),
//     so a PR that ungates an audio pin, drops a ref, or recouples the axis compiles green and only
//     reds the release lane.
//   * release.yml's "Instantiate + build the game profile" step then proves the pins RESOLVE and the
//     four assemblies land — that half genuinely needs instantiation + a feed and stays release-only.
//
// This is the same "PR-gated tests must be in the slnx" gap #350 closed for the generated product's
// launch host (see TemplateLaunchExpressionCoherenceTests). This test hoists the STATIC half — the
// text-only wiring-shape coherence — one gate earlier: it reads the same two template files
// STATICALLY (no instantiation, so it is cheap enough for the PR-gated slnx lane) and asserts the
// same G-AXIS / G-PINS / G-REFS / G-GATE invariants AudioProfileWiringTests does. Change the template
// audio wiring without keeping it coherent and this reds the PR instead of the release.
//
// Kept deliberately in lockstep with tests/Package.Tests/AudioProfileWiringTests.fs: the two read the
// same source-of-truth template files, so a real drift fails BOTH — the release check is not weakened,
// only mirrored earlier. The resolve-and-land proof (network + build) is out of scope here by design.

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
/// Mirrors AudioProfileWiringTests.enclosingGate so the two checks classify gates identically.
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
let templateAudioProfileWiringCoherenceTests =
    testList
        "#366 — audio profile wiring coherence (PR-time twin of the release-only ADR-0024 gate)"
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

          // G-PINS — every FS.GG.Audio.* package is pinned THROUGH the audio axis (never a hardcoded
          // literal and never $(FsGgUiVersion)/$(FsGgGameVersion), which would recouple the component).
          test "all four FS.GG.Audio.* packages are pinned through $(FsGgAudioVersion)" {
              let props = File.ReadAllText propsPath
              for pkg in audioPackages do
                  let m = Regex.Match(props, $"<PackageVersion\\s+Include=\"{Regex.Escape pkg}\"\\s+Version=\"([^\"]+)\"")
                  Expect.isTrue m.Success $"{pkg} is pinned in Directory.Packages.props"
                  Expect.equal (m.Groups.[1].Value) "$(FsGgAudioVersion)" $"{pkg} derives through the audio axis, not $(FsGgUiVersion)/$(FsGgGameVersion) or a literal"
          }

          // G-REFS — the product references all four so a game/sample-pack product compiles the real
          // host-side realization behind the pure AudioEffect edge.
          test "the product references all four FS.GG.Audio.* packages" {
              let proj = File.ReadAllText projPath
              for pkg in audioPackages do
                  Expect.stringContains proj $"Include=\"{pkg}\"" $"Product.fsproj references {pkg}"
          }

          // G-GATE — every pin and every ref sits inside the `game || sample-pack` gate, so the audio
          // realization can never leak into an app/none/governed scaffold.
          test "every FS.GG.Audio pin and reference is gated to the game/sample-pack profiles only" {
              let props = File.ReadAllText propsPath
              let proj = File.ReadAllText projPath
              for pkg in audioPackages do
                  Expect.equal (gateOf props pkg) (Some(Some simProfileGate)) $"{pkg} pin is gated to the sim profiles only"
                  Expect.equal (gateOf proj pkg) (Some(Some simProfileGate)) $"{pkg} reference is gated to the sim profiles only"
          }

          // G-INIT (issue #458) — the INITIAL state reaches the cue seam. The PR-time half of the
          // rule its release-only twin asserts, hoisted here for the reason this whole file exists:
          // Package.Tests is not in the slnx, so a PR that deletes the Init wiring would compile
          // green and red only the release lane. That would be a gate for the #458 fix that does not
          // gate — which is the same class of failure #458 itself is about.
          //
          // `forTransition` is a function of a TRANSITION, and `initialModel` does not make one. While
          // `Init` was `fun () -> initialModel, []`, any effect the initial state implied was silently
          // never emitted — and state that is LOADED rather than transitioned into (settings, a save
          // game, a resumed session) is exactly the state that enters through that door.
          test "the game host routes the INITIAL model through the audio cue seam (#458)" {
              let evidenceCommands = File.ReadAllText(repositoryPath "template/base/src/Product/EvidenceCommands.fs")
              let model = File.ReadAllText(repositoryPath "template/base/src/Product/Model.fs")
              let audioCues = File.ReadAllText(repositoryPath "template/base/src/Product/AudioCues.fs")

              Expect.stringContains
                  evidenceCommands
                  "AppRoot.AudioCues.forTransition Started initialModel initialModel"
                  "generatedHost.Init dispatches Started through the SAME cue seam Update uses — not a separate startup branch"

              // Scoped to `generatedHost`. `interactiveHost` (app profile) still has the effect-free
              // Init, correct TODAY only because that profile compiles no AudioCues.fs and so has no
              // seam to miss. #429 gave the interactive host an audio sink and #436 wires audio into
              // the app profile — whoever lands that must route this Init through the seam too, and
              // widen this assertion, or #458 reappears one profile over.
              let generatedHostRegion =
                  let start = evidenceCommands.IndexOf "let generatedHost"
                  Expect.isGreaterThan start -1 "EvidenceCommands.fs defines generatedHost"
                  let after = evidenceCommands.IndexOf("\nlet ", start + 1)
                  let stop = if after < 0 then evidenceCommands.Length else after
                  evidenceCommands.Substring(start, stop - start)

              Expect.isFalse
                  (generatedHostRegion.Contains "Init = fun () -> initialModel, []")
                  "generatedHost.Init must not go back to producing the initial model with no effects (the #458 hole)"

              Expect.stringContains model "| Started" "the starter Msg declares Started"
              Expect.stringContains audioCues "| Started ->" "AudioCues.forTransition handles Started"

              // A `Started` case returning [] makes the generated product's own regression test
              // vacuous — it would pass whether or not Init is wired to the seam at all, which is how
              // this class survives its own fix (#266). The scaffold must ship it emitting something.
              let startedCue = Regex.Match(audioCues, @"\|\s*Started\s*->\s*\[(?<cues>[^\]]*)\]")

              Expect.isTrue startedCue.Success "AudioCues ships a `Started` cue list"

              Expect.isFalse
                  (System.String.IsNullOrWhiteSpace startedCue.Groups.["cues"].Value)
                  "the scaffold ships `Started` wired to a real cue — an empty one makes the product's own regression test vacuous"
          }
        ]
