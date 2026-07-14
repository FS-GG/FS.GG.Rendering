#!/usr/bin/env dotnet fsi
//
// #541 — a Rendering PR can edit a frozen skill mirror it does not own and go 6/6 green.
// #738 — ...and the guard that closed that could be reddened, on a pristine `main`, by another repo.
//
// WHAT A FROZEN MIRROR IS. Four product skills have their canonical bodies in FS.GG.Game (ADR-0022 §6,
// the two-copies cost of the P4 skill-ownership migration). Rendering still ships byte-identical copies
// so a `--profile game` scaffold keeps working. The org registry (FS-GG/.github registry/skills.yml)
// records, per skill, its `owner` and the `sha256` of the OWNER'S canonical body.
//
// THE GAP #541 CLOSED. Nothing in this repo's CI knew those mirrors existed. So an author could edit a
// body Rendering does not own, pass every required gate, merge — and the break surfaced later, in another
// repo's red gate, after the context that produced the edit was gone. It happened three times to
// `fs-gg-persistence` alone, each time under a full set of green checks, each time by an author doing
// correct work on the content who was never told they were editing a mirror. That is why this FAILS
// rather than warns: all three breaks happened under six green checks, and a warning in that stream is a
// warning nobody reads.
//
// ---------------------------------------------------------------------------------------------------
// TWO LANES, BECAUSE THERE ARE TWO QUESTIONS AND ONLY ONE OF THEM IS ABOUT THIS COMMIT (#738)
// ---------------------------------------------------------------------------------------------------
//
// This guard used to ask both at once, in the REQUIRED, `enforce_admins` `Deterministic gate`:
//
//   1. "Did THIS change edit a body this repo does not own?"           <- a fact about this commit's diff
//   2. "Is our mirror what FS.GG.Game's `main` says it is RIGHT NOW?"  <- a fact about ANOTHER REPO
//
// Question 2 has an input that is not in this tree, and its answer moves when somebody else merges. So the
// same Rendering commit was green today and red tomorrow — and because the gate is admin-enforced, that red
// wedged EVERY merge in the repo, fixable only by a human hand-copying bytes. Nine such incidents in three
// days (#696); #714 is the one that had to be unwedged by hand. The job is called "Deterministic gate," and
// with question 2 in it, it was not one.
//
// ADR-0105 settles it: *a context may be required only if its verdict on a given commit is stable —
// re-running it later, with no change in this repository, must return the same verdict.* The operational
// test is one sentence: **could this gate turn an already-green commit red without anyone changing this
// repository?** If yes, it may not be required.
//
//   --required    Question 1 ONLY. Reads the tree and `git`. No registry, no canonical, no network, no
//                 token. Its verdict is a function of the commit alone, so it is eligible to be required —
//                 and it keeps every one of #541's teeth, because #541's case was ALWAYS provable from the
//                 commit alone. The guard simply never separated it from the questions that were not.
//
//   --freshness   Question 2. Reads the org registry and the owners' canonical bodies. Its SUBJECT *is*
//                 another repo's `main`, so it is NOT required and must never be able to wedge this repo —
//                 the same line `gate.yml` already draws for `template-payload-restore-gate`, whose subject
//                 is nuget.org. It reports a stale mirror; the remedy is a re-freeze PR.
//
//   (no flag)     Both. What you want locally, and what a re-freeze author should run before pushing.
//
// WHY THE DEMOTION IS NOT A FAIL-OPEN, which is the thing to be suspicious of here. The verdict that
// catches the #541 case (`MirrorEdited`) is decided by `git` against the merge base, and it is the ONLY
// verdict the required lane can produce — it still reds, still in the required gate, still admin-enforced.
// What left the required lane is the pair of verdicts nobody in this repo can act on and no PR here can
// prevent. And `--freshness` is not `continue-on-error`: it goes RED, loudly, on a stale mirror. It simply
// cannot take the merge button away from the repo while it does.
//
// AND IT IS NOT ENOUGH TO DEMOTE `CanonicalMoved` ALONE, which is what #738's own suggested shape proposed.
// `CanonicalMoved` and `DriftUnattributed` are THE SAME STALE MIRROR at two moments in time: `decide`
// returns the first only while the registry's nightly bot has not yet reconciled, and the identical,
// untouched, still-stale mirror falls through to the second the moment it does. Demoting one and not the
// other buys a wedge that waits for a cron job. See `FrozenMirrorVerdict.failsIn`.
//
// THE MIRROR SET IS PINNED IN THIS TREE, AND THAT IS ALSO #738 (ADR-0105 option 1). It used to be DERIVED
// from the registry — which meant the required gate fetched `FS-GG/.github`'s `main` just to learn which
// files it was guarding, and therefore that `.github` adding ANY `owner: fs-gg-game` product row reddened
// this repo's required gate with `undeclared foreign skill`, with no change here and nothing a PR here could
// do about it. Same for a registry it could not READ (exit 2, by design). Both are now the freshness lane's
// business. `FrozenMirrorVerdict.foreignSkills` is the pin; the freshness lane asserts it against the live
// registry so it cannot rot silently.
//
// ABSENCE IS DECLARED, NEVER INFERRED. Every foreign row carries an explicit `Mirrored` / `NoCounterpart`
// disposition, so `File.Exists = false` cannot mean two things at once ("correctly has no counterpart" vs
// "somebody deleted the mirror"). Deriving the set instead — "a registry row this repo also ships a body
// for" — reads well and fails open: `rm -rf template/product-skills/fs-gg-game-core` printed "3 mirrors"
// and exited 0, and a `--profile game` scaffold silently lost a skill.
//
// Deciding WHY a mirror differs, and what to say about it, lives in `FrozenMirrorVerdict.fs`: pure, git-only,
// no network, and therefore actually tested (`tests/Package.Tests/Feature541FrozenMirrorGuardTests.fs`).
// This file keeps what genuinely needs a token — reading the org registry, and the owners' canonical bodies —
// and now confines it to the lane that is allowed to need one.

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

