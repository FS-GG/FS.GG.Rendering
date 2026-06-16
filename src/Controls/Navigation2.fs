namespace FS.GG.UI.Controls

module Navigation2 =

    let private onPayload eventKind (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun ev -> map (ev.Payload |> Option.defaultValue ""))

    module Breadcrumb =
        let create attrs = Control.create "breadcrumb" attrs

    module Steps =
        let create attrs = Control.create "steps" attrs

    module Pagination =
        let create attrs = Control.create "pagination" attrs
        let total pages = Attr.create "value" Content (FloatValue(float pages))
        let onChange map = onPayload "onChange" map

    module Segmented =
        let create attrs = Control.create "segmented" attrs
        let onChange map = onPayload "onChange" map

    module Anchor =
        let create attrs = Control.create "anchor" attrs

    module Affix =
        let create attrs = Control.create "affix" attrs
        let text value = Attr.text value
