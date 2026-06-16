# Feature Specification: Design-System Template Parameter (--designSystem wcag/ant)

**Feature Branch**: `128-design-system-template-param`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "next item in fs.gg" → Workstream F3: `--designSystem wcag|ant` template parameter + `TemplateCheck`/generated-product validation.

## Overview

Workstream F2 delivered a selectable **color policy** abstraction (`wcag`, `ant`) inside the framework — the *rules* a design system's colors are held to. But that choice currently lives only inside the framework's own tests; a person scaffolding a brand-new product from the framework's project template has no way to *declare which design language their product should be governed by*. They get the WCAG-only behavior by default with no alternative and no record of the decision.

This feature makes the design-system policy a **first-class scaffolding choice**. When a maintainer creates a new product from the framework's project template, they may select the governing color policy by name (`wcag` or `ant`). The generated product then carries that decision: its color/contrast governance evaluates against the chosen policy, and the choice is recorded in the product so the project is self-describing.

Two guarantees frame the work:

- **`wcag` is the compatibility default.** Generating a product without choosing a policy (or explicitly choosing `wcag`) produces a project byte-identical to what the template produces today. No existing scaffolding workflow changes.
- **`ant` is a real, validated alternative.** Choosing `ant` imprints the Ant-derived design language (the F1 tokens and the F2 `ant` policy) onto the generated product, and the product validates its colors against Ant's rules rather than the WCAG-only gate.

> **Scope guardrail (from the plan):** the governance gate "keeps its name but delegates to the selected policy." Selecting `ant` must change which *policy* governs the generated product — not merely swap a palette. The deliverable is the scaffolding choice plus the validation that proves both generated variants are correct.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scaffold a product with a chosen design-system policy (Priority: P1)

A maintainer creating a new product from the framework's project template wants to declare, at creation time, which design language governs the product's colors. They pass a design-system option naming the policy (`wcag` or `ant`). If they pass nothing, the product is governed by `wcag`, exactly as today. The generated product records the chosen policy so the decision is visible in the project itself, not only in the command that created it.

**Why this priority**: This is the core capability — turning the F2 policy into a selectable scaffolding parameter. Without it there is no template surface for the design-system choice at all, and the validation in US2/US3 has nothing to exercise. The default-to-`wcag` guarantee is what keeps every existing scaffolding workflow unchanged.

**Independent Test**: Generate a product without the option and assert it is identical to today's template output (no new or changed files); generate one with the option set to each accepted value and assert the generated product records the corresponding policy. Delivers a working, backward-compatible scaffolding choice on its own.

**Acceptance Scenarios**:

1. **Given** a maintainer scaffolds a product without specifying a design-system policy, **When** the product is generated, **Then** it is governed by `wcag` and is identical to the product the template produces today (no diff introduced by this feature).
2. **Given** a maintainer scaffolds a product and selects `wcag` explicitly, **When** the product is generated, **Then** the result is identical to the no-option default.
3. **Given** a maintainer scaffolds a product and selects `ant`, **When** the product is generated, **Then** the generated product records `ant` as its governing design-system policy.
4. **Given** a maintainer supplies an unrecognized design-system value, **When** they attempt to scaffold, **Then** generation is rejected with a clear message listing the accepted values; no product is generated with a silently substituted policy.

---

### User Story 2 - A generated product validates its colors against the selected policy (Priority: P2)

A maintainer who scaffolded a product with a chosen policy wants the product's own color/contrast governance to enforce *that* policy. A product created with `wcag` enforces the WCAG contrast rules (today's behavior); a product created with `ant` enforces Ant Design's contrast expectations over the Ant-derived colors. In both cases the governance gate keeps its established name and role — only the policy it delegates to changes with the scaffolding choice.

> **F3 scope note (altitude):** In F3 the generated product does **not** *execute* the policy itself — `ColorPolicy` remains `internal` and the runtime style-resolver restyle is deferred to **F4**. "The product's governance" is therefore evaluated **framework-side, keyed by each product's recorded choice** (the F2 engine run over the recorded policy string). The acceptance scenarios below describe the governing verdicts that evaluation must produce; making the generated product run the policy at its own runtime is F4's job, not F3's.

**Why this priority**: This is the proof that the parameter selects *policy*, not a cosmetic preset. It is what makes the choice meaningful in the generated product, and it depends on US1 having imprinted the choice, so it is P2.

**Independent Test**: Generate a product under each policy and run that product's color/contrast governance; assert the `wcag` product applies the WCAG thresholds and the `ant` product applies Ant's thresholds, and that the `ant` product passes the Ant pairings (which a WCAG-only gate would not all certify).

