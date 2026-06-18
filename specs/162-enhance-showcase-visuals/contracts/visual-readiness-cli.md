# Contract: Visual Readiness CLI

## Command

```bash
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- \
  visual-readiness \
  --seed 1 \
  --size 1600x1000 \
  --themes light,dark \
  --out specs/162-enhance-showcase-visuals/readiness/visual-evidence
```

## Purpose

Capture real screenshot evidence for AntShowcase visual readiness. This command is distinct from
the existing deterministic `evidence` command: deterministic state and non-blank PNG proof do not
by themselves prove visual layout quality.

## Options

- `--seed <int>`: deterministic seed for page state and scripted interactions.
- `--size <width>x<height>`: accepted capture size. Preferred full-readiness size is `1600x1000`;
  minimum representative size is `1280x800`.
- `--themes <list>`: comma-separated theme list. Full readiness may be requested with CLI aliases
  `light,dark`, which resolve to canonical theme ids `antLight,antDark` in summaries and records.
- `--pages <list>`: optional comma-separated page ids. Omitted means all pages.
- `--out <dir>`: output directory for screenshots, contact sheets, completeness results,
  reviewer defect template, and summaries.
- `--summarize <visual-evidence-dir>`: assemble readiness from an existing evidence directory.
- `--minimum-size <dir>`: optional minimum-size evidence directory to include during summary.
- `--json`: optional machine-readable summary beside markdown output.

## Required Behavior

- Enumerate pages from `PageRegistry.all`.
- Resolve theme aliases through the existing Ant theme resolver and persist canonical theme ids.
- Render each page inside the full shell at the requested size.
- Write one screenshot per page per requested theme when capture succeeds.
- Remove or ignore stale screenshots for failed captures.
- Write automated completeness results.
- Write one contact sheet per requested theme when all images for that theme are valid.
- Write a reviewer defect-classification rubric template for the captured matrix.
- Report capture unavailability without claiming readiness.

## Exit Behavior

- `0`: Evidence package or environment-limited report was written successfully.
- `1`: Command failed before writing a reviewer-visible report.
- `2`: Invalid arguments, unknown page id, unsupported size, unsupported theme, or unreadable
  existing evidence directory.

Rejected, blocked, or environment-limited visual-readiness status is still a successful evidence
publication and should return `0`.

## Acceptance Rules

- Full preferred readiness requires all 19 pages in both canonical themes `antLight` and `antDark`
  at `1600x1000`.
- Minimum-size representative evidence uses `1280x800` and must not be reported as the full
  preferred matrix.
- The command cannot mark visual readiness accepted until reviewer defect classification exists and
  records no critical defects.
- Capture-unavailable, missing, degraded, stale, undecodable, or wrong-size screenshots prevent
  accepted visual readiness.
