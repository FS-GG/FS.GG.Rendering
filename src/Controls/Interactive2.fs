namespace FS.GG.UI.Controls

module Interactive2 =

    let private onPayload eventKind (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun ev -> map (ev.Payload |> Option.defaultValue ""))

    module Collapse =
        let create attrs = Control.create "collapse" attrs
        let onChange map = onPayload "onChange" map

    module Rate =
        let create attrs = Control.create "rate" attrs
        let value stars = Attr.create "value" Content (FloatValue stars)
        let onChange map = onPayload "onChange" map

    module Carousel =
        let create attrs = Control.create "carousel" attrs

    module Calendar =
        let create attrs = Control.create "calendar" attrs
        let onChange map = onPayload "onChange" map
