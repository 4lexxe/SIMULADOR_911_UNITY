using UnityEngine;
namespace EmergencyDispatch
{
    // Only a local relay address is stored in the player. Credentials belong to Backend/.env.
    public static class GeminiConfig
    {
        public static string BackendUrl
        {
            get => PlayerPrefs.GetString("ULTIMA_LLAMADA_BACKEND", "ws://127.0.0.1:8787");
            set { PlayerPrefs.SetString("ULTIMA_LLAMADA_BACKEND", value); PlayerPrefs.Save(); }
        }
        public static void RemoveLegacyCredential()
        {
            PlayerPrefs.DeleteKey("GEMINI_API_KEY_DISPATCH");
            PlayerPrefs.DeleteKey("GEMINI_MODEL_NAME_DISPATCH");
            PlayerPrefs.Save();
        }
    }
}
