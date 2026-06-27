# Feature Specification: Move git-init / chmod Out of the fs-gg-ui Template Post-Actions

**Feature Branch**: `205-scaffold-git-init-chmod`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" — resolved to Coordination board item **P1 · rendering — Move git-init/chmod out of template post-actions into scaffold path** (workstream: Lifecycle; contract: `fs-gg-ui-template`; phase P1 Rendering; effort M). Board note: *"CI-hang / VS-skip risk. Move to scaffold path or keep strictly behind skipGitInit."*

## Overview

When the `fs-gg-ui` template generates a product, it currently runs scripts **automatically as part of generation**: it marks the generated shell scripts executable (`chmod`) and, unless the caller opts out, initializes a Git repository and creates an initial commit. These run as template post-actions that execute a process during instantiation.

Running scripts automatically during template generation is the source of two recurring problems:

- **CI-hang risk** — a process that runs as a side effect of generation can block or hang in headless/automation environments, and the existing "continue on error" guard does not prevent a hang. Today the project's own validation tooling and tests must defensively pass an opt-out flag on every invocation to stay safe.
- **IDE-skip risk** — IDE-hosted template instantiation (the "create new project" experience) deliberately refuses to run process-executing post-actions for security, so the executable-bit fix silently does not happen there, and the result differs from a command-line scaffold for the same inputs.

This feature removes automatic, generation-time script execution from the template so that template generation is **side-effect-free by default**: generating a product never runs Git or `chmod` on its own. Responsibility for repository initialization and making scripts executable moves to the **scaffold path** — the orchestrator that drives template instantiation — which can perform them deterministically and observably. A standalone caller who instantiates the template directly (not through the scaffold orchestrator) retains a clear, **explicit opt-in** way to perform the same steps, plus the existing manual instructions, so no capability is lost — only the surprising automatic behavior is removed.

This is a P1 Rendering / Lifecycle item under ADR-0002 (composition by scaffold; the scaffold path owns lifecycle and orchestration). It is tracked separately from the sibling lifecycle-symbol item, which explicitly scoped these post-actions out.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generating a product never hangs or runs hidden scripts (Priority: P1)

An automation or CI job generates a product from the template as one step of a larger pipeline. Generation produces the expected files and returns promptly, without spawning Git or any other process as a side effect, so it cannot hang or block on a process the job did not ask for. The job does not need to remember to pass any defensive opt-out flag to stay safe.

**Why this priority**: Eliminating the CI-hang risk is the core reason the board item exists. It is the gate for the whole feature: if generation can still auto-run a process, the risk is not removed. It also removes the footgun that currently forces every internal caller to pass an opt-out flag defensively.

**Independent Test**: Generate each profile in a headless/automation context **without** passing any Git-related flag and confirm generation completes promptly, produces the expected file tree, and starts no Git repository and runs no external process as part of generation.

**Acceptance Scenarios**:

1. **Given** a caller generates any profile without specifying any Git-related option, **When** generation completes, **Then** no Git repository is created and no process is run as a side effect of generation.
2. **Given** an automation/CI invocation that does not pass any defensive opt-out flag, **When** it generates a product, **Then** generation completes without hanging and without requiring that flag.
3. **Given** a generated product directory, **When** generation has just completed by default, **Then** there is no auto-created initial commit and no auto-created repository inside it (unless it was generated inside an already-existing repository, which is left untouched).

---

### User Story 2 - The scaffold path initializes the repository and makes scripts executable (Priority: P1)

A product composed through the scaffold orchestrator (`fsgg-sdd scaffold --provider rendering`) still ends up, when appropriate, as an initialized Git repository with an initial commit and with its generated shell scripts marked executable. These steps are now performed by the scaffold path after instantiation, as explicit, observable steps the orchestrator controls — not as hidden effects of template generation.

**Why this priority**: "Move to scaffold path" is the stated destination of the board item and the ADR-0002 direction (the scaffold path owns orchestration). The capability must survive the move so orchestrated products are not worse off than before. The rendering-repo deliverable is to make the template stop owning these effects and to define the contract the scaffold path fulfills; the scaffold-side execution is owned by the scaffold orchestrator.

**Independent Test**: Drive a product through the scaffold path and confirm the resulting product has an initialized repository with an initial commit and executable generated scripts, achieved by the scaffold path's own steps rather than by template post-actions.

**Acceptance Scenarios**:

1. **Given** the scaffold path generates a product that is not already inside a repository, **When** scaffolding completes, **Then** the product is an initialized repository with an initial commit.
2. **Given** the scaffold path generates a product whose contract specifies the steps to run, **When** scaffolding completes, **Then** the generated shell scripts are executable.
3. **Given** the scaffold path generates a product **inside an existing repository**, **When** scaffolding completes, **Then** no nested repository is created and the surrounding repository is left intact.

