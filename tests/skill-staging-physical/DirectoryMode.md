# Rendering skill directory and mode plan

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/DirectoryModeTests.fsx
```

The prior file-only plan accepted a source tree after an empty directory was
added: the red-before control failed because the file set was unchanged. The
Linux descriptor enumeration now records every source directory, including
the product root and empty nested directories, with a mode from its stable
descriptor checks. The source-only plan projects them to `skills/<id>` and
`skills/<id>/<relative>` paths. A fresh check refuses added or removed empty
directories and changed directory modes. A pure projection check refuses
missing, extra, wrong-path and wrong-mode directory facts.

This records source modes for a possible later copier. Rendering's live Python
stager calls `shutil.copytree`, but this branch does not write an output tree or
prove exact staged modes, ACL behavior, ownership, manifest output mode, or
installed-package parity. The manifest and product roots are captured at
different instants; adversarial ABA, concurrent in-place changes after the
final checks, and crash behavior remain outside this source-only result.

#1332 Python preflight and #1334/#1335 source acceptance are separate gates.
No release/BOM output, publication, merge or receiver pin is authorized.
