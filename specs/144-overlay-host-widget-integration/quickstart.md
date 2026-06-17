# Quickstart: Overlay Host Widget Integration

## Prerequisites

- .NET SDK that supports `net10.0`
- Local checkout on branch `144-overlay-host-widget-integration`
- Optional OpenGL/offscreen host support for visual proof scenarios

## Restore And Build

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx
```

Expected outcome: restore and build complete without warnings-as-errors failures.

## Validate Transient Metadata

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature144|Feature143"
```

Expected outcome:

- all eight supported transient categories expose complete metadata
- disabled trigger fixtures remain closed and emit no state-change or selection messages
- closed-state compatibility fixtures report no unexpected visual, hit-test, or diagnostic changes

## Validate Pointer, Keyboard, Focus, And Dispatch

```bash
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature144|Feature143"
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --filter "Feature144|Feature143"
```

Expected outcome:

- topmost pointer hits, outside dismissal, Escape dismissal, modal blocking, and pass-through policy are
  deterministic
- focus enters, cycles, and recovers through documented targets
- product-owned open/close and selection requests are visible to products
- each completed selection or command dispatches exactly once

## Validate Rendering And Replay Evidence

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature144|Feature143"
```

Expected outcome:

- direct, retained, cache-enabled, and cache-disabled runs produce equivalent visible output and hit order
- three repeated scripts produce byte-identical logs
- offscreen visual proof is recorded when the host supports it
- unsupported-host limitations record owner, cause, next proof path, and behavioral evidence rationale

## Validate Reference Date Picker Flow

```bash
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature144|Feature143|DatePicker"
```

Expected outcome:

- date picker starts closed
- trigger activation opens the calendar through a product-visible request
- navigation and selection dispatch exactly one selected date
- selection closes the calendar when policy requires it
- focus recovers to the trigger or field
- final closed render has no stale visible or hit-testable calendar content
- evidence paths are recorded in the feature readiness folder

## Baselines And Compatibility

Run when public surface, baseline, or diagnostic output changes intentionally:

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Expected outcome:

- public surface drift is either zero or documented with migration guidance and versioning rationale
- baseline changes are linked from `specs/144-overlay-host-widget-integration/readiness/`
- scope review confirms no P6/render-anywhere, compositor, intrinsic layout, text, editing, or catalog-redesign work
