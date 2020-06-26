using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaintTheRings
{
    public class RingController : MonoBehaviour
    {


        [SerializeField]
        private RingPieceController[] ringPieceControls = null;

        public GameObject CrossMarkPrefab = null;

        /// <summary>
        /// Move this ring from current position to rotator object position with given time
        /// </summary>
        public void MoveToRotatorPosition(float movingTime, int paintedPieceNumber)
        {
            StartCoroutine(EnablePaintedPiece(paintedPieceNumber));
            StartCoroutine(Moving(movingTime));
        }
        private IEnumerator Moving(float time)
        {
            RotatorController rotatorControl = FindObjectOfType<RotatorController>();

            float t = 0;
            Vector3 startPos = transform.position;
            Vector3 endPos = rotatorControl.transform.position + Vector3.up * GameController.Instance.RingPieceHeight * rotatorControl.transform.childCount;
            while (t < time)
            {
                t += Time.deltaTime;
                float factor = t / time;
                transform.position = Vector3.Lerp(startPos, endPos, factor);
                yield return null;
            }

            //Create fading ring
            Vector3 fadingCirclePos = transform.position + Vector3.down * (GameController.Instance.RingPieceHeight / 2f);
            GameController.Instance.CreateFadingCircle(fadingCirclePos, transform);

            //Set parent for this ring and reset local position, local euler angles
            transform.SetParent(rotatorControl.transform);
            Vector3 localPos = transform.localPosition;
            localPos.x = 0;
            localPos.z = 0;
            transform.localPosition = localPos;
            transform.localEulerAngles = Vector3.zero;

            //Move and bounce the rotator
            rotatorControl.MoveDownAndBounce(time / 2f);
        }

        private IEnumerator EnablePaintedPiece(int paintedPieceNumber)
        {
            List<RingPieceController> listRingPieceControl = new List<RingPieceController>();
            foreach (RingPieceController o in ringPieceControls)
            {
                listRingPieceControl.Add(o);
            }

            //Enable painted pieces
            while (paintedPieceNumber > 0)
            {
                int index = Random.Range(0, listRingPieceControl.Count);

                listRingPieceControl[index].ChangeColor();
                listRingPieceControl.Remove(listRingPieceControl[index]);
                yield return null;
                paintedPieceNumber--;
            }
        }

        /// <summary>
        /// Painted all pieces
        /// </summary>
        public void PaintedAllPieces()
        {
            foreach (RingPieceController o in ringPieceControls)
            {
                o.ChangeColor();
                o.RemoveCrossPrefab();
            }
        }
    }
}