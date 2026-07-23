// #720 — WHY a frozen mirror differs from its canonical, and what to do about it.
//
// `check-frozen-mirrors.fsx` compares this repo's mirrored skill body against the LIVE canonical in the
// owning repo. Two completely different events produce the identical hash mismatch:
//
//   1. SOMEBODY EDITED THE MIRROR HERE. A body this repo does not own was changed locally. This is what
//      the guard was built for (#541), and its remedy is: take the change to the owning repo, and do NOT
//      re-freeze — re-freezing would silently delete real work (FS.GG.Game#163 is the case where it would
//      have destroyed 25 lines).
//
//   2. THE CANONICAL MOVED. Nobody here touched anything; the owning repo landed a change and the
//      downstream copies are now stale. Its remedy is the exact OPPOSITE: re-freeze from the canonical,
//      which deletes nothing, because the new content IS the canonical.
//
// The guard reported case 2 AS case 1, in capitals — "FROZEN MIRROR EDITED" — and then forbade the one
// remedy that fixes it ("do NOT re-freeze this file from the canonical"). That is not a cosmetic defect.
// Case 2 fails on a PRISTINE `main` tree, inside the REQUIRED `Deterministic gate` (`enforce_admins`, so
// not even an admin can merge past it), so when it fires it wedges the WHOLE REPO — and it greets every
// worker with an error saying they edited a file they never opened, while warning them off the action
// that would unwedge it. That is a multiplier on the worst possible moment (ADR-0103: "THE COST OF A RED
// HERE IS THE WHOLE REPO"). It happened for real: FS.GG.Game#279 moved a canonical, and #714 was the
// re-sync that unwedged this repo.
//
// The guard's own header ALREADY admitted this was its blind spot — "stale in exactly the direction this
// guard cannot see (the canonical moving underneath us)" — while its output went on asserting the
// opposite. A file that documents its blind spot and then speaks past it is worse than one that says
// nothing: the comment is read by maintainers, and the error is read by whoever is wedged.
//
// IT NEED NOT GUESS. Two independent signals are already available, and the guard was printing one of
// them as a NOTE:
//
//   * `git` KNOWS. If the body at the merge base hashes the same as the body here, THIS CHANGE did not
//     touch it — so the drift is not this author's. Only a difference against the merge base implies an
//     edit made here. This is the strong signal, and it is exact.
//   * THE REGISTRY SHA. The org registry's `sha256` is a CACHE of the canonical, reconciled by a nightly
//     bot, so it LAGS a canonical edit. In the live wedge the bodies hashed exactly the registry's sha
//     while the registry disagreed with the canonical — `body == registry && registry != canonical` is a
//     near-certain "the canonical moved and the bot has not caught up".
//
// WHAT THE TWO SIGNALS CANNOT SETTLE, AND WHY THIS FILE SAYS SO OUT LOUD. Once the bot reconciles the
// registry, `registry == canonical` and the second signal goes silent — a body that is simply BEHIND the
// canonical then looks, by digest alone, exactly like a body somebody edited here long ago and landed.
// Git can still say "not in THIS change", but not "not in ANY change". So there is a third, genuinely
// undecidable state, and folding it into either of the other two would just reintroduce this bug facing
// the other way. It gets its own verdict (`DriftUnattributed`) whose remedy is the only honest one:
// READ THE DIFF before you re-freeze. That is not a hedge — it is the discipline the waiver machinery
// already institutionalises, and it is exactly what stopped #620 from destroying the #436/#429 content.
//
// This module is PURE (`decide`) plus one GIT-ONLY probe (`baselineOf`). It reaches no network and needs
// no token, which is why `tests/Package.Tests/Feature541FrozenMirrorGuardTests.fs` can compile it in and
// test the decision against real git trees, offline, with no mocks — the thing the guard's own tests said
// they could not do ("a mock of it here would assert that the mock works"). The `gh` calls that fetch the
// registry and the canonical bodies stay in the script, where they belong.
//
// One definition, two consumers, no drift — the `SurfaceRenderer.fs` rule (#661), for the same reason.

