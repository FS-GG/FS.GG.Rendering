module Feature231SkillManifestTests

// Feature 231 — ADR-0014 P2 gates: the product skill-manifest, the vendored materialize
// algorithm's content parity with the published FS.GG.Contracts library, and the
// no-dangling-route guard.
//
// Four gate families, all deterministic, GL-free, NO `dotnet new`:
//   G-MANIFEST  — template/skill-manifest/skill-manifest.json conforms to skill-manifest schema
//                 v1, is digest-fresh against the canonical bodies, and is catalog-coherent with
//                 the template.json emission rows (every catalogued id has a row and vice versa).
//   G-PARITY    — the vendored FsGg.Vendored.SkillMirror (template/lifecycle/, compiled into this
//                 test project) is behaviorally identical to Fsgg.SkillMirror from the pinned
//                 FS.GG.Contracts package over representative + adversarial inputs (roadmap §6:
//                 two lanes, one algorithm). Red case: perturb the vendored copy locally and any
//                 of these tests fails.
//   G-NODANGLE  — no canonical fs-gg-* skill body references a path absent from the product tree
//                 it ships into (audit F3 class); the extractor itself is proven on a synthetic
//                 12-line wrapper body (the exact pre-231 defect shape). The live-scaffold half
//                 runs in scripts/validate-lifecycle-template.fsx (dangling-routes=0).
//   G-TARGET    — template/base/Directory.Build.props carries the single FsGgMaterializeSkillRoots
//                 build step wired to the emitted script (ADR-0014 §Decision 2 standalone lane).

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private manifestPath = repositoryPath "template/skill-manifest/skill-manifest.json"
let private templateJsonPath = repositoryPath ".template.config/template.json"

/// The catalog contract: id -> canonical body source (mirrors scripts/generate-skill-manifest.fsx).
/// ADR-0063 (2026-07-21 amendment) RETIRED the four game-owned rows (fs-gg-game-core, fs-gg-audio,
/// fs-gg-persistence, fs-gg-model-swap) from this provider (FS.GG.Rendering#965): they are now
/// owner-sourced from FS.GG.Game.Skills, no longer frozen here, so they carry no catalog row.
let private canonicalSources =
    [ "fs-gg-collision", "template/product-skills/fs-gg-collision/SKILL.md"
      "fs-gg-elmish", "template/product-skills/fs-gg-elmish/SKILL.md"
      "fs-gg-feedback-report", "template/feedback-report/skill/SKILL.md"
      "fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md"
      "fs-gg-keyboard-input", "template/product-skills/fs-gg-keyboard-input/SKILL.md"
      "fs-gg-layout", "template/product-skills/fs-gg-layout/SKILL.md"
      "fs-gg-line-drawing", "template/product-skills/fs-gg-line-drawing/SKILL.md"
      "fs-gg-project", "template/base/.agents/skills/fs-gg-project/SKILL.md"
      "fs-gg-samples", "template/fragments/samples/skill/SKILL.md"
      "fs-gg-scene", "template/product-skills/fs-gg-scene/SKILL.md"
      "fs-gg-skiaviewer", "template/product-skills/fs-gg-skiaviewer/SKILL.md"
      "fs-gg-styling", "template/product-skills/fs-gg-styling/SKILL.md"
      "fs-gg-symbol-design", "template/product-skills/fs-gg-symbol-design/SKILL.md"
      "fs-gg-symbology", "template/product-skills/fs-gg-symbology/SKILL.md"
      "fs-gg-testing", "template/product-skills/fs-gg-testing/SKILL.md"
      "fs-gg-ui-widgets", "template/product-skills/fs-gg-ui-widgets/SKILL.md"
      "fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md" ]

type private ManifestEntry =
    { Id: string
      Scope: string
      Sha256: string
      ResolvablePath: string }

let private jsonStr (e: JsonElement) : string =
    e.GetString() |> Option.ofObj |> Option.defaultValue ""

