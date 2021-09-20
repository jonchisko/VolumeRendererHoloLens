using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SavePopup : MonoBehaviour
{
    public delegate void OnButtonClickDelegate(string name);
    public OnButtonClickDelegate saveButtonClick;
    public DictationHandler dictationObject;

    public TextMeshPro showText;
    private string saveFileName = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }



    public void OnSaveButton()
    {
        saveButtonClick?.Invoke(this.saveFileName);
        //dictationObject.StopRecording();
        this.gameObject.SetActive(false);
    }

    public void OnStartRecordingButton(string text)
    {
        Debug.Log("Saver : text received: " + text);
        char[] charsToTrim = { '*', ' ', '\'', '.', ':', '\n' };
        text = text.Trim(charsToTrim);
        text = text.Replace(' ', '_');
        this.saveFileName = text;
        showText.text = this.saveFileName;
    }
}
