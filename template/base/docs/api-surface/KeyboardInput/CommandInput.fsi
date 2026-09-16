// See skill: fs-gg-keyboard-input
namespace FS.GG.UI.KeyboardInput

/// <summary>A non-keyboard route that makes a command discoverable and operable.</summary>
[<RequireQualifiedAccess>]
type CommandAlternative =
    /// <summary>Expose the command through a command palette.</summary>
    | Palette
    /// <summary>Expose the command through a menu.</summary>
    | Menu
    /// <summary>Expose a labelled pointer-operated control.</summary>
    | Pointer of label: string

/// <summary>Declares whether product validation must supply a typed argument.</summary>
[<RequireQualifiedAccess>]
type CommandArgumentPolicy =
    /// <summary>The command carries no argument.</summary>
    | NoArgument
    /// <summary>The product validates an argument against the named schema.</summary>
    | RequiresArgument of schemaId: string

/// <summary>Product-supplied metadata for one stable semantic command.</summary>
type CommandDescriptor =
    {
        /// <summary>Stable namespaced command identity.</summary>
        Id: CommandId
        /// <summary>Human-readable display label, distinct from identity.</summary>
        Label: string
        /// <summary>Contexts in which product availability may permit invocation.</summary>
        Contexts: string list
        /// <summary>Optional opaque key used by the product's availability projection.</summary>
        AvailabilityKey: string option
        /// <summary>Press, repeat, or held-action policy.</summary>
        Trigger: CommandTriggerPolicy
        /// <summary>Argument validation requirement.</summary>
        Argument: CommandArgumentPolicy
        /// <summary>Accessible alternatives used by palettes, menus and pointer controls.</summary>
        Alternatives: CommandAlternative list
    }

/// <summary>Controls whether a command fires once, may follow repeat, or contributes continuous held state.</summary>
[<RequireQualifiedAccess>]
type CommandTriggerPolicy =
    /// <summary>Emit at most once for one press ownership cycle.</summary>
    | OncePerPress
    /// <summary>Allow explicit host repeat observations.</summary>
    | RepeatWhileHeld
    /// <summary>Maintain a held contribution sampled by a later runtime.</summary>
    | Continuous

/// <summary>A validated profile after all ordered overrides have been applied.</summary>
type EffectiveInputProfile =
    {
        /// <summary>The source profile identity.</summary>
        Id: string
        /// <summary>The effective deterministic binding list.</summary>
        Bindings: InputBinding list
    }

/// <summary>Binds one typed gesture to a command in one context.</summary>
type InputBinding =
    {
        /// <summary>The typed gesture identity.</summary>
        Gesture: InputGesture
        /// <summary>The target semantic command.</summary>
        Command: CommandId
        /// <summary>The context in which this binding participates.</summary>
        Context: string
    }

/// <summary>An ordered profile override that preserves replacement, alias and explicit-unbind meaning.</summary>
[<RequireQualifiedAccess>]
type InputBindingOverride =
    /// <summary>Remove every current binding for a command, then add the supplied replacements.</summary>
    | ReplaceCommand of command: CommandId * bindings: InputBinding list
    /// <summary>Add another binding without removing existing command bindings.</summary>
    | AddAlias of InputBinding
    /// <summary>Remove every current binding for a command and keep it explicitly unbound.</summary>
    | UnbindCommand of command: CommandId

/// <summary>The single product catalog used to validate dispatch and discovery data.</summary>
type InputCatalog =
    {
        /// <summary>Known modal contexts.</summary>
        Contexts: InputContextDescriptor list
        /// <summary>Known semantic commands.</summary>
        Commands: CommandDescriptor list
        /// <summary>Host-reserved gestures that profiles cannot claim.</summary>
        ReservedGestures: InputGesture list
        /// <summary>Whether a terminal may also be a longer sequence prefix.</summary>
        AllowTerminalPrefixes: bool
    }

/// <summary>Describes one modal context and its explicit overlap relation.</summary>
type InputContextDescriptor =
    {
        /// <summary>Stable context identity.</summary>
        Id: string
        /// <summary>Resolution priority; equal values in overlapping contexts must remain unambiguous.</summary>
        Priority: int
        /// <summary>Whether the context blocks lower-priority fallthrough while active.</summary>
        Exclusive: bool
        /// <summary>Other contexts that may be active simultaneously.</summary>
        Overlaps: string list
    }

