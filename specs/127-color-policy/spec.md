# Feature Specification: Color Validation Policies (wcag / ant)

**Feature Branch**: `127-color-policy`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "next item in fs.gg" → Workstream F2: `ColorPolicy` abstraction + `wcag` (compat) and `ant` policies; policy unit tests + generated policy report.

## Overview

Today the framework judges color choices one way only: a fixed WCAG contrast gate, wired directly into the design system. There is no way for a project to declare *which* design language's color rules should govern it. Workstream F1 introduced an Ant-derived color **token taxonomy** (the colors); this feature introduces the matching **policy** (the rules those colors are held to).

A *color policy* is a named, selectable set of rules that decides whether the design system's color pairings (text-on-surface, control foreground/background, semantic success/warning/error feedback) are acceptable. This feature delivers two policies:

- **`wcag`** — the existing, accessibility-certified contrast behavior, reproduced exactly. This is the default and preserves current behavior for every existing consumer.
- **`ant`** — Ant Design's own semantic color and contrast expectations, which differ from WCAG thresholds. Choosing `ant` changes the *rules a color must satisfy*, not merely the palette.

Each policy can be evaluated against the design system's colors to produce a deterministic, human-readable **policy report**: every validated pairing, the rule applied, the measured contrast, and the verdict. The report doubles as documentation and as drift-checked evidence.

> **Scope guardrail (from the plan):** selecting `ant` must change *policy*, not just colors — otherwise it is a "paint preset," not a real design-system choice. Validation semantics are the deliverable here.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validate the design system's colors against a named policy (Priority: P1)

A framework maintainer wants to confirm that the colors the framework ships are acceptable under a chosen design language's rules. They select a color policy by name and evaluate it against the design system's color pairings, getting a per-pairing pass/fail verdict and an overall result. With no choice made, the `wcag` policy applies and the result is identical to today's behavior.

**Why this priority**: This is the core capability — a selectable rule set evaluated over the framework's colors. Without it there is no policy abstraction at all, and the `ant` option and the report have nothing to build on. The `wcag`-as-default guarantee is what makes the change safe to land for existing consumers.

**Independent Test**: Evaluate the default (`wcag`) policy over the current design-system color pairings and assert the verdicts match the framework's existing contrast results exactly (byte-for-byte, including thresholds and role classifications). Delivers a working, backward-compatible policy mechanism on its own.

**Acceptance Scenarios**:

1. **Given** no policy is explicitly chosen, **When** the design system's colors are validated, **Then** the `wcag` policy is applied and every verdict matches the framework's current contrast behavior with no change in outcome.
2. **Given** the `wcag` policy, **When** a color pairing meets the required contrast ratio for its role, **Then** the pairing is reported as passing; **and** when it falls below the required ratio, it is reported as failing.
3. **Given** a policy is identified by name, **When** an unknown policy name is requested, **Then** the system rejects it explicitly (no silent fallback to a different policy).

---

### User Story 2 - Hold colors to Ant Design's rules with the `ant` policy (Priority: P2)

A maintainer evaluating Ant Design as the framework's design language selects the `ant` policy and validates the Ant-derived semantic colors (primary, success, warning, error, info, and the text/surface families from F1) against Ant's own contrast and color-usage expectations — which are *not* the same as the WCAG thresholds. Where the two policies disagree on a given pairing, the difference is explicit and attributable to the policy, not to a color change.

**Why this priority**: This is the proof that a policy is a *rule set*, not a palette swap. It validates the anti-scope-creep guardrail and is the first concrete non-WCAG policy, establishing the pattern future policies (material, fluent) plug into. It depends on US1's abstraction, so it is P2.

**Independent Test**: Evaluate the `ant` policy over the Ant-derived semantic color pairings and assert (a) it applies Ant's thresholds/expectations rather than the WCAG ones, and (b) at least one pairing receives a verdict under `ant` that differs from its verdict under `wcag`, proving the policy — not the color — drove the outcome.

**Acceptance Scenarios**:

