using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.jon_skoberne.UI
{
    public class SkullMenuRotation : MonoBehaviour
    {
        public float rotationSpeed = 1.0f;

        private float zRotation = 0.0f;
        private Vector3 oldRotation = Vector3.zero;

        // Start is called before the first frame update
        void Start()
        {
            oldRotation = this.transform.localRotation.eulerAngles;
        }

        // Update is called once per frame
        void Update()
        {
            RotateSelf();
        }

        private void RotateSelf()
        {
            zRotation += rotationSpeed * Time.deltaTime;
            zRotation = Mathf.Clamp(zRotation, -180.0f, 180.0f);
            this.transform.localRotation = Quaternion.Euler(oldRotation.x, oldRotation.y, zRotation);
        }
    }
}

