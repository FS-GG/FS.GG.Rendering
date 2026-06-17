# Routing Evidence

Feature 144 routes overlay interactions through the existing pure coordinator before lower content:

- pointer routing records topmost hits, outside-dismiss, modal blocking, and pass-through behavior
- focus routing enters initial overlay focus, cycles modal focus, and recovers stale targets
- keyboard evidence confirms normalized Escape, Tab, Shift+Tab, Enter, Space, and arrow keys
- Controls.Elmish maps overlay effects to ordered adapter commands

Evidence:

- `tests/Controls.Tests/Feature144OverlayPointerRoutingTests.fs`
- `tests/Controls.Tests/Feature144OverlayFocusRoutingTests.fs`
- `tests/KeyboardInput.Tests/Feature144OverlayKeyboardEvidenceTests.fs`
- `tests/Elmish.Tests/Feature144OverlayHostRoutingTests.fs`
- `tests/Elmish.Tests/Feature144ProductDispatchTests.fs`
