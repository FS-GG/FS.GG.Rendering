# Contract: Shared `ReadinessStatus` public surface (additive — Tier 1)

**Home**: `FS.GG.UI.Diagnostics` — `src/Diagnostics/Diagnostics.fsi` (+ `.fs`). **Additive only**:
no existing Diagnostics symbol is removed or re-typed in a breaking way; the existing
`ReadinessDiagnosticStatus`/`readinessStatusToken`/`tryParseReadinessStatus` are migrated to reuse the
shared vocabulary while keeping their emitted tokens byte-identical.

## Proposed `.fsi` additions (shape — exact names finalized in implementation)

```fsharp
namespace FS.GG.UI.Diagnostics

/// Single canonical readiness vocabulary shared by every readiness consumer.
type ReadinessStatus =
    | Accepted
    | Rejected
    | Blocked
    | Missing
    | Unsupported
    | EnvironmentLimited
    | Degraded
    | Incomplete
    | Failed
    | FallbackOnly
    | Pending
    | Unknown

module ReadinessStatus =
    /// The one display-text projection (SC-001). Tokens match existing per-domain statusText byte-for-byte.
    val statusToken : ReadinessStatus -> string
    /// The one canonical accept/block rule (SC-001). Accepted & EnvironmentLimited are non-blocking by default.
    val blocksAcceptance : ReadinessStatus -> bool
    /// Inverse of statusToken; subsumes the existing tryParseReadinessStatus.
    val tryParse : string -> ReadinessStatus option
```

## Contract obligations

| ID | Obligation |
|----|------------|
| C-RS-1 | `statusToken` is total and its output equals the existing per-domain `statusText` for every shared case (byte-for-byte). |
| C-RS-2 | Exactly one `statusToken` and one `blocksAcceptance` exist in the repository after migration (the ~9 duplicate mapper bodies are deleted). |
| C-RS-3 | `tryParse (statusToken s) = Some s` for every case; unknown tokens → `None`. |
| C-RS-4 | `ReadinessDiagnosticStatus` reuses/aliases the shared cases; `readinessStatusToken` output is unchanged (`accepted`/`blocked`/`review-required`/`environment-limited`). If `review-required` has no shared case, it is preserved as a domain projection. |
| C-RS-5 | Each migrated `Testing.fs` DU keeps its public case names (Testing.fsi unchanged) and adds a private `toShared` projection; domain-specific cases keep their existing literal strings. |
| C-RS-6 | Domains whose accept/block rule diverges from the default keep a one-line documented override (e.g. Feature159 blocks on `EnvironmentLimited`). |
| C-RS-7 | `readiness/surface-baselines/FS.GG.UI.Diagnostics.txt` is regenerated for the additive surface; `FS.GG.UI.Testing.txt` is unchanged. |

## Verification

- `dotnet build FS.GG.Rendering.slnx -c Release` clean.
- Full `dotnet test` shows no new failures vs baseline; all readiness golden assertions stay green
  (byte-stability of serialized status text — FR-006).
- Surface-drift check passes after regenerating only `FS.GG.UI.Diagnostics.txt`.
