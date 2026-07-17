namespace FS.GG.TestSupport

open System.Text.RegularExpressions

/// Reading an F# signature file the way the api-surface gates need to read it.
module SurfaceSignature =

    /// A DECLARATION, not a mention: start of line, optional indent, then `val`/`module`/`type` `internal`.
    ///
    /// `type` is in the list and it is not decoration. #585 is written in terms of `val internal` and
    /// `module internal` — those are what its table counts — but a `type internal` is the same defect and
    /// the WORSE one: `RuntimeStampResult<'msg>` shipped in the mirror as the return type of three
    /// `val internal` functions, so a gate that stripped only the functions would leave an ORPHAN type
    /// declared, unreferenced, and still uncallable. Anchoring on the issue's literal wording would have
    /// reported green over a live leak.
    ///
    /// Anchoring matters the other way too. The naive `grep -rc "val internal"` (#585's own reproduce line)
    /// also counts PROSE: the signatures legitimately mention `module internal RetainedRender` and `module
    /// internal Reconcile` inside `///` doc comments when explaining why a PUBLIC member behaves as it
    /// does. Those mentions must survive — a reader that eats them strips the explanation and leaves the
    /// member.
    ///
    /// Group 1 is the INDENT, and it has no reader today: it existed for `stripInternalDeclarations`, which
    /// measured a declaration's nesting from it to decide how much of what followed belonged to it. #753
    /// deleted that transform along with the mirror gates that were its only callers — the mirror is a build
    /// output now (#752), so nothing computes "src minus the internals" any more. The group is kept because
    /// S-INT's subject did not go away with it (see `ApiSurfaceInternalTests`): the generator has no
    /// internal filter, so a manifest line can still put an internal declaration in the mirror, and the next
    /// reader that must decide how much of one to consume will want the indent back.
    let internalDeclaration = Regex(@"^(\s*)(val|module|type)\s+internal\s")

