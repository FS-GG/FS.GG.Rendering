// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/MapGen.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A dense, row-major rectangular grid of `'T`, addressed by the shared `Cell` (`{ Col; Row }`). The
/// backing `Cells` array is indexed `Row * Width + Col`; a grid is an immutable value the generators
/// build and return. `TileMap` is the wall/floor specialisation; the generic `Grid<'T>` also carries the
/// integer height fields and classified maps the noise/scatter family produces (`Grid<int>`, `Grid<'a>`).
/// Structural equality (element-wise over `Cells`) is the byte-identity a seeded generator is tested on,
/// so the same seed compares equal across runs and platforms.
type Grid<'T> =
    { Width: int
      Height: int
      Cells: 'T[] }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The base tile classification every wall/floor generator emits. A domain game maps it onto its own
/// richer tiles at the boundary (as `Ai` keeps `'T` = your facts). Structural equality makes a generated
/// `TileMap` a deterministic golden-testable value.
[<Struct>]
type Tile =
    | Wall
    | Floor

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A wall/floor map — the `Grid<Tile>` the cellular-automata, BSP, and maze families produce and the
/// connectivity toolkit operates over.
type TileMap = Grid<Tile>

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A connected component of `Floor` cells: a stable integer `Id` and its `Cells` in row-major order.
/// `Id` is assigned by the row-major scan order of each region's first cell, so the labelling is
/// independent of the seed that produced the map — a region's identity is a function of the map, not of
/// generation history. Structural equality makes a region set golden-testable.
type Region =
    { Id: int
      Cells: Cell[] }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Parameters for the cellular-automata cave generator. `WallChance` is the initial random-fill wall
/// density (clamped to `[0,1]`); `SmoothingPasses` is how many 4-5 smoothing iterations to run (clamped
/// to `>= 0`); `Neighbourhood` governs the final cavern's connectivity/region check (the CA smoothing
/// itself is always the 8-cell Moore neighbourhood the 4-5 rule is defined over).
type CaveParams =
    { WallChance: float
      SmoothingPasses: int
      Neighbourhood: Neighbourhood }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Parameters for the BSP room-and-corridor dungeon generator. `MinLeaf` is the smallest a partition
/// leaf may be along an axis (clamped `>= 1`); `MaxLeaf` is the size above which a node is split
/// (clamped `>= MinLeaf`); `RoomPadding` is the margin left inside each leaf before its room (clamped
/// `>= 0`).
type BspParams =
    { MinLeaf: int
      MaxLeaf: int
      RoomPadding: int }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// One room of a BSP dungeon: a stable integer `Id` (ascending in leaf-traversal order) and its `Bounds`
/// as a `Rect` holding integer cell values (`X`=col, `Y`=row, `Width`/`Height`=cell extents), the same
/// cell-space `Rect` `Grids.cellRect` produces.
type Room = { Id: int; Bounds: Rect }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The structural metadata of a BSP dungeon: every `Room` (in ascending `Id` order) and the `Corridors`
/// as room-id pairs a corridor joins. A game places its start/exit/loot on this graph; it is a
/// deterministically-ordered value, byte-identical for a seed.
type RoomGraph =
    { Rooms: Room[]
      Corridors: (int * int)[] }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The role of a room in a branching-walk floor (M4, roguelike §4.8). `Start` is the origin; `Boss`/
/// `Treasure`/`Shop`/`Secret` are the special rooms assigned to dead-ends; `Normal` is everything else.
type RoomKind =
    | Normal
    | Start
    | Boss
    | Treasure
    | Shop
    | Secret

/// Public contract type exposed by the FS.GG.Game.Core package.
/// One room of a branching-walk floor: its grid `Cell`, its `Kind`, and an `Rng`-drawn `TemplateId` a game
/// uses to populate the room interior. Doors are not a field — they are implied by `FloorLayout.Adjacency`
/// (a door is a shared edge between two adjacent rooms).
type FloorRoom =
    { Cell: Cell
      Kind: RoomKind
      TemplateId: int }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A branching-walk floor as a graph of rooms: every `FloorRoom` (in placement order, Start first) and the
/// `Adjacency` as the 4-adjacent room-cell pairs (sorted, each edge once). Byte-identical for a seed.
type FloorLayout =
    { Rooms: FloorRoom[]
      Adjacency: (Cell * Cell)[] }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Parameters for the branching-walk floor generator. `RoomCount` is the target room count (clamped to
/// `[1, MaxRooms]`); `MaxRooms` is the hard cap (clamped `>= 1`); `SpecialRooms` is the ordered list of
/// special kinds to assign to dead-ends, farthest-first (extras that do not fit are omitted).
type FloorParams =
    { RoomCount: int
      MaxRooms: int
      SpecialRooms: RoomKind list }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Parameters for the fractal value-noise height field (M5). `Octaves` is how many doubling-frequency
/// layers to sum (clamped `>= 1`); `Frequency` is the base lattice frequency in cells⁻¹ (a non-finite or
/// non-positive value falls back to `0.1`); `Persistence` is the amplitude falloff per octave (clamped to
/// `[0,1]`).
type NoiseParams =
    { Octaves: int
      Frequency: float
      Persistence: float }

/// Public contract module exposed by the FS.GG.Game.Core package.
/// The shared substrate every map generator builds on: a dense `Grid<'T>` container, total addressing,
/// and a flood-fill connectivity toolkit. Everything here is pure, total, and byte-deterministic — the
/// `Rng` is threaded explicitly, iteration is row-major, and region ids are fixed by row-major position,
/// so no hash-set enumeration or draw order can leak into the output. Milestone M1 of the procedural map
/// generation design; the family generators (caves, BSP dungeons, room-graph floors, maze/noise/scatter)
/// layer on top of it.
[<RequireQualifiedAccess>]
module MapGen =

    /// A `width * height` grid with every cell set to `value`. A non-positive dimension is clamped to 0,
    /// so `filled 0 h v` / `filled w 0 v` is a valid empty grid (empty `Cells`) rather than an error.
    val filled: width: int -> height: int -> value: 'T -> Grid<'T>

    /// Whether `cell` addresses a cell inside `grid` (`0 <= Col < Width`, `0 <= Row < Height`).
    val inBounds: grid: Grid<'T> -> cell: Cell -> bool

    /// The value at `cell`, or `ValueNone` when `cell` is out of bounds. Total.
    val get: grid: Grid<'T> -> cell: Cell -> 'T voption

    /// A copy of `grid` with `cell` set to `value`. An out-of-bounds `cell` is a no-op — the grid is
    /// returned unchanged. Total; copy-on-write, so the input is never mutated.
    val set: grid: Grid<'T> -> cell: Cell -> value: 'T -> Grid<'T>

    /// Flood-fill the `Floor` cells of `map` into connected components over `neighbourhood`. Each
    /// `Region` carries its `Cells` in row-major order and an `Id` ascending by the row-major position of
    /// its first cell — the same labelling regardless of the seed that produced `map`. Wall cells and an
    /// empty grid yield `[]`.
    val regions: neighbourhood: Neighbourhood -> map: TileMap -> Region list

    /// The largest `Floor` region of `map` under `neighbourhood` (most cells; ties broken by lowest
    /// `Id`), or `ValueNone` when `map` has no `Floor`.
    val largestRegion: neighbourhood: Neighbourhood -> map: TileMap -> Region voption

    /// Carve `Floor` L-corridors joining every `Floor` region of `map` to the largest, so the result is
    /// a single connected floor under `neighbourhood`. Regions are joined in ascending `Id` order; each
    /// is connected by the nearest cell pair to the target (minimum squared `Cell` distance, ties broken
    /// by the smallest `(fromCell, toCell)` pair) carved as an axis-then-axis L. Threads the `Rng`
    /// through (reserved for corridor jitter; deterministic without it today). A map with 0 or 1 region
    /// is returned unchanged.
    val connect: neighbourhood: Neighbourhood -> rng: Rng -> map: TileMap -> struct (TileMap * Rng)

    /// Generate an organic cave `TileMap` of `width * height` (M2). Random-fills the interior at
    /// `CaveParams.WallChance`, runs `CaveParams.SmoothingPasses` iterations of the 4-5 Moore rule (a cell
    /// becomes `Wall` when at least 5 of its 8 neighbours — out of bounds counted as wall — are wall),
    /// forces a solid `Wall` border, then `connect`s the result into a single traversable cavern under
    /// `CaveParams.Neighbourhood`. Threads the `Rng`; byte-identical for a seed. Total: a non-positive
    /// dimension is an empty grid; `WallChance` clamps to `[0,1]`, `SmoothingPasses` to `>= 0`; never throws.
    val caves: width: int -> height: int -> parameters: CaveParams -> rng: Rng -> struct (TileMap * Rng)

    /// Generate a structured room-and-corridor dungeon of `width * height` (M3). Recursively partitions the
    /// rectangle (BSP) — splitting the longer dimension when it exceeds `BspParams.MaxLeaf`, the split
    /// position drawn from the `Rng` — down to leaves, places one room per leaf inset by
    /// `BspParams.RoomPadding`, joins sibling subtrees with L-corridors, and applies `connect` as a
    /// connectivity safety net. Returns the carved `TileMap`, a `RoomGraph` of the rooms (ascending `Id` by
    /// leaf-traversal order) and the corridor room-id pairs, and the threaded `Rng` — all byte-identical for
    /// a seed. Total: a non-positive dimension or impossible params (min > max) yield an empty `TileMap` and
    /// empty `RoomGraph`; never throws.
    val bspDungeon:
        width: int -> height: int -> parameters: BspParams -> rng: Rng -> struct (TileMap * RoomGraph * Rng)

    /// Generate an Isaac-style floor as a graph of rooms (M4, roguelike §4.8). A branching random walk from
    /// a `Start` room at the origin places up to `FloorParams.RoomCount` rooms (never on an occupied cell,
    /// never where a candidate would touch two or more placed rooms — so the adjacency graph stays
    /// connected), draws a `TemplateId` per room, and assigns `FloorParams.SpecialRooms` to the farthest
    /// dead-ends (by graph distance from `Start`, extras omitted). Returns the `FloorLayout` (rooms in
    /// placement order, adjacency sorted) and the threaded `Rng`; byte-identical for a seed. Total:
    /// `RoomCount <= 0` yields a single `Start` room; never throws.
    val floorLayout: parameters: FloorParams -> rng: Rng -> struct (FloorLayout * Rng)

    /// Derive a floor's seed from the run seed and floor index (M4). Distinct `floorIndex` values give
    /// well-separated seeds (via the module's splitmix mixing), so each floor of a run is reproducible yet
    /// independent — feed the result to `Rng.ofSeed` for that floor's `floorLayout`.
    val floorSeed: runSeed: uint64 -> floorIndex: int -> uint64

    /// Generate a recursive-backtracker perfect maze of `width * height` tiles (M5). Maze cells sit on the
    /// odd lattice with wall tiles between; the carved passages form a single `bfs`-traversable floor with a
    /// `Wall` border. Threads the `Rng`; byte-identical for a seed. Total: a dimension `< 3` yields an
    /// all-`Wall` map; never throws.
    val maze: width: int -> height: int -> rng: Rng -> struct (TileMap * Rng)

    /// Generate a `width * height` fractal value-noise height field (M5) — the sandbox-survival worldgen
    /// first stage. Sums `NoiseParams.Octaves` layers of hashed-lattice value noise (smootherstep-
    /// interpolated) at doubling frequency and `Persistence` amplitude, normalised and scaled to a `Grid<int>`
    /// of heights in `[0, 255]`. The noise salt is drawn from `rng` — pass a `Rng.split` child to keep the
    /// noise stream separate from a simulation `Rng`. Byte-identical for a seed. Total: a non-positive
    /// dimension is an empty grid; never throws.
    val heightField: width: int -> height: int -> parameters: NoiseParams -> rng: Rng -> Grid<int>

    /// Classify a `Grid<int>` height field into a `Grid<'T>` via an ascending `(threshold, value)` table
    /// (M5). Each cell maps to the `value` of the highest `threshold <= its height`, or the lowest band when
    /// below all. The table is sorted by threshold internally, so any order is accepted. Total: an empty
    /// table yields an empty grid (no `'T` exists to fill it); never throws.
    val classify: table: (int * 'T) list -> field: Grid<int> -> Grid<'T>

    /// Poisson-disk scatter (Bridson) over a `Grid<bool>` mask (M5) — props, spawns, ore. Returns cells that
    /// are all mask-eligible (`true`) and pairwise at least `minDist` apart, in a deterministic insertion
    /// order, threading the `Rng`. Candidates are integer rejection-sampled in the annulus (no trig). Total:
    /// an empty/all-false mask or `minDist < 1` yields the documented result (empty or a clamped spacing);
    /// never throws.
    val poissonScatter: mask: Grid<bool> -> minDist: int -> rng: Rng -> struct (Cell list * Rng)
