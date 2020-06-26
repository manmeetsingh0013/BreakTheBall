using System.Collections;
using UnityEngine;

namespace PaintTheRings
{
    public class PaintedBallController : MonoBehaviour
    {

        private RaycastHit hit;
        private void Update()
        {
            if (UIManager.Instance.isGameOver)
                return;

            if (GameController.Instance.GameState == GameState.GameOver)
            {
                GameController.Instance.PlayPaintedBallExplode(transform.position);
                Destroy(gameObject);
            }
        }

        public void Shoot(float speed)
        {
            Ray rayForward = new Ray(transform.position, Vector3.forward);
            if (Physics.Raycast(rayForward, out hit, 100f))
            {
                StartCoroutine(MovingToPos(hit.point, speed));
            }
        }
        private IEnumerator MovingToPos(Vector3 hitPos, float speed)
        {
            Vector3 startPos = transform.position;
            float t = 0;
            float movingTime = Vector3.Distance(transform.position, hitPos) / speed;

            while (t < movingTime)
            {
                t += Time.deltaTime;
                float factor = t / movingTime;
                transform.position = Vector3.Lerp(startPos, hitPos, factor);
                yield return null;
            }

            SoundManager.Instance.PlaySound(SoundManager.Instance.paintRingPiece);

            Ray rayForward = new Ray(transform.position + Vector3.back * (transform.localScale.z / 2f), Vector3.forward);
            if (Physics.Raycast(rayForward, out hit, 1f))
            {
                if (hit.collider.CompareTag("Finish"))
                {
                    GameController.Instance.HandleHitPaintedRingPiece();
                }
                else
                {
                    if (!GameController.Instance.IsOutOfPaintedBall)
                    {
                        hit.collider.GetComponent<RingPieceController>().ChangeColor();
                    }
                    else
                    {
                        RingController ringControl = hit.transform.parent.GetComponent<RingController>();

                        //Painted all pieces
                        ringControl.PaintedAllPieces();

                        //Create fading ring effect
                        GameController.Instance.CreateFadingRing(ringControl.transform.position);
                    }
                    GameController.Instance.HandleHitNormalRingPiece();
                }

                FindObjectOfType<RotatorController>().ShakeRotator();
                Vector3 particlePos = transform.position + Vector3.back * 0.05f;
                GameController.Instance.PlayPaintedBallExplode(particlePos);
                Destroy(gameObject);
            }
        }





        /// <summary>
        /// Move this painted ball forward with given distance
        /// </summary>
        public void MoveForward(float forwardDistance, float time)
        {
            StartCoroutine(MovingForward(forwardDistance, time));
        }
        private IEnumerator MovingForward(float forwardDistance, float time)
        {
            float t = 0;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.forward * forwardDistance;
            while (t < time)
            {
                t += Time.deltaTime;
                float factor = t / time;
                transform.position = Vector3.Lerp(startPos, endPos, factor);
                yield return null;
            }
        }
    }
}