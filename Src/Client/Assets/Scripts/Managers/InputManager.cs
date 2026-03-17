using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoSingleton<InputManager>
{
    public bool IsInputMode { get; set; }

    // 当前打开的 UI 面板数量
    private int openUICount = 0;

    // 是否有 UI 面板打开（外部只读）
    public bool HasOpenUI => openUICount > 0;
    /*
        // 在所有 3D 射线检测前统一用
        if (EventSystem.current.IsPointerOverGameObject() || InputManager.Instance.HasOpenUI)
        return;
     */

    public bool IsOverUI = false;

    // 是否应该显示光标
    public bool ShouldShowCursor => openUICount > 0 || IsInputMode || IsOverUI;

    // UI 面板打开时调用
    public void OnUIOpen()
    {
        openUICount++;
        UpdateCursor();
    }

    // UI 面板关闭时调用
    public void OnUIClose()
    {
        openUICount = Mathf.Max(0, openUICount - 1);
        UpdateCursor();
    }

    public void UpdateCursor()
    {
        if (ShouldShowCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}