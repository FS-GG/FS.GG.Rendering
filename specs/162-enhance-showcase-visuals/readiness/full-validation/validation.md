# Full Validation

- Restore/build command: `dotnet restore FS.GG.Rendering.slnx && dotnet build FS.GG.Rendering.slnx -c Release --no-restore`
- Restore/build status: passed, 0 warnings, 0 errors
- Restore/build duration: 3.42s for the final build step after restore

- Full test command attempted: `dotnet test FS.GG.Rendering.slnx -c Release --no-restore --no-build`
- Full test status: canceled due to a stuck `Controls.Tests` child process
- Full test duration: canceled after several minutes with no additional output from `Controls.Tests`
- Full test output before cancellation: 809 passed, 20 skipped, 0 failed across the projects that reported before cancellation. `Controls.Tests` started but did not complete.

- Feature-specific substitute gate: `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore`
- Feature-specific status: passed, 78 passed, 0 failed, 0 skipped