---

### User Story 3 - A standalone caller can still opt in to initialization (Priority: P2)

A developer who instantiates the template **directly** (not via the scaffold orchestrator) and wants the old convenience — an initialized repository with an initial commit and executable scripts — gets it by passing a single explicit opt-in, or by following the manual instructions the template surfaces. With no opt-in, they get the safe, side-effect-free default from User Story 1.

**Why this priority**: Preserves the standalone direct-instantiation experience so removing the automatic behavior does not strand direct users, but it is below the safety and scaffold-path stories because it is a convenience path, not the risk being fixed.

**Independent Test**: Instantiate the template directly twice — once with no opt-in (confirm no repository, no process run) and once with the explicit opt-in (confirm initialized repository, initial commit, and executable scripts) — and confirm the manual instructions describing the steps are present in both cases.

**Acceptance Scenarios**:

1. **Given** a direct caller passes the explicit opt-in, **When** generation completes and the product is not already inside a repository, **Then** the repository is initialized with an initial commit and the generated scripts are executable.
2. **Given** a direct caller passes no opt-in, **When** generation completes, **Then** the behavior matches the side-effect-free default (no repository created, no process run).
3. **Given** any direct caller, **When** they inspect the generated output or the template's guidance, **Then** clear manual instructions describe how to initialize the repository and make scripts executable themselves.

### Edge Cases

