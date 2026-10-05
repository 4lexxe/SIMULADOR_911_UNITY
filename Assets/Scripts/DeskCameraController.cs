using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EmergencyDispatch
{
    public class DeskCameraController : MonoBehaviour
    {
        public static DeskCameraController Instance { get; private set; }
        public enum CameraViewMode { DeskOverview, LeftMonitor, CenterMonitor, RightMonitor }
        public CameraViewMode Mode { get; private set; }
        [Header("Editable camera poses")]
        public Transform[] viewpoints;
        public float[] fieldOfViews = {54,57,57,57};
        private Camera cameraComponent;
        private Vector3 destination;
        private Quaternion rotation;
        private float fov;
        private void Awake()
        {
            Instance = this; cameraComponent = GetComponent<Camera>();
            SetView(CameraViewMode.DeskOverview); Snap();
        }
        public void SetView(CameraViewMode mode)
        {
            Mode = mode;
            int index=(int)mode;
            if(viewpoints==null||index>=viewpoints.Length||viewpoints[index]==null) return;
            destination=viewpoints[index].position; rotation=viewpoints[index].rotation;
            fov=fieldOfViews[index];
        }
        public void Snap() { transform.SetPositionAndRotation(destination, rotation); if (cameraComponent != null) cameraComponent.fieldOfView = fov; }
        private void Update()
        {
            bool typing = DispatchView.SafeIsTyping() || (DispatchView.Instance != null && (DispatchView.Instance.MapBoardVisible || DispatchView.Instance.ResultVisible || DispatchView.Instance.LobbyVisible));
            var keys = Keyboard.current;
            if (!typing && keys != null)
            {
                if (keys.digit0Key.wasPressedThisFrame || keys.f4Key.wasPressedThisFrame) SetView(CameraViewMode.DeskOverview);
                if (keys.digit1Key.wasPressedThisFrame || keys.f1Key.wasPressedThisFrame) SetView(CameraViewMode.LeftMonitor);
                if (keys.digit2Key.wasPressedThisFrame || keys.f2Key.wasPressedThisFrame) SetView(CameraViewMode.CenterMonitor);
                if (keys.digit3Key.wasPressedThisFrame || keys.f3Key.wasPressedThisFrame) SetView(CameraViewMode.RightMonitor);
            }
            Quaternion look = rotation;
            var mouse = Mouse.current;
            if (!typing && mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 p = mouse.position.ReadValue();
                look *= Quaternion.Euler(Mathf.Clamp((.5f - p.y / Screen.height) * 10, -5, 5), Mathf.Clamp((p.x / Screen.width - .5f) * 18, -9, 9), 0);
            }
            float blend = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 9);
            transform.position = Vector3.Lerp(transform.position, destination, blend);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, blend);
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, fov, blend);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
