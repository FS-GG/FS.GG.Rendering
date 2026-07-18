module Feature238SkillMaterializesWhenTests

// Feature 238 (issue #71, ADR-0017 P1 Rendering) — the product skill-manifest records, per entry,
// WHEN its body materializes and WHERE the body comes from:
//   materializes-when — the predicate that gates the skill's body.
//   supplied-by       — the provider source directory holding the canonical SKILL.md.
//
// The original point was fs-gg-project: declared scope:product but emitted only under
// lifecycle == spec-kit, so under the sdd lane it was declared-but-unsupplied (ADR-0017 §C2, a
// tolerated gap). Issue #91 CLOSED that seam: fs-gg-project is the product-orientation umbrella and
// its body is lane-neutral, so it now ships via a dedicated profile-gated (lifecycle-agnostic)
// source like every other product skill — materializing on the sdd/none lanes too. materializes-when
// still lets the union gate (.github#164) tell legitimate profile-scoped suppression from a real
// [missing], and G-HONESTY below now pins the widened (all-lifecycle) behaviour.
//
// GRAMMAR (issue #77) — the manifest ships materializes-when in the ADR-0017 CANONICAL grammar the
// skill-union gate (.github scripts/skill-union-assert.sh) + the typed Fsgg.Registry validator
// evaluate:  always | <clause> [ (and|or) <clause> ]...  where a clause is  <param> == <value> |
// <param> != <value> | <param> in [<v>, ...].  No parens, bare unquoted tokens, `and`/`or` (not
// `&&`/`||`).  template.json keeps the C-style grammar the `dotnet new` engine requires; the
// generator's normalizeCondition is the bridge.  Shipping the raw C-style predicate made the gate
// read the whole thing as one always-false clause and red every real scaffold (issue #77).
//
// Guards (deterministic, GL-free, no `dotnet new`):
//   G-PRESENT     — every entry carries non-empty materializes-when + supplied-by.
//   G-GRAMMAR     — every materializes-when is in the canonical grammar (no `&&`/`||`/parens/quotes)
//                   and parses under the tiny gate evaluator.
//   G-EQUIV       — materializes-when(id) is SEMANTICALLY EQUAL to the verbatim template.json
//                   body-source condition: both evaluate identically over the full parameter grid.
//                   This ties manifest → template.json without pinning the exact normalized string,
//                   so template.json stays the single source of truth and any un-anticipated
//                   condition shape (that normalizeCondition mistranslates) fails here.
//   G-SUPPLIEDBY  — supplied-by(id) == dirname(canonical source) + "/".
//   G-HONESTY     — fs-gg-project's condition evaluates TRUE for a composed profile under EVERY
//                   lifecycle (sdd/spec-kit/none — issue #91 closed the §C2 seam) and FALSE for an
//                   off-list profile, evaluated with the canonical gate evaluator.

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
    [ "fs-gg-audio", "template/product-skills/fs-gg-audio/SKILL.md"
      "fs-gg-collision", "template/product-skills/fs-gg-collision/SKILL.md"
      "fs-gg-elmish", "template/product-skills/fs-gg-elmish/SKILL.md"
      "fs-gg-feedback-capture", "template/feedback/skill/SKILL.md"
      "fs-gg-feedback-report", "template/feedback-report/skill/SKILL.md"
      "fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md"
      "fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md"
      "fs-gg-keyboard-input", "template/product-skills/fs-gg-keyboard-input/SKILL.md"
      "fs-gg-layout", "template/product-skills/fs-gg-layout/SKILL.md"
      "fs-gg-line-drawing", "template/product-skills/fs-gg-line-drawing/SKILL.md"
      "fs-gg-model-swap", "template/product-skills/fs-gg-model-swap/SKILL.md"
      "fs-gg-persistence", "template/product-skills/fs-gg-persistence/SKILL.md"
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

/// The authoritative id -> body-source condition map, read live from template.json (C-style grammar):
/// every product skill — fs-gg-project included (issue #91) — ships via a dedicated source targeting
/// .agents/skills/<id>/, so a single target-prefix scan recovers the whole catalog's conditions.
let private templateConditions () : Map<string, string> =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    let str (s: JsonElement) (prop: string) =
        match s.TryGetProperty prop with
        | true, v -> v.GetString() |> Option.ofObj |> Option.defaultValue ""
        | _ -> ""
    [ for s in doc.RootElement.GetProperty("sources").EnumerateArray() do
        let target = (str s "target").Replace('\\', '/')
        let cond = str s "condition"
        if target.StartsWith ".agents/skills/fs-gg-" then
            yield target.Substring(".agents/skills/".Length).TrimEnd('/'), cond ]
    |> Map.ofList

/// Provider source dir (trailing slash) that holds the canonical SKILL.md — supplied-by's contract.
let private suppliedByOf (source: string) =
    let dir = source.Substring(0, source.LastIndexOf '/')
    dir + "/"

// ---- Evaluator for the template.json C-style condition grammar --------------------------------
// or := and ("||" and)* ; and := primary ("&&" primary)* ;
// primary := "(" or ")" | operand "==" operand ; operand := ident | "string" | true | false
// Operands resolve to strings: identifier -> params value; literal -> its text; bool -> "true"/"false".

let private tokenizeCStyle (s: string) : string list =
    let tokens = System.Text.RegularExpressions.Regex.Matches(s, "\"[^\"]*\"|==|&&|\\|\\||\\(|\\)|[A-Za-z0-9_\\-]+")
    [ for m in tokens -> m.Value ]

let private evalCStyleParsed (parameters: Map<string, string>) (condition: string) : bool =
    let toks = tokenizeCStyle condition |> List.toArray
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

/// #434: a `sources` row with no `condition` fires on EVERY scaffold — the engine's native
/// "unconditional", which the canonical grammar spells `always`. `templateConditions` surfaces such
/// a row as the empty string, which tokenizes to nothing and would index past the end of `toks`.
/// Modelling it as `true` is what makes `always` ≡ no-condition provable by G-EQUIV rather than asserted.
let private evalCStyle (parameters: Map<string, string>) (condition: string) : bool =
    if System.String.IsNullOrWhiteSpace condition then true
    else evalCStyleParsed parameters condition

// ---- Evaluator for the ADR-0017 CANONICAL grammar (mirror of skill-union-assert.sh) -----------
// condition := clause [ (" and " | " or ") clause ]...  ; `and` binds tighter than `or`.
// clause := "always" | "true" | "false" | <param> " in [" <v>, ... "]" | <param> "!=" <v> | <param> "==" <v>
// A param absent from the map reads as "" so `== v` is false and `!= v` is true (gate semantics).

let private evalCanonicalClause (parameters: Map<string, string>) (clause: string) : bool =
    let c = clause.Trim()
    let paramValue key = parameters |> Map.tryFind key |> Option.defaultValue ""
    if c = "always" || c = "true" then true
    elif c = "false" then false
    elif c.Contains " in [" then
        let key = c.Substring(0, c.IndexOf " in [").Trim()
        let openBracket = c.IndexOf '['
        let closeBracket = c.LastIndexOf ']'
        Expect.isTrue (openBracket >= 0 && closeBracket > openBracket) (sprintf "balanced [] in canonical clause: '%s'" c)
        let items =
            c.Substring(openBracket + 1, closeBracket - openBracket - 1).Split(',')
            |> Array.map (fun s -> s.Trim())
            |> Array.filter (fun s -> s <> "")
        Array.contains (paramValue key) items
    elif c.Contains "!=" then
        let i = c.IndexOf "!="
        (paramValue (c.Substring(0, i).Trim())) <> c.Substring(i + 2).Trim()
    elif c.Contains "==" then
        let i = c.IndexOf "=="
        (paramValue (c.Substring(0, i).Trim())) = c.Substring(i + 2).Trim()
    else
        failtestf "unparseable canonical materializes-when clause: '%s'" c

let private evalCanonical (parameters: Map<string, string>) (condition: string) : bool =
    condition.Split([| " or " |], StringSplitOptions.None)
    |> Array.exists (fun conjunction ->
        conjunction.Split([| " and " |], StringSplitOptions.None)
        |> Array.forall (evalCanonicalClause parameters))

/// The full scaffold-parameter grid the two evaluators are compared over. Covers every value the
/// live conditions branch on plus an off-lane sentinel per param, so agreement across the grid is
/// true semantic equivalence for these predicates.
let private parameterGrid : Map<string, string> list =
    [ for profile in [ "app"; "headless-scene"; "governed"; "sample-pack"; "game"; "controls" ] do
        for lifecycle in [ "spec-kit"; "sdd"; "none" ] do
            for feedback in [ "true"; "false" ] do
                yield Map.ofList [ "profile", profile; "lifecycle", lifecycle; "feedback", feedback ] ]

[<Tests>]
let feature238SkillMaterializesWhenTests =
    testList
        "Feature238 skill-manifest materializes-when + supplied-by (issue #71 / #77 / ADR-0017)"
        [
          test "G-PRESENT every entry carries non-empty materializes-when + supplied-by" {
              let entries = readEntries ()
              Expect.equal (entries |> List.map (fun e -> e.Id) |> Set.ofList) (canonicalSources |> List.map fst |> Set.ofList) "manifest ids equal the declared catalog"
              for e in entries do
                  Expect.isNotEmpty e.MaterializesWhen (sprintf "%s: materializes-when present (additive field, FR-001)" e.Id)
                  Expect.isNotEmpty e.SuppliedBy (sprintf "%s: supplied-by present (additive field, FR-002)" e.Id)
          }

          test "G-GRAMMAR materializes-when is in the ADR-0017 canonical grammar (no C-style tokens)" {
              // issue #77: the gate evaluator does not recognise `&&`/`||`/parens/quoted literals — a
              // predicate carrying any of them reads as one always-false clause and reds every scaffold.
              for e in readEntries () do
                  for banned in [ "&&"; "||"; "("; ")"; "\"" ] do
                      Expect.isFalse (e.MaterializesWhen.Contains banned) (sprintf "%s: materializes-when '%s' must not contain %s (not canonical grammar)" e.Id e.MaterializesWhen banned)
                  // and it must parse+evaluate under the tiny gate evaluator for every grid point.
                  for parameters in parameterGrid do
                      evalCanonical parameters e.MaterializesWhen |> ignore
          }

          test "G-GRID the parameter grid covers every identifier the conditions branch on (keeps G-EQUIV honest)" {
              // G-EQUIV is only a real semantic-equivalence check while every param a condition
              // references is varied by the grid: an ungridded param reads as the non-matching
              // sentinel in BOTH evaluators at every point, so they agree vacuously and a
              // mistranslation involving it would pass green. Enforce grid ⊇ condition-identifiers so
              // adding a new scaffold param to a condition forces extending parameterGrid.
              let gridKeys = parameterGrid |> List.collect (Map.toList >> List.map fst) |> Set.ofList
              let identifierPattern = System.Text.RegularExpressions.Regex "^[A-Za-z][A-Za-z0-9_-]*$"
              let conditionIdentifiers =
                  templateConditions ()
                  |> Map.toList
                  |> List.collect (snd >> tokenizeCStyle)                       // quoted literals stay quoted → excluded
                  |> List.filter (fun t -> identifierPattern.IsMatch t && t <> "true" && t <> "false")
                  |> Set.ofList
              Expect.isEmpty
                  (Set.difference conditionIdentifiers gridKeys)
                  (sprintf "parameterGrid must vary every param the conditions use; add the missing one(s) to keep G-EQUIV a real check (grid=%A, conditions use=%A)" gridKeys conditionIdentifiers)
          }

          test "G-EQUIV materializes-when is semantically equal to the verbatim template.json condition" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              let conditions = templateConditions ()
              for id, _ in canonicalSources do
                  let cStyle =
                      match Map.tryFind id conditions with
                      | Some c -> c
                      | None -> failwithf "%s: no template.json body-source condition found (mapping gap)" id
                  let canonical = (Map.find id entries).MaterializesWhen
                  for parameters in parameterGrid do
                      Expect.equal
                          (evalCanonical parameters canonical)
                          (evalCStyle parameters cStyle)
                          (sprintf "%s: canonical '%s' must mean the same as template.json '%s' at %A — regenerate the manifest if template.json changed" id canonical cStyle parameters)
          }

          test "G-SUPPLIEDBY supplied-by equals dirname(canonical source) + '/'" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              for id, source in canonicalSources do
                  Expect.equal (Map.find id entries).SuppliedBy (suppliedByOf source) (sprintf "%s: supplied-by is the provider source dir" id)
          }

          test "G-HONESTY fs-gg-project materializes on every lifecycle for a composed profile (issue #91: §C2 seam closed)" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              let cond = (Map.find "fs-gg-project" entries).MaterializesWhen
              // Issue #91 / ADR-0017 §C2: the product-orientation umbrella is now lifecycle-agnostic —
              // it covers the DEFAULT sdd lane (was the tolerated gap) as well as spec-kit and none.
              for lifecycle in [ "sdd"; "spec-kit"; "none" ] do
                  let lane = Map.ofList [ "profile", "game"; "lifecycle", lifecycle; "feedback", "false" ]
                  Expect.isTrue (evalCanonical lane cond) (sprintf "fs-gg-project materializes for profile=game under lifecycle=%s (§C2 seam closed — every composed product gets the top-level map)" lifecycle)
              // Still profile-gated: an off-list profile (grid sentinel) does not get it.
              let offProfile = Map.ofList [ "profile", "controls"; "lifecycle", "sdd"; "feedback", "false" ]
              Expect.isFalse (evalCanonical offProfile cond) "fs-gg-project suppressed for an off-list profile (condition is profile-scoped, not `always`)"
          }

          test "G-HONESTY the canonical evaluator is faithful to the grammar (sanity over gated skills)" {
              let entries = readEntries () |> List.map (fun e -> e.Id, e) |> Map.ofList
              let scene = (Map.find "fs-gg-scene" entries).MaterializesWhen
              let app = Map.ofList [ "profile", "app"; "lifecycle", "spec-kit"; "feedback", "false" ]
              let headless = Map.ofList [ "profile", "headless-scene"; "lifecycle", "sdd"; "feedback", "false" ]
              let governed = Map.ofList [ "profile", "governed"; "lifecycle", "sdd"; "feedback", "false" ]
              let controls = Map.ofList [ "profile", "controls"; "lifecycle", "sdd"; "feedback", "false" ]
              Expect.isTrue (evalCanonical app scene) "fs-gg-scene emits for profile=app (in [..] membership)"
              Expect.isTrue (evalCanonical headless scene) "fs-gg-scene emits for profile=headless-scene"
              Expect.isTrue (evalCanonical governed scene) "fs-gg-scene emits for profile=governed"
              Expect.isFalse (evalCanonical controls scene) "fs-gg-scene suppressed for an off-list profile"
              let feedback = (Map.find "fs-gg-feedback-capture" entries).MaterializesWhen
              let feedbackOn = Map.ofList [ "profile", "app"; "lifecycle", "spec-kit"; "feedback", "true" ]
              let feedbackOff = Map.ofList [ "profile", "app"; "lifecycle", "spec-kit"; "feedback", "false" ]
              Expect.isTrue (evalCanonical feedbackOn feedback) "fs-gg-feedback-capture emits when feedback == true and lifecycle == spec-kit"
              Expect.isFalse (evalCanonical feedbackOff feedback) "fs-gg-feedback-capture suppressed when feedback == false (conjunction)"

              // Issue #248 / #434: the report is the UNCONDITIONAL counterpart of the capture skill.
              // Capture's `(feedback == true) && lifecycle == spec-kit` gate is load-bearing (it is Spec
              // Kit hook machinery); the report is agent-invoked at cycle end and reads only optional,
              // guarded evidence, so it must emit on EVERY lane — including one that captured none.
              // #434: it was `feedback == true`, and since `feedback` defaults to false that shipped it
              // to NOBODY. Re-adding ANY clause — lifecycle, profile, or the old capability flag — is the
              // regression this asserts against, so evaluate it over the WHOLE grid rather than a sample.
              let report = (Map.find "fs-gg-feedback-report" entries).MaterializesWhen
              Expect.equal report "always" "fs-gg-feedback-report is unconditional (#434)"
              for parameters in parameterGrid do
                  Expect.isTrue
                      (evalCanonical parameters report)
                      (sprintf "fs-gg-feedback-report materializes at EVERY grid point — including feedback=false, every lifecycle, and an off-list profile (#434); failed at %A" parameters)
          }
        ]
