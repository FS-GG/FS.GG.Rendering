// Feature 209 — FS.GG.UI version-staleness / coherence guard.
//
// Makes the Feature-204 version-staleness bug class a LOUD, LOCAL, AUTOMATIC failure in this repo's
// own merge-blocking gate, before any consumer scaffolds a product. Mirrors the two-layer shape of
// scripts/validate-bom-consumer.fsx:
//
//   * Structural verdict-core (always, env-free): re-derives, from the repo + pushed git tags, that
//     the single <FsGgUiVersion> literal is well-formed and present exactly once; the pin matches an
//     existing fs-gg-ui/v<V> snapshot tag and does NOT lag the latest such tag (preview-aware
//     SemVer compare, not string); the BOM uses the single [$version$] exact-bracket token; the
//     packable FS.GG.UI.* set == the BOM dependency set; the template's consumed pins all derive
//     through $(FsGgUiVersion) and equal the documented 11-member manifest; and build.fsx's runtime
//     regex still matches the literal. Exits non-zero NAMING the specific mismatch expected-vs-actual.
//
//   * Restore-grounded proof (FS_GG_RUN_VERSION_COHERENCE_SMOKE=1): packs the 16 FS.GG.UI.* members
//     + the BOM from source at the pinned V to a throwaway feed, restores FS.GG.UI@V in a clean
//     consumer, and asserts the COMPLETE member set resolves to exactly V (FR-008, anti-text-grep);
//     a member off V fails loudly with [restore-partial], never a silent partial graph.
//
// Exit codes (contract §1): 0 coherent · 1 drift (>=1 conjunct false) · 2 guard error (inputs
// unreadable / tags not fetched / pack-restore tooling failed) — fails CLOSED, never green-by-absence.
//
// The repo-root <Version> (Directory.Build.props) is DECOUPLED by default (D5) and is NOT compared.

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName
let repo (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))
let live = Environment.GetEnvironmentVariable "FS_GG_RUN_VERSION_COHERENCE_SMOKE" = "1"

/// RELEASE LANE — set by any job that gates a PUBLISH (`release.yml`). Disables every RELEASE-PENDING
/// waiver: the waivers exist because a tag cannot point at a commit that does not exist yet, which is
/// only ever true BEFORE the merge. At publish time every tag is due, so nothing is pending, and a
/// missing tag is drift. Without this, `workflow_dispatch (version:)` — a first-class publish trigger
/// that creates NO tag — sails through every waiver and ships the coherent set unannounced.
///
/// It bounds the WAIVERS; it does not bind the guard's SUBJECT. This guard only ever validates the
/// REPO's `<Version>`, while `release.yml`'s publish job resolves the version it ships from the trigger
/// (`inputs.version` on a dispatch). Those are the same string only because release.yml now asserts they
/// are, immediately before pushing. Without that assertion the release lane validates one version and
/// publishes another — the `workflow_dispatch` hole.
let releaseLane = Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_RELEASE_LANE" = "1"

/// Raised for any unreadable input / unfetched tags / tooling failure ⇒ exit 2 (fail closed).
exception GuardError of string

