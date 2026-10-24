import { instanceResearchOrder, validateInstanceResearchResult } from "./suite-contract.js";
import researchCorpus from "./instance-research-command-streams.json" with { type: "json" };

// Pure command oracle; it does not implement or time the production renderer.
export function instanceResearchStates(corpus = researchCorpus, count = 100) {
  const ensure = (ok, detail) => { if (!ok) throw Error(`instance corpus: ${detail}`); };
  ensure(corpus.schema === "svg-instance-command-streams/2" && corpus.seed === 1729, "identity");
  ensure(JSON.stringify(corpus.counts) === "[100,250,500]" && corpus.counts.includes(count), "count");
  ensure(JSON.stringify(corpus.viewBox) === "[0,0,410,510]", "common viewBox");
  let revision = 0;
  let instances = Array.from({length:count}, (_,i) => ({id:`research-${i}-0`, index:i, x:10+(i%20)*20, y:10+Math.floor(i/20)*20}));
  const states = [];
  for (const command of corpus.commands) {
    switch (command.op) {
      case "snapshot": break;
      case "move":
        ensure(command.stride === 10 && Number.isFinite(command.dx) && Number.isFinite(command.dy), "sparse motion");
        instances = instances.map(v => v.index % command.stride === 0 ? {...v,x:v.x+command.dx,y:v.y+command.dy} : v); break;
      case "definition": revision = command.revision; break;
      case "remove":
        ensure(command.indices.every(i => instances.some(v => v.index === i)), "remove existing");
        instances = instances.filter(v => !command.indices.includes(v.index)); break;
      case "create":
        ensure(command.indices.every(i => Number.isInteger(i) && i >= 0 && i < count && !instances.some(v => v.index === i)), "create absent");
        instances = [...instances, ...command.indices.map(i => ({id:`research-${i}-${command.generation}`, index:i, x:10+(i%20)*20,y:10+Math.floor(i/20)*20}))]; break;
      default: throw Error("instance corpus: unknown command");
    }
    const definition = corpus.definitionRevisions.find(d => d.revision === revision);
    ensure(definition && definition.radius > 0 && definition.rgb.length === 3, "definition");
    ensure(instances.length <= count && new Set(instances.map(v=>v.id)).size === instances.length, "population");
    ensure(instances.every(v => Number.isFinite(v.x) && Number.isFinite(v.y) && v.x-definition.radius >= 0 && v.y-definition.radius >= 0 && v.x+definition.radius <= 410 && v.y+definition.radius <= 510), "visible geometry bounds");
    states.push({name:command.name,revision,radius:definition.radius,rgb:[...definition.rgb],instances:instances.map(v=>({...v}))});
  }
  return states;
}

// Compare independently observed geometry/style/layer descriptors with the command oracle.
export function assertInstanceResearchSemantics(expected, observed) {
  if (JSON.stringify(expected) !== JSON.stringify(observed)) throw Error("instance corpus: semantic mismatch");
}

export function assertInstanceResearchHit(f, node, instance, require) {
  // A tall root cannot expose every point at once. Scroll only outside timed replacement spans.
  node.scrollIntoView({block:"center",inline:"center"});
  const actual = f.documentHit(instance.x, instance.y);
  require(actual === instance.id, "instance hit identity", {expected:instance.id,actual,x:instance.x,y:instance.y});
}

function instanceResearchControl(f, rootOf, liveMatchesExport, require, countUpdate, now, order) {
  const results = [], mountedRoot = rootOf();
  for (const count of researchCorpus.counts) {
    const states = instanceResearchStates(researchCorpus,count);
    for (const variant of instanceResearchOrder(order)) {
      let previous = new Map();
      const observations = new Map();
      const {warmupPasses,samplePasses} = researchCorpus.measurement;
      require(warmupPasses === 2 && samplePasses === 5, "instance timing bounds");
      // Counters and timing run in different passes; replay the complete ordered stream.
      for (let pass = 0; pass < 1 + warmupPasses + samplePasses; pass++) {
      for (const state of states) {
        const ids=state.instances.map(v=>v.id), xs=state.instances.map(v=>v.x), ys=state.instances.map(v=>v.y);
        const replace = () => f.replaceInstanceResearchDocument(variant,state.revision,ids,xs,ys);
        let error, updateCounts, elapsed;
        if (pass === 0) { const counted=countUpdate(replace); ({value:error,...updateCounts}=counted); }
        else if (pass > warmupPasses) { const start=now();error=replace();elapsed=now()-start;require(Number.isFinite(elapsed)&&elapsed>=0,"instance timing clock"); }
        else error=replace();
        require(error === null, `instance ${count}/${variant}/${state.name}: ${error}`);
        liveMatchesExport();
        const root = rootOf();
        require(root === mountedRoot, "instance root rebuilt");
        require(root.getAttribute("viewBox") === researchCorpus.viewBox.join(" "), "instance common viewBox");
        root.scrollIntoView({block:"center",inline:"center"});
        const elements = [...root.children].filter(n=>n.hasAttribute("data-fsgg-element-id"));
        require(JSON.stringify(elements.map(n=>n.getAttribute("data-fsgg-element-id"))) === JSON.stringify(state.instances.map(v=>v.id)), "instance layer order");
        const current = new Map(elements.map(n=>[n.getAttribute("data-fsgg-element-id"),n]));
        for (const [id,node] of previous) {
          if (current.has(id)) require(current.get(id) === node, "instance surviving ownership");
          else require(!node.isConnected, "instance removed ownership");
        }
        const observed = state.instances.map(v => {
          const node = current.get(v.id);
          const circle = variant === "shared-symbol" ? root.querySelector("defs circle") : node.querySelector("circle");
          require(circle !== null, "instance resolved geometry");
          require(Number(circle.getAttribute("cx")) === 0 && Number(circle.getAttribute("cy")) === 0, "instance local geometry");
          if (variant === "shared-symbol") {
            require(node.querySelector("use")?.getAttribute("href") === `#${root.querySelector("defs symbol").id}`, "instance definition reference");
            require(root.querySelector("defs symbol").getAttribute("viewBox") === "-5 -5 10 10"
              && ["x","y","width","height"].map(name=>node.querySelector("use").getAttribute(name)).join(" ") === "-5 -5 10 10", "instance symbol viewport");
          }
          assertInstanceResearchHit(f, node, v, require);
          require(node.getAttribute("transform") === `matrix(1 0 0 1 ${v.x} ${v.y})`, "instance transform");
          return {id:v.id,radius:Number(circle.getAttribute("r")),fill:circle.getAttribute("fill")};
        });
        const expected = state.instances.map(v=>({id:v.id,radius:state.radius,fill:`rgb(${state.rgb.join(" ")})`}));
        assertInstanceResearchSemantics(expected,observed);
        previous = current;
        if (pass === 0) observations.set(state.name,{count,variant,command:state.name,instances:elements.length,updateCounts,timing:{samplesMs:[],warmupPasses,samplePasses,scope:researchCorpus.measurement.scope}});
        if (elapsed !== undefined) observations.get(state.name).timing.samplesMs.push(elapsed);
      }
      }
      for (const observation of observations.values()) {
        require(observation.timing.samplesMs.length === samplePasses,"instance sample coverage");
        results.push(observation);
      }
    }
  }
  return results;
}

