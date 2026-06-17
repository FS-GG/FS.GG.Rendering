package-id: FS.GG.UI.Scene
package-version: 0.1.9-preview.1
symbol-count: 6
omitted-symbol-reasons: none
unsupported-symbols: none
source: src/Scene/Scene.fsi

```fsharp
/// Scene rectangle.
type Rect = { X: float; Y: float; Width: float; Height: float }
/// Scene paint.
type Paint =
    | LinearGradient of startPoint: Point * endPoint: Point * colors: Color list
    | DropShadow of dx: float * dy: float * blur: float * color: Color
type TextRun = { Text: string }
type SceneElementKind = RectangleElement
```
