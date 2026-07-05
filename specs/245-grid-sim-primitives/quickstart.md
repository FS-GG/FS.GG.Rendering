# Quickstart — Validate Grid Simulation Primitives

Proves the two helpers work end-to-end the way a grid-game consumer uses them. Prerequisites: repo builds (`dotnet build`), .NET `net10.0`.

## 1. FSI consumer smoke (run before the property suite — Foundational phase)

An FSI transcript that loads the packed Canvas surface and drives each helper as a consumer would. Expected: a routed path across a walled grid, and a splash query returning the near items.

```fsharp
// scripts/grid-sim-prelude.fsx — exercises the .fsi the way a consumer would
open FS.GG.UI.Canvas
open FS.GG.UI.Scene

// A 5x5 grid with a wall column at Col=2 except a gap at Row=4.
let blocked = set [ for r in 0..3 -> (2, r) ]
let walkable (c: Cell) =
    c.Col >= 0 && c.Col <= 4 && c.Row >= 0 && c.Row <= 4
    && not (blocked.Contains(c.Col, c.Row))

let path = Pathfinding.astar EightWay 1000 walkable { Col = 0; Row = 0 } { Col = 4; Row = 0 }
// Expected: Some [ ... ] routing DOWN to the Row=4 gap, across, and back UP — never through Col=2 rows 0-3.
printfn "path = %A" path
// Determinism: identical call must be byte-identical.
printfn "deterministic = %b" (path = Pathfinding.astar EightWay 1000 walkable { Col = 0; Row = 0 } { Col = 4; Row = 0 })

// Splash query: bucket some enemies, ask who is within radius 3 of a blast at (10,10).
let enemies = [ { X = 10.0; Y = 11.0 }, "a"; { X = 20.0; Y = 20.0 }, "b"; { X = 12.0; Y = 9.0 }, "c" ]
let grid = SpatialGrid.build 4.0 enemies
printfn "splash = %A" (SpatialGrid.queryRadius { X = 10.0; Y = 10.0 } 3.0 grid)   // Expected: ["a"; "c"] in insertion order
printfn "rect   = %A" (SpatialGrid.query { X = 0.0; Y = 0.0; Width = 15.0; Height = 15.0 } grid)  // Expected: ["a"; "c"]
```

Run: `dotnet fsi scripts/grid-sim-prelude.fsx` (after a build so the DLLs exist).

## 2. Property + example tests

```sh
dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj
```

Covers (see `data-model.md` invariants):

- **Correctness / optimality (INV-2)**: hand-computed shortest paths on small grids for `FourWay`/`EightWay`, `astar` and `bfs`; corner-cut refusal; endpoint inclusion; `start=goal`.
- **No-path & bounds (FR-005)**: walled-off goal → `None`; blocked endpoints → `None`; `maxVisited` cap honoured.
- **Determinism (INV-1)**: FsCheck — for random grids/queries, repeat calls return byte-identical results; and multiple equal-cost routes still yield one stable path.
- **SpatialGrid exactness (INV-3)**: FsCheck — `query`/`queryRadius` equal a brute-force filter over the same items; degenerate `cellSize`/`radius` totals.

## 3. Surface + additivity gates

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj   # SurfaceAreaTests: baseline matches, additive-only
```

Expected new baseline lines in `readiness/surface-baselines/FS.GG.UI.Canvas.txt`:
`FS.GG.UI.Canvas.Cell`, `FS.GG.UI.Canvas.Neighbourhood` (+ `+FourWay`/`+EightWay`), `FS.GG.UI.Canvas.Pathfinding`, `FS.GG.UI.Canvas.SpatialGrid\`1`. No existing line removed or changed (additivity).

## 4. Done-when

- FSI smoke prints the expected path + splash results.
- `Canvas.Tests` and `Package.Tests` green.
- Baseline regenerated & committed; product-skill (`fs-gg-game-core`) grid guidance points at the real API.
- (Release, separate authorized step) coherent-set bump + registry/compatibility flip (publish-before-flip).
