// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/Rules.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Versioned, product-neutral rule metadata.
type RuleMetadata =
    {
        Id: string
        Version: int
        Title: string
        Summary: string
        DependsOn: string list
    }

/// Formal-model identity and implementation correspondence carried beside a catalog.
type RuleFormalEvidence =
    {
        ModelId: string
        ModelSha256: string
        Tool: string
        ToolVersion: string
        Invariants: string list
        ImplementationBinding: string
    }

/// One causal contribution to an explanation.
type RuleCause = { Code: string; Message: string }

/// Evaluation returned by a product-owned rule implementation.
type RuleEvaluation<'effect> =
    {
        RuleId: string
        Applies: bool
        Explanation: string
        Causes: RuleCause list
        Effects: 'effect list
    }

/// Product-owned executable rule paired with generic metadata.
type RuleDefinition<'facts, 'effect> =
    {
        Metadata: RuleMetadata
        Evaluate: 'facts -> RuleEvaluation<'effect>
    }

/// Validated catalog. Construction is available only through <c>RuleCatalog.create</c>.
type RuleCatalog<'facts, 'effect> = private RuleCatalog of RuleFormalEvidence * RuleDefinition<'facts, 'effect> list

[<RequireQualifiedAccess>]
type RuleCatalogIssue =
    | MissingRuleId
    | InvalidRuleVersion of ruleId: string * version: int
    | MissingTitle of ruleId: string
    | DuplicateRuleId of string
    | UnknownDependency of ruleId: string * dependencyId: string
    | SelfDependency of string
    | DependencyCycle of string list
    | MissingModelId
    | MissingModelSha256
    | MissingImplementationBinding
    | MissingInvariant
    | EvaluationRuleMismatch of expected: string * actual: string

/// Dependency-ordered evaluation with every causal explanation retained.
type RuleInspection<'effect> =
    {
        RequestedRuleId: string
        Evaluations: RuleEvaluation<'effect> list
        Applies: bool
    }

[<RequireQualifiedAccess>]
module RuleCatalog =
    val create:
        evidence: RuleFormalEvidence ->
        rules: RuleDefinition<'facts, 'effect> list ->
            Result<RuleCatalog<'facts, 'effect>, RuleCatalogIssue list>

    val evidence: catalog: RuleCatalog<'facts, 'effect> -> RuleFormalEvidence
    val metadata: catalog: RuleCatalog<'facts, 'effect> -> RuleMetadata list

    /// Evaluate dependencies before the requested rule. A rule implementation remains the only semantic authority.
    val inspect:
        ruleId: string ->
        facts: 'facts ->
        catalog: RuleCatalog<'facts, 'effect> ->
            Result<RuleInspection<'effect>, RuleCatalogIssue>
