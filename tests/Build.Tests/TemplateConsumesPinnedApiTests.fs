module TemplateConsumesPinnedApiTests

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open System.Text
open System.Text.RegularExpressions
open Expecto

// Issue #504 — the template-vs-PIN assertion.
//
// THE GAP. `src/` compiles against ITSELF, and the bundled api-surface mirror
// (`template/base/docs/api-surface/**`) is frozen against `src/`. But a SCAFFOLDED product compiles
// against the PUBLISHED package at $(FsGgUiVersion). So the framework and its mirror can agree with
// each other while BOTH disagree with the only surface the template can actually call.
//
// That is what #429's audio seam did: `Viewer.runAppWithAudio` lived in `src/`, the mirror duly
// advertised it, the template pinned 0.8.0 — which did not contain it — and NOTHING went red for the
// entire life of 0.8.0. #436 found it by trying to build; #492 had to cut a release.
//
// WHY THE MIRROR CANNOT BE THE ORACLE. #437's M-MIR diffs the mirror against `src/`. That is a real
// gate, but it is blind to this class BY CONSTRUCTION, because `src/` is the side that is ahead. A
// PERFECT mirror-vs-src gate still reports green while the template cannot compile. The missing
// assertion is not mirror-vs-src. It is template-vs-pin.
//
// TWO LAYERS, mirroring scripts/validate-template-payload-pins.fsx:
//
//   * Structural core (always, offline). Extract the framework entry points the template's
//     `Program.fs` ACTUALLY calls, and resolve each against the bundled mirror. This catches a call
//     site that names nothing at all. It is deliberately NOT the pin proof — the mirror tracks
//     `src/`, so this layer is green in exactly the #429 case. Its real job is to keep the EXTRACTOR
//     honest: zero call sites is an ERROR, never a pass (the fails-open class of FS-GG/.github#266 —
//     "nothing to check" and "checked, and it's fine" must not share an exit code).
//
//   * Pin-grounded proof (FS_GG_RUN_TEMPLATE_PINNED_API=1, network). Restore the pinned packages at
//     the template's real axes and COMPILE the extracted call sites against them. This is the layer
//     that turns red the moment the framework grows API the template cannot consume — i.e. at
//     #433's merge, not at #436's build three days and one emergency release later.
//
// It is a compile probe rather than reflection on purpose: `Assembly.LoadFrom` on a bare package
// DLL trips over unresolved dependencies, and `MetadataLoadContext` would add a PackageReference to
// this locked-restore project. `nameof` resolves the symbol at COMPILE time and needs neither, so a
// member the pinned package does not export is a compiler error — which is precisely the question
// being asked ("can the template consume the pin?").
//
// This test lives in Build.Tests, NOT Package.Tests, DELIBERATELY. Package.Tests is release-only and
// absent from FS.GG.Rendering.slnx, so a rule placed there "runs after the merge that breaks it,
// never on the PR" (gate.yml) — that is how Renovate PR #233 reached 4/4 green on a pin no local
// pack could produce. A check that fires after the merge cannot deliver #504, whose entire point is
// to fire ON it. Build.Tests is in the slnx, so this runs on every gate.
//
// RELEASE-PENDING (#543) — THE ONE WINDOW IN WHICH THE PROBE CANNOT RUN.
//
// The FS.GG.UI.* packages are published FROM THIS REPO, and the pin bump is what CAUSES the publish:
// `release-tags.yml` cuts `fs-gg-ui/v<pin>` on merge to `main` and calls `release.yml`. So on a
// release PR, $(FsGgUiVersion) NECESSARILY names a version nuget.org does not carry yet, the restore
// fails NU1102 BY CONSTRUCTION, and this test goes red on the commit whose whole job is to be merged.
//
// That is fatal here in a way it was not for the sibling gates. `scripts/validate-template-payload-pins.fsx`
// (#506) sits on an ADVISORY job a releaser could merge past; this test runs in Build.Tests, on the
// REQUIRED `Deterministic gate`. `main` has `enforce_admins` ON and `--admin` is forbidden (ADR-0103),
// so the red does not merely shout — it wedges the merge button, and the release cannot land at all.
//
// So the probe is DEFERRED in that window, on the same bounds `validate-version-coherence.fsx`'s
// `PinPending` and `validate-template-payload-pins.fsx`'s `releasePending` already carry. Copied, not
// approximated — each conjunct is load-bearing:
//
//   * ONLY when the probe's failure is EXACTLY "the pinned FS.GG.UI.* packages are not on the feed".
//     Read off the restore we already ran, so the evidence for the waiver is the very diagnostic it
//     excuses. Any OTHER error — an FS0039 from a call site the pin does not export, an NU1101 typo'd
//     id, an NU1603 upward resolution — and the waiver is off: those are the failures this test exists
//     to catch, and none of them is what a release window looks like.
//
//   * ONLY the $(FsGgUiVersion) axis. This is the whole safety of the waiver. FS.GG.Audio.* is pinned
//     in this probe too and ships from ANOTHER repo, where a bump HERE publishes nothing — so an
//     unpublished Audio pin is a real defect EVEN ON THE COMMIT THAT BUMPED IT, and is never waived.
//     A naive "this commit bumped an axis ⇒ waive" would reopen #235: a stale component pin, green.
//     The bound falls out of the rule above — an NU1102 naming FS.GG.Audio.* is not a UI NU1102.
//
//   * ONLY when THIS commit bumped it (`bumpedInCommitUnderTest`, the predicate #209 proved out). A pin
//     NOBODY bumped that the feed does not carry is stale or typo'd — drift, and still red. That is the
//     half of this check that must survive the waiver, and it is the reason the waiver is not simply
//     "NU1102 on a UI package is fine".
//
//   * NEVER in the release lane (`FS_GG_VERSION_COHERENCE_RELEASE_LANE`, set job-wide by `release.yml`).
//     The premise is "these packages cannot exist yet — this very commit creates them", which is only
//     true BEFORE the publish. At publish time they are DUE, and a missing one is drift.
//
// SKIPPED IS NOT PASSED. The probe genuinely cannot run in the window, so it says so and is reported
// IGNORED — it does not report green having verified nothing, which is the fails-open shape (#266) this
// file's own header forbids. The three structural tests above it are offline and keep running, so the
// extractor, the launch seam and the mirror are still asserted on a release PR; it is the pin-grounded
// layer, and only that, which defers to the publish.

// ---------------------------------------------------------------------------------------------
// Repo layout
// ---------------------------------------------------------------------------------------------

let private repoRoot =
    let rec up (dir: DirectoryInfo | null) =
        match dir with
        | null -> failwith "could not locate repo root (FS.GG.Rendering.slnx) walking up from test base dir"
        | d ->
            if File.Exists(Path.Combine(d.FullName, "FS.GG.Rendering.slnx")) then d.FullName
            else up d.Parent

    up (DirectoryInfo(AppContext.BaseDirectory))

let private repoPath (rel: string) =
    Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private mirrorRoot = repoPath "template/base/docs/api-surface"
let private programPath = repoPath "template/base/src/Product/Program.fs"

/// Repo-RELATIVE, and the absolute path is derived from it — because the RELEASE-PENDING waiver below
/// asks `git diff` about this same file, and git only speaks repo-relative. Two literals could drift,
/// and the failure would be silent in the worst possible way: `git diff` on a path that no longer
/// exists exits 0 with empty output, which reads as "nobody bumped the pin", and the waiver quietly
/// stops firing — putting the always-red release gate (#543) back with nothing to show why.
let private packagesPropsRel = "template/base/Directory.Packages.props"
let private packagesPropsPath = repoPath packagesPropsRel

// ---------------------------------------------------------------------------------------------
// The framework surface, as the TEMPLATE bundles it.
//
// Each mirrored `.fsi` is one package: its `namespace` IS the package id (`namespace
// FS.GG.UI.SkiaViewer` -> package `FS.GG.UI.SkiaViewer`), which is what lets the probe below derive
// its PackageReferences instead of hardcoding a list that would rot.
// ---------------------------------------------------------------------------------------------

type FrameworkModule =
    { /// The package id AND the namespace to `open` — they are the same string.
      Namespace: string
      /// Top-level module name, as a call site spells it (`Viewer`, `ControlsElmish`, `Audio`).
      Name: string
      /// `val` members exported by that module.
      Members: Set<string> }

let private namespaceRegex = Regex(@"^namespace\s+([\w.]+)", RegexOptions.Compiled)
let private moduleRegex = Regex(@"^module\s+(\w+)", RegexOptions.Compiled)
let private valRegex = Regex(@"^\s+val\s+(?:inline\s+)?([a-z]\w*)\s*:", RegexOptions.Compiled)

