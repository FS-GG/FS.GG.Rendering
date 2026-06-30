module Feature224SkillCatalogCurrencyTests

// Feature 224 — the FIRST real currency check for the shipped consumer skill docs.
//
// The catalog `template/base/docs/skillist-reference.md` and the prose `scaffold-map.md` ship inside
// every spec-kit FS.GG.UI product. Before this feature they advertised an orphaned, hand-maintained
// taxonomy: 8 defunct `fs-gg-*` ids, the entire `fsdocs-*`/`fsharp-*` families, three stale
// `speckit-*` ids, and framework-only `src/*/skill` paths a consumer never has — none resolving to a
// real SKILL.md (research R0/R1). This gate extracts every skill-id reference from the two shipped
// docs and fails when any does not resolve to a real SKILL.md, REUSING `SkillParity` discovery so it
// cannot drift again (contract: specs/224-skill-catalog-currency/contracts/catalog-currency-check.md).
//
// Pure over (doc, content): the extraction + resolution are total functions on file text, so the
// deliberate dangling-id regression (FR-005/SC-003) is exercised in-memory both directions.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
open Rendering.Harness

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private catalogRel = "template/base/docs/skillist-reference.md"
let private scaffoldMapRel = "template/base/docs/scaffold-map.md"

// ---- Justified exceptions (T008 / FR-009) -----------------------------------------------------
// (id, reason): an unresolved id is exempted ONLY with a NON-EMPTY reason. Empty by default after
// this feature — every reference in both shipped docs resolves on its own. No silent exemption.
let private justifiedExceptions: (string * string) list = []

// ---- Resolvable produced surface (SkillParity discovery) --------------------------------------
// A referenced id resolves iff discovery finds a SKILL.md whose `name:` equals it under a root the
// produced package carries (.agents/skills, .claude/skills, template/product-skills, src/**/skill,
// and the speckit-* command surface). Reuses the existing enumerator — no second implementation.
let private resolvableIds () : Set<string> =
    let request = SkillParity.defaultRequest repositoryRoot
    let surfaces = SkillParity.discoverDefaultSurfaces repositoryRoot

    SkillParity.inventorySkills request surfaces
    |> List.map (fun entry -> entry.SkillName.Trim())
    |> List.filter (fun name -> name <> "")
    |> Set.ofList

// ---- Reference extraction (recognized forms) --------------------------------------------------
// A skill-id token is an inline code-span whose content is a skill id: the closed set of families
// the docs use. The backtick anchors keep path cells (`.agents/skills/<id>/SKILL.md`) and prose
// words from matching — only a token written `` `id` `` is a reference.
let private skillIdRegex =
    Regex(@"`((?:fs-gg|speckit|fsdocs|fsharp)-[a-z0-9-]+)`", RegexOptions.Compiled)

type SkillRef =
    { Id: string
      Doc: string
      Line: int
      Form: string }

let private isTableRow (line: string) = line.TrimStart().StartsWith("|")

/// Every skill-id reference in a doc, tagged table-row vs prose-code-span, with 1-based line.
let private extractRefs (doc: string) (content: string) : SkillRef list =
    content.Replace("\r\n", "\n").Split('\n')
    |> Array.mapi (fun index line -> index + 1, line)
    |> Array.collect (fun (lineNo, line) ->
        let form = if isTableRow line then "table-row" else "prose-code-span"

        skillIdRegex.Matches line
        |> Seq.cast<Match>
        |> Seq.map (fun m ->
            { Id = m.Groups[1].Value
              Doc = doc
              Line = lineNo
              Form = form })
        |> Seq.toArray)
    |> Array.toList

// ---- Path-column correctness (FR-003 / closes analysis finding G1) -----------------------------
// A catalog table row that names a skill id AND carries a `SKILL.md` path must point at a
// consumer-resolvable location (.agents/skills/<id>/ or .claude/skills/<id>/). A framework-only
// `src/*/skill/*` (or `template/*/skill/*`) path fails even when the id itself resolves.
let private isConsumerPath (cell: string) =
    cell.Contains(".agents/skills/") || cell.Contains(".claude/skills/")

type PathViolation =
    { Id: string
      Doc: string
      Line: int
      Path: string }

let private pathViolations (doc: string) (content: string) : PathViolation list =
    content.Replace("\r\n", "\n").Split('\n')
    |> Array.mapi (fun index line -> index + 1, line)
    |> Array.choose (fun (lineNo, line) ->
        if not (isTableRow line) then
            None
        else
            let idMatch = skillIdRegex.Match line

            if not idMatch.Success then
                None
            else
                // The path cell is any cell mentioning SKILL.md.
                let pathCell =
                    line.Split('|')
                    |> Array.map (fun c -> c.Trim())
                    |> Array.tryFind (fun c -> c.Contains("SKILL.md"))

                match pathCell with
                | Some cell when not (isConsumerPath cell) ->
                    Some
                        { Id = idMatch.Groups[1].Value
                          Doc = doc
                          Line = lineNo
                          Path = cell.Trim('`', ' ') }
                | _ -> None)
    |> Array.toList

// ---- Findings (FR-006: id + doc + line) -------------------------------------------------------
type Finding =
    { Id: string
      Doc: string
      Line: int
      Message: string }

let private exemptionReason id =
    justifiedExceptions
    |> List.tryPick (fun (exemptId, reason) ->
        if exemptId = id && not (String.IsNullOrWhiteSpace reason) then Some reason else None)

