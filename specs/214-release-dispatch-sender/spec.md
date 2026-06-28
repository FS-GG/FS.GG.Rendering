# Feature Specification: Release → Templates Dispatch Sender

**Feature Branch**: `214-release-dispatch-sender`

**Created**: 2026-06-28

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" — resolved to FS-GG/FS.GG.Rendering#10 (H4 · rendering — Wire release → `fs-gg-ui-template-released` repository_dispatch to Templates, the missing sender). Part of FS-GG/.github#16; Contract: `fs-gg-ui-template`.

## User Scenarios & Testing *(mandatory)*

When Rendering publishes a new release of the `FS.GG.UI.Template` coherent set, the downstream Templates repository must learn the new version automatically. The receiver already exists in FS.GG.Templates (`upstream-bump.yml` listens for the `fs-gg-ui-template-released` event and opens a pin-bump PR). The only missing half of the contract is the **sender** on the Rendering side. Today a maintainer must manually trigger the receiver to move the pin — this feature removes that manual step.

### User Story 1 - Downstream pin updates automatically on a template release (Priority: P1)

When Rendering publishes a new `FS.GG.UI.Template` version, the Templates repository is notified without any human action, so its scaffold-provider pin can move to the new version promptly and the two repos stay coherent.

**Why this priority**: This is the entire purpose of the item — closing the automation gap so a template release propagates to consumers. Without it the contract notification is permanently manual, which is the problem the H-series ("auto-update fabric") exists to remove.

**Independent Test**: Perform (or simulate) a canonical Rendering template release and confirm that the Templates receiver fires and a pin-bump PR carrying the released version appears in FS.GG.Templates — with no manual `workflow_dispatch` performed.

**Acceptance Scenarios**:

1. **Given** a new `FS.GG.UI.Template` version is released from the canonical Rendering repository, **When** the release completes, **Then** a notification carrying that exact version is delivered to FS.GG.Templates and the receiver opens a pin-bump PR for that version.
2. **Given** the notification has been delivered, **When** the version it carries equals the version the receiver derives, **Then** the resulting pin-bump PR targets the released version (no version drift between sent and received).

---

### User Story 2 - Release operators can see whether the notification was sent (Priority: P2)

A release operator (or anyone auditing a release) can tell from the release run whether the downstream notification succeeded or failed, so a missed notification is caught at release time rather than discovered later as a stale downstream pin.

**Why this priority**: Observability turns a silent integration into an operable one. It is not required for the happy path to work, but without it a failed send is invisible and the coherence gap silently reopens.

**Independent Test**: Inspect a release run and confirm the notification step reports a clear success/failure outcome; force a failure (e.g. revoke the cross-repo credential) and confirm the release surfaces it rather than passing silently.

**Acceptance Scenarios**:

1. **Given** the notification is sent successfully, **When** the release run is inspected, **Then** the send is recorded as a successful, attributable step.
2. **Given** the notification cannot be delivered (credential, network, or receiver error), **When** the release run completes, **Then** the failure is surfaced visibly (the responsible step does not report success) so an operator can react.

---

### User Story 3 - Notifications fire only for genuine canonical template releases (Priority: P3)

The notification fires only for real template releases from the canonical repository, never from forks and never for unrelated release events, so consumers are not spammed with spurious or unauthorized pin-bump requests and no fork can inject a dispatch.

**Why this priority**: A safety boundary. The happy path works without it, but it protects against fork-triggered dispatch and against noise from non-template release activity, consistent with the repo's existing fork-restriction posture (release jobs already gate on the canonical repository).

**Independent Test**: Trigger the release path from a fork and on a non-template release event; confirm no notification is sent in either case.

**Acceptance Scenarios**:

1. **Given** a release event originates from a fork of the repository, **When** the release path runs, **Then** no notification is sent to FS.GG.Templates.
2. **Given** a release event that does not correspond to a new template version, **When** the release path runs, **Then** no template-released notification is sent.

---

### Edge Cases

