module SkillStagingLinuxDescriptors

open System
open System.IO
open System.Runtime.InteropServices
open System.Text
open Microsoft.Win32.SafeHandles

/// Linux-only descriptor operations. Every child is opened relative to a held directory fd.
/// Adapted for Rendering's read-only skill observer; no output descriptor is opened.
module LinuxDescriptors =
    type Kind = Regular | Directory | Special
    type Failure = Link | NonRegular | Unreadable | Changed

    let private directoryFlags = 0x80000 ||| 0x20000 ||| 0x10000 // CLOEXEC, NOFOLLOW, DIRECTORY
    let private entryFlags = 0x80000 ||| 0x20000 ||| 0x800 // CLOEXEC, NOFOLLOW, NONBLOCK
    let private utf8 = UTF8Encoding(false, true)

    [<DllImport("libc", EntryPoint = "openat", SetLastError = true, CharSet = CharSet.Ansi)>]
    extern int private openat(int parent, string name, int flags, uint32 mode)

    [<DllImport("libc", EntryPoint = "statx", SetLastError = true, CharSet = CharSet.Ansi)>]
    extern int private statx(int parent, string name, int flags, uint32 mask, [<Out>] byte[] buffer)

    [<DllImport("libc", EntryPoint = "getdents64", SetLastError = true)>]
    extern int private getdents64(int fd, [<Out>] byte[] buffer, uint32 size)

    [<DllImport("libc", EntryPoint = "dup", SetLastError = true)>]
    extern int private dup(int fd)

    [<DllImport("libc", EntryPoint = "lseek", SetLastError = true)>]
    extern int64 private lseek(int fd, int64 offset, int whence)

    let private number (handle: SafeFileHandle) = int (handle.DangerousGetHandle())

    let private typeBits parent name flags =
        let buffer = Array.zeroCreate<byte> 256
        if statx(parent, name, flags, 0x3u, buffer) <> 0 then Error Unreadable
        else Ok(int (BitConverter.ToUInt16(buffer, 28)) &&& 0xF000)

    let private diagnose parent name =
        match typeBits parent name 0x100 with
        | Ok 0xA000 -> Link
        | Ok 0x8000 | Ok 0x4000 -> Unreadable
        | Ok _ -> NonRegular
        | Error _ -> Unreadable

    let private kind (handle: SafeFileHandle) =
        match typeBits (number handle) "" 0x1000 with // AT_EMPTY_PATH binds the opened fd.
        | Ok 0x8000 -> Ok Regular
        | Ok 0x4000 -> Ok Directory
        | Ok 0xA000 -> Error Link
        | Ok _ -> Ok Special
        | Error issue -> Error issue

    let openChild (parent: SafeFileHandle) name =
        let fd = openat(number parent, name, entryFlags, 0u)
        if fd < 0 then Error(diagnose (number parent) name)
        else
            let handle = new SafeFileHandle(nativeint fd, true)
            match kind handle with
            | Ok entryKind -> Ok(handle, entryKind)
            | Error issue ->
                handle.Dispose()
                Error issue

    let private openDirectory parent name =
        match openChild parent name with
        | Ok(handle, Directory) -> Ok handle
        | Ok(handle, _) ->
            handle.Dispose()
            Error NonRegular
        | Error issue -> Error issue

    let openRoot (absolutePath: string) =
        let fd = openat(-100, "/", directoryFlags, 0u)
        if fd < 0 then Error Unreadable
        else
            use filesystemRoot = new SafeFileHandle(nativeint fd, true)
            let components = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries) |> Array.toList
            let rec descend (parent: SafeFileHandle) remaining =
                match remaining with
                | [] ->
                    let copy = dup(number parent)
                    if copy < 0 then Error Unreadable
                    else Ok(new SafeFileHandle(nativeint copy, true))
                | name :: tail ->
                    match openDirectory parent name with
                    | Error issue -> Error issue
                    | Ok child ->
                        use child = child
                        descend child tail
            descend filesystemRoot components

    let private stamp (handle: SafeFileHandle) =
        let buffer = Array.zeroCreate<byte> 256
        // BASIC_STATS requests mode, mtime, and ctime. Compare mode/uid/gid too,
        // so a copy plan cannot silently inherit metadata from another instant.
        if statx(number handle, "", 0x1000, 0x7ffu, buffer) <> 0 then Error Unreadable
        elif (BitConverter.ToUInt32(buffer, 0) &&& 0xC2u) <> 0xC2u then Error Changed
        else
            let fingerprint =
                [| buffer.[16..31]; buffer.[32..47]; buffer.[96..127]; buffer.[136..143] |]
                |> Array.concat
            let mode = uint32 (BitConverter.ToUInt16(buffer, 28)) &&& 0o7777u
            Ok(fingerprint, mode)

    let private readNames (afterFirstBatch: unit -> unit) (directory: SafeFileHandle) =
        let buffer = Array.zeroCreate<byte> 32768
        let observed = ResizeArray<string>()
        let mutable doneReading = false
        let mutable failure = false
        let mutable firstBatch = true
        while not doneReading && not failure do
            let count = getdents64(number directory, buffer, uint32 buffer.Length)
            if count < 0 then failure <- true
            elif count = 0 then doneReading <- true
            else
                if firstBatch then
                    firstBatch <- false
                    afterFirstBatch ()
                let mutable offset = 0
                while offset < count && not failure do
                    if count - offset < 20 then failure <- true
                    else
                        let length = int (BitConverter.ToUInt16(buffer, offset + 16))
                        if length < 20 || offset + length > count then failure <- true
                        else
                            let start = offset + 19
                            let mutable finish = start
                            while finish < offset + length && buffer.[finish] <> 0uy do
                                finish <- finish + 1
                            if finish = offset + length then failure <- true
                            else
                                try
                                    let name = utf8.GetString(buffer, start, finish - start)
                                    if name <> "." && name <> ".." then observed.Add name
                                with :? DecoderFallbackException -> failure <- true
                            offset <- offset + length
        if failure then Error Unreadable
        else
            observed
            |> Seq.sortWith (fun a b -> StringComparer.Ordinal.Compare(a, b))
            |> Seq.toList
            |> Ok

    /// Refuses observed stamp or roster changes across two scans; this is not an atomic snapshot.
    let namesStable (afterFirstBatch: unit -> unit) (directory: SafeFileHandle) =
        match stamp directory with
        | Error issue -> Error issue
        | Ok before ->
            let first = readNames afterFirstBatch directory
            match stamp directory with
            | Error issue -> Error issue
            | Ok middle when before <> middle -> Error Changed
            | Ok middle ->
                match first with
                | Error issue -> Error issue
                | Ok names ->
                    if lseek(number directory, 0L, 0) < 0L then Error Unreadable
                    else
                        let second = readNames ignore directory
                        match stamp directory, second with
                        | Ok after, Ok repeated when after = middle && names = repeated -> Ok names
                        | Error issue, _ -> Error issue
                        | _, Error issue -> Error issue
                        | _ -> Error Changed

    /// Refuses observed content changes across two reads of one opened regular-file identity.
    let readBytesStable (afterFirstChunk: unit -> unit) (handle: SafeFileHandle) =
        match stamp handle with
        | Error issue -> Error issue
        | Ok before ->
            let copy = dup(number handle)
            if copy < 0 then Error Unreadable
            else
                use copyHandle = new SafeFileHandle(nativeint copy, true)
                use stream = new FileStream(copyHandle, FileAccess.Read)
                let readPass afterChunk =
                    use memory = new MemoryStream()
                    let buffer = Array.zeroCreate<byte> 4096
                    let mutable count = stream.Read(buffer, 0, buffer.Length)
                    let mutable firstChunk = true
                    while count > 0 do
                        memory.Write(buffer, 0, count)
                        if firstChunk then
                            firstChunk <- false
                            afterChunk ()
                        count <- stream.Read(buffer, 0, buffer.Length)
                    memory.ToArray()
                let first = readPass afterFirstChunk
                match stamp handle with
                | Error issue -> Error issue
                | Ok middle when before <> middle -> Error Changed
                | Ok middle ->
                    stream.Seek(0L, SeekOrigin.Begin) |> ignore
                    let second = readPass ignore
                    match stamp handle with
                    | Error issue -> Error issue
                        | Ok after when middle = after && first = second -> Ok(first, snd after)
                    | Ok _ -> Error Changed
