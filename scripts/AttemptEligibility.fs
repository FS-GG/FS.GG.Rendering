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

/// The current journey is a separate selected contract; historical admit stays 180/4.
type CurrentJourney = {
    OriginalCustodyVerified: bool
    NativeTemplatesQualified: bool
    ProviderPassed: int
    BrowserPassed: int list
    BrowserUnexpected: int
    BrowserSkipped: int
    BrowserFlaky: int
    AtPassed: int
    AtCustodyQualified: bool
}

let currentJourneyQualified facts =
    facts.OriginalCustodyVerified && facts.NativeTemplatesQualified && facts.ProviderPassed=186
    && facts.BrowserPassed=[8;8;8] && facts.BrowserUnexpected=0 && facts.BrowserSkipped=0 && facts.BrowserFlaky=0
    && facts.AtPassed=15 && facts.AtCustodyQualified

let admitCurrent journey completeReadCensus scopeStatuses effectiveWrite =
    currentJourneyQualified journey && completeReadCensus && List.length scopeStatuses=19
    && List.forall ((=) (Some 200)) scopeStatuses && effectiveWrite="unknown-before-attempt"

/// Separate accepted font-disposal receiver contract; historical predicates remain unchanged.
type FontDisposalJourney = {
    OriginalCustodyVerified: bool
    NativeTemplatesQualified: bool
    ProviderPassed: int
    BrowserPassed: int list
    BrowserUnexpected: int
    BrowserSkipped: int
    BrowserFlaky: int
    AtPassed: int
    AtSpeechWindows: int
    AtEvidenceMembers: int
    AtCustodyQualified: bool
}

let fontDisposalJourneyQualified (facts: FontDisposalJourney) =
    facts.OriginalCustodyVerified && facts.NativeTemplatesQualified && facts.ProviderPassed=186
    && facts.BrowserPassed=[15;15;15] && facts.BrowserUnexpected=0 && facts.BrowserSkipped=0 && facts.BrowserFlaky=0
    && facts.AtPassed=15 && facts.AtSpeechWindows=4 && facts.AtEvidenceMembers=27 && facts.AtCustodyQualified

let admitFontDisposal journey completeReadCensus scopeStatuses effectiveWrite =
    fontDisposalJourneyQualified journey && completeReadCensus && List.length scopeStatuses=19
    && List.forall ((=) (Some 200)) scopeStatuses && effectiveWrite="unknown-before-attempt"
