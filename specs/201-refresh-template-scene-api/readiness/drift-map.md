# Drift Map (T006) — grounded in the T005 per-profile smoke run (feed V=0.1.48-preview.1)

Authoritative drift signal = real `dotnet new` → restore → build → evidence per profile.

## Profile build results (initial smoke, before edits)

| Profile | Restore | Build | Evidence | Notes |
|---|---|---|---|---|
| headless-scene | ok | **0/0 clean** | scene ok, layout ok (NoLayoutOverlap) | no drift |
| governed | ok | **0/0 clean** | scene ok, layout ok | no drift |
| app | ok | **FAILED** | — | API drift in LayoutEvidence.fs + EvidenceCommands.fs |
| sample-pack | ok | **FAILED** | — | app's drift PLUS missing-Controls structural defect |

## Drift item 1 — LayoutEvidence.fs interactive branch (app + sample-pack) — FIXED

- **Symptom**: `report.TextBounds` makes F# infer `report : VisualTextInspection` (the last-declared
  `FS.GG.UI.Scene` type carrying a `TextBounds` field) instead of `LayoutEvidenceReport`. Cascade:
  `'VisualTextInspection' does not define GameplayBounds/HudRegion/GameplayRegion`, `Rect` vs
  `Rect option`, `Option<_> has no IsEmpty`. Also surfaces at `EvidenceCommands.fs:140`
  (`validateGeneratedLayout report`).
- **Surface**: `FS.GG.UI.Scene` Inspection types (`VisualTextInspection`, `VisualRegionBoundary`,
  `VisualInspectionNode`) added to the SAME namespace, sharing field names (`Name`/`Bounds`/`TextBounds`/
  `Diagnostics`/`MeasurementMode`) with the Layout* types but using `Rect option`. NOT a rename — the
  Layout types are unchanged (the headless branch, which uses the same types with an explicit annotation,
  compiles clean). The public surface GREW, making the seed's un-annotated helpers ambiguous.
- **Current equivalent**: same types; disambiguate by annotation.
- **Resolution (DONE)**: annotate the two un-annotated helpers in the interactive branch —
  `overlapDiagnostics (report: LayoutEvidenceReport)` and `validateGeneratedLayout (report: LayoutEvidenceReport)`.
  This fixes every LayoutEvidence.fs error AND EvidenceCommands.fs:140. Verified: **app now builds 0/0 and
  emits scene/layout/launch/image evidence (all ok)**.

## Drift item 2 — sample-pack missing Controls package set — FIXED (operator approved)

- **Symptom**: sample-pack errors `namespace 'Controls'/'DesignSystem'/'Themes' is not defined`, plus
  `ChartSeries/DataGridColumn/RichTextBlock/Attr/Widget/Control/ControlsElmish/AdapterCommand` undefined,
  across Model.fs/View.fs/EvidenceCommands.fs.
- **Root cause**: the shared interactive `//#else` branch (app + sample-pack) in Model.fs/View.fs/
  EvidenceCommands.fs opens and uses `FS.GG.UI.Controls(.Elmish)`, `DesignSystem`, `Themes.Default`,
  `KeyboardInput` (typed Controls front door + Widget.toControl + Control.renderTree). But the package
  guards in `template/base/Directory.Packages.props` and `template/base/src/Product/Product.fsproj`
  reference `Controls/Controls.Elmish/DesignSystem/Themes.Default/KeyboardInput/Layout` for
  `profile == "app"` ONLY. So sample-pack generates Controls-using seed code WITHOUT the Controls packages.
- **Pre-existing**: the seed `//#else` branch has opened `Controls` since the first import (commit 2414cb4);
  the `app`-only guards predate the rebrand. This is a **long-standing latent defect** (sample-pack has not
  been buildable), surfaced by this conformance run — NOT introduced by the version re-pin. (Likely widened
  by the feature-125 split of DesignSystem/Themes out of Controls without updating sample-pack's guards.)
- **Proven fix**: widen the `Controls/Controls.Elmish/DesignSystem/Themes.Default/KeyboardInput/Layout`
  guards from `(profile == "app")` to `(profile == "app" || profile == "sample-pack")` in BOTH
  `Directory.Packages.props` and `Product.fsproj`. Proof: adding those packages to a generated sample-pack
  removed ALL "namespace not defined" errors; the only residue was Drift item 1, already fixed in source.
  Aligns with the governance contract (else-branch = Controls-based Model/View for both; host differs:
  app → ControlsElmish.runInteractiveApp, sample-pack → Viewer.runApp).
- **Scope tension**: SC-001 requires all 4 profiles to build; FR-009 forbids "profile changes." Widening a
  profile's package set is the minimal way to satisfy SC-001 but touches the profile guards. → escalated to
  the operator.
- **Operator decision (this run)**: "Fix it (widen guards)". Applied — widened the `#if (profile == "app")`
  guard to `#if (profile == "app" || profile == "sample-pack")` in `template/base/Directory.Packages.props`
  and `template/base/src/Product/Product.fsproj`. Verified: sample-pack now generates → restores → builds
  **0/0**, emits scene/layout/launch/image evidence (all ok), and passes 28 GovernanceTests. The other
  three profiles are unaffected (governed/headless-scene use the first branch; app already included).
