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
///
/// The version is `\d+(\.\d+){2,}` — two dots OR MORE, not exactly two, and greedy. Both halves are load-
/// bearing, and this repo has been bitten through each of them. Greedy, because `0.9.2` must not match
/// inside `0.9.20` and silently agree with the wrong pin (#711 found exactly that bound). Two-or-more,
/// because a 4-segment pin — NuGet permits `1.2.3.4` — would otherwise be read as `1.2.3`, which does not
/// equal the pin it was copied from, and the gate would red on prose that is CORRECT. A guard that reds on
/// correct input gets silenced, and then there is no guard at all.
let private pinClaimPattern =
    Regex(@"\$\(FsGgUiVersion\)`?\s+is(?:\s+now)?\s+(?<v>\d+(?:\.\d+){2,}(?:-[0-9A-Za-z.-]+)?)", RegexOptions.Compiled)

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

/// The ledgers whose prose NARRATES A RELEASE — each exists to say which version shipped what, so each
/// carries an epitaph crediting the pin. Only these are REQUIRED to make a claim (the vacuity test), and the
/// distinction is a real one rather than a convenience.
///
/// `surface-doc-ledger.txt` (S-DOC) is the third ledger and is deliberately NOT here. #709 guessed it
/// "likely has the same shape"; it does not — it curates the documented public surface and names no version
/// at all, so demanding an epitaph of it would be inventing a rule to satisfy a guess. It is still CHECKED:
/// the glob finds it, and any claim it grows must equal the pin. What it does not get is the vacuity guard —
/// so a claim written there in a spelling the pattern does not know (a backticked version, a line-wrapped
/// sentence) would go unchecked in silence, where the same mistake in the two files below reds. If S-DOC
/// ever does start narrating a release, move it up here and it inherits the teeth.
let private narratingLedgers =
    [ "tests/Package.Tests/mirror-pending-release-ledger.txt"
      "tests/Build.Tests/pinned-api-doc-ledger.txt" ]

/// Every ledger — DISCOVERED, not listed. A hand-written list is a finder that silently stops reaching its
/// subject the moment somebody adds a fourth ledger: its epitaph would rot with every gate green, which is
/// #690 all over again in a file this gate never looks at. That failure has a name in this repo — #725, the
/// finder that never reached the samples — and a list is how you get it. So the set is globbed, and the
/// discovery is itself asserted below (a glob that matches nothing is the vacuous pass, one level up again).
///
/// `bin`/`obj` are excluded: nothing copies these to the output today, but a future `CopyToOutputDirectory`
/// would otherwise have the gate grading a build artifact's stale duplicate of the file it already checks.
let private allLedgers =
    Directory.GetFiles(repo "tests", "*ledger*.txt", SearchOption.AllDirectories)
    |> Array.map (fun f -> Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
    |> Array.filter (fun rel -> not (rel.Contains "/bin/" || rel.Contains "/obj/"))
    |> Array.sort
    |> Array.toList

/// Read once each. The neighbouring guard (#706) sets the precedent and the reason: this suite runs in the
/// REQUIRED deterministic tier on every PR, and rescanning the same file for the same answer is pure
/// duplicate work in the gate everyone waits on.
let private claimsByLedger =
    lazy (allLedgers |> List.map (fun l -> l, claimsIn (File.ReadAllText(repo l))) |> Map.ofList)

let private claimsOf (relative: string) =
    match Map.tryFind relative claimsByLedger.Value with
    | Some claims -> claims
    | None -> failwith $"{relative} was not discovered under tests/ — the glob and the narrating list disagree, which the discovery test below exists to catch."

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

          // DISCOVERY, ASSERTED. The two tests above are only as wide as the glob that feeds them, so the
          // glob is the next place this gate can go quietly vacuous: match nothing, check nothing, pass.
          // It must find at least the ledgers we KNOW exist — a rename or a move that slips past it would
          // otherwise silently drop a file from the gate rather than red.
          test "the ledger set is really DISCOVERED — the glob reaches its subjects" {
              Expect.isNonEmpty
                  allLedgers
                  "the `*ledger*.txt` glob under tests/ matched NOTHING, so both tests above iterated an empty list and passed without checking a single file. That is the vacuous pass this whole suite exists to refuse."

              for narrating in narratingLedgers do
                  Expect.contains
                      allLedgers
                      narrating
                      $"""the discovery glob no longer reaches {narrating} — a release-narrating ledger has been renamed or moved out of tests/, and with it out of this gate. Its epitaph would rot unchecked, which is #690 exactly. Found: {String.Join(", ", allLedgers)}"""
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

          // THE VERSION IS READ WHOLE, OR THE GATE REDS ON PROSE THAT IS RIGHT. Two bounds, one test:
          //
          //   * a 4-segment pin (NuGet permits it) must not be truncated to its first three. Read `1.2.3`
          //     out of a correct `is 1.2.3.4` and the claim no longer equals the pin it was copied from —
          //     the gate reds on a correct sentence, and a guard that reds on correct input gets silenced.
          //   * `0.9.2` must not match inside `0.9.20`. #711 found precisely that bound in the pin-probe
          //     waiver, so it is not hypothetical in this repo; it is a bug that has already happened once.
          test "a version is read WHOLE — 4 segments are not truncated, and 0.9.2 does not match inside 0.9.20" {
              let four = claimsIn "# `$(FsGgUiVersion)` is 1.2.3.4 and the pinned packages export all four"
              Expect.hasLength four 1 "the claim is found"

              Expect.equal
                  four.[0].Version
                  "1.2.3.4"
                  "a 4-segment version must be captured WHOLE. Truncating it to `1.2.3` makes a correct sentence disagree with the pin it names, and reds the gate on prose that is right."

              let twenty = claimsIn "# `$(FsGgUiVersion)` is 0.9.20, published FS.GG.UI.SkiaViewer 0.9.20 exports the case"
              Expect.hasLength twenty 1 "the claim is found"

              Expect.equal
                  twenty.[0].Version
                  "0.9.20"
                  "`0.9.20` must be captured whole, not read as `0.9.2` with a stray `0`. A prefix match here would let a ledger claiming 0.9.20 agree with a 0.9.2 pin — green, and wrong (the #711 bound)."
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
