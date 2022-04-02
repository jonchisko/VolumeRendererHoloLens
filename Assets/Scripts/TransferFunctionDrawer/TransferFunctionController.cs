using com.jon_skoberne.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;


namespace com.jon_skoberne.TransferFunctionDrawer
{
    public enum TransferFunctionDims
    {
        None,
        Dim1,
        Dim2,
    }


    public enum TransferFunctionMode
    {
        TF1D,
        TF1Dplus,
        Ellipse,
        TFRectangle,
    }

    public enum TransferFunctionColorMode
    {
        None = -1,
        HSV = 0,
        Opacity = 1,
    }

    public class TransferFunctionController : MonoBehaviour
    {
        public delegate void OnRedrawTexture(Texture2D tex);
        public static OnRedrawTexture OnEventRedrawTexture;

        public static int textureDimensions = 2048;

        public GameObject tfPoint;
        public Material textureMaterialColors;
        public Material textureMaterialOpacities;
        public TransferFunctionMode tfMode;

        public GameObject tfColorsCanvas;
        public GameObject tfOpacitiesCanvas;

        public GameObject colorPointsController;
        public GameObject opacityPointsController;

        public float pointScale = 0.2f;
        public float pointZAxis = -0.02f;

        [Header("ComputeShaders")]
        public ComputeShader csTf1d;
        public ComputeShader csTf2d;
        public ComputeShader csEllipse;
        public ComputeShader csRect;
        public ComputeShader csClear;

        [Header("Point Color Sliders")]
        public SliderLogic sliderPosX;
        public SliderLogic sliderPosY;
        public SliderLogic sliderRx;
        public SliderLogic sliderRy;
        public SliderLogic sliderWeight;
        public SliderLogic sliderHue;
        public SliderLogic sliderSat;
        public SliderLogic sliderVal;

        [Header("Point Opacity Sliders")]
        public SliderLogic sliderPosXOp;
        public SliderLogic sliderPosYOp;
        public SliderLogic sliderRxOp;
        public SliderLogic sliderRyOp;
        public SliderLogic sliderWeightOp;
        public SliderLogic sliderAlpha;

        [Header("LoadPopups")]
        public GameObject loaderColors;
        public GameObject loaderOpacities;
        [Header("SavePopups")]
        public GameObject saverColors;
        public GameObject saverOpacities;
        //[Header("RemovePoint")]
        //public GameObject RemovePopup;

        private LinkedList<TransferFunctionPoint> tfPointsColors;
        private LinkedList<TransferFunctionPoint> tfPointsOpacities;

        public TransferFunctionPoint selectedPoint;
        public TransferFunctionColorMode selectedPointMode; // public for debugging purposes

        private Texture2D transferFunctionTex;
        private RenderTexture rt;

        #region Unity Callbacks

