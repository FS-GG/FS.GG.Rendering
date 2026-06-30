# Feature Specification: Republish the `game`-Profile-Bearing Template (Release Feature 220)

**Feature Branch**: `222-republish-game-template`

**Created**: 2026-06-30

**Status**: Draft

**Tier**: Tier 1 (realizes a cross-repo contract mechanism — the `fs-gg-ui-template` package/coherence surface). No `FS.GG.UI.*` public surface is added, removed, or changed: the producer code for the additive `game` profile already shipped in Feature 220 (commit `b78e72a`, on `main`). This feature touches only release/packaging cadence, the org GitHub Packages feed, and the cross-repo registry — so no `.fsi` / design-token / surface-area baseline applies (see the plan's Constitution Check). Tier 1 here denotes the **cross-repo contract** surface (the registry-recorded `fs-gg-ui-template` coordinates), not an F# `.fsi` surface: the constitution's Tier-1 `.fsi` / surface-area-baseline obligations are therefore N/A (no F# surface changes), while its documentation/registry-update obligation applies and is met by FR-006.

**Input**: User description: "start the next Rendering owned item on the coordination board." — resolved on the org **Coordination** board (Projects v2 #1) to the single `Ready`, Rendering-owned item **FS-GG/FS.GG.Rendering#33** (Phase **P1 Rendering** · Workstream **Composition** · Contract `fs-gg-ui-template`, labels `cross-repo` + `contract-change`). It is the blocker that holds the in-progress consumer item **#31** (`Blocked by: FS.GG.Rendering#33`) and the downstream SDD default-flip. All other Rendering-owned board items are `Done`.

## Context & Background

Feature 220 (commit `b78e72a`, ADR `docs/product/decisions/0010-fs-gg-ui-template-default-starter.md`, **Accepted**) added an explicit, additive **`game` profile** to the `fs-gg-ui` template: a minimal replaceable Pong-style MVU skeleton (`Model`/`Msg`/`update`/`view` + tick) intended as the game/rendering **default** starter, plus a family-agnostic relaxation of the durable governance entrypoint assertion (game → `Viewer.runApp viewerOptions generatedHost`, satisfiable by the skeleton **and** a Pong swap, with **zero** `GovernanceTests` edits). `app` remains the explicit opt-in controls-showcase option; `headless-scene` / `governed` / `sample-pack` generated output stays byte-identical (FR-007, diff-verified). That change is **merged to `main` but not on the feed**.

The org GitHub Packages feed (`nuget.pkg.github.com/FS-GG`) currently serves the coherent set at **`0.1.53-preview.1`** (tag `fs-gg-ui-template/v0.1.53-preview.1`, commit `55e5967` = Feature 218). It has been **confirmed** that this published version does **not** contain the Feature-220 commit (`git merge-base --is-ancestor b78e72a fs-gg-ui-template/v0.1.53-preview.1` → false), so a scaffold against the feed today cannot select the `game` profile.

The registry (`FS-GG/.github` `registry/dependencies.yml`, `fs-gg-ui-template`) **records** the new `game` profile (added by the merged registry PR FS-GG/.github#77) but marks it **UNRELEASED until the next `fs-gg-ui-template` republish**, with no version/coherence flip. So two facts must change together: a coherent set carrying `game` must be published to the feed, **and** the registry entry must flip UNRELEASED → released (version + coherence + the `docs/registry/compatibility.md` projection), per the `contract-change` protocol.

This gates the consumer default-flip **FS-GG/FS.GG.SDD#44** (`app → game` default-selection, owned by the SDD scaffold-provider), which cannot land until the template carrying the `game` profile is resolvable on the org feed. It is also the explicit `Blocked by` for the in-progress board item **#31** (the consumer feedback that the game-template default was a controls demo, now resolved on the producer side by Feature 220).

The producer machinery already exists and requires no change: `release.yml`'s `publish-packages` job packs the whole coherent set (every `FS.GG.UI.*` package **and** the template at the same version `V`) from a `v*` / coherent-set tag and pushes to the org feed with `GITHUB_TOKEN` (`packages: write`); `scripts/derive-template-version.sh` derives the released version from the `fs-gg-ui-template/v*` tag and feeds the Feature-216 reusable dispatch-sender that notifies Templates. The published version is already org-readable (Feature 218 resolved visibility). So this feature is a **release-cadence + registry** change, not new product code.

The new coherent-set version MUST be strictly greater than `0.1.53-preview.1` (next preview in the established cadence: `0.1.54-preview.1`) and MUST contain the Feature-220 commit.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The `game` profile is selectable from the org feed (Priority: P1)

A composition engineer installs the latest `FS.GG.UI.Template` from the org feed (`nuget.pkg.github.com/FS-GG`) with an ordinary org consumer token (`packages: read`) and scaffolds the game/rendering provider selecting the `game` profile. The minimal replaceable Pong-style starter is produced and its no-flag launch renders a live interactive game scene — no `-- pong`-style flag, no missing-profile error.

**Why this priority**: This is the entire point of #33 and the last producer-side link unblocking SDD#44 and board item #31. Until a `game`-bearing version is resolvable on the feed, the consumer cannot flip the default and the board items stay `Ready`/`Blocked`.

**Independent Test**: From a clean environment authenticated only as an org consumer (`packages: read`), install the newly published `FS.GG.UI.Template@<new-version>` and scaffold with the `game` profile selected; observe that the `game` choice is accepted and the minimal MVU starter is generated (captured as evidence: feed listing + install/scaffold transcript).

**Acceptance Scenarios**:

1. **Given** the org feed serves the new `FS.GG.UI.Template` version, **When** a consumer queries the feed, **Then** a `FS.GG.UI.Template` version `> 0.1.53-preview.1` is served and contains the Feature-220 change.
2. **Given** the new version is installed, **When** the scaffold runs selecting the `game` profile, **Then** the choice is accepted and the minimal Pong-style starter (`Model`/`Msg`/`update`/`view` + tick) is generated — no missing-profile / unknown-choice error.
3. **Given** the generated `game` product, **When** it builds and runs with no flag, **Then** its governance tests pass without any `GovernanceTests` edits (family-agnostic entrypoint assertion).

---

### User Story 2 - The `game`-bearing coherent set is published to the org feed (Priority: P1)

A release engineer cuts a coherent-set release whose version is strictly greater than `0.1.53-preview.1` and that contains the Feature-220 commit, and pushes the whole `FS.GG.UI.*` + template set to the org feed at that single version.

**Why this priority**: This is the publish gate. Without a published version that carries the `game` profile, nothing downstream can select it from the feed.

**Independent Test**: Tag the coherent set (`fs-gg-ui-template/v<new-version>` and the sibling `v*` tags) so the existing `release.yml` `publish-packages` job packs and pushes the set; then query the org feed and confirm a `FS.GG.UI.Template` version `> 0.1.53-preview.1` is served whose contents include the Feature-220 commit.

**Acceptance Scenarios**:

1. **Given** Feature 220 is on `main`, **When** the coherent-set release is cut and published, **Then** the org feed serves a `FS.GG.UI.Template` (and matching `FS.GG.UI.*` packages) at one new version `> 0.1.53-preview.1` containing the Feature-220 change.
2. **Given** the publish succeeded, **When** the release notifies Templates via the existing dispatch-sender, **Then** the released version string is propagated so consumers can re-pin.

---

### User Story 3 - The cross-repo contract record flips UNRELEASED → released (Priority: P1)

A maintainer updates `FS-GG/.github` `registry/dependencies.yml` (`fs-gg-ui-template`) so the `game` profile is recorded as **released** at the new `package-version`, flips the relevant `coherence` entry, and regenerates the `docs/registry/compatibility.md` projection — then comments on #33 with the published version and the registry PR link, per the `contract-change` protocol.

**Why this priority**: ADR-0001 makes the registry the source of contract truth; a `contract-change` item MUST update the registry (and its compatibility projection) as part of its resolution. The downstream SDD default-flip (SDD#44) keys off the released version recorded here, so this is on the unblock path, not just bookkeeping.

**Independent Test**: Inspect the registry entry and compatibility projection after the landing; confirm the `game` profile no longer reads "UNRELEASED", names the new version, and the `coherence` entry is flipped; confirm #33 carries the published version + registry PR reference.

**Acceptance Scenarios**:

1. **Given** the coherent set is published, **When** the registry PR lands, **Then** `fs-gg-ui-template` records the `game` profile as released at the new `package-version` and the `coherence` entry is flipped.
2. **Given** the registry PR lands, **When** `docs/registry/compatibility.md` is regenerated, **Then** its projection names the new version (no stale `0.1.53-preview.1` for this surface).

---

### User Story 4 - The board and blocked consumers are released (Priority: P2)

After the coherent landing, the Coordination board and linked issues reflect reality: #33 is closed (with the published version + registry PR linked), board item #33 moves to `Done`, the `Blocked` on item #31 is cleared, and the downstream cross-repo asks (SDD#44) are notified that the `game`-bearing template is now resolvable on the feed.

**Why this priority**: The board is the source of order; closing the producer item and clearing the `Blocked` mirror is what lets the consumer flip proceed. It is auditable bookkeeping that is not itself on the install critical path.

**Independent Test**: Confirm #33 is closed with the version + registry PR linked; confirm item #31's `Blocked by` no longer points at an open #33; confirm SDD#44 carries a comment with the published version.

**Acceptance Scenarios**:

1. **Given** the registry flip landed, **When** the maintainer closes #33, **Then** it carries the published version string and the registry PR link, and the board item moves to `Done`.
2. **Given** #33 is closed, **When** the board is inspected, **Then** item #31 is no longer `Blocked by` an open #33 and SDD#44 has been notified.

### Edge Cases

- **Released version not strictly greater than `0.1.53-preview.1`** → reject; the feed and registry must advance, never re-tag an existing version (NuGet feeds are append-only and a stale version would still lack `game`).
- **Tagged commit does not contain Feature 220** (`b78e72a` not an ancestor of the release tag) → the publish would re-ship a `game`-less template; the release MUST be cut from a `main` commit that contains Feature 220, and the contents verified before the registry flips.
- **Coherent set is incoherent** (template version ≠ `FS.GG.UI.*` package versions) → reject; `publish-packages` packs the whole set at one version `V`, and the registry coherence entry asserts they match.
- **Registry flips before the feed actually serves the version** → the registry would claim "released" while the feed 404s; the flip MUST follow a confirmed feed listing (publish first, then flip).
- **Package re-privatized / not org-readable** → an org consumer token would hit exit 103; visibility was resolved in Feature 218 and MUST remain org-readable for the new version.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The release MUST publish a coherent `FS.GG.UI.*` + `FS.GG.UI.Template` set at a single new version strictly greater than `0.1.53-preview.1` (the established next preview is `0.1.54-preview.1`).
- **FR-002**: The published `FS.GG.UI.Template` version MUST contain the Feature-220 commit (`b78e72a`) — i.e. the additive `game` profile and the family-agnostic governance relaxation.
- **FR-003**: After publish, the org feed (`nuget.pkg.github.com/FS-GG`) MUST serve the new `FS.GG.UI.Template` version, installable and scaffold-selectable for the `game` profile by an ordinary org consumer token (`packages: read`) with no special private-package grant.
- **FR-004**: A scaffold from the published version selecting the `game` profile MUST generate the minimal replaceable Pong-style MVU starter, whose default no-flag launch passes governance with zero `GovernanceTests` edits (preserving the Feature-220 acceptance).
- **FR-005**: The non-`game` profiles (`app`, `headless-scene`, `governed`, `sample-pack`) MUST be unaffected by the republish — `app` still scaffolds the controls showcase; the three non-game profiles stay byte-identical to Feature 220's diff-verified output.
- **FR-006**: The `FS-GG/.github` registry entry for `fs-gg-ui-template` MUST flip the `game` profile from UNRELEASED → released at the new `package-version`, flip the relevant `coherence` entry, and regenerate the `docs/registry/compatibility.md` projection — landing as a `contract-change` PR.
- **FR-007**: The registry flip MUST follow a confirmed feed listing (publish first, then flip) so the contract record never claims "released" for a version the feed does not serve.
- **FR-008**: Issue #33 MUST be resolved with the published version string and the registry PR link recorded on it; the board item moves to `Done` and the `Blocked` mirror on #31 is cleared.
- **FR-009**: The downstream consumer (SDD#44, the `app → game` default-flip) MUST be notified of the published version via a cross-repo comment, so the default-selection flip can proceed. (The existing dispatch-sender separately notifies Templates to re-pin per FR-010 / US2 AS2; SDD#44 is notified directly.)
- **FR-010**: The release MUST reuse the existing producer machinery (`release.yml` `publish-packages` + `scripts/derive-template-version.sh` + the Feature-216 reusable dispatch-sender) without adding new product code or new `FS.GG.UI.*` public surface.

### Key Entities

- **Coherent release set**: every `FS.GG.UI.*` package plus `FS.GG.UI.Template`, all packed and pushed at one version `V > 0.1.53-preview.1`; the unit the `publish-packages` job ships and the unit the registry `coherence` entry asserts.
- **`fs-gg-ui-template` registry entry**: the versioned cross-repo contract record (`registry/dependencies.yml` + `docs/registry/compatibility.md`) that names the `game` profile, its release state, the `package-version`, and the coherence flip.
- **`game` profile**: the additive, replaceable Pong-style MVU starter (the intended game/rendering default), exercised by the new feed version; produced by Feature 220, here made resolvable on the feed.
- **Coordination item #33**: the `Ready`, Rendering-owned `contract-change` board item being resolved; blocker of #31 and gate of SDD#44.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An org consumer with only `packages: read` can install the new `FS.GG.UI.Template` from the org feed and scaffold the `game` profile successfully (exit 0; no missing-profile error, no exit 103) — captured as a transcript.
- **SC-002**: The org feed serves exactly one new coherent-set version `> 0.1.53-preview.1`, and its `FS.GG.UI.Template` contents include the Feature-220 commit (verified by content inspection, not just version string).
- **SC-003**: A `game` product scaffolded from the published version builds and passes its governance tests with **zero** `GovernanceTests` edits, and the three non-game profiles' generated output is byte-identical to Feature 220's diff-verified baseline.
- **SC-004**: The `fs-gg-ui-template` registry entry and `docs/registry/compatibility.md` no longer read "UNRELEASED" for the `game` profile and name the new version; the `coherence` entry is flipped.
- **SC-005**: #33 is closed with the version + registry PR linked; the board item is `Done`, #31 is no longer blocked by an open #33, and SDD#44 has the published version — all verifiable on GitHub.

## Assumptions

- The next preview in the established coherent-set cadence is `0.1.54-preview.1`; the release engineer may choose a higher version, provided it is strictly `> 0.1.53-preview.1` and cut from a `main` commit containing Feature 220.
- Package visibility resolved in Feature 218 (org-readable `FS.GG.UI.Template`) carries forward to the new version; no new visibility action is required.
- The existing release machinery (`release.yml` `publish-packages`, `scripts/derive-template-version.sh`, the Feature-216 reusable dispatch-sender) is functioning and is the publish path used here — no new workflow is authored.
- Cutting/pushing release tags and merging the `FS-GG/.github` registry PR require the appropriate org/release permissions; the registry change follows the ADR-0001 `contract-change` protocol.
- The `app → game` default-selection flip itself is **out of scope** here — it is owned by the SDD scaffold-provider (SDD#44); this feature only makes the `game`-bearing template resolvable on the feed and records it in the registry.
- No `FS.GG.UI.*` F# public surface, design tokens, or scene/control APIs change; producer code already shipped in Feature 220.
