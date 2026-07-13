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
///
/// THE MEMBER MAY END IN A PRIME, and it must, or the rule INVENTS violations. `checked` is a reserved
/// F# word, so the attribute is spelled `checked'` — `CheckBox.checked'`, `Switch.checked'`. Stopping the
/// member at `\w*` truncates that to `CheckBox.checked`, which no package exports BECAUSE IT DOES NOT
/// EXIST, and the rule then reports a correct doc as a defect. (It was latent in the fence extractor
/// before #598 widened this regex to doc-comments, where the mirror actually writes the spelling.)
///
/// `'?(?!\w)` takes the prime ONLY when the identifier really ends there. English prose in a doc-comment
/// is full of possessives — "`Control.render`'s output" — and a bare `'?` would swallow the apostrophe
/// and extract `render'`, a symbol nothing exports: the same invented violation, from the other side. The
/// lookahead makes the prime backtrack when a word character follows it, so `render's` yields `render`
/// and `checked'` yields `checked'`.
let private callRegex =
    Regex(@"(?<![\w.])((?:[A-Z]\w*\.)*)([A-Z]\w*)\.([a-z]\w*'?(?!\w))", RegexOptions.Compiled)

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

/// A stalled restore must fail, not hang. Generous enough for a cold restore on a slow runner.
let private probeTimeoutMs = 6 * 60 * 1000

/// Probe-private packages folder, OUTSIDE the throwaway work dir so a cold restore is paid once per
/// machine rather than on every test run. It is still not the machine's global packages folder — the
/// point of the isolation is that nothing this repo `pack`s locally can ever be resolved from here;
/// only nuget.org can populate it, and a published (id, version) is immutable, so reuse is safe.
let private probePackagesDir =
    Path.Combine(Path.GetTempPath(), "fsgg-pinned-api-probe-packages")

/// Restore `packages` at their axis pins from nuget.org ALONE, compile a `Probe.fs` whose body is nothing
/// but `nameof` lines, and hand back the compiler's verdict. Both probes in this file are this function:
/// #504's (do the template's CALLS resolve against the pin?) and #611's (does a LEDGERED case really NOT?).
///
/// One body, because the guarantee that makes either probe mean anything is the `<clear />` + probe-local
/// packages folder below, and a second copy of that setup is a second place for it to rot. A probe that
/// silently resolved from the machine's global cache would answer a question nobody asked.
let private runNameofProbe (namespaces: string list) (nameofLines: string list) =
    // F4 — a NAMESPACE IS NOT A PACKAGE ID, and conflating them makes the probe un-greenable. The mirror
    // declares sub-namespaces that no package is named after (`FS.GG.UI.Controls.Typed`,
    // `FS.GG.UI.Controls.Elmish.Authoring`, `FS.GG.UI.Themes.Default.Theming`); referencing one of those as
    // a package is an NU1101, the probe fails for a reason that is nothing to do with the symbol, and the
    // failure reads as "this member is absent from the pin". A rule whose diagnosis blames the ledger for a
    // bug in the prober is worse than no rule. `packageForNamespace` is the existing, correct mapping — the
    // PackageReferences come from it, and the `open` lines stay as the namespaces they are.
    let packages =
        namespaces |> List.map packageForNamespace |> List.distinct |> List.sort
    let workDir = Path.Combine(Path.GetTempPath(), "fsgg-pinned-api-probe-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory workDir |> ignore

    try
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

        for ns in namespaces do
            probe.AppendLine($"open {ns}") |> ignore

        probe.AppendLine().AppendLine("let private probed : string list =").AppendLine("    [") |> ignore

        for line in nameofLines do
            probe.AppendLine($"      nameof {line}") |> ignore

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

/// #504's probe: every entry point the TEMPLATE'S `Program.fs` calls, resolved against the pin.
let private runProbeBuild () =
    // The namespaces actually called. `runNameofProbe` maps each to the package that carries it.
    let namespaces =
        callSites |> List.choose callNamespace |> List.distinct |> List.sort

    let lines =
        callSites
        |> List.sortBy (fun c -> c.Module, c.Member)
        |> List.map (fun c -> $"{c.Module}.{c.Member}")

    runNameofProbe namespaces lines

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
/// what makes it the sharpest subject. The PROSE around it is judged too, by `skillProseSymbols` below
/// (#597); this extractor stays fence-only so the two can be read, and reasoned about, separately.
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

/// `Module.member` inside a `///` DOC-COMMENT of the shipped api-surface mirror (#598).
///
/// THE BLIND SPOT THIS CLOSES. The two extractors above read DECLARATIONS — a mirror's `val`, a skill's
/// ```fsharp fence. Neither reads the prose wrapped around them, so a mirror could *instruct* a product
/// author, at length, to call a symbol the pin does not export, and this rule stayed green. That is not
/// hypothetical: `ControlsElmish.fsi` told its reader to set `MapKey` to `ViewerKeyboard.mapKeyRaw`,
/// which NO published FS.GG.UI.KeyboardInput exports — #589's own gate was blind to it BY CONSTRUCTION,
/// and #591 fixed the two *ledgered* sites while this one sat, unseeable, in a `///` comment. The
/// instance is cheap; the CLASS is unbounded, and these doc-comments are dense with API names.
///
/// WHY THE MIRROR'S PROSE IS JUDGED WHERE A SKILL'S IS NOT. `skillFenceSymbols` deliberately skips prose,
/// because a sentence in a skill ("`interpret` is deprecated") MENTIONS an API rather than teaching a
/// reader to call it. The mirror is not that. `docs/scaffold-map.md` designates it the product author's
/// authoritative signature set: it is the file they read to find out what they may call, and a `///`
/// comment on a `val` is the instruction attached to that val. A name in it is guidance, not chatter.
///
/// ONLY `Module.lowercaseMember` MATCHES, which is what keeps this off the prose that is genuinely not a
/// call: `callRegex` demands a lowercase member, so a DU case (`ViewerEffect.DispatchInput`), an issue
/// ref (`FS-GG/.github#416`) and a bare type name are all correctly invisible. What survives is the
/// shape a reader can actually copy into their `update`.
///
/// A PATH IS NOT A CALL, and prose names files constantly. `Program.fs` and `Model.fs` are already in
/// these doc-comments — they are harmless only because `Program` and `Model` are SCAFFOLD modules, which
/// `isJudgedDocModule` exempts. That is a coincidence, not a guard. `Scene`, `Control`, `Loop` and
/// `Persistence` are all column-0 modules of the mirror, so the day a doc-comment writes `Scene.fs` this
/// extractor yields the member `fs`, no package exports it, and the rule reddens a CORRECT doc with no
/// honest remedy: you cannot rewrite `fs` to a bindable spelling, and ledgering a non-symbol is a lie.
///
/// Two guards, kept deliberately NARROW. A `/` immediately before the match means a path, whatever
/// follows it. And the member may not be one of the three F# SOURCE extensions, which no API member is
/// ever named. It is tempting to reject every extension-shaped member — `md`, `txt`, `json` — and that
/// is the fails-open direction (#266): those are legal F# identifiers, so a blanket list would excuse a
/// REAL member the day someone exports one, and excuse it silently. Reject what cannot be a symbol, not
/// what merely resembles a file.
let private sourceFileExtensions = set [ "fs"; "fsi"; "fsx" ]

let private mirrorDocCommentSymbols =
    Directory.EnumerateFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')

        File.ReadAllLines path
        |> Array.mapi (fun i line -> i + 1, line)
        |> Array.collect (fun (lineNo, raw) ->
            let trimmed = raw.TrimStart()

            if not (trimmed.StartsWith("///", StringComparison.Ordinal)) then
                [||]
            else
                let text = trimmed.Substring 3

                callRegex.Matches text
                |> Seq.filter (fun m ->
                    let precededBySlash = m.Index > 0 && text.[m.Index - 1] = '/'
                    not precededBySlash && not (sourceFileExtensions.Contains m.Groups.[3].Value))
                |> Seq.map (fun m ->
                    m.Groups.[1].Value,
                    { Doc = rel
                      Line = lineNo
                      Module = m.Groups.[2].Value
                      Member = m.Groups.[3].Value })
                |> Array.ofSeq))
    |> List.ofSeq

/// `Module.member` inside a `///` DOC-COMMENT of the SCAFFOLD'S OWN SOURCE — `template/base/src/**` and
/// `template/fragments/**` (#608).
///
/// THE LAST UNJUDGED SHIPPED DOC SURFACE. #598 widened this rule to the mirror's prose and closed the
/// blind spot it was filed for; it did not close the CLASS. These files are shipped into every scaffolded
/// product — they are the product's own source, the first thing a reader opens — and the rule read their
/// CODE (via #504's `Program.fs` call sites) while the PROSE WRAPPED AROUND THE CODE went unjudged. A
/// `///` comment here naming a symbol the pin does not export is exactly #550/#598: a shipped doc telling
/// a product author to call something they cannot bind.
///
/// FILED WHILE THE SURFACE IS CLEAN, WHICH IS THE ONLY CHEAP MOMENT. Today these files name exactly one
/// framework symbol in prose (`SpatialGrid.build`, in the vec2 fragment) and it RESOLVES — so there is no
/// violation to fix, and the gate can simply be widened. Widen it after the first violation lands and you
/// are paying #550's price again, with a ledger entry instead of a green build.
///
/// THE SCAFFOLD'S OWN MODULES MUST STAY EXEMPT, and that matters more here than anywhere else, because
/// these files ARE the scaffold: `template/fragments/vec2/src/Product/Vec2.fs` DEFINES `module Geometry`,
/// and FS.GG.Game.Core exports a `Geometry` module too — a different one. Judging the product's own module
/// against the framework's same-named one reports a defect in correct guidance. `isJudgedDocModule` already
/// does this (it is what `scaffoldModules` is for), so this extractor inherits it rather than re-deriving
/// it — the whole point of reusing #598's machinery instead of writing a second one that drifts.
///
/// The two guards are #598's, unchanged and for its reasons: a `/` immediately before the match means a
/// PATH (prose names files constantly — `Model.fs` yields the member `fs` on the module `Model`), and the
/// member may not be one of the three F# SOURCE extensions. Not a blanket extension list: `md`/`txt`/`json`
/// are legal F# identifiers, so blacklisting them fails OPEN the day someone exports one.
let private scaffoldSourceRoots =
    [ repoPath "template/base/src"; repoPath "template/fragments" ]
    |> List.filter Directory.Exists

