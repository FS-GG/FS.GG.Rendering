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
open FS.GG.TestSupport

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
// `PinPending` and `validate-template-payload-pins.fsx`'s `releasePending` already carry.
//
// AND THE DEFERRAL IS STRUCTURAL, NOT REMEMBERED (#673). It is written ONCE, inside `PinnedApi`, which
// owns every way of asking nuget.org about the pin and hands out no un-waived one. A rule that "forgets"
// the waiver therefore does not wedge the next release — IT DOES NOT COMPILE. That is not tidiness: the
// waiver used to be copy-pasted per rule, and #611 duly added a pin-probing rule WITHOUT it, which is what
// wedged 0.9.1 (#642, #651). The bounds, and the bill, are in `PinnedApi`'s header; each conjunct is still
// load-bearing, and there is exactly one copy of them.
//
// SKIPPED IS NOT PASSED. The probe genuinely cannot run in the window, so it says so and is reported
// IGNORED — it does not report green having verified nothing, which is the fails-open shape (#266) this
// file's own header forbids. The three structural tests above it are offline and keep running, so the
// extractor, the launch seam and the mirror are still asserted on a release PR; it is the pin-grounded
// layer, and only that, which defers to the publish.

// ---------------------------------------------------------------------------------------------
// Repo layout
// ---------------------------------------------------------------------------------------------

let private repoRoot = RepositoryRoot.value

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
      /// Dotted module path WITHIN the namespace (`ControlsElmish`, `ControlsElmish.Perf`). This is
      /// the spelling a `nameof` needs under `open <Namespace>`, and `Name` is not: a nested module
      /// cannot be named bare.
      Path: string
      /// INNERMOST module name, as a call site spells it (`Viewer`, `ControlsElmish`, `Perf`).
      Name: string
      /// `val` members declared DIRECTLY by that module — not by its children.
      Members: Set<string> }

let private namespaceRegex = Regex(@"^namespace\s+([\w.]+)", RegexOptions.Compiled)

/// `module` and `val`, with their INDENT, so a nested declaration can be told from a column-0 one.
/// Shared by both mirror walks below — there used to be two spellings of "what is a module
/// declaration", and the weaker one is what #648 was.
///
/// `val internal` and `module internal` are not product surface (#585's subject, not this file's),
/// so the `val` regex refuses the former and the module regex CAPTURES the latter — an internal
/// module must still be TRACKED, or its `val`s fall through to the nearest public ancestor and
/// invent members nobody exports (`module internal Coalescing` inside `ControlsElmish` is exactly
/// that shape). Track it; mark it; judge nothing under it.
let private mirrorModuleRegex =
    Regex(
        @"^(?<indent>\s*)(?:\[<[^>]*>\]\s*)*module\s+(?<access>internal\s+|private\s+)?(?<name>\w+)",
        RegexOptions.Compiled
    )

// #695 convergence: the one `.fsi` `val` reader, prime-aware, lives in TestSupport. This used a
// no-prime `[a-z]\w*` copy that silently missed `val checked'` (a fail-open hole, #598); folding onto
// SurfaceSignature.publicValRegex both dedups and closes it.
let private mirrorValRegex = SurfaceSignature.publicValRegex

/// Parse one mirrored `.fsi` into the modules a call site can NAME — every public one, at every
/// depth, each owning only the `val`s it declares ITSELF.
///
/// IT USED TO READ COLUMN-0 MODULES ONLY, and that was #648. A nested `module Perf` inside `module
/// ControlsElmish` is its own type in the assembly (`ControlsElmish+Perf`), its own key in the
/// pinned-package oracle below (which reads `NestedPublic` types and always did), and its own name
/// at a call site (`ControlsElmish.Perf.runScript`). Reading only column-0 declarations left the
/// EXTRACTOR blind to a module the ORACLE could see, and the two disagreeing is the whole bug:
/// `isFrameworkCall` found no module named `Perf`, returned false, and every `Module.Submodule.member`
/// a shipped doc named was dropped as "the product calling itself" — silently, with no diagnostic.
/// It fails in the DANGEROUS direction: not a false alarm, a false PASS, on the exact spelling
/// `fs-gg-elmish` teaches product authors to drive their UI with.
///
/// The old reading of this was that #504's subject IS the column-0 entry points `Program.fs` writes,
/// so the narrow walk was adequate and `mirrorValSymbols` (below) could keep its own, correct one.
/// That reasoning had a hole: `frameworkModulesByName` does not only feed #504. It feeds
/// `isJudgedDocModule`, and #589's subject is every spelling a shipped DOC names — nested ones
/// included. One walk now, so the two cannot drift apart again.
///
/// Attributing a nested `val` to the PARENT — which the old walk also did, since its `val` regex
/// matched at any indent — invents `ControlsElmish.runScript` (from `Perf.runScript`) and
/// `ControlsElmish.foreground` (from `DesignTokens.Light`), members nothing exports. Nothing looked
/// them up, so it never fired; it is fixed here because the correct walk gives it away for free, and
/// a member set that lies is a trap left armed for the next reader.
let private parseMirrorFile (path: string) =
    let mutable ns = ""

    // (indent, name, isInternal), innermost first. A declaration at indent N closes every module
    // opened at indent >= N, which is what puts a parent-level `val` back under its parent after a
    // nested module ends.
    let mutable stack: (int * string * bool) list = []

    // Keyed by (namespace, PATH): the namespace is bound when the module is declared, not read off a
    // mutable after the loop, so a file that ever declares two namespaces attributes each module to
    // the one it was actually written under instead of to whichever came last.
    let members = Collections.Generic.Dictionary<string * string, ResizeArray<string>>()
    let declared = ResizeArray<string * string>()

    let closeTo (indent: int) =
        stack <- stack |> List.skipWhile (fun (i, _, _) -> i >= indent)

    let underInternal () = stack |> List.exists (fun (_, _, isInternal) -> isInternal)
    let currentPath () = stack |> List.rev |> List.map (fun (_, name, _) -> name) |> String.concat "."

    for line in File.ReadAllLines path do
        let nsMatch = namespaceRegex.Match line
        let moduleMatch = mirrorModuleRegex.Match line
        let valMatch = mirrorValRegex.Match line

        if nsMatch.Success then
            ns <- nsMatch.Groups.[1].Value
            stack <- []
        elif moduleMatch.Success then
            let indent = moduleMatch.Groups.["indent"].Value.Length
            closeTo indent

            stack <-
                (indent, moduleMatch.Groups.["name"].Value, moduleMatch.Groups.["access"].Success)
                :: stack

            // An internal ANYWHERE up the chain makes everything beneath it internal, and an internal
            // module is not a name a product can reach: `RetainedRender` must never become framework.
            if ns <> "" && not (underInternal ()) then
                let key = ns, currentPath ()

                if not (members.ContainsKey key) then
                    members.[key] <- ResizeArray()
                    declared.Add key
        elif valMatch.Success then
            let indent = valMatch.Groups.["indent"].Value.Length
            closeTo indent

            if ns <> "" && not (underInternal ()) && not stack.IsEmpty then
                match members.TryGetValue((ns, currentPath ())) with
                | true, vals -> vals.Add valMatch.Groups.["name"].Value
                | _ -> ()

    declared
    |> Seq.map (fun (moduleNs, modulePath) ->
        { Namespace = moduleNs
          Path = modulePath
          Name = modulePath.Substring(modulePath.LastIndexOf '.' + 1)
          Members = Set.ofSeq members.[(moduleNs, modulePath)] })
    |> List.ofSeq

/// EVERY public module the mirror declares, INCLUDING the ones that declare no `val` of their own.
///
/// The old walk dropped a member-less module, which was harmless when only column-0 modules existed
/// (a column-0 module with no `val`s declares only types, and no `Module.member` can name it). It is
/// NOT harmless now: a CONTAINER — `module DesignTokens` around `Light`/`Dark`, `module Audio` around
/// `Cmd` — legitimately declares nothing itself, and dropping it would take its NAME out of the closed
/// world. A doc naming `DesignTokens.foo` would then be read as the product calling itself and go
/// unjudged, which is the fails-open shape (.github#266) this file refuses. A module that exports
/// nothing is exactly a module on which every member a doc names is a violation, so it must be IN.
let private frameworkModules =
    Directory.EnumerateFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect parseMirrorFile
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

/// Everything that ENCLOSES a module: its namespace, plus its parent modules if it is nested.
/// `ControlsElmish.Perf` is enclosed by `FS.GG.UI.Controls.Elmish.ControlsElmish`; the top-level
/// `ControlsElmish` is enclosed by `FS.GG.UI.Controls.Elmish` alone.
let private enclosingPath (m: FrameworkModule) =
    match m.Path.LastIndexOf '.' with
    | -1 -> m.Namespace
    | i -> $"{m.Namespace}.{m.Path.Substring(0, i)}"

/// Reached through an `open` rather than spelled out. ONE definition, because BOTH halves branch on it
/// — `admittedCandidates` admits every candidate when it holds, and `isFrameworkCall` must then rule out
/// the product's own module of that name — and two spellings of "unqualified" that drift apart is the
/// two-halves-disagree shape this file keeps paying for (#648, #683).
let private isUnqualified (qualifier: string) = qualifier.TrimEnd('.') = ""

/// The mirrored modules a `[qualifier.]Module` spelling could REACH: the ones the mirror declares
/// under that name, keeping only those the qualifier admits — either the spelling is unqualified
/// (reached through an `open`, so every candidate is in play) or the qualifier names some SUFFIX of
/// what encloses the module, which is precisely what F# resolution accepts given the `open`s in scope.
/// `FS.GG.Audio.Host.OpenAlBackend.create` (the whole path) and `ControlsElmish.Perf.runScript` (the
/// tail of one, under `open FS.GG.UI.Controls.Elmish`) both admit their module; a qualifier that is
/// anything else — `AppRoot.` — admits nothing, and is the product calling itself.
///
/// IT USED TO DEMAND `m.Namespace = qualifier`, an EXACT match against the namespace. That reads a
/// nested call as unresolvable even once the module is known: `ControlsElmish.Perf.runScript` offers
/// the qualifier `ControlsElmish`, and `Perf`'s namespace is `FS.GG.UI.Controls.Elmish` — never equal,
/// so the call was dropped. Both halves of #648 had to move: the extractor could not SEE `Perf`, and
/// this could not have resolved it if it had.
///
/// The suffix must be DOT-ALIGNED. A bare `EndsWith` would let the qualifier `mish` match
/// `...Controls.Elmish`, and a substring is not a name.
///
/// ONE candidate set, and #683 is why it is its own function rather than a clause inside
/// `isFrameworkCall`. "Which modules can this spelling reach?" is asked TWICE — once to decide the
/// spelling is framework at all, and once to decide WHICH PACKAGE'S module the pinned oracle must be
/// asked about. Answering it two ways is how a module map keyed on the bare name got to excuse a doc
/// with an unrelated package's member; the two questions now read the same answer.
let private admittedCandidates (qualifier: string) (moduleName: string) =
    match frameworkModulesByName.TryFind moduleName with
    | None -> []
    | Some candidates ->
        if isUnqualified qualifier then
            candidates
        else
            let qualified = qualifier.TrimEnd('.')

            candidates
            |> List.filter (fun m ->
                let enclosing = enclosingPath m
                enclosing = qualified || enclosing.EndsWith($".{qualified}", StringComparison.Ordinal))

/// A match is a framework call only if the mirror declares the module AND the qualifier agrees.
let private isFrameworkCall (qualifier: string) (moduleName: string) =
    match admittedCandidates qualifier moduleName with
    | [] -> false
    | _ ->
        // An unqualified spelling admits every candidate, so the only thing left to rule out is the
        // product's OWN module of that name (`AppRoot.LayoutEvidence` reached through its `open`).
        not (isUnqualified qualifier && productModules.Contains moduleName)

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

/// The mirrored module a `Module.member` resolves to: the one that DECLARES the member, else — when no
/// mirrored module of that name declares it — ANY module of that name.
///
/// The fallback is deliberate, and it is what `callNamespace` below needs. The probe emits a `nameof`
/// for EVERY call site, so it must emit an `open` for every call site too. Resolving through
/// `owningModule` instead would drop exactly the call sites the mirror is MISSING, and the probe would
/// then fail FS0039 on an unopened namespace and blame the PIN — reporting "the framework grew API a
/// scaffolded product cannot reach" when the pin is fine and only the mirror is stale. A wrong diagnosis
/// on a real failure is worse than no diagnosis.
///
/// ONE resolver, used by `callNamespace`, `probeSpelling` and `docPackages`. They each need a different
/// FIELD of the answer (namespace, path, package) and they must all get it from the SAME module — see
/// `probeSpelling` for what a disagreement costs.
let private resolveModule (moduleName: string) (memberName: string) =
    frameworkModulesByName
    |> Map.tryFind moduleName
    |> Option.bind (fun candidates ->
        candidates
        |> List.tryFind (fun m -> m.Members.Contains memberName)
        |> Option.orElse (List.tryHead candidates))

/// The namespace to `open` (and the package to reference) for a call site.
let private callNamespace (call: CallSite) =
    resolveModule call.Module call.Member |> Option.map (fun m -> m.Namespace)

/// How the PROBE must spell a call site: the module's path WITHIN its namespace, which is the bare
/// name for a top-level module and a dotted one for a nested module.
///
/// `nameof Perf.runScript` does not compile under `open FS.GG.UI.Controls.Elmish` — `Perf` is reached
/// through its parent, so the probe has to write `ControlsElmish.Perf.runScript`. The extractor keys a
/// call site by its INNERMOST name (that is what a call site spells, and what the oracle keys on), so
/// the path has to be recovered here or the probe would emit a line that cannot bind and blame the PIN
/// for it — a wrong diagnosis on a real failure, which is worse than no diagnosis.
///
/// It resolves through `resolveModule`, the SAME function `callNamespace` uses, and that is a
/// correctness requirement rather than tidiness: the probe writes `open <Namespace>` from one and
/// `nameof <Path>.<member>` from the other. Let the two pick DIFFERENT candidates of a name the mirror
/// declares twice — `Cmd` is both `Authoring.Cmd` and `Audio.Cmd` — and the probe opens one package and
/// names a path from another, which cannot bind. It would then report the PIN as missing an API that is
/// present. One resolver, so they cannot disagree.
///
/// `Program.fs` calls no nested module TODAY, so nothing currently depends on the nested spelling. That
/// is exactly why it is written now: the day someone adds `ControlsElmish.Perf.runScript` to the
/// template, the probe must fail on the PIN or pass on the PIN, and not fail on itself.
let private probeSpelling (call: CallSite) =
    resolveModule call.Module call.Member
    |> Option.map (fun m -> m.Path)
    |> Option.defaultValue call.Module

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
//
// The MACHINERY moved (#673). It is private to `PinnedApi` below — the module that owns every way of
// asking nuget.org about the pin — because a waiver each reader has to REMEMBER is a waiver the next
// reader forgets. See that module's header for the bounds and for the bill #611 ran up.
// ---------------------------------------------------------------------------------------------

/// The one axis the waiver can ever apply to: the axis THIS repo's own merge publishes. It is named out
/// here because the RULES quote it in their messages ("…at $(FsGgUiVersion)=0.9.2"); the PREDICATES that
/// act on it are `PinnedApi`'s alone, and are reachable from nowhere else.
let private uiAxis = "FsGgUiVersion"

// ---------------------------------------------------------------------------------------------
// The pinned packages themselves: which ones exist, which one ships a namespace, and where the probes
// restore them to. Plumbing shared by BOTH pin probes — the compile probe and the metadata read — which
// is why it stays out here while the probes themselves are private to `PinnedApi` below.
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

