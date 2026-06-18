# Feature 159 Validation Plan

Status: `executed`

## Scope

- Generate same-profile promotion evidence with `compositor-promotion --feature 159 --policy layer-promotion-v1`.
- Assemble the readiness package with `compositor-readiness --feature 159`.
- Capture unsupported-host behavior with display variables removed.
- Refresh package surface baselines after `.fsi` changes.
- Run focused Feature 159 tests and Feature 155/157/158 preservation tests.
- Run the full solution test suite and whitespace check before closeout.

## Acceptance Rules

- Accepted Feature 159 evidence must be same-profile, parity-passing, policy-matched, and net-positive.
- Unsupported, ambiguous, parity-failing, stale, cross-profile, missing-policy, or resource-limited evidence contributes zero accepted reuse and promotion artifacts.
- The shipped compositor performance claim remains `performance-not-accepted`.

## Synthetic Fixture Audit

- Pure policy, counter, identity, and replay unit tests use synthetic fixtures only where the test name includes `Synthetic`.
- Each synthetic fixture block in the audited test files is marked with a `// SYNTHETIC:` comment and rationale.

