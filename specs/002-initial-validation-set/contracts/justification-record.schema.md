# Contract: Justification Record (`docs/validation/justification-records.md`)

The per-candidate test/check justification — the constitution's "every active check carries
a justification" rule made concrete.

## Required structure

A table with these columns, in order:

| Candidate | Product contract | Failure mode | Owner | Frequency | Cost | Decision | Note |
|---|---|---|---|---|---|---|---|

## Field rules

- **Candidate**: unique; a source test project or named check.
- **Product contract / Failure mode / Owner / Cost**: required, non-empty.
- **Frequency**: one of `local`, `ci`, `release-only`, `manual-advisory`.
- **Decision**: one of `import-now`, `defer`, `archive`, `rewrite-smaller`.
- **Note**: required when Decision is `rewrite-smaller` (describe the smaller form) or when
  the candidate is an open question (list options); otherwise may be `—`.

## Coverage requirement (FR-007)

The records MUST include at least one candidate in each class: focused runtime unit tests,
public API surface-drift, package/consumer checks, template pack/install/instantiate, docs
build, broad historical readiness reports, generated fixtures.

## Acceptance (maps to spec)

- [ ] Every reviewed candidate has all six core fields + a Decision. *(SC-001)*
- [ ] Every Decision ∈ {import-now, defer, archive, rewrite-smaller}. *(FR-002)*
- [ ] All seven candidate classes appear. *(FR-007)*
- [ ] `rewrite-smaller` rows describe the smaller form. *(edge case)*

## Example rows

```text
| Color.Tests | Color contrast/palette correctness | wrong contrast ratios / palette regressions | rendering maintainer | local | fast, pure, no GPU | import-now | — |
| surface-baselines | Public .fsi surface stability | unintended public API drift | rendering maintainer | ci | low; regenerate on intended change | import-now | — |
| Parity.Tests | Gallery visual parity | unnoticed visual regressions | rendering maintainer | manual-advisory | heavy; image fixtures; flake risk | rewrite-smaller | fold into Stage R5 harness T1 offscreen checks |
| Governance.Tests | (governance behavior) | — | — | — | — | archive | governance machinery removed by constitution; not a product concern |
```
