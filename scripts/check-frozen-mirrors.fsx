#!/usr/bin/env dotnet fsi
//
// #541 — a Rendering PR can edit a frozen skill mirror it does not own and go 6/6 green.
//
// WHAT A FROZEN MIRROR IS. Four product skills have their canonical bodies in FS.GG.Game
// (ADR-0022 §6, the two-copies cost of the P4 skill-ownership migration). Rendering still ships
// byte-identical copies so a `--profile game` scaffold keeps working. The org registry
// (FS-GG/.github registry/skills.yml) records, per skill, its `owner` and the `sha256` of the
// OWNER'S canonical body.
//
// THE GAP THIS CLOSES. Nothing in this repo's CI knew those mirrors existed. No workflow referenced
// them; `skill-parity` only checks wrapper↔body parity WITHIN this repo. So an author could edit a
// body Rendering does not own, pass every required gate, merge — and the break surfaced later, in
// another repo's red gate, after the context that produced the edit was gone. It happened three
// times to `fs-gg-persistence` alone, each time under a full set of green checks, each time by an
// author doing correct work on the content who was never told they were editing a mirror.
//
// The cost of a warning here would be nil: all three breaks happened under six green checks, and a
// warning in that stream is a warning nobody reads. This FAILS.
//
// THE MIRROR SET IS DERIVED, AND ABSENCE IS DECLARED. Those are two different things and the first
// draft of this guard conflated them, with a fail-open at the bottom.
//
// Deriving "a mirror is a registry row this repo also ships a body for" reads well and is wrong,
// because it makes `File.Exists = false` mean TWO things: "this row correctly has no Rendering
// counterpart" (fs-gg-ballistics/ai/effects/physics — authored in Game, never migrated) and "somebody
// deleted the mirror". So `rm -rf template/product-skills/fs-gg-game-core` printed "3 mirrors" and
// exited 0. A `--profile game` scaffold silently loses a skill, which is every bit as much a break as
// editing one.
//
// Worse, it DECAYED. The three drifted mirrors survived deletion only by accident — via the stale-waiver
// arm — and this file tells you to delete a waiver once its mirror is re-synced. So the day the drift is
// fixed (the stated end state) all four mirrors become silently deletable. A guard that is strongest on
// the day it lands and weakens as the repo gets healthier is worse than none.
//
// So every non-Rendering-owned product row now needs an EXPLICIT disposition, and BOTH directions are
// asserted:
//   * `Mirrored`     — this repo ships it; the body must EXIST and match the canonical.
//   * `NoCounterpart`— this repo never had it; the body must NOT exist (vendoring one in would
//                      manufacture a two-copies obligation ADR-0022 §6 never imposed — .github#486
//                      warns about exactly that, and Rendering#505 asked for it).
//
// A row with no disposition is a HARD FAIL. That is what keeps a ninth mirror from arriving unguarded —
// the property a hand-list of "what to check" would lose, and the reason #541 asks for derivation. The
// count is still not hand-trusted: #541 says there are eight mirrors; there are four.
//
// AND IT NO LONGER MISNAMES THE DRIFT IT FINDS (#720). A hash mismatch has more than one cause, and this
// script used to report every one of them as "FROZEN MIRROR EDITED" — including the case where nobody
// edited anything and the OWNING repo moved the canonical, where it then forbade the re-freeze that is
// the actual fix. Deciding WHICH cause it is, and what to say about it, now lives in
// `FrozenMirrorVerdict.fs`: pure, git-only, no network, and therefore actually tested (see
// `tests/Package.Tests/Feature541FrozenMirrorGuardTests.fs`). This file keeps what genuinely needs a
// token — reading the org registry, and reading the owners' canonical bodies.

#load "FrozenMirrorVerdict.fs"

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions

open FsGg.Governance.FrozenMirrorVerdict

let repoRoot =
    let rec up (dir: DirectoryInfo) =
        if isNull dir then failwith "not inside a git checkout"
        elif Directory.Exists(Path.Combine(dir.FullName, ".git")) || File.Exists(Path.Combine(dir.FullName, ".git")) then dir.FullName
        else up dir.Parent

    up (DirectoryInfo(__SOURCE_DIRECTORY__))

