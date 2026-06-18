# Regression Evidence Contract

## Purpose

Define the broad validation evidence required to prove final P8 acceptance does not regress prior
rendering, layout, protocol, text, compositor, package, or public-surface guarantees.

## Required Areas

Evidence must classify results for at least:

- retained rendering parity;
- default layout compatibility;
- disabled-cache parity;
- overlay behavior;
- render-anywhere packaging/protocol evidence;
- text-shaping evidence;
- compositor-readiness evidence;
- public surface compatibility;
- package validation;
- full solution build/test.

## Evidence Record

Each record includes:

- area;
- command or test filter;
- expected outcome;
- actual outcome;
- classification: accepted, P8 regression, pre-existing unrelated failure, environment-limited,
  skipped, synthetic-only, or blocker;
- evidence path;
- reviewer-visible diagnostics.

## Classification Rules

- A failing required regression is a blocker unless fixed or explicitly scoped out with
  reviewer-visible rationale.
- An unrelated pre-existing failure must be named and linked before P8 readiness can proceed.
- Environment-limited checks must state the unsupported host or missing capability and the behavior
  that is not claimed.
- Synthetic-only evidence may support failure-path coverage but cannot replace required acceptance
  evidence.
- Public surface changes must match `.fsi`, semantic/package tests, and surface baselines.

## Acceptance Rules

Final P8 accepted status requires every required area to be accepted or explicitly classified as a
bounded non-blocking limitation. Missing or unclassified evidence blocks acceptance.
