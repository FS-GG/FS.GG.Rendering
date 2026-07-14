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
//   * Feed-grounded proof (FS_GG_RUN_VERSION_COHERENCE_FEED=1, #718): asks the PUBLIC feed whether the
//     packages the CUT TAGS promise are actually there. Everything above reads the tag NAMESPACE, and
//     `pin == latest tag` is what a release that worked and a release that was abandoned BOTH look
//     like — so the guard certified 0.9.1 as COHERENT while nothing had been published and no
//     automation could reach the state (#679). A tag with no package behind it is DRIFT, not
//     coherence. Runs in the NON-REQUIRED `release-publish-gate` job, never in the verdict-core: its
//     subject is the world, not the commit (ADR-0105). See `feedRules` for the four-state table.
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

/// FEED LANE (#718) — the only layer that reads the network. Opt-in, exactly as the restore smoke is:
/// the verdict-core must stay offline and env-free, because it runs inside the REQUIRED `Deterministic
/// gate` and a required check that depends on a feed hands the merge button to that feed's uptime
/// (ADR-0105, cadence-map §4b/§5).
let feedLane = Environment.GetEnvironmentVariable "FS_GG_RUN_VERSION_COHERENCE_FEED" = "1"

/// The flat-container base the feed lane probes. nuget.org is the SUBJECT because it is the feed a
/// consumer actually restores from: `release.yml` dual-publishes to the org feed FIRST and then to
/// nuget.org (ADR-0012 §4, gated ordering), so "present on nuget.org" is the strictly stronger claim —
/// an org push that succeeded while the public push failed is a phantom for every consumer, and only
/// this feed can see it. It is also unauthenticated, so fork PRs get a real answer instead of the
/// no-token `FeedUnavailable` that `apicompat-check.sh` must live with.
///
/// Overridable so the tests can point the lane at a local listener and drive all four states offline —
/// without which the only way to test this layer would be to hit the real network from inside the
/// required gate, which is the very thing the layer exists to keep out of it.
let feedBase =
    match Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_FEED_URL" with
    | null | "" -> "https://api.nuget.org/v3-flatcontainer"
    | url -> url.TrimEnd '/'

/// Raised for any unreadable input / unfetched tags / tooling failure ⇒ exit 2 (fail closed).
exception GuardError of string

/// How long a freshly cut tag is allowed to have no package behind it before that becomes a phantom.
///
/// THE THIRD STATE, and the reason this is not a two-way test. `release-tags.yml` pushes the tag triple
/// and only THEN calls `release.yml`, whose two test jobs + publish take ~25 minutes, after which
/// nuget.org's flat container takes minutes more to index. For that whole window a cut tag legitimately
/// has no package behind it — the publish is IN FLIGHT, not abandoned. This repo merges continuously, so
/// a check without this grace would go red on every PR that lands in the window, on a job whose reds are
/// then worth nothing: "a gate that is red whenever it matters teaches people to ignore it" is written
/// twice already in this file, about this very rule's ancestors (#155/#159/#163, #506).
///
/// The grace only ever DELAYS detection; it can never suppress it. A publish that died leaves the tag
/// there forever, so the next run past the grace reports it — and on this repo the next run is the next
/// merge, which is minutes away.
///
/// A malformed value is a GUARD ERROR, not a silent fall-back to the default: this knob decides whether a
/// missing package is a phantom or a publish in flight, so an operator who set `30m` (rather than `30`)
/// and got 60 minutes anyway would have turned a dial that does nothing, and never be told. `lazy` because
/// it may raise, and nothing that can raise may run before `main` is entered — see `readInputs`.
let publishGraceMinutes =
    lazy
        (match Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_PUBLISH_GRACE_MIN" with
         | null | "" -> 60.0
         | s ->
             match Double.TryParse(s, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture) with
             | true, v when v >= 0.0 && not (Double.IsInfinity v) -> v
             | _ ->
                 raise (
                     GuardError(
                         sprintf
                             "FS_GG_VERSION_COHERENCE_PUBLISH_GRACE_MIN=%A is not a non-negative number of minutes — fail closed rather than silently using the 60-minute default for a knob you meant to turn"
                             s
                     )
                 ))

