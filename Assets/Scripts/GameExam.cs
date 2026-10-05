using UnityEngine;

namespace EmergencyDispatch
{
    public partial class GameManager
    {
        public const float ExamDuration = 180f;
        public const string ExamCaseId = "CASE_001";
        public bool ExamMode { get; private set; }
        public bool ExamFinished { get; private set; }
        public bool ExamTimedOut { get; private set; }
        private double examStartedAt;
        private float examElapsed;
        public float ExamElapsed => ExamFinished ? examElapsed : Mathf.Clamp((float)(Time.realtimeSinceStartupAsDouble - examStartedAt), 0, ExamDuration);
        public float ExamRemaining => Mathf.Max(0, ExamDuration - ExamElapsed);
        public bool IsCaseInScope(string caseId) => !ExamMode || caseId == ExamCaseId;

        public void StartExam()
        {
            // Set the mode before initializing: the exam must never overwrite a saved guard.
            ResetExam(); ExamMode = true; examStartedAt = Time.realtimeSinceStartupAsDouble;
            BeginGuard(0);
            SetNotice("EXAMEN · 180 segundos desde el inicio. Atendé el caso de San Salvador de Jujuy. El menú no pausa el reloj.");
        }
        private void ResetExam() { ExamMode = ExamFinished = ExamTimedOut = false; examElapsed = 0; }
        private bool TryNavigationCommand(string text)
        {
            if (!VoiceNavigation.TryParse(text, out var view)) return false;
            DeskCameraController.Instance?.SetView(view);
            SetNotice("COMANDO RECONOCIDO · " + text);
            return true;
        }
        private void TickExam()
        {
            if (!ExamMode || ExamFinished || ExamRemaining > 0) return;
            ExamTimedOut = true;
            // Expiry must also close a pending dispatch, a recording or a stalled service.
            Voice.StopTalking(); relay.Disconnect(); Voice.ClearPlayback(); Busy = false;
            View.HideLobby(); View.IncomingBoard.Hide(); View.CloseModal();
            if (Session == null) SelectIncomingCall(ExamCaseId);
            if (Phase == CallPhase.Incoming) Session.Answer(); // No network connection at the deadline.
            if (Phase == CallPhase.Decision) Session.CancelDecision();
            FinishCall();
            SetNotice("TIEMPO CUMPLIDO · Se evalúa solamente lo registrado hasta los 180 segundos.");
        }
        private void CompleteExam()
        {
            if (!ExamMode) return;
            examElapsed = ExamElapsed; ExamFinished = true;
            Session.Seconds = Mathf.Min(Session.Seconds, ExamDuration);
        }
    }
}
