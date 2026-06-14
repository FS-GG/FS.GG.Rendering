---
title: Rendering project
category: FS.GG
categoryindex: 6
index: 3
description: Scope and operating model for the rendering/runtime repository in the FS.GG split.
---

# Rendering project

The rendering project owns the UI framework as a product. It should be possible
to build, test, package, document, and release it with normal repository tools
and standard Spec Kit, without depending on an experimental governance platform.

## Scope

The rendering project owns:

- scene and drawing primitives;
- layout;
- input and keyboard abstractions;
- Skia viewer and host behavior;
- Elmish integration;
- controls and typed control front doors;
- design-system primitives, theme models, and concrete themes;
- optional design-specific kits where a design system introduces product
  patterns beyond styling;
- testing helpers that are part of the product contract;
- runtime packages and package metadata;
- product documentation;
- templates and generated-product smoke tests, unless template cadence later
  justifies a separate repository.

## Design and controls

Controls and design live in the rendering project, but not in the same layer.
The product should have one semantic control set and multiple theme or design
system layers over it.

Recommended internal split:

| Layer | Owns |
|---|---|
| Rendering core | Scene, layout, input, drawing, and host-independent primitives. |
| Controls | Semantic controls, behavior, state, focus, keyboard, pointer, value, and accessibility contracts. |
| Design system | Token model, theme model, density, typography, radii, color roles, visual states, and component token slots. |
| Themes | Concrete Ant Design, Fluent, Material, or product-specific theme values. |
| Kits/patterns | Design-specific compositions when behavior or layout conventions exceed styling. |

Specific design languages should not get duplicated controls by default. A
`Button` remains `Button`; Ant Design, Fluent, and Material normally provide
different themes for the same control. A design-specific module is justified
only when it introduces a real pattern, such as Ant Design-style form layout,
table filtering conventions, result pages, descriptions, statistics, or other
opinionated compositions.

See [Design and controls](design-and-controls.md) for the boundary rules.

## Workflow

Use standard Spec Kit for feature specification, planning, and task breakdown.
Keep the workflow boring and recognizable. The rendering repository may retain
repo-owned checks that are already valuable, but those checks should stay
narrow:

- API surface drift checks;
- package skew checks;
- template pack/install/instantiate checks;
- docs build checks;
- selected visual or scenario smoke checks;
- release packaging checks.

The initial repository version should be intentionally light. Tests and
governance mechanisms are important, but they must be moved over because they
protect a current product contract or a known failure mode, not because they
exist in the old repository. Every imported check should have a short
justification: what it protects, when it runs, who owns it, and what maintenance
cost it adds.

Do not introduce a custom feature graph as the source of truth for ordinary
rendering work. `spec.md`, `plan.md`, and `tasks.md` may remain authored Spec
Kit artifacts unless a future lightweight tool proves a better path without
raising the cost of contribution.

The repository should be created as a fresh standard Spec Kit repository before
product code is imported. This keeps import decisions explicit and prevents old
workflow state from becoming the new product workflow by accident.

## Governance boundary

The rendering project can consume optional governance tools, but it must not be
blocked by them. A useful test is:

> Could a contributor clone the rendering repository, read the standard feature
> artifacts, run the documented build/test commands, and ship a routine
> rendering change without understanding the governance repository?

If the answer is no, the boundary has failed.

## Release posture

The rendering project should own its package and docs release policy directly.
Governance tooling can help check that policy, but release identity belongs to
the rendering product:

- package IDs;
- package versions;
- template identity;
- docs URL;
- supported target frameworks;
- release notes;
- migration guidance.

The release path should be conservative and explicit, not generated from an
unproven governance schema.

## What not to carry forward

Do not carry the following as active runtime-repository requirements:

- a mandatory custom `ProjectGraph`;
- a mandatory custom `ProductGraph`;
- a custom `FeatureGraph` replacing standard Spec Kit artifacts;
- graph-bound task completion as the only accepted source of task status;
- generated `spec.md`, `plan.md`, or `tasks.md` as the initial workflow;
- governance workspaces and FAKE concurrency policy as a prerequisite for
  normal product changes;
- historical tests, evidence reports, generated fixtures, or governance checks
  that do not have a current owner and product-risk justification.

These ideas can be revisited later if the separate governance project proves a
small, stable implementation.
