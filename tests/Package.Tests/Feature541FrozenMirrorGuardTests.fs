module Feature541FrozenMirrorGuardTests

// #541 — the guard that stops a Rendering PR editing a frozen skill mirror it does not own.
//
// THIS FILE GUARDS THE GUARD, and the first version of it did not. Every assertion was a
// `stringContains` on the script's source text, so all four passed against a seven-line stub that kept
// the magic strings and printed "everything is fine, trust me". They guarded four string literals.
//
// What is asserted here now is what can be checked OFFLINE and would actually rot:
//   * the waiver digests still match the files they waive — the single most likely thing to go stale,
//     and the exact canonical-vs-drifted copy-paste that would make a waiver never fire;
//   * every mirrored body still exists (a DELETED mirror is as much a break as an edited one);
//   * the guard is wired into the REQUIRED job, not merely mentioned in the workflow file.
//
// The digest comparison against the ORG REGISTRY stays in the script: it needs the network and a token,
// and a mock of it here would assert that the mock works.

// #720 — AND THE VERDICT IS NOW TESTED, not merely described.
//
// The paragraph above says the digest comparison "stays in the script: it needs the network and a token".
// That was true of the COMPARISON and it was never true of the DECISION — of working out WHY a mirror
// differs (somebody edited it here? the owning repo moved the canonical?) and what to tell the reader to
// do about it. That half needs no network at all, and while it sat in the script it went untested — which
// is how it came to report a canonical that MOVED as a mirror somebody EDITED, in capitals, and then
// forbid the re-freeze that was the actual fix. On a pristine `main`, inside the REQUIRED gate.
//
// `scripts/FrozenMirrorVerdict.fs` is that half, split out and compiled in here. So the truth table below
// is the real one the guard runs, the git probe is driven against real throwaway git repos, and — the
// test that would have caught #720 — the ERROR TEXT of each verdict is asserted. No mocks anywhere.

open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
open FsGg.Governance.FrozenMirrorVerdict

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relative: string) =
    Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))

let private scriptRelative = "scripts/check-frozen-mirrors.fsx"
let private script = File.ReadAllText(repositoryPath scriptRelative)

let private sha256Of (path: string) =
    use sha = System.Security.Cryptography.SHA256.Create()

    sha.ComputeHash(File.ReadAllBytes path)
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

/// The waivers the script declares: `{ Id = "…"; DriftedSha = "…"; … }`.
let private waivers =
    Regex.Matches(script, @"Id\s*=\s*""(?<id>[^""]+)""\s*\n\s*DriftedSha\s*=\s*""(?<sha>[0-9a-f]{64})""")
    |> Seq.cast<Match>
    |> Seq.map (fun m -> m.Groups.["id"].Value, m.Groups.["sha"].Value)
    |> List.ofSeq

/// The script with its `//` comment lines stripped, so prose about waivers cannot be counted as one.
let private scriptCode =
    script.Split '\n'
    |> Array.filter (fun line -> not (line.TrimStart().StartsWith "//"))
    |> String.concat "\n"

/// A crude, INDEPENDENT count of the same records — one `DriftedSha = "…"` assignment per waiver.
///
/// The structured parse above can fail in two ways that look identical from here: the script genuinely
/// declares no waivers, or the regex has rotted against a shape the script now uses. Counting the same
/// thing a second way tells them apart — the trick the guarded script already plays on the org registry
/// ("A PARTIAL parse is not a pass either": it counts the YAML rows and insists the structured parse
/// agrees). Same overloaded-absence bug, same treatment.
///
/// INDEPENDENT is the load-bearing word, and the first version of this was not (#640). It anchored the
/// count at the start of a line (`^\s*DriftedSha`) — the same assumption the structured regex makes — so
/// a waiver written `{ Id = "…"; DriftedSha = "…"` defeated BOTH, both counted zero, `0 = 0` compared
/// equal, and the vacuous green this cross-check exists to prevent came back wearing its badge. A
/// backstop that shares the parser's blind spot is not a backstop. This one matches the assignment
/// wherever it sits.
let private driftedShaAssignments =
    Regex.Matches(scriptCode, @"DriftedSha\s*=\s*""").Count

/// Both readers above scrape one field NAME out of F# source. That is the last assumption they share, and
/// it is the last way they can both go blind at once: rename `DriftedSha` and BOTH count zero, `0 = 0`
/// compares equal, and a waiver list full of unchecked pins sails through green. Cross-checking two parsers
/// against each other cannot catch what neither can see, so the schema itself is asserted rather than
/// assumed — a rename now has to come here and say so.
let private declaresDriftedShaField =
    Regex.IsMatch(script, @"type\s+Waiver\s*=(.|\n)*?DriftedSha\s*:\s*string")

