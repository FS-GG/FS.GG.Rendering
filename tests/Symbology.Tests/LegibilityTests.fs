module Symbology.Tests.LegibilityTests

// Feature 194 (M7 — legibility linter) semantic tests over the public `Legibility` surface.
// Each test maps to a behavioural-contract row (C1–C12, C14) in contracts/legibility-api.md.
// Fail-before (against the T005 stub) / pass-after the implementation (Constitution I/V).
// Only the public `Legibility` surface is exercised — no internals.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

/// A clean, fully in-domain baseline unit.
let private baseUnit = Symbology.defaultToken

/// Distinct `Custom` faction by index (each colour counted separately, research D2).
let private customFaction i =
    Custom { Red = byte i; Green = 0uy; Blue = 0uy; Alpha = 255uy }

/// Distinct `Mark` sigil by index (distinct PathSpec value).
let private markSigil i =
    Sigil.Mark
        { Commands = [ PathCommand.MoveTo { X = float i; Y = 0.0 } ]
          FillType = PathFillType.Winding }

let private usageOf (report: Legibility.Report) (channel: Legibility.Channel) =
    report.Usage |> List.find (fun u -> u.Channel = channel)

[<Tests>]
let tests =
    testList
        "Legibility"
        [
          // ── C1 / C10 / C9 — clean / all-identical / empty ────────────────────────────────
          test "C1 — a within-capacity board → Findings = [], Verdict = Clean" {
              let board =
                  [ { baseUnit with Faction = Ally; Klass = Heavy; Sigil = Ring; Speed = 1 }
                    { baseUnit with Faction = Enemy; Klass = Scout; Sigil = Fang; Speed = 2 }
                    { baseUnit with Faction = Neutral; Klass = Mobile; Sigil = Bolt; Speed = 3 } ]

              let report = Legibility.score board
              Expect.equal report.Findings [] "a within-capacity board yields no findings"
              Expect.equal report.Verdict Legibility.Clean "verdict is Clean"
          }

          test "C10 — all-identical roster → Clean, each categorical channel DistinctLevels = 1" {
              let board = List.replicate 5 baseUnit
              let report = Legibility.score board
              Expect.equal report.Verdict Legibility.Clean "all-identical is Clean"

              for ch in [ Legibility.Faction; Legibility.Klass; Legibility.Sigil; Legibility.State; Legibility.Shield ] do
                  Expect.equal (usageOf report ch).DistinctLevels 1 (sprintf "%A reports exactly one distinct level" ch)
          }

          test "C9 — empty score [] / scoreAnimated [] → Findings = [], Clean, usage all 0" {
              let r = Legibility.score []
              Expect.equal r.Findings [] "empty board: no findings"
              Expect.equal r.Verdict Legibility.Clean "empty board: Clean"
              Expect.equal r.Usage.Length 11 "usage has one entry per per-unit channel"
              Expect.isTrue (r.Usage |> List.forall (fun u -> u.DistinctLevels = 0)) "all usage at 0 distinct levels"

              let ra = Legibility.scoreAnimated []
              Expect.equal ra.Findings [] "empty animated board: no findings"
              Expect.equal ra.Verdict Legibility.Clean "empty animated board: Clean"
          }

          // ── C2 — categorical overload (one Warning per over-capacity channel) ─────────────
          test "C2 — > 7 distinct factions → exactly one Warning on Faction; in-capacity channels silent" {
              let board = [ for i in 0..7 -> { baseUnit with Faction = customFaction i } ] // 8 distinct
              let report = Legibility.score board
              Expect.equal report.Findings.Length 1 "exactly one finding"
              let f = report.Findings.Head
              Expect.equal f.Channel Legibility.Faction "finding is on Faction"
              Expect.equal f.Severity Legibility.Warning "overload is a Warning"
              Expect.stringContains f.Message "8" "message reports the used count"
              Expect.stringContains f.Message "7" "message reports the capacity"
              Expect.isNonEmpty f.Units "the contributing units are named"
              Expect.equal report.Verdict Legibility.HasWarnings "verdict is HasWarnings"
          }

          test "C2 — > 12 distinct sigils → exactly one Warning on Sigil" {
              // Bolt, Ring, Fang (3) + 10 distinct Marks = 13 distinct > capacity 12.
              let board =
                  [ yield { baseUnit with Sigil = Bolt }
                    yield { baseUnit with Sigil = Ring }
                    yield { baseUnit with Sigil = Fang }
                    for i in 0..9 -> { baseUnit with Sigil = markSigil i } ]

              let report = Legibility.score board
              Expect.equal report.Findings.Length 1 "exactly one finding"
              let f = report.Findings.Head
              Expect.equal f.Channel Legibility.Sigil "finding is on Sigil"
              Expect.equal f.Severity Legibility.Warning "overload is a Warning"
              Expect.stringContains f.Message "13" "message reports 13 distinct"
          }

          // ── C3 — Speed (Ordered) overload ────────────────────────────────────────────────
          test "C3 — > 4 distinct Speed bead counts → one Warning on Speed naming the units" {
              let board = [ for s in 0..4 -> { baseUnit with Speed = s } ] // 5 distinct, all in [0,6]
              let report = Legibility.score board
              Expect.equal report.Findings.Length 1 "exactly one finding"
              let f = report.Findings.Head
              Expect.equal f.Channel Legibility.Speed "finding is on Speed"
              Expect.equal f.Severity Legibility.Warning "ordered overload is a Warning"
              Expect.isNonEmpty f.Units "units are named"
          }

          // ── C4 / C5 / C6 — out-of-domain / degenerate / non-finite (Errors, scan continues) ─
          test "C4 — Threat/Charge/Health outside [0,1] or Speed outside [0,6] → Error on that channel" {
              let report =
                  Legibility.score
                      [ { baseUnit with Threat = 2.0 } // unit 0: Threat out of band
                        baseUnit // unit 1: clean (scan must continue)
                        { baseUnit with Speed = 7 } ] // unit 2: Speed out of band

              let threat = report.Findings |> List.filter (fun f -> f.Channel = Legibility.Threat)
              Expect.equal threat.Length 1 "one Threat error"
              Expect.equal threat.Head.Severity Legibility.Error "Threat out-of-domain is an Error"
              Expect.equal threat.Head.Units [ 0 ] "names the offending unit"

              let speed = report.Findings |> List.filter (fun f -> f.Channel = Legibility.Speed)
              Expect.equal speed.Length 1 "one Speed error"
              Expect.equal speed.Head.Severity Legibility.Error "Speed out-of-domain is an Error"
              Expect.equal speed.Head.Units [ 2 ] "names the offending unit (scan continued past unit 0)"
          }

          test "C5 — R <= 0 degenerate → one Error on Size; remaining units still scored" {
              let report =
                  Legibility.score
                      [ { baseUnit with R = 0.0 } // degenerate
                        { baseUnit with Threat = 5.0 } ] // still scored after the degenerate

              let size = report.Findings |> List.filter (fun f -> f.Channel = Legibility.Size)
              Expect.equal size.Length 1 "one Size error"
              Expect.equal size.Head.Severity Legibility.Error "degenerate is an Error"
              Expect.equal size.Head.Units [ 0 ] "names the degenerate unit"

              let threat = report.Findings |> List.filter (fun f -> f.Channel = Legibility.Threat)
              Expect.equal threat.Length 1 "the unit after the degenerate is still scored"
              Expect.equal threat.Head.Units [ 1 ] "the second unit's out-of-domain Threat is reported"
          }

          test "C6 — non-finite float on any field → one Error on that channel, no exception" {
              let report =
                  Legibility.score
                      [ { baseUnit with Threat = nan }
                        { baseUnit with R = infinity }
                        { baseUnit with Heading = -infinity } ]

              Expect.equal
                  (report.Findings |> List.filter (fun f -> f.Channel = Legibility.Threat) |> List.length)
                  1
                  "NaN Threat → one Error on Threat"
              Expect.equal
                  (report.Findings |> List.filter (fun f -> f.Channel = Legibility.Size) |> List.length)
                  1
                  "infinite R → one Error on Size"
              Expect.equal
                  (report.Findings |> List.filter (fun f -> f.Channel = Legibility.Heading) |> List.length)
                  1
                  "non-finite Heading → one Error on Heading"
              Expect.isTrue (report.Findings |> List.forall (fun f -> f.Severity = Legibility.Error)) "all are Errors"
          }

          // ── C9 (continuous exempt) — many distinct continuous values, no overload ─────────
          test "C9-continuous-exempt — many distinct continuous values emit no overload (FR-009)" {
              // 20 units, identical categoricals/speed, but every continuous channel distinct & in-domain.
              let board =
                  [ for i in 0..19 ->
                        { baseUnit with
                            R = 10.0 + float i
                            Threat = float i / 20.0
                            Charge = float i / 25.0
                            Health = float i / 30.0
                            Heading = float i * 13.0 } ]

              let report = Legibility.score board
              Expect.equal report.Findings [] "continuous channels are overload-exempt — no findings"
              Expect.equal report.Verdict Legibility.Clean "Clean despite 20 distinct continuous values"
          }

          // ── C11 / C12 — whole-board motion load ──────────────────────────────────────────
          test "C11 — > 1 distinct non-Idle rhythm → one Warning on Motion, Units = []" {
              let report = Legibility.scoreAnimated [ (Pulse, baseUnit); (Spin, baseUnit) ]
              let motion = report.Findings |> List.filter (fun f -> f.Channel = Legibility.Motion)
              Expect.equal motion.Length 1 "one Motion finding"
              Expect.equal motion.Head.Severity Legibility.Warning "motion load is a Warning"
              Expect.equal motion.Head.Units [] "a whole-board finding names no single unit"
          }

          test "C12 — one rhythm across many moving units → no Motion finding" {
              let report = Legibility.scoreAnimated [ (Moving, baseUnit); (Moving, baseUnit); (Moving, baseUnit) ]
              Expect.isEmpty
                  (report.Findings |> List.filter (fun f -> f.Channel = Legibility.Motion))
                  "a single rhythm is not a stack"

              // Idle does not count as an active rhythm: Idle + one rhythm is still a single beat.
              let withIdle = Legibility.scoreAnimated [ (Idle, baseUnit); (Pulse, baseUnit); (Pulse, baseUnit) ]
              Expect.isEmpty
                  (withIdle.Findings |> List.filter (fun f -> f.Channel = Legibility.Motion))
                  "Idle is not an active rhythm"
          }

          // ── C7 / C8 / C14 — determinism / machine-actionable / advisory ──────────────────
          test "C7 — determinism: score s = score s (structural equality)" {
              let board = [ for i in 0..7 -> { baseUnit with Faction = customFaction i; Speed = i % 3 } ]
              Expect.equal (Legibility.score board) (Legibility.score board) "two scorings are structurally equal"
          }

          test "C8 — machine-actionable: filter by Channel + Severity, derive Verdict without parsing text" {
              let board =
                  [ for i in 0..7 -> { baseUnit with Faction = customFaction i } ] @ [ { baseUnit with Threat = 9.0 } ]

              let report = Legibility.score board
              let warnings = report.Findings |> List.filter (fun f -> f.Severity = Legibility.Warning)
              let errors = report.Findings |> List.filter (fun f -> f.Severity = Legibility.Error)
              Expect.isNonEmpty warnings "the faction overload is filterable as a Warning"
              Expect.isNonEmpty errors "the out-of-domain Threat is filterable as an Error"
              Expect.isTrue (warnings |> List.exists (fun f -> f.Channel = Legibility.Faction)) "Warning on Faction by identity"

              let derived = if report.Findings.IsEmpty then Legibility.Clean else Legibility.HasWarnings
              Expect.equal report.Verdict derived "Verdict is derivable from Findings without reading Message"
          }

          test "C14 — advisory: a valid-but-overloaded set returns a report, never throws" {
              let board = [ for i in 0..9 -> { baseUnit with Faction = customFaction i } ]
              let report = Legibility.score board // must not raise
              Expect.equal report.Verdict Legibility.HasWarnings "an overloaded set scores HasWarnings, not an exception"
              Expect.isNonEmpty report.Findings "the overload is reported as data"
          } ]

