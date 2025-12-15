using GameServer.Entities;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class FriendManager
    {
        Character Owner;
        // 维护每个玩家的好友列表
        List<NFriendInfo> friends = new List<NFriendInfo>();

        bool friendChanged = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner"></param>
        public FriendManager(Character owner)
        {
            this.Owner = owner;
            this.InitFriends();
        }
        public void InitFriends()
        {
            this.friends.Clear();
            foreach (var friend in this.Owner.Data.Friends)
            {
                this.friends.Add(GetFriendInfo(friend));
            }
        }
        public NFriendInfo GetFriendInfo(TCharacterFriend friend)
        {
            NFriendInfo friendInfo = new NFriendInfo();
            var character = CharacterManager.Instance.GetCharacter(friend.FriendID);
            friendInfo.friendInfo = new NCharacterInfo();
            friendInfo.Id = friend.Id;
            if(character == null)
            {
                friendInfo.friendInfo.Id = friend.FriendID;
                friendInfo.friendInfo.Name = friend.FriendName;
                friendInfo.friendInfo.Class = (CharacterClass)friend.Class;
                friendInfo.friendInfo.Level = friend.Level;
                friendInfo.Status = 0;
            }
            else
            {
                friendInfo.friendInfo = GetBasicInfo(character.Info);
                friendInfo.friendInfo.Name = character.Info.Name;
                friendInfo.friendInfo.Class = character.Info.Class;
                friendInfo.friendInfo.Level = character.Info.Level;
                character.FriendManager.UpdateFriendInfo(this.Owner.Info, 1);
                friendInfo.Status = 1;
            }

            return friendInfo;
        }

        public NFriendInfo GetFriendInfo(int friendId)
        {
            foreach(var f in this.friends)
            {
                if(f.friendInfo.Id == friendId)
                {
                    return f;
                }
            }
            return null;
        }

        NCharacterInfo GetBasicInfo(NCharacterInfo info)
        {
            return new NCharacterInfo()
            {
                Id = info.Id,
                Name = info.Name,
                Class = info.Class,
                Level = info.Level
            };
        }

        /// <summary>
        /// 给 Character 初始化使用
        /// </summary>
        /// <param name="list"></param>
        public void GetFriendInfos(List<NFriendInfo> list)
        {
            foreach (var friend in this.friends)
            {
                list.Add(friend);
            }
        }
        /// <summary>
        /// 添加好友
        /// </summary>
        /// <param name="friend"></param>
        public void AddFriend(Character friend)
        {
            TCharacterFriend tf = new TCharacterFriend()
            {
                FriendID = friend.Id,
                FriendName = friend.Data.Name,
                Class = friend.Data.Class,
                Level = friend.Data.Level
            };
            this.Owner.Data.Friends.Add(tf);
            friendChanged = true;   // 标记好友列表变化了
        }
        /// <summary>
        /// 删除好友
        /// </summary>
        /// <param name="friend"></param>
        public bool RemoveFriendByFriendId(int friendid)
        {
            // 从数据库中删除一条记录
            var removeItem = this.Owner.Data.Friends.FirstOrDefault(v => v.FriendID == friendid);
            if(removeItem != null)
            {
                DBService.Instance.Entities.TCharacterFriends.Remove(removeItem);
            }
            friendChanged = true;   // 标记好友列表变化了
            return true;
        }
        public bool RemoveFriendById(int id)
        {
            // 从数据库中删除一条记录
            var removeItem = this.Owner.Data.Friends.FirstOrDefault(v => v.Id == id);
            if (removeItem != null)
            {
                DBService.Instance.Entities.TCharacterFriends.Remove(removeItem);
            }
            friendChanged = true;   // 标记好友列表变化了
            return true;
        }

        /// <summary>
        /// 更新好友状态（本玩家下线的时候调用）
        /// </summary>
        /// <param name="friendInfo"></param>
        /// <param name="status"></param>
        public void UpdateFriendInfo(NCharacterInfo friendInfo, int status)
        {
            foreach(var f in this.friends)
            {
                if(f.friendInfo.Id == friendInfo.Id)
                {
                    f.Status = status;
                    break;
                }
            }
            this.friendChanged = true;
        }

        public void PostProcess(NetMessageResponse message)
        {
            if (friendChanged)
            {
                this.InitFriends();
                if(message.friendList == null)
                {
                    message.friendList = new FriendListResponse();
                    message.friendList.Friends.AddRange(this.friends);
                }
                friendChanged = false;
            }
        }
    }
}
