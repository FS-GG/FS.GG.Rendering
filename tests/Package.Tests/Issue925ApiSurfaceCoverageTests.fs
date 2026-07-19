module Issue925ApiSurfaceCoverageTests

// #925 — the api-surface mirror gate's drift check is blind to an allowlist OMISSION: a public member the
// pin SHIPS that no `+` line teaches merged into the mirror untaught, green (that is how 0.13.0 shipped
// #911's key-wiring surface unteaching for the whole #912 window). `scripts/ApiSurfaceCoverage.fs` is the
// reconciliation that closes it, and it is a compiled `.fs` — `#load`ed by the generator, compiled in
// HERE — for the #720/#661 reason `FrozenMirrorVerdict.fs` is: the DECISION (which members are uncovered,
// which waivers rotted) is pure and needs no restore, so it must be tested rather than only exercised by a
// live gate. These tests are that verdict, plus the offline half of the wiring the gate depends on.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport
open FsGg.ApiSurface.Coverage

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

// A tiny synthetic surface — three public members in one package/source, none real, so the reconciliation
// is tested on its own terms and no restore is needed.
let private mAudit = key "FS.GG.UI.Scene" "SceneHost.fsi" "val" "GeneratedAppHost.auditKeyWiring"
let private mReach = key "FS.GG.UI.Scene" "SceneHost.fsi" "val" "GeneratedAppHost.reachableMessages"
let private mWiring = key "FS.GG.UI.Scene" "SceneHost.fsi" "type" "SceneHostKeyWiring`1"
let private universe = [ mAudit; mReach; mWiring ]

[<Tests>]
let coverageVerdictTests =
    testList
        "issue-925 api-surface coverage verdict"
        [ test "a member neither taught nor waived is reported UNTAUGHT — and named" {
              // The #911 shape: the pin ships it, nothing teaches it, nothing waives it. Silent under the
              // drift check; this is the gate that catches it, and the name is what makes it actionable.
              let v = reconcile universe Set.empty Set.empty
              Expect.equal v.Untaught universe "every member of the universe is untaught when nothing is taught or waived"
              Expect.isFalse (isClean v) "an untaught member is not a clean surface"
              Expect.stringContains (waiveLine mAudit) "GeneratedAppHost.auditKeyWiring" "the report names the member"
          }

          test "a TAUGHT member is not untaught" {
              let v = reconcile universe (Set.ofList [ mAudit; mReach; mWiring ]) Set.empty
              Expect.isEmpty v.Untaught "teaching every member accounts for the whole surface"
              Expect.isTrue (isClean v) "fully taught is clean"
          }

          test "a WAIVED member is not untaught" {
              // Waiving is the other half of the decision: 'we ship this and deliberately do not teach it.'
              let v = reconcile universe (Set.ofList [ mAudit ]) (Set.ofList [ mReach; mWiring ])
              Expect.isEmpty v.Untaught "taught ∪ waived covers the surface"
              Expect.isTrue (isClean v) "a mix of taught and waived is clean"
          }

          test "a partial split leaves exactly the undecided member untaught" {
              let v = reconcile universe (Set.ofList [ mAudit ]) (Set.ofList [ mReach ])
              Expect.equal v.Untaught [ mWiring ] "only the member that is neither taught nor waived is reported"
          }

          test "a waiver for a member the pin does NOT export is STALE" {
              // Fails closed (#266): a waiver that waives nothing must red, or the list accretes dead entries
              // the next real omission hides behind.
              let ghost = key "FS.GG.UI.Scene" "SceneHost.fsi" "val" "GeneratedAppHost.removedInThePin"
              let v = reconcile universe (Set.ofList [ mAudit; mReach; mWiring ]) (Set.ofList [ ghost ])
              Expect.equal v.StaleWaivers [ ghost ] "a waiver naming a non-exported member is stale"
              Expect.isFalse (isClean v) "a stale waiver is not clean"
          }

          test "a waiver for a member that is ALSO taught is STALE" {
              // Contradictory: you do not waive what you teach. Flag it rather than let both stand.
              let v = reconcile universe (Set.ofList [ mAudit; mReach; mWiring ]) (Set.ofList [ mAudit ])
              Expect.equal v.StaleWaivers [ mAudit ] "a waiver duplicating a taught member is stale"
          }

          test "reports are sorted, so a diff shows what entered or left the surface" {
              let v = reconcile [ mWiring; mAudit; mReach ] Set.empty Set.empty
              Expect.equal v.Untaught (List.sortBy (fun m -> m.Package, m.Source, m.Path, m.Kind) universe) "untaught is ordered"
          } ]

// -----------------------------------------------------------------------------------------------------
// publicMembers — the extraction decision, tested on synthetic trees. This is where the subtle bugs live
// (internal-subtree pruning, `and`-group inheritance), so it is here and not only behind a live restore.
// -----------------------------------------------------------------------------------------------------

let private d kind name internal_ children : Decl =
    { Kind = kind; Name = name; Internal = internal_; Children = children }

