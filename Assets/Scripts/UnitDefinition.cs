using UnityEngine;

namespace EmergencyDispatch
{
    // A dispatch resource is data, not a simulated vehicle in the operator's room.
    public sealed class UnitDefinition : MonoBehaviour
    {
        public EmergencyUnit initial;
        public EmergencyUnit CreateState() => JsonUtility.FromJson<EmergencyUnit>(JsonUtility.ToJson(initial));
    }
}
