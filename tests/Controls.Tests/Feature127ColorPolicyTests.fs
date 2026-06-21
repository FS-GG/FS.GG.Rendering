module Feature127ColorPolicyTests

// Feature 127 (Workstream F, F2) — color-validation policies (`wcag` / `ant`).
//
// The policy ENGINE is `module internal ColorPolicy` in FS.GG.UI.Color (reached here via
// InternalsVisibleTo). The design-system PAIRING CATALOG, all evaluation, and the report drift gate
// live in this test assembly (the one place that already references Color + DesignTokens +
// the internal DesignTokensExt), so DesignSystem stays at dep = Scene only.
//
// Coverage:
//   * US1 (FR-002/SC-001): `wcag` verdicts byte-identical to Contrast.check; default = wcag;
//     unknown names rejected; overall summary; alpha-composite + Indeterminate edge cases.
//   * US2 (FR-004/FR-005/FR-010/FR-011/SC-002/SC-003): `ant` is a genuinely different rule set
//     (≥1 pairing diverges), covers all Ant families, discloses out-of-scope + no-overclaim.
//   * US3 (FR-008/FR-009/SC-004): the committed reports render idempotently, are complete, and the
//     drift gate byte-compares them against the live render (with an UPDATE_POLICY_REPORTS=1 regen).

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Color
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private pairing name fg bg role : ColorPolicy.Pairing =
    { Name = name
      Foreground = fg
      Background = bg
      Role = role }

/// The shared catalog: real design-system token values (public DesignTokens primitives + the F1
/// internal Ant families via IVT). Both policies evaluate this same list; the order is the report
/// row order. The Ant semantic families (primary/success/warning/error/info/text-on-surface) are
/// all present (SC-003); `primary-hover-fg-on-surface` is the policy-divergent pairing (ratio ≈
/// 2.99: WCAG Fail, ant Pass — drives FR-005 divergence and FR-010 no-overclaim);
/// `decorative-hairline-on-surface` is the out-of-scope exemplar for `ant` (FR-011).
let private catalog =
    [ pairing "text-on-canvas" DesignTokensExt.Alias.Light.textDefault DesignTokensExt.Alias.Light.surfaceCanvas Text
      pairing "text-on-surface" DesignTokensExt.Alias.Light.textDefault DesignTokensExt.Map.Light.colorBgContainer Text
      pairing
          "muted-text-on-surface"
          DesignTokensExt.Alias.Light.textSecondary
          DesignTokensExt.Map.Light.colorBgContainer
          Text
      pairing "primary-fg-on-surface" DesignTokensExt.Seed.colorPrimary DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "success-fg-on-surface" DesignTokensExt.Seed.colorSuccess DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "warning-fg-on-surface" DesignTokensExt.Seed.colorWarning DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "error-fg-on-surface" DesignTokensExt.Seed.colorError DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing "info-fg-on-surface" DesignTokensExt.Seed.colorInfo DesignTokensExt.Map.Light.colorBgContainer GraphicOrUi
      pairing
          "primary-hover-fg-on-surface"
          DesignTokensExt.Map.Light.colorPrimaryHover
          DesignTokensExt.Map.Light.colorBgContainer
          GraphicOrUi
      pairing
          "decorative-hairline-on-surface"
          DesignTokensExt.Map.Light.colorBorder
          DesignTokensExt.Map.Light.colorBgContainer
          Decorative ]

let private reportPath name =
    Path.Combine(repositoryRoot, "docs", "reports", sprintf "color-policy-%s.md" name)

let private resultFor (results: ColorPolicy.PairingResult list) name =
    results |> List.find (fun r -> r.Pairing = name)

// T022 / US3 regeneration: when UPDATE_POLICY_REPORTS=1, (re)write both committed reports via the
// SAME renderReport evaluator the drift gate verifies. Runs at module load (before the tests read
// the files), so a single `UPDATE_POLICY_REPORTS=1 dotnet test` run regenerates then passes.
let private regenerateReportsIfRequested () =
    if Environment.GetEnvironmentVariable "UPDATE_POLICY_REPORTS" = "1" then
        let dir = Path.Combine(repositoryRoot, "docs", "reports")
        Directory.CreateDirectory dir |> ignore
        File.WriteAllText(reportPath "wcag", ColorPolicy.renderReport ColorPolicy.wcag catalog)
        File.WriteAllText(reportPath "ant", ColorPolicy.renderReport ColorPolicy.ant catalog)

do regenerateReportsIfRequested ()

