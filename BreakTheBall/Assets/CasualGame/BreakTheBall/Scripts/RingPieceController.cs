using UnityEngine;

namespace PaintTheRings
{
    public class RingPieceController : MonoBehaviour
    {

        private MeshRenderer render = null;


        /// <summary>
        /// Change color form current color to given color 
        /// </summary>
        /// <param name="newColor"></param>
        /// <param name="setDeadTag"></param>
        public void ChangeColor()
        {
            if (render == null)
                render = GetComponent<MeshRenderer>();
            if (!gameObject.CompareTag("Finish"))
            {
                AttachCrossPrefab();
                render.material = GameController.Instance.CurrentRingMaterial;
                gameObject.tag = "Finish";
            }
        }


        private void AttachCrossPrefab()
        {
            GameObject crossMark = (GameObject)Instantiate(this.gameObject.GetComponentInParent<RingController>().CrossMarkPrefab);
            crossMark.transform.parent = this.gameObject.transform;
            crossMark.transform.localPosition = new Vector3(-4.918f, 0, -0.95f);
            crossMark.transform.localScale = new Vector3(0.468f, 0.468f, 0.468f);
            crossMark.transform.localEulerAngles = new Vector3(0, 80f, 0);
        }

        public void RemoveCrossPrefab()
        {
            if (this.gameObject.transform.childCount > 0)
            {
                Destroy(gameObject.transform.GetChild(0).gameObject);
            }
        }
    }
}