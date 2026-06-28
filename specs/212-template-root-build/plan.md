# Implementation Plan: Root-buildable generated products (template emits root solution + build wrapper)

**Branch**: `212-template-root-build` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/212-template-root-build/spec.md`

## Summary

Generated fs-gg-ui products have **no root solution**, so stock `dotnet build`/`dotnet test`/`dotnet run`
at the product root fail — blocking FS.GG.SDD's composition-acceptance probes (which call stock,
declared-or-default commands). This plan makes every generated product **root-buildable with the stock
.NET CLI** by emitting, from the template, a root `<Name>.slnx` (referencing the `src/` product project
and the `tests/` project), a `global.json` SDK pin, and a `build.sh`/`build.cmd` verb wrapper
(`restore|build|test|run|verify|pack`) that routes through the existing FAKE script so FAKE stays the
single rich/governed path. The template's release-only instantiation test is extended to scaffold a
product and assert stock `dotnet build` + `dotnet test` at the root succeed and stock `dotnet run` of
the product project executes. This is a **Tier 1 contract change** to `fs-gg-ui-template`; the cross-repo
registry/compatibility records are kept coherent as part of resolution.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The central hypotheses here are *behavioral about the build*, not about runtime rendering: (a) a single
> `.slnx` at the product root makes stock `dotnet build/test/run` resolve, and (b) in headless CI the
> product entrypoint degrades to exit 0 (`UnsupportedEnvironment`) so `dotnet run` does not hang. Both are
> verifiable only by **actually instantiating the template and running the stock commands** — `/speckit-tasks`
> MUST schedule that live instantiate-and-run smoke in the Foundational phase (before building out the verb
> wrapper / release wiring), confirming or replacing (a) and (b) against a really-scaffolded product.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (SDK present locally: 10.0.301). Build orchestration is an
F# script (`build.fsx`) run via `dotnet fsi`; the new wrapper is POSIX shell + Windows `cmd`.

**Primary Dependencies**: .NET SDK / MSBuild (stock `dotnet` CLI), FAKE-style `build.fsx` (already
present), `dotnet new` templating engine (`.template.config/template.json`). No new package dependencies.

**Storage**: N/A (template content + CI workflow files).

**Testing**: The template's release-only `template-product-tests` job in `.github/workflows/release.yml`
(instantiate → stock `dotnet build`/`test`/`run` at the product root). Generated product unit tests
(`tests/Product.Tests`) continue to run via the existing path; they are unchanged.

**Target Platform**: Generated products target `net10.0`; the root build must work cross-platform
(Linux/macOS/Windows) and on machines whose default SDK differs from the product's (hence `global.json`).

**Project Type**: .NET project **template** (the deliverable is template content emitted into a product,
plus a CI assertion). This is not an app/library feature; it is build-wiring of the generated product.

**Performance Goals**: N/A (no runtime hot path). Constraint: the wrapper adds no measurable overhead
beyond the underlying `dotnet`/`fsi` invocation.

**Constraints**:
- `No change to FAKE Verify semantics` (issue acceptance) — `Verify`/`Test` targets in `build.fsx` keep
  their exact current behavior.
- Byte-neutral with respect to `designSystem` (`wcag`/`ant`) and emitted for every `profile` and every
  `lifecycle` (`spec-kit`/`sdd`/`none`) — the root artifacts belong to the ungated product, not the gated
  lifecycle workspace.
- The stock root path and the FAKE path MUST build the same project set (no silent divergence).

**Scale/Scope**: Four files added to `template/base/` (`Product.slnx`, `global.json`, `build.sh`,
`build.cmd`) + new pass-through verb targets in `build.fsx`; `template.json` source/substitution wiring;
one extended CI job; cross-repo registry note. ~1 small template + 1 workflow + 1 registry touch.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Tier declared: **Tier 1 (contracted change)** — modifies the `fs-gg-ui-template` cross-repo contract
(adds the root-buildable surface SDD depends on). Requires spec + plan + test evidence + docs/registry
coherence. No public F# API surface is added.

| Principle | Verdict | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | PASS | Spec authored. The "FSI" path here is the build script itself; the executable evidence is the release-time instantiate-build-test-run assertion. No new public `.fs` module is introduced, so the FSI-first ordering for library code does not apply. |
| II. Visibility lives in `.fsi` | N/A | No new public F# module. `build.fsx` is a script (no `.fsi`); `Program.fs`/product modules are untouched. |
| III. Idiomatic simplicity is the default | PASS | Wrapper is a thin verb→`dotnet fsi build.fsx -t <Target>` shim; new FAKE targets shell to stock `dotnet` against the single `.slnx`. No framework, no new abstraction. |
| IV. Elmish/MVU boundary | N/A | No stateful/I/O feature behavior added. |
| V. Test evidence is mandatory | PASS | Release-only instantiation test asserts stock build/test/run at root; fails on regression (SC-005). |
| VI. Observability & safe failure | PASS | Wrapper reports supported verbs on unknown/missing input; the product entrypoint already degrades to exit 0 on `UnsupportedEnvironment`, making headless `dotnet run` deterministic. |
| Engineering constraints | PASS | `net10.0` pinned via `global.json`; F#/.NET only; **no new package dependency**; pack output unchanged. |

**Result: PASS** (no violations; Complexity Tracking left empty).

## Project Structure

### Documentation (this feature)

```text
specs/212-template-root-build/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (artifact/contract entities)
├── quickstart.md        # Phase 1 output (instantiate + stock build/test/run validation)
├── contracts/
│   └── template-root-build.contract.md   # emitted-artifact + verb-wrapper + release-test contract
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