// ---- which lane(s) are we ----------------------------------------------------------------------

let private argv = Environment.GetCommandLineArgs()
let private asked (flag: string) = argv |> Array.contains flag

/// No flag runs BOTH — the local-development default, and what a re-freeze author should run before
/// pushing. CI names its lane explicitly, because in CI the distinction is the entire point.
let private lanes =
    match asked "--required", asked "--freshness" with
    | false, false -> [ Required; Freshness ]
    | required, freshness ->
        [ if required then Required
          if freshness then Freshness ]

let mutable failed = false

// ================================================================================================
// THE REQUIRED LANE — the commit, and nothing else.
// ================================================================================================

let private runRequired () =
    printfn "frozen-mirror [required] — did THIS change edit a body this repo does not own? (#541)"
    printfn "  Reads the working tree and `git`. NOT the registry, NOT the canonical, NO network (#738)."
    printfn ""

    for skill in foreignSkills do
        let relative = mirrorPath skill.Id
        let body = repoPath relative

        match skill.Disposition, File.Exists body with
        // THE FAIL-OPEN THIS ARM CLOSES. A deleted mirror used to be indistinguishable from a row that
        // never had one, so `rm -rf template/product-skills/fs-gg-game-core` exited 0 — and a
        // `--profile game` scaffold silently lost a skill. Deleting a mirror is as much a break as editing
        // one. A fact about the tree, so it stays in the required lane.
        | Mirrored, false ->
            failed <- true

            eprintfn
                "::error title=frozen mirror DELETED::`%s` is declared `Mirrored` (this repo ships a byte-identical copy of %s's canonical, ADR-0022 §6) but `%s` does not exist. Deleting a mirror is as much a break as editing one: a `--profile game` scaffold loses the skill. Restore it, or change the disposition in scripts/FrozenMirrorVerdict.fs to `NoCounterpart` deliberately and say why."
                skill.Id
                skill.Owner
                relative

        // Vendoring in a body this repo has no counterpart for manufactures a two-copies obligation
        // ADR-0022 §6 never imposed (.github#486; Rendering#505 asked for exactly this and was refused).
        // Also a fact about the tree.
        | NoCounterpart, true ->
            failed <- true

            eprintfn
                "::error file=%s::`%s` is owned by %s and this repo declares it has NO counterpart — but a body now exists here. Vendoring it in manufactures a two-copies obligation ADR-0022 §6 never imposed (.github#486; Rendering#505 asked for exactly this and was refused). Delete it, or change the disposition in scripts/FrozenMirrorVerdict.fs to `Mirrored` deliberately and freeze it against the canonical."
                relative
                skill.Id
                skill.Owner

        | NoCounterpart, false -> ()

        | Mirrored, true ->
            let local = sha256Of body

            // #541's ENTIRE case, and `git` proves it from the merge base without consulting anything
            // outside this checkout. THIS is what may be required.
            match baselineOf repoRoot relative local with
            | (EditedHere _ | AddedHere) as baseline ->
                failed <- true
                eprintfn "%s" (describeEditedHere skill.Id skill.Owner skill.Source relative baseline local)

            // A probe that did not run has proved nothing, and it must not prove it QUIETLY. With the
            // canonical comparison gone from this lane, a blind `git` would leave the required gate with
            // NOTHING to say about a mirror edit — a silent fail-open of exactly the #266 family. It fails
            // closed, and it is allowed to: its subject is THIS checkout's config (`fetch-depth: 0`, which
            // gate.yml sets), not another repo's `main`, so ADR-0105's test still answers NO.
            | Unknown why ->
                failed <- true
                eprintfn "%s" (describeProbeFailed skill.Id relative why)

            | UnchangedHere _ -> printfn "  frozen-mirror OK     %-22s this change did not touch it" skill.Id

    printfn ""

    printfn
        "frozen-mirror [required]: %d foreign skill(s) pinned in-tree, %d of them mirrored."
        (List.length foreignSkills)
        (foreignSkills |> List.filter (fun s -> s.Disposition = Mirrored) |> List.length)

    // SAY WHAT THIS LANE DID NOT CHECK. A reader who takes a green `Deterministic gate` to mean "the
    // mirrors are in sync with FS.GG.Game" would be wrong, and that misreading is exactly how a gate
    // becomes decoration. This lane cannot answer that question — deliberately — and says so every run.
    printfn "  NOT checked here: whether these mirrors still MATCH FS.GG.Game's canonicals. That is another"
    printfn "  repo's `main`, so it may not decide whether THIS commit merges (ADR-0105). The non-required"
    printfn "  `frozen-mirror-freshness` job answers it, and a re-freeze PR is its remedy."

