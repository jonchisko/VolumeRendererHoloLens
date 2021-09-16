using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;


namespace com.jon_skoberne.Volume
{
    public class ClipPlaneTool : MonoBehaviour
    {

        [SerializeField]
        private ClipingPlane clipPlane;
        [SerializeField]
        private Transform volumeCube;

        public void ToggleClipPlaneMeshRenderer()
        {
            bool active = clipPlane.IsVisible() ? false : true;
            clipPlane.ToggleMeshRenderer(active);
            // if visible, no constraints, if not visible, constrain movement!
            clipPlane.GetComponent<MoveAxisConstraint>().enabled = !active;
            clipPlane.GetComponent<RotationAxisConstraint>().enabled = !active;
        }

        public void ToggleClipPlane()
        {
            // enable clip plane
            bool active = clipPlane.gameObject.activeSelf ? false : true;
            clipPlane.gameObject.SetActive(active);

        }

        public void ResetClipPlanePosition()
        {
            clipPlane.SetTransform(volumeCube.transform);
        }
    }
}

