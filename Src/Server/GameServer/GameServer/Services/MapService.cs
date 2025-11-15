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
        internal void SendEntityUpdate(NetConnection<NetSession> connection, NEntitySync entitySync)
        {
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapEntitySync = new MapEntitySyncResponse();
            message.Response.mapEntitySync.entitySyncs.Add(entitySync);

            byte[] data = PackageHandler.PackMessage(message);
            connection.SendData(data, 0, data.Length);
        }

        #endregion

        #region 响应客户端请求
        /// <summary>
        /// 响应同步消息 MapEntitySyncRequest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnMapEntitySync(NetConnection<NetSession> sender, MapEntitySyncRequest request)
        {
            // 1. 验证会话的有效性（避免还没进游戏就开始同步）
            if(sender.Session == null)
            {
                Log.Warning("[MapService] OnMapEntitySync: 会话为 null, drop");
                return;
            }
            // 2. 验证角色是否已绑定到会话
            Character cha = sender.Session.Character;
            if(cha == null)
            {
                Log.Warning($"[MapService] OnMapEntitySync: state={sender.Session} 但是 Character==null, drop.");
                return;
            }
            // 3. 验证地图是否绑定
            var mapId = cha.Info.mapId;
            var map = MapManager.Instance[mapId];
            if (map == null)
            {
                Log.Error($"[MapService] OnMapEntitySync: map {mapId} 未发现, drop.");
                return;
            }

            // 4. 正常处理
            Character character = sender.Session.Character;

            // Log.InfoFormat("[MapService] OnMapEntitySync: characterID:{0}:{1} Entity.Id:{2} Evt:{3} Entity:{4}", character.Id, character.Info.Name, request.entitySync.Id, request.entitySync.Event, request.entitySync.Entity.String());

            MapManager.Instance[character.Info.mapId].UpdateEntity(request.entitySync);
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
            }

            // 获取传送目标点
            TeleporterDefine target = DataManager.Instance.Teleporters[source.LinkTo];

            // 角色离开原地图
            MapManager.Instance[source.MapID].CharacterLeave(character);
            // 角色进入新地图
            character.Position = target.Position;
            character.Direction = target.Direction;
            MapManager.Instance[target.MapID].CharacterEnter(sender, character);
        }
        #endregion
    }
}
