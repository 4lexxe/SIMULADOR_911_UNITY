using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyDispatch
{
    [Serializable] public class GuardSave
    {
        public int version = 3, nextCaseIndex;
        public string date;
        public float queueElapsed;
        public string selectedCaseId = "", priorityCaseId = "";
        public bool liveMode;
        public List<CallResult> calls = new List<CallResult>();
        public List<EmergencyUnit> units = new List<EmergencyUnit>();

        public static bool TryParse(string json, List<EmergencyCase> cases, out GuardSave save)
        {
            save = null;
            try
            {
                var parsed = JsonUtility.FromJson<GuardSave>(json);
                if (parsed == null || string.IsNullOrWhiteSpace(parsed.date) || parsed.calls == null || parsed.calls.Count > cases.Count) return false;
                int last = -1;
                int version = json.Contains("\"version\"") ? parsed.version : 1;
                if (version < 1 || version > 3) return false;
                var completed = new HashSet<string>();
                foreach (var result in parsed.calls)
                {
                    if (result == null) return false;
                    int number = cases.FindIndex(c => c.id == result.caseId);
                    if (number < 0 || !completed.Add(result.caseId) || (version < 3 && number <= last)) return false;
                    int[] values = DispatchAudit.Values(result);
                    for (int i = 0; i < values.Length; i++) if (values[i] < 0 || values[i] > DispatchAudit.Maxima[i]) return false;
                    result.transcriptLog = result.transcriptLog ?? new List<string>();
                    result.positiveNotes = result.positiveNotes ?? new List<string>();
                    result.improvementNotes = result.improvementNotes ?? new List<string>();
                    result.feedback = result.feedback ?? new List<string>();
                    if(result.evaluationPending)result.evaluationSource="Local · auditoría IA interrumpida";
                    result.evaluationPending = false;
                    last = number;
                }
                if (parsed.nextCaseIndex < 0 || parsed.nextCaseIndex > cases.Count) return false;
                if (version < 3)
                {
                    if (version < 2 || (parsed.nextCaseIndex == 0 && last >= 0)) parsed.nextCaseIndex = last + 1;
                    if (parsed.nextCaseIndex < last + 1) return false;
                    parsed.selectedCaseId = parsed.nextCaseIndex < cases.Count ? cases[parsed.nextCaseIndex].id : "";
                    parsed.priorityCaseId = parsed.selectedCaseId;
                    parsed.queueElapsed = 0;
                }
                if (float.IsNaN(parsed.queueElapsed) || float.IsInfinity(parsed.queueElapsed) || parsed.queueElapsed < 0) return false;
                parsed.selectedCaseId = parsed.selectedCaseId ?? ""; parsed.priorityCaseId = parsed.priorityCaseId ?? "";
                if (parsed.priorityCaseId.Length > 0 && !cases.Exists(c => c.id == parsed.priorityCaseId)) return false;
                var queue = new IncomingCallQueue(cases, parsed.queueElapsed, parsed.priorityCaseId);
                if (parsed.selectedCaseId.Length > 0 && !cases.Exists(c => c.id == parsed.selectedCaseId && !completed.Contains(c.id) && queue.HasArrived(c))) return false;
                parsed.version = 3;
                parsed.units = parsed.units ?? new List<EmergencyUnit>();
                save = parsed; return true;
            }
            catch (Exception) { return false; }
        }
    }
    public static class DispatchPreferences
    {
        public static string OperatorName => PlayerPrefs.GetString("ULTIMA_OPERATOR_NAME", "OP-07");
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat("ULTIMA_VOLUME", .8f));
        public static void Save(string name, float volume, bool live)
        {
            if (Application.isBatchMode) return;
            name = (name ?? "").Trim(); if (name.Length == 0) name = "OP-07";
            PlayerPrefs.SetString("ULTIMA_OPERATOR_NAME", name.Length > 18 ? name.Substring(0, 18) : name);
            PlayerPrefs.SetFloat("ULTIMA_VOLUME", Mathf.Clamp01(volume));
            PlayerPrefs.SetInt("ULTIMA_LIVE_MODE", live ? 1 : 0); PlayerPrefs.Save();
        }
    }
}
