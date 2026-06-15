# Implementation Plan: Bridge the Old Repository (Migration Stage R7)

**Branch**: `007-bridge-old-repository` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-bridge-old-repository/spec.md`

## Summary

Produce the **handoff** from the archived source repository `EHotwagner/FS-Skia-UI` to this
repository, the now-canonical home of the rendering product. R7 is a **documentation-only**
stage: it creates a small set of bridge artifacts in this repo (a bridge hub that declares the
canonical home, names the source commit, states the directional policy, and marks the old repo's
historical artifacts as archive-only), completes the existing `PROVENANCE.md` to bridge-grade
coverage, and writes **copy-ready** content for the changes that belong on repositories this
feature does not own (the archived old repo's README/package redirect; the org `FS-GG/.github`).
Because the old repo is archived (read-only) and outside this working tree, those out-of-repo
changes are delivered as content **plus a recorded action**, never reported as applied
(Constitution Principle VI — no overclaiming). R7 renames nothing: package/template identity
stays `FS.Skia.UI.*`; a migration note records the *retained* mapping and defers any rebrand to
Stage R8 (`docs/product/decisions/0001-package-identity.md`).

No product code, build configuration, package identity, namespace, or template content changes.
The "tests" for this feature are lightweight, mechanical validations (link integrity over in-repo
targets, provenance path-map coverage against the imported tree, and a no-product-change diff
guard), described in `quickstart.md`.

## Technical Context

**Language/Version**: None (documentation-only). The repo's product stack is F# on .NET `net10.0`;
this feature adds no code to it.

**Primary Dependencies**: None new. Validation uses tooling already present in the environment
(shell + `git diff` for the no-change guard; a Markdown link/coverage check expressible as a short
shell/`grep` pass — no new NuGet, no new project).

**Storage**: Durable in-repo Markdown artifacts under `docs/bridge/`, an updated `PROVENANCE.md`,
and a one-line discoverability link added to `README.md`. No runtime storage.

**Testing**: Mechanical validation rather than unit tests — (1) **link integrity**: every in-repo
cross-reference among the bridge artifacts, `PROVENANCE.md`, decision `0001`, and `README.md`
resolves to an existing target; (2) **provenance coverage**: every imported top-level area
(`src/`, `tests/`, `template/`, `.template.config/`, `.template.package/`, imported `docs/`, build
metadata) appears in the `PROVENANCE.md` path map or is a named, recorded gap; (3) **no-product-change
guard**: `git diff` touches only Markdown (`docs/bridge/**`, `PROVENANCE.md`, `README.md`,
spec artifacts) — zero `src/`, `tests/*.fs(i)`, `*.props`, `*.slnx`, or `template/**` edits.

**Target Platform**: N/A (docs). Validations run headless anywhere a shell + git are available
(the default local tier; no DISPLAY/GL needed).

**Project Type**: Migration handoff — documentation and provenance over an existing product. Not
product API.

**Performance Goals**: N/A. Validation completes in seconds and adds nothing to the build/test inner
loop.

**Constraints**: Constitution v1.0.0. **Principle VI (central):** any change destined for a
repository this feature does not own (archived old repo, org `.github`) is copy-ready content + a
recorded action, never claimed as applied; the redirect must supersede the old repo's stale
self-description rather than silently coexist. **Principle V:** the no-product-change guard and the
coverage/link checks are the evidence; they must actually fail when violated (a coverage check that
passes with a missing path proves nothing). Identity stays `FS.Skia.UI.*` (Engineering Constraints);
R7 must not begin a rebrand (that is R8).

**Scale/Scope**: ~3 new bridge docs under `docs/bridge/` (hub incl. directional-policy + archive-note
sections; old-repo redirect; package-identity migration note), 1 updated `PROVENANCE.md`, 1
`README.md` link. 3 design contracts (bridge-hub schema, redirect-notice schema, provenance-coverage
rule). **0 new projects, 0 new NuGet, 0 product code, 0 identity changes.**

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This feature is **documentation/handoff** over existing product code — it adds no F# surface. The
code-centric principles are N/A for new surface; the honesty/observability principle binds fully and
is the heart of the work.

| Principle | Assessment |
|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | No code, no public surface. The artifact chain is spec → plan → validated docs. **PASS (N/A for new surface)** |
| II. Visibility in `.fsi` | No F# module added ⇒ no `.fsi`/baseline obligation. **PASS (N/A)** |
| III. Idiomatic Simplicity | Plain Markdown; reuses the existing `docs/` layout and `PROVENANCE.md` rather than inventing a new doc system; validations are short shell/grep, no new tooling. **PASS** |
| IV. Elmish/MVU boundary | No stateful/I-O workflow. **PASS (N/A)** |
| V. Test Evidence Mandatory | The link-integrity, provenance-coverage, and no-product-change checks must demonstrably fail when violated — a coverage check that greens with a missing path is itself a defect. No assertion weakened. **PASS** |
| VI. Observability & Safe Failure | **Central.** Out-of-repo changes are copy-ready + recorded action, never claimed applied; the redirect supersedes the stale old-repo framing; identity-confusion and dead links are surfaced, not swallowed. **PASS** |
| Engineering Constraints | `net10.0` product untouched; no new NuGet; package identity stays `FS.Skia.UI.*` (rebrand deferred to R8); no product behavior change. **PASS** |

**Change Classification**: **Tier 2 (internal).** Adds documentation and provenance; no public-API
change, no new dependency, no observable product-behavior change. (Per Constitution: a Tier 2 change
requires spec + evidence; `.fsi`/baselines remain untouched — there are none to touch here.)

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/007-bridge-old-repository/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: handoff methodology decisions
├── data-model.md        # Phase 1: the bridge document entities + fields/relationships
├── quickstart.md        # Phase 1: how to validate the bridge (link/coverage/no-change checks)
├── contracts/
│   ├── bridge-hub.md            # schema: required sections of the bridge hub document
│   ├── old-repo-redirect.md     # schema: copy-ready redirect notice + recorded-action header
│   └── provenance-coverage.md   # rule: what "bridge-grade, complete" provenance means
└── checklists/requirements.md
```

### Source Code (repository root)

```text
docs/bridge/
├── README.md                       # Bridge hub (FR-001/002/007/008/012): canonical home, source
│                                   #   commit, what moved, *Directional policy* section, *Archive
│                                   #   note* section; links provenance + redirect + migration note
├── old-repo-redirect.md            # FR-004/011: copy-ready old-repo README banner + package-page
│                                   #   deprecation text, under a "recorded action — not yet applied"
│                                   #   header acknowledging the archived/read-only old repo
└── package-identity-migration.md   # FR-005/006: retained FS.Skia.UI.* identity mapping; rebrand
                                     #   deferred to R8 → links docs/product/decisions/0001-package-identity.md

PROVENANCE.md                       # FR-003: completed to bridge-grade (every imported top-level
                                    #   area in the path map; adaptations/exclusions with rationale)
README.md                           # FR-012: one-hop discoverability link to docs/bridge/README.md
```

**Structure Decision**: Bridge artifacts live under a new `docs/bridge/` folder, parallel to the
existing `docs/{product,validation,ci,audit,imported,harness}/` families — the repo already groups
docs by concern, and "bridge/handoff" is a new concern, so a sibling folder fits the established
layout (Principle III) without disturbing any existing doc. The **directional policy** (FR-008) and
the **archive note** (FR-007) are realized as named sections inside the bridge hub (`docs/bridge/README.md`)
rather than separate files: they are short, belong to the same hub a visitor reads once, and keeping
them as sections avoids a proliferation of one-paragraph files while still mapping cleanly to their
FRs and success criteria. `PROVENANCE.md` stays the single lineage record (the bridge *references*
it, FR-002) and is completed in place rather than duplicated. The old-repo redirect and the org
`.github` updates are **content this repo cannot apply** (the targets are archived/owned elsewhere),
so they are authored here as copy-ready blocks with an explicit recorded-action header (FR-011,
Principle VI).

## Complexity Tracking

No constitution violations — section intentionally empty.