let repoPath (relative: string) =
    Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar))

let private sha256Of (path: string) =
    use sha = SHA256.Create()
    sha.ComputeHash(File.ReadAllBytes path)
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

// ---- the org registry -------------------------------------------------------------------------
//
// Fetched, not vendored: a vendored copy is a fifth statement of the same fact, and it would go stale in
// exactly the direction that matters most here — the canonical moving underneath us.
//
// This used to end "…the direction this guard CANNOT SEE", and that clause is now false, which is why it
// is deleted rather than left standing: the guard sees that direction, names it `CANONICAL MOVED`, and
// tells you to re-freeze (#720). Leaving the sentence would have taught the next reader that the blind
// spot is still there — and this file has already been bitten once by commentary that outlived its
// condition (see the `fs-gg-audio` paragraph in the waiver block: "the COMMENTARY that outlives a waiver
// can lie just as loudly as the waiver did").
//
// A registry we cannot read is NOT a pass. That is the `apicompat-check.sh` rule, and it is the same
// rule for the same reason: a check that did not run has proved nothing, and reporting green for it
// is how a gate becomes decoration.
let private fetchRegistry () =
    let psi = ProcessStartInfo "gh"
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true

    [ "api"
      "repos/FS-GG/.github/contents/registry/skills.yml"
      "--jq"
      ".content" ]
    |> List.iter psi.ArgumentList.Add

    match Process.Start psi with
    | null -> failwith "could not start `gh` to read the org skill registry"
    | proc ->
        use proc = proc
        let out = proc.StandardOutput.ReadToEnd()
        let err = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        if proc.ExitCode <> 0 || String.IsNullOrWhiteSpace out then
            eprintfn "::error title=frozen-mirror check did not run::could not read FS-GG/.github registry/skills.yml (gh exit %d). NO mirror was compared, so this is NOT a pass — it is the check failing to run. %s" proc.ExitCode err
            exit 2

        Encoding.UTF8.GetString(Convert.FromBase64String(out.Trim()))

/// One `- { id: …, scope: …, owner: …, sha256: … }` row of the registry.
type RegistryRow =
    { Id: string
      Scope: string
      Owner: string
      Sha256: string
      Source: string }

let private parseRegistry (yaml: string) =
    Regex.Matches(yaml, @"^\s*- \{([^}]*)\}", RegexOptions.Multiline)
    |> Seq.cast<Match>
    |> Seq.choose (fun m ->
        let row = m.Groups.[1].Value

        let field name =
            let f = Regex.Match(row, $@"{name}:\s*([^,}}]+)")
            if f.Success then Some(f.Groups.[1].Value.Trim().Trim('"')) else None

        match field "id", field "scope", field "owner", field "sha256" with
        | Some id, Some scope, Some owner, Some sha ->
            Some
                { Id = id
                  Scope = scope
                  Owner = owner
                  Sha256 = sha
                  Source = field "source" |> Option.defaultValue "" }
        | _ -> None)
    |> List.ofSeq

/// What this repo does about a product skill it does not own. Absence is a DECISION here, never an
/// inference from a missing file.
type Disposition =
    /// Rendering ships a byte-identical copy (ADR-0022 §6, the two-copies cost of the P4 migration).
    | Mirrored
    /// Authored in the owning repo and never migrated — Rendering has no counterpart, and must not grow
    /// one. (.github#486; Rendering#505 asked for exactly that and was refused.)
    | NoCounterpart

let private dispositions =
    [ "fs-gg-game-core", Mirrored
      "fs-gg-audio", Mirrored
      "fs-gg-persistence", Mirrored
      "fs-gg-model-swap", Mirrored
      "fs-gg-ballistics", NoCounterpart
      "fs-gg-ai", NoCounterpart
      "fs-gg-effects", NoCounterpart
      "fs-gg-physics", NoCounterpart ]
    |> Map.ofList

