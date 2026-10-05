using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyDispatch
{
    public enum UnitType { Ambulance, Fire, Police }
    public enum UnitStatus { Available, EnRoute, OnScene, Unavailable }
    public enum CallPhase { Idle, Incoming, Active, Decision, Result, ShiftComplete }
    [Serializable] public class CaseCatalogue { public EmergencyCase[] cases; }
    [Serializable] public class CaseFact
    {
        public string id, label, value, response;
        public bool uncertain;
        public string verifiedValue, verificationResponse;
        public string[] keywords;
        public int points;
    }
    [Serializable] public class EmergencyCase
    {
        public string id, title, callerName, personality, opening;
        public string locality, region, incomingSummary;
        public float latitude, longitude, arrivalSeconds;
        public UnitType requiredUnitType;
        public Vector2 mapNormalizedCoord;
        public CaseFact[] facts;
    }
    [Serializable] public class EmergencyUnit
    {
        public string id, displayName, baseLocation, assignedCaseId = "";
        public UnitType type;
        public UnitStatus status;
        public float baseEtaMinutes, remainingEtaSeconds;
    }
    [Serializable] public class FactRecord
    {
        public string id, value;
        public bool verified;
    }
    [Serializable] public class CallResult
    {
        public string caseId, grade, resource;
        public int information, communication, decision, efficiency, handling;
        public float seconds;
        public string observation;
        public string evaluationSource = "Evaluación local";
        public bool evaluationPending;
        public int turns, operatorCharacters;
        public ScoreAreaReview[] areas;
        public CommunicationCriterion[] communicationReview;
        public List<string> positiveNotes = new List<string>();
        public List<string> improvementNotes = new List<string>();
        public List<string> feedback = new List<string>();
        public List<string> transcriptLog = new List<string>();
        public int Total => information + communication + decision + efficiency + handling;
    }

    // The model proposes evidence; only the game owns facts, state and scoring.
    public sealed class CallSession
    {
        public EmergencyCase Definition { get; }
        public CallPhase Phase { get; private set; } = CallPhase.Incoming;
        public readonly Dictionary<string, FactRecord> Facts = new Dictionary<string, FactRecord>();
        public EmergencyUnit SelectedUnit { get; private set; }
        public EmergencyUnit DispatchedUnit { get; private set; }
        public int Turns { get; private set; }
        private readonly HashSet<int> navigationTurns = new HashSet<int>();
        public int ScoredTurns => Turns - navigationTurns.Count;
        public void ExcludeNavigationTurn(int turn)
        {
            if (turn <= 0 || turn > Turns) return;
            navigationTurns.Add(turn); tags.Remove(turn);
        }
        public int Calm { get; private set; } = 35;
        public int Trust { get; private set; } = 40;
        public int Cooperation { get; private set; } = 50;
        public float Seconds;
        public CallResult Result { get; private set; }
        private readonly Dictionary<int, HashSet<string>> tags = new Dictionary<int, HashSet<string>>();
        private readonly HashSet<int> emotionTurns = new HashSet<int>();
        private readonly HashSet<string> events = new HashSet<string>();
        public CallSession(EmergencyCase definition) { Definition = definition; }
        public bool Answer()
        {
            if (Phase != CallPhase.Incoming) return false;
            Phase = CallPhase.Active; return true;
        }
        public int BeginTurn()
        {
            if (Phase != CallPhase.Active) return 0;
            tags[++Turns] = new HashSet<string>(); return Turns;
        }
        public bool RegisterFact(int turn, string eventId, string id, string value, bool verified)
        {
            if (Phase != CallPhase.Active && Phase != CallPhase.Decision) return false;
            if (turn < 0 || turn > Math.Max(1, Turns)) return false;
            if (string.IsNullOrEmpty(eventId) || events.Contains(eventId)) return false;
            CaseFact fact = Array.Find(Definition.facts, f => f.id == id);
            if (fact == null) return false;
            string canonical = fact.uncertain && verified ? fact.verifiedValue : fact.value;
            if (value != canonical) return false;
            events.Add(eventId);
            if (Facts.TryGetValue(id, out var old) && (old.verified || !verified)) return false;
            Facts[id] = new FactRecord { id = id, value = canonical, verified = !fact.uncertain || verified };
            return true;
        }
        public bool RegisterTag(int turn, string tag)
        {
            if (Phase != CallPhase.Active && Phase != CallPhase.Decision) return false;
            if (turn <= 0 || !tags.ContainsKey(turn)) return false;
            switch (tag)
            {
                case "relevant": case "clear": case "active_listening": case "repeated":
                case "confusing": case "interruption": case "confirmation": case "closure":
                    return tags[turn].Add(tag);
                default: return false;
            }
        }
        public void AdjustEmotion(int turn, int delta)
        {
            if (Phase != CallPhase.Active && Phase != CallPhase.Decision) return;
            if (turn > 0 && !emotionTurns.Add(turn)) return;
            delta = Mathf.Clamp(delta, -5, 5);
            Calm = Mathf.Clamp(Calm + delta, 0, 100);
            Trust = Mathf.Clamp(Trust + delta, 0, 100);
            Cooperation = Mathf.Clamp(Cooperation + delta, 0, 100);
        }
        private bool CanAccept(int turn) => (Phase == CallPhase.Active || Phase == CallPhase.Decision);
        public bool SelectUnit(EmergencyUnit unit)
        {
            if (Phase != CallPhase.Active || DispatchedUnit != null || unit == null || unit.status != UnitStatus.Available || !Facts.ContainsKey("location")) return false;
            SelectedUnit = unit; Phase = CallPhase.Decision; return true;
        }
        public void CancelDecision() { if (Phase == CallPhase.Decision) { SelectedUnit = null; Phase = CallPhase.Active; } }
        public bool ConfirmDispatch()
        {
            if (Phase != CallPhase.Decision || SelectedUnit == null || SelectedUnit.status != UnitStatus.Available) return false;
            DispatchedUnit = SelectedUnit;
            DispatchedUnit.assignedCaseId = Definition.id;
            DispatchedUnit.status = UnitStatus.EnRoute;
            DispatchedUnit.remainingEtaSeconds = DispatchedUnit.baseEtaMinutes * 60;
            Phase = CallPhase.Active; return true;
        }
        public int Count(string tag)
        {
            int count = 0;
            foreach (var turn in tags.Values) if (turn.Contains(tag)) count++;
            return count;
        }
        public string GenerateObservation(CallResult result)
        {
            var parts = new List<string>();
            int confusing = Count("confusing");
            int listening = Count("active_listening");
            int closure = Count("closure");
            int repeated = Count("repeated");

            if (confusing > 0)
                parts.Add("Al inicio se acumularon varias preguntas juntas y la persona se sintió abrumada.");
            else if (listening >= 2 && Calm >= 45)
                parts.Add("Excelente manejo de la calma y empatía para contener a la persona en shock.");
            else if (listening > 0)
                parts.Add("Buena contención emocional para facilitar el diálogo.");

            if (result.information >= 40)
                parts.Add("Se completó la totalidad de la información clave del caso con precisión.");
            else if (result.information >= 25)
                parts.Add("Se reunieron los datos principales, aunque quedaron detalles relevantes sin verificar.");
            else
                parts.Add("La recolección de datos fue insuficiente para dimensionar el incidente.");

            if (repeated > 1)
                parts.Add("Hubo preguntas reiteradas sobre datos ya informados.");

            if (DispatchedUnit == null)
            {
                parts.Add("No se coordinó el envío de ninguna unidad de auxilio.");
            }
            else if (result.decision == 20)
            {
                if (closure > 0)
                    parts.Add("Se despachó la unidad correspondiente (" + DispatchedUnit.displayName + ") y se comunicó el cierre claramente.");
                else
                    parts.Add("Se despachó la unidad correcta (" + DispatchedUnit.displayName + ").");
            }
            else
            {
                parts.Add("La unidad enviada no era la adecuada para el tipo de incidente.");
            }

            return string.Join(" ", parts);
        }
        public CallResult Finish()
        {
            if (Result != null) return Result;
            if (Phase != CallPhase.Active) return null;
            var result = new CallResult { caseId = Definition.id, seconds = Seconds, resource = DispatchedUnit?.displayName ?? "Sin despacho" };
            foreach (var fact in Definition.facts)
            {
                if (Facts.TryGetValue(fact.id, out var found) && found.verified) result.information += fact.points;
                else result.feedback.Add(found == null ? "Faltó obtener: " + fact.label + "." : "Faltó verificar: " + fact.label + ".");
            }
            int relevant = Mathf.Min(15, Count("relevant") * 3);
            int clarity = Mathf.Clamp(Count("clear") - Count("confusing"), 0, 5);
            int listening = Mathf.Clamp(Count("active_listening") * 3 - Count("interruption"), 0, 5);
            result.communication = Mathf.Clamp(relevant + clarity + listening - Count("repeated"), 0, 25);
            result.decision = DispatchedUnit != null && DispatchedUnit.type == Definition.requiredUnitType ? 20 : 0;
            result.efficiency = result.information >= 20 ? Mathf.Clamp(10 - Mathf.Max(0, ScoredTurns - 8) - Count("repeated"), 0, 10) : 0;
            result.handling = (Count("confirmation") > 0 ? 2 : 0) + (DispatchedUnit != null && Count("closure") > 0 ? 3 : 0);
            if (result.decision == 0) result.feedback.Add(DispatchedUnit == null ? "Finalizaste sin asignar un recurso." : "El recurso no coincide con las reglas de este caso ficticio.");
            if (listening < 5) result.feedback.Add("Reconocé lo que expresa la persona y verificá que la entendiste.");
            if (Count("confirmation") == 0) result.feedback.Add("Confirmá los datos importantes antes de cerrar.");
            if (Count("closure") == 0) result.feedback.Add("Comunicá el cierre y el recurso asignado.");
            if (result.feedback.Count == 0) result.feedback.Add("Obtuviste y verificaste la información. Comunicaste el cierre y asignaste el recurso previsto.");
            result.grade = result.Total >= 90 ? "Excelente" : result.Total >= 75 ? "Caso resuelto" : result.Total >= 60 ? "Resolución parcial" : "Información insuficiente";
            DispatchAudit.BuildLocal(this, result);
            Result = result; Phase = CallPhase.Result; return result;
        }
    }
}
