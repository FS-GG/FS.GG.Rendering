// THE RELEASE WINDOW — a pin ahead of its release tags names a package the public feed does not have yet.
//
// A version bump and the tags/packages that publish it CANNOT land atomically: `release-tags.yml` cuts
// `fs-gg-ui/v*` -> `fs-gg-ui-template/v*` -> `v*` and calls `release.yml` only AFTER the bump commit is
// on `main`, because a tag can only point at a commit that exists. If protected preflight stops before
// mutation, the same state persists across the repair commit needed to resume publication.
//
// `scripts/validate-version-coherence.fsx` has always known this — it is what its RELEASE-PENDING
// waivers (`PinPending`/`TemplateTagPending`/`ReleaseTagPending`) are for, and its own
// `bumpedInCommitUnderTest` is the same predicate as the one below. This file exists because #848 gave
// the rule a SECOND consumer: the api-surface mirror became a build output generated from the pinned
// package, and its generator restores the pin. Nothing taught it the window, so a release bump reds
// `gate.yml`'s required `Deterministic gate` on NU1102 and the release cannot merge — while publishing
// requires merging. That deadlock is what this predicate closes (Rendering#815's cut found it; #848
// landed after v0.11.0, so no release had crossed that gate).
//
// ONE DEFINITION, because two would drift for the reason `release-tags.yml` already refuses to
// re-derive the pending tag set: a waiver predicate and the rules it waives must answer the same
// question. The coherence guard still carries its own copy — it is deliberately self-contained and
// `#load`-free, and its copy raises `GuardError` where this one returns a `Result`, so collapsing them
// is a refactor of a REQUIRED gate and belongs in its own change, not in the one unblocking a release.
// They must agree; converge them here when the guard is next opened.
//
// WHAT IT IS NOT. This is not "the pin is missing from the feed, so assume a release". The pin must be
// strictly ahead of every tag in its lane. A missing historical tag below a later release remains drift,
// and the release lane disables the waiver before publication.

module ReleaseWindow

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

