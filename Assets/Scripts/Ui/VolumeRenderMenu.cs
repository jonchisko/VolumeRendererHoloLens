using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace com.jon_skoberne.UI
{
    public class VolumeRenderMenu : MonoBehaviour
    {
        public GameObject clipMenu;
        public GameObject tfMenu;
        public GameObject shaderMenu;
        public GameObject manipulatorMenu;


        // Start is called before the first frame update
        void Start()
        {

        }



        public void ToggleClipMenu()
        {
            clipMenu.SetActive(!clipMenu.activeSelf);
        }

        public void ToggleTfMenu()
        {
            tfMenu.SetActive(!tfMenu.activeSelf);
        }

        public void ToggleShaderMenu()
        {
            shaderMenu.SetActive(!shaderMenu.activeSelf);
        }

        public void ToggleManipulatorMenu()
        {
            manipulatorMenu.SetActive(!manipulatorMenu.activeSelf);
        }

        public void QuitToMenu()
        {
            // 0 is the first scene in build order: App entry point
            SceneManager.LoadScene(0);
        }
    }
}