// #833 — ...AND THE LANE SPLIT LEFT A RE-FREEZE UNMERGEABLE, because the required lane reds on the very
// edit a re-freeze must make.
//
// #738 (above) put exactly one question in the required lane — *did THIS change edit a body this repo does
// not own?* — and answered it from `git` against the merge base. That is right about #541's case and WRONG
// about the one edit this repo is obliged to make: a RE-FREEZE, where the canonical moved and somebody
// copies the new bytes down. Both change the body vs. the merge base, so both read `EditedHere`, so both
// red — and the required lane is `enforce_admins`, so the re-freeze that unwedges the repo could not be
// merged by anyone. #789 hit it first (PR #832 needed a one-time protection lift to land); every prior
// re-freeze (#714, #773, #780, #781) was pre-#738, back when the required lane checked FRESHNESS, which a
// re-freeze SATISFIES. #696 counts ~40 of these, so it recurs forever.
//
// The offline lane cannot tell the two apart by looking at the bytes: "edited toward the canonical" and
// "edited away from it" are the same diff against the merge base, and the canonical is the input ADR-0105
// forbids it from reading. So it is TOLD. A `Mirrored` skill now DECLARES the canonical digest it is frozen
// to (`Disposition`, below), and the required lane's whole question becomes one the tree can answer:
//
//     is the body in this tree the body this tree SAYS it froze?   (`sanctionOf`)
//
// A re-freeze updates the body and the declaration in ONE commit and passes. #541's author — editing content
// in a body they did not know was a mirror — updates only the body, and still reds. The teeth are where they
// always were; what changed is that the correct edit now has a legal way to say so.
//
// WHAT THIS DELIBERATELY GIVES UP, stated plainly because a sanction nobody understands is a rubber stamp.
// An author who edits the body AND writes a matching declaration passes the required lane. That is not an
// oversight — it is the trade, and it is bounded on three sides:
//
//   * It cannot happen by ACCIDENT, which is the entirety of #541's case. All three of the breaks that
//     motivated this guard were authors who never knew the file was a mirror; not one of them would have
//     written a declaration. The gate still catches every one of them.
//   * It reds the FRESHNESS lane, and cannot be waived there: the declaration is `sha256(body)`, so
//     `declaration <> canonical` IS `body <> canonical`, which is the drift `decide` already convicts on.
//     Forging the declaration buys a green required lane and a red freshness lane — trading a silent break
//     for a loud one, which is the direction this repo keeps pushing everything.
//   * It is a deliberate, reviewable act, in the diff, next to the body it sanctions.
//
// AND `git` LEAVES THE REQUIRED LANE'S VERDICT ENTIRELY. This falls out of the above, and it is worth saying
// once, loudly, because it deletes a fail-closed arm (#738's `describeProbeFailed`) that was RIGHT when it
// was written: that lane convicted on `git` and nothing else, so a `git` that could not answer had to red,
// or a shallow clone was a silent fail-open of #541 (the #266 family). The declaration replaces `git` as the
// oracle, and `sanctionOf` reads only the working tree — so a blind `git` no longer costs the lane its
// verdict: an undeclared edit reds whether or not `git` can see the merge base. Reding on `Unknown` NOW would
// be a false red carrying a message ("we cannot tell whether you edited it") about a question this lane no
// longer asks. `baselineOf` stays: it is the freshness lane's oracle, and here it supplies the EVIDENCE an
// error message needs. It simply no longer decides what the tree already answers.
module FsGg.Governance.FrozenMirrorVerdict

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography

/// What `git` says about whether THIS working tree changed the mirror body, relative to the commit the
/// change forked from. `Unknown` is a real state and is never silently read as either of the others.
type Baseline =
    /// The body at the merge base hashes the same as the body here: this change did not touch it.
    | UnchangedHere of baseSha: string
    /// The body at the merge base differs from the body here: this change EDITED it.
    | EditedHere of baseSha: string
    /// There was no body at the merge base: this change ADDED it.
    | AddedHere
    /// `git` could not answer — no merge base, a shallow clone, no `git`. Say so; never guess.
    | Unknown of why: string

/// WHY a mirror's body differs from its owner's canonical. Each verdict exists because its REMEDY is
/// different; two verdicts that would print the same advice would not be worth telling apart.
type Verdict =
    /// Byte-identical to the canonical. Nothing to do.
    | InSync
    /// This change edited (or added) a body this repo does not own. #541's case: route it UP, never
    /// re-freeze over it, never waive it.
    | MirrorEdited
    /// Nobody here touched the body — the owning repo moved the canonical underneath it. Re-freezing is
    /// the FIX, and it deletes nothing.
    | CanonicalMoved
    /// Drift this change did not introduce, whose cause the digests cannot settle (see the header). Could
    /// be a canonical that moved before the registry caught up with it, or an edit that landed here long
    /// ago. READ THE DIFF before re-freezing.
    | DriftUnattributed

