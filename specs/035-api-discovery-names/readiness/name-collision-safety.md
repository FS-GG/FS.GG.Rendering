# Name Collision Safety

| Name | Scene owner | Controls owner | Symbol kind | Risk | Decision | Guidance | Validation scenario |
|------|-------------|----------------|-------------|------|----------|----------|---------------------|
| `Text` | `FS.GG.UI.Scene.TextRun` | `FS.GG.UI.Controls.TextBlock.create` | text authoring | risk: open-order-sensitive | decision: contract-qualified | Use `FS.GG.UI.Scene.TextRun` and `FS.GG.UI.Controls.TextBlock.create`. | validation: mixed-scene-controls-open-scene-first |
| `Width` | `FS.GG.UI.Scene.Rect.Width` | controls layout attributes | geometry field | risk: open-order-sensitive | decision: consumer-guidance | Use `FS.GG.UI.Scene.Rect` for geometry and qualified Controls builders for widgets. | validation: mixed-scene-controls-open-controls-first |
| `Height` | `FS.GG.UI.Scene.Rect.Height` | controls layout attributes | geometry field | risk: open-order-sensitive | decision: consumer-guidance | Use `FS.GG.UI.Scene.Rect` for scene boxes. | validation: mixed-scene-controls-open-scene-first |
| `Color` | `FS.GG.UI.Scene.Paint` | control style values | color authoring | risk: open-order-sensitive | decision: contract-qualified | Use `FS.GG.UI.Scene.Paint` for scene paint. | validation: mixed-scene-controls-open-controls-first |
| `Changed` | scene diagnostics | `FS.GG.UI.Controls.TextBox.onChanged` | event authoring | risk: open-order-sensitive | decision: contract-qualified | Use `FS.GG.UI.Controls.TextBox.onChanged` for control events. | validation: mixed-scene-controls-open-scene-first |
| `children` | scene grouping | `FS.GG.UI.Controls.Stack.children` | builder authoring | risk: open-order-sensitive | decision: consumer-guidance | Use `FS.GG.UI.Controls.Stack.children` for control composition. | validation: mixed-scene-controls-open-controls-first |
| `create` | scene construction helpers | controls builders | builder authoring | risk: open-order-sensitive | decision: consumer-guidance | Qualify builders by package module. | validation: mixed-scene-controls-open-scene-first |

compatibility: no-contract-change
compatibility: signature-change-reviewed

Guidance samples:

```fsharp
let rect : FS.GG.UI.Scene.Rect = { X = 0.0; Y = 0.0; Width = 120.0; Height = 48.0 }
let paint = FS.GG.UI.Scene.Paint.Solid(FS.GG.UI.Scene.Colors.black)
let text = FS.GG.UI.Scene.TextRun
let title = FS.GG.UI.Controls.TextBlock.create []
let input = FS.GG.UI.Controls.TextBox.onChanged id
let stack = FS.GG.UI.Controls.Stack.children []
```
