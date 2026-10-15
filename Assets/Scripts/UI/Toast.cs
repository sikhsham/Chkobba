using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chkobba
{
    public class Toast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMPro.TMP_Text label;
        [SerializeField] private float seconds = 2.4f;

        private Coroutine running;

        private void Awake() { Hide(); }

        public void Show(string message)
        {
            if (label != null) label.text = message;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FadeRoutine(1f, seconds));
        }

        public void Hide()
        {
            if (running != null) StopCoroutine(running);
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
            running = null;
        }

        private IEnumerator FadeRoutine(float target, float hold)
        {
            float t = 0f;
            float from = group.alpha;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, target, t / 0.2f);
                yield return null;
            }
            group.alpha = target;
            if (target > 0.5f)
            {
                yield return new WaitForSecondsRealtime(hold);
                yield return FadeRoutine(0f, 0f);
            }
            else running = null;
        }
    }
}
