# T003 — Pre-adoption lockfile inventory & scope reference (Feature 213)

Date: 2026-06-28

## Committed `packages.lock.json` count (pre-adoption)

`find . -name packages.lock.json -not -path '*/bin/*' -not -path '*/obj/*' | wc -l` → **38**

The plan estimated ~39; the **actual discovered count is 38** (capture-not-assert, per T003). This
equals the 38 `FS.GG.Rendering.slnx` LOCKED members (18 src + 17 tests + 2 samples + 1 tools), each
of which owns a committed lockfile (`RestoreLockTests` VR-1). The two committed sample lockfiles
outside-but-also-counted are `samples/CanvasDemo/packages.lock.json` and
`samples/SymbologyBoard/packages.lock.json` (these two samples ARE slnx members). The excluded lanes
(`tests/Package.Tests` + the 4 shadowing samples AntShowcase/SampleApps/SecondAntShowcase/ControlsGallery)
carry no lockfile (VR-2). All 38 are regenerated under transitive pinning (T011).

## `template/base/` unchanged-scope reference (INV-7 / C-8)

`git rev-parse HEAD:template/base` → `46b33a325b0a2b00b3bcf38fac0cc88b30303be7`
(pre-adoption tree of `template/base/`; T022 confirms it is byte-unchanged at the end.)

Repo HEAD at start: `b6ac246df4af8bc21a351f22af604f7a20f86e9f`
