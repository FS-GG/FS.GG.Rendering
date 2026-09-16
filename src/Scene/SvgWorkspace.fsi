namespace FS.GG.UI.Scene

/// <summary>The four stable SVG workspace modes projected into input contexts.</summary>
[<RequireQualifiedAccess>]
type SvgWorkspaceMode =
    /// <summary>Vector-art creation and editing.</summary>
    | Create
    /// <summary>Scene placement, hierarchy and property editing.</summary>
    | Arrange
    /// <summary>Local or connected gameplay.</summary>
    | Play
    /// <summary>Inspection, replay and evidence review.</summary>
    | Review

/// <summary>A supported docking edge.</summary>
[<RequireQualifiedAccess>]
type SvgWorkspaceDock =
    /// <summary>The leading side panel edge.</summary>
    | Left
    /// <summary>The trailing side panel edge.</summary>
    | Right
    /// <summary>The horizontal panel edge below the scene.</summary>
    | Bottom

/// <summary>Preferred or currently effective panel placement.</summary>
[<RequireQualifiedAccess>]
type SvgPanelPlacement =
    /// <summary>A panel at a stable edge and non-negative ordering position.</summary>
    | Docked of edge: SvgWorkspaceDock * order: int
    /// <summary>A panel at finite workspace coordinates with a positive size.</summary>
    | Floating of x: float * y: float * width: float * height: float
    /// <summary>A panel reduced to its accessible reopening affordance.</summary>
    | Collapsed

/// <summary>One stable panel identity with preferred and responsive placement.</summary>
type SvgWorkspacePanel =
    {
        /// <summary>Stable panel identity.</summary>
        Id: string
        /// <summary>User-accepted placement retained across responsive changes.</summary>
        Preferred: SvgPanelPlacement
        /// <summary>Placement currently projected for the viewport.</summary>
        Effective: SvgPanelPlacement
    }

/// <summary>A versioned workspace layout suitable for persistence.</summary>
type SvgWorkspaceLayout =
    {
        /// <summary>Exact layout schema identity.</summary>
        Schema: string
        /// <summary>Monotonic accepted-preference revision.</summary>
        Revision: int
        /// <summary>At most 32 uniquely identified panels.</summary>
        Panels: SvgWorkspacePanel list
    }

/// <summary>The innermost workspace overlay and its valid restoration target.</summary>
[<RequireQualifiedAccess>]
type SvgWorkspaceOverlay =
    /// <summary>Command discovery with the stable target to restore on close.</summary>
    | CommandPalette of restoreFocus: string
    /// <summary>Live possible-input help with its restoration target.</summary>
    | PossibleInputHelp of restoreFocus: string
    /// <summary>Raw binding capture for a stable command.</summary>
    | RebindCommand of command: string * restoreFocus: string

/// <summary>Portable workspace state; scene, history, camera and selection remain owner state.</summary>
type SvgWorkspaceState =
    {
        /// <summary>Owner-selected workspace mode projected into input contexts.</summary>
        Mode: SvgWorkspaceMode
        /// <summary>Accepted and responsive panel layout.</summary>
        Layout: SvgWorkspaceLayout
        /// <summary>Last accepted finite non-negative viewport width.</summary>
        ViewportWidth: float
        /// <summary>Stable control or scene identity that currently owns focus.</summary>
        FocusTarget: string
        /// <summary>The sole innermost overlay, when present.</summary>
        Overlay: SvgWorkspaceOverlay option
    }

/// <summary>Pure workspace observations.</summary>
[<RequireQualifiedAccess>]
type SvgWorkspaceMessage =
    /// <summary>Project an owner-selected mode into active input contexts.</summary>
    | SetMode of SvgWorkspaceMode
    /// <summary>Recompute effective placements without changing preferences.</summary>
    | SetViewportWidth of float
    /// <summary>Accept and persist one panel preference.</summary>
    | SetPanelPlacement of panelId: string * placement: SvgPanelPlacement
    /// <summary>Open command discovery above the current interaction.</summary>
    | OpenPalette of restoreFocus: string
    /// <summary>Open live possible-input help.</summary>
    | OpenHelp of restoreFocus: string
    /// <summary>Begin raw input capture for one command.</summary>
    | BeginRebind of command: string * restoreFocus: string
    /// <summary>Close the innermost overlay and restore its focus target.</summary>
    | CloseOverlay
    /// <summary>Validate and atomically adopt versioned layout bytes.</summary>
    | ImportLayout of byte[]

/// <summary>Effects interpreted by persistence, focus and input composition hosts.</summary>
[<RequireQualifiedAccess>]
type SvgWorkspaceEffect =
    /// <summary>Replace the resolver's active context projection.</summary>
    | ActiveContextsChanged of string list
    /// <summary>Render the accepted effective layout.</summary>
    | LayoutChanged of SvgWorkspaceLayout
    /// <summary>Persist canonical accepted preference bytes.</summary>
    | PersistLayout of byte[]
    /// <summary>Move focus to the named stable target.</summary>
    | RequestFocus of string
    /// <summary>Report a refused observation without mutating accepted state.</summary>
    | WorkspaceDiagnostic of code: string * message: string

/// <summary>Deterministic workspace state, responsive placement and layout interchange.</summary>
[<RequireQualifiedAccess>]
module SvgWorkspace =
    /// <summary>The exact supported persisted-layout schema.</summary>
    val layoutSchema: string
    /// <summary>The width below which left and right docks collapse.</summary>
    val narrowViewport: float
    /// <summary>The bounded built-in tools, inspector and timeline layout.</summary>
    val defaultLayout: SvgWorkspaceLayout
    /// <summary>Create workspace state and apply its first responsive projection.</summary>
    val init: mode: SvgWorkspaceMode -> viewportWidth: float -> SvgWorkspaceState
    /// <summary>Derive base, mode and innermost-overlay contexts from owner state.</summary>
    val activeContexts: state: SvgWorkspaceState -> string list
    /// <summary>Encode accepted preferences to deterministic UTF-8 bytes.</summary>
    val encodeLayout: layout: SvgWorkspaceLayout -> byte[]
    /// <summary>Decode at most 64 KiB while preserving raw validation failures.</summary>
    val decodeLayout: bytes: byte[] -> Result<SvgWorkspaceLayout, string list>
    /// <summary>Apply one pure workspace observation and emit ordered host effects.</summary>
    val update: message: SvgWorkspaceMessage -> state: SvgWorkspaceState -> SvgWorkspaceState * SvgWorkspaceEffect list
