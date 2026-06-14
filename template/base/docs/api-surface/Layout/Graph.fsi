namespace FS.Skia.UI.Layout

open FS.Skia.UI.Scene

/// Public contract type exposed by this FS.Skia.UI package.
type GraphTarget =
    | Node of nodeId: string
    | Edge of edgeIndex: int

/// Public contract module exposed by this FS.Skia.UI package.
module Graph =
    /// Public contract function exposed by this FS.Skia.UI package.
    val layout : graph: GraphDefinition -> Result<GraphLayoutResult, GraphValidationIssue list>
    /// Public contract function exposed by this FS.Skia.UI package.
    val directed : graph: GraphDefinition -> Result<Scene, GraphValidationIssue list>
    /// Public contract function exposed by this FS.Skia.UI package.
    val undirected : graph: GraphDefinition -> Result<Scene, GraphValidationIssue list>
    /// Public contract function exposed by this FS.Skia.UI package.
    val hitTest : layout: GraphLayoutResult -> x: float -> y: float -> GraphTarget option