let private readManifest () =
    Expect.isTrue (File.Exists manifestPath) (sprintf "manifest missing at %s — run dotnet fsi scripts/generate-skill-manifest.fsx" manifestPath)
    use doc = JsonDocument.Parse(File.ReadAllText manifestPath)
    let root = doc.RootElement
    let schemaVersion = root.GetProperty("schemaVersion").GetInt32()
    let entries =
        [ for e in root.GetProperty("skills").EnumerateArray() ->
            { Id = jsonStr (e.GetProperty "id")
              Scope = jsonStr (e.GetProperty "scope")
              Sha256 = jsonStr (e.GetProperty "sha256")
              ResolvablePath = jsonStr (e.GetProperty "resolvablePath") } ]
    schemaVersion, entries

/// Digest semantics of Fsgg.SkillMirror.sha256 / the generator: hex(SHA256(UTF8(text))).
let private sha256Text (body: string) =
    Encoding.UTF8.GetBytes body
    |> SHA256.HashData
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

// ---- G-NODANGLE extraction (same rules as validate-lifecycle-template.fsx) ---------------------

/// Backtick-quoted path-like tokens that must resolve in the product a skill ships into.
/// Placeholders (`<`, `*`, `{`) and the readiness/ product convention (created on first build)
/// are skipped; any `../` escape is always extracted.
let private pathTokens (body: string) =
    [ for m in System.Text.RegularExpressions.Regex.Matches(body, "`([^`\n]+)`") do
        let token = m.Groups.[1].Value.Trim()
        let isPlaceholder = token.Contains "<" || token.Contains "*" || token.Contains "{"
        let isProductConvention = token = "readiness/" || token.StartsWith "readiness/"
        let looksRooted =
            [ "docs/"; "src/"; "samples/"; "scripts/"; ".specify/"; ".agents/"; ".claude/"; ".codex/" ]
            |> List.exists token.StartsWith
        if not isPlaceholder && not isProductConvention && (looksRooted || token.StartsWith "../") then
            yield token ]

/// Fold `.` and `..` over a '/'-separated path. A `..` that cannot be cancelled is retained, so a
/// path escaping its root stays visibly prefixed with `..`.
let private normalizeRelative (path: string) =
    path.Split('/')
    |> Array.filter (fun s -> s <> "" && s <> ".")
    |> Array.fold
        (fun acc segment ->
            match segment, acc with
            | "..", top :: rest when top <> ".." -> rest
            | _ -> segment :: acc)
        []
    |> List.rev
    |> String.concat "/"

/// Does a path token extracted from `containingFile` resolve in the product the skill ships into?
/// Resolution universe: the ungated template/base tree (docs/, src-shape, load-product.fsx …), the
/// skill's own directory, and the spec-kit workspace (.specify/ from the repo).
let private resolves (skillId: string) (skillRoot: string) (containingFile: string) (token: string) =
    let cleaned = (token.TrimEnd('.', ':', ',')).TrimEnd('/')
    // Branch on the raw token: `cleaned` strips the trailing slash, so a bare `../` would otherwise
    // arrive here as `..` and fall through to the product-root arm as `template/base/..` — the repo.
    if token.StartsWith "../" then
        // A relative link resolves from the file that holds it, not from the product root. Anchor at
        // the skill's product location and normalize: a link that stays inside the skill is a real file
        // next to its author (`reference/labels.md` -> `../SKILL.md`), while one that climbs out lands
        // at the product root (a wrapper's `../../../src/X`) and must exist in the ungated
        // template/base tree. (The depth-3 anchor is wrong for the three canonical sources that do not
        // ship to `.agents/skills/<id>/` — feedback, feedback-report, samples. Carried from the
        // `../../../`-strip this replaces; the product location belongs in `canonicalSources`.)
        let skillPrefix = ".agents/skills/" + skillId
        let containingDir =
            Path.GetDirectoryName containingFile |> Option.ofObj |> Option.defaultValue skillRoot
        let fileDir =
            Path.GetRelativePath(skillRoot, containingDir).Replace(Path.DirectorySeparatorChar, '/')
        let fileDir = if fileDir = "." then "" else fileDir + "/"
        let productPath = normalizeRelative (skillPrefix + "/" + fileDir + cleaned)
        let segments = productPath.Split('/')
        if segments.[0] = ".." then
            false // climbs out of the product entirely
        elif productPath.StartsWith(skillPrefix + "/") then
            let insideSkill = productPath.Substring(skillPrefix.Length + 1)
            let onDisk = Path.Combine(skillRoot, insideSkill.Replace('/', Path.DirectorySeparatorChar))
            File.Exists onDisk || Directory.Exists onDisk
        else
            File.Exists(repositoryPath ("template/base/" + productPath))
            || Directory.Exists(repositoryPath ("template/base/" + productPath))
    elif cleaned.StartsWith ".specify" then
        File.Exists(repositoryPath cleaned) || Directory.Exists(repositoryPath cleaned)
    elif cleaned = "samples" || cleaned.StartsWith "samples/" then
        // emitted by the sample-pack row: template/fragments/samples/ -> samples/
        Directory.Exists(repositoryPath "template/fragments/samples")
    else
        File.Exists(repositoryPath ("template/base/" + cleaned))
        || Directory.Exists(repositoryPath ("template/base/" + cleaned))

