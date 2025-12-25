using GameServer.Entities;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class Team
    {
        public int Id;  // 队伍id
        public Character Leader;

        public List<Character> Members = new List<Character>();

        public double timestamp;  // 由于队伍是公用的，因此不能像好友那样使用一个bool friendChanged来触发后处理，使用时间戳代表队伍信息发生变化的时间，以此来触发后处理 PostProcess

        public Team(Character leader)
        {
            this.AddMember(leader);
        }

        public void AddMember(Character member)
        {
            if(this.Members.Count == 0)
            {
                this.Leader = member;
            }
            this.Members.Add(member);
            member.Team = this;
            timestamp = TimeUtil.timestamp; // Team添加成员，队伍信息发生改变，更新时间戳
        }
        public void Leave(Character member)
        {
            this.Members.Remove(member);
            if (member == this.Leader)
            {
                if(this.Members.Count > 0)
                {
                    this.Leader = this.Members[0];
                }
                else
                {
                    this.Leader = null;
                }
            }
            member.Team = null;
            timestamp = TimeUtil.timestamp; // Team离开成员，队伍信息发生改变，更新时间戳
        }

        /// <summary>
        /// 后处理
        /// </summary>
        /// <param name="message"></param>
        public void PostProcess(NetMessageResponse message)
        {
            if (message.teamInfo == null)
            {
                message.teamInfo = new TeamInfoResponse();
                message.teamInfo.Result = Result.Success;
                message.teamInfo.Team = new NTeamInfo();
                message.teamInfo.Team.Id = this.Id;
                message.teamInfo.Team.Leader = this.Leader.Id;
                foreach(var member in this.Members)
                {
                    message.teamInfo.Team.Members.Add(member.GetBasicInfo());
                }
            }
        }
    }
}
