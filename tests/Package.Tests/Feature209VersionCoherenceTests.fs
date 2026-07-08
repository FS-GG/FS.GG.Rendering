module Feature209VersionCoherenceTests

// Feature 209 — release-lane / local-dev mirror of the version-coherence verdict.
//
// A1 AUTHORITY: this xUnit/Expecto wrapper MIRRORS, never replaces, the canonical documented shell
// scenarios in specs/209-version-staleness-guard/readiness/version-coherence-scenarios.md (the source
// of truth). It re-derives the STRUCTURAL verdict env-free (no pack/restore) so the coherent baseline
// passing + the forced-drift fixtures going red are also enforced in the release lane and locally.
// The deeper generate→restore→build of a product from the template stays in release.yml (T032), not
// duplicated here.

open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

// ---- preview-aware SemVer comparator (mirrors the script's D7 comparator) ----------------------
let private parse (s: string) =
    let s = s.Trim()
    let core, pre =
        match s.IndexOf '-' with
        | -1 -> s, ""
        | i -> s.Substring(0, i), s.Substring(i + 1)
    let nums = core.Split('.')
    let n i = if i < nums.Length then int nums.[i] else 0
    (n 0, n 1, n 2), (if pre = "" then [] else pre.Split('.') |> List.ofArray)

let private cmpId (a: string) (b: string) =
    match Int32.TryParse a, Int32.TryParse b with
    | (true, x), (true, y) -> compare x y
    | (true, _), (false, _) -> -1
    | (false, _), (true, _) -> 1
    | _ -> String.CompareOrdinal(a, b)

let private cmp (a: string) (b: string) =
    let (ca, pa), (cb, pb) = parse a, parse b
    if ca <> cb then compare ca cb
    else
        match pa, pb with
        | [], [] -> 0
        | [], _ -> 1
        | _, [] -> -1
        | _ ->
            let rec loop xs ys =
                match xs, ys with
                | [], [] -> 0
                | [], _ -> -1
                | _, [] -> 1
                | x :: xs', y :: ys' -> let c = cmpId x y in if c <> 0 then c else loop xs' ys'
            loop pa pb

// ---- env-free readers (re-derived directly from the repo) --------------------------------------
let private propsText = File.ReadAllText(repo "template/base/Directory.Packages.props")
let private nuspecText = File.ReadAllText(repo "src/Meta/FS.GG.UI.nuspec")

