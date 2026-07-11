module AudioSkillSurfaceCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only fs-gg-audio skill-surface gate.
//
// WHY THIS EXISTS. The shipped `template/product-skills/fs-gg-audio/SKILL.md` tells authors to consume
// a specific FS.GG.Audio surface (the pure Core vocabulary plus, since #245, the host-side
// `Audio.play`), and the template bundles the `docs/api-surface/Audio.Core/Audio.fsi` +
// `Audio.Host/Host.fsi` doc copies the skill cites. `AudioSkillSurfaceTests` (tests/Package.Tests)
// enforces that the skill only names members the bundled surface declares, that the cited surface is
// the one the product ships, and that the Canvas-era audio doc copy retired with Canvas 0.3.0 (#158)
// has not crept back. But Package.Tests is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs
// only under `dotnet test … -c Release` in release.yml. So a SKILL.md edit that renames or hallucinates
// an `Audio.<member>`, a bundled surface that stops declaring `Audio.play`, a reintroduced
// `Canvas/Audio.fsi`, or a doc copy whose namespace no longer carries its package directory all compile
// green on a PR and only red the release lane. That is the "PR-gated tests must be in the slnx" gap
// #350/#382 already closed for the launch host and the audio profile wiring, and
// #388/#397/#401/#402/#405/#408/#410/#417/#418 have been closing rule-by-rule for the other twins.
//
// WHAT IT LOCKS. AudioSkillSurfaceTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it re-derives everything from the shipped skill body, the two bundled
// FS.GG.Audio `.fsi` doc copies, and the whole bundled api-surface tree. So this hoist is a faithful
// mirror — it reads the SAME source-of-truth inputs STATICALLY and asserts the SAME invariants, one
// gate earlier:
//   A-MEMBERS — every `Audio.<member>` the shipped SKILL.md names resolves to a `val`/`type` in the
//               bundled surface (a renamed/hallucinated/relocated member fails).
//   A-BUNDLE  — the surface the skill CITES is the surface the product actually ships.
//   A-RETIRED — the retired Canvas audio doc-copy is gone, and the body no longer opens Canvas for
//               audio (naming Canvas in the migration pitfall is fine — that is the point of it).
//   A-NS      — EVERY bundled `docs/api-surface/<Pkg>/*.fsi` declares a namespace carrying `<Pkg>` as
//               a dotted component (the drift check that would have caught #158 at the source).
//
// Kept in deliberate lockstep with tests/Package.Tests/AudioSkillSurfaceTests.fs: the two read the same
// repo inputs and assert the same parity, so a real drift fails BOTH — the release check is mirrored
// earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing (L-INPUTS) so the mirror
// cannot silently desync.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private skillBodyPath = repositoryPath "template/product-skills/fs-gg-audio/SKILL.md"
let private apiSurfaceRoot = repositoryPath "template/base/docs/api-surface"

/// The product-relative path the skill cites, and where the template really bundles it.
let private citedSurfaceRelative = "docs/api-surface/Audio.Core/Audio.fsi"
let private audioCoreFsiPath = repositoryPath ("template/base/" + citedSurfaceRelative)

/// Issue #245 — the skill's runtime-playback claim became TRUE, so the skill now cites the host-side
/// drive (`Audio.play backend`) as well as the pure vocabulary. That member lives in FS.GG.Audio.Host,
/// so the Host surface is bundled too and A-MEMBERS resolves against both. Cite-what-you-ship is
/// unchanged; the shipped set simply grew by the surface the claim depends on.
let private citedHostSurfaceRelative = "docs/api-surface/Audio.Host/Host.fsi"
let private audioHostFsiPath = repositoryPath ("template/base/" + citedHostSurfaceRelative)

/// The doc copy retired with Canvas 0.3.0 (ADR-0024, #158) — it must not come back.
let private retiredCanvasAudioFsi = repositoryPath "template/base/docs/api-surface/Canvas/Audio.fsi"

/// A packed .fsi declares a member `m` when it carries `val m:` or `type m`.
let private declaresMember (fsiText: string) (m: string) =
    Regex.IsMatch(fsiText, sprintf @"(^|\s)val\s+%s\s*:" (Regex.Escape m), RegexOptions.Multiline)
    || Regex.IsMatch(fsiText, sprintf @"(^|\s)type\s+%s\b" (Regex.Escape m))

/// The first `namespace` declaration in a signature file, if any.
let private declaredNamespace (fsiText: string) =
    let m = Regex.Match(fsiText, @"^namespace\s+(\S+)", RegexOptions.Multiline)
    if m.Success then Some(m.Groups.[1].Value) else None