/// The skills this repo MIRRORS (as opposed to `NoCounterpart`).
///
/// Read from the VALUE, not scraped out of the script's source with a regex (#738). It used to be the
/// latter, because the table lived in the `.fsx` and a test cannot `#load` one — but the table is now
/// pinned in `FrozenMirrorVerdict.fs`, which this project compiles in, so the test can assert the thing
/// the guard actually runs on instead of a regex's opinion of it. One less parser to rot.
let private mirrored =
    foreignSkills
    |> List.filter (fun skill -> skill.Disposition = Mirrored)
    |> List.map (fun skill -> skill.Id)

// ---- #720: driving the real verdict, against real git ------------------------------------------

/// Four distinct digests. Only their EQUALITY matters to `decide`, so they are named for the role they
/// play and never for a value — a test that hard-codes a real body's sha would go red the day that body
/// is legitimately re-frozen, which is the failure mode #640 already fixed once in this file.
let private localSha = String.replicate 64 "1"
let private canonicalSha = String.replicate 64 "2"
let private registrySha = String.replicate 64 "3"
let private baseSha = String.replicate 64 "4"

let private describeDrift verdict baseline =
    describe
        verdict
        "fs-gg-audio"
        "fs-gg-game"
        "FS.GG.Game/template/product-skills/fs-gg-audio/SKILL.md"
        "template/product-skills/fs-gg-audio/SKILL.md"
        baseline
        localSha
        canonicalSha
        None

let private git (dir: string) (args: string list) =
    let psi = ProcessStartInfo "git"
    psi.WorkingDirectory <- dir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add

    match Process.Start psi with
    | null -> failwith "could not start `git`"
    | proc ->
        use proc = proc
        let out = proc.StandardOutput.ReadToEnd()
        let err = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        if proc.ExitCode <> 0 then
            failwithf "git %s failed in %s: %s%s" (String.concat " " args) dir out err

/// `Path.GetDirectoryName` is nullable under this project's nullness checking, and every caller here has
/// just built the path from a known-nested relative, so a null parent means the test itself is wrong.
let private ensureParent (path: string) =
    match Path.GetDirectoryName path with
    | null -> failwithf "no parent directory for %s" path
    | parent -> Directory.CreateDirectory parent |> ignore

let private sha256OfText (text: string) =
    use sha = System.Security.Cryptography.SHA256.Create()

    sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes text)
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

