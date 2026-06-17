# Quickstart Validation

Commands run on 2026-06-17 in `/home/developer/projects/FS.GG.Rendering`:

- `dotnet restore FS.GG.Rendering.slnx` - passed
- `dotnet build FS.GG.Rendering.slnx` - passed with 0 warnings and 0 errors
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature144|Feature143"` - passed
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature144|Feature143"` - passed
- `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --filter "Feature144|Feature143"` - passed
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature144|Feature143"` - passed
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature144|Feature143|DatePicker"` - passed
- `dotnet fsi scripts/refresh-surface-baselines.fsx` - passed

Environment note: real offscreen visual proof is not claimed by this run; the unsupported-host limitation path is tested.
