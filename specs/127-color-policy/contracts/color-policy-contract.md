# Contract: Color Policy Engine (internal)

**Module**: `module internal ColorPolicy` in `src/Color/ColorPolicy.fs` (namespace `FS.GG.UI.Color`,
**no `.fsi`**). Reached by `Controls.Tests` via `InternalsVisibleTo`. Reuses `Color`, `Role`,
`Verdict`, `ContrastResult`, and the `Contrast` module from the same assembly. **Zero** public-surface
delta (FR-012/SC-006).

## Types (indicative F#)

```fsharp
namespace FS.GG.UI.Color

module internal ColorPolicy =

    /// What a policy claims as its authority (drives no-overclaim disclosure, FR-010).
    type Authority =
        | WcagCertified        // the wcag policy
        | AntExpectation       // the ant policy (not WCAG-certified)

    /// Tri-state-plus outcome of one pairing under one policy (FR-011).
    type PolicyOutcome =
        | Passed
        | Failed
        | OutOfScope           // pairing not in this policy's validated set — NOT a pass
        | Indeterminate        // non-solid / unmeasurable (mirrors Verdict.Indeterminate)

    /// A named, selectable rule set.
    type ColorPolicy =
        { Name: string                         // "wcag" | "ant" (stable identity, FR-001)
          Label: string                        // human-readable, for report headers
          Authority: Authority
          Threshold: Role -> float             // required minimum ratio per role
          Classify: Role -> float -> Verdict } // ratio+role -> verdict (wcag delegates to Contrast.verdict)

    /// One validated pairing (catalog entry).
    type Pairing =
        { Name: string
          Foreground: Color
          Background: Color
          Role: Role }

    /// Result of evaluating one pairing under one policy.
    type PairingResult =
        { Pairing: string
          Measured: float                      // Contrast.ratio post-composite; nan for Indeterminate
          Threshold: float option              // None for Decorative/exempt
          Outcome: PolicyOutcome
          Verdict: Verdict
          AuthorityNote: string option }       // Some when policy certifies a WCAG-failing pairing (FR-010)
```

## Operations

```fsharp
    /// The two delivered policies.
    val wcag: ColorPolicy        // Authority = WcagCertified; Classify = Contrast.verdict (byte-identical, FR-002)
    val ant:  ColorPolicy        // Authority = AntExpectation; Ant thresholds (FR-004)

    /// Default applied when no policy is chosen (FR-003).
    val defaultPolicy: ColorPolicy   // = wcag

    /// Resolve by name; explicit failure on unknown — NO silent fallback (FR-006/SC-005).
    val byName: name: string -> Result<ColorPolicy, string>

    /// Is this pairing in the policy's validated set? (drives OutOfScope, FR-011)
    val inScope: policy: ColorPolicy -> pairing: Pairing -> bool

    /// Evaluate one pairing under one policy (composites alpha via Contrast.compositeOver first).
    val evaluatePairing: policy: ColorPolicy -> pairing: Pairing -> PairingResult

    /// Evaluate a whole catalog -> per-pairing results (catalog order preserved).
    val evaluate: policy: ColorPolicy -> catalog: Pairing list -> PairingResult list

    /// Overall pass = no Failed rows (OutOfScope/Indeterminate listed, not counted as fail).
    val overall: results: PairingResult list -> bool

    /// Deterministic, human-readable report (FR-008). Single evaluator shared with the tests.
    val renderReport: policy: ColorPolicy -> catalog: Pairing list -> string
```

## Behavioral guarantees (must be tested)

1. **wcag ≡ today (FR-002/SC-001)**: for every catalog pairing, `(evaluatePairing wcag p).Verdict`
   equals `Contrast.check p.Role p.Background p.Foreground |> _.Verdict` — byte-for-byte; and
   `wcag.Classify` *is* `Contrast.verdict` (delegation, not a copy).
2. **ant ≠ wcag (FR-005/SC-002)**: ≥1 catalog pairing has a different `Outcome`/`Verdict` under `ant`
   than under `wcag`, with identical color inputs (difference attributable to the policy).
3. **Default (FR-003)**: `defaultPolicy = wcag`.
4. **Unknown name (FR-006/SC-005)**: `byName "Wcag"`, `byName "material"`, `byName ""` → `Error _`
   (never an `Ok` of a different policy).
5. **ant families covered (SC-003)**: catalog under `ant` includes pairings for primary, success,
   warning, error, info, text-on-surface — each yields a `PairingResult` with a threshold + measured +
   verdict.
6. **Out-of-scope (FR-011)**: a pairing with `inScope policy p = false` evaluates to
   `Outcome = OutOfScope` (never `Passed`).
7. **No-overclaim (FR-010)**: an `ant` pairing that WCAG would `Fail` carries `AuthorityNote = Some _`.
8. **Alpha (edge case)**: a semi-transparent foreground is composited over its background
   (`Contrast.compositeOver`) before measurement — alpha is never ignored.

## Ant threshold table (authored literals, provenance-traced)

> Final numbers fixed during implementation from the FS-Skia-UI Ant adoption analysis (provenance per
> spec Assumptions). Shape: a `Role -> float` table whose values differ from WCAG's `7.0/4.5/3.0` role
> gates enough that ≥1 shared design-system pairing changes verdict. Recorded here as the contract so
> the unit test and the report cannot drift from one another (single source = this module).

| Role | wcag required | ant required (illustrative — finalize in impl) |
|---|---|---|
| `Text` | 4.5 (Aa) / 7.0 (Aaa) / 3.0 (AaLarge) | Ant body-text expectation (distinct from WCAG gate) |
| `GraphicOrUi` | 3.0 | Ant component foreground/border expectation |
| `Decorative` | exempt | exempt |

## Non-goals (out of scope for F2)

- No public `.fsi`/promotion (F5). No `--design-system` CLI/template flag (F3). No resolver migration
  (F4). No new project, no new dependency, no DesignSystem dep change.