/// Parse one mirrored `.fsi` into its top-level modules. Only column-0 `module` declarations are
/// entry points a call site can name; nested modules are reached through their parent and are not
/// what `Program.fs` writes.
let private parseMirrorFile (path: string) =
    let mutable ns = ""
    let mutable current = ""
    // Keyed by (namespace, module): the namespace is bound when the MODULE is declared, not read off
    // a mutable after the loop, so a file that ever declares two namespaces attributes each module to
    // the one it was actually written under instead of to whichever came last.
    let members = System.Collections.Generic.Dictionary<string * string, ResizeArray<string>>()

    for line in File.ReadAllLines path do
        let nsMatch = namespaceRegex.Match line
        let moduleMatch = moduleRegex.Match line
        let valMatch = valRegex.Match line

        if nsMatch.Success then ns <- nsMatch.Groups.[1].Value
        elif moduleMatch.Success then
            current <- moduleMatch.Groups.[1].Value
            if ns <> "" && not (members.ContainsKey((ns, current))) then
                members.[(ns, current)] <- ResizeArray()
        elif valMatch.Success && current <> "" && ns <> "" then
            match members.TryGetValue((ns, current)) with
            | true, vals -> vals.Add(valMatch.Groups.[1].Value)
            | _ -> ()

    members
    |> Seq.map (fun kvp ->
        { Namespace = fst kvp.Key
          Name = snd kvp.Key
          Members = Set.ofSeq kvp.Value })
    |> List.ofSeq

let private frameworkModules =
    Directory.EnumerateFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect parseMirrorFile
    |> Seq.filter (fun m -> not m.Members.IsEmpty)
    |> List.ofSeq

/// A module name is only a framework entry point if the mirror declares it. This is what keeps the
/// extractor from mistaking the product's OWN modules (`AppRoot.WindowOptions.parseWindowBehavior`)
/// or FSharp.Core's (`List.ofArray`, `Option.defaultValue`) for framework calls.
let private frameworkModulesByName =
    frameworkModules
    |> List.groupBy (fun m -> m.Name)
    |> Map.ofList

// ---------------------------------------------------------------------------------------------
// The call sites, as the TEMPLATE'S Program.fs actually writes them.
//
// The `//#if` profile directives are COMMENTS, so reading the file as raw text sees every profile's
// code at once — app, game, sample-pack, governed, headless-scene — which is what we want: the pin
// must satisfy every profile the template can scaffold.
// ---------------------------------------------------------------------------------------------

type CallSite =
    { Module: string
      Member: string
      Line: int }

let private stringLiteral = Regex("\"(\\\\.|[^\"\\\\])*\"", RegexOptions.Compiled)