**Acceptance Scenarios**:

1. **Given** a product generated with `wcag`, **When** its color/contrast governance runs, **Then** it applies the WCAG policy and reaches the same verdicts the framework reaches today.
2. **Given** a product generated with `ant`, **When** its color/contrast governance runs, **Then** it applies the Ant policy over the Ant-derived colors and passes the Ant pairings.
3. **Given** the same color pairing in a `wcag` product and an `ant` product, **When** each product's governance evaluates it, **Then** the governing authority recorded with the verdict reflects the product's selected policy, and any disagreement is attributable to the policy rather than a different color.

---

### User Story 3 - Validate that both generated variants build and govern correctly (Priority: P3)

A framework maintainer maintaining the template wants assurance that *both* design-system variants actually produce a correct, buildable product — so the parameter can never silently ship a broken `ant` (or regressed `wcag`) scaffold. A repeatable check generates a product under each accepted policy value, confirms it builds, and confirms its color/contrast governance reports the expected verdicts for that policy.

**Why this priority**: This is the durability/quality net around the parameter. It turns "the option exists" into "the option is proven for every accepted value." It depends on US1 and US2 being in place to validate, so it is P3.

**Independent Test**: Run the generated-product validation for each accepted policy value and assert each generated product builds successfully and its governance reports pass for that policy; tampering (e.g., an `ant` product that fails its Ant pairings, or a `wcag` product that diverges from today) is detected as a failure.

**Acceptance Scenarios**:

1. **Given** the template validation, **When** it generates a product for each accepted policy value, **Then** every generated product builds successfully without manual intervention.
2. **Given** a generated `wcag` product, **When** validation compares it to the template's current default output, **Then** they are equivalent (the `wcag` path introduces no regression).
3. **Given** a generated `ant` product, **When** validation runs its color/contrast governance, **Then** the governance reports the product is conformant under the Ant policy.
4. **Given** the set of accepted design-system values, **When** the parameter's choices are enumerated, **Then** the validation covers every accepted value (no accepted value ships unvalidated).

---

### Edge Cases

- **No design-system value supplied** → `wcag` is applied as the default; generated output is identical to today.
- **Unrecognized / misspelled value** → scaffolding is rejected with a clear message listing accepted values; never silently substituted with another policy.
- **`ant` product validated against WCAG-only expectations** → not the contract; the `ant` product's governance authority is the Ant policy, and where Ant and WCAG diverge the product discloses Ant as the authority (no-overclaim, consistent with F2).
- **Case / formatting of the supplied value** → handled per the template's existing parameter-choice conventions, consistent with the existing profile parameter, rather than inventing a new matching rule.
- **Future policies (material, fluent)** → out of scope here; the parameter's accepted set is `wcag` and `ant` for this feature, but the shape must not preclude adding them later.
- **Interaction with the existing profile parameter** → the design-system choice is orthogonal to the product profile; any profile may be generated under either policy.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The project template MUST expose a design-system selection parameter that accepts the policy by name, with `wcag` and `ant` as the accepted values for this feature.
- **FR-002**: The design-system parameter MUST default to `wcag` when no value is supplied.
- **FR-003**: Generating a product with the default (or an explicit `wcag`) MUST produce output identical to what the template produces today — this feature introduces no diff on the default path.
- **FR-004**: Generating a product with `ant` MUST imprint the Ant design language on the product: the Ant-derived colors (F1) as its tokens and the `ant` policy (F2) as its governing color policy.
- **FR-005**: The generated product MUST record its selected design-system policy so the project is self-describing (the choice is discoverable from the project, not only from the creation command).
- **FR-006**: A generated product's color/contrast governance gate MUST delegate to the product's selected policy while retaining its established name and role; selecting a policy changes which rules the gate enforces, not the gate's identity.
- **FR-007**: An unrecognized design-system value MUST be rejected at scaffolding time with a clear message listing the accepted values; the system MUST NOT silently fall back to a different policy.
- **FR-008**: There MUST be a repeatable validation that generates a product for each accepted policy value, confirms each builds successfully, and confirms each product's color/contrast governance reports the expected verdicts for its policy.
- **FR-009**: The validation MUST cover every accepted design-system value, so no accepted value can ship unvalidated.
- **FR-010**: The `ant`-generated product MUST pass the Ant color pairings under its governance, and the `wcag`-generated product MUST reach the same verdicts the framework reaches today.
- **FR-011**: This feature MUST be behavior-neutral for existing consumers and for the framework's own public package surface: the default scaffolding path is unchanged, and no public package API changes (the policy machinery remains as delivered in F2; this feature wires it to the template only).
- **FR-012**: This feature MUST NOT introduce any React, DOM, web, or icon-font dependency into the template or generated products; Ant remains a design language only, expressed in the framework's existing color/contrast primitives.

