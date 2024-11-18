using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueDisplay : MonoBehaviour
{
    public Slider slider;
    public Text progessNumber;

    void Start()
    {
        slider.value = 0;
    }

    // Update is called once per frame
    void Update()
    {
        int progressValue = Mathf.RoundToInt(slider.value);
        progessNumber.text = progressValue + "%";
    }
}