// ---- the waivers ------------------------------------------------------------------------------
//
// THREE MIRRORS ARE ALREADY BROKEN ON MAIN, which is the whole reason #541 exists. This guard cannot
// simply fail on them: it would red main on arrival, and "fix" would mean re-freezing from the
// canonical — silently deleting real, correct work that three authors did on the content (#541 says
// so explicitly, and FS.GG.Game#163 is the case where it would destroy 25 lines).
//
// So each drifted mirror is waived AT ITS EXACT CURRENT DIGEST. That matters: a waiver by ID alone
// would be a blanket licence to keep editing the file, which is the very hole this closes. Pinning
// the digest means the NEXT edit to a waived mirror is still a RED — the waiver excuses the drift
// that already exists, and nothing more.
//
// And a waiver whose mirror is back in sync is DELETED, not kept: a stale waiver is a lie that hides
// the next one (the same rule R-REACH's exemption test applies).
type Waiver =
    { Id: string
      DriftedSha: string
      Because: string }

// THE LIST IS NOW EMPTY, and that is the end state the pattern above was aiming at: all four mirrors are
// byte-identical to their canonicals, so there is no drift left to excuse. The machinery stays — the
// stale-waiver arms below are what let this list drain, and a fifth mirror arriving drifted would need
// them again. What must NOT come back is a waiver for NEW drift: the error text below says so, and the
// three that were here all recorded edits made BEFORE this guard existed.
let private waivers: Waiver list =
    // fs-gg-persistence's waiver is GONE, and that is this file working exactly as designed (#629).
    //
    // It waived three correct-but-unowned edits (Rendering#520/#462, #539/#445, #590/#550) and said the
    // content had to be READ into FS.GG.Game's canonical rather than re-frozen over. It was:
    // FS.GG.Game#163 -> #199 adopted this very body upstream, #201 then repointed the dead #535 at #587,
    // and the canonical has since taken #619's correction too. So the mirror is byte-identical to the
    // canonical again (067c0842), the drift this waiver excused no longer exists, and the waiver is
    // deleted rather than kept — a stale waiver is a lie that hides the next one, and this file has said
    // so from the day it landed. The round trip closed. One waiver down, two to go.
    //
    // fs-gg-model-swap's waiver is gone too, and it was a LIE THIS GUARD WAS TELLING ITSELF (#629).
    //
    // #560/#576 re-mirrored it and the round trip closed — the mirror has matched its canonical for a
    // while. But the old comparison judged the mirror against the REGISTRY's `sha256`, which lagged, so
    // the guard went on believing the mirror was drifted and went on excusing it. The waiver outlived its
    // cause by exactly as long as nobody could see it: precisely the "stale waiver hides the next break"
    // failure the paragraph above warns about, running inside the file that warns about it.
    //
    // Judging the BODY made it visible in one run. That is the argument for the oracle change in a
    // sentence.
    //
    // fs-gg-audio's waiver is gone LAST, and it is the one this whole pattern was written for (#640).
    //
    // It did not excuse a careless edit — it excused a REFUSAL. #620 asked for a straight re-freeze from
    // the canonical, and READING THE DIFF (which the waiver told the next person to do) is what stopped
    // it: Rendering's mirror carried the FS.GG.Rendering#436/#429 content — audio materializes on the
    // `app` profile too, plus the four-package × profile table — and the canonical still taught
    // `game`/`sample-pack` ONLY. Copying it over would have destroyed correct work AND shipped an `app`
    // author a skill saying it did not apply to them, while the template materialized it for them anyway.
    // So the waiver named its own creditor instead: FS.GG.Game#204, to route that content UP.
    //
    // It landed (FS.GG.Game#215). The canonical now carries the widened `## Scope`, the profile table and
    // the per-family launch entry points, so the mirror is byte-identical again (66ad2654) and the debt is
    // PAID. That is what a waiver is for: it held the drift still, with a name on it, until the repo that
    // owned the body could take the content.
    //
    // AND THE LAST PIECE OF THAT DEBT IS PAID TOO (#649). This paragraph used to warn the next reader NOT
    // to "restore" the app-family ```fsharp block calling `ControlsElmish.runInteractiveAppWithAudio`: that
    // entry point first shipped in FS.GG.UI 0.9.0, Game pinned the UI train at 0.5.0, and Game COMPILES
    // every ```fsharp block in a published SKILL.md — so the repo that OWNED the body could not typecheck
    // it, and fencing it as non-F# to slip past that gate would have been a green tick over uncompiled code.
    // The canonical carried those entry points as a TABLE instead, and getting the block back meant moving
    // Game's UI train first.
    //
    // Game moved it. The train is on 0.9.0 (FS.GG.Game#217), `FS.GG.UI.Controls.Elmish` 0.9.0 is pinned
    // gate-only and in the skills-corpus `PackageRefs`, and the canonical typechecks the block it once could
    // not — 51 blocks compiled, green (FS.GG.Game#225). So the block is BACK in the canonical, on purpose,
    // and this mirror now carries it (4bee8ef2). Every premise of the old warning is false, which is why it
    // is deleted rather than kept: left standing it would have told whoever did this re-freeze to treat the
    // new block as something the mirror must not carry — i.e. to re-introduce the exact divergence the
    // re-freeze closes.
    //
    // That is the same failure mode as a stale waiver, one level up: the COMMENTARY that outlives a waiver
    // can lie just as loudly as the waiver did, and it is read by exactly the person least able to catch it.
    // Sunset the prose with the condition it described.
    []

