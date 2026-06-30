# Feature 222 — per-phase Spec Kit / fs-gg-ui feedback (T026)

Captured via the `fs-gg-feedback-capture` flow. Severity: 🟢 minor · 🟡 friction · 🔴 blocker.

## Foundational (Phase 2)

- 🟡 **`tasks.md` T012 named a non-existent package (`FS.GG.UI.Core`)** as the coherent-set sibling
  probe. The real members are `FS.GG.UI.{Scene,Build,Controls,…}` + the `FS.GG.UI` BOM. The probe was
  run against real siblings and the discrepancy disclosed in `coherent-set.md`.
  **Generalizable**: a `/speckit-tasks` step that validates example package names against the org feed
  (or `Directory.Packages.props`) would catch invented identifiers.

## Publish (Phase 3)

- 🟡 **Closing-keyword premature close.** The `main` feature commit prose contained `close #33`, so
  pushing `main` auto-closed #33 at 19:01 — **before** the feed served `V` and before the registry
  flip (publish-before-flip, FR-007). The substantive resolution completed afterward and is recorded on
  the issue, but the issue-state ordering briefly contradicted FR-007.
  **How to apply**: for publish-before-flip features, keep closing keywords OUT of the publishing commit;
  let the registry-PR merge (or an explicit post-flip close) close the tracking issue.

## Selectable (Phase 4)

- 🟢 **Local template shadows the feed package.** `dotnet new` preferred the in-repo `.template.config`
  over the freshly-installed feed package (same template identity), which would have silently validated
  the *local* tree instead of the feed artifact. Uninstalling the local source
  (`dotnet new uninstall <repo>`) made the feed package unambiguous.
  **Generalizable**: the consumer-probe quickstart should include the local-source uninstall as an
  explicit step so feed-vs-local can't be conflated.

## Cross-cutting

- 🟢 The standing **early-live-probe** discipline (read the real feed before any tag) again paid off:
  the gap (`0.1.53` lacks `b78e72a`) was confirmed against the real feed, not inferred.
- 🟢 No `src/.fs` change made the regression argument structural; the heavy 21-project baseline is
  corroboration, not the load-bearing evidence — the live cross-repo proof is.
