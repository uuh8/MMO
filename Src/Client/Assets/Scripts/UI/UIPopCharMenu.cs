using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// 聊天界面点击玩家后弹出的“人物操作菜单”
/// 关键需求：点击任意地方（弹窗外）自动消失
/// 实现方式：
/// 1) 弹窗出现后主动 Select() 自己，成为 EventSystem 的当前选中对象
/// 2) 当玩家点击其他地方导致选中对象切换时，EventSystem 会回调 OnDeselect()
/// 3) 在 OnDeselect 中判断是否点击在弹窗外，外部点击则 Close()
public class UIPopCharMenu : UIWindow, IDeselectHandler
{
    public int targetId;
    public string targetName;

    /// <summary>
    /// IDeselectHandler 接口是用于“取消选择”后的处理
    /// ISelectHandler 接口是用于“选择”后的处理
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDeselect(BaseEventData eventData)
    {
        // Deselect 事件里通常会携带 PointerEventData（鼠标/触摸相关数据）
        PointerEventData ed = eventData as PointerEventData;

        // hovered：当前指针悬停命中的 UI 对象列表 (Raycast 命中链) (从根节点到子节点都包含)
        // 这里的意图是：如果指针仍然在弹窗上，就不要关闭；
        // 否则认为点到了弹窗外 -> 关闭弹窗
        if (ed.hovered.Contains(this.gameObject))
            return;

        // WindowResult.None：表示用户不是点击了某个具体操作按钮导致的关闭，而是“点外部取消”
        this.Close(WindowResult.None);
    }

    public void OnEnable()
    {
        // 弹窗一显示就立刻选中自己
        // 这样当用户点击其他地方时，EventSystem 才会触发 OnDeselect
        // 前提：同一个 GameObject 上要挂 Selectable（否则会 NullReference）
        this.GetComponent<Selectable>().Select();
        this.Root.transform.position = Input.mousePosition + new Vector3(80, 0, 0);
    }

    public void OnChat()
    {
        this.Close(WindowResult.No);
    }

    public void OnAddFriend()
    {
        this.Close(WindowResult.No);
    }
    public void OnInviteTeam()
    {
        this.Close(WindowResult.No);
    }
}
