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

    // 面板激活时自动调用（UIManager.Show 触发 SetActive(true) 时执行）
    private void OnEnable()
    {
        InputManager.Instance.OnUIOpen();
    }

    // 面板隐藏时自动调用（UIManager.Close 触发 SetActive(false) 时执行）
    private void OnDisable()
    {
        InputManager.Instance.OnUIClose();
    }

    public void Close(WindowResult result = WindowResult.None)
    {
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Win_Close);
        UIManager.Instance.Close(this.type);
        if (this.OnClose != null)
            this.OnClose(this, result);
        this.OnClose = null;
    }

    // 三个虚函数，子类如果需要可以重写
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