/// WHICH LANE is asking — and therefore WHOSE COMMIT is allowed to decide the verdict (ADR-0105, #738).
///
/// This is the whole of #738 in one type. The guard used to run in ONE place, the required, admin-enforced
/// `Deterministic gate`, and to answer a question it CANNOT answer from this repo's tree: "is our mirror
/// byte-identical to the body that is in FS.GG.Game's `main` RIGHT NOW?" The answer moves when FS.GG.Game
/// merges, so the same commit here was green today and red tomorrow — and because the gate is
/// `enforce_admins`, that red wedged EVERY merge in the repo, fixable only by a human copying bytes
/// (#714, and nine such incidents in three days per #696).
///
/// ADR-0105's test: *could this gate turn an already-green commit red without anyone changing this
/// repository?* Each lane below answers it, and the answer is what decides where it may run.
type Lane =
    /// The REQUIRED `Deterministic gate`. Its verdict must be a function of THIS COMMIT ALONE, so it reads
    /// the tree and `git` and NOTHING else — no registry, no canonical, no network, no token. It can
    /// therefore answer exactly one question, which is also the only one #541 ever asked: *did THIS change
    /// edit a body this repo does not own?*
    | Required
    /// The NON-REQUIRED `frozen-mirror-freshness` lane. Its SUBJECT *is* another repo's `main`, so by the
    /// rule `gate.yml` already applies to the template-payload restore gate it may report a stale mirror
    /// and must never be able to wedge the repo. Its remedy is a re-freeze PR — which is ADR-0105's
    /// option (1) working as intended: the external thing moving produces a reviewable PR here, not a red
    /// `main`.
    | Freshness

/// What this repo does about a product skill it does not own. Absence is a DECISION, never an inference
/// from a missing file.
type Disposition =
    /// This repo ships a byte-identical copy (ADR-0022 §6, the two-copies cost of the P4 migration), and
    /// DECLARES the canonical digest that copy is frozen to (#833).
    ///
    /// THE DECLARATION IS WHAT MAKES A RE-FREEZE LANDABLE. The required lane is offline, so it cannot ask
    /// FS.GG.Game what the canonical is — but it can ask whether the body here is the body this tree SAYS it
    /// froze. A re-freeze updates both in one commit and passes; an edit that updates only the body does not.
    ///
    /// IT CARRIES NO INFORMATION THE BODY DOES NOT ALREADY HAVE, and that is the point rather than a flaw. A
    /// correct mirror is byte-identical to its canonical, so this digest always equals `sha256(body)` — it is
    /// pure redundancy, and the redundancy IS the mechanism: it is an expectation the next edit must
    /// consciously break, exactly as a waiver's `DriftedSha` pin is ("pinning the digest means the NEXT edit
    /// to a waived mirror is still a RED"). What it adds is not a fact. It is INTENT, in the diff, reviewable.
    | Mirrored of frozenAt: string
    /// Authored in the owning repo and never migrated — this repo has no counterpart and must not grow one
    /// (.github#486; Rendering#505 asked for exactly this and was refused). No body, so nothing to declare.
    | NoCounterpart

/// Does this repo ship a body for the skill at all?
///
/// `= Mirrored` used to answer this by equality. It cannot now that `Mirrored` carries the declaration
/// (#833) — and F# would not have complained: `Mirrored "…" = Mirrored "…"` compares the DIGESTS, so every
/// such site would have quietly started asking a different question. Hence one named predicate.
let isMirrored (disposition: Disposition) =
    match disposition with
    | Mirrored _ -> true
    | NoCounterpart -> false

/// One foreign (non-Rendering-owned) product skill, PINNED IN THIS TREE.
///
/// This table is ADR-0105 option (1) — "pin the external input into the tree" — and it is the reason the
/// required lane needs no network. It used to live half here (`dispositions`) and half in the org registry
/// (`owner`, `source`), which meant the required gate had to FETCH `FS-GG/.github`'s `main` just to learn
/// which files it was guarding. Three consequences, all of them ADR-0105 violations, and only the first is
/// the one #738 was filed about:
///
///   * a canonical that MOVED reddened it (the wedge everyone noticed);
///   * a NEW registry row reddened it — `.github` adding any `owner: fs-gg-game` product row makes this
///     repo's required gate fail `undeclared foreign skill`, with no change here and nothing a PR here
///     could do about it;
///   * a registry it could not READ reddened it (exit 2, by design — and `check-frozen-mirrors.fsx`'s own
///     reasoning for that, *"a check that did not run has proved nothing"*, is SOUND, which is precisely
///     ADR-0105:87-90's point: a gate that must fail closed on an input it does not own is a gate that
///     hands the merge button to another repo).
///
/// Pinned here, all three become facts about THIS commit. The pin cannot rot silently: the `Freshness`
/// lane asserts this table against the live registry (owner, source, and the registry's own `mirrored:`
/// flag), and reds — non-required — when they disagree.
type Foreign =
    { Id: string
      Owner: string
      /// `<repo>/<path>` — where the OWNER keeps the canonical body. The registry's `source` field.
      Source: string
      Disposition: Disposition }

