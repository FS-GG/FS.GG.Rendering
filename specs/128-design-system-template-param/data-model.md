# Phase 1 Data Model: Design-System Template Parameter (F3)

F3 adds **no new F# types** to any package (the F1 tokens and F2 `ColorPolicy` engine are reused
verbatim, both `internal`). The "entities" here are the **template parameter**, the **recorded policy
marker** in a generated product, and the **validation report** — the artifacts the feature introduces.

---

## E1 — Design-System Parameter

The scaffolding choice on the project template selecting the governing color policy.

| Attribute | Value | Source / constraint |
|---|---|---|
| Parameter name | `designSystem` | `template.json` `symbols` (FR-001); kebab CLI form `--designSystem` |
| Datatype | `choice` | reuses `profile`'s machinery (assumption: template-mechanism reuse) |
| Accepted values | `wcag`, `ant` | FR-001; enumerable from the symbol's `choices` (drives validation coverage, FR-009) |
| Default | `wcag` | FR-002 |
| Disposition — `wcag` | emits **no** new content | byte-identical to today (FR-003/SC-001); recorded by documented default-by-absence |
| Disposition — `ant` | fires conditional sources → copies `template/design-system/ant/` overlay | imprints Ant tokens (F1) + Ant policy (F2) record (FR-004) |
| Unknown value | rejected at `dotnet new`, accepted set surfaced, **no** substitution | FR-007/SC-005 (native `choice` validation) |
| Extensibility | new values add a `choices` entry + an overlay dir; shape unchanged | edge case "future policies"; no reshaping required |

**Relationship to `profile`**: orthogonal — any `profile` × any `designSystem` is valid; no behaviour
fork, no control duplication (edge case "interaction with profile").

---

## E2 — Generated Product Policy Record (self-describing marker)

What a generated product carries so it is self-describing about its governing policy.

| Attribute | `wcag` product | `ant` product |
|---|---|---|
| On-disk record | **none** (default-by-absence) | `design-system.json` + copied `docs/reports/color-policy-ant.md` |
| `policy` field | n/a (implicit `wcag`) | `"ant"` (FR-005/SC-002) |
| `authority` field | n/a (implicit WCAG-certified) | `"AntExpectation"` (mirrors F2 `Authority`) |
| Discoverable from project | yes — absence ⇒ `wcag` default | yes — explicit record file |
| Governs at runtime in F3? | no (F4 wires the resolver) | no (F4) — record is data only |

`design-system.json` shape (illustrative literal — declarative, no runtime consumer in F3):

```json
{ "policy": "ant", "authority": "AntExpectation" }
```

The record's `policy` value MUST be a member of E1's accepted set and MUST round-trip through
`ColorPolicy.byName` to the matching F2 policy (the validation asserts this — E3).

---

## E3 — Generated-Product Validation (the contract gate)

A repeatable check that every accepted value yields a correct, buildable, correctly-governed product.

| Attribute | Definition | Requirement |
|---|---|---|
| Covered values | every member of E1's `choices` (currently `wcag`, `ant`) | FR-009/SC-006 — fails if any accepted value is unrun |
| Per-value: scaffold | `dotnet new fs-gg-ui --designSystem <v>` succeeds | US3-1 |
| Per-value: no-diff (`wcag`) | scaffold byte-equals today's default scaffold | FR-003/SC-001/US3-2 |
| Per-value: record (`ant`) | scaffold contains E2 record with `policy == "ant"` | FR-005/SC-002 |
| Per-value: build | real `dotnet build` succeeds | FR-008/US3-1 (assumption: real build) |
| Per-value: verdicts | `ColorPolicy.byName(recorded) |> evaluate catalog` matches committed `docs/reports/color-policy-<v>.md`; `overall` is FAIL for `wcag` (1 failing pairing) and PASS for `ant` | FR-008/FR-010/SC-003 |
| Divergence | ≥1 pairing's outcome differs between the `wcag` and `ant` products (the F2 `primary-hover-fg-on-surface` pairing) | FR-006/SC-004 |
| No-overclaim | `ant`'s certifying-where-WCAG-fails verdict carries Ant authority, not WCAG | FR-010 (F2 `AuthorityNote`) |
| Report artifact | `readiness/design-system-template-validation.md` (deterministic) | asserted by the always-on gate test |
| Package neutrality | framework surface baselines unchanged; no new public rows | FR-011/SC-007 |

**Verdict oracle**: the committed F2 reports `docs/reports/color-policy-wcag.md` (overall **FAIL**) and
`docs/reports/color-policy-ant.md` (overall **PASS**, 1 out-of-scope, no-overclaim note) — reused as the
single source of truth, not re-derived.

---

## Reused F2 entities (unchanged — referenced, not redefined)

From `src/Color/ColorPolicy.fs` (`module internal`, no `.fsi`). The env-gated regenerator script reaches
these by `#load`-ing the F2 source closure (`ColorPolicy.fs` + the design-system `Pairing` catalog source)
so the engine compiles into the **script's own assembly** — same-assembly access, so no
`InternalsVisibleTo` is required (an `fsx` is not the `Controls.Tests` assembly and cannot rely on its IVT
grant):

- `ColorPolicy` (`Name`, `Label`, `Authority`, `Threshold`, `Classify`), with `wcag` and `ant` values.
- `byName : string -> Result<ColorPolicy, string>` (the recorded `policy` string → policy; the rejection
  path mirrors FR-007 at the engine level too).
- `evaluate` / `overall` / `renderReport` over the design-system `Pairing` catalog.

F3 changes none of these.