let private scaffoldSourceDocCommentSymbols =
    scaffoldSourceRoots
    |> Seq.collect (fun root ->
        Seq.append
            (Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories))
            (Directory.EnumerateFiles(root, "*.fsi", SearchOption.AllDirectories)))
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')

        File.ReadAllLines path
        |> Array.mapi (fun i line -> i + 1, line)
        |> Array.collect (fun (lineNo, raw) ->
            let trimmed = raw.TrimStart()

            if not (trimmed.StartsWith("///", StringComparison.Ordinal)) then
                [||]
            else
                let text = trimmed.Substring 3

                callRegex.Matches text
                |> Seq.filter (fun m ->
                    let precededBySlash = m.Index > 0 && text.[m.Index - 1] = '/'
                    not precededBySlash && not (sourceFileExtensions.Contains m.Groups.[3].Value))
                |> Seq.map (fun m ->
                    m.Groups.[1].Value,
                    { Doc = rel
                      Line = lineNo
                      Module = m.Groups.[2].Value
                      Member = m.Groups.[3].Value })
                |> Array.ofSeq))
    |> List.ofSeq

// ---------------------------------------------------------------------------------------------
// #611 — the pin-grounded rule judged VALS, not CASES.
//
// `callRegex` demands a LOWERCASE member (the F# convention for functions and values), and a DU case is
// capitalised and hangs off a TYPE, not a module. So `ViewerEffect.Persist` — and every union case and
// record field a shipped mirror declares — was invisible to the pin, whether the mirror taught it or
// omitted it. That is not a defect in the regex; vals were the class it was built for. It is a hole in
// the coverage, and it is not hypothetical: #535 added `ViewerEffect.Persist` to the shipped mirror
// (M-MIR/TYPE compels it — a mirrored type must match src member-for-member), the published
// FS.GG.UI.SkiaViewer 0.9.0 exports 15 `ViewerEffect` cases and `Persist` is not among them, and every
// gate in this repo was green. A product author reading the shipped mirror is told about a case they
// cannot construct.
//
// The `val` half of that same commit — `Viewer.runAppWithPersistence` — WAS caught, and is ledgered two
// lines above this one. The case half sat unseen. Same doc, same release, same fix; one visible to the
// oracle and one not.
//
// EXTRACTED FROM DECLARATIONS, NOT PROSE, and that is the whole reason this is cheap and safe. A `///`
// comment naming `ViewerEffect.Persist` is ambiguous (is it a case? a module member? a sentence?), but
// `| Persist of effects: PersistenceEffect list` under `type ViewerEffect =` is not ambiguous at all.
// So this reads the mirror's own syntax and asks the pin about exactly what the mirror DECLARES.
/// Which construct the mirror DECLARED. Kept because the compile-probe below can only address one of
/// them: `nameof ViewerEffect.Persist` resolves a union case with no value and no instance, but a record
/// field is an instance member and `nameof Attr.Name` does not compile even when the field exists. Probing
/// a field would therefore report "unreachable" for a field that is perfectly reachable — confirming a
/// false ledger entry, which is the one outcome a proof must never produce.
type MemberKind =
    | UnionCase
    | RecordField

type TypeMember =
    { Doc: string
      Line: int
      Namespace: string
      Type: string
      Member: string
      Kind: MemberKind }

/// `| Case`, at the DU's indent, under a `type X =`. `of …` is dropped: the pin is asked about the case's
/// NAME, which is what a reader writes.
let private duCaseRegex = Regex(@"^\s*\|\s*(?<case>[A-Z]\w*)\b", RegexOptions.Compiled)

/// `type X =` AND `and X =` — column 0 or nested; the arity mangle (``Foo`1``) never appears in an `.fsi`.
///
/// The `and` half is not a nicety. F# joins mutually recursive types with it and the mirrors are full of
/// them (`type Control<'msg> = { … }` / `and Attr<'msg> = { … }` / `and AttrValue<'msg> = | TextValue …`).
/// Reading only `type` hangs every `and`-joined type's members on the last `type` seen and manufactures
/// members that exist nowhere — `Control.TextValue`, `SceneNode.Nodes`. That is a FALSE POSITIVE, and a
/// false positive is the one failure this rule cannot survive: the first person it wrongly accuses will
/// ledger it, and a ledgered lie is a rule that has been switched off.
let private typeDeclRegex =
    Regex(
        @"^(?<indent>\s*)(?:type|and)\s+(?<name>[A-Z]\w*)\s*(?<gen><[^>]*>)?(?<rest>.*)$",
        RegexOptions.Compiled
    )

/// A type's identity is its NAME AND ITS GENERIC ARITY. Keyed on the bare name, `Attr<'msg>` and a
/// hypothetical `Attr` would merge — and in the pin they really do: FS.GG.UI.SkiaViewer 0.9.0 exports both
/// `ViewerEffect` and `ViewerEffect<'msg>`. The key is IL's own spelling (``Attr`1``), so the mirror side
/// and the oracle side need no translation between them.
let private arityKey (name: string) (generics: string) =
    if String.IsNullOrWhiteSpace generics then
        name
    else
        // `<'a, 'b when 'a: comparison>` — the constraints follow the parameters, and the commas inside
        // them are not parameter separators. Cut them off before counting.
        let inner = generics.Trim([| '<'; '>' |])

        let parameters =
            match inner.IndexOf(" when ", StringComparison.Ordinal) with
            | -1 -> inner
            | i -> inner.Substring(0, i)

        let arity =
            parameters.Split(',')
            |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
            |> Array.length

        if arity = 0 then name else $"{name}`{arity}"

/// #611 (F3) — the WHOLE BODY on the `type` line: `type Rng = { State: uint64; Bump: int }`, or
/// `type ThemeMode = Light | Dark`. Ten shipped mirror types are written this way, and a line-by-line
/// reader that treats a `type` line as nothing but a header never looks at them again — so every member
/// they declare was silently unjudged. That is the fails-open direction: the rule reported green on types
/// it had never read.
///
/// It reads only the two shapes it can be SURE of. `type Foo = Bar` is a type ABBREVIATION and
/// `type Foo = Bar of int` is a single-case union, and nothing in the text tells them apart — so a
/// one-line body with neither `{` nor `|` is left alone rather than guessed at, and `assertNoAmbiguousOneLiner`
/// below makes sure no mirror ever writes one.
let private oneLineFieldRegex = Regex(@"(?<field>[A-Z]\w*)\s*:", RegexOptions.Compiled)

let private inlineMembers (rest: string) =
    let body = rest.TrimStart()

    let body =
        if body.StartsWith("=", StringComparison.Ordinal) then
            body.Substring(1).Trim()
        else
            ""

    if body.StartsWith("{", StringComparison.Ordinal) then
        [ for m in oneLineFieldRegex.Matches body -> m.Groups.["field"].Value, RecordField ]
    elif body.Contains "|" then
        body.Split('|')
        |> Array.choose (fun segment ->
            let name = segment.Trim()

            // `Light`, or `Circle of radius: float` — the case is the leading capitalised token.
            let token =
                name.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.tryHead

            match token with
            | Some tk when tk.Length > 0 && Char.IsUpper tk.[0] && tk |> Seq.forall (fun c -> Char.IsLetterOrDigit c || c = '_') ->
                Some(tk, UnionCase)
            | _ -> None)
        |> List.ofArray
    else
        []

/// A record field: `{ Field: T` or a continuation line `  Field: T }`. Deliberately requires the
/// capital-initial + colon shape, which is what a record field IS — an inline `///` comment above it is
/// skipped by the `///` guard, and a `val` line cannot match because `val` is lowercase.
let private recordFieldRegex = Regex(@"^\s*[{]?\s*(?<field>[A-Z]\w*)\s*:", RegexOptions.Compiled)

