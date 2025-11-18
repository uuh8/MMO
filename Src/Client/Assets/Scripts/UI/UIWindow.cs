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

    public enum WindowResult
    {
        None = 0,
        Yes,
        No,
    }

    public void Close(WindowResult result = WindowResult.None)
    {
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

    private void OnMouseDown()
    {
        Debug.LogFormat(this.name + "Clicked!");
    }
}
