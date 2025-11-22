using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIIconItem : MonoBehaviour
{
    public Image mainImage;
    public Text mainText;

    public Image secondImage;

    public void SetMainIcon(string iconName, string text)
    {
        // overrideSprite: Image组件的一个属性，用于覆盖默认显示的 Sprite
        this.mainImage.overrideSprite = Resloader.Load<Sprite>(iconName);
        this.mainText.text = text;
    }

}