/// Multi-line forms have to go before the file is split into lines: a `(* ... *)` block or a `"""…"""`
/// string spans lines, so no per-line rule can see it. Each match is replaced by JUST its newlines,
/// which erases the content while keeping every later line at its true line number (the numbers are
/// reported to a human chasing a failure, so they have to be real).
let private blockForms =
    Regex(@"\(\*.*?\*\)|"""""".*?""""""", RegexOptions.Compiled ||| RegexOptions.Singleline)

let private eraseKeepingLines (text: string) =
    blockForms.Replace(
        text,
        fun m -> String(m.Value |> Seq.filter (fun c -> c = '\n') |> Seq.toArray))

/// Strings BEFORE comments: a `//` inside a string literal is not a comment. Both must go, because
/// `Program.fs` NAMES framework API in prose and in string literals without calling it —
/// `let desktopSessionDiagnosticApi = "Viewer.desktopSessionDiagnostic()"` is a label, not a call.
/// Counting it would make this test assert something the template does not actually do, and would
/// then demand the pinned package export API the template never touches.
let private stripCommentsAndStrings (line: string) =
    let withoutStrings = stringLiteral.Replace(line, "\"\"")

    match withoutStrings.IndexOf("//", StringComparison.Ordinal) with
    | -1 -> withoutStrings
    | i -> withoutStrings.Substring(0, i)

/// `[qualifier.]Module.member` — a capitalised module, a lowercase-initial member (the F# convention
/// for functions and values). The qualifier is captured rather than discarded because it is the only
/// thing that disambiguates a genuine name collision: the framework exports
/// `FS.GG.UI.Scene.LayoutEvidence`, and the TEMPLATE'S OWN `AppRoot.LayoutEvidence` shares its name.
/// Matching on the bare module name alone would read the product's calls to itself as framework
/// calls — and then demand the pinned package export them.
let private callRegex = Regex(@"(?<![\w.])((?:[A-Z]\w*\.)*)([A-Z]\w*)\.([a-z]\w*)", RegexOptions.Compiled)

/// The template's own modules (`module AppRoot.LayoutEvidence`), so an unqualified reference to one
/// of them is never mistaken for the framework module it happens to share a name with.
let private productModules =
    Directory.EnumerateFiles(Path.GetDirectoryName programPath |> Option.ofObj |> Option.defaultValue "", "*.fs")
    |> Seq.collect File.ReadAllLines
    |> Seq.choose (fun line ->
        let m = Regex.Match(line, @"^module\s+(?:[\w.]+\.)?(\w+)\s*$")
        if m.Success then Some m.Groups.[1].Value else None)
    |> Set.ofSeq

/// A match is a framework call only if the mirror declares the module AND the qualifier agrees:
/// either the call is unqualified (reached through an `open`) or it is spelled out in full with the
/// framework namespace (`FS.GG.Audio.Host.OpenAlBackend.create`). A qualifier that is anything else
/// — `AppRoot.` — is the product calling itself.
let private isFrameworkCall (qualifier: string) (moduleName: string) =
    match frameworkModulesByName.TryFind moduleName with
    | None -> false
    | Some candidates ->
        let qualified = qualifier.TrimEnd('.')

        if qualified = "" then not (productModules.Contains moduleName)
        else candidates |> List.exists (fun m -> m.Namespace = qualified)

let private callSites =
    (File.ReadAllText programPath |> eraseKeepingLines).Split('\n')
    |> Array.mapi (fun i line -> i + 1, stripCommentsAndStrings line)
    |> Array.collect (fun (lineNo, line) ->
        callRegex.Matches line
        |> Seq.map (fun m ->
            m.Groups.[1].Value,
            { Module = m.Groups.[2].Value
              Member = m.Groups.[3].Value
              Line = lineNo })
        |> Array.ofSeq)
    |> Array.filter (fun (qualifier, c) -> isFrameworkCall qualifier c.Module)
    |> Array.map snd
    |> Array.distinctBy (fun c -> c.Module, c.Member)
    |> List.ofArray

/// Resolve a call site to the mirrored module that EXPORTS THE MEMBER (a module name is unique
/// across the mirror in practice; if two packages ever export the same module name, any that
/// declares the member satisfies the call, which is what the F# resolver would do given the `open`s).
/// `None` means the mirror does not declare this member — which is what the mirror test asserts on.
let private owningModule (call: CallSite) =
    frameworkModulesByName
    |> Map.tryFind call.Module
    |> Option.bind (fun candidates -> candidates |> List.tryFind (fun m -> m.Members.Contains call.Member))

/// The namespace to `open` (and the package to reference) for a call site — resolved at MODULE level,
/// deliberately falling back to any module of that name when no mirrored module declares the member.
///
/// The probe emits a `nameof` for EVERY call site, so it must emit an `open` for every call site too.
/// Deriving the namespace from `owningModule` instead would drop exactly the call sites the mirror is
/// missing, and the probe would then fail FS0039 on an unopened namespace and blame the PIN — reporting
/// "the framework grew API a scaffolded product cannot reach" when the pin is fine and only the mirror
/// is stale. A wrong diagnosis on a real failure is worse than no diagnosis.
let private callNamespace (call: CallSite) =
    frameworkModulesByName
    |> Map.tryFind call.Module
    |> Option.bind (fun candidates ->
        candidates
        |> List.tryFind (fun m -> m.Members.Contains call.Member)
        |> Option.orElse (List.tryHead candidates))
    |> Option.map (fun m -> m.Namespace)

// ---------------------------------------------------------------------------------------------
// The pins the template hands a scaffolded product.
// ---------------------------------------------------------------------------------------------

let private readAxis (axis: string) =
    let props = File.ReadAllText packagesPropsPath
    let m = Regex.Match(props, $"<{axis}>([^<]+)</{axis}>")
    if m.Success then m.Groups.[1].Value else failwith $"<{axis}> not found in {packagesPropsPath}"

/// A package id derives its version from the axis its family is released on — the same three axes
/// `Directory.Packages.props` declares. Getting this wrong would restore a version the template
/// never pins, and the probe would then prove nothing about the real product.
let private pinFor (packageId: string) =
    if packageId.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then readAxis "FsGgUiVersion"
    elif packageId.StartsWith("FS.GG.Audio.", StringComparison.Ordinal) then readAxis "FsGgAudioVersion"
    elif packageId.StartsWith("FS.GG.Game.", StringComparison.Ordinal) then readAxis "FsGgGameVersion"
    else failwith $"no version axis covers package '{packageId}'"

// ---------------------------------------------------------------------------------------------
// RELEASE-PENDING (#543): is this the release window, in which the pin CANNOT resolve yet?
// See the header for why each conjunct below is load-bearing.
// ---------------------------------------------------------------------------------------------

let private uiAxis = "FsGgUiVersion"

/// Set job-wide by any job that gates a PUBLISH (`release.yml`). Kills the waiver outright: its
/// premise is "these packages cannot exist yet — this very commit creates them", which stops being
/// true at publish time, when they are due. Nothing runs this test in that lane today; reading the
/// flag `release.yml` already sets shuts the door the moment someone adds it, rather than depending
/// on them reading this comment.
let private releaseLane =
    Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_RELEASE_LANE" = "1"

/// Sequential pipe drain is safe HERE and would not be in `runProbeBuild`: a `--unified=0` diff of one
/// small props file is a few hundred bytes, far under the pipe buffer, so the child cannot block on a
/// pipe nobody is reading. `dotnet build` can and does, which is why that one drains concurrently.
let private runGit (args: string list) =
    let psi = ProcessStartInfo "git"
    psi.WorkingDirectory <- repoRoot
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add

    match Process.Start psi with
    | null -> -1, "could not start 'git'"
    | started ->
        use proc = started
        let out = proc.StandardOutput.ReadToEnd()
        let err = proc.StandardError.ReadToEnd()
        proc.WaitForExit()
        proc.ExitCode, out + err

/// Did the commit under test change the VALUE of `<element>` in `rel`? — the RELEASE-PENDING signal,
/// with the semantics of `scripts/validate-template-payload-pins.fsx`'s `bumpedInCommitUnderTest`.
/// Duplicated rather than shared for the same reason that one duplicates the SemVer comparator: it is
/// a standalone `dotnet fsi` entry point, with no package reference between it and this project.
///
/// Compares the element's VALUE across the diff, not merely whether its line was TOUCHED: this
/// predicate waives a fail-closed check, so a reindent or a line-ending change to the <FsGgUiVersion>
/// line must not be able to silence it. Added values must exist and differ from removed ones.
///
/// Env-free by construction: `HEAD~1` is the first parent, which is the base branch for a
/// `pull_request` merge-ref checkout AND the previous `main` commit for a squash push — so the same
/// diff answers both contexts without reading GITHUB_*.
///
/// `Error` — never `false` — if git cannot answer (a shallow clone with no `HEAD~1`). Reading that as
/// "not bumped" would be the quiet choice and the wrong one: it silently restores the always-red gate
/// this waiver exists to remove, and nobody would know why. The `gate` job that runs Build.Tests checks
/// out with `fetch-depth: 0`, so the diff is available.
let private bumpedInCommitUnderTest (rel: string) (element: string) : Result<bool, string> =
    let exitCode, out = runGit [ "diff"; "HEAD~1"; "HEAD"; "--unified=0"; "--"; rel ]

    if exitCode <> 0 then
        Error
            $"`git diff HEAD~1 HEAD -- {rel}` failed, so the RELEASE-PENDING waiver cannot be \
              evaluated — most likely a shallow clone. CI must check out with `fetch-depth: 0`.\n\n{out}"
    else
        let rx = Regex($"<{Regex.Escape element}>([^<]*)</{Regex.Escape element}>")

        // "+++" / "---" are the file headers, not content lines.
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
        Ok(not added.IsEmpty && added <> removed)

/// The severity+code of a build diagnostic, in MSBuild's canonical format (`… error NU1102: …`).
let private errorCodeRegex = Regex(@"\berror\s+[A-Z]+[0-9]+\b", RegexOptions.Compiled)

/// The FS.GG.* package ids named on a line. Read off the IDS THEMSELVES rather than NuGet's English
/// prose ("Unable to find package X with version (>= Y)"), so the waiver's scope is decided by what the
/// diagnostic is ABOUT and not by wording that a localized or reworded NuGet could change underneath it.
let private fsGgIdRegex = Regex(@"\bFS\.GG\.[A-Za-z0-9.]*[A-Za-z0-9]", RegexOptions.Compiled)

/// Is the probe's failure EXACTLY "the FS.GG.UI.* packages this commit pins are not on the feed yet",
/// and nothing else? That is the only failure a release window can produce, and the only one waivable.
///
/// Three conditions, and each rules out a failure that is NOT a release window:
///
///   1. EVERY error is an NU1102 ("the feed does not carry this exact id@version"). An FS0039 from a
///      call site the pinned package does not export, an NU1101 typo'd id, an NU1603 upward resolution
///      — any of those and the waiver is off. They are the failures this test exists to catch.
///
///   2. At least one NU1102 actually NAMES a pinned package, so an output carrying no identifiable pin
///      diagnostic at all — a probe TIMEOUT, say, which reports no diagnostics whatsoever — cannot waive
///      by vacuous `forall`. "Nothing to check" and "checked, and it's fine" must not share a verdict
///      (the fails-open shape of FS-GG/.github#266, which this file's own header forbids).
///
///   3. Every NU1102 that NAMES an FS.GG.* package names ONLY FS.GG.UI.* ids, at EXACTLY the current
///      $(FsGgUiVersion). This is the axis bound, and the restore hands it to us for free: an unpublished
///      FS.GG.Audio.* pin — a real defect even on the commit that bumped it, since Audio publishes from
///      another repo — produces an NU1102 naming FS.GG.Audio.*, and the waiver is off.
///
/// NuGet prefixes an NU1102's CONTINUATION lines with the code as well, so ONE unresolved pin arrives as
/// three `error NU1102:` lines: the header that names id and version, then `  - Found N version(s)…` and
/// `  - Versions from …/library-packs were not considered`, neither of which names a package. Those are
/// elaboration of the header above them, so condition 3 is asked only of the lines that name a package.
/// Asking it of every NU1102 line lets a NuGet detail line veto the waiver — which is precisely what the
/// first cut of this predicate did, and it is why the bound is written against `namedPins`.
///
/// It fails CLOSED: if NuGet's diagnostics ever stop matching, the waiver does not fire and this test is
/// red in the release window exactly as it is today. That is the safe direction to be wrong in.
let private failedOnlyOnUnpublishedUiPin (output: string) (uiPin: string) =
    let errorLines =
        output.Replace("\r\n", "\n").Split('\n')
        |> Array.filter errorCodeRegex.IsMatch

    let unresolvedPins = errorLines |> Array.filter (fun l -> l.Contains "NU1102")

    let namedPins =
        unresolvedPins
        |> Array.map (fun line -> line, fsGgIdRegex.Matches line |> Seq.map (fun m -> m.Value) |> List.ofSeq)
        |> Array.filter (fun (_, ids) -> not ids.IsEmpty)

    // 1. nothing failed except unresolved pins ...
    unresolvedPins.Length = errorLines.Length
    // 2. ... the pins are really what failed (never waive on an empty diagnostic set) ...
    && not (Array.isEmpty namedPins)
    // 3. ... and every pin named is the UI axis, at the version THIS commit pins.
    && namedPins
       |> Array.forall (fun (line, ids) ->
           ids |> List.forall (fun id -> id.StartsWith("FS.GG.UI.", StringComparison.Ordinal))
           && line.Contains uiPin)

// ---------------------------------------------------------------------------------------------
// The pin-grounded proof: compile the call sites against the RESTORED pinned packages.
// ---------------------------------------------------------------------------------------------

/// A stalled restore must fail, not hang. Generous enough for a cold restore on a slow runner.
let private probeTimeoutMs = 6 * 60 * 1000

/// Probe-private packages folder, OUTSIDE the throwaway work dir so a cold restore is paid once per
/// machine rather than on every test run. It is still not the machine's global packages folder — the
/// point of the isolation is that nothing this repo `pack`s locally can ever be resolved from here;
/// only nuget.org can populate it, and a published (id, version) is immutable, so reuse is safe.
let private probePackagesDir =
    Path.Combine(Path.GetTempPath(), "fsgg-pinned-api-probe-packages")

let private runProbeBuild () =
    let workDir = Path.Combine(Path.GetTempPath(), "fsgg-pinned-api-probe-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory workDir |> ignore

    try
        // The namespaces actually called, each one a package to restore at its axis pin.
        let packages =
            callSites
            |> List.choose callNamespace
            |> List.distinct
            |> List.sort

        let references =
            packages
            |> List.map (fun id -> $"    <PackageReference Include=\"{id}\" Version=\"{pinFor id}\" />")
            |> String.concat "\n"

        // The probe must see what a REAL scaffolded product sees: the PUBLISHED package on nuget.org.
        //
        // Restoring from the ambient NuGet cache would defeat the whole test. This repo's own
        // `dotnet pack` writes locally-built FS.GG.* packages into the machine's global packages
        // folder, and a locally-packed 0.8.0 carries whatever was in `src/` at pack time — including
        // the very seam the published 0.8.0 might not have. Resolve against that and the probe goes
        // GREEN precisely when the published pin is missing the API, which is the failure it exists
        // to catch. So: `<clear />` the sources down to nuget.org, and restore into a probe-local
        // packages folder that no local pack can have seeded.
        let nugetConfig =
            """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"""

        // Built OUTSIDE the repo tree so the repo's Directory.Build.props / central package
        // management / locked-restore rules do not apply to the probe.
        //
        // NU1603 (the pin does not exist, so NuGet quietly resolved UPWARD to the nearest version
        // that does) and NU1101/NU1102 (no such package/version at all) are ERRORS here, exactly as
        // in scripts/validate-template-payload-pins.fsx. Without that, a nonexistent pin would
        // silently restore a NEWER package that does contain the API, and the probe would prove the
        // opposite of what it claims.
        let project =
            $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <RestorePackagesPath>{probePackagesDir}</RestorePackagesPath>
    <WarningsAsErrors>NU1603;NU1101;NU1102;NU1608</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Probe.fs" />
{references}
  </ItemGroup>
</Project>
"""

        // `nameof` is a COMPILE-TIME symbol resolution: it needs no value, no type instantiation and
        // no generic instantiation (so a generic entry point like `Viewer.runApp<'model,'msg>` does
        // not trip the value restriction), and it fails to compile if the pinned package does not
        // export the member. That is exactly the question, and nothing more.
        let probe = StringBuilder()
        probe.AppendLine("module Probe").AppendLine() |> ignore

        for ns in packages do
            probe.AppendLine($"open {ns}") |> ignore

        probe.AppendLine().AppendLine("let private entryPointsTheTemplateCalls : string list =").AppendLine("    [") |> ignore

        for call in callSites |> List.sortBy (fun c -> c.Module, c.Member) do
            probe.AppendLine($"      nameof {call.Module}.{call.Member}") |> ignore

        probe.AppendLine("    ]") |> ignore

        File.WriteAllText(Path.Combine(workDir, "NuGet.config"), nugetConfig)
        File.WriteAllText(Path.Combine(workDir, "Probe.fsproj"), project)
        File.WriteAllText(Path.Combine(workDir, "Probe.fs"), probe.ToString())

        let psi = ProcessStartInfo("dotnet", "build Probe.fsproj -c Release -m:1 --nologo")
        psi.WorkingDirectory <- workDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        match Process.Start psi with
        | null -> failwith "could not start 'dotnet' to build the pinned-API probe"
        | started ->
            use proc = started

            // Both pipes are drained CONCURRENTLY. Reading one to the end before touching the other
            // deadlocks the moment the child fills the pipe it is NOT being read from — the child
            // blocks writing, the parent blocks reading, and neither moves. `dotnet build` emits
            // plenty on both, and this test is default-on, so that would hang the gate rather than
            // fail it.
            let output = StringBuilder()

            let append (data: string | null) =
                match data with
                | null -> ()
                | text -> lock output (fun () -> output.AppendLine text |> ignore)

            proc.OutputDataReceived.Add(fun e -> append e.Data)
            proc.ErrorDataReceived.Add(fun e -> append e.Data)
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            // And the wait is BOUNDED. The accepted cost of running this by default is that a feed
            // outage can turn the gate RED; an unbounded wait would instead let a stalled restore
            // HANG it until the job timeout, which is strictly worse and is not what was signed up
            // for. A timeout is reported as a failure with its own message, not as a missing API.
            if proc.WaitForExit probeTimeoutMs then
                // Let the async readers flush before the buffer is read.
                proc.WaitForExit()
                proc.ExitCode, lock output (fun () -> output.ToString())
            else
                try proc.Kill true with _ -> ()

                let minutes = probeTimeoutMs / 60_000

                -1,
                $"the pinned-API probe did not finish within {minutes} minute(s) — most likely the \
                  restore from nuget.org stalled. This is an infrastructure failure, NOT a missing \
                  API.\n\n{lock output (fun () -> output.ToString())}"
    finally
        try Directory.Delete(workDir, true) with _ -> ()

// ---------------------------------------------------------------------------------------------
// #589 — the same question, asked of the DOCS instead of Program.fs.
//
// THE GAP #504 LEFT. Everything above asks "can the template's CODE call the pin?". Nothing asked
// whether the template's DOCS only NAME the pin. They are different subjects with the same failure:
// a scaffolded product pins $(FsGgUiVersion), restores THAT package, and a reader who copies a skill's
// `fsharp` block verbatim gets a hard build error.
//
// It was invisible from inside this repo BY CONSTRUCTION, because every doc check here resolves
// against `src/`, where the symbol exists: SkillParity's `UnresolvedApiSymbol` resolves fenced symbols
// against `src/**` plus the surface baselines; S-DOC and M-MIR assert against `src/`. Source has the
// symbol. The package a reader restores does not. Every gate is green and the shipped doc is wrong.
// #550 is the instance (`Persistence.interpretRecordOnly`, taught by two shipped docs, exported by no
// published FS.GG.UI.Canvas — FS.GG.Game#163 could not adopt it), and it was fixed BY HAND. This is
// the rule, so the next one cannot be.
//
// Every OTHER doc gate in this repo is a COHERENCE check (doc => src). This is a REACHABILITY check
// (doc => what a consumer can BIND), and the distinction is the entire bug: #445's rename was coherent
// with `src/` on the commit that made it, and wrong for every reader on a released package from that
// moment until #550.
//
// THE ORACLE IS THE PACKAGE, NOT THE MIRROR. The mirror is a defendant here, not a witness: it tracks
// `src/`, so it advertises exactly the unreleased API this rule exists to find (two of the three
// findings below are IN it). The mirror decides only WHICH MODULES ARE FRAMEWORK — a closed-world
// question `src/` can answer — and the restored package decides what those modules EXPORT.
//
// WHY THIS READS METADATA WHERE THE PROBE ABOVE COMPILES. The probe asks one yes/no question about a
// whole file; this rule needs a PER-SYMBOL verdict, because it carries a ledger and must name the doc
// site and line of each violation. `PEReader` answers exactly that, is IN-BOX (no PackageReference —
// the objection the header raises against `MetadataLoadContext` does not reach it), and never loads an
// assembly, so it cannot trip over the unresolved dependencies that rule out `Assembly.LoadFrom`.
// ---------------------------------------------------------------------------------------------

let private productSkillsRoot = repoPath "template/product-skills"
let private docLedgerRel = "tests/Build.Tests/pinned-api-doc-ledger.txt"
let private docLedgerPath = repoPath docLedgerRel

/// The TFM a scaffolded product compiles against — the same one `runProbeBuild` writes into its probe.
let private templateTfm = "net10.0"

/// Modules the SCAFFOLD MATERIALIZES INTO THE PRODUCT'S OWN SOURCE — `template/base/src/**` and the
/// profile fragments. A reader binds these from a file the template WROTE them, not from a package, so
/// they must never be judged against the pin.
///
/// This is not hypothetical tidiness: it is the one false-positive class this rule actually has, and it
/// exists because the package oracle is WIDER than the src one. `fs-gg-model-swap` teaches
/// `Geometry.toRect`, and `template/fragments/vec2/src/Product/Vec2.fs` defines `module Geometry` with
/// `toRect` — while FS.GG.Game.Core ALSO exports a `Geometry` module, which does NOT. Judging the
/// scaffold's own module against the framework's same-named one reports a defect in correct guidance.
/// `productModules` above covers only `template/base/src/Product/*.fs`, so it does not see the fragment;
/// it is deliberately left alone (it feeds the #504 rule) and this widens the exemption for #589 alone.
///
/// A module ALIAS IS NOT A MATERIALIZED MODULE, and the `$` anchor is what says so. `View.fs` opens with
/// eight of them —
///
///     module Button = FS.GG.UI.Controls.Typed.Button
///
/// — which do not DEFINE `Button`, they POINT AT the framework's. Reading them as scaffold-local exempts
/// `Button`, `Stack`, `DataGrid`, `TextBlock`, `TextBox`, `RichText`, `LineChart` and `GraphView` from
/// this rule outright, and with them every `Button.create` / `DataGrid.visibleRange` the widget skills
/// teach: a silent hole through the middle of the surface most likely to drift, reporting green because
/// it checked nothing. That is the fails-open shape (FS-GG/.github#266) this file exists to refuse, so
/// an aliased module is judged against the pin exactly like the framework module it resolves to.
///
/// `module X` and `module X =` (a real definition) match; `module X = A.B.C` (an alias) does not.
let private scaffoldModuleRegex =
    Regex(@"^module\s+(?:[\w.]+\.)?(?<name>\w+)\s*(?:=\s*)?$", RegexOptions.Compiled)

let private scaffoldModules =
    [ repoPath "template/base/src"; repoPath "template/fragments" ]
    |> List.filter Directory.Exists
    |> List.collect (fun root ->
        Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories) |> List.ofSeq)
    |> List.collect (fun path -> File.ReadAllLines path |> List.ofArray)
    |> List.choose (fun line ->
        let m = scaffoldModuleRegex.Match line
        if m.Success then Some m.Groups.["name"].Value else None)
    |> Set.ofList

