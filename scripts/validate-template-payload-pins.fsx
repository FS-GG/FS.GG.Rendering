// #241 — template payload pin restore gate.
//
// `template/base/Directory.Packages.props` declares three version axes — $(FsGgUiVersion),
// $(FsGgGameVersion), $(FsGgAudioVersion) — that become a SCAFFOLDED PRODUCT's package pins.
// Nothing in CI ever restored them. Package.Tests reads the file as TEXT: AudioProfileWiringTests
// asserts axis STRUCTURE (one literal per axis, the four FS.GG.Audio.* deriving through
// $(FsGgAudioVersion), the `game || sample-pack` gate) and explicitly disclaims the VALUE. Template
// payload is content, not a nuspec dependency, so NU5104 cannot see it either.
//
// The consequence was #235: a STABLE fs-gg-ui template (0.4.0) scaffolded products restoring
// PRERELEASE FS.GG.Game.* / FS.GG.Audio.*, with $(FsGgGameVersion) also two minors stale, and
// nothing was red for months. A pin that is stale, prerelease, yanked, or simply nonexistent passed
// every check in this repo. This is the fails-open class of FS-GG/.github#266: "nothing to check"
// and "checked, and it's fine" must not share an exit code.
//
// Two layers, mirroring scripts/validate-version-coherence.fsx:
//
//   * Structural verdict-core (always, env-free, offline): the three axis literals are present
//     exactly once each and are well-formed SemVer; EVERY `FS.GG.*` PackageVersion derives through
//     one of the three axes (never a bare literal, which no axis bump would sweep); every profile's
//     `<!--#if -->` gate has a shape this script can evaluate; and every profile resolves a
//     NON-EMPTY pin set. Zero pins matched is an ERROR, never a pass.
//
//   * Restore-grounded proof (FS_GG_RUN_TEMPLATE_PAYLOAD_RESTORE=1): for each of the five scaffold
//     profiles, restore that profile's real pin set on the template's real TFM (net10.0) against
//     nuget.org, and assert the RESOLVED GRAPH — not the literals:
//       - every pinned (id, version) exists on the feed;
//       - no resolved `FS.GG.*` package, INCLUDING TRANSITIVE, carries a prerelease suffix (a
//         prerelease can enter a scaffolded product's graph without ever appearing in the props
//         file — `FS.GG.Game.Render` reaches down to `FS.GG.UI.Scene` — so a regex over the three
//         literals would be insufficient), unless the template is on the preview channel, which is
//         asserted by $(FsGgUiVersion) alone (see `previewChannel`);
//       - NU1603 (version substituted) / NU1608 / NU1101 / NU1102 are ERRORS, so a nonexistent pin
//         cannot resolve upward silently;
//       - the $(FsGgGameVersion) / $(FsGgAudioVersion) pins do not LAG feed-newest (the staleness
//         direction that caught nothing when Game sat two minors behind). Sibling prior art:
//         `.github`'s check-feed-coherence.py, which fails BOTH directions.
//
// WHY $(FsGgUiVersion) IS EXEMPT FROM THE STALENESS RULE (and only that rule). The FS.GG.UI.* set is
// published FROM THIS REPO, and Feature 209's guard already pins it to `fs-gg-ui/v*` snapshot tags —
// the source of truth that exists at merge time. Feed-newest does not: between a release landing on
// `main` and the packages appearing on nuget.org, feed-newest LAGS the repo, and a "pin >= newest"
// rule would red every PR in that window against this repo's own release. Game/Audio ship from OTHER
// repos, where the feed IS the source of truth and no such window exists. UI pins are still proved to
// exist and to be non-prerelease by the restore layer.
//
// Exit codes (fails CLOSED, never green-by-absence):
//   0  coherent
//   1  drift — a named pin/graph violation, expected-vs-actual
//   2  guard error — inputs unreadable, feed unreachable, an unevaluable gate, zero pins matched,
//      or restore tooling failed. The guard did not run; that is not a pass (#216).
//
// This runs as a SEPARATE, feed-dependent job (`template-payload-restore-gate`), not as a step of
// the required `gate` job — the same bound ADR-0101 places on `api-compatibility-gate`: requiring a
// feed-dependent check takes a dependency on nuget.org's availability, and a feed outage must not
// wedge every merge in the repo. "Advisory" means NOT-REQUIRED; it does NOT mean `continue-on-error`
// (#216). This script reddens its job whenever it cannot prove the payload restores.
//
// RELEASE-PENDING (#506) — WHY AN UNPUBLISHED $(FsGgUiVersion) IS NOT ALWAYS DRIFT.
//
// The FS.GG.UI.* set is published FROM THIS REPO, and the pin bump is what CAUSES the publish:
// `release-tags.yml` cuts `fs-gg-ui/v<pin>` on merge to `main` and calls `release.yml`. So on a
// release PR the pin NECESSARILY names a version nuget.org does not carry yet, and `pin-not-published`
// + NU1102 `pin-does-not-resolve` on all five profiles fired BY CONSTRUCTION — this gate was red on
// every framework release (0.8.0 #426, 0.9.0 #498) and merged past both times, correctly.
//
// That is the always-red advisory gate of FS.GG.SDD#362, and it was worse than noise: a GENUINE
// `pin-not-published` (a typo, a pin onto a version never published, a half-failed release) produced a
// BYTE-IDENTICAL red. "That's just the release window, merge it" is right on a release PR and
// catastrophic on any other, and nothing in the output told the two apart.
//
// So the waiver, bounded exactly as `validate-version-coherence.fsx`'s `PinPending` is:
//
//   * ONLY the $(FsGgUiVersion) axis. This is the same asymmetry that exempts it from the staleness
//     rule (see the note above), and it is the whole safety of the waiver. FS.GG.Game.* / FS.GG.Audio.*
//     ship from OTHER repos: bumping their axis HERE publishes nothing, so the version must already
//     exist on the feed. An unpublished Game/Audio pin is a real defect EVEN ON THE COMMIT THAT BUMPED
//     IT, and is never waived. A naive "this commit bumped the axis ⇒ waive" would reopen #235 —
//     a stale/nonexistent component pin, green.
//
//   * ONLY when THIS commit bumped it (`bumpedInCommitUnderTest`, the predicate #209 already proved
//     out). A pin nobody bumped that the feed does not carry is stale or typo'd — drift, as before.
//
//   * ONLY when the pin is genuinely absent from the feed. If it is published, nothing is pending and
//     the full restore proof runs as usual — which is the common case, and it keeps the git call off
//     the path entirely.
//
//   * NEVER in the release lane (`FS_GG_VERSION_COHERENCE_RELEASE_LANE`, set job-wide by `release.yml`).
//     The waiver's premise is "these packages cannot exist yet — this very commit creates them", which
//     is only true BEFORE the publish. At publish time they are due, and a missing one is drift. Nothing
//     runs this script in that lane TODAY; reading the flag release.yml already sets means the door is
//     shut the moment someone adds it, rather than depending on them reading this comment. (The name
//     carries a legacy prefix — it is a property of the JOB, "this job gates a publish", not of the
//     version-coherence guard; release.yml documents it as exactly that.)
//
// WHAT THE WAIVER COSTS, AND WHY THAT IS NAMED RATHER THAN HIDDEN. In the window, no profile can
// restore — every profile pins FS.GG.UI.Build/.Scene — so the proofs that NEED A RESOLVED GRAPH cannot
// run: `pin-resolved-elsewhere`, and the TRANSITIVE half of `prerelease-in-scaffolded-graph`. They are
// SKIPPED, not passed: printing "COHERENT (structural + restore-grounded)" there would be the #266 lie
// this script's own header forbids ("nothing to check" and "checked, and it's fine" must not share an
// exit code). The RELEASE-PENDING block names what did not run and what still did.
//
// The waiver skips the RESTORE — it does not skip the rules the restore is not needed for. Everything
// answerable from the axis literals or from the feed keeps running, and keeps deciding the exit code:
// the structural verdict-core; Game/Audio existence and staleness; and the DIRECT half of
// `prerelease-in-scaffolded-graph` (`directPrereleaseFailures`), because a prerelease sitting in an axis
// literal needs no graph to see. Dropping that last one through the window would let a stable release
// ship a template pinning a prerelease $(FsGgAudioVersion) and exit 0 — #235 in a new coat, waved
// through by the very waiver that is supposed to be bounded.
//
// THE WAIVER IS BOUNDED IN TIME, NOT BY THE FEED'S MOMENTARY STATE. A half-failed publish (tags cut,
// some packages up, some not) is waived at the bump commit — deliberately, because failing there would
// red on the ordinary race between this gate and an in-flight publish, which is a red-that-means-ok
// again. It self-heals: the NEXT commit to `main` does not bump the pin, the waiver is off, and
// `pin-not-published` reds for real. Same self-heal `pin-no-tag`/`pkg-no-release-tag` rely on.

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.RegularExpressions

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName
let repo (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))
let live = Environment.GetEnvironmentVariable "FS_GG_RUN_TEMPLATE_PAYLOAD_RESTORE" = "1"

