using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TfMenu : MonoBehaviour
{
    public GameObject hist1d;
    public GameObject hist2d_grad;
    public GameObject tf1d;
    public GameObject tf2d;
    public GameObject tfellipse;
    public GameObject tfrect;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    
    public void ToggleHistogram1d()
    {
        hist1d.SetActive(!hist1d.activeSelf);
    }

    public void ToggleHistogram2d_grad()
    {
        hist2d_grad.SetActive(!hist2d_grad.activeSelf);
    }

    public void ToggleTf1D()
    {
        tf1d.SetActive(true);
        tf2d.SetActive(false);
        tfellipse.SetActive(false);
        tfrect.SetActive(false);
    }

    public void ToggleTf2D()
    {
        tf1d.SetActive(false);
        tf2d.SetActive(true);
        tfellipse.SetActive(false);
        tfrect.SetActive(false);
    }

    public void ToggleTfEllipse()
    {
        tf1d.SetActive(false);
        tf2d.SetActive(false);
        tfellipse.SetActive(true);
        tfrect.SetActive(false);
    }

    public void ToggleTfRect()
    {
        tf1d.SetActive(false);
        tf2d.SetActive(false);
        tfellipse.SetActive(false);
        tfrect.SetActive(true);
    }
}
