# Feature Specification: Finalize the root-buildable template guarantee (release the coherent set + close #9)

**Feature Branch**: `215-root-build-release-closure`

**Created**: 2026-06-28

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → resolved with the user to **Finalize H1 root-build (FS-GG/FS.GG.Rendering#9)**: move the root-buildable template guarantee from *In review* to *Done*. The capability (root `.slnx` + `global.json` + verb wrapper, Feature 212) is built, live-verified, and merged to `main` at commit `b6ac246`, but it is **not yet released**: the latest published template tag `fs-gg-ui-template/v0.1.50-preview.1` predates the root-build work, and the cross-repo registry-coherence PR `FS-GG/.github#25` is still open.

## Context & Background

Feature 212 made every product scaffolded from the `fs-gg-ui` template (contract id `fs-gg-ui-template`)
**root-buildable with the stock .NET toolchain** — a root `<Name>.slnx`, a `global.json` SDK pin, and a
`restore|build|test|run|verify|pack` verb wrapper that delegates to the governed FAKE path — while keeping
FAKE as the rich path. That work is complete, live-verified across 12 profile×lifecycle combinations, and
**merged to `main`** (commit `b6ac246`). It directly unblocks FS-GG/FS.GG.SDD's composition-acceptance
probes, whose own H1 item ("acceptance build/run probes invoke declared-or-default command") is already
**Done** and waiting on a released root-buildable template.

The capability is therefore *built* but not *delivered*. Three gaps keep issue #9 in "In review" rather
than "Done":

1. **It isn't released.** The most recent published template package is `fs-gg-ui-template/v0.1.50-preview.1`,
   tagged before the root-build work merged. A consumer (including an SDD probe) who installs the published
   template still gets a product with **no root solution**. The guarantee only exists on `main`.
2. **The contract registry isn't coherent yet.** Per ADR-0001 (registry coherence) / contract C5, the
   org contract registry in `FS-GG/.github` must record the new `root-buildable` surface on the
   `fs-gg-ui-template` contract and a coherence entry pinned to the **released** version. PR
   `FS-GG/.github#25` carries those deltas but is still open, so the registry advertises a guarantee that
   no published artifact yet satisfies.
3. **The item isn't closed.** Issue #9 is still open and the Coordination board still shows "In review";
   the downstream SDD consumer has no released-artifact signal that it can now proceed.

This feature is the **closure slice**: it adds no new product capability. It releases the root-buildable
template as a coherent set, makes the contract registry coherent against that released version, confirms the
downstream unblock against the published artifact, and closes #9 / flips the board to Done with evidence.
Version coherence here is policy-gated — the published template version, the registry coherence-entry
version, and the org `FsGgUiVersion` line must agree so the staleness guard (Feature 209) does not flag a
straggler.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A consumer installs the published template and gets a root-buildable product (Priority: P1)

A consumer (a person, an SDD composition-acceptance probe, or any CI that does not know about FAKE)
installs the **published** `FS.GG.UI.Template` package — not a local branch — scaffolds a product, and from
the product root runs the stock restore → build → test → run sequence. It all succeeds without any FAKE
knowledge, because the published template now carries the root `.slnx`, the SDK pin, and the verb wrapper.

**Why this priority**: This is the delivery outcome that closes the gap. The capability already exists on
`main`; the whole point of finalization is that it reaches consumers through a released artifact. Until a
published version carries it, #9 cannot be Done and the SDD probes cannot consume it.

**Independent Test**: From a clean environment with no access to this repo's working tree, install the
newly published template version, scaffold a product into an empty directory, and run the stock
restore/build/test/run sequence at the product root. All succeed and the `app` profile starts (headless
exit 0), with no FAKE invocation.

**Acceptance Scenarios**:

1. **Given** the newly published template version, **When** a consumer installs it and scaffolds a product,
   **Then** the generated product contains a root solution, an SDK pin, and the verb wrapper, identical in
   behavior to the `main`-built template.
2. **Given** a product scaffolded from the published template, **When** a consumer runs stock build and
   stock test at the product root, **Then** both succeed without FAKE knowledge.
3. **Given** an `app`-profile product from the published template, **When** a consumer runs the stock "run"
   of the product source project headlessly, **Then** the application starts and exits cleanly.
4. **Given** the published coherent set, **When** the published template version is compared to the org
   `FsGgUiVersion` line and the latest `fs-gg-ui` tag, **Then** they agree (no version straggler) and the
   Feature 209 staleness guard does not flag the release.

---

### User Story 2 - The contract registry coherently advertises the released guarantee (Priority: P2)

A maintainer or downstream repo reads the org contract registry / compatibility surface to learn what
`fs-gg-ui-template` guarantees. After finalization, the registry records the `root-buildable` surface and a
coherence entry pinned to the **released** template version, so the advertised guarantee matches a published
artifact — not an unreleased branch.

**Why this priority**: ADR-0001 coherence is the cross-repo contract of record. An open coherence PR means
the registry either omits the guarantee or (once merged prematurely) advertises one no published package
satisfies. It depends on the release (US1) existing so the entry can pin a real version, but it is the thing
that makes the guarantee *discoverable and trustworthy* across repos.

**Independent Test**: Read the merged registry/compatibility files and confirm the `fs-gg-ui-template`
entry carries the `root-buildable` surface and a coherence row marked coherent, pinned to the released
template version and referencing tracker #9.

**Acceptance Scenarios**:

1. **Given** the released template version, **When** the registry-coherence change lands, **Then** the
   `fs-gg-ui-template` contract entry records the `root-buildable` surface and a coherence entry marked
   coherent.
2. **Given** the merged registry, **When** its coherence entry version is compared to the published
   template version, **Then** they match (the entry pins what was actually released).
3. **Given** the compatibility surface, **When** a downstream reader inspects it, **Then** the
   root-buildable guarantee and its tracker (#9) are visible and attributed to this feature/Feature 212.

---

### User Story 3 - The board and issue close with released-artifact evidence (Priority: P3)

The roadmap owner and the downstream SDD consumer need an unambiguous "done" signal: issue #9 closed and
the Coordination board flipped from "In review" to "Done", with evidence that the guarantee is delivered (a
released version, a green release gate, a coherent registry), not merely merged.

**Why this priority**: This is the closure guardrail and the cross-repo handshake. US1/US2 deliver and
record the guarantee; US3 makes the completion legible so the SDD H4 follow-on ("composition-acceptance
consumes the dispatched registry") can dequeue against a real released template rather than a promise.

**Independent Test**: Inspect issue #9 and the Coordination board; confirm #9 is closed with a closing
comment linking the released version, the green release-gate run, and the merged registry coherence change,
and that the board status reads "Done".

**Acceptance Scenarios**:

1. **Given** US1 and US2 are satisfied, **When** finalization completes, **Then** issue #9 is closed with a
   comment citing the released template version, the release-gate evidence, and the merged registry change.
2. **Given** #9 is closed, **When** the Coordination board is viewed, **Then** the H1 rendering item shows
   "Done".
3. **Given** the closure, **When** the downstream SDD consumer checks its blocker, **Then** the released
   root-buildable template is identifiable as available for its acceptance probes.

---

### Edge Cases

- **Release gate must run on a real release, not a dry run.** The release-only `template-product-tests`
  gate that asserts stock build/test/run at the product root must execute and pass on the actual release
  that publishes the new template version — a locally demonstrated gate is not sufficient evidence for #9.
- **Version straggler.** If the published template version lags the latest `fs-gg-ui` tag or the org
  `FsGgUiVersion`, the Feature 209 staleness guard will (correctly) flag it; the release must advance the
  coherent set so all three agree.
- **Premature registry merge.** Landing `.github#25` before the template is actually published would make
  the registry advertise a guarantee no package satisfies — coherence (US2) must pin the *released*
  version, so the registry change lands with or after the release, not before.
- **Re-release / version already taken.** If the intended version was already consumed by an earlier
  publish, the release must select the next coherent version rather than overwrite a tag.
- **Partial publish.** If the coherent set publishes some packages but the release gate fails, the
  guarantee is not delivered; #9 stays open until a fully green release exists.
- **No product-capability regression.** Finalization must not alter what Feature 212 emits; the published
  template must be behaviorally identical to the `main`-built template (no scope creep into new template
  behavior).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The root-buildable template capability (root solution, SDK pin, verb wrapper) MUST be made
  available to consumers through a **published** template release, not only on `main`.
- **FR-002**: The release that publishes the template MUST run the release-only gate that asserts stock
  build, stock test, and (for the `app` profile) stock run succeed at the root of a product scaffolded from
  the template under release, and MUST block the release if any regress.
- **FR-003**: A product scaffolded from the **published** template version MUST be behaviorally identical
  to one scaffolded from the `main`-built template (same emitted artifacts, same stock-vs-FAKE project sets,
  byte-neutral across `designSystem`).
- **FR-004**: The published template version, the contract-registry coherence-entry version, and the org
  `FsGgUiVersion` line MUST agree (no version straggler) such that the Feature 209 staleness guard does not
  flag the release.
- **FR-005**: The org contract registry MUST record the `root-buildable` surface on the `fs-gg-ui-template`
  contract and a coherence entry marked coherent, pinned to the released template version and referencing
  tracker #9 (the deltas carried by `FS-GG/.github#25`).
- **FR-006**: The registry coherence change MUST land **with or after** the release (never before), so the
  advertised guarantee always corresponds to a published artifact.
- **FR-007**: The compatibility surface MUST expose the root-buildable guarantee and its tracker so
  downstream repos can discover it.
- **FR-008**: Issue #9 MUST be closed with a closing comment linking the released template version, the
  green release-gate evidence, and the merged registry coherence change.
- **FR-009**: The Coordination board entry for the H1 rendering item MUST be set to "Done".
- **FR-010**: The downstream SDD consumer's dependency on a released root-buildable template MUST be
  satisfiable — the released version MUST be identifiable as the artifact its acceptance probes can consume.
- **FR-011**: Finalization MUST NOT introduce new product-capability changes to the template beyond what
  Feature 212 already merged (closure only; no scope creep).

### Key Entities *(include if feature involves data)*

- **Published template release**: the tagged, published `FS.GG.UI.Template` package version that carries
  the root-build artifacts; the unit consumers install.
- **Coherent set**: the agreeing trio of published template version, registry coherence-entry version, and
  org `FsGgUiVersion` line that the staleness guard validates.
- **Release-gate evidence**: the green run of the release-only `template-product-tests` gate proving stock
  build/test/run at the product root on the actual release.
- **Registry coherence entry**: the `fs-gg-ui-template` `root-buildable` surface + coherence row in the org
  contract registry / compatibility surface, pinned to the released version and tracker #9.
- **Tracker #9**: the H1 rendering Coordination-board issue whose closure (with evidence) is the
  finalization signal.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A consumer can, from a clean environment, install the published template, scaffold a product,
  and complete stock restore → build → test → run at the product root with a 100% success rate and zero
  FAKE invocations.
- **SC-002**: The release-only root-buildability gate runs and passes on the actual release that publishes
  the new template version (green run exists and blocks on regression).
- **SC-003**: The published template version, the registry coherence-entry version, and the org
  `FsGgUiVersion` line are identical, and the staleness guard reports no straggler.
- **SC-004**: The contract registry / compatibility surface advertises the `root-buildable` guarantee
  pinned to the released version, with no window in which the guarantee is advertised but unreleased.
- **SC-005**: Issue #9 is closed with released-artifact evidence and the Coordination board shows the H1
  rendering item as "Done".
- **SC-006**: The downstream SDD acceptance probes can target the released root-buildable template (the
  consumer's blocker is satisfiable against a published version, not a branch).

## Assumptions

- Feature 212's root-build capability on `main` (commit `b6ac246`) is correct and final; this feature does
  not re-implement or modify it.
- The mechanism to publish a coherent set (release workflow + tagging) already exists and is the same one
  used for prior `fs-gg-ui` / `fs-gg-ui-template` releases; finalization triggers it for the next coherent
  version rather than building new release machinery.
- `FS-GG/.github#25` already contains the correct registry/compatibility deltas; finalization ensures it
  pins the released version and lands at the right time, adjusting the pinned version if the release number
  differs from the PR's current draft.
- The SDD consumer side (its H1 acceptance-probe item) is already Done; no SDD code change is required by
  this feature — only a released artifact it can consume.
- The release publishes to the org package feed consumers already use; no new feed or credential is
  introduced by this feature.

## Dependencies

- **FS-GG/.github#25** (registry: `fs-gg-ui-template` root-buildable guarantee) — must land coherently with
  the release.
- **Feature 209 staleness guard** — gates version coherence of the coherent set.
- **The release-only `template-product-tests` gate** (delivered by Feature 212) — must execute on the real
  release.
- **FS-GG/FS.GG.SDD H1** (acceptance build/run probes, already Done) — the downstream consumer this release
  unblocks.

## Out of Scope

- Any change to *what* the template emits or to FAKE `Verify` semantics (owned by Feature 212).
- The unrelated H4 dispatch-sender work (issue #10 / Feature 214) and its `.github#22` blocker.
- Building new release or publishing infrastructure (assumed to already exist).
- SDD-side code changes (its consuming item is already Done).
