# Contract: Readiness Evaluation

## Inputs

Readiness evaluation consumes:

- classified `RuntimeDiagnostic` records
- optional `DiagnosticException` records
- artifact write diagnostics
- optional environment-limitation acceptance metadata from the active lane or
  sample command

It must not parse raw console text.

## Evaluation Rules

Rules are applied in order:

| Order | Condition | Result |
|-------|-----------|--------|
| 1 | Any diagnostic has missing severity or category | `review-required` |
| 2 | Any exception is invalid or unmatched | `review-required` |
| 3 | Any unexcepted `readiness-blocker` diagnostic exists | `blocked` |
| 4 | Any diagnostic explicitly maps to `blocks-readiness` | `blocked` |
| 5 | Any diagnostic explicitly maps to `requires-review` | `review-required` |
| 6 | Only accepted environment limitations prevent full proof | `environment-limited` |
| 7 | Otherwise all diagnostics are classified and non-blocking | `accepted` |

## Non-Blocking Defaults

These diagnostics remain visible but do not block by themselves:

- expected `environment` diagnostics
- expected `backend-cost` diagnostics
- known `rendering-limitation` diagnostics that include clear action guidance
- valid accepted exceptions

## Blocker Behavior

A blocked summary must identify:

- blocker count
- first blocker source
- category and severity
- original message
- action guidance
- artifact path when available

## Review-Required Behavior

A review-required summary must identify:

- unclassified count
- missing fields by diagnostic group
- invalid or unmatched exception ids
- developer-action diagnostics that need review

Review-required status is not accepted readiness. A maintainer must classify the
diagnostic or attach a scoped exception before the summary can become accepted
or environment-limited.

## Environment-Limited Behavior

Environment-limited status is allowed only when:

- no blocker diagnostics remain;
- missing live capability is classified as `environment`;
- the summary names the limitation;
- the limitation is accepted for the lane or sample scope.

Environment-limited status does not pretend live readiness was proven.

## Exception Rules

Exceptions can change a blocker/review diagnostic into accepted-with-exception
only when:

- the exception scope matches source, code, category, fingerprint, lane, or
  sample;
- the exception reason is non-empty;
- the exception is not expired;
- the diagnostic remains visible in the summary and JSON.

Exceptions never hide counts or raw diagnostic groups.
