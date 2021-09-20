using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TfMenu : MonoBehaviour
{
    public GameObject hist;
    public GameObject tf1d;
    public GameObject tf2d;
    public GameObject tfellipse;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    
    public void ToggleHistogram()
    {
        hist.SetActive(!hist.activeSelf);
    }

    public void ToggleTf1D()
    {
        tf1d.SetActive(true);
        tf2d.SetActive(false);
        tfellipse.SetActive(false);
    }

    public void ToggleTf2D()
    {
        tf1d.SetActive(false);
        tf2d.SetActive(true);
        tfellipse.SetActive(false);
    }

    public void ToggleTfEllipse()
    {
        tf1d.SetActive(false);
        tf2d.SetActive(false);
        tfellipse.SetActive(true);
    }

}
