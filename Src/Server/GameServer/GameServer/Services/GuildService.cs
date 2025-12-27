using Azure;
using Common;
using GameServer.Entities;
using GameServer.Managers;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    class GuildService : Singleton<GuildService>
    {
        public GuildService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildCreateRequest>(this.OnGuildCreate);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildListRequest>(this.OnGuildList);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildJoinRequest>(this.OnGuildJoinRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildJoinResponse>(this.OnGuildJoinResponse);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildLeaveRequest>(this.OnGuildLeaveRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<GuildAdminRequest>(this.OnGuildAdmin);
        }

         
        public void Init()
        {
            GuildManager.Instance.Init();
        }

        #region 客户端消息处理
        /// <summary>
        /// 收到创建公会请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnGuildCreate(NetConnection<NetSession> sender, GuildCreateRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[GuildService] OnGuildCreate: GuildName:{0} character:[{1}] {2} ", request.GuildName, character.Id, character.Name);

            sender.Session.Response.guildCreate = new GuildCreateResponse();
            // 校验时候已经加入公会
            if (character.Guild != null)
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "已经有公会";
                sender.SendResponse();
                return;
            }
            // 校验公会名称是否已存在
            if (GuildManager.Instance.CheckNameExisted(request.GuildName))
            {
                sender.Session.Response.guildCreate.Result = Result.Failed;
                sender.Session.Response.guildCreate.Errormsg = "公会名称已存在";
                sender.SendResponse();
                return;
            }

            GuildManager.Instance.CreateGuild(request.GuildName, request.GuildNotice, character);
            sender.Session.Response.guildCreate.Result = Result.Success;
            sender.Session.Response.guildCreate.guildInfo = character.Guild.GuildInfo(character);
            sender.SendResponse();
        }

        /// <summary>
        /// 请求公会列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnGuildList(NetConnection<NetSession> sender, GuildListRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[GuildService] OnGuildList: character:[{1}] {2} ", character.Id, character.Name);

            // 先保证：给B也有一个回包可发（否则 response 为空，GetResponse 直接 null）
            sender.Session.Response.guildList = new GuildListResponse();

            sender.Session.Response.guildList.Guilds.AddRange(GuildManager.Instance.GetGuildsInfo());
            sender.Session.Response.guildList.Result = Result.Success;
            sender.SendResponse();
        }

        /// <summary>
        /// 收到加入公会请求(仅会长/管理员)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        public void OnGuildJoinRequest(NetConnection<NetSession> sender, GuildJoinRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[GuildService] OnGuildJoinRequest: GuildId:{0} character:[{1}] {2} ", request.Apply.GuildId, request.Apply.characterId, request.Apply.Name);

            var guild = GuildManager.Instance.GetGuild(request.Apply.GuildId);
            if(guild == null)
            {
                sender.Session.Response.guildJoinRes = new GuildJoinResponse();
                sender.Session.Response.guildJoinRes.Result = Result.Success;
                sender.Session.Response.guildJoinRes.Errormsg = "公会不存在";
                sender.SendResponse();
                return;
            }
            request.Apply.characterId = character.Data.ID;
            request.Apply.Name = character.Data.Name;
            request.Apply.Class = character.Data.Class;
            request.Apply.Level = character.Data.Level;

            if (guild.JoinApply(request.Apply))
            {
                var leader = SessionManager.Instance.GetSession(guild.Data.LeaderID);
                // 校验会长是否在线
                if(leader != null)
                {
                    // 给会长发送申请加入的请求
                    leader.Session.Response.guildJoinReq = request;
                    leader.SendResponse();
                }
                else
                {
                    sender.Session.Response.guildJoinRes = new GuildJoinResponse();
                    sender.Session.Response.guildJoinRes.Result = Result.Success;
                    sender.Session.Response.guildJoinRes.Errormsg = "请勿重复申请";
                    sender.SendResponse();
                }
            }
        }
        /// <summary>
        /// 收到了会长/管理员的回复，服务端负责把“会长审批结果”落地
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        public void OnGuildJoinResponse(NetConnection<NetSession> sender, GuildJoinResponse response)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[GuildService] OnGuildJoinRequest: GuildId:{0} character:[{1}] {2} ", response.Apply.GuildId, response.Apply.characterId, response.Apply.Name);

            var guild = GuildManager.Instance.GetGuild(response.Apply.GuildId);
            if (response.Result == Result.Success)
            {
                // 让 Guild 模型去完成“更新申请状态、同意则加入成员、保存数据库、更新 timestamp”等一整套状态变化。
                guild.JoinApprove(response.Apply);
            }

            // 校验申请者还在不在线,在线才发消息
            var requester = SessionManager.Instance.GetSession(response.Apply.characterId);
            if(requester != null)
            {
                requester.Session.Character.Guild = guild;

                requester.Session.Response.guildJoinRes = response;
                requester.Session.Response.guildJoinRes.Result = Result.Success;
                requester.Session.Response.guildJoinRes.Errormsg = "加入公会成功";
                requester.SendResponse();
            }
        }
        /// <summary>
        /// 离开公会
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        public void OnGuildLeaveRequest(NetConnection<NetSession> sender, GuildLeaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[GuildService] OnGuildLeaveRequest: character:{0} ", character.Id);

            sender.Session.Response.guildLeave = new GuildLeaveResponse();
            sender.Session.Response.guildLeave.Result = Result.Success;

            character.Guild.Leave(character);

            DBService.Instance.Save();

            sender.SendResponse();
        }

        /// <summary>
        /// 工会管理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnGuildAdmin(NetConnection<NetSession> sender, GuildAdminRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[GuildService] OnGuildAdmin: characterId:{0}", character.Id);
            sender.Session.Response.guildAdmin = new GuildAdminResponse();

            // 校验当前角色有没有公会
            if (character.Guild == null)
            {
                sender.Session.Response.guildAdmin.Result = Result.Failed;
                sender.Session.Response.guildAdmin.Errormsg = "[GuildService] 你没公会可管理";
                sender.SendResponse();
                return;
            }

            character.Guild.ExecuteAdmin(request.Command, request.Target, character.Id);

            // 校验目标在不在线
            var target = SessionManager.Instance.GetSession(request.Target);
            if (target == null)
            {
                target.Session.Response.guildAdmin = new GuildAdminResponse();
                target.Session.Response.guildAdmin.Result = Result.Success;
                target.Session.Response.guildAdmin.Command = request;
                target.SendResponse();
                return;
            }

            target.Session.Response.guildAdmin.Result = Result.Success;
            target.Session.Response.guildAdmin.Command = request;
            target.SendResponse();
        }

        #endregion
    }
}
