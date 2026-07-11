module TemplateProductProbeCompileTests

// FS-GG/FS.GG.Rendering#366 — deliverable 2: a PR-time, offline probe-compile of a scaffolded product.
//
// WHY THIS EXISTS. The generated product's source lives in `template/base/src/Product/**`, gated by the
// template engine's `//#if (profile == …)` / `//#else` / `//#endif` markers. Nothing on a PR ever
// COMPILES it: `gate.yml`'s template-payload step only *restores* the five profiles' pins (it proves the
// pins resolve, not that the product builds), and the real `dotnet new` → `dotnet build`/`test` of a
// scaffolded product runs only in `release.yml` (template-product-tests, release-only). So a PR that
// edits `template/base/src/Product/**` — or a breaking change to the FS.GG.UI.* framework surface the
// product consumes — compiles green on the PR and only reds the release lane post-merge. That is the
// "half-release" failure mode this issue names: the tags are already cut when the build breaks. The twin
// lane (#419…#424) closed the STATIC release-only rules; this closes the remaining half — an actual
// build — for at least one profile, at PR time, the cheapest way that stays honest.
//
// WHAT IT DOES. The DEFAULT `app` profile references only FS.GG.UI.* packages, every one of which THIS
// repo builds. So the product source can be compiled fully OFFLINE against the repo's own projects at
// HEAD — no `dotnet new`, no feed, no pack:
//   1. materialize `template/base/src/Product/{*.fs, Product.fsproj}` into a scratch dir, preprocessing
//      the `//#if`/`//#else`/`//#endif` (in .fs) and `<!--#if -->`/`<!--#endif -->` (in the .fsproj)
//      markers for `profile == "app"`. The `Product`/`product`/`AppRoot`/`approot` substitution tokens
//      are left verbatim: they are valid F# identifiers, so leaving them is a compile-equivalent rename
//      (the substitution engine renames them to `effectiveName`/`effectiveIdentifier`, and the token-leak
//      twins #422/#423/#424 already guard the case where substitution would produce an ILLEGAL name).
//   2. rewrite each `<PackageReference Include="FS.GG.UI.X" />` to a `<ProjectReference>` onto the repo's
//      `src/…` project for that package, so the product compiles against the CURRENT framework surface.
//   3. `dotnet build` the materialized product (Debug, offline). A non-zero build reds the PR.
//
// This catches, at PR time: a product-source edit that does not compile, and a breaking FS.GG.UI.* change
// at HEAD that the app product cannot consume — exactly the half-release class.
//
// HONESTY CAVEAT (constitution Principle V). Bounded on purpose:
//   * ONE profile (`app`). The `game`/`sample-pack` profiles also reference FS.GG.Game.* / FS.GG.Audio.*
//     from OTHER repos (feed-only), so they cannot be probed offline here; their build stays release-only.
//   * Compiled against the repo's HEAD projects, NOT the pinned FS.GG.UI.* PACKAGES — so a break that
//     manifests only against the pinned (older) version, not HEAD, is out of scope (the payload restore
//     gate proves the pins resolve; the release lane builds against them).
//   * A COMPILE, not the full `dotnet new` instantiation + `dotnet test` — byte-identical scaffolding and
//     the generated product's own tests remain release-only (release.yml template-product-tests).
//   * NOT a twin of a static Package.Tests rule, so it is deliberately NOT named `*CoherenceTests` and is
//     NOT registered in `ReleaseOnlyTwinLockstepTests`; it mirrors the release-only product BUILD, not a
//     text rule, and has no shared-inputs counterpart to lockstep against.

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Diagnostics
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

/// The profile we probe. The default `app` is the one release.yml instantiates first (`GeneratedProduct`,
/// no `--profile`), and the only one whose framework references are ALL repo-built FS.GG.UI.* packages —
/// which is what makes an offline ProjectReference compile possible.
let private profile = "app"

let private productSourceDir = repositoryPath "template/base/src/Product"