- **Version cannot be determined**: If the released version cannot be derived from the release identifier, the notification MUST NOT be sent with an empty/placeholder version (the receiver rejects an absent version); the condition is surfaced as a failure rather than sending a malformed payload.
- **Cross-repo credential missing or unauthorized**: When the credential authorized to notify FS.GG.Templates is absent or rejected, the send fails visibly (US2) rather than silently succeeding. (This is the standing dependency on the org cross-repo credential — see Dependencies.)
- **Receiver temporarily unavailable**: A transient delivery failure is surfaced; re-running the release (or a maintainer's manual receiver trigger, which still exists) recovers without corrupting state.
- **Duplicate / repeat notification for the same version**: A repeated notification for an already-pinned version is safe — it results in a no-op or no-change pin-bump on the receiver side, not a broken or conflicting PR.
- **Concurrent releases**: Two releases in close succession each send their own version; the latest pin-bump reflects the latest released version.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: On completion of a canonical release that publishes a new `FS.GG.UI.Template` version, the Rendering release process MUST send a cross-repo notification to FS.GG.Templates using the agreed event identifier `fs-gg-ui-template-released`.
- **FR-002**: The notification MUST carry the released template version as `client_payload.version` in the exact form the receiver expects (e.g. `0.1.50-preview.1`), matching the `upstream-bump.yml` contract.
- **FR-003**: The released version MUST be derived from the release identifier (the template-scoped coherent-set release/tag) and MUST NOT be hard-coded or hand-entered for the automated path.
- **FR-004**: The notification MUST be sent with a credential authorized to dispatch into FS.GG.Templates; the repository's default per-repo release credential (scoped only to the current repo) MUST NOT be relied upon for cross-repo delivery.
- **FR-005**: The notification MUST be sent only from the canonical `FS-GG/FS.GG.Rendering` repository; forks MUST NOT send it (no fork secret exposure, consistent with existing release-job restrictions).
- **FR-006**: A failure to deliver the notification (missing/invalid credential, network error, receiver error, or undeterminable version) MUST be surfaced as a visible failure of the responsible release step, not silently swallowed.
- **FR-007**: The notification MUST NOT be sent for release events that do not correspond to a new `FS.GG.UI.Template` version.
- **FR-008**: Sending the notification MUST NOT alter or weaken existing release behavior — Package.Tests, the template-instantiation generated-product tests, and the Feature 212 stock-root build/test/run assertion MUST continue to run and gate the release unchanged.
- **FR-009**: The sender's payload shape and event identifier MUST stay coherent with the receiver contract; if either side changes, the `fs-gg-ui-template` registry/contract entry is the source of truth and MUST be updated (contract-change).

### Key Entities *(include if feature involves data)*

- **Template-released notification**: The cross-repo message from Rendering to Templates. Key attributes: event identifier (`fs-gg-ui-template-released`) and payload carrying a single `version` string. It is the "sent" half of an existing send/receive contract.
- **Released template version**: The version string identifying the just-published `FS.GG.UI.Template` coherent set (e.g. `0.1.50-preview.1`), derived from the release identifier. It is the sole meaningful field of the notification payload.
- **Cross-repo credential**: The authorization that permits Rendering's release to deliver a notification into FS.GG.Templates. Its provisioning is owned outside this repo (org-level; see Dependencies).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of canonical `FS.GG.UI.Template` releases result in exactly one template-released notification carrying the released version, with zero manual steps between the Rendering release and the Templates pin-bump PR appearing.
- **SC-002**: The version that arrives at the receiver equals the released version in 100% of automated sends (no drift between sent and received version).
- **SC-003**: 0 notifications originate from forks or from non-template release events.
- **SC-004**: Every delivery failure is visible in the release record (100% of failures surfaced; 0 silently-passing failed sends).
- **SC-005**: After this feature, a maintainer no longer needs to manually trigger the Templates receiver to propagate a routine template release; the manual trigger remains available only as a fallback.

## Assumptions

- The receiver in FS.GG.Templates (`upstream-bump.yml`, event `fs-gg-ui-template-released`, reading `client_payload.version`) already exists and is correct; this feature adds only the sender and does not change the receiver.
- A "template release" is the publication of the `FS.GG.UI.Template` coherent set under its template-scoped release/tag (per Feature 206, e.g. `fs-gg-ui-template/v0.1.50-preview.1`); the version is the portion after the tag's `v` prefix. The exact trigger wiring (release event vs. tag pattern) is an implementation detail for planning, provided FR-001/FR-003/FR-007 hold.
- The send may be implemented either directly from the Rendering release workflow or via the planned reusable org dispatch-sender; either satisfies this spec as long as the functional requirements are met.
- Coherent-set versions follow the existing `<major>.<minor>.<patch>[-preview.N]` form already used across the registry; no new versioning scheme is introduced.

## Dependencies

- **Cross-repo credential (BLOCKING for the live path)**: Delivering a notification into FS.GG.Templates requires a credential beyond the default per-repo release token. This is owned by FS-GG/.github#21 (`[ADMIN]` org cross-repo credential) and FS-GG/.github#22 (reusable dispatch-sender), both still open. This spec and its sender logic can be authored ahead of that credential; end-to-end live delivery cannot succeed until the credential is provisioned. The board models this as #10 "Blocked by H4 · .github dispatch-sender".
- **Receiver contract**: FS.GG.Templates `upstream-bump.yml` — the event identifier and payload shape are fixed by this existing receiver and recorded under the `fs-gg-ui-template` registry contract.
