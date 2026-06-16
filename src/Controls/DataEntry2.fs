namespace FS.GG.UI.Controls

module DataEntry2 =

    let private onPayload eventKind (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun ev -> map (ev.Payload |> Option.defaultValue ""))

    module Cascader =
        let create attrs = Control.create "cascader" attrs
        let onChange map = onPayload "onChange" map

    module AutoComplete =
        let create attrs = Control.create "auto-complete" attrs
        let value value = Attr.value value
        let onChange map = onPayload "onChange" map

    module Upload =
        let create attrs = Control.create "upload" attrs
        let text value = Attr.text value
        let onChange map = onPayload "onChange" map
