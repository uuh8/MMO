using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Entities;
using SkillBridge.Message;

namespace GameServer.Managers
{
    class CharacterManager : Singleton<CharacterManager>
    {
        public Dictionary<int, Character> Characters = new Dictionary<int, Character>();

        public CharacterManager()
        {

        }

        public void Init()
        {

        }

        public void Clear()
        {
            this.Characters.Clear();
        }
        
        /// <summary>
        /// 创建/移除 Character 实体
        /// </summary>
        /// <param name="cha"></param>
        /// <returns></returns>
        public Character AddCharacter(TCharacter cha)
        {
            // 使用数据库中的数据创建实体(实体对象是在“进入游戏”这个过程中创建的）
            Character character = new Character(CharacterType.Player, cha);
            EntityManager.Instance.AddEntity(cha.MapID, character);
            character.Info.Id = character.Id;   // 将 Entity 的 Id 赋给网络的NCharacter的Id，否则客户端拿到的 Entity Id 是数据库的Table Id
            this.Characters[cha.ID] = character;
            return character;
        }
        public void RemoveCharacter(int characterId)
        {
            Character cha = this.Characters[characterId];
            EntityManager.Instance.RemoveEntity(cha.Data.MapID, cha);
            // 离开游戏会把角色从实体字典中删除，保证游戏服务器上都是"在线的"玩家
            this.Characters.Remove(characterId);
        }
    }
}
