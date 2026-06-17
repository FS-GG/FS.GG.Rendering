<!--
Sync Impact Report
==================
Adapted from: FS-Skia-UI Constitution v1.3.0 (sibling repo, source/provenance only)
This file: FS.GG.Rendering Constitution v1.1.0

Version change: 1.0.0 → 1.1.0
Bump rationale: Material governance update aligning package identity with the accepted
Stage R8 rebrand decision (`FS.Skia.UI.*` → `FS.GG.UI.*`) already reflected in source,
README, and product decision records.

Principles (renamed/renumbered from source):
- I.   Spec → FSI → Semantic Tests → Implementation   (kept; "by hope" wording trimmed)
- II.  Visibility Lives in `.fsi`, Not in `.fs`        (kept)
- III. Idiomatic Simplicity Is the Default             (kept)
- IV.  Elmish/MVU Is the Boundary for Stateful/I-O     (kept; synthetic cross-ref removed)
- V.   Test Evidence Is Mandatory                      (kept; lightweight synthetic disclosure folded in)
- VI.  Observability and Safe Failure                  (kept)

Removed from source (not applicable to a standard Spec Kit rendering repo):
- Principle "Synthetic Evidence Requires Loud, Repeated Disclosure" — the heavy
  machinery ([S]/[S*]/[SEH] task markers, EvidenceAudit after_implement hook,
  --accept-synthetic override, readiness/synthetic-evidence.json). The durable
  lesson (prefer real evidence; disclose synthetic) is folded into Principle V.
- "Local Agent Skills" section's mandatory gates — skillist task metadata, the
  post-generation skill evaluation gate, the pre-task skill loading gate, and
  readiness-blocking on skills. Replaced by an advisory "Local Skills" section.
- "Workflow & Quality Gates" gate ladder (specification/planning/task/
  implementation/evidence gates incl. evidence audit + skill gates). Replaced by
  a lightweight "Development Workflow" mapped to standard Spec Kit.

Renderer change: Vulkan → OpenGL (GL). This repository targets SkiaSharp over
OpenGL; Vulkan is explicitly out of scope here.

Sections:
- Added: Local Skills (advisory)
- Changed: Engineering Constraints (package identity aligned to `FS.GG.UI.*`; GL backend;
  governance/evidence constraints dropped)
- Changed: Development Workflow (standard Spec Kit; gate ladder removed)
- Kept:    Change Classification (Tier 1 / Tier 2)

Templates / artifacts reviewed:
- .specify/templates/plan-template.md      ✅ generic "Constitution Check" gate, no edits needed
- .specify/templates/spec-template.md      ✅ no governance terms, no edits needed
- .specify/templates/tasks-template.md     ✅ no skillist/evidence terms, no edits needed
- .specify/templates/checklist-template.md ✅ no edits needed
- docs/product/decisions/0001-package-identity.md ✅ records accepted Stage R8 rebrand
- README.md ✅ already documents `FS.GG.UI.*` packages
- .specify/templates/commands/ ⚠ not present in this checkout; no command templates to update

Deferred TODOs:
- TODO(STRUCTURED_LOGGING): logging library not yet selected; record in an ADR when chosen.
-->

# FS.GG.Rendering Constitution

The rendering/runtime repository owns the F# UI framework as a product: scene,
layout, input, the SkiaSharp viewer/host, Elmish integration, controls, the
design-system/theme layers, testing helpers, packages, docs, and templates. It
MUST be buildable, testable, documentable, packable, and releasable with normal
repository tooling and standard Spec Kit — without depending on any external
governance platform.

## Core Principles

### I. Spec → FSI → Semantic Tests → Implementation

Every non-trivial change MUST follow this order:

1. **Specify.** The feature spec names the user-visible outcome, scope
   boundaries, change classification (Tier 1 / Tier 2), public API impact, and
   verification approach.
2. **Sketch in FSI.** The intended public surface is drafted as a `.fsi`
   signature and exercised interactively in F# Interactive before any `.fs`
   implementation exists. API shape is validated by use.
3. **Semantic tests for FSI.** Tests MUST exercise the API through the same FSI
   surface a human or script would use: load the packed library (or a prelude
   script) and call the public functions. Tests assert behavior, not internals.
4. **Implement.** Write the `.fs` body against the now-stable signature and
   passing tests.

Rationale: FSI is the honest audience. If the shape is awkward in FSI, it is
awkward in production. Designing through FSI catches API mistakes before `.fs`
code exists to defend them.

### II. Visibility Lives in `.fsi`, Not in `.fs`

Every public F# module MUST have a corresponding `.fsi` signature file. The
`.fsi` is the sole declaration of the module's public surface; symbols omitted
from the `.fsi` are private — the F# compiler enforces this.

