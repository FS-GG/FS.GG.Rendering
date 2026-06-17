package-id: FS.GG.UI.KeyboardInput
package-version: 0.1.9-preview.1
symbol-count: 4
omitted-symbol-reasons: none
unsupported-symbols: none
source: src/KeyboardInput/KeyboardInput.fsi

```fsharp
/// Keyboard state.
type KeyboardModel = { PressedKeys: Set<KeyId> }
type KeyboardEvent = ViewerKeyEvent
type KeyboardMsg =
    | KeyDown of KeyId
    | KeyUp of KeyId
```