### Key Entities *(include if feature involves data)*

- **Design-System Parameter**: A named scaffolding choice on the project template selecting the governing color policy. Attributes: a stable parameter name, an enumerated accepted value set (`wcag`, `ant`), a default (`wcag`), and the disposition each value imprints on the generated product. Designed so additional values (material, fluent) can be added without reshaping it.
- **Generated Product Policy Record**: The selected policy as recorded inside a generated product, making the product self-describing about which design language governs its colors. Attributes: the policy name and the governance behavior it activates.
- **Generated-Product Validation**: A repeatable check that, for each accepted policy value, generates a product, confirms it builds, and confirms its color/contrast governance reports the expected verdicts. Attributes: coverage of every accepted value, build success, and policy-conformance verdicts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product scaffolded with no design-system value, and one scaffolded with `wcag` explicitly, are byte-identical to each other and to today's template output — zero diff introduced on the default path.
- **SC-002**: A product scaffolded with `ant` records `ant` as its governing policy and uses the Ant-derived colors; this is discoverable from the generated project itself.
- **SC-003**: A generated `wcag` product's color/contrast governance reaches verdicts identical to the framework's current behavior, and a generated `ant` product's governance passes the Ant pairings.
- **SC-004**: For at least one color pairing, the `wcag` and `ant` generated products reach different verdicts (evaluated framework-side over each product's recorded policy in F3 — see the US2 scope note), demonstrating the parameter selects rules (policy) rather than a palette.
- **SC-005**: An unrecognized design-system value is rejected 100% of the time at scaffolding with a clear message listing accepted values, and never resolves to a different policy.
- **SC-006**: The generated-product validation covers every accepted policy value (currently 2 of 2) and each generated product builds successfully and reports the expected governance verdicts.
- **SC-007**: The change adds zero public-surface delta (the framework's surface-baseline gate is green without new public rows) and leaves the framework's existing rendered/gallery output and pass/skip test counts unchanged.

## Assumptions

- **F3 scope boundary**: This feature delivers the `--designSystem wcag|ant` template parameter and the generated-product/template validation only. It builds on F2's `ColorPolicy` abstraction (`wcag`/`ant`) and F1's Ant token taxonomy, both already landed. Migrating the design-system style resolver onto the policy is deferred to F4; promoting the policy/token surface to the public package API with a decision record is deferred to F5; Ant interaction-pattern docs and the agent skill are deferred to F6 (optional).
- **Reuse of F1/F2, no new color machinery**: The `ant` path reuses F1's Ant-derived tokens and F2's `ant` policy verbatim; the `wcag` path reuses the existing contrast gate. This feature introduces no new color values or new policy rules — it selects among what already exists.
- **Template-mechanism reuse**: The design-system selection reuses the project template's existing parameter/choice mechanism (the same machinery as the existing profile choice) rather than inventing a new selection or matching scheme, so behavior (defaulting, validation of unknown values, casing) is consistent with the established template conventions.
- **Default path is a true no-op**: The `wcag`/default path must produce a byte-identical scaffold to today; the safe way to guarantee this is for the default to induce no change to generated content, consistent with how other optional template parameters (e.g. feedback) default to "no diff."
- **Governance gate keeps its name**: The generated product's color/contrast governance gate retains its established identity and simply delegates to the selected policy, so existing documentation and workflows referring to the gate stay valid.
- **Validation runs the real build**: Generated-product validation generates an actual product per accepted value and builds it (consistent with the existing package/template validation that scaffolds and builds a consumer), so a broken variant cannot ship undetected.
- **Provenance**: The Ant design-language rules trace to the Ant Design adoption analysis in the archived `EHotwagner/FS-Skia-UI` repo; adoption is recorded in provenance per the cross-cutting rules, with `FS.Skia.UI.*` identifiers rebranded to `FS.GG.UI.*`. The `FS.Skia.UI.* → FS.GG.UI.*` rebrand is an **explicit prior release decision** carried by F1/F2 (already landed in this repo); F3 reuses the already-rebranded identifiers verbatim and introduces **no new identity change**. (Note: the constitution's "package identity stays `FS.Skia.UI.*`" clause still reflects the pre-rebrand baseline; recording the completed rebrand there requires a **separate, explicit constitution amendment** — out of scope for this feature, but flagged so the divergence is traceable rather than silent.)
