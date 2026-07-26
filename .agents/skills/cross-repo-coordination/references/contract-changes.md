# Contract changes

Identify the owner of the versioned contract and file the change there. State old/new semantics,
compatibility window, producer and consumer acceptance, version policy, and rollout order. Sequence
producer publication before consumer adoption unless the change is explicitly backward-compatible.

Update the compatibility/dependency registry and add an ADR when the choice changes a durable
cross-repo rule. Avoid source-project references across repositories; consumers validate the published
artifact they will actually use.
