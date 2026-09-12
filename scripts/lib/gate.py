"""The shared harness for this repo's ``scripts/check-*.py`` gates (.github#1159, decisions in #1158).

WHY THIS EXISTS. Twenty-five gates each re-implement the same four things — an ``ExitCode`` contract,
a ``GateError``, the ``cli()`` broad-except wrapper, and the ``on:``→``True`` PyYAML 1.1 workaround —
and they had drifted: the exit-code contract was spelled two ways, the ``on:`` normaliser coerced list
items in some copies and not others, and ten gates hand-rolled ``GateError``. That is #485's shape
exactly — "computed in N places, agrees in N-1 at best" — and the gate fail-opens #1158 catalogues are
what the disagreements cost. Hoisting the shared body here makes the contract uniform BY CONSTRUCTION:
a gate that imports :data:`ExitCode` and :func:`run` physically cannot invent an ad-hoc code, and the
one ``on:`` normaliser cannot disagree with itself.

WHAT IS AND IS NOT HERE. This module is the harness. Gates migrate onto it in bounded follow-ups
(#1160 began with pin-coherence), and ``tests/gate-harness/`` protects the shared contract while that
migration proceeds. A shared helper landing does not silently migrate unrelated consumers.

THE ONE INVARIANT EVERYTHING HERE SERVES: "I could not look" and "I looked, and it is clean" must never
share an exit code (#266, #320). Python exits 1 on any uncaught exception, and 1 is the FINDING code —
so a gate that crashes would report a confident wrong finding unless something converts the crash into a
no-verdict. :func:`run` is that something, and it is why every gate's ``__main__`` must route through it.

HOW A GATE IMPORTS THIS. Every gate already does ``sys.path.insert(0, os.path.dirname(os.path.abspath(
__file__)))`` — which puts ``scripts/`` on the path so the gate is importable by its test — and that is
all this needs::

    import os, sys
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from lib.gate import ExitCode, GateError, SubjectCensus, Unreachable, run, triggers, load_yaml, read_text, workflow_files

``lib`` is a namespace package (no ``__init__.py``); ``scripts/`` on the path is the whole contract.
"""

from __future__ import annotations

import argparse
import glob
import hashlib
import json
import os
import sys
import traceback
from dataclasses import dataclass
from enum import IntEnum
from pathlib import Path
from typing import Callable, Sequence

__all__ = [
    "ExitCode",
    "GateError",
    "SubjectCensus",
    "Unreachable",
    "run",
    "triggers",
    "load_yaml",
    "read_text",
    "workflow_files",
    "base_parser",
    "report_ok",
    "report_findings",
    "subject_census",
]


class ExitCode(IntEnum):
    """The one exit-code contract, so a gate cannot return an ad-hoc code (#1158 D2).

    These are the values the existing gates already agree on where they bother to; the whole point of
    codifying them is that the ~11 gates that collapsed to ``0/1`` gain the no-verdict distinction for
    free once they migrate, and the ~9 that spelled it by hand stop being able to spell it differently.

    ``IntEnum`` so ``sys.exit(ExitCode.OK)`` and ``return ExitCode.FINDING`` behave as the plain ints
    the shell and ``sys.exit`` expect.
    """

    OK = 0
    """Looked, and there is nothing to report. The ONLY green code."""

    FINDING = 1
    """Looked, and there IS something to report — a real, actionable finding about the subject."""

    NO_VERDICT_RETRYABLE = 2
    """Could not look, and looking again might succeed — a rate limit, an outage, a network 5xx. NOT a
    finding and NOT green: we do not know what is there, so a caller must not act as if we do."""

    NO_VERDICT_PERMANENT = 3
    """Could not reach a verdict, and retrying will not help — an unparsable input, a missing subject,
    a bug in the gate. Still never green: a gate that cannot judge must say so, not pass."""


class GateError(Exception):
    """A definite reason the gate cannot reach a verdict.

    :func:`run` maps this to :attr:`ExitCode.NO_VERDICT_PERMANENT` (3) — never to green, never to a
    finding. Raise it from a read or parse helper (see :func:`read_text`, :func:`load_yaml`) the moment
    the gate's subject cannot be established: a failed read is not a coherent tree, and reporting green
    over one is the exact fail-open this harness exists to make impossible.

    It carries only the standard exception message; the exit code is the wrapper's to assign, so a gate
    never has to remember which number a no-verdict is.
    """


class Unreachable(GateError):
    """A read that could not be MADE — the answer is unknown, not bad (#266).

    A subclass of :class:`GateError` on purpose, in the fail-closed direction: :func:`run` checks it
    FIRST and maps it to :attr:`ExitCode.NO_VERDICT_RETRYABLE` (2), but a gate that only catches
    ``GateError`` still degrades to :attr:`ExitCode.NO_VERDICT_PERMANENT` (3) rather than leaking a
    green or a finding. Use it for the transient no-verdict — a 5xx, a rate limit, a timeout — that a
    later run might resolve; use plain :class:`GateError` for the permanent one. A 404 or a 403 is a
    real answer from the API, not an unreachable one, and should not be raised as this.
    """


