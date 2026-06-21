# Contract: US4 — Remove the untyped flat-chart fallback (Tier 2, FR-004)

## Precondition (descope gate — re-verify in Foundational)
Removal proceeds **only if** no in-tree consumer (src, samples, template) authors flat
`float list`/`float array` chart data. Research D4 found **zero**; `/speckit-tasks` re-runs the scan in
Foundational. If any author is found → **drop US4**, record the finding (FR-004 / Acceptance 4.3), break
nothing.

## Edits (precondition met)
1. `src/Controls/Control.fs:482-483` — delete the two fallback arms:
   ```fsharp
   | UntypedValue(:? (float list) as values) -> Some(indexed values)
   | UntypedValue(:? (float array) as values) -> Some(indexed (Array.toList values))
   ```
   Keep the typed arms `479-481` (`ChartSeries list` / `ChartPoint list`) and update the doc-comment
   `469-471` (drop the flat-list mention).
2. Delete the one fallback test `tests/Controls.Tests/Feature080ExtractionTests.fs:62-71`
   ("flat float-list fallback still extracts (legacy authoring)").

## Invariants
- **Byte-stable (I1):** `chartValues` output for every typed-front-door chart in the corpus equals
  baseline (typed arms untouched).
- **Surface (I2):** none (internal branch) — no bump, no ledger entry (Tier 2 — research D1).
- **Tests (I3):** fallback test deleted (not weakened); typed extraction coverage retained.

## Acceptance (spec US4)
1. Typed-front-door charts read byte-identically.
2. Scan shows zero flat-list authors → removal justified.
3. (If any author found) US4 descoped, finding recorded, consumer unbroken.