let private pinVersion =
    Regex.Match(propsText, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>").Groups.[1].Value.Trim()

let private pinOccurrences = Regex.Matches(propsText, "<FsGgUiVersion>([^<]*)</FsGgUiVersion>").Count

let private tagVersions () =
    let psi = ProcessStartInfo("git")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    [ "tag"; "--list"; "fs-gg-ui/v*" ] |> List.iter psi.ArgumentList.Add
    let out =
        match Process.Start psi with
        | null -> failwith "git tag could not be started"
        | p ->
            use p = p
            let o = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            o
    out.Replace("\r\n", "\n").Split('\n')
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (fun s -> s.StartsWith("fs-gg-ui/v", StringComparison.Ordinal))
    |> Array.map (fun s -> s.Substring("fs-gg-ui/v".Length))
    |> Array.toList

// P5 (#48) — release-lane readers: the template PACKAGE version-of-truth and the v* / template tag
// lanes, decoupled from the framework pin above.
let private templateFsprojText = File.ReadAllText(repo ".template.package/FS.GG.UI.Template.fsproj")

let private pkgVersion =
    Regex.Match(templateFsprojText, "<Version>([^<]+)</Version>").Groups.[1].Value.Trim()

let private pkgOccurrences = Regex.Matches(templateFsprojText, "<Version>([^<]*)</Version>").Count

/// Versions carried by tags matching `glob` whose ref starts with `prefix` (prefix stripped).
let private gitTagVersions (glob: string) (prefix: string) =
    let psi = ProcessStartInfo("git")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    [ "tag"; "--list"; glob ] |> List.iter psi.ArgumentList.Add
    let out =
        match Process.Start psi with
        | null -> failwith "git tag could not be started"
        | p ->
            use p = p
            let o = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            o
    out.Replace("\r\n", "\n").Split('\n')
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (fun s -> s.StartsWith(prefix, StringComparison.Ordinal))
    |> Array.map (fun s -> s.Substring(prefix.Length))
    |> Array.toList

/// Did the commit under test change the VALUE of `<element>` in `rel`? — mirrors the script's
/// RELEASE-PENDING signal (scripts/validate-version-coherence.fsx `bumpedInCommitUnderTest`), and must
/// stay in lockstep with it: these assertions are the second, independent classifier of the same
/// invariant.
///
/// A bump and the tag that publishes it cannot land atomically — the tag points at the commit carrying
/// the bump — so "this version already has a tag" is unsatisfiable on the bump itself. `HEAD~1` is the
/// first parent: the base branch under a `pull_request` merge-ref checkout, the previous `main` commit
/// under a squash/merge push. Both answer "did THIS change bump it?" with no env var.
///
/// Compares VALUES, not touched lines: this predicate waives a fail-closed assertion, so a reindent of
/// the `<Version>` line must not silence it.
let private bumpedInCommitUnderTest (rel: string) (element: string) =
    let psi = ProcessStartInfo("git")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    [ "diff"; "HEAD~1"; "HEAD"; "--unified=0"; "--"; rel ] |> List.iter psi.ArgumentList.Add
    let ec, out =
        match Process.Start psi with
        | null -> failwith "git diff could not be started"
        | p ->
            use p = p
            let o = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            p.ExitCode, o
    if ec <> 0 then
        failwithf "git diff HEAD~1 HEAD -- %s failed — need full history (fetch-depth: 0); fail closed" rel
    let rx = Regex(sprintf "<%s>([^<]*)</%s>" (Regex.Escape element) (Regex.Escape element))
    let valuesOn (sign: char) =
        let header = String(sign, 3)
        out.Replace("\r\n", "\n").Split('\n')
        |> Array.filter (fun l -> l.Length > 0 && l.[0] = sign && not (l.StartsWith(header, StringComparison.Ordinal)))
        |> Array.choose (fun l ->
            let m = rx.Match l
            if m.Success then Some(m.Groups.[1].Value.Trim()) else None)
        |> Set.ofArray
    let removed = valuesOn '-'
    let added = valuesOn '+'
    not added.IsEmpty && added <> removed

let private pinBumpedHere () = bumpedInCommitUnderTest "template/base/Directory.Packages.props" "FsGgUiVersion"
let private pkgBumpedHere () = bumpedInCommitUnderTest ".template.package/FS.GG.UI.Template.fsproj" "Version"

/// The three release tags have a MANDATED PUSH ORDER — only the last triggers release.yml:
///
///     fs-gg-ui/v<pin>  →  fs-gg-ui-template/v<pkg>  →  v<pkg>
///
/// so `v<pkg>` existing means the release is UNDER WAY: every tag before it is due NOW, not "next".
/// This bound is what keeps the RELEASE-PENDING waiver OUT of release.yml. That workflow triggers on
/// `push: tags: ['v*']` and runs THIS mirror at the tag commit — which is the commit that bumped
/// <Version>, so `pkgBumpedHere` is true here. Unbounded, the waiver let a `v*`-pushed-first release
/// pass, `publish-packages` (needs: package-tests) ship the set, and template-dispatch.yml — which
/// triggers ONLY on `fs-gg-ui-template/v*` — never fire: published, unannounced (FS-GG/.github#250).
let private releaseTagCut () = List.contains pkgVersion (gitTagVersions "v*" "v")

let private discoveredMembers () =
    Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun proj ->
        let t = File.ReadAllText proj
        let m name = Regex.Match(t, sprintf "<%s>([^<]*)</%s>" name name)
        let pid = let g = m "PackageId" in if g.Success then g.Groups.[1].Value.Trim() else ""
        let packable = let g = m "IsPackable" in g.Success && g.Groups.[1].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
        if packable && pid.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then Some pid else None)
    |> Set.ofArray

