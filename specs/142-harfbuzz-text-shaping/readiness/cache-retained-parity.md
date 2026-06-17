# Feature 142 Cache And Retained Parity

Status: implemented for retained text measurement cache invalidation.

- `TextMeasureKey` includes `Scene.textMeasurementVersionBucket()`.
- Switching between pure fallback, bundled fallback, and HarfBuzz shaping changes the bucket and prevents stale text metric reuse.
- Cache-enabled and cache-disabled paths continue to measure through `Scene.measureTextResolved`.
- Validation:
  - `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 157 passed, 17 skipped.
  - `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 797 passed, 1 skipped.
