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
    class TeamService : Singleton<TeamService>
    {
        public TeamService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamInviteRequest>(this.OnTeamInviteRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamInviteResponse>(this.OnTeamInviteResponse);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<TeamLeaveRequest>(this.OnTeamLeave); 
        }

        public void Init()
        {
            TeamManager.Instance.Init();
        }

        #region 客户端消息处理
        /// <summary>
        /// 收到组队请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamInviteRequest(NetConnection<NetSession> sender, TeamInviteRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[TeamService] OnTeamInviteRequest: FromId:{0} FromName:{1} ToID:{2} ToName:{3}", request.FromId, request.FromName, request.ToId, request.ToName);

            NetConnection<NetSession> target = SessionManager.Instance.GetSession(request.ToId);
            // 校验好友是否在线
            if(target == null)
            {
                sender.Session.Response.teamInviteRes = new TeamInviteResponse();
                sender.Session.Response.teamInviteRes.Result = Result.Failed;
                sender.Session.Response.teamInviteRes.Errormsg = "好友不在线";
                sender.SendResponse();
                return;
            }

            // 校验好友是否已经有Team了
            if(target.Session.Character.Team != null)
            {
                sender.Session.Response.teamInviteRes = new TeamInviteResponse();
                sender.Session.Response.teamInviteRes.Result = Result.Failed;
                sender.Session.Response.teamInviteRes.Errormsg = "对方已经有队伍";
                sender.SendResponse();
                return;
            }

            // 转发请求给目标好友
            target.Session.Response.teamInviteReq = request;
            target.SendResponse();
        }
        /// <summary>
        /// 收到组队响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamInviteResponse(NetConnection<NetSession> sender, TeamInviteResponse response)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[TeamService] OnTeamInviteResponse: character:{0} Result:{1} FromId:{2} ToId:{3}", character.Id, response.Result, response.Request.FromId, response.Request.ToId);

            // 先保证：给B也有一个回包可发（否则 response 为空，GetResponse 直接 null）
            sender.Session.Response.teamInviteRes = response;

            // 接受了组队请求
            if (response.Result == Result.Success)
            {
                // 将消息结果返回给组队发起者
                var requester = SessionManager.Instance.GetSession(response.Request.FromId);
                // 校验，例如requester中途下线
                if (requester == null)
                {
                    sender.Session.Response.teamInviteRes.Result = Result.Failed;
                    sender.Session.Response.teamInviteRes.Errormsg = "请求者已下线";
                }
                else
                {
                    TeamManager.Instance.AddTeamMember(requester.Session.Character, character);
                    // 客户端A 的回包
                    requester.Session.Response.teamInviteRes = response;
                    requester.SendResponse();   // 发给请求者
                }
            }
            // B 的回包
            sender.SendResponse();  // 发回好友自身
        }
        /// <summary>
        /// 离开队伍响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        public void OnTeamLeave(NetConnection<NetSession> sender, TeamLeaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[TeamService] OnTeamLeave: character:{0} TeamId:{1} :{2}", character.Id, request.TeamId, request.characterId);

            sender.Session.Response.teamLeave = new TeamLeaveResponse();
            sender.Session.Response.teamLeave.Result = Result.Success;
            sender.Session.Response.teamLeave.characterId = request.characterId;

            character.Team.Leave(character);

            sender.SendResponse();
            return;
        }
        #endregion
    }
}
