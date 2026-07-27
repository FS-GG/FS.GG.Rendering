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
//     feedExistenceFailures:  if not (waiveUi && axis = uiAxis) then <check existence>
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
let private contractsAxis = "FsGgContractsVersion"

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

/// `feedExistenceFailures`' rule, reduced to the question that matters: which axes still get checked?
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
          test "a bumped-but-unpublished Game/Audio/Contracts axis is NEVER waived, even inside the release window" {
            for axis in [ gameAxis; audioAxis; contractsAxis ] do
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

          // ---- #1102: the SPLIT VERDICT, and the lane `pin-lags-feed` may not return to -----------
          //
          // The decision (FS.GG.Rendering#1102, 2026-07-27) is that a feed-COMPARING verdict may not
          // block a PR whose commits did not change. `pin-lags-feed` is `f(tree, WORLD)`: an upstream
          // FS.GG.Contracts publish reddened every open PR in this repo on 2026-07-27 with nobody's
          // commit to blame, and the remedy lived in a file none of those items had declared.
          //
          // Everything else in this guard stays merge-blocking, INCLUDING the feed-reading existence
          // rule — `pin-not-published` accuses the commit that wrote the pin, and no upstream publish
          // can flip it. That distinction is the whole decision, and it is one `let` away from being
          // undone by somebody tidying two similar-looking rules back into one function. So it is
          // pinned here, where the rest of this file's bounds are.

          test "source lockstep: pin-lags-feed is declared in stalenessFailures and NOWHERE else" {
              let ruleDecl = Regex.Matches(guardSource, @"Rule\s*=\s*""pin-lags-feed""")

              Expect.equal ruleDecl.Count 1
                  "`pin-lags-feed` must be yielded from exactly one place. A second copy is how a rule quietly re-enters a lane it was removed from (#1102)."

              // Take the text of `stalenessFailures` up to the next top-level `let`/`//` banner and
              // assert the rule is inside it. Substring, not a parser: the point is only that the
              // declaration sits under this binding rather than under `feedExistenceFailures`.
              let sweepFn =
                  Regex.Match(guardSource, @"let stalenessFailures \(i: Inputs\) : Failure list =[\s\S]*?\n// ----")

              Expect.isTrue sweepFn.Success "stalenessFailures must still exist as the sweep lane's rule set"
              Expect.isTrue (sweepFn.Value.Contains "pin-lags-feed")
                  "`pin-lags-feed` must live in `stalenessFailures` — the SCHEDULED lane. If it moved back into the PR lane, an upstream publish reds PRs nobody's commit broke (#1102)."
          }

          test "source lockstep: the PR restore lane calls feedExistenceFailures, never stalenessFailures" {
              Expect.isTrue
                  (squashedSource.Contains "let feed = feedExistenceFailures waiveUi i")
                  "the restore lane must ask for EXISTENCE only. `feedExistenceFailures` is the half that accuses the commit under test; staleness is the sweep's (#1102)."

              // Exactly one call site, and it is the sweep branch's.
              let calls = Regex.Matches(guardSource, @"stalenessFailures i\b")

              Expect.equal calls.Count 1
                  "`stalenessFailures` must be called from exactly one place — the `stalenessSweep` branch of `main`. A second caller is the rule leaking back into a PR lane (#1102)."

              Expect.isTrue
                  (squashedSource.Contains "if stalenessSweep then")
                  "…and that one caller must be guarded by `stalenessSweep`, the env var only the scheduled workflow sets"
          }

          test "source lockstep: the two lanes are refused together rather than silently ordered" {
              Expect.isTrue
                  (squashedSource.Contains "if stalenessSweep && live then raise (")
                  "setting both FS_GG_TEMPLATE_PIN_STALENESS_SWEEP=1 and FS_GG_RUN_TEMPLATE_PAYLOAD_RESTORE=1 must fail CLOSED. A precedence rule would decide in secret whether `pin-lags-feed` ran on a PR, which is the one question #1102 exists to answer out loud."
          }

          // And the other end of the contract: the workflow that owns the lane. A split that exists only
          // in the script is a rule that runs NOWHERE — which would silently reintroduce the #235
          // staleness blindness the rule was written for, while looking like a fix.
          test "the scheduled sweep exists, drives the sweep lane, and cannot red a PR" {
              let sweepPath = Path.Combine(root, ".github", "workflows", "template-pin-staleness-sweep.yml")

              Expect.isTrue (File.Exists sweepPath)
                  "`pin-lags-feed` left the PR lane, so something must still run it. Deleting .github/workflows/template-pin-staleness-sweep.yml does not simplify the gate — it restores the #235 silence."

              let wf = File.ReadAllText sweepPath

              Expect.stringContains wf "FS_GG_TEMPLATE_PIN_STALENESS_SWEEP: '1'"
                  "the sweep must actually drive the staleness lane"

              Expect.stringContains wf "- cron:" "the sweep must be scheduled — that is the lane it moved to"

              // The PR trigger exists to exercise the renderer; the verdict step must exclude it, or
              // this workflow reds a PR because somebody else published a package — #1102, verbatim, on
              // the workflow that abolished it.
              Expect.stringContains
                  wf
                  "if: steps.sweep.outputs.rc != '0' && github.event_name != 'pull_request'"
                  "the Verdict step must not fail a `pull_request` run"

              // The finding is only work if a worker can pick it up: no touch-set ⇒ `take`/`batch`
              // refuse it (FS-GG/.github#442); no class ⇒ the row reads as unclassed (#1651).
              Expect.stringContains wf "Paths:" "the filed item must declare a touch-set"
              Expect.stringContains wf "Class: defect" "the filed item must declare a class"

              // gate.yml must not have grown a copy of the rule back.
              let gate = File.ReadAllText(Path.Combine(root, ".github", "workflows", "gate.yml"))

              Expect.isFalse
                  (gate.Contains "FS_GG_TEMPLATE_PIN_STALENESS_SWEEP")
                  "the PR gate must never run the staleness lane (#1102)"
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
