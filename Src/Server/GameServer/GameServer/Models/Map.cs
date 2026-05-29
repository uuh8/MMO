using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;

using Common;
using Common.Data;

using Network;
using GameServer.Managers;
using GameServer.Entities;
using ProtoBuf.Serializers;
using GameServer.Services;

namespace GameServer.Models
{
    // 地图类：表示一张游戏地图实例，并维护该地图上所有在线角色的连接与运行时实体
    class Map
    {
        // 地图的静态定义信息（来自配置，比如地图名、尺寸、刷怪点等）
        internal MapDefine Define;

        /// <summary>
        /// 内部类型：表示地图上的某个玩家（包括网络连接和领域实体）
        /// </summary>
        internal class MapCharacter
        {
            public NetConnection<NetSession> connection;    // 该玩家对应的网络连接（用于发送消息回客户端）
            public Character character;                     // 该玩家对应的运行时角色实体

            public MapCharacter(NetConnection<NetSession> conn, Character cha)
            {
                this.connection = conn;
                this.character = cha;
            }
        }

        // 地图的ID（从地图定义中获取）
        public int ID
        {
            get { return this.Define.ID; }
        }

        // 地图上当前在线的角色集合：Key 是 characterId
        Dictionary<int, MapCharacter> MapCharacters = new Dictionary<int, MapCharacter>();

        // 刷怪管理器
        private SpawnManager SpawnManager = new SpawnManager();
        // 怪物管理器
        public MonsterManager MonsterManager = new MonsterManager();

        internal Map(MapDefine define)
        {
            this.Define = define;
            this.SpawnManager.Init(this);
            this.MonsterManager.Init(this);
        }

        public IEnumerable<Character> GetAllCharacters()
        {
            foreach (var kv in MapCharacters)
                yield return kv.Value.character;
        }

        internal void Update()
        {
            SpawnManager.Update();
            MonsterManager.Update();
        }

        #region 实体行为
        /// <summary>
        /// 角色进入地图
        /// </summary>
        /// <param name="character"></param>
        internal void CharacterEnter(NetConnection<NetSession> conn, Character character, bool sendNow)
        {
            Log.InfoFormat("[Map] CharacterEnter: Map:{0} characterId:{1}", this.Define.ID, character.Id);

            // 角色进入的是哪一张地图
            character.Info.mapId = this.ID;

            // 把“自己”存进“地图中的角色”容器
            this.MapCharacters[character.Id] = new MapCharacter(conn, character);

            // 1) 给“进入者”准备完整的进入地图列表（自己 + 地图已有玩家 + 地图怪物）
            //    注意：这里只是“填充 response”，不要在这里强行 SendResponse（由上层决定何时发送）
            conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;

            foreach (var kv in this.MapCharacters)
            {
                // 进入者收到：地图上所有角色（包含自己）
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.character.Info);

                // 其他在线玩家收到：这个新角色进入（增量广播）
                if (kv.Value.character != character)
                    this.AddCharacterEnterMap(kv.Value.connection, character.Info);
            }
            
            foreach (var kv in this.MonsterManager.Monsters)
            {
                conn.Session.Response.mapCharacterEnter.Monsters.Add(kv.Value.MonsterInfo);
            }

            // 2) 是否立刻发给进入者？
            //    - 进入游戏：sendNow = false（让 UserService.OnGameEnter 最后一次性发出 gameEnter + mapCharacterEnter）
            //    - 传送/切图：sendNow = true（希望立即看到切图结果）
            if (sendNow)
            {
                // 进入地图属于关键消息，如果有“限频/合包”，建议强制 flush，避免被 100ms 规则吞掉
                conn.Session.ForceFlush = true;
                conn.SendResponse();
            }
        }

        /// <summary>
        /// 角色离开地图
        /// </summary>
        /// <param name="info"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void CharacterLeave(Character cha)
        {
            Log.InfoFormat("[Map] CharacterLeave:Map {0} characterId: {1}", this.Define.ID, cha.Id);
            
            // 通知其他在线玩家本玩家离开
            foreach (var kv in this.MapCharacters)
            {
                this.SendCharacterLeaveMap(kv.Value.connection, cha);
            }
            this.MapCharacters.Remove(cha.Id);
        }

        /// <summary>
        /// 怪物进入地图
        /// </summary>
        /// <param name="character"></param>
        internal void MonsterEnter(Monster monster)
        {
            Log.InfoFormat("[Map] MonsterEnter:{0} monsterId:{1}", this.Define.ID, monster.Id);
            foreach (var kv in MapCharacters)
            {
                this.AddMonsterEnterMap(kv.Value.connection, monster.MonsterInfo);
            }
        }
        void AddMonsterEnterMap(NetConnection<NetSession> conn, NMonsterInfo monster)
        {
            if (conn.Session.Response.mapCharacterEnter == null)
            {
                conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
                conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;
            }
            // 怪物放进 Monsters 列表，不放 Characters
            conn.Session.Response.mapCharacterEnter.Monsters.Add(monster);
            conn.SendResponse();
        }
        #endregion


        #region 服务器广播发送给客户端

        /// <summary>
        /// 广播给服务器中的其他人本人进入地图
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="character"></param>
        void AddCharacterEnterMap(NetConnection<NetSession> conn, NCharacterInfo character)
        {
            if(conn.Session.Response.mapCharacterEnter == null)
            {
                conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
                conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;
            }
            conn.Session.Response.mapCharacterEnter.Characters.Add(character);
            conn.SendResponse();
        }

        /// <summary>
        /// 广播给服务器中的其他人本人离开地图
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="character"></param>
        void SendCharacterLeaveMap(NetConnection<NetSession> conn, Character character)
        {
            conn.Session.Response.mapCharacterLeave = new MapCharacterLeaveResponse();
            conn.Session.Response.mapCharacterLeave.entityId = character.entityId;
            conn.SendResponse();
        }

        #endregion

        /// <summary>
        /// 更新实体信息
        /// </summary>
        /// <param name="entitySync"></param>
        internal void UpdateEntity(NEntitySync entitySync)
        {
            foreach (var kv in this.MapCharacters)  
            {
                if (kv.Value.character.entityId == entitySync.Id)    
                {
                    // 如果kv是玩家本人，把自己的位置更新到服务器
                    kv.Value.character.Position = entitySync.Entity.Position;
                    kv.Value.character.Direction = entitySync.Entity.Direction;
                    kv.Value.character.Speed = entitySync.Entity.Speed;
                    if(entitySync.Event == EntityEvent.Ride) 
                    {
                        kv.Value.character.Ride = entitySync.Param;
                    }
                }
                else 
                {
                    // 如果kv是其他人,把自己的实体信息发送给其他人
                    MapService.Instance.SendEntityUpdate(kv.Value.connection, entitySync);
                }
            }
        }

        /// <summary>
        /// 把怪物状态变化广播给地图里所有玩家
        /// </summary>
        public void BroadcastMonsterState(MonsterStateSync sync)
        {
            foreach (var kv in MapCharacters)
            {
                var conn = kv.Value.connection;

                if (conn.Session.Response.monsterStateSync == null)
                    conn.Session.Response.monsterStateSync = new MonsterStateSyncResponse();

                conn.Session.Response.monsterStateSync.Syncs.Add(sync);
                conn.SendResponse();
            }
        }

    }
}
