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
let private releaseWindowSource = File.ReadAllText(repo "scripts/lib/ReleaseWindow.fsx")
let private apiMirrorSource = File.ReadAllText(repo "scripts/refresh-api-surface-mirror.fsx")
let private gateSource = File.ReadAllText(repo ".github/workflows/gate.yml")

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
///
/// THE FEED LANE IS SCRUBBED HERE TOO (#718), for the same reason and with a sharper edge.
/// `FS_GG_RUN_VERSION_COHERENCE_FEED=1` is what makes the guard talk to a package feed, and THIS SUITE
/// RUNS INSIDE THE REQUIRED `Deterministic gate`. A guard spawned from here that inherited that flag from
/// an ambient job environment would put a live network dependency inside the required gate — which is the
/// exact thing ADR-0105 forbids, and the exact thing the feed lane's own NON-required job exists to avoid.
/// The three feed variables are removed at the choke point so no test can acquire one by accident; the
/// feed tests below opt IN explicitly, through `env`, and always against a loopback fixture.
let private runInWithEnv (workDir: string) (env: (string * string) list) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.Environment.Remove "FS_GG_VERSION_COHERENCE_RELEASE_LANE" |> ignore
    psi.Environment.Remove "FS_GG_RUN_VERSION_COHERENCE_FEED" |> ignore
    psi.Environment.Remove "FS_GG_VERSION_COHERENCE_FEED_URL" |> ignore
    psi.Environment.Remove "FS_GG_VERSION_COHERENCE_PUBLISH_GRACE_MIN" |> ignore
    // A PR-base binding belongs to the real checkout only. Synthetic fixture repositories own their
    // own ancestry and must exercise the guard's HEAD~1 fallback rather than inherit an unrelated SHA.
    if not (Path.GetFullPath(workDir).Equals(Path.GetFullPath(root), StringComparison.Ordinal)) then
        psi.Environment.Remove "FS_GG_VERSION_COHERENCE_BASE_SHA" |> ignore
    for (k, v) in env do
        psi.Environment.[k] <- v
    args |> List.iter psi.ArgumentList.Add
    match Process.Start psi with
    | null -> failwithf "%s could not be started" exe
    | p ->
        use p = p
        // DRAIN BOTH PIPES AT ONCE. Reading stdout to the end and only THEN reading stderr deadlocks the
        // moment a child writes more to stderr than the pipe buffer holds (~64 KB): the child blocks
        // writing stderr, so it never exits, so it never closes stdout, so `ReadToEnd` on stdout never
        // returns, so stderr is never drained. Nothing times out — the run simply hangs forever.
        //
        // This was latent here for as long as the helper has existed, and the feed lane is the first thing
        // loud enough to reach it: a phantom release emits an 18-package DRIFT block (four lines each) on
        // stderr, and the suite hung with the child parked in `anon_pipe_write` while every test reported
        // passed. Start both reads before waiting, and neither can starve the other.
        let out = p.StandardOutput.ReadToEndAsync()
        let err = p.StandardError.ReadToEndAsync()
        p.WaitForExit()
        p.ExitCode, out.Result + err.Result

let private runIn (workDir: string) (exe: string) (args: string list) = runInWithEnv workDir [] exe args

