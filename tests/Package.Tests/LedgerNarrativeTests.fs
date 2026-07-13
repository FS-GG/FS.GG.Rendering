module LedgerNarrativeTests

// #709 — the ledgers' PROSE, held against the pin it narrates.
//
// THE BLIND SPOT, AND IT IS ONE LEVEL UP FROM THE ONE THE LEDGERS CLOSE. Both release ledgers ratchet on
// rules that red LINES:
//
//   * `tests/Package.Tests/mirror-pending-release-ledger.txt` — P-PEND/PIN: an entry's stamp must equal
//     $(FsGgUiVersion), so "a pin bump reds EVERY line, forcing each to be judged afresh".
//   * `tests/Build.Tests/pinned-api-doc-ledger.txt` — the STALE rule: "the pin NOW exports the symbol ->
//     delete the line".
//
// Both files are EMPTY, which is their declared goal state. **A bump with no lines to red forces no
// re-judgement of anything.** So the 0.9.1 -> 0.9.2 bump sailed straight past prose asserting
//
//     `$(FsGgUiVersion)` is 0.9.1, published FS.GG.UI.SkiaViewer 0.9.1 ...
//
// naming a package that does not exist and never will — 0.9.1's tag triple was cut, both release-only gate
// jobs failed, `publish-packages` was skipped, and nothing was ever pushed to nuget.org (#690, root cause
// #681). Every gate was green the whole time, because these are `#` comment lines and every parser that
// reads these files filters them out before doing anything. The entries are machine-checked; the paragraph
// that EXPLAINS them was not — and that paragraph is the entire reason a reader opens the file, since both
// exist to say which version shipped what. The reader most likely to be misled is the one doing an audit.
//
// This file closes that. It is the ledgers' own idiom turned on their own prose.
//
// WHAT IS CHECKED, AND — JUST AS LOAD-BEARING — WHAT IS NOT. Only a claim about what the pin IS *now*:
//
//     `$(FsGgUiVersion)` is 0.9.2          <- checked. Must equal the real pin.
//     `$(FsGgUiVersion)` is now 0.9.2      <- checked. Same claim, same rule.
//
// Every OTHER version in these files is deliberate HISTORY and must be left alone: "the published line runs
// 0.9.0 -> 0.9.2", "0.9.1 IS A PHANTOM TAG", the retired entry `ViewerEffect.Persist @ 0.9.0 #594`. Between
// them the two ledgers carry ~20 semver mentions and exactly TWO are pin claims. A guard that held every
// version number against the pin would red both files on their own correct account of the past — and a guard
// that reds on correct input gets silenced, which is how you end up with no guard at all. So the claim is
// ANCHORED on `$(FsGgUiVersion)`, which is the only phrase in either file that asserts the present tense.
//
// THE FEED HALF IS DELIBERATELY NOT HERE. #709 also suggests holding `published <Package> <v>` claims against
// the real feed. That is left undone on purpose, and not merely because it is "optional": Package.Tests runs
// in the REQUIRED deterministic tier, which is static by design — no feed, no restore, no network — because a
// feed outage must not be indistinguishable from a real break. And the check would add little: a `published`
// claim naming the PIN is already proven by `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs` (#589),
// which restores the pinned packages and compiles against them, while a `published` claim naming any OTHER
// version is history — the thing this file is careful not to touch.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