// T024 [US3] Label is inspection-detail, NOT a pre-attentive channel (FR-011/SC-006). Adding labels to a
// roster must NOT change the linter's `Report` — the label is not in the capacity table, so the verdict and
// every channel-usage count are identical with and without labels. (`Legibility.score` takes `Token list`
// and has no grammar parameter, so the verdict is grammar-independent by construction.)
[<Tests>]
let labelInvariance =
    let roster =
        [ { baseUnit with Faction = Ally; Klass = Heavy; Sigil = Ring; Speed = 1 }
          { baseUnit with Faction = Enemy; Klass = Scout; Sigil = Fang; Speed = 2 }
          { baseUnit with Faction = Neutral; Klass = Mobile; Sigil = Bolt; Speed = 3 } ]

    let labelled =
        roster |> List.mapi (fun i t -> { t with Label = Some(sprintf "U-%d" i) })

    testList
        "US3 label linter-invariance"
        [ test "label presence does not change the roster's Report (score)" {
              Expect.equal (Legibility.score labelled) (Legibility.score roster) "the label is inspection-detail; it does not enter pop-out governance (FR-011)"
          }

          test "label presence does not change the animated Report (scoreAnimated)" {
              let board = roster |> List.map (fun t -> Pulse, t)
              let boardL = labelled |> List.map (fun t -> Pulse, t)
              Expect.equal (Legibility.scoreAnimated boardL) (Legibility.scoreAnimated board) "labels do not alter animated governance (FR-011)"
          }

          test "even a roster of all-identical labels leaves the verdict unchanged" {
              let sameLabel = roster |> List.map (fun t -> { t with Label = Some "SAME" })
              Expect.equal (Legibility.score sameLabel) (Legibility.score roster) "the label channel is never governed (SC-006)"
          }

          // T015 [US3] MULTI-LINE labels are inspection-detail too (FR-011/SC-006): a roster carrying
          // `\n`-bearing / over-budget labels yields an IDENTICAL Report to the same roster with no labels —
          // multi-line content never enters the capacity table, so the verdict is unchanged and (since
          // `score` takes `Token list` with no grammar parameter) grammar-independent by construction.
          test "multi-line label presence does not change the roster's Report (score)" {
              let multiline = roster |> List.mapi (fun i t -> { t with Label = Some(sprintf "U-%d\nLINE-B\nLINE-C\nLINE-D" i) })
              Expect.equal (Legibility.score multiline) (Legibility.score roster) "a multi-line label is inspection-detail; it does not enter governance (FR-011)"
          }

          test "multi-line label presence does not change the animated Report (scoreAnimated)" {
              let multiline = roster |> List.mapi (fun i t -> { t with Label = Some(sprintf "U-%d\nLINE-B" i) })
              let board = roster |> List.map (fun t -> Spin, t)
              let boardL = multiline |> List.map (fun t -> Spin, t)
              Expect.equal (Legibility.scoreAnimated boardL) (Legibility.scoreAnimated board) "multi-line labels do not alter animated governance (FR-011)"
          } ]
