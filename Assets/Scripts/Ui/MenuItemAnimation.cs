using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace com.jon_skoberne.UI
{
    public class MenuItemAnimation : MonoBehaviour
    {
        public delegate void notifyFunction(MenuItemAnimation ele);
        public static notifyFunction OnComplete;

        public LeanTweenType inType;
        public LeanTweenType outType;
        public CanvasGroup cGroup;

        // Start is called before the first frame update
        void Start()
        {

        }

        public void EnableMenu()
        {
            this.gameObject.SetActive(true);
            AnimateIn();
        }

        public void DisableMenu()
        {
            LeanTween.alphaCanvas(this.cGroup, 0.0f, 0.25f).setEase(this.outType).setOnComplete(DisableMe);
        }

        private void AnimateIn()
        {
            this.cGroup.alpha = 0.0f;
            LeanTween.alphaCanvas(this.cGroup, 1.0f, 0.25f).setEase(this.inType);
        }

        private void DisableMe()
        {
            this.gameObject.SetActive(false);
            OnComplete?.Invoke(this);
        }

    }
}