// ================================================================================================
// THE FRESHNESS LANE — the world. Reports it; may never wedge on it.
// ================================================================================================

/// One `- { id: …, scope: …, owner: …, sha256: …, mirrored: … }` row of the registry.
type RegistryRow =
    { Id: string
      Scope: string
      Owner: string
      Sha256: string
      Source: string
      Mirrored: bool option }

/// The org registry. Fetched, not vendored: a vendored copy is a fifth statement of the same fact, and it
/// would go stale in exactly the direction that matters most here — the canonical moving underneath us.
///
/// A registry we cannot read is NOT a pass (the `apicompat-check.sh` rule: a check that did not run has
/// proved nothing). It is `exit 2`. That reasoning is sound, and it is precisely WHY this read may not sit
/// in a required gate — ADR-0105:87-90 makes the argument: *a gate that must fail closed on an input it
/// does not own is a gate that hands the merge button to another repo.* So it still fails closed, but here,
/// where failing closed costs a red advisory job rather than the repo's merge button.
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
            eprintfn "::error title=frozen-mirror freshness did not run::could not read FS-GG/.github registry/skills.yml (gh exit %d). NO mirror was compared, so this is NOT a pass — it is the check failing to run. %s" proc.ExitCode err
            exit 2

        Encoding.UTF8.GetString(Convert.FromBase64String(out.Trim()))

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
                  Source = field "source" |> Option.defaultValue ""
                  Mirrored = field "mirrored" |> Option.map (fun v -> v = "true") }
        | _ -> None)
    |> List.ofSeq

/// The OWNER'S canonical body, fetched from the owning repo — the thing ADR-0022 §6 actually says this
/// mirror must be byte-identical to.
///
/// NOT the registry's `sha256`, which is what this guard used to compare against and which is WRONG in a way
/// that only shows up when the guard is working (#629). That digest is a CACHE, reconciled by a nightly bot,
/// so between a canonical edit and the bot's next run it names a body that no longer exists, and the mirror
/// is judged against a ghost. The consequence was backwards: re-mirroring CORRECTLY failed, while leaving the
/// mirror stale PASSED via its waiver.
///
/// So the body is the oracle, and the registry supplies only OWNERSHIP and the PATH.
let private fetchCanonicalBody (source: string) =
    let parts = source.Split('/')

    if parts.Length < 2 then
        eprintfn "::error title=frozen-mirror freshness did not run::registry `source` is not <repo>/<path>: %s" source
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
                "::error title=frozen-mirror freshness did not run::could not read the canonical body FS-GG/%s/%s (gh exit %d). NO comparison was made for this mirror, so this is NOT a pass. %s"
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

