# Metadata Coverage

Supported categories covered by tests and widget lowering:

- menu
- context menu
- split-button menu
- combo dropdown
- auto-complete suggestions
- date-picker calendar
- color-picker palette
- dialog modal

Evidence:

- `tests/Controls.Tests/Feature144TransientMetadataTests.fs`
- `tests/Controls.Tests/Feature144TransientMetadataFailureTests.fs`
- `tests/Controls.Tests/Feature144ClosedStateCompatibilityTests.fs`
- `src/Controls/catalog.yml`

Disabled triggers suppress open requests and emit `DisabledOverlayTrigger`. Open metadata without current-frame anchor bounds emits `MissingOverlayAnchor`. Closed metadata is compatible with absent anchor evidence.