// One functional witness shared by the self-running suite and packed browser qualification.
// Keep this function self-contained: Playwright evaluates this exact function in the packed page.
export function definitionIsolationControl(w = window) {
  const f = w.svgFoundation, doc = w.document;
  const require = (condition, detail, evidence) => { if (!condition) throw new Error(`document-reconciliation gate: ${detail}${evidence ? ": " + JSON.stringify(evidence) : ""}`); };
  const rootOf = () => doc.querySelector("#document-fixture svg");
  const root = rootOf();
  require(root, "definition isolation document not mounted");
  const canonical = node => [node.namespaceURI, node.localName, [...node.attributes].map(a => [a.name,a.value]).sort((a,b) => a[0].localeCompare(b[0])), node.children.length ? null : node.textContent, [...node.children].map(canonical)];
  const liveMatchesExport = () => {
    const parsed = new w.DOMParser().parseFromString(f.documentExport(), "image/svg+xml");
    require(!parsed.querySelector("parsererror") && JSON.stringify(canonical(rootOf())) === JSON.stringify(canonical(parsed.documentElement)), "live/export structure differs");
  };
  const keyed = () => new Map([...root.querySelectorAll("[data-fsgg-element-id]")].map(node => [node.getAttribute("data-fsgg-element-id"),node]));
  const expectKilled = (name, detail, operation) => {
    let failure;try { operation(); } catch(error) { failure = error.message; }
    require(failure === `document-reconciliation gate: ${detail}`, `${name} mutant survived`);
    return {name,outcome:"killed"};
  };
  const replaceStructure = (mode, ids) => {
    const previous = keyed();
    require(f.replaceReconciliationDocument(mode) === null, "definition candidate refused");
    require(rootOf() === root, "definition root rebuilt");
    const current = keyed();
    const actualOrder = [...root.children].filter(node => node.hasAttribute("data-fsgg-element-id")).map(node => node.getAttribute("data-fsgg-element-id"));
    require(JSON.stringify(actualOrder) === JSON.stringify(ids), "definition instance order changed");
    for (const [id,node] of previous) if (current.has(id)) require(current.get(id) === node, "definition surviving element rebuilt");
    liveMatchesExport();
    return {mode,accepted:true};
  };
  // Definition-only replacement must not touch a disjoint definition/instance cohort.
  const abIds = ["control-a-0", "control-a-1", "control-b-0", "control-b-1"];
  replaceStructure("definition-ab-original", abIds);
  root.scrollIntoView({block:"center",inline:"center"});
  const abElements = keyed();
  const definitionOf = node => {
    const href = node.querySelector("use")?.getAttribute("href");
    require(href?.startsWith("#"), "definition cohort reference missing");
    const definition = root.querySelector(`[id='${w.CSS.escape(href.slice(1))}']`);
    require(definition?.localName === "symbol", "definition cohort symbol missing");
    return definition;
  };
  const definitionA = definitionOf(abElements.get("control-a-0"));
  const definitionB = definitionOf(abElements.get("control-b-0"));
  require(definitionA !== definitionB
    && definitionOf(abElements.get("control-a-1")) === definitionA
    && definitionOf(abElements.get("control-b-1")) === definitionB, "definition cohorts not independent");
  const subtree = node => [node, ...[...node.childNodes].flatMap(subtree)];
  const bRoots = [definitionB, abElements.get("control-b-0"), abElements.get("control-b-1")];
  const bSnapshot = bRoots.map(node => ({node, nodes:subtree(node), markup:node.outerHTML}));
  const bFill = w.getComputedStyle(definitionB.querySelector("circle")).fill;
  const bHits = [120,150].map(x => f.documentHit(x,40));
  require(JSON.stringify(bHits) === JSON.stringify(["control-b-0","control-b-1"]), "definition B initial hit identity", {expected:["control-b-0","control-b-1"],actual:bHits,points:[[120,40],[150,40]],viewBox:root.getAttribute("viewBox"),bounds:root.getBoundingClientRect().toJSON()});
  const initialAHits = [47,77].map(x => f.documentHit(x,40));
  require(initialAHits.every(hit => hit === null), "definition A initial geometry", {expected:[null,null],actual:initialAHits,points:[[47,40],[77,40]]});
  const bObserver = new w.MutationObserver(() => {});
  // Root child-list records are needed too: moving B records a change on its parent.
  bObserver.observe(root, {subtree:true,childList:true,attributes:true,characterData:true});
  const affectsB = record => bRoots.some(node => record.target === node || node.contains(record.target)
    || [...record.addedNodes,...record.removedNodes].some(changed => changed === node || changed.contains(node)));
  const assertB = () => {
    require(definitionOf(keyed().get("control-b-0")) === definitionB, "unrelated definition B rebuilt");
    for (const snapshot of bSnapshot) {
      const actual = subtree(snapshot.node);
      require(actual.length === snapshot.nodes.length && actual.every((node,i) => node === snapshot.nodes[i])
        && snapshot.node.isConnected && snapshot.node.outerHTML === snapshot.markup, "unrelated B subtree changed");
    }
    require(["control-b-0","control-b-1"].every(id => keyed().get(id) === abElements.get(id)), "unrelated B instance rebuilt");
    require(w.getComputedStyle(definitionB.querySelector("circle")).fill === bFill
      && JSON.stringify([120,150].map(x => f.documentHit(x,40))) === JSON.stringify(bHits), "unrelated B style/hit changed");
  };
  const assertNoBMutation = () => require(bObserver.takeRecords().filter(affectsB).length === 0, "unrelated B mutation observed");
  let definitionIsolation;
  try {
    const update = replaceStructure("definition-ab-changed", abIds);
    assertB();assertNoBMutation();
    const aCircle = definitionOf(keyed().get("control-a-0")).querySelector("circle");
    require(Number(aCircle?.getAttribute("r")) === 8 && aCircle.getAttribute("fill") === "rgb(220 80 60)", "definition A replacement geometry/style");
    const dependentAHits = [47,77].map(x => f.documentHit(x,40));
    require(JSON.stringify(dependentAHits) === JSON.stringify(["control-a-0","control-a-1"]), "definition A dependent hit update", {expected:["control-a-0","control-a-1"],actual:dependentAHits,points:[[47,40],[77,40]]});
    // The identity witness catches a semantically identical unrelated subtree rebuild.
    const clonedB = definitionB.cloneNode(true);
    definitionB.replaceWith(clonedB);
    let rebuildControl;
    try { rebuildControl = expectKilled("rebuild-unrelated-definition-b", "unrelated definition B rebuilt", assertB); }
    finally { clonedB.replaceWith(definitionB); }
    const rebuildMutationControl = expectKilled("rebuild-unrelated-definition-b-mutation", "unrelated B mutation observed", assertNoBMutation);
    assertB();assertNoBMutation();
    // Final equality alone cannot detect removing and reinserting the same B node.
    const bParent = definitionB.parentNode, bNext = definitionB.nextSibling;
    bParent.removeChild(definitionB);bParent.insertBefore(definitionB,bNext);
    assertB();
    const moveControl = expectKilled("move-unrelated-definition-b", "unrelated B mutation observed", assertNoBMutation);
    assertB();assertNoBMutation();
    // A redundant write preserves markup and identity but must fail the scoped mutation witness.
    const bCircle = definitionB.querySelector("circle");
    bCircle.setAttribute("r", bCircle.getAttribute("r"));
    const mutationControl = expectKilled("write-unrelated-definition-b", "unrelated B mutation observed", assertNoBMutation);
    assertB();assertNoBMutation();liveMatchesExport();
    definitionIsolation = {update,unrelatedMutations:0,unrelatedSubtreesRetained:true,bHits,dependentHits:["control-a-0","control-a-1"],controls:[rebuildControl,rebuildMutationControl,moveControl,mutationControl]};
  } finally { bObserver.disconnect(); }
  require(f.replaceOriginalDocument() === null, "restore after definition isolation refused");
  liveMatchesExport();
  return definitionIsolation;
}

