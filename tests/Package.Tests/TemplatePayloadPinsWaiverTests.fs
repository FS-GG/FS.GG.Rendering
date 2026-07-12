module TemplatePayloadPinsWaiverTests

// Issue #544 — THE TEMPLATE-PAYLOAD RELEASE-PENDING WAIVER, AND THE BOUNDS THAT ARE THE ONLY THING
// STOPPING IT FAILING OPEN.
//
// #506 gave `scripts/validate-template-payload-pins.fsx` a RELEASE-PENDING waiver: on a release PR the
// pin necessarily names a version nuget.org does not carry yet, so `pin-not-published` necessarily reds.
// The waiver suppresses that — and its entire safety rests on a conjunction, plus one axis guard, that
// nothing re-checked:
//
//     releasePending pending =
//         not pending.IsEmpty && not releaseLane && bumpedInCommitUnderTest propsRel uiAxis
//
//     feedFailures:  if not (waiveUi && axis = uiAxis) then <check existence>
//
// Drop any one conjunct and the guard fails OPEN. The axis guard is the most dangerous: the naive
// "this commit bumped an axis ⇒ waive it" would waive an unpublished `FS.GG.Game.*` pin and exit 0 —
// which is #235, the exact defect this script exists to catch, in a new coat. Nothing in CI would
// notice, because the only thing that exercises the waiver is a real release PR: once a minor, and the
// worst imaginable place to discover a fail-open.
//
// WHY THIS MIRRORS THE PREDICATES RATHER THAN DRIVING THE SCRIPT.
//
// The guard is a standalone `dotnet fsi` entry point: it ends in a top-level `exit`, so it cannot be
// `#load`ed, and its feed layer talks to `https://api.nuget.org/...` with no seam. Driving it as a
// subprocess would therefore put nuget.org on the critical path of this suite — and every scenario in
// the table below needs the feed, because `releasePending` short-circuits on `pending` (which only the
// feed can answer) before it consults anything else.
//
// Giving the script a feed-stub env var to make it drivable offline would be worse than the gap it
// closes: a switch that lets a caller replace the feed with a fixture is a green-by-substitution bypass,
// in a guard whose whole ethos is fail-closed. So the decision layer is mirrored here — the precedent
// #544 itself points at (`Feature209VersionCoherenceTests` mirrors `validate-version-coherence.fsx`'s
// `PinPending` for the same reason).
//
// AND THE WEAKNESS OF THAT PRECEDENT IS FIXED, NOT INHERITED. A mirror tests a COPY: edit the script and
// the copy still passes, which is a fail-open one level up. Feature209 manages that with a "keep in
// lockstep" COMMENT. Comments do not fail builds. So `source lockstep` below reads the real script and
// asserts the predicate and the axis guard still have the shape this file models. Change either one and
// these tests go red, naming this file — which is exactly the moment a human must re-derive the bounds.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private guardPath = Path.Combine(root, "scripts", "validate-template-payload-pins.fsx")
let private guardSource = File.ReadAllText guardPath

/// Collapse runs of whitespace so an assertion about the SHAPE of a predicate is not an assertion about
/// its indentation — reformatting the script must not red this suite, but rewriting its logic must.
let private squash (s: string) = Regex.Replace(s, @"\s+", " ").Trim()

let private squashedSource = squash guardSource

// ---- the mirrored decision layer ---------------------------------------------------------------
//
// One record per input the real predicates read. Every field is something the script derives from the
// world (feed, env, git, props); nothing here is invented.

let private uiAxis = "FsGgUiVersion"
let private gameAxis = "FsGgGameVersion"
let private audioAxis = "FsGgAudioVersion"

type private World =
    { /// FS.GG.UI.* ids the feed does NOT carry at $(FsGgUiVersion). Empty is the normal state.
      PendingUi: string list
      /// FS_GG_VERSION_COHERENCE_RELEASE_LANE=1 — set job-wide by release.yml, and it KILLS the waiver.
      ReleaseLane: bool
      /// Did THIS commit bump $(FsGgUiVersion)? `false` for every ordinary commit that merely inherits it.
      BumpedUiHere: bool
      /// Axes whose pinned packages the feed does not carry at their pinned version.
      UnpublishedAxes: string list
      /// A prerelease pinned DIRECTLY in an axis literal, on a stable (non-preview) template.
      DirectPrerelease: bool }

let private clean =
    { PendingUi = []
      ReleaseLane = false
      BumpedUiHere = false
      UnpublishedAxes = []
      DirectPrerelease = false }

/// `releasePending` — mirrors the script exactly, conjunct for conjunct, in order.
let private releasePending (w: World) =
    not w.PendingUi.IsEmpty && not w.ReleaseLane && w.BumpedUiHere

