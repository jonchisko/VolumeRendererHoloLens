using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.jon_skoberne.UI
{

    public class MainMenu : MonoBehaviour
    {

        [SerializeField]
        private MenuItemAnimation loaderScreen = null;
        [SerializeField]
        private MenuItemAnimation connectionScreen = null;


        // Start is called before the first frame update
        void Start()
        {
            RegisterEvents();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDestroy()
        {
            DeregisterEvents();
        }

        public void QuitApplication()
        {
            Application.Quit();
        }

        public void ShowLoaderScreen()
        {
            connectionScreen.DisableMenu();
        }

        public void ShowConnectionScreen()
        {
            loaderScreen.DisableMenu();
        }

        public void LoadVolumeScene()
        {
            SceneManager.LoadScene(1);
        }

        private void RegisterEvents()
        {
            MenuItemAnimation.OnComplete += EnableGivenMenuItem;
        }

        private void DeregisterEvents()
        {
            MenuItemAnimation.OnComplete -= EnableGivenMenuItem;
        }

        private void EnableGivenMenuItem(MenuItemAnimation item)
        {
            if (item != loaderScreen)
            {
                loaderScreen.EnableMenu();
            } else
            {
                connectionScreen.EnableMenu();
            }
        }
    }
}