[<Tests>]
let feature127ColorPolicyTests =
    testList
        "Feature127 color policy"
        [
          // ---- US1: validate against a named policy ----------------------------------------

          // T006 (FR-002/SC-001): every catalog pairing's wcag verdict equals Contrast.check
          // byte-for-byte, and wcag.Classify delegates to Contrast.verdict (behaviourally proven
          // over sampled ratios — function values cannot be compared structurally).
          test "wcag is byte-identical to Contrast.check for every pairing (FR-002/SC-001)" {
              for p in catalog do
                  let viaPolicy = (ColorPolicy.evaluatePairing ColorPolicy.wcag p).Verdict
                  let viaContrast = (Contrast.check p.Role p.Background p.Foreground).Verdict
                  Expect.equal viaPolicy viaContrast (sprintf "wcag verdict must match Contrast.check for %s" p.Name)

              for role in [ Text; GraphicOrUi; Decorative ] do
                  for ratio in [ 1.0; 2.9; 3.0; 4.4; 4.5; 6.9; 7.0; 21.0 ] do
                      Expect.equal
                          (ColorPolicy.wcag.Classify role ratio)
                          (Contrast.verdict role ratio)
                          (sprintf "wcag.Classify must delegate to Contrast.verdict (role %A, ratio %f)" role ratio)
          }

          // T007 (FR-003): the default policy is wcag (same value).
          test "defaultPolicy is wcag (FR-003)" {
              Expect.isTrue
                  (obj.ReferenceEquals(ColorPolicy.defaultPolicy, ColorPolicy.wcag))
                  "defaultPolicy must be the wcag policy"
              Expect.equal ColorPolicy.defaultPolicy.Name "wcag" "defaultPolicy.Name = wcag"
          }

          // T008 (FR-006/SC-005): unknown names are rejected explicitly — never a silent fallback.
          test "byName rejects unknown names with an Error (FR-006/SC-005)" {
              for bad in [ "material"; "Wcag"; "" ] do
                  match ColorPolicy.byName bad with
                  | Result.Error _ -> ()
                  | Result.Ok p -> failtestf "byName %A must be Error, got Ok %s" bad p.Name

              // exact lowercase names still resolve (ColorPolicy holds function fields, so assert
              // identity via the resolved Name rather than structural record equality).
              for good in [ "wcag"; "ant" ] do
                  match ColorPolicy.byName good with
                  | Result.Ok p -> Expect.equal p.Name good (sprintf "byName %A resolves to that policy" good)
                  | Result.Error e -> failtestf "byName %A must be Ok, got Error %s" good e
          }

          // T028 (FR-007): overall pass/fail summary — false with ≥1 Failed, true with none; the
          // rendered summary line reports the correct failing / out-of-scope / indeterminate counts.
          test "overall summary reflects failing / out-of-scope / indeterminate counts (FR-007)" {
              let wcagResults = ColorPolicy.evaluate ColorPolicy.wcag catalog
              let antResults = ColorPolicy.evaluate ColorPolicy.ant catalog
              // wcag fails the low-contrast hover pairing; ant has no Failed rows (one out-of-scope).
              Expect.isFalse (ColorPolicy.overall wcagResults) "wcag has a Failed row -> overall false"
              Expect.isTrue (ColorPolicy.overall antResults) "ant has no Failed row -> overall true"

              let wcagReport = ColorPolicy.renderReport ColorPolicy.wcag catalog
              let antReport = ColorPolicy.renderReport ColorPolicy.ant catalog
              Expect.stringContains
                  wcagReport
                  "**Overall: FAIL** (1 failing of 10 validated; 0 out-of-scope; 0 indeterminate)"
                  "wcag summary line"
              Expect.stringContains
                  antReport
                  "**Overall: PASS** (0 failing of 9 validated; 1 out-of-scope; 0 indeterminate)"
                  "ant summary line"
          }

          // T029 (edge cases): alpha is composited before measurement; an unmeasurable foreground
          // is Indeterminate with nan.
          test "alpha is composited before measurement; transparent fg is Indeterminate (edge cases)" {
              let bg = DesignTokensExt.Map.Light.colorBgContainer

              let semiTransparent =
                  { DesignTokensExt.Alias.Light.textDefault with
                      Alpha = 128uy }

              let semiPairing = pairing "alpha-text-on-surface" semiTransparent bg Text
              let semiResult = ColorPolicy.evaluatePairing ColorPolicy.wcag semiPairing
              let expectedMeasured = Contrast.ratio (Contrast.compositeOver bg semiTransparent) bg
              Expect.equal semiResult.Measured expectedMeasured "alpha foreground must be composited over bg before measuring"

              let transparent =
                  { DesignTokensExt.Alias.Light.textDefault with
                      Alpha = 0uy }

              let transparentPairing = pairing "transparent-text-on-surface" transparent bg Text
              let transparentResult = ColorPolicy.evaluatePairing ColorPolicy.wcag transparentPairing
              Expect.equal transparentResult.Outcome ColorPolicy.Indeterminate "unmeasurable fg -> Indeterminate"
              Expect.isTrue (Double.IsNaN transparentResult.Measured) "Indeterminate measured -> nan"
          }

          // ---- US2: the `ant` rule set --------------------------------------------------------

          // T012 (FR-005/SC-002): ≥1 shared pairing diverges between ant and wcag with identical
          // colors — the difference is the policy, not the colors.
          test "ant diverges from wcag on a shared pairing (FR-005/SC-002)" {
              let wcagResults = ColorPolicy.evaluate ColorPolicy.wcag catalog
              let antResults = ColorPolicy.evaluate ColorPolicy.ant catalog
              let name = "primary-hover-fg-on-surface"
              let w = resultFor wcagResults name
              let a = resultFor antResults name
              Expect.notEqual a.Outcome w.Outcome (sprintf "ant must reach a different outcome than wcag on %s" name)
              Expect.equal w.Outcome ColorPolicy.Failed (sprintf "%s fails under wcag" name)
              Expect.equal a.Outcome ColorPolicy.Passed (sprintf "%s passes under ant" name)
          }

          // T013 (FR-004/SC-003): ant yields a full PairingResult (threshold + measured + verdict)
          // for each Ant semantic family.
          test "ant covers every Ant semantic family with a full result (FR-004/SC-003)" {
              let antResults = ColorPolicy.evaluate ColorPolicy.ant catalog

              let families =
                  [ "primary-fg-on-surface"
                    "success-fg-on-surface"
                    "warning-fg-on-surface"
                    "error-fg-on-surface"
                    "info-fg-on-surface"
                    "text-on-surface" ]

              for name in families do
                  let r = resultFor antResults name
                  Expect.isSome r.Threshold (sprintf "%s has a threshold" name)
                  Expect.isFalse (Double.IsNaN r.Measured) (sprintf "%s has a measured ratio" name)
                  Expect.notEqual r.Outcome ColorPolicy.OutOfScope (sprintf "%s is in scope for ant" name)
          }

          // T014 (FR-011): the out-of-scope exemplar evaluates to OutOfScope under ant, never Passed.
          test "out-of-scope pairing is disclosed as OutOfScope under ant (FR-011)" {
              let antResults = ColorPolicy.evaluate ColorPolicy.ant catalog
              let r = resultFor antResults "decorative-hairline-on-surface"
              Expect.equal r.Outcome ColorPolicy.OutOfScope "decorative hairline is out of ant's validated set"
              Expect.notEqual r.Outcome ColorPolicy.Passed "out-of-scope must never read as Passed"
          }

          // T015 (FR-010): an ant pairing that WCAG would Fail carries an AuthorityNote.
          test "ant carries a no-overclaim AuthorityNote where it certifies a WCAG-failing pairing (FR-010)" {
              let antResults = ColorPolicy.evaluate ColorPolicy.ant catalog
              let r = resultFor antResults "primary-hover-fg-on-surface"
              Expect.equal r.Outcome ColorPolicy.Passed "ant certifies the hover pairing"
              Expect.isSome r.AuthorityNote "ant must disclose it is not WCAG-certified for this pairing"
              // and WCAG genuinely fails the same pairing
              let wcagResults = ColorPolicy.evaluate ColorPolicy.wcag catalog
              Expect.equal (resultFor wcagResults "primary-hover-fg-on-surface").Outcome ColorPolicy.Failed "wcag fails it"
          }

          // ---- US3: the policy report (idempotent, complete, drift-gated) ----------------------

          // T018 (SC-004): renderReport is idempotent — two renders of identical inputs are equal.
          test "renderReport is idempotent (SC-004)" {
              Expect.equal
                  (ColorPolicy.renderReport ColorPolicy.wcag catalog)
                  (ColorPolicy.renderReport ColorPolicy.wcag catalog)
                  "wcag report renders byte-identically twice"
              Expect.equal
                  (ColorPolicy.renderReport ColorPolicy.ant catalog)
                  (ColorPolicy.renderReport ColorPolicy.ant catalog)
                  "ant report renders byte-identically twice"
          }

          // T019 (FR-008/SC-003): each report has one data row per catalog pairing, in order; the
          // ant report names every family and discloses the out-of-scope row (not as a pass).
          test "reports are complete: one row per pairing, families present, out-of-scope disclosed (FR-008/SC-003)" {
              let antReport = ColorPolicy.renderReport ColorPolicy.ant catalog

              let dataRows =
                  antReport.Split('\n')
                  |> Array.filter (fun l -> l.StartsWith "| ")
                  |> Array.filter (fun l -> not (l.StartsWith "| Pairing"))

              Expect.equal dataRows.Length catalog.Length "one data row per validated pairing, in catalog order"

              for name in
                  [ "primary-fg-on-surface"
                    "success-fg-on-surface"
                    "warning-fg-on-surface"
                    "error-fg-on-surface"
                    "info-fg-on-surface"
                    "text-on-surface" ] do
                  Expect.stringContains antReport name (sprintf "ant report includes %s" name)

              Expect.stringContains antReport "out-of-scope" "ant report discloses the out-of-scope row"
          }

          // T020 (FR-009/SC-004): the committed reports byte-match the live render (drift gate +
          // tamper detection). Regenerate with UPDATE_POLICY_REPORTS=1.
          test "committed reports match the live render — drift gate (FR-009/SC-004)" {
              for name, policy in [ "wcag", ColorPolicy.wcag; "ant", ColorPolicy.ant ] do
                  let path = reportPath name
                  Expect.isTrue (File.Exists path) (sprintf "committed report %s must exist (regenerate with UPDATE_POLICY_REPORTS=1)" path)
                  let committed = File.ReadAllText path
                  let live = ColorPolicy.renderReport policy catalog
                  Expect.equal committed live (sprintf "committed docs/reports/color-policy-%s.md is out of date (drift)" name)
          }
        ]