/// <summary>A command-agnostic keyboard or device gesture.</summary>
[<RequireQualifiedAccess>]
type InputGesture =
    /// <summary>One key identity and its complete modifier set.</summary>
    | KeyChord of InputKeyIdentity * InputModifiers
    /// <summary>An ordered sequence of one to four key steps.</summary>
    | KeySequence of (InputKeyIdentity * InputModifiers) list
    /// <summary>An opaque pointer action supplied by a host.</summary>
    | Pointer of string
    /// <summary>An opaque touch action supplied by a host.</summary>
    | Touch of string
    /// <summary>An opaque gamepad control supplied by a host.</summary>
    | Gamepad of string

/// <summary>Identifies either a produced logical key value or a layout-position physical code.</summary>
[<RequireQualifiedAccess>]
type InputKeyIdentity =
    /// <summary>A logical value such as <c>k</c> or <c>Escape</c>.</summary>
    | LogicalKey of string
    /// <summary>A physical position such as <c>KeyK</c>.</summary>
    | PhysicalCode of string

/// <summary>Modifier identity retained for matching and display without collapsing AltGraph into Ctrl+Alt.</summary>
type InputModifiers =
    {
        /// <summary>Whether Control is active.</summary>
        Ctrl: bool
        /// <summary>Whether the platform Meta or Command modifier is active.</summary>
        Meta: bool
        /// <summary>Whether Alt is active.</summary>
        Alt: bool
        /// <summary>Whether Shift is active.</summary>
        Shift: bool
        /// <summary>Whether AltGraph is active independently of Ctrl and Alt reports.</summary>
        AltGraph: bool
    }

/// <summary>An ordered, versioned input profile before validation and override application.</summary>
type InputProfile =
    {
        /// <summary>The exact profile schema identifier.</summary>
        Schema: string
        /// <summary>Stable profile identity.</summary>
        Id: string
        /// <summary>Default bindings retained in declaration order.</summary>
        Defaults: InputBinding list
        /// <summary>Explicit overrides applied in order.</summary>
        Overrides: InputBindingOverride list
    }

/// <summary>A located refusal produced before an invalid catalog or profile is constructed.</summary>
type InputProfileDiagnostic =
    {
        /// <summary>Stable machine-readable diagnostic code.</summary>
        Code: string
        /// <summary>Profile or catalog location.</summary>
        Location: string
        /// <summary>Human-readable explanation.</summary>
        Message: string
    }

/// <summary>Validates catalogs and compiles ordered binding profiles.</summary>
[<RequireQualifiedAccess>]
module CommandInput =
    /// <summary>Validates raw declarations, applies ordered overrides, and validates the effective result.</summary>
    /// <param name="catalog">The product catalog and host policy.</param>
    /// <param name="profile">The untrusted ordered profile.</param>
    val compile:
        catalog: InputCatalog -> profile: InputProfile -> Result<EffectiveInputProfile, InputProfileDiagnostic list>

    /// <summary>Returns a stable typed identity for validation, help and codecs.</summary>
    /// <param name="gesture">The gesture to identify.</param>
    val gestureId: gesture: InputGesture -> string
    /// <summary>The modifier set with every modifier inactive.</summary>
    val noModifiers: InputModifiers
    /// <summary>Migrates a v1 key map as logical-key defaults without changing its reader or inventing overrides.</summary>
    /// <param name="profileId">Identity for the migrated profile.</param>
    /// <param name="contextId">Context assigned to every migrated binding.</param>
    /// <param name="keymap">The existing v1 key map.</param>
    val ofKeymap: profileId: string -> contextId: string -> keymap: Keymap -> InputProfile
    /// <summary>The supported profile schema identifier.</summary>
    val profileSchema: string

/// <summary>Reads and writes the deterministic portable input-profile envelope.</summary>
[<RequireQualifiedAccess>]
module InputProfileCodec =
    /// <summary>Decodes a profile while retaining raw order and duplicates for later validation.</summary>
    /// <param name="bytes">The candidate envelope bytes.</param>
    val decode: bytes: byte[] -> Result<InputProfile, InputProfileDiagnostic list>
    /// <summary>Encodes an ordered profile to deterministic UTF-8 bytes.</summary>
    /// <param name="profile">The profile to encode.</param>
    val encode: profile: InputProfile -> byte[]
    /// <summary>The stable envelope discriminator.</summary>
    val formatId: string
    /// <summary>The supported envelope version.</summary>
    val formatVersion: int
