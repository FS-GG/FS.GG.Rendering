namespace FS.GG.UI.Controls

module Display2 =

    module Tag =
        let create attrs = Control.create "tag" attrs
        let text value = Attr.text value
        let onClose msg = Attr.on "onClose" msg

    module Avatar =
        let create attrs = Control.create "avatar" attrs
        let text value = Attr.text value

    module Card =
        let create attrs = Control.create "card" attrs
        let title value = Attr.text value

    module Descriptions =
        let create attrs = Control.create "descriptions" attrs

    module Statistic =
        let create attrs = Control.create "statistic" attrs
        let value value = Attr.value value

    module Timeline =
        let create attrs = Control.create "timeline" attrs

    module Empty =
        let create attrs = Control.create "empty" attrs
        let text value = Attr.text value

    module Skeleton =
        let create attrs = Control.create "skeleton" attrs

    module QrCode =
        let create attrs = Control.create "qr-code" attrs
        let value value = Attr.text value

    module Watermark =
        let create attrs = Control.create "watermark" attrs
        let text value = Attr.text value