/// One `Module.member` a SHIPPED template doc names, and where it says it.
type DocSymbol =
    { Doc: string // repo-relative, so a failure is clickable
      Line: int
      Module: string
      Member: string }

let private docKey (s: DocSymbol) = $"{s.Doc}::{s.Module}.{s.Member}"

/// Judged only if the mirror declares the module (=> framework, not product or FSharp.Core) AND the
/// scaffold does not materialize one of that name. A module NOBODY declares is product-local or
/// pseudo-code in an example, and is not this rule's business — the same closed world SkillParity uses.
let private isJudgedDocModule (qualifier: string) (moduleName: string) =
    isFrameworkCall qualifier moduleName && not (scaffoldModules.Contains moduleName)

/// `Module.member` inside the ```fsharp fences of a product skill — the block a reader COPIES, which is
/// what makes it the sharpest subject. Prose is deliberately not matched: naming an API in a sentence
/// ("`interpret` is deprecated") is not teaching a reader to call it.
let private skillFenceSymbols =
    Directory.EnumerateFiles(productSkillsRoot, "*.md", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
        let mutable inFence = false

        (File.ReadAllText path |> eraseKeepingLines).Split('\n')
        |> Array.mapi (fun i line -> i + 1, line)
        |> Array.collect (fun (lineNo, raw) ->
            let opener = raw.TrimStart()

            if opener.StartsWith("```", StringComparison.Ordinal) then
                // Close ANY fence; open only an fsharp one. A ```console block's closing fence is seen
                // while `inFence` is false and correctly opens nothing.
                inFence <-
                    not inFence
                    && opener.TrimStart('`').TrimStart().StartsWith("fsharp", StringComparison.OrdinalIgnoreCase)

                [||]
            elif not inFence then
                [||]
            else
                callRegex.Matches(stripCommentsAndStrings raw)
                |> Seq.map (fun m ->
                    m.Groups.[1].Value,
                    { Doc = rel
                      Line = lineNo
                      Module = m.Groups.[2].Value
                      Member = m.Groups.[3].Value })
                |> Array.ofSeq))
    |> List.ofSeq