/// `Module.member` inside the F# fences of a product skill — the block a reader COPIES, which is what
/// makes it the sharpest subject. The PROSE around it is judged too, by `skillProseSymbols` below (#597);
/// this extractor stays fence-only so the two can be read, and reasoned about, separately.
///
/// #669: WHERE THE FENCE IS READ, and where it is not. This used to hand-roll its own reader —
/// backticks only, toggled, F# = `StartsWith "fsharp"` — which disagreed with S-DOC's about what an F#
/// block IS: it missed `fs`/`fsx`/`fsi`/`f#` and opened on anything merely prefixed `fsharp`. Fences are
/// now `MarkdownFences.scan`'s answer, once, for all three extractors. What stays here is what is
/// genuinely this rule's own: an F# block is read as F# SOURCE — `stripCommentsAndStrings`, so a name in
/// a comment or a string literal is not a call site.
///
/// THE FENCES ARE READ FROM THE RAW DOCUMENT, and the F#-level erasure is applied AFTERWARDS, to the
/// lines the scan says are F#. A fence is a markdown fact; `(* ... *)` is not one, and letting the F#
/// erasure run first meant a block comment could eat a fence line. `eraseKeepingLines` preserves the line
/// count precisely so the erased text can still be indexed by the scan's (1-based) line numbers.
let private skillFenceSymbols =
    Directory.EnumerateFiles(productSkillsRoot, "*.md", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
        let text = (File.ReadAllText path).Replace("\r\n", "\n")
        let erased = (eraseKeepingLines text).Split('\n')

        MarkdownFences.scan text
        |> MarkdownFences.fsharpLines
        |> List.collect (fun line ->
            // Same document, same line count — so the scan's line number indexes the erased text too.
            let source = erased.[line.Number - 1]

            callRegex.Matches(stripCommentsAndStrings source)
            |> Seq.map (fun m ->
                m.Groups.[1].Value,
                { Doc = rel
                  Line = line.Number
                  Module = m.Groups.[2].Value
                  Member = m.Groups.[3].Value })
            |> List.ofSeq))
    |> List.ofSeq

/// The public `val`s of the SHIPPED api-surface mirror, qualified by the INNERMOST module that declares
/// them. The mirror is what `docs/scaffold-map.md` designates a product's authoritative signature set —
/// so a `val` it declares that the pinned package does not export is a signature a product author reads,
/// at length, and cannot call.
///
/// INNERMOST, by indent. A nested `module Perf` inside `module ControlsElmish` is its OWN type in the
/// assembly (`ControlsElmish+Perf`) and is its own name at a call site (`Perf.runScript`) — so
/// attributing its `val`s to the parent invents `ControlsElmish.runScript`, which nothing exports and
/// no doc names. That misreading put nine phantom violations on the first run of this rule.
///
/// This walk used to be the ONLY correct one, because `parseMirrorFile` read column-0 modules alone and
/// was "deliberately left alone: it feeds #504, whose subject IS the column-0 entry points `Program.fs`
/// writes." That was wrong, and #648 is the bill: `parseMirrorFile` also builds `frameworkModulesByName`,
/// which `isJudgedDocModule` gates THIS rule on — so every symbol extracted here under a nested owner
/// (`Perf`, `Live`, `Light`, `Dark`, `Cmd`) was thrown away downstream by a registry that had never
/// heard of the module. Both walks read indents now, and `mirrorModuleRegex`/`mirrorValRegex` are shared
/// with `parseMirrorFile` so a third spelling of "what is a module declaration" cannot appear.
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

/// ONE walk of the mirror, THREE projections — the same discipline `readTypeSurface` keeps on the pin's
/// side, and for the same reason: the two halves of a gate have to agree about what a TYPE is, or the gate
/// reports green on their difference (#648). `arityKey` and `typeDeclRegex` are the shared primitives, so a
/// type is keyed here exactly as IL spells it there.
let private mirrorParse =
    let acc = ResizeArray<TypeMember>()

    /// (namespace, arity-keyed name) for every `type X` the mirror DECLARES — including the ones that carry
    /// no case and no field, which `acc` cannot see. An opaque `type Keymap` has no members to record, and
    /// it is precisely the shape the mirror was found to have dropped.
    let declaredTypes = ResizeArray<string * string>()

    /// The namespaces the mirror actually COVERS. This is what bounds the completeness rule: a namespace no
    /// mirror file declares is one the mirror does not claim to document, so its types are out of scope
    /// rather than missing. The `FS.GG.UI.Controls.Typed.*` carve-out (feature 085, FR-013) falls out of
    /// this structurally — no mirror file declares that namespace — instead of being a hardcoded exception
    /// that would rot the moment the carve-out moved.
    let namespaces = ResizeArray<string>()

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

        // A mirror file with no `namespace` line covers nothing — recording `""` would mint an empty
        // "covered namespace" that matches no pin type, which is harmless, and a pin type whose namespace
        // is genuinely `""` (there is none) would then be judged, which is not. Keep the set honest.
        if ns <> "" then
            namespaces.Add ns

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

                // Guarded exactly as `namespaces` is: a mirror file with no `namespace` line contributes
                // no covered namespace, so it must contribute no declared type either. Guarding one and
                // not the other left the two projections of this walk disagreeing about which files count.
                if ns <> "" then
                    declaredTypes.Add(ns, typeName)

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

    List.ofSeq acc, Set.ofSeq declaredTypes, Set.ofSeq namespaces

let private mirrorTypeMembers =
    let members, _, _ = mirrorParse
    members

/// (namespace, arity-keyed type) every mirror file declares.
let private mirrorDeclaredTypes =
    let _, types, _ = mirrorParse
    types

/// The namespaces the mirror claims to document — the completeness rule's scope (#752).
let private mirrorNamespaces =
    let _, _, namespaces = mirrorParse
    namespaces

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
/// A FENCE OF ANY LANGUAGE IS NEITHER. `skillFenceSymbols` reads only F# blocks; this one must skip EVERY
/// fence, or a ```console block's `dotnet build` becomes a call site. So it reads only what falls OUTSIDE
/// every fence — `MarkdownFences.proseLines`, which is that idea and nothing else (#669). It used to
/// hand-roll the tracking, toggling on any line starting with three backticks: a ```` ~~~ ```` block was
/// invisible to it, and its contents were judged as prose.
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

        File.ReadAllText path
        |> MarkdownFences.scan
        |> MarkdownFences.proseLines
        |> List.collect (fun line ->
            // Prose is English, NOT F#: no `stripCommentsAndStrings` here (a `//` in a sentence is not a
            // comment), and #598's two guards instead — a preceding `/` (prose names PATHS constantly), and
            // the three F# source extensions.
            callRegex.Matches line.Text
            |> Seq.filter (fun m ->
                let precededBySlash = m.Index > 0 && line.Text.[m.Index - 1] = '/'
                not precededBySlash && not (sourceFileExtensions.Contains m.Groups.[3].Value))
            |> Seq.map (fun m ->
                m.Groups.[1].Value,
                { Doc = rel
                  Line = line.Number
                  Module = m.Groups.[2].Value
                  Member = m.Groups.[3].Value })
            |> List.ofSeq))
    |> List.ofSeq

/// Every shipped doc surface, reduced to the symbols this rule may judge — EVERY occurrence, not one per
/// symbol, and each still carrying THE QUALIFIER IT WAS WRITTEN WITH. `docSymbols` below dedups for the
/// verdict; this keeps the sites, because the verdict and the WORK are different questions.
///
/// The qualifier used to be dropped here (`List.map snd`) the moment `isJudgedDocModule` had consumed it,
/// and #683 is what that cost: it is the ONLY thing that says which package a doc means by `Cmd`, so
/// throwing it away left the pinned oracle with no choice but to answer for all of them at once.
let private judgedDocOccurrences =
    List.concat
        [ skillFenceSymbols
          skillProseSymbols
          mirrorValSymbols
          mirrorDocCommentSymbols
          scaffoldSourceDocCommentSymbols ]
    |> List.filter (fun (qualifier, s) -> isJudgedDocModule qualifier s.Module)

/// The judged occurrences as bare symbols — what everything except the pin resolution wants.
let private judgedDocSymbols = judgedDocOccurrences |> List.map snd

/// Every QUALIFIER a judged `docKey` is written with, deduped. `resolvesInPin` judges the symbol under
/// each of them: one docKey, but a doc can reach the same member by more than one spelling, and each
/// spelling is a separate claim about a separate package.
let private docKeyQualifiers =
    judgedDocOccurrences
    |> List.groupBy (fun (_, s) -> docKey s)
    |> List.map (fun (key, occurrences) ->
        key, occurrences |> List.map (fun (qualifier, _) -> qualifier.TrimEnd('.')) |> List.distinct)
    |> Map.ofList

/// Every line a judged `docKey` occurs on. The dedup below is right for the VERDICT (one symbol in one
/// doc is one violation) and badly wrong for the REPORT: `Attr.onChanged` was written in EIGHT
/// doc-comments of Controls/Control.fsi and `distinctBy` showed exactly one of them, so the failure read
/// as four sites when thirteen needed editing. It converges — fix the named line and the next takes its
/// place — but a message that understates the work by 3x sends the reader back around the loop for
/// nothing. Name them all.
let private docKeySites =
    judgedDocSymbols
    |> List.groupBy docKey
    |> List.map (fun (key, sites) -> key, sites |> List.map (fun s -> s.Line) |> List.distinct |> List.sort)
    |> Map.ofList

/// The symbols this rule judges: one verdict per `docKey`.
let private docSymbols =
    judgedDocSymbols
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
/// the same string: `runProbeBuild` (below, since #673 put `PinnedApi` between them) may assume they are,
/// because the namespaces `Program.fs` calls all happen to be package ids — but the mirror at large
/// declares SUB-namespaces that are not —
/// `FS.GG.UI.Controls.Typed`, `FS.GG.UI.Controls.Elmish.Authoring`, `FS.GG.UI.Themes.Default.Theming`.
/// Referencing one of those as a package is an NU1101 for a package that was never supposed to exist.
/// The packages the docs actually talk about — derived from the mirror and the props, never hardcoded,
/// so the set cannot rot as the framework grows.
let private docPackages =
    let fromVals =
        docSymbols
        |> List.choose (fun s -> resolveModule s.Module s.Member |> Option.map (fun m -> packageForNamespace m.Namespace))

    // #611 — and every package whose mirror declares a TYPE, because the case rule judges those and can
    // only judge what the oracle restored. Deriving the restore set from the `val` symbols ALONE (which is
    // all it used to need) silently coupled one rule's coverage to another rule's subject matter: a mirror
    // that declares cases but no `val` would restore no package, every one of its types would be missing
    // from the oracle, and the case rule would pass over them without a word. It happens to cover all 21
    // mirrors today — which is luck, not structure, and luck is what this file is written against.
    let fromTypes =
        mirrorTypeMembers |> List.map (fun m -> packageForNamespace m.Namespace)

    // #683 — and every package declaring a module a judged occurrence could RESOLVE TO, which is not the
    // same set as `fromVals`. That derives one package per symbol, from `resolveModule`'s single pick;
    // the oracle now asks EVERY admitted candidate, and a candidate whose package was never restored has
    // no key in the map. In a per-package oracle an absent key does not excuse the symbol — it ACCUSES
    // it — so an unrestored candidate would read as "the pin does not export this" and report a correct
    // doc as a defect. The restore set has to cover every package the resolution can reach.
    let fromCandidates =
        judgedDocOccurrences
        |> List.collect (fun (qualifier, s) -> admittedCandidates qualifier s.Module)
        |> List.map (fun m -> packageForNamespace m.Namespace)

    // #752 — and every package whose namespace the mirror COVERS, because the completeness rule judges the
    // PIN's types against the mirror, and a package that was never restored contributes no types at all. Its
    // omissions would be invisible and the rule would report green over a package it never opened — the same
    // "an oracle that silently knows nothing about ONE package excuses every symbol in it" hole the header
    // above forbids, reached from the other direction.
    //
    // `fromTypes` is NOT enough for this, and the reason is the same coupling #611 called out: it derives a
    // package only from a mirrored type that carries a CASE OR A FIELD. A mirror file of pure `val`s, or one
    // whose types are all opaque, restores nothing — so the completeness rule's coverage would depend on the
    // case rule's subject matter, and a whole package could go unjudged because none of its mirrored types
    // happened to be a record or a union.
    let fromMirror =
        mirrorNamespaces |> Set.toList |> List.map packageForNamespace

    fromVals @ fromTypes @ fromCandidates @ fromMirror |> List.distinct |> List.sort

// ---------------------------------------------------------------------------------------------
// The oracle: what the PINNED packages actually export.
// ---------------------------------------------------------------------------------------------

/// Module PATH within its namespace -> the members the PUBLISHED assembly exports.
///
/// An F# module compiles to a STATIC class — `abstract` AND `sealed`. A union, a record or a class is
/// never both, so this keys modules and nothing else, which is what a doc's `Module.member` means. Two
/// spellings have to be undone: F# suffixes a module whose name collides with a type's (`module Scene`
/// beside `type Scene` becomes `SceneModule`), and generic arity is mangled (``Foo`1``) — a doc writes
/// neither. Property accessors are recorded under their bare name as well as their `get_`/`set_` one.
///
/// THE PATH, NOT THE BARE NAME, and #683 is the bill for the bare name. `Audio.Cmd` (a `module Cmd`
/// nested in `module Audio`, FS.GG.Audio.Elmish) and `Cmd` (top-level in namespace
/// `FS.GG.UI.Controls.Elmish.Authoring`) are DIFFERENT modules with DISJOINT members — `ofEngine`,
/// `playSfx`, … on one; `none` alone on the other. Keyed on the innermost name they are both `Cmd`,
/// and `readSurfaceAt` then unioned them, so FS.GG.Audio.Elmish's `ofEngine` EXCUSED a UI doc naming
/// `Cmd.ofEngine` — whose reader gets a hard build error. The path is what the MIRROR keys on
/// (`FrameworkModule.Path`), so it is what the two sides can agree on, and agreeing is the whole job:
/// a gate whose two halves disagree about what a module IS reports green on the difference (#648).
/// ONE walk, TWO projections — the modules the assembly DECLARES (with their namespace), and the members
/// each exports. The completeness rule (#752) needs the first and the doc-vs-pin rule needs the second, and
/// a second copy of "what counts as a public module" is how the two come to disagree (#648).
///
/// A MODULE is as omittable as a type, and the first cut of #752 forgot it: `readTypeSurface` excludes
/// modules by construction (`not (isModule td)`), so a completeness rule built on types alone judges only
/// half the surface. The pin at 0.9.2 exports `module Keymap` — twelve rebind functions — and `module
/// KeymapCodec`, and the mirror declares NEITHER. A types-only rule passes both, green, which is the very
/// fails-open shape (#266) this rule exists to close, reproduced inside the fix for it.
let private readModuleSurface (dll: string) : (string * string) list * (string * string) list =
    use stream = File.OpenRead dll
    use pe = new PEReader(stream)
    let md = pe.GetMetadataReader()

    let isPublic (td: TypeDefinition) =
        let visibility = td.Attributes &&& TypeAttributes.VisibilityMask
        visibility = TypeAttributes.Public || visibility = TypeAttributes.NestedPublic

    let isFSharpModule (td: TypeDefinition) =
        td.Attributes.HasFlag TypeAttributes.Abstract
        && td.Attributes.HasFlag TypeAttributes.Sealed

    // Per SEGMENT, because every segment of a nested path carries both manglings.
    let segment (raw: string) =
        let withoutArity = match raw.IndexOf '`' with | -1 -> raw | i -> raw.Substring(0, i)

        if withoutArity.EndsWith("Module", StringComparison.Ordinal) && withoutArity.Length > 6 then
            withoutArity.Substring(0, withoutArity.Length - 6)
        else
            withoutArity

    // The dotted path WITHIN the namespace — `Audio.Cmd` for a module nested in `module Audio`, `Cmd`
    // for a top-level one. The namespace does not appear: it is not part of the mirror's `Path` either,
    // and the PACKAGE (which `readSurfaceAt` pairs this with) is what identifies the assembly.
    let rec pathOf (td: TypeDefinition) =
        let name = segment (md.GetString td.Name)
        let parent = td.GetDeclaringType()

        if parent.IsNil then
            name
        else
            $"{pathOf (md.GetTypeDefinition parent)}.{name}"

    // A module reachable only THROUGH something a product cannot name is not surface. Every ancestor
    // must itself be a public F# module — which is what `module Foo = module Bar` compiles to, and
    // what the mirror's own walk records. Anything else nested in here is the compiler's furniture.
    let rec nameable (td: TypeDefinition) =
        let parent = td.GetDeclaringType()

        if parent.IsNil then
            true
        else
            let pd = md.GetTypeDefinition parent
            isPublic pd && isFSharpModule pd && nameable pd

    // Same reason as `readTypeSurface`'s: a module nested in a module carries an EMPTY namespace in IL —
    // the namespace lives on the outermost enclosing type. Reading `td.Namespace` directly would file every
    // nested module under `""`, no mirror namespace would match, and the rule would quietly judge nothing.
    let rec namespaceOf (td: TypeDefinition) =
        let parent = td.GetDeclaringType()

        if parent.IsNil then
            md.GetString td.Namespace
        else
            namespaceOf (md.GetTypeDefinition parent)

    let declaredModules = ResizeArray<string * string>()

    let members =
        [ for handle in md.TypeDefinitions do
            let td = md.GetTypeDefinition handle

            if isPublic td && isFSharpModule td && nameable td then
                let path = pathOf td

                declaredModules.Add(namespaceOf td, path)

                for methodHandle in td.GetMethods() do
                    let m = md.GetMethodDefinition methodHandle

                    if (m.Attributes &&& MethodAttributes.MemberAccessMask) = MethodAttributes.Public then
                        let memberName = md.GetString m.Name
                        yield path, memberName

                        for prefix in [ "get_"; "set_" ] do
                            if memberName.StartsWith(prefix, StringComparison.Ordinal) then
                                yield path, memberName.Substring prefix.Length ]

    List.ofSeq declaredModules, members

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

