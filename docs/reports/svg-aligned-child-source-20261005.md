# Aligned-child reconciliation candidate

The existing document reconciler appends every matched child, even when child keys, tags and order already agree. Software diagnostics after the attribute guard retained 307 shared and 702 expanded already-owned child appends per update, including repeated snapshots. This motivates a bounded candidate, not a performance claim.

The candidate synchronizes aligned positional children directly. A count, key or tag mismatch uses the original matching, cloning, ordering and removal path. Attribute comparison/removal, text updates, portable validation, export, parsing, accepted-state assignment and public APIs remain unchanged. No cache or presentation clock is added.

Focused controls cover zero child moves for unchanged and transform-only snapshots, stale attribute repair, keyed reorder and topmost paint, insertion/removal, incompatible primitive tags and unkeyed wrappers. Deliberate surface faults require the exact order, structure or identity predicate; unrelated errors do not count as detected faults.

Native package/Fable/browser qualification and before/after CPU evidence are pending. Compare against protected-main `6b661cb44d763661989ecebe402a736546783804`, which contains the merged attribute guard. Earlier private composition timings used the original unguarded producer and cannot serve as this baseline. Private assets and raw evidence remain outside the producer package. The owning SVG programme remains open; no GPU, publication or product adoption claim is made.
