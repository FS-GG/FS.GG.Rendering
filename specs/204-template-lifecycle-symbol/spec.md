# Feature Specification: Lifecycle Choice Symbol for the fs-gg-ui Template

**Feature Branch**: `204-template-lifecycle-symbol`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" — resolved to Coordination board item **P1 · rendering — Add `lifecycle` choice symbol (spec-kit|sdd|none) + conditions; default spec-kit byte-identical** (workstream: Lifecycle; contract: `fs-gg-ui-template`; phase P1 Rendering).

## Overview

The `fs-gg-ui` project template currently always emits a full Spec Kit lifecycle alongside the generated product: the `.specify/` workspace, a project constitution, the agent skill/context files (`.agents/`, `.claude/`), and the generated agent-context tree. That lifecycle is appropriate when someone scaffolds a standalone product and drives it through Spec Kit directly. It is **not** appropriate when the product is being composed by an external orchestrator (the `fsgg-sdd scaffold` path, which owns and supplies its own lifecycle), nor when a consumer simply wants a bare product with no governance lifecycle attached.

This feature adds a single `lifecycle` choice to the template so the caller can declare which lifecycle the generated product should carry. The default value preserves today's output exactly (byte-for-byte), so every existing caller and every existing profile test is unaffected. Opting into a non-default value suppresses the Spec-Kit lifecycle scaffolding, leaving the generated product itself untouched.

This is the foundational P1 Rendering task: the "Publish FS.GG.UI.Template carrying the new parameter" board item is blocked on it, and the downstream P2 SDD composition epic (`scaffold --provider rendering --param lifecycle=sdd`) consumes the value introduced here.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Default callers see no change (Priority: P1)

A team that already scaffolds products with `fs-gg-ui` (via `dotnet new fs-gg-ui` or the equivalent scaffold invocation) runs the template exactly as they do today, without mentioning the new lifecycle option. They receive the same files they have always received — the generated product plus the full Spec Kit lifecycle, the constitution, and the agent skill/context files — with no differences whatsoever.

**Why this priority**: The non-regression guarantee is the gate for the entire feature. If the default output drifts by a single byte, existing automation, profile tests, and downstream consumers break. Every other story depends on this being airtight.

**Independent Test**: Scaffold each existing profile (`app`, `headless-scene`, `governed`, `sample-pack`) without supplying a lifecycle value and confirm the produced tree is byte-identical to the pre-feature output, and that all existing profile/template tests pass unmodified.

**Acceptance Scenarios**:

1. **Given** a caller scaffolds any existing profile and does not pass a lifecycle value, **When** generation completes, **Then** the produced file tree is byte-identical to the output produced before this feature existed.
2. **Given** the existing profile and template test suites, **When** they run against the template after this feature is added, **Then** they pass without any modification to the tests.
3. **Given** a caller explicitly passes the default lifecycle value, **When** generation completes, **Then** the output is identical to passing no lifecycle value at all.

### User Story 2 - Compose a product under an external lifecycle owner (Priority: P2)

An orchestrator (the SDD scaffold path) generates a product and intends to supply the lifecycle itself. It requests the template with the lifecycle value that means "I will own the lifecycle." The template emits the generated product **without** the Spec-Kit lifecycle scaffolding (`.specify/`, the constitution, the agent skill/context files, and the generated agent-context tree are all suppressed), so there is nothing for the orchestrator to collide with or have to delete.

**Why this priority**: This is the reason the feature exists — it unblocks the downstream composition work. It is P2 rather than P1 only because it cannot be safely shipped until the default-preservation guarantee (P1) is proven.

**Independent Test**: Scaffold a profile with the "external lifecycle owner" value and confirm the generated product is present and buildable while none of the gated lifecycle artifacts are emitted.

**Acceptance Scenarios**:

1. **Given** a caller requests a profile with the external-lifecycle-owner value, **When** generation completes, **Then** the generated product source, project files, and product tests are present and the product builds.
2. **Given** the same request, **When** generation completes, **Then** the `.specify/` workspace, the constitution, the agent skill/context files, and the generated agent-context tree are all absent from the output.

### User Story 3 - Scaffold a bare product with no lifecycle (Priority: P3)

A consumer who wants only the rendered product, with no governance or Spec Kit lifecycle attached and no expectation that an external orchestrator will add one, requests the template with the "no lifecycle" value. They receive the generated product alone.

**Why this priority**: A useful, low-cost completion of the option set, but not on the critical path for the composition work that motivates the feature.

**Independent Test**: Scaffold a profile with the "no lifecycle" value and confirm only the generated product is produced, with the lifecycle scaffolding suppressed.

**Acceptance Scenarios**:

1. **Given** a caller requests a profile with the no-lifecycle value, **When** generation completes, **Then** the generated product is present and the lifecycle scaffolding is absent.

### Edge Cases

