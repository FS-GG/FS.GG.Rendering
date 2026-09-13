# SVG scale spatial working-set qualification

`SpatialWorkingSet` adds an immutable, deterministic chunk index at the portable Scene boundary. A viewport
query visits only intersecting chunks, applies overscan, and then restores explicitly pinned semantic identities
for focus and selection. Results preserve source order and expose visited-chunk, candidate and total-entry
counters so downstream browser measurements can distinguish world size from live work.

The unit qualification compares the same 100 visible entries in a 100-entry world and a 1,000-entry world whose
extra 900 entries are in distant chunks. Visible identities, visited chunks and candidate count remain equal. A
separate case proves offscreen focused and selected identities remain represented, while malformed bounds,
missing identities and duplicate identities are refused deterministically.

The dedicated pull-request gate runs all Scene tests and rechecks the accepted Preview A raw receipt for zero
idle frames, mutations and rebuilds. Absolute browser cost and live-node growth remain measured by the later
SVG-SCALE-01.3 qualification against the frozen SVG-SCALE-01.1 contract.