let private mirrorTypeMembers =
    let acc = ResizeArray<TypeMember>()

    for path in Directory.EnumerateFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories) do
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
        let lines = File.ReadAllLines path

        let ns =
            lines
            |> Array.tryPick (fun line ->
                if line.StartsWith("namespace ", StringComparison.Ordinal) then
                    Some(line.Substring(10).Trim())
                else
                    None)
            |> Option.defaultValue ""

        // The type a `| Case` / `Field:` line belongs to is simply the last `type X =` seen. A `val` or a
        // `module` closes it: neither is a type body, and continuing to attribute lines to the last type
        // across them is how a record field of one type gets hung on another.
        let mutable current: string option = None

        for i in 0 .. lines.Length - 1 do
            let line = lines.[i]
            let trimmed = line.TrimStart()

            if trimmed.StartsWith("///", StringComparison.Ordinal) || trimmed = "" then
                ()
            elif typeDeclRegex.IsMatch line then
                let m = typeDeclRegex.Match line
                let typeName = arityKey m.Groups.["name"].Value m.Groups.["gen"].Value
                current <- Some typeName

                // F3: the body may be RIGHT HERE, on this same line, and this is the only chance to read it.
                for (memberName, kind) in inlineMembers m.Groups.["rest"].Value do
                    acc.Add
                        { Doc = rel
                          Line = i + 1
                          Namespace = ns
                          Type = typeName
                          Member = memberName
                          Kind = kind }
            elif
                trimmed.StartsWith("val ", StringComparison.Ordinal)
                || trimmed.StartsWith("module ", StringComparison.Ordinal)
                || trimmed.StartsWith("namespace", StringComparison.Ordinal)
            then
                current <- None
            else
                match current with
                | None -> ()
                | Some typeName ->
                    let case = duCaseRegex.Match line
                    let field = recordFieldRegex.Match line

                    if case.Success then
                        acc.Add
                            { Doc = rel
                              Line = i + 1
                              Namespace = ns
                              Type = typeName
                              Member = case.Groups.["case"].Value
                              Kind = UnionCase }
                    elif field.Success then
                        acc.Add
                            { Doc = rel
                              Line = i + 1
                              Namespace = ns
                              Type = typeName
                              Member = field.Groups.["field"].Value
                              Kind = RecordField }

    List.ofSeq acc

let private typeMemberKey (m: TypeMember) = $"{m.Doc}::{m.Type}.{m.Member}"

/// `Module.member` in the PROSE of a shipped product skill — every line that is not inside a fence (#597).
///
/// THE LAST UNJUDGED SHIPPED DOC SURFACE, and the one aimed straight at the reader. #598 widened this rule
/// to the mirror's `///` prose and #608 to the scaffold source's; the product SKILLS — the documents the
/// scaffold hands an author as *the* way to use a capability — had only their ```fsharp fences read.
///
/// THE ARGUMENT THIS OVERTURNS was `skillFenceSymbols`' own: *"naming an API in a sentence (`interpret` is
/// deprecated) is not teaching a reader to call it."* That is true of a MENTION and false of the corpus.
/// These skills teach in prose and merely illustrate in fences — `fs-gg-keyboard-input` says **"Surface it
/// with `AdapterCmd.diagnostics`"**, which is an imperative, and published FS.GG.UI.Controls.Elmish 0.9.0
/// exports no such member. #592 saw it, could not act on it, and said so: *"which compounds the lie —
/// though prose is not what #589's gate judges."* It is now.
///
/// And the distinction never protected anything, because the rule only fires on a symbol the PIN DOES NOT
/// EXPORT. A skill that merely mentions a real symbol resolves and stays green; a skill that mentions an
/// unbindable one is misleading a reader whether or not the sentence was an instruction — they cannot tell
/// a mention from a recommendation, and #592 called that compounding the lie. Where naming an unpinned
/// symbol is DELIBERATE — a warning that a spelling is not in the pin yet — the ledger is the honest
/// remedy, and it already carries exactly that shape for `Viewer.runAppWithPersistence`.
///
/// FENCES vs PROSE, and why this is not simply "read the whole file". A fence is F# and gets
/// `stripCommentsAndStrings`; prose is English and must not. Prose also needs #598's two guards, which a
/// fence does not: a `/` immediately before the match (prose names PATHS constantly —
/// `docs/api-surface/Canvas/Persistence.fsi` would otherwise yield the member `fsi` on the module
/// `Persistence`), and the three F# SOURCE extensions. Not a blanket extension list: `md`/`txt`/`json` are
/// legal F# identifiers, so blacklisting them fails OPEN the day someone exports one (#266).
///
/// A NON-fsharp FENCE IS NEITHER. `skillFenceSymbols` opens only on ```fsharp; this one must skip EVERY
/// fence, or a ```console block's `dotnet build` becomes a call site. So it tracks any fence open/close,
/// and reads only what falls outside.
///
/// FOUR OF THESE SKILLS ARE FROZEN MIRRORS THIS REPO MAY NOT EDIT, and a violation in one has a DIFFERENT
/// remedy — read this before "fixing the doc" the failure tells you to fix. `fs-gg-persistence`,
/// `fs-gg-game-core`, `fs-gg-audio` and `fs-gg-model-swap` are mirrors of FS.GG.Game's canonicals;
/// `scripts/check-frozen-mirrors.fsx` (#541) REDS a Rendering PR that edits one, and its own comment records
/// that three PRs already did so "correctly, and none of them was told it was a mirror". So this rule and
/// that one can pull in opposite directions: this one says *fix the doc*, and #541 says *you do not own it*.
///
/// The LEDGER is the way out, and it is the honest one — it is already built for a violation "whose fix is
/// somebody's named, filed work", which is exactly what a canonical in another repo is. Ledger the symbol
/// against the OWNING repo's issue; do not edit the mirror, and do not narrow this rule to exclude the
/// mirrors (that would blind it to four shipped skills a product actually receives, which is the fails-open
/// shape (#266) this file refuses everywhere else). No mirrored skill violates today.
let private skillProseSymbols =
    Directory.EnumerateFiles(productSkillsRoot, "*.md", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
        let mutable inFence = false

        File.ReadAllLines path
        |> Array.mapi (fun i line -> i + 1, line)
        |> Array.collect (fun (lineNo, raw) ->
            let opener = raw.TrimStart()

            if opener.StartsWith("```", StringComparison.Ordinal) then
                // ANY fence, not just an fsharp one: inside a ```console or ```json block this rule has no
                // subject, and outside every fence it has all of them.
                inFence <- not inFence
                [||]
            elif inFence then
                [||]
            else
                callRegex.Matches raw
                |> Seq.filter (fun m ->
                    let precededBySlash = m.Index > 0 && raw.[m.Index - 1] = '/'
                    not precededBySlash && not (sourceFileExtensions.Contains m.Groups.[3].Value))
                |> Seq.map (fun m ->
                    m.Groups.[1].Value,
                    { Doc = rel
                      Line = lineNo
                      Module = m.Groups.[2].Value
                      Member = m.Groups.[3].Value })
                |> Array.ofSeq))
    |> List.ofSeq

/// Every shipped doc surface, reduced to the symbols this rule may judge — EVERY occurrence, not one per
/// symbol. `docSymbols` below dedups for the verdict; this keeps the sites, because the verdict and the
/// WORK are different questions.
let private judgedDocOccurrences =
    List.concat
        [ skillFenceSymbols
          skillProseSymbols
          mirrorValSymbols
          mirrorDocCommentSymbols
          scaffoldSourceDocCommentSymbols ]
    |> List.filter (fun (qualifier, s) -> isJudgedDocModule qualifier s.Module)
    |> List.map snd

/// Every line a judged `docKey` occurs on. The dedup below is right for the VERDICT (one symbol in one
/// doc is one violation) and badly wrong for the REPORT: `Attr.onChanged` was written in EIGHT
/// doc-comments of Controls/Control.fsi and `distinctBy` showed exactly one of them, so the failure read
/// as four sites when thirteen needed editing. It converges — fix the named line and the next takes its
/// place — but a message that understates the work by 3x sends the reader back around the loop for
/// nothing. Name them all.
let private docKeySites =
    judgedDocOccurrences
    |> List.groupBy docKey
    |> List.map (fun (key, sites) -> key, sites |> List.map (fun s -> s.Line) |> List.distinct |> List.sort)
    |> Map.ofList

/// The symbols this rule judges: one verdict per `docKey`.
let private docSymbols =
    judgedDocOccurrences
    |> List.distinctBy docKey
    |> List.sortBy (fun s -> s.Doc, s.Line)

/// `path:line  Module.member`, and every OTHER line the same symbol is written on.
let private renderDocSymbol (s: DocSymbol) =
    let head = $"{s.Doc}:{s.Line}  {s.Module}.{s.Member}"

    match docKeySites |> Map.tryFind (docKey s) with
    | Some (_ :: _ :: _ as lines) ->
        let rendered = lines |> List.map string |> String.concat ", "
        $"{head}  ({lines.Length} sites: lines {rendered})"
    | _ -> head

/// Every `FS.GG.*` package a scaffolded product pins — read from the props file, which IS the set it
/// restores. This is the authority for turning a mirrored NAMESPACE into a PACKAGE, and the two are not
/// the same string: `runProbeBuild` above may assume they are, because the namespaces `Program.fs` calls
/// all happen to be package ids, but the mirror at large declares SUB-namespaces that are not —
/// `FS.GG.UI.Controls.Typed`, `FS.GG.UI.Controls.Elmish.Authoring`, `FS.GG.UI.Themes.Default.Theming`.
/// Referencing one of those as a package is an NU1101 for a package that was never supposed to exist.
/// The packages the docs actually talk about — derived from the mirror and the props, never hardcoded,
/// so the set cannot rot as the framework grows.
let private docPackages =
    let fromVals =
        docSymbols
        |> List.choose (fun s ->
            frameworkModulesByName
            |> Map.tryFind s.Module
            |> Option.bind (fun candidates ->
                candidates
                |> List.tryFind (fun m -> m.Members.Contains s.Member)
                |> Option.orElse (List.tryHead candidates))
            |> Option.map (fun m -> packageForNamespace m.Namespace))

    // #611 — and every package whose mirror declares a TYPE, because the case rule judges those and can
    // only judge what the oracle restored. Deriving the restore set from the `val` symbols ALONE (which is
    // all it used to need) silently coupled one rule's coverage to another rule's subject matter: a mirror
    // that declares cases but no `val` would restore no package, every one of its types would be missing
    // from the oracle, and the case rule would pass over them without a word. It happens to cover all 21
    // mirrors today — which is luck, not structure, and luck is what this file is written against.
    let fromTypes =
        mirrorTypeMembers |> List.map (fun m -> packageForNamespace m.Namespace)

    fromVals @ fromTypes |> List.distinct |> List.sort

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