The deliverable is template content + template wiring + a CI assertion + a cross-repo registry note:

```text
template/base/
├── Product.slnx                 # NEW — root solution; sourceName rewrites Product→<Name>;
│                                #       references src/Product/Product.fsproj + tests/Product.Tests/Product.Tests.fsproj
├── global.json                  # NEW — SDK pin (10.0.x band) for reproducible stock root builds
├── build.sh                     # NEW — POSIX verb wrapper: restore|build|test|run|verify|pack → FAKE
├── build.cmd                    # NEW — Windows verb wrapper (parity with build.sh)
├── build.fsx                    # EDIT — add pass-through targets Restore/Build/Run/Pack over the root
│                                #        .slnx; Test/Verify UNCHANGED (semantics frozen)
├── Directory.Build.props        # (unchanged) already pins net10.0 + lockfile policy — root build inherits
├── Directory.Packages.props     # (unchanged) central versions
├── src/Product/Product.fsproj   # (unchanged) referenced by the slnx
└── tests/Product.Tests/...      # (unchanged) referenced by the slnx

.template.config/template.json   # EDIT — ensure the four new files are copied with sourceName
                                 #        substitution (slnx paths/name) and NOT excluded/copyOnly-frozen

.github/workflows/release.yml    # EDIT — template-product-tests job: after instantiate, assert stock
                                 #        `dotnet build` + `dotnet test` at PRODUCT_DIR root, and
                                 #        `dotnet run --project src/<Name>` exits cleanly

FS-GG/.github (separate repo, via cross-repo-coordination)
└── registry/dependencies.yml + docs/registry/compatibility.md
                                 # contract-change: fs-gg-ui-template gains the root-buildable surface;
                                 # flip/annotate the coherence entry SDD consumes (issue #9 is the tracker)
```

**Structure Decision**: All product-facing change is concentrated in `template/base/` (the ungated
product content root) so the root artifacts ship for every profile/lifecycle/designSystem combination;
`template.json` carries the emit/substitution wiring; `release.yml` carries the regression gate; the
contract delta is recorded cross-repo per ADR-0001/`cross-repo-coordination`. No new top-level
solution/project is added to the framework repo itself.

## Complexity Tracking

> No constitution violations — table intentionally empty.
