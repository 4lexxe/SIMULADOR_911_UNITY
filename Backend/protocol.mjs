export const tags = ['relevant', 'clear', 'active_listening', 'repeated', 'confusing', 'interruption', 'confirmation', 'closure'];
const string = { type: 'STRING' };
const tool = (name, description, properties, required) => ({ name, description, parameters: { type: 'OBJECT', properties, required } });

export const declarations = [
  tool('registrar_dato', 'Proponer un dato que acabás de comunicar oralmente. Usá el valor canónico exacto y citá una frase de tu respuesta como evidencia.', {
    factId: string, value: string, verified: { type: 'BOOLEAN' }, evidence: string
  }, ['factId', 'value', 'verified', 'evidence']),
  tool('registrar_pregunta', 'Clasificar una intervención del operador. La evidencia debe ser una cita de sus palabras; nunca otorgar puntos.', {
    tag: { type: 'STRING', enum: tags }, evidence: string
  }, ['tag', 'evidence']),
  tool('cambiar_estado_npc', 'Proponer cambio de calma entre -5 y 5, una vez por intervención.', {
    delta: { type: 'INTEGER' }, evidence: string
  }, ['delta', 'evidence'])
];

export const normalize = value => String(value ?? '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9 ]/g, ' ').replace(/\s+/g, ' ').trim();

export function validateProposal(call, definition, turnId) {
  if (!Number.isInteger(turnId) || turnId < 1 || typeof call.id !== 'string') return null;
  const a = call.args ?? {};
  if (typeof a.evidence !== 'string' || normalize(a.evidence).length < 2 || a.evidence.length > 600) return null;
  const base = { caseId: definition.id, turnId, eventId: call.id, evidence: a.evidence };
  if (call.name === 'registrar_dato') {
    const f = definition.facts.find(x => x.id === a.factId);
    if (!f || typeof a.verified !== 'boolean') return null;
    const value = f.uncertain && a.verified ? f.verifiedValue : f.value;
    if (a.value !== value) return null;
    return { ...base, type: 'fact', factId: f.id, value, verified: !f.uncertain || a.verified };
  }
  if (call.name === 'registrar_pregunta' && tags.includes(a.tag)) return { ...base, type: 'tag', tag: a.tag };
  if (call.name === 'cambiar_estado_npc' && Number.isInteger(a.delta))
    return { ...base, type: 'emotion', delta: Math.max(-5, Math.min(5, a.delta)) };
  return null;
}

export function hasEvidence(proposal, operator, caller) {
  const target = normalize(proposal.type === 'fact' ? caller : operator);
  const evidence = normalize(proposal.evidence);
  if (!evidence || !target) return false;
  if (target.includes(evidence) || evidence.includes(target)) return true;
  
  // Fuzzy token matching
  const words = evidence.split(' ').filter(w => w.length > 2);
  if (words.length > 0) {
    const matched = words.filter(w => target.includes(w));
    if (matched.length >= Math.ceil(words.length * 0.4)) return true;
  }
  return false;
}

export const FACT_TOKENS = {
  CASE_001: {
    location: ['san martin', '240', 'deportivo norte'],
    incident: ['cayo', 'caida', 'jugando', 'golpeo', 'se golpeo'],
    people: ['solo mi hermano', '1 persona', 'una persona', 'no hay otra persona', 'yo estoy bien', 'nadie mas', 'solo martin'],
    condition: ['despierto', 'responde', 'consciente', 'hablo'],
    risks: ['no veo fuego', 'sin peligro', 'cables', 'no hay peligro', 'ningun peligro', 'otro peligro', 'no hay cables']
  },
  CASE_002: {
    location: ['belgrano', '815', 'rueda sur'],
    incident: ['llama', 'fuego', 'mesa de trabajo', 'empezo a salir humo'],
    people: ['tres personas', '3 personas', 'eramos tres', 'los tres', 'todos afuera', 'en la vereda'],
    condition: ['podemos hablar', 'lastimado', 'lesiones', 'ninguno lastimado', 'ninguno dice'],
    risks: ['recipientes', 'pintura', 'recipientes de pintura', 'combustible']
  },
  CASE_003: {
    location: ['mitre', 'rivadavia'],
    incident: ['chocaron', 'choque', 'dos autos', 'ruido fuerte', 'auto detenido', 'un solo auto', 'suposicion'],
    people: ['una persona visible', '1 persona visible', 'persona junto al auto', 'una persona junto', '1 persona junto'],
    condition: ['de pie', 'lesiones desconocidas', 'no puedo saber si tiene lesiones'],
    risks: ['obstruye', 'vehiculos lo esquivan', 'esquivan', 'transito', 'calzada']
  }
};

export function inferFacts(definition, operator, caller, turnId) {
  const events = [];
  const normCaller = normalize(caller);
  if (!normCaller) return events;

  const caseTokens = FACT_TOKENS[definition.id] || {};

  for (const fact of definition.facts) {
    const normResp = normalize(fact.response);
    const normVal = normalize(fact.value);
    const normVerResp = fact.uncertain ? normalize(fact.verificationResponse) : '';
    const normVerVal = fact.uncertain ? normalize(fact.verifiedValue) : '';

    const specificTokens = caseTokens[fact.id] || [];
    const hasSpecificToken = fact.id !== 'location' && specificTokens.some(tok => normCaller.includes(normalize(tok)));
    // A city name or street fragment is only a signal origin, never a confirmed dispatch address.
    const exactValueMatch = normCaller.includes(normVal) || (normVerVal && normCaller.includes(normVerVal));
    const exactRespMatch = normCaller.includes(normResp) || (normVerResp && normCaller.includes(normVerResp));

    const callerMatched = hasSpecificToken || exactValueMatch || exactRespMatch;

    if (callerMatched) {
      const isVerified = !fact.uncertain || (normVerVal && normCaller.includes(normVerVal)) || (normVerResp && normCaller.includes(normVerResp));
      const value = isVerified && fact.uncertain ? fact.verifiedValue : fact.value;
      events.push({
        caseId: definition.id,
        turnId,
        eventId: `inferred-fact-${turnId}-${fact.id}`,
        type: 'fact',
        factId: fact.id,
        value,
        verified: isVerified
      });
    }
  }

  return events;
}

export function getVoiceForCase(definition) {
  // Case 2 is Diego (workshop mechanic) -> Male voice
  if (definition?.callerName?.toLowerCase().includes('diego') || ['CASE_002', 'CASE_005', 'CASE_006'].includes(definition?.id)) {
    return 'Puck'; // Male voice
  }
  // Case 3 is Elena -> Female voice
  if (definition?.callerName?.toLowerCase().includes('elena') || definition?.id === 'CASE_003') {
    return 'Kore'; // Female voice
  }
  // Case 1 is Lucía -> Female voice
  return 'Aoede'; // Female voice
}

export function setupMessage(definition, model) {
  const facts = definition.facts.map(({ keywords, points, ...fact }) => fact);
  const voiceName = getVoiceForCase(definition);
  const isMale = voiceName === 'Puck' || voiceName === 'Charon' || voiceName === 'Fenrir';

  const systemInstructions =
    `Interpretás a ${definition.callerName} (${isMale ? 'voz masculina' : 'femenina'}), una persona en estado de emergencia, shock y nerviosismo en un juego de comunicación y despacho de emergencias.\n` +
    `Personalidad y contexto: ${definition.personality}\n` +
    `Apertura obligatoria: "${definition.opening}"\n\n` +
    `Las frases exactas "central mostrar mapa", "central mostrar llamada", "central mostrar unidades" y "central mostrar escritorio" son comandos de interfaz. Si las oís, esperá la siguiente pregunta con una respuesta breve; no aportes hechos nuevos ni clasifiques esos comandos con herramientas.\n` +
    `DATOS DE AUTORÍA DEL CASO (Tu única fuente de hechos reales):\n` +
    JSON.stringify(facts) + `\n\n` +
    `REGLAS ESTRICTAS DE REALISMO PSICOLÓGICO Y DINÁMICA DE JUEGO:\n` +
    `1. REACCIÓN ANTE PREGUNTAS MÚLTIPLES O APURADAS:\n` +
    `   Si el operador te hace muchas preguntas de golpe (más de dos preguntas en un solo turno, o te pide todos los datos juntos a la vez), NO respondas todos los datos como un robot. Te sentís abrumado/a, aturdido/a o confundido/a por la desesperación. Respondé con agobio: "¡Pará, por favor! Me estás preguntando muchas cosas juntas y no puedo pensar con los nervios... ¿Qué me preguntaste primero?" o "¡No me apures que estoy temblando! Preguntame de a una cosa por favor." Invocá cambiar_estado_npc con delta negativo (-3 a -5) y registrar_pregunta con tag confusing o repeated. NUNCA invoques registrar_dato cuando estés abrumado/a o si no diste la información concreta en tus palabras.\n\n` +
    `2. UBICACIÓN EN JUJUY:\n` +
    `   La llamada procede de ${definition.locality}, región ${definition.region}. El mapa del operador muestra solo una señal aproximada de la localidad, nunca la dirección exacta.\n` +
    `   Cuando pregunte dónde estás, indicá la localidad y la dirección o referencia completa del dato location de este caso. No le exijas adivinar una calle ni un número a partir del punto del mapa. Usá únicamente las referencias de este caso, sin agregar monumentos ni calles.\n` +
    `   Invocá registrar_dato para location solo después de comunicar su referencia completa. Nombrar la localidad por sí sola no confirma la dirección.\n\n` +
    `3. EMPATÍA Y CALMA:\n` +
    `   Si el operador te habla con calma, contención o escucha activa ("Tranquilo/a, respirá, te estoy escuchando, ya te vamos a ayudar"), sentite más aliviado/a y respondé con mayor claridad. Invocá cambiar_estado_npc con delta positivo (+2 a +5).\n\n` +
    `4. DATOS INCIERTOS:\n` +
    `   Solo los hechos marcados uncertain tienen una suposición inicial. Si preguntan cómo lo sabés, respondé con verificationResponse y el verifiedValue de ese hecho. No importes hechos o dudas de otros casos.\n\n` +
    `5. FORMATO:\n` +
    `   Hablá en español argentino natural y coloquial, de 1 a 3 frases por respuesta. Invocá registrar_dato cuando confirmes datos y registrar_pregunta para clasificar las intervenciones.`;

  return { setup: {
    model: 'models/' + model,
    generationConfig: {
      responseModalities: ['AUDIO'],
      temperature: 0.6,
      speechConfig: {
        voiceConfig: {
          prebuiltVoiceConfig: {
            voiceName: voiceName
          }
        }
      }
    },
    realtimeInputConfig: { automaticActivityDetection: { disabled: true } },
    inputAudioTranscription: {}, outputAudioTranscription: {},
    systemInstruction: { parts: [{ text: systemInstructions }] },
    tools: [{ functionDeclarations: declarations }]
  } };
}