/// The OWNER'S canonical body, fetched from the owning repo — the thing ADR-0022 §6 actually says this
/// mirror must be byte-identical to.
///
/// NOT the registry's `sha256`, which is what this guard used to compare against and which is WRONG in a
/// way that only shows up when the guard is working (#629). That digest is a CACHE, reconciled by a nightly
/// bot, so between a canonical edit and the bot's next run it names a body that no longer exists. In that
/// window the registry disagrees with the file it points at — right now it says `e9f471b0…` for
/// fs-gg-persistence while that file hashes `067c0842…` — and the mirror is judged against a ghost.
///
/// The consequence was backwards: re-mirroring CORRECTLY (mirror == canonical, which is the entire point of
/// the exercise) FAILED, while leaving the mirror stale PASSED via its waiver. A guard that reds the fix and
/// greens the bug is worse than no guard, and this one would have done it to every re-mirror from now on.
///
/// So the body is the oracle and the registry supplies only OWNERSHIP and the PATH. A registry that has not
/// caught up is then merely a registry that has not caught up — which is the bot's job, not this file's.
let private fetchCanonicalBody (source: string) =
    // `FS.GG.Game/template/product-skills/fs-gg-audio/SKILL.md` -> repo `FS-GG/FS.GG.Game`, path `template/…`.
    let parts = source.Split('/')

    if parts.Length < 2 then
        eprintfn "::error title=frozen-mirror check did not run::registry `source` is not <repo>/<path>: %s" source
        exit 2

    let repo = parts.[0]
    let path = String.Join("/", parts.[1..])

    let psi = ProcessStartInfo "gh"
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true

    [ "api"; $"repos/FS-GG/{repo}/contents/{path}"; "--jq"; ".content" ]
    |> List.iter psi.ArgumentList.Add

    match Process.Start psi with
    | null -> failwith "could not start `gh` to read a canonical skill body"
    | proc ->
        use proc = proc
        let out = proc.StandardOutput.ReadToEnd()
        let err = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        // Fail CLOSED, exactly as the registry read does: a canonical we could not read is a comparison
        // that did not happen, and reporting green for it is how a gate becomes decoration.
        if proc.ExitCode <> 0 || String.IsNullOrWhiteSpace out then
            eprintfn
                "::error title=frozen-mirror check did not run::could not read the canonical body FS-GG/%s/%s (gh exit %d). NO comparison was made for this mirror, so this is NOT a pass. %s"
                repo
                path
                proc.ExitCode
                err

            exit 2

        let bytes = Convert.FromBase64String(out.Trim())

        use sha = SHA256.Create()

        sha.ComputeHash bytes
        |> Array.map (fun b -> b.ToString "x2")
        |> String.concat ""


// ---- the check --------------------------------------------------------------------------------

let private registryYaml = fetchRegistry ()
let private registry = parseRegistry registryYaml

if List.isEmpty registry then
    eprintfn "::error title=frozen-mirror check did not run::the org registry parsed to ZERO rows, so no mirror was compared. Not a pass."
    exit 2

