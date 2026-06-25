namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput

module Canvas =

    let scene (scene: Scene) : Attr<'msg> =
        Attr.create ControlPrimitives.CanvasSceneAttr AttrCategory.Data (SceneValue scene)

    let viewport (transform: PerspectiveTransform) : Attr<'msg> =
        Attr.create ControlPrimitives.CanvasViewportAttr AttrCategory.Data (UntypedValue(transform :> obj))

    // A generalizable record literal (all fields are syntactic values) so the value-typed attribute
    // generalizes to `Attr<'msg>` without tripping the value restriction.
    let volatile': Attr<'msg> =
        { Name = ControlPrimitives.CanvasVolatileAttr
          Category = AttrCategory.State
          Value = BoolValue true }

    // Raw input handlers ride `UntypedValue` (the same boxing the standard event channel uses); the
    // Controls.Elmish router unboxes them for `canvas` nodes (C6). Looked up by the attribute name.
    let onPointer (map: PointerSample -> 'msg) : Attr<'msg> =
        Attr.create "onPointer" AttrCategory.Event (UntypedValue(map :> obj))

    let onKey (map: ViewerKey -> KeyModifiers -> 'msg) : Attr<'msg> =
        Attr.create "onKey" AttrCategory.Event (UntypedValue(map :> obj))

    let create (attrs: Attr<'msg> list) : Control<'msg> = Control.create "canvas" attrs