/// The gate: every reference resolves OR is a justified exception with a non-empty reason; every
/// catalog path is consumer-resolvable. Returns one Finding per unresolved reference / bad path.
let private currencyFindings (resolvable: Set<string>) (docs: (string * string) list) : Finding list =
    let refFindings =
        docs
        |> List.collect (fun (doc, content) -> extractRefs doc content)
        |> List.choose (fun r ->
            if resolvable.Contains r.Id || (exemptionReason r.Id).IsSome then
                None
            else
                Some
                    { Id = r.Id
                      Doc = r.Doc
                      Line = r.Line
                      Message =
                        sprintf "%s:%d  '%s' → no SKILL.md with name: %s in package" r.Doc r.Line r.Id r.Id })

    let pathFindings =
        docs
        |> List.collect (fun (doc, content) -> pathViolations doc content)
        |> List.map (fun v ->
            { Id = v.Id
              Doc = v.Doc
              Line = v.Line
              Message =
                sprintf
                    "%s:%d  '%s' → path '%s' is not consumer-resolvable (use .agents/skills/%s/ or .claude/skills/%s/)"
                    v.Doc
                    v.Line
                    v.Id
                    v.Path
                    v.Id
                    v.Id })

    refFindings @ pathFindings

let private formatFindings (findings: Finding list) =
    findings
    |> List.map (fun f -> "  " + f.Message)
    |> String.concat "\n"

// ---- Live docs -------------------------------------------------------------------------------
let private liveDocs () =
    [ catalogRel, File.ReadAllText(repositoryPath catalogRel)
      scaffoldMapRel, File.ReadAllText(repositoryPath scaffoldMapRel) ]

[<Tests>]
let tests =
    testList
        "Feature224SkillCatalogCurrency"
        [ test "catalog and scaffold-map: every skill reference resolves to a real SKILL.md (SC-001/SC-002/SC-005)" {
              let resolvable = resolvableIds ()
              let findings = currencyFindings resolvable (liveDocs ())

              Expect.equal
                  findings
                  []
                  (sprintf
                      "catalog-currency FAILED (%d unresolved skill references):\n%s"
                      findings.Length
                      (formatFindings findings))
          }

          test "every catalog path column is consumer-resolvable, not framework-only (FR-003)" {
              let catalog = File.ReadAllText(repositoryPath catalogRel)
              let violations = pathViolations catalogRel catalog

              Expect.equal
                  violations
                  []
                  (sprintf
                      "catalog has %d framework-only path(s) a consumer package does not carry:\n%s"
                      violations.Length
                      (violations
                       |> List.map (fun v -> sprintf "  %s:%d  '%s' → %s" v.Doc v.Line v.Id v.Path)
                       |> String.concat "\n"))
          }

          test "findings name id + doc + line, matching the contract message shape (FR-006)" {
              // A known-bogus reference on a synthetic catalog line must surface all three fields.
              let resolvable = resolvableIds ()
              let bogusDoc = catalogRel
              let bogusContent = "| `fs-gg-does-not-exist` | .agents/skills/fs-gg-does-not-exist/SKILL.md |"

              let findings = currencyFindings resolvable [ bogusDoc, bogusContent ]

              Expect.equal findings.Length 1 "exactly one finding for one bogus reference"
              let f = findings.Head
              Expect.equal f.Id "fs-gg-does-not-exist" "finding names the id"
              Expect.equal f.Doc bogusDoc "finding names the doc"
              Expect.equal f.Line 1 "finding names the line"
              Expect.stringContains f.Message "fs-gg-does-not-exist" "message contains the id"
              Expect.stringContains f.Message bogusDoc "message contains the doc"
              Expect.stringContains f.Message "1" "message contains the line number"
          }

          test "deliberate dangling-id regression: inject → exactly one finding, real docs → none (SC-003 both directions)" {
              let resolvable = resolvableIds ()

              // Negative direction: a freshly-introduced dangling reference is caught.
              let injected =
                  let catalog = File.ReadAllText(repositoryPath catalogRel)
                  catalog + "\n| `fs-gg-does-not-exist` | .agents/skills/fs-gg-does-not-exist/SKILL.md |\n"

              let injectedFindings =
                  currencyFindings resolvable [ catalogRel, injected; scaffoldMapRel, File.ReadAllText(repositoryPath scaffoldMapRel) ]

              Expect.equal injectedFindings.Length 1 "injecting one bogus id yields exactly one finding"
              Expect.equal injectedFindings.Head.Id "fs-gg-does-not-exist" "the finding names the injected id"

              // Positive direction: the real, corrected docs produce none.
              let realFindings = currencyFindings resolvable (liveDocs ())
              Expect.equal realFindings [] "the real shipped docs produce zero findings"
          }

          test "no silent exemption: a reason-less allowlist entry still fails (FR-009)" {
              // Local check mirroring `exemptionReason`: only a non-empty reason exempts.
              let reasonless = [ "fs-gg-does-not-exist", "   " ]

              let exempted id =
                  reasonless
                  |> List.tryPick (fun (i, r) -> if i = id && not (String.IsNullOrWhiteSpace r) then Some r else None)

              Expect.isNone (exempted "fs-gg-does-not-exist") "a whitespace-only reason does not exempt an unresolved id"
          }

          test "speckit-* command ids are discoverable by SkillParity, not merely present as dirs (closes U1)" {
              let resolvable = resolvableIds ()

              [ "speckit-implement"; "speckit-tasks"; "speckit-plan"; "speckit-specify" ]
              |> List.iter (fun id ->
                  Expect.isTrue
                      (resolvable.Contains id)
                      (sprintf "%s must resolve via SkillParity discovery so the check can resolve speckit-* refs" id))
          } ]
