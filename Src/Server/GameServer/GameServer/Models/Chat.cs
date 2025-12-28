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
            if(message.Chat == null)
            {
                message.Chat = new ChatResponse();
                message.Chat.Result = Result.Success;
            }
            // 本地
            this.localIdx = ChatManager.Instance.GetLocalMessages(this.Owner.Info.mapId, this.localIdx, message.Chat.localMessages);
            // 世界
            this.worldIdx = ChatManager.Instance.GetWorldMessages(this.worldIdx, message.Chat.worldMessages);
            // 系统
            this.systemIdx = ChatManager.Instance.GetSystemMessages(this.systemIdx, message.Chat.systemMssages);
            // 队伍
            if (this.Owner.Team != null)
            {
                this.teamIdx = ChatManager.Instance.GetTeamMessages(this.Owner.Team.Id, this.teamIdx, message.Chat.teamMessages);
            }
            // 公会
            if (this.Owner.Guild != null)
            {
                this.guildIdx = ChatManager.Instance.GetTeamMessages(this.Owner.Guild.Id, this.guildIdx, message.Chat.teamMessages);
            }
        }
    }
}