@dataclass(frozen=True)
class SubjectCensus:
    """Revision-bound evidence that a gate resolved every subject it declared.

    The declaration and resolution stay separate until the shared reporting boundary.  A caller
    therefore cannot turn ``some paths existed`` into ``the declared subject was complete`` with a
    local Boolean.  ``authority_revision`` identifies the declaration contract; ``authority_digest``
    binds its exact ordered contents at resolution time.
    """

    declared: tuple[str, ...]
    resolved: tuple[str, ...]
    examined: tuple[str, ...]
    producers: tuple[str, ...]
    authority_revision: str
    authority_digest: str

    @property
    def unresolved(self) -> tuple[str, ...]:
        resolved = set(self.resolved)
        return tuple(subject for subject in self.declared if subject not in resolved)

    @property
    def unexamined_producers(self) -> tuple[str, ...]:
        examined = set(self.examined)
        return tuple(subject for subject in self.producers if subject not in examined)

    @property
    def complete(self) -> bool:
        return bool(
            self.declared
            and self.resolved
            and self.examined
            and self.producers
            and self.authority_revision.strip()
            and self.authority_digest.strip()
            and not self.unresolved
            and not self.unexamined_producers
            and set(self.resolved) == set(self.declared)
        )


def subject_census(
    *,
    declared: Sequence[str],
    resolved: Sequence[str],
    examined: Sequence[str],
    producers: Sequence[str],
    authority_revision: str,
) -> SubjectCensus:
    """Build a census from explicit declaration, resolution, enumeration, and producer facts."""

    declaration = tuple(str(subject) for subject in declared)
    producer_facts = tuple(str(subject) for subject in producers)
    authority_digest = hashlib.sha256(
        json.dumps(
            {
                "revision": authority_revision,
                "declared": declaration,
                "producers": producer_facts,
            },
            ensure_ascii=False,
            separators=(",", ":"),
            sort_keys=True,
        ).encode("utf-8")
    ).hexdigest()
    return SubjectCensus(
        declaration,
        tuple(str(subject) for subject in resolved),
        tuple(str(subject) for subject in examined),
        producer_facts,
        authority_revision,
        authority_digest,
    )


def run(main: Callable[[Sequence[str]], int], argv: Sequence[str], *, name: str) -> int:
    """Run ``main(argv)`` and GUARANTEE the exit code is a verdict, never an accident.

    This is the wrapper every gate's ``__main__`` must route through::

        if __name__ == "__main__":
            sys.exit(run(main, sys.argv[1:], name="check-foo"))

    The reason it is not optional: Python exits 1 on any uncaught exception, and 1 is the gate's FINDING
    code. So a ``KeyError`` deep in the gate would surface to CI as a confident "the subject is broken"
    — a wrong finding indistinguishable from a real one. Catching every exception and returning a
    no-verdict is what keeps a bug in the gate from masquerading as a bug in its subject.

    The ordering is load-bearing: :class:`Unreachable` before :class:`GateError` (so the retryable case
    keeps its own code, code 2, even though it is a ``GateError``), and the bare ``except`` last so a
    crash still lands on a no-verdict rather than escaping as exit 1.
    """
    try:
        return int(main(argv))
    except Unreachable as e:
        print(f"::error::{name}: no verdict — {e}", file=sys.stderr)
        return int(ExitCode.NO_VERDICT_RETRYABLE)
    except GateError as e:
        print(f"::error::{name}: no verdict — {e}", file=sys.stderr)
        return int(ExitCode.NO_VERDICT_PERMANENT)
    except Exception:  # noqa: BLE001 — deliberately broad; a crash must become a no-verdict, not exit 1.
        traceback.print_exc()
        print(
            f"::error::{name}: the gate CRASHED, so it has NO VERDICT. This is not a finding about the "
            f"subject — it is a bug in the gate. See the traceback above.",
            file=sys.stderr,
        )
        return int(ExitCode.NO_VERDICT_PERMANENT)


def triggers(doc: dict, what: str = "workflow") -> dict:
    """The workflow's ``on:`` block, with all three legal spellings normalised to a mapping.

    THE TRAP THIS REPLACES: PyYAML resolves the bare key ``on`` to the boolean ``True`` under YAML 1.1,
    so a plain ``doc["on"]`` misses it and every workflow reads as having no triggers — a coherence gate
    then silently skips the file it was meant to judge (#266). Five gates re-solved this, and they did
    not all coerce list items the same way. This is the single reconciled copy: it iterates the key
    candidates ``("on", True)`` (string first, then the boolean), and ``str()``-coerces list items so a
    trigger written as a bare word and one written quoted compare equal.

    An ``on:`` that is present but is neither a mapping, a list, nor a string is a workflow this cannot
    read, so it raises :class:`GateError` rather than guessing — guessing here is the skip that fails
    open. An ABSENT ``on:`` returns ``{}``: a doc that is not a workflow legitimately has no triggers.
    """
    for key in ("on", True):
        if key in doc:
            got = doc[key]
            if isinstance(got, dict):
                return got
            if isinstance(got, list):
                return {str(k): None for k in got}
            if isinstance(got, str):
                return {got: None}
            raise GateError(
                f"{what}: `on:` is {type(got).__name__}, not a string, list, or mapping — this gate "
                f"cannot tell what triggers the workflow, and guessing would silently skip it (#266)."
            )
    return {}


