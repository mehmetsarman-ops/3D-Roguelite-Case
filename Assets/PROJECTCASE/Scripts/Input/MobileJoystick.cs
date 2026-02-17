using UnityEngine;
using UnityEngine.EventSystems;

namespace RogueliteGame.Input
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform handle;

        [Header("Behaviour")]
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;
        [SerializeField] private bool snapBackInstant = true;
        [SerializeField] private float snapBackSpeed = 15f;

        private RectTransform backgroundRect;
        private Canvas parentCanvas;
        private UnityEngine.Camera canvasCamera;
        private float joystickRadius;

        private Vector2 inputVector;
        private bool isPressed;

        public Vector2 InputDirection => inputVector;
        public float InputMagnitude => inputVector.magnitude;
        public bool IsPressed => isPressed;

        private void Awake()
        {
            backgroundRect = GetComponent<RectTransform>();

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Canvas rootCanvas = parentCanvas.rootCanvas;
                canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera;
            }

            joystickRadius = backgroundRect.sizeDelta.x * 0.5f;

            if (handle != null)
                handle.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (!isPressed && !snapBackInstant && handle != null)
            {
                handle.anchoredPosition = Vector2.Lerp(
                    handle.anchoredPosition, Vector2.zero, Time.deltaTime * snapBackSpeed);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    backgroundRect, eventData.position, canvasCamera, out Vector2 localPoint))
                return;

            Vector2 normalized = new Vector2(
                localPoint.x / (backgroundRect.sizeDelta.x * 0.5f),
                localPoint.y / (backgroundRect.sizeDelta.y * 0.5f));

            if (normalized.magnitude > 1f)
                normalized = normalized.normalized;

            // Dead zone altındaki girişleri sıfırladım, küçük dokunuşlarda karakter kaymasın diye
            float magnitude = normalized.magnitude;
            if (magnitude < deadZone)
            {
                inputVector = Vector2.zero;
            }
            else
            {
                float remapped = (magnitude - deadZone) / (1f - deadZone);
                inputVector = normalized.normalized * remapped;
            }

            if (handle != null)
            {
                handle.anchoredPosition = normalized * joystickRadius;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            inputVector = Vector2.zero;

            if (handle != null && snapBackInstant)
                handle.anchoredPosition = Vector2.zero;
        }
    }
}
