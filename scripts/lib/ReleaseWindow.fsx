// THE RELEASE WINDOW — the one commit where a pin names a package the feed does not have yet.
//
// A version bump and the tags/packages that publish it CANNOT land atomically: `release-tags.yml` cuts
// `fs-gg-ui/v*` -> `fs-gg-ui-template/v*` -> `v*` and calls `release.yml` only AFTER the bump commit is
// on `main`, because a tag can only point at a commit that exists. So on the bump commit itself, and
// only there, `<FsGgUiVersion>` names a version no feed can serve. Any rule that resolves the pin
// against the feed is therefore UNSATISFIABLE on exactly the change that performs the bump.
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
// WHAT IT IS NOT. This is not "the pin is missing from the feed, so assume a release". A feed outage,
// or a typo'd pin, must stay RED. The subject is the COMMIT: did THIS change bump the value? Only then
// is the absent package a legal transient rather than a defect — and the coherence guard independently
// fails `pin-no-tag` for an unpublished pin that this commit did NOT bump, so the two compose.

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
/// Env-free by construction: `HEAD~1` is the first parent, which is the base branch for a
/// `pull_request` merge-ref checkout AND the previous `main` commit for a squash/merge push — so the
/// same diff answers both contexts without reading GITHUB_*. Returns `Error` if git cannot answer
/// (e.g. a shallow clone with no `HEAD~1`) so the caller can fail CLOSED rather than green-by-absence;
/// CI must use `fetch-depth: 0`, which it already does.
let bumpedInCommitUnderTest (repoRoot: string) (rel: string) (element: string) : Result<bool, string> =
    let ec, out =
        run repoRoot "git" [ "diff"; "HEAD~1"; "HEAD"; "--unified=0"; "--"; rel ]

    if ec <> 0 then
        Error(
            sprintf
                "git diff HEAD~1 HEAD -- %s failed — need full history (fetch-depth: 0); fail closed rather than green-by-absence"
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
