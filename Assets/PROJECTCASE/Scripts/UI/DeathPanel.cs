using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace RogueliteGame.UI
{
    // Bu scripti panelin kendisine değil, her zaman aktif kalan bir GameObject'e (örn. Canvas) atıyorum
    public class DeathPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Player.PlayerMovement player;

        private Animator playerAnimator;
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Start()
        {
            if (player == null)
                player = FindObjectOfType<Player.PlayerMovement>();

            playerAnimator = player.GetComponent<Animator>();

            respawnButton.onClick.AddListener(OnRespawn);
            exitButton.onClick.AddListener(OnExit);

            panel.SetActive(false);

            player.OnDied += HandleDeath;
        }

        private void HandleDeath()
        {
            StartCoroutine(WaitForDeathAnimation());
        }

        // Ölüm animasyonunun tamamen bitmesini bekleyip paneli ondan sonra açıyorum
        private IEnumerator WaitForDeathAnimation()
        {
            if (playerAnimator != null)
            {
                yield return null;

                while (!IsDeathAnimationFinished())
                    yield return null;
            }

            panel.SetActive(true);
        }

        private bool IsDeathAnimationFinished()
        {
            var state = playerAnimator.GetCurrentAnimatorStateInfo(0);
            return state.normalizedTime >= 1f && !playerAnimator.IsInTransition(0);
        }

        private void OnRespawn()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (player != null) player.OnDied -= HandleDeath;
            if (respawnButton != null) respawnButton.onClick.RemoveListener(OnRespawn);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
        }
    }
}
