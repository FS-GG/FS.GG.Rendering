module Feature131AntPatternDocsTests

// Feature 131 (F6): docs honesty/coverage check for the Ant interaction-pattern docs,
// the curated semantic-parts snapshot, the enterprise-template recipes, and the advisory
// `fs-gg-ant-design` skill. Pure file-read + reflection over the already-referenced public
// assemblies — no new project reference, no Markdown/YAML/JSON parser dependency, no GL/display/network.
// Contract: specs/131-ant-pattern-docs-skill/contracts/docs-coverage-check.md (12 named cases).

open System
open System.IO
open System.Reflection
open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Color
open FS.GG.TestSupport

// ---------------------------------------------------------------------------
// Repo root + doc locations (slnx marker; the established Feature126/127 pattern)
// ---------------------------------------------------------------------------

let private repositoryRoot = RepositoryRoot.value

let private antRoot = Path.Combine(repositoryRoot, "docs", "product", "ant-design")
let private patternsDir = Path.Combine(antRoot, "patterns")
let private templatesDir = Path.Combine(antRoot, "templates")
let private readmePath = Path.Combine(antRoot, "README.md")
let private skillPath =
    Path.Combine(repositoryRoot, ".claude", "skills", "fs-gg-ant-design", "SKILL.md")
let private hubPath = Path.Combine(antRoot, "reference", "ant-llms-sources.md")

// ---------------------------------------------------------------------------
// Line parsers (no Markdown/YAML dependency — research R2)
// ---------------------------------------------------------------------------

let private normalize (text: string) = text.Replace("\r\n", "\n").Replace("\r", "\n")

// Path.GetFileName / GetDirectoryName are nullable under this repo's nullness settings.
let private fileNameOf (p: string) = match Path.GetFileName p with | null -> p | n -> n
let private dirOf (p: string) = match Path.GetDirectoryName p with | null -> "" | d -> d

/// Parse the leading `---`-delimited front-matter into a key→value map (quotes trimmed).
let private readFrontMatter (text: string) : Map<string, string> =
    let lines = (normalize text).Split('\n')
    if lines.Length = 0 || lines.[0].Trim() <> "---" then Map.empty
    else
        let mutable result = Map.empty
        let mutable i = 1
        let mutable stop = false
        while i < lines.Length && not stop do
            let line = lines.[i]
            if line.Trim() = "---" then stop <- true
            else
                let idx = line.IndexOf(':')
                if idx > 0 then
                    let k = line.Substring(0, idx).Trim()
                    let v = line.Substring(idx + 1).Trim().Trim('"')
                    result <- Map.add k v result
            i <- i + 1
        result

/// Collect the non-blank lines inside the fenced ```refs blocks.
let private readRefLines (text: string) : string list =
    let lines = (normalize text).Split('\n')
    let acc = ResizeArray<string>()
    let mutable inBlock = false
    for line in lines do
        let t = line.Trim()
        if not inBlock && t.StartsWith "```" && t.Substring(3).Trim() = "refs" then
            inBlock <- true
        elif inBlock && t.StartsWith "```" then
            inBlock <- false
        elif inBlock && t <> "" then
            acc.Add t
    List.ofSeq acc

// ---------------------------------------------------------------------------
// Loaded docs
// ---------------------------------------------------------------------------

type private Doc =
    { Path: string
      Name: string
      Front: Map<string, string>
      RefLines: string list
      Text: string }

let private loadDir (dir: string) : Doc list =
    if Directory.Exists dir then
        Directory.GetFiles(dir, "*.md")
        |> Array.sort
        |> Array.map (fun p ->
            let text = File.ReadAllText p
            { Path = p
              Name = fileNameOf p
              Front = readFrontMatter text
              RefLines = readRefLines text
              Text = text })
        |> List.ofArray
    else []

