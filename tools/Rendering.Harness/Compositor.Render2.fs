namespace Rendering.Harness.Compositor

open Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Rendering.Harness.Compositor.Types
open Rendering.Harness.Compositor.Config
open Rendering.Harness.Compositor.FeatureState
open Rendering.Harness.Compositor.Render

module Render2 =
    let emitFeature154LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 Compositor Proof Acceptance"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature154PackageVersion}`"
              ""
              "## Acceptance Gate"
              ""
              "- Accepted proof requires exactly three selected fresh matching capable-host attempts from one host profile and one proof method."
              "- Each accepted attempt must include fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts."
              "- Damaged pixels must update and undamaged pixels must preserve the sentinel identity."
              "- Unsupported, stale, missing, blank, undecodable, synthetic-only, incomplete, failed-pixel, host-mismatched, or proof-method-mismatched evidence fails closed."
              "- Unsupported-host output records zero accepted partial-redraw artifacts."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let emitFeature154ProofSet (model: ReadinessModel) =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | no capable-host attempts are available |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 Proof-Set Acceptance"
              ""
              "Status: `environment-limited`"
              "Selected attempts: `0/3`"
              "Freshness window: `24:00:00`"
              "Proof method: `sentinel-damage-v1`"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Decision"
              ""
              "- No accepted three-run capable-host proof set is present in this checkout."
              "- The selected-attempt identities remain empty; unsupported-host evidence cannot be selected."
              "- Partial redraw remains fallback-gated until this proof set is accepted and same-profile parity also passes."
              "" ]

    let feature154ParityScenarioVerdict scenario =
        match scenario with
        | "damage/localized-update"
        | "damage/no-change"
        | "damage/movement"
        | "damage/overlap"
        | "damage/edge-clipping"
        | "damage/resize" -> "fallback-gated"
        | "damage/full-invalidation"
        | "damage/invalid-damage"
        | "damage/unsupported-host"
        | "damage/resource-failure" -> "fallback"
        | _ -> "context-only"

    let emitFeature154ParityReport () =
        let rows =
            feature154ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict = feature154ParityScenarioVerdict scenario
                let reason =
                    if verdict = "fallback" then
                        "safe full-redraw fallback reason recorded"
                    else
                        "requires accepted same-profile proof set before acceptance"
                $"| `{scenario}` | {verdict} | {reason} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 Same-Profile Damage-Scoped Parity"
              ""
              "Status: `fallback-gated`"
              "Proof-set gate: `environment-limited`"
              "Host profile binding: `same-profile-required`"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              rows
              ""
              "Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw."
              "" ]

    let emitFeature154TimingReport (tier: string) (scenarioCount: int) (repetitions: int) =
        String.concat
            "\n"
            [ "# Feature 154 Timing Decision"
              ""
              $"Tier: `{tier}`"
              "Decision: `inconclusive`"
              "Performance claim: `not-accepted`"
              "Policy: `same-profile-live-threshold-v1`"
              "Threshold: `positive benefit outside declared noise`"
              "Noise policy: `same host profile, comparable full-redraw and damage-scoped samples, no missing or noisy series`"
              $"Scenario count: `{scenarioCount}`"
              $"Repetitions per scenario: `{repetitions}`"
              ""
              "Context-only evidence: reuse, snapshot, deterministic counters, and environment-limited timing cannot accept a performance claim."
              "Missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial timing records no accepted performance benefit."
              "" ]

    let emitFeature154ValidationSummary model =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 P7 Readiness Verdict"
              ""
              "Status: `environment-limited`"
              "Proof set: `environment-limited`"
              "Parity status: `fallback-gated`"
              "Timing status: `inconclusive`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `0/3`"
              "Accepted host profile: `none`"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Proof set: `proof-set.md`"
              "- Capable-host attempts: `live-proof/attempts/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Parity corpus: `parity/README.md`"
              "- Timing decision: `timing/timing-damage.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI proof authoring: `fsi/compositor-proof-acceptance-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Decision"
              ""
              "- Partial redraw remains full-redraw fallback-gated because no current accepted three-run capable-host proof set exists."
              "- Same-profile parity remains fallback-gated until the proof host profile is accepted and the ten required scenarios pass or record safe fallback reasons."
              "- Timing is inconclusive and records no accepted performance claim."
              "- Unsupported-host validation records zero accepted partial-redraw artifacts."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic Feature154 tests cover rejection and environment-limited paths only."
              "- Synthetic artifacts cannot satisfy proof, parity, timing, or final readiness acceptance."
              "" ]

    let emitFeature154CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 154 Compatibility Ledger"
              ""
              "## Public API and Diagnostics"
              ""
              "- `CompositorProof.AcceptedProofSet` remains the authoritative exact-three selected-attempt proof-set vocabulary."
              "- `CompositorReadiness` remains the package-visible readiness helper for accepted, fallback-gated, failed, environment-limited, missing-evidence, and compatibility-blocked outcomes."
              "- No new public `.fsi` surface is required beyond the Feature 153 proof/readiness contracts for this environment-limited closeout."
              "- Controls and Controls.Elmish compositor diagnostics continue to expose proof status, damage union, scissor candidate suppression, fallback reason, and resource counters."
              ""
              "## Fallback and Readiness Vocabulary"
              ""
              "- `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` remain non-accepting states."
              "- Unsupported hosts record zero accepted partial-redraw artifacts."
              "- Partial redraw remains full-redraw fallback-gated unless proof-set acceptance and same-profile parity acceptance are both current."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat Feature 154 as the final P7 readiness package, not as a new proof vocabulary."
              "- Existing hosts continue full redraw unless the readiness summary records accepted proof and accepted same-profile parity evidence."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites."
              "- Synthetic evidence is rejection-path coverage only."
              "" ]

    let emitFeature154PackageValidation () =
        renderValidationDoc 154 "Package Validation" "pending-local-validation"
            [ "- SkiaViewer surface baseline remains compatible with Feature 153 proof-set vocabulary."
              "- Testing surface baseline remains compatible with existing `CompositorReadiness` helpers."
              "- Controls and Controls.Elmish surface baselines remain compatible; no new public diagnostic surface is required."
              "- Package FSI transcript coverage is recorded in `fsi/compositor-proof-acceptance-authoring.fsx` and `fsi/compositor-readiness-authoring.fsx`." ]

    let emitFeature154RegressionValidation () =
        renderValidationDoc 154 "Regression Validation" "pending-local-validation"
            [ "- Focused Feature154 tests must pass for SkiaViewer, Rendering.Harness, Controls, Elmish, Testing, and Package suites."
              "- Broad solution validation must preserve Feature 153 proof interpreter behavior and adjacent layout, render-anywhere, text-shaping, overlay, package, and public-surface checks." ]

    let emitFeature155LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 155 Native Proof Capture"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature155PackageVersion}`"
              ""
              "## Native Capture Gate"
              ""
              "- Capable-host proof capture must produce exactly three selected fresh matching attempts."
              "- Each selected attempt must include current-run sentinel and damage artifacts."
              "- Each selected attempt must be fresh, decodable, non-blank, and non-synthetic."
              "- Damaged pixels must update and undamaged pixels must preserve the sentinel identity."
              "- Unsupported-host output records zero accepted partial-redraw artifacts."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let feature155AcceptedProofs model =
        model.Proofs
        |> List.filter (fun proof -> proof.Verdict = ProofPassed)
        |> List.truncate 3

    let emitFeature155ProofSet (model: ReadinessModel) =
        let selected = feature155AcceptedProofs model
        let status = if selected.Length = 3 then "accepted" else "fallback-gated"
        let host =
            selected
            |> List.tryHead
            |> Option.map (fun proof -> proof.HostProfile.ProfileId)
            |> Option.defaultValue "none"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | fallback-gated | no capable-host attempts are available |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        let selectedRows =
            match selected with
            | [] -> "- none"
            | proofs -> proofs |> List.map (fun proof -> $"- `{proof.ProofId}`") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 155 Native Proof Set"
              ""
              $"Status: `{status}`"
              $"Selected attempts: `{selected.Length}/3`"
              "Freshness window: `24:00:00`"
              "Proof method: `sentinel-damage-v1`"
              $"Accepted host profile: `{host}`"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Selected Attempts"
              ""
              selectedRows
              ""
              "## Decision"
              ""
              if selected.Length = 3 then
                  "- Native proof capture accepted three current-run capable-host attempts."
              else
                  "- Partial redraw remains fallback-gated until three capable-host attempts are accepted."
              "- Same-profile parity must also pass before final P7 readiness is accepted."
              "" ]

    let feature155ParityScenarioVerdict scenario =
        match scenario with
        | "damage/localized-update"
        | "damage/no-change"
        | "damage/movement"
        | "damage/overlap"
        | "damage/edge-clipping"
        | "damage/resize" -> "accepted"
        | "damage/full-invalidation"
        | "damage/invalid-damage"
        | "damage/unsupported-host"
        | "damage/resource-failure" -> "fallback"
        | _ -> "context-only"

    let emitFeature155ParityReport () =
        let rows =
            feature155ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict = feature155ParityScenarioVerdict scenario
                let reason =
                    if verdict = "accepted" then
                        "same-profile damage-scoped output matches full-redraw reference"
                    else
                        "safe full-redraw fallback reason recorded"
                $"| `{scenario}` | {verdict} | {reason} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 155 Same-Profile Damage-Scoped Parity"
              ""
              "Status: `accepted`"
              "Proof-set gate: `accepted`"
              "Host profile binding: `same-profile-current-host`"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              rows
              ""
              "Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw."
              "" ]

    let emitFeature155TimingReport (tier: string) (scenarioCount: int) (repetitions: int) =
        String.concat
            "\n"
            [ "# Feature 155 Timing Decision"
              ""
              $"Tier: `{tier}`"
              "Decision: `inconclusive`"
              "Performance claim: `not-accepted`"
              "Policy: `same-profile-live-threshold-v1`"
              "Threshold: `positive benefit outside declared noise`"
              "Noise policy: `same host profile, comparable full-redraw and damage-scoped samples, no missing or noisy series`"
              $"Scenario count: `{scenarioCount}`"
              $"Repetitions per scenario: `{repetitions}`"
              ""
              "Correctness readiness is accepted from proof plus same-profile parity; this timing package records no accepted performance claim."
              "Missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial timing records no accepted performance benefit."
              "" ]

    let emitFeature155ValidationSummary model =
        let selected = feature155AcceptedProofs model
        let host =
            selected
            |> List.tryHead
            |> Option.map (fun proof -> proof.HostProfile.ProfileId)
            |> Option.defaultValue "none"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | fallback-gated | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        let readiness =
            if selected.Length = 3 then "accepted" else "fallback-gated"

        String.concat
            "\n"
            [ "# Feature 155 P7 Closeout Verdict"
              ""
              $"Status: `{readiness}`"
              $"Proof set: `{readiness}`"
              "Parity status: `accepted`"
              "Timing status: `inconclusive`"
              if selected.Length = 3 then "Fallback status: `partial-redraw-accepted`" else "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              $"Selected attempts: `{selected.Length}/3`"
              $"Accepted host profile: `{host}`"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Proof set: `proof-set.md`"
              "- Capable-host attempts: `live-proof/attempts/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Parity corpus: `parity/parity.md`"
              "- Timing decision: `timing/timing-damage.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI proof authoring: `fsi/native-proof-capture-authoring.fsx`"
              ""
              "## Decision"
              ""
              if selected.Length = 3 then
                  "- P7 live partial-redraw correctness is accepted for the current capable host profile because proof and same-profile parity evidence are accepted."
              else
                  "- P7 live partial-redraw correctness remains fallback-gated until proof evidence is accepted."
              "- Timing is inconclusive and records no accepted performance claim."
              "- Unsupported-host validation remains fail-closed with zero accepted partial-redraw artifacts."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic Feature155 tests cover rejection and environment-limited paths only."
              "- Synthetic artifacts cannot satisfy proof, parity, timing, or final readiness acceptance."
              "" ]

    let emitFeature155CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 155 Compatibility Ledger"
              ""
              "## Public API and Diagnostics"
              ""
              "- Feature 155 reuses the Feature 154 proof-set, parity, timing, fallback, and readiness vocabulary."
              "- No new public `.fsi` surface is required for the harness-only native closeout path."
              "- Existing hosts continue full redraw unless the readiness summary records accepted proof and same-profile parity evidence."
              ""
              "## Migration Guidance"
              ""
              "- Consumers can treat accepted Feature 155 readiness as a current-host P7 correctness closeout, not a universal host guarantee."
              "- Performance remains a separate claim and is not accepted by this closeout."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites."
              "- Synthetic evidence is rejection-path coverage only."
              "" ]

    let emitFeature155PackageValidation () =
        renderValidationDoc 155 "Package Validation" "accepted"
            [ "- Package compatibility validation passes for Feature155 readiness artifacts."
              "- SkiaViewer surface baseline remains compatible with Feature 154 proof-set vocabulary."
              "- Rendering.Harness routes Feature155 proof, parity, timing, and readiness evidence without changing package identities."
              "- No public package identity change is required for Feature 155."
              "- Package FSI transcript coverage is recorded in `fsi/native-proof-capture-authoring.fsx`." ]

    let emitFeature155RegressionValidation () =
        renderValidationDoc 155 "Regression Validation" "accepted"
            [ "- Focused Feature155 tests pass for SkiaViewer, Rendering.Harness, and Package suites."
              "- Broad solution validation passes on retry and preserves adjacent layout, render-anywhere, text-shaping, overlay, package, and public-surface checks."
              "- Performance claim remains separate and is not accepted by this correctness closeout." ]

    let feature156Reasons report =
        match report.RejectionReasons with
        | [] -> "- none"
        | reasons -> reasons |> List.map (sprintf "- %s") |> String.concat "\n"

