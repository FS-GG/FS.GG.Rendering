namespace FS.GG.UI.Controls

type Widget<'msg> = { Lowered: Control<'msg> }

module Widget =
    let ofControl (control: Control<'msg>) : Widget<'msg> = { Lowered = control }

    let toControl (widget: Widget<'msg>) : Control<'msg> = widget.Lowered

    let render (theme: Theme) (widget: Widget<'msg>) : ControlRenderResult<'msg> =
        Control.render theme widget.Lowered

    // Feature 108 (US5, FR-014): map the message type through the lowered control.
    let map (f: 'a -> 'b) (widget: Widget<'a>) : Widget<'b> =
        ofControl (Control.map f (toControl widget))
