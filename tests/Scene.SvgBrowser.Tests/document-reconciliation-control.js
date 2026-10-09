import researchCorpus from "./instance-research-command-streams.json" with { type: "json" };

// Pure command oracle; it does not implement or time the production renderer.
export function instanceResearchStates(corpus = researchCorpus, count = 100) {
  const ensure = (ok, detail) => { if (!ok) throw Error(`instance corpus: ${detail}`); };
  ensure(corpus.schema === "svg-instance-command-streams/1" && corpus.seed === 1729, "identity");
  ensure(corpus.counts.includes(count) && [100,250].includes(count), "count");
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
    states.push({name:command.name,revision,radius:definition.radius,rgb:[...definition.rgb],instances:instances.map(v=>({...v}))});
  }
  return states;
}

// Compare independently observed geometry/style/layer descriptors with the command oracle.
export function assertInstanceResearchSemantics(expected, observed) {
  if (JSON.stringify(expected) !== JSON.stringify(observed)) throw Error("instance corpus: semantic mismatch");
}

function instanceResearchControl(f, rootOf, liveMatchesExport, require) {
  const results = [], mountedRoot = rootOf();
  for (const count of researchCorpus.counts) {
    const states = instanceResearchStates(researchCorpus,count);
    for (const variant of researchCorpus.variants) {
      let previous = new Map();
      for (const state of states) {
        const error = f.replaceInstanceResearchDocument(variant,state.revision,state.instances.map(v=>v.id),state.instances.map(v=>v.x),state.instances.map(v=>v.y));
        require(error === null, `instance ${count}/${variant}/${state.name}: ${error}`);
        liveMatchesExport();
        const root = rootOf();
        require(root === mountedRoot, "instance root rebuilt");
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
          }
          require(f.documentHit(v.x,v.y) === v.id, "instance hit identity");
          require(node.getAttribute("transform") === `matrix(1 0 0 1 ${v.x} ${v.y})`, "instance transform");
          return {id:v.id,radius:Number(circle.getAttribute("r")),fill:circle.getAttribute("fill")};
        });
        const expected = state.instances.map(v=>({id:v.id,radius:state.radius,fill:`rgb(${state.rgb.join(" ")})`}));
        assertInstanceResearchSemantics(expected,observed);
        previous = current;
        results.push({count,variant,command:state.name,instances:elements.length});
      }
    }
  }
  return results;
}

// Uses the actual compiled document host; no renderer implementation is reproduced here.
export function documentReconciliationControl(w) {
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
  const count = (operation) => {
    const ep = w.Element.prototype, set = ep.setAttribute, remove = ep.removeAttribute;
    const writes = [], removals = [], childMoves = [];
    const np = w.Node.prototype, append = np.appendChild, insert = np.insertBefore;
    np.appendChild = function(child) { childMoves.push({kind:"append", alreadyOwned:child.parentNode === this});return append.call(this,child); };
    np.insertBefore = function(child,before) { childMoves.push({kind:"insert", alreadyOwned:child.parentNode === this});return insert.call(this,child,before); };
    ep.setAttribute = function(name,value) { writes.push({ name, changed: this.getAttribute(name) !== String(value) }); return set.call(this,name,value); };
    ep.removeAttribute = function(name) { removals.push(name); return remove.call(this,name); };
    let value;
    try { value = operation(); } finally { ep.setAttribute = set; ep.removeAttribute = remove; np.appendChild = append; np.insertBefore = insert; }
    require(ep.setAttribute === set && ep.removeAttribute === remove && np.appendChild === append && np.insertBefore === insert, "native counters not restored");
    return { value, writes: writes.length, changedWrites: writes.filter(v => v.changed).length, unchangedWrites: writes.filter(v => !v.changed).length, removals: removals.length, childMoves:childMoves.length, alreadyOwnedMoves:childMoves.filter(v=>v.alreadyOwned).length };
  };
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
    const instanceResearch = instanceResearchControl(f,rootOf,liveMatchesExport,require);
    require(instanceResearch.length === 24, "instance research report coverage");
    require(f.replaceOriginalDocument() === null, "restore original after instance research");liveMatchesExport();
    require(rootOf() === root && JSON.stringify(exportTree()) === JSON.stringify(initialTree), "instance research restoration differs");
    const clone = root.cloneNode(true);root.replaceWith(clone);const reconstruction = expectKilled("full-reconstruction","root rebuilt",identity);clone.replaceWith(root);f.documentDispose();
    const disposedRoots = [];
    for(let cycle=0;cycle<5;cycle++) { f.documentMount();require(rootOf(), "remount failed after namespace release");disposedRoots.push(f.documentDispose());require(!rootOf(), "disposed root retained"); }
    require(disposedRoots.every(n => n === 0) && listeners.length === 0 && frames.size === 0, "document effects retained after disposal");
    return { noOp, changed, repeat, repair, reordered, restored, invalid, reconstruction, structural:structuralResult, instanceResearch, disposedRoots, ownedListeners:listeners.length, ownedFrames:frames.size, scope:"effects registered after fixture bootstrap; foundation controls excluded", countersRestored:true };
  } finally {
    f.documentDispose();
    for(const id of frames)cancel.call(w,id);
    for(const l of listeners)remove.call(l.target,l.type,l.handler,l.capture);
    ep.addEventListener = add;ep.removeEventListener = remove;w.requestAnimationFrame = raf;w.cancelAnimationFrame = cancel;
  }
}