1. **Given** the `ant` policy, **When** Ant's semantic color families are validated, **Then** each family (primary / success / warning / error / info / text-on-surface) is evaluated under Ant's stated contrast expectation and assigned a verdict.
2. **Given** the same color pairing, **When** it is validated under `wcag` and then under `ant`, **Then** the policy applied is recorded with the verdict, and any disagreement between the two is attributable to the policy rules rather than a different color input.
3. **Given** the `ant` policy validates a color it does not certify as WCAG-conformant, **When** the result is reported, **Then** the report discloses that `ant` (not WCAG certification) is the authority for that verdict.

---

### User Story 3 - Generate a policy report as documentation and evidence (Priority: P3)

A maintainer (or reviewer reading the docs) wants a single, readable artifact that enumerates, for a given policy, every color pairing it validates: the rule/threshold applied, the measured contrast, and the verdict. The report is generated deterministically from the policy and the design-system colors, regenerated on demand, and checked for drift so it never silently diverges from the actual rules — mirroring the token-generation gate established in F1.

**Why this priority**: The report turns the policy from internal machinery into reviewable, durable evidence and documentation. It is valuable but depends on US1/US2 producing verdicts to report, so it is P3.

**Independent Test**: Generate the report for both policies and assert it lists every validated pairing with rule, measured value, and verdict; then run the drift check and assert the committed report matches a freshly generated one (exit success on no drift, failure on tampering).

**Acceptance Scenarios**:

1. **Given** a policy, **When** the report is generated, **Then** it contains one entry per validated color pairing showing the rule applied, the measured contrast, and the verdict, plus an overall pass/fail summary.
2. **Given** a committed report, **When** the drift check runs and the committed report matches a freshly generated one, **Then** the check passes; **and** when they differ, the check fails and identifies the divergence.
3. **Given** the report is generated twice from the same inputs, **When** the two outputs are compared, **Then** they are byte-identical (deterministic, no wall-clock or random content).

---

### Edge Cases

- **No policy selected** → the `wcag` policy is applied as the default, preserving today's behavior exactly.
- **Unknown / misspelled policy name** → rejected explicitly with a clear message; never silently substituted with another policy.
- **A color pairing not covered by a policy** → disclosed as out-of-scope/unvalidated for that policy rather than silently passed.
- **Semi-transparent colors** → composited over their background before measurement (consistent with the existing contrast machinery) so alpha is never ignored.
- **`ant` validates a color WCAG would fail** → permitted, but the report discloses that `ant` is the authority and the pairing is not WCAG-certified (no overclaim).
- **A pairing fails its policy** → surfaced as a failing verdict; the feature reports the failure rather than mutating colors to force a pass.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST provide a named, selectable color-policy abstraction. A policy is identified by a stable name and defines the set of color pairings it validates, the rule/threshold applied to each, and the verdict produced.
- **FR-002**: The framework MUST provide a `wcag` policy that reproduces the current contrast-validation behavior exactly — same role classifications, same required ratios, same verdicts — so that existing consumers see no change in outcome.
- **FR-003**: The `wcag` policy MUST be the default applied when no policy is explicitly selected.
- **FR-004**: The framework MUST provide an `ant` policy that validates the Ant-derived semantic color families (primary, success, warning, error, info, and text/surface pairings) under Ant Design's own contrast and color-usage expectations.
- **FR-005**: Selecting a policy MUST change the validation rules applied, not only the colors evaluated. The two policies MUST be able to reach different verdicts for the same color pairing, attributable to the policy rather than a different color input.
- **FR-006**: Requesting an unknown policy name MUST be rejected explicitly; the system MUST NOT silently fall back to a different policy.
- **FR-007**: Each policy MUST be evaluable against the design system's color pairings to produce a per-pairing verdict and an overall pass/fail summary.
- **FR-008**: The framework MUST generate a deterministic, human-readable policy report per policy, enumerating every validated pairing with the rule applied, the measured contrast, and the verdict, plus an overall summary. Regenerating from identical inputs MUST produce byte-identical output.
- **FR-009**: The policy report MUST be drift-checkable: a check MUST confirm the committed report matches a freshly generated one and fail (identifying the divergence) when they differ.
- **FR-010**: Where a policy reaches a verdict not certified by WCAG (notably under `ant`), the report MUST disclose that the policy — not WCAG conformance — is the authority for that verdict (no-overclaim).
- **FR-011**: A color pairing outside a policy's validated set MUST be disclosed as out-of-scope/unvalidated for that policy rather than silently treated as passing.
- **FR-012**: This feature MUST be behavior-neutral for existing consumers: under the default `wcag` policy, rendered output and existing contrast results MUST be unchanged, and the public package surface MUST NOT change (the policy abstraction lands internally, consistent with F1; public promotion is deferred to a later feature).
- **FR-013**: The feature MUST NOT introduce any React, DOM, web, or icon-font dependency; Ant is adopted as a design language only, expressed in the framework's existing color/contrast primitives.