/// RELEASE LANE — set job-wide by any job that gates a PUBLISH (`release.yml`). Kills the
/// RELEASE-PENDING waiver outright: it exists because the packages cannot exist before the commit that
/// publishes them, which is only true BEFORE the merge. At publish time they are due. See the header.
let releaseLane = Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_RELEASE_LANE" = "1"

/// Raised for any unreadable input / unreachable feed / tooling failure ⇒ exit 2 (fail closed).
exception GuardError of string

/// A named pin/graph violation ⇒ exit 1.
type Failure =
    { Rule: string
      Location: string
      Expected: string
      Actual: string
      Fix: string }

// ---- shell + io helpers -----------------------------------------------------------------------
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
/// Lifted verbatim in semantics from validate-version-coherence.fsx, whose `PinPending` this mirrors;
/// duplicated for the same reason the SemVer comparator is (standalone `dotnet fsi` entry points, no
/// package reference between them).
///
/// Compares the element's VALUE across the diff, not merely whether its line was touched: this
/// predicate WAIVES a fail-closed rule, so a reindent or a line-ending change to the <FsGgUiVersion>
/// line must not be able to silence it. Added values must exist and differ from removed ones.
///
/// Env-free by construction: `HEAD~1` is the first parent, which is the base branch for a
/// `pull_request` merge-ref checkout AND the previous `main` commit for a squash/merge push — so the
/// same diff answers both contexts without reading GITHUB_*.
///
/// Fails CLOSED if git cannot answer (a shallow clone with no `HEAD~1`). Defaulting to "not bumped"
/// would be the quiet choice and the wrong one: it reverts silently to the always-red gate #506 is
/// about, and nobody would know why. CI must check out with `fetch-depth: 0` — gate.yml's
/// `template-payload-restore-gate` job now does.
let bumpedInCommitUnderTest (rel: string) (element: string) =
    let ec, out = run repoRoot "git" [ "diff"; "HEAD~1"; "HEAD"; "--unified=0"; "--"; rel ]
    if ec <> 0 then
        raise (GuardError(sprintf "git diff HEAD~1 HEAD -- %s failed — the RELEASE-PENDING waiver cannot be evaluated without full history (fetch-depth: 0); fail closed rather than red-for-the-wrong-reason" rel))
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

