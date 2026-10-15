using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chkobba
{
    public class SheetPanel : MonoBehaviour
    {
        [SerializeField] private MainMenuController menu;
        [SerializeField] private RectTransform panel;
        [SerializeField] private float slideSeconds = 0.3f;

        private Coroutine running;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (panel != null) panel.anchoredPosition = new Vector2(0f, -panel.rect.height - 40f);
        }

        public void Open(MainMenuController opener)
        {
            if (IsOpen) return;
            IsOpen = true;
            menu = opener;
            gameObject.SetActive(true);
            if (menu != null) menu.NotifySheetOpened(this);
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(SlideRoutine(true));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(SlideRoutine(false));
        }

        private IEnumerator SlideRoutine(bool opening)
        {
            if (panel == null) yield break;
            var closed = new Vector2(0f, -panel.rect.height - 40f);
            var opened = Vector2.zero;
            if (opening) panel.anchoredPosition = closed;
            var from = panel.anchoredPosition;
            var to = opening ? opened : closed;
            float t = 0f;
            while (t < slideSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / slideSeconds));
                panel.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }
            panel.anchoredPosition = to;
            running = null;
            if (!opening)
            {
                gameObject.SetActive(false);
                if (menu != null) menu.NotifySheetClosed(this);
            }
        }
    }
}
