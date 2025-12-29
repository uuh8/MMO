using Candlelight.UI;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIChat : MonoBehaviour
{
    public HyperText textAera;  // 聊天内容显示区域

    public TabView channelTab;

    public InputField chatText; // 聊天输入控件
    public Text chatTarget;

    public Dropdown channelSelect;
    
    // Start is called before the first frame update
    void Start()
    {
        this.channelTab.OnTabSelect += OnDisplayChannelSelected;
        ChatManager.Instance.OnChat += RefreshUI;   // 订阅事件，当有消息发回来的时候更新UI
    }
    void Update()
    {
        // 每一帧都检查是不是在聊天输入模式（避免按键冲突）
        InputManager.Instance.IsInputMode = chatText.isFocused; // 有焦点说明在聊天模式
    }
    void OnDestroy()
    {
        ChatManager.Instance.OnChat -= RefreshUI;
    }


    private void OnDisplayChannelSelected(int idx)
    {
        // 告诉聊天管理器当前选择频道改变
        ChatManager.Instance.displayChannel = (ChatManager.LocalChannel)idx;
        RefreshUI();
    }

    private void RefreshUI()
    {
        this.textAera.text = ChatManager.Instance.GetCurrentMessages();
        this.channelSelect.value = (int)ChatManager.Instance.sendChannel - 1;

        // 如果是私聊模式需要单独设置“私聊对象”的信息
        if(ChatManager.Instance.SendChannel == ChatChannel.Private)
        {
            this.chatTarget.gameObject.SetActive(true);
            // 开启私聊模式后校验有没有私聊对象
            if(ChatManager.Instance.PrivateID != 0)
            {
                this.chatTarget.text = ChatManager.Instance.PrivateName + ":";
            }
            else
            {
                this.chatTarget.text = "<无>";
            }
        }
        else
        {
            this.chatTarget.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 点击聊天中的链接link（会弹出UIPopCharMenu）
    /// </summary>
    /// <param name="text"></param>
    /// <param name="link"></param>
    public void OnClickChatLink(HyperText text, HyperText.LinkInfo link)
    {
        Debug.Log($"[HyperTextClick] Name='{link.Name}', Class='{link.ClassName}', Index={link.Index}");

        // 1) link.Name 就是 <a name="..."> 的值
        if (string.IsNullOrEmpty(link.Name))
            return;

        // 2) 我们规定 name 格式为 "c:<id>"
        if (!link.Name.StartsWith("c:"))
            return;

        // 3) 解析 id
        string idStr = link.Name.Substring(2);
        if (!int.TryParse(idStr, out int id))
            return;

        // 4) 通过缓存取玩家名
        //    如果取不到，说明缓存还没被更新（例如你点的是很早的历史消息但没走 AddMessage）
        string displayName = ChatManager.Instance.GetPlayerName(id);
        if (string.IsNullOrEmpty(displayName))
            displayName = $"玩家{id}"; // 兜底显示，避免 menu 里空白

        // 5) 弹出菜单并赋值
        var menu = UIManager.Instance.Show<UIPopCharMenu>();
        menu.targetId = id;
        menu.targetName = displayName;
    }

    /// <summary>
    /// 点击发送 
    /// </summary>
    public void OnClickSend()
    {
        Debug.Log("[UIChat] OnClickSend");
        OnEndInput();
    }
    public void OnEndInput()
    {
        // 直接从 InputField 读值，不用回调参数
        string text = this.chatText.text;

        if (!string.IsNullOrWhiteSpace(text))
        {
            ChatManager.Instance.SendChat(text, ChatManager.Instance.PrivateID, ChatManager.Instance.PrivateName);
        }

        this.chatText.text = "";
        this.chatText.ActivateInputField(); // 继续保持输入状态（可选但推荐）
    }

    public void OnSendChannelChanged(int idx)
    {
        // 不能在all（也就是“综合”）中发消息，Dropdown中也没有“综合”选项，因此idx + 1表示偏移量
        if (ChatManager.Instance.sendChannel == (ChatManager.LocalChannel)(idx + 1))
            return;

        // 校验切换频道有没有出错，例如没有加入队伍就不能切换到队伍频道
        if (!ChatManager.Instance.SetSendChannel((ChatManager.LocalChannel)idx + 1))
            this.channelSelect.value = (int)ChatManager.Instance.sendChannel - 1;
        else
            this.RefreshUI();
    }


}
