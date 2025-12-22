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
    public class GuildService : Singleton<GuildService>, IDisposable
    {
        public UnityAction OnGuildUpdate;
        // 工会创建事件
        public UnityAction<bool> OnGuildCreateResult;
        // 工会列表请求更新的事件
        public UnityAction<List<NGuildInfo>> OnGuildListResult;

        public void Init()
        {

        }

        public GuildService()
        {
            MessageDistributer.Instance.Subscribe<GuildCreateResponse>(this.OnGuildCreate);
            MessageDistributer.Instance.Subscribe<GuildListResponse>(this.OnGuildList);
            MessageDistributer.Instance.Subscribe<GuildJoinRequest>(this.OnGuildJoinRequest);
            MessageDistributer.Instance.Subscribe<GuildJoinResponse>(this.OnGuildJoinResponse);
            MessageDistributer.Instance.Subscribe<GuildResponse>(this.OnGuild);
            MessageDistributer.Instance.Subscribe<GuildLeaveResponse>(this.OnGuildLeave);
        }

        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<GuildCreateResponse>(this.OnGuildCreate);
            MessageDistributer.Instance.Unsubscribe<GuildListResponse>(this.OnGuildList);
            MessageDistributer.Instance.Unsubscribe<GuildJoinRequest>(this.OnGuildJoinRequest);
            MessageDistributer.Instance.Unsubscribe<GuildJoinResponse>(this.OnGuildJoinResponse);
            MessageDistributer.Instance.Unsubscribe<GuildResponse>(this.OnGuild);
            MessageDistributer.Instance.Unsubscribe<GuildLeaveResponse>(this.OnGuildLeave);
        }

        #region 消息发送
        /// <summary>
        /// 发送创建工会请求
        /// </summary>
        /// <param name="guildName"></param>
        /// <param name="notice"></param>
        public void SendGuildCreate(string guildName, string notice)
        {
            Debug.Log("[GuildService] SendGuildCreate");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildCreate = new GuildCreateRequest();
            message.Request.guildCreate.GuildName = guildName;
            message.Request.guildCreate.GuildNotice = notice;
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 发送加入工会请求
        /// </summary>
        /// <param name="guildId"></param>
        public void SendGuildJoinRequeset(int guildId)
        {
            Debug.Log("[GuildService] SendGuildJoinRequeset");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildJoinReq = new GuildJoinRequest();
            message.Request.guildJoinReq.Apply = new NGuildApplyInfo();
            message.Request.guildJoinReq.Apply.GuildId = guildId;
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 发送加入工会请求回应(只有会长或者管理员才能回复)(由OnGuildJoinRequest调用)
        /// 是否允许申请者加入自己的公会
        /// </summary>
        /// <param name="guildId"></param>
        public void SendGuildJoinResponse(bool accept, GuildJoinRequest request)
        {
            Debug.Log("[GuildService] SendGuildJoinResponse");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildJoinRes = new GuildJoinResponse();
            message.Request.guildJoinRes.Result = Result.Success;
            message.Request.guildJoinRes.Apply = request.Apply;
            message.Request.guildJoinRes.Apply.Result = accept ? ApplyResult.Accept : ApplyResult.Reject;
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 发送离开队伍Request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="friendId"></param>
        public void SendGuildLeaveRequest()
        {
            Debug.Log("[GuildService] SendGuildLeaveRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildLeave = new GuildLeaveRequest();
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 申请公会列表请求
        /// </summary>
        public void sendGuildListRequest()
        {
            Debug.Log("[GuildService] SendGuildLeaveRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.guildList = new GuildListRequest();
            NetClient.Instance.SendMessage(message);
        }
        #endregion

        #region 服务端消息处理
        /// <summary>
        /// 收到工会创建响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnGuildCreate(object sender, GuildCreateResponse response)
        {
            Debug.LogFormat("[GuildService] OnGuildCreateResponse : {0}", response.Result);
            if(OnGuildCreateResult != null)
            {
                // 事件触发
                this.OnGuildCreateResult(response.Result == Result.Success);
            }
            if(response.Result == Result.Success)
            {
                GuildManager.Instance.Init(response.guildInfo);
                MessageBox.Show(string.Format("{0} 公会创建成功", response.guildInfo.GuildName), "公会");
            }
            else
            {
                MessageBox.Show(string.Format("{0} 公会创建失败", response.guildInfo.GuildName), "公会");
            }
        }
        /// <summary>
        /// 收到公会申请响应
        /// </summary>
        /// <param name="accept"></param>
        /// <param name="request"></param>
        public void OnGuildJoinRequest(object sender, GuildJoinRequest request)
        {
            var confirm = MessageBox.Show(
                string.Format("{0} 申请加入公会", request.Apply.Name),
                "公会申请",
                MessageBoxType.Confirm,
                "接受",
                "拒绝"
                );
            confirm.OnYes = () =>
            {
                this.SendGuildJoinResponse(true, request);
            };
            confirm.OnNo = () =>
            {
                this.SendGuildJoinResponse(false, request);
            };
        }

        /// <summary>
        /// 收到会长/管理员答复后服务都分发的响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnGuildJoinResponse(object sender, GuildJoinResponse response)
        {
            Debug.Log("[GuildService] OnGuildJoinResponse");
            if(response.Result == Result.Success)
            {
                MessageBox.Show("加入公会成功", "公会");
            }
            else
            {
                MessageBox.Show("加入公会失败", "公会");
            }
        }

        /// <summary>
        /// 刷新公会UI信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnGuild(object sender, GuildResponse response)
        {
            Debug.LogFormat("OnGuild:{0} {1}:{2}", response.Result, response.guildInfo.Id, response.guildInfo.GuildName);
            GuildManager.Instance.Init(response.guildInfo);
            if(this.OnGuildUpdate != null)
            {
                // 刷新UI
                this.OnGuildUpdate();
            }
        }

        /// <summary>
        /// 离开公会响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnGuildLeave(object sender, GuildLeaveResponse message)
        {
            if (message.Result == Result.Success)
            {
                GuildManager.Instance.Init(null);
                MessageBox.Show("退出公会成功", "公会");
            }
            else
                MessageBox.Show("退出公会失败", "公会", MessageBoxType.Error);
        }

        /// <summary>
        /// 收到列表更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnGuildList(object sender, GuildListResponse response)
        {
            if (OnGuildListResult != null)
            {
                // 触发事件
                this.OnGuildListResult(response.Guilds);
            }
        }
        #endregion

    }
}
