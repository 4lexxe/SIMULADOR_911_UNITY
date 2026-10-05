using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    [RequireComponent(typeof(AudioSource))]
    public class VoiceManager : MonoBehaviour
    {
        public bool Recording { get; private set; }
        public bool Playing { get { lock (audioLock) return buffered > 0; } }
        public float Level { get; private set; }
        public long ReceivedSamples { get; private set; }
        public event Action<string> Problem;
        public Func<bool> CanTalk;
        public Func<int> BeginTurn;
        public Action<int> TurnEnded;
        private GeminiEmergencyService relay;
        private AudioSource speaker;
        private AudioClip capture, playback;
        private int readPosition, turn;
        private float nextRead, captureStarted;
        private readonly object audioLock = new object();
        private readonly float[] samples = new float[24000 * 30];
        private int head, tail, buffered;
        private uint noiseSeed = 12345u;
        public void Initialize(GeminiEmergencyService service)
        {
            relay = service;
            speaker = GetComponent<AudioSource>();
            playback = AudioClip.Create("Gemini PCM 24k", 24000, 1, 24000, true, ReadAudio);
            speaker.clip = playback; speaker.Play();
        }
        private void ReadAudio(float[] output)
        {
            lock (audioLock)
            {
                for (int i = 0; i < output.Length; i++)
                {
                    float s = 0f;
                    if (buffered > 0)
                    {
                        s = samples[head];
                        head = (head + 1) % samples.Length;
                        buffered--;
                        // Ruido sutil de línea telefónica analógica / celular (calculado de forma segura en el thread de audio)
                        noiseSeed = (noiseSeed * 1664525u + 1013904223u);
                        float noise = ((float)(noiseSeed & 0xFFFF) / 65535f - 0.5f) * 0.0035f;
                        s += noise;
                    }
                    output[i] = s;
                }
            }
        }
        public void ReceiveAudio(string base64, int rate)
        {
            if (rate != 24000 || string.IsNullOrEmpty(base64)) return;
            byte[] pcm;
            try { pcm = Convert.FromBase64String(base64); } catch (FormatException) { return; }
            ReceivedSamples += pcm.Length / 2;
            lock (audioLock)
            {
                for (int i = 0; i + 1 < pcm.Length && buffered < samples.Length; i += 2)
                {
                    samples[tail] = (short)(pcm[i] | pcm[i + 1] << 8) / 32768f;
                    tail = (tail + 1) % samples.Length; buffered++;
                }
            }
        }
        public void ClearPlayback() { lock (audioLock) { buffered = 0; head = tail = 0; } }
        private void Update()
        {
            var keyboard = Keyboard.current;
            bool typing = DispatchView.SafeIsTyping();
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && !typing) StartTalking();
            if (Recording && (keyboard == null || keyboard.spaceKey.wasReleasedThisFrame || CanTalk?.Invoke() != true)) StopTalking();
            if (Recording && Time.unscaledTime >= nextRead) { FlushCapture(); nextRead = Time.unscaledTime + .04f; }
            if (Recording && Time.unscaledTime - captureStarted > 30) StopTalking();
            if (!Recording) Level = Mathf.MoveTowards(Level, 0, Time.deltaTime * 2);
        }
        public void StartTalking()
        {
            if (Recording || CanTalk?.Invoke() != true || !relay.Ready) return;
            if (Microphone.devices.Length == 0) { Problem?.Invoke("No se detectó un micrófono. Podés usar la entrada de texto."); return; }
            try { capture = Microphone.Start(null, true, 2, 16000); }
            catch (Exception) { Problem?.Invoke("No se pudo abrir el micrófono. Revisá el permiso de Windows."); return; }
            if (capture == null) { Problem?.Invoke("El micrófono no está disponible."); return; }
            turn = BeginTurn?.Invoke() ?? 0;
            ClearPlayback();
            Recording = true; readPosition = 0; captureStarted = Time.unscaledTime;
            relay.Send(new RelayMessage { type = "activity_start", turnId = turn });
        }
        private void FlushCapture()
        {
            if (capture == null) return;
            int write = Microphone.GetPosition(null);
            if (write < 0) { Problem?.Invoke("Se perdió el micrófono."); StopTalking(); return; }
            int count = (write - readPosition + capture.samples) % capture.samples;
            if (count == 0) return;
            var raw = new float[count * capture.channels];
            capture.GetData(raw, readPosition);
            readPosition = write;
            int outputCount = Mathf.FloorToInt(count * 16000f / capture.frequency);
            var pcm = new byte[outputCount * 2];
            float peak = 0;
            for (int i = 0; i < outputCount; i++)
            {
                int index = Mathf.Min(count - 1, Mathf.FloorToInt(i * capture.frequency / 16000f));
                float sample = 0;
                for (int ch = 0; ch < capture.channels; ch++) sample += raw[index * capture.channels + ch];
                sample /= capture.channels;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
                short value = (short)(Mathf.Clamp(sample, -1, 1) * 32767);
                pcm[i * 2] = (byte)(value & 255); pcm[i * 2 + 1] = (byte)((value >> 8) & 255);
            }
            Level = peak;
            relay.Send(new RelayMessage { type = "audio", turnId = turn, sampleRate = 16000, data = Convert.ToBase64String(pcm) });
        }
        public void StopTalking()
        {
            if (!Recording) return;
            Recording = false; // Set first to make failure paths non-recursive.
            FlushCapture();
            Microphone.End(null);
            if (capture != null) Destroy(capture);
            capture = null;
            relay.Send(new RelayMessage { type = "activity_end", turnId = turn });
            TurnEnded?.Invoke(turn);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) StopTalking(); }
        private void OnDestroy()
        {
            StopTalking();
            if (playback != null) Destroy(playback);
        }
    }
}
