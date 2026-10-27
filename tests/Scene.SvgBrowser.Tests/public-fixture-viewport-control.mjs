// Evaluated verbatim in each real software browser against the package-backed F# fixture.
// No renderer, timing threshold, heap estimate, or document simulation revision is modeled here.
export function publicFixtureViewportControl(w = window) {
  const f = w.svgFoundation, doc = w.document, container = doc.querySelector('#document-fixture');
  const require = (ok, detail) => { if (!ok) throw Error(`public fixture viewport: ${detail}`); };
  const rootOf = () => container.querySelector('svg');
  const nodeOf = (root, id) => [...root.querySelectorAll('[data-fsgg-element-id]')].find(n => n.getAttribute('data-fsgg-element-id') === id);
  const definitions = root => [...root.querySelectorAll('defs > *')];
  const treeNodes = node => [node, ...node.querySelectorAll('*')];
  const canonical = node => [node.namespaceURI, node.localName,
    [...node.attributes].map(a => [a.name, a.value]).sort((a, b) => a[0].localeCompare(b[0])),
    node.children.length ? null : node.textContent, [...node.children].map(canonical)];
  const firstDifference = (expected, actual, path = []) => {
    if (Array.isArray(expected) && Array.isArray(actual)) {
      for (let i = 0; i < Math.max(expected.length, actual.length); i++) {
        const difference = firstDifference(expected[i], actual[i], [...path, i]);
        if (difference) return difference;
      }
      return null;
    }
    return expected === actual ? null : { path, exported: expected ?? null, live: actual ?? null };
  };
  const idsOf = root => [...nodeOf(root, 'bench-camera').children].map(n => n.getAttribute('data-fsgg-element-id'));
  const counts = root => ({ nodes: root.querySelectorAll('*').length + 1, definitions: definitions(root).length, listeners: listeners.size, frames: pending.size });
  const baseline = [{ id: 'viewport-stable', x: 60, y: 100 }, { id: 'viewport-edge', x: 470, y: 100 }];
  let active = false, listeners = new Set(), pending = new Set();
  const add = w.EventTarget.prototype.addEventListener, remove = w.EventTarget.prototype.removeEventListener;
  const raf = w.requestAnimationFrame, cancel = w.cancelAnimationFrame;
  // Scope instrumentation to synchronous calls into this document host. Its source owns no
  // listeners or frame loop; a future scheduling regression must fail this witness.
  w.EventTarget.prototype.addEventListener = function(type, callback, options) {
    if (active) listeners.add(callback);
    return add.call(this, type, callback, options);
  };
  w.EventTarget.prototype.removeEventListener = function(type, callback, options) {
    if (active) listeners.delete(callback);
    return remove.call(this, type, callback, options);
  };
  w.requestAnimationFrame = function(callback) {
    const id = raf.call(w, timestamp => { pending.delete(id); callback(timestamp); });
    if (active) pending.add(id);
    return id;
  };
  w.cancelAnimationFrame = function(id) { pending.delete(id); return cancel.call(w, id); };
  const ownedCall = action => { active = true; try { return action(); } finally { active = false; } };
  const apply = (variant, units, x = 0, zoom = 1) => require(ownedCall(() => f.replacePublicFixtureViewport(variant, zoom, x, 0, units.map(u => u.id), units.map(u => u.x), units.map(u => u.y))) === null, 'Replace refused');
  const exportDocument = () => {
    const parsed = new w.DOMParser().parseFromString(f.documentExport(), 'image/svg+xml');
    require(!parsed.querySelector('parsererror'), 'export parse');
    return parsed.documentElement;
  };
  const rejected = (name, mutate, inspect, restore) => {
    mutate(); let caught = false;
    try { inspect(); } catch (error) { caught = error.message.startsWith('public fixture viewport:'); }
    finally { restore(); }
    require(caught, `causal control did not reject ${name}`);
    try { inspect(); } catch (error) { throw Error(`public fixture viewport: control restoration ${name}: ${error.message}`); }
    return name;
  };
  const results = [];
  try {
    // Instrument positive controls use the same hooks, then restore their known resources.
    const target = doc.createElement('span'), callback = () => {};
    let probeFrame;
    ownedCall(() => { target.addEventListener('witness', callback); probeFrame = w.requestAnimationFrame(callback); });
    require(listeners.size === 1 && pending.size === 1, 'resource instrument positive control');
    ownedCall(() => { target.removeEventListener('witness', callback); w.cancelAnimationFrame(probeFrame); });
    require(listeners.size === 0 && pending.size === 0, 'resource instrument cleanup control');
    for (const variant of ['shared-symbol', 'expanded-geometry']) {
      ownedCall(() => f.documentMount());
      apply(variant, baseline);
      const root = rootOf(); root.scrollIntoView({ block: 'start' });
      const stable = nodeOf(root, baseline[0].id), stableGlyph = nodeOf(root, `${baseline[0].id}/glyph`);
      const defs = definitions(root), initial = counts(root);
      const unaffected = [...baseline.map(u => nodeOf(root, u.id)), ...defs].map(node => ({ node, descendants: treeNodes(node) }));
      const inspect = (units, x = 0, zoom = 1, removed = []) => {
        require(rootOf() === root, 'root reconstructed');
        require(nodeOf(root, baseline[0].id) === stable && nodeOf(root, `${baseline[0].id}/glyph`) === stableGlyph, 'unaffected node reconstructed');
        require(JSON.stringify(idsOf(root)) === JSON.stringify(units.map(u => u.id)), 'population includes stale removed nodes');
        for (const retained of unaffected) {
          const current = treeNodes(retained.node);
          require(retained.node.isConnected && current.length === retained.descendants.length
            && current.every((node, i) => node === retained.descendants[i]), 'unaffected descendant reconstructed');
        }
        require(definitions(root).length === defs.length && definitions(root).every((n, i) => n === defs[i]), 'definition stale/reconstructed');
        require(nodeOf(root, 'bench-camera').getAttribute('transform') === `matrix(${zoom} 0 0 ${zoom} ${x} 0)`, 'camera geometry');
        for (const unit of units) {
          const node = nodeOf(root, unit.id), glyph = nodeOf(root, `${unit.id}/glyph`);
          require(node?.getAttribute('transform') === `matrix(1 0 0 1 ${unit.x} ${unit.y})`, 'unit geometry');
          require(w.getComputedStyle(node).visibility === 'visible' && w.getComputedStyle(node).display !== 'none', 'wrong visibility');
          const circle = variant === 'shared-symbol' ? root.querySelector('defs circle') : glyph.querySelector('circle');
          require(circle?.getAttribute('r') === '4', 'circle geometry');
          const px = unit.x * zoom + x, py = unit.y * zoom;
          const visible = px > 4 && px < 406 && py > 4 && py < 506;
          // HitTest takes SVG viewport coordinates and performs native geometry picking.
          require(f.documentHit(px, py) === (visible ? unit.id : null), `viewport hit/visibility ${unit.id} at ${px},${py}`);
        }
        for (const old of removed) {
          require(!old.node.isConnected && !nodeOf(root, old.id), 'removed instance owned DOM');
          require(f.documentHit(old.x, old.y) !== old.id, 'removed instance hit');
        }
        const exported = exportDocument();
        require(JSON.stringify(idsOf(exported)) === JSON.stringify(units.map(u => u.id)), 'full accepted export population');
        const exportDifference = firstDifference(canonical(exported), canonical(root));
        require(!exportDifference, `full accepted export geometry/definitions: ${JSON.stringify(exportDifference)}`);
        require(counts(root).listeners === 0 && counts(root).frames === 0, 'document owned resources');
      };
      inspect(baseline);
      const controls = [];
      // Mutate the actual browser DOM while accepted host state stays fixed.
      const extra = stable.cloneNode(true);
      controls.push(rejected('stale-node', () => stable.parentNode.append(extra), () => inspect(baseline), () => extra.remove()));
      const extraDef = doc.createElementNS('http://www.w3.org/2000/svg', 'symbol'); extraDef.id = 'stale-definition-control';
      const defsContainer = root.querySelector('defs');
      controls.push(rejected('stale-definition', () => defsContainer.append(extraDef), () => inspect(baseline), () => extraDef.remove()));
      const clone = stable.cloneNode(true);
      controls.push(rejected('reconstruction', () => stable.replaceWith(clone), () => inspect(baseline), () => clone.replaceWith(stable)));
      // Mutate SVG presentation directly: CSSOM style access may materialize an empty
      // style attribute after removal, which would itself contaminate export equality.
      const priorVisibility = stable.getAttribute('visibility');
      controls.push(rejected('wrong-visibility', () => stable.setAttribute('visibility', 'hidden'), () => inspect(baseline), () => {
        if (priorVisibility === null) stable.removeAttribute('visibility'); else stable.setAttribute('visibility', priorVisibility);
      }));
      for (const [name, descendant] of [
        ['health-descendant-reconstruction', nodeOf(root, `${baseline[0].id}/health`).firstElementChild],
        ['offscreen-descendant-reconstruction', nodeOf(root, `${baseline[1].id}/glyph`).firstElementChild],
        ...(defs.length ? [['definition-descendant-reconstruction', defs[0].firstElementChild]] : []),
      ]) {
        const descendantClone = descendant.cloneNode(true);
        controls.push(rejected(name, () => descendant.replaceWith(descendantClone), () => inspect(baseline), () => descendantClone.replaceWith(descendant)));
      }
      const originalExport = f.documentExport;
      controls.push(rejected('truncated-export', () => {
        f.documentExport = () => {
          const exported = new w.DOMParser().parseFromString(originalExport(), 'image/svg+xml');
          // Preserve every instance ID while dropping off-viewport accepted glyph content.
          nodeOf(exported.documentElement, `${baseline[1].id}/glyph`).replaceChildren();
          return new w.XMLSerializer().serializeToString(exported);
        };
      }, () => inspect(baseline), () => { f.documentExport = originalExport; }));
      if (defs.length) {
        const definitionClone = defs[0].cloneNode(true);
        controls.push(rejected('definition-reconstruction', () => defs[0].replaceWith(definitionClone), () => inspect(baseline), () => definitionClone.replaceWith(defs[0])));
      }
      const samples = [], burstSamples = [];
      for (let cycle = 0; cycle < 6; cycle++) {
        const spawned = Array.from({ length: 4 }, (_, i) => ({ id: `viewport-burst-${cycle}-${i}`, x: 130 + i * 30, y: 160 }));
        const population = [...baseline, ...spawned];
        apply(variant, population); inspect(population);
        require(counts(root).nodes > initial.nodes, 'spawn count positive control');
        burstSamples.push(counts(root));
        require(JSON.stringify(burstSamples.at(-1)) === JSON.stringify(burstSamples[0]), 'bounded burst resources');
        const removed = spawned.map(u => ({ ...u, node: nodeOf(root, u.id) }));
        apply(variant, baseline); inspect(baseline, 0, 1, removed);
        require(JSON.stringify(counts(root)) === JSON.stringify(initial), 'bounded return population resources');
        // Full export retains the off-viewport unit; camera changes only presentation geometry.
        apply(variant, baseline, -400); inspect(baseline, -400);
        apply(variant, baseline, -800, 2); inspect(baseline, -800, 2);
        apply(variant, baseline); inspect(baseline);
        require(JSON.stringify(counts(root)) === JSON.stringify(initial), 'bounded camera resources');
        samples.push(counts(root));
      }
      // Definitions removed by a representation transition must detach, too.
      apply(variant === 'shared-symbol' ? 'expanded-geometry' : 'shared-symbol', baseline);
      if (variant === 'shared-symbol') require(defs.every(n => !n.isConnected) && definitions(root).length === 0, 'removed definitions retained');
      ownedCall(() => f.documentDispose());
      require(!root.isConnected && !rootOf() && defs.every(n => !n.isConnected), 'disposal owned DOM/definitions');
      require(listeners.size === 0 && pending.size === 0, 'disposal owned resources');
      results.push({ variant, cycles: 6, population: { baseline: 2, maximum: 6 }, initial, samples, burstSamples, controls, disposed: { nodes: container.querySelectorAll('svg *').length, listeners: listeners.size, frames: pending.size } });
    }
    return { result: 'pass', representations: results, resourceControl: 'known listener and frame detected then removed', revisionContract: 'document host has no simulation revision; retained-host cameraRevisionIsolation is measured separately', scope: 'software DOM/geometry/hit/export correctness; owned synchronous document-host resources, not heap or GPU memory' };
  } finally {
    ownedCall(() => f.documentDispose());
    for (const id of pending) cancel.call(w, id);
    w.EventTarget.prototype.addEventListener = add; w.EventTarget.prototype.removeEventListener = remove;
    w.requestAnimationFrame = raf; w.cancelAnimationFrame = cancel;
  }
}
