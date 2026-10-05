using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyDispatch
{
    // Simulation time is paused by the menu and audit screen. Arrival never answers a call.
    public sealed class IncomingCallQueue
    {
        private readonly IReadOnlyList<EmergencyCase> cases;
        public float Elapsed { get; private set; }
        public string PriorityCaseId { get; }
        public IncomingCallQueue(IReadOnlyList<EmergencyCase> definitions, float elapsed = 0, string priorityCaseId = "")
        { cases = definitions; Elapsed = Mathf.Max(0, elapsed); PriorityCaseId = priorityCaseId ?? ""; }
        private float Arrival(EmergencyCase definition) => definition.id == PriorityCaseId ? 0 : definition.arrivalSeconds;
        public bool HasArrived(EmergencyCase definition) => Elapsed >= Arrival(definition);
        public float WaitingSeconds(EmergencyCase definition) => Mathf.Max(0, Elapsed - Arrival(definition));
        public float SecondsUntilNextArrival
        {
            get { float next = float.PositiveInfinity; foreach (var c in cases) if (!HasArrived(c)) next = Mathf.Min(next, Arrival(c) - Elapsed); return next; }
        }
        public bool Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return false;
            float previous = Elapsed; Elapsed += seconds;
            foreach (var c in cases) if (Arrival(c) > previous && Arrival(c) <= Elapsed) return true;
            return false;
        }
    }
}
