module FDs3VisualStateCountTests

// F-DS-3 (2026-07-15 review): the `Style.fsi` resolver doc-comment and the shipped design-system
// skill body both spell out how many `VisualState` cases the resolver is total over. That English
// count rotted (said "eight" while the DU carries nine — `FocusedHover` and `Validation` landed
// later). Per the review's meta-observation ("guards should assert derived == source, never
// text != known-bad-literal"), this gate does not pin nine: it DERIVES the count by reflecting
// over the `VisualState` union and asserts every prose spot spells that number, so adding or
// retiring a case forces the prose to move with it. Pure reflection + file read over the
// already-referenced DesignSystem assembly — no new project reference, no GL/display/network.
//
// Source of truth: `FS.GG.UI.DesignSystem.VisualState` (Types.DesignSystem.fs).
// Guarded prose — every hand-maintained copy of the count, so none can rot behind the others:
//   * `src/DesignSystem/Style.fsi` (`resolve` doc-comment)
//   * `src/DesignSystem/skill/SKILL.md` — the real skill body (the 12-line published
//     `.claude/skills/fs-gg-design-system/SKILL.md` is a pointer stub, not this body)
//   * `template/base/docs/api-surface/DesignSystem/Style.fsi` — the product-facing api-surface
//     MIRROR of the same doc-comment. The M-MIR mirror gate strips `//` comments before comparing,
//     so it would never catch this count drifting, and this .fsi is not in `inRepoExactCopies`;
//     without this line the mirror is an ungated third copy — exactly F-DS-3's failure class.

open System.IO
open System.Text.RegularExpressions
open FSharp.Reflection
open Expecto
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private styleFsiPath =
    Path.Combine(repositoryRoot, "src", "DesignSystem", "Style.fsi")

let private skillPath =
    Path.Combine(repositoryRoot, "src", "DesignSystem", "skill", "SKILL.md")

let private mirrorFsiPath =
    Path.Combine(repositoryRoot, "template", "base", "docs", "api-surface", "DesignSystem", "Style.fsi")

/// Every hand-maintained prose spot, paired with the marker regex whose one capture group is the
/// cardinal it currently spells. The two `.fsi`s share the resolver doc-comment phrasing; the skill
/// body wraps "all <N>\n  visual states" (flattened here to one line).
let private guardedProse =
    [ "Style.fsi", styleFsiPath, @"all (\w+) `VisualState` cases"
      "skill SKILL.md", skillPath, @"all (\w+) visual states"
      "api-surface mirror Style.fsi", mirrorFsiPath, @"all (\w+) `VisualState` cases" ]

/// The single source of truth: how many cases the `VisualState` DU actually carries.
let private visualStateCaseCount =
    FSharpType.GetUnionCases(typeof<VisualState>).Length

/// English cardinal for the small counts the DU can plausibly reach. Kept intentionally small so a
/// count outside this range fails loudly rather than silently comparing against an empty string.
let private cardinalWord (n: int) : string =
    [ 1, "one"; 2, "two"; 3, "three"; 4, "four"; 5, "five"; 6, "six"; 7, "seven"; 8, "eight"
      9, "nine"; 10, "ten"; 11, "eleven"; 12, "twelve" ]
    |> List.tryFind (fun (k, _) -> k = n)
    |> Option.map snd
    |> Option.defaultValue ""

/// Collapse all whitespace runs to single spaces so a prose phrase that wraps across a line
/// boundary (the skill's "all eight\n  visual states") reads as one flat string.
let private flatten (text: string) =
    Regex.Replace(text.Replace("\r\n", "\n").Replace("\r", "\n"), @"\s+", " ")

[<Tests>]
let fds3Tests =
    testList
        "F-DS-3 VisualState case count in prose"
        [ test "DU carries a plausible, spellable case count" {
              // Non-vacuous floor: reflection must have found the union and the count must be within
              // the cardinal table, else the assertions below would compare against "".
              Expect.isGreaterThan visualStateCaseCount 1 "VisualState should have several cases"
              Expect.notEqual (cardinalWord visualStateCaseCount) "" "case count must be spellable" }

          for label, path, marker in guardedProse do
              test $"{label} states the derived VisualState case count" {
                  let expected = cardinalWord visualStateCaseCount
                  // Locate the marker phrase regardless of the number it currently spells, so a stale
                  // count reds here instead of the phrase silently going missing.
                  let m = Regex.Match(flatten (File.ReadAllText path), marker)
                  Expect.isTrue m.Success (sprintf "%s must state the '%s' marker phrase" label marker)
                  Expect.equal
                      (m.Groups.[1].Value)
                      expected
                      (sprintf
                          "%s spells '%s' but the VisualState DU has %d cases (%s)"
                          label
                          m.Groups.[1].Value
                          visualStateCaseCount
                          expected) } ]