// Finite public-fixture correctness, shared verbatim by both real browser callers.
// Keep self-contained for page.evaluate; documentHit proves semantic routing, not pixels/GPU.
export function instanceAppearanceControl(w = window) {
  const f = w.svgFoundation, doc = w.document;
  const require = (condition, detail) => { if (!condition) throw Error(`instance-appearance gate: ${detail}`); };
  const rootOf = () => doc.querySelector("#document-fixture svg"), root = rootOf();
  require(root, "document not mounted");
  const canonical = node => [node.namespaceURI, node.localName,
    [...node.attributes].map(a => [a.name,a.value]).sort((a,b) => a[0].localeCompare(b[0])),
    node.children.length ? null : node.textContent, [...node.children].map(canonical)];
  const exported = () => {
    const parsed = new w.DOMParser().parseFromString(f.documentExport(), "image/svg+xml");
    require(!parsed.querySelector("parsererror"), "export invalid");
    return canonical(parsed.documentElement);
  };
  const liveMatchesExport = () => require(JSON.stringify(canonical(rootOf())) === JSON.stringify(exported()), "live/export mismatch");
  const original = JSON.stringify(exported());
  const subtree = node => [node, ...[...node.childNodes].flatMap(subtree)];
  const ids = Array.from({length:10}, (_,i) => `appearance-${i}`);
  const base = ids.map((id,i) => ({id,x:24+i*32,y:32,faction:i%2,health:6,selected:false}));
  const steps = [
    {name:"initial",faction:0,health:6,selected:false,x:24,y:32},
    {name:"faction",faction:1,health:6,selected:false,x:24,y:32},
    {name:"health-zero",faction:1,health:0,selected:false,x:24,y:32},
    {name:"health-ten",faction:1,health:10,selected:false,x:24,y:32},
    {name:"outline-add",faction:1,health:10,selected:true,x:24,y:32},
    {name:"outline-remove",faction:1,health:10,selected:false,x:24,y:32},
    {name:"combined",faction:0,health:3,selected:true,x:32,y:36},
    {name:"identical-repeat",faction:0,health:3,selected:true,x:32,y:36},
    {name:"restore",faction:0,health:6,selected:false,x:24,y:32},
  ];
  let observer, firstFailure, result;
  const representations = [];
  try {
    liveMatchesExport();
    for (const representation of ["shared-symbol","expanded-geometry"]) {
      const apply = state => f.replaceHostedBenchmarkDocument(representation,0,0,
        state.map(u=>u.id),state.map(u=>u.x),state.map(u=>u.y),
        state.map(u=>u.faction),state.map(u=>u.health),state.map(u=>u.selected));
      const stateFor = step => base.map((u,i) => i === 0 ? {...u,...step} : {...u});
      require(apply(stateFor(steps[0])) === null, "initial replacement refused");
      const camera = root.querySelector("[data-fsgg-element-id='bench-camera']");
      require(camera, "camera missing");
      const units = () => [...camera.children].filter(n=>n.hasAttribute("data-fsgg-element-id"));
      const keyed = () => new Map(units().map(n=>[n.getAttribute("data-fsgg-element-id"),n]));
      const initialUnits = keyed(), target = initialUnits.get(ids[0]);
      require(initialUnits.size === 10 && target, "initial unit population");
      const part = (node, name) => [...node.children].find(n=>n.getAttribute("data-fsgg-element-id") === `${node.getAttribute("data-fsgg-element-id")}/${name}`);
      const symbols = [...root.querySelectorAll("defs > symbol")];
      const definition = faction => symbols.find(n=>n.firstElementChild?.getAttribute("data-fsgg-element-id") === `bench-faction-${faction}-geometry`);
      require(symbols.length === (representation === "shared-symbol" ? 2 : 0), "definition population");
      const protectedRoots = [...root.querySelectorAll("defs"),...ids.slice(1).map(id=>initialUnits.get(id))];
      const snapshots = protectedRoots.map(node=>({node,nodes:subtree(node),markup:node.outerHTML,parent:node.parentNode}));
      const stableTarget = subtree(target), retiredOutlines = [];
      const hit = (u,node) => {
        node.scrollIntoView({block:"center",inline:"center"});
        require(f.documentHit(u.x,u.y) === u.id, `semantic hit ${u.id}`);
      };
      const assertProtected = () => {
        const current = keyed();
        require(ids.slice(1).every(id=>current.get(id) === initialUnits.get(id)), "unrelated owner rebuilt");
        for (const snapshot of snapshots) {
          const actual = subtree(snapshot.node);
          require(snapshot.node.parentNode === snapshot.parent && root.contains(snapshot.node)
            && actual.length === snapshot.nodes.length && actual.every((n,i)=>n === snapshot.nodes[i])
            && snapshot.node.outerHTML === snapshot.markup, "unrelated subtree changed");
        }
      };
      const affectsProtected = record => protectedRoots.some(node => record.target === node || node.contains(record.target)
        || [...record.addedNodes,...record.removedNodes].some(changed=>changed === node || changed.contains(node)));
      observer = new w.MutationObserver(() => {});
      observer.observe(root,{subtree:true,childList:true,attributes:true,characterData:true,attributeOldValue:true});
      const assertNoProtectedMutation = records => require(records.filter(affectsProtected).length === 0, "unrelated mutation observed");
      const assertState = state => {
        require(rootOf() === root && root.querySelector("[data-fsgg-element-id='bench-camera']") === camera, "root/camera rebuilt");
        require(root.getAttribute("viewBox") === "0 0 410 510" && camera.getAttribute("transform") === "matrix(1 0 0 1 0 0)", "viewport/camera changed");
        require(JSON.stringify(units().map(n=>n.getAttribute("data-fsgg-element-id"))) === JSON.stringify(ids), "unit population/order");
        const current = keyed();
        for (const u of state) {
          const node = current.get(u.id), glyph = part(node,"glyph"), health = part(node,"health"), outline = part(node,"outline");
          require(node === initialUnits.get(u.id), "surviving unit owner rebuilt");
          require(node.getAttribute("transform") === `matrix(1 0 0 1 ${u.x} ${u.y})`, "unit translation");
          require(JSON.stringify([...node.children].map(n=>n.getAttribute("data-fsgg-element-id"))) ===
            JSON.stringify([`${u.id}/glyph`,`${u.id}/health`,...(u.selected ? [`${u.id}/outline`] : [])]), "part membership/order");
          const colour = u.faction ? "rgb(220 80 60)" : "rgb(70 130 220)";
          const circle = representation === "shared-symbol" ? definition(u.faction)?.querySelector("circle") : glyph?.querySelector("circle");
          require(circle && Number(circle.getAttribute("cx")) === 0 && Number(circle.getAttribute("cy")) === 0
            && Number(circle.getAttribute("r")) === 4 && circle.getAttribute("fill") === colour, "faction geometry/colour");
          if (representation === "shared-symbol") {
            const use = glyph?.querySelector("use"), symbol = definition(u.faction);
            require(symbol && use?.getAttribute("href") === `#${symbol.id}`, "faction reference");
            require(symbol.getAttribute("viewBox") === "-5 -5 10 10"
              && ["x","y","width","height"].map(n=>use.getAttribute(n)).join(" ") === "-5 -5 10 10", "symbol viewport");
          }
          const bars = [...(health?.querySelectorAll("rect") ?? [])];
          require(bars.length === 2 && bars.every((n,i)=>
            Number(n.getAttribute("x")) === -5 && Number(n.getAttribute("y")) === -9
            && Number(n.getAttribute("height")) === 2 && Number(n.getAttribute("width")) === (i ? u.health : 10)
            && n.getAttribute("fill") === (i ? "rgb(60 190 90)" : "rgb(60 60 60)")), "health geometry");
          const outlineRects = [...(outline?.querySelectorAll("rect") ?? [])];
          const edges = [[-6,-6,12,1],[-6,5,12,1],[-6,-5,1,10],[5,-5,1,10]];
          require(Boolean(outline) === u.selected && outlineRects.length === (u.selected ? 4 : 0)
            && outlineRects.every((n,i)=>["x","y","width","height"].every((a,j)=>Number(n.getAttribute(a)) === edges[i][j])
              && n.getAttribute("fill") === "rgb(250 210 40)"), "outline geometry/order");
          hit(u,node);
        }
        require(root.querySelectorAll("rect").length === 20 + (representation === "shared-symbol" ? 10 : 0) + (state[0].selected ? 4 : 0), "state rectangle population");
        require(root.querySelectorAll("circle").length === (representation === "shared-symbol" ? 2 : 10), "circle population");
        const actualTarget = subtree(target);
        require(stableTarget.every(n=>actualTarget.includes(n)), "surviving target descendant rebuilt");
        require(retiredOutlines.every(n=>!n.isConnected && !root.contains(n)), "removed outline retained");
        assertProtected();liveMatchesExport();
      };
      const controls = [], observations = [];
      // Each control mutates the actual observed DOM. Cleanup preserves the original failure.
      const kill = (name, detail, mutate, restore, check) => {
        let failure, killed = false;
        try {
          mutate();
          try { check(); } catch(error) {
            if (error.message !== `instance-appearance gate: ${detail}`) throw error;
            killed = true;
          }
          require(killed, `${name} mutant survived`);
        } catch(error) { failure = error; }
        finally {
          try { restore(); } catch(error) { failure ??= error; }
          observer.takeRecords();
        }
        if (failure) throw failure;
        controls.push({name,outcome:"killed"});
      };
      let priorOutline;
      for (const step of steps) {
        const state = stateFor(step);
        observer.takeRecords();
        const previousOutline = part(target,"outline");
        require(apply(state) === null, `${step.name} replacement refused`);
        const records = observer.takeRecords(), currentOutline = part(target,"outline");
        assertNoProtectedMutation(records);
        if (previousOutline && !step.selected) retiredOutlines.push(previousOutline);
        if (previousOutline && step.selected) require(currentOutline === previousOutline, "surviving outline rebuilt");
        assertState(state);
        if (step.name === "initial" || step.name === "identical-repeat") require(records.length === 0, "identical replacement mutated DOM");
        observations.push({name:step.name,target:{faction:step.faction,health:step.health,selected:step.selected,x:step.x,y:step.y},
          unitCount:10,unrelatedInstances:9,unrelatedMutations:0,rectangles:20+(representation === "shared-symbol" ? 10 : 0)+(step.selected ? 4 : 0),
          mutations:records.length,liveExportEqual:true,semanticHits:10});
        if (step.name === "faction") {
          const node = representation === "shared-symbol" ? part(target,"glyph").querySelector("use") : part(target,"glyph").querySelector("circle");
          const attribute = representation === "shared-symbol" ? "href" : "fill", actual = node.getAttribute(attribute);
          kill("stale-faction", representation === "shared-symbol" ? "faction reference" : "faction geometry/colour",
            ()=>node.setAttribute(attribute,representation === "shared-symbol" ? `#${definition(0).id}` : "rgb(70 130 220)"),
            ()=>node.setAttribute(attribute,actual), ()=>assertState(state));
        }
        if (step.name === "health-zero" || step.name === "health-ten") {
          const bar = part(target,"health").querySelectorAll("rect")[1], width = bar.getAttribute("width");
          kill(`stale-${step.name}`,"health geometry",()=>bar.setAttribute("width",step.health === 0 ? "6" : "0"),()=>bar.setAttribute("width",width),()=>assertState(state));
        }
        if (step.name === "outline-add") {
          priorOutline = currentOutline; const next = currentOutline.nextSibling;
          kill("stale-outline-absent","part membership/order",()=>target.removeChild(currentOutline),()=>target.insertBefore(currentOutline,next),()=>assertState(state));
        }
        if (step.name === "outline-remove") {
          kill("stale-outline-retained","part membership/order",()=>target.appendChild(priorOutline),()=>target.removeChild(priorOutline),()=>assertState(state));
        }
        if (step.name === "combined") {
          const neighbour = initialUnits.get(ids[1]), parent = neighbour.parentNode, next = neighbour.nextSibling, clone = neighbour.cloneNode(true);
          kill("clone-unrelated-instance","unrelated owner rebuilt",()=>neighbour.replaceWith(clone),()=>clone.replaceWith(neighbour),assertProtected);
          kill("remove-reinsert-unrelated-instance","unrelated mutation observed",
            ()=>{parent.removeChild(neighbour);parent.insertBefore(neighbour,next);},()=>{},()=>{assertProtected();assertNoProtectedMutation(observer.takeRecords());});
          kill("redundant-unrelated-write","unrelated mutation observed",()=>neighbour.setAttribute("transform",neighbour.getAttribute("transform")),()=>{},()=>assertNoProtectedMutation(observer.takeRecords()));
          kill("redundant-identical-write","identical replacement mutated DOM",()=>target.setAttribute("transform",target.getAttribute("transform")),()=>{},()=>require(observer.takeRecords().length === 0,"identical replacement mutated DOM"));
        }
        assertState(state);assertNoProtectedMutation(observer.takeRecords());
      }
      observer.disconnect();observer = null;
      representations.push({representation,steps:observations,controls,retiredOutlines:retiredOutlines.length,
        unrelatedSubtreesRetained:true,survivingTargetNodesRetained:true,identicalReplacementNoWrites:true});
    }
    result = {representations,scope:"finite functional compiled Replace; documentHit semantic routing; no painted-pixel, GPU or performance claim"};
  } catch(error) { firstFailure = error; }
  finally {
    observer?.disconnect();
    try {
      require(f.replaceOriginalDocument() === null, "original restoration refused");
      require(rootOf() === root && JSON.stringify(exported()) === original, "original restoration differs");
      liveMatchesExport();
    } catch(error) { firstFailure ??= error; }
  }
  if (firstFailure) throw firstFailure;
  return {...result,originalRestored:true,observersDisconnected:true};
}

