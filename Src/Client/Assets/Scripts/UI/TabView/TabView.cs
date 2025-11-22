using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabView : MonoBehaviour
{
    public TabButton[] tabButtons;
    public GameObject[] tabPages;

    public int index = -1;
    
    // 初始化
    IEnumerator Start()
    {
        for(int i = 0; i < tabButtons.Length; i++)
        {
            tabButtons[i].tabView = this;
            tabButtons[i].tabIndex = i;
        }
        yield return new WaitForEndOfFrame();   // 等一帧
        SelectTab(0);   // 默认选择第一页
    }

    public void SelectTab(int index)
    {
        if(this.index != index)
        {
            for(int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].Select(i == index);
                tabPages[i].SetActive(i == index);
            }
        }
    }
}
