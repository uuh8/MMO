using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    public Sprite activeImage;      // 激活时候的图片
    private Sprite normalImage;     // 正常时候的图片

    public TabView tabView;

    public int tabIndex = 0;
    public bool selected = false;

    private Image tabImage;

    private void Start()
    {
        tabImage = this.GetComponent<Image>();
        normalImage = tabImage.sprite;
        // 绑定点击的事件
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void Select(bool select)
    {
        tabImage.overrideSprite = select ? activeImage : normalImage;
    }

    void OnClick()
    {
        this.tabView.SelectTab(this.tabIndex);
    }
}
