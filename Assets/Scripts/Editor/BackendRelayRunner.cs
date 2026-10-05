using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EmergencyDispatch.Editor
{
    [InitializeOnLoad]
    public static class BackendRelayRunner
    {
        private static Process relayProcess;
        private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        static BackendRelayRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static async void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (Application.isBatchMode) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                bool isRunning = await IsRelayRunningAsync();
                if (!isRunning)
                {
                    Debug.Log("<color=#00e5ff>[Última Llamada]</color> Iniciando backend relay de Gemini Live automáticamente...");
                    StartRelay();
                }
                else
                {
                    Debug.Log("<color=#00e5ff>[Última Llamada]</color> Backend relay de Gemini Live detectado y activo en el puerto 8787.");
                }
            }
        }

        [MenuItem("Tools/Última Llamada/⚡ Iniciar Relay de Voz Gemini (Node.js)")]
        public static async void StartRelayMenu()
        {
            if (await IsRelayRunningAsync())
            {
                EditorUtility.DisplayDialog("Relay de Voz Activo", "El servidor relay ya está ejecutándose correctamente en http://127.0.0.1:8787/health con Gemini Live.", "Aceptar");
                return;
            }

            StartRelay();
            await Task.Delay(1000);
            if (await IsRelayRunningAsync())
            {
                EditorUtility.DisplayDialog("Relay Iniciado", "¡El relay de Gemini Live se inició exitosamente en http://127.0.0.1:8787!", "Excelente");
            }
            else
            {
                EditorUtility.DisplayDialog("Aviso", "Se envió la orden de inicio. Comprobá la consola de Unity.", "Aceptar");
            }
        }

        [MenuItem("Tools/Última Llamada/⏹️ Detener Relay de Voz")]
        public static void StopRelayMenu()
        {
            StopRelay();
            EditorUtility.DisplayDialog("Relay Detenido", "El proceso del relay ha sido finalizado.", "Aceptar");
        }

        [MenuItem("Tools/Última Llamada/🔍 Probar Conexión con Gemini Live")]
        public static async void TestConnectionMenu()
        {
            try
            {
                var response = await http.GetAsync("http://127.0.0.1:8787/health");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    EditorUtility.DisplayDialog("Conexión Exitosa", $"El relay respondió correctamente (HTTP 200):\n{json}", "Aceptar");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error de Relay", $"El relay respondió con error HTTP {(int)response.StatusCode}", "Aceptar");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Relay no disponible", $"No se pudo conectar al relay en http://127.0.0.1:8787/health.\n\nError: {ex.Message}\n\nPuedes iniciarlo desde el menú:\nTools → Última Llamada → Iniciar Relay de Voz Gemini", "Aceptar");
            }
        }

        public static async Task<bool> IsRelayRunningAsync()
        {
            try
            {
                var resp = await http.GetAsync("http://127.0.0.1:8787/health");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static void StartRelay()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string backendDir = Path.Combine(projectRoot, "Backend");
                string serverScript = Path.Combine(backendDir, "server.mjs");

                if (!File.Exists(serverScript))
                {
                    Debug.LogError($"[Última Llamada] No se encontró server.mjs en: {serverScript}");
                    return;
                }

                // Check if .env or .env.local exists
                string envFile = Path.Combine(backendDir, ".env");
                string envLocalFile = Path.Combine(backendDir, ".env.local");
                if (!File.Exists(envFile) && File.Exists(envLocalFile))
                {
                    File.Copy(envLocalFile, envFile, true);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--env-file-if-exists=.env server.mjs",
                    WorkingDirectory = backendDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                relayProcess = new Process { StartInfo = startInfo };
                relayProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.Log($"<color=#00e5ff>[Relay Node]</color> {e.Data}"); };
                relayProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.LogWarning($"<color=#ffaa00>[Relay Node Error]</color> {e.Data}"); };

                relayProcess.Start();
                relayProcess.BeginOutputReadLine();
                relayProcess.BeginErrorReadLine();

                Debug.Log($"<color=#00e5ff>[Última Llamada]</color> Proceso Node.js iniciado (PID {relayProcess.Id}). Escuchando en ws://127.0.0.1:8787");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Última Llamada] Error al iniciar relay de Node.js: {ex.Message}. Asegúrate de tener Node.js instalado.");
            }
        }

        public static void StopRelay()
        {
            if (relayProcess != null && !relayProcess.HasExited)
            {
                try
                {
                    relayProcess.Kill();
                    relayProcess.Dispose();
                }
                catch { }
                relayProcess = null;
            }
        }
    }
}
