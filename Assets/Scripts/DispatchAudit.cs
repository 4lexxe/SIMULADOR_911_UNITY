using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmergencyDispatch
{
    [Serializable] public class ScoreAreaReview
    {
        public string name, explanation;
        public int score, maximum;
    }
    [Serializable] public class CommunicationCriterion
    {
        public string id, reason, evidence;
        public int score;
    }
    public static class DispatchAudit
    {
        public static readonly string[] Names = { "Precisión", "Comunicación", "Decisión táctica", "Eficiencia operativa", "Protocolo" };
        public static readonly int[] Maxima = { 40, 25, 20, 10, 5 };
        public static readonly string[] CriterionIds = { "relevance", "clarity", "empathy", "listening", "respect" };
        public static readonly string[] CriterionNames = { "Pertinencia", "Claridad", "Contención", "Escucha", "Trato" };

        public static int[] Values(CallResult r) => new[] { r.information, r.communication, r.decision, r.efficiency, r.handling };
        public static void BuildLocal(CallSession session, CallResult r)
        {
            r.turns = session.ScoredTurns;
            var known = new List<string>(); var missing = new List<string>();
            foreach (var fact in session.Definition.facts)
            {
                bool verified = session.Facts.TryGetValue(fact.id, out var record) && record.verified;
                (verified ? known : missing).Add(fact.label + " (" + fact.points + " pts)");
            }
            var explanations = new[] {
                "Datos verificados: " + (known.Count == 0 ? "ninguno." : string.Join(", ", known) + ".") + (missing.Count > 0 ? " Pendientes: " + string.Join(", ", missing) + "." : " Los cinco datos están verificados."),
                "Registro local: " + session.Count("relevant") + " intervenciones pertinentes, " + session.Count("clear") + " claras y " + session.Count("active_listening") + " de contención. " + session.Count("repeated") + " repeticiones; " + session.Count("confusing") + " intervenciones confusas. Fórmula: pertinencia hasta 15, claridad hasta 5 y escucha hasta 5; se descuentan repeticiones.",
                session.DispatchedUnit == null ? "No se despachó ninguna unidad: 0 de 20 puntos." : r.decision == 20 ? "La unidad " + r.resource + " corresponde al recurso requerido por el caso: 20 de 20 puntos." : "La unidad " + r.resource + " no corresponde al recurso requerido por el caso: 0 de 20 puntos.",
                r.information < 20 ? "Se necesitan al menos 20 puntos de precisión para valorar la eficiencia. Una llamada corta sin datos suficientes no gana puntos." : "10 puntos base; se descuenta 1 por intervención después de la octava y 1 por repetición. " + session.ScoredTurns + " intervenciones y " + session.Count("repeated") + " repeticiones registradas. El tiempo se informa, no se premia hablar más rápido.",
                "Confirmación de datos: " + (session.Count("confirmation") > 0 ? "2/2." : "0/2; no se registró.") + " Comunicación del envío tras despachar: " + (session.DispatchedUnit != null && session.Count("closure") > 0 ? "3/3." : "0/3; no se registró.")
            };
            var values = Values(r);
            r.areas = Enumerable.Range(0, 5).Select(i => new ScoreAreaReview { name = Names[i], maximum = Maxima[i], score = values[i], explanation = explanations[i] }).ToArray();
            RefreshSummary(r);
        }
        public static bool ApplyCommunication(CallResult r, CommunicationCriterion[] criteria)
        {
            if (criteria == null || criteria.Length != 5 || r.areas == null) return false;
            var operatorLines = r.transcriptLog.Where(x => x.StartsWith("OPERADOR: ")).Select(x => x.Substring(10)).ToArray();
            var validated = new List<CommunicationCriterion>();
            foreach (string id in CriterionIds)
            {
                var matches = criteria.Where(c => c != null && c.id == id).ToArray();
                if (matches.Length != 1) return false;
                var c = matches[0];
                if (c.score < 0 || c.score > 5 || string.IsNullOrWhiteSpace(c.reason) || c.reason.Length > 280) return false;
                bool hasQuote = !string.IsNullOrWhiteSpace(c.evidence);
                if ((c.score > 0 && !hasQuote) || (hasQuote && !operatorLines.Any(t => t.IndexOf(c.evidence, StringComparison.OrdinalIgnoreCase) >= 0))) return false;
                validated.Add(c);
            }
            r.communicationReview = validated.ToArray();
            r.communication = validated.Sum(c => c.score);
            r.areas[1].score = r.communication;
            r.areas[1].explanation = "Cinco criterios de 0 a 5 puntos, contrastados con las intervenciones del operador. La extensión del texto no suma puntos.";
            r.evaluationSource = "IA · evidencia contrastada";
            r.evaluationPending = false;
            RefreshSummary(r);
            return true;
        }
        public static void RefreshSummary(CallResult r)
        {
            r.grade = r.Total >= 90 ? "Excelente trabajo" : r.Total >= 75 ? "Caso resuelto" : r.Total >= 60 ? "Resolución parcial" : "Caso a revisar";
            if (r.Total >= 60 && r.decision < 20) r.grade = r.resource == "Sin despacho" ? "Despacho pendiente" : "Revisar despacho";
            r.positiveNotes.Clear(); r.improvementNotes.Clear();
            if (r.information == 40) r.positiveNotes.Add("Verificaste los cinco datos de la emergencia.");
            else if (r.information > 0) r.positiveNotes.Add("Reuniste datos verificados: " + r.information + "/40 en precisión.");
            if (r.decision == 20) r.positiveNotes.Add("Despachaste el recurso adecuado: " + r.resource + ".");
            else r.improvementNotes.Add(r.resource == "Sin despacho" ? "La llamada terminó sin una unidad asignada." : "El recurso asignado no corresponde a este caso.");
            if (r.information < 40) r.improvementNotes.Add("Quedaron datos sin verificar: " + (40-r.information) + " puntos de precisión pendientes.");
            if (r.handling == 5) r.positiveNotes.Add("Confirmaste los datos y comunicaste el envío.");
            else r.improvementNotes.Add("Completá la confirmación y el cierre: " + r.handling + "/5 en protocolo.");
            if (r.communicationReview != null)
            {
                foreach (var c in r.communicationReview.OrderBy(c => c.score))
                {
                    string label = CriterionNames[Array.IndexOf(CriterionIds, c.id)] + ": " + c.score + "/5. " + c.reason;
                    if (c.score < 5) r.improvementNotes.Add(label);
                    else r.positiveNotes.Add(label);
                }
                var weakest = r.communicationReview.OrderBy(c => c.score).First();
                r.observation = "Comunicación " + r.communication + "/25. " + CriterionNames[Array.IndexOf(CriterionIds, weakest.id)] + ": " + weakest.score + "/5. " + weakest.reason;
            }
            else
            {
                if (r.communication == 25) r.positiveNotes.Add("La rúbrica local registra comunicación completa.");
                else r.improvementNotes.Add("Comunicación " + r.communication + "/25: revisá las conductas registradas en el informe.");
                r.observation = r.Total == 100 ? "Cumpliste todos los criterios de la rúbrica local. El informe muestra cómo se obtuvo cada punto." : "Comunicación " + r.communication + "/25 según las conductas registradas. El informe detalla pertinencia, claridad, contención y repeticiones.";
            }
            if(r.decision<20) r.observation=r.resource=="Sin despacho"?"La llamada terminó sin ayuda asignada: 0/20 en decisión táctica. Revisá el recurso requerido antes de cerrar.":"El recurso asignado no corresponde al caso: 0/20 en decisión táctica. Revisá la unidad seleccionada.";
            else if(r.information<25) r.observation="Precisión "+r.information+"/40. Faltaron datos esenciales por verificar; el informe enumera cuáles y cuánto vale cada uno.";
            if (r.efficiency < 10) r.improvementNotes.Add("Eficiencia " + r.efficiency + "/10: revisá datos pendientes y repeticiones.");
            r.feedback = new List<string>(r.improvementNotes);
        }
        public static CallResult Aggregate(IList<CallResult> calls)
        {
            var r = new CallResult { resource = "Resumen de guardia", evaluationSource = "Promedio de los casos", areas = new ScoreAreaReview[5] };
            if (calls.Count == 0) return r;
            int[] values = new int[5];
            for (int i=0;i<5;i++) { values[i] = Mathf.RoundToInt((float)calls.Sum(c=>Values(c)[i])/calls.Count); r.areas[i] = new ScoreAreaReview { name=Names[i],score=values[i],maximum=Maxima[i],explanation="Promedio de " + calls.Count + " casos. Consultá el historial para ver las evidencias de cada llamada." }; }
            r.information=values[0];r.communication=values[1];r.decision=values[2];r.efficiency=values[3];r.handling=values[4];
            r.seconds=calls.Sum(c=>c.seconds);r.turns=calls.Sum(c=>c.turns);r.operatorCharacters=calls.Sum(c=>c.operatorCharacters);
            r.evaluationPending=calls.Any(c=>c.evaluationPending); r.grade="Guardia completada";
            foreach (var c in calls) { r.transcriptLog.Add("CASO " + c.caseId + " · " + c.Total + "/100 · " + c.evaluationSource); r.transcriptLog.AddRange(c.transcriptLog); }
            r.observation="Promedio por área de " + calls.Count + " llamadas. El total es la suma de las cinco áreas redondeadas; el tiempo corresponde a toda la guardia.";
            foreach(var c in calls) (c.Total>=75?r.positiveNotes:r.improvementNotes).Add(c.caseId + ": " + c.Total + "/100 · " + c.grade + ".");
            return r;
        }
    }
}
