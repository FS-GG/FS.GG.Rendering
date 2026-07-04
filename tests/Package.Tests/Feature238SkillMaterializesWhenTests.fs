module Feature238SkillMaterializesWhenTests

// Feature 238 (issue #71, ADR-0017 P1 Rendering) — the product skill-manifest records, per entry,
// WHEN its body materializes and WHERE the body comes from:
//   materializes-when — the VERBATIM template.json sources[].condition that gates the skill's body.
//   supplied-by       — the provider source directory holding the canonical SKILL.md.
//
// The point is fs-gg-project: declared scope:product but emitted only under lifecycle == "spec-kit",
// so under the sdd lane it is declared-but-unsupplied. Recording its condition turns the sdd-lane
// gap from an untyped `declared ∧ absent` into an honest `declared ∧ condition-false ∧ absent`, so
// the downstream union gate (.github#164) can tell legitimate suppression from a real [missing].
//
// Guards (deterministic, GL-free, no `dotnet new`):
//   G-PRESENT     — every entry carries non-empty materializes-when + supplied-by.
//   G-CONDITION   — materializes-when(id) == the verbatim template.json condition for that skill's
//                   body source (product/samples/feedback by target .agents/skills/<id>/;
//                   fs-gg-project by the whole-tree template/base/.agents/ source). No drift.
//   G-SUPPLIEDBY  — supplied-by(id) == dirname(canonical source) + "/".
//   G-HONESTY     — fs-gg-project's condition evaluates FALSE under {lifecycle=sdd} and TRUE under
//                   {lifecycle=spec-kit} (a minimal evaluator over the ==/&&/||/parens grammar).

open System
open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private manifestPath = repositoryPath "template/skill-manifest/skill-manifest.json"
let private templateJsonPath = repositoryPath ".template.config/template.json"

/// id -> canonical body source (mirrors scripts/generate-skill-manifest.fsx / Feature231).
let private canonicalSources =
    [ "fs-gg-elmish", "template/product-skills/fs-gg-elmish/SKILL.md"
      "fs-gg-feedback-capture", "template/feedback/skill/SKILL.md"
      "fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md"
      "fs-gg-keyboard-input", "template/product-skills/fs-gg-keyboard-input/SKILL.md"
      "fs-gg-layout", "template/product-skills/fs-gg-layout/SKILL.md"
      "fs-gg-project", "template/base/.agents/skills/fs-gg-project/SKILL.md"
      "fs-gg-samples", "template/fragments/samples/skill/SKILL.md"
      "fs-gg-scene", "template/product-skills/fs-gg-scene/SKILL.md"
      "fs-gg-skiaviewer", "template/product-skills/fs-gg-skiaviewer/SKILL.md"
      "fs-gg-styling", "template/product-skills/fs-gg-styling/SKILL.md"
      "fs-gg-symbology", "template/product-skills/fs-gg-symbology/SKILL.md"
      "fs-gg-testing", "template/product-skills/fs-gg-testing/SKILL.md"
      "fs-gg-ui-widgets", "template/product-skills/fs-gg-ui-widgets/SKILL.md" ]

type private ManifestEntry =
    { Id: string
      MaterializesWhen: string
      SuppliedBy: string }

let private jsonStr (e: JsonElement) : string =
    e.GetString() |> Option.ofObj |> Option.defaultValue ""

let private optStr (e: JsonElement) (prop: string) : string option =
    match e.TryGetProperty prop with
    | true, v -> v.GetString() |> Option.ofObj
    | _ -> None

let private readEntries () =
    Expect.isTrue (File.Exists manifestPath) (sprintf "manifest missing at %s — run dotnet fsi scripts/generate-skill-manifest.fsx" manifestPath)
    use doc = JsonDocument.Parse(File.ReadAllText manifestPath)
    [ for e in doc.RootElement.GetProperty("skills").EnumerateArray() ->
        { Id = jsonStr (e.GetProperty "id")
          MaterializesWhen = optStr e "materializes-when" |> Option.defaultValue ""
          SuppliedBy = optStr e "supplied-by" |> Option.defaultValue "" } ]

/// The authoritative id -> body-source condition map, read live from template.json:
/// product/samples/feedback rows target .agents/skills/<id>/; fs-gg-project's body ships via the
/// whole-tree template/base/.agents/ source (its condition is the gate for the one skill inside it).
let private templateConditions () : Map<string, string> =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    let str (s: JsonElement) (prop: string) =
        match s.TryGetProperty prop with
        | true, v -> v.GetString() |> Option.ofObj |> Option.defaultValue ""
        | _ -> ""
    [ for s in doc.RootElement.GetProperty("sources").EnumerateArray() do
        let target = (str s "target").Replace('\\', '/')
        let source = (str s "source").Replace('\\', '/')
        let cond = str s "condition"
        if target.StartsWith ".agents/skills/fs-gg-" then
            yield target.Substring(".agents/skills/".Length).TrimEnd('/'), cond
        if source = "template/base/.agents/" then
            yield "fs-gg-project", cond ]
    |> Map.ofList

/// Provider source dir (trailing slash) that holds the canonical SKILL.md — supplied-by's contract.
let private suppliedByOf (source: string) =
    let dir = source.Substring(0, source.LastIndexOf '/')
    dir + "/"

// ---- Minimal evaluator for the template.json condition grammar --------------------------------
// or := and ("||" and)* ; and := primary ("&&" primary)* ;
// primary := "(" or ")" | operand "==" operand ; operand := ident | "string" | true | false
// Operands resolve to strings: identifier -> params value; literal -> its text; bool -> "true"/"false".

