using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public class SliderLogicEvent : UnityEvent<SliderEventData> { }


[Serializable]
public struct SliderLogicValue
{
    [SerializeField]
    float val;
    [SerializeField]
    bool toggleIntegers;

    float delta, min, max;

    public SliderLogicValue(float val, float min, float max, bool toggleIntegers)
    {
        Debug.Assert(min < max, "Slider Logic Value, min must strictly be less than max!");
        Debug.Assert(min <= val && max >= val, "Slider Logic Value, val must be GE to min and LE to max!");

        this.val = val;
        this.min = min;
        this.max = max;
        this.toggleIntegers = toggleIntegers;
        this.delta = this.max - this.min;
    }

    public float GetVal()
    {
        return this.val;
    }

    public float GetFraction()
    {
        return (this.val - this.min) / this.delta;
    }

    public void SetValueWithFraction(float value)
    {
        Debug.Assert(value >= 0.0 && value <= 1.0, "Value in Set Value must be between 0 and 1!");
        float result = this.min + this.delta * value;
        if (toggleIntegers) result = Mathf.RoundToInt(result);
        this.val = result;
    }
}

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
    private float value = 0.0f;

    private SliderLogicValue currentValue;

    private void Awake()
    {
        Debug.Assert(minValue < maxValue, "minValue is NOT strictly smaller than maxValue!");
        if (toggleIntegers)
        {
            this.minValue = Mathf.RoundToInt(minValue);
            this.maxValue = Mathf.RoundToInt(maxValue);
        }
        this.currentValue = new SliderLogicValue(value, minValue, maxValue, toggleIntegers);

        slider.SliderValue = this.currentValue.GetFraction();
        Debug.Assert(slider.SliderValue == this.currentValue.GetFraction(), "SliderValue SHOULD be the same as currentValue!");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnSliderUpdated(SliderEventData eventData)
    {
        SliderLogicValue oldValue = this.currentValue;
        // eventData new value is between 0.0 and 1.0, need to transform it to our range
        this.currentValue.SetValueWithFraction(eventData.NewValue);
        // SliderEventData -> first param is old val, second is new val, third is pointer, fourth is the object
        SliderEventData updated_eventData = new SliderEventData(oldValue.GetVal(), this.currentValue.GetVal(), eventData.Pointer, eventData.Slider);
        changedValue?.Invoke(updated_eventData);
    }

    public float CurrentValue()
    {
        return this.currentValue.GetVal();
    }
}
