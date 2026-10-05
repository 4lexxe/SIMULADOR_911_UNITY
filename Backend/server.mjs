import http from 'node:http';
import { evaluateCommunication } from './evaluation.mjs';
import { readFileSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import WebSocket, { WebSocketServer } from 'ws';
import { setupMessage, validateProposal, hasEvidence, inferFacts } from './protocol.mjs';

// Auto-load environment variables from .env or .env.local if not already in process.env
function loadEnv() {
  for (const file of ['.env.local', '.env']) {
    try {
      const p = new URL(file, import.meta.url);
      if (existsSync(p)) {
        const text = readFileSync(p, 'utf8');
        for (const line of text.split('\n')) {
          const match = line.match(/^\s*([\w_]+)\s*=\s*(.*)?\s*$/);
          if (match && !process.env[match[1]]) {
            process.env[match[1]] = match[2]?.trim().replace(/^["']|["']$/g, '');
          }
        }
      }
    } catch {}
  }
}
loadEnv();

const catalogue = JSON.parse(readFileSync(new URL('../Assets/Resources/Cases/cases.json', import.meta.url), 'utf8').replace(/^\uFEFF/, '')).cases;

export async function generateAIEvaluation({ apiKey = process.env.GEMINI_API_KEY, caseId, transcript }) {
  return evaluateCommunication({ apiKey, definition: catalogue.find(c => c.id === caseId), transcript });
}
export function createRelay({ apiKey = process.env.GEMINI_API_KEY, model = process.env.GEMINI_MODEL || 'gemini-3.8-live', upstreamFactory } = {}) {
  const server = http.createServer((req, res) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    res.setHeader('Access-Control-Allow-Methods', 'POST, GET, OPTIONS');
    if (req.method === 'OPTIONS') { res.writeHead(200); res.end(); return; }

    if (req.url === '/health') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ status: 'ok', configured: Boolean(apiKey), model, auditVersion: 2 }));
      return;
    }

    if (req.url === '/evaluate' && req.method === 'POST') {
      let body = '';
      req.on('data', chunk => { body += chunk; });
      req.on('end', async () => {
        try {
          const payload = JSON.parse(body || '{}');
          const evalResult = await generateAIEvaluation({ ...payload, apiKey });
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify(evalResult));
        } catch (e) {
          res.writeHead(400, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: 'invalid_json' }));
        }
      });
      return;
    }

    res.writeHead(404, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'not_found' }));
  });
  const clients = new WebSocketServer({ noServer: true, maxPayload: 256 * 1024 });
  server.on('upgrade', (req, socket, head) => {
    // Desktop client has no Origin. Reject browser-origin connections to the local credentialed relay.
    if (req.headers.origin || clients.clients.size >= 2 || req.url !== '/') { socket.destroy(); return; }
    clients.handleUpgrade(req, socket, head, client => clients.emit('connection', client));
  });
  clients.on('connection', client => {
    let upstream, definition, ready = false, turnId = 0, recording = false, busy = false;
    let operator = '', caller = '', pending = new Map(), seen = new Set(), deadline, audioBytes = 0;
    const emit = event => {
      if (client.readyState === WebSocket.OPEN) {
        if (client.bufferedAmount > 2 * 1024 * 1024) { client.terminate(); return; }
        client.send(JSON.stringify({ caseId: definition?.id ?? '', ...event }));
      }
    };
    const send = message => { if (upstream?.readyState === WebSocket.OPEN) upstream.send(JSON.stringify(message)); };
    const fail = text => { emit({ type: 'error', text }); client.close(1011, 'Session ended'); };
    function finishTurn() {
      const emittedFactIds = new Set();
      for (const proposal of pending.values()) {
        const hasEv = hasEvidence(proposal, operator, caller);
        if (hasEv) {
          const { evidence, ...event } = proposal;
          emit(event);
          if (event.factId) emittedFactIds.add(event.factId);
        }
      }
      if (definition) {
        const inferred = inferFacts(definition, operator, caller, turnId);
        for (const event of inferred) {
          if (!emittedFactIds.has(event.factId)) {
            emittedFactIds.add(event.factId);
            emit(event);
          }
        }
      }
      pending.clear(); busy = false;
      emit({ type: 'turn_complete', turnId });
    }
    function beginTurn(message) {
      if (!ready || busy || recording || !Number.isInteger(message.turnId) || message.turnId !== turnId + 1) return false;
      turnId = message.turnId; operator = ''; caller = ''; pending.clear(); audioBytes = 0;
      return true;
    }
    client.on('message', (raw, binary) => {
      try {
        if (binary) throw new Error('JSON required');
        const msg = JSON.parse(raw.toString());
        if (msg.type === 'start') {
          if (definition) throw new Error('Already started');
          definition = catalogue.find(c => c.id === msg.caseId);
          if (!definition) throw new Error('Unknown case');
          if (!apiKey && !upstreamFactory) { fail('Falta GEMINI_API_KEY en Backend/.env. Usá ensayo local o configurá el backend.'); return; }
          upstream = upstreamFactory ? upstreamFactory() : new WebSocket('wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key=' + encodeURIComponent(apiKey), { maxPayload: 4 * 1024 * 1024 });
          deadline = setTimeout(() => fail('Gemini no confirmó la conexión. Revisá la clave, el modelo y la cuota.'), 15000);
          upstream.on('open', () => send(setupMessage(definition, model)));
          upstream.on('error', () => fail('No se pudo conectar con Gemini Live.'));
          upstream.on('close', () => { if (client.readyState === WebSocket.OPEN) fail('Gemini cerró la sesión. Podés continuar en ensayo local.'); });
          upstream.on('message', rawMessage => {
            try {
              const response = JSON.parse(rawMessage.toString());
              if (response.error) { fail('Gemini rechazó la sesión. Revisá la clave, el modelo y la cuota del backend.'); return; }
              if (response.setupComplete) {
                clearTimeout(deadline); ready = true; busy = true; emit({ type: 'ready' });
                send({ clientContent: { turns: [{ role: 'user', parts: [{ text: 'Comenzá la llamada diciendo solamente tu frase de apertura.' }] }], turnComplete: true } });
              }
              const content = response.serverContent;
              if (content) {
                if (content.interrupted) { pending.clear(); emit({ type: 'interrupted' }); }
                if (content.inputTranscription?.text) { operator += content.inputTranscription.text; emit({ type: 'operator', text: content.inputTranscription.text, turnId }); }
                if (content.outputTranscription?.text) { caller += content.outputTranscription.text; emit({ type: 'caller', text: content.outputTranscription.text, turnId }); }
                for (const part of content.modelTurn?.parts ?? []) {
                  if (part.inlineData?.mimeType?.startsWith('audio/pcm')) {
                    const rate = Number(/rate=(\d+)/.exec(part.inlineData.mimeType)?.[1] ?? 24000);
                    emit({ type: 'audio', data: part.inlineData.data, sampleRate: rate, turnId });
                  }
                }
              }
              for (const id of response.toolCallCancellation?.ids ?? []) pending.delete(id);
              if (response.toolCall) {
                const functionResponses = [];
                for (const call of response.toolCall.functionCalls ?? []) {
                  const proposal = validateProposal(call, definition, turnId);
                  const accepted = proposal && !seen.has(call.id);
                  if (accepted) { seen.add(call.id); pending.set(call.id, proposal); }
                  functionResponses.push({ id: call.id, name: call.name, response: { status: accepted ? 'pending_evidence' : 'rejected' } });
                }
                send({ toolResponse: { functionResponses } });
              }
              if (content?.turnComplete) finishTurn();
              if (response.goAway) fail('La sesión Live alcanzó su límite. Continuá en ensayo local.');
            } catch { fail('Se recibió una respuesta de voz inválida.'); }
          });
          return;
        }
        if (!definition || msg.caseId !== definition.id) throw new Error('Wrong case');
        if (msg.type === 'text') {
          if (typeof msg.text !== 'string' || !msg.text.trim() || msg.text.length > 1200 || !beginTurn(msg)) throw new Error('Invalid turn');
          operator = msg.text; busy = true;
          send({ clientContent: { turns: [{ role: 'user', parts: [{ text: msg.text }] }], turnComplete: true } });
        } else if (msg.type === 'activity_start') {
          if (!beginTurn(msg)) throw new Error('Invalid turn');
          recording = true; send({ realtimeInput: { activityStart: {} } });
        } else if (msg.type === 'audio') {
          if (!recording || msg.turnId !== turnId || msg.sampleRate !== 16000 || typeof msg.data !== 'string' || !/^[A-Za-z0-9+/]*={0,2}$/.test(msg.data)) throw new Error('Invalid audio');
          const bytes = Buffer.from(msg.data, 'base64');
          audioBytes += bytes.length;
          if (bytes.length % 2 || audioBytes > 16000 * 2 * 32) throw new Error('Audio limit');
          if (upstream.bufferedAmount > 1024 * 1024) throw new Error('Backpressure');
          send({ realtimeInput: { audio: { data: msg.data, mimeType: 'audio/pcm;rate=16000' } } });
        } else if (msg.type === 'activity_end') {
          if (!recording || msg.turnId !== turnId) throw new Error('Invalid end');
          recording = false; busy = true; send({ realtimeInput: { activityEnd: {} } });
        } else throw new Error('Unknown message');
      } catch { fail('Mensaje de sesión inválido. Reiniciá la conexión en la próxima llamada.'); }
    });
    client.on('error', () => {});
    client.on('close', () => { clearTimeout(deadline); upstream?.close(); });
  });
  return { server, clients };
}
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const { server } = createRelay();
  const port = Number(process.env.PORT || 8787);
  server.listen(port, '127.0.0.1', () => console.log('Última Llamada relay: http://127.0.0.1:' + port + '/health'));
}

