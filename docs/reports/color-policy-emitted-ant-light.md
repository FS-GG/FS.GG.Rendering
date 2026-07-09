# Color Policy Report — WCAG 2.x contrast (`wcag`) — emitted styles, `ant-light` theme

> GENERATED — do not edit. Regenerate via: UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature127
> Authority: WCAG-certified

| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |
|---------|-----------|-----------|------|----------|-----------|---------|------|
| button/neutral/normal#text | #f5f5f5 | #1677ff | Text | 3.76 | 4.50 | Fail |  |
| button/neutral/normal#surface | #1677ff | #f5f5f5 | GraphicOrUi | 3.76 | 3.00 | Aa |  |
| button/neutral/pressed#text | #f5f5f5 | #d9d9d9 | Text | 1.29 | 4.50 | Fail |  |
| button/neutral/pressed#surface | #d9d9d9 | #f5f5f5 | GraphicOrUi | 1.29 | 3.00 | Fail |  |
| button/neutral/disabled#text | #d9d9d9 | #d9d9d9 | Decorative | 1.00 | n/a | Exempt |  |
| button/neutral/disabled#surface | #d9d9d9 | #f5f5f5 | Decorative | 1.29 | n/a | Exempt |  |
| button/neutral/invalid#text | #b91c1c | #1677ff | Text | 1.58 | 4.50 | Fail |  |
| button/danger/normal#text | #f5f5f5 | #b91c1c | Text | 5.93 | 4.50 | Aa |  |
| button/danger/normal#surface | #b91c1c | #f5f5f5 | GraphicOrUi | 5.93 | 3.00 | Aa |  |
| button/danger/invalid#text | #b91c1c | #b91c1c | Text | 1.00 | 4.50 | Fail |  |
| button/success/normal#text | #f5f5f5 | #15803d | Text | 4.60 | 4.50 | Aa |  |
| button/success/normal#surface | #15803d | #f5f5f5 | GraphicOrUi | 4.60 | 3.00 | Aa |  |
| button/success/invalid#text | #b91c1c | #15803d | Text | 1.29 | 4.50 | Fail |  |
| button/warning/normal#text | #f5f5f5 | #b45309 | Text | 4.61 | 4.50 | Aa |  |
| button/warning/normal#surface | #b45309 | #f5f5f5 | GraphicOrUi | 4.61 | 3.00 | Aa |  |
| button/warning/invalid#text | #b91c1c | #b45309 | Text | 1.29 | 4.50 | Fail |  |
| button/ghost/normal#text | #1f2937 | #f5f5f5 | Text | 13.46 | 4.50 | Aaa |  |
| button/ghost/hover#text | #1f2937 | #1677ff | Text | 3.58 | 4.50 | Fail |  |
| button/ghost/pressed#text | #1f2937 | #d9d9d9 | Text | 10.40 | 4.50 | Aaa |  |
| button/ghost/invalid#text | #b91c1c | #f5f5f5 | Text | 5.93 | 4.50 | Aa |  |
| icon-button/neutral/normal#text | #1677ff | #f5f5f5 | Text | 3.76 | 4.50 | Fail |  |
| icon-button/neutral/hover#text | #1677ff | #1677ff | Text | 1.00 | 4.50 | Fail |  |
| icon-button/neutral/pressed#text | #1677ff | #d9d9d9 | Text | 2.91 | 4.50 | Fail |  |
| icon-button/ghost/normal#border | #1f2937 | #f5f5f5 | GraphicOrUi | 13.46 | 3.00 | Aa |  |

**Overall: FAIL** (11 failing of 24 validated; 0 out-of-scope; 0 indeterminate)
