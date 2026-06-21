# Contract — descriptor-keyed CLI command table (US2)

**Scope**: `tools/Rendering.Harness/Cli.(fsi/fs)`. Replaces the 12 `isFeature###` predicates and the
`if/elif` dispatch chains with a command table keyed by `FeatureDescriptor`, and extracts the shared
performance/readiness runner once. No shipped surface change (FR-008).

## Surface (harness-internal)

```fsharp
// Feature selection from CLI args routes through the catalog, not per-feature predicates:
val selectFeature : string list -> FeatureDescriptor option   // reads "--feature <alias>", uses tryByAlias

// The per-feature command bodies become entries keyed by descriptor + command kind:
type CommandKind = CompositorReadiness | CompositorPerformance | CompositorDamage | // …existing kinds
val dispatch : CommandKind -> FeatureDescriptor -> string list -> int   // returns exit code
```

The shared runner that the ~400-line per-feature handlers duplicate (probe host → derive profile →
classify reason → write artifacts → exit code) is extracted to a single parameterized body driven by the
descriptor + `FeatureConfig`; feature-specific steps remain as small hooks where genuinely divergent.

## Behavioral contract

| # | Guarantee |
|---|---|
| C-CT-1 | For **every** existing per-feature command invoked with its existing arguments, **stdout, stderr, and exit code are byte-for-byte identical** to the pre-refactor harness (FR-004, SC-002, US2 acceptance). |
| C-CT-2 | The artifacts a command writes (`readiness/**`) are byte-identical to baseline (shares the US1 oracle). |
| C-CT-3 | Feature selection accepts exactly the alias set the old `isFeature###` predicates accepted (`"NNN"`, `"featureNNN"`, slug), via `FeatureDescriptor.tryByAlias` (C-FD-4). |
| C-CT-4 | The legacy fall-through (`runLegacyCompositorReadinessCmd` for features below the table) is preserved for any feature not in the catalog — no command silently changes target. |
| C-CT-5 | The shared performance/readiness runner emits the same metrics and readiness verdicts as the per-feature bodies it replaces (US2 acceptance scenario 2). |
| C-CT-6 | A genuinely-divergent per-feature command body that would grow or obscure under the shared runner is retained as an explicit hook (FR-007), recorded with rationale. |

## Verification

- C-CT-1 / C-CT-5: capture stdout/stderr/exit code of **every** per-feature command before and after
  (`quickstart.md` lists the command matrix); `diff` must be empty.
- C-CT-2: shared with the US1 byte-diff of `readiness/**`.
- C-CT-3 / C-CT-4: `Rendering.Harness.Tests` assertions on alias resolution and legacy fall-through.
- C-CT-6: FR-007 retentions recorded in the plan's Implementation Outcome.
