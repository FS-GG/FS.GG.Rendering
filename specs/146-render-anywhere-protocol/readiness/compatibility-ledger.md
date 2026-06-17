# Feature 146 Compatibility Ledger

## Public Surface Changes

- `FS.GG.UI.Scene` adds `SceneCodec.fsi/fs`:
  - deterministic `SceneCodec.exportScene` / `export`
  - `SceneCodec.importPackage`
  - `SceneCodec.inspect` / `inspectWith`
  - `SceneCodec.compareScenes`
  - package identity, resource manifest, capability requirement, package diagnostic, and inspection
    report records.
- `FS.GG.UI.SkiaViewer` adds `ReferenceRendering.fsi/fs`:
  - MVU-style `ReferenceRenderingModel`, `ReferenceRenderingMsg`, and `ReferenceRenderingEffect`
  - `ReferenceRendering.init`, `update`, `renderPackage`, `writeEvidenceSummary`, and `run`
  - passed, failed, and environment-limited evidence records.
- `FS.GG.UI.Testing` adds `PackageInspectionAssertions.validate` for package inspection reports.
- `tests/Rendering.Harness` adds `RenderAnywhere.fsi/fs` with the Feature 146 corpus, reference
  evidence command, and browser feasibility fallback report.

## Migration Guidance

- Existing `Scene` constructors and `SceneNode` cases are unchanged.
- `Scene.Image` local source strings are not encoded as portable resource identities. During export,
  image nodes are rewritten to package-local `ResourceId` values and the original source is retained
  only as diagnostic `SourceLabel`.
- Consumers should call `SceneCodec.inspectWith` before rendering packages and reject or degrade
  based on `PackageInspectionReport.Status`.
- Reference rendering consumers should treat `ReferenceEnvironmentLimited` as no accepted image
  evidence, not as a visual pass.

## Intentional Limitations

- The first browser feasibility implementation records a documented CanvasKit command-stream
  fallback decision; it does not claim a production browser backend.
- Auto-discovered image resources use package-local ids and unresolved diagnostic content hashes
  unless callers supply explicit resource availability.
- Unknown optional TLV tags are skipped and reported as warnings; unknown required tags reject.
- Reference rendering uses the existing Skia raster/offscreen path and reports environment-limited
  evidence if native Skia cannot produce a PNG.

## Evidence Links

- Round-trip corpus: `roundtrip/corpus.md`
- Reference rendering: `reference/README.md`
- Browser feasibility: `browser/README.md` (`browser-feasibility.md` when generated)
- Validation summary: `validation-summary.md`
