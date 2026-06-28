# Feature Specification: Adopt Reusable App-Token Dispatch-Sender

**Feature Branch**: `216-adopt-reusable-dispatch-sender`

**Created**: 2026-06-28

**Status**: Draft

**Tier**: Tier 1 (realizes a cross-repo contract mechanism). No F# public surface is added, removed, or changed — this feature touches only GitHub Actions YAML and a Bash helper, so no `.fsi` or surface-area baseline applies (see the plan's Constitution Check).

**Input**: User description: "next Rendering item on the project coordination board" — resolved to **FS-GG/FS.GG.Rendering#10** ("H4 · rendering — Wire release → `fs-gg-ui-template-released` repository_dispatch to Templates"). The sender authored in Feature 214 fired on the 0.1.52-preview.1 release tag but failed because its credential (`secrets.TEMPLATES_DISPATCH_TOKEN`) is empty. Per the user's decision, this feature adopts the org reusable App-token dispatch-sender (the documented direction, tracked by `.github#22`) rather than provisioning a per-repo token.

## Context & Background

When Rendering publishes a FS.GG.UI.Template coherent-set release — the template-scoped tag `fs-gg-ui-template/v<version>` (Feature 206) — the downstream repo FS.GG.Templates must be notified so its `upstream-bump.yml` receiver opens a pin-bump PR for the new version. The contract is `fs-gg-ui-template`.

Feature 214 built the **sender half**: `.github/workflows/template-dispatch.yml` + `scripts/template-released-dispatch.sh`, locally proven (actionlint + dry-run harness all green). On the first real template tag (`fs-gg-ui-template/v0.1.52-preview.1`, the Feature 215 release) the sender ran but **failed in ~9s** with `GH_TOKEN (from secrets.TEMPLATES_DISPATCH_TOKEN) is empty/unset` — it correctly refused to send unauthenticated. The live end-to-end criterion of #10 therefore remains unmet.

The org's chosen approach (board Phase H4) is a **single reusable dispatch-sender workflow** in `FS-GG/.github` that mints a short-lived GitHub App installation token at run time (`create-github-app-token`) and POSTs the `repository_dispatch` — so no consumer repo stores a long-lived cross-repo PAT. That reusable workflow is tracked by `.github#22` (still **open**), but Phase 0 research (R0) established it **already exists on `FS-GG/.github` `main`** (commit `5fed283`) with a fully specified `workflow_call` interface — so the consumption interface is known, not speculative. The org App and secrets were provisioned by `.github#21` (**closed**). The residual live blocker is therefore not "the workflow doesn't exist" but "are the org App secrets actually set under the expected names, and does a real release authenticate" (the FR-008/FR-009 deferred check).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Release automatically notifies Templates (Priority: P1)

A release engineer publishes a FS.GG.UI.Template coherent set by pushing the tag `fs-gg-ui-template/v<version>`. With no further manual action, FS.GG.Templates is notified and opens (or updates) a pin-bump PR for that exact version.

**Why this priority**: This is the entire point of #10 and the last unfinished link of the H4 auto-update fabric. Until a real release produces a real receiver PR, the contract is unproven and the board item stays Blocked.

**Independent Test**: Push a real (or pre-release) `fs-gg-ui-template/v*` tag in the canonical repo; observe the sender run succeed and a matching pin-bump PR appear in FS.GG.Templates — captured as live evidence (run URL + PR URL), not a local dry-run.

**Acceptance Scenarios**:

1. **Given** the org App credential is available to the canonical repo, **When** a `fs-gg-ui-template/v<version>` tag is pushed, **Then** a `repository_dispatch` of type `fs-gg-ui-template-released` carrying `<version>` is delivered to FS.GG.Templates and its receiver opens/updates a pin-bump PR for `<version>`.
2. **Given** a successful dispatch, **When** the maintainer inspects the result, **Then** the live evidence (sender run URL + receiver PR URL) is sufficient to close #10 and mark the board item Done.