// ---- the waivers ------------------------------------------------------------------------------
//
// A waiver excuses drift that ALREADY EXISTED when this guard landed, AT ITS EXACT CURRENT DIGEST — never
// an edit made after it. Pinning the digest means the NEXT edit to a waived mirror is still a RED: the
// waiver excuses the drift that already exists, and nothing more. A waiver whose mirror is back in sync is
// DELETED, not kept — a stale waiver is a lie that hides the next one.
//
// Waivers live in the FRESHNESS lane, because what they excuse is drift against the canonical, which is a
// freshness question. The required lane has no use for them: it convicts on `git`, and a waiver has never
// been a licence to edit a mirror.
type Waiver =
    { Id: string
      DriftedSha: string
      Because: string }

// THE LIST IS EMPTY, and that is the end state the pattern above was aiming at: all four mirrors are
// byte-identical to their canonicals, so there is no drift left to excuse. The machinery stays — the
// stale-waiver arms below are what let this list drain, and a fifth mirror arriving drifted would need them
// again. What must NOT come back is a waiver for NEW drift.
//
// The three that were here (fs-gg-persistence, fs-gg-model-swap, fs-gg-audio) all recorded edits made BEFORE
// this guard existed, and each was retired by routing its content UP to FS.GG.Game rather than re-freezing
// over it — the discipline the `DriftUnattributed` verdict now institutionalises. That history is in git and
// is not re-narrated here: commentary that outlives its condition lies exactly as loudly as a stale waiver
// does, and this file has been bitten by that before (#649).
let private waivers: Waiver list = []

