# Feature Specification: Publish & Make-Readable the productName-Enabled Template

**Feature Branch**: `218-publish-readable-template`

**Created**: 2026-06-29

**Status**: Draft

**Tier**: Tier 1 (realizes a cross-repo contract mechanism — the `fs-gg-ui-template` package/coherence surface). No F# public surface is added, removed, or changed: the producer code already shipped in Feature 217 (commit `6df0d39`, now on `main`). This feature touches only release/packaging cadence, GitHub Packages visibility, and the cross-repo registry — so no `.fsi` or surface-area baseline applies (see the plan's Constitution Check).

**Input**: User description: "start the next unblocked Rendering item on the coordination board." — resolved on the org **Coordination** board (Projects v2 #1) to the two `Ready`, unblocked, Rendering-owned cross-repo requests that are the **coupled gates** on the SDD-orchestrated composition path: **FS-GG/FS.GG.Rendering#29** (publish a Feature-217 / `productName`-bearing `FS.GG.UI.Template` to the org feed) and **FS-GG/FS.GG.Rendering#26** (that package is *private* → make it org-readable). The board (Phase P4 Templates · Workstream Composition · Contract `fs-gg-ui-template`) and the issue cross-comments both state the ideal landing is **one** release that is *both* Feature-217-bearing *and* feed-readable; resolving only one leaves FS-GG/FS.GG.Templates#32 (the pin-bump → full composition) `Blocked`. This feature scopes that single coherent landing.

## Context & Background

Feature 217 added the additive `--productName` scaffold symbol to the `fs-gg-ui` template so the SDD scaffold-provider (`fsgg-sdd scaffold --provider rendering --param productName=<P>`) stops failing with **exit 127**. That fix is **merged to `main` but not on the feed**. The org GitHub Packages feed (`nuget.pkg.github.com/FS-GG`) currently serves **only `FS.GG.UI.Template@0.1.52-preview.1`** (published 2026-06-28) — the version that *predates* `productName` and still rejects `--productName` with exit 127. The registry (`FS-GG/.github` `registry/dependencies.yml`, `fs-gg-ui-template`) records the param as "UNRELEASED on the feed (lands next fs-gg-ui-template release)", `package-version: 0.1.52-preview.1`.

There is a **second, independent gate** on the same path. Even once a `productName`-bearing version is published, FS.GG.Templates CI cannot install it: `FS.GG.UI.Template` is a **private** package, so a consumer's run-scoped `GITHUB_TOKEN` (`packages: read`) hits **exit 103 (NotFound/auth)** on `dotnet new install`. The two public CLIs in the same job (`FS.GG.Governance.Cli`, `FS.GG.SDD.Cli`) install fine — the token reads org packages; the template package is simply private.

These are the **two coupled gates**, both Rendering-owned (#29 publish, #26 visibility):

- Publishing a new version that is **still private** → Templates CI stays red with **exit 103**.
- Fixing visibility on the **current** version → it still rejects `--productName` with **exit 127**.

So the only landing that moves FS-GG/FS.GG.Templates#32 is **one version that is both Feature-217-bearing and feed-readable by the Templates CI token**.

The producer machinery already exists: `release.yml`'s `publish-packages` job packs the whole coherent set (every `FS.GG.UI.*` package **and** the template at the same version `V`) from a `v*` / coherent-set tag and pushes to the org feed with `GITHUB_TOKEN` (`packages: write`); `scripts/derive-template-version.sh` derives the released version from the template tag and feeds the Feature-216 reusable dispatch-sender that notifies Templates. So this feature is a **release-cadence + visibility + registry** change, not new product code.

Package **visibility** is set in GitHub org package settings (`https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings`) — an admin-gated action (UI or REST), analogous to the other admin-gated Coordination items. The preferred fix is **internal** visibility (org-wide read, matching the already-public CLIs); the fallback is granting `FS-GG/FS.GG.Templates` repo *Read* access to the package.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - SDD-orchestrated scaffold succeeds end-to-end (Priority: P1)

A composition engineer runs the CLI-orchestrated scaffold against the **org feed** — `fsgg-sdd scaffold --provider rendering --param productName=<P>` — with only an ordinary org consumer token (`packages: read`). The template installs and the `productName` parameter is accepted: no exit 127, no exit 103.

**Why this priority**: This combined outcome is the entire point of both #29 and #26 and the last link unblocking FS-GG/FS.GG.Templates#32. Until it holds, the SDD-orchestrated composition path stays red and the board items stay `Ready`/`Blocked`.

**Independent Test**: From a clean environment authenticated only as an org consumer (`packages: read`, no special grant to a private package), install the newly published `FS.GG.UI.Template` from `nuget.pkg.github.com/FS-GG` and scaffold with `--productName=<P>`; observe exit 0 (neither 127 nor 103), captured as evidence (feed listing + install/scaffold transcript).

**Acceptance Scenarios**:

1. **Given** the org feed serves the new `FS.GG.UI.Template` version, **When** a consumer token with only `packages: read` runs `dotnet new install FS.GG.UI.Template@<new-version>`, **Then** the install succeeds (exit 0) — no exit 103.
2. **Given** the new version is installed, **When** the scaffold runs with `--productName=<P>`, **Then** it is accepted and the product scaffolds — no exit 127.

---

### User Story 2 - Feature-217-bearing version is on the org feed (Priority: P1)

A release engineer cuts a `FS.GG.UI.Template` release whose version is strictly greater than `0.1.52-preview.1` and that contains the Feature-217 commit, and pushes the coherent set to the org feed. (Resolves #29, the producer half.)

**Why this priority**: This is the publish gate. Without a published version that carries `productName`, fixing visibility alone still yields exit 127.

**Independent Test**: Query the org feed for `FS.GG.UI.Template`; confirm a version `> 0.1.52-preview.1` is served and that installing it locally and scaffolding with `--productName` succeeds (no exit 127).

**Acceptance Scenarios**:

1. **Given** Feature 217 is on `main`, **When** the coherent-set release is cut and published, **Then** the org feed serves a `FS.GG.UI.Template` version `> 0.1.52-preview.1` containing the Feature-217 change.
2. **Given** the publish succeeded, **When** the maintainer replies on #29 with the published version string, **Then** Templates has the exact version it needs to re-pin.

---

### User Story 3 - The template package is org-readable (Priority: P1)

A platform admin makes `FS.GG.UI.Template` readable by org consumers — preferring **internal** visibility (matching the public CLIs), or granting `FS-GG/FS.GG.Templates` repo *Read* access. (Resolves #26, the visibility half.)

**Why this priority**: This is the auth gate. Without it, even a `productName`-bearing version stays uninstallable by Templates CI (exit 103).

**Independent Test**: With a token that does **not** carry an explicit private-package grant, `dotnet new install` the package from a different repo's job context; observe exit 0 (no exit 103).

**Acceptance Scenarios**:

1. **Given** the package visibility is changed to internal (or Templates is granted Read), **When** an org consumer CI token (`packages: read`) installs the package, **Then** the install authenticates and succeeds — no exit 103.

---

### User Story 4 - Cross-repo contract record stays coherent (Priority: P2)

After the coherent landing, the registry and the board reflect reality: `FS-GG/.github` `registry/dependencies.yml` (`fs-gg-ui-template` `package-version` + coherence) and its `docs/registry/compatibility.md` projection name the new version; #29 and #26 are closed; the two board items move to `Done`.

**Why this priority**: ADR-0001 makes the registry the source of contract truth and the board the source of order; a contract-change must update the registry as part of its resolution. This is bookkeeping that makes the landing auditable but is not on the install critical path.

**Independent Test**: Inspect `registry/dependencies.yml` and the compatibility projection for the new `package-version` and flipped coherence entry; confirm #29/#26 closed and the board items `Done`.

**Acceptance Scenarios**:

1. **Given** the new version is published and readable, **When** the registry + compatibility projection are updated and the issues closed, **Then** the `fs-gg-ui-template` record shows the new `package-version` and the coherence entry is flipped to reflect the released `productName` support.

---

### Edge Cases

- **Half-landing (publish only):** new version on the feed but still private → consumer install fails with exit 103. The release is **not** done; both gates must hold (FR-004).
- **Half-landing (visibility only):** visibility fixed but only `0.1.52-preview.1` on the feed → scaffold still fails with exit 127. Not done (FR-004).
- **Version collision / non-monotonic version:** the published version must be **strictly greater** than `0.1.52-preview.1`; re-using or regressing the version is rejected (FR-001).
- **Coherent-set drift:** the template must be packed at the **same** version as the rest of the `FS.GG.UI.*` set (the existing release invariant); a template-only bump that desyncs the set is not acceptable (FR-006).
- **Internal visibility disallowed by org policy:** fall back to granting `FS-GG/FS.GG.Templates` repo *Read* (FR-003 alternative) — the readability outcome is what matters, not the mechanism.
- **Admin rights unavailable:** the visibility change is admin-gated; if it cannot be performed in-session it must be surfaced as a manual step (like other admin-gated board items), not silently skipped.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST publish a `FS.GG.UI.Template` version **strictly greater than `0.1.52-preview.1`** that includes the Feature-217 commit (`6df0d39`, now on `main`) to the org GitHub Packages feed (`nuget.pkg.github.com/FS-GG`).
- **FR-002**: The published version MUST accept the additive `--productName` alias when scaffolding — installing and scaffolding with `--productName=<P>` MUST NOT fail with **exit 127**.
- **FR-003**: The `FS.GG.UI.Template` package MUST be readable by an ordinary org consumer CI token holding only `packages: read` — i.e. `dotnet new install` MUST NOT fail with **exit 103**. This MUST be achieved by changing package visibility to **internal** (preferred, matching the public `FS.GG.*.Cli` packages) or, failing that, granting `FS-GG/FS.GG.Templates` repo *Read* access.
- **FR-004**: The publish (FR-001/002) and the readability (FR-003) MUST land **coherently**: there MUST exist a single `FS.GG.UI.Template` version that is *both* Feature-217-bearing *and* feed-readable by the Templates CI token. A state where only one holds is NOT "done".
- **FR-005**: The org feed MUST be confirmed to actually serve the new version (queryable / listable), not merely pushed.
- **FR-006**: The release MUST preserve the coherent-set invariant — every `FS.GG.UI.*` package and the template (the 17 `FS.GG.UI.*` packables + 1 template package) are packed at the same version `V` (as the existing `release.yml` / `dev-repack` flow does); the template MUST NOT be bumped out of step with the set.
- **FR-007**: After a successful publish, the feature MUST reply on **#29** with the published version string and MUST close **#29** and **#26** once both gates hold.
- **FR-008**: The feature MUST update the cross-repo registry as the `contract-change` landing point — `FS-GG/.github` `registry/dependencies.yml` (`fs-gg-ui-template` `package-version` and the coherence entry) and its `docs/registry/compatibility.md` projection — to name the new version and reflect released `productName` support.
- **FR-009**: The feature MUST NOT change any `fs-gg-ui-template` **contract surface** (template parameter names/semantics, provider interface). The change is additive/version-only: `productName` was already specified in Feature 217 and is exposed by publishing, not by altering the contract.
- **FR-010**: The coherent-set release SHOULD let the existing Feature-216 template-released dispatch fire on the release tag so FS.GG.Templates is notified of the new version (consistency with the established auto-update fabric); a missing dispatch MUST NOT, by itself, block FR-004 if the version is otherwise published and readable.

### Key Entities *(include if feature involves data)*

- **`FS.GG.UI.Template` package version**: the NuGet/template package on `nuget.pkg.github.com/FS-GG`; must be `> 0.1.52-preview.1`, carry Feature 217, and be packed coherently with the rest of the set.
- **Package visibility / read grant**: the org package-settings state (`private` → `internal`, or a repo Read grant to `FS-GG/FS.GG.Templates`) that decides whether a consumer token can install it.
- **`fs-gg-ui-template` registry entry**: the contract record in `FS-GG/.github` (`registry/dependencies.yml` + compatibility projection) carrying `package-version` and the coherence flag.
- **Cross-repo requests #29 / #26 and board items**: the mailbox messages and their Coordination-board rows (Phase P4 Templates · Workstream Composition) whose closure + `Done` status records the landing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The org feed serves a `FS.GG.UI.Template` version `> 0.1.52-preview.1` that contains Feature 217 (verifiable by listing the feed).
- **SC-002**: A consumer authenticated with only `packages: read` (no explicit private-package grant) installs the new version with `dotnet new install` at **exit 0** — zero exit-103 failures.
- **SC-003**: Scaffolding the installed version with `--productName=<P>` succeeds — zero exit-127 failures.
- **SC-004**: With the new version pinned, the downstream gated composition run (`FSGG_COMPOSITION_FULL=1` in FS.GG.Templates, the gate the two requests unblock) reaches its full pass count (29/29), and FS-GG/FS.GG.Templates#32 can move its pin.
- **SC-005**: #29 and #26 are closed, their two Coordination-board items are `Done`, and the `fs-gg-ui-template` registry `package-version` + coherence entry name the published version.

## Assumptions

- The Feature-217 change already on `main` (`6df0d39`) is **sufficient** producer code; this feature adds no F# product code and changes no public surface — it is release cadence, package visibility, and registry bookkeeping only.
- The next version is a **preview bump** following the existing scheme (e.g. `0.1.53-preview.1`); the exact string is derived by the existing release machinery from the coherent-set tag, not hand-picked in the spec.
- The release is cut through the **existing** `release.yml` `publish-packages` job (packs the coherent set, pushes to the org feed with `GITHUB_TOKEN` `packages: write`) on a `v*` / coherent-set tag — no new publish pipeline is built.
- **Internal** visibility (org-wide read) is the preferred readability mechanism; if org policy forbids `internal` for this feed, granting `FS-GG/FS.GG.Templates` repo *Read* is an acceptable equivalent (the outcome — no exit 103 — is what matters).
- The visibility change is **admin-gated** (GitHub org package settings, UI or REST); if it cannot be completed in-session it is surfaced as an explicit manual step, consistent with the other admin-gated Coordination items.
- The downstream registry/compatibility update lives in `FS-GG/.github` and is partly coordinated cross-repo; FR-008 is satisfied by landing (or filing the linked PR for) that update, per ADR-0001's contract-change rule.
- The re-pin and the `29/29` composition verification (SC-004) are executed in **FS.GG.Templates** (per #29's "Downstream (we own, after publish)"); this feature's responsibility ends at delivering a both-feature-bearing-and-readable version plus the registry/issue/board closure, and confirming the downstream gate is unblocked.