// Uses the actual compiled document host; no renderer implementation is reproduced here.
export function documentReconciliationControl(w, representationOrder = "shared-first") {
  const f = w.svgFoundation, doc = w.document;
  const require = (condition, detail) => { if (!condition) throw new Error(`document-reconciliation gate: ${detail}`); };
  const rootOf = () => doc.querySelector("#document-fixture svg");
  const canonical = (node) => [node.namespaceURI, node.localName, [...node.attributes].map(a => [a.name,a.value]).sort((a,b) => a[0].localeCompare(b[0])), node.children.length ? null : node.textContent, [...node.children].map(canonical)];
  const exportTree = () => {
    const parsed = new w.DOMParser().parseFromString(f.documentExport(), "image/svg+xml");
    require(!parsed.querySelector("parsererror"), "export must be valid SVG");
    return canonical(parsed.documentElement);
  };
  const liveMatchesExport = () => require(JSON.stringify(canonical(rootOf())) === JSON.stringify(exportTree()), "live/export structure differs");
  const count = operation => countInstanceResearchUpdate(w,operation,require);
  // Track effects registered by the mounted document during this control only.
  const ep = w.EventTarget.prototype, add = ep.addEventListener, remove = ep.removeEventListener;
  const raf = w.requestAnimationFrame, cancel = w.cancelAnimationFrame, listeners = [], frames = new Set();
  const capture = options => typeof options === "boolean" ? options : !!options?.capture;
  ep.addEventListener = function(type,handler,options) { if (handler && !listeners.some(l => l.target === this && l.type === type && l.handler === handler && l.capture === capture(options))) listeners.push({target:this,type,handler,capture:capture(options)}); return add.call(this,type,handler,options); };
  ep.removeEventListener = function(type,handler,options) { const i = listeners.findIndex(l => l.target === this && l.type === type && l.handler === handler && l.capture === capture(options)); if(i >= 0) listeners.splice(i,1); return remove.call(this,type,handler,options); };
  w.requestAnimationFrame = callback => { let id; id = raf.call(w,now => {frames.delete(id);callback(now);}); frames.add(id);return id; };
  w.cancelAnimationFrame = id => {frames.delete(id);return cancel.call(w,id);};
  try {
    f.documentMount();
    const root = rootOf(), initialExport = f.documentExport(), initialTree = exportTree();
    const elements = new Map([...root.querySelectorAll("[data-fsgg-element-id]")].map(n => [n.getAttribute("data-fsgg-element-id"),n]));
    const definitions = new Map([...root.querySelectorAll("defs [id]")].map(n => [n.id,{node:n,markup:n.outerHTML}]));
    const order = () => [...root.children].filter(n => n.hasAttribute("data-fsgg-element-id")).map(n => n.getAttribute("data-fsgg-element-id"));
    const originalOrder = order();
    const identity = () => {
      require(rootOf() === root, "root rebuilt");
      require([...elements].every(([id,node]) => root.querySelector(`[data-fsgg-element-id='${w.CSS.escape(id)}']`) === node), "element rebuilt");
      require([...definitions].every(([id,value]) => doc.getElementById(id) === value.node && value.node.outerHTML === value.markup), "definition rebuilt/changed");
    };
    const accepted = operation => { const observation = count(operation);require(observation.value === null, "valid candidate refused");require(observation.unchangedWrites === 0, "unnecessary attribute write");identity();liveMatchesExport();return observation; };
    liveMatchesExport();
    const noOp = accepted(() => f.replaceOriginalDocument());require(noOp.writes === 0 && noOp.removals === 0 && noOp.childMoves === 0 && f.documentExport() === initialExport, "no-op changed accepted document");
    const changed = accepted(() => f.replaceChangedDocument());require(changed.changedWrites === 1 && changed.childMoves === 0, "changed transform must be written");
    const target = elements.get("luminance-element");require(target, "transform target missing");
    const matrix = target.transform.baseVal.consolidate()?.matrix;
    require(matrix && matrix.a === 1 && matrix.b === 0 && matrix.c === 0 && matrix.d === 1 && matrix.e === 3 && matrix.f === 0, "transform change incorrect");
    const repeat = accepted(() => f.replaceChangedDocument());require(repeat.writes === 0 && repeat.removals === 0 && repeat.childMoves === 0, "repeated snapshot writes attributes");
    root.setAttribute("data-control-stale", "obsolete");target.setAttribute("transform", "translate(999 999)");
    const repair = accepted(() => f.replaceChangedDocument());require(repair.changedWrites === 1 && repair.removals === 1 && repair.childMoves === 0 && !root.hasAttribute("data-control-stale"), "stale attribute removal/value repair");
    const reordered = accepted(() => f.replaceReorderedDocument());require(JSON.stringify(order()) === JSON.stringify([...originalOrder].reverse()), "sibling order incorrect");
    const restored = accepted(() => f.replaceOriginalDocument());require(JSON.stringify(exportTree()) === JSON.stringify(initialTree), "restored content differs");
    const beforeInvalid = root.outerHTML, exportBeforeInvalid = f.documentExport();
    const invalid = count(() => f.replaceInvalidDocument());require(String(invalid.value).includes("duplicate-id") && invalid.writes === 0 && invalid.removals === 0 && invalid.childMoves === 0 && root.outerHTML === beforeInvalid && f.documentExport() === exportBeforeInvalid, "invalid candidate partially mutated state");identity();
    const expectKilled = (name, expectedDetail, operation) => { let failure;try { operation(); } catch(error) { failure=error.message; } require(failure === `document-reconciliation gate: ${expectedDetail}`, `${name} mutant survived`);return {name,outcome:"killed"}; };
    // Mutation of the observed live surface proves the identity gate is effective.
    // Exercise the fallback through the compiled producer's public Replace API.
    const structural = [], structuralMutants = [];
    const keyed = () => new Map([...root.querySelectorAll("[data-fsgg-element-id]")].map(n => [n.getAttribute("data-fsgg-element-id"),n]));
    const expectedOrder = ids => require(JSON.stringify(order()) === JSON.stringify(ids), "structural order incorrect");
    const replaceStructure = (mode, ids) => {
      const previous = keyed(), observation = count(() => f.replaceReconciliationDocument(mode));
      require(observation.value === null && observation.unchangedWrites === 0, "structural candidate refused or unchanged attributes written");
      require(rootOf() === root, "structural root rebuilt");
      expectedOrder(ids);liveMatchesExport();
      const now = keyed();
      for(const [id,node] of previous) {
        if(now.has(id))require(now.get(id) === node, "structural survivor rebuilt");
        else require(!node.isConnected, "removed node retained");
      }
      structural.push({mode, ids, ...observation});return now;
    };
    const initialIds = ["control-a","control-b","control-c"];
    replaceStructure("original",initialIds);
    // Same-order changes must preserve the actual topmost painted target.
    const topmost = expected => {
      root.scrollIntoView({block:"center",inline:"center"});const m=root.getScreenCTM();
      require(m, "structural screen matrix missing");
      const target=doc.elementFromPoint(m.a*20+m.c*20+m.e,m.b*20+m.d*20+m.f)?.closest("[data-fsgg-semantic-id]")?.getAttribute("data-fsgg-semantic-id");
      require(target === expected, "structural topmost incorrect");return target;
    };
    const topmostOriginal=topmost("control-c");
    replaceStructure("reverse",[...initialIds].reverse());const topmostReverse=topmost("control-a");
    const misplaced=keyed().get("control-c");root.appendChild(misplaced);
    structuralMutants.push(expectKilled("ignore-reorder","structural order incorrect",()=>expectedOrder([...initialIds].reverse())));
    replaceStructure("reverse",[...initialIds].reverse());
    replaceStructure("original",initialIds);
    replaceStructure("insert",["control-first","control-a","control-middle","control-b","control-c","control-last"]);
    replaceStructure("original",initialIds);
    for(const [mode,ids] of [["remove-first",["control-b","control-c"]],["remove-middle",["control-a","control-c"]],["remove-last",["control-a","control-b"]]]) {
      replaceStructure(mode,ids);replaceStructure("original",initialIds);
    }
    const owner=keyed().get("control-a"),oldPrimitive=owner.firstElementChild;
    replaceStructure("tag",initialIds);const primitive=owner.firstElementChild;
    require(primitive.localName !== oldPrimitive.localName && primitive !== oldPrimitive && !oldPrimitive.isConnected, "incompatible tag not replaced");
    primitive.replaceWith(oldPrimitive);
    structuralMutants.push(expectKilled("ignore-incompatible-tag","live/export structure differs",liveMatchesExport));
    replaceStructure("tag",initialIds);
    const removedClone=keyed().get("control-a").cloneNode(true);replaceStructure("remove-first",["control-b","control-c"]);root.appendChild(removedClone);
    structuralMutants.push(expectKilled("retain-removed-node","live/export structure differs",liveMatchesExport));
    replaceStructure("remove-first",["control-b","control-c"]);
    replaceStructure("unkeyed",["control-unkeyed"]);const unkeyedOwner=keyed().get("control-unkeyed"),wrappers=[...unkeyedOwner.children];
    require(wrappers.length === 2 && wrappers.every(n=>!n.hasAttribute("data-fsgg-node")&&!n.id), "unkeyed wrappers unavailable");
    const unkeyedChanged=replaceStructure("unkeyed-changed",["control-unkeyed"]);
    require(wrappers.every((n,i)=>unkeyedChanged.get("control-unkeyed").children[i] === n), "unkeyed positional survivor rebuilt");
    require(structural.at(-1).childMoves === 0, "aligned unkeyed children moved");
    wrappers[0].setAttribute("transform","translate(999 999)");
    structuralMutants.push(expectKilled("skip-changed-attribute","live/export structure differs",liveMatchesExport));
    replaceStructure("unkeyed-changed",["control-unkeyed"]);
    const structuralResult={steps:structural,mutants:structuralMutants,topmostOriginal,topmostReverse,unkeyedWrappersRetained:true};
    const definitionIsolation = definitionIsolationControl(w);
    const instanceAppearance = instanceAppearanceControl(w);
    const instanceResearch = instanceResearchControl(f,rootOf,liveMatchesExport,require,count,()=>w.performance.now(),representationOrder);
    validateInstanceResearchResult(instanceResearch,representationOrder);
    require(f.replaceOriginalDocument() === null, "restore original after instance research");liveMatchesExport();
    require(rootOf() === root && JSON.stringify(exportTree()) === JSON.stringify(initialTree), "instance research restoration differs");
    const clone = root.cloneNode(true);root.replaceWith(clone);const reconstruction = expectKilled("full-reconstruction","root rebuilt",identity);clone.replaceWith(root);f.documentDispose();
    const disposedRoots = [];
    for(let cycle=0;cycle<5;cycle++) { f.documentMount();require(rootOf(), "remount failed after namespace release");disposedRoots.push(f.documentDispose());require(!rootOf(), "disposed root retained"); }
    require(disposedRoots.every(n => n === 0) && listeners.length === 0 && frames.size === 0, "document effects retained after disposal");
    return { noOp, changed, repeat, repair, reordered, restored, invalid, reconstruction, structural:structuralResult, definitionIsolation, instanceAppearance, instanceResearch, disposedRoots, ownedListeners:listeners.length, ownedFrames:frames.size, scope:"effects registered after fixture bootstrap; foundation controls excluded", countersRestored:true };
  } finally {
    f.documentDispose();
    for(const id of frames)cancel.call(w,id);
    for(const l of listeners)remove.call(l.target,l.type,l.handler,l.capture);
    ep.addEventListener = add;ep.removeEventListener = remove;w.requestAnimationFrame = raf;w.cancelAnimationFrame = cancel;
  }
}