// ---- preview-aware SemVer comparator ----------------------------------------------------------
// Same semantics as validate-version-coherence.fsx's: numeric core numerically, then dotted
// prerelease identifiers per SemVer §11. Duplicated rather than shared because these scripts are
// standalone `dotnet fsi` entry points with no package reference between them; the self-check below
// is what keeps the duplicate honest.
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
        | (true, _), (false, _) -> -1
        | (false, _), (true, _) -> 1
        | _ -> String.CompareOrdinal(a, b)

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
            | [], _ -> 1
            | _, [] -> -1
            | pa, pb ->
                let rec loop xs ys =
                    match xs, ys with
                    | [], [] -> 0
                    | [], _ -> -1
                    | _, [] -> 1
                    | x :: xs', y :: ys' ->
                        let c = cmpId x y in if c <> 0 then c else loop xs' ys'
                loop pa pb

    let lt a b = cmp (parse a) (parse b) < 0
    let isPrerelease (s: string) = not (parse s).Pre.IsEmpty
    let wellFormed (s: string) = Regex.IsMatch(s.Trim(), @"^\d+\.\d+\.\d+(-[0-9A-Za-z.\-]+)?$")

/// Fail closed if the comparator ever regresses — every staleness verdict below rides on it.
let semverSelfCheck () =
    if not (SemVer.lt "0.1.9" "0.1.10") then raise (GuardError "comparator regressed: 0.1.9 < 0.1.10")
    if not (SemVer.lt "0.1.0-preview.1" "0.1.0") then
        raise (GuardError "comparator regressed: 0.1.0-preview.1 < 0.1.0 (prerelease is lower)")
    if not (SemVer.lt "0.2.0" "0.4.0") then raise (GuardError "comparator regressed: 0.2.0 < 0.4.0")

// ---- the template payload ---------------------------------------------------------------------
let propsRel = "template/base/Directory.Packages.props"

/// The `profile` choices of `.template.config/template.json`. Every one of them scaffolds a product
/// whose pins must restore, so every one of them is proved.
let profiles = [ "app"; "headless-scene"; "governed"; "sample-pack"; "game" ]

/// The axis this repo PUBLISHES. It is the only one a commit here can make legitimately-unpublished
/// (the bump is what triggers the release), so it is the only one the RELEASE-PENDING waiver touches,
/// and — for the same reason — the only one exempt from the staleness rule. See the header.
let uiAxis = "FsGgUiVersion"

/// The three axes, and which feed package's newest version each is compared against for staleness.
/// `None` = exempt from the staleness rule (see the header note on $(FsGgUiVersion)).
let axes =
    [ uiAxis, None
      "FsGgGameVersion", Some "FS.GG.Game.Core"
      "FsGgAudioVersion", Some "FS.GG.Audio.Core" ]

/// The `dotnet new` gate opened for the line at `lineIndex`, or None if the line is ungated. Walks
/// upward to the nearest `<!--#if ... -->` not already closed by an intervening `<!--#endif -->`
/// (the same walk AudioProfileWiringTests uses; the props file's regions do not nest today, and the
/// depth counter keeps this correct if they ever do).
let enclosingGate (lines: string[]) (lineIndex: int) =
    let ifRegex = Regex(@"<!--#if\s+\((?<cond>.*?)\)\s*-->")
    let rec walk i depth =
        if i < 0 then None
        elif lines.[i].Contains "<!--#endif" then walk (i - 1) (depth + 1)
        else
            let m = ifRegex.Match lines.[i]
            if m.Success then
                if depth = 0 then Some(m.Groups.["cond"].Value.Trim()) else walk (i - 1) (depth - 1)
            else walk (i - 1) depth
    walk lineIndex 0

