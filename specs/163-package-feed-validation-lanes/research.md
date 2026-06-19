# Research: Package Feed Validation Lanes

## Decision: Discover current package versions from packable source projects

**Rationale**: The authoritative package version for local package-consuming sample validation is
the source-controlled packable project metadata. The validation tool will scan `src/*/*.fsproj`
files whose `PackageId` starts with `FS.GG.UI.` and whose project is packable, then record
`PackageId`, project path, and `Version`. Expected sample pins are package-specific so the workflow
can handle the edge case where packable projects do not all share one version.

**Alternatives considered**:

- Use one hardcoded repository version: rejected because the spec requires detecting package-
  specific drift and handles non-uniform package versions.
- Read versions from package files in the local feed: rejected because a stale or missing feed is
  one of the failure modes this feature must detect.
- Use sample package pins as authority: rejected because stale sample pins are the primary trap.

## Decision: Validate package pins before sample build or test execution

**Rationale**: Stale `FS.GG.UI.*` pins should fail fast without waiting for restore, build, or
sample tests. The check will parse selected sample `.fsproj` files for `PackageReference` entries
matching discovered package ids and report package id, expected version, actual declared version,
and sample path. Compatibility exceptions are allowed only when explicitly recorded in evidence.

**Alternatives considered**:

- Detect drift from build failures: rejected because stale packages can still build and produce
  false confidence.
- Normalize pins through central package management: rejected because package-consuming samples
  intentionally pin inline to model real consumers.
- Update every sample unconditionally: rejected because selected samples and compatibility
  exceptions must remain reviewer-visible.

## Decision: Use package source mapping plus an isolated package cache for source proof

**Rationale**: Matching version strings does not prove source selection. The source proof will
generate a temporary NuGet config that maps `FS.GG.UI.*` only to the local feed and maps approved
third-party packages to approved external sources. Restore runs with a dedicated `NUGET_PACKAGES`
cache by default, records feed/cache paths, and parses restore output plus generated assets to
verify resolved package ids and versions. This avoids destructive global-cache clearing in the
default workflow while still preventing a stale global cache from satisfying the proof.

**Alternatives considered**:

- Clear the global NuGet cache by default: rejected because the spec forbids destructive global
  cache clearing unless explicitly requested.
- Rely only on `samples/*/nuget.config`: rejected because source mapping must be proven for the
  selected run and evidence must record the exact rules used.
- Inspect `project.assets.json` without an isolated cache: rejected because assets alone do not
  prove the package could not have come from an unintended cache/source.

## Decision: Make local-feed refresh explicit and evidence-producing

**Rationale**: Maintainers need one repeatable workflow to pack current `FS.GG.UI.*` packages into
`~/.local/share/nuget-local/`, refresh selected sample pins, and verify the result. The refresh
mode will record packable projects, expected packages, feed path, packages found in the feed,
sample files updated, and any stale or missing packages. Multiple versions in the local feed are
disclosed; missing expected versions fail.

**Alternatives considered**:

- Only check pins and leave packing manual: rejected because the retrospective identifies feed
  refresh as part of the trap.
- Delete older packages from the local feed automatically: rejected because it is destructive and
  unnecessary when exact pins plus source mapping are enforced.
- Hide refresh edits inside restore: rejected because source-controlled sample pin changes must be
  reviewable.

## Decision: Implement validation lanes as declarative process lanes with isolated outputs

**Rationale**: A single solution-wide test command can hang or hide which lane is blocked. The lane
runner will execute named lanes such as `package-proof`, `antshowcase-sample`, `controls`,
`rendering-harness`, and `aggregate-solution`. Each lane declares its command, result directory,
log file, timeout, optional no-progress timeout, required/optional classification, and output
isolation. Dotnet test lanes get separate output roots when concurrently eligible to avoid runtime
file locks.

**Alternatives considered**:

- Keep one aggregate validation command: rejected because it was the failure mode recorded in the
  retrospective.
- Run all lanes serially only: rejected because some lanes can run concurrently when outputs are
  isolated.
- Use shell-only timeout wrappers as the core model: rejected because tests and summaries need a
  typed status model, not only process exit codes.

## Decision: Classify readiness fail-closed from lane and package proof status

**Rationale**: The final summary must make incomplete evidence visible. Required lanes that are
failed, timed out, hung/no-progress, canceled, skipped, not run, or environment-limited without an
accepted exception keep overall readiness blocked or incomplete. Focused lane success is reported
separately from aggregate full-solution validation, so a skipped/canceled aggregate lane cannot be
mistaken for a green release gate.

**Alternatives considered**:

- Treat skipped aggregate validation as acceptable when focused lanes pass: rejected because the
  spec requires explicit distinction and honest caveats.
- Collapse all non-passing states into `failed`: rejected because reviewers need different
  remediation paths for timeout, cancel, skip, environment limitation, and source violation.
- Produce only Markdown summaries: rejected because machine-checkable status is needed for tests
  and future CI wiring.

## Decision: Keep the reusable model in Rendering.Harness with thin scripts

**Rationale**: The workflows are stateful, I/O-heavy, and need tests. `Rendering.Harness` already
owns repository validation evidence and can expose `.fsi`-curated models for package feed proof,
lane results, and summaries. Thin scripts under `scripts/` provide simple maintainer commands while
delegating core behavior to the tested harness.

**Alternatives considered**:

- Put all behavior in standalone `.fsx` scripts: rejected because it would bypass the repository's
  `.fsi` and semantic-test discipline for non-trivial workflows.
- Add a new package-visible `FS.GG.UI.*` library: rejected because no public UI framework API is
  needed for this repository validation feature.
- Add an external validation tool dependency: rejected because built-in .NET/NuGet/process APIs are
  sufficient and simpler to maintain.
