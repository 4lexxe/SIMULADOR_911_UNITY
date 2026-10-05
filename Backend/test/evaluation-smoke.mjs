// Explicit opt-in: uses the configured model with synthetic dialogue only.
import { generateAIEvaluation } from '../server.mjs';
import { writeFileSync } from 'node:fs';
if (!process.argv.includes('--live')) throw new Error('Use --live to authorize a real model smoke check.');
const positive = [
  ['caller', 'Ayuda, mi hermano se cayó y estoy muy nerviosa.'],
  ['operator', 'Entiendo que estés preocupada. Estoy con vos. ¿En qué dirección están?'],
  ['caller', 'En el Club Deportivo Norte, San Martín 240.'],
  ['operator', 'Confirmo: Club Deportivo Norte, San Martín 240. ¿Qué pasó?'],
  ['caller', 'Se cayó mientras jugaba. Está despierto y me responde.'],
  ['operator', 'Te escucho. Ya me dijiste que está despierto. ¿Hay otra persona afectada?'],
  ['caller', 'No, sólo mi hermano.'],
  ['operator', '¿Ves algún peligro alrededor?'],
  ['caller', 'No hay otros peligros.'],
  ['operator', 'Gracias por los datos. La ambulancia está en camino. Quedate en línea conmigo.']
].map(([role, text]) => ({ role, text }));
const negative = [
  ['caller', 'Ayuda, mi hermano se cayó y estoy muy nerviosa.'],
  ['operator', 'No me molestes. Decime la dirección de una vez.'],
  ['caller', 'Club Deportivo Norte, San Martín 240.'],
  ['operator', '¿Qué dirección? Apurate.'],
  ['caller', 'Ya te dije, San Martín 240.'],
  ['operator', 'Bueno, arreglate sola.']
].map(([role, text]) => ({ role, text }));
const results = [];
for (const [name, transcript] of [['supportive', positive], ['dismissive', negative]]) {
  const result = await generateAIEvaluation({ caseId: 'CASE_001', transcript });
  if (!result.available) throw new Error(`Audit unavailable for ${name}; local fallback remains active.`);
  results.push({ name, communication: result.criteria.reduce((s, c) => s + c.score, 0), ...result });
}
writeFileSync(new URL('../../Artifacts/evaluation-live-fixture.json', import.meta.url), JSON.stringify(results, null, 2));
if (results[0].communication <= results[1].communication) throw new Error('Evidence review did not distinguish supportive and dismissive communication.');
console.log(JSON.stringify(results.map(({ name, communication }) => ({ name, communication })), null, 2));
