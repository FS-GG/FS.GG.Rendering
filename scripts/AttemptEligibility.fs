module Rendering.AttemptEligibility

/// Eligibility permits one bounded attempt; it does not assert coherent publication or writer ACL proof.
type Facts = {
    OriginalCustodyVerified: bool
    NativeTemplatesQualified: bool
    ProviderPassed: int
    BrowserPassed: int list
    BrowserUnexpected: int
    BrowserSkipped: int
    BrowserFlaky: int
    CompleteReadCensus: bool
    NuGetScopeStatuses: int option list
    GitHubEffectiveWrite: string
}

let admit facts =
    facts.OriginalCustodyVerified && facts.NativeTemplatesQualified && facts.ProviderPassed=180
    && facts.BrowserPassed=[4;4;4] && facts.BrowserUnexpected=0 && facts.BrowserSkipped=0 && facts.BrowserFlaky=0
    && facts.CompleteReadCensus && facts.NuGetScopeStatuses.Length=19
    && List.forall ((=) (Some 200)) facts.NuGetScopeStatuses
    && facts.GitHubEffectiveWrite="unknown-before-attempt"
