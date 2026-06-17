# Feature 142 Fallback Diagnostics

Status: implemented.

- Provider install, clear, and failure states are explicit in `Fonts.TextShapingProviderStatus`.
- Missing glyph and substituted glyph diagnostics are retained from the existing bundled-font fallback chain.
- Newline and unsupported bidi controls are disclosed as single-line/out-of-scope diagnostics.
- Validation: `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build --logger "console;verbosity=minimal"` PASS, 107 passed.
