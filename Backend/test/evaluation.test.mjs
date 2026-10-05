import test from 'node:test';
import assert from 'node:assert/strict';
import { criterionIds, validateEvaluation, evaluateCommunication } from '../evaluation.mjs';

const transcript = [{ role: 'caller', text: 'Tengo miedo.' }, { role: 'operator', text: 'Entiendo. Estoy con vos. ¿Dónde estás?' }];
const criteria = criterionIds.map(id => ({ id, score: 3, reason: 'Intervención adecuada; faltó verificar lo entendido.', evidence: 'Estoy con vos.' }));
test('communication sums only the five bounded evidence-backed criteria', () => {
  const result = validateEvaluation({ criteria }, transcript);
  assert.equal(result.criteria.reduce((sum, c) => sum + c.score, 0), 15);
  assert.equal(result.available, true);
});
test('rejects fabricated evidence, caller quotes, duplicate criteria and out-of-range scores', () => {
  for (const change of [{ evidence: 'Texto inventado' }, { evidence: 'Tengo miedo.' }, { score: 6 }, { score: -1 }, { score: 3.5 }, { id: 'clarity' }]) {
    assert.equal(validateEvaluation({ criteria: [{ ...criteria[0], ...change }, ...criteria.slice(1)] }, transcript), null);
  }
});
test('missing conduct earns zero and cannot supply positive points without evidence', () => {
  const c = structuredClone(criteria); c[0].evidence = '';
  assert.equal(validateEvaluation({ criteria: c }, transcript), null);
  c[0].score = 0;
  assert.equal(validateEvaluation({ criteria: c }, transcript).criteria[0].score, 0);
});
test('request includes the whole conversation, including a final intervention after long text', async () => {
  const full = [...transcript, { role: 'operator', text: 'Una intervención extensa. '.repeat(600) }, { role: 'operator', text: 'CIERRE FINAL: la ayuda está en camino.' }];
  let prompt;
  const result = await evaluateCommunication({ apiKey: 'fixture-only', definition: { title: 'Caso de prueba' }, transcript: full,
    fetchImpl: async (url, options) => { prompt = JSON.parse(options.body).contents[0].parts[0].text; return { ok: true, json: async () => ({ candidates: [{ content: { parts: [{ text: JSON.stringify({ criteria }) }] } }] }) }; }
  });
  assert.equal(result.available, true);
  assert.ok(prompt.includes(JSON.stringify(full)));
  assert.ok(prompt.includes('sin premiar cantidad de palabras'));
});
test('unavailable model and invalid output preserve an explicit local fallback', async () => {
  for (const fetchImpl of [async () => ({ ok: false }), async () => { throw new Error('offline'); }, async () => ({ ok: true, json: async () => ({}) })]) {
    assert.deepEqual(await evaluateCommunication({ apiKey: 'fixture-only', definition: { title: 'test' }, transcript, fetchImpl }), { available: false, criteria: [] });
  }
  assert.deepEqual(await evaluateCommunication({ apiKey: '', transcript }), { available: false, criteria: [] });
});
test('deductions get a second evidence review and only the final score is returned', async () => {
  let calls = 0;
  const corrected = criteria.map(c => ({ ...c, score: 5, reason: 'Criterio cumplido; no hay una carencia demostrable.' }));
  const result = await evaluateCommunication({ apiKey: 'fixture-only', definition: { title: 'test' }, transcript,
    fetchImpl: async (_url, options) => {
      calls++;
      if (calls === 2) assert.ok(JSON.parse(options.body).contents[0].parts[0].text.includes('REVISIÓN FINAL DE DEDUCCIONES'));
      return { ok: true, json: async () => ({ candidates: [{ content: { parts: [{ text: JSON.stringify({ criteria: calls === 1 ? criteria : corrected }) }] } }] }) };
    }
  });
  assert.equal(calls, 2);
  assert.equal(result.criteria.reduce((s, c) => s + c.score, 0), 25);
});
