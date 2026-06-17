# Closed-State Compatibility

Closed transient controls preserve existing output and authoring expectations.

- closed metadata does not create coordinator open state
- closed metadata validates without current-frame anchor bounds
- reset/dismissal produces product-visible close requests
- runtime state remains free of hidden overlay visibility

Evidence:

- `tests/Controls.Tests/Feature144ClosedStateCompatibilityTests.fs`
- `tests/Elmish.Tests/Feature144ProductOwnedVisibilityTests.fs`
- `tests/Controls.Tests/Feature144CompatibilityContractTests.fs`
