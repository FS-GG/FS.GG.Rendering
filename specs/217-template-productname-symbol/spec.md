# Feature Specification: fs-gg-ui `productName` Scaffold Symbol

**Feature Branch**: `217-template-productname-symbol`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "start the next unblocked Rendering item on the coordination board." → Coordination board item FS-GG/FS.GG.Rendering#27: *"fs-gg-ui rejects --productName from the SDD scaffold-provider (exit 127) — add a productName symbol or align the name param."*

## Background *(context, not requirements)*

The cross-repo composition path lets an external Spec-Driven-Development (SDD) lifecycle owner scaffold a rendering product through a single orchestrated command:

```
fsgg-sdd scaffold --provider rendering --param productName=Acme
```

Today this fails. The SDD provider-runner names the product by passing `--productName`, but the `fs-gg-ui` project template exposes no such option — it takes the product name through the built-in `--name`/`-n` flag. The templating engine rejects the unknown option and aborts with exit code 127 ("invalid template option(s)"), so the two repos do not compose.

The template payload itself is healthy: instantiating with the correct name flag and building is clean. This feature closes the **parameter-surface mismatch only** — it makes `fs-gg-ui` accept `productName` as the authoritative product name, conforming to the SDD scaffold-provider naming convention used uniformly across providers. The chosen resolution is the **Rendering side** (option 1 in the request), which keeps the `productName` convention consistent for every provider.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - SDD scaffold-provider composition succeeds (Priority: P1)

An SDD lifecycle owner runs the orchestrated composition command, supplying the product name the SDD way (`--param productName=Acme`). The rendering provider instantiates `fs-gg-ui`, the template accepts the name, and a correctly-named, buildable product is produced.

**Why this priority**: This is the entire reason the item exists. Without it the rendering provider cannot be driven from SDD at all (hard failure, exit 127), which blocks FS.GG.Templates#30 (composition CI: full scaffold). Delivering only this story already unblocks the downstream consumer.

**Independent Test**: Run the full composition path against the org feed (`fsgg-sdd scaffold --provider rendering --param productName=Acme`, or equivalently the underlying `dotnet new fs-gg-ui … --productName Acme`) and confirm it (a) exits successfully instead of 127 and (b) emits a product whose project/namespace/identifier reflect `Acme`, which then builds clean.

**Acceptance Scenarios**:

1. **Given** the `fs-gg-ui` template is installed from the org feed, **When** it is instantiated with `--productName Acme` (and no `--name`/`-n`), **Then** instantiation succeeds (no "invalid option" / exit 127) and the generated product is named `Acme` throughout (project files, namespaces, slug) exactly as `-n Acme` would have produced today.
2. **Given** a generated `Acme` product from the `--productName` path, **When** it is built in Release, **Then** the build completes with no warnings and no errors.
3. **Given** the SDD provider-runner's exact invocation (`--designSystem wcag --lifecycle sdd --productName Acme --profile app`), **When** composition runs end-to-end, **Then** it produces the same buildable `Acme` product as the manual equivalent.

---

### User Story 2 - Existing name-based consumers are unaffected (Priority: P2)

A current consumer who scaffolds with `-n`/`--name` (or relies on the default name) continues to get byte-identical output. The new option is purely additive.

**Why this priority**: The change touches the public template parameter surface, which many existing flows and the default scaffold depend on. Backward compatibility is a hard constraint, but it is a guardrail on Story 1 rather than independent new value.

**Independent Test**: Generate a product the existing way (`-n Foo`, and separately with no name flag) before and after the change and diff the output; confirm no differences.

**Acceptance Scenarios**:

1. **Given** the updated template, **When** a product is instantiated with `-n Foo` and no `--productName`, **Then** the output is byte-identical to today's output for the same flags.
2. **Given** the updated template, **When** a product is instantiated with neither `-n` nor `--productName`, **Then** the default-named output is byte-identical to today's default output.
3. **Given** the updated template, **When** the full parameter matrix that exists today (`--profile`, `--designSystem`, `--lifecycle`, etc.) is exercised without `--productName`, **Then** every combination matches today's output.

---

### User Story 3 - Cross-repo contract stays coherent (Priority: P3)

