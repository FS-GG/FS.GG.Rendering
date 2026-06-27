# Contract: Cross-Repo Record (US3 — FR-008)

Defines the cross-repo registry update that records the BOM as part of the coherent set, and the
gate that must hold before it is made.

## XR-1 — Where

The `fs-skia-ui-version` compatibility registry lives in **`FS-GG/.github`**:
`registry/dependencies.yml` + `docs/registry/compatibility.md`. It is mutated through the
GitHub-native cross-repo coordination protocol (`gh` + the `cross-repo-coordination` skill), **not**
through files in this repository.

## XR-2 — What

Record the **`FS.GG.UI` BOM/metapackage** as part of the coherent `FS.GG.UI` package set for the
snapshot version `V`, under or alongside the existing `fs-skia-ui-version` contract row — so other
FS-GG repos can discover the BOM and depend on it as the one-reference pin for the set.

## XR-3 — Gate (FR-008): record only after the behavior holds

The record MUST NOT be made before US1+US2 are verified. The preconditions:

- **US1 verified** — CP-A/CP-B pass: a clean consumer referencing only `FS.GG.UI@V` restores+builds
  with 100% of members at `V`.
- **US2 verified** — CP-D pass: a forced member mismatch fails loud (no mixed graph).
- **US3 evidence** — SC-004 pass: clean restore is reproducible (identical resolved set twice), and
  the BOM channel matches the members.

Only then post the registry update + (if a tracking issue exists) a `## Response` linking the
evidence, consistent with the verified publish/restore proof. A partial/missing snapshot keeps the
record **unmade** (loud, closed — no premature coherence claim).

## Pass conditions

| ID | Condition | Maps to |
|----|-----------|---------|
| XR-A | Registry records `FS.GG.UI` BOM in the coherent set, consistent with verified US1/US2/SC-004 evidence. | FR-008, SC-005, US3 AS3 |
| XR-B | The record is made **only after** CP-A/CP-B/CP-D pass — never on a hypothesis. | FR-008 |
| XR-C | No second FS.GG.UI version literal is introduced by the feature anywhere (registry included). | FR-009, SC-005 |
