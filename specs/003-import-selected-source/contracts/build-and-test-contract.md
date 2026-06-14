# Contract: R4 build-and-test acceptance

The acceptance contract for the import. Unlike R2/R3 (review-based), R4 is verified by
**building and testing real code**.

## Build contract

- A fresh checkout restores and **builds the full solution** (`dotnet build FS.GG.Rendering.sln`).
- Every R2 `import-from-source` module is present and compiles in dependency order.
- Target `net10.0`; SkiaSharp pinned to `4.147.0-preview.3.1`; `Silk.NET.OpenGL` 2.23.0.
- No `Silk.NET.Vulkan` / `GRVkBackend` reference anywhere; the vestigial
  `ViewerBackendPreference.Vulkan` case is removed or returns the unsupported result only.

## Test contract

- The **default local tier** runs (`dotnet test` over the local-frequency projects) and
  passes: per-module unit tests + `Smoke.Tests` (GL).
- CI-frequency checks (`surface-baselines`, `fsdocs` docs build) are present and runnable.
- Release-only checks (`Package.Tests`, template `Product.Tests`) are present but not part of
  the default local run.
- Environment-blocked tests are skipped **with written rationale** (never marked passing,
  never weakened). Disclosure follows constitution Principle V.

## Compliance contract

- Every public module has a `.fsi`; **zero** `.fs` top-level access modifiers.
- A surface-area baseline exists per public module (imported or regenerated, origin recorded).
- No `SkillSupport`/`Governance.Tests`/`SkillSupport.Tests`; no project/feature-graph or
  evidence-gate imports.
- Package identity `FS.Skia.UI.*` preserved (verify actual mapping); `PROVENANCE.md` present.

## Acceptance (maps to spec)

- [ ] `dotnet build` succeeds from a fresh checkout. *(SC-001)*
- [ ] Default local test tier passes. *(SC-002)*
- [ ] No governance machinery; excluded test projects absent. *(SC-003)*
- [ ] `.fsi` + baseline per module; no `.fs` access modifiers. *(SC-004)*
- [ ] GL backend, no Vulkan dependency. *(SC-005)*
- [ ] Provenance complete. *(SC-006)*
- [ ] `FS.Skia.UI.*` / `net10.0` / SkiaSharp pinned. *(SC-007)*
