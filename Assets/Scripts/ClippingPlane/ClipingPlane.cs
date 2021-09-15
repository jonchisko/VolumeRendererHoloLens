using UnityEngine;



namespace com.jon_skoberne.Volume
{

    [ExecuteAlways]
    public class ClipingPlane : MonoBehaviour
    {

        [SerializeField]
        private Material _mat;
        [SerializeField]
        private MeshRenderer _meshRenderer;

        private Plane _plane = new Plane();

        // Update is called once per frame
        void Update()
        {
            UpdatePlanePosition();
            SetPlaneInMaterial(GetPlaneRepresentation());
        }

        private void OnEnable()
        {
            _mat.EnableKeyword("CLIP");
        }

        private void OnDisable()
        {
            _mat.DisableKeyword("CLIP");
        }

        private void UpdatePlanePosition()
        {
            _plane.SetNormalAndPosition(this.transform.up, this.transform.position);
        }

        private Vector4 GetPlaneRepresentation()
        {
            return new Vector4(_plane.normal.x, _plane.normal.y, _plane.normal.z, _plane.distance);
        }

        private void SetPlaneInMaterial(Vector4 planeRepresentation)
        {
            _mat.SetVector("_Plane", planeRepresentation);
        }

        public void ToggleMeshRenderer(bool enable)
        {
            _meshRenderer.enabled = enable;
        }

        public bool IsVisible()
        {
            return GetComponent<MeshRenderer>().enabled;
        }
    }
}

