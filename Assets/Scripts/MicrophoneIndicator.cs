using UnityEngine;

namespace EmergencyDispatch
{
    [RequireComponent(typeof(Renderer))]
    public sealed class MicrophoneIndicator : MonoBehaviour
    {
        public VoiceManager voice;
        public Color idle=new Color(.16f,.05f,.05f), recording=new Color(1,.04f,.08f);
        private Renderer indicator;
        private MaterialPropertyBlock properties;
        private void Awake() { indicator=GetComponent<Renderer>();properties=new MaterialPropertyBlock(); }
        private void Update()
        {
            bool active=voice!=null&&voice.Recording;
            indicator.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor",active?recording:idle);
            properties.SetColor("_EmissionColor",active?recording*2:Color.black);
            indicator.SetPropertyBlock(properties);
        }
    }
}
