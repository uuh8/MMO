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
    class FriendService : Singleton<FriendService>
    {
        public FriendService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FriendAddRequest>(this.OnFriendAddRequest);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FriendAddResponse>(this.OnFriendAddResponse);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FriendRemoveRequest>(this.OnFriendRemove);
        }

        public void Init()
        {

        }

        #region 处理客户端的消息
        /// <summary>
        /// 收到加好友请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnFriendAddRequest(NetConnection<NetSession> sender, FriendAddRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[FriendService] OnFriendAddRequest: FromId:{0} FromName:{1} ToTD:{2} ToName:{3}", request.FromId, request.FromName, request.ToId, request.ToName);

            // 注意这儿查找的是在线的玩家，因为是在 CharacterManager 中的，如果要加离线的好友就需要在db中找
            if (request.ToId == 0)
            {
                // ToId == 0 表示没有传入 ID，就按照 Name 查找并补齐 ToId
                foreach (var cha in CharacterManager.Instance.Characters)
                {
                    if(cha.Value.Data.Name == request.ToName)
                    {
                        request.ToId = cha.Key;
                        break;
                    }
                }
            }

            // 查找想加玩家的 Session
            NetConnection<NetSession> friend = null;
            // ToId > 0 表示传入了ID，就按照 ID 来查找被加好友的玩家
            if (request.ToId > 0)
            {
                // 校验添加对象是否已经是好友了
                if(character.FriendManager.GetFriendInfo(request.ToId) != null)
                {
                    sender.Session.Response.friendAddRes = new FriendAddResponse();
                    sender.Session.Response.friendAddRes.Result = Result.Failed;
                    sender.Session.Response.friendAddRes.Errormsg = "你们已经是好友了";
                    sender.SendResponse();
                    return;
                }

                // 用SessionManager 找到该玩家的 Session
                // 用 Session 的原因是怪物和玩家是共同管理的，但只有玩家有 Session
                friend = SessionManager.Instance.GetSession(request.ToId);
            }

            // 防止执行该函数的中途该玩家掉线造成 friend 为 null
            if (friend == null)
            {
                sender.Session.Response.friendAddRes = new FriendAddResponse();
                sender.Session.Response.friendAddRes.Result = Result.Failed;
                sender.Session.Response.friendAddRes.Errormsg = "好友不存在或离线";
                sender.SendResponse();
                return;
            }

            // ⭐向好友的 Session 直接原封不动转发 “玩家a发来的想加玩家b为好友” 的这个请求
            friend.Session.Response.friendAddReq = request;
            friend.SendResponse();
        }
        /// <summary>
        /// 收到加好友响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnFriendAddResponse(NetConnection<NetSession> sender, FriendAddResponse response)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[FriendService] OnFriendAddResponse: character:{0} Result:{1} FromId:{2} ToTD:{3} ", character.Id, response.Result, response.Request.FromId, response.Request.ToId);

            sender.Session.Response.friendAddRes = response;
            if (response.Result == Result.Success)
            {
                // 接受了好友请求
                var requester = SessionManager.Instance.GetSession(response.Request.FromId);
                if(requester == null)
                {
                    sender.Session.Response.friendAddRes.Result = Result.Failed;
                    sender.Session.Response.friendAddRes.Errormsg = "请求者已下线";
                }
                else
                {
                    // 对方没离线并且同意了好友请求
                    character.FriendManager.AddFriend(requester.Session.Character); // a 添加 b
                    requester.Session.Character.FriendManager.AddFriend(character); // b 添加 a
                    DBService.Instance.Save();
                    // 发送给a（请求者）
                    requester.Session.Response.friendAddRes = response;
                    requester.Session.Response.friendAddRes.Result = Result.Success;
                    requester.Session.Response.friendAddRes.Errormsg = "添加好友成功";
                    requester.SendResponse();
                }
                sender.SendResponse();
            }
        }

        private void OnFriendRemove(NetConnection<NetSession> sender, FriendRemoveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[FriendService] OnFriendRemove: character{0} FriendReletionID:{1}", character.Id, request.Id);
            sender.Session.Response.friendRemove = new FriendRemoveResponse();
            sender.Session.Response.friendRemove.Id = request.Id;

            // 删除自己好友
            if (character.FriendManager.RemoveFriendById(request.Id))
            {
                sender.Session.Response.friendRemove.Result = Result.Success;
                // 删除别人好友中的自己
                var friend = SessionManager.Instance.GetSession(request.friendId);
                if(friend != null)
                {
                    // 好友在线，先删内存再删数据库
                    friend.Session.Character.FriendManager.RemoveFriendByFriendId(character.Id);
                }
                else
                {
                    // 不在线，直接删数据库
                    this.RemoveFriend(request.friendId, character.Id);
                }
            }
            else
            {
                sender.Session.Response.friendRemove.Result = Result.Failed;
            }
            DBService.Instance.Save();
            sender.SendResponse();
        }

        private void RemoveFriend(int charId, int friendId)
        {
            TCharacterFriend removeItem = DBService.Instance.Entities.TCharacterFriends.FirstOrDefault(v => v.CharacterID == charId && v.FriendID == friendId);
            if (removeItem != null)
            {
                DBService.Instance.Entities.TCharacterFriends.Remove(removeItem);
            }
        }
        #endregion
    }
}
