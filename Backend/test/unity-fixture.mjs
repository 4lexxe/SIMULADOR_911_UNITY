// Local integration fixture; never calls Google or uses a credential.
import { WebSocketServer } from 'ws';
import WebSocket from 'ws';
import { createRelay } from '../server.mjs';
import { readFileSync } from 'node:fs';
const definition = JSON.parse(readFileSync(new URL('../../Assets/Resources/Cases/cases.json', import.meta.url), 'utf8').replace(/^\uFEFF/, '')).cases[0];
const upstream = new WebSocketServer({ host: '127.0.0.1', port: 0 });
const send = (ws, message) => ws.send(JSON.stringify(message));
const audio = { inlineData: { mimeType: 'audio/pcm;rate=24000', data: Buffer.alloc(4800).toString('base64') } };
upstream.on('connection', ws => {
  let turn = 0;
  ws.on('message', raw => {
    const msg = JSON.parse(raw.toString());
    if (msg.setup) send(ws, { setupComplete: {} });
    if (msg.clientContent && turn++ === 0) {
      send(ws, { serverContent: { outputTranscription: { text: definition.opening }, modelTurn: { parts: [audio] }, turnComplete: true } });
    } else if (msg.clientContent) {
      send(ws, { toolCall: { functionCalls: [{ id: 'unity-location', name: 'registrar_dato', args: { factId: 'location', value: definition.facts[0].value, verified: true, evidence: 'Club Deportivo Norte' } }] } });
    } else if (msg.toolResponse) {
      send(ws, { serverContent: { outputTranscription: { text: definition.facts[0].response }, modelTurn: { parts: [audio] }, turnComplete: true } });
    }
  });
});
const { server } = createRelay({ apiKey: '', upstreamFactory: () => new WebSocket('ws://127.0.0.1:' + upstream.address().port) });
server.listen(18787, '127.0.0.1', () => console.log('Unity integration fixture ready on 18787 (synthetic audio, no Gemini).'));
