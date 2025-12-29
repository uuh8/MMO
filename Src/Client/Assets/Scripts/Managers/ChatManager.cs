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
        ChatChannel.Local | ChatChannel.World | ChatChannel.Guild | ChatChannel.Team | ChatChannel.Private | ChatChannel.System, // all
        ChatChannel.Local,  // 1 Local
        ChatChannel.World,  // 2 World
        ChatChannel.Team,   // 3 Team   
        ChatChannel.Guild,  // 4 Guild  
        ChatChannel.Private // 5 Private
    };

    // 当前本地所有聊天信息
    public List<ChatMessage>[] Messages = new List<ChatMessage>[6]
    {
        new List<ChatMessage>(),    // all
        new List<ChatMessage>(),    // local
        new List<ChatMessage>(),    // world
        new List<ChatMessage>(),    // guild
        new List<ChatMessage>(),    // team
        new List<ChatMessage>()     // private
    };

    // 角色ID -> 角色名 缓存
    // 目的：HyperText.LinkInfo 没有 Text，所以点击时只能拿到 id，名字要靠我们自己保存。
    private readonly Dictionary<int, string> _playerNameCache = new Dictionary<int, string>();

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
    /// 写入/更新缓存。任何收到的聊天消息都可以顺手更新一次。
    /// </summary>
    private void CachePlayerName(int id, string name)
    {
        if (id <= 0) return;
        if (string.IsNullOrEmpty(name)) return;

        // 直接覆盖即可：同一个 id 以后可能改名
        _playerNameCache[id] = name;
    }
    /// <summary>
    /// 给外部用：通过 id 取名字。取不到就返回空串。
    /// </summary>
    public string GetPlayerName(int id)
    {
        return _playerNameCache.TryGetValue(id, out var name) ? name : "";
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
        if (channel == LocalChannel.Team)
        {
            if (User.Instance.TeamInfo == null)
            {
                AddSystemMessage("你没有加入任何队伍");
                return false;
            }
        }
        else if (channel == LocalChannel.Guild)
        {
            if (User.Instance.CurrentCharacter.Guild == null)
            {
                AddSystemMessage("你没有加入任何公会");
                return false;
            }
        }

        this.sendChannel = channel;
        Debug.LogFormat("[ChatManager] SetChannel:{0}", this.sendChannel);
        return true;
    }

    internal void AddMessages(ChatChannel channel, List<ChatMessage> messages)
    {
        // 缓存角色名：保证点击链接时能通过 id 找到 name
        foreach (var m in messages)
            CachePlayerName(m.FromId, m.FromName);

        for (int ch = 0; ch < 6; ch++)
        {
            if ((this.ChannelFilter[ch] & channel) != 0)
            {
                this.Messages[ch].AddRange(messages);
            }
        }

        this.OnChat?.Invoke();
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
            sb.Append(FormatMessage(message));
            sb.Append('\n');
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
        string name = FormatFromPlayer(message);
        string body = Escape(message.Message);

        switch (message.Channel)
        {
            case ChatChannel.Local:
                return $"[本地]{name}: {body}";
            case ChatChannel.World:
                return $"<color=cyan>[世界]{name}: {body}</color>";
            case ChatChannel.System:
                return $"<color=yellow>[系统]{body}</color>";
            case ChatChannel.Private:
                return $"<color=magenta>[私聊]{name}: {body}</color>";
            case ChatChannel.Team:
                return $"<color=green>[队伍]{name}: {body}</color>";
            case ChatChannel.Guild:
                return $"<color=blue>[公会]{name}: {body}</color>";
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
        // 自己发的消息：你可以继续显示 [我]
        if (message.FromId == User.Instance.CurrentCharacter.Id)
        {
            // 注意：name 仍然给一个可解析的 id（例如用真实 id 更好）
            // 用真实 id 的好处：点“我”也能弹菜单（可选）
            int myId = User.Instance.CurrentCharacter.Id;
            return $"<a name=\"c:{myId}\" class=\"player\">[我]</a>";

        }

        // 其他玩家：name 只放 id，显示文本仍显示名字（但要 Escape 防止富文本注入）
        return $"<a name=\"c:{message.FromId}\" class=\"player\">[{Escape(message.FromName)}]</a>";
    }
    /// <summary>
    /// 对名字和 message 内容做最基本的转义
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) 
            return "";

        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }

}
