namespace FS.GG.UI.Color

// Feature 127 (Workstream F, F2) — the color-validation policy engine.
//
// `module internal` with NO companion .fsi: additive, zero public-surface delta (FR-012/SC-006);
// public promotion is deferred to F5. Reached by Controls.Tests via InternalsVisibleTo, where the
// design-system pairing catalog + evaluation + the report drift gate live (keeping DesignSystem at
// dep = Scene only). Hosted here in FS.GG.UI.Color, beside Contrast, so the `wcag` policy reuses
// Contrast.verdict byte-for-byte (FR-002/SC-001) with zero new project dependency.
//
// Per the repo's Principle II, visibility lives in .fsi; this file has none, so NO top-level access
// modifiers appear on bindings (the whole module is already internal).

open System
open System.Globalization
open FS.GG.UI.Scene

module internal ColorPolicy =

    /// What a policy claims as its authority (drives no-overclaim disclosure, FR-010).
    type Authority =
        | WcagCertified
        | AntExpectation

    /// Tri-state-plus outcome of one pairing under one policy (FR-011).
    type PolicyOutcome =
        | Passed
        | Failed
        | OutOfScope
        | Indeterminate

    /// A named, selectable rule set.
    type ColorPolicy =
        { Name: string
          Label: string
          Authority: Authority
          Threshold: Role -> float
          Classify: Role -> float -> Verdict }

    /// One validated pairing (catalog entry).
    type Pairing =
        { Name: string
          Foreground: Color
          Background: Color
          Role: Role }

    /// Result of evaluating one pairing under one policy.
    type PairingResult =
        { Pairing: string
          Measured: float
          Threshold: float option
          Outcome: PolicyOutcome
          Verdict: Verdict
          AuthorityNote: string option }

    // ---- shared evaluation machinery -------------------------------------------------------

    /// Map an underlying verdict to a policy outcome (Exempt/Aaa/Aa/AaLarge are passes).
    let outcomeOfVerdict verdict =
        match verdict with
        | Fail -> Failed
        | Verdict.Indeterminate -> PolicyOutcome.Indeterminate
        | _ -> Passed

    // ---- the two delivered policies --------------------------------------------------------

    /// Ant Design's required-ratio table, authored as F# literals with provenance.
    ///
    /// Provenance: adapted from the FS-Skia-UI Ant adoption analysis (`FS.Skia.UI.*` →
    /// `FS.GG.UI.*`). Deliberately distinct from WCAG's 7.0/4.5/3.0 role gates so at least one
    /// shared design-system pairing changes verdict under `ant` vs `wcag` (FR-005/SC-002):
    ///   * Text 4.5        — Ant holds body text to a flat AA bar (no WCAG "large text" 3.0 tier),
    ///                       so high-contrast text that WCAG rates Aaa is rated Aa here.
    ///   * GraphicOrUi 2.5 — Ant accepts lower contrast for branded/hover component accents than
    ///                       WCAG's 3.0 non-text floor; where this certifies a pairing WCAG would
    ///                       fail, the result carries an AuthorityNote (FR-010 no-overclaim).
    let antThreshold role =
        // `open FS.GG.UI.Scene` brings SceneNode.Text into scope, which shadows Role.Text; the
        // Role cases are therefore qualified throughout this file (the contrast modules avoid this
        // by declaring Role after the open, which is not possible from here).
        match role with
        | Role.Text -> 4.5
        | Role.GraphicOrUi -> 2.5
        | Role.Decorative -> nan

    /// `wcag` — Authority = WcagCertified; Classify delegates directly to `Contrast.verdict`
    /// (not a re-implemented copy) so default behavior is byte-identical to today (FR-002).
    let wcag =
        { Name = "wcag"
          Label = "WCAG 2.x contrast"
          Authority = WcagCertified
          Threshold =
            (fun role ->
                match role with
                | Role.Text -> 4.5
                | Role.GraphicOrUi -> 3.0
                | Role.Decorative -> nan)
          Classify = Contrast.verdict }

    /// `ant` — Authority = AntExpectation; its own threshold table (FR-004). Classify maps the
    /// measured ratio against the Ant threshold to a verdict; Decorative is exempt.
    let ant =
        { Name = "ant"
          Label = "Ant Design contrast expectations"
          Authority = AntExpectation
          Threshold = antThreshold
          Classify =
            (fun role ratio ->
                match role with
                | Role.Decorative -> Exempt
                | _ -> if ratio >= antThreshold role then Aa else Fail) }

    /// Default applied when no policy is chosen (FR-003).
    let defaultPolicy = wcag

    /// Resolve by exact lowercase name; explicit failure on unknown — NO silent fallback
    /// (FR-006/SC-005).
    let byName (name: string) : Result<ColorPolicy, string> =
        match name with
        | "wcag" -> Ok wcag
        | "ant" -> Ok ant
        // `Error` is qualified: `open FS.GG.UI.Scene` brings DiagnosticSeverity.Error into scope.
        | other -> Result.Error(sprintf "Unknown color policy '%s'; known policies: wcag, ant" other)

    /// Is this pairing in the policy's validated set? Drives OutOfScope (FR-011). `wcag` validates
    /// every pairing (Decorative resolves to Exempt); `ant` validates text + component foregrounds
    /// but not decorative hairlines.
    let inScope (policy: ColorPolicy) (pairing: Pairing) =
        match policy.Authority with
        | WcagCertified -> true
        | AntExpectation -> pairing.Role <> Role.Decorative

    /// Evaluate one pairing under one policy. A semi-transparent foreground is composited over its
    /// background via `Contrast.compositeOver` BEFORE measuring with `Contrast.ratio` (alpha is
    /// never ignored); a fully-transparent (unmeasurable) foreground yields Indeterminate + nan; an
    /// out-of-scope pairing yields OutOfScope, never Passed.
    let evaluatePairing (policy: ColorPolicy) (pairing: Pairing) =
        let thresholdOpt =
            match pairing.Role with
            | Role.Decorative -> None
            | role -> Some(policy.Threshold role)

        if pairing.Foreground.Alpha = 0uy then
            { Pairing = pairing.Name
              Measured = nan
              Threshold = thresholdOpt
              Outcome = PolicyOutcome.Indeterminate
              Verdict = Verdict.Indeterminate
              AuthorityNote = None }
        else
            let resolved = Contrast.compositeOver pairing.Background pairing.Foreground
            let measured = Contrast.ratio resolved pairing.Background
            let verdict = policy.Classify pairing.Role measured

            if not (inScope policy pairing) then
                { Pairing = pairing.Name
                  Measured = measured
                  Threshold = thresholdOpt
                  Outcome = OutOfScope
                  Verdict = verdict
                  AuthorityNote = None }
            else
                let outcome = outcomeOfVerdict verdict

                let note =
                    if
                        outcome = Passed
                        && policy.Authority <> WcagCertified
                        && Contrast.verdict pairing.Role measured = Fail
                    then
                        Some(sprintf "%s: not WCAG-certified" policy.Name)
                    else
                        None

                { Pairing = pairing.Name
                  Measured = measured
                  Threshold = thresholdOpt
                  Outcome = outcome
                  Verdict = verdict
                  AuthorityNote = note }

    /// Evaluate a whole catalog → per-pairing results (catalog order preserved).
    let evaluate (policy: ColorPolicy) (catalog: Pairing list) =
        catalog |> List.map (evaluatePairing policy)

    /// Overall pass = no Failed rows (OutOfScope/Indeterminate are listed, not counted as fail).
    let overall (results: PairingResult list) =
        results |> List.forall (fun r -> r.Outcome <> Failed)

    // ---- deterministic report rendering (single evaluator shared with the tests) ------------

    let private formatMeasured (m: float) =
        if Double.IsNaN m then
            "n/a"
        else
            m.ToString("F2", CultureInfo.InvariantCulture)

    let private formatThreshold (t: float option) =
        match t with
        | Some v -> v.ToString("F2", CultureInfo.InvariantCulture)
        | None -> "n/a"

    let private formatHex (c: Color) =
        if c.Alpha = 255uy then
            sprintf "#%02x%02x%02x" c.Red c.Green c.Blue
        else
            sprintf "#%02x%02x%02x%02x" c.Red c.Green c.Blue c.Alpha

    let private roleText role =
        match role with
        | Role.Text -> "Text"
        | Role.GraphicOrUi -> "GraphicOrUi"
        | Role.Decorative -> "Decorative"

    /// The disclosure column: out-of-scope / indeterminate take precedence over the verdict so a
    /// pairing outside the policy's set is never shown as a pass (FR-011).
    let private disclosureText (r: PairingResult) =
        match r.Outcome with
        | OutOfScope -> "out-of-scope"
        | PolicyOutcome.Indeterminate -> "indeterminate"
        | _ ->
            match r.Verdict with
            | Aaa -> "Aaa"
            | Aa -> "Aa"
            | AaLarge -> "AaLarge"
            | Fail -> "Fail"
            | Exempt -> "Exempt"
            | Verdict.Indeterminate -> "indeterminate"

    let private authorityText authority =
        match authority with
        | WcagCertified -> "WCAG-certified"
        | AntExpectation -> "Ant Design expectation (not WCAG-certified)"

    /// Deterministic, human-readable report (FR-008): static generated-marker + authority header,
    /// one row per pairing in fixed catalog order, fixed F2 invariant-culture numerics, lowercase
    /// hex colors, `\n` line endings, an overall summary — no clock/random/culture-sensitive
    /// content. The same evaluator the tests exercise, so the committed report cannot drift.
    let renderReport (policy: ColorPolicy) (catalog: Pairing list) : string =
        let results = evaluate policy catalog
        let countWhere f = results |> List.filter f |> List.length
        let failing = countWhere (fun r -> r.Outcome = Failed)
        let validated = countWhere (fun r -> r.Outcome = Passed || r.Outcome = Failed)
        let outOfScope = countWhere (fun r -> r.Outcome = OutOfScope)
        let indeterminate = countWhere (fun r -> r.Outcome = PolicyOutcome.Indeterminate)
        let pass = failing = 0

        let header =
            [ sprintf "# Color Policy Report — %s (`%s`)" policy.Label policy.Name
              ""
              "> GENERATED — do not edit. Regenerate via: UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature127"
              sprintf "> Authority: %s" (authorityText policy.Authority)
              ""
              "| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |"
              "|---------|-----------|-----------|------|----------|-----------|---------|------|" ]

        let rows =
            List.zip catalog results
            |> List.map (fun (p, r) ->
                sprintf
                    "| %s | %s | %s | %s | %s | %s | %s | %s |"
                    r.Pairing
                    (formatHex p.Foreground)
                    (formatHex p.Background)
                    (roleText p.Role)
                    (formatMeasured r.Measured)
                    (formatThreshold r.Threshold)
                    (disclosureText r)
                    (defaultArg r.AuthorityNote ""))

        let summary =
            [ ""
              sprintf
                  "**Overall: %s** (%d failing of %d validated; %d out-of-scope; %d indeterminate)"
                  (if pass then "PASS" else "FAIL")
                  failing
                  validated
                  outOfScope
                  indeterminate ]

        (header @ rows @ summary |> String.concat "\n") + "\n"
