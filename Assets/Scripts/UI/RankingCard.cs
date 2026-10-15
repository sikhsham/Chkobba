using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chkobba
{
    public class RankingCard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private MainMenuController menu;
        [SerializeField] private float shakeSeconds = 0.45f;

        private Coroutine running;

        public void Shake()
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            var rt = (RectTransform)transform;
            var basePos = rt.anchoredPosition;
            float t = 0f;
            while (t < shakeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(t / shakeSeconds * Mathf.PI * 6f) * (1f - t / shakeSeconds);
                rt.anchoredPosition = basePos + new Vector2(k * 6f, 0f);
                yield return null;
            }
            rt.anchoredPosition = basePos;
            running = null;
        }

        public void OnPointerClick(PointerEventData e) { if (menu != null) menu.OnRanking(); }
    }
}
