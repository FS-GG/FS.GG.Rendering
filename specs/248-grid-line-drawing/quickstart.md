# Quickstart: Grid Line-Drawing helper in a generated game product

**Feature**: `248-grid-line-drawing` | **Date**: 2026-07-05

Proves each User Story 1 acceptance scenario on a **real** generated product (the only thing that catches
the fragment-target rename trap — see the `fragment-target-sourcename-rename` note).

## 1. Scaffold a game product (AS-1 — the helper is present and product-owned)

```sh
dotnet new install .                                   # install the fs-gg-ui template from this repo
dotnet new fs-gg-ui --profile game --productName LineDemo -o /tmp/LineDemo
ls /tmp/LineDemo/src/LineDemo/LineDrawing.fs           # present, next to the renamed project
```

The file sits in the product's own source tree (`src/LineDemo/`), not behind a package reference, and its
namespace was `sourceName`-substituted to `LineDemo` — confirming the fragment `source/target` rename
landed it correctly.

## 2. Build the product (AS-1 — it compiles)

```sh
cd /tmp/LineDemo
./fake.sh build -t Dev        # or: dotnet build
```

`LineDrawing.fs` compiles as part of the product via the `Exists`-guarded, profile-gated Compile item.

## 3. Draw a line and query line-of-sight (AS-2, AS-3)

From the product's `update`/`view` (or an FSI scratch — see `scripts/line-drawing-prelude.fsx`):

```fsharp
open FS.GG.UI.Canvas   // Cell

let a = { Col = 0; Row = 0 }
let b = { Col = 5; Row = 2 }

let cells = LineDrawing.line a b           // ordered tiles a..b, endpoints included, each step adjacent

// tile line-of-sight over your own wall map:
let wall = { Col = 3; Row = 1 }
let isTransparent (c: Cell) = c <> wall
LineDrawing.lineOfSight isTransparent a b  // false — the wall tile blocks sight
// remove the wall from your map → lineOfSight ... returns true
```

Switch the thin line for the no-diagonal-gap variant with a one-word edit — `LineDrawing.supercover a b`
— and rebuild: the emitted region changes with **no** framework edit (AS-3).

## 4. Delete the helper (AS-4 — delete-safe)

```sh
rm /tmp/LineDemo/src/LineDemo/LineDrawing.fs
./fake.sh build -t Dev        # still green — the Compile item is Exists-guarded
```

No governance/acceptance gate hard-fails solely because the helper was removed (`Product.fsproj` stays a
"durable — do not touch" file).

## Cleanup

```sh
dotnet new uninstall .
rm -rf /tmp/LineDemo
```
