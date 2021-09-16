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
            clipPlane.ToggleMeshRenderer(clipPlane.IsVisible() ? false : true);
        }

        public void ToggleClipPlane()
        {
            // enable clip plane
            clipPlane.gameObject.SetActive(clipPlane.gameObject.activeSelf ? false : true);
        }

        public void ResetClipPlanePosition()
        {
            clipPlane.SetTransform(volumeCube.transform);
        }
    }
}