- **Generated inside an existing repository**: Neither the scaffold path nor the explicit opt-in may create a nested repository or commit into the surrounding repository unexpectedly; an existing repository is detected and left untouched (today's behavior is preserved on this point).
- **Git not installed**: When repository initialization is requested but Git is unavailable, the step is skipped with a clear, non-fatal message rather than failing the scaffold or leaving a half-initialized state.
- **No shell scripts to mark**: When the selected profile/lifecycle emits no shell scripts (e.g., a non-`spec-kit` lifecycle), the make-executable step is a harmless no-op and never errors.
- **Cross-platform parity**: The behavior must be consistent regardless of host platform — generation is side-effect-free everywhere by default, and the opt-in / scaffold-path steps achieve the same end state on every supported platform (the make-executable step is simply unnecessary on platforms that have no executable bit).
- **Lifecycle interaction**: Under non-`spec-kit` lifecycles the gated lifecycle scripts are not emitted; the feature must not assume those files exist and must not reintroduce a hidden process to handle them.
- **Default-output expectations**: Existing callers and tests that previously relied on the *automatic* repository being created by default must be updated to request it explicitly (or via the scaffold path); the change in default behavior is intended and must be documented.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Template generation MUST NOT, by default, run any external process (including Git or file-permission changes) as a side effect of instantiation.
- **FR-002**: By default, generating a product MUST NOT initialize a Git repository or create a commit.
- **FR-003**: A standalone caller who instantiates the template directly MUST be able to opt in, with a single explicit option, to the previous convenience behavior (initialize repository, create initial commit, and mark generated scripts executable when the host supports it).
- **FR-004**: The capability to initialize the repository and make generated scripts executable MUST be available through the scaffold path, performed as explicit, observable steps of the scaffold orchestration rather than as hidden effects of template generation.
- **FR-005**: Repository initialization (whether via the scaffold path or the explicit opt-in) MUST detect an already-existing surrounding repository and leave it untouched — never creating a nested repository or committing into the parent unexpectedly.
- **FR-006**: When repository initialization is requested but Git is unavailable, the step MUST be skipped with a clear, non-fatal message and MUST NOT fail the overall generation/scaffold or leave a partially-initialized repository.
- **FR-007**: Making generated scripts executable MUST be a no-op (never an error) on hosts without an executable permission bit and when no shell scripts were emitted.
- **FR-008**: The feature MUST NOT change which **files** the template emits for any profile/lifecycle combination; it changes only the generation-time *behavior* (the automatic process execution), not the generated file content.
- **FR-009**: The template's documentation and surfaced guidance MUST be updated so callers understand the new side-effect-free default, the explicit opt-in for direct instantiation, and that the scaffold path owns initialization for composed products; clear manual instructions for performing the steps by hand MUST be retained.
- **FR-010**: Existing validation tooling, tests, and internal callers MUST be updated to the new default — they MUST NOT rely on the removed automatic behavior, and they MUST NOT need to pass a defensive opt-out flag to remain CI-safe.
- **FR-011**: The behavior MUST be consistent across all supported host platforms: side-effect-free by default everywhere, with the opt-in / scaffold-path steps reaching the same end state on each platform.
- **FR-012**: The new option(s) governing this behavior MUST be discoverable and self-describing to a caller inspecting the template's options, consistent with the template's existing options.

### Key Entities *(include if feature involves data)*

- **Generation-time side effect**: An action the template performs by running a process during instantiation (today: `chmod` on emitted scripts; `git init` + initial commit). This feature removes these from the default generation behavior.
- **Scaffold path**: The orchestrator that drives template instantiation (`fsgg-sdd scaffold --provider rendering`) and, after this change, owns repository initialization and making scripts executable as explicit steps.
- **Standalone opt-in**: The single explicit option a direct caller passes to reproduce the previous convenience (initialize repository + initial commit + executable scripts) without going through the scaffold path.
- **Initial repository state**: The end state for an orchestrated or opted-in product — an initialized repository with an initial commit and executable generated scripts — now produced outside template post-actions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Generating any of the four profiles with no Git-related option runs **zero** external processes as a side effect of generation and creates **zero** Git repositories.
- **SC-002**: An automation/CI invocation can generate any profile **without** passing any defensive opt-out flag and completes without hanging — the previously-required defensive flag is no longer needed anywhere in the repo's own tooling/tests.
- **SC-003**: A product produced through the scaffold path reaches the same end state as before (initialized repository, initial commit, executable scripts) in 100% of cases where it is not generated inside an existing repository.
- **SC-004**: A direct caller passing the explicit opt-in reaches that same end state; a direct caller passing nothing reaches the side-effect-free default — both outcomes verifiable without inspecting implementation details.
- **SC-005**: The set of files emitted for every profile/lifecycle combination is unchanged by this feature (the change is behavioral only; zero file-content diff attributable to this change).
- **SC-006**: Generation inside an existing repository never creates a nested repository and never commits into the surrounding repository (0 occurrences across all profiles and both the scaffold-path and opt-in cases).

## Assumptions

- **Resolution chosen: "move to scaffold path," with a retained explicit opt-in for standalone callers.** The board note offered two options ("Move to scaffold path **or** keep strictly behind skipGitInit"). Consistent with ADR-0002 (the scaffold path owns composition and lifecycle orchestration), the primary resolution is to make template generation side-effect-free and let the scaffold path own initialization, while keeping a single explicit opt-in so direct `dotnet new`-style callers are not stranded. This is the central decision of the feature; if the preferred resolution is instead "keep the post-actions but never run them unless explicitly opted in" (a pure default-flip without moving ownership to the scaffold path), reopen via `/speckit-clarify`.
- **Default behavior changes intentionally.** Unlike the sibling lifecycle-symbol feature, this feature deliberately changes the *default generation behavior* (today a direct generation auto-creates a repository unless opted out; afterward it does not). The "no-diff default" guarantee here applies to **emitted files**, not to the removed automatic process behavior.
- **Scaffold-side execution is owned by the scaffold orchestrator.** The rendering-repo deliverable is to stop the template owning these effects and to define the behavior the scaffold path fulfills. The actual initialization/permission steps inside `fsgg-sdd scaffold` are owned by the SDD-side scaffold orchestrator (a cross-repo concern coordinated under the `fs-gg-ui-template` contract); this feature does not re-implement the orchestrator.
- **Existing opt-out semantics inform the opt-in.** The template already exposes a Git-initialization toggle; this feature reshapes the default and the option's meaning so the default is side-effect-free and initialization is explicit, rather than adding an unrelated mechanism.
- **Manual instructions are retained.** The template already surfaces manual instructions for the chmod and git steps; those remain so any caller can perform the steps by hand regardless of host or tooling.
- **Lifecycle-symbol feature is a sibling, not a dependency for file emission.** This feature composes with the `lifecycle` choice (non-`spec-kit` lifecycles emit fewer/zero shell scripts) but does not depend on it for correctness; the make-executable step degrades to a no-op when no scripts are emitted.
- **No new product runtime behavior.** This feature concerns only template generation/scaffold orchestration behavior; it does not change the generated product's runtime, source, or tests beyond updating those that asserted the removed automatic behavior.

## Dependencies

- **Contract**: `fs-gg-ui-template` (the template option surface and its instantiation behavior). A behavioral change to how the template instantiates is a change to this contract; coordinate per the cross-repo registry if the scaffold path's expectations shift.
- **ADR-0002**: composition by scaffold — establishes that the scaffold path owns lifecycle/orchestration, which is the basis for moving these steps off the template.
- **Sibling item**: P1 · rendering — lifecycle choice symbol (Done) — this feature was explicitly scoped out of that one and is its follow-on.
