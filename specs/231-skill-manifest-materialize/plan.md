# Implementation Plan: product skill-manifest + single standalone materialize; drop dev-surface vendoring (ADR-0014 P2)

**Branch**: `231-skill-manifest-materialize` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/231-skill-manifest-materialize/spec.md`

## Summary

Implements roadmap P2 (issue #43) for the `fs-gg-ui` template. Four moves, one mechanism:
(1) **R2.1** — the template stops vendoring the repo's `.agents/skills/` dev surface (the
`include: ["speckit-*/**"]` narrowing kills the ~13-17 dangling wrappers) and publishes a
content-addressed **product skill-manifest** (`skill-manifest` schema v1, full 12-entry
catalog, emitted to `.agents/skills/skill-manifest.json` in every lifecycle). (2) **R2.2** —
Feature 230's 24 per-skill `.claude`/`.codex` twins + 2 blanket copies are replaced by **one
materialize step**: a spec-kit-emitted, vendored copy of `Fsgg.SkillMirror`
(`skill-mirror-vendored.fs` + `materialize-skill-roots.fsx` driver) invoked by an MSBuild
target in the product's `Directory.Build.props` on first build (post-action ruled out by
Feature 205's side-effect-free property); a Package.Tests **content-parity gate** pins
`FS.GG.Contracts 1.4.0` and asserts behavioral equality. (3) **R2.3** — skill emission goes
`copyOnly` with name-neutralized canonical bodies (this is also what makes the manifest
digests stable). (4) **R2.4** — a **no-dangling-route guard** joins the release gates.
`sdd`/`none` placement is unchanged. Template `0.1.60-preview.1 → 0.1.61-preview.1`;
coherent-set release after merge.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The F3/F5 defect mechanics were verified statically (wrapper bodies, substitution rows)
> but the emission behavior claims (what `dotnet new` actually materializes before/after)
> MUST be confirmed by an early live scaffold in the Foundational phase before gate rework
> is built on them.

## Technical Context

**Language/Version**: F# / .NET 10 (Expecto gates, `.fsx` validator/driver); JSON
(`.template.config/template.json`, manifest).
**Primary Dependencies**: `FS.GG.Contracts` 1.4.0 (**new, test-only**, nuget.org; parity
authority); dotnet templating engine; `dotnet fsi` (SDK-bundled) for the product-side driver.
**Storage**: files (template content, manifest, scaffold trees). **Testing**: Expecto
(`tests/Package.Tests`, the release gate) + env-gated live loop
(`scripts/validate-lifecycle-template.fsx`, `FS_GG_RUN_LIFECYCLE_VALIDATION=1`).
**Target Platform**: template consumers on Linux/macOS/Windows (driver is BCL-only fsx).
**Project Type**: template/package content + gates; **no `src/**` change**.
**Performance Goals**: materialize adds ≤ ~3s to a product's *first* build (fsi startup),
~0 thereafter (stamp incrementality). **Constraints**: default generation stays
side-effect-free (Feature 205); `sdd`/`none` byte-placement unchanged (Templates#47 closed
chain); staged rollout — in-product verify advisory, repo gates enforcing (roadmap P4 flips).
**Scale/Scope**: template.json 38→14 skill-surface rows; 2 new template content files; 1
manifest; ~5 gate files reworked; 1 new gate file; 12-skill catalog.

## Constitution Check — PASS

- **Tier 2** (template content/config + test logic; no `src/**`, no public F# module, no
  `.fsi`/baseline impact). Observable scaffold emission changes implement accepted ADR-0014
  (extending ADR-0011) — the constitution's "template pack/install/instantiate checks" are
  exactly the gates being reworked, each with a stated protected contract (C1–C4).
- **I/II**: N/A for `src/` (none touched). The vendored `.fs` is template *content*, not a
  public repo module — no `.fsi` required; its public surface is contract-checked by the
  parity gate instead (disclosed here per Principle II's spirit).
- **III**: honored — plain modules/records; the vendored file deliberately transliterates the
  upstream library rather than "improving" it (parity beats elegance; justified complexity: none).
- **IV**: N/A — the driver is a linear script (enumerate→copy→verify→report), not a stateful
  workflow; no Elmish boundary warranted.
- **V**: honored — every behavior change lands with fail-before/pass-after gates; live-scaffold
  evidence recorded in the 204 readiness report (`provenance: live`); no synthetic evidence
  planned (real `dotnet new` + real fsi runs).
- **VI**: driver prints structured per-skill drift lines; `--enforce` fails loudly; silent
  drift is the defect class this feature exists to kill.
- **New dependency** (constitution: minimized): `FS.GG.Contracts` 1.4.0, test-only, exact
  central pin, justification in research.md; maintenance owner = the parity gate.
- **Local Skills advisory**: product skill *placement/emission* changes; no repo skill
  added/removed.

## Project Structure

### Documentation (this feature)

```text
specs/231-skill-manifest-materialize/
├── spec.md, plan.md, research.md, data-model.md, quickstart.md
├── contracts/skill-manifest-and-materialize.md
├── checklists/requirements.md
└── tasks.md            # /speckit-tasks output
```

### Source touch-points (verified against current tree)

```text
.template.config/template.json                 # R5 source-row delta (data-model.md table)
template/skill-manifest/skill-manifest.json    # NEW — canonical catalog (R3)
template/lifecycle/skill-mirror-vendored.fs    # NEW — vendored algorithm (R2)
template/lifecycle/materialize-skill-roots.fsx # NEW — IO driver (C3)
template/base/Directory.Build.props            # + FsGgMaterializeSkillRoots target (R1)
template/product-skills/{fs-gg-testing,fs-gg-layout}/SKILL.md   # name-neutralize (R4)
template/base/.agents/skills/fs-gg-project/SKILL.md             # audit for tokens/danglers
scripts/generate-skill-manifest.fsx            # NEW — manifest (re)generator (R3)
scripts/validate-lifecycle-template.fsx        # classifier + live-loop rework (R8)
tests/Package.Tests/Feature231SkillManifestTests.fs   # NEW gate file (R2/R3/R7 + target shape)
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs  # re-derive emission table
tests/Package.Tests/Feature204LifecycleTemplateTests.fs    # gated-source audit rework
tests/Package.Tests/{Feature217,Feature224,Feature225}*.fs # audit; re-derive if they encode 230 shape
tests/Package.Tests/Package.Tests.fsproj       # + vendored Compile include, + FS.GG.Contracts ref
Directory.Packages.props                       # + FS.GG.Contracts 1.4.0 central pin
.template.package/FS.GG.UI.Template.fsproj     # 0.1.60-preview.1 -> 0.1.61-preview.1
docs/scaffold-map.md / template/base/docs/*    # document the materialize step where the map lives
specs/204-.../readiness/lifecycle-template-validation.md   # regenerated (provenance: live)
```

**Structure Decision**: all work stays in the existing template/gate layout; the two new
template content files live under `template/lifecycle/` (new dir, mirrors the existing
`template/{product-skills,fragments,feedback}` pattern).

## Phase 0 → research.md (10 decisions, all NEEDS CLARIFICATION resolved)

Key: R1 build-target (not post-action — Feature 205); R2 vendored F# + behavioral parity
(package is dll-only); R3 full-catalog manifest at `.agents/skills/skill-manifest.json`,
freshness gate; R4 `copyOnly` + name-neutral bodies (digest stability ⊃ F5 fix); R5 emission
boundary table; R6 advisory-in-product / enforcing-in-gates; R7 dangling-route extraction
rules; R8 gate rework map; R9 version/release.

## Phase 1 → data-model.md, contracts/, quickstart.md

Complete. Agent context updated (CLAUDE.md plan pointer → this file).

## Complexity Tracking

*No constitution violations.* The one deliberate duplication — a vendored copy of a published
library — is the ADR-0014-prescribed standalone-lane mechanism, and the parity gate is the
mandated control making it safe (roadmap §6).