- **Unknown lifecycle value**: A caller supplies a value outside the allowed set. The template MUST reject the request rather than silently producing partial or default output, consistent with how the template already rejects unknown `designSystem` values.
- **Lifecycle combined with every profile**: The lifecycle choice MUST behave consistently across all profiles (`app`, `headless-scene`, `governed`, `sample-pack`); suppressing the lifecycle MUST NOT remove or alter any profile-specific product content.
- **Lifecycle combined with other opt-ins**: The lifecycle choice MUST compose cleanly with the existing `designSystem` and `feedback` options without one silently overriding the other's effect.
- **Suppressed-but-referenced artifacts**: When the lifecycle scaffolding is suppressed, no remaining emitted file may contain a dangling reference that assumes those suppressed files exist (e.g., instructions pointing at a `.specify/` workspace that was not produced).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The template MUST expose a single `lifecycle` choice with exactly three allowed values: `spec-kit`, `sdd`, and `none`.
- **FR-002**: The default value of the `lifecycle` choice MUST be `spec-kit`, and producing output with the default MUST be byte-identical to the template's output before this feature was introduced, for every profile.
- **FR-003**: When `lifecycle` is `spec-kit`, the template MUST emit the full lifecycle scaffolding exactly as today: the `.specify/` workspace, the project constitution, the agent skill files (`.agents/` and `.claude/`), and the generated agent-context tree.
- **FR-004**: When `lifecycle` is `sdd` or `none`, the template MUST suppress the gated lifecycle scaffolding — the `.specify/` workspace, the constitution, the agent skill/context files, and the generated agent-context tree — while still emitting the complete generated product (source, project files, and product tests) for the selected profile.
- **FR-005**: Suppressing the lifecycle scaffolding MUST NOT change any non-lifecycle output: the generated product produced for a given profile under `sdd`/`none` MUST be the same product content that profile produces today, minus only the gated lifecycle files.
- **FR-006**: The template MUST reject an unrecognized `lifecycle` value with a clear failure rather than falling back to a default or producing partial output.
- **FR-007**: The `lifecycle` choice MUST be available for, and behave consistently across, all four profiles.
- **FR-008**: The `lifecycle` choice MUST compose with the existing `designSystem` and `feedback` options so that any valid combination produces the union of their intended effects with no silent overrides.
- **FR-009**: The existing profile and template test suites MUST continue to pass without modification, and the feature MUST add coverage demonstrating that each non-default lifecycle value suppresses exactly the gated set and nothing else.
- **FR-010**: The `lifecycle` choice MUST be discoverable and self-describing to a caller inspecting the template's options (each value carries a human-readable description of what it emits/suppresses), consistent with the template's existing options.

### Key Entities *(include if feature involves data)*

- **Lifecycle choice**: A named template option with three mutually exclusive values (`spec-kit`, `sdd`, `none`) and a default of `spec-kit`. Determines whether the lifecycle scaffolding is emitted.
- **Gated lifecycle scaffolding**: The set of artifacts controlled by the lifecycle choice — the `.specify/` workspace, the project constitution, the agent skill/context files (`.agents/`, `.claude/`), and the generated agent-context tree. Emitted in full for `spec-kit`; suppressed entirely for `sdd` and `none`.
- **Generated product**: The rendered product for a selected profile (source, project files, product tests, and profile-specific content). Independent of the lifecycle choice — never altered by it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scaffolding any of the four profiles with the default lifecycle produces output that is 100% byte-identical to the pre-feature output (zero diff).
- **SC-002**: The existing profile and template test suites pass with zero modifications after the feature is added.
- **SC-003**: Scaffolding any profile with `sdd` or `none` emits zero files from the gated lifecycle set and 100% of that profile's non-lifecycle product files.
- **SC-004**: Every supported combination of `lifecycle` (3 values) × profile (4 values) generates successfully, and every unsupported `lifecycle` value fails fast with a clear error (0% silent fallbacks).
- **SC-005**: A caller can determine, from the template's own option descriptions alone, which value to pass for each of the three lifecycle intents without consulting external documentation.

## Assumptions

- **Default value is `spec-kit`**: Chosen so the existing, implicit behavior maps onto the new default and today's output is preserved byte-for-byte (the same "no-diff default" precedent the template already uses for `designSystem=wcag` and `feedback=false`).
- **Gated set follows the board item literally**: The lifecycle choice gates the four artifact groups named in the board item — `.specify/`, the constitution, `.agents/`/`.claude/`, and the generated agent-context tree. The constitution is currently delivered via the generated agent-context tree (`.template.config/generated/.specify/memory/constitution.md`) and is suppressed as part of that group.
- **`sdd` and `none` suppress the same template-level files**: At the template's own output level, both non-default values suppress the identical gated set; the distinction between them is the *declared intent* the value carries (a downstream orchestrator recognizes `sdd` and supplies its own lifecycle, whereas `none` expects no lifecycle to be added). The template is not responsible for emitting a separate SDD "skeleton" — that is produced by the downstream scaffold (P2 SDD epic), not by this template change. **Decision (accepted, 2026-06-27): no template-emitted `sdd` skeleton/marker — `sdd` and `none` suppress the identical template-level set.** This locks research CC-3; reopen via `/speckit-clarify` only if a distinct `sdd` marker is later required.
- **All agent skills are part of the gated set**: Following the board item's inclusion of `.agents/`, the agent skill files (including product-authoring skills) are suppressed for non-`spec-kit` lifecycles; under `sdd`, the downstream scaffold re-supplies whatever skills the composed product needs. **Decision (accepted, 2026-06-27): gate ALL `.agents/`/`.claude/` skill sources — product-authoring skills included — for non-`spec-kit` lifecycles** (the literal board-item reading; all 8 product-skill + 2 sample-pack skill source entries are gated). This locks research CC-2; reopen via `/speckit-clarify` only if product-authoring skills should survive while only Spec-Kit/governance files are gated (which would narrow the gated-source map and un-gate the product-skill entries).
- **Constitution ownership for `sdd` is settled downstream**: The open cross-repo P0 decision "constitution ownership for lifecycle=sdd products (Rendering vs SDD)" governs *who* supplies the constitution when `lifecycle=sdd`. This feature only ensures the template stops emitting its own constitution for non-`spec-kit` values; it does not decide downstream ownership.
- **Existing options unchanged**: The `profile`, `designSystem`, `feedback`, and git-initialization options keep their current behavior and defaults; this feature adds the lifecycle choice without altering them.
- **Git-init/chmod post-actions are out of scope**: The separate board item "Move git-init/chmod out of template post-actions into scaffold path" is tracked independently and is not part of this feature.