// A PARTIAL parse is not a pass either. `parseRegistry` reads flow-style rows (`- { … }`), which is all
// the registry emits today — but if `.github`'s writer ever wraps a line or switches one row to block
// style, that row is SILENTLY SKIPPED and its mirror goes unguarded. The "registry is empty" check above
// only fires when EVERYTHING is gone; dropping one row would be green. Same overloaded-absence bug the
// dispositions above exist to kill, so it gets the same treatment: count the rows, and insist they match.
let private rowsInYaml =
    registryYaml.Split '\n'
    |> Array.filter (fun line -> Regex.IsMatch(line, @"^\s*- "))
    |> Array.length

if rowsInYaml <> List.length registry then
    eprintfn
        "::error title=frozen-mirror check did not fully parse::the registry has %d list rows but only %d parsed. A row this guard cannot read is a mirror it cannot check, and silently skipping it is the fail-open this guard exists to close. Teach `parseRegistry` the new shape."
        rowsInYaml
        (List.length registry)

    exit 2

/// Every product skill this repo does NOT own — mirrored or not. The disposition decides which.
let private foreign =
    registry
    |> List.filter (fun row -> row.Scope = "product" && row.Owner <> "fs-gg-rendering")

if List.isEmpty foreign then
    eprintfn "::error title=frozen-mirror check is vacuous::the registry names NO product skill owned outside fs-gg-rendering, so this guard is asserting nothing. Either the ownership migration was reverted or the derivation is broken."
    exit 2

let mutable failed = false

// EVERY foreign row needs a declared disposition. An undeclared one is how a ninth mirror arrives
// unguarded — the exact thing #541's acceptance asks to prevent.
let private mirrors =
    foreign
    |> List.choose (fun row ->
        let relative = $"template/product-skills/{row.Id}/SKILL.md"
        let body = repoPath relative
        let exists = File.Exists body

        match Map.tryFind row.Id dispositions with
        | None ->
            failed <- true

            eprintfn
                "::error title=undeclared foreign skill::the org registry says `%s` is a product skill owned by %s, and scripts/check-frozen-mirrors.fsx does not say what this repo does about it. Declare it: `Mirrored` (this repo ships a byte-identical copy — ADR-0022 §6) or `NoCounterpart` (it was authored there and never migrated; do NOT vendor it in — .github#486). An undeclared row is an unguarded mirror."
                row.Id
                row.Owner

            None

        | Some NoCounterpart when exists ->
            failed <- true

            eprintfn
                "::error file=%s::`%s` is owned by %s and this repo declares it has NO counterpart — but a body now exists here. Vendoring it in manufactures a two-copies obligation ADR-0022 §6 never imposed (.github#486; Rendering#505 asked for exactly this and was refused). Delete it, or change the disposition to `Mirrored` deliberately and freeze it against the canonical."
                relative
                row.Id
                row.Owner

            None

        | Some NoCounterpart -> None

        | Some Mirrored when not exists ->
            // THE FAIL-OPEN THIS CLOSES. A deleted mirror used to be indistinguishable from a row that
            // never had one, so `rm -rf template/product-skills/fs-gg-game-core` exited 0 — and a
            // `--profile game` scaffold silently lost a skill.
            failed <- true

            eprintfn
                "::error title=frozen mirror DELETED::`%s` is declared `Mirrored` (this repo ships a byte-identical copy of %s's canonical, ADR-0022 §6) but `%s` does not exist. Deleting a mirror is as much a break as editing one: a `--profile game` scaffold loses the skill. Restore it, or change the disposition to `NoCounterpart` deliberately and say why."
                row.Id
                row.Owner
                relative

            None

        | Some Mirrored -> Some(row, body, sha256Of body))


