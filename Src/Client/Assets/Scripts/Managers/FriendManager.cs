using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillBridge.Message;

namespace Managers
{
    public class FriendManager : Singleton<FriendManager>
    {
        // 所有有效任务
        public List<NFriendInfo> allFriends;

        public void Init(List<NFriendInfo> friends)
        {
            this.allFriends = friends;
        }
    }
}