// ---- the feed lane's fixture (#718) -----------------------------------------------------------
//
// ONE loopback flat-container feed for the whole suite. `arm` decides what it SAYS, so every arm of the
// guard's four-state table can be driven WITHOUT A NETWORK — which is not a convenience. These tests run
// inside the REQUIRED `Deterministic gate`, and reaching nuget.org from there would take a hard dependency
// on that feed's uptime for every merge in the repo: the precise failure ADR-0105 forbids, and the reason
// the feed lane itself is a non-required job. A test for a feed check must not become the feed dependency
// the check was designed to keep out of the required set.
//
// ONE LISTENER, NOT ONE PER TEST, and that is load-bearing. Standing an HttpListener up and tearing it down
// around each test — while that same test spawns a child process that calls back INTO it — makes the suite
// depend on bind/dispose ordering and on the OS not recycling a just-freed port into the next test. It does
// not have to: the tests are sequenced, so a single long-lived listener with a swappable responder is
// enough, and it deletes the whole class. (Observed with per-test listeners: each feed test passed in
// isolation, and the second one hung forever when run after the first.)
//
// The serve threads are OWNED, never borrowed from the thread pool. Expecto's test threads ARE pool
// threads, and each feed test blocks one reading the child guard's stdout until that child exits — while
// the child waits on this fixture to answer. Serving from the pool would need a thread the blocked test is
// holding, and the run deadlocks with every test reported as passed and the host never exiting.
//
// It also records every path it was asked for, which is what lets `the verdict-core does not touch the
// network` assert ZERO requests rather than merely asserting a green exit.
module private FakeFeed =
    let private seen = Collections.Concurrent.ConcurrentQueue<string>()
    let mutable private respond: string -> int * string = fun _ -> 404, ""

    let private baseUrl =
        lazy
            (let listener = new System.Net.HttpListener()

             let mutable bound = ""
             let mutable attempt = 0
             while bound = "" && attempt < 16 do
                 attempt <- attempt + 1
                 // Ask the OS for a free loopback port, then hand it to HttpListener. Retried: the port is
                 // released before HttpListener claims it, so another process can take it in between.
                 let probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0)
                 probe.Start()
                 let port = (probe.LocalEndpoint :?> System.Net.IPEndPoint).Port
                 probe.Stop()
                 let candidate = sprintf "http://127.0.0.1:%d/" port
                 listener.Prefixes.Clear()
                 listener.Prefixes.Add candidate
                 try
                     listener.Start()
                     bound <- candidate
                 with _ ->
                     ()
             if bound = "" then
                 failwith "FakeFeed: could not bind a loopback port"

             let serve () =
                 while listener.IsListening do
                     try
                         let ctx = listener.GetContext()
                         let path =
                             match ctx.Request.Url with
                             | null -> "/"
                             | url -> url.AbsolutePath
                         seen.Enqueue path
                         let status, body = respond path
                         ctx.Response.StatusCode <- status
                         let bytes = Text.Encoding.UTF8.GetBytes body
                         ctx.Response.ContentLength64 <- int64 bytes.Length
                         ctx.Response.OutputStream.Write(bytes, 0, bytes.Length)
                         ctx.Response.OutputStream.Close()
                     with _ ->
                         ()

             // The guard probes the whole coherent set concurrently, so answer concurrently.
             for _ in 1..4 do
                 let t = System.Threading.Thread(serve)
                 t.IsBackground <- true
                 t.Start()

             bound.TrimEnd '/')

    /// Point the feed at a new answer and forget what was asked before. Returns the base URL to hand the
    /// guard via FS_GG_VERSION_COHERENCE_FEED_URL. Safe because the feed tests are `testSequenced`.
    let arm (answer: string -> int * string) =
        let url = baseUrl.Value
        respond <- answer
        seen.Clear()
        url

    /// Every flat-container path the guard actually asked for since the last `arm`.
    let requests () = seen.ToArray() |> List.ofArray

/// A flat-container index carrying exactly `versions` — the shape nuget.org serves.
let private flatContainer (versions: string list) =
    200, sprintf """{"versions":[%s]}""" (String.Join(",", versions |> List.map (sprintf "\"%s\"")))

/// The versions `main` currently names. The guard probes exactly these, so the fixture must answer about
/// them — read from the repo rather than spelled, so a release cannot rot the test.
let private currentPin =
    Regex
        .Match(File.ReadAllText(repo "template/base/Directory.Packages.props"), "<FsGgUiVersion>([^<]+)</FsGgUiVersion>")
        .Groups.[1]
        .Value.Trim()

let private currentPkgVersion =
    Regex
        .Match(File.ReadAllText(repo ".template.package/FS.GG.UI.Template.fsproj"), "<Version>([^<]+)</Version>")
        .Groups.[1]
        .Value.Trim()