[<Tests>]
let publicMembersTests =
    testList
        "issue-925 public-member extraction"
        [ test "public type/val/and are keys; the module that holds them is NOT" {
              let tree =
                  [ d "module" "Paint" false [ d "val" "Paint.fill" false []; d "type" "Paint.Brush" false [] ] ]
              let members = publicMembers "Pkg" "F.fsi" tree
              Expect.equal
                  (members |> List.map (fun m -> m.Kind, m.Path))
                  [ ("val", "Paint.fill"); ("type", "Paint.Brush") ]
                  "the members are keyed; the module shell is not a member"
          }

          test "a `module internal` prunes its ENTIRE subtree — children carry no keyword of their own" {
              // The #Composition/#ControlInternals shape: the members' own lines are keyword-free, so a
              // per-node filter would wrongly demand waivers for internal surface no consumer can see.
              let tree =
                  [ d "val" "publicOne" false []
                    d "module" "Reconcile" true [ d "val" "Reconcile.step" false []; d "type" "Reconcile.State" false [] ] ]
              let paths = publicMembers "Pkg" "F.fsi" tree |> List.map (fun m -> m.Path)
              Expect.equal paths [ "publicOne" ] "only the public sibling survives; the whole internal module is pruned"
          }

          test "an `and` continuing an internal type-group is non-public, though its own line is bare" {
              // `type internal Foo = … and Bar = …`: parseFsi emits `and Bar` as a top-level SIBLING with no
              // modifier on its line. Accessibility is inherited from the group leader, so Bar is internal too.
              let tree =
                  [ d "type" "Foo" true []
                    d "and" "Bar" false []
                    d "type" "PublicType" false []
                    d "and" "PublicCont" false [] ]
              let paths = publicMembers "Pkg" "F.fsi" tree |> List.map (fun m -> m.Path)
              Expect.equal paths [ "PublicType"; "PublicCont" ] "the internal group (Foo/Bar) is excluded; the public group survives"
          }

          test "a `val` between type-groups resets the group — a later public `and` is not tainted" {
              let tree =
                  [ d "type" "Internal" true []
                    d "val" "aVal" false []
                    d "type" "Leader" false []
                    d "and" "Cont" false [] ]
              let paths = publicMembers "Pkg" "F.fsi" tree |> List.map (fun m -> m.Path)
              Expect.equal paths [ "aVal"; "Leader"; "Cont" ] "only the internal leader is dropped"
          }

          test "public members nested in a public module survive the descent" {
              let tree =
                  [ d "module" "Outer" false [ d "module" "Inner" false [ d "val" "Outer.Inner.go" false [] ] ] ]
              let paths = publicMembers "Pkg" "F.fsi" tree |> List.map (fun m -> m.Path)
              Expect.equal paths [ "Outer.Inner.go" ] "descent reaches an arbitrarily nested public member"
          } ]

// -----------------------------------------------------------------------------------------------------
// The offline half of the wiring the gate depends on. String assertions, like Feature541's: they catch
// the guard being deleted or unwired without a restore. The verdict itself is tested above, for real.
// -----------------------------------------------------------------------------------------------------

let private generator = File.ReadAllText(repositoryPath "scripts/refresh-api-surface-mirror.fsx")
let private manifest = File.ReadAllText(repositoryPath "scripts/api-surface-manifest.txt")
let private gate = File.ReadAllText(repositoryPath ".github/workflows/gate.yml")

[<Tests>]
let coverageWiringTests =
    testList
        "issue-925 api-surface coverage wiring"
        [ test "the generator loads the coverage module and calls reconcile" {
              Expect.stringContains generator "#load \"ApiSurfaceCoverage.fs\"" "the generator #loads the pure verdict"
              Expect.stringContains generator "Coverage.reconcile" "the generator calls the reconciliation"
          }

          test "the generator fails on untaught members and on stale waivers" {
              Expect.stringContains generator "coverage.Untaught" "it acts on the untaught set"
              Expect.stringContains generator "coverage.StaleWaivers" "it acts on the stale-waiver set"
          }

          test "the generator offers the --emit-waivers regeneration path" {
              Expect.stringContains generator "--emit-waivers" "the seed/regeneration path exists"
          }

          test "the gate runs the generator with --check (merge-blocking)" {
              Expect.stringContains gate "refresh-api-surface-mirror.fsx --check" "the required gate exercises the coverage check via --check"
          }

          test "the committed manifest actually carries waivers, and they are the current shape" {
              let waiveLines =
                  manifest.Replace("\r\n", "\n").Split('\n')
                  |> Array.filter (fun l -> l.StartsWith "waive ")
              Expect.isGreaterThan waiveLines.Length 0 "the manifest declares its deliberate omissions"
              // Every waive line is exactly `waive <pkg> <source> <kind> <path>` — four tokens after the verb.
              for l in waiveLines do
                  let parts = Regex.Split(l.Trim(), @"\s+")
                  Expect.equal parts.Length 5 (sprintf "well-formed waiver (verb + 4 tokens): %s" l)
          }

          test "no duplicate waivers — a dup is dead weight the parser silently collapses" {
              let waiveLines =
                  manifest.Replace("\r\n", "\n").Split('\n')
                  |> Array.filter (fun l -> l.StartsWith "waive ")
                  |> Array.map (fun l -> l.Trim())
              let dups =
                  waiveLines
                  |> Array.countBy id
                  |> Array.filter (fun (_, n) -> n > 1)
                  |> Array.map fst
              Expect.isEmpty dups (sprintf "duplicate waiver line(s): %A" dups)
          } ]
