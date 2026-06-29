# T004 — Two-gate root-cause map

The composition path fails for **two independent reasons**. "Done" (FR-004) requires **one** version
`V` for which **both** are fixed. Neither alone is sufficient.

| Gate | Symptom | Root cause | Fix | Story | FR / INV / SC |
|---|---|---|---|---|---|
| **A. Publish** | `dotnet new fs-gg-ui --productName …` → **exit 127** (`--productName` not a known option) | The org feed serves only `0.1.52-preview.1`, which **predates** Feature 217 (`6df0d39`). The installed template has no `productName` symbol. | Publish a coherent-set version **`> 0.1.52-preview.1`** that carries Feature 217. | **US2** | FR-001/005/006, INV-1/2/3/6, SC-001/003 |
| **B. Visibility** | `dotnet new install FS.GG.UI.Template@V` from a foreign/consumer token → **exit 103** ("could not be authenticated" / NotFound) | The `FS.GG.UI.Template` package is **`private`**; an ordinary org-consumer `GITHUB_TOKEN` (`packages: read`, no explicit private grant) cannot read it. | Flip package visibility **`private → internal`** (or grant Templates repo Read). | **US3** | FR-002/003, INV-8/9, SC-002 |
| **C. Conjunction (binding invariant)** | Either symptom present ⇒ composition still red | A version published-but-private still 103s; a version readable-but-old still 127s. | The **same** `V` must satisfy A **and** B. | **US1** | FR-004, INV-15, SC (combined) |

## Why the two are independent (research R3)

Package **visibility is per-package and persists across versions**. Publishing `0.1.53-preview.1`
under the existing `private` package leaves it `private` (still 103). Flipping visibility on the
`private` package while the feed still serves only `0.1.52` leaves `--productName` rejected (still
127). The two gates touch **different systems** — repo pins/feed (A) vs. org package settings (B) —
so US2 and US3 can proceed in parallel, but **US1 only passes once both hold for one `V`**.

## Mapping to authoritative pass/fail signals

- Exit **127** is the only authoritative signal for Gate A (`--productName` honored).
- Exit **103** is the only authoritative signal for Gate B (consumer-token install).
- Green local packs / in-repo `template-product-tests` do **not** substitute — they say nothing
  about what the feed serves or whether a foreign token can read it (Feature 175/216 lesson).