Therefore `.fs` files MUST NOT carry `private`, `internal`, or `public` access
modifiers on top-level bindings. Visibility is determined by presence or absence
in the `.fsi`, not by keywords scattered across `.fs`. Surface-area baselines
MUST be maintained per public module and validated by an automated test (an API
surface-drift check).

Rationale: Two sources of truth for visibility is one too many. The `.fsi`
already gives the compiler the full picture; access modifiers in `.fs` only
invite drift.

### III. Idiomatic Simplicity Is the Default

Code SHOULD prefer the plainest F# that solves the problem: functions over
classes, records over hierarchies, pipelines over mutation, the standard library
over clever abstractions. A reader should not need a textbook to follow ordinary
code.

Complex features MAY be used, but their use MUST be justified in the feature's
spec or plan. The following require explicit justification:

- Custom operators beyond the F# standard set
- Statically-resolved type parameters (SRTP) and inline tricks that force it
- Reflection and dynamic dispatch
- Non-trivial computation expressions (beyond `async`, `task`, `option`, `result`, `seq`)
- Type providers
- Active patterns beyond single-case or simple discriminants

If such a feature appears without matching justification, the reviewer treats it
as a spec defect, not a code defect.

**Mutation is allowed when it is the simpler or faster code.** `mutable`
bindings, `for` / `while` loops, and `ref` cells MAY be used when they are
demonstrably plainer than the immutable alternative or are needed on a measured
hot path — a single unaliased accumulator, an inner loop over a buffer, a
performance-critical render routine. Disclose the reason at the use site with a
one-line comment (e.g. `// mutable: hot path`) so a reader doesn't waste effort
"fixing" it.

**Recursion is for branching structure, not for hiding state.** `let rec` fits
genuinely recursive problems — state-machine transitions, tree / graph walks,
branching evaluators, parser combinators. It is the wrong tool when its only
purpose is to thread an accumulator through self-calls to avoid a `mutable`;
there the `mutable` is clearer — prefer it.

Rationale: Complexity compounds in F# because the language rewards expressive
tricks, so a simplicity bias keeps code legible. Dogmatic immutability is itself
the cleverness this principle discourages.

### IV. Elmish/MVU Is the Boundary for Stateful or I/O Workflows

Any feature with multi-step state, external I/O, retries, user interaction,
background work, or operational recovery MUST model its behavior through an
Elmish-style Model-View-Update boundary before implementation. Simple pure
functions do not need Elmish ceremony, but once behavior includes stateful
workflow or I/O, the public `.fsi` surface MUST expose or clearly wrap:

- `Model` — the durable state the workflow owns
- `Msg` — the events, user actions, external responses, and internal transitions
  the workflow accepts
- `Effect` or `Cmd<Msg>` — the I/O the workflow requests but does not execute
  inside `update`
- `init` — initial state plus requested startup effects
- `update` — a pure transition from `Msg` and `Model` to next `Model` plus effects
- an interpreter at the edge that executes effects and turns results back into `Msg`

The Elmish package is the preferred runtime when the host benefits from its
`Program`, `Cmd`, subscription, or renderer integration. For libraries, CLIs,
services, and small hosts, a local MVU/effect algebra is acceptable when it
preserves the same separation: `update` is pure, I/O is represented as data or
`Cmd<Msg>`, and interpretation happens only at the edge.

Semantic tests MUST cover both sides of the boundary:

- pure transition tests: given `Model` + `Msg`, assert the next `Model` and
  emitted effects
- interpreter tests: execute effects against real filesystem, process, network,
  or other real dependencies where safe
- FSI transcripts: exercise `init` and representative `update` paths through the
  packed library or prelude, not private helpers

Rationale: Elmish makes the hard part observable. State transitions become plain
values that can be tested exhaustively, and I/O becomes an explicit contract that
can be audited, interpreted, and exercised with real evidence.

### V. Test Evidence Is Mandatory

Behavior-changing code MUST include automated tests that fail before the change
and pass after. Prefer tests that run against real dependencies (real
filesystem, real GL/window-system surface, real network where safe).

