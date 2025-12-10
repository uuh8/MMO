using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    public Sprite activeImage;      // 激活时候的图片
    private Sprite normalImage;     // 正常时候的图片
    public TabView tabView;

    public int tabIndex = 0;        // 第几页
    public bool selected = false;

    private Image tabImage;

    private void Start()
    {
        tabImage = this.GetComponent<Image>();
        normalImage = tabImage.sprite;
        // 给按钮添加事件
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }
    /// <summary>
    /// 点击按钮后切换按钮为“激活颜色”
    /// </summary>
    /// <param name="selected"></param>
    public void Select(bool selected)
    {
        tabImage.overrideSprite = selected ? activeImage : normalImage;
    }
    /// <summary>
    /// 切换页数
    /// </summary>
    void OnClick()
    {
        this.tabView.SelectTab(this.tabIndex);
    }
}
