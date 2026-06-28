# Feature Specification: Close the Lifecycle-Agnostic Template Epic

**Feature Branch**: `210-lifecycle-template-closure`

**Created**: 2026-06-28

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → resolved to the P1 Rendering epic *"Make fs-gg-ui emit Spec Kit only when asked (lifecycle-agnostic template)"*. Its three child features (204 lifecycle symbol, 205 scaffold git-init/chmod, 206 publish coherent set) are all Done; this feature delivers the **Rendering-owned epic-closure work** that turns those parts into a single, defensible "epic is closeable" statement.

## Why this feature exists

The P1 epic's acceptance is *"Rendering template gains a lifecycle parameter; default spec-kit byte-identical."* The mechanics are implemented and the template is published, but the epic cannot be responsibly closed today because:

- The proof that the template **"emits Spec Kit only when asked"** is scattered across three separate child-feature readiness reports and was produced against each feature's local working tree, not consolidated against the *currently published* package.
- Consumers have no single guidance on **which lifecycle value to choose** (`spec-kit` vs `sdd` vs `none`), how to use the standalone `none` path, or how to migrate off the pre-lifecycle template.
- The genuinely-remaining work is owned by **other repos** (the SDD scaffold path and a cross-repo constitution-ownership decision). The epic's closure state must distinguish "Rendering is done" from "epic is fully done", with the remainder tracked rather than silently blocking.

This feature is **acceptance + guidance + coordination only**. It does not change the template's generated output, and it does not implement scaffold-orchestrator behavior owned by other repos.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Single epic-level acceptance against the published template (Priority: P1)

The Rendering maintainer wants one consolidated record proving the **currently published/tagged** FS.GG.UI template emits Spec Kit only when asked: `lifecycle=spec-kit` produces the full Spec Kit lifecycle surface, `lifecycle=sdd` produces an app-only product with that surface suppressed, `lifecycle=none` produces neither (and attaches no orchestrator expectation) — and the default (`spec-kit`) output is byte-identical to the pre-lifecycle template across all supported profiles. With this record the P1 epic can be closed (on the Rendering side) on evidence rather than inference.

**Why this priority**: This is the epic's actual acceptance criterion. Without a single, reproducible, version-traceable proof against the published artifact, "Done" is an assertion, not a verified fact. Everything else in this feature supports or follows from this record.

**Independent Test**: Instantiate the published/tagged template for each lifecycle value across the supported profiles; confirm the gated lifecycle file set is present/absent exactly as specified per value, and that the default output is byte-for-byte identical to the pre-lifecycle baseline. The acceptance record alone lets a reviewer reach the close/don't-close decision without opening the three child folders.

**Acceptance Scenarios**:

1. **Given** the currently published/tagged FS.GG.UI template, **When** a product is generated with `lifecycle=spec-kit` (the default), **Then** the full Spec Kit lifecycle surface is emitted and the output is byte-identical to the pre-lifecycle template baseline across every supported profile.
2. **Given** the same published template, **When** a product is generated with `lifecycle=sdd`, **Then** the Spec Kit lifecycle surface is absent and the remaining app-only product is intact and buildable.
3. **Given** the same published template, **When** a product is generated with `lifecycle=none`, **Then** neither the Spec Kit lifecycle surface nor any external-orchestrator expectation is present, and the product is intact and buildable.
4. **Given** the acceptance record, **When** a reviewer reads it, **Then** it names the exact published version and tag it was validated against and can be re-run to reproduce the result.

---

### User Story 2 - Consumer lifecycle guidance and migration note (Priority: P2)

A consumer deciding how to scaffold a product wants clear guidance: when to choose `spec-kit`, `sdd`, or `none`; what each value includes and excludes; how to use `lifecycle=none` as a standalone product with no governance or orchestrator attached; and how to move from the pre-lifecycle template to the lifecycle-aware version. With this they can adopt the template correctly without consulting a maintainer.

**Why this priority**: The capability is shipped but undiscoverable. Guidance converts a working parameter into an adoptable feature and prevents the predictable failure where a consumer picks `none` and is surprised that no governance/orchestrator is attached.

**Independent Test**: A reader who has never used the template can, from the guidance alone, pick the correct lifecycle value for each of the three scenarios (governed Spec Kit product, SDD-composed app-only product, bare standalone product) and follow the migration note to upgrade an existing consumer.

**Acceptance Scenarios**:

