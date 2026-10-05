namespace EmergencyDispatch
{
    public static class VoiceNavigation
    {
        // Match the whole recognized utterance; conversational mentions are not commands.
        public static bool TryParse(string transcript, out DeskCameraController.CameraViewMode view)
        {
            view = DeskCameraController.CameraViewMode.DeskOverview;
            string words = System.Text.RegularExpressions.Regex.Replace(OfflineConversation.Normalize(transcript), @"[^\p{L}\p{Nd}\s]", " ");
            words = System.Text.RegularExpressions.Regex.Replace(words, @"\s+", " ").Trim();
            switch (words)
            {
                case "central mostrar mapa": view = DeskCameraController.CameraViewMode.LeftMonitor; return true;
                case "central mostrar llamada": view = DeskCameraController.CameraViewMode.CenterMonitor; return true;
                case "central mostrar unidades": view = DeskCameraController.CameraViewMode.RightMonitor; return true;
                case "central mostrar escritorio": return true;
                default: return false;
            }
        }
    }
}
