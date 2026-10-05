using System.Collections.Generic;
using UnityEngine;
namespace EmergencyDispatch
{
    public class UnitFleetManager : MonoBehaviour
    {
        public List<EmergencyUnit> Units { get; private set; } = new List<EmergencyUnit>();
        [Header("Permanent dispatch resources")]
        public UnitDefinition[] roster;
        [Tooltip("Solo acelera el viaje y la permanencia de las unidades; no altera el reloj ni la evaluación de la llamada.")]
        [Range(1, 12)] public float travelTimeScale = 6;
        public UnitType? Filter { get; private set; }
        public void ResetUnits()
        {
            var states=new List<EmergencyUnit>();
            foreach(var definition in roster)states.Add(definition.CreateState());
            InitializeUnits(states);
        }
        public void InitializeUnits(List<EmergencyUnit> units) { Units = units; Filter = null; }
        public void SetFilter(UnitType? filter) { Filter = filter; GameManager.Instance?.View.RefreshFleet(); }
        private void Update()
        {
            if (GameManager.Instance?.View?.LobbyVisible == true) return;
            foreach (var unit in Units)
            {
                if (unit.status != UnitStatus.EnRoute && unit.status != UnitStatus.OnScene) continue;
                unit.remainingEtaSeconds = Mathf.Max(0, unit.remainingEtaSeconds - Time.deltaTime * travelTimeScale);
                if (unit.remainingEtaSeconds > 0) continue;
                if (unit.status == UnitStatus.EnRoute)
                {
                    unit.status = UnitStatus.OnScene; unit.remainingEtaSeconds = 30;
                    EmergencyAudioManager.Instance?.PlayRadioBeep();
                }
                else
                {
                    unit.status = UnitStatus.Available;
                    unit.assignedCaseId = "";
                }
                // Resource arrival never closes or replaces the player's active conversation.
                GameManager.Instance?.View.RefreshFleet();
            }
        }
    }
}
