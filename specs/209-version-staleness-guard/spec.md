# Feature Specification: Make the FS.GG.UI Version-Staleness Bug Class Structurally Impossible

**Feature Branch**: `209-version-staleness-guard`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → resolved to the next not-Done, Rendering-owned item: **P5 Versioning · Epic — Make the FsSkiaUiVersion staleness bug class structurally impossible** (board: FS-GG `Coordination`, Phase `P5 Versioning`, Workstream `Versioning`, target 2026-08-29). The P1 Rendering phase (lifecycle template parameter) is complete; this is the rendering-side versioning hardening that follows the just-completed 204 (coherence restore), 207 (BOM), and 208 (version-machinery rename).

## Context

The `FS.GG.UI.*` package set is versioned by **one** number that must agree across several
places that all live in this same repository:

- the framework library sources under `src/**` and the repo-root declared `<Version>`;
- the **published coherent set** on the local feed, snapshotted by the `fs-gg-ui/v<V>` git tags;
- the template's single source of truth, `<FsGgUiVersion>` in `template/base/Directory.Packages.props`
  (Feature 064), which every generated product's 11 `FS.GG.UI.*` pins and `build.fsx`'s runtime
  regex resolve from;
- the optional **BOM/metapackage** exact `[V]` pins (Feature 207) covering the full 16-package set.

Nothing in this repository fails when these fall out of lockstep. The pin is a hand-edited literal
bumped by humans, so it can silently **lag**. This is not hypothetical: Feature 204 had to *manually*
repair exactly this — the template pinned `0.1.0-preview.1` while framework HEAD had shipped a
refactored Scene API — and the only signal was a **downstream consumer's broken build** routed back
as a cross-repo `blocked` request (`FS-GG/FS.GG.Rendering#1`). The 208 plan calls out a sibling
failure mode: a **half-bump** (literal renamed/bumped but a pin still references the old/undefined
property) fails restore fast, and a green text-grep is *not* evidence of coherence.

This feature closes that **bug class**: drift between the single version source, the framework it must
match, the published snapshot tag, the member-package set, and the BOM exact pins must become a
**loud, local, automatic failure in this repo** — caught before merge to `main`, never again
discovered by a consumer. It builds on the single-source property (064), the BOM (207), the snapshot
tags (206/208), and `scripts/validate-bom-consumer.fsx`; it changes **no runtime/rendering behavior**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Drift fails this repo's own validation, not a downstream consumer's build (Priority: P1)

A maintainer changes the framework or the version pin and opens a PR. If the template's
`FsGgUiVersion`, the published coherent set it claims to match, the full `FS.GG.UI.*` member set, and
the BOM exact pins are not all in lockstep, this repository's own validation goes **red** with a
message naming the specific mismatch — before the change can merge to `main`, and long before any
consumer scaffolds a product.

**Why this priority**: This is the entire point of the epic. The version-staleness bug class is
defined by *where* it is detected: today it is detected by a stranger's broken build (Feature 204);
making it structurally impossible means detecting it here, automatically, every time. Everything else
in this feature serves this outcome.

**Independent Test**: Re-introduce the Feature-204 condition — set `FsGgUiVersion` to a stale value
that does not match the current coherent set — and run the repo's validation. It fails and names the
mismatch. Restore the correct value; it passes.

**Acceptance Scenarios**:

1. **Given** the template pin set to a version with no corresponding `fs-gg-ui/v<V>` published snapshot, **When** the repo's coherence validation runs, **Then** it fails and names the missing snapshot.
2. **Given** the framework version advanced past the template pin (the 204 drift), **When** validation runs, **Then** it fails and reports the template pin as stale relative to the coherent set, expected-vs-actual.
3. **Given** all locations in lockstep, **When** validation runs, **Then** it passes with no manual cross-checking.
4. **Given** a PR that would introduce drift, **When** the repo's CI/validation lane runs, **Then** the lane is red and the PR is blocked from merging to `main`.

---

### User Story 2 - A version bump is one coherent operation; a partial bump cannot ship (Priority: P2)

A maintainer cuts a new coherent set. They change the version in **one** place; the machinery
propagates or verifies every derived location (template pin, BOM exact pins, the value `build.fsx`'s
runtime regex reads, the expected snapshot tag). A half-bump — any derived location left behind — is
detected and rejected.