/// The pinned table. Every product row in the org registry whose `owner` is not `fs-gg-rendering`.
///
/// A row the registry has and this table does not is a HARD FAIL in the `Freshness` lane — that is what
/// stops a ninth mirror arriving unguarded, and it is the property #541's acceptance asks for. It is no
/// longer a hard fail in the REQUIRED lane, because a row appearing is another repo's commit.
let foreignSkills: Foreign list =
    let game id disposition =
        { Id = id
          Owner = "fs-gg-game"
          Source = $"FS.GG.Game/template/product-skills/{id}/SKILL.md"
          Disposition = disposition }

    // ADR-0063 (2026-07-21 amendment) — the four game product skills that USED to be `Mirrored` here
    // (`fs-gg-game-core`, `fs-gg-audio`, `fs-gg-persistence`, `fs-gg-model-swap`) are now `NoCounterpart`.
    // The frozen `--profile game` copies were retired (FS.GG.Rendering#965) once FS.GG.Game's producer
    // manifest flipped them to `mirrored: false` and `FS.GG.Game.Skills 0.2.0` began delivering them
    // owner-sourced (FS.GG.Game#454, SDD#631). "No rendering mirror exists" is now true for the WHOLE game
    // product class — exactly as it already was for ai/ballistics/effects/physics/playtest. This repo ships
    // NO body for any of them, so there is nothing to declare; the freshness lane asserts this table AGREES
    // with the registry's `mirrored: false`, and the required lane reds if a body reappears (.github#486).
    [ game "fs-gg-game-core" NoCounterpart
      game "fs-gg-audio" NoCounterpart
      game "fs-gg-persistence" NoCounterpart
      game "fs-gg-model-swap" NoCounterpart
      game "fs-gg-ballistics" NoCounterpart
      game "fs-gg-ai" NoCounterpart
      game "fs-gg-effects" NoCounterpart
      game "fs-gg-physics" NoCounterpart
      game "fs-gg-mapcraft" NoCounterpart
      // fs-gg-playtest: owner fs-gg-game, registry `mirrored: false` (#1194/.github#299). Authored
      // there, deliberately not vendored here — so NOT a game-scaffold skill in this repo and NOT a
      // Mirrored copy. Declaring `Mirrored` would hand-forge an obligation the owner never asserted
      // (the #505/#658 trap). If the scaffold is ever meant to offer it, the prerequisite is the org
      // registry flipping to `mirrored: true` first; only then does a Mirrored bundle become valid.
      game "fs-gg-playtest" NoCounterpart ]

/// Where this repo keeps its copy of a foreign skill's body.
let mirrorPath (id: string) = $"template/product-skills/{id}/SKILL.md"

/// Is this body the body the tree DECLARES it froze? (#833)
type Sanction =
    /// The body hashes exactly its declared canonical. Nothing to answer for: either nobody touched it, or
    /// this change re-froze it and SAID SO in the same commit.
    | Declared
    /// The body does not hash its declared canonical. #541's case — and the only verdict the required lane
    /// can produce.
    | Undeclared

/// THE REQUIRED LANE'S ENTIRE VERDICT (#833). A function of the WORKING TREE alone: no registry, no
/// canonical, no network, no token, and — since the declaration replaced `git` as this lane's oracle — no
/// `git` either. ADR-0105's test (*could this turn an already-green commit red without anyone changing this
/// repository?*) answers NO, and now answers it without needing an argument about `fetch-depth`.
///
/// It is trivial, and it is a named function anyway, for #661's reason: the lane policy is this module's to
/// decide. A script that inlined `if local = frozenAt` would put the sanction somewhere no test looks — which
/// is precisely how #720's text and #738's lane policy each came to live where they could drift.
let sanctionOf (frozenAt: string) (local: string) : Sanction =
    if local = frozenAt then Declared else Undeclared

/// The verdict. PURE — every input is already resolved, so this is a truth table and is tested as one.
///
/// `local` is the body in the working tree, `registry` the org registry's `sha256` (a lagging CACHE of the
/// canonical, NOT the oracle — #629), and `canonical` the owner's live body (the oracle ADR-0022 §6 names).
///
/// TWO OF ITS FOUR OUTCOMES ARE NOT THE REQUIRED LANE'S BUSINESS, and `failsIn` is where that is enforced:
/// `canonical` is an input this repo does not own, so any verdict that turns on it belongs to `Freshness`.
/// The required lane never calls this function at all — it has no `canonical` to pass.
let decide (baseline: Baseline) (local: string) (registry: string) (canonical: string) : Verdict =
    if local = canonical then
        InSync
    else
        // `git` is the STRONGER signal and is checked first: it is exact about the only question that can
        // convict this author — did THIS change touch the file? A canonical that moved does not change the
        // merge base, so an edit here stays an edit here even when the canonical moved too, and the author
        // still has to route it up.
        match baseline with
        | EditedHere _
        | AddedHere -> MirrorEdited

        | UnchangedHere _
        | Unknown _ ->
            // Not this change (or we cannot tell). The registry is the only remaining witness: a body that
            // hashes exactly what the registry records as the canonical, while the registry disagrees with
            // the LIVE canonical, is a canonical that moved and a bot that has not caught up.
            //
            // Note this is safe even under `Unknown`: for it to be wrong, somebody would have to have
            // hand-edited the mirror to be byte-identical to the lagged canonical — in which case
            // re-freezing onto the current canonical is still exactly the right thing to do.
            if local = registry && registry <> canonical then
                CanonicalMoved
            else
                DriftUnattributed

