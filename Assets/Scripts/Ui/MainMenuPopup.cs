using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuPopup : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI titleText = null;
    [SerializeField]
    private TextMeshProUGUI contentText = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    public void SetTitle(string text)
    {
        titleText.text = text;
    }

    public void SetContent(string text)
    {
        contentText.text = text;
    }
}