let private tokenize (s: string) : string list =
    let tokens = System.Text.RegularExpressions.Regex.Matches(s, "\"[^\"]*\"|==|&&|\\|\\||\\(|\\)|[A-Za-z0-9_\\-]+")
    [ for m in tokens -> m.Value ]

let private evalCondition (parameters: Map<string, string>) (condition: string) : bool =
    let toks = tokenize condition |> List.toArray
    let mutable pos = 0
    let peek () = if pos < toks.Length then Some toks.[pos] else None
    let next () = let t = toks.[pos] in pos <- pos + 1; t
    let operandValue (t: string) =
        if t.StartsWith "\"" then t.Trim('"')
        elif t = "true" || t = "false" then t
        else parameters |> Map.tryFind t |> Option.defaultValue ("<unbound:" + t + ">")
    let rec parseOr () =
        let mutable v = parseAnd ()
        while peek () = Some "||" do (next () |> ignore); v <- (parseAnd ()) || v
        v
    and parseAnd () =
        let mutable v = parsePrimary ()
        while peek () = Some "&&" do (next () |> ignore); v <- (parsePrimary ()) && v
        v
    and parsePrimary () =
        match peek () with
        | Some "(" ->
            next () |> ignore
            let v = parseOr ()
            Expect.equal (next ()) ")" "balanced parens in condition"
            v
        | _ ->
            let lhs = operandValue (next ())
            Expect.equal (next ()) "==" "comparison operator == in condition"
            let rhs = operandValue (next ())
            lhs = rhs
    parseOr ()

[<Tests>]
let feature238SkillMaterializesWhenTests =
    testList
        "Feature238 skill-manifest materializes-when + supplied-by (issue #71 / ADR-0017)"
        [
          test "G-PRESENT every entry carries non-empty materializes-when + supplied-by" {
              let entries = readEntries ()
              Expect.equal (entries |> List.map (fun e -> e.Id) |> Set.ofList) (canonicalSources |> List.map fst |> Set.ofList) "manifest ids equal the declared catalog"
              for e in entries do
                  Expect.isNotEmpty e.MaterializesWhen (sprintf "%s: materializes-when present (additive field, FR-001)" e.Id)
                  Expect.isNotEmpty e.SuppliedBy (sprintf "%s: supplied-by present (additive field, FR-002)" e.Id)
          }

          test "G-CONDITION materializes-when equals the verbatim template.json body-source condition (no drift)" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              let conditions = templateConditions ()
              for id, _ in canonicalSources do
                  let expected =
                      match Map.tryFind id conditions with
                      | Some c -> c
                      | None -> failwithf "%s: no template.json body-source condition found (mapping gap)" id
                  Expect.equal (Map.find id entries).MaterializesWhen expected (sprintf "%s: materializes-when must equal the verbatim template.json condition — regenerate if template.json changed" id)
          }

          test "G-SUPPLIEDBY supplied-by equals dirname(canonical source) + '/'" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              for id, source in canonicalSources do
                  Expect.equal (Map.find id entries).SuppliedBy (suppliedByOf source) (sprintf "%s: supplied-by is the provider source dir" id)
          }

          test "G-HONESTY fs-gg-project materializes only in the spec-kit lane (false@sdd, true@spec-kit)" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              let cond = (Map.find "fs-gg-project" entries).MaterializesWhen
              // sdd lane params (new-sdd-fullstack default) — the skill legitimately does NOT emit.
              let sdd = Map.ofList [ "profile", "game"; "lifecycle", "sdd"; "feedback", "false" ]
              // spec-kit lane — the skill DOES emit (today's behaviour, unchanged).
              let specKit = Map.ofList [ "profile", "game"; "lifecycle", "spec-kit"; "feedback", "false" ]
              Expect.isFalse (evalCondition sdd cond) "fs-gg-project is legitimately absent under lifecycle=sdd (issue #71: not a [missing] supply failure)"
              Expect.isTrue (evalCondition specKit cond) "fs-gg-project materializes under lifecycle=spec-kit (unchanged emission behaviour)"
          }

          test "G-HONESTY the evaluator is faithful to the grammar (sanity over a profile-gated skill)" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              let scene = (Map.find "fs-gg-scene" entries).MaterializesWhen
              let app = Map.ofList [ "profile", "app"; "lifecycle", "spec-kit"; "feedback", "false" ]
              let headless = Map.ofList [ "profile", "headless-scene"; "lifecycle", "sdd"; "feedback", "false" ]
              let governedProfileMissing = Map.ofList [ "profile", "governed"; "lifecycle", "sdd"; "feedback", "false" ]
              Expect.isTrue (evalCondition app scene) "fs-gg-scene emits for profile=app"
              Expect.isTrue (evalCondition headless scene) "fs-gg-scene emits for profile=headless-scene"
              Expect.isTrue (evalCondition governedProfileMissing scene) "fs-gg-scene emits for profile=governed"
              let feedback = (Map.find "fs-gg-feedback-capture" entries).MaterializesWhen
              let feedbackOn = Map.ofList [ "profile", "app"; "lifecycle", "spec-kit"; "feedback", "true" ]
              let feedbackOff = Map.ofList [ "profile", "app"; "lifecycle", "spec-kit"; "feedback", "false" ]
              Expect.isTrue (evalCondition feedbackOn feedback) "fs-gg-feedback-capture emits when feedback=true && lifecycle=spec-kit"
              Expect.isFalse (evalCondition feedbackOff feedback) "fs-gg-feedback-capture suppressed when feedback=false"
          }
        ]
