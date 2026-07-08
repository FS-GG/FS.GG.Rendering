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
    |> Array.filter (fun s -> Regex.IsMatch(s, @"^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.\-]+)?$"))
    |> Array.toList

// P5 (#48) — release-lane readers: the template PACKAGE version-of-truth and the v* / template tag
// lanes, decoupled from the framework pin above.
let private templateFsprojText = File.ReadAllText(repo ".template.package/FS.GG.UI.Template.fsproj")

let private pkgVersion =
    Regex.Match(templateFsprojText, "<Version>([^<]+)</Version>").Groups.[1].Value.Trim()

let private pkgOccurrences = Regex.Matches(templateFsprojText, "<Version>([^<]*)</Version>").Count

/// Versions carried by tags matching `glob` whose ref starts with `prefix` (prefix stripped).
/// Fails CLOSED, mirroring the script's `tagVersionsOf`: a git failure or an unfetched tag namespace
/// must never be answerable as "no tags". The waiver bounds below (`releaseTagCut`) are keyed on tag
/// PRESENCE, so an empty-by-accident list would silently GRANT a waiver — green-by-absence, in the one
/// job that gates `publish-packages`. The script raises GuardError here; so must this. (The script's
/// module-level raises escape its `try/with` and surface as exit 1, not the contract's exit 2 — a
/// pre-existing defect. Both still fail CLOSED, which is what this bound depends on.)
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
    if ec <> 0 then
        failwithf "git tag --list %s failed — tags must be visible (fetch-depth: 0); fail closed rather than green-by-absence" glob
    let versions =
        out.Replace("\r\n", "\n").Split('\n')
        |> Array.map (fun s -> s.Trim())
        |> Array.filter (fun s -> s.StartsWith(prefix, StringComparison.Ordinal))
        |> Array.map (fun s -> s.Substring(prefix.Length))
        // `v*` also matches `vnext`, `validate`, `v2-wip`. Filter by SHAPE, not by the glob: an
        // unparseable stray raises out of the sort comparer, and a numeric one (`v9.9`) invents a lag.
        |> Array.filter (fun s -> Regex.IsMatch(s, @"^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.\-]+)?$"))
        |> Array.toList
    if versions.IsEmpty then
        failwithf "no %s tags visible — CI must fetch tags (fetch-depth: 0); fail closed rather than green-by-absence" glob
    versions

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
