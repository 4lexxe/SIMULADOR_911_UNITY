using System;
using System.Collections.Generic;
using UnityEngine;
namespace EmergencyDispatch
{
    public static class ScenarioDatabase
    {
        public static List<EmergencyCase> GetDefaultCases()
        {
            var asset = Resources.Load<TextAsset>("Cases/cases");
            if (asset == null) throw new InvalidOperationException("Falta Resources/Cases/cases.json.");
            var catalogue = JsonUtility.FromJson<CaseCatalogue>(asset.text);
            if (catalogue?.cases == null || catalogue.cases.Length == 0) throw new InvalidOperationException("El catálogo está vacío.");
            var caseIds = new HashSet<string>();
            foreach (var c in catalogue.cases)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.id) || !caseIds.Add(c.id) || c.facts == null ||
                    string.IsNullOrWhiteSpace(c.locality) || string.IsNullOrWhiteSpace(c.incomingSummary) ||
                    float.IsNaN(c.arrivalSeconds) || float.IsInfinity(c.arrivalSeconds) || c.arrivalSeconds < 0 ||
                    float.IsNaN(c.mapNormalizedCoord.x) || float.IsNaN(c.mapNormalizedCoord.y) ||
                    c.mapNormalizedCoord.x < 0 || c.mapNormalizedCoord.x > 1 || c.mapNormalizedCoord.y < 0 || c.mapNormalizedCoord.y > 1)
                    throw new InvalidOperationException("Llamada o coordenadas inválidas en el catálogo.");
                int total = 0;
                var ids = new HashSet<string>();
                foreach (var f in c.facts)
                {
                    if (!ids.Add(f.id) || string.IsNullOrWhiteSpace(f.value) || f.points < 0 || (f.uncertain && string.IsNullOrWhiteSpace(f.verifiedValue)))
                        throw new InvalidOperationException("Dato inválido en " + c.id);
                    total += f.points;
                }
                if (total != 40 || ids.Count != 5) throw new InvalidOperationException("Cada caso requiere cinco datos que sumen 40 puntos.");
            }
            return new List<EmergencyCase>(catalogue.cases);
        }
        public static List<EmergencyUnit> GetDefaultUnits() => new List<EmergencyUnit>
        {
            Unit("AMB-04", UnitType.Ambulance, "Base Norte", 3), Unit("AMB-05", UnitType.Ambulance, "Base Centro", 5),
            Unit("BOM-02", UnitType.Fire, "Estación 1", 4), Unit("PAT-03", UnitType.Police, "Sector Este", 8), Unit("PAT-07", UnitType.Police, "Base Sur", 6)
        };
        private static EmergencyUnit Unit(string id, UnitType type, string location, int eta) =>
            new EmergencyUnit { id = id, displayName = id, type = type, baseLocation = location, baseEtaMinutes = eta };
    }
}