Tests blocked by out-of-scope issues MUST be marked skipped (the test
framework's skip mechanism) with written rationale. Never mark a failing test as
passed. Never weaken an assertion to green a build — narrow the scope instead,
and document it.

**Synthetic evidence** — mocks, stubs, fakes, hardcoded fixtures, in-memory
substitutes, canned responses — MAY be used when real evidence is unavailable or
prohibitively expensive AND a real-evidence path is planned or documented as
infeasible. Every synthetic use MUST be disclosed at the use site with a comment
naming the fact and reason (e.g. `// SYNTHETIC: no GL context in CI; real path
tracked in <issue>`), MUST carry the token `Synthetic` in the test name, and MUST
be listed in the PR description. Prefer explicit, ugly literals over clever
factories that make synthetic data feel real.

Rationale: Synthetic evidence is the quiet failure mode of "passing" tests.
Visible disclosure keeps it honest without requiring a governance platform.

### VI. Observability and Safe Failure

Operationally significant events (startup, subsystem initialization, GL/context
creation, asset/IO failure, recovery paths) MUST emit structured diagnostics
with actionable context. Errors MUST fail fast or degrade explicitly; silent
failure and swallowed exceptions are forbidden in critical paths.

GL smoke failures MUST distinguish implementation defects from a missing
window-system or presentation setup, rather than assuming GPU access is
unavailable.

## Change Classification

Every feature declares a tier in its spec:

- **Tier 1 (contracted change)** — adds, removes, or modifies public API
  surface; introduces new dependencies; changes inter-project or package
  contracts; or alters observable behavior covered by existing specs. Requires
  the full artifact chain: spec, plan, `.fsi` updates, surface-area baseline
  updates, test evidence, and documentation updates.
- **Tier 2 (internal change)** — refactors, performance, or internal cleanup
  with no behavioral change. Requires spec and tests; `.fsi` and baselines remain
  untouched.

A Tier 1 change that fails to update `.fsi` or baselines is a defect, regardless
of whether tests pass.

## Engineering Constraints

- F# on .NET is the exclusive stack. Cross-language integration, if ever needed,
  uses gRPC or OpenAPI over separate projects.
- Rendering backend is SkiaSharp over **OpenGL (GL)**. Vulkan is out of scope for
  this repository.
- Target framework is .NET `net10.0` unless a plan justifies a narrower target.
- SkiaSharp is pinned to explicit versions; preview packages require an explicit
  version pin.
- Every public `.fs` module requires a curated `.fsi`.
- Stateful or I/O-bearing features use an Elmish/MVU boundary (`Model`, `Msg`,
  `Effect` or `Cmd<Msg>`, pure `update`, edge interpreter).
- Surface-area baseline files are required for each public module.
- Public API changes document compatibility impact and migration guidance.
- Dependencies are minimized; each new dependency states need, version-pinning
  strategy, and maintenance owner.
- Controls, design-system primitives, themes, and design-specific kits are
  distinct layers. There is one semantic control set with multiple themes;
  controls MUST NOT fork per theme (no `AntButton`/`FluentButton` behavior
  copies). A design-specific kit is justified only when a design language adds
  composition or workflow behavior beyond styling.
- Package identity is `FS.GG.UI.*` as accepted in
  `docs/product/decisions/0001-package-identity.md`; legacy `FS.Skia.UI.*` IDs
  remain deprecated/frozen migration identities. Any future rebrand away from
  `FS.GG.UI.*` is a separate, explicit release decision, not part of ordinary
  work.
- Pack output location: `~/.local/share/nuget-local/`.
- Structured-logging library: TODO(STRUCTURED_LOGGING) — not yet selected; record
  the choice in an ADR.

## Local Skills

Repo-local skills under `.claude/skills/`, and package-owned `src/*/skill/SKILL.md`
files imported alongside their packages, are **advisory aids**. When a task
matches a skill's description, contributors SHOULD consult it and prefer it over
generic guidance; when several apply, use the minimal set that covers the work.

Skills are not gates. There is no mandatory skill-loading step, no `skillist`
task metadata, and skill usage never blocks task completion or merge readiness.
A contributor can clone this repository, read the standard Spec Kit artifacts,
run the documented build/test commands, and ship a routine rendering change
without loading any skill.

## Development Workflow

Use standard Spec Kit for feature work: specify → plan → tasks → implement.
`spec.md`, `plan.md`, and `tasks.md` are authored artifacts, not a generated
graph; no custom feature/product/project graph is the source of truth for
ordinary rendering work.

Repo-owned checks are kept only when they are narrow and pay for themselves —
for example API surface-drift checks, package-skew checks, docs build checks,
template pack/install/instantiate checks, and release packaging checks. Each
active check SHOULD have a short justification: what product contract it
protects, when it runs, who owns it, and what it costs. No check requires an
external governance repository.

Any intentional deferral MUST be explicit in the spec or plan and scoped as a
bounded follow-up.

## Governance

This constitution overrides conflicting local habits, informal preferences, and
agent prompts for work in this repository. Compliance review SHOULD occur at
specification, planning, implementation review, and merge readiness review.

**Amendment procedure:** PR with rationale and migration impact; maintainer
review required. Amendments MUST update dependent templates and guidance files in
the same change. When the constitution and a template disagree, the constitution
is correct and the template is defective until synchronized.

**Versioning policy:**

- MAJOR — backward-incompatible governance changes or principle removals
- MINOR — new principles, new mandatory constraints, or materially expanded
  obligations
- PATCH — clarifications that do not change the meaning of the rules

**Version**: 1.1.0 | **Ratified**: 2026-06-14 | **Last Amended**: 2026-06-17