---

### User Story 2 - Credential is minted per-run, never stored (Priority: P2)

The cross-repo send authenticates with a short-lived org GitHub App installation token created during the workflow run, rather than a long-lived personal/repo token kept as a Rendering secret.

**Why this priority**: Removes the secret-management/rotation/leak surface that caused the live failure, and aligns Rendering with the one org-wide maintained path so auth logic isn't duplicated per repo.

**Independent Test**: Inspect the sender configuration and a run log — confirm the token is obtained at run time from the App (not read from a stored `TEMPLATES_DISPATCH_TOKEN`-style long-lived secret) and that no such long-lived cross-repo secret remains required by Rendering.

**Acceptance Scenarios**:

1. **Given** the migrated sender, **When** a release fires it, **Then** authentication succeeds using a run-time-minted App token and no long-lived cross-repo PAT is referenced.
2. **Given** the App credential is missing or misconfigured, **When** the sender runs, **Then** it fails loudly and sends nothing (no silent or unauthenticated dispatch).

---

### User Story 3 - Release gating and fork safety are preserved (Priority: P3)

Migrating the sender must not change package-release behavior or expose forks to cross-repo credentials.

**Why this priority**: The sender shares a repo with the release pipeline; a regression there would be far costlier than the feature's benefit. Fork safety is a standing security requirement.

**Independent Test**: Diff `release.yml` against `origin/main` (expect no change); confirm the template-tag trigger is disjoint from the release `v*` trigger; confirm the sender is gated to the canonical repo so forks never run it.

**Acceptance Scenarios**:

1. **Given** the migration, **When** `release.yml` is compared to `origin/main`, **Then** it is byte-unchanged and its `v*`/template-tag triggers remain disjoint.
2. **Given** a fork of the repo, **When** a template tag exists there, **Then** the sender does not run and no credential is exposed.

---

### Edge Cases

- **Org App secret names / credential not yet confirmed** (the reusable workflow itself already exists on `.github` `main` per research R0; the open item is confirming the exact `app-id`/`app-private-key` org secret names and that the `.github#21` App authenticates): the Rendering-side adoption must be complete and ready, the dependency surfaced as a cross-repo request, and the board item stays Blocked until that confirmation lands — without faking a green live result.
- **Non-tag / manual run**: a manual `workflow_dispatch` (operator inspection) lands on a non-tag ref → version derivation fails → the job fails loudly rather than sending.
- **Malformed version** in the tag (not `MAJOR.MINOR.PATCH[-preview.N]`): rejected before any dispatch.
- **Receiver unavailable or rejects the dispatch**: the sender run reports failure (not a false success).
- **Re-pushed / duplicate tag**: a repeated dispatch for an already-handled version must not corrupt or duplicate the receiver's pin-bump beyond an idempotent update.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: On push of a tag matching `fs-gg-ui-template/v<version>` in the canonical repo, the system MUST deliver a `repository_dispatch` of type `fs-gg-ui-template-released` to FS.GG.Templates carrying the released `<version>`.
- **FR-002**: The send MUST authenticate with a short-lived org GitHub App installation token minted during the run; the Rendering repo MUST NOT depend on a stored long-lived cross-repo personal/repo token.
- **FR-003**: Rendering MUST consume the org's shared reusable dispatch-sender (`.github#22`) rather than maintain a bespoke Rendering-only auth/dispatch implementation, so the cross-repo send logic is maintained once org-wide.
- **FR-004**: The sender MUST run only in the canonical repository (`FS-GG/FS.GG.Rendering`); forks MUST NOT send or be able to access the cross-repo credential.
- **FR-005**: The sender MUST derive and validate the version from the tag ref and MUST fail loudly — sending nothing — on an undeterminable/malformed version or a missing/invalid credential (never a silent or unauthenticated send).
- **FR-006**: The release workflow (`release.yml`, `v*` trigger) MUST remain byte-unchanged, and the template-tag trigger MUST stay disjoint from the release trigger so package-release gating is unaffected.
- **FR-007**: A successful release MUST result in the FS.GG.Templates receiver opening or updating a pin-bump PR for the dispatched version with no manual intervention.
- **FR-008**: If the org App secret names/credential are not yet confirmed (the reusable workflow already exists on `.github` `main` per research R0), the feature MUST record the dependency as a cross-repo request against `.github#22`/`.github#21`, keep the board item Blocked, and leave the Rendering-side adoption ready to go green the moment the secret names are confirmed and the App authenticates.
- **FR-009**: Closure of FS-GG/FS.GG.Rendering#10 MUST be backed by live end-to-end evidence (sender run URL + receiver PR URL), not local dry-run output.

