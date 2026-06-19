# AntShowcase Structured Evidence Adoption

Status: `accepted`

## Selected Shell

- ✅ Page: `charts-statistical`
- ✅ Themes: `antLight`, `antDark`
- ✅ Size bucket: `preferred`
- ✅ Preferred screenshot target count: `38`
- ✅ Minimum screenshot target count: `12`

## Evidence

- ✅ `samples/AntShowcase/AntShowcase.Core/Evidence.fsi` exposes `RetainedInspectionEvidenceRecord` and retained inspection serialization helpers.
- ✅ `samples/AntShowcase/AntShowcase.Core/Evidence.fs` emits deterministic JSON and Markdown retained inspection evidence.
- ✅ `samples/AntShowcase/AntShowcase.Tests/VisualShellTests.fs` now asserts structured retained evidence while preserving shell render checks.
- ✅ `dotnet run --project samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore -- --filter-test-list Feature170 --summary`: passed, 1 test.
- ✅ `dotnet run --project samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore -- --filter-test-list VisualReadiness --summary`: passed, 5 tests.

The structured adoption evidence records affected visual regions (`content` and `layout`) and keeps screenshot readiness authoritative for the existing 38 preferred and 12 minimum target matrices.
