module ReleaseOnlyTwinLockstepTests

// FS-GG/FS.GG.Rendering#366 — the meta-guard that keeps a PR-time twin paired with the release-only
// rule it mirrors, so the mirror cannot silently DESYNC.
//
// WHY THIS EXISTS. #350, #382 and this issue's BOM slice each hoist a release-only check into a cheap
// static twin in this slnx-resident project, so drift reds the PR instead of only the release lane.
// But a hoisted twin is a COPY, and a copy rots: someone edits the release-only rule (renames it,
// points it at a different source file, deletes it) and forgets the twin — or the reverse. The twin
// then quietly diverges from the rule it claims to mirror, and the PR gate goes back to lying. That
// silent desync is the core risk the issue names.
//
// WHAT IT LOCKS. A registry of (twin, release-only counterpart) pairs. For each pair this asserts,
// STATICALLY (it only reads the test sources as text — it never compiles or runs them):
//   L-EXISTS  both endpoints still exist on disk. Rename or delete either side and this reds, forcing
//             the pair to be reconciled in the same PR.
//   L-NAMES   the twin's header text names its release-only counterpart, so a maintainer standing in
//             front of one is pointed at the other (documentation lockstep — the precedents already
//             do this; this makes it a checked invariant, not a courtesy).
//   L-INPUTS  for a faithful MIRROR pair, the set of repository source-of-truth paths the twin reads
//             equals the set the release-only rule reads. If one side starts reading a new input the
//             other does not, they have diverged on WHAT they check — the exact silent desync above —
//             and this reds. (Skipped for the launch pair, whose release-only counterpart is the
//             INSTANTIATED Product.Tests, not a text-mirror of the same inputs.)
//   L-CLOSED  every `*CoherenceTests.fs` in this project is registered here. A new twin cannot be
//             added without declaring the release-only rule it pairs with — so no twin escapes the
//             lockstep guard by simply being new.
//
// Adding a twin: give it the `…CoherenceTests.fs` name, register it below with its release-only
// counterpart, and (for a Package.Tests mirror) keep the two reading the same inputs. If a twin must
// legitimately read a different input set than its counterpart, set `sharedInputs = false` and say why
// — that turns a silent divergence into a documented, reviewed decision.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repoPath (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private twinDirectory = "tests/Rendering.Harness.Tests"

/// One hoisted rule: the slnx-resident twin, the release-only rule it mirrors, the token the twin's
/// header must name it by, and whether the two are expected to read the same repository inputs.
type private TwinPair =
    { /// Repo-relative path to the PR-time twin (in this project).
      Twin: string
      /// Repo-relative path to the release-only counterpart — a Package.Tests source file, or, for the
      /// instantiated launch case, the template's Product.Tests directory.
      ReleaseOnly: string
      /// A token the twin's header must contain so it points a reader at its counterpart.
      HeaderNames: string
      /// True for a text-mirror pair that must read the same source-of-truth inputs (L-INPUTS).
      SharedInputs: bool }

let private registry =
    [ // #350 — the generated product's per-family default launch host. The release-only counterpart is
      // the INSTANTIATED template/base/tests/Product.Tests, so the twin reads the template source rather
      // than mirroring a Package.Tests file's inputs (SharedInputs = false).
      { Twin = $"{twinDirectory}/TemplateLaunchExpressionCoherenceTests.fs"
        ReleaseOnly = "template/base/tests/Product.Tests"
        HeaderNames = "Product.Tests"
        SharedInputs = false }
      // #382 — the ADR-0024 audio profile wiring shape. Text-mirror of a release-only Package.Tests rule.
      { Twin = $"{twinDirectory}/TemplateAudioProfileWiringCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/AudioProfileWiringTests.fs"
        HeaderNames = "AudioProfileWiringTests"
        SharedInputs = true }
      // #366 — the FS.GG.UI BOM membership parity. Text-mirror of a release-only Package.Tests rule.
      { Twin = $"{twinDirectory}/BomMembershipCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature207BomMembershipTests.fs"
        HeaderNames = "Feature207BomMembershipTests"
        SharedInputs = true }
      // #366 — the bundled api-surface mirror contract (M-REF/M-PTR/M-PROV + the #259 completeness
      // checks). Verbatim text-mirror of a release-only Package.Tests rule, so it reads the same four
      // source-of-truth inputs (the bundled tree, Product.fsproj, the template pins, the product-skills).
      { Twin = $"{twinDirectory}/ApiSurfaceMirrorCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/ApiSurfaceMirrorTests.fs"
        HeaderNames = "ApiSurfaceMirrorTests"
        SharedInputs = true }
      // #366 — the skill-manifest materializes-when + supplied-by coherence against template.json.
      // Text-mirror of a release-only Package.Tests rule: both read the skill-manifest and template.json
      // and evaluate their conditions over the same parameter grid, so they share the same two inputs.
      { Twin = $"{twinDirectory}/SkillMaterializesWhenCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs"
        HeaderNames = "Feature238SkillMaterializesWhenTests"
        SharedInputs = true }
      // #366 — the collision-safe Vec2 wiring gate (#138). Text-mirror of a release-only Package.Tests
      // rule: both read the fragment source, the base Model.fs, Product.fsproj and template.json, and
      // scan them for the naming invariant + delivery wiring, so they share the same four inputs.
      { Twin = $"{twinDirectory}/CollisionSafeVec2CoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature250CollisionSafeVec2Tests.fs"
        HeaderNames = "Feature250CollisionSafeVec2Tests"
        SharedInputs = true } ]

