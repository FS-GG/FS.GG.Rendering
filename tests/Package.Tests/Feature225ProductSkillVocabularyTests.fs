module Feature225ProductSkillVocabularyTests

// Feature 225 — the product-skill leak guard.
//
// The 7 shipped product skills (`template/product-skills/fs-gg-*/SKILL.md`) carry good bodies with
// leaky framing: framework-repo evidence process (`refresh-local-feed-and-samples`, `package-feed`,
// `specs/*/readiness/` + `.gitignore` allowlist, `BaseOutputPath`), unconditional
// `specs/<feature>/feedback/` references, and framework feature/spec-number stamps ("Feature 168",
// "feature 199/200", "spec-196"). This gate scans the WHOLE shipped product-skill set and fails the
// build if any of the three leak classes reappears, REUSING `SkillParity` discovery so it scans
// exactly the produced surface and cannot drift (contract:
// specs/225-deleak-product-skill-vocab/contracts/leak-guard-check.md).
//
// Pure over (skill, file, content): the scan is a total function on body text, so the synthetic
// inject/revert regression (FR-007 / SC-005) is exercised in-memory both directions. No new public
// surface — it consumes existing public discovery only (Principle II; mirrors Feature 224).

open System
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
open Rendering.Harness

let private repositoryRoot = RepositoryRoot.value

// The 7 product skills shipped today (research R0 / produced-surface T004). The guard scans whatever
// discovery actually finds; this set only backstops the "scan must not silently narrow" assertion —
// a regression that drops skills from the discovery surface is caught, not masked by a fixed list.
let private expectedProductSkillIds =
    set
        [ "fs-gg-elmish"
          "fs-gg-keyboard-input"
          "fs-gg-scene"
          "fs-gg-skiaviewer"
          "fs-gg-symbology"
          "fs-gg-testing"
          "fs-gg-ui-widgets" ]

// ---- Leak classes (contract: leak-guard-check.md) --------------------------------------------
type LeakClass =
    | ClassA // framework evidence process — banned outright
    | ClassB // lifecycle feedback path — conditional on a spec-kit gating phrase
    | ClassC // framework feature/spec-number stamp — banned outright

let private classToken =
    function
    | ClassA -> "A framework-evidence"
    | ClassB -> "B unconditional-feedback"
    | ClassC -> "C feature-number-stamp"

// Class A — any match is a finding (framework-repo-only evidence process). `\.gitignore` flags the
// allowlist instruction; `specs/.../readiness` is framework output, never a product-author location.
let private classAPatterns =
    [ "refresh-local-feed-and-samples"
      "package-feed"
      @"specs/[^\s`)]*?/readiness"
      @"\.gitignore"
      "BaseOutputPath" ]
    |> List.map (fun p -> Regex(p, RegexOptions.Compiled))

// Class B — conditional: a `specs/.../feedback` mention is a finding only when its enclosing
// paragraph carries no spec-kit gating phrase (research R2 / FR-002 keeps the gated path).
let private classBPattern = Regex(@"specs/[^\s`)]*?/feedback", RegexOptions.Compiled)

// Class C — any match is a finding. `spec-\d+` deliberately does NOT match `spec-kit`.
let private classCPatterns =
    [ Regex(@"[Ff]eature\s+\d+", RegexOptions.Compiled)
      Regex(@"spec-\d+", RegexOptions.Compiled) ]

// The gating-phrase set is a small named constant so it can be extended without reshaping the guard.
let private specKitGatingPhrases = [ "spec kit"; "spec-kit" ]

// ---- Findings (FR-007 / Principle VI: skill + class + token + file:line) -----------------------
type Finding =
    { Skill: string
      Class: LeakClass
      Token: string
      File: string
      Line: int }

let private formatFinding (f: Finding) =
    sprintf "  %s  [%s]  '%s'  %s:%d" f.Skill (classToken f.Class) f.Token f.File f.Line

let private formatFindings (findings: Finding list) =
    findings |> List.map formatFinding |> String.concat "\n"

// The blank-line-delimited paragraph containing a given 0-based line index.
let private paragraphAround (lines: string[]) (idx: int) =
    let isBlank (s: string) = String.IsNullOrWhiteSpace s
    let mutable lo = idx

    while lo > 0 && not (isBlank lines[lo - 1]) do
        lo <- lo - 1

    let mutable hi = idx

    while hi < lines.Length - 1 && not (isBlank lines[hi + 1]) do
        hi <- hi + 1

    String.Join("\n", lines[lo..hi])

let private hasGatingPhrase (paragraph: string) =
    let lower = paragraph.ToLowerInvariant()
    specKitGatingPhrases |> List.exists lower.Contains

/// Scan one skill body — a total function over (skill, file, content); one finding per matched token.
let private scanBody (skill: string) (file: string) (content: string) : Finding list =
    let lines = content.Replace("\r\n", "\n").Split('\n')

    [ for i in 0 .. lines.Length - 1 do
          let line = lines[i]
          let lineNo = i + 1

          // Class A — banned outright.
          for rx in classAPatterns do
              for m in rx.Matches line do
                  yield
                      { Skill = skill
                        Class = ClassA
                        Token = m.Value
                        File = file
                        Line = lineNo }

          // Class C — banned outright.
          for rx in classCPatterns do
              for m in rx.Matches line do
                  yield
                      { Skill = skill
                        Class = ClassC
                        Token = m.Value
                        File = file
                        Line = lineNo }

          // Class B — a finding only when the enclosing paragraph lacks a spec-kit gating phrase.
          for m in classBPattern.Matches line do
              if not (hasGatingPhrase (paragraphAround lines i)) then
                  yield
                      { Skill = skill
                        Class = ClassB
                        Token = m.Value
                        File = file
                        Line = lineNo } ]

