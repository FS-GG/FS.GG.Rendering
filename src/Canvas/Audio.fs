namespace FS.GG.UI.Canvas

// Feature 243: pure audio request surface + record-only interpreter. A product's `update` emits
// AudioEffect values (never plays sound); the interpreter folds a batch into ordered evidence.
// Scene-only, dependency-light — the real audio-output backend is a deferred follow-up and will
// consume the same values without changing this surface.

type SoundId = SoundId of string

type TrackId = TrackId of string

type AudioEffect =
    | PlaySfx of sound: SoundId * volume: float
    | PlayMusic of track: TrackId * loop: bool
    | StopMusic
    | SetMasterVolume of level: float

type AudioEvidence = { Requested: AudioEffect list }

[<RequireQualifiedAccess>]
module Audio =

    let minVolume = 0.0
    let maxVolume = 1.0

    // Total clamp into [minVolume, maxVolume]. `nan` fails both comparisons, so it falls through to
    // minVolume — a defined, non-throwing floor (Principle VI: safe failure, no surprise on bad input).
    let clampVolume (level: float) : float =
        if level <= minVolume || System.Double.IsNaN level then minVolume
        elif level >= maxVolume then maxVolume
        else level

    let playSfx (sound: SoundId) (volume: float) : AudioEffect = PlaySfx(sound, clampVolume volume)

    let playMusic (track: TrackId) (loop: bool) : AudioEffect = PlayMusic(track, loop)

    let stopMusic: AudioEffect = StopMusic

    let setMasterVolume (level: float) : AudioEffect = SetMasterVolume(clampVolume level)

    // Normalize the volume carried by an effect so recorded evidence is always in range, regardless
    // of whether the caller went through a smart constructor.
    let private normalize (effect: AudioEffect) : AudioEffect =
        match effect with
        | PlaySfx(sound, volume) -> PlaySfx(sound, clampVolume volume)
        | SetMasterVolume level -> SetMasterVolume(clampVolume level)
        | PlayMusic _
        | StopMusic -> effect

    let emptyEvidence: AudioEvidence = { Requested = [] }

    // Append to the tail so Requested stays oldest-first without a reverse per call. Requested is a
    // small per-frame batch, so the O(n) append is not a hot path.
    let record (effect: AudioEffect) (evidence: AudioEvidence) : AudioEvidence =
        { evidence with Requested = evidence.Requested @ [ normalize effect ] }

    let interpret (effects: AudioEffect list) : AudioEvidence =
        { Requested = List.map normalize effects }
