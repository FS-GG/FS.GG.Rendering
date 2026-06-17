package-id: FS.GG.UI.Controls
package-version: 0.1.9-preview.1
symbol-count: 6
omitted-symbol-reasons: none
unsupported-symbols: none
source: src/Controls/Types.fsi
source: src/Controls/Control.fsi
source: src/Controls/Attributes.fsi
source: src/Controls/DataGrid.fsi
source: src/Controls/Charts.fsi
source: src/Controls/TextInput.fsi
source: src/Controls/RichText.fsi

```fsharp
/// The typed `Props` front door is preserved in the package-shaped reference.
type Control<'msg> = obj
type KnownControl = KnownControl.TextBlock
type StandardAttributeName = StandardAttributeName.VisibleRange
module DataGrid =
    val create: columns: DataGridColumn list -> attrs: Attr<'msg> list -> Control<'msg>
module LineChart =
    val series: ChartSeries list -> Attr<'msg>
module TextBox =
    val onChanged: (string -> 'msg) -> Attr<'msg>
```

Qualified samples: DataGrid.create, LineChart.series, TextBox.onChanged.