/// Does this verdict RED **this lane**? (#738)
///
/// It used to be `fails : Verdict -> bool`, and every drift verdict reded the one gate there was. The
/// predicate is unchanged for `Freshness`; what changed is that there is now somewhere else to ask from.
///
/// The split is ADR-0105's test, applied verdict by verdict — *could this turn an already-green commit red
/// without anyone changing this repository?*
///
///   * `MirrorEdited` — NO. In the freshness lane `decide` returns it only when `git` says THIS change
///     edited or added the body (`EditedHere`/`AddedHere`); in the required lane it is what `sanctionOf`
///     reports as `Undeclared` (#833). Both are facts about this commit and nothing else. It keeps every
///     tooth it had in #541, in the required lane, where it belongs.
///   * `CanonicalMoved` — YES, trivially: FS.GG.Game merges, and a commit that was green is red.
///   * `DriftUnattributed` — **YES, AND THIS IS THE ONE THAT LOOKS SAFE AND IS NOT.** #738's suggested
///     shape moved only `CanonicalMoved`, which would have left the wedge in place with a new name on it:
///     the two verdicts are the SAME STALE MIRROR at two moments in time. `decide` returns `CanonicalMoved`
///     only while `local = registry`, i.e. while the nightly bot has not yet reconciled the registry to the
///     moved canonical. The moment it does, `local <> registry` and the identical, untouched, still-stale
///     mirror falls through to `DriftUnattributed`. Demoting one and not the other buys a wedge that waits
///     for a cron job.
///
/// So the required lane fails on exactly what this change DID, and the freshness lane fails on everything
/// that is true of the world. `InSync` reds nothing, anywhere.
///
/// AND THE ANNOTATION LEVEL MOVES WITH THE EXIT CODE — the trap the old comment here warned whoever did
/// this. It is now closed BY CONSTRUCTION rather than by a matching rule that could drift: `describe` is
/// only ever reached from the freshness lane, which reds on every verdict it can produce, and the required
/// lane prints its own message (`describeUndeclared`) for the only verdict it can produce. Neither lane can
/// emit an `::error` for something it does not fail on, because neither lane can emit the other's verdicts.
let failsIn (lane: Lane) (verdict: Verdict) : bool =
    match verdict with
    | InSync -> false

    // This change's own diff. Deterministic; reds both lanes.
    | MirrorEdited -> true

    // The world's. Reportable, never a merge freeze.
    | CanonicalMoved
    | DriftUnattributed ->
        match lane with
        | Required -> false
        | Freshness -> true

// ---- the git probe ----------------------------------------------------------------------------
//
// GIT ONLY. No network, no token — which is what lets the tests drive this against real, throwaway git
// trees instead of a mock. The `gh` calls live in the script.

/// `git` in `repoRoot`, capturing raw stdout BYTES.
///
/// Bytes, not text, and that is load-bearing: the digest is over the file's exact contents, so decoding
/// the blob to a string and re-encoding it would mangle CRLF and any non-UTF8 byte and produce a digest
/// that matches nothing. A guard whose oracle is a hash must never round-trip through a decoder.
let private git (repoRoot: string) (args: string list) : int * byte[] * string =
    // A MISSING `git` THROWS — it does not return null — and an unhandled exception here would take the
    // whole guard down with a stack trace. That fails closed, so it is not a security hole, but it is the
    // wrong FAILURE: this guard's readers are people whose repo is already wedged, and `Unknown` exists
    // precisely so a probe that cannot answer degrades to "I do not know" (and the registry signal) rather
    // than to a crash. Every way `git` can fail to run has to arrive as a value.
    try
        let psi = ProcessStartInfo "git"
        psi.WorkingDirectory <- repoRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        // Git speaks the caller's language. Nothing below parses its prose any more (see `baselineOf`),
        // but the strings we DO surface in an `Unknown` reason are read by whoever is unwedging the repo.
        psi.Environment.["LC_ALL"] <- "C"
        args |> List.iter psi.ArgumentList.Add

        match Process.Start psi with
        | null -> 127, [||], "could not start `git`"
        | proc ->
            use proc = proc
            use buffer = new MemoryStream()
            proc.StandardOutput.BaseStream.CopyTo buffer
            let err = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            proc.ExitCode, buffer.ToArray(), err
    with ex ->
        127, [||], $"could not run `git`: {ex.Message}"