let private loadFile (p: string) : Doc option =
    if File.Exists p then
        let text = File.ReadAllText p
        Some
            { Path = p
              Name = fileNameOf p
              Front = readFrontMatter text
              RefLines = readRefLines text
              Text = text }
    else None

let private patternDocs = loadDir patternsDir
let private templateDocs = loadDir templatesDir
let private readmeDoc = loadFile readmePath
let private skillDoc = loadFile skillPath

/// Every doc whose `refs` block participates in resolution (patterns, templates, README, skill).
let private allRefDocs =
    patternDocs @ templateDocs @ (Option.toList readmeDoc) @ (Option.toList skillDoc)

let private allowedPrefixes = set [ "control"; "token"; "resolver"; "policy"; "doc"; "part" ]

/// (doc, prefix, value) for every well-formed ref line.
let private flatRefs =
    allRefDocs
    |> List.collect (fun d ->
        d.RefLines
        |> List.choose (fun line ->
            let idx = line.IndexOf(':')
            if idx > 0 then Some(d, line.Substring(0, idx), line.Substring(idx + 1)) else None))

let private refsOf (prefix: string) =
    flatRefs |> List.filter (fun (_, p, _) -> p = prefix)

// ---------------------------------------------------------------------------
// Sources of truth (Catalog data + reflection over public DesignSystem/Color)
// ---------------------------------------------------------------------------

let private categorySet = Catalog.categories () |> Set.ofList
let private controlIds = Catalog.supportedControls |> List.map (fun c -> c.Id) |> Set.ofList

let private designSystemAsm =
    // Touch a public member to guarantee the assembly is loaded, then locate it.
    DesignTokensExt.Seed.colorPrimary |> ignore
    AppDomain.CurrentDomain.GetAssemblies()
    |> Array.find (fun a -> a.GetName().Name = "FS.GG.UI.DesignSystem")

let private staticFlags =
    BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly

let private resolverMembers : Set<string> =
    match designSystemAsm.GetType "FS.GG.UI.DesignSystem.StyleResolver" with
    | null -> Set.empty
    | t ->
        let members = t.GetMembers staticFlags |> Array.map (fun m -> m.Name)
        let nested = t.GetNestedTypes BindingFlags.Public |> Array.map (fun n -> n.Name)
        Set.ofArray (Array.append members nested)

/// Relative-qualified token names, e.g. "Seed.colorPrimary", "Map.Light.colorBorder", "Light.foreground".
let private tokenNames : Set<string> =
    let acc = System.Collections.Generic.HashSet<string>()
    let rec walk (t: Type) (prefix: string) =
        for p in t.GetProperties staticFlags do acc.Add(prefix + p.Name) |> ignore
        for f in t.GetFields staticFlags do acc.Add(prefix + f.Name) |> ignore
        for n in t.GetNestedTypes BindingFlags.Public do walk n (prefix + n.Name + ".")
    for rootName in [ "DesignTokensExt"; "DesignTokens" ] do
        match designSystemAsm.GetType("FS.GG.UI.DesignSystem." + rootName) with
        | null -> ()
        | root -> for n in root.GetNestedTypes BindingFlags.Public do walk n (n.Name + ".")
    Set.ofSeq acc

let private policyResolves (name: string) =
    match ColorPolicy.byName name with
    | Ok _ -> true
    | _ -> false

let private templateNames = set [ "workbench"; "list"; "detail"; "form"; "result"; "exception" ]

let private designLanguageMarker =
    "Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency."