/// The repository source-of-truth paths a test source reads, extracted from its `repositoryPath "…"`
/// / `repo "…"` helper calls (the convention every twin and its Package.Tests counterpart follow).
/// A path named only in a `//` comment is NOT in this form, so prose references do not leak in.
let private inputPathsRead (testSourcePath: string) : Set<string> =
    let text = File.ReadAllText testSourcePath
    Regex.Matches(text, @"\b(?:repositoryPath|repo)\s+""([^""]+)""")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Set.ofSeq

let private exists (repoRelative: string) =
    let full = repoPath repoRelative
    File.Exists full || Directory.Exists full

[<Tests>]
let releaseOnlyTwinLockstepTests =
    testList
        "#366 — release-only ↔ PR-time twin lockstep"
        [
          // L-EXISTS — neither endpoint may vanish without the other being reconciled in the same PR.
          test "every registered twin and its release-only counterpart still exist" {
              for pair in registry do
                  Expect.isTrue (exists pair.Twin) (sprintf "twin %s exists" pair.Twin)
                  Expect.isTrue
                      (exists pair.ReleaseOnly)
                      (sprintf
                          "release-only counterpart %s exists (if it was renamed or removed, update the twin %s and this registry in lockstep)"
                          pair.ReleaseOnly
                          pair.Twin)
          }

          // L-NAMES — the twin header must point a reader at the rule it mirrors.
          test "every twin header names its release-only counterpart" {
              for pair in registry do
                  let twinText = File.ReadAllText(repoPath pair.Twin)
                  Expect.stringContains
                      twinText
                      pair.HeaderNames
                      (sprintf
                          "twin %s must name its release-only counterpart '%s' so the two stay discoverable from each other"
                          pair.Twin
                          pair.HeaderNames)
          }

          // L-INPUTS — a faithful text-mirror reads exactly the inputs its counterpart does. A divergence
          // here IS the silent desync this guard exists to catch.
          test "every text-mirror pair reads the same repository source-of-truth inputs" {
              for pair in registry |> List.filter (fun p -> p.SharedInputs) do
                  let twinInputs = inputPathsRead (repoPath pair.Twin)
                  let releaseInputs = inputPathsRead (repoPath pair.ReleaseOnly)

                  // Fail loud, never vacuous: an extraction that silently matched nothing on both sides
                  // would satisfy set-equality trivially while guarding nothing.
                  Expect.isNonEmpty
                      (Set.toList twinInputs)
                      (sprintf "twin %s must read at least one repository input via the path helper" pair.Twin)

                  let onlyInTwin = Set.difference twinInputs releaseInputs |> Set.toList
                  let onlyInRelease = Set.difference releaseInputs twinInputs |> Set.toList

                  Expect.isEmpty
                      onlyInTwin
                      (sprintf
                          "twin %s reads input(s) its release-only counterpart %s does not — the mirror has diverged: %A"
                          pair.Twin
                          pair.ReleaseOnly
                          onlyInTwin)
                  Expect.isEmpty
                      onlyInRelease
                      (sprintf
                          "release-only %s reads input(s) the twin %s does not — the twin is stale against the rule it mirrors: %A"
                          pair.ReleaseOnly
                          pair.Twin
                          onlyInRelease)
          }

          // L-CLOSED — no twin escapes the guard by being new. Every `*CoherenceTests.fs` here must be
          // registered above, so adding one forces declaring the release-only rule it pairs with.
          test "every *CoherenceTests.fs in this project is registered" {
              let registeredTwins = registry |> List.map (fun p -> Path.GetFileName p.Twin) |> Set.ofList

              let onDisk =
                  Directory.GetFiles(repoPath twinDirectory, "*CoherenceTests.fs")
                  |> Array.map Path.GetFileName
                  |> Set.ofArray

              // Guard against a convention change silently emptying the scan.
              Expect.isNonEmpty
                  (Set.toList onDisk)
                  "at least one *CoherenceTests.fs twin must exist for this lockstep guard to be meaningful"

              let unregistered = Set.difference onDisk registeredTwins |> Set.toList
              Expect.isEmpty
                  unregistered
                  (sprintf
                      "these coherence twins are not registered in ReleaseOnlyTwinLockstepTests — register each with the release-only rule it pairs with: %A"
                      unregistered)
          }
        ]
