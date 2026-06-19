# Contract: Interaction Coverage

## Coverage Source

Required timed actions come from:

```fsharp
SecondAntShowcase.Core.InteractionContracts.all
```

Display-only exclusions come from:

```fsharp
SecondAntShowcase.Core.InteractionContracts.displayOnlyReasons
```

The runner must not maintain a separate hardcoded list of interactive families.

## Required Families

The current representative families are:

- `button-click`
- `toggle-switch`
- `text-entry`
- `numeric-entry`
- `date-time`
- `slider-rating`
- `selection-single`
- `selection-multi`
- `navigation`
- `disclosure`
- `upload`
- `data-collection`
- `form-validation`
- `graph-custom`

If the source contracts change, the runner and summary use the source contracts at runtime and tests update expected counts through the public contract list, not duplicated literals.

## Per-Action Requirements

Each timed representative action records:

- `contractId`
- `pageId`
- `controlFamily`
- `controlIds`
- `actionType`
- `inputKind`
- `expectedVisibleResult`
- measured or non-accepted execution status

The runner must identify the target control on the visible surface. If target lookup fails, the record is non-accepted and names the missing or ambiguous target.

## Display-Only Exclusions

Each display-only exclusion records:

- `controlId`
- `reason`
- `acceptanceStatus = excluded`

Display-only exclusions do not count as timed failures and do not count as accepted interactive families.

## Coverage Summary Rules

`summary.json.coverage` includes:

- `requiredInteractiveFamilies`
- `acceptedInteractiveFamilies`
- `rejectedInteractiveFamilies`
- `blockedInteractiveFamilies`
- `displayOnlyExclusions`
- `missingInteractiveFamilies`

Accepted readiness requires:

- every required family appears in `acceptedInteractiveFamilies`
- no required family appears in `missingInteractiveFamilies`
- display-only exclusions are listed with reasons

When a family is exercised but fails timing or drag continuity, it appears in `rejectedInteractiveFamilies`, not `missingInteractiveFamilies`.

When a family cannot be measured because of environment or target issues, it appears in `blockedInteractiveFamilies`.

## Regression Requirements

The validation package must also preserve:

- clean `InteractionContracts.coverage()`
- slider/rating/value-changing state updates
- navigation and disclosure visible state changes
- existing display-only reasons
- visual readiness and coverage artifacts from the SecondAntShowcase sample