        private void Awake()
        {
            rt = new RenderTexture(TransferFunctionController.textureDimensions, TransferFunctionController.textureDimensions, 24, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.Create();

            this.transferFunctionTex = new Texture2D(TransferFunctionController.textureDimensions, TransferFunctionController.textureDimensions, TextureFormat.RGBAFloat, false);
            this.transferFunctionTex.wrapMode = TextureWrapMode.Clamp;

            this.tfPointsColors = new LinkedList<TransferFunctionPoint>();
            this.tfPointsOpacities = new LinkedList<TransferFunctionPoint>();

            this.textureMaterialColors.SetTexture("_MainTex", this.transferFunctionTex);
            this.textureMaterialOpacities.SetTexture("_MainTex", this.transferFunctionTex); // _TransferTexture

            if (tfPoint == null) Debug.LogError("TransferFunctionPoint is not set in TransferFunctionController!");

            if (tfMode == TransferFunctionMode.TF1D || tfMode == TransferFunctionMode.TF1Dplus)
            {
                if (tfPointsColors.Count == 0) AddBorderPoints(TransferFunctionColorMode.HSV);
                if (tfPointsOpacities.Count == 0) AddBorderPoints(TransferFunctionColorMode.Opacity);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            RegisterToEvents();
            OnEnableDrawer();
        }

        private void OnDestroy()
        {
            rt.Release();
            DeregisterFromEvenets();
        }

        private void OnEnable()
        {
            Debug.Log("TF Controller awake");
            OnEnableDrawer();
        }

        #endregion



        #region Helper Methods

        public Texture2D GetTransferTexture()
        {
            return this.transferFunctionTex;
        }

        private void OnEnableDrawer()
        {
            // this needs to be done, otherwise the last awake just sets its own transferFunctionTex and it stays so
            this.textureMaterialColors.SetTexture("_MainTex", this.transferFunctionTex);
            this.textureMaterialOpacities.SetTexture("_MainTex", this.transferFunctionTex); // _TransferTexture
            ComputeTexture();
        }

        private void ComputeTexture()
        {
            Vector2[] pointColorsPositions = new Vector2[tfPointsColors.Count];
            Vector4[] pointColorsColors = new Vector4[tfPointsColors.Count];

            Vector2[] pointOpacitiesPositions = new Vector2[tfPointsOpacities.Count];
            Vector4[] pointOpacitiesColors = new Vector4[tfPointsOpacities.Count];

            Vector2[] pointColorsRxRy = new Vector2[tfPointsColors.Count];
            float[] pointColorsWeights = new float[tfPointsColors.Count];

            Vector2[] pointOpacitiesRxRy = new Vector2[tfPointsOpacities.Count];
            float[] pointOpacitiesWeights = new float[tfPointsOpacities.Count];


            uint i = 0;
            foreach (TransferFunctionPoint p in tfPointsColors)
            {
                pointColorsPositions[i] = ConstructTextureVectorFromPoint(p);
                pointColorsColors[i] = p.GetColor();
                pointColorsRxRy[i] = ConstructTextureVectorFromPointEllipsoid(p);
                pointColorsWeights[i] = p.GetEllipsoidWeight();
                i++;
            }

            i = 0;
            foreach (TransferFunctionPoint p in tfPointsOpacities)
            {
                pointOpacitiesPositions[i] = ConstructTextureVectorFromPoint(p);
                pointOpacitiesColors[i] = p.GetColor();
                pointOpacitiesRxRy[i] = ConstructTextureVectorFromPointEllipsoid(p);
                pointOpacitiesWeights[i] = p.GetEllipsoidWeight();
                i++;
            }

            TransferFunctionDims tfDim = TransferFunctionDims.None;

            switch (tfMode)
            {
                case TransferFunctionMode.TF1D:
                    {
                        ComputeBuffer positionsColorsBuffer = new ComputeBuffer(pointColorsPositions.Length, 2 * sizeof(float));
                        ComputeBuffer colorsColorsBuffer = new ComputeBuffer(pointColorsColors.Length, 4 * sizeof(float));

                        ComputeBuffer positionsOpacitiesBuffer = new ComputeBuffer(pointOpacitiesPositions.Length, 2 * sizeof(float));
                        ComputeBuffer colorsOpacitiesBuffer = new ComputeBuffer(pointOpacitiesColors.Length, 4 * sizeof(float));


                        positionsColorsBuffer.SetData(pointColorsPositions);
                        colorsColorsBuffer.SetData(pointColorsColors);

                        positionsOpacitiesBuffer.SetData(pointOpacitiesPositions);
                        colorsOpacitiesBuffer.SetData(pointOpacitiesColors);

                        csTf1d.SetBuffer(0, "_PointColorsPositions", positionsColorsBuffer);
                        csTf1d.SetBuffer(0, "_PointColorsColors", colorsColorsBuffer);

                        csTf1d.SetInt("_Width", rt.width);
                        csTf1d.SetInt("_Height", rt.height);
                        csTf1d.SetBool("_OpMode", false);
                        csTf1d.SetInt("_NumColPoints", pointColorsPositions.Length);
                        csTf1d.SetTexture(0, "Result", rt);
                        csTf1d.Dispatch(0, rt.width / 16, rt.height / 16, 1);


                        csTf1d.SetBuffer(0, "_PointColorsPositions", positionsOpacitiesBuffer);
                        csTf1d.SetBuffer(0, "_PointColorsColors", colorsOpacitiesBuffer);

                        csTf1d.SetInt("_Width", rt.width);
                        csTf1d.SetInt("_Height", rt.height);
                        csTf1d.SetBool("_OpMode", true);
                        csTf1d.SetInt("_NumColPoints", pointOpacitiesPositions.Length);
                        csTf1d.SetTexture(0, "Result", rt);
                        csTf1d.Dispatch(0, rt.width / 16, rt.height / 16, 1);

                        Graphics.CopyTexture(rt, transferFunctionTex);

                        positionsColorsBuffer.Release();
                        positionsColorsBuffer = null;
                        colorsColorsBuffer.Release();
                        colorsColorsBuffer = null;

                        positionsOpacitiesBuffer.Release();
                        positionsOpacitiesBuffer = null;
                        colorsOpacitiesBuffer.Release();
                        colorsOpacitiesBuffer = null;

                        tfDim = TransferFunctionDims.Dim1;
                        break;
                    }
                case TransferFunctionMode.TF1Dplus:
                    {
                        ComputeBuffer positionsColorsBuffer = new ComputeBuffer(pointColorsPositions.Length, 2 * sizeof(float));
                        ComputeBuffer colorsColorsBuffer = new ComputeBuffer(pointColorsColors.Length, 4 * sizeof(float));

                        //ComputeBuffer positionsOpacitiesBuffer = new ComputeBuffer(pointOpacitiesPositions.Length, 2 * sizeof(float));
                        //ComputeBuffer colorsOpacitiesBuffer = new ComputeBuffer(pointOpacitiesColors.Length, 4 * sizeof(float));


                        positionsColorsBuffer.SetData(pointColorsPositions);
                        colorsColorsBuffer.SetData(pointColorsColors);

                        //positionsOpacitiesBuffer.SetData(pointOpacitiesPositions);
                        //colorsOpacitiesBuffer.SetData(pointOpacitiesColors);

                        csTf2d.SetBuffer(0, "_PointColorsPositions", positionsColorsBuffer);
                        csTf2d.SetBuffer(0, "_PointColorsColors", colorsColorsBuffer);

                        csTf2d.SetInt("_Width", rt.width);
                        csTf2d.SetInt("_Height", rt.height);
                        csTf2d.SetBool("_OpMode", false);
                        csTf2d.SetInt("_NumColPoints", pointColorsPositions.Length);
                        csTf2d.SetTexture(0, "Result", rt);
                        csTf2d.Dispatch(0, rt.width / 16, rt.height / 16, 1);


                        //csTf2d.SetBuffer(0, "_PointColorsPositions", positionsOpacitiesBuffer);
                        //csTf2d.SetBuffer(0, "_PointColorsColors", colorsOpacitiesBuffer);

                        //csTf2d.SetBool("_OpMode", true);
                        //csTf2d.SetInt("_NumColPoints", pointOpacitiesPositions.Length);
                        //csTf2d.SetTexture(0, "Result", rt);
                        //csTf2d.Dispatch(0, rt.width / 16, rt.height / 16, 1);

                        Graphics.CopyTexture(rt, transferFunctionTex);

                        positionsColorsBuffer.Release();
                        positionsColorsBuffer = null;
                        colorsColorsBuffer.Release();
                        colorsColorsBuffer = null;

                        //positionsOpacitiesBuffer.Release();
                        //positionsOpacitiesBuffer = null;
                        //colorsOpacitiesBuffer.Release();
                        //colorsOpacitiesBuffer = null;
                        tfDim = TransferFunctionDims.Dim1;
                        break;
                    }
                case TransferFunctionMode.Ellipse:
                    {
                        ComputeEllipseRect(csEllipse, pointColorsRxRy, pointColorsWeights, pointColorsPositions, pointColorsColors, pointOpacitiesRxRy, pointOpacitiesWeights,
                            pointOpacitiesPositions, pointOpacitiesColors);
                        tfDim = TransferFunctionDims.Dim2;
                        break;
                    }

                case TransferFunctionMode.TFRectangle:
                    {
                        ComputeEllipseRect(csRect, pointColorsRxRy, pointColorsWeights, pointColorsPositions, pointColorsColors, pointOpacitiesRxRy, pointOpacitiesWeights,
                            pointOpacitiesPositions, pointOpacitiesColors);
                        tfDim = TransferFunctionDims.Dim2;
                        break;
                    }

            }

            OnEventRedrawTexture?.Invoke(this.transferFunctionTex);


        }

        private void ComputeEllipseRect(ComputeShader cs, Vector2[] pointColorsRxRy, float[] pointColorsWeights, Vector2[] pointColorsPositions, Vector4[] pointColorsColors,
            Vector2[] pointOpacitiesRxRy, float[] pointOpacitiesWeights, Vector2[] pointOpacitiesPositions, Vector4[] pointOpacitiesColors)
        {
            if (tfPointsColors.Count > 0)
            {
                Debug.Log("COMPUTE ELIPSE/RECT");
                ComputeBuffer pointColorsRxRyBuffer = new ComputeBuffer(pointColorsRxRy.Length, 2 * sizeof(float));
                ComputeBuffer elipseColorsWeightsBuffer = new ComputeBuffer(pointColorsWeights.Length, sizeof(float));
                ComputeBuffer positionsColorsBuffer = new ComputeBuffer(pointColorsPositions.Length, 2 * sizeof(float));
                ComputeBuffer colorsColorsBuffer = new ComputeBuffer(pointColorsColors.Length, 4 * sizeof(float));

                pointColorsRxRyBuffer.SetData(pointColorsRxRy);
                elipseColorsWeightsBuffer.SetData(pointColorsWeights);
                positionsColorsBuffer.SetData(pointColorsPositions);
                colorsColorsBuffer.SetData(pointColorsColors);



                cs.SetBuffer(0, "_PointColorsRxRy", pointColorsRxRyBuffer);
                cs.SetBuffer(0, "_PointColorsElipseWeights", elipseColorsWeightsBuffer);
                cs.SetBuffer(0, "_PointColorsPositions", positionsColorsBuffer);
                cs.SetBuffer(0, "_PointColorsColors", colorsColorsBuffer);



                cs.SetInt("_Width", rt.width);
                cs.SetInt("_Height", rt.height);
                cs.SetBool("_OpMode", false);
                cs.SetInt("_NumColPoints", pointColorsPositions.Length);
                cs.SetTexture(0, "Result", rt);
                cs.Dispatch(0, rt.width / 16, rt.height / 16, 1);

                Graphics.CopyTexture(rt, transferFunctionTex);

                pointColorsRxRyBuffer.Release();
                pointColorsRxRyBuffer = null;
                elipseColorsWeightsBuffer.Release();
                elipseColorsWeightsBuffer = null;
                positionsColorsBuffer.Release();
                positionsColorsBuffer = null;
                colorsColorsBuffer.Release();
                colorsColorsBuffer = null;
            }
            else
            {
                csClear.SetTexture(0, "Result", rt);
                csClear.SetBool("_OpMode", false);
                csClear.Dispatch(0, rt.width / 16, rt.height / 16, 1);

                Graphics.CopyTexture(rt, transferFunctionTex);
            }

            if (tfPointsOpacities.Count > 0)
            {
                ComputeBuffer pointOpacitiesRxRyBuffer = new ComputeBuffer(pointOpacitiesRxRy.Length, 2 * sizeof(float));
                ComputeBuffer elipseOpacitiesWeightsBuffer = new ComputeBuffer(pointOpacitiesWeights.Length, sizeof(float));
                ComputeBuffer positionsOpacitiesBuffer = new ComputeBuffer(pointOpacitiesPositions.Length, 2 * sizeof(float));
                ComputeBuffer colorsOpacitiesBuffer = new ComputeBuffer(pointOpacitiesColors.Length, 4 * sizeof(float));

                pointOpacitiesRxRyBuffer.SetData(pointOpacitiesRxRy);
                elipseOpacitiesWeightsBuffer.SetData(pointOpacitiesWeights);
                positionsOpacitiesBuffer.SetData(pointOpacitiesPositions);
                colorsOpacitiesBuffer.SetData(pointOpacitiesColors);


                cs.SetBuffer(0, "_PointColorsRxRy", pointOpacitiesRxRyBuffer);
                cs.SetBuffer(0, "_PointColorsElipseWeights", elipseOpacitiesWeightsBuffer);
                cs.SetBuffer(0, "_PointColorsPositions", positionsOpacitiesBuffer);
                cs.SetBuffer(0, "_PointColorsColors", colorsOpacitiesBuffer);
                
                cs.SetBool("_OpMode", true);
                cs.SetInt("_NumColPoints", pointOpacitiesPositions.Length);
                cs.SetTexture(0, "Result", rt);
                cs.Dispatch(0, rt.width / 16, rt.height / 16, 1);


                Graphics.CopyTexture(rt, transferFunctionTex);


                pointOpacitiesRxRyBuffer.Release();
                pointOpacitiesRxRyBuffer = null;
                elipseOpacitiesWeightsBuffer.Release();
                elipseOpacitiesWeightsBuffer = null;
                positionsOpacitiesBuffer.Release();
                positionsOpacitiesBuffer = null;
                colorsOpacitiesBuffer.Release();
                colorsOpacitiesBuffer = null;
            }
            else
            {
                csClear.SetTexture(0, "Result", rt);
                csClear.SetBool("_OpMode", true);
                csClear.Dispatch(0, rt.width / 16, rt.height / 16, 1);

                Graphics.CopyTexture(rt, transferFunctionTex);
            }
        }

        private Vector2 ConstructTextureVectorFromPoint(TransferFunctionPoint p)
        {
            Vector2 pointTexture = ConstructTextureVectorFromPointGivenLambda(p, (point) => {
                return new Vector2((int)(point.GetPositionX() * transferFunctionTex.width), (int)(point.GetPositionY() * transferFunctionTex.height));
            });

            return pointTexture;
        }

        private Vector2 ConstructTextureVectorFromPointEllipsoid(TransferFunctionPoint p)
        {
            Vector2 pointTexture = ConstructTextureVectorFromPointGivenLambda(p, (point) => {
                return new Vector2((int)(point.GetRx() * transferFunctionTex.width), (int)(point.GetRy() * transferFunctionTex.height));
            });

            return pointTexture;
        }

        private Vector2 ConstructTextureVectorFromPointGivenLambda(TransferFunctionPoint p, System.Func<TransferFunctionPoint, Vector2> lambda)
        {
            Vector2 pointTexture = lambda(p);
            return pointTexture;
        }

        private Color GetRgbFromHsv(Color hsv)
        {
            return Color.HSVToRGB(hsv.r, hsv.g, hsv.b);
        }

        private Color GetHsvFromRgb(Color rgb)
        {
            Color hsv = Color.clear;
            Color.RGBToHSV(rgb, out hsv.r, out hsv.g, out hsv.b);
            return hsv;
        }

        private void RegisterToEvents()
        {
            TransferFunctionPoint.pointClickDelegate += PointClick;

            loaderColors.GetComponent<LoadPopup>().buttonClick += LoadColPoints;
            loaderOpacities.GetComponent<LoadPopup>().buttonClick += LoadOpPoints;

            saverColors.GetComponent<SavePopup>().saveButtonClick += SaveColConfirm;
            saverOpacities.GetComponent<SavePopup>().saveButtonClick += SaveOpConfirm;
        }

        private void DeregisterFromEvenets()
        {
            TransferFunctionPoint.pointClickDelegate -= PointClick;

            loaderColors.GetComponent<LoadPopup>().buttonClick -= LoadColPoints;
            loaderOpacities.GetComponent<LoadPopup>().buttonClick -= LoadOpPoints;

            saverColors.GetComponent<SavePopup>().saveButtonClick -= SaveColConfirm;
            saverOpacities.GetComponent<SavePopup>().saveButtonClick -= SaveOpConfirm;
        }

        private void PointClick(TransferFunctionPoint point)
        {

            /*
             *            selectedPoint = point;
            selectedPoint.Select();
            foreach (TransferFunctionPoint p in tfPoints)
            {
                p.SetInteractivity(false);
            } 
             */

            if (this.selectedPoint == point) return;

            bool isColorPoint = tfPointsColors.Contains(point);
            bool isOpacityPoint = tfPointsOpacities.Contains(point);

            if (isColorPoint || isOpacityPoint)
            {
                // this function is called on static event, so it might be that the user clicked on point in some other drawer
                if (this.selectedPoint != null)
                {
                    DeselectPoint();
                    if (this.selectedPointMode == TransferFunctionColorMode.HSV)
                    {
                        this.colorPointsController.SetActive(false);
                    }
                    else
                    {
                        this.opacityPointsController.SetActive(false);
                    }
                }

                this.selectedPoint = point;
                this.selectedPoint.Select();

                if (isColorPoint)
                {
                    this.selectedPointMode = TransferFunctionColorMode.HSV;
                    this.colorPointsController.SetActive(!this.colorPointsController.activeSelf);
                    SetColorSlidersValueToPointValue();
                }
                else
                {
                    this.selectedPointMode = TransferFunctionColorMode.Opacity;
                    this.opacityPointsController.SetActive(!this.opacityPointsController.activeSelf);
                    SetOpacitySlidersValueToPointValue();
                }

                ComputeTexture();
            }

        }

        private TransferFunctionPoint CreateNewPoint()
        {
            TransferFunctionPoint p = Instantiate(tfPoint).GetComponent<TransferFunctionPoint>();
            p.transform.localScale = new Vector3(this.pointScale, this.pointScale, this.pointScale);
            p.SetPositionX(0.5f); // to match default value of slider
            return p;
        }

        private void PositionNewPointInUi(TransferFunctionPoint point, TransferFunctionColorMode colorMode)
        {
            switch (colorMode)
            {
                case TransferFunctionColorMode.HSV: SetPointInUi(point, tfColorsCanvas); break;
                case TransferFunctionColorMode.Opacity: SetPointInUi(point, tfOpacitiesCanvas); break;
            }
        }

        private void SetPointInUi(TransferFunctionPoint point, GameObject canvas)
        {
            point.transform.SetParent(canvas.transform);
            Vector3 canvasDim = canvas.GetComponent<Renderer>().bounds.size;

            Vector3 pointPosition = point.transform.position;
            pointPosition.x = canvas.transform.position.x - (canvasDim.x / 2) + (point.GetPositionX() * canvasDim.x);
            pointPosition.y = canvas.transform.position.y - (canvasDim.y / 2) + (point.GetPositionY() * canvasDim.y);
            pointPosition.z = canvas.transform.position.z + this.pointZAxis; // add some delta so we are in front + we are in 3d (: 

            point.transform.position = pointPosition;
        }

        private TransferFunctionColorMode GetColorModeFromInt(int colorMode)
        {
            return (TransferFunctionColorMode)colorMode;
        }

        private void AddBorderPoints(TransferFunctionColorMode colorMode)
        {
            TransferFunctionPoint p1 = CreateNewPoint();
            p1.SetPositionX(0);
            p1.SetPositionY(0);
            p1.SetEndPoint(true);

            TransferFunctionPoint p2 = CreateNewPoint();
            p2.SetPositionX(1);
            p2.SetPositionY(0);
            p2.SetEndPoint(true);

            Debug.Log("Border points values (p1 alpha p2 alpha): " + p1.GetColor().a + ", " + p2.GetColor().a);
            switch (colorMode)
            {
                case TransferFunctionColorMode.HSV: tfPointsColors.AddLast(p1); tfPointsColors.AddLast(p2); break;
                case TransferFunctionColorMode.Opacity: tfPointsOpacities.AddLast(p1); tfPointsOpacities.AddLast(p2); break;
            }
            PositionNewPointInUi(p1, colorMode); PositionNewPointInUi(p2, colorMode);
        }

        private void RemovePoint(TransferFunctionPoint point, TransferFunctionColorMode colorMode)
        {
            if (!point.IsEndPoint())
            {
                switch (colorMode)
                {
                    case TransferFunctionColorMode.HSV: tfPointsColors.Remove(point); break;
                    case TransferFunctionColorMode.Opacity: tfPointsOpacities.Remove(point); break;
                }
                Destroy(point.gameObject);
            }
        }

        private void ClearPoints(TransferFunctionColorMode colorMode)
        {
            switch (colorMode)
            {
                case TransferFunctionColorMode.HSV:
                    {
                        while (tfPointsColors.Count > 0)
                        {
                            TransferFunctionPoint point = tfPointsColors.First.Value;
                            tfPointsColors.Remove(point);
                            Destroy(point.gameObject);
                        }
                        break;
                    }
                case TransferFunctionColorMode.Opacity:
                    {
                        while (tfPointsOpacities.Count > 0)
                        {
                            TransferFunctionPoint point = tfPointsOpacities.First.Value;
                            tfPointsOpacities.Remove(point);
                            Destroy(point.gameObject);
                        }
                        break;
                    }
            }
        }

        private void DeselectPoint()
        {
            this.selectedPoint.Deselect();
            this.selectedPoint = null;
            //this.selectedPointMode = TransferFunctionColorMode.None;            
        }

        private void ReadColors()
        {
            if (this.selectedPoint != null)
            {
                Color colorHsv = Color.clear;
                colorHsv.r = sliderHue.CurrentValue();
                colorHsv.g = sliderSat.CurrentValue();
                colorHsv.b = sliderVal.CurrentValue();
                Debug.Log("Read point colors from sliders: " + colorHsv.r + ", " + colorHsv.g + ", " + colorHsv.b);
                colorHsv.a = this.selectedPoint.GetColor().a;
                this.selectedPoint.SetColor(colorHsv);
            }
        }

        private string ConstructFileExtension(TransferFunctionColorMode colorMode)
        {
            string fileMark = "";
            switch (tfMode)
            {
                case TransferFunctionMode.TF1D: fileMark += colorMode == TransferFunctionColorMode.HSV ? VolumeAssetNames.tf1dName : VolumeAssetNames.tf1dOpacityName; break;
                case TransferFunctionMode.TF1Dplus: fileMark += VolumeAssetNames.tf1dplusName; break;
                case TransferFunctionMode.Ellipse: fileMark += colorMode == TransferFunctionColorMode.HSV ? VolumeAssetNames.tfEllipseName : VolumeAssetNames.tfEllipseOpacityName; break;
                case TransferFunctionMode.TFRectangle: fileMark += colorMode == TransferFunctionColorMode.HSV ? VolumeAssetNames.tfRectangleName : VolumeAssetNames.tfRectangleOpacityName; break;
            }
            return fileMark;
        }

        private void LoadPoints(LinkedList<TransferFunctionPoint> points, LinkedList<TransferFunctionSaveObject> loadedPoints, TransferFunctionColorMode mode)
        {
            ClearPoints(mode);
            foreach (var p in loadedPoints)
            {
                var newPoint = CreateNewPoint();
                newPoint.SetValuesFromSavedOject(p);
                points.AddLast(newPoint);
                PositionNewPointInUi(newPoint, mode);
            }
            ComputeTexture();
        }

        private void RemovePoint(LinkedList<TransferFunctionPoint> collection)
        {
            if (this.selectedPoint != null && !this.selectedPoint.IsEndPoint())
            {
                collection.Remove(this.selectedPoint);
                this.selectedPoint.Deselect();
                Destroy(this.selectedPoint.gameObject);
                this.selectedPoint = null;
                //this.selectedPointMode = TransferFunctionColorMode.None;
            }
        }

        #endregion



        #region TF Button Methods
        public void OnCreatePoint(int colorMode)
        {
            TransferFunctionColorMode mode = GetColorModeFromInt(colorMode);
            TransferFunctionPoint point = CreateNewPoint();
            switch (mode)
            {
                case TransferFunctionColorMode.HSV: tfPointsColors.AddLast(point); break;
                case TransferFunctionColorMode.Opacity: tfPointsOpacities.AddLast(point); break;
            }
            PositionNewPointInUi(point, mode);
        }

        public void OnLoadColorPoints()
        {
            this.loaderColors.SetActive(!this.loaderColors.activeSelf);
            string extension = ConstructFileExtension(TransferFunctionColorMode.HSV);
            LinkedList<string> saveFiles = TransferFunctionSaver.ListAvailableTransferFunctions(extension);
            this.loaderColors.GetComponent<LoadPopup>().FillTheContent(saveFiles);
        }

        public void OnLoadOpacityPoints()
        {
            this.loaderOpacities.SetActive(!this.loaderOpacities.activeSelf);
            string extension = ConstructFileExtension(TransferFunctionColorMode.Opacity);
            LinkedList<string> saveFiles = TransferFunctionSaver.ListAvailableTransferFunctions(extension);
            this.loaderOpacities.GetComponent<LoadPopup>().FillTheContent(saveFiles);
        }

        public void LoadColPoints(string fileName)
        {
            // Load values
            var loadedPoints = TransferFunctionSaver.LoadPoints(fileName);
            LoadPoints(this.tfPointsColors, loadedPoints, TransferFunctionColorMode.HSV);
            this.loaderColors.SetActive(false);
        }

        public void LoadOpPoints(string fileName)
        {
            var loadedPoints = TransferFunctionSaver.LoadPoints(fileName);
            LoadPoints(this.tfPointsOpacities, loadedPoints, TransferFunctionColorMode.Opacity);
            this.loaderOpacities.SetActive(false);
        }

        public void OnSaveColorPoints()
        {
            this.saverColors.SetActive(!this.saverColors.activeSelf);
        }

        public void OnSaveOpPoints()
        {
            this.saverOpacities.SetActive(!this.saverOpacities.activeSelf);
        }

        public void SaveColConfirm(string fileName)
        {
            string fileMark = ConstructFileExtension(TransferFunctionColorMode.HSV);
            fileName += fileMark;
            TransferFunctionSaver.SavePoints(this.tfPointsColors, fileName);
        }

        public void SaveOpConfirm(string fileName)
        {
            string fileMark = ConstructFileExtension(TransferFunctionColorMode.Opacity);
            fileName += fileMark;
            TransferFunctionSaver.SavePoints(this.tfPointsOpacities, fileName);
        }

        public void OnClearPoints(int colorMode)
        {
            TransferFunctionColorMode mode = GetColorModeFromInt(colorMode);

            switch (mode) 
            {
                case TransferFunctionColorMode.HSV: this.colorPointsController.SetActive(false); break;
                case TransferFunctionColorMode.Opacity: this.opacityPointsController.SetActive(false); break;
            }

            ClearPoints(mode);
            if(tfMode == TransferFunctionMode.TF1D || tfMode == TransferFunctionMode.TF1Dplus)
            {
                AddBorderPoints(mode);
            }

            ComputeTexture();
        }

        public void OnRecompute()
        {
            ComputeTexture();
        }

        #endregion

        #region Point Button Methods

        public void OnRemoveButton(int colorMode)
        {
            TransferFunctionColorMode mode = GetColorModeFromInt(colorMode);
            switch (mode)
            {
                case TransferFunctionColorMode.HSV: RemovePoint(this.tfPointsColors); this.colorPointsController.SetActive(false); break;
                case TransferFunctionColorMode.Opacity: RemovePoint(this.tfPointsOpacities); this.opacityPointsController.SetActive(false); break;
            }
        }

        public void OnClosePointMenu(int colorMode)
        {
            TransferFunctionColorMode mode = GetColorModeFromInt(colorMode);
            switch (mode)
            {
                case TransferFunctionColorMode.HSV: DeselectPoint(); this.colorPointsController.SetActive(false); break;
                case TransferFunctionColorMode.Opacity: DeselectPoint(); this.opacityPointsController.SetActive(false); break;
            }
            ComputeTexture();
        }

        #endregion

        #region Point Slider Methods
        private void SetColorSlidersValueToPointValue()
        {
            Debug.Log("Opacity: " + this.selectedPoint.GetColor().a + ", y: " + this.selectedPoint.GetPositionY());
            this.sliderPosX?.SetSliderValue(this.selectedPoint.GetPositionX());
            this.sliderPosY?.SetSliderValue(this.selectedPoint.GetPositionY());
            this.sliderRx?.SetSliderValue(this.selectedPoint.GetRx());
            this.sliderRy?.SetSliderValue(this.selectedPoint.GetRy());
            this.sliderWeight?.SetSliderValue(this.selectedPoint.GetEllipsoidWeight());

            Color hsv = this.selectedPoint.GetColor();
            this.sliderHue?.SetSliderValue(hsv.r);
            this.sliderSat?.SetSliderValue(hsv.g);
            this.sliderVal?.SetSliderValue(hsv.b);
    }

        private void SetOpacitySlidersValueToPointValue()
        {
            this.sliderPosXOp?.SetSliderValue(this.selectedPoint.GetPositionX());
            this.sliderPosYOp?.SetSliderValue(this.selectedPoint.GetPositionY());
            this.sliderRxOp?.SetSliderValue(this.selectedPoint.GetRx());
            this.sliderRyOp?.SetSliderValue(this.selectedPoint.GetRy());
            this.sliderWeightOp?.SetSliderValue(this.selectedPoint.GetEllipsoidWeight());

            this.sliderAlpha?.SetSliderValue(this.selectedPoint.GetColor().a);
        }

        public void OnSliderX(SliderEventData data)
        {
            if (this.selectedPoint != null)
            {
                this.selectedPoint.SetPositionX(data.NewValue);
                PositionNewPointInUi(this.selectedPoint, this.selectedPointMode);
            }
        }

        public void OnSliderY(SliderEventData data)
        {
            if (this.selectedPoint != null)
            {
                this.selectedPoint.SetPositionY(data.NewValue);
                PositionNewPointInUi(this.selectedPoint, this.selectedPointMode);
            }
        }

        public void OnSliderRx(SliderEventData data)
        {
            if (this.selectedPoint != null)
            {
                this.selectedPoint.SetRx(data.NewValue);
            }
        }

        public void OnSliderRy(SliderEventData data)
        {
            if (this.selectedPoint != null)
            {
                this.selectedPoint.SetRy(data.NewValue);
            }
        }

        public void OnSliderWeight(SliderEventData data)
        {
            if (this.selectedPoint != null)
            {
                this.selectedPoint.SetEllipsoidWeight(data.NewValue);
            }
        }

        public void OnSliderHue(SliderEventData data)
        {
            ReadColors();
        }

        public void OnSliderSaturation(SliderEventData data)
        {
            ReadColors();
        }

        public void OnSliderValue(SliderEventData data)
        {
            ReadColors();
        }

        public void OnSliderOpacity(SliderEventData data)
        {
            if (this.selectedPoint != null)
            {
                Color opacityCol = this.selectedPoint.GetColor();
                opacityCol.a = data.NewValue;
                this.selectedPoint.SetColor(opacityCol);
            }
        }

        #endregion
    }
}

