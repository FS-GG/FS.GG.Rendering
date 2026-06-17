# Build Evidence

Run date: 2026-06-17.

`dotnet restore FS.GG.Rendering.slnx`

- result: passed
- restored solution projects successfully

`dotnet build FS.GG.Rendering.slnx`

- result: passed
- warnings: 0
- errors: 0

AntShowcase is outside `FS.GG.Rendering.slnx`; it was built as part of its focused test command.