// ---- shell helper -----------------------------------------------------------------------------
let run (workDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    let out = proc.StandardOutput.ReadToEnd()
    let err = proc.StandardError.ReadToEnd()
    proc.WaitForExit()
    proc.ExitCode, out + err

let readFile (path: string) =
    if not (File.Exists path) then raise (GuardError(sprintf "required input missing: %s" path))
    File.ReadAllText path

/// Did the commit under test change the VALUE of `<element>` in `rel`? — the RELEASE-PENDING signal.
///
/// A version bump and the tag that publishes it CANNOT land atomically: the tag can only point at
/// the commit that carries the bump, so it is cut *after* that commit exists. Any "this version must
/// already have a tag" rule is therefore unsatisfiable on the very change that performs the bump —
/// it fired on every release PR (#155, #159, #163) and on `main` it merely raced the tag push. This
/// predicate distinguishes the legal transient (the bump is HERE, the tag comes next) from the real
/// defect (an untagged version left behind by a release that was never cut).
///
/// Compares the element's VALUE across the diff, not merely whether its line was touched: this
/// predicate WAIVES a fail-closed rule, so a reindent or line-ending change to the `<Version>` line
/// must not be able to silence it. Added values must exist and differ from removed ones.
///
/// Env-free by construction: `HEAD~1` is the first parent, which is the base branch for a
/// `pull_request` merge-ref checkout AND the previous `main` commit for a squash/merge push — so the
/// same diff answers both contexts without reading GITHUB_*. Fails closed if git cannot answer
/// (e.g. a shallow clone with no HEAD~1): CI must use `fetch-depth: 0`, which it already does.
let bumpedInCommitUnderTest (rel: string) (element: string) =
    let ec, out = run repoRoot "git" [ "diff"; "HEAD~1"; "HEAD"; "--unified=0"; "--"; rel ]
    if ec <> 0 then
        raise (GuardError(sprintf "git diff HEAD~1 HEAD -- %s failed — need full history (fetch-depth: 0); fail closed rather than green-by-absence" rel))
    let rx = Regex(sprintf "<%s>([^<]*)</%s>" (Regex.Escape element) (Regex.Escape element))
    let valuesOn (sign: char) =
        let header = String(sign, 3) // "+++" / "---" file headers are not content lines
        out.Replace("\r\n", "\n").Split('\n')
        |> Array.filter (fun l -> l.Length > 0 && l.[0] = sign && not (l.StartsWith(header, StringComparison.Ordinal)))
        |> Array.choose (fun l ->
            let m = rx.Match l
            if m.Success then Some(m.Groups.[1].Value.Trim()) else None)
        |> Set.ofArray
    let removed = valuesOn '-'
    let added = valuesOn '+'
    not added.IsEmpty && added <> removed

// ---- preview-aware SemVer comparator (D7, T008) -----------------------------------------------
// Numeric major.minor.patch compared numerically; then dotted prerelease identifiers per SemVer §11
// (numeric identifiers numerically, alphanumeric lexically, numeric < alphanumeric, fewer < more,
// and a version WITHOUT prerelease outranks the same core WITH prerelease). Hand-rolled so the
// script needs no package reference.
module SemVer =
    type V = { Major: int; Minor: int; Patch: int; Pre: string list }

    let parse (s: string) : V =
        let s = s.Trim()
        let core, pre =
            match s.IndexOf '-' with
            | -1 -> s, ""
            | i -> s.Substring(0, i), s.Substring(i + 1)
        let nums = core.Split('.')
        let n i =
            if i < nums.Length then
                match Int32.TryParse nums.[i] with
                | true, v -> v
                | _ -> raise (GuardError(sprintf "malformed version (non-numeric core): %s" s))
            else 0
        { Major = n 0
          Minor = n 1
          Patch = n 2
          Pre = if pre = "" then [] else pre.Split('.') |> List.ofArray }

    let private cmpId (a: string) (b: string) =
        match Int32.TryParse a, Int32.TryParse b with
        | (true, x), (true, y) -> Operators.compare x y
        | (true, _), (false, _) -> -1 // numeric identifier has lower precedence than alphanumeric
        | (false, _), (true, _) -> 1
        | _ -> String.CompareOrdinal(a, b)

    /// -1 / 0 / +1, preview-aware.
    let cmp (a: V) (b: V) : int =
        let core =
            [ Operators.compare a.Major b.Major
              Operators.compare a.Minor b.Minor
              Operators.compare a.Patch b.Patch ]
            |> List.tryFind ((<>) 0)
            |> Option.defaultValue 0
        if core <> 0 then core
        else
            match a.Pre, b.Pre with
            | [], [] -> 0
            | [], _ -> 1 // no prerelease outranks prerelease
            | _, [] -> -1
            | pa, pb ->
                let rec loop xs ys =
                    match xs, ys with
                    | [], [] -> 0
                    | [], _ -> -1 // fewer identifiers ⇒ lower precedence
                    | _, [] -> 1
                    | x :: xs', y :: ys' ->
                        let c = cmpId x y in if c <> 0 then c else loop xs' ys'
                loop pa pb

    let lt a b = cmp (parse a) (parse b) < 0

/// Self-check the exact spec edge pairs (T008) — fail closed if the comparator ever regresses.
/// A function, not a module-level `do`: see `readInputs` for why nothing that can raise GuardError may
/// run before `main` is entered.
let semverSelfCheck () =
    if not (SemVer.lt "0.1.9-preview.1" "0.1.10-preview.1") then
        raise (GuardError "comparator regressed: 0.1.9-preview.1 must be < 0.1.10-preview.1")
    if not (SemVer.lt "0.1.51-preview.1" "0.1.51-preview.2") then
        raise (GuardError "comparator regressed: …-preview.1 must be < …-preview.2")

/// A tag's suffix is a VERSION only if it has a numeric core. `git tag --list 'v*'` also matches
/// `vnext`, `validate`, `v2-wip`; feeding those to `SemVer.parse` raises out of `List.sortWith`
/// (an unhandled comparer exception, not a named rule), and a numeric stray like `v9.9` invents a
/// `pkg-lags-release-tag` against a tag that was never a release. Nothing forbids `v`-prefixed tags,
/// so filter by SHAPE rather than trusting the glob. Load-bearing now that `ReleaseTagCut` reads it.
let private versionShaped (s: string) = Regex.IsMatch(s, @"^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.\-]+)?$")

// ---- failure shape + verdict (data-model §8) --------------------------------------------------
type Failure =
    { Rule: string
      Location: string
      Expected: string
      Actual: string
      Fix: string }

let private lineOf (text: string) (needle: string) =
    let lines = text.Replace("\r\n", "\n").Split('\n')
    lines
    |> Array.tryFindIndex (fun l -> l.Contains needle)
    |> Option.map ((+) 1)
    |> Option.defaultValue 0

// ---- input locations (constants: nothing here can fail) ---------------------------------------
let propsRel = "template/base/Directory.Packages.props"
let nuspecRel = "src/Meta/FS.GG.UI.nuspec"
let buildFsxRel = "template/base/build.fsx"
let templateFsprojRel = ".template.package/FS.GG.UI.Template.fsproj"

// The documented consumed manifest (data-model §5, surface-map T004) — 12 product-facing members.
// Feature 240 (#73): FS.GG.UI.Canvas is consumed on the game/sample-pack profiles (FixedStep + Rng).
let templateExpected =
    Set.ofList
        [ "FS.GG.UI.Build"; "FS.GG.UI.Scene"; "FS.GG.UI.Canvas"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Elmish"
          "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Controls.Elmish"
          "FS.GG.UI.DesignSystem"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Testing" ]

/// Versions carried by tags matching `glob` whose ref starts with `prefix` (the prefix stripped).
/// Fails closed if git errors — never green-by-absence.
let tagVersionsOf (glob: string) (prefix: string) =
    let ec, out = run repoRoot "git" [ "tag"; "--list"; glob ]
    if ec <> 0 then raise (GuardError(sprintf "git tag --list %s failed" glob))
    out.Replace("\r\n", "\n").Split('\n')
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (fun s -> s.StartsWith(prefix, StringComparison.Ordinal))
    |> Array.map (fun s -> s.Substring(prefix.Length))
    |> Array.filter versionShaped
    |> Array.toList

/// Everything the rules read, derived once from the repo + the pushed tags.
type Inputs =
    { PropsText: string
      Occurrences: int
      PinVersion: string
      PropsLoc: string
      TagVersions: string list
      LatestTag: string
      PublishedMembers: Set<string>
      BomDeps: (string * string) list
      BomIds: Set<string>
      TemplatePins: (string * string) list
      TemplateIds: Set<string>
      RuntimeRegexResolves: bool
      PkgVersion: string
      PkgVersionLoc: string
      ReleaseTagVersions: string list
      TemplateTagVersions: string list
      LatestReleaseTag: string
      LatestTemplateTag: string
      PinBumpedHere: bool
      PkgBumpedHere: bool
      TemplateTagCut: bool
      ReleaseTagCut: bool
      PinPending: bool
      TemplateTagPending: bool
      ReleaseTagPending: bool }

// ---- pure input readers (T009) — each fails closed on unreadable input ------------------------
//
// A FUNCTION, called from `main` — not a run of module-level `let`s. F# evaluates module-level bindings
// when the script's startup class is initialized, which is BEFORE `main` is entered and therefore
// outside the `try/with` at the bottom that maps GuardError to the contract's exit 2. Every raise below
// used to escape as an unhandled exception, which `dotnet fsi` reports as exit 1 — the code this
// guard's contract reserves for DRIFT. "The guard could not read its inputs" and "the repo is
// incoherent" were then the same observable, so the fail-closed exit named the wrong cause. Reading the
// inputs inside `main` is what makes exit 2 reachable at all.
let readInputs () : Inputs =
    // SingleVersionSource
    let propsText = readFile (repo propsRel)
    let fsGgUiMatches = Regex.Matches(propsText, "<FsGgUiVersion>([^<]*)</FsGgUiVersion>")
    let occurrences = fsGgUiMatches.Count
    let pinVersion =
        if occurrences >= 1 then fsGgUiMatches.[0].Groups.[1].Value.Trim()
        else raise (GuardError(sprintf "<FsGgUiVersion> not found in %s — single source of version truth missing" propsRel))
    let propsLoc = sprintf "%s:%d <FsGgUiVersion>" propsRel (lineOf propsText "<FsGgUiVersion>")

    // CoherentSnapshotTag set (fail closed if tags are unfetched — never green-by-absence)
    let tagVersions = tagVersionsOf "fs-gg-ui/v*" "fs-gg-ui/v"
    if tagVersions.IsEmpty then
        raise (GuardError "no fs-gg-ui/v* tags visible — CI must fetch tags (fetch-depth: 0 / fetch-tags); fail closed rather than green-by-absence")
    let latestTag = tagVersions |> List.sortWith (fun a b -> SemVer.cmp (SemVer.parse a) (SemVer.parse b)) |> List.last

    // PublishedMemberSet P — packable FS.GG.UI.* under src/** (reuses validate-bom-consumer discovery)
    let publishedMembers =
        Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
        |> Array.choose (fun proj ->
            let t = File.ReadAllText proj
            let m name = Regex.Match(t, sprintf "<%s>([^<]*)</%s>" name name)
            let pid = let g = m "PackageId" in if g.Success then g.Groups.[1].Value.Trim() else ""
            let packable = let g = m "IsPackable" in g.Success && g.Groups.[1].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            if packable && pid.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then Some pid else None)
        |> Set.ofArray

    // BomDependencySet B
    let bomDeps =
        let text = readFile (repo nuspecRel)
        Regex.Matches(text, "<dependency\\s+id=\"([^\"]+)\"\\s+version=\"([^\"]+)\"")
        |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
        |> Seq.toList

    // TemplateConsumedPinSet T (id + its Version attribute, in file order)
    let templatePins =
        Regex.Matches(propsText, "<PackageVersion\\s+Include=\"(FS\\.GG\\.UI\\.[^\"]+)\"\\s+Version=\"([^\"]+)\"")
        |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
        |> Seq.toList

    // RuntimeResolution (build.fsx:60 regex still matches the literal in the current tree)
    let runtimeRegexResolves =
        let buildText = readFile (repo buildFsxRel)
        // build.fsx applies this exact regex to Directory.Packages.props at runtime.
        let m = Regex.Match(buildText, "<FsGgUiVersion>\\(\\[\\^<\\]\\+\\)</FsGgUiVersion>")
        let pattern = "<FsGgUiVersion>([^<]+)</FsGgUiVersion>"
        m.Success && Regex.IsMatch(propsText, pattern)

    // ---- release lane (P5 / #48) --------------------------------------------------------------
    // The FRAMEWORK set (FS.GG.UI.*, versioned by <FsGgUiVersion> above, snapshotted by fs-gg-ui/v*) is
    // DECOUPLED from the TEMPLATE PACKAGE (FS.GG.UI.Template, versioned by <Version> in .template.package,
    // snapshotted by the v* release trigger + fs-gg-ui-template/v* tags). The pin MAY lag the template
    // package (a template-only content release advances the package over an unchanged framework pin); it
    // must never LEAD it. Validate that release lane too, env-free, fail-closed, from repo + pushed tags.
    let templateFsprojText = readFile (repo templateFsprojRel)
    let pkgVersionMatches = Regex.Matches(templateFsprojText, "<Version>([^<]*)</Version>")
    let pkgVersion =
        if pkgVersionMatches.Count = 1 then pkgVersionMatches.[0].Groups.[1].Value.Trim()
        elif pkgVersionMatches.Count = 0 then raise (GuardError(sprintf "<Version> not found in %s — template-package version source missing" templateFsprojRel))
        else raise (GuardError(sprintf "<Version> appears %d times in %s — expected exactly one template-package version source" pkgVersionMatches.Count templateFsprojRel))
    let pkgVersionLoc = sprintf "%s:%d <Version>" templateFsprojRel (lineOf templateFsprojText "<Version>")

    // `v*` matches only the release trigger tags (fs-gg-ui/v* and fs-gg-ui-template/v* do not start "v").
    let releaseTagVersions = tagVersionsOf "v*" "v"
    let templateTagVersions = tagVersionsOf "fs-gg-ui-template/v*" "fs-gg-ui-template/v"
    if releaseTagVersions.IsEmpty then
        raise (GuardError "no v* release tags visible — CI must fetch tags (fetch-depth: 0 / fetch-tags); fail closed rather than green-by-absence")
    if templateTagVersions.IsEmpty then
        raise (GuardError "no fs-gg-ui-template/v* tags visible — CI must fetch tags; fail closed rather than green-by-absence")
    let latestReleaseTag = releaseTagVersions |> List.sortWith (fun a b -> SemVer.cmp (SemVer.parse a) (SemVer.parse b)) |> List.last
    let latestTemplateTag = templateTagVersions |> List.sortWith (fun a b -> SemVer.cmp (SemVer.parse a) (SemVer.parse b)) |> List.last

    // The framework pin / template package were bumped by THIS change ⇒ their tags are cut next, not now.
    let pinBumpedHere = bumpedInCommitUnderTest propsRel "FsGgUiVersion"
    let pkgBumpedHere = bumpedInCommitUnderTest templateFsprojRel "Version"

    // The three tags of a release have a MANDATED PUSH ORDER — only the last one triggers release.yml:
    //
    //     fs-gg-ui/v<pin>  →  fs-gg-ui-template/v<pkg>  →  v<pkg>
    //
    // so `v<pkg>` existing means the release is UNDER WAY, not pending: every tag that must precede it
    // is due NOW. This is what bounds the RELEASE-PENDING waiver.
    //
    // Without that bound the waiver leaks into `release.yml`. That workflow triggers on `push: tags:
    // ['v*']` and runs the Package.Tests coherence mirror at the TAG COMMIT — which IS the commit that
    // bumped <Version>, so `pkgBumpedHere` is true there and `pkg-no-template-tag` was waived. Pushing
    // `v*` before `fs-gg-ui-template/v*` then went green, `publish-packages` (needs: package-tests)
    // shipped the coherent set, and template-dispatch.yml — which triggers ONLY on
    // `fs-gg-ui-template/v*` — never fired, so FS.GG.Templates never got its pin-bump PR. Published,
    // unannounced: the exact half-executed publish-before-flip class of FS-GG/.github#250 that the
    // waiver was introduced to stop hiding. The waiver's premise ("the tag cannot exist yet — it points
    // at THIS commit") is simply false once a later tag in the order already exists.
    //
    // "Has tag T been cut for version V?" — a tag is a successor only WITHIN ITS OWN RELEASE, so each
    // rule asks about the version IT is keyed on. `fs-gg-ui-template/v*` and `v*` both carry the template
    // package's version; a FRAMEWORK release bumps pin and package together (`pin-leads-package` forbids
    // pin > pkg), so on the only change where a `fs-gg-ui/v<pin>` snapshot is pending, `pin = pkg` and the
    // pin's successors carry `pinVersion`. Keying the pin's bound on `pkgVersion` instead would count the
    // PREVIOUS release's tags as successors of a new snapshot — a false red on any pin-only bump.
    let templateTagCutFor v = List.contains v templateTagVersions
    let releaseTagCutFor v = List.contains v releaseTagVersions

    // A bump waives its own missing tag only while NO SUCCESSOR tag in the push order has been cut.
    // Each tag's successors are exactly the tags to its right in the order above:
    //
    //     fs-gg-ui/v<pin>          successors: fs-gg-ui-template/v<pkg>, v<pkg>
    //     fs-gg-ui-template/v<pkg> successors: v<pkg>
    //     v<pkg>                   successors: none — it lands last, so its waiver needs no bound
    //                                          (and `pkg-no-release-tag` is only reached when it is absent)
    //
    // Bounding the pin by `ReleaseTagCut` alone would check only its FURTHEST successor: pushing
    // `fs-gg-ui-template/v<pkg>` before `fs-gg-ui/v<pin>` would still waive `pin-no-tag` and go green,
    // while template-dispatch.yml (which fires on `fs-gg-ui-template/v*`) has already told FS.GG.Templates
    // to bump its pin to a framework snapshot that was never cut and never published. That is the same
    // half-executed release as FS-GG/.github#250, mirrored: announce-before-publish.
    //
    // Note what this does NOT do: it never asks "did <Version> bump here too?". Asking about the pin's OWN
    // successor tags makes that question unnecessary — and it keeps a pin-only bump (pin raised to a new
    // framework snapshot below the released package version, which `pin-leads-package` permits) a legal
    // pending state, as it was before this bound existed. Ask about tags, not bumps.
    //
    // `releaseLane` kills all three waivers outright: successor-tag bounds can only see mis-orderings that
    // LEAVE A TAG BEHIND, and a publish need not leave one.
    //
    // Known limitation, unchanged by this bound: `bumpedInCommitUnderTest` reads `HEAD~1..HEAD`, so a
    // release split across two commits on `main` (rebase-merge) is seen as two unrelated changes, and the
    // package lane reds at the second. Squash-merge and merge-commit both keep the whole release in one
    // diff. This is why the merge method is load-bearing; see gate.yml's header.
    let templateTagCut = templateTagCutFor pkgVersion
    let releaseTagCut = releaseTagCutFor pkgVersion

    { PropsText = propsText
      Occurrences = occurrences
      PinVersion = pinVersion
      PropsLoc = propsLoc
      TagVersions = tagVersions
      LatestTag = latestTag
      PublishedMembers = publishedMembers
      BomDeps = bomDeps
      BomIds = bomDeps |> List.map fst |> Set.ofList
      TemplatePins = templatePins
      TemplateIds = templatePins |> List.map fst |> Set.ofList
      RuntimeRegexResolves = runtimeRegexResolves
      PkgVersion = pkgVersion
      PkgVersionLoc = pkgVersionLoc
      ReleaseTagVersions = releaseTagVersions
      TemplateTagVersions = templateTagVersions
      LatestReleaseTag = latestReleaseTag
      LatestTemplateTag = latestTemplateTag
      PinBumpedHere = pinBumpedHere
      PkgBumpedHere = pkgBumpedHere
      TemplateTagCut = templateTagCut
      ReleaseTagCut = releaseTagCut
      PinPending = not releaseLane && pinBumpedHere && not (templateTagCutFor pinVersion) && not (releaseTagCutFor pinVersion)
      // `v<pkg>` is the only successor of `fs-gg-ui-template/v<pkg>`, so once it exists the template-scoped
      // tag is overdue, not pending. `v<pkg>` itself lands last and has no successor to bound it — hence
      // `ReleaseTagPending` needs only the bump (and its rule is reached only when `v<pkg>` is absent).
      TemplateTagPending = not releaseLane && pkgBumpedHere && not releaseTagCut
      ReleaseTagPending = not releaseLane && pkgBumpedHere }

// ---- rules ------------------------------------------------------------------------------------

// US1 — pin must resolve to a published snapshot tag and must not lag the latest (FR-001/002/009).
// `pin-no-tag` fires when the pin is untagged and the snapshot tag is not PENDING — i.e. a tag that was
// never cut, rather than one this change made due (see `PinPending`).
let us1Failures (i: Inputs) : Failure list =
    if SemVer.lt i.PinVersion i.LatestTag then
        [ { Rule = "pin-lags-tag"
            Location = i.PropsLoc
            Expected = sprintf ">= %s (latest fs-gg-ui/v* tag)" i.LatestTag
            Actual = i.PinVersion
            Fix = sprintf "bump <FsGgUiVersion> to %s (the latest coherent snapshot), or cut a newer fs-gg-ui/v* tag" i.LatestTag } ]
    elif not (List.contains i.PinVersion i.TagVersions) && not i.PinPending then
        [ { Rule = "pin-no-tag"
            Location = i.PropsLoc
            Expected = sprintf "a tag fs-gg-ui/v%s" i.PinVersion
            Actual =
                if releaseLane then "none — and this is the release lane, where every tag is due; nothing is pending at publish time"
                elif not i.PinBumpedHere then "none — and this change did not bump the pin, so no tag is pending"
                elif List.contains i.PinVersion i.ReleaseTagVersions then sprintf "none — and v%s is already cut, so this tag was due BEFORE it (push order)" i.PinVersion
                else sprintf "none — and fs-gg-ui-template/v%s is already cut, so this tag was due BEFORE it (push order)" i.PinVersion
            Fix = sprintf "cut & push the fs-gg-ui/v%s snapshot tag (and feed) — it precedes fs-gg-ui-template/v* and v* — or correct <FsGgUiVersion> to a published version" i.PinVersion } ]
    else []

// US2 — a half-bump cannot ship, independent of any warnings-as-errors policy (FR-003/004/005)
let bomTokenFailures (i: Inputs) : Failure list =
    i.BomDeps
    |> List.collect (fun (id, v) ->
        let notToken = v <> "[$version$]"
        let notExact = not (v.StartsWith "[" && v.EndsWith "]" && not (v.Contains ","))
        [ if notToken then
              { Rule = "bom-pin-not-token"
                Location = sprintf "%s %s" nuspecRel id
                Expected = "[$version$]"
                Actual = v
                Fix = sprintf "restore %s's version to the single token [$version$]" id }
          if notExact then
              { Rule = "bom-exact-bracket"
                Location = sprintf "%s %s" nuspecRel id
                Expected = "an exact [..] bracket with no comma"
                Actual = v
                Fix = sprintf "pin %s with an exact bracket so any deviation fails loudly" id } ])

let bomMemberSkewFailures (i: Inputs) : Failure list =
    [ for missing in Set.difference i.PublishedMembers i.BomIds ->
        { Rule = "bom-member-skew"
          Location = nuspecRel
          Expected = sprintf "a <dependency> for every packable FS.GG.UI.* member (%d)" i.PublishedMembers.Count
          Actual = sprintf "missing %s" missing
          Fix = sprintf "add <dependency id=\"%s\" version=\"[$version$]\" /> to the BOM" missing }
      for extra in Set.difference i.BomIds i.PublishedMembers ->
        { Rule = "bom-member-skew"
          Location = nuspecRel
          Expected = sprintf "only packable FS.GG.UI.* members (%d)" i.PublishedMembers.Count
          Actual = sprintf "extra %s (no packable src/** member)" extra
          Fix = sprintf "remove %s from the BOM, or add the packable src/** member" extra } ]

let templateFailures (i: Inputs) : Failure list =
    [ // every consumed pin derives through $(FsGgUiVersion) — no hardcoded literal
      for (id, v) in i.TemplatePins do
          if v <> "$(FsGgUiVersion)" then
              yield
                  { Rule = "template-pin-hardcoded"
                    Location = sprintf "%s %s" propsRel id
                    Expected = "$(FsGgUiVersion)"
                    Actual = v
                    Fix = sprintf "route %s's Version through $(FsGgUiVersion) (the single source)" id }
      // consumed set ⊆ published, and == the documented 11-member manifest
      for extra in Set.difference i.TemplateIds i.PublishedMembers ->
          { Rule = "template-consumed-skew"
            Location = propsRel
            Expected = "every consumed pin is a packable FS.GG.UI.* member"
            Actual = sprintf "%s is not in the published set" extra
            Fix = sprintf "remove %s from the template, or publish it as a packable member" extra }
      for missing in Set.difference templateExpected i.TemplateIds ->
          { Rule = "template-consumed-skew"
            Location = propsRel
            Expected = "the documented 11-member consumed manifest"
            Actual = sprintf "missing %s" missing
            Fix = sprintf "restore the consumed pin %s" missing }
      for extra in Set.difference i.TemplateIds templateExpected ->
          { Rule = "template-consumed-skew"
            Location = propsRel
            Expected = "the documented 11-member consumed manifest"
            Actual = sprintf "unexpected consumed pin %s" extra
            Fix = sprintf "drop %s, or update the documented consumed manifest in surface-map.md" extra } ]

let invariantFailures (i: Inputs) : Failure list =
    [ if i.Occurrences <> 1 then
          { Rule = "single-source-not-unique"
            Location = i.PropsLoc
            Expected = "exactly 1 <FsGgUiVersion> literal"
            Actual = string i.Occurrences
            Fix = "collapse to a single <FsGgUiVersion> literal (the one source of truth)" }
      if not i.RuntimeRegexResolves then
          { Rule = "runtime-regex-broken"
            Location = sprintf "%s:60" buildFsxRel
            Expected = "build.fsx's <FsGgUiVersion>([^<]+)</FsGgUiVersion> regex matches the literal"
            Actual = "no match (renamed/half-renamed property breaks runtime engine resolution)"
            Fix = "keep the <FsGgUiVersion> element name in lockstep with build.fsx's regex" } ]

// P5 (#48) — the template-package RELEASE lane vs the framework pin. The package must not LAG the
// latest v* / fs-gg-ui-template/v* tag, must not be left UNTAGGED by a release that was never cut,
// and the framework pin must not LEAD it (pin <= package version).
//
// Three states, not two (the distinction this guard originally missed):
//
//   LAGS     pkg <  latest tag                      → always a defect: main names a stale package.
//   RELEASED pkg has its tag                        → the steady state.
//   PENDING  pkg >  latest tag, no tag yet          → the bump landed; the tag is cut next.
//
// PENDING is LEGAL on the change that performs the bump and a DEFECT anywhere else. Demanding the tag
// on the bump itself is unsatisfiable — a tag can only point at a commit that already exists — which
// is why this rule went red on every release PR (#155, #159, #163) and was merged past each time
// (see specs/252-retire-canvas-audio/spec.md, which records the reds as "expected"). On `main` it was
// worse than useless: it raced the tag push and passed or failed on runner scheduling. A gate that is
// red whenever it matters teaches people to ignore it, which is how an unrelated half-executed
// publish-before-flip (FS-GG/.github#250) sat unnoticed for a day.
//
// So: keep the rule, and let it fire only when PENDING is NOT explained by a bump in this very change,
// bounded by the SUCCESSOR tags in the push order (see `readInputs` — without which the waiver
// green-lights a mis-ordered release), and disabled outright in the `releaseLane` (where every tag is
// due and nothing can be pending).
// A release bump goes green on the PR and on the merge commit; if the tag is never cut, the very
// next commit to `main` turns it red and names the tags to cut. No race, no accepted red.
//
// Emitted in PUSH ORDER: `printDrift` and the $GITHUB_STEP_SUMMARY block iterate this list top-to-bottom,
// and on a stale release these ARE the operator's tag-cutting instructions. Listing `pkg-no-release-tag`
// (push `v*`) before `pkg-no-template-tag` would have them push the trigger tag first — which the
// push-order bound above then reds inside release.yml, stranding the release mid-flight behind a
// force-deleted tag. The output that names these tags must name them in the order they are pushed.
let releaseLaneFailures (i: Inputs) : Failure list =
    [ if SemVer.lt i.PkgVersion i.LatestTemplateTag then
          { Rule = "pkg-lags-template-tag"
            Location = i.PkgVersionLoc
            Expected = sprintf ">= %s (latest fs-gg-ui-template/v* tag)" i.LatestTemplateTag
            Actual = i.PkgVersion
            Fix = sprintf "bump <Version> to %s (the latest template coherent-set snapshot)" i.LatestTemplateTag }
      elif not (List.contains i.PkgVersion i.TemplateTagVersions) && not i.TemplateTagPending then
          { Rule = "pkg-no-template-tag"
            Location = i.PkgVersionLoc
            Expected = sprintf "a template-scoped tag fs-gg-ui-template/v%s" i.PkgVersion
            Actual =
                if releaseLane then "none — and this is the release lane, where every tag is due; nothing is pending at publish time"
                elif i.ReleaseTagCut then
                    sprintf "none — and v%s is already cut, so this tag was due BEFORE it (push order); template-dispatch.yml never fired" i.PkgVersion
                else "none — and this change did not bump <Version>, so no tag is pending"
            Fix = sprintf "cut & push fs-gg-ui-template/v%s (the template coherent-set snapshot) BEFORE v%s" i.PkgVersion i.PkgVersion }
      if SemVer.lt i.PkgVersion i.LatestReleaseTag then
          { Rule = "pkg-lags-release-tag"
            Location = i.PkgVersionLoc
            Expected = sprintf ">= %s (latest v* release tag)" i.LatestReleaseTag
            Actual = i.PkgVersion
            Fix = sprintf "bump <Version> to %s (the latest released template package)" i.LatestReleaseTag }
      elif not (List.contains i.PkgVersion i.ReleaseTagVersions) && not i.ReleaseTagPending then
          { Rule = "pkg-no-release-tag"
            Location = i.PkgVersionLoc
            Expected = sprintf "a release trigger tag v%s" i.PkgVersion
            Actual =
                if releaseLane then "none — and this is the release lane; a publish must be triggered by its own v* tag"
                else "none — and this change did not bump <Version>, so no tag is pending"
            Fix = sprintf "cut & push the v%s release tag LAST (it triggers release.yml), or correct <Version> to a released version" i.PkgVersion }
      if SemVer.lt i.PkgVersion i.PinVersion then
          { Rule = "pin-leads-package"
            Location = i.PropsLoc
            Expected = sprintf "<= the released template package version %s" i.PkgVersion
            Actual = sprintf "framework pin %s" i.PinVersion
            Fix = sprintf "a framework bump requires a template release at >= the pin — cut the template package + tags at %s or higher, or lower the pin" i.PinVersion } ]

let structuralFailures (i: Inputs) =
    us1Failures i @ bomTokenFailures i @ bomMemberSkewFailures i @ templateFailures i @ invariantFailures i
    @ releaseLaneFailures i

// ---- restore-grounded proof (live, US3/T027) --------------------------------------------------
type LiveResult =
    { V: string
      MembersResolved: int
      AtV: int
      Partial: Failure list
      CleanBuild: bool }

let liveProof (i: Inputs) : LiveResult =
    let v = i.PinVersion
    if String.IsNullOrWhiteSpace v then raise (GuardError "pinned version is undefined — cannot run restore proof")
    let tmp = Path.Combine(Path.GetTempPath(), "vcoh209-" + Guid.NewGuid().ToString("N").Substring(0, 8))
    let feed = Path.Combine(tmp, "feed")
    let gpf = Path.Combine(tmp, "gpf")
    Directory.CreateDirectory feed |> ignore

    // pack the coherent snapshot (16 members + BOM) from source at the pinned V
    let pc, po = run repoRoot "dotnet" [ "pack"; "FS.GG.Rendering.slnx"; "-c"; "Release"; sprintf "-p:Version=%s" v; "-o"; feed ]
    if pc <> 0 then raise (GuardError(sprintf "pack-from-source at %s failed:\n%s" v po))

    // clean consumer: ONLY FS.GG.UI@V
    let cdir = Path.Combine(tmp, "consumer")
    Directory.CreateDirectory cdir |> ignore
    let nugetConfig =
        sprintf
            "<configuration><config><add key=\"globalPackagesFolder\" value=\"%s\" /></config><packageSources><clear /><add key=\"local\" value=\"%s\" /><add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" /></packageSources><packageSourceMapping><packageSource key=\"local\"><package pattern=\"FS.GG.UI*\" /></packageSource><packageSource key=\"nuget.org\"><package pattern=\"*\" /></packageSource></packageSourceMapping></configuration>"
            gpf feed
    File.WriteAllText(Path.Combine(cdir, "nuget.config"), nugetConfig)
    File.WriteAllText(Path.Combine(cdir, "Library.fs"), "module Consumer.Library")
    File.WriteAllText(
        Path.Combine(cdir, "Consumer.fsproj"),
        sprintf
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup><ItemGroup><PackageReference Include=\"FS.GG.UI\" Version=\"%s\" /></ItemGroup><ItemGroup><Compile Include=\"Library.fs\" /></ItemGroup></Project>"
            v)

    let rc, ro = run cdir "dotnet" [ "restore"; "Consumer.fsproj" ]
    if rc <> 0 then raise (GuardError(sprintf "clean restore of FS.GG.UI@%s failed:\n%s" v ro))
    let _, listOut = run cdir "dotnet" [ "list"; "Consumer.fsproj"; "package"; "--include-transitive" ]
    let resolved =
        Regex.Matches(listOut, "(FS\\.GG\\.UI[A-Za-z.]*)\\s+(?:[0-9][^\\s]*\\s+)?([0-9][0-9A-Za-z.\\-]*)")
        |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
        |> Seq.distinct
        |> Seq.filter (fun (id, _) -> id.StartsWith "FS.GG.UI." && id <> "FS.GG.UI")
        |> Seq.toList
    let bc, _ = run cdir "dotnet" [ "build"; "Consumer.fsproj"; "-c"; "Release"; "--no-restore" ]

    let offV = resolved |> List.filter (fun (_, rv) -> rv <> v)
    let resolvedIds = resolved |> List.map fst |> Set.ofList
    let partialFailures =
        [ for (id, rv) in offV ->
            { Rule = "restore-partial"
              Location = sprintf "FS.GG.UI@%s clean restore" v
              Expected = sprintf "all members @%s" v
              Actual = sprintf "%s @%s" id rv
              Fix = "republish the lagging member(s) at the pinned V so the snapshot is complete" }
          // a member that did not resolve at all is also a partial graph
          for missing in Set.difference i.PublishedMembers resolvedIds ->
            { Rule = "restore-partial"
              Location = sprintf "FS.GG.UI@%s clean restore" v
              Expected = sprintf "all %d members resolve @%s" i.PublishedMembers.Count v
              Actual = sprintf "%s did not resolve" missing
              Fix = sprintf "publish %s@%s to the feed" missing v } ]

    { V = v
      MembersResolved = resolved.Length
      AtV = resolved |> List.filter (fun (_, rv) -> rv = v) |> List.length
      Partial = partialFailures
      CleanBuild = (bc = 0) }

// ---- aggregate verdict + report (T014/T024/T028) ----------------------------------------------
let reportPath = repo "specs/209-version-staleness-guard/readiness/version-coherence.md"

let writeReport (i: Inputs) (provenance: string) (failures: Failure list) (liveOpt: LiveResult option) =
    Directory.CreateDirectory(Path.GetDirectoryName reportPath) |> ignore
    let sb = System.Text.StringBuilder()
    let line (s: string) = sb.AppendLine s |> ignore
    let ok = failures.IsEmpty
    line "# FS.GG.UI version coherence — verdict report"
    line ""
    line "Regenerated by `scripts/validate-version-coherence.fsx`. The merge-blocking gate step"
    line "\"Version coherence guard\" re-derives this verdict on every PR."
    line ""
    line "- feature: 209-version-staleness-guard"
    line (sprintf "- result: %s" (if ok then "pass" else "fail"))
    line (sprintf "- provenance: %s" provenance)
    line (sprintf "- single-version-source: %s (`%s`, occurrences=%d)" i.PinVersion i.PropsLoc i.Occurrences)
    line (sprintf "- latest-snapshot-tag: fs-gg-ui/v%s" i.LatestTag)
    line (sprintf "- published-members: %d · bom-deps: %d · template-consumed-pins: %d" i.PublishedMembers.Count i.BomIds.Count i.TemplateIds.Count)
    line (sprintf "- runtime-regex-resolves: %b" i.RuntimeRegexResolves)
    line (sprintf "- template-package-version: %s (`%s`)" i.PkgVersion i.PkgVersionLoc)
    line (sprintf "- latest-release-tag: v%s · latest-template-tag: fs-gg-ui-template/v%s" i.LatestReleaseTag i.LatestTemplateTag)
    line (sprintf "- framework-pin-vs-package: %s <= %s = %b" i.PinVersion i.PkgVersion (not (SemVer.lt i.PkgVersion i.PinVersion)))
    match liveOpt with
    | Some r ->
        line (sprintf "- resolved-members-at-version: %d/%d at %s" r.AtV r.MembersResolved r.V)
        line (sprintf "- clean-consumer-build: %s" (if r.CleanBuild then "pass" else "fail"))
    | None ->
        line "- resolved-members-at-version: pending-live (run FS_GG_RUN_VERSION_COHERENCE_SMOKE=1)"
    line ""
    if ok then
        line "All lockstep conjuncts hold for the layers that ran."
    else
        line "## Drift — named locations (expected-vs-actual)"
        line ""
        for f in failures do
            line (sprintf "- `DRIFT [%s]` %s — expected `%s`; actual `%s`" f.Rule f.Location f.Expected f.Actual)
    File.WriteAllText(reportPath, sb.ToString())

let printDrift (failures: Failure list) =
    for f in failures do
        eprintfn "DRIFT [%s] %s" f.Rule f.Location
        eprintfn "  expected: %s" f.Expected
        eprintfn "  actual:   %s" f.Actual
        eprintfn "  fix:      %s" f.Fix
    // GitHub step summary (SC-006) — reviewer sees the named location without opening logs.
    match Environment.GetEnvironmentVariable "GITHUB_STEP_SUMMARY" with
    | null | "" -> ()
    | summaryPath ->
        let s = System.Text.StringBuilder()
        s.AppendLine "### Version coherence guard — DRIFT" |> ignore
        s.AppendLine "" |> ignore
        for f in failures do
            s.AppendLine(sprintf "- `DRIFT [%s]` %s — expected `%s`; actual `%s` — fix: %s" f.Rule f.Location f.Expected f.Actual f.Fix) |> ignore
        File.AppendAllText(summaryPath, s.ToString())

/// The tags this change has made due but that do not exist yet (see `bumpedInCommitUnderTest`).
/// `v*` is LAST: only it triggers release.yml, so the snapshot tags must already be pushed when it
/// lands. Mirrors the waiver conditions exactly — a tag is PENDING iff its rule is being waived, so
/// once `v<pkg>` is cut the earlier tags are reported as drift, never as "due next".
let pendingTags (i: Inputs) : string list =
    [ if i.PinPending && not (List.contains i.PinVersion i.TagVersions) then
          sprintf "fs-gg-ui/v%s" i.PinVersion
      if i.TemplateTagPending && not i.TemplateTagCut then
          sprintf "fs-gg-ui-template/v%s" i.PkgVersion
      if i.ReleaseTagPending && not i.ReleaseTagCut then
          sprintf "v%s" i.PkgVersion ]

/// A version bump with no tag yet is a transient, not drift — but it is not silence either. Name the
/// tags, in push order, with a greppable sentinel. This is the state release PRs sit in.
///
/// Printed on EVERY verdict, not only a green one. Suppressing it on red loses the push order exactly
/// when it is most needed — and leaves `printDrift` as the only tag instruction, which enumerates
/// failures, not a procedure. What was wrong with the old line was the WORD "legal", a claim about a
/// verdict this function does not know. It no longer makes one; the exit code does. Note the list is
/// built from the waiver predicates, so a tag whose rule is FIRING never appears here as "due next".
let printReleasePending (tags: string list) =
    if not tags.IsEmpty then
        printfn "RELEASE-PENDING: this change bumps %d version tag(s) that are not cut yet." tags.Length
        printfn "  push these tags at the merge commit, in this order (only v* triggers release.yml):"
        for t in tags do printfn "    git tag %s && git push origin %s" t t
        printfn "  if they are never cut, the next commit to main fails `pkg-no-release-tag`/`pin-no-tag`."
        match Environment.GetEnvironmentVariable "GITHUB_STEP_SUMMARY" with
        | null | "" -> ()
        | summaryPath ->
            let s = System.Text.StringBuilder()
            s.AppendLine "### Version coherence guard — RELEASE-PENDING" |> ignore
            s.AppendLine "" |> ignore
            s.AppendLine "This change bumps a version whose tag is not cut yet. Push at the merge commit, in order:" |> ignore
            s.AppendLine "" |> ignore
            s.AppendLine "```sh" |> ignore
            for t in tags do s.AppendLine(sprintf "git tag %s && git push origin %s" t t) |> ignore
            s.AppendLine "```" |> ignore
            File.AppendAllText(summaryPath, s.ToString())

// ---- main -------------------------------------------------------------------------------------
// RELEASE-PENDING is announced FIRST, on every verdict and before the expensive live proof. It states
// which tags this change made due, in push order; it makes no claim about the verdict, which the exit
// code carries. Printing it before `liveProof` matters: that call can raise GuardError (a pack or
// restore failure — e.g. the known local NU1403), and an operator running the smoke locally to learn
// the push order would otherwise get nothing.
let main () =
    semverSelfCheck ()
    let i = readInputs ()
    printReleasePending (pendingTags i)
    if live then
        let r = liveProof i
        let allFailures = structuralFailures i @ r.Partial
        writeReport i "live" allFailures (Some r)
        if allFailures.IsEmpty then
            printfn "version coherence: COHERENT (structural + live). %d/%d members @%s; wrote %s" r.AtV r.MembersResolved r.V reportPath
            0
        else
            printDrift allFailures
            eprintfn "version coherence: DRIFT — %d failure(s); wrote %s" allFailures.Length reportPath
            1
    else
        let failures = structuralFailures i
        writeReport i "verdict-core" failures None
        if failures.IsEmpty then
            // Say what is true of the PIN. `pendingTags` also carries the two package-lane tags, so on a
            // template-only release (the common shape: pin held, <Version> bumped) keying off the whole
            // list suppressed "pin == latest tag" for a pin that is fully released — a success line that
            // misstates the state is the same class of lie as a red-that-means-ok.
            let pinNote =
                if i.PinPending && not (List.contains i.PinVersion i.TagVersions) then
                    sprintf "pin %s RELEASE-PENDING" i.PinVersion
                else sprintf "pin %s == latest tag" i.PinVersion
            printfn "version coherence: COHERENT (structural verdict-core). %s; wrote %s" pinNote reportPath
            0
        else
            printDrift failures
            eprintfn "version coherence: DRIFT — %d failure(s); wrote %s" failures.Length reportPath
            1

let exitCode =
    try
        main ()
    with
    | GuardError msg ->
        eprintfn "GUARD ERROR (fails closed, exit 2): %s" msg
        2
    | ex ->
        eprintfn "GUARD ERROR (fails closed, exit 2): %s" ex.Message
        2

exit exitCode
