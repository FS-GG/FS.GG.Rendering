# Phase 0 Research: Second Ant Showcase Sample

## Decision: Build a new `samples/SecondAntShowcase` sample beside `samples/AntShowcase`

Rationale: The feature requires an independent second showcase that does not replace, rename, or weaken the existing sample. The existing AntShowcase already proves the right repository shape: package-consuming sample, Core/App/Tests split, deterministic evidence, GL-gated interactive mode, and visual-readiness commands. Reusing that shape minimizes risk while allowing the second sample to raise the interaction and visual review bar.

Alternatives considered:

- Modify `samples/AntShowcase`: rejected because FR-001 requires the existing sample to remain intact.
- Create a generic generated-product sample: rejected because the feature is specifically a second Ant showcase with enterprise templates and complete control coverage.
- Add the sample to the root solution as a default test tier member: rejected for planning because existing samples are outside the default tier and consume packed packages through their own local feed proof.

## Decision: Treat Ant Design as local design-language guidance only

Rationale: The repo's Ant guidance states that FS.GG adopts Ant as a design language only. Ant facts come from `docs/product/ant-design/reference/ant-llms-sources.md`, the family pattern docs under `docs/product/ant-design/patterns/`, and the `fs-gg-ant-design` skill. The sample must compose existing semantic `Catalog` controls, theme them with `FS.GG.UI.Themes.AntDesign`, and validate pairings through existing token, resolver, and color-policy seams.

Alternatives considered:

- Pull live upstream Ant docs during implementation: rejected because this repo has a local source hub with a pinned retrieval date and central citation policy.
- Introduce React/DOM/HTML/CSS assets to imitate Ant: rejected by the repo layering rule and by this feature's scope.
- Create `Ant*` behavior controls for sample fidelity: rejected because the semantic control set must remain singular and theme-styled.

## Decision: Use a live catalog coverage bijection

Rationale: "Every current control exactly once" is best enforced by reading `FS.GG.UI.Controls.Catalog.supportedControls` and comparing it to catalog page assignments. Current planning context shows 96 catalog controls and the proven 13 catalog-page grouping used by AntShowcase. The second sample should keep template pages outside the bijection and validate template composition separately.

Alternatives considered:

- Hard-code the current count as the only acceptance rule: rejected because the control catalog can drift.
- Allow controls to appear on both catalog and template pages in the same coverage count: rejected because templates are composition examples and would create false duplicates.
- Group only by the 11 Ant pattern families: rejected because the existing 13 page grouping gives charts and dense input/control pages enough inspection room while still tracing to the 11 pattern docs.

## Decision: Model all interaction through a pure MVU Core

Rationale: The sample has navigation, theme switching, overlays, forms, validation, selection, pagination, and evidence scripts. The constitution requires stateful workflows to be observable through an Elmish/MVU boundary. A single `Model`, `Msg`, and `update` keeps interactions deterministic, testable, and theme-independent.

Alternatives considered:

- Store state inside individual control construction helpers: rejected because interaction evidence and theme switching would be difficult to prove.
- Use imperative App-side callbacks as the behavior source of truth: rejected because tests would depend on live host behavior and GL availability.
- Split every page into a separate state model: rejected for initial implementation because it adds ceremony without improving review coverage.

## Decision: Seed representative content deterministically

Rationale: FR-004 requires realistic content, not empty placeholders. Deterministic literals for table rows, chart series, graph nodes, form values, dates, menu choices, and status content let the same review path produce repeatable evidence.

Alternatives considered:

- Generate random content per run: rejected because evidence must repeat with the same inputs.
- Leave difficult controls empty and label them as unsupported: rejected because the feature explicitly requires representative content.
- Read data from external fixtures or services: rejected because no storage or network dependency is needed.

## Decision: Preserve state across theme switching by separating data from visual resolution

Rationale: Theme mode should only select `AntTheme.antLight` or `AntTheme.antDark`; all page and control state remains in the sample model. This directly satisfies FR-011 and avoids stale visual state coupling.

Alternatives considered:

- Reinitialize page state on theme change: rejected by the theme-switch acceptance scenarios.
- Keep separate light and dark state stores: rejected because behavior must be identical across appearances.

## Decision: Review all pages at both accepted sizes

Rationale: This feature's spec requires all pages in both Ant light and Ant dark at the accepted preferred and minimum review sizes. With 13 catalog pages and six templates, the required visual review matrix is 76 targets. This is stricter than the older AntShowcase minimum representative subset and is necessary for the requested iterative Ant fidelity loop.

Alternatives considered:

- Reuse AntShowcase's 38 preferred plus 12 minimum matrix: rejected because FR-014 states all pages at accepted preferred and minimum sizes for this feature.
- Test only dense pages at minimum size: rejected because clipping and overlap can appear on any page.
- Accept headless screenshots as final visual proof: rejected because environment-limited evidence cannot prove live visual fidelity.

## Decision: Make visual findings first-class evidence

Rationale: FR-015 through FR-017 require recording palette, spacing, typography, contrast, clipping, overlap, alignment, state, and Ant conformance findings, and keeping them unresolved until fixed and reviewed again. A finding record with status transitions from `open` to `fixed` to `reviewed` to `closed` makes the fix-and-review loop auditable.

Alternatives considered:

- Use only pass/fail screenshot completeness: rejected because completeness does not prove Ant fidelity.
- Store reviewer notes as free-form Markdown only: rejected because unresolved counts must be machine-checkable.
- Close findings automatically after code changes: rejected because the spec requires affected surfaces to be reviewed again.

## Decision: Keep evidence honest about environment limitations

Rationale: The repo's testing principle requires real evidence where possible and visible disclosure for synthetic or environment-limited substitutes. The sample must complete without hanging when no live GL/display is available, but such output is a limitation disclosure, not accepted visual evidence.

Alternatives considered:

- Fail the command when no live display exists: rejected because SC-009 requires completion under 30 seconds with clear limitation disclosure.
- Treat offscreen or headless output as final visual acceptance: rejected because visual fidelity must not be overstated.
- Skip evidence generation entirely on headless hosts: rejected because maintainers still need coverage, determinism, and limitation records.
