# Contract: Generated-Product Design-System Validation

The repeatable check proving every accepted `designSystem` value yields a correct, buildable,
correctly-governed product (US3). Two layers, mirroring the repo's existing
`GeneratedConsumerValidationTests` (always-on report assert) + `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE`
(env-gated heavy live op).

## Layer 1 — Always-on gate test

`tests/Package.Tests/Feature128DesignSystemTemplateTests.fs` — deterministic, GL-free, no `dotnet new`.
Asserts the committed report `specs/128-design-system-template-param/readiness/design-system-template-validation.md`.

Required report content (string-asserted, the contract tokens):

```text
covered-values: wcag, ant            # equals the template's designSystem choice set (FR-009/SC-006)
wcag: build=pass diff-vs-today=none overall=FAIL authority=WcagCertified   # FR-003/SC-001, today's verdicts
ant:  build=pass record=ant          overall=PASS authority=AntExpectation # FR-004/FR-005/SC-002/SC-003
divergent-pairing: primary-hover-fg-on-surface  wcag=Fail ant=Aa           # SC-004 (policy, not palette)
no-overclaim-note: ant: not WCAG-certified       # FR-010
result: pass
```

| ID | Assertion | Maps to |
|---|---|---|
| GV-1 | `covered-values` equals the enumerated `designSystem` choices (no accepted value missing). | FR-009/SC-006 |
| GV-2 | Every covered value reports `build=pass`. | FR-008/SC-006 |
| GV-3 | `wcag` reports `diff-vs-today=none` and `overall=FAIL` matching `docs/reports/color-policy-wcag.md`. | FR-003/FR-010/SC-001/SC-003 |
| GV-4 | `ant` reports `record=ant` and `overall=PASS` matching `docs/reports/color-policy-ant.md`. | FR-004/FR-005/SC-002/SC-003 |
| GV-5 | A divergent pairing is reported with opposite outcomes under the two policies. | FR-006/SC-004 |
| GV-6 | The no-overclaim authority note is present for `ant`. | FR-010 |
| GV-7 | `result: pass` only when GV-1..GV-6 hold; a missing/failed variant ⇒ loud failure. | US3; Principle VI |
| GV-8 | Report missing ⇒ test **fails** (failing-first before the regenerator runs). | Principle V |

## Layer 2 — Env-gated live regenerator

`scripts/validate-design-system-template.fsx` — runs the real work; gated by an env flag
(e.g. `FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1`), the `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE` precedent.

For **each** value `v` in the enumerated `designSystem` choice set:

1. `dotnet new fs-gg-ui --name <Tmp> --designSystem <v> -o <tmpdir>` → must succeed.
2. If `v == "wcag"`: byte-compare the scaffold tree against a no-value scaffold ⇒ **identical** (records
   `diff-vs-today=none`). If `v == "ant"`: assert the `design-system.json` record has `policy == "ant"`.
3. `dotnet build` the scaffold ⇒ success (records `build=pass`).
4. Resolve the recorded policy: `ColorPolicy.byName <recorded>` (for `wcag`, the documented default).
   Run `evaluate catalog |> overall` and `renderReport`; compare to the committed
   `docs/reports/color-policy-<v>.md` oracle ⇒ must match (records `overall`/`authority`/divergence/note).
5. Fail the run if any accepted value was not processed (coverage guard, FR-009).

Writes `readiness/design-system-template-validation.md` deterministically (sorted value order, invariant
culture, no wall-clock content) so Layer 1's assertions are stable and drift is detectable.

## Invariants

- **Real evidence**: a real `dotnet new` + real `dotnet build` per value; verdicts from the real F2 engine
  over real F1 tokens. No mocked scaffold/build (Principle V).
- **Single verdict oracle**: per-policy verdicts come from F2's drift-gated `docs/reports/`; the validation
  reuses them rather than re-deriving (no second source of truth).
- **Zero package-surface delta**: the run asserts the framework surface baselines are unchanged
  (FR-011/SC-007).
- **Determinism**: identical inputs ⇒ byte-identical report.
