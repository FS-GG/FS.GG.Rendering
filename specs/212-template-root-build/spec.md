# Feature Specification: Root-buildable generated products (template emits root solution + build wrapper)

**Feature Branch**: `212-template-root-build`

**Created**: 2026-06-28

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → H1 · rendering (FS-GG/FS.GG.Rendering#9): Emit root `.slnx` + `Directory.Build.props` + `global.json` + a build-verb wrapper in the fs-gg-ui template so generated products are root-buildable with generic `dotnet build`/`dotnet run`, while keeping FAKE as the rich path; the release test asserts `dotnet build`/`dotnet test` at the product root.

## Context & Background

The fs-gg-ui template (`FS.GG.UI.Template`, contract id `fs-gg-ui-template`) generates products whose
build is driven exclusively by a FAKE script (`build.fsx`). A generated product today has **no root
solution file**: its projects live under `src/<Name>` and `tests/<Name>.Tests`, and nothing at the
product root ties them together for a stock .NET toolchain. As a result, a generic `dotnet build`,
`dotnet test`, or `dotnet run` invoked at the product root fails.

This blocks the SDD acceptance probes (FS-GG/FS.GG.SDD), whose composition tests invoke
**declared-or-default** build/run commands at the generated product root and expect them to succeed.
It is part of the org-wide "uniform build" pillar (FS-GG/.github#16) and is a `contract-change` against
`fs-gg-ui-template`.

The goal is to make every generated product **root-buildable with the stock .NET CLI** while keeping
FAKE as the richer, governed build path. The two paths must not diverge in what they build or verify.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generated product builds, tests, and runs at root with the stock toolchain (Priority: P1)

A consumer (a person, an SDD acceptance probe, or any CI that does not know about FAKE) scaffolds a
product from the template and, from the product root, runs the stock .NET commands. The product
restores, builds, and tests cleanly, and the app profile runs — without anyone having to discover or
invoke the FAKE script.

**Why this priority**: This is the unblocking outcome. Without a root solution and SDK pin, generic
`dotnet build`/`dotnet test`/`dotnet run` fail, and SDD's acceptance probes (which use stock commands)
cannot pass. Everything else in this feature is in service of this slice.

**Independent Test**: Scaffold a product from the template into an empty directory, then from the
product root run the stock restore → build → test → run sequence. The build and tests succeed and the
app profile starts, with no FAKE invocation.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded product (default `app` profile), **When** a consumer runs a stock
   build at the product root, **Then** the root solution resolves both the `src` product project and
   the `tests` project and the build succeeds.
2. **Given** a freshly scaffolded product, **When** a consumer runs the stock test command at the
   product root, **Then** the product test project is discovered and its tests run and pass.
3. **Given** a freshly scaffolded `app`-profile product, **When** a consumer runs the stock "run"
   command targeting the product source project, **Then** the application starts.
4. **Given** a machine whose default .NET SDK differs from the one the product targets, **When** a
   consumer builds at the product root, **Then** the product pins the expected SDK so the build is
   reproducible (or fails fast with a clear SDK-mismatch message) rather than silently building
   against an unexpected SDK.
5. **Given** the product name contains the casing/characters the template already supports, **When**
   the product is scaffolded, **Then** the root solution and SDK pin are emitted with the product's
   real name (not the template placeholder) and reference the correctly-named projects.

---

### User Story 2 - One uniform build vocabulary that delegates to the rich FAKE path (Priority: P2)

A developer or automation working in a generated product wants a single, predictable set of build
verbs — `restore`, `build`, `test`, `run`, `verify`, `pack` — that work the same way across every
FS-GG product, without needing to remember FAKE-specific invocation. Invoking a verb runs the
governed FAKE path (so the rich behavior, including `verify`, is preserved), giving one surface that
maps onto FAKE underneath.

**Why this priority**: The uniform verb wrapper is what makes the build *uniform* across repos (the
pillar's intent) and preserves the governed FAKE behavior as the rich path. It depends on P1 existing
but adds the cross-repo consistency the roadmap item calls for. It is valuable on its own even before
CI asserts it.

**Independent Test**: In a scaffolded product, invoke each verb through the wrapper on both supported
operating-system shells and confirm each verb performs the corresponding action by delegating to FAKE,
and that `verify` behaves exactly as FAKE's existing `Verify` does.

**Acceptance Scenarios**:

1. **Given** a scaffolded product, **When** a developer invokes the wrapper with each of
   `restore | build | test | run | verify | pack`, **Then** each verb delegates to the FAKE path and
   performs the corresponding action.
2. **Given** the wrapper exists, **When** it is invoked on either supported shell family (POSIX shell
   and Windows command), **Then** the same verbs are available and behave equivalently.
3. **Given** FAKE defines the rich `Verify` behavior today, **When** a consumer runs the wrapper's
   `verify` verb, **Then** the verification semantics are unchanged from FAKE's existing `Verify`.
4. **Given** an unknown or missing verb, **When** the wrapper is invoked, **Then** it reports the
   supported verbs rather than failing obscurely.

---

### User Story 3 - The release pipeline proves root buildability so it cannot silently regress (Priority: P3)

The template maintainer needs the template's release process to **prove** that a product scaffolded
from the just-built template is root-buildable, so a future change that breaks generic
`dotnet build`/`dotnet test`/`dotnet run` is caught at release time rather than by a downstream
consumer.

**Why this priority**: This is the regression guardrail. P1/P2 deliver the capability; P3 makes it
durable. It is lowest priority because the capability has value the moment it exists, but without the
gate it can rot.

**Independent Test**: Run the template release/instantiation test; confirm it scaffolds a product and
asserts that stock build and test at the product root succeed and that the app profile runs, failing
the release if any of those regress.

**Acceptance Scenarios**:

1. **Given** the template release/instantiation test, **When** it runs, **Then** it scaffolds a
   product from the template under test and asserts stock build **and** stock test at the product root
   both succeed.
2. **Given** the same test, **When** it runs against an `app`-profile product, **Then** it asserts the
   stock "run" of the product source project starts.
3. **Given** a regression that breaks root buildability, **When** the release test runs, **Then** it
   fails and blocks the release.

---

### Edge Cases

- **Profile variants**: Not every profile is a runnable app (e.g. `headless-scene`, `governed`,
  `sample-pack`). The root solution and verbs must build/test for every profile; the "run" assertion
  applies only to profiles that produce a runnable application.
- **Lifecycle variants**: The root build artifacts must be emitted for `lifecycle=spec-kit | sdd |
  none` alike — they belong to the generated product, not the gated lifecycle workspace that `sdd`/
  `none` suppress.
- **designSystem variants**: Emitting the root solution + SDK pin must be byte-neutral with respect to
  the `wcag` (default) vs `ant` design-system overlay — it changes build wiring, not product visuals.
- **Existing root files**: The template already ships `Directory.Build.props` and
  `Directory.Packages.props`; the feature must reconcile with those (not duplicate or fight them) and
  add only what is missing (root solution, SDK pin, verb wrapper).
- **Name rewriting**: The product name is substituted at scaffold time; the root solution, SDK pin,
  and any project references must carry the real product name, never the template placeholder.
- **Two paths agree**: A product built via stock CLI at root and the same product built via FAKE must
  build the same project set; the stock path must not silently exclude a project FAKE includes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The template MUST emit a root solution file at the generated product root that
  references the product source project under `src/` and the product test project under `tests/`,
  carrying the real (scaffolded) product name.
- **FR-002**: The template MUST emit a root SDK pin (`global.json`) so that a stock build at the
  product root is reproducible across machines whose default SDK differs.
- **FR-003**: The template MUST ensure the root build configuration (`Directory.Build.props` and any
  companion root settings) is present and consistent with the root solution, without duplicating or
  conflicting with the build properties the template already ships.
- **FR-004**: A consumer MUST be able to run, from the product root with the stock .NET CLI and no
  knowledge of FAKE: restore, build, test, and (for runnable profiles) run of the product source
  project — all succeeding for a freshly scaffolded product.
- **FR-005**: The template MUST emit a build-verb wrapper exposing the verbs `restore`, `build`,
  `test`, `run`, `verify`, and `pack`, available for both supported shell families (POSIX shell and
  Windows command).
- **FR-006**: Each wrapper verb MUST delegate to the governed FAKE path so that the rich build
  behavior is preserved through the uniform surface.
- **FR-007**: The feature MUST NOT change the semantics of FAKE's existing `Verify` behavior; the
  wrapper's `verify` verb MUST be equivalent to invoking FAKE's `Verify` directly.
- **FR-008**: The root build artifacts MUST be emitted for every profile and every `lifecycle` value
  (including `sdd`/`none`, which suppress the lifecycle workspace), and MUST be neutral with respect
  to the `designSystem` overlay.
- **FR-009**: The template's release/instantiation test MUST scaffold a product from the template
  under test and assert that stock build and stock test at the product root both succeed, and that the
  stock run of the product source project starts for a runnable profile; a regression MUST fail the
  release.
- **FR-010**: The set of projects built by the stock root path MUST match the set FAKE builds for the
  same product (no silent divergence between the two paths).
- **FR-011**: The change MUST be expressed as a `contract-change` against `fs-gg-ui-template` and keep
  the cross-repo registry/compatibility records coherent, since SDD's acceptance probes depend on this
  surface.

### Key Entities *(include if feature involves data)*

- **Generated product**: The scaffolded output of the template — a source project under `src/<Name>`,
  a test project under `tests/<Name>.Tests`, plus root build files. Root buildability is a property of
  this product.
- **Root solution**: The product-root file that ties the source and test projects together for the
  stock toolchain. Carries the real product name.
- **SDK pin**: The product-root declaration of which .NET SDK the product expects, ensuring
  reproducible stock builds.
- **Build-verb wrapper**: The product-root entry point exposing `restore|build|test|run|verify|pack`,
  delegating to the FAKE path.
- **FAKE build path**: The pre-existing rich/governed build script (`build.fsx`) that remains the
  authoritative build behavior, including `Verify`.
- **Release/instantiation test**: The template's release-time check that scaffolds a product and
  asserts root buildability.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of profiles produced by the template build and test successfully at the product
  root using only the stock toolchain, with no FAKE invocation required.
- **SC-002**: For every runnable profile, a stock "run" of the scaffolded product source project
  starts successfully from the product root.
- **SC-003**: All six verbs (`restore`, `build`, `test`, `run`, `verify`, `pack`) are available and
  behave equivalently on both supported shell families.
- **SC-004**: FAKE's `Verify` outcome for a scaffolded product is identical before and after this
  feature (no change in verification semantics).
- **SC-005**: The template release/instantiation test fails when root buildability regresses and
  passes when it holds — demonstrated by at least one passing run and one deliberately-broken run.
- **SC-006**: A stock root build on a machine whose default SDK differs from the product's expected
  SDK still produces the expected result (reproducible via the SDK pin), rather than silently building
  against a different SDK.
- **SC-007**: A consumer with no FAKE knowledge can go from "just scaffolded" to "built and tested at
  root" using only stock commands and the product's README, with no additional setup steps.

## Assumptions

- The template content root is `template/base/`, which already ships `Directory.Build.props`,
  `Directory.Packages.props`, FAKE `build.fsx`, and `fake.sh`/`fake.cmd`; this feature adds the root
  solution, the SDK pin, and the `build`-verb wrapper, and reconciles with what already exists rather
  than re-deriving it.
- "Stock toolchain" means the standard .NET CLI (`dotnet restore/build/test/run`) with no extra
  tooling beyond the pinned SDK.
- "Runnable profile" means a profile that produces an executable application (e.g. the default `app`
  profile); non-runnable profiles (e.g. `headless-scene`, `governed`, `sample-pack`) are exempt from
  the run assertion but not from build/test.
- The product source project lives under `src/<Name>` and the test project under `tests/<Name>.Tests`,
  per the template's current layout and `sourceName` rewriting.
- The release/instantiation test runs in the template's release workflow (`release.yml`) and can
  scaffold the template under test and invoke stock commands at the product root.
- This is a `contract-change` to `fs-gg-ui-template`; downstream consumers (notably SDD's
  composition-acceptance probes in FS-GG/.github#16) rely on the new root-buildable surface, and the
  cross-repo registry/compatibility records are updated as part of resolution.
- The two supported shell families are POSIX shell (`.sh`) and Windows command (`.cmd`), matching the
  existing `fake.sh`/`fake.cmd` pairing.
