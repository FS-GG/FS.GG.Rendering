# Validation strategy

This folder defines the **initial validation set** for FS.GG.Rendering — the output of
migration **Stage R3 (Define the initial validation set)**. It decides *which* tests and
checks are worth importing from the source repository (FS-Skia-UI), with a justification for
each, **before** any tests are copied (test import is Stage R4; the harness is built at
Stage R5).

## Contents

- **[justification-records.md](./justification-records.md)** — one record per candidate
  test/check (product contract, failure mode, owner, frequency, cost, decision); the
  authoritative triage.
- **[validation-set.md](./validation-set.md)** — the bounded `import-now` set, partitioned by
  frequency (local inner loop / CI / release-only); the default local tier is what
  contributors run.
- **[deferral-ledger.md](./deferral-ledger.md)** — deferred / archived / rewrite-pending
  candidates with reasons; discoverable but **not** active obligations.
- **[harness.md](./harness.md)** — the rendering test harness recorded as deliberate
  infrastructure (built at Stage R5), distinct from imported legacy tests.

## See also

- Product shape (Stage R2): [`../product/`](../product/) — the module map that scopes what is
  owned vs excluded.
- Project rules: [`../../.specify/memory/constitution.md`](../../.specify/memory/constitution.md)
  (Development Workflow: every active check carries a justification).
- The migration roadmap and this stage's spec: `specs/002-initial-validation-set/`.