1. **Given** the guidance, **When** a consumer wants a governed product with the Spec Kit lifecycle, **Then** the decision tree directs them to `lifecycle=spec-kit` and states what is emitted.
2. **Given** the guidance, **When** a consumer wants an app-only product composed by the SDD scaffold, **Then** it directs them to `lifecycle=sdd` and states that an external orchestrator supplies the lifecycle/governance.
3. **Given** the guidance, **When** a consumer wants a bare product with nothing attached, **Then** it directs them to `lifecycle=none` and explicitly states no governance or orchestrator is attached and none is expected.
4. **Given** a consumer on the pre-lifecycle template, **When** they follow the migration note, **Then** they can adopt the lifecycle-aware version and reproduce their prior output by selecting the default.

---

### User Story 3 - Track the cross-repo remainder and record true closure state (Priority: P3)

The cross-repo coordinator wants the SDD-owned remainder — the scaffold path's init/permission obligations and the constitution-ownership decision for `lifecycle=sdd` — captured as tracked cross-repo requests (without duplicating ones already open), and the Coordination board updated to reflect the epic's accurate state: Rendering-side complete, with the remaining items visibly owned by other repos.

**Why this priority**: It keeps the board honest. Closing the epic outright would hide real downstream blockers; leaving it untracked loses them. This makes the epic's closure auditable and prevents silent gaps, but it depends on US1/US2 being settled first.

**Independent Test**: From the closure record and the board, a reader can enumerate every item still required for *full* epic closure, see which repo owns each, and follow a tracked request for each — with no untracked blockers and no duplicate requests.

**Acceptance Scenarios**:

1. **Given** the SDD-owned scaffold obligations, **When** the closure work runs, **Then** a tracked cross-repo request exists for them and is referenced from the closure record (reusing the existing open request rather than duplicating it).
2. **Given** the unresolved constitution-ownership decision for `lifecycle=sdd`, **When** the closure work runs, **Then** it is captured as a tracked cross-repo/decision item referenced from the closure record.
3. **Given** the Coordination board, **When** the feature completes, **Then** the P1 epic's status reflects Rendering-side completion and the remaining items are attributed to their owning repos.

---

### Edge Cases

- **Drift between child evidence and the published artifact**: acceptance MUST be run against the currently published/tagged package (not a local working tree), so a later republish that changed behavior is caught rather than masked by stale child reports.
- **`none` selected when governance is expected**: guidance MUST state plainly that `lifecycle=none` attaches no governance and no orchestrator, so a consumer cannot reasonably assume one will be added later.
- **Defining "byte-identical"**: the baseline, the set of profiles, and whether the comparison covers both file presence and file content MUST be stated explicitly so the result is unambiguous and reproducible.
- **Remainder never completed**: the closure record MUST define what "Rendering-side done" means independently of the SDD-owned remainder, so the Rendering epic state is not held hostage to another repo's schedule while still surfacing that full closure is pending.
- **Duplicate cross-repo asks**: an already-open request (e.g., the existing SDD scaffold-path request) MUST be reused, not re-filed.
- **Published version moves during the feature**: if a newer template version is published while this feature is in flight, the acceptance record MUST pin the specific version/tag it validated and that pin MUST be the one cited at closure.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST produce a single, consolidated **epic acceptance record** that verifies — against the currently published/tagged FS.GG.UI template package — the behavior of all three lifecycle values, rolling up (not merely linking) the per-value results into one close/don't-close statement.
- **FR-002**: The acceptance record MUST confirm `lifecycle=spec-kit` emits the full Spec Kit lifecycle surface.
- **FR-003**: The acceptance record MUST confirm `lifecycle=sdd` suppresses the Spec Kit lifecycle surface and yields an intact, buildable app-only product (buildability verified by the build spot-check defined in Assumptions).
- **FR-004**: The acceptance record MUST confirm `lifecycle=none` emits neither the Spec Kit lifecycle surface nor any external-orchestrator expectation, and yields an intact, buildable product (buildability verified by the build spot-check defined in Assumptions).
- **FR-005**: The acceptance record MUST confirm the default (`lifecycle=spec-kit`) output is byte-identical to the pre-lifecycle template baseline across all supported profiles, stating the baseline, the profile set, and whether presence and content are both compared.
- **FR-006**: The acceptance record MUST name the exact published version and the nearest tag anchor it was validated against — and, where no dedicated template tag exists at that version, MUST disclose that gap rather than imply a tag exists — and MUST be reproducible from that pin (reproduction depends on the feed package version, not the tag).
- **FR-007**: The feature MUST publish consumer-facing **lifecycle guidance** that includes a decision tree covering all three values and states, for each, what is included and excluded.
- **FR-008**: The guidance MUST describe the standalone use of `lifecycle=none`, explicitly stating that no governance and no orchestrator are attached or expected.
- **FR-009**: The guidance MUST include a **migration note** for consumers moving from the pre-lifecycle template to the lifecycle-aware version, including how to reproduce prior output via the default.
- **FR-010**: The feature MUST ensure the SDD-owned remainder (scaffold init/permission obligations; constitution-ownership decision for `lifecycle=sdd`) is captured as tracked cross-repo request(s)/decision item(s), reusing existing open requests rather than duplicating them, and referenced from the closure record.
- **FR-011**: The feature MUST record the epic's closure state on the Coordination board, distinguishing Rendering-side completion from items still owned by other repos.
- **FR-012**: The feature MUST NOT change the published template's generated output, and MUST NOT implement scaffold-orchestrator behavior or constitution ownership owned by other repos (scope boundary — acceptance, guidance, and coordination only).

