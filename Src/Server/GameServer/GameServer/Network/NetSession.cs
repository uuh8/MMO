using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameServer;
using GameServer.Entities;
using GameServer.Services;
using SkillBridge.Message;

namespace Network
{
    class NetSession : INetSession
    {
        /*1. TUser User
        定义：TUser 是一个用户对象，通常对应于数据库中的 Users 表。
        作用：存储当前连接的用户信息，例如用户名、密码等。*/
        public TUser User { get; set; }             // 当前登录的用户    

        public Character Character { get; set; }    // 当前用户选择的角色
        public NEntity Entity { get; set; }         // 当前用户选择的角色在游戏中的实体
		public IPostResponser PostResponser { get; set; }   // 响应后处理器

        public void Disconnected()
        {
            this.PostResponser = null;  // 断开时需要清空
            if (this.Character != null)
                UserService.Instance.CharacterLeave(this.Character);
        }


        private NetMessage response;    // 用于响应的详细 

        public NetMessageResponse Response
        {
            get
            {
                if (response == null)
                {
                    response = new NetMessage();
                }
                if (response.Response == null)
                    response.Response = new NetMessageResponse();

                return response.Response; 
            }
        }

        // 实现 INetSession 接口
        public byte[] GetResponse()
        {
            if (response != null)
            {
                if (PostResponser != null)
                    this.PostResponser.PostProcess(Response);

                byte[] data = PackageHandler.PackMessage(response);
                // 只要消息发送给客户端，就清空这个response消息，这样就保证了response一定是在会话session一开始创建，会话session一结束清空；并且我们可以在会话session期间对response多次赋值让其包含多个消息
                response = null; 
                return data;
            }
            return null;
        }
    }
}