let private runFreshness () =
    printfn "frozen-mirror [freshness] — do our mirrors still match FS.GG.Game's canonicals? (#738)"
    printfn "  Reads the org registry and the owners' live bodies. NOT REQUIRED: this lane's subject is"
    printfn "  another repo's `main`, so it REPORTS staleness and may never wedge this repo (ADR-0105)."
    printfn ""

    let registryYaml = fetchRegistry ()
    let registry = parseRegistry registryYaml

    if List.isEmpty registry then
        eprintfn "::error title=frozen-mirror freshness did not run::the org registry parsed to ZERO rows, so no mirror was compared. Not a pass."
        exit 2

    // A PARTIAL parse is not a pass either. `parseRegistry` reads flow-style rows (`- { … }`), which is all
    // the registry emits today — but if `.github`'s writer ever wraps a line or switches one row to block
    // style, that row is SILENTLY SKIPPED and its mirror goes unguarded. The "registry is empty" check above
    // only fires when EVERYTHING is gone; dropping one row would be green.
    let rowsInYaml =
        registryYaml.Split '\n'
        |> Array.filter (fun line -> Regex.IsMatch(line, @"^\s*- "))
        |> Array.length

    if rowsInYaml <> List.length registry then
        eprintfn
            "::error title=frozen-mirror freshness did not fully parse::the registry has %d list rows but only %d parsed. A row this guard cannot read is a mirror it cannot check, and silently skipping it is the fail-open this guard exists to close. Teach `parseRegistry` the new shape."
            rowsInYaml
            (List.length registry)

        exit 2

    let foreignRows =
        registry
        |> List.filter (fun row -> row.Scope = "product" && row.Owner <> "fs-gg-rendering")

    if List.isEmpty foreignRows then
        eprintfn "::error title=frozen-mirror freshness is vacuous::the registry names NO product skill owned outside fs-gg-rendering, so this guard is asserting nothing. Either the ownership migration was reverted or the derivation is broken."
        exit 2

    // ---- THE PIN vs THE WORLD ------------------------------------------------------------------
    //
    // `foreignSkills` is pinned in this tree so the REQUIRED lane needs no network (ADR-0105 option 1). A pin
    // nothing checks is a pin that rots, and it would rot in the one direction that matters most: a new
    // foreign skill arriving unguarded, which is exactly what #541's acceptance asks to prevent. So the pin
    // is asserted against the live registry HERE — in the lane that is allowed to depend on it, and that
    // cannot take the merge button away when it disagrees.

    for row in foreignRows do
        match foreignSkills |> List.tryFind (fun s -> s.Id = row.Id) with
        | None ->
            failed <- true

            eprintfn
                "::error title=undeclared foreign skill::the org registry says `%s` is a product skill owned by %s, and scripts/FrozenMirrorVerdict.fs does not declare what this repo does about it. Add it to `foreignSkills`: `Mirrored` (this repo ships a byte-identical copy — ADR-0022 §6) or `NoCounterpart` (it was authored there and never migrated; do NOT vendor it in — .github#486). An undeclared row is an unguarded mirror."
                row.Id
                row.Owner

        | Some pinned ->
            if pinned.Owner <> row.Owner then
                failed <- true

                eprintfn
                    "::error title=frozen-mirror pin is STALE::`%s` is pinned in scripts/FrozenMirrorVerdict.fs as owned by %s, but the org registry now says %s. That pin is what the REQUIRED lane guards against, so a wrong pin guards the wrong thing. Update it."
                    row.Id
                    pinned.Owner
                    row.Owner

            if pinned.Source <> row.Source then
                failed <- true

                eprintfn
                    "::error title=frozen-mirror pin is STALE::`%s` is pinned with source `%s`, but the org registry now says `%s`. Update the pin in scripts/FrozenMirrorVerdict.fs. Nothing downstream of here trusts it — the freshness comparison and the re-freeze command it prints are both built from the REGISTRY's path, deliberately, so a stale pin cannot make this guard tell you to overwrite one skill's mirror with another skill's body. What it DOES break is the required lane, which is offline and has only the pin to name the owning file with."
                    row.Id
                    pinned.Source
                    row.Source

            // The registry states `mirrored:` itself. Disagreeing with it means one of the two is wrong about
            // whether this repo is supposed to ship a body at all — and BOTH readings are breaks: a mirror
            // nobody guards, or a body vendored in that ADR-0022 §6 never asked for.
            let pinSaysMirrored = pinned.Disposition = Mirrored

            match row.Mirrored with
            | Some registrySaysMirrored when registrySaysMirrored <> pinSaysMirrored ->
                failed <- true

                eprintfn
                    "::error title=frozen-mirror disposition DISAGREES with the registry::`%s` is pinned `%s` in scripts/FrozenMirrorVerdict.fs, but the org registry says `mirrored: %b`. One of them is wrong, and both readings are breaks: a mirror nobody guards, or a body vendored in that ADR-0022 §6 never asked for (.github#486)."
                    row.Id
                    (if pinSaysMirrored then "Mirrored" else "NoCounterpart")
                    registrySaysMirrored
            | _ -> ()

    // ...and the other direction: a pin naming a skill the registry no longer carries is dead weight that
    // will mask a real one later, exactly as a stale waiver does.
    for skill in foreignSkills do
        if not (foreignRows |> List.exists (fun row -> row.Id = skill.Id)) then
            failed <- true

            eprintfn
                "::error title=frozen-mirror pin is DEAD::scripts/FrozenMirrorVerdict.fs pins `%s` as a foreign product skill, but the org registry no longer names it as one. Either the ownership moved (in which case this repo may now OWN the body, and it should leave `foreignSkills`) or the pin is stale. Resolve it — a dead pin masks the next real one."
                skill.Id

    // ---- THE MIRRORS vs THEIR CANONICALS -------------------------------------------------------

    let mirrors =
        foreignSkills
        |> List.filter (fun skill -> skill.Disposition = Mirrored)
        |> List.choose (fun skill ->
            let relative = mirrorPath skill.Id
            let body = repoPath relative

            // A MISSING body is the REQUIRED lane's finding — it is a fact about the tree, and it reds
            // there. Reporting it once, in the lane that owns it, is the point of having lanes.
            if File.Exists body then
                Some(skill, relative, sha256Of body)
            else
                printfn "  frozen-mirror SKIP   %-22s body missing — the required lane reds on this" skill.Id
                None)

    for skill, relative, local in mirrors do
        // WHERE THE CANONICAL LIVES IS THE REGISTRY'S FACT, NOT THE PIN'S — and getting that backwards
        // makes this guard print a command that DESTROYS the mirror.
        //
        // The in-tree pin carries a `Source` so the required lane can name the owning file offline. It is
        // a CACHE of the registry's `source`, and the arm above reds when the two disagree. But reding is
        // not enough: if the comparison below then FETCHED from the stale pin, the `CANONICAL MOVED`
        // message would name the wrong body and print a runnable re-freeze command built from it —
        //
        //     gh api …/fs-gg-ai/SKILL.md … > template/product-skills/fs-gg-audio/SKILL.md
        //
        // — i.e. "fix your mirror by overwriting it with a different skill's body". The whole point of that
        // message (#720) is that it is a remedy the reader can run while the repo is red, so a wrong one is
        // worse than none. The registry is the authority on where the owner keeps the body; the pin is only
        // this repo's copy of that fact, and a copy that disagrees is the copy that is wrong.
        match foreignRows |> List.tryFind (fun row -> row.Id = skill.Id) with
        | None ->
            // Already reded above as a DEAD pin. There is no registry row, so there is no `source` — this
            // guard does not know where the canonical lives and will not guess at it.
            printfn "  frozen-mirror SKIP   %-22s no registry row — see the DEAD pin error above" skill.Id

        | Some row ->

        // The registry's `sha256` — the LAGGING-CACHE signal `decide` uses to recognise a canonical that
        // moved before the nightly bot caught up. A witness, never the oracle (#629).
        let registrySha = row.Sha256

        // The OWNER'S BODY, fetched from the path the REGISTRY names. See `fetchCanonicalBody`.
        let canonical = fetchCanonicalBody row.Source

        // The registry lagging its own canonical is normal (a nightly bot reconciles it) and is NOT this
        // guard's business — but it IS worth saying out loud, because a reader comparing digests by hand
        // will otherwise be baffled by exactly the discrepancy that used to break this check.
        if canonical <> registrySha then
            printfn
                "  frozen-mirror NOTE   %-22s registry sha256 (%s…) lags its canonical (%s…) — the autofix bot will reconcile it; this guard judges the BODY."
                skill.Id
                (registrySha.Substring(0, min 12 registrySha.Length))
                (canonical.Substring(0, 12))

        let baseline = baselineOf repoRoot relative local

        match baseline with
        | Unknown why ->
            printfn
                "  frozen-mirror NOTE   %-22s git cannot say whether this change edited it (%s). The REQUIRED lane reds on that; here it only weakens the attribution of any drift below."
                skill.Id
                why
        | UnchangedHere _
        | EditedHere _
        | AddedHere -> ()

        let verdict = decide baseline local registrySha canonical

        match verdict with
        | InSync ->
            // In sync. Any waiver for it is now a lie.
            match waivers |> List.tryFind (fun w -> w.Id = skill.Id) with
            | Some _ ->
                failed <- true

                eprintfn
                    "::error file=%s::STALE WAIVER — `%s` now matches its canonical in %s, so the waiver in scripts/check-frozen-mirrors.fsx is excusing drift that no longer exists. Delete it. A waiver kept past its cause is what hides the next break (#541)."
                    relative
                    skill.Id
                    skill.Owner
            | None -> printfn "  frozen-mirror OK     %-22s == %s canonical" skill.Id skill.Owner

        | drift ->
            match waivers |> List.tryFind (fun w -> w.Id = skill.Id && w.DriftedSha = local) with
            | Some w -> printfn "  frozen-mirror WAIVED %-22s (known drift) — %s" skill.Id (w.Because.Substring(0, min 60 w.Because.Length) + "…")
            | None ->
                if failsIn Freshness drift then
                    failed <- true

                // A waiver pinned to a DIFFERENT body means this mirror was edited again — which is only a
                // coherent accusation for `MirrorEdited`, so `describe` is the one that decides what to do
                // with it.
                let waivedAt =
                    waivers
                    |> List.tryFind (fun w -> w.Id = skill.Id)
                    |> Option.map (fun w -> w.DriftedSha)

                // `row.Owner`/`row.Source`, not the pin's: this message carries the runnable re-freeze
                // command, and it must name the file the canonical ACTUALLY lives in. See the fetch above.
                eprintfn "%s" (describe drift skill.Id row.Owner row.Source relative baseline local canonical waivedAt)

    // A waiver for a skill that is not a mirror at all is dead weight that will mask a real one later.
    for w in waivers do
        if not (mirrors |> List.exists (fun (skill, _, _) -> skill.Id = w.Id)) then
            failed <- true

            eprintfn
                "::error title=stale waiver::scripts/check-frozen-mirrors.fsx waives `%s`, but it is not a frozen mirror this repo ships. Delete the waiver."
                w.Id

    printfn ""
    printfn "frozen-mirror [freshness]: %d mirror(s) compared against their canonicals, %d waived" (List.length mirrors) (List.length waivers)

// ---- run ---------------------------------------------------------------------------------------

for lane in lanes do
    match lane with
    | Required -> runRequired ()
    | Freshness -> runFreshness ()

    printfn ""

if failed then exit 1 else exit 0