/// Every FS.GG.UI.* package an `app` product can reference, mapped to the repo project that produces it.
/// The map is asserted COMPLETE against the materialized fsproj (P-MAP-COMPLETE): if the app profile ever
/// references a package not here — e.g. a future edit pulls FS.GG.Game.* into the app branch — the probe
/// can no longer be offline and must fail loudly rather than silently drop the reference.
let private frameworkProjects =
    Map [ "FS.GG.UI.Scene", "src/Scene/Scene.fsproj"
          "FS.GG.UI.SkiaViewer", "src/SkiaViewer/SkiaViewer.fsproj"
          "FS.GG.UI.Elmish", "src/Elmish/Elmish.fsproj"
          "FS.GG.UI.KeyboardInput", "src/KeyboardInput/KeyboardInput.fsproj"
          "FS.GG.UI.Layout", "src/Layout/Layout.fsproj"
          "FS.GG.UI.Controls", "src/Controls/Controls.fsproj"
          "FS.GG.UI.Controls.Elmish", "src/Controls.Elmish/Controls.Elmish.fsproj"
          "FS.GG.UI.DesignSystem", "src/DesignSystem/DesignSystem.fsproj"
          "FS.GG.UI.Themes.Default", "src/Themes.Default/Themes.Default.fsproj"
          // Issue #430: the fs-gg-symbology skill ships on the app profile, so the packages it tells the
          // author to `open` are now referenced there (they were pinned on NO profile before — the bug).
          // Symbology.Render is the raster bridge and reaches SkiaViewer, already mapped above.
          "FS.GG.UI.Symbology", "src/Symbology/Symbology.fsproj"
          "FS.GG.UI.Symbology.Render", "src/Symbology.Render/Symbology.Render.fsproj"
          // Not in the app branch today, but legitimately reachable if a future edit widens the gate;
          // keeping them mapped means such an edit still probes offline instead of failing P-MAP-COMPLETE.
          "FS.GG.UI.Canvas", "src/Canvas/Canvas.Lib.fsproj"

          "FS.GG.UI.Testing", "src/Testing/Testing.fsproj" ]

/// Packages from ANOTHER FS-GG repo that the app profile references (issue #436 put FS.GG.Audio.Core
/// and .Host on it, so the Controls family can reach #429's audio sink). This repo builds no project
/// for them, so — unlike the FS.GG.UI.* rows above — there is no HEAD to compile against, and the
/// honest thing to probe is what a real generated product actually consumes: the PINNED release,
/// resolved from the feed at the template's own `$(FsGgAudioVersion)`.
///
/// This does NOT weaken the offline contract. Both are already in the global-packages folder by the
/// time this test runs, because the FRAMEWORK depends on them — `src/SkiaViewer` references
/// FS.GG.Audio.Core and `tests/SkiaViewer.Tests` references FS.GG.Audio.Host, so the slnx restore that
/// precedes this test has fetched them. That is exactly why the split #436 chose is Core/Host and not
/// all four: FS.GG.Audio.Engine/.Elmish are referenced by nothing in this repo, are never in the cache,
/// and putting them on `app` would make this probe need the network.
///
/// A package added here must therefore satisfy BOTH: it is genuinely feed-only (no repo project), and
/// something in the slnx already pulls it. Anything else belongs in `frameworkProjects` — or nowhere,
/// and P-MAP-COMPLETE will say so.
let private feedOnlyPackages = set [ "FS.GG.Audio.Core"; "FS.GG.Audio.Host" ]

/// The template's own audio pin, read from where the template declares it, so the probe compiles the
/// version a scaffolded product would actually get and cannot drift from it.
let private audioVersion =
    let props = File.ReadAllText(repositoryPath "template/base/Directory.Packages.props")
    let m = Regex.Match(props, "<FsGgAudioVersion>(?<v>[^<]+)</FsGgAudioVersion>")
    if not m.Success then
        failwith
            "template/base/Directory.Packages.props declares no <FsGgAudioVersion> — the probe cannot pin the feed-only audio packages it must restore"
    m.Groups.["v"].Value.Trim()

// ---- the profile-marker preprocessor ----------------------------------------------------------
/// Does a `//#if`/`<!--#if -->` condition of the shape `profile == "a" || profile == "b"` hold for the
/// probed profile? Only that disjunction-of-equalities shape is understood; anything else (a `!=`, a
/// `&&`, a negation) RAISES rather than being silently mis-evaluated into the wrong branch — the same
/// fail-loud contract validate-template-payload-pins.fsx's `gateHolds` keeps.
let private disjunctionShape =
    Regex(@"^\s*profile\s*==\s*""[^""]+""\s*(\|\|\s*profile\s*==\s*""[^""]+""\s*)*$")