// ---- Produced surface (SkillParity discovery) -------------------------------------------------
// Enumerate the shipped product skills the way the package carries them — the same authoritative
// enumerator parity + Feature 224 use, filtered to `template/product-skills`. Never a hardcoded list
// of 7 (so a later-added leaky skill is also scanned).
let private discoveredProductSkills () : SkillParity.SkillEntry list =
    let request = SkillParity.defaultRequest repositoryRoot
    let surfaces = SkillParity.discoverDefaultSurfaces repositoryRoot

    SkillParity.inventorySkills request surfaces
    |> List.filter (fun e -> e.Path.Replace('\\', '/').Contains "template/product-skills")

let private liveFindings () =
    discoveredProductSkills ()
    |> List.collect (fun e -> scanBody (e.SkillName.Trim()) (e.Path.Replace('\\', '/')) e.Content)

// ---- Synthetic bodies for the in-memory regression (Principle V) -------------------------------
// One token of each class — one `package-feed` (A), one ungated `specs/<feature>/feedback/` (B), one
// `feature 200` (C) — must yield exactly three findings, each naming its class and line.
let private syntheticLeakyBody =
    String.concat
        "\n"
        [ "# Synthetic skill"
          ""
          "Use the `package-feed` proof workflow to catch stale pins."
          ""
          "Record findings under `specs/<feature>/feedback/` for this product."
          ""
          "This capability landed in feature 200 of the framework." ]

// A properly gated feedback paragraph (the de-leaked phrasing) must pass — the spec-kit path survives.
let private gatedFeedbackBody =
    String.concat
        "\n"
        [ "## Persistent problems"
          ""
          "If your product uses Spec Kit, record findings under `specs/<feature>/feedback/`;"
          "otherwise record them in this skill's Sources / durable-lessons line." ]

[<Tests>]
let tests =
    testList
        "Feature225ProductSkillVocabulary"
        [ test "discovery surface did not narrow: the template/product-skills scan covers the 7 expected ids (FR-007 edge case)" {
              let discovered =
                  discoveredProductSkills ()
                  |> List.map (fun e -> e.SkillName.Trim())
                  |> Set.ofList

              let missing = Set.difference expectedProductSkillIds discovered

              Expect.isEmpty
                  missing
                  (sprintf
                      "the leak guard's discovery surface dropped expected product skill(s): %s — a fixed-list scan would have masked this"
                      (String.concat ", " missing))
          }

          test "no product skill leaks a framework token (real shipped set → zero findings; SC-001/002/003/005)" {
              let findings = liveFindings ()

              Expect.equal
                  findings
                  []
                  (sprintf
                      "product-skill leak guard FAILED (%d leak token(s)):\n%s"
                      findings.Length
                      (formatFindings findings))
          }

          test "synthetic inject: one token of each class → exactly three findings, one per class (SC-005 negative)" {
              let findings =
                  scanBody "fs-gg-synthetic" "template/product-skills/fs-gg-synthetic/SKILL.md" syntheticLeakyBody

              Expect.equal findings.Length 3 (sprintf "expected exactly three findings, got:\n%s" (formatFindings findings))

              let classes = findings |> List.map (fun f -> f.Class) |> Set.ofList
              Expect.equal classes (set [ ClassA; ClassB; ClassC ]) "one finding of each leak class"

              // Each finding names a real line and the matched token (FR-007 / Principle VI).
              for f in findings do
                  Expect.isGreaterThan f.Line 0 "finding names a 1-based line"
                  Expect.isFalse (String.IsNullOrWhiteSpace f.Token) "finding names the matched token"
          }

          test "conditional spec-kit feedback path is preserved: a gated paragraph passes (FR-002)" {
              let findings =
                  scanBody "fs-gg-gated" "template/product-skills/fs-gg-gated/SKILL.md" gatedFeedbackBody

              Expect.equal
                  findings
                  []
                  (sprintf
                      "a `specs/<feature>/feedback/` mention gated by a spec-kit phrase must NOT be a finding, got:\n%s"
                      (formatFindings findings))
          }

          test "ungated feedback is still caught while the same path gated passes (FR-002 both directions)" {
              let ungated =
                  scanBody "fs-gg-x" "f.md" "Record findings under `specs/<feature>/feedback/` always."

              Expect.equal ungated.Length 1 "an ungated feedback path is one Class-B finding"
              Expect.equal ungated.Head.Class ClassB "the ungated finding is Class B"

              let gated = scanBody "fs-gg-x" "f.md" gatedFeedbackBody
              Expect.isEmpty gated "the same path, gated by a spec-kit phrase, is clean"
          }

          test "finding message names skill + class + token + file:line, matching the contract shape (FR-007)" {
              let findings =
                  scanBody "fs-gg-synthetic" "template/product-skills/fs-gg-synthetic/SKILL.md" syntheticLeakyBody

              let classA = findings |> List.find (fun f -> f.Class = ClassA)
              let message = formatFinding classA
              Expect.stringContains message "fs-gg-synthetic" "message names the skill"
              Expect.stringContains message "package-feed" "message names the matched token"
              Expect.stringContains message "SKILL.md:" "message names file:line"
          } ]