/// THE FEED LANE'S FIXTURES OWN THEIR TAG WORLD — AND UNTIL #587 THEY BORROWED THE REPO'S.
///
/// The lane under test asks exactly one question: for a tag that IS cut, are that release's packages really
/// on the feed? So every arm below — phantom, publish-in-flight, feed-down, garbage-200 — needs a cut tag
/// naming the CURRENT pin before it has anything to probe. Run against the real repo, that holds on every
/// commit but one: THE RELEASE COMMIT ITSELF, where the pin is bumped and its tags are not cut yet (they are
/// cut from the merge commit, by `release-tags.yml` — merging IS the publish). There the guard is correct and
/// says so —
///
///     feed lane: probed 0 package(s) ... no cut tag names the current pin/package
///
/// — and the fixtures were not: four asserted on arms that cannot fire, and the fifth ("a cut tag whose
/// packages ARE on the feed") went GREEN on `probed 0`, which is the vacuous pass its own comment warns
/// about. This suite is in the REQUIRED `Deterministic gate`, so that is not noise — it wedges the merge
/// button on the one commit whose whole job is to be merged, exactly as the missing `RELEASE-PENDING` waiver
/// wedged `TemplateConsumesPinnedApiTests` (#673) and the 0.9.1 cut before it. 0.10.0 is the first release
/// since the feed lane (#718) shipped, which is why nothing caught it sooner.
///
/// The deferral idiom those wedges taught is the WRONG fix here: a fixture that skips on a release commit
/// drops the lane's teeth on the only commit whose release state it exists to police. So the fixtures own
/// their world instead — this file's own rule, four lines up from where it was not applied. A throwaway
/// clone, with the pin's tag triple cut at HEAD IF IT IS NOT ALREADY THERE:
///
///   * ordinary commit — the tags exist, pointing at the real release commit. Nothing is created, and the
///     fixtures see precisely the world they see today.
///   * release commit  — the tags are cut at HEAD, which is the state this very commit is about to create.
///     Every arm fires, and the lane keeps its teeth where they matter most.
///
/// ONE CLONE, AT A STABLE PATH, SWEPT ON THE WAY IN — NOT A FRESH GUID EACH RUN. A `Guid` name plus an
/// `AppDomain.ProcessExit` cleanup looks tidy and leaks: **`ProcessExit` does not run under the VSTest
/// host**, and its ~2s budget would not delete a repo-sized tree if it did. Measured, before this was
/// changed: seven `vcoh-released-*` trees and 193 MB in `/tmp`, one per `dotnet test` run, with no path that
/// ever removed them. The clone is a pure function of (root, HEAD, the two version files), so it does not
/// need to be unique — it needs to be FRESH. A path derived from `root` gives exactly one per checkout, and
/// deleting it on the way in makes a crashed run self-healing rather than sticky.
let private releasedRoot =
    lazy
        (let tmp =
            Path.Combine(
                Path.GetTempPath(),
                "vcoh-released-"
                + (Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(Text.Encoding.UTF8.GetBytes root))).Substring(0, 12))

         if Directory.Exists tmp then
             Directory.Delete(tmp, true)

         let ec, out = runIn root "git" [ "clone"; "--no-hardlinks"; "--quiet"; root; tmp ]

         if ec <> 0 then
             failwithf "feed-lane fixture: could not clone the repo to a throwaway root:\n%s" out

         // THE CLONE CARRIES THE FILES UNDER TEST, NOT HEAD'S COPIES OF THEM (#478). `git clone` carries
         // COMMITTED HEAD, but `currentPin`/`currentPkgVersion` above are read from the WORKING TREE — so
         // without these copies the fixture straddles the two, and an UNCOMMITTED pin bump makes it cut a tag
         // for a version the clone's own props do not name. The guard then reports a bogus pin/tag skew and
         // every arm below fails for a reason that has nothing to do with the lane. CI never sees it (the
         // checkout is clean); the developer who just edited the pin sees nothing else. The guard goes with
         // them, for the same reason one step further on: a fixture that exercises the COMMITTED script could
         // never fail on an edit to it.
         for rel in
             [ "scripts/validate-version-coherence.fsx"
               "template/base/Directory.Packages.props"
               ".template.package/FS.GG.UI.Template.fsproj" ] do
             File.Copy(repo rel, Path.Combine(tmp, rel.Replace('/', Path.DirectorySeparatorChar)), true)

         for tag in
             [ $"fs-gg-ui/v{currentPin}"
               $"fs-gg-ui-template/v{currentPkgVersion}"
               $"v{currentPkgVersion}" ] do
             let exists, _ = runIn tmp "git" [ "rev-parse"; "--verify"; "--quiet"; $"refs/tags/{tag}" ]

             if exists <> 0 then
                 let ec, out = runIn tmp "git" [ "tag"; tag; "HEAD" ]

                 if ec <> 0 then
                     failwithf "feed-lane fixture: could not cut %s in the throwaway clone:\n%s" tag out

         tmp)