for row, body, local in mirrors do
    let relative = $"template/product-skills/{row.Id}/SKILL.md"

    // The OWNER'S BODY, not the registry's record of it. See `fetchCanonicalBody`.
    let canonical = fetchCanonicalBody row.Source

    // The registry lagging its own canonical is normal (a nightly bot reconciles it) and is NOT this
    // guard's business — but it IS worth saying out loud, because a reader comparing digests by hand will
    // otherwise be baffled by exactly the discrepancy that used to break this check.
    if canonical <> row.Sha256 then
        printfn
            "  frozen-mirror NOTE %-22s registry sha256 (%s…) lags its canonical (%s…) — the autofix bot will reconcile it; this guard judges the BODY."
            row.Id
            (row.Sha256.Substring(0, 12))
            (canonical.Substring(0, 12))

    // WHY does it differ? `git` answers the only question that can convict this author — did THIS change
    // touch the file — and the registry's lagging sha answers the other one. Both are already to hand; the
    // guard used to consult neither and simply assert an edit. See `FrozenMirrorVerdict.fs`.
    let baseline = baselineOf repoRoot relative local

    // A PROBE THAT DID NOT RUN HAS PROVED NOTHING, and it must not prove it QUIETLY — the same rule this
    // file already applies to the registry read, one level down.
    //
    // If git cannot find a merge base (an unfetched `origin/main`, a shallow clone, a fork), every verdict
    // silently falls back to the registry signal ALONE, which is strictly weaker: it can still recognise a
    // canonical that moved, but a genuine local edit — #541's entire subject — decays from `MIRROR EDITED`
    // to "the cause cannot be determined". That is a real loss of teeth, and on a HEALTHY run it would be
    // completely invisible: all four mirrors print OK and nobody learns the probe is dead until the day it
    // has to convict someone and cannot. So it is said out loud, every run, whether or not there is drift.
    match baseline with
    | Unknown why ->
        printfn
            "  frozen-mirror NOTE %-22s git cannot say whether this change edited it (%s). Falling back to the registry signal alone — a real local edit will read as 'cause undecidable' rather than MIRROR EDITED. Fix the checkout (fetch-depth: 0), or set FSGG_FROZEN_MIRROR_BASE."
            row.Id
            why
    | UnchangedHere _
    | EditedHere _
    | AddedHere -> ()

    // The BODY's digest is the oracle, not `row.Sha256` — the registry's CACHE, which lags behind a
    // canonical edit until the nightly bot runs (#629). The old message printed the cache while CALLING it
    // "the canonical": the guard judged the body and then named a ghost, so on #649 it demanded
    // `9401ccd9…` while the canonical body hashed `4bee8ef2…`, and a correct re-freeze landed on a digest
    // the error said was wrong. Judge the body, REPORT the body — `describe` takes `canonical`.
    let verdict = decide baseline local row.Sha256 canonical

    match verdict with
    | InSync ->
        // In sync. Any waiver for it is now a lie.
        match waivers |> List.tryFind (fun w -> w.Id = row.Id) with
        | Some _ ->
            failed <- true

            eprintfn
                "::error file=%s::STALE WAIVER — `%s` now matches its canonical in %s, so the waiver in scripts/check-frozen-mirrors.fsx is excusing drift that no longer exists. Delete it. A waiver kept past its cause is what hides the next break (#541)."
                relative
                row.Id
                row.Owner
        | None -> printfn "  frozen-mirror OK   %-22s == %s canonical" row.Id row.Owner

    | drift ->
        match waivers |> List.tryFind (fun w -> w.Id = row.Id && w.DriftedSha = local) with
        | Some w -> printfn "  frozen-mirror WAIVED %-20s (known drift) — %s" row.Id (w.Because.Substring(0, min 60 w.Because.Length) + "…")
        | None ->
            if fails drift then
                failed <- true

            // A waiver pinned to a DIFFERENT body means this mirror was edited again — which is only a
            // coherent accusation for `MirrorEdited`, so `describe` is the one that decides what to do
            // with it.
            let waivedAt =
                waivers
                |> List.tryFind (fun w -> w.Id = row.Id)
                |> Option.map (fun w -> w.DriftedSha)

            eprintfn "%s" (describe drift row.Id row.Owner row.Source relative baseline local canonical waivedAt)

// A waiver for a skill that is not a mirror at all is dead weight that will mask a real one later.
for w in waivers do
    if not (mirrors |> List.exists (fun (row, _, _) -> row.Id = w.Id)) then
        failed <- true

        eprintfn
            "::error title=stale waiver::scripts/check-frozen-mirrors.fsx waives `%s`, but it is not a frozen mirror (the registry does not name it as owned elsewhere, or this repo no longer ships its body). Delete the waiver."
            w.Id

printfn ""
printfn "frozen-mirror: %d mirror(s) derived from the org registry, %d waived" (List.length mirrors) (List.length waivers)

if failed then exit 1 else exit 0
