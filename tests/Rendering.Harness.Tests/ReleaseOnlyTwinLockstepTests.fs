module ReleaseOnlyTwinLockstepTests

// FS-GG/FS.GG.Rendering#366, #613 — the meta-guard that keeps a PR-time twin paired with the
// release-only rule it mirrors, so the mirror cannot silently DESYNC.
//
// WHAT #613 CHANGED, AND WHY THE REGISTRY IS NOW ONE PAIR. This guard used to police sixteen twins.
// Fifteen of them hoisted a rule out of Package.Tests, which was not a slnx member and so ran only in
// the release lane. #540 made Package.Tests a member; the gate derives its test list from the slnx, so
// those rules now run on every PR AT THEIR SOURCE. The fifteen twins were left as byte-identical copies
// of checks that already fire on the PR, and #613 deleted them. What replaced their implicit guarantee
// is `PackageTestsGateMembershipTests` — an explicit assertion that Package.Tests stays in the slnx,
// which is the premise the deletion rests on.
//
// ONE TWIN SURVIVES: `TemplateLaunchExpressionCoherenceTests`, paired with `template/base/tests/Product.Tests`
// — the tests of the INSTANTIATED product. #613 kept it because "no PR job instantiates the template",
// and #680 (PR #704) MADE THAT FALSE: gate.yml's `generated-product-gate` now scaffolds every profile
// and runs its tests on every PR. #719 re-derived the pair from scratch rather than re-asserting the
// dead sentence, and the twin still earns its keep — on a narrower ground. Its counterpart's launch
// assertions are all EXISTENTIAL (`stringContains`); the twin's are UNIVERSAL (set equality over the
// launch expressions the template emits, and `audioSink` on every one of them). An expression NOBODY
// asserts — a new family's, or a sinkless call added beside an asserted one — passes all five
// instantiated profiles and reds only in the twin. That is #436 exactly. The twin's own header carries
// the full argument; `GeneratedProductGateCoverageTests` asserts the premise it now rests on.
//
// L-RULES, L-INPUTS AND L-FORMS ARE GONE, AND THAT IS NOT A ROLLBACK OF #612 (#623). Those three checks
// landed days before this change and were RIGHT: L-INPUTS had proxied "do the two check the same things?"
// with "do the two read the same files?", and #623 showed the proxy was blind to the one failure it most
// needed to see — `ApiSurfaceMirrorCoherenceTests` omitted M-MIR for months while L-INPUTS stayed green.
// L-RULES compared the twins' `test "…"` sets directly and caught it; L-FORMS kept L-INPUTS' completeness
// claim honest; the decoder they share was tested against the real argument forms in the tree.
//
// Every one of them takes a TEXT-MIRROR PAIR as its subject, and after #613 there are none. Both flags on
// the surviving pair were ALREADY false (`SharedInputs = false`, `MirroredRules = false`): its counterpart
// is a DIRECTORY of instantiated tests, with no single source to compare inputs against and no `test "…"`
// set to compare rule-for-rule. So L-RULES and L-INPUTS would each iterate an EMPTY filter of the registry
// and pass having asserted nothing, and L-FORMS would be policing the completeness of an L-INPUTS that no
// longer exists. A check that reports green over a missing subject is the precise failure #623 was written
// to end — leaving these three dormant would reproduce it, one level up. So they are deleted rather than
// left lying, and #612's LESSON is kept where it can still act: in the rule below, for whoever adds the
// next twin.
//
// IF A TEXT-MIRROR TWIN IS EVER ADDED BACK, restore them from #623 (`git show 77cb612c`) — WITH the
// rule-set comparison, not just the input one. Do not re-derive a weaker guard from scratch; that is the
// mistake #612 exists to record.
//
// WHAT IT LOCKS. A registry of (twin, release-only counterpart) pairs. For each pair this asserts,
// STATICALLY (it only reads the test sources as text — it never compiles or runs them):
//   L-EXISTS  both endpoints still exist on disk. Rename or delete either side and this reds, forcing
//             the pair to be reconciled in the same PR.
//   L-NAMES   the twin's HEADER names its release-only counterpart, so a maintainer standing in front
//             of one is pointed at the other.
//   L-CLOSED  every `*CoherenceTests.fs` in this project is registered here. A new twin cannot be added
//             without declaring the release-only rule it pairs with.
//
// ADDING A TWIN: first ask whether you need one, and note that the OLD test for that is obsolete (#719).
// It used to be "can the counterpart run on a PR?" — and the answer was no only for the instantiated
// product. #680 put the instantiated product on the PR gate too, so by that test NOTHING would ever
// earn a twin again, and the one twin left would already be gone. That test is not the right one.
//
// The right test is: CAN THE COUNTERPART EXPRESS THE RULE AT ALL? A twin earns its keep when it asserts
// something its counterpart structurally cannot, even with the counterpart running on every PR. The
// surviving twin qualifies because its counterpart's assertions are existential and the rule is
// universal — a closure over what the template emits, which no per-profile presence check can state.
// A twin that merely re-states a rule its counterpart already runs is a second copy of a check that
// already fires, which is exactly what #613 deleted 3,600 lines of — write the rule where it belongs
// and stop.
//
// If you do have a real case, give it the `…CoherenceTests.fs` name, register it below, and restore
// L-RULES (above) so the copy cannot rot.