/// The public `val`s of the SHIPPED api-surface mirror, qualified by the INNERMOST module that declares
/// them. The mirror is what `docs/scaffold-map.md` designates a product's authoritative signature set —
/// so a `val` it declares that the pinned package does not export is a signature a product author reads,
/// at length, and cannot call.
///
/// INNERMOST, by indent, and not the column-0 module `parseMirrorFile` tracks for #504. A nested `module
/// Perf` inside `module ControlsElmish` is its OWN type in the assembly (`ControlsElmish+Perf`) and is
/// its own name at a call site (`Perf.runScript`) — so attributing its `val`s to the parent invents
/// `ControlsElmish.runScript`, which nothing exports and no doc names. That misreading put nine
/// phantom violations on the first run of this rule. `parseMirrorFile` is deliberately left alone: it
/// feeds #504, whose subject IS the column-0 entry points `Program.fs` writes.
///
/// `val internal` and `module internal` are not product surface — that is #585's subject, not this
/// rule's — and are not judged. Both exclusions are load-bearing, and the SECOND one is subtle: an
/// internal module must still be TRACKED, or its `val`s fall through to the nearest public ancestor.
/// `module internal Coalescing` inside `module ControlsElmish` is exactly that shape, and refusing to
/// push it invented four violations (`ControlsElmish.isCoalescibleSample`, …) that name nothing anyone
/// exports and no product can reach. Track it; mark it; judge nothing under it.
let private mirrorModuleRegex =
    Regex(
        @"^(?<indent>\s*)(?:\[<[^>]*>\]\s*)*module\s+(?<access>internal\s+|private\s+)?(?<name>\w+)",
        RegexOptions.Compiled
    )

let private mirrorValRegex =
    Regex(@"^(?<indent>\s*)val\s+(?!internal\b)(?:inline\s+)?(?<name>[a-z]\w*)\s*:", RegexOptions.Compiled)

let private mirrorValSymbols =
    Directory.EnumerateFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')

        // (indent, name, isInternal), innermost first. A line at indent N closes every module opened at
        // indent >= N, which is what puts a parent-level `val` back under its parent after a nested
        // module ends.
        let mutable stack: (int * string * bool) list = []

        let closeTo (indent: int) =
            stack <- stack |> List.skipWhile (fun (i, _, _) -> i >= indent)

        File.ReadAllLines path
        |> Array.mapi (fun i line -> i + 1, line)
        |> Array.choose (fun (lineNo, line) ->
            let moduleMatch = mirrorModuleRegex.Match line
            let valMatch = mirrorValRegex.Match line

            if line.StartsWith("namespace", StringComparison.Ordinal) then
                stack <- []
                None
            elif moduleMatch.Success then
                let indent = moduleMatch.Groups.["indent"].Value.Length
                closeTo indent

                stack <-
                    (indent, moduleMatch.Groups.["name"].Value, moduleMatch.Groups.["access"].Success)
                    :: stack

                None
            elif valMatch.Success then
                let indent = valMatch.Groups.["indent"].Value.Length
                closeTo indent

                match stack with
                // An internal ANYWHERE up the chain makes everything beneath it internal.
                | _ when stack |> List.exists (fun (_, _, isInternal) -> isInternal) -> None
                | (_, owner, _) :: _ ->
                    Some(
                        "",
                        { Doc = rel
                          Line = lineNo
                          Module = owner
                          Member = valMatch.Groups.["name"].Value }
                    )
                | [] -> None
            else
                None))
    |> List.ofSeq

/// Both shipped doc surfaces, reduced to the symbols this rule may judge.
let private docSymbols =
    List.append skillFenceSymbols mirrorValSymbols
    |> List.filter (fun (qualifier, s) -> isJudgedDocModule qualifier s.Module)
    |> List.map snd
    |> List.distinctBy docKey
    |> List.sortBy (fun s -> s.Doc, s.Line)

/// Every `FS.GG.*` package a scaffolded product pins — read from the props file, which IS the set it
/// restores. This is the authority for turning a mirrored NAMESPACE into a PACKAGE, and the two are not
/// the same string: `runProbeBuild` above may assume they are, because the namespaces `Program.fs` calls
/// all happen to be package ids, but the mirror at large declares SUB-namespaces that are not —
/// `FS.GG.UI.Controls.Typed`, `FS.GG.UI.Controls.Elmish.Authoring`, `FS.GG.UI.Themes.Default.Theming`.
/// Referencing one of those as a package is an NU1101 for a package that was never supposed to exist.
let private pinnedPackageIds =
    Regex.Matches(File.ReadAllText packagesPropsPath, @"<PackageVersion\s+Include=""(FS\.GG\.[^""]+)""")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Seq.distinct
    |> List.ofSeq

/// The package that SHIPS a namespace: the longest pinned id that prefixes it. A namespace no pinned
/// package covers is a hole in the oracle, so it raises rather than being dropped — silently skipping it
/// would excuse every doc symbol underneath it.
let private packageForNamespace (ns: string) =
    pinnedPackageIds
    |> List.filter (fun id -> ns = id || ns.StartsWith(id + ".", StringComparison.Ordinal))
    |> List.sortByDescending String.length
    |> List.tryHead
    |> Option.defaultWith (fun () ->
        failwith
            $"the api-surface mirror declares `namespace {ns}`, and NO FS.GG.* package pinned in \
              {packagesPropsRel} ships it. The doc-vs-pin oracle cannot restore it, and skipping it would \
              EXCUSE every doc symbol it declares.")

