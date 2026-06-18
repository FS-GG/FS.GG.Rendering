# Compatibility Ledger

## Default

Feature 162 changed AntShowcase sample code, AntShowcase tests, and Feature 162 readiness artifacts only.

- No `src/**` package `.fsi` file changed for this feature.
- No FS.GG.UI public package API was added, removed, or renamed.
- `samples/AntShowcase/AntShowcase.Core/Evidence.fsi` is a sample-project signature file, not a packable FS.GG.UI package surface.
- AntShowcase package references were moved from `0.1.0-preview.1` to the current locally packed `0.1.23-preview.1` package set so the sample validates the current package feed.
- `samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj` adds a direct `SkiaSharp` dependency for contact-sheet composition inside the sample app edge.

## Escalation

No lower-level package change was required. If a lower-level package change becomes necessary later, record:

- changed `.fsi` files
- semantic tests
- surface-baseline updates
- owning limitation or follow-up
- package version and local feed validation