open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repoPath (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private twinDirectory = "tests/Rendering.Harness.Tests"

/// One hoisted rule: the slnx-resident twin, the release-only rule it mirrors, and the token the twin's
/// header must name it by.
type private TwinPair =
    { /// Repo-relative path to the PR-time twin (in this project).
      Twin: string
      /// Repo-relative path to the release-only counterpart.
      ReleaseOnly: string
      /// A token the twin's header must contain so it points a reader at its counterpart.
      HeaderNames: string }

let private registry =
    [ // #350 — the generated product's per-family default launch host. The counterpart is the INSTANTIATED
      // template/base/tests/Product.Tests. Since #680 those tests DO run on every PR (gate.yml scaffolds
      // every profile), so this pair is no longer "a release-only rule hoisted one gate earlier" — what
      // it is now is a UNIVERSAL rule standing over an existential counterpart, which is the whole of why
      // #719 kept it. See the twin's header. It is a directory, not a text-mirror of one source: no input
      // set to compare, and no `test "…"` set to compare rule-for-rule (which is why it carried
      // `SharedInputs = false` and `MirroredRules = false` while those checks existed).
      // L-EXISTS/L-NAMES/L-CLOSED guard it.
      { Twin = $"{twinDirectory}/TemplateLaunchExpressionCoherenceTests.fs"
        ReleaseOnly = "template/base/tests/Product.Tests"
        HeaderNames = "Product.Tests" } ]

let private exists (repoRelative: string) =
    let full = repoPath repoRelative
    File.Exists full || Directory.Exists full

/// The twin's leading `//` comment block — its header, and nothing else.
///
/// L-NAMES has to read THIS rather than the whole file. The surviving twin names its counterpart in a path
/// literal (`repositoryPath "template/base/tests/Product.Tests"`) as well as in its prose, so a whole-file
/// `stringContains` passes even with the header deleted outright — asserting nothing while reporting green,
/// which is the failure this guard exists to catch.
let private headerOf (twinPath: string) =
    File.ReadLines(repoPath twinPath)
    |> Seq.skipWhile (fun line -> not (line.StartsWith "//"))
    |> Seq.takeWhile (fun line -> line.StartsWith "//" || line.Trim() = "")
    |> String.concat "\n"

[<Tests>]
let releaseOnlyTwinLockstepTests =
    testList
        "#366 — release-only ↔ PR-time twin lockstep"
        [
          // L-EXISTS — neither endpoint may vanish without the other being reconciled in the same PR.
          test "every registered twin and its release-only counterpart still exist" {
              // Fail loud, never vacuous: an empty registry would satisfy every loop in this file
              // trivially. If the last twin is ever retired, DELETE this guard — do not leave it
              // asserting nothing over an empty list (#613).
              Expect.isNonEmpty
                  registry
                  "the twin registry must not be empty — if the last twin was retired, delete this guard rather than leaving it reporting green over no subject"

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
                  let header = headerOf pair.Twin

                  Expect.isNotEmpty
                      header
                      (sprintf "twin %s must open with a `//` header block explaining what it mirrors" pair.Twin)

                  Expect.stringContains
                      header
                      pair.HeaderNames
                      (sprintf
                          "the HEADER of twin %s must name its release-only counterpart '%s' so the two stay discoverable from each other (a mention in a path literal further down does not count — the reader lands on the header)"
                          pair.Twin
                          pair.HeaderNames)
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
                      "these coherence twins are not registered in ReleaseOnlyTwinLockstepTests — register each with the release-only rule it pairs with, and check FIRST that it needs to exist at all: a Package.Tests rule already runs on the PR gate (#540, #613): %A"
                      unregistered)
          }
        ]