/// Run the real guard against a repo whose pin IS released (see `releasedRoot`), with the feed lane pointed
/// at `feed`.
///
/// `grace` is pinned by every caller rather than left to default, and that is what makes these tests
/// DETERMINISTIC. The guard waives an absent package while its tag is younger than the grace (a publish
/// takes ~25 minutes), so a test that inherited the 60-minute default would pass or fail on HOW LONG AGO
/// THE LAST RELEASE HAPPENED — green on a normal day, and mysteriously red for anyone running the suite in
/// the hour after a release. Grace 0 means "any absent package is a phantom"; a huge grace means "every
/// tag is young". Both are the arm under test, neither is the clock.
let private runFeedLane (feedUrl: string) (graceMin: string) =
    runInWithEnv
        releasedRoot.Value
        [ "FS_GG_RUN_VERSION_COHERENCE_FEED", "1"
          "FS_GG_VERSION_COHERENCE_FEED_URL", feedUrl
          "FS_GG_VERSION_COHERENCE_PUBLISH_GRACE_MIN", graceMin ]
        "dotnet"
        [ "fsi"; Path.Combine("scripts", "validate-version-coherence.fsx") ]

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
/// the bump — so "this version already has a tag" is unsatisfiable on the bump itself. Exact-head PR
/// checkouts supply the immutable PR base; push/main falls back to `HEAD~1`.
///
/// Compares VALUES, not touched lines: this predicate waives a fail-closed assertion, so a reindent of
/// the `<Version>` line must not silence it.
let private bumpedInCommitUnderTest (rel: string) (element: string) =
    let baseRevision =
        match Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_BASE_SHA" with
        | null | "" -> "HEAD~1"
        | value when Regex.IsMatch(value, "^[0-9a-f]{40}$") -> value
        | value -> failwithf "FS_GG_VERSION_COHERENCE_BASE_SHA must be a full lowercase git SHA, got %s" value
    let psi = ProcessStartInfo("git")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    [ "diff"; baseRevision; "HEAD"; "--unified=0"; "--"; rel ] |> List.iter psi.ArgumentList.Add
    let ec, out =
        match Process.Start psi with
        | null -> failwith "git diff could not be started"
        | p ->
            use p = p
            let o = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            p.ExitCode, o
    if ec <> 0 then
        failwithf "git diff %s HEAD -- %s failed — need full history (fetch-depth: 0); fail closed" baseRevision rel
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

        test "exact-head release window binds the PR base and consumes the packed local feed" {
            Expect.stringContains
                releaseWindowSource
                "FS_GG_VERSION_COHERENCE_BASE_SHA"
                "the API-mirror release classifier must share the exact-head PR-base input"

            Expect.stringContains
                releaseWindowSource
                "\"rev-parse\"; \"--verify\"; baseRevision + \"^{commit}\""
                "the release classifier must reject an unresolvable explicit base"

            Expect.stringContains
                apiMirrorSource
                "FS_GG_PRODUCT_LOCAL_FEED is required during the exact-head release window"
                "an unpublished pin must fail closed when the exact-head packed feed is absent"

            Expect.stringContains
                gateSource
                "FS_GG_PRODUCT_LOCAL_FEED: ${{ steps.package-tests-feed.outputs.feed }}"
                "the API-surface step must receive the feed packed earlier in the same required job"
        }

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

                // Deliberately a version no real release will ever cut. The clone carries the repo's real
                // tags, and this test CREATES the triple for `next` — so deriving it from the pin (0.9.0 ->
                // 0.10.0) would plant a time bomb that goes off the day the repo actually ships that
                // version and `git tag` finds it already there. It only has to sort above every existing
                // tag, which is all the guard's ordering rules ask of a pin.
                let next = "999.0.0"

                // Each file carries ITS OWN version literal, and after a TEMPLATE-ONLY release (#956 — pin
                // held, <Version> bumped) they are NOT equal: the two framework-pinned files carry
                // `pinVersion`, while `.template.package/FS.GG.UI.Template.fsproj` carries the package
                // `<Version>` = `pkgVersion`, which has already moved off the pin. Replacing `pinVersion`
                // uniformly (the old code) left the fsproj at `pkgVersion` — pin=next but pkg unchanged —
                // which is `pin-leads-package` DRIFT, a fixture artifact, not the subject. Replace each file
                // by its own literal so the synthetic release is a coherent lockstep bump (pin == pkg == next)
                // regardless of any pre-existing skew. When pin == pkg (a lockstep repo) this is identical to
                // the old behaviour.
                for (rel, currentLit) in
                    [ "template/base/Directory.Packages.props", pinVersion
                      ".template.package/FS.GG.UI.Template.fsproj", pkgVersion
                      "template/product-skills/fs-gg-symbology/reference.fsx", pinVersion ] do
                    let path = Path.Combine(tmp, rel.Replace('/', Path.DirectorySeparatorChar))
                    File.WriteAllText(path, File.ReadAllText(path).Replace(currentLit, next))

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

        // ---- the feed lane (#718, epic #693) --------------------------------------------------
        //
        // SEQUENCED, and not for tidiness. Each of these spawns a real `dotnet fsi` guard and blocks the
        // test's thread reading its stdout, while a loopback fixture answers that child's HTTP. Expecto's
        // test threads are THREAD-POOL threads, so running these in parallel puts several blocked-on-a-
        // child threads and the sockets those children are waiting on into the same pool — and the pool
        // runs out. Every test then reports PASSED and the host never exits (observed: a 27-minute hang).
        // `FakeFeed` owns its serve threads so it can never be the starved party; sequencing these is the
        // other half, and it is what keeps the wall clock honest rather than the run merely lucky.
        testSequenced (
            testList
                "feed lane — does the release the tag promises actually exist?"
                [
            // ---- the feed lane (#718, epic #693) --------------------------------------------------
            //
            // THE BUG THESE PIN DOWN. Every other rule in this guard reads the TAG NAMESPACE, and
            // `pin == latest tag` is EXACTLY what a release that worked looks like — and exactly what a
            // release that was tagged and never published looks like. So the guard printed
            //
            //     version coherence: COHERENT (structural verdict-core). pin 0.9.1 == latest tag
            //
            // over a repo whose 0.9.1 packages did not exist, emitted no RELEASE-PENDING block, and therefore
            // gave `release-tags.yml` nothing to cut and `release.yml` no reason to run. 0.9.1 was never
            // published, no automation could reach the state, and a human unwedged it by hand (#679).
            //
            // The four states below are the whole check, and the two that look like bugs are the load-bearing
            // ones: a publish IN FLIGHT and a feed that DID NOT ANSWER must both stay green. A check that
            // reddens on either is a check that is red for reasons nobody can act on, and this file already
            // records what that does — `pkg-lags-template-tag` "went red on every release PR (#155, #159, #163)
            // and was merged past each time", which is how the repo learned to merge past a red version gate.
            test "feed lane: a cut tag whose packages ARE on the feed is a real release" {
                let feedUrl = FakeFeed.arm (fun _ -> flatContainer [ currentPin; currentPkgVersion ])
                let ec, out = runFeedLane feedUrl "0"

                Expect.equal ec 0 (sprintf "every package the cut tags promise is on the feed — that is a real release:\n%s" out)
                Expect.isFalse (out.Contains "release-phantom")
                    (sprintf "a published release must never be reported as a phantom — a rule that fires on everything is as useless as one that fires on nothing:\n%s" out)
                Expect.stringContains out "feed lane: probed"
                    "the lane must say how many packages it probed, so a run that checked nothing cannot read as a pass"

                // AND THE COUNT MUST BE NON-ZERO — the line above only checks that the lane SAID it. `probed 0
                // package(s)` satisfies every other assertion in this test (exit 0, no phantom, the word
                // "probed"), so without this the fixture goes GREEN having checked nothing. That is not
                // hypothetical: it is what this test did on every release commit until `releasedRoot` gave these
                // fixtures their own tags, while its four siblings went red. "Checked nothing" and "everything is
                // published" must not share an observable (FS-GG/.github#266) — and this assertion is also what
                // makes `releasedRoot` self-guarding: if it ever stops cutting the triple, this reds instead of
                // quietly passing.
                Expect.isMatch out @"feed lane: probed [1-9]\d* package\(s\)"
                    (sprintf "the lane probed NOTHING, and still reported success — the fixture is vacuous:\n%s" out)
            }

            test "feed lane: a cut tag with NO package behind it is the #679 phantom, not coherence" {
                // The feed ANSWERS, and says the package does not exist. That is the whole difference between
                // this and an outage, and it is the only state that may be called drift.
                let feedUrl = FakeFeed.arm (fun _ -> 404, "")
                let ec, out = runFeedLane feedUrl "0"

                Expect.equal ec 1 (sprintf "a tag with nothing behind it is DRIFT (exit 1), not coherence — this is #679:\n%s" out)
                Expect.stringContains out "release-phantom" "the failure must name the rule"
                Expect.stringContains out (sprintf "fs-gg-ui/v%s" currentPin)
                    "the failure must name the TAG — it is the thing a human must either honour (re-run the release) or retract"

                // The verdict-core, on this very same repo and tag set, is perfectly happy. That is the point:
                // the offline half CANNOT see this, which is why the feed lane exists and why it may not live
                // inside it.
                let coreEc, _ = runIn root "dotnet" [ "fsi"; repo "scripts/validate-version-coherence.fsx" ]
                Expect.equal coreEc 0 "the structural verdict-core is blind to a phantom release by construction — it never asks the feed"
            }

            test "feed lane: a publish still IN FLIGHT is not a phantom" {
                // `release-tags.yml` pushes the tag triple and only THEN calls `release.yml`, whose tests +
                // publish take ~25 min, after which nuget.org takes minutes more to index. For that window a cut
                // tag legitimately has no package behind it. This repo merges continuously, so a check without
                // the grace would red on every PR landing in that window — and a gate that is red whenever a
                // release happens is a gate people learn to merge past.
                let feedUrl = FakeFeed.arm (fun _ -> 404, "")
                let ec, out = runFeedLane feedUrl "99999999"

                Expect.equal ec 0 (sprintf "a tag younger than the grace has its publish in flight, not abandoned:\n%s" out)
                Expect.isFalse (out.Contains "release-phantom")
                    (sprintf "an in-flight publish must never be reported as a phantom — that is the false red that kills the gate:\n%s" out)
                Expect.stringContains out "PUBLISH-IN-FLIGHT"
                    "and it must still be SAID — silence and 'all published' must not share an observable"
            }

            test "feed lane: a feed that does not answer is never drift, and never a silent pass" {
                // THE DIRECTION THAT MATTERS. If a silent feed could redden this repo, a nuget.org outage would
                // announce that every release here is a phantom — and were this check ever required, it would
                // wedge every merge in the repo behind someone else's uptime (ADR-0105). Exit 0 is mandatory.
                //
                // But exit 0 must not be a PASS either: "the feed did not answer" and "every release is real"
                // must not produce the same observable (#216 — a check that could not run never reports a pass).
                // `apicompat-check.sh` draws this exact line, with the exact same split.
                let feedUrl = FakeFeed.arm (fun _ -> 503, "upstream unavailable")
                let ec, out = runFeedLane feedUrl "0"

                Expect.equal ec 0 (sprintf "a feed that did not answer says NOTHING about this repo and must not fail it:\n%s" out)
                Expect.isFalse (out.Contains "release-phantom")
                    (sprintf "an outage is not a phantom — collapsing the two is how a feed outage reads as 'every release is abandoned':\n%s" out)
                Expect.stringContains out "did not answer"
                    "the run must say it could not check, loudly — a silent exit 0 here is green-by-absence"
                Expect.stringContains out "NOTHING was compared"
                    "and the SUCCESS LINE must say it too: 'every cut tag has its packages' after 18 failed probes is a claim the guard never checked"
            }

            test "feed lane: a 200 that is not a flat-container index is the feed NOT answering, not a phantom" {
                // A proxy error page, a captive portal, or a CDN interstitial answers 200 with HTML. The version
                // regex then matches nothing, which reads as an EMPTY version list — "the package is not
                // published" — and every package becomes a phantom. That inverts the whole safety argument at
                // the one moment it matters: a MALFUNCTIONING feed would redden the repo, which is exactly what
                // the Unavailable arm exists to prevent (ADR-0105).
                //
                // Measured before the fix: `<html>503 via proxy</html>` with status 200 reported all 18 packages
                // as phantoms and exited 1.
                let feedUrl = FakeFeed.arm (fun _ -> 200, "<html><body>503 Service Unavailable</body></html>")
                let ec, out = runFeedLane feedUrl "0"

                Expect.equal ec 0 (sprintf "a 200 that is not the document we asked for is the feed failing to answer, not a phantom release:\n%s" out)
                Expect.isFalse (out.Contains "release-phantom")
                    (sprintf "a proxy error page must NEVER be reported as an abandoned release:\n%s" out)
                Expect.stringContains out "not a flat-container index"
                    "and it must say WHY it could not check — 'the feed answered' and 'the feed answered with garbage' are different findings"
            }

            // THE PROMISED SET IS THE TAG'S, NOT HEAD'S.
            //
            // A tag promises the coherent set as it stood AT THE COMMIT IT POINTS AT. Probing today's member
            // list against a historical tag's version means every member added SINCE that release is absent from
            // the feed — so adding a packable member reports a `release-phantom` against a release that was
            // perfectly fine, and keeps reporting it until the next release. And nothing makes that commit
            // illegal: the verdict-core exits 0 on it (asserted below), because nothing in this repo requires a
            // pin bump to land a new member.
            //
            // That red would be byte-identical to a genuine phantom, so it would camouflage the very defect this
            // layer exists to catch — the #506 mistake, one gate over. The fix is `membersPromisedBy`, which
            // reads the BOM AT THE TAG; this test is what stops it regressing to HEAD.
            test "feed lane: a member added AFTER the release tag is not a phantom of that release" {
                let tmp = Path.Combine(Path.GetTempPath(), "vcoh718-newmember-" + Guid.NewGuid().ToString("N").Substring(0, 8))
                try
                    // A real clone of the RELEASED root, so the tags — and `git show <tag>:…` — are real. Cloned
                    // from `releasedRoot` rather than `root` for the reason given there: on a release commit the
                    // pin's tags are not cut yet, and this fixture's whole subject is "a member landed AFTER the
                    // pin's tag was cut" — which needs that tag to exist.
                    //
                    // TWO WORKING DIRECTORIES, AND THEY ARE NOT INTERCHANGEABLE. The clone is invoked from
                    // `root`; everything after it must run in `tmp`. Binding one `git` helper to `root` and
                    // using it for both made the fixture's `commit -am` land IN THE REPOSITORY UNDER TEST —
                    // it committed the developer's working tree (and, in CI, would commit the checkout)
                    // under the fixture's own commit message. A test may not write to the repo it is
                    // reading. The two helpers are named apart so the next reader cannot make that mistake
                    // by reaching for the shorter one.
                    let gitInRepo (args: string list) = runIn root "git" args
                    let gitInClone (args: string list) = runIn tmp "git" args

                    let ec, out = gitInRepo [ "clone"; "--no-hardlinks"; "--quiet"; releasedRoot.Value; tmp ]

                    Expect.equal
                        ec
                        0
                        (sprintf "clone the released repo (with the pin's tag triple) to a throwaway root:\n%s" out)

                    // THE CLONE CARRIES THE COMMITTED SCRIPT, NOT THE ONE UNDER TEST. Without this copy the
                    // fixture exercises whatever is at HEAD, so it would pass no matter what the working tree
                    // says — a test that cannot fail, which is the exact defect (#478) this suite exists to
                    // prevent. Copy the guard as it stands now.
                    File.Copy(
                        repo "scripts/validate-version-coherence.fsx",
                        Path.Combine(tmp, "scripts", "validate-version-coherence.fsx"),
                        true)

                    // A new packable member, landed after the current pin's tag was cut. It must ALSO go in the
                    // BOM or `bom-member-skew` fires — which is the point: this is the ONLY legal shape of the
                    // change, and it is structurally coherent.
                    let projDir = Path.Combine(tmp, "src", "BrandNew")
                    Directory.CreateDirectory projDir |> ignore
                    File.WriteAllText(
                        Path.Combine(projDir, "FS.GG.UI.BrandNew.fsproj"),
                        "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><PackageId>FS.GG.UI.BrandNew</PackageId><IsPackable>true</IsPackable></PropertyGroup></Project>")

                    let nuspecPath = Path.Combine(tmp, "src", "Meta", "FS.GG.UI.nuspec")
                    let nuspec = File.ReadAllText nuspecPath
                    let anchor = "<dependency id=\"FS.GG.UI.Scene\" version=\"[$version$]\" />"
                    Expect.stringContains nuspec anchor "the BOM must carry FS.GG.UI.Scene — this test hangs its new entry off it"
                    File.WriteAllText(
                        nuspecPath,
                        nuspec.Replace(anchor, anchor + "\n      <dependency id=\"FS.GG.UI.BrandNew\" version=\"[$version$]\" />"))

                    // In the CLONE. `-am` stages every modified tracked file, so pointing this at `root`
                    // commits whatever the developer happens to be working on.
                    let ec, out =
                        gitInClone [ "-c"; "user.email=fixture@fsgg"; "-c"; "user.name=fixture"
                                     "commit"; "-am"; "feat: a new packable member, no pin bump" ]
                    Expect.equal ec 0 (sprintf "commit the new member in the CLONE:\n%s" out)

                    // The feed carries the release exactly as it was published: every id the TAG's BOM names, at
                    // the pin — and a 404 for the member that did not exist when that tag was cut.
                    let feedUrl =
                        FakeFeed.arm (fun path ->
                                if path.Contains "brandnew" then 404, "" else flatContainer [ currentPin; currentPkgVersion ])

                    let ec, out =
                        runInWithEnv
                            tmp
                            [ "FS_GG_RUN_VERSION_COHERENCE_FEED", "1"
                              "FS_GG_VERSION_COHERENCE_FEED_URL", feedUrl
                              "FS_GG_VERSION_COHERENCE_PUBLISH_GRACE_MIN", "0" ]
                            "dotnet"
                            [ "fsi"; Path.Combine("scripts", "validate-version-coherence.fsx") ]

                    Expect.isFalse (out.Contains "release-phantom")
                        (sprintf "the release at the pin published everything ITS OWN BOM named; a member added afterwards was never part of it, and calling that release abandoned is a false red that camouflages a real one:\n%s" out)
                    Expect.equal ec 0 (sprintf "a new packable member is a coherent commit — the feed lane must stay green on it:\n%s" out)

                    Expect.isFalse ((FakeFeed.requests ()) |> List.exists (fun p -> p.Contains "brandnew"))
                        (sprintf "the lane must not even ASK the feed about a member the tag never promised — it reads the BOM AT THE TAG, not HEAD's src/**. Paths requested: %A" (FakeFeed.requests ()))
                finally
                    try
                        for f in Directory.EnumerateFiles(tmp, "*", SearchOption.AllDirectories) do
                            File.SetAttributes(f, FileAttributes.Normal)
                        Directory.Delete(tmp, true)
                    with _ -> ()
            }

            // ADR-0105 — a required gate's verdict must be a function of the commit alone. The verdict-core runs
            // as a step of the REQUIRED `Deterministic gate`, so it may not read a feed: an outage would
            // otherwise wedge every merge in the repo. That is a claim about what the code DOES NOT DO, and the
            // only honest way to test it is to make the network observable and assert it was never touched —
            // asserting a green exit would pass just as well if the guard were quietly calling nuget.org.
            test "the verdict-core does not touch the network — the feed lane is strictly opt-in" {
                let feedUrl = FakeFeed.arm (fun _ -> flatContainer [ currentPin ])

                // FS_GG_VERSION_COHERENCE_FEED_URL is set; FS_GG_RUN_VERSION_COHERENCE_FEED is NOT. If the
                // verdict-core probed the feed at all, this fixture would see the requests.
                let ec, out =
                    runInWithEnv
                        root
                        [ "FS_GG_VERSION_COHERENCE_FEED_URL", feedUrl ]
                        "dotnet"
                        [ "fsi"; repo "scripts/validate-version-coherence.fsx" ]

                Expect.equal ec 0 (sprintf "the repo is coherent offline:\n%s" out)
                Expect.isEmpty (FakeFeed.requests ())
                    (sprintf
                        "the verdict-core made %d feed request(s). It runs inside the REQUIRED gate, where a feed dependency hands the merge button to that feed's uptime (ADR-0105). The feed lane must stay opt-in, in its own non-required job. Paths requested: %A"
                        (FakeFeed.requests ()).Length
                        (FakeFeed.requests ()))
            }

            // #478's lesson, applied to the rule that exists because a guard reported green over a wedged repo:
            // a rule that cannot fire looks exactly like a repo that never drifts. The guard self-checks the
            // feed rules on EVERY invocation (all lanes, including the offline one), so a dead arm exits 2 —
            // GUARD ERROR — rather than quietly certifying a phantom. Keep that call wired.
            test "the feed rules are self-checked on every run, so a dead rule fails the guard rather than the repo" {
                let src = File.ReadAllText(repo "scripts/validate-version-coherence.fsx")
                let mainIdx = src.IndexOf("let main () =", StringComparison.Ordinal)
                Expect.isGreaterThan mainIdx -1 "the guard must have a main"

                let body = src.Substring mainIdx
                Expect.stringContains body "feedRulesSelfCheck ()"
                    "main must run feedRulesSelfCheck — it is the only thing proving `release-phantom` can still fire, and #478 is what a dead rule costs (four minors of silent drift under a green gate)"
            }
                ]
        )
    ]
