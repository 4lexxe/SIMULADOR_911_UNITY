using System.Collections.Generic;
using UnityEngine;

namespace EmergencyDispatch
{
    public partial class GameManager
    {
        public IReadOnlyList<EmergencyCase> Cases => cases;
        public IncomingCallQueue Queue { get; private set; }
        public int RemainingCalls => (ExamMode ? 1 : cases.Count) - Results.Count;
        public bool CanChooseCall => Queue != null && !shiftComplete && (Phase == CallPhase.Idle || Phase == CallPhase.Incoming);
        public bool IsCompleted(string caseId) => Results.Exists(r => r.caseId == caseId);
        public bool IsWaiting(EmergencyCase definition) => IsCaseInScope(definition.id) && Queue != null && Queue.HasArrived(definition) && !IsCompleted(definition.id) &&
            !(Session?.Definition.id == definition.id && (Phase == CallPhase.Active || Phase == CallPhase.Decision));
        public int WaitingCallCount { get { int count = 0; foreach (var c in cases) if (IsWaiting(c)) count++; return count; } }

        public bool SelectIncomingCall(string caseId)
        {
            if (!CanChooseCall) return false;
            int selected = cases.FindIndex(c => c.id == caseId);
            if (selected < 0 || !IsWaiting(cases[selected])) return false;
            index = selected; Session = new CallSession(cases[index]);
            CallerText = "Señal de origen: " + cases[index].locality + ". Dirección por confirmar.";
            OperatorText = ""; Notice = "Llamada seleccionada. Atendé cuando estés listo.";
            EmergencyAudioManager.Instance?.PlayCallSelect();
            SaveResults(); View.Refresh(); return true;
        }

        public void OpenIncomingMap()
        {
            if (Queue == null) { StartShift(); return; }
            View.IncomingBoard.Show();
        }

        public GuardSave CreateGuardSnapshot() => new GuardSave
        {
            date = System.DateTime.UtcNow.ToString("o"), calls = new List<CallResult>(Results),
            units = Fleet.Units, liveMode = LiveMode, nextCaseIndex = Results.Count,
            queueElapsed = Queue?.Elapsed ?? 0, priorityCaseId = Queue?.PriorityCaseId ?? "",
            selectedCaseId = Session != null && !IsCompleted(Session.Definition.id) ? Session.Definition.id : ""
        };
    }
}
