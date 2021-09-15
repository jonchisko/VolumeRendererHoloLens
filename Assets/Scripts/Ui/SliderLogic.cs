using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public class SliderLogicEvent : UnityEvent<SliderEventData> { }


public class SliderLogic : MonoBehaviour
{

    public PinchSlider slider;
    public SliderLogicEvent changedValue;

    [Tooltip("Set this to the desired min value.")]
    public float minValue = 0.0f;
    [Tooltip("Set this to the desired max value.")]
    public float maxValue = 1.0f;
    [Tooltip("Set this to true if you want values to be treated as integers.")]
    public bool toggleIntegers = false;

    [SerializeField]
    private float currentValue = 0.0f;

    private void Awake()
    {
        Debug.Assert(minValue < maxValue, "minValue is NOT strictly smaller than maxValue!");
        if (this.currentValue < minValue) this.currentValue = minValue;
        slider.SliderValue = this.currentValue;
        Debug.Assert(slider.SliderValue == this.currentValue, "SliderValue SHOULD be the same as currentValue!");

        if(toggleIntegers)
        {
            this.minValue = Mathf.RoundToInt(minValue);
            this.maxValue = Mathf.RoundToInt(maxValue);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnSliderUpdated(SliderEventData eventData)
    {
        float oldValue = this.currentValue;
        // eventData new value is between 0.0 and 1.0, need to transform it to our range
        this.currentValue = TransformValue(eventData.NewValue);
        // SliderEventData -> first param is old val, second is new val, third is pointer, fourth is the object
        SliderEventData updated_eventData = new SliderEventData(oldValue, currentValue, eventData.Pointer, eventData.Slider);
        changedValue?.Invoke(updated_eventData);
    }

    public float CurrentValue()
    {
        return currentValue;
    }

    private float TransformValue(float value)
    {
        float delta = this.maxValue - this.minValue;
        float result = this.minValue + delta * value;
        if (toggleIntegers) result = Mathf.RoundToInt(result);
        return result;
    }
}
