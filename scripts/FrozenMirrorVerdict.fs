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

/// The verdict. PURE — every input is already resolved, so this is a truth table and is tested as one.
///
/// `local` is the body in the working tree, `registry` the org registry's `sha256` (a lagging CACHE of the
/// canonical, NOT the oracle — #629), and `canonical` the owner's live body (the oracle ADR-0022 §6 names).
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

/// Does this verdict RED the gate?
///
/// All three drift verdicts do, today. That is deliberate and it is NOT the same claim as "all three
/// belong in the required gate" — `CanonicalMoved`'s subject is ANOTHER repo's `main`, which can move at
/// any moment for reasons no PR here controls, so it makes the required, admin-enforced `Deterministic
/// gate` non-deterministic in this repo's tree: the same commit is green today and red tomorrow because
/// FS.GG.Game merged something. By the repo's own rule for feed-dependent lanes (cadence-map §4b/§5) that
/// is a lane which should not be required.
///
/// Moving it is a GATE-POLICY change, not a message fix: demoting it to a warning inside the required job
/// with nowhere else to land would be a fail-open, and this guard exists precisely because "a warning in
/// that stream is a warning nobody reads". It needs a non-required lane to land in first (gate.yml,
/// cadence-map, CadenceCoverageTests) — filed separately.
///
/// WHOEVER DOES THAT: this predicate is where the decision belongs, but flipping it is not the whole job,
/// and a comment claiming otherwise would be the next thing to mislead somebody. `describe` emits a
/// literal `::error` for every drift verdict, and an `::error` annotation on a verdict that no longer reds
/// the gate is the same class of lie this file was written to remove — the ANNOTATION LEVEL has to move
/// with the exit code.
let fails (verdict: Verdict) : bool =
    match verdict with
    | InSync -> false
    | MirrorEdited
    | CanonicalMoved
    | DriftUnattributed -> true

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

/// The GitHub-Actions error line for a drift verdict. The TEXT is the bug #720 is about, so it is defined
/// here, next to the verdict that selects it, and asserted in the tests — not assembled at the call site
/// where the two can drift apart.
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