### Key Entities *(include if feature involves data)*

- **Color Policy**: A named rule set (e.g. `wcag`, `ant`) that governs how color pairings are judged. Attributes: a stable identity/name, the threshold/rule it applies per pairing role, a human-readable label for reports, and the authority it claims (e.g. WCAG-certified vs. Ant's own expectation). New policies (material, fluent) are intended to plug into the same shape later.
- **Validated Pairing**: A foreground/background (or semantic feedback) color relationship that a policy evaluates. Attributes: the two colors, the role (text / UI element / decorative / semantic feedback), the measured contrast, the policy threshold, and the resulting verdict.
- **Policy Report**: A deterministic, human-readable artifact summarizing one policy's evaluation across all its validated pairings, including per-pairing detail, authority disclosure, and an overall pass/fail summary. Regenerable and drift-checked.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With no policy selected, validating the design system's colors yields verdicts identical in every respect (roles, thresholds, pass/fail) to the framework's current contrast behavior — zero behavioral change for existing consumers.
- **SC-002**: At least two named policies (`wcag`, `ant`) are selectable, and for at least one color pairing the two policies produce different verdicts, demonstrating the choice changes rules rather than colors.
- **SC-003**: Every Ant-derived semantic color family (primary, success, warning, error, info, text-on-surface) is validated by the `ant` policy and appears in its report with a rule, a measured value, and a verdict.
- **SC-004**: The policy report is byte-identical across two regenerations from the same inputs, and the drift check passes against the committed report and fails when the report is tampered with.
- **SC-005**: An unknown policy name is rejected 100% of the time with a clear message and never resolves to a different policy.
- **SC-006**: The change adds zero public-surface delta (the surface-baseline gate is green without new public rows) and leaves rendered/gallery output and existing pass/skip test counts unchanged.

## Assumptions

- **F2 scope boundary**: This feature delivers the policy *abstraction*, the `wcag` and `ant` policies, the unit tests, and the generated policy report. Wiring policy selection to a template/CLI parameter (`--design-system wcag|ant`) is explicitly **out of scope** and deferred to F3; migrating the design-system resolver onto the policy is deferred to F4; promoting the policy/token surface to the public package API (with a decision record) is deferred to F5.
- **Internal-first delivery (Tier 2)**: Following the F1 precedent, the policy abstraction lands as internal, behavior-neutral machinery (reachable by the test project) with no public-surface change. Public promotion is a later, separate decision (F5). This keeps the change low-risk and the surface-drift gate green.
- **Reuse of existing contrast machinery**: The `wcag` policy reuses the framework's existing contrast measurement (relative luminance, ratio, role thresholds, alpha compositing) rather than reimplementing it, which is what guarantees byte-identical default behavior.
- **Reuse of F1 tokens**: The `ant` policy draws on the Ant-derived semantic color families introduced by F1's token taxonomy as its inputs; it does not introduce new color values of its own.
- **Ant as authority for its own policy**: Under the `ant` policy, Ant Design's stated contrast/color expectations are the authority; the `ant` policy is not required to be WCAG-certified, and the report discloses this where the two diverge.
- **Deterministic, generator-driven report**: The policy report is generated by a script (mirroring F1's token generator and `--check` drift mode), is free of wall-clock/random content, and is regenerated as part of the same change rather than as a follow-up — so the drift gate stays green.
- **Provenance**: The `ant` policy rules trace to the Ant Design adoption analysis in the archived `EHotwagner/FS-Skia-UI` repo; adoption is recorded in provenance per the cross-cutting rules, with `FS.Skia.UI.*` identifiers rebranded to `FS.GG.UI.*`.