/// #611 — the PINNED package's TYPE surface: every union case and record field, keyed by type.
///
/// A DU's cases are not methods and are not on a module, so `readModuleSurface` cannot see them.
///
/// Read from the F# COMPILER'S OWN MARKING — `CompilationMappingAttribute`, whose first fixed argument is
/// the `SourceConstructFlags` (`UnionCase = 8`, `Field = 4`) — and not from the shapes those constructs
/// happen to take in IL. Every shape-based reading of this is WRONG, and each was tried:
///
///   * the nested `Tags` type: absent on a SINGLE-CASE union (`type CollectionEffect = | VisibleRangeChanged
///     of VisibleRange` compiles to a class with no `Tags` at all), so the oracle would not know the one
///     case it has and would accuse the mirror of inventing it — a false positive, and a false positive is
///     how this rule gets ledgered into silence by the first person it wrongly accuses;
///   * a `New`-prefix match: a genuine member `NewSession` would mint the phantom case `Session`, WIDENING
///     the oracle — and a wider oracle excuses a real violation, the fail-open direction this file forbids;
///   * every `get_`: `Tag` and `Item` are compiler-generated on any DU, and both would land in the oracle.
///
/// The attribute is authoritative, and it costs nothing: the compiler puts it there precisely so a reader
/// can recover the F# construct from the IL. Nullary cases arrive as `get_Case`, the rest as `NewCase`.
let private compilationMappingFlags (md: MetadataReader) (handle: CustomAttributeHandle) =
    let ca = md.GetCustomAttribute handle

    let name =
        match ca.Constructor.Kind with
        | HandleKind.MemberReference ->
            let mr = md.GetMemberReference(MemberReferenceHandle.op_Explicit ca.Constructor)

            match mr.Parent.Kind with
            | HandleKind.TypeReference ->
                md.GetString (md.GetTypeReference(TypeReferenceHandle.op_Explicit mr.Parent)).Name
            | _ -> ""
        | HandleKind.MethodDefinition ->
            let m = md.GetMethodDefinition(MethodDefinitionHandle.op_Explicit ca.Constructor)
            md.GetString (md.GetTypeDefinition(m.GetDeclaringType())).Name
        | _ -> ""

    if name <> "CompilationMappingAttribute" then
        None
    else
        // Blob layout: `01 00` prolog, then the fixed args. The first is the SourceConstructFlags enum,
        // an int32. A shorter blob is a constructor overload this reader does not model — skip it rather
        // than guess, since a guess here silently changes what the oracle believes.
        let bytes = md.GetBlobBytes ca.Value

        if bytes.Length >= 6 then
            Some(BitConverter.ToInt32(bytes, 2))
        else
            None

[<Literal>]
let private SourceConstructUnionCase = 8

[<Literal>]
let private SourceConstructField = 4

let private readTypeSurface (dll: string) =
    use stream = File.OpenRead dll
    use pe = new PEReader(stream)
    let md = pe.GetMetadataReader()

    // An F# MODULE is abstract+sealed; a type is not.
    let isModule (td: TypeDefinition) =
        td.Attributes.HasFlag TypeAttributes.Abstract
        && td.Attributes.HasFlag TypeAttributes.Sealed

    [ for handle in md.TypeDefinitions do
        let td = md.GetTypeDefinition handle
        let visibility = td.Attributes &&& TypeAttributes.VisibilityMask

        let isPublic =
            visibility = TypeAttributes.Public || visibility = TypeAttributes.NestedPublic

        // A type NESTED IN A TYPE is not something a mirror can declare — it is the compiler's own
        // furniture: a DU's `Tags`, and one class per union case (`ViewerEffect+CaptureScreenshot`).
        // Registering those as top-level types mints entries called `Circle`, `KeyDown`, `Custom`, each
        // carrying `Item`/`Item1`, which then collide with REAL types of the same name in other packages.
        // A type nested in a MODULE is different — that is what `module Foo = type Bar = …` compiles to,
        // and a mirror can declare it — so those are kept.
        let declaredAtTypeLevel =
            let parent = td.GetDeclaringType()
            parent.IsNil || isModule (md.GetTypeDefinition parent)

        // `readModuleSurface` owns the modules, and the two maps stay apart so a `type Scene` case can
        // never excuse a `module Scene` member.
        if isPublic && not (isModule td) && declaredAtTypeLevel then
            // IL's own name, arity mangle AND ALL (`Attr`1`). NOT stripped: a type's identity is its name
            // AND its arity, and published FS.GG.UI.SkiaViewer 0.9.0 exports BOTH `ViewerEffect` and
            // `ViewerEffect`1` (an unrelated `ViewerEffect<'msg>` from Host/Diagnostics). Merge them and a
            // case the generic one carries EXCUSES the same-named case on the closed one — a wider oracle,
            // and a wider oracle excuses a real violation. #594's ledger keys on arity for this exact
            // reason; the oracle it is checked against has to agree with it. The mirror extractor mangles
            // its side to match, so no translation is needed between them.
            let name = md.GetString td.Name

            for methodHandle in td.GetMethods() do
                let m = md.GetMethodDefinition methodHandle
                let memberName = md.GetString m.Name

                let flags =
                    m.GetCustomAttributes() |> Seq.tryPick (compilationMappingFlags md)

                if flags = Some SourceConstructUnionCase then
                    if memberName.StartsWith("New", StringComparison.Ordinal) then
                        yield name, memberName.Substring 3
                    elif memberName.StartsWith("get_", StringComparison.Ordinal) then
                        yield name, memberName.Substring 4

            for propertyHandle in td.GetProperties() do
                let pd = md.GetPropertyDefinition propertyHandle

                let flags =
                    pd.GetCustomAttributes() |> Seq.tryPick (compilationMappingFlags md)

                if flags = Some SourceConstructField || flags = Some SourceConstructUnionCase then
                    yield name, md.GetString pd.Name ]

