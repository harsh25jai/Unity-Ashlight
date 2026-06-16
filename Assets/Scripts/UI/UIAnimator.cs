using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Reusable UI Toolkit animation helpers for fade, scale, and slide transitions.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIAnimator : MonoBehaviour
    {
        private const float MinDuration = 0.01f;

        /// <summary>
        /// Evaluates normalized fade progress for unit tests and runtime tweens.
        /// </summary>
        /// <param name="elapsed">Elapsed time in seconds.</param>
        /// <param name="duration">Total duration in seconds.</param>
        /// <returns>Clamped progress from 0 to 1.</returns>
        public static float EvaluateProgress(float elapsed, float duration)
        {
            if (duration <= MinDuration)
            {
                return 1f;
            }

            return Mathf.Clamp01(elapsed / duration);
        }

        /// <summary>
        /// Fades a visual element's opacity between two values.
        /// </summary>
        /// <param name="host">Coroutine host.</param>
        /// <param name="element">Target element.</param>
        /// <param name="from">Starting opacity.</param>
        /// <param name="to">Target opacity.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="onComplete">Optional completion callback.</param>
        public static void Fade(
            MonoBehaviour host,
            VisualElement element,
            float from,
            float to,
            float duration,
            Action onComplete = null)
        {
            if (host == null || element == null)
            {
                onComplete?.Invoke();
                return;
            }

            host.StartCoroutine(FadeRoutine(element, from, to, duration, onComplete));
        }

        /// <summary>
        /// Scales a visual element between two uniform scale values.
        /// </summary>
        /// <param name="host">Coroutine host.</param>
        /// <param name="element">Target element.</param>
        /// <param name="from">Starting scale.</param>
        /// <param name="to">Target scale.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="onComplete">Optional completion callback.</param>
        public static void Scale(
            MonoBehaviour host,
            VisualElement element,
            float from,
            float to,
            float duration,
            Action onComplete = null)
        {
            if (host == null || element == null)
            {
                onComplete?.Invoke();
                return;
            }

            host.StartCoroutine(ScaleRoutine(element, from, to, duration, onComplete));
        }

        /// <summary>
        /// Slides a visual element vertically using translate.
        /// </summary>
        /// <param name="host">Coroutine host.</param>
        /// <param name="element">Target element.</param>
        /// <param name="fromY">Starting Y offset in pixels.</param>
        /// <param name="toY">Target Y offset in pixels.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="onComplete">Optional completion callback.</param>
        public static void SlideY(
            MonoBehaviour host,
            VisualElement element,
            float fromY,
            float toY,
            float duration,
            Action onComplete = null)
        {
            if (host == null || element == null)
            {
                onComplete?.Invoke();
                return;
            }

            host.StartCoroutine(SlideYRoutine(element, fromY, toY, duration, onComplete));
        }

        /// <summary>
        /// Loops a subtle opacity pulse for atmospheric UI accents.
        /// </summary>
        /// <param name="host">Coroutine host.</param>
        /// <param name="element">Target element.</param>
        /// <param name="minOpacity">Minimum opacity.</param>
        /// <param name="maxOpacity">Maximum opacity.</param>
        /// <param name="cycleDuration">Seconds per pulse cycle.</param>
        /// <returns>Started coroutine handle.</returns>
        public static Coroutine PulseOpacity(
            MonoBehaviour host,
            VisualElement element,
            float minOpacity,
            float maxOpacity,
            float cycleDuration)
        {
            if (host == null || element == null)
            {
                return null;
            }

            return host.StartCoroutine(PulseOpacityRoutine(element, minOpacity, maxOpacity, cycleDuration));
        }

        private static IEnumerator FadeRoutine(
            VisualElement element,
            float from,
            float to,
            float duration,
            Action onComplete)
        {
            float elapsed = 0f;
            element.style.opacity = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EvaluateProgress(elapsed, duration);
                element.style.opacity = Mathf.Lerp(from, to, t);
                yield return null;
            }

            element.style.opacity = to;
            onComplete?.Invoke();
        }

        private static IEnumerator ScaleRoutine(
            VisualElement element,
            float from,
            float to,
            float duration,
            Action onComplete)
        {
            float elapsed = 0f;
            element.style.scale = new Scale(new Vector3(from, from, 1f));

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EvaluateProgress(elapsed, duration);
                float scale = Mathf.Lerp(from, to, t);
                element.style.scale = new Scale(new Vector3(scale, scale, 1f));
                yield return null;
            }

            element.style.scale = new Scale(new Vector3(to, to, 1f));
            onComplete?.Invoke();
        }

        private static IEnumerator SlideYRoutine(
            VisualElement element,
            float fromY,
            float toY,
            float duration,
            Action onComplete)
        {
            float elapsed = 0f;
            element.style.translate = new Translate(0f, fromY);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EvaluateProgress(elapsed, duration);
                float y = Mathf.Lerp(fromY, toY, t);
                element.style.translate = new Translate(0f, y);
                yield return null;
            }

            element.style.translate = new Translate(0f, toY);
            onComplete?.Invoke();
        }

        private static IEnumerator PulseOpacityRoutine(
            VisualElement element,
            float minOpacity,
            float maxOpacity,
            float cycleDuration)
        {
            float halfCycle = Mathf.Max(MinDuration, cycleDuration * 0.5f);

            while (element != null)
            {
                yield return FadeRoutine(element, minOpacity, maxOpacity, halfCycle, null);
                yield return FadeRoutine(element, maxOpacity, minOpacity, halfCycle, null);
            }
        }
    }
}
