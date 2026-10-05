import test from 'node:test';
import assert from 'node:assert/strict';
import { once } from 'node:events';
import { readFileSync } from 'node:fs';
import WebSocket, { WebSocketServer } from 'ws';
import { createRelay } from '../server.mjs';
import { validateProposal, hasEvidence, setupMessage, inferFacts } from '../protocol.mjs';

const cases = JSON.parse(readFileSync(new URL('../../Assets/Resources/Cases/cases.json', import.meta.url), 'utf8').replace(/^\uFEFF/, '')).cases;
const definition = cases[0];
function inbox(ws) {
  const queued = [], waiting = [];
  ws.on('message', raw => {
    const value = JSON.parse(raw.toString());
    const waiter = waiting.shift();
    if (waiter) waiter(value); else queued.push(value);
  });
  return () => queued.length ? Promise.resolve(queued.shift()) : new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('Timed out waiting for message')), 3000);
    waiting.push(value => { clearTimeout(timer); resolve(value); });
  });
}
const send = (ws, value) => ws.send(JSON.stringify(value));

test('catalogue has eight complete Jujuy cases with 40 information points', () => {
  assert.equal(cases.length, 8);
  assert.equal(new Set(cases.map(c => c.id)).size, 8);
  assert.equal(new Set(cases.map(c => c.locality)).size, 8);
  for (const c of cases) {
    assert.equal(new Set(c.facts.map(f => f.id)).size, 5);
    assert.equal(c.facts.reduce((sum, f) => sum + f.points, 0), 40);
    assert.ok(c.facts.every(f => !f.uncertain || f.verifiedValue));
    assert.ok(c.latitude > -25 && c.latitude < -21 && c.longitude > -68 && c.longitude < -64);
    assert.ok(Math.abs(c.mapNormalizedCoord.x - (c.longitude + 68.05) / 4.9) < .00001);
    assert.ok(Math.abs(c.mapNormalizedCoord.y - (c.latitude + 24.7) / 3.35) < .00001);
    assert.ok(c.arrivalSeconds >= 0);
  }
});

