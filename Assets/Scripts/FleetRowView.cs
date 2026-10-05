using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public sealed class FleetRowView : MonoBehaviour
    {
        public UnitDefinition definition;
        public Text title, typeLabel, state, baseLocation, eta, actionLabel;
        public Image stateBadge;
        public Button assign;
        private GameManager game;
        private EmergencyUnit unit;
        public void Bind(GameManager manager)
        {
            game=manager;assign.onClick.AddListener(Assign);Refresh();
        }
        private void Assign() { if(unit!=null)game.SelectUnit(unit); }
        public void Refresh()
        {
            if(game?.Fleet==null)return;
            unit=game.Fleet.Units.FirstOrDefault(u=>u.id==definition.initial.id);
            if(unit==null) { assign.interactable=false;return; }
            title.text=unit.displayName;baseLocation.text=unit.baseLocation;
            typeLabel.text=unit.type==UnitType.Ambulance?"AMBULANCIA":unit.type==UnitType.Fire?"BOMBEROS":"POLICÍA";
            state.text=unit.status==UnitStatus.Available?"DISPONIBLE EN BASE":unit.status==UnitStatus.EnRoute?"EN TRASLADO":"EN SERVICIO";
            state.color=unit.status==UnitStatus.Available?DispatchView.Green:unit.status==UnitStatus.EnRoute?DispatchView.Amber:DispatchView.Blue;
            stateBadge.color=new Color(state.color.r*.2f,state.color.g*.2f,state.color.b*.2f,.75f);
            eta.text=unit.status==UnitStatus.Available?"ETA BASE --":unit.status==UnitStatus.EnRoute?"ETA "+Mathf.CeilToInt(unit.remainingEtaSeconds/60)+" min":"EN ESCENA";
            bool located=game.Session!=null&&game.Session.Facts.ContainsKey("location");
            assign.interactable=game.Phase==CallPhase.Active&&!game.Busy&&!game.Voice.Recording&&unit.status==UnitStatus.Available&&game.Session.DispatchedUnit==null&&located;
            actionLabel.text=game.Session?.DispatchedUnit==unit?"ASIGNADA ✓":game.Session?.DispatchedUnit!=null?"BLOQUEADA":!located?"SIN UBICACIÓN >":"DESPACHAR >";
            actionLabel.color=game.Session?.DispatchedUnit==unit?DispatchView.Green:!located?DispatchView.Amber:DispatchView.CyanAccent;
        }
        private void OnDestroy() { if(assign!=null)assign.onClick.RemoveListener(Assign); }
    }
}
