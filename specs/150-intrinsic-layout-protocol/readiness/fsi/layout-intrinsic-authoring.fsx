#r "nuget: FS.GG.UI.Layout, 0.1.12-preview.1"
#r "nuget: FS.GG.UI.Controls, 0.1.12-preview.1"
#r "nuget: FS.GG.UI.Controls.Elmish, 0.1.12-preview.1"
#r "nuget: FS.GG.UI.Testing, 0.1.12-preview.1"

open FS.GG.UI.Layout
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Testing

let node = Defaults.layoutNode "sample"
let constraints = Layout.constraints Viewport 0.0 (Some 320.0) 0.0 (Some 240.0)
let inputKey = Layout.layoutInputKey node
let query = Layout.intrinsicQuery node.Id IntrinsicMaxHeight (Some 320.0) inputKey DiagnosticProbe
let readinessStatus = LayoutReadiness.statusText LayoutReadinessAccepted

printfn "%s %s %s" constraints.NormalizedIdentity query.QueryIdentity readinessStatus

