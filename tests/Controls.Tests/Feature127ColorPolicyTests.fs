module Feature127ColorPolicyTests

// Feature 127 (Workstream F, F2) — color-validation policies (`wcag` / `ant`).
//
// The policy ENGINE is `module internal ColorPolicy` in FS.GG.UI.Color, and the pairing CATALOGS are
// `module internal StyleCatalog` beside it (both reached here via InternalsVisibleTo). Issue #174
// moved the catalogs out of this file: while they lived here the policy linted a hand-authored list
// and never saw a `ResolvedStyle` the resolver produced, so no consumer could violate it. This
// assembly now supplies only the THEMES and the report drift gate.
//
// Coverage:
//   * US1 (FR-002/SC-001): `wcag` verdicts byte-identical to Contrast.check; default = wcag;
//     unknown names rejected; overall summary; alpha-composite + Indeterminate edge cases.
//   * US2 (FR-004/FR-005/FR-010/FR-011/SC-002/SC-003): `ant` is a genuinely different rule set
//     (≥1 pairing diverges), covers all Ant families, discloses out-of-scope + no-overclaim.
//   * US3 (FR-008/FR-009/SC-004): the committed reports render idempotently, are complete, and the
//     drift gate byte-compares them against the live render (with an UPDATE_POLICY_REPORTS=1 regen).
//   * Issue #174: the policy evaluates the styles `StyleResolver` actually emits — the emitted
//     catalog is derived from the resolver, a low-contrast theme is rejected, and the emitted
//     reports are drift-gated so a newly unreadable style cannot land unremarked.

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

/// The design-system token catalog — now owned by the library (issue #174), not this file.
let private catalog = StyleCatalog.designSystemTokens

/// The built-in themes whose emitted style set is drift-gated. Fully qualified: the `Theme` modules
/// would otherwise shadow the `Theme` type this file also names.
let private gatedThemes: (string * Theme) list =
    [ "default-light", FS.GG.UI.Themes.Default.Theme.light
      "default-dark", FS.GG.UI.Themes.Default.Theme.dark
      "ant-light", FS.GG.UI.Themes.AntDesign.AntTheme.antLight
      "ant-dark", FS.GG.UI.Themes.AntDesign.AntTheme.antDark ]

/// F-DS-1 waiver. The Ant neutral `default`/`dashed` control border sits BELOW WCAG 1.4.11's 3.0
/// non-text floor by Ant's own design: #d9d9d9 on the light canvas (≈1.29) and #424242 on the
/// black canvas (≈2.09). Ant identifies a `default` button by its label, its surface fill and its
/// drop shadow (elevation), not by a high-contrast edge; this renderer models no elevation, so the
/// bare border measures low. Recolouring Ant's canonical border token would break Ant fidelity
/// (Ant is the design source of truth, per CLAUDE.md), so the boundary gate below waives these two
/// pairings by EVIDENCE rather than repairing them — and only these two: the gate asserts the
/// waived set is exactly this list, so a new sub-3.0 boundary still reds and a later Ant repair
/// forces the stale entry out. Keyed `(slug, border colour, canvas)`; the border tokens are the
/// same the Ant `IntentPolicy` paints from (`AntIntentPolicy.policyFor`).
let private antNeutralBorderWaiver: (string * Color * Color) list =
    [ "ant-light", DesignTokensExt.Component.Button.defaultBorder, FS.GG.UI.Themes.AntDesign.AntTheme.antLight.Background
      "ant-dark", DesignTokensExt.Map.Dark.colorBorder, FS.GG.UI.Themes.AntDesign.AntTheme.antDark.Background ]

let private reportPath name =
    Path.Combine(repositoryRoot, "docs", "reports", sprintf "color-policy-%s.md" name)

let private emittedReportPath slug = reportPath (sprintf "emitted-%s" slug)

/// The emitted reports are rendered under `wcag` — the WCAG-certified authority is the one whose
/// verdict a low-contrast style must answer to.
let private emittedReport (slug: string) (theme: Theme) =
    ColorPolicy.renderReportFor
        (Some(sprintf "emitted styles, `%s` theme" slug))
        ColorPolicy.wcag
        (StyleCatalog.emittedPairings theme)

let private resultFor (results: ColorPolicy.PairingResult list) name =
    results |> List.find (fun r -> r.Pairing = name)

