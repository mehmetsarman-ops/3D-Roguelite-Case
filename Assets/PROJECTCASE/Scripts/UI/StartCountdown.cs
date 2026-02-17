using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace RogueliteGame.UI
{
    public class StartCountdown : MonoBehaviour
    {
        // Sırasıyla 3, 2, 1 sayılarına karşılık gelen sprite'ları Inspector'dan atıyorum
        [Header("Sprites")]
        [SerializeField] private Sprite[] numberSprites = new Sprite[3];

        [Header("References")]
        [SerializeField] private Image displayImage;

        public event Action OnCountdownFinished;

        private WaitForSeconds oneSecond;

        private void Awake()
        {
            oneSecond = new WaitForSeconds(1f);

            if (displayImage == null)
                displayImage = GetComponentInChildren<Image>();
        }

        private void OnEnable()
        {
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            for (int i = 0; i < numberSprites.Length; i++)
            {
                displayImage.sprite = numberSprites[i];
                yield return oneSecond;
            }

            OnCountdownFinished?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