/// A REAL git repo, on a real `main`, thrown away afterwards. Nothing is mocked: the probe shells out to
/// the same `git` the guard does, which is the only way to know it reads real trees correctly.
let private withRepo (build: string -> unit) (test: string -> unit) =
    let dir = Path.Combine(Path.GetTempPath(), "fsgg-frozen-" + System.Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory dir |> ignore

    try
        git dir [ "init"; "-q"; "." ]
        // Not `init -b main`: older `git` does not have it, and the fallback in `baselineOf` looks for
        // `origin/main` and then `main`.
        git dir [ "symbolic-ref"; "HEAD"; "refs/heads/main" ]
        git dir [ "config"; "user.email"; "t@example.invalid" ]
        git dir [ "config"; "user.name"; "t" ]
        // A developer with commit signing on globally would otherwise fail every commit below.
        git dir [ "config"; "commit.gpgsign"; "false" ]
        build dir
        test dir
    finally
        try
            Directory.Delete(dir, true)
        with _ ->
            ()

let private mirrorRelative = "template/product-skills/fs-gg-audio/SKILL.md"

let private commitMirror (dir: string) (content: string) =
    let path = Path.Combine(dir, mirrorRelative.Replace('/', Path.DirectorySeparatorChar))
    ensureParent path
    File.WriteAllText(path, content)
    git dir [ "add"; "-A" ]
    git dir [ "commit"; "-qm"; "the mirror" ]

[<Tests>]
let issue720FrozenMirrorVerdictTests =
    testList
        "Issue720 frozen-mirror verdict"
        [
          // THE TRUTH TABLE. `decide` is pure, so this is exhaustive rather than representative.
          test "a body byte-identical to the canonical is IN SYNC, whatever the registry says" {
              // The BODY is the oracle, not the registry's `sha256` — that is a CACHE and it lags (#629).
              // A lagging registry must never manufacture drift out of a mirror that is already correct.
              for baseline in [ UnchangedHere baseSha; EditedHere baseSha; AddedHere; Unknown "no base" ] do
                  Expect.equal
                      (decide baseline canonicalSha registrySha canonicalSha)
                      InSync
                      "a mirror that hashes exactly its canonical is in sync — the registry is a lagging cache and gets no vote"
          }

          test "a body this change EDITED or ADDED is MIRROR EDITED — git outranks the registry" {
              Expect.equal
                  (decide (EditedHere baseSha) localSha registrySha canonicalSha)
                  MirrorEdited
                  "the body differs from its copy at the merge base, so THIS change edited it (#541)"

              Expect.equal
                  (decide AddedHere localSha registrySha canonicalSha)
                  MirrorEdited
                  "the body did not exist at the merge base, so this change added it"

              // The registry signal must not be able to TALK THE GUARD OUT OF a conviction git can prove.
              // A canonical that moved does not change the merge base, so an edit here is still an edit
              // here even when the canonical moved too — and the author still has to route it upstream.
              Expect.equal
                  (decide (EditedHere baseSha) registrySha registrySha canonicalSha)
                  MirrorEdited
                  "git says this change edited the body; a registry that happens to agree with the result does not un-edit it"
          }

          test "a body this change did NOT touch, matching the lagging registry, is CANONICAL MOVED" {
              // #720's case, exactly: FS.GG.Game#279 moved the canonical, the registry had not caught up,
              // and the mirror still hashed what the registry recorded. Nobody edited anything.
              Expect.equal
                  (decide (UnchangedHere localSha) localSha localSha canonicalSha)
                  CanonicalMoved
                  "body == merge base (nobody here touched it) and body == registry != canonical: the canonical moved underneath us"

              // Same conclusion when git cannot answer at all — a shallow clone must not turn a moved
              // canonical into an accusation. It is safe: for this to be wrong somebody would have to have
              // hand-edited the mirror to be byte-identical to the LAGGED canonical, and re-freezing onto
              // the current one is still right in that case.
              Expect.equal
                  (decide (Unknown "shallow clone") localSha localSha canonicalSha)
                  CanonicalMoved
                  "with git blind, a body that still hashes the registry's record of a canonical that has since moved is a moved canonical"
          }

          test "drift this change did not introduce, that the digests cannot explain, is UNATTRIBUTED" {
              // The state the issue's own two-way split misses, and the reason this is three verdicts and
              // not two: once the nightly bot reconciles the registry, `registry == canonical` and the
              // digest signal goes SILENT. A mirror that is merely behind then looks exactly like one
              // somebody edited here long ago. Folding this into either neighbour would reintroduce #720
              // facing the other way — either accusing the innocent again, or telling someone to re-freeze
              // over work that would be destroyed.
              Expect.equal
                  (decide (UnchangedHere localSha) localSha canonicalSha canonicalSha)
                  DriftUnattributed
                  "registry has caught up with the moved canonical, so it can no longer witness anything: cause undecidable"

              Expect.equal
                  (decide (UnchangedHere localSha) localSha registrySha canonicalSha)
                  DriftUnattributed
                  "the body matches neither the registry nor the canonical, and git says this change did not do it"

              Expect.equal
                  (decide (Unknown "no merge base") localSha registrySha canonicalSha)
                  DriftUnattributed
                  "git blind AND the registry inconclusive: the guard must say so, not guess"
          }

          // THE TEST THAT WOULD HAVE CAUGHT #720. The verdict being right is worth nothing if the text
          // still says the opposite — and for the whole life of this bug the CODE knew the canonical could
          // move (the script's own header said so) while the OUTPUT went on asserting an edit.
          test "CANONICAL MOVED does not accuse anyone of editing, and does not forbid the re-freeze" {
              let message = describeDrift CanonicalMoved (UnchangedHere localSha)

              Expect.stringContains message "CANONICAL MOVED" "the verdict must lead with what actually happened"

              Expect.isFalse
                  (message.Contains "FROZEN MIRROR EDITED")
                  "this is the #720 bug verbatim: nobody edited the mirror, and 'FROZEN MIRROR EDITED' is the first thing the reader sees. It is false, and it is read by every worker in a wedged repo."

              Expect.isFalse
                  (message.Contains "do NOT re-freeze")
                  "re-freezing IS the remedy for a moved canonical — it deletes nothing, because the new content IS the canonical. Forbidding it here is what left the repo wedged with no legal way out."

              Expect.stringContains
                  message
                  "nobody here edited"
                  "the reader is almost certainly someone whose unrelated PR just went red; say so plainly"

              // The remedy must be RUNNABLE, not merely described: this fires on a pristine `main` inside a
              // required gate, so the reader is unwedging a repo, not studying a message.
              Expect.stringContains
                  message
                  "gh api repos/FS-GG/FS.GG.Game/contents/template/product-skills/fs-gg-audio/SKILL.md --jq .content | base64 -d > template/product-skills/fs-gg-audio/SKILL.md"
                  "print the exact re-freeze command; a remedy the reader has to assemble is one they will get wrong while the repo is red"
          }

          test "MIRROR EDITED keeps every bit of #541's strictness" {
              let message = describeDrift MirrorEdited (EditedHere baseSha)

              Expect.stringContains message "FROZEN MIRROR EDITED" "the #541 case is still named exactly as it was"

              Expect.stringContains
                  message
                  "DO NOT REVERT YOUR WORK"
                  "correct-but-unowned work must not be thrown away — three authors' edits were nearly lost this way (#541)"

              Expect.stringContains
                  message
                  "do NOT re-freeze"
                  "re-freezing over a genuine local edit SILENTLY DELETES it (FS.GG.Game#163 would have lost 25 lines). #720 must not have loosened this."

              Expect.stringContains
                  message
                  "DO NOT ADD A WAIVER FOR YOURSELF"
                  "a waiver records drift that predates the guard; it is not a way to land new drift"

              // The evidence git gave us, rather than a bare assertion that the author did something.
              Expect.stringContains message (baseSha.Substring(0, 12)) "name the merge-base digest the body diverged FROM — the reader can then see what they changed"
          }

          test "an UNATTRIBUTED mirror offers both readings and commits to neither" {
              let message = describeDrift DriftUnattributed (UnchangedHere localSha)

              Expect.isFalse
                  (message.Contains "FROZEN MIRROR EDITED")
                  "the guard cannot tell who caused this drift, so it must not accuse the reader of causing it"

              Expect.stringContains
                  message
                  "CANNOT BE READ OFF THE DIGESTS"
                  "say plainly that the cause is undecidable from here — a guess in either direction is how work gets destroyed or an innocent gets blamed"

              Expect.stringContains message "re-freeze" "reading (a): the canonical moved and the mirror is behind"

              Expect.stringContains
                  message
                  "SILENTLY DELETE"
                  "reading (b): somebody edited it here, and re-freezing would destroy that work — the reader must check before choosing"
          }

          // THE DELIBERATE CHANGE THE OLD TEST DEMANDED (#738).
          //
          // This used to be `every drift verdict reds the gate; only IN SYNC is green`, pinned so that
          // moving `CanonicalMoved` out of the required lane "has to be a deliberate change, made here,
          // with this comment in front of it." This is that change, and it is deliberate. The old test's
          // REASON — "a verdict that does not red the gate is a verdict nobody acts on" — is preserved
          // exactly: every drift verdict still reds a gate. What changed is WHICH gate, and the reason is
          // that a verdict about ANOTHER REPO'S `main` may not decide whether THIS commit merges
          // (ADR-0105). Nothing became a warning; nothing stopped failing.
          test "the REQUIRED lane reds on what THIS change did, and on nothing else" {
              Expect.isFalse (failsIn Required InSync) "a mirror that matches its canonical is not a finding"

              // #541's case, and the only verdict `git` can prove from the commit alone. It keeps every
              // tooth it had: still red, still in the required, admin-enforced gate.
              Expect.isTrue
                  (failsIn Required MirrorEdited)
                  "MIRROR EDITED is a fact about THIS change's diff (git, against the merge base). It is deterministic, it is #541's entire subject, and demoting it would be the fail-open this whole guard exists to prevent."

              // Both of these are the SAME STALE MIRROR, at two moments in time — see `failsIn`. A commit
              // that is green today would go red tomorrow because FS.GG.Game merged something, which is
              // exactly what wedged the repo in #714.
              for verdict in [ CanonicalMoved; DriftUnattributed ] do
                  Expect.isFalse
                      (failsIn Required verdict)
                      $"{verdict}'s subject is ANOTHER repo's `main`. In the required, enforce_admins gate it makes a green commit red with no change here and no way for a PR here to prevent it — nine 'main is RED, nothing can merge' incidents in three days (#696). ADR-0105 forbids it."
          }

          // ...AND IT IS STILL A RED SOMEWHERE. This is the half that makes the demotion a LANE SPLIT and
          // not a fail-open, and it is the assertion to break if somebody ever "simplifies" the freshness
          // lane into a warning. A stale mirror ships a `--profile game` scaffold a skill FS.GG.Game has
          // already moved on from; it must never merge quietly.
          test "the FRESHNESS lane reds on every drift, so nothing was demoted into silence" {
              Expect.isFalse (failsIn Freshness InSync) "a mirror that matches its canonical is not a finding"

              for verdict in [ MirrorEdited; CanonicalMoved; DriftUnattributed ] do
                  Expect.isTrue
                      (failsIn Freshness verdict)
                      $"{verdict} is drift, and drift that reds NO lane is drift nobody fixes. The point of #738 is that this red cannot take the merge button away — not that it stops being a red."
          }

          // THE TRAP #738 NAMED IN ADVANCE: "`describe` emits a literal `::error` for every drift verdict,
          // and an `::error` annotation on a verdict that no longer reds the gate is the same class of lie
          // #720 removed — the ANNOTATION LEVEL has to move with the exit code."
          //
          // It does, and BY CONSTRUCTION rather than by a matching rule that could drift: `describe` is
          // reachable only from the freshness lane, which reds on every verdict it can produce. So every
          // `::error` it prints is backed by a non-zero exit in the lane that printed it. The required lane
          // cannot reach `describe` at all — it has no `canonical` to pass it — and prints its own message
          // for the only verdict it can produce.
          test "every ::error a lane prints is one that lane actually fails on" {
              for verdict in [ MirrorEdited; CanonicalMoved; DriftUnattributed ] do
                  let message = describeDrift verdict (UnchangedHere localSha)

                  Expect.stringContains
                      message
                      "::error"
                      $"{verdict} is annotated as an error by `describe`..."

                  Expect.isTrue
                      (failsIn Freshness verdict)
                      $"...so the lane that prints it MUST fail on it. An ::error over a green exit is a gate that cries wolf, and it teaches exactly one lesson: that red is noise."
          }

          // ---- the git probe, against real trees ----

          test "git probe: a body this change did not touch reads UNCHANGED — the pristine-main case" {
              // The #720 wedge fires on a clean `main`, where the merge base IS `HEAD`. That must come back
              // `UnchangedHere`, or the guard convicts every worker in the repo of an edit none of them made.
              withRepo
                  (fun dir -> commitMirror dir "the canonical body\n")
                  (fun dir ->
                      let sha = sha256OfText "the canonical body\n"

                      Expect.equal
                          (baselineOf dir mirrorRelative sha)
                          (UnchangedHere sha)
                          "the working tree matches the merge base, so this change did not touch the mirror")
          }

          test "git probe: a body this change edited reads EDITED, and names the base digest" {
              withRepo
                  (fun dir ->
                      commitMirror dir "the canonical body\n"
                      // Uncommitted working-tree edit — the case a PR author is actually in.
                      File.WriteAllText(
                          Path.Combine(dir, mirrorRelative.Replace('/', Path.DirectorySeparatorChar)),
                          "edited locally\n"
                      ))
                  (fun dir ->
                      Expect.equal
                          (baselineOf dir mirrorRelative (sha256OfText "edited locally\n"))
                          (EditedHere(sha256OfText "the canonical body\n"))
                          "the body differs from the merge base, and the probe reports the digest it diverged from")
          }

          test "git probe: a body absent from the merge base reads ADDED, not UNKNOWN" {
              // git ANSWERED — "there was nothing there" — so this is knowledge, not ignorance. Reading it
              // as `Unknown` would drop a newly-vendored mirror into the registry-only path and lose the
              // conviction; `.github#486` is explicit that vendoring one in is itself the break.
              withRepo
                  (fun dir -> commitMirror dir "some other file's content\n" |> ignore)
                  (fun dir ->
                      // A DIFFERENT path, never committed, but present on disk — which is the shape git
                      // reports as "exists on disk, but not in <commit>" rather than "does not exist".
                      let added = "template/product-skills/fs-gg-newly-vendored/SKILL.md"
                      let path = Path.Combine(dir, added.Replace('/', Path.DirectorySeparatorChar))
                      ensureParent path
                      File.WriteAllText(path, "vendored in\n")

                      Expect.equal
                          (baselineOf dir added (sha256OfText "vendored in\n"))
                          AddedHere
                          "the path was not in the merge base, so this change added it")
          }

          test "git probe: no merge base reads UNKNOWN — it never guesses UNCHANGED" {
              // Fail towards "I do not know", never towards "nobody edited it". `Unknown` still lets a
              // moved canonical be recognised via the registry, but it can never manufacture the innocence
              // of a body git could not read.
              withRepo
                  (fun dir ->
                      commitMirror dir "the canonical body\n"
                      // Leave `main` behind entirely: no `origin/main`, no `main`, so no merge base exists.
                      git dir [ "checkout"; "-qb"; "detached-work" ]
                      git dir [ "branch"; "-D"; "main" ])
                  (fun dir ->
                      match baselineOf dir mirrorRelative (sha256OfText "the canonical body\n") with
                      | Unknown _ -> ()
                      | other -> failtestf "expected Unknown when there is no merge base, got %A" other)
          }

          test "git probe: a tree git cannot read at all degrades to UNKNOWN, it does not throw" {
              // The probe's failure mode matters as much as its answers. `git` missing from PATH THROWS
              // rather than returning a code, and an unhandled exception would take the guard down with a
              // stack trace — in front of the one reader who is least able to deal with it, since this
              // guard reds a REQUIRED gate and its audience is a repo that is already wedged. Every way the
              // probe can fail to answer must arrive as `Unknown`, where the registry signal can still
              // recognise a moved canonical.
              let notARepo = Path.Combine(Path.GetTempPath(), "fsgg-not-a-repo-" + System.Guid.NewGuid().ToString("N"))
              Directory.CreateDirectory notARepo |> ignore

              try
                  match baselineOf notARepo mirrorRelative localSha with
                  | Unknown _ -> ()
                  | other -> failtestf "a directory that is not a git checkout must read Unknown, got %A" other
              finally
                  Directory.Delete(notARepo, true)
          }

          // The REQUIRED lane's own message (#738). It runs offline, so it has no canonical digest to name —
          // and every one of #541's "do NOT"s has to survive that. The strictness was never what made the
          // gate wedge; the external input was.
          test "the required lane's MIRROR EDITED keeps every bit of #541's strictness" {
              let message =
                  describeEditedHere
                      "fs-gg-audio"
                      "fs-gg-game"
                      "FS.GG.Game/template/product-skills/fs-gg-audio/SKILL.md"
                      "template/product-skills/fs-gg-audio/SKILL.md"
                      (EditedHere baseSha)
                      localSha

              Expect.stringContains message "FROZEN MIRROR EDITED" "the #541 case is still named exactly as it was"

              Expect.stringContains
                  message
                  "DO NOT REVERT YOUR WORK"
                  "correct-but-unowned work must not be thrown away — three authors' edits were nearly lost this way (#541)"

              Expect.stringContains
                  message
                  "do NOT re-freeze"
                  "re-freezing over a genuine local edit SILENTLY DELETES it (FS.GG.Game#163 would have lost 25 lines)"

              Expect.stringContains
                  message
                  "DO NOT ADD A WAIVER FOR YOURSELF"
                  "a waiver records drift that predates the guard; it is not a way to land new drift"

              Expect.stringContains
                  message
                  (baseSha.Substring(0, 12))
                  "name the merge-base digest the body diverged FROM — that is the evidence git gave us, and it is all this lane needs"
          }

          // THE LAST PLACE #541's TEETH COULD HAVE BEEN LOST QUIETLY (#738).
          //
          // The required lane convicts on `git` and nothing else. So if `git` cannot answer, it has NO
          // other signal to fall back on — the registry and the canonical are gone from this lane on
          // purpose. Passing there would mean a mirror edit sails through the required gate whenever the
          // checkout is shallow: a silent fail-open, in the exact family (#266) this repo keeps closing.
          //
          // It fails CLOSED, and it is allowed to: its subject is THIS checkout's configuration, not
          // another repo's `main`, so ADR-0105's test still answers NO.
          test "the required lane fails CLOSED when git cannot tell it whether you edited the mirror" {
              let message = describeProbeFailed "fs-gg-audio" "template/product-skills/fs-gg-audio/SKILL.md" "shallow clone"

              Expect.stringContains message "::error" "a probe that did not run has proved nothing, and it must not prove it quietly"

              Expect.stringContains
                  message
                  "NOT a pass"
                  "say plainly that this is the check failing to RUN, not the check passing — the distinction #266 is entirely about"

              Expect.stringContains
                  message
                  "fetch-depth: 0"
                  "name the remedy: this is a fault in the CHECKOUT, and it is fixable here, unlike everything else this lane stopped reding on"
          }

          // ONE DEFINITION, TWO CONSUMERS (#661's rule). If the script stops calling the shared verdict and
          // grows its own copy, every test above goes on passing while the guard does whatever it likes —
          // which is precisely how the renderer bug in #657 hid behind a green gate.
          test "the script consumes the shared verdict rather than re-deriving one" {
              Expect.stringContains
                  script
                  "#load \"FrozenMirrorVerdict.fs\""
                  "scripts/check-frozen-mirrors.fsx must load the shared verdict module"

              Expect.stringContains
                  scriptCode
                  "decide baseline local registrySha canonical"
                  "the script must reach its verdict through `decide`. If this line moved, make sure the guard still consults BOTH signals (git and the registry) — the tests above assert the module, not the script, and a script that stopped calling it would keep them all green."

              // The lane policy is the module's to decide, not the script's (#738). A script that grew its
              // own `if verdict = CanonicalMoved then ...` would put the demotion somewhere no test looks.
              Expect.stringContains
                  scriptCode
                  "failsIn Freshness drift"
                  "the script must ask `failsIn` which verdicts red ITS lane, rather than re-deciding. The whole of #738 is that this policy lives in one place."

              Expect.isFalse
                  (scriptCode.Contains "FROZEN MIRROR EDITED")
                  "the error TEXT belongs in FrozenMirrorVerdict, next to the verdict that selects it. A copy in the script is one that can be printed for the wrong verdict — which is #720."
          }
        ]

[<Tests>]
let feature541FrozenMirrorGuardTests =
    testList
        "Feature541 frozen skill-mirror guard"
        [
          // THE HIGHEST-VALUE OFFLINE CHECK, and it needs no network. A waiver pins the digest of a body
          // that was ALREADY drifted when the guard landed. If somebody edits a waived mirror and updates
          // the pin, this stays green (that is the script's job to catch) — but if a pin is simply WRONG,
          // the waiver never matches and the guard reds `main` for a reason nobody can see. And a pin
          // copy-pasted from the CANONICAL digest instead of the drifted one would never fire at all,
          // silently un-waiving the drift it was written for.
          //
          // AN EMPTY WAIVER LIST IS THE END STATE, NOT A BUG — and this test used to say otherwise (#640).
          // It asserted `Expect.isNonEmpty waivers`, so it went RED on the day the last drifted mirror was
          // re-frozen: the script's stated goal ("a waiver whose mirror is back in sync is DELETED") was a
          // state its own test forbade. The two guards contradicted each other at exactly one point — zero
          // waivers — and the repo could not be healthy and green at the same time.
          //
          // The intent behind `isNonEmpty` was sound: with no waivers parsed, the loop below asserts NOTHING,
          // so a rotted regex would pass vacuously. That is a real hazard and it is still guarded — just not
          // by forbidding the healthy state. Absence is now CROSS-CHECKED (`driftedShaAssignments`, plus the
          // `declaresDriftedShaField` schema canary) instead of being read as failure, which is the same fix
          // the script itself applies to the registry parse.
          test "every waiver's pinned digest is the digest of the file it waives" {
              Expect.isTrue
                  declaresDriftedShaField
                  "scripts/check-frozen-mirrors.fsx no longer declares a `DriftedSha: string` field on `type Waiver`. BOTH readers in this file scrape that field name, so a rename blinds them together: they would both count zero, `0 = 0` would compare equal, and every waiver's pin would go unchecked under a green tick. If you renamed the field, rename it here too."

              Expect.equal
                  (List.length waivers)
                  driftedShaAssignments
                  "the waiver parser in this file disagrees with a crude count of `DriftedSha =` assignments in scripts/check-frozen-mirrors.fsx. The regex has rotted against the script's shape, so the per-waiver assertions below would silently assert NOTHING — a vacuous green over unchecked pins. Teach the regex the new shape. (0 = 0 is fine, and is the end state: every mirror in sync, nothing left to waive.)"

              for id, pinned in waivers do
                  let body = repositoryPath $"template/product-skills/{id}/SKILL.md"

                  Expect.isTrue (File.Exists body) $"a waiver names `{id}`, so this repo must still ship that mirror"

                  Expect.equal
                      (sha256Of body)
                      pinned
                      $"the waiver for `{id}` pins a digest that is NOT the current body's. A wrong pin is not a small error: the waiver stops matching and the gate reds for a reason nobody can see — or, if the digest was copy-pasted from the CANONICAL instead of the drifted body, it never fires and silently un-waives the drift it exists for."
          }

          // A DELETED mirror is as much a break as an edited one — a `--profile game` scaffold just loses
          // the skill. Checked here as well as in the script, because it is free offline.
          test "every mirror the script declares is still shipped" {
              Expect.isNonEmpty mirrored "the script declares which foreign skills this repo mirrors"

              for id in mirrored do
                  Expect.isTrue
                      (File.Exists(repositoryPath $"template/product-skills/{id}/SKILL.md"))
                      $"`{id}` is declared `Mirrored`, so this repo must ship a byte-identical copy of its canonical (ADR-0022 §6). Deleting it is a break: a `--profile game` scaffold loses the skill."
          }

          // The step is the gate. A guard the workflow does not call is a guard that reports green — which
          // is the whole shape of #541, and of the .github#266 fail-open family.
          //
          // #738 SPLIT THIS INTO TWO ASSERTIONS, AND BOTH HALVES MATTER. It used to assert only that the
          // required job ran the guard, which the lane split would satisfy vacuously — a required job
          // running `--freshness` would still "run the guard" while handing the merge button back to
          // FS.GG.Game. So the lane each job runs is now pinned, in BOTH directions.
          test "the REQUIRED Deterministic gate runs the guard — and runs the lane that is a function of THIS commit" {
              let gate = File.ReadAllText(repositoryPath ".github/workflows/gate.yml")

              // The Deterministic gate is the required job; everything from its `name:` to the next job's
              // 2-space `key:` is its body.
              let deterministic =
                  let start = gate.IndexOf "name: Deterministic gate"
                  Expect.isGreaterThan start 0 "gate.yml declares the Deterministic gate job"
                  let rest = gate.Substring start
                  let next = Regex.Match(rest, @"\n  [a-z][a-z0-9-]*:\n")
                  if next.Success then rest.Substring(0, next.Index) else rest

              Expect.stringContains
                  deterministic
                  "dotnet fsi scripts/check-frozen-mirrors.fsx --required"
                  "the guard must run in the REQUIRED Deterministic gate job (a check nobody invokes is a check that passes — #541), and it must run the `--required` LANE, whose verdict is a function of this commit alone."

              Expect.isFalse
                  (deterministic.Contains "check-frozen-mirrors.fsx --freshness")
                  "the freshness lane reads FS.GG.Game's `main`. In the required, enforce_admins gate that hands another repo this repo's merge button — #714, and the whole of #738. It may not run here."

              // THE ADR-0105 PROPERTY, ASSERTED WHERE IT CAN ACTUALLY REGRESS. The required lane needs no
              // token because it reads no network — and the single easiest way to undo all of #738 is for
              // somebody to "fix" a future step by pasting a GH_TOKEN back onto this job, at which point
              // the guard could quietly start reading another repo again. The absence of a token IS the
              // invariant, so it is the thing that gets a test.
              //
              // Asserted against the job's CODE, with `#` comment lines stripped — the same treatment
              // `scriptCode` gets above, and for the same reason. The first draft of this test matched the
              // prose two paragraphs up in gate.yml, which *explains* that the token used to be here: a
              // test that a file may not DISCUSS its own history is a test that punishes the comment doing
              // the most good. What must not come back is the `env:` key, not the word.
              let deterministicCode =
                  deterministic.Split '\n'
                  |> Array.filter (fun line -> not (line.TrimStart().StartsWith "#"))
                  |> String.concat "\n"

              Expect.isFalse
                  (deterministicCode.Contains "GH_TOKEN")
                  "the REQUIRED Deterministic gate must not be given a GitHub token. Its verdict has to be a function of THIS commit (ADR-0105), and a token is how an external input creeps back in — it is exactly what let the frozen-mirror guard read FS.GG.Game's `main` and wedge this repo on a pristine tree (#714)."
          }

          // ...and the demoted half must still RUN somewhere, or #738 traded a wedge for a blind spot.
          // A stale mirror ships a `--profile game` scaffold a skill FS.GG.Game has already moved on from.
          test "the NON-REQUIRED freshness job runs the lane the required gate gave up" {
              let gate = File.ReadAllText(repositoryPath ".github/workflows/gate.yml")

              let freshness =
                  let start = gate.IndexOf "  frozen-mirror-freshness:"

                  Expect.isGreaterThan
                      start
                      0
                      "gate.yml must declare a `frozen-mirror-freshness` job. Without it, demoting CANONICAL MOVED out of the required gate is not a lane split — it is a deletion, and a stale mirror merges in silence (#738)."

                  let rest = gate.Substring start
                  let next = Regex.Match(rest.Substring 1, @"\n  [a-z][a-z0-9-]*:\n")
                  if next.Success then rest.Substring(0, next.Index + 1) else rest

              Expect.stringContains
                  freshness
                  "dotnet fsi scripts/check-frozen-mirrors.fsx --freshness"
                  "the freshness job must actually run the freshness lane"

              // NOT-REQUIRED IS NOT `continue-on-error` (#216). The job may not wedge the repo; that is not
              // the same as the job being allowed to pass while it finds drift. A stale mirror is a RED.
              Expect.isFalse
                  (freshness.Contains "continue-on-error")
                  "`frozen-mirror-freshness` is NON-REQUIRED, which means branch protection does not block on it — it does NOT mean the job may go green on a stale mirror. `continue-on-error` would turn the one red that still reports this class of break into decoration (#216)."
          }
        ]
