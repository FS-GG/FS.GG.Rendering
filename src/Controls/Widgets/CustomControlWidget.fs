namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

module CustomControl =
    let ofControl (control: Control<'msg>) : Widget<'msg> = Widget.ofControl control
