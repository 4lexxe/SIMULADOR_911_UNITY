using UnityEngine;
using UnityEngine.UI;
namespace EmergencyDispatch
{
    public class IncidentMapManager : MonoBehaviour
    {
        public RectTransform Content, Pin, Rings;
        public Text Caption, ScaleLabel;
        public IncomingCallMarker[] incomingMarkers;
        private GameManager game;
        public void Bind(GameManager manager) { game=manager;foreach(var marker in incomingMarkers)marker.Bind(manager); }
        private float zoom = 1;
        private Vector2 coordinate = new Vector2(.5f, .5f);
        private bool located;
        public void SetCase(CallSession session)
        {
            foreach(var marker in incomingMarkers)marker.Refresh();
            located = session != null && session.Facts.ContainsKey("location");
            if (located) coordinate = session.Definition.mapNormalizedCoord;
            Pin.gameObject.SetActive(located); Rings.gameObject.SetActive(located);
            Caption.text = located
                ? "DIRECCIÓN CONFIRMADA: " + session.Facts["location"].value + "\nSeñal aproximada de la localidad."
                : "JUJUY · " + (game?.WaitingCallCount ?? 0) + " LLAMADAS EN ESPERA\nElegí un punto rojo o abrí el mapa provincial.";
            if (located)
            {
                Vector2 pos = new Vector2((coordinate.x - .5f) * Content.rect.width, (coordinate.y - .5f) * Content.rect.height);
                Pin.anchoredPosition = pos; Rings.anchoredPosition = pos;
            }
        }
        public void Zoom(float amount)
        {
            zoom = Mathf.Clamp(zoom + amount, 1, 2.5f);
            Content.localScale = Vector3.one * zoom;
            Center();
            ScaleLabel.text = "JUJUY · " + zoom.ToString("0.00") + "×";
        }
        public void Center()
        {
            Vector2 limit=Vector2.Max(Vector2.zero,(Content.rect.size*zoom-((RectTransform)Content.parent).rect.size)/2);
            Vector2 position=located ? -Pin.anchoredPosition * (zoom - 1) : Vector2.zero;
            Content.anchoredPosition=new Vector2(Mathf.Clamp(position.x,-limit.x,limit.x),Mathf.Clamp(position.y,-limit.y,limit.y));
        }
        private void Update()
        {
            if (Rings != null && located) Rings.localScale = Vector3.one * (1 + Mathf.Sin(Time.time * 2) * .12f);
        }
    }
}
