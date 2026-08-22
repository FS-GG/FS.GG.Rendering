import React, { useEffect, useLayoutEffect, useState, useTransition } from "react";
import { createRoot } from "react-dom/client";
import {
  TransitionFocusTarget,
  TransitionHostInput,
  TransitionHostMsg$2,
  TransitionHost_beginTransition,
  TransitionHost_authoritative,
  TransitionHost_committed,
  TransitionHost_controlledValue,
  TransitionHost_init,
  TransitionHost_isPending,
  TransitionHost_ledger,
  TransitionHost_update,
  TransitionRequest$1,
  TransitionResponse$2,
  TransitionResponseKind,
  TransitionVisibility,
} from "../generated/src/Elmish/TransitionHost.js";
import "./styles.css";

const focus = (controlId, ariaLabel) => new TransitionFocusTarget(controlId, ariaLabel);
const request = (target) => new TransitionRequest$1(
  target,
  focus("transition-status", `${target} workspace is loading`),
  focus(`${target.toLowerCase()}-workspace`, `${target} workspace`),
);

let model = TransitionHost_init(TransitionVisibility.Visible);
let present = () => {};
let updateControlledText = () => {};
let resetPresentation = () => {};
let resetRenderedRows = () => {};
let latestPresentation;
let requestedPresentations = 0;
let resumePresentations = 0;
let releaseEffects = 0;
let suppressEffects = 0;
let stagedPendingChecks = 0;
let awaitingResume = false;
let completion;

function apply(message) {
  const result = TransitionHost_update(message, model);
  model = result[0];

  for (const effect of result[1]) {
    switch (effect.tag) {
      case 0:
        latestPresentation = effect.fields[0];
        requestedPresentations += 1;
        if (awaitingResume) resumePresentations += 1;
        present(latestPresentation);
        break;
      case 1:
        releaseEffects += 1;
        break;
      case 2:
        suppressEffects += 1;
        break;
      case 3: {
        const target = effect.fields[0];
        queueMicrotask(() => document.getElementById(target.ControlId)?.focus({ preventScroll: true }));
        break;
      }
      default:
        throw new Error(`Unknown transition-host effect tag ${effect.tag}`);
    }
  }
}

function begin(target) {
  const result = TransitionHost_beginTransition(request(target), model);
  model = result[0];

  for (const effect of result[1]) {
    if (effect.tag === 0) {
      latestPresentation = effect.fields[0];
      requestedPresentations += 1;
      present(latestPresentation);
    } else if (effect.tag === 3) {
      const focusTarget = effect.fields[0];
      queueMicrotask(() => document.getElementById(focusTarget.ControlId)?.focus({ preventScroll: true }));
    }
  }

  return TransitionHost_authoritative(model);
}

function respond(token, target, kind, payload) {
  apply(new TransitionHostMsg$2(1, [new TransitionResponse$2(token.Generation, target, kind, payload)]));
}

function setVisibility(visibility) {
  apply(new TransitionHostMsg$2(2, [visibility]));
}

function input(inputMessage) {
  apply(new TransitionHostMsg$2(4, [inputMessage]));
}

function acknowledge(token) {
  apply(new TransitionHostMsg$2(3, [token]));
}

function defer() {
  return new Promise((resolve) => setTimeout(resolve, 4));
}

function nextPaint() {
  return new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));
}

async function resetTransitionJourney() {
  model = TransitionHost_init(TransitionVisibility.Visible);
  latestPresentation = undefined;
  requestedPresentations = 0;
  resumePresentations = 0;
  releaseEffects = 0;
  suppressEffects = 0;
  stagedPendingChecks = 0;
  awaitingResume = false;
  completion = undefined;
  resetPresentation();
  resetRenderedRows();
  await nextPaint();
  return {
    target: document.querySelector("[data-target]")?.getAttribute("data-target"),
    rows: document.querySelectorAll("[data-workspace-row]").length,
    ledgerEntries: [...TransitionHost_ledger(model)].length,
    pending: TransitionHost_isPending(model),
  };
}

