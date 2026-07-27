module Issue1101ContractsSurfaceTests

// #1101 — THE PRE-#782 HAND-COPY BRIDGE IS DELETED, AND MUST STAY DELETED.
//
// `scripts/refresh-api-surface-mirror.fsx` used to carry a `legacyPre782Surfaces` map: a version-keyed
// escape hatch pointing FS.GG.Contracts at a hand-written `scripts/legacy-api-surfaces/**/Schemas.fsi`
// instead of the package's own packed `api-surface/`. The version key was a TRIPWIRE — the next pin bump
// stops matching, so nobody carries a hand-copy forward unnoticed.
//
// WHY A TEST AND NOT JUST A DELETION. The tripwire fired once and was stepped over (#1094 added a second
// entry for 7.2.0, on a measured-at-the-time argument). Its whole value was that the second time cannot
// happen quietly, and a deletion alone does not stop a third: the next worker who meets a package with no
// `api-surface/` faces exactly the same tempting one-liner. This suite is what makes reintroducing it a
// deliberate, visible act rather than a plausible local fix.
//
// WHAT THE BRIDGE COST, measured, and why "it was only one type" is not a defence. While an entry existed
// the generator never read that package's real surface, so it could not detect the taught type drifting.
// At the 7.2.0 pin `--emit-waivers` emitted 969 lines and NOT ONE was a Contracts member: the member-level
// COVERAGE rule (#925) had nothing to demand, because it only ever saw the hand-copy. Deleting the bridge
// and pinning 7.4.0 — the first Contracts release that packs `api-surface/*.fsi`, via FS-GG/FS.GG.SDD#742
// discharging #782's producer half — took it to 1081, all 112 new lines Contracts members.
//
// These are source-text assertions, deliberately. The behaviour they guard is a NEGATIVE ("no hand-copy
// path exists") plus the shape of an error message, and neither is observable from a green generate: the
// bridge was invisible on every run where the pin happened to match a packed release. Feature541 and #925
// establish this pattern in this suite for the same reason.

open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private generator = File.ReadAllText(repositoryPath "scripts/refresh-api-surface-mirror.fsx")
let private manifest = File.ReadAllText(repositoryPath "scripts/api-surface-manifest.txt")
let private pins = File.ReadAllText(repositoryPath "template/base/Directory.Packages.props")

/// The bridge's own identifiers. Matched against CODE only — the generator keeps a prose account of what
/// was removed and why, and a test that forbade the words outright would forbid the explanation too.
let private codeLines =
    generator.Replace("\r\n", "\n").Split('\n')
    |> Array.filter (fun l -> not (l.TrimStart().StartsWith "//"))

let private codeMentions (needle: string) =
    codeLines |> Array.exists (fun l -> l.Contains needle)

[<Tests>]
let issue1101ContractsSurfaceTests =
    testList
        "issue-1101 contracts api-surface bridge"
        [ test "the generator carries no legacy hand-copy surface map" {
              // The map itself. A reintroduction under a new name still has to point somewhere, which the
              // next two tests cover; this one catches the literal revert.
              Expect.isFalse (codeMentions "legacyPre782Surfaces") "no `legacyPre782Surfaces` map survives in the generator's code"
          }

          test "no code path reads a hand-written surface directory out of scripts/" {
              // The directory is what makes a bridge possible at all. Naming it in code — under any map
              // name — is the reintroduction, so this is the test that survives a rename.
              Expect.isFalse (codeMentions "legacy-api-surfaces") "no code path resolves `scripts/legacy-api-surfaces`"
              Expect.isFalse
                  (Directory.Exists(repositoryPath "scripts/legacy-api-surfaces"))
                  "scripts/legacy-api-surfaces/ does not exist"
          }

          test "the missing-surface failure asks the feed instead of prescribing a fixed remedy" {
              // AC3. The old message was one fixed sentence — "Bump the pin to a release that packs its
              // .fsi" — which was UNFOLLOWABLE for months: 7.0.0, 7.1.0, 7.2.0 and 7.3.0 were every
              // published FS.GG.Contracts and none packed `api-surface/`. Advice that may be impossible is
              // what pushed #1094 into the bridge, so the diagnostic has to decide, not assume.
              Expect.stringContains generator "packingReleaseAtOrAbove" "the failure path probes the feed for a release the pin could move UP to"
              Expect.stringContains generator "BUMPING THE PIN CANNOT FIX THIS" "it says so plainly when no such release exists"
              Expect.stringContains generator "The remedy belongs to the PRODUCING" "and it names whose problem it actually is"
          }

          test "the no-release advice does not send the reader back to a hand-copy" {
              // The one remedy that must never be suggested, in the one message a reader hits at the exact
              // moment it looks attractive.
              Expect.stringContains generator "Do NOT hand-copy the surface into this repo" "the impossible-bump branch forbids the bridge by name"
          }

          test "an unreachable feed fails closed rather than claiming no release exists" {
              // #266/#606. "I could not check" rendering as "no packing release exists" would send a worker
              // to file a producer issue that is already discharged — the failure mode this run was warned
              // about, made structural.
              Expect.stringContains generator "Could not determine whether any published release" "the unreachable-feed branch is distinct"
              Expect.stringContains generator "failing closed rather than guessing" "and it says it is failing closed"
          }

          test "the pin is at or above the first Contracts release that packs api-surface/" {
              // 7.4.0 is that release (FS-GG/FS.GG.SDD#742). Below it the generator has no surface to read
              // and, with the bridge gone, no fallback — so this is the pin's floor, not a preference.
              // Exact-version witnessing stays where #1102 AC2 put it
              // (tests/Package.Tests/Issue1039PerformanceEvidenceTests.fs); this asserts only the floor.
              let m =
                  System.Text.RegularExpressions.Regex.Match(pins, @"<FsGgContractsVersion>(?<v>[^<]+)</FsGgContractsVersion>")

              Expect.isTrue m.Success "the payload props declare $(FsGgContractsVersion)"

              let pinned = m.Groups.["v"].Value
              let parts = pinned.Split('.') |> Array.map int
              Expect.isTrue (parts.Length >= 2) "the pin has a major.minor core"
              Expect.isTrue
                  (parts.[0] > 7 || (parts.[0] = 7 && parts.[1] >= 4))
                  (sprintf "$(FsGgContractsVersion)=%s is >= 7.4.0, the first release that packs api-surface/" pinned)
          }

          test "the member-level coverage rule now sees FS.GG.Contracts at all" {
              // The blind spot, asserted directly. Before #1101 this count was ZERO — not because Contracts
              // exported nothing, but because the bridge fed the reconciliation a 22-line hand-copy. A
              // regression to zero means the generator stopped reading the real package, whatever the
              // reason, and that is the condition worth failing on rather than the exact number.
              let waivers =
                  manifest.Replace("\r\n", "\n").Split('\n')
                  |> Array.filter (fun l -> l.StartsWith "waive FS.GG.Contracts ")

              Expect.isGreaterThan
                  waivers.Length
                  0
                  "the manifest records FS.GG.Contracts members as taught-or-waived decisions; zero means the real surface is not being read"
          } ]
