using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Services
{
    class TeamService : Singleton<TeamService>, IDisposable
    {
        public void Init()
        {

        }

        public TeamService()
        {
            MessageDistributer.Instance.Subscribe<TeamInviteRequest>(this.OnTeamInviteRequest);
            MessageDistributer.Instance.Subscribe<TeamInviteResponse>(this.OnTeamInviteResponse);
            MessageDistributer.Instance.Subscribe<TeamInfoResponse>(this.OnTeamInfo);
            MessageDistributer.Instance.Subscribe<TeamLeaveResponse>(this.OnTeamLeave);
        }

        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<TeamInviteRequest>(this.OnTeamInviteRequest);
            MessageDistributer.Instance.Unsubscribe<TeamInviteResponse>(this.OnTeamInviteResponse);
            MessageDistributer.Instance.Unsubscribe<TeamInfoResponse>(this.OnTeamInfo);
            MessageDistributer.Instance.Unsubscribe<TeamLeaveResponse>(this.OnTeamLeave);
        }

        #region 消息发送
        /// <summary>
        /// 发送组队邀请(邀请好友加入自己Team)
        /// </summary>
        /// <param name="friendId"></param>
        /// <param name="friendName"></param>
        public void SendTeamInviteRequest(int friendId, string friendName)
        {
            Debug.Log("[TeamService] SendTeamInviteRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.teamInviteReq = new TeamInviteRequest();
            message.Request.teamInviteReq.FromId = User.Instance.CurrentCharacter.Id;
            message.Request.teamInviteReq.FromName = User.Instance.CurrentCharacter.Name;
            message.Request.teamInviteReq.ToId = friendId;
            message.Request.teamInviteReq.ToName = friendName;
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 收到其他玩家发来的组队邀请后，根据“接受”还是“拒绝”发回去
        /// </summary>
        /// <param name="id"></param>
        /// <param name="friendId"></param>
        public void SendTeamInviteResponse(bool accept, TeamInviteRequest request)
        {
            Debug.Log("[TeamService] SendTeamInviteResponse");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.teamInviteRes = new TeamInviteResponse();
            message.Request.teamInviteRes.Result = accept ? Result.Success : Result.Failed;
            message.Request.teamInviteRes.Errormsg = accept ? "组队成功" : "对方拒绝了组队邀请";
            message.Request.teamInviteRes.Request = request;
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 发送离开队伍Request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="friendId"></param>
        public void SendTeamLeaveRequest(int id)
        {
            Debug.Log("[TeamService] SendTeamLeaveRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.teamLeave = new TeamLeaveRequest();
            message.Request.teamLeave.TeamId = User.Instance.TeamInfo.Id;
            message.Request.teamLeave.characterId = User.Instance.CurrentCharacter.Id;
            NetClient.Instance.SendMessage(message);
        }
        #endregion

        #region 服务端消息处理
        /// <summary>
        /// 本玩家收到组队邀请响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnTeamInviteRequest(object sender, TeamInviteRequest request)
        {
            var confirm = MessageBox.Show(
                string.Format("{0} 邀请你加入队伍", request.FromName),
                "组队请求",
                MessageBoxType.Confirm,
                "接受",
                "拒绝"
                );
            confirm.OnYes = () =>
            {
                this.SendTeamInviteResponse(true, request);
            };
            confirm.OnNo = () =>
            {
                this.SendTeamInviteResponse(false, request);
            };
        }
        /// <summary>
        /// 收到组队邀请响应
        /// </summary>
        /// <param name="accept"></param>
        /// <param name="request"></param>
        public void OnTeamInviteResponse(object sender, TeamInviteResponse response)
        {
            if (response.Result == Result.Success)
                MessageBox.Show(response.Request.ToName + "加入您的队伍", "邀请组队成功");
            else
                MessageBox.Show(response.Errormsg, "邀请组队失败");
        }


        /// <summary>
        /// 收到队伍信息后更新队伍信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnTeamInfo(object sender, TeamInfoResponse message)
        {
            try
            {
                Debug.Log("[TeamService] OnTeamInfo");
                TeamManager.Instance.UpdateTeamInfo(message.Team);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 收到队伍离开响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnTeamLeave(object sender, TeamLeaveResponse message)
        {
            if (message.Result == Result.Success)
            {
                TeamManager.Instance.UpdateTeamInfo(null);
                MessageBox.Show("退出成功", "退出队伍");
            }
            else
                MessageBox.Show("退出失败", "退出队伍", MessageBoxType.Error);
        }

        #endregion

    }
}
