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
    /// <summary>
    /// Guild 领域模型（Domain Model）
    ///
    /// 可以把它理解成：
    /// - Service：网络入口/用例编排（收包、鉴权、回包、决定调用谁）
    /// - Manager：内存索引/对象仓库（GetGuild(id) / 缓存所有 Guild）
    /// - Model(Guild)：这一个“公会”本身，负责公会的业务规则与状态变化
    ///
    /// 注意：你当前实现属于“折中方案”——Model 里直接操作 EF Entity 并 Save。
    /// 更严格的分层会把 Save 交给 Service/Repository，但现阶段可先这样跑通。
    /// </summary>
    class Guild
    {
        /// <summary>
        /// 公会ID/Name：直接来自数据库实体 Data
        /// </summary>
        public int Id 
        { 
            get { return this.Data.Id; } 
        } 
        public string Name 
        { 
            get { return this.Data.Name; } 
        }

        /// <summary>
        /// 用时间戳来表达“公会信息发生过变化”，以此来触发后处理 PostProcess
        /// 客户端通过对比时间戳做 UI 后处理/增量刷新（避免每次都全量同步）。
        /// </summary>
        public int timestamp;

        /// <summary>
        /// Data 是 EF Entity（数据库行/表关系的投影）。
        /// GuildManager 会把 Data 缓存在内存里，避免频繁查库。
        /// 重要：Data.TGuildMembers / Data.TGuildApplies 是导航集合。
        /// 如果这些集合没被 Include 加载，Count/遍历可能为空或触发延迟加载。
        /// </summary>
        public TGuild Data;

        public Guild(TGuild guild)
        {
            this.Data = guild;
        }

        // --------------------------------------------------------------------
        // 1) 申请入会：写入申请表（TGuildApply），并更新内存数据结构
        // --------------------------------------------------------------------
        /// <summary>
        /// 加入公会申请
        /// </summary>
        /// <param name="apply"></param>
        /// <returns></returns>
        internal bool JoinApply(NGuildApplyInfo apply)
        {
            // 校验是否申请过，一个角色只能申请一次
            var oldApply = this.Data.TGuildApplies.FirstOrDefault(v => v.CharacterId == apply.characterId);
            if(oldApply != null)
            {
                return false;
            }

            // 创建数据库实体：TGuildApply
            var dbApply = DBService.Instance.Entities.TGuildApplies.Create();

            dbApply.GuildId = apply.GuildId;
            dbApply.CharacterId = apply.characterId;
            dbApply.Name = apply.Name;
            dbApply.Class = apply.Class;
            dbApply.Level = apply.Level;
            dbApply.ApplyTime = DateTime.Now;

            // 写入数据库集合 + 写入当前公会的导航集合
            // 这样后续在内存 Data.TGuildApplies 里就能读到最新申请
            DBService.Instance.Entities.TGuildApplies.Add(dbApply);
            this.Data.TGuildApplies.Add(dbApply);
            // 保存数据库
            DBService.Instance.Save();

            // 标记状态变化：用于后处理（推送公会信息更新）
            this.timestamp = TimeUtil.timestamp;
            return true;
        }

        // --------------------------------------------------------------------
        // 2) 会长审批：更新申请状态；如果同意则把成员加入公会
        // --------------------------------------------------------------------
        /// <summary>
        /// 会长/管理员审批入会申请。
        /// </summary>
        /// <param name="apply"></param>
        /// <returns></returns>
        internal bool JoinApprove(NGuildApplyInfo apply)
        {
            // 校验是否申请过，一个角色只能申请一次(v.Result == 0 表示还没申请过)
            var oldApply = this.Data.TGuildApplies.FirstOrDefault(v => v.CharacterId == apply.characterId && v.Result == 0);
            if (oldApply != null)
            {
                return false;
            }

            // 把数据库里这条申请标记为 Accept/Reject
            oldApply.Result = (int)apply.Result;

            // 如果同意，真正把玩家加入公会
            if (apply.Result == ApplyResult.Accept)
            {
                this.AddMember(apply.characterId, apply.Name, apply.Class, apply.Level, GuildTitle.None);
            }

            // 保存数据库
            DBService.Instance.Save();

            this.timestamp = TimeUtil.timestamp;
            return true;
        }
        /// <summary>
        /// 把角色加入公会成员表，并同步角色 GuildId。
        /// </summary>
        public void AddMember(int characterId, string name, int @class, int level, GuildTitle title)
        {
            // 新创建一个成员加入Members
            DateTime now = DateTime.Now;
            TGuildMember dbMember = new TGuildMember()
            {
                CharacterId = characterId,
                Name = name,
                Class = @class,
                Level = level,
                Title = (int)title,
                JoinTime = now,
                LastTime = now
            };
            this.Data.TGuildMembers.Add(dbMember);

            // 同步角色 GuildId（角色表里的 GuildId 是加入公会后的“归属”）
            // - 在线：直接改内存角色的 Data（后续 Save 会写入）
            // - 离线：直接查库并更新
            var character = CharacterManager.Instance.GetCharacter(characterId);
            if (character != null)
            {
                // 角色在线
                character.Data.GuildId = this.Id;
            }
            else
            {
                // 角色不在线
                TCharacter dbCharacter = DBService.Instance.Entities.Characters.SingleOrDefault(c => c.ID == characterId);
                dbCharacter.GuildId = this.Id;
            }

            timestamp = TimeUtil.timestamp;
        }

        public void Leave(Character member)
        {
            Log.InfoFormat("[Guild] Leave Guild: {0}:{1}", member.Id, member.Info.Name);
            // TODO
            timestamp = TimeUtil.timestamp;
        }

        // --------------------------------------------------------------------
        // 3) 后处理：把公会信息填充到 NetMessageResponse 上
        // --------------------------------------------------------------------
        /// <summary>
        /// 后处理（PostProcess）
        /// 思路：你的很多 Service 回包不一定都主动携带 GuildResponse。但只要公会信息发生变化（timestamp 更新），就“顺便”把最新公会信息带回客户端，
        /// 这样客户端 UI 能靠 OnGuildUpdate 自动刷新。
        /// </summary>
        public void PostProcess(Character from ,NetMessageResponse message)
        {
            // 如果当前响应里没有 GuildResponse，就补一个
            if (message.Guild == null)
            {
                message.Guild = new GuildResponse();
                message.Guild.Result = Result.Success;
                message.Guild.guildInfo = this.GuildInfo(from);
            }
        }

        /// <summary>
        /// 组装“公会信息”网络结构（NGuildInfo）
        /// </summary>
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

            // 只有当前公会的成员来请求成员信息的时候，才返回公会成员信息(只有公会成员才能查看公会信息)
            if (from != null)
            {
                info.Members.AddRange(GetMemberInfos());

                // 只有会长才能看到申请列表（用于审批）
                if (from.Id == this.Data.LeaderID)
                {
                    info.Applies.AddRange(GetApplyInfos());
                }
            }

            return info;
        }

        // --------------------------------------------------------------------
        // 4) 数据库实体 -> 网络结构：成员列表 / 申请列表
        // --------------------------------------------------------------------
        /// <summary>
        /// 把 TGuildMember 列表转换为客户端需要的 NGuildMemberInfo 列表
        /// </summary>
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

                // 在线成员：用内存中的角色数据刷新快照，并标记 Status=1
                // 离线成员：用数据库快照组装（Name/Class/Level），标记 Status=0
                var character = CharacterManager.Instance.GetCharacter(member.CharacterId);
                if(character != null)
                {
                    // 成员在线，公会更新该成员的信息刷新在列表中，并设置Status为1
                    memberInfo.Info = character.GetBasicInfo();
                    memberInfo.Status = 1;

                    // 同步数据库快照（方便离线显示）
                    member.Level = character.Data.Level;
                    member.Name = character.Data.Name;
                    member.LastTime = DateTime.Now;
                }
                else
                {
                    // 设置Status为0
                    memberInfo.Info = this.GetMemberInfo(member);
                    memberInfo.Status = 0;
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
        /// 把 TGuildApply（申请表）转换为 NGuildApplyInfo（网络结构）
        /// 注意：这里只返回 Result==None 的“待审批”申请。
        /// </summary>
        private List<NGuildApplyInfo> GetApplyInfos()
        {
            List<NGuildApplyInfo> applies = new List<NGuildApplyInfo>();

            // 遍历数据库里的信息
            foreach (var apply in this.Data.TGuildApplies)
            {
                if (apply.Result != (int)ApplyResult.None) 
                    continue;

                applies.Add(new NGuildApplyInfo()
                {
                    characterId = apply.CharacterId,
                    GuildId = apply.GuildId,
                    Class = apply.Class,
                    Level = apply.Level,
                    Name = apply.Name,
                    Result = (ApplyResult)apply.Result,
                });
            }
            return applies;
        }

        // --------------------------------------------------------------------
        // 5) 管理命令：提升/罢免/转让/踢人 等
        // --------------------------------------------------------------------
        internal void ExecuteAdmin(GuildAdminCommand command, int targetId, int sourceId)
        {
            // 这里是直接操作“数据库快照”（Data.TGuildMembers），然后 Save。
            // 更严谨的做法是校验权限/状态机（例如只有会长能转让，副会长只能踢普通成员）。
            var target = GetDBMember(targetId);
            var source = GetDBMember(sourceId);
            switch (command)
            {
                case GuildAdminCommand.Promote:
                    // 晋升：职位设置成副会长
                    target.Title = (int)GuildTitle.VicePresident;
                    break;
                case GuildAdminCommand.Depost:
                    // 罢免：职位设置为null
                    target.Title = (int)GuildTitle.None;
                    break;
                case GuildAdminCommand.Transfer:
                    // 转让会长：
                    // - target 变会长
                    // - source（原会长）变普通成员
                    // - 更新公会表的 LeaderID/LeaderName
                    target.Title = (int)GuildTitle.VicePresident;
                    source.Title = (int)GuildTitle.None;
                    this.Data.LeaderID = targetId;
                    this.Data.LeaderName = target.Name;
                    break;
                case GuildAdminCommand.Kickout:
                    // 踢出公会：TODO
                    break;
            }
            DBService.Instance.Save();
            timestamp = TimeUtil.timestamp;
        }
        /// <summary>
        /// 从 Data.TGuildMembers 里找到指定角色的成员记录（数据库快照）
        /// </summary>
        private TGuildMember GetDBMember(int characterId)
        {
            foreach (var member in this.Data.TGuildMembers)
            {
                if (member.CharacterId == characterId)
                    return member;
            }
            return null;
        }
    }
}