/// The packages the docs actually talk about — derived from the mirror and the props, never hardcoded,
/// so the set cannot rot as the framework grows.
let private docPackages =
    docSymbols
    |> List.choose (fun s ->
        frameworkModulesByName
        |> Map.tryFind s.Module
        |> Option.bind (fun candidates ->
            candidates
            |> List.tryFind (fun m -> m.Members.Contains s.Member)
            |> Option.orElse (List.tryHead candidates))
        |> Option.map (fun m -> packageForNamespace m.Namespace))
    |> List.distinct
    |> List.sort

// ---------------------------------------------------------------------------------------------
// The oracle: what the PINNED packages actually export.
// ---------------------------------------------------------------------------------------------

/// Simple module name -> the members the PUBLISHED assembly exports.
///
/// An F# module compiles to a STATIC class — `abstract` AND `sealed`. A union, a record or a class is
/// never both, so this keys modules and nothing else, which is what a doc's `Module.member` means. Two
/// spellings have to be undone: F# suffixes a module whose name collides with a type's (`module Scene`
/// beside `type Scene` becomes `SceneModule`), and generic arity is mangled (``Foo`1``) — a doc writes
/// neither. Property accessors are recorded under their bare name as well as their `get_`/`set_` one.
let private readModuleSurface (dll: string) =
    use stream = File.OpenRead dll
    use pe = new PEReader(stream)
    let md = pe.GetMetadataReader()

    [ for handle in md.TypeDefinitions do
        let td = md.GetTypeDefinition handle
        let visibility = td.Attributes &&& TypeAttributes.VisibilityMask

        let isPublic =
            visibility = TypeAttributes.Public || visibility = TypeAttributes.NestedPublic

        let isFSharpModule =
            td.Attributes.HasFlag TypeAttributes.Abstract
            && td.Attributes.HasFlag TypeAttributes.Sealed

        if isPublic && isFSharpModule then
            let raw = md.GetString td.Name
            let withoutArity = match raw.IndexOf '`' with | -1 -> raw | i -> raw.Substring(0, i)

            let name =
                if withoutArity.EndsWith("Module", StringComparison.Ordinal) && withoutArity.Length > 6 then
                    withoutArity.Substring(0, withoutArity.Length - 6)
                else
                    withoutArity

            for methodHandle in td.GetMethods() do
                let m = md.GetMethodDefinition methodHandle

                if (m.Attributes &&& MethodAttributes.MemberAccessMask) = MethodAttributes.Public then
                    let memberName = md.GetString m.Name
                    yield name, memberName

                    for prefix in [ "get_"; "set_" ] do
                        if memberName.StartsWith(prefix, StringComparison.Ordinal) then
                            yield name, memberName.Substring prefix.Length ]

