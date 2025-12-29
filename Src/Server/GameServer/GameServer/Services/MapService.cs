using Common;
using Common.Data;
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
    class MapService : Singleton<MapService>
    {
        public MapService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<MapEntitySyncRequest>(this.OnMapEntitySync);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<MapTeleportRequest>(this.OnMapTeleport); 
        }
        public void Init()
        {
            MapManager.Instance.Init();
        }

        #region 发送信息给客户端
        /// <summary>
        /// 把 entitySync 信息发送给 connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entitySync"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void SendEntityUpdate(NetConnection<NetSession> sender, NEntitySync entitySync)
        {
            // 1) 不要每次 new：Response 是“累加器”
            //    同一个网络 tick / 同一次要发的响应里，可能会塞多个 entitySync。
            //    如果每次都 new，会把之前已经塞进去的列表覆盖掉（丢同步）。
            if (sender.Session.Response.mapEntitySync == null)
                sender.Session.Response.mapEntitySync = new MapEntitySyncResponse();

            // 2) 把这一次的 entitySync 追加到列表里（增量）
            sender.Session.Response.mapEntitySync.entitySyncs.Add(entitySync);

            // 3) 立刻触发一次发送
            //    重点：SendResponse() 会调用 session.GetResponse()
            //    而 GetResponse() 会触发 Character.PostProcess()
            //    从而 Chat.PostProcess() 才能“顺便把聊天增量塞进这次回包”。
            sender.SendResponse();
        }

        /*NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapEntitySync = new MapEntitySyncResponse();
            message.Response.mapEntitySync.entitySyncs.Add(entitySync);

            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);*/
        #endregion

        #region 响应客户端请求
        /// <summary>
        /// 响应同步消息 MapEntitySyncRequest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnMapEntitySync(NetConnection<NetSession> sender, MapEntitySyncRequest request)
        {
            // 1) 会话校验：避免客户端还没完成登录/进入地图就开始发同步包
            if (sender.Session == null)
            {
                Log.Warning("[MapService] OnMapEntitySync: 会话为 null, drop");
                return;
            }

            // 2) 角色校验：服务器权威必须有“这条连接对应的角色”
            Character cha = sender.Session.Character;
            if(cha == null)
            {
                Log.Warning($"[MapService] OnMapEntitySync: state={sender.Session} 但是 Character==null, drop.");
                return;
            }

            // 3) 地图校验：角色必须已经绑定到某张地图实例
            var mapId = cha.Info.mapId;
            var map = MapManager.Instance[mapId];
            if (map == null)
            {
                Log.Error($"[MapService] OnMapEntitySync: map {mapId} 未发现, drop.");
                return;
            }

            // 4) 更新服务端权威状态
            //    注意：这里是“收客户端输入 → 服务器校验/裁决 → 更新服务器状态”
            map.UpdateEntity(request.entitySync);

            // 5) 触发一次回包，让“后处理”有机会把聊天/队伍/公会等增量顺带发回客户端
            //    否则如果移动链路不回包，客户端可能长时间收不到聊天（你遇到的现象）
            sender.SendResponse();
        }

        /// <summary>
        /// 响应角色进入传送点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnMapTeleport(NetConnection<NetSession> sender, MapTeleportRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[MapService] OnMapTeleport: characterID:{0}:{1} TeleporterId:{2}", character.Id, character.Data, request.teleporterId);

            // 校验传送点存不存在
            if (!DataManager.Instance.Teleporters.ContainsKey(request.teleporterId))
            {
                Log.WarningFormat("Source TeleporterID [{0}] not existed", request.teleporterId);
                return;
            }

            // 读传送点数据，检验传送目标点是否存在
            TeleporterDefine source = DataManager.Instance.Teleporters[request.teleporterId];
            if (source.LinkTo == 0 || !DataManager.Instance.Teleporters.ContainsKey(source.LinkTo))
            {
                Log.WarningFormat("Source TeleporterID [{0}] LinkTo ID [{1}] not existed", request.teleporterId, source.LinkTo);
                return;
            }

            // 获取传送目标点
            TeleporterDefine target = DataManager.Instance.Teleporters[source.LinkTo];

            // 角色离开原地图
            MapManager.Instance[source.MapID].CharacterLeave(character);
            // 角色进入新地图
            character.Position = target.Position;
            character.Direction = target.Direction;
            // 进入目标地图：传送属于“强即时反馈”的关键行为
            // 必须立刻把 mapCharacterEnter 发给客户端，否则会出现：客户端不切图/位置不刷新/要等下一次网络包才更新
            MapManager.Instance[target.MapID].CharacterEnter(sender, character, sendNow: true);

        }
        #endregion
    }
}
