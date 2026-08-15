# Gate-inversion evidence — FS.GG.Rendering#1240

Every gate this change adds ships with evidence that it can fail. A gate whose inversion survives
has not been shown to run. Each mutation below was applied in place, the gate was executed, the
output recorded verbatim, and the file restored with `git checkout --`.

Gate under test: `src/FS.GG.Rendering.Skills/verify-package.sh`, invoked as
`bash src/FS.GG.Rendering.Skills/verify-package.sh` from the repository root.

Measurement environment: this repository's worktree at branch `item/1240-rendering-skills-package`,
`dotnet` SDK pinned by `global.json` (`10.0.302`), Python 3 stdlib only. No network access is used
by the gate; `dotnet pack` restores from the committed empty lockfile.

## Baseline (unmutated)

```
== 1. stage + derive parity (all product rows staged and content-addressed) ==
   18 product skill(s) staged & content-addressed
== 2. a newly added product row is staged from the manifest, even sourced out-of-tree ==
   synthetic out-of-tree product row flowed from manifest to staged bytes, sidecar included
== 3. pack + content assert (bodies AND their sidecars) ==
   nupkg carries the manifest + every delivered SKILL.md + its sidecars + the handle + README
== 4. content-addressed: every PACKED byte matches its manifest sha256 ==
   every packed SKILL.md verifies against the manifest — the ADR-0014 record a consumer uses
== 5. a tampered byte is REJECTED by that same verify (fail-loud) ==
      content-address mismatch: fs-gg-collision sha256 f8d5b327bd54dd41153b485c253294da00005b4706077a33c4ff77c6341fd362 != manifest cf22a55d88a46e26d47a4c86e35c064b9cc4e0a968b589153283a8afab709d83
   tampered skill 'fs-gg-collision' rejected by the content-addressed verify, as required
verify-package: OK
```

Exit code `0`.

Note that step 5 is itself a mutation test that ships inside the gate: it appends `CORRUPT` to a
packed body and requires the step-4 verdict function to reject it. The four inversions below are
about the gate's own assertions — including whether step 5 can fail.

---

## M1 — the content-addressed verify never fails

Proves **VO-004 / step 5** is load-bearing. This is the inversion that matters most: without it,
step 5 could be a comment.

**Mutation** — in `verify-package.sh`, `content_addressed_ok` short-circuits:

```bash
 content_addressed_ok() {
+  return 0   # MUTATION M1: the verify never fails
   local dir="$1" row id want got
```

**Observed** — steps 1 through 4 still pass, because they are independent checks. Only step 5
notices, and it names the reason exactly:

```
== 4. content-addressed: every PACKED byte matches its manifest sha256 ==
   every packed SKILL.md verifies against the manifest — the ADR-0014 record a consumer uses
== 5. a tampered byte is REJECTED by that same verify (fail-loud) ==
verify-package: FAIL — the content-addressed verify PASSED against a tampered 'fs-gg-collision' — it is not firing
```

Exit code `1` (was `0`).

---

## M2 — the stager copies only `SKILL.md`, dropping sidecars

Proves **VO-005 / step 2's and step 3's sidecar assertions**. This mutation is FS.GG.Game's
single-file stager copied verbatim, which is what a reviewer comparing the two packages would
most plausibly propose. Every digest still matches, because only `SKILL.md` is content-addressed.

**Mutation** — in `stage-skills.py`:

```python
-        # The whole row directory, so a body's sidecars travel with it (ADR-0014 clause 4).
-        shutil.copytree(src_dir, dest)
+        # MUTATION M2: body only, sidecars dropped.
+        os.makedirs(dest, exist_ok=True)
+        shutil.copy2(body_path, os.path.join(dest, BODY_FILENAME))
```

**Observed** — step 1 passes (all 18 digests still match), and the synthetic fixture in step 2
catches it before the pack:

```
== 1. stage + derive parity (all product rows staged and content-addressed) ==
   18 product skill(s) staged & content-addressed
== 2. a newly added product row is staged from the manifest, even sourced out-of-tree ==
verify-package: FAIL — a new product row's sidecar was not staged alongside its body
```

Exit code `1` (was `0`).

Had step 2 not carried a sidecar, step 3 would have caught the same mutation against the real
catalog (`fs-gg-symbology/reference.fsx`, `fs-gg-symbol-design/reference.fsx`,
`fs-gg-feedback-report/scripts/feedback-tool.fsx`). Both assertions are present; step 2 is simply
the earlier of the two.

---

## M3 — the source path is derived from the skill id instead of read from `supplied-by`

Proves the packed set really is read from the manifest's per-row `supplied-by`, which is the whole
reason 3 of 18 rows are not lost. This is the "obvious" spelling.

**Mutation** — in `stage-skills.py`:

```python
-        src_dir = safe_relative(repo_root_real, supplied_by, f"skill {skill_id} supplied-by")
+        src_dir = safe_relative(repo_root_real, f"template/product-skills/{skill_id}", f"skill {skill_id} supplied-by")
```

**Observed** — the stager refuses at the first out-of-tree row:

```
== 1. stage + derive parity (all product rows staged and content-addressed) ==
stage-skills: skill fs-gg-feedback-report: supplied-by 'template/feedback-report/skill/' is not a directory
```

Exit code `2` (was `0`).

`fs-gg-feedback-report` is precisely the row whose absence from a product tree opened
`.github#2380`, and it is the first of the three the id-derived path drops.

---

## M4 — the stager ships one directory the manifest does not declare

Proves **VO-001 / step 1's exact-count assertion**. Every per-row digest check still passes, so a
per-row loop alone cannot catch an *extra* row — only an equality on the set size can.

**Mutation** — in `stage-skills.py`, immediately before the `staged == 0` check:

```python
+    os.makedirs(os.path.join(out, "skills", "fs-gg-undeclared"), exist_ok=True)
+    open(os.path.join(out, "skills", "fs-gg-undeclared", "SKILL.md"), "w").write("undeclared\n")
```

**Observed**:

```
== 1. stage + derive parity (all product rows staged and content-addressed) ==
verify-package: FAIL — staged 19 skill dir(s) but the manifest declares 18 product row(s)
```

Exit code `1` (was `0`).

---

## Summary

| id | mutation | assertion proved | caught at | exit |
|---|---|---|---|---|
| M1 | `content_addressed_ok` always returns 0 | the content-addressed verify fires (VO-004) | step 5 | 1 |
| M2 | stage only `SKILL.md`, drop sidecars | sidecars are delivered (VO-005) | step 2 | 1 |
| M3 | derive source path from the skill id | `supplied-by` is the authority (VO-002) | step 1 | 2 |
| M4 | stage one undeclared directory | the staged set is exactly the declared set (VO-001) | step 1 | 1 |

Four mutations, four distinct failure messages, four different assertions. No inversion survived.

## What is NOT covered here, and why

`verify-package.sh` asserts against a **locally packed** nupkg. Acceptance 2 of the item asks for
the comparison to be made against **published** bytes, which cannot be done before publication
exists. That assertion is implemented in `.github/workflows/release-skills.yml`'s
"Verify the published bytes on both feeds" step — it downloads the exact version from nuget.org and
from GitHub Packages, compares the two payload digests, and re-derives every body's digest from the
downloaded artifact against the committed manifest. It is unexercised until the first `skills/v*`
tag, and it is named as a post-merge obligation rather than claimed as evidence here.