/// ONE walk, TWO projections — and they must stay one walk (#752).
///
/// Two rules judge OPPOSITE directions of the same relation. #611 asks *the mirror declares this case; does
/// the pin export it?* (mirror ⊆ pin). #752 asks the converse — *the pin exports this type; does the mirror
/// declare it?* (pin ⊆ mirror) — and the converse is the direction that was never asked, which is why a
/// mirror could omit a type entirely and stay green.
///
/// Both need "what counts as a public type", and a SECOND copy of that predicate is how the two directions
/// come to disagree about what a type IS — the #648 shape, where a gate's two halves report green on their
/// own difference. So the predicate is written once and both projections fall out of the same pass.
///
/// The NAMESPACE is what the completeness rule needs and what the case/field rule never did. It cannot be
/// read off `td.Namespace`: a type nested in a MODULE (`module Foo = type Bar = …`, which
/// `declaredAtTypeLevel` admits) carries an EMPTY namespace in IL — the namespace lives on the outermost
/// enclosing type. Reading it directly would file every module-nested type under `""`, no mirror namespace
/// would ever match, and the rule would quietly judge nothing. Walk to the root and take its namespace.
let private readTypeSurface (dll: string) : (string * string) list * (string * string) list =
    use stream = File.OpenRead dll
    use pe = new PEReader(stream)
    let md = pe.GetMetadataReader()

    // An F# MODULE is abstract+sealed; a type is not.
    let isModule (td: TypeDefinition) =
        td.Attributes.HasFlag TypeAttributes.Abstract
        && td.Attributes.HasFlag TypeAttributes.Sealed

    let rec namespaceOf (td: TypeDefinition) =
        let parent = td.GetDeclaringType()

        if parent.IsNil then
            md.GetString td.Namespace
        else
            namespaceOf (md.GetTypeDefinition parent)

    let declaredTypes = ResizeArray<string * string>()

    let members =
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

                // The pin ⊆ mirror subject (#752). Recorded for EVERY public type, not only the ones that
                // carry a case or a field: an opaque type (`type Keymap`, whose representation the mirror
                // cannot see) has neither, and it is exactly the kind the mirror was found to have dropped.
                declaredTypes.Add(namespaceOf td, name)

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

    List.ofSeq declaredTypes, members

// ---------------------------------------------------------------------------------------------
// THE PINNED SURFACE, AND THE ONLY DOORS TO IT (#673)
//
// Every way of asking nuget.org about the pin is PRIVATE to the module below, and the only way out of it
// is through an accessor that has ALREADY applied the RELEASE-PENDING waiver. That is a structural claim,
// and it is the entire point: a pin-probing rule that "forgets" the waiver no longer fails at release
// time — IT DOES NOT COMPILE, because there is no un-waived accessor left for it to call.
//
// WHY THIS IS A MODULE AND NOT FOUR CAREFUL COPIES. It WAS four careful copies, and the shape was not
// incidental — it was the direct cause of a bug that wedged the release lane. #543 (PR #558) added the
// deferral to the val-level probe. #611 then added a NEW pin-probing rule — "every union case and record
// field a SHIPPED mirror declares exists in the PINNED package" — and it shipped WITHOUT the deferral,
// because nothing forced it to have one.
//
// The omission is invisible except on the one commit it breaks: A RELEASE. In the release window
// $(FsGgUiVersion) names a version nuget.org does not carry yet — the merge is what publishes it — so the
// restore fails NU1102 BY CONSTRUCTION and a bare `failtest` hard-fails the required `Deterministic gate`,
// with `enforce_admins` ON and `--admin` forbidden (ADR-0103). THE COMMIT WHOSE WHOLE JOB IS TO BE MERGED
// CANNOT BE MERGED. It sat latent for the entire life of that rule and surfaced only because 0.9.1 (#587,
// PR #672) was the first release cut since #611 landed; #642 and #651 both hit it. PR #672 fixed the
// INSTANCE by copying the waiver a fourth time — the right call for a release PR, and it reproduced the
// exact shape that caused the bug. #673 is the bill.
//
// The bounds are UNCHANGED. This asks for ONE copy of them, not a weaker one, and each conjunct is still
// load-bearing:
//
//   * ONLY when the failure is EXACTLY "the pinned FS.GG.UI.* packages are not on the feed". Read off the
//     restore we already ran, so the evidence for the waiver is the very diagnostic it excuses. Any OTHER
//     error — an FS0039 from a call site the pin does not export, an NU1101 typo'd id, an NU1603 upward
//     resolution — and the waiver is off: those are the failures these rules exist to catch, and none of
//     them is what a release window looks like.
//
//   * ONLY the $(FsGgUiVersion) axis. FS.GG.Audio.* is pinned in these probes too and ships from ANOTHER
//     repo, where a bump HERE publishes nothing — so an unpublished Audio pin is a real defect EVEN ON THE
//     COMMIT THAT BUMPED IT, and is never waived. A naive "this commit bumped an axis ⇒ waive" would
//     reopen #235: a stale component pin, green.
//
//   * ONLY when THIS commit bumped it. A pin NOBODY bumped that the feed does not carry is stale or
//     typo'd — drift, and still red. That is the half of the check that must survive the waiver.
//
//   * NEVER in the release lane (`FS_GG_VERSION_COHERENCE_RELEASE_LANE`, set job-wide by `release.yml`).
//     The premise is "these packages cannot exist yet — this very commit creates them", which is only true
//     BEFORE the publish. At publish time they are DUE, and a missing one is drift.
//
// TWO FAMILIES OF PROBE, SO TWO DOORS — AND THAT IS NOT SYMMETRY FOR ITS OWN SAKE. A rule can ask about
// the pin in exactly two ways: RESTORE IT AND READ ITS METADATA (`withPinnedSurface`), or COMPILE A
// `nameof` AGAINST IT (`probe`). BOTH fail NU1102 in the release window, so both need the waiver, and
// hiding it behind only the first would leave the identical trap one door over — a `runNameofProbe` any
// new rule could still reach for, un-waived, exactly as #611 reached for the un-waived surface.
//
// AND THE SECOND FAMILY HAD ALREADY DRIFTED. The pending-release rule's compile probe carried its OWN copy
// of the bounds, and a WEAKENED one: "an unresolved UI pin ⇒ skip", with no `releaseLane` conjunct and no
// bumped-in-this-commit conjunct. It never fired, because that rule ALSO read the metadata surface first
// and skipped on ANY error from it — which is the more serious half, and the one to carry away: a merely
// STALE pin (one nobody bumped, which the other three rules correctly redden) failed that restore, and the
// whole rule quietly went IGNORED rather than red. So the bounds existed in the codebase in two strengths,
// the weaker one shadowed by a handler that was weaker still, and NOTHING would have told anyone. One
// implementation of the bounds; two doors through it; no third way in.
//
// SKIPPED IS NOT PASSED — but it is not RED either, and that is the distinction the old pending rule blurred.
// A deferral is honest only when the probe genuinely cannot run (the release window). Skipping because the
// restore failed for some OTHER reason silently retires a rule on exactly the commits it should be judging,
// so every door here FAILS on an unavailability that is not a release window, and defers on nothing else.
// ---------------------------------------------------------------------------------------------

/// What the PINNED packages export, as a rule is allowed to see it.
///
/// There is no way to obtain one of these except from `PinnedApi`, and that is what makes the waiver
/// unforgettable rather than merely documented: a rule cannot hold the pinned surface without having gone
/// through the door that defers in the release window.
type PinnedSurface =
    { /// (package, module path) -> the `val`s the published assembly exports. Keyed by PACKAGE as well as
      /// path for the SAME reason `Types` is, and it took a second bug (#683) to say so: `Cmd` is
      /// FS.GG.Audio.Elmish's `Audio.Cmd` (`ofEngine`, `playSfx`, …) AND FS.GG.UI.Controls.Elmish's
      /// top-level `Cmd` (`none`, alone) — disjoint member sets. Keyed on the bare name they merge, and the
      /// Audio package's `ofEngine` then EXCUSES a UI doc naming `Cmd.ofEngine`, whose reader gets a hard
      /// build error. And by PATH, not by simple name, because that is what the mirror keys on: the two
      /// halves have to agree about what a module IS, or the gate reports green on the difference (#648).
      Modules: Map<string * string, Set<string>>
      /// (package, type) -> the union cases / record fields it exports. Keyed by PACKAGE as well as name
      /// because the pinned packages declare seventeen type names twice or more, and `Scene`'s `Fatal`
      /// case must not excuse its absence from `Layout`'s same-named type.
      Types: Map<string * string, Set<string>>
      /// #752 — (package, namespace, type) for EVERY public type the pin exports. The subject of the
      /// completeness rule, and the one thing `Types` cannot be: `Types` is keyed by name alone and holds
      /// only types that carry a case or a field, so it can neither see an opaque type nor tell
      /// `FS.GG.UI.Controls.Typed.ButtonProps` (deliberately NOT mirrored — feature 085, FR-013) from
      /// `FS.GG.UI.Controls.Widget` (mirrored namespace, simply missing). Without the namespace the
      /// completeness rule would have to accuse both or neither, and accusing a documented carve-out is how
      /// a rule gets ledgered into silence by the first person it wrongly accuses.
      DeclaredTypes: Set<string * string * string>
      /// #752 — (package, namespace, module path) for every public F# MODULE the pin exports. Kept APART
      /// from `DeclaredTypes` for the reason the maps above are kept apart: a module and a type are
      /// different things that may share a name (`module Scene` beside `type Scene`), and a merged set would
      /// let a mirrored `type Scene` excuse an omitted `module Scene`. A module is exactly as omittable as a
      /// type — `module Keymap` (twelve rebind functions) and `module KeymapCodec` are both absent from the
      /// mirror at 0.9.2 — and a types-only completeness rule reports green on every one of them.
      DeclaredModules: Set<string * string * string> }

