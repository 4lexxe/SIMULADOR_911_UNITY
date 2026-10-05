using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EmergencyDispatch
{
    public partial class GameManager
    {
        public GuardSave SavedGuard { get; private set; }
        public bool HasCurrentGuard => Queue != null && !shiftComplete && !(ExamMode && ExamFinished);
        public bool CanContinueGuard => HasCurrentGuard || SavedGuard != null && SavedGuard.calls.Count < cases.Count;
        public bool CanOpenMenu => !Busy && !Voice.Recording && !Voice.Playing && Phase != CallPhase.Decision;
        public void RefreshSavedGuard()
        {
            if (Application.isBatchMode) return;
            SavedGuard = null;
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "ultimo-turno.json");
                if (File.Exists(path) && GuardSave.TryParse(File.ReadAllText(path), cases, out var saved)) SavedGuard = saved;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        public void StartAtCase(int caseIndex)
        {
            if (caseIndex < -1 || caseIndex >= cases.Count) return;
            ResetExam(); BeginGuard(caseIndex);
        }
        private void BeginGuard(int caseIndex)
        {
            View.ResetGuardView();
            relay.Disconnect(); Voice.StopTalking(); Voice.ClearPlayback();
            Results.Clear(); index = -1; Session = null; shiftComplete = false; Busy = false;
            callTranscript.Clear(); inputTranscript = ""; callerTurnStarted = operatorTurnRecorded = false;
            Queue = new IncomingCallQueue(cases, 0, caseIndex >= 0 ? cases[caseIndex].id : "");
            Fleet.ResetUnits(); NextCase();
            if (caseIndex >= 0) SelectIncomingCall(cases[caseIndex].id);
        }
        public void ContinueGuard()
        {
            if (HasCurrentGuard) { View.HideLobby(); View.Refresh(); if(CanChooseCall) View.IncomingBoard.Show(); return; }
            if (SavedGuard == null || SavedGuard.calls.Count >= cases.Count) return;
            var saved = SavedGuard;
            ResetExam();
            if(saved.version>=2) { LiveMode=saved.liveMode;ConnectionStatus=LiveMode?"VOZ · GEMINI LIVE":"ENSAYO LOCAL · TEXTO"; }
            Results.Clear(); Results.AddRange(saved.calls); index = -1;
            Queue = new IncomingCallQueue(cases, saved.queueElapsed, saved.priorityCaseId);
            Session = null; shiftComplete = false; Busy = false;
            Fleet.ResetUnits();
            foreach (var previous in saved.units)
            {
                if (previous == null) continue;
                var unit = Fleet.Units.FirstOrDefault(u => u.id == previous.id);
                if (unit == null || (int)previous.status < (int)UnitStatus.Available || (int)previous.status > (int)UnitStatus.Unavailable) continue;
                unit.status = previous.status; unit.remainingEtaSeconds = Mathf.Clamp(previous.remainingEtaSeconds, 0, 3600);
                unit.assignedCaseId = previous.assignedCaseId ?? "";
            }
            NextCase();
            if (!string.IsNullOrEmpty(saved.selectedCaseId)) SelectIncomingCall(saved.selectedCaseId);
        }
    }
}
