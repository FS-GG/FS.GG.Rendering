# Feature 158 Excluded Timing Validation

Status: `accepted-with-recorded-limitations`

- Measurement-policy classifier coverage is in `tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs`.
- Explicit proof/probe readback samples are classified as `probe-readback-included`, status `probe`, reason `probe-run-excluded`.
- Accepted timing samples from `timing/summary.md` remain `readback-free` and exclude proof/readback artifacts from the measured interval.