/// Restore `packages` at `versionOf`, and read their module + type surface out of the restored assemblies.
///
/// PARAMETERISED BY VERSION, and that is the whole point (#688). Two callers want two different subjects:
/// the RULES below judge the doc against `$(FsGgUiVersion)` — a moving target, which is correct, because
/// that is what a scaffolded product restores. The ORACLE SELF-CHECK judges the READER, and for that it
/// needs a package that CANNOT change under it. Those are different versions, and collapsing them into one
/// is what reddened `main`.
let private readSurfaceAt
    (packages: string list)
    (versionOf: string -> string)
    : Result<Map<string, Set<string>> * Map<string * string, Set<string>>, string> =
    let workDir = Path.Combine(Path.GetTempPath(), "fsgg-doc-pin-probe-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory workDir |> ignore

    try
        let references =
            packages
            |> List.map (fun id -> $"    <PackageReference Include=\"{id}\" Version=\"{versionOf id}\" />")
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
                    let types =
                        Collections.Generic.Dictionary<string * string, Collections.Generic.HashSet<string>>()

                    for packageId in packages do
                        let dir =
                            Path.Combine(probePackagesDir, packageId.ToLowerInvariant(), versionOf packageId, "lib")

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
                        | None -> missing.Add $"{packageId} {versionOf packageId}"
                        | Some path ->
                            for (moduleName, memberName) in readModuleSurface path do
                                if not (surface.ContainsKey moduleName) then
                                    surface.[moduleName] <- Collections.Generic.HashSet<string>()

                                surface.[moduleName].Add memberName |> ignore

                            // #611 — the SAME assembly, read a second way. Modules and types are disjoint
                            // in the metadata (abstract+sealed vs not), so those two maps cannot collide,
                            // which is why they are kept apart rather than merged: a merged map would let a
                            // `type Scene` case excuse a `module Scene` member, and vice versa.
                            //
                            // And the type map is keyed by (PACKAGE, type) — NOT by type name alone. The
                            // pinned packages declare seventeen type names TWICE or more (`DiagnosticSeverity`
                            // is in both FS.GG.UI.Layout and FS.GG.UI.Scene; so are `Point`, `Rect`,
                            // `ViewerMsg`, …), and Scene's has a `Fatal` case that Layout's does not. Keyed
                            // on the bare name they merge, and adding `| Fatal` to the LAYOUT mirror — the
                            // precise scenario this rule was written for — would be EXCUSED by Scene's
                            // unrelated type, silently, with every gate green. A mirror is judged against the
                            // package it actually belongs to, or it is not judged at all.
                            for (typeName, memberName) in readTypeSurface path do
                                let key = (packageId, typeName)

                                if not (types.ContainsKey key) then
                                    types.[key] <- Collections.Generic.HashSet<string>()

                                types.[key].Add memberName |> ignore

                    if missing.Count > 0 then
                        let names = String.Join(", ", missing)

                        Error $"restored, but no lib assembly was found under {probePackagesDir} for: \
                                {names}. The oracle would have a hole in it, and a hole EXCUSES every doc \
                                symbol that belongs in it — fail closed instead.\n\n{text}"
                    elif surface.Count = 0 then
                        Error "the pinned packages exported ZERO F# modules — the metadata reader has stopped \
                               seeing the surface. That is a defect in this test, not an empty framework."
                    else
                        let modules = surface |> Seq.map (fun kvp -> kvp.Key, Set.ofSeq kvp.Value) |> Map.ofSeq
                        let typeMap = types |> Seq.map (fun kvp -> kvp.Key, Set.ofSeq kvp.Value) |> Map.ofSeq
                        Ok(modules, typeMap)
    finally
        try Directory.Delete(workDir, true) with _ -> ()

/// Restored ONCE per run, not once per test. Two tests below ask for the pinned surface (the rule, and
/// the ledger's staleness check), and a restore is the expensive, network-bound half of this file — so
/// asking twice would double the gate's exposure to nuget.org for an answer that cannot have changed
/// between them. `Lazy` is thread-safe by default, which matters: Expecto runs the list in parallel.
let private pinnedSurface = lazy (readSurfaceAt docPackages pinFor)

/// THE ORACLE'S GROUND TRUTH — a published, IMMUTABLE (id, version), deliberately NOT `$(FsGgUiVersion)`.
///
/// The self-check below validates the READER (does it see nullary cases? does it invent `Tag`/`Item`?), and
/// a reader is validated against facts that cannot move. `0.9.0` is the last release before `#535` added
/// `ViewerEffect.Persist`, so it pins both halves the self-check needs: `OpenWindow`/`CloseWindow` present,
/// `Persist` absent.
///
/// It used to read `pinnedSurface`, which was the same thing only for as long as the pin HAPPENED to be
/// 0.9.0 (#688). The moment 0.9.2 published, the oracle restored a package that legitimately DOES export
/// `Persist`, the "immutable" assertion failed, and the required `Deterministic gate` went red on `main` —
/// on a repo where nothing was wrong. Worse, it stayed hidden through the whole 0.9.1 window because the
/// pin named a version nobody had published, so the restore failed and the test SKIPPED rather than ran.
///
/// Only FS.GG.UI.SkiaViewer, because `ViewerEffect` is all the self-check reads: restoring the other twelve
/// doc packages a second time would double this gate's nuget.org exposure to answer a question about one
/// type. If a future case needs another package, add it here — not to `docPackages`, which is the RULES'
/// subject and must keep tracking the pin.
let private oracleVersion = "0.9.0"

let private oracleSurface =
    lazy (readSurfaceAt [ "FS.GG.UI.SkiaViewer" ] (fun _ -> oracleVersion))

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

// ---------------------------------------------------------------------------------------------
// #611 / #594 — THE PENDING-RELEASE LEDGER'S CLAIM, ASKED OF THE ACTUAL PACKAGE.
//
// #594 gave a mirrored TYPE a way to wait for its release: `tests/Package.Tests/
// mirror-pending-release-ledger.txt` declares "src has this member, the pin does not, so the mirror omits
// it", and M-MIR/TYPE honours the omission. Three rules keep it honest — P-PEND/SRC (src really declares
// it), P-PEND/OMIT (the mirror really omits it), P-PEND/PIN (the entry's version stamp equals the pin).
//
// NOTHING ASKS THE PACKAGE. P-PEND/PIN is a string comparison: it proves the entry was WRITTEN against
// this pin, not that this pin LACKS the member. Every P-PEND rule resolves against `src/`, which is where
// the member always exists; the published package is never opened. So the ledger's load-bearing claim —
// the one that buys the omission — is the only one taken on trust.
//
// This file already restores and reads that package, so asking costs nothing.
// ---------------------------------------------------------------------------------------------------

let private pendingLedgerRel = "tests/Package.Tests/mirror-pending-release-ledger.txt"
let private pendingLedgerPath = repoPath pendingLedgerRel

/// `SkiaViewer::ViewerEffect.Persist @ 0.9.0 #587`, and its generic form `Foo::Bar<1>.Baz @ …`. The arity
/// is carried through into IL's spelling (``Bar`1``) so it keys the same map the mirror does.
let private pendingEntryRegex =
    Regex(
        @"^(?<dir>[\w.]+)::(?<type>[A-Z]\w*)(?:<(?<arity>\d+)>)?\.(?<member>\w+)\s*@\s*(?<pin>[^\s#]+)",
        RegexOptions.Compiled
    )

type PendingMember =
    { Line: int
      Dir: string
      /// EVERY namespace the mirror directory declares. A directory can declare more than one
      /// (`Controls/` declares `FS.GG.UI.Controls` and `FS.GG.UI.Controls.Typed`), and the probe must
      /// `open` all of them or the ledgered type may simply not be in scope — which would fail the probe
      /// for a reason that has nothing to do with the claim it is testing.
      Namespaces: string list
      Type: string
      Member: string }

/// The namespace a mirror DIRECTORY declares — which, per this file's header, IS its package id.
let private namespaceOfMirrorDir (dir: string) =
    let full = Path.Combine(mirrorRoot, dir)

    if not (Directory.Exists full) then
        None
    else
        Directory.EnumerateFiles(full, "*.fsi", SearchOption.AllDirectories)
        |> Seq.collect File.ReadAllLines
        |> Seq.choose (fun line ->
            if line.StartsWith("namespace ", StringComparison.Ordinal) then
                Some(line.Substring(10).Trim())
            else
                None)
        |> Seq.distinct
        |> List.ofSeq
        |> function
            | [] -> None
            | namespaces -> Some namespaces

let private pendingMembers =
    if not (File.Exists pendingLedgerPath) then
        []
    else
        File.ReadAllLines pendingLedgerPath
        |> Array.mapi (fun i line -> i + 1, line.Trim())
        |> Array.filter (fun (_, line) ->
            line.Length > 0 && not (line.StartsWith("#", StringComparison.Ordinal)))
        |> Array.choose (fun (lineNo, line) ->
            let m = pendingEntryRegex.Match line

            if not m.Success then
                None
            else
                let dir = m.Groups.["dir"].Value
                let arity = m.Groups.["arity"].Value
                let bare = m.Groups.["type"].Value

                let typeName =
                    if String.IsNullOrEmpty arity || arity = "0" then
                        bare
                    else
                        $"{bare}`{arity}"

                namespaceOfMirrorDir dir
                |> Option.map (fun namespaces ->
                    { Line = lineNo
                      Dir = dir
                      Namespaces = namespaces
                      Type = typeName
                      Member = m.Groups.["member"].Value }))
        |> List.ofArray


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

            // #597's extractor, under the same guard as its siblings and for the same reason: it was ADDED
            // because the rule read a product skill's FENCES and not the prose around them, so an
            // implementation that matches nothing restores that blind spot while reporting green (#266).
            //
            // Anchored by NAME, not by count. `isNonEmpty` would be satisfied by any one skill, so an
            // extractor that silently stopped skipping fences — reading only code, or only ```console
            // blocks — could still pass a bare count. `Keyboard.update` is named in fs-gg-keyboard-input's
            // PROSE, outside every fence, and it resolves; if it falls out, the prose reader has broken.
            Expect.isTrue
                (skillProseSymbols
                 |> List.exists (fun (_, s) -> s.Module = "Keyboard" && s.Member = "update"))
                $"`Keyboard.update` must be among the symbols extracted from the PROSE of the shipped product \
                  skills ({List.length skillProseSymbols} found). It is named in a sentence of \
                  fs-gg-keyboard-input, outside any fence — so if it falls out, the prose extractor has \
                  stopped seeing the surface a product author actually reads (#597's blind spot, reopened)."

            Expect.isNonEmpty
                mirrorValSymbols
                "public `val`s were extracted from the shipped api-surface mirror."

            // #598's extractor, under the same guard as its two siblings and for the same reason: this
            // one was ADDED because the rule was silently blind to doc-comments, so an implementation
            // that matches nothing would restore the exact blind spot it exists to close — and would do
            // it while reporting green, which is the shape (#266) this file refuses.
            Expect.isNonEmpty
                mirrorDocCommentSymbols
                "`Module.member` symbols were extracted from the `///` doc-comments of the shipped \
                 api-surface mirror. Zero means the doc-comment extractor has stopped seeing the prose \
                 the mirror instructs a product author with — the #598 blind spot, reopened."

            // #608's extractor, under the same guard as its three siblings and for the same reason. It was
            // added because the rule was blind to the prose in the scaffold's OWN SOURCE — the files a
            // product author opens on day one — so an implementation that matches nothing restores that
            // blind spot while reporting green (#266).
            //
            // The RAW half: did the extractor read the fragments at all? Anchored by NAME, because
            // `isNonEmpty` would be satisfied by `template/base/src` alone (Model.fs names two symbols), so
            // an extractor that silently dropped `template/fragments` would pass a count check. A named
            // anchor fails when the coverage narrows; a count does not.
            Expect.isTrue
                (scaffoldSourceDocCommentSymbols
                 |> List.exists (fun (_, s) -> s.Module = "SpatialGrid" && s.Member = "build"))
                $"`SpatialGrid.build` must be among the symbols extracted from the `///` doc-comments of the \
                  scaffold's own source ({List.length scaffoldSourceDocCommentSymbols} found across \
                  template/base/src + template/fragments). It is named in template/fragments/vec2/src/Product/\
                  Vec2.fs, which is a FRAGMENT — so if it falls out, either that comment was reworded (the \
                  likelier cause: the file's own header tells a product author to adapt it) or the extractor \
                  has stopped reading the fragments. Check the comment FIRST; a wrong diagnosis on a real \
                  failure is worse than no diagnosis."


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

            // The JUDGED half — and this is the one that actually guards #608's contribution.
            //
            // The raw check above is NOT enough, and review proved it. EXACTLY ONE scaffold-source symbol
            // survives `isJudgedDocModule` today (`SpatialGrid.build`; the other five are scaffold modules or
            // absent from the mirror). So a single `module SpatialGrid =` appearing in any fragment — and
            // five such fragments already exist, all wrapping FS.GG.Game.Core, with `fs-gg-product-collision`
            // already teaching "broad-phase over SpatialGrid" — makes `scaffoldModules` swallow every
            // `SpatialGrid.*` on EVERY surface. #608's contribution to the judged set goes to ZERO, and the
            // raw anchor stays green because the symbol is still extracted.
            //
            // Review planted exactly that and got 39/39 passing with an undeclared symbol sitting in a
            // shipped scaffold doc-comment — the #550 class, in the surface this item exists to close. The
            // #598 sibling below anchors on `judged` for this reason; the first draft of this one did not,
            // while claiming to be "under the same guard".
            Expect.isTrue
                (judged.Contains "SpatialGrid.build")
                $"`SpatialGrid.build` must survive into the JUDGED set, not merely be extracted. It is the \
                  ONLY scaffold-source symbol that does, so if a `module SpatialGrid` ever appears in the \
                  scaffold (a `spatial-grid` fragment beside the five that already exist), `scaffoldModules` \
                  exempts it and #608 silently judges NOTHING while every test stays green. That is the \
                  fails-open shape (.github#266) this file refuses everywhere else. If this fires, do not \
                  delete it — find what started exempting the symbol."

            Expect.isTrue
                (judged.Contains "Button.create" && judged.Contains "DataGrid.visibleRange")
                "the widget symbols the product skills teach (`Button.create`, `DataGrid.visibleRange`) are \
                 among the symbols this rule judges. If they fall out, the exemption has overreached again \
                 and the rule is checking less than it claims."

            // #597's JUDGED half, and it needs its own anchor for exactly the reason the #608 one above
            // does: being EXTRACTED is not being CHECKED. `Keyboard.update` is named in fs-gg-keyboard-input's
            // PROSE, `Keyboard` is a mirror module and no fragment declares one, so it must survive
            // `isJudgedDocModule`. If it stops, skill prose is being read and then discarded, and #597's
            // blind spot is open again behind a green run.
            Expect.isTrue
                (judged.Contains "Keyboard.update")
                "`Keyboard.update` must survive into the JUDGED set from a product skill's PROSE, not merely \
                 be extracted from it. If it falls out, prose is being read and thrown away — the #597 blind \
                 spot, reopened while every test stays green."

            // #597's fence tracking FAILS OPEN, so the fences have to be proven balanced.
            //
            // `skillProseSymbols` decides prose-vs-code by toggling on every ``` line. An UNBALANCED fence
            // — an opener whose closer was dropped, a stray ``` in a sentence — therefore leaves the reader
            // stuck "inside code" for the WHOLE REST OF THE FILE, and every prose line below it is silently
            // skipped. A skill could then name any unpinned symbol past that point and this rule would report
            // green having read none of it: "nothing to check" and "checked, and it's fine" sharing an exit
            // code, which is the shape (.github#266) this file refuses everywhere else.
            //
            // The anti-vacuity anchor above cannot catch it — it proves ONE symbol in ONE file survives, and
            // says nothing about the other sixteen. An even fence count per file is what actually holds it.
            let unbalanced =
                Directory.EnumerateFiles(productSkillsRoot, "*.md", SearchOption.AllDirectories)
                |> Seq.choose (fun path ->
                    let fences =
                        File.ReadAllLines path
                        |> Array.filter (fun line -> line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                        |> Array.length

                    if fences % 2 = 0 then
                        None
                    else
                        Some $"{Path.GetRelativePath(repoRoot, path).Replace('\\', '/')} ({fences} fence lines)")
                |> List.ofSeq

            Expect.isEmpty
                unbalanced
                "every shipped product skill closes every ``` fence it opens. An ODD fence count leaves the \
                 prose reader stuck inside a code block for the rest of the file, so every symbol below the \
                 unclosed fence goes UNJUDGED and this rule reports green having read nothing — the fails-open \
                 shape (.github#266). Fix the fence in the skill; do not relax this."

            // #598's path guard, held from BOTH sides.
            //
            // It must bite: no judged symbol may have an F# source extension as its member. `Scene`,
            // `Control`, `Loop` and `Persistence` are all column-0 mirror modules, so an unguarded
            // extractor turns a doc-comment that merely NAMES `Scene.fs` into `Scene.fs` — a member no
            // package exports — and reddens a correct doc with no honest remedy.
            let judgedFileShaped =
                docSymbols
                |> List.filter (fun s -> sourceFileExtensions.Contains s.Member)
                |> List.map (fun s -> $"{s.Doc}:{s.Line}  {s.Module}.{s.Member}")

            Expect.isEmpty
                judgedFileShaped
                "no judged doc symbol has `fs`/`fsi`/`fsx` as its member — those are FILES a doc-comment \
                 mentioned, not API a reader can call, and judging one invents a violation against a \
                 correct doc."

            // And it must not overreach: the doc-comment extractor still has to reach the real thing.
            // `ViewerKeyboard.toKeyId` is named ONLY by a `///` comment (ControlsElmish.fsi) — no `val`
            // and no fence carries it — so it is the sharpest proof that #598's extractor is doing the
            // work, and that the guard above did not swallow it on the way.
            Expect.isTrue
                (judged.Contains "ViewerKeyboard.toKeyId")
                "`ViewerKeyboard.toKeyId` — named only in a `///` doc-comment of the shipped mirror — is \
                 among the judged symbols. If it falls out, #598's doc-comment extractor has stopped \
                 reaching the prose it was added for, and the rule is checking less than it claims."
        }

        // THE RULE (#589).
        //
        // Deferred in the same window, on the same bounds, as the probe above: in the release window the
        // pinned packages do not exist, so the oracle cannot be built. SKIPPED, NOT PASSED.
        // #597 — the `advance` trap, which is the reason this rule resolves QUALIFIED names and never bare
        // ones. Asserted rather than assumed, because the trap is invisible until it fires and the failure
        // is silent in both directions.
        //
        // `fs-gg-game-core` teaches `Loop.advance` — Game.Core, public, exported at the pin, correct
        // guidance. The framework ALSO has `RetainedRender.advance`, an `AnimationClock` seam declared
        // `module internal` and reachable only through `InternalsVisibleTo`. Same bare name, different
        // module, OPPOSITE verdicts. A bare-name matcher conflates them and fails whichever way it guesses:
        // red on `Loop.advance` (a correct doc reported as a defect, with no honest remedy), or green on a
        // doc that genuinely taught the internal.
        //
        // Both halves are asserted. The first is the one #597's acceptance names; the second is the one that
        // rots quietly, because a module that stops being judged stops being reported.
        test "the rule resolves QUALIFIED names, so `Loop.advance` and `RetainedRender.advance` part company (#597)" {
            let judged = docSymbols |> List.map (fun s -> $"{s.Module}.{s.Member}") |> Set.ofList

            Expect.isTrue
                (judged.Contains "Loop.advance")
                "`Loop.advance` is taught by fs-gg-game-core, is public in FS.GG.Game.Core, and is exported at \
                 the pin — so it must be JUDGED and must PASS. (That it passes is the rule above; that it is \
                 judged at all is this assertion.) If it is missing, the extractor has stopped reading the \
                 skill that teaches it, and the trap below is no longer being tested by anything."

            let retained = judged |> Set.filter (fun s -> s.StartsWith("RetainedRender.", StringComparison.Ordinal))

            Expect.isEmpty
                retained
                "`RetainedRender` is `module internal` and is NOT in the shipped mirror, so no symbol on it may \
                 ever enter the judged set. If one does, the closed world has been widened to modules a product \
                 cannot reach, and `RetainedRender.advance` will now be conflated with the `Loop.advance` that \
                 fs-gg-game-core correctly teaches — the bare-name unsoundness this rule exists to avoid."
        }

        // #597 / #585 — FRAMEWORK skills are not judged, and that is a decision, not an omission.
        //
        // #585's second criterion read "framework skills teach APIs a product cannot reach, as if it could",
        // and named three: `InteractionRepro` (InternalsVisibleTo), `RetainedRender.hitTestLayout` (`module
        // internal`), `Viewer.traceStartCapture`/`traceDrainCapture`/`traceEmit` (`val internal`). All three
        // are taught in `src/*/skill/SKILL.md`.
        //
        // ON THOSE THREE THERE IS NO DEFECT, and this test is where that verdict is written down.
        // A FRAMEWORK skill is not shipped: it is not packed into the package and it is not in
        // `template/product-skills/`, so no generated product ever receives one. Its audience is people
        // working ON this framework, who reach internals through `InternalsVisibleTo` — which is precisely
        // what `traceStartCapture` EXISTS for. So `src/Diagnostics/skill/SKILL.md` saying "a test or tool can
        // `Viewer.traceStartCapture ()`" is TRUE FOR ITS READERS, and its doc comment ("internal — diagnostic
        // seam, not a product contract") agrees with it rather than contradicting it.
        //
        // The audience is the whole distinction, and it is the thing to carry away: a doc is honest or
        // dishonest RELATIVE TO WHO RECEIVES IT. Judging a framework skill against the product's pin would
        // report a defect in a document no product will ever read, and would pressure someone to delete a
        // true sentence from it. The rule that generalizes: judge a doc against the surface ITS READER has.
        //
        // The real gap #585 was pointing at is the one this rule now closes — nothing held a SHIPPED product
        // skill to the symbols a product can actually call — and it is the opposite direction from the one
        // the criterion described.
        test "framework skills are not a judged doc surface, and no product skill is a framework skill (#585/#597)" {
            let frameworkSkills =
                Directory.EnumerateDirectories(repoPath "src")
                |> Seq.map (fun d -> Path.Combine(d, "skill", "SKILL.md"))
                |> Seq.filter File.Exists
                |> List.ofSeq

            Expect.isNonEmpty
                frameworkSkills
                "there are framework skills under src/*/skill/ — if there are none, this test's subject is gone \
                 and the comment above it is stale."

            // The rule's subject is `template/product-skills/**` and the shipped scaffold. Not one framework
            // skill may leak into it: they are allowed to teach internals, so judging one would either report
            // a false defect or, worse, force a true sentence out of a doc whose readers can call the symbol.
            let judgedDocs = docSymbols |> List.map (fun s -> s.Doc) |> List.distinct

            let leaked =
                judgedDocs
                |> List.filter (fun doc -> doc.StartsWith("src/", StringComparison.Ordinal) && doc.Contains "/skill/")

            Expect.isEmpty
                leaked
                "no `src/*/skill/SKILL.md` may enter the judged set. A framework skill is NOT shipped (not \
                 packed, not in template/product-skills/), its readers reach internals through \
                 InternalsVisibleTo, and `Viewer.traceStartCapture` is exactly such a seam — so teaching it \
                 there is correct. Judge a doc against the surface ITS READER has."
        }

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

                | Ok(pinned, _pinnedTypes) ->
                    let undeclared =
                        docSymbols
                        |> List.filter (fun s -> not (resolvesInPin pinned s))
                        |> List.filter (fun s -> not (docLedger.Contains(docKey s)))
                        |> List.map renderDocSymbol

                    let rendered = String.concat "; " undeclared
                    let pin = readAxis uiAxis

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


        // #611 — THE SAME QUESTION, ASKED OF CASES.
        //
        // The rule above judges `Module.member` and is blind to union cases and record fields by
        // construction (`callRegex` demands a lowercase member; a case is capitalised and hangs off a TYPE).
        // So a shipped mirror could declare a case the pinned package does not export, and every gate in
        // this repo stayed green. That is not hypothetical: #535 added `ViewerEffect.Persist` to the shipped
        // SkiaViewer mirror — M-MIR/TYPE COMPELS it, since a mirrored type must match src member-for-member —
        // and published FS.GG.UI.SkiaViewer 0.9.0 exports 15 `ViewerEffect` cases, of which `Persist` is not
        // one. The `val` half of that same commit (`Viewer.runAppWithPersistence`) WAS caught and is
        // ledgered. The case half was not, because nothing could see it.
        //
        // This is #550's rule applied to types, and it is the half #611 calls the more valuable one: it
        // would have caught `Persist` on the day #535 landed, without anybody reasoning about it.
        //
        // Judged from DECLARATIONS, not prose — `| Persist of effects: …` under `type ViewerEffect =` is
        // unambiguous in a way a sentence never is.
        //
        // RELEASE-PENDING (#587) — THE WAIVER BELOW IS NOT OPTIONAL, AND ITS ABSENCE WEDGED THE RELEASE LANE.
        // This rule shipped (#611) without the deferral its val-level siblings above already carried, and the
        // omission is invisible until the one commit it breaks: a RELEASE. On that commit `$(FsGgUiVersion)`
        // names a version nuget.org does not carry yet — the merge is what publishes it — so `pinnedSurface`
        // cannot restore, and a bare `failtest` here hard-fails NU1102 on the required `Deterministic gate`
        // with `enforce_admins` ON and `--admin` forbidden. The commit whose whole job is to be merged CANNOT
        // BE MERGED. 0.9.1 is the first release cut since #611 landed, and it is how this was found.
        //
        // The bounds are copied from the val-level sibling, not re-invented, because the safety IS the bounds:
        // only an NU1102 (never an FS0039, never a feed error), only on the $(FsGgUiVersion) axis, only when
        // THIS commit bumped it, and NEVER in the release lane — where the packages are DUE, not pending.
        testCase "every union case and record field a SHIPPED mirror declares exists in the PINNED package" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API" with
            | null | "" ->
                let uiPin = readAxis uiAxis

                match pinnedSurface.Value with
                | Error why when failedOnlyOnUnpublishedUiPin why uiPin && not releaseLane ->
                    match bumpedInCommitUnderTest packagesPropsRel uiAxis with
                    | Ok true ->
                        skiptest
                            $"RELEASE-PENDING: this commit bumps $({uiAxis}) to {uiPin}, which nuget.org does \
                              not carry yet — merging it is what publishes it. The pinned TYPE surface cannot \
                              be read, so the case/field-vs-pin rule is DEFERRED to the publish; it is NOT \
                              passing. (The mirror may legitimately declare cases that only {uiPin} will \
                              export — `ViewerEffect.Persist` is exactly that, and judging it here would fail \
                              the release that publishes it.)\n\n{why}"
                    | Ok false ->
                        failtest
                            $"the feed does not carry the FS.GG.UI.* packages at $({uiAxis})={uiPin}, and this \
                              commit did NOT bump $({uiAxis}) — so this is NOT the release window. The pin is \
                              stale or typo'd.\n\n{why}"
                    | Error gitWhy -> failtest $"{gitWhy}\n\n{why}"

                | Error why -> failtest why

                | Ok(_, pinnedTypes) ->
                    // The oracle must actually KNOW about types, or this rule excuses everything while
                    // reporting green — the fails-open shape (#266) this file refuses.
                    Expect.isNonEmpty
                        (Map.toList pinnedTypes)
                        "the pinned packages exported ZERO types with cases or fields — the TYPE oracle has \
                         stopped seeing the surface. That is a defect in this test, not an empty framework."

                    Expect.isNonEmpty
                        mirrorTypeMembers
                        "the shipped mirror declares union cases / record fields (if this is empty the \
                         extractor has stopped reading them, and the rule below judges nothing)."

                    let undeclared =
                        mirrorTypeMembers
                        |> List.filter (fun m ->
                            // Against the package this mirror IS, never against "some package that happens
                            // to have a type of that name" — see the (package, type) key in the oracle.
                            match Map.tryFind (packageForNamespace m.Namespace, m.Type) pinnedTypes with
                            | Some members -> not (members.Contains m.Member)
                            // The pin has NO type of that name — and that is the rule at its SHARPEST, not
                            // an exemption. A whole type a product cannot reach is strictly worse than one
                            // missing case on a type it can.
                            //
                            // Returning `false` here was the comfortable choice and a fails-open one:
                            // "the pin has never heard of this type" and "the pin is happy with this type"
                            // would share a verdict (#266), and the hole would be invisible — a mirror could
                            // evade the rule ENTIRELY by declaring a type the pin does not know, which is
                            // exactly what an unreleased type IS. It is green today because all 2,083
                            // members of all 21 mirrors resolve to a type the pin carries; if that stops
                            // being true it must be somebody's decision, not this branch's silence.
                            | None -> true)
                        |> List.filter (fun m -> not (docLedger.Contains(typeMemberKey m)))
                        |> List.map (fun m -> $"{m.Type}.{m.Member} ({m.Doc}:{m.Line})")

                    let rendered = String.concat "; " undeclared
                    let pin = readAxis uiAxis

                    Expect.isEmpty
                        undeclared
                        $"these union cases / record fields are DECLARED by a shipped api-surface mirror and \
                          are exported by NO package a scaffolded product restores at $({uiAxis})={pin}. A \
                          product author reading the mirror is told about a case they cannot construct — the \
                          #550 class, one type-system level down, and invisible to every other gate here \
                          because they all judge `Module.member` and a case is neither.\n\n\
                          Fix the MIRROR to declare what the released package exports, or — if the member is \
                          genuinely unreleased and the mirror must carry it anyway (M-MIR/TYPE compels a \
                          mirrored type to match src member-for-member, so this WILL happen) — declare it in \
                          {docLedgerRel}, whose anti-rot rules retire the entry the moment the release \
                          lands.\n\n\
                          Undeclared: {rendered}"

            | _ -> skiptest "FS_GG_SKIP_TEMPLATE_PINNED_API is set — the case-vs-pin rule did NOT run."

        // #611 — THE ORACLE, ANCHORED. The rules above are only as good as `readTypeSurface`, and every way
        // it can be wrong is SILENT:
        //
        //   * blinded (it stops seeing cases) -> the case rule finds nothing and reports GREEN;
        //   * widened (it invents members)    -> a real violation is excused and it reports GREEN.
        //
        // Both were live bugs in this file's first cut, and neither made a test red. So the oracle is
        // pinned to facts about a package that is PUBLISHED AND IMMUTABLE — FS.GG.UI.SkiaViewer 0.9.0 —
        // and a published (id, version) cannot change under us. If these stop holding, the reader broke.
        testCase "the pinned-TYPE oracle reads a published DU the way F# actually emits it" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API" with
            | null | "" ->
                match oracleSurface.Value with
                | Error why -> skiptest why
                | Ok(_, pinnedTypes) ->
                    match Map.tryFind ("FS.GG.UI.SkiaViewer", "ViewerEffect") pinnedTypes with
                    | None ->
                        failtest
                            $"the oracle sees no `ViewerEffect` in FS.GG.UI.SkiaViewer {oracleVersion} at \
                              all. It has gone BLIND, and a blind oracle passes every rule above by finding \
                              nothing."
                    | Some cases ->
                        // Multi-field case (emitted `NewOpenWindow`) and nullary case (emitted
                        // `get_CloseWindow`): the two shapes a union case takes in IL, and the reader must
                        // read both. Missing the nullary half would hide exactly the cases that look most
                        // like ordinary properties.
                        Expect.isTrue
                            (cases.Contains "OpenWindow")
                            "the oracle lost `ViewerEffect.OpenWindow` — a case with fields, emitted as \
                             `NewOpenWindow`. The union-case reader is broken."

                        Expect.isTrue
                            (cases.Contains "CloseWindow")
                            "the oracle lost `ViewerEffect.CloseWindow` — a NULLARY case, emitted as a \
                             static property `get_CloseWindow` and not as a `New…` factory. Reading only \
                             `New…` silently drops every nullary case."

                        // The negative half, and the one no other test can reach. `Tag` and `Item` are
                        // compiler-generated on EVERY union; if they appear, the reader is matching IL
                        // SHAPE (a `get_` prefix) instead of the compiler's `CompilationMappingAttribute`,
                        // and a widened oracle excuses real violations while every test stays green.
                        Expect.isFalse
                            (cases.Contains "Tag" || cases.Contains "Item")
                            "the oracle invented `ViewerEffect.Tag` / `.Item` — compiler-generated members \
                             that are not union cases. It is reading IL shape rather than the F# compiler's \
                             own CompilationMappingAttribute, and a WIDER oracle EXCUSES a real violation."

                        // The NEGATIVE half of the reader's ground truth: `oracleVersion` predates #535, so
                        // it genuinely does not carry `Persist`. This is a fact about THAT version and no
                        // other — asserting it against `$(FsGgUiVersion)` is exactly what reddened `main`
                        // once the pin moved past #535 (#688). The message names the version it READ, so it
                        // can never again accuse an innocent 0.9.0 of a fact about some other release.
                        Expect.isFalse
                            (cases.Contains "Persist")
                            $"published FS.GG.UI.SkiaViewer {oracleVersion} now exports `ViewerEffect.Persist` \
                             — which it cannot, since a published version is immutable. The oracle is reading \
                             something other than {oracleVersion} (a locally-packed package leaking into the probe \
                             folder is the classic cause), and the ledger entry it justifies is void."
            | _ -> skiptest "FS_GG_SKIP_TEMPLATE_PINNED_API is set — the oracle anchor did NOT run."

        // #611 / #594 — THE PENDING-RELEASE LEDGER'S CLAIM, PROVED. TWICE.
        //
        // An entry in #594's ledger BUYS an omission from the shipped mirror: M-MIR/TYPE stops demanding
        // the member, and the reader is never told the case exists. What it buys it with is the claim "the
        // pinned package does not carry this member" — and P-PEND/PIN, the rule that supposedly guards
        // that claim, only checks the entry's version STAMP equals the current pin. It never opens the
        // package. If the claim is false, the ledger suppresses a member a product could have used, and
        // every gate in the repo is green.
        //
        // So it is asked of the package, and asked twice, because the two witnesses fail differently:
        //
        //   * the METADATA oracle (`readTypeSurface`) — cheap, reads every entry, and is code I wrote;
        //   * the F# COMPILER (`nameof Type.Case` against the restored pin) — the ground truth, and the
        //     only witness that cannot share a bug with the oracle it is checking.
        //
        // Only arity-0 types are compile-probed: `nameof` on a generic type needs its type arguments, and
        // a probe that fails for a reason other than the one claimed proves nothing. The metadata oracle
        // judges every entry regardless, so a generic entry is checked, just not twice.
        testCase "every PENDING-RELEASE entry really is absent from the pinned package (#594's claim)"
        <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API", pendingMembers with
            | (null | ""), [] ->
                // Empty is a legitimate state — every pending member has been released. But the FILE going
                // missing is not: this rule would then check nothing and report green, and #594's ledger
                // would have been deleted with nobody noticing. "Nothing to check" and "checked, and it's
                // fine" must not share a verdict (#266).
                Expect.isTrue
                    (File.Exists pendingLedgerPath)
                    $"{pendingLedgerRel} does not exist. #594's pending-release ledger is the file this \
                      rule verifies, and without it the rule is a silent no-op — it would report green \
                      forever while checking nothing."

                skiptest "#594's pending-release ledger is empty — every pending member has been released."

            | (null | ""), entries ->
                match pinnedSurface.Value with
                | Error why -> skiptest why
                | Ok(_, pinnedTypes) ->
                    // Witness 1: the metadata oracle, on every entry.
                    let released =
                        entries
                        |> List.filter (fun e ->
                            e.Namespaces
                            |> List.exists (fun ns ->
                                match Map.tryFind (packageForNamespace ns, e.Type) pinnedTypes with
                                | Some members -> members.Contains e.Member
                                | None -> false))
                        |> List.map (fun e -> $"{e.Type}.{e.Member} ({pendingLedgerRel}:{e.Line})")

                    let renderedReleased = String.concat "; " released

                    Expect.isEmpty
                        released
                        $"these are declared in {pendingLedgerRel} as members the pin does NOT carry — and \
                          the pinned package EXPORTS them. The claim is false, so the entry is buying an \
                          omission it has not paid for: the shipped mirror is HIDING a member a product on \
                          the pin could use, and M-MIR/TYPE has been told not to mind. Either the release \
                          landed (delete the entry and grow the mirror) or the entry was never \
                          true.\n\nReleased: {renderedReleased}"

                    // Witness 2: the compiler. Ground truth, and it cannot share a bug with witness 1.
                    let probeable =
                        entries |> List.filter (fun e -> not (e.Type.Contains "`"))

                    match probeable with
                    | [] -> ()
                    | probeable ->
                        let namespaces =
                            probeable |> List.collect (fun e -> e.Namespaces) |> List.distinct |> List.sort

                        let lines = probeable |> List.map (fun e -> $"{e.Type}.{e.Member}")
                        let exitCode, output = runNameofProbe namespaces lines
                        let uiPin = readAxis uiAxis

                        if exitCode = 0 then
                            let rendered =
                                probeable |> List.map (fun e -> $"{e.Type}.{e.Member}") |> String.concat "; "

                            failtest
                                $"the pending-release ledger says the pin does not carry these, and they \
                                  COMPILE against it. The omission they buy from the shipped mirror is \
                                  unpaid for.\n\nCompiled fine: {rendered}"

                        elif failedOnlyOnUnpublishedUiPin output uiPin then
                            skiptest
                                $"release window: the pinned FS.GG.UI.* packages at {uiPin} are not \
                                  published yet, so the claim cannot be probed on this commit."
                        else
                            // The build failed — but it must have failed for THE REASON CLAIMED. An FS0039
                            // that never names the member would let ANY broken probe "prove" ANY entry,
                            // which is the fails-open shape this whole file is written against.
                            let unresolved =
                                output.Replace("\r\n", "\n").Split('\n')
                                |> Array.filter (fun l -> l.Contains "FS0039")

                            // F5 — the member is matched QUOTED, as F# writes it ("...member named
                            // 'Persist'"). A bare substring test would let an FS0039 about
                            // `PersistRunEvidence` — a real case of this very DU — "prove" that `Persist`
                            // is absent. Two entries, one diagnostic, and the shorter name rides in free.
                            let unproven =
                                probeable
                                |> List.filter (fun e ->
                                    let quoted = $"'{e.Member}'"
                                    unresolved |> Array.exists (fun l -> l.Contains quoted) |> not)
                                |> List.map (fun e -> $"{e.Type}.{e.Member}")

                            let renderedUnproven = String.concat "; " unproven

                            Expect.isEmpty
                                unproven
                                $"the probe failed to build, but NOT with an FS0039 naming these pending \
                                  members — so their absence from the pin is UNPROVEN, and the failure is \
                                  something else (a malformed probe, a feed error, a package that no longer \
                                  restores). A probe that fails for the wrong reason proves \
                                  nothing.\n\nUnproven: {renderedUnproven}\n\nProbe \
                                  output:\n{output}"

            | _ -> skiptest "FS_GG_SKIP_TEMPLATE_PINNED_API is set — the pending-release claim was NOT probed."

        // Anti-rot 1 (stale). The release landed and the symbol is reachable now; the excuse has outlived
        // its reason. This is the rule that retires a ledger entry at exactly the right moment.
        testCase "no doc-ledger entry names a symbol the PINNED package now exports" <| fun _ ->
            match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API", Set.isEmpty docLedger with
            | (null | ""), false ->
                match pinnedSurface.Value with
                | Error _ -> skiptest "the pinned surface could not be read — the rule above reports why."
                | Ok(pinned, pinnedTypes) ->
                    let staleVals =
                        docSymbols
                        |> List.filter (fun s -> docLedger.Contains(docKey s) && resolvesInPin pinned s)
                        |> List.map docKey

                    // #611 — and the same retirement for a ledgered CASE or FIELD. Without this, the type
                    // half of the ledger is write-only: entries go in and nothing ever takes them out, so
                    // `ViewerEffect.Persist` would keep excusing itself for as long as the file exists —
                    // long after the release that makes it reachable. An exemption that cannot expire is
                    // not an exemption, it is a hole.
                    let staleTypes =
                        mirrorTypeMembers
                        |> List.filter (fun m ->
                            docLedger.Contains(typeMemberKey m)
                            && (match Map.tryFind (packageForNamespace m.Namespace, m.Type) pinnedTypes with
                                | Some members -> members.Contains m.Member
                                | None -> false))
                        |> List.map typeMemberKey

                    let stale = staleVals @ staleTypes
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
            // #611 — BOTH kinds of subject, because the ledger now carries both. Leaving the type members
            // out would not merely miss a phantom: `ViewerEffect.Persist` is a live doc site, so this rule
            // would call the ONLY entry keeping the build green a phantom and demand its deletion — a gate
            // that orders you to remove the exemption another gate requires. The two halves have to see the
            // same world.
            let live =
                Set.union
                    (docSymbols |> List.map docKey |> Set.ofList)
                    (mirrorTypeMembers |> List.map typeMemberKey |> Set.ofList)

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
