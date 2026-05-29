using Common.Data;
using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Services
{
    class MapService : Singleton<MapService>, IDisposable
    {
        public int CurrentMapId { get; set; }

        public MapService()
        {
            // 监听来自服务器的消息。当服务器返回用户注册结果时，触发对应的方法。
            MessageDistributer.Instance.Subscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Subscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            MessageDistributer.Instance.Subscribe<MapEntitySyncResponse>(this.OnMapEntitySync);
            MessageDistributer.Instance.Subscribe<MonsterStateSyncResponse>(this.OnMonsterStateSync);

        }

        public void Dispose()
        {
            //资源释放，解除订阅的事件和消息，防止内存泄漏或对象被销毁后仍然调用事件逻辑。
            MessageDistributer.Instance.Unsubscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Unsubscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            MessageDistributer.Instance.Unsubscribe<MapEntitySyncResponse>(this.OnMapEntitySync);
            MessageDistributer.Instance.Unsubscribe<MonsterStateSyncResponse>(this.OnMonsterStateSync);
        }

        public void Init()
        {

        }

        #region 发送消息给服务器

        /// <summary>
        /// 发送同步信息
        /// </summary>
        /// <param name="entityEvent"></param>
        /// <param name="nEntity"></param>
        public void SendMapEntitySync(EntityEvent entityEvent, NEntity nEntity, int param)
        {
            // Debug.LogFormat("[MapService] MapEntityUpdateRequest :ID:{0} POS:{1} DIR:{2} SPD:{3} ", nEntity.Id, nEntity.Position.String(), nEntity.Direction.String(), nEntity.Speed);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapEntitySync = new MapEntitySyncRequest();
            message.Request.mapEntitySync.entitySync = new NEntitySync()
            {
                Id = nEntity.Id,
                Event = entityEvent,
                Entity = nEntity,
                Param = param
            };
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 角色进入传送点
        /// </summary>
        /// <param name="iD"></param>
        public void SendMapTeleport(int teleporterID)
        {
            Debug.LogFormat("[MapService] MapTeleportRequest: teleporterID:{0}", teleporterID);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapTeleport = new MapTeleportRequest();
            message.Request.mapTeleport.teleporterId = teleporterID;
            NetClient.Instance.SendMessage(message);
        }

        #endregion

        #region 响应服务器发来的消息

        /// <summary>
        /// 角色进入地图响应（MapService 不负责 LoadScene，只负责实体列表管理）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnMapCharacterEnter(object sender, MapCharacterEnterResponse response)
        {
            /*
             * message MapCharacterEnterResponse{
	                int32 mapId = 1;                    // 当前地图ID
	                repeated NCharacterInfo characters = 2; // 地图内可见角色列表
                }
             */
            Debug.LogFormat("[MapService] OnMapCharacterEnter:Map:{0} Count:{1}", response.mapId, response.Characters.Count);

            // 换图时先清理旧地图实体（避免残留）
            if (CurrentMapId != 0 && CurrentMapId != response.mapId)
            {
                CharacterManager.Instance.Clear();
                MonsterManager.Instance.Clear(); // 换图时也清理怪物
            }
            CurrentMapId = response.mapId;

            // 玩家列表 → CharacterManager
            foreach (var cha in response.Characters)
            {
                if (!CharacterManager.Instance.CharactersMngr.ContainsKey(cha.EntityId))
                    CharacterManager.Instance.AddCharacter(cha);
            }

            // 怪物列表 → MonsterManager（现在直接传 NMonsterInfo，不需要转换了）
            foreach (var monster in response.Monsters)
            {
                if (!MonsterManager.Instance.MonstersMngr.ContainsKey(monster.EntityId))
                    MonsterManager.Instance.AddMonster(monster);
            }

        }
        private void EnterMap(int mapId)
        {
            // 从DataManager中拿到地图的信息
            if (DataManager.Instance.Maps.ContainsKey(mapId))
            {
                MapDefine map = DataManager.Instance.Maps[mapId];
                User.Instance.CurrentMapData = map;
                SceneManager.Instance.LoadScene(map.Resource);
                SoundManager.Instance.PlayMusic(map.Music);
            }
            else
            {
                Debug.LogErrorFormat("[MapService] EnterMap: Map {0} not existed", mapId);
            }
        }

        /// <summary>
        /// 角色离开地图响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnMapCharacterLeave(object sender, MapCharacterLeaveResponse response)
        {
            Debug.LogFormat("[MapService] OnMapCharacterLeave: CharId:{0}", response.entityId);

            if (response.entityId == User.Instance.CurrentCharacter.EntityId)
            {
                // 自己离开，全部清理
                CharacterManager.Instance.Clear();
                MonsterManager.Instance.Clear();
                return;
            }

            // 先判断是玩家还是怪物再分别移除
            if (CharacterManager.Instance.CharactersMngr.ContainsKey(response.entityId))
            {
                CharacterManager.Instance.RemoveCharacter(response.entityId);
            }
            else if (MonsterManager.Instance.MonstersMngr.ContainsKey(response.entityId))
            {
                MonsterManager.Instance.RemoveMonster(response.entityId);
            }
        }

        /// <summary>
        /// 角色同步信息的响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnMapEntitySync(object sender, MapEntitySyncResponse response)
        {
            foreach(var entity in response.entitySyncs)
            {
                EntityManager.Instance.OnEntitySync(entity);
            }
        }

        private void OnMonsterStateSync(object sender, MonsterStateSyncResponse response)
        {
            // 先确认消息有没有收到
            Debug.LogFormat("[MapService] OnMonsterStateSync 收到消息，数量:{0}", response.Syncs.Count);

            foreach (var sync in response.Syncs)
            {
                // 把这条消息交给 GameObjectManager 处理
                // GameObjectManager 持有所有怪物的 GameObject 引用
                // 由它来找到对应的怪物并切换行为
                GameObjectManager.Instance.OnMonsterStateSync(sync);
            }
        }



        #endregion

    }
}
