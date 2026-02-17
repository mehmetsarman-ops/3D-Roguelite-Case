using UnityEngine;

namespace RogueliteGame.UI
{
    // Cinemachine'den sonra çalışması için yüksek execution order verdim
    [DefaultExecutionOrder(200)]
    public class BillboardToCamera : MonoBehaviour
    {
        private Transform cachedCamera;

        private void LateUpdate()
        {
            if (cachedCamera == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                cachedCamera = cam.transform;
            }

            transform.forward = cachedCamera.forward;
        }
    }
}
