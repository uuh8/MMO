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

namespace GameServer.Models
{
    class Map
    {
        internal class MapCharacter
        {
            public NetConnection<NetSession> connection;
            public Character character;

            public MapCharacter(NetConnection<NetSession> conn, Character cha)
            {
                this.connection = conn;
                this.character = cha;
            }
        }

        public int ID
        {
            get { return this.Define.ID; }
        }
        internal MapDefine Define;

        // 地图上的角色
        Dictionary<int, MapCharacter> MapCharacters = new Dictionary<int, MapCharacter>();


        internal Map(MapDefine define)
        {
            this.Define = define;
        }

        internal void Update()
        {
        }

        /// <summary>
        /// 角色进入地图
        /// </summary>
        /// <param name="character"></param>
        internal void CharacterEnter(NetConnection<NetSession> conn, Character character)
        {
            Log.InfoFormat("[Map] CharacterEnter: Map:{0} characterId:{1}", this.Define.ID, character.Id);

            // 进入的是哪一张地图
            character.Info.mapId = this.ID;

            // 构造角色进入地图的DTO
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            message.Response.mapCharacterEnter.mapId = this.Define.ID;
            message.Response.mapCharacterEnter.Characters.Add(character.Info);
            
            // 把“自己”进入地图的消息发送给其他服务器的其他角色
            foreach (var kv in this.MapCharacters)
            {
                message.Response.mapCharacterEnter.Characters.Add(kv.Value.character.Info);
                this.SendCharacterEnterMap(kv.Value.connection, character.Info);
            }
            
            // 把“自己”存进“地图中的角色”容器
            this.MapCharacters[character.Id] = new MapCharacter(conn, character);

            // 发送消息给自己的客户端，告知“自己已进入地图”
            byte[] data = PackageHandler.PackMessage(message);
            conn.SendData(data, 0, data.Length);
        }

        /// <summary>
        /// 发送给服务器中的其他人
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="character"></param>
        void SendCharacterEnterMap(NetConnection<NetSession> conn, NCharacterInfo character)
        {
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();

            message.Response.mapCharacterEnter = new MapCharacterEnterResponse();
            message.Response.mapCharacterEnter.mapId = this.Define.ID;
            message.Response.mapCharacterEnter.Characters.Add(character);

            byte[] data = PackageHandler.PackMessage(message);
            conn.SendData(data, 0, data.Length);
        }
    }
}
