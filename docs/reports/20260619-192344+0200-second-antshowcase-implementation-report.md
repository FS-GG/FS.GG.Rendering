# Second Ant Showcase Implementation Report

Timestamp: 2026-06-19 19:23:44 +0200

## Scope

Implemented feature `171-second-antshowcase-sample` as a new package-consuming sample under
`samples/SecondAntShowcase`, with Core/App/Tests projects, public Core `.fsi` signatures,
coverage and interaction contracts, review finding lifecycle support, readiness evidence,
and visual-readiness artifacts.

## Library and Framework Problems Encountered

1. Package-feed refresh UX was stricter than the quickstart implied.
   Running `dotnet fsi scripts/refresh-local-feed-and-samples.fsx` failed with
   `package-feed: at least one --sample <path> is required`. The script is named like a
   repo-wide refresh, but the underlying workflow requires explicit `--sample` arguments.
   This is easy to miss during implementation and should be clearer in docs or validated
   before the expensive path starts.

2. FSI loading from the Core output directory did not resolve package dependencies.
   `SecondAntShowcase.Core/bin/Release/net10.0` contains the Core assembly and deps file, but
   not the copied package DLL closure. Loading the Core DLL from FSI failed on
   `FS.GG.UI.Scene`. Pointing FSI at the App output worked because it contains the dependency
   closure. This makes FSI-first API evidence awkward for package-consuming samples.

3. The existing Ant showcase visual-readiness defaults were easy to copy with stale feature
   paths. The inherited summary default still pointed at feature 164 readiness output until
   corrected. This suggests the visual-readiness command needs fewer hardcoded feature paths
   or a feature-local config.

4. The previous minimum-size model covered only six representative pages. Feature 171 requires
   all 19 pages at both accepted sizes, so `VisualConfig.minimumRepresentativePageIds` and
   `PageProfiles.MinimumSizeRepresentative` had to be widened. The framework has no single
   source of truth for "all pages at size X"; page profiles and visual target selection can
   drift.

5. Catalog metadata does not say whether a control is interactive or display-only. I had to
   author `InteractionContracts.displayOnlyReasons` manually and initially missed `timeline`.
   The coverage command can prove every control is assigned to a page, but not whether the
   sample has an honest interaction/display-only classification for each control.

6. Visual readiness can capture complete screenshots but still cannot become accepted without
   reviewer classifications. The generated summaries correctly report `pending-review`, but
   there is no first-class CLI command that consolidates preferred + minimum summaries into the
   feature-level `visual-review-summary.md/json` required by this spec. I created the root
   summaries explicitly.

7. Review findings were not available as a sample-local workflow. I added a small
   `ReviewFindings` Core module and `review-findings` CLI path, but a reusable framework-level
   finding lifecycle would avoid each sample inventing its own record shape and gate behavior.

8. Public `.fsi` surface baselines are not supported by a dedicated sample helper. The sample
   now has a compact SHA-256 manifest test, but this was hand-rolled. A standard baseline tool
   would reduce drift and keep reports consistent across samples.

9. Screenshot evidence and visual-readiness evidence use related but separate shapes. The
   representative `evidence` command writes per-page records, while `visual-readiness` writes
   matrix summaries and contact sheets. Consumers must know how to correlate them manually.

10. The copied sample contains inherited regression suites with feature-specific names. They are
    useful, but they add cognitive noise when adapting the sample to a new feature. This is a
    framework/sample-maintenance issue rather than a correctness issue.

## Improvements for the Library and Framework

1. Add interaction metadata to `Catalog.supportedControls`.
   Suggested fields: `InteractionKind`, `DisplayOnlyReason option`, `RepresentativeAction`,
   and `EvidenceFamily`. Then samples can generate interaction-coverage checks directly from
   catalog metadata.

2. Provide a package-consuming sample scaffold command.
   It should create Core/App/Tests projects, local-feed config, `.fsi` inventory, baseline
   tests, package pins, evidence folders, and README/provenance placeholders without copying an
   older sample by hand.

3. Make `refresh-local-feed-and-samples.fsx` discover samples or fail with actionable usage
   before invoking the harness. A `--sample samples/SecondAntShowcase` example should be printed
   in the error.

4. Add a framework-supported FSI prelude for package-consuming samples. The prelude should
   resolve the dependency closure reliably, ideally by pointing at the built App output or a
   generated assembly probing path.

5. Add a visual-readiness aggregate command:
   `visual-readiness --aggregate --preferred <dir> --minimum <dir> --out <readiness>`.
   It should write `visual-review-summary.md`, `visual-review-summary.json`, and preserve
   caveats about pending reviewer classifications.

6. Promote review finding lifecycle support into `FS.GG.UI.Testing`.
   Samples should not need to reimplement statuses like `open`, `fixed`, `reviewed`, and
   `closed`, unresolved-count gates, malformed finding checks, and target classification checks.

7. Provide a standard `.fsi` surface baseline helper.
   A tool or test helper could hash or normalize `.fsi` files, write baselines, and produce
   clear update instructions when the public sample surface changes.

8. Separate visual capture completeness from visual acceptance more explicitly in APIs.
   The current summaries do this, but downstream code still needs to inspect several fields to
   explain "screenshots complete but blocked pending reviewer classification."

9. Add a feature-local readiness config file.
   Commands should not need hardcoded paths to `specs/164...` or `specs/171...`; they should
   read the active feature readiness root or accept a single config path.

10. Add a sample evidence aggregator that writes coverage, interaction, evidence, visual, and
    finding summaries in the contract shape required by Spec Kit tasks.

## Skill Improvements

1. `speckit-implement` should explicitly remind agents to run package-feed proof with
   `--sample <path>` when the active feature is a package-consuming sample.

2. `speckit-implement` should include a readiness allowlist checklist item before writing files
   under `specs/*/readiness/`, including the exact `git check-ignore` proof pattern.

3. `fs-gg-ant-design` could include a sample-specific checklist for visual readiness:
   all pages, both themes, both sizes, local Ant docs only, and no accepted status without
   reviewer classification.

4. A sample implementation skill would be useful for converting an existing sample into a new
   package-consuming sample without stale paths, stale feature names, or copied minimum-matrix
   assumptions.

5. The skills should distinguish "real screenshot captured" from "visual fidelity accepted" in
   their done criteria. This feature produced complete screenshots but remains blocked pending
   reviewer classifications.

## Final Caveat

Implementation and automated validation are complete, and real screenshot artifacts were
generated for all 76 preferred/minimum visual targets. Final visual fidelity is not accepted
yet because reviewer classifications are still missing, and the readiness summaries keep that
blocked status visible.