// ---- shell helper -----------------------------------------------------------------------------
//
// BOTH PIPES ARE DRAINED AT ONCE, and that is not a style choice. Reading stdout to the end and only THEN
// reading stderr deadlocks as soon as a child writes more to stderr than the pipe buffer holds (~64 KB):
// the child blocks writing stderr, so it never exits, so it never closes stdout, so `ReadToEnd` on stdout
// never returns, so stderr is never drained. Nothing times out; the guard just hangs, and a hung guard in
// the REQUIRED gate is a repo that cannot merge.
//
// The `git` calls here are quiet enough never to have reached it. `liveProof` is not — it shells out to
// `dotnet pack` and `dotnet restore`, whose warning streams are exactly the kind of output that fills a
// pipe — and gate.yml runs that smoke on every PR. This was a live hazard, not a hypothetical: the same
// pattern in Package.Tests' subprocess helper hung the suite outright once the feed lane began emitting an
// 18-package DRIFT block on stderr (the child parked in `anon_pipe_write`, every test reporting passed).
let run (workDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    let out = proc.StandardOutput.ReadToEndAsync()
    let err = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()
    proc.ExitCode, out.Result + err.Result

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
// The one RUNNABLE artifact the packaged fs-gg-symbology skill ships. Its `#r "nuget: FS.GG.UI.*"`
// lines must pin FsGgUiVersion so it resolves the library it ships beside, not the latest PUBLISHED
// set (which can predate the recipe and make it throw — #304).
let symbologyRecipeRel = "template/product-skills/fs-gg-symbology/reference.fsx"

// The documented consumed manifest (data-model §5, surface-map T004) — 14 product-facing members.
// Feature 240 (#73): FS.GG.UI.Canvas is consumed on the game/sample-pack profiles (FixedStep + Rng).
// Issue #430: FS.GG.UI.Symbology(.Render) are consumed on the app/sample-pack/game profiles — the
// scaffold shipped the fs-gg-symbology skill and the Symbology api-surface while pinning neither.
let templateExpected =
    Set.ofList
        [ "FS.GG.UI.Build"; "FS.GG.UI.Scene"; "FS.GG.UI.Canvas"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Elmish"
          "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Controls.Elmish"
          "FS.GG.UI.DesignSystem"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Testing"
          "FS.GG.UI.Symbology"; "FS.GG.UI.Symbology.Render" ]

// The FS.GG.UI packages the symbology reference recipe MUST `#r` (#304). A floor so the pin check cannot
// go silently green when the regex matches nothing — a recipe reformatted, renamed, or switched to
// `#load` would otherwise reserve no pins and read as coherent.
let symbologyRecipeExpected =
    Set.ofList
        [ "FS.GG.UI.Scene"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Symbology"; "FS.GG.UI.Symbology.Render" ]

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
      SymbologyRecipePins: (string * string) list
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

    // SymbologyRecipePinSet — each `#r "nuget: FS.GG.UI.<pkg>[, <ver>]"` in the packaged reference recipe,
    // as (id, version) with version "" when the `#r` is unpinned. Read in file order.
    let symbologyRecipePins =
        let text = readFile (repo symbologyRecipeRel)
        Regex.Matches(text, "#r\\s+\"nuget:\\s*(FS\\.GG\\.UI\\.[A-Za-z0-9.]+)\\s*(?:,\\s*([^\"]+))?\"")
        |> Seq.map (fun m -> m.Groups.[1].Value, (if m.Groups.[2].Success then m.Groups.[2].Value.Trim() else ""))
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
      SymbologyRecipePins = symbologyRecipePins
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

// #304 — the packaged symbology reference recipe is the skill's one runnable artifact, and its
// `#r "nuget: FS.GG.UI.*"` lines must resolve the library it ships beside, not "latest published".
// An unpinned `#r` resolves the newest published package, which can PREDATE the recipe: a library
// behaviour change that has merged but not shipped makes the recipe throw against a library that never
// saw it (validate-published-acceptance's #294-lane-2 read this as a publish lag). Hold every FS.GG.UI
// `#r` pin equal to the single FsGgUiVersion source, exactly as the props pins derive through it.
/// The rules, over their INPUTS rather than over `Inputs` — so `rulesSelfCheck` can feed them synthetic
/// pins and prove each one still fires. That is the whole defence against #478: these two rules were
/// dead for four minors, and nothing noticed, because a rule that never fires looks exactly like a repo
/// that never drifts.
let symbologyRecipeRules (pinVersion: string) (pins: (string * string) list) : Failure list =
    [ // Floor: every expected FS.GG.UI `#r` is actually present — the pin check below is vacuous over a
      // recipe the regex found nothing in (reformatted / renamed / switched to `#load`).
      let pinned = pins |> List.map fst |> Set.ofList
      for missing in Set.difference symbologyRecipeExpected pinned ->
          { Rule = "symbology-recipe-missing"
            Location = sprintf "%s %s" symbologyRecipeRel missing
            Expected = sprintf "a pinned `#r \"nuget: %s, %s\"`" missing pinVersion
            Actual = "no matching `#r \"nuget: FS.GG.UI...\"` directive in the recipe"
            Fix = sprintf "restore the `#r \"nuget: %s, %s\"` line (the recipe's one runnable proof needs it)" missing pinVersion }
      // `yield` is NOT optional here, and the compiler said so. Once a list comprehension contains an
      // explicit `->` arm (the floor above), a bare `for … do` whose body is an `if`/`elif` WITHOUT an
      // `else` is statement position: the `Failure` records below were built and then implicitly
      // DISCARDED (`warning FS3221`), so both rules were dead from the day they were written. The floor
      // checked that the `#r` lines EXIST; nothing ever checked what they were pinned TO — and the
      // recipe sat at 0.4.0 against a 0.8.0 framework for four minors while the gate reported green
      // (#478). A guard rule that cannot fail is worse than no rule: it is a green light nobody audits.
      for (id, v) in pins do
        if v = "" then
            yield
                { Rule = "symbology-recipe-unpinned"
                  Location = sprintf "%s %s" symbologyRecipeRel id
                  Expected = sprintf "%s (FsGgUiVersion)" pinVersion
                  Actual = "unpinned #r — resolves the latest PUBLISHED library, which can predate the recipe"
                  Fix = sprintf "pin the #r to `nuget: %s, %s` (your FsGgUiVersion)" id pinVersion }
        elif v <> pinVersion then
            yield
                { Rule = "symbology-recipe-pin-skew"
                  Location = sprintf "%s %s" symbologyRecipeRel id
                  Expected = pinVersion
                  Actual = v
                  Fix = sprintf "repin %s's #r to %s (the single FsGgUiVersion source)" id pinVersion } ]

let symbologyRecipeFailures (i: Inputs) : Failure list =
    symbologyRecipeRules i.PinVersion i.SymbologyRecipePins

/// Self-check the SYMBOLOGY-RECIPE rules the way `semverSelfCheck` self-checks the comparator (T008) —
/// fail closed if any of the three ever stops firing. #478: `symbology-recipe-unpinned` and
/// `symbology-recipe-pin-skew` were silently discarded by the list comprehension and could not fail; the
/// recipe drifted to 0.4.0 under a 0.8.0 framework and the gate stayed green for four minors. The
/// compiler DID say so (`warning FS3221`), and the warning scrolled past in the gate log every run.
///
/// A guard is only evidence if it can distinguish a healthy repo from a sick one, so prove it can: feed
/// each rule an input it MUST reject, and one it must accept. A dead rule now fails the guard itself
/// (exit 2, GUARD ERROR) instead of quietly reporting the repo coherent.
///
/// SCOPE — deliberately these three rules, not all of them: the name says `symbology` because that is
/// what it covers, and a self-check that overstates its reach is the same lie as a rule that cannot
/// fire. The other families (`us1` / `bomToken` / `bomMemberSkew` / `template` / `invariant` /
/// `releaseLane`) build their lists with `->` arms or `[ if … then … ]`, both of which DO yield; a sweep
/// of every `scripts/*.fsx` for the discarded-value warning that killed these two came back clean. Feed
/// a new rule family through here too if you ever add one whose body is a bare `for … do`.
let symbologyRulesSelfCheck () =
    let v = "0.8.0"
    let fired pins = symbologyRecipeRules v pins |> List.map (fun f -> f.Rule) |> Set.ofList
    let expected = Set.toList symbologyRecipeExpected
    let allGood = expected |> List.map (fun id -> id, v)
    let one (id: string) (ver: string) =
        (id, ver) :: (expected |> List.filter ((<>) id) |> List.map (fun x -> x, v))

    // A healthy recipe must produce NO failures — a rule that fires on everything is as useless as one
    // that fires on nothing, and would make the whole guard unfalsifiable.
    if not (fired allGood).IsEmpty then
        raise (GuardError "rule regressed: a correctly-pinned symbology recipe must produce no failures")
    if not ((fired (one "FS.GG.UI.Scene" "")).Contains "symbology-recipe-unpinned") then
        raise (GuardError "rule DEAD: symbology-recipe-unpinned did not fire on an unpinned `#r` (see #478 — check for warning FS3221, an implicitly discarded Failure)")
    if not ((fired (one "FS.GG.UI.Scene" "0.4.0")).Contains "symbology-recipe-pin-skew") then
        raise (GuardError "rule DEAD: symbology-recipe-pin-skew did not fire on a recipe pinned off FsGgUiVersion (see #478 — check for warning FS3221, an implicitly discarded Failure)")
    if not ((fired []).Contains "symbology-recipe-missing") then
        raise (GuardError "rule DEAD: symbology-recipe-missing did not fire on a recipe with no FS.GG.UI `#r` at all")

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
    @ symbologyRecipeFailures i @ releaseLaneFailures i

// ---- feed-grounded proof (#718, epic #693) ----------------------------------------------------
//
// THE HOLE. Every rule above reads the TAG NAMESPACE, and `pin == latest tag` is EXACTLY what a release
// that worked looks like — and exactly what a release that was tagged and never published looks like.
// So this guard printed
//
//     version coherence: COHERENT (structural verdict-core). pin 0.9.1 == latest tag
//
// over a repo whose 0.9.1 packages did not exist. And the expensive part is what follows from it: a
// COHERENT verdict emits no RELEASE-PENDING block, so `release-tags.yml` cut nothing, so `release.yml`
// was never called, so 0.9.1 was never published — EVER. The repo's own guard certified a release that
// does not exist, no automation could reach the state, and a human unwedged it by hand (#679).
//
// The missing question is not about the tags. It is: DOES THE PACKAGE THE TAG PROMISES ACTUALLY EXIST?
//
// WHY THIS MAY NEVER BE A REQUIRED CHECK (ADR-0105). Its verdict is a function of the FEED and the TAG
// NAMESPACE — two things no commit in this repo owns. Apply the ADR's one-sentence test: *could this
// gate turn an already-green commit red without anyone changing this repository?* Yes — trivially, by a
// yank, or by a release dying. So it may not be required, and that is not a limitation to route around:
// requiring it would mean a genuine phantom release WEDGES EVERY MERGE IN THE REPO, which is #515's
// disease (a red `main` nobody can fix by merging) bolted onto #679's. This layer must REPORT the wedge,
// never amplify it. It lives in the non-required `release-publish-gate` job; the verdict-core stays
// offline and env-free.
//
// Note the difference from `api-compatibility-gate`, which reads a feed and IS required: there the feed
// is consulted only to FIND A BASELINE, so its silence cannot redden a green commit. Here the feed is
// half the SUBJECT. That is the same line `template-payload-restore-gate` is drawn on (cadence-map §4b).

/// What the feed said about one (id, version) that a cut tag promises.
type FeedProbe =
    /// The feed answered, and the package is there. The release behind the tag is real.
    | Present
    /// The feed ANSWERED, and this version is not on it. A tag with nothing behind it — the #679 phantom.
    | Absent
    /// The feed did not answer (transport error, 5xx, timeout). Says NOTHING about this repo.
    | Unavailable of string

type FeedObservation =
    { Id: string
      Version: string
      /// The tag that promises this package — named in the failure, because it is the thing a human must
      /// either honour (re-run the release) or retract (delete the tag).
      Tag: string
      /// Minutes since that tag was created. A publish is not instantaneous, so this is load-bearing.
      TagAgeMin: float
      Probe: FeedProbe }

/// The observations, partitioned. ONE partition, computed once, and everything downstream — the rule, the
/// log, the success line — reads it. They used to each re-derive their own, which is how the log and the
/// verdict drift apart, and those two are precisely what a human reads to decide whether a green exit
/// means "checked" or "could not check".
type FeedTally =
    { Published: FeedObservation list
      /// Absent, and the tag is older than the grace. THE #679 PHANTOM — the only state that is drift.
      Phantom: FeedObservation list
      /// Absent, but the tag is younger than the grace: the publish it triggered is still running.
      InFlight: FeedObservation list
      /// The feed did not answer. Says nothing about this repo, so it is never drift — and never a pass.
      Unreachable: (FeedObservation * string) list }

/// THE FOUR STATES. Pure, so `feedRulesSelfCheck` can prove every arm still fires without touching the
/// network — which is not ceremony: this entire layer exists because a guard reported green over a wedged
/// repo, and #478 is this repo's own precedent for a rule that silently could not fire while the thing it
/// watched drifted for four minors.
///
///   | the feed says          | the tag is           | what that means          | verdict            |
///   |------------------------|----------------------|--------------------------|--------------------|
///   | the package is there   | any age              | a real release           | coherent           |
///   | the package is ABSENT  | older than the grace | the #679 PHANTOM         | DRIFT — exit 1     |
///   | the package is ABSENT  | inside the grace     | the publish is in flight | coherent (notice)  |
///   | nothing (5xx/garbage)  | any age              | nothing about this repo  | exit 0, ::error::  |
///
/// The last row is not a courtesy. The feed is read to CHECK a claim this repo makes, and its silence
/// cannot make the repo incoherent — that is `apicompat-check.sh`'s `FeedUnavailable` split and ADR-0105's
/// option (2), and it is the only thing standing between a nuget.org outage and a gate announcing that
/// every release in this repo is a phantom.
let tallyFeed (graceMin: float) (obs: FeedObservation list) : FeedTally =
    { Published = obs |> List.filter (fun o -> o.Probe = Present)
      Phantom = obs |> List.filter (fun o -> o.Probe = Absent && o.TagAgeMin >= graceMin)
      InFlight = obs |> List.filter (fun o -> o.Probe = Absent && o.TagAgeMin < graceMin)
      Unreachable =
        obs
        |> List.choose (fun o ->
            match o.Probe with
            | Unavailable why -> Some(o, why)
            | _ -> None) }

let feedRules (graceMin: float) (t: FeedTally) : Failure list =
    t.Phantom
    |> List.map (fun o ->
        { Rule = "release-phantom"
          Location = sprintf "tag %s" o.Tag
          Expected = sprintf "%s %s on the feed — the tag promises a published release" o.Id o.Version
          Actual =
            sprintf
                "the feed ANSWERED, and %s %s is not on it (tag cut %.0f min ago; grace is %.0f min)"
                o.Id
                o.Version
                o.TagAgeMin
                graceMin
          Fix =
            sprintf
                "the release behind %s never landed. Re-run it (`gh workflow run release.yml -f version=%s`), or DELETE the tag if it was abandoned. Leaving it is the worst option: the verdict-core reads `pin == latest tag` and certifies a release that does not exist (#679)."
                o.Tag
                o.Version })

/// Prove each arm of the table still fires (the `symbologyRulesSelfCheck` pattern, and the same #478
/// lesson: a rule that cannot fail is a green light nobody audits). A dead rule fails the GUARD — exit 2,
/// GUARD ERROR — instead of quietly reporting the repo coherent, which is the exact failure this whole
/// layer was written to end.
let feedRulesSelfCheck () =
    let obs probe age =
        { Id = "FS.GG.UI.Scene"
          Version = "0.9.1"
          Tag = "fs-gg-ui/v0.9.1"
          TagAgeMin = age
          Probe = probe }
    let fired o = feedRules 60.0 (tallyFeed 60.0 [ o ]) |> List.map (fun f -> f.Rule) |> Set.ofList

    if not ((fired (obs Absent 1440.0)).Contains "release-phantom") then
        raise (GuardError "rule DEAD: release-phantom did not fire on a day-old tag with NO package behind it — that is #679 exactly, and a guard that cannot see it certifies a release that does not exist")
    if not (fired (obs Present 1440.0)).IsEmpty then
        raise (GuardError "rule regressed: a package that IS on the feed must not be a phantom — a rule that fires on everything is as useless as one that fires on nothing")
    if not (fired (obs Absent 1.0)).IsEmpty then
        raise (GuardError "rule regressed: a tag cut a minute ago has its publish IN FLIGHT, not abandoned — reporting it is the false red that teaches people to ignore the gate")
    if not (fired (obs (Unavailable "HTTP 503") 1440.0)).IsEmpty then
        raise (GuardError "rule regressed: an unreachable feed says nothing about this repo and must NEVER be drift (ADR-0105) — otherwise a nuget.org outage reads as 'every release here is a phantom'")

/// One HttpClient for the whole lane. `lazy` so it is constructed inside `main`'s try/with, like every
/// other input reader here — see `readInputs` for why nothing that can throw may run at module init.
let private feedClient =
    lazy (new Net.Http.HttpClient(Timeout = TimeSpan.FromSeconds 20.0))

/// Ask the flat container whether `id@version` is on the feed.
///
/// THREE OUTCOMES, and the split between them is the whole safety of this layer.
///
///   * 404 — the feed ANSWERED: this id has never been published. Absent.
///   * 200 with a flat-container index — the feed answered. Present iff `version` is in it.
///   * anything else — the feed did not answer. Unavailable.
///
/// They must never collapse: `apicompat-check.sh`'s `latest_version` returned "" for both "not published"
/// and "curl failed", the caller read "" as "no baseline yet", and a feed outage therefore reported every
/// package as a happy first publish and exited 0 (#216). Same split, same reason, one layer up.
///
/// A 200 IS NOT ENOUGH — IT MUST ALSO BE AN INDEX. A proxy error page, a captive portal, or a CDN
/// interstitial answers 200 with HTML, in which the version regex matches nothing — which reads as an
/// EMPTY version list, i.e. "the package is not published", i.e. a phantom. That inverts the rule above at
/// the one moment it matters: a malfunctioning feed would redden this repo, which is precisely what the
/// `Unavailable` arm exists to prevent. So the body must actually be the document we asked for; anything
/// else is the feed not answering. (Verified: a server returning `<html>503 via proxy</html>` with status
/// 200 reported all 18 packages as phantoms before this check existed.)
///
/// MEMBERSHIP, never max. The phantom is one specific version missing from an id that has plenty of others
/// — 0.9.1 is absent from a feed carrying 0.5.0 through 0.9.2 — so reading the newest version off the
/// array would find a package, call it Present, and miss the hole completely.
let probeFeedAsync (id: string) (version: string) : Async<FeedProbe> =
    async {
        let url = sprintf "%s/%s/index.json" feedBase (id.ToLowerInvariant())
        try
            use! resp = feedClient.Value.GetAsync url |> Async.AwaitTask
            if resp.StatusCode = Net.HttpStatusCode.NotFound then
                return Absent
            elif not resp.IsSuccessStatusCode then
                return Unavailable(sprintf "HTTP %d" (int resp.StatusCode))
            else
                let! body = resp.Content.ReadAsStringAsync() |> Async.AwaitTask
                if not (body.Contains "\"versions\"") then
                    return
                        Unavailable(
                            sprintf
                                "HTTP 200 but the body is not a flat-container index (no \"versions\" key, %d bytes) — a proxy or error page, not the feed"
                                body.Length
                        )
                else
                    // {"versions":["0.5.0","0.6.0",…]} — the key does not start with a digit, so this picks
                    // out the version strings and nothing else (apicompat-check.sh greps the same shape).
                    let published =
                        Regex.Matches(body, "\"([0-9][^\"]*)\"")
                        |> Seq.map (fun m -> m.Groups.[1].Value.Trim().ToLowerInvariant())
                        |> Set.ofSeq
                    // NuGet normalizes and lowercases what it serves, so compare on that footing.
                    return (if published.Contains(version.Trim().ToLowerInvariant()) then Present else Absent)
        with ex ->
            return Unavailable(sprintf "%s: %s" (ex.GetType().Name) (ex.Message.Replace("\n", " ")))
    }

/// CONCURRENTLY. The probes are independent, and serially they are not merely slow: a network that DROPS
/// packets rather than refusing the connection makes each one wait out the full client timeout, so ~18
/// probes × 20s = six minutes of a job whose whole job is to fail fast and loud. Run them together and the
/// wall clock is one timeout, not eighteen.
let probeAll (targets: (string * string) list) : Map<string * string, FeedProbe> =
    targets
    |> List.map (fun (id, v) ->
        async {
            let! probe = probeFeedAsync id v
            return (id, v), probe
        })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Map.ofArray

/// Minutes since `tag` was created. `%(creatordate:unix)` is the tag object's date for an annotated tag
/// and the tagged COMMIT's committer date for a lightweight one — and `release-tags.yml` cuts lightweight
/// tags on the merge commit, so for every automated release this reads "how long ago did the release
/// land", which is exactly the clock its publish runs against.
let tagAgeMinutes (tag: string) : float =
    let ec, out = run repoRoot "git" [ "for-each-ref"; "--format=%(creatordate:unix)"; sprintf "refs/tags/%s" tag ]
    if ec <> 0 then
        raise (GuardError(sprintf "git for-each-ref refs/tags/%s failed — cannot age the tag; fail closed" tag))
    match Int64.TryParse(out.Trim()) with
    | true, secs -> (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds secs).TotalMinutes
    | _ ->
        raise (
            GuardError(
                sprintf
                    "no creation time for tag %s (git said %A) — CI must fetch tags (fetch-depth: 0); fail closed rather than green-by-absence"
                    tag
                    (out.Trim())
            )
        )

/// The packages a tag PROMISED — read from the tag's OWN COMMIT, never from HEAD.
///
/// THIS IS THE WHOLE CORRECTNESS OF THE LAYER, and reading HEAD instead is a false-red generator. A tag
/// promises the coherent set as it stood AT THE COMMIT IT POINTS AT. Probe today's member list against
/// that tag's version and every member added SINCE the release is absent from the feed — so adding a
/// packable member (a structurally coherent commit: the verdict-core exits 0, and nothing in the repo
/// requires a pin bump to land one) would report a `release-phantom` against a release that was perfectly
/// fine, and keep reporting it on every run until the next release. That red is byte-identical to a
/// genuine phantom, so it would camouflage the very defect this layer exists to catch — the #506 mistake,
/// exactly, one gate over.
///
/// The BOM at the tag IS that set, and no extra bookkeeping is needed to know it: `bom-member-skew`
/// already holds, at every commit, that the BOM's dependency ids are exactly the packable FS.GG.UI.*
/// members. So the nuspec at the tag names both the members that release published and the meta-package
/// that fronts them. One `git show`, grounded in an invariant this same guard enforces.
///
/// Fails CLOSED on an unreadable or empty BOM: "I could not find out what this release promised" is not
/// "the release is fine", and an empty set would silently make every phantom unreportable.
let membersPromisedBy (tag: string) : Set<string> =
    let ec, out = run repoRoot "git" [ "show"; sprintf "%s:%s" tag nuspecRel ]
    if ec <> 0 then
        raise (
            GuardError(
                sprintf
                    "git show %s:%s failed — cannot read the coherent set that tag promised; fail closed rather than probing HEAD's member list against a historical release"
                    tag
                    nuspecRel
            )
        )
    let deps =
        Regex.Matches(out, "<dependency\\s+id=\"([^\"]+)\"")
        |> Seq.map (fun m -> m.Groups.[1].Value.Trim())
        |> Set.ofSeq
    if deps.IsEmpty then
        raise (GuardError(sprintf "the BOM at %s declares no dependencies — refusing to certify a release complete on the strength of an empty set" tag))
    let bomId =
        let m = Regex.Match(out, "<id>([^<]+)</id>")
        if m.Success then m.Groups.[1].Value.Trim()
        else raise (GuardError(sprintf "no <id> in %s at %s — cannot name the BOM package" nuspecRel tag))
    Set.add bomId deps

/// The template package's id AT ITS TAG, for the same reason: a `<PackageId>` rename after the release
/// would otherwise have us probing a name that release never published.
let templatePackageIdAt (tag: string) : string =
    let ec, out = run repoRoot "git" [ "show"; sprintf "%s:%s" tag templateFsprojRel ]
    if ec <> 0 then
        raise (GuardError(sprintf "git show %s:%s failed — cannot name the template package that tag promised; fail closed" tag templateFsprojRel))
    let m = Regex.Match(out, "<PackageId>([^<]+)</PackageId>")
    if m.Success then m.Groups.[1].Value.Trim()
    else raise (GuardError(sprintf "<PackageId> not found in %s at %s" templateFsprojRel tag))

/// What the CUT tags promise, and what the feed says about each.
///
/// SCOPED TO THE VERSIONS `main` CURRENTLY NAMES — the pin and the template package version — and
/// deliberately NOT to every `v*` tag ever cut. Two reasons; the second decides it:
///
///   * It is the state that HARMS. #679 was not "an old tag is untidy". It was "`main` is pinned to a
///     version nobody published", and every consumer scaffolding off that pin got NU1102. The versions
///     `main` names are the versions a consumer resolves.
///   * A whole-namespace sweep would be PERMANENTLY RED, and a permanently red gate is one nobody reads.
///     `v0.9.1` is a real phantom sitting in this repo's tag namespace RIGHT NOW — the abandoned #679
///     release, absent from nuget.org on every member — and the older tags predate `publish-packages`
///     existing at all. Reporting them on every run would bury the one tag that wedges the repo under a
///     wall of history nobody can act on. This layer goes red when the repo is WEDGED and green when it is
///     fixed, which is the only way its red means anything.
///
/// A tag that is not cut yet promises nothing, so this layer never speaks about one: `pin-no-tag` /
/// `pkg-no-release-tag` own "the tag is missing", and RELEASE-PENDING owns "it is coming". That is why
/// this layer needs no waiver of its own and is silent on a release PR by construction.
let feedObservations (i: Inputs) : FeedObservation list =
    // Every (id, version) the cut tags promise, resolved AT THE TAG (see `membersPromisedBy`), then probed
    // in one concurrent sweep.
    let framework =
        if List.contains i.PinVersion i.TagVersions then
            let tag = sprintf "fs-gg-ui/v%s" i.PinVersion
            // ALL of them, not a sample: a publish that died partway through `dotnet nuget push` leaves a
            // PARTIAL set, and a partial set is exactly what a consumer's restore breaks on. The live
            // smoke's `restore-partial` proves the set is complete on a feed packed FROM SOURCE; nothing
            // has ever proved it against the feed consumers actually use.
            [ for id in membersPromisedBy tag -> id, i.PinVersion, tag, tagAgeMinutes tag ]
        else
            []

    let template =
        // `v<pkg>` promises the TEMPLATE package, on its own decoupled version axis (the two axes
        // `release.yml` packs). It is the tag that TRIGGERS the publish, so a phantom here is #679's shape
        // exactly: the trigger fired into a void.
        if List.contains i.PkgVersion i.ReleaseTagVersions then
            let tag = sprintf "v%s" i.PkgVersion
            [ templatePackageIdAt tag, i.PkgVersion, tag, tagAgeMinutes tag ]
        else
            []

    let targets = framework @ template
    let probes = probeAll (targets |> List.map (fun (id, v, _, _) -> id, v))

    targets
    |> List.map (fun (id, v, tag, age) ->
        { Id = id
          Version = v
          Tag = tag
          TagAgeMin = age
          Probe = probes.[(id, v)] })

/// What was probed, what answered, and — loudly — what could not be checked.
///
/// `Unreachable` is announced separately and never as a pass: "the feed did not answer" and "every release
/// is real" must not produce the same observable. That is #216's rule (a check that could not run never
/// reports a pass), applied to the one layer here most likely to be unable to run.
let printFeedVerdict (t: FeedTally) (graceMin: float) =
    let total = t.Published.Length + t.Phantom.Length + t.InFlight.Length + t.Unreachable.Length
    printfn "feed lane: probed %d package(s) against %s" total feedBase
    printfn
        "  on the feed: %d · absent: %d · feed did not answer: %d"
        t.Published.Length
        (t.Phantom.Length + t.InFlight.Length)
        t.Unreachable.Length

    if not t.InFlight.IsEmpty then
        printfn
            "PUBLISH-IN-FLIGHT: %d package(s) are not on the feed yet, but their tag is younger than the %.0f-minute grace."
            t.InFlight.Length
            graceMin
        printfn "  a release takes ~25 min to publish and minutes more to index — this is not a phantom yet."
        for o in t.InFlight do
            printfn "    %s %s (tag %s, cut %.0f min ago)" o.Id o.Version o.Tag o.TagAgeMin

    if not t.Unreachable.IsEmpty then
        eprintfn
            "::error title=Feed coherence did not run::the feed did not answer for %d package(s) — NOTHING was checked for them. This is not a pass."
            t.Unreachable.Length
        for (o, why) in t.Unreachable do
            eprintfn "  %s %s (tag %s): %s" o.Id o.Version o.Tag why
        match Environment.GetEnvironmentVariable "GITHUB_STEP_SUMMARY" with
        | null | "" -> ()
        | summaryPath ->
            let s = System.Text.StringBuilder()
            s.AppendLine "### Version coherence — the feed lane could not run" |> ignore
            s.AppendLine "" |> ignore
            s.AppendLine(
                sprintf
                    "The feed did not answer for **%d** package(s). Nothing was compared for them, so this is **not a pass** — it is a check that could not run (#216)."
                    t.Unreachable.Length
            )
            |> ignore
            File.AppendAllText(summaryPath, s.ToString())

/// The success LINE, and it must say what is actually true.
///
/// A green feed lane means one of three quite different things, and only one of them is "the releases are
/// real": the feed may have answered for everything, or a publish may still be in flight, or THE FEED MAY
/// NOT HAVE ANSWERED AT ALL. Exit 0 is right in all three (ADR-0105 — a silent feed cannot redden this
/// repo), but a last line reading "every cut tag has its packages on the feed" after eighteen probes timed
/// out is a claim the guard never checked. That is the same class of lie as a red-that-means-ok, and this
/// file already makes exactly this correction once: see `pinNote`, where keying the success line off the
/// wrong predicate made it misstate a fully-released pin.
///
/// So: the exit code carries the VERDICT; this line carries the STATE. A run that proved nothing says so.
let feedNote (t: FeedTally) (graceMin: float) =
    let total = t.Published.Length + t.Phantom.Length + t.InFlight.Length + t.Unreachable.Length
    if total = 0 then
        // No cut tag names the current pin/package — a release is pending. Nothing was promised, so nothing
        // was checked, and saying so is the honest form of a green run.
        "no cut tag names the current pin/package — the feed lane had nothing to check"
    elif t.Unreachable.Length = total then
        sprintf "feed lane DID NOT RUN — the feed did not answer for any of the %d package(s). NOTHING was compared." total
    else
        let parts =
            [ sprintf "%d/%d on %s" t.Published.Length total feedBase
              if not t.InFlight.IsEmpty then
                  sprintf "%d still publishing (inside the %.0f-min grace)" t.InFlight.Length graceMin
              if not t.Unreachable.IsEmpty then
                  sprintf "%d NOT CHECKED — the feed did not answer" t.Unreachable.Length ]
        String.Join("; ", parts)

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
let reportPathRel = "specs/209-version-staleness-guard/readiness/version-coherence.md"
let reportPath = repo reportPathRel

// #514 — A COMMITTED, BYTE-GATED ARTIFACT MUST BE A PURE FUNCTION OF THE COMMIT IT LIVES IN.
//
// This report used to record the tags it observed via `git tag --list` (`i.LatestTag`,
// `i.LatestReleaseTag`, `i.LatestTemplateTag`). Those are EXTERNAL, MUTABLE state: the tag namespace
// changes with no commit at all. So the file self-invalidated on every release — `release-tags.yml`
// cuts `fs-gg-ui/v<pin>` AFTER the bump merges, and the artifact the release PR committed named the
// PREVIOUS tag the instant the new one appeared. The Deterministic gate is REQUIRED on `main`, so main
// went red, and every PR branched from it inherited that red: #515 stopped the whole repo, and it was
// the third recurrence (#435, #477 both regenerated it by hand). The class is #517.
//
// The old comment below called that "KNOWN, INTENDED FRICTION" and offered regenerate-from-the-release-
// lane as the escape. That treats the symptom: re-committing an impure artifact only narrows the window
// (and it cannot close it here anyway — `main` requires this very gate with `enforce_admins`, so the
// lane cannot push the fix directly). The defect is the impurity itself, and a tag pushed or deleted by
// hand would rot the file again with the choreography in place.
//
// So the recorded tags are now derived from what the COMMIT says, not from what the tag namespace says:
// the snapshot tag this PIN owes, and the release/template tags this PACKAGE VERSION owes. In a coherent
// repo those are equal to the observed tags by definition — that equality IS the invariant this guard
// enforces — so today's bytes are unchanged. What changes is that they no longer mutate underneath the
// commit: on a release the file records the tag the release is about to cut, matches the fresh render on
// the release PR, and STILL matches it after `release-tags.yml` cuts that tag. The staleness is gone.
//
// The tags are still CHECKED against reality, and that has not moved: `pin-no-tag` / `pkg-no-release-tag`
// / the successor-tag bounds all still read the real `git tag --list` and still fail on real drift, with
// the RELEASE-PENDING waivers unchanged. This changes only what is WRITTEN DOWN, never what is verified —
// which is the distinction the #435 rule (an evidence artifact must be read by a gate) needs to survive.
let renderReport (i: Inputs) (provenance: string) (failures: Failure list) (liveOpt: LiveResult option) =
    let sb = System.Text.StringBuilder()
    let line (s: string) = sb.AppendLine s |> ignore
    let ok = failures.IsEmpty
    line "# FS.GG.UI version coherence — verdict report"
    line ""
    line "Regenerated by `scripts/validate-version-coherence.fsx`. The merge-blocking gate step"
    line "\"Version coherence guard\" re-derives this verdict on every PR."
    line ""
    line "The tag lines below are the tags this COMMIT owes — derived from the pin and the package"
    line "version, never from `git tag --list`. A committed artifact that recorded the observed tag set"
    line "would go stale the moment a release cut a tag, with no commit to blame (#514, class #517); that"
    line "the owed tags equal the observed ones is checked live, on every run, and is what `result:` says."
    line ""
    line "- feature: 209-version-staleness-guard"
    line (sprintf "- result: %s" (if ok then "pass" else "fail"))
    line (sprintf "- provenance: %s" provenance)
    line (sprintf "- single-version-source: %s (`%s`, occurrences=%d)" i.PinVersion i.PropsLoc i.Occurrences)
    line (sprintf "- snapshot-tag-for-pin: fs-gg-ui/v%s" i.PinVersion)
    line (sprintf "- published-members: %d · bom-deps: %d · template-consumed-pins: %d" i.PublishedMembers.Count i.BomIds.Count i.TemplateIds.Count)
    line (sprintf "- runtime-regex-resolves: %b" i.RuntimeRegexResolves)
    line (sprintf "- template-package-version: %s (`%s`)" i.PkgVersion i.PkgVersionLoc)
    line (sprintf "- release-tag-for-package: v%s · template-tag-for-package: fs-gg-ui-template/v%s" i.PkgVersion i.PkgVersion)
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
    sb.ToString()

let writeRendered (content: string) =
    Directory.CreateDirectory(Path.GetDirectoryName reportPath) |> ignore
    File.WriteAllText(reportPath, content)

let writeReport (i: Inputs) (provenance: string) (failures: Failure list) (liveOpt: LiveResult option) =
    writeRendered (renderReport i provenance failures liveOpt)

// ---- the artifact is EVIDENCE, so something must read it (#435) --------------------------------
//
// This script has always WRITTEN the verdict report; nothing ever checked that the copy COMMITTED to
// the repo still says what a fresh run would say. So the artifact rotted in place: it recorded the
// 0.5.0 world while the repo sat at 0.8.0 — the 0.6.0, 0.7.0 and 0.8.0 releases each moved the pin and
// none regenerated it. The gate stayed green throughout, because the LIVE script always computes the
// right answer; only the committed evidence lied. An evidence artifact no gate reads is not evidence,
// and it will always drift — so read it here, in the guard that owns it, and call a stale copy DRIFT.
//
// Compared against the copy at HEAD, NOT the working tree. gate.yml runs this script TWICE — once bare,
// then once with FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 — and the second run overwrites the file with the
// `live` render. A working-tree comparison would therefore be self-satisfying on the first run and
// impossible on the second. HEAD is the only stable subject.
//
// The canonical committed artifact is the VERDICT-CORE render (its `provenance:` line says so, and the
// live lines it lacks say `pending-live`). The live render is transient CI output that is never
// committed, so the check runs in verdict-core mode only and always compares against a verdict-core
// render — which is also why `writeReport` below is handed `structural` rather than the full failure
// list: the report must not describe its own staleness, or regenerating it could never make it match.
//
// A RELEASE NO LONGER MAKES THIS RULE FIRE (#514). It used to, by construction: the report recorded the
// latest `fs-gg-ui/v*`, `v*` and `fs-gg-ui-template/v*` tags as OBSERVED, and those tags are pushed AFTER
// the release commit merges — so the artifact the release PR committed named the previous tags the moment
// the new ones were cut, and the first change to land after a release reddened with `artifact-stale`. That
// was written off as intended friction, and it was not: the Deterministic gate is REQUIRED on `main`, so
// the "friction" was the whole repo unable to merge (#515), for the third time (#435, #477).
//
// The report now records the tags the COMMIT OWES (pin-derived and package-derived) rather than the tags
// the namespace happens to hold — see `renderReport`. A committed artifact is a pure function of its
// commit again, so cutting a tag cannot rot it, and this rule fires only for the reason it should: a
// derived file whose INPUTS IN THE TREE changed and which nobody regenerated.
//
// Do not reintroduce an observed-tag field here. It reads as richer evidence and it is not: it makes the
// artifact self-invalidating, and the thing it would "evidence" (that the owed tags exist) is already
// checked live on every run by `pin-no-tag` / `pkg-no-release-tag` / the successor-tag bounds — which is
// where a REAL tag drift must surface, because those rules can fail the build and a stale byte-compare
// can only say "regenerate me".
let normalize (s: string) = s.Replace("\r\n", "\n")

/// The artifact as COMMITTED at HEAD — `None` when HEAD has no such path (never committed / deleted).
/// Fails closed (exit 2) if git cannot answer at all: "the guard could not decide" is not "the repo is
/// coherent", and a shallow or detached checkout must not silently waive the only check of this file.
let committedReport () : string option =
    let ec, out = run repoRoot "git" [ "rev-parse"; "--verify"; "HEAD" ]
    if ec <> 0 then
        raise (GuardError(sprintf "git rev-parse HEAD failed — cannot read the committed artifact to compare against; fail closed rather than green-by-absence:\n%s" out))
    let ec, out = run repoRoot "git" [ "show"; sprintf "HEAD:%s" reportPathRel ]
    if ec <> 0 then None else Some out

/// The first line on which the committed artifact and a fresh render disagree — so the operator sees
/// WHAT rotted (house style: expected-vs-actual), not merely that the file is "different". `None` ⇔ the
/// two agree, so this IS the equality test: line-wise over the newline-normalized text, which keeps a
/// CRLF checkout from reading as drift.
let private firstDiff (committed: string) (fresh: string) =
    let c = (normalize committed).Split('\n')
    let f = (normalize fresh).Split('\n')
    Seq.init (max c.Length f.Length) id
    |> Seq.tryPick (fun n ->
        let at (a: string[]) = if n < a.Length then a.[n] else "<end of file>"
        if at c <> at f then Some(n + 1, at c, at f) else None)

/// Takes the ALREADY-RENDERED verdict-core report — the same string that is written to disk — so the
/// bytes compared against HEAD and the bytes offered as the fix cannot drift apart.
let artifactStaleFailures (fresh: string) : Failure list =
    let fix =
        sprintf
            "regenerate and commit it: `dotnet fsi scripts/validate-version-coherence.fsx && git add -- %s`  (commit the BARE run's output — the FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 render is transient CI output, not the artifact)"
            reportPathRel
    match committedReport () with
    | None ->
        [ { Rule = "artifact-not-committed"
            Location = reportPathRel
            Expected = "the verdict report is committed — it is this feature's readiness evidence"
            Actual = "no such path at HEAD"
            Fix = fix } ]
    | Some committed ->
        match firstDiff committed fresh with
        | None -> []
        | Some(n, committed', fresh') ->
            [ { Rule = "artifact-stale"
                Location = sprintf "%s:%d" reportPathRel n
                Expected = fresh'
                Actual = sprintf "%s (committed)" committed'
                Fix = fix } ]

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
    symbologyRulesSelfCheck ()
    feedRulesSelfCheck ()
    let i = readInputs ()
    printReleasePending (pendingTags i)
    if feedLane then
        // A SEPARATE LAYER, not a richer verdict-core. It re-runs the structural rules first — they are
        // cheap, and a feed verdict pronounced over an already-incoherent repo would be noise — and then
        // asks the feed about the tags that actually exist.
        //
        // IT DOES NOT WRITE THE REPORT, and that is deliberate. The committed artifact is byte-gated
        // against a verdict-core render, and #514 is the whole story of what happens when external
        // mutable state gets recorded in it: the OBSERVED tag set rotted the file on every release, the
        // required gate reddened on `main`, and nothing in the repo could merge — three times (#435,
        // #477, #515). Feed state is strictly worse than tag state: it moves with no commit AND no tag.
        // The feed verdict belongs in the exit code and the job summary, which is where a non-required
        // job's finding belongs — not in a file another gate compares byte-for-byte.
        let grace = publishGraceMinutes.Value
        let t = tallyFeed grace (feedObservations i)
        let failures = structuralFailures i @ feedRules grace t
        printFeedVerdict t grace
        if failures.IsEmpty then
            printfn "version coherence: COHERENT (structural verdict-core). %s" (feedNote t grace)
            0
        else
            printDrift failures
            eprintfn "version coherence: DRIFT — %d failure(s) (structural + feed)" failures.Length
            1
    elif live then
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
        // Render ONCE, then write that exact string and compare that exact string against HEAD — the
        // staleness verdict is added to the EXIT code, never to the file. An artifact that described its
        // own staleness would move the target every time it was regenerated, so it could never converge.
        let structural = structuralFailures i
        let fresh = renderReport i "verdict-core" structural None
        writeRendered fresh
        // The artifact records a VERDICT, so only check the evidence when there is a clean verdict for it
        // to record. Running it while the repo is ALREADY incoherent adds a second red for a file nobody
        // broke, and its `Fix` would tell the author to commit an artifact whose own `result:` is `fail` —
        // advice that fixes nothing and enters a failing verdict into the readiness record. Fix the drift;
        // the evidence is checked on the way back to green.
        let failures = if structural.IsEmpty then artifactStaleFailures fresh else structural
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
