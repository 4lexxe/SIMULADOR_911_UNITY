using System;
namespace EmergencyDispatch
{
    [Serializable] public class RelayMessage
    {
        public string type, caseId, eventId, factId, value, text, data, tag;
        public int turnId, sampleRate, delta;
        public bool verified;
    }
}
