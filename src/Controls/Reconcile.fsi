namespace FS.Skia.UI.Controls

/// Internal keyed VDOM diff over the lowered `Control<'msg>` IR. Pure, total,
/// deterministic; not wired into the render path (feature 067, internal only).
///
/// This module is `internal` — assembly-internal accessibility, genuinely
/// unreachable from package consumers (matches the `module internal SceneRenderer`
/// precedent). It is deliberately NOT added to the Controls capability `contracts:`
/// list, so `ApiSurfaceGen`/`PackageSurfaceCheck` emit no public-surface entry
/// (FR-002, SC-005 — zero baseline delta). The Expecto/FsCheck property tests reach
/// `diff`/`apply` via `[<assembly: InternalsVisibleTo("Controls.Tests")>]`.
module internal Reconcile =

    /// "Field unchanged" vs "field set to this value" — avoids `'a option option`
    /// when a `Content`/`Accessibility` field is set to `None`.
    type FieldChange<'a> =
        | Unchanged
        | ChangedTo of 'a

    /// Attribute-level change, matched by `Attr.Name` (FR-007). The emitted list is
    /// sorted by `Name` for deterministic output (FR-009).
    type AttrChange<'msg> =
        | AttrSet of Attr<'msg>
        | AttrRemoved of name: string

    /// One node's diff. FR-004 operation set; `Update` recurses into children (FR-005).
    [<RequireQualifiedAccess>]
    type NodePatch<'msg> =
        | Keep
        | Replace of Control<'msg>
        | Update of UpdatePatch<'msg>

    /// Targeted in-place change for a matched same-`Kind` node.
    and UpdatePatch<'msg> =
        { AttrChanges: AttrChange<'msg> list
          ContentChange: FieldChange<string option>
          AccessibilityChange: FieldChange<AccessibilityMetadata option>
          Children: ChildOp<'msg> list }

    /// Ordered child operation. `ChildKeep`/`ChildMove` carry the matched child's
    /// **prev**-list index as their source; the child's position in the next list is
    /// its ordinal among the non-`ChildRemove` ops (which are emitted in next order).
    /// `ChildInsert.index` and `ChildMove.toIndex` are next-list positions;
    /// `ChildRemove.index` is the prev-list position. FR-004.
    and ChildOp<'msg> =
        | ChildKeep of index: int * patch: NodePatch<'msg>
        | ChildMove of fromIndex: int * toIndex: int * patch: NodePatch<'msg>
        | ChildInsert of index: int * node: Control<'msg>
        | ChildRemove of key: ControlId option * index: int

    /// `diff` result: the patch plus any diagnostics (e.g. duplicate-key
    /// `KeyCollision`). The function is total and never throws (FR-011, SC-007).
    type ReconcileResult<'msg> =
        { Patch: NodePatch<'msg>
          Diagnostics: ControlDiagnostic list }

    /// Pure, total, deterministic diff from a previous to a next `Control<'msg>`
    /// tree. Children match by `Key` first, then unkeyed residuals positionally
    /// (FR-003/FR-010). A `Kind` mismatch on a matched pair yields a whole-subtree
    /// replace (FR-006). FR-001/FR-009.
    val diff: prev: Control<'msg> -> next: Control<'msg> -> ReconcileResult<'msg>

    /// Apply a patch produced by `diff prev _` back onto `prev`, reconstructing a
    /// tree structurally equal to `next` (up to attribute ordering, which the diff
    /// canonicalizes by `Name`). Exists to prove the round-trip invariant
    /// (FR-008/SC-002); pure.
    val apply: prev: Control<'msg> -> patch: NodePatch<'msg> -> Control<'msg>
