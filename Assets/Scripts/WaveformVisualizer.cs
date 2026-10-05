using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public class WaveformVisualizer : MonoBehaviour
    {
        [Header("Waveform Settings")]
        [SerializeField] private int barCount = 36;
        [SerializeField] private float maxBarHeight = 42f;
        [SerializeField] private float minBarHeight = 4f;
        [SerializeField] private Color waveColor = new Color(0.35f, 0.65f, 0.95f, 0.9f);

        [SerializeField] private RectTransform[] bars;
        [SerializeField] private Image[] barImages;
        private bool isSpeaking = true;
        private float speechTimer = 0f;


        public void SetSpeaking(bool speaking)
        {
            isSpeaking = speaking;
        }

        private void Update()
        {
            if (bars == null || bars.Length == 0) return;

            speechTimer += Time.deltaTime * 6f;

            for (int i = 0; i < bars.Length; i++)
            {
                float normalizedIndex = (float)i / barCount;
                // Bell curve envelope in center
                float centerEnvelope = Mathf.Sin(normalizedIndex * Mathf.PI);

                float targetHeight;
                if (isSpeaking)
                {
                    float noise1 = Mathf.PerlinNoise(i * 0.25f, speechTimer * 1.5f);
                    float noise2 = Mathf.Sin(speechTimer * 2f + i * 0.4f) * 0.5f + 0.5f;
                    float combined = (noise1 * 0.7f + noise2 * 0.3f) * centerEnvelope;
                    targetHeight = Mathf.Lerp(minBarHeight, maxBarHeight, combined);
                }
                else
                {
                    // Gentle idle pulse
                    float idleNoise = (Mathf.Sin(speechTimer * 0.5f + i * 0.15f) * 0.5f + 0.5f) * 0.2f * centerEnvelope;
                    targetHeight = Mathf.Lerp(minBarHeight, minBarHeight + 6f, idleNoise);
                }

                Vector2 size = bars[i].sizeDelta;
                size.y = Mathf.Lerp(size.y, targetHeight, Time.deltaTime * 18f);
                bars[i].sizeDelta = size;

                // Subtle alpha glow based on height
                if (barImages[i] != null)
                {
                    float alpha = Mathf.Lerp(0.4f, 1.0f, size.y / maxBarHeight);
                    Color col = waveColor;
                    col.a = alpha;
                    barImages[i].color = col;
                }
            }
        }
    }
}
