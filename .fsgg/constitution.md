# Product Constitution

This constitution governs spec-driven development for this product. It is the
highest-precedence engineering authority here: it overrides conflicting habits,
prompts, and generated plans. It was seeded by the SDD skeleton as a populated
baseline; ratify or amend it to fit this product, then treat it as the contract
every change is measured against.

## Core Principles

### I. Specify Before Implementing

Every non-trivial change MUST start from a written specification: the
user-visible outcome, the scope boundary, the change tier, the public-surface
impact, and how the change will be verified. Sketch the public shape and exercise
it interactively before the implementation hardens it. Specs precede code so that
humans, scripts, and agents can agree on the contract while it is still cheap to
change.

### II. Structured Artifacts Are the Machine Contract

Markdown is an authoring surface for humans. Schema-versioned structured artifacts
are the contract tools and gates rely on. Each lifecycle stage MUST declare which
data is authoritative; when prose and structured data disagree, the plan MUST say
which wins, how the conflict is reported, and which view records it. Avoid
replacing prose drift with schema drift: keep typed contracts stable and
versioned.

### III. Public Surface Is Declared, Not Incidental

The public surface of a module MUST be declared explicitly — in signature files
where the language supports them — rather than left as a side effect of
implementation. Maintain a surface baseline once code exists. A contracted change
that does not update signatures, baselines, tests, and docs together is
incomplete.

### IV. Idiomatic Simplicity Is the Default

Prefer the plain, idiomatic form: functions over classes, records and discriminated
unions over hierarchies, simple modules over frameworks, and the standard library
over clever abstractions. Reach for advanced or metaprogramming features only with
a justification recorded in the plan. Mutation and loops are allowed where they are
clearer or measurably necessary; say so in a short comment.

### V. Model–Update–Effect Is the Boundary for State and I/O

Any workflow with multi-step state or external I/O MUST expose or clearly wrap a
Model–Update–Effect boundary: durable state, explicit messages, requested effects,
a pure transition, and an edge interpreter that performs the real I/O. Pure
parsers, data models, and validators need no such ceremony. Keep I/O out of pure
transitions so behavior stays testable and deterministic.

### VI. Test Evidence Is Mandatory

Behavior-changing code MUST ship with automated tests that fail before the change
and pass after. Prefer real filesystem, process, and schema fixtures over mocks;
when synthetic data is unavoidable, disclose it near the test and say what real
path it stands in for. Generated views, schema migrations, and output contracts
need snapshot or golden coverage once they are tool-facing.

### VII. Agents and Humans Share One Contract

Command-line users, automation, and coding agents MUST operate over the same
artifacts. Agent prompts and skills may help author files, but they are never a
second source of truth. If an agent writes an authoring surface, the corresponding
structured model and views are refreshed by the workflow or report a stale-view
diagnostic.

### VIII. Observability and Safe Failure

Operationally significant events MUST produce actionable diagnostics: parse
failures, missing artifacts, stale views, conflicting state, and integration
failures. Distinguish malformed user input from tool defects. Critical paths fail
fast and visibly; optional integrations degrade explicitly rather than silently.

## Change Classification

Every change declares a tier in its spec:

- **Tier 1 (contracted change):** public surface, schema, generated view, command,
  artifact layout, agent-skill contract, or external integration. Requires a spec,
  a plan, tasks, signatures where code exists, tests, docs, and migration notes
  when applicable.
- **Tier 2 (internal change):** implementation cleanup with no externally visible
  contract change. Requires a spec and tests; signatures and baselines stay
  unchanged.

## Development Workflow

Use the spec-driven loop: specify, clarify as needed, plan, break into tasks,
implement, and analyze before merge. For lifecycle features, the plan identifies
the authored artifacts, the structured contracts, the generated views, the schema
and migration posture, the agent-facing behavior, any optional external governance
integration, and the tests and fixtures that cover stale or conflicting artifacts.

## Governance

This constitution overrides conflicting local habits, prompts, and generated plans.
Amendments require a change with a stated rationale and migration impact. When the
constitution and a template disagree, the constitution wins and the template is
defective until synchronized.

Versioning policy:

- MAJOR: backward-incompatible principle or governance changes.
- MINOR: new principles or materially expanded obligations.
- PATCH: clarifications that do not change obligations.

This baseline is unratified. Record your product's ratification once the team has
reviewed and adopted these principles.