/// The fact every claim below is judged against. Same regex as Feature209VersionCoherenceTests and
/// build.fsx, which is the repo's one spelling of "read the pin"; #209 separately asserts that exactly one
/// `<FsGgUiVersion>` literal exists, so there is no ambiguity about which one this is.
let private pin =
    let props = File.ReadAllText(repo "template/base/Directory.Packages.props")
    Regex.Match(props, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>").Groups.[1].Value.Trim()

type private PinClaim =
    { Line: int
      Version: string
      Text: string }

/// The extractor. `$(FsGgUiVersion)` — optionally backticked, as both ledgers write it — followed by `is`
/// or `is now` and a version. Nothing else is a claim about the present pin.
let private pinClaimPattern =
    Regex(@"\$\(FsGgUiVersion\)`?\s+is(?:\s+now)?\s+(?<v>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)", RegexOptions.Compiled)

/// Claims in raw text. Split out from the file read so the fixtures below can drive it directly — the
/// extractor is the part that can silently stop matching, so it is the part that must be provably able to.
let private claimsIn (text: string) : PinClaim list =
    text.Split '\n'
    |> Array.toList
    |> List.indexed
    |> List.collect (fun (i, line) ->
        pinClaimPattern.Matches line
        |> Seq.map (fun m ->
            { Line = i + 1
              Version = m.Groups.["v"].Value
              Text = line.Trim() })
        |> Seq.toList)

let private claimsOf (relative: string) = claimsIn (File.ReadAllText(repo relative))

/// The ledgers whose prose NARRATES A RELEASE — each exists to say which version shipped what, so each
/// carries an epitaph crediting the pin. They are required to make a claim (see the vacuity test).
let private narratingLedgers =
    [ "tests/Package.Tests/mirror-pending-release-ledger.txt"
      "tests/Build.Tests/pinned-api-doc-ledger.txt" ]

/// Every ledger in the repo. `surface-doc-ledger.txt` (S-DOC) is checked but NOT required to claim: #709
/// guessed it "likely has the same shape", and it does not — it curates the documented public surface and
/// mentions no version at all. Requiring an epitaph of it would be inventing a rule. Including it costs
/// nothing and means that if anyone ever DOES write a version claim into it, that claim is born checked.
let private allLedgers =
    narratingLedgers @ [ "tests/Package.Tests/surface-doc-ledger.txt" ]

[<Tests>]
let ledgerNarrativeTests =
    testList
        "Ledger narrative vs the pin"
        [
          // THE GATE. This is the assertion that would have caught #690 the day it landed.
          test "every pin claim in every ledger's prose equals the real pin" {
              for ledger in allLedgers do
                  for claim in claimsOf ledger do
                      Expect.equal
                          claim.Version
                          pin
                          $"{ledger}:{claim.Line} says the pin is {claim.Version}, but template/base/Directory.Packages.props pins {pin}.\n\n  {claim.Text}\n\nThis is the #690 defect exactly: the ledger's ENTRIES are machine-checked and its PROSE was not, so a pin bump left the paragraph crediting a release that never happened — with every gate green. Update the sentence to name the real pin ({pin}). If the release it credits genuinely did not publish, say so in the prose the way the 0.9.1 phantom paragraphs already do; do not leave the present-tense claim wrong."
          }

          // THE TEETH. Everything above is vacuous the instant the extractor stops matching — which is the
          // SAME failure this whole file exists to close, one level further up: a check that binds nothing
          // passes forever. "Nothing to check" and "checked, and it's fine" must not share an exit code
          // (FS-GG/.github#266). Both of these files narrate a release in their epitaph; if either stops
          // making a claim the extractor can see, that is a guard error, not a pass.
          test "each release-narrating ledger really MAKES a pin claim — the gate above is not vacuous" {
              for ledger in narratingLedgers do
                  let claims = claimsOf ledger

                  Expect.isNonEmpty
                      claims
                      $"{ledger} makes no `$(FsGgUiVersion)` is <version> claim that the extractor can see, so the test above checked NOTHING for this file and would go on passing forever.\n\nEither the epitaph paragraph crediting the pin was deleted — it is the reason a reader opens this file, so put it back — or the prose was reworded past the pattern (`\\$\\(FsGgUiVersion\\)`? is [now] <v>`). If the wording must change, change the pattern in this file with it. An empty ledger with no lines to red is exactly how #690 rode green; do not let the check on the PROSE rot the same way."
          }

          // The pin itself has to be real, or every comparison above is against "".
          test "the pin resolves" {
              Expect.isMatch
                  pin
                  @"^\d+\.\d+\.\d+"
                  $"could not read <FsGgUiVersion> out of template/base/Directory.Packages.props (got '{pin}'). Every assertion in this file compares against it."
          }

          // ---- The extractor, driven against fixtures. A guard that cannot fail is not a guard. ----

          // The #690 prose, verbatim. The gate must SEE this claim and read 0.9.1 out of it — if it did not,
          // the test above would have passed on the very file that was lying.
          test "the #690 sentence is caught — the exact prose that rode green" {
              let claims =
                  claimsIn "# `$(FsGgUiVersion)` is 0.9.1, published FS.GG.UI.SkiaViewer 0.9.1 exports the case, and so M-MIR/TYPE takes"

              Expect.hasLength claims 1 "the #690 sentence carries exactly one pin claim"
              Expect.equal claims.[0].Version "0.9.1" "the claimed version is read out of the sentence"
              Expect.notEqual claims.[0].Version pin "...and it does not equal the real pin, which is what reds the gate"
          }

          test "the `is now` spelling is caught too — both ledgers word it differently" {
              let claims =
                  claimsIn "# spelling to rewrite them TO. `$(FsGgUiVersion)` is now 0.9.1 and the pinned packages export all four, so"

              Expect.hasLength claims 1 "`is now` is the same claim as `is`"
              Expect.equal claims.[0].Version "0.9.1" "the claimed version is read out of the sentence"
          }

          // NO FALSE POSITIVES, AND THIS IS THE TEST THAT KEEPS THE GATE ALIVE. Every line below is real
          // prose from the two ledgers, and every version in it is deliberate history: the phantom 0.9.1,
          // the published line, a retired entry's stamp. A guard that matched bare version numbers would red
          // both files on their own correct account of the past — and a guard that reds on correct input is a
          // guard somebody deletes.
          test "history is not a claim — the deliberate 0.9.0 / 0.9.1 prose is NOT matched" {
              let history =
                  String.Join(
                      "\n",
                      [ "# The one entry this file was filed for — `SkiaViewer::ViewerEffect.Persist @ 0.9.0 #594` — is RETIRED."
                        "# 0.9.1 IS A PHANTOM TAG — IT WAS TAGGED AND ABANDONED, AND IT NEVER PUBLISHED (FS.GG.Rendering#690)."
                        "# `publish-packages` was correctly skipped, and no `0.9.1` package was ever pushed to nuget.org: the"
                        "# published line runs 0.9.0 -> 0.9.2, and `fs-gg-ui/v0.9.1` still points at 1fddbd0b as an explained gap"
                        "# the very next commit bumped to 0.9.2 (88f8ae2c) — because with the pin naming a version nuget.org"
                        "# The release that actually made `Persist` bindable is 0.9.2 (#679 / #684), which is why this paragraph"
                        "# FORMAT: <mirror dir>::<Type>.<Member> @ <pin the omission was judged against> #<issue that retires it>" ])

              Expect.isEmpty
                  (claimsIn history)
                  "the ledgers' historical prose must not be read as a claim about the CURRENT pin. Only `$(FsGgUiVersion) is [now] <v>` asserts the present tense; every other version in these files is a deliberate record of the past (the 0.9.1 phantom, the 0.9.0 -> 0.9.2 published line, a retired entry's stamp). Matching those would red both ledgers on prose that is correct."
          }

          // The pin is EXEMPT from the staleness rule elsewhere (docs/ci/cadence-map.md), and prose about
          // the property is not prose about its value. `$(FsGgUiVersion) is exempt from ...` must not be
          // mistaken for a version claim — a greedier pattern (e.g. `is .*(\d+\.\d+\.\d+)`) would scrape the
          // next version off the line and red on a sentence making no claim at all.
          test "prose ABOUT the property is not a claim about its value" {
              let claims =
                  claimsIn "**Why `$(FsGgUiVersion)` is exempt from the staleness rule, and only that rule.** The 0.9.2 pin ..."

              Expect.isEmpty
                  claims
                  "`$(FsGgUiVersion) is exempt from ...` asserts nothing about the pin's VALUE. The pattern must require a version immediately after `is`/`is now`, or it will scrape an unrelated number off the rest of the line and red on correct prose."
          }
        ]