/// Does `condition` hold for `profile`? Only the disjunction-of-equalities shape the props file uses
/// is understood. Anything else raises: silently mis-evaluating a gate would hand a profile the wrong
/// pin set and prove the wrong graph, which is worse than not running.
let gateHolds (condition: string) (profile: string) =
    let shape = Regex(@"^\s*profile\s*==\s*""[^""]+""\s*(\|\|\s*profile\s*==\s*""[^""]+""\s*)*$")
    if not (shape.IsMatch condition) then
        raise (GuardError(sprintf "unevaluable `<!--#if -->` gate in %s: `%s` — this guard understands only `profile == \"a\" || profile == \"b\"`; teach it the new shape rather than letting a profile restore the wrong pin set" propsRel condition))
    Regex.Matches(condition, "\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Seq.contains profile

type Pin =
    { Id: string
      Axis: string
      Version: string
      Gate: string option }

type Inputs =
    { AxisVersions: Map<string, string>
      Pins: Pin list
      /// A bare-literal (non-axis-derived) FS.GG.* pin: `id`, `version`.
      LiteralPins: (string * string) list
      PreviewChannel: bool }

/// The pins a given profile's scaffold actually restores.
let pinsFor (i: Inputs) (profile: string) =
    i.Pins
    |> List.filter (fun p ->
        match p.Gate with
        | None -> true
        | Some c -> gateHolds c profile)

let readInputs () : Inputs =
    let text = readFile (repo propsRel)
    let lines = text.Replace("\r\n", "\n").Split('\n')

    let axisVersions =
        axes
        |> List.map (fun (axis, _) ->
            let ms = Regex.Matches(text, sprintf "<%s>([^<]*)</%s>" axis axis)
            if ms.Count <> 1 then
                raise (GuardError(sprintf "%s declares <%s> %d times — expected exactly 1; the axis is the single source of that component's pin" propsRel axis ms.Count))
            let v = ms.[0].Groups.[1].Value.Trim()
            if not (SemVer.wellFormed v) then
                raise (GuardError(sprintf "%s: <%s> is `%s`, not a well-formed SemVer" propsRel axis v))
            axis, v)
        |> Map.ofList

    // `Version="$(Axis)"` is the only legal form for an FS.GG.* pin. A bare literal is reported as
    // drift rather than raised: it is a real, nameable defect in the props file, not a guard failure.
    let axisRef = Regex(@"^\$\((?<axis>\w+)\)$")
    let pkgVersion = Regex(@"<PackageVersion\s+Include=""(?<id>[^""]+)""\s+Version=""(?<ver>[^""]+)""")
    let matched =
        lines
        |> Array.mapi (fun idx line -> idx, pkgVersion.Match line)
        |> Array.filter (fun (_, m) -> m.Success && m.Groups.["id"].Value.StartsWith("FS.GG.", StringComparison.Ordinal))
        |> Array.toList

    if matched.IsEmpty then
        raise (GuardError(sprintf "%s declares zero FS.GG.* PackageVersion pins — nothing to check is not the same as checked-and-fine (#266)" propsRel))

    let pins, literalPins =
        matched
        |> List.fold
            (fun (pins, lits) (idx, m) ->
                let id = m.Groups.["id"].Value
                let ver = m.Groups.["ver"].Value
                let am = axisRef.Match ver
                if not am.Success then pins, (id, ver) :: lits
                else
                    let axis = am.Groups.["axis"].Value
                    match Map.tryFind axis axisVersions with
                    | None -> pins, (id, ver) :: lits
                    | Some v -> { Id = id; Axis = axis; Version = v; Gate = enclosingGate lines idx } :: pins, lits)
            ([], [])

    let pins = List.rev pins
    // The template's CHANNEL is $(FsGgUiVersion), and only that: it is the framework pin the
    // fs-gg-ui template ships, so `-preview.N` there means "this template scaffolds preview
    // products" and a prerelease component is coherent with it.
    //
    // It must NOT be "any axis is prerelease". That reading is the #235 defect wearing the guard's
    // uniform: a prerelease $(FsGgAudioVersion) would declare the preview channel that excuses
    // itself, and a stable 0.4.0 template would go on scaffolding preview-consuming products, green.
    // The channel is asserted by the axis that cannot lie about it.
    let previewChannel = SemVer.isPrerelease (Map.find uiAxis axisVersions)
    { AxisVersions = axisVersions
      Pins = pins
      LiteralPins = List.rev literalPins
      PreviewChannel = previewChannel }

// ---- structural verdict-core (env-free) -------------------------------------------------------
let structuralFailures (i: Inputs) : Failure list =
    [ for (id, ver) in i.LiteralPins do
          yield
              { Rule = "pin-not-axis-derived"
                Location = sprintf "%s (%s)" propsRel id
                Expected = "Version=\"$(FsGgUiVersion|FsGgGameVersion|FsGgAudioVersion)\""
                Actual = sprintf "Version=\"%s\"" ver
                Fix = sprintf "derive %s through its component's axis — a bare literal is invisible to an axis bump and to this guard's staleness rule" id }

      // Every profile must resolve a non-empty pin set. A profile that restores nothing is either a
      // broken gate or a deleted region, and it would otherwise report a vacuous PASS.
      for profile in profiles do
          if (pinsFor i profile).IsEmpty then
              yield
                  { Rule = "profile-restores-nothing"
                    Location = sprintf "%s (profile=%s)" propsRel profile
                    Expected = ">= 1 FS.GG.* pin"
                    Actual = "0 pins matched"
                    Fix = "a scaffold profile with no FS.GG.* pin proves nothing — check its `<!--#if -->` gate" } ]

// ---- the feed ---------------------------------------------------------------------------------
// Version enumeration comes from the flat container, whose `index.json` is the authoritative list of
// a package's published versions. An unreachable feed / a 5xx / a 200 listing nothing is a GUARD ERROR:
// the question "does this pin exist?" was not answered, and unanswered must never read as answered-yes.
//
// A 404 IS AN ANSWER, and it must not be confused with the feed failing to give one. The flat container
// returns 404 for an id it carries no package under — "I have nothing by that name" — which is a FACT
// about the feed, not a failure to establish one. It is also the NORMAL state of a package the release
// under test INTRODUCES (FS.GG.UI.Symbology.Render, .DesignSystem and .Themes.Default each entered the
// props file this way). Raising here would exit 2 BEFORE the RELEASE-PENDING waiver is ever consulted,
// so a release that adds a package would stay red — the exact thing #506 exists to stop. Treat it as
// the empty version list it is, and let the rules above decide what that means:
//   * in the release window, the package lands in `pending` and is waived;
//   * outside it, `pin-not-published` reports it as named drift (exit 1) rather than an opaque guard
//     error — which is also a strictly better answer for a typo'd package id.
let http = new Net.Http.HttpClient(Timeout = TimeSpan.FromSeconds 30.0)

let feedCache = Collections.Generic.Dictionary<string, string list>()

let feedVersions (packageId: string) : string list =
    match feedCache.TryGetValue packageId with
    | true, vs -> vs
    | _ ->
        let url = sprintf "https://api.nuget.org/v3-flatcontainer/%s/index.json" (packageId.ToLowerInvariant())
        let resp =
            try
                http.GetAsync(url).GetAwaiter().GetResult()
            with ex ->
                raise (GuardError(sprintf "nuget.org unreachable for %s: %s" packageId ex.Message))
        let versions =
            if resp.StatusCode = Net.HttpStatusCode.NotFound then
                [] // the feed answered: it carries no package by this id. See the note above.
            elif not resp.IsSuccessStatusCode then
                raise (GuardError(sprintf "nuget.org returned %d for %s — the feed could not answer whether %s exists; fail closed" (int resp.StatusCode) url packageId))
            else
                let body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                use doc = JsonDocument.Parse body
                let vs =
                    doc.RootElement.GetProperty("versions").EnumerateArray()
                    |> Seq.map (fun e -> e.GetString())
                    |> Seq.toList
                // A 200 that lists nothing is the feed contradicting itself — it claims the package
                // exists and then names no version of it. That is not an answer; fail closed.
                if vs.IsEmpty then
                    raise (GuardError(sprintf "nuget.org returned 200 but listed zero versions for %s — fail closed" packageId))
                vs
        feedCache.[packageId] <- versions
        versions

/// Newest version on the feed under the repo's channel: stable-only unless the repo declares a
/// preview channel in its axis literals.
let feedNewest (previewChannel: bool) (packageId: string) =
    let candidates =
        feedVersions packageId
        |> List.filter (fun v -> previewChannel || not (SemVer.isPrerelease v))
    match candidates with
    | [] ->
        raise (GuardError(sprintf "%s has no %s version on nuget.org — the staleness rule has nothing to compare against; fail closed" packageId (if previewChannel then "published" else "stable")))
    | vs -> vs |> List.reduce (fun a b -> if SemVer.lt a b then b else a)

// ---- restore-grounded proof (live) ------------------------------------------------------------
/// NuGet diagnostics that mean "the pin did not resolve as written". Promoted to errors in the probe
/// project so a substituted or missing version fails the restore instead of warning into a green run.
let restoreDiagnostics = [ "NU1603"; "NU1608"; "NU1101"; "NU1102"; "NU1605" ]

type ProfileGraph =
    { Profile: string
      Pinned: (string * string) list
      /// Every FS.GG.* package in the restored graph, transitive included: id -> version.
      Resolved: (string * string) list }

/// Read `obj/project.assets.json` — the resolved graph a restore just wrote — and judge it.
let analyseGraph (dir: string) (i: Inputs) (pins: Pin list) (profile: string) : ProfileGraph * Failure list =
    let assets = Path.Combine(dir, "obj", "project.assets.json")
    if not (File.Exists assets) then
        raise (GuardError(sprintf "restore of the %s payload reported success but wrote no project.assets.json — cannot read the resolved graph" profile))

    // `libraries` keys are `id/version` for the WHOLE resolved graph, transitive included. That is
    // the object under test: a prerelease can enter a scaffolded product without ever appearing in
    // the props file (FS.GG.Game.Render reaches down to FS.GG.UI.Scene).
    let resolved =
        use doc = JsonDocument.Parse(File.ReadAllText assets)
        doc.RootElement.GetProperty("libraries").EnumerateObject()
        |> Seq.map (fun p -> p.Name)
        |> Seq.filter (fun k -> k.StartsWith("FS.GG.", StringComparison.Ordinal))
        |> Seq.map (fun k ->
            let idx = k.IndexOf '/'
            k.Substring(0, idx), k.Substring(idx + 1))
        |> Seq.sort
        |> Seq.toList

    if resolved.IsEmpty then
        raise (GuardError(sprintf "the %s payload restored zero FS.GG.* packages — a graph with nothing in it cannot prove the pins; fail closed" profile))

    let resolvedMap = Map.ofList resolved
    let failures =
        [ // A prerelease anywhere in the graph, transitive included.
          if not i.PreviewChannel then
              for (id, ver) in resolved do
                  if SemVer.isPrerelease ver then
                      let direct = pins |> List.exists (fun p -> p.Id = id)
                      yield
                          { Rule = "prerelease-in-scaffolded-graph"
                            Location = sprintf "profile=%s (%s)" profile (if direct then "pinned directly" else "transitive")
                            Expected = "a stable template scaffolds a stable graph"
                            Actual = sprintf "%s %s" id ver
                            Fix =
                              if direct then sprintf "re-pin %s onto a stable version in %s" id propsRel
                              else sprintf "%s is pulled in transitively — bump the direct pin whose graph reaches it, or wait for its stable release" id }

          // The resolved version of a directly pinned package must be the version written down.
          // NU1603 catches the upward substitution; this catches a downgrade or a graph-wide unification.
          for p in pins do
              match Map.tryFind p.Id resolvedMap with
              | Some v when v <> p.Version ->
                  yield
                      { Rule = "pin-resolved-elsewhere"
                        Location = sprintf "profile=%s (%s)" profile p.Id
                        Expected = sprintf "%s %s (as pinned through $(%s))" p.Id p.Version p.Axis
                        Actual = sprintf "%s %s" p.Id v
                        Fix = "the resolved graph unified this package onto another version — reconcile the pins" }
              | Some _ -> ()
              | None ->
                  yield
                      { Rule = "pin-resolved-elsewhere"
                        Location = sprintf "profile=%s (%s)" profile p.Id
                        Expected = sprintf "%s %s in the restored graph" p.Id p.Version
                        Actual = "absent"
                        Fix = "a pinned package that is not in the resolved graph means the restore did not do what the props file says" } ]

    { Profile = profile; Pinned = pins |> List.map (fun p -> p.Id, p.Version); Resolved = resolved }, failures

let restoreProfile (tmpRoot: string) (gpf: string) (i: Inputs) (profile: string) : ProfileGraph * Failure list =
    let pins = pinsFor i profile
    if pins.IsEmpty then raise (GuardError(sprintf "profile %s matched zero pins — refusing to report a vacuous pass" profile))
    let dir = Path.Combine(tmpRoot, profile)
    Directory.CreateDirectory dir |> ignore

    // `<clear />` so the probe resolves from nuget.org and nothing the machine happens to have
    // (issue #186's reasoning, applied to the probe). A scratch globalPackagesFolder keeps the
    // shared cache out of the blast radius.
    File.WriteAllText(
        Path.Combine(dir, "nuget.config"),
        sprintf
            "<configuration><config><add key=\"globalPackagesFolder\" value=\"%s\" /></config><packageSources><clear /><add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" protocolVersion=\"3\" /></packageSources><packageSourceMapping><packageSource key=\"nuget.org\"><package pattern=\"*\" /></packageSource></packageSourceMapping></configuration>"
            gpf)

    // A C# project, not F#: the F# SDK injects its bundled `FSharp/library-packs` folder as an
    // implicit restore source, whose FSharp.Core nupkg differs byte-for-byte from nuget.org's
    // (nuget.config's header records the same hazard). The TFM is what the payload restores against,
    // and it is the template's real one; FSharp.Core still arrives transitively through FS.GG.*.
    let packageRefs =
        pins
        |> List.map (fun p -> sprintf "    <PackageReference Include=\"%s\" Version=\"%s\" />" p.Id p.Version)
        |> String.concat "\n"
    File.WriteAllText(
        Path.Combine(dir, "Payload.csproj"),
        sprintf
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <OutputType>Library</OutputType>\n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>\n    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\n    <WarningsAsErrors>%s</WarningsAsErrors>\n  </PropertyGroup>\n  <ItemGroup>\n%s\n  </ItemGroup>\n</Project>\n"
            (String.concat ";" restoreDiagnostics)
            packageRefs)

    let ec, out = run dir "dotnet" [ "restore"; "Payload.csproj" ]
    let diagnosed = restoreDiagnostics |> List.filter (fun d -> out.Contains d)
    if ec <> 0 then
        if diagnosed.IsEmpty then
            // Not a pin defect — the tooling or the network failed. Exit 2, never a silent pass.
            raise (GuardError(sprintf "restore of the %s payload failed with no NuGet pin diagnostic — tooling/network, not the pins:\n%s" profile out))
        let failures =
            [ for d in diagnosed ->
                { Rule = "pin-does-not-resolve"
                  Location = sprintf "%s (profile=%s)" propsRel profile
                  Expected = "every pinned version resolves exactly as written"
                  Actual = sprintf "restore failed with %s" d
                  Fix = sprintf "correct the pin — %s means the requested version was substituted or does not exist; a scaffolded product would restore a graph you never declared" d } ]
        { Profile = profile; Pinned = pins |> List.map (fun p -> p.Id, p.Version); Resolved = [] }, failures
    else
        analyseGraph dir i pins profile

/// The FS.GG.UI.* pins that nuget.org does not carry at $(FsGgUiVersion). Empty is the normal state.
/// `feedVersions` is cached, so asking this costs nothing the feed layer was not already paying.
let unpublishedUiMembers (i: Inputs) : string list =
    let version = Map.find uiAxis i.AxisVersions
    i.Pins
    |> List.filter (fun p -> p.Axis = uiAxis)
    |> List.map (fun p -> p.Id)
    |> List.distinct
    |> List.filter (fun id -> not (feedVersions id |> List.contains version))

/// Is the FS.GG.UI.* set legitimately not on the feed yet — i.e. is this the release window?
///
/// Read the conjuncts in order; each one is load-bearing, and the ORDER is too. `pending` is empty in
/// the normal case, which short-circuits before the git call — so a shallow clone still runs the full
/// live proof whenever the pin is published, and `bumpedInCommitUnderTest` is only ever consulted when
/// the answer actually changes a verdict. See the header for why each conjunct is there.
let releasePending (pending: string list) =
    not pending.IsEmpty
    && not releaseLane
    && bumpedInCommitUnderTest propsRel uiAxis

/// The half of `prerelease-in-scaffolded-graph` that needs no graph.
///
/// RELEASE-PENDING skips the restore, and with it that rule — but the rule has two halves, and only one
/// of them needs a resolved graph. A TRANSITIVE prerelease (FS.GG.Game.Render reaching down to
/// FS.GG.UI.Scene) genuinely cannot be seen without restoring. A DIRECTLY PINNED prerelease is visible
/// in the axis literal itself, and dropping it through the window would let a stable release ship a
/// template pinning a prerelease $(FsGgAudioVersion), exit 0 — #235 in a new coat, waved through by the
/// very waiver that is supposed to be bounded. So the cheap half keeps running, window or not.
///
/// `PreviewChannel` is asserted by $(FsGgUiVersion) alone (see `readInputs`): on a preview template a
/// prerelease component is coherent, and this rule is correctly silent.
let directPrereleaseFailures (i: Inputs) : Failure list =
    if i.PreviewChannel then
        []
    else
        [ for p in i.Pins |> List.distinctBy (fun p -> p.Id) do
              if SemVer.isPrerelease p.Version then
                  yield
                      { Rule = "prerelease-in-scaffolded-graph"
                        Location = sprintf "%s ($(%s), pinned directly)" propsRel p.Axis
                        Expected = "a stable template scaffolds a stable graph"
                        Actual = sprintf "%s %s" p.Id p.Version
                        Fix =
                            sprintf
                                "re-pin $(%s) onto a stable version. The TRANSITIVE half of this rule needs the restore that RELEASE-PENDING skipped; a directly pinned prerelease needs no graph to see, so it is still checked"
                                p.Axis } ]

/// Existence on the feed, and (for the axes the feed owns) non-staleness. Both directions, like
/// `.github`'s check-feed-coherence.py: a pin BEHIND feed-newest is stale; a pin the feed does not
/// carry at all never resolves.
///
/// `waiveUi` suppresses `pin-not-published` for the $(FsGgUiVersion) axis ALONE, and only in the
/// release window (see `releasePending`). Game/Audio existence is never waived: this repo's bump does
/// not publish them, so an absent one is a real defect on any commit.
let feedFailures (waiveUi: bool) (i: Inputs) : Failure list =
    [ for KeyValue(axis, version) in i.AxisVersions do
        // Existence is asked of every pinned package on that axis, not of a representative: axis
        // members are published together, and one member missing at V is exactly the partial-graph
        // defect the version-coherence guard calls `restore-partial`.
        let members = i.Pins |> List.filter (fun p -> p.Axis = axis) |> List.map (fun p -> p.Id) |> List.distinct
        if not (waiveUi && axis = uiAxis) then
            for id in members do
                if not (feedVersions id |> List.contains version) then
                    yield
                        { Rule = "pin-not-published"
                          Location = sprintf "%s ($(%s) = %s)" propsRel axis version
                          Expected = sprintf "%s %s published on nuget.org" id version
                          Actual =
                            match (feedVersions id).Length with
                            | 0 -> sprintf "nuget.org carries no package named %s at all" id
                            | n -> sprintf "%s is not among the %d published versions of %s" version n id
                          Fix =
                            if axis = uiAxis then
                                sprintf "publish %s@%s, or re-pin $(%s) onto a version the feed carries. NOTE: this is NOT waived as RELEASE-PENDING, so this commit did not bump $(%s) — the pin is stale or typo'd, not mid-release" id version axis axis
                            else
                                sprintf "publish %s@%s from its OWN repo, or re-pin $(%s) onto a version the feed carries — bumping this axis here publishes nothing" id version axis }

        match axes |> List.tryPick (fun (a, rep) -> if a = axis then rep else None) with
        | None -> () // $(FsGgUiVersion): Feature 209 owns its staleness, against tags. See the header.
        | Some representative ->
            let newest = feedNewest i.PreviewChannel representative
            if SemVer.lt version newest then
                yield
                    { Rule = "pin-lags-feed"
                      Location = sprintf "%s ($(%s))" propsRel axis
                      Expected = sprintf "%s (newest %s on nuget.org)" newest (if i.PreviewChannel then "version" else "stable version")
                      Actual = version
                      Fix = sprintf "bump $(%s) to %s — a scaffolded product otherwise starts life on a stale component" axis newest } ]

// ---- reporting --------------------------------------------------------------------------------
let printDrift (failures: Failure list) =
    for f in failures do
        eprintfn "DRIFT [%s] %s" f.Rule f.Location
        eprintfn "  expected: %s" f.Expected
        eprintfn "  actual:   %s" f.Actual
        eprintfn "  fix:      %s" f.Fix

let writeStepSummary (title: string) (body: string seq) =
    match Environment.GetEnvironmentVariable "GITHUB_STEP_SUMMARY" with
    | null | "" -> ()
    | path ->
        let sb = Text.StringBuilder()
        sb.AppendLine(sprintf "### %s" title) |> ignore
        sb.AppendLine "" |> ignore
        for l in body do sb.AppendLine l |> ignore
        sb.AppendLine "" |> ignore
        File.AppendAllText(path, sb.ToString())

let summariseDrift (failures: Failure list) =
    writeStepSummary
        "Template payload pins — DRIFT"
        [ for f in failures ->
            sprintf "- `DRIFT [%s]` %s — expected `%s`; actual `%s` — fix: %s" f.Rule f.Location f.Expected f.Actual f.Fix ]

/// The release window, stated rather than merely survived. Says three things, and needs all three:
/// WHAT is pending, WHAT WAS NOT CHECKED because of it, and WHAT STILL WAS. A waiver that printed only
/// the first would be indistinguishable from a pass, which is the #266 failure this script exists to
/// refuse — the gate's own header forbids "nothing to check" and "checked, and it's fine" sharing an
/// exit code, and that bans them sharing an OUTPUT too.
///
/// It says CHECKED, never PROVED. This block is printed before the verdict is known — the Game/Audio
/// rules it names may be about to fail — so a word like "proved" would be a claim about a verdict this
/// function does not have. (The sibling guard learned the same lesson about the word "legal" on its own
/// RELEASE-PENDING line.) The exit code carries the verdict; these lines carry only the scope.
let printReleasePending (i: Inputs) (pending: string list) =
    let version = Map.find uiAxis i.AxisVersions
    printfn "RELEASE-PENDING: this commit bumps $(%s) to %s, and the %d FS.GG.UI.* package(s) it pins are not on nuget.org yet." uiAxis version pending.Length
    printfn "  That is the release window, not drift: merging this is what cuts fs-gg-ui/v%s and publishes them (release-tags.yml -> release.yml)." version
    printfn "  awaiting publish @ %s: %s" version (String.concat ", " pending)
    printfn "  NOT CHECKED (a graph cannot restore against packages that do not exist yet):"
    printfn "    - the resolved graph of all %d scaffold profiles — the TRANSITIVE half of `prerelease-in-scaffolded-graph`, and `pin-resolved-elsewhere`" profiles.Length
    printfn "  STILL CHECKED (this bump does not publish these, so the window excuses nothing about them):"
    printfn "    - the structural verdict-core"
    printfn "    - $(FsGgGameVersion) / $(FsGgAudioVersion) existence + staleness"
    printfn "    - the DIRECT half of `prerelease-in-scaffolded-graph` — a pinned prerelease needs no graph to see"
    printfn "  Those still decide the exit code: a failure in any of them reds this run, waiver or no waiver."
    printfn "  If the publish never lands, the next commit to main does not bump the pin, the waiver is OFF, and this gate reds on `pin-not-published`."
    writeStepSummary
        "Template payload pins — RELEASE-PENDING"
        [ sprintf "This commit bumps `$(%s)` to **%s**, whose FS.GG.UI.* packages are not published yet. That is the release window — merging is what publishes them — so `pin-not-published` / `pin-does-not-resolve` are **waived on that axis alone**." uiAxis version
          ""
          sprintf "- **awaiting publish @ %s:** %s" version (String.concat ", " pending)
          sprintf "- **not checked:** the resolved graph of all %d scaffold profiles — the transitive half of `prerelease-in-scaffolded-graph`, and `pin-resolved-elsewhere`. Skipped, not passed." profiles.Length
          "- **still checked:** the structural verdict-core; `$(FsGgGameVersion)` / `$(FsGgAudioVersion)` existence + staleness; and the direct half of `prerelease-in-scaffolded-graph`. A failure in any of these still reds the run."
          ""
          "If the publish never lands, the next commit to `main` reds this gate on `pin-not-published`." ]

// ---- main -------------------------------------------------------------------------------------
let main () =
    semverSelfCheck ()
    let i = readInputs ()
    let axisLine =
        i.AxisVersions |> Map.toList |> List.map (fun (a, v) -> sprintf "$(%s)=%s" a v) |> String.concat " · "
    printfn "template payload: %s%s" axisLine (if i.PreviewChannel then " (PREVIEW CHANNEL)" else "")

    if not live then
        let failures = structuralFailures i
        if failures.IsEmpty then
            printfn
                "template payload pins: STRUCTURAL OK — %d FS.GG.* pins, all axis-derived; %d profiles each restore >= 1. Run with FS_GG_RUN_TEMPLATE_PAYLOAD_RESTORE=1 to prove the resolved graph."
                i.Pins.Length
                profiles.Length
            0
        else
            printDrift failures
            summariseDrift failures
            eprintfn "template payload pins: DRIFT — %d failure(s)" failures.Length
            1
    else
        let structural = structuralFailures i
        // A bare-literal or empty-profile pin set makes the restore layer's subject wrong, so stop
        // here rather than proving a graph the props file does not describe.
        if not structural.IsEmpty then
            printDrift structural
            summariseDrift structural
            eprintfn "template payload pins: DRIFT — %d structural failure(s); restore proof not attempted" structural.Length
            1
        else
            let tmpRoot = Path.Combine(Path.GetTempPath(), "tpp241-" + Guid.NewGuid().ToString("N").Substring(0, 8))
            let gpf = Path.Combine(tmpRoot, "gpf")
            Directory.CreateDirectory gpf |> ignore
            try
                // Ask the feed FIRST, and let the answer decide whether a restore is even meaningful.
                // In the release window every profile's restore would NU1102 on the unpublished
                // FS.GG.UI.* pins — five identical reds that say nothing the RELEASE-PENDING block does
                // not say better. Skip them, and SAY they were skipped.
                let pending = unpublishedUiMembers i
                let waiveUi = releasePending pending
                let feed = feedFailures waiveUi i

                if waiveUi then
                    printReleasePending i pending
                    // The waiver skips the RESTORE, not the rules the restore is not needed for. The
                    // direct half of `prerelease-in-scaffolded-graph` reads the axis literals, so the
                    // window is no excuse for dropping it — see `directPrereleaseFailures`.
                    let stillChecked = feed @ directPrereleaseFailures i
                    if stillChecked.IsEmpty then
                        // Deliberately NOT "COHERENT": the graph was not proved, and a success line that
                        // said so would be the lie the header forbids. The exit code says "not drift";
                        // the words say exactly what was and was not established.
                        printfn "template payload pins: RELEASE-PENDING — structural core OK, Game/Audio axes OK, no directly pinned prerelease; restore proof deferred to the publish."
                        0
                    else
                        // The waiver covers $(FsGgUiVersion)'s PUBLICATION and nothing else, so anything
                        // left here is a defect the release window does not excuse.
                        printDrift stillChecked
                        summariseDrift stillChecked
                        eprintfn "template payload pins: DRIFT — %d failure(s) the release window does not excuse (RELEASE-PENDING waives only the publication of $(%s))" stillChecked.Length uiAxis
                        1
                else
                    let graphs, restoreFailures =
                        profiles
                        |> List.map (restoreProfile tmpRoot gpf i)
                        |> List.unzip
                    let failures = feed @ List.concat restoreFailures

                    let allResolved = graphs |> List.collect (fun g -> g.Resolved) |> List.distinct |> List.sort
                    let prereleases = allResolved |> List.filter (snd >> SemVer.isPrerelease)
                    for g in graphs do
                        printfn "  %-14s %d pinned · %d resolved FS.GG.* (transitive incl.)" g.Profile g.Pinned.Length g.Resolved.Length
                    printfn
                        "total distinct FS.GG.* resolved across %d profiles: %d · prerelease: %s"
                        profiles.Length
                        allResolved.Length
                        (if prereleases.IsEmpty then "NONE" else prereleases |> List.map (fun (id, v) -> sprintf "%s %s" id v) |> String.concat ", ")

                    if failures.IsEmpty then
                        printfn "template payload pins: COHERENT (structural + restore-grounded)."
                        writeStepSummary
                            "Template payload pins — coherent"
                            [ sprintf "- axes: %s" axisLine
                              sprintf "- profiles proved: %s" (String.concat ", " profiles)
                              sprintf "- distinct `FS.GG.*` resolved (transitive incl.): **%d** · prerelease: **NONE**" allResolved.Length ]
                        0
                    else
                        printDrift failures
                        summariseDrift failures
                        eprintfn "template payload pins: DRIFT — %d failure(s)" failures.Length
                        1
            finally
                try Directory.Delete(tmpRoot, true) with _ -> ()

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