let private sha256OfBytes (bytes: byte[]) =
    use sha = SHA256.Create()

    sha.ComputeHash bytes
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

/// The refs we will try to find a merge base against, best first.
///
/// `FSGG_FROZEN_MIRROR_BASE` is the escape hatch for a checkout whose remote is not `origin` (a fork) —
/// without it the probe would answer `Unknown` there and the guard would fall back to the registry signal
/// alone, which is weaker but still honest. That degradation is the point: no candidate ref is ever
/// ASSUMED to be the base.
let private baseRefCandidates () =
    match Environment.GetEnvironmentVariable "FSGG_FROZEN_MIRROR_BASE" with
    | null
    | "" ->
        // `GITHUB_BASE_REF` is the branch a PULL REQUEST targets, and it is the most accurate base there
        // is on the event this guard mostly runs on. It is empty on a `push`, where `origin/main` is
        // right (and on a push to `main` the merge base is HEAD itself — the pristine-main case #720 is
        // about, which must read `UnchangedHere`).
        let prBase =
            match Environment.GetEnvironmentVariable "GITHUB_BASE_REF" with
            | null
            | "" -> []
            | branch -> [ $"origin/{branch}"; branch ]

        prBase @ [ "origin/main"; "main" ]
    | explicitRef -> [ explicitRef ]

/// Did THIS change touch the mirror body?
///
/// Answered against the MERGE BASE, not `HEAD~1` and not the working tree's own history: the question is
/// "does this change differ from the commit it forked from", and on a PR with many commits, or on a
/// pristine `main` (where the merge base IS `HEAD`), nothing else gives the right answer.
///
/// The pristine-`main` case is the one #720 is about, and it falls out for free: on `main`, `merge-base
/// origin/main HEAD` is `HEAD`, the committed body equals the working-tree body, so the answer is
/// `UnchangedHere` — nobody edited it — which is exactly the truth the old guard could not see.
let baselineOf (repoRoot: string) (relative: string) (localSha: string) : Baseline =
    let rec tryRefs refs =
        match refs with
        | [] -> Error "no merge base against origin/main or main (shallow clone? fork? set FSGG_FROZEN_MIRROR_BASE)"
        | baseRef :: rest ->
            match git repoRoot [ "merge-base"; baseRef; "HEAD" ] with
            | 0, out, _ when out.Length > 0 -> Ok(Text.Encoding.UTF8.GetString(out).Trim())
            | _ -> tryRefs rest

    match tryRefs (baseRefCandidates ()) with
    | Error why -> Unknown why
    | Ok baseCommit ->
        // IS THE PATH IN THE BASE COMMIT AT ALL? Asked STRUCTURALLY — `rev-parse --verify --quiet` exits 0
        // (printing the blob id) when it is and 1, silently, when it is not.
        //
        // The obvious alternative is to run `cat-file` and read the failure out of git's stderr
        // ("path '…' does not exist in '…'"). That is a guard whose verdict depends on git's PROSE: it
        // has two different spellings depending on whether the file is also on disk, it is localised, and
        // it is free to change between versions. When it fails to match, a mirror this change VENDORED IN
        // (`.github#486`: vendoring one in is itself the break) stops reading as `AddedHere` and decays to
        // `Unknown` — a conviction lost to a translated string. Ask git a question with an exit code.
        match git repoRoot [ "rev-parse"; "--verify"; "--quiet"; $"{baseCommit}:{relative}" ] with
        | 1, _, _ ->
            // git ANSWERED — "there is nothing there" — so this is knowledge, not ignorance.
            AddedHere

        | 0, _, _ ->
            // `cat-file blob` rather than `show`: `show` is a porcelain command and will happily apply
            // textconv/smudge filters, which is precisely how a digest oracle acquires a silent bug.
            match git repoRoot [ "cat-file"; "blob"; $"{baseCommit}:{relative}" ] with
            | 0, blob, _ ->
                let baseSha = sha256OfBytes blob

                if baseSha = localSha then
                    UnchangedHere baseSha
                else
                    EditedHere baseSha

            | code, _, err -> Unknown $"git cat-file {baseCommit}:{relative} exited {code}: {err.Trim()}"

        | code, _, err -> Unknown $"git rev-parse {baseCommit}:{relative} exited {code}: {err.Trim()}"

