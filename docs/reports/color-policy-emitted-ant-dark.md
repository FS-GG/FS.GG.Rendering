# Color Policy Report — WCAG 2.x contrast (`wcag`) — emitted styles, `ant-dark` theme

> GENERATED — do not edit. Regenerate via: UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature127
> Authority: WCAG-certified

| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |
|---------|-----------|-----------|------|----------|-----------|---------|------|
| button/neutral/normal#text | #000000 | #1677ff | Text | 5.12 | 4.50 | Aa |  |
| button/neutral/normal#surface | #1677ff | #000000 | GraphicOrUi | 5.12 | 3.00 | Aa |  |
| button/neutral/pressed#text | #000000 | #424242 | Text | 2.09 | 4.50 | Fail |  |
| button/neutral/pressed#surface | #424242 | #000000 | GraphicOrUi | 2.09 | 3.00 | Fail |  |
| button/neutral/disabled#text | #424242 | #424242 | Decorative | 1.00 | n/a | Exempt |  |
| button/neutral/disabled#surface | #424242 | #000000 | Decorative | 2.09 | n/a | Exempt |  |
| button/neutral/invalid#text | #b91c1c | #1677ff | Text | 1.58 | 4.50 | Fail |  |
| button/danger/normal#text | #000000 | #b91c1c | Text | 3.25 | 4.50 | Fail |  |
| button/danger/normal#surface | #b91c1c | #000000 | GraphicOrUi | 3.25 | 3.00 | Aa |  |
| button/danger/invalid#text | #b91c1c | #b91c1c | Text | 1.00 | 4.50 | Fail |  |
| button/success/normal#text | #000000 | #15803d | Text | 4.19 | 4.50 | Fail |  |
| button/success/normal#surface | #15803d | #000000 | GraphicOrUi | 4.19 | 3.00 | Aa |  |
| button/success/invalid#text | #b91c1c | #15803d | Text | 1.29 | 4.50 | Fail |  |
| button/warning/normal#text | #000000 | #b45309 | Text | 4.18 | 4.50 | Fail |  |
| button/warning/normal#surface | #b45309 | #000000 | GraphicOrUi | 4.18 | 3.00 | Aa |  |
| button/warning/invalid#text | #b91c1c | #b45309 | Text | 1.29 | 4.50 | Fail |  |
| button/ghost/normal#text | #f1f5f9 | #000000 | Text | 19.17 | 4.50 | Aaa |  |
| button/ghost/hover#text | #f1f5f9 | #1677ff | Text | 3.75 | 4.50 | Fail |  |
| button/ghost/pressed#text | #f1f5f9 | #424242 | Text | 9.17 | 4.50 | Aaa |  |
| button/ghost/invalid#text | #b91c1c | #000000 | Text | 3.25 | 4.50 | Fail |  |
| icon-button/neutral/normal#text | #1677ff | #000000 | Text | 5.12 | 4.50 | Aa |  |
| icon-button/neutral/hover#text | #1677ff | #1677ff | Text | 1.00 | 4.50 | Fail |  |
| icon-button/neutral/pressed#text | #1677ff | #424242 | Text | 2.45 | 4.50 | Fail |  |
| icon-button/ghost/normal#border | #f1f5f9 | #000000 | GraphicOrUi | 19.17 | 3.00 | Aa |  |

**Overall: FAIL** (13 failing of 24 validated; 0 out-of-scope; 0 indeterminate)