// T022 / US3 regeneration: when UPDATE_POLICY_REPORTS=1, (re)write every committed report via the
// SAME renderReport evaluator the drift gate verifies. Runs at module load (before the tests read
// the files), so a single `UPDATE_POLICY_REPORTS=1 dotnet test` run regenerates then passes.
let private regenerateReportsIfRequested () =
    if Environment.GetEnvironmentVariable "UPDATE_POLICY_REPORTS" = "1" then
        let dir = Path.Combine(repositoryRoot, "docs", "reports")
        Directory.CreateDirectory dir |> ignore
        File.WriteAllText(reportPath "wcag", ColorPolicy.renderReport ColorPolicy.wcag catalog)
        File.WriteAllText(reportPath "ant", ColorPolicy.renderReport ColorPolicy.ant catalog)

        for slug, theme in gatedThemes do
            File.WriteAllText(emittedReportPath slug, emittedReport slug theme)

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

          // T006b (Review P7 / D2): a normal-size Text pairing measuring in the WCAG large-text
          // band (3.0 ≤ ratio < 4.5) must NOT count as a `wcag` pass. `Pairing` carries no font
          // size, so the AaLarge (large-text-only) tier is never evidenced; counting it a pass
          // overclaims a body-text pairing at 3.x:1 under WcagCertified authority. colorPrimary on
          // white ≈ 4.10 lands in the band — wcag must gate it at the declared 4.5 Text threshold.
          test "wcag does not pass a normal-size Text pairing in the large-text band (Review P7)" {
              let bandPairing =
                  pairing
                      "primary-as-text-on-surface"
                      DesignTokensExt.Seed.colorPrimary
                      DesignTokensExt.Map.Light.colorBgContainer
                      Text

              let r = ColorPolicy.evaluatePairing ColorPolicy.wcag bandPairing
              // the raw verdict stays AaLarge — Classify still delegates to Contrast.verdict…
              Expect.isTrue
                  (r.Measured >= 3.0 && r.Measured < 4.5)
                  (sprintf "colorPrimary-on-white must land in the large-text band (measured %f)" r.Measured)
              Expect.equal r.Verdict AaLarge "Contrast.verdict rates the large-text band AaLarge"
              // …but with no size evidence it is not a certified pass, and it drags overall to fail.
              Expect.equal r.Outcome ColorPolicy.Failed "no size evidence -> AaLarge is not a wcag pass"
              Expect.isFalse
                  (ColorPolicy.overall [ r ])
                  "a large-text-band Text pairing must not count toward overall PASS"
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

          // ---- Issue #174: the policy sees the styles the resolver emits ----------------------

          // The catalog must be DERIVED from StyleResolver, not a second hand-authored list that
          // happens to agree with it today. Ask the resolver for a style, then demand the pairings
          // carry exactly those colours: re-hardcoding them breaks this test.
          //
          // Membership in the emitted catalog is asserted by COLOUR, not by row name — dedup elects
          // one witness combination per (fg, bg, role), and which one it elects is not the contract.
          test "the emitted catalog is derived from StyleResolver, not restated (issue #174)" {
              let theme = FS.GG.UI.Themes.Default.Theme.light

              // `button/danger/invalid` is the witness: the `danger` variant fills with `theme.Danger`
              // and carries an on-fill label, while the `Invalid` validation state adds a red BORDER.
              // Until issue #359, `Invalid` also repainted the LABEL `theme.Danger`, so the resolver
              // emitted a danger-on-danger label — invisible, ratio 1.00. It now tints only the stroke
              // (symmetric with `Valid`/`Pending`), so the label stays the variant's readable on-fill
              // foreground and the red border carries the invalid signal.
              let style = StyleResolver.resolve theme "button" "" [ Variant StyleVariant.Danger ] (VisualState.Validation(Invalid ""))
              Expect.notEqual style.Foreground style.Fill "issue #359: the invalid label is no longer painted onto its own fill"
              Expect.equal style.Foreground theme.Background "the danger variant's on-fill label survives validation"
              Expect.equal style.Stroke theme.Danger "the invalid signal moved to the border"

              let rows = StyleCatalog.pairingsOfStyle theme.Background "button/danger/invalid" false style
              let textRow = rows |> List.find (fun p -> p.Name.EndsWith "#text")
              Expect.equal textRow.Foreground style.Foreground "the pairing carries the resolver's own foreground"

              Expect.equal
                  textRow.Background
                  (Contrast.compositeOver theme.Background style.Fill)
                  "…and the resolver's own fill, composited over the canvas"

              Expect.equal
                  (ColorPolicy.evaluatePairing ColorPolicy.wcag textRow).Outcome
                  ColorPolicy.Passed
                  "issue #359: a readable invalid label is no longer a policy failure"

              let key (p: ColorPolicy.Pairing) = p.Foreground, p.Background, p.Role

              Expect.contains
                  (StyleCatalog.emittedPairings theme |> List.map key)
                  (key textRow)
                  "the emitted catalog contains the style the resolver produced"
          }

          // The domain is closed and every combination is reached: 2 kinds x 12 scenarios x 11
          // states (6 variants under the identity intent + 6 intent scenarios over the bare base).
          // Dedup by (fg, bg, role) collapses them, but nothing may be silently dropped.
          test "the emitted catalog covers the closed kind x scenario x state domain (issue #174 / F-DS-1)" {
              Expect.equal StyleCatalog.scenarios.Length (StyleCatalog.variants.Length + StyleCatalog.intents.Length) "scenarios = variants + intents"
              Expect.equal (StyleCatalog.kinds.Length * StyleCatalog.scenarios.Length * StyleCatalog.states.Length) 264 "closed domain size"

              let scenarioNames = StyleCatalog.scenarios |> List.map (fun (name, _, _) -> name)

              for slug, theme in gatedThemes do
                  let emitted = StyleCatalog.emittedPairings theme
                  Expect.isNonEmpty emitted (sprintf "%s emits pairings" slug)

                  let keys = emitted |> List.map (fun p -> p.Foreground, p.Background, p.Role)
                  Expect.equal keys.Length (List.distinct keys).Length (sprintf "%s rows are deduplicated by (fg, bg, role)" slug)

                  // every row names a real combination from the enumerated domain
                  for p in emitted do
                      let combination = p.Name.Split('#').[0]
                      let parts = combination.Split('/')
                      Expect.equal parts.Length 3 (sprintf "%s: %s names kind/scenario/state" slug p.Name)
                      Expect.contains StyleCatalog.kinds parts.[0] (sprintf "%s: %s names a real kind" slug p.Name)
                      Expect.contains scenarioNames parts.[1] (sprintf "%s: %s names a real scenario" slug p.Name)
                      Expect.contains (StyleCatalog.states |> List.map fst) parts.[2] (sprintf "%s: %s names a real state" slug p.Name)
          }

          // A disabled control is WCAG-exempt (1.4.3 / 1.4.11 both exempt inactive components), so
          // the resolver's muted-on-muted Disabled delta must not be reported as a failure — and a
          // control with neither fill nor stroke has no boundary, so it emits no boundary pairing.
          test "disabled pairings are exempt; a boundary-less control emits no boundary row (issue #174)" {
              let theme = FS.GG.UI.Themes.Default.Theme.light
              let emitted = StyleCatalog.emittedPairings theme

              let disabled = emitted |> List.filter (fun p -> p.Name.Contains "/disabled#")
              Expect.isNonEmpty disabled "disabled styles are enumerated, not skipped"

              for p in disabled do
                  Expect.equal p.Role Decorative (sprintf "%s: an inactive component is exempt" p.Name)
                  Expect.notEqual (ColorPolicy.evaluatePairing ColorPolicy.wcag p).Outcome ColorPolicy.Failed (sprintf "%s must not fail" p.Name)

              // the ghost button at rest: transparent fill, zero stroke width -> no boundary to measure
              let ghost = StyleResolver.resolve theme "button" "" [ Variant StyleVariant.Ghost ] Normal
              Expect.equal ghost.Fill Colors.transparent "the ghost base fill is transparent"
              Expect.equal ghost.StrokeWidth 0.0 "the button base draws no stroke"

              let rows = StyleCatalog.pairingsOfStyle theme.Background "button/ghost/normal" false ghost
              Expect.equal (rows |> List.map (fun p -> p.Name)) [ "button/ghost/normal#text" ] "only the text pairing — no unmeasurable boundary row"
          }

          // The acceptance test the issue asks for: a deliberately low-contrast theme is REJECTED.
          // Painting every role the same colour makes every emitted label invisible; `overall` must
          // be false, and it must be false because rows FAILED, not because they were exempted or
          // ruled out of scope.
          test "a deliberately low-contrast theme is rejected (issue #174)" {
              let flat = { Red = 0x77uy; Green = 0x77uy; Blue = 0x77uy; Alpha = 255uy }

              let lowContrast =
                  { FS.GG.UI.Themes.Default.Theme.light with
                      Name = "low-contrast"
                      Foreground = flat
                      Background = flat
                      Accent = flat
                      Danger = flat
                      Success = flat
                      Warning = flat
                      Muted = flat }

              let results = ColorPolicy.evaluate ColorPolicy.wcag (StyleCatalog.emittedPairings lowContrast)
              Expect.isFalse (ColorPolicy.overall results) "a theme whose every colour is identical must not pass"

              let failed = results |> List.filter (fun r -> r.Outcome = ColorPolicy.Failed)
              Expect.isNonEmpty failed "rejection must come from Failed rows, not from an empty catalog"

              // The un-modulated states paint one flat colour on itself: exactly 1.00.
              Expect.isNonEmpty
                  (failed |> List.filter (fun r -> r.Measured = 1.0))
                  "a flat theme's unmodulated states measure 1.00"

              // Since issue #181 the state layer derives its own shades off the fill (hover lightens,
              // pressed/selected darken), so a flat theme no longer yields 1.00 on EVERY row. Those
              // shades are single emphasis steps off one colour, so every failing row still measures
              // below 3.0 — the most permissive threshold any non-decorative role is held to. The
              // rejection remains the theme's doing, not a gate that refuses all input.
              for r in failed do
                  Expect.isLessThan r.Measured 3.0 (sprintf "%s: a flat theme's every shade is near-invisible" r.Pairing)

              // …and the shipped theme it was derived from is genuinely different: it passes rows
              // this one fails, so the rejection is the theme's doing, not the gate refusing all input.
              let shipped = ColorPolicy.evaluate ColorPolicy.wcag (StyleCatalog.emittedPairings FS.GG.UI.Themes.Default.Theme.light)
              Expect.isNonEmpty (shipped |> List.filter (fun r -> r.Outcome = ColorPolicy.Passed)) "the shipped light theme passes some pairings"
          }

          // The gate with teeth: the emitted style set of every built-in theme is committed. A
          // resolver or theme edit that makes a style unreadable (or repairs one) changes these
          // files, and the build fails until the change is seen and re-committed.
          test "committed emitted-style reports match the live render — drift gate (issue #174)" {
              for slug, theme in gatedThemes do
                  let path = emittedReportPath slug
                  Expect.isTrue (File.Exists path) (sprintf "committed report %s must exist (regenerate with UPDATE_POLICY_REPORTS=1)" path)
                  let committed = File.ReadAllText path
                  let live = emittedReport slug theme
                  Expect.equal committed live (sprintf "committed docs/reports/color-policy-emitted-%s.md is out of date (drift)" slug)
          }

          // The drift gate above only asserts the emitted set did not CHANGE — a committed report
          // that reads `Overall: FAIL` keeps CI green, so a shipped unreadable label goes unremarked
          // (issue #360; the concrete instance was #379/PR #387, an antLight Text row that stayed at
          // 3.76 with no check red). This is the gate that actually blocks: every readable label the
          // resolver emits — every `Role.Text` pairing of every built-in theme — must PASS `wcag`.
          //
          // The exemptions the issue asks to carve out are structural, not enumerated here:
          //   * disabled controls resolve to `Role.Decorative` (WCAG 1.4.3 exempts inactive
          //     components), so the `Role.Text` filter already drops them;
          //   * the impossible (variant × validation-state) combos — e.g. danger + Invalid — resolve
          //     to their base variant's colours and are deduped away by `emittedPairings` before they
          //     reach here, so a `Role.Text` row that survives is a real, reachable label.
          // ghost is a legible label over the canvas (~19:1), not an exemption, so it is gated too.
          test "every emitted Text label of every built-in theme passes wcag — blocking gate (issue #360)" {
              for slug, theme in gatedThemes do
                  let textLabels =
                      StyleCatalog.emittedPairings theme
                      |> List.filter (fun p -> p.Role = Role.Text)
                  Expect.isNonEmpty textLabels (sprintf "%s emits readable Text labels" slug)

                  for p in textLabels do
                      Expect.notEqual
                          (ColorPolicy.evaluatePairing ColorPolicy.wcag p).Outcome
                          ColorPolicy.Failed
                          (sprintf "%s: %s is a readable label — it must not fail wcag" slug p.Name)
          }

          // F-DS-1 companion to the Text gate, one role out. Enumerating the intent vocabulary
          // (`StyleCatalog.scenarios`) makes the theme's `IntentPolicy` chrome reach the catalog, so
          // the boundary a control is identified by — its stroke, or its fill when unstroked; WCAG
          // 1.4.11 rates it GraphicOrUi at the 3.0 non-text floor — is now measurable. This is the
          // gate with teeth: every emitted boundary of every built-in theme must PASS wcag, except
          // the two intentionally-subtle Ant neutral borders carried in `antNeutralBorderWaiver`
          // (evidence there). The waiver is enumerated, not a blanket skip, and checked BOTH ways so
          // it can neither grow silently nor go stale.
          test "every emitted boundary passes wcag except the evidenced Ant neutral-border waiver (F-DS-1)" {
              let boundaryFails theme =
                  StyleCatalog.emittedPairings theme
                  |> List.filter (fun p -> p.Role = Role.GraphicOrUi)
                  |> List.filter (fun p -> (ColorPolicy.evaluatePairing ColorPolicy.wcag p).Outcome = ColorPolicy.Failed)

              // (1) every sub-floor boundary that IS emitted must be explicitly waived — an unwaived
              //     one is a real, reachable defect the intent-blind `intent = ""` catalog hid.
              for slug, theme in gatedThemes do
                  for p in boundaryFails theme do
                      let waived =
                          antNeutralBorderWaiver
                          |> List.exists (fun (s, fg, bg) -> s = slug && fg = p.Foreground && bg = p.Background)

                      Expect.isTrue
                          waived
                          (sprintf
                              "%s: boundary %s (%A on %A) falls below the 3.0 floor and is not in the evidenced waiver — fix it or waive with evidence"
                              slug
                              p.Name
                              p.Foreground
                              p.Background)

              // (2) the waiver may not go stale: each waived pairing must still be emitted AND still
              //     fail. A later Ant repair (or a token retint) makes it pass or disappear, reds
              //     this, and forces the dead entry out — no silent over-waiving.
              for slug, fg, bg in antNeutralBorderWaiver do
                  let theme = gatedThemes |> List.find (fun (s, _) -> s = slug) |> snd

                  match
                      StyleCatalog.emittedPairings theme
                      |> List.tryFind (fun p -> p.Role = Role.GraphicOrUi && p.Foreground = fg && p.Background = bg)
                  with
                  | None -> failtestf "%s: waived boundary (%A on %A) is no longer emitted — remove the stale waiver" slug fg bg
                  | Some p ->
                      Expect.equal
                          (ColorPolicy.evaluatePairing ColorPolicy.wcag p).Outcome
                          ColorPolicy.Failed
                          (sprintf "%s: waived boundary (%A on %A) now PASSES wcag — remove the stale waiver" slug fg bg)
          }

          // F-DS-2 (Phase 6): `DesignTokens.{Light,Dark}.contrastRequiredRatio` (DTCG source
          // `design-tokens.tokens.json`) is published as "the minimum foreground/background contrast
          // ratio the theme MUST satisfy" (DesignTokens.fsi:45,86) — but NO runtime code read it. The
          // WCAG gates hardcode the fixed 7.0/4.5/3.0 role tiers (Contrast.fs), deliberately distinct
          // from any per-theme token, so the published token was inert documentation of a constraint
          // nothing enforced. This is the gate that makes the published constraint TRUE. The token
          // primitives feed the built theme unchanged (Theme.fs:14-15,34-35), so the assertion is over
          // the ACTUAL shipped theme: a token retint (or a slackened floor) that drops the default
          // theme's own foreground-on-background below its declared ratio now reds here instead of
          // passing silently.
          test "each default theme satisfies its own declared contrastRequiredRatio token (F-DS-2)" {
              let declared =
                  [ "default-light",
                    FS.GG.UI.Themes.Default.Theme.light,
                    DesignTokens.Light.contrastRequiredRatio,
                    DesignTokens.Light.foreground,
                    DesignTokens.Light.background
                    "default-dark",
                    FS.GG.UI.Themes.Default.Theme.dark,
                    DesignTokens.Dark.contrastRequiredRatio,
                    DesignTokens.Dark.foreground,
                    DesignTokens.Dark.background ]

              for slug, theme, required, tokenFg, tokenBg in declared do
                  // Non-vacuous: a ratio of 1.0 is met by any colour pair, so a token slackened to 1.0
                  // would make this gate certify nothing. The shipped floor is a real AA-class bar.
                  Expect.isGreaterThan required 1.0 (sprintf "%s: contrastRequiredRatio must be a real floor, not vacuous" slug)

                  // The token primitives ARE what the built theme paints with, so the ratio measured
                  // below is the ratio the shipped theme actually presents (guards against the token
                  // constraining primitives the theme has since diverged from).
                  Expect.equal theme.Foreground tokenFg (sprintf "%s: theme foreground is the DTCG token primitive" slug)
                  Expect.equal theme.Background tokenBg (sprintf "%s: theme background is the DTCG token primitive" slug)

                  // The constraint the token DOCUMENTS is now ENFORCED: the theme clears its own ratio.
                  let measured = Contrast.ratio theme.Foreground theme.Background

                  Expect.isGreaterThanOrEqual
                      measured
                      required
                      (sprintf
                          "%s: foreground-on-background contrast (%.2f) must satisfy the declared contrastRequiredRatio (%.2f)"
                          slug
                          measured
                          required)
          }
        ]
