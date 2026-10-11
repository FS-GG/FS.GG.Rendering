// Small shared original-reserve join, used by the actual hosted runner.
export async function settlePendingCreation(promise, status, deadline, wait) {
  try { await wait(promise, deadline, 'pending creation unresolved in original reserve', false); return null; }
  catch (error) { return {cause:String(error.message), unknown:status().endsWith('pending')}; }
}