export function countInstanceResearchUpdate(w, operation, require) {
    const ep = w.Element.prototype, set = ep.setAttribute, remove = ep.removeAttribute;
    const writes = [], removals = [], childMoves = [], childRemovals = [];
    const np = w.Node.prototype, append = np.appendChild, insert = np.insertBefore, removeChild = np.removeChild;
    np.removeChild = function(child) { childRemovals.push(child);return removeChild.call(this,child); };
    np.appendChild = function(child) { childMoves.push({kind:"append", alreadyOwned:child.parentNode === this});return append.call(this,child); };
    np.insertBefore = function(child,before) { childMoves.push({kind:"insert", alreadyOwned:child.parentNode === this});return insert.call(this,child,before); };
    ep.setAttribute = function(name,value) { writes.push({ name, changed: this.getAttribute(name) !== String(value) }); return set.call(this,name,value); };
    ep.removeAttribute = function(name) { removals.push(name); return remove.call(this,name); };
    let value;
    try { value = operation(); } finally { ep.setAttribute = set; ep.removeAttribute = remove; np.appendChild = append; np.insertBefore = insert; np.removeChild = removeChild; }
    require(ep.setAttribute === set && ep.removeAttribute === remove && np.appendChild === append && np.insertBefore === insert && np.removeChild === removeChild, "native counters not restored");
    return { value, writes: writes.length, changedWrites: writes.filter(v => v.changed).length, unchangedWrites: writes.filter(v => !v.changed).length, removals: removals.length, childRemovals:childRemovals.length, childMoves:childMoves.length, alreadyOwnedMoves:childMoves.filter(v=>v.alreadyOwned).length };
}
