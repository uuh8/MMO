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

        // 地图上当前在线的角色集合：Key = character Id
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

        internal void Update()
        {
            SpawnManager.Update();
        }

        #region 实体行为

        /// <summary>
        /// 角色进入地图
        /// </summary>
        /// <param name="character"></param>
        internal void CharacterEnter(NetConnection<NetSession> conn, Character character)
        {
            Log.InfoFormat("[Map] CharacterEnter: Map:{0} characterId:{1}", this.Define.ID, character.Id);

            // 角色进入的是哪一张地图
            character.Info.mapId = this.ID;
            // 把“自己”存进“地图中的角色”容器
            this.MapCharacters[character.Id] = new MapCharacter(conn, character);

            conn.Session.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            conn.Session.Response.mapCharacterEnter.mapId = this.Define.ID;

            foreach(var kv in this.MapCharacters)
            {
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.character.Info);
                if (kv.Value.character != character)
                    this.AddCharacterEnterMap(kv.Value.connection, character.Info);
            }
            // 把“自己”进入地图的消息广播给其他服务器的其他角色
            foreach (var kv in this.MonsterManager.Monsters)
            {
                conn.Session.Response.mapCharacterEnter.Characters.Add(kv.Value.Info);
            }
            conn.SendResponse();
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
                this.AddCharacterEnterMap(kv.Value.connection, monster.Info);
            }
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
            conn.Session.Response.mapCharacterLeave.characterId = character.Id;
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
                }
                else 
                {
                    // 如果kv是其他人,把自己的实体信息发送给其他人
                    MapService.Instance.SendEntityUpdate(kv.Value.connection, entitySync);
                }
            }
        }
    
    }
}
