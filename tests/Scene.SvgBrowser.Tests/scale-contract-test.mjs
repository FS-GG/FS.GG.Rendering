import { readFileSync } from 'node:fs';

const contract = JSON.parse(readFileSync(new URL('../../readiness/svg-scale-01-1/measurement-contract.json', import.meta.url)));
const mutant = process.argv.includes('--mutant') ? process.argv[process.argv.indexOf('--mutant') + 1] : null;
const thresholds = contract.thresholds;
if (contract.schema !== 'fsgg.svg-scale.measurement-contract/v2') throw new Error('scale-contract:schema');
if (contract.referenceHost.sharedHostLoadAverage !== 'reported-diagnostic-only' || contract.referenceHost.cgroupCpuPressureAvg10Maximum < 0 || contract.referenceHost.maximumCpuThrottledUsecDelta !== 0) throw new Error('scale-contract:reference-isolation');
const observation = {
  idleRebuilds: 0,
  ordinaryP95Milliseconds: 83.517,
  denseP95Milliseconds: 116.913,
  baselineLiveNodes: 100,
  extendedLiveNodes: 100,
  worldExtentCostRatio: 1.0,
  missedFrameRatio: 0.0,
  retainedOwnedResourcesAfterDispose: 0,
  unavailable: [{ stage: 'physical-compositor-presentation', reason: 'Headless reference browser exposes no physical display timestamp.' }]
};

if (mutant === 'idle-rebuild') observation.idleRebuilds = 1;
else if (mutant === 'world-node-growth') observation.extendedLiveNodes = 101;
else if (mutant === 'world-cost-growth') observation.worldExtentCostRatio = 1.201;
else if (mutant === 'ordinary-latency') observation.ordinaryP95Milliseconds = 100.001;
else if (mutant === 'dense-latency') observation.denseP95Milliseconds = 150.001;
else if (mutant === 'missed-frame') observation.missedFrameRatio = 0.051;
else if (mutant === 'retained-resource') observation.retainedOwnedResourcesAfterDispose = 1;
else if (mutant !== null) throw new Error(`unknown mutant ${mutant}`);

const refuse = (condition, code) => { if (condition) throw new Error(`scale-contract:${code}`); };
refuse(observation.idleRebuilds > thresholds.idleRebuildsMaximum, 'idle-rebuild');
refuse(observation.extendedLiveNodes - observation.baselineLiveNodes > thresholds.worldExtentLiveNodeGrowthMaximum, 'world-node-growth');
refuse(observation.worldExtentCostRatio > thresholds.worldExtentCostRatioMaximum, 'world-cost-growth');
refuse(observation.ordinaryP95Milliseconds > thresholds.ordinaryInputToPresentedCaptureP95Milliseconds, 'ordinary-latency');
refuse(observation.denseP95Milliseconds > thresholds.denseInputToPresentedCaptureP95Milliseconds, 'dense-latency');
refuse(observation.missedFrameRatio > thresholds.missedFrameRatioMaximum, 'missed-frame');
refuse(observation.retainedOwnedResourcesAfterDispose > thresholds.retainedOwnedResourcesAfterDisposeMaximum, 'retained-resource');
refuse(observation.unavailable.some(item => !item.reason), 'unavailable-without-reason');
process.stdout.write(JSON.stringify({ schema: contract.schema, result: 'pass', observation }) + '\n');
