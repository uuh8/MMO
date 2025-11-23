using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabView : MonoBehaviour
{
    public TabButton[] tabButtons;
    public GameObject[] tabPages;

    public int index = -1;

    // 如果 Start 的签名是 IEnumerator Start()，Unity 会自动把它当成协程来执行，协程启动时间就是Start函数的执行时间
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

    /// <summary>
    /// 切换到选中的页数
    /// </summary>
    /// <param name="index"></param>
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
