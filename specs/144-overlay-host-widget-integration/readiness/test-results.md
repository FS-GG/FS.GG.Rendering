# Test Results

Run date: 2026-06-17.

| Command | Result | Count |
|---|---|---:|
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature144|Feature143"` | passed | 42 passed |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature144|Feature143"` | passed | 12 passed |
| `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --filter "Feature144|Feature143"` | passed | 4 passed |
| `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature144|Feature143"` | passed | 4 passed |
| `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature144|Feature143|DatePicker"` | passed | 5 passed |

Total focused tests: 67 passed, 0 failed, 0 skipped.
