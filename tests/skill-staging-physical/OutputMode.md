# Fresh Rendering skill output mode contract

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/OutputModeTests.fsx
```

The live Python stager at SHA-256
`fc28e741661100031d5a6aeabd89a79af96395c73672d20685c77c837f55cb7c`
removes and recreates the output root, writes `skill-manifest.json` with
`open(..., "wb")`, and calls `shutil.copytree` with its default copy function.
`OutputModeReference.py` checks that source shape and the installed Python
default `copy2`/directory `copystat` behavior without calling the stager or
writing an output tree.

The prior file and directory checks accepted a wrong manifest output mode:
the red-before control failed with “wrong manifest output mode 0600 was
unrepresented and accepted.” The pure F# contract now takes an explicit umask
and projects paths, kinds, and modes for a **successful fresh target**:

- receiver root `.` and `skills`: `0777 & ~umask`;
- manifest file: `0666 & ~umask`;
- skill files and directories: their descriptor-captured source modes.

The independent fixture compares these facts with the Python source-only
reference for umasks `022` and `077`. Wrong manifest, container, copied-file,
and empty-directory modes, wrong kinds, missing/extra/duplicate paths, and an
invalid umask refuse.

This is a conditional source-only contract, not an output observation. It
does not model ACLs, ownership, destination umask changes, concurrent writers,
rollback, or exact installed-package modes. Manifest and product roots remain
non-atomic across capture; ABA and post-check mutation remain possible. #1332
Python preflight, #1334/#1335 source acceptance, and installed-package parity
are separate gates. No staging output, release/BOM update, publication, merge,
or receiver pin is authorized.
