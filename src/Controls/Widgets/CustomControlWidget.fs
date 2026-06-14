namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

module CustomControl =
    let ofControl (control: Control<'msg>) : Widget<'msg> = Widget.ofControl control
