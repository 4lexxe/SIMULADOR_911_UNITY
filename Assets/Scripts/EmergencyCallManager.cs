using UnityEngine;
namespace EmergencyDispatch
{
    // Kept as a small scene-facing adapter; rules and score live in CallSession.
    public class EmergencyCallManager : MonoBehaviour
    {
        public void Answer() => GameManager.Instance?.Answer();
        public void SendOperatorQuery(string text) => GameManager.Instance?.SubmitText(text);
        public void Finish() => GameManager.Instance?.RequestFinish();
    }
}
