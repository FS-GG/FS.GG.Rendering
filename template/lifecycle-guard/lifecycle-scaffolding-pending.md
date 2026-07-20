# Lifecycle scaffolding pending (sdd lane)

This product was scaffolded with `--lifecycle sdd` — the default since **ADR-0056**
(`sdd` is the default lifecycle; `spec-kit` is legacy, frozen, and scheduled for
removal). The `sdd` lane emits the product **only** and expects an external SDD
lifecycle owner to re-supply the lifecycle scaffolding (the constitution and the
governance gate set the workspace is governed by).

Until that is supplied, this product is **lifecycle-less**, and this file is the
guard that says so:

- The `FsGgSddLifecycleGuard` build target (in `Directory.Build.props`) **warns on
  every build** while this file is present.
- The `Verify` readiness/doctor gate **fails closed (red)** while this file is
  present — a lifecycle-less product cannot pass the merge-gate audit.

`sdd` and `none` produce a byte-identical product tree; this sentinel is the one
file that records which intent you chose, so `none` (a deliberately lifecycle-less
product) stays silent while `sdd` does not.

## To resolve, do ONE of

- **Re-supply the lifecycle:** run `fsgg-sdd`, which composes the lifecycle owner
  onto this product and **removes this file** — the guard clears and the readiness
  check goes green.
- **Accept a lifecycle-less product deliberately:** re-scaffold with
  `--lifecycle none` (the byte-identical, unguarded lane), or simply delete this
  file to acknowledge the state and clear the guard.

See ADR-0056 for the full decision and the removal timeline for `spec-kit`.
