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
    public TouchScreenKeyboard keyboard;

    public TextMeshPro showText;
    private string saveFileName = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        if(keyboard != null)
        {
            string keyboardText = keyboard.text;
            this.saveFileName = ClearText(keyboardText);
            showText.text = this.saveFileName;
        }
    }

    public void OnSaveButton()
    {
        saveButtonClick?.Invoke(this.saveFileName);
        dictationObject.StopRecording();
        this.gameObject.SetActive(false);
    }

    public void OnStartRecordingButton(string text)
    {
        Debug.Log("Saver : text received: " + text);
        this.saveFileName = ClearText(text);
        showText.text = this.saveFileName;
    }

    public void OnKeyboardOpen()
    {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "enter filename");
    }

    private string ClearText(string text)
    {
        char[] charsToTrim = { '*', ' ', '\'', '.', ':', '\n' };
        text = text.Trim(charsToTrim);
        text = text.Replace(' ', '_');
        return text;
    }
}
