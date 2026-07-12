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

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions

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
// Fetched, not vendored: a vendored copy is a fifth statement of the same fact, and it would go
// stale in exactly the direction this guard cannot see (the canonical moving underneath us).
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

let private waivers =
    [ { Id = "fs-gg-persistence"
        DriftedSha = "5848164d540ff236d6e3516251ca8c269ff48985ad0507b2733c4c05b4a4875e"
        Because =
          "Rendering#520 (#462), Rendering#539 (#445) and Rendering#590 (#550) each edited this body — correctly, and none of them was told it was a mirror. The content belongs in FS.GG.Game's canonical: FS-GG/FS.GG.Game#163 is the adoption, and it must be READ, not re-frozen over." }
      { Id = "fs-gg-audio"
        DriftedSha = "8421dfd19cc1c1f1fb05fa43539afbb18ec3746675a082704eee1894f8281fa1"
        Because = "Drifted before this guard existed. Route the content to FS.GG.Game's canonical (ADR-0022 §6); do not re-freeze without reading the diff." }
      { Id = "fs-gg-model-swap"
        DriftedSha = "4c88cf8fbafd878e8deaaefb5699dad751d332ada7e95fb921f62163041a3805"
        Because = "Drifted before this guard existed. Route the content to FS.GG.Game's canonical (ADR-0022 §6); do not re-freeze without reading the diff." } ]

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

    if local = row.Sha256 then
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
    else
        match waivers |> List.tryFind (fun w -> w.Id = row.Id && w.DriftedSha = local) with
        | Some w -> printfn "  frozen-mirror WAIVED %-20s (known drift) — %s" row.Id (w.Because.Substring(0, min 60 w.Because.Length) + "…")
        | None ->
            failed <- true

            let waived = waivers |> List.tryFind (fun w -> w.Id = row.Id)

            let extra =
                match waived with
                | Some w ->
                    $"\n\nThis mirror HAS a waiver, but for a different body ({w.DriftedSha.Substring(0, 12)}…) — you have edited it AGAIN. A waiver excuses the drift that already existed, never the next edit."
                | None -> ""

            eprintfn
                "::error file=%s::FROZEN MIRROR EDITED — `%s` is owned by %s (%s), and this repo only ships a byte-identical copy (ADR-0022 §6). Your copy now hashes %s; the canonical is %s.\n\nDO NOT REVERT YOUR WORK, do NOT re-freeze this file from the canonical (that would silently delete it), and DO NOT ADD A WAIVER FOR YOURSELF in scripts/check-frozen-mirrors.fsx — a waiver records drift that already existed before this guard, and is not a way to land new drift. Take the change to the OWNING repo's canonical body (%s), and re-freeze here once it lands. This repo gives you no other signal that you are editing a body it does not own, which is why three correct edits to this file merged green before this check existed (#541).%s"
                relative
                row.Id
                row.Owner
                row.Source
                (local.Substring(0, 12) + "…")
                (row.Sha256.Substring(0, 12) + "…")
                row.Source
                extra

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
