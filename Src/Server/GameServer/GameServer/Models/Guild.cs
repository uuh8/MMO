using Common;
using GameServer.Entities;
using GameServer.Managers;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class Guild
    {
        public int Id 
        { 
            get { return this.Data.Id; } 
        } 
        public string Name 
        { 
            get { return this.Data.Name; } 
        }

        public Character Leader;
        public List<Character> Members = new List<Character>();
        public int timestamp;   // 使用时间戳表示公会信息发、变化的时间，以此来触发后处理 PostProcess
        public TGuild Data;     // 存一下公会的数据库信息在Manager，避免频繁查询数据库
        public Guild(TGuild guild)
        {
            this.Data = guild;
        }


        internal bool JoinApply(NGuildApplyInfo apply)
        {
            // 校验是否申请过，一个角色只能申请一次
            var oldApply = this.Data.TGuildApplies.FirstOrDefault(v => v.CharacterId == apply.characterId);
            if(oldApply != null)
            {
                return false;
            }

            // 如果没申请，就创建新申请
            var dbApply = DBService.Instance.Entities.TGuildApplies.Create();
            dbApply.GuildId = apply.GuildId;
            dbApply.CharacterId = apply.characterId;
            dbApply.Name = apply.Name;
            dbApply.Class = apply.Class;
            dbApply.Level = apply.Level;
            dbApply.ApplyTime = DateTime.Now;

            // 添加进数据库
            DBService.Instance.Entities.TGuildApplies.Add(dbApply);
            this.Data.TGuildApplies.Add(dbApply);
            // 保存数据库
            DBService.Instance.Save();

            this.timestamp = TimeUtil.timestamp;
            return true;
        }

        internal bool JoinApprove(NGuildApplyInfo apply)
        {
            // 校验是否申请过，一个角色只能申请一次(v.Result == 0 表示还没申请过)
            var oldApply = this.Data.TGuildApplies.FirstOrDefault(v => v.CharacterId == apply.characterId && v.Result == 0);
            if (oldApply != null)
            {
                return false;
            }

            oldApply.Result = (int)apply.Result;

            if(apply.Result == ApplyResult.Accept)
            {
                this.AddMember(apply.characterId, apply.Name, apply.Class, apply.Level, GuildTitle.None);
            }

            // 保存数据库
            DBService.Instance.Save();

            this.timestamp = TimeUtil.timestamp;
            return true;
        }

        public void AddMember(int id, string name, int @class, int level, GuildTitle title)
        {
            // 新创建一个成员加入Members
            DateTime now = DateTime.Now;
            TGuildMember dbMember = new TGuildMember()
            {
                CharacterId = id,
                Name = name,
                Class = @class,
                Level = level,
                Title = (int)title,
                JoinTime = now,
                LastTime = now
            };
            this.Data.TGuildMembers.Add(dbMember);
            timestamp = TimeUtil.timestamp;
        }

        public void Leave(Character member)
        {
            /*Log.InfoFormat("[Guild] Leave Guild: {0}:{1}", member.Id, member.Info.Name);
            this.Members.Remove(member);
            if(member == this.Leader)
            {
                if (this.Members.Count > 0)
                    this.Leader = this.Members[0];
                else
                    this.Leader = null;
            }
            member.Guild = null;
            timestamp = TimeUtil.timestamp;*/
        }

        /// <summary>
        /// 后处理
        /// </summary>
        /// <param name="message"></param>
        public void PostProcess(Character from ,NetMessageResponse message)
        {
            // 后处理校验公会是不是null
            if (message.Guild == null)
            {
                // 如果没有公会，填充一下
                message.Guild = new GuildResponse();
                message.Guild.Result = Result.Success;
                message.Guild.guildInfo = this.GuildInfo(from);
            }
        }

        internal NGuildInfo GuildInfo(Character from)
        {
            NGuildInfo info = new NGuildInfo()
            {
                Id = this.Id,
                GuildName = this.Name,
                Notice = this.Data.Notice,
                leaderId = this.Data.LeaderID,
                leaderName = this.Data.LeaderName,
                createTime = (long)TimeUtil.GetTimestamp(this.Data.CreateTime),
                memberCount = this.Data.TGuildMembers.Count
            };

            // 只有当前公会的成员来请求成员信息的时候，才返回公会成员信息(只有公会成员才能查看工会信息)
            if(from != null)
            {
                info.Members.AddRange(GetMemberInfos());
                // 只有申请人是会长的时候，才会把发 “申请信息”（只有会长能审批申请信息）
                if(from.Id == this.Data.LeaderID)
                {
                    info.Applies.AddRange(GetApplyInfos());
                }
            }

            return info;
        }

        /// <summary>
        /// 从数据库把数据库信息转变为网络信息
        /// </summary>
        /// <returns></returns>
        private List<NGuildMemberInfo> GetMemberInfos()
        {
            List<NGuildMemberInfo> members = new List<NGuildMemberInfo>();

            // 遍历数据库里的信息
            foreach(var member in this.Data.TGuildMembers)
            {
                var memberInfo = new NGuildMemberInfo
                {
                    Id = member.Id,
                    characterId = member.CharacterId,
                    Title = (GuildTitle)member.Title,
                    joinTime = (long)TimeUtil.GetTimestamp(member.JoinTime),
                    lastTime = (long)TimeUtil.GetTimestamp(member.LastTime),
                };

                // 校验成员在不在线
                var character = CharacterManager.Instance.GetCharacter(member.CharacterId);
                if(character != null)
                {
                    // 成员在线，公会更新该成员的信息刷新在列表中，并设置Status为1
                    memberInfo.Info = character.GetBasicInfo();
                    memberInfo.Status = 1;
                    member.Level = character.Data.Level;
                    member.Name = character.Data.Name;
                    member.LastTime = DateTime.Now;
                    if(member.Id == this.Data.LeaderID)
                    {
                        this.Leader = character;
                    }
                }
                else
                {
                    // 设置Status为0
                    memberInfo.Info = this.GetMemberInfo(member);
                    memberInfo.Status = 0;
                    if (member.Id == this.Data.LeaderID)
                    {
                        this.Leader = null;
                    }
                }

                members.Add(memberInfo);
            }
            return members;
        }
        private NCharacterInfo GetMemberInfo(TGuildMember member)
        {
            return new NCharacterInfo()
            {
                Id = member.CharacterId,
                Name = member.Name,
                Class = (CharacterClass)member.Class,
                Level = member.Level
            };
        }

        /// <summary>
        /// 数据库信息转变为网络信息
        /// </summary>
        /// <returns></returns>
        private List<NGuildApplyInfo> GetApplyInfos()
        {
            List<NGuildApplyInfo> applies = new List<NGuildApplyInfo>();

            // 遍历数据库里的信息
            foreach (var apply in this.Data.TGuildApplies)
            {
                applies.Add(new NGuildApplyInfo()
                {
                    characterId = apply.CharacterId,
                    GuildId = apply.CharacterId,
                    Class = apply.Class,
                    Level = apply.Level,
                    Name = apply.Name,
                    Result = (ApplyResult)apply.Result,
                });
            }
            return applies;
        }

    }
}
