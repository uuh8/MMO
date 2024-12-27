using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameServer;
using GameServer.Entities;
using SkillBridge.Message;

namespace Network
{
    class NetSession
    {
        /*1. TUser User
        定义：TUser 是一个用户对象，通常对应于数据库中的 Users 表。
        作用：存储当前连接的用户信息，例如用户名、密码等。*/
        public TUser User { get; set; }             // 当前登录的用户    

        public Character Character { get; set; }    //当前用户选择的角色
        public NEntity Entity { get; set; }         //当前用户选择的角色在游戏中的实体
    }
}
