using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace com.jon_skoberne.TransferFunctionDrawer
{
    public class LoadPopup : MonoBehaviour
    {
        public delegate void OnButtonClickDelegate(string value);
        public OnButtonClickDelegate buttonClick;

        public GameObject buttonPrefab;
        public GameObject scrollviewContent;


        public void FillTheContent(LinkedList<string> storedFileNames)
        {
            ClearContentOnDisable();
            foreach (string fileName in storedFileNames)
            {
                GameObject spawnedButton = Instantiate(buttonPrefab);

                Interactable button = spawnedButton.GetComponent<Interactable>();
                string tmpString = string.Copy(fileName);
                button.GetComponentInChildren<TextMesh>().text = Path.GetFileName(fileName);
                button.OnClick.AddListener(delegate { OnButtonClick(tmpString); });
                spawnedButton.transform.SetParent(scrollviewContent.transform);


                /* This is for "modern" buttons, but they are weirdly rendered ...
                 * ButtonConfigHelper button = spawnedButton.GetComponent<ButtonConfigHelper>();
                string tmpString = string.Copy(fileName);
                button.MainLabelText = Path.GetFileName(fileName);
                button.OnClick.AddListener(delegate { OnButtonClick(tmpString); });
                spawnedButton.transform.SetParent(scrollviewContent.transform);*/
            }
            var gridOb = scrollviewContent.GetComponent<GridObjectCollection>();
            gridOb.UpdateCollection();
        }

        private void OnDisable()
        {
            ClearContentOnDisable();
        }

        private void OnButtonClick(string buttonText)
        {
            Debug.Log("Load Popup - button clicked: " + buttonText);
            buttonClick?.Invoke(buttonText);
        }

        private void ClearContentOnDisable()
        {
            foreach (Transform go in scrollviewContent.GetComponentsInChildren<Transform>())
            {
                if (go.GetComponent<GridObjectCollection>() == null) Destroy(go.gameObject);
            }
        }
    }
}