let private run (workingDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    args |> List.iter psi.ArgumentList.Add
    psi.WorkingDirectory <- workingDir
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    use p = Process.Start psi
    let out = p.StandardOutput.ReadToEnd()
    p.StandardError.ReadToEnd() |> ignore
    p.WaitForExit()
    p.ExitCode, out

/// Did the commit under test change the VALUE of `<element>` in `rel`? — the RELEASE-PENDING signal.
///
/// Compares the element's VALUE across the diff, not merely whether its line was touched: this
/// predicate WAIVES a fail-closed rule, so a reindent or line-ending change to the pin line must not
/// be able to silence it. Added values must exist and differ from removed ones.
///
/// Exact-head PR checkouts supply their immutable base through `FS_GG_VERSION_COHERENCE_BASE_SHA`;
/// push/main falls back to `HEAD~1`. This is the same release-window input used by the independent
/// version-coherence classifier. The explicit value must be a full lowercase SHA and resolve as a
/// commit. Returns `Error` if the input or git history cannot be verified, so the caller fails CLOSED
/// rather than treating an unknown release window as steady state.
let bumpedInCommitUnderTest (repoRoot: string) (rel: string) (element: string) : Result<bool, string> =
    let explicitBase = Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_BASE_SHA"

    if not (String.IsNullOrEmpty explicitBase) && not (Regex.IsMatch(explicitBase, "^[0-9a-f]{40}$")) then
        Error $"FS_GG_VERSION_COHERENCE_BASE_SHA must be a full lowercase git SHA, got {explicitBase}"
    else
        let baseRevision = if String.IsNullOrEmpty explicitBase then "HEAD~1" else explicitBase
        let verifyEc, _ = run repoRoot "git" [ "rev-parse"; "--verify"; baseRevision + "^{commit}" ]

        if verifyEc <> 0 then
            Error $"release-window base {baseRevision} is not a resolvable commit — need full history (fetch-depth: 0); fail closed rather than green-by-absence"
        else
            let ec, out =
                run repoRoot "git" [ "diff"; baseRevision; "HEAD"; "--unified=0"; "--"; rel ]

            if ec <> 0 then
                Error(
                    sprintf
                        "git diff %s HEAD -- %s failed — need full history (fetch-depth: 0); fail closed rather than green-by-absence"
                        baseRevision
                        rel
                )
            else
                let rx =
                    Regex(sprintf "<%s>([^<]*)</%s>" (Regex.Escape element) (Regex.Escape element))

                let valuesOn (sign: char) =
                    let header = String(sign, 3) // "+++" / "---" file headers are not content lines

                    out.Replace("\r\n", "\n").Split('\n')
                    |> Array.filter (fun l ->
                        l.Length > 0
                        && l.[0] = sign
                        && not (l.StartsWith(header, StringComparison.Ordinal)))
                    |> Array.choose (fun l ->
                        let m = rx.Match l
                        if m.Success then Some(m.Groups.[1].Value.Trim()) else None)
                    |> Set.ofArray

                let removed = valuesOn '-'
                let added = valuesOn '+'
                Ok(not added.IsEmpty && added <> removed)

/// Is `version` strictly newer than every pushed tag in this release lane?
///
/// Unlike the exact-commit bump predicate, this remains true across a repair commit after a protected
/// publication stops before tag mutation. Exact membership is insufficient: a missing historical tag
/// below a newer release is drift, not a pending release. The protected release lane independently
/// disables consumers' waivers, and ordered successor tags bound earlier pending tags.
let versionAheadOfTags
    (repoRoot: string)
    (tagGlob: string)
    (tagPrefix: string)
    (version: string)
    : Result<bool, string> =
    let parse (value: string) =
        let value = value.Trim()
        let core, pre =
            match value.IndexOf '-' with
            | -1 -> value, [||]
            | i -> value.Substring(0, i), value.Substring(i + 1).Split('.')
        let parts = core.Split('.')
        if parts.Length < 2 || parts.Length > 3 then
            Error $"malformed version core: {value}"
        else
            let parsed = parts |> Array.map Int32.TryParse
            if parsed |> Array.exists (fst >> not) then Error $"malformed version core: {value}"
            else
                let nums = parsed |> Array.map snd
                Ok(nums.[0], nums.[1], (if nums.Length = 3 then nums.[2] else 0), pre)

    let compareIdentifiers (left: string) (right: string) =
        match Int32.TryParse left, Int32.TryParse right with
        | (true, l), (true, r) -> compare l r
        | (true, _), (false, _) -> -1
        | (false, _), (true, _) -> 1
        | _ -> StringComparer.Ordinal.Compare(left, right)

    let compareVersions left right =
        match parse left, parse right with
        | Error e, _ | _, Error e -> Error e
        | Ok(lmaj, lmin, lpatch, lpre), Ok(rmaj, rmin, rpatch, rpre) ->
            let core = compare (lmaj, lmin, lpatch) (rmaj, rmin, rpatch)
            if core <> 0 then Ok core
            elif lpre.Length = 0 && rpre.Length = 0 then Ok 0
            elif lpre.Length = 0 then Ok 1
            elif rpre.Length = 0 then Ok -1
            else
                let mutable verdict = 0
                let mutable index = 0
                while verdict = 0 && index < min lpre.Length rpre.Length do
                    verdict <- compareIdentifiers lpre.[index] rpre.[index]
                    index <- index + 1
                Ok(if verdict <> 0 then verdict else compare lpre.Length rpre.Length)

    let ec, output = run repoRoot "git" [ "tag"; "--list"; tagGlob ]
    if ec <> 0 then Error $"git tag --list {tagGlob} failed"
    else
        let tags =
            output.Replace("\r\n", "\n").Split('\n')
            |> Array.map (fun tag -> tag.Trim())
            |> Array.filter (fun tag -> tag.StartsWith(tagPrefix, StringComparison.Ordinal))
            |> Array.map (fun tag -> tag.Substring(tagPrefix.Length))
            |> Array.filter (fun tag -> Regex.IsMatch(tag, @"^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.\-]+)?$"))

        if tags.Length = 0 then Error $"no {tagGlob} tags visible — need fetch-depth: 0"
        else
            tags
            |> Array.fold
                (fun state tag ->
                    match state, compareVersions version tag with
                    | Error e, _ | _, Error e -> Error e
                    | Ok ahead, Ok comparison -> Ok(ahead && comparison > 0))
                (Ok true)

/// `PackageId -> project directory`, for every project under `src/` that actually ships a package.
///
/// Same discovery rule as `scripts/check-packed-api-surface.fsx`: a `<PackageId>` and not
/// `<IsPackable>false</IsPackable>`. `src/ColorPolicy` falls out by the rule rather than by a special
/// case — it declares no `PackageId` and is not packable, so it ships nothing.
let packableProjects (repoRoot: string) : Map<string, string> =
    Directory.GetFiles(Path.Combine(repoRoot, "src"), "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun project ->
        let text = File.ReadAllText project
        let packageId = Regex.Match(text, @"<PackageId>\s*([^<\s]+)\s*</PackageId>")

        let notPackable =
            Regex.IsMatch(text, @"<IsPackable>\s*false\s*</IsPackable>", RegexOptions.IgnoreCase)

        if packageId.Success && not notPackable then
            Some(packageId.Groups.[1].Value, Path.GetDirectoryName project)
        else
            None)
    |> Map.ofArray

/// The `.fsi` a project has, as forward-slashed paths relative to the project directory — which is
/// exactly what `Directory.Build.local.props`'s
/// `PackagePath="api-surface/%(RecursiveDir)%(Filename)%(Extension)"` produces in the nupkg. That
/// identity is what lets `src/` stand in for the package inside the window, and
/// `scripts/check-packed-api-surface.fsx` is what keeps it true: it proves, on every PR touching
/// `src/` and again immediately before `dotnet nuget push`, that the nupkg carries exactly this set.
let projectFsi (projectDir: string) : (string * string) list =
    Directory.GetFiles(projectDir, "*.fsi", SearchOption.AllDirectories)
    |> Array.map (fun f -> Path.GetRelativePath(projectDir, f).Replace('\\', '/'), f)
    |> Array.filter (fun (rel, _) ->
        not (
            rel.StartsWith("bin/", StringComparison.Ordinal)
            || rel.StartsWith("obj/", StringComparison.Ordinal)
        ))
    |> Array.toList
