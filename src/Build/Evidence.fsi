namespace FS.GG.UI.Build.Evidence

// Feature 202: curated public surface of the in-process governance engine. Visibility lives
// here (Principle II) — anything omitted is private. The reflected entrypoint build.fsx binds
// is `GeneratedRunner.run` (contracts/engine-invocation-contract.md); the evidence-node and
// audit-verdict types (data-model.md §3–4) are exposed so the gates are testable through the
// same public surface a script would use, not only by their file side-effects.

/// The derived state of one sensed `readiness/**` evidence artifact.
[<RequireQualifiedAccess>]
type EvidenceState =
    /// The artifact is present and satisfies its token contract.
    | PresentValid
    /// The artifact is present but malformed (missing a required token, or empty); the reason
    /// names the failing class. A present-invalid artifact is a defect in the generated
    /// product's own evidence, distinct from a framework/feed engine-resolution failure.
    | PresentInvalid of reason: string

/// One sensed readiness artifact plus its derived state. EvidenceGraph senses what exists under
/// the product's `readiness/` tree (it graphs the available surface; absent optional artifacts
/// are not failures) and records one node per recognized artifact present.
type EvidenceNode =
    { /// Path of the sensed artifact, relative to the product root (e.g. `readiness/layout-evidence.txt`).
      ArtifactPath: string
      /// Evidence kind (layout / scene / launch / image / screenshot / pixel-readback /
      /// window-diagnostics / window-options / bounded-smoke / window-visibility / generated-validation).
      Kind: string
      /// The artifact's derived state.
      State: EvidenceState }

/// The pass/fail judgement the EvidenceAudit target emits.
[<RequireQualifiedAccess>]
type Verdict =
    | Pass
    /// Audit failed; the reason names the failing class and distinguishes a product-evidence
    /// defect from a framework/feed condition (FR-005, US3 acceptance #2).
    | Fail of reason: string

/// EvidenceGraph: sense the product's readiness surface and render the validation graph.
module Graph =

    /// Sense the recognized `readiness/**` artifacts present under the given product directory.
    /// Returns one node per recognized artifact that exists (present-valid or present-invalid);
    /// recognized artifacts that are absent are omitted — the graph reflects what exists.
    val sense: dir: string -> EvidenceNode list

    /// Render a real synthesized markdown graph over the sensed nodes (and the raw set of files
    /// found under `readiness/`), suitable for `readiness/evidence-graph.md`. Never a log-only stub.
    val render: dir: string -> nodes: EvidenceNode list -> string

/// EvidenceAudit: judge the sensed graph and render the governance verdict.
module Audit =

    /// Derive the audit verdict from the sensed nodes: PASS when no present artifact is malformed,
    /// FAIL (naming the malformed artifacts as a product-evidence defect) otherwise.
    val evaluate: nodes: EvidenceNode list -> Verdict

    /// Render the `readiness/evidence-audit.md` body: always carries the required `verdict` token,
    /// and on failure the failing class plus the framework/feed-vs-product-defect clarification.
    val render: verdict: Verdict -> nodes: EvidenceNode list -> string

/// The reflected façade build.fsx invokes by simple-name reflection
/// (`FS.GG.UI.Build.Evidence.GeneratedRunner` + static `run`).
module GeneratedRunner =

    /// Run a governance evidence target against the generated product working directory.
    /// `target` is `"EvidenceGraph"` or `"EvidenceAudit"`; `dir` is the product root. Writes the
    /// corresponding `readiness/evidence-*.md` report (creating parent dirs) and returns a
    /// process-style exit code (0 = pass; non-0 = fail). An unknown target returns non-0 with a
    /// loud diagnostic.
    val run: target: string -> dir: string -> int
