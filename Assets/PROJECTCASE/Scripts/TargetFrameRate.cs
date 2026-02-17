using UnityEngine;

namespace RogueliteGame
{
    public class TargetFrameRate : MonoBehaviour
    {
        [SerializeField] private int targetFPS = 120;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }
    }
}
