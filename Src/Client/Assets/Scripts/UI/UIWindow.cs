using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 父类(给所有UI）
public abstract class UIWindow : MonoBehaviour
{
    public delegate void CloseHandler(UIWindow sender, WindowResult result);
    public event CloseHandler OnClose;  // 关闭窗口的事件

    public virtual Type type { get { return this.GetType(); } }

    public GameObject Root; // 该窗口的父节点(Panel)

    public enum WindowResult
    {
        None = 0,
        Yes,
        No,
    }

    public void Close(WindowResult result = WindowResult.None)
    {
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Win_Close);
        UIManager.Instance.Close(this.type);
        if (this.OnClose != null)
            this.OnClose(this, result);
        this.OnClose = null;
    }

    // 两个虚函数，子类如果需要可以重写
    public virtual void OnCloseClick()
    {
        // 用于关闭
        this.Close();
    }
    public virtual void OnYesClick()
    {
        // 用于确认
        this.Close(WindowResult.Yes);
    }
    public virtual void OnNoClick()
    {
        // 用于确认
        this.Close(WindowResult.No);
    }

    private void OnMouseDown()
    {
        Debug.LogFormat(this.name + "Clicked!");
    }
}
