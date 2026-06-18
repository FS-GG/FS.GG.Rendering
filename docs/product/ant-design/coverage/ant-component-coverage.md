# Ant component coverage matrix (D2.1)

**Ant source**: the repo Ant reference hub — [`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md).
**Snapshot retrieval date**: `2026-06-16` (owned by the hub; this matrix does not restate an upstream version label — Ant publishes none in its LLM docs).

One row per Ant component-overview entry. `disposition` ∈ {`existing`, `net-new`, `composition`, `not-applicable`}.
`repoControls` ids resolve in `FS.GG.UI.Controls.Catalog`; `tokenEntries` resolve in the `FS.GG.UI.DesignSystem` public token surface.
The honesty check `tests/Controls.Tests/Feature132CoverageMatrixTests.fs` fails on any missing row, dangling control/token reference, blank disposition, or missing rationale.

| antComponent | antCategory | disposition | repoControls | tokenEntries | rationale |
|---|---|---|---|---|---|
| Button | General | existing | button | Component.Button.primaryBg, Seed.colorPrimary | — |
| FloatButton | General | net-new | float-button | Seed.colorPrimary | — |
| Icon | General | existing | icon | Alias.Light.textDefault | — |
| Typography | General | existing | text-block, label, rich-text | Type.Body.fontSize | — |
| Divider | Layout | existing | separator | Alias.Light.borderDefault | — |
| Flex | Layout | existing | wrap | Space.md | — |
| Grid | Layout | existing | grid | Space.md | — |
| Layout | Layout | existing | dock | Alias.Light.surfaceCanvas | — |
| Space | Layout | existing | stack | Space.sm | — |
| Splitter | Layout | existing | split-view | Alias.Light.borderDefault | — |
| Anchor | Navigation | net-new | anchor | Seed.colorPrimary | — |
| Breadcrumb | Navigation | net-new | breadcrumb | Alias.Light.textSecondary | — |
| Dropdown | Navigation | composition | menu, button | Component.Menu.itemHoverBg | Button trigger + menu overlay; no new primitive needed. |
| Menu | Navigation | existing | menu | Component.Menu.itemSelectedBg | — |
| Pagination | Navigation | net-new | pagination | Seed.colorPrimary | — |
| Steps | Navigation | net-new | steps | Seed.colorPrimary | — |
| Tabs | Navigation | existing | tabs | Component.Tabs.inkBar | — |
| AutoComplete | Data Entry | net-new | auto-complete | Component.Input.activeBorder | — |
| Cascader | Data Entry | net-new | cascader | Alias.Light.borderDefault | — |
| Checkbox | Data Entry | existing | check-box | Seed.colorPrimary | — |
| ColorPicker | Data Entry | existing | color-picker | Alias.Light.borderDefault | — |
| DatePicker | Data Entry | existing | date-picker | Component.Input.activeBorder | — |
| Form | Data Entry | composition | stack, label, text-box, validation-message | Space.md | A layout of labelled fields + validation; expressed by composition, not a primitive. |
| Input | Data Entry | existing | text-box | Component.Input.activeBorder | — |
| InputNumber | Data Entry | existing | numeric-input | Component.Input.hoverBorder | — |
| Mentions | Data Entry | composition | text-area, menu | Component.Input.placeholderText | Text-area + suggestion menu overlay; MVU too heavy for the value, composed instead. |
| Radio | Data Entry | existing | radio-group | Seed.colorPrimary | — |
| Rate | Data Entry | net-new | rate | Seed.colorWarning | — |
| Select | Data Entry | existing | combo-box | Component.Input.activeBorder | — |
| Slider | Data Entry | existing | slider | Seed.colorPrimary | — |
| Switch | Data Entry | existing | switch | Seed.colorPrimary | — |
| TimePicker | Data Entry | existing | time-picker | Component.Input.activeBorder | — |
| Transfer | Data Entry | composition | list-box, button | Component.Table.borderColor | Dual list-box panels + move buttons; composition over a heavy MVU primitive. |
| TreeSelect | Data Entry | existing | tree-view | Component.Input.activeBorder | — |
| Upload | Data Entry | net-new | upload | Alias.Light.borderDefault | — |
| Avatar | Data Display | net-new | avatar | Seed.colorPrimary | — |
| Badge | Data Display | existing | badge | Seed.colorError | — |
| Calendar | Data Display | net-new | calendar | Alias.Light.borderDefault | — |
| Card | Data Display | net-new | card | Alias.Light.surfaceContainer | — |
| Carousel | Data Display | net-new | carousel | Alias.Light.surfaceContainer | — |
| Collapse | Data Display | net-new | collapse | Alias.Light.surfaceContainer | — |
| Descriptions | Data Display | net-new | descriptions | Alias.Light.textSecondary | — |
| Empty | Data Display | net-new | empty | Alias.Light.textSecondary | — |
| Image | Data Display | existing | image | Alias.Light.borderDefault | — |
| List | Data Display | existing | list-view | Component.Table.rowHoverBg | — |
| Popover | Data Display | net-new | popover | Alias.Light.surfaceElevated | — |
| QRCode | Data Display | net-new | qr-code | Alias.Light.textDefault | — |
| Segmented | Data Display | net-new | segmented | Alias.Light.itemSelectedBg | — |
| Statistic | Data Display | net-new | statistic | Seed.colorPrimary | — |
| Table | Data Display | existing | data-grid | Component.Table.headerBg | — |
| Tag | Data Display | net-new | tag | Seed.colorPrimary | — |
| Timeline | Data Display | net-new | timeline | Seed.colorPrimary | — |
| Tooltip | Data Display | existing | tooltip | Alias.Light.surfaceElevated | — |
| Tour | Data Display | net-new | tour | Alias.Light.surfaceElevated | — |
| Tree | Data Display | existing | tree-view | Component.Menu.itemHoverBg | — |
| Alert | Feedback | net-new | alert | Seed.colorWarning | — |
| Drawer | Feedback | net-new | drawer | Alias.Light.surfaceElevated | — |
| Message | Feedback | composition | toast | Alias.Light.surfaceElevated | Transient top banner; the toast control expresses it directly. |
| Modal | Feedback | existing | dialog | Alias.Light.surfaceElevated | — |
| Notification | Feedback | composition | toast | Alias.Light.surfaceElevated | Stacked corner toasts; composition of the toast control. |
| Popconfirm | Feedback | net-new | popconfirm | Alias.Light.surfaceElevated | — |
| Progress | Feedback | existing | progress-bar | Seed.colorPrimary | — |
| Result | Feedback | net-new | result | Seed.colorSuccess | — |
| Skeleton | Feedback | net-new | skeleton | Alias.Light.borderDefault | — |
| Spin | Feedback | existing | spinner | Seed.colorPrimary | — |
| Watermark | Feedback | net-new | watermark | Alias.Light.textSecondary | — |
| Affix | Other | net-new | affix | Seed.colorPrimary | — |
| App | Other | not-applicable | — | — | React context wrapper; no UI-control analog. |
| ConfigProvider | Other | not-applicable | — | — | React/DOM theme provider; theme + IntentPolicy selection exists, not as a control. |
| Util | Other | not-applicable | — | — | Internal React utilities; not a UI component. |

## Summary

Total Ant overview components dispositioned: **70** (zero un-dispositioned — SC-002).

- existing: 31
- net-new: 30
- composition: 6
- not-applicable: 3
