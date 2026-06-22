# Semantic Equivalence — Harness Data-Table Refactor (185)

Behavior-preservation evidence (FR-008, SC-005). Every story re-emitted the full 12-feature corpus
and semantic-diffed it against the pre-refactor live baseline `/tmp/185-baseline` (160 files); the
CI-grepped path/header literals stayed byte-identical.

## Method

`scripts/emit-harness-readiness.sh <dir>` re-emits all 12 features' readiness; `scripts/semantic-diff-artifacts.fsx /tmp/185-baseline <dir>`
normalizes embedded timestamps/run-ids (content + timestamp-bearing filenames) and the absolute
`--out` root, then compares the normalized path-set and per-file content. `problems=0` ⇒ semantically
equivalent. The harness was validated against itself (two independent baseline re-emits diff clean).

## Per-story results (all clean)

| Checkpoint | semantic-diff vs `/tmp/185-baseline` | Rendering.Harness.Tests |
|---|---|---|
| US1 (SSOT) | 160 files, **problems=0** | 209/209 |
| US2 (Compositor split) | 160 files, **problems=0** | 209/209 |
| US3 (CLI collapse) | 160 files, **problems=0** | 209/209 |

## Byte-identity of frozen literals (SC-005, FR-008)

The non-timestamp report files are byte-identical to baseline, e.g.:

```
diff -q /tmp/185-baseline/156/package-validation.md /tmp/185-us3v/156/package-validation.md  → identical
diff -q /tmp/185-baseline/158/package-validation.md /tmp/185-us3v/158/package-validation.md  → identical
diff -q /tmp/185-baseline/160/package-validation.md /tmp/185-us3v/160/package-validation.md  → identical
```

The `# Feature <N> …` report titles and `specs/<slug>/readiness/…` directory strings (the
`frozen-literals.md` set) are reproduced byte-for-byte by the descriptor helpers and the relocated
renderer bodies.

## One intended observable change (US3, FR-011)

`compositor-readiness --feature 999` (unknown feature) previously fell through to the legacy default
and returned **exit 0** (silent). It now routes through the fail-loud `FeatureCatalog.descriptorByAlias`
→ `CatalogError` → error message + **exit 2**. This is the spec-required fail-loud behavior
(FR-011, US3-AS2, edge "Feature not in the catalog"), not a regression. All **12 catalog features**
remain byte/semantically identical (the diffs above + `problems=0`).
