// #925 — THE ALLOWLIST-OMISSION HALF OF THE API-SURFACE MIRROR GATE, SPLIT OUT SO IT IS TESTED.
//
// `scripts/refresh-api-surface-mirror.fsx` regenerates `(pinned .fsi) ∩ api-surface-manifest.txt` and
// `gate.yml` fails on any diff against the committed mirror. That drift check catches a mirror that was
// hand-edited or that drifted from the pin — but it is blind in one direction: a public member the pin
// SHIPS that is simply ABSENT from the manifest allowlist. The regenerated `(surface ∩ manifest)` and the
// committed mirror still agree, so there is no drift, so the gate is green — while a shipped public
// surface is taught nowhere in the scaffold. That is exactly how 0.13.0 shipped #911's key-wiring surface
// (`auditKeyWiring` / `runKeyScriptToModel` / `reachableMessages`, `SceneHostKeyWiring<'msg>`) with the
// mirror silently omitting it and #912 reading as "mirror can't teach it" for the whole window (#924
// fixed the omission by hand; nothing would have flagged it).
//
// This module is the reconciliation that closes that direction, and it lives HERE — a compiled `.fs`,
// `#load`ed by the generator AND compiled into `tests/Package.Tests` — for the #720/#661 reason
// `FrozenMirrorVerdict.fs` does: the DECISION (which members are uncovered, which waivers have rotted) is
// pure and needs neither the network nor a restore to test, so it must not sit untested inside an `.fsx`
// that only a live gate ever exercises. The generator supplies the surface (it restores the pin); this
// says whether that surface is fully accounted for.
//
// WHY A WAIVER LIST, AND WHY PER-MEMBER. The mirror is a CURATED teaching document — it teaches roughly
// half the pin's members by design (`docs/scaffold-map.md` sanctions the omissions) — so "every public
// member must be taught" is the wrong rule; it would demand the mirror teach its whole surface. The rule
// is "every public member must be a DECISION": taught, or explicitly WAIVED in the manifest, never
// omitted by silence. The waiver is per-member and NOT a wildcard on purpose: a `waive Module.*` would
// swallow the next member added under `Module` too, which is the precise "untaught by silence" this gate
// exists to prevent. So a new member is unwaived by default and forces a maintainer to decide.
//
// FAILS CLOSED (#266). A waiver that waives nothing — its member was removed or renamed in the pin, or is
// now taught — is a HARD error, not a shrug: a dead waiver list silently accretes entries, and the next
// real omission hides among them. Same posture the generator takes on a manifest line naming a member the
// pin does not export.

namespace FsGg.ApiSurface

