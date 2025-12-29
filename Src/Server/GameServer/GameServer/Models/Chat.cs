using GameServer.Entities;
using GameServer.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    /// <summary>
    /// 每个 Chat 针对当前玩家(owner)
    /// </summary>
    class Chat
    {
        Character Owner;

        public int localIdx;
        public int worldIdx;
        public int systemIdx;
        public int teamIdx;
        public int guildIdx;

        public Chat(Character owner)
        {
            this.Owner = owner;
        }

        public void PostProcess(NetMessageResponse message)
        {
            // 先用临时列表收集“新增消息”
            var locals = new List<ChatMessage>();
            var worlds = new List<ChatMessage>();
            var systems = new List<ChatMessage>();
            var teams = new List<ChatMessage>();
            var guilds = new List<ChatMessage>();
            var privs = new List<ChatMessage>();

            // 本地/世界/系统
            this.localIdx = ChatManager.Instance.GetLocalMessages(this.Owner.Info.mapId, this.localIdx, locals);
            this.worldIdx = ChatManager.Instance.GetWorldMessages(this.worldIdx, worlds);
            this.systemIdx = ChatManager.Instance.GetSystemMessages(this.systemIdx, systems);

            // 队伍
            if (this.Owner.Team != null)
                this.teamIdx = ChatManager.Instance.GetTeamMessages(this.Owner.Team.Id, this.teamIdx, teams);

            // 公会
            if (this.Owner.Guild != null)
                this.guildIdx = ChatManager.Instance.GetGuildMessages(this.Owner.Guild.Id, this.guildIdx, guilds);

            // 私聊（如果你们实现了私聊池/idx，这里也一样取）
            // this.privIdx = ...

            // 没有任何增量：不创建 ChatResponse，真正做到“顺便但不打扰”
            bool hasAny = locals.Count > 0 || worlds.Count > 0 || systems.Count > 0
                       || teams.Count > 0 || guilds.Count > 0 || privs.Count > 0;
            if (!hasAny) return;

            // 有增量才创建/附加
            if (message.Chat == null)
                message.Chat = new ChatResponse();

            message.Chat.Result = Result.Success;
            message.Chat.localMessages.AddRange(locals);
            message.Chat.worldMessages.AddRange(worlds);
            message.Chat.systemMssages.AddRange(systems); // 注意下面第2点拼写
            message.Chat.teamMessages.AddRange(teams);
            message.Chat.guildMessages.AddRange(guilds);
            message.Chat.privateMessages.AddRange(privs);
        }

    }
}
