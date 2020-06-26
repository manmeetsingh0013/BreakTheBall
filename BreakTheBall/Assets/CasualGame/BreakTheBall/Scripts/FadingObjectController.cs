using System.Collections;
using UnityEngine;

namespace PaintTheRings
{
    public class FadingObjectController : MonoBehaviour
    {

        private Renderer render = null;
        private Color originalColor = Color.white;


        /// <summary>
        /// Use for fading circle object
        /// </summary>
        /// <param name="newScale"></param>
        /// <param name="fadingTime"></param>
        public void CircleFading(Vector3 newScale, float fadingTime)
        {
            if (render == null)
                render = GetComponent<Renderer>();
            if (originalColor == Color.white)
                originalColor = render.material.color;
            StartCoroutine(FadingThisCircle(newScale, fadingTime));
        }
        IEnumerator FadingThisCircle(Vector3 newScale, float fadingTime)
        {
            Vector3 startScale = transform.localScale;
            Color startColor = render.material.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            float t = 0;
            while (t < fadingTime)
            {
                t += Time.deltaTime;
                float factor = t / fadingTime;
                transform.localScale = Vector3.Lerp(startScale, newScale, factor);
                render.material.color = Color.Lerp(startColor, endColor, factor);
                yield return null;
            }

            transform.localScale = Vector3.one;
            render.material.color = originalColor;
            transform.SetParent(null);
            gameObject.SetActive(false);
        }


        /// <summary>
        /// Use for fading ring object
        /// </summary>
        /// <param name="newScale"></param>
        /// <param name="newColor"></param>
        /// <param name="fadingTime"></param>
        public void RingFading(Vector3 newScale, float fadingTime)
        {
            if (render == null)
                render = GetComponent<Renderer>();
            render.material.color = GameController.Instance.CurrentRingMaterial.color;
            StartCoroutine(FadingThisRing(newScale, fadingTime));
        }
        IEnumerator FadingThisRing(Vector3 newScale, float fadingTime)
        {
            Vector3 startScale = transform.localScale;
            Color startColor = render.material.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            float t = 0;
            while (t < fadingTime)
            {
                t += Time.deltaTime;
                float factor = t / fadingTime;
                transform.localScale = Vector3.Lerp(startScale, newScale, factor);
                render.material.color = Color.Lerp(startColor, endColor, factor);
                yield return null;
            }

            transform.localScale = Vector3.one;
            render.material.color = originalColor;
            transform.SetParent(null);
            gameObject.SetActive(false);
        }
    }
}