/// Restore the pinned packages and read their exported module surface. `Error` — never an empty map —
/// if anything at all went wrong: an oracle that silently knows nothing would report every doc symbol
/// as unresolved, and an oracle that silently knows nothing about ONE package would excuse every symbol
/// in it. Both are the fails-open shape (FS-GG/.github#266) this file's header forbids.
let private readPinnedSurface () : Result<Map<string, Set<string>>, string> =
    let workDir = Path.Combine(Path.GetTempPath(), "fsgg-doc-pin-probe-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory workDir |> ignore

    try
        let references =
            docPackages
            |> List.map (fun id -> $"    <PackageReference Include=\"{id}\" Version=\"{pinFor id}\" />")
            |> String.concat "\n"

        // Same isolation, and for the same reason, as `runProbeBuild`: `<clear />` down to nuget.org so
        // nothing this repo `dotnet pack`s locally can satisfy the restore. A locally-packed 0.9.0
        // carries whatever was in `src/` at pack time — INCLUDING the very symbols this rule exists to
        // catch — so resolving against the ambient cache would make it green precisely when it should
        // be red.
        let nugetConfig =
            """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"""

        let project =
            $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <RestorePackagesPath>{probePackagesDir}</RestorePackagesPath>
    <WarningsAsErrors>NU1603;NU1101;NU1102;NU1608</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
{references}
  </ItemGroup>
</Project>
"""

        File.WriteAllText(Path.Combine(workDir, "NuGet.config"), nugetConfig)
        File.WriteAllText(Path.Combine(workDir, "Probe.fsproj"), project)

        let psi = ProcessStartInfo("dotnet", "restore Probe.fsproj --nologo")
        psi.WorkingDirectory <- workDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        match Process.Start psi with
        | null -> Error "could not start 'dotnet' to restore the doc-vs-pin oracle"
        | started ->
            use proc = started
            let output = StringBuilder()

            let append (data: string | null) =
                match data with
                | null -> ()
                | text -> lock output (fun () -> output.AppendLine text |> ignore)

            proc.OutputDataReceived.Add(fun e -> append e.Data)
            proc.ErrorDataReceived.Add(fun e -> append e.Data)
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            if not (proc.WaitForExit probeTimeoutMs) then
                try proc.Kill true with _ -> ()
                Error $"the doc-vs-pin restore did not finish within {probeTimeoutMs / 60_000} minute(s) — most \
                        likely the restore from nuget.org stalled. This is an infrastructure failure, NOT a \
                        missing API.\n\n{lock output (fun () -> output.ToString())}"
            else
                proc.WaitForExit()
                let text = lock output (fun () -> output.ToString())

                if proc.ExitCode <> 0 then
                    Error $"the doc-vs-pin restore FAILED, so the pinned surface is unknown and no doc symbol \
                            can be judged against it.\n\n{text}"
                else
                    // Every pinned package must yield exactly one assembly. A package that restored but
                    // whose lib/ we cannot find is an oracle with a hole in it, and a hole excuses every
                    // symbol that would have landed in it.
                    let missing = ResizeArray<string>()
                    let surface = Collections.Generic.Dictionary<string, Collections.Generic.HashSet<string>>()

                    for packageId in docPackages do
                        let dir =
                            Path.Combine(probePackagesDir, packageId.ToLowerInvariant(), pinFor packageId, "lib")

                        // The template's REAL TFM, and the one a scaffolded product compiles against — so
                        // it is the surface to judge, not merely the "newest" folder. Ordering the paths
                        // lexically would be worse than arbitrary: `netstandard2.0` sorts ABOVE `net10.0`
                        // (and so does `net9.0`), so "take the last" reliably picks the wrong one the day a
                        // package multi-targets. Ask for the TFM by name; fall back only if it has none.
                        let dll =
                            if Directory.Exists dir then
                                let byTfm = Path.Combine(dir, templateTfm, $"{packageId}.dll")

                                if File.Exists byTfm then
                                    Some byTfm
                                else
                                    Directory.EnumerateFiles(dir, $"{packageId}.dll", SearchOption.AllDirectories)
                                    |> Seq.tryHead
                            else
                                None

                        match dll with
                        | None -> missing.Add $"{packageId} {pinFor packageId}"
                        | Some path ->
                            for (moduleName, memberName) in readModuleSurface path do
                                if not (surface.ContainsKey moduleName) then
                                    surface.[moduleName] <- Collections.Generic.HashSet<string>()

                                surface.[moduleName].Add memberName |> ignore

                    if missing.Count > 0 then
                        let names = String.Join(", ", missing)

                        Error $"restored, but no lib assembly was found under {probePackagesDir} for: \
                                {names}. The oracle would have a hole in it, and a hole EXCUSES every doc \
                                symbol that belongs in it — fail closed instead.\n\n{text}"
                    elif surface.Count = 0 then
                        Error "the pinned packages exported ZERO F# modules — the metadata reader has stopped \
                               seeing the surface. That is a defect in this test, not an empty framework."
                    else
                        surface
                        |> Seq.map (fun kvp -> kvp.Key, Set.ofSeq kvp.Value)
                        |> Map.ofSeq
                        |> Ok
    finally
        try Directory.Delete(workDir, true) with _ -> ()

/// Restored ONCE per run, not once per test. Two tests below ask for the pinned surface (the rule, and
/// the ledger's staleness check), and a restore is the expensive, network-bound half of this file — so
/// asking twice would double the gate's exposure to nuget.org for an answer that cannot have changed
/// between them. `Lazy` is thread-safe by default, which matters: Expecto runs the list in parallel.
let private pinnedSurface = lazy (readPinnedSurface ())

/// Does the PINNED package export what the doc names?
let private resolvesInPin (pinned: Map<string, Set<string>>) (s: DocSymbol) =
    match pinned |> Map.tryFind s.Module with
    | Some members -> members.Contains s.Member
    | None ->
        // The mirror calls this module framework, and the pinned package has no module of that name at
        // all. That is the rule's subject at its sharpest — an ENTIRE module a product cannot reach —
        // so it is a violation, not an exemption.
        false

// ---------------------------------------------------------------------------------------------
// The ledger: violations that are KNOWN, and whose fix is somebody's declared work.
//
// A gate landing on a repo that already violates it has two honest options, and "quietly narrow the
// rule until it is green" is not one of them. This is S-DOC's idiom (`surface-doc-ledger.txt`), for
// S-DOC's reason: a gap must be A DECISION SOMEBODY MADE rather than an omission nobody noticed.
//
// It is a RATCHET, not a dumping ground, and the two anti-rot rules below are what make it one:
//   * a ledger entry the pin NOW exports  -> the release landed. Delete the line.  (stale)
//   * a ledger entry no doc names anymore -> the doc was fixed. Delete the line.   (phantom)
//
// The phantom rule is the load-bearing one, and it is why the ledger cannot re-open #550. Without it a
// dead entry would sit there excusing its symbol forever — so when #587 publishes `interpretRecordOnly`
// and someone re-applies the spelling EARLY, the stale entry would wave it straight back through. That
// is the precise re-opening this item exists to prevent, so the ledger refuses to outlive its subjects.
// ---------------------------------------------------------------------------------------------

/// `<doc-path>::<Module.member>` per line; `#` comments and blanks ignored.
let private docLedger =
    if not (File.Exists docLedgerPath) then
        Set.empty
    else
        File.ReadAllLines docLedgerPath
        |> Array.map (fun l -> l.Trim())
        |> Array.filter (fun l -> l <> "" && not (l.StartsWith("#", StringComparison.Ordinal)))
        |> Set.ofArray

// ---------------------------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------------------------

[<Tests>]
let templateConsumesPinnedApiTests =
    testList "Template consumes the pinned framework API (#504)" [

        // The extractor is the load-bearing part of every assertion below it. If it silently matches
        // nothing, every other test in this list passes VACUOUSLY — green because it checked
        // nothing, which is the failure this whole item exists to stop.
        test "the template's Program.fs calls framework entry points (extractor is not vacuous)" {
            Expect.isNonEmpty frameworkModules "the bundled api-surface mirror declares framework modules"

            Expect.isNonEmpty
                callSites
                $"framework entry points were extracted from {programPath}. Zero call sites means the \
                  extractor has stopped seeing the template's framework usage — that is a defect in \
                  this test, not a passing template."
        }

        // The seam from #429 — the concrete API that existed in `src/`, shipped in no package the
        // template pinned, and was unreachable from every scaffolded product for the life of 0.8.0.
        // If a refactor ever stops the extractor from seeing the viewer launch calls, this is the
        // test that says so out loud rather than quietly reducing the check to nothing.
        //
        // #436 completes that story, so the list below changed shape. `Viewer.runApp` and
        // `ControlsElmish.runInteractiveApp` — the two SINKLESS overloads — are no longer called by
        // the template at all: every profile that opens a window now launches through the
        // audio-carrying sibling, because #429's seam existing in `src/` was only half the fix while
        // the scaffold still launched past it. These four ARE the launch seam now, and asserting the
        // sinkless pair here would be asserting the defect.
        test "the viewer launch seam is among the extracted call sites" {
            let extracted = callSites |> List.map (fun c -> $"{c.Module}.{c.Member}") |> Set.ofList
            let rendered = extracted |> Set.toList |> String.concat ", "

            [ "Viewer.runAppWithAudio" // game / sample-pack, default launch
              "Viewer.runAppWithWindowBehaviorAndAudio" // game / sample-pack, --window-* launch
              "ControlsElmish.runInteractiveAppWithAudio" // app, default launch
              "ControlsElmish.runInteractiveAppWithWindowBehaviorAndAudio" ] // app, --window-* launch
            |> List.iter (fun entryPoint ->
                Expect.isTrue
                    (extracted.Contains entryPoint)
                    $"'{entryPoint}' is one of the framework entry points the template's Program.fs calls \
                      (extracted: {rendered})")

            // And the sinkless overloads are NOT called. This is the #436 invariant stated where it can
            // be enforced against the real extractor: a scaffolded product that launches through
            // `runApp`/`runInteractiveApp` discards every `PlayAudio` batch its cue seam produces, and
            // does so silently — it compiles, it runs, it is simply mute. That is the exact defect, and
            // it is invisible to every other check in this file.
            [ "Viewer.runApp"; "ControlsElmish.runInteractiveApp" ]
            |> List.iter (fun sinkless ->
                Expect.isFalse
                    (extracted.Contains sinkless)
                    $"the template must not launch through the sink-discarding '{sinkless}' — every windowed \
                      profile carries the audio sink (#436) (extracted: {rendered})")
        }

        // Offline necessary-but-not-sufficient condition. It CANNOT catch #429 (the mirror tracks
        // `src/`, so it advertises the seam the pin lacks) — the pin-grounded test below is what
        // does. It catches the other direction: a call site that names nothing at all.
        test "every framework entry point the template calls exists in the bundled mirror" {
            let unresolved =
                callSites
                |> List.filter (fun c -> (owningModule c).IsNone)
                |> List.map (fun c -> $"{c.Module}.{c.Member} (Program.fs:{c.Line})")

            Expect.equal
                (String.concat "; " unresolved)
                ""
                "every framework entry point called by the template's Program.fs is declared in the \
                 bundled api-surface mirror"
        }

        // THE assertion #504 asks for, and it runs BY DEFAULT — including on the gate.
        //
        // It is deliberately NOT opt-in. The sibling restore proofs
        // (scripts/validate-template-payload-pins.fsx) are gated behind an opt-in env var that the
        // WORKFLOW sets; this test cannot rely on that, because switching it on would mean editing
        // gate.yml. An opt-in check that nothing opts into is a check that never runs — it would
        // report green having verified nothing, which is the exact fails-open shape (#266) that let
        // #429 sit unreachable for the life of 0.8.0. #504 exists to fire ON the PR, so it fires.
        //
        // The cost is honest and named: this restores from nuget.org, so the gate's test step now
        // depends on the feed being up. FS_GG_SKIP_TEMPLATE_PINNED_API=1 skips it for offline work —
        // an explicit, visible opt-OUT rather than a silent default-off.
        //
        // And it DEFERS, rather than failing, in the one window where the pin cannot resolve by
        // construction: the release PR that bumps $(FsGgUiVersion) to the version it is about to
        // publish (#543 — see the RELEASE-PENDING note in the header for the bounds).
        testCase "every framework entry point the template calls exists in the PINNED package" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API" with
            | null | "" ->
                let exitCode, output = runProbeBuild ()
                let uiPin = readAxis uiAxis
                let audioPin = readAxis "FsGgAudioVersion"

                // The verdict the probe was BUILT to deliver. Everything below only decides whether a
                // failure is the release window or the real thing — so on a green probe none of it runs,
                // and the common case pays for no git call and no extra analysis.
                let probeFailed =
                    $"the template's framework call sites compile against the PINNED packages \
                      (FsGgUiVersion={uiPin}, FsGgAudioVersion={audioPin}). \
                      A failure here means the framework has grown public API that a scaffolded product \
                      CANNOT reach — the #429/#492 class. Either the seam is unreleased (cut the release, \
                      then bump the pin) or the template calls API that no longer exists.\n\n{output}"

                if exitCode = 0 then
                    () // The probe ran and the template consumes the pin. The only pass.
                elif not (failedOnlyOnUnpublishedUiPin output uiPin) then
                    // Not the release window: the probe failed for a reason a release does not produce.
                    failtest probeFailed
                elif releaseLane then
                    failtest
                        $"RELEASE LANE: $(FsGgUiVersion)={uiPin} is not on the feed, and this job gates the \
                          PUBLISH — so those packages are DUE, not pending. RELEASE-PENDING does not apply \
                          here; a missing package at publish time is drift.\n\n{probeFailed}"
                else
                    match bumpedInCommitUnderTest packagesPropsRel uiAxis with
                    | Error why -> failtest $"{why}\n\n{probeFailed}"

                    | Ok false ->
                        failtest
                            $"the feed does not carry the FS.GG.UI.* packages at $({uiAxis})={uiPin}, and this \
                              commit did NOT bump $({uiAxis}) — so this is NOT the release window. The pin is \
                              stale or typo'd, or a release half-failed. Publish it, or re-pin onto a version \
                              the feed carries.\n\n{probeFailed}"

                    | Ok true ->
                        skiptest
                            $"RELEASE-PENDING: this commit bumps $({uiAxis}) to {uiPin}, and the FS.GG.UI.* \
                              packages it pins are not on nuget.org yet — the merge of this very commit is what \
                              publishes them (release-tags.yml). The pin-grounded proof therefore CANNOT run and \
                              is DEFERRED to the publish; it is NOT passing. Still asserted on this commit: the \
                              extractor is non-vacuous, the viewer launch seam is called, every call site resolves \
                              in the bundled mirror, and the SAME restore reported no unresolved FS.GG.Audio.* / \
                              FS.GG.Game.* pin (FsGgAudioVersion={audioPin}) — those axes publish from other repos, \
                              so the waiver is bounded to $({uiAxis}) alone and would not have fired for them.\n\n{output}"

            | _ ->
                skiptest
                    "FS_GG_SKIP_TEMPLATE_PINNED_API is set — the pinned-package proof did NOT run. This \
                     check is default-on; skipping it means the template-vs-pin question is unanswered."

        // ---- #589: the shipped DOCS, against the same pin ------------------------------------

        // Same guard as the extractor test above, for the same reason: every assertion below is a
        // `forall` over these lists, so an extractor that silently matches nothing turns all of them
        // green having checked nothing (FS-GG/.github#266).
        test "the shipped docs name framework API (the doc extractor is not vacuous)" {
            Expect.isNonEmpty
                skillFenceSymbols
                $"`Module.member` symbols were extracted from the ```fsharp fences of {productSkillsRoot}. \
                  Zero means the fence extractor has stopped seeing the skills — a defect in this test, \
                  not a repo whose skills teach no API."

            Expect.isNonEmpty
                mirrorValSymbols
                "public `val`s were extracted from the shipped api-surface mirror."

            Expect.isNonEmpty
                docSymbols
                "at least one shipped doc symbol survives the framework/product filter and is judged."

            // The exemption is what keeps this rule off correct guidance, so it must not silently
            // evaporate: if the fragments ever stop being read, `Geometry.toRect` becomes a false
            // finding and someone "fixes" a doc that was right all along.
            Expect.isTrue
                (scaffoldModules.Contains "Geometry")
                $"the scaffold-materialized modules were read from template/base/src + template/fragments \
                  ({scaffoldModules.Count} found). `Geometry` must be among them — the scaffold writes \
                  `module Geometry` into the product (template/fragments/vec2/src/Product/Vec2.fs) while \
                  FS.GG.Game.Core ALSO exports one, and without this exemption the product's own module is \
                  judged against the framework's and correct guidance is reported as a defect."

            // And the exemption must not OVERREACH, which is the far more dangerous direction: it makes
            // the rule silently check less while still reporting green. `View.fs` ALIASES eight framework
            // modules (`module Button = FS.GG.UI.Controls.Typed.Button`); reading an alias as a
            // materialized module exempts the entire widget surface the skills teach.
            for aliased in [ "Button"; "Stack"; "DataGrid"; "TextBlock"; "TextBox"; "RichText"; "LineChart"; "GraphView" ] do
                Expect.isFalse
                    (scaffoldModules.Contains aliased)
                    $"`{aliased}` is ALIASED by the scaffold (`module {aliased} = FS.GG.UI.Controls.Typed.{aliased}` \
                      in template/base/src/Product/View.fs), not DEFINED by it — the alias resolves to the \
                      framework module, so every `{aliased}.member` a skill teaches must be judged against the \
                      pin. Treating it as scaffold-local exempts it, and the rule goes quietly blind to the \
                      widget surface while still reporting green."

            // The positive half of the same guard, asserted on the real subject rather than on the
            // exemption set: the widget symbols the skills teach must actually reach the rule.
            let judged = docSymbols |> List.map (fun s -> $"{s.Module}.{s.Member}") |> Set.ofList

            Expect.isTrue
                (judged.Contains "Button.create" && judged.Contains "DataGrid.visibleRange")
                "the widget symbols the product skills teach (`Button.create`, `DataGrid.visibleRange`) are \
                 among the symbols this rule judges. If they fall out, the exemption has overreached again \
                 and the rule is checking less than it claims."
        }

        // THE RULE (#589).
        //
        // Deferred in the same window, on the same bounds, as the probe above: in the release window the
        // pinned packages do not exist, so the oracle cannot be built. SKIPPED, NOT PASSED.
        testCase "every FS.GG.* symbol a shipped template doc names exists in the PINNED package" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API" with
            | null | "" ->
                let uiPin = readAxis uiAxis

                match pinnedSurface.Value with
                | Error why when failedOnlyOnUnpublishedUiPin why uiPin && not releaseLane ->
                    match bumpedInCommitUnderTest packagesPropsRel uiAxis with
                    | Ok true ->
                        skiptest
                            $"RELEASE-PENDING: this commit bumps $({uiAxis}) to {uiPin}, which nuget.org does \
                              not carry yet — merging it is what publishes it. The pinned surface cannot be \
                              read, so the doc-vs-pin rule is DEFERRED to the publish; it is NOT passing. \
                              (The docs may legitimately name API that only {uiPin} will export — which is \
                              exactly why this cannot be judged here.)\n\n{why}"
                    | Ok false ->
                        failtest
                            $"the feed does not carry the FS.GG.UI.* packages at $({uiAxis})={uiPin}, and this \
                              commit did NOT bump $({uiAxis}) — so this is NOT the release window. The pin is \
                              stale or typo'd.\n\n{why}"
                    | Error gitWhy -> failtest $"{gitWhy}\n\n{why}"

                | Error why -> failtest why

                | Ok pinned ->
                    let undeclared =
                        docSymbols
                        |> List.filter (fun s -> not (resolvesInPin pinned s))
                        |> List.filter (fun s -> not (docLedger.Contains(docKey s)))
                        |> List.map (fun s -> $"{s.Doc}:{s.Line}  {s.Module}.{s.Member}")

                    let rendered = String.concat "; " undeclared

                    Expect.isEmpty
                        undeclared
                        $"these symbols are named by a SHIPPED template doc and are exported by NO package a \
                          scaffolded product restores at $({uiAxis})={uiPin}. A reader who copies the block \
                          gets a hard build error — the #550 class, which every other doc gate in this repo is \
                          blind to because they all resolve against `src/`, where the symbol exists.\n\n\
                          Fix the DOC to name the spelling the released package exports (that is what #550 \
                          did), or — if the symbol is genuinely unreleased and the doc must wait for it — cut \
                          the release, bump $({uiAxis}), and re-apply the doc AFTER the publish. Declaring it \
                          in {docLedgerRel} is the third option, and it is for a violation whose fix is \
                          somebody's named, filed work.\n\n\
                          Undeclared: {rendered}"

            | _ ->
                skiptest
                    "FS_GG_SKIP_TEMPLATE_PINNED_API is set — the doc-vs-pin rule did NOT run."

        // Anti-rot 1 (stale). The release landed and the symbol is reachable now; the excuse has outlived
        // its reason. This is the rule that retires a ledger entry at exactly the right moment.
        testCase "no doc-ledger entry names a symbol the PINNED package now exports" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API", Set.isEmpty docLedger with
            | (null | ""), false ->
                match pinnedSurface.Value with
                | Error _ -> skiptest "the pinned surface could not be read — the rule above reports why."
                | Ok pinned ->
                    let stale =
                        docSymbols
                        |> List.filter (fun s -> docLedger.Contains(docKey s) && resolvesInPin pinned s)
                        |> List.map docKey

                    let rendered = String.concat "; " stale

                    Expect.isEmpty
                        stale
                        $"these are declared in {docLedgerRel} as naming API the pin does not carry — and the \
                          pinned package NOW EXPORTS them. The release landed; the ledger only shrinks. Delete \
                          the line.\n\nStale: {rendered}"
            | _ -> skiptest "no ledger entries, or the pinned proof is skipped."

        // Anti-rot 2 (phantom), and the one that keeps the ledger from re-opening #550: a dead entry
        // would excuse its symbol forever, so when #587 publishes `interpretRecordOnly` and someone
        // re-applies the spelling EARLY, the entry would wave the bug straight back through. The ledger
        // is not allowed to outlive its subjects.
        test "no doc-ledger entry names a doc site that no longer names it" {
            let live = docSymbols |> List.map docKey |> Set.ofList
            let phantom = docLedger - live |> Set.toList
            let rendered = String.concat "; " phantom

            Expect.isEmpty
                phantom
                $"these are declared in {docLedgerRel}, and no shipped doc names them any more — the doc was \
                  fixed and the exemption outlived it. Delete the line: an entry that survives its subject \
                  silently re-excuses the symbol if a doc ever names it again.\n\n\
                  Phantom: {rendered}"
        }
    ]
