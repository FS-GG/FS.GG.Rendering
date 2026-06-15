# Contract: Provenance coverage (bridge-grade)

**Artifact**: `PROVENANCE.md` (completed in place) · **Satisfies**: FR-003 · **SC**: SC-002

Defines what "bridge-grade, complete" provenance means and the mechanical rule that verifies it.

## Required content

1. **Source repository + pinned commit** — `EHotwagner/FS-Skia-UI` at `f759f399` (2026-06-14).
2. **Path map** — for every imported top-level area, an entry mapping source path → repo path.
3. **Adaptations** — every deliberate divergence from source, each with a one-line rationale.
4. **Exclusions** — what was deliberately left in the source archive.
5. **Named gaps** — any imported area not (yet) mapped, listed explicitly. A named gap is acceptable;
   a silent omission is not.

## Coverage rule (the check)

Let `IMPORTED` be the set of imported top-level areas:

```text
src/                       tests/
template/                  .template.config/        .template.package/
docs/imported/             tests/surface-baselines/ scripts/
Directory.Build.props      Directory.Packages.props FS.GG.Rendering.slnx
```

For **each** area in `IMPORTED`, `PROVENANCE.md` MUST either:
- contain a path-map row covering it, **or**
- list it under *Named gaps*.

**Pass** ⇔ zero areas are neither mapped nor named. (SC-002: 100% accounted for.)

> Note: `FS.GG.Rendering.slnx` is repo-authored (the .NET 10 `.slnx` format), not copied verbatim;
> it is recorded as an *adaptation* ("solution authored as `.slnx`") rather than a 1:1 path-map row.
> That counts as "accounted for."

## Honesty rule

The bridge hub references this record (FR-002); it MUST NOT restate the path map. A change to imported
scope updates `PROVENANCE.md` only.