[<Tests>]
let audioSkillSurfaceCoherenceTests =
    testList
        "#366 twin — fs-gg-audio skill surface"
        [
          // A-MEMBERS — a member the body names but no bundled surface declares is a lie the build must
          // not ship. `Audio.playSfx` resolves in Core; `Audio.play` resolves in Host (issue #245); a
          // renamed or hallucinated member resolves in neither and fails.
          test "every Audio.<member> cited in SKILL.md resolves in a bundled FS.GG.Audio surface" {
              let body = File.ReadAllText skillBodyPath
              let fsiText = File.ReadAllText audioCoreFsiPath + "\n" + File.ReadAllText audioHostFsiPath

              // Code position only: members are lowercase-initial F# values; the lookbehind rejects
              // file-path / qualified contexts like `Audio.Core/Audio.fsi` or `X.Audio.y`.
              let cited =
                  Regex.Matches(body, @"(?<![\w./])Audio\.([a-z][A-Za-z0-9']*)")
                  |> Seq.map (fun m -> m.Groups.[1].Value)
                  |> Seq.distinct
                  |> Seq.toList

              Expect.isNonEmpty cited "the skill body cites at least one Audio member"

              let unresolved = cited |> List.filter (declaresMember fsiText >> not)
              Expect.isEmpty unresolved $"every cited Audio.<member> resolves in {citedSurfaceRelative} or {citedHostSurfaceRelative}"
          }

          // Issue #245 — the runtime-playback claim is only honest while the member backing it ships and
          // the skill points a reader at it. That the Audio.Host mirror EXISTS, sits in the right
          // namespace, and records its provenance is `ApiSurfaceMirrorTests` (M-REF / M-NS / M-PROV,
          // issue #247); this asserts only what the skill's claim adds on top.
          test "the skill's runtime-playback claim points at a surface that declares Audio.play" {
              let hostFsi = File.ReadAllText audioHostFsiPath
              Expect.isTrue (declaresMember hostFsi "play") "the bundled Host surface declares Audio.play"

              let body = File.ReadAllText skillBodyPath
              Expect.stringContains body citedHostSurfaceRelative "SKILL.md cites the bundled host surface path"
          }

          // A-BUNDLE — the surface the skill points a reader at is the one the product ships, and it
          // is the FS.GG.Audio.Core surface (not a Canvas-era copy sitting at a new path).
          test "the cited FS.GG.Audio.Core surface is bundled and declares its own namespace" {
              Expect.isTrue (File.Exists audioCoreFsiPath) $"bundled surface missing: {citedSurfaceRelative}"

              let body = File.ReadAllText skillBodyPath
              Expect.stringContains body citedSurfaceRelative "SKILL.md cites the bundled surface path"

              let ns = declaredNamespace (File.ReadAllText audioCoreFsiPath)
              Expect.equal ns (Some "FS.GG.Audio.Core") "the bundled audio surface declares namespace FS.GG.Audio.Core"
          }

          // A-RETIRED — the Canvas-era doc copy is gone, and the body opens the new namespace. Naming
          // FS.GG.UI.Canvas in the migration pitfall is intentional, so assert on the `open`, not the token.
          test "the retired Canvas audio surface is gone and the skill opens FS.GG.Audio.Core" {
              Expect.isFalse
                  (File.Exists retiredCanvasAudioFsi)
                  "template/base/docs/api-surface/Canvas/Audio.fsi was retired with Canvas 0.3.0 (#158)"

              let body = File.ReadAllText skillBodyPath
              Expect.stringContains body "open FS.GG.Audio.Core" "the skill opens the standalone audio namespace"

              Expect.isFalse
                  (Regex.IsMatch(body, @"^\s*open\s+FS\.GG\.UI\.Canvas\s*$", RegexOptions.Multiline))
                  "the skill no longer opens FS.GG.UI.Canvas for the audio vocabulary"
          }

          // A-NS — the general anti-drift rule. A bundled doc copy cannot outlive the package it claims:
          // its declared namespace must carry the directory name as a dotted component.
          test "every bundled api-surface .fsi declares a namespace carrying its package directory" {
              let offenders =
                  Directory.GetDirectories apiSurfaceRoot
                  |> Seq.collect (fun dir ->
                      let pkg = DirectoryInfo(dir).Name

                      Directory.GetFiles(dir, "*.fsi")
                      |> Seq.choose (fun file ->
                          let text = File.ReadAllText file

                          match declaredNamespace text with
                          | None -> Some(file, "<no namespace declaration>")
                          | Some ns ->
                              let pattern = sprintf @"(^|\.)%s(\.|$)" (Regex.Escape pkg)
                              if Regex.IsMatch(ns, pattern) then None else Some(file, ns)))
                  |> Seq.toList

              Expect.isEmpty offenders "each bundled <Pkg>/*.fsi declares a namespace containing <Pkg>"
          }
        ]
