namespace FS.GG.UI.Controls

module Feedback2 =

    module Alert =
        let create attrs = Control.create "alert" attrs
        let text value = Attr.text value
        let onClose msg = Attr.on "onClose" msg

    module Result =
        let create attrs = Control.create "result" attrs
        let title value = Attr.text value

    module Drawer =
        let create attrs = Control.create "drawer" attrs
        let title value = Attr.text value
        let onClose msg = Attr.on "onClose" msg

    module Popover =
        let create attrs = Control.create "popover" attrs
        let text value = Attr.text value

    module Popconfirm =
        let create attrs = Control.create "popconfirm" attrs
        let text value = Attr.text value
        let onConfirm msg = Attr.on "onConfirm" msg
        let onCancel msg = Attr.on "onCancel" msg

    module Tour =
        let create attrs = Control.create "tour" attrs
        let text value = Attr.text value

    module FloatButton =
        let create attrs = Control.create "float-button" attrs
        let text value = Attr.text value
        let onClick msg = Attr.on "onClick" msg
