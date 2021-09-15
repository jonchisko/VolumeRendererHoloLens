using UnityEngine;


namespace com.jon_skoberne.Volume
{
    public class ClipPlaneTool : MonoBehaviour
    {

        [SerializeField]
        private ClipingPlane clipPlane;


        public void ToggleClipPlaneMeshRenderer()
        {
            clipPlane.ToggleMeshRenderer(clipPlane.IsVisible() ? false : true);
        }

        public void ToggleClipPlane()
        {
            // enable clip plane
            clipPlane.gameObject.SetActive(clipPlane.gameObject.activeSelf ? false : true);
        }
    }
}

