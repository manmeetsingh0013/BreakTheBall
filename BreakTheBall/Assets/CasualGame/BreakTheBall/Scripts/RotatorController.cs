using System.Collections;
using UnityEngine;

namespace PaintTheRings
{
    public class RotatorController : MonoBehaviour
    {

        private bool stopRotate = false;
        private bool isRotated = false;

        private IEnumerator Rotating()
        {
            while (true)
            {
                float angle = Random.Range(GameController.Instance.CurrentLevelData.MinRotatingDegrees, GameController.Instance.CurrentLevelData.MaxRotatingDegrees);
                float rotatingTime = angle / Random.Range(GameController.Instance.CurrentLevelData.MinRotatingSpeed, GameController.Instance.CurrentLevelData.MaxRotatingSpeed);
                float t = 0;
                LerpType lerpType = GameController.Instance.CurrentLevelData.RotatingTypes[Random.Range(0, GameController.Instance.CurrentLevelData.RotatingTypes.Length)];

                Vector3 startAngles = transform.eulerAngles;
                Vector3 endAngles = startAngles + (Random.value <= 0.5f ? (Vector3.up * angle) : (Vector3.down * angle));
                while (t < rotatingTime)
                {
                    while (stopRotate || GameController.Instance.GameState != GameState.Playing)
                    {
                        yield return null;
                    }

                    t += Time.deltaTime;
                    float factor = EasyType.MatchedLerpType(lerpType, t / rotatingTime);
                    transform.eulerAngles = Vector3.Lerp(startAngles, endAngles, factor);
                    yield return null;
                }
            }
        }


        /// <summary>
        /// Move this rotator down and bouncing
        /// </summary>
        /// <param name="bounceTime"></param>
        public void MoveDownAndBounce(float bounceTime)
        {
            if (transform.childCount > 1)
            {
                StartCoroutine(Bouncing(bounceTime));
            }
            else
            {
                if (!isRotated)
                {
                    isRotated = true;
                    StartCoroutine(Rotating());
                }
            }
        }
        private IEnumerator Bouncing(float bounceTime)
        {
            stopRotate = true;
            float t = 0;
            float bouncingDownTime = bounceTime / 2f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * (GameController.Instance.RingPieceHeight + 0.5f);
            while (t < bouncingDownTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuad, t / bouncingDownTime);
                transform.position = Vector3.Lerp(startPos, endPos, factor);
                yield return null;
            }

            t = 0;
            startPos = transform.position;
            endPos = startPos + Vector3.up * 0.5f;
            while (t < bouncingDownTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuad, t / bouncingDownTime);
                transform.position = Vector3.Lerp(startPos, endPos, factor);
                yield return null;
            }

            stopRotate = false;
        }



        /// <summary>
        /// Shaking this rotator
        /// </summary>
        public void ShakeRotator()
        {
            StartCoroutine(Shaking());
        }
        private IEnumerator Shaking()
        {
            float t = 0;
            float movingTime = 0.05f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.forward * 0.2f;
            while (t < movingTime)
            {
                t += Time.deltaTime;
                float factor = t / movingTime;
                transform.position = Vector3.Lerp(startPos, endPos, factor);
                yield return null;
            }

            t = 0;
            while (t < movingTime)
            {
                t += Time.deltaTime;
                float factor = t / movingTime;
                transform.position = Vector3.Lerp(endPos, startPos, factor);
                yield return null;
            }
        }
    }
}