let private conditionHolds (condition: string) =
    if not (disjunctionShape.IsMatch condition) then
        failwithf
            "unevaluable profile marker condition: `%s` — the probe preprocessor understands only `profile == \"a\" || profile == \"b\"`; teach it the new shape rather than letting it silently mis-evaluate a branch"
            condition
    Regex.Matches(condition, "\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Seq.contains profile

/// Walk nested `#if`/`#else`/`#endif` directives and keep only the lines the probed profile's branch
/// ships. Each active nesting level is `(condition-holds, past-its-#else)`; a line is included iff every
/// active level admits it. Directive lines themselves are dropped. This is the same nested walk the
/// release-only SwapChecklistTemplateTests uses (`branchFor`), generalized over the directive syntax so
/// it serves both the `//#…` F# sources and the `<!--#… -->` fsproj.
let private preprocess (ifRe: Regex) (elseRe: Regex) (endifRe: Regex) (text: string) =
    let mutable levels : (bool * bool) list = []
    let included () = levels |> List.forall (fun (holds, inElse) -> if inElse then not holds else holds)
    let sb = StringBuilder()
    for raw in text.Replace("\r\n", "\n").Split('\n') do
        let mIf = ifRe.Match raw
        if mIf.Success then
            levels <- (conditionHolds (mIf.Groups.[1].Value), false) :: levels
        elif endifRe.IsMatch raw then
            levels <- (match levels with _ :: rest -> rest | [] -> [])
        elif elseRe.IsMatch raw then
            levels <- (match levels with (holds, _) :: rest -> (holds, true) :: rest | [] -> [])
        elif included () then
            sb.AppendLine raw |> ignore
    sb.ToString()

let private fsIf = Regex(@"^\s*//#if\s+\((.*?)\)\s*$")
let private fsElse = Regex(@"^\s*//#else\b")
let private fsEndif = Regex(@"^\s*//#endif\b")
let private xmlIf = Regex(@"^\s*<!--#if\s+\((.*?)\)\s*-->\s*$")
let private xmlElse = Regex(@"^\s*<!--#else\s*-->\s*$")
let private xmlEndif = Regex(@"^\s*<!--#endif\s*-->\s*$")

let private preprocessFs = preprocess fsIf fsElse fsEndif
let private preprocessXml = preprocess xmlIf xmlElse xmlEndif

// ---- materialization + build ------------------------------------------------------------------
let private packageRefRegex = Regex(@"<PackageReference\s+Include=""(?<id>[^""]+)""\s*/>")

type private Materialized =
    { Dir: string
      CompileItems: string list
      ProjectRefs: string list
      /// FS.GG.* package ids the app fsproj still references AFTER preprocessing (before the rewrite).
      ReferencedPackages: string list }

/// Materialize the app product into `dir`: preprocessed sources + a fsproj whose FS.GG.UI.* package
/// references are rewritten to repo ProjectReferences.
let private materialize (dir: string) : Materialized =
    Directory.CreateDirectory dir |> ignore

    for file in Directory.GetFiles(productSourceDir, "*.fs") do
        let name =
            match Path.GetFileName file with
            | null -> failwithf "unexpected null filename enumerating %s" productSourceDir
            | n -> n
        File.WriteAllText(Path.Combine(dir, name), preprocessFs (File.ReadAllText file))

    let projText = preprocessXml (File.ReadAllText(Path.Combine(productSourceDir, "Product.fsproj")))

    let referencedPackages =
        packageRefRegex.Matches projText
        |> Seq.map (fun m -> m.Groups.["id"].Value)
        |> Seq.filter (fun id -> id.StartsWith("FS.GG.", StringComparison.Ordinal))
        |> Seq.distinct
        |> Seq.toList

    let rewritten =
        packageRefRegex.Replace(
            projText,
            fun m ->
                let id = m.Groups.["id"].Value
                match Map.tryFind id frameworkProjects with
                | Some proj -> sprintf "<ProjectReference Include=\"%s\" />" (repositoryPath proj)
                // A feed-only package from another FS-GG repo (#436): no repo project to compile
                // against, so keep it a real PackageReference at the template's pinned version. The
                // probe dir carries no Directory.Packages.props, so the Version must be explicit here
                // — a bare central-managed reference would not restore.
                | None when Set.contains id feedOnlyPackages ->
                    sprintf "<PackageReference Include=\"%s\" Version=\"%s\" />" id audioVersion
                | None -> sprintf "<!-- UNMAPPED PACKAGE: %s -->" id)

    File.WriteAllText(Path.Combine(dir, "Product.fsproj"), rewritten)

    let itemsOf (marker: string) =
        rewritten.Replace("\r\n", "\n").Split('\n')
        |> Array.filter (fun l -> l.Contains marker)
        |> Array.map (fun l -> l.Trim())
        |> Array.toList

    { Dir = dir
      CompileItems = itemsOf "Compile Include"
      ProjectRefs = itemsOf "ProjectReference"
      ReferencedPackages = referencedPackages }

/// Wall-clock ceiling on the child build; a wedged MSBuild/restore must fail this test with a message,
/// not hang until the CI job-level timeout.
let private buildTimeoutMs = 300_000

let private runBuild (dir: string) : int * string =
    let psi = ProcessStartInfo("dotnet")
    psi.WorkingDirectory <- dir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    // Restore against the REPO's pinned nuget.config (issue #186): its `<clear />` + packageSourceMapping
    // is what blocks the SDK's implicit `FSharp/library-packs` FSharp.Core source, whose nupkg differs
    // byte-for-byte from the feed's. The probe dir lives under /tmp and would otherwise inherit none of
    // that, exposing the restore to the exact poisoned-FSharp.Core hazard the repo config exists to
    // prevent. Reusing the config (not a scratch feed) keeps the probe OFFLINE: packages already sit in
    // the default global-packages folder the slnx restore populated.
    // -m:1 and no shared compiler: this build is nested inside `dotnet test`, and the F# compiler server
    // contends across concurrent MSBuild nodes (MSB6006). Serial + private compilation keeps it stable.
    [ "build"
      "Product.fsproj"
      "-c"
      "Debug"
      "-m:1"
      "/p:UseSharedCompilation=false"
      sprintf "/p:RestoreConfigFile=%s" (repositoryPath "nuget.config")
      "--nologo" ]
    |> List.iter psi.ArgumentList.Add
    match Process.Start psi with
    | null -> failwith "could not start `dotnet` to probe-compile the app product"
    | proc ->
        use proc = proc
        // Read the pipes asynchronously BEFORE waiting: a synchronous ReadToEnd would block on a hung
        // child that never closes stdout, defeating the timeout. The tasks complete once the process
        // exits or is killed.
        let outTask = proc.StandardOutput.ReadToEndAsync()
        let errTask = proc.StandardError.ReadToEndAsync()
        if proc.WaitForExit buildTimeoutMs then
            proc.ExitCode, outTask.Result + errTask.Result
        else
            (try proc.Kill true with _ -> ())
            -1, sprintf "probe-compile build exceeded %d ms and was killed" buildTimeoutMs

/// Materialize into a fresh scratch dir, hand it to `f`, and always clean up. Each test that needs the
/// materialized tree gets its own, so tests stay independent (materialization is cheap file IO; only the
/// compile test pays the build cost).
let private withMaterialized (f: Materialized -> unit) =
    let dir = Path.Combine(Path.GetTempPath(), "fsgg366-probe-" + Guid.NewGuid().ToString("N").Substring(0, 8))
    try
        f (materialize dir)
    finally
        try Directory.Delete(dir, true) with _ -> ()

[<Tests>]
let templateProductProbeCompileTests =
    testList
        "#366 — app-profile template product probe-compile (offline)"
        [ // P-PREPROC — the load-bearing marker walk. A bug that included the wrong branch would compile
          // the wrong thing and give false confidence, so pin it with nested fixtures: the probed profile
          // keeps its own branch and the negation of the others', at every nesting depth.
          test "the profile-marker preprocessor keeps exactly the probed branch, nesting included" {
              let fixture =
                  String.concat "\n"
                      [ "top"
                        "//#if (profile == \"governed\" || profile == \"headless-scene\")"
                        "governed-only"
                        "//#else"
                        "//#if (profile == \"game\")"
                        "game-only"
                        "//#else"
                        "app-or-samplepack"
                        "//#if (profile == \"app\")"
                        "app-only"
                        "//#endif"
                        "//#endif"
                        "//#endif"
                        "bottom" ]
              let kept = preprocessFs fixture
              // profile = "app": drops governed branch, takes the outer #else, drops the game branch,
              // takes the inner #else, and keeps the app-only nested #if.
              Expect.stringContains kept "top" "unconditional lines survive"
              Expect.stringContains kept "bottom" "unconditional lines survive"
              Expect.stringContains kept "app-or-samplepack" "the app/sample-pack branch is kept"
              Expect.stringContains kept "app-only" "a nested app-only #if inside the kept branch is kept"
              Expect.isFalse (kept.Contains "governed-only") "the governed branch is dropped"
              Expect.isFalse (kept.Contains "game-only") "the game branch is dropped"
              Expect.isFalse (kept.Contains "#if") "directive lines are stripped"
          }

          // P-MAP-COMPLETE — every FS.GG.* package the app fsproj still references after preprocessing is
          // accounted for: either it maps to a repo project (compiled at HEAD) or it is a declared
          // feed-only package from another FS-GG repo (restored at its pinned version, #436). One that is
          // NEITHER means the app profile is no longer probeable as configured — e.g. a feed-only
          // FS.GG.Game.* package crept into the app branch, or FS.GG.Audio.Engine/.Elmish did, neither of
          // which is in the global-packages folder. Fail loudly here rather than silently dropping the
          // reference and compiling a product that is missing a dependency.
          test "every referenced FS.GG.* package is probeable (repo project, or a declared feed-only pin)" {
              withMaterialized (fun mat ->
                  let unaccounted =
                      mat.ReferencedPackages
                      |> List.filter (fun id ->
                          not (Map.containsKey id frameworkProjects) && not (Set.contains id feedOnlyPackages))

                  Expect.isEmpty
                      unaccounted
                      (sprintf
                          "the app profile references FS.GG.* package(s) that are neither built in this repo nor declared feed-only, so the probe cannot resolve them: %A — add a `frameworkProjects` mapping if the package is built here; add it to `feedOnlyPackages` ONLY if something in the slnx already restores it (otherwise the probe would need the network); else move the probe off the app profile"
                          unaccounted)
                  Expect.isFalse
                      ((File.ReadAllText(Path.Combine(mat.Dir, "Product.fsproj"))).Contains "UNMAPPED")
                      "no PackageReference was left unmapped in the materialized fsproj")
          }

          // P-NONVACUOUS — a preprocessor bug (or a moved source tree) that yielded an empty product would
          // make the compile pass vacuously. Pin a floor on what the app tree ships.
          test "the materialized app product is non-empty (not a vacuous compile)" {
              withMaterialized (fun mat ->
                  Expect.isGreaterThan
                      (List.length mat.CompileItems)
                      3
                      "the app product compiles several sources (Model/View/Program at minimum)"
                  Expect.isGreaterThan
                      (List.length mat.ProjectRefs)
                      3
                      "the app product references several FS.GG.UI.* framework projects"
                  // The app profile must NOT drag in the game/sample-pack-only simulation helpers.
                  // AudioCues.fs is NO LONGER one of them: #436 gave the app profile a cue seam (the
                  // Controls host launches through `runInteractiveAppWithAudio`), so the file compiles
                  // on every windowed profile and is asserted PRESENT just below.
                  let joined = String.concat "\n" mat.CompileItems
                  for gameOnly in [ "Vec2.fs"; "Collision.fs"; "Visibility.fs"; "Grids.fs"; "LineDrawing.fs" ] do
                      Expect.isFalse (joined.Contains gameOnly) (sprintf "%s is game/sample-pack-only and must not be in the app tree" gameOnly)

                  // #436, the positive half: the app tree really does carry the cue seam. Without this,
                  // deleting `AudioCues.fs` from the app gate would silently pass every check in this
                  // file — the product would simply compile with no sound, which is the defect #436
                  // fixed and precisely the state this probe was blind to before.
                  Expect.stringContains joined "AudioCues.fs" "the app profile compiles AudioCues.fs — it has an audio cue seam (#436)")
          }

          // P-COMPILES — the deliverable. The materialized app product compiles offline against the repo's
          // HEAD framework projects. A non-zero build is the half-release defect, caught on the PR instead
          // of the release lane.
          test "the app-profile product compiles offline against the repo framework at HEAD" {
              withMaterialized (fun mat ->
                  let exitCode, output = runBuild mat.Dir
                  Expect.equal
                      exitCode
                      0
                      (sprintf
                          "the scaffolded `app` product failed to compile against the framework at HEAD — a PR-visible half-release defect. Build output (tail):\n%s"
                          (let lines = output.Replace("\r\n", "\n").Split('\n') in String.concat "\n" (Array.skip (max 0 (lines.Length - 40)) lines))))
          } ]
