# Implementation Plan: Move git-init / chmod Out of the fs-gg-ui Template Post-Actions

**Branch**: `205-scaffold-git-init-chmod` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/205-scaffold-git-init-chmod/spec.md`

## Summary

The `fs-gg-ui` `dotnet new` template runs three platform-conditional **post-actions** at
generation time (`.template.config/template.json`, lines 299–351): on Unix it `chmod +x`'s
emitted shell scripts and, unless `--skipGitInit true`, runs `git init && git add . && git commit`;
on Windows it runs the same Git steps via PowerShell. Because these run a *process as a side effect
of generation*, they hang in headless/CI contexts (the repo's own validation scripts carry a
300-second wait-and-`Kill` loop to defend against the spinning post-action — `scripts/validate-lifecycle-template.fsx:222–230`)
and are silently skipped by IDE "create new project" hosts that refuse process post-actions.

This feature makes template generation **side-effect-free by default**: remove the
auto-running post-actions, **flip the option from an opt-out (`skipGitInit`, default `false` ⇒
auto-init) to a single explicit opt-in** (`initGit`, default `false` ⇒ do nothing), and have the
opt-in (when passed) run the same chmod + git-init + initial-commit convenience for direct callers.
The **scaffold path** (`fsgg-sdd scaffold --provider rendering`, owned by the SDD repo) becomes the
owner of these steps for composed products; this rendering-repo deliverable is to **stop the
template owning the effects** and to **publish the contract** the scaffold path fulfils. Manual
instructions are retained and always surfaced. No emitted **files** change for any
profile/lifecycle (FR-008/SC-005) — only generation-time behavior.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The CI-hang / IDE-skip root cause is treated as confirmed by the existing in-repo evidence
> (the kill-loop in `validate-lifecycle-template.fsx`, the `skipGitInit` defensive flag threaded
> through every validation invocation). `/speckit-tasks` MUST still schedule an **early live
> generation smoke** in the Foundational phase: scaffold each profile with **no** Git flag in a
> headless context and observe that generation returns promptly, starts no process, and creates no
> repository — confirming the behavioral fix on the real `dotnet new` host before user-story work.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (template metadata is JSON; validation tooling is F#
`.fsx`; tests are F# in `tests/Package.Tests/`). No product-runtime F# changes.

**Primary Dependencies**: `dotnet new` template engine (Template Engine post-action processors);
`git`, `bash`/`powershell` at the host (now invoked only on opt-in / by the scaffold path).

**Storage**: N/A (filesystem template emission only).

**Testing**: F# package/template tests under `tests/Package.Tests/`
(`Feature204LifecycleTemplateTests.fs`, `GeneratedConsumerValidationTests.fs`) and the live
validation scripts `scripts/validate-lifecycle-template.fsx`, `scripts/validate-design-system-template.fsx`.
Add a Feature-205 behavioral test asserting (a) default generation creates no `.git` and runs no
process, (b) `--initGit true` produces an initialized repo + executable scripts, (c) generation
inside an existing repo creates no nested repo.

**Target Platform**: Cross-platform template instantiation (Linux/macOS/Windows + IDE hosts).

**Project Type**: `dotnet new` template + its packaging/validation tooling (not a library API change).

**Performance Goals**: Default generation completes promptly with **zero** spawned processes
(removes the 300 s defensive wait entirely).

**Constraints**: Zero file-content diff for emitted products (behavioral-only change); cross-platform
parity; never create a nested repo or commit into a surrounding repo; degrade non-fatally when git
is absent or no shell scripts were emitted.

**Scale/Scope**: One template manifest, its README/docs, two validation `.fsx` scripts, the
package tests, and one published contract doc. Four profiles × lifecycle/designSystem matrix.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Change Classification — Tier 1 (contracted change).** This alters the `fs-gg-ui-template`
  option surface (removes `skipGitInit`, adds `initGit`) and changes observable generation
  behavior. Requires the full artifact chain: spec ✅, plan (this), contract update (Phase 1
  `contracts/`), test evidence (Feature-205 test), and documentation updates (template README +
  generated docs). **PASS** — chain scheduled.
- **I. Spec → FSI → Semantic Tests → Implementation.** No public F# module is added or changed;
  the contract surface here is `template.json` + the published template contract doc, not an
  `.fsi`. The FSI-first mandate is **N/A** to template metadata; the analogous discipline (design
  the option surface, then test it through the real `dotnet new` invocation a user runs) is
  honored via the live validation scripts and the Feature-205 test. **PASS.**
- **II. Visibility in `.fsi`.** No `.fs` public modules touched ⇒ no `.fsi`/surface-baseline
  drift. **PASS / N/A.**
- **III. Idiomatic Simplicity.** Net simplification: deletes per-platform auto-run post-actions and
  the defensive `skipGitInit true` threading + kill-loop scaffolding. No clever features
  introduced. **PASS.**
- **IV. Elmish/MVU boundary.** No stateful/I-O *product* workflow added; the only I/O (git/chmod)
  is moved out of generation to an explicit, observable step. **PASS / N/A.**
- **V. Test Evidence.** New behavioral test fails before (default generation currently creates a
  repo) and passes after. Live scaffold evidence is real (real `dotnet new`, real filesystem); no
  synthetic evidence required. **PASS.**
- **VI. Observability & Safe Failure.** Opt-in / scaffold-path git step must emit a clear,
  non-fatal message when git is absent (FR-006) and never leave a half-initialized repo. **PASS** —
  encoded in the contract.

**No violations → Complexity Tracking table omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/205-scaffold-git-init-chmod/
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 — option-shape, manual-instruction surfacing, cross-repo decisions
├── data-model.md        # Phase 1 — option surface, post-action states, behavior matrix
├── quickstart.md        # Phase 1 — runnable validation scenarios (SC-001..SC-006)
├── contracts/
│   └── fs-gg-ui-template-generation.md   # Phase 1 — published template-behavior contract
├── checklists/
│   └── requirements.md  # (already present)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
.template.config/
└── template.json                 # REMOVE 3 auto-run postActions; REMOVE `skipGitInit` symbol;
                                   #   ADD `initGit` (bool, default false) opt-in symbol;
                                   #   ADD opt-in-gated chmod+git postAction(s) (Unix + Windows);
                                   #   ADD always-on instructions-only postAction (manual steps)

.template.package/
└── README.md                     # Options table: drop `--skipGitInit`, add `--initGit`; explain
                                   #   side-effect-free default + scaffold-path ownership

template/base/
├── README.md                     # Generated-product guidance: manual init/chmod instructions
└── docs/                         # (if a generated doc references git-init, update wording)

scripts/
├── validate-lifecycle-template.fsx       # Drop `--skipGitInit true`; remove/relax the 300 s
│                                          #   wait+Kill loop now that generation is side-effect-free
└── validate-design-system-template.fsx   # Same: drop defensive flag, simplify post-action guard

tests/Package.Tests/
├── Feature205TemplateSideEffectTests.fs  # NEW — default no-process/no-repo; opt-in end-state;
│                                          #   existing-repo no-nest; emitted-file-set unchanged
├── Feature204LifecycleTemplateTests.fs   # Update if it asserted skipGitInit/post-action behavior
└── GeneratedConsumerValidationTests.fs   # Update any reliance on the removed auto-init default

CLAUDE.md                          # Repoint SPECKIT plan marker to this plan
```

**Structure Decision**: This is a template-metadata + tooling/docs change, not a library feature.
The contract surface is `.template.config/template.json` (consumed via `dotnet new fs-gg-ui`),
mirrored by the published contract doc under `contracts/`. No `src/` product code, no `.fsi`, no
surface-area baseline is touched. The SDD-side scaffold orchestrator that *executes* the moved
steps lives in another repo and is coordinated via the `fs-gg-ui-template` contract (see Phase 0
cross-repo note); this plan does not implement it.

## Complexity Tracking

*No constitution violations — table intentionally empty.*