def load_yaml(text: str, what: str) -> dict:
    """Parse ``text`` as a YAML mapping, or raise :class:`GateError` naming ``what``.

    A parse failure and a non-mapping top level are both no-verdict, not findings: the gate could not
    establish its subject, so it must not report on it. PyYAML is imported lazily so a gate that never
    touches YAML need not depend on it.
    """
    import yaml  # lazy: only YAML gates pay for it.

    try:
        doc = yaml.safe_load(text)
    except yaml.YAMLError as e:
        raise GateError(f"{what}: not parsable as YAML — {e}") from e
    if not isinstance(doc, dict):
        raise GateError(f"{what}: not a YAML mapping")
    return doc


def read_text(path: str | os.PathLike[str], what: str) -> str:
    """Read a file as UTF-8, or raise :class:`GateError` — a FAILED READ is not a clean subject.

    An ``OSError`` here (missing file, permission, I/O) is precisely the case a gate must not paper over
    with a green: "the file was not there to check" is not "the file checked out". Naming ``what`` keeps
    the no-verdict message actionable.
    """
    try:
        return Path(path).read_text(encoding="utf-8")
    except OSError as e:
        raise GateError(
            f"cannot read {what} at {path}: {e}. That is a FAILED READ, not a coherent tree — "
            f"refusing to report green."
        ) from e


def workflow_files(root: str = ".") -> list[str]:
    """Every ``.github/workflows/*.{yml,yaml}`` under ``root``, sorted — or a no-verdict if there are none.

    An empty result is treated as a BROKEN GATE, not a clean one: a workflow-auditing gate that finds no
    workflows has almost certainly been pointed at the wrong tree, and reporting green would hide that.
    Both the missing-directory and empty-directory cases raise :class:`GateError`, so the gate goes red
    (no verdict) rather than passing vacuously (#266).
    """
    d = os.path.join(root, ".github", "workflows")
    if not os.path.isdir(d):
        raise GateError(f"{d} is not a directory — there are no workflows to audit")
    files = sorted(f for ext in ("yml", "yaml") for f in glob.glob(os.path.join(d, f"*.{ext}")))
    if not files:
        raise GateError(f"{d} contains no workflow files — there is nothing to audit")
    return files


def base_parser(description: str, *, root: bool = True) -> argparse.ArgumentParser:
    """An ``argparse`` parser seeded with the arg nearly every gate declares: ``--root`` (default ``.``).

    Gates re-declared this parser boilerplate one file at a time (D1). This gives them the common shell
    and lets each add its own arguments to the returned parser. Pass ``root=False`` for the few gates
    that take a registry path or ``--repo`` instead of a working tree.
    """
    ap = argparse.ArgumentParser(description=description)
    if root:
        ap.add_argument("--root", default=".", help="repo root / working tree to audit (default: .)")
    return ap


def report_ok(name: str, summary: str, census: SubjectCensus) -> int:
    """Report green only for a complete, revision-bound subject census.

    The census is structurally required: omission is a Python call error which :func:`run` maps to a
    permanent no-verdict, never a compatibility green.
    """
    if not census.complete:
        missing = ", ".join(census.unresolved) or "<none>"
        unexamined = ", ".join(census.unexamined_producers) or "<none>"
        print(
            f"{name}: NO VERDICT — subject census is incomplete; unresolved declarations: {missing}; "
            f"unexamined producer subjects: {unexamined}; examined={len(census.examined)}; "
            f"authority revision: {census.authority_revision or '<missing>'}.",
            file=sys.stderr,
        )
        return int(ExitCode.NO_VERDICT_PERMANENT)
    print(f"{name}: OK — {summary}")
    return int(ExitCode.OK)


def report_findings(name: str, findings: Sequence[str]) -> int:
    """Print each finding as a ``::error::`` annotation, then a ``FAILED`` summary, and return :attr:`ExitCode.FINDING`.

    Findings share the ``::error::`` annotation channel with no-verdict messages on purpose: the EXIT
    CODE — 1 here versus 2/3 from :func:`run` — is what separates "found a problem" from "could not
    look", not the prefix. Callers with nothing to report should call :func:`report_ok` instead; this
    returns FINDING unconditionally and must not be handed an empty list.
    """
    for f in findings:
        print(f"::error::{f}", file=sys.stderr)
    print(f"::error::{name} FAILED — {len(findings)} finding(s)", file=sys.stderr)
    return int(ExitCode.FINDING)