let private shortSha (sha: string) =
    if sha.Length >= 12 then sha.Substring(0, 12) + "…" else sha

/// The command that re-freezes a mirror from its owner's canonical. Printed literally, because the whole
/// point of `CanonicalMoved` is that this is the FIX and the guard used to forbid it — and a remedy the
/// reader has to assemble themselves is a remedy they will get wrong while the repo is wedged.
let refreezeCommand (source: string) (relative: string) =
    // registry `source` is `<repo>/<path>`, e.g. `FS.GG.Game/template/product-skills/fs-gg-audio/SKILL.md`.
    let parts = source.Split('/')
    let repo = parts.[0]
    let path = String.Join("/", parts.[1..])

    $"gh api repos/FS-GG/{repo}/contents/{path} --jq .content | base64 -d > {relative}"

/// The REQUIRED lane's `MIRROR EDITED` line (#738, #833). Offline, so it has NO canonical digest to name —
/// and that absence is the point, not a shortcoming: this lane convicts on the DECLARATION, and it never
/// needed the canonical to do it. #541's case was always provable from the commit alone; the guard simply
/// never separated it from the questions that were not.
///
/// Every "do NOT" from #541 survives here verbatim. The strictness was never what made the gate wedge.
///
/// AND IT MUST NOT READ AS "UPDATE THE DECLARATION TO GO GREEN" (#833). This message is the ONLY place the
/// sanction is explained to the person who tripped it, and its reader is, overwhelmingly likely, #541's
/// author: someone doing correct content work who has just been told off for editing a file they did not
/// know was a mirror. Handing them a one-line trick to silence the gate would convert this guard into the
/// rubber stamp the whole design is arranged to avoid. So the re-freeze escape is stated LAST, gated on a
/// precondition the reader can check ("the canonical moved and you are copying it down"), and paired with
/// what happens if they take it anyway — the freshness lane compares the declaration against the LIVE
/// canonical, so a forged one reds there.
let describeUndeclared
    (id: string)
    (owner: string)
    (source: string)
    (relative: string)
    (baseline: Baseline)
    (local: string)
    (frozenAt: string)
    : string =
    // `git` no longer decides anything here — it only says HOW the body and its declaration came apart, and
    // every one of its answers (including "I cannot tell") leaves the verdict untouched.
    let evidence =
        match baseline with
        | AddedHere ->
            "This change ADDED this body; it did not exist at the merge base. Vendoring in a skill this repo has no counterpart for is itself the break (.github#486)."
        | EditedHere baseSha ->
            $"This change EDITED it: at the merge base it hashed {shortSha baseSha}, and it now hashes {shortSha local}."
        | UnchangedHere _ ->
            "`git` says this change did not touch the BODY, so it is the DECLARATION that moved — or the two were already apart on `main`. Either way they must agree: a declaration edited on its own sanctions nothing, and would sanction the NEXT edit to this body if it were allowed to stand."
        | Unknown why ->
            $"`git` could not tell whether this change touched it ({why}) — which does not change this verdict. This lane compares the body against its declaration, and they do not match either way."

    $"::error file={relative}::FROZEN MIRROR EDITED — `{id}` is owned by {owner} ({source}), and this repo only ships a byte-identical copy (ADR-0022 §6). The body hashes {shortSha local}, but this tree DECLARES it frozen at {shortSha frozenAt} (scripts/FrozenMirrorVerdict.fs). {evidence}\n\nDO NOT REVERT YOUR WORK, do NOT re-freeze this file from the canonical (that would silently delete it), and DO NOT ADD A WAIVER FOR YOURSELF in scripts/check-frozen-mirrors.fsx — a waiver records drift that already existed before this guard, and is not a way to land new drift. Take the change to the OWNING repo's canonical body ({source}), and re-freeze here once it lands. This repo gives you no other signal that you are editing a body it does not own, which is why three correct edits to this file merged green before this check existed (#541).\n\nONLY IF YOU ARE RE-FREEZING — that is, {owner} moved the canonical and you are copying the new bytes DOWN, authoring nothing — update the declaration for `{id}` in scripts/FrozenMirrorVerdict.fs to {shortSha local} in this same commit, and this lane will pass it (#833). That is the one edit this repo is obliged to make and could not merge before. It is NOT a way to land your own content: the declaration is compared against {owner}'s LIVE canonical in the non-required `frozen-mirror-freshness` job, so a declaration that names bytes {owner} does not have reds there, loudly, and the drift is still yours to route upstream."

