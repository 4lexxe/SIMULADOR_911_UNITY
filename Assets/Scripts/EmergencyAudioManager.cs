using UnityEngine;
namespace EmergencyDispatch
{
    [RequireComponent(typeof(AudioSource))]
    public class EmergencyAudioManager : MonoBehaviour
    {
        public static EmergencyAudioManager Instance { get; private set; }
        private AudioSource source;
        private AudioClip click, beep, ring, end, dispatch, hover, select, signal, tally, reward, completion;
        private float nextHover;
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            source = GetComponent<AudioSource>();
            click = Tone("Click", .04f, 880, 880);
            beep = Tone("Dato", .1f, 880, 1200);
            ring = Tone("Llamada entrante", .65f, 440, 480);
            end = Tone("Fin de llamada", .18f, 420, 300);
            dispatch = Tone("Despacho", .22f, 660, 880);
            hover = Tone("Enfoque", .028f, 520, 680);
            select = Melody("Señal seleccionada", new[]{523.25f,783.99f}, .08f);
            signal = Melody("Nueva señal", new[]{783.99f,659.25f,783.99f}, .09f);
            tally = Tone("Conteo", .036f, 1100, 1400);
            reward = Melody("Evaluación alta", new[]{523.25f,659.25f,783.99f,1046.5f}, .105f);
            completion = Melody("Evaluación completa", new[]{523.25f,587.33f,659.25f}, .12f);
        }
        private static AudioClip Tone(string name, float duration, float a, float b)
        {
            const int rate = 24000;
            var data = new float[(int)(duration * rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate, envelope = Mathf.Sin(Mathf.PI * i / data.Length);
                data[i] = Mathf.Sin(2 * Mathf.PI * (i < data.Length / 2 ? a : b) * t) * envelope * .4f;
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        private static AudioClip Melody(string name, float[] notes, float beat)
        {
            const int rate=24000; var data=new float[Mathf.CeilToInt(notes.Length*beat*rate)];
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate;int note=Mathf.Min(notes.Length-1,(int)(t/beat));float local=t-note*beat;
                float envelope=Mathf.Min(1,local/.008f)*Mathf.Pow(Mathf.Clamp01(1-local/beat),1.6f);
                data[i]=Mathf.Sin(2*Mathf.PI*notes[note]*local)*envelope*.32f;
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void PlayHover() { if(Time.unscaledTime<nextHover)return;nextHover=Time.unscaledTime+.08f;source?.PlayOneShot(hover,.35f); }
        public void PlayCallSelect() => source?.PlayOneShot(select,.7f);
        public void PlayNewSignal() => source?.PlayOneShot(signal,.5f);
        public void PlayScoreTick() => source?.PlayOneShot(tally,.28f);
        public void PlayScoreReveal(int score) => source?.PlayOneShot(score>=75?reward:completion,.8f);
        public void PlayButtonClick() { source?.PlayOneShot(click); }
        public void PlayRadioBeep() { source?.PlayOneShot(beep); }
        public void PlayIncomingRing() { source?.PlayOneShot(ring); }
        public void PlayCallEnd() { source?.PlayOneShot(end); }
        public void PlayDispatchSuccess() { source?.PlayOneShot(dispatch); }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var clip in new[] { click, beep, ring, end, dispatch, hover, select, signal, tally, reward, completion }) if (clip != null) Destroy(clip);
        }
    }
}
