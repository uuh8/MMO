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
    // 本地频道枚举：用于 UI 显示和过滤，与协议层的 ChatChannel 分开定义
    public enum LocalChannel
    {
        all = 0,    // 综合（显示所有频道消息）
        Local = 1,  // 本地
        World = 2,  // 世界
        Team = 3,   // 队伍
        Guild = 4,  // 公会
        Private = 5,// 私聊
    }

    // 每个 LocalChannel 对应一个 ChatChannel 位掩码
    // "综合"频道用按位或把所有频道都包含进来，其他频道只对应自己
    private ChatChannel[] ChannelFilter = new ChatChannel[6]
    {
        ChatChannel.Local | ChatChannel.World | ChatChannel.Guild | ChatChannel.Team | ChatChannel.Private | ChatChannel.System,
        ChatChannel.Local,
        ChatChannel.World,
        ChatChannel.Team,
        ChatChannel.Guild,
        ChatChannel.Private
    };

    // 二维消息列表：第一维是 LocalChannel 索引，第二维是该频道的消息列表
    // 同一条消息可能被写入多个频道（例如本地消息同时写入 all 和 Local）
    public List<ChatMessage>[] Messages = new List<ChatMessage>[6]
    {
        new List<ChatMessage>(),    // all
        new List<ChatMessage>(),    // local
        new List<ChatMessage>(),    // world
        new List<ChatMessage>(),    // guild
        new List<ChatMessage>(),    // team
        new List<ChatMessage>()     // private
    };

    public LocalChannel displayChannel;  // 当前 UI 显示的频道
    public LocalChannel sendChannel;     // 当前发送消息使用的频道

    public int PrivateID = 0;       // 私聊目标的角色 ID
    public string PrivateName = ""; // 私聊目标的角色名

    // 消息刷新事件：有新消息到达或频道切换时触发，UIChat 订阅此事件刷新显示
    public Action OnChat { get; internal set; }

    // LocalChannel -> 协议层 ChatChannel 的转换属性
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

    /// <summary>
    /// 初始化：每次进入游戏时清空历史聊天记录
    /// </summary>
    public void Init()
    {
        foreach (var messages in this.Messages)
            messages.Clear();
    }

    /// <summary>
    /// 发起私聊：设置私聊目标并切换到私聊频道
    /// </summary>
    internal void StartPrivateChat(int targetId, string targetName)
    {
        this.PrivateID = targetId;
        this.PrivateName = targetName;
        this.sendChannel = LocalChannel.Private;
        this.OnChat?.Invoke();
    }

    /// <summary>
    /// 向服务器发送聊天请求
    /// </summary>
    public void SendChat(string content, int toId = 0, string toName = "")
    {
        ChatService.Instance.SendChat(this.SendChannel, content, toId, toName);
    }

    /// <summary>
    /// 切换发送频道，切换前校验条件（如切换队伍频道前需已入队）
    /// 返回 false 表示切换失败
    /// </summary>
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
        return true;
    }

    /// <summary>
    /// 收到服务器推送的批量消息后调用
    /// 根据 ChannelFilter 位掩码，把消息分发写入对应的频道列表
    /// 同一条消息会被写入所有匹配的频道（包括"综合"）
    /// </summary>
    internal void AddMessages(ChatChannel channel, List<ChatMessage> messages)
    {
        for (int ch = 0; ch < 6; ch++)
        {
            // 位与运算：判断该频道是否在当前消息的 channel 掩码中
            if ((this.ChannelFilter[ch] & channel) != 0)
                this.Messages[ch].AddRange(messages);
        }

        // 通知 UI 刷新
        this.OnChat?.Invoke();
    }

    /// <summary>
    /// 添加系统消息（本地生成，不走网络）
    /// 只写入"综合"频道，不分发到其他频道
    /// </summary>
    public void AddSystemMessage(string message, string from = "")
    {
        this.Messages[(int)LocalChannel.all].Add(new ChatMessage()
        {
            Channel = ChatChannel.System,
            Message = message,
            FromName = from
        });
        this.OnChat?.Invoke();
    }

    /// <summary>
    /// 获取当前显示频道的所有消息，拼装成富文本字符串供 UI 显示
    /// 使用 StringBuilder 避免字符串拼接产生大量 GC Alloc
    /// </summary>
    public string GetCurrentMessages()
    {
        StringBuilder sb = new StringBuilder();
        var msgs = this.Messages[(int)displayChannel];
        for (int i = 0; i < msgs.Count; i++)
        {
            sb.Append(FormatMessage(msgs[i]));
            if (i < msgs.Count - 1)
                sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// 将一条 ChatMessage 格式化为带颜色标记的富文本字符串
    /// 不同频道用不同颜色区分
    /// </summary>
    private string FormatMessage(ChatMessage message)
    {
        string name = FormatFromPlayer(message);
        string body = Escape(message.Message);

        switch (message.Channel)
        {
            case ChatChannel.Local:
                return $"【本地】{name}: {body}";
            case ChatChannel.World:
                return $"<color=cyan>【世界】{name}: {body}</color>";
            case ChatChannel.System:
                return $"<color=yellow>【系统】{body}</color>";
            case ChatChannel.Private:
                return $"<color=magenta>【私聊】{name}: {body}</color>";
            case ChatChannel.Team:
                return $"<color=green>【队伍】{name}: {body}</color>";
            case ChatChannel.Guild:
                return $"<color=blue>【公会】{name}: {body}</color>";
        }
        return "";
    }

    /// <summary>
    /// 格式化发送者名字
    /// 自己发的消息显示【我】，其他玩家显示其角色名
    /// </summary>
    private string FormatFromPlayer(ChatMessage message)
    {
        if (message.FromId == User.Instance.CurrentCharacter.Id)
            return "【我】";

        // Escape 防止玩家名里含有 < > 等字符破坏富文本格式
        return $"【{Escape(message.FromName)}】";
    }

    /// <summary>
    /// 对字符串做基础 HTML 转义
    /// 防止玩家名或消息内容里的特殊字符破坏 Unity 富文本标签
    /// </summary>
    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }
}