using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chkobba
{
    public class PlayCardsAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform[] cards;
        [SerializeField] private float seconds = 1.2f;

        public IEnumerator CollectAndDeal(string message, Toast toast)
        {
            if (cards == null || cards.Length == 0 || seconds <= 0f) yield break;
            var basePos = new Vector2[cards.Length];
            for (int i = 0; i < cards.Length; i++) basePos[i] = cards[i].anchoredPosition;

            if (toast != null) toast.Show(message);
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                float pull = Mathf.Sin(k * Mathf.PI);
                for (int i = 0; i < cards.Length; i++)
                    cards[i].anchoredPosition = basePos[i] + new Vector2(0f, pull * 26f * (1f + (i % 3) * 0.18f));
                yield return null;
            }
            for (int i = 0; i < cards.Length; i++) cards[i].anchoredPosition = basePos[i];
        }
    }
}
