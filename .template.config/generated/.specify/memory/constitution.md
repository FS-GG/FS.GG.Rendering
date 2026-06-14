# Generated Product Constitution

## Core Principles

### I. Spec-First Delivery

Feature work starts with a specification under `specs/`, followed by an
implementation plan, task breakdown, and readiness evidence. Implementation
work should stay traceable to those artifacts.

### II. Public Contracts Stay Explicit

Public behavior should be captured in clear source contracts, tests, and
documentation before it is treated as stable.

### III. Keep Product Code Simple

Prefer direct F# modules, small functions, explicit data, and ordinary .NET
tooling. Add abstractions only when they remove real duplication or make a
workflow easier to test.

### IV. State And Effects Are Boundaries

Stateful or I/O-bearing workflows should keep pure decision logic separate from
edge interpreters such as file, process, network, UI, or renderer code.

### V. Evidence Must Be Honest

Readiness evidence must distinguish real execution from synthetic, mocked, or
placeholder proof. Synthetic-only evidence is acceptable only when it is named
and justified.

### VI. Tests Are Required For Change

Meaningful changes should include focused tests or executable checks that prove
the intended behavior and protect the relevant contract.

### VII. Failures Must Be Actionable

Validation failures should name the missing file, broken contract, failed
command, or unsupported environment condition clearly enough for the next
engineer or agent to act.

## Generated Product Constraints

- Use the generated `build.fsx` targets as the local command surface.
- Keep FS.Skia.UI framework implementation source out of this product unless a
  feature plan explicitly opts into framework development.
- Product features may consume selected FS.Skia.UI packages and local skills,
  but product readiness evidence belongs under this repository's own `specs/`
  tree.
- Local skills under `.agents/skills/*/SKILL.md` and generated product
  capability skill paths are product governance artifacts. Agents MUST use the
  applicable local skill whenever a task matches the skill's description, and
  MUST prefer local product or capability skills over generic guidance when
  both apply.
- When a live renderer or host environment is unavailable, record the
  environment limitation separately from product implementation failures.
