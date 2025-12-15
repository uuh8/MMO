using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Events;

namespace Services
{
    class FriendService : Singleton<FriendService>, IDisposable
    {
        public UnityAction OnFriendUpdate;
        public void Init()
        {

        }

        public FriendService()
        {
            // 如果其他玩家添加本玩家为好友，本玩家就会收到FriendAddRequest，因此需要单独处理客户端的FriendAddRequest
            MessageDistributer.Instance.Subscribe<FriendAddRequest>(this.OnFriendAddRequest);

            MessageDistributer.Instance.Subscribe<FriendAddResponse>(this.OnFriendAddResponse);
            MessageDistributer.Instance.Subscribe<FriendListResponse>(this.OnFriendList);
            MessageDistributer.Instance.Subscribe<FriendRemoveResponse>(this.OnFriendRemove);
        }

        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<FriendAddRequest>(this.OnFriendAddRequest);
            MessageDistributer.Instance.Unsubscribe<FriendAddResponse>(this.OnFriendAddResponse);
            MessageDistributer.Instance.Unsubscribe<FriendListResponse>(this.OnFriendList);
            MessageDistributer.Instance.Unsubscribe<FriendRemoveResponse>(this.OnFriendRemove);
        }

        #region 消息发送
        /// <summary>
        /// 发送添加好友的请求
        /// </summary>
        /// <param name="friendId"></param>
        /// <param name="friendName"></param>
        public void SendFriendAddRequest(int friendId, string friendName)
        {
            Debug.Log("[FriendService] SendFriendAddRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.friendAddReq = new FriendAddRequest();
            message.Request.friendAddReq.FromId = User.Instance.CurrentCharacter.Id;
            message.Request.friendAddReq.FromName = User.Instance.CurrentCharacter.Name;
            message.Request.friendAddReq.ToId = friendId;
            message.Request.friendAddReq.ToName = friendName;
            NetClient.Instance.SendMessage(message);
        }
        /// <summary>
        /// 发送好友删除请求
        /// </summary>
        /// <param name="id"></param>
        /// <param name="friendId"></param>
        public void SendFriendRemoveRequest(int id, int friendId)
        {
            Debug.Log("[FriendService] SendFriendRemoveRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.friendRemove = new FriendRemoveRequest();
            message.Request.friendRemove.Id = id;
            message.Request.friendRemove.friendId = friendId;
            NetClient.Instance.SendMessage(message);
        }

        #endregion

        #region 服务端消息处理
        /// <summary>
        /// 收到添加好友请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnFriendAddRequest(object sender, FriendAddRequest request)
        {
            var confirm = MessageBox.Show(string.Format("{0} 请求添加你为好友", "好友请求", MessageBoxType.Confirm, "接受", "拒绝"));
            confirm.OnYes = () =>
            {
                this.SendFriendAddResponse(true, request);
            };
            confirm.OnNo = () =>
            {
                this.SendFriendAddResponse(false, request);
            };
        }
        /// <summary>
        /// 是否同意别人发来的好友请求，由上面这个OnFriendAddRequest调用
        /// </summary>
        /// <param name="accept"></param>
        /// <param name="request"></param>
        public void SendFriendAddResponse(bool accept, FriendAddRequest request)
        {
            Debug.Log("[FriendService] SendFriendAddResponse");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.friendAddRes = new FriendAddResponse();
            message.Request.friendAddRes.Result = accept ? Result.Success : Result.Failed;
            message.Request.friendAddRes.Errormsg = accept ? "对方同意了你的好友请求" : "对方拒绝了你的好友请求";
            message.Request.friendAddRes.Request = request;
            NetClient.Instance.SendMessage(message);
        }


        /// <summary>
        /// 收到添加好友响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnFriendAddResponse(object sender, FriendAddResponse message)
        {
            if (message.Result == Result.Success)
                MessageBox.Show(message.Request.ToName + "接受了您的请求", "添加好友成功");
            else
                MessageBox.Show(message.Errormsg, "添加好友失败");
        }
        private void OnFriendRemove(object sender, FriendRemoveResponse message)
        {
            if (message.Result == Result.Success)
                MessageBox.Show("删除成功", "删除好友");
            else
                MessageBox.Show("删除失败", "删除好友", MessageBoxType.Error);
        }

        private void OnFriendList(object sender, FriendListResponse message)
        {
            Debug.Log("[FriendService] OnFriendList");
            FriendManager.Instance.allFriends = message.Friends;
            if (this.OnFriendUpdate != null)
                this.OnFriendUpdate();
        }
        #endregion

    }
}
