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
    const writes = [], removals = [];
    ep.setAttribute = function(name,value) { writes.push({ name, changed: this.getAttribute(name) !== String(value) }); return set.call(this,name,value); };
    ep.removeAttribute = function(name) { removals.push(name); return remove.call(this,name); };
    let value;
    try { value = operation(); } finally { ep.setAttribute = set; ep.removeAttribute = remove; }
    require(ep.setAttribute === set && ep.removeAttribute === remove, "attribute counters not restored");
    return { value, writes: writes.length, changedWrites: writes.filter(v => v.changed).length, unchangedWrites: writes.filter(v => !v.changed).length, removals: removals.length };
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
    const noOp = accepted(() => f.replaceOriginalDocument());require(noOp.writes === 0 && noOp.removals === 0 && f.documentExport() === initialExport, "no-op changed accepted document");
    const changed = accepted(() => f.replaceChangedDocument());require(changed.changedWrites === 1, "changed transform must be written");
    const target = elements.get("luminance-element");require(target, "transform target missing");
    const matrix = target.transform.baseVal.consolidate()?.matrix;
    require(matrix && matrix.a === 1 && matrix.b === 0 && matrix.c === 0 && matrix.d === 1 && matrix.e === 3 && matrix.f === 0, "transform change incorrect");
    const repeat = accepted(() => f.replaceChangedDocument());require(repeat.writes === 0 && repeat.removals === 0, "repeated snapshot writes attributes");
    root.setAttribute("data-control-stale", "obsolete");target.setAttribute("transform", "translate(999 999)");
    const repair = accepted(() => f.replaceChangedDocument());require(repair.changedWrites === 1 && repair.removals === 1 && !root.hasAttribute("data-control-stale"), "stale attribute removal/value repair");
    const reordered = accepted(() => f.replaceReorderedDocument());require(JSON.stringify(order()) === JSON.stringify([...originalOrder].reverse()), "sibling order incorrect");
    const restored = accepted(() => f.replaceOriginalDocument());require(JSON.stringify(exportTree()) === JSON.stringify(initialTree), "restored content differs");
    const beforeInvalid = root.outerHTML, exportBeforeInvalid = f.documentExport();
    const invalid = count(() => f.replaceInvalidDocument());require(String(invalid.value).includes("duplicate-id") && invalid.writes === 0 && invalid.removals === 0 && root.outerHTML === beforeInvalid && f.documentExport() === exportBeforeInvalid, "invalid candidate partially mutated state");identity();
    const expectKilled = (name, operation) => { let failure;try { operation(); } catch(error) { failure=error.message; } require(failure?.includes("document-reconciliation gate"), `${name} mutant survived`);return {name,outcome:"killed"}; };
    // Mutation of the observed live surface proves the identity gate is effective.
    const clone = root.cloneNode(true);root.replaceWith(clone);const reconstruction = expectKilled("full-reconstruction",identity);clone.replaceWith(root);f.documentDispose();
    const disposedRoots = [];
    for(let cycle=0;cycle<5;cycle++) { f.documentMount();require(rootOf(), "remount failed after namespace release");disposedRoots.push(f.documentDispose());require(!rootOf(), "disposed root retained"); }
    require(disposedRoots.every(n => n === 0) && listeners.length === 0 && frames.size === 0, "document effects retained after disposal");
    return { noOp, changed, repeat, repair, reordered, restored, invalid, reconstruction, disposedRoots, ownedListeners:listeners.length, ownedFrames:frames.size, scope:"effects registered after fixture bootstrap; foundation controls excluded", countersRestored:true };
  } finally {
    f.documentDispose();
    for(const id of frames)cancel.call(w,id);
    for(const l of listeners)remove.call(l.target,l.type,l.handler,l.capture);
    ep.addEventListener = add;ep.removeEventListener = remove;w.requestAnimationFrame = raf;w.cancelAnimationFrame = cancel;
  }
}
