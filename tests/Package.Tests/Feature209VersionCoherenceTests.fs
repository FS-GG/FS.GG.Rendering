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

/// Run `exe args` in `workDir`; return its exit code and stdout+stderr merged. Used by the exit-code
/// contract tests and the #514 fixture, which invoke the real guard script against a throwaway root.
///
/// THE CHILD NEVER INHERITS THE RELEASE LANE (#679). `release.yml`'s `package-tests` sets
/// FS_GG_VERSION_COHERENCE_RELEASE_LANE=1 for the whole JOB, and a spawned `dotnet fsi` inherits the
/// job's environment. That variable disables every RELEASE-PENDING waiver — right for THIS process (the
/// in-process mirror below reads it deliberately; it is the point of the release lane) and wrong for a
/// guard run against a SYNTHETIC repo whose tag namespace the fixture owns. The #514 fixture asserts the
/// guard is coherent at a release commit whose tags are NOT CUT YET — a state that is legal only BECAUSE
/// the waivers hold — so inheriting the lane made it demand tags for its own fixture version one step
/// before it cuts them. It went DRIFT on a repo that was fine, and only in the release lane: the PR gate
/// never sets the variable, so this passed every PR and failed the one run that publishes. That is the
/// 0.9.1 wedge — the release aborted, `main` stayed pinned to a version nobody published, and every PR
/// went red on NU1102.
///
/// The fixtures own their world; the ambient lane of the job running them is not part of it. Scrubbed at
/// the single choke point every subprocess here goes through, so a new guard-spawning test cannot
/// reintroduce this by forgetting.
let private runIn (workDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.Environment.Remove "FS_GG_VERSION_COHERENCE_RELEASE_LANE" |> ignore
    args |> List.iter psi.ArgumentList.Add
    match Process.Start psi with
    | null -> failwithf "%s could not be started" exe
    | p ->
        use p = p
        let out = p.StandardOutput.ReadToEnd()
        let err = p.StandardError.ReadToEnd()
        p.WaitForExit()
        p.ExitCode, out + err

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
    |> Array.filter (fun s -> Regex.IsMatch(s, @"^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.\-]+)?$"))
    |> Array.toList

// P5 (#48) — release-lane readers: the template PACKAGE version-of-truth and the v* / template tag
// lanes, decoupled from the framework pin above.
let private templateFsprojText = File.ReadAllText(repo ".template.package/FS.GG.UI.Template.fsproj")

let private pkgVersion =
    Regex.Match(templateFsprojText, "<Version>([^<]+)</Version>").Groups.[1].Value.Trim()

let private pkgOccurrences = Regex.Matches(templateFsprojText, "<Version>([^<]*)</Version>").Count

/// The FAIL-CLOSED decision, isolated from the process call so it can be exercised over its whole state
/// space (`tagQueryFailClosed`, below) instead of only over a live repo that is always healthy.
///
/// Both inputs are errors. `ec <> 0` is git refusing to answer. `[]` is subtler and is the one that has
/// bitten: the waiver bounds are keyed on tag PRESENCE (`releaseTagCut = List.contains v tags`), so an
/// empty-by-accident list answers "not cut" to every question and silently GRANTS every waiver —
/// green-by-absence, in the one job that gates `publish-packages`. Neither may be answerable as "no tags".
let internal tagsOrFailClosed (glob: string) (ec: int) (versions: string list) : string list =
    if ec <> 0 then
        failwithf "git tag --list %s failed — tags must be visible (fetch-depth: 0); fail closed rather than green-by-absence" glob
    if versions.IsEmpty then
        failwithf "no %s tags visible — CI must fetch tags (fetch-depth: 0); fail closed rather than green-by-absence" glob
    versions

/// Versions carried by tags matching `glob` whose ref starts with `prefix` (prefix stripped).
/// Fails CLOSED via `tagsOrFailClosed`, mirroring the script's `tagVersionsOf`.
let private gitTagVersions (glob: string) (prefix: string) =
    let psi = ProcessStartInfo("git")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    [ "tag"; "--list"; glob ] |> List.iter psi.ArgumentList.Add
    let ec, out =
        match Process.Start psi with
        | null -> failwith "git tag could not be started"
        | p ->
            use p = p
            let o = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            p.ExitCode, o
    out.Replace("\r\n", "\n").Split('\n')
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (fun s -> s.StartsWith(prefix, StringComparison.Ordinal))
    |> Array.map (fun s -> s.Substring(prefix.Length))
    // `v*` also matches `vnext`, `validate`, `v2-wip`. Filter by SHAPE, not by the glob: an
    // unparseable stray raises out of the sort comparer, and a numeric one (`v9.9`) invents a lag.
    |> Array.filter (fun s -> Regex.IsMatch(s, @"^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.\-]+)?$"))
    |> Array.toList
    |> tagsOrFailClosed glob ec

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

/// Set by `release.yml`'s `package-tests` job — the job that gates `publish-packages`. See
/// `scripts/validate-version-coherence.fsx` `releaseLane`.
let private releaseLane = Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_RELEASE_LANE" = "1"

// ---- the waiver predicates, as PURE functions -------------------------------------------------
//
// The three release tags have a MANDATED PUSH ORDER — only the last triggers release.yml:
//
//     fs-gg-ui/v<pin>  →  fs-gg-ui-template/v<pkg>  →  v<pkg>
//
// A bump waives its own missing tag only while NO SUCCESSOR tag has been cut, and never in the release
// lane. These are `let`-bound over BOOLEANS, not over the live repo, for one reason: the live repo is
// always in the coherent steady state, so every waiver branch below is dead in every real run. That is
// exactly how commit 0c7e091 shipped a regression through a green suite. `waiverTruthTable` (below)
// exercises all 2^3 states of each; deleting a bound now fails a test.
//
// Keep in lockstep with scripts/validate-version-coherence.fsx (`pinPending` / `templateTagPending` /
// `releaseTagPending`) — the two are independent classifiers of one invariant.

/// `fs-gg-ui/v<pin>` — successors: `fs-gg-ui-template/v<pkg>`, `v<pkg>`.
let internal pinWaived (releaseLane: bool) (pinBumped: bool) (templateTagCut: bool) (releaseTagCut: bool) =
    not releaseLane && pinBumped && not templateTagCut && not releaseTagCut

/// `fs-gg-ui-template/v<pkg>` — successor: `v<pkg>`. Unbounded, this is the hole that let a
/// `v*`-pushed-first release pass `package-tests`, ship via `publish-packages`, and never fire
/// template-dispatch.yml (which triggers ONLY on `fs-gg-ui-template/v*`): published, unannounced
/// (FS-GG/.github#250). release.yml runs THIS mirror at the tag commit, where `pkgBumped` is true.
let internal templateTagWaived (releaseLane: bool) (pkgBumped: bool) (releaseTagCut: bool) =
    not releaseLane && pkgBumped && not releaseTagCut

/// `v<pkg>` — lands last, no successor to bound it. Its rule is reached only when `v<pkg>` is absent.
let internal releaseTagWaived (releaseLane: bool) (pkgBumped: bool) = not releaseLane && pkgBumped

/// A tag is a successor only WITHIN ITS OWN RELEASE, so each rule asks about the version IT is keyed
/// on. Both successor tags carry the template package's version; a framework release bumps pin and
/// package together (`pin-leads-package` forbids pin > pkg), so where a `fs-gg-ui/v<pin>` snapshot is
/// pending, `pin = pkg`. Keying the pin's bound on `pkgVersion` would count the PREVIOUS release's tags
/// as successors of a new snapshot — a false red on any pin-only bump.
let private templateTagCutFor v = List.contains v (gitTagVersions "fs-gg-ui-template/v*" "fs-gg-ui-template/v")
let private releaseTagCutFor v = List.contains v (gitTagVersions "v*" "v")

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
        // simulation primitives consumed via the fs-gg-game-core skill).
        // Issue #430: FS.GG.UI.Symbology (pure channel grammar) and FS.GG.UI.Symbology.Render (its
        // headless Scene->PNG design-loop bridge) are pinned for the app/sample-pack/game profiles — the
        // scaffold shipped the fs-gg-symbology skill and the Symbology api-surface while pinning neither,
        // so the loop it documents did not compile. A 14-member manifest.
        [ "FS.GG.UI.Build"; "FS.GG.UI.Scene"; "FS.GG.UI.Canvas"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Elmish"
          "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Controls.Elmish"
          "FS.GG.UI.DesignSystem"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Testing"
          "FS.GG.UI.Symbology"; "FS.GG.UI.Symbology.Render" ]

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
            // `fs-gg-ui/v<pin>` has TWO successors in the push order. Bounding on `v<pkg>` alone would
            // still waive when `fs-gg-ui-template/v<pkg>` was pushed first — and that tag fires
            // template-dispatch.yml, so FS.GG.Templates would be told to pin a framework snapshot that
            // was never cut and never published: announce-before-publish.
            let pinPending = pinWaived releaseLane (pinBumpedHere ()) (templateTagCutFor pinVersion) (releaseTagCutFor pinVersion)
            if not pinPending then
                Expect.isTrue (List.contains pinVersion tags) (sprintf "pin %s is untagged and its fs-gg-ui/v%s snapshot tag is not pending (release lane, or a successor tag in the push order is already cut, or this change did not bump the pin) ⇒ the tag was never cut (pin-no-tag)" pinVersion pinVersion)
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
            let cut = List.contains pkgVersion releaseTags
            // Asserted in PUSH ORDER, matching the script's `releaseLaneFailures`: on a stale release these
            // messages are the operator's instructions, and Expecto aborts the block at the first failure.
            // Telling them to push `v*` before `fs-gg-ui-template/v*` strands the release behind the very
            // bound this test enforces.
            Expect.isFalse (cmp pkgVersion latestTemplate < 0) (sprintf "package %s must not lag latest fs-gg-ui-template/v* tag %s (pkg-lags-template-tag)" pkgVersion latestTemplate)
            if not (templateTagWaived releaseLane bumped cut) then
                Expect.isTrue (List.contains pkgVersion templateTags) (sprintf "package %s has no fs-gg-ui-template/v%s tag, and it is not pending (release lane, or v%s is already cut so the template tag was due before it in the push order, or this change did not bump <Version>) ⇒ template-dispatch.yml never fired (pkg-no-template-tag)" pkgVersion pkgVersion pkgVersion)
            Expect.isFalse (cmp pkgVersion latestRelease < 0) (sprintf "package %s must not lag latest v* tag %s (pkg-lags-release-tag)" pkgVersion latestRelease)
            if not (releaseTagWaived releaseLane bumped) then
                Expect.isTrue cut (sprintf "package %s is untagged and not pending (release lane, or this change did not bump <Version>) ⇒ the v%s release tag was never cut (pkg-no-release-tag)" pkgVersion pkgVersion)
            Expect.isFalse (cmp pkgVersion pinVersion < 0) (sprintf "framework pin %s must not lead the released package %s (pin-leads-package)" pinVersion pkgVersion)
        }

        // The waiver bounds, exercised over their FULL state space. Everything above reads the live repo,
        // which is always coherent — so every waiver branch there is dead, and a deleted bound is
        // invisible. That is precisely how 0c7e091 shipped a regression through a green suite. These are
        // the tests that fail if a bound is removed. Constitution Principle V.
        test "waiver truth table: a bump waives its tag only while no successor tag is cut, never in the release lane" {
            // fs-gg-ui/v<pin> — successors: fs-gg-ui-template/v<pkg>, v<pkg>
            //                       lane   pinBumped  tmplCut  relCut
            Expect.isTrue  (pinWaived false true      false    false) "release PR/merge, no tags cut ⇒ pin waived"
            Expect.isFalse (pinWaived false false     false    false) "no pin bump ⇒ never pending"
            Expect.isFalse (pinWaived false true      true     false) "fs-gg-ui-template/v* cut first ⇒ snapshot tag was DUE BEFORE it (announce-before-publish)"
            Expect.isFalse (pinWaived false true      false    true ) "v* cut first ⇒ snapshot tag was DUE BEFORE it"
            Expect.isFalse (pinWaived false true      true     true ) "both successors cut ⇒ overdue"
            Expect.isFalse (pinWaived true  true      false    false) "release lane ⇒ no waiver, every tag is due"

            // fs-gg-ui-template/v<pkg> — successor: v<pkg>
            //                                 lane   pkgBumped  relCut
            Expect.isTrue  (templateTagWaived false true       false) "release PR/merge, v* not cut ⇒ template tag waived"
            Expect.isFalse (templateTagWaived false false      false) "no <Version> bump ⇒ never pending (a release that was never cut)"
            Expect.isFalse (templateTagWaived false true       true ) "v* cut first ⇒ template tag was DUE BEFORE it (publish-before-announce, #250)"
            Expect.isFalse (templateTagWaived true  true       false) "release lane ⇒ no waiver"

            // v<pkg> — lands last, no successor
            Expect.isTrue  (releaseTagWaived false true)  "bump ⇒ v* is due next"
            Expect.isFalse (releaseTagWaived false false) "no bump ⇒ the release was never cut"
            Expect.isFalse (releaseTagWaived true  true)  "release lane ⇒ a publish must be triggered by its own v* tag"
        }

        // The bounds are strictly ordered: a pin waiver implies a template-tag waiver implies a release-tag
        // waiver. If a refactor ever inverts one, this catches it without anyone reasoning about push order.
        test "waiver bounds are monotone along the push order" {
            for lane in [ false; true ] do
              for pinB in [ false; true ] do
                for pkgB in [ false; true ] do
                  for tCut in [ false; true ] do
                    for rCut in [ false; true ] do
                      if pinWaived lane pinB tCut rCut && pinB && pkgB then
                          Expect.isTrue (templateTagWaived lane pkgB rCut) "pin waived ⇒ template tag waived (its successor set is a superset)"
                      if templateTagWaived lane pkgB rCut then
                          Expect.isTrue (releaseTagWaived lane pkgB) "template tag waived ⇒ v* waived (v* lands last)"
        }

        // #188 — `[]` NEVER GRANTS A WAIVER.
        //
        // The first half asserts the counterfactual, so the reason for the fail-closed rule is executable
        // rather than a comment: fed an empty tag list, every `...TagCut` predicate answers false, and a
        // false `cut` is exactly what each waiver bound is waiting for. An empty list is therefore not a
        // neutral "no information" answer — it is an affirmative "no successor tag was cut", the most
        // permissive answer there is. Green-by-absence, in the job that gates `publish-packages`.
        //
        // The second half asserts the rule that makes that state unreachable.
        test "an empty tag list would grant every waiver, so it must be unreachable (fail closed)" {
            // Counterfactual: what `[]` says to the bounds. `pkgVersion` is a real, released version.
            let cutPerEmptyList = List.contains pkgVersion []
            Expect.isFalse cutPerEmptyList "an empty tag list reports even a RELEASED version as 'not cut'"
            Expect.isTrue (templateTagWaived false true cutPerEmptyList)
                "…and 'not cut' waives pkg-no-template-tag — the #250 publish-before-announce waiver, granted by absence"
            Expect.isTrue (pinWaived false true cutPerEmptyList cutPerEmptyList)
                "…and it waives pin-no-tag too: both of the pin's successor bounds read the same empty list"

            // So neither an empty list nor a git failure may ever reach the bounds.
            Expect.throws (fun () -> tagsOrFailClosed "v*" 0 [] |> ignore)
                "ec=0 with no tags (unfetched namespace / shallow clone) must fail closed, not return []"
            Expect.throws (fun () -> tagsOrFailClosed "v*" 128 [] |> ignore)
                "git could not answer ⇒ fail closed"
            // A non-empty list does not rescue a failed query: a partial read is still not an answer.
            Expect.throws (fun () -> tagsOrFailClosed "v*" 1 [ "0.1.51-preview.1" ] |> ignore)
                "a non-zero exit code fails closed even when the parsed output looks plausible"
            Expect.equal (tagsOrFailClosed "v*" 0 [ "0.1.51-preview.1" ]) [ "0.1.51-preview.1" ]
                "a successful, non-empty query passes through unchanged"
        }

        // #188 — the guard's EXIT-CODE CONTRACT (`scripts/validate-version-coherence.fsx` header §1):
        //   0 coherent · 1 drift · 2 guard error (inputs unreadable / tags not fetched / tooling failed)
        //
        // Failing closed is necessary but not sufficient: 1 and 2 mean different things to whoever reads
        // them. 1 says "the repo is incoherent — here are the named locations to fix"; 2 says "the guard
        // could not decide". Every input reader used to run at MODULE scope, i.e. before `main` and hence
        // outside the `try/with` that maps GuardError to 2, so `dotnet fsi` reported the escaping exception
        // as 1. A broken guard was indistinguishable from a drifting repo, and the fix for one is not the
        // fix for the other. `readInputs` is now called from inside `main`; these two cases pin that down.
        //
        // Both run the real script against a throwaway root (its `repoRoot` is the parent of its own
        // directory), so neither can be satisfied by the healthy repo this suite otherwise reads.
        test "guard error exits 2, not 1: an unreadable input is not reported as drift" {
            let tmp = Path.Combine(Path.GetTempPath(), "vcoh188-unreadable-" + Guid.NewGuid().ToString("N").Substring(0, 8))
            Directory.CreateDirectory(Path.Combine(tmp, "scripts")) |> ignore
            try
                File.Copy(repo "scripts/validate-version-coherence.fsx", Path.Combine(tmp, "scripts", "validate-version-coherence.fsx"))
                // No template/base/Directory.Packages.props under this root ⇒ `readFile` raises GuardError.
                let ec, out = runIn tmp "dotnet" [ "fsi"; Path.Combine("scripts", "validate-version-coherence.fsx") ]
                Expect.notEqual ec 1 (sprintf "an unreadable input must NOT be reported as DRIFT (exit 1):\n%s" out)
                Expect.equal ec 2 (sprintf "contract §1: inputs unreadable ⇒ exit 2:\n%s" out)
                Expect.stringContains out "GUARD ERROR" "the guard names itself as the failure, not the repo"
            finally
                try Directory.Delete(tmp, true) with _ -> ()
        }

        test "guard error exits 2, not 1: git failing to answer is not reported as drift" {
            let tmp = Path.Combine(Path.GetTempPath(), "vcoh188-nogit-" + Guid.NewGuid().ToString("N").Substring(0, 8))
            Directory.CreateDirectory(Path.Combine(tmp, "scripts")) |> ignore
            Directory.CreateDirectory(Path.Combine(tmp, "template", "base")) |> ignore
            try
                File.Copy(repo "scripts/validate-version-coherence.fsx", Path.Combine(tmp, "scripts", "validate-version-coherence.fsx"))
                // The pin reads fine; the very next thing the guard does is ask git for the snapshot tags.
                // `tmp` is under the system temp dir, so it is not a work tree: `git tag --list` exits non-zero.
                File.Copy(repo "template/base/Directory.Packages.props", Path.Combine(tmp, "template", "base", "Directory.Packages.props"))
                let ec, out = runIn tmp "dotnet" [ "fsi"; Path.Combine("scripts", "validate-version-coherence.fsx") ]
                Expect.notEqual ec 1 (sprintf "a git-query failure must NOT be reported as DRIFT (exit 1):\n%s" out)
                Expect.equal ec 2 (sprintf "contract §1: tags not fetched / git failed ⇒ exit 2:\n%s" out)
                Expect.stringContains out "git tag --list" "the guard names the query that could not be answered"
            finally
                try Directory.Delete(tmp, true) with _ -> ()
        }

        // #188 — the `workflow_dispatch (version:)` hole.
        //
        // `package-tests` sets FS_GG_VERSION_COHERENCE_RELEASE_LANE=1 and thereby proves a version fully
        // tagged before `publish-packages` runs. But it proves it of the version it READS FROM THE REPO,
        // and `publish-packages` ships the version it resolves FROM THE TRIGGER. On `release` / `push: tags`
        // those coincide. On `workflow_dispatch` `inputs.version` is free text, so the guard validated one
        // string and the job published another — untagged, and invisible to template-dispatch.yml, which
        // fires only on `fs-gg-ui-template/v*`.
        //
        // The binding step closes that. What makes it load-bearing is its POSITION: a check that runs after
        // the pack, or after either `dotnet nuget push`, cannot unpublish anything. Assert the order.
        test "release.yml: publish-packages binds the published version to the guard's subject, before publishing" {
            let yml = File.ReadAllText(repo ".github/workflows/release.yml")
            let idx (needle: string) = yml.IndexOf(needle, StringComparison.Ordinal)

            let verify = idx "Verify the version to publish is the version the guard validated"
            Expect.isGreaterThan verify -1 "publish-packages must verify the version it is about to ship"

            // It must read the guard's subject — the repo's <Version> — not merely echo the trigger's.
            let verifyStep = yml.Substring verify
            Expect.stringContains verifyStep ".template.package/FS.GG.UI.Template.fsproj"
                "the binding must compare against the repo's <Version>, the string the guard validated"
            Expect.stringContains verifyStep "steps.ver.outputs.push == 'true'"
                "a pack-only dry run publishes nothing and is exempt"

            // Position: before the template pack (which stamps $VER into the package) and before every push.
            Expect.isGreaterThan (idx "dotnet pack .template.package") verify
                "the version must be validated before it is stamped into a package"
            let firstPush = idx "dotnet nuget push"
            Expect.isGreaterThan firstPush verify "a check after the first push cannot unpublish it"
            Expect.isGreaterThan (yml.LastIndexOf("dotnet nuget push", StringComparison.Ordinal)) verify
                "…nor after the last (nuget.org dual-publish, ADR-0012)"
        }

        // #517 — THE CLASS: a release must not be able to red `main` after the fact.
        //
        // A committed, derived artifact whose inputs live OUTSIDE its commit is invalidated by the act of
        // cutting a tag, and there is no commit to blame. #515 was the unlucky draw — the Deterministic
        // gate is REQUIRED, so `main` went red and NOTHING IN THE REPO COULD MERGE, discovered one PR at a
        // time by five separate workers. #514 made `version-coherence.md` pure so it cannot happen to THAT
        // artifact again; this asserts the structural guard that stops the class returning via a NEW one.
        //
        // `release-tags.yml` now cuts the tags LOCALLY, re-derives the committed artifacts against the
        // post-cut world, and refuses to push if any of them moved. What makes that load-bearing is its
        // POSITION: the window is unclosable from the other end — `main` requires this very gate with
        // `enforce_admins`, so the workflow cannot push a repair commit to `main` for a red it caused, and
        // a check placed after `git push origin <tag>` cannot un-push it. Assert the order, exactly as the
        // `publish-packages` binding test above does.
        test "release-tags.yml: a tag cut that dirties a committed artifact fails the release, before any push" {
            let yml = File.ReadAllText(repo ".github/workflows/release-tags.yml")
            let idx (needle: string) = yml.IndexOf(needle, StringComparison.Ordinal)

            let regenerate = idx "re-deriving committed artifacts against the post-cut tag set"
            Expect.isGreaterThan regenerate -1
                "the cut must re-derive the committed artifacts against the tag set it just created (#517)"

            let check = idx "git status --porcelain --untracked-files=no"
            Expect.isGreaterThan check regenerate
                "…and read the diff AFTER regenerating them, or it is asserting nothing"

            // THE ORDER. A push cannot be taken back; the only moment the damage is preventable is before it.
            let firstPush = idx "git push origin \"$t\""
            Expect.isGreaterThan firstPush -1 "the cut still pushes the tags"
            Expect.isGreaterThan firstPush check
                "the artifact check must run BEFORE the first tag push — after it, main is already red and this workflow cannot repair it (main requires the Deterministic gate with enforce_admins)"

            // And it must actually FAIL the release, not merely warn: a red that does not stop the release
            // is the always-red advisory this repo already knows trains everyone to ignore it (#506).
            let failure = yml.Substring(check)
            Expect.stringContains failure "exit 1"
                "a dirtied artifact must fail the release — warning and pushing anyway is how #515 happened"
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

        // #514 (class #517) — CUTTING A RELEASE TAG MUST NOT ROT THE COMMITTED ARTIFACT.
        //
        // The verdict report is a committed artifact and the Deterministic gate byte-compares it against a
        // fresh render. It used to record the tags it OBSERVED (`git tag --list`) — external state that
        // changes with no commit — so every release rotted it: `release-tags.yml` cuts `fs-gg-ui/v<pin>`
        // AFTER the bump merges, and the artifact the release PR committed named the previous tag the
        // instant the new one appeared. The gate is REQUIRED on `main`, so main went red and every PR
        // branched from it inherited that red. #515 stopped the entire repo; #435 and #477 had each already
        // regenerated the file by hand. The fix records the tags the COMMIT OWES (pin/package-derived), so
        // there is nothing left for a tag push to invalidate.
        //
        // This replays a real release against a throwaway CLONE — bump the three files a release bump
        // actually touches (see 893757b5), regenerate, commit, then cut the tag triple — and asserts the
        // artifact still matches afterwards. On the pre-#514 script step 5 below FAILS with
        // `DRIFT [artifact-stale] … latest-snapshot-tag`, which is precisely #515.
        test "a release tag cut does not make the committed verdict artifact stale" {
            let tmp = Path.Combine(Path.GetTempPath(), "vcoh514-" + Guid.NewGuid().ToString("N").Substring(0, 8))

            let git args =
                let ec, out = runIn tmp "git" args
                if ec <> 0 then failwithf "git %s failed in the clone:\n%s" (String.concat " " args) out
                out

            let guard () = runIn tmp "dotnet" [ "fsi"; Path.Combine("scripts", "validate-version-coherence.fsx") ]

            try
                // A clone, so the tag namespace is ours to cut into and the real repo is never touched.
                // `--no-hardlinks`: the temp dir is routinely on a different filesystem from the checkout,
                // and git's default object-hardlinking fails outright across one.
                //
                // NOTE the clone takes HEAD, not the working tree — so this exercises the guard AS COMMITTED,
                // which is exactly the subject the gate judges. Editing the script without committing it will
                // not change what this test sees.
                let ec, out = runIn (Path.GetTempPath()) "git" [ "clone"; "--quiet"; "--no-hardlinks"; root; tmp ]
                if ec <> 0 then failwithf "could not clone the repo under test:\n%s" out

                git [ "config"; "user.email"; "vcoh514@example.invalid" ] |> ignore
                git [ "config"; "user.name"; "vcoh514" ] |> ignore

                // 1. Bump the coherent-set axes, exactly as a release does. Not a synthetic edit: these are
                //    the three files the 0.9.0 release (893757b5) touched, and the guard cross-checks all of
                //    them against the pin, so bumping fewer would fail for reasons that are not the subject.
                let current = pinVersion

                // Deliberately a version no real release will ever cut. The clone carries the repo's real
                // tags, and this test CREATES the triple for `next` — so deriving it from the pin (0.9.0 ->
                // 0.10.0) would plant a time bomb that goes off the day the repo actually ships that
                // version and `git tag` finds it already there. It only has to sort above every existing
                // tag, which is all the guard's ordering rules ask of a pin.
                let next = "999.0.0"

                for rel in
                    [ "template/base/Directory.Packages.props"
                      ".template.package/FS.GG.UI.Template.fsproj"
                      "template/product-skills/fs-gg-symbology/reference.fsx" ] do
                    let path = Path.Combine(tmp, rel.Replace('/', Path.DirectorySeparatorChar))
                    File.WriteAllText(path, File.ReadAllText(path).Replace(current, next))

                git [ "commit"; "-am"; sprintf "release: cut %s" next ] |> ignore

                // 2. Regenerate and fold the artifact INTO the release commit — what a release PR does, and
                //    what it must do: the bump and the artifact it invalidates belong to one commit.
                guard () |> ignore
                git [ "add"; "-A" ] |> ignore
                git [ "commit"; "--amend"; "--no-edit" ] |> ignore

                // 3. The release PR is green: pin bumped, tags not cut yet, waivers hold, artifact matches.
                let ec, out = guard ()
                Expect.equal ec 0 (sprintf "the release commit must be coherent before its tags are cut:\n%s" out)

                // 4. THE REGRESSION, asserted before anything else about the artifact's shape — so that a
                //    reintroduction fails HERE, naming the bug, rather than tripping a cosmetic field check
                //    further down. Cut the triple `release-tags.yml` cuts, in its order, and re-run. NOTHING
                //    IN THE TREE CHANGED: only the tag namespace, which no commit owns. That is the entire
                //    point — a committed artifact may not depend on it.
                for tag in [ sprintf "fs-gg-ui/v%s" next; sprintf "fs-gg-ui-template/v%s" next; sprintf "v%s" next ] do
                    git [ "tag"; tag ] |> ignore

                let ec, out = guard ()

                Expect.isFalse (out.Contains "artifact-stale")
                    (sprintf "cutting the release tags must not rot the committed artifact — this is #515, and it stopped the whole repo:\n%s" out)

                Expect.equal ec 0
                    (sprintf "the guard is still coherent after its own release tags are cut:\n%s" out)

                // 5. And the reason it survived: it recorded the tag the commit OWES, not the tag that
                //    happened to exist when it was rendered. Pre-#514 this line read the OBSERVED latest tag
                //    — the one the cut above was about to supersede.
                let artifact = File.ReadAllText(Path.Combine(tmp, "specs", "209-version-staleness-guard", "readiness", "version-coherence.md"))
                Expect.stringContains artifact (sprintf "snapshot-tag-for-pin: fs-gg-ui/v%s" next)
                    "the artifact records the snapshot tag the PIN owes — a value the tag cut cannot change"
                Expect.isFalse (artifact.Contains "latest-snapshot-tag")
                    "the observed-tag field is gone; reintroducing it makes the artifact self-invalidating again"
            finally
                // A clone is read-only .git objects; on Windows they are marked read-only and Delete throws.
                try
                    for f in Directory.EnumerateFiles(tmp, "*", SearchOption.AllDirectories) do
                        File.SetAttributes(f, FileAttributes.Normal)
                    Directory.Delete(tmp, true)
                with _ -> ()
        }
    ]