module private PinnedApi =

    // -----------------------------------------------------------------------------------------
    // The waiver's bounds. ONE implementation, private to this module, reachable from nowhere else.
    // -----------------------------------------------------------------------------------------

    /// Set job-wide by any job that gates a PUBLISH (`release.yml`). Kills the waiver outright: its
    /// premise is "these packages cannot exist yet — this very commit creates them", which stops being
    /// true at publish time, when they are due. Nothing runs these tests in that lane today; reading the
    /// flag `release.yml` already sets shuts the door the moment someone adds it, rather than depending on
    /// them reading this comment.
    let private releaseLane =
        Environment.GetEnvironmentVariable "FS_GG_VERSION_COHERENCE_RELEASE_LANE" = "1"

    /// The explicit, visible opt-OUT for offline work — never a silent default-off. It lives HERE, on the
    /// doors, for the same reason the waiver does: it was copy-pasted at five call sites, and the sixth
    /// rule would have been the one that forgot it and hard-failed every offline run.
    let private skipRequested =
        match Environment.GetEnvironmentVariable "FS_GG_SKIP_TEMPLATE_PINNED_API" with
        | null | "" -> false
        | _ -> true

    /// Sequential pipe drain is safe HERE and would not be in `runNameofProbe`: a `--unified=0` diff of one
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

    /// Does this line name EXACTLY `version` — as a whole version token, not as a substring of a longer one?
    ///
    /// #711. This used to be `line.Contains uiPin`, and a bare substring test reads "0.9.2" INSIDE "0.9.20"
    /// and inside "0.9.2-preview.1". So with the pin at 0.9.2, an NU1102 naming an FS.GG.UI.* package at
    /// EITHER of those versions satisfied the version bound, and the waiver fired for a version THIS COMMIT
    /// NEVER BUMPED — a stale or typo'd pin, deferred instead of reddened, in exactly the fail-open
    /// direction the axis bound exists to close. It reaches the same defect as a widened id check, just
    /// through the version half instead. And it stops being exotic the moment a patch number passes 9:
    /// 0.9.1 is a substring of 0.9.10 … 0.9.19, and this repo is at 0.9.x today.
    ///
    /// A version token ends where a version character ends, so the boundary rejects `[\w.-]` on both sides:
    /// that takes `(>= 0.9.2)` (the real diagnostic) and refuses `0.9.20` (longer patch) and
    /// `0.9.2-preview.1` (a prerelease of it). It is deliberately NOT a match against NuGet's sentence —
    /// the whole reason `fsGgIdRegex` exists is to keep this predicate off wording that could be localized
    /// or reworded, and a bound that demanded "with version (>= …)" would throw that away.
    let private namesVersion (line: string) (version: string) =
        Regex.IsMatch(line, $@"(?<![\w.\-]){Regex.Escape version}(?![\w.\-])")

    /// Is the failure EXACTLY "the FS.GG.UI.* packages this commit pins are not on the feed yet", and
    /// nothing else? That is the only failure a release window can produce, and the only one waivable.
    ///
    /// Three conditions, and each rules out a failure that is NOT a release window:
    ///
    ///   1. EVERY error is an NU1102 ("the feed does not carry this exact id@version"). An FS0039 from a
    ///      call site the pinned package does not export, an NU1101 typo'd id, an NU1603 upward resolution
    ///      — any of those and the waiver is off. They are the failures these tests exist to catch.
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
    /// It fails CLOSED: if NuGet's diagnostics ever stop matching, the waiver does not fire and these tests
    /// are red in the release window exactly as they were before #543. That is the safe direction to be
    /// wrong in.
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
        // 3. ... and every pin named is the UI axis, at the version THIS commit pins — as a whole version
        //    token, because "0.9.2" is a SUBSTRING of "0.9.20" and of "0.9.2-preview.1" (#711).
        && namedPins
           |> Array.forall (fun (line, ids) ->
               ids |> List.forall (fun id -> id.StartsWith("FS.GG.UI.", StringComparison.Ordinal))
               && namesVersion line uiPin)

    // -----------------------------------------------------------------------------------------
    // The verdict on a FAILED probe — the one place the waiver can be granted.
    // -----------------------------------------------------------------------------------------

    /// What a failed pin probe MEANS. `ReleasePending` is UNFORGEABLE outside this module: it is the only
    /// thing that can turn a red release into an honest IGNORED, and it is constructed in exactly one
    /// place, from exactly the bounds above.
    ///
    /// Not `private` (#711), and the distinction is narrow on purpose: `PinnedApi` is itself `module
    /// private`, so this type is reachable within THIS FILE and nowhere else in the repo. What that buys is
    /// `pinnedApiWaiverBoundsTests` at the foot of the file, which drives the bounds below over every world
    /// and asserts the verdict. What it costs is nothing: a verdict is not a surface — see `classifyIn`.
    type PinFailure =
        /// Not about the pin's AVAILABILITY at all — an FS0039, a feed error, a malformed probe, a timeout.
        /// The rule that asked must judge this ITSELF: this is the failure it exists to catch, and the
        /// waiver has no opinion on it.
        | NotAboutAvailability
        /// The release window. The probe cannot run, by construction, on the commit that publishes the pin.
        | ReleasePending of reason: string
        /// The pin is unavailable and that is a DEFECT, not a window: a stale or typo'd pin nobody bumped,
        /// a package still missing at publish time, or a git that cannot answer the question.
        | Unavailable of reason: string

    /// Asked ONCE. Four rules read the pinned surface, and in the release window ALL FOUR take the failure
    /// path — so a per-reader call would spawn four `git diff` subprocesses to answer one question about one
    /// commit, whose answer cannot differ between them. Same reasoning as the restore lazies below, and the
    /// same thread-safety: Expecto runs the list in parallel, and `Lazy` is thread-safe by default.
    let private uiPinBumped = lazy (bumpedInCommitUnderTest packagesPropsRel uiAxis)

    /// THE BOUNDS, as a total function OF the world rather than a reader OF it (#711).
    ///
    /// `classify` below reads three things off the ambient process — the release-lane env var, the `git
    /// diff` that answers "did THIS commit bump the pin?", and the version the props file pins — and a
    /// function that reads its world cannot be driven through the worlds it is not in. The test process is
    /// always the same world (no release lane, no bump, whatever `main` pins today), so a truth table over
    /// the ambient reader would exercise ONE row and call itself a table. Every conjunct here is
    /// load-bearing in the FAIL-OPEN direction — weaken one and the waiver starts excusing a real defect,
    /// silently, with the gate green — and until #711 no test went red if one was deleted.
    ///
    /// The parameters deliberately SHADOW the module-level `releaseLane` and `uiPinBumped`. That is the
    /// point: inside this function there is no way to name the ambient ones, so it cannot half-read its
    /// world and it cannot be driven into a world that is only partly synthetic.
    ///
    /// THIS IS NOT A WIDENING OF #673's DOOR, and the difference is exactly what makes it safe. What #673
    /// makes unreachable is the PINNED SURFACE — `runNameofProbe`, `readSurfaceAt` and the lazies over them
    /// — so that no rule can hold a restore's answer without having come through the waiver. All of those
    /// are still `private`, and this adds no path to any of them. What is reachable here is the VERDICT
    /// function: it restores nothing, opens no feed, and hands back no surface, so a rule that called it
    /// would still have precisely nothing to assert against. The un-waived door stays shut; only the bounds
    /// became checkable.
    let classifyIn
        (releaseLane: bool)
        (uiPinBumped: Result<bool, string>)
        (uiPin: string)
        (subject: string)
        (output: string)
        : PinFailure =
        if not (failedOnlyOnUnpublishedUiPin output uiPin) then
            NotAboutAvailability
        elif releaseLane then
            Unavailable
                $"RELEASE LANE: $({uiAxis})={uiPin} is not on the feed, and this job gates the PUBLISH — so \
                  those packages are DUE, not pending. RELEASE-PENDING does not apply here; a missing package \
                  at publish time is drift.\n\n{output}"
        else
            match uiPinBumped with
            | Error why -> Unavailable $"{why}\n\n{output}"

            | Ok false ->
                Unavailable
                    $"the feed does not carry the FS.GG.UI.* packages at $({uiAxis})={uiPin}, and this commit \
                      did NOT bump $({uiAxis}) — so this is NOT the release window. The pin is stale or typo'd, \
                      or a release half-failed. Publish it, or re-pin onto a version the feed carries.\n\n{output}"

            | Ok true ->
                ReleasePending
                    $"RELEASE-PENDING: this commit bumps $({uiAxis}) to {uiPin}, and the FS.GG.UI.* packages it \
                      pins are not on nuget.org yet — the merge of THIS commit is what publishes them \
                      (release-tags.yml). {subject} therefore CANNOT run and is DEFERRED to the publish; it is \
                      NOT passing.\n\n\
                      The waiver is bounded to $({uiAxis}) alone, and the SAME restore is the evidence: it \
                      reported no unresolved FS.GG.Audio.* / FS.GG.Game.* pin, and would not have waived one — \
                      those axes publish from other repos, where a bump here publishes nothing. Still asserted \
                      on this commit: every rule in this file that does not need the pinned package.\n\n{output}"

    /// The AMBIENT reading of the bounds: the same function, over the world this process is actually in.
    /// The one place the world is read, so `classifyIn` can stay total.
    let private classify (subject: string) (output: string) : PinFailure =
        classifyIn releaseLane uiPinBumped.Value (readAxis uiAxis) subject output

    /// What a verdict COSTS the rule that asked. Split out of `settle` (#711) because a truth table on the
    /// verdict alone is only half a proof: leave every conjunct above perfect and invert THIS mapping — let
    /// a `ReleasePending` fall through to `()` — and the caller sails on to read a surface that was never
    /// restored, which is the same fail-open by another door. The verdict and its consequence are both
    /// load-bearing, so both are asserted.
    let enact (verdict: PinFailure) : unit =
        match verdict with
        | NotAboutAvailability -> ()
        | ReleasePending why -> skiptest why
        | Unavailable why -> failtest why

    /// Settle a failed probe's AVAILABILITY question, and ONLY that. It RETURNS — rather than throwing —
    /// exactly when the failure is the calling rule's own business to judge.
    ///
    /// This is the whole waiver, and it is the only path to it.
    let private settle (subject: string) (output: string) : unit =
        enact (classify subject output)

    /// The skip message, in one place, so the opt-out reads the same whichever door was knocked on.
    let private skipped (subject: string) : 'a =
        skiptest
            $"FS_GG_SKIP_TEMPLATE_PINNED_API is set — {subject} did NOT run. These proofs are default-on; \
              skipping them leaves the template-vs-pin question unanswered."

    // -----------------------------------------------------------------------------------------
    // The two ways to ask nuget.org about the pin. Both private; both reachable only through a door.
    // -----------------------------------------------------------------------------------------

    /// Restore `packages` at their axis pins from nuget.org ALONE, compile a `Probe.fs` whose body is nothing
    /// but `nameof` lines, and hand back the compiler's verdict. Both compile probes in this file are this
    /// function: #504's (do the template's CALLS resolve against the pin?) and #594/#611's (does a LEDGERED
    /// case really NOT?).
    ///
    /// One body, because the guarantee that makes either probe mean anything is the `<clear />` + probe-local
    /// packages folder below, and a second copy of that setup is a second place for it to rot. A probe that
    /// silently resolved from the machine's global cache would answer a question nobody asked.
    ///
    /// PRIVATE, and it must stay private (#673): this is the UN-WAIVED probe, and a rule that reached it
    /// directly is precisely the bug this module exists to make impossible. Go through `probe`.
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
            let probeSource = StringBuilder()
            probeSource.AppendLine("module Probe").AppendLine() |> ignore

            for ns in namespaces do
                probeSource.AppendLine($"open {ns}") |> ignore

            probeSource.AppendLine().AppendLine("let private probed : string list =").AppendLine("    [") |> ignore

            for line in nameofLines do
                probeSource.AppendLine($"      nameof {line}") |> ignore

            probeSource.AppendLine("    ]") |> ignore

            File.WriteAllText(Path.Combine(workDir, "NuGet.config"), nugetConfig)
            File.WriteAllText(Path.Combine(workDir, "Probe.fsproj"), project)
            File.WriteAllText(Path.Combine(workDir, "Probe.fs"), probeSource.ToString())

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

    /// Restore `packages` at `versionOf`, and read their module + type surface out of the restored assemblies.
    ///
    /// PARAMETERISED BY VERSION, and that is the whole point (#688). Two callers want two different subjects:
    /// the RULES below judge the doc against `$(FsGgUiVersion)` — a moving target, which is correct, because
    /// that is what a scaffolded product restores. The ORACLE SELF-CHECK judges the READER, and for that it
    /// needs a package that CANNOT change under it. Those are different versions, and collapsing them into one
    /// is what reddened `main`.
    ///
    /// PRIVATE (#673). It is the un-waived read: the two lazies below are the only callers, and the two doors
    /// below them are the only way to a result.
    let private readSurfaceAt
        (packages: string list)
        (versionOf: string -> string)
        : Result<
            Map<string * string, Set<string>>
            * Map<string * string, Set<string>>
            * Set<string * string * string>
            * Set<string * string * string>,
            string
          > =
        let workDir = Path.Combine(Path.GetTempPath(), "fsgg-doc-pin-probe-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory workDir |> ignore

        try
            let references =
                packages
                |> List.map (fun id -> $"    <PackageReference Include=\"{id}\" Version=\"{versionOf id}\" />")
                |> String.concat "\n"

            // Same isolation, and for the same reason, as `runNameofProbe`: `<clear />` down to nuget.org so
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
                        let surface =
                            Collections.Generic.Dictionary<string * string, Collections.Generic.HashSet<string>>()
                        let types =
                            Collections.Generic.Dictionary<string * string, Collections.Generic.HashSet<string>>()
                        let declared = Collections.Generic.HashSet<string * string * string>()
                        let declaredModules = Collections.Generic.HashSet<string * string * string>()

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
                                // #683 — keyed by (PACKAGE, module path), for the SAME reason the type map
                                // below is keyed by (package, type), and it took a second bug to say so. This
                                // used to key on the module's BARE NAME and union the members of every
                                // restored package under it — so `resolvesInPin` answered "does ANY pinned
                                // package export a module of this name that has this member?" rather than
                                // "does the package this doc's module actually belongs to export it?". Two
                                // names span packages today: `Audio` (FS.GG.Audio.Core, .Elmish and .Host,
                                // whose member sets are disjoint BY DESIGN — Core plays, Host devices, Elmish
                                // commands) and `Cmd` (`Audio.Cmd` in FS.GG.Audio.Elmish; `Cmd` in
                                // FS.GG.UI.Controls.Elmish, which exports `none` and nothing else). Under the
                                // union, a UI doc naming `Cmd.ofEngine` resolved against the merged key and
                                // PASSED — and its reader gets a hard build error, the #550 class this rule
                                // exists to refuse.
                                let modulesInPackage, moduleMembers = readModuleSurface path

                                for (ns, modulePath) in modulesInPackage do
                                    declaredModules.Add((packageId, ns, modulePath)) |> ignore

                                for (modulePath, memberName) in moduleMembers do
                                    let key = (packageId, modulePath)

                                    if not (surface.ContainsKey key) then
                                        surface.[key] <- Collections.Generic.HashSet<string>()

                                    surface.[key].Add memberName |> ignore

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
                                let typesInPackage, membersInPackage = readTypeSurface path

                                for (ns, typeName) in typesInPackage do
                                    declared.Add((packageId, ns, typeName)) |> ignore

                                for (typeName, memberName) in membersInPackage do
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
                            Ok(modules, typeMap, Set.ofSeq declared, Set.ofSeq declaredModules)
        finally
            try Directory.Delete(workDir, true) with _ -> ()

    /// Restored ONCE per run, not once per test. Four rules below ask for the pinned surface, and a restore
    /// is the expensive, network-bound half of this file — so asking four times would quadruple the gate's
    /// exposure to nuget.org for an answer that cannot have changed between them. `Lazy` is thread-safe by
    /// default, which matters: Expecto runs the list in parallel.
    let private pinnedSurfaceResult = lazy (readSurfaceAt docPackages pinFor)

    /// THE ORACLE'S GROUND TRUTH — a published, IMMUTABLE (id, version), deliberately NOT `$(FsGgUiVersion)`.
    ///
    /// The oracle self-check validates the READER (does it see nullary cases? does it invent `Tag`/`Item`?),
    /// and a reader is validated against facts that cannot move. `0.9.0` is the last release before `#535`
    /// added `ViewerEffect.Persist`, so it pins both halves the self-check needs: `OpenWindow`/`CloseWindow`
    /// present, `Persist` absent.
    ///
    /// It used to read the PIN, which was the same thing only for as long as the pin HAPPENED to be 0.9.0
    /// (#688). The moment 0.9.2 published, the oracle restored a package that legitimately DOES export
    /// `Persist`, the "immutable" assertion failed, and the required `Deterministic gate` went red on `main` —
    /// on a repo where nothing was wrong. Worse, it stayed hidden through the whole 0.9.1 window because the
    /// pin named a version nobody had published, so the restore failed and the test SKIPPED rather than ran.
    ///
    /// Only FS.GG.UI.SkiaViewer, because `ViewerEffect` is all the self-check reads: restoring the other twelve
    /// doc packages a second time would double this gate's nuget.org exposure to answer a question about one
    /// type. If a future case needs another package, add it here — not to `docPackages`, which is the RULES'
    /// subject and must keep tracking the pin.
    let oracleVersion = "0.9.0"

    let private oracleSurfaceResult =
        lazy (readSurfaceAt [ "FS.GG.UI.SkiaViewer" ] (fun _ -> oracleVersion))

    // -----------------------------------------------------------------------------------------
    // The doors. These are the module's ENTIRE public surface, and the only way to reach the pin.
    // -----------------------------------------------------------------------------------------

    /// THE ONLY WAY TO READ THE PINNED SURFACE. There is no un-waived accessor, by construction — the lazy
    /// above is `private` to this module, so a rule that "forgot" the RELEASE-PENDING deferral would not
    /// merely wedge the next release, IT WOULD NOT COMPILE. That is the whole of #673.
    ///
    /// `subject` names the rule, and lands in the deferral message a releaser reads ("…the case/field-vs-pin
    /// rule is DEFERRED to the publish; it is NOT passing"). Say what is not being checked.
    ///
    /// A restore failure that is NOT about the pin's availability is a hard failure here, for EVERY reader.
    /// Two of them used to `skiptest` on it instead — deferring the diagnosis to a sibling rule — and each
    /// was a bespoke error policy one edit away from becoming a fifth copy of the waiver. There is one
    /// policy now, and it lives here.
    let withPinnedSurface (subject: string) (f: PinnedSurface -> unit) : unit =
        if skipRequested then
            skipped subject
        else
            match pinnedSurfaceResult.Value with
            | Ok(modules, types, declared, declaredModules) ->
                f
                    { Modules = modules
                      Types = types
                      DeclaredTypes = declared
                      DeclaredModules = declaredModules }
            | Error why ->
                // Defers in the release window; fails on a stale pin, in the release lane, or on a git that
                // cannot answer. Returns only when the failure is something else entirely —
                settle subject why
                // — and then the surface simply could not be read, which no rule can proceed without.
                failtest why

    /// The ORACLE's surface, at a published and IMMUTABLE version. Deliberately NOT waived, and that is not
    /// an oversight: `oracleVersion` is a released package, so "the merge of this commit is what publishes
    /// it" is a state it can never be in. A failure here is a feed outage, and it is reported IGNORED.
    ///
    /// It is a separate door precisely so that the un-waived read is a NAMED, single, argued exception rather
    /// than an accessor anybody can reach for. `readSurfaceAt` stays private; this is the only other way out.
    let withOracleSurface (subject: string) (f: PinnedSurface -> unit) : unit =
        if skipRequested then
            skipped subject
        else
            match oracleSurfaceResult.Value with
            | Ok(modules, types, declared, declaredModules) ->
                f
                    { Modules = modules
                      Types = types
                      DeclaredTypes = declared
                      DeclaredModules = declaredModules }
            | Error why -> skiptest why

    /// THE ONLY WAY TO COMPILE A `nameof` PROBE AGAINST THE PIN, and the second half of #673's fix.
    ///
    /// A failure that is about the pin's AVAILABILITY never reaches the caller: it is deferred here in the
    /// release window and FAILED here otherwise. So a rule cannot forget the waiver, and — the other half —
    /// cannot MIS-DIAGNOSE an unpublished pin as a missing API, which is what its caller's message would
    /// have said. What comes back is the compiler's verdict on the question the rule actually asked.
    let probe (subject: string) (namespaces: string list) (lines: string list) : int * string =
        if skipRequested then
            skipped subject
        else
            let exitCode, output = runNameofProbe namespaces lines

            if exitCode <> 0 then
                settle subject output

            exitCode, output

/// #504's probe: every entry point the TEMPLATE'S `Program.fs` calls, resolved against the pin.
///
/// It lives BELOW `PinnedApi` because it has to: `PinnedApi.probe` is now the only way to reach the
/// compiler, and F# will not let this call a module declared after it. That ordering is the point rather
/// than an accident — the un-waived `runNameofProbe` is out of scope here, so this cannot be written
/// without the deferral even by someone who has never read a word of #673.
let private runProbeBuild () =
    // The namespaces actually called. `PinnedApi.probe` maps each to the package that carries it.
    let namespaces =
        callSites |> List.choose callNamespace |> List.distinct |> List.sort

    let lines =
        callSites
        |> List.sortBy (fun c -> c.Module, c.Member)
        |> List.map (fun c -> $"{probeSpelling c}.{c.Member}")

    PinnedApi.probe "the pin-grounded proof that the template's call sites compile" namespaces lines

/// Does the PINNED package export what ONE occurrence of a doc symbol names, spelled the way that
/// occurrence spells it?
///
/// The doc says `Cmd.none`; it does NOT say which package it means. The MIRROR does — `Cmd` is
/// declared by FS.GG.UI.Controls.Elmish and (as `Audio.Cmd`) by FS.GG.Audio.Elmish — and the
/// occurrence's own QUALIFIER is what picks between them, exactly as F# resolution would given the
/// `open`s in scope. So the candidates are the ones `admittedCandidates` allows, each is asked of the
/// package IT belongs to, and the symbol resolves if one of them exports the member.
///
/// A candidate whose (package, path) is absent from the pin resolves to `false`, not to "unknown".
/// That is the rule's subject at its sharpest — an ENTIRE module a product cannot reach — and it is
/// now asked PER PACKAGE, which is the half #683 was missing: a UI module the pin does not ship used
/// to be excused by an unrelated Audio module that merely shared its name.
///
/// PASS IF ANY ADMITTED CANDIDATE RESOLVES, and that is a stated rule rather than an accident. For a
/// QUALIFIED spelling the qualifier usually leaves exactly one candidate, so this is a strict
/// per-package judgement. For a BARE `Cmd.x` with two candidates it is not: the rule passes if either
/// package exports it, which is honest about what the oracle can know (a doc-comment or a prose
/// sentence carries no `open`s to resolve against) and still catches the case this rule was built for
/// — that NO candidate exports it. The stricter reading, judging a bare name against the package the
/// surrounding block's `open`s imply, needs a notion of doc context this rule does not have; it is
/// #695's subject (compile the docs, do not parse them), not something to smuggle in here. Anything
/// stricter that does NOT read the `open`s was measured and rejected: admitting only the candidates
/// that are top-level in their namespace would wrongly accuse the 20 shipped sites that write a bare
/// `Perf.runScript`, and a false positive is how this rule gets ledgered into silence by the first
/// person it wrongly accuses.
let private occurrenceResolvesInPin
    (pinned: Map<string * string, Set<string>>)
    (qualifier: string)
    (s: DocSymbol)
    =
    admittedCandidates qualifier s.Module
    |> List.exists (fun m ->
        match pinned |> Map.tryFind (packageForNamespace m.Namespace, m.Path) with
        | Some members -> members.Contains s.Member
        | None -> false)

/// Does the PINNED package export what the doc names — at EVERY spelling the doc names it with?
///
/// One verdict per `docKey`, but a docKey is a set of OCCURRENCES, and they need not agree: a doc may
/// write `Cmd.none` bare in one place and `FS.GG.UI.Controls.Elmish.Authoring.Cmd.ofEngine` in
/// another. Each occurrence is a claim a reader can copy, so each must resolve on its own qualifier —
/// `forall`, not `exists`. Taking the verdict from whichever occurrence `distinctBy` happened to keep
/// would make it depend on file order.
let private resolvesInPin (pinned: Map<string * string, Set<string>>) (s: DocSymbol) =
    docKeyQualifiers
    |> Map.tryFind (docKey s)
    |> Option.defaultValue [ "" ]
    |> List.forall (fun qualifier -> occurrenceResolvesInPin pinned qualifier s)

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
// #752 — THE OTHER DIRECTION, AND THE ONE NOBODY ASKED.
//
// Every rule above judges MIRROR ⊆ PIN: *the mirror declares this; does the pin export it?* Not one judges
// the converse, PIN ⊆ MIRROR: *the pin exports this; does the mirror declare it?* So a mirror could omit a
// type — or a whole file — and every gate here reported green, because an omission declares nothing and a
// rule that only judges declarations has nothing to judge.
//
// It is not hypothetical, and it is not small. At $(FsGgUiVersion)=0.9.2 the mirror omitted 285 of the 673
// public types the pinned packages export — `Keymap`, `KeymapCodec`, `Widget`, `ChordDiagram`,
// `ChartGeometry`, `CapabilityVerdict`, … — and whole SOURCE FILES had never been mirrored at all
// (`src/Controls/Charts2.fsi` shipped in 0.9.2 and appears nowhere in `docs/api-surface/`). Nothing was
// ledgered, nothing was skipped, and CI was green: the mirror was simply half the surface, and the half it
// dropped was invisible BY CONSTRUCTION. That is the fails-open shape (FS-GG/.github#266) this file's own
// header forbids, sitting in the middle of the file that forbids it.
//
// A product author reads the mirror to learn what the framework exposes. An omission does not mislead them
// about a symbol — it hides the symbol, which is worse: there is nothing to reconcile, no error to hit, and
// no reason to look. `scaffold-map.md` tells them "when they disagree, the `.fsi` wins", so a missing type
// reads as a type that does not exist.
//
// THE SCOPE IS THE MIRROR'S OWN CLAIM, AND THAT IS WHAT KEEPS IT HONEST. The mirror is a CURATED contract,
// not a complete dump — `docs/api-surface/` deliberately omits the typed front door
// (`FS.GG.UI.Controls.Typed.*`, feature 085 FR-013), and demanding it would redden CI over a documented
// decision. A rule that falsely accuses is a rule that gets ledgered into silence by the first person it
// wrongly accuses. So the subject is exactly the namespaces the mirror DECLARES: cover a namespace and you
// must cover it COMPLETELY; decline it and it is out of scope. The FR-013 carve-out then falls out of the
// structure — no mirror file declares `FS.GG.UI.Controls.Typed` — rather than being an exception someone
// has to remember.
//
// A RATCHET, like `pinned-api-doc-ledger.txt`, and for its reason: a gate landing on a repo that already
// violates it has two honest options, and "quietly narrow the rule until it is green" is not one of them.
// The 285 omissions are seeded into the ledger, so the hole is A DECISION SOMEBODY MADE — countable, in the
// diff, one line each — rather than an omission nobody noticed. What the ledger buys is that no NEW omission
// can land. What it does not buy is silence: the entries are the debt, and #694's generator is what pays it.
//
// Two anti-rot rules keep it a ratchet rather than a dumping ground:
//   * a ledger entry the mirror NOW declares -> the gap was closed.   Delete the line.  (stale)
//   * a ledger entry the pin NO LONGER exports -> the type was cut.   Delete the line.  (phantom)
// ---------------------------------------------------------------------------------------------

let private omissionLedgerRel = "tests/Build.Tests/mirror-omission-ledger.txt"
let private omissionLedgerPath = repoPath omissionLedgerRel

/// `<kind> <package>::<namespace>.<Name>` — e.g. `type FS.GG.UI.Controls::FS.GG.UI.Controls.Widget`,
/// `module FS.GG.UI.KeyboardInput::FS.GG.UI.KeyboardInput.Keymap`.
///
/// The KIND is part of the key and must be: a module and a type are different things that may share a name
/// (`module Scene` beside `type Scene`), and a kind-less key would let one excuse the other's omission —
/// the same merge the `Modules`/`Types` maps are kept apart to prevent.
///
/// The name is spelled as IL spells it, arity mangle and all (``Foo`1``), because that is what both sides
/// key on; a module's is its PATH within the namespace (`Audio.Cmd`), which is what the mirror keys on.
let private omissionKey (kind: string) (package: string, ns: string, name: string) =
    $"{kind} {package}::{ns}.{name}"

let private omissionLedger =
    if not (File.Exists omissionLedgerPath) then
        Set.empty
    else
        File.ReadAllLines omissionLedgerPath
        |> Array.map (fun l -> l.Trim())
        |> Array.filter (fun l -> l <> "" && not (l.StartsWith("#", StringComparison.Ordinal)))
        |> Set.ofArray

/// The seeded size, and a CEILING. The ledger's header promises it "may only SHRINK"; this is what makes
/// that a rule rather than a hope. Without it the ratchet is aspirational: a worker whose change reddens the
/// completeness rule can go green by appending one line — which is exactly what the header forbids and
/// nothing else would detect. Lower it as the debt is paid.
///
/// RAISING IT IS NOT FORBIDDEN — IT IS EXPENSIVE, AND THAT IS THE POINT. This line used to end "never raise
/// it", which contradicted the rule it documents: the failure message this ceiling produces says, in as many
/// words, "if an entry is genuinely a new DELIBERATE curation, lower nothing and argue it: raise
/// `OmissionLedgerCeiling` in the same commit, with the reason". Both cannot be true, and the absolute
/// reading is the one that fails: a pinned dependency can ADD public surface the scaffold has no business
/// teaching (Audio 0.3.0 did — a whole device/diagnostics lane), and under "never raise" the only ways to a
/// green gate are to mirror a lane the product cannot reach or to narrow the rule. Both are worse than an
/// argued entry, and the second is what the completeness rule explicitly forbids.
///
/// So: a raise is a DECISION, and it must read like one — the ledger carries the argument, this number moves
/// in the same commit, and a reviewer sees both in one diff. What the ratchet actually buys is that the debt
/// cannot grow SILENTLY. Raised 373 -> 382 for the FS.GG.Audio.Host device lane; see the ledger's own entry
/// for why those nine are deliberate (#752). Raised 382 -> 392 for the FS.GG.Game.Core dice / grid-edge /
/// hex / pathfinding-landmark-and-region surface the 0.14.0 pin added — every member is waived in
/// scripts/api-surface-manifest.txt, so these ten module/type omissions reconcile this ledger to that
/// manifest decision; see the ledger's own dated stanza (#941).
[<Literal>]
let private OmissionLedgerCeiling = 392

/// EVERYTHING the pin exports inside the mirror's own claimed scope — types AND modules, keyed alike.
///
/// Modules are here because the first cut of this rule left them out, and leaving them out is not a partial
/// fix but a fails-open one: `readTypeSurface` excludes modules by construction, so a types-only rule passes
/// green over `module Keymap` (twelve rebind functions) and `module KeymapCodec`, both of which the mirror
/// omits at 0.9.2. Half a subject reports green on the other half.
let private pinSurfaceInMirrorScope (surface: PinnedSurface) : Set<string> =
    let inScope (declared: Set<string * string * string>) kind =
        declared
        |> Set.filter (fun (_, ns, _) -> mirrorNamespaces.Contains ns)
        |> Set.map (omissionKey kind)

    Set.union (inScope surface.DeclaredTypes "type") (inScope surface.DeclaredModules "module")

/// EVERYTHING the mirror declares, keyed the same way — so the three rules below compare like with like.
///
/// The package is derived through `packageForNamespace`, the same mapping the pin side is keyed by, rather
/// than matched on a suffix. An `EndsWith "::{ns}.{name}"` test — the first cut — ignores the package half of
/// the key outright, so a mirror declaration in one package would retire a ledger entry written for another.
/// The file's own comments name four type names that span packages (`DiagnosticSeverity`, `Point`, `Rect`,
/// `ViewerMsg`), which is precisely where that would bite.
let private mirrorSurfaceKeys : Set<string> =
    let types =
        mirrorDeclaredTypes
        |> Set.map (fun (ns, name) -> omissionKey "type" (packageForNamespace ns, ns, name))

    let modules =
        frameworkModules
        |> List.map (fun m -> omissionKey "module" (packageForNamespace m.Namespace, m.Namespace, m.Path))
        |> Set.ofList

    Set.union types modules

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
            // The env opt-out and the RELEASE-PENDING deferral are BOTH `PinnedApi`'s, not this rule's
            // (#673). What is left here is the only thing that was ever this rule's own: what a probe
            // failure MEANS once we know it is not the release window.
            let exitCode, output = runProbeBuild ()

            if exitCode <> 0 then
                let uiPin = readAxis uiAxis
                let audioPin = readAxis "FsGgAudioVersion"

                failtest
                    $"the template's framework call sites do NOT compile against the PINNED packages \
                      (FsGgUiVersion={uiPin}, FsGgAudioVersion={audioPin}). \
                      A failure here means the framework has grown public API that a scaffolded product \
                      CANNOT reach — the #429/#492 class. Either the seam is unreleased (cut the release, \
                      then bump the pin) or the template calls API that no longer exists.\n\n{output}"

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

            // #597's fence tracking FAILS OPEN, so the fences have to be proven closed.
            //
            // An UNCLOSED fence — an opener whose closer was dropped, a stray fence in a sentence — leaves the
            // prose reader stuck "inside code" for the WHOLE REST OF THE FILE, and every prose line below it is
            // silently skipped. A skill could then name any unpinned symbol past that point and this rule would
            // report green having read none of it: "nothing to check" and "checked, and it's fine" sharing an
            // exit code, which is the shape (.github#266) this file refuses everywhere else.
            //
            // The anti-vacuity anchor above cannot catch it — it proves ONE symbol in ONE file survives, and
            // says nothing about the other sixteen.
            //
            // #669: this used to count ``` lines and demand the count be EVEN, which is a fourth hand-rolled
            // reading of a fence and a weaker one — it cannot see a ~~~ block at all, and it calls a document
            // balanced when a ```` closes a ``` it never opened. The scanner already answers this exactly, and
            // it is the same scanner the reader above uses, so the property proven here is the property the
            // reader actually has.
            let unclosed =
                Directory.EnumerateFiles(productSkillsRoot, "*.md", SearchOption.AllDirectories)
                |> Seq.filter (fun path -> (MarkdownFences.scan (File.ReadAllText path)).UnclosedFence)
                |> Seq.map (fun path -> Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
                |> List.ofSeq

            let unclosedList = String.Join(", ", unclosed)

            Expect.isEmpty
                unclosed
                $"every shipped product skill closes every fence it opens. An unclosed fence leaves the prose \
                  reader stuck inside a code block for the rest of the file, so every symbol below it goes \
                  UNJUDGED and this rule reports green having read nothing — the fails-open shape (.github#266). \
                  Fix the fence in the skill; do not relax this. Unclosed: {unclosedList}"

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

        // #648 — `Module.Submodule.member`, the spelling the rule was blind to.
        //
        // The extractor read COLUMN-0 `module` declarations only, so `ControlsElmish.Perf.runScript` found no
        // module named `Perf`, `isFrameworkCall` returned false, and the call site was dropped as "the product
        // calling itself" — SILENTLY, with no diagnostic. It failed in the dangerous direction: not a false
        // alarm, a false PASS. A shipped doc could teach an UNRELEASED nested API and merge green, which is
        // precisely the #550 class this rule was built to close.
        //
        // It was found because the gate flagged `ControlsElmish.audioRequests` and said NOTHING about
        // `ControlsElmish.Perf.runScriptToEffects` — named on the line ABOVE it, in the same doc, in the same
        // fenced block, and just as unbindable on the pin.
        //
        // The oracle could always see these: `readModuleSurface` reads `NestedPublic` types, so `Perf` was in
        // the pinned surface the whole time. Only the EXTRACTOR was blind, and a gate whose two halves
        // disagree about what a module IS reports green on the difference. (It keyed them by their SIMPLE
        // NAME then, which was a second bug in the same neighbourhood and is #683 below; the two halves now
        // agree on the PATH.)
        test "a NESTED framework module is judged, so a doc cannot teach an unreleased `Module.Submodule.member` (#648)" {
            let judged = docSymbols |> List.map (fun s -> $"{s.Module}.{s.Member}") |> Set.ofList

            // The anti-vacuity anchor, by NAME rather than by count: the walk must actually reach the nested
            // declarations. `Perf` and `Live` are nested in `ControlsElmish`, `Light`/`Dark` in `DesignTokens`,
            // `Cmd` in `Audio` — five, across three packages.
            for nested in [ "Perf"; "Live"; "Light"; "Dark" ] do
                Expect.isTrue
                    (frameworkModules |> List.exists (fun m -> m.Name = nested && m.Path.Contains "."))
                    $"`{nested}` is a NESTED module of the shipped mirror and must be a framework entry point in \
                      its own right. If it falls out, `parseMirrorFile` has gone back to reading column-0 \
                      declarations only, and every `Module.{nested}.member` a shipped doc names is silently \
                      unjudged again (#648)."

            // THE INSTANCE, and the sharpest anchor in this test: it must be JUDGED, not merely extracted.
            // `fs-gg-elmish` is what teaches a product author to drive their UI headlessly, so `Perf.*` is one
            // of the most-copied spellings in the shipped corpus — and it was the one nothing could see.
            Expect.isTrue
                (judged.Contains "Perf.runScriptToEffects")
                "`ControlsElmish.Perf.runScriptToEffects` — named by the shipped mirror AND by fs-gg-elmish's \
                 fenced block — must survive into the JUDGED set. It is #648's instance: the gate flagged \
                 `ControlsElmish.audioRequests` on the very next line and was silent on this one. If it falls \
                 out, the nested spelling is unjudged again and a doc can teach an unreleased nested API green."

            Expect.isTrue
                (judged.Contains "Perf.runScriptToModel" && judged.Contains "Live.runScript")
                "the nested spellings the product skills actually teach (`Perf.runScriptToModel` in fs-gg-elmish \
                 and fs-gg-testing, `Live.runScript` in the mirror) must be judged. A single instance passing \
                 proves the walk reached one file; these prove it reached the surface."

            // A nested `val` belongs to the module that DECLARES it, not to its parent. The old walk matched
            // `val` at ANY indent while tracking only column-0 modules, so `Perf.runScript` was recorded as
            // `ControlsElmish.runScript` — a member nothing exports. Nothing looked it up, so it never fired;
            // this keeps it from being re-armed.
            let controlsElmish =
                frameworkModules |> List.filter (fun m -> m.Path = "ControlsElmish")

            Expect.isNonEmpty controlsElmish "the mirror declares `module ControlsElmish`."

            for m in controlsElmish do
                Expect.isFalse
                    (m.Members.Contains "runScript")
                    "`runScript` is declared by `ControlsElmish.Perf` and `ControlsElmish.Live`, NOT by \
                     `ControlsElmish` itself. If it appears among the parent's members, the walk is attributing \
                     nested `val`s to their ancestor again and inventing `ControlsElmish.runScript` — a spelling \
                     nothing exports and no doc names."

            // THE RESOLUTION HALF, asserted DIRECTLY — and it has to be, because nothing else in this test can
            // see it. The docs spell a nested call BOTH ways: `ControlsElmish.Perf.runScriptToEffects` in
            // fs-gg-elmish's fences, and a bare `Perf.runScript` in its prose. The mirror's own `val` always
            // arrives UNQUALIFIED (`mirrorValSymbols` reports the innermost owner and no qualifier), so a nested
            // symbol lands in the judged SET through the empty-qualifier path whatever this function does — which
            // means every symbol-level assertion above still passes with the qualified path completely broken.
            //
            // Verified, not assumed: reverting this to the old `m.Namespace = qualified` leaves all 39 tests
            // green. The exact-namespace test can never match a nested module — `Perf`'s namespace is
            // `FS.GG.UI.Controls.Elmish`, and the qualifier a call site offers is `ControlsElmish` — so every
            // QUALIFIED occurrence is dropped, silently, and only the mirror's `val` keeps the symbol alive.
            Expect.isTrue
                (isFrameworkCall "ControlsElmish." "Perf")
                "`ControlsElmish.Perf.runScript` must resolve as a framework call. The qualifier a call site \
                 offers (`ControlsElmish`) is a SUFFIX of what encloses the module \
                 (`FS.GG.UI.Controls.Elmish.ControlsElmish`), never equal to its namespace — so an \
                 exact-namespace test drops every qualified nested spelling in the shipped corpus (#648)."

            Expect.isFalse
                (isFrameworkCall "AppRoot." "Perf")
                "a qualifier that is NOT a suffix of the module's enclosing path is the product calling itself, \
                 and must not resolve. If this passes, the suffix match has been widened into a bare substring \
                 or dropped altogether, and the product's own modules are being judged against the pin."

            // The SITES half, which is what the reader actually pays for. `renderDocSymbol` names every line a
            // symbol occurs on, deliberately ("a message that understates the work by 3x sends the reader back
            // around the loop for nothing"). fs-gg-elmish teaches `Perf.runScriptToEffects` once in PROSE (bare)
            // and twice in FENCES (qualified) — the blocks a reader COPIES. Drop the qualified path and the bare
            // prose mention is the ONLY site that still resolves: the symbol still reports, and the two lines a
            // reader must actually edit vanish from the message.
            //
            // Asserted as a COUNT, not as a line number. `List.exists (fun line -> line > 126)` was the first
            // spelling, and it is the fails-open shape in miniature: 126 is where the prose mention happens to
            // sit today, so one edit to the skill that pushes the prose further down the file satisfies it with
            // the fenced sites gone and the qualified path completely broken. A count cannot be satisfied by the
            // site it is meant to be checking PAST.
            let effectsSites =
                docKeySites
                |> Map.tryFind "template/product-skills/fs-gg-elmish/SKILL.md::Perf.runScriptToEffects"
                |> Option.defaultValue []

            Expect.isGreaterThan
                effectsSites.Length
                1
                $"`ControlsElmish.Perf.runScriptToEffects` is named by fs-gg-elmish in PROSE (bare) and in two \
                  FENCES (qualified), so it must report MORE THAN ONE site (found: {effectsSites}). Only the bare \
                  prose mention resolves through the empty-qualifier path — so a single site means the QUALIFIED \
                  spelling is being dropped again, and the fenced blocks a reader actually copies have gone \
                  unjudged (#648)."

            // The probe has to be able to SPELL what the extractor found. `nameof Perf.runScript` does not
            // compile under `open FS.GG.UI.Controls.Elmish`; only `ControlsElmish.Perf.runScript` does. Nothing
            // in `Program.fs` calls a nested module today, so this is the guard that keeps the probe honest on
            // the day something does — otherwise it would fail on ITSELF and blame the pin.
            Expect.equal
                (probeSpelling { Module = "Perf"; Member = "runScript"; Line = 0 })
                "ControlsElmish.Perf"
                "the probe must spell a nested call site by its path WITHIN the namespace. A bare `Perf.runScript` \
                 cannot bind under `open FS.GG.UI.Controls.Elmish`, so the probe would fail to compile and report \
                 the PIN as missing an API that is present — a wrong diagnosis on a real failure."

            // THE CONTAINER MUST SURVIVE UN-HOISTING, held where it can actually fail. Attributing `val`s to
            // the module that really declares them empties the pure CONTAINERS — `DesignTokens` around
            // `Light`/`Dark`, the Elmish `Audio` around `Cmd` declare no `val` of their own — and the old walk
            // would have dropped them for it. `frameworkModules` deliberately keeps them (see its comment), and
            // this is the assertion that says so: if a member-less module is ever filtered out again, a doc
            // naming `DesignTokens.<member>` stops being framework, goes UNJUDGED, and the rule reports green
            // having read nothing. An unjudged symbol is not a failure, it is a SILENCE — no other test here
            // would go red.
            Expect.isTrue
                (frameworkModules |> List.exists (fun m -> m.Path = "DesignTokens"))
                "`DesignTokens` declares no `val` of its own — its members live in the nested `Light`/`Dark` — \
                 but it is still a framework module a doc can name. If it has fallen out of the mirror's module \
                 set, un-hoisting the nested members has quietly narrowed the closed world."

            // AND THE MULTI-CANDIDATE NAME STILL RESOLVES THROUGH THE MODULE THAT DECLARES THE MEMBER. `Audio`
            // is declared by THREE mirrors — Audio.Core, Audio.Host, and the member-less Elmish container above
            // — and the shipped audio skills name `Audio.interpret` / `Audio.playSfx` ~40 times. `resolveModule`
            // picks the candidate that owns the member, so an EMPTY container sharing the name must never be
            // what answers for them. This is the highest-traffic surface in the corpus, and nothing else pins it.
            Expect.isTrue
                (judged.Contains "Audio.interpret" && judged.Contains "Audio.playSfx")
                "`Audio.interpret` / `Audio.playSfx` are taught throughout the audio skills and declared by the \
                 Audio.Core / Audio.Host mirrors. They must stay judged. If they fall out, the `Audio` name is \
                 resolving through the EMPTY Elmish container instead of the mirrors that declare the members, \
                 and the real audio surface has gone unjudged — silently, with every other test still green."
        }

        // #683 — THE MODULE ORACLE IS KEYED BY (PACKAGE, PATH), NOT BY BARE NAME.
        //
        // `readSurfaceAt` used to key the module surface by the module's SIMPLE NAME and union the members of
        // every restored package under it, so `resolvesInPin` answered "does ANY pinned package export a
        // module of this name that has this member?" — not "does the package this doc's module actually
        // belongs to export it?". #611 had already keyed the TYPE map by (package, type) for precisely this
        // reason, and spelled the reasoning out at length; the module map was the same shape and never got
        // the same treatment.
        //
        // Two names span packages TODAY, so this is not hypothetical. `Cmd` is `FS.GG.Audio.Elmish`'s
        // `Audio.Cmd` (`ofEffects`, `ofEngine`, `playSfx`, …) and `FS.GG.UI.Controls.Elmish`'s top-level `Cmd`
        // (`none`, and nothing else) — DISJOINT member sets, by design. Merged under one key, the Audio
        // package's `ofEngine` EXCUSED a UI doc naming `Cmd.ofEngine`, and its reader gets a hard build
        // error: a false PASS, the #550 class this rule exists to refuse. (`Audio` is the same shape across
        // Core/Elmish/Host.) It never bit for want of luck, not structure — no shipped doc happened to name a
        // colliding member on the wrong side.
        //
        // The surface below is SYNTHETIC, with the shape the real pin has, so the property is stated without
        // paying for a restore — and the anchor above it is what keeps the statement from going vacuous if
        // the framework ever stops colliding on `Cmd`. The REAL oracle's keys are held against the real
        // packages by the doc-vs-pin rule itself, which is the half a hand-written surface cannot prove.
        test "the module oracle is keyed by (package, path), so one package's module cannot excuse another's (#683)" {
            let candidates = admittedCandidates "" "Cmd"

            let packages =
                candidates
                |> List.map (fun m -> packageForNamespace m.Namespace)
                |> List.distinct
                |> List.sort

            // ANTI-VACUITY. Every assertion below is about what happens when ONE NAME spans TWO PACKAGES; if
            // the mirror stops declaring `Cmd` in more than one, they all pass by describing nothing.
            Expect.isGreaterThan
                packages.Length
                1
                $"`Cmd` must be declared by MORE THAN ONE pinned package for this test to be testing anything \
                  (found: {packages}). It is `Audio.Cmd` in FS.GG.Audio.Elmish and a top-level `Cmd` in \
                  FS.GG.UI.Controls.Elmish. If the collision is gone, re-point this test at whatever name \
                  spans packages now — do NOT delete it: the unsoundness it guards is in the KEY, not in the \
                  particular name that exposed it."

            // The two `Cmd`s, keyed the way the real oracle keys them, with the members the real packages
            // export. Under the old bare-name key these two rows were ONE, and its member set was the union.
            let surface =
                Map.ofList
                    [ ("FS.GG.UI.Controls.Elmish", "Cmd"), Set.ofList [ "none" ]
                      ("FS.GG.Audio.Elmish", "Audio.Cmd"),
                      Set.ofList
                          [ "ofEffects"; "ofEngine"; "playMusic"; "playSfx"; "setMasterVolume"; "stopMusic" ] ]

            let cmd memberName =
                { Doc = "synthetic"; Line = 0; Module = "Cmd"; Member = memberName }

            // THE INSTANCE. A doc that reaches `Cmd` through the UI package's own namespace and names
            // `ofEngine` is naming a member of the AUDIO package's unrelated `Cmd`. The UI `Cmd` exports
            // `none` and nothing else, so this must be REFUSED — and under the bare-name key it was not.
            Expect.isFalse
                (occurrenceResolvesInPin surface "FS.GG.UI.Controls.Elmish.Authoring" (cmd "ofEngine"))
                "`Cmd.ofEngine`, qualified into FS.GG.UI.Controls.Elmish, must NOT resolve: that package's \
                 `Cmd` exports `none` alone, and `ofEngine` belongs to FS.GG.Audio.Elmish's unrelated \
                 `Audio.Cmd`. If it resolves, the module surface is keyed on the BARE NAME again and one \
                 package's module is excusing another's — a false PASS on a doc whose reader gets a hard \
                 build error (#683)."

            // The other half, and it is what keeps the fix from being "refuse everything": the SAME qualifier
            // must still resolve the member that package really does export.
            Expect.isTrue
                (occurrenceResolvesInPin surface "FS.GG.UI.Controls.Elmish.Authoring" (cmd "none"))
                "`Cmd.none` IS exported by FS.GG.UI.Controls.Elmish, and must resolve. If this fails, the \
                 (package, path) key does not agree with the mirror's `Path` and the oracle now accuses \
                 correct docs — a false POSITIVE, which is how this rule gets ledgered into silence by the \
                 first person it wrongly accuses."

            // ...and the qualifier is what picks the package. Reached through `module Audio`, the very same
            // `Cmd.ofEngine` is correct.
            Expect.isTrue
                (occurrenceResolvesInPin surface "Audio" (cmd "ofEngine"))
                "`Audio.Cmd.ofEngine` must resolve: the qualifier `Audio` admits FS.GG.Audio.Elmish's nested \
                 `Audio.Cmd`, which exports it. If this fails, the qualifier is no longer selecting the \
                 candidate — and a rule that cannot tell the two `Cmd`s apart can only be wrong in one \
                 direction or the other."

            // AND A MEMBER NO CANDIDATE EXPORTS IS STILL A VIOLATION, from either side. This is the rule's
            // subject at its sharpest, and the one thing the ANY-of-candidates reading must never lose.
            for qualifier in [ ""; "Audio"; "FS.GG.UI.Controls.Elmish.Authoring" ] do
                Expect.isFalse
                    (occurrenceResolvesInPin surface qualifier (cmd "ofNothing"))
                    $"`Cmd.ofNothing` (qualifier: '{qualifier}') is exported by NO candidate, so it must be a \
                      violation however it is spelled. If it resolves, the oracle has a hole in it, and a \
                      hole EXCUSES every symbol that belongs in it (.github#266)."
        }

        // The walk itself, against a mirror written FOR the test — the only way to assert the half of it the
        // shipped corpus cannot reach.
        //
        // `internal` IS handled by `parseMirrorFile`, and today's mirror exercises NONE of it: the api-surface
        // mirror ships zero `module internal` / `val internal` declarations (src has 55; mirroring strips them).
        // So an assertion phrased against the real corpus — "`Coalescing` is not a framework module" — is
        // vacuously true, stays green if the handling is deleted outright, and is exactly the "green because it
        // checked nothing" shape (.github#266) this file refuses everywhere else. It was written that way first,
        // and a mutation run is what exposed it: removing the `internal` tracking left all 39 tests passing.
        //
        // The handling still has to be there and still has to be RIGHT, because the mirror is REGENERATED from
        // src, and one regen that carries an internal module through is all it takes. An internal module must be
        // TRACKED, not skipped: refuse to push it and its `val`s fall through to the nearest public ancestor,
        // inventing `ControlsElmish.isCoalescibleSample` — a member no package exports, a phantom violation
        // against a correct mirror, with no honest remedy. Track it; mark it; judge nothing under it.
        //
        // A synthetic mirror asserts the contract directly, so the walk is pinned by something that fails when
        // it breaks rather than by the corpus's current good luck.
        test "the mirror walk: a nested module owns its own `val`s, and `internal` is tracked but never judged (#648)" {
            let dir = Path.Combine(Path.GetTempPath(), "fsgg-mirror-walk-" + Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory dir |> ignore

            try
                let source =
                    "namespace FS.GG.Test.Pkg\n\
                     \n\
                     module Outer =\n\
                     \n\
                     \x20   val topLevel: int -> int\n\
                     \n\
                     \x20   module Inner =\n\
                     \x20       val nested: int -> int\n\
                     \n\
                     \x20   module internal Hidden =\n\
                     \x20       val secret: int -> int\n\
                     \n\
                     \x20   val afterNested: int -> int\n\
                     \x20   val internal notSurface: int -> int\n"

                let path = Path.Combine(dir, "Probe.fsi")
                File.WriteAllText(path, source)

                let parsed = parseMirrorFile path
                let byPath = parsed |> List.map (fun m -> m.Path, m) |> Map.ofList

                // `Hidden` is internal: tracked (so `secret` does not fall through to `Outer`) and registered
                // NOWHERE. If it appears here, the closed world now contains a module no product can bind.
                Expect.equal
                    (parsed |> List.map (fun m -> m.Path) |> List.sort)
                    [ "Outer"; "Outer.Inner" ]
                    "the walk registers every PUBLIC module at every depth, and no internal one. `Outer.Hidden` \
                     is `module internal` — a product cannot reach it, so it must never become a framework entry \
                     point (the `RetainedRender` unsoundness, one level down)."

                let outer = byPath.["Outer"]

                // The indent stack, asserted end to end: `topLevel` precedes the nested blocks and `afterNested`
                // FOLLOWS them, so it is only attributed correctly if the nested modules were CLOSED at dedent.
                // `nested` and `secret` must not appear, and neither may `notSurface` (`val internal`).
                Expect.equal
                    (outer.Members |> Set.toList |> List.sort)
                    [ "afterNested"; "topLevel" ]
                    "a module owns the `val`s it declares ITSELF. `nested` belongs to `Outer.Inner` and `secret` \
                     to the internal `Outer.Hidden`; attributing either to the parent invents `Outer.nested` — the \
                     `ControlsElmish.runScript` phantom. `afterNested` is declared AFTER the nested blocks, so it \
                     lands on `Outer` only if they were closed at the dedent. And `val internal notSurface` is not \
                     product surface at all."

                let inner = byPath.["Outer.Inner"]

                Expect.equal (inner.Name, inner.Members |> Set.toList) ("Inner", [ "nested" ])
                    "a nested module is keyed by its INNERMOST name — that is what a call site spells \
                     (`Outer.Inner.nested`) and what the pinned-package oracle keys on (`Outer+Inner`)."

                Expect.equal (enclosingPath inner) "FS.GG.Test.Pkg.Outer"
                    "what ENCLOSES a nested module is its namespace PLUS its parents, which is why a qualifier \
                     (`Outer`) is a suffix of it and never equal to the namespace. This is the comparison \
                     `isFrameworkCall` makes, and the one #648's exact-namespace test could never satisfy."
            finally
                try Directory.Delete(dir, true) with _ -> ()
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
            PinnedApi.withPinnedSurface "the doc-vs-pin rule" <| fun surface ->
                let uiPin = readAxis uiAxis

                // #683 — THE ORACLE ITSELF, held against the REAL packages before it is asked anything.
                // The synthetic test above proves the RESOLVER reads a (package, path) key correctly; only
                // this proves `readModuleSurface` actually EMITS one. Re-key it by the bare name and the two
                // rows below merge into one whose members are the union — which is #683 exactly, and the
                // resolver test would not notice, because its surface is hand-written.
                //
                // Held as DISJOINT MEMBER SETS rather than as two keys: two keys can exist and still carry
                // the merged members, and it is the merge that excuses the doc.
                for (package, path, mustHave, mustNotHave) in
                    [ "FS.GG.UI.Controls.Elmish", "Cmd", "none", "ofEngine"
                      "FS.GG.Audio.Elmish", "Audio.Cmd", "ofEngine", "none" ] do
                    match surface.Modules |> Map.tryFind (package, path) with
                    | None ->
                        failtest
                            $"the pinned surface has no module `{path}` in {package}. The (package, path) \
                              key no longer agrees with the mirror's `Path`, so every doc symbol on that \
                              module now reads as unexported and the rule is about to accuse correct docs \
                              (#683)."
                    | Some members ->
                        Expect.isTrue
                            (members.Contains mustHave)
                            $"{package}'s `{path}` must export `{mustHave}` at the pin — it is what the \
                              module is FOR. If it does not, the oracle is reading the wrong assembly."

                        Expect.isFalse
                            (members.Contains mustNotHave)
                            $"{package}'s `{path}` must NOT export `{mustNotHave}` — that member belongs to \
                              the OTHER package's same-named `Cmd`. If it appears here, the module surface \
                              has been keyed on the bare name again and the two packages' members are \
                              unioned, so one package's `Cmd` will excuse a doc naming the other's — a false \
                              PASS on a doc whose reader gets a hard build error (#683)."

                let undeclared =
                    docSymbols
                    |> List.filter (fun s -> not (resolvesInPin surface.Modules s))
                    |> List.filter (fun s -> not (docLedger.Contains(docKey s)))
                    |> List.map renderDocSymbol

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
        // RELEASE-PENDING — THIS RULE IS WHY `PinnedApi` EXISTS (#673).
        //
        // It shipped (#611) WITHOUT the deferral its val-level siblings already carried, because nothing
        // forced it to have one, and the omission is invisible until the one commit it breaks: a RELEASE. On
        // that commit `$(FsGgUiVersion)` names a version nuget.org does not carry yet — the merge is what
        // publishes it — so the pinned surface cannot be restored, and a bare `failtest` hard-fails NU1102 on
        // the required `Deterministic gate` with `enforce_admins` ON and `--admin` forbidden. The commit whose
        // whole job is to be merged CANNOT BE MERGED. 0.9.1 (#587) was the first release cut since #611
        // landed, and it is how this was found; #642 and #651 hit it too.
        //
        // There is no waiver written here any more, and THAT IS THE FIX. The bounds live once, behind
        // `withPinnedSurface`, which is now the only way to obtain a `PinnedSurface` at all — so the next rule
        // to ask this question cannot ship without them either. See `PinnedApi`'s header.
        testCase "every union case and record field a SHIPPED mirror declares exists in the PINNED package" <| fun _ ->
            PinnedApi.withPinnedSurface "the case/field-vs-pin rule" <| fun surface ->
                let uiPin = readAxis uiAxis

                // The oracle must actually KNOW about types, or this rule excuses everything while
                // reporting green — the fails-open shape (#266) this file refuses.
                Expect.isNonEmpty
                    (Map.toList surface.Types)
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
                        match Map.tryFind (packageForNamespace m.Namespace, m.Type) surface.Types with
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

                Expect.isEmpty
                    undeclared
                    $"these union cases / record fields are DECLARED by a shipped api-surface mirror and \
                      are exported by NO package a scaffolded product restores at $({uiAxis})={uiPin}. A \
                      product author reading the mirror is told about a case they cannot construct — the \
                      #550 class, one type-system level down, and invisible to every other gate here \
                      because they all judge `Module.member` and a case is neither.\n\n\
                      Fix the MIRROR to declare what the released package exports, or — if the member is \
                      genuinely unreleased and the mirror must carry it anyway (M-MIR/TYPE compels a \
                      mirrored type to match src member-for-member, so this WILL happen) — declare it in \
                      {docLedgerRel}, whose anti-rot rules retire the entry the moment the release \
                      lands.\n\n\
                      Undeclared: {rendered}"

        // #752 — the converse of the rule above, and the direction that was never asked.
        testCase "every TYPE and MODULE the PINNED package exports in a mirrored namespace is declared by the mirror" <| fun _ ->
            PinnedApi.withPinnedSurface "the mirror-completeness rule" <| fun surface ->
                let uiPin = readAxis uiAxis

                // The oracle must actually SEE both kinds, or the rule finds no omissions of the kind it is
                // blind to and reports green while judging half its subject — the fails-open shape (#266) it
                // exists to close, and the bug the first cut of this very rule shipped with.
                Expect.isNonEmpty
                    (Set.toList surface.DeclaredTypes)
                    "the pinned packages exported ZERO public types — the TYPE reader has stopped seeing the \
                     surface. That is a defect in this test, not an empty framework."

                Expect.isNonEmpty
                    (Set.toList surface.DeclaredModules)
                    "the pinned packages exported ZERO public modules — the MODULE reader has stopped seeing \
                     the surface, and every omitted module would pass unjudged."

                Expect.isNonEmpty
                    (Set.toList mirrorNamespaces)
                    "the shipped mirror declares NO namespace — the extractor has stopped reading them, and \
                     the rule below would have an empty subject and pass over everything."

                let inScope = pinSurfaceInMirrorScope surface

                Expect.isNonEmpty
                    (Set.toList inScope)
                    "nothing the pin exports falls in any namespace the mirror declares. Either the mirror's \
                     namespaces and the pin's have stopped agreeing (a spelling change on one side), or the \
                     reader is blind — either way this rule is judging nothing."

                let omitted =
                    Set.difference (Set.difference inScope mirrorSurfaceKeys) omissionLedger
                    |> Set.toList

                let rendered = String.concat "\n  " omitted

                Expect.isEmpty
                    omitted
                    $"these types / modules are exported by a package a scaffolded product restores at \
                      $({uiAxis})={uiPin}, in a namespace the mirror DECLARES — and the mirror does not \
                      declare them. A product author reading `docs/api-surface/` is not misled about them; \
                      they are told these do not exist, which is worse, because there is nothing to \
                      reconcile and no error to hit (`scaffold-map.md`: \"when they disagree, the `.fsi` \
                      wins\").\n\n\
                      Add them to the MIRROR — something whose source file has no mirror file at all needs \
                      the file creating — or, if the omission is DELIBERATE (the mirror is a curated \
                      contract, and the typed front door is deliberately absent per feature 085 FR-013), \
                      declare it in {omissionLedgerRel} with the reason. Do NOT narrow this rule to make it \
                      green.\n\n\
                      Omitted:\n  {rendered}"

        // #752 — ANTI-ROT 1 (stale). A ledger entry the mirror NOW declares has been paid off, and a ledger
        // that outlives its subjects stops being a ratchet: the entry sits there excusing something that no
        // longer needs excusing, so the NEXT omission of it — a real regression — walks straight back through.
        testCase "no mirror-omission ledger entry names something the mirror NOW declares" <| fun _ ->
            let paid = Set.intersect omissionLedger mirrorSurfaceKeys |> Set.toList
            let renderedPaid = String.concat "; " paid

            Expect.isEmpty
                paid
                $"these {omissionLedgerRel} entries name something the shipped mirror NOW declares — the gap \
                  was closed and the entry is dead. A dead entry is not harmless: it goes on excusing its \
                  subject forever, so if that subject is ever dropped from the mirror again the omission rule \
                  waves the regression straight through. Delete the line(s): {renderedPaid}"

        // #752 — ANTI-ROT 2 (phantom). An entry for something the pin no longer exports is excusing nothing,
        // and it is load-bearing that it goes: it is the only thing between the ledger and a dumping ground
        // nobody ever has to empty.
        testCase "no mirror-omission ledger entry names something the PIN no longer exports" <| fun _ ->
            PinnedApi.withPinnedSurface "the mirror-omission ledger's anti-rot rule" <| fun surface ->
                let uiPin = readAxis uiAxis
                let live = pinSurfaceInMirrorScope surface
                let phantom = Set.difference omissionLedger live |> Set.toList
                let renderedPhantom = String.concat "; " phantom

                Expect.isEmpty
                    phantom
                    $"these {omissionLedgerRel} entries name something that NO package a scaffolded product \
                      restores at $({uiAxis})={uiPin} exports in a mirrored namespace — it was cut, renamed, \
                      or moved out of the mirror's scope, and the entry now excuses nothing. Delete the \
                      line(s): {renderedPhantom}"

        // #752 — ANTI-ROT 3 (the ratchet itself). The ledger's header promises it may only SHRINK. Without
        // this the promise is prose: a worker whose change reddens the completeness rule could go green by
        // appending a line, which is precisely what the header forbids and what nothing else here detects.
        testCase "the mirror-omission ledger only ever SHRINKS" <| fun _ ->
            Expect.isLessThanOrEqual
                (Set.count omissionLedger)
                OmissionLedgerCeiling
                $"{omissionLedgerRel} has grown. It is a RATCHET: it records the debt that existed when the \
                  completeness rule landed, and it may only shrink as the mirror is fixed. If you added an \
                  entry to make a red gate green, that is the one thing it is not for — mirror the symbol \
                  instead. If an entry is genuinely a new DELIBERATE curation, lower nothing and argue it: \
                  raise `OmissionLedgerCeiling` in the same commit, with the reason, so the growth is a \
                  decision somebody made rather than a line nobody noticed."

        // #611 — THE ORACLE, ANCHORED. The rules above are only as good as `readTypeSurface`, and every way
        // it can be wrong is SILENT:
        //
        //   * blinded (it stops seeing cases) -> the case rule finds nothing and reports GREEN;
        //   * widened (it invents members)    -> a real violation is excused and it reports GREEN.
        //
        // Both were live bugs in this file's first cut, and neither made a test red. So the oracle is
        // pinned to facts about a package that is PUBLISHED AND IMMUTABLE — FS.GG.UI.SkiaViewer 0.9.0 —
        // and a published (id, version) cannot change under us. If these stop holding, the reader broke.
        //
        // It goes through `withOracleSurface`, NOT `withPinnedSurface`, and the difference is the whole of
        // #688: this rule's subject is a version that CANNOT MOVE. It is also the one read of a package that
        // is deliberately NOT release-pending-waived — a published version can never be "about to be
        // published by this merge" — which is why it needs, and gets, its own named door rather than an
        // accessor anyone could reach for.
        testCase "the pinned-TYPE oracle reads a published DU the way F# actually emits it" <| fun _ ->
            PinnedApi.withOracleSurface "the oracle anchor" <| fun oracle ->
                match Map.tryFind ("FS.GG.UI.SkiaViewer", "ViewerEffect") oracle.Types with
                | None ->
                    failtest
                        $"the oracle sees no `ViewerEffect` in FS.GG.UI.SkiaViewer {PinnedApi.oracleVersion} at \
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
                        $"published FS.GG.UI.SkiaViewer {PinnedApi.oracleVersion} now exports \
                         `ViewerEffect.Persist` — which it cannot, since a published version is immutable. The \
                         oracle is reading something other than {PinnedApi.oracleVersion} (a locally-packed \
                         package leaking into the probe folder is the classic cause), and the ledger entry it \
                         justifies is void."

        // Anti-rot 1 (stale). The release landed and the symbol is reachable now; the excuse has outlived
        // its reason. This is the rule that retires a ledger entry at exactly the right moment.
        testCase "no doc-ledger entry names a symbol the PINNED package now exports" <| fun _ ->
            if Set.isEmpty docLedger then
                skiptest "the doc ledger is empty — there is no entry that could have gone stale."
            else
                PinnedApi.withPinnedSurface "the ledger's staleness check" <| fun surface ->
                    let staleVals =
                        docSymbols
                        |> List.filter (fun s -> docLedger.Contains(docKey s) && resolvesInPin surface.Modules s)
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
                            && (match Map.tryFind (packageForNamespace m.Namespace, m.Type) surface.Types with
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

// ---------------------------------------------------------------------------------------------
// #711 — THE WAIVER'S BOUNDS, DRIVEN.
//
// #673 collapsed the RELEASE-PENDING waiver from four copy-pasted copies into ONE, behind `PinnedApi`,
// whose accessors are the only way to reach the pin — so a rule that FORGETS the waiver does not compile.
// That fixed the omission class outright, and it left the harder one untouched: nothing tested the BOUNDS.
// `PinnedApi.classifyIn` is where all four are decided, which makes it simultaneously the best-protected
// code in this file and the highest-consequence, and no test in this repo went red if a conjunct were
// deleted from it. Each one is load-bearing in the FAIL-OPEN direction — weaken any of them and the waiver
// starts excusing a real defect, silently, on the REQUIRED `Deterministic gate`:
//
//   * only an NU1102 (never an FS0039, an NU1101, an NU1603) — the others are the defects these tests exist
//     to catch, and a waiver that swallowed them would retire the whole file;
//   * only on the $(FsGgUiVersion) axis — an unpublished FS.GG.Audio.* / FS.GG.Game.* pin is a real defect
//     even on the commit that bumped it, because those publish from OTHER repos, where a bump here
//     publishes nothing (that is #235, the defect the sibling script was written for);
//   * only when THIS commit bumped it — a pin nobody bumped that the feed lacks is stale or typo'd, and
//     stays red;
//   * never in the release lane — there the packages are DUE, not pending.
//
// THE ASYMMETRY THIS CLOSES. The SIBLING waiver — `releasePending` in
// `scripts/validate-template-payload-pins.fsx` — has had an eight-world truth table since #544
// (`tests/Package.Tests/TemplatePayloadPinsWaiverTests.fs`). It guards an ADVISORY job. This one guards the
// REQUIRED gate, and had nothing. The better-protected waiver was on the weaker gate.
//
// AND THIS TESTS THE REAL PREDICATE, WHERE THE SIBLING COULD ONLY MIRROR ONE. #544 had to re-implement its
// script's decision layer in the test — the guard is a `dotnet fsi` entry point that ends in `exit`, so it
// cannot be `#load`ed — and then pin the copy to the original with `source lockstep` string assertions,
// because a mirror that drifts from its subject passes while the subject rots. `PinnedApi` is ordinary F#
// in a compiled project, so there is no copy here and there is nothing to keep in lockstep: these tests
// call the function the gate calls. That is strictly the stronger of the two arrangements, and it is why
// this file grows a truth table instead of a mirror.
//
// NO NETWORK, NO GIT, NO ENV. `classifyIn` takes the world as an argument (that is the #711 seam), so the
// eight worlds are eight tuples and the restore outputs are string fixtures. The suite is offline and runs
// on every gate — including, and this is the point, inside the release window it exists to bound.
// ---------------------------------------------------------------------------------------------

/// A synthetic pin, NOT `readAxis uiAxis`. These tests drive worlds the repo is not in, and reading the
/// real pin would make the table's rows depend on whatever `main` happens to pin this week.
let private testPin = "0.9.2"

/// MSBuild prefixes every diagnostic with the project that raised it. The probe's work dir is under
/// `Path.GetTempPath()` and is spelled `fsgg-…`, not `FS.GG.…`, so the path itself names no package —
/// which matters, because `fsGgIdRegex` reads ids off the LINE, and a path that looked like an id would
/// veto the waiver from inside the noise.
let private probeProj = "/tmp/fsgg-pinned-api-probe-4f2a/Probe.fsproj"

/// One NU1102, AS NUGET REALLY EMITS IT: a header naming the id and version, then two CONTINUATION lines
/// that repeat the code and name no package at all.
///
/// The continuations are not decoration. They are the exact shape that broke the first cut of the axis
/// bound — a detail line, naming no pin, failing the "every named pin is FS.GG.UI.*" test and vetoing a
/// waiver that should have fired — which is why the bound is asked only of the lines that NAME a package,
/// and why every fixture here that carries an NU1102 carries its continuations too. A fixture that emitted
/// the header alone would pass without ever touching the reason the code is written the way it is.
let private nu1102 (packageId: string) (version: string) =
    [ $"{probeProj} : error NU1102: Unable to find package {packageId} with version (>= {version})"
      $"{probeProj} : error NU1102:   - Found 12 version(s) in nuget.org [ Nearest version: 0.9.1 ]"
      $"{probeProj} : error NU1102:   - Versions from /usr/lib/dotnet/library-packs were not considered" ]

/// The framework grew API the template cannot consume — #504's entire subject, and the failure the waiver
/// must NEVER swallow.
let private fs0039 (symbol: string) =
    [ $"/tmp/fsgg-pinned-api-probe-4f2a/Probe.fs(7,13): error FS0039: The value, constructor, namespace or \
       field '{symbol}' is not defined." ]

let private restoreOutput (lines: string list) = String.concat "\n" lines

/// The one world that may defer: the UI pin is unresolved, this commit bumped it, and we are not gating a
/// publish.
let private releaseWindowOutput = restoreOutput (nu1102 "FS.GG.UI.Scene" testPin)

let private subject = "the pin-grounded proof"

let private classify releaseLane bumped output =
    PinnedApi.classifyIn releaseLane bumped testPin subject output

/// Every reason a verdict carries must ECHO the restore that produced it. Asserted instead of the prose,
/// which is a releaser-facing message that may legitimately be reworded — the EVIDENCE may not go missing.
let private reasonOf =
    function
    | PinnedApi.ReleasePending why
    | PinnedApi.Unavailable why -> why
    | PinnedApi.NotAboutAvailability -> ""

[<Tests>]
let pinnedApiWaiverBoundsTests =
    testList
        "issue-711 PinnedApi RELEASE-PENDING waiver bounds"
        [
          // ---- the headline, and the shape #544 froze for the sibling ----------------------------
          //
          // A conjunction is exactly the thing a well-meaning edit loosens by one term, so assert the
          // whole space rather than the rows someone thought to write down.

          test "the waiver opens in exactly one of the eight possible worlds" {
            let worlds =
                [ for unresolvedUiPin in [ true; false ] do
                      for releaseLane in [ true; false ] do
                          for bumpedHere in [ true; false ] do
                              yield unresolvedUiPin, releaseLane, bumpedHere ]

            let defers (unresolvedUiPin, releaseLane, bumpedHere) =
                let output =
                    if unresolvedUiPin then
                        releaseWindowOutput
                    else
                        restoreOutput (fs0039 "Viewer.runAppWithAudio")

                match classify releaseLane (Ok bumpedHere) output with
                | PinnedApi.ReleasePending _ -> true
                | _ -> false

            let opened = worlds |> List.filter defers

            Expect.equal
                opened
                [ true, false, true ]
                "exactly ONE of the eight worlds may defer: the UI pin is unresolved, we are NOT gating the \
                 publish, and THIS commit bumped it. Every other world names a defect that must stay red. If \
                 this is now failing, a conjunct in `PinnedApi.classifyIn` was weakened — re-derive the \
                 bounds from its header before you touch this table."
          }

          // ---- conjunct 1: only an NU1102. The rest are what this file exists to catch -----------

          test "an FS0039 is NEVER waived — it is the defect the probe exists to find" {
            // #429's audio seam: the framework grew `Viewer.runAppWithAudio`, the template pinned a version
            // that did not carry it, and nothing went red for the life of 0.8.0. A waiver that swallowed an
            // FS0039 would retire this entire file, and would do it in the release window — when the pin is
            // MOST likely to be the thing that is wrong.
            let verdict =
                classify false (Ok true) (restoreOutput (fs0039 "Viewer.runAppWithAudio"))

            Expect.equal
                verdict
                PinnedApi.NotAboutAvailability
                "an unresolved SYMBOL is not an unresolved PACKAGE. The waiver has no opinion on it, and the \
                 rule that asked must judge it itself (that is what `NotAboutAvailability` buys)."
          }

          test "an FS0039 is not waived even in the release lane — the availability question is asked first" {
            // Order is load-bearing: `classifyIn` asks "is this even ABOUT availability?" BEFORE it asks
            // about the lane. Swap the two and a genuine API break in the release lane is reported as
            // "the packages are DUE" — a true statement, and the wrong diagnosis, on the commit that ships.
            let verdict = classify true (Ok true) (restoreOutput (fs0039 "Viewer.runAppWithPersistence"))

            Expect.equal
                verdict
                PinnedApi.NotAboutAvailability
                "the lane decides what an UNAVAILABLE pin means; it does not turn a compile error into one"
          }

          test "an NU1101 (typo'd id) and an NU1603 (upward resolution) are never waived" {
            for code, line in
                [ "NU1101", $"{probeProj} : error NU1101: Unable to find package FS.GG.UI.Scne. No packages \
                              exist with this id."
                  "NU1603", $"{probeProj} : error NU1603: FS.GG.UI.Scene depends on FS.GG.UI.Core (>= \
                              {testPin}) but FS.GG.UI.Core {testPin} was not found. FS.GG.UI.Core 0.9.3 was \
                              resolved instead." ] do
                Expect.equal
                    (classify false (Ok true) line)
                    PinnedApi.NotAboutAvailability
                    $"only an NU1102 means 'the feed does not carry this exact id@version'. An {code} is a \
                      different defect and the waiver must not reach it — it fails CLOSED, and a release \
                      wedged by a real {code} is the correct outcome."
          }

          test "an NU1102 mixed with an FS0039 is not waived — EVERY error must be the pin" {
            // The dangerous shape: a genuine release window that ALSO broke an API. Waive on "some error is
            // an NU1102" instead of "every error is" and the API break ships inside the window.
            let output = restoreOutput (nu1102 "FS.GG.UI.Scene" testPin @ fs0039 "Viewer.runAppWithAudio")

            Expect.equal
                (classify false (Ok true) output)
                PinnedApi.NotAboutAvailability
                "the window excuses an absent PACKAGE, never an absent SYMBOL. One FS0039 anywhere in the \
                 output and the whole restore is the rule's own business again."
          }

          // ---- conjunct 2: the UI axis alone. This is the fail-open the sibling calls its worst ----

          test "an unpublished FS.GG.Audio.* / FS.GG.Game.* pin is NEVER waived, window or not" {
            // Bumping $(FsGgAudioVersion) HERE publishes nothing — that package ships from its own repo — so
            // an absent Audio/Game pin is a real defect on every commit, including the one that bumped it.
            // A waiver keyed on "an axis was bumped and the feed lacks it" would sail straight past it: #235,
            // the exact defect the sibling script was written to catch, in a new coat.
            for packageId in [ "FS.GG.Audio.Core"; "FS.GG.Game.Core" ] do
                let alone = restoreOutput (nu1102 packageId "0.4.0")

                Expect.equal
                    (classify false (Ok true) alone)
                    PinnedApi.NotAboutAvailability
                    $"{packageId} publishes from ANOTHER repo. A bump here publishes nothing, so an \
                      unpublished pin is drift on every commit — there is no window for it to be pending in."

                // ...and it survives a genuine UI release window happening around it, which is the case a
                // careless axis bound really would let through.
                let alongsideAGenuineWindow =
                    restoreOutput (nu1102 "FS.GG.UI.Scene" testPin @ nu1102 packageId "0.4.0")

                Expect.equal
                    (classify false (Ok true) alongsideAGenuineWindow)
                    PinnedApi.NotAboutAvailability
                    $"the UI release window is genuinely open, and it still may not excuse {packageId}. The \
                      waiver is bounded to $(FsGgUiVersion) ALONE."
          }

          test "an FS.GG.UI.* pin at a version this commit does NOT pin is not waived" {
            // The window's premise is "the packages THIS commit pins are not published yet". A UI package
            // unresolved at some OTHER version is not that: it is a stale transitive pin, and it is red.
            //
            // THE LAST TWO ARE WHY THE BOUND IS A TOKEN MATCH AND NOT A SUBSTRING. `line.Contains uiPin`
            // reads "0.9.2" INSIDE "0.9.20" and inside "0.9.2-preview.1" — so with the pin at 0.9.2, an
            // unresolved UI package at either of those versions satisfied the version bound and the waiver
            // fired on a pin this commit never bumped. That is the fail-open the axis bound exists to
            // prevent, reached through the version half of it instead of the id half, and it becomes
            // ORDINARY the moment a patch number passes 9 (0.9.1 is a substring of 0.9.10 through 0.9.19).
            // Found by #711 while building this table; the bound now demands a delimited version token.
            for absent in [ "0.7.1"; "0.9.20"; $"{testPin}-preview.1" ] do
                Expect.equal
                    (classify false (Ok true) (restoreOutput (nu1102 "FS.GG.UI.Scene" absent)))
                    PinnedApi.NotAboutAvailability
                    $"the NU1102 names FS.GG.UI.Scene at {absent}, and this commit pins {testPin} — a \
                      DIFFERENT version. Waiving it excuses a stale pin that the bump merely happened to \
                      look like."
          }

          // ---- conjunct 3: THIS commit bumped it ------------------------------------------------

          test "an ordinary commit inheriting an unpublished pin is NOT waived — it is stale, and red" {
            // Without the bumped-here conjunct the waiver keys on "the feed lacks it", which is true of a
            // typo'd or half-released pin on every commit thereafter — so the gate would go quiet exactly
            // when the repo is broken, and stay quiet.
            let verdict = classify false (Ok false) releaseWindowOutput

            match verdict with
            | PinnedApi.Unavailable why ->
                Expect.stringContains
                    why
                    releaseWindowOutput
                    "the verdict must carry the restore that produced it, or nobody can diagnose it"
            | other ->
                failtestf
                    "a pin nobody bumped that the feed does not carry is STALE — a half-failed release, or a \
                     typo. It must be `Unavailable` (red), never deferred. Got: %A"
                    other
          }

          test "a git that cannot answer 'was it bumped?' is Unavailable — never a silent 'no'" {
            // A shallow clone has no HEAD~1. Reading that as "not bumped" would be the QUIET choice and the
            // wrong one: it silently restores the always-red release gate #543 exists to remove, and nobody
            // would ever learn why. `bumpedInCommitUnderTest` returns Error, and Error is red.
            let why = "`git diff HEAD~1 HEAD` failed — most likely a shallow clone."
            let verdict = classify false (Error why) releaseWindowOutput

            match verdict with
            | PinnedApi.Unavailable reason ->
                Expect.stringContains reason why "the git failure must reach the human, not be swallowed"
            | other ->
                failtestf
                    "an unanswerable bump question must fail CLOSED — it is neither a window nor a pass. \
                     Got: %A"
                    other
          }

          // ---- conjunct 4: never in the release lane ---------------------------------------------

          test "FS_GG_VERSION_COHERENCE_RELEASE_LANE=1 kills the waiver — the packages are DUE, not pending" {
            // The waiver's premise is "these packages cannot exist yet — this very commit creates them".
            // That stops being true at publish time. A waiver that survived into the lane would let
            // `release.yml` publish a coherent set whose members are not there.
            let verdict = classify true (Ok true) releaseWindowOutput

            match verdict with
            | PinnedApi.Unavailable _ -> ()
            | other ->
                failtestf
                    "the lane that gates the PUBLISH gets no waiver: by then the version must really be on \
                     the feed. Got: %A"
                    other
          }

          // ---- the vacuous case: "nothing to check" may not read as "checked, and it's fine" ------

          test "a probe that names no pin at all cannot waive by empty `forall` (.github#266)" {
            // THE FAILS-OPEN SHAPE THIS FILE'S OWN HEADER FORBIDS. `failedOnlyOnUnpublishedUiPin` asks
            // "is every error an NU1102, and is every NAMED pin a UI pin at this version?" — and over an
            // EMPTY set of diagnostics both halves are vacuously true. A probe TIMEOUT reports no
            // diagnostics whatsoever, and would then have waived itself into a green release window while
            // having verified precisely nothing. The `not (Array.isEmpty namedPins)` conjunct is the whole
            // defence, and this is the test that holds it there.
            let vacuous =
                [ "", "a timeout: the probe reported nothing at all"
                  "The build timed out after 360s and was killed.", "a timeout that reports PROSE, not diagnostics"
                  restoreOutput
                      [ $"{probeProj} : error NU1102:   - Found 12 version(s) in nuget.org"
                        $"{probeProj} : error NU1102:   - Versions from /usr/lib/dotnet/library-packs were not considered" ],
                  "NU1102 CONTINUATION lines only — the code is there, but no line names a package" ]

            for output, description in vacuous do
                Expect.equal
                    (classify false (Ok true) output)
                    PinnedApi.NotAboutAvailability
                    $"{description}. 'Nothing to check' and 'checked, and it's fine' must not share a \
                      verdict. Even in a real release window, an output that names no unresolved pin is not \
                      evidence OF one."
          }

          test "the NU1102 continuation lines do not VETO a waiver that should fire" {
            // The other side of the same coin, and the bug the first cut of this predicate really had:
            // NuGet repeats the error code on its detail lines, so one unresolved pin arrives as three
            // `error NU1102:` lines and only the first names a package. Ask "is every NU1102 line about a UI
            // pin?" of ALL of them and the detail lines — which name nothing — fail the test, the waiver
            // never fires, and the release is wedged by its own elaboration. Hence: asked of `namedPins`.
            match classify false (Ok true) releaseWindowOutput with
            | PinnedApi.ReleasePending why ->
                Expect.stringContains
                    why
                    releaseWindowOutput
                    "the deferral must show the releaser the restore it is deferring on"
            | other ->
                failtestf
                    "this is THE release window — a single unresolved UI pin at the bumped version, with the \
                     detail lines NuGet really emits. It must defer. Got: %A"
                    other
          }

          // ---- the verdict's CONSEQUENCE. A perfect table over a broken mapping is still a fail-open --

          test "a verdict's consequence is not merely its name: `enact` must skip, fail, or return" {
            // `classifyIn` can be flawless and the gate still fail open if `enact` mishandles the verdict —
            // a `ReleasePending` that fell through to `()` would hand the calling rule a surface that was
            // never restored, and a `NotAboutAvailability` that skipped would retire every rule in this file
            // on the first unrelated restore hiccup. Both halves are load-bearing; both are asserted.

            Expect.throwsT<Expecto.IgnoreException>
                (fun () -> PinnedApi.enact (PinnedApi.ReleasePending "the window is open"))
                "RELEASE-PENDING must be reported IGNORED — skipped is not passed, and it is not red either"

            // `AssertException` is what `failtest` raises, and it is the same exception a failed `Expect`
            // throws — which is the point: an `Unavailable` verdict must be indistinguishable from the rule
            // itself asserting and losing. Red is red.
            Expect.throwsT<Expecto.AssertException>
                (fun () -> PinnedApi.enact (PinnedApi.Unavailable "the pin is stale"))
                "an unavailable pin that is NOT a release window is a DEFECT, and must redden the gate"

            // No exception: the rule that asked gets its failure back to judge itself. This is the case that
            // keeps the waiver from having an opinion on an FS0039.
            PinnedApi.enact PinnedApi.NotAboutAvailability
          }

          test "every verdict that carries a reason carries the RESTORE that produced it" {
            // The messages are releaser-facing prose and may be reworded; the EVIDENCE may not go missing.
            // A deferral or a failure whose reason does not show the restore is one nobody can act on.
            let worlds =
                [ classify false (Ok true) releaseWindowOutput, "the release window"
                  classify false (Ok false) releaseWindowOutput, "a stale pin"
                  classify true (Ok true) releaseWindowOutput, "the release lane" ]

            for verdict, description in worlds do
                Expect.stringContains
                    (reasonOf verdict)
                    releaseWindowOutput
                    $"the verdict for {description} must echo the restore output — it is the only evidence a \
                      human has, and every one of these lands in CI where the restore itself is long gone"
          }
        ]
