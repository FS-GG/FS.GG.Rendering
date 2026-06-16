# Phase 0 Research: Design-System Template Parameter (F3)

All NEEDS CLARIFICATION resolved. The feature builds on F1 (`126-ant-token-taxonomy`) and F2
(`127-color-policy`), both landed. The decisions below were derived by auditing the existing template
(`.template.config/template.json`), the F2 engine (`src/Color/ColorPolicy.fs`), and the repo's existing
template/consumer validation tests.

---

## R1 — How can a generated product be "governed by the selected policy" when `ColorPolicy` is internal?

**Decision**: F3 delivers **selection + a recorded self-describing choice + framework-side validation**.
The generated product **records** its policy but does **not** call the policy engine at runtime. The
"governance gate that delegates to the selected policy" (FR-006) is realized by the framework's existing
**internal** `ColorPolicy` evaluation, run by the validation **keyed by each scaffolded product's
recorded choice**.

**Rationale**:
- `ColorPolicy` is `module internal` in `FS.GG.UI.Color` with no `.fsi`; public promotion is **explicitly
  deferred to F5** (per the spec's F3 scope boundary and the F2 plan). A generated product consumes the
  framework as NuGet packages and can reach only the **public** `Contrast` surface (`Contrast.verdict`/
  `check`, WCAG-only) — never `ColorPolicy.ant`.
- Wiring the design-system **style resolver** onto the policy is **deferred to F4**. So in F3 the product
  has no runtime path that consumes the policy at all.
- Reaching for a public policy API in F3 would drag F5's public-surface promotion (and a surface-baseline
  delta) into this feature, violating FR-011/SC-007 ("no public package API changes; zero public-surface
  delta") and the deliberate F2 internal-first posture.
- The framework repo itself **can** reach `ColorPolicy` (via `Controls.Tests` `InternalsVisibleTo`),
  exactly as the F2 report drift gate does. So the per-policy *verdict* check legitimately runs there,
  reading each scaffolded product's recorded choice and resolving it with `ColorPolicy.byName`.

**Alternatives considered**:
- *Expose `ColorPolicy` publicly now so the product evaluates its own colors* — rejected: pulls F5 into
  F3, adds public surface (breaks SC-007), couples scope.
- *Have the generated product reimplement the `ant` thresholds against public `Contrast`* — rejected:
  duplicates F2's rule table in product code (a second source of truth that drifts), and is exactly the
  "policy in two places" F2 was built to avoid.

---

## R2 — How is the `wcag`/default scaffold kept byte-identical to today while `ant` records its choice?

**Decision**: Add a `designSystem` **`choice`** parameter (default `"wcag"`, choices `wcag`/`ant`). Record
the choice **only on the `ant` path** via **conditional `sources`** (`condition: (designSystem ==
"ant")`) that copy a new `template/design-system/ant/` overlay into the product. **Edit no file under
`template/base/`.** The `wcag`/default path fires no source → emits no new content → byte-identical to
today (SC-001). `wcag` is the **documented default, identified by the absence** of an override.

**Rationale**:
- This is the **exact `feedback` precedent**: `feedback` is a default-`false` parameter whose
  `(feedback == true)` conditional sources add files only when chosen — its description literally says
  "Default false induces no diff." Mirroring it inherits a proven no-diff guarantee and keeps the template
  internally consistent.
- Editing a base file (even to add a conditional region or a `replaces` token defaulting to `wcag`) risks
  the byte-identity of a load-bearing scaffold file; a pure additive **overlay copied only for `ant`**
  touches the default path not at all — the strongest possible no-diff guarantee.
- Reconciles the three constraints that look contradictory: **FR-005** ("product MUST record its policy"),
  **SC-001** ("`wcag`/default byte-identical to today"), **SC-002** ("`ant` records `ant`"). The only
  reading satisfying all three is: materialize a concrete record on the **non-default** path; on the
  default path the policy is `wcag` by documented convention (absence of any override). The spec's own
  edge-case note ("No design-system value supplied → `wcag` … identical to today") and the assumption
  "Default path is a true no-op … other optional template parameters (e.g. feedback) default to 'no diff'"
  endorse this directly.

**Recorded marker (what the `ant` overlay contains)**:
- `design-system.json` — a small declarative record: `{ "policy": "ant", "authority": "AntExpectation" }`
  — machine-readable, discoverable from the project itself (FR-005/SC-002), drives nothing at runtime in F3.
- `docs/reports/color-policy-ant.md` — F2's committed Ant report copied in as the **Ant design-language
  imprint expressed as data** (FR-004), so the generated `ant` product carries the Ant rules/verdicts it
  is held to without any runtime policy call.

**Alternatives considered**:
- *`replaces` token in `template/base` defaulting to `wcag`* — rejected: adds a `wcag` line to today's
  default output → breaks "byte-identical to today."
- *`//#if (designSystem == "ant")` conditional region inside `Product.fsproj`* — viable (markers are
  stripped on the default path, as `profile` regions in `GovernanceTests.fs` demonstrate) but riskier
  than a pure overlay for a load-bearing file; deferred unless an in-fsproj property proves necessary.

---

## R3 — What unknown-value rejection / casing behavior does the parameter get?

**Decision**: Use the dotnet template engine's native `choice` validation — the **same** machinery as the
existing `profile` parameter. An unrecognized value is rejected at `dotnet new` time with the accepted set
surfaced; there is **no** silent fallback. Casing/format follow the engine's existing choice-matching
rules (consistent with `profile`), not a new scheme.

**Rationale**: FR-007/SC-005 require rejection-with-accepted-values and never substitution; the
`choice` datatype already does this and is the convention the spec's "template-mechanism reuse" assumption
mandates. Inventing a matching rule would contradict that assumption and add untested surface.

**Alternatives considered**: *Custom string parameter + hand-rolled validation* — rejected: reinvents
choice validation, diverges from `profile`, more code and more failure modes.

---

## R4 — How is the generated-product validation built, and how does it cover every value?

**Decision**: Reuse the repo's established **two-layer** validation pattern:
1. **Always-on gate test** (`Feature128DesignSystemTemplateTests` in `Package.Tests`) — asserts a
   committed report (`readiness/design-system-template-validation.md`) shows: coverage == every choice in
   the `designSystem` enum, both variants build-pass, `wcag` ≡ today (no-diff), `ant` passes its Ant
   pairings, and ≥1 pairing diverges by policy. Deterministic, GL-free, no `dotnet new`. This mirrors
   `GeneratedConsumerValidationTests`, which asserts a committed validation report rather than running the
   heavy op in the default tier.
2. **Env-gated live regenerator** (`scripts/validate-design-system-template.fsx`) — performs the heavy
   real work behind an env flag (the `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE` precedent): for **each** accepted
   value, `dotnet new fs-gg-ui --designSystem <p>`, real `dotnet build`, assert `wcag` scaffold == today's
   default scaffold and `ant` scaffold records `ant`, then resolve each scaffold's recorded policy with
   `ColorPolicy.byName` and run `evaluate`/`overall`/`renderReport` over the F2 catalog, comparing to the
   committed `docs/reports/color-policy-<policy>.md` oracle. Writes the report the gate asserts.

**Coverage (FR-009/SC-006)**: the regenerator **enumerates the `designSystem` choice set** (parsed from
`template.json`) and fails if any accepted value is missing from the run — so a future value (`material`,
`fluent`) cannot ship unvalidated. The gate test independently asserts the report's covered-values list
equals the enum.

**Rationale**: This is the precedent already in the codebase for template/consumer validation (heavy
`dotnet`-driven op gated by env; lightweight contract asserted always-on via a committed report). It
honors the "validation runs the real build" assumption while keeping the default `Dev`/`Verify`/`Ci`
tiers fast, and it reuses F2's reports as the single verdict oracle (no second policy source of truth).

**Alternatives considered**:
- *Run `dotnet new` + build inside the always-on test* — rejected: too slow/fragile for the default tier;
  contradicts the repo's deferred-heavy-op convention.
- *Generate a fresh per-product report instead of reusing F2's `docs/reports/`* — rejected: introduces a
  second source of truth for the same verdicts that can drift from F2's drift-gated reports.

---

## R5 — Provenance and neutrality

**Decision**: Record in `PROVENANCE.md` that the `ant` imprint **reuses F1's tokens and F2's policy
verbatim** — F3 introduces **no new color values and no new policy rules**; it only selects among what F1
and F2 already landed. The Ant design-language rules trace (via F2) to the Ant Design adoption analysis in
the archived `EHotwagner/FS-Skia-UI` repo, with `FS.Skia.UI.*` → `FS.GG.UI.*` rebranding.

**Rationale**: The cross-cutting provenance rule (spec "Provenance" assumption) requires recording adopted
design-language sources; since F3 adds no new values, the note is a reuse pointer to F1/F2, not a new
adoption. Neutrality (FR-011/SC-007) is proven by the unchanged surface baselines and unchanged base
template.

**Alternatives considered**: *Re-derive Ant values in the overlay* — rejected: would create new
unprovenance-tracked values and duplicate F1; the overlay copies F2's already-provenanced report instead.