[<Tests>]
let feature231SkillManifestTests =
    testList
        "Feature231 skill manifest + vendored parity + no-dangling guard"
        [
          // ---- G-MANIFEST ------------------------------------------------------------------

          test "G-MANIFEST manifest conforms to skill-manifest schema v1 (shape, order, scope, paths)" {
              let schemaVersion, entries = readManifest ()
              Expect.equal schemaVersion 1 "schemaVersion is 1 (Fsgg.Schemas.skillManifestVersion)"
              Expect.equal (entries |> List.map (fun e -> e.Id)) (entries |> List.map (fun e -> e.Id) |> List.sort) "entries sorted by id (deterministic manifest)"
              for e in entries do
                  Expect.equal e.Scope "product" (sprintf "%s: provider manifest carries scope 'product' only (dev surface never ships)" e.Id)
                  Expect.equal e.ResolvablePath (sprintf ".agents/skills/%s/SKILL.md" e.Id) (sprintf "%s: resolvablePath is the provider-source-root skill path" e.Id)
                  Expect.isTrue (e.Sha256.Length = 64 && e.Sha256 |> Seq.forall (fun c -> Char.IsAsciiDigit c || (c >= 'a' && c <= 'f'))) (sprintf "%s: sha256 is 64-char lowercase hex" e.Id)
          }

          test "G-MANIFEST digests are fresh against the canonical bodies (content-addressed, ADR-0014 §1/§3)" {
              let _, entries = readManifest ()
              let byId = entries |> List.map (fun e -> e.Id, e) |> Map.ofList
              Expect.equal (entries |> List.map (fun e -> e.Id) |> Set.ofList) (canonicalSources |> List.map fst |> Set.ofList) "manifest ids equal the declared catalog"
              for id, source in canonicalSources do
                  let body = File.ReadAllText(repositoryPath source)
                  Expect.equal (Map.find id byId).Sha256 (sha256Text body) (sprintf "%s: manifest sha256 stale vs %s — run dotnet fsi scripts/generate-skill-manifest.fsx" id source)
          }

          test "G-MANIFEST catalog is coherent with the template.json emission rows" {
              use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
              let emittedIds =
                  [ for s in doc.RootElement.GetProperty("sources").EnumerateArray() do
                      let str (prop: string) =
                          match s.TryGetProperty prop with
                          | true, v -> v.GetString() |> Option.ofObj |> Option.defaultValue ""
                          | _ -> ""
                      let target = (str "target").Replace('\\', '/')
                      // an emission row for skill <id> targets .agents/skills/<id>/ — issue #91 gave
                      // fs-gg-project its own such row, so the scan now recovers the whole catalog.
                      if target.StartsWith ".agents/skills/fs-gg-" then
                          yield target.Substring(".agents/skills/".Length).TrimEnd('/') ]
                  |> Set.ofList
              Expect.equal emittedIds (canonicalSources |> List.map fst |> Set.ofList) "every catalogued skill has an emission row and every emitted fs-gg-* skill is catalogued"
          }

          // ---- G-PARITY (vendored ≡ FS.GG.Contracts 1.4.0) ----------------------------------

          test "G-PARITY constants: agentSkillRoots and providerSourceRoot match the library" {
              Expect.equal FsGg.Vendored.SkillMirror.agentSkillRoots Fsgg.Schemas.agentSkillRoots "vendored AGENT_SKILL_ROOTS equals Fsgg.Schemas.agentSkillRoots"
              Expect.equal FsGg.Vendored.SkillMirror.providerSourceRoot Fsgg.SkillMirror.providerSourceRoot "vendored providerSourceRoot equals the library's"
          }

          test "G-PARITY sha256 / skillPath / skillIdOfPath / mirrorTargetRoots / retargetSkillPath agree on the corpus" {
              let bodies = [ ""; "x"; "line1\nline2\n"; "unicode ⚑ ünïcødé\r\nCRLF"; String.replicate 10000 "a" ]
              for b in bodies do
                  Expect.equal (FsGg.Vendored.SkillMirror.sha256 b) (Fsgg.SkillMirror.sha256 b) (sprintf "sha256 diverges for %A" b)
              let roots = [ ".claude"; ".codex"; ".agents"; ".other" ]
              for r in roots do
                  Expect.equal (FsGg.Vendored.SkillMirror.skillPath r "fs-gg-scene") (Fsgg.SkillMirror.skillPath r "fs-gg-scene") "skillPath diverges"
              let paths =
                  [ ".agents/skills/fs-gg-scene/SKILL.md"
                    ".agents\\skills\\fs-gg-scene\\SKILL.md"
                    ".claude/skills/x/SKILL.md"
                    ".agents/skills/fs-gg-symbology/reference.fsx"
                    ".agents/skills/skill-manifest.json"
                    "skills/only/SKILL.md"
                    ""
                    "SKILL.md" ]
              for p in paths do
                  Expect.equal (FsGg.Vendored.SkillMirror.skillIdOfPath p) (Fsgg.SkillMirror.skillIdOfPath p) (sprintf "skillIdOfPath diverges for %A" p)
                  for targetRoot in [ ".claude"; ".codex" ] do
                      Expect.equal
                          (FsGg.Vendored.SkillMirror.retargetSkillPath targetRoot p)
                          (Fsgg.SkillMirror.retargetSkillPath targetRoot p)
                          (sprintf "retargetSkillPath diverges for %A -> %s" p targetRoot)
              Expect.equal
                  (FsGg.Vendored.SkillMirror.mirrorTargetRoots [ ".claude"; ".codex"; ".agents" ])
                  (Fsgg.SkillMirror.mirrorTargetRoots [ ".claude"; ".codex"; ".agents" ])
                  "mirrorTargetRoots diverges"
          }

          test "G-PARITY mirror produces identical write plans (order, paths, bodies)" {
              let cases =
                  [ [], [ ".claude"; ".codex"; ".agents" ]
                    [ "b-skill", "body-b"; "a-skill", "body-a" ], [ ".claude"; ".codex"; ".agents" ]
                    [ "only", "" ], [ ".agents" ]
                    [ "s1", "x"; "s2", "y"; "s3", "z" ], [] ]
              for skills, roots in cases do
                  let vendored = FsGg.Vendored.SkillMirror.mirror roots skills |> List.map (fun w -> w.Path, w.Body)
                  let library = Fsgg.SkillMirror.mirror roots skills |> List.map (fun w -> w.Path, w.Body)
                  Expect.equal vendored library (sprintf "mirror plan diverges for %A / %A" skills roots)
          }

          test "G-PARITY verify produces identical drift verdicts (missing / divergent / hash-mismatch / clean / no-digest)" {
              let roots = [ ".claude"; ".codex"; ".agents" ]
              let goodBody = "the canonical body\n"
              let goodHash = sha256Text goodBody
              // expected: one digest-checked product skill, one no-digest process skill, one absent everywhere.
              let expectedSpec = [ "fs-gg-scene", "product", goodHash; "speckit-plan", "process", ""; "fs-gg-ghost", "product", goodHash ]
              // actual: scene divergent in .codex + hash-mismatched there; speckit-plan missing in .claude; ghost absent.
              let actualSpec =
                  [ ".claude", "fs-gg-scene", Some goodBody
                    ".codex", "fs-gg-scene", Some "tampered\n"
                    ".agents", "fs-gg-scene", Some goodBody
                    ".codex", "speckit-plan", Some "process body"
                    ".agents", "speckit-plan", Some "process body" ]
              let vendoredDrift =
                  FsGg.Vendored.SkillMirror.verify
                      roots
                      (expectedSpec |> List.map (fun (id, scope, h) ->
                          { FsGg.Vendored.SkillMirror.ExpectedSkill.Id = id
                            Scope = (if scope = "product" then FsGg.Vendored.SkillMirror.Product else FsGg.Vendored.SkillMirror.Process)
                            Sha256 = h }))
                      (actualSpec |> List.map (fun (r, id, b) ->
                          { FsGg.Vendored.SkillMirror.ActualCopy.Root = r; Id = id; Body = b }))
                  |> List.map (fun d -> d.Id, d.MissingRoots, d.Divergent, d.HashMismatchRoots, (match d.Scope with FsGg.Vendored.SkillMirror.Product -> "product" | _ -> "process"))
              let libraryDrift =
                  Fsgg.SkillMirror.verify
                      roots
                      (expectedSpec |> List.map (fun (id, scope, h) ->
                          { Fsgg.SkillMirror.ExpectedSkill.Id = id
                            Scope = (if scope = "product" then Fsgg.Schemas.SkillScope.Product else Fsgg.Schemas.SkillScope.Process)
                            Sha256 = h }))
                      (actualSpec |> List.map (fun (r, id, b) ->
                          { Fsgg.SkillMirror.ActualCopy.Root = r; Id = id; Body = b }))
                  |> List.map (fun d -> d.Id, d.MissingRoots, d.Divergent, d.HashMismatchRoots, (match d.Scope with Fsgg.Schemas.SkillScope.Product -> "product" | _ -> "process"))
              Expect.equal vendoredDrift libraryDrift "verify verdicts diverge between the vendored copy and the library"
              // sanity: the corpus actually exercises all three drift dimensions.
              Expect.equal (vendoredDrift |> List.map (fun (id, _, _, _, _) -> id)) [ "fs-gg-ghost"; "fs-gg-scene"; "speckit-plan" ] "expected drift set"
          }

          // ---- G-NODANGLE --------------------------------------------------------------------

          test "G-NODANGLE canonical fs-gg-* bodies carry no unresolvable path reference (env-free core)" {
              let failures =
                  [ for id, source in canonicalSources do
                      let dir = Path.GetDirectoryName(repositoryPath source) |> Option.ofObj |> Option.defaultValue "."
                      for file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories) do
                          for token in pathTokens (File.ReadAllText file) do
                              if not (resolves id dir file token) then
                                  yield sprintf "%s: `%s` (in %s)" id token (Path.GetFileName file) ]
              Expect.isEmpty failures (sprintf "dangling skill routes (audit F3 class): %s" (String.concat "; " failures))
          }

          test "G-NODANGLE the extractor flags the pre-231 wrapper shape (red-case proof)" {
              let wrapperBody =
                  "# FS.GG Diagnostics\n\nThis is the Codex-active wrapper for the canonical local skill.\n\n"
                  + "Before acting, read the canonical instructions in:\n\n`../../../src/Diagnostics/skill/SKILL.md`\n"
              Expect.equal (pathTokens wrapperBody) [ "../../../src/Diagnostics/skill/SKILL.md" ] "the 12-line wrapper's repo-internal route is extracted"
              let placeholderBody = "See `src/<YourProduct>/<YourProduct>.fsproj` and record under `readiness/logs/`."
              Expect.isEmpty (pathTokens placeholderBody) "placeholders and the readiness/ convention are not flagged"
          }

          test "G-NODANGLE a relative route resolves from the file that holds it, not the product root" {
              // #307: every token used to resolve against template/base/, so an intra-skill sibling link
              // (`../SKILL.md` from reference/labels.md) could never resolve and read as a dangling route.
              let symbologyRoot = repositoryPath "template/product-skills/fs-gg-symbology"
              let labels = Path.Combine(symbologyRoot, "reference", "labels.md")
              Expect.isTrue
                  (resolves "fs-gg-symbology" symbologyRoot labels "../SKILL.md")
                  "a sibling-relative link to the skill's own SKILL.md resolves"
              Expect.isFalse
                  (resolves "fs-gg-symbology" symbologyRoot labels "../no-such-file.md")
                  "a sibling-relative link to nothing is still a dangling route"

              // The wrapper shape must keep resolving at the product root: `.agents/skills/<id>/` is three
              // deep, so `../../../X` lands on X in template/base/.
              let projectRoot = repositoryPath "template/base/.agents/skills/fs-gg-project"
              let projectSkill = Path.Combine(projectRoot, "SKILL.md")
              Expect.isTrue
                  (resolves "fs-gg-project" projectRoot projectSkill "../../../docs")
                  "a wrapper route climbing to the product root resolves against template/base/"
              Expect.isFalse
                  (resolves "fs-gg-project" projectRoot projectSkill "../../../../escape.md")
                  "a route climbing out of the product does not resolve"

              // `cleaned` drops the trailing slash, so a bare `../` must not fall through to the
              // product-root arm and probe `template/base/..` — the repository itself.
              Expect.isTrue
                  (resolves "fs-gg-project" projectRoot projectSkill "../")
                  "a bare ../ names the skills directory, which exists"
              Expect.isFalse
                  (resolves "fs-gg-project" projectRoot projectSkill "../../../no-such-dir/")
                  "a trailing slash does not smuggle a missing directory past the guard"
          }

          test "G-NODANGLE normalizeRelative folds . and .. and keeps an uncancelled escape visible" {
              Expect.equal (normalizeRelative ".agents/skills/x/reference/../SKILL.md") ".agents/skills/x/SKILL.md" "a .. cancels its parent"
              Expect.equal (normalizeRelative ".agents/skills/x/./SKILL.md") ".agents/skills/x/SKILL.md" "a . is dropped"
              Expect.equal (normalizeRelative ".agents/skills/x/../../../src/X") "src/X" "three .. reach the product root"
              Expect.equal (normalizeRelative ".agents/skills/x/../../../../out") "../out" "an uncancelled .. survives"
              // an escape is the leading *segment* `..`, not any segment that starts with those two chars.
              Expect.equal (normalizeRelative ".agents/skills/x/../../../..data/y") "..data/y" "a segment merely beginning with .. is not an escape"
          }

          // ---- G-TARGET ----------------------------------------------------------------------

          test "G-TARGET Directory.Build.props carries the single FsGgMaterializeSkillRoots build step" {
              let props = File.ReadAllText(repositoryPath "template/base/Directory.Build.props")
              Expect.stringContains props "Name=\"FsGgMaterializeSkillRoots\"" "the materialize target exists"
              Expect.stringContains props "BeforeTargets=\"Build\"" "it runs before Build"
              Expect.stringContains props ".specify/scripts/fs-gg/materialize-skill-roots.fsx" "it is wired to the emitted vendored script"
              Expect.stringContains props "dotnet fsi" "it invokes the script via dotnet fsi (SDK-bundled, no restore)"
              Expect.stringContains props "Exists('$(FsGgSkillMaterializeScript)')" "it self-gates on the spec-kit-only script (never fires under sdd/none)"
              let occurrences =
                  System.Text.RegularExpressions.Regex.Matches(props, "FsGgMaterializeSkillRoots").Count
              Expect.isTrue (occurrences >= 1) "target present"
              // exactly one <Target> WRITES THE ROOTS — ONE mechanism (SC-003). ADR-0056 added a
              // second, unrelated target (FsGgSddLifecycleGuard) that writes nothing and only warns on
              // the sdd-lane guard sentinel, so count the roots-materialize target by name rather than
              // all <Target>s — the SC-003 guarantee is "one roots mechanism", not "one target total".
              Expect.equal
                  (System.Text.RegularExpressions.Regex.Matches(props, "Name=\"FsGgMaterializeSkillRoots\"").Count)
                  1
                  "exactly one roots-materialize target (ONE mechanism, SC-003)"
          }
        ]
