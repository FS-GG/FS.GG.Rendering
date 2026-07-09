# Color Policy Report — WCAG 2.x contrast (`wcag`) — emitted styles, `default-dark` theme

> GENERATED — do not edit. Regenerate via: UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature127
> Authority: WCAG-certified

| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |
|---------|-----------|-----------|------|----------|-----------|---------|------|
| button/neutral/normal#text | #111827 | #60a5fa | Text | 6.98 | 4.50 | Aa |  |
| button/neutral/normal#surface | #60a5fa | #111827 | GraphicOrUi | 6.98 | 3.00 | Aa |  |
| button/neutral/pressed#text | #111827 | #94a3b8 | Text | 6.92 | 4.50 | Aa |  |
| button/neutral/pressed#surface | #94a3b8 | #111827 | GraphicOrUi | 6.92 | 3.00 | Aa |  |
| button/neutral/disabled#text | #94a3b8 | #94a3b8 | Decorative | 1.00 | n/a | Exempt |  |
| button/neutral/disabled#surface | #94a3b8 | #111827 | Decorative | 6.92 | n/a | Exempt |  |
| button/neutral/invalid#text | #ff9592 | #60a5fa | Text | 1.21 | 4.50 | Fail |  |
| button/danger/normal#text | #111827 | #ff9592 | Text | 8.42 | 4.50 | Aaa |  |
| button/danger/normal#surface | #ff9592 | #111827 | GraphicOrUi | 8.42 | 3.00 | Aa |  |
| button/danger/invalid#text | #ff9592 | #ff9592 | Text | 1.00 | 4.50 | Fail |  |
| button/success/normal#text | #111827 | #4ade80 | Text | 10.18 | 4.50 | Aaa |  |
| button/success/normal#surface | #4ade80 | #111827 | GraphicOrUi | 10.18 | 3.00 | Aa |  |
| button/success/invalid#text | #ff9592 | #4ade80 | Text | 1.21 | 4.50 | Fail |  |
| button/warning/normal#text | #111827 | #fbbf24 | Text | 10.63 | 4.50 | Aaa |  |
| button/warning/normal#surface | #fbbf24 | #111827 | GraphicOrUi | 10.63 | 3.00 | Aa |  |
| button/warning/invalid#text | #ff9592 | #fbbf24 | Text | 1.26 | 4.50 | Fail |  |
| button/ghost/normal#text | #f1f5f9 | #111827 | Text | 16.19 | 4.50 | Aaa |  |
| button/ghost/hover#text | #f1f5f9 | #60a5fa | Text | 2.32 | 4.50 | Fail |  |
| button/ghost/pressed#text | #f1f5f9 | #94a3b8 | Text | 2.34 | 4.50 | Fail |  |
| button/ghost/invalid#text | #ff9592 | #111827 | Text | 8.42 | 4.50 | Aaa |  |
| icon-button/neutral/normal#text | #60a5fa | #111827 | Text | 6.98 | 4.50 | Aa |  |
| icon-button/neutral/hover#text | #60a5fa | #60a5fa | Text | 1.00 | 4.50 | Fail |  |
| icon-button/neutral/pressed#text | #60a5fa | #94a3b8 | Text | 1.01 | 4.50 | Fail |  |
| icon-button/ghost/normal#border | #f1f5f9 | #111827 | GraphicOrUi | 16.19 | 3.00 | Aa |  |

**Overall: FAIL** (8 failing of 24 validated; 0 out-of-scope; 0 indeterminate)