### Key Entities *(include if feature involves data)*

- **Epic Acceptance Record**: the single consolidated artifact that proves "emit Spec Kit only when asked" against the published template. Attributes: validated version + tag, supported profile set, per-lifecycle file-set result, byte-identical result and its definition, reproduction steps, and an explicit Rendering-side close/don't-close conclusion.
- **Lifecycle Guidance**: consumer-facing material. Attributes: decision tree across the three values, per-value include/exclude description, standalone-`none` statement, migration note.
- **Cross-Repo Remainder Item**: a tracked request or decision for work owned by another repo. Attributes: owning repo, what it blocks, link to the existing/created tracking issue, referenced from the closure record.
- **Closure State**: the board-visible status of the epic. Attributes: Rendering-side status, list of remaining items with owners, distinction between Rendering-done and epic-fully-done.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reviewer can reach the Rendering-side close/don't-close decision for the epic from a single document, without opening the three child feature folders.
- **SC-002**: All three lifecycle values are verified against the published package, with 100% of gated lifecycle files correctly present or absent per value (zero misclassified files).
- **SC-003**: The default (`spec-kit`) output is byte-identical to the stated baseline across all supported profiles, with zero unexpected file or content differences.
- **SC-004**: A consumer unfamiliar with the template can select the correct lifecycle value for all three scenarios (governed, SDD-composed, standalone) using the guidance alone, with no maintainer consultation required.
- **SC-005**: Every SDD-owned remaining item has exactly one tracked cross-repo request/decision referenced from the closure record — no untracked blockers and no duplicate requests.
- **SC-006**: On feature completion, the Coordination board shows the P1 epic's accurate status (Rendering-side complete) with each remaining item attributed to its owning repo. If board access is unavailable in the run environment, SC-006 is satisfied by the disclosed `environment-limited` substitute (the recorded intended transition plus the exact `gh project` command to apply it), not by a silent skip.
- **SC-007**: The generated `sdd` and `none` products build successfully on the `app` profile (build spot-check exit 0); if the build toolchain is unavailable, the buildability line is recorded `environment-limited` (disclosed, never reported as a silent pass) and the close conclusion names the unbuilt cell.

## Assumptions

- **"Emit Spec Kit only when asked"** means: `lifecycle=spec-kit` (the default) emits the Spec Kit lifecycle surface; `lifecycle=sdd` and `lifecycle=none` suppress exactly the gated lifecycle set established by Feature 204 (the `.specify/`, agent-context, and generated lifecycle docs). This feature reuses that definition rather than redefining the gated set.
- **Acceptance validates the currently published/tagged FS.GG.UI template package** — the latest published version at implementation time (most recent template tag is `fs-gg-ui-template/v0.1.50-preview.1`; Feature 208's merge bumped the package to `0.1.51-preview.1` in the local feed). The record pins whichever version it actually validates.
- **Supported profiles** are the four profiles already exercised by Features 204 and 206; this feature does not introduce new profiles.
- **The SDD scaffold path, the constitution-ownership P0 decision, and confirmation/closure of the existing SDD-side request are owned by SDD/Coordination** and are out of scope here; this feature only tracks them and references them from the closure record.
- **No change to the template's generated output** — this feature adds acceptance evidence, consumer guidance, and coordination updates only; the published artifact's behavior is taken as-is and verified, not modified.
- **The existing open cross-repo request for the scaffold-path obligations remains the channel** for that work; new requests are filed only where no tracking item already exists.
- **"Byte-identical" is evaluated against the pre-lifecycle template output** captured by Feature 204/206 as the baseline; the record restates the baseline so it is self-contained.
- **"Buildable" is verified by a bounded build spot-check** rather than building the full 12-instantiation matrix: the harness runs `dotnet build` on the `app`-profile output for `lifecycle=sdd` and `lifecycle=none` (the two values whose "intact and buildable" is a distinct requirement) and asserts success. The `spec-kit` default's buildability follows from its byte-identity to the known-good pre-lifecycle baseline (FR-005), so it is not separately built. If the build toolchain/restore is unavailable, the buildability line is recorded `environment-limited` (Constitution V/VI — disclosed, never a silent pass) and the close conclusion names the unbuilt cell.
