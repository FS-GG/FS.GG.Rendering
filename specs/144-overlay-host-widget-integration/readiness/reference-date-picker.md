# Reference Date Picker

AntShowcase now carries product-owned reference date-picker state:

- `DatePickerOpen`
- `DatePickerSelected`
- `DatePickerFocused`

The `text-numeric-input` page publishes transient `DatePickerCalendar` metadata from that product state. The reference script in `AntShowcase.Core.Scripts.datePickerReferenceFlow` covers:

- navigate to the input page
- request open
- focus calendar
- select 2026-06-17
- close
- recover focus to the trigger

Evidence:

- `samples/AntShowcase/AntShowcase.Tests/Feature144DatePickerFlowTests.fs`
- `samples/AntShowcase/AntShowcase.Tests/Feature144DatePickerStaleOverlayTests.fs`
- `AntShowcase.Core.Evidence.datePickerReferenceOverlayEvidence`
