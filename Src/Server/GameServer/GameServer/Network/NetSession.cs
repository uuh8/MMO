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
        public TUser User { get; set; }             // 当前登录的用户    
        public Character Character { get; set; }    // 当前用户选择的角色
        public NEntity Entity { get; set; }         // 当前用户选择的角色在游戏中的实体
		public IPostResponser PostResponser { get; set; }   // 响应后处理器

        private const int MinSendIntervalMs = 100;
        private long lastSendTick = 0;
        
        public bool ForceFlush { get; set; } = false;   // 某些关键消息想绕过限频（登录/切图/强提示等）


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
            if (response == null) return null;

            // 1) 限频判断：距离上次真正发包不足 100ms，就先不发（把 response 留着继续累计）
            long now = Environment.TickCount;
            if (!ForceFlush && now - lastSendTick < MinSendIntervalMs)
            {
                // 注意：这里不能清空 response，否则累计的包会丢
                return null;
            }

            // 2) 允许发送：先跑后处理，把“顺便发送”的系统消息塞进 Response
            if (PostResponser != null)
                this.PostResponser.PostProcess(Response);

            // 3) 打包并清空缓冲
            byte[] data = PackageHandler.PackMessage(response);
            response = null;

            // 4) 记录真正发包时间，并清除强制标记
            lastSendTick = now;
            ForceFlush = false;

            return data;
        }
    }
}