A maintainer of another FS-GG repo (or a future audit) can see that the `scaffold-provider` contract now guarantees `fs-gg-ui` accepts `productName`, and that the SDD side (FS-GG/FS.GG.SDD#35) and the compatibility projection agree.

**Why this priority**: The fix is a recorded `contract-change` on `scaffold-provider`. Recording it keeps the cross-repo registry/compatibility projection truthful, but the functional unblock (Stories 1–2) does not depend on the paperwork landing first.

**Independent Test**: Inspect the cross-repo contract/compatibility registry and confirm an entry records the `scaffold-provider` change (rendering now honors `productName`) and references the coordinating SDD issue.

**Acceptance Scenarios**:

1. **Given** the contract change has shipped, **When** the cross-repo contract/compatibility registry is inspected, **Then** it records that the `scaffold-provider` (rendering) contract now accepts `productName`, with the version/compatibility note reflecting an additive (backward-compatible) change.
2. **Given** the registry entry, **When** it is read alongside FS-GG/FS.GG.SDD#35, **Then** the two are consistent about which side owns the `productName` ↔ name mapping (Rendering side honors `productName`).

---

### Edge Cases

- **Both `--productName` and `-n`/`--name` supplied**: precedence MUST be defined and deterministic (see FR-005) rather than producing a confusing mixed-name product or an error that breaks composition.
- **Neither name flag supplied**: behavior MUST remain today's default (default source name), unchanged.
- **`productName` value needing normalization** (mixed case, characters that feed the lowercased project slug): the value MUST flow through the same name-derivation/casing the existing name path uses, so the slug and namespaces are consistent.
- **Empty or whitespace `productName`**: MUST be treated as "not supplied" (fall back to default) rather than producing an empty/invalid product name.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `fs-gg-ui` template MUST accept a `productName` parameter (`--productName <value>`) without rejecting it as an invalid option.
- **FR-002**: When `productName` is supplied, the generated product's name-derived artifacts (project/file names, namespaces, lowercased slug, and any place the current name substitution applies) MUST be derived from the `productName` value, producing output equivalent to supplying the same value via `-n`/`--name` today.
- **FR-003**: The generated product produced via `productName` MUST build cleanly (Release configuration, zero warnings, zero errors), i.e. `productName` only renames; it MUST NOT alter the F#/Skia/Elmish payload.
- **FR-004**: The change MUST be additive and backward-compatible: the existing `-n`/`--name` flag and the default (no-name) scaffold MUST keep working and produce byte-identical output to today when `productName` is not supplied.
- **FR-005**: When both `productName` and `-n`/`--name` are supplied, the template MUST apply a single, documented precedence so the result is a consistently-named product (never a half-renamed mix) and never the exit-127 invalid-option failure.
- **FR-006**: An empty or whitespace-only `productName` MUST be treated as not supplied (fall back to the default name) rather than yielding an empty/invalid product name.
- **FR-007**: The end-to-end SDD composition path (`fsgg-sdd scaffold --provider rendering --param productName=…`, mirroring the SDD provider-runner's invocation) MUST succeed and yield the expected named, buildable product.
- **FR-008**: The `scaffold-provider` contract change MUST be recorded in the cross-repo contract/compatibility registry (and compatibility projection), marked as additive/backward-compatible, and cross-referenced with the coordinating SDD issue (FS-GG/FS.GG.SDD#35) so the two repos stay coherent.

### Key Entities *(include if feature involves data)*

- **`productName` parameter**: the new, additive product-name input on the `fs-gg-ui` template; conforms to the SDD scaffold-provider naming convention and feeds the same name derivation the built-in name flag uses.
- **`scaffold-provider` contract (rendering)**: the cross-repo contract describing how the SDD scaffolder drives the rendering template; this feature extends its accepted parameter surface.
- **Generated product name surface**: the set of name-derived outputs (project/file names, namespaces, lowercased slug) that must reflect the chosen name regardless of which flag supplied it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The SDD composition command for the rendering provider with `productName` succeeds 100% of the time (0 exit-127 failures) where it previously always failed.
- **SC-002**: A product scaffolded via `productName=Acme` is named `Acme` across 100% of its name-derived artifacts and builds in Release with 0 warnings and 0 errors.
- **SC-003**: For every existing parameter combination scaffolded without `productName`, output is byte-identical to the pre-change template (0 diffs), confirming full backward compatibility.
- **SC-004**: A product scaffolded with `productName=Acme` and one scaffolded with `-n Acme` (same other flags) are byte-identical (0 diffs), confirming the two name paths converge.
- **SC-005**: The cross-repo registry/compatibility projection reflects the `scaffold-provider` change and references FS-GG/FS.GG.SDD#35, with no remaining contract incoherence between the rendering and SDD sides.

## Assumptions

- **Resolution side**: The request's preferred option 1 (Rendering side) is adopted — `fs-gg-ui` honors `productName` — rather than the SDD-side remap (option 2, tracked in FS-GG/FS.GG.SDD#35). The SDD issue remains the coordination point but no SDD code change is required for this feature.
- **Precedence (FR-005)**: When both name inputs are given, an explicitly-supplied `productName` is treated as the authoritative product name. (If consumers expect the opposite, this is the one decision to revisit in planning.)
- **Convention source of truth**: The SDD `productName` convention is taken from the SDD provider fixtures (`tests/fixtures/scaffold-provider/ok/.template.config/template.json` in the SDD repo) as cited in the request.
- **Registry location**: The cross-repo contract/compatibility registry referenced by FR-008 is the org-level coordination registry (per the FS-GG cross-repo coordination protocol in `FS-GG/.github`), not a file in this repository; the projection update is performed there.
- **Validation toolchain**: End-to-end validation uses the same toolchain proven in the request (`fsgg-sdd` ≥ 0.2.0 and `FS.GG.UI.Template` from the org feed), against the org package feed.
- **Scope boundary**: This feature only reconciles the name/parameter surface. It introduces no new product capability, profile, design system, or lifecycle behavior, and does not modify the generated F#/Skia/Elmish payload.
