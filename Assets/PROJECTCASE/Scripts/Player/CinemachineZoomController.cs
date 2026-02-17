using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

namespace RogueliteGame.Player
{
    // Oyun başladığında CinemachineFollowZoom'un Width değerini başlangıçtan bitişe yumuşak geçişle değiştirdim
    public class CinemachineZoomController : MonoBehaviour
    {
        [Header("Cinemachine Reference")]
        [SerializeField] private CinemachineFollowZoom followZoom;

        [Header("Zoom Transition")]
        [SerializeField] private float startWidth = 2f;
        [SerializeField] private float endWidth = 15f;
        [SerializeField] private float transitionDuration = 2f;

        [Header("Easing")]
        [Tooltip("Geçiş eğrisini buradan özelleştirebilirsiniz")]
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private void Start()
        {
            if (followZoom == null)
                followZoom = GetComponent<CinemachineFollowZoom>();

            if (followZoom == null)
            {
                Debug.LogWarning("CinemachineZoomController: CinemachineFollowZoom componenti bulunamadı.", this);
                return;
            }

            StartCoroutine(ZoomTransitionCoroutine());
        }

        private IEnumerator ZoomTransitionCoroutine()
        {
            followZoom.Width = startWidth;

            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float curveValue = easeCurve.Evaluate(t);
                followZoom.Width = Mathf.Lerp(startWidth, endWidth, curveValue);
                yield return null;
            }

            followZoom.Width = endWidth;
        }
    }
}
