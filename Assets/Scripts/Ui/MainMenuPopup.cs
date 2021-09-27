using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace com.jon_skoberne.UI
{
    public class MainMenuPopup : MonoBehaviour
    {
        public LeanTweenType inType;
        public LeanTweenType outType;

        [SerializeField]
        private TextMeshProUGUI titleText = null;
        [SerializeField]
        private TextMeshProUGUI contentText = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        public void OpenPopup(string titleText, string contentText)
        {
            SetTitle(titleText);
            SetContent(contentText);
            if (!this.gameObject.activeSelf)
            {
                EnableMe();
                AnimateIntro();
            }
        }

        public void OnCloseButton()
        {
            LeanTween.scale(this.gameObject, new Vector3(0, 0, 0), 0.25f).setEase(this.outType).setOnComplete(DisableMe);
        }

        private void SetTitle(string text)
        {
            titleText.text = text;
        }

        private void SetContent(string text)
        {
            contentText.text = text;
        }

        private void AnimateIntro()
        {
            this.transform.localScale = Vector3.zero;
            LeanTween.scale(this.gameObject, new Vector3(1, 1, 1), 0.25f).setEase(this.inType);
        }

        private void EnableMe()
        {
            this.gameObject.SetActive(true);
        }

        private void DisableMe()
        {
            this.gameObject.SetActive(false);
        }
    }
}


