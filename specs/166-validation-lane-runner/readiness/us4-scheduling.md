# US4 Scheduling Evidence

Focused tests:

```text
Feature166Scheduling.unsafe parallel requests name lanes sharing concurrency group or output scope
Feature166Scheduling.sequential schedule accepts lanes that share generated output metadata
```

Result: parallel requests with shared concurrency group or output scope fail
preflight with `unsafe-schedule`; the default sequential schedule is accepted.
This prevents accidental shared-output races before lane execution starts.
