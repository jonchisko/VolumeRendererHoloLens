using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace com.jon_skoberne.TransferFunctionDrawer 
{

    [System.Serializable]
    public class TransferFunctionSaveObject
    {
        private float x;
        private float y;
        private float rx;
        private float ry;
        private float w;
        private float r;
        private float g;
        private float b;
        private float a;
        private bool endPoint;

        public TransferFunctionSaveObject()
        {

        }

        public TransferFunctionSaveObject(float x, float y, float rx, float ry, float w, float r, float g, float b, float a, bool endPoint)
        {
            this.x = x;
            this.y = y;
            this.rx = rx;
            this.ry = ry;
            this.w = w;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
            this.endPoint = endPoint;
        }

        public void SetX(float x)
        {
            this.x = x;
        }

        public void SetY(float y)
        {
            this.y = y;
        }

        public void SetRx(float rx)
        {
            this.rx = rx;
        }

        public void SetRy(float ry)
        {
            this.ry = ry;
        }

        public void SetWeight(float w)
        {
            this.w = w;
        }

        public void SetColor(Color col)
        {
            r = col.r;
            g = col.g;
            b = col.b;
            a = col.a;
        }

        public void SetEndPoint(bool endPoint)
        {
            this.endPoint = endPoint;
        }

        public float GetX()
        {
            return x;
        }

        public float GetY()
        {
            return y;
        }

        public float GetRx()
        {
            return rx;
        }

        public float GetRy()
        {
            return ry;
        }

        public float GetWeight()
        {
            return w;
        }

        public bool IsEndPoint()
        {
            return endPoint;
        }



        public Color GetColor()
        {
            return new Color(r, g, b, a);
        }

    }



    public class TransferFunctionPoint : MonoBehaviour
    {
        public delegate void OnPointClickDelegate(TransferFunctionPoint p);
        public static OnPointClickDelegate pointClickDelegate;

        public SpriteRenderer img;
        public GameObject selectedImg;
        public Interactable button;

        private float positionX = 0.0f;
        private float positionY = 0.0f;
        private float ellipsoid_rx = 0.0f;
        private float ellipsoid_ry = 0.0f;
        private float ellipsoid_weight = 2.0f;
        private Color color = new Color(1, 1, 1, 1);

        
        private bool endPoint;

        private void Awake()
        {
        }


        #region Setters/Getters
        public void SetPositionX(float value)
        {
            if (!endPoint)
            {
                positionX = value;
            }
        }

        public void SetPositionY(float value)
        {
            positionY = value;
        }

        public void SetRx(float value)
        {
            ellipsoid_rx = value;
        }

        public void SetRy(float value)
        {
            ellipsoid_ry = value;
        }

        public void SetEllipsoidWeight(float value)
        {
            ellipsoid_weight = value % 2 == 0 ? value : value + 1; // keep em even
        }

        public void SetColor(Color value)
        {
            color = value;
            if(img != null) img.color = color;
        }

        public float GetPositionX()
        {
            return positionX;
        }

        public float GetPositionY()
        {
            return positionY;
        }

        public float GetRx()
        {
            return ellipsoid_rx;
        }

        public float GetRy()
        {
            return ellipsoid_ry;
        }

        public float GetEllipsoidWeight()
        {
            return ellipsoid_weight;
        }

        public Color GetColor()
        {
            return color;
        }
        #endregion

        public void Select()
        {
            selectedImg?.SetActive(true);
            SetInteractivity(false);
        }

        public void Deselect()
        {
            selectedImg?.SetActive(false);
            SetInteractivity(true);
        }

        public void OnPointClick()
        {
            Debug.Log("Point: " + this.GetInstanceID() + " clicked!");
            pointClickDelegate?.Invoke(this);
        }

        public void SetInteractivity(bool value)
        {
            button.IsEnabled = value;
            //button.enabled = value;
        }

        public void SetEndPoint(bool value)
        {
            endPoint = value;
        }

        public bool IsEndPoint()
        {
            return endPoint;
        }

        public TransferFunctionSaveObject GetSaveObject()
        {
            return new TransferFunctionSaveObject(positionX, positionY, ellipsoid_rx, ellipsoid_ry, ellipsoid_weight, color.r, color.g, color.b, color.a, endPoint);
        }

        public void SetValuesFromSavedOject(TransferFunctionSaveObject so)
        {
            this.positionX = so.GetX();
            this.positionY = so.GetY();
            this.ellipsoid_rx = so.GetRx();
            this.ellipsoid_ry = so.GetRy();
            this.ellipsoid_weight = so.GetWeight();
            this.color = so.GetColor();
            this.endPoint = so.IsEndPoint();
        }
    }
}