let private prefixesIn (d: Doc) =
    d.RefLines
    |> List.choose (fun line ->
        let idx = line.IndexOf(':')
        if idx > 0 then Some(line.Substring(0, idx)) else None)
    |> Set.ofList

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[<Tests>]
let feature131Tests =
    testList
        "Feature131 Ant pattern docs and skill"
        [ test "Family_coverage_is_bijective" {
              let families = patternDocs |> List.choose (fun d -> Map.tryFind "family" d.Front)
              let dups = families |> List.countBy id |> List.filter (fun (_, n) -> n > 1) |> List.map fst
              Expect.isEmpty dups (sprintf "duplicate family front-matter across patterns/: %A" dups)
              let docSet = Set.ofList families
              let missing = Set.difference categorySet docSet |> Set.toList
              let extra = Set.difference docSet categorySet |> Set.toList
              Expect.isEmpty missing (sprintf "Catalog.categories with no pattern doc: %A" missing)
              Expect.isEmpty extra (sprintf "pattern docs with unknown family: %A" extra) }

          test "Each_template_recipe_present_once_and_groundwork" {
              let pairs =
                  templateDocs
                  |> List.map (fun d -> d.Name, Map.tryFind "template" d.Front, Map.tryFind "status" d.Front)
              let names = pairs |> List.choose (fun (_, t, _) -> t)
              let counts = names |> List.countBy id |> Map.ofList
              for name in templateNames do
                  let c = Map.tryFind name counts |> Option.defaultValue 0
                  Expect.equal c 1 (sprintf "template '%s' must appear exactly once (found %d)" name c)
              let extra = Set.difference (Set.ofList names) templateNames |> Set.toList
              Expect.isEmpty extra (sprintf "unknown template names: %A" extra)
              for (name, t, s) in pairs do
                  Expect.isSome t (sprintf "%s: missing template front-matter" name)
                  Expect.equal s (Some "groundwork") (sprintf "%s: status must be 'groundwork'" name) }

          test "All_control_refs_resolve" {
              for (d, _, v) in refsOf "control" do
                  Expect.isTrue (controlIds.Contains v) (sprintf "%s: control ref '%s' is not a catalog id" d.Name v) }

          test "All_resolver_refs_resolve" {
              for (d, _, v) in refsOf "resolver" do
                  Expect.isTrue (resolverMembers.Contains v) (sprintf "%s: resolver ref '%s' is not a public StyleResolver member" d.Name v) }

          test "All_token_refs_resolve" {
              for (d, _, v) in refsOf "token" do
                  Expect.isTrue (tokenNames.Contains v) (sprintf "%s: token ref '%s' does not resolve on a public DesignSystem token type" d.Name v) }

          test "All_policy_refs_resolve" {
              for (d, _, v) in refsOf "policy" do
                  Expect.isTrue (policyResolves v) (sprintf "%s: policy ref '%s' is not accepted by ColorPolicy.byName" d.Name v) }

          test "All_doc_links_resolve" {
              for (d, _, v) in refsOf "doc" do
                  let target = Path.GetFullPath(Path.Combine(dirOf d.Path, v))
                  Expect.isTrue (File.Exists target) (sprintf "%s: doc ref '%s' does not resolve to a file" d.Name v) }

          test "Pattern_docs_have_required_refs" {
              Expect.isNonEmpty patternDocs "no pattern docs found under patterns/"
              for d in patternDocs do
                  let ps = prefixesIn d
                  for required in [ "control"; "token"; "resolver"; "policy" ] do
                      Expect.isTrue (ps.Contains required) (sprintf "%s: pattern doc needs >=1 '%s:' ref" d.Name required) }

          test "Pattern_docs_declare_semantic_parts" {
              for d in patternDocs do
                  let parts =
                      d.RefLines
                      |> List.choose (fun line ->
                          let idx = line.IndexOf(':')
                          if idx > 0 && line.Substring(0, idx) = "part" then Some(line.Substring(idx + 1)) else None)
                  Expect.isNonEmpty parts (sprintf "%s: pattern doc needs >=1 'part:<Component>/<partName>' ref (FR-011)" d.Name)
                  for value in parts do
                      let segs = value.Split('/')
                      Expect.equal segs.Length 2 (sprintf "%s: part ref '%s' must be <Component>/<partName> (exactly one '/')" d.Name value)
                      if segs.Length = 2 then
                          Expect.isTrue (segs.[0].Length > 0) (sprintf "%s: part ref '%s' has empty Ant component" d.Name value)
                          Expect.isTrue (segs.[1].Length > 0) (sprintf "%s: part ref '%s' has empty part name" d.Name value)
                  // companion code refs that anchor the parts to repo machinery
                  let ps = prefixesIn d
                  for required in [ "control"; "token"; "resolver" ] do
                      Expect.isTrue (ps.Contains required) (sprintf "%s: a doc with part: refs must also carry a '%s:' ref" d.Name required) }

          test "Pattern_docs_state_design_language_only" {
              for d in patternDocs do
                  Expect.stringContains d.Text designLanguageMarker (sprintf "%s: missing the design-language-only assertion line" d.Name) }

          test "Skill_is_advisory_and_reminds_layering" {
              match skillDoc with
              | None -> failtestf "SKILL.md not found at %s" skillPath
              | Some d ->
                  Expect.equal (Map.tryFind "name" d.Front) (Some "fs-gg-ant-design") "skill front-matter name must be fs-gg-ant-design"
                  match Map.tryFind "description" d.Front with
                  | Some desc -> Expect.isTrue (desc.Length > 0) "skill description must be non-empty"
                  | None -> failtest "skill front-matter must contain a description"
                  Expect.stringContains d.Text designLanguageMarker "skill must contain the no-React/DOM statement"
                  let lower = d.Text.ToLowerInvariant()
                  Expect.stringContains lower "per-theme" "skill must state the no-per-theme-fork layering rule"
                  let gatePhrases =
                      [ "blocking step"; "readiness gate"; "must pass before merge"; "required gate"; "do not proceed until" ]
                  for phrase in gatePhrases do
                      Expect.isFalse (lower.Contains phrase) (sprintf "skill must stay advisory — found gating phrase '%s'" phrase) }

          test "Upstream_source_hub_is_central" {
              Expect.isTrue (File.Exists hubPath) (sprintf "central Ant source-of-truth hub missing at %s" hubPath)
              let hubText = File.ReadAllText hubPath
              for f in [ "llms.txt"; "llms-full.txt"; "llms-semantic.md" ] do
                  Expect.stringContains hubText f (sprintf "hub must catalog the Ant LLM file '%s'" f)
              let hubFull = Path.GetFullPath hubPath
              let citesHub (doc: Doc option) =
                  match doc with
                  | None -> false
                  | Some d ->
                      d.RefLines
                      |> List.exists (fun line ->
                          let idx = line.IndexOf(':')
                          if idx > 0 && line.Substring(0, idx) = "doc" then
                              let target = Path.GetFullPath(Path.Combine(dirOf d.Path, line.Substring(idx + 1)))
                              target = hubFull
                          else false)
              Expect.isTrue (citesHub skillDoc) "SKILL.md must carry a doc: ref resolving to the central hub"
              Expect.isTrue (citesHub readmeDoc) "README.md must carry a doc: ref resolving to the central hub" }

          test "No_unknown_ref_prefixes" {
              for d in allRefDocs do
                  for line in d.RefLines do
                      let idx = line.IndexOf(':')
                      Expect.isTrue (idx > 0) (sprintf "%s: ref line '%s' is not prefix:value" d.Name line)
                      if idx > 0 then
                          let prefix = line.Substring(0, idx)
                          let value = line.Substring(idx + 1)
                          Expect.isTrue (allowedPrefixes.Contains prefix) (sprintf "%s: unknown ref prefix in '%s'" d.Name line)
                          Expect.notEqual (line.[idx - 1]) ' ' (sprintf "%s: space before ':' in '%s'" d.Name line)
                          Expect.isTrue (value.Length > 0 && value.[0] <> ' ') (sprintf "%s: empty value or space after ':' in '%s'" d.Name line) } ]