/// The GitHub-Actions error line for a drift verdict. The TEXT is the bug #720 is about, so it is defined
/// here, next to the verdict that selects it, and asserted in the tests — not assembled at the call site
/// where the two can drift apart.
///
/// FRESHNESS LANE ONLY (#738). Every verdict it can be handed reds that lane, so every line it emits is an
/// `::error` and that is honest. The required lane cannot reach it — it has no `canonical` to pass — which
/// is what keeps the annotation level and the exit code from drifting apart.
let describe
    (verdict: Verdict)
    (id: string)
    (owner: string)
    (source: string)
    (relative: string)
    (baseline: Baseline)
    (local: string)
    (canonical: string)
    (waivedAt: string option)
    : string =
    let refreeze = refreezeCommand source relative

    match verdict with
    | InSync -> ""

    | MirrorEdited ->
        // #541's original text, and it stays EXACTLY as strict — for the case it was actually written for.
        // Every "do NOT" below is correct here and only here.
        let againAndAgain =
            match waivedAt with
            | Some drifted ->
                $"\n\nThis mirror HAS a waiver, but for a different body ({shortSha drifted}) — you have edited it AGAIN. A waiver excuses the drift that already existed, never the next edit."
            | None -> ""

        let evidence =
            match baseline with
            | AddedHere -> "This change ADDED this body; it did not exist at the merge base."
            | EditedHere baseSha -> $"This change EDITED it: at the merge base it hashed {shortSha baseSha}, and it now hashes {shortSha local}."
            | UnchangedHere _
            | Unknown _ -> $"Your copy now hashes {shortSha local}."

        $"::error file={relative}::FROZEN MIRROR EDITED — `{id}` is owned by {owner} ({source}), and this repo only ships a byte-identical copy (ADR-0022 §6). {evidence} The canonical is {shortSha canonical}.\n\nDO NOT REVERT YOUR WORK, do NOT re-freeze this file from the canonical (that would silently delete it), and DO NOT ADD A WAIVER FOR YOURSELF in scripts/check-frozen-mirrors.fsx — a waiver records drift that already existed before this guard, and is not a way to land new drift. Take the change to the OWNING repo's canonical body ({source}), and re-freeze here once it lands. This repo gives you no other signal that you are editing a body it does not own, which is why three correct edits to this file merged green before this check existed (#541).{againAndAgain}"

    | CanonicalMoved ->
        // The case the guard used to misname, and the ONLY one where re-freezing is right. Note what this
        // message does NOT say: it does not accuse the reader of editing anything, and it does not forbid
        // the re-freeze. It is also explicit that this is not a defect in the PR under review, because the
        // reader is almost certainly someone whose unrelated PR just went red (#720).
        $"::error file={relative}::CANONICAL MOVED — nobody here edited `{relative}`. It is byte-identical to what the org registry records for `{id}`, and {owner} has since moved the canonical underneath it ({source}): the canonical now hashes {shortSha canonical}, this mirror hashes {shortSha local}.\n\nThis is NOT a defect in your change — this mirror was already stale on `main` before you branched, and it will red every PR in this repo until it is re-synced. RE-FREEZE IT FROM THE CANONICAL. That is the fix, and it deletes nothing: the new content IS the canonical.\n\n    {refreeze}\n\nCommit that, and link {owner}'s PR that moved it. If the diff turns out to carry content this repo authored and the canonical lacks, STOP — that is not this case; route the content up to {owner} instead (see the waiver history in scripts/check-frozen-mirrors.fsx)."

    | DriftUnattributed ->
        let notYou =
            match baseline with
            | UnchangedHere _ -> "`git` says this change did not touch it: the body is identical to its copy at the merge base, so the drift was already on `main` before you branched."
            | Unknown why -> $"`git` could not tell whether this change touched it ({why}), so this may or may not be yours — check `git diff` against the merge base before you assume either way."
            | EditedHere _
            | AddedHere -> "" // unreachable: those baselines decide `MirrorEdited`.

        $"::error file={relative}::FROZEN MIRROR STALE — `{relative}` does not match `{id}`'s canonical in {owner} ({source}): the mirror hashes {shortSha local}, the canonical hashes {shortSha canonical}. {notYou}\n\nTHE CAUSE CANNOT BE READ OFF THE DIGESTS, so this guard will not guess, and you must not either. It is one of:\n\n  (a) {owner} moved the canonical and this mirror is simply BEHIND — re-freeze it:\n\n        {refreeze}\n\n  (b) somebody edited this mirror here, long enough ago that it is on `main` — re-freezing would SILENTLY DELETE their work. (This is not hypothetical: #620 asked for a straight re-freeze of fs-gg-audio, and reading the diff is what stopped it from destroying the #436/#429 content.)\n\nREAD THE DIFF between the two bodies and decide which. If the mirror carries content the canonical does not, it is (b): route that content UP to {owner} and re-freeze here once it lands. If it carries nothing the canonical lacks, it is (a): re-freeze."
