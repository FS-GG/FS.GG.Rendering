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

/// Issue #436: Core/Host reach every profile that opens a VIEWER WINDOW — app as well as
/// game/sample-pack.
let private viewerProfileGate =
    "profile == \"app\" || profile == \"sample-pack\" || profile == \"game\""

/// The gate each audio package's pin and reference must sit in. Kept byte-identical to
/// AudioProfileWiringTests.expectedGate: these two files are deliberate twins, and a gate that drifted
/// between them would let the PR lane and the release lane disagree about what the template may say.
///
/// Core/Host follow the fs-gg-audio skill onto the viewer profiles — they are what it tells an author
/// to `open`, and what the scaffold's AudioCues.fs/Program.fs compile (#430 R-REACH). Engine/Elmish
/// are prose-only in the skill, referenced by no scaffold source, and unresolvable by the #366 offline
/// probe (this repo builds no FS.GG.Audio project; only Core/Host are in the global-packages folder,
/// because the framework itself depends on them) — so they stay on the simulation profiles.
let private expectedGate =
    Map [ "FS.GG.Audio.Core", viewerProfileGate
          "FS.GG.Audio.Host", viewerProfileGate
          "FS.GG.Audio.Engine", simProfileGate
          "FS.GG.Audio.Elmish", simProfileGate ]

/// Both hosts EvidenceCommands.fs defines must route the initial model through the cue seam —
/// `generatedHost` since #458, `interactiveHost` since #436.
let private cueSeamHosts = [ "generatedHost"; "interactiveHost" ]

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

          // G-GATE — each audio package's pin and ref sit in ITS profile gate (#436): Core/Host on the
          // viewer profiles, so audio reaches the Controls family; Engine/Elmish on the simulation
          // profiles. Neither half can leak into a headless-scene/governed scaffold, which launches no
          // viewer and can make no sound. Pin and ref are held to the SAME expectation: a package
          // pinned where it is not referenced is dead weight, and referenced where it is not pinned
          // does not restore.
          test "every FS.GG.Audio pin and reference sits in its package's profile gate" {
              let props = File.ReadAllText propsPath
              let proj = File.ReadAllText projPath
              for pkg in audioPackages do
                  let gate = Map.find pkg expectedGate
                  Expect.equal (gateOf props pkg) (Some(Some gate)) $"{pkg} pin is gated to `{gate}`"
                  Expect.equal (gateOf proj pkg) (Some(Some gate)) $"{pkg} reference is gated to `{gate}`"
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

              // Was scoped to `generatedHost` while `interactiveHost` (app) still carried the
              // effect-free Init — correct only for as long as that profile compiled no AudioCues.fs
              // and so had no seam to miss. #436 gave it one, so the assertion covers BOTH hosts now:
              // each dispatches `Started` through the SAME `forTransition` its Update calls, and
              // neither is left with a seam it silently bypasses at startup.
              for host in cueSeamHosts do
                  let region = hostRegion host

                  Expect.stringContains
                      region
                      "AppRoot.AudioCues.forTransition Started initialModel initialModel"
                      $"{host}.Init dispatches Started through the SAME cue seam Update uses — not a separate startup branch"

                  Expect.isFalse
                      (region.Contains "Init = fun () -> initialModel, []")
                      $"{host}.Init must not go back to producing the initial model with no effects (the #458 hole)"

                  // An Init-only wiring would emit the startup cue and then go silent for every
                  // subsequent transition — the seam has to carry Update's cues out too.
                  Expect.stringContains
                      region
                      "AppRoot.AudioCues.forTransition msg model next"
                      $"{host}.Update lifts each transition's cues onto PlayAudio"

              // Comments are stripped FIRST, and every assertion below reads the STRIPPED text.
              // AudioCues.fs documents the seam with a worked example of a `Started` cue, so matching
              // the raw file counts prose as wiring. This gate used to take the FIRST regex match, and
              // in the shipped file that match WAS the comment — delete both real cue arms and it still
              // passed.
              let audioCuesCode =
                  audioCues.Replace("\r\n", "\n").Split('\n')
                  |> Array.filter (fun line -> not ((line.TrimStart()).StartsWith "//"))
                  |> String.concat "\n"

              Expect.stringContains model "| Started" "the starter Msg declares Started"
              Expect.stringContains audioCuesCode "| Started ->" "AudioCues.forTransition handles Started"

              // A `Started` case returning [] makes the generated product's own regression test
              // vacuous — it would pass whether or not Init is wired to the seam at all, which is how
              // this class survives its own fix (#266). The scaffold must ship it emitting something,
              // in EVERY starter's cue map: #436 gave AudioCues.fs one `forTransition` per starter,
              // because the two profiles ship different `Msg` types.
              let startedCues = Regex.Matches(audioCuesCode, @"\|\s*Started\s*->\s*\[(?<cues>[^\]]*)\]")

              Expect.equal
                  startedCues.Count
                  2
                  "AudioCues ships a wired `Started` cue for BOTH starters (the game's and the Controls one)"

              for startedCue in startedCues do
                  Expect.isFalse
                      (System.String.IsNullOrWhiteSpace startedCue.Groups.["cues"].Value)
                      "the scaffold ships `Started` wired to a real cue — an empty one makes the product's own regression test vacuous"
          }
        ]