test('a geographic signal or street fragment cannot confirm a dispatch address', () => {
  for (const c of cases) {
    assert.ok(!inferFacts(c, '¿Dónde estás?', c.locality, 1).some(e => e.factId === 'location'));
    assert.ok(inferFacts(c, '¿Cuál es la dirección?', c.facts[0].response, 1).some(e => e.factId === 'location'));
  }
  assert.ok(!inferFacts(cases[0], '¿Dónde estás?', 'Creo que es San Martín', 1).some(e => e.factId === 'location'));
});
test('setup configures native audio, manual PTT, transcriptions and no score tool', () => {
  const setup = setupMessage(definition, 'configured-model').setup;
  assert.equal(setup.model, 'models/configured-model');
  assert.deepEqual(setup.generationConfig.responseModalities, ['AUDIO']);
  assert.equal(setup.realtimeInputConfig.automaticActivityDetection.disabled, true);
  assert.ok(setup.inputAudioTranscription);
  assert.ok(setup.outputAudioTranscription);
  assert.ok(!JSON.stringify(setup.tools).includes('overallScore'));
});
test('proposals reject hallucinated facts, unknown tools and invalid turns', () => {
  const call = { id: 'f1', name: 'registrar_dato', args: { factId: 'location', value: definition.facts[0].value, verified: true, evidence: 'Club Deportivo Norte' } };
  assert.equal(validateProposal(call, definition, 1).value, definition.facts[0].value);
  assert.equal(validateProposal(call, definition, 0), null);
  assert.equal(validateProposal({ ...call, args: { ...call.args, value: 'Another address' } }, definition, 1), null);
  assert.equal(validateProposal({ ...call, name: 'give_points' }, definition, 1), null);
  assert.equal(validateProposal({ ...call, args: { ...call.args, evidence: '' } }, definition, 1), null);
});
test('fact evidence must come from caller; classification from operator', () => {
  assert.ok(hasEvidence({ type: 'fact', evidence: 'club deportivo' }, 'hola', 'Estamos en el Club Deportivo Norte.'));
  assert.ok(!hasEvidence({ type: 'fact', evidence: 'club deportivo' }, 'Club Deportivo', 'No sé.'));
  assert.ok(hasEvidence({ type: 'tag', evidence: 'dónde están' }, '¿Dónde están?', 'Aquí'));
});
test('uncertain information accepts only authored provisional/corrected values', () => {
  const f = cases[2].facts[1];
  const call = { id: 'x', name: 'registrar_dato', args: { factId: 'incident', value: f.value, verified: false, evidence: 'Creo que chocaron' } };
  assert.equal(validateProposal(call, cases[2], 1).verified, false);
  call.args.verified = true;
  assert.equal(validateProposal(call, cases[2], 2), null);
  call.args.value = f.verifiedValue;
  assert.equal(validateProposal(call, cases[2], 2).verified, true);
});
test('missing key is an explicit error, never an automatic fake Live response', async t => {
  const { server, clients } = createRelay({ apiKey: '' });
  server.listen(0, '127.0.0.1'); await once(server, 'listening');
  t.after(() => { for (const c of clients.clients) c.terminate(); clients.close(); server.close(); });
  const client = new WebSocket('ws://127.0.0.1:' + server.address().port);
  const next = inbox(client); await once(client, 'open');
  send(client, { type: 'start', caseId: definition.id });
  const message = await next();
  assert.equal(message.type, 'error'); assert.match(message.text, /GEMINI_API_KEY/);
});
test('real WebSocket relay handles setup, text, tool evidence, cancellation and PTT in order', async t => {
  const upstreamServer = new WebSocketServer({ port: 0, host: '127.0.0.1' });
  await once(upstreamServer, 'listening');
  const connection = once(upstreamServer, 'connection');
  const { server, clients } = createRelay({ apiKey: 'test-only', upstreamFactory: () => new WebSocket('ws://127.0.0.1:' + upstreamServer.address().port) });
  server.listen(0, '127.0.0.1'); await once(server, 'listening');
  t.after(() => {
    for (const c of clients.clients) c.terminate();
    for (const c of upstreamServer.clients) c.terminate();
    clients.close(); server.close(); upstreamServer.close();
  });
  const client = new WebSocket('ws://127.0.0.1:' + server.address().port);
  t.after(() => client.terminate());
  const nextClient = inbox(client); await once(client, 'open');
  send(client, { type: 'start', caseId: definition.id });
  const [upstream] = await connection;
  const nextUpstream = inbox(upstream);
  const setup = await nextUpstream();
  assert.equal(setup.setup.realtimeInputConfig.automaticActivityDetection.disabled, true);
  send(upstream, { setupComplete: {} });
  assert.equal((await nextClient()).type, 'ready');
  assert.equal((await nextUpstream()).clientContent.turnComplete, true);
  send(upstream, { serverContent: { turnComplete: true } });
  assert.equal((await nextClient()).type, 'turn_complete');
  send(client, { type: 'text', caseId: definition.id, text: '¿Dónde están?', turnId: 1 });
  assert.equal((await nextUpstream()).clientContent.turns[0].parts[0].text, '¿Dónde están?');
  send(upstream, { toolCall: { functionCalls: [
    { id: 'fact-1', name: 'registrar_dato', args: { factId: 'location', value: definition.facts[0].value, verified: true, evidence: 'Club Deportivo Norte' } },
    { id: 'cancel-me', name: 'registrar_pregunta', args: { tag: 'clear', evidence: 'Dónde están' } },
    { id: 'unsupported', name: 'registrar_pregunta', args: { tag: 'relevant', evidence: 'palabras nunca dichas' } }
  ] } });
  const response = await nextUpstream();
  assert.equal(response.toolResponse.functionResponses.length, 3);
  send(upstream, { toolCallCancellation: { ids: ['cancel-me'] } });
  send(upstream, { serverContent: { outputTranscription: { text: 'Estamos en el Club Deportivo Norte.' }, turnComplete: true } });
  assert.equal((await nextClient()).type, 'caller');
  const fact = await nextClient();
  assert.equal(fact.type, 'fact'); assert.equal(fact.turnId, 1); assert.equal(fact.value, definition.facts[0].value);
  assert.equal((await nextClient()).type, 'turn_complete'); // no canceled/unevidenced tags
  send(client, { type: 'activity_start', caseId: definition.id, turnId: 2 });
  assert.deepEqual((await nextUpstream()).realtimeInput.activityStart, {});
  send(client, { type: 'audio', caseId: definition.id, turnId: 2, sampleRate: 16000, data: Buffer.alloc(640).toString('base64') });
  assert.equal((await nextUpstream()).realtimeInput.audio.mimeType, 'audio/pcm;rate=16000');
  send(client, { type: 'activity_end', caseId: definition.id, turnId: 2 });
  assert.deepEqual((await nextUpstream()).realtimeInput.activityEnd, {});
  send(upstream, { serverContent: { modelTurn: { parts: [{ inlineData: { mimeType: 'audio/pcm;rate=24000', data: 'AAA=' } }] }, turnComplete: true } });
  assert.equal((await nextClient()).sampleRate, 24000);
  assert.equal((await nextClient()).type, 'turn_complete');
  send(client, { type: 'text', caseId: definition.id, text: 'Replay old turn', turnId: 1 });
  assert.equal((await nextClient()).type, 'error');
});