function inspectWorkspaceRows() {
  const grid = document.querySelector(".workspace-grid");
  const rows = [...document.querySelectorAll("[data-workspace-row]")];
  const observations = rows.map((row) => {
    const bounds = row.getBoundingClientRect();
    const style = getComputedStyle(row);
    return {
      bounds,
      rendered: row.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true })
        && style.visibility === "visible"
        && style.display !== "none"
        && style.contentVisibility !== "hidden"
        && Number(style.opacity) > 0
        && !row.closest('[aria-hidden="true"]'),
    };
  });
  const semantics = rows.every((row, index) => {
    const score = (index * 17) % 101;
    return row.tagName === "DIV"
      && row.dataset.index === `${index}`
      && row.dataset.score === `${score}`
      && row.getAttribute("aria-label") === `Simulate row ${index}, score ${score}`;
  });

  return {
    count: rows.length,
    semantics,
    visible: observations.every(({ bounds, rendered }) => rendered && bounds.width > 0 && bounds.height === 20),
    columns: grid ? getComputedStyle(grid).gridTemplateColumns.split(" ").length : 0,
    distinctColumnPositions: new Set(observations.map(({ bounds }) => bounds.x)).size,
    widthSpread: observations.length
      ? Math.max(...observations.map(({ bounds }) => bounds.width))
        - Math.min(...observations.map(({ bounds }) => bounds.width))
      : Number.POSITIVE_INFINITY,
  };
}

function buildWorkspaceRows(target, count) {
  return Object.freeze(Array.from({ length: count }, (_, index) => {
    const score = (index * 17) % 101;
    return (
      // Each cached element still maps one-to-one to a visible semantic DOM row. The cache keeps
      // repeated production journeys from allocating the same immutable React element metadata.
      <div
        className="workspace-row"
        data-workspace-row="true"
        data-index={index}
        data-score={score}
        aria-label={`${target} row ${index}, score ${score}`}
        key={index}
      />
    );
  }));
}

const workspaceRows = Object.freeze({
  Editor: buildWorkspaceRows("Editor", 120),
  Plan: buildWorkspaceRows("Plan", 600),
  Simulate: buildWorkspaceRows("Simulate", 1200),
});

async function runTransitionJourney(run) {
  const mark = `fsgg-transition-${run}`;
  performance.mark(`${mark}-start`);
  completion = undefined;
  awaitingResume = false;
  resumePresentations = 0;
  releaseEffects = 0;
  suppressEffects = 0;
  stagedPendingChecks = 0;

  const plan = begin("Plan");
  input(new TransitionHostInput(0, ["workspace-title", `mission-${run}`]));
  updateControlledText(`mission-${run}`);
  input(new TransitionHostInput(1, ["workspace-file", `brief-${run}.json`]));
  input(new TransitionHostInput(2, ["workspace-title"]));
  input(new TransitionHostInput(3, [17n]));
  input(new TransitionHostInput(4, ["Enter"]));
  input(new TransitionHostInput(5, ["old-plan-command"]));
  input(new TransitionHostInput(6, ["workspace-file", `stale-${run}.json`]));

  const simulate = begin("Simulate");
  setVisibility(TransitionVisibility.Hidden);

  await defer();
  respond(plan, "Plan", TransitionResponseKind.PlanningWorker, "obsolete-plan");
  await defer();
  respond(plan, "Plan", TransitionResponseKind.ClientFeatures, "obsolete-features");
  await defer();
  respond(simulate, "Simulate", TransitionResponseKind.PlanningWorker, "simulation-plan");
  await defer();
  respond(simulate, "Simulate", TransitionResponseKind.ClientFeatures, "simulation-features");

  if (latestPresentation) acknowledge(latestPresentation.Token);

  const committedAfterHiddenAck = TransitionHost_committed(model);
  if (
    committedAfterHiddenAck?.Target === "Simulate"
    && committedAfterHiddenAck.Generation.fields[0] === simulate.Generation.fields[0]
  ) {
    throw new Error("Hidden presentation acknowledgement committed Simulate");
  }

  const committedPromise = new Promise((resolve, reject) => {
    completion = { resolve, reject, generation: simulate.Generation, revision: 2n };
  });

  awaitingResume = true;
  setVisibility(TransitionVisibility.Visible);
  awaitingResume = false;
  await committedPromise;
  await nextPaint();

  const committed = TransitionHost_committed(model);
  const controlled = TransitionHost_controlledValue("workspace-title", model);
  const ledgerTags = [...TransitionHost_ledger(model)].map((entry) => entry.tag);

  if (committed?.Target !== "Simulate" || committed.Revision !== 2n) {
    throw new Error("Latest Simulate generation did not commit at response revision 2");
  }
  if (controlled !== `mission-${run}`) throw new Error("Controlled text was not preserved");
  if (resumePresentations !== 1) throw new Error(`Expected one resume presentation, observed ${resumePresentations}`);
  if (releaseEffects !== 1 || suppressEffects !== 4) {
    throw new Error(`Unsafe input directives mismatch: release=${releaseEffects}, suppress=${suppressEffects}`);
  }
  if (![0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].every((tag) => ledgerTags.includes(tag))) {
    throw new Error(`Typed ledger is incomplete: ${ledgerTags.join(",")}`);
  }

  performance.mark(`${mark}-end`);
  performance.measure(mark, `${mark}-start`, `${mark}-end`);
  return {
    target: committed.Target,
    revision: Number(committed.Revision),
    controlled,
    rows: document.querySelectorAll("[data-workspace-row]").length,
    resumePresentations,
    releaseEffects,
    suppressEffects,
    pending: TransitionHost_isPending(model),
    ledgerEntries: ledgerTags.length,
    stagedPendingChecks,
  };
}