/// `feedFailures`' existence rule, reduced to the question that matters: which axes still get checked?
/// The waiver suppresses `pin-not-published` for the UI axis ALONE — `not (waiveUi && axis = uiAxis)`.
let private pinNotPublishedFor (w: World) =
    let waiveUi = releasePending w
    w.UnpublishedAxes |> List.filter (fun axis -> not (waiveUi && axis = uiAxis))

/// The guard's exit-code contract: 0 coherent · 1 named drift · 2 fail-closed (guard could not decide).
let private exitCode (w: World) =
    let failures =
        pinNotPublishedFor w
        @ (if w.DirectPrerelease then [ "prerelease-in-scaffolded-graph" ] else [])

    if failures.IsEmpty then 0 else 1

[<Tests>]
let tests =
    testList
        "issue-544 template-payload RELEASE-PENDING waiver bounds"
        [
          // ---- the table #544 asks to be frozen -------------------------------------------------
          //
          // Every row is a verdict the waiver must reach. The two that matter most are the two that
          // must still be RED: an ordinary commit inheriting an unpublished pin, and a Game/Audio axis
          // bumped to a version nobody published. Those are the fail-open cases.

          test "published pin, no bump: coherent, and the waiver never engages" {
            let w = clean
            Expect.isFalse (releasePending w) "nothing is pending, so there is no release window to be in"
            Expect.equal (exitCode w) 0 "the ordinary, everyday state of the repo"
          }

          test "THIS commit bumps the pin and the feed lacks it: RELEASE-PENDING, exit 0" {
            let w =
                { clean with
                    PendingUi = [ "FS.GG.UI.Scene" ]
                    BumpedUiHere = true
                    UnpublishedAxes = [ uiAxis ] }

            Expect.isTrue (releasePending w) "bump + absent from the feed + not the release lane = the window"
            Expect.isEmpty (pinNotPublishedFor w) "pin-not-published is suppressed for the UI axis"
            Expect.equal (exitCode w) 0 "a release PR can pass its own gate — that is the whole point of #506"
          }

          // THE FAIL-OPEN, #1. Without `bumpedInCommitUnderTest` the waiver would key on "the feed lacks
          // it", which is true of a typo'd or stale pin on every ordinary commit thereafter.
          test "an ordinary commit inheriting an unpublished pin is NOT waived — exit 1" {
            let w =
                { clean with
                    PendingUi = [ "FS.GG.UI.Scene" ]
                    BumpedUiHere = false // the bump was some EARLIER commit, or never happened
                    UnpublishedAxes = [ uiAxis ] }

            Expect.isFalse (releasePending w) "no bump in THIS commit ⇒ not a release window ⇒ no waiver"
            Expect.equal (pinNotPublishedFor w) [ uiAxis ] "the pin is stale or typo'd, and must be named"
            Expect.equal (exitCode w) 1 "a pin the feed does not carry is drift on any commit but the bump"
          }

          // THE FAIL-OPEN, #2, and the worst of them. Bumping $(FsGgGameVersion) here publishes NOTHING —
          // that package ships from its own repo — so an absent Game/Audio pin is a real defect on every
          // commit, release window or not. A waiver that keyed on the axis being bumped would sail past
          // it: #235, the defect this script was written for.
          test "a bumped-but-unpublished Game/Audio axis is NEVER waived, even inside the release window" {
            for axis in [ gameAxis; audioAxis ] do
                let w =
                    { clean with
                        PendingUi = [ "FS.GG.UI.Scene" ] // a genuine UI release IS in flight...
                        BumpedUiHere = true
                        UnpublishedAxes = [ uiAxis; axis ] } // ...and this axis is also unpublished

                Expect.isTrue (releasePending w) "the release window is genuinely open"

                Expect.equal (pinNotPublishedFor w) [ axis ]
                    (sprintf
                        "the window waives the UI axis ONLY — $(%s) is still checked. Waiving it would ship a template pinning a package nobody published (#235)"
                        axis)

                Expect.equal (exitCode w) 1 (sprintf "$(%s) unpublished is drift, window or not" axis)
          }

          // The release lane gates the actual publish, and there the tags DO exist. A waiver that
          // survived into it would let `release.yml` publish a coherent-set member that is not there.
          test "FS_GG_VERSION_COHERENCE_RELEASE_LANE=1 kills the waiver — exit 1" {
            let w =
                { clean with
                    PendingUi = [ "FS.GG.UI.Scene" ]
                    BumpedUiHere = true
                    ReleaseLane = true
                    UnpublishedAxes = [ uiAxis ] }

            Expect.isFalse (releasePending w) "the lane that gates the publish gets no waiver"
            Expect.equal (exitCode w) 1 "by publish time the version must really be on the feed"
          }

          // RELEASE-PENDING skips the RESTORE, and with it the transitive half of this rule. The DIRECT
          // half needs no graph — it is visible in the axis literal — so it must keep running, or a
          // stable release ships a template pinning a prerelease and exits 0.
          test "a directly pinned prerelease still reds inside the release window — exit 1" {
            let w =
                { clean with
                    PendingUi = [ "FS.GG.UI.Scene" ]
                    BumpedUiHere = true
                    UnpublishedAxes = [ uiAxis ]
                    DirectPrerelease = true }

            Expect.isTrue (releasePending w) "the window is open, and it suppresses pin-not-published…"
            Expect.isEmpty (pinNotPublishedFor w) "…which it does"
            Expect.equal (exitCode w) 1 "…but the cheap half of prerelease-in-scaffolded-graph still fires"
          }

          // Exhaustive, because a conjunction is exactly the thing a well-meaning edit loosens by one
          // term. Only ONE of the eight worlds may open the window.
          test "the waiver opens in exactly one of the eight possible worlds" {
            let worlds =
                [ for pending in [ true; false ] do
                      for lane in [ true; false ] do
                          for bumped in [ true; false ] do
                              yield
                                  { clean with
                                      PendingUi = (if pending then [ "FS.GG.UI.Scene" ] else [])
                                      ReleaseLane = lane
                                      BumpedUiHere = bumped } ]

            let opened = worlds |> List.filter releasePending

            Expect.equal opened.Length 1
                "exactly one world is a release window: pin pending, NOT the release lane, bumped by THIS commit"

            let w = opened.Head
            Expect.isNonEmpty w.PendingUi "…pending"
            Expect.isFalse w.ReleaseLane "…not the release lane"
            Expect.isTrue w.BumpedUiHere "…bumped here"
          }

          // ---- source lockstep: the mirror above may not silently drift from the script ----------
          //
          // This is the part that makes the rest of this file worth anything. Everything above tests a
          // COPY of the guard's decision layer; if the guard changes and the copy does not, the copy
          // still passes and #544 is back — one level up, and quieter. So assert the real source still
          // has the shape modelled here. These are not style checks: each one pins a load-bearing term
          // whose removal is a fail-open.

          // `Expect.isTrue`, not `Expect.stringContains`: the latter prints the whole SUBJECT on failure,
          // and the subject here is an 840-line script — an 88KB failure message nobody reads. These
          // assertions fail with a sentence that says what broke and what to do.
          test "source lockstep: releasePending is still the exact three-conjunct predicate modelled here" {
            Expect.isTrue
                (squashedSource.Contains
                    "let releasePending (pending: string list) = not pending.IsEmpty && not releaseLane && bumpedInCommitUnderTest propsRel uiAxis")
                "the waiver's conjunction changed. Every term is load-bearing (#544): `not pending.IsEmpty` (there is a real absence), `not releaseLane` (we are not gating the publish), `bumpedInCommitUnderTest` (THIS commit caused it). Re-derive the bounds, then update the World model in this file."
          }

          test "source lockstep: the waiver is still confined to the UI axis in feedFailures" {
            Expect.isTrue
                (squashedSource.Contains "if not (waiveUi && axis = uiAxis) then")
                "the axis guard changed. Widening the waiver past $(FsGgUiVersion) waives an unpublished FS.GG.Game/Audio pin and exits 0 — #235, which this script exists to catch. If this MUST change, update the Game/Audio test above and explain why in the PR."
          }

          test "source lockstep: the direct-prerelease half still runs when the restore is skipped" {
            // It must not be nested under a release-window branch. The cheap half needs no graph.
            Expect.isTrue
                (squashedSource.Contains "let directPrereleaseFailures (i: Inputs) : Failure list =")
                "directPrereleaseFailures is the half of prerelease-in-scaffolded-graph that survives the window — it must still exist"

            Expect.isTrue
                (Regex.IsMatch(guardSource, @"directPrereleaseFailures\s+\w+"))
                "…and it must still be CALLED, or the window ships a template pinning a prerelease"
          }

          test "source lockstep: bumpedInCommitUnderTest still fails CLOSED when git cannot answer" {
            // A shallow clone has no HEAD~1. Defaulting that to "not bumped" would be the safe direction
            // for the waiver but the wrong one for the guard: it turns an unanswerable question into a
            // red-for-the-wrong-reason. The script raises GuardError ⇒ exit 2, which is a THIRD verdict:
            // "the guard could not decide", never confused with "the repo is incoherent".
            let fn =
                Regex.Match(guardSource, @"let bumpedInCommitUnderTest[\s\S]{0,600}?raise \(GuardError")

            Expect.isTrue fn.Success
                "bumpedInCommitUnderTest must still raise GuardError (⇒ exit 2) when `git diff HEAD~1 HEAD` fails — a shallow clone must not silently answer 'not bumped'"
          }
        ]
