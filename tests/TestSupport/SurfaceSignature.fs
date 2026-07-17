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

    /// The ONE reader of a PUBLIC `val` declaration in a signature file (spec 255 / #695 convergence).
    ///
    /// `val NAME :`, at any indent, EXCLUDING `val internal` (a `(?!internal\b)` guard, not a blanklist),
    /// allowing an `inline` modifier, and — this is the part every ad-hoc copy got wrong at least once —
    /// allowing a trailing PRIME on the name. #598: "the member may end in a prime, and it must, or the
    /// rule invents violations." The mirror really ships one (`val checked'` in Controls), and a name class
    /// of `[a-z]\w*` (no prime) does not merely miss the name — it fails the whole match, because after
    /// `checked` comes `'`, not the `:` the regex then demands, so the declaration is INVISIBLE to the gate.
    /// That is a fail-OPEN hole, which is why this is the one reader and the ad-hoc `[a-z]\w*` copies fold
    /// onto it.
    ///
    /// Group `indent` is the leading whitespace — a column-0 `val` is illegal in F# (a `val` lives in a
    /// module or type), so in practice it is always non-empty; a caller that wants nested-only can still
    /// gate on its length. Group `name` is the identifier a call site spells.
    let publicValRegex =
        Regex(@"^(?<indent>\s*)val\s+(?!internal\b)(?:inline\s+)?(?<name>[a-z][A-Za-z0-9_]*'?)\s*:", RegexOptions.Compiled)

