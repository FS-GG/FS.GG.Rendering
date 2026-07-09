# Color Policy Report — WCAG 2.x contrast (`wcag`) — emitted styles, `default-light` theme

> GENERATED — do not edit. Regenerate via: UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature127
> Authority: WCAG-certified

| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |
|---------|-----------|-----------|------|----------|-----------|---------|------|
| button/neutral/normal#text | #f8fafc | #2563eb | Text | 4.94 | 4.50 | Aa |  |
| button/neutral/normal#surface | #2563eb | #f8fafc | GraphicOrUi | 4.94 | 3.00 | Aa |  |
| button/neutral/pressed#text | #f8fafc | #64748b | Text | 4.55 | 4.50 | Aa |  |
| button/neutral/pressed#surface | #64748b | #f8fafc | GraphicOrUi | 4.55 | 3.00 | Aa |  |
| button/neutral/disabled#text | #64748b | #64748b | Decorative | 1.00 | n/a | Exempt |  |
| button/neutral/disabled#surface | #64748b | #f8fafc | Decorative | 4.55 | n/a | Exempt |  |
| button/neutral/invalid#text | #b91c1c | #2563eb | Text | 1.25 | 4.50 | Fail |  |
| button/danger/normal#text | #f8fafc | #b91c1c | Text | 6.18 | 4.50 | Aa |  |
| button/danger/normal#surface | #b91c1c | #f8fafc | GraphicOrUi | 6.18 | 3.00 | Aa |  |
| button/danger/invalid#text | #b91c1c | #b91c1c | Text | 1.00 | 4.50 | Fail |  |
| button/success/normal#text | #f8fafc | #15803d | Text | 4.79 | 4.50 | Aa |  |
| button/success/normal#surface | #15803d | #f8fafc | GraphicOrUi | 4.79 | 3.00 | Aa |  |
| button/success/invalid#text | #b91c1c | #15803d | Text | 1.29 | 4.50 | Fail |  |
| button/warning/normal#text | #f8fafc | #b45309 | Text | 4.80 | 4.50 | Aa |  |
| button/warning/normal#surface | #b45309 | #f8fafc | GraphicOrUi | 4.80 | 3.00 | Aa |  |
| button/warning/invalid#text | #b91c1c | #b45309 | Text | 1.29 | 4.50 | Fail |  |
| button/ghost/normal#text | #1f2937 | #f8fafc | Text | 14.03 | 4.50 | Aaa |  |
| button/ghost/hover#text | #1f2937 | #2563eb | Text | 2.84 | 4.50 | Fail |  |
| button/ghost/pressed#text | #1f2937 | #64748b | Text | 3.08 | 4.50 | Fail |  |
| button/ghost/invalid#text | #b91c1c | #f8fafc | Text | 6.18 | 4.50 | Aa |  |
| icon-button/neutral/normal#text | #2563eb | #f8fafc | Text | 4.94 | 4.50 | Aa |  |
| icon-button/neutral/hover#text | #2563eb | #2563eb | Text | 1.00 | 4.50 | Fail |  |
| icon-button/neutral/pressed#text | #2563eb | #64748b | Text | 1.09 | 4.50 | Fail |  |
| icon-button/ghost/normal#border | #1f2937 | #f8fafc | GraphicOrUi | 14.03 | 3.00 | Aa |  |

**Overall: FAIL** (8 failing of 24 validated; 0 out-of-scope; 0 indeterminate)
