using Models;
using Services;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ChatManager : Singleton<ChatManager>
{
    public enum LocalChannel
    {
        all = 0,    // 综合
        Local = 1,  // 本地
        World = 2,  // 世界
        Team = 3,  // 队伍
        Guild = 4,  // 公会
        Private = 5,  // 私聊
    }
    private ChatChannel[] ChannelFilter = new ChatChannel[6]
    {
        ChatChannel.Local | ChatChannel.World | ChatChannel.Guild | ChatChannel.Team | ChatChannel.Private | ChatChannel.System,    // all频道包含所有
        ChatChannel.Local,
        ChatChannel.World,
        ChatChannel.Guild,
        ChatChannel.Team,
        ChatChannel.Private
    };

    // 当前本地所有聊天信息
    public List<ChatMessage>[] Messages = new List<ChatMessage>[6]
    {
        new List<ChatMessage>(),
        new List<ChatMessage>(),
        new List<ChatMessage>(),
        new List<ChatMessage>(),
        new List<ChatMessage>(),
        new List<ChatMessage>()
    };

    public LocalChannel displayChannel;

    public LocalChannel sendChannel;

    public int PrivateID = 0;
    public string PrivateName = "";

    // UIChat 中注册了OnChat，一旦调用 OnChat 就会触发RefreshUI
    public Action OnChat { get; internal set; }

    public ChatChannel SendChannel
    {
        get
        {
            switch (sendChannel)
            {
                case LocalChannel.Local: return ChatChannel.Local;
                case LocalChannel.World: return ChatChannel.World;
                case LocalChannel.Guild: return ChatChannel.Guild;
                case LocalChannel.Team: return ChatChannel.Team;
                case LocalChannel.Private: return ChatChannel.Private;
            }
            return ChatChannel.Local;
        }
    }
    public void Init()
    {
        // 每次启动的时候聊天记录清空
        foreach (var messages in this.Messages)
        {
            messages.Clear();
        }
    }

    /// <summary>
    /// 发起私聊
    /// </summary>
    /// <param name="targetId"></param>
    /// <param name="targetName"></param>
    internal void StartPrivateChat(int targetId, string targetName)
    {
        this.PrivateID = targetId;
        this.PrivateName = targetName;

        // 设置频道为私聊频道
        this.sendChannel = LocalChannel.Private;
        // 刷新界面
        if (this.OnChat != null)
        {
            this.OnChat();
        }
    }

    public void SendChat(string content, int toId = 0, string toName = "")
    {
        ChatService.Instance.SendChat(this.SendChannel, content, toId, toName);
    }

    public bool SetSendChannel(LocalChannel channel)
    {
        if(channel == LocalChannel.Team)
        {
            if(User.Instance.TeamInfo == null)
            {
                this.AddSystemMessage("你没有加入任何队伍");
                return false;
            }
            if (User.Instance.CurrentCharacter.Guild == null)
            {
                this.AddSystemMessage("你没有加入任何公会");
                return false;
            }
        }
        // 没问题,则设置为所选择的频道
        this.sendChannel = channel;
        Debug.LogFormat("[ChatManager] SetChannel:{0}", this.sendChannel);
        return true;
    }

    internal void AddMessages(ChatChannel channel, List<ChatMessage> messages)
    {
        for(int ch = 0; ch < 6; ch++)
        {
            if ((this.ChannelFilter[ch] & channel) == channel)
            {
                this.Messages[ch].AddRange(messages);
            }
        }
        if (this.OnChat != null)
            this.OnChat();
    }

    /// <summary>
    /// 添加消息到Messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="from"></param>
    public void AddSystemMessage(string message, string from = "")
    {
        this.Messages[(int)LocalChannel.all].Add(new ChatMessage()
        {
            Channel = ChatChannel.System,
            Message = message,
            FromName = from
        });
        if (this.OnChat != null)
            this.OnChat();
    }

    /// <summary>
    /// 获取所有消息
    /// </summary>
    /// <returns></returns>
    public string GetCurrentMessages()
    {
        StringBuilder sb = new StringBuilder();
        foreach(var message in this.Messages[(int)displayChannel])
        {
            sb.AppendLine(FormatMessage(message));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 格式化 Message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private string FormatMessage(ChatMessage message)
    {
        switch (message.Channel)
        {
            case ChatChannel.Local:
                return string.Format("[本地]{0}{1}", FormatFromPlayer(message), message.Message);
            case ChatChannel.World:
                return string.Format("<color=cyan>[世界]{0}{1}</color>", FormatFromPlayer(message), message.Message);
            case ChatChannel.System:
                return string.Format("<color=yellow>[系统]{0}</color>", message.Message);
            case ChatChannel.Private:
                return string.Format("<color=magenta>[私聊]{0}{1}</color>", FormatFromPlayer(message), message.Message);
            case ChatChannel.Team:
                return string.Format("<color=green>[队伍]{0}{1}</color>", FormatFromPlayer(message), message.Message);
            case ChatChannel.Guild:
                return string.Format("<color=blue>[公会]{0}{1}</color>", FormatFromPlayer(message), message.Message);
        }
        return "";
    }
    /// <summary>
    /// 自己和其他玩家发出来的消息显示出区分
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private string FormatFromPlayer(ChatMessage message)
    {
        if(message.FromId == User.Instance.CurrentCharacter.Id)
        {
            return "<a name=\"\" class=\"player\">[我]</a>";
        }
        else
        {
            return string.Format("<a name=\"c:{0}:{1}\" class=\"player\">[{1}]</a>", message.FromId, message.FromName);
        }
    }
}
