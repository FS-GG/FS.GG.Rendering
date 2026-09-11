#!/usr/bin/env python3
import json
import pathlib
import sys


def integer(value):
    return int(value["#bigint"])


def state_value(state, name):
    value = state[name]
    if isinstance(value, dict) and "#bigint" in value:
        return str(integer(value))
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


if len(sys.argv) < 3:
    raise SystemExit("usage: extract-retained-traces.py [--document] <output.tsv> <trace.itf.json>...")

document_mode = sys.argv[1] == "--document"
offset = 2 if document_mode else 1
output = pathlib.Path(sys.argv[offset])
traces = [pathlib.Path(value) for value in sys.argv[offset + 1:]]
header = [
    "trace", "step", "action", "arg1", "arg2", "arg3", "arg4", "arg5", "arg6",
    "revision", "selected", "focused", "panX", "panY", "zoom", "captured",
    "objectAAvailable", "objectBAvailable", "outcome",
]
rows = ["\t".join(header)]
actions = set()
outcomes = set()
for trace_index, path in enumerate(sorted(traces)):
    document = json.loads(path.read_text())
    states = document["states"]
    if not states or states[0]["state"]["lastAction"] != "Init":
        raise SystemExit(f"{path}: missing initial state")
    for step, envelope in enumerate(states[1:], 1):
        state = envelope["state"]
        action = state["lastAction"]
        outcome = integer(envelope["outcome"])
        actions.add(action)
        outcomes.add(outcome)
        values = [str(trace_index), str(step), action]
        values.extend(state_value(state, f"arg{i}") for i in range(1, 7))
        values.extend(state_value(state, name) for name in (
            "revision", "selected", "focused", "panX", "panY", "zoom", "captured",
            "objectAAvailable", "objectBAvailable",
        ))
        values.append(str(outcome))
        rows.append("\t".join(values))

expected_actions = {
    "Select", "ReplaceDocument" if document_mode else "ReplaceScene", "ClearSelection", "FocusNext", "FocusPrevious",
    "SetCamera", "CapturePointer", "ReleasePointer",
}
if actions != expected_actions:
    raise SystemExit(f"action coverage drifted: {sorted(actions)}")
if outcomes != set(range(7)):
    raise SystemExit(f"outcome coverage drifted: {sorted(outcomes)}")
output.write_text("\n".join(rows) + "\n")
