using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIChat : MonoBehaviour
{
    public Text textAera;           // 聊天内容显示区域（普通 Text）
    public TabView channelTab;      // 频道切换 Tab 控件
    public InputField chatText;     // 聊天输入框
    public Text chatTarget;         // 私聊目标名显示（仅私聊频道可见）
    public Dropdown channelSelect;  // 发送频道下拉选择

    void Start()
    {
        // 订阅频道 Tab 切换事件
        this.channelTab.OnTabSelect += OnDisplayChannelSelected;

        // 订阅 ChatManager 的消息刷新事件
        // 每当收到新消息或频道切换时，自动刷新 UI
        ChatManager.Instance.OnChat += RefreshUI;

        // 用 InputField 的 onSubmit 事件处理发送
        // 这样 Unity 内部的 Enter 处理和我们的发送逻辑走同一条路，不会冲突
        chatText.onSubmit.AddListener(OnInputSubmit);
    }

    void OnDestroy()
    {
        // 解除所有订阅，防止对象销毁后仍然收到回调导致空引用
        ChatManager.Instance.OnChat -= RefreshUI;
        chatText.onSubmit.RemoveListener(OnInputSubmit);
    }

    void Update()
    {
        bool enterPressed = Input.GetKeyDown(KeyCode.Return)
                         || Input.GetKeyDown(KeyCode.KeypadEnter);

        // Enter 键：只在输入框没有焦点时激活输入框
        // 有焦点时的发送逻辑交给 onSubmit 处理，避免同帧双重触发
        if (enterPressed && !chatText.isFocused)
        {
            chatText.ActivateInputField();
            chatText.Select();
        }

        // 只在状态变化时才更新 IsInputMode，避免每帧都写
        if (chatText.isFocused != InputManager.Instance.IsInputMode)
        {
            InputManager.Instance.IsInputMode = chatText.isFocused;
            InputManager.Instance.UpdateCursor();
        }
    }

    /// <summary>
    /// InputField 的 onSubmit 回调（按下 Enter 且输入框有焦点时触发）
    /// </summary>
    private void OnInputSubmit(string text)
    {
        DoSend();
    }

    /// <summary>
    /// 切换显示频道时触发
    /// </summary>
    private void OnDisplayChannelSelected(int idx)
    {
        ChatManager.Instance.displayChannel = (ChatManager.LocalChannel)idx;
        RefreshUI();
    }

    /// <summary>
    /// 刷新聊天 UI
    /// 更新消息文本、发送频道选择、私聊目标显示
    /// </summary>
    private void RefreshUI()
    {
        this.textAera.text = ChatManager.Instance.GetCurrentMessages();
        this.channelSelect.value = (int)ChatManager.Instance.sendChannel - 1;

        // 私聊频道需要额外显示私聊对象名
        if (ChatManager.Instance.SendChannel == ChatChannel.Private)
        {
            this.chatTarget.gameObject.SetActive(true);
            this.chatTarget.text = ChatManager.Instance.PrivateID != 0
                ? ChatManager.Instance.PrivateName + ":"
                : "<无>";
        }
        else
        {
            this.chatTarget.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 点击发送按钮
    /// </summary>
    public void OnClickSend()
    {
        DoSend();
    }

    /// <summary>
    /// 统一的发送逻辑，Enter 键（onSubmit）和点击发送按钮都走这里
    /// </summary>
    private void DoSend()
    {
        string text = this.chatText.text;

        if (!string.IsNullOrWhiteSpace(text))
            ChatManager.Instance.SendChat(text, ChatManager.Instance.PrivateID, ChatManager.Instance.PrivateName);

        this.chatText.text = "";

        // 取消焦点，退出输入模式
        this.chatText.DeactivateInputField();
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// 发送频道下拉选择变化时触发
    /// idx + 1 是因为 Dropdown 没有"综合"选项，索引从 Local(1) 开始
    /// </summary>
    public void OnSendChannelChanged(int idx)
    {
        if (ChatManager.Instance.sendChannel == (ChatManager.LocalChannel)(idx + 1))
            return;

        // SetSendChannel 内部会校验条件（如未入队不能切队伍频道）
        // 校验失败则把 Dropdown 还原回当前实际频道
        if (!ChatManager.Instance.SetSendChannel((ChatManager.LocalChannel)idx + 1))
            this.channelSelect.value = (int)ChatManager.Instance.sendChannel - 1;
        else
            this.RefreshUI();
    }

    public void OnEndInput() { }
}