# Contract: Parameterized readiness validator + per-feature config (Tier 2, internal)

Replaces `Feature159Readiness`, `Feature160ThroughputReadiness`, `Feature161HostLaneReadiness`
(`src/Testing/Testing.fs`, ~lines 4194 / 4300 / 4418) with **one** validator driven by a per-feature
configuration record (FR-004). Internal — not added to `Testing.fsi` unless an existing public entry
point requires it (then the existing `validate` signatures are preserved as thin wrappers for
source-compat, FR-007).

## Shape (conceptual — exact record finalized in implementation)

```fsharp
// Per-feature configuration: the only thing that differs between 159/160/161.
type ReadinessValidatorConfig<'Check, 'Result> =
    { RequiredScenarios   : ScenarioId list           // coverage each feature demands
      RequiredArtifacts   : ArtifactId list            // absence → missing-artifact entry
      DomainChecks        : ('Check -> Diagnostic list) list   // feature-specific predicate→diagnostic rules
      UnsupportedFacts    : ('Check -> Diagnostic list)        // environment/violation facts (F161 etc.)
      DeriveStatus        : DerivationInputs -> ReadinessStatus // ordered status rule
      Blocks              : ReadinessStatus -> bool             // shared default, or per-domain override
      BuildResult         : DerivationInputs -> 'Result }       // pack verdict+diagnostics+evidence

// One validator body for all three features.
val validateReadiness : ReadinessValidatorConfig<'Check,'Result> -> 'Check -> 'Result
```

The three features become three `let feature159Config = { … }`, `feature160Config`, `feature161Config`
entries; their public `validate` functions (if surfaced) delegate: `validate = validateReadiness feature159Config`.

## Per-feature differences the config must capture (verified)

| Feature | Required coverage | Distinct domain checks |
|---------|-------------------|------------------------|
| 159 | scenario set A | parity-passed; net-saved-work count; non-beneficial vs fallback-only decision; **EnvironmentLimited blocks** (override) |
| 160 | coverage set B | `WarmupCount = 3`; `MeasuredRepetitions = 5`; sample-policy accepted; full-validation status |
| 161 | coverage set C | host-lane facts (display-server, display/renderer identity, direct-rendering, refresh, driver, package-version-set, cpu/gpu load, host profile, run/scenario/policy identities, artifact paths); unsupported facts (missing-display, indirect-rendering, software-raster, unknown-renderer, virtualized-presentation, stale-package); prior-gate statuses |

## Contract obligations

| ID | Obligation |
|----|------------|
| C-VC-1 | For each of features 159/160/161, the parameterized validator (driven by its config) produces a **byte-identical** status, diagnostics list, and missing-artifact list vs baseline (FR-004 acceptance, SC-002). |
| C-VC-2 | The three original `Feature*Readiness` modules' validator bodies are deleted; only config records + (optional) wrapper `validate`s remain (SC-002). |
| C-VC-3 | Domain-specific status cases and the Feature159 environment-limited override are preserved exactly. |
| C-VC-4 | A new same-shaped feature is expressible as one config entry with no validator-body copy (SC-006). |
| C-VC-5 | Any public `validate` entry point referenced by tests/consumers keeps a source-compatible signature. |

## Verification

- Build clean; full `dotnet test` no new failures vs baseline.
- The 159/160/161 readiness suites (status + diagnostics + missing-artifact golden assertions) stay
  green — they are the byte-for-byte oracle for this story.
