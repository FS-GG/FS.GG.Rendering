# Feature150 ScrollViewer Validation

## Cases

Focused tests cover:

- overflowing content;
- smaller-than-viewport content;
- empty content;
- fixed viewport compatibility.

The target representative corpus still lists exact-fit, barely overflowing, substantially overflowing, nested scroll, clipped parent, layered parent, text/content natural size, dynamic content change, and invalid intrinsic fallback cases as follow-up expansion for the complete P8 acceptance package.

## Commands

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150ScrollViewerExtent
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150LayoutDiagnostics
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150LayoutCompatibility
```

## Local Verdicts

| Command | Verdict | Notes |
|---|---|---|
| `dotnet build tests/Controls.Tests/Controls.Tests.fsproj --no-restore` | accepted | Feature150 Controls tests compile. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature150` | accepted | 4 passed. |

## Accepted Evidence

- `Control.scrollViewport` reports `ExtentSource = IntrinsicContentExtent` for overflowing content.
- `ContentHeight > Viewport.Height` and `MaxVerticalOffset > 0` for overflowing content.
- Small content normalizes `ContentWidth`/`ContentHeight` to the viewport and reports zero max offsets.
- Empty content reports `EmptyContentExtent` and no diagnostics.

## Limitations

The first slice validates focused ScrollViewer extent readback. It does not yet claim the full 10-case ScrollViewer corpus required for final P8 acceptance.