**Why this priority**: The single-source property (064) already centralizes *reading* the version, but
the *bump* still touches several artifacts by hand, which is where half-bumps (the 208 plan's warning)
and BOM-pin drift creep in. Making the bump atomic-or-rejected removes the remaining manual coupling
that lets staleness re-enter.

**Independent Test**: Bump only the single source and run validation → passes. Then perturb exactly
one derived location (e.g. a single BOM exact pin, or one `FS.GG.UI.*` pin) and run validation → fails,
naming the lagging location.

**Acceptance Scenarios**:

1. **Given** a coherent bump from one source of truth, **When** validation runs, **Then** all derived pins and the BOM resolve to the same single version with no mismatch.
2. **Given** a half-bump where one BOM exact pin lags the single source, **When** validation runs, **Then** it fails and names the lagging pin — and does so **regardless** of the `warnings-as-errors` policy (the in-repo coherence gate does not depend on consumer build policy).
3. **Given** a new `FS.GG.UI.*` member package added to the framework but not wired into the template pins and BOM, **When** validation runs, **Then** it fails and names the unwired member.

---

### User Story 3 - The pinned version must resolve to the complete real coherent set (Priority: P3)

A scaffolded product's restore cannot quietly pass against an incomplete or non-existent package set.
A pinned version that points at an undefined property, a partially-published set, or a missing member
fails restore **loudly and immediately** in this repo's generate→restore→build verification, rather
than producing a half-restored graph that only breaks later.

**Why this priority**: This hardens the verification half — the 208 plan's standing assumption that
"the rename is unverified until a product is generated, restored, and built." It guarantees the
coherence check above is grounded in a real restore, not a text comparison.

**Independent Test**: Point the pin at a version that is not fully published (or at a deliberately
half-renamed property), generate a product, and restore. Restore fails fast and names the
undefined/missing dependency; it never reports success on a partial graph.

**Acceptance Scenarios**:

1. **Given** a pin referencing a version with no published member set, **When** a product is generated and restored, **Then** restore fails with a missing-package error naming the version, not a silent partial success.
2. **Given** a half-renamed/undefined version property, **When** restore runs, **Then** it fails fast on the undefined property and the failure is surfaced by the repo's verification lane.

---

### Edge Cases

- **Pin with no snapshot tag**: `FsGgUiVersion` set to a value that was never tagged `fs-gg-ui/v<V>` — must fail (no phantom versions).
- **Snapshot tag with no matching pin**: a newer `fs-gg-ui/v<V>` tag exists but the template still pins an older value — must be reported as a stale pin (lag detection, the 204 case).
- **Member-set skew**: the framework publishes a member package that is not in the template pins or the BOM (or vice versa: a pin for a member the framework no longer publishes).
- **BOM policy loophole**: BOM exact-bracket drift today only blocks under `warnings-as-errors`; the in-repo coherence gate must catch it unconditionally.
- **Pre-release ordering**: `0.1.9-preview.1` vs `0.1.10-preview.1` and `-preview.1` vs `-preview.2` must compare as versions, not strings, when deciding "lagging".
- **Repo-root `<Version>` skew**: the repo-root declared `<Version>` (`0.1.0-preview.1`) differing from the published/tagged set — clarify whether it participates in the coherence set or is intentionally decoupled (see Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repository MUST provide a single automated coherence check that fails when the template's `FsGgUiVersion` does not equal the version of the current published coherent `FS.GG.UI.*` set.
- **FR-002**: The check MUST verify that the pinned version corresponds to an existing published coherent snapshot identified by an `fs-gg-ui/v<V>` git tag, and MUST report a pin that lags the latest such snapshot as stale.
- **FR-003**: The check MUST verify that every `FS.GG.UI.*` member package the framework publishes is pinned exactly once at the single version in the template (and represented in the BOM), with no missing member and no stale/extra member.
- **FR-004**: The check MUST verify the BOM/metapackage exact `[V]` pins equal the single-source version, and MUST fail on any mismatch **independently of** the `warnings-as-errors` policy that today gates BOM-bracket loudness for consumers.
- **FR-005**: A version bump MUST be expressible as an edit to one source of truth, with every derived location (template `FsGgUiVersion`, the value `build.fsx` resolves at runtime, BOM exact pins, expected snapshot tag) propagated or verified together; any partial bump MUST cause the check to fail.
- **FR-006**: The coherence check MUST run automatically in this repository's CI / validation lane on changes to `main`-bound branches, so drift cannot merge; it MUST NOT rely on a maintainer remembering to run it on demand.
- **FR-007**: On failure, the check MUST name the specific mismatching location and report expected-vs-actual so the corrective edit is unambiguous (no bare "incoherent" failures).
- **FR-008**: Verification MUST include a real generate→restore→build of a product from the template such that a pin that cannot resolve to the complete published set fails loudly (missing-package / undefined-property), never reporting success on a partial graph; a text-grep alone MUST NOT be accepted as coherence evidence.
- **FR-009**: When this repository publishes a new coherent set, the act of recording it (snapshot tag + feed) and the template pin MUST be kept in agreement by the same mechanism, so a published-but-unpinned (or pinned-but-unpublished) state is rejected.
- **FR-010**: The cross-repo registry contract `fs-gg-ui-version` (and `fs-gg-ui-bom`) MUST remain coherent with the in-repo guard; the feature MUST record how the structural guard upholds the contract (per the cross-repo coordination protocol).

### Key Entities *(include if feature involves data)*

- **Single version source of truth**: the one value (`FsGgUiVersion`) that all `FS.GG.UI.*` pins and the runtime engine resolution derive from.
- **Coherent snapshot tag**: `fs-gg-ui/v<V>` marking a published, internally-consistent `FS.GG.UI.*` set; the reference for "latest published coherent set."
- **Member package set**: the full set of co-versioned `FS.GG.UI.*` packages (16 published; 11 consumed by a generated product) that must all carry the single version.
- **BOM / metapackage**: the optional package pinning the full set at exact `[V]` versions; its pins must equal the single source.
- **Coherence guard**: the automated in-repo check + verification lane that enforces lockstep across the above and blocks merges on drift.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Re-introducing the Feature-204 drift (template pin set to a stale version) causes this repository's validation to fail within a single CI run, before any product is scaffolded by a consumer — 100% detection.
- **SC-002**: A half-bump (exactly one derived location — a BOM pin, a member pin, or the tag expectation — left behind) is detected every time, with the failure naming the lagging location.
- **SC-003**: Cutting and verifying a coherent release requires **zero** manual cross-checking steps beyond editing the single source of truth and running the automated guard; the guard is the sole authority on coherence.
- **SC-004**: Adding a new `FS.GG.UI.*` member package to the framework without wiring it into the template pins and BOM fails the guard.
- **SC-005**: Version-staleness incidents of the Feature-204 class reaching a downstream consumer (a `cross-repo` / `blocked` coherence request) drop to zero after this feature lands.
- **SC-006**: The guard's failure messages let a maintainer locate and fix the offending location without reading the guard's implementation (expected-vs-actual, named location).

## Assumptions

- **Intra-repo drift, intra-repo fix**: the framework sources and the template both live in `FS.GG.Rendering`, so the staleness is detectable and preventable by this repository's own CI — no cross-repo round-trip is needed to detect it.
- **Reference for "published coherent set"**: the `fs-gg-ui/v<V>` tag namespace (plus the local feed at `~/.local/share/nuget-local/`) is the authority for the latest published coherent version; the guard compares against the tags rather than re-deriving coherence from scratch.
- **Consumer-side lockfiles are out of scope**: committing `packages.lock.json` + `--locked-mode` CI in *consumer* repos (the sibling board item "P5 · cross-repo — Commit packages.lock.json…", target 2026-08-29) is a separate, cross-repo work item. This feature owns the **rendering-side** structural guard only; if a lockfile in *this* repo's template strengthens the guarantee, it is in scope, but rolling lockfiles out to consumers is not.
- **Versioning machinery only**: like Features 204/207/208, this changes versioning/identity/validation machinery and CI — no `src/**` public surface change and no runtime/rendering behavior change.
- **Repo-root `<Version>` is decoupled by default**: the repo-root `Directory.Build.props` `<Version>` (`0.1.0-preview.1`) is assumed *not* to be the published coherence number (publishing bumps the feed/tag independently via the merge/repack flow); the guard treats the `fs-gg-ui/v<V>` tag + feed as the coherent-version authority. If this assumption is wrong, the repo-root `<Version>` joins the lockstep set (FR-005).
- **Builds on existing artifacts**: single-source `FsGgUiVersion` (064), BOM/metapackage (207), snapshot tags (206/208), and `scripts/validate-bom-consumer.fsx` are the foundation to extend, not replace.
- **Cross-repo coordination**: the `fs-gg-ui-version` / `fs-gg-ui-bom` registry rows and any contract note are maintained via the `cross-repo-coordination` protocol as part of resolution (FR-010).