function App() {
  const [view, setView] = useState({ target: "Editor", revision: -1, responses: 0, token: undefined });
  const [controlledText, setControlledText] = useState("mission-0");
  const [renderedRows, setRenderedRows] = useState(120);
  const [reactPending, startTransition] = useTransition();

  present = (presentation) => {
    startTransition(() => {
      setView({
        target: presentation.Token.Target,
        revision: Number(presentation.Token.Revision),
        responses: [...presentation.Responses].length,
        token: presentation.Token,
      });
    });
  };
  updateControlledText = setControlledText;
  resetRenderedRows = () => setRenderedRows(120);
  resetPresentation = () => {
    setView({ target: "Editor", revision: -1, responses: 0, token: undefined });
    setControlledText("mission-0");
  };

  const targetRows = view.target === "Simulate" ? 1200 : view.target === "Plan" ? 600 : 120;

  useEffect(() => {
    if (renderedRows === targetRows) return undefined;
    if (renderedRows > targetRows) {
      setRenderedRows(targetRows);
      return undefined;
    }

    const frame = requestAnimationFrame(() => {
      startTransition(() => setRenderedRows((count) => Math.min(targetRows, count + 120)));
    });
    return () => cancelAnimationFrame(frame);
  }, [renderedRows, startTransition, targetRows]);

  useLayoutEffect(() => {
    if (!view.token || renderedRows !== targetRows) {
      if (view.token && view.target === "Simulate" && renderedRows < targetRows) {
        const busy = document.querySelector("main")?.getAttribute("aria-busy") === "true";
        const status = document.getElementById("transition-status")?.textContent;
        if (!TransitionHost_isPending(model) || !busy || status !== "Simulate workspace is loading") {
          throw new Error(`Staged Simulate rows escaped pending state at ${renderedRows}: busy=${busy}, status=${status}`);
        }
        stagedPendingChecks += 1;
      }
      return;
    }

    acknowledge(view.token);
    const committed = TransitionHost_committed(model);
    if (
      completion
      && committed?.Target === "Simulate"
      && committed.Revision === completion.revision
      && committed.Generation.fields[0] === completion.generation.fields[0]
    ) {
      const done = completion;
      completion = undefined;
      done.resolve();
    }
  }, [renderedRows, targetRows, view.token]);

  return (
    <main aria-busy={reactPending || TransitionHost_isPending(model)}>
      <header>
        <h1>Transition-aware Elmish workspace</h1>
        <label htmlFor="workspace-title">Workspace title</label>
        <input
          id="workspace-title"
          value={controlledText}
          onChange={(event) => {
            const value = event.currentTarget.value;
            input(new TransitionHostInput(0, ["workspace-title", value]));
            setControlledText(value);
          }}
        />
        <label htmlFor="workspace-file">Workspace file</label>
        <input
          id="workspace-file"
          type="file"
          onChange={(event) => input(new TransitionHostInput(1, ["workspace-file", event.currentTarget.files?.[0]?.name]))}
        />
      </header>
      <p id="transition-status" role="status" tabIndex={-1} aria-live="polite">
        {reactPending || TransitionHost_isPending(model) ? `${view.target} workspace is loading` : `${view.target} workspace ready`}
      </p>
      <section
        id={`${view.target.toLowerCase()}-workspace`}
        aria-label={`${view.target} workspace`}
        tabIndex={-1}
        data-target={view.target}
        data-revision={view.revision}
      >
        <h2>{view.target}</h2>
        <p>{view.responses} asynchronous responses accepted</p>
        <div className="workspace-grid">
          {workspaceRows[view.target].slice(0, renderedRows)}
        </div>
      </section>
    </main>
  );
}

window.runTransitionJourney = runTransitionJourney;
window.resetTransitionJourney = resetTransitionJourney;
window.inspectWorkspaceRows = inspectWorkspaceRows;
window.transitionHostReady = true;
createRoot(document.getElementById("root")).render(<App />);