### Key Entities *(include if feature involves data)*

- **Template-released dispatch**: the cross-repo signal — event type `fs-gg-ui-template-released`, payload carrying the released semver `<version>` — sent from Rendering to FS.GG.Templates.
- **Template-scoped release tag**: `fs-gg-ui-template/v<version>` (Feature 206); the sole trigger for the sender and the source of the dispatched version.
- **Org App credential**: the run-time-minted GitHub App installation token (provisioned by `.github#21`) authorizing the cross-repo dispatch.
- **Pin-bump PR**: the receiver-side outcome in FS.GG.Templates that repoints the `fs-gg-ui-template` pin to the released version.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of FS.GG.UI.Template coherent-set releases notify FS.GG.Templates automatically, with zero manual dispatch steps.
- **SC-002**: A real template release tag yields a successful sender run and a corresponding pin-bump PR in FS.GG.Templates within 10 minutes of the tag push.
- **SC-003**: No long-lived cross-repo personal/repo token is stored as a Rendering secret; the dispatch credential is minted per run.
- **SC-004**: Release-only gating behavior is unchanged — `release.yml` is byte-identical to `origin/main` and its trigger set stays disjoint from the template-tag trigger.
- **SC-005**: FS-GG/FS.GG.Rendering#10 is closed with live end-to-end evidence (sender run URL + receiver pin-bump PR URL) and the board item moves Blocked → Done.

## Assumptions

- The org GitHub App from `.github#21` is installed on both FS.GG.Rendering and FS.GG.Templates and is permitted to send/receive `repository_dispatch`, and its identity (app id + private key, or equivalent org-level secret/variable) is exposable to the canonical repo's workflow.
- The reusable dispatch-sender (`.github#22`) is the intended consumption path and exposes a `workflow_call` (or equivalent) interface the Rendering sender can call; if it is not yet delivered, this feature's live criterion is gated on it (per FR-008).
- The FS.GG.Templates receiver (`upstream-bump.yml`) already listens for `fs-gg-ui-template-released` and opens the pin-bump PR (established by #10's prior work); no receiver change is in scope here.
- The template-scoped tag scheme `fs-gg-ui-template/v*` (Feature 206) remains the release trigger and version source.
- The Feature 214 sender (`template-dispatch.yml` + `scripts/template-released-dispatch.sh`) is the migration baseline — its trigger/guard/validation behavior is preserved; only the credential/auth mechanism changes to the reusable App-token path.

## Dependencies

- **Cross-repo (blocking the live criterion)**: the reusable dispatch-sender workflow already exists on `FS-GG/.github` `main` (research R0); what remains is confirming the exact `app-id`/`app-private-key` org secret names and that the `.github#21` App authenticates from FS.GG.Rendering. Track via a `cross-repo`/`cross-repo:request` issue against `.github#22`/`.github#21` and the Coordination board (`Contract: fs-gg-ui-template`); the board item stays Blocked until that confirmation lands.
- **Cross-repo (satisfied)**: `FS-GG/.github#21` — org App + Packages feed + dispatch/feed secrets — closed.
- **Local baseline**: Feature 214 sender artifacts in this repo.
