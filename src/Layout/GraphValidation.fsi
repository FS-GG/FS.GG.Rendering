namespace FS.GG.UI.Layout

/// Public contract type exposed by this FS.GG.UI package.
type GraphValidationIssue =
    | DuplicateNodeId of string
    | MissingSource of edgeIndex: int * nodeId: string
    | MissingTarget of edgeIndex: int * nodeId: string
    | SelfLoop of edgeIndex: int * nodeId: string
    | CycleDetected of nodeIds: string list

/// Public contract module exposed by this FS.GG.UI package.
module GraphValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate : graph: GraphDefinition -> GraphValidationIssue list
    /// Public contract function exposed by this FS.GG.UI package.
    val hasCycle : graph: GraphDefinition -> bool
    /// Public contract function exposed by this FS.GG.UI package.
    val disconnectedComponents : graph: GraphDefinition -> string list list