module Coverage =

    /// One public declaration, addressed the way both the pin surface and the manifest address it:
    /// the package that ships it, the pin's `.fsi` file it lives in, its F# kind (`type`/`val`/`and`),
    /// and the dotted path within that file (`Paint.fill`). This is the join key between the three sets.
    type MemberKey =
        { Package: string
          Source: string
          Kind: string
          Path: string }

    /// Build a key. A function, not a record literal at the call sites, because the generator's own
    /// `Include` record shares `Source`/`Kind`/`Path` — an unqualified `{ Source = … }` there binds to
    /// `Include`, so a helper defined HERE (where there is no such collision) is what keeps the callers clean.
    let key package source kind path =
        { Package = package
          Source = source
          Kind = kind
          Path = path }

    /// A declaration projected from the pin's parse tree — just what coverage needs: its F# kind, its
    /// dotted name within the file, whether its OWN line declared it `internal`/`private`, and (for a
    /// module) its children. The generator projects `FsiSurface.Node` onto this; keeping the decision that
    /// consumes it here — rather than in the `.fsx` — is what makes it testable without a package restore.
    type Decl =
        { Kind: string
          Name: string
          Internal: bool
          Children: Decl list }

    /// The public `type`/`val`/`and` members a source file exports, as coverage keys. Two things make this
    /// more than a filter, and both are why it is tested:
    ///
    ///   * ACCESSIBILITY IS INHERITED. A `val` inside a `module internal` carries no modifier on its own
    ///     line, and neither does `and Bar` continuing a `type internal Foo = … and Bar = …` group — yet
    ///     both are non-public. Descending only through public modules prunes the internal subtree at its
    ///     root; carrying the group leader's accessibility across its `and` continuations catches the other.
    ///   * MODULES ARE NOT KEYS. They are structural shells the generator re-emits around whatever members
    ///     are taught, so accounting for the members accounts for the module.
    let publicMembers (package: string) (source: string) (decls: Decl list) : MemberKey list =
        let rec collect (decls: Decl list) =
            let acc = ResizeArray<MemberKey>()
            // The accessibility of the current mutually-recursive group's LEADER. An `and` inherits it; any
            // other declaration starts fresh (a group cannot span a `val`/`module` in valid F#).
            let mutable groupPublic = true

            for d in decls do
                match d.Kind with
                | "type" ->
                    groupPublic <- not d.Internal
                    if groupPublic then acc.Add(key package source d.Kind d.Name)
                | "and" ->
                    if groupPublic && not d.Internal then
                        acc.Add(key package source d.Kind d.Name)
                | "val" ->
                    groupPublic <- true
                    if not d.Internal then acc.Add(key package source d.Kind d.Name)
                | "module" ->
                    groupPublic <- true
                    if not d.Internal then acc.AddRange(collect d.Children)
                | _ -> ()

            List.ofSeq acc

        collect decls

    /// The reconciliation verdict. Both lists EMPTY is the only pass.
    type Verdict =
        { /// Public members the pin exports that are neither taught nor waived — the gap #925 closes.
          Untaught: MemberKey list
          /// Waivers that no longer waive anything: their member is absent from the pin's current public
          /// surface (removed/renamed), or it is now TAUGHT. A rotted waiver must red, or the list
          /// accretes dead entries the next real omission can hide behind.
          StaleWaivers: MemberKey list }

    let private ordered (keys: MemberKey seq) =
        keys
        |> Seq.sortBy (fun m -> m.Package, m.Source, m.Path, m.Kind)
        |> Seq.toList

    /// Reconcile the pin's public surface against the manifest's teach + waive decisions.
    ///
    /// `universe` is every public `type`/`val`/`and` the pinned packages export (the same surface the
    /// generator reads, filtered to public — modules are structural containers, accounted for by the
    /// members inside them). `taught` is the manifest's `+` includes; `waived` its `waive` lines.
    let reconcile (universe: MemberKey list) (taught: Set<MemberKey>) (waived: Set<MemberKey>) : Verdict =
        let universeSet = Set.ofList universe

        let untaught =
            universe
            |> List.filter (fun m -> not (taught.Contains m) && not (waived.Contains m))

        let staleWaivers =
            waived
            |> Set.filter (fun w -> not (universeSet.Contains w) || taught.Contains w)

        { Untaught = ordered untaught
          StaleWaivers = ordered staleWaivers }

    /// True when the pin's public surface is fully accounted for: every member taught or waived, and no
    /// waiver rotted.
    let isClean (v: Verdict) = v.Untaught.IsEmpty && v.StaleWaivers.IsEmpty

    /// The manifest line that would waive a member — the token order the generator's `+` includes use
    /// (`waive <pkg> <source> <kind> <path>`), so a maintainer can paste an `Untaught` report straight in.
    let waiveLine (m: MemberKey) = sprintf "waive %s %s %s %s" m.Package m.Source m.Kind m.Path

    // #984 — THE PROFILE-COMPLETENESS HALF: a game-profile module may not be ENTIRELY waived.
    //
    // The #925 reconcile above closes "untaught by silence" MEMBER by member — every member taught or
    // waived — but a maintainer can satisfy it while dropping a whole MODULE, by waiving every one of its
    // members. That is exactly how the scaffold shipped for the whole pre-#984 window: `Effects`,
    // `Ballistics`, `Dice`, `Los`, `Fov`, `Ai`/`Difficulty`, `Visibility` and `Scene.Animation` were each
    // waived member-for-member, so a game scaffold carried NONE of the surface a game needs, and every gate
    // was green — the reconcile because each member WAS a decision, the drift check because a fully-waived
    // module and its (absent) mirror agree. This closes the module direction: a declared game-profile
    // module must teach at least one member, so re-waiving the last one of them REDS here.
    //
    // Addressed by (package, source .fsi) — the whole module, not a member — because "the module is
    // vendored at all" is the property, and a per-member list would just restate the waiver block. FAILS
    // CLOSED (#266) the same way the stale-waiver check does: a declared module the pin ships no public
    // member for at all is a ROTTED declaration (the module left the pin, or was renamed), reported so the
    // list cannot decay into a vacuous green the way #259's unstamped Game.Core mirror did.
    type ProfileModule = { Package: string; Source: string }

    /// A declared game-profile module that is NOT fully vendored, with the reason. Empty is the only pass.
    type ProfileGap =
        { Package: string
          Source: string
          /// `AllWaived` — the pin ships public members but the manifest teaches none of them (the #984
          /// regression). `Vanished` — the pin ships no public member under this source at all, so the
          /// declaration has rotted and must be removed or corrected.
          Reason: string }

    /// The game-profile modules a scaffolded product must vendor in FULL — at least one taught member each,
    /// never entirely waived (#984). These are the modules a game/sample-pack profile reaches for, that the
    /// pre-#984 manifest had dropped wholesale. Editing this list is the deliberate act that adds or retires
    /// a completeness guarantee; the reconcile below turns it into a merge-blocking check.
    let gameProfileModules: ProfileModule list =
        [ { Package = "FS.GG.Game.Core"; Source = "Ai.fsi" }
          { Package = "FS.GG.Game.Core"; Source = "Ballistics.fsi" }
          { Package = "FS.GG.Game.Core"; Source = "Dice.fsi" }
          { Package = "FS.GG.Game.Core"; Source = "Effects.fsi" }
          { Package = "FS.GG.Game.Core"; Source = "Fov.fsi" }
          { Package = "FS.GG.Game.Core"; Source = "Los.fsi" }
          { Package = "FS.GG.Game.Core"; Source = "Visibility.fsi" }
          { Package = "FS.GG.UI.Scene"; Source = "Animation.fsi" } ]

    /// Reconcile the declared game-profile modules against the pin's surface and the manifest's taught set:
    /// each declared module must have at least one public member the manifest teaches. Reported sorted so a
    /// diff shows what regressed.
    let profileGaps (universe: MemberKey list) (taught: Set<MemberKey>) (declared: ProfileModule list) : ProfileGap list =
        let bySource =
            universe
            |> List.groupBy (fun m -> m.Package, m.Source)
            |> Map.ofList

        declared
        |> List.choose (fun m ->
            match bySource |> Map.tryFind (m.Package, m.Source) with
            | None -> Some { Package = m.Package; Source = m.Source; Reason = "Vanished" }
            | Some members when members |> List.exists taught.Contains -> None
            | Some _ -> Some { Package = m.Package; Source = m.Source; Reason = "AllWaived" })
        |> List.sortBy (fun g -> g.Package, g.Source)

    /// One `+` include of a `file` stanza, addressed the way the manifest writes it — the `.fsi` it draws
    /// from, the F# kind, and the dotted path within that file.
    type Include = { Source: string; Kind: string; Path: string }

    /// The `+` includes a single `file` stanza REPEATS. The generator emits each `+` line's member verbatim,
    /// in list order, so a member named twice in ONE stanza is RENDERED twice — the `Pathfinding.fsi`
    /// `Step`/`Reach` TRIPLICATION that TowerDefense1#4 saw was exactly this: three `+ type Pathfinding.Step`
    /// / `+ type Pathfinding.Reach` lines in the manifest, three copies in the mirror. Nothing caught it: the
    /// #925 coverage `taughtSet` is a `Set`, so the repeat COLLAPSES there and the reconcile stays green, and
    /// the drift check compares the generator's (tripled) output against the committed (tripled) mirror and
    /// agrees with itself. A member repeated inside one stanza is malformed, always — a type has one
    /// declaration — so the generator fails closed on it (#266), and this is the pure decision that says so,
    /// kept here rather than in the `.fsx` so it is tested without a package restore (the #925/#661 rule).
    ///
    /// Cross-stanza repeats are NOT duplicates: the same member legitimately taught in two mirror FILES is
    /// two renders in two files, which is a curation choice, not a triplication. This is per-stanza only.
    let duplicateIncludes (includes: Include list) : Include list =
        includes
        |> List.countBy id
        |> List.filter (fun (_, n) -> n > 1)
        |> List.map fst
        |> List.sortBy (fun i -> i.Source, i.Path, i.Kind)