let private bomDeps () =
    Regex.Matches(nuspecText, "<dependency\\s+id=\"([^\"]+)\"\\s+version=\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
    |> Seq.toList

let private templatePins () =
    Regex.Matches(propsText, "<PackageVersion\\s+Include=\"(FS\\.GG\\.UI\\.[^\"]+)\"\\s+Version=\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
    |> Seq.toList

let private templateExpected =
    Set.ofList
        // Feature 240 (#73): FS.GG.UI.Canvas is pinned for the game/sample-pack profiles (FixedStep + Rng
        // simulation primitives consumed via the fs-gg-game-core skill) — a 12-member manifest.
        [ "FS.GG.UI.Build"; "FS.GG.UI.Scene"; "FS.GG.UI.Canvas"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Elmish"
          "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Controls.Elmish"
          "FS.GG.UI.DesignSystem"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Testing" ]

[<Tests>]
let feature209VersionCoherenceTests =
    testList "Feature209 version coherence (structural verdict mirror)" [

        // T008 — comparator self-check on the exact spec edge pairs (preview-aware, not string compare).
        test "preview-aware comparator orders the spec edge pairs" {
            Expect.isTrue (cmp "0.1.9-preview.1" "0.1.10-preview.1" < 0) "0.1.9-preview.1 < 0.1.10-preview.1 (numeric core, not lexical)"
            Expect.isTrue (cmp "0.1.51-preview.1" "0.1.51-preview.2" < 0) "…-preview.1 < …-preview.2"
            Expect.isTrue (cmp "0.1.51-preview.1" "0.1.51-preview.1" = 0) "equal versions compare equal"
        }

        // Scenario A / US1 #3 — the coherent baseline: single literal, pin == an existing tag and not
        // lagging the latest. `pin-no-tag` is waived when THIS change bumps the pin: the fs-gg-ui/v* tag
        // can only be cut on the resulting commit, so requiring it here is unsatisfiable (that is why
        // this assertion went red on every framework-major PR and was merged past as an "expected red").
        // The waiver is bounded by `releaseTagCut ()`: once `v<pkg>` exists the snapshot tag was due
        // BEFORE it (push order), so a mis-ordered release fails here rather than publishing.
        test "coherent baseline: single literal, pin matches latest snapshot tag (no lag, no phantom)" {
            let tags = tagVersions ()
            Expect.equal pinOccurrences 1 "exactly one <FsGgUiVersion> literal"
            Expect.isNonEmpty tags "fs-gg-ui/v* tags must be visible (fetch-depth: 0); empty ⇒ fail closed"
            let latest = tags |> List.sortWith cmp |> List.last
            Expect.isFalse (cmp pinVersion latest < 0) (sprintf "pin %s must not lag latest tag %s (pin-lags-tag)" pinVersion latest)
            // A pending fs-gg-ui/v<pin> snapshot exists only as part of a framework release, which bumps
            // pin AND package together (pin-leads-package forbids pin > pkg). A pin bumped alone is
            // pinning at a snapshot nobody is cutting — never pending. And once v<pkg> is cut the
            // snapshot tag was due before it (push order), so the waiver is off.
            let pinPending = pinBumpedHere () && pkgBumpedHere () && not (releaseTagCut ())
            if not pinPending then
                Expect.isTrue (List.contains pinVersion tags) (sprintf "pin %s is untagged and this is not a pending framework release (bump of pin+<Version> with v%s not yet cut) ⇒ the fs-gg-ui/v%s snapshot tag was never cut (pin-no-tag)" pinVersion pkgVersion pinVersion)
        }

        // Scenario B / T013 — the forced 204-lag fixture goes red (preview-aware).
        test "fixture: a lagging pin is detected as pin-lags-tag" {
            let tags = tagVersions ()
            let latest = tags |> List.sortWith cmp |> List.last
            Expect.isTrue (cmp "0.1.0-preview.1" latest < 0) "the 204 stale pin lags the latest tag"
        }

        // Scenario E / T012 — a phantom pin (ahead of every tag) has no snapshot tag.
        test "fixture: a phantom pin has no snapshot tag" {
            let tags = tagVersions ()
            Expect.isFalse (List.contains "0.1.99-preview.1" tags) "0.1.99-preview.1 is a phantom (no fs-gg-ui/v tag)"
        }

        // P5 (#48) — the template-package RELEASE lane vs the framework pin, mirroring the script's
        // releaseLaneFailures: the package does not LAG the latest v* / fs-gg-ui-template/v* tag, is not
        // left UNTAGGED by a release that was never cut, and the framework pin does not LEAD it
        // (pin <= package — a template-only release advances the package over an unchanged pin).
        //
        // The no-tag conjuncts are waived when THIS change bumps <Version>: the tags point at the commit
        // carrying the bump, so they cannot exist yet. That transient is RELEASE-PENDING, not drift. If
        // the tags are never cut, the next commit to main no longer bumps <Version> and these fire.
        //
        // `pkg-no-template-tag`'s waiver is additionally bounded by `releaseTagCut ()` — see its doc
        // comment. `v*` lands LAST in the push order, so `pkg-no-release-tag` needs no such bound (it
        // is only reached when `v<pkg>` is absent, which is exactly when its waiver is legitimate).
        test "release lane: template package matches v*/template tags (no lag) and pin does not lead" {
            Expect.equal pkgOccurrences 1 "exactly one <Version> in .template.package (release-lane source)"
            let releaseTags = gitTagVersions "v*" "v"
            let templateTags = gitTagVersions "fs-gg-ui-template/v*" "fs-gg-ui-template/v"
            Expect.isNonEmpty releaseTags "v* release tags must be visible (fetch-depth: 0); empty ⇒ fail closed"
            Expect.isNonEmpty templateTags "fs-gg-ui-template/v* tags must be visible; empty ⇒ fail closed"
            let latestRelease = releaseTags |> List.sortWith cmp |> List.last
            let latestTemplate = templateTags |> List.sortWith cmp |> List.last
            let bumped = pkgBumpedHere ()
            let templateTagPending = bumped && not (releaseTagCut ())
            Expect.isFalse (cmp pkgVersion latestRelease < 0) (sprintf "package %s must not lag latest v* tag %s (pkg-lags-release-tag)" pkgVersion latestRelease)
            if not bumped then
                Expect.isTrue (List.contains pkgVersion releaseTags) (sprintf "package %s is untagged and THIS change did not bump <Version> ⇒ the v%s release tag was never cut (pkg-no-release-tag)" pkgVersion pkgVersion)
            Expect.isFalse (cmp pkgVersion latestTemplate < 0) (sprintf "package %s must not lag latest fs-gg-ui-template/v* tag %s (pkg-lags-template-tag)" pkgVersion latestTemplate)
            if not templateTagPending then
                Expect.isTrue (List.contains pkgVersion templateTags) (sprintf "package %s has no fs-gg-ui-template/v%s tag, and either THIS change did not bump <Version> or v%s is already cut so the template tag was due before it (push order) ⇒ template-dispatch.yml never fired (pkg-no-template-tag)" pkgVersion pkgVersion pkgVersion)
            Expect.isFalse (cmp pkgVersion pinVersion < 0) (sprintf "framework pin %s must not lead the released package %s (pin-leads-package)" pinVersion pkgVersion)
        }

        // US2 / FR-003/004 — BOM token + bracket + member parity (policy-independent, structural).
        test "BOM: single [$version$] token, exact bracket, B.ids == P.members" {
            let deps = bomDeps ()
            let ids = deps |> List.map fst |> Set.ofList
            let members = discoveredMembers ()
            Expect.equal ids members "BOM dependency-id set must equal the discovered packable FS.GG.UI.* set"
            for id, v in deps do
                Expect.equal v "[$version$]" (sprintf "%s must use the single [$version$] token" id)
                Expect.isTrue (v.StartsWith "[" && v.EndsWith "]" && not (v.Contains ",")) (sprintf "%s must be exact-bracket" id)
        }

        // US2 / FR-005/D6 — template pins all derive, ⊆ published, == the 11-member manifest.
        test "template pins all derive through $(FsGgUiVersion) and equal the 11-member manifest" {
            let pins = templatePins ()
            let ids = pins |> List.map fst |> Set.ofList
            let members = discoveredMembers ()
            for id, v in pins do
                Expect.equal v "$(FsGgUiVersion)" (sprintf "%s must derive through $(FsGgUiVersion), not a hardcoded literal" id)
            Expect.isTrue (Set.isSubset ids members) "consumed pins ⊆ published members"
            Expect.equal ids templateExpected "consumed set must equal the documented 12-member manifest"
        }

        // FR-005 — build.fsx's runtime regex still matches the literal (208 half-rename class).
        test "build.fsx runtime regex still resolves the literal" {
            let buildText = File.ReadAllText(repo "template/base/build.fsx")
            Expect.isTrue (Regex.IsMatch(buildText, "<FsGgUiVersion>\\(\\[\\^<\\]\\+\\)</FsGgUiVersion>")) "build.fsx keeps the resolution regex"
            Expect.isTrue (Regex.IsMatch(propsText, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>")) "the literal still matches that regex"
        }
    ]
