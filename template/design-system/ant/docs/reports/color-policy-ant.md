# Color Policy Report — Ant Design contrast expectations (`ant`)

> GENERATED — do not edit. Regenerate via: UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature127
> Authority: Ant Design expectation (not WCAG-certified)

| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |
|---------|-----------|-----------|------|----------|-----------|---------|------|
| text-on-canvas | #1f2937 | #f5f5f5 | Text | 13.46 | 4.50 | Aa |  |
| text-on-surface | #1f2937 | #ffffff | Text | 14.68 | 4.50 | Aa |  |
| muted-text-on-surface | #64748b | #ffffff | Text | 4.76 | 4.50 | Aa |  |
| primary-fg-on-surface | #1677ff | #ffffff | GraphicOrUi | 4.10 | 2.50 | Aa |  |
| success-fg-on-surface | #15803d | #ffffff | GraphicOrUi | 5.02 | 2.50 | Aa |  |
| warning-fg-on-surface | #b45309 | #ffffff | GraphicOrUi | 5.02 | 2.50 | Aa |  |
| error-fg-on-surface | #b91c1c | #ffffff | GraphicOrUi | 6.47 | 2.50 | Aa |  |
| info-fg-on-surface | #1677ff | #ffffff | GraphicOrUi | 4.10 | 2.50 | Aa |  |
| primary-hover-fg-on-surface | #4096ff | #ffffff | GraphicOrUi | 2.99 | 2.50 | Aa | ant: not WCAG-certified |
| decorative-hairline-on-surface | #d9d9d9 | #ffffff | Decorative | 1.41 | n/a | out-of-scope |  |

**Overall: PASS** (0 failing of 9 validated; 1 out-of-scope; 0 indeterminate)

<!--
PROVENANCE (Feature 128, Workstream F3): this imprint is F2's docs/reports/color-policy-ant.md
reused verbatim as committed product data — NO new color values or rules are introduced here.
The Ant thresholds/tokens trace to the archived EHotwagner/FS-Skia-UI Ant adoption analysis,
rebranded FS.Skia.UI.* -> FS.GG.UI.*. The report above is the body of F2's drift-gated oracle
(docs/reports/color-policy-ant.md); the generated product carries it as a self-describing record
of the Ant policy it declares (see design-system.json), not as a runtime input (F4 wires runtime use).